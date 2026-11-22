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

    public static readonly string Declared = WarFlags.LDeclared;
    public static readonly string Peace = WarFlags.LPeace;
    public static readonly string Act = WarFlags.LActive;
    public static readonly string Lib = WarFlags.L(WarFlags.Liberated);
    public static readonly string Def = WarFlags.L(WarFlags.Defeated);
    public static readonly string Cf = $"{WarFlags.L(WarFlags.Ceasefire)} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.Defeated)}";
    public static readonly string Over = WarFlags.L(WarFlags.Over);
    public static readonly string Two = WarFlags.L(WarFlags.TwoWars);
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
                 })
        {
            Variables.Register(v);
        }

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
        t.Variant("Doubt18Lost", 18, new[] { 17, 19, 23 }, $"({Def}) or ({Cf})", "Manus Sazon",
            "\"We have only just failed to bring Zille home by force. It is difficult for my party to support another war, this time against the Grand Duchy.\"",
            new[] { "#11" });
        t.Variant("Doubt53Act", 53, new[] { 51, 52 }, Act, "Manus Sazon",
            "\"It is difficult for the RPP to support an all-out attack on the Grand Duchy while our soldiers are still fighting in Zille.\"",
            new[] { "#54" });
        t.Variant("Doubt53Over", 53, new[] { 51, 52 }, Over, "Manus Sazon",
            "\"It is difficult for the RPP to support an all-out attack on the Grand Duchy. We have barely finished counting the dead from Zille.\"",
            new[] { "#54" });
        t.Variant("Tax41", 41, new[] { 33 }, Act, "Manus Sazon",
            "\"In the name of equity, the RPP is proposing an excess profits tax on the businesses and industries that stand to generate the most revenue from the campaigns in Pales and Zille.\"",
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
        t.Logic("Start", new[] { "Act", "Lib", "Def", "Cf", "#3" }, condition: Declared, hooks: From(1));

        // --- the war goes on
        t.Logic("Act", new[] { "ActM", "ActD" }, condition: Act);
        t.Line("ActM", manus,
            "\"My constituents in Brenas have sons in Zille, Your Majesty. Many of them are fighting over fields their grandfathers farmed " +
            "before 1927. They ask this House two questions. When will this war end, and who will govern Zille once it does?\"",
            new[] { "ActM2" }, condition: Rpp);
        t.Line("ActM2", daria,
            "\"The Rizian National Coalition stands behind every soldier in Zille, Mr. Speaker. We ask only that the Crown not give away " +
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
            "\"These are the same factories that are arming our soldiers, Mr. Sazon. They should be thanked, not punished.\"", new[] { "Tax3" });
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

        // --- who governs Zille: vanilla's Brenas or Rizia Imperii argument, made concrete by the war
        t.Logic("Admin", new[] { "Adm1Lib", "Adm1Act" });
        t.Line("Adm1Act", daria,
            "\"Which brings us to what comes after. Once Zille is liberated, it should be administered from Porte Drazon, the way the " +
            "Pales Administrative District is. Stability first.\"", new[] { "Adm2" }, condition: WarFlags.LNot(WarFlags.Liberated));
        t.Line("Adm1Lib", daria,
            "\"Which brings us to what comes next. Zille has been through a war. It should be administered from Porte Drazon, the way " +
            "the Pales Administrative District is, until it is stable.\"", new[] { "Adm2" }, condition: Lib);
        t.Line("Adm2", manus,
            "\"Objection. Zille has always been part of the province of Brenas. Brenas paid for this war with its sons. Brenas should " +
            "govern what they won back.\"", new[] { "Adm3" });
        t.Line("Adm3", Narrator, "The volume in the room grew as the Delegates began arguing among themselves. I turned to Hugo.", new[] { "Adm4" });
        t.Line("Adm4", hugo,
            "\"You have empowered the Sazons enough without handing them a liberated city, Your Majesty. Whatever you decide, decide it " +
            "before the generals decide it for you.\"", new[] { "AdmB", "AdmI", "AdmW" });
        t.Choice("AdmB", "\"Zille will return to Brenas, as it was before 1927.\"", new[] { "AdmBR" },
            script: Str(VarZilleAdmin, "brenas") + Inc("RiziaDLC.Country_HouseSazon", 1) + Inc("RiziaDLC.Country_HouseToras", -1));
        t.Line("AdmBR", Narrator, "The RPP benches broke into applause. Beside me, Hugo closed his eyes.", new[] { "#191" });
        t.Choice("AdmI", "\"Zille will be governed from Porte Drazon until it is secure.\"", new[] { "AdmIR" },
            script: Str(VarZilleAdmin, "imperii") + Inc("RiziaDLC.Country_HouseSazon", -1));
        t.Line("AdmIR", Narrator, "Daria smiled. Manus gripped the edges of his podium until his knuckles went white.", new[] { "#191" });
        t.Choice("AdmW", "\"That can wait until the war is truly behind us.\"", new[] { "AdmWR" }, script: Str(VarZilleAdmin, "undecided"));
        t.Line("AdmWR", hugo, "\"Sooner rather than later, one hopes.\"", new[] { "#191" });

        // --- liberated
        t.Logic("Lib", new[] { "LibM", "LibD" }, condition: Lib);
        t.Line("LibM", manus,
            "\"The first matter on our agenda is a happy one. On behalf of the people of Brenas, this House congratulates His Majesty and " +
            "the Royal Army. After nearly three decades, Zille is Rizian again.\"", new[] { "LibM2" }, condition: Rpp);
        t.Line("LibM2", daria, "\"The RNC never doubted it for a moment, Mr. Speaker.\"", new[] { "LibQ" });
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
        t.Line("LibA1r", Narrator, "Manus inclined his head. Daria's smile thinned.", new[] { "Admin" });
        t.Choice("LibA2", "\"Those who served Smolak will answer for it. The rest have nothing to fear.\"", new[] { "LibA2r" });
        t.Line("LibA2r", Narrator, "Applause rose from the RNC benches. Manus wrote something down.", new[] { "Admin" });

        // --- lost
        t.Logic("Def", new[] { "DefM", "DefD" }, condition: Def);
        t.Line("DefM", manus,
            "\"Your Majesty, I will not pretend this is an easy session. We were promised a short war and a homecoming. Brenas has " +
            "received neither. Only telegrams.\"", new[] { "DefM2" }, condition: Rpp);
        t.Line("DefM2", daria,
            "\"The RNC will never abandon the claim to Zille, Mr. Speaker. But the people want to know who failed them. The army, or " +
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
        t.Choice("DefA2", "\"Zille is not lost. We lost a battle, not the claim.\"", new[] { "DefA2r" });
        t.Line("DefA2r", daria, "\"Then the RNC will hold you to that, Your Majesty.\"", new[] { "DefH" });
        t.Choice("DefA3", "\"This House will not put the Crown on trial.\"", new[] { "DefA3r" },
            script: Inc("RiziaDLC.LeaningValue_Democratic", -1));
        t.Line("DefA3r", Narrator, "Manus opened his mouth, then closed it again. Beside me, Hugo gave a small nod of approval.", new[] { "DefH" });
        t.Line("DefH", hugo, "\"Let them grieve, Romus. Just don't let them choose a culprit.\"", new[] { "#191" });

        // --- ceasefire
        t.Logic("Cf", new[] { "CfM", "CfD" }, condition: Cf);
        t.Line("CfM", manus,
            "\"Your Majesty, the guns in Zille have fallen silent, and my party is grateful for it. But a ceasefire is not a peace. Our " +
            "constituents want to know where the line runs, and who is left living on the wrong side of it.\"", new[] { "CfM2" }, condition: Rpp);
        t.Line("CfM2", daria,
            "\"The RNC accepted the ceasefire because His Majesty asked us to. We will not accept a border drawn in Konstantium by people " +
            "who have never set foot in Zille.\"", new[] { "CfRef" });
        t.Line("CfD", daria,
            "\"The RNC accepted the ceasefire because His Majesty asked us to, Your Majesty. But we will not accept a border drawn in " +
            "Konstantium by people who have never set foot in Zille.\"", new[] { "CfD2" }, condition: NotRpp);
        t.Line("CfD2", manus,
            "\"A ceasefire is not a peace, Madame Speaker. Our constituents want to know where the line runs, and who is left living on " +
            "the wrong side of it.\"", new[] { "CfRef" });
        t.Line("CfRef", manus, "\"Then let the people of Zille draw it. A referendum, under international supervision.\"", new[] { "CfH" });
        t.Line("CfH", hugo,
            "\"Don't go down that road, Your Majesty. Any vote in Zille is an open door for Wehlen, and Smolak will walk through it.\"",
            new[] { "CfA1", "CfA2" });
        t.Choice("CfA1", "\"Zille's future will be decided by Rizia, not by ballots.\"", new[] { "CfA1r" });
        t.Line("CfA1r", daria, "\"Hear, hear.\"", new[] { "#191" });
        t.Choice("CfA2", "\"Perhaps a vote is the only way this ends without more graves.\"", new[] { "CfA2r" },
            script: Set(VarReferendumFloated, "true"));
        t.Line("CfA2r", Narrator, "Manus looked at me in surprise. Hugo did not look at me at all.", new[] { "#191" });

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
        // Voice of Rizia's war profits tax story is about Pales.
        Vanilla.HideNews("RD_V_WarProfitsTax", () => IsDeclared && !PalesWar);
        // "Wehlen slowly appears to lose grip over the security situation" in a Zille that is Rizian again.
        Vanilla.HideNews("Turn07_G_BFFZilleActivitySpike", () => Variables.GetBool(WarFlags.Liberated));
    }

    public static void OnStep()
    {
        EarlyT7Diplomacy.OnStep();
        EarlyT7Home.OnStep();
        if (!IsDeclared)
        {
            return;
        }

        bool active = Variables.GetBool(WarFlags.Active);
        bool liberated = Variables.GetBool(WarFlags.Liberated);

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
                "Wehzek officials accuse foreign powers of arming the rebels and of timing their raids to the fighting in the south. " +
                "Analysts in Konstantium note that the Wehzek army, already one of the weakest in Eastern Merkopa, can ill afford a " +
                "campaign on its own soil.",
                "The operation will be felt across the region, particularly in Lespia, Rumburg and Sordland, which has a large Bludish " +
                "population inside its borders."));
        }

        // Two wars: Lespia closes the strait for Rizia in the north and sells Pales its warships in the south.
        // EarlyT6 files the same story ("E6_LespiaBothSides") as soon as both wars run; publish this one only if it did not.
        if (Variables.GetBool(WarFlags.TwoWars) && Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 &&
            !Variables.GetBool(VarNewsLespiaBothSides) && !Variables.GetBool("ZilleWar.E6_News_LespiaBothSides"))
        {
            Variables.Set(VarNewsLespiaBothSides, true);
            NewsWire.Publish("E7_LespiaBothSides", "G", "Lespian Destroyers Sail for Pales as Lespia Guards Kreleck for Rizia", string.Join("\n\n",
                "PERLA, LESPIA - Two Lespian-built destroyers sailed for the Palesian port of Remaz this week, only weeks after the Lespian " +
                "navy closed the Strait of Kreleck to Wehzek shipping at Rizia's request.",
                "Asked how Lespia could help Rizia against Wehlen while arming the Grand Duchy against Rizia, a spokesman for Prime Minister " +
                "Patricio Alvarez said only that Lespia \"honors its contracts. All of them.\"",
                "Officials in Porte Drazon declined to comment. Diplomats in Konstantium were less discreet. \"Lespia does not have allies,\" " +
                "one of them told Geopolitico. \"It has customers.\""));
        }

        if (!PalesWar && active && Variables.GetBool("RiziaDLC.Policy_RD_Economy_WarProfitsTax") && !Variables.GetBool(VarNewsWarTax))
        {
            Variables.Set(VarNewsWarTax, true);
            NewsWire.Publish("E7_WarTax", "RH", "Every Rizian Shares the Burden", string.Join("\n\n",
                "By royal decree, the extraordinary profits made on war contracts will be taxed for the duration of the campaign in Zille. " +
                "The soldier gives his blood; it is only right, the Palace says, that the factory give its share. The Treasury expects the " +
                "tax to raise a full ten billion guilders for the army before the year is out.",
                "The Royal Herald is confident the great houses of industry will answer the call with the same patriotism as the men of Brenas."));
            NewsWire.Publish("E7_WarTax", "V", "War Profiteers Taxed", string.Join("\n\n",
                "If the war in Zille must go on, then at the very least our corporations should not grow fat on it. His Majesty has " +
                "decreed a heavy tax on war profits, and for once the Palace and this newspaper agree on something.",
                "The arms works of Cardesse-Montaklar have run day and night since Konstantium. The families of Brenas have paid for this " +
                "war in sons. It is only fair that those who have profited from it pay in guilders. We can only hope His Majesty has " +
                "protected himself from the howls of House Azaro, which will be heard from here to the Alca."));
        }
    }
}
