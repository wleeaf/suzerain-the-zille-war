using HarmonyLib;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The supply war and the foreign capitals while the Zille war is fought. Each war turn, before the war table
/// opens, the King takes calls (story fragments named "ZilleWar.Pre_Call_&lt;Who&gt;_T&lt;turn&gt;"):
/// - Queen Beatrice (SupplyBeatrice.cs): the oil embargo priced in Rizian gas, Rumburgian war credits repaid per
///   turn, Bludish Freedom Front raids and fighters. Her patience runs out; the "Thornbourgh dossier" hurts.
/// - Prime Minister Alvarez (SupplyAlvarez.cs): the Lespian navy closes the Strait of Kreleck, for the MITZ vote and
///   a price (Aureus gas, Caleqabiz, money); later invoices, the double game with Pales, the Ramiro confrontation.
/// - The war room (SupplyWarRoom.cs): fleet, submarines, the Aureus squadron, the garrison's state.
/// - Other voices (SupplyVoices.cs): Chancellor Hegel's warnings, Smolak's ceasefire offer, Saltana's planes,
///   and the consulate files that name Héctor Ramiro.
/// News and reports for every consequence are in SupplyNews.cs, HUD modifiers in SupplyModifiers.cs.
/// </summary>
internal static partial class SupplyWar
{
    public enum Lever { Embargo, BffRaids, BffFighters, RoyalNavy, Submarines, Lespia }

    // ---- conversations
    private const string BeatriceConv = "ZilleWar/Calls/Beatrice";
    private const string AlvarezConv = "ZilleWar/Calls/Alvarez";
    private const string WarRoomConv = "ZilleWar/Calls/WarRoom";
    private const string HegelConv = "ZilleWar/Calls/Hegel";
    private const string SmolakConv = "ZilleWar/Calls/Smolak";
    private const string SaltanaConv = "ZilleWar/Calls/Saltana";
    private const string ConsulateConv = "ZilleWar/Calls/Consulate";
    private const string ConvPrefix = "ZilleWar/Calls/";

    // ---- speakers
    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Smolak = "Wiktor Smolak (R)";
    private const string Saltana = "Alma Saltana";
    private const string Lucita = "Lucita Azaro";
    private const string Carlos = "Carlos Robles Azaro";
    private const string Esquibel = "Laurento Esquibel";
    private const string Narrator = Talk.Narrator;

    // ---- vanilla variables
    internal const string Budget = "RiziaDLC.Resources_Budget";
    internal const string Energy = "RiziaDLC.Resources_Energy";
    internal const string Authority = "RiziaDLC.Resources_Authority";
    internal const string Ships = "RiziaDLC.Army_Unit_Ship_Total";
    internal const string Subs = "RiziaDLC.Army_Unit_Submarine_Total";
    internal const string Mercs = "RiziaDLC.Army_Unit_Mercenary_Total";
    internal const string Fighters = "RiziaDLC.Army_Unit_Fighter_Total";
    internal const string NpRumburg = "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower";
    internal const string NpWehlen = "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower";
    internal const string NpLespia = "RiziaDLC.Diplomacy_Lespia_NegotiatingPower";
    internal const string NpValgsland = "RiziaDLC.Diplomacy_Valgsland_NegotiatingPower";
    internal const string NpSordland = "RiziaDLC.Diplomacy_Sordland_NegotiatingPower";
    internal const string NpPales = "RiziaDLC.Diplomacy_Pales_NegotiatingPower";
    internal const string NpMorella = "RiziaDLC.Diplomacy_Morella_NegotiatingPower";
    internal const string HighExport = "RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport";
    internal const string AverageExport = "RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport";
    internal const string ExportFailed = "RiziaDLC.Situation_Diplomacy_EnergyCommitment_Rumburg_Failed";
    internal const string PalesWar = "RiziaDLC.Pales_War";
    internal const string LucitaArrested = "RiziaDLC.Lucita_Arrested";
    internal const string BlockDrillship = "RiziaDLC.Diplomacy_Pales_Military_BlockDrillship";
    internal const string GasFieldBuy = "RiziaDLC.Diplomacy_Pales_GasField_Buy";
    internal const string GasFieldBuyCancelled = "RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled";
    internal const string MitzPromise = "RiziaDLC.Diplomacy_Lespia_PromiseITZSupport";
    internal const string MitzPromiseLate = "RiziaDLC.Diplomacy_Lespia_PromiseITZSupport_Late";
    internal const string VoteNationalization = "RiziaDLC.Turn05_ITZVote_Nationalization";
    internal const string VoteRenegotiation = "RiziaDLC.Turn05_ITZVote_Renegotiation";
    internal const string VoteStatusQuo = "RiziaDLC.Turn05_ITZVote_StatusQuo";
    internal const string LespianPalesSupport = "RiziaDLC.War_Pales_LespianSupport";
    internal const string TurnLua = "(Variable[\"RiziaDLCSetup.CurrentTurn\"] or 0)";
    internal const string Extractions = "RiziaDLC.Policy_Economy_AureusField_Extractions";   // Aureus gas flowing (Turn 6 deal)
    internal const int LastTurn = 10;

    // ---- our state: read these from other modules
    /// <summary>Budget still owed to Rumburg for war credits (int, 0 when repaid).</summary>
    public const string VarRumburgDebt = "ZilleWar.Rumburg_CreditDebt";
    /// <summary>Number of Rumburgian war credits taken (0..2).</summary>
    public const string VarRumburgCredits = "ZilleWar.Rumburg_Credits";
    /// <summary>The HUD value of the repayment line (negative while repaying), shown by WorldModifiers.</summary>
    public const string ModRumburgCredit = WorldModifiers.RumburgRepayment;
    /// <summary>How many more favours Beatrice will grant (int). 0 or less: she says no at the next call.</summary>
    public const string VarRumburgPatience = "ZilleWar.Rumburg_Patience";
    /// <summary>Beatrice has refused further help; her calls stop.</summary>
    public const string VarRumburgSaidNo = "ZilleWar.Rumburg_SaidNo";
    /// <summary>Beatrice asked the King to end the war ("an end date").</summary>
    public const string VarRumburgWantsCeasefire = "ZilleWar.Rumburg_WantsCeasefire";
    /// <summary>Wehlen published the "Thornbourgh dossier" on Rumburg's arms for the Bludish Freedom Front.</summary>
    public const string VarDossier = "ZilleWar.Rumburg_DossierPublished";

    /// <summary>The Lespian navy currently keeps the Strait of Kreleck closed to Wehzek shipping.</summary>
    public const string VarLespiaStrait = "ZilleWar.Lespia_StraitClosed";
    /// <summary>Lespia was given naval rights at Caleqabiz for the war (VanillaLate: gate the T8 Caleqabiz lease).</summary>
    public const string VarLespiaCaleqabiz = "ZilleWar.Lespia_Caleqabiz";
    /// <summary>Rizia sold Aureus gas to LesPower at Alvarez's price.</summary>
    public const string VarLespiaGasDeal = "ZilleWar.Lespia_GasDeal";
    /// <summary>Rizia bound its MITZ vote to Lespia for the strait (also sets the vanilla promise flags).</summary>
    public const string VarLespiaMitz = "ZilleWar.Lespia_MitzBound";
    /// <summary>Rizia paid cash for the strait.</summary>
    public const string VarLespiaPaid = "ZilleWar.Lespia_PaidBudget";
    /// <summary>Lespia's navy has left the strait for good (Ramiro published, or the King sent it away).</summary>
    public const string VarLespiaWithdrawn = "ZilleWar.Lespia_Withdrawn";
    /// <summary>The consulate files naming Héctor Ramiro have been found.</summary>
    public const string VarRamiroFound = "ZilleWar.Ramiro_Found";
    /// <summary>"publish" | "confront" | "leverage" | "bury".</summary>
    public const string VarRamiro = "ZilleWar.Ramiro_Choice";

    /// <summary>Valgsland's warnings: 0 none, 1 Hegel's cable answered, 2 Hegel's call answered.</summary>
    public const string VarValgslandStage = "ZilleWar.Valgsland_Stage";
    /// <summary>Answer to the cable: "guarantee" | "defied" | "mitz" | "stalled".</summary>
    public const string VarValgslandCable = "ZilleWar.Valgsland_CableAnswer";
    /// <summary>Answer to the call: "yielded" | "defied".</summary>
    public const string VarValgslandCall = "ZilleWar.Valgsland_CallAnswer";
    /// <summary>Valgslandian ports closed to Rizian cargo while the war lasts (budget line).</summary>
    public const string VarValgslandPorts = "ZilleWar.Valgsland_PortsClosed";
    public const string ModValgsland = "ZilleWar.Mod_ValgslandPorts";
    /// <summary>The King promised Hegel to keep the submarines out of the strait.</summary>
    public const string VarSubsGrounded = "ZilleWar.Navy_SubmarinesGrounded";
    /// <summary>The Aureus squadron was pulled north for the war.</summary>
    public const string VarAureusRecalled = "ZilleWar.Navy_AureusRecalled";

    /// <summary>Smolak's offer: "accepted" | "countered" | "refused".</summary>
    public const string VarSmolakAnswer = "ZilleWar.Smolak_CeasefireAnswer";
    /// <summary>The King turned down Smolak's ceasefire: +1 power projection (WorldModifiers), unless the war is then lost.</summary>
    public const string VarSmolakRefused = "ZilleWar.Smolak_RefusedCeasefire";
    /// <summary>Terms of an accepted ceasefire: "full" (Rico, Bludish ban, Wehzek rights) | "reduced" (ban, rights) | "rights".</summary>
    public const string VarCeasefireTerms = "ZilleWar.Ceasefire_Terms";
    /// <summary>Morellan aircraft bought without markings.</summary>
    public const string VarMorellaAircraft = "ZilleWar.Morella_Aircraft";

    // ---- internal bookkeeping
    private const string VarLastOfferTurn = "ZilleWar.Calls_LastTurn";
    private const string VarUpkeepTurn = "ZilleWar.Supply_UpkeepTurn";
    private const string VarEmbargoCounted = "ZilleWar.SupplyWar_EmbargoCounted";
    private const string VarBeatriceCalls = "ZilleWar.Call_Beatrice_Count";
    private const string VarAlvarezCalls = "ZilleWar.Call_Alvarez_Count";
    private const string VarWarRoomMeetings = "ZilleWar.Call_WarRoom_Count";
    private const string VarSubPatrols = "ZilleWar.Call_SubPatrolsSinceLoss";
    private const string VarPatienceSet = "ZilleWar.Rumburg_PatienceSet";
    private const string VarTwoWarsPenaltyTurn = "ZilleWar.Rumburg_TwoWarsTurn";
    private const string VarLespiaDealTurn = "ZilleWar.Lespia_DealTurn";
    private const string VarStraitThin = "ZilleWar.Lespia_StraitThin";
    private const string VarGrudgeApplied = "ZilleWar.Lespia_GasGrudgeApplied";
    private const string VarStraitTurns = "ZilleWar.Lespia_StraitTurns";
    private const string VarRamiroConfronted = "ZilleWar.Ramiro_Confronted";
    private const string VarRamiroOutcome = "ZilleWar.Ramiro_ConfrontOutcome";   // "recalled" | "compensated" | "sentaway" | "free"
    private const string VarAlvarezPalesDone = "ZilleWar.Alvarez_PalesTopic";      // "paid" | "leverage" | "ignored"
    private const string VarAlvarezGasDone = "ZilleWar.Alvarez_GasTopic";          // "paid" | "refused"
    private const string VarAlvarezGiftDone = "ZilleWar.Alvarez_GiftDone";
    private const string VarValgslandStageTurn = "ZilleWar.Valgsland_StageTurn";
    private const string VarSmolakCalls = "ZilleWar.Smolak_Calls";
    private const string VarSmolakTurn = "ZilleWar.Smolak_LastTurn";
    private const string VarSaltanaCalled = "ZilleWar.Saltana_Called";
    private const string VarSaltanaGulf = "ZilleWar.Saltana_GulfFavour";
    private const string VarPhotoBoats = "ZilleWar.WarRoom_PhotoBoats";            // "shadow" | "ignore"
    private const string VarDefector = "ZilleWar.WarRoom_Defector";
    /// <summary>The Bludish companies' massacre near Zille: "trial" | "home" | "kept" ("" before it happens).</summary>
    public const string VarBludsIncident = "ZilleWar.WarRoom_BludsIncident";
    /// <summary>What came of the Lespian photographs: "raid" | "expelled" | "silent" | "returned" | "kept".</summary>
    public const string VarPhotoPayoff = "ZilleWar.WarRoom_PhotoPayoff";
    private const string VarBffFirstTurn = "ZilleWar.Rumburg_BffFirstTurn";
    private const string VarPriorityApplied = "ZilleWar.Rumburg_PriorityApplied";
    private const string VarWarnedApplied = "ZilleWar.Rumburg_WarnedApplied";

    // ---- the war as the calls see it, set once per turn before the calls (SupplyWar.PrepareCallState)
    /// <summary>Phases fought so far when this turn's calls were offered.</summary>
    internal const string VarCallFought = "ZilleWar.Call_Fought";
    /// <summary>The phase (1..3) of the last offensive fought, 0 before the first.</summary>
    internal const string VarCallLastPhase = "ZilleWar.Call_LastPhase";
    /// <summary>The last offensive fought reached its aim.</summary>
    internal const string VarCallLastWon = "ZilleWar.Call_LastWon";
    /// <summary>The aim of the last offensive fought: "Harbor" | "Wall" | "Ports" ("" before the first).</summary>
    internal const string VarCallLastAim = "ZilleWar.Call_LastAim";
    /// <summary>The last offensive's cost: 0 light, 1 heavy, 2 severe.</summary>
    internal const string VarCallLossTier = "ZilleWar.Call_LossTier";
    /// <summary>The King held the front last turn (the phase was skipped).</summary>
    internal const string VarCallHeld = "ZilleWar.Call_Held";
    /// <summary>The last offensive took more cities than its aim.</summary>
    internal const string VarCallExtra = "ZilleWar.Call_Extra";
    /// <summary>Wehzek cities (of Zille, Auremina, Isor, Nanta) in Rizian hands.</summary>
    internal const string VarCallCities = "ZilleWar.Call_Cities";
    /// <summary>A Rizian border town is in Wehzek hands.</summary>
    internal const string VarCallOwnLost = "ZilleWar.Call_OwnLost";
    /// <summary>How the last phase went (Front.Kind): "won" | "wonLoss" | "partial" | "other" | "mixed" | "missed" | "lost".</summary>
    internal const string VarCallKind = "ZilleWar.Call_Kind";
    /// <summary>The city the last phase won, to name on the phone: the part of the aim taken, else the first city taken.</summary>
    internal const string VarCallGain = "ZilleWar.Call_Gain";
    /// <summary>The city the last phase lost, to name on the phone ("" when none).</summary>
    internal const string VarCallLoss = "ZilleWar.Call_Loss";
    /// <summary>The part of the last phase's aim it did not take ("Isor"; "" when the aim was taken).</summary>
    internal const string VarCallMissed = "ZilleWar.Call_Missed";

    // ---- what was said on the previous calls (read back on the next one)
    private const string VarBeaGave = "ZilleWar.Call_Beatrice_Gave";             // "" | "none" | "embargo" | "credit" | "raids" | "fighters"
    private const string VarBeaGavePrev = "ZilleWar.Call_Beatrice_GavePrev";
    private const string VarBeaPromise = "ZilleWar.Call_Beatrice_Promise";       // "" | "zille" | "end"
    private const string VarBeaPromisePrev = "ZilleWar.Call_Beatrice_PromisePrev";
    private const string VarBeaToldDossier = "ZilleWar.Call_Beatrice_ToldDossier";
    private const string VarBeaToldBluds = "ZilleWar.Call_Beatrice_ToldBluds";
    private const string VarBeaNoEmbargo = "ZilleWar.Call_Beatrice_RefusedEmbargo";
    private const string VarBeaNoEmbargoCount = "ZilleWar.Call_Beatrice_RefusedEmbargoCount";
    private const string VarBeaNoCredit = "ZilleWar.Call_Beatrice_RefusedCredit";
    private const string VarBeaNoRaids = "ZilleWar.Call_Beatrice_RefusedRaids";
    private const string VarBeaNoFighters = "ZilleWar.Call_Beatrice_RefusedFighters";
    private const string VarBeaAskedEmbWhy = "ZilleWar.Call_Beatrice_AskedEmbargoWhy";
    private const string VarBeaAskedInterest = "ZilleWar.Call_Beatrice_AskedInterest";
    private const string VarBeaAskedRaidWhy = "ZilleWar.Call_Beatrice_AskedRaidWhy";
    private const string VarBeaAskedFigWhy = "ZilleWar.Call_Beatrice_AskedFightersWhy";
    private const string VarAlvRefusedStrait = "ZilleWar.Call_Alvarez_RefusedStrait";
    private const string VarAlvRefusedMitz = "ZilleWar.Call_Alvarez_RefusedMitz";
    private const string VarAlvRefusals = "ZilleWar.Call_Alvarez_RefusalCount";
    private const string VarAlvAskedWhy = "ZilleWar.Call_Alvarez_AskedWhy";
    private const string VarAlvAskedMitzWhy = "ZilleWar.Call_Alvarez_AskedMitzWhy";
    private const string VarAlvInvoices = "ZilleWar.Call_Alvarez_Invoices";
    private const string VarAlvJabbed = "ZilleWar.Call_Alvarez_Jabbed";

    // ---- read from the Turns 4-7 module
    private const string VarRumburgPriority = "ZilleWar.E7_RumburgPriority";   // "zille" | "pales" | "both"
    private const string VarRumburgWarned = "ZilleWar.E7_RumburgWarned";
    private const string VarRumburgPromise = EarlyT7.VarRumburgPromise;   // "quick" | "repay" | "none"
    private const string VarLespiaBetrayedT5 = "ZilleWar.E5_LespiaBetrayed";
    private const string VarAlvarezGasPriceT6 = "ZilleWar.E6_AlvarezGasPrice"; // "paid" | "refused" | "caleqabiz"

    /// <summary>Times each lever was pulled.</summary>
    public static string Count(Lever l) => $"ZilleWar.Lever_{l}";

    private static string ReportedVar(Lever l) => $"ZilleWar.LeverReported_{l}";

    /// <summary>Rumburg buys less Wehzek oil than before the war, or none (the Turn 3 deal halved or cut it; the telephone cut it).</summary>
    public static bool RumburgEmbargo => RumburgOilCut || RumburgOilHalved;

    /// <summary>Rumburg buys no Wehzek oil at all: the Turn 3 deal cut it off ("Cut off Wehlen completely"), or Beatrice did on the telephone.</summary>
    public static bool RumburgOilCut =>
        !Variables.GetBool(ExportFailed) && (Variables.GetBool(HighExport) || Variables.GetInt(Count(Lever.Embargo)) > 0);

    /// <summary>Rumburg still buys half its old Wehzek oil (the Turn 3 deal: "Can you halve your imports?").</summary>
    public static bool RumburgOilHalved =>
        !Variables.GetBool(ExportFailed) && Variables.GetBool(AverageExport) && !RumburgOilCut;

    internal const string LuaOilCut =
        "(Variable[\"RiziaDLC.Situation_Diplomacy_EnergyCommitment_Rumburg_Failed\"] ~= true and (Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or (Variable[\"ZilleWar.Lever_Embargo\"] or 0) > 0))";
    internal const string LuaOilHalved =
        "(Variable[\"RiziaDLC.Situation_Diplomacy_EnergyCommitment_Rumburg_Failed\"] ~= true and Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true and not " + LuaOilCut + ")";

    private static readonly string[] IntVars =
    {
        VarRumburgDebt, VarRumburgCredits, ModRumburgCredit, VarRumburgPatience, ModValgsland, VarValgslandStage,
        VarBeatriceCalls, VarBeaNoEmbargoCount, VarAlvRefusals, VarAlvarezCalls, VarWarRoomMeetings, VarSubPatrols, VarTwoWarsPenaltyTurn,
        VarLespiaDealTurn, VarStraitTurns, VarValgslandStageTurn, VarSmolakCalls, VarSmolakTurn, VarLastOfferTurn,
        VarUpkeepTurn, VarBffFirstTurn, VarCallFought, VarCallLastPhase, VarCallLossTier, VarCallCities, VarAlvInvoices,
    };

    private static readonly string[] BoolVars =
    {
        VarEmbargoCounted, VarPatienceSet, VarRumburgSaidNo, VarRumburgWantsCeasefire, VarDossier, VarLespiaStrait,
        VarLespiaCaleqabiz, VarLespiaGasDeal, VarLespiaMitz, VarLespiaPaid, VarLespiaWithdrawn, VarRamiroFound,
        VarRamiroConfronted, VarStraitThin, VarAlvarezGiftDone, VarValgslandPorts, VarSubsGrounded, VarAureusRecalled,
        VarMorellaAircraft, VarSaltanaCalled, VarSaltanaGulf, VarDefector, VarPriorityApplied, VarWarnedApplied, VarGrudgeApplied,
        VarCallLastWon, VarCallHeld, VarCallExtra, VarCallOwnLost, VarBeaToldDossier, VarBeaToldBluds, VarBeaNoEmbargo, VarBeaNoCredit,
        VarBeaNoRaids, VarBeaNoFighters, VarBeaAskedEmbWhy, VarBeaAskedInterest, VarBeaAskedRaidWhy, VarBeaAskedFigWhy, VarAlvRefusedStrait,
        VarAlvRefusedMitz, VarAlvAskedWhy, VarAlvAskedMitzWhy, VarAlvJabbed, VarSmolakRefused,
    };

    private static readonly string[] StringVars =
    {
        VarRamiro, VarRamiroOutcome, VarAlvarezPalesDone, VarAlvarezGasDone, VarValgslandCable, VarValgslandCall,
        VarSmolakAnswer, VarCeasefireTerms, VarPhotoBoats, VarBludsIncident, VarPhotoPayoff, VarCallLastAim, VarBeaGave, VarBeaGavePrev,
        VarCallKind, VarCallGain, VarCallLoss, VarCallMissed,
        VarBeaPromise, VarBeaPromisePrev,
    };

    /// <summary>Vanilla numbers our scripts do arithmetic on.</summary>
    private static readonly string[] VanillaNumbers =
    {
        Budget, Energy, Authority, Ships, Subs, Mercs, Fighters, NpRumburg, NpWehlen, NpLespia, NpValgsland, NpSordland, NpPales,
        NpMorella, "RiziaDLC.Energy_Modifier_RumburgEnergyExport", "RiziaDLC.Budget_Modifier_RumburgTrade", LespianPalesSupport,
        "RiziaDLC.Country_GlobalPerception",
    };

    public static void Register()
    {
        foreach (Lever l in Enum.GetValues<Lever>())
        {
            Variables.Register(Count(l));
            Variables.Register(ReportedVar(l));
        }

        foreach (string v in IntVars.Concat(BoolVars).Concat(StringVars).Concat(new[] { VarAskedAll }).Where(v => v != ModRumburgCredit))
        {
            Variables.Register(v);   // ModRumburgCredit is WorldModifiers' variable and registered there
        }

        foreach (string v in Asked)
        {
            Variables.Register(v);
        }

        foreach (string v in BoolVars)
        {
            Vanilla.Expose(v);
        }

        foreach (string v in new[] { VarRumburgDebt, VarRumburgCredits, ModRumburgCredit, ModValgsland, VarValgslandStage })
        {
            Vanilla.Expose(v, isBool: false);
        }

        foreach (string c in new[] { BeatriceConv, AlvarezConv, WarRoomConv, HegelConv, SmolakConv, SaltanaConv, ConsulateConv })
        {
            ConversationRegistry.RegisterConversation(c);
        }

        RegisterGrudges();
        RegisterBeatrice();
        RegisterAlvarez();
        RegisterWarRoom();
        RegisterHegel();
        RegisterSmolak();
        RegisterSaltana();
        RegisterConsulate();
        SupplyNews.Register();

        // Our own step hook (after WarFlow's, which subscribed earlier): upkeep, modifiers, calls, news.
        Events.OnEvaluateStep += (_, _) => OnStep();
    }

    /// <summary>At the declaration: an embargo already in force bites at once.</summary>
    public static void OnWarDeclared()
    {
        EnsureNumbers();
        if (RumburgEmbargo && !Variables.GetBool(VarEmbargoCounted))
        {
            Variables.Set(VarEmbargoCounted, true);
            ZilleCampaign.Strength -= 1;
        }
    }

    /// <summary>Lua cannot do arithmetic on unset variables, so every number the calls touch gets one first.</summary>
    public static void EnsureNumbers()
    {
        ZilleCampaign.Strength = ZilleCampaign.Strength;
        foreach (string v in Enum.GetValues<Lever>().Select(Count).Concat(IntVars).Concat(VanillaNumbers))
        {
            // Only fill in missing values; vanilla resources can hold fractions that must not be rounded.
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = 0 end");
        }

        foreach (string v in BoolVars)
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = false end");
        }

        foreach (string v in StringVars)
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = \"\" end");
        }
    }

    private static int Turn => GameState.CurrentTurnNum ?? 0;

    /// <summary>War turns since the declaration (0 on the turn of the declaration).</summary>
    private static int WarTurn => Turn - Math.Max(1, Variables.GetInt(AnDeclaration.VarDeclaredTurn));

    /// <summary>Every story step (self-subscribed; also safe to call from WarFlow/WarCouncil): upkeep, modifiers, calls, news. Idempotent.</summary>
    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !Variables.GetBool(AnDeclaration.VarDeclared))
        {
            return;
        }

        foreach ((string what, Action step) in new (string, Action)[]
                 {
                     ("numbers", EnsureNumbers), ("grudges", Grudges), ("upkeep", Upkeep), ("gas contract", GasContract), ("modifiers", SupplyModifiers.Install), ("calls", Offer),
                     ("levers", ReportNewLevers), ("news", SupplyNews.OnStep),
                 })
        {
            try
            {
                step();
            }
            catch (Exception ex)
            {
                Core.Log.Error($"Supply war {what} failed: {ex}");
            }
        }
    }

    // ------------------------------------------------------------------ per turn

    /// <summary>
    /// Alvarez's "my admirals do get bored at Kreleck" when the King refused his gas price at the Turn 6 ceremony (EarlyT6):
    /// the next turn his patrols in the strait are thinner, exactly as he warned.
    /// </summary>
    private static void Grudges()
    {
        // the refusal of Smolak's ceasefire (also for saves made before the flag existed)
        if (Str(VarSmolakAnswer) == "refused" && !Variables.GetBool(VarSmolakRefused))
        {
            Variables.Set(VarSmolakRefused, true);
        }

        if (Str(VarAlvarezGasPriceT6) == "refused" && !Variables.GetBool(VarGrudgeApplied) && Variables.GetBool(VarLespiaStrait))
        {
            Variables.Set(VarGrudgeApplied, true);
            Variables.Set(VarStraitThin, true);
        }

        ForeignGrudges();
    }

    /// <summary>
    /// Lespia's price for the strait: six units of Aureus gas to LesPower every turn for the price of two, from the turn the gas
    /// flows. Shown and charged through WorldModifiers' LesPower lines.
    /// </summary>
    private static void GasContract()
    {
        bool owed = Variables.GetBool(VarLespiaGasDeal) && Variables.GetBool(Extractions);
        Variables.Set(WorldModifiers.LespiaGas, owed ? -6 : 0);
        Variables.Set(WorldModifiers.LespiaGasBudget, owed ? 2 : 0);
    }

    /// <summary>Once per new turn: the strait closure bites, credits are repaid, the dossier lands.</summary>
    private static void Upkeep()
    {
        int turn = Turn;
        int last = Variables.GetInt(VarUpkeepTurn);
        if (turn <= 0 || turn <= last)
        {
            return;
        }

        Variables.Set(VarUpkeepTurn, turn);
        if (last > 0)
        {
            NewTurn(turn);
        }
    }

    private static void NewTurn(int turn)
    {
        bool atWar = WarFlow.AtWar;

        // The Lespian navy in the strait: every turn it stays closed, fewer convoys reach Zille.
        if (atWar && Variables.GetBool(VarLespiaStrait) && turn > Variables.GetInt(VarLespiaDealTurn))
        {
            if (!Variables.GetBool(VarStraitThin))
            {
                ZilleCampaign.Strength -= 1;
                Variables.Set(VarStraitTurns, Variables.GetInt(VarStraitTurns) + 1);
            }

            Variables.Set(VarStraitThin, false);
        }

        // Rumburgian war credit: the repayment line counts itself down (WorldModifiers "_Turns"); keep the debt figure in step.
        int left = Variables.GetInt(ModRumburgCredit + "_Turns");
        Variables.Set(VarRumburgDebt, left > 0 ? left * -Variables.GetInt(ModRumburgCredit) : 0);

        // Wraclawitz publishes its file on Rumburg and the BFF a turn after the Bluds first strike for Rizia.
        bool bff = Variables.GetInt(Count(Lever.BffRaids)) + Variables.GetInt(Count(Lever.BffFighters)) > 0;
        if (atWar && bff && !Variables.GetBool(VarDossier) && turn > Variables.GetInt(VarBffFirstTurn))
        {
            Variables.Set(VarDossier, true);
            Variables.Set(VarRumburgPatience, Variables.GetInt(VarRumburgPatience) - 1);
        }

        // Valgslandian ports reopen when the war is over.
        if (!atWar && Variables.GetBool(VarValgslandPorts))
        {
            Variables.Set(VarValgslandPorts, false);
            Variables.Set(ModValgsland, 0);
        }
    }

    // ------------------------------------------------------------------ the calls

    /// <summary>
    /// Once per war turn, before the war table: the calls that have something to say. Safe to call more than once
    /// per step (WarCouncil should call it before it checks for pending "ZilleWar.Pre_" fragments).
    /// </summary>
    public static void Offer()
    {
        if (!WarFlow.AtWar || !Variables.GetBool(AnDeclaration.VarDeclared))
        {
            return;
        }

        EnsureNumbers();
        int turn = Turn;
        if (turn <= 0 || Variables.GetInt(VarLastOfferTurn) >= turn)
        {
            return;
        }

        if (Variables.GetInt(WarFlow.VarLastOperationTurn) >= turn || ZilleCampaign.Available().Count == 0)
        {
            return;     // this turn's operation is already under way, or nothing is left to fight for
        }

        Variables.Set(VarLastOfferTurn, turn);
        int w = WarTurn;
        PrepareCallState(turn);
        PrepareBeatrice(turn, w);

        if (!Variables.GetBool(VarRumburgSaidNo))
        {
            AddCall(BeatriceConv, "Beatrice", SuzerainTokenName.RiziaCityPorteDrazon, "A Call from Thornbourgh", "Palas Rezna, Porte Drazon");
        }

        if (AlvarezHasSomethingToSay(w))
        {
            AddCall(AlvarezConv, "Alvarez", SuzerainTokenName.RiziaCityMonqiz, "Call With Prime Minister Alvarez", "Palas Toras, Monqiz");
        }

        // The fleet and the strait are settled at the War Council itself (SupplyWarRoom.cs builds that part of it).

        // At most two other voices per turn, most urgent first.
        int events = 0;
        foreach ((Func<bool> when, Action add) in new (Func<bool>, Action)[]
                 {
                     (() => ConsulateReady(w), () => AddCall(ConsulateConv, "Consulate", SuzerainTokenName.RiziaCityPorteDrazon,
                         "The Consulate Files", "Palas Rezna, Porte Drazon", ConversationData.ConversationType.SecurityAndOrder)),
                     (() => SmolakReady(turn), () => AddCall(SmolakConv, "Smolak", SuzerainTokenName.RiziaCityPorteDrazon,
                         "A Call from Wraclawitz", "Palas Rezna, Porte Drazon")),
                     (() => HegelCallReady(turn), () => AddCall(HegelConv, "Hegel", SuzerainTokenName.RiziaCityPorteDrazon,
                         "Call from Chancellor Hegel", "Palas Rezna, Porte Drazon")),
                     (() => HegelCableReady(w), () => AddCall(HegelConv, "Hegel", SuzerainTokenName.RiziaCityPorteDrazon,
                         "A Cable from Valgsland", "Palas Rezna, Porte Drazon")),
                     (() => SaltanaReady(turn, w), () => AddCall(SaltanaConv, "Saltana", SuzerainTokenName.RiziaCityPorteDrazon,
                         "Call With Prime Minister Saltana", "Palas Rezna, Porte Drazon")),
                 })
        {
            if (events < 2 && when())
            {
                add();
                events++;
            }
        }
    }

    private static readonly string[] RizianTowns = { "Mave Nortus", "Dapalle", "Balle", "Jales" };

    /// <summary>
    /// The war as it stands when this turn's calls are offered (before this turn's phase): the last phase fought, its aim,
    /// result and cost, whether the King held last month, how many Wehzek cities are ours. Every call reads these to open,
    /// react and close differently from the last one.
    /// </summary>
    private static void PrepareCallState(int turn)
    {
        int k = ZilleCampaign.OperationsDone;
        Variables.Set(VarCallFought, k);
        Front.Phase last = Front.Of(k);
        string aim = last?.Objective.ToString() ?? string.Empty;
        bool won = last?.Won ?? false;
        Variables.Set(VarCallLastAim, aim);
        Variables.Set(VarCallLastWon, won);
        Variables.Set(VarCallLastPhase, last?.Number ?? 0);
        int playerDead = k > 0 ? Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)) : 0;
        int enemyDead = k > 0 ? Variables.GetInt(ZilleCampaign.EnemyDeadVar(k)) : 0;
        Variables.Set(VarCallLossTier, playerDead >= 6 || (playerDead >= 3 && playerDead > enemyDead) ? 2 : playerDead >= 3 || playerDead > enemyDead ? 1 : 0);
        int held = Variables.GetInt(WarCouncil.VarHeldTurn);
        Variables.Set(VarCallHeld, held > 0 && held == turn - 1);

        Variables.Set(VarCallCities, Front.Held.Count);
        // More cities than the aim, in a phase that took its aim and lost nothing ("two cities for the price of one").
        Variables.Set(VarCallExtra, last != null && last.Kind == Front.Kind.Won && last.Extra.Count > 0);
        string kind = last == null ? string.Empty : last.Kind switch
        {
            Front.Kind.Won => "won", Front.Kind.WonLoss => "wonLoss", Front.Kind.Partial => "partial", Front.Kind.Other => "other",
            Front.Kind.Mixed => "mixed", Front.Kind.Missed => "missed", _ => "lost",
        };
        Variables.Set(VarCallKind, kind);
        string gain = last == null ? string.Empty
            : last.AimTaken.FirstOrDefault() ?? last.CitiesGained.FirstOrDefault() ?? last.TownsRetaken.FirstOrDefault() ?? string.Empty;
        Variables.Set(VarCallGain, gain);
        Variables.Set(VarCallLoss, last == null ? string.Empty : last.CitiesLost.FirstOrDefault() ?? last.TownsLost.FirstOrDefault() ?? string.Empty);
        Variables.Set(VarCallMissed, last == null || last.Won ? string.Empty : Front.Join(last.AimMissed));

        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        Variables.Set(VarCallOwnLost, RizianTowns.Any(c => ZilleCampaign.TownTile(c) is { } tile && ZilleCampaign.IsEnemy(owners, tile)));
    }

    private static void PrepareBeatrice(int turn, int w)
    {
        if (!Variables.GetBool(VarPatienceSet))
        {
            int np = Variables.GetInt(NpRumburg);
            int patience = 3 + (np >= 2 ? 1 : 0) - (np <= -1 ? 1 : 0) + (Variables.GetBool(HighExport) ? 1 : 0)
                           + (Variables.GetString(AnDeclaration.VarRumburgCable) == "grateful" ? 1 : 0);
            Variables.Set(VarRumburgPatience, patience);
            Variables.Set(VarPatienceSet, true);
        }

        // Beatrice's "Zille or Pales?" in Thornbourgh (EarlyT7Diplomacy): her help tapers, or stops for Zille.
        string priority = Str(VarRumburgPriority);   // unset reads "nil", which must not count as an answer
        if (priority.Length > 0 && !Variables.GetBool(VarPriorityApplied))
        {
            Variables.Set(VarPriorityApplied, true);
            int p = Variables.GetInt(VarRumburgPatience);
            Variables.Set(VarRumburgPatience, priority == "pales" ? Math.Min(p, 0) : priority == "both" ? p - 1 : p);
        }

        if (Variables.GetBool(VarRumburgWarned) && !Variables.GetBool(VarWarnedApplied))
        {
            Variables.Set(VarWarnedApplied, true);
            Variables.Set(VarRumburgPatience, Variables.GetInt(VarRumburgPatience) - 1);
        }

        // Rumburg carrying two of Rizia's wars wears her out faster.
        if (w >= 1 && Variables.GetBool(PalesWar) && Variables.GetInt(VarTwoWarsPenaltyTurn) < turn)
        {
            Variables.Set(VarTwoWarsPenaltyTurn, turn);
            Variables.Set(VarRumburgPatience, Variables.GetInt(VarRumburgPatience) - 1);
        }
    }

    private static bool AlvarezHasSomethingToSay(int w)
    {
        if (Variables.GetBool(VarLespiaWithdrawn))
        {
            return false;
        }

        if (Variables.GetString(VarRamiro) == "confront" && !Variables.GetBool(VarRamiroConfronted))
        {
            return true;
        }

        // Rizia broke faith at the Meftiem since his last call: he rings to say what it will cost.
        if (GrievanceUnsaid)
        {
            return true;
        }

        if (Variables.GetInt(Count(Lever.Lespia)) == 0)
        {
            return w >= 1 || Variables.GetString(AnDeclaration.VarLespianNote) == "kept";
        }

        // The strait is bought; the Meftiem is a separate bargain he still wants to make (until the board's last vote).
        bool voted = Variables.GetBool(VoteNationalization) || Variables.GetBool(VoteRenegotiation) || Variables.GetBool(VoteStatusQuo);
        bool bound = (Variables.GetBool(VarLespiaMitz) || (Variables.GetBool(MitzPromise) && !voted)) && !Variables.GetBool(MitzScrewed);
        bool mitzOpen = !bound && !Variables.GetBool(MitzPromiseLate) && !Variables.GetBool(ValgslandMitz) && Turn <= 8;
        if (mitzOpen)
        {
            return true;
        }

        bool palesTopic = Variables.GetBool(PalesWar) && Variables.GetInt(LespianPalesSupport) > 0 &&
                          Str(VarAlvarezPalesDone).Length == 0;
        const bool gasTopic = false;   // the gas is a standing per-turn contract
        bool photoTopic = Str(VarPhotoPayoff) == "expelled" && !Variables.GetBool(VarAlvarezPhotoSaid);
        return palesTopic || gasTopic || photoTopic || !Variables.GetBool(VarAlvarezGiftDone);
    }

    private static bool ConsulateReady(int w) =>
        w >= 1 && Variables.GetBool(WarFlags.ZilleCityHeld) && !Variables.GetBool(VarRamiroFound);

    private static bool SmolakReady(int turn)
    {
        int calls = Variables.GetInt(VarSmolakCalls);
        if (calls == 0)
        {
            return ZilleCampaign.OperationsDone >= 1;
        }

        return calls == 1 && Variables.GetString(VarSmolakAnswer) == "refused" && ZilleCampaign.Strength <= 4 &&
               turn > Variables.GetInt(VarSmolakTurn);
    }

    private static bool HegelCableReady(int w) => w >= 1 && Variables.GetInt(VarValgslandStage) == 0;

    private static bool HegelCallReady(int turn)
    {
        if (turn == 5 || Variables.GetInt(VarValgslandStage) != 1 || turn <= Variables.GetInt(VarValgslandStageTurn))
        {
            return false;
        }

        string cable = Variables.GetString(VarValgslandCable);
        return cable is "defied" or "stalled" || Variables.GetInt(Count(Lever.BffRaids)) > 0 || Variables.GetBool(VarLespiaCaleqabiz) ||
               Variables.GetInt(Count(Lever.Submarines)) >= 2 || Str(VarBludsIncident) == "kept";
    }

    // The vanilla Morella negotiations already bring Saltana to the table in Turn 5.
    private static bool SaltanaReady(int turn, int w) => w >= 1 && turn != 5 && !Variables.GetBool(VarSaltanaCalled);

    private static void AddCall(string conversation, string who, string token, string title, string subtitle,
        ConversationData.ConversationType type = ConversationData.ConversationType.ForeignPolicyAndTrade)
    {
        string fragment = $"ZilleWar.Pre_Call_{who}_T{Turn}";
        try
        {
            if (GameState.StoryFragmentExistsInCurrentStep(fragment))
            {
                return;
            }

            GameState.AddCustomConversation(conversation);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(fragment, SuzerainStoryPackInfo.Rizia, token, title, subtitle, type, conversation));
            Core.Log.Msg($"Supply war: offered '{fragment}' ({title}).");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding '{fragment}' failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ lever news

    /// <summary>News and reports for levers pulled since the last check (persistent, so a reload never repeats them).</summary>
    public static void ReportNewLevers()
    {
        foreach (Lever l in Enum.GetValues<Lever>())
        {
            int now = Variables.GetInt(Count(l));
            int seen = Variables.GetInt(ReportedVar(l));
            // The embargo and the strait are announced once; SupplyNews covers what follows.
            int max = l is Lever.Embargo or Lever.Lespia or Lever.BffFighters ? Math.Min(now, 1) : Math.Min(now, 3);
            for (int i = seen + 1; i <= max; i++)
            {
                NewsWire.LeverNews(l, i);
            }

            if (now > seen)
            {
                Variables.Set(ReportedVar(l), now);
            }
        }
    }

    // ------------------------------------------------------------------ dialogue helpers

    /// <summary>Per-call "already asked" flags. Every call resets them when it starts.</summary>
    private static readonly string[] Asked = Enumerable.Range(1, 12).Select(i => $"ZilleWar.Call_Asked_{i}").ToArray();
    private const string VarAskedAll = "ZilleWar.Call_Asked_Reset";

    private static string ResetAsked => string.Concat(Asked.Select(a => $"Variable[\"{a}\"] = false; "));

    private static string Q(int i) => Asked[i - 1];

    /// <summary>A string variable, "" when unset (Dialogue System reads nil as "nil").</summary>
    internal static string Str(string v)
    {
        string s = Variables.GetString(v);
        return string.IsNullOrEmpty(s) || s == "nil" ? string.Empty : s;
    }

    internal static string V(string v) => $"Variable[\"{v}\"]";
    internal static string N(string v) => $"(Variable[\"{v}\"] or 0)";
    internal static string Is(string v) => $"(Variable[\"{v}\"] == true)";
    internal static string Not(string v) => $"(Variable[\"{v}\"] ~= true)";
    internal static string Eq(string v, string s) => $"(Variable[\"{v}\"] == \"{s}\")";

    // ---- the last phase as the calls read it (PrepareCallState): one class per outcome, so no call misreads a gain
    internal static string CallFought => $"{N(VarCallFought)} >= 1";
    internal static string CallKind(params string[] kinds) =>
        $"({CallFought} and {Not(VarCallHeld)} and (" + string.Join(" or ", kinds.Select(k => Eq(VarCallKind, k))) + "))";
    /// <summary>Every city a call can name as won or lost: the four Zille cities and the Rizian border towns.</summary>
    internal static readonly string[] CallPlaces = { "Zille", "Auremina", "Isor", "Nanta", "Mave Nortus", "Dapalle", "Balle", "Jales" };
    internal static bool IsZilleCity(string c) => Array.IndexOf(AfterAction.ZilleCities, c) >= 0;
    internal static string Inc(string v, int d) => $"Variable[\"{v}\"] = (Variable[\"{v}\"] or 0) + ({d}); ";
    internal static string T(string v) => $"Variable[\"{v}\"] = true; ";
    internal static string F(string v) => $"Variable[\"{v}\"] = false; ";
    internal static string S(string v, string s) => $"Variable[\"{v}\"] = \"{s}\"; ";
    internal static string SetTurn(string v) => $"Variable[\"{v}\"] = {TurnLua}; ";

    /// <summary>Strengthen the Zille garrison by n steps, never above 10.</summary>
    internal static string Strengthen(int n)
    {
        string v = V(ZilleCampaign.VarStrength);
        return string.Concat(Enumerable.Repeat($"if ({v} or 8) < 10 then {v} = ({v} or 8) + 1 end; ", n));
    }

    internal const string Music = "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\"); ";
    private const string LucitaArrestedMidWar = "ZilleWar.E6_LucitaArrestedMidWar";
    private static readonly string LucitaHere = $"({Not(LucitaArrested)} and {Not(LucitaArrestedMidWar)})";
    private static readonly string LucitaGone = $"({Is(LucitaArrested)} or {Is(LucitaArrestedMidWar)})";

    /// <summary>
    /// A small wrapper over <see cref="Talk"/> for the calls. Conditions sit directly on player options (a choice whose
    /// condition fails is not shown), so every menu needs at least one unconditional option.
    /// </summary>
    private sealed class Dlg
    {
        private readonly Talk _t;

        public Dlg(string conversation, string prefix) => _t = new Talk(conversation, prefix);

        public Dlg Start(string conversation, string script, params string[] next)
        {
            _t.Logic("Start", next, script: script, hooks: Talk.AtStart(conversation));
            return this;
        }

        public Dlg Say(string name, string speaker, string text, params string[] next)
        {
            _t.Line(name, speaker, text, next);
            return this;
        }

        public Dlg SayIf(string name, string condition, string speaker, string text, params string[] next)
        {
            _t.Line(name, speaker, text, next, condition: condition);
            return this;
        }

        public Dlg SayDo(string name, string speaker, string text, string script, params string[] next)
        {
            _t.Line(name, speaker, text, next, script: script);
            return this;
        }

        public Dlg SayIfDo(string name, string condition, string speaker, string text, string script, params string[] next)
        {
            _t.Line(name, speaker, text, next, condition: condition, script: script);
            return this;
        }

        public Dlg Enter(string name, string speaker, string text, string script, params string[] next)
        {
            _t.Line(name, speaker, text, next, script: script, conversant: true);
            return this;
        }

        public Dlg EnterIf(string name, string condition, string speaker, string text, params string[] next)
        {
            _t.Line(name, speaker, text, next, condition: condition, conversant: true);
            return this;
        }

        public Dlg Scene(string name, string text, string sequence, string script, params string[] next)
        {
            _t.Line(name, Narrator, text, next, script: script, sequence: sequence);
            return this;
        }

        public Dlg SceneIf(string name, string condition, string text, string sequence, string script, params string[] next)
        {
            _t.Line(name, Narrator, text, next, condition: condition, script: script, sequence: sequence);
            return this;
        }

        /// <summary>An NPC-side branch: the first child whose condition holds is taken.</summary>
        public Dlg Branch(string name, params string[] next)
        {
            _t.Logic(name, next);
            return this;
        }

        public Dlg Do(string name, string script, params string[] next)
        {
            _t.Logic(name, next, script: script);
            return this;
        }

        public Dlg DoIf(string name, string condition, string script, params string[] next)
        {
            _t.Logic(name, next, condition: condition, script: script);
            return this;
        }

        /// <summary>A player option, hidden while its condition is false.</summary>
        public Dlg Opt(string name, string condition, string text, string cost, string script, params string[] next)
        {
            _t.Choice(name, text, next, condition: string.IsNullOrEmpty(condition) ? null : condition, script: script, cost: cost);
            return this;
        }

        /// <summary>A player option shown with a shorter menu text.</summary>
        public Dlg OptMenu(string name, string condition, string text, string menu, string cost, string script, params string[] next)
        {
            _t.Choice(name, text, next, condition: string.IsNullOrEmpty(condition) ? null : condition, script: script, menu: menu, cost: cost);
            return this;
        }

        /// <summary>A war-room line spoken by Lucita, or by Carlos Robles Azaro when she is under arrest.</summary>
        public Dlg Staff(string name, string lucita, string carlos, params string[] next)
        {
            _t.Logic(name, new[] { name + "_L", name + "_C" });
            _t.Line(name + "_L", Lucita, lucita, next, condition: LucitaHere);
            _t.Line(name + "_C", Carlos, carlos ?? lucita, next, condition: LucitaGone);
            return this;
        }

        /// <summary>As <see cref="Staff"/>, only when the condition holds (as one child of a branch).</summary>
        public Dlg StaffIf(string name, string condition, string lucita, string carlos, params string[] next)
        {
            _t.Logic(name, new[] { name + "_L", name + "_C" }, condition: condition);
            _t.Line(name + "_L", Lucita, lucita, next, condition: LucitaHere);
            _t.Line(name + "_C", Carlos, carlos ?? lucita, next, condition: LucitaGone);
            return this;
        }

        public void Finish()
        {
            _t.End();
            _t.Register();
        }
    }

    // Conditions sit directly on the choices (proven in play: a choice whose condition fails is not shown).
    // Kept as a marker for conditional options in the menus.
    internal static string Gate(string name) => name;
}
