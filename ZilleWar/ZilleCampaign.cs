using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>An operation's aim. The War Council announces them in a fixed order (<see cref="ZilleCampaign.Sequence"/>).</summary>
internal enum Objective
{
    Wall,       // break the border wall, take the Auremina gold hills
    Harbor,     // the Gulf coast to Zille city and its harbor
    Ports,      // the Sudis coast to Nanta
    Clear,      // mop up whatever Wehlen still holds
}

/// <summary>An operation's aim. <see cref="Towns"/> must all be taken; <see cref="Target"/> is their display name.</summary>
internal sealed record ObjectiveInfo(Objective Id, string Codename, string Target, string Aim, string[] Towns);

/// <summary>
/// Campaign state for the Zille war, persisted through Suzerain Modding Kit variables so it lives in the
/// save file: tile ownership carried between operations, operations fought, next objective and Wehlen's
/// hidden strength (the supply-war meter).
/// </summary>
internal static class ZilleCampaign
{
    public const string VarOwners = "ZilleWar.State_Owners";            // "P:1,2,3|E:40,41" land tiles off their starting side
    public const string VarOpsDone = "ZilleWar.State_OperationsDone";
    public const string VarNextObjective = "ZilleWar.State_NextObjective";
    public const string VarStrength = "ZilleWar.War_Zille_Strength";    // 3..10, starts at 8
    public const string VarLiberated = "ZilleWar.War_Zille_Liberated";
    public const string VarHistory = "ZilleWar.State_History";          // "Wall:won;Harbor:lost"
    public const string VarCurrentOp = "ZilleWar.State_CurrentOperation"; // "k:Objective" while an operation is set up
    public const string VarOwnersAtStart = "ZilleWar.State_OwnersAtStart";
    public const string VarDefeated = "ZilleWar.War_Zille_Defeated";      // an operation failed: the war is lost

    /// <summary>The after-action report of operation k, kept so the report can be rebuilt after a reload.</summary>
    public static string ReportTextVar(int k) => $"ZilleWar.State_Report{k}";

    /// <summary>A string variable, empty when unset (the Lua bridge returns "nil" for unset variables).</summary>
    public static string Str(string v)
    {
        string s = Variables.GetString(v);
        return string.IsNullOrEmpty(s) || s == "nil" ? string.Empty : s;
    }

    public const int DefaultStrength = 8;
    public const int MaxOperations = 4;

    public static readonly ObjectiveInfo[] Objectives =
    {
        new(Objective.Harbor, "Operation Golden Harbor", "Zille", "Drive down the river to the Gulf of Moridia and take the city of Zille and its harbor.", new[] { "Zille" }),
        new(Objective.Wall, "Operation Lodestone", "Auremina", "Break into the hills and take Auremina and its gold mines.", new[] { "Auremina" }),
        new(Objective.Ports, "Operation Northern Crown", "Isor and Nanta", "Take the ports of Isor and Nanta and cut Wehlen's last lifelines across the strait.", new[] { "Isor", "Nanta" }),
    };

    /// <summary>The campaign is fought in this order. A failed operation loses the war.</summary>
    public static readonly Objective[] Sequence = { Objective.Harbor, Objective.Wall, Objective.Ports };

    public static void RegisterVariables()
    {
        foreach (string v in new[]
                 {
                     VarOwners, VarOpsDone, VarNextObjective, VarStrength, VarLiberated, VarHistory, VarCurrentOp, VarOwnersAtStart, VarDefeated,
                     AfterAction.VarNotesTold,
                 })
        {
            Variables.Register(v);
        }

        for (int k = 1; k <= MaxOperations; k++)
        {
            Variables.Register(ResultVar(k));
            Variables.Register(EnemyDeadVar(k));
            Variables.Register(PlayerDeadVar(k));
            Variables.Register(ReportTextVar(k));
        }
    }

    public static string ResultVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Won";
    public static string EnemyDeadVar(int k) => $"ZilleWar.War_Zille_Operation{k}_TotalEnemy_Dead";
    public static string PlayerDeadVar(int k) => $"ZilleWar.War_Zille_Operation{k}_TotalPlayer_Dead";

    public static int OperationsDone => Variables.GetInt(VarOpsDone);

    public static int Strength
    {
        get
        {
            int s = Variables.GetInt(VarStrength);
            return s <= 0 ? DefaultStrength : Math.Clamp(s, 3, 10);
        }
        set => Variables.Set(VarStrength, Math.Clamp(value, 3, 10));
    }

    /// <summary>Garrison tier from the strength meter.</summary>
    public static string Tier => Strength switch
    {
        >= 8 => "Strong",
        >= 6 => "Default",
        >= 4 => "Strained",
        _ => "Starved",
    };

    /// <summary>
    /// Wehlen's warships in Zille waters: fewer as the supply war bites and as the strait is contested; one more while
    /// part of our fleet still sits on the Aureus blockade, one fewer once the War Council has brought that squadron north.
    /// </summary>
    public static int EnemyShips
    {
        get
        {
            WarCouncil.Orders orders = WarCouncil.CurrentOrders();
            return Math.Max(0, Tier switch { "Strong" => 3, "Default" => 3, "Strained" => 2, _ => 1 }
                               - Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.RoyalNavy))
                               - Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia))
                               + (orders.FleetSplit ? 1 : 0)
                               - (orders.FleetRecalled ? 1 : 0));
        }
    }

    /// <summary>"Operation Golden Harbor" as vanilla writes it in titles: "Operation: Golden Harbor".</summary>
    public static string Title(ObjectiveInfo info) =>
        info.Codename.StartsWith("Operation ", StringComparison.Ordinal) ? "Operation: " + info.Codename.Substring(10) : info.Codename;

    public static ObjectiveInfo Info(Objective o) => Objectives.First(x => x.Id == o);

    /// <summary>The operation that is set up / being fought, from the saved "k:Objective".</summary>
    public static (int k, Objective objective) CurrentOperation()
    {
        string[] s = Str(VarCurrentOp).Split(':');
        return s.Length == 2 && int.TryParse(s[0], out int k) && Enum.TryParse(s[1], out Objective o) ? (k, o) : (0, Objective.Wall);
    }

    public static Objective NextObjective
    {
        get => Available().FirstOrDefault();
        set => Variables.Set(VarNextObjective, value.ToString());
    }

    public static bool Defeated => Variables.GetBool(VarDefeated);
    public static bool Liberated => Variables.GetBool(VarLiberated);

    /// <summary>The next operation in the fixed order whose towns Wehlen still holds (empty when the war is over).</summary>
    public static List<Objective> Available()
    {
        var list = new List<Objective>();
        if (Defeated || Liberated)
        {
            return list;
        }

        Dictionary<string, bool> owners = CurrentOwners();
        if (!owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille"))
        {
            return list;
        }

        Objective? next = Sequence.Cast<Objective?>().FirstOrDefault(o => Info(o.Value).Towns.Any(t => IsEnemy(owners, TownTile(t))));
        if (next != null)
        {
            list.Add(next.Value);
        }

        return list;
    }

    public static bool AllTaken(ObjectiveInfo info, Dictionary<string, bool> owners) =>
        info.Towns.All(t => !IsEnemy(owners, TownTile(t)));

    /// <summary>The first objective town Wehlen still holds (the operation's focus).</summary>
    public static string FocusTile(ObjectiveInfo info, Dictionary<string, bool> owners) =>
        info.Towns.Select(TownTile).FirstOrDefault(t => IsEnemy(owners, t)) ?? TownTile(info.Towns[0]);

    public static Objective CycleObjective() => NextObjective;

    // ---- tile ownership: true = Rizia (player), false = Wehlen (enemy); land tiles only ----

    public static Dictionary<string, bool> CurrentOwners() => ParseOwners(Str(VarOwners));

    public static Dictionary<string, bool> OwnersAtStart() => ParseOwners(Str(VarOwnersAtStart));

    public static void MarkOperationStart() => Variables.Set(VarOwnersAtStart, Str(VarOwners));

    private static Dictionary<string, bool> ParseOwners(string s)
    {
        var owners = ZilleData.Tiles.Where(t => t.Type == "Land").ToDictionary(t => t.Name, t => t.Region == "rizia");
        s ??= string.Empty;
        foreach (string part in s.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2)
            {
                continue;
            }

            bool player = kv[0] == "P";
            foreach (string n in kv[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (owners.ContainsKey(n))
                {
                    owners[n] = player;
                }
            }
        }

        return owners;
    }

    private static void SaveOwners(Dictionary<string, bool> owners)
    {
        var p = owners.Where(kv => kv.Value && ZilleData.ByName[kv.Key].Region != "rizia").Select(kv => kv.Key);
        var e = owners.Where(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "rizia").Select(kv => kv.Key);
        Variables.Set(VarOwners, $"P:{string.Join(",", p)}|E:{string.Join(",", e)}");
    }

    public static bool IsEnemy(Dictionary<string, bool> owners, string tile) =>
        tile != null && owners.TryGetValue(tile, out bool p) && !p;

    public static string TownTile(string town) => ZilleData.Towns.FirstOrDefault(t => t.Name == town)?.Tile;

    public static void Reset()
    {
        Variables.Set(VarOwners, string.Empty);
        Variables.Set(VarOpsDone, 0);
        Variables.Set(VarNextObjective, Objective.Wall.ToString());
        Variables.Set(VarStrength, DefaultStrength);
        Variables.Set(VarLiberated, false);
        Variables.Set(VarDefeated, false);
        Variables.Set(VarHistory, string.Empty);
        Variables.Set(AfterAction.VarNotesTold, string.Empty);
        foreach (TownSpec t in ZilleData.Towns.Where(t => t.Role == "city"))
        {
            Variables.Set(ZilleOperation.CapturedVar(t.Name), false);
        }

        Core.Log.Msg("Zille campaign state reset.");
    }

    public static string Describe()
    {
        Dictionary<string, bool> o = CurrentOwners();
        int zilleHeld = o.Count(kv => kv.Value && ZilleData.ByName[kv.Key].Region == "zille");
        int zilleTotal = ZilleData.Tiles.Count(t => t.Region == "zille" && t.Type == "Land");
        int riziaLost = o.Count(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "rizia");
        return $"ops done={OperationsDone}, next={(Available().Count > 0 ? Info(NextObjective).Codename : "none")}, defeated={Defeated}, " +
               $"strength={Strength} ({Tier}), Zille tiles held {zilleHeld}/{zilleTotal}, Rizian tiles lost {riziaLost}, " +
               $"history='{Str(VarHistory)}', liberated={Variables.GetBool(VarLiberated)}";
    }

    /// <summary>Called right before the game finishes one of our operations: snapshot the map, decide the result.</summary>
    public static void OnOperationFinished(int k, Objective objective)
    {
        Dictionary<string, bool> owners = CurrentOwners();
        WarTileManager tm = Managers.Instance?.WarTileManager;
        int read = 0;
        if (tm?.allTiles != null)
        {
            foreach (WarTile t in tm.allTiles)
            {
                string n = t?.currentWarTileData?.tileName;
                if (n == null || !owners.ContainsKey(n))
                {
                    continue;
                }

                if (t.currentAllegiance == WarToken.WarAllegiance.Player)
                {
                    owners[n] = true;
                }
                else if (t.currentAllegiance == WarToken.WarAllegiance.Enemy)
                {
                    owners[n] = false;
                }

                read++;
            }
        }

        SaveOwners(owners);
        ObjectiveInfo info = Info(objective);
        bool won = AllTaken(info, owners);
        Variables.Set(ResultVar(k), won);
        foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille" && t.Tile != null))
        {
            Variables.Set(ZilleOperation.CapturedVar(town.Name), !IsEnemy(owners, town.Tile));
        }

        // The war ends here if Zille is cleared, if the last operation succeeds or every objective of the campaign
        // is already held (the remaining garrisons surrender), or if an operation fails. Without the last check an
        // offensive that overran later objectives would leave nothing to order and the war could never end.
        bool cleared = !owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille");
        bool allObjectives = Sequence.All(o => AllTaken(Info(o), owners));
        bool liberated = won && (cleared || objective == Sequence[^1] || allObjectives);
        if (liberated && !cleared)
        {
            foreach (string t in owners.Keys.Where(t => ZilleData.ByName[t].Region == "zille").ToList())
            {
                owners[t] = true;
            }

            SaveOwners(owners);
        }

        Variables.Set(VarLiberated, liberated);
        Variables.Set(VarDefeated, !won);
        Variables.Set(VarOpsDone, Math.Max(OperationsDone, k));
        string hist = Str(VarHistory);
        Variables.Set(VarHistory, $"{hist}{(string.IsNullOrEmpty(hist) ? "" : ";")}{objective}:{(won ? "won" : "lost")}");
        AfterAction a = AfterAction.Compute(k, OwnersAtStart(), owners);
        string report = a.FullReport(info, won);
        Variables.Set(ReportTextVar(k), report);
        ZilleReports.SetDescription($"Operation_Zille_{k}_Result_Won", report);
        ZilleReports.SetDescription($"Operation_Zille_{k}_Result_Lost", report);
        NewsWire.Batch(() => NewsWire.PublishOperationResult(k, info, won, a));
        NewsWire.OperationReports(k, info, won, a);
        Core.Log.Msg($"Operation {k} ({info.Codename}) finished: read {read} tiles, won={won}, liberated={liberated}. {Describe()}\n{report}");
    }
}
