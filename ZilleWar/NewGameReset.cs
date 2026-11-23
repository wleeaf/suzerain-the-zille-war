using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// A new game must start with none of the mod's state.
/// The modding kit keeps the running game's mod variables in moddingkit/active.json. When a new campaign begins, the game loads its
/// fresh active save and the kit reloads that file, so the game left behind (its war, its treaty, its decrees) leaks into the new
/// one: Wehlen hostile on Turn 1, the victory's public opinion, the Caleqabiz lease, the CSP embargo.
/// The fix: when a new campaign is created, every registered ZilleWar.* variable is cleared again on the first story step, before
/// any other handler of the mod runs (Core subscribes first). A cleared variable is nil, exactly as on the first game after install.
/// As a safety net, a game before Turn 4 that claims a declared war is stale data too, and is cleared the same way.
/// </summary>
internal static class NewGameReset
{
    private static bool _pending;

    internal static void MarkNewCampaign() => _pending = true;

    /// <summary>A new campaign of another story pack (Sordland) is none of the mod's business.</summary>
    internal static void Cancel() => _pending = false;

    /// <summary>Called first on every story step.</summary>
    internal static void OnStep()
    {
        int turn = SuzerainModdingKit.GameState.CurrentTurnNum ?? 0;
        bool stale = turn is > 0 and < 4 && Variables.GetBool(WarFlags.Declared);
        if (!_pending && !stale)
        {
            return;
        }

        int n = ClearAll();
        Core.Log.Msg(_pending
            ? $"New game: cleared {n} Zille War variables left by the previous game."
            : $"Turn {turn} save carried a declared war: cleared {n} stale Zille War variables.");
        _pending = false;
    }

    private static int ClearAll()
    {
        var field = typeof(Variables).GetField("RegisteredVariables", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is not System.Collections.Generic.List<string> names)
        {
            Core.Log.Warning("New game reset: the modding kit's variable list was not found.");
            return 0;
        }

        int n = 0;
        foreach (string name in names)
        {
            if (!name.StartsWith("ZilleWar.", System.StringComparison.Ordinal))
            {
                continue;
            }

            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"Variable[\"{name}\"] = nil");
            n++;
        }

        return n;
    }
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.CreateNewCampaign))]
internal static class Patch_NewCampaignReset
{
    private static void Prefix() => NewGameReset.MarkNewCampaign();
}
