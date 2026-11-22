using HarmonyLib;
using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// Makes each operation open on the front where the last one ended. After the war table has built its
/// tiles, every land tile is compared with the campaign's saved ownership; any tile the engine set up
/// differently (for example back to its pre-war owner) is handed to its rightful side before the units
/// are placed.
/// </summary>
[HarmonyPatch(typeof(WarTileManager), nameof(WarTileManager.SetupReinforcementTiles))]
internal static class OwnershipCheck
{
    private static IntPtr _checkedFragment = IntPtr.Zero;

    /// <summary>Called when a war fragment is set up, so the next map build is checked again.</summary>
    public static void Reset() => _checkedFragment = IntPtr.Zero;

    private static void Postfix(WarTileManager __instance)
    {
        try
        {
            WarFlowManager flow = Managers.Instance?.WarFlowManager;
            WarFragmentData onTable = Patch_WarFlowManager_SetupWarFragment.OnTable(flow);
            if (flow == null || !ZilleOperation.IsOurFragment(onTable) || __instance?.allTiles == null)
            {
                return;
            }

            // The game also calls SetupReinforcementTiles whenever a tile changes hands during play. Only the first call of
            // each operation (the map being built) may be checked; later ones are real captures and must stand.
            IntPtr fragment = onTable.Pointer;
            if (flow.isInOperationPhase || fragment == _checkedFragment)
            {
                return;
            }

            _checkedFragment = fragment;

            Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
            int held = 0, fixedTiles = 0;
            foreach (WarTile t in __instance.allTiles)
            {
                string n = t?.currentWarTileData?.tileName;
                if (n == null || !owners.TryGetValue(n, out bool player))
                {
                    continue;
                }

                // The paratroopers' ground starts Rizian on a fresh map although the saved front still shows it Wehzek.
                player |= n == ZilleOperation.FreshDropZone;

                if (player && ZilleData.ByName[n].Region == "zille")
                {
                    held++;
                }

                WarToken.WarAllegiance want = player ? WarToken.WarAllegiance.Player : WarToken.WarAllegiance.Enemy;
                if (t.currentAllegiance != want)
                {
                    Core.Log.Msg($"  tile {n}: set up as {t.currentAllegiance}, campaign says {want}; correcting.");
                    t.SetTileAllegiance(want, false);
                    fixedTiles++;
                }
            }

            Core.Log.Msg($"Ownership check: {held} Zille tiles in Rizian hands carried over, {fixedTiles} tile(s) corrected.");
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Ownership check failed: {ex.Message}");
        }
    }
}
