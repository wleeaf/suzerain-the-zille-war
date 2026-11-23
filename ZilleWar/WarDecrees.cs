using Il2Cpp;
using SuzerainModdingKit;
using ConditionalInstruction = Il2Cpp.ConditionalInstructionProperties.ConditionalInstruction;

namespace ZilleWar;

/// <summary>
/// Royal decrees of the Zille war, added to vanilla's Royal Decrees panel the way vanilla defines its own:
/// - a DecreeData per decree (Title, CostText in vanilla's multi-line format, IsEnabled, OnDecreeEnact with the vanilla
///   resource changes, ErrorValidationDatas, category, "Royal Decrees Panel"), plus one DecreeApprovalData per councilor
///   (the 13 vanilla DecreeApproval characters show only while they sit on the council, as in vanilla);
/// - HUD per-turn lines and Overview policy cards for the decrees that stay in force (values computed every step).
/// The panel rebuilds its list from EntityDataManager.DecreesData on every panel update, and signing a decree runs its
/// OnDecreeEnact and then GameFlowManager.EvaluateCurrentStep, so <see cref="OnStep"/> sees the enactment at once and applies
/// the mod-side effects (garrison strength, news, our variables). Expresso cannot write ZilleWar variables, so the decree
/// scripts only touch vanilla variables.
/// Persistence: vanilla saves IsEnacted as an entity update keyed by name, which a load applies before our entities exist
/// (the game logs and skips unknown names). The enacted state therefore lives in our own saved variables
/// (ZilleWar.Decree_*) and is written back into the entity whenever it is (re-)added. Entities are re-added after the game
/// reloads its data (<see cref="ResetAdded"/>, called from EntityReload) and whenever they are found missing.
/// </summary>
internal static partial class WarDecrees
{
    // vanilla order of the DecreeApproval characters (DecreeApprovalCharacterData)
    private static readonly string[] Characters =
    {
        "HugoToras", "AngelicaSazon", "AxelReinhart", "SalIgnacius", "LucitaAzaro", "TaddeusAzaro", "TitusGordion", "RicoToras",
        "LaurentoEsquibel", "VinaToras", "ElenaWerner", "RustyMontoro", "DariaDeRava",
    };

    private const string Panel = "Royal Decrees Panel";
    private const string Authority = "RiziaDLC.Resources_Authority";
    private const string Budget = "RiziaDLC.Resources_Budget";
    private const string NoAuthority = "Insufficient authority to enact decree";
    private const string NoBudget = "Insufficient budget to enact decree";

    // properties, not static readonly fields: the class is partial, and C# runs the field initializers of different partial
    // files in an unspecified order
    private static string Active => WarFlags.X(WarFlags.Active);
    private static string Declared => WarFlags.X(WarFlags.Declared);
    private static string Over => WarFlags.X(WarFlags.Over);

    /// <summary>
    /// A decree. Open: an optional C# gate, exposed as ZilleWar.DecOpen_&lt;key&gt; and ANDed into IsEnabled (for conditions
    /// Expresso cannot read). Leverage: added to the peace treaty's opening leverage while enacted. Rows: conditional
    /// instruction rows added to vanilla's recompute entries (perception, living standards, power projection).
    /// </summary>
    private sealed record Decree(
        string Key, string Category, string Path, string Title, string Cost, string Description, string Enabled, string Enact,
        (string message, string condition)[] Errors, int[] Approvals, Action Enacted,
        Func<bool> Open = null,
        Func<int> Leverage = null,
        (string ci, string cond, string instr)[] Rows = null);

    private static string Name(string key) => $"ZilleWar_Decree_{key}";
    private static string EnactedVar(string key) => $"ZilleWar.Decree_{key}";
    private static string TurnVar(string key) => $"ZilleWar.Decree_{key}_Turn";
    private static string NotifiedVar(string key) => $"ZilleWar.Decree_{key}_Notified";
    private static string OpenVar(string key) => $"ZilleWar.DecOpen_{key}";
    private static bool Enacted(string key) => Variables.GetBool(EnactedVar(key));
    private static int EnactTurn(string key) => Variables.GetInt(TurnVar(key));
    private static string EnabledExpr(Decree d) => d.Open == null ? d.Enabled : $"({d.Enabled}) && {OpenVar(d.Key)} == true";

    // the war's outcome, for decrees and their effects
    private static int Since => WorldSystems.TurnsSinceEnd;
    private static bool Lib => LateState.Liberated && LateState.Over;
    private static bool Part => LateState.Partial;
    private static bool Lost => LateState.Lost;
    private static bool TalksDone =>
        Variables.GetBool(ZilleTreaty.VarSigned) || (Variables.GetString(ZilleTreaty.VarKind) is { Length: > 0 } k && k != "nil");

    private const string LastTickVar = "ZilleWar.Dec_LastTick";
    private const string LeverageVar = "ZilleWar.Decree_TreatyLeverage";

    private static string Std(string extra = null) => "RiziaDLC.RoyalDecrees_Passed += 1;\n" + (extra ?? string.Empty);

    private static (string, string) LowAuthority(int below) => (NoAuthority, $"{Authority} <= {below}");
    private static (string, string) LowBudget(int below) => (NoBudget, $"{Budget} <= {below}");

    private const string Reserves = "BrenasReserves";
    private const string Censorship = "WarCensorship";
    private const string Shipping = "MerchantFleet";
    private const string Rationing = "FuelRationing";
    private const string Deserters = "DeserterSafeConduct";
    private const string Protect = "ProtectWehzeks";
    private const string Intern = "InternWehzeks";
    private const string Pensions = "VeteransPensions";
    private const string StandDown = "StandDownReserves";
    private const string LiftCensor = "LiftCensorship";
    private const string ReleaseInterned = "ReleaseInternees";
    private const string Loan = "WarLoan";
    private const string Seize = "SeizeAssets";
    private const string Footing = "WarFooting";
    private const string Legion = "ZilleanLegion";
    private const string Prices = "PriceControls";

    private const string OrderPath = "Rizia/Royal Decrees Panel/Order Decrees";
    private const string EconomyPath = "Rizia/Royal Decrees Panel/Economy Decrees";
    private const string WelfarePath = "Rizia/Royal Decrees Panel/Welfare Decrees";
    private const string MilitaryPassed = "RiziaDLC.RoyalDecrees_Military_Passed += 1;";

    // recompute entries the decrees add rows to
    private const string PerceptionCi = "Diplomacy_GlobalPerceptionWar";
    private const string LivingCi = "LivingStandards_Default";
    private const string PowerCi = "PowerProjection_War_Outcome";

    private static readonly List<Decree> Decrees = new()
    {
        // ---------------------------------------------------------------- the eight decrees of the first build (rebalanced)
        new(Reserves, "Order", OrderPath, "Call Up the Brenas Reserve Divisions [RNC Proposal]",
            "-2 Authority\n-2 Budget\n+750 Military Manpower\n-1 Budget Per Turn While Under Arms",
            "The reserve divisions of Brenas, men who served their conscription years and went home to the mines and farms of the " +
            "province, are recalled to the colors for the duration of the campaign in Zille. Mustered in Sallabes and Mave Nortus, a " +
            "day's march from the front, they will refill the ranks that the first battles have thinned.\n\n" +
            "The call-up will put thousands of men under arms within weeks, but the Crown must pay and feed them until it sends them " +
            "home, and the pits and fields of Brenas will be worked by their wives and fathers in the meantime. House Sazon will not " +
            "forget whose province gave its sons first, nor how long the Crown kept them.",
            Active,
            $"{Authority} -= 2;\n{Budget} -= 2;\nRiziaDLC.Resources_Military_Manpower += 750;\nRiziaDLC.Country_PublicOpinion -= 1;\n" +
            "RiziaDLC.RD_RNCDecreesPassed += 1;\n" + Std(MilitaryPassed),
            new[] { LowAuthority(1), LowBudget(1) },
            new[] { 1, 1, -1, 0, 1, 1, 1, 0, 0, -1, -1, -1, 1 },
            OnReserves,
            Rows: new[]
            {
                // the reserves hold the armistice line
                (PowerCi, $"{EnactedVar(Reserves)} == true && {EnactedVar(StandDown)} == false && {WarFlags.X(WarFlags.Over)} && " +
                          WarFlags.X(WarFlags.Liberated, false), "RiziaDLC.Country_Military_PowerProjection += 1;"),
            }),

        new(StandDown, "Order", OrderPath, "Stand Down the Brenas Reserves",
            "-1 Authority\n-500 Military Manpower\nEnds Brenas Reserve Pay",
            "The reserve divisions of Brenas, called up for the campaign in Zille, are released from service and sent home to their " +
            "mines and farms. Their rifles go back to the depots in Sallabes, and the Crown's allowances to their families end with " +
            "the last pay parade.\n\n" +
            "Brenas will welcome its sons home, and the treasury will be spared their keep. Where the guns have stopped but nothing " +
            "has been signed, the line will be held by fewer and younger men, and the General Staff will say so.",
            $"{Over} && {WarFlags.X(EnactedVar(Reserves))}",
            $"{Authority} -= 1;\nRiziaDLC.Resources_Military_Manpower -= 500;\nRiziaDLC.Country_PublicOpinion += 1;\n" +
            Std(),
            new[] { LowAuthority(0), ("Lack of manpower to enact decree", "RiziaDLC.Resources_Military_Manpower <= 499") },
            new[] { 0, 1, 1, 1, -1, -1, 0, 0, 1, 1, 1, 1, 0 },
            OnStandDown),

        new(Censorship, "Order", OrderPath, "Impose Wartime Censorship",
            "-2 Authority\n+1 Authority Per Turn During the War\n-1 Global Perception While in Force",
            "For the duration of the war with Wehlen, every dispatch from the Zille front, every casualty list and every photograph " +
            "will pass through the censors of the General Staff before it reaches a newspaper, a printing press or a radio studio. The " +
            "positions of units and ships, the names of the fallen and the cost of the campaign will be published only when the Staff " +
            "allows it.\n\n" +
            "The Crown will speak of the war with one voice, and bad news, if it comes, will reach the public late and softened. " +
            "Foreign correspondents will call the measure what it is, and the Kingdom's friends abroad will remember it for as long " +
            "as it lasts.",
            Active,
            $"{Authority} -= 2;\nRiziaDLC.LeaningValue_Democratic -= 2;\n" + Std(),
            new[] { LowAuthority(1), ("The press already answers to the Crown", "RiziaDLC.Policy_RD_Welfare_FreePressRevoked == true") },
            new[] { 1, 1, -1, 0, 1, 1, 1, 1, -1, -1, 0, 0, 1 },
            OnCensorship,
            Rows: new[]
            {
                (PerceptionCi, $"{EnactedVar(Censorship)} == true && {EnactedVar(LiftCensor)} == false",
                    "RiziaDLC.Country_GlobalPerception -= 1"),
            }),

        new(LiftCensor, "Order", OrderPath, "Lift Wartime Censorship",
            "-1 Authority\nEnds the Global Perception Penalty",
            "The censors of the General Staff are withdrawn from the newspapers, printing presses and radio studios of the Kingdom. " +
            "Casualty lists, the names of regiments and photographs from Zille may be published again, including those held back " +
            "during the war.\n\n" +
            "Abroad, the decree will be read as a sign that the Crown has nothing left to hide. At home, the papers will print " +
            "everything the censors kept from them, and the Kingdom will learn at last what the campaign in Zille really cost.",
            $"{Over} && {WarFlags.X(EnactedVar(Censorship))}",
            $"{Authority} -= 1;\nRiziaDLC.LeaningValue_Democratic += 1;\n" + Std(),
            new[] { LowAuthority(0) },
            new[] { 0, 0, 1, 0, -1, -1, -1, 1, 1, 1, 0, 0, -1 },
            OnLiftCensorship),

        new(Shipping, "Order", OrderPath, "Requisition the Merchant Fleet",
            "-2 Authority\n-1 Budget\n+1 Marines\n-1 Budget Per Turn During the War",
            "Freighters, ferries and coasters of the Rizian merchant marine are requisitioned by the Royal Navy for the duration of the " +
            "war. Refitted in the yards of Monqiz and Fort Ales, they will carry troops, fuel and ammunition to the Zille front, and " +
            "give the Staff enough lift to put another battalion of the Royal Marines ashore wherever it is needed.\n\n" +
            "The shipping houses of Monqiz will be compensated at the Admiralty's rates rather than the market's, and the trade their " +
            "ships carried in peacetime will have to wait until the war is over.",
            Active,
            $"{Authority} -= 2;\n{Budget} -= 1;\nRiziaDLC.Army_Unit_Marine_Total += 1;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" +
            Std(MilitaryPassed),
            new[] { LowAuthority(1), LowBudget(0) },
            new[] { 1, 0, -1, 0, 1, 1, 1, 0, 0, 0, -1, -1, -1 },
            OnShipping),

        new(Rationing, "Economy", EconomyPath, "Wartime Fuel Rationing",
            "-2 Authority\n+2 Energy Per Turn During the War",
            "Petrol, diesel and heating oil will be sold to civilians only against ration coupons for as long as the war lasts. " +
            "Private motoring is limited to two days a week, and the fuel saved goes to the army's trucks, the navy's bunkers and the " +
            "airfields serving the Zille front.\n\n" +
            "The Kingdom's reserves will stretch further, but queues will form at every pump in Rizia, and the shopkeepers, drivers " +
            "and farmers who live by fuel will feel the war in their pockets long before they read about it in the newspapers.",
            $"{Active} && {WarFlags.X(EnactedVar(Prices), false)}",
            $"{Authority} -= 2;\nRiziaDLC.Country_PublicOpinion -= 2;\n" + Std(),
            new[]
            {
                LowAuthority(1),
                ("The Energy Conservation Ordinance is in force",
                    $"{EnactedVar(ROrdinance)} == true && {EnactedVar(RLiftOrdinance)} == false"),
                ("Energy heating subsidies are still in force", "RiziaDLC.Policy_RD_Economy_GasHeatingSubsidiesForCitizens == true"),
                ("Industrial energy subsidies are still in force", "RiziaDLC.Policy_RD_Economy_EnergySubsidiesForIndustries == true"),
            },
            new[] { 0, 0, -1, 0, 1, 1, 1, 0, 0, -1, 1, -1, -1 },
            OnRationing),

        new(Deserters, "Order", OrderPath, "Safe Conduct for Wehzek Deserters",
            "-1 Authority\n-2 Budget\n-1 Wehzek Garrison Strength in Zille",
            "Leaflets dropped over the Wehzek lines, and broadcasts in Wehzek on the army's radio, will promise every soldier of the " +
            "Zille garrison who lays down his arms safe conduct, hot food and a reward in gold for his rifle. Those who come over will " +
            "be held in camps far from the front and sent home when the war is over.\n\n" +
            "Every deserter is one rifle fewer facing our men, and one more voice telling his comrades that Wraclawitz has abandoned " +
            "them. The old officers call it buying victory with coin. The paymasters call it cheaper than a battle.",
            Active,
            $"{Authority} -= 1;\n{Budget} -= 2;\n" + Std(),
            new[]
            {
                LowAuthority(0), LowBudget(1),
                ("The Wehzek garrison is already at its breaking point",
                    $"{ZilleCampaign.VarStrength} <= 3 && {ZilleCampaign.VarStrength} >= 1"),
            },
            new[] { 0, 0, 1, 0, 1, -1, 0, 0, 1, 1, -1, 0, 0 },
            OnDeserters),

        new(Protect, "Welfare", WelfarePath, "Royal Protection of Wehzek Subjects",
            "-2 Authority\n-1 Budget\n+1 Global Perception While in Force",
            "The Crown declares that its subjects of Wehzek descent, in Little Wraclawitz, in the mining towns of Brenas and wherever " +
            "else they live, remain under the King's protection during the war with Wehlen and after it. Their homes, shops and " +
            "sanctuaries will be guarded by the police, attacks on them will be prosecuted as attacks on the Crown, and no Wehzek " +
            "subject will lose his work or his property because of the war.\n\n" +
            "Nationalists will call it softness towards the enemy, and some will say so in the street. Abroad, and in the Wehzek " +
            "quarters of Rizia, it will be read as proof that the King's war is with Smolak's army and not with a people, and when " +
            "the time comes to make peace, President Smolak will have one grievance fewer to bring to the table.",
            $"{Declared} && {WarFlags.X(EnactedVar(Intern), false)}",
            $"{Authority} -= 2;\n{Budget} -= 1;\nRiziaDLC.LeaningValue_Democratic += 1;\nRiziaDLC.Country_PublicOpinion -= 1;\n" + Std(),
            new[] { LowAuthority(1), LowBudget(0) },
            new[] { 0, -1, 1, 1, -1, 0, 0, -1, 1, 1, 1, 0, 0 },
            OnProtect,
            Leverage: () => 1,
            Rows: new[] { (PerceptionCi, $"{EnactedVar(Protect)} == true", "RiziaDLC.Country_GlobalPerception += 1") }),

        new(Intern, "Order", OrderPath, "Internment of Wehzek Nationals",
            "+1 Authority\n-1 Budget Per Turn Until Released\n-1 Energy Per Turn During the War\n-1 Global Perception While in Force",
            "Wehzek nationals living in the Kingdom, and subjects of Wehzek descent whose loyalty the security services doubt, will be " +
            "registered with the police. Men of fighting age will be held in camps in the interior for the duration of the war, and " +
            "the property of the interned will be placed under royal administration.\n\n" +
            "The security services expect the measure to silence Wehlen's informants in Iza and the mining towns of Brenas. The pits " +
            "will lose some of their best miners, the camps must be guarded and fed for as long as they stand, and the Kingdom's " +
            "friends abroad will remember them long after the war is over.",
            $"{Active} && {WarFlags.X(EnactedVar(Protect), false)}",
            $"{Authority} += 1;\nRiziaDLC.Country_PublicOpinion += 1;\nRiziaDLC.LeaningValue_Democratic -= 2;\n" +
            "RiziaDLC.Diplomacy_Valgsland_NegotiatingPower -= 1;\n" + Std(),
            Array.Empty<(string, string)>(),
            new[] { 1, 1, -1, -1, 1, 1, 1, 1, -1, -1, -1, 0, 0 },
            OnIntern,
            Leverage: () => -1,
            Rows: new[]
            {
                (PerceptionCi, $"{EnactedVar(Intern)} == true && {EnactedVar(ReleaseInterned)} == false",
                    "RiziaDLC.Country_GlobalPerception -= 1"),
            }),

        new(ReleaseInterned, "Order", OrderPath, "Release the Wehzek Internees",
            "-1 Authority\nEnds the Internment Camps",
            "The internment camps opened for Wehzek nationals and suspect subjects during the war are closed, and their inmates sent " +
            "home with a railway warrant and a week's pay. Property placed under royal administration is returned to its owners.\n\n" +
            "Little Wraclawitz will welcome its men back, and the pits of Brenas their miners. Those who cheered the trucks when they " +
            "came will say the King has forgotten the war before its graves have grassed over.",
            $"{Over} && {WarFlags.X(EnactedVar(Intern))}",
            $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion -= 1;\nRiziaDLC.LeaningValue_Democratic += 1;\n" +
            Std(),
            new[] { LowAuthority(0) },
            new[] { 0, 1, 1, 1, -1, -1, -1, 1, 1, 1, 1, 1, 0 },
            OnReleaseInternees),

        new(Pensions, "Welfare", WelfarePath, "Pensions for the Veterans of Zille",
            "-2 Budget\n-1 Budget Per Turn\n+1 Authority Per Turn",
            "Every soldier who served in the Zille campaign will receive a royal pension and a medal struck at the Porte Drazon mint, " +
            "and the widows and orphans of the fallen will be pensioned by the Crown for life. The military hospitals of Mave Nortus " +
            "and Porte Drazon will treat the wounded of the campaign free of charge.\n\n" +
            "The pensions will weigh on the treasury for years, but the men who fought in Zille, and their families, will know that " +
            "the Crown which sent them there has not forgotten them.",
            Over,
            $"{Budget} -= 2;\nRiziaDLC.Country_PublicOpinion += 2;\n" + Std(),
            new[] { LowBudget(1) },
            new[] { 1, 1, 0, 1, 1, 1, 1, 0, 0, 1, -1, -1, 0 },
            OnPensions),

        // ---------------------------------------------------------------- wartime measures (Turns 4 to 6, while the war is fought)
        new(Loan, "Economy", EconomyPath, "War Loan of the Zille Campaign",
            "+4 Budget\n-1 Budget Per Turn for 5 Turns After the War",
            "The Royal Treasury raises a loan from the banks of Porte Drazon and Monqiz to pay for the campaign in Zille. The money is " +
            "available at once, and the Crown repays it, with interest, in five yearly installments once the guns have fallen " +
            "silent.\n\n" +
            "The army will not want for pay or shells while the war lasts. The bill will come due in peacetime, when the victory, or " +
            "the defeat, will have to share the treasury with the interest owed to the bankers.",
            $"{Active} && {EnactedVar(RGoldBonds)} == false",
            $"{Budget} += 4;\nRiziaDLC.LeaningValue_ProBusiness += 1;\n" + Std(),
            Array.Empty<(string, string)>(),
            new[] { 1, 0, 0, 0, 1, 1, 0, -1, 0, 0, -1, 1, 1 },
            OnLoan),

        new(Seize, "Economy", EconomyPath, "Seize Wehzek State Assets",
            "-1 Authority\n+2 Budget",
            "The accounts of the Wehzek state and its companies in Rizian banks, the property of the Wehzek consulates and the cargoes " +
            "of Wehzek state shipping in Rizian ports are placed under royal sequestration for the duration of the war.\n\n" +
            "The treasury gains at a stroke what Wraclawitz kept in Monqiz. Foreign investors will ask whose deposits are next, and " +
            "President Smolak will not forget the account when the time comes to talk peace.",
            Active,
            $"{Authority} -= 1;\n{Budget} += 2;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" + Std(),
            new[] { LowAuthority(0) },
            new[] { 1, 1, -1, 0, 1, 1, 1, 1, -1, 0, -1, -1, 0 },
            OnSeize,
            Leverage: () => -1),

        new(Footing, "Economy", EconomyPath, "Put the Iza Works on a War Footing",
            "-2 Authority\n-1 Budget\n+500 Military Equipment\n+500 Military Equipment Per Turn During the War\n" +
            "-1 Energy Per Turn During the War",
            "The arms works and foundries of Iza are placed under the orders of the War Office for the duration of the campaign. " +
            "Shifts are doubled, civilian orders are set aside, and the coal and power of the district go first to the production of " +
            "shells, rifles and spare parts for the Zille front.\n\n" +
            "The army will be supplied faster than any market could supply it, and the owners of the works will be paid handsomely " +
            "for it. The shops of Iza will run short of everything the factories no longer make, and its workers will feel the war " +
            "in their hours.",
            Active,
            $"{Authority} -= 2;\n{Budget} -= 1;\nRiziaDLC.Resources_Military_Equipment += 500;\nRiziaDLC.LeaningValue_ProBusiness += 1;\n" +
            Std(MilitaryPassed),
            new[] { LowAuthority(1), LowBudget(0) },
            new[] { 1, 0, -1, 0, 1, 1, 1, -1, 0, -1, -1, 0, 1 },
            OnFooting,
            Rows: new[]
            {
                (LivingCi, $"{EnactedVar(Footing)} == true && {WarFlags.X(WarFlags.Active)}", "RiziaDLC.Country_LivingStandards -= 1;"),
            }),

        new(Legion, "Order", OrderPath, "Raise the Zillean Legion",
            "-1 Authority\n-1 Budget\n+1 Infantry Division",
            "Zilleans who fled the lease, and the sons of harbor families who settled in the south, are invited to enlist in a " +
            "volunteer Legion under the Rizian flag. Trained at Sallabes and led by Rizian officers, it will fight for the " +
            "liberation of its own streets.\n\n" +
            "The Legion will tell the world that Zille fights for itself. If the war is lost, the families its volunteers left behind " +
            "in Zille will be the first to pay for it.",
            Active,
            $"{Authority} -= 1;\n{Budget} -= 1;\nRiziaDLC.Army_Unit_Infantry_Total += 1;\nRiziaDLC.Country_PublicOpinion += 1;\n" +
            "RiziaDLC.Relations_Estela += 1;\n" + Std(MilitaryPassed),
            new[] { LowAuthority(0), LowBudget(0) },
            new[] { 0, -1, 0, 0, 1, -1, 0, 1, 0, 1, 0, 0, 0 },
            OnLegion,
            // after a lost war, Smolak demands an amnesty for the Legion's families
            Leverage: () => Lost ? -1 : 0),

        new(Prices, "Economy", EconomyPath, "Price Controls on Bread and Fuel [RPP Proposal]",
            "-1 Authority\n-1 Budget Per Turn During the War",
            "The prices of bread, flour, cooking oil, coal and fuel are fixed by royal decree at their level on the day war was " +
            "declared, and the Treasury pays the difference to millers, bakers and fuel merchants who would otherwise sell at a " +
            "loss.\n\n" +
            "Families will not pay for the war at the baker's counter, and the RPP will claim the measure as its own. Merchants call " +
            "it ruin, and the economists of Porte Drazon expect a black market before the first month is out.",
            $"{Active} && {WarFlags.X(EnactedVar(Rationing), false)}",
            $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion += 1;\nRiziaDLC.LeaningValue_ProBusiness -= 2;\n" +
            "RiziaDLC.RD_RPPDecreesPassed += 1;\n" + Std(),
            new[] { LowAuthority(0), ("Fuel is already rationed", $"{EnactedVar(Rationing)} == true") },
            new[] { -1, 0, 0, 1, 0, 0, 0, 1, 0, 1, -1, -1, -1 },
            OnPrices,
            // cancels the war's -1
            Rows: new[]
            {
                (LivingCi, $"{EnactedVar(Prices)} == true && {WarFlags.X(WarFlags.Active)}", "RiziaDLC.Country_LivingStandards += 1;"),
            }),
    };

    // ------------------------------------------------------------------ HUD lines and Overview cards

    private sealed record Line(string Name, string Stat, string Variable, string Description, Func<int> Value)
    {
        public string Condition => Value == null ? null : $"{Variable} {(Description.StartsWith("+", StringComparison.Ordinal) ? ">= 1" : "<= -1")}";
    }

    private static bool AtWar => Variables.GetBool(WarFlags.Active);
    private static int While(string key, int value) => Enacted(key) && AtWar ? value : 0;

    private static readonly List<Line> Lines = new()
    {
        new("Rizia_Budget_Modifier_ZilleWar_BrenasReservePay", "Rizia_HUDStat_Budget", "ZilleWar.Dec_ReservePay",
            "-{0} per turn from Brenas Reserve Pay", () => Enacted(Reserves) && !Enacted(StandDown) ? -1 : 0),
        new("Rizia_Budget_Modifier_ZilleWar_RequisitionedShipping", "Rizia_HUDStat_Budget", "ZilleWar.Dec_Requisition",
            "-{0} per turn from Requisitioned Merchant Shipping", () => While(Shipping, -1)),
        new("Rizia_Budget_Modifier_ZilleWar_InternmentCamps", "Rizia_HUDStat_Budget", "ZilleWar.Dec_Internment",
            "-{0} per turn from Internment Camps", () => Enacted(Intern) && !Enacted(ReleaseInterned) ? -1 : 0),
        new("Rizia_Energy_Modifier_ZilleWar_InternedMiners", "Rizia_HUDStat_Energy", "ZilleWar.Dec_InternedMiners",
            "-{0} per turn from Interned Brenas Miners", () => While(Intern, -1)),
        new("Rizia_Budget_Modifier_ZilleWar_VeteransPensions", "Rizia_HUDStat_Budget", "ZilleWar.Dec_Pensions",
            "-{0} per turn from Zille Veterans' Pensions", () => Enacted(Pensions) ? -1 : 0),
        new("Rizia_Energy_Modifier_ZilleWar_FuelRationing", "Rizia_HUDStat_Energy", "ZilleWar.Dec_Rationing",
            "+{0} per turn from Wartime Fuel Rationing", () => While(Rationing, 2)),
        new("Rizia_Authority_Modifier_ZilleWar_Censorship", "Rizia_HUDStat_Authority", "ZilleWar.Dec_Censorship",
            "+{0} per turn from Wartime Censorship", () => While(Censorship, 1)),
        new("Rizia_Authority_Modifier_ZilleWar_VeteransLoyalty", "Rizia_HUDStat_Authority", "ZilleWar.Dec_VeteransLoyalty",
            "+{0} per turn from Loyalty of the Zille Veterans", () => Enacted(Pensions) ? 1 : 0),
        new("Rizia_Budget_Modifier_ZilleWar_WarLoan", "Rizia_HUDStat_Budget", "ZilleWar.Dec_WarLoan",
            "-{0} per turn from Zille War Loan Repayments", () => Enacted(Loan) && Since is >= 0 and <= 4 ? -1 : 0),
        new("Rizia_Energy_Modifier_ZilleWar_WarFooting", "Rizia_HUDStat_Energy", "ZilleWar.Dec_WarFooting",
            "-{0} per turn from the Iza Works on a War Footing", () => While(Footing, -1)),
        new("Rizia_Budget_Modifier_ZilleWar_PriceControls", "Rizia_HUDStat_Budget", "ZilleWar.Dec_PriceControls",
            "-{0} per turn from Wartime Price Controls", () => While(Prices, -1)),
    };

    private sealed record Card(string Name, string Category, string Variable, string Title, string Description, Func<bool> When);

    private static readonly List<Card> Cards = new()
    {
        new("ZilleWar_Military_BrenasReserves", "Military", "ZilleWar.DecPol_Reserves", "Brenas Reserves Under Arms",
            "The reserve divisions of Brenas have been recalled to the colors for the campaign in Zille. The Crown pays and feeds them " +
            "until they are stood down.", () => Enacted(Reserves) && !Enacted(StandDown)),
        new("ZilleWar_Law_WartimeCensorship", "Law", "ZilleWar.DecPol_Censorship", "Wartime Censorship",
            "Reporting on the war with Wehlen is read by the censors of the General Staff before publication. Casualty lists, unit " +
            "positions and photographs from Zille appear only when the Staff allows it.",
            () => Enacted(Censorship) && !Enacted(LiftCensor)),
        new("ZilleWar_Military_RequisitionedShipping", "Military", "ZilleWar.DecPol_Shipping", "Requisitioned Merchant Fleet",
            "Freighters and ferries of the merchant marine sail under the Royal Navy's orders for the duration of the war, carrying " +
            "troops, fuel and ammunition to the Zille front.", () => Enacted(Shipping) && AtWar),
        new("ZilleWar_Economy_FuelRationing", "Economy", "ZilleWar.DecPol_Rationing", "Wartime Fuel Rationing",
            "Petrol, diesel and heating oil are sold to civilians only against ration coupons. Private motoring is limited to two " +
            "days a week while the war lasts.", () => Enacted(Rationing) && AtWar),
        new("ZilleWar_Military_DeserterSafeConduct", "Military", "ZilleWar.DecPol_Deserters", "Safe Conduct for Deserters",
            "Wehzek soldiers who lay down their arms in Zille are promised safe conduct, food and a reward, and are held in camps far " +
            "from the front until the war is over.", () => Enacted(Deserters) && AtWar),
        new("ZilleWar_Order_WehzekProtection", "Order", "ZilleWar.DecPol_Protect", "Wehzek Subjects Under Royal Protection",
            "Rizia's subjects of Wehzek descent are under the King's protection. Attacks on their homes, shops and sanctuaries are " +
            "prosecuted as attacks on the Crown.", () => Enacted(Protect)),
        new("ZilleWar_Order_WehzekInternment", "Order", "ZilleWar.DecPol_Intern", "Internment of Wehzek Nationals",
            "Wehzek nationals and suspect subjects of Wehzek descent are registered with the police, and men of fighting age are " +
            "held in camps in the interior until the Crown releases them.", () => Enacted(Intern) && !Enacted(ReleaseInterned)),
        new("ZilleWar_Welfare_VeteransPensions", "Welfare", "ZilleWar.DecPol_Pensions", "Zille Veterans' Pensions",
            "The soldiers of the Zille campaign receive a royal pension, and the widows and orphans of the fallen are pensioned by " +
            "the Crown for life.", () => Enacted(Pensions)),
        new("ZilleWar_Economy_WarFooting", "Economy", "ZilleWar.DecPol_WarFooting", "Iza Works on a War Footing",
            "The arms works and foundries of Iza work double shifts under the orders of the War Office, and the coal and power of the " +
            "district go first to the Zille front.", () => Enacted(Footing) && AtWar),
        new("ZilleWar_Economy_PriceControls", "Economy", "ZilleWar.DecPol_PriceControls", "Wartime Price Controls",
            "The prices of bread, flour, cooking oil, coal and fuel are fixed at their level on the day war was declared, and the " +
            "Treasury pays the difference to millers, bakers and fuel merchants.", () => Enacted(Prices) && AtWar),
    };

    // ------------------------------------------------------------------ per-turn ticks, recompute rows, line adjusters

    /// <summary>Run once at the start of every new turn (resources without a HUD stat: equipment, manpower).</summary>
    private static readonly List<Action> Ticks = new();

    /// <summary>Rows added to vanilla's recompute conditional instructions (the only way perception, living standards, growth
    /// and power projection stick: one-off script changes are erased by the recompute).</summary>
    private static readonly List<(string ci, string cond, string instr)> Rows = new();
    private static readonly HashSet<string> RowTargets = new(StringComparer.Ordinal);

    internal static void AddRow(string ci, string cond, string instr)
    {
        if (Rows.Contains((ci, cond, instr)))
        {
            return;
        }

        Rows.Add((ci, cond, instr));
        RowTargets.Add(ci);
        LuaLint.RecordExpresso($"Decree row {ci}", cond);
    }

    /// <summary>Changes to WorldModifiers' per-turn values (WorldModifiers.Set calls <see cref="Adjust"/>).</summary>
    private static readonly List<(string var, Func<int, int> f)> Adjusters = new();

    internal static int Adjust(string modVar, int value)
    {
        foreach ((string v, Func<int, int> f) in Adjusters)
        {
            if (v != modVar)
            {
                continue;
            }

            try
            {
                value = f(value);
            }
            catch (Exception ex)
            {
                Core.Log.Error($"WarDecrees adjust {v}: {ex}");
            }
        }

        return value;
    }

    // hooks for the post-war decrees (WarDecreesPostWar.cs); this file compiles without them
    static partial void RegisterPostWar();
    static partial void PostWarStep();

    // hooks for the royal decrees of every game (RoyalDecrees.cs)
    static partial void RegisterRoyal();
    static partial void RoyalStep();

    // ------------------------------------------------------------------ registration

    public static void Register()
    {
        RegisterPostWar();
        RegisterRoyal();
        if (!Ticks.Contains(FootingTick))
        {
            Ticks.Add(FootingTick);
        }

        Variables.Register(LastTickVar);
        Variables.Register(LeverageVar);
        Vanilla.Expose(LeverageVar, isBool: false);
        foreach (string v in OnceFlags)
        {
            Variables.Register(v);
        }

        foreach (Decree d in Decrees)
        {
            Variables.Register(EnactedVar(d.Key));
            Variables.Register(TurnVar(d.Key));
            Variables.Register(NotifiedVar(d.Key));
            Vanilla.Expose(EnactedVar(d.Key));
            if (d.Open != null)
            {
                Variables.Register(OpenVar(d.Key));
                Vanilla.Expose(OpenVar(d.Key));
            }

            foreach ((string ci, string cond, string instr) in d.Rows ?? Array.Empty<(string, string, string)>())
            {
                AddRow(ci, cond, instr);
            }

            LuaLint.RecordExpresso($"Decree {d.Key} IsEnabled", EnabledExpr(d));
            foreach ((string message, string condition) in d.Errors)
            {
                LuaLint.RecordExpresso($"Decree {d.Key} error '{message}'", condition);
            }

            if (d.Approvals.Length != Characters.Length)
            {
                Core.Log.Error($"WarDecrees: decree {d.Key} has {d.Approvals.Length} approvals, expected {Characters.Length}.");
            }
        }

        foreach (Line l in Lines)
        {
            Variables.Register(l.Variable);
            Vanilla.Expose(l.Variable, isBool: false);
            LuaLint.RecordExpresso($"Decree HUD line {l.Name}", l.Condition);
        }

        foreach (Card c in Cards)
        {
            Variables.Register(c.Variable);
            Vanilla.Expose(c.Variable);
        }

        // the deserters' error row reads the garrison strength
        Vanilla.Expose(ZilleCampaign.VarStrength, isBool: false);
    }

    public static void OnStep()
    {
        Run("entities", EnsureEntities);
        Run("enactments", Detect);
        Run("values", Compute);
        Run("ticks", Tick);
        Run("rows", AddRows);
        Run("notifications", Notify);
        Run("post-war", () => PostWarStep());
        Run("royal", () => RoyalStep());
        Run("aftermath", Aftermath);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WarDecrees {what} failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ entities

    private static bool _added;
    private static readonly Dictionary<string, bool> LastSeen = new(StringComparer.Ordinal);

    /// <summary>The game rebuilt its entity lists (a save was loaded): add ours again on the next step.</summary>
    internal static void ResetAdded()
    {
        _added = false;
        LastSeen.Clear();
    }

    private static DecreeData Find(string name)
    {
        var list = EntityDataManager.DecreesData;
        if (list == null)
        {
            return null;
        }

        foreach (DecreeData d in list)
        {
            if (d?.NameInDatabase == name)
            {
                return d;
            }
        }

        return null;
    }

    private static void EnsureEntities()
    {
        if (EntityDataManager.DecreesData == null || EntityDataManager.HUDStatData == null)
        {
            return;
        }

        if (_added && Find(Name(Decrees[0].Key)) != null)
        {
            return;
        }

        LastSeen.Clear();
        _added = AddApprovals() && AddDecrees() && AddLines() && AddCards();
        if (_added)
        {
            Core.Log.Msg($"WarDecrees: {Decrees.Count} royal decrees, {Lines.Count} HUD lines and {Cards.Count} policy cards added.");
        }
    }

    private static string Id(string name) => $"0x5A12{(uint)name.GetHashCode():X8}";

    private static bool AddApprovals()
    {
        DecreeApprovalData template = null;
        foreach (DecreeApprovalData a in EntityDataManager.DecreeApprovalData)
        {
            if (a?.NameInDatabase == "DecreeApproval_HugoToras_WarProfitsTax")
            {
                template = a;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Decree d in Decrees)
        {
            for (int i = 0; i < Characters.Length && i < d.Approvals.Length; i++)
            {
                string name = ApprovalName(Characters[i], d.Key);
                var conditions = new Il2CppSystem.Collections.Generic.List<DecreeApprovalProperties.DecreeApprovalCondition>();
                conditions.Add(new DecreeApprovalProperties.DecreeApprovalCondition { Approval = d.Approvals[i], Condition = string.Empty });
                var data = new DecreeApprovalData
                {
                    Id = Id(name),
                    NameInDatabase = name,
                    Path = $"Rizia/Royal Decrees Panel/Decree Approvals/ZilleWar/{d.Category}",
                    IsWIP = false,
                    Notes = string.Empty,
                    AppBundleProperties = template.AppBundleProperties,
                    DecreeApprovalProperties = new DecreeApprovalProperties
                    {
                        DecreeApprovalCharacter = $"DecreeApproval_{Characters[i]}",
                        DecreeApprovalConditions = conditions,
                    },
                };
                ZilleOperation.AddByName(EntityDataManager.decreeApprovalData, data, x => x.NameInDatabase);
                ZilleOperation.AddByName(EntityDataManager.DecreeApprovalData, data, x => x.NameInDatabase);
            }
        }

        return true;
    }

    private static string ApprovalName(string character, string key) => $"DecreeApproval_{character}_ZilleWar_{key}";

    private static bool AddDecrees()
    {
        DecreeData template = Find("Economy_Taxes_WarProfitsTax");
        if (template?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Decree d in Decrees)
        {
            var approvals = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (string c in Characters)
            {
                approvals.Add(ApprovalName(c, d.Key));
            }

            var errors = new Il2CppSystem.Collections.Generic.List<DecreeProperties.ErrorValidationData>();
            foreach ((string message, string condition) in d.Errors)
            {
                errors.Add(new DecreeProperties.ErrorValidationData { ErrorMessage = message, ErrorCondition = condition });
            }

            bool enacted = Enacted(d.Key);
            var data = new DecreeData
            {
                Id = Id(Name(d.Key)),
                NameInDatabase = Name(d.Key),
                Path = d.Path,
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template.AppBundleProperties,
                DecreeProperties = new DecreeProperties
                {
                    Title = d.Title,
                    Description = d.Description,
                    CostText = d.Cost,
                    IsEnabled = EnabledExpr(d),
                    IsEnacted = enacted,
                    OnDecreeEnact = d.Enact,
                    DecreeApprovals = approvals,
                    HiddenUntilStepNo = -1,
                    HiddenUntilTurnNo = -1,
                    AssignedDecreePanel = Panel,
                    DecreeCategoryString = d.Category,
                    ErrorValidationDatas = errors,
                    IsNotificationActive = false,
                    DecreeCounselCondition = string.Empty,
                    DecreeCounselConversation = string.Empty,
                    DecreeCounselConfirmationText = string.Empty,
                },
            };
            TrySet(() => data.DecreeCategory = Enum.Parse<DecreeData.DecreeCategoryType>(d.Category));
            ZilleOperation.AddByName(EntityDataManager.decreesData, data, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.DecreesData, data, x => x.NameInDatabase);
            LastSeen[d.Key] = enacted;
        }

        return true;
    }

    private static bool AddLines()
    {
        HUDPeriodicStatModifierData template = null;
        foreach (HUDPeriodicStatModifierData m in EntityDataManager.HUDPeriodicStatModifierData)
        {
            if (m?.NameInDatabase == "Rizia_Budget_Modifier_WarExpenditures")
            {
                template = m;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Line l in Lines)
        {
            var data = new HUDPeriodicStatModifierData
            {
                Id = Id(l.Name),
                NameInDatabase = l.Name,
                Path = "Rizia/HUD Panel/Modifiers/ZilleWar",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template.AppBundleProperties,
                HUDPeriodicStatModifierProperties = new HUDPeriodicStatModifierProperties
                {
                    Variable = l.Variable,
                    Condition = l.Condition,
                    // vanilla's "{0}" already carries the sign
                    Description = l.Description.Substring(1),
                },
            };
            ZilleOperation.AddByName(EntityDataManager.hudPeriodicStatModifierData, data, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.HUDPeriodicStatModifierData, data, x => x.NameInDatabase);
        }

        foreach (var list in new[] { EntityDataManager.hudStatData, EntityDataManager.HUDStatData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (HUDStatData s in list)
            {
                var mods = s?.HUDStatProperties?.Modifiers;
                if (mods == null)
                {
                    continue;
                }

                foreach (Line l in Lines.Where(x => x.Stat == s.NameInDatabase))
                {
                    if (!mods.Contains(l.Name))
                    {
                        mods.Add(l.Name);
                    }
                }
            }
        }

        return true;
    }

    private static bool AddCards()
    {
        PolicyData template = null;
        foreach (PolicyData p in EntityDataManager.PoliciesData)
        {
            if (p?.NameInDatabase == "Economy_WarTax")
            {
                template = p;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Card c in Cards)
        {
            var data = new PolicyData
            {
                Id = Id(c.Name),
                NameInDatabase = c.Name,
                Path = $"Rizia/Policies/{c.Category}/Non Default Policies",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template.AppBundleProperties,
                TagsProperties = template.TagsProperties,
                PolicyProperties = new PolicyProperties
                {
                    Title = c.Title,
                    Description = c.Description,
                    IsEnabledVariable = c.Variable,
                    IsNotificationActive = false,
                },
                OverviewCategoryProperties = new OverviewCategoryProperties { OverviewCategoryString = c.Category },
            };
            TrySet(() => data.OverviewCategory = Enum.Parse<OverviewPanel.OverviewCategoryType>(c.Category));
            ZilleOperation.AddByName(EntityDataManager.policiesData, data, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.PoliciesData, data, x => x.NameInDatabase);
        }

        return true;
    }

    private static void TrySet(Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"WarDecrees: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------ enactment, values, notifications

    /// <summary>
    /// A decree whose entity turned enacted since the last step (the panel just signed it) and whose variable is still false
    /// has just been enacted: record it and apply the mod-side effects once. Otherwise the entity follows the saved variable.
    /// </summary>
    private static void Detect()
    {
        if (!_added)
        {
            return;
        }

        foreach (Decree d in Decrees)
        {
            DecreeData data = Find(Name(d.Key));
            DecreeProperties p = data?.DecreeProperties;
            if (p == null)
            {
                continue;
            }

            bool now = p.IsEnacted;
            bool saved = Enacted(d.Key);
            LastSeen.TryGetValue(d.Key, out bool before);
            if (now && !before && !saved)
            {
                saved = true;
                Variables.Set(EnactedVar(d.Key), true);
                Variables.Set(TurnVar(d.Key), WorldSystems.Turn);
                Core.Log.Msg($"WarDecrees: '{d.Title}' enacted on turn {WorldSystems.Turn}.");
                Run($"effects of {d.Key}", d.Enacted);
            }

            if (p.IsEnacted != saved)
            {
                p.IsEnacted = saved;
            }

            LastSeen[d.Key] = saved;
        }
    }

    private static void Compute()
    {
        foreach (Line l in Lines)
        {
            int v = l.Value();
            if (Variables.GetInt(l.Variable) != v)
            {
                Variables.Set(l.Variable, v);
            }
        }

        foreach (Card c in Cards)
        {
            bool on = c.When();
            if (Variables.GetBool(c.Variable) != on)
            {
                Variables.Set(c.Variable, on);
            }
        }

        int leverage = 0;
        foreach (Decree d in Decrees)
        {
            if (d.Open != null)
            {
                bool open;
                try
                {
                    open = d.Open();
                }
                catch (Exception ex)
                {
                    Core.Log.Error($"WarDecrees: gate of {d.Key} failed: {ex.Message}");
                    open = false;
                }

                if (Variables.GetBool(OpenVar(d.Key)) != open)
                {
                    Variables.Set(OpenVar(d.Key), open);
                }
            }

            if (d.Leverage != null && Enacted(d.Key))
            {
                try
                {
                    leverage += d.Leverage();
                }
                catch (Exception ex)
                {
                    Core.Log.Error($"WarDecrees: leverage of {d.Key} failed: {ex.Message}");
                }
            }
        }

        if (Variables.GetInt(LeverageVar) != leverage)
        {
            Variables.Set(LeverageVar, leverage);
        }
    }

    /// <summary>Once per new turn, run every tick (the first turn seen is only recorded).</summary>
    private static void Tick()
    {
        int turn = WorldSystems.Turn, last = Variables.GetInt(LastTickVar);
        if (turn <= 0 || turn == last)
        {
            return;
        }

        Variables.Set(LastTickVar, turn);
        if (last <= 0 || turn < last)
        {
            return;
        }

        foreach (Action t in Ticks)
        {
            Run("tick", t);
        }
    }

    /// <summary>Add our rows to vanilla's recompute conditional instructions (idempotent; matched by entry name).</summary>
    private static void AddRows()
    {
        if (Rows.Count == 0)
        {
            return;
        }

        foreach (var list in new[] { EntityDataManager.conditionalInstructionData, EntityDataManager.ConditionalInstructionData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (ConditionalInstructionData c in list)
            {
                string name = c?.NameInDatabase;
                if (name == null || !RowTargets.Contains(name))
                {
                    continue;
                }

                var rows = c.ConditionalInstructionProperties?.ConditionalInstructions;
                if (rows == null)
                {
                    continue;
                }

                foreach ((string ci, string cond, string instr) in Rows)
                {
                    if (name != ci)
                    {
                        continue;
                    }

                    bool present = false;
                    foreach (ConditionalInstruction r in rows)
                    {
                        present |= r?.Condition == cond && r.Instruction == instr;
                    }

                    if (!present)
                    {
                        rows.Add(new ConditionalInstruction { Condition = cond, Instruction = instr });
                    }
                }
            }
        }
    }

    /// <summary>Light the panel's notification once, when a decree first becomes available.</summary>
    private static void Notify()
    {
        if (!_added)
        {
            return;
        }

        foreach (Decree d in Decrees)
        {
            if (Variables.GetBool(NotifiedVar(d.Key)) || Enacted(d.Key))
            {
                continue;
            }

            DecreeData data = Find(Name(d.Key));
            if (data == null || !data.IsEnabled)
            {
                continue;
            }

            Variables.Set(NotifiedVar(d.Key), true);
            TrySet(() => data.IsNotificationActive = true);
        }
    }

    // ------------------------------------------------------------------ once-only consequences after the war

    private const string CushionFlag = "ZilleWar.Decree_WarCensorship_Cushion";
    private const string RationingEndFlag = "ZilleWar.Decree_FuelRationing_Ended";
    private const string PricesEndFlag = "ZilleWar.Decree_PriceControls_Ended";
    private const string DesertersFlag = "ZilleWar.Decree_DeserterSafeConduct_After";
    private const string LegionFlag = "ZilleWar.Decree_ZilleanLegion_After";
    private const string Opinion = "RiziaDLC.Country_PublicOpinion";

    private static readonly string[] OnceFlags = { CushionFlag, RationingEndFlag, PricesEndFlag, DesertersFlag, LegionFlag };

    private static void ShiftOpinion(int delta)
    {
        if (delta != 0)
        {
            Variables.Set(Opinion, Variables.GetInt(Opinion) + delta);
        }
    }

    /// <summary>
    /// Once-only consequences of the wartime decrees after the guns fall silent. Each fires once, when its condition first
    /// holds; a save that reaches it long after the war (an older build) only records it, without a late effect or news.
    /// </summary>
    private static void Aftermath()
    {
        if (!LateState.Over)
        {
            return;
        }

        int since = Since;
        bool fresh = since is >= 0 and <= 1;
        // the defeat is reported late and softened (the war-end swing was -3 or -1)
        Once(CushionFlag, Enacted(Censorship) && Lost && Variables.GetBool(VictoryParade.VarOpinionApplied), fresh, CensorshipCushion);
        // the coupons go with the war: the price in opinion was the war's, not forever
        Once(RationingEndFlag, Enacted(Rationing), fresh, RationingEnds);
        Once(PricesEndFlag, Enacted(Prices), fresh, PricesEnd);
        Once(DesertersFlag, Enacted(Deserters) && since >= 1, since <= 2, DesertersRemembered);
        Once(LegionFlag, Enacted(Legion) && Lost && since >= 1, since <= 2, LegionReprisals);
    }

    private static void Once(string flag, bool when, bool timely, Action effect)
    {
        if (!when || Variables.GetBool(flag))
        {
            return;
        }

        Variables.Set(flag, true);
        if (timely)
        {
            Run(flag, effect);
        }
    }

    private static void FootingTick()
    {
        if (!Enacted(Footing) || !AtWar)
        {
            return;
        }

        const string equipment = "RiziaDLC.Resources_Military_Equipment";
        Variables.Set(equipment, Variables.GetInt(equipment) + 500);
        Core.Log.Msg("WarDecrees: +500 military equipment from the Iza works.");
    }

    private static void CensorshipCushion()
    {
        ShiftOpinion(1);
        News("WarCensorship_Cushion", "RH", "An Orderly Withdrawal", NewsWire.P(
            "The General Staff's communique on the end of the fighting in Zille speaks of an orderly withdrawal to prepared positions, " +
            "carried out without the loss of a single gun. Our regiments, it says, have kept their colors and their discipline.",
            "\"The army has not been beaten,\" a staff officer told the Royal Herald. \"It has been asked to wait.\""));
    }

    private static void RationingEnds()
    {
        ShiftOpinion(1);
        News("FuelRationing_Ended", "RH", "Coupons Withdrawn", NewsWire.P(
            "With the guns silent in Zille, the Palace has announced the end of wartime fuel rationing. Post offices will stop issuing " +
            "coupons at the end of the week, and private motoring is no longer limited to two days.",
            "Queues formed at the pumps of Porte Drazon again yesterday, this time to fill up. \"I kept my coupons,\" one driver said. " +
            "\"I will frame them.\""));
    }

    private static void PricesEnd()
    {
        if (FreePressRevoked)
        {
            return;
        }

        News("PriceControls_Ended", "V", "The Black Market Closes Its Doors", NewsWire.P(
            "The wartime price controls on bread and fuel have lapsed with the war, and the Treasury has stopped paying millers and " +
            "fuel merchants the difference. In the back streets of Bronaz and Iza, the men who sold flour and coal at twice the fixed " +
            "price have packed up their stalls.",
            "Bread now costs what it costs. The Voice of Rizia notes that the families who could not pay the black market's prices " +
            "during the war are not much better placed to pay the baker's now."));
    }

    private static void DesertersRemembered() =>
        News("DeserterSafeConduct_After", "G", "Wraclawitz Stops the Pensions of Deserters' Families", NewsWire.P(
            "WRACLAWITZ, WEHLEN - The Wehzek defense ministry has struck from its pension rolls the families of soldiers who crossed to " +
            "the Rizian lines in Zille under the safe-conduct offer of King Romus. Several hundred households are affected.",
            "Most of the deserters are still in Rizian camps. Few of them have asked to be sent home."));

    private static void LegionReprisals() =>
        News("ZilleanLegion_After", "G", "Reprisals Against Legion Families in Zille", NewsWire.P(
            "ZILLE - Wehzek police have begun arresting the relatives of Zilleans who enlisted in the Rizian Legion during the war. " +
            "Families in the harbor district report that their homes were searched and their ration cards withdrawn.",
            "Wraclawitz describes the arrests as \"an inquiry into collaboration with the enemy.\" Alliance observers on the line have " +
            "asked to visit the detained, so far without an answer."));

    // ------------------------------------------------------------------ what each decree does beyond its vanilla script

    private static void News(string key, string paper, string title, string text) =>
        NewsWire.Publish($"Decree_{key}", paper, title, text);

    private static bool FreePressRevoked => Variables.GetBool("RiziaDLC.Policy_RD_Welfare_FreePressRevoked");

    private static void OnReserves() => NewsWire.Batch(() =>
    {
        News(Reserves, "RH", "The Reserves of Brenas Answer the Call", NewsWire.P(
            "By royal decree, the reserve divisions of Brenas have been recalled to the colors for the duration of the campaign in " +
            "Zille. By noon yesterday the depots in Sallabes and Mave Nortus were turning men away for want of uniforms.",
            "\"They gave Zille away while my father was still working the pits,\" one miner told the Royal Herald outside the Sallabes " +
            "depot. \"I would like to see it Rizian before I go back down.\""));
        News(Reserves, "V", "Brenas Sends Its Miners to the Front", NewsWire.P(
            "The call-up of the Brenas reserves has emptied the night shifts of the province's mines. Managers in Zpana and Argno expect " +
            "output to fall by a fifth before the month is out, while the wives of reservists queue at the paymasters' offices for the " +
            "first of the Crown's allowances.",
            "The Voice of Rizia asks how long the treasury can pay an army that grows by the week, and who will dig the coal while it does."));
    });

    private static void OnCensorship() => NewsWire.Batch(() =>
    {
        News(Censorship, "RH", "General Staff to Review War Reporting", NewsWire.P(
            "Under a royal decree issued yesterday, every dispatch from the Zille front will be read by officers of the General Staff " +
            "before publication. \"Every word printed about the position of our men is a word read in Wraclawitz,\" a palace " +
            "spokesman said.",
            "The Royal Herald welcomes the measure, and will go on bringing its readers the news of our victories as they come."));
        if (!FreePressRevoked)
        {
            News(Censorship, "V", "White Columns", NewsWire.P(
                "Readers of this morning's Voice of Rizia will find white columns where our reporting from Zille should have been. The " +
                "censors of the General Staff removed the casualty figures, the names of two regiments and a photograph of a field " +
                "hospital outside Mave Nortus.",
                "We have left the spaces empty, so that our readers can see how much of the war they are no longer allowed to see."));
        }

        News(Censorship, "G", "Rizia Imposes Military Censorship", NewsWire.P(
            "PORTE DRAZON, RIZIA - The Rizian government has placed all reporting on its war with Wehlen under military censorship. " +
            "Foreign correspondents in Mave Nortus have been told that their dispatches must be cleared by army officers before they " +
            "are cabled abroad.",
            "The Valgish foreign ministry called the decree \"the act of a government with something to hide.\""));
    });

    private static void OnShipping() => NewsWire.Batch(() =>
    {
        News(Shipping, "RH", "The Merchant Fleet Joins the War Effort", NewsWire.P(
            "Freighters and ferries of the merchant marine are being repainted in navy grey in the yards of Monqiz and Fort Ales, after " +
            "the Crown requisitioned them for the campaign in Zille. The Admiralty reports that their crews have volunteered almost to " +
            "a man.",
            "The first of them has already sailed north with a battalion of the Royal Marines aboard."));
        News(Shipping, "V", "Monqiz Shipowners Count Their Losses", NewsWire.P(
            "The trading houses of Monqiz will be paid for their ships at the Admiralty's rates, which their owners say come to barely " +
            "a third of what the vessels earn in peacetime.",
            "Cargoes bound for Sordland and Rumburg are waiting on the quays for hulls that now carry soldiers, and the dockers who " +
            "loaded them are waiting with them."));
    });

    private static void OnRationing() => NewsWire.Batch(() =>
    {
        News(Rationing, "RH", "Every Drop for the Front", NewsWire.P(
            "Ration coupons for petrol and heating oil went on sale at post offices across the Kingdom yesterday, as the royal decree " +
            "on wartime fuel rationing came into force. Private motoring is now limited to two days a week.",
            "\"It is a small sacrifice next to the one our soldiers are making,\" a Porte Drazon taxi driver told the Royal Herald, " +
            "standing beside his car."));
        News(Rationing, "V", "Queues at the Pumps", NewsWire.P(
            "Queues stretched around the block at petrol stations in Porte Drazon, Montaklar and Bronaz on the first day of fuel " +
            "rationing. Farmers in Cardesse-Montaklar warn that the harvest cannot be brought in on two days of diesel a week.",
            "Taxi drivers in the capital say they will be out of work by the end of the month. The fuel, the Palace assures them, is " +
            "going to the front."));
    });

    private static void OnDeserters()
    {
        int before = ZilleCampaign.Strength;
        if (before > 3)
        {
            ZilleCampaign.Strength = before - 1;
        }

        Core.Log.Msg($"WarDecrees: Wehzek garrison strength {before} -> {ZilleCampaign.Strength} (deserters).");
        NewsWire.Batch(() =>
        {
            News(Deserters, "RH", "Safe Conduct Offered to Wehzek Soldiers", NewsWire.P(
                "Army aircraft dropped leaflets over the Wehzek lines in Zille last night, printed in Wehzek and signed in the King's " +
                "name, promising safe conduct, food and a reward to any soldier of the garrison who lays down his arms. The army's radio " +
                "repeats the offer every hour.",
                "Officers at the front report that the first men came over before dawn, cold, hungry and glad to be done with " +
                "Smolak's war."));
            News(Deserters, "G", "Wehzek Soldiers Cross the Lines in Zille", NewsWire.P(
                "MAVE NORTUS, RIZIA - Rizia's offer of safe conduct to Wehzek deserters appears to be bearing fruit. Small groups of " +
                "soldiers from the Zille garrison have been seen crossing to Rizian positions under white flags.",
                "Wraclawitz radio called the offer \"bribery\" and announced that the families of deserters would lose their state pensions."));
        });
    }

    private static void OnProtect() => NewsWire.Batch(() =>
    {
        News(Protect, "RH", "The King's Protection for His Wehzek Subjects", NewsWire.P(
            "By royal decree, the Crown has placed its subjects of Wehzek descent under its protection, during the war and after it. " +
            "Police posts now stand at the entrances of Little Wraclawitz in Iza, and attacks on Wehzek homes, shops and sanctuaries " +
            "will be prosecuted as offences against the Crown.",
            "\"His Majesty is at war with an army, not with a people,\" a palace spokesman said."));
        News(Protect, "V", "Little Wraclawitz Opens Its Shutters", NewsWire.P(
            "For the first time since the declaration of war, the shops of Little Wraclawitz opened their shutters yesterday. A " +
            "policeman stood outside the bakery whose window was broken in the first days of the war.",
            "\"I put my sign back up,\" the baker told our reporter. \"In Wehzek and in Rizian, both.\""));
        News(Protect, "G", "Rizia Shields Its Wehzek Minority", NewsWire.P(
            "PORTE DRAZON, RIZIA - King Romus Toras has decreed the protection of Rizia's Wehzek minority, after weeks of attacks on " +
            "Wehzek property in Iza and Brenas.",
            "The Valgish foreign ministry, which had warned of reprisals against Wehzeks in Rizia, said in a short statement that it " +
            "\"took note\" of the decree."));
    });

    private static void OnIntern() => NewsWire.Batch(() =>
    {
        News(Intern, "RH", "Enemy Nationals to Be Interned", NewsWire.P(
            "Under a royal decree issued yesterday, Wehzek nationals living in the Kingdom, and subjects of Wehzek descent whose " +
            "loyalty is in doubt, must register with the police. Men of fighting age will be held in camps in the interior until the " +
            "end of the war.",
            "The security services say the measure will put an end to Wehlen's network of informants in Iza and Brenas."));
        if (!FreePressRevoked)
        {
            News(Intern, "V", "The Trucks Came Before Dawn", NewsWire.P(
                "Police trucks drove into Little Wraclawitz before dawn yesterday. By morning, the men of the quarter between eighteen " +
                "and fifty were gone, among them shopkeepers who have paid Rizian taxes for thirty years and sons who were born in Iza.",
                "In the mining towns of Brenas, foremen say a third of the night shift did not come to work. Nobody will say where the " +
                "camps are."));
        }

        News(Intern, "G", "Rizia Interns Its Wehzeks", NewsWire.P(
            "PORTE DRAZON, RIZIA - Rizia has begun interning Wehzek nationals and members of its Wehzek minority in camps away from " +
            "the front, in a measure the Rizian government describes as a wartime security precaution.",
            "Valgsland summoned the Rizian ambassador and called the camps \"a disgrace that Merkopa will not forget.\" Wraclawitz radio " +
            "has broadcast little else since."));
    });

    private static void OnPensions()
    {
        string quote = Lib
            ? "\"They gave Rizia back what was taken from her,\" a palace spokesman said. \"The Kingdom owes them no less.\""
            : Part
                ? "\"They held Zille city when nobody thought it could be held,\" a palace spokesman said. \"The Kingdom owes them no less.\""
                : "\"They did everything that was asked of them,\" a palace spokesman said. \"The Kingdom owes them no less.\"";
        NewsWire.Batch(() =>
        {
            News(Pensions, "RH", "The Crown Remembers Its Soldiers", NewsWire.P(
                "Every soldier who served in the Zille campaign will receive a royal pension and a medal struck at the Porte Drazon mint, " +
                "under a decree signed by His Majesty yesterday. The widows and orphans of the fallen will be pensioned by the Crown " +
                "for life.",
                quote));
            News(Pensions, "V", "A Pension, at Last", NewsWire.P(
                "The Voice of Rizia has counted the cost of the war in Zille line by line, and the pensions decreed yesterday will add to " +
                "it for years.",
                "This time, we do not object. Of all the war's bills, it is the one most honestly owed."));
        });
    }

    private static void OnStandDown() => NewsWire.Batch(() =>
    {
        News(StandDown, "RH", "The Brenas Divisions Come Home", NewsWire.P(
            "The reserve divisions of Brenas held their last pay parade in Sallabes yesterday, before boarding the trains that will take " +
            "them home to the mines and farms of the province. Their rifles have been handed in at the depots.",
            "A band played them out of the station. \"They answered when the Crown called,\" their commanding officer said. \"Now Brenas " +
            "can have them back.\""));
        if (!FreePressRevoked)
        {
            News(StandDown, "V", "Back to the Pits", NewsWire.P(
                "The night shifts are full again in Zpana and Argno, and the mine managers who spent the war short of hands are " +
                "relieved. The Crown's allowances to the reservists' wives end with this month's payment.",
                "Not every family will see a son come home. In the mining towns, the Voice of Rizia met widows who will collect the " +
                "last allowance and nothing after it."));
        }

        if ((Part || Lost) && !Variables.GetBool(ZilleTreaty.VarPeace))
        {
            News(StandDown, "G", "Rizia Sends Its Reservists Home", NewsWire.P(
                "SALLABES, RIZIA - Rizia has released the reserve divisions it called up for the war in Zille.",
                "Regular troops now hold the line of contact alone, although no peace has been made with Wehlen."));
        }
    });

    private static void OnLiftCensorship()
    {
        ShiftOpinion(Lib ? 1 : Lost ? -1 : 0);
        NewsWire.Batch(() =>
        {
            if (!FreePressRevoked)
            {
                News(LiftCensor, "V", "The White Columns Are Filled", NewsWire.P(
                    "For the first time since the war began, this newspaper goes to print without an officer of the General Staff reading " +
                    "over our shoulder. Today we publish what the censors held back: the casualty lists, the names of the regiments, " +
                    "and the photograph from the field hospital outside Mave Nortus.",
                    Lib
                        ? "The victory in Zille was real, and so was its price. Our readers may now see both."
                        : Lost
                            ? "Our readers will find that the war went worse, and earlier, than they were told."
                            : "Our readers may now judge for themselves what was won in Zille, and what it cost."));
            }

            News(LiftCensor, "G", "Rizia Ends Military Censorship", NewsWire.P(
                "PORTE DRAZON, RIZIA - The Rizian government has lifted the military censorship it imposed on reporting from Zille. " +
                "Foreign correspondents may again cable their dispatches without submitting them to army officers.",
                "Valgish diplomats welcomed the decision, which they described as \"overdue.\""));
        });
    }

    private static void OnReleaseInternees() => NewsWire.Batch(() =>
    {
        if (!FreePressRevoked)
        {
            News(ReleaseInterned, "V", "The Trucks Come Back", NewsWire.P(
                "The same police trucks that drove into Little Wraclawitz before dawn when the camps were opened drove back into it " +
                "yesterday afternoon. The men they brought home carried railway warrants, a week's pay and very little else.",
                "Some of the shops they left will open again on Monday. Others were sold while their owners were in the camps, and " +
                "the royal administrators who sold them have yet to explain to whom."));
        }

        News(ReleaseInterned, "G", "Rizia Closes Its Internment Camps", NewsWire.P(
            "PORTE DRAZON, RIZIA - Rizia has released the Wehzek nationals and subjects of Wehzek descent it interned during the war " +
            "in Zille, and has begun returning their property.",
            "The Valgish foreign ministry said that it \"notes the release,\" and that it had expected it much sooner."));
    });

    private static void OnLoan() => NewsWire.Batch(() =>
    {
        News(Loan, "RH", "The Banks Answer the Crown", NewsWire.P(
            "The banks of Porte Drazon and Monqiz have subscribed in full the Crown's loan for the campaign in Zille, the Royal " +
            "Treasury announced yesterday. The money will pay the army's wages and shells for as long as the war lasts.",
            "\"Rizian capital stands behind Rizian soldiers,\" a Treasury official said after the signing."));
        if (!FreePressRevoked)
        {
            News(Loan, "V", "A War Paid for on Credit", NewsWire.P(
                "The Crown has borrowed to pay for the war in Zille, and will repay the bankers, with interest, for five years after " +
                "the guns fall silent. The terms of the loan have not been published.",
                "The Voice of Rizia asks who will pay the installments. Not the men who signed for them, but our children, in schools " +
                "and hospitals that will not be built."));
        }
    });

    private static void OnSeize() => NewsWire.Batch(() =>
    {
        News(Seize, "RH", "Wehzek Assets Placed Under Sequestration", NewsWire.P(
            "By royal decree, the accounts of the Wehzek state in Rizian banks, the property of its consulates and the cargoes of its " +
            "ships in Rizian ports have been placed under royal sequestration for the duration of the war.",
            "\"Wraclawitz will not pay for its garrison in Zille with money kept in Monqiz,\" a Treasury spokesman said."));
        News(Seize, "G", "Rizia Seizes Wehzek State Assets", NewsWire.P(
            "PORTE DRAZON, RIZIA - Rizia has frozen Wehzek state accounts and seized Wehzek ships and cargoes in its ports. " +
            "Wraclawitz radio called the decree \"theft under the cover of war\".",
            "Foreign bankers in Monqiz are said to be uneasy, and some have begun to ask whose deposits will be next."));
    });

    private static void OnFooting() => NewsWire.Batch(() =>
    {
        News(Footing, "RH", "Iza Works for the Front", NewsWire.P(
            "The arms works and foundries of Iza have been placed under the orders of the War Office, and the first trains of shells " +
            "stamped with the city's name left for the Zille front last night.",
            "\"Every shift in Iza is a shift at the front,\" the director of the largest works told the Royal Herald."));
        if (!FreePressRevoked)
        {
            News(Footing, "V", "Double Shifts and Empty Shelves", NewsWire.P(
                "Workers in Iza's arms works are now on twelve-hour shifts, and the factories that made pots, bicycles and sewing " +
                "machines make shell casings instead. The shops of the city have begun to run short of all three.",
                "The owners of the works will be paid handsomely by the War Office. Their workers, the Voice of Rizia has learned, " +
                "will be paid as before."));
        }
    });

    private static void OnLegion() => NewsWire.Batch(() =>
        News(Legion, "RH", "Zilleans Enlist Under the Rizian Flag", NewsWire.P(
            "Hundreds of Zilleans who left the province during the lease, and the sons of harbor families settled in the south, " +
            "queued at the recruiting offices of Porte Drazon and Sallabes yesterday to join the new Zillean Legion.",
            "\"My grandfather built boats in the harbor,\" one volunteer said. \"I would like to see it before I am too old to walk " +
            "there.\"")));

    private static void OnPrices() => NewsWire.Batch(() =>
        News(Prices, "RH", "Bread at Yesterday's Price", NewsWire.P(
            "By royal decree, the prices of bread, flour, cooking oil, coal and fuel have been fixed at their level on the day war " +
            "was declared. The Treasury will pay millers, bakers and fuel merchants the difference.",
            "The Rizian People's Party, which proposed the measure, called it \"the least the Crown owes the families who send their " +
            "sons to Zille.\" Merchants' associations in Porte Drazon have protested.")));
}
