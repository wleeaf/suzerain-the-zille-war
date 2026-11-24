using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Vanilla royal decrees edited in every Rizia game, peace or war: costs and texts that did not match their scripts, dead or
/// overpowered decrees, energy trade at the world price, arms sales closed during the Pales war or under export controls,
/// House effects as recompute rows. Unlike the war-only edits in <see cref="VanillaDecrees"/>, these are never restored.
/// <see cref="VanillaDecrees"/> applies both registries every step, field by field from the remembered vanilla values; in a war
/// game a war-only edit wins over an all-games edit of the same field.
/// </summary>
internal static class VanillaDecreesAll
{
    internal sealed class AEdit
    {
        public string Title, Description, Append, Cost, EnabledOr, EnabledReplace, EnactAppend;
        public Func<string> CostFn, EnactFn;
        public readonly List<string> EnabledAnd = new();
        public readonly List<(string message, string condition)> Errors = new();
        public readonly List<(string from, string to)> Replace = new();
        public readonly List<(string from, Func<string> to)> ReplaceFn = new();
        public readonly List<(string from, string to)> ApprovalSwaps = new();
    }

    internal sealed record Watcher(string Flag, string Seen, Action Effect);

    internal static readonly Dictionary<string, AEdit> Edits = new(StringComparer.Ordinal);
    internal static readonly Dictionary<string, (int approval, string condition)[]> Approvals = new(StringComparer.Ordinal);
    internal static readonly List<Watcher> Watchers = new();

    // flags read by Builder N's HUD lines: the decree was signed under this build, so its new per-turn line applies
    internal const string VarLandTaxNew = "ZilleWar.VDec_LandTax_New";
    internal const string VarLeviesNew = "ZilleWar.VDec_Levies_New";
    internal const string VarTaxCutsNew = "ZilleWar.VDec_TaxCuts_New";
    internal const string VarInit = "ZilleWar.VDec_AllInit";

    private const string High = "RiziaDLC.Situation_Economy_GlobalEnergyPrice_High";
    private const string Low = "RiziaDLC.Situation_Economy_GlobalEnergyPrice_Low";
    private const string ExportControls = "ZilleWar.Decree_ArmsExportControls";

    private static bool _registered;

    // ------------------------------------------------------------------ the edits

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        RegisterTrade();
        RegisterEconomy();
        RegisterState();

        Variables.Register(VarInit);
        Variables.Register(VarLandTaxNew);
        Variables.Register(VarLeviesNew);
        Variables.Register(VarTaxCutsNew);
        foreach (Watcher w in Watchers)
        {
            Variables.Register(w.Seen);
        }
    }

    // ------------------------------------------------------------------ decree news (every game, every step: data reloads reset it)

    private const string NurityNews = "RD_V_NurityStateReligion";
    private const string NurityVar = "RiziaDLCSupport.News_RD_V_NurityStateReligion";
    private const string ZpanaVar = "RiziaDLCSupport.News_RD_V_ZpanaHydroelectricDam";

    /// <summary>
    /// Vanilla news slips. The Voice's Nurity item was switched on by the Zpana dam's variable, so each decree printed the
    /// other's article: it gets its own variable (EnableNews writes true to the item's variable). The Herald's bomber purchase
    /// was written about support vehicles; the Voice's Mave Nortus oil field "began production" on the day construction began.
    /// </summary>
    private static void FixDecreeNews()
    {
        foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (NewsData n in list)
            {
                if (n?.NameInDatabase == NurityNews && n.NewsProperties != null && n.NewsProperties.IsEnabledVariable == ZpanaVar)
                {
                    n.NewsProperties.IsEnabledVariable = NurityVar;
                }
            }
        }

        RewriteNews("RD_RH_Buy_BomberPlanesOne",
            ("a fleet of cutting-edge military support vehicles", "a fleet of modern bomber aircraft"),
            ("aims to fortify", "strengthens"),
            ("logistics and supply chain capabilities", "air power"));
        RewriteNews("RD_V_BuildMaveNortusOilField",
            ("The Mave Nortus Oil Field has officially begun production, promising economic growth and energy security.",
             "Construction has begun on the Mave Nortus Oil Field, which the government promises will bring economic growth and energy security."));
    }

    /// <summary>Vanilla text with the given phrases replaced (a phrase not found is left alone).</summary>
    private static void RewriteNews(string name, params (string from, string to)[] edits)
    {
        string text = WorldText.OriginalText("news", name);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach ((string from, string to) in edits)
        {
            text = text.Replace(from, to, StringComparison.Ordinal);
        }

        WorldText.News(name, null, text);
    }

    /// <summary>The world energy price: +1 when High, -1 when Low, 0 at Average.</summary>
    private static int PriceDelta => Variables.GetBool(High) ? 1 : Variables.GetBool(Low) ? -1 : 0;

    private static string PriceLine(int d) => d > 0 ? "\nAt High Global Energy Prices" : d < 0 ? "\nAt Low Global Energy Prices" : string.Empty;

    private static void RegisterTrade()
    {
        // E1: Medium and Large energy purchases follow the world price (the Budget paid moves by 1)
        foreach ((string key, int auth, int energy, int b, int e) in new[]
                 {
                     ("BuyMediumEnergyAmountOne", 1, 3, 5, 4), ("BuyMediumEnergyAmountTwo", 2, 3, 6, 5),
                     ("BuyMediumEnergyAmountThree", 2, 3, 6, 5), ("BuyMediumEnergyAmountFour", 2, 3, 6, 5),
                     ("BuyLargeEnergyAmountOne", 2, 5, 7, 6), ("BuyLargeEnergyAmountTwo", 3, 5, 9, 8),
                     ("BuyLargeEnergyAmountThree", 3, 5, 9, 8),
                 })
        {
            string name = $"Economy_Resources_{key}";
            CostFn(name, () =>
            {
                int d = PriceDelta;
                return $"-{auth} Authority\n+{energy} Energy\n-{b + d} Budget{PriceLine(d)}";
            });
            EnactFn(name, () => PriceDelta switch
            {
                > 0 => "RiziaDLC.Resources_Budget -= 1;",
                < 0 => "RiziaDLC.Resources_Budget += 1;",
                _ => string.Empty,
            });
            ReplaceErrorFn(name, $"RiziaDLC.Resources_Budget <= {e}", () => $"RiziaDLC.Resources_Budget <= {e + PriceDelta}");
        }

        // E1 and E2: sales follow the world price (the Budget earned moves by 1); Large sales cost 1 less Authority
        foreach ((string key, int auth, int energy, int b) in new[]
                 {
                     ("SellMediumEnergyAmountOne", 0, 3, 2), ("SellMediumEnergyAmountTwo", 0, 4, 2), ("SellMediumEnergyAmountThree", 1, 4, 2),
                     ("SellMediumEnergyAmountFour", 2, 4, 2), ("SellLargeEnergyAmountOne", 1, 5, 3), ("SellLargeEnergyAmountTwo", 2, 6, 3),
                     ("SellLargeEnergyAmountThree", 2, 6, 3),
                 })
        {
            string name = $"Economy_Resources_{key}";
            string authLine = auth > 0 ? $"-{auth} Authority\n" : string.Empty;
            CostFn(name, () =>
            {
                int d = PriceDelta;
                return $"{authLine}-{energy} Energy\n+{b + d} Budget{PriceLine(d)}";
            });
            EnactFn(name, () => PriceDelta switch
            {
                > 0 => "RiziaDLC.Resources_Budget += 1;",
                < 0 => "RiziaDLC.Resources_Budget -= 1;",
                _ => string.Empty,
            });
        }

        const string largeSale = "A contract of this size is negotiated by the Crown itself, and it pays best when the world is short of fuel " +
                                 "and buyers compete for every cargo. What is sold, however, is gone; if the winter is hard or the fields " +
                                 "fall short, the Kingdom will have to buy back abroad, at a dearer price, what it has just let go.";
        EnactAppend("Economy_Resources_SellLargeEnergyAmountOne", "RiziaDLC.Resources_Authority += 1;");
        ReplaceError("Economy_Resources_SellLargeEnergyAmountOne", "RiziaDLC.Resources_Authority <= 1", "RiziaDLC.Resources_Authority <= 0");
        EnactAppend("Economy_Resources_SellLargeEnergyAmountTwo", "RiziaDLC.Resources_Authority += 1;");
        ReplaceError("Economy_Resources_SellLargeEnergyAmountTwo", "RiziaDLC.Resources_Authority <= 2", "RiziaDLC.Resources_Authority <= 1");
        EnactAppend("Economy_Resources_SellLargeEnergyAmountThree", "RiziaDLC.Resources_Authority += 1;");
        ReplaceError("Economy_Resources_SellLargeEnergyAmountThree", "RiziaDLC.Resources_Authority <= 2", "RiziaDLC.Resources_Authority <= 1");
        ReplaceError("Economy_Resources_SellLargeEnergyAmountThree", "RiziaDLC.Resources_Energy <= 6", "RiziaDLC.Resources_Energy <= 5");
        foreach (string n in new[] { "One", "Two", "Three" })
        {
            Append($"Economy_Resources_SellLargeEnergyAmount{n}", largeSale);
        }

        // E3: the energy councillors weigh a sale or a large purchase against the stock
        foreach (string c in new[] { "AxelReinhart", "ElenaWerner", "VinaToras" })
        {
            ApprovalConditions($"DecreeApproval_{c}_SellMediumEnergyAmount",
                (1, "RiziaDLC.Resources_Energy >= 7"),
                (0, "RiziaDLC.Resources_Energy >= 4 && RiziaDLC.Resources_Energy <= 6"),
                (-1, "RiziaDLC.Resources_Energy <= 3"));
            ApprovalConditions($"DecreeApproval_{c}_SellLargeEnergyAmount",
                (1, "RiziaDLC.Resources_Energy >= 10"),
                (0, "RiziaDLC.Resources_Energy >= 6 && RiziaDLC.Resources_Energy <= 9"),
                (-1, "RiziaDLC.Resources_Energy <= 5"));
        }

        foreach (string c in new[] { "AxelReinhart", "ElenaWerner", "RustyMontoro", "VinaToras" })
        {
            ApprovalConditions($"DecreeApproval_{c}_BuyLargeEnergy",
                (1, "RiziaDLC.Resources_Energy <= 2"),
                (0, "RiziaDLC.Resources_Energy >= 3 && RiziaDLC.Resources_Energy <= 6"),
                (-1, "RiziaDLC.Resources_Energy >= 7"));
        }

        // E4: no arms sales abroad during the Pales war or under the Royal Arms Export Controls
        foreach (string s in VanillaDecrees.SalesKeys)
        {
            EnabledAnd($"Order_Military_{s}", $"RiziaDLC.Pales_War == false && {ExportControls} == false");
        }

        // E5: the purchases' cost lines show the cooldown their scripts already lower
        foreach ((string kind, string cd) in new[] { ("Bomber", "Airstrike"), ("Fighter", "Air Support") })
        {
            Cost($"Order_Military_Buy{kind}PlanesOne", $"-2 Authority\n-3 Budget\n+25 {kind} Planes\n-1 {cd} Cooldown");
            Cost($"Order_Military_Buy{kind}PlanesTwo", $"-2 Authority\n-4 Budget\n+25 {kind} Planes\n-1 {cd} Cooldown");
            Cost($"Order_Military_Buy{kind}PlanesThree", $"-3 Authority\n-5 Budget\n+25 {kind} Planes\n-1 {cd} Cooldown");
        }

        For("Order_Military_BuyFighterPlanesOne").Title = "Buy First Batch of Military Fighter Planes from International Market";

        // E6: sale bugs and prices
        EnactAppend("Order_Military_SellMilitaryEquipmentThree", "RiziaDLC.Resources_Military_Equipment -= 250;");
        ReplaceError("Order_Military_SellBomberPlanesTwo", "RiziaDLC.Resources_Military_BomberPlanes <= 24",
            "RiziaDLC.Resources_Military_BomberPlanes <= 49");
        EnactAppend("Order_Military_SellFighterPlanesThree", "RiziaDLC.Resources_Budget += 1;");
        Cost("Order_Military_SellFighterPlanesThree", "-1 Authority\n+2 Budget\n-25 Fighter Planes");
        EnactAppend("Order_Military_SellShipsOne", "RiziaDLC.Resources_Budget -= 1;");
        Cost("Order_Military_SellShipsOne", "-2 Authority\n+3 Budget\n-2 Battle Ships");
        EnactAppend("Order_Military_SellShipsTwo", "RiziaDLC.Resources_Budget -= 1;");
        Cost("Order_Military_SellShipsTwo", "-3 Authority\n+3 Budget\n-2 Battle Ships");
        foreach (string n in new[] { "One", "Two" })
        {
            Append($"Order_Military_SellShips{n}",
                "Second-hand warships fetch less than new ones, and a coast without ships is a coast others will test.");
        }

        foreach (string kind in new[] { "Bomber", "Fighter" })
        {
            foreach (string n in new[] { "One", "Two", "Three" })
            {
                Append($"Order_Military_Sell{kind}Planes{n}",
                    "Every airframe sold abroad is one the air force will not have when it is needed, and the arms trade is watched " +
                    "closely in Konstantium: buyers are seldom particular about where their purchases end up.");
            }
        }

        // E7: the support vehicle approvals read the trucks, not the tanks; Taddeus judges a bomber sale as one
        foreach (string c in new[] { "AxelReinhart", "ElenaWerner", "VinaToras" })
        {
            ApprovalConditions($"DecreeApproval_{c}_SellSupportVehicles",
                (1, "RiziaDLC.Resources_Military_Trucks >= 200"),
                (0, "RiziaDLC.Resources_Military_Trucks >= 101 && RiziaDLC.Resources_Military_Trucks <= 199"),
                (-1, "RiziaDLC.Resources_Military_Trucks <= 100"));
        }

        For("Order_Military_SellBomberPlanesOne").ApprovalSwaps.Add(("DecreeApproval_TaddeusAzaro_SellSupportVehicles",
            "DecreeApproval_TaddeusAzaro_SellBomberPlanes"));

        // E8: the Arms Exporter's reputation costs perception, unless the export controls are in force
        WarDecrees.AddRow("Diplomacy_GlobalPerceptionMisc",
            $"RiziaDLC.Situation_Economy_ArmsExporter == true && {ExportControls} == false",
            "RiziaDLC.Country_GlobalPerception -= 1;");
    }

    private static void RegisterEconomy()
    {
        // E9: welfare privatization costs more Authority and cannot follow higher welfare funding; both privatizations cost LS
        const string privatize = "Economy_Interventionism_AllowWelfarePrivatization";
        EnactAppend(privatize, "RiziaDLC.Resources_Authority -= 1;");
        ReplaceError(privatize, "RiziaDLC.Resources_Authority <= 2", "RiziaDLC.Resources_Authority <= 3");
        Cost(privatize, "-4 Authority\n+3 Budget\n+1 Energy Per Turn\n+1 Administrative Balance");
        EnabledAnd(privatize, "RiziaDLC.Policy_RD_Welfare_IncreasedHealthEducationFunds == false");
        WarDecrees.AddRow("LivingStandards_WelfareDecreesTwo",
            "RiziaDLC.Policy_RD_Economy_AllowWelfarePrivatization == true && RiziaDLC.Policy_RD_Economy_PrivatizeCrownLands == true",
            "RiziaDLC.Country_LivingStandards -= 1;");

        // E10: business tax credits give the growth they promise
        WarDecrees.AddRow("Economy_RoyalDecreesOne",
            "RiziaDLC.Policy_RD_Economy_BusinessTaxCredits == true && RiziaDLCSetup.CurrentTurn >= 1",
            "RiziaDLC.Country_EconomicGrowth += 1;");

        // E11: repealing them gives back the industry's energy
        const string repealCredits = "Economy_Interventionism_RemoveBusinessTaxCredits";
        EnactAppend(repealCredits, "RiziaDLC.Energy_Modifier_IndustrialProduction += 1;");
        Cost(repealCredits, "-2 Authority\n+2 Budget Per Turn\n+1 Energy Per Turn");

        // E12: the football decree opens on Turns 2 to 4 (its vanilla flag is never set)
        const string football = "Economy_Interventionism_InvestInFootballInfrastructure";
        EnabledOr(football, "RiziaDLCSetup.CurrentTurn >= 2 && RiziaDLCSetup.CurrentTurn <= 4");
        EnactAppend(football, "RiziaDLC.Resources_Budget -= 2;");
        ReplaceError(football, "RiziaDLC.Resources_Budget <= 0", "RiziaDLC.Resources_Budget <= 2");
        Cost(football, "-1 Authority\n-3 Budget");

        // E13: removing the income tax removes the whole Income Tax line
        const string incomeAfter = "RiziaDLC.Resources_BudgetModifiersTotal - RiziaDLC.Budget_Modifier_IncomeTax < 0";
        foreach ((string d, string from) in new[]
                 {
                     ("Economy_Taxes_RemoveBaseIncomeTax", "RiziaDLC.Resources_BudgetModifiersTotal <= 0"),
                     ("Economy_Taxes_RemoveIncreasedIncomeTax", "RiziaDLC.Resources_BudgetModifiersTotal <= 1"),
                 })
        {
            CostFn(d, () =>
            {
                int n = Variables.GetInt("RiziaDLC.Budget_Modifier_IncomeTax");
                return n >= 1 ? $"-3 Authority\n-{n} Budget Per Turn" : "-3 Authority";
            });
            ReplaceError(d, from, incomeAfter);
        }

        // E14: removing the war tax removes its +2 per turn
        Cost("Economy_Taxes_RemoveWarProfitsTax", "-2 Authority\n-2 Budget Per Turn");
        ReplaceError("Economy_Taxes_RemoveWarProfitsTax", "RiziaDLC.Resources_BudgetModifiersTotal <= 0",
            "RiziaDLC.Resources_BudgetModifiersTotal <= 1");

        // E15: the land tax leaves registers that bring in a little every year (HUD line V-1, Builder N)
        const string landTax = "Economy_Taxes_LandOwnersTax";
        Cost(landTax, "-4 Authority\n+3 Budget\n+1 Budget Per Turn");
        For(landTax).Description =
            "The enactment of this decree imposes a one-time, retroactive tax on landowners with territories exceeding 15 square " +
            "kilometers, aiming to bolster the state's finances and address wealth disparities. Beyond the first collection, the land " +
            "registers it creates will bring a modest yearly sum to the Treasury.\n\nThe move, while progressive in addressing " +
            "socio-economic imbalances, is likely to kindle significant discontent and opposition among the royal houses and the " +
            "aristocratic community.";
        Watch("RiziaDLC.Policy_RD_Economy_LandOwnersTax", "ZilleWar.VDec_All_LandTax_Seen", () => Variables.Set(VarLandTaxNew, true));
        ApprovalConditions("DecreeApproval_AxelReinhart_LandOwnersTax",
            (1, "RiziaDLC.Resources_Budget <= 3"),
            (0, "RiziaDLC.Country_HouseAzaro <= 5 || RiziaDLC.Country_HouseSazon <= 5 || RiziaDLC.Country_HouseToras <= 5"),
            (-1, "RiziaDLC.Country_HouseAzaro <= -10 || RiziaDLC.Country_HouseSazon <= -10 || RiziaDLC.Country_HouseToras <= -10"));

        // E16: higher welfare funding no longer leaves a hidden -1 on the Administrative Balance; not after privatization
        const string welfare = "Welfare_HealthEducation_IncreaseWelfareFunding";
        EnactAppend(welfare, "RiziaDLC.Budget_Modifier_AdministrativeBalanceSheet += 1;");
        Cost(welfare, "-2 Authority\n-4 Budget\n-3 Budget Per Turn");
        EnabledAnd(welfare, "RiziaDLC.Policy_RD_Economy_AllowWelfarePrivatization == false");

        // E17: the cheap free transport needs an efficient network; the expensive one rebuilds a neglected one
        For("Welfare_FreePublicTransportation").EnabledReplace = "RiziaDLC.Situation_Economy_TransportationSystem_Efficient == true";
        LuaLint.RecordExpresso("Vanilla decree Welfare_FreePublicTransportation IsEnabled (all games)",
            "RiziaDLC.Situation_Economy_TransportationSystem_Efficient == true");
        const string transportDear = "Welfare_FreePublicTransportationExpensive";
        For(transportDear).EnabledReplace = "RiziaDLC.Situation_Economy_TransportationSystem_Efficient == false";
        LuaLint.RecordExpresso($"Vanilla decree {transportDear} IsEnabled (all games)",
            "RiziaDLC.Situation_Economy_TransportationSystem_Efficient == false");
        Append(transportDear, "Years of neglect have left the Kingdom's trains and ferries in poor repair, and fare-free service will " +
                              "first have to be rebuilt at the Crown's expense.");

        // E18: fuel subsidies take the 2 Energy their text says
        EnactAppend("Welfare_FuelSubsidiesCheaper", "RiziaDLC.Resources_Energy -= 1;");

        // E19: universal basic income within reach of a rich treasury
        const string ubi = "Welfare_UniversalBasicIncome";
        EnactAppend(ubi, "RiziaDLC.Budget_Modifier_UniversalBasicIncome += 2;\nRiziaDLC.Resources_Budget -= 2;");
        Cost(ubi, "-6 Authority\n-2 Budget\n-6 Budget Per Turn");
        ReplaceError(ubi, "RiziaDLC.Resources_Budget <= 7 || RiziaDLC.Resources_BudgetModifiersTotal <= 7",
            "RiziaDLC.Resources_Budget <= 4 || RiziaDLC.Resources_BudgetModifiersTotal <= 5");

        // E20: workers' rights and the RNC's labor deregulation exclude each other
        EnabledAnd("Welfare_Citizenship_WorkersRightsAllIndustries", "ZilleWar.Decree_LaborDeregulation == false");
        // ...also at the Turn 4 House vote (c312), whose overrule options enact the safety rules by script: once the RNC's
        // deregulation is in force they are gone ([138], siding with the majority, stays)
        foreach (int e in new[] { 139, 140 })
        {
            Vanilla.Require("Rizia/Turn 4/A_HouseOfDelegates", e, "Variable[\"ZilleWar.Decree_LaborDeregulation\"] ~= true");
        }
    }

    private static void RegisterState()
    {
        // E21: the industrial park's equipment
        Cost("Economy_ConstructValenqirHeavyIndustrialPark",
            "-2 Authority\n-4 Budget\n~3 Turn Construction\n-2 Energy Per Turn On Completion\n+500 Military Equipment On Completion");

        // E22: House Sazon keeps its gain when the Sallabes park is completed
        WarDecrees.AddRow("HouseOpinions_DecreesFive", "RiziaDLC.Policy_RD_Economy_SallabesConsumerIndustry_Completed == true",
            "RiziaDLC.Country_HouseSazon += 2;");

        // E23: the gold mine's damage to the vineyards is charged to House Toras
        WarDecrees.AddRow("HouseOpinions_DecreesSeven",
            "RiziaDLC.Policy_RD_Economy_QalusGoldMine_Completed == true && RiziaDLC.Situation_Economy_WineIndustry_EnvironmentalDamage == true",
            "RiziaDLC.Country_HouseToras -= 2;");
        Append("Economy_Resources_BuildQalusGoldMine", "The vineyards of Valenqiris lie downstream of the workings, and House Toras " +
                                                       "will count every spoiled harvest against the Crown until the damage is repaired.");

        // E24: the completed fighter factory pleases House Sazon (both vanilla rows read the project)
        WarDecrees.AddRow("HouseOpinions_DecreesSeven", "RiziaDLC.Policy_RD_Order_FighterPlaneFactory_Completed == true",
            "RiziaDLC.Country_HouseSazon += 2;");

        // E25: the Pales forts take the 2 Authority their text says
        EnactAppend("Order_Military_StrengthenPalesBorderFortifications", "RiziaDLC.Resources_Authority -= 1;");

        // E26, E27: completion effects in the cost text
        const string hospital = "Welfare_HealthEducation_EstablishRomusTorasHospital";
        Cost(hospital, "-2 Authority\n-3 Budget\n~2 Turn Construction\n+250 Military Manpower On Completion\n" +
                       "+50 Military Manpower Per Turn On Completion");
        Append(hospital, "Its wards will be open first to the wounded of the army, and the families of Porte Drazon will judge the " +
                         "Crown by how quickly the rest of its beds are opened to them.");
        Cost("Welfare_HealthEducation_FoundRomusTorasUniversity",
            "-1 Authority\n-3 Budget\n~2 Turn Construction\n+250 Military Equipment\n+250 Military Equipment On Completion\n" +
            "-1 Energy Per Turn On Completion");

        // E28: more levies show what they give, and cost an upkeep (HUD line V-2, Builder N); in a war game the Houses pay in sons
        const string levies = "Order_ConscriptionLevy_IncreaseProvincialLevies";
        Cost(levies, "-5 Authority\n+3 House Troops\n+500 Military Manpower\n+500 Military Equipment\n+1 Authority Per Turn\n" +
                     "-1 Budget Per Turn");
        Watch("RiziaDLC.Policy_RD_Order_IncreaseProvincialLevies", "ZilleWar.VDec_All_Levies_Seen", () => Variables.Set(VarLeviesNew, true));
        WarDecrees.AddRow("HouseOpinions_DecreesThree",
            "RiziaDLC.Policy_RD_Order_IncreaseProvincialLevies == true && RiziaDLC.Policy_RD_Order_RemoveProvincialLevies == false && " +
            "ZilleWar.War_Declared == true",
            "RiziaDLC.Country_HouseSazon -= 1;\nRiziaDLC.Country_HouseAzaro -= 1;");

        // E29: abolishing the levies: the per-turn Authority they gave is lost
        const string noLevies = "Order_ConscriptionLevy_RemoveProvincialLevies";
        Cost(noLevies, "-7 Authority\n-1 Budget\n-750 Military Equipment\n-500 Military Manpower\n-1 Authority Per Turn");
        Append(noLevies, "The dukes will not forgive the loss of their soldiers, and if the provincial police are abolished as well, the " +
                         "Crown alone will answer for order in every province, and command it.");

        // E30: abolishing the provincial police: text, and the journal flag vanilla sets on the levies' entry
        const string police = "Order_SecurityIntel_RemoveProvincialPolice";
        Cost(police, "-4 Authority\n-2 Budget\n-250 Military Manpower\n-1 Authority Per Turn");
        Watch("RiziaDLC.Policy_RD_Order_RemoveProvincialPolice", "ZilleWar.VDec_All_Police_Seen", () =>
        {
            Variables.Set("RiziaDLCSupport.Journal_RD_RemoveProvincialPolice", true);
            Variables.Set("RiziaDLCSupport.Journal_RD_RemoveProvincialLevies",
                Variables.GetBool("RiziaDLC.Policy_RD_Order_RemoveProvincialLevies"));
        });

        // E31: no anti-narcotics campaign without a drug trade
        EnabledAnd("Order_SecurityIntel_AntiNarcoticsOperations", "RiziaDLC.Country_DrugTrade >= 1");

        // E32: legalization pays enough to be a real choice
        const string legalize = "Order_SecurityIntel_LegalizeIllegalSubstances";
        EnactAppend(legalize, "RiziaDLC.Budget_Modifier_DrugTax = 2;");
        Cost(legalize, "-3 Authority\n+2 Budget Per Turn");
        Append(legalize, "The Church, the dukes of Toras and Azaro and the governments of Derdia and Morella will see the Crown profiting " +
                         "from vice, and the Dastnurist preachers of Sallabes will say so from the pulpit.");

        // E33: tax cuts for the religious orders are a standing cost (HUD line V-3, Builder N)
        const string taxCuts = "Religion_TaxCutsForReligiousOrgs";
        EnactAppend(taxCuts, "RiziaDLC.Resources_Budget += 2;");
        ReplaceError(taxCuts, "RiziaDLC.Resources_Budget <= 3", "RiziaDLC.Resources_Budget <= 1");
        Cost(taxCuts, "-1 Authority\n-2 Budget\n-1 Budget Per Turn");
        Watch("RiziaDLC.Policy_RD_Religion_TaxCutsForReligiousOrgs", "ZilleWar.VDec_All_TaxCuts_Seen", () => Variables.Set(VarTaxCutsNew, true));

        // E34: revoking them ends the yearly cost instead of refunding it (vanilla terms for cuts signed before this build)
        const string revoke = "Religion_RemoveTaxCutsForReligiousOrgs";
        CostFn(revoke, () => Variables.GetBool(VarTaxCutsNew) ? "-2 Authority\n+1 Budget Per Turn" : "-2 Authority\n+2 Budget");
        EnactFn(revoke, () => Variables.GetBool(VarTaxCutsNew) ? "RiziaDLC.Resources_Budget -= 2;" : string.Empty);

        // E35 (war only, V1): the war tax charged to House Azaro as a row, since script changes to the Houses are recomputed away
        WarDecrees.AddRow("HouseOpinions_DecreesFive",
            "RiziaDLC.Policy_RD_Economy_WarProfitsTax == true && ZilleWar.War_Declared == true",
            "RiziaDLC.Country_HouseAzaro -= 1;");
    }

    // ------------------------------------------------------------------ registry helpers

    internal static AEdit For(string name)
    {
        if (!Edits.TryGetValue(name, out AEdit e))
        {
            Edits[name] = e = new AEdit();
        }

        return e;
    }

    private static void Cost(string name, string cost) => For(name).Cost = cost;

    private static void CostFn(string name, Func<string> f) => For(name).CostFn = f;

    private static void Append(string name, string paragraph) => For(name).Append = paragraph;

    private static void EnactAppend(string name, string instructions) => For(name).EnactAppend = instructions;

    private static void EnactFn(string name, Func<string> f) => For(name).EnactFn = f;

    private static void EnabledOr(string name, string expresso)
    {
        For(name).EnabledOr = expresso;
        LuaLint.RecordExpresso($"Vanilla decree {name} IsEnabled (or, all games)", expresso);
    }

    private static void EnabledAnd(string name, string expresso)
    {
        For(name).EnabledAnd.Add(expresso);
        LuaLint.RecordExpresso($"Vanilla decree {name} IsEnabled (and, all games)", expresso);
    }

    private static void ReplaceError(string name, string from, string to)
    {
        For(name).Replace.Add((from, to));
        LuaLint.RecordExpresso($"Vanilla decree {name} error (replaced, all games)", to);
    }

    /// <summary>The error whose vanilla condition is <paramref name="from"/> gets the condition <paramref name="to"/> computes
    /// at every step (rewritten from the vanilla value, so it can move both ways).</summary>
    private static void ReplaceErrorFn(string name, string from, Func<string> to)
    {
        For(name).ReplaceFn.Add((from, to));
        LuaLint.RecordExpresso($"Vanilla decree {name} error (indexed, all games)", from);
    }

    private static void ApprovalConditions(string approval, params (int approval, string condition)[] conditions)
    {
        Approvals[approval] = conditions;
        foreach ((int _, string condition) in conditions)
        {
            LuaLint.RecordExpresso($"Vanilla approval {approval}", condition);
        }
    }

    private static void Watch(string flag, string seen, Action effect) => Watchers.Add(new Watcher(flag, seen, effect));

    // ------------------------------------------------------------------ every step

    /// <summary>Apply the approval edits to vanilla's DecreeApprovalData (in place, so every reference to the entity sees them).</summary>
    internal static void ApplyApprovals()
    {
        if (Approvals.Count == 0)
        {
            return;
        }

        foreach (var list in new[] { EntityDataManager.decreeApprovalData, EntityDataManager.DecreeApprovalData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecreeApprovalData a in list)
            {
                string name = a?.NameInDatabase;
                if (name == null || !Approvals.TryGetValue(name, out var want))
                {
                    continue;
                }

                var props = a.DecreeApprovalProperties;
                if (props == null)
                {
                    continue;
                }

                var conds = props.DecreeApprovalConditions;
                if (conds == null)
                {
                    conds = new Il2CppSystem.Collections.Generic.List<DecreeApprovalProperties.DecreeApprovalCondition>();
                    props.DecreeApprovalConditions = conds;
                }

                bool same = conds.Count == want.Length;
                for (int i = 0; same && i < want.Length; i++)
                {
                    same = conds[i] != null && conds[i].Approval == want[i].approval && conds[i].Condition == want[i].condition;
                }

                if (same)
                {
                    continue;
                }

                conds.Clear();
                foreach ((int approval, string condition) in want)
                {
                    conds.Add(new DecreeApprovalProperties.DecreeApprovalCondition { Approval = approval, Condition = condition });
                }
            }
        }
    }

    /// <summary>
    /// Record each watched vanilla flag; on a false-to-true change, run its effect once, in every game. The first step under
    /// this build (a new game, or a save made before it) only records, so a decree signed earlier is not taken for a new one.
    /// </summary>
    internal static void Watch()
    {
        FixDecreeNews();
        bool init = Variables.GetBool(VarInit);
        foreach (Watcher w in Watchers)
        {
            bool now = Variables.GetBool(w.Flag);
            if (Variables.GetBool(w.Seen) == now)
            {
                continue;
            }

            Variables.Set(w.Seen, now);
            if (now && init)
            {
                try
                {
                    w.Effect();
                }
                catch (Exception ex)
                {
                    Core.Log.Error($"VanillaDecreesAll effect of {w.Flag} failed: {ex}");
                }
            }
        }

        if (!init)
        {
            Variables.Set(VarInit, true);
        }
    }
}
