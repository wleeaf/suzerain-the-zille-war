using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// The mod's variables follow the game being played, through every way a game starts or comes back.
/// How a game is entered: the game scene loads, the entity lists are rebuilt, then PersistenceManager.LoadSaveFile runs on the
/// active save (also after "New Game", "Continue" and "Load"). It resets the Lua environment to that save's variables (which
/// never include the mod's: the modding kit keeps those in moddingkit/&lt;save&gt;.json), and the kit's postfix then loads the
/// mod file (active.json for the active save; a save without a mod file deletes active.json and loads nothing).
/// - New game: CreateNewCampaign makes a fresh active save but leaves active.json alone, so the kit loaded the game left behind
///   into the new one (Wehlen hostile on Turn 1, the victory's public opinion, the Caleqabiz lease, the CSP embargo). Right
///   after the kit's load, every registered ZilleWar.* variable is cleared (nil, as on the first game after install) and the
///   stale active.json is deleted, before the game evaluates its first step. A clear before the first step evaluation is kept as
///   a fallback in case a new campaign is ever entered without LoadSaveFile.
/// - Stale save: a game before Turn 4 that claims a declared war carries data leaked by an earlier build; it is cleared the same
///   way, on load and before every step evaluation of Turns 1-3.
/// - Checkpoint: the game's checkpoint (taken when the war production is confirmed, before a battle) holds the vanilla variables
///   only; "Load last checkpoint" then reloaded the mod's state from active.json, which can be the state after that battle
///   (an autosave at its end) or before steps taken since the last save. The mod's variables are snapshotted with the checkpoint
///   and put back when it is loaded, and active.json is rewritten to match.
/// </summary>
internal static class NewGameReset
{
    private static bool _pending;                          // a new campaign was created and its first load has not been seen
    private static Dictionary<string, object> _checkpoint; // the mod's variables when the game's checkpoint was taken
    private static bool _restoringCheckpoint;              // "Load last checkpoint" is in progress (until the next step evaluation)

    internal static void MarkNewCampaign()
    {
        _pending = true;
        _checkpoint = null;
        _restoringCheckpoint = false;
    }

    /// <summary>Postfix of LoadSaveFile, after the modding kit's own postfix has loaded the save's mod file.</summary>
    internal static void OnSaveLoaded(SaveFile saveFile)
    {
        SaveData data = saveFile?.saveData;
        bool active = data != null && data.saveFileType == PersistenceManager.SaveFileType.Active;
        if (_pending)
        {
            _pending = false;
            if (active)
            {
                int n = ClearAll();
                DeleteActiveModFile();
                Core.Log.Msg($"New game: cleared {n} Zille War variables left by the previous game.");
                return;
            }
        }

        if (_restoringCheckpoint && _checkpoint != null)
        {
            RestoreCheckpoint();
            return;
        }

        // Any other load (Continue, Load) clears the game's checkpoint too; the snapshot is dropped with it even if a clear was missed.
        _checkpoint = null;

        int turn = data?.turnNo ?? 0;
        bool rizia = data?.currentStoryPack?.Contains("Rizia", StringComparison.OrdinalIgnoreCase) ?? false;
        if (rizia && turn is > 0 and < 4 && Variables.GetBool(WarFlags.Declared))
        {
            int n = ClearAll();
            Core.Log.Msg($"Turn {turn} save carried a declared war: cleared {n} stale Zille War variables.");
        }
    }

    /// <summary>Prefix of GameFlowManager.EvaluateCurrentStep, ahead of every other handler of the step.</summary>
    internal static void BeforeStep()
    {
        _restoringCheckpoint = false;
        if (_pending)
        {
            _pending = false;
            int cleared = ClearAll();
            DeleteActiveModFile();
            Core.Log.Msg($"New game (first step): cleared {cleared} Zille War variables left by the previous game.");
        }

        if (!SuzerainModdingKit.GameState.IsCurrentStoryPack(SuzerainModdingKit.VanillaData.SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        int turn = SuzerainModdingKit.GameState.CurrentTurnNum ?? 0;
        if (turn is > 0 and < 4 && Variables.GetBool(WarFlags.Declared))
        {
            int n = ClearAll();
            Core.Log.Msg($"Turn {turn} game carried a declared war: cleared {n} stale Zille War variables.");
        }
    }

    // ------------------------------------------------------------------ checkpoint

    internal static void OnCheckpointCreated()
    {
        var snap = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string name in Registered())
        {
            if (!DialogueLua.DoesVariableExist(name))
            {
                continue;
            }

            Lua.Result r = DialogueLua.GetVariable(name);
            if (r.IsBool)
            {
                snap[name] = r.AsBool;
            }
            else if (r.IsNumber)
            {
                snap[name] = r.AsFloat;
            }
            else if (r.IsString)
            {
                snap[name] = r.AsString;
            }
        }

        _checkpoint = snap;
        _restoringCheckpoint = false;
    }

    internal static void OnCheckpointCleared()
    {
        _checkpoint = null;
        _restoringCheckpoint = false;
    }

    internal static void OnLoadLastCheckpoint() => _restoringCheckpoint = _checkpoint != null;

    private static void RestoreCheckpoint()
    {
        int n = 0;
        foreach (string name in Registered())
        {
            if (_checkpoint.TryGetValue(name, out object v))
            {
                Variables.Set(name, v);
            }
            else
            {
                Lua.Run($"Variable[\"{name}\"] = nil");
            }

            n++;
        }

        SaveActiveModFile();
        Core.Log.Msg($"Checkpoint loaded: {n} Zille War variables put back as they were when it was taken.");
    }

    // ------------------------------------------------------------------ helpers

    private static IEnumerable<string> Registered()
    {
        var field = typeof(Variables).GetField("RegisteredVariables", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is not List<string> names)
        {
            Core.Log.Warning("Zille War state: the modding kit's variable list was not found.");
            return Array.Empty<string>();
        }

        return names.Where(n => n.StartsWith("ZilleWar.", StringComparison.Ordinal)).ToList();
    }

    private static int ClearAll()
    {
        int n = 0;
        foreach (string name in Registered())
        {
            Lua.Run($"Variable[\"{name}\"] = nil");
            n++;
        }

        return n;
    }

    private static Type SaveManager => typeof(Variables).Assembly.GetType("SuzerainModdingKit.Save.SaveManager");

    /// <summary>The kit's active.json held the game left behind; the new game has no mod data yet.</summary>
    private static void DeleteActiveModFile()
    {
        try
        {
            string dir = SaveManager?.GetField("ModSavePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as string;
            string path = dir == null ? null : Path.Combine(dir, "active.json");
            if (path != null && File.Exists(path))
            {
                File.Delete(path);
                Core.Log.Msg("New game: the previous game's moddingkit/active.json was deleted.");
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"New game: could not delete the previous game's active.json: {ex.Message}");
        }
    }

    /// <summary>Write active.json from the current variables, as the kit does when the game saves its active file.</summary>
    private static void SaveActiveModFile()
    {
        try
        {
            SaveManager?.GetMethod("Save", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] { "active.json", true });
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Checkpoint: could not rewrite active.json: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.CreateNewCampaign))]
internal static class Patch_NewCampaignReset
{
    private static void Prefix() => NewGameReset.MarkNewCampaign();
}

/// <summary>After the modding kit's postfix (it loads the save's mod file), whatever the order the mods were patched in.</summary>
[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.LoadSaveFile))]
internal static class Patch_SaveLoadedReset
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(SaveFile saveFile)
    {
        try
        {
            NewGameReset.OnSaveLoaded(saveFile);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Zille War state on load failed: {ex}");
        }
    }
}

/// <summary>Before anything else evaluates the step: the game's own story fragment conditions, the scheduler, the vanilla gates.</summary>
[HarmonyPatch(typeof(GameFlowManager), nameof(GameFlowManager.EvaluateCurrentStep))]
internal static class Patch_ResetBeforeStepEvaluation
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix()
    {
        try
        {
            NewGameReset.BeforeStep();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"New game reset failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.CreateCheckpoint))]
internal static class Patch_CheckpointSnapshot
{
    private static void Postfix()
    {
        try
        {
            NewGameReset.OnCheckpointCreated();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Checkpoint snapshot failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.ClearCheckpoint))]
internal static class Patch_CheckpointCleared
{
    private static void Postfix() => NewGameReset.OnCheckpointCleared();
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.LoadLastCheckpoint))]
internal static class Patch_CheckpointLoad
{
    private static void Prefix() => NewGameReset.OnLoadLastCheckpoint();
}
