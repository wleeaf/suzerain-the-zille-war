using SuzerainModdingKit;
using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Turn 10, the epilogue and the endings for a Rizia that went to war over Zille. Liberation reuses vanilla's
/// "Zille returned" content (LateState sets ZilleReturn_Success); this class swaps the lines that cannot be true after
/// a war: Laurento's farewell, the Queen Mother's reproach, the Foreign Affairs folder on Zille, Angelica Sazon's
/// grievance, "Wiktor Smolak sends his regards", the Wehzek tobacco, Wehzek help against unrest, the epilogue titles
/// (the Restorer for a liberated Zille; never the Peaceful; a lost war can make the Frailer), Continued Reign's border
/// item, Estela's last years in Peaceful Power Transition and Exile, and the Civil War's Wehzek offer.
/// </summary>
internal static class LateT10
{
    private const string Executions = "Rizia/Turn 10/Executions";
    private const string Gather = "Rizia/Turn 10/HOUSES GATHER/HOUSES GATHER Conversation";
    private const string CouncilMeeting = "Rizia/Turn 10/CouncilMeeting";
    private const string Announcement = "Rizia/Turn 10/RelationshipAnnouncement";
    private const string ForeignAffairs = "Rizia/Turn 10/Foreign_Affairs/Foreign_Affairs Conversation";
    private const string Sazon = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE SAZON CONVO";
    private const string NatDes = "Rizia/Turn 10/NATIONAL DESTABILIZATION";
    private const string Epilogue = "Rizia/Epilogue";
    private const string Continued = "Rizia/Continued Reign";
    private const string Peaceful = "Rizia/Peaceful Power Transition";
    private const string Exile = "Rizia/Exile";
    private const string CivilWar = "Rizia/Civil War";

    private const string Laurento = "Laurento Esquibel";
    private const string Narr = Talk.Narrator;
    private const string R = Talk.Romus;

    private const string VarRamiroNews = "ZilleWar.Late_News_Ramiro";
    private const string VarSazonReport = "ZilleWar.Late_Report_SazonLevies";
    private const string VarNantaReport = "ZilleWar.Late_Report_Nanta";

    public static void Register()
    {
        foreach (string v in new[] { VarRamiroNews, VarSazonReport, VarNantaReport })
        {
            Variables.Register(v);
        }

        RegisterTurn10();
        RegisterEpilogue();
        RegisterEndings();
    }

    /// <summary>One-shot news and reports that follow choices made in Turns 8 and 9.</summary>
    public static void OnStep()
    {
        if (!LateState.Declared)
        {
            return;
        }

        int turn = LateState.Turn;
        if (turn >= 8 && Variables.GetString(LateT8.VarRamiro) == "seized" && !Variables.GetBool(VarRamiroNews))
        {
            Variables.Set(VarRamiroNews, true);
            NewsWire.Publish("Late_RamiroSeized", "G", "Rizian Troops Storm Lespian Consulate in Zille", string.Join("\n\n",
                "Rizian soldiers forced their way into the Lespian consulate on the harbor front of Zille city and arrested a Lespian political " +
                "officer, Héctor Ramiro, whom Porte Drazon accuses of organizing the 1950 Friendship Day bombing.",
                "Lespia has recalled its ambassador and called the raid \"an act of piracy against diplomatic soil.\" Kyrute reminded both " +
                "governments of their obligations under the conventions on consular immunity. Rizia has not said what was in the consulate's safe."));
        }

        if (turn >= 10 && LateState.Liberated && !Variables.GetBool(VarSazonReport) && Variables.GetBool("RiziaDLC.ZilleOwnership_Brenas"))
        {
            Variables.Set(VarSazonReport, true);
            NewsWire.Report("Late_SazonLevies", "Rizia_City_Zille", "Sazon Levies Parade in Zille",
                "House Sazon's levies, many of them veterans of the Zille campaign, marched through Zille city to mark the province's return " +
                "to Brenas. The men carried their own colors, not the Crown's, and the crowd cheered Duke Sazon's name louder than the King's. " +
                "Officers at the garrison note that the levies kept their rifles after the war, and have not been asked to hand them in.");
        }

        if (turn >= 10 && LateState.Liberated && !Variables.GetBool(VarNantaReport))
        {
            Variables.Set(VarNantaReport, true);
            string nanta = Variables.GetString(LateT9.VarNanta);
            // no deal, no threat and no trust (the King hung up, or never took the call): nothing happened in Nanta worth a report
            if (nanta is "deal" or "threat" or "trust")
            NewsWire.Report("Late_Nanta", "Rizia_City_Nanta", nanta == "threat" ? "Wehzek Leaflets in Nanta" : "Nanta Quiet",
                nanta == "deal"
                    ? "The last Wehzek prisoners of war crossed the strait from Nanta this week. Since then the city has been calm, and the leaflets " +
                      "that used to appear on market days have stopped. Local officials credit the King's understanding with President Smolak."
                    : nanta == "trust"
                    ? "The leaflets printed in Wehlen that used to appear in the Wehzek quarter of Nanta on market days have stopped. The garrison " +
                      "reports a calm month. Local officials credit the Crown's policy towards the city's Wehzeks."
                    : "Leaflets printed in Wehlen have appeared in the Wehzek quarter of Nanta, calling on \"the Wehzeks of Zille\" to refuse Rizian " +
                      "identity papers. The garrison has doubled its patrols. Nobody has claimed responsibility, but the paper is Wehzek army stock.");
        }
    }

    // ------------------------------------------------------------------ Turn 10

    private static void RegisterTurn10()
    {
        var ex = new Talk(Executions, "ZilleWar.Late.Executions.");
        Swap(ex, Executions, "Regards", 135, new[] { 113 }, R, new S(
            Ongoing: "\"Zille sends its regards.\"", Lib: "\"Zille sends its regards.\"", Part: "\"Zille sends its regards.\"",
            Lost: "\"Zille has no regards to send, after what it took from us.\""), new[] { "#146" });
        ex.Register();

        var g = new Talk(Gather, "ZilleWar.Late.Gather.");
        Swap(g, Gather, "Tobacco", 894, new[] { 892 }, "Russello Montoro", new S(
            Ongoing: "\"We can't import these. Something about a war. Ring any bells?\"",
            Part: "\"Can't sell these at home just yet. The army still treats every truck out of Zille like a smuggler's. But I'm working on it.\"",
            Lost: "\"We can't import these. Something about a war. Ring any bells?\""), new[] { "#895" });
        g.Register();

        var c = new Talk(CouncilMeeting, "ZilleWar.Late.Council10.");
        Swap(c, CouncilMeeting, "Regret", 55, new[] { 53 }, Laurento, new S(
            Ongoing: "\"Of course, I deeply regret that diplomacy was never given its chance in Zille. I hope the guns fall silent there soon.\"",
            Lost: "\"Of course, I deeply regret that diplomacy was never given its chance in Zille. Konstantium will remember what we did there for a long time.\""),
            new[] { "#112" });
        // divided: the cities on each side, by name
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) => Swap(c, CouncilMeeting, "Regret" + id, 55, new[] { 53 }, Laurento, new S(
            Part: $"\"{ours} {Are(ours)} ours, and {theirs} {be} still behind a line of trenches. That is the hardest kind of peace to keep. I regret I will not be here to help keep it.\"",
            PartBorder: $"\"{ours} {Are(ours)} ours, and {theirs} {be} across a border, with a treaty to keep it. That is still the hardest kind of peace to keep. I regret I will not be here to help keep it.\""),
            new[] { "#112" }, when: cond));
        // a divided war whose treaty handed over the last Wehzek city (Auremina): all of Zille is ours by treaty
        Swap(c, CouncilMeeting, "RegretAll", 55, new[] { 53 }, Laurento, new S(PartBorder:
            "\"All of Zille is ours, and a treaty keeps it so. Smolak signed it, but he will not forget it. I regret I will not be here to help keep that peace.\""),
            new[] { "#112" }, when: AllHeld);
        Swap(c, CouncilMeeting, "Feat", 111, new[] { 109 }, Laurento, new S(Lib:
            "\"For my part, I would have preferred that Zille come back to us with a signature rather than a rifle. But it has come back, and I will not pretend that I am sorry.\""),
            new[] { "#40" });
        c.Register();

        var a = new Talk(Announcement, "ZilleWar.Late.Announcement.");
        Swap(a, Announcement, "OneJob", 353, new[] { 351 }, "Estela Toras", new S(
            Ongoing: "\"To add insult to injury, my Zille is still a battlefield. If I may say so, dear... you had one job!\"",
            Part: "\"And Zille cut up like a cake at a christening. At least my old street is on the right side of the line, no thanks to anyone in this room.\"",
            Lost: "\"To add insult to injury, you started a war for Zille, my Zille, and lost it. You had one job, dear!\""), new[] { "#317" });
        a.Register();

        var f = new Talk(ForeignAffairs, "ZilleWar.Late.FA.");
        Swap(f, ForeignAffairs, "Unchanged", 227, new[] { 203 }, Narr, new S(
            Lib: "\"Zille has been under Rizian administration since the liberation. The Wehzek garrison is gone from the harbor, and Wraclawitz protests the loss every week.\"",
            Ongoing: "\"The war in Zille goes on. Neither army has moved the line in months, and both are losing men to it every week.\""),
            new[] { "#916" });
        Swap(f, ForeignAffairs, "UnchangedLost", 227, new[] { 203 }, Narr, new S(
                Lost: "\"Zille remains under Wehzek occupation. Since the war, President Smolak has doubled the garrison and fortified the hills above the city.\""),
            new[] { "#916" }, when: $"not ({WarFlags.L(LateT9.VarRecognized)})");
        Swap(f, ForeignAffairs, "UnchangedRecognized", 227, new[] { 203 }, Narr, new S(
                Lost: "\"Zille is Wehzek by treaty now. The garrison is Smolak's, and the hills above the city are fortified against any return.\""),
            new[] { "#916" }, when: WarFlags.L(LateT9.VarRecognized));
        // divided: the cities on each side of the line, by name (Zille city is always ours)
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) => Swap(f, ForeignAffairs, "UnchangedLine" + id, 227, new[] { 203 }, Narr, new S(
                Part: $"\"The armistice line still runs through Zille. {ours} {Remain(ours)} under our administration, while {theirs} {Stay(theirs)} in Wehzek hands. The Alliance's observers report violations by both sides.\"",
                PartBorder: $"\"Zille now has a border, with a treaty to keep it. {ours} {Remain(ours)} under our administration, while {theirs} {Lie(theirs)} across it. The Alliance's observers report few incidents.\""),
            new[] { "#916" }, when: cond));
        Swap(f, ForeignAffairs, "UnchangedAll", 227, new[] { 203 }, Narr, new S(PartBorder:
            "\"Zille now has a border at the strait, with a treaty to keep it. All four of its cities are under our administration. The Alliance's observers report few incidents.\""),
            new[] { "#916" }, when: AllHeld);
        Swap(f, ForeignAffairs, "Crackdown", 205, new[] { 917 }, Narr, new S(
            Lib: "\"Rizian military police are hunting down the Wehzek agents and the collaborators they left behind in Zille. The arrests are quiet, and the lists are long.\"",
            Ongoing: "\"Behind the Wehzek lines, the military police are hunting anyone suspected of helping our army.\"",
            Part: "\"On the Wehzek side of the line, the military police are hunting down those who cheered our columns when they first crossed the river.\"",
            Lost: "\"Wehzek military police are detaining the Zilleans who flew our colors during the war. We estimate several hundred arrests.\""),
            new[] { "#920" });
        f.Register();

        var s = new Talk(Sazon, "ZilleWar.Late.Sazon.");
        Swap(s, Sazon, "Smolak1275", 1275, new[] { 1268, 1271 }, "Angelica Sazon", new S(
            Ongoing: "\"Yes, I know. A war for Zille, and still no end to it. Brenas sends its sons, and the Crown sends telegrams.\"",
            Lost: "\"Yes, I know. None of us could have anticipated that Your Majesty would start a war for Zille and lose it. Brenas buried its sons for nothing.\""),
            new[] { "#1276" });
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) => Swap(s, Sazon, "Smolak1275" + id, 1275, new[] { 1268, 1271 }, "Angelica Sazon", new S(
            Part: $"\"Your Majesty took {ours} and left {theirs} to Smolak. Brenas expected more for its sons.\""), new[] { "#1276" }, when: cond));
        Swap(s, Sazon, "Smolak1275All", 1275, new[] { 1268, 1271 }, "Angelica Sazon", new S(PartBorder:
            "\"Zille is ours again, though it took a war and a treaty to do it. Brenas paid for both with its sons.\""), new[] { "#1276" }, when: AllHeld);
        Swap(s, Sazon, "Losing1162", 1162, new[] { 1161 }, "Angelica Sazon", new S(
                Lib: "\"Especially after Zille. Our sons won it back, and the Crown has still not thanked them.\"",
                Lost: "\"Especially after the disappointment in Zille.\""),
            new[] { "#1264" });
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) => Swap(s, Sazon, "Losing1162" + id, 1162, new[] { 1161 }, "Angelica Sazon", new S(
                Part: $"\"Especially after Zille. Our sons won back {ours}, and {theirs} still {Sit(theirs)} behind a line of trenches.\"",
                PartBorder: $"\"Especially after Zille. Our sons won back {ours}, and {theirs} {Lie(theirs)} beyond a border, with a treaty to keep it.\""),
            new[] { "#1264" }, when: cond));
        Swap(s, Sazon, "Losing1162All", 1162, new[] { 1161 }, "Angelica Sazon", new S(PartBorder:
            "\"Especially after Zille. Our sons won it back, and the Crown has still not thanked them.\""), new[] { "#1264" }, when: AllHeld);
        s.Register();

        var n = new Talk(NatDes, "ZilleWar.Late.NatDes.");
        var aid = new S(
            Ongoing: "\"And Wehlen would sooner send troops against us than for us, after what happened in Zille.\"",
            Lib: "\"And Wehlen would sooner send troops against us than see us keep Zille.\"",
            Part: "\"And Wehlen would sooner send troops against us than for us, after what happened in Zille.\"",
            Lost: "\"And Wehlen would sooner send troops against us than for us, after what it did in Zille.\"");
        Swap(n, NatDes, "Wehlen7", 7, new[] { 108 }, Laurento, aid, new[] { "#683" });
        Swap(n, NatDes, "Wehlen105", 105, new[] { 107 }, Laurento, aid, new[] { "#683" });
        n.Register();
    }

    /// <summary>Divided, and every Zille city Rizian: the Mave Nortus Treaty handed over the last one (Auremina).</summary>
    private static readonly string AllHeld = And(Part, Front.LWehzekExactly(Array.Empty<string>()));

    // verbs for a list of cities ("Zille city is", "Zille city and Nanta are")
    private static bool Many(string cities) => cities.Contains(" and ");
    private static string Are(string cities) => Many(cities) ? "are" : "is";
    private static string Remain(string cities) => Many(cities) ? "remain" : "remains";
    private static string Stay(string cities) => Many(cities) ? "stay" : "stays";
    private static string Lie(string cities) => Many(cities) ? "lie" : "lies";
    private static string Sit(string cities) => Many(cities) ? "sit" : "sits";

    // ------------------------------------------------------------------ the epilogue titles

    private static void RegisterEpilogue()
    {
        var t = new Talk(Epilogue, "ZilleWar.Late.Epilogue.");
        // after the Conqueror check: Zille won back by war
        t.Logic("Restorer", new[] { "#94" }, condition: Lib, script: Set(LateState.VarTitleRestorer, "true"), hooks: Talk.From(40));
        // never the Peaceful after a declaration of war
        t.Logic("NotPeaceful", new[] { "#58" }, condition: War, hooks: Talk.From(54));
        // a lost war for Zille, with a weak army, is remembered as frailty
        t.Logic("Frailer", new[] { "#94" },
            condition: And(Lost, "Variable[\"RiziaDLC.Situation_Diplomacy_PowerProjection_Weak\"] == true"),
            script: Set("RiziaDLC.Title_Frailer", "true"), hooks: Talk.From(62));
        t.Register();
    }

    // ------------------------------------------------------------------ the endings

    private static void RegisterEndings()
    {
        // Turn 9's call with Smolak (LateT9): a line made permanent, or Wehzek Zille recognized on paper, is remembered here.
        string permanent = WarFlags.L(LateT9.VarLinePermanent);
        string recognized = WarFlags.L(LateT9.VarRecognized);
        var cr = new Talk(Continued, "ZilleWar.Late.Continued.");
        Swap(cr, Continued, "Border", 453, new[] { 447 }, Narr, new S(
            Lib: "With Zille back under the Crown, the troublemakers on your northern border had mostly gone quiet, though a few clashes had already occurred.\nYour advice was to...",
            Ongoing: "The war in Zille had settled into a long, grinding stalemate, and the people of Brenas had stopped reading the casualty lists.\nYour advice was to..."),
            new[] { "#200", "#199", "#201" });
        Swap(cr, Continued, "BorderLine", 453, new[] { 447 }, Narr, new S(
            Part: "The armistice line in Zille had held, barely. Wehzek patrols probed it every month, and President Smolak demanded Zille city back at every session of the Alliance.\nYour advice was to...",
            Lost: "Since the failed war, the Wehzek border guards in Zille had grown bold. Shells had landed on Rizian farms along the line twice that spring.\nYour advice was to..."),
            new[] { "#200", "#199", "#201" }, when: $"not ({permanent}) and not ({recognized})");
        // how the line or the recognition came about: the treaty (ZilleTreaty) or Smolak's Turn 9 call
        string kind = $"Variable[\"{ZilleTreaty.VarKind}\"]";
        string byTreaty = $"{kind} == \"border\" or {kind} == \"recognized\"";
        Swap(cr, Continued, "BorderSigned", 453, new[] { 447 }, Narr, new S(
            Part: "The line through Zille was a border now, agreed on a telephone call from Monqiz and drawn up by the Alliance. Smolak had put up his sign, in both languages. The Sazons spat when they passed it.\nYour advice was to...",
            Lost: "Zille was Wehzek on paper now, signed away for good in exchange for the accounts Wehlen had frozen. The border was quiet. Brenas was not, and never forgave it.\nYour advice was to..."),
            new[] { "#200", "#199", "#201" }, when: $"(({permanent}) or ({recognized})) and not ({byTreaty})");
        Swap(cr, Continued, "BorderTreaty", 453, new[] { 447 }, Narr, new S(
            Part: "The line through Zille was a border now, signed in a schoolhouse near Mave Nortus under the Alliance's flag. Smolak had put up his sign, in both languages. The Sazons spat when they passed it.\nYour advice was to...",
            Lost: "Zille was Wehzek on paper now, recognized in Wraclawitz on the terms Smolak had written himself. The border was quiet. Brenas was not, and never forgave it.\nYour advice was to..."),
            new[] { "#200", "#199", "#201" }, when: $"(({permanent}) or ({recognized})) and ({byTreaty})");
        cr.Register();

        string homeTaken = WarFlags.L(LateT8.VarEstelaHome);
        var pt = new Talk(Peaceful, "ZilleWar.Late.Peaceful.");
        Swap(pt, Peaceful, "Mother", 468, new[] { 466 }, Narr, new S(
            Lib: "Your mother, feeling homesick, went back to her old house in the harbor district of Zille, which the Crown had kept in the family. You saw her off, promising to visit as often as you could.\nYour focus turned to...",
            Ongoing: "Your mother, feeling homesick, asked every month whether the war in Zille was over yet. One day she stopped asking.\nYour focus turned to...",
            Part: "Your mother, feeling homesick, went back to her old house in the harbor district of Zille, a few miles from the armistice line. You saw her off, promising to visit as often as you could.\nYour focus turned to...",
            PartBorder: "Your mother, feeling homesick, went back to her old house in the harbor district of Zille, a few miles from the new border. You saw her off, promising to visit as often as you could.\nYour focus turned to...",
            Lost: "Your mother, feeling homesick, petitioned the Wehzek government to let her spend her remaining years in her old house in the harbor district of Zille. The answer took four months and was one line long. She did not ask again.\nYour focus turned to..."),
            new[] { "#280", "#281", "#282", "#396" }, when: $"not ({homeTaken})");
        // the house Smolak asked for in Wraclawitz (LateT8), and got
        Swap(pt, Peaceful, "MotherHouse", 468, new[] { 466 }, Narr, new S(
            Lost: "Your mother, feeling homesick, asked the Wehzek government only to let her see her old garden in Zille once more. Smolak's son lived in the house now; you had signed it over in Wraclawitz. The answer was a single line. She did not ask again.\nYour focus turned to..."),
            new[] { "#280", "#281", "#282", "#396" }, when: homeTaken);
        pt.Register();

        var ex = new Talk(Exile, "ZilleWar.Late.Exile.");
        Swap(ex, Exile, "Mother", 544, new[] { 537 }, Narr, new S(
            Lib: "Even when she was surrounded by loving family and friends, you could tell that Estela never truly felt at ease in her new home.\nWith her health beginning to fail, she went back to the harbor district of Zille, where she grew up, and asked for nothing more than a room with a view of the sea. She told you she hoped you'd find peace. You...",
            Ongoing: "Even when she was surrounded by loving family and friends, you could tell that Estela never truly felt at ease in her new home.\nShe followed the war in Zille in the newspapers every morning, and told you, the last time you saw her, that she hoped you'd find peace. You...",
            Part: "Even when she was surrounded by loving family and friends, you could tell that Estela never truly felt at ease in her new home.\nWith her health beginning to fail, she went back to her old house in the harbor district of Zille, on the Rizian side of the armistice line. As she boarded the boat, she told you she hoped you'd find peace. You...",
            PartBorder: "Even when she was surrounded by loving family and friends, you could tell that Estela never truly felt at ease in her new home.\nWith her health beginning to fail, she went back to her old house in the harbor district of Zille, on the Rizian side of the new border. As she boarded the boat, she told you she hoped you'd find peace. You...",
            Lost: "Even when she was surrounded by loving family and friends, you could tell that Estela never truly felt at ease in her new home.\nWith her health beginning to fail, she petitioned Wehlen to let her live out her last days in the harbor district of Zille, where she grew up. Wehlen refused the mother of the king who had invaded it. She read the refusal twice, and told you she hoped you'd find peace. You..."),
            new[] { "#545", "#546", "#604" });
        ex.Register();

        var cw = new Talk(CivilWar, "ZilleWar.Late.CivilWar.");
        cw.Line("Wehlen", Narr,
            "Unwilling to choose sides in the conflict, the majority of the international community refused to intervene on your behalf at this time.\n" +
            "President Smolak did not offer you troops. He sent them to the Zille border instead, to protect the Zilleans if the fighting spread north.",
            new[] { "#23" }, condition: NotLib, script: Add("RiziaDLC.Epilogue_CivilWar_WinPoints", -1), hooks: Talk.From(585));
        cw.Line("WehlenLib", Narr,
            "Unwilling to choose sides in the conflict, the majority of the international community refused to intervene on your behalf at this time.\n" +
            "In Nanta, Wehzek irregulars armed from across the strait rose against the Rizian garrison.",
            new[] { "#23" }, condition: And(Lib, $"Variable[\"{LateT9.VarNanta}\"] ~= \"deal\" and Variable[\"{LateT9.VarNanta}\"] ~= \"trust\""), script: Add("RiziaDLC.Epilogue_CivilWar_WinPoints", -1), hooks: Talk.From(585));
        cw.Line("WehlenLibDeal", Narr,
            "Unwilling to choose sides in the conflict, the majority of the international community refused to intervene on your behalf at this time.\n" +
            "In Nanta, the Wehzek quarter kept its word and its shutters closed.",
            new[] { "#23" }, condition: And(Lib, $"Variable[\"{LateT9.VarNanta}\"] == \"deal\""), script: Add("RiziaDLC.Epilogue_CivilWar_WinPoints", -1), hooks: Talk.From(585));
        cw.Line("WehlenLibTrust", Narr,
            "Unwilling to choose sides in the conflict, the majority of the international community refused to intervene on your behalf at this time.\n" +
            "In Nanta, the Wehzek quarter kept its shutters closed.",
            new[] { "#23" }, condition: And(Lib, $"Variable[\"{LateT9.VarNanta}\"] == \"trust\""), script: Add("RiziaDLC.Epilogue_CivilWar_WinPoints", -1), hooks: Talk.From(585));
        Vanilla.AppendScript(CivilWar, 382,
            $"if {War} then Variable[\"RiziaDLCText.ZilleWarAlly\"] = \"The Zilleans who had cheered your soldiers into Zille city fought by your side as well.\" end");
        cw.Register();
    }
}
