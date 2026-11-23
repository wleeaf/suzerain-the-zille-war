using Il2Cpp;
using SuzerainModdingKit;
using ConditionalInstruction = Il2Cpp.ConditionalInstructionProperties.ConditionalInstruction;

namespace ZilleWar;

/// <summary>
/// The war on the HUD and in the Overview panel, built the way vanilla builds its own lines:
/// - HUD per-turn lines ("-2 per turn from Zille War Expenditures"): a HUDPeriodicStatModifierData whose name is listed
///   in the stat's Modifiers, a value variable and a condition. Values are recomputed at every step from the war's state.
///   Lines fed by other modules (Rumburg credit, Lespian fees and gas...) only read their variables; see the Mod_* constants.
/// - Overview situations and a policy, driven by plain variables.
/// - Vanilla lines the war ends (Wehlen trade) are switched off, unused vanilla content is put to work
///   (Maritime Trade in Turmoil, Rumburg Suzerain, ATO Influence, the Lespian sanctions line).
/// - The war's weight on the economy, living standards, global perception and power projection, as extra rows in vanilla's
///   recompute-from-scratch conditional instructions. Never stacked on top of vanilla's always-on Pales war rows.
/// </summary>
internal static class WorldModifiers
{
    // ---- values other modules set (signed per-turn amounts; 0 hides the line). A matching "<name>_Turns" counter, when
    //      above 0, counts the line down one per new turn and clears the value when it reaches 0 (0 = until changed).
    public const string RumburgCredit = "ZilleWar.Mod_RumburgCredit";           // budget, positive: Rumburgian war credits flowing in
    public const string RumburgRepayment = "ZilleWar.Mod_RumburgRepayment";     // budget, negative: repaying them
    public const string RumburgGas = "ZilleWar.Mod_RumburgGas";                 // energy, negative: gas sent to Rumburg for the embargo
    public const string LespiaEscort = "ZilleWar.Mod_LespiaEscort";             // budget, negative: Lespian escort fees
    public const string LespiaGas = "ZilleWar.Mod_LespiaGas";                   // energy, negative: discounted gas to LesPower
    public const string LespiaGasBudget = "ZilleWar.Mod_LespiaGasBudget";       // budget, positive: what LesPower pays for it
    public const string BffContracts = "ZilleWar.Mod_BFFContracts";             // budget, negative: BFF contracts paid by Rizia
    public const string WehlenReparations = "ZilleWar.Mod_WehlenReparations";   // budget, negative: reparations after a lost war
    public const string CaleqabizRent = "ZilleWar.Mod_CaleqabizRent";           // budget, positive: Lespia's rent for its 50-year lease
    /// <summary>Bool: the Strait of Kreleck is closed to Wehzek shipping by someone (Lespia, our fleet). Ends "Maritime Trade in Turmoil".</summary>
    public const string StraitClosed = "ZilleWar.World_StraitClosed";

    private static readonly string[] External =
        { RumburgCredit, RumburgRepayment, RumburgGas, LespiaEscort, LespiaGas, LespiaGasBudget, BffContracts, WehlenReparations, CaleqabizRent,
          ZilleTreaty.ModReparations, ZilleTreaty.ModOil, ZilleTreaty.ModStrait, ZilleTreaty.ModTrade };

    // ---- values computed here
    private const string WarExpenditures = "ZilleWar.Mod_WarExpenditures";
    private const string Occupation = "ZilleWar.Mod_Occupation";
    private const string Reconstruction = "ZilleWar.Mod_Reconstruction";
    private const string ZilleGold = "ZilleWar.Mod_ZilleGold";
    private const string ZillePort = "ZilleWar.Mod_ZillePort";
    private const string ZilleEnergy = "ZilleWar.Mod_ZilleEnergy";
    private const string Refugees = "ZilleWar.Mod_Refugees";
    private const string Shipping = "ZilleWar.Mod_Shipping";
    private const string WarBonds = "ZilleWar.Mod_WarBonds";
    private const string WarFuel = "ZilleWar.Mod_WarFuel";
    private const string Fervor = "ZilleWar.Mod_Fervor";
    private const string Weariness = "ZilleWar.Mod_Weariness";
    private const string TwoFronts = "ZilleWar.Mod_TwoFronts";
    private const string Humiliation = "ZilleWar.Mod_Humiliation";
    private const string Victory = "ZilleWar.Mod_Victory";
    private const string Tutelage = "ZilleWar.Mod_RumburgTutelage";
    private const string LeaseShame = "ZilleWar.Mod_CaleqabizLeaseAuthority";   // authority, negative: a Lespian flag over Caleqabiz

    private static readonly string[] Computed =
    {
        WarExpenditures, Occupation, Reconstruction, ZilleGold, ZillePort, ZilleEnergy, Refugees, Shipping, WarBonds, WarFuel,
        Fervor, Weariness, TwoFronts, Humiliation, Victory, Tutelage, LeaseShame,
    };

    // ---- situation / policy switches
    private const string SitOccupation = "ZilleWar.Sit_Occupation";
    private const string SitRefugees = "ZilleWar.Sit_Refugees";
    private const string SitLost = "ZilleWar.Sit_Lost";
    private const string SitDivided = "ZilleWar.Sit_Divided";
    private const string SitReconstruction = "ZilleWar.Sit_Reconstruction";
    private const string PolRepudiated = "ZilleWar.Pol_ZLLARepudiated";
    private const string SitCeasefire = "ZilleWar.Sit_Ceasefire";
    private const string VarLastTurn = "ZilleWar.Mod_LastTurn";

    private const string Budget = "Rizia_HUDStat_Budget";
    private const string Energy = "Rizia_HUDStat_Energy";
    private const string Authority = "Rizia_HUDStat_Authority";

    private sealed record Line(string Name, string Stat, string Variable, string Condition, string Description);

    private static string Neg(string v) => $"{v} <= -1";
    private static string Pos(string v) => $"{v} >= 1";

    private static readonly Line[] Lines =
    {
        // budget
        new("Rizia_Budget_Modifier_ZilleWarExpenditures", Budget, WarExpenditures, Neg(WarExpenditures), "{0} per turn from Zille War Expenditures"),
        new("Rizia_Budget_Modifier_ZilleOccupation", Budget, Occupation, Neg(Occupation), "{0} per turn from Military Administration of Zille"),
        new("Rizia_Budget_Modifier_ZilleReconstruction", Budget, Reconstruction, Neg(Reconstruction), "{0} per turn from Zille Reconstruction"),
        new("Rizia_Budget_Modifier_ZilleGoldMines", Budget, ZilleGold, Pos(ZilleGold), "{0} per turn from Zille Gold Mines"),
        new("Rizia_Budget_Modifier_ZillePort", Budget, ZillePort, Pos(ZillePort), "{0} per turn from the Port of Zille"),
        new("Rizia_Budget_Modifier_ZilleRefugees", Budget, Refugees, Neg(Refugees), "{0} per turn from Zille Refugee Relief"),
        new("Rizia_Budget_Modifier_ShippingDisruption", Budget, Shipping, Neg(Shipping), "{0} per turn from Disrupted Maritime Trade"),
        new("Rizia_Budget_Modifier_ZilleWarBonds", Budget, WarBonds, Pos(WarBonds), "{0} per turn from Zille Diaspora War Bonds"),
        new("Rizia_Budget_Modifier_RumburgWarCredits", Budget, RumburgCredit, Pos(RumburgCredit), "{0} per turn from Rumburgian War Credits"),
        new("Rizia_Budget_Modifier_RumburgCreditRepayment", Budget, RumburgRepayment, Neg(RumburgRepayment), "{0} per turn from Rumburgian War Credit Repayments"),
        new("Rizia_Budget_Modifier_LespianEscortFees", Budget, LespiaEscort, Neg(LespiaEscort), "{0} per turn from Lespian Naval Escort Fees"),
        new("Rizia_Budget_Modifier_LesPowerGasSales", Budget, LespiaGasBudget, Pos(LespiaGasBudget), "{0} per turn from Gas Sales to LesPower"),
        new("Rizia_Budget_Modifier_CaleqabizLease", Budget, CaleqabizRent, Pos(CaleqabizRent), "{0} per turn from the Lespian Lease of Caleqabiz"),
        new("Rizia_Budget_Modifier_BFFContracts", Budget, BffContracts, Neg(BffContracts), "{0} per turn from Bludish Freedom Front Contracts"),
        new("Rizia_Budget_Modifier_WehlenReparations", Budget, WehlenReparations, Neg(WehlenReparations), "{0} per turn from Reparations to Wehlen"),
        new("Rizia_Budget_Modifier_WehzekReparations", Budget, ZilleTreaty.ModReparations, Pos(ZilleTreaty.ModReparations), "{0} per turn from Wehzek War Reparations"),
        new("Rizia_Budget_Modifier_WehzekMarket", Budget, ZilleTreaty.ModTrade, Pos(ZilleTreaty.ModTrade), "{0} per turn from Tariff-Free Trade with Wehlen"),
        new("Rizia_Budget_Modifier_KreleckPassage", Budget, ZilleTreaty.ModStrait, Pos(ZilleTreaty.ModStrait), "{0} per turn from Free Passage through the Strait of Kreleck"),
        // the vanilla variable Turn 9 MITZ already moves, never shown in vanilla; only in a war playthrough
        new("Rizia_Budget_Modifier_LespiaSanctions", Budget, "RiziaDLC.Budget_Modifier_LespiaSanctions",
            "RiziaDLC.Situation_Diplomacy_Lespia_SanctionsRizia == true && ZilleWar.War_Declared == true", "{0} per turn from Lespian Sanctions"),
        // energy
        new("Rizia_Energy_Modifier_ZilleWarFuel", Energy, WarFuel, Neg(WarFuel), "{0} per turn from Wartime Fuel Consumption"),
        new("Rizia_Energy_Modifier_LesPowerDiscountGas", Energy, LespiaGas, Neg(LespiaGas), "{0} per turn from Discounted Gas Sales to LesPower"),
        new("Rizia_Energy_Modifier_RumburgWarGas", Energy, RumburgGas, Neg(RumburgGas), "{0} per turn from Gas Shipments to Rumburg"),
        new("Rizia_Energy_Modifier_ZillePowerStations", Energy, ZilleEnergy, Pos(ZilleEnergy), "{0} per turn from Zille Power Stations"),
        new("Rizia_Energy_Modifier_TreatyOil", Energy, ZilleTreaty.ModOil, Pos(ZilleTreaty.ModOil), "{0} per turn from Wehzek Oil under the Treaty"),
        // authority
        new("Rizia_Authority_Modifier_ZilleWarFervor", Authority, Fervor, Pos(Fervor), "{0} per turn from Zille War Fervor"),
        new("Rizia_Authority_Modifier_ZilleVictory", Authority, Victory, Pos(Victory), "{0} per turn from Victory in Zille"),
        new("Rizia_Authority_Modifier_ZilleWarWeariness", Authority, Weariness, Neg(Weariness), "{0} per turn from War Weariness"),
        new("Rizia_Authority_Modifier_TwoFrontWar", Authority, TwoFronts, Neg(TwoFronts), "{0} per turn from Two-Front War Strain"),
        new("Rizia_Authority_Modifier_ZilleDefeat", Authority, Humiliation, Neg(Humiliation), "{0} per turn from the Defeat in Zille"),
        new("Rizia_Authority_Modifier_CaleqabizLease", Authority, LeaseShame, Neg(LeaseShame), "{0} per turn from the Lespian Flag over Caleqabiz"),
        new("Rizia_Authority_Modifier_RumburgTutelage", Authority, Tutelage, Neg(Tutelage), "{0} per turn from Rumburgian Tutelage"),
    };

    /// <summary>Vanilla Wehlen lines: off for good once the war is declared (Wehlen stays hostile after it).</summary>
    private static readonly string[] WehlenLines =
    {
        "Rizia_Budget_Modifier_WehlenTrade", "Rizia_Energy_Modifier_WehlenGasExport", "Rizia_Energy_Modifier_WehlenOilImport",
        "Rizia_Equipment_Modifier_WehlenArmsExports", "Rizia_Budget_Modifier_WehlenPipeline",
    };

    private sealed record Card(string Name, string Category, string Severity, string Variable, string Title, string Description);

    private static readonly Card[] Situations =
    {
        new("ZilleWar_Diplomacy_Wehlen_War", "Diplomacy", "High", WarState.VarActive, "State of War with the Republic of Wehlen",
            "The declaration of war against the Republic of Wehlen, made before the Alliance of Nations in Konstantium, ended thirty " +
            "years of uneasy lease and began a campaign to restore the territory of Zille to the Crown. \n\nThe conflict turns the " +
            "border at Mave Nortus into a front line and puts Rizian shipping within reach of Wehzek raiders in the Strait of Kreleck. " +
            "Success depends on swift operations in Zille and on keeping the friends who lend their navies, their credit and their oil."),
        new("ZilleWar_Military_TwoFronts", "Military", "High", WarFlags.TwoWars, "War on Two Fronts",
            "The Royal Army is fighting in Pales and in Zille at the same time. Divisions, fuel and shipping are split between two " +
            "theaters hundreds of kilometers apart, and every reinforcement sent to one front is denied to the other. \n\nThe strain " +
            "is felt at home as much as at the front, in the ministries, the factories and the families who have sons in both armies."),
        new("ZilleWar_Military_ZilleOccupation", "Military", "Medium", SitOccupation, "Military Administration in Zille",
            "Rizian troops hold the city of Zille and its harbor. Wehzek officials have fled or been interned, and army officers now " +
            "run the bread queues, the water supply and the police in a city that has not answered to Porte Drazon in a generation. " +
            "\n\nOrder holds, but it is held by soldiers, and the city's Wehzek residents are waiting to see what kind of masters they have."),
        new("ZilleWar_Order_ZilleRefugees", "Order", "Medium", SitRefugees, "Refugees from Zille",
            "Families fleeing the fighting in Zille, Rizian and Wehzek alike, crowd the roads into Mave Nortus, Dapalle and Jales. " +
            "Relief stations run by the army and the sanctuaries struggle to cope. \n\nOld resentments travel with the refugees, and " +
            "the Wehzek quarters of Brenas feel them first."),
        new("ZilleWar_Economy_ZilleReconstruction", "Economy", "Medium", SitReconstruction, "Reconstruction of Zille",
            "The harbor, the rail line to Nanta and the power stations that serve the mines were damaged in the fighting. Engineers " +
            "of the Royal Army and contractors from Brenas are clearing rubble and rebuilding piers. \n\nUntil the work is done, " +
            "Zille costs the treasury more than it returns. Once it is done, its gold will flow south again."),
        new("ZilleWar_Military_ZilleDefeat", "Military", "High", SitLost, "Defeat in Zille",
            "The campaign in Zille has failed. Wehzek forces hold the region more firmly than before, and the army has come home " +
            "with its losses and its questions. \n\nAbroad, the failure is read as the limit of the Rizian crown's reach. At home, " +
            "it is read as the King's."),
        new("ZilleWar_Military_ZilleDividedArmistice", "Military", "Medium", SitDivided, "Armistice Line in Zille",
            "The armistice line, watched by Alliance observers, runs where the armies stopped. Rizian troops hold Zille city and its harbor, " +
            "and Wehzek forces hold the ports and the hills. \n\nNeither government calls this peace, and both armies are still " +
            "dug in within sight of each other."),
        new("ZilleWar_Military_ZilleArmistice", "Military", "Medium", SitCeasefire, "Armistice Line in Zille",
            "The guns in Zille are silent under a ceasefire supervised by the Alliance of Nations. The line of contact runs through " +
            "the region, and families on either side of it cannot cross. \n\nNeither government calls this peace, and both armies " +
            "are still dug in within sight of each other."),
    };

    private static readonly Card[] Policies =
    {
        new("ZilleWar_Diplomacy_ZLLA_Repudiated", "Diplomacy", null, PolRepudiated, "Zille Agreement Repudiated",
            "By royal declaration, the Kingdom of Rizia no longer recognizes the Zille Agreement of 1927 or any extension of it. The " +
            "Crown regards Zille as Rizian soil under foreign occupation, and every Wehzek office in the region as illegitimate."),
    };

    /// <summary>Extra rows in vanilla's recompute-from-scratch conditional instructions (condition, instruction).</summary>
    private static readonly (string ci, string cond, string instr)[] Rows =
    {
        // vanilla already charges the Pales war (-3 growth, -2 living standards, -3 perception); never stack the Zille war on top
        ("Economy_General", "ZilleWar.War_Active == true && RiziaDLC.Pales_War == false", "RiziaDLC.Country_EconomicGrowth -= 2;"),
        ("Economy_General", "ZilleWar.TwoWars == true", "RiziaDLC.Country_EconomicGrowth -= 1;"),
        ("Economy_General", "ZilleWar.Mod_ZilleGold >= 1", "RiziaDLC.Country_EconomicGrowth += 1;"),
        ("LivingStandards_Default", "ZilleWar.War_Active == true && RiziaDLC.Pales_War == false", "RiziaDLC.Country_LivingStandards -= 1;"),
        ("Diplomacy_GlobalPerceptionWar", "ZilleWar.War_Active == true && RiziaDLC.Pales_War == false", "RiziaDLC.Country_GlobalPerception -= 2"),
        ("Diplomacy_GlobalPerceptionWar", "ZilleWar.TwoWars == true", "RiziaDLC.Country_GlobalPerception -= 1"),
        // War_Zille_Defeated is any end after the last phase without a liberation: a lost war only when Zille city is Wehzek
        // (with Zille city held it is the divided outcome, like the HUD line and the "Defeat in Zille" card)
        ("Diplomacy_GlobalPerceptionWar", "ZilleWar.War_Zille_Defeated == true && ZilleWar.ZilleCity_Held == false", "RiziaDLC.Country_GlobalPerception -= 1"),
        ("Diplomacy_GlobalPerceptionWar", "ZilleWar.War_Zille_Liberated == true && ZilleWar.Event_Zille_Restraint == true", "RiziaDLC.Country_GlobalPerception += 1"),
        // the consulate files published during the war: the world learned who planned the Friendship Day bombing
        // (vanilla recomputes perception every step, so the effect is a row, not a script change)
        ("Diplomacy_GlobalPerceptionMisc", "ZilleWar.War_Declared == true && RiziaDLC.Zille_Publicized_LespianInvolvement == true", "RiziaDLC.Country_GlobalPerception += 1"),
        // a liberation weighs more than vanilla's Pales victory (+4): a Merkopan army beaten and a province taken back
        ("PowerProjection_War_Outcome", "ZilleWar.War_Zille_Liberated == true", "RiziaDLC.Country_Military_PowerProjection += 5;"),
        ("PowerProjection_War_Outcome", "ZilleWar.War_Zille_Liberated == true && ZilleWar.Treaty_Signed == true && ZilleWar.Treaty_T_Army == true", "RiziaDLC.Country_Military_PowerProjection += 2;"),
        ("PowerProjection_War_Outcome", "ZilleWar.War_Zille_Liberated == true && ZilleWar.Treaty_Signed == true && ZilleWar.Treaty_T_Demil == true", "RiziaDLC.Country_Military_PowerProjection += 1;"),
        ("PowerProjection_War_Outcome", "ZilleWar.War_Zille_Defeated == true && ZilleWar.ZilleCity_Held == false", "RiziaDLC.Country_Military_PowerProjection -= 4;"),
        // turning down Smolak's ceasefire shows nerve, unless the war is then lost
        ("PowerProjection_War_Outcome", "ZilleWar.Smolak_RefusedCeasefire == true", "RiziaDLC.Country_Military_PowerProjection += 1;"),
        ("PowerProjection_War_Outcome", "ZilleWar.Smolak_RefusedCeasefire == true && ZilleWar.War_Zille_Defeated == true && ZilleWar.ZilleCity_Held == false", "RiziaDLC.Country_Military_PowerProjection -= 1;"),
        // the war stopped with Zille city Rizian, by a ceasefire or an armistice after the last phase, and nothing signed yet
        ("PowerProjection_War_Outcome", "(ZilleWar.War_Zille_Ceasefire == true || ZilleWar.War_Zille_Defeated == true) && ZilleWar.ZilleCity_Held == true && ZilleWar.War_Zille_Liberated == false && ZilleWar.Treaty_Signed == false", "RiziaDLC.Country_Military_PowerProjection += 1;"),
        // House Sazon (Brenas lost Zille in 1927) never forgives a signature that gives Zille, or part of it, away: Wehzek Zille
        // recognized (the Terms of Wraclawitz, or Smolak's Turn 9 call), or the line made a border (Mave Nortus, or the call)
        ("HouseOpinions_DialoguesTwelve", "ZilleWar.Late_RecognizedWehzekZille == true", "RiziaDLC.Country_HouseSazon -= 3;"),
        ("HouseOpinions_DialoguesTwelve", "ZilleWar.Late_LinePermanent == true", "RiziaDLC.Country_HouseSazon -= 2;"),
    };

    private const string WehlenGate = "ZilleWar.War_Declared == false";

    private static readonly HashSet<string> RowTargets = new(Rows.Select(r => r.ci), StringComparer.Ordinal);

    public static void Register()
    {
        foreach (string v in External.Concat(External.Select(Turns)).Concat(Computed)
                     .Concat(new[] { StraitClosed, SitOccupation, SitRefugees, SitReconstruction, PolRepudiated, VarLastTurn, SitLost, SitDivided, SitCeasefire }))
        {
            Variables.Register(v);
        }

        foreach (string v in External.Concat(Computed))
        {
            Vanilla.Expose(v, isBool: false);
        }

        foreach (string v in new[] { StraitClosed, SitOccupation, SitRefugees, SitReconstruction, PolRepudiated, SitLost, SitDivided, SitCeasefire, CityEvents.Flag("Zille_Restraint") })
        {
            Vanilla.Expose(v);
        }

        Vanilla.Expose(WarFlow.VarCeasefire);

        foreach (Line l in Lines)
        {
            LuaLint.RecordExpresso($"HUD line {l.Name}", l.Condition);
        }

        foreach ((string ci, string cond, _) in Rows)
        {
            LuaLint.RecordExpresso($"Row {ci}", cond);
        }
    }

    private static string Turns(string v) => v + "_Turns";

    public static void OnStep()
    {
        EnsureEntities();
        CountDown();
        Compute();
    }

    // ------------------------------------------------------------------ values

    private static void CountDown()
    {
        int turn = WorldSystems.Turn;
        int last = Variables.GetInt(VarLastTurn);
        if (turn <= last)
        {
            return;
        }

        Variables.Set(VarLastTurn, turn);
        if (last == 0)
        {
            return;     // first step we see: nothing to count yet
        }

        foreach (string v in External)
        {
            int left = Variables.GetInt(Turns(v));
            if (left <= 0)
            {
                continue;
            }

            Variables.Set(Turns(v), left - 1);
            if (left - 1 == 0)
            {
                Variables.Set(v, 0);
            }
        }
    }

    private static void Compute()
    {
        foreach (string v in External.Concat(Computed))
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = 0 end");
        }

        bool declared = WorldSystems.Declared;
        bool active = WorldSystems.Active;
        bool two = WorldSystems.TwoWars;
        bool liberated = WorldSystems.Liberated;
        bool defeated = WorldSystems.Defeated;
        bool ceasefire = WorldSystems.Ceasefire;
        bool cityHeld = WorldSystems.ZilleCityHeld;
        int w = WorldSystems.WarTurn;
        int since = WorldSystems.TurnsSinceEnd;
        bool strong = Variables.GetBool("RiziaDLC.Situation_Diplomacy_PowerProjection_Strong") ||
                      Variables.GetBool("RiziaDLC.Situation_Diplomacy_PowerProjection_Overwhelming");
        bool straitClosed = WorldSystems.StraitClosedNow;

        int occupation = (active || ceasefire) && cityHeld ? -1 : 0;
        int refugees = (active && w >= 2) || (defeated && since is >= 0 and <= 1) ? -1 : 0;
        int reconstruction = liberated && since is >= 0 and <= 2 ? -2 : 0;
        int shipping = active && !straitClosed ? (two ? -2 : -1) : 0;
        bool suzerain = declared && WorldSystems.RumburgFavors >= 3 && (active || since is >= 0 and <= 2);

        Set(WarExpenditures, active ? (strong ? -3 : -2) : 0);
        // the Lespian escort fee is paid only while the war lasts and the Lespian ships are in the strait (they leave when the
        // consulate files are published or the King sends them away)
        if ((!active || Variables.GetBool(SupplyWar.VarLespiaWithdrawn)) && Variables.GetInt(LespiaEscort) != 0)
        {
            Set(LespiaEscort, 0);
        }

        Set(Occupation, occupation);
        Set(Reconstruction, reconstruction);
        Set(ZilleGold, liberated && since >= 1 ? (since <= 2 ? 1 : 2) : 0);
        // the piers of Nanta, if they were blown during the war, keep the port idle a while longer
        int portFrom = Variables.GetBool(CityEvents.Flag("Nanta_PortSabotaged")) ? 3 : 1;
        Set(ZillePort, (liberated || (ceasefire && cityHeld)) && since >= portFrom ? 1 : 0);
        Set(ZilleEnergy, liberated && since >= 3 ? 1 : 0);
        Set(Refugees, refugees);
        Set(Shipping, shipping);
        Set(WarBonds, active && w is >= 2 and <= 3 && Variables.GetBool(EarlyT5.VarWarBonds) ? 1 : 0);
        Set(WarFuel, active ? -1 : 0);
        Set(Fervor, active && !two && w <= 2 ? 1 : 0);
        Set(Weariness, active && !two && w >= 3 ? (w >= 4 ? -2 : -1) : 0);
        Set(TwoFronts, two ? -1 : 0);
        // "from the Defeat in Zille": a lost war only, like its Overview card (an armistice with Zille city held is the divided outcome)
        Set(Humiliation, defeated && !cityHeld && since >= 0 ? (since <= 1 ? -2 : since <= 3 ? -1 : 0) : 0);
        Set(Victory, liberated && since is >= 0 and <= 1 ? 1 : 0);
        Set(Tutelage, suzerain ? -1 : 0);
        Set(LeaseShame, Variables.GetBool(SupplyWar.VarLespiaLease) ? -1 : 0);
        // decrees may have changed some of these (WarDecrees.Adjust): the Overview cards follow the adjusted values
        occupation = Variables.GetInt(Occupation);
        refugees = Variables.GetInt(Refugees);
        reconstruction = Variables.GetInt(Reconstruction);

        Variables.Set(SitOccupation, occupation < 0);
        Variables.Set(SitRefugees, refugees < 0);
        Variables.Set(SitReconstruction, reconstruction < 0);
        // once Rizia has signed away the claim (recognized) or accepted the line as a border, the declaration no longer stands and
        // there is no armistice line left; an accord keeps the claim and the armistice line ("terms" after a lost war keeps the claim too)
        bool settled = Variables.GetBool(ZilleTreaty.VarSigned) && Variables.GetString(ZilleTreaty.VarKind) is "recognized" or "border";
        Variables.Set(PolRepudiated, declared && !liberated && !settled);
        Variables.Set(SitCeasefire, ceasefire && !settled);
        Variables.Set(SitLost, declared && defeated && !cityHeld);
        Variables.Set(SitDivided, declared && defeated && cityHeld && !settled);
        UpdateCardTexts(declared && defeated, cityHeld, ceasefire && !settled);

        if (declared)
        {
            // Unused vanilla situations, now owned by the war: "Maritime Trade in Turmoil" and "Rumburg Suzerain".
            Variables.Set("RiziaDLC.Situation_Diplomacy_Shipping_Disruptions", shipping < 0);
            Variables.Set("RiziaDLC.Situation_Diplomacy_Rumburg_Suzerain", suzerain);
            // "ATO Influence": Lespian warships in the strait (and perhaps at Caleqabiz) are the ATO's foot in the door. It stays.
            if (WorldSystems.LespiaHelps)
            {
                Variables.Set("RiziaDLC.Situation_Diplomacy_GlobalInfluence_ATO", true);
            }

            // "CSP Influence": the King bargained with Hegel to keep Valgsland out of the war (SupplyWar's Valgsland calls).
            string cable = Variables.GetString("ZilleWar.Valgsland_CableAnswer");
            if (cable is "guarantee" or "mitz" || Variables.GetString("ZilleWar.Valgsland_CallAnswer") == "yielded")
            {
                Variables.Set("RiziaDLC.Situation_Diplomacy_GlobalInfluence_CSP", true);
            }
        }
    }

    // ------------------------------------------------------------------ card texts that follow the map

    private const string DividedCard = "ZilleWar_Military_ZilleDividedArmistice";
    private const string LostCard = "ZilleWar_Military_ZilleDefeat";
    private const string CeasefireCard = "ZilleWar_Military_ZilleArmistice";
    private static readonly Dictionary<string, string> CardTexts = new(StringComparer.Ordinal);

    /// <summary>The Rizian border towns Wehlen still holds after the armistice, in words, or "" when none.</summary>
    private static string OccupiedTowns() =>
        Front.Join(WarFlags.RizianTowns.Where(t => Variables.GetBool(WarFlags.Occupied(t))));

    /// <summary>The armistice and defeat cards name the cities each side holds once the war is over (the static text is the
    /// default before then).</summary>
    private static void UpdateCardTexts(bool ended, bool cityHeld, bool ceasefire)
    {
        string towns = OccupiedTowns();
        string townsLine = towns.Length > 0 ? $" On the Rizian side of the old border, Wehzek troops still hold {towns}." : string.Empty;
        if (ceasefire)
        {
            // the line of contact runs through the region only when Rizia holds part of it
            SetCardText(CeasefireCard, Front.Held.Count > 0
                ? "The guns in Zille are silent under a ceasefire supervised by the Alliance of Nations. The line of contact runs through " +
                  $"the region: Rizian troops hold {Front.HeldWords}, and Wehzek forces hold {Front.WehzekWords}.{townsLine} \n\nNeither " +
                  "government calls this peace, and both armies are still dug in within sight of each other."
                : "The guns in Zille are silent under a ceasefire supervised by the Alliance of Nations. The line of contact runs close to " +
                  $"the old border, and Wehlen holds every city in Zille.{townsLine} \n\nNeither government calls this peace, and both " +
                  "armies are still dug in within sight of each other.");
        }

        if (!ended)
        {
            return;
        }

        if (cityHeld)
        {
            SetCardText(DividedCard,
                "The armistice line, watched by Alliance observers, runs where the armies stopped. Rizian troops hold " +
                $"{Front.HeldWords}, and Wehzek forces hold {Front.WehzekWords}.{townsLine} \n\nNeither government calls this peace, and " +
                "both armies are still dug in within sight of each other.");
        }
        else
        {
            List<string> held = Front.Held;
            SetCardText(LostCard, held.Count > 0
                ? "The campaign in Zille has failed. Wehzek forces hold Zille city, and Rizian troops hold only " +
                  $"{Front.HeldWords} behind an armistice line watched by Alliance observers.{townsLine} \n\nAbroad, the failure is read " +
                  "as the limit of the Rizian crown's reach. At home, it is read as the King's."
                : "The campaign in Zille has failed. Wehzek forces hold the region more firmly than before, and the army has come home " +
                  $"with its losses and its questions.{townsLine} \n\nAbroad, the failure is read as the limit of the Rizian crown's reach. " +
                  "At home, it is read as the King's.");
        }
    }

    private static void SetCardText(string name, string text)
    {
        if (CardTexts.TryGetValue(name, out string last) && last == text)
        {
            return;
        }

        bool found = false;
        foreach (var list in new[] { EntityDataManager.situationsData, EntityDataManager.SituationsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (SituationData d in list)
            {
                if (d?.NameInDatabase == name && d.SituationProperties != null)
                {
                    d.SituationProperties.Description = text;
                    found = true;
                }
            }
        }

        if (found)
        {
            CardTexts[name] = text;
        }
    }

    private static void Set(string v, int value)
    {
        value = WarDecrees.Adjust(v, value);
        if (Variables.GetInt(v) != value)
        {
            Variables.Set(v, value);
        }
    }

    // ------------------------------------------------------------------ entities (added once per session, re-checked each step)

    private static bool _added;

    /// <summary>The game rebuilt its entity lists (a save was loaded): add ours again on the next step.</summary>
    internal static void ResetAdded()
    {
        _added = false;
        CardTexts.Clear();
    }

    private static void EnsureEntities()
    {
        if (EntityDataManager.HUDStatData == null)
        {
            return;
        }

        if (!_added)
        {
            _added = AddLines() && AddCards();
            if (_added)
            {
                LogCard("ZilleWar_Diplomacy_Wehlen_War");
            }
        }

        PatchWehlenLines();
        AddRows();
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
            return false;   // data not loaded yet; try again next step
        }

        foreach (Line l in Lines)
        {
            var data = new HUDPeriodicStatModifierData
            {
                Id = $"0x5A11{StableHash.Of(l.Name):X8}",
                NameInDatabase = l.Name,
                Path = "Rizia/HUD Panel/Modifiers/ZilleWar",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template?.AppBundleProperties,
                HUDPeriodicStatModifierProperties = new HUDPeriodicStatModifierProperties
                {
                    Variable = l.Variable,
                    Condition = l.Condition,
                    Description = l.Description,
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

        Core.Log.Msg($"World: {Lines.Length} HUD lines registered.");
        return true;
    }

    /// <summary>Diagnostic: is the card in both situation lists, and what does its variable read?</summary>
    private static void LogCard(string name)
    {
        bool raw = false, filtered = false;
        string variable = null;
        foreach (SituationData d in EntityDataManager.situationsData ?? new Il2CppSystem.Collections.Generic.List<SituationData>())
        {
            if (d?.NameInDatabase == name)
            {
                raw = true;
                variable = d.SituationProperties?.IsEnabledVariable;
            }
        }

        foreach (SituationData d in EntityDataManager.SituationsData ?? new Il2CppSystem.Collections.Generic.List<SituationData>())
        {
            filtered |= d?.NameInDatabase == name;
        }

        string value = variable == null ? "?" : Il2CppPixelCrushers.DialogueSystem.DialogueLua.GetVariable(variable).asString;
        Core.Log.Msg($"World: card '{name}' in situationsData={raw}, SituationsData={filtered}, {variable}={value}.");
    }

    private static bool AddCards()
    {
        SituationData sTemplate = null;
        foreach (SituationData s in EntityDataManager.SituationsData)
        {
            if (s?.NameInDatabase == "Diplomacy_Pales_War")
            {
                sTemplate = s;
                break;
            }
        }

        if (sTemplate?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Card c in Situations)
        {
            var data = new SituationData
            {
                Id = $"0x5A11{StableHash.Of(c.Name):X8}",
                NameInDatabase = c.Name,
                Path = $"Rizia/Situations/{c.Category}/Non Default Situations",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = sTemplate?.AppBundleProperties,
                TagsProperties = sTemplate?.TagsProperties,
                SituationProperties = new SituationProperties
                {
                    Title = c.Title,
                    Description = c.Description,
                    IsEnabledVariable = c.Variable,
                    IsNotificationActive = false,
                    SituationSeverityString = c.Severity,
                },
                OverviewCategoryProperties = new OverviewCategoryProperties { OverviewCategoryString = c.Category },
            };
            TrySet(() => data.SituationSeverity = Enum.Parse<SituationData.SituationSeverityType>(c.Severity));
            TrySet(() => data.OverviewCategory = Enum.Parse<OverviewPanel.OverviewCategoryType>(c.Category));
            ZilleOperation.AddByName(EntityDataManager.situationsData, data, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.SituationsData, data, x => x.NameInDatabase);
        }

        PolicyData pTemplate = null;
        foreach (PolicyData p in EntityDataManager.PoliciesData)
        {
            if (p?.NameInDatabase == "Diplomacy_ZLLA_Active")
            {
                pTemplate = p;
                break;
            }
        }

        if (pTemplate?.AppBundleProperties == null)
        {
            return false;
        }

        foreach (Card c in Policies)
        {
            var data = new PolicyData
            {
                Id = $"0x5A11{StableHash.Of(c.Name):X8}",
                NameInDatabase = c.Name,
                Path = "Rizia/Policies/Diplomacy",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = pTemplate?.AppBundleProperties,
                TagsProperties = pTemplate?.TagsProperties,
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
            Core.Log.Warning($"World: {ex.Message}");
        }
    }

    private static void PatchWehlenLines()
    {
        foreach (var list in new[] { EntityDataManager.hudPeriodicStatModifierData, EntityDataManager.HUDPeriodicStatModifierData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (HUDPeriodicStatModifierData m in list)
            {
                HUDPeriodicStatModifierProperties p = m?.HUDPeriodicStatModifierProperties;
                if (p == null || !WehlenLines.Contains(m.NameInDatabase) || (p.Condition ?? string.Empty).Contains(WehlenGate, StringComparison.Ordinal))
                {
                    continue;
                }

                p.Condition = string.IsNullOrWhiteSpace(p.Condition) ? WehlenGate : $"({p.Condition}) && {WehlenGate}";
            }
        }
    }

    private static void AddRows()
    {
        foreach (var list in new[] { EntityDataManager.conditionalInstructionData, EntityDataManager.ConditionalInstructionData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (ConditionalInstructionData c in list)
            {
                // one name read per entry (this runs every step over a few hundred entries, peace games included)
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
}
