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
    public const string VarCurrentOp = "ZilleWar.State_CurrentOperation"; // "k:Objective:phase" while a phase is set up
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
        new(Objective.Harbor, "the battle for Zille", "Zille", "Drive down the river to the Gulf of Moridia and take the city of Zille and its harbor.", new[] { "Zille" }),
        new(Objective.Wall, "the battle for Auremina", "Auremina", "Break into the hills and take Auremina and its gold mines.", new[] { "Auremina" }),
        new(Objective.Ports, "the battle for the ports", "Isor and Nanta", "Take the ports of Isor and Nanta and cut Wehlen's last lifelines across the strait.", new[] { "Isor", "Nanta" }),
    };

    // ---- one operation in three phases, like vanilla's Golden Shield: Phase I on Turn 4, II on Turn 5, III on Turn 6.
    public const string OperationCodename = "Operation Northern Crown";
    public const string OperationTitle = "Operation: Northern Crown";
    public const int LastPhase = 3;
    public const string VarPhase = "ZilleWar.State_Phase";                  // the phase being fought / last fought (0 before the first)
    public static string PhaseOfVar(int k) => $"ZilleWar.State_Phase{k}";  // which phase the k-th offensive was

    /// <summary>The phase a War Council on this turn sends the army into: I on Turn 4, II on Turn 5, III on Turn 6 (and after).</summary>
    public static int PhaseForTurn(int turn) => Math.Clamp(turn - 3, 1, LastPhase);

    public static int Phase => Variables.GetInt(VarPhase);

    public static string Roman(int p) => p switch { 1 => "I", 2 => "II", 3 => "III", _ => p.ToString() };

    /// <summary>"Operation: Northern Crown - Phase II", as vanilla titles its phases.</summary>
    public static string PhaseTitle(int p) => $"{OperationTitle} - Phase {Roman(p)}";

    /// <summary>"Phase II of Operation Northern Crown", for prose.</summary>
    public static string PhaseName(int p) => $"Phase {Roman(p)} of {OperationCodename}";

    /// <summary>"the opening phase", "the second phase", "the final phase", for prose.</summary>
    public static string PhaseWords(int p) => p switch { 1 => "the opening phase", 2 => "the second phase", _ => "the final phase" };

    /// <summary>The campaign is fought in this order. A failed operation loses the war.</summary>
    public static readonly Objective[] Sequence = { Objective.Harbor, Objective.Wall, Objective.Ports };

    public static void RegisterVariables()
    {
        foreach (string v in new[]
                 {
                     VarOwners, VarOpsDone, VarNextObjective, VarStrength, VarLiberated, VarHistory, VarCurrentOp, VarOwnersAtStart, VarDefeated,
                     AfterAction.VarNotesTold, VarPhase, VarPhaseDone, PhaseResume.VarLanded, PhaseResume.VarLandedAt, VarEnemyDeadTotal, VarPlayerDeadTotal, VarEnemyDeadAtStart, VarPlayerDeadAtStart,
                 })
        {
            Variables.Register(v);
        }

        Variables.Register(ResultVar(0));
        for (int k = 1; k <= MaxOperations; k++)
        {
            Variables.Register(ResultVar(k));
            Variables.Register(EnemyDeadVar(k));
            Variables.Register(PlayerDeadVar(k));
            Variables.Register(ReportTextVar(k));
            Variables.Register(PhaseOfVar(k));
            foreach (string v in new[] { PartialVar(k), GainedVar(k), LostVar(k), AimVar(k), GainsVar(k), LossesVar(k) })
            {
                Variables.Register(v);
            }

            Vanilla.Expose(PartialVar(k));
            Vanilla.Expose(GainsVar(k), isBool: false);
            Vanilla.Expose(LossesVar(k), isBool: false);
        }

        Vanilla.Expose(VarPhase, isBool: false);
    }

    /// <summary>The k-th offensive (phase fought) reached its aim; k = 0 is the whole operation (Zille liberated).</summary>
    public static string ResultVar(int k) => k == 0 ? "ZilleWar.War_Zille_Operation_Won" : $"ZilleWar.War_Zille_Operation{k}_Won";
    public static string EnemyDeadVar(int k) => $"ZilleWar.War_Zille_Operation{k}_TotalEnemy_Dead";
    public static string PlayerDeadVar(int k) => $"ZilleWar.War_Zille_Operation{k}_TotalPlayer_Dead";

    // ---- what each phase did, city by city (set in OnPhaseEnded; read by the reports, the press, the councils and the calls)
    /// <summary>Bool: the k-th phase took part of a two-city aim but not all of it (e.g. Nanta, not Isor).</summary>
    public static string PartialVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Partial";
    /// <summary>String: the cities the k-th phase won, comma-separated (Zille cities taken, and Rizian towns won back).</summary>
    public static string GainedVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Gained";
    /// <summary>String: the cities the k-th phase lost to Wehzek counterattacks, comma-separated (Zille cities and Rizian towns).</summary>
    public static string LostVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Lost";
    /// <summary>String: the aim as it stood when the k-th phase began ("Isor and Nanta", or "Isor" once Nanta is held).</summary>
    public static string AimVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Aim";
    /// <summary>Int: how many cities the k-th phase gained, and lost (for Lua conditions).</summary>
    public static string GainsVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Gains";
    public static string LossesVar(int k) => $"ZilleWar.War_Zille_Operation{k}_Losses";

    /// <summary>"Isor", "Isor and Nanta", "Zille, Auremina and Nanta".</summary>
    public static string JoinNames(IList<string> names) =>
        names.Count <= 1 ? names.FirstOrDefault() ?? string.Empty : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";

    public static List<string> Gained(int k) => SplitList(Str(GainedVar(k)));
    public static List<string> Lost(int k) => SplitList(Str(LostVar(k)));
    public static bool Partial(int k) => Variables.GetBool(PartialVar(k));

    private static List<string> SplitList(string s) =>
        s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>The towns of an aim that Wehlen still holds ("Isor" once Nanta is ours), or the full target when none is left.</summary>
    public static List<string> TownsLeft(ObjectiveInfo info, Dictionary<string, bool> owners) =>
        info.Towns.Where(t => IsEnemy(owners, TownTile(t))).ToList();

    /// <summary>The aim's display name as it stands now: "Isor and Nanta", "Isor", "Zille".</summary>
    public static string AimName(ObjectiveInfo info, Dictionary<string, bool> owners)
    {
        List<string> left = TownsLeft(info, owners);
        return left.Count == 0 || left.Count == info.Towns.Length ? info.Target : string.Join(" and ", left);
    }

    /// <summary>The aim's display name now, from the saved map.</summary>
    public static string AimName(ObjectiveInfo info) => AimName(info, CurrentOwners());

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

    /// <summary>The title of the phase being fought (or last fought): "Operation: Northern Crown - Phase II".</summary>
    public static string Title(ObjectiveInfo info) => PhaseTitle(Math.Max(1, CurrentOperation().phase));

    public static ObjectiveInfo Info(Objective o) => Objectives.First(x => x.Id == o);

    /// <summary>The offensive that is set up / being fought, from the saved "k:Objective:phase" (k counts the phases fought).</summary>
    public static (int k, Objective objective, int phase) CurrentOperation()
    {
        string[] s = Str(VarCurrentOp).Split(':');
        if (s.Length >= 2 && int.TryParse(s[0], out int k) && Enum.TryParse(s[1], out Objective o))
        {
            int p = s.Length >= 3 && int.TryParse(s[2], out int pp) ? pp : Math.Min(k, LastPhase);
            return (k, o, p);
        }

        return (0, Objective.Harbor, 0);
    }

    public static Objective NextObjective
    {
        get => Available().FirstOrDefault();
        set => Variables.Set(VarNextObjective, value.ToString());
    }

    public static bool Defeated => Variables.GetBool(VarDefeated);

    /// <summary>
    /// Safety net: the war cannot outlive Turn 6. If the last phase was never booked (a reload between phases, the table left
    /// without ending the phase), end it as the last phase would have: liberated if every city is held, otherwise an armistice
    /// on the line as it stands.
    /// </summary>
    public static void ForceEnd(string why)
    {
        bool liberated = ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille")
            .All(t => Variables.GetBool(ZilleOperation.CapturedVar(t.Name)));
        Variables.Set(VarLiberated, liberated);
        Variables.Set(VarDefeated, !liberated);
        Variables.Set(ResultVar(0), liberated);
        Core.Log.Warning($"The Zille war was still open {why}; ended it ({(liberated ? "liberated" : "armistice on the line")}).");
    }
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

    /// <summary>A treaty moves a town across the line: its tile changes hands (and, for a Wehzek city, its captured flag).</summary>
    public static void CedeTown(string town, bool toRizia)
    {
        string tile = TownTile(town);
        if (tile == null)
        {
            return;
        }

        Dictionary<string, bool> owners = CurrentOwners();
        owners[tile] = toRizia;
        SaveOwners(owners);
        if (ZilleData.Towns.Any(t => t.Name == town && t.Side == "zille" && t.Role == "city"))
        {
            Variables.Set(ZilleOperation.CapturedVar(town), toRizia);
        }

        Core.Log.Msg($"Treaty: {town} passes to {(toRizia ? "Rizia" : "Wehlen")}.");
    }

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
        Variables.Set(VarPhase, 0);
        Variables.Set(VarPhaseDone, 0);
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
        return $"phases fought={OperationsDone}, phase={Phase}, next={(Available().Count > 0 ? AimName(Info(NextObjective)) : "none")}, defeated={Defeated}, " +
               $"strength={Strength} ({Tier}), Zille tiles held {zilleHeld}/{zilleTotal}, Rizian tiles lost {riziaLost}, " +
               $"history='{Str(VarHistory)}', liberated={Variables.GetBool(VarLiberated)}";
    }

    // ---- casualties: the engine counts the whole operation's dead; each phase's share is the difference since it began
    public const string VarEnemyDeadTotal = "ZilleWar.War_Zille_TotalEnemy_Dead";
    public const string VarPlayerDeadTotal = "ZilleWar.War_Zille_TotalPlayer_Dead";
    private const string VarEnemyDeadAtStart = "ZilleWar.State_EnemyDeadAtPhaseStart";
    private const string VarPlayerDeadAtStart = "ZilleWar.State_PlayerDeadAtPhaseStart";
    public const string VarPhaseDone = "ZilleWar.State_PhaseDone";          // the last phase whose end was booked

    /// <summary>A phase is being set up: remember the dead counts so far, so the phase's own losses can be told apart.</summary>
    public static void MarkPhaseStart()
    {
        Variables.Set(VarEnemyDeadAtStart, Variables.GetInt(VarEnemyDeadTotal));
        Variables.Set(VarPlayerDeadAtStart, Variables.GetInt(VarPlayerDeadTotal));
        // Only a resumed phase lands Wehzek reinforcements (PhaseResume); never report an earlier phase's landing again.
        Variables.Set(PhaseResume.VarLanded, 0);
        Variables.Set(PhaseResume.VarLandedAt, string.Empty);
    }

    private static int PhaseDead(string total, string atStart)
    {
        int t = Variables.GetInt(total), s = Variables.GetInt(atStart);
        return t >= s ? t - s : t;     // the engine may reset the total when the operation starts
    }

    /// <summary>True when the operation must end now whatever the phase: every objective taken, or the army gone.</summary>
    public static bool EndsEarly(Dictionary<string, bool> owners) =>
        Sequence.All(o => AllTaken(Info(o), owners)) || !owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille") ||
        Variables.GetInt("RiziaDLC.Army_Unit_GrandTotal_Deployed") + Variables.GetInt("RiziaDLC.Army_Unit_GrandTotal_Total") <= 0;

    /// <summary>
    /// Called when a phase of the operation ends (the player ends the phase, or the game finishes the operation): snapshot the
    /// map, book the phase's result and losses, publish the coverage. The war ends with the last phase, or earlier when every
    /// objective is taken; a phase that misses its aim is a setback, and the next phase goes for the same aim. Safe to call
    /// twice for the same phase (the engine's finish and end-of-phase hooks both call it on the last phase).
    /// </summary>
    public static void OnPhaseEnded()
    {
        (int k, Objective objective, int phase) = CurrentOperation();
        if (k <= 0 || Variables.GetInt(VarPhaseDone) >= k)
        {
            return;
        }

        Variables.Set(VarPhaseDone, k);
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
        Dictionary<string, bool> before = OwnersAtStart();
        Variables.Set(AimVar(k), AimName(info, before));
        bool won = AllTaken(info, owners);
        Variables.Set(ResultVar(k), won);
        Variables.Set(PhaseOfVar(k), phase);
        Variables.Set(EnemyDeadVar(k), PhaseDead(VarEnemyDeadTotal, VarEnemyDeadAtStart));
        Variables.Set(PlayerDeadVar(k), PhaseDead(VarPlayerDeadTotal, VarPlayerDeadAtStart));
        foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille" && t.Tile != null))
        {
            Variables.Set(ZilleOperation.CapturedVar(town.Name), !IsEnemy(owners, town.Tile));
        }

        // The war ends here when Zille is cleared or every objective is held (the remaining garrisons surrender), or when the
        // last phase is over: whatever is not taken by then stays Wehzek behind an armistice line.
        bool cleared = !owners.Any(kv => !kv.Value && ZilleData.ByName[kv.Key].Region == "zille");
        bool allObjectives = Sequence.All(o => AllTaken(Info(o), owners));
        bool final = phase >= LastPhase || EndsEarly(owners);
        bool liberated = cleared || allObjectives;
        if (liberated)
        {
            // the Wehzek garrisons surrender with the rest: those still in Zille, and those still on Rizian soil (Mave Nortus,
            // Dapalle, Jales), so no Rizian town stays occupied after a liberation
            List<string> surrender = owners.Keys
                .Where(t => !owners[t] && (ZilleData.ByName[t].Region == "rizia" || (!cleared && ZilleData.ByName[t].Region == "zille"))).ToList();
            foreach (string t in surrender)
            {
                owners[t] = true;
            }

            if (surrender.Count > 0)
            {
                SaveOwners(owners);
                Core.Log.Msg($"Liberation: the Wehzek garrisons surrender ({string.Join(", ", surrender)}).");
            }

            if (!cleared)
            {
                foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille"))
                {
                    Variables.Set(ZilleOperation.CapturedVar(town.Name), true);
                }
            }
        }

        if (final)
        {
            Variables.Set(VarLiberated, liberated);
            Variables.Set(VarDefeated, !liberated);
            Variables.Set(ResultVar(0), liberated);
        }

        Variables.Set(VarOpsDone, Math.Max(OperationsDone, k));
        string hist = Str(VarHistory);
        Variables.Set(VarHistory, $"{hist}{(string.IsNullOrEmpty(hist) ? "" : ";")}{objective}:{(won ? "won" : "lost")}");
        AfterAction a = AfterAction.Compute(k, before, owners);
        var gained = a.CitiesTaken.Concat(a.CitiesRetaken).ToList();
        Variables.Set(GainedVar(k), string.Join(",", gained));
        Variables.Set(LostVar(k), string.Join(",", a.CitiesLost));
        Variables.Set(GainsVar(k), gained.Count);
        Variables.Set(LossesVar(k), a.CitiesLost.Count);
        Variables.Set(PartialVar(k), !won && info.Towns.Any(t => IsEnemy(before, TownTile(t)) && !IsEnemy(owners, TownTile(t))));
        VictoryParade.OnPhaseResult(won, gained.Count, a.CitiesLost.Count);   // booked once per phase (VarPhaseDone above)
        string report = a.FullReport(info, won);
        Variables.Set(ReportTextVar(k), report);
        if (final)
        {
            ZilleReports.SetDescription(ZilleReports.WonName, report);
            ZilleReports.SetDescription(ZilleReports.LostName, report);
        }

        ZilleReports.SetDescription($"Operation_Zille_Phase{phase}_Report", report);

        NewsWire.Batch(() => NewsWire.PublishOperationResult(k, info, won, a));
        NewsWire.OperationReports(k, info, won, a);
        Core.Log.Msg($"Phase {phase} (offensive {k}, aim {Str(AimVar(k))}) ended: read {read} tiles, aim reached={won}, final={final}, " +
                     $"liberated={liberated && final}. {Describe()}\n{report}");
    }
}
