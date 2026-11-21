using System.Reflection;
using HarmonyLib;
using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// Dev tracing for the war setup: logs entry into key manager methods and failed tile lookups,
/// so native exceptions (which arrive without a stack) can be located by the last log line.
/// </summary>
internal static class Trace
{
    private static readonly (Type type, string method)[] Entries =
    {
        (typeof(WarTokenManager), nameof(WarTokenManager.Setup)),
        (typeof(WarTokenManager), nameof(WarTokenManager.DeployToken)),
        (typeof(WarSupplyLineManager), nameof(WarSupplyLineManager.Setup)),
        (typeof(WarSupplyLineManager), nameof(WarSupplyLineManager.SetupSupplyLines)),
        (typeof(WarSupplyLineManager), nameof(WarSupplyLineManager.CreateSupplyLine)),
        (typeof(WarNavalBlockadeManager), nameof(WarNavalBlockadeManager.Setup)),
        (typeof(WarNavalBlockadeManager), nameof(WarNavalBlockadeManager.SetupNavalBlockades)),
        (typeof(WarNavalBlockadeManager), nameof(WarNavalBlockadeManager.CreateNavalBlockade)),
        (typeof(WarNavalBlockadeManager), nameof(WarNavalBlockadeManager.SetTilesNavalBlockedBy)),
        (typeof(WarObjectiveManager), nameof(WarObjectiveManager.Setup)),
        (typeof(WarObjectiveManager), nameof(WarObjectiveManager.StartCityObjective)),
        (typeof(WarTileManager), nameof(WarTileManager.SetupReinforcementTiles)),
        (typeof(WarSupplyLine), nameof(WarSupplyLine.SetupTiles)),
    };

    private static int _tileSetups;

    public static void Install(HarmonyLib.Harmony harmony)
    {
        var enter = new HarmonyMethod(typeof(Trace).GetMethod(nameof(Enter), BindingFlags.NonPublic | BindingFlags.Static));
        foreach ((Type type, string method) in Entries)
        {
            foreach (MethodInfo mi in type.GetMethods().Where(m => m.Name == method && m.DeclaringType == type))
            {
                try
                {
                    harmony.Patch(mi, prefix: enter);
                }
                catch (Exception ex)
                {
                    Core.Log.Warning($"trace: cannot patch {type.Name}.{method}: {ex.Message}");
                }
            }
        }

        Patch(harmony, typeof(WarTileManager), nameof(WarTileManager.GetTileByName), nameof(TileLookup), postfix: true);
        Patch(harmony, typeof(WarTileManager), nameof(WarTileManager.GetEnabledTileByName), nameof(TileLookup), postfix: true);
        Patch(harmony, typeof(WarTile), nameof(WarTile.Setup), nameof(TileSetup), postfix: false);
    }

    private static void Patch(HarmonyLib.Harmony h, Type type, string method, string hook, bool postfix)
    {
        var hm = new HarmonyMethod(typeof(Trace).GetMethod(hook, BindingFlags.NonPublic | BindingFlags.Static));
        foreach (MethodInfo mi in type.GetMethods().Where(m => m.Name == method && m.DeclaringType == type))
        {
            try
            {
                if (postfix)
                {
                    h.Patch(mi, postfix: hm);
                }
                else
                {
                    h.Patch(mi, prefix: hm);
                }
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"trace: cannot patch {type.Name}.{method}: {ex.Message}");
            }
        }
    }

    private static void Enter(MethodBase __originalMethod) =>
        Core.Log.Msg($"  > {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}");

    private static void TileLookup(MethodBase __originalMethod, string tileName, WarTile __result)
    {
        if (__result == null)
        {
            Core.Log.Warning($"  ! {__originalMethod.Name}('{tileName}') -> null");
        }
    }

    private static void TileSetup(WarTile __instance, WarTileData warTileData, WarToken.WarAllegiance allegiance)
    {
        _tileSetups++;
        if (warTileData == null || _tileSetups <= 2 || _tileSetups % 50 == 0)
        {
            Core.Log.Msg($"  > WarTile.Setup #{_tileSetups} '{__instance?.name}' data='{warTileData?.tileName}' allegiance={allegiance}");
        }
    }
}
