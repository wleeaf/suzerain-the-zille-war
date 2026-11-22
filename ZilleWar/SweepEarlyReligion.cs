using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Turn 6 religion (part of SweepEarly): the night of the Alignment of the Six Stars with Sal Ignacius
/// (Rizia/Turn 6/A_ReligionBriefing, c360), the charities decision that follows it and the Royal Herald's report of the night.
/// EarlyT6.ReligionBriefing already varies [741] (war being fought), [745], [764], [768], [680] and adds Sal's just-war
/// lines after [109]/[488]. This file covers what that pass missed:
/// - the King's war questions [1227]/[1228] and [741] once the war is over, the opening narration ([1421], [1425], after [1424]);
/// - Sal on Smolak and Derdia ([700], [702], [1172]), a "Derdian warriors for Zille" option at [1586];
/// - the Bludish ban as a ceasefire term ([634], [1042], [1354], [1357]);
/// - Sevrio's letter (after [1539]), the Dastnurist congregations (Zille is a Dastnurist city, and so are Rizia's Wehzeks),
///   and the moment the Alignment appears (after [1406]);
/// - Turn06_Decision_FundReligiousCharities reworded for wartime; the Alignment news gets a war paragraph.
/// </summary>
internal static class SweepEarlyReligion
{
    private const string Conv = "Rizia/Turn 6/A_ReligionBriefing";
    private const string Sal = "Sal Ignacius";

    /// <summary>"guard" | "words" | "suspect": what the King told the Dastnurist archpriests through Sal (war being fought).</summary>
    public const string VarDastnurists = "ZilleWar.SE_Rel_Dastnurists";
    /// <summary>The King told Sal that Rizia needs Derdian warriors to finish the war in Zille (T6, before the Derdia visit).</summary>
    public const string VarDerdiaForZille = "ZilleWar.SE_Rel_DerdiaForZille";

    private const string AskedDastnurists = "ZilleWar.SE_Rel_Asked_Dastnurists";
    private const string ReportDastnurists = "ZilleWar.SE_Rel_Report_Dastnurists";

    private const string AlignmentNews = "Turn06_RH_Briefing_Religion_AlignmentOfTheSixStars";
    private const string Charities = "Turn06_Decision_FundReligiousCharities";

    private const string BludishBanFree = "Variable[\"RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishBan\"] ~= true";
    private const string KnowsMotherFaith = "Variable[\"RiziaDLC.Turn05_MurderInvest_SuspectEstela\"] == true";

    public static void Register()
    {
        foreach (string v in new[] { VarDastnurists, VarDerdiaForZille, AskedDastnurists, ReportDastnurists })
        {
            Variables.Register(v);
        }

        var t = new Talk(Conv, "ZilleWar.SE.Rel.");
        Opening(t);
        WarQuestions(t);
        SmolakAndDerdia(t);
        CeasefireBan(t);
        Sevrio(t);
        Dastnurists(t);
        Alignment(t);
        t.Register();

        // The Royal Herald's report of the night learns about the war.
        News(AlignmentNews, (s, _) => AlignmentText(s));
    }

    // ================================================================== the drive to the observatory

    private static void Opening(Talk t)
    {
        // [1421] (Pales war declared or being prepared): "...the impending war."
        t.Variant("Mind1421Active", 1421, new[] { 1419 }, Active, Narrator,
            "It was going to be difficult to get my mind off of the war in Zille, let alone the one coming in Pales. But I had to try.",
            new[] { "#804" });
        t.Variant("Mind1421Won", 1421, new[] { 1419 }, Liberated, Narrator,
            "Zille was ours again, and already another war was coming. It was going to be difficult to get my mind off of it. But I had to try.",
            new[] { "#804" });
        t.Variant("Mind1421Lost", 1421, new[] { 1419 }, LostOrStopped, Narrator,
            "It was going to be difficult to get my mind off of Zille, and off of the war now coming in Pales. But I had to try.",
            new[] { "#804" });

        // [1425] (joint border investigation): "...the possibility of war with Pales."
        t.Variant("Mind1425Active", 1425, new[] { 1423 }, Active, Narrator,
            "It was going to be difficult to get my mind off of the fighting in Zille, and the possibility of a second war with Pales. But I had to try.",
            new[] { "#804" });

        // No Pales crisis: vanilla says nothing ([1424] -> [804]).
        t.Line("MindActive", Narrator, "It was going to be difficult to get my mind off of the fighting in Zille. But I had to try.",
            new[] { "#804" }, condition: Active, hooks: From(1424));
        t.Line("MindWon", Narrator,
            "Zille was ours again. For one evening, I meant to let myself believe the hardest part was over.",
            new[] { "#804" }, condition: Liberated, hooks: From(1424));
        t.Line("MindLost", Narrator,
            "It was going to be difficult to get my mind off of Zille, and the men who had not come back from it. But I had to try.",
            new[] { "#804" }, condition: LostOrStopped, hooks: From(1424));
    }

    // ================================================================== "the role of religion in armed conflict"

    private static void WarQuestions(Talk t)
    {
        // [1227] "We might have to resort to violent actions in Zille. Could you advise me on the matter?"
        t.Variant("Ask1227Active", 1227, new[] { 740 }, Active, Romus,
            "\"Men are dying in Zille on my orders as we speak. I would hear what our faith says of that.\"", new[] { "#461" });
        t.Variant("Ask1227Won", 1227, new[] { 740 }, Liberated, Romus,
            "\"I took Zille back by force of arms. Now that it is done, I would hear what our faith says of it.\"", new[] { "#461" });
        t.Variant("Ask1227Lost", 1227, new[] { 740 }, LostOrStopped, Romus,
            "\"I sent men to die for Zille, and Zille is still Wehzek. I would hear what our faith says of that.\"", new[] { "#461" });

        // [741] "We may need to reclaim Zille through less peaceful means..." (EarlyT6 covers the war being fought).
        t.Variant("Ask741Won", 741, new[] { 740 }, Liberated, Romus,
            "\"Zille was taken back by force of arms. Tell me the religious context and implications of what I have done.\"", new[] { "#461" });
        t.Variant("Ask741Lost", 741, new[] { 740 }, LostOrStopped, Romus,
            "\"I went to war for Zille and could not win it. Tell me the religious context and implications of what I have done.\"",
            new[] { "#461" });

        // [1228] (Pales war coming): "There is a war on the horizon. How will our faith handle this?"
        t.Variant("Ask1228Active", 1228, new[] { 739 }, Active, Romus,
            "\"One war is already being fought in Zille, and another is on the horizon. How will our faith handle this?\"", new[] { "#461" });
        t.Variant("Ask1228Over", 1228, new[] { 739 }, WarFlags.L(WarFlags.Over), Romus,
            "\"We have only just come out of one war over Zille, and another is on the horizon. How will our faith handle this?\"",
            new[] { "#461" });
    }

    // ================================================================== Derdia, Smolak and "another adversary on our doorstep"

    private static void SmolakAndDerdia(Talk t)
    {
        // [702] "You did not give into Wehzek pressure to ban Derdians from our borders..."
        t.Variant("NoPressure", 702, new[] { 696 }, $"{Declared} and {BludishBanFree}", Sal,
            "\"Wehlen is in no position to press us into banning Derdians from our borders anymore. I believe the door is open for " +
            "negotiations with their Supreme Wiseman.\"", new[] { "#677", "#697", "#699" });

        // [699] "With the bonus that I stood up to Wiktor Smolak." -> [700] "...if events in Zille go from bad to worse." -> [723]
        t.Variant("Worse700Active", 700, new[] { 699 }, Active, Sal,
            "\"You did rather more than stand up to him, Your Majesty. Pray that the war in Zille does not go from bad to worse.\"",
            new[] { "#986" });
        t.Variant("Worse700Won", 700, new[] { 699 }, Liberated, Sal,
            "\"You took Zille from him, Your Majesty. Supreme Wiseman Azmal respects strength. Let us hope he does not learn to fear it.\"",
            new[] { "#986" });
        t.Variant("Worse700Lost", 700, new[] { 699 }, LostOrStopped, Sal,
            "\"And Zille is still in his hands, Your Majesty. Standing up to President Smolak has cost us dearly enough already.\"",
            new[] { "#986" });

        // [1172] "The last thing we need is increased tension that could spill over into violence." ([1171] only knows the Pales war)
        t.Variant("Tension1172", 1172, new[] { 1170 }, Active, Sal,
            "\"With our soldiers already fighting in Zille, the last thing we need is increased tension that could spill over into violence.\"",
            new[] { "#1294" });

        // At the Derdia menu ([1586]): vanilla's "Rizia needs help from Derdia regarding Zille" ([978]) is unreachable; at war it is the point.
        t.Choice("Warriors", "\"Anyway, the point is Rizia needs Derdian warriors to finish the war in Zille. We just need to ensure it benefits them as well.\"",
            new[] { "#657" }, condition: Active, script: Set(VarDerdiaForZille, "true"), hooks: From(1586));
    }

    // ================================================================== the Bludish ban as one of Smolak's ceasefire terms

    private static void CeasefireBan(Talk t)
    {
        // Under the war the ban only exists when the King accepted it among Smolak's ceasefire terms (SupplyVoices).
        string banByCeasefire = CeasefireOnly;

        t.Variant("Deal634", 634, new[] { 632 }, banByCeasefire, Sal,
            "\"I can only hope the border ban President Smolak demanded for the ceasefire will at least spare us such disruptions in " +
            "the future. Though it is already being condemned as a 'Golcondist border ban.'\"",
            new[] { "#472", "#1354", "#1356", "#1358" });
        t.Variant("Plan1042", 1042, new[] { 1038 }, banByCeasefire, Sal,
            "\"As was your acceptance of President Smolak's demand, at the ceasefire, to ban Bludish Golcondists from our lands.\"",
            new[] { "#1230" });
        t.Variant("Price1354", 1354, new[] { 634, 729 }, banByCeasefire, Romus,
            "\"Not this again. The ban was part of the price for ending the fighting in Zille. I would pay it again.\"", new[] { "#1355" });

        // [1357] "...But I am with Wehlen in recognizing the threat Golcondists pose."
        t.Variant("Agree1357", 1357, new[] { 1356 }, Declared, Sal,
            "\"There may yet come a chance to undo it. Though I never thought I would find myself agreeing with Wiktor Smolak about " +
            "anything, Your Majesty. Least of all now.\"", new[] { "#78" });
    }

    // ================================================================== Sevrio Castellanus's letter

    private static void Sevrio(Talk t)
    {
        // [1539] "...He sent me a message to pass on to you." -> [1544] (the letter: living standards, then the faithful's mood)
        t.Line("LetterActive", Sal,
            "\"Before anything else, he asks me to tell you that his sanctuaries bless every company that marches north to Zille, and " +
            "bury their dead without charge. The rest of his letter is about the living.\"",
            new[] { "#1544" }, condition: Active, hooks: From(1539));
        t.Line("LetterWon", Sal,
            "\"Before anything else, he asks me to tell you that every bell in his parishes rang the day Zille was taken. The rest of his " +
            "letter is about what the bells cannot do.\"",
            new[] { "#1544" }, condition: Liberated, hooks: From(1539));
        t.Line("LetterLost", Sal,
            "\"Before anything else, he asks me to tell you that his sanctuaries are still burying the men who came back from Zille in " +
            "coffins. The rest of his letter is about the living.\"",
            new[] { "#1544" }, condition: LostOrStopped, hooks: From(1539));
    }

    // ================================================================== the Dastnurists: Rizia's Wehzeks, and Zille itself

    private static void Dastnurists(Talk t)
    {
        int[] beforeGolcondists = { 1347, 1348, 1400, 1401, 1402 }; // every Sal line that leads into [622] "As for the Golcondist community..."
        string once = WarFlags.LNot(AskedDastnurists);

        t.Line("Dast1", Sal,
            "\"One more thing about the Dastnurists, Your Majesty. Since Konstantium, the doors of their Wehzek sanctuaries in Iza have " +
            "been daubed with paint, and 'Wehzek' is the kindest word written on them.\"",
            new[] { "Dast2" }, condition: $"{Active} and {once}", script: Set(AskedDastnurists, "true"), hooks: From(beforeGolcondists));
        t.Line("Dast2", Sal,
            "\"Their archpriests ask me, quite plainly, whether the Crown still counts their flocks as Rizians. And they remind me that " +
            "Zille is a Dastnurist city. Half of them have cousins there, under Wehzek curfews and our shells alike.\"",
            new[] { "DastMother", "DastMenu" });
        t.Line("DastMother", Narrator,
            "I thought of my mother, raised in Zille, slipping out of the palace at night to pray at a Dastnurist service.",
            new[] { "DastMenu" }, condition: KnowsMotherFaith);
        t.Logic("DastMenu", new[] { "Guard", "Words", "Suspect" });

        t.Choice("Guard", "\"Post police outside their sanctuaries. Wehzek or not, they are my subjects.\"", new[] { "GuardA" },
            cost: "-1 Budget", script: Set(VarDastnurists, "\"guard\"") + Inc("RiziaDLC.Resources_Budget", -1));
        t.Line("GuardA", Sal,
            "\"Generous, Your Majesty. I shall tell them.\" He smoothed his robes. \"Some in my own congregations will ask why the Crown " +
            "guards Dastnurist altars while their sons fight Wehzeks in Zille. I shall have to think of an answer.\"",
            new[] { "#622" });

        t.Choice("Words", "\"Tell them my war is with Wiktor Smolak, not with their faith.\"", new[] { "WordsA" },
            script: Set(VarDastnurists, "\"words\""));
        t.Line("WordsA", Sal,
            "\"They will take the words, Your Majesty. In wartime, a minority takes whatever it is given.\"", new[] { "#622" });

        t.Choice("Suspect", "\"The Wehzeks among them would do well to keep their heads down until the war is over.\"", new[] { "SuspectA" },
            script: Set(VarDastnurists, "\"suspect\""));
        t.Line("SuspectA", Sal,
            "\"I won't pretend that will sadden every Wruhecist in Porte Drazon.\" He paused. \"But a frightened flock strays, Your " +
            "Majesty. And it rarely strays toward the shepherd.\"",
            new[] { "#622" });

        // After the war: one line, no question.
        t.Line("DastWon", Sal,
            "\"The Dastnurist archpriests have one request since Zille came home, Your Majesty. Its sanctuaries are theirs, and they ask " +
            "that the Crown treat them as Rizian houses of God, not as spoils of war.\"",
            new[] { "#622" }, condition: $"{Liberated} and {once}", script: Set(AskedDastnurists, "true"), hooks: From(beforeGolcondists));
        t.Line("DastLost", Sal,
            "\"The Dastnurist archpriests of Iza tell me their cousins in Zille are paying for our war. Wehlen has closed every sanctuary " +
            "there suspected of sheltering our soldiers.\"",
            new[] { "#622" }, condition: $"{LostOrStopped} and {once}", script: Set(AskedDastnurists, "true"), hooks: From(beforeGolcondists));
    }

    // ================================================================== the Alignment appears

    private static void Alignment(Talk t)
    {
        // [1406] "Below, the city hushed..." -> [321] Sal: "My God... it's even more breathtaking..."
        t.Line("StarsActive", Narrator,
            "Somewhere to the north, under the same six lights, our soldiers were dug in around Zille. I wondered whether any of them had looked up.",
            new[] { "#321" }, condition: Active, hooks: From(1406));
        t.Line("StarsWon", Narrator,
            "The same six lights hung over Zille that night, over a city that was Rizian again. For once, I did not care whether it was an omen.",
            new[] { "#321" }, condition: Liberated, hooks: From(1406));
        t.Line("StarsLost", Narrator,
            "The same six lights hung over Zille that night, over the fields where we had left our dead. I wondered what St. Wruhec would have made of that.",
            new[] { "#321" }, condition: Defeated, hooks: From(1406));
        t.Line("StarsCeasefire", Narrator,
            "The same six lights hung over Zille that night, where Wehzek sentries now stood on the line we had agreed to. Peace, of a kind.",
            new[] { "#321" }, condition: CeasefireOnly, hooks: From(1406));
    }

    // ================================================================== step logic: news, report, decision text

    public static void OnStep()
    {
        bool declared = Variables.GetBool(WarFlags.Declared);
        RewriteCharities(!declared ? null : Variables.GetBool(WarFlags.Active) ? CharitiesWar : CharitiesAfter);

        if (!declared || Turn < 6)
        {
            return;
        }

        DastnuristReport();
    }

    private static string AlignmentText(string state)
    {
        string war = By(state,
            "On the Zille front, army chaplains led open-air services for the soldiers of the northern army, and many of His " +
            "Majesty's soldiers were seen gazing upward from their positions outside the city. \"If the heavens are aligned,\" one " +
            "chaplain told our correspondent, \"it is because they are on the side of the King and of Zille.\"",
            "In Zille, celebrating its first nights back under the Rizian flag, crowds gathered on the old harbor walls to watch " +
            "the alignment together. Dastnurist and Wruhecist families alike lit candles for the soldiers who brought the city home.",
            "In the villages of Brenas, many families of soldiers who did not return from Zille kept vigil beneath the alignment, " +
            "lighting candles in sanctuary windows. Grand Wiseman Ignacius asked the faithful to remember them in their prayers.");

        return P(
            "In a celestial event that captured the hearts and imagination of Rizians across the kingdom, the Alignment of the Six " +
            "Stars unfolded in a breathtaking display in the night sky. Citizens from all walks of life ventured outdoors to witness " +
            "this rare occurrence, creating a collective sense of wonder and unity under the celestial canopy.",
            "To mark this significant moment, various viewing events were organized throughout Rizia, providing the public with " +
            "telescopes, guided explanations, and a chance to share in the communal experience. Families, friends, and even strangers " +
            "gathered in open spaces, fostering a sense of togetherness beneath the vast expanse of the night sky.",
            war,
            "King Romus and Wiseman Sal Ignacius observed the event together at the Porte Drazon observatory tower, reinforcing the " +
            "cultural and symbolic importance of the celestial alignment. Their shared enthusiasm for the astronomical wonder " +
            "resonated with the gathered crowds, emphasizing the connection between the kingdom's leadership and its people.",
            "As the Alignment of the Six Stars concluded, Rizians carried the memory of this extraordinary event back into their " +
            "daily lives, a reminder, in difficult times, of the unity and shared experiences that bind the people of Rizia together.");
    }

    private static void DastnuristReport()
    {
        string answer = Variables.GetString(VarDastnurists);
        if (answer != "guard" && answer != "suspect")
        {
            return;
        }

        if (!Once(ReportDastnurists))
        {
            return;
        }

        bool charity = Variables.GetBool("RiziaDLC.Decision_Turn06_FundReligiousCharities_Dastnurist") ||
                       Variables.GetBool("RiziaDLC.Decision_Turn06_FundReligiousCharities_FundAll");
        if (answer == "guard")
        {
            NewsWire.Report("SE_Rel_SanctuaryGuards", "Rizia_City_Iza", "Police Posted Outside Dastnurist Sanctuaries", NewsWire.P(
                "Royal police officers now stand guard outside Dastnurist sanctuaries in Iza and Porte Drazon, by order of the Crown, " +
                "after a string of incidents in which sanctuary doors were daubed with anti-Wehzek slogans since the declaration of war " +
                "in Konstantium.",
                "\"We were told our sons and grandsons were suspects,\" an elderly member of an Iza congregation said. \"Now there is a " +
                "policeman at our door who says he is there for us. We will see.\"" +
                (charity ? " The Crown's recent donation to the community's leading charity has also been noted with gratitude." : ""),
                "Some Wruhecist parishes in Brenas have complained that the police would be better used guarding the homes of soldiers' families."));
        }
        else
        {
            NewsWire.Report("SE_Rel_WehzekHarassment", "Rizia_City_Iza", "Wehzek Families in Iza Report Harassment", NewsWire.P(
                "Several Wehzek-speaking families in Iza have reported broken windows, threatening letters and the defacing of a " +
                "Dastnurist sanctuary in the weeks since the war with Wehlen began. Local police described the incidents as \"regrettable\" " +
                "but said they had \"more urgent matters\" in wartime.",
                "Community leaders say a number of young Wehzek men have stopped attending services altogether, and that some have left " +
                "the city. Wehzek state radio has seized on the reports as proof that \"Rizia persecutes the Wehzek nation wherever it finds it.\""));
        }
    }

    // ================================================================== Turn06_Decision_FundReligiousCharities

    private sealed class Rewrite
    {
        public string Description;
        public string[] Options; // same order as vanilla's six options
    }

    private static readonly Rewrite CharitiesWar = new()
    {
        Description =
            "In the Rizian Kingdom faith plays a vital role in the lives of many citizens, and the war in Zille has made it more vital " +
            "still. Wruhecist charities are feeding the families of the soldiers sent north, while Dastnurist congregations, many of them " +
            "Wehzek, have seen their donors melt away since Konstantium. The crown can back a Wruhecist, Golcondist, or Dastnurist charity, " +
            "each with its own doctrines and influence within Rizian society. Alternatively, it could keep every coin for the war and fund " +
            "no religious organization at all.",
        Options = new[]
        {
            "Fund the Wruhecist charities feeding soldiers' families (-1 Budget)",
            "Fund Golcondist charity (-1 Budget)",
            "Fund the Dastnurist charities in Iza's Wehzek quarters (-1 Budget)",
            "Fund all Nurity charities (-3 Budget)",
            "Fund none, every coin goes to the war (-1 Authority)",
            "Fund none, every coin goes to the war",
        },
    };

    private static readonly Rewrite CharitiesAfter = new()
    {
        Description =
            "In the Rizian Kingdom faith plays a vital role in the lives of many citizens. Since the war over Zille, the Sanctuary's " +
            "charities have been caring for the wounded and for the families of the fallen, while Dastnurist congregations, many of them " +
            "Wehzek, are still shunned by their neighbors. The crown can back a Wruhecist, Golcondist, or Dastnurist charity, each with its " +
            "own doctrines and influence within Rizian society. Alternatively, the government could opt to refrain from funding any " +
            "religious organizations, maintaining a stance of more secular governance.",
        Options = new[]
        {
            "Fund the Wruhecist charities caring for veterans' families (-1 Budget)",
            "Fund Golcondist charity (-1 Budget)",
            "Fund the Dastnurist charities in Iza's Wehzek quarters (-1 Budget)",
            "Fund all Nurity charities (-3 Budget)",
            "Fund none (-1 Authority)",
            "Fund none",
        },
    };

    private static (string desc, string[] options)? _original;

    private static void RewriteCharities(Rewrite text)
    {
        try
        {
            foreach (var list in new[] { EntityDataManager.decisionsData, EntityDataManager.DecisionsData, EntityDataManager.AllDecisionsData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (DecisionData d in list)
                {
                    DecisionProperties p = d?.DecisionProperties;
                    if (d?.NameInDatabase != Charities || p?.Options == null || p.Options.Count != CharitiesWar.Options.Length)
                    {
                        continue;
                    }

                    if (_original == null)
                    {
                        var vanillaTexts = new string[p.Options.Count];
                        for (int i = 0; i < vanillaTexts.Length; i++)
                        {
                            vanillaTexts[i] = p.Options[i].Text;
                        }

                        _original = (p.Description, vanillaTexts);
                    }

                    (string desc, string[] options) = text != null ? (text.Description, text.Options) : _original.Value;
                    p.Description = desc;
                    for (int i = 0; i < options.Length; i++)
                    {
                        p.Options[i].Text = options[i];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not reword {Charities}: {ex.Message}");
        }
    }
}
