using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// New royal decrees for every Rizia game, peace or war (Turns 2 to 10): energy and arms trade, import duties, an energy
/// ordinance, gold bonds, party proposals of the RPP and RNC, a provincial tax inspectorate, the Montaklar resorts, shelters for
/// the refugees of Pales, the Rizian societies of Zille, the reformist assemblies, Su Omina in Iza and the minority faiths.
/// They use the war decrees' machinery (<see cref="Decree"/> records, saved ZilleWar.Decree_* flags, HUD lines, Overview cards
/// and recompute rows) and publish no news, so peace games keep vanilla's news. Every House effect is a row, because House
/// standing is rebuilt from the HouseOpinions_* rows at every story fragment.
/// Everything is built inside <see cref="RegisterRoyal"/>, never in static field initializers (the class is partial).
/// </summary>
internal static partial class WarDecrees
{
    private const string RForwardGas = "ForwardGasContracts";
    private const string RGraceArms = "GraceArmsCredit";
    private const string RValgsArms = "ValgsArmsPackage";
    private const string RImportDuties = "ImportDuties";
    private const string RRepealDuties = "RepealImportDuties";
    private const string RExportControls = "ArmsExportControls";
    private const string ROrdinance = "EnergyOrdinance";
    private const string RLiftOrdinance = "LiftEnergyOrdinance";
    private const string RGoldBonds = "GoldBonds";
    private const string ROldAgePensions = "OldAgePensions";
    private const string RDeregulation = "LaborDeregulation";
    private const string RInspectorate = "TaxInspectorate";
    private const string RResorts = "MontaklarResorts";
    private const string RShelters = "MermiShelters";
    private const string RSocieties = "ZilleSocieties";
    private const string RBanAssemblies = "BanAssemblies";
    private const string RSuOmina = "SuOminaCrackdown";
    private const string RRepealReligiousTax = "RepealReligiousTax";
    private const string RInterfaith = "InterfaithCouncil";

    private const string ResortsDoneVar = "ZilleWar.Decree_MontaklarResorts_Done";
    private const string GoldBondsLineVar = "ZilleWar.Dec_GoldBonds";

    private const string TradeResourcePath = "Rizia/Royal Decrees Panel/Trade Decrees/Resource";
    private const string TradeMilitaryPath = "Rizia/Royal Decrees Panel/Trade Decrees/Military";
    private const string ReligionPath = "Rizia/Royal Decrees Panel/Religion Decrees";

    private const string RTurn = "RiziaDLCSetup.CurrentTurn";
    private const string RStep = "RiziaDLCSetup.CurrentStep";
    private const string CivilianPassed = "RiziaDLC.RoyalDecrees_Civilian_Passed += 1;\n";

    private static (string, string) Sanctions => ("Transactions blocked by AN sanctions", "RiziaDLC.Situation_Diplomacy_ANSanctions_Heavy == true");

    private static string On(string key) => $"{EnactedVar(key)} == true";
    private static string Off(string key) => $"{EnactedVar(key)} == false";

    /// <summary>Turn 2 to step 1 of Turn 6: the party proposals count toward the Turn 6 party tally.</summary>
    private static string PartyWindow => $"{RTurn} >= 2 && ({RTurn} <= 5 || ({RTurn} == 6 && {RStep} <= 1))";

    /// <summary>The Interfaith Council sits while Sal's laws are not in force (it dissolves itself if they pass later).</summary>
    private static string InterfaithActive =>
        $"{On(RInterfaith)} && RiziaDLC.Policy_RD_Law_WruhecismAsSoleFaith == false && RiziaDLC.Policy_RD_Law_BanOnNonNurityFaiths == false";

    private static bool InterfaithSits =>
        Enacted(RInterfaith) && !Variables.GetBool("RiziaDLC.Policy_RD_Law_WruhecismAsSoleFaith") &&
        !Variables.GetBool("RiziaDLC.Policy_RD_Law_BanOnNonNurityFaiths");

    /// <summary>Vanilla's Foreign Goods Reliance line (Rizia_Budget_Modifier_ForeignGoodsReliance), read in C#.</summary>
    private static bool ForeignGoodsReliance =>
        WorldSystems.Turn >= 8 && Variables.GetBool("RiziaDLC.Situation_Economy_ProductionCapacity_Low") &&
        (Variables.GetBool("RiziaDLC.Situation_Economy_SelfSufficiency_Dependent") ||
         (!Variables.GetBool("RiziaDLC.Policy_RD_Economy_InvestInTimberIndustry") &&
          !Variables.GetBool("RiziaDLC.Policy_RD_Economy_InvestInPharmaceuticals")));

    /// <summary>A per-turn charge for <paramref name="turns"/> turns after the decree's turn.</summary>
    private static bool Installment(string key, int turns)
    {
        if (!Enacted(key))
        {
            return false;
        }

        int t = WorldSystems.Turn, t0 = EnactTurn(key);
        return t > t0 && t <= t0 + turns;
    }

    static partial void RegisterRoyal()
    {
        Variables.Register(ResortsDoneVar);
        Vanilla.Expose(ResortsDoneVar);
        AddRoyalDecrees();
        AddRoyalLines();
        AddRoyalCards();
        RegisterRoyalHouses();
    }

    static partial void RoyalStep() => Run("Montaklar resorts", ResortsCompletion);

    /// <summary>The resorts open two turns after the decree: tourism and a little pride on the coast.</summary>
    private static void ResortsCompletion()
    {
        if (!Enacted(RResorts) || Variables.GetBool(ResortsDoneVar) || WorldSystems.Turn < EnactTurn(RResorts) + 2)
        {
            return;
        }

        Variables.Set(ResortsDoneVar, true);
        PwAdd("RiziaDLC.Country_Tourism", 1);
        PwAdd("RiziaDLC.Country_PublicOpinion", 1);
        Core.Log.Msg($"WarDecrees: the Montaklar coast resorts are open (turn {WorldSystems.Turn}); tourism +1.");
    }

    // ------------------------------------------------------------------ the decrees

    private static void AddRoyalDecrees()
    {
        var list = new List<Decree>
        {
            // ---------------------------------------------------------------- trade
            new(RForwardGas, "Trade", TradeResourcePath, "Forward Gas Contracts with Lespian Utilities",
                "-2 Authority\n+3 Budget\n-1 Energy Per Turn",
                "The Crown's energy board signs a series of forward contracts with the utilities of Lespia, committing a fixed share of " +
                "Rizian gas to the markets of the west for the coming years at today's high prices. The buyers pay a large part of the " +
                "sum in advance, and some nine billion guilders reach the treasury at once.\n\n" +
                "The contracts bind the Kingdom whatever the coming winters bring. Every year a part of our output will leave the " +
                "country before our own furnaces are served, and should the fields fall short, the Crown will have to buy back abroad " +
                "what it has already sold. Perla will remember whether Rizia kept its word.",
                "RiziaDLC.Situation_Economy_GlobalEnergyPrice_High == true && RiziaDLC.CountryRelation_Lespia_Hostile == false",
                $"{Authority} -= 2;\n{Budget} += 3;\nRiziaDLC.Diplomacy_Lespia_NegotiatingPower += 1;\nRiziaDLC.RoyalDecrees_Energy_Passed += 1;\n" + Std(),
                new[]
                {
                    LowAuthority(1),
                    ("Our energy output cannot spare a steady export", "RiziaDLC.Resources_EnergyModifiersTotal <= 0"),
                    Sanctions,
                },
                new[] { 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1 },
                () => { }),

            new(RGraceArms, "Trade", TradeMilitaryPath, "Rumburgian Arms on GRACE Credit",
                "-1 Authority\n-1 Budget\n+500 Military Equipment\n+25 Tanks\n-1 Budget Per Turn for 4 Turns",
                "Under the commercial articles of the Guild of Royal Allies for Commercial Exchange, Rumburg offers Rizia a package of " +
                "infantry arms and armored vehicles from its own arsenals, payable over four years at the low rates the Guild extends " +
                "to its members.\n\n" +
                "The terms are generous because the purpose is political: every rifle bought from Rumburg ties our army more closely to " +
                "its patron, and Valgsland will read the deal as a choice of camp. The debt will sit on the treasury's books long after " +
                "the crates are opened.",
                $"{RTurn} >= 2 && RiziaDLC.Policy_Diplomacy_GRACEMember == true && RiziaDLC.GRACETrade_Ends == false && " +
                $"RiziaDLC.CountryRelation_Rumburg_Hostile == false && {Off(RValgsArms)}",
                $"{Authority} -= 1;\n{Budget} -= 1;\nRiziaDLC.Resources_Military_Equipment += 500;\nRiziaDLC.Resources_Military_Tanks += 25;\n" +
                "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower += 1;\nRiziaDLC.Diplomacy_Valgsland_NegotiatingPower -= 1;\n" +
                "RiziaDLC.ImportArmsCount += 1;\n" + Std(MilitaryPassed),
                new[] { LowAuthority(0), LowBudget(0), Sanctions },
                new[] { 1, 1, -1, 1, 1, 1, 1, 1, 0, -1, 0, -1, 1 },
                () => { }),

            new(RValgsArms, "Trade", TradeMilitaryPath, "Accept the Valgslandian Arms Package",
                "-2 Authority\n-3 Budget\n+750 Military Equipment\n+75 Support Vehicles",
                "Valgsland offers Rizia a package of surplus rifles, artillery and trucks from the arsenals of the Commonwealth, at a " +
                "price well below what the western markets ask, as a gesture of goodwill toward a neighbor it would like to see " +
                "outside the western camp.\n\n" +
                "The arms are cheap and plentiful, but no gift from a socialist republic comes without meaning. The Rumburgian court " +
                "will take it as an affront to the Guild, and the monarchists of the Coalition will ask what the Crown is doing in such " +
                "company.",
                $"{RTurn} >= 2 && RiziaDLC.CountryRelation_Valgsland_Hostile == false && RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO == false && " +
                $"{Off(RGraceArms)} && {WarFlags.X(WarFlags.Declared, false)}",
                $"{Authority} -= 2;\n{Budget} -= 3;\nRiziaDLC.Resources_Military_Equipment += 750;\nRiziaDLC.Resources_Military_Trucks += 75;\n" +
                "RiziaDLC.Diplomacy_Valgsland_NegotiatingPower += 2;\nRiziaDLC.Diplomacy_Rumburg_NegotiatingPower -= 2;\n" +
                "RiziaDLC.ImportArmsCount += 1;\n" + Std(MilitaryPassed),
                new[] { LowAuthority(1), LowBudget(2), Sanctions },
                new[] { -1, 0, 1, -1, 1, 1, 0, -1, 1, 0, 1, -1, -1 },
                () => { }),

            new(RExportControls, "Trade", TradeMilitaryPath, "Royal Arms Export Controls",
                "-2 Authority\n+1 Global Perception While in Force\nEnds All Arms Sales Abroad",
                "By royal decree, no Rizian arms may be sold abroad. The surplus rifles, vehicles, ships and aircraft of the armed " +
                "forces will be kept in reserve or scrapped, and the Crown renounces a trade that has made other small nations rich.\n\n" +
                "The world will note that Rizia does not arm the wars of others, and the Crown's standing abroad will rise with it. But " +
                "the treasury loses a ready source of money, the merchants and officers who profited from the trade will resent it, and " +
                "in a lean year the King will have no stockpile to turn into gold.",
                $"{RTurn} >= 2",
                $"{Authority} -= 2;\nRiziaDLC.PersonalValue_Pacifist += 1;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" + Std(),
                new[] { LowAuthority(1) },
                new[] { -1, -1, 0, 1, -1, -1, 0, -1, 0, 1, -1, -1, 0 },
                () => { },
                Rows: new[] { ("Diplomacy_GlobalPerceptionMisc", On(RExportControls), "RiziaDLC.Country_GlobalPerception += 1;") }),

            // ---------------------------------------------------------------- economy
            new(RImportDuties, "Economy", EconomyPath, "Import Duties on Foreign Manufactures",
                "-3 Authority\n+1 Budget Per Turn\n+1 Budget Per Turn During Foreign Goods Reliance",
                "The Crown imposes duties on manufactured goods brought into the Kingdom, from machinery and vehicles to household " +
                "wares, to shelter Rizian workshops from foreign competition and to make the country pay a little less for its reliance " +
                "on goods it cannot yet make itself.\n\n" +
                "The duties will fill the treasury and please the provincial producers, but shoppers will pay more for nearly everything " +
                "in the stores, merchants will grumble, and our partners will not overlook it: Rumburg will call it a breach of the " +
                "Guild's spirit, and Sordland, which sells us much of what we now tax, will answer in kind.",
                $"{RTurn} >= 3 && (RiziaDLC.Situation_Economy_SelfSufficiency_Dependent == true || " +
                "RiziaDLC.Decision_Floating_ForeignGoodsReliance_PenaltyWarning == true)",
                $"{Authority} -= 3;\nRiziaDLC.Country_PublicOpinion -= 2;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" +
                "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower -= 1;\nRiziaDLC.Diplomacy_Sordland_NegotiatingPower -= 1;\n" + Std(),
                new[] { LowAuthority(2) },
                new[] { 1, 0, -1, 0, 0, 0, 0, 1, 0, 0, -1, -1, 1 },
                () => { },
                Rows: new[]
                {
                    ("Economy_RoyalDecreesTwo", $"{On(RImportDuties)} && {Off(RRepealDuties)}", "RiziaDLC.Country_EconomicGrowth -= 1;"),
                }),

            new(RRepealDuties, "Economy", EconomyPath, "Repeal the Import Duties",
                "-2 Authority\nEnds the Import Duties",
                "The duties on foreign manufactures are lifted, and goods from abroad once more enter the Kingdom on the terms of the " +
                "Guild and our trade agreements.\n\n" +
                "Prices in the stores will ease and our partners will take note of the gesture, but the revenue the duties brought in " +
                "will be lost, and the producers who grew used to their shelter will feel the cold wind of competition again.",
                On(RImportDuties),
                $"{Authority} -= 2;\nRiziaDLC.Country_PublicOpinion += 1;\nRiziaDLC.LeaningValue_ProBusiness += 1;\n" +
                "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower += 1;\nRiziaDLC.Diplomacy_Sordland_NegotiatingPower += 1;\n" + Std(),
                new[] { LowAuthority(1) },
                new[] { -1, 0, 1, 0, 0, 0, 0, -1, 0, 0, 1, 1, -1 },
                () => { }),

            new(ROrdinance, "Economy", EconomyPath, "Energy Conservation Ordinance",
                "-2 Authority\n+2 Energy Per Turn While in Force",
                "By royal ordinance, shop windows and public buildings will go dark after ten in the evening, street lighting in the " +
                "cities is halved, and factories, offices and households are held to monthly quotas of electricity and heating gas. " +
                "Inspectors of the Ministry of Energy will fine those who exceed them, and the power and gas saved will keep the grid " +
                "running without new purchases abroad.\n\n" +
                "The Kingdom will save its energy without spending a guilder, but its subjects will pay in darker streets, colder homes " +
                "and shorter shifts at the mills of Valenqir and Iza. The industrialists warn of lost orders, and every month the " +
                "ordinance stays in force will be counted against the Crown.",
                $"{RTurn} >= 2 && (RiziaDLC.Resources_Energy <= 3 || RiziaDLC.Resources_EnergyModifiersTotal <= 0) && {Off(Rationing)}",
                $"{Authority} -= 2;\nRiziaDLC.Country_PublicOpinion -= 2;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" +
                "RiziaDLC.RoyalDecrees_Energy_Passed += 1;\n" + Std(),
                new[] { LowAuthority(1) },
                new[] { 0, 0, 1, 1, -1, -1, 0, -1, 0, 1, 1, -1, -1 },
                () => { },
                Rows: new[]
                {
                    ("Economy_RoyalDecreesOne", $"{On(ROrdinance)} && {Off(RLiftOrdinance)}", "RiziaDLC.Country_EconomicGrowth -= 1;"),
                    ("LivingStandards_WelfareDecreesTwo", $"{On(ROrdinance)} && {Off(RLiftOrdinance)}", "RiziaDLC.Country_LivingStandards -= 1;"),
                }),

            new(RLiftOrdinance, "Economy", EconomyPath, "Lift the Energy Conservation Ordinance",
                "-1 Authority\nEnds the Energy Conservation Ordinance",
                "The quotas on electricity and heating gas are lifted, the street lamps of the cities are lit again, and the inspectors " +
                "of the Ministry of Energy return to their offices.\n\n" +
                "Shopkeepers and factory owners will welcome the end of the dark evenings, but the grid will once again draw on every " +
                "unit the Kingdom produces or buys.",
                On(ROrdinance),
                $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion += 1;\n" + Std(),
                new[] { LowAuthority(0) },
                new[] { 0, 0, -1, 0, 1, 1, 0, 1, 0, 0, -1, 1, 1 },
                () => { }),

            new(RGoldBonds, "Economy", EconomyPath, "Royal Gold Bonds",
                "+5 Budget\n-1 Budget Per Turn for 6 Turns",
                "The Royal Treasury will issue bonds secured on the Crown's stake in Rizian Royal Gold, sold in Porte Drazon to the " +
                "banks, the great merchant families and the Houses of Azaro and Toras. The subscribers lend the Crown fifteen billion " +
                "guilders at once, and the Treasury pays them their coupons, with interest, over the next six years.\n\n" +
                "The money will reach the Treasury within weeks, and the Houses will welcome a safe return on their fortunes. Every " +
                "year, though, the coupon will come out of the budget of a King who borrowed against his own gold.",
                $"{RTurn} >= 2 && {RTurn} <= 5 && RiziaDLC.Situation_Economy_RRG_StateOwns15 == false && {Off(Loan)}",
                $"{Budget} += 5;\nRiziaDLC.LeaningValue_ProBusiness += 1;\n" + CivilianPassed + Std(),
                Array.Empty<(string, string)>(),
                new[] { 1, 0, 0, -1, 1, 1, 0, 1, 0, -1, 0, 1, 1 },
                () => { },
                // the bondholders, while their coupons are paid
                Rows: new[]
                {
                    ("HouseOpinions_DecreesFive", $"{GoldBondsLineVar} <= -1", "RiziaDLC.Country_HouseAzaro += 1;\nRiziaDLC.Country_HouseToras += 1;"),
                }),

            new(RDeregulation, "Economy", EconomyPath, "Relax Labor Regulations [RNC Proposal]",
                "-3 Authority",
                "On the proposal of the Rizian National Coalition, the Crown strikes out the rules that bind employers in the mines, the " +
                "building trades and the ports: the cap on working hours, the inspection of workshops, and the duty to keep a worker on " +
                "through an illness. Employers will be free to hire by the day and to dismiss without notice.\n\n" +
                "The factory owners and merchant houses of Porte Drazon promise new investment and new jobs within the year, and the " +
                "Coalition will count the decree among its victories. The miners of Brenas and the dockers of Monqiz will count it " +
                "differently, and the RPP will remind them every day who signed it.",
                $"{PartyWindow} && RiziaDLC.Policy_RD_Welfare_WorkersRightsAllIndustries == false && RiziaDLC.MITZ_Concession_Regulations == false",
                $"{Authority} -= 3;\nRiziaDLC.Country_PublicOpinion -= 2;\nRiziaDLC.LeaningValue_ProBusiness += 3;\n" +
                "RiziaDLC.PersonalValue_Malenyevist -= 2;\nRiziaDLC.RD_RNCDecreesPassed += 1;\n" + CivilianPassed + Std(),
                new[] { LowAuthority(2) },
                new[] { 1, -1, -1, -1, 1, 1, 0, 1, 0, -1, 0, 1, 1 },
                () => { },
                Rows: new[]
                {
                    ("Economy_RoyalDecreesOne", On(RDeregulation), "RiziaDLC.Country_EconomicGrowth += 2;"),
                    ("LivingStandards_WelfareDecreesOne", On(RDeregulation), "RiziaDLC.Country_LivingStandards -= 2;"),
                    ("HouseOpinions_DecreesFive", On(RDeregulation),
                        "RiziaDLC.Country_HouseToras += 1;\nRiziaDLC.Country_HouseAzaro += 1;\nRiziaDLC.Country_HouseSazon -= 2;"),
                }),

            new(RInspectorate, "Economy", EconomyPath, "Royal Tax Inspectorate for the Provinces",
                "-3 Authority\n-1 Budget\n+1 Budget Per Turn",
                "A Royal Tax Inspectorate, answerable to the Treasury in Porte Drazon, will send its own officers into Valenqiris, " +
                "Brenas and Cardesse-Montaklar to audit the ledgers of the ducal tax offices. For generations the provinces have " +
                "collected the Crown's share themselves and sent on what they saw fit; from now on, the Crown will count it.\n\n" +
                "The arrears recovered and the leaks stopped will add to the Treasury every year. The dukes will see royal clerks in " +
                "their counting houses as an insult to their ancient rights, and all three Houses will remember which King sent them.",
                $"{RTurn} >= 4 && {RTurn} <= 9 && RiziaDLC.Situation_Economy_ProvincialTaxes_FullyPaid == false",
                $"{Authority} -= 3;\n{Budget} -= 1;\nRiziaDLC.Country_PublicOpinion += 1;\nRiziaDLC.LeaningValue_ProBusiness -= 1;\n" +
                "RiziaDLC.PersonalValue_BrokeTradition += 1;\n" + CivilianPassed + Std(),
                new[] { LowAuthority(2), LowBudget(0) },
                new[] { -1, -1, 1, 0, -1, -1, 0, -1, 0, 1, 1, -1, -1 },
                () => { },
                Rows: new[]
                {
                    ("HouseOpinions_DecreesFive", On(RInspectorate),
                        "RiziaDLC.Country_HouseAzaro -= 1;\nRiziaDLC.Country_HouseSazon -= 1;\nRiziaDLC.Country_HouseToras -= 1;"),
                }),

            // ---------------------------------------------------------------- welfare
            new(ROldAgePensions, "Welfare", WelfarePath, "Royal Old-Age Pensions [RPP Proposal]",
                "-2 Authority\n-1 Budget\n-1 Budget Per Turn",
                "On the proposal of the Rizian People's Party, every subject past the age of sixty-five who has worked in the Kingdom " +
                "for twenty years will receive a modest pension from the Royal Treasury, paid each month through the post offices of " +
                "every town. Until now, the old of Rizia have depended on their children, their parish and the charity of the " +
                "Houses.\n\n" +
                "The pension will be small, but it will be the first the Crown has ever paid to those who never wore its uniform, and " +
                "the RPP will carry the credit into every village of Brenas. The Coalition calls it the first step toward a Malenyevist " +
                "treasury, and the bill will grow every year the Kingdom's people grow older.",
                PartyWindow,
                $"{Authority} -= 2;\n{Budget} -= 1;\nRiziaDLC.Country_PublicOpinion += 2;\nRiziaDLC.PersonalValue_Malenyevist += 1;\n" +
                "RiziaDLC.RD_RPPDecreesPassed += 1;\n" + CivilianPassed + Std(),
                new[] { LowAuthority(1), LowBudget(0) },
                new[] { -1, 1, 1, 1, -1, 0, 0, -1, 0, 1, 0, -1, -1 },
                () => { },
                Rows: new[]
                {
                    ("LivingStandards_WelfareDecreesOne", On(ROldAgePensions), "RiziaDLC.Country_LivingStandards += 1;"),
                    ("HouseOpinions_DecreesFive", On(ROldAgePensions), "RiziaDLC.Country_HouseSazon += 1;\nRiziaDLC.Country_HouseToras -= 1;"),
                }),

            new(RShelters, "Welfare", WelfarePath, "Shelters for the Refugees of Pales in Mermi",
                "-1 Authority\n-3 Budget\n-1 Budget Per Turn During the Pales War\n+1 Global Perception",
                "Camps of tents and requisitioned warehouses are raised on the edge of Mermi to shelter the families fleeing the " +
                "fighting in Pales, with field kitchens, a clinic and a register of the displaced. The army's engineers will build them, " +
                "and the Crown will feed them for as long as the war lasts.\n\n" +
                "The shelters will cost the Treasury every month of the war, and some in the capital will ask why Rizian money feeds " +
                "Palesian mouths. But a kingdom that lets the displaced starve on its border will be judged for it in Thornbourgh and " +
                "before the Alliance of Nations.",
                "RiziaDLC.Pales_War == true",
                $"{Authority} -= 1;\n{Budget} -= 3;\nRiziaDLC.Country_WarRefugees -= 2;\nRiziaDLC.Country_PublicOpinion += 1;\n" +
                "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower += 1;\n" + Std(),
                new[] { LowAuthority(0), LowBudget(2) },
                new[] { 0, 1, 1, 1, 0, 0, -1, 0, 1, 1, 0, -1, 0 },
                () => { },
                Rows: new[]
                {
                    ("HumanRights_Decrees", On(RShelters), "RiziaDLC.Country_HumanRights += 1;"),
                    ("Diplomacy_GlobalPerceptionMisc", On(RShelters), "RiziaDLC.Country_GlobalPerception += 1;"),
                }),

            // ---------------------------------------------------------------- construction
            new(RResorts, "Construction", ConstructionPath, "Develop the Montaklar Coast Resorts",
                "-2 Authority\n-3 Budget\n~2 Turn Construction\n+1 Tourism On Completion\n-1 Energy Per Turn On Completion",
                "The Crown commissions hotels, a coastal promenade and a rail spur along the Montaklar shore, opening the beaches of " +
                "Cardesse-Montaklar to visitors from Sordland, Lespia and beyond. The works will be carried out by the construction " +
                "firms of the province under the patronage of House Azaro.\n\n" +
                "The resorts will bring foreign money and seasonal work to a province that has long watched the Crown spend its treasure " +
                "in the north, and the Azaros will take the credit. The clergy of the coast warn that the visitors will bring their " +
                "customs with them, and the new lines will draw on a grid that is already stretched.",
                $"{RTurn} >= 2 && {RTurn} <= 8 && RiziaDLC.Situation_Order_DrugTrade_Vibrant == false",
                $"{Authority} -= 2;\n{Budget} -= 3;\n" + CivilianPassed + Std(),
                new[] { LowAuthority(1), LowBudget(2) },
                new[] { 0, 0, 1, -1, 1, 1, 0, 0, 1, 0, 1, 1, 1 },
                () => { },
                Rows: new[]
                {
                    ("HouseOpinions_DecreesSix", $"{On(RResorts)} && {ResortsDoneVar} == false", "RiziaDLC.Country_HouseAzaro += 1;"),
                    ("HouseOpinions_DecreesSix", $"{ResortsDoneVar} == true", "RiziaDLC.Country_HouseAzaro += 2;"),
                }),

            // ---------------------------------------------------------------- order
            new(RSocieties, "Order", OrderPath, "Fund the Rizian Societies of Zille",
                "-1 Authority\n-2 Budget",
                "Through private foundations in Porte Drazon, the Crown will quietly fund the Rizian clubs, schools, choirs and " +
                "newspapers of Zille, which have kept the city's ties to the Kingdom alive through the years of the Wehzek lease. " +
                "Scholarships will bring the brightest of Zille's young Rizians to the academies of the capital.\n\n" +
                "The Zilleans who long for the Kingdom will grow louder and better organized, and their voice will count when the " +
                "city's future is discussed. Wraclawitz will know exactly who pays for the choirs, and will not thank the Crown for it.",
                $"{RTurn} >= 2 && {RTurn} <= 7 && {WarFlags.X(WarFlags.Declared, false)} && RiziaDLC.ZilleReturn_Success == false",
                $"{Authority} -= 1;\n{Budget} -= 2;\nRiziaDLC.Zille_ReferendumPower += 2;\nRiziaDLC.Diplomacy_Wehlen_NegotiatingPower -= 1;\n" +
                "RiziaDLC.Country_PublicOpinion += 1;\n" + Std(),
                new[] { LowAuthority(0), LowBudget(1) },
                new[] { 1, 0, -1, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
                () => { }),

            new(RBanAssemblies, "Order", OrderPath, "Prohibit Reformist Assemblies [RNC Proposal]",
                "-3 Authority\n-1 Global Perception While in Force",
                "Proposed by the Rizian National Coalition, the decree bans public assemblies, marches and congresses called in the " +
                "name of constitutional reform, and empowers the police to disperse them. Reformist newspapers may still print, but " +
                "their readers may no longer gather in the squares.\n\n" +
                "The movement will lose its crowds, but not its grievances, and those who remain will be the angriest of them. House " +
                "Sazon, whose towns host the reformers' congresses, will take the ban as aimed at Brenas, and foreign observers will " +
                "note it.",
                $"{RTurn} >= 5 && {RTurn} <= 9 && RiziaDLC.Reform_Movement_Strength >= 8 && RiziaDLC.Reform_Path == false && " +
                "RiziaDLC.Reform_Success == false",
                $"{Authority} -= 3;\nRiziaDLC.Country_PublicOpinion -= 2;\nRiziaDLC.LeaningValue_Democratic -= 2;\n" +
                "RiziaDLC.Diplomacy_Morella_NegotiatingPower -= 1;\nRiziaDLC.RD_RNCDecreesPassed += 1;\n" + Std(),
                new[] { LowAuthority(2) },
                new[] { 1, -1, -1, 0, 1, 1, 1, 1, -1, -1, 0, 1, 1 },
                () => { },
                Rows: new[]
                {
                    ("Reform_Movement_OtherModifiers", On(RBanAssemblies),
                        "RiziaDLC.Reform_Movement_Strength -= 3;\nRiziaDLC.Reform_Movement_Radicalism += 2;"),
                    ("HumanRights_Decrees", On(RBanAssemblies), "RiziaDLC.Country_HumanRights -= 1;"),
                    ("Diplomacy_GlobalPerceptionMisc", On(RBanAssemblies), "RiziaDLC.Country_GlobalPerception -= 1;"),
                    ("HouseOpinions_DecreesFour", On(RBanAssemblies),
                        "RiziaDLC.Country_HouseSazon -= 2;\nRiziaDLC.Country_HouseToras += 1;\nRiziaDLC.Country_HouseAzaro += 1;"),
                }),

            new(RSuOmina, "Order", OrderPath, "Royal Crackdown on Su Omina in Iza",
                "-3 Authority\n-2 Budget\nEnds Su Omina Destabilization",
                "The gendarmerie and the royal intelligence service are ordered into Iza to arrest Su Omina's commanders, seize their " +
                "arms caches and close the clubs and charities that fund them. Members who surrender their weapons will be released " +
                "under watch.\n\n" +
                "Su Omina's violence against the Crown will end, and Wraclawitz will welcome the arrests. But the Rizian families of " +
                "Iza will see their sons taken, House Toras will resent soldiers in its city, and the Crown will lose the only fighters " +
                "ready to act for it in Zille.",
                $"RiziaDLC.Situation_Order_SuOmina_Destabilizing == true && {RTurn} >= 5 && {RTurn} <= 9 && {WarFlags.X(WarFlags.Declared, false)}",
                $"{Authority} -= 3;\n{Budget} -= 2;\nRiziaDLC.Zille_ViolentResistancePower -= 2;\nRiziaDLC.Diplomacy_Wehlen_NegotiatingPower += 1;\n" +
                "RiziaDLC.Country_PublicOpinion += 1;\n" + Std(),
                new[] { LowAuthority(2), LowBudget(1) },
                new[] { 1, 0, 0, 0, 1, 1, 1, -1, 1, -1, 0, -1, 1 },
                () => { },
                Rows: new[]
                {
                    // appended last, it overrides the recomputed power; SuOmina_AlliedToCrown then clears the destabilization
                    ("SuOmina_Power", On(RSuOmina), "RiziaDLC.Iza_SuOminaSuppressed = true;\nRiziaDLC.Iza_SuOminaPowerful = false;"),
                    ("HouseOpinions_DecreesFour", On(RSuOmina), "RiziaDLC.Country_HouseToras -= 2;"),
                    ("Iza_AntiMonarchism", On(RSuOmina), "RiziaDLC.Iza_AntiMonarchism += 1;"),
                }),

            // ---------------------------------------------------------------- religion
            new(RRepealReligiousTax, "Religion", ReligionPath, "Repeal the Non-Wruhecist Religious Tax",
                "-2 Authority\n-2 Budget Per Turn",
                "The Crown repeals the special tax levied on Dastnurist and Golcondist congregations, restoring them to the same footing " +
                "as the Wruhecist faithful. Collections already made will not be returned.\n\n" +
                "The Dastnurists of Sallabes and their co-religionists abroad will welcome the repeal, and Rumburg will note it. The " +
                "Coalition that proposed the tax will call it a surrender to the street, and the Treasury will feel the loss every month.",
                $"RiziaDLC.Policy_RD_Economy_NonWruhecistReligiousTax == true && RiziaDLC.Religious_WruhecsGuardians_Sal == false && " +
                $"RiziaDLC.Policy_RD_Religion_RemoveStateReligion == false && {RTurn} >= 8",
                $"{Authority} -= 2;\nRiziaDLC.Policy_RD_Economy_NonWruhecistReligiousTax = false;\nRiziaDLC.Country_PublicOpinion -= 1;\n" +
                "RiziaDLC.PersonalValue_Religious -= 1;\nRiziaDLC.Diplomacy_Rumburg_NegotiatingPower += 1;\n" +
                "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower += 1;\n" + Std(),
                new[] { LowAuthority(1) },
                new[] { -1, 1, 0, -1, 0, 0, -1, -1, 1, 1, 0, 1, -1 },
                () => { },
                Rows: new[]
                {
                    ("SectarianOpinion_DecreesFive", On(RRepealReligiousTax),
                        "RiziaDLC.Country_DastnuristOpinion += 1;\nRiziaDLC.Country_WruhecistOpinion -= 1;"),
                }),

            new(RInterfaith, "Religion", ReligionPath, "Convene the Royal Interfaith Council",
                "-2 Authority\n-2 Budget\n-1 Budget Per Turn",
                "The King convenes a standing council of Wruhecist priests, Dastnurist elders and Golcondist clerics, which will meet in " +
                "Porte Drazon each season to settle disputes over sanctuaries, schools and festivals before they reach the streets. The " +
                "Crown will pay its sittings and its staff.\n\n" +
                "The minority faiths will have a seat near the throne for the first time, and their congregations, and Derdia, will " +
                "notice. The Wruhecist clergy will resent sharing the King's ear with those they call heretics, and Sal and his " +
                "followers will say the state faith has been demoted.",
                $"{RTurn} >= 3 && {RTurn} <= 9 && RiziaDLC.Policy_RD_Religion_RemoveStateReligion == false && " +
                "RiziaDLC.Policy_RD_Law_WruhecismAsSoleFaith == false && RiziaDLC.Policy_RD_Law_BanOnNonNurityFaiths == false && " +
                "RiziaDLC.Religious_WruhecsGuardians_Sal == false",
                $"{Authority} -= 2;\n{Budget} -= 2;\nRiziaDLC.PersonalValue_Religious += 1;\nRiziaDLC.Diplomacy_Derdia_NegotiatingPower += 1;\n" + Std(),
                new[] { LowAuthority(1), LowBudget(1) },
                new[] { 0, 1, 1, -1, 0, 0, 0, 0, 1, 1, 0, 0, 0 },
                () => { },
                Rows: new[]
                {
                    ("SectarianOpinion_DecreesFive", InterfaithActive,
                        "RiziaDLC.Country_DastnuristOpinion += 2;\nRiziaDLC.Country_GolcondistOpinion += 2;\nRiziaDLC.Country_WruhecistOpinion -= 1;"),
                    ("HouseOpinions_DecreesTwo", InterfaithActive, "RiziaDLC.Country_HouseSazon += 1;\nRiziaDLC.Country_HouseToras -= 1;"),
                }),
        };

        foreach (Decree d in list)
        {
            if (!Decrees.Exists(x => x.Key == d.Key))
            {
                Decrees.Add(d);
            }
        }
    }

    // ------------------------------------------------------------------ HUD lines and Overview cards

    private static void AddRoyalLines()
    {
        const string budget = "Rizia_HUDStat_Budget", energy = "Rizia_HUDStat_Energy";
        AddLine(new("Rizia_Energy_Modifier_Royal_ForwardGas", energy, "ZilleWar.Dec_ForwardGas",
            "-{0} per turn from Forward Gas Contracts", () => Enacted(RForwardGas) ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_GraceArmsCredit", budget, "ZilleWar.Dec_GraceArmsCredit",
            "-{0} per turn from Rumburgian Arms Credit", () => Installment(RGraceArms, 4) ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_ImportDuties", budget, "ZilleWar.Dec_ImportDuties",
            "+{0} per turn from Import Duties",
            () => Enacted(RImportDuties) && !Enacted(RRepealDuties) ? 1 + (ForeignGoodsReliance ? 1 : 0) : 0));
        AddLine(new("Rizia_Energy_Modifier_Royal_EnergyOrdinance", energy, "ZilleWar.Dec_EnergyOrdinance",
            "+{0} per turn from the Energy Conservation Ordinance", () => Enacted(ROrdinance) && !Enacted(RLiftOrdinance) ? 2 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_GoldBonds", budget, GoldBondsLineVar,
            "-{0} per turn from Royal Gold Bond Coupons", () => Installment(RGoldBonds, 6) ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_OldAgePensions", budget, "ZilleWar.Dec_OldAgePensions",
            "-{0} per turn from Royal Old-Age Pensions", () => Enacted(ROldAgePensions) ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_TaxInspectorate", budget, "ZilleWar.Dec_TaxInspectorate",
            "+{0} per turn from the Royal Tax Inspectorate", () => Enacted(RInspectorate) ? 1 : 0));
        AddLine(new("Rizia_Energy_Modifier_Royal_MontaklarResorts", energy, "ZilleWar.Dec_MontaklarResorts",
            "-{0} per turn from the Montaklar Coast Resorts", () => Variables.GetBool(ResortsDoneVar) ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_MermiShelters", budget, "ZilleWar.Dec_MermiShelters",
            "-{0} per turn from the Refugee Shelters in Mermi", () => Enacted(RShelters) && Variables.GetBool("RiziaDLC.Pales_War") ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_InterfaithCouncil", budget, "ZilleWar.Dec_InterfaithCouncil",
            "-{0} per turn from the Royal Interfaith Council", () => InterfaithSits ? -1 : 0));
    }

    private static void AddLine(Line l)
    {
        if (!Lines.Exists(x => x.Name == l.Name))
        {
            Lines.Add(l);
        }
    }

    private static void AddCard(Card c)
    {
        if (!Cards.Exists(x => x.Name == c.Name))
        {
            Cards.Add(c);
        }
    }

    private static void AddRoyalCards()
    {
        AddCard(new("ZilleWar_Economy_ImportDuties", "Economy", "ZilleWar.DecPol_ImportDuties", "Import Duties",
            "Manufactured goods brought into the Kingdom pay a royal duty at the ports and border posts.",
            () => Enacted(RImportDuties) && !Enacted(RRepealDuties)));
        AddCard(new("ZilleWar_Military_ArmsExportControls", "Military", "ZilleWar.DecPol_ArmsExportControls", "Royal Arms Export Controls",
            "No Rizian arms may be sold abroad; the surplus of the armed forces is kept in reserve or scrapped.",
            () => Enacted(RExportControls)));
        AddCard(new("ZilleWar_Economy_EnergyOrdinance", "Economy", "ZilleWar.DecPol_EnergyOrdinance", "Energy Conservation Ordinance",
            "Shop windows go dark at ten, street lighting is halved, and electricity and heating gas are sold against monthly quotas.",
            () => Enacted(ROrdinance) && !Enacted(RLiftOrdinance)));
        AddCard(new("ZilleWar_Welfare_OldAgePensions", "Welfare", "ZilleWar.DecPol_OldAgePensions", "Royal Old-Age Pensions",
            "Subjects past sixty-five who worked twenty years in the Kingdom receive a monthly pension from the Royal Treasury.",
            () => Enacted(ROldAgePensions)));
        AddCard(new("ZilleWar_Economy_LaborDeregulation", "Economy", "ZilleWar.DecPol_LaborDeregulation", "Relaxed Labor Regulations",
            "Employers may hire by the day and dismiss without notice; working hours and workshops are no longer inspected.",
            () => Enacted(RDeregulation)));
        AddCard(new("ZilleWar_Economy_TaxInspectorate", "Economy", "ZilleWar.DecPol_TaxInspectorate", "Royal Tax Inspectorate",
            "Royal officers audit the ledgers of the ducal tax offices in every province.",
            () => Enacted(RInspectorate)));
        AddCard(new("ZilleWar_Law_BanAssemblies", "Law", "ZilleWar.DecPol_BanAssemblies", "Reformist Assemblies Prohibited",
            "Assemblies, marches and congresses in the name of constitutional reform are banned and dispersed by the police.",
            () => Enacted(RBanAssemblies)));
        AddCard(new("ZilleWar_Law_InterfaithCouncil", "Law", "ZilleWar.DecPol_InterfaithCouncil", "Royal Interfaith Council",
            "Wruhecist priests, Dastnurist elders and Golcondist clerics meet in Porte Drazon each season under the Crown's patronage.",
            () => InterfaithSits));
    }
}
