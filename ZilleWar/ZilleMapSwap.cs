using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace ZilleWar;

/// <summary>
/// When a Zille war fragment starts, turn the freshly loaded Pales war map into the Zille map:
/// swap the six panel textures, replace the 133 Pales tiles with ours, widen the camera bounds.
/// Runs right before WarTileManager collects its tiles.
/// </summary>
/// <summary>Remember which war fragment is being set up (WarFlowManager keeps it in a coroutine closure).</summary>
[HarmonyPatch(typeof(WarFlowManager), nameof(WarFlowManager.SetupWarFragment))]
internal static class Patch_WarFlowManager_SetupWarFragment
{
    internal static WarFragmentData Current;

    private static void Prefix(WarFragmentData warFragmentData)
    {
        Current = warFragmentData;
        Core.Log.Msg($"SetupWarFragment('{warFragmentData?.NameInDatabase}', op='{warFragmentData?.WarFragmentProperties?.Operation}')");
        if (ZilleOperation.IsOurFragment(warFragmentData))
        {
            OwnershipCheck.Reset();
            WarSaveSlot.ClearForZille(warFragmentData);
        }
    }
}

// SetupCoroutine()/Setup() are tiny wrappers that IL2CPP inlines, so hooks on them never fire.
// Hook the iterators' MoveNext instead (called through IEnumerator, never inlined) and swap on the
// first step where the war map exists — before the tile manager collects its tiles.
[HarmonyPatch(typeof(WarTileManager._SetupCoroutine_d__61), nameof(WarTileManager._SetupCoroutine_d__61.MoveNext))]
internal static class Patch_TileSetup_MoveNext
{
    private static void Prefix(WarTileManager._SetupCoroutine_d__61 __instance)
    {
        Step("WarTileManager.SetupCoroutine", __instance.__1__state);
    }

    internal static void Step(string where, int state)
    {
        try
        {
            Core.Log.Msg($"{where} step state={state}");
            ZilleMapSwap.TryApply();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Zille map swap failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(WarFlowManager._SetupWarFragmentCoroutine_d__65), nameof(WarFlowManager._SetupWarFragmentCoroutine_d__65.MoveNext))]
internal static class Patch_WarFragmentSetup_MoveNext
{
    private static void Prefix(WarFlowManager._SetupWarFragmentCoroutine_d__65 __instance)
    {
        Patch_TileSetup_MoveNext.Step("WarFlowManager.SetupWarFragmentCoroutine", __instance.__1__state);
    }
}

internal static class ZilleMapSwap
{
    private const string SwappedSuffix = " [Zille]";
    private static Bounds? _originalBounds;
    private static MapConfiguration _config;

    public static void TryApply()
    {
        Managers m = Managers.Instance;
        WarFragmentData frag = Patch_WarFlowManager_SetupWarFragment.Current;
        MapContainer container = m?.MapManager?.CurrentlyLoadedMap;
        GameObject root = container?.mapGameObject;
        if (!ZilleOperation.IsOurFragment(frag))
        {
            RestoreBounds();
            RestoreVanillaBlockades();
            SetTableProps(true);
            if (root != null && root.name.EndsWith(SwappedSuffix))
            {
                Core.Log.Warning("A vanilla war is starting on a map that was converted to Zille. Reload the save before playing the Pales war.");
            }

            return;
        }

        if (root == null || !root.name.StartsWith("Map_Rizia_PalesWar"))
        {
            return;     // war map not loaded yet; a later coroutine step will retry
        }

        if (root.name.EndsWith(SwappedSuffix))
        {
            return;
        }

        SwapPanels(root.transform, out Rect mapRect);
        BuildTiles(root.transform, mapRect);
        DisableVanillaBlockades();
        SetTableProps(false);
        root.name += SwappedSuffix;
        Core.Log.Msg("Zille war map ready.");
    }

    private static void SwapPanels(Transform root, out Rect mapRect)
    {
        Texture2D[] panels = ZilleData.Panels;
        float xmin = float.MaxValue, xmax = float.MinValue, ymin = float.MaxValue, ymax = float.MinValue;
        int swapped = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            string n = c.name.Trim();
            if (n == "PalesReferenceMap")
            {
                c.gameObject.SetActive(false);
                continue;
            }

            if (!n.StartsWith("Pales Map_") || !int.TryParse(n.Substring("Pales Map_".Length), out int idx) || idx < 1 || idx > 6)
            {
                continue;
            }

            Vector3 p = c.localPosition, s = c.localScale;
            xmin = Math.Min(xmin, p.x - s.x / 2);
            xmax = Math.Max(xmax, p.x + s.x / 2);
            ymin = Math.Min(ymin, p.y - s.y / 2);
            ymax = Math.Max(ymax, p.y + s.y / 2);

            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            Material mat = mr.material;
            foreach (string prop in mat.GetTexturePropertyNames())
            {
                Texture t = mat.GetTexture(prop);
                if (t != null && t.name.StartsWith("T_Map_Pales"))
                {
                    mat.SetTexture(prop, panels[idx - 1]);
                    swapped++;
                }
            }

            c.name = $"Zille Map_0{idx}";
        }

        mapRect = Rect.MinMaxRect(xmin, ymin, xmax, ymax);
        Core.Log.Msg($"Swapped {swapped} panel textures; map rect {mapRect}");
    }

    private static void BuildTiles(Transform root, Rect mapRect)
    {
        Transform tileMap = root.Find("Tile Map");
        var oldTiles = new List<GameObject>();
        for (int i = 0; i < tileMap.childCount; i++)
        {
            oldTiles.Add(tileMap.GetChild(i).gameObject);
        }

        GameObject template = oldTiles.First(g => g.GetComponent<WarTile>() != null);
        Vector3 tplPos = template.transform.localPosition;
        Vector3 tplScale = template.transform.localScale;
        Quaternion tplRot = template.transform.localRotation;
        string texProp = WarTileManager.tileTextureReference;
        Core.Log.Msg($"Tile template '{template.name}' texProp='{texProp}' material='{template.GetComponent<MeshRenderer>().sharedMaterial?.name}'");

        // Tile Map gets an identity transform so tile local coordinates equal map-local coordinates.
        Vector3 tmPos = tileMap.localPosition;
        tileMap.localPosition = new Vector3(0, 0, tmPos.z);
        tileMap.localScale = Vector3.one;

        var created = new Dictionary<string, WarTile>();
        foreach (TileSpec spec in ZilleData.Tiles)
        {
            GameObject go = UnityEngine.Object.Instantiate(template, tileMap);
            go.name = $"Tile_{spec.Name}";
            float x = mapRect.xMin + spec.QuadCenterUv[0] * mapRect.width;
            float y = mapRect.yMax - spec.QuadCenterUv[1] * mapRect.height;
            go.transform.localPosition = new Vector3(x, y, tplPos.z);
            go.transform.localRotation = tplRot;
            go.transform.localScale = new Vector3(spec.QuadSizeUv[0] * mapRect.width, spec.QuadSizeUv[1] * mapRect.height, tplScale.z);

            Texture2D tex = ZilleData.TileTexture(spec.Name);
            WarTile wt = go.GetComponent<WarTile>();
            wt.warTileTexture = tex;
            wt.tokenCenterPositionOverride = null;
            wt.currentWarTileData = new WarTileData
            {
                tileName = spec.Name,
                tileType = spec.Type == "Sea" ? WarTile.TileType.Sea : WarTile.TileType.Land,
                warTileStatChanges = new Il2CppSystem.Collections.Generic.List<WarTile.WarTileStatChange>(),
            };
            wt.adjacentWarTiles = new Il2CppSystem.Collections.Generic.List<WarTile>();

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            Material mat = mr.material;
            if (!string.IsNullOrEmpty(texProp) && mat.HasProperty(texProp))
            {
                mat.SetTexture(texProp, tex);
            }

            created[spec.Name] = wt;
        }

        foreach (TileSpec spec in ZilleData.Tiles)
        {
            WarTile wt = created[spec.Name];
            foreach (string nb in spec.Neighbors)
            {
                if (created.TryGetValue(nb, out WarTile other))
                {
                    wt.adjacentWarTiles.Add(other);
                }
            }
        }

        foreach (GameObject g in oldTiles)
        {
            UnityEngine.Object.DestroyImmediate(g);
        }

        Core.Log.Msg($"Replaced {oldTiles.Count} Pales tiles with {created.Count} Zille tiles.");
    }

    private static void WidenBounds(MapContainer container, Transform root)
    {
        _config = container.GetMapConfiguration();
        if (_config == null)
        {
            return;
        }

        var b = new Bounds();
        bool first = true;
        foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (!mr.gameObject.name.StartsWith("Zille Map_"))
            {
                continue;
            }

            if (first)
            {
                b = mr.bounds;
                first = false;
            }
            else
            {
                b.Encapsulate(mr.bounds);
            }
        }

        _originalBounds ??= _config.bounds;
        Bounds o = _originalBounds.Value;
        // The Pales art fills ~96% x 51.5% of its texture; keep the same camera margin around our full-bleed art.
        float marginX = b.extents.x * 0.96f - o.extents.x;
        float marginZ = b.extents.z * 0.515f - o.extents.z;
        var nb = new Bounds(new Vector3(b.center.x, o.center.y, b.center.z),
            new Vector3(Math.Max(0.05f, b.extents.x - marginX) * 2, o.size.y, Math.Max(0.05f, b.extents.z - marginZ) * 2));
        _config.bounds = nb;
        Core.Log.Msg($"Camera bounds {o} -> {nb} (map world bounds {b})");
    }

    private static Il2CppSystem.Collections.Generic.List<WarNavalBlockade> _vanillaBlockades;

    /// <summary>WarConfiguration's naval blockades name Pales sea tiles (S8, S12...); our S-tiles are elsewhere.</summary>
    private static void DisableVanillaBlockades()
    {
        WarConfiguration cfg = Managers.Instance?.WarFlowManager?.warConfiguration;
        if (cfg == null || _vanillaBlockades != null)
        {
            return;
        }

        _vanillaBlockades = cfg.navalBlockades;
        cfg.navalBlockades = new Il2CppSystem.Collections.Generic.List<WarNavalBlockade>();
        Core.Log.Msg($"Disabled {_vanillaBlockades?.Count} Pales naval blockades for the Zille war.");
    }

    // The King's headphones sit on the table under the bottom-right corner of the Zille sheet and show
    // through as a black shape behind the action points and the End Turn button.
    private static readonly string[] HiddenTableProps =
    {
        "Environments/Environment_Rizia_Default/Table_Rizia_Default/Objects/Persistent Decorative Objects/Heaphones",
    };

    private static readonly List<GameObject> _hiddenProps = new();

    internal static void SetTableProps(bool visible)
    {
        if (visible)
        {
            foreach (GameObject g in _hiddenProps)
            {
                if (g != null)
                {
                    g.SetActive(true);
                }
            }

            _hiddenProps.Clear();
            return;
        }

        foreach (string path in HiddenTableProps)
        {
            GameObject g = GameObject.Find(path);
            if (g != null && g.activeSelf)
            {
                g.SetActive(false);
                _hiddenProps.Add(g);
            }
        }
    }

    private static void RestoreVanillaBlockades()
    {
        WarConfiguration cfg = Managers.Instance?.WarFlowManager?.warConfiguration;
        if (cfg != null && _vanillaBlockades != null)
        {
            cfg.navalBlockades = _vanillaBlockades;
            _vanillaBlockades = null;
        }
    }

    private static void RestoreBounds()
    {
        if (_config != null && _originalBounds.HasValue)
        {
            _config.bounds = _originalBounds.Value;
        }
    }
}

/// <summary>Dev tracing of the war setup pipeline.</summary>
[HarmonyPatch(typeof(WarOperationManager), nameof(WarOperationManager.Setup))]
internal static class Trace_WarOperationManager_Setup
{
    private static void Prefix(OperationData operationData) =>
        Core.Log.Msg($"WarOperationManager.Setup('{operationData?.NameInDatabase}')");
}

[HarmonyPatch(typeof(MapManager), nameof(MapManager.LoadMap), typeof(MapConfiguration.MapType))]
internal static class Trace_MapManager_LoadMap
{
    private static void Prefix(MapConfiguration.MapType mapType)
    {
        Core.Log.Msg($"MapManager.LoadMap({mapType})");
        if (mapType != MapConfiguration.MapType.War)
        {
            ZilleMapSwap.SetTableProps(true);
            WarSaveSlot.RestoreAfterZille();
        }
    }
}

/// <summary>
/// Decide the Zille operation's result just before the game finishes it. Vanilla does this with a
/// conditional instruction (articy:expresso), which can't see custom variables, so we set it here.
/// Victory = the operation's target city taken (Clear: no Wehlen-held land left).
/// </summary>
[HarmonyPatch(typeof(WarFlowManager._FinishOperationCoroutine_d__111), nameof(WarFlowManager._FinishOperationCoroutine_d__111.MoveNext))]
internal static class Patch_FinishOperation_MoveNext
{
    private static void Prefix(WarFlowManager._FinishOperationCoroutine_d__111 __instance)
    {
        if (__instance.__1__state != 0 || !ZilleOperation.IsOurFragment(Patch_WarFlowManager_SetupWarFragment.Current))
        {
            return;
        }

        (int k, Objective objective) = ZilleCampaign.CurrentOperation();
        ZilleCampaign.OnOperationFinished(k, objective);
        WarFlow.OnOperationFinished();
    }
}
