using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// The world around the war: the HUD's per-turn lines and the overview situations (<see cref="WorldModifiers"/>),
/// the codex (<see cref="WorldCodex"/>), vanilla news and reports that the war contradicts (<see cref="WorldPress"/>),
/// and the papers, letters and dispatches that follow the war from turn to turn (<see cref="WorldVoices"/>).
/// </summary>
internal static class WorldSystems
{
    /// <summary>Keys of one-off items already published, as "|key|key|".</summary>
    public const string VarDone = "ZilleWar.World_Done";
    /// <summary>Turn in which the war ended (0 while it lasts or before it starts).</summary>
    public const string VarEndTurn = "ZilleWar.World_EndTurn";
    /// <summary>Turn in which the Pales war and the Zille war were first fought together (0 if never).</summary>
    public const string VarTwoWarsTurn = "ZilleWar.World_TwoWarsTurn";

    private static bool _registered;

    public static void Register()
    {
        foreach (string v in new[] { VarDone, VarEndTurn, VarTwoWarsTurn })
        {
            Variables.Register(v);
        }

        Run("news archive", NewsWire.Register);
        Run("modifiers", WorldModifiers.Register);
        Run("codex", WorldCodex.Register);
        Run("press", WorldPress.Register);
        Run("voices", WorldVoices.Register);
        Run("embargo", WorldEmbargo.Register);

        if (!_registered)
        {
            _registered = true;
            // Shared codex entries (Wehlen, Smolak, Beatrice) are also read in Sordland games: put their vanilla text back there.
            Events.OnEvaluateStep += (_, _) =>
            {
                if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
                {
                    Run("restore", WorldText.RestoreAll);
                }
            };
        }
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        Run("phase backfill", Front.Backfill);
        if (Declared && WarEnded && Variables.GetInt(VarEndTurn) == 0)
        {
            Variables.Set(VarEndTurn, Turn);
        }

        if (TwoWars && Variables.GetInt(VarTwoWarsTurn) == 0)
        {
            Variables.Set(VarTwoWarsTurn, Turn);
        }

        Run("modifiers step", WorldModifiers.OnStep);
        Run("codex step", WorldCodex.OnStep);
        Run("press step", WorldPress.OnStep);
        Run("voices step", WorldVoices.OnStep);
        Run("embargo step", WorldEmbargo.OnStep);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WorldSystems {what} failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ shared state, read the same way by every World* file

    public static int Turn => GameState.CurrentTurnNum ?? 0;
    public static int Step => GameState.CurrentStepNum ?? 0;

    public static bool Declared => Variables.GetBool(AnDeclaration.VarDeclared);
    /// <summary>Being fought now (computed, so it is right even before WarState stores it for this step).</summary>
    public static bool Active => WarFlow.AtWar;
    public static bool WarEnded => Declared && !WarFlow.AtWar;
    public static bool Liberated => ZilleCampaign.Liberated;
    public static bool Defeated => ZilleCampaign.Defeated;
    public static bool Ceasefire => Declared && !Liberated && !Defeated && Variables.GetBool(WarFlow.VarCeasefire);
    public static bool PalesWar => Variables.GetBool("RiziaDLC.Pales_War");
    public static bool TwoWars => Active && PalesWar;
    public static bool ZilleCityHeld => Declared && Variables.GetBool(ZilleOperation.CapturedVar("Zille"));

    public static int DeclaredTurn
    {
        get
        {
            int t = Variables.GetInt(AnDeclaration.VarDeclaredTurn);
            return t > 0 ? t : 4;
        }
    }

    /// <summary>1 in the turn of the declaration, 2 in the next, and so on.</summary>
    public static int WarTurn => Declared ? Turn - DeclaredTurn + 1 : 0;

    /// <summary>Turn the war ended in, or 0.</summary>
    public static int EndTurn => Variables.GetInt(VarEndTurn);

    /// <summary>Turns since the war ended (0 in the turn it ended), or -1 while it lasts.</summary>
    public static int TurnsSinceEnd => EndTurn > 0 ? Turn - EndTurn : -1;

    // SupplyWar state, read by name so this module does not depend on SupplyWar's internals.
    private const string LespiaStraitVar = "ZilleWar.Lespia_StraitClosed";
    private const string LespiaWithdrawnVar = "ZilleWar.Lespia_Withdrawn";

    /// <summary>Lespia closed the Strait of Kreleck for us at some point in the war.</summary>
    public static bool LespiaHelps => Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 || Variables.GetBool(LespiaStraitVar);

    /// <summary>The strait is closed to Wehzek shipping right now (Lespian patrols, or another module says so).</summary>
    public static bool StraitClosedNow =>
        Variables.GetBool(LespiaStraitVar) || Variables.GetBool(WorldModifiers.StraitClosed) ||
        (Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 && !Variables.GetBool(LespiaWithdrawnVar));

    public static bool LespiaCaleqabiz => Variables.GetBool("ZilleWar.Lespia_Caleqabiz");

    /// <summary>What Rizia gave Lespia for the strait, as the end of a sentence ("..., in exchange for ...").</summary>
    public static string LespiaPrice =>
        Variables.GetBool(SupplyWar.VarLespiaLease) ? "in exchange for a fifty-year lease of the Rizian island of Caleqabiz"
        : LespiaCaleqabiz ? "in exchange for naval rights at the Rizian island of Caleqabiz"
        : Variables.GetBool("ZilleWar.Lespia_GasDeal") && Variables.GetBool("RiziaDLC.Policy_Economy_AureusField_Extractions") ? "in exchange for Aureus gas sold to LesPower at a discount"
        : Variables.GetBool("ZilleWar.Lespia_PaidBudget") ? "for a fee of several billion guilders paid by the Rizian treasury"
        : Variables.GetString("ZilleWar.Ramiro_ConfrontOutcome") == "free" ? "free of charge, by the Lespian government's own account"
        : "at a price neither government has made public";

    /// <summary>Rizia has made Lespia's part in the Friendship Day bombing public.</summary>
    public static bool LespiaExposed =>
        Variables.GetBool("RiziaDLC.Zille_Publicized_LespianInvolvement") || Variables.GetString("ZilleWar.Ramiro_Choice") == "publish";
    public static int RumburgFavors =>
        Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Embargo)) + Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffRaids)) +
        Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffFighters)) + Variables.GetInt("ZilleWar.Rumburg_Credits") +
        (Variables.GetInt(WorldModifiers.RumburgCredit) != 0 ? 1 : 0);

    /// <summary>The token reports about Zille go on: the region while it is foreign, the city once it is Rizian again.</summary>
    public static string ZilleToken => Variables.GetBool("RiziaDLC.ZilleReturn_Success") ? "Rizia_City_Zille" : ZilleOperation.TokenName;

    /// <summary>True the first time it is called for <paramref name="key"/> in this playthrough (stored in the save).</summary>
    public static bool Once(string key)
    {
        string done = DoneList;
        string marked = $"|{key}|";
        if (done.Contains(marked, StringComparison.Ordinal))
        {
            return false;
        }

        Variables.Set(VarDone, (done.Length == 0 ? "|" : done) + key + "|");
        return true;
    }

    public static bool Seen(string key) => DoneList.Contains($"|{key}|", StringComparison.Ordinal);

    private static string DoneList
    {
        get
        {
            string s = Variables.GetString(VarDone);
            return string.IsNullOrEmpty(s) || s == "nil" ? string.Empty : s;
        }
    }
}

/// <summary>
/// What each phase of Operation Northern Crown really did, city by city, for every text that reads the front: the calls,
/// the councils, the family, the codex, the summary. Built on ZilleCampaign's per-phase variables (aim, cities gained and
/// lost, partial); <see cref="Backfill"/> fills them in for phases booked by an older build. C# readers use <see cref="Of"/>;
/// Lua conditions use <see cref="Last"/> and the per-phase helpers, which read the variables OnPhaseEnded sets at once, so a
/// scene later in the same step already sees the new result.
/// </summary>
internal static class Front
{
    public static readonly string[] Cities = AfterAction.ZilleCities;

    /// <summary>How a phase went: aim taken (with or without a loss), part of a two-city aim, aim missed but other cities
    /// taken, gains and losses in one phase, nothing moved, or cities lost and none won.</summary>
    public enum Kind { Won, WonLoss, Partial, Other, Mixed, Missed, Lost }

    public sealed class Phase
    {
        public int K, Number;
        public Objective Objective;
        public string Aim = string.Empty;
        public bool Won;
        public List<string> Gained = new(), Lost = new();

        public List<string> CitiesGained => Gained.Where(c => Cities.Contains(c)).ToList();
        public List<string> TownsRetaken => Gained.Where(c => !Cities.Contains(c)).ToList();
        public List<string> CitiesLost => Lost.Where(c => Cities.Contains(c)).ToList();
        public List<string> TownsLost => Lost.Where(c => !Cities.Contains(c)).ToList();
        public List<string> AimTowns => Aim.Split(" and ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        /// <summary>The aim's cities this phase took ("Nanta").</summary>
        public List<string> AimTaken => AimTowns.Where(Gained.Contains).ToList();
        /// <summary>The aim's cities this phase did not take ("Isor").</summary>
        public List<string> AimMissed => AimTowns.Where(t => !Gained.Contains(t)).ToList();
        /// <summary>Cities taken outside the aim ("Auremina" when Phase I took Zille and Auremina).</summary>
        public List<string> Extra => CitiesGained.Where(c => !AimTowns.Contains(c)).ToList();
        public bool Partial => !Won && AimTaken.Count > 0;

        public Kind Kind =>
            Won ? (Lost.Count == 0 ? Kind.Won : Kind.WonLoss)
            : Gained.Count > 0 ? (Lost.Count > 0 ? Kind.Mixed : Partial ? Kind.Partial : Kind.Other)
            : Lost.Count > 0 ? Kind.Lost : Kind.Missed;

        /// <summary>Rizia came out of the phase with more than it went in with, and lost nothing.</summary>
        public bool Gain => Gained.Count > 0 && Lost.Count == 0;
    }

    public static int Fought => ZilleCampaign.OperationsDone;

    private static string[] History =>
        ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);

    private static Objective ObjectiveOf(int k)
    {
        string[] h = History;
        return k >= 1 && k <= h.Length && Enum.TryParse(h[k - 1].Split(':')[0], out Objective o) ? o : Objective.Harbor;
    }

    private static bool WonOf(int k)
    {
        string[] h = History;
        return Variables.GetBool(ZilleCampaign.ResultVar(k)) ||
               (k >= 1 && k <= h.Length && h[k - 1].Split(':') is { Length: > 1 } kv && kv[1] == "won");
    }

    /// <summary>The k-th phase fought (1-based), or null when it has not been fought.</summary>
    public static Phase Of(int k)
    {
        if (k < 1 || k > Fought)
        {
            return null;
        }

        int p = Variables.GetInt(ZilleCampaign.PhaseOfVar(k));
        var ph = new Phase
        {
            K = k, Number = p > 0 ? p : Math.Min(k, ZilleCampaign.LastPhase), Objective = ObjectiveOf(k), Won = WonOf(k),
            Aim = ZilleCampaign.Str(ZilleCampaign.AimVar(k)),
        };
        if (ph.Aim.Length == 0)
        {
            Legacy(k, ph);
            return ph;
        }

        ph.Gained = ZilleCampaign.Gained(k);
        ph.Lost = ZilleCampaign.Lost(k);
        return ph;
    }

    /// <summary>The last phase fought, or null before the first.</summary>
    public static Phase LastPhase => Of(Fought);

    public static IEnumerable<Phase> All => Enumerable.Range(1, Fought).Select(Of).Where(p => p != null);

    /// <summary>Zille cities in Rizian hands now, in map order (Zille, Auremina, Isor, Nanta).</summary>
    public static List<string> Held => Cities.Where(c => Variables.GetBool(ZilleOperation.CapturedVar(c))).ToList();
    /// <summary>Zille cities still in Wehzek hands now.</summary>
    public static List<string> Wehzek => Cities.Where(c => !Variables.GetBool(ZilleOperation.CapturedVar(c))).ToList();

    /// <summary>The Zille cities in Rizian hands, in words: "Zille city, Auremina and Nanta".</summary>
    public static string HeldWords => Join(Held.Select(c => c == "Zille" ? "Zille city" : c));
    /// <summary>The Zille cities Wehlen still holds, in words: "Isor", "Isor and Nanta".</summary>
    public static string WehzekWords => Join(Wehzek.Select(c => c == "Zille" ? "Zille city" : c));

    /// <summary>The city was taken by some phase of this war (even if Wehlen took it back later).</summary>
    public static bool EverTaken(string city) => All.Any(p => p.Gained.Contains(city));

    public static string Join(IEnumerable<string> items)
    {
        var l = items.ToList();
        return l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
    }

    public static string Has(ICollection<string> l) => l.Count > 1 ? "have" : "has";
    public static string Is(ICollection<string> l) => l.Count > 1 ? "are" : "is";
    public static string Was(ICollection<string> l) => l.Count > 1 ? "were" : "was";

    // ------------------------------------------------------------------ older saves

    /// <summary>
    /// Phases booked before the per-phase variables existed have no aim, gains or losses: derive them from the history and the
    /// captured-city flags, and store them so every reader (Lua included) sees the same answer. A city held now that no phase's
    /// aim accounts for was taken in the last phase before the aims moved past it (Phase I's Auremina when Phase II went
    /// for the ports). Losses are unknown for those phases and read as none.
    /// </summary>
    public static void Backfill()
    {
        if (!Declared)
        {
            return;
        }

        for (int k = 1; k <= Fought; k++)
        {
            if (ZilleCampaign.Str(ZilleCampaign.AimVar(k)).Length > 0)
            {
                continue;
            }

            var ph = new Phase { K = k, Objective = ObjectiveOf(k), Won = WonOf(k) };
            Legacy(k, ph);
            Variables.Set(ZilleCampaign.AimVar(k), ph.Aim);
            Variables.Set(ZilleCampaign.GainedVar(k), string.Join(",", ph.Gained));
            Variables.Set(ZilleCampaign.LostVar(k), string.Empty);
            Variables.Set(ZilleCampaign.GainsVar(k), ph.Gained.Count);
            Variables.Set(ZilleCampaign.LossesVar(k), 0);
            Variables.Set(ZilleCampaign.PartialVar(k), ph.Partial);
            Core.Log.Msg($"Front: phase {k} backfilled from an older save: aim {ph.Aim}, gained '{string.Join(",", ph.Gained)}', won={ph.Won}.");
        }
    }

    private static bool Declared => Variables.GetBool(AnDeclaration.VarDeclared);

    private static void Legacy(int k, Phase target)
    {
        int n = Fought;
        var gained = new List<string>[n + 1];
        var aims = new string[n + 1];
        var objective = new Objective[n + 1];
        var taken = new HashSet<string>();
        List<string> held = Held;
        for (int j = 1; j <= n; j++)
        {
            objective[j] = ObjectiveOf(j);
            string[] towns = ZilleCampaign.Info(objective[j]).Towns;
            var left = towns.Where(t => !taken.Contains(t)).ToList();
            aims[j] = left.Count == 0 || left.Count == towns.Length ? ZilleCampaign.Info(objective[j]).Target : string.Join(" and ", left);
            gained[j] = WonOf(j) ? left : left.Where(held.Contains).ToList();
            taken.UnionWith(gained[j]);
        }

        int Index(Objective o) => Array.IndexOf(ZilleCampaign.Sequence, o);
        foreach (string c in held.Where(c => !taken.Contains(c)))
        {
            Objective own = ZilleCampaign.Sequence.First(o => ZilleCampaign.Info(o).Towns.Contains(c));
            int j = Enumerable.Range(1, n).FirstOrDefault(j => j == n || Index(objective[j + 1]) > Index(own));
            if (j >= 1)
            {
                gained[j].Add(c);
            }
        }

        target.Aim = aims[k];
        target.Gained = Cities.Where(gained[k].Contains).ToList();
        target.Lost = new List<string>();
    }

    // ------------------------------------------------------------------ Lua

    public static readonly string Ops = $"(Variable[\"{ZilleCampaign.VarOpsDone}\"] or 0)";

    /// <summary>A per-phase condition, applied to the last phase fought.</summary>
    public static string Last(Func<int, string> perPhase) =>
        "(" + string.Join(" or ", Enumerable.Range(1, ZilleCampaign.LastPhase).Select(k => $"({Ops} == {k} and {perPhase(k)})")) + ")";

    public static string LWon(int k) => $"(Variable[\"{ZilleCampaign.ResultVar(k)}\"] == true)";
    public static string LGains(int k) => $"(Variable[\"{ZilleCampaign.GainsVar(k)}\"] or 0)";
    public static string LLosses(int k) => $"(Variable[\"{ZilleCampaign.LossesVar(k)}\"] or 0)";
    /// <summary>Part of the aim taken (the flag, and at least one city gained, which also guards against a stale flag).</summary>
    public static string LPartial(int k) => $"(Variable[\"{ZilleCampaign.PartialVar(k)}\"] == true and not {LWon(k)} and {LGains(k)} >= 1)";
    /// <summary>The phase gained exactly this one city.</summary>
    public static string LGainedOnly(int k, string city) => $"(Variable[\"{ZilleCampaign.GainedVar(k)}\"] == \"{city}\")";
    /// <summary>The phase lost exactly this one city.</summary>
    public static string LLostOnly(int k, string city) => $"(Variable[\"{ZilleCampaign.LostVar(k)}\"] == \"{city}\")";
    public static string LAim(int k, string aim) => $"(Variable[\"{ZilleCampaign.AimVar(k)}\"] == \"{aim}\")";

    public static string LKind(int k, Kind kind) => kind switch
    {
        Kind.Won => $"({LWon(k)} and {LLosses(k)} == 0)",
        Kind.WonLoss => $"({LWon(k)} and {LLosses(k)} >= 1)",
        Kind.Partial => $"({LPartial(k)} and {LLosses(k)} == 0)",
        Kind.Other => $"(not {LWon(k)} and not {LPartial(k)} and {LGains(k)} >= 1 and {LLosses(k)} == 0)",
        Kind.Mixed => $"(not {LWon(k)} and {LGains(k)} >= 1 and {LLosses(k)} >= 1)",
        Kind.Missed => $"(not {LWon(k)} and {LGains(k)} == 0 and {LLosses(k)} == 0)",
        _ => $"(not {LWon(k)} and {LGains(k)} == 0 and {LLosses(k)} >= 1)",
    };

    /// <summary>The last phase fought went this way.</summary>
    public static string LastKind(params Kind[] kinds) => Last(k => "(" + string.Join(" or ", kinds.Select(x => LKind(k, x))) + ")");

    /// <summary>Lua: the city is in Rizian hands now.</summary>
    public static string LHeld(string city) => $"(Variable[\"{ZilleOperation.CapturedVar(city)}\"] == true)";

    /// <summary>Lua: exactly these Zille cities are Wehzek now (the rest are Rizian).</summary>
    public static string LWehzekExactly(IEnumerable<string> wehzek)
    {
        var w = wehzek.ToList();
        return "(" + string.Join(" and ", Cities.Select(c => w.Contains(c) ? $"not {LHeld(c)}" : LHeld(c))) + ")";
    }

    /// <summary>Every non-empty set of the given cities (for a line per set of cities still Wehzek, or held).</summary>
    public static IEnumerable<List<string>> Subsets(params string[] of)
    {
        for (int m = 1; m < 1 << of.Length; m++)
        {
            yield return of.Where((_, i) => (m & (1 << i)) != 0).ToList();
        }
    }
}

