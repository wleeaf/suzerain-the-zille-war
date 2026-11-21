using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What the player chooses as an operation's aim (set in the war council; dev keys for now).</summary>
internal enum Objective
{
    Wall,       // break the border wall, take the Auremina gold hills
    Harbor,     // the Gulf coast to Zille city and its harbor
    Ports,      // the Sudis coast to Nanta
    Clear,      // mop up whatever Wehlen still holds
}

internal sealed record ObjectiveInfo(Objective Id, string Codename, string Target, string Aim);

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

    public const int DefaultStrength = 8;
    public const int MaxOperations = 4;

    public static readonly ObjectiveInfo[] Objectives =
    {
        new(Objective.Wall, "Operation Iron Gate", "Auremina", "Breach the Wehzek border wall and take the gold hills of Auremina."),
        new(Objective.Harbor, "Operation Golden Harbor", "Zille", "Drive along the Gulf of Moridia and take Zille and its harbor."),
        new(Objective.Ports, "Operation Northern Crown", "Nanta", "Push up the Sudis coast and take the port of Nanta."),
        new(Objective.Clear, "Operation Homecoming", null, "Clear the last Wehzek positions from the mainland."),
    };

    public static void RegisterVariables()
    {
        foreach (string v in new[] { VarOwners, VarOpsDone, VarNextObjective, VarStrength, VarLiberated, VarHistory, VarCurrentOp, VarOwnersAtStart })
        {
            Variables.Register(v);
        }

        for (int k = 1; k <= MaxOperations; k++)
        {
            Variables.Register(ResultVar(k));
            Variables.Register(EnemyDeadVar(k));
            Variables.Register(PlayerDeadVar(k));
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

    public static ObjectiveInfo Info(Objective o) => Objectives.First(x => x.Id == o);

    /// <summary>The operation that is set up / being fought, from the saved "k:Objective".</summary>
    public static (int k, Objective objective) CurrentOperation()
    {
        string[] s = (Variables.GetString(VarCurrentOp) ?? string.Empty).Split(':');
        return s.Length == 2 && int.TryParse(s[0], out int k) && Enum.TryParse(s[1], out Objective o) ? (k, o) : (0, Objective.Wall);
    }

    public static Objective NextObjective
    {
        get
        {
            string s = Variables.GetString(VarNextObjective);
            Objective o = Enum.TryParse(s, out Objective parsed) ? parsed : Objective.Wall;
            return Available().Contains(o) ? o : Available().FirstOrDefault();
        }
        set => Variables.Set(VarNextObjective, value.ToString());
    }

    /// <summary>Objectives whose target city is still Wehlen's (Clear once all three are taken).</summary>
    public static List<Objective> Available()
    {
        Dictionary<string, bool> owners = CurrentOwners();
        var list = Objectives.Where(o => o.Target != null && IsEnemy(owners, TownTile(o.Target))).Select(o => o.Id).ToList();
        if (list.Count == 0 && owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille"))
        {
            list.Add(Objective.Clear);
        }

        return list;
    }

    public static Objective CycleObjective()
    {
        List<Objective> av = Available();
        if (av.Count == 0)
        {
            return NextObjective;
        }

        int i = av.IndexOf(NextObjective);
        NextObjective = av[(i + 1) % av.Count];
        return NextObjective;
    }

    // ---- tile ownership: true = Rizia (player), false = Wehlen (enemy); land tiles only ----

    public static Dictionary<string, bool> CurrentOwners() => ParseOwners(Variables.GetString(VarOwners));

    public static Dictionary<string, bool> OwnersAtStart() => ParseOwners(Variables.GetString(VarOwnersAtStart));

    public static void MarkOperationStart() => Variables.Set(VarOwnersAtStart, Variables.GetString(VarOwners) ?? string.Empty);

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
        Variables.Set(VarHistory, string.Empty);
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
        return $"ops done={OperationsDone}, next={NextObjective} ({Info(NextObjective).Codename}), available=[{string.Join(",", Available())}], " +
               $"strength={Strength} ({Tier}), Zille tiles held {zilleHeld}/{zilleTotal}, Rizian tiles lost {riziaLost}, " +
               $"history='{Variables.GetString(VarHistory)}', liberated={Variables.GetBool(VarLiberated)}";
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
        bool won = objective == Objective.Clear
            ? !owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille")
            : !IsEnemy(owners, TownTile(info.Target));
        Variables.Set(ResultVar(k), won);
        foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille" && t.Tile != null))
        {
            Variables.Set(ZilleOperation.CapturedVar(town.Name), !IsEnemy(owners, town.Tile));
        }

        bool liberated = !owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille");
        Variables.Set(VarLiberated, liberated);
        Variables.Set(VarOpsDone, Math.Max(OperationsDone, k));
        string hist = Variables.GetString(VarHistory);
        Variables.Set(VarHistory, $"{hist}{(string.IsNullOrEmpty(hist) ? "" : ";")}{objective}:{(won ? "won" : "lost")}");
        List<Objective> next = Available();
        if (next.Count > 0)
        {
            NextObjective = next[0];
        }

        AfterAction a = AfterAction.Compute(k, OwnersAtStart(), owners);
        string report = a.FullReport(info, won);
        ZilleReports.SetDescription($"Operation_Zille_{k}_Result_Won", report);
        ZilleReports.SetDescription($"Operation_Zille_{k}_Result_Lost", report);
        NewsWire.PublishOperationResult(k, info, won, a);
        Core.Log.Msg($"Operation {k} ({info.Codename}) finished: read {read} tiles, won={won}, liberated={liberated}. {Describe()}\n{report}");
    }
}
