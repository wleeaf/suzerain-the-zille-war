using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The game keeps one war-map snapshot in the save (SaveData.warSaveData), meant for the next phase of the
/// same operation, and re-applies it by tile name to every war map it sets up. Our tiles share names with
/// the Pales map, so a leftover snapshot could redraw the Zille front. Before a Zille operation the slot is
/// emptied (a Pales snapshot is put aside), and once the Zille operation is over the Pales snapshot goes back.
/// </summary>
internal static class WarSaveSlot
{
    private const string AnyOperationInProgress = "RiziaDLCSetup.IsAnyOperationInProgress";
    private static WarSaveData _stashed;

    private static SaveData Save => Singleton<PersistenceManager>.Instance?.activeSaveFile?.saveData;

    /// <summary>Called right before a Zille war fragment is set up.</summary>
    public static void ClearForZille(WarFragmentData fragment)
    {
        try
        {
            SaveData save = Save;
            if (save == null)
            {
                return;
            }

            WarSaveData current = save.warSaveData;
            string owner = current?.warFragmentName;
            int tiles = current?.warTileSaveData?.Count ?? 0;
            if (!string.IsNullOrEmpty(owner) && !ZilleOperation.IsOurFragmentName(owner))
            {
                _stashed = new WarSaveData(current);
                Core.Log.Msg($"War save slot held '{owner}' ({tiles} tiles); set aside until the Zille operation ends.");
            }
            else if (tiles > 0)
            {
                Core.Log.Msg($"War save slot held a stale map ({tiles} tiles, '{owner}'); cleared.");
            }

            Singleton<PersistenceManager>.Instance.SetWarSaveData(new WarSaveData());
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not clear the war save slot: {ex.Message}");
        }
    }

    /// <summary>Called when the base map loads again: give a Pales operation its snapshot back.</summary>
    public static void RestoreAfterZille()
    {
        if (_stashed == null)
        {
            return;
        }

        try
        {
            Singleton<PersistenceManager>.Instance.SetWarSaveData(_stashed);
            SuzerainModdingKit.Variables.Set(AnyOperationInProgress, true);
            Core.Log.Msg($"Restored the war save slot for '{_stashed.warFragmentName}'.");
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not restore the war save slot: {ex.Message}");
        }

        _stashed = null;
    }
}
