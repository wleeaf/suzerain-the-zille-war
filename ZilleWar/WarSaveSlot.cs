using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The game keeps one war-map snapshot in the save (SaveData.warSaveData). It writes it when a phase of an operation ends
/// (not the last), resumes the next phase of the same operation from it, and re-applies its tiles by name to every war map
/// it sets up. The Zille operation uses it exactly like vanilla's Golden Shield: Phases II and III resume from it. Around
/// that:
/// - Phase I (or a phase that must be set up fresh) starts from an empty slot, so a leftover snapshot can't redraw the front
///   (a Pales snapshot, which can't exist this early in vanilla's calendar, would be put aside and given back afterwards).
/// - When the Zille war ends, the Zille snapshot is cleared and the operation marked finished, so nothing of the Zille map
///   leaks into the Pales war's maps later (the tiles share names).
/// </summary>
internal static class WarSaveSlot
{
    private const string AnyOperationInProgress = "RiziaDLCSetup.IsAnyOperationInProgress";
    private static WarSaveData _stashed;

    private static SaveData Save => Singleton<PersistenceManager>.Instance?.activeSaveFile?.saveData;

    /// <summary>True when the save holds the map of a Zille phase that ended (with its units), ready for the next phase.</summary>
    public static bool HoldsZilleSnapshot()
    {
        try
        {
            WarSaveData current = Save?.warSaveData;
            return current != null && ZilleOperation.IsOurFragmentName(current.warFragmentName) && (current.warTokenSaveData?.Count ?? 0) > 0;
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not read the war save slot: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Called right before a Zille war fragment is set up. A phase that resumes keeps the slot; a phase that starts fresh
    /// gets an empty one.
    /// </summary>
    public static void PrepareForZille(WarFragmentData fragment)
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
            int tokens = current?.warTokenSaveData?.Count ?? 0;
            OperationData op = ZilleOperation.FindStoryOperation(fragment?.WarFragmentProperties?.Operation);
            int index = op?.OperationProperties?.Phases?.IndexOf(fragment?.NameInDatabase) ?? -1;
            if (index >= 1)
            {
                Core.Log.Msg($"Phase {index + 1} resumes the map saved by '{owner}' ({tokens} tokens, {tiles} tiles).");
                return;
            }

            if (!string.IsNullOrEmpty(owner) && !ZilleOperation.IsOurFragmentName(owner))
            {
                _stashed = new WarSaveData(current);
                Core.Log.Msg($"War save slot held '{owner}' ({tiles} tiles); set aside until the Zille phase ends.");
            }
            else if (tiles > 0 || tokens > 0)
            {
                Core.Log.Msg($"War save slot held a stale map ({tiles} tiles, '{owner}'); cleared.");
            }

            Singleton<PersistenceManager>.Instance.SetWarSaveData(new WarSaveData());
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not prepare the war save slot: {ex.Message}");
        }
    }

    /// <summary>Called when the base map loads again: give a Pales operation its snapshot back, unless a Zille phase is waiting.</summary>
    public static void RestoreAfterZille()
    {
        if (_stashed == null || (HoldsZilleSnapshot() && WarFlow.AtWar))
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

    /// <summary>
    /// The Zille war is over (last phase played, every objective taken, or a ceasefire before the last phase): clear the Zille
    /// snapshot and mark the operation finished, as the game does when an operation ends. Idempotent.
    /// </summary>
    public static void EndZilleOperation()
    {
        try
        {
            if (HoldsZilleSnapshot() || ZilleOperation.IsOurFragmentName(Save?.warSaveData?.warFragmentName))
            {
                Singleton<PersistenceManager>.Instance.SetWarSaveData(new WarSaveData());
                Core.Log.Msg("The Zille war is over: its war map snapshot was cleared.");
                if (_stashed == null)
                {
                    SuzerainModdingKit.Variables.Set(AnyOperationInProgress, false);
                }
            }

            OperationData op = ZilleOperation.FindStoryOperation(ZilleOperation.MainOperation);
            if (op?.OperationProperties != null && !op.OperationProperties.IsFinished)
            {
                op.FinishOperation();
                Core.Log.Msg("Operation Northern Crown marked finished.");
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not close the Zille operation: {ex.Message}");
        }

        RestoreAfterZille();
    }
}
