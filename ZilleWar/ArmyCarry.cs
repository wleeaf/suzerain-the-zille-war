using System.Collections;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using SuzerainModdingKit;
using UnityEngine;

namespace ZilleWar;

/// <summary>
/// The army stays where it stood between operations. When a Zille operation ends, every surviving Rizian unit is
/// recorded (type, tile, organization) in a saved variable. When the next operation's deployment opens, the units are
/// deployed again on those tiles through the deployment panel itself, so the reserve counts stay honest and the player
/// can still adjust before pressing Start. Damaged units keep their reduced organization.
/// </summary>
internal static class ArmyCarry
{
    public const string VarArmy = "ZilleWar.State_PlayerArmy";   // "unitType|tile|organization;..."
    private static IntPtr _restoredFor = IntPtr.Zero;

    public static void Register() => Variables.Register(VarArmy);

    /// <summary>At the end of an operation, while the map still exists.</summary>
    public static void Record()
    {
        try
        {
            WarTokenManager tokens = Managers.Instance?.WarTokenManager;
            var list = tokens?.GetWarTokens();
            if (list == null)
            {
                return;
            }

            var parts = new List<string>();
            foreach (WarToken t in list)
            {
                if (t == null || t.isDead || t.allegiance != WarToken.WarAllegiance.Player || t.IsCity() ||
                    t.warUnitTypeData == null || t.currentWarTile?.currentWarTileData == null)
                {
                    continue;
                }

                parts.Add($"{t.warUnitTypeData.name}|{t.currentWarTile.currentWarTileData.tileName}|{t.remainingOrganization}");
            }

            if (parts.Count == 0)
            {
                return;     // the map is already being torn down: keep what an earlier call recorded
            }

            Variables.Set(VarArmy, string.Join(";", parts));
            Core.Log.Msg($"Army carried over: {parts.Count} unit(s) recorded where they stand.");
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Recording the army failed: {ex.Message}");
        }
    }

    /// <summary>The deployment panel of a Zille operation has opened: put the army back.</summary>
    internal static void OnDeploymentShown(WarDeploymentPanel panel)
    {
        WarFlowManager flow = Managers.Instance?.WarFlowManager;
        if (panel == null || flow == null || !ZilleOperation.IsOurFragment(flow.currentWarFragmentData) || flow.isInOperationPhase)
        {
            return;
        }

        IntPtr frag = flow.currentWarFragmentData.Pointer;
        if (frag == _restoredFor)
        {
            return;
        }

        _restoredFor = frag;
        string saved = Variables.GetString(VarArmy);
        if (string.IsNullOrEmpty(saved) || saved == "nil")
        {
            return;
        }

        MelonCoroutines.Start(Restore(panel, saved));
    }

    private static IEnumerator Restore(WarDeploymentPanel panel, string saved)
    {
        for (int i = 0; i < 30; i++)
        {
            yield return null;     // let the panel and the tokens finish setting up
        }

        WarTileManager tiles = Managers.Instance?.WarTileManager;
        var types = Managers.Instance?.WarFlowManager?.warConfiguration?.allWarUnitTypeDatas;
        if (tiles == null || types == null)
        {
            yield break;
        }

        int placed = 0, skipped = 0;
        foreach (string entry in saved.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] f = entry.Split('|');
            if (f.Length < 3)
            {
                continue;
            }

            try
            {
                WarUnitTypeData type = null;
                foreach (WarUnitTypeData u in types)
                {
                    if (u != null && u.name == f[0])
                    {
                        type = u;
                        break;
                    }
                }

                WarTile tile = tiles.GetTileByName(f[1]);
                if (type == null || tile == null || tile.currentAllegiance != WarToken.WarAllegiance.Player || tile.currentWarToken != null)
                {
                    skipped++;
                    continue;
                }

                panel.DeployToken(type, tile);
                WarToken token = tile.currentWarToken;
                if (token != null && int.TryParse(f[2], out int org) && org > 0 && org < token.remainingOrganization)
                {
                    token.remainingOrganization = org;
                }

                placed += token != null ? 1 : 0;
                skipped += token == null ? 1 : 0;
            }
            catch (Exception ex)
            {
                skipped++;
                Core.Log.Warning($"  could not redeploy '{entry}': {ex.Message}");
            }
        }

        Core.Log.Msg($"Army carried over: {placed} unit(s) back in position, {skipped} could not be placed (tile lost, occupied or no reserve).");
    }
}

[HarmonyPatch(typeof(WarDeploymentPanel), nameof(WarDeploymentPanel.Show))]
internal static class Patch_DeploymentShown
{
    private static void Postfix(WarDeploymentPanel __instance)
    {
        try
        {
            ArmyCarry.OnDeploymentShown(__instance);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Army carry-over failed: {ex.Message}");
        }
    }
}
