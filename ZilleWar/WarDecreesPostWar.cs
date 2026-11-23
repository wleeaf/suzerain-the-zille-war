using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// The post-war decrees of a war game (Turns 6 to 10): the administration of Zille (liberated or divided), its harbor and its
/// Wehzeks, the prisoners of war, the reckoning after a failed campaign, the debts and grievances the war left behind, and the
/// home front under the Valgish embargo. Also the changes these decrees make to WorldModifiers' war lines (through
/// <see cref="Adjust"/>) and the edits to vanilla decrees in a war game (<see cref="VanillaDecrees"/>).
/// Everything is built inside <see cref="RegisterPostWar"/>, never in static field initializers: the class is partial, and
/// C# runs the field initializers of different partial files in an unspecified order.
/// </summary>
internal static partial class WarDecrees
{
    private const string CivilAdminKey = "CivilAdmin";
    private const string RebuildHarborKey = "RebuildHarbor";
    private const string LoyaltyOathKey = "LoyaltyOath";
    private const string WehzekSchoolsKey = "WehzekSchools";
    private const string ReleasePrisonersKey = "ReleasePrisoners";
    private const string RansomSoldiersKey = "RansomSoldiers";
    private const string CourtMartialKey = "CourtMartial";
    private const string InquiryKey = "Inquiry";
    private const string SuspendReparationsKey = "SuspendReparations";
    private const string RepayRumburgKey = "RepayRumburg";
    private const string SettleLespiaKey = "SettleLespia";
    private const string SouthernTradeKey = "SouthernTrade";
    private const string ZilleanLegionKey = "ZilleanLegion";        // Part A's wartime decree

    private const string HarborDoneVar = "ZilleWar.Decree_RebuildHarbor_Done";

    // Order, Economy and Welfare paths: WarDecrees.cs
    private const string ConstructionPath = "Rizia/Royal Decrees Panel/Construction Decrees";

    private const string PerceptionRows = "Diplomacy_GlobalPerceptionWar";
    private const string ProjectionRows = "PowerProjection_War_Outcome";

    // ------------------------------------------------------------------ state the post-war decrees read

    /// <summary>The rebuilt harbor of Zille is open: two turns after the decree.</summary>
    private static bool PwHarborDone => Enacted(RebuildHarborKey) && WorldSystems.Turn >= EnactTurn(RebuildHarborKey) + 2;

    /// <summary>Rizia still holds its Wehzek prisoners of war (liberated or divided war).</summary>
    private static bool PwPrisonersHeld => Variables.GetString(LateT8.VarPrisoners) is "held" or "ransomed";

    /// <summary>A failed phase in Zille: a lost war, or a divided one that ended after a defeat.</summary>
    private static bool PwFailedCampaign => Lost || (Part && Variables.GetBool(WarFlags.Defeated));

    /// <summary>The "Defeat in Zille" authority penalty runs (WorldModifiers' Humiliation rule: a lost war after a failed
    /// operation, for four turns): the court-martial is offered only while it has a penalty to end.</summary>
    private static bool PwDefeatPenalty => Lost && Variables.GetBool(WarFlags.Defeated) && Since is >= 0 and <= 3;

    /// <summary>The peace talks are over and Rizia still holds Wehzek prisoners (liberated or divided war), treaty or not.</summary>
    private static bool PwCampsOpen => (Lib || Part) && TalksDone && PwPrisonersHeld;

    /// <summary>The Valgish embargo applies now (WorldEmbargo's rule).</summary>
    private static bool PwEmbargo =>
        WorldSystems.Declared && (WorldSystems.Active || WorldSystems.ZilleCityHeld || WorldSystems.Liberated) &&
        !Variables.GetBool(ZilleTreaty.VarPeace) && !Variables.GetBool("RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP") &&
        !Variables.GetBool("RiziaDLC.Trait_CommieKing");

    private static string PwHolding => "(ZilleWar.War_Zille_Liberated == true || ZilleWar.ZilleCity_Held == true)";

    private static void PwAdd(string variable, int delta) => Variables.Set(variable, Variables.GetInt(variable) + delta);

    // ------------------------------------------------------------------ registration

    static partial void RegisterPostWar()
    {
        Variables.Register(HarborDoneVar);
        AddPostWarDecrees();
        AddPostWarLines();
        AddPostWarCards();
        AddPostWarAdjusters();

        // the Wehzeks of Nanta have their schools and courts: no protest against the return to Rizia
        Vanilla.HideReport("Turn10_Nanta_WehzeksProtest", () => Enacted(WehzekSchoolsKey));

        VanillaDecrees.Register();
    }

    static partial void PostWarStep()
    {
        Run("harbor of Zille", HarborCompletion);
        Run("vanilla decrees", VanillaDecrees.OnStep);
    }

    private static void AddPostWarAdjusters()
    {
        // a divided war pays for the Military Administration of Zille city, however it ended, until a civil one replaces it
        Adjusters.Add(("ZilleWar.Mod_Occupation", v => Part && WorldSystems.ZilleCityHeld ? (Enacted(CivilAdminKey) ? 0 : -1) : v));
        // the port earns from the first turn after a divided war; a rebuilt harbor earns more, and lifts the Nanta delay
        Adjusters.Add(("ZilleWar.Mod_ZillePort", v =>
        {
            int p = v;
            if (Part && Since >= 1)
            {
                p = Math.Max(p, 1);
            }

            if (Lib && Enacted(RebuildHarborKey) && Since >= 1)
            {
                p = Math.Max(p, 1);
            }

            if (PwHarborDone)
            {
                p += 1;
            }

            return p;
        }));
        Adjusters.Add(("ZilleWar.Mod_Humiliation", v => Enacted(CourtMartialKey) ? 0 : v));
        Adjusters.Add(("ZilleWar.Mod_RumburgTutelage", v => Enacted(RepayRumburgKey) ? 0 : v));
    }

    private static void AddPostWarLines()
    {
        Lines.Add(new("Rizia_Budget_Modifier_ZilleWar_PrisonerCamps", "Rizia_HUDStat_Budget", "ZilleWar.Dec_PrisonerCamps",
            "-{0} per turn from Prisoner of War Camps",
            () => PwCampsOpen && !Enacted(ReleasePrisonersKey) ? -1 : 0));
        Lines.Add(new("Rizia_Budget_Modifier_ZilleWar_WehzekSchools", "Rizia_HUDStat_Budget", "ZilleWar.Dec_WehzekSchools",
            "-{0} per turn from Wehzek Schools and Courts in Zille",
            () => Enacted(WehzekSchoolsKey) && WorldSystems.Turn - EnactTurn(WehzekSchoolsKey) is >= 0 and <= 2 ? -1 : 0));
        Lines.Add(new("Rizia_Energy_Modifier_ZilleWar_LesPowerSettlement", "Rizia_HUDStat_Energy", "ZilleWar.Dec_LesPowerSettlement",
            "-{0} per turn from the Gas Settlement with LesPower",
            () => Enacted(SettleLespiaKey) && WorldSystems.Turn - EnactTurn(SettleLespiaKey) is >= 0 and <= 2 ? -2 : 0));
        Lines.Add(new("Rizia_Budget_Modifier_ZilleWar_SouthernTrade", "Rizia_HUDStat_Budget", "ZilleWar.Dec_SouthernTrade",
            "+{0} per turn from Southern Trade Routes", () => Enacted(SouthernTradeKey) && PwEmbargo ? 1 : 0));
    }

    private static void AddPostWarCards()
    {
        Cards.Add(new("ZilleWar_Order_ZilleCivilAdministration", "Order", "ZilleWar.DecPol_CivilAdmin", "Civil Administration in Zille City",
            "Zille city is governed by Rizian civil servants under a royal commissioner. The curfew is lifted and the city council " +
            "meets again, while the armistice line still divides the province.", () => Enacted(CivilAdminKey) && Part));
        Cards.Add(new("ZilleWar_Economy_ZilleHarborWorks", "Economy", "ZilleWar.DecPol_HarborWorks", "Harbor Works in Zille",
            "The quays, cranes and warehouses of Zille wrecked in the fighting are being rebuilt at the Crown's expense, and the " +
            "harbor mouth is being cleared of wrecks.", () => Enacted(RebuildHarborKey) && !PwHarborDone));
        Cards.Add(new("ZilleWar_Law_ZilleLoyaltyOath", "Law", "ZilleWar.DecPol_LoyaltyOath", "Loyalty Oath in Zille",
            "Every official, teacher, judge and policeman in Zille who served under the Wehzek administration must swear " +
            "allegiance to the Crown of Rizia, or lose his post and his pension.", () => Enacted(LoyaltyOathKey)));
        Cards.Add(new("ZilleWar_Welfare_WehzekSchools", "Welfare", "ZilleWar.DecPol_WehzekSchools", "Wehzek Schools and Courts in Zille",
            "Wehzek-language schools, bilingual courts and street signs in both languages are guaranteed in the towns of Zille " +
            "where Wehzeks live.", () => Enacted(WehzekSchoolsKey)));
        Cards.Add(new("ZilleWar_Economy_SouthernTrade", "Economy", "ZilleWar.DecPol_SouthernTrade", "Southern Trade Routes",
            "Rizian cargoes turned away by Valgsland and its partners travel south through Derdian and Morellan ports, under " +
            "royal guarantees and reduced port fees.", () => Enacted(SouthernTradeKey) && PwEmbargo));
        Cards.Add(new("ZilleWar_Economy_ReparationsSuspended", "Economy", "ZilleWar.DecPol_ReparationsSuspended",
            "Reparations to Wehlen Suspended",
            "The Crown has stopped paying the installments agreed under the Terms of Wraclawitz, holding that a treaty signed " +
            "under the guns of a victorious army does not bind a free kingdom.", () => Enacted(SuspendReparationsKey)));
    }

    // ------------------------------------------------------------------ the twelve decrees

    private static void AddPostWarDecrees()
    {
        string rights = $"{ZilleTreaty.VarSigned} == true && {ZilleTreaty.Term("Rights")} == true";

        Decrees.AddRange(new Decree[]
        {
            new(CivilAdminKey, "Order", OrderPath, "Civil Administration for Zille City",
                "-2 Authority\n-2 Budget\nEnds the Military Administration of Zille",
                "The army officers who have governed Zille city since the guns fell silent hand its town hall, courts and police to " +
                "Rizian civil servants answerable to the Crown. Curfews are lifted, the harbor reopens to civilian shipping at night, " +
                "and the city council meets again under a royal commissioner.\n\n" +
                "Zille city will be governed more cheaply and more gently, and the Zilleans will see that Rizian rule means more than " +
                "checkpoints. The generals of House Azaro, who have grown used to the city, will not hand it over gladly.",
                $"{Over} && ZilleWar.ZilleCity_Held == true && ZilleWar.War_Zille_Liberated == false",
                $"{Authority} -= 2;\n{Budget} -= 2;\nRiziaDLC.LeaningValue_Democratic += 1;\n" + Std(),
                new[] { LowAuthority(1), LowBudget(1) },
                new[] { 1, 1, 0, 0, -1, -1, -1, 0, 1, 1, 1, 0, 1 },
                PwCivilAdmin,
                Open: () => Part),

            new(RebuildHarborKey, "Construction", ConstructionPath, "Rebuild the Harbor of Zille",
                "-2 Authority\n-3 Budget\n~2 Turn Construction\n+1 Budget Per Turn On Completion",
                "Cranes, quays and warehouses wrecked in the fighting for Zille are rebuilt at the Crown's expense, the harbor mouth " +
                "is cleared of wrecks, and the old fish market where the Queen Mother played as a girl is restored stone by stone.\n\n" +
                "Rebuilt, the harbor will bring trade and customs duties back to Zille within two years. Until then the treasury will " +
                "carry the cost, and the merchants of Monqiz will watch a rival port rise in the north.",
                $"{Over} && {PwHolding}",
                $"{Authority} -= 2;\n{Budget} -= 3;\nRiziaDLC.Relations_Estela += 1;\n" + Std("RiziaDLC.RoyalDecrees_Civilian_Passed += 1;"),
                new[] { LowAuthority(1), LowBudget(2) },
                new[] { 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1 },
                PwRebuildHarbor,
                Open: () => Lib || Part),

            new(LoyaltyOathKey, "Order", OrderPath, "Loyalty Oath for the Officials of Zille",
                "-1 Authority\n-1 Global Perception While in Force",
                "Every official, teacher, judge and policeman who served in Zille under the Wehzek administration must swear " +
                "allegiance to the Crown of Rizia before the month is out, or lose his post and his pension.\n\n" +
                "The province will be run by men whose loyalty is sworn, and Brenas will cheer the measure. Many Wehzeks will choose " +
                "to leave rather than swear, and the foreign press will report every family that does.",
                $"{Over} && {PwHolding} && {WarFlags.X(EnactedVar(WehzekSchoolsKey), false)}",
                $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion += 1;\nRiziaDLC.LeaningValue_Democratic -= 1;\n" + Std(),
                new[] { LowAuthority(0), ("The treaty guarantees the Wehzeks of Zille their homes and their places", rights) },
                new[] { 1, 1, -1, 0, 1, 1, 1, 0, -1, -1, 0, 0, 1 },
                PwLoyaltyOath,
                Open: () => Lib || Part,
                Leverage: () => -1,
                Rows: new[] { (PerceptionRows, WarFlags.X(EnactedVar(LoyaltyOathKey)), "RiziaDLC.Country_GlobalPerception -= 1;") }),

            new(WehzekSchoolsKey, "Welfare", WelfarePath, "Wehzek Schools and Courts in Zille",
                "-1 Authority\n-1 Budget Per Turn for 3 Turns\n+1 Global Perception While in Force",
                "Wehzek-language schools, bilingual courts and street signs in both languages are guaranteed by royal decree in the " +
                "towns of Zille where Wehzeks live, and Wehzek officials who keep the peace keep their posts.\n\n" +
                "Zille's Wehzeks will have one reason fewer to look north, and the Crown will be praised abroad for its restraint. In " +
                "Brenas they will ask what the war was for, if the enemy's language is still taught in Zille.",
                $"{Over} && {PwHolding} && {WarFlags.X(EnactedVar(LoyaltyOathKey), false)}",
                $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion -= 1;\nRiziaDLC.LeaningValue_Democratic += 1;\n" + Std(),
                new[] { LowAuthority(0) },
                new[] { 0, -1, 1, 0, -1, -1, 0, 0, 1, 1, 0, 0, -1 },
                PwWehzekSchools,
                Open: () => Lib || Part,
                Leverage: () => 1,
                Rows: new[] { (PerceptionRows, WarFlags.X(EnactedVar(WehzekSchoolsKey)), "RiziaDLC.Country_GlobalPerception += 1;") }),

            new(ReleasePrisonersKey, "Order", OrderPath, "Release the Wehzek Prisoners of War",
                "-1 Authority\nEnds the Prisoner of War Camps",
                "The Wehzek soldiers still held in Rizian camps since the war are released and taken under Alliance of Nations escort " +
                "to the border, officers and men alike, whatever Wraclawitz has or has not yet paid for them.\n\n" +
                "The camps will close and their guards go home, and the Kingdom will be thanked in Konstantium. Rizia gives up the " +
                "last hostage it held over Wehlen, and the families of our own dead will ask why Smolak's men go home first.",
                Over,
                $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion -= 1;\n" + Std(),
                new[] { LowAuthority(0) },
                new[] { 0, -1, 1, 1, -1, -1, 0, 0, 1, 1, 1, 0, 0 },
                PwReleasePrisoners,
                Open: () => PwCampsOpen,
                Rows: new[] { (PerceptionRows, WarFlags.X(EnactedVar(ReleasePrisonersKey)), "RiziaDLC.Country_GlobalPerception += 1;") }),

            new(RansomSoldiersKey, "Order", OrderPath, "Ransom Our Soldiers",
                "-1 Authority\n-3 Budget",
                "The Crown pays Wraclawitz for the release of the Rizian soldiers taken prisoner in Zille, who have waited in Wehzek " +
                "camps since the guns fell silent. The money goes to the Wehzek treasury through Alliance of Nations intermediaries.\n\n" +
                "Mothers in Brenas and Montaklar will see their sons before winter, and the army will remember that the Crown did not " +
                "abandon them. President Smolak will be paid for his victory a second time, and will make sure the world knows it.",
                $"{Over} && {LateState.VarZilleLost} == true",
                $"{Authority} -= 1;\n{Budget} -= 3;\nRiziaDLC.Country_PublicOpinion += 2;\n" + Std(),
                new[] { LowAuthority(0), LowBudget(2) },
                new[] { 0, 1, 0, 1, 1, 1, 0, 0, 1, 1, -1, 0, 0 },
                PwRansomSoldiers,
                Open: () => Lost && TalksDone && Variables.GetString(LateT8.VarPrisoners) != "released" &&
                            !(Variables.GetBool(ZilleTreaty.VarSigned) && Variables.GetBool(ZilleTreaty.Term("Pow")))),

            new(CourtMartialKey, "Order", OrderPath, "Court-Martial of the Zille Commanders",
                "+1 Authority\nEnds the Defeat in Zille Penalty",
                "The generals who commanded the failed operations in Zille are relieved of their commands and brought before a " +
                "court-martial in Porte Drazon, charged with negligence in the face of the enemy.\n\n" +
                "The public will have its culprits, and the Crown will be seen to demand accountability. House Azaro will take the " +
                "trial of its officers as an insult it does not forgive, and the army will remember who was blamed for a war the King " +
                "declared.",
                $"{Over} && ZilleWar.War_Zille_Liberated == false && {WarFlags.X(EnactedVar(InquiryKey), false)} && RiziaDLCSetup.CurrentTurn <= 9",
                $"{Authority} += 1;\nRiziaDLC.Country_PublicOpinion += 2;\n" + Std(),
                Array.Empty<(string, string)>(),
                new[] { 0, 1, 0, 0, -1, -1, 1, 1, 0, 0, 0, 0, 1 },
                PwCourtMartial,
                Open: () => PwDefeatPenalty,
                Rows: new[] { (ProjectionRows, WarFlags.X(EnactedVar(CourtMartialKey)), "RiziaDLC.Country_Military_PowerProjection -= 1;") }),

            new(InquiryKey, "Order", OrderPath, "Royal Commission of Inquiry into the Zille Campaign",
                "-2 Authority",
                "A royal commission of judges, retired officers and members of the House of Delegates is appointed to examine the " +
                "conduct of the Zille campaign, from the declaration in Konstantium to the last day of fighting, and to report " +
                "publicly to the Crown.\n\n" +
                "The King accepts his share of the blame before the commission has written a word, and the army is spared a witch " +
                "hunt. The report will make painful reading, and the lessons it draws will serve the army in the wars still to come.",
                $"{Over} && ZilleWar.War_Zille_Liberated == false && {WarFlags.X(EnactedVar(CourtMartialKey), false)} && RiziaDLCSetup.CurrentTurn <= 9",
                $"{Authority} -= 2;\nRiziaDLC.Country_PublicOpinion -= 1;\nRiziaDLC.LeaningValue_Democratic += 1;\n" + Std(),
                new[] { LowAuthority(1) },
                new[] { 0, 0, 1, 1, 1, 0, 0, -1, 1, 1, 1, 0, -1 },
                PwInquiry,
                Open: () => PwFailedCampaign,
                Rows: new[]
                {
                    (PerceptionRows, WarFlags.X(EnactedVar(InquiryKey)), "RiziaDLC.Country_GlobalPerception += 1;"),
                    (ProjectionRows, WarFlags.X(EnactedVar(InquiryKey)), "RiziaDLC.Country_Military_PowerProjection += 1;"),
                }),

            new(SuspendReparationsKey, "Economy", EconomyPath, "Suspend Reparation Payments to Wehlen",
                "-1 Authority\nEnds the Reparations to Wehlen\n-2 Global Perception While in Force",
                "The Crown suspends the installments it agreed to pay Wraclawitz under the Terms of Wraclawitz, on the ground that a " +
                "treaty signed under the guns of a victorious army does not bind a free kingdom.\n\n" +
                "Brenas will cheer and the treasury will breathe again. In Konstantium and Thornbourgh the decree will be read as a " +
                "broken word, and President Smolak will be glad to remind the world of it at every session of the Alliance.",
                $"{Over} && {LateState.VarZilleLost} == true && {WorldModifiers.WehlenReparations} <= -1",
                $"{Authority} -= 1;\nRiziaDLC.Country_PublicOpinion += 1;\n" +
                "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower -= 1;\nRiziaDLC.Diplomacy_Morella_NegotiatingPower -= 1;\n" + Std(),
                new[] { LowAuthority(0) },
                new[] { 1, 1, 0, 0, 1, 1, 1, 1, -1, -1, 0, -1, 1 },
                PwSuspendReparations,
                Open: () => Lost,
                Rows: new[] { (PerceptionRows, WarFlags.X(EnactedVar(SuspendReparationsKey)), "RiziaDLC.Country_GlobalPerception -= 2;") }),

            new(RepayRumburgKey, "Economy", EconomyPath, "Repay the Rumburgian War Credits Early",
                "-4 Budget\nEnds the Rumburgian Credit Repayments",
                "The Royal Treasury repays in one sum what remains of the war credits Queen Beatrice's government extended to Rizia " +
                "during the Zille campaign, years ahead of schedule.\n\n" +
                "Thornbourgh will be pleased, and it will be harder for anyone there to speak of Rizia as a client. The money will be " +
                "missed at home, where it could have rebuilt a harbor or paid a pension.",
                $"{Over} && {WorldModifiers.RumburgRepayment} <= -1",
                $"{Budget} -= 4;\nRiziaDLC.Diplomacy_Rumburg_NegotiatingPower += 1;\n" + Std(),
                new[] { LowBudget(3) },
                new[] { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, -1, 0, 0 },
                PwRepayRumburg,
                Open: () => Variables.GetInt(WorldModifiers.RumburgRepayment + "_Turns") >= 2),

            new(SettleLespiaKey, "Economy", EconomyPath, "Settle Lespia's Accounts",
                "-1 Authority\n-2 Energy Per Turn for 3 Turns",
                "To close the quarrel of the war years, Rizia offers LesPower Aureus gas at a discount for three years and sends a " +
                "delegation to Perla with the King's personal regrets for promises that were not kept.\n\n" +
                "Lespia's trade, and perhaps its friendship, will return, and Prime Minister Alvarez may think twice before arming " +
                "Rizia's enemies again. Those who know who planned the bombing in Zille will ask why the Crown is paying Perla, and not " +
                "the other way round.",
                $"{Over} && {SupplyWar.VarLespiaGrievance} == true",
                $"{Authority} -= 1;\nRiziaDLC.Diplomacy_Lespia_NegotiatingPower += 2;\n" + Std(),
                new[] { LowAuthority(0), ("Insufficient energy to enact decree", "RiziaDLC.Resources_Energy <= 1") },
                new[] { 0, 0, -1, 0, -1, -1, 0, -1, 1, 0, -1, 1, 1 },
                PwSettleLespia),

            new(SouthernTradeKey, "Economy", EconomyPath, "Redirect Trade Through the South",
                "-2 Authority\n-1 Budget\n+1 Budget Per Turn While the Embargo Lasts",
                "Rizian trading houses are encouraged, with royal guarantees and cheaper port fees, to send the cargoes Valgsland and " +
                "its partners no longer take through Derdian and Morellan ports and on to the markets of the south.\n\n" +
                "The embargo will cost the Kingdom less for as long as it lasts, and Derdia and Morella will profit from the traffic. " +
                "The trade will be slower and dearer than the old routes, and Contana will see in it one more sign that Rizia has " +
                "chosen its camp.",
                Declared,
                $"{Authority} -= 2;\n{Budget} -= 1;\nRiziaDLC.Diplomacy_Derdia_NegotiatingPower += 1;\n" +
                "RiziaDLC.Diplomacy_Morella_NegotiatingPower += 1;\nRiziaDLC.LeaningValue_ProBusiness += 1;\n" + Std(),
                new[] { LowAuthority(1), LowBudget(0) },
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1 },
                PwSouthernTrade,
                Open: () => PwEmbargo),
        });
    }

    // ------------------------------------------------------------------ once-only checks after the war

    /// <summary>The rebuilt harbor opens two turns after the decree.</summary>
    private static void HarborCompletion()
    {
        if (!PwHarborDone || Variables.GetBool(HarborDoneVar))
        {
            return;
        }

        Variables.Set(HarborDoneVar, true);
        NewsWire.Batch(() =>
        {
            News(RebuildHarborKey + "_Done", "RH", "The Harbor of Zille Reopens", NewsWire.P(
                "The first merchant ship to dock in Zille since the war tied up at the rebuilt quays yesterday morning, to the cheers " +
                "of the dockers and the bells of the harbor district. The cranes are new, the warehouses are full, and the old fish " +
                "market has its roof again.",
                "\"Zille has always lived from the sea,\" the harbor master told the Royal Herald. \"Now it can do so again, under " +
                "the Rizian flag.\""));
            News(RebuildHarborKey + "_Done", "G", "Zille Harbor Back in Business", NewsWire.P(
                "ZILLE - The harbor of Zille, wrecked in the fighting between Rizia and Wehlen, has reopened to merchant shipping " +
                "after a reconstruction paid for by the Rizian Crown.",
                "Shipping agents in Monqiz expect part of the northern trade to move to Zille within a year. Wraclawitz has not " +
                "commented."));
        });
    }

    // ------------------------------------------------------------------ what each decree does beyond its vanilla script

    private static void PwCivilAdmin()
    {
        bool legion = Enacted(ZilleanLegionKey);
        if (legion)
        {
            PwAdd("RiziaDLC.Country_PublicOpinion", 1);
        }

        NewsWire.Batch(() =>
        {
            News(CivilAdminKey, "RH", "A Royal Commissioner for Zille", NewsWire.P(
                "The army officers who have governed Zille city since the guns fell silent handed its town hall, courts and police to " +
                "a royal commissioner yesterday, under a decree signed by His Majesty. The curfew has been lifted, and the city " +
                "council will meet next week for the first time since the war.",
                legion
                    ? "The new city police will be recruited from the veterans of the Zillean Legion, who fought for these streets and " +
                      "will now keep them. \"They are Zilleans, and they are the King's men,\" the commissioner said."
                    : "\"Zille was won by soldiers,\" the commissioner told the crowd outside the town hall. \"It will be governed by " +
                      "its citizens, under the Crown.\""));
            if (!FreePressRevoked)
            {
                News(CivilAdminKey, "V", "The Curfew Lifts", NewsWire.P(
                    "For the first time since the guns fell silent, the cafés of the harbor district stayed open after nine last night, and " +
                    "nobody was asked for his papers on the way home.",
                    "Zille remains cut in two by the armistice line, and the families on the far side of it are as far away as ever. " +
                    "But a city run by clerks instead of colonels is a better place to wait for them."));
            }
        });
    }

    private static void PwRebuildHarbor() => NewsWire.Batch(() =>
    {
        News(RebuildHarborKey, "RH", "Work Begins on the Harbor of Zille", NewsWire.P(
            "Engineers of the Royal Navy began clearing the wrecks from the harbor mouth of Zille yesterday, the first step in the " +
            "reconstruction of its quays, cranes and warehouses decreed by His Majesty.",
            "Her Majesty the Queen Mother, who grew up a few streets from the old fish market, has asked to see the plans for its " +
            "restoration herself."));
    });

    private static void PwLoyaltyOath()
    {
        bool lib = Lib;
        NewsWire.Batch(() =>
        {
            News(LoyaltyOathKey, "RH", "Zille's Officials Swear Allegiance", NewsWire.P(
                "Teachers, judges, clerks and policemen lined up at the town halls of Zille yesterday to swear allegiance to the " +
                "Crown of Rizia. Under a royal decree, every official who served the Wehzek administration has until the end of the " +
                "month to take the oath.",
                "\"A man who wants to serve Zille must first be loyal to it,\" a palace spokesman said. In Brenas, flags were hung " +
                "from the windows."));
            News(LoyaltyOathKey, "G", "Wehzek Officials Leave Zille Rather Than Swear", NewsWire.P(
                lib
                    ? "NANTA, RIZIA - Long queues formed at the Nanta ferry this week, as Wehzek officials and their families left " +
                      "Zille rather than swear the loyalty oath demanded by the Rizian Crown."
                    : "ZILLE - Long queues formed at the crossings of the armistice line this week, as Wehzek officials and their " +
                      "families left Zille city rather than swear the loyalty oath demanded by the Rizian Crown.",
                "Among them were schoolteachers, two judges of the city court and most of the harbor police. Wraclawitz radio called " +
                "the oath \"an expulsion by other means.\""));
        });
    }

    private static void PwWehzekSchools() => NewsWire.Batch(() =>
    {
        if (!FreePressRevoked)
        {
            News(WehzekSchoolsKey, "V", "Two Languages in Zille", NewsWire.P(
                "Wehzek children in Zille went back to their schools this week, under a royal decree that guarantees lessons in their " +
                "own language, bilingual courts and street signs in both tongues wherever Wehzeks live.",
                "It is the kind of decree that wins no parades. It may win something better: neighbors who no longer have a reason " +
                "to look north."));
        }

        News(WehzekSchoolsKey, "G", "Rizia Guarantees Wehzek Schools in Zille", NewsWire.P(
            "PORTE DRAZON, RIZIA - The Rizian Crown has guaranteed Wehzek-language schools and bilingual courts in Zille, in a " +
            "decree welcomed by Alliance of Nations observers in the province.",
            "Wehzek state radio dismissed the decree as \"crumbs from an occupier's table.\" Diplomats in Konstantium were more " +
            "generous."));
    });

    private static void PwReleasePrisoners()
    {
        if (Variables.GetString(LateT8.VarPrisoners) == "ransomed")
        {
            Variables.Set(PrisonersRansomedVar, true);
        }

        Variables.Set(LateT8.VarPrisoners, "released");
        NewsWire.Batch(() =>
        {
            News(ReleasePrisonersKey, "G", "Wehzek Prisoners of War Go Home", NewsWire.P(
                "ZILLE - Columns of Wehzek prisoners of war marched north under Alliance of Nations escort this week, released by the " +
                "Rizian Crown without further conditions.",
                "In Wraclawitz, families waited at the railway station with flowers. President Smolak's office thanked the Alliance, " +
                "and did not mention Rizia."));
            News(ReleasePrisonersKey, "RH", "The Camps Are Closed", NewsWire.P(
                "His Majesty has ordered the release of the Wehzek soldiers held in Rizian camps since the war, and the camps are " +
                "being closed.",
                "\"Rizia has nothing to fear from tired men who want to go home,\" a palace spokesman said."));
        });
    }

    private static void PwRansomSoldiers() => NewsWire.Batch(() =>
    {
        News(RansomSoldiersKey, "RH", "Our Soldiers Come Home", NewsWire.P(
            "The first trains carrying Rizian soldiers released from Wehzek camps arrived in Sallabes yesterday, after the Crown paid " +
            "for their freedom through Alliance of Nations intermediaries.",
            "Mothers from Brenas and Montaklar had waited on the platform since dawn. \"The King did not forget them,\" one of them " +
            "told the Royal Herald. \"That is all I will say.\""));
        News(RansomSoldiersKey, "G", "Rizia Pays for Its Prisoners", NewsWire.P(
            "WRACLAWITZ, WEHLEN - Wehlen has released the Rizian soldiers captured in Zille, after the Rizian government paid an " +
            "undisclosed sum through Alliance of Nations intermediaries.",
            "\"Rizia has paid for its war twice,\" President Wiktor Smolak told a rally in Wraclawitz. \"Once in Zille, and once " +
            "at the cashier's window.\""));
    });

    private static void PwCourtMartial() => NewsWire.Batch(() =>
    {
        News(CourtMartialKey, "RH", "Zille Commanders to Face Court-Martial", NewsWire.P(
            "The generals who commanded the failed operations in Zille were relieved of their commands yesterday. They will stand " +
            "trial before a court-martial in Porte Drazon, charged with negligence in the face of the enemy.",
            "\"The Crown asked for victory and was given excuses,\" a palace spokesman said. \"Those responsible will answer for it.\""));
        if (!FreePressRevoked)
        {
            News(CourtMartialKey, "V", "Who Sent Them?", NewsWire.P(
                "The court-martial ordered yesterday will put a handful of generals in the dock for the failure in Zille. Some of them " +
                "may well deserve it.",
                "But the war was declared in Konstantium, and no general signed that declaration. The Voice of Rizia notes that the " +
                "dock in Porte Drazon has room for only so many."));
        }
    });

    private static void PwInquiry()
    {
        if (WorldSystems.PalesWar && !Variables.GetBool("RiziaDLC.War_Pales_Won") && !Variables.GetBool("RiziaDLC.War_Pales_Loss"))
        {
            PwAdd("RiziaDLC.War_Pales_RizianWarSupport", 1);
        }

        NewsWire.Batch(() =>
        {
            News(InquiryKey, "RH", "Royal Commission to Examine the Zille Campaign", NewsWire.P(
                "His Majesty has appointed a royal commission of judges, retired officers and Delegates to examine the conduct of " +
                "the campaign in Zille, from the declaration in Konstantium to the last day of the fighting.",
                "\"The King accepts his share of the responsibility,\" a palace spokesman said. \"The commission will establish the " +
                "rest.\""));
            if (!FreePressRevoked)
            {
                News(InquiryKey, "V", "The King Takes His Share", NewsWire.P(
                    "It is not often that a crown admits a mistake. This week, ours did, and appointed a commission that will report " +
                    "in public rather than to the General Staff.",
                    "Its report will make painful reading. The Voice of Rizia hopes it will be read, by the soldiers who came back " +
                    "from Zille and by whoever plans the next war."));
            }
        });
    }

    private static void PwSuspendReparations()
    {
        Variables.Set(WorldModifiers.WehlenReparations, 0);
        Variables.Set(WorldModifiers.WehlenReparations + "_Turns", 0);
        NewsWire.Batch(() =>
        {
            News(SuspendReparationsKey, "RH", "Not Another Guilder for Wraclawitz", NewsWire.P(
                "By royal decree, the Crown has suspended the installments it agreed to pay Wehlen under the Terms of Wraclawitz. " +
                "\"A treaty dictated under the guns of an occupying army does not bind a free kingdom,\" a palace spokesman said.",
                "In Brenas, crowds gathered outside the town halls to cheer the news."));
            News(SuspendReparationsKey, "G", "Rizia Stops Paying Wehlen", NewsWire.P(
                "WRACLAWITZ, WEHLEN - Rizia has stopped paying the reparations it agreed to under the Terms of Wraclawitz. Wehzek " +
                "state radio called the decree \"the act of a debtor who signs his name and then denies it.\"",
                "Diplomats in Konstantium said the decision would make it harder for Rizia to find friends at the Alliance. In " +
                "Thornbourgh and Cavette, governments that hold Rizian debts of their own were said to be reading the decree closely."));
        });
    }

    private static void PwRepayRumburg()
    {
        Variables.Set(WorldModifiers.RumburgRepayment, 0);
        Variables.Set(WorldModifiers.RumburgRepayment + "_Turns", 0);
        NewsWire.Batch(() =>
        {
            News(RepayRumburgKey, "RH", "Rizia Pays Its Debts", NewsWire.P(
                "The Royal Treasury has repaid in a single sum what remained of the war credits extended by Queen Beatrice's " +
                "government during the Zille campaign, years ahead of schedule.",
                "\"Rizia is grateful to its friends,\" a palace spokesman said. \"It does not intend to remain in their debt.\""));
            News(RepayRumburgKey, "G", "Rizia Clears Its Rumburgian Debt Early", NewsWire.P(
                "THORNBOURGH, RUMBURG - Rizia has repaid its wartime credits from Rumburg in full, the Rumburgian treasury confirmed " +
                "this week.",
                "Observers in Thornbourgh noted that the early repayment leaves Porte Drazon with one obligation fewer to its " +
                "powerful neighbor, and Queen Beatrice's court with one lever fewer."));
        });
    }

    private static void PwSettleLespia()
    {
        // Lespia's support for Pales is decided in the Turn 6 briefing (PalesIncidentPress); a settlement after it cuts it back
        if (Variables.GetInt("RiziaDLC.War_Pales_LespianSupport") > 0 && !Variables.GetBool("RiziaDLC.War_Pales_Won") &&
            !Variables.GetBool("RiziaDLC.War_Pales_Loss"))
        {
            PwAdd("RiziaDLC.War_Pales_LespianSupport", -1);
        }

        NewsWire.Batch(() =>
        {
            News(SettleLespiaKey, "G", "Lespia Accepts Rizian Gas Settlement", NewsWire.P(
                "PERLA, LESPIA - Prime Minister Patricio Alvarez has accepted a Rizian offer of discounted Aureus gas for LesPower " +
                "over three years, closing a quarrel that began during the war in Zille.",
                "\"Lespia remembers who keeps their word,\" Alvarez told reporters. \"It also remembers who makes amends.\""));
            if (WorldSystems.LespiaExposed && !FreePressRevoked)
            {
                News(SettleLespiaKey, "V", "Paying Perla", NewsWire.P(
                    "The Crown has agreed to sell Lespia gas below cost for three years, with the King's personal regrets for promises " +
                    "that were not kept.",
                    "The Friendship Day bombing in Zille was organized by an officer of the Lespian consulate. We would like to know " +
                    "who will send regrets to the families of its victims."));
            }
        });
    }

    private static void PwSouthernTrade() => NewsWire.Batch(() =>
    {
        News(SouthernTradeKey, "RH", "New Routes for Rizian Trade", NewsWire.P(
            "Rizian trading houses have begun sending their cargoes south through Derdian and Morellan ports, under a royal decree " +
            "that offers Crown guarantees and lower port fees to merchants hit by the Valgish embargo.",
            "\"Valgsland may close its doors,\" a palace spokesman said. \"The world has other doors.\""));
        News(SouthernTradeKey, "G", "Rizian Trade Turns South", NewsWire.P(
            "PORTE DRAZON, RIZIA - Derdian and Morellan ports report a sharp rise in Rizian cargoes this month, as Rizian merchants " +
            "route around the embargo imposed by Valgsland and its partners.",
            "Shipping agents say the southern route adds days and costs to every voyage. In Contana, officials said the embargo " +
            "would stay in place for as long as Rizia refused to make peace with Wehlen."));
    });
}
