using System.Collections;
using System.Text;
using Il2Cpp;
using MelonLoader;
using SuzerainModdingKit;
using UnityEngine;
using UnityEngine.UI;

namespace ZilleWar;

/// <summary>
/// Testing shortcuts inside a Zille operation (after deployment):
/// F6 takes the operation's objective on the real battlefield: the defenders are destroyed, a Rizian unit
///    moves in, the tile changes hands and the capture events and objectives fire as they would in battle.
/// F7 moves the operation to its last round. End that round to finish the operation (won or lost).
/// F8 (in a war) also saves a screenshot and logs what is drawn under the bottom HUD.
/// </summary>
internal static class WarDebug
{
    private static int _firstTurnSeen = int.MaxValue;
    private static WarFragmentData _trackedFragment;

    private static bool InOurOperation(out WarFlowManager flow)
    {
        flow = Managers.Instance?.WarFlowManager;
        if (flow == null || !ZilleOperation.IsOurFragment(flow.currentWarFragmentData))
        {
            Core.Log.Warning("Not in a Zille operation.");
            return false;
        }

        if (!flow.isInOperationPhase)
        {
            Core.Log.Warning("Finish the deployment first (press Start).");
            return false;
        }

        return true;
    }

    /// <summary>Called every frame: remembers the first war turn number of the current operation.</summary>
    public static void Track()
    {
        WarFlowManager flow = Managers.Instance?.WarFlowManager;
        if (flow == null || flow.currentWarFragmentData == null)
        {
            return;
        }

        if (flow.currentWarFragmentData.Pointer != (_trackedFragment?.Pointer ?? IntPtr.Zero))
        {
            _trackedFragment = flow.currentWarFragmentData;
            _firstTurnSeen = int.MaxValue;
        }

        if (flow.isInOperationPhase && flow.currentWarTurn < _firstTurnSeen)
        {
            _firstTurnSeen = flow.currentWarTurn;
        }
    }

    // ------------------------------------------------------------------ F6: reach the goal

    public static void ReachGoal()
    {
        if (!InOurOperation(out _))
        {
            return;
        }

        (int k, Objective objective) = ZilleCampaign.CurrentOperation();
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        WarTileManager tm = Managers.Instance.WarTileManager;
        var targets = new List<WarTile>();
        foreach (string town in info.Towns)
        {
            string tileName = ZilleCampaign.TownTile(town);
            WarTile t = tileName == null ? null : tm?.GetTileByName(tileName);
            if (t != null && t.currentAllegiance != WarToken.WarAllegiance.Player)
            {
                targets.Add(t);
            }
        }

        if (targets.Count == 0)
        {
            Core.Log.Msg($"Operation {k} ({info.Codename}): the objective is already held.");
            return;
        }

        Core.Log.Msg($"Operation {k} ({info.Codename}): taking {string.Join(", ", targets.Select(t => t.currentWarTileData.tileName))} by force.");
        MelonCoroutines.Start(Capture(targets, info));
    }

    private static IEnumerator Capture(List<WarTile> targets, ObjectiveInfo info)
    {
        WarTokenManager tokens = Managers.Instance.WarTokenManager;
        WarObjectiveManager objectives = Managers.Instance.WarObjectiveManager;
        foreach (WarTile tile in targets)
        {
            string n = tile.currentWarTileData.tileName;
            WarToken defender = tile.currentWarToken;
            if (defender != null && defender.allegiance == WarToken.WarAllegiance.Enemy)
            {
                Core.Log.Msg($"  {n}: destroying {(defender.IsCity() ? "city" : "unit")} '{defender.warUnitTypeData?.unitName}'");
                tokens.DestroyEnemyWarToken(n);
            }
        }

        for (int i = 0; i < 60; i++)
        {
            yield return null;
        }

        foreach (WarTile tile in targets)
        {
            string n = tile.currentWarTileData.tileName;
            tokens.SpawnWarToken("LandUnit_Infantry", n, "Player");
        }

        for (int i = 0; i < 90; i++)
        {
            yield return null;
        }

        foreach (WarTile tile in targets)
        {
            string n = tile.currentWarTileData.tileName;
            if (tile.currentAllegiance != WarToken.WarAllegiance.Player)
            {
                Core.Log.Msg($"  {n}: setting allegiance to Player directly");
                tile.SetTileAllegiance(WarToken.WarAllegiance.Player, true);
                objectives?.OnTileAllegianceChange(tile);
            }

            Core.Log.Msg($"  {n}: now {tile.currentAllegiance}, token '{tile.currentWarToken?.warUnitTypeData?.unitName}'");
        }

        foreach (string town in info.Towns)
        {
            Variables.Set(ZilleOperation.CapturedVar(town), true);
        }

        Core.Log.Msg("Objective taken. Press F7 to jump to the last round, or play on.");
    }

    // ------------------------------------------------------------------ F7: last round

    public static void LastRound()
    {
        if (!InOurOperation(out WarFlowManager flow))
        {
            return;
        }

        int limit = flow.warConfiguration?.phaseWarTurnLimit ?? 10;
        int first = _firstTurnSeen == int.MaxValue ? 1 : _firstTurnSeen;
        int last = first + limit - 1;
        Core.Log.Msg($"War turn {flow.currentWarTurn} -> {last} (first turn {first}, limit {limit}). End this round to finish the operation.");
        flow.currentWarTurn = last;
        flow.UpdateWarPlayerActionsValues();
    }

    // ------------------------------------------------------------------ F8 in a war: what is under the HUD

    public static void ProbeHud()
    {
        WarFlowManager flow = Managers.Instance?.WarFlowManager;
        if (flow == null || flow.currentWarFragmentData == null)
        {
            return;
        }

        string dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ZilleWar");
        string shot = Path.Combine(dir, "hud_probe.png");
        try
        {
            ScreenCapture.CaptureScreenshot(shot);
            Core.Log.Msg($"Screenshot: {shot}");
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Screenshot failed: {ex.Message}");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"HUD probe, screen {Screen.width}x{Screen.height}");
        Rect bottom = new(0, 0, Screen.width, Screen.height * 0.3f);   // screen space, origin bottom-left

        sb.AppendLine("-- UI graphics in the bottom 30% of the screen --");
        foreach (Graphic g in UnityEngine.Object.FindObjectsOfType<Graphic>())
        {
            if (g == null || !g.isActiveAndEnabled)
            {
                continue;
            }

            Rect r = ScreenRect(g.rectTransform, g.canvas);
            if (r.width < 4 || r.height < 4 || !r.Overlaps(bottom))
            {
                continue;
            }

            string tex = g.mainTexture != null ? g.mainTexture.name : "-";
            string mat = g.material != null ? $"{g.material.name}/{g.material.shader?.name}" : "-";
            string sprite = g.TryCast<Image>()?.sprite?.name ?? "-";
            sb.AppendLine($"{NodePath(g.transform)} [{g.GetIl2CppType().Name}] rect={Fmt(r)} color={g.color} alpha={g.canvasRenderer?.GetAlpha():0.00} " +
                          $"sprite={sprite} tex={tex} mat={mat} canvas={g.canvas?.name}/{g.canvas?.renderMode}");
        }

        sb.AppendLine("-- renderers (non-tile, non-token) under the bottom 30% --");
        Camera cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine($"camera {cam.name} pos={cam.transform.position} ortho={cam.orthographic} clear={cam.clearFlags} bg={cam.backgroundColor}");
            foreach (Renderer rd in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                if (rd == null || !rd.enabled || !rd.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string path = NodePath(rd.transform);
                if (path.Contains("Tile Map") || path.Contains("WarToken") || path.Contains("Token"))
                {
                    continue;
                }

                Bounds b = rd.bounds;
                Rect r = ScreenRectOf(cam, b);
                if (r.width < 4 || r.height < 4 || !r.Overlaps(bottom))
                {
                    continue;
                }

                Material m = rd.sharedMaterial;
                sb.AppendLine($"{path} [{rd.GetIl2CppType().Name}] rect={Fmt(r)} mat={m?.name}/{m?.shader?.name} tex={m?.mainTexture?.name}");
            }
        }

        File.WriteAllText(Path.Combine(dir, "hud_probe.txt"), sb.ToString());
        Core.Log.Msg($"HUD probe written to {Path.Combine(dir, "hud_probe.txt")}");
    }

    private static Rect ScreenRect(RectTransform rt, Canvas canvas)
    {
        var corners = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
        rt.GetWorldCorners(corners);
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Math.Min(minX, p.x);
            minY = Math.Min(minY, p.y);
            maxX = Math.Max(maxX, p.x);
            maxY = Math.Max(maxY, p.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static Rect ScreenRectOf(Camera cam, Bounds b)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 c = new(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z);
            Vector3 p = cam.WorldToScreenPoint(c);
            if (p.z < 0)
            {
                continue;
            }

            minX = Math.Min(minX, p.x);
            minY = Math.Min(minY, p.y);
            maxX = Math.Max(maxX, p.x);
            maxY = Math.Max(maxY, p.y);
        }

        return minX > maxX ? default : Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static string Fmt(Rect r) => $"({r.xMin:0},{r.yMin:0})-({r.xMax:0},{r.yMax:0})";

    private static string NodePath(Transform t)
    {
        var parts = new List<string>();
        for (Transform x = t; x != null && parts.Count < 8; x = x.parent)
        {
            parts.Add(x.name);
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
