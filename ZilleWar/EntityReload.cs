using HarmonyLib;
using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The game rebuilds every entity list from its data files when a game is loaded. Anything the mod added once (HUD lines,
/// Overview cards and policies, codex entries) is gone afterwards, so the modules that add entities once are told to add
/// them again on the next story step.
/// </summary>
[HarmonyPatch(typeof(EntityDataManager), nameof(EntityDataManager.Initialize))]
internal static class EntityReload
{
    private static void Postfix() => Reset();

    internal static void Reset()
    {
        WorldModifiers.ResetAdded();
        WorldCodex.ResetAdded();
        SupplyModifiers.ResetAdded();
        WarDecrees.ResetAdded();
        Core.Log.Msg("Entity data reloaded: the mod's cards, lines and codex entries will be added again.");
    }
}

[HarmonyPatch(typeof(EntityDataManager), nameof(EntityDataManager.InitializeAllEntities))]
internal static class EntityReloadAll
{
    private static void Postfix() => EntityReload.Reset();
}
