using SuzerainModdingKit;
using static ZilleWar.Talk;

namespace ZilleWar;

/// <summary>
/// Vanilla Turn 7 adjusted for the Zille war. By Turn 7 the war may still be fought, or be over (liberated,
/// lost or ceasefired), and the vanilla Pales war may be running at the same time.
/// - This file: shared Lua conditions, the House of Delegates (c384 and its sub-conversations c385-c388),
///   the Wehzek medicine decision and the Turn 7 newspapers.
/// - EarlyT7Diplomacy: Rumburg (c389), Duke Reinhart (c393), Derdia (c377, c380-c383, c378/c379).
/// - EarlyT7Home: the security councilor (c396), Rico (c398, c399), Hugo and Manus (c395), dinner (c394), Pabel (c392).
/// Variables others can read: ZilleWar.E7_ZilleAdmin ("brenas" | "imperii" | "undecided"),
/// ZilleWar.E7_RumburgPriority ("zille" | "pales" | "both"), ZilleWar.E7_RumburgWarned, ZilleWar.E7_RumburgPromise,
/// ZilleWar.E7_ZilleGold ("rumburg" | "market" | "later"), ZilleWar.E7_ReferendumFloated.
/// </summary>
internal static class EarlyT7
{
    // ------------------------------------------------------------------ shared Lua

    public const string VarZilleAdmin = "ZilleWar.E7_ZilleAdmin";
    public const string VarRumburgPriority = "ZilleWar.E7_RumburgPriority";
    public const string VarRumburgWarned = "ZilleWar.E7_RumburgWarned";
    public const string VarRumburgPromise = "ZilleWar.E7_RumburgPromise";
    public const string VarZilleGold = "ZilleWar.E7_ZilleGold";
    public const string VarReferendumFloated = "ZilleWar.E7_ReferendumFloated";
    private const string VarNewsDerdiaG = "ZilleWar.E7_News_DerdiaG";
    private const string VarNewsDerdiaV = "ZilleWar.E7_News_DerdiaV";
    private const string VarNewsWehlenNorth = "ZilleWar.E7_News_WehlenNorth";
    private const string VarNewsWarTax = "ZilleWar.E7_News_WarTax";
    private const string VarNewsLespiaBothSides = "ZilleWar.E7_News_LespiaBothSides";
    // The King's answer on who governs Zille, as bools for the House recompute rows (a divided Zille; a liberated one moves
    // vanilla's own ZilleOwnership flags, LateState).
    public const string VarAdminBrenas = "ZilleWar.E7_ZilleAdmin_Brenas";
    public const string VarAdminImperii = "ZilleWar.E7_ZilleAdmin_Imperii";
    /// <summary>The war profits tax was signed before any war in Pales: it was a Zille war tax, and the Voice's Pales-worded
    /// stories about it stay hidden or rewritten even after the Pales war starts.</summary>
    private const string VarWarTaxForZille = "ZilleWar.E7_WarTaxForZille";

    public static readonly string Declared = WarFlags.LDeclared;
    public static readonly string Peace = WarFlags.LPeace;
    public static readonly string Act = WarFlags.LActive;
    public static readonly string Lib = WarFlags.L(WarFlags.Liberated);
    public static readonly string Def = WarFlags.L(WarFlags.Defeated);
    public static readonly string Cf = $"{WarFlags.L(WarFlags.Ceasefire)} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.Defeated)}";
    public static readonly string Over = WarFlags.L(WarFlags.Over);
    public static readonly string Two = WarFlags.L(WarFlags.TwoWars);
    // After the war (always the case from Turn 7 on): liberated / divided (Zille city ours, the rest Wehzek) / lost,
    // each either by the King's own ceasefire or by the armistice when the offensive ran out.
    public static readonly string Part = LK.Part;
    public static readonly string Lost = LK.Lost;
    public static readonly string KingCf = WarFlags.L(WarFlags.Ceasefire);
    public static readonly string NoKingCf = WarFlags.LNot(WarFlags.Ceasefire);
    public static readonly string NoPales = WarFlags.LNot("RiziaDLC.Pales_War");
    /// <summary>The Zille war is being fought and the Pales war is not: vanilla's generic war lines apply to Zille.</summary>
    public static readonly string ZilleOnly = $"{Act} and {NoPales}";
    public static readonly string Rpp = "Variable[\"RiziaDLC.HouseElections_RPPWinsSeats\"] == true";
    public static readonly string NotRpp = "Variable[\"RiziaDLC.HouseElections_RPPWinsSeats\"] ~= true";

    public static string Num(string v) => $"({V(v)} or 0)";
    public static string Inc(string v, int d) => $"{V(v)} = {Num(v)} + ({d}); ";
    public static string Str(string v, string value) => $"{V(v)} = \"{value}\"; ";

    /// <summary>Vanilla's "Pales_War == true" branch also plays under <paramref name="cond"/>: a logic node off the
    /// branching entry <paramref name="hub"/> that jumps to the war line <paramref name="target"/>.</summary>
    public static void Widen(Talk t, string name, int hub, int target, string cond) =>
        t.Logic(name, new[] { "#" + target }, condition: cond, hooks: From(hub));

    // ------------------------------------------------------------------ registration

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarZilleAdmin, VarRumburgPriority, VarRumburgWarned, VarRumburgPromise, VarZilleGold, VarReferendumFloated,
                     VarNewsDerdiaG, VarNewsDerdiaV, VarNewsWehlenNorth, VarNewsWarTax, VarNewsLespiaBothSides,
                     VarRepBluds, VarRepDerdians, VarAdminBrenas, VarAdminImperii, VarWarTaxForZille,
                 })
        {
            Variables.Register(v);
        }

        // Who governs a divided Zille moves the Houses for good: vanilla recomputes House standing after every fragment, so a
        // one-off change in the debate's script would be gone by the next scene.
        Vanilla.Expose(VarAdminBrenas);
        Vanilla.Expose(VarAdminImperii);
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarAdminBrenas} == true && {LateState.VarDivided} == true",
            "RiziaDLC.Country_HouseSazon += 1;\nRiziaDLC.Country_HouseToras -= 1;");
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarAdminImperii} == true && {LateState.VarDivided} == true",
            "RiziaDLC.Country_HouseSazon -= 1;");

        // Wehzek medicine for an army fighting Wehlen: never while or after the war.
        Vanilla.Gate("Turn07_Decision_War_Both_WehlenDrugs", WarFlags.XPeace);

        RegisterHouseOfDelegates();
        RegisterPalesWarUpdate();
        RegisterZilleDebate();
        RegisterPublicOpinion();
        RegisterReligion();
        RegisterNews();

        EarlyT7Diplomacy.Register();
        EarlyT7Home.Register();
    }

    // ------------------------------------------------------------------ c384 House of Delegates II

    private static void RegisterHouseOfDelegates()
    {
        var t = new Talk("Rizia/Turn 7/A_HouseOfDelegatesII", "ZilleWar.E7.HoD.");
        // Generic war lines (Hugo's "shutter the House while the war is ongoing", "this turbulent time", Sal's prayer for
        // the troops, the King's "we are at war", "God bless our troops") also play during the Zille war.
        Widen(t, "HugoGrim", 17, 16, ZilleOnly);
        Widen(t, "ManusTurbulent", 89, 88, ZilleOnly);
        Widen(t, "DariaTurbulent", 101, 100, ZilleOnly);
        Widen(t, "SalPrayer", 115, 118, ZilleOnly);
        Widen(t, "WelfareWar", 184, 183, ZilleOnly);
        Widen(t, "RemindWar", 188, 187, ZilleOnly);
        Widen(t, "BlessTroops", 203, 206, ZilleOnly);
        Widen(t, "RemindWarD", 243, 256, ZilleOnly);
        Widen(t, "WelfareWarD", 251, 248, ZilleOnly);
        Widen(t, "BlessTroopsD", 273, 268, ZilleOnly);
        // After the Zille war (no Pales war): Sal's opening prayer remembers the fallen.
        t.Variant("SalFallen", 119, new[] { 117 }, $"{Over} and {NoPales}", "Sal Ignacius",
            "\"We pray for the souls of those who fell in Zille, and for the wisdom to choose paths that lead to prosperity and peace for our kingdom.\"",
            new[] { "#120" });
        t.Register();
    }

    // ------------------------------------------------------------------ c385 Pales war update and tax (both wars, or after Zille)

    private static void RegisterPalesWarUpdate()
    {
        const string conv = "Rizia/Turn 7/Pales War Update And Tax";
        var t = new Talk(conv, "ZilleWar.E7.PalesTax.");
        t.Variant("Doubt18Act", 18, new[] { 17, 19, 23 }, Act, "Manus Sazon",
            "\"It is difficult for my party to support a second war, Your Majesty, when the first one is still being fought in Zille.\"",
            new[] { "#11" });
        t.Variant("Doubt18Lib", 18, new[] { 17, 19, 23 }, Lib, "Manus Sazon",
            "\"Zille has come home, and Brenas thanks the army for it. But it is difficult for my party to support another war so soon, this time against the Grand Duchy.\"",
            new[] { "#11" });
        t.Variant("Doubt18Part", 18, new[] { 17, 19, 23 }, Part, "Manus Sazon",
            "\"Zille's harbor flies our flag again, and Brenas thanks the army for it. But part of the region is still behind Wehzek guns, and it is difficult for my party to support another war, this time against the Grand Duchy.\"",
            new[] { "#11" });
        t.Variant("Doubt18Lost", 18, new[] { 17, 19, 23 }, Lost, "Manus Sazon",
            "\"We have only just failed to bring Zille home by force. It is difficult for my party to support another war, this time against the Grand Duchy.\"",
            new[] { "#11" });
        t.Variant("Doubt53Act", 53, new[] { 51, 52 }, Act, "Manus Sazon",
            "\"It is difficult for the RPP to support an all-out attack on the Grand Duchy while our soldiers are still fighting in Zille.\"",
            new[] { "#54" });
        t.Variant("Doubt53Over", 53, new[] { 51, 52 }, Over, "Manus Sazon",
            "\"It is difficult for the RPP to support an all-out attack on the Grand Duchy. We have barely finished counting the dead from Zille.\"",
            new[] { "#54" });
        t.Variant("Tax41", 41, new[] { 33 }, Act, "Manus Sazon",
            "\"In the name of equity, the RPP proposes an excess profits tax on the industries that profit most from the campaigns in Pales and Zille.\"",
            new[] { "#34" });
        t.Register();
    }

    // ------------------------------------------------------------------ c386 Zille Treaty Discussion -> the Zille war debate

    private static void RegisterZilleDebate()
    {
        const string conv = "Rizia/Turn 7/Zille Treaty Discussion";
        const string manus = "Manus Sazon";
        const string daria = "Daria de Rava";
        const string hugo = "Hugo Toras";
        var t = new Talk(conv, "ZilleWar.E7.Zille.");

        // The treaty, the probation period and the "peaceful transfer" no longer exist: the House debates the war.
        // From Turn 7 the war is over: liberated, divided (Zille city ours, the rest behind an armistice or ceasefire line)
        // or lost. "Act" is kept for safety only.
        t.Logic("Start", new[] { "Act", "Lib", "Part", "Def", "Cf", "#3" }, condition: Declared, hooks: From(1));

        // --- the war goes on (cannot happen from Turn 7 on)
        t.Logic("Act", new[] { "ActM", "ActD" }, condition: Act);
        t.Line("ActM", manus,
            "\"My constituents in Brenas have sons in Zille, Your Majesty. Many of them are fighting over fields their grandfathers farmed " +
            "before 1927. They ask this House two questions. When will this war end, and who will govern Zille once it does?\"",
            new[] { "ActM2" }, condition: Rpp);
        t.Line("ActM2", daria,
            "\"The Rizian National Coalition stands behind every soldier in Zille, Madame Speaker. We ask only that the Crown not give away " +
            "at a negotiating table what our army is winning in the field.\"", new[] { "ActSO" });
        t.Line("ActD", daria,
            "\"The Rizian National Coalition stands behind every soldier in Zille, Your Majesty. Our only request is that the Crown not " +
            "give away at a negotiating table what our army is winning in the field.\"", new[] { "ActD2" }, condition: NotRpp);
        t.Line("ActD2", manus,
            "\"Nobody in this chamber wants to give anything away, Madame Speaker. But my constituents in Brenas have sons in Zille. " +
            "They want to know when this war ends, and who will govern Zille once it does.\"", new[] { "ActSO" });
        t.Logic("ActSO", new[] { "ActSO1", "ActAnswer" });
        t.Line("ActSO1", manus,
            "\"There are also reports of Su Omina irregulars fighting alongside the Royal Army. If they settle old scores with Wehzek " +
            "civilians, it will be the Crown's name on the graves.\"", new[] { "ActSO2" },
            condition: $"{Num("RiziaDLC.Zille_ViolentResistancePower")} > 0");
        t.Line("ActSO2", daria,
            "\"Su Omina's men know the streets of Zille better than any general, Mr. Sazon. I would rather they fought for us than against us.\"",
            new[] { "ActAnswer" });
        t.Logic("ActAnswer", new[] { "A1", "A2", "A3" });
        t.Choice("A1", "\"The army will finish what it started. That is the only timetable I can give this House.\"", new[] { "A1r" });
        t.Line("A1r", Narrator, "Daria nodded firmly. Manus said nothing, but he wrote something down.", new[] { "Tax" });
        t.Choice("A2", "\"Every soldier will be home as soon as Zille is ours. I promise this House that much.\"", new[] { "A2r" });
        t.Line("A2r", Narrator, "A murmur of approval ran along the benches. On the RPP side, a few Delegates exchanged doubtful looks.",
            new[] { "Tax" });
        t.Choice("A3", "\"The conduct of this war is a matter for the Crown and the General Staff. It is not up for debate.\"", new[] { "A3r" });
        t.Line("A3r", Narrator, "The chamber fell quiet. Manus held my gaze for a moment before looking down at his binder.", new[] { "Tax" });

        // The RPP's war profits tax, the Zille version of vanilla's Pales proposal (c385 covers it when Pales is at war).
        string taxOpen = $"{NoPales} and Variable[\"RiziaDLC.Policy_RD_Economy_WarProfitsTax\"] ~= true and " +
                         "Variable[\"RiziaDLC.Enable_Policy_RD_Economy_WarProfitsTax\"] ~= true";
        t.Logic("Tax", new[] { "Tax1", "Admin" });
        t.Line("Tax1", manus,
            "\"One more matter, Your Majesty. War is a source of suffering for many and of enormous windfalls for a few. The RPP proposes " +
            "an excess profits tax on the industries that stand to gain the most from the Zille campaign.\"", new[] { "Tax2" }, condition: taxOpen);
        t.Line("Tax2", daria,
            "\"These are the same factories that armed our soldiers, Mr. Sazon. They should be thanked, not punished.\"", new[] { "Tax3" });
        t.Line("Tax3", hugo,
            "\"Your Majesty, a tax like that would fall hardest on House Azaro, and on the de Ravas themselves. Tread carefully.\"",
            new[] { "TaxYes", "TaxDesk", "TaxNo" });
        t.Choice("TaxYes", "\"Rizia will implement the war profits tax.\"", new[] { "TaxYesR" },
            condition: $"{Num("RiziaDLC.Resources_Authority")} >= 3", cost: "-3 Authority, +3 Budget, +2 Budget Per Turn",
            script: "EnactDecree(\"Economy_Taxes_WarProfitsTax\");");
        t.Line("TaxYesR", Narrator, "Manus bowed his head. Across the aisle, Daria's pen stopped moving.", new[] { "Admin" });
        t.Choice("TaxDesk", "\"Send the proposal to my desk. I will decide on it later.\"", new[] { "TaxDeskR" },
            script: Set("RiziaDLC.Enable_Policy_RD_Economy_WarProfitsTax", "true"));
        t.Line("TaxDeskR", manus, "\"Thank you, Your Majesty. It will be there by morning.\"", new[] { "Admin" });
        t.Choice("TaxNo", "\"Not while our soldiers depend on those factories.\"", new[] { "TaxNoR" });
        t.Line("TaxNoR", daria, "\"Thank you, Your Majesty. The House will make a note of it.\"", new[] { "Admin" });

        // --- who governs Zille (the whole region, or the part that is ours): vanilla's Brenas or Rizia Imperii argument
        t.Logic("Admin", new[] { "Adm1Lib", "Adm1Part", "Adm1Act", "AdmLost" });
        t.Logic("AdmLost", new[] { "#191" }, condition: Lost);
        t.Line("Adm1Act", daria,
            "\"Which brings us to what comes after. Once Zille is liberated, it should be administered from Porte Drazon, the way the " +
            "Pales Administrative District is. Stability first.\"", new[] { "Adm2" }, condition: Act);
        t.Line("Adm1Lib", daria,
            "\"Which brings us to what comes next. Zille has been through a war. It should be administered from Porte Drazon, the way " +
            "the Pales Administrative District is, until it is stable.\"", new[] { "Adm2" }, condition: Lib);
        t.Line("Adm1Part", daria,
            "\"Which brings us to the part of Zille that is ours. A city on an armistice line should be administered from Porte Drazon, " +
            "the way the Pales Administrative District is, until that line is settled.\"", new[] { "Adm2Part" }, condition: Part);
        t.Line("Adm2", manus,
            "\"Objection. Zille has always been part of the province of Brenas. Brenas paid for this war with its sons. Brenas should " +
            "govern what they won back.\"", new[] { "Adm3" });
        // Divided: the question is only about the city of Zille and the strip we hold (it feeds E7_ZilleAdmin for our own
        // lines and the divided map layer; LateState sets vanilla ZilleOwnership_* only for a liberated Zille).
        t.Line("Adm2Part", manus,
            "\"Objection. The city of Zille has always been part of the province of Brenas, line or no line. Brenas paid for it with its " +
            "sons. Brenas should govern what they won back.\"", new[] { "Adm3" });
        t.Line("Adm3", Narrator, "The volume in the room grew as the Delegates began arguing among themselves. I turned to Hugo.", new[] { "Adm4" });
        t.Line("Adm4", hugo,
            "\"You have empowered the Sazons enough without handing them a city, Your Majesty. Whatever you decide, decide it " +
            "before the generals decide it for you.\"", new[] { "AdmB", "AdmBPart", "AdmI", "AdmIPart", "AdmW", "AdmWLib", "AdmWPart" });
        string notPart = $"not ({Part})";
        t.Choice("AdmBPart", "\"The city of Zille and the land we hold will return to Brenas, as before 1927.\"", new[] { "AdmBR" },
            condition: Part,
            script: Str(VarZilleAdmin, "brenas") + Set(VarAdminBrenas, "true") + Set(VarAdminImperii, "false"));
        t.Choice("AdmIPart", "\"What we hold of Zille will be governed from Porte Drazon until the line is settled.\"", new[] { "AdmIR" },
            condition: Part, script: Str(VarZilleAdmin, "imperii") + Set(VarAdminImperii, "true") + Set(VarAdminBrenas, "false"));
        t.Choice("AdmB", "\"Zille will return to Brenas, as it was before 1927.\"", new[] { "AdmBR" }, condition: notPart,
            script: Str(VarZilleAdmin, "brenas") + Set(VarAdminBrenas, "true") + Set(VarAdminImperii, "false"));
        t.Line("AdmBR", Narrator, "The RPP benches broke into applause. Beside me, Hugo closed his eyes.", new[] { "#191" });
        t.Choice("AdmI", "\"Zille will be governed from Porte Drazon until it is secure.\"", new[] { "AdmIR" }, condition: notPart,
            script: Str(VarZilleAdmin, "imperii") + Set(VarAdminImperii, "true") + Set(VarAdminBrenas, "false"));
        t.Line("AdmIR", Narrator, "Daria smiled. Manus gripped the edges of his podium until his knuckles went white.", new[] { "#191" });
        t.Choice("AdmW", "\"That can wait until the war is truly behind us.\"", new[] { "AdmWR" }, condition: Act,
            script: Str(VarZilleAdmin, "undecided"));
        t.Choice("AdmWLib", "\"That can wait until Zille is back on its feet.\"", new[] { "AdmWR" }, condition: Lib,
            script: Str(VarZilleAdmin, "undecided"));
        t.Choice("AdmWPart", "\"That can wait until we know where the line will stay.\"", new[] { "AdmWR" }, condition: Part,
            script: Str(VarZilleAdmin, "undecided"));
        t.Line("AdmWR", hugo, "\"Sooner rather than later, one hopes.\"", new[] { "#191" });

        // --- liberated
        t.Logic("Lib", new[] { "LibM", "LibD" }, condition: Lib);
        t.Line("LibM", manus,
            "\"The first matter on our agenda is a happy one. On behalf of the people of Brenas, this House congratulates His Majesty and " +
            "the Royal Army. After a quarter of a century, Zille is Rizian again.\"", new[] { "LibM2" }, condition: Rpp);
        t.Line("LibM2", daria, "\"The RNC never doubted it for a moment, Madame Speaker.\"", new[] { "LibQ" });
        t.Line("LibD", daria,
            "\"The first matter on our agenda is a happy one. This House congratulates His Majesty and the Royal Army on the liberation " +
            "of Zille. The RNC never doubted it for a moment.\"", new[] { "LibD2" }, condition: NotRpp);
        t.Line("LibD2", manus, "\"Neither did Brenas, Madame Speaker. Brenas paid for it.\"", new[] { "LibQ" });
        t.Line("LibQ", manus,
            "\"But victory brings its own questions. Thousands of Wehzeks live in Zille and Nanta. They did not choose Smolak's war any " +
            "more than our soldiers did. My party asks for guarantees that they will be treated as subjects of the Crown, not as prisoners.\"",
            new[] { "LibQ2" });
        t.Line("LibQ2", daria,
            "\"Guarantees can wait until the collaborators have been dealt with, Mr. Sazon. Smolak's police never asked our people for " +
            "their opinion.\"", new[] { "LibA1", "LibA2" });
        t.Choice("LibA1", "\"Every Wehzek in Zille who lays down his arms is a subject of the Crown. You have my word.\"", new[] { "LibA1r" },
            script: Inc("RiziaDLC.LeaningValue_Democratic", 1));
        t.Line("LibA1r", Narrator, "Manus inclined his head. Daria's smile thinned.", new[] { "Tax" });
        t.Choice("LibA2", "\"Those who served Smolak will answer for it. The rest have nothing to fear.\"", new[] { "LibA2r" });
        t.Line("LibA2r", Narrator, "Applause rose from the RNC benches. Manus wrote something down.", new[] { "Tax" });

        // --- divided: Zille city is ours; the cities Wehlen still holds are named (any of Auremina, Isor and Nanta)
        var parts = new List<string>();
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) =>
        {
            parts.Add("Part" + id);
            t.Logic("Part" + id, new[] { "PartCfM" + id, "PartCfDT" + id, "PartCfD" + id, "PartArmM" + id, "PartArmD" + id }, condition: cond);
            // A ceasefire on Turns 4-5 brings the treaty before this debate: the King may already have signed the line as a border.
            t.Line("PartCfM" + id, manus,
                "\"Your Majesty, Zille's harbor flies our flag again, and Brenas is grateful to the army for it. But the ceasefire has left a " +
                $"line across the region. {theirs} {be} still Wehzek, and our constituents have cousins on the far side of it.\"",
                new[] { "PartCfM2T" + id, "PartCfM2" + id }, condition: $"{Rpp} and {KingCf}");
            t.Line("PartCfM2T" + id, daria,
                "\"The RNC accepted the ceasefire because His Majesty agreed to it, Madame Speaker. Now he has signed that line into a border, " +
                "and the RNC will not forget it.\"", new[] { "PartRef" + id }, condition: LK.BorderTreaty);
            t.Line("PartCfM2" + id, daria,
                "\"The RNC accepted the ceasefire because His Majesty agreed to it, Madame Speaker. We will not accept that line as a border, nor " +
                "one drawn in Konstantium by people who have never set foot in Zille.\"", new[] { "PartRef" + id });
            t.Line("PartCfDT" + id, daria,
                "\"Your Majesty, Zille's harbor flies our flag again. The RNC accepted the ceasefire because you agreed to it. But you have " +
                "since signed that line into a border, and the RNC will not forget it.\"",
                new[] { "PartCfD2T" + id }, condition: $"{NotRpp} and {KingCf} and {LK.BorderTreaty}");
            t.Line("PartCfD2T" + id, manus,
                $"\"A border is a border, Madame Speaker. {theirs} {be} Wehzek by treaty now, and our constituents have cousins on the far " +
                "side of it.\"", new[] { "PartRef" + id });
            t.Line("PartCfD" + id, daria,
                "\"Your Majesty, Zille's harbor flies our flag again. The RNC accepted the ceasefire because you agreed to it. But we will not " +
                "accept that line as a border, nor one drawn in Konstantium by people who have never set foot in Zille.\"",
                new[] { "PartCfD2" + id }, condition: $"{NotRpp} and {KingCf}");
            t.Line("PartCfD2" + id, manus,
                $"\"A ceasefire is not a peace, Madame Speaker. {theirs} {be} still Wehzek, and our constituents have cousins on the far " +
                "side of that line.\"", new[] { "PartRef" + id });
            t.Line("PartArmM" + id, manus,
                "\"Your Majesty, Zille's harbor flies our flag again, and Brenas is grateful to the army for it. But the offensive stopped " +
                $"short of {theirs}, and the armistice has frozen the line where our soldiers stood when their strength ran out.\"",
                new[] { "PartArmM2" + id }, condition: $"{Rpp} and {NoKingCf}");
            t.Line("PartArmM2" + id, daria,
                "\"The RNC will never accept that line as a border, Madame Speaker. The army took Zille's harbor. Given time, it can finish the work.\"",
                new[] { "PartRef" + id });
            t.Line("PartArmD" + id, daria,
                "\"Your Majesty, Zille's harbor flies our flag again, and the RNC salutes the army for it. But we will never accept the " +
                "armistice line as a border. Given time, the army can finish the work.\"", new[] { "PartArmD2" + id }, condition: $"{NotRpp} and {NoKingCf}");
            t.Line("PartArmD2" + id, manus,
                $"\"Finish it with whose sons, Madame Speaker? The offensive ran out short of {theirs}, and Brenas has buried enough of " +
                "them already.\"", new[] { "PartRef" + id });
            t.Line("PartRef" + id, manus,
                $"\"Then let the people on the other side of the line decide where they belong. A referendum in {theirs}, under " +
                "international supervision.\"", new[] { "PartH" });
        });
        t.Logic("Part", parts.ToArray(), condition: Part);

        t.Line("PartH", hugo,
            "\"Don't go down that road, Your Majesty. Any vote across that line is an open door for Wehlen, and Smolak will walk through it.\"",
            new[] { "CfA1", "CfA2" });
        t.Choice("CfA1", "\"Zille's future will be decided by Rizia, not by ballots.\"", new[] { "CfA1r" });
        t.Line("CfA1r", daria, "\"Hear, hear.\"", new[] { "Tax" });
        t.Choice("CfA2", "\"Perhaps a vote is the only way this ends without more graves.\"", new[] { "CfA2r" },
            script: Set(VarReferendumFloated, "true"));
        t.Line("CfA2r", Narrator, "Manus looked at me in surprise. Hugo did not look at me at all.", new[] { "Tax" });

        // --- lost, after the offensive ran out (armistice)
        t.Logic("Def", new[] { "DefM", "DefD" }, condition: $"{Lost} and {NoKingCf}");
        t.Line("DefM", manus,
            "\"Your Majesty, I will not pretend this is an easy session. We were promised a short war and a homecoming. Brenas has " +
            "received neither. Only telegrams.\"", new[] { "DefM2" }, condition: Rpp);
        t.Line("DefM2", daria,
            "\"The RNC will never abandon the claim to Zille, Madame Speaker. But the people want to know who failed them. The army, or " +
            "its orders.\"", new[] { "DefA1", "DefA2", "DefA3" });
        t.Line("DefD", daria,
            "\"Your Majesty, the RNC will never abandon the claim to Zille. But the people want to know who failed them. The army, or " +
            "its orders.\"", new[] { "DefD2" }, condition: NotRpp);
        t.Line("DefD2", manus,
            "\"We were promised a short war and a homecoming, Madame Speaker. Brenas has received neither. Only telegrams.\"",
            new[] { "DefA1", "DefA2", "DefA3" });
        t.Choice("DefA1", "\"The failure is mine to carry. Not the army's.\"", new[] { "DefA1r" },
            script: Inc("RiziaDLC.LeaningValue_Democratic", 1));
        t.Line("DefA1r", Narrator, "The chamber went silent. Even Daria lowered her eyes.", new[] { "DefH" });
        t.Choice("DefA2", "\"Zille is not lost. We lost a campaign, not the claim.\"", new[] { "DefA2r" });
        t.Line("DefA2r", daria, "\"Then the RNC will hold you to that, Your Majesty.\"", new[] { "DefH" });
        t.Choice("DefA3", "\"This House will not put the Crown on trial.\"", new[] { "DefA3r" },
            script: Inc("RiziaDLC.LeaningValue_Democratic", -1));
        t.Line("DefA3r", Narrator, "Manus opened his mouth, then closed it again. Beside me, Hugo gave a small nod of approval.", new[] { "DefH" });
        t.Line("DefH", hugo, "\"Let them grieve, Romus. Just don't let them choose a culprit.\"", new[] { "Tax" });

        // --- lost, after the King's own ceasefire (Zille city never taken)
        // A ceasefire on Turns 4-5 brings the treaty before this debate: the King may already have recognized Wehzek Zille.
        string recognized = $"{V(ZilleTreaty.VarKind)} == \"recognized\"";
        t.Logic("Cf", new[] { "CfM", "CfDR", "CfD" }, condition: $"{Lost} and {KingCf}");
        t.Line("CfM", manus,
            "\"Your Majesty, the guns have fallen silent, and my party is grateful for it. But Brenas buried its sons for a city that " +
            "still flies Smolak's flag. Our constituents want to know what it was for.\"", new[] { "CfM2R", "CfM2" }, condition: Rpp);
        t.Line("CfM2R", daria,
            "\"The RNC accepted the ceasefire because His Majesty agreed to it, Madame Speaker. We never agreed to the paper he signed in " +
            "Wraclawitz.\"", new[] { "DefA1", "CfSigned", "DefA3" }, condition: recognized);
        t.Line("CfM2", daria,
            "\"The RNC accepted the ceasefire because His Majesty agreed to it, Madame Speaker. We did not accept it as the end of the claim.\"",
            new[] { "DefA1", "CfTime", "DefA3" });
        t.Line("CfDR", daria,
            "\"Your Majesty, the RNC accepted the ceasefire because you agreed to it. It never agreed to the paper you signed in Wraclawitz.\"",
            new[] { "CfD2R" }, condition: $"{NotRpp} and {recognized}");
        t.Line("CfD2R", manus,
            "\"And Brenas buried its sons for a city the Crown has now signed away, Madame Speaker. Our constituents want to know what " +
            "it was for.\"", new[] { "DefA1", "CfSigned", "DefA3" });
        t.Line("CfD", daria,
            "\"Your Majesty, the RNC accepted the ceasefire because you agreed to it. We did not accept it as the end of the claim to Zille.\"",
            new[] { "CfD2" }, condition: NotRpp);
        t.Line("CfD2", manus,
            "\"And Brenas buried its sons for a city that still flies Smolak's flag, Madame Speaker. Our constituents want to know what " +
            "it was for.\"", new[] { "DefA1", "CfTime", "DefA3" });
        t.Choice("CfTime", "\"The ceasefire buys us time. Smolak will not hold Zille forever.\"", new[] { "CfTimeR" });
        t.Line("CfTimeR", Narrator, "Daria nodded slowly. Manus did not look convinced.", new[] { "DefH" });
        t.Choice("CfSigned", "\"I signed for peace, and I would sign it again to end the funerals.\"", new[] { "CfSignedR" });
        t.Line("CfSignedR", Narrator, "Daria looked away. On the RPP benches, a few heads nodded slowly.", new[] { "DefH" });

        t.Register();
    }

    // ------------------------------------------------------------------ c387 Public Opinion

    private static void RegisterPublicOpinion()
    {
        var t = new Talk("Rizia/Turn 7/Public Opinion", "ZilleWar.E7.Opinion.");
        t.Line("Focus97", "Daria de Rava",
            "\"Objection. Our efforts as a kingdom should be focused on winning the war in Zille, not on costly policies that will satisfy no one.\"",
            new[] { "#126" }, condition: ZilleOnly, hooks: From(122));
        t.Line("Focus143", "Daria de Rava",
            "\"Mr. Sazon. Our efforts as a kingdom should be focused on winning the war in Zille, not on costly policies that will satisfy no one.\"",
            new[] { "#162" }, condition: ZilleOnly, hooks: From(177));
        Widen(t, "DespiteWar", 263, 266, ZilleOnly);
        Widen(t, "SupportWar", 303, 306, ZilleOnly);
        t.Register();
    }

    // ------------------------------------------------------------------ c388 Religion

    private static void RegisterReligion()
    {
        var t = new Talk("Rizia/Turn 7/Religion", "ZilleWar.E7.Religion.");
        const string act = "\"With our soldiers fighting Wehlen in Zille, our Wehzek citizens fear they will be treated as the enemy. This decree tells them they are not.\"";
        const string over = "\"After this war with Wehlen, our Wehzek citizens fear they are still seen as the enemy. This decree tells them they are not.\"";
        t.Variant("Wehzek105Act", 105, new[] { 107 }, Act, "Manus Sazon", act, new[] { "#110", "#116", "#111" });
        t.Variant("Wehzek105Over", 105, new[] { 107 }, Over, "Manus Sazon", over, new[] { "#110", "#116", "#111" });
        t.Variant("Wehzek340Act", 340, new[] { 342 }, Act, "Manus Sazon", act, new[] { "#345", "#348", "#346" });
        t.Variant("Wehzek340Over", 340, new[] { 342 }, Over, "Manus Sazon", over, new[] { "#345", "#348", "#346" });
        t.Register();
    }

    // ------------------------------------------------------------------ newspapers

    private static bool IsDeclared => Variables.GetBool(WarFlags.Declared);
    private static bool PalesWar => Variables.GetBool("RiziaDLC.Pales_War");

    private static void RegisterNews()
    {
        // "This violation of his treaty": there is no treaty with Smolak any more.
        Vanilla.HideNews("Turn07_G_DerdiansReallowed", () => IsDeclared);
        Vanilla.HideNews("Turn07_V_DerdianBanLifted", () => IsDeclared);
        // Smolak's northern campaign, written for a Wehlen at peace with Rizia.
        Vanilla.HideNews("G_WehlenDeploysForces", () => IsDeclared);
        // Voice of Rizia's war profits tax story is about Pales. A tax signed for the Zille war stays a Zille story even after the
        // Pales war starts (the hide is latched: lifting it would print the Pales story weeks after the decree).
        Vanilla.HideNews("RD_V_WarProfitsTax", () => IsDeclared && (!PalesWar || Variables.GetBool(VarWarTaxForZille)));
        // "Wehlen slowly appears to lose grip over the security situation" in a Zille (city) that is Rizian again.
        Vanilla.HideNews("Turn07_G_BFFZilleActivitySpike",
            () => Variables.GetBool(WarFlags.Liberated) || Variables.GetBool(WarFlags.ZilleCityHeld));
        // Reports written for the peacetime Zille agreement: "despite previous agreements with Wehlen", Wehzek border guards
        // on the far side of the Mave Nortus crossing. Replaced in OnStep by war-aware versions.
        Vanilla.HideReport("Turn07_MaveNortus_BludsLetIn", () => IsDeclared);
        Vanilla.HideReport("Turn07_Sallabes_DerdiansLetIn", () => IsDeclared);
    }

    private const string VarRepBluds = "ZilleWar.E7_Report_BludsLetIn";
    private const string VarRepDerdians = "ZilleWar.E7_Report_DerdiansLetIn";

    private static void PublishReports()
    {
        if (Variables.GetBool("RiziaDLCSupport.Reports_Turn07_MaveNortus_BludsLetIn") && !Variables.GetBool(VarRepBluds))
        {
            Variables.Set(VarRepBluds, true);
            bool lib = Variables.GetBool(WarFlags.Liberated);
            bool held = Variables.GetBool(WarFlags.ZilleCityHeld);
            // Mave Nortus can still be in Wehzek hands after the war: the families then cross the armistice line elsewhere, and the
            // report is filed in Porte Drazon
            bool maveLost = Variables.GetBool(WarFlags.Occupied("Mave Nortus"));
            string camps = maveLost ? "Relief camps have been set up well away from Mave Nortus, which is still in Wehzek hands."
                : "Relief camps have been set up along the road to Mave Nortus.";
            NewsWire.Report("E7_BludsLetIn", maveLost ? SuzerainModdingKit.VanillaData.SuzerainTokenName.RiziaCityPorteDrazon : "Rizia_City_MaveNortus", "Rizia Opens Its Border to Bluds", lib
                ? NewsWire.P(
                    "Thousands of Bludish families fleeing the Wehzek civil war have begun crossing into Zille, now that its border posts are " +
                    "manned by Rizian soldiers instead of Wehzek police. " + camps,
                    "Wraclawitz has accused Porte Drazon of opening a door for terrorists into the region it lost.")
                : held
                    ? NewsWire.P(
                        "Thousands of Bludish families fleeing the Wehzek civil war have begun crossing the armistice line into Rizian-held " +
                        "Zille. Wehzek patrols on the far side tried to turn them back, then gave up. " + camps,
                        "Wraclawitz has called it a breach of the armistice. Our officers on the line call it a flood.")
                    : maveLost
                    ? NewsWire.P(
                        "Rizia has opened its side of the armistice line to Bluds fleeing the Wehzek civil war. With Mave Nortus still in Wehzek " +
                        "hands, the families cross on foot through the hills, and Wehzek patrols have stopped trying to turn them back.",
                        camps + " Wraclawitz has called it one more provocation from a kingdom that has already gone to war over the region.")
                    : NewsWire.P(
                        "Rizia has opened the Mave Nortus crossing to Bluds fleeing the Wehzek civil war. Wehzek border police on the Zille side " +
                        "of the river tried to stop the thousands who wanted to cross, then had to let them pass.",
                        "Wraclawitz has called it one more provocation from a kingdom that has already gone to war over the region."));
        }

        if (Variables.GetBool("RiziaDLCSupport.Reports_Turn07_Sallabes_DerdiansLetIn") && !Variables.GetBool(VarRepDerdians))
        {
            Variables.Set(VarRepDerdians, true);
            NewsWire.Report("E7_DerdiansLetIn", "Rizia_City_Sallabes", "Hundreds of Derdians Arrive in Sallabes", NewsWire.P(
                "Ships full of Derdian Golcondists have started arriving in the port of Sallabes, and the harbor master has opened a second " +
                "quay to take them.",
                "In Wraclawitz, President Wiktor Smolak called the arrivals proof that Rizia means to arm every enemy Wehlen has."));
        }
    }

    public static void OnStep()
    {
        EarlyT7Diplomacy.OnStep();
        EarlyT7Home.OnStep();
        // the Voice's story on the end of the war tax thanks "the Pales conflict" for it: a Zille war tax gets a Zille story
        if (IsDeclared && Variables.GetBool(VarWarTaxForZille))
        {
            WorldText.News("RD_V_RemoveWarProfitsTax", null,
                "The Crown has lifted the war profits tax, to the relief of profiteers everywhere. The one good thing the war in Zille brought " +
                "-- an excess of government income that could be spent on the people -- is gone, wasted on shells and on the graves of the " +
                "boys who fired them.\n\nWe at the Voice of Rizia continue to call for a peaceful and multipolar world, where not a single " +
                "additional guilder is spent on war and destruction.");
        }
        else
        {
            WorldText.RestoreNews("RD_V_RemoveWarProfitsTax");
        }

        if (!IsDeclared)
        {
            return;
        }

        if (!PalesWar && Variables.GetBool("RiziaDLC.Policy_RD_Economy_WarProfitsTax") && !Variables.GetBool(VarWarTaxForZille))
        {
            Variables.Set(VarWarTaxForZille, true);
        }

        // the House rows read bools; saves from before them only have the admin string
        string admin = Variables.GetString(VarZilleAdmin) ?? string.Empty;
        if (Variables.GetBool(VarAdminBrenas) != (admin == "brenas"))
        {
            Variables.Set(VarAdminBrenas, admin == "brenas");
        }

        if (Variables.GetBool(VarAdminImperii) != (admin == "imperii"))
        {
            Variables.Set(VarAdminImperii, admin == "imperii");
        }

        bool active = Variables.GetBool(WarFlags.Active);
        bool liberated = Variables.GetBool(WarFlags.Liberated);
        PublishReports();

        if (Variables.GetBool("RiziaDLCSupport.News_Turn07_G_DerdiansReallowed") && !Variables.GetBool(VarNewsDerdiaG))
        {
            Variables.Set(VarNewsDerdiaG, true);
            NewsWire.Publish("E7_DerdiansReallowed", "G", "Rizia Reopens Its Border to Derdians", string.Join("\n\n",
                "The Kingdom of Rizia announced this week that Derdian citizens will once more be allowed to enter its territory, days after " +
                "King Romus Toras returned from talks behind the walls of Supreme Wiseman Jorga Azmal's Silver Garrison.",
                "Few observers were surprised. Derdia's government has never hidden its contempt for President Wiktor Smolak, whose " +
                "treatment of Wehlen's Golcondists it has called a crime against the faith, and its mercenaries are known across Merkopa. " +
                "It is not known what, if anything, Derdia offered in return.",
                active
                    ? "With Rizian and Wehzek soldiers still fighting in Zille, Wehzek state radio was quick to call the decision \"a holy " +
                      "alliance of aggressors.\" Porte Drazon has not responded."
                    : "Wraclawitz called the decision \"one more provocation from a kingdom that has already shown what it thinks of its " +
                      "neighbors.\" Porte Drazon has not responded."));
        }

        if (Variables.GetBool("RiziaDLCSupport.News_Turn07_V_DerdianBanLifted") && !Variables.GetBool(VarNewsDerdiaV))
        {
            Variables.Set(VarNewsDerdiaV, true);
            NewsWire.Publish("E7_DerdianBanLifted", "V", "Derdians Once More Welcome in Rizia", string.Join("\n\n",
                "King Romus has finally ended the golcondophobic border ban on Derdian citizens. We would like to believe it was a change " +
                "of heart. We suspect it has rather more to do with Zille, and with Supreme Wiseman Azmal's famous mercenaries.",
                "Either way, the families who were turned back at our borders can now come home to their relatives. That is a good thing, " +
                "whatever the reason. Plenty of work remains before Rizia treats all of its peoples as equals, beginning with the Wehzek " +
                "Rizians who have spent this year being looked at like spies in their own streets."));
        }

        if (Variables.GetBool("SharedSupport.News_G_WehlenDeploysForces") && !Variables.GetBool(VarNewsWehlenNorth))
        {
            Variables.Set(VarNewsWehlenNorth, true);
            string title = active ? "Wehlen Fights on Two Fronts as Bludish Rebels Strike in the North"
                : liberated ? "Smolak Turns His Army North After Losing Zille"
                : "Wehlen Moves Troops North From the Zille Front";
            string opening = active
                ? "WRACLAWITZ, WEHLEN - With the war against Rizia still raging in Zille, President Wiktor Smolak has put the city of Vernen " +
                  "and seven towns in the northeast of the country under curfew, and has sent specially trained units north to fight the " +
                  "Bludish Freedom Front."
                : liberated
                    ? "WRACLAWITZ, WEHLEN - Weeks after his garrison was driven out of Zille, President Wiktor Smolak has put the city of Vernen " +
                      "and seven towns in the northeast of the country under curfew, and has sent his remaining special forces north against " +
                      "the Bludish Freedom Front."
                    : "WRACLAWITZ, WEHLEN - With the guns silent in Zille, President Wiktor Smolak has put the city of Vernen and seven towns " +
                      "in the northeast of the country under curfew, and has moved specially trained units from the Rizian front to fight the " +
                      "Bludish Freedom Front.";
            NewsWire.Publish("E7_WehlenNorth", "G", title, string.Join("\n\n", opening,
                "Wehzek officials accuse foreign powers of arming the rebels and of timing their raids to the " +
                (active ? "fighting in the south. " : "war in the south. ") +
                "Analysts in Konstantium note that the Wehzek army, already one of the weakest in Eastern Merkopa, can ill afford a " +
                "campaign on its own soil.",
                "The operation will be felt across the region, particularly in Lespia, Rumburg and Sordland, which has a large Bludish " +
                "population inside its borders."));
        }

        // Two wars: Lespia closes the strait for Rizia in the north and sells Pales its warships in the south.
        // EarlyT6 files the same story ("E6_LespiaBothSides") as soon as both wars run; publish this one only if it did not.
        // destroyers only when Lespia arms Pales fully (support level 2; 0 sends nothing, 1 a token shipment of armor)
        if (Variables.GetBool(WarFlags.TwoWars) && Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 &&
            Variables.GetInt("RiziaDLC.War_Pales_LespianSupport") >= 2 &&
            !Variables.GetBool(SupplyWar.VarLespiaWithdrawn) && !Variables.GetBool("ZilleWar.Lespia_StraitThin") &&
            !Variables.GetBool(SupplyWar.VarLespiaGrievance) && !Variables.GetBool(VarNewsLespiaBothSides) && !Variables.GetBool("ZilleWar.E6_News_LespiaBothSides"))
        {
            Variables.Set(VarNewsLespiaBothSides, true);
            NewsWire.Publish("E7_LespiaBothSides", "G", "Lespian Destroyers Sail for Pales After Lespia Guarded Kreleck for Rizia", string.Join("\n\n",
                "PERLA, LESPIA - Two Lespian-built destroyers sailed for the Palesian port of Remaz this week, only weeks after the Lespian " +
                "navy closed the Strait of Kreleck to Wehzek shipping at Rizia's request.",
                "Asked how Lespia could help Rizia against Wehlen while arming the Grand Duchy against Rizia, a spokesman for Prime Minister " +
                "Patricio Alvarez said only that Lespia \"honors its contracts. All of them.\"",
                "Officials in Porte Drazon declined to comment. Diplomats in Konstantium were less discreet. \"Lespia does not have allies,\" " +
                "one of them told Geopolitico. \"It has customers.\""));
        }

        if (!PalesWar && Variables.GetBool("RiziaDLC.Policy_RD_Economy_WarProfitsTax") && !Variables.GetBool(VarNewsWarTax))
        {
            Variables.Set(VarNewsWarTax, true);
            NewsWire.Publish("E7_WarTax", "RH", "Every Rizian Shares the Burden", string.Join("\n\n",
                "By royal decree, the extraordinary profits made on war contracts will be taxed, now that the campaign in Zille has shown what the army costs. " +
                "The soldier gives his blood; it is only right, the Palace says, that the factory give its share. The Treasury expects the " +
                "tax to raise a full ten billion guilders for the army before the year is out.",
                "The Royal Herald is confident the great houses of industry will answer the call with the same patriotism as the men of Brenas."));
            NewsWire.Publish("E7_WarTax", "V", "War Profiteers Taxed", string.Join("\n\n",
                "If a war in Zille must be fought, then at the very least our corporations should not grow fat on it. His Majesty has " +
                "decreed a heavy tax on war profits, and for once the Palace and this newspaper agree on something.",
                "The arms works of Cardesse-Montaklar have run day and night since Konstantium. The families of Brenas have paid for this " +
                "war in sons. It is only fair that those who have profited from it pay in guilders. We can only hope His Majesty has " +
                "protected himself from the howls of House Azaro, which will be heard from here to the Alca."));
        }
    }
}
