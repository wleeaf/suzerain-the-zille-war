using SuzerainModdingKit;
using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Audit of the vanilla story of Turn 8 against the Zille war (see <see cref="Audit"/>). By Turn 8 the war is always over:
/// liberated, divided (Zille city ours, the rest behind an armistice line) or lost, with or without the Pales war. LateT8 and
/// SweepMiddleT8 hold the earlier Turn 8 work; this file adds what the line-by-line pass found missing:
/// - the security brief (c409): the briefing's agenda ("the transfer of Zille to Rizian governance", "Zille's impending
///   reunification"), the evidence discussion (no intelligence sharing with an enemy, no "Zille's return in the bag"), and
///   Lucita's boast about the army after a lost war; the war brief itself is LateT8's;
/// - Rusty on the train (c416): the Lespia evidence "puts Zille in your hands", "good luck in Zille" on the way to Wraclawitz;
/// - the House of Delegates (c422): after a divided or lost war the RPP and RNC openings no longer talk about Smolak's
///   referendum and the Alliance restoring Zille, and the governance debate knows what Rizia holds and whether the King
///   already settled who governs Zille in the Turn 7 session (EarlyT7);
/// - Sal's Wehzek timber (c595) after the border closed;
/// - the Voice-style Geopolitico story about Su Omina in a Zille that stayed Wehzek.
/// </summary>
internal static class AuditT8
{
    private const string Brief = "Rizia/Turn 8/SnO_SecurityBriefAndZille";
    private const string Rusty = "Rizia/Turn 8/RustyCall";
    private const string Hod = "Rizia/Turn 8/A_HouseOfDelegates3";
    private const string Religion = "Rizia/Turn 8/Religious_Affairs/Religious_Affairs Conversation";

    private const string Lucita = "Lucita Azaro";
    private const string Titus = "Titus Gordion";
    private const string Taddeus = "Taddeus Azaro";
    private const string Montoro = "Russello Montoro";
    private const string Adarfo = "Adarfo Sotelato";
    private const string Manus = "Manus Sazon";
    private const string Daria = "Daria de Rava";
    private const string R = Talk.Romus;

    /// <summary>Divided or lost: the war is over and Zille is not (all) ours.</summary>
    private static readonly string NotLibOver = $"({Part}) or ({Lost})";

    /// <summary>The King settled who governs Zille in the Turn 7 House session (EarlyT7).</summary>
    private static readonly string AdminDecided =
        $"{V(EarlyT7.VarZilleAdmin)} == \"brenas\" or {V(EarlyT7.VarZilleAdmin)} == \"imperii\"";

    public static void Register()
    {
        RegisterBrief();
        RegisterRusty();
        RegisterHod();
        RegisterReligion();
    }

    public static void OnStep()
    {
        UpdateNews();
    }

    // ------------------------------------------------------------------ c409 the security brief, first half

    private static void RegisterBrief()
    {
        var t = new Talk(Brief, "ZilleWar.A8.Brief.");

        // the agenda: there is no transfer of governance to talk about
        S Agenda(string lead) => new(
            Lib: $"\"{lead}the latest from Zille, now that the army governs it.\"",
            Part: $"\"{lead}the latest from Zille and the armistice line.\"",
            Lost: $"\"{lead}the latest on Zille, and on what Wehlen is doing with it.\"");
        foreach ((int entry, int[] parents, string speaker, string lead, string next) in new[]
                 {
                     (533, new[] { 489, 545, 547 }, Titus, "As you know, we're to go over ", "#1311"),
                     (548, new[] { 491 }, Lucita, "As you know, we're to go over ", "#1310"),
                     (848, new[] { 839 }, Titus, "We're to go over ", "#911"),
                     (909, new[] { 841 }, Titus, "Very well. As you know, we're here to go over ", "#911"),
                     (916, new[] { 840 }, Titus, "Of course. As you know, we're here to go over ", "#911"),
                     (917, new[] { 915 }, Titus, "As you know, we're here to go over ", "#911"),
                     (1655, new[] { 1646 }, Lucita, "Thank you, Titus. As you know, we're to go over ", "#1661"),
                     (1658, new[] { 1657, 1718 }, Lucita, "As you already know, we're to go over ", "#1661"),
                     (1689, new[] { 1688 }, Titus, "As you know, we're to go over ", "#1721"),
                     (1720, new[] { 1650, 1719 }, Titus, "As you already know, we're to go over ", "#1721"),
                 })
        {
            Swap(t, Brief, $"Agenda{entry}", entry, parents, speaker, Agenda(lead), new[] { next });
        }

        S Onward(string lead) => new(
            Lib: $"\"{lead}I suggest we move on to the matter of governing Zille.\"",
            Part: $"\"{lead}I suggest we move on to Zille and the armistice line.\"",
            Lost: $"\"{lead}I suggest we move on to Zille, such as it is.\"");
        foreach ((int entry, int[] parents, string lead, string next) in new[]
                 {
                     (683, new[] { 678, 679, 681, 682 }, "Well, if there's nothing more of the investigation to discuss, General, ", "#684"),
                     (1619, new[] { 1504, 1538, 1539, 1540 }, "Well, if there's nothing more of the investigation to discuss, ", "#1620"),
                     (1625, new[] { 1546, 1547, 1548, 1606 }, "In any case, that's all on my end. ", "#1626"),
                     (1680, new[] { 1675, 1676, 1677, 1679, 1695, 1697, 1698, 1706 }, "Well, if there's nothing left to discuss regarding this matter, ", "#1639"),
                 })
        {
            Swap(t, Brief, $"Onward{entry}", entry, parents, Titus, Onward(lead), new[] { next });
        }

        Swap(t, Brief, "Bombing121", 121, new[] { 117, 707, 708, 713, 714 }, Titus, S.All(
            "\"It goes without saying that everything that has happened in Zille since, the war included, began with the Friendship Day bombing.\""),
            new[] { "#134" });

        // Lespia's motive
        Swap(t, Brief, "Worked1050", 1050, new[] { 1042 }, R, S.All("\"Damn them, it worked. It ended in a war.\""), new[] { "#1063" });
        Swap(t, Brief, "Question1073", 1073, new[] { 1066 }, Lucita, S.All(
            "\"The question now is what this means for a war we fought over that bomb, not to mention our relations with Lespia.\""), new[] { "#1057" });
        Swap(t, Brief, "Question1074", 1074, new[] { 1067 }, Taddeus, S.All(
            "\"Now the question is what ramifications this has for the war we fought over that bomb and, notably, our relations with Lespia.\""), new[] { "#1076" });
        Swap(t, Brief, "Question1075", 1075, new[] { 1072 }, Titus, S.All(
            "\"Now the question is, what does this mean for the war we fought over that bomb, and for our relations with Lespia?\""), new[] { "#1079" });

        // The Turn 4 intelligence cooperation with Wehlen ended with the war: the evidence is not handed to Wraclawitz
        // automatically (vanilla 1082-1084 publish it at once); the councilor says so and the no-sharing path follows.
        string shared = And(War, "Variable[\"RiziaDLC.Decision_Turn04_WehlenInvestigation_Cooperate\"] == true");
        const string noShare = "\"Our intelligence-sharing arrangement with Wehlen died with the declaration of war.\"";
        t.Line("NoShareL", Lucita, noShare, new[] { "#1085" }, condition: shared, hooks: Talk.From(1057));
        t.Line("NoShareTa", Taddeus, noShare, new[] { "#1086" }, condition: shared, hooks: Talk.From(1076));
        t.Line("NoShareTi", Titus, noShare, new[] { "#1087" }, condition: shared, hooks: Talk.From(1079));

        // after "Wehlen will know we were mistaken / lying" (the arrested scapegoat)
        Swap(t, Brief, "Bag1138", 1138, new[] { 1321, 1362 }, R, new S(
            Lib: "\"It doesn't matter. Zille is already ours.\"",
            Part: "\"It doesn't matter. Smolak has been calling us liars since the war began.\"",
            Lost: "\"It doesn't matter. Smolak has been calling us liars since the war began.\""), new[] { "#1368" });
        Vanilla.Require(Brief, 1139, Peace); // "...ruin our chances for a peaceful Zille return?"
        Swap(t, Brief, "Cleared1140", 1140, new[] { 1321, 1362 }, R, S.All(
            "\"But still, Rizia's name will be cleared. Let Smolak explain why he kept Zille over a bomb that Lespia planted.\""), new[] { "#1368" });

        // keep the evidence as leverage, or not
        Swap(t, Brief, "Lever1141", 1141, new[] { 1111, 1114 }, R, new S(
            Lib: "\"I like that idea. Zille is ours already. Let the evidence work for us somewhere else.\"",
            Part: "\"I like that idea. It may be worth more to us at the armistice table than in the newspapers.\"",
            Lost: "\"I like that idea. It may be worth more to us at the armistice table than in the newspapers.\""), new[] { "#1368" });
        Swap(t, Brief, "Lever1142", 1142, new[] { 1111, 1114 }, R, S.All(
            "\"The leverage isn't worth it. Too many of our soldiers died over that bomb.\""), new[] { "#1368" });
        Swap(t, Brief, "Lever1143", 1143, new[] { 1111, 1114 }, R, new S(
            Part: "\"Would publishing it help us get the rest of Zille back?\"",
            Lost: "\"Would publishing it help us get Zille back?\""), new[] { "#1368" });
        Vanilla.Require(Brief, 1143, $"not ({Lib})");

        // Lucita's offer to close the House: the army is not "stronger than ever" right after losing Zille
        Swap(t, Brief, "Army1870", 1870, new[] { 1867 }, Lucita, new S(
            Lost: "\"Zille cost us dearly. But thanks to you, our armies are still the strongest Rizia has ever had.\""), new[] { "#1869" });
        t.Register();
    }

    // ------------------------------------------------------------------ c416 Rusty on the train

    private static void RegisterRusty()
    {
        var t = new Talk(Rusty, "ZilleWar.A8.Rusty.");
        Swap(t, Rusty, "Smile540", 540, new[] { 881, 884, 886 }, Montoro, new S(
            Lib: "\"Do me a favor, at least. Take a picture of old Smolak's face when he has to sit across from you in his own strait.\"",
            Part: "\"Do me a favor, at least. Take a picture of old Smolak's face when he looks at that armistice line.\"",
            Lost: "\"Do me a favor, at least. Don't let old Smolak keep the pen when he's done with you.\""), new[] { "#486" });

        // Rusty's evidence against Lespia, in a Rizia that already fought the war the bomb started
        Swap(t, Rusty, "Hands606", 606, new[] { 595, 602 }, Montoro, S.All(
            "\"Because it'd prove Lespia's bomb started your war. Try telling the Alliance you're the villain after that.\""), new[] { "#633" });
        Swap(t, Rusty, "Hands609", 609, new[] { 594 }, Montoro, S.All(
            "\"Please, Your Majesty. This would prove Lespia's bomb started your war.\""), new[] { "#633" });
        Swap(t, Rusty, "Hands610", 610, new[] { 607 }, Montoro, S.All(
            "\"I'll help you, Your Majesty. With this, nobody will call your war anything but justified.\""), new[] { "#633" });
        Swap(t, Rusty, "Leap956", 956, new[] { 955 }, Montoro, S.All(
            "\"I'm willing to turn on Lespia and hand you proof that its bomb started your war. All so you and I can keep pushing Rizia to economic greatness.\""),
            new[] { "#963" });
        Swap(t, Rusty, "Pass999", 999, new[] { 1031 }, Montoro, S.All(
            "\"But you're sure you want to pass up the information I'm offering? It'll show the whole world who really started your war.\""),
            new[] { "#1025", "Pass1019", "#1021" });
        t.Choice("Pass1019", "\"I don't need that evidence to justify my war.\"", new[] { "#1029" });

        // the trip goes to the armistice commission or to Wraclawitz, not into Zille
        foreach ((string name, int entry, int[] parents, string[] next) in new[]
                 {
                     ("Luck653", 653, new[] { 651, 1179 }, new[] { "#681", "#685", "#686", "#687" }),
                     ("Luck668", 668, new[] { 666 }, new[] { "#679", "#680", "#681", "#685", "#687" }),
                     ("Luck832", 832, new[] { 830, 831, 833, 834, 837, 838 }, new[] { "#283" }),
                 })
        {
            Swap(t, Rusty, name, entry, parents, Montoro, new S(Part: "\"Good luck with Smolak.\"", Lost: "\"Good luck in Wraclawitz.\""), next);
        }

        // Mave Nortus held by Wehlen after the armistice: the train stops short of it
        string maveHeld = WarFlags.L(WarFlags.Occupied("Mave Nortus"));
        Swap(t, Rusty, "Train3", 3, new[] { 719, 720 }, Talk.Narrator, S.All(
            "The greenery of Brenas whizzed past the windows of the royal train as we chugged toward the border. Mave Nortus, where the royal train used to stop, was on the Wehzek side of the armistice line now."),
            new[] { "#8" }, when: maveHeld);
        Swap(t, Rusty, "Pabel23", 23, new[] { 19 }, "Pabel Adria", S.All(
            "\"You'll have to make it quick, I'm afraid. We're reaching the border shortly.\""), new[] { "#36" }, when: maveHeld);

        Swap(t, Rusty, "Onwards716", 716, new[] { 283, 709, 1070 }, Talk.Narrator, new S(
            Part: "The train chugged onwards, toward the armistice line.",
            Lost: "The train chugged onwards, toward the border and the Wehzek flags beyond it."), new[] { "#1086" });
        t.Register();
    }

    // ------------------------------------------------------------------ c422 the House of Delegates

    private static void RegisterHod()
    {
        var t = new Talk(Hod, "ZilleWar.A8.Hod.");

        // Hugo on the way in (LateT8 has his first line): the King's answers
        Swap(t, Hod, "Sugar46", 46, new[] { 40 }, R, new S(
            Part: "\"Don't try to sugarcoat it, Uncle. We lost half of Zille, and now we're losing Pales.\"",
            Lost: "\"Don't try to sugarcoat it, Uncle. We lost Zille, and now we're losing Pales.\""), new[] { "#69" });
        Swap(t, Hod, "Strong85", 85, new[] { 82 }, R, new S(
            Part: "\"As long as our government stays strong, Zille will be ours in the end.\"",
            Lost: "\"As long as our government stays strong, Zille will be ours in the end.\""), new[] { "#86" });

        // The openings after a divided or lost war (LateT8 swaps the first sentence): the speeches that follow talked about
        // Smolak's referendum and the Alliance restoring Zille. Each speaker gets a war version that joins vanilla again.
        t.Line("AdPart", Adarfo, "\"Half a province, for the lives of our sons. The RPP opposed this war from the first day, and we will leave it to others to call that a victory.\"",
            new[] { "AdSplit" }, condition: And(War, Part), hooks: Talk.From(175));
        t.Line("AdLost", Adarfo, "\"The RPP warned this House that a war for Zille would be paid for by miners and farmhands, not by Sazons or de Ravas. We were right, and I take no pleasure in it.\"",
            new[] { "AdSplit" }, condition: And(War, Lost), hooks: Talk.From(175));
        // reform path: Hugo's aside and Daria's hand (vanilla 353); otherwise Adarfo's tirade against the Crown (vanilla 151)
        t.Logic("AdSplit", new[] { "AdReform", "AdTirade" });
        t.Logic("AdReform", new[] { "#353" }, condition: "Variable[\"RiziaDLC.Reform_Path\"] == true");
        t.Line("AdTirade", Adarfo, "\"But we are sadly not surprised. This is a country ruled by a family that gave away an entire region to stay in power, and then sent our sons to take it back.\"",
            new[] { "#151" }, condition: "Variable[\"RiziaDLC.Reform_Path\"] ~= true");

        t.Line("ManPart", Manus, "\"Brenas gave its sons to this war, and Zille city is ours again for it. The rest of Zille is still behind Wehzek wire. It is my hope that the Zilleans on both sides of that wire will one day come home to a Rizia that has changed for the better.\"",
            new[] { "#569" }, condition: And(War, Part), hooks: Talk.From(490));
        t.Line("ManLost", Manus, "\"We sent the sons of Brenas to bring Zille home, and they came back without it. This House owes their families more than speeches. It is my hope that when Zille does come home, it comes home to a Rizia that has changed for the better.\"",
            new[] { "#569" }, condition: And(War, Lost), hooks: Talk.From(490));

        t.Line("DarPart", Daria, "\"Nonetheless, Zille city is free, and the RNC is confident the rest of the region will follow.\"",
            new[] { "#685" }, condition: And(War, Part), hooks: Talk.From(625));
        t.Line("DarLost", Daria, "\"Nonetheless, the RNC will never abandon the claim to Zille. Neither will the Crown.\"",
            new[] { "#685" }, condition: And(War, Lost), hooks: Talk.From(625));

        Swap(t, Hod, "Future271", 271, new[] { 270 }, Daria, new S(
            Part: "\"Zille city is ours again, and the rest of the region will follow in time. We must look to the future and decide how it will be reintegrated.\"",
            Lost: "\"Zille will be Rizian again, whatever Wehlen thinks. We must look to the future and decide how it will one day be reintegrated.\""),
            new[] { "#756" });
        Swap(t, Hod, "Future755", 755, new[] { 754 }, Daria, new S(
            Part: "\"We must look to the future. Zille city is ours, and there is still the question of how it will be governed.\"",
            Lost: "\"We must look to the future. When Zille is ours again, there is still the question of how the region will be reintegrated.\""),
            new[] { "#778" });

        // the governance debate: what Rizia holds, and whether the King already decided it in the Turn 7 session
        const string decided = "His Majesty has already decided who will govern Zille. Not everyone in this chamber has made peace with it.";
        foreach ((string name, int entry, int parent, string lead, string tail, string next) in new[]
                 {
                     ("Gov389", 389, 396, "My colleague in the RPP is missing one important point. ", " May I?", "#552"),
                     ("Gov708", 708, 710, "Thank you, Mr. Sotelato, but I am not finished speaking. ", "", "#1714"),
                     ("Gov769", 769, 771, "Thank you, Mr. Sazon, but I am not finished speaking. ", "", "#713"),
                 })
        {
            Swap(t, Hod, name, entry, new[] { parent }, Daria, new S(Lib: $"\"{lead}{decided}{tail}\""), new[] { next }, when: AdminDecided);
        }

        foreach ((string name, int entry, int parent, string lead, string tail, string next) in new[]
                 {
                     ("Gov398", 398, 397, "My colleague in the RPP is missing one important point. ", " May I?", "#552"),
                     ("Gov712", 712, 711, "Thank you, Mr. Sotelato, but I am not finished speaking. ", "", "#1714"),
                     ("Gov773", 773, 772, "Thank you, Mr. Sazon, but I am not finished speaking. ", "", "#713"),
                 })
        {
            Swap(t, Hod, name + "D", entry, new[] { parent }, Daria, new S(Part: $"\"{lead}{decided}{tail}\""), new[] { next }, when: AdminDecided);
            Swap(t, Hod, name, entry, new[] { parent }, Daria, new S(
                Part: $"\"{lead}Zille city is ours again, but the future governance of Zille remains undecided.{tail}\"",
                Lost: $"\"{lead}Zille is lost for now, not forever, and its future governance remains undecided.{tail}\""),
                new[] { next }, when: $"not ({AdminDecided}) or ({Lost})");
        }

        Swap(t, Hod, "Gov954", 954, new[] { 957 }, Daria, new S(Lib: "\"His Majesty has decided who will govern Zille. Not everyone here has made peace with that.\""),
            new[] { "#959" }, when: AdminDecided);
        Swap(t, Hod, "Gov955D", 955, new[] { 958 }, Daria, new S(Part: "\"His Majesty has decided who will govern Zille. Not everyone here has made peace with that.\""),
            new[] { "#959" }, when: AdminDecided);
        Swap(t, Hod, "Gov955", 955, new[] { 958 }, Daria, new S(
            Part: "\"With Zille city back in our hands, its governance must be decided upon.\"",
            Lost: "\"Zille will be ours again one day, and its governance must be decided upon.\""),
            new[] { "#959" }, when: $"not ({AdminDecided}) or ({Lost})");
        Swap(t, Hod, "Mind964", 964, new[] { 961 }, R, new S(
            Lib: "\"Thank you, Baroness de Rava, but I have already made up my mind on Zille. This House will not make it up again for me.\"",
            Part: "\"Thank you, Baroness de Rava, but I have already made up my mind on Zille. This House will not make it up again for me.\""),
            new[] { "#970" }, when: AdminDecided);

        Swap(t, Hod, "Scars530", 530, new[] { 514, 515, 522, 523, 524, 525 }, Daria, new S(
            Lib: "\"I will add that Zille still bears the scars of the war we fought for it. Its reconstruction must be overseen with the full strength of the Crown.\"",
            Part: "\"I will add that Zille still bears the scars of the war we fought for it. Its reconstruction must be overseen with the full strength of the Crown.\"",
            Lost: "\"I will add that whenever Zille returns, it will bear the scars of the war we fought for it. Its reconstruction must be overseen with the full strength of the Crown.\""),
            new[] { "#294" });
        Swap(t, Hod, "Ours532", 532, new[] { 535 }, Adarfo, new S(
            Part: "\"Your intentions are transparent, Ms. de Rava. Half of Zille isn't even ours, and you're already scheming about how to use its reconstruction to enrich your own family.\""),
            new[] { "#1195" });

        // Adarfo's "might as well remain Wehzek" and the King's "when Wehlen hands Zille back"
        string[] after547 = { "#1020", "#1024", "Res1055_Lib", "Res1055_Part", "Res1055_Lost", "#1028" };
        Swap(t, Hod, "Wehzek547", 547, new[] { 546 }, Adarfo, new S(
            Lib: "\"If we can't do that, Zille might as well have stayed Wehzek.\"",
            Part: "\"If we can't do that, the rest of Zille might as well stay Wehzek.\""), after547);
        Swap(t, Hod, "Res1055", 1055, new[] { 547 }, R, new S(
            Lib: "\"We will resume this conversation once Zille is formally back in the realm. For now, are there any other topics on the agenda?\"",
            Part: "\"We will resume this conversation when the rest of Zille is ours. For now, are there any other topics on the agenda?\"",
            Lost: "\"We will resume this conversation when Zille is ours again. For now, are there any other topics on the agenda?\""), new[] { "#1056" });
        Swap(t, Hod, "Wrest299", 299, new[] { 302 }, R, new S(
            Part: "\"We will consider Zille's reintegration once the rest of it is ours. For now, what else is on the agenda, Mr. Speaker?\""), new[] { "#310" });
        Swap(t, Hod, "Squabble965", 965, new[] { 968 }, R, new S(
            Part: "\"Half of Zille isn't even back in our hands, and you two are already squabbling over it? There are more important things to discuss.\""),
            new[] { "#1084" });
        // Hugo after the session, on Manus's claim to Zille
        string brenas = $"{V(EarlyT7.VarZilleAdmin)} == \"brenas\"";
        string imperii = $"{V(EarlyT7.VarZilleAdmin)} == \"imperii\"";
        Swap(t, Hod, "Nerve1486", 1486, new[] { 1467 }, "Hugo Toras", S.All(
            "\"You already gave the Sazon boy Zille, Your Majesty, in that very House. And still he stands up and asks for more.\""), new[] { "#1487" }, when: brenas);
        Swap(t, Hod, "Nerve1486I", 1486, new[] { 1467 }, "Hugo Toras", S.All(
            "\"The Sazon boy has some nerve, demanding Zille after you settled it for Porte Drazon.\""), new[] { "#1487" }, when: imperii);
        Swap(t, Hod, "Army1500", 1500, new[] { 1498, 1514 }, R, S.All(
            "\"I gave Zille to Brenas. If the Sazons ever raise an army there, I will know exactly whom to blame.\""), new[] { "#1515" }, when: brenas);
        Swap(t, Hod, "Trick1502", 1502, new[] { 1504 }, R, new S(
            Lib: "\"I don't even want to think about this until Zille is formally back in the realm. The army still governs it, and Smolak still has friends there.\""),
            new[] { "#1526" }, when: $"not ({AdminDecided})");
        Swap(t, Hod, "Decided1502", 1502, new[] { 1504 }, R, S.All("\"I have already decided about Zille, Hugo. The House can talk all it likes.\""),
            new[] { "#1526" }, when: AdminDecided);
        Swap(t, Hod, "Decided1506", 1506, new[] { 1505 }, R, new S(Part: "\"I have already decided about Zille, Hugo. The House can talk all it likes.\""),
            new[] { "#1526" }, when: AdminDecided);

        // Manus's hope for the Zilleans, who in a liberated Zille have already left Smolak's regime
        Swap(t, Hod, "Free568", 568, new[] { 566, 567 }, Manus, new S(
            Lib: "\"May they find, in a Rizia that has changed, the freedom that President Smolak never gave them.\""), new[] { "#569" });

        // "with the return of Zille we will regain millions of new citizens"
        foreach ((string name, int entry, int parent, string speaker, string next, bool pales) in new[]
                 {
                     ("Citizens690", 690, 688, Adarfo, "#692", true), ("Citizens691", 691, 689, Adarfo, "#692", false),
                     ("Citizens761", 761, 759, Manus, "#765", true), ("Citizens762", 762, 760, Manus, "#765", false),
                 })
        {
            string pal = pales ? " -- to say nothing of Pales --" : string.Empty;
            Swap(t, Hod, name, entry, new[] { parent }, speaker, new S(
                Part: $"\"With Zille city back in the realm{pal} we have gained hundreds of thousands of new citizens, many of Wehzek origin. It is up to us to make sure none of them get left behind.\"",
                Lost: $"\"Whenever Zille does return{(pales ? ", to say nothing of Pales," : ",")} it will bring millions of new citizens, many of Wehzek origin. It is up to us to make sure none of them get left behind.\""),
                new[] { next });
        }

        foreach ((string name, int entry, int parent, bool pales) in new[] { ("Rejoins1103", 1103, 1576, true), ("Rejoins1107", 1107, 1577, false) })
        {
            Swap(t, Hod, name, entry, new[] { parent }, Manus, new S(
                Part: $"\"When the rest of Zille rejoins us{(pales ? " -- to say nothing of Pales --" : string.Empty)} we will gain millions of new citizens. How do we make sure nobody is left behind?\""),
                new[] { "#1109", "#1112", "#1225", "#1226" });
        }

        Swap(t, Hod, "Unity1144", 1144, new[] { 1143 }, Daria, new S(
            Part: "\"The armistice in Zille has been a blow to national unity. We must find ways to keep morale up.\"",
            Lost: "\"The defeat in Zille has been a heavy blow to national unity. We must find ways to keep morale up.\""), new[] { "#1145" });
        t.Register();
    }

    // ------------------------------------------------------------------ c595 the sanctuary timber from Wehlen

    private static void RegisterReligion()
    {
        var t = new Talk(Religion, "ZilleWar.A8.Religion.");
        // the Wehzek timber was chosen before the war; the border closed with the declaration
        Swap(t, Religion, "Timber909", 909, new[] { 879 }, "Sal Ignacius", S.All(
            "\"We had begun sourcing timber from Wehlen, Your Majesty, until the war closed the border. The builders have made do with pine from Valenqiris since, and the work has slowed.\""),
            new[] { "#916" });
        t.Register();
    }

    // ------------------------------------------------------------------ news

    private const string TensionsNews = "Turn08_G_ZilleTensionsRise";

    /// <summary>Su Omina's attacks in a Zille that stayed Wehzek after the war (LateState hides the item once Zille city is ours).</summary>
    private static void UpdateNews()
    {
        bool lost = LateState.Lost;
        if (lost)
        {
            WorldText.News(TensionsNews, null,
                "Despite limited casualties, a series of attacks likely perpetrated by the Rizian nationalist Su Omina group has left the " +
                "citizens of Zille shaking in their boots. Since the Rizian offensive stalled and the armistice left the city in Wehzek hands, " +
                "Wehlen has tightened its grip on the region, with checkpoints on every road out of the old town and a curfew after dark.\n\n" +
                "Wraclawitz blames Porte Drazon for every bomb, but has so far failed to provide substantial evidence, and the group has " +
                "managed to avoid capture. Reports of internal silencing within the group have also emerged, with suspected members often " +
                "found dead in their homes.");
        }
        else
        {
            WorldText.RestoreNews(TensionsNews);
        }
    }
}
