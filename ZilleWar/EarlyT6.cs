using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;

namespace ZilleWar;

/// <summary>
/// Vanilla Turn 6 adjusted for the Zille war. Turn 6 is where the vanilla Pales crisis breaks out, so it is the
/// first "two wars at once" turn: the council meeting, the night council, the Duke's calls, the general staff
/// briefing and the balcony speech all learn that Rizia is already fighting Wehlen in the north. Peacetime lines
/// (Wehlen backing Rizia at the AN, "the restoration of Zille" as a future task, "our peaceful nation") get
/// war variants in each character's voice. News that lists Wehlen among Rizia's backers is replaced.
/// </summary>
internal static class EarlyT6
{
    // ---- variables other modules can read
    /// <summary>"zille" | "pales" | "both": the King's answer to Taddeus at the Pales staff briefing.</summary>
    public const string VarWarPriority = "ZilleWar.E6_WarPriority";
    /// <summary>Wehlen lobbied against Rizia at the Aureus arbitration (one support point lost).</summary>
    public const string VarWehlenVotedAgainst = "ZilleWar.E6_WehlenVotedAgainst";
    /// <summary>The King told the generals to spare Zille's old harbor quarter (Estela's sanctuary).</summary>
    public const string VarSpareOldQuarter = "ZilleWar.E6_SpareOldQuarter";
    /// <summary>"paid" | "refused" | "caleqabiz": Alvarez's price for the strait, asked at the Aureus deal signing.</summary>
    public const string VarAlvarezGasPrice = "ZilleWar.E6_AlvarezGasPrice";
    /// <summary>Lucita Azaro was arrested while the Zille war was being fought.</summary>
    public const string VarLucitaArrestedMidWar = "ZilleWar.E6_LucitaArrestedMidWar";

    private const string AskedWehlenSuspect = "ZilleWar.E6_Asked_WehlenSuspect";
    private const string AskedSmolakTaunt = "ZilleWar.E6_Asked_SmolakTaunt";
    private const string AskedNorthSpeech = "ZilleWar.E6_Asked_NorthSpeech";
    private const string AlvarezAsideDone = "ZilleWar.E6_AlvarezAsideDone";

    // news/report guards
    private const string NewsANRuling = "ZilleWar.E6_News_ANRuling";
    private const string NewsBuildup = "ZilleWar.E6_News_Buildup";
    private const string NewsLespiaBothSides = "ZilleWar.E6_News_LespiaBothSides";
    private const string NewsSmolakLespia = "ZilleWar.E6_News_SmolakLespia";
    private const string ReportAzaroOfficers = "ZilleWar.E6_Report_AzaroOfficers";

    // ---- speakers
    private const string Lucita = "Lucita Azaro";
    private const string Taddeus = "Taddeus Azaro";
    private const string Carlos = "Carlos Robles Azaro";
    private const string Hugo = "Hugo Toras";
    private const string Laurento = "Laurento Esquibel";
    private const string Elena = "Elena Werner";
    private const string Estela = "Estela Toras";
    private const string Vina = "Vina Toras";
    private const string Sal = "Sal Ignacius";
    private const string Axel = "Axel Reinhart";
    private const string Titus = "Titus Gordion";
    private const string Alvarez = "Patricio Alvarez (R)";

    // ---- Lua conditions
    private static readonly string Active = WarFlags.LActive;
    private static readonly string Declared = WarFlags.LDeclared;
    // Active is recomputed at step start, while Liberated/Defeated/Ceasefire can flip mid-step (an operation or a ceasefire
    // during the step). The outcome conditions exclude Active so a variant group never shows two versions at once.
    private static readonly string Liberated = $"{WarFlags.L(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.Active)}";
    private static readonly string Defeated = WarFlags.L(WarFlags.Defeated);
    private static readonly string Ceasefire = WarFlags.L(WarFlags.Ceasefire);
    private static readonly string LostOrStopped =
        $"({Defeated} or {Ceasefire}) and {WarFlags.LNot(WarFlags.Active)} and {WarFlags.LNot(WarFlags.Liberated)}";
    private const string LespiaDeal = "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0";
    private const string PalesWarComing =
        "(Variable[\"RiziaDLC.Pales_War\"] == true or Variable[\"RiziaDLC.Diplomacy_Pales_PrepareForWar\"] == true or Variable[\"RiziaDLC.Diplomacy_Pales_DeclareWar\"] == true)";

    /// <summary>Lespia holds a grudge (grievance, MITZ betrayal, broken promise) or the strait patrols were lax: no claim that the strait was held shut.</summary>
    private static readonly string LespiaCold =
        $"(Variable[\"{SupplyWar.VarLespiaGrievance}\"] == true or Variable[\"RiziaDLC.MITZ_ScrewedOverLespia\"] == true or " +
        $"Variable[\"{EarlyT5.VarLespiaBetrayed}\"] == true or Variable[\"ZilleWar.Lespia_StraitThin\"] == true)";
    private static bool LespiaColdNow =>
        Variables.GetBool(SupplyWar.VarLespiaGrievance) || Variables.GetBool("RiziaDLC.MITZ_ScrewedOverLespia") ||
        Variables.GetBool(EarlyT5.VarLespiaBetrayed) || Variables.GetBool("ZilleWar.Lespia_StraitThin");

    private static string NotSet(string v) => $"Variable[\"{v}\"] ~= true";

    /// <summary>Nil-safe increment of a vanilla number.</summary>
    private static string Inc(string v, int d) => $"Variable[\"{v}\"] = (Variable[\"{v}\"] or 0) + ({d}); ";

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarWarPriority, VarWehlenVotedAgainst, VarSpareOldQuarter, VarAlvarezGasPrice, VarLucitaArrestedMidWar,
                     AskedWehlenSuspect, AskedSmolakTaunt, AskedNorthSpeech, AlvarezAsideDone,
                     NewsANRuling, NewsBuildup, NewsLespiaBothSides, NewsSmolakLespia, ReportAzaroOfficers,
                 })
        {
            Variables.Register(v);
        }

        CouncilMeeting();
        IndividualSupport();
        NightCouncil();
        PostIncidentMeeting();
        BorderInvestigation();
        LucitaUpdate();
        DealSigning();
        DukeWarCall();
        GeneralStaff();
        Cemetery();
        BalconyIntro();
        Balcony();
        ReligionBriefing();

        // Geopolitico's Aureus ruling lists Wehlen among Rizia's backers; at war we publish our own copy.
        Vanilla.HideNews("Turn06_G_ANFavoursRiziaNavy", () => Variables.GetBool(WarFlags.Declared));
        Vanilla.HideNews("Turn06_G_ANFavoursRiziaNoNavy", () => Variables.GetBool(WarFlags.Declared));
        // The build-up story ignores the army already fighting in the north.
        Vanilla.HideNews("Turn06_G_RiziaMilitaryBuildup", () => Variables.GetBool(WarFlags.Active));
    }

    // ================================================================== c361 Council meeting (AN ruling, Pales choice)

    private static void CouncilMeeting()
    {
        var t = new Talk("Rizia/Turn 6/A_CouncilMeeting", "ZilleWar.E6.Council.");

        // [39] decides whether Wehlen backs Rizia at the arbitration. A country at war with us lobbies against us instead.
        Vanilla.Require("Rizia/Turn 6/A_CouncilMeeting", 40, WarFlags.LPeace);
        t.Logic("WehlenAgainst", new[] { "#42" }, condition: Declared, hooks: From(39),
            script: Set(VarWehlenVotedAgainst, "true") + Set("RiziaDLC.Diplomacy_Wehlen_Arbitration_Support", "false") +
                    Inc("RiziaDLC.Arbitration_TotalSupport", -1));

        // The King has just ordered preparations to invade Pales, with the army already fighting in Zille.
        t.Line("TwoFronts1", Lucita,
            "\"One more thing, Your Majesty, before I go. Our northern divisions are committed in Zille. If we open the Alca front " +
            "now, I will be fighting Wehlen with one hand and Pales with the other.\"",
            new[] { "TwoFronts2" }, condition: Active, hooks: From(747));
        t.Line("TwoFronts2", Elena,
            "\"The treasury was built for one war, Your Majesty. Not two. Something will have to give, and I would rather we chose " +
            "what than let the bills choose for us.\"", new[] { "TwoFronts3" });
        t.Line("TwoFronts3", Laurento,
            "\"President Smolak will be delighted. Every Rizian soldier sent south is one fewer in Zille, and he knows it.\"",
            new[] { "TwoFronts4" });
        t.Line("TwoFronts4", Hugo,
            "\"Or Smolak will learn that Rizia can fight on two fronts and win both. Your father never could. That is reason enough to try.\"",
            new[] { "TwoFronts5" });
        t.Line("TwoFronts5", Lucita,
            "\"The general staff will plan for both. But they will want to know which front comes first, Your Majesty. Sooner rather than later.\"",
            new[] { "#748", "#749", "#750" });

        // The blockade order: "If they fire at us, we will declare war."
        t.Line("Stretched", Lucita,
            "\"Though I should warn you, the fleet is already stretched thin between the Aureus field and the Zille coast. If Pales " +
            "does fire, I will not have a second squadron to send.\"",
            new[] { "#573" }, condition: Active, hooks: From(554));
        t.Register();
    }

    // ================================================================== c364 AN individual support

    private static void IndividualSupport()
    {
        var t = new Talk("Rizia/Turn 6/AN Individual Support", "ZilleWar.E6.ANSupport.");
        t.Line("Wehlen", Laurento,
            "\"Wehlen's delegation, needless to say, voted against anything that bore our name. President Smolak's envoy called our " +
            "claim 'the appetite of a kingdom that has already shown it prefers tanks to treaties.'\"",
            new[] { "#16" }, condition: Declared, hooks: From(13));
        t.Line("LespiaBothWays", Laurento,
            "\"I note that Prime Minister Alvarez voted with Pales all the same, ships at Kreleck or not. He sells us the strait in the " +
            "north and backs Reinhart in the south. I would expect nothing less of him.\"",
            new[] { "#29", "#30" }, condition: $"{Active} and {LespiaDeal}", hooks: From(28));
        t.Line("LespiaBothWaysOver", Laurento,
            "\"I note that Prime Minister Alvarez voted with Pales all the same. He sold us the strait while our army was in Zille, and " +
            "now he backs Reinhart in the south. I would expect nothing less of him.\"",
            new[] { "#29", "#30" }, condition: $"{AuditT6.Over} and {LespiaDeal} and not {LespiaCold}", hooks: From(28));
        t.Line("LespiaBothWaysCold", Laurento,
            "\"I note that Prime Minister Alvarez voted with Pales all the same. He promised us the strait while our army was in Zille, " +
            "kept it half open, and now backs Reinhart in the south. I would expect nothing less of him.\"",
            new[] { "#29", "#30" }, condition: $"{AuditT6.Over} and {LespiaDeal} and {LespiaCold}", hooks: From(28));
        t.Register();
    }

    // ================================================================== c359 Night council after the Pales incident

    private static void NightCouncil()
    {
        var t = new Talk("Rizia/Turn 6/SnO_Incident", "ZilleWar.E6.Night.");

        // On the way to the chambers.
        t.Choice("Titus", "\"Titus? Is this about Zille? Has something happened at the front?\"", new[] { "TitusA" },
            condition: Active, hooks: From(19));
        t.Line("TitusA", Titus,
            "\"I don't believe so, Your Majesty. They said the south. That's all they would tell me.\"", new[] { "#142" });

        // The suspects: nobody mentions the one man who gains most from a second front.
        t.Choice("Wehlen", "\"What if it's Wehlen? A second front is exactly what Smolak needs.\"", new[] { "Wehlen1" },
            condition: $"{Active} and {NotSet(AskedWehlenSuspect)}", script: Set(AskedWehlenSuspect, "true"), hooks: From(175));
        t.Line("Wehlen1", Lucita,
            "\"It would be like him. Smolak cannot win in Zille, so he needs us bleeding somewhere else.\"", new[] { "Wehlen2" });
        t.Line("Wehlen2", Hugo,
            "\"Wehzek soldiers at the River Alca? I doubt it. But Smolak will applaud whoever did this, you may be sure of that.\"",
            new[] { "Wehlen3" });
        t.Line("Wehlen3", Laurento,
            "\"Or Pales is counting on us being too busy in the north to answer. Either way, Your Majesty, we are being measured.\"",
            new[] { "#175" });
        t.Register();
    }

    // ================================================================== c358 Meeting with the Duke after the incident

    private static void PostIncidentMeeting()
    {
        const string conv = "Rizia/Turn 6/SnO_PalesPostIncidentMeeting";
        var t = new Talk(conv, "ZilleWar.E6.Duke.");
        string[] after257 = { "#240", "#238", "#258", "#244" };

        // "Do you think Pales can stand against Rizia?"
        t.Variant("StandActive", 257, new[] { 239 }, Active, Axel,
            "\"You are already at war in the north, Your Majesty. Your soldiers are dying in Zille over a lease your father signed. " +
            "Do you truly want a second front on the Alca?\"", after257);
        t.Variant("StandLiberated", 257, new[] { 239 }, Liberated, Axel,
            "\"I watched what your army did in Zille, Your Majesty. Believe me, I watched. But I am not Smolak, and Pales is not a lease.\"",
            after257);
        t.Variant("StandLost", 257, new[] { 239 }, AuditT6.Lost, Axel,
            "\"Your army could not even take Zille from Smolak, Your Majesty. Spare me the threats.\"", after257);
        t.Variant("StandPart", 257, new[] { 239 }, AuditT6.Part, Axel,
            "\"Your army could not finish Smolak, Your Majesty. He still holds half of Zille behind his armistice line. Spare me the threats.\"",
            after257);

        // The suspects.
        t.Choice("Suspect", "\"Wehlen.\"", new[] { "Suspect1", "Suspect1Lib", "Suspect1Over" }, condition: Declared, hooks: From(464));
        t.Line("Suspect1", Axel,
            "\"Smolak? He has his hands full with your army. Though I grant you, nothing would please him more than to see us at each other's throats.\"",
            new[] { "Suspect2", "Suspect3" }, condition: Active);
        t.Line("Suspect1Lib", Axel,
            "\"Smolak? He has only just lost Zille to you. I doubt he has the stomach for more. Though I grant you, nothing would please " +
            "him more than to see us at each other's throats.\"", new[] { "Suspect2", "Suspect3" }, condition: Liberated);
        t.Line("Suspect1Over", Axel,
            "\"Smolak? He has only just signed an armistice with you. Though I grant you, nothing would please him more than to see us " +
            "at each other's throats.\"", new[] { "Suspect2", "Suspect3" }, condition: AuditT6.NotLib);
        t.Choice("Suspect2", "\"Every division I send south is one he no longer has to face. Think about who that serves.\"", new[] { "Suspect4" });
        t.Choice("Suspect3", "\"Smolak has been trying to divide this region since the day he decided Zille's lease was his to keep.\"", new[] { "Suspect4" });
        t.Line("Suspect4", Axel,
            "\"There are no reports or traces of any Wehzek activity. If you cannot show me any proof -- this will only sound like a conspiracy theory.\"",
            new[] { "#779" });
        t.Register();
    }

    // ================================================================== c371 The border investigation: arrest Lucita?

    private static void BorderInvestigation()
    {
        var t = new Talk("Rizia/Turn 6/A_BorderInvestigationResults", "ZilleWar.E6.Arrest.");
        t.Line("Hugo", Hugo,
            "\"And let us not forget the war we are already fighting. Arrest her now, Romus, and you arrest the woman running the Zille " +
            "campaign. Half the officers on that front are Azaros or owe the Azaros their commissions.\"",
            new[] { "Laurento" }, condition: Active, hooks: From(70));
        t.Line("Laurento", Laurento,
            "\"And refuse, and Pales and the whole Alliance will say that Rizia stages incidents to start its wars. While we are already " +
            "fighting one, Your Majesty, that is a reputation we cannot afford.\"",
            new[] { "#71" });
        t.Register();
    }

    // ================================================================== c375 Lucita's update

    private static void LucitaUpdate()
    {
        const string conv = "Rizia/Turn 6/P_LucitaAzaroUpdate";
        var t = new Talk(conv, "ZilleWar.E6.Lucita.");

        // "I didn't want to risk further jeopardizing the return of Zille."
        t.Variant("OneWarActive", 274, new[] { 273 }, Active, Romus,
            "\"I love a good battle as much as the next chap. But one war at a time. Our boys are still in Zille.\"", new[] { "OneWarActiveA" });
        t.Line("OneWarActiveA", Lucita, "\"Smolak is getting the message about how Rizia deals with misbehaving neighbors. The hard way.\"",
            new[] { "#284" });
        t.Variant("OneWarWon", 274, new[] { 273 }, Liberated, Romus,
            "\"We had only just taken Zille back. I wasn't going to gamble it on Pales.\"", new[] { "OneWarWonA" });
        t.Line("OneWarWonA", Lucita, "\"We took Zille. Pales knows it. Why do you think Reinhart is so polite these days?\"", new[] { "#284" });
        t.Variant("OneWarLost", 274, new[] { 273 }, LostOrStopped, Romus,
            "\"After Zille, the army needed time. Not another war.\"", new[] { "OneWarLostA" });
        t.Logic("OneWarLostA", new[] { "OneWarLostA1", "OneWarLostCease", "OneWarPartA" });
        t.Line("OneWarLostA1", Lucita, "\"And Smolak would have laughed at us from Wraclawitz if we'd lost twice in one year.\"", new[] { "#284" },
            condition: $"not ({AuditT6.Part}) and {WarFlags.LNot(WarFlags.Ceasefire)}");
        t.Line("OneWarLostCease", Lucita, "\"And Smolak would have laughed at us from Wraclawitz if we'd stopped short twice in one year.\"",
            new[] { "#284" }, condition: $"not ({AuditT6.Part}) and {Ceasefire}");
        t.Line("OneWarPartA", Lucita,
            "\"And Smolak is still sitting behind his armistice line, waiting for us to stumble. Better he waits a long time.\"", new[] { "#284" },
            condition: AuditT6.Part);

        // "How is House Azaro feeling about the fact that we aren't at war right now?"
        t.Variant("AzaroWar", 289, new[] { 287 }, Active, Romus,
            "\"And how is House Azaro feeling about fighting only one war?\"", new[] { "#291" });

        // "My house has begun to complain..."
        t.Line("AzaroComplaint", Lucita,
            "\"They say Azaro regiments are doing the bleeding in Zille while the newspapers thank everyone else.\"",
            new[] { "#859" }, condition: Active, hooks: From(246));
        t.Register();
    }

    // ================================================================== c369 The Aureus deal signing

    private static void DealSigning()
    {
        const string conv = "Rizia/Turn 6/SnO_PalesDealSigning";
        var t = new Talk(conv, "ZilleWar.E6.Signing.");

        // "It is my intention that Rizia continue on the path of peaceful conflict resolution."
        t.Variant("Peace", 271, new[] { 268 }, Active, Romus,
            "\"Even as our soldiers fight to bring Zille home, Rizia shows the world that it makes peace wherever peace is possible. " +
            "We will remain one of the most reliable trading partners in Merkopa.\"", new[] { "#273" });

        // Alvarez collects on the Strait of Kreleck once the cameras are gone (he is present when Rizia did not buy the field).
        const string alvarezHere = "Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Buy\"] ~= true";
        t.Line("Aside", Narrator,
            "After the ceremony, as the guests drifted towards the buffet, Alvarez caught my elbow and steered me to a window, " +
            "away from the photographers.", new[] { "Aside1" },
            condition: $"{Declared} and {LespiaDeal} and {alvarezHere} and {NotSet(AlvarezAsideDone)}",
            script: Set(AlvarezAsideDone, "true") + $"AddConversant(\"{Alvarez}\"); AddConversant(\"{Laurento}\");", hooks: From(374));
        t.Logic("Aside1", new[] { "Aside1On", "Aside1Over", "Aside1Cold" });
        t.Line("Aside1On", Alvarez,
            "\"My admirals have been holding the Strait of Kreleck shut for you, Your Majesty. Wehzek tankers are rusting at anchor in " +
            "Wraclawitz because of it. And today, the Aureus gas starts flowing.\"", new[] { "Aside2" }, condition: Active);
        t.Line("Aside1Over", Alvarez,
            "\"My admirals held the Strait of Kreleck shut for you while your army was in Zille, Your Majesty. Wehzek tankers rusted at " +
            "anchor in Wraclawitz because of it. And today, the Aureus gas starts flowing.\"", new[] { "Aside2" },
            condition: $"{AuditT6.Over} and not {LespiaCold}");
        t.Line("Aside1Cold", Alvarez,
            "\"My admirals kept an eye on the Strait of Kreleck while your army was in Zille, Your Majesty, a good deal less closely " +
            "than I promised you. You will remember the Meftiem. So do I. And today, the Aureus gas starts flowing.\"",
            new[] { "Aside2" }, condition: $"{AuditT6.Over} and {LespiaCold}");
        t.Line("Aside2", Alvarez,
            "\"I don't hold grudges. I hold invoices. I think Rizia can afford to sell some of its share back to its friends at a friendly price. Shall we say three Arcasian Liras per thousand cubic meters? Six units of the Aureus share for six billion guilders, between friends.\"",
            new[] { "Pay", "Caleqabiz", "Refuse" });
        t.Choice("Pay", "\"Rizia pays its debts, Mr. Prime Minister.\"", new[] { "PayA" }, cost: "-6 Energy, +2 Budget",
            script: Set(VarAlvarezGasPrice, "\"paid\"") + Inc("RiziaDLC.Resources_Energy", -6) + Inc("RiziaDLC.Resources_Budget", 2) +
                    Inc("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", 1));
        t.Line("PayA", Alvarez, "\"Safety in numbers, Your Majesty. I knew we would understand each other.\"", new[] { "Laurento" });
        t.Choice("Caleqabiz", "\"Your navy was paid in rights at Caleqabiz. That was the price, and you agreed to it.\"", new[] { "CaleqabizA" },
            condition: "Variable[\"ZilleWar.Lespia_Caleqabiz\"] == true",
            script: Set(VarAlvarezGasPrice, "\"caleqabiz\""));
        t.Line("CaleqabizA", Alvarez,
            "\"So it was. You can't blame a man for asking twice.\" He raised his glass to me, entirely unembarrassed.", new[] { "Laurento" });
        t.Choice("Refuse", "\"Six units of gas for six billion guilders is not friendship, Mr. Prime Minister. It is extortion.\"", new[] { "RefuseA" },
            script: Set(VarAlvarezGasPrice, "\"refused\"") + Inc("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2));
        t.Line("RefuseA", Alvarez,
            "\"Extortion is such an ugly word. I prefer 'terms'. My admirals will be disappointed, Your Majesty. They do get bored at Kreleck.\"",
            new[] { "Laurento" });
        t.Line("Laurento", Laurento,
            "\"He waited until the cameras were gone,\" Laurento murmured once Alvarez had moved on. \"That is Alvarez all over.\"",
            new[] { "#375", "#376" });
        t.Register();
    }

    // ================================================================== c368 The Duke war call

    private static void DukeWarCall()
    {
        const string conv = "Rizia/Turn 6/FPnT_DukeWarPhoneCall";
        var t = new Talk(conv, "ZilleWar.E6.DukeCall.");

        // "I'm sure I don't need to remind you of how much stronger the Rizian Armed Forces are..." -> "And?"
        t.Variant("AndActive", 56, new[] { 49, 53 }, Active, Axel,
            "\"Your armed forces are in Zille, Your Majesty. What exactly is left over for me?\"", new[] { "#48" });
        t.Variant("AndLiberated", 56, new[] { 49, 53 }, Liberated, Axel,
            "He was silent for a long moment. \"I... saw what your army did in Zille. And?\"", new[] { "#48" });
        t.Variant("AndLost", 56, new[] { 49, 53 }, LostOrStopped, Axel,
            "\"Stronger? Wehlen did not seem to think so.\"", new[] { "#48" });

        t.Choice("Smolak", "\"Wiktor Smolak thought he could keep what was ours. Ask him how that turned out.\"", new[] { "SmolakActive", "SmolakWon" },
            condition: $"({Active} or {Liberated}) and {NotSet(AskedSmolakTaunt)}", script: Set(AskedSmolakTaunt, "true"), hooks: From(45));
        t.Line("SmolakActive", Axel,
            "\"Smolak is still holding most of it, last I heard. And your army is still in the mud trying to take it from him.\"",
            new[] { "#48" }, condition: Active);
        t.Line("SmolakWon", Axel,
            "There was a pause on the line. \"Smolak was not ready for you. Pales is.\"", new[] { "#48" }, condition: Liberated);

        t.Variant("Begun", 78, new[] { 88 }, Active, Narrator,
            "And just like that, the Second Pales War had begun. Rizia was at war on two borders.", new[] { "#90" });
        t.Register();
    }

    // ================================================================== c357 The general staff briefing on the invasion of Pales

    private static void GeneralStaff()
    {
        const string conv = "Rizia/Turn 6/SnO_WarEruptionBriefing";
        var t = new Talk(conv, "ZilleWar.E6.Staff.");
        string blockade = "Variable[\"RiziaDLC.Diplomacy_Pales_Military_BlockDrillship\"] == true";

        // After Lucita: "our ground forces will be ready to cross the border within 48 hours."
        t.Line("Q1", Taddeus,
            "\"Before we speak of Pales, Lieutenant, the general staff has one question for you. And I am afraid it is not a military one.\"",
            new[] { "Q2" }, condition: Active, hooks: From(131));
        t.Line("Q2", Carlos,
            "\"The northern army group is committed in Zille, Your Majesty. It holds the line from Mave Nortus to the coast. Every " +
            "division we send south is one fewer there, and Smolak counts our divisions as carefully as we count his.\"",
            new[] { "Q3Blockade", "Q3" });
        t.Line("Q3Blockade", Lucita,
            "\"And the navy cannot be in three places at once. Half the fleet is still around the Aureus field, the rest is hunting " +
            "Wehzek convoys in the north. Whatever I send to the Gulf of Ales comes out of one of them.\"",
            new[] { "Q3Lespia", "Q4" }, condition: blockade);
        t.Line("Q3", Lucita,
            "\"And the navy cannot be in the Strait of Kreleck and the Gulf of Ales at once. Whatever I send south, the Wehzek convoys " +
            "will notice within a week.\"", new[] { "Q3Lespia", "Q4" }, condition: $"not ({blockade})");
        t.Line("Q3Lespia", Lucita,
            "\"The Lespians are keeping Kreleck closed for us, for now. I would not stake a war on Prime Minister Alvarez's patience.\"",
            new[] { "Q4" }, condition: LespiaDeal);
        t.Line("Q4", Taddeus,
            "\"So, Your Majesty. Which war matters more?\"", new[] { "Zille", "Pales", "Both" });
        t.Choice("Zille", "\"Zille. The north keeps everything it has. Pales gets what is left.\"", new[] { "ZilleA" },
            script: Set(VarWarPriority, "\"zille\""));
        t.Line("ZilleA", Taddeus,
            "\"Then we fight Pales with one hand. It has been done before.\" He smiled thinly. \"Badly, usually. But it has been done.\"",
            new[] { "Close" });
        t.Choice("Pales", "\"Pales. Hold the line in the north and send everything you can spare south.\"", new[] { "PalesA" },
            script: Set(VarWarPriority, "\"pales\""));
        t.Line("PalesA", Carlos,
            "\"Then Zille goes over to the defensive, Your Majesty. I will tell the northern commanders to dig in and wait.\"",
            new[] { "Close" });
        t.Choice("Both", "\"Both. I will not choose between them.\"", new[] { "BothA" },
            script: Set(VarWarPriority, "\"both\""));
        t.Line("BothA", Taddeus,
            "\"Your father said very much the same in 1923, Lieutenant. I was there.\" He tapped the map with his pointer. " +
            "\"So we will not fight them both at once. We will sequence them. Zille holds, Pales breaks, then we turn north again.\"",
            new[] { "Close" });
        t.Line("Close", Narrator,
            "Aides moved quietly around the table, shifting silver tokens on the map of Zille and golden ones on the map of Pales. " +
            "Nobody in the room said what everyone was thinking. Rizia had not fought on two borders in living memory.",
            new[] { "#261" });

        // Zille already won before the Pales war.
        t.Line("FreeNorth", Carlos,
            "\"One piece of good news, Your Majesty. With Zille taken, the northern army group can come south. And Pales has heard " +
            "what happened to the Wehzeks. That is worth a division on its own.\"",
            new[] { "#261" }, condition: Liberated, hooks: From(131));

        // Carlos' order of battle, after the tanks.
        t.Line("Northern", Carlos,
            "\"I should add that these figures exclude the northern army group, Your Majesty. Those divisions are committed to Zille " +
            "and cannot be moved without abandoning the front there.\"",
            new[] { "#414" }, condition: Active, hooks: From(410, 411, 412, 413));

        // Lucita's navy, after the submarines.
        t.Line("Subs", Lucita,
            "\"Some of those submarines are still north of the Gulf of Moridia hunting Wehzek tankers. I can bring them south, but " +
            "Smolak will notice the day they leave.\"",
            new[] { "#449" }, condition: Active, hooks: From(446, 447));

        // Lespian destroyers in the Palesian navy, while Lespia closes Kreleck for us.
        t.Line("Customers1", Lucita,
            "\"And yes, Your Majesty, those are the same Lespian shipyards. Alvarez sells us the strait in the north and sells " +
            "Reinhart destroyers in the south.\"", new[] { "Customers2" }, condition: $"{Active} and {LespiaDeal}", hooks: From(167, 189));
        t.Line("Customers1Over", Lucita,
            "\"And yes, Your Majesty, those are the same Lespian shipyards. Alvarez sold us the strait while we fought for Zille, and " +
            "sold Reinhart destroyers all the while.\"", new[] { "Customers2" }, condition: $"{AuditT6.Over} and {LespiaDeal} and not {LespiaCold}", hooks: From(167, 189));
        t.Line("Customers1Cold", Lucita,
            "\"And yes, Your Majesty, those are the same Lespian shipyards. Alvarez promised us the strait while we fought for Zille, " +
            "kept it half open, and sold Reinhart destroyers all the while.\"", new[] { "Customers2" },
            condition: $"{AuditT6.Over} and {LespiaDeal} and {LespiaCold}", hooks: From(167, 189));
        t.Line("Customers2", Taddeus, "\"Lespia does not have allies, Lieutenant. It has customers.\"", new[] { "#177" });
        t.Register();
    }

    // ================================================================== c370 The cemetery: Estela

    private static void Cemetery()
    {
        const string conv = "Rizia/Turn 6/P_Cemetary";
        var t = new Talk(conv, "ZilleWar.E6.Estela.");

        // Pales-war branch: "...when his attention should be focused on the rightful restoration of Zille."
        t.Variant("Know77Active", 77, new[] { 76 }, Active, Estela,
            "\"I know that my son, with half his army already in Zille, has gone looking for a second war in the south.\"", new[] { "#78" });
        t.Variant("Know77Won", 77, new[] { 76 }, Liberated, Estela,
            "\"I know that my son brought Zille home, and instead of savoring it, has gone looking for another war.\"", new[] { "#78" });
        t.Variant("Know77Lost", 77, new[] { 76 }, LostOrStopped, Estela,
            "\"I know that my son could not finish one war in Zille, and now hopes a second one in Pales will make the country forget it.\"",
            new[] { "#78" });

        t.Variant("See257Active", 257, new[] { 256 }, Active, Estela,
            "\"I see a son with one war unfinished in Zille, determined to start another in Pales.\"", new[] { "#258" });
        t.Variant("See257Over", 257, new[] { 256 }, WarFlags.L(WarFlags.Over), Estela,
            "\"I see a son who has had his war over Zille, and has not yet had enough of war.\"", new[] { "#258" });
        t.Variant("Zille255", 255, new[] { 622 }, Active, Estela,
            "\"Your soldiers are still in Zille, Romus! Finish that before you start on Pales!\"", new[] { "#259" });
        t.Variant("Zille623", 623, new[] { 98 }, Active, Estela,
            "\"Relying solely on divine intervention is misguided. Speaking of which, you have one war still unfinished in Zille. Finish it before you start on Pales.\"",
            new[] { "#259" });

        // No Pales war: the Zille vows become present tense.
        t.Variant("Vow182", 182, new[] { 82 }, Active, Romus,
            "\"Our soldiers are in Zille as we speak. I will undo his mistake of giving it away so freely.\"", new[] { "#502" });
        t.Variant("Vow261", 261, new[] { 266 }, Active, Romus,
            "\"Our soldiers are fighting for Zille as we speak. I will finish what he gave away.\"", new[] { "#271" });
        t.Variant("Vow262", 262, new[] { 265 }, Active, Romus,
            "\"Our soldiers are fighting for Zille as we speak. God willing, I will finish what he gave away.\"", new[] { "#273" });
        t.Variant("Vow263", 263, new[] { 269 }, Active, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. That is why I went to war to take it back.\"", new[] { "#272" });
        t.Variant("Vow270", 270, new[] { 268 }, Active, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. That is why I went to war to take it back. Whatever it costs.\"",
            new[] { "#274" });
        t.Variant("Won182", 182, new[] { 82 }, Liberated, Romus,
            "\"Zille is Rizian again. I have undone his mistake of giving it away so freely.\"", new[] { "#502" });
        t.Variant("Won261", 261, new[] { 266 }, Liberated, Romus,
            "\"Zille is ours again. That is how I honor his memory.\"", new[] { "#271" });
        t.Variant("Won262", 262, new[] { 265 }, Liberated, Romus,
            "\"Zille is ours again, God be thanked. That is how I honor his memory.\"", new[] { "#273" });
        t.Variant("Won263", 263, new[] { 269 }, Liberated, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. That is why I went to war to take it back, and why it is ours again.\"",
            new[] { "#272" });
        t.Variant("Won270", 270, new[] { 268 }, Liberated, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. I took it back by force, because there was no other way.\"",
            new[] { "#274" });

        // Estela's grandmother and the Dastnurist sanctuary in Zille.
        t.Line("Sanct1", Estela,
            "\"I wonder whether it still stands. It sat on the hill above the harbor, two streets from where I was born. Your guns are " +
            "close enough now to find out.\"", new[] { "Spare", "NoPromise" }, condition: Active, hooks: From(773));
        t.Choice("Spare", "\"I'll tell the generals to spare the old harbor quarter.\"", new[] { "SpareA" },
            script: Set(VarSpareOldQuarter, "true") + Inc("RiziaDLC.Relations_Estela", 1));
        t.Line("SpareA", Estela, "\"Thank you, darling.\" She squeezed my hand. \"Don't tell your generals why. They'd only laugh.\"", new[] { "#774" });
        t.Choice("NoPromise", "\"I can't promise what the guns will spare, Mother.\"", new[] { "NoPromiseA" });
        t.Line("NoPromiseA", Estela, "\"No. I suppose a king can't.\" She looked away from me for a long moment.", new[] { "#774" });
        t.Line("SanctWon", Estela,
            "\"It's ours again now, isn't it? When the roads are safe, I want to see whether it still stands. Before any minister does.\"",
            new[] { "#774" }, condition: Liberated, hooks: From(773));
        t.Line("SanctLost", Estela,
            "\"And now I suppose I shall never see it again. Not while Smolak's flag flies over the harbor.\"",
            new[] { "#774" }, condition: AuditT6.Lost, hooks: From(773));
        t.Line("SanctPart", Estela,
            "\"It's on our side of the line now, isn't it? When the roads are safe, I want to see whether it still stands. Before any minister does.\"",
            new[] { "#774" }, condition: AuditT6.Part, hooks: From(773));
        t.Register();
    }

    // ================================================================== c373 Before the balcony speech (family)

    private static void BalconyIntro()
    {
        const string conv = "Rizia/Turn 6/Intro Section";
        var t = new Talk(conv, "ZilleWar.E6.Intro.");
        t.Variant("Crucible", 4, new[] { 1 }, Active, Hugo,
            "\"War is a crucible, Romus. You are walking into two of them at once. Not even Queen Lyza fought on two borders. " +
            "Survive both, and no one will ever question House Toras again.\"", new[] { "#5", "#6", "#31", "#42", "#9" });
        t.Variant("Burden", 21, new[] { 23 }, Active, Vina,
            "\"Two wars, Father. Brenas is already burying its sons for Zille, and now Cardesse will bury its own. It was your choice. You've got to bear the burden.\"",
            new[] { "#74" });
        t.Variant("Again", 105, new[] { 104 }, Active, Estela,
            "\"Valero lost Zille with a signature and nearly lost Pales with an army. Listen to me, Romus. Don't you dare lose both.\"",
            new[] { "#106" });
        t.Variant("AgainWon", 105, new[] { 104 }, Liberated, Estela,
            "\"You brought Zille home. Now listen to me, Romus. We can't lose to Pales again.\"", new[] { "#106" });
        t.Register();
    }

    // ================================================================== c372 The balcony speech

    private static void Balcony()
    {
        const string conv = "Rizia/Turn 6/A_PalaceBalconySpeech";
        var t = new Talk(conv, "ZilleWar.E6.Balcony.");

        // "Wehlen, despite years of alliance, refrained from standing by us when it mattered most."
        t.Variant("WehlenPart", 200, new[] { 51 }, AuditT6.Part, Romus,
            "\"Wehlen, which still holds half of Zille behind an armistice line, stood against us once again. It will not hold it forever.\"",
            new[] { "#341" });
        t.Variant("Wehlen", 200, new[] { 51 }, $"{Declared} and not ({Liberated}) and not ({AuditT6.Part})", Romus,
            "\"Wehlen, which still holds our people in Zille by force, stood against us once again. It will not stand for long.\"",
            new[] { "#341" });
        t.Variant("WehlenWon", 200, new[] { 51 }, Liberated, Romus,
            "\"Wehlen, beaten in Zille, spoke against us in Konstantium. Let it. Its words weigh less than its army did.\"",
            new[] { "#341" });

        // "Our peaceful nation now faces war, imposed by our southern neighbor."
        t.Variant("Peaceful", 88, new[] { 90, 91, 92, 93, 94 }, Active, Romus,
            "\"Our nation, already fighting to bring Zille home, now faces a second war, imposed by our southern neighbor.\"",
            new[] { "#148" });

        // A topic among "other subjects": the soldiers already in the north.
        t.Choice("North", "\"In the north, our soldiers are already bringing Zille home. Today I ask this nation to carry a second burden.\"",
            new[] { "NorthA" }, menu: "Speak of the soldiers in Zille.",
            condition: $"{Active} and {NotSet(AskedNorthSpeech)}", script: Set(AskedNorthSpeech, "true"), hooks: From(157));
        t.Line("NorthA", Romus,
            "\"Wiktor Smolak and Axel Reinhart have both mistaken our patience for weakness. Two borders, one kingdom. We will answer them both.\"",
            new[] { "NorthB" });
        t.Line("NorthB", Narrator,
            "The roar that answered me rolled across the gardens and back again. If I was honest with myself, the cheers were louder for Zille than for Pales.",
            new[] { "#442" }, script: Inc("RiziaDLC.Turn06_WarSpeech_Impact", 1));
        t.Choice("NorthWon", "\"In the north, our soldiers have already brought Zille home. What they did to Wehlen, they will do in the south.\"",
            new[] { "NorthWonA" }, menu: "Speak of the victory in Zille.",
            condition: $"{Liberated} and {NotSet(AskedNorthSpeech)}", script: Set(AskedNorthSpeech, "true"), hooks: From(157));
        t.Line("NorthWonA", Narrator,
            "The crowd took up the name before I had finished the sentence. \"Zille! Zille!\" It was some time before I could go on.",
            new[] { "#442" }, script: Inc("RiziaDLC.Turn06_WarSpeech_Impact", 1));
        t.Choice("NorthPart", "\"Our soldiers brought Zille city home, and they still stand guard on the armistice line. Today I ask this nation to carry a second burden.\"",
            new[] { "NorthPartA" }, menu: "Speak of the soldiers in Zille.",
            condition: $"{AuditT6.Part} and {NotSet(AskedNorthSpeech)}", script: Set(AskedNorthSpeech, "true"), hooks: From(157));
        t.Line("NorthPartA", Narrator,
            "The cheer that answered me was loud, and louder still when I named the regiments that had taken Zille city.",
            new[] { "#442" }, script: Inc("RiziaDLC.Turn06_WarSpeech_Impact", 1));
        t.Choice("NorthLost", "\"Our soldiers gave everything for Zille. I will not let their sacrifice be forgotten, and I will ask no less courage of us now.\"",
            new[] { "NorthLostA" }, menu: "Speak of the fallen of Zille.",
            condition: $"{AuditT6.Lost} and {NotSet(AskedNorthSpeech)}", script: Set(AskedNorthSpeech, "true"), hooks: From(157));
        t.Line("NorthLostA", Narrator, "For a moment the crowd was silent. Then, slowly, the applause began, and it did not stop for a long time.",
            new[] { "#442" });
        t.Register();
    }

    // ================================================================== c360 Sal and the religious briefing

    private static void ReligionBriefing()
    {
        const string conv = "Rizia/Turn 6/A_ReligionBriefing";
        var t = new Talk(conv, "ZilleWar.E6.Sal.");

        t.Variant("Context", 741, new[] { 740 }, Active, Romus,
            "\"Our soldiers are already fighting Wehlen in Zille. Tell me the religious context of what I have begun, Sal.\"", new[] { "#461" });
        t.Variant("Troubles", 745, new[] { 744 }, Declared, Romus,
            "\"The war for Zille troubles me more than I let my generals see. Does Wruhecism justify it?\"", new[] { "#86" });
        t.Variant("Divine", 764, new[] { 763 }, Active, Romus,
            "\"How can we know if our war with Wehlen aligns with the divine order?\"", new[] { "#109" });
        t.Variant("Discern", 768, new[] { 767 }, Active, Romus,
            "\"How can we discern whether our war with Wehlen aligns with the text you speak of?\"", new[] { "#488" });
        t.Variant("DivineOver", 764, new[] { 763 }, AuditT6.Over, Romus,
            "\"How can I know whether our war with Wehlen was in accord with the divine order?\"", new[] { "#109" });
        t.Variant("DiscernOver", 768, new[] { 767 }, AuditT6.Over, Romus,
            "\"How can I discern whether our war with Wehlen was in accord with the text you speak of?\"", new[] { "#488" });

        // After the just-war doctrine.
        t.Line("Hearth", Sal,
            "\"St. Wruhec forgave the sword drawn for one's own hearth, Your Majesty. He did not forgive the sword that could not be " +
            "sheathed. Be sure you know where Zille ends.\"", new[] { "#110" },
            condition: $"{Active} and not {PalesWarComing}", hooks: From(109, 488));
        t.Line("TwoSwords", Sal,
            "\"One just war is a heavy enough burden for a soul, Your Majesty. Two is a question the Lord will put to you personally. " +
            "I would have an answer ready.\"", new[] { "#110" },
            condition: $"{Active} and {PalesWarComing}", hooks: From(109, 488));
        t.Line("HearthOver", Sal,
            "\"St. Wruhec forgave the sword drawn for one's own hearth, Your Majesty. He also taught that it must go back into its " +
            "sheath. I would remember both halves of the lesson.\"", new[] { "#110" },
            condition: $"{AuditT6.Over} and not {PalesWarComing}", hooks: From(109, 488));
        t.Line("TwoSwordsOver", Sal,
            "\"One war is a heavy burden for a soul, Your Majesty, and you have carried one already this year. Be sure of your answer " +
            "before you take up a second. The Lord will ask for it.\"", new[] { "#110" },
            condition: $"{AuditT6.Over} and {PalesWarComing}", hooks: From(109, 488));

        // Derdia: "this could anger President Smolak... and further complicate Zille's return."
        t.Variant("Azmal", 680, new[] { 703 }, Declared, Sal,
            "\"Keep in mind, President Smolak is already your enemy. But Supreme Wiseman Azmal is not yet your friend.\"", new[] { "#714" });
        t.Register();
    }

    // ================================================================== news and reports

    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || (GameState.CurrentTurnNum ?? 0) < 6 || !Variables.GetBool(WarFlags.Declared))
        {
            return;
        }

        ANRuling();
        Buildup();
        LespiaBothSides();
        SmolakOnLespia();
        AzaroOfficers();
    }

    private static bool Once(string guard)
    {
        if (Variables.GetBool(guard))
        {
            return false;
        }

        Variables.Set(guard, true);
        return true;
    }

    private static void ANRuling()
    {
        bool navy = Variables.GetBool("RiziaDLCSupport.News_Turn06_G_ANFavoursRiziaNavy");
        bool noNavy = Variables.GetBool("RiziaDLCSupport.News_Turn06_G_ANFavoursRiziaNoNavy");
        if (!(navy || noNavy) || !Once(NewsANRuling))
        {
            return;
        }

        NewsWire.Publish("E6_ANRuling", "G", "Disputed Gas Field Should Be Shared, AN Rules", P(
            "The Alliance of Nations announced its long awaited ruling on the Aureus gas field dispute, ruling that due to the " +
            "overlapping maritime borders of Rizia and Pales, the field should be shared equally between the two countries. The " +
            "committee rejected the Palesian argument that Caleqabiz shouldn't have the same maritime border radius as mainland " +
            "Rizia, as it is to be considered part of Rizia proper. Rumburg, Morella and Derdia backed up this position.",
            (WarFlow.AtWar
                ? "Wehlen, at war with Rizia since the King's declaration in Konstantium, voted against the ruling."
                : "Wehlen, whose war with Rizia over Zille has only just ended, voted against the ruling.") +
            " Its envoy told the committee that \"a kingdom that settles its borders with tanks has no business asking this assembly to " +
            "draw its maps.\"",
            "While some in Rizia are disappointed over not receiving full exploitation rights, the AN stresses that both countries " +
            "should now work together towards peacefully dividing the field and repairing diplomatic relations." +
            (navy
                ? "\n\nThe ruling also contained a passage on the presence of Rizian naval vessels in the vicinity of the gas field, " +
                  "demanding an immediate withdrawal and considering it a clear violation of international law."
                : "")));
    }

    private static void Buildup()
    {
        if (!Variables.GetBool("RiziaDLCSupport.News_Turn06_G_RiziaMilitaryBuildup") || !Variables.GetBool(WarFlags.Active) || !Once(NewsBuildup))
        {
            return;
        }

        NewsWire.Publish("E6_Buildup", "G", "Already at War in Zille, Rizia Masses Troops on Pales Border", P(
            "The entirety of Merkopa is following Rizia's military build up on the Palesian border closely. In recent days, " +
            "multiple divisions are reported to be on the move southwards, with Rizian airplanes flying dangerously close to " +
            "Palesian airspace almost daily, even as the kingdom's northern army remains locked in its war with Wehlen over Zille.",
            "\"No one in Konstantium thought King Romus would open a second front while the first is still burning,\" one diplomat " +
            "told Geopolitico. \"No one in Konstantium thought he would declare the first one, either.\"",
            "Both ATO and CSP have urged Rizia for restraint. Rumburg has defended the Rizian military buildup, highlighting that " +
            "any country has the right to move troops within its own borders as it pleases. Wehzek state radio has welcomed the " +
            "news, telling its listeners that \"every Rizian soldier sent south is a soldier who will not reach Zille.\""));
    }

    private static void LespiaBothSides()
    {
        // The Pales war starts on Turn 6, after the Zille war has ended: past tense.
        if (!Variables.GetBool(WarFlags.PalesWar) || !Once(NewsLespiaBothSides))
        {
            return;
        }

        bool strait = Variables.GetInt("ZilleWar.Lever_Lespia") > 0 && !LespiaColdNow;
        NewsWire.Publish("E6_LespiaBothSides", "G",
            strait ? "Lespian Shipyards Arm Pales After Lespian Navy Guarded Kreleck for Rizia" : "Lespian Armor Reaches Pales as Rizia Goes to War Again",
            P(
                strait
                    ? "Lespian destroyers delivered to the Palesian navy this year will now face the Rizian fleet in the Gulf of Ales, " +
                      "after Lespian warships kept the Strait of Kreleck closed to the Wehzek convoys supplying Zille. Lespia armed Pales " +
                      "while it shut the strait for Rizia, and was, in effect, on both sides of Rizia's two wars."
                    : "Lespian armored vehicles and destroyers delivered to Pales in recent months will now face the Rizian army on " +
                      "the River Alca. Rizia, which fought Wehlen over Zille earlier this year, now fights a second neighbor armed from Perla.",
                "Asked about the apparent contradiction, a spokesman for Prime Minister Patricio Alvarez said that Lespia \"honors " +
                "its contracts with every partner, and expects every partner to honor theirs.\"",
                "Analysts in Konstantium note that LesPower's interest in the Aureus field depends on Pales surviving this war, while " +
                "Lespia's quarrel with Wraclawitz made the strait worth selling. \"Alvarez has always wanted both sides to " +
                "owe him,\" one said."));
    }

    private static void SmolakOnLespia()
    {
        if (!Variables.GetBool("SharedSupport.News_G_WehlenCondemnsLespia") || Variables.GetInt("ZilleWar.Lever_Lespia") <= 0 || LespiaColdNow || !Once(NewsSmolakLespia))
        {
            return;
        }

        // After a liberation Wehlen has no garrison left in Zille.
        string route = ZilleCampaign.Liberated
            ? "cutting the sea route to the garrison Wehlen once kept in Zille"
            : "cutting the sea route between the Wehzek mainland and its garrison in Zille";

        NewsWire.Publish("E6_SmolakLespia", "G", "Smolak: Lespia Lends Its Navy to Rizian Aggression", P(
            "WRACLAWITZ, WEHLEN - President Wiktor Smolak has accused Lespia of \"lending its navy to Rizian aggression\" while " +
            "inviting Arcasian soldiers to the borders of Wehlen, in his sharpest attack yet on the government of Prime Minister " +
            "Patricio Alvarez.",
            "Lespian warships have kept the Strait of Kreleck closed to Wehzek shipping for weeks, " + route + ". \"Perla claims neutrality in the morning and blockades our ports in the " +
            "afternoon,\" Smolak said. \"We will remember which flags flew over the strait.\"",
            "The Lespian foreign ministry declined to comment."));
    }

    private static void AzaroOfficers()
    {
        if (!Variables.GetBool("RiziaDLC.Lucita_Arrested") || !Once(ReportAzaroOfficers))
        {
            return;
        }

        bool atWar = WarFlow.AtWar;
        if (atWar)
        {
            Variables.Set(VarLucitaArrestedMidWar, true);
        }

        NewsWire.Report("E6_AzaroOfficers", "Rizia_City_Montaklar", "Azaro Officers Question Command After Duchess's Arrest", P(
            "The arrest of Duchess Lucita Azaro has caused deep unease among the officers of House Azaro who served on the Zille front. " +
            "Several regimental commanders are reported to have asked the general staff, in writing, who now speaks for " +
            (atWar ? "the war council in Porte Drazon." : "the Zille garrison."),
            "In Montaklar, Azaro retainers gathered outside the family's townhouse through the night. \"The Duchess sent our sons " +
            "north,\" one of them told reporters. \"Now the Crown has sent her to a cell. Who is running this " + (atWar ? "war?\"" : "army?\""),
            (atWar ? "General Carlos Robles Azaro has assumed day-to-day command of operations in the north"
                : ZilleCampaign.Liberated ? "General Carlos Robles Azaro has taken over the army holding Zille"
                : "General Carlos Robles Azaro has taken over the army on the armistice line") +
            " and has called on all officers to \"serve the Crown, whatever their family's grievances.\""));
    }

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);
}
