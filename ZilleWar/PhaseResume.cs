using HarmonyLib;
using SuzerainModdingKit;
using Il2Cpp;
using TokenSaveList = Il2CppSystem.Collections.Generic.List<Il2Cpp.WarSaveData.WarTokenSaveData>;

namespace ZilleWar;

/// <summary>
/// Phases II and III of the Zille operation resume the map from the game's snapshot: every unit where it stood, with its
/// organization. Two things need help:
/// - The Zille cities are tokens of our own unit types (cloned from vanilla's), which the game can't load back by name. They
///   are taken out of the snapshot before the game reads it and recreated here, on the same tile, with the same owner and
///   organization (the captured-city variables keep working).
/// - Between phases Wehlen brings men across the Strait of Kreleck: whatever the War Council's naval orders and the Lespian
///   patrols let through, plus the replacements its supply situation allows. They land on free Wehzek-held ground near the
///   ports, out of contact with our lines.
/// </summary>
[HarmonyPatch(typeof(WarTokenManager), nameof(WarTokenManager.SetupFromSaveData))]
internal static class PhaseResume
{
    private static readonly List<WarSaveData.WarTokenSaveData> Held = new();

    private static bool Ours => ZilleOperation.IsOurFragment(Patch_WarFlowManager_SetupWarFragment.Current);

    private static string CityTypeName(TownSpec town) => $"City_{town.Name}";

    private static void Prefix(TokenSaveList warTokenSaveData)
    {
        Held.Clear();
        if (!Ours || warTokenSaveData == null)
        {
            return;
        }

        try
        {
            var cityNames = ZilleData.Towns.Where(t => t.Role == "city").Select(CityTypeName).ToHashSet();
            int units = 0;
            for (int i = warTokenSaveData.Count - 1; i >= 0; i--)
            {
                WarSaveData.WarTokenSaveData e = warTokenSaveData[i];
                if (e != null && cityNames.Contains(e.unitTypeName))
                {
                    Held.Add(e);
                    warTokenSaveData.RemoveAt(i);
                }
                else
                {
                    units++;
                }
            }

            Core.Log.Msg($"Resuming the Zille map: {units} unit(s) from the snapshot, {Held.Count} city token(s) to rebuild.");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Reading the Zille snapshot failed: {ex}");
        }
    }

    private static void Postfix(WarTokenManager __instance, TokenSaveList warTokenSaveData)
    {
        if (!Ours)
        {
            return;
        }

        try
        {
            UnitTypes ut = UnitTypes.FromVanilla(ZilleOperation.FindWarOperation("Operation_1").warOperationData);
            RestoreCities(__instance, ut);
            Reinforce(__instance, ut);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Resuming the Zille map failed: {ex}");
        }
        finally
        {
            // Keep the save's own list as it was.
            foreach (WarSaveData.WarTokenSaveData e in Held)
            {
                warTokenSaveData?.Add(e);
            }

            Held.Clear();
        }
    }

    private static WarTile FindTile(string name)
    {
        WarTileManager tm = Managers.Instance?.WarTileManager;
        if (tm?.allTiles == null || name == null)
        {
            return null;
        }

        string data = name.StartsWith("Tile_", StringComparison.Ordinal) ? name.Substring(5) : name;
        foreach (WarTile t in tm.allTiles)
        {
            if (t?.currentWarTileData?.tileName == data || t?.name == name)
            {
                return t;
            }
        }

        return null;
    }

    private static WarToken Create(WarTokenManager tm, WarTile tile, WarUnitTypeData type, WarToken.WarAllegiance allegiance, string guid)
    {
        var data = new WarTokenData
        {
            startingTileName = tile.currentWarTileData.tileName,
            startingTile = tile,
            unitType = type,
            allegiance = allegiance,
            guid = guid ?? Guid.NewGuid().ToString(),
            isDeployedWithInstruction = false,
        };
        return tm.CreateWarTokenOperationStart(data);
    }

    private static void RestoreCities(WarTokenManager tm, UnitTypes ut)
    {
        int made = 0;
        foreach (WarSaveData.WarTokenSaveData e in Held)
        {
            TownSpec town = ZilleData.Towns.FirstOrDefault(t => CityTypeName(t) == e.unitTypeName);
            WarTile tile = FindTile(e.tileName);
            if (town == null || tile == null)
            {
                Core.Log.Warning($"  city token '{e.unitTypeName}' on '{e.tileName}': no {(town == null ? "town" : "tile")}; skipped.");
                continue;
            }

            bool zille = town.Side == "zille";
            WarUnitTypeData type = zille
                ? UnitTypes.City(ut.EnemyCityTemplate, town.Name, ZilleOperation.CapturedVar(town.Name))
                : UnitTypes.City(ut.PlayerCityTemplate ?? ut.EnemyCityTemplate, town.Name, null);
            WarToken token = Create(tm, tile, type, (WarToken.WarAllegiance)e.allegiance, e.guid);
            if (token != null)
            {
                token.remainingOrganization = e.remainingOrganization;
                made++;
            }
        }

        if (Held.Count > 0)
        {
            Core.Log.Msg($"  rebuilt {made} of {Held.Count} city token(s).");
        }
    }

    /// <summary>Wehzek units landed between phases: the strait's leak plus what Wehlen's supply situation can replace.</summary>
    private static void Reinforce(WarTokenManager tm, UnitTypes ut)
    {
        WarCouncil.Orders orders = WarCouncil.CurrentOrders();
        int refill = ZilleCampaign.Tier switch { "Strong" => 2, "Default" => 1, _ => 0 };
        int n = orders.Reinforcements + refill;
        if (n <= 0 || ut.Infantry == null)
        {
            Core.Log.Msg("  no Wehzek reinforcements this phase.");
            return;
        }

        WarTileManager tiles = Managers.Instance?.WarTileManager;
        if (tiles?.allTiles == null)
        {
            return;
        }

        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        var ports = new[] { "Nanta", "Isor", "Zille" }.Select(ZilleCampaign.TownTile)
            .Where(t => t != null && ZilleCampaign.IsEnemy(owners, t)).ToList();
        string anchor = ports.FirstOrDefault() ?? ZilleCampaign.FocusTile(ZilleCampaign.Info(ZilleCampaign.NextObjective), owners);
        var free = new List<WarTile>();
        foreach (WarTile t in tiles.allTiles)
        {
            WarTileData d = t?.currentWarTileData;
            if (d == null || d.tileType != WarTile.TileType.Land || t.currentAllegiance != WarToken.WarAllegiance.Enemy ||
                t.currentWarToken != null || !ZilleData.ByName.ContainsKey(d.tileName))
            {
                continue;
            }

            bool contact = false;
            if (t.adjacentWarTiles != null)
            {
                foreach (WarTile a in t.adjacentWarTiles)
                {
                    contact |= a != null && a.currentAllegiance == WarToken.WarAllegiance.Player;
                }
            }

            if (!contact)
            {
                free.Add(t);
            }
        }

        var chosen = free.OrderBy(t => anchor == null ? 0 : Plan.Dist(t.currentWarTileData.tileName, anchor)).Take(n).ToList();
        int landed = 0;
        for (int i = 0; i < chosen.Count; i++)
        {
            WarUnitTypeData type = i == 0 && ZilleCampaign.Tier == "Strong" && ut.Tank != null ? ut.Tank : ut.Infantry;
            if (Create(tm, chosen[i], type, WarToken.WarAllegiance.Enemy, null) != null)
            {
                landed++;
            }
        }

        Variables.Set(VarLanded, landed);
        Core.Log.Msg($"  Wehzek reinforcements: {landed} unit(s) landed (strait {orders.Reinforcements}, replacements {refill}) near tile {anchor}.");
    }

    /// <summary>Wehzek units landed before the phase being fought (for the reports).</summary>
    public const string VarLanded = "ZilleWar.State_ReinforcementsLanded";
}
