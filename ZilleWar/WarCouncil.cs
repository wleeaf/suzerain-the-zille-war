using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The Zille War Council: one conversation in the Council Chambers each war turn, modelled on vanilla's
/// "Briefing on the Invasion of Pales" (c357) and "Operation 2 Next Steps" (c412).
/// The first council (right after Konstantium) sets the war plan once: the officers weigh the plans against the army the
/// player actually has (Army_Unit_*_Total) and the King picks the campaign's tactic (direct, feint, armored thrust, landing,
/// airborne drop, mountaineers), the Aureus squadron, the fleet's opening posture and the submarines, and a barrage.
/// Later councils are progress meetings about the same war: the result and its cost, how the plan is working, push / hold /
/// ceasefire, the intelligence, at most one disruption, one rotating matter (morale, supply, reinforcements), the next
/// objective as the campaign's next step with the plan carried onto it (in its nearest sensible form where it does not fit
/// the ground or the units are gone), the fleet's current orders (keep or change), the submarines, and the barrage.
/// The tactic is never re-picked; the navy can be. Orders are read by <see cref="ZilleOperation"/>.
/// No two progress meetings run alike: the map, the salute, the result (aim reached, missed, reached late, overrun, or a month
/// held), its cost, the plan's assessment (a second one says something new), the garrison read-out ("no change" when it has
/// not changed), the objective and its phase words, the fleet, the barrage and the closing all branch on phase and outcome,
/// and before the last phase Laurento brings Konstantium's armistice lines.
/// The war table only opens once the council has given its order and no other "ZilleWar.Pre_" fragment
/// (or supply-war call) is pending in the current step, so everything that belongs before the war comes first.
/// </summary>
internal static class WarCouncil
{
    public const string Conv = "ZilleWar/Council/War";
    public const string FragmentPrefix = "ZilleWar.Pre_Council_T";
    private static readonly string[] PrePrefixes = { "ZilleWar.Pre_", "ZilleWar.Call_" };

    // ---- the King's orders (read by ZilleOperation.BuildWarOperation)
    public const string VarApproach = "ZilleWar.Plan_Approach";            // the war plan: an Approach name, chosen once at the first council
    public const string VarBombard = "ZilleWar.Plan_Bombard";              // preparatory barrage ordered (per operation)
    public const string VarDelegated = "ZilleWar.Plan_Delegated";          // the General Staff drew up the plan
    public const string VarFleetRecalled = "ZilleWar.Plan_FleetRecalled";  // Aureus squadron brought north (stays true)
    public const string VarNavy = "ZilleWar.Plan_Navy";                    // standing order: "Strait" | "Coast" | "Home"
    public const string VarSubsOut = "ZilleWar.Plan_SubsOut";              // standing order: submarines on patrol
    public const string VarOpForm = "ZilleWar.Plan_OperationForm";         // the plan's form in the operation last built
    public const string VarOpAdapt = "ZilleWar.Plan_OperationAdapt";       // "" | "hills" | "inland" | "units"

    // ---- council bookkeeping
    public const string VarDoneTurn = "ZilleWar.Council_DoneTurn";         // last turn a council was completed
    public const string VarOrderTurn = "ZilleWar.Council_OrderTurn";       // last turn the council ordered an operation
    public const string VarHeldTurn = "ZilleWar.Council_HeldTurn";         // last turn the King chose to hold
    private const string VarMeetings = "ZilleWar.Council_Meetings";        // councils completed
    private const string VarToldEmbargo = "ZilleWar.Council_Told_Embargo"; // once-per-war intelligence lines
    private const string VarToldRaids = "ZilleWar.Council_Told_Raids";

    // ---- set by C# right before the council opens (Lua reads them)
    private const string VarTurn = "ZilleWar.Council_Turn";
    private const string VarKind = "ZilleWar.Council_Kind";                // 1 = first briefing, 2 = progress meeting
    private const string VarObjective = "ZilleWar.Council_Objective";      // Objective enum name
    private const string VarCoastal = "ZilleWar.Council_Coastal";
    private const string VarLastObjective = "ZilleWar.Council_LastObjective";
    private const string VarLossTier = "ZilleWar.Council_LossTier";        // 0 light, 1 heavy, 2 severe
    private const string VarSetback = "ZilleWar.Council_Setback";
    private const string VarWaited = "ZilleWar.Council_Waited";
    private const string VarExtraTaken = "ZilleWar.Council_ExtraTaken";
    private const string VarVinaAsked = "ZilleWar.Council_VinaAsked";
    private const string VarFought = "ZilleWar.Council_OperationsFought";   // operations fought so far (0: nothing to report yet)
    private const string VarForm = "ZilleWar.Council_PlanForm";             // the plan's form for the next objective
    private const string VarAdapt = "ZilleWar.Council_PlanAdapt";           // why it differs from the plan ("" when it doesn't)
    private const string VarLastAdapt = "ZilleWar.Council_LastAdapt";       // the same for the operation just fought
    private const string VarMatter = "ZilleWar.Council_Matter";             // Rest | Morale | Supply | Reinforce
    private const string VarPhase = "ZilleWar.Council_Phase";               // the phase this council sends the army into (1..3)
    private const string VarLastWon = "ZilleWar.Council_LastWon";           // the operation just fought reached its aim
    private const string VarLastPhase = "ZilleWar.Council_LastPhase";       // the phase just fought (1..3), 0 before the first
    private const string VarGarrTold = "ZilleWar.Council_GarrisonTold";     // the garrison tier read out at the last council (1..4)
    private const string VarBarrages = "ZilleWar.Council_Barrages";         // barrages ordered so far
    private static string LostVar(string city) => $"ZilleWar.Council_Lost_{city.Replace(" ", string.Empty)}";
    private static readonly string[] RizianCities = { "Mave Nortus", "Dapalle", "Balle", "Jales" };

    // ---- asked this council (reset at start)
    private const string AskedForces = "ZilleWar.Council_Asked_Forces";
    private const string AskedGarrison = "ZilleWar.Council_Asked_Garrison";
    private const string AskedFronts = "ZilleWar.Council_Asked_Fronts";
    private const string HubVisited = "ZilleWar.Council_HubVisited";
    private const string AskedNavyWhy = "ZilleWar.Council_Asked_NavyWhy";  // once per war

    // ---- speakers
    private const string Lucita = "Lucita Azaro";
    private const string Carlos = "Carlos Robles Azaro";
    private const string Taddeus = "Taddeus Azaro";
    private const string Laurento = "Laurento Esquibel";
    private const string Elena = "Elena Werner";
    private const string Hugo = "Hugo Toras";
    private const string Vina = "Vina Toras";

    /// <summary>Armor: massed tanks through the wall; Airborne: paratroopers behind the line; Mountain: mountaineers over the ridges.</summary>
    public enum Approach { Direct, Feint, Landing, Armor, Airborne, Mountain }

    /// <summary>
    /// The orders for an operation. Approach: the war plan in the form it takes for this objective; Plan: the plan the King
    /// chose at the start of the campaign; Adapt: why they differ ("hills", "inland", "units", or empty). Navy: "Strait"
    /// (hunting convoys or their escorts), "Coast" (shelling the coastal batteries), "Home". Reinforcements: Wehzek units
    /// brought across the strait before the operation.
    /// </summary>
    public sealed record Orders(Approach Approach, bool Bombard, bool Delegated, bool FleetRecalled, bool FleetSplit,
        string Navy = "Home", bool SubsOut = false, int Reinforcements = 0, Approach Plan = Approach.Direct, string Adapt = "");

    /// <summary>The war plan chosen at the first council (Direct when none was chosen).</summary>
    public static Approach WarPlan => Enum.TryParse(ZilleCampaign.Str(VarApproach), out Approach x) ? x : Approach.Direct;

    /// <summary>
    /// The war plan carried onto an objective in its nearest sensible form: tanks can't lead in the Auremina hills (infantry
    /// first, armor behind: a direct push); a landing can't reach inland Auremina (the marines land on the coast behind the
    /// hills and draw the reserves: a feint); and when the units the plan needs are spent, it becomes a direct assault.
    /// </summary>
    public static Approach Form(Approach plan, Objective objective, out string adapt)
    {
        static int U(string u) => Variables.GetInt($"RiziaDLC.Army_Unit_{u}_Total");
        adapt = string.Empty;
        switch (plan)
        {
            case Approach.Armor when objective == Objective.Wall:
                adapt = "hills";
                return Approach.Direct;
            case Approach.Armor when U("Tank") < 2:
            case Approach.Airborne when U("Paratrooper") < 1:
            case Approach.Mountain when U("Mountaineer") < 1:
                adapt = "units";
                return Approach.Direct;
            case Approach.Landing when U("Marine") < 1 || U("Ship") < 1:
                adapt = "units";
                return objective == Objective.Wall ? Approach.Feint : Approach.Direct;
            case Approach.Landing when objective == Objective.Wall:
                adapt = "inland";
                return Approach.Feint;
            default:
                return plan;
        }
    }

    /// <summary>The orders for the operation being built now: the plan's form is computed and kept for the report.</summary>
    public static Orders OrdersFor(Objective objective)
    {
        Approach form = Form(WarPlan, objective, out string adapt);
        Variables.Set(VarOpForm, form.ToString());
        Variables.Set(VarOpAdapt, adapt);
        return Build(form, adapt);
    }

    /// <summary>The orders of the operation last built (or, before any, the plan's form for the next objective).</summary>
    public static Orders CurrentOrders()
    {
        if (Enum.TryParse(ZilleCampaign.Str(VarOpForm), out Approach form))
        {
            return Build(form, ZilleCampaign.Str(VarOpAdapt));
        }

        form = Form(WarPlan, ZilleCampaign.NextObjective, out string adapt);
        return Build(form, adapt);
    }

    private static Orders Build(Approach form, string adapt)
    {
        bool recalled = Variables.GetBool(VarFleetRecalled);
        bool split = !recalled && Variables.GetBool("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship");
        string navy = ZilleCampaign.Str(VarNavy) is "Strait" or "Coast" ? ZilleCampaign.Str(VarNavy) : "Home";
        bool subs = Variables.GetBool(VarSubsOut);
        return new Orders(form, Variables.GetBool(VarBombard), Variables.GetBool(VarDelegated), recalled, split,
            navy, subs, SupplyWar.StraitReinforcements(navy, subs), WarPlan, adapt ?? string.Empty);
    }

    private static readonly HashSet<string> Reinjected = new();     // "operation:turn", one re-injection per turn and session
    private static Talk t;

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarApproach, VarBombard, VarDelegated, VarFleetRecalled, VarNavy, VarSubsOut, VarOpForm, VarOpAdapt, AskedNavyWhy,
                     VarDoneTurn, VarOrderTurn, VarHeldTurn, VarMeetings, VarToldEmbargo, VarToldRaids,
                     VarTurn, VarKind, VarObjective, VarCoastal, VarLastObjective, VarLossTier, VarSetback, VarWaited,
                     VarExtraTaken, VarVinaAsked, VarFought, VarForm, VarAdapt, VarLastAdapt, VarMatter, VarPhase, VarLastWon, VarLastPhase,
                     VarGarrTold, VarBarrages,
                     AskedForces, AskedGarrison, AskedFronts, HubVisited,
                 }.Concat(RizianCities.Select(LostVar)))
        {
            Variables.Register(v);
        }

        ConversationRegistry.RegisterConversation(Conv);
        BuildConversation();
    }

    // ------------------------------------------------------------------ runtime

    /// <summary>Each step while the war is on: offer the council, then open the war table when nothing else should come first.</summary>
    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !WarFlow.AtWar)
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0;
        if (turn <= 0 || (turn == 1 && (GameState.CurrentStepNum ?? 0) < 2))
        {
            return;
        }

        EnsureNumbers();
        Reinject(turn);
        OfferCouncil(turn);
        OpenWarTable(turn);
    }

    /// <summary>
    /// Open (or re-open) this turn's war table if the council has given its order and nothing that belongs before the war
    /// is pending. Safe to call at any time and idempotent; called before the game evaluates whether the step is complete,
    /// so the war fragment exists before "continue to the next chapter" can be offered.
    /// </summary>
    public static void TryOpenWarTable()
    {
        try
        {
            if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !WarFlow.AtWar)
            {
                return;
            }

            int turn = GameState.CurrentTurnNum ?? 0;
            if (turn <= 0)
            {
                return;
            }

            Reinject(turn);
            OpenWarTable(turn);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"TryOpenWarTable failed: {ex}");
        }
    }

    /// <summary>True while one of our pre-war fragments (council, calls, other modules' "ZilleWar.Pre_*") is still to be played.</summary>
    public static bool PrePending()
    {
        if (Scheduler.HasQueued(PrePrefixes))
        {
            return true;    // still waiting its turn in this step
        }

        var names = Managers.Instance?.GameFlowManager?.enabledNotDoneStoryFragmentsNames;
        if (names == null)
        {
            return false;
        }

        foreach (string n in names)
        {
            if (n != null && PrePrefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static void OfferCouncil(int turn)
    {
        string name = FragmentPrefix + turn;
        if (Variables.GetBool(WarFlow.VarOperationReady) || Variables.GetInt(WarFlow.VarLastOperationTurn) >= turn ||
            Variables.GetInt(VarDoneTurn) >= turn || GameState.StoryFragmentExistsInCurrentStep(name) || ZilleCampaign.Available().Count == 0)
        {
            return;
        }

        bool first = !Variables.GetBool(WarFlow.VarCouncilHeld) && ZilleCampaign.OperationsDone == 0;
        ObjectiveInfo next = ZilleCampaign.Info(ZilleCampaign.NextObjective);
        Prepare(turn, first, next);
        try
        {
            GameState.AddCustomConversation(Conv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(name, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                first ? "Briefing on the Liberation of Zille" : $"{ZilleCampaign.PhaseTitle(ZilleCampaign.PhaseForTurn(turn))} Briefing",
                "Council Chambers, Palas Rezna", ConversationData.ConversationType.SecurityAndOrder, Conv));
            Core.Log.Msg($"War Council offered: {name} ({(first ? "briefing" : "next steps")}, phase {ZilleCampaign.PhaseForTurn(turn)}, aim {next.Target}).");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the War Council failed: {ex}");
        }
    }

    /// <summary>Everything the council's lines branch on, computed from the campaign state.</summary>
    private static void Prepare(int turn, bool first, ObjectiveInfo next)
    {
        Variables.Set(VarTurn, turn);
        Variables.Set(VarPhase, ZilleCampaign.PhaseForTurn(turn));
        Variables.Set(VarKind, first ? 1 : 2);
        Variables.Set(VarObjective, next.Id.ToString());
        Variables.Set(VarCoastal, next.Id != Objective.Wall);
        Variables.Set(VarWaited, Variables.GetInt(VarHeldTurn) > 0 && Variables.GetInt(VarHeldTurn) == turn - 1);

        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        foreach (string city in RizianCities)
        {
            Variables.Set(LostVar(city), ZilleCampaign.IsEnemy(owners, ZilleCampaign.TownTile(city)));
        }

        int k = ZilleCampaign.OperationsDone;
        Variables.Set(VarFought, k);
        string[] hist = ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);
        string lastObjective = hist.Length > 0 ? hist[^1].Split(':')[0] : string.Empty;
        Variables.Set(VarLastObjective, lastObjective);
        Variables.Set(VarLastWon, k > 0 && (Variables.GetBool(ZilleCampaign.ResultVar(k)) || (hist.Length > 0 && hist[^1].EndsWith(":won"))));
        int lastPhase = k > 0 ? Variables.GetInt(ZilleCampaign.PhaseOfVar(k)) : 0;
        Variables.Set(VarLastPhase, lastPhase > 0 ? lastPhase : Math.Min(k, ZilleCampaign.LastPhase));
        int playerDead = k > 0 ? Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)) : 0;
        int enemyDead = k > 0 ? Variables.GetInt(ZilleCampaign.EnemyDeadVar(k)) : 0;
        int tier = playerDead >= 6 || (playerDead >= 3 && playerDead > enemyDead) ? 2 : playerDead >= 3 || playerDead > enemyDead ? 1 : 0;
        Variables.Set(VarLossTier, tier);
        bool lostOurOwn = RizianCities.Any(c => Variables.GetBool(LostVar(c)));
        Variables.Set(VarSetback, !first && (lostOurOwn || tier == 2));
        bool extra = Enum.TryParse(lastObjective, out Objective lo) &&
                     ZilleData.Towns.Any(tw => tw.Role == "city" && tw.Side == "zille" && !ZilleCampaign.Info(lo).Towns.Contains(tw.Name) &&
                                               Variables.GetBool(ZilleOperation.CapturedVar(tw.Name)) &&
                                               ZilleCampaign.Sequence.SkipWhile(o => o != lo).Skip(1).Any(o => ZilleCampaign.Info(o).Towns.Contains(tw.Name)));
        Variables.Set(VarExtraTaken, extra);

        // The war plan, carried onto the next objective (and how it was carried onto the last one).
        Approach form = Form(WarPlan, next.Id, out string adapt);
        Variables.Set(VarForm, form.ToString());
        Variables.Set(VarAdapt, adapt);
        Variables.Set(VarLastAdapt, k > 0 ? ZilleCampaign.Str(VarOpAdapt) : string.Empty);

        // One matter per progress meeting, in rotation, so no two meetings run the same way.
        int n = Math.Max(Variables.GetInt(VarMeetings), k);
        Variables.Set(VarMatter, Variables.GetBool(VarWaited) ? "Rest" : (n % 3) switch { 1 => "Morale", 2 => "Supply", _ => "Reinforce" });

        // Standing naval orders lapse when the ships for them are gone (or Hegel has the submarines' word).
        int ships = Variables.GetInt(SupplyWar.Ships), subs = Variables.GetInt(SupplyWar.Subs);
        string navy = ZilleCampaign.Str(VarNavy);
        if ((navy == "Strait" && ships < 1) || (navy == "Coast" && ships < 2))
        {
            Variables.Set(VarNavy, "Home");
        }

        if (Variables.GetBool(VarSubsOut) && (subs < 1 || Variables.GetBool(SupplyWar.VarSubsGrounded)))
        {
            Variables.Set(VarSubsOut, false);
        }
    }

    /// <summary>The council has given its order and nothing that belongs before the war is left: open the war table.</summary>
    private static void OpenWarTable(int turn)
    {
        if (Variables.GetInt(VarOrderTurn) != turn || Variables.GetInt(WarFlow.VarLastOperationTurn) >= turn ||
            Variables.GetBool(WarFlow.VarOperationReady) || PrePending() || ZilleCampaign.Available().Count == 0)
        {
            return;
        }

        if (ZilleOperation.BuildNext(out WarFragmentData frag))
        {
            Variables.Set(WarFlow.VarLastOperationTurn, turn);
            Variables.Set(WarFlow.VarOperationReady, true);
            Reinjected.Add($"{ZilleCampaign.OperationsDone + 1}:{turn}");
            WarProbe.InjectWarFragment(frag);
        }
    }

    /// <summary>
    /// An operation was ordered but its fragment is gone: the game was reloaded, or the turn ended before the war table
    /// was played (the operation is still "ready" on a later turn). Put it back, once per turn, once nothing that belongs
    /// before the war is pending. Without this the campaign would wait forever (the council is not offered while an
    /// operation is ready).
    /// </summary>
    private static void Reinject(int turn)
    {
        int k = ZilleCampaign.OperationsDone + 1;
        string key = $"{k}:{turn}";
        if (!Variables.GetBool(WarFlow.VarOperationReady) || Reinjected.Contains(key) ||
            GameState.StoryFragmentExistsInCurrentStep(ZilleOperation.FragmentName(ZilleCampaign.PhaseForTurn(turn))) || PrePending())
        {
            return;
        }

        Reinjected.Add(key);
        if (ZilleCampaign.Available().Count == 0)
        {
            Variables.Set(WarFlow.VarOperationReady, false);
            return;
        }

        if (ZilleOperation.BuildNext(out WarFragmentData frag))
        {
            Core.Log.Msg($"Re-opening the war table for operation {k} (turn {turn}).");
            Variables.Set(WarFlow.VarLastOperationTurn, turn);
            WarProbe.InjectWarFragment(frag);
        }
    }

    private static void EnsureNumbers()
    {
        foreach (string v in new[]
                 {
                     "RiziaDLC.Army_Unit_Infantry_Total", "RiziaDLC.Army_Unit_Support_Total", "RiziaDLC.Army_Unit_Tank_Total",
                     "RiziaDLC.Army_Unit_Marine_Total", "RiziaDLC.Army_Unit_Ship_Total", "RiziaDLC.Army_Unit_Submarine_Total",
                     "RiziaDLC.Resources_Military_Equipment", VarDoneTurn, VarOrderTurn, VarHeldTurn, WarFlow.VarLastOperationTurn,
                     ZilleCampaign.VarStrength, VarGarrTold, VarBarrages, VarLastPhase, PhaseResume.VarLanded,
                 })
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = 0 end");
        }

        if (Variables.GetInt(ZilleCampaign.VarStrength) <= 0)
        {
            ZilleCampaign.Strength = ZilleCampaign.DefaultStrength;
        }
    }

    // ------------------------------------------------------------------ Lua conditions

    private static string Num(string v) => $"({V(v)} or 0)";
    private static string Is(string v) => $"{V(v)} == true";
    private static string IsNot(string v) => $"{V(v)} ~= true";
    private static string And(params string[] c) => string.Join(" and ", c.Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})"));
    private static string Not(string c) => $"not ({c})";

    private const string LucitaHere = "Variable[\"RiziaDLC.Lucita_Arrested\"] ~= true and Variable[\"RiziaDLC.Lucita_Executed\"] ~= true " +
                                      "and Variable[\"RiziaDLC.Council_Security_Lucita\"] ~= false and Variable[\"" + EarlyT6.VarLucitaArrestedMidWar + "\"] ~= true";
    private const string HugoHere = "Variable[\"RiziaDLC.Hugo_Resigned\"] ~= true and Variable[\"RiziaDLC.Hugo_Arrested\"] ~= true " +
                                    "and Variable[\"RiziaDLC.Hugo_Executed\"] ~= true";
    private const string LaurentoHere = "Variable[\"RiziaDLC.Council_Foreign_Vina\"] ~= true and Variable[\"RiziaDLC.Council_Foreign_Empty\"] ~= true";
    private const string ElenaHere = "Variable[\"RiziaDLC.Council_Treasury_Rusty\"] ~= true and Variable[\"RiziaDLC.Council_Treasury_Daria\"] ~= true " +
                                     "and Variable[\"RiziaDLC.Council_Treasury_Empty\"] ~= true";
    private const string VinaHere = "Variable[\"RiziaDLC.Vina_OnCouncil\"] == true";

    // Taddeus only sits in once vanilla has brought him back (the Pales briefing) or made him war councilor.
    private const string TaddeusHere = "(Variable[\"RiziaDLC.War_Happened\"] == true or Variable[\"RiziaDLC.Council_Security_Taddeus\"] == true) " +
                                       "and Variable[\"RiziaDLC.Taddeus_Arrested\"] ~= true and Variable[\"RiziaDLC.Taddeus_Executed\"] ~= true";

    private static string First => $"{V(VarKind)} == 1";
    private static string Later => $"{V(VarKind)} ~= 1";
    private static string Fought => $"{Num(VarFought)} > 0";
    private static string Eq(string v, string value) => $"{V(v)} == \"{value}\"";
    private static string PlanIs(Approach a) => Eq(VarApproach, a.ToString());
    private static string FormIs(Approach a) => Eq(VarForm, a.ToString());

    /// <summary>The King picks the war plan: at the first council, or (old saves) whenever none was ever chosen.</summary>
    private static string ChoosePlan => $"({First}) or {V(VarApproach)} == nil or {V(VarApproach)} == \"\"";
    private static string Obj(Objective o) => $"{V(VarObjective)} == \"{o}\"";
    private static string Equip(int n) => $"{Num("RiziaDLC.Resources_Military_Equipment")} >= {n}";
    private static string Lever(SupplyWar.Lever l) => $"{Num(SupplyWar.Count(l))} > 0";

    private const string Embargo =
        "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true or (Variable[\"ZilleWar.Lever_Embargo\"] or 0) > 0";

    private static string Strength => Num(ZilleCampaign.VarStrength);

    /// <summary>Holding skips this turn's phase; there is nothing to wait for before the last one.</summary>
    private static string CanHold => $"{Num(VarPhase)} < {ZilleCampaign.LastPhase}";

    // Turn 5, when EarlyT5 has not already held its own, fuller council scene: the Royal Council sits in.
    private const string T5Council = "Variable[\"ZilleWar.Council_Turn\"] == 5 and Variable[\"ZilleWar.E5_CouncilOffered\"] ~= true";
    private static string FleetSplit => And(Is("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship"), IsNot(VarFleetRecalled));
    private const int BarrageCost = 500;

    // ------------------------------------------------------------------ helpers

    /// <summary>A line that only plays when <paramref name="cond"/> holds; otherwise the conversation moves straight on.</summary>
    private static void Opt(string name, string speaker, string text, string cond, string next, string script = null)
    {
        t.Logic(name, new[] { name + "_Say", next });
        t.Line(name + "_Say", speaker, text, new[] { next }, condition: cond, script: script);
    }

    /// <summary>Lucita's line, spoken by General Robles Azaro when she is not on the council (arrested, replaced).</summary>
    private static void Intel(string name, string text, string[] next, string cond = null, string carlosText = null)
    {
        t.Logic(name, new[] { name + "_L", name + "_C" }, condition: cond);
        t.Line(name + "_L", Lucita, text, next, condition: LucitaHere);
        t.Line(name + "_C", Carlos, carlosText ?? text, next, condition: Not(LucitaHere));
    }

    /// <summary>Optional Lucita/Carlos line: plays when cond holds, otherwise skipped.</summary>
    private static void IntelOpt(string name, string text, string cond, string next, string carlosText = null, string script = null)
    {
        t.Logic(name, new[] { name + "_L", name + "_C", next });
        t.Line(name + "_L", Lucita, text, new[] { next }, condition: And(cond, LucitaHere), script: script);
        t.Line(name + "_C", Carlos, carlosText ?? text, new[] { next }, condition: And(cond, Not(LucitaHere)), script: script);
    }

    /// <summary>The first variant whose condition holds (the last one should have none).</summary>
    private static void Pick(string name, string speaker, string next, params (string cond, string text)[] variants)
    {
        t.Logic(name, variants.Select((_, i) => $"{name}_{i}").ToArray());
        for (int i = 0; i < variants.Length; i++)
        {
            t.Line($"{name}_{i}", speaker, variants[i].text, new[] { next }, condition: variants[i].cond);
        }
    }

    // ------------------------------------------------------------------ the conversation

    private static void BuildConversation()
    {
        t = new Talk(Conv, "ZilleWar.Council.");
        string reset = Set(AskedForces, "false") + Set(AskedGarrison, "false") + Set(AskedFronts, "false") + Set(HubVisited, "false") +
                       Set(VarBombard, "false") + Set(VarDelegated, "false") +
                       // The war plan and the fleet's opening posture are set at the first council only, then carried on.
                       $"if {First} then " + Set(VarApproach, "\"Direct\"") + Set(VarNavy, "\"\"") + Set(VarSubsOut, "false") + "end; ";
        string conversants =
            $"AddConversant(\"{Carlos}\"); " +
            $"if {LucitaHere} then AddConversant(\"{Lucita}\") end; " +
            $"if {TaddeusHere} then AddConversant(\"{Taddeus}\") end; " +
            $"if ({First} or ({T5Council})) then " +
            $"if {HugoHere} then AddConversant(\"{Hugo}\") end; if {ElenaHere} then AddConversant(\"{Elena}\") end; " +
            $"if {VinaHere} then AddConversant(\"{Vina}\") end; end; " +
            $"if {LaurentoHere} then AddConversant(\"{Laurento}\") end; ";
        t.Logic("Start", new[] { "Truce", "Open1", "Open2Quiet", "Open2Held", "Open2Setback", "Open2Extra", "Open2MissedZ", "Open2MissedA", "Open2Aure", "Open2" }, hooks: AtStart(Conv), script: reset + conversants);

        // The war ended earlier this turn (a ceasefire was accepted on the telephone): the briefing becomes a short stand-down.
        t.Line("Truce", Narrator,
            "The generals had already heard. The maps were still on the table, but nobody reached for the pointers.",
            new[] { "Truce2" }, condition: $"Variable[\"{WarState.VarActive}\"] ~= true or Variable[\"{WarFlow.VarCeasefire}\"] == true");
        t.Line("Truce2", Carlos,
            "\"The orders went out an hour ago, Your Majesty. Every unit holds where it stands and fires only if fired upon. If the " +
            "Wehzeks keep their word, the guns stay quiet tonight.\"",
            new[] { "Truce3" });
        t.Choice("Truce3", "\"See that they do. And see that ours do too.\"", new[] { "End" });

        Arrival();
        PoliticsFirst();
        ResultsAndPolitics();
        Assessment();
        PushOrHold();
        IntelReadout();
        Matters();
        Briefing();
        Objectives();
        ChooseWarPlan();
        CarryOn();
        FleetAndBarrage();
        Closing();

        t.End();
        t.Register();
    }

    private static void Arrival()
    {
        // The first council: straight back from Konstantium.
        t.Line("Open1", Narrator,
            "The flight back from Kyrute had been a quiet one. Nobody aboard wanted to be the first to say out loud what we had done.",
            new[] { "Open1b" }, condition: First, sequence: "PlaySceneMusic(Security);");
        t.Line("Open1b", Narrator,
            "By the time I reached Palas Rezna, the Council Chambers had been turned into a war room. My councilors sat along one wall. " +
            "The rest of the room belonged to the generals, bent over a map of Zille covered in golden and silver tokens.",
            new[] { "Salute" });

        // Later councils: the map has changed (or, if no offensive has been fought yet, it has not).
        t.Line("Open2Quiet", Narrator,
            "The map in the Council Chambers looked exactly as I had left it. The golden tokens still stood on our side of the border, " +
            "and the silver ones had not moved an inch.",
            new[] { "Open2b" }, condition: And(Later, Not(Fought)), sequence: "PlaySceneMusic(Security);");
        t.Line("Open2", Narrator,
            "The map in the Council Chambers had been redrawn since I last saw it. The golden tokens had crossed the border, and the " +
            "silver ones had been pushed back toward the sea.",
            new[] { "Open2b" }, sequence: "PlaySceneMusic(Security);");
        t.Line("Open2Setback", Narrator,
            "The map in the Council Chambers had been redrawn since I last saw it. Some of the golden tokens had crossed the border. " +
            "Others had been swept off the table, and a few silver ones now stood on our side of the line.",
            new[] { "Open2b" }, condition: And(Later, Is(VarSetback)), sequence: "PlaySceneMusic(Security);",
            hooks: null);
        // Each progress meeting finds the map changed in its own way: held, overrun, short of the aim, or past it.
        t.Line("Open2Held", Narrator,
            "Nothing on the map in the Council Chambers had moved in a month. Someone had dusted the tokens, and the golden ones still stood " +
            "exactly where the last phase had left them.",
            new[] { "Open2b" }, condition: And(Later, Fought, Is(VarWaited)), sequence: "PlaySceneMusic(Security);");
        t.Line("Open2Extra", Narrator,
            "The map in the Council Chambers had been redrawn twice since I last saw it: once for the objective, and once more for everything " +
            "our columns had taken beyond it.",
            new[] { "Open2b" }, condition: And(Later, Is(VarExtraTaken)), sequence: "PlaySceneMusic(Security);");
        t.Line("Open2MissedZ", Narrator,
            "The map in the Council Chambers had hardly changed. The golden tokens had crossed the border and reached the river below Zille, " +
            "and there they had stopped. The silver ones still ringed the city.",
            new[] { "Open2b" }, condition: And(Later, Fought, IsNot(VarLastWon), $"{V(VarLastObjective)} == \"Harbor\""),
            sequence: "PlaySceneMusic(Security);");
        t.Line("Open2MissedA", Narrator,
            "On the map in the Council Chambers, the golden tokens had climbed into the foothills and stopped short of Auremina. Someone had " +
            "drawn the new line in pencil, as if nobody quite trusted it yet.",
            new[] { "Open2b" }, condition: And(Later, Fought, IsNot(VarLastWon), $"{V(VarLastObjective)} == \"Wall\""),
            sequence: "PlaySceneMusic(Security);");
        t.Line("Open2Aure", Narrator,
            "Someone had moved the golden tokens up into the hills on the map in the Council Chambers. The silver ones that were left stood " +
            "along the northern coast, around Isor and Nanta.",
            new[] { "Open2b" }, condition: And(Later, Fought, $"{V(VarLastObjective)} == \"Wall\""), sequence: "PlaySceneMusic(Security);");
        t.Logic("Open2b", new[] { "Open2Laurento3", "Open2Laurento", "Salute" });
        t.Line("Open2Laurento3", Narrator,
            "Laurento Esquibel sat at the far end of the table. His folder of cables from Konstantium had grown a good deal thicker since last month.",
            new[] { "Salute" }, condition: And(LaurentoHere, $"{Num(VarPhase)} >= 3"));
        t.Line("Open2Laurento", Narrator,
            "Laurento Esquibel sat at the far end of the table with a folder of cables from Konstantium, looking as if he would rather be anywhere else.",
            new[] { "Salute" }, condition: LaurentoHere);

        t.Logic("Salute", new[] { "SaluteFirst", "SaluteLast", "SaluteLater" });
        t.Line("SaluteLast", Narrator,
            "Nobody needed telling that this was the last briefing of the campaign. The officers came to attention as I entered, and Carlos " +
            "did not wait for them to sit.", new[] { "Taddeus" }, condition: $"{Num(VarPhase)} >= 3");
        t.Line("SaluteFirst", Narrator, "As soon as I entered, the chatter stopped and every officer in the room came to attention.",
            new[] { "SaluteA", "SaluteB", "SaluteC" }, condition: First);
        t.Line("SaluteLater", Narrator, "The officers came to attention as I entered. Carlos was already at the map, with the month's reports under his arm.",
            new[] { "Taddeus" });
        t.Choice("SaluteA", "I saluted.", new[] { "SaluteA2" }, menu: "Salute.");
        t.Line("SaluteA2", Narrator, "The room returned the salute with full force.", new[] { "Taddeus" });
        t.Choice("SaluteB", "\"At ease.\"", new[] { "SaluteB2" });
        t.Line("SaluteB2", Narrator, "Shoulders relaxed, slightly. Nobody sat down.", new[] { "Taddeus" });
        t.Choice("SaluteC", "\"Let's not waste any time.\"", new[] { "SaluteC2" });
        t.Line("SaluteC2", Carlos, "\"Your Majesty.\"", new[] { "Taddeus" });

        t.Logic("Taddeus", new[] { "TaddeusFirst", "TaddeusLast", "TaddeusLater", "Carlos" });
        t.Line("TaddeusLast", Narrator,
            "Taddeus Azaro had asked to be wheeled closer to the map. He had not said a word since he arrived, and his eyes stayed on the line " +
            "where the last phase had stopped.", new[] { "Carlos" }, condition: And(TaddeusHere, $"{Num(VarPhase)} >= 3"));
        t.Line("TaddeusFirst", Narrator,
            "General Taddeus Azaro had been wheeled to the head of the table. He raised a trembling hand to his brow, and his eyes never left the map.",
            new[] { "Carlos" }, condition: And(First, TaddeusHere));
        t.Line("TaddeusLater", Narrator, "Taddeus Azaro sat at the head of the table, where he had sat for every briefing since the war began.",
            new[] { "Carlos" }, condition: TaddeusHere);

        t.Logic("Carlos", new[] { "Carlos1", "Pol2" });
        t.Line("Carlos1", Narrator,
            "General Carlos Robles Azaro, Commander of the Armed Forces, stepped forward. He had been running the mobilization from " +
            "this room since the first cable arrived from Konstantium.",
            new[] { "Carlos2" }, condition: First);
        t.Line("Carlos2", Carlos,
            "\"Your Majesty. The northern army group is mobilized. Our divisions are at Dapalle and Mave Nortus, and the fleet is putting to sea " +
            "from Sallabes. We can cross the border within forty-eight hours of your order.\"",
            new[] { "Pol1" });
    }

    /// <summary>The first council, the morning after Konstantium: the Royal Council has its say before the generals.</summary>
    private static void PoliticsFirst()
    {
        string anyone = $"({HugoHere}) or ({LaurentoHere}) or ({ElenaHere}) or ({VinaHere})";
        t.Logic("Pol1", new[] { "Pol1Hugo", "Pol1HugoNone", "Intel" });
        t.Line("Pol1Hugo", Hugo,
            "\"Before the generals begin, Your Majesty. The news reached Porte Drazon before you did. There were crowds at the palace gates by " +
            "nightfall, singing the anthem. I don't believe I've heard it sung like that since your father's day.\"",
            new[] { "Pol1Laur" }, condition: HugoHere);
        t.Line("Pol1HugoNone", Narrator, "My councilors had waited up for me. They did not wait for the generals to finish saluting.",
            new[] { "Pol1Laur" }, condition: And(Not(HugoHere), anyone));

        Opt("Pol1Laur", Laurento,
            "\"Konstantium was rather less musical, Your Grace. President Smolak walked out of the session, and the Security Committee sat until " +
            "midnight without agreeing on a censure. That is the good news. The bad news is that they will meet again next week, and the week " +
            "after that, for as long as this war lasts.\"",
            LaurentoHere, "Pol1Elena");
        Opt("Pol1Elena", Elena,
            "\"And for as long as it lasts, it will cost us. The mobilization alone has eaten what I had set aside for the winter. I'll find the " +
            "money, Your Majesty. I would only ask the generals to remember that every shell has a price.\"",
            ElenaHere, "Pol1Vina");
        Opt("Pol1Vina", Vina,
            "\"Has anyone asked the people of Zille what they want? The Rizians there, and the Wehzeks too. They're the ones who will be under the shells.\"",
            VinaHere, "Pol1HugoVina", script: Set(VarVinaAsked, "true"));
        Opt("Pol1HugoVina", Hugo,
            "\"They have been asking since the lease expired, Your Highness. His Majesty has finally answered them.\"",
            And(VinaHere, HugoHere), "Pol1ReplyGate");
        // A node offers either lines or choices, never both: Dialogue System plays a valid line before any choice.
        t.Logic("Pol1ReplyGate", new[] { "Pol1Reply", "Intel" });
        t.Logic("Pol1Reply", new[] { "Pol1R1", "Pol1R2", "Pol1R3" }, condition: anyone);
        t.Choice("Pol1R1", "\"Wehlen made this choice when it refused to give back what was ours.\"", new[] { "Pol1R1b" });
        t.Line("Pol1R1b", Narrator, "Nobody argued. Around the table, a few heads nodded slowly.", new[] { "Intel" });
        t.Choice("Pol1R2", "\"Your concerns are heard. The best answer to all of them is a short war.\"", new[] { "Pol1R2b" });
        t.Line("Pol1R2b", Carlos, "\"Then let us make it a short one, Your Majesty.\"", new[] { "Intel" });
        t.Choice("Pol1R3", "\"The decision is made. General, proceed.\"", new[] { "Pol1R3b" });
        t.Line("Pol1R3b", Narrator, "The councilors fell silent. Carlos cleared his throat and turned back to the map.", new[] { "Intel" });
    }

    /// <summary>Later councils: the last operation's result, and (Turn 5) the Royal Council's view of the war.</summary>
    private static void ResultsAndPolitics()
    {
        // Turn 5: the council sits in. This is what the player sees where vanilla had the Wehlen trade talks.
        string t5 = T5Council;
        t.Logic("Pol2", new[] { "Pol2Open", "Res" });
        t.Line("Pol2Open", Narrator,
            "The Royal Council had asked to sit in on the briefing. They took the chairs along the wall, and the generals pretended not to mind.",
            new[] { "Pol2Laur" }, condition: And(t5, $"({HugoHere}) or ({LaurentoHere}) or ({ElenaHere}) or ({VinaHere})"));
        Opt("Pol2Laur", Laurento,
            "\"The Security Committee met again on Tuesday, Your Majesty. Valgsland tabled a censure of our declaration, and it failed, just as " +
            "Smolak's protocol failed before it. Konstantium has now failed to agree on Zille twice in one month. I am not yet sure whether to " +
            "call that a victory.\"",
            LaurentoHere, "Pol2Elena");
        Opt("Pol2Elena", Elena,
            "\"Before all this, Smolak was going to offer us his oil for our gold. Now we buy our oil from someone else at twice the price, and " +
            "the army burns it faster than the rest of the country put together.\"",
            ElenaHere, "Pol2Elena2");
        Opt("Pol2Elena2", Elena,
            "\"And we are still sending our own energy north to Thornbourgh under the Rumburg contract. I'd like that remembered the next time " +
            "the Queen calls to ask for a favor.\"",
            And(ElenaHere, "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
                           "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true"), "Pol2Hugo");
        Opt("Pol2Hugo", Hugo,
            "\"The House of Delegates met yesterday. Baroness de Rava and her party are delighted. The Brenas members are not. Half of them " +
            "have family on the other side of the Zille line.\"",
            HugoHere, "Pol2Vina");
        t.Logic("Pol2Vina", new[] { "Pol2Vina1", "Pol2Vina2", "Pol2Reply" });
        t.Line("Pol2Vina1", Vina,
            "\"I asked whether anyone had asked the people of Zille. They're answering now, Father. From their cellars.\"",
            new[] { "Pol2Reply" }, condition: And(VinaHere, Is(VarVinaAsked)));
        t.Line("Pol2Vina2", Vina,
            "\"The people of Zille are in their cellars, Father. Rizians and Wehzeks both. Does anyone in this room intend to ask them what they want?\"",
            new[] { "Pol2Reply" }, condition: And(VinaHere, IsNot(VarVinaAsked)));
        t.Logic("Pol2Reply", new[] { "Pol2R1", "Pol2R2" });
        t.Choice("Pol2R1", "\"Every day we argue in this room is another day in those cellars. Let's end it quickly.\"", new[] { "Pol2R1b" });
        t.Logic("Pol2R1b", new[] { "Pol2R1HV", "Pol2R1N" });
        t.Line("Pol2R1HV", Narrator, "Hugo murmured his agreement. Vina looked away.", new[] { "Res" }, condition: And(HugoHere, VinaHere));
        t.Line("Pol2R1N", Narrator, "A few heads nodded along the wall. Nobody asked to speak again.", new[] { "Res" });
        t.Choice("Pol2R2", "\"Thank you all. Now let the generals speak.\"", new[] { "Res" });

        // The result of the last operation (nothing to report while no offensive has been fought yet).
        t.Logic("Res", new[] { "ResHeld", "ResSay", "ResWaited" });
        // After a month's hold there is no new result to report, only the quiet.
        t.Logic("ResHeld", new[] { "ResHeldL", "ResHeldC" }, condition: And(Fought, Is(VarWaited)));
        t.Logic("ResHeldL", new[] { "ResWaited" }, condition: LucitaHere);
        t.Line("ResHeldC", Carlos,
            "\"The front has been quiet since we halted, Your Majesty. Patrols, a few raids at night, and the weather. The men have rested, " +
            "and so, I'm afraid, have theirs.\"", new[] { "ResTwoWars" });
        t.Logic("ResSay", new[] { "ResSayPick" }, condition: Fought);
        Pick("ResSayPick", Carlos, "ResWhat",
            ($"{Num(VarLastPhase)} >= 2", "\"Your Majesty. The second phase is over, and here is where it left us.\""),
            (null, "\"Your Majesty. The reports from the first phase are in.\""));
        string lastObj(string o) => $"{V(VarLastObjective)} == \"{o}\"";
        string missedAim = IsNot(VarLastWon);
        t.Logic("ResWhat", new[] { "ResMissZ2", "ResMissZ", "ResMissA", "ResMissP", "ResHarborLate", "ResHarbor", "ResWall", "ResOther" });
        // A missed aim is a setback in a war that goes on: the next phase goes in from the ground we hold.
        t.Line("ResMissZ2", Carlos,
            "\"We have failed to take Zille a second time, Your Majesty. Our lines are closer than they were, but the harbor and the station " +
            "are still theirs.\"", new[] { "ResExtra" }, condition: And(missedAim, lastObj("Harbor"), $"{Num(VarFought)} >= 2"));
        t.Line("ResMissZ", Carlos,
            "\"We did not take Zille. The columns reached the river crossings and the outskirts, but the harbor and the station are still in " +
            "Wehzek hands. The next phase goes in from the ground we hold.\"", new[] { "ResExtra" }, condition: And(missedAim, lastObj("Harbor")));
        t.Line("ResMissA", Carlos,
            "\"Auremina held. We are in the foothills and among the first mine heads, but the town and the ridges above it are still theirs.\"",
            new[] { "ResExtra" }, condition: And(missedAim, lastObj("Wall")));
        t.Line("ResMissP", Carlos,
            "\"The ports held, Your Majesty. Our columns are on the coast road, but Isor and Nanta are still in Wehzek hands.\"",
            new[] { "ResExtra" }, condition: missedAim);
        t.Line("ResHarborLate", Carlos,
            "\"Zille is ours at last. It took two phases instead of one, but the harbor, the station and their headquarters are in our hands.\"",
            new[] { "ResExtra" }, condition: And(lastObj("Harbor"), $"{Num(VarFought)} >= 2"));
        t.Line("ResHarbor", Carlos,
            "\"Zille is ours. The harbor, the railway station and the Wehzek headquarters. Their whole southern front has lost its port.\"",
            new[] { "ResExtra" }, condition: $"{V(VarLastObjective)} == \"Harbor\"");
        t.Line("ResWall", Carlos,
            "\"Auremina has fallen, and the gold hills with it. The roads north to Isor and Nanta are open.\"",
            new[] { "ResExtra" }, condition: $"{V(VarLastObjective)} == \"Wall\"");
        t.Line("ResOther", Carlos, "\"Our last offensive took everything we asked of it.\"", new[] { "ResExtra" });
        Opt("ResExtra", Carlos,
            "\"And we took more than the Staff had planned for. Some of the towns beyond the objective gave up as soon as they saw our flags.\"",
            Is(VarExtraTaken), "ResLostMave");
        Opt("ResLostMave", Carlos,
            "\"But while we were pushing west, the enemy took Mave Nortus. Their flag is flying over our own border town.\"",
            Is(LostVar("Mave Nortus")), "ResLostDapalle");
        Opt("ResLostDapalle", Carlos,
            "\"They have also broken into Dapalle. Our headquarters had to fall back to Balle.\"",
            Is(LostVar("Dapalle")), "ResLostBalle");
        Opt("ResLostBalle", Carlos, "\"Balle is in Wehzek hands. They are closer to the heart of Brenas than they have ever been.\"",
            Is(LostVar("Balle")), "ResLostJales");
        Opt("ResLostJales", Carlos, "\"And they have landed at Jales. I don't yet know how they got past the fleet.\"",
            Is(LostVar("Jales")), "ResLosses");
        string second = $"{Num(VarLastPhase)} >= 2";
        Pick("ResLosses", Carlos, "ResWaited",
            (And($"{V(VarLossTier)} == 2", second),
                "\"And it cost us more than any month of this war. The lists no longer fit on the board in the corridor. The clerks have started a second one.\""),
            ($"{V(VarLossTier)} == 2", "\"It cost us dearly. The casualty lists are the longest this army has seen since the Reclamation War.\""),
            (And($"{V(VarLossTier)} == 1", second),
                "\"It was not cheap. The hospital trains have run south day and night, and the field hospitals at Balle are taking the overflow.\""),
            ($"{V(VarLossTier)} == 1", "\"It was not cheap. The hospitals in Dapalle are full, and the trains south are full of wounded.\""),
            (second, "\"Our losses were light again. The men have started to believe in their own luck, which worries me more than the Wehzeks do.\""),
            (null, "\"Our losses were light, thank God. The men are tired, but in good spirits.\""));
        Opt("ResWaited", Lucita,
            "\"The front has been quiet since we halted. Our men have rested. I'm afraid so have theirs.\"",
            And(Is(VarWaited), LucitaHere), "ResTwoWars");
        Opt("ResTwoWars", Taddeus,
            "\"Two wars at once, Lieutenant. I have seen what that does to an army. We will not fight them both at full stretch. We sequence them. " +
            "Zille holds, Pales breaks, then we turn north again.\"",
            And(TaddeusHere, "Variable[\"RiziaDLC.Pales_War\"] == true"), "Pol3");

        // Before the last phase, Laurento brings word from Konstantium: the armistice lines are being drawn.
        string phase3 = $"{Num(VarPhase)} >= 3";
        t.Logic("Pol3", new[] { "Pol3Held", "Pol3Not", "Assess" });
        t.Line("Pol3Held", Laurento,
            "\"One word from Konstantium before the generals go on, Your Majesty. The Secretary-General's staff have started drafting " +
            "armistice lines. They draw them where the armies stand on the day they finish drafting, and I would like us to be standing a " +
            "little farther north.\"", new[] { "Assess" }, condition: And(phase3, LaurentoHere, Is(WarFlags.ZilleCityHeld)));
        t.Line("Pol3Not", Laurento,
            "\"One word from Konstantium, Your Majesty. The Secretary-General's staff have started drafting armistice lines. As things " +
            "stand tonight, theirs runs south of Zille.\"", new[] { "Assess" }, condition: And(phase3, LaurentoHere));
    }

    /// <summary>Push on, hold for a turn, or seek a ceasefire. On a setback the generals push back, as in c412.</summary>
    private static void PushOrHold()
    {
        t.Logic("Push", new[] { "PushSetback", "PushGood" });
        string missed = And(Fought, IsNot(VarLastWon), IsNot(VarWaited));
        string last = $"{Num(VarPhase)} >= {ZilleCampaign.LastPhase}";
        t.Logic("PushGood", new[] { "PushGoodHeld", "PushGoodMissed", "PushGoodLast", "PushGood0" }, condition: IsNot(VarSetback));
        t.Line("PushGoodHeld", Carlos, "\"The men are rested and the guns are up, Your Majesty. The Staff is ready to move the moment you give the word.\"",
            new[] { "PushTad" }, condition: Is(VarWaited));
        t.Line("PushGoodMissed", Carlos,
            "\"The divisions are still in front of the objective, Your Majesty, and they want another go at it. The Staff is ready when you are.\"",
            new[] { "PushTad" }, condition: missed);
        t.Line("PushGoodLast", Carlos,
            "\"One more phase, Your Majesty, and the Staff is ready for it. After this one there is no next month to save anything for.\"",
            new[] { "PushTad" }, condition: last);
        t.Line("PushGood0", Carlos, "\"The troops are waiting for their orders, Your Majesty. The Staff is ready to move on the next objective.\"",
            new[] { "PushTad" });
        t.Logic("PushTad", new[] { "PushTadMissed", "PushTadLast", "PushTad0", "PushMenu" });
        t.Line("PushTadMissed", Taddeus, "\"We were close, Lieutenant. Closer than the newspapers say.\"", new[] { "PushMenu" },
            condition: And(TaddeusHere, missed));
        t.Line("PushTadLast", Taddeus, "\"One more push, Lieutenant. Finish it, and finish it properly.\"", new[] { "PushMenu" },
            condition: And(TaddeusHere, last));
        t.Line("PushTad0", Taddeus, "\"A complete victory is in sight, Lieutenant.\"", new[] { "PushMenu" }, condition: TaddeusHere);
        t.Logic("PushMenu", new[] { "PushGo", "PushGoMissed", "PushGoLast", "PushWait", "Cease1", "Cease2" });
        t.Choice("PushGo", "\"Then we keep moving. Smolak doesn't get a month to recover.\"", new[] { "Intel" },
            condition: And(Not(last), Not(missed)));
        t.Choice("PushGoMissed", "\"Then we go back in. Same objective, and this time we take it.\"", new[] { "Intel" },
            condition: And(Not(last), missed));
        t.Choice("PushGoLast", "\"Then we finish it. All of it, if the army can.\"", new[] { "Intel" }, condition: last);
        t.Choice("PushWait", "\"We'll hold for now. The men need their rest.\"", new[] { "PushWait2" }, condition: CanHold);
        Intel("PushWait2", "\"Wehlen will use the time too, Your Majesty. Every quiet month is another month of convoys across the strait.\"",
            new[] { "PushWaitYes", "PushWaitNo" });
        t.Choice("PushWaitYes", "\"I know. We hold.\"", new[] { "Hold" });
        t.Choice("PushWaitNo", "\"You're right. We go on.\"", new[] { "Intel" });

        t.Line("PushSetback", Carlos,
            "\"We are not in a good position, Your Majesty. The divisions that went in last time are exhausted, and we have lost ground of our own.\"",
            new[] { "SetbackMenu" }, condition: Is(VarSetback));
        t.Logic("SetbackMenu", new[] { "SbPush", "SbHold", "SbLast", "Cease1", "Cease2" });
        t.Choice("SbPush", "\"Simply holding the line is unacceptable. We push on, whatever it costs.\"", new[] { "SbObj1" });
        t.Line("SbObj1", Carlos, "\"Our hands are tied, Your Majesty. Those men are not ready to go in again.\"", new[] { "SbObj2" });
        Intel("SbObj2", "\"If we push now and fail, Your Majesty, we lose the men we need to hold what we've already taken.\"",
            new[] { "SbObj3" }, carlosText: "\"If we push now and fail, we lose the men we need to hold what we've already taken, Your Majesty.\"");
        Opt("SbObj3", Taddeus, "\"Please listen to them, Lieutenant. We can't overextend.\"", TaddeusHere, "SbMenu2");
        t.Logic("SbMenu2", new[] { "SbCoward", "SbRespect", "SbYield" });
        t.Choice("SbCoward", "\"Cowards, all of you! You will attack, or I will find generals who will.\"", new[] { "SbCoward2" });
        t.Line("SbCoward2", Narrator, "Everyone seemed taken aback, but nobody dared to protest.", new[] { "SbAsYouWish" });
        t.Choice("SbRespect", "\"Respectfully, General, I disagree. I order you to attack.\"", new[] { "SbAsYouWish" });
        t.Line("SbAsYouWish", Carlos, "\"As you wish, Your Majesty.\"", new[] { "Intel" });
        t.Choice("SbYield", "\"You may have a point. Very well. We hold, for now.\"", new[] { "SbYield2" }, condition: CanHold);
        t.Line("SbYield2", Carlos, "\"Agreed. We should wait for better conditions.\"", new[] { "Hold" });
        t.Choice("SbHold", "\"Then be the wall that protects Brenas. Hold the line, and let nothing through.\"", new[] { "Hold" }, condition: CanHold);
        // Before the last phase there is no waiting for better weather: it is one more push, or talks.
        t.Choice("SbLast", "\"Then this is the last push, General. Give the men what you can, and we go in one final time.\"", new[] { "SbLast2" },
            condition: Not(CanHold));
        t.Line("SbLast2", Carlos,
            "\"One final time, then. I'll put the freshest divisions in front and keep the tired ones on the line we hold. If it fails, Your " +
            "Majesty, the line we hold is where this war will end.\"", new[] { "Intel" });

        t.Line("Hold", Carlos, "\"Then we dig in and wait. I'll see that the men get their rest.\"", new[] { "Hold2" });
        t.Line("Hold2", Narrator, "The generals saluted. For this month, at least, the war table would stay quiet, and the next phase would wait.", new[] { "HoldEnd" });
        t.Logic("HoldEnd", new[] { "End" },
            script: Set(VarDoneTurn, V(VarTurn)) + Set(VarHeldTurn, V(VarTurn)) + Set(WarFlow.VarCouncilHeld, "true") + Met +
                    $"if {Strength} < 10 then {V(ZilleCampaign.VarStrength)} = {Strength} + 1 end; ");

        // The ceasefire, spoken. Laurento takes it to Konstantium when he's here.
        t.Choice("Cease1", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseLaur" },
            condition: LaurentoHere);
        t.Choice("Cease2", "\"Enough. Tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseCarlos" },
            condition: Not(LaurentoHere));
        t.Line("CeaseLaur", Laurento,
            "\"I'll speak to the Secretary-General tonight, Your Majesty. Smolak will refuse the first offer on principle. He'll accept the " +
            "second, because his treasury will make him.\"",
            new[] { "CeaseCarlos" });
        t.Line("CeaseCarlos", Carlos, "\"Understood, Your Majesty. The army will hold its positions and fire only if fired upon.\"", new[] { "CeaseTad" });
        Opt("CeaseTad", Taddeus, "\"We had them, Lieutenant. We had them.\"", TaddeusHere, "Cease3");
        t.Line("Cease3", Narrator, "The generals saluted, slower than before. The tokens on the map stayed exactly where they were.", new[] { "CeaseEnd" });
        t.Logic("CeaseEnd", new[] { "End" },
            script: Set(VarDoneTurn, V(VarTurn)) + Set(WarFlow.VarCeasefire, "true") + Set(WarFlow.VarCouncilHeld, "true") + Met);
    }

    /// <summary>What we know: the garrison's strength and the supply war so far.</summary>
    private static void IntelReadout()
    {
        t.Logic("Intel", new[] { "IntelOpenL", "IntelOpenC" }, script: SupplyWar.CouncilMeetingScript);
        t.Logic("IntelOpenL", new[] { "IntelOpenL3", "IntelOpenL2", "IntelOpenL1" }, condition: LucitaHere);
        t.Logic("IntelOpenC", new[] { "IntelOpenC3", "IntelOpenC2", "IntelOpenC1" }, condition: Not(LucitaHere));
        string ph3 = $"{Num(VarPhase)} >= 3", ph2 = And(Later, $"{Num(VarPhase)} == 2");
        t.Line("IntelOpenL3", Narrator, "Lucita did not bother with the pointer this time. She laid a thin folder on the table and opened it at the last page.",
            new[] { "Garr" }, condition: ph3);
        t.Line("IntelOpenL2", Narrator, "Lucita pinned a fresh set of reconnaissance photographs beside the map and waited for the room to settle.",
            new[] { "Garr" }, condition: ph2);
        t.Line("IntelOpenL1", Narrator, "Lucita stepped up to the map and tapped the Strait of Kreleck with her pointer.", new[] { "Garr" });
        t.Line("IntelOpenC3", Narrator, "Carlos laid a thin folder on the table and opened it at the last page.", new[] { "Garr" }, condition: ph3);
        t.Line("IntelOpenC2", Narrator, "Carlos pinned the month's reconnaissance photographs beside the map.", new[] { "Garr" }, condition: ph2);
        t.Line("IntelOpenC1", Narrator, "Carlos picked up a pointer and tapped the Strait of Kreleck.", new[] { "Garr" });
        // The same reading as at the last council is told as "no change", not word for word again.
        string told(int n) => And(Later, $"{Num(VarGarrTold)} == {n}");
        t.Logic("Garr", new[] { "GarrSame1", "GarrSame2", "GarrSame3", "GarrSame4", "Garr1", "Garr2", "Garr3", "Garr4" });
        Intel("GarrSame1", "\"The garrison is as strong as it was last month, Your Majesty. Whatever we sink or kill, Wehlen replaces.\"",
            new[] { "SupEmbargo" }, And($"{Strength} >= 8", told(1)));
        Intel("GarrSame2", "\"Their numbers haven't moved since we last met. The convoys still come, only slower, and they spend whatever lands as fast " +
                           "as it lands.\"", new[] { "SupEmbargo" }, And($"{Strength} >= 6 and {Strength} < 8", told(2)));
        Intel("GarrSame3", "\"The garrison is still on short rations. Nothing that crossed the strait this month has changed that.\"",
            new[] { "SupEmbargo" }, And($"{Strength} >= 4 and {Strength} < 6", told(3)));
        Intel("GarrSame4", "\"Still starving, Your Majesty. The deserters have started bringing their officers' maps with them.\"",
            new[] { "SupEmbargo" }, And($"{Strength} < 4", told(4)));
        Intel("Garr1", "\"The Wehzek garrison in Zille is strong, and it is still growing. Their convoys cross the strait every night, and they have had " +
                       "months to dig in.\"", new[] { "SupGarr1" }, $"{Strength} >= 8");
        t.Logic("SupGarr1", new[] { "SupEmbargo" }, script: Set(VarGarrTold, "1"));
        Intel("Garr2", "\"The Wehzek garrison is holding its numbers. Their convoys still cross the strait, but the reinforcements have slowed.\"",
            new[] { "SupGarr2" }, $"{Strength} >= 6 and {Strength} < 8");
        t.Logic("SupGarr2", new[] { "SupEmbargo" }, script: Set(VarGarrTold, "2"));
        Intel("Garr3", "\"The garrison is feeling the squeeze. Their convoys arrive late or not at all, and prisoners talk about rationed shells and " +
                       "tanks parked for want of fuel.\"", new[] { "SupGarr3" }, $"{Strength} >= 4 and {Strength} < 6");
        t.Logic("SupGarr3", new[] { "SupEmbargo" }, script: Set(VarGarrTold, "3"));
        Intel("Garr4", "\"The garrison is starving, Your Majesty. Fuel, shells, bread. Deserters come over every night now, most of them without rifles.\"",
            new[] { "SupGarr4" }, $"{Strength} < 4");
        t.Logic("SupGarr4", new[] { "SupEmbargo" }, script: Set(VarGarrTold, "4"));

        IntelOpt("SupEmbargo", "\"Rumburg no longer buys Wehzek oil. Smolak's treasury feels it, and so do his fuel depots in Zille.\"",
            And(Embargo, IsNot(VarToldEmbargo)), "SupRaids", script: Set(VarToldEmbargo, "true"));
        IntelOpt("SupRaids",
            "\"The Bludish Freedom Front has been busy in the north of Wehlen. Every battalion Smolak keeps at home to guard his ports is one he " +
            "can't send to Zille.\"", And(Lever(SupplyWar.Lever.BffRaids), IsNot(VarToldRaids)), "SupLanded", script: Set(VarToldRaids, "true"));
        // What the strait let through just before the last phase (PhaseResume landed it on the map).
        IntelOpt("SupLanded", "\"And they put fresh battalions ashore at Nanta just before our last push. We met them on the line within the day.\"",
            And(Later, Fought, IsNot(VarWaited), $"{Num(PhaseResume.VarLanded)} >= 1"), "Surprise");

        // At most one disruption (the Bludish companies, the Lespian cameras, a defector); the strait itself comes with the orders.
        SupplyWar.CouncilSurprise(t, "Surprise", "Brief", LucitaHere);
        t.Logic("Brief", new[] { "BriefFirst", "Matter" });
    }

    /// <summary>"Would you like to hear it?" and the questions hub.</summary>
    private static void Briefing()
    {
        t.Line("BriefFirst", Carlos,
            "\"Your Majesty, you have the final word on how this war is fought. But the Staff has prepared a briefing. Would you like to hear it?\"",
            new[] { "BriefYes", "BriefNo" }, condition: First);
        t.Choice("BriefYes", "\"Please, General.\"", new[] { "Hub" });
        t.Choice("BriefNo", "\"No. I've read the reports. Tell me the objective.\"", new[] { "BriefNo2" });
        t.Line("BriefNo2", Carlos, "\"Of course, Your Majesty.\"", new[] { "Obj" });

        t.Logic("Hub", new[] { "HubQ1", "HubQ2" });
        t.Line("HubQ1", Carlos, "\"What would you like to hear about first?\"", new[] { "HubMenu" }, condition: IsNot(HubVisited),
            script: Set(HubVisited, "true"));
        t.Line("HubQ2", Carlos, "\"What else would you like to know?\"", new[] { "HubMenu" });
        t.Logic("HubMenu", new[] { "AskForces", "AskGarrison", "AskFronts", "AskDone" });
        t.Choice("AskForces", "\"What's the status of our forces?\"", new[] { "Forces" }, condition: IsNot(AskedForces), script: Set(AskedForces, "true"));
        t.Choice("AskGarrison", "\"What do we know about the Wehzek garrison?\"", new[] { "Garrison" }, condition: IsNot(AskedGarrison),
            script: Set(AskedGarrison, "true"));
        t.Choice("AskFronts", "\"Give me the details of each front.\"", new[] { "Fronts" }, condition: IsNot(AskedFronts), script: Set(AskedFronts, "true"));
        t.Choice("AskDone", "\"I think I have enough information.\"", new[] { "HubDone" });
        t.Logic("HubDone", new[] { "HubDoneFirst", "Fleet" });
        t.Logic("HubDoneFirst", new[] { "Obj" }, condition: ChoosePlan);

        // Our forces, read from the army the player actually has (thresholds as in c357).
        string inf = Num("RiziaDLC.Army_Unit_Infantry_Total"), sup = Num("RiziaDLC.Army_Unit_Support_Total"), tank = Num("RiziaDLC.Army_Unit_Tank_Total");
        string marine = Num("RiziaDLC.Army_Unit_Marine_Total"), ships = Num("RiziaDLC.Army_Unit_Ship_Total"), subs = Num("RiziaDLC.Army_Unit_Submarine_Total");
        Pick("Forces", Carlos, "ForcesSup",
            ($"{inf} >= 8", "\"We can field eight or nine fully equipped infantry divisions, Your Majesty. More than enough to hold the line and carry the attack.\""),
            ($"{inf} > 5", "\"We can field six or seven infantry divisions. Enough, if we don't waste them.\""),
            (null, "\"We can field five infantry divisions at most. We will have to choose our battles carefully.\""));
        Pick("ForcesSup", Carlos, "ForcesTank",
            ($"{sup} >= 7", "\"We have a strong body of mechanized support divisions to back them up.\""),
            ($"{sup} >= 4", "\"We have a fair number of mechanized support divisions to back them up.\""),
            (null, "\"Our mechanized support is thin. We should not risk losing what we have.\""));
        Pick("ForcesTank", Carlos, "ForcesMarine",
            ($"{tank} >= 6", "\"And our tank divisions outnumber anything Wehlen has in Zille. They will lead the assault.\""),
            ($"{tank} >= 3", "\"And we have enough tanks to lead an assault on one front, if not on two.\""),
            (null, "\"As for tanks, I won't pretend. We can barely field two divisions, and the Wehzeks know it.\""));
        IntelOpt("ForcesMarine", "\"Our marines and their landing ships are ready at Sallabes.\"", $"{marine} >= 1", "ForcesShips");
        t.Logic("ForcesShips", new[] { "ForcesShips1", "ForcesShips2", "ForcesSubs" });
        Intel("ForcesShips1", "\"The fleet has enough destroyers to rule the Gulf of Moridia.\"", new[] { "ForcesSubs" }, $"{ships} >= 5");
        Intel("ForcesShips2", "\"The fleet can contest the gulf, but I wouldn't ask much more of it.\"", new[] { "ForcesSubs" }, $"{ships} >= 1 and {ships} < 5");
        t.Logic("ForcesSubs", new[] { "ForcesSubs1", "ForcesSubs2", "ForcesBff" });
        Intel("ForcesSubs1", "\"The submarines are already out in the strait, hunting their convoys.\"", new[] { "ForcesBff" },
            And($"{subs} >= 1", Lever(SupplyWar.Lever.Submarines)));
        Intel("ForcesSubs2", "\"The submarines are in port at Sallabes, waiting for orders.\"", new[] { "ForcesBff" },
            And($"{subs} >= 1", Not(Lever(SupplyWar.Lever.Submarines))));
        Opt("ForcesBff", Carlos,
            "\"And the Bludish volunteers Rumburg sent us have arrived at Dapalle. My officers can't stand them. They fight well, I'll give them that.\"",
            Lever(SupplyWar.Lever.BffFighters), "ForcesPara");
        IntelOpt("ForcesPara", "\"The paratroopers are ready at the airfield outside Sallabes, with the planes to carry them.\"", $"{Num(Para)} >= 1",
            "ForcesMount");
        Opt("ForcesMount", Carlos, "\"And the mountaineers are at full strength, if anyone needs to climb.\"", $"{Num(Mount)} >= 1", "Hub");

        // The garrison: composition by tier (matches what Garrison.Build puts on the map).
        Pick("Garrison", Carlos, "GarrisonWall",
            ($"{Strength} >= 8", "\"Close to a full corps of regulars, with four tank battalions in reserve and hardly any militia. They have had months to prepare.\""),
            ($"{Strength} >= 6", "\"Regular infantry along the line, three or so tank battalions in reserve, and militia filling the gaps.\""),
            ($"{Strength} >= 4", "\"Two tank battalions, and more militia than regulars now. The regulars are the ones they can no longer resupply.\""),
            (null, "\"One tank battalion, if it has fuel. The rest are militia and police with rifles.\""));
        t.Logic("GarrisonWall", new[] { "GarrisonWall1", "GarrisonWall2" });
        t.Line("GarrisonWall1", Carlos,
            "\"Along the border they've built a wall of concrete and wire from Mave Nortus down to the Gulf of Moridia, with a bunker covering every crossing.\"",
            new[] { "Hub" }, condition: First);
        t.Line("GarrisonWall2", Carlos, "\"Their border wall is broken where we went through. What is left of it won't stop us.\"", new[] { "Hub" });

        // The fronts, per objective.
        t.Logic("Fronts", new[] { "FrontHarbor", "FrontWall", "FrontPorts" });
        t.Line("FrontHarbor", Carlos,
            "\"Zille sits on its bay. The main road and the railway come down the river valley from Dapalle. The harbor is covered by coastal " +
            "guns, and the city has the only anti-aircraft batteries in the territory. Until those are silenced, our planes will stay away.\"",
            new[] { "FrontHarbor2" }, condition: Obj(Objective.Harbor));
        Intel("FrontHarbor2", "\"From the sea, the bay is wide open. But those guns will be waiting for any ship that comes in.\"", new[] { "FrontHome" });
        t.Line("FrontWall", Carlos,
            "\"Auremina is up in the hills. Mining country: narrow roads, ridges, and old tunnels where they can hide a battalion. Our tanks won't " +
            "be much use up there. It will be infantry work.\"", new[] { "FrontHome" }, condition: Obj(Objective.Wall));
        t.Line("FrontPorts", Carlos,
            "\"Isor is on the west coast, Nanta on the north. Nanta is where their convoys land, and it has coastal guns of its own. Take both, " +
            "and Wehlen has nowhere left to bring its men ashore.\"", new[] { "FrontHome" }, condition: Obj(Objective.Ports));
        string anyLost = string.Join(" or ", RizianCities.Select(c => $"({Is(LostVar(c))})"));
        Opt("FrontHome", Carlos, "\"And before anything else, we must take back our own towns behind the line. Brenas will not forgive us otherwise.\"",
            anyLost, "Hub");
    }

    /// <summary>The objective is announced in the fixed order: the first council lays out the campaign, later ones its next step.</summary>
    private static void Objectives()
    {
        string phase3 = $"{Num(VarPhase)} >= 3";
        string lastMissed(string o) => And(Fought, IsNot(VarLastWon), $"{V(VarLastObjective)} == \"{o}\"");
        t.Logic("Obj", new[]
        {
            "ObjHarbor", "ObjHarborLast", "ObjHarborL", "ObjPortsEarly", "ObjPortsAgain", "ObjPortsL", "ObjWallAgain", "ObjWallLastHeld",
            "ObjWallLast", "ObjWallL", "ObjWall", "ObjPorts",
        });
        t.Line("ObjHarborLast", Carlos,
            "\"It is still Zille, Your Majesty. This is the last phase of Northern Crown. If Zille is not ours when it ends, the line we hold " +
            "is the line the diplomats will draw.\"", new[] { "Command" }, condition: And(Later, Obj(Objective.Harbor), phase3));
        t.Line("ObjPortsEarly", Carlos,
            "\"We are ahead of the plan, Your Majesty. With Auremina already ours, the Staff goes straight for Isor and Nanta. Take them in " +
            "this phase, and the war ends a month early.\"", new[] { "Command" }, condition: And(Later, Obj(Objective.Ports), Not(phase3)));
        t.Line("ObjPortsAgain", Carlos,
            "\"Isor and Nanta, still. This is the last phase, Your Majesty. Whatever we hold on that coast when it ends, we keep. Whatever we " +
            "don't, Wehlen keeps.\"", new[] { "Command" }, condition: And(Later, Obj(Objective.Ports), lastMissed("Ports")));
        t.Line("ObjWallAgain", Carlos,
            "\"Auremina again, Your Majesty. The hills cost us last time, but this time we go in knowing where their guns are. And it is the " +
            "last phase: whatever we hold when it ends is what the armistice will draw.\"",
            new[] { "Command" }, condition: And(Later, Obj(Objective.Wall), lastMissed("Wall")));
        t.Line("ObjWallLastHeld", Carlos,
            "\"We held for a month, so the last phase goes for Auremina and its mines. Isor and Nanta will have to wait for the diplomats.\"",
            new[] { "Command" }, condition: And(Later, Obj(Objective.Wall), phase3, Is(VarWaited)));
        t.Line("ObjWallLast", Carlos,
            "\"Zille cost us more time than the plan allowed, so the last phase goes for Auremina and its mines. Isor and Nanta will have to " +
            "wait for the diplomats.\"", new[] { "Command" }, condition: And(Later, Obj(Objective.Wall), phase3));
        t.Line("ObjHarbor", Carlos,
            "\"The objective is the city of Zille itself: the harbor, the railway station, and the Wehzek headquarters. Take Zille, and every " +
            "Wehzek soldier south of the hills is cut off from the sea.\"", new[] { "ObjHarbor2" }, condition: And(First, Obj(Objective.Harbor)));
        t.Line("ObjHarbor2", Carlos,
            "\"After that, the hills and the gold mines at Auremina. Then the northern ports, Isor and Nanta, and the war is won. The Staff calls " +
            "the whole campaign Operation Northern Crown, in three phases, one each month, every phase starting from the ground the last one won. Zille is the first.\"", new[] { "Command" });
        t.Line("ObjHarborL", Carlos,
            "\"The objective hasn't changed, Your Majesty. Zille, its harbor and its railway station. The next phase goes in from the lines we hold " +
            "now, as soon as you give the word.\"",
            new[] { "Command" }, condition: And(Later, Obj(Objective.Harbor)));
        t.Line("ObjWallL", Carlos,
            "\"With Zille taken, the campaign moves on as planned: up the valley to Auremina and its mines. Take the hills, and their garrison in " +
            "the north is cut off from everything south of it. That is the next phase of Northern Crown.\"",
            new[] { "Command" }, condition: And(Later, Obj(Objective.Wall)));
        t.Line("ObjPortsL", Carlos,
            "\"And now the last step of the campaign: Isor and Nanta. Every Wehzek convoy lands at one or the other. Take both, and their garrison " +
            "has nowhere left to go but home. The last phase of Northern Crown.\"", new[] { "Command" }, condition: And(Later, Obj(Objective.Ports)));
        t.Line("ObjWall", Carlos,
            "\"The objective is Auremina. Take the hills and the mines, and their garrison in the north is cut off from everything south of " +
            "it. That is the next phase of Northern Crown.\"", new[] { "Command" }, condition: Obj(Objective.Wall));
        t.Line("ObjPorts", Carlos,
            "\"The objective is the northern coast: Isor and Nanta. Every Wehzek convoy lands at one or the other. Take both, and their " +
            "garrison has nowhere left to go but home. The last phase of Northern Crown.\"", new[] { "Command" });
        t.Logic("Command", new[] { "CommandPlan", "Carry" });
    }

    private const string Para = "RiziaDLC.Army_Unit_Paratrooper_Total";
    private const string Mount = "RiziaDLC.Army_Unit_Mountaineer_Total";
    private const int FeintLand = 12;      // land divisions needed to spare two for a feint

    /// <summary>All land divisions the King can put on the Zille front (marines go by sea).</summary>
    private static string LandUnits => "(" + string.Join(" + ", new[]
    {
        "Infantry", "Support", "Tank", "Mountaineer", "Paratrooper", "Mercenary", "Azaro", "GoldenGuard", "Sazon", "Toras",
    }.Select(u => Num($"RiziaDLC.Army_Unit_{u}_Total"))) + ")";

    /// <summary>
    /// The war plan, chosen once, as in c357: the officers weigh each plan against the army we actually have and against the
    /// whole campaign (the river to Zille, the Auremina hills, the northern coast), then the King gives the order.
    /// </summary>
    private static void ChooseWarPlan()
    {
        string tank = Num("RiziaDLC.Army_Unit_Tank_Total"), marine = Num("RiziaDLC.Army_Unit_Marine_Total");
        string ships = Num("RiziaDLC.Army_Unit_Ship_Total"), para = Num(Para), mount = Num(Mount), land = LandUnits;
        string canFeint = $"{land} >= {FeintLand}";
        string canArmor = $"{tank} >= 3";
        string canLanding = And($"{marine} >= 1", $"{ships} >= 2");
        string canDrop = $"{para} >= 1";
        string canMount = $"{mount} >= 1";

        t.Line("CommandPlan", Carlos,
            "\"Now, the plan for the campaign. Whatever you decide today, the Staff will carry it through every phase until Zille is ours. " +
            "And not every plan on paper is one this army can carry out.\"",
            new[] { "RFeint" }, condition: ChoosePlan);

        // The officers' reasoning, from the numbers.
        Pick("RFeint", Carlos, "RArmor",
            ($"{land} >= 16", "\"We have well over a dozen divisions to call on. We can spare two of them for a feint each time and still hit hard where it counts.\""),
            (canFeint, "\"We have a dozen divisions, give or take. Enough to spare two for a feint, though not many more.\""),
            (null, "\"A feint is out of the question, I'm afraid. With fewer than a dozen divisions, every one of them is needed on the real line.\""));
        Pick("RArmor", Carlos, "RSea",
            ($"{tank} >= 6", "\"We have six tank divisions or more. Massed on the river road, they can be through the wall before their reserves wake up. " +
                             "In the hills around Auremina they'll have to follow the infantry, but everywhere else they can lead.\""),
            (canArmor, "\"We have enough tanks for one armored thrust at a time. They can lead on the river road and the coast, though not in the hills.\""),
            (null, "\"An armored breakthrough is not on the table. Two tank divisions in a column would be a gift to their gunners.\""));
        t.Logic("RSea", new[] { "RSeaYes", "RSeaNoMarines", "RSeaNoShips", "RDrop" });
        Intel("RSeaYes", "\"Zille and Nanta both sit on the sea. The marines are ready at Sallabes, and we have the ships to carry them and cover them.\"",
            new[] { "RDrop" }, canLanding);
        Intel("RSeaNoMarines", "\"We have the ships for a landing, but no marines. Ordinary infantry in boats against a defended shore would be a slaughter.\"",
            new[] { "RDrop" }, And($"{ships} >= 2", $"{marine} < 1"));
        Intel("RSeaNoShips", "\"The marines are ready, but the fleet is too thin to carry them and cover them at the same time.\"",
            new[] { "RDrop" }, And($"{marine} >= 1", $"{ships} < 2"));
        t.Logic("RDrop", new[] { "RDropHarbor", "RDrop2", "RMount" });
        Intel("RDropHarbor", "\"The paratroopers can go in the night before, behind the city. Not on it. The anti-aircraft guns in Zille would tear them apart.\"",
            new[] { "RMount" }, And(canDrop, Obj(Objective.Harbor)));
        Intel("RDrop2", "\"The paratroopers can go in the night before every attack and hold the roads behind the Wehzeks until the army comes up.\"",
            new[] { "RMount" }, canDrop);
        t.Logic("RMount", new[] { "RMountTad", "RMountCarlos", "Tactic" });
        t.Line("RMountTad", Taddeus,
            "\"And our mountaineers were trained for the hills around Auremina, Lieutenant. Send them over the ridges while the Wehzeks watch the roads.\"",
            new[] { "Tactic" }, condition: And(canMount, TaddeusHere));
        t.Line("RMountCarlos", Carlos,
            "\"And our mountaineers were trained for the hills around Auremina. They can go over the ridges while the Wehzeks watch the roads.\"",
            new[] { "Tactic" }, condition: canMount);

        // The King's order, for the whole campaign.
        t.Line("Tactic", Carlos, "\"How do you want this war fought, Your Majesty?\"",
            new[] { "TDirect", "TFeint", "TArmor", "TLanding", "TDrop", "TMount" });
        t.Choice("TDirect", "\"We go straight at them, every time. Everything down the main road, as fast as it will go.\"", new[] { "TDirect2" },
            script: Set(VarApproach, "\"Direct\""));
        t.Line("TDirect2", Carlos,
            "\"Direct assaults, then. They'll see us coming, but if we're quick, their armor won't be up in time to meet us.\"", new[] { "PlanSet" });

        t.Choice("TFeint", "\"We keep them guessing. Every attack starts with a feint somewhere else.\"", new[] { "TFeint2" }, condition: canFeint,
            script: Set(VarApproach, "\"Feint\""));
        t.Logic("TFeint2", new[] { "FeintHarbor", "FeintOther" });
        t.Line("FeintHarbor", Carlos,
            "\"For Zille, we'll make a show toward Auremina and the hills. If they take the bait, their armor goes north, and Zille is left with " +
            "its garrison. Each feint costs us a day or two, and they'll use it to bring a few more men across the strait.\"",
            new[] { "PlanSet" }, condition: Obj(Objective.Harbor));
        t.Line("FeintOther", Carlos,
            "\"Each time, we make a show somewhere else first and see how much they leave behind. It costs us a day or two, and they'll use it to " +
            "bring a few more men across the strait.\"", new[] { "PlanSet" });

        t.Choice("TArmor", "\"The tanks lead. We break through and keep going.\"", new[] { "TArmor2" }, condition: canArmor,
            script: Set(VarApproach, "\"Armor\""));
        t.Line("TArmor2", Carlos,
            "\"The tanks lead, then. Their wall will hold us for a morning. After that, it's a race to the objective.\"", new[] { "PlanSet" });

        t.Choice("TLanding", "\"We use the sea. The marines go ashore behind their lines.\"", new[] { "TLanding2" }, condition: canLanding,
            script: Set(VarApproach, "\"Landing\""));
        Intel("TLanding2", "\"The landing ships go in while the army comes down the road, at Zille first and at Nanta after. If their coastal guns " +
                           "are still firing when the boats go in, we will lose ships.\"", new[] { "PlanSet" });

        t.Choice("TDrop", "\"The paratroopers go in first, behind their lines, the night before every attack.\"", new[] { "TDrop2" }, condition: canDrop,
            script: Set(VarApproach, "\"Airborne\""));
        Intel("TDrop2", "\"They'll be holding a patch of ground behind the Wehzeks by dawn. Each time, the army has to reach them before the Wehzeks do.\"",
            new[] { "PlanSet" });

        t.Choice("TMount", "\"The mountaineers lead. Over the ridges, wherever there are ridges.\"", new[] { "TMount2" }, condition: canMount,
            script: Set(VarApproach, "\"Mountain\""));
        t.Line("TMount2", Carlos,
            "\"They'll be on the high ground before the Wehzeks know they've left our lines. In the hills they'll be at home, and on the coast " +
            "they'll keep to the heights on the flanks.\"", new[] { "PlanSet" });

        t.Line("PlanSet", Carlos,
            "\"Then that is the plan for the campaign, Your Majesty, from the first shot to the last. The Staff will fit every phase to it.\"",
            new[] { "Fleet" });
    }

    /// <summary>Later councils: the war plan carried onto the next objective, in its nearest sensible form, and the Staff says so.</summary>
    private static void CarryOn()
    {
        var names = new List<string>();
        void C(string name, string speaker, string cond, string text)
        {
            names.Add(name);
            if (speaker == null)
            {
                Intel(name, text, new[] { "CarryAsk" }, cond);
            }
            else
            {
                t.Line(name, speaker, text, new[] { "CarryAsk" }, condition: cond);
            }
        }

        string adapt(string a) => Eq(VarAdapt, a);
        string units = adapt("units");
        C("CHills", Carlos, adapt("hills"),
            "\"Your plan was to let the tanks lead, Your Majesty, and in those hills they can't. The Staff will carry it on as far as the ground " +
            "allows: the infantry goes up the valley road first, with the armor close behind to finish what it finds.\"");
        C("CInland", null, adapt("inland"),
            "\"Auremina is a long way from the sea, so the landing can't go in on top of it. The marines will go ashore on the coast behind the " +
            "hills instead and make enough noise to drag their reserves down to meet them.\"");
        C("CUnitsArmor", Carlos, And(units, PlanIs(Approach.Armor)),
            "\"We haven't enough tanks left for another armored thrust, Your Majesty. The Staff will carry your plan on as a straight push down " +
            "the road, with what armor we have in front.\"");
        C("CUnitsLanding", null, And(units, PlanIs(Approach.Landing)),
            "\"We no longer have the marines and the ships for a landing, Your Majesty. The army goes in by land, and the Staff will make the " +
            "most of it.\"");
        C("CUnitsDrop", null, And(units, PlanIs(Approach.Airborne)),
            "\"The paratroopers are spent, Your Majesty. There is nobody left to drop. The Staff will carry the plan on as a direct assault.\"");
        C("CUnitsMount", Carlos, And(units, PlanIs(Approach.Mountain)),
            "\"The mountaineers are spent, Your Majesty. The ridges will be ordinary infantry work this time, and the plan becomes a direct assault.\"");
        C("CFeintWall", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Wall)),
            "\"We feint first, as you ordered at the start. This time the noise goes toward the coast road to Isor, and they'll have to pull men " +
            "out of the hills to cover it.\"");
        C("CFeintPorts", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Ports)),
            "\"We feint first, as always. A great deal of noise in front of Nanta, and we'll see how much they leave behind in Isor.\"");
        C("CFeint", Carlos, FormIs(Approach.Feint),
            "\"We feint first, as you ordered. A show toward Auremina and the hills, and if they take the bait, Zille is left with its garrison.\"");
        C("CArmorPorts", Carlos, And(FormIs(Approach.Armor), Obj(Objective.Ports)),
            "\"Out of the hills, the tanks lead again, as you ordered. The coast road to Isor and Nanta is good tank country.\"");
        C("CArmor", Carlos, FormIs(Approach.Armor),
            "\"The tanks lead, as you ordered. Massed on the river road, straight for the harbor.\"");
        C("CLandingPorts", null, And(FormIs(Approach.Landing), Obj(Objective.Ports)),
            "\"The landing goes in as you planned it, Your Majesty. The marines come ashore between Isor and Nanta and cut the coast road.\"");
        C("CLanding", null, FormIs(Approach.Landing),
            "\"The landing goes in as you planned it. The marines go into the bay while the army comes down the river.\"");
        C("CDropWall", null, And(FormIs(Approach.Airborne), Obj(Objective.Wall)),
            "\"The paratroopers go in the night before, as planned. This time on the ridges behind Auremina, to hold the road until the army comes up.\"");
        C("CDropPorts", null, And(FormIs(Approach.Airborne), Obj(Objective.Ports)),
            "\"The paratroopers go in the night before, as planned. They'll drop between Isor and Nanta and cut the coast road.\"");
        C("CDrop", null, FormIs(Approach.Airborne),
            "\"The paratroopers go in the night before, as planned. Behind the city, not on it. The anti-aircraft guns in Zille would tear them apart.\"");
        C("CMountWallT", Taddeus, And(FormIs(Approach.Mountain), Obj(Objective.Wall), TaddeusHere),
            "\"This is the ground they were trained for, Lieutenant. Over the ridges while the Wehzeks watch the roads.\"");
        C("CMountWall", Carlos, And(FormIs(Approach.Mountain), Obj(Objective.Wall)),
            "\"This is the ground the mountaineers were trained for, Your Majesty. They go over the ridges while the Wehzeks watch the roads.\"");
        C("CMount", Carlos, FormIs(Approach.Mountain),
            "\"The mountaineers keep to your plan where the ground lets them: the high ground on the flanks, while the army goes down the road below.\"");
        C("CDirectAgain", Carlos, And(Obj(Objective.Harbor), Fought),
            "\"The same plan, Your Majesty, and the same road. Straight at Zille, harder and faster than last time.\"");
        C("CDirectWall", Carlos, Obj(Objective.Wall),
            "\"The plan stays as you set it, Your Majesty. Straight up the valley road, as fast as the hills allow.\"");
        C("CDirectPorts", Carlos, Obj(Objective.Ports),
            "\"Straight at them again, as you ordered at the start. Down the coast road to Isor, and on to Nanta.\"");
        C("CDirect", Carlos, null,
            "\"The plan stays as you set it at the start, Your Majesty. Straight at them and fast, before their armor can come up.\"");
        t.Logic("Carry", names.ToArray());

        t.Logic("CarryAsk", new[] { "CarryAsk3", "CarryAsk2" });
        t.Line("CarryAsk3", Carlos, "\"Is there anything you want to go over before the last orders of this campaign go out?\"",
            new[] { "CarryAskYes", "CarryAskNo" }, condition: $"{Num(VarPhase)} >= 3");
        t.Line("CarryAsk2", Carlos, "\"Is there anything you want to go over before the orders go out?\"", new[] { "CarryAskYes", "CarryAskNo" });
        t.Choice("CarryAskYes", "\"Take me through where we stand.\"", new[] { "Hub" });
        t.Choice("CarryAskNo", "\"No. Go on.\"", new[] { "Fleet" });
    }

    /// <summary>The Aureus squadron, the fleet and the submarines (SupplyWarRoom.cs), then the barrage.</summary>
    private static void FleetAndBarrage()
    {
        string ships = Num("RiziaDLC.Army_Unit_Ship_Total");
        t.Logic("Fleet", new[] { "FleetAsk", "Navy" });
        Intel("FleetAsk",
            "\"Then there is the fleet. Half our destroyers are still sitting on the Aureus field, babysitting a Palesian drillship.\"",
            new[] { "FleetAsk2" }, And(FleetSplit, $"{ships} >= 1"));
        Intel("FleetAsk2", "\"Bring them north, and the Wehzek navy loses the gulf. Leave them, and the Duke's drillship stays idle.\"",
            new[] { "FleetNorth", "FleetStay" });
        t.Choice("FleetNorth", "\"Bring the squadron north. Zille comes first.\"", new[] { "FleetNorth2" }, cost: "-1 Authority",
            script: Set(VarFleetRecalled, "true") + Set("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship", "false") +
                    Set("RiziaDLCSupport.Journal_Turn04_Pales_NavalWithdrawal", "true") + Set(SupplyWar.VarAureusRecalled, "true") +
                    $"{V("RiziaDLC.Resources_Authority")} = {Num("RiziaDLC.Resources_Authority")} - 1; " +
                    $"{V(SupplyWar.NpPales)} = {Num(SupplyWar.NpPales)} + 1; {V(SupplyWar.NpLespia)} = {Num(SupplyWar.NpLespia)} + 1; ");
        Intel("FleetNorth2", "\"I'll send the signal tonight. Reinhart will have his drillship working by the end of the week, and he'll thank no one for it.\"",
            new[] { "Navy" });
        t.Choice("FleetStay", "\"The blockade stays. We fight Wehlen with what we have.\"", new[] { "FleetStay2" });
        Intel("FleetStay2", "\"As you wish. Their ships will have a little more room in the gulf.\"", new[] { "Navy" });

        // The fleet's opening posture at the first council; its current orders, to keep or change, at the later ones.
        string landing = $"(({First}) and {PlanIs(Approach.Landing)}) or (({Later}) and {FormIs(Approach.Landing)})";
        SupplyWar.CouncilNavy(t, "Navy", "Barrage", LucitaHere, Is(VarCoastal), Obj(Objective.Harbor), Obj(Objective.Ports),
            landing, AskedNavyWhy, Later);

        // The barrage: the King's own call, every operation (it costs shells each time).
        t.Logic("Barrage", new[] { "BarrageAsk", "Close" });
        t.Logic("BarrageAsk", new[] { "BarrageAskFirst", "BarrageAskLater" }, condition: Equip(BarrageCost));
        t.Line("BarrageAskFirst", Carlos,
            "\"Last of all, the artillery. We can give their forward positions a full night's barrage before the infantry goes in. It will cost " +
            "us shells we may want later, and it won't be kind to the villages along the line.\"",
            new[] { "BarrageYes", "BarrageNo" }, condition: First);
        t.Logic("BarrageAskLater", new[] { "BarrageAskLast", "BarrageAskAgain", "BarrageAskSaved" });
        t.Line("BarrageAskLast", Carlos,
            "\"Last of all, the guns. There is no sense saving shells for a phase that won't come. A full night's barrage before the infantry " +
            "goes in, if you want one.\"", new[] { "BarrageYes", "BarrageNo" }, condition: $"{Num(VarPhase)} >= 3");
        t.Line("BarrageAskAgain", Carlos,
            "\"And the guns again. The Wehzeks have learned to sleep in their deepest bunkers since the last barrage, but it still costs them " +
            "every gun they leave above ground. Another night of it?\"", new[] { "BarrageYes", "BarrageNo" }, condition: $"{Num(VarBarrages)} >= 1");
        t.Line("BarrageAskSaved", Carlos,
            "\"And the guns. We held them back last time, so the gunners have the shells for a full night's barrage now, if you want one.\"",
            new[] { "BarrageYes", "BarrageNo" });
        t.Choice("BarrageYes", "\"Open fire the night before. Spare nothing.\"", new[] { "BarrageYes2" }, cost: $"-{BarrageCost} Military Equipment",
            script: Set(VarBombard, "true") + $"{V(VarBarrages)} = {Num(VarBarrages)} + 1; " +
                    $"{V("RiziaDLC.Resources_Military_Equipment")} = {Num("RiziaDLC.Resources_Military_Equipment")} - {BarrageCost}; ");
        t.Line("BarrageYes2", Carlos, "\"Understood. The guns will be in position by nightfall.\"", new[] { "Close" });
        t.Choice("BarrageNo", "\"No barrage. We may need those shells, and the people in those villages are ours.\"", new[] { "BarrageNo2" });
        t.Line("BarrageNo2", Carlos, "\"Very well. The infantry will do it the hard way.\"", new[] { "Close" });
    }

    private static string Met => $"{V(VarMeetings)} = {Num(VarMeetings)} + 1; ";

    private static void Closing()
    {
        t.Logic("Close", new[] { "CloseFirst", "CloseLater" });
        t.Line("CloseFirst", Carlos, "\"We will relay your orders immediately, Your Majesty.\"", new[] { "Close2" }, condition: First);
        t.Line("Close2", Narrator, "The generals in the room gave me a military salute and answered as one. \"Yes, sir.\"",
            new[] { "CloseA", "CloseB", "CloseC" });
        t.Choice("CloseA", "\"Good luck to us all.\"", new[] { "CloseA2" });
        Intel("CloseA2", "\"Good luck to all of us, Your Majesty.\"", new[] { "Close3" });
        t.Choice("CloseB", "\"For king and country.\"", new[] { "CloseB2" });
        t.Line("CloseB2", Carlos, "\"For king and country.\"", new[] { "Close3" });
        t.Choice("CloseC", "\"Do not fail me, General.\"", new[] { "CloseC2" });
        t.Line("CloseC2", Carlos, "\"We won't, Your Majesty.\"", new[] { "Close3" });
        Intel("Close3", "\"I will come to you as soon as we have news from the front.\"", new[] { "Close4" });
        t.Line("Close4", Narrator, "And there it was. The war for Zille was about to begin.", new[] { "Order" });

        string lastPhase = $"{Num(VarPhase)} >= {ZilleCampaign.LastPhase}";
        Pick("CloseLater", Carlos, "CloseLater2",
            (lastPhase, "\"The orders go out tonight. This is the last phase of Northern Crown, Your Majesty. Whatever happens, the next time we " +
                        "meet in this room, it will be to talk about the peace.\""),
            (Obj(Objective.Ports), "\"The orders go out tonight, Your Majesty. If the ports fall, the next time we meet in this room, it will be to talk about the peace.\""),
            (And(Obj(Objective.Harbor), Fought), "\"The orders go out tonight, Your Majesty. Zille again, and this time the men know the streets.\""),
            (Obj(Objective.Wall), "\"The orders go out tonight. By morning the columns will be on the valley road.\""),
            (null, "\"The orders go out tonight, Your Majesty. I'll come to you as soon as we have news.\""));
        Pick("CloseLater2", Narrator, "Order",
            (lastPhase, "That night, the trains to the north ran without lights, and nobody in Palas Rezna slept much."),
            (And(Obj(Objective.Harbor), Fought),
                "On my way out, I saw that someone had drawn a small circle around Zille on the map in pencil, and then gone over it again in ink."),
            (Obj(Objective.Wall), "On my way out, I saw a young captain lean over the map and move a golden token an inch toward Auremina."),
            (Obj(Objective.Ports), "That night, the trains to the north ran without lights."),
            (null, "Somewhere north of Dapalle, the guns were already being moved forward."));
        t.Logic("Order", new[] { "End" },
            script: Set(VarOrderTurn, V(VarTurn)) + Set(VarDoneTurn, V(VarTurn)) + Set(WarFlow.VarCouncilHeld, "true") + Met);
    }

    /// <summary>Later councils: how the war plan worked in the operation just fought, tied to the result.</summary>
    private static void Assessment()
    {
        string good = And($"{Num(VarLossTier)} == 0", IsNot(VarSetback), Is(VarLastWon));
        string again = $"{Num(VarFought)} >= 2";   // the second assessment of the same plan says something new
        var names = new List<string>();
        void A(string name, string speaker, string cond, string text)
        {
            names.Add(name);
            if (speaker == null)
            {
                Intel(name, text, new[] { "Push" }, cond);
            }
            else
            {
                t.Line(name, speaker, text, new[] { "Push" }, condition: cond);
            }
        }

        string last(string a) => Eq(VarLastAdapt, a);
        A("AsHills", Carlos, last("hills"),
            "\"In the hills the tanks could only follow the infantry, as we expected. Out on the coast road they'll lead again.\"");
        A("AsInland", null, last("inland"),
            "\"The marines' landing on the coast pulled their reserves down out of the hills, as we hoped. They're watching every beach now.\"");
        A("AsUnits2", Carlos, And(last("units"), again),
            "\"It was the plain way again, straight down the road. Until the units your plan needs are rebuilt, it will stay that way.\"");
        A("AsUnits", Carlos, last("units"),
            "\"We fought it the plain way last time, straight down the road. There was nothing left to fight it any other way.\"");
        A("AsFeint2G", Carlos, And(PlanIs(Approach.Feint), good, again),
            "\"Your feint worked again, Your Majesty. They changed their generals after the first one, but not their habits.\"");
        A("AsFeint2", Carlos, And(PlanIs(Approach.Feint), again),
            "\"The feint fooled nobody this time. They have learned to count our columns before they move. The Staff will make the next " +
            "one louder, and earlier.\"");
        A("AsFeintG", Carlos, And(PlanIs(Approach.Feint), good),
            "\"Your feint worked, Your Majesty. Their reserves spent two days marching to defend a town we never meant to take. Smolak's " +
            "generals will be wiser next time, so the Staff will change the bait, not the plan.\"");
        A("AsFeint", Carlos, PlanIs(Approach.Feint),
            "\"The feint bought us less than we hoped. Their reserves hesitated, but not for long. Still, it keeps their armor guessing, and the plan holds.\"");
        A("AsArmor2G", Carlos, And(PlanIs(Approach.Armor), good, again),
            "\"The tanks did it again. Once they are out in front, the Wehzek infantry stops digging and starts walking.\"");
        A("AsArmor2", Carlos, And(PlanIs(Approach.Armor), again),
            "\"The tanks are worn down to the tracks, Your Majesty. The fitters work through the night, and we still have fewer of them in " +
            "front than we did last month.\"");
        A("AsArmorG", Carlos, And(PlanIs(Approach.Armor), good),
            "\"The tanks did what you asked of them. Once they were through the wall, nothing the Wehzeks had could stand in front of them for long.\"");
        A("AsArmor", Carlos, PlanIs(Approach.Armor),
            "\"The tanks got through, but we left a good many of them burning by the road. The Wehzeks have learned to wait for the infantry behind them.\"");
        A("AsLanding2G", null, And(PlanIs(Approach.Landing), good, again),
            "\"The marines went ashore again and found the beaches emptier than the first time. They are running out of men to watch every cove.\"");
        A("AsLanding2", null, And(PlanIs(Approach.Landing), again),
            "\"The second landing was dearer than the first. They had moved guns onto every headland we used before.\"");
        A("AsLandingG", null, And(PlanIs(Approach.Landing), good),
            "\"The landing caught them facing the wrong way. They are still pulling battalions off the line to watch the beaches.\"");
        A("AsLanding", null, PlanIs(Approach.Landing),
            "\"The landing worked, but the beaches were expensive. Every Wehzek battery on that coast now knows what our boats look like.\"");
        A("AsDrop2G", null, And(PlanIs(Approach.Airborne), good, again),
            "\"The paratroopers held again until the columns reached them. Every battalion the Wehzeks keep back to watch the sky is one " +
            "missing from their front, and it shows.\"");
        A("AsDrop2", null, And(PlanIs(Approach.Airborne), again),
            "\"The paratroopers were alone too long again. The men who came back have stopped joking about the jump.\"");
        A("AsDropG", null, And(PlanIs(Approach.Airborne), good),
            "\"The paratroopers held until the columns reached them, just as planned. The Wehzeks now keep a battalion behind every line, watching the sky.\"");
        A("AsDrop", null, PlanIs(Approach.Airborne),
            "\"The paratroopers held, but they were alone longer than anyone wanted. We lost good men waiting for the columns.\"");
        A("AsMount2G", Carlos, And(PlanIs(Approach.Mountain), good, again),
            "\"The mountaineers were over the ridges again before the Wehzeks looked up. The Wehzeks know their badge by now, Your Majesty, " +
            "and they still can't stop them.\"");
        A("AsMount2", Carlos, And(PlanIs(Approach.Mountain), again),
            "\"The mountaineers took the heights again, and paid for them again. There are fewer of them every time the Staff counts.\"");
        A("AsMountGT", Taddeus, And(PlanIs(Approach.Mountain), good, TaddeusHere),
            "\"Over the ridges, Lieutenant, and they never once looked up. I told you they wouldn't.\"");
        A("AsMountG", Carlos, And(PlanIs(Approach.Mountain), good),
            "\"The mountaineers were on the ridges before the Wehzeks knew they had left our lines. Your plan suits this country, Your Majesty.\"");
        A("AsMount", Carlos, PlanIs(Approach.Mountain),
            "\"The mountaineers took the ridges, but the Wehzek positions below them made them pay for every one.\"");
        A("AsDirect2G", Carlos, And(good, again),
            "\"Straight at them again, and it held. Their armor still comes up a day late. I think they have stopped expecting to arrive in time.\"");
        A("AsDirect2", Carlos, again,
            "\"The direct road again, and the bill again at the head of every column. They mine the obvious roads now, Your Majesty, and " +
            "every road we take is obvious.\"");
        A("AsDirectG", Carlos, good,
            "\"Straight at them and fast, as you ordered. Their armor still arrives a day after we do, and the men have stopped expecting it sooner.\"");
        A("AsDirect", Carlos, null,
            "\"Going straight at them works, Your Majesty, but it is paid for at the head of every column. They know now which road we'll take.\"");

        t.Logic("Assess", new[] { "AssessGo", "Push" });
        t.Logic("AssessGo", names.ToArray(), condition: And(Later, Fought, IsNot(VarWaited)));
    }

    /// <summary>One matter per progress meeting, in rotation: the men's rest, their morale, the supply roads, the reserves.</summary>
    private static void Matters()
    {
        string m(string x) => Eq(VarMatter, x);
        t.Logic("Matter", new[] { "MRest", "MMorale", "MSupply", "MReinforcePales", "MReinforce", "Obj" });
        t.Line("MRest", Carlos,
            "\"The month's rest did the men good, Your Majesty. The mail caught up with them, and the field kitchens found something other than beans.\"",
            new[] { "Obj" }, condition: m("Rest"));

        t.Line("MMorale", Carlos,
            "\"One thing before the map, Your Majesty. The divisions that went in first have been in the line since the declaration. The " +
            "chaplains tell me the letters home have changed their tone.\"", new[] { "MMoraleA", "MMoraleB" }, condition: m("Morale"));
        t.Choice("MMoraleA", "\"Rotate them out. Fresh divisions lead this time.\"", new[] { "MMoraleA2" });
        t.Line("MMoraleA2", Carlos, "\"It will cost us a day on the roads, but the men will know someone is counting. I'll see to it.\"", new[] { "Obj" });
        t.Choice("MMoraleB", "\"Not yet. Tell them the war ends sooner if we keep moving.\"", new[] { "MMoraleB2" });
        t.Line("MMoraleB2", Carlos, "\"I'll tell them, Your Majesty. They would rather hear it from you, but they'll take it from me.\"", new[] { "Obj" });

        t.Line("MSupply", Carlos,
            "\"The quartermasters have a complaint, and for once it's a fair one. The Wehzeks blew every bridge behind them, and every mile we " +
            "advance is another mile for the fuel trucks.\"", new[] { "MSupplyA", "MSupplyB" }, condition: m("Supply"));
        t.Choice("MSupplyA", "\"Put the engineers on the bridges first. The army moves when the road does.\"", new[] { "MSupplyA2" });
        t.Line("MSupplyA2", Carlos, "\"Then the engineers go in behind the first wave. The bridges will be up before the trucks need them.\"", new[] { "Obj" });
        t.Choice("MSupplyB", "\"Use the Wehzek depots we've taken.\"", new[] { "MSupplyB2" });
        t.Line("MSupplyB2", Carlos, "\"There is Wehzek fuel in the railway yards, and it burns just as well in our tanks. I'll have it counted tonight.\"",
            new[] { "Obj" });

        t.Line("MReinforcePales", Carlos,
            "\"The reserve classes have finished their training at Balle, Your Majesty, but half of them are already on the trains south to the " +
            "Palesian border. Zille gets what is left.\"", new[] { "MReinA", "MReinB" },
            condition: And(m("Reinforce"), "Variable[\"RiziaDLC.Pales_War\"] == true"));
        t.Line("MReinforce", Carlos,
            "\"The reserve classes called up at the declaration have finished their training at Balle. Eleven thousand men, and most of them " +
            "have never heard a shot fired.\"", new[] { "MReinA", "MReinB" }, condition: m("Reinforce"));
        t.Choice("MReinA", "\"Put them in the quiet sectors first. Let them learn before they bleed.\"", new[] { "MReinA2" });
        t.Line("MReinA2", Carlos, "\"The veterans will be glad of the relief. I'll see it done.\"", new[] { "Obj" });
        t.Choice("MReinB", "\"Send them where they're needed. They'll learn fast.\"", new[] { "MReinB2" });
        t.Line("MReinB2", Carlos, "\"They will, Your Majesty. Some of them only once.\"", new[] { "Obj" });
    }
}
