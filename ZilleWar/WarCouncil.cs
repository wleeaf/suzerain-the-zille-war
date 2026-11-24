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
    private static readonly string[] PrePrefixes = { "ZilleWar.Pre_" };

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

    // ---- what the phase just fought really did, city by city (ZilleCampaign's per-phase record; Prepare sets them)
    private const string VarLastResult = "ZilleWar.Council_LastResult";    // won | partial | other (aim missed, a city won elsewhere) | missed
    private const string VarLastAim = "ZilleWar.Council_LastAim";          // the aim as it stood: "Isor and Nanta", "Isor", "Zille"
    private const string VarLastPart = "ZilleWar.Council_LastPart";        // partial: the aim's city taken ("Nanta")
    private const string VarExtraList = "ZilleWar.Council_ExtraList";      // Zille cities won outside the aim: "Auremina and Nanta"
    private const string VarLostZList = "ZilleWar.Council_LostZList";      // Zille cities lost back to Wehlen: "Zille"
    private const string VarGainedAny = "ZilleWar.Council_GainedAny";      // any city won (Rizian towns retaken included)
    private const string VarLostAny = "ZilleWar.Council_LostAny";          // any city lost
    private const string VarLastGround = "ZilleWar.Council_LastGround";    // gained | lost | none (districts, net)
    private const string VarSetbackWhy = "ZilleWar.Council_SetbackWhy";    // town (a Rizian town held by Wehlen) | city (a Zille city lost) | men
    private const string VarNextAim = "ZilleWar.Council_NextAim";          // the next aim's name now: "Isor" once Nanta is ours
    private const string VarNextRetake = "ZilleWar.Council_NextRetake";    // the next aim is a city we took once and lost
    private const string VarPortsLeft = "ZilleWar.Council_PortsLeft";      // the ports Wehlen holds: "Isor and Nanta" | "Isor" | "Nanta" | ""
    private const string VarLandedAt = "ZilleWar.Council_LandedAt";        // where Wehzek reinforcements came ashore before the last phase
    // ---- the staff briefing's picture of the moment (Prepare sets them)
    private const string VarHq = "ZilleWar.Council_Hq";                    // where Wehlen's command sits now: the first Wehzek city of Zille, Auremina, Nanta, Isor
    private const string VarZilleBack = "ZilleWar.Council_ZilleBack";      // Zille city was ours once and Wehlen holds it again
    private const string VarWallBroken = "ZilleWar.Council_WallBroken";    // we hold a tile of Wehlen's old border wall
    private const string VarMoridia = "ZilleWar.Council_MoridiaOurs";      // Fort Moridia (tile only) is Rizian
    private const string VarOurDead = "ZilleWar.Council_OurDead";          // formations we lost in the phase just fought
    private const string VarTheirDead = "ZilleWar.Council_TheirDead";      // formations Wehlen lost in it
    private const string VarHeldList = "ZilleWar.Council_HeldList";        // the Zille cities we hold: "Zille and Nanta"
    private const string VarLeftList = "ZilleWar.Council_LeftList";        // the Zille cities Wehlen holds
    private const string VarOccupied = "ZilleWar.Council_Occupied";        // our own cities Wehlen holds: "Mave Nortus and Dapalle"
    private static string LostNowVar(string city) => $"ZilleWar.Council_LostNow_{city.Replace(" ", string.Empty)}";
    private static string BackVar(string city) => $"ZilleWar.Council_Retaken_{city.Replace(" ", string.Empty)}";
    private static readonly string[] ZilleCities = AfterAction.ZilleCities;
    private static readonly string[] AimNames = { "Zille", "Auremina", "Isor and Nanta", "Isor", "Nanta" };

    // ---- asked this council (reset at start)
    private const string AskedForces = "ZilleWar.Council_Asked_Forces";
    private const string AskedGarrison = "ZilleWar.Council_Asked_Garrison";
    private const string AskedFronts = "ZilleWar.Council_Asked_Fronts";
    private const string AskedSea = "ZilleWar.Council_Asked_Sea";
    private const string AskedNext = "ZilleWar.Council_Asked_Next";
    private const string AskedLast = "ZilleWar.Council_Asked_Last";
    private const string AskedWhy = "ZilleWar.Council_Asked_Why";
    private const string AskedLine = "ZilleWar.Council_Asked_Line";
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
        LapseStandingOrders();
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

    /// <summary>A loaded save starts the turn afresh: its war table may need injecting again.</summary>
    internal static void ForgetInjections() => Reinjected.Clear();

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
                     AskedForces, AskedGarrison, AskedFronts, HubVisited, AskedSea, AskedNext, AskedLast, AskedWhy, AskedLine,
                     VarHq, VarZilleBack, VarWallBroken, VarMoridia, VarOurDead, VarTheirDead, VarHeldList, VarLeftList, VarOccupied,
                     VarLastResult, VarLastAim, VarLastPart, VarExtraList, VarLostZList, VarGainedAny, VarLostAny, VarLastGround,
                     VarSetbackWhy, VarNextAim, VarNextRetake, VarPortsLeft, VarLandedAt,
                 }.Concat(RizianCities.Select(LostVar)).Concat(RizianCities.Concat(ZilleCities).SelectMany(c => new[] { LostNowVar(c), BackVar(c) })))
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

            int turn = GameState.CurrentTurnNum ?? 0, step = GameState.CurrentStepNum ?? 0;
            // Past the war's last step WarFlow ends it after this evaluation (ForceEnd): never open a table first.
            if (turn <= 0 || turn >= 7 || (turn == 6 && step >= 6))
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
        if (Scheduler.HasQueued(name))
        {
            return;     // already offered, waiting for the calls it follows
        }

        try
        {
            GameState.AddCustomConversation(Conv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(name, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                first ? "Briefing on the Liberation of Zille" : $"{ZilleCampaign.PhaseTitle(ZilleCampaign.PhaseForTurn(turn))} Briefing",
                "Council Chambers, Palas Rezna", ConversationData.ConversationType.SecurityAndOrder, Conv));
            Core.Log.Msg($"War Council offered: {name} ({(first ? "briefing" : "next steps")}, phase {ZilleCampaign.PhaseForTurn(turn)}, aim {ZilleCampaign.AimName(next)}).");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the War Council failed: {ex}");
        }
    }

    /// <summary>
    /// The council is opening (ConversationPanel.Setup): read the campaign state again, so the calls played after it was
    /// offered (Hegel's word on the submarines, ships lost, a held month) are what the council talks about.
    /// </summary>
    internal static void OnOpened(string fragmentName)
    {
        try
        {
            int turn = GameState.CurrentTurnNum ?? 0;
            if (fragmentName == null || !fragmentName.StartsWith(FragmentPrefix, StringComparison.Ordinal) || turn <= 0 ||
                !WarFlow.AtWar || Variables.GetInt(VarDoneTurn) >= turn || ZilleCampaign.Available().Count == 0)
            {
                return;
            }

            bool first = !Variables.GetBool(WarFlow.VarCouncilHeld) && ZilleCampaign.OperationsDone == 0;
            Prepare(turn, first, ZilleCampaign.Info(ZilleCampaign.NextObjective));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Preparing the War Council failed: {ex.Message}");
        }
    }

    /// <summary>Standing naval orders lapse when the ships for them are gone, or Hegel has the submarines' word.</summary>
    private static void LapseStandingOrders()
    {
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
        Variables.Set(VarNextAim, ZilleCampaign.AimName(next, owners));
        Variables.Set(VarPortsLeft, string.Join(" and ", ZilleCampaign.TownsLeft(ZilleCampaign.Info(Objective.Ports), owners)));
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

        // What the phase just fought really did: the aim taken whole, in part, missed with a city won elsewhere, or missed;
        // the cities won and lost; the ground.
        ObjectiveInfo lastInfo = Enum.TryParse(lastObjective, out Objective lo) ? ZilleCampaign.Info(lo) : null;
        bool lastWon = Variables.GetBool(VarLastWon);
        (List<string> gained, List<string> lost, string lastAim, int tilesGained, int tilesLost) = LastPhaseFacts(k, lastInfo, lastWon);
        string[] aimTowns = lastAim.Split(" and ", StringSplitOptions.RemoveEmptyEntries);
        List<string> aimPart = gained.Where(aimTowns.Contains).ToList();
        List<string> extras = ZilleCities.Where(c => gained.Contains(c) && !aimTowns.Contains(c)).ToList();
        Variables.Set(VarLastAim, lastAim);
        Variables.Set(VarLastResult, k <= 0 ? string.Empty : lastWon ? "won" : aimPart.Count > 0 ? "partial" : gained.Count > 0 ? "other" : "missed");
        Variables.Set(VarLastPart, ZilleCampaign.JoinNames(aimPart));
        Variables.Set(VarExtraList, ZilleCampaign.JoinNames(extras));
        Variables.Set(VarLostZList, ZilleCampaign.JoinNames(ZilleCities.Where(lost.Contains).ToList()));
        Variables.Set(VarGainedAny, gained.Count > 0);
        Variables.Set(VarLostAny, lost.Count > 0);
        Variables.Set(VarLastGround, tilesLost > tilesGained ? "lost" : tilesGained == 0 && tilesLost == 0 ? "none" : "gained");
        foreach (string c in RizianCities.Concat(ZilleCities))
        {
            Variables.Set(LostNowVar(c), lost.Contains(c));
            Variables.Set(BackVar(c), gained.Contains(c) && RizianCities.Contains(c));
        }

        // A city we took once and lost again is a retaking, not a first attempt.
        var lostEver = new HashSet<string>(lost);
        for (int j = 1; j < k; j++)
        {
            lostEver.UnionWith(ZilleCampaign.Lost(j));
        }

        Variables.Set(VarNextRetake, k > 0 && ZilleCampaign.TownsLeft(next, owners).Any(lostEver.Contains));
        string landedAt = ZilleCampaign.Str(PhaseResume.VarLandedAt);
        Variables.Set(VarLandedAt, landedAt);

        // The staff briefing's picture: the line as it stands, Wehlen's command, the old wall, the last phase's dead.
        bool ours(string c) => Variables.GetBool(ZilleOperation.CapturedVar(c));
        Variables.Set(VarHeldList, ZilleCampaign.JoinNames(ZilleCities.Where(ours).ToList()));
        Variables.Set(VarLeftList, ZilleCampaign.JoinNames(ZilleCities.Where(c => !ours(c)).ToList()));
        Variables.Set(VarOccupied, ZilleCampaign.JoinNames(RizianCities.Where(c => Variables.GetBool(LostVar(c))).ToList()));
        Variables.Set(VarHq, new[] { "Zille", "Auremina", "Nanta", "Isor" }.FirstOrDefault(c => !ours(c)) ?? string.Empty);
        Variables.Set(VarZilleBack, k > 0 && lostEver.Contains("Zille") && !ours("Zille"));
        Variables.Set(VarWallBroken, ZilleData.Tiles.Any(x => x.Region == "zille" && x.Type == "Land" &&
            x.Neighbors.Any(nb => ZilleData.ByName.TryGetValue(nb, out TileSpec r) && r.Region == "rizia" && r.Type == "Land") &&
            owners.TryGetValue(x.Name, out bool w) && w));
        string moridia = ZilleCampaign.TownTile("Fort Moridia");
        Variables.Set(VarMoridia, moridia == null || !ZilleCampaign.IsEnemy(owners, moridia));
        Variables.Set(VarOurDead, playerDead);
        Variables.Set(VarTheirDead, enemyDead);

        bool lostOurOwn = RizianCities.Any(c => Variables.GetBool(LostVar(c)));
        bool lostCity = lost.Any(ZilleCities.Contains);
        Variables.Set(VarSetback, !first && (lostOurOwn || lostCity || tier == 2));
        Variables.Set(VarSetbackWhy, lostOurOwn ? "town" : lostCity ? "city" : "men");
        Variables.Set(VarExtraTaken, (lastWon || aimPart.Count > 0) && extras.Count > 0);

        // The war plan, carried onto the next objective (and how it was carried onto the last one).
        Approach form = Form(WarPlan, next.Id, out string adapt);
        Variables.Set(VarForm, form.ToString());
        Variables.Set(VarAdapt, adapt);
        Variables.Set(VarLastAdapt, k > 0 ? ZilleCampaign.Str(VarOpAdapt) : string.Empty);

        // One matter per progress meeting, in rotation, so no two meetings run the same way.
        int n = Math.Max(Variables.GetInt(VarMeetings), k);
        Variables.Set(VarMatter, Variables.GetBool(VarWaited) ? "Rest" : (n % 3) switch { 1 => "Morale", 2 => "Supply", _ => "Reinforce" });

        LapseStandingOrders();
    }

    /// <summary>
    /// The k-th phase's cities won and lost, its aim as it stood, and the districts won and lost. The cities and the aim come from
    /// ZilleCampaign's per-phase record through <see cref="Front.Of"/>, which also reads older saves; the districts come from the
    /// map the phase started from, while it is kept (until the next phase is set up).
    /// </summary>
    private static (List<string> gained, List<string> lost, string aim, int tilesGained, int tilesLost) LastPhaseFacts(int k, ObjectiveInfo info, bool won)
    {
        if (k <= 0)
        {
            return (new List<string>(), new List<string>(), string.Empty, 0, 0);
        }

        // The start map is the last phase's while no later phase has been set up.
        bool mapFits = ZilleCampaign.CurrentOperation().k == k;
        Dictionary<string, bool> before = ZilleCampaign.OwnersAtStart(), now = ZilleCampaign.CurrentOwners();
        int tg = 0, tl = 0;
        if (mapFits)
        {
            foreach ((string tile, bool ours) in now)
            {
                bool was = before.TryGetValue(tile, out bool w) && w;
                tg += ours && !was ? 1 : 0;
                tl += !ours && was ? 1 : 0;
            }
        }

        // The cities and the aim as every other reader sees them (Front reads an older save's phases the same way for all).
        Front.Phase ph = Front.Of(k);
        return ph != null
            ? (ph.Gained, ph.Lost, ph.Aim, tg, tl)
            : (new List<string>(), new List<string>(), info?.Target ?? string.Empty, tg, tl);
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

    /// <summary>Every non-empty subset of the items, each in the items' order.</summary>
    private static IEnumerable<List<string>> Subsets(IList<string> items)
    {
        for (int mask = 1; mask < 1 << items.Count; mask++)
        {
            yield return items.Where((_, i) => (mask & (1 << i)) != 0).ToList();
        }
    }

    /// <summary>A node-name-safe key for a city list ("Auremina and Nanta" becomes "AureminaNanta").</summary>
    private static string Key(string names) => new string(names.Replace(" and ", string.Empty).Where(char.IsLetter).ToArray());

    private static string Plural(string names) => names.Contains(" and ") ? "are" : "is";

    private static string Result(string r) => Eq(VarLastResult, r);
    private static string AimWas(string aim) => Eq(VarLastAim, aim);
    private static string AimIs(string aim) => Eq(VarNextAim, aim);

    // ------------------------------------------------------------------ the conversation

    private static void BuildConversation()
    {
        t = new Talk(Conv, "ZilleWar.Council.");
        string reset = Set(AskedForces, "false") + Set(AskedGarrison, "false") + Set(AskedFronts, "false") + Set(HubVisited, "false") +
                       Set(AskedSea, "false") + Set(AskedNext, "false") + Set(AskedLast, "false") + Set(AskedWhy, "false") + Set(AskedLine, "false") +
                       Set(VarBombard, "false") + Set(VarDelegated, "false") +
                       // The war plan and the fleet's opening posture are set at the first council only, then carried on. The plan
                       // is cleared, not defaulted: it is written only when the King chooses it, so a council that never reached
                       // the choice offers it again (ChoosePlan), and the operation reads an empty plan as Direct (WarPlan).
                       $"if {First} then " + Set(VarApproach, "\"\"") + Set(VarNavy, "\"\"") + Set(VarSubsOut, "false") + "end; ";
        string conversants =
            $"AddConversant(\"{Carlos}\"); " +
            $"if {LucitaHere} then AddConversant(\"{Lucita}\") end; " +
            $"if {TaddeusHere} then AddConversant(\"{Taddeus}\") end; " +
            $"if ({First} or ({T5Council})) then " +
            $"if {HugoHere} then AddConversant(\"{Hugo}\") end; if {ElenaHere} then AddConversant(\"{Elena}\") end; " +
            $"if {VinaHere} then AddConversant(\"{Vina}\") end; end; " +
            $"if {LaurentoHere} then AddConversant(\"{Laurento}\") end; ";
        t.Logic("Start", new[] { "Truce", "Open1", "Open2Quiet", "Open2Held" }.Concat(OpenMap()).ToArray(), hooks: AtStart(Conv), script: reset + conversants);

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

    /// <summary>
    /// The map at a progress meeting, as the last phase really left it: cities lost back, our own towns overrun, part of the aim
    /// taken, a city won off the plan, the aim missed (with ground gained, lost or neither), the hills taken, or a plain advance.
    /// Returns the lines' names in the order they are tried.
    /// </summary>
    private static IEnumerable<string> OpenMap()
    {
        var names = new List<string>();
        const string music = "PlaySceneMusic(Security);";
        string fought = And(Later, Fought, IsNot(VarWaited));
        void L(string name, string cond, string text)
        {
            names.Add(name);
            t.Line(name, Narrator, text, new[] { "Open2b" }, condition: cond == null ? fought : And(fought, cond), sequence: music);
        }

        foreach (string lost in Subsets(ZilleCities).Select(ZilleCampaign.JoinNames))
        {
            L($"Open2LostG{Key(lost)}", And(Eq(VarLostZList, lost), Is(VarGainedAny)),
                $"The map in the Council Chambers had been redrawn, and not all of it in our favor. Our golden tokens had gone forward in " +
                $"places, but silver ones stood in {lost} again.");
            L($"Open2Lost{Key(lost)}", Eq(VarLostZList, lost),
                $"The map in the Council Chambers had been redrawn against us. Silver tokens stood in {lost} again, and nobody in the room " +
                "was looking at that part of the table.");
        }

        string ourTownLost = string.Join(" or ", RizianCities.Select(c => $"({Is(LostNowVar(c))})"));
        L("Open2SetbackG", And(ourTownLost, Is(VarGainedAny)),
            "The map in the Council Chambers had been redrawn since I last saw it. Some of the golden tokens had gone forward. " +
            "Others had been swept off the table, and a few silver ones now stood on our side of the line.");
        L("Open2Setback", ourTownLost,
            "The map in the Council Chambers had been redrawn since I last saw it, and not in our favor. A few silver tokens now stood on " +
            "our side of the border.");
        L("Open2PartNanta", And(Result("partial"), AimWas("Isor and Nanta"), Eq(VarLastPart, "Nanta")),
            "On the map in the Council Chambers, a golden flag stood over Nanta at last. Isor, on the west coast, was still ringed with silver.");
        L("Open2PartIsor", And(Result("partial"), AimWas("Isor and Nanta"), Eq(VarLastPart, "Isor")),
            "On the map in the Council Chambers, a golden flag stood over Isor at last. Nanta, on the north coast, was still ringed with silver.");
        foreach (string extra in Subsets(ZilleCities).Select(ZilleCampaign.JoinNames))
        {
            L($"Open2Other{Key(extra)}", And(Result("other"), Eq(VarExtraList, extra)),
                $"The map in the Council Chambers had been redrawn, though not where the Staff had planned. Golden flags stood over {extra}, " +
                "but the objective itself was still ringed with silver.");
        }

        L("Open2OtherOwn", Result("other"),
            "The map in the Council Chambers had been redrawn. Our golden tokens had won back ground on our own side of the border, but the " +
            "objective itself was still ringed with silver.");
        L("Open2Extra", Is(VarExtraTaken),
            "The map in the Council Chambers had been redrawn twice since I last saw it: once for the objective, and once more for everything " +
            "our columns had taken beyond it.");
        L("Open2Pushed", And(Result("missed"), Eq(VarLastGround, "lost")),
            "The map in the Council Chambers had been redrawn, and not in our favor. The golden tokens stood farther back than I had left " +
            "them, and not one town had changed hands.");
        L("Open2Still", And(Result("missed"), Eq(VarLastGround, "none")),
            "The map in the Council Chambers had hardly changed since I last saw it. The front ran where it had run a month before, and every " +
            "town on it still flew the same flag.");
        L("Open2MissedZ", And(Result("missed"), Eq(VarLastObjective, "Harbor")),
            "The map in the Council Chambers had hardly changed. The golden tokens had reached the river below Zille, " +
            "and there they had stopped. The silver ones still ringed the city.");
        L("Open2MissedA", And(Result("missed"), Eq(VarLastObjective, "Wall")),
            "On the map in the Council Chambers, the golden tokens had climbed into the foothills and stopped short of Auremina. Someone had " +
            "drawn the new line in pencil, as if nobody quite trusted it yet.");
        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            L($"Open2MissedP{Key(aim)}", And(Result("missed"), AimWas(aim)),
                $"On the map in the Council Chambers, the golden tokens had come up the coast road and stopped short of {aim}. The silver ones " +
                $"still held {(aim.Contains(" and ") ? "both ports" : "the port")}.");
        }

        string wallWon = And(Result("won"), Eq(VarLastObjective, "Wall"));
        L("Open2Aure", And(wallWon, Eq(VarPortsLeft, "Isor and Nanta")),
            "Someone had moved the golden tokens up into the hills on the map in the Council Chambers. The silver ones that were left stood " +
            "along the northern coast, around Isor and Nanta.");
        L("Open2AureIsor", And(wallWon, Eq(VarPortsLeft, "Isor")),
            "Someone had moved the golden tokens up into the hills on the map in the Council Chambers. The silver ones that were left stood " +
            "on the west coast, around Isor.");
        L("Open2AureNanta", And(wallWon, Eq(VarPortsLeft, "Nanta")),
            "Someone had moved the golden tokens up into the hills on the map in the Council Chambers. The silver ones that were left stood " +
            "around Nanta, on the north coast.");
        names.Add("Open2");
        t.Line("Open2", Narrator,
            "The map in the Council Chambers had been redrawn since I last saw it. The golden tokens had crossed the border, and the " +
            "silver ones had been pushed back toward the northern coast.",
            new[] { "Open2b" }, sequence: music);
        return names;
    }

    private static void Arrival()
    {
        // The first council: straight back from Konstantium.
        t.Line("Open1", Narrator,
            "The flight back from Kyrute had been a quiet one, and the night at Qalus that followed was longer still. Nobody had wanted to be the first to say out loud what we had done.",
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
        // Each progress meeting finds the map changed in its own way: held, overrun, short of the aim, or past it.
        t.Line("Open2Held", Narrator,
            "Nothing on the map in the Council Chambers had moved in a month. Someone had dusted the tokens, and the golden ones still stood " +
            "exactly where the last phase had left them.",
            new[] { "Open2b" }, condition: And(Later, Fought, Is(VarWaited)), sequence: "PlaySceneMusic(Security);");
        // the King refused Smolak's ceasefire on the telephone earlier in this step (ReactionsHome)
        t.Logic("Open2b", new[] { "RefusedSmolak", "RefusedSmolakN", "Open2c" });
        t.Line("RefusedSmolak", Carlos, ReactionsHome.CouncilRefusalTook, new[] { "Open2c" },
            condition: And(ReactionsHome.LRefusedToday, ReactionsHome.LTookCity));
        t.Line("RefusedSmolakN", Carlos, ReactionsHome.CouncilRefusalNone, new[] { "Open2c" },
            condition: And(ReactionsHome.LRefusedToday, Not(ReactionsHome.LTookCity)));
        t.Logic("Open2c", new[] { "Open2Laurento3", "Open2Laurento", "Salute" });
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
            "\"Konstantium was rather less musical, Your Majesty. President Smolak walked out of the session, and the Security Committee sat until " +
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
        string won = Result("won"), missedAim = And(Not(Result("won")), Not(Result("partial")));
        string again = $"{Num(VarFought)} >= 2";
        var what = new List<string>();
        void R(string name, string cond, string text)
        {
            what.Add(name);
            t.Line(name, Carlos, text, new[] { "ResExtra" }, condition: cond);
        }

        // The whole aim taken.
        R("ResHarborLate", And(won, lastObj("Harbor"), again),
            "\"Zille is ours at last. It took two phases instead of one, but the harbor, the station and their headquarters are in our hands.\"");
        R("ResHarbor", And(won, lastObj("Harbor")),
            "\"Zille is ours. The harbor, the railway station and the Wehzek headquarters. Their whole southern front has lost its port.\"");
        string wallWon = And(won, lastObj("Wall"));
        R("ResWall", And(wallWon, Eq(VarPortsLeft, "Isor and Nanta")),
            "\"Auremina has fallen, and the gold hills with it. The roads north to Isor and Nanta are open.\"");
        R("ResWallIsor", And(wallWon, Eq(VarPortsLeft, "Isor")),
            "\"Auremina has fallen, and the gold hills with it. The road west to Isor is open, and Isor is the last port they have.\"");
        R("ResWallNanta", And(wallWon, Eq(VarPortsLeft, "Nanta")),
            "\"Auremina has fallen, and the gold hills with it. The road north to Nanta is open, and Nanta is the last port they have.\"");
        R("ResWallAll", wallWon, "\"Auremina has fallen, and the gold hills with it. The mines are ours.\"");
        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            R($"ResPorts{Key(aim)}", And(won, AimWas(aim)),
                $"\"{aim} {Plural(aim)} ours, Your Majesty. Wehlen has no port left on that coast to land its convoys.\"");
        }

        // Part of the aim.
        R("ResPartNanta", And(Result("partial"), AimWas("Isor and Nanta"), Eq(VarLastPart, "Nanta")),
            "\"Nanta is ours, Your Majesty, and with it the harbor where their convoys landed. Isor held out. Its garrison is still dug in on " +
            "the west coast.\"");
        R("ResPartIsor", And(Result("partial"), AimWas("Isor and Nanta"), Eq(VarLastPart, "Isor")),
            "\"Isor is ours, Your Majesty. Nanta held out, and its coastal guns are still firing. Their convoys still have one port to land at.\"");
        // The last port taken while the other one, ours already, was lost: the loss is told next.
        foreach (string port in new[] { "Isor", "Nanta" })
        {
            R($"ResPartOnly{port}", And(Result("partial"), Eq(VarLastPart, port)),
                $"\"{port} is ours, Your Majesty. The garrison gave up the port when our columns reached the quays.\"");
        }

        // The aim missed, but a city won off the plan.
        foreach (string aim in AimNames)
        {
            string[] aimTowns = aim.Split(" and ");
            foreach (string extra in Subsets(ZilleCities.Where(c => !aimTowns.Contains(c)).ToList()).Select(ZilleCampaign.JoinNames))
            {
                R($"ResOther{Key(aim)}_{Key(extra)}", And(Result("other"), AimWas(aim), Eq(VarExtraList, extra)),
                    $"\"We did not take {aim}, Your Majesty. But {extra} fell to us instead, and {(Plural(extra) == "are" ? "they are" : "it is")} ours now.\"");
            }
        }

        // The aim missed: the line pushed back, or not moved at all. A missed aim is a setback in a war that goes on.
        foreach (string aim in AimNames)
        {
            R($"ResBack{Key(aim)}", And(missedAim, AimWas(aim), Eq(VarLastGround, "lost")),
                $"\"We did not take {aim}, Your Majesty. Their counterattacks drove us back, and our line is behind where it stood a month ago.\"");
            R($"ResStill{Key(aim)}", And(Result("missed"), AimWas(aim), Eq(VarLastGround, "none")),
                $"\"We did not take {aim}, Your Majesty. The front has hardly moved, and not one town on it has changed hands.\"");
        }

        R("ResMissZ2", And(missedAim, lastObj("Harbor"), again),
            "\"We have failed to take Zille a second time, Your Majesty. Our lines are closer than they were, but the harbor and the station " +
            "are still theirs.\"");
        R("ResMissZ", And(missedAim, lastObj("Harbor")),
            "\"We did not take Zille. The columns reached the river crossings and the outskirts, but the harbor and the station are still in " +
            "Wehzek hands. The next phase goes in from the ground we hold.\"");
        R("ResMissA", And(missedAim, lastObj("Wall")),
            "\"We did not take Auremina. We are in the foothills and among the first mine heads, but the town and the ridges above it are still theirs.\"");
        R("ResMissPIsor", And(missedAim, AimWas("Isor")),
            "\"Isor is still theirs, Your Majesty. Our columns are on the coast road, but the port is still in Wehzek hands.\"");
        R("ResMissPNanta", And(missedAim, AimWas("Nanta")),
            "\"Nanta is still theirs, Your Majesty. Our columns are on the coast road, but the port and its guns are still in Wehzek hands.\"");
        R("ResMissP", missedAim,
            "\"The ports are still theirs, Your Majesty. Our columns are on the coast road, but Isor and Nanta are still in Wehzek hands.\"");
        R("ResOther", null, "\"Our last offensive took everything we asked of it.\"");
        t.Logic("ResWhat", what.ToArray());

        // Beyond the aim: the cities taken past it, the Zille cities lost back, and our own towns lost, taken back or still occupied.
        var extraNames = new List<string>();
        foreach (string extra in Subsets(ZilleCities).Select(ZilleCampaign.JoinNames))
        {
            string name = $"ResExtra{Key(extra)}";
            extraNames.Add(name);
            bool many = Plural(extra) == "are";
            t.Line(name, Carlos,
                $"\"And we took more than the Staff had planned for. The garrison{(many ? "s" : "")} of {extra} gave up as soon as " +
                $"{(many ? "they" : "it")} saw our flags.\"", new[] { "ResLostZZille" }, condition: And(Is(VarExtraTaken), Eq(VarExtraList, extra)));
        }

        t.Logic("ResExtra", extraNames.Append("ResLostZZille").ToArray());
        var lostTexts = new Dictionary<string, string>
        {
            ["Zille"] = "we have lost Zille, Your Majesty. They counterattacked into the city, and the harbor is theirs again.",
            ["Auremina"] = "we have lost Auremina. They came back over the ridges, and the mines are theirs again.",
            ["Isor"] = "we have lost Isor. They came back along the coast road, and the port is theirs again.",
            ["Nanta"] = "we have lost Nanta. Fresh troops came off their ships, and the port is theirs again.",
        };
        for (int i = 0; i < ZilleCities.Length; i++)
        {
            string c = ZilleCities[i], name = $"ResLostZ{Key(c)}";
            string next = i + 1 < ZilleCities.Length ? $"ResLostZ{Key(ZilleCities[i + 1])}" : "ResOwnMaveNortus";
            t.Logic(name, new[] { name + "G", name + "N", next });
            t.Line(name + "G", Carlos, $"\"But {lostTexts[c]}\"", new[] { next }, condition: And(Is(LostNowVar(c)), Is(VarGainedAny)));
            t.Line(name + "N", Carlos, $"\"And {lostTexts[c]}\"", new[] { next }, condition: Is(LostNowVar(c)));
        }

        var ownLost = new Dictionary<string, string>
        {
            ["Mave Nortus"] = "\"While our columns were pushing west, the enemy took Mave Nortus. Their flag is flying over our own border town.\"",
            ["Dapalle"] = "\"They have also broken into Dapalle. Our headquarters had to fall back to Balle.\"",
            ["Balle"] = "\"Balle is in Wehzek hands. They are closer to the heart of Brenas than they have ever been.\"",
            ["Jales"] = "\"And Jales has fallen. They came the whole way across Brenas to reach it, and nothing we had in front of them held.\"",
        };
        for (int i = 0; i < RizianCities.Length; i++)
        {
            string c = RizianCities[i], name = $"ResOwn{Key(c)}";
            string next = i + 1 < RizianCities.Length ? $"ResOwn{Key(RizianCities[i + 1])}" : "ResLosses";
            t.Logic(name, new[] { name + "Lost", name + "Back", name + "Still", next });
            if (c == "Dapalle")
            {
                t.Logic(name + "Lost", new[] { name + "LostB", name + "LostDeep" }, condition: Is(LostNowVar(c)));
                t.Line(name + "LostB", Carlos, ownLost[c], new[] { next }, condition: IsNot(LostVar("Balle")));
                t.Line(name + "LostDeep", Carlos, "\"They have also broken into Dapalle. Our headquarters had to fall back deeper into Brenas.\"", new[] { next });
            }
            else
            {
                t.Line(name + "Lost", Carlos, ownLost[c], new[] { next }, condition: Is(LostNowVar(c)));
            }
            t.Line(name + "Back", Carlos, $"\"And we have {c} back. Our flag is over the town hall again.\"", new[] { next }, condition: Is(BackVar(c)));
            t.Line(name + "Still", Carlos, $"\"{c} is still in Wehzek hands, Your Majesty.\"", new[] { next }, condition: Is(LostVar(c)));
        }

        string second = $"{Num(VarLastPhase)} >= 2";
        Pick("ResLosses", Carlos, "ResWaited",
            (And($"{V(VarLossTier)} == 2", second),
                "\"And it cost us more than any month of this war. The lists no longer fit on the board in the corridor. The clerks have started a second one.\""),
            ($"{V(VarLossTier)} == 2", "\"It cost us dearly. The casualty lists are the longest this army has seen since the Reclamation War.\""),
            (And($"{V(VarLossTier)} == 1", second, IsNot(LostVar("Balle"))),
                "\"It was not cheap. The hospital trains have run south day and night, and the field hospitals at Balle are taking the overflow.\""),
            (And($"{V(VarLossTier)} == 1", second),
                "\"It was not cheap. The hospital trains have run south day and night, and the field hospitals behind the line are taking the overflow.\""),
            (And($"{V(VarLossTier)} == 1", IsNot(LostVar("Dapalle"))),
                "\"It was not cheap. The hospitals in Dapalle are full, and the trains south are full of wounded.\""),
            (And($"{V(VarLossTier)} == 1", IsNot(LostVar("Balle"))),
                "\"It was not cheap. The hospitals in Balle are full, and the trains south are full of wounded.\""),
            ($"{V(VarLossTier)} == 1", "\"It was not cheap. The hospitals behind the line are full, and the trains south are full of wounded.\""),
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
        t.Logic("Push", new[] { "PushSetback", "PushSetbackCity", "PushSetbackWon", "PushSetbackMen", "PushGood" });
        string missed = And(Fought, IsNot(VarLastWon), IsNot(VarWaited));
        string last = $"{Num(VarPhase)} >= {ZilleCampaign.LastPhase}";
        t.Logic("PushGood", new[] { "PushGoodHeld", "PushGoodMissed", "PushGoodLast", "PushGood0" }, condition: $"(({IsNot(VarSetback)}) or ({Is(VarWaited)}))");
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
            condition: And(TaddeusHere, missed, $"{V(VarLastGround)} ~= \"lost\""));
        t.Line("PushTadLast", Taddeus, "\"One more push, Lieutenant. Finish it, and finish it properly.\"", new[] { "PushMenu" },
            condition: And(TaddeusHere, last));
        t.Line("PushTad0", Taddeus, "\"A complete victory is in sight, Lieutenant.\"", new[] { "PushMenu" }, condition: TaddeusHere);
        t.Logic("PushMenu", new[] { "PushGo", "PushGoMissed", "PushGoLast", "PushWait", "Cease1", "Cease2", "Cease1N", "Cease2N", "Cease1R", "Cease2R", "Cease1NR", "Cease2NR" });
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
            new[] { "SetbackMenu" }, condition: And(Is(VarSetback), IsNot(VarWaited), Eq(VarSetbackWhy, "town")));
        t.Line("PushSetbackCity", Carlos,
            "\"We are not in a good position, Your Majesty. The divisions that went in last time are exhausted, and they have just given back " +
            "ground we had won.\"", new[] { "SetbackMenu" }, condition: And(Is(VarSetback), IsNot(VarWaited), Eq(VarSetbackWhy, "city")));
        t.Line("PushSetbackWon", Carlos,
            "\"The divisions that went in last time are exhausted, Your Majesty. We have the ground, but we paid for it with men this army " +
            "cannot replace in a month.\"", new[] { "SetbackMenu" }, condition: And(Is(VarSetback), IsNot(VarWaited), Is(VarGainedAny)));
        t.Line("PushSetbackMen", Carlos,
            "\"We are not in a good position, Your Majesty. The divisions that went in last time are exhausted, and they paid dearly for " +
            "very little.\"", new[] { "SetbackMenu" }, condition: And(Is(VarSetback), IsNot(VarWaited)));
        t.Logic("SetbackMenu", new[] { "SbPush", "SbHold", "SbLast", "Cease1", "Cease2", "Cease1N", "Cease2N", "Cease1R", "Cease2R", "Cease1NR", "Cease2NR" });
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

        // The ceasefire, spoken. Laurento takes it to Konstantium when he's here. "What we have taken" only when a Zille city is ours.
        string tookCity = string.Join(" or ", ZilleCities.Select(c => $"({Is(ZilleOperation.CapturedVar(c))})"));
        t.Choice("Cease1", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseLaur" },
            condition: And(LaurentoHere, tookCity, Not(ReactionsHome.LRefusedToday)));
        t.Choice("Cease2", "\"Enough. Tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseCarlos" },
            condition: And(Not(LaurentoHere), tookCity, Not(ReactionsHome.LRefusedToday)));
        t.Choice("Cease1N", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk.\"", new[] { "CeaseLaur" },
            condition: And(LaurentoHere, Not(tookCity), Not(ReactionsHome.LRefusedToday)));
        t.Choice("Cease2N", "\"Enough. Tell Konstantium we are ready to talk.\"", new[] { "CeaseCarlos" },
            condition: And(Not(LaurentoHere), Not(tookCity), Not(ReactionsHome.LRefusedToday)));
        // the same day the King refused Smolak on the telephone (+1 Authority there): asking for talks now takes it back
        string refused = ReactionsHome.LRefusedToday;
        string pay = $"{V("RiziaDLC.Resources_Authority")} = {Num("RiziaDLC.Resources_Authority")} - 1; ";
        t.Choice("Cease1R", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseLaurR" },
            condition: And(LaurentoHere, tookCity, refused), cost: "-1 Authority", script: pay);
        t.Choice("Cease2R", "\"Enough. Tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseCarlos" },
            condition: And(Not(LaurentoHere), tookCity, refused), cost: "-1 Authority", script: pay);
        t.Choice("Cease1NR", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk.\"", new[] { "CeaseLaurR" },
            condition: And(LaurentoHere, Not(tookCity), refused), cost: "-1 Authority", script: pay);
        t.Choice("Cease2NR", "\"Enough. Tell Konstantium we are ready to talk.\"", new[] { "CeaseCarlos" },
            condition: And(Not(LaurentoHere), Not(tookCity), refused), cost: "-1 Authority", script: pay);
        t.Line("CeaseLaurR", Laurento, ReactionsHome.LaurentoAfterRefusal, new[] { "CeaseCarlos" });
        t.Line("CeaseLaur", Laurento,
            "\"I'll speak to the Secretary-General tonight, Your Majesty. Smolak will refuse the first offer on principle. He'll accept the " +
            "second, because his treasury will make him.\"",
            new[] { "CeaseCarlos" });
        Pick("CeaseCarlos", Carlos, "CeaseTad",
            (tookCity, "\"Understood, Your Majesty. The army will hold its positions and fire only if fired upon.\""),
            (null, "\"Understood, Your Majesty. The army will stand where it is and fire only if fired upon.\""));
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
        t.Logic("IntelOpenC", new[] { "IntelOpenCGone", "IntelOpenC3", "IntelOpenC2", "IntelOpenC1" }, condition: Not(LucitaHere));
        t.Line("IntelOpenCGone", Narrator, "Lucita Azaro's place at the table was empty. Carlos had taken her folders, and he read them out himself.",
            new[] { "Garr" }, condition: And(Later, "Variable[\"RiziaDLC.Lucita_Arrested\"] == true or Variable[\"" + EarlyT6.VarLucitaArrestedMidWar + "\"] == true"));
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
        string told(int n) => And(Later, IsNot(VarWaited), $"{Num(VarGarrTold)} == {n}");
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
            And(SupplyWar.LuaOilCut, IsNot(VarToldEmbargo)), "SupEmbargoHalf", script: Set(VarToldEmbargo, "true"));
        IntelOpt("SupEmbargoHalf", "\"Rumburg buys half the Wehzek oil it used to. Smolak's treasury feels it, though his fuel depots in Zille are still full.\"",
            And(SupplyWar.LuaOilHalved, IsNot(VarToldEmbargo)), "SupRaids", script: Set(VarToldEmbargo, "true"));
        IntelOpt("SupRaids",
            "\"The Bludish Freedom Front has been busy in the north of Wehlen. Every battalion Smolak keeps at home to guard his ports is one he " +
            "can't send to Zille.\"", And(Lever(SupplyWar.Lever.BffRaids), IsNot(VarToldRaids)), "SupLanded", script: Set(VarToldRaids, "true"));
        // What the strait let through just before the last phase (PhaseResume landed it on the map).
        // At the port they really used: the first of Nanta, Isor and Zille that was still theirs.
        string landed = And(Later, Fought, IsNot(VarWaited), $"{Num(PhaseResume.VarLanded)} >= 1");
        t.Logic("SupLanded", new[] { "SupLandedNanta", "SupLandedIsor", "SupLandedZille", "SupLandedAny", "SupLandedNone" });
        foreach (string port in new[] { "Nanta", "Isor", "Zille" })
        {
            Intel($"SupLanded{port}", $"\"And they put fresh battalions ashore at {port} just before our last push. We met them on the line within the day.\"",
                new[] { "Surprise" }, And(landed, Eq(VarLandedAt, port)));
        }

        Intel("SupLandedAny", "\"And they put fresh battalions ashore just before our last push. We met them on the line within the day.\"",
            new[] { "Surprise" }, landed);
        t.Logic("SupLandedNone", new[] { "Surprise" });

        // At most one disruption (the Bludish companies, the Lespian cameras, a defector); the strait itself comes with the orders.
        SupplyWar.CouncilSurprise(t, "Surprise", "Brief", LucitaHere);
        t.Logic("Brief", new[] { "BriefFirst", "Matter" });
    }

    /// <summary>
    /// "Would you like to hear it?" and the staff briefing, in the manner of vanilla's Pales briefing (c357): the King asks, the
    /// staff answer from the state of the war. Each meeting has its own questions. The first lays out the campaign (the army, the
    /// garrison and why it is as strong as it is, the way into Zille, the sea, what Smolak will do). The second is a month's
    /// reckoning (the first phase by name and its dead, how Wehlen has answered, what keeps their garrison going or has worn it down,
    /// our army, the ground to the next aim from what we hold, the sea, Smolak's next move). The last is the end of the campaign
    /// (what the last phase cost, what is left of their garrison, whether the army has one more push in it, where we go in, where the
    /// line would run if the guns stopped tonight, what Smolak does when we go in).
    /// </summary>
    private static void Briefing()
    {
        t.Line("BriefFirst", Carlos,
            "\"Your Majesty, you have the final word on how this war is fought. But the Staff has prepared a briefing. Would you like to hear it?\"",
            new[] { "BriefYes", "BriefNo" }, condition: First);
        t.Choice("BriefYes", "\"Please, General.\"", new[] { "Hub" });
        t.Choice("BriefNo", "\"No. I've read the reports. Tell me the objective.\"", new[] { "BriefNo2" });
        t.Line("BriefNo2", Carlos, "\"Of course, Your Majesty.\"", new[] { "Obj" });

        string p1 = First;
        string p2 = And(Later, $"{Num(VarPhase)} < 3");
        string p3 = And(Later, $"{Num(VarPhase)} >= 3");
        string s = Strength;
        string held(string c) => Is(ZilleOperation.CapturedVar(c));
        string occ(string c) => Is(LostVar(c));
        string free(string c) => IsNot(LostVar(c));

        t.Logic("Hub", new[] { "HubQ1", "HubQ2" });
        t.Logic("HubQ1", new[] { "HubQ1Last", "HubQ1First" }, condition: IsNot(HubVisited), script: Set(HubVisited, "true"));
        t.Line("HubQ1Last", Carlos, "\"Ask what you need, Your Majesty. There won't be another briefing like this one.\"", new[] { "HubMenu" }, condition: p3);
        t.Line("HubQ1First", Carlos, "\"What would you like to hear about first?\"", new[] { "HubMenu" });
        t.Line("HubQ2", Carlos, "\"What else would you like to know?\"", new[] { "HubMenu" });

        // ---- the King's questions, a different set at each meeting
        var menu = new List<string>();
        void Q(string name, string cond, string text, string asked, string next)
        {
            menu.Add(name);
            t.Choice(name, text, new[] { next }, condition: And(cond, IsNot(asked)), script: Set(asked, "true"));
        }

        Q("Q1Army", p1, "\"Where does the army stand?\"", AskedForces, "BArmy");
        Q("Q1Enemy", p1, "\"What are we up against in Zille?\"", AskedGarrison, "BEnemy");
        Q("Q1Ground", And(p1, Obj(Objective.Harbor)), "\"How do we get into Zille?\"", AskedFronts, "BGround");
        Q("Q1GroundX", And(p1, Not(Obj(Objective.Harbor))), "\"Show me the ground.\"", AskedFronts, "BGround");
        Q("Q1Sea", p1, "\"And the sea?\"", AskedSea, "BSea");
        Q("Q1Next", p1, "\"What will Smolak do when we cross?\"", AskedNext, "BNext");

        Q("Q2Last1", And(p2, Fought, $"{Num(VarLastPhase)} < 2"), "\"Take me through the first phase.\"", AskedLast, "BLast");
        Q("Q2Last2", And(p2, Fought, $"{Num(VarLastPhase)} >= 2"), "\"Take me through the last phase.\"", AskedLast, "BLast");
        Q("Q2Enemy", p2, "\"How has Wehlen answered?\"", AskedGarrison, "BEnemy");
        Q("Q2WhyS", And(p2, $"{s} >= 8"), "\"Why are they still so strong?\"", AskedWhy, "BWhy");
        Q("Q2WhyD", And(p2, $"{s} >= 6 and {s} < 8"), "\"What keeps their garrison going?\"", AskedWhy, "BWhy");
        Q("Q2WhyW", And(p2, $"{s} < 6"), "\"What has worn them down?\"", AskedWhy, "BWhy");
        Q("Q2Army", p2, "\"And our own army?\"", AskedForces, "BArmy");
        foreach (string aim in AimNames)
        {
            Q($"Q2Ground{Key(aim)}", And(p2, AimIs(aim)), $"\"How do we reach {aim}?\"", AskedFronts, "BGround");
        }

        Q("Q2Sea", p2, "\"And the sea?\"", AskedSea, "BSea");
        Q("Q2Next", p2, "\"What will Smolak try next?\"", AskedNext, "BNext");

        Q("Q3Last", And(p3, Fought), "\"What did the last phase cost us?\"", AskedLast, "BLast");
        Q("Q3Enemy", p3, "\"What is left of their garrison?\"", AskedGarrison, "BEnemy");
        Q("Q3Army", p3, "\"Has the army one more push in it?\"", AskedForces, "BArmy");
        Q("Q3Ground", p3, "\"Where do we go in?\"", AskedFronts, "BGround");
        Q("Q3Line", p3, "\"If the guns stopped tonight, where would the line be?\"", AskedLine, "BLine");
        Q("Q3Next", p3, "\"What will Smolak do when we go in?\"", AskedNext, "BNext");

        // Always offered, so the briefing can end in every state.
        t.Choice("QDone", "\"I think I have enough information.\"", new[] { "HubDone" }, condition: Not(p3));
        t.Choice("QDone3", "\"That's enough. Let's give the orders.\"", new[] { "HubDone" }, condition: p3);
        t.Logic("HubMenu", menu.Concat(new[] { "QDone", "QDone3" }).ToArray());
        t.Logic("HubDone", new[] { "HubDoneFirst", "Fleet" });
        t.Logic("HubDoneFirst", new[] { "Obj" }, condition: ChoosePlan);

        BriefArmy(p1, p3);
        BriefEnemy(p1, p3, s, held);
        BriefWhy(s);
        BriefGround(held, occ, free);
        BriefSea();
        BriefNext(s, held, free);
        BriefLast(p2);
        BriefLine();
    }

    /// <summary>The first variant whose condition holds, spoken by Lucita (or Carlos without her); then <paramref name="next"/>.</summary>
    private static void IntelPick(string name, string next, params (string cond, string text)[] variants)
    {
        t.Logic(name, variants.Select((_, i) => $"{name}_{i}").Append(next).ToArray());
        for (int i = 0; i < variants.Length; i++)
        {
            Intel($"{name}_{i}", variants[i].text, new[] { next }, variants[i].cond ?? "true");
        }
    }

    /// <summary>The first variant whose condition holds, spoken by Laurento (or Carlos without him); then <paramref name="next"/>.</summary>
    private static void DipPick(string name, string next, params (string cond, string text)[] variants)
    {
        t.Logic(name, variants.Select((_, i) => $"{name}_{i}").Append(next).ToArray());
        for (int i = 0; i < variants.Length; i++)
        {
            string n = $"{name}_{i}";
            t.Logic(n, new[] { n + "_D", n + "_C" }, condition: variants[i].cond ?? "true");
            t.Line(n + "_D", Laurento, variants[i].text, new[] { next }, condition: LaurentoHere);
            t.Line(n + "_C", Carlos, variants[i].text, new[] { next }, condition: Not(LaurentoHere));
        }
    }

    /// <summary>The same estimate of men the after-action dispatch prints for a number of destroyed formations (AfterAction.Men).</summary>
    private static string Men(int units, int salt) =>
        units <= 0 ? "a few dozen" : (units * 850 + (units * 137 + salt * 31) % 300).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private const int CountedUnits = 30;   // dead formations told by number; more are told as "more than ..."

    /// <summary>"A and B", "A, B and C", or the first three, then "We also have ..." for the rest.</summary>
    private static string Gains(IList<string> items, string lead)
    {
        if (items.Count <= 3)
        {
            return $"{lead} {ZilleCampaign.JoinNames(items)}.";
        }

        return $"{lead} {ZilleCampaign.JoinNames(items.Take(3).ToList())}. We also have {ZilleCampaign.JoinNames(items.Skip(3).ToList())}.";
    }

    /// <summary>Every subset of the conditional items, with the condition that exactly those hold.</summary>
    private static IEnumerable<(string cond, List<string> texts)> Combos(IList<(string cond, string text)> items)
    {
        for (int mask = 1; mask < 1 << items.Count; mask++)
        {
            var conds = new List<string>();
            var texts = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                bool inSet = (mask & (1 << i)) != 0;
                conds.Add(inSet ? items[i].cond : Not(items[i].cond));
                if (inSet)
                {
                    texts.Add(items[i].text);
                }
            }

            yield return (And(conds.ToArray()), texts);
        }
    }

    /// <summary>Our army: the divisions, the armor and the special units we really have, and what has joined it since the declaration.</summary>
    private static void BriefArmy(string p1, string p3)
    {
        string inf = Num("RiziaDLC.Army_Unit_Infantry_Total"), sup = Num("RiziaDLC.Army_Unit_Support_Total"), tank = Num("RiziaDLC.Army_Unit_Tank_Total");
        t.Logic("BArmy", new[] { "BArmyLand" });

        (string cond, string text)[] infT =
        {
            ($"{inf} >= 10", "ten infantry divisions or more"), ($"{inf} >= 8", "eight or nine infantry divisions"),
            ($"{inf} >= 6", "six or seven infantry divisions"), ($"{inf} >= 4", "four or five infantry divisions"),
            (null, "fewer than four infantry divisions"),
        };
        (string cond, string text)[] supT =
        {
            ($"{sup} >= 7", "a strong body of mechanized support"), ($"{sup} >= 4", "a fair number of mechanized support divisions"),
            ($"{sup} >= 1", "only a little mechanized support"), (null, "no mechanized support to speak of"),
        };
        var land = new List<(string, string)>();
        string spent = And(Later, Fought);
        foreach ((string ic, string it) in infT)
        {
            foreach ((string sc, string st) in supT)
            {
                land.Add((And(p3, spent, ic, sc), $"\"One more, yes, Your Majesty. Not two. We can still field {it}, with {st} behind them.\""));
                land.Add((And(spent, ic, sc), $"\"After what the fighting has cost us, we can still field {it}, with {st} behind them.\""));
                land.Add((And(ic, sc), $"\"We can field {it}, Your Majesty, with {st} behind them.\""));
            }
        }

        Pick("BArmyLand", Carlos, "BArmyArmor", land.Select(x => (x.Item1 == string.Empty ? null : x.Item1, x.Item2)).ToArray());

        // The armor, and the special units the plan can use.
        (string cond, string text)[] tankT =
        {
            ($"{tank} >= 6", "\"Six tank divisions or more. In open country they can lead anywhere."),
            ($"{tank} >= 3", "\"Enough tanks for one armored thrust at a time, not two."),
            ($"{tank} >= 1", "\"One or two tank divisions. I won't pretend that is an armored force."),
            (null, "\"No tanks to speak of."),
        };
        var special = new List<(string cond, string text)>
        {
            ($"{Num("RiziaDLC.Army_Unit_Marine_Total")} >= 1", "the marines at Sallabes"),
            ($"{Num(Para)} >= 1", "the paratroopers at their airfield"),
            ($"{Num(Mount)} >= 1", "the mountaineers"),
        };
        var armor = new List<(string, string)>();
        foreach ((string tc, string tt) in tankT)
        {
            foreach ((string sc, List<string> sp) in Combos(special))
            {
                armor.Add((And(tc, sc), $"{tt} We also have {ZilleCampaign.JoinNames(sp)}.\""));
            }

            armor.Add((tc, $"{tt}\""));
        }

        Pick("BArmyArmor", Carlos, "BArmyNew", armor.Select(x => (x.Item1 == string.Empty ? null : x.Item1, x.Item2)).ToArray());

        // What has joined the army since the declaration: volunteers, aircraft, decrees.
        var gained = new List<(string cond, string text)>
        {
            (And(Lever(SupplyWar.Lever.BffFighters), $"{V(SupplyWar.VarBludsIncident)} ~= \"trial\"", $"{V(SupplyWar.VarBludsIncident)} ~= \"home\""),
                "the Bludish companies from Rumburg"),
            (Is("ZilleWar.Decree_ZilleanLegion"), "the Zillean Legion"),
            (Is("ZilleWar.Decree_MerchantFleet"), "another marine battalion, in requisitioned ships"),
            (Is(SupplyWar.VarMorellaAircraft), "the Morellan aircraft"),
            (And(Is("ZilleWar.Decree_BrenasReserves"), IsNot("ZilleWar.Decree_StandDownReserves")), "the Brenas reserves"),
            (Is("ZilleWar.Decree_WarFooting"), "the shells from the Iza works"),
        };
        var news = new List<string>();
        foreach ((string gc, List<string> gt) in Combos(gained))
        {
            string n1 = $"BArmyNewL{news.Count}";
            news.Add(n1);
            Intel(n1, $"\"{Gains(gt, "Since the declaration we have gained")}\"", new[] { "Hub" }, And(Later, gc));
            string n2 = $"BArmyNewL{news.Count}";
            news.Add(n2);
            Intel(n2, $"\"{Gains(gt, "We already have")}\"", new[] { "Hub" }, gc);
        }

        t.Logic("BArmyNew", news.Append("Hub").ToArray());
    }

    /// <summary>
    /// The enemy. First meeting: the garrison as it will meet us, and why. Later: what changed on their side (where their command
    /// sits, the air defence lost or regained with Zille, how they stand on the ground); at the last meeting, why as well.
    /// </summary>
    private static void BriefEnemy(string p1, string p3, string s, Func<string, string> held)
    {
        t.Logic("BEnemy", new[] { "BEnemyFirst", "BEnemyHq" });
        // The garrison by tier (what Garrison.Build puts on the first phase's map).
        t.Logic("BEnemyFirst", new[] { "BEnemyTier" }, condition: p1);
        Pick("BEnemyTier", Carlos, "BWhy",
            ($"{s} >= 8", "\"Close to a full corps of regulars, with four tank battalions in reserve and hardly any militia. They have had months to prepare.\""),
            ($"{s} >= 6", "\"Regular infantry along the line, three or so tank battalions in reserve, and militia filling the gaps.\""),
            ($"{s} >= 4", "\"Two tank battalions, and more militia than regulars now. The regulars are the ones they can no longer resupply.\""),
            (null, "\"One tank battalion, if it has fuel. The rest are militia and police with rifles.\""));

        // Where their command sits now, and what went with Zille.
        var hq = new List<(string, string)>();
        foreach (string c in new[] { "Auremina", "Nanta", "Isor" })
        {
            hq.Add((And(held("Zille"), Eq(VarHq, c)),
                $"\"Their headquarters has moved up to {c}. With Zille gone they lost the only anti-aircraft guns in the territory, and our planes have the sky.\""));
        }

        hq.Add((Is(VarZilleBack),
            "\"Their headquarters went back into Zille the day they retook it. So did the anti-aircraft guns, and our pilots have noticed.\""));
        hq.Add((null, "\"Their headquarters is still in Zille, under the guns that cover the harbor. Until the city falls, our planes keep away from it.\""));
        IntelPick("BEnemyHq", "BEnemyGround", hq.ToArray());

        // How they stand on the ground now.
        var ground = new List<(string, string)>();
        foreach (string aim in AimNames)
        {
            ground.Add((And(Is(VarWaited), AimIs(aim)),
                $"\"They used the month we gave them. Every road into {aim} is mined now, and their trenches have roofs.\""));
        }

        ground.Add(($"{s} < 6",
            "\"On the ground, their regulars are thinning out. They fill the gaps in the line with militia and police, and the militia know it.\""));
        ground.Add((And(Fought, Not(Result("won")), Not(Result("partial")), $"{V(VarLastGround)} ~= \"gained\""),
            "\"On the ground, they have dug in where they stopped us, and they still have the men to hold it.\""));
        foreach (string aim in AimNames)
        {
            ground.Add((AimIs(aim), $"\"On the ground, they have pulled their regulars back around {aim} and dug in. Whatever we take next, we take from regulars.\""));
        }

        ground.Add((null, "\"On the ground, they have pulled their regulars back and dug in. Whatever we take next, we take from regulars.\""));
        Pick("BEnemyGround", Carlos, "BEnemyAfter", ground.ToArray());
        // At the last meeting there is no separate question for the reasons: they follow here.
        t.Logic("BEnemyAfter", new[] { "BEnemyWhy", "Hub" });
        t.Logic("BEnemyWhy", new[] { "BWhy" }, condition: p3);
    }

    /// <summary>Why the garrison is as strong as it is: the strait, the money and fuel, the friends and enemies Wehlen made, and whether it can replace its losses.</summary>
    private static void BriefWhy(string s)
    {
        string closed = Is(SupplyWar.VarLespiaStrait);
        IntelPick("BWhy", "BWhyMoney",
            (And(closed, Is("ZilleWar.Lespia_StraitThin")),
                "\"The strait first. Lespia still patrols it on our account, but thinly since Perla took offense, and more gets through every week.\""),
            (closed, "\"The strait first. Since Perla's deal with us, Lespian ships stop and search most of what crosses, and only the night runners get through.\""),
            (Is(SupplyWar.VarLespiaWithdrawn), "\"The strait first. The Lespian ships left, and the convoys came back within the week.\""),
            (And(Lever(SupplyWar.Lever.RoyalNavy), Lever(SupplyWar.Lever.Submarines)),
                "\"The strait first. Our destroyers and submarines have sunk enough transports that their convoys cross only at night, and not all of them arrive.\""),
            (Lever(SupplyWar.Lever.RoyalNavy), "\"The strait first. Our destroyers have sunk enough of their transports that the convoys cross only at night now.\""),
            (Lever(SupplyWar.Lever.Submarines), "\"The strait first. Our submarines have made every crossing a gamble, and the Wehzek captains know it.\""),
            (null, "\"The strait first, Your Majesty. Nobody is closing it. Whatever Wraclawitz sends across, arrives.\""));

        string fuel = Is(SupplyWar.VarLespiaFuel);
        IntelPick("BWhyMoney", "BWhyOther",
            (And(fuel, SupplyWar.LuaOilCut),
                "\"Rumburg no longer buys their oil, but Perla sells their tankers fuel now. Smolak's treasury empties while his depots in Zille stay full.\""),
            (fuel, "\"And Perla sells fuel to Wehzek tankers now. Whatever else Zille is short of, it is not diesel.\""),
            (SupplyWar.LuaOilCut, "\"And Rumburg no longer buys Wehzek oil. Smolak is paying for this war from a treasury that has lost its best customer.\""),
            (SupplyWar.LuaOilHalved, "\"And Rumburg buys half the Wehzek oil it used to. Smolak's treasury feels it, though his garrison doesn't yet.\""));

        IntelPick("BWhyOther", "BWhyReplace",
            (Is(VarWaited), "\"And we gave them a month. They used every day of it.\""),
            (Eq(SupplyWar.VarValgslandCable, "defied"),
                "\"And since you answered Hegel as you did, Contanan freighters sail through Valgsland's ports exactly on time, with rifles for Wehlen.\""),
            (And(Is(SupplyWar.VarSmolakRefused), $"{s} >= 6"),
                "\"And since you refused his ceasefire, Smolak has been sending across the strait the reservists he was keeping at home.\""),
            (Lever(SupplyWar.Lever.BffRaids),
                "\"The Bludish raids help. Every battalion Smolak keeps at home to guard his ports is one he can't send to Zille.\""),
            (Is("ZilleWar.Decree_DeserterSafeConduct"),
                "\"And your safe-conduct leaflets work. Deserters come over every night, and each one tells his friends how well we feed them.\""),
            (Is("ZilleWar.WarRoom_Defector"), "\"And one of their transport captains brought us his shells instead of delivering them. The others have heard.\""),
            (Eq(SupplyWar.VarValgslandCable, "mitz"), "\"And the Contanan freighters find Valgsland's harbors very crowded these days. Hegel has a vote to think of.\""),
            (Eq(SupplyWar.VarValgslandCable, "guarantee"),
                "\"And since you gave Hegel his guarantees, a few Contanan freighters are being held in Valgslandian ports. Smolak has noticed.\""));

        // Whether they can replace what they lose (PhaseResume: replacements only above the Strained tier).
        Pick("BWhyReplace", Carlos, "Hub",
            ($"{s} >= 6", "\"And Wehlen can still replace what we kill, Your Majesty, as long as the convoys get through.\""),
            (null, "\"And Wehlen can no longer replace what it loses. Whatever reaches Zille now has to slip through the strait.\""));
    }

    /// <summary>The ground to the next aim, approached from what we actually hold (see MAPS 3.4).</summary>
    private static void BriefGround(Func<string, string> held, Func<string, string> occ, Func<string, string> free)
    {
        string moridia = Is(VarMoridia), broken = Is(VarWallBroken);
        t.Logic("BGround", new[] { "BGroundZ", "BGroundA", "BGroundP", "BGroundI", "BGroundN", "BGroundHome" });

        // Zille: from Dapalle down the river valley, from Fort Moridia up the coast road, or down the valley from Auremina.
        t.Logic("BGroundZ", new[] { "BGroundZ1" }, condition: Obj(Objective.Harbor));
        Pick("BGroundZ1", Carlos, "BGroundZ2",
            (First, "\"Zille sits on its bay, one ridge behind their wall. The main road and the railway come down the river valley from Dapalle, the coast road up from Fort Moridia.\""),
            (And(held("Auremina"), free("Dapalle")),
                "\"This time from two sides: down the valley from Auremina, which we hold, and along the river road from Dapalle. Zille is close to both.\""),
            (And(held("Auremina"), moridia),
                "\"Down the valley from Auremina, which we hold, and up the coast road from Fort Moridia. With Dapalle theirs, the river road is closed to us.\""),
            (held("Auremina"), "\"Down the valley from Auremina, which we hold. With Dapalle and Fort Moridia theirs, it is the only road we have.\""),
            (And(occ("Dapalle"), moridia),
                "\"With Dapalle in their hands, the river road is closed. We go up the coast road from Fort Moridia, with the guns on the bay watching every yard.\""),
            (occ("Dapalle"), "\"With Dapalle and Fort Moridia in their hands, both roads to Zille start on their side of the line. We have to take one of them back first.\""),
            (And(broken, moridia), "\"The same roads as before, but we start inside their wall now: the river road from Dapalle, and the coast road from Fort Moridia beside it.\""),
            (broken, "\"The river road from Dapalle, and we start inside their wall now. Fort Moridia is theirs, so the coast road is not.\""),
            (moridia, "\"The same roads as before: the river valley down from Dapalle and the coast road from Fort Moridia. Their wall is still in front of both.\""),
            (null, "\"The river valley down from Dapalle. Their wall is still in front of it, and with Fort Moridia theirs, the coast road is closed.\""));
        Pick("BGroundZ2", Carlos, "BGroundZ3",
            (Is(VarZilleBack), "\"And they know the streets are worth dying for now. They took the city back once, and they will have mined every cellar we used.\""),
            (First, "\"Their wall runs from Mave Nortus to the gulf: concrete, wire, a bunker on every crossing. Zille itself has coastal guns, and the only anti-aircraft guns they have.\""),
            (null, "\"Zille still has its coastal guns, and the only anti-aircraft guns in the territory. Until they are silenced, our planes and ships keep away.\""));
        t.Logic("BGroundZ3", new[] { "BGroundZ3Say", "BGroundHome" });
        Intel("BGroundZ3Say", "\"From the sea, the bay is wide open. But those guns will be waiting for any ship that comes in.\"", new[] { "BGroundHome" }, First);

        // Auremina (Zille is ours): up the valley from Zille, over the ridge from Dapalle, down from the ports we hold.
        t.Logic("BGroundA", new[] { "BGroundA1" }, condition: Obj(Objective.Wall));
        var aure = new List<(string, string)>();
        foreach ((string dc, string dt) in new[] { (free("Dapalle"), " A second column can climb the ridge from Dapalle."), (occ("Dapalle"), " With Dapalle theirs, the valley road is the only way up from the south.") })
        {
            aure.Add((And(dc, held("Nanta"), held("Isor")), $"\"Up the valley road from Zille: Auremina sits in the hills just north of the city.{dt} And our men in Nanta and Isor can come down at it from the north and west.\""));
            aure.Add((And(dc, held("Nanta")), $"\"Up the valley road from Zille: Auremina sits in the hills just north of the city.{dt} And our men in Nanta can come down at it from the north.\""));
            aure.Add((And(dc, held("Isor")), $"\"Up the valley road from Zille: Auremina sits in the hills just north of the city.{dt} And our men in Isor can come at it over the hill road from the west.\""));
            aure.Add((dc, $"\"Up the valley road from Zille: Auremina sits in the hills just north of the city.{dt}\""));
        }

        Pick("BGroundA1", Carlos, "BGroundA2", aure.ToArray());
        t.Line("BGroundA2", Carlos,
            "\"Mining country: narrow roads, ridges, old tunnels where a battalion can hide. Our tanks won't be much use up there. It will be infantry work.\"",
            new[] { "BGroundHome" });

        // The ports (Zille and Auremina are ours).
        t.Logic("BGroundP", new[] { "BGroundP1" }, condition: And(Obj(Objective.Ports), AimIs("Isor and Nanta")));
        Pick("BGroundP1", Carlos, "BGroundP2",
            (free("Mave Nortus"), "\"Nanta from two sides: along the coast road from Mave Nortus, and down through the hills from Auremina. Isor lies west of Auremina, over the hill road.\""),
            (null, "\"With Mave Nortus theirs, the coast road to Nanta is closed, so everything goes over the hills from Auremina: north to Nanta, west to Isor.\""));
        t.Line("BGroundP2", Carlos,
            "\"Nanta has coastal guns of its own, and it is where their convoys land first. Isor has no guns, but it is their other port.\"",
            new[] { "BGroundHome" });
        t.Logic("BGroundI", new[] { "BGroundI1" }, condition: And(Obj(Objective.Ports), AimIs("Isor")));
        t.Line("BGroundI1", Carlos,
            "\"From two sides: west along the north coast from Nanta, and over the hill road from Auremina. Isor has no coastal guns, and it is the last port they have.\"",
            new[] { "BGroundHome" });
        t.Logic("BGroundN", new[] { "BGroundN1" }, condition: And(Obj(Objective.Ports), AimIs("Nanta")));
        Pick("BGroundN1", Carlos, "BGroundN2",
            (free("Mave Nortus"), "\"From three sides: east along the north coast from Isor, over the hills from Auremina, and along the coast road from Mave Nortus.\""),
            (null, "\"From two sides: east along the north coast from Isor, and over the hills from Auremina. Mave Nortus is theirs, so the coast road is shut.\""));
        t.Line("BGroundN2", Carlos, "\"Nanta still has its coastal guns, and with Isor ours, every convoy that crosses lands there.\"", new[] { "BGroundHome" });

        // Our own towns behind the line, by name.
        var home = new List<string>();
        foreach (List<string> towns in Subsets(RizianCities))
        {
            string list = ZilleCampaign.JoinNames(towns), name = $"BGroundHome{Key(list)}";
            home.Add(name);
            bool many = towns.Count > 1;
            Intel(name, $"\"And {list} {(many ? "are" : "is")} still theirs. Whatever road we take, Brenas will ask when we take {(many ? "them" : "it")} back.\"",
                new[] { "Hub" }, Eq(VarOccupied, list));
        }

        t.Logic("BGroundHome", home.Append("Hub").ToArray());
    }

    /// <summary>The sea: where our fleet is (Aureus or the north), Lespia in the strait, Valgsland's ports.</summary>
    private static void BriefSea()
    {
        string ships = Num("RiziaDLC.Army_Unit_Ship_Total"), subs = Num("RiziaDLC.Army_Unit_Submarine_Total");
        IntelPick("BSea", "BSeaLespia",
            ($"{ships} + {subs} <= 0", "\"We have nothing left at sea that can fight, Your Majesty.\""),
            ($"{ships} <= 0", "\"Our surface fleet is gone. What we have left is under the water.\""),
            (FleetSplit, "\"Half our destroyers are still off Aureus, watching the Palesian drillship. The rest are here in the north.\""),
            (Is(VarFleetRecalled), "\"The Aureus squadron came north on your order. The whole fleet is in our waters now.\""),
            ($"{ships} >= 5", "\"The whole fleet is in the north, with destroyers enough to rule the Gulf of Moridia.\""),
            (null, "\"The whole fleet is in the north, but it is thin. It can contest the gulf. I wouldn't ask much more of it.\""));
        string closed = Is(SupplyWar.VarLespiaStrait);
        IntelPick("BSeaLespia", "BSeaValgsland",
            (And(closed, Is("ZilleWar.Lespia_StraitThin")), "\"Lespia still patrols the strait on our account, with fewer ships since Perla took offense.\""),
            (closed, "\"Lespian warships patrol the strait on our account. Alvarez is paid for it, and he makes sure we remember.\""),
            (Is(SupplyWar.VarLespiaWithdrawn), "\"The Lespian ships have gone home. Alvarez wanted us to notice, and we have.\""),
            (Is(SupplyWar.VarLespiaFuel), "\"Lespia keeps its navy out of the strait, and Perla sells fuel to Wehzek tankers.\""),
            (Is(SupplyWar.VarLespiaGrievance), "\"Lespia keeps its ships out of the strait, and Alvarez keeps his books. We are in them, on the wrong page.\""),
            (null, "\"Lespia keeps its ships out of the strait. Alvarez is waiting to see what this war is worth to us.\""));
        IntelPick("BSeaValgsland", "Hub",
            (Is(SupplyWar.VarValgslandPorts), "\"And Valgsland has closed its ports to our cargo. Its destroyers are still in the Markian Sea.\""),
            (Is(SupplyWar.VarSubsGrounded), "\"And Valgsland has your word on the submarines. Its destroyers stay in the Markian Sea to remind us.\""),
            (SupplyWar.EmbargoOn, "\"Valgsland's embargo on our cargo has held since the declaration. Hegel calls it neutrality.\""));
    }

    /// <summary>What Wehlen will do next: hold what it took of ours, counterattack, sit and wait, or dig in and hope.</summary>
    private static void BriefNext(string s, Func<string, string> held, Func<string, string> free)
    {
        var next = new List<(string, string)>();
        foreach (List<string> towns in Subsets(RizianCities))
        {
            string list = ZilleCampaign.JoinNames(towns);
            next.Add((And(Eq(VarOccupied, list), $"{s} >= 6"),
                $"\"Keep what he has of ours. He will dig in at {list} and dare us to shell our own streets.\""));
        }

        string anyHeld = string.Join(" or ", ZilleCities.Select(c => $"({held(c)})"));
        next.Add((And($"{s} >= 8", held("Zille")),
            "\"Counterattack. He has the men to try for Zille again, and he will, the moment our columns move away from it.\""));
        next.Add((And($"{s} >= 8", free("Dapalle"), free("Mave Nortus")),
            "\"Counterattack. He has the men for it. If our columns stall, he will go for Dapalle and Mave Nortus, right on the border.\""));
        next.Add(($"{s} >= 8", "\"Counterattack. He has the men for it, and he will go for whatever we leave thin behind our columns.\""));
        next.Add((And($"{s} >= 6", anyHeld),
            "\"Hold, and wait for his convoys. He hasn't the men to attack, and he can't afford to lose another city. He is hoping we tire first.\""));
        next.Add(($"{s} >= 6", "\"Hold, and wait for his convoys. He hasn't the men to attack, and he can't afford to lose Zille. He is hoping we tire first.\""));
        next.Add((null, "\"Dig in and hope. What he has left can hold a city, not take one back. His officers in Zille are already burning papers.\""));
        IntelPick("BNext", "Hub", next.ToArray());
    }

    /// <summary>The last phase by name: our dead and theirs (the dispatch's own figures), and, at the second meeting, the line as it runs tonight.</summary>
    private static void BriefLast(string p2)
    {
        var ours = new List<(string, string)>();
        foreach ((string pc, string word) in new[] { ($"{Num(VarLastPhase)} >= 2", "The second phase"), ((string)null, "The first phase") })
        {
            for (int n = 0; n <= CountedUnits; n++)
            {
                ours.Add((And(pc, $"{Num(VarOurDead)} == {n}"), $"\"{word} of Northern Crown cost us {Men(n, 7)} men killed, wounded or missing, Your Majesty.\""));
            }

            ours.Add((pc, $"\"{word} of Northern Crown cost us more than {Men(CountedUnits, 7)} men killed, wounded or missing, Your Majesty.\""));
        }

        Pick("BLast", Carlos, "BLastTheirs", ours.Select(x => (x.Item1 == string.Empty ? null : x.Item1, x.Item2)).ToArray());
        var theirs = new List<(string, string)> { ($"{Num(VarTheirDead)} <= 0", "\"By our count, they lost only a few dozen men. It was not their worst month.\"") };
        for (int n = 1; n <= CountedUnits; n++)
        {
            theirs.Add(($"{Num(VarTheirDead)} == {n}", $"\"By our count, they lost about {Men(n, 13)} men. Their newspapers say a third of that.\""));
        }

        theirs.Add((null, $"\"By our count, they lost more than {Men(CountedUnits, 13)} men. Their newspapers say a third of that.\""));
        IntelPick("BLastTheirs", "BLastLine", theirs.ToArray());

        // The second meeting: where the line runs tonight (the last meeting asks it as its own question).
        t.Logic("BLastLine", new[] { "BLastLineGo", "Hub" });
        var line = new List<(string, string)>
        {
            (And(Eq(VarHeldList, ""), Is(VarWallBroken)), "\"As the line stands tonight, all four cities of Zille are still theirs. We are through their wall, and not much further.\""),
            (Eq(VarHeldList, ""), "\"As the line stands tonight, all four cities of Zille are still theirs, and their wall is still standing.\""),
        };
        foreach (List<string> h in Subsets(ZilleCities))
        {
            if (h.Count == ZilleCities.Length)
            {
                continue;
            }

            string hl = ZilleCampaign.JoinNames(h), ll = ZilleCampaign.JoinNames(ZilleCities.Where(c => !h.Contains(c)).ToList());
            line.Add((Eq(VarHeldList, hl), $"\"As the line stands tonight, we hold {hl}. {ll} {Plural(ll)} still theirs.\""));
        }

        t.Logic("BLastLineGo", new[] { "BLastLinePick" }, condition: p2);
        IntelPick("BLastLinePick", "BLastOcc", line.ToArray());
        var occ = new List<string>();
        foreach (List<string> towns in Subsets(RizianCities))
        {
            string list = ZilleCampaign.JoinNames(towns), name = $"BLastOcc{Key(list)}";
            occ.Add(name);
            t.Line(name, Carlos, $"\"And on our side of the border, they still hold {list}.\"", new[] { "Hub" }, condition: Eq(VarOccupied, list));
        }

        t.Logic("BLastOcc", occ.Append("Hub").ToArray());
    }

    /// <summary>The last meeting: where the observers would draw the line if the guns stopped tonight, and what the world would call it.</summary>
    private static void BriefLine()
    {
        var line = new List<(string, string)>
        {
            (Eq(VarHeldList, ""), "\"Where it ran before the war, Your Majesty, give or take a few hills. Every city in Zille would stay Wehzek.\""),
        };
        foreach (List<string> h in Subsets(ZilleCities))
        {
            if (h.Count == ZilleCities.Length)
            {
                continue;
            }

            string hl = ZilleCampaign.JoinNames(h), ll = ZilleCampaign.JoinNames(ZilleCities.Where(c => !h.Contains(c)).ToList());
            line.Add((Eq(VarHeldList, hl), $"\"Through the middle of Zille, Your Majesty. {hl} on our side, {ll} on theirs.\""));
        }

        DipPick("BLine", "BLineMeans", line.ToArray());
        DipPick("BLineMeans", "BLineOcc",
            (Is(ZilleOperation.CapturedVar("Zille")),
                "\"With Zille city ours, Konstantium would call it a divided territory. Not a defeat, but not the whole of what we came for.\""),
            (null, "\"Without Zille city, the world would call it a lost war, whatever else we hold. Smolak would make sure of that.\""));
        var occ = new List<(string, string)>();
        foreach (List<string> towns in Subsets(RizianCities))
        {
            string list = ZilleCampaign.JoinNames(towns);
            occ.Add((Eq(VarOccupied, list), $"\"And {list} would stay in Wehzek hands until a treaty says otherwise.\""));
        }

        DipPick("BLineOcc", "Hub", occ.ToArray());
    }

    /// <summary>The objective is announced in the fixed order: the first council lays out the campaign, later ones its next step.</summary>
    private static void Objectives()
    {
        string phase3 = $"{Num(VarPhase)} >= 3";
        string lastMissed(string o) => And(Fought, IsNot(VarLastWon), $"{V(VarLastObjective)} == \"{o}\"");
        string retake = And(Later, Is(VarNextRetake));
        var objs = new List<string>();
        void O(string name, string cond, string text, string next = "Command")
        {
            objs.Add(name);
            t.Line(name, Carlos, text, new[] { next }, condition: cond);
        }

        O("ObjHarbor", And(First, Obj(Objective.Harbor)),
            "\"The objective is the city of Zille itself: the harbor, the railway station, and the Wehzek headquarters. Take Zille, and every " +
            "Wehzek soldier south of the hills is cut off from the sea.\"", "ObjHarbor2");
        t.Line("ObjHarbor2", Carlos,
            "\"After that, the hills and the gold mines at Auremina. Then the northern ports, Isor and Nanta, and the war can be won. The Staff calls " +
            "the whole campaign Operation Northern Crown, in three phases, one each month, every phase starting from the ground the last one won. Zille is the first.\"", new[] { "Command" });

        // A city we took and lost again comes first: the order of the campaign does not change, only what is left of it.
        O("ObjHarborBackLast", And(retake, Obj(Objective.Harbor), phase3),
            "\"Zille first, Your Majesty. We took it once and lost it, and this is the last phase of Northern Crown. If Zille is not ours " +
            "when it ends, the line we hold is the line the diplomats will draw.\"");
        O("ObjHarborBack", And(retake, Obj(Objective.Harbor)),
            "\"Zille first, Your Majesty. We took it once and lost it, and nothing else we hold is safe until the harbor is ours again.\"");
        O("ObjWallBackLast", And(retake, Obj(Objective.Wall), phase3),
            "\"Auremina again, Your Majesty. They took it back from us, and from those ridges their guns reach half of what we hold. It is " +
            "the last phase: whatever we hold when the guns stop is where the observers will draw the line.\"");
        O("ObjWallBack", And(retake, Obj(Objective.Wall)),
            "\"Auremina again, Your Majesty. They took it back from us, and from those ridges their guns reach half of what we hold.\"");
        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            bool many = aim.Contains(" and ");
            O($"ObjPortsBack{Key(aim)}", And(retake, Obj(Objective.Ports), AimIs(aim), phase3),
                $"\"{aim} again, Your Majesty. We took {(many ? "them" : "it")} once and lost {(many ? "them" : "it")}. This is the last phase: " +
                "whatever we hold on that coast when it ends, we keep.\"");
        }

        O("ObjHarborLast", And(Later, Obj(Objective.Harbor), phase3),
            "\"It is still Zille, Your Majesty. This is the last phase of Northern Crown. If Zille is not ours when it ends, the line we hold " +
            "is the line the diplomats will draw.\"");
        O("ObjHarborL", And(Later, Obj(Objective.Harbor)),
            "\"The objective hasn't changed, Your Majesty. Zille, its harbor and its railway station. The next phase goes in from the lines we hold " +
            "now, as soon as you give the word.\"");

        // The ports, named as they stand: "Isor" once Nanta is ours.
        string ports = And(Later, Obj(Objective.Ports));
        O("ObjPortsEarly", And(ports, Not(phase3), AimIs("Isor and Nanta")),
            "\"We are ahead of the plan, Your Majesty. With Auremina already ours, the Staff goes straight for Isor and Nanta. Take them in " +
            "this phase, and the war ends a month early.\"");
        foreach ((string port, string other) in new[] { ("Isor", "Nanta"), ("Nanta", "Isor") })
        {
            O($"ObjPortsEarly{port}", And(ports, Not(phase3), AimIs(port)),
                $"\"We are ahead of the plan, Your Majesty. With Auremina and {other} already ours, the Staff goes straight for {port}. Take it " +
                "in this phase, and the war ends a month early.\"");
        }

        O("ObjPortsAgain", And(ports, lastMissed("Ports"), AimIs("Isor and Nanta")),
            "\"Isor and Nanta, still. This is the last phase, Your Majesty. Whatever we hold on that coast when it ends, we keep. Whatever we " +
            "don't, Wehlen keeps.\"");
        foreach ((string port, string other) in new[] { ("Isor", "Nanta"), ("Nanta", "Isor") })
        {
            O($"ObjPortsAgain{port}", And(ports, lastMissed("Ports"), AimIs(port)),
                $"\"{port}, Your Majesty. {other} is ours, so {port} is the last port they have. This is the last phase: whatever we hold on " +
                "that coast when it ends, we keep.\"");
        }

        O("ObjPortsL", And(ports, AimIs("Isor and Nanta")),
            "\"And now the last step of the campaign: Isor and Nanta. Every Wehzek convoy lands at one or the other. Take both, and their garrison " +
            "has nowhere left to go but home. The last phase of Northern Crown.\"");
        foreach ((string port, string other) in new[] { ("Isor", "Nanta"), ("Nanta", "Isor") })
        {
            O($"ObjPortsL{port}", And(ports, AimIs(port)),
                $"\"And now the last step of the campaign: {port}. With {other} ours, every Wehzek convoy lands there. Take it, and their " +
                "garrison has nowhere left to go but home. The last phase of Northern Crown.\"");
        }

        O("ObjWallAgain", And(Later, Obj(Objective.Wall), lastMissed("Wall")),
            "\"Auremina again, Your Majesty. The hills cost us last time, but this time we go in knowing where their guns are. And it is the " +
            "last phase: whatever we hold when the guns stop is where the observers will draw the line.\"");
        // The last phase goes for Auremina: the ports still Wehzek wait for the diplomats.
        string wallLast = And(Later, Obj(Objective.Wall), phase3);
        foreach (string left in new[] { "Isor and Nanta", "Isor", "Nanta", "" })
        {
            string wait = left == string.Empty ? "The ports are ours already." : $"{left} will have to wait for the diplomats.";
            O($"ObjWallLastHeld{Key(left)}", And(wallLast, Is(VarWaited), Eq(VarPortsLeft, left)),
                $"\"We held for a month, so the last phase goes for Auremina and its mines. {wait}\"");
            O($"ObjWallLast{Key(left)}", And(wallLast, Eq(VarPortsLeft, left)),
                $"\"Zille cost us more time than the plan allowed, so the last phase goes for Auremina and its mines. {wait}\"");
        }

        O("ObjWallL", And(Later, Obj(Objective.Wall)),
            "\"With Zille taken, the campaign moves on as planned: up the valley to Auremina and its mines. Take the hills, and their garrison in " +
            "the north is cut off from everything south of it. That is the next phase of Northern Crown.\"");
        O("ObjWall", Obj(Objective.Wall),
            "\"The objective is Auremina. Take the hills and the mines, and their garrison in the north is cut off from everything south of " +
            "it. That is the next phase of Northern Crown.\"");
        foreach ((string port, string other) in new[] { ("Isor", "Nanta"), ("Nanta", "Isor") })
        {
            O($"ObjPorts{port}", AimIs(port),
                $"\"The objective is {port}. With {other} ours, every Wehzek convoy lands there. Take it, and their garrison has nowhere left to " +
                "go but home. The last phase of Northern Crown.\"");
        }

        O("ObjPorts", null,
            "\"The objective is the northern coast: Isor and Nanta. Every Wehzek convoy lands at one or the other. Take both, and their " +
            "garrison has nowhere left to go but home. The last phase of Northern Crown.\"");
        t.Logic("Obj", objs.ToArray());
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
            "\"Now, the plan for the campaign. Whatever you decide today, the Staff will carry it through every phase of Northern Crown, until the last objective is taken or the war ends. " +
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
        string held(string city) => Is(ZilleOperation.CapturedVar(city));
        string both = AimIs("Isor and Nanta");
        C("CFeintWallCoast", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Wall), held("Isor")),
            "\"We feint first, as you ordered at the start. This time the noise goes along the coast, and they'll have to pull men out of " +
            "the hills to cover it.\"");
        C("CFeintWall", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Wall)),
            "\"We feint first, as you ordered at the start. This time the noise goes toward the coast road to Isor, and they'll have to pull men " +
            "out of the hills to cover it.\"");
        C("CFeintPorts", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Ports), both),
            "\"We feint first, as always. A great deal of noise in front of Nanta, and we'll see how much they leave behind in Isor.\"");
        C("CFeintIsor", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Ports), AimIs("Isor")),
            "\"We feint first, as always. A great deal of noise on the coast north of Isor, and we'll see how much they leave in the town.\"");
        C("CFeintNanta", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Ports), AimIs("Nanta")),
            "\"We feint first, as always. A great deal of noise on the hills south of Nanta, and we'll see how much they leave in the port.\"");
        C("CFeintHarborHills", Carlos, And(FormIs(Approach.Feint), Obj(Objective.Harbor), held("Auremina")),
            "\"We feint first, as you ordered. A show along the coast road toward Isor, and if they take the bait, Zille is left with its garrison.\"");
        C("CFeint", Carlos, FormIs(Approach.Feint),
            "\"We feint first, as you ordered. A show toward Auremina and the hills, and if they take the bait, Zille is left with its garrison.\"");
        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            C($"CArmorPorts{Key(aim)}", Carlos, And(FormIs(Approach.Armor), Obj(Objective.Ports), AimIs(aim)),
                $"\"Out of the hills, the tanks lead again, as you ordered. The coast road to {aim} is good tank country.\"");
        }

        C("CArmorPorts", Carlos, And(FormIs(Approach.Armor), Obj(Objective.Ports)),
            "\"Out of the hills, the tanks lead again, as you ordered. The coast road is good tank country.\"");
        C("CArmor", Carlos, FormIs(Approach.Armor),
            "\"The tanks lead, as you ordered. Massed on the river road, straight for the harbor.\"");
        foreach (string port in new[] { "Isor", "Nanta" })
        {
            C($"CLanding{port}", null, And(FormIs(Approach.Landing), Obj(Objective.Ports), AimIs(port)),
                $"\"The landing goes in as you planned it, Your Majesty. The marines come ashore beside {port} and cut the coast road behind it.\"");
        }

        C("CLandingPorts", null, And(FormIs(Approach.Landing), Obj(Objective.Ports)),
            "\"The landing goes in as you planned it, Your Majesty. The marines come ashore between Isor and Nanta and cut the coast road.\"");
        C("CLanding", null, FormIs(Approach.Landing),
            "\"The landing goes in as you planned it. The marines go into the bay while the army comes down the river.\"");
        C("CDropWall", null, And(FormIs(Approach.Airborne), Obj(Objective.Wall)),
            "\"The paratroopers go in the night before, as planned. This time on the ridges behind Auremina, to hold the road until the army comes up.\"");
        foreach (string port in new[] { "Isor", "Nanta" })
        {
            C($"CDrop{port}", null, And(FormIs(Approach.Airborne), Obj(Objective.Ports), AimIs(port)),
                $"\"The paratroopers go in the night before, as planned. They'll drop on the road behind {port} and hold it until the army comes up.\"");
        }

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
        foreach (string port in new[] { "Isor", "Nanta" })
        {
            C($"CDirect{port}", Carlos, And(Obj(Objective.Ports), AimIs(port)),
                $"\"Straight at them again, as you ordered at the start. Down the coast road to {port}, and into the port.\"");
        }

        C("CDirectPorts", Carlos, Obj(Objective.Ports),
            "\"Straight at them again, as you ordered at the start. Down the coast road to Isor, and on to Nanta.\"");
        C("CDirect", Carlos, null,
            "\"The plan stays as you set it at the start, Your Majesty. Straight at them and fast, before their armor can come up.\"");
        t.Logic("Carry", names.ToArray());

        t.Logic("CarryAsk", new[] { "CarryAsk3", "CarryAsk2" });
        t.Line("CarryAsk3", Carlos, "\"This is the last briefing of the campaign, Your Majesty. Do you want the whole picture before the last orders go out?\"",
            new[] { "CarryAskYes3", "CarryAskNo" }, condition: $"{Num(VarPhase)} >= 3");
        t.Line("CarryAsk2", Carlos, "\"The Staff has the month's picture ready, Your Majesty, if you want it before the orders go out.\"",
            new[] { "CarryAskYes", "CarryAskNo" });
        t.Choice("CarryAskYes", "\"Take me through the month.\"", new[] { "Hub" });
        t.Choice("CarryAskYes3", "\"Take me through it, one last time.\"", new[] { "Hub" });
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
            landing, AskedNavyWhy, Later,
            $"({IsNot(ZilleOperation.CapturedVar("Zille"))}) or ({IsNot(ZilleOperation.CapturedVar("Nanta"))})");

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
            "\"And the guns again. The Wehzeks have learned to sleep in their deepest bunkers, but it still costs them " +
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
        t.Line("Close2", Narrator, "The generals in the room gave me a military salute and answered as one. \"Yes, Your Majesty.\"",
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
            (And(Obj(Objective.Ports), AimIs("Isor")), "\"The orders go out tonight, Your Majesty. If Isor falls, the next time we meet in this room, it will be to talk about the peace.\""),
            (And(Obj(Objective.Ports), AimIs("Nanta")), "\"The orders go out tonight, Your Majesty. If Nanta falls, the next time we meet in this room, it will be to talk about the peace.\""),
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
        t.Logic("Matter", new[] { "MRest", "MMorale", "MSupply", "MSupplyWall", "MReinforcePales", "MReinforce", "MReinforceSouth", "Obj" });
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
            "advance is another mile for the fuel trucks.\"", new[] { "MSupplyA", "MSupplyB", "MSupplyC" }, condition: And(m("Supply"), Is(VarWallBroken)));
        t.Line("MSupplyWall", Carlos,
            "\"The quartermasters have a fair complaint. The whole army is fed down two roads to the wall, and their guns have both of them ranged to the yard.\"", new[] { "MSupplyD", "MSupplyC" }, condition: m("Supply"));
        t.Choice("MSupplyD", "\"Then the engineers cut a third road.\"", new[] { "MSupplyD2" });
        t.Line("MSupplyD2", Carlos, "\"A rough one, through the hills. But their guns haven't found it yet, and by the time they do, the trucks will be through.\"", new[] { "Obj" });
        t.Choice("MSupplyA", "\"Put the engineers on the bridges first. The army moves when the road does.\"", new[] { "MSupplyA2" });
        t.Line("MSupplyA2", Carlos, "\"Then the engineers go in behind the first wave. The bridges will be up before the trucks need them.\"", new[] { "Obj" });
        t.Choice("MSupplyB", "\"Use the Wehzek depots we've taken.\"", new[] { "MSupplyB2" }, condition: Is(ZilleOperation.CapturedVar("Zille")));
        t.Choice("MSupplyC", "\"Then the convoys move at night.\"", new[] { "MSupplyC2" });
        t.Line("MSupplyC2", Carlos, "\"Without lights, then. Slower, and the drivers will hate it, but more of the trucks will arrive.\"", new[] { "Obj" });
        t.Line("MSupplyB2", Carlos, "\"There is Wehzek fuel in the railway yards, and it burns just as well in our tanks. I'll have it counted tonight.\"",
            new[] { "Obj" });

        t.Line("MReinforcePales", Carlos,
            "\"The reserve classes have finished their training at Balle, Your Majesty, but half of them are already on the trains south to the " +
            "Palesian border. Zille gets what is left.\"", new[] { "MReinA", "MReinB" },
            condition: And(m("Reinforce"), "Variable[\"RiziaDLC.Pales_War\"] == true", IsNot(LostVar("Balle"))));
        t.Line("MReinforce", Carlos,
            "\"The reserve classes called up at the declaration have finished their training at Balle. Eleven thousand men, and most of them " +
            "have never heard a shot fired.\"", new[] { "MReinA", "MReinB" }, condition: And(m("Reinforce"), IsNot(LostVar("Balle"))));
        t.Line("MReinforceSouth", Carlos,
            "\"The reserve classes called up at the declaration have finished their training at Montaklar. Eleven thousand men, and most of them " +
            "have never heard a shot fired.\"", new[] { "MReinA", "MReinB" }, condition: m("Reinforce"));
        t.Choice("MReinA", "\"Put them in the quiet sectors first. Let them learn before they bleed.\"", new[] { "MReinA2" });
        t.Line("MReinA2", Carlos, "\"The veterans will be glad of the relief. I'll see it done.\"", new[] { "Obj" });
        t.Choice("MReinB", "\"Send them where they're needed. They'll learn fast.\"", new[] { "MReinB2" });
        t.Line("MReinB2", Carlos, "\"They will, Your Majesty. Some of them only once.\"", new[] { "Obj" });
    }
}
