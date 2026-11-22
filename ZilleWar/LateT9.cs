using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.LK;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Turn 9 for a Rizia that went to war over Zille:
/// - the referendum (c448, c441) and Smolak's "Zille for Pales" call (c434) are gated off (LateState); Smolak calls
///   anyway, from a different place each time: a ceasefire offer, a gloat, a sore loser's threat, or a permanent line;
/// - Queen Beatrice's tea (c433) talks about the war she helped fight instead of "You are getting Zille back", and the
///   flight home looks down on the real Zille;
/// - the return speech (c440) becomes the formal ceremony in a city still rebuilding from the war (fought in Turns 4-6),
///   and keeps the Turn 7 decision on who governs Zille;
/// - the Morella mediation (c436) stops calling a war "a delicate situation".
/// </summary>
internal static class LateT9
{
    private const string Tea = "Rizia/Turn 9/FPnT_GRACERumburgMeeting";
    private const string Speech = "Rizia/Turn 9/A_ZilleReturnedSpeech";
    private const string Mediation = "Rizia/Turn 9/Morella Derdia Mediation";
    private const string CallConv = "ZilleWar/Late/SmolakT9";

    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Smolak = "Wiktor Smolak (R)";
    private const string Estela = "Estela Toras";
    private const string Narr = Talk.Narrator;
    private const string R = Talk.Romus;

    public const string VarCallOffered = "ZilleWar.Late_SmolakT9Offered";
    public const string VarRecognized = "ZilleWar.Late_RecognizedWehzekZille";
    public const string VarLinePermanent = "ZilleWar.Late_LinePermanent";
    public const string VarNanta = "ZilleWar.Late_NantaWarning";            // "deal" | "threat"
    public const string VarRumburgOwed = "ZilleWar.Late_RumburgOwed";
    public const string VarEmbargoLifted = "ZilleWar.Late_RumburgEmbargoLifted";
    private const string VarVoteNews = "ZilleWar.Late_News_WehzekVote";

    /// <summary>The one definition of "Rumburg helped in the Zille war": the Turn 3 oil deal (cut or halved), any telephone lever
    /// (embargo, BFF raids, BFF fighters) or war credits.</summary>
    public const string LuaRumburgHelped =
        "(" + SupplyWar.LuaOilCut + " or " + SupplyWar.LuaOilHalved +
        " or (Variable[\"ZilleWar.Lever_BffRaids\"] or 0) > 0 or (Variable[\"ZilleWar.Lever_BffFighters\"] or 0) > 0" +
        " or (Variable[\"ZilleWar.Rumburg_Credits\"] or 0) > 0)";

    public static bool RumburgHelpedNow =>
        SupplyWar.RumburgOilCut || SupplyWar.RumburgOilHalved ||
        Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffRaids)) > 0 || Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffFighters)) > 0 ||
        Variables.GetInt(SupplyWar.VarRumburgCredits) > 0;

    private static readonly string RumburgHelped = LuaRumburgHelped;

    /// <summary>Queen Beatrice telephoned during the war (SupplyBeatrice's call counter).</summary>
    private static readonly string BeatriceCalled = "((Variable[\"ZilleWar.Call_Beatrice_Count\"] or 0) > 0)";

    /// <summary>Rumburg is not buying Wehzek oil: the Turn 3 export deal, or the embargo Beatrice agreed on the telephone.</summary>
    private static readonly string RumburgEmbargo =
        "(Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true " +
        "or (Variable[\"ZilleWar.Lever_Embargo\"] or 0) > 0)";

    public static void Register()
    {
        foreach (string v in new[] { VarCallOffered, VarRecognized, VarLinePermanent, VarNanta, VarRumburgOwed, VarEmbargoLifted, VarVoteNews })
        {
            Variables.Register(v);
        }

        ConversationRegistry.RegisterConversation(CallConv);
        RegisterTea();
        RegisterSpeech();
        RegisterMediation();
        RegisterCall();
    }

    public static void OnStep()
    {
        if (!LateState.Declared || LateState.Turn != 9)
        {
            return;
        }

        if (LateState.Lost && !Variables.GetBool(VarVoteNews) && LateState.Step >= 8)
        {
            Variables.Set(VarVoteNews, true);
            NewsWire.Publish("Late_WehzekVote", "G", "Wehlen Holds Vote in Zille", string.Join("\n\n",
                "Wehlen has held what it calls a \"referendum on reunification with the Wehzek motherland\" in the Zille region. According to " +
                "Wehzek state radio, ninety-seven percent of voters approved. No foreign observers were admitted.",
                (SuzerainModdingKit.Variables.GetBool(WarFlags.Occupied("Mave Nortus")) ? "Rizian families who fled across the armistice line say ballot boxes" : "Rizian families who fled across the border at Mave Nortus say ballot boxes")+" in the harbor district of Zille were carried in " +
                "already sealed. President Smolak called the result \"the final word on the matter,\" and announced a national holiday."));
        }

        // vanilla's Smolak call (c434) had Turn 9 step 8
        if (LateState.Step < 8 || Variables.GetBool(VarCallOffered))
        {
            return;
        }

        const string frag = "ZilleWar.Late_SmolakT9";
        if (GameState.StoryFragmentExistsInCurrentStep(frag))
        {
            return;
        }

        try
        {
            GameState.AddCustomConversation(CallConv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(frag, SuzerainStoryPackInfo.Rizia, "Rizia_City_Monqiz",
                "An Evening in Monqiz", "Palas Toras, Monqiz", Il2Cpp.ConversationData.ConversationType.ForeignPolicyAndTrade, CallConv));
            Variables.Set(VarCallOffered, true);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the Turn 9 Smolak call failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ c433 tea with Beatrice

    private static void RegisterTea()
    {
        var t = new Talk(Tea, "ZilleWar.Late.Tea.");
        string[] start = { "On", "OnNoEmbargo", "LibHelped", "LibAlone", "Part", "PartArmistice", "LostHelped", "LostAlone", "LostRefused" };
        t.Logic("Zille1", start, condition: War, hooks: Talk.From(534));
        t.Logic("Zille2", start, condition: War, hooks: Talk.From(538));

        t.Line("On", Beatrice, "\"And then there is your war. I cannot keep my refineries on Rizian gas and good intentions forever, Romus. Rumburg will need Wehzek oil for what's coming, and what's coming is not in Zille.\"",
            new[] { "OnMore", "OnLift" }, condition: And(Ongoing, RumburgEmbargo));
        t.Line("OnNoEmbargo", Beatrice, "\"And then there is your war. My ministers ask me every morning when it ends, Romus, and I have run out of clever answers. I don't enjoy running out of anything.\"",
            new[] { "OnMore" }, condition: And(Ongoing, $"not {RumburgEmbargo}"));
        t.Choice("OnMore", "\"A few more months. That's all I ask.\"", new[] { "OnMore2" });
        t.Line("OnMore2", Beatrice, "\"A few more months is what every general says. You may have until the spring. After that, my ministers will start making their own arrangements with Wraclawitz, and Smolak is always open for business.\"", new[] { "#592" });
        t.Choice("OnLift", "\"Then lift the embargo. Rizia will manage.\"", new[] { "OnLift2" },
            condition: RumburgEmbargo,
            script: Set(VarEmbargoLifted, "true") +
                    $"if (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) < 10 then Variable[\"{ZilleCampaign.VarStrength}\"] = (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) + 1 end; " +
                    Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 1));
        t.Line("OnLift2", Beatrice, "\"Thank you. I had hoped you'd make me say it, so I could be gracious about it.\"", new[] { "#592" });

        t.Line("LibHelped", Beatrice, "\"You took Zille with a good deal of Rumburgian help, and Smolak knows exactly how much. A splendid job, my dear. And naturally, a bill. Rumburg has a very long memory.\"",
            new[] { "LibGold" }, condition: And(Lib, RumburgHelped));
        t.Line("LibAlone", Beatrice, "\"And you took Zille without me, I notice. I'm torn between admiration and offense. I shall probably settle on both.\"",
            new[] { "LibGold" }, condition: And(Lib, $"not {RumburgHelped}"));
        t.Logic("LibGold", new[] { "LibGoldR", "LibGoldM", "LibMenu" });
        t.Logic("LibMenu", new[] { "LibPay", "LibOurs" });
        // what the King promised her in Thornbourgh about Zille's gold (EarlyT7Diplomacy)
        t.Line("LibGoldR", Beatrice, "\"And I have not forgotten that Rumburg has first refusal on the Auremina gold. My bankers have already drawn up the contract. They drew it up in Thornbourgh, the evening you said yes.\"",
            new[] { "LibMenu" }, condition: $"{V(EarlyT7.VarZilleGold)} == \"rumburg\"");
        t.Line("LibGoldM", Beatrice, "\"Zille's gold goes to whoever pays best, you told me in Thornbourgh. Very well. Rumburg will pay best. You'll find it costs you more than you think.\"",
            new[] { "LibMenu" }, condition: $"{V(EarlyT7.VarZilleGold)} == \"market\"");
        t.Choice("LibPay", "\"Rizia pays its debts, Beatrice.\"", new[] { "LibPay2" }, script: Set(VarRumburgOwed, "true") + Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 1));
        t.Line("LibPay2", Beatrice, "\"I'll hold you to that when Sordland comes knocking. And it will.\"", new[] { "#592" });
        t.Choice("LibOurs", "\"Rizian soldiers paid for Zille. Nobody else.\"", new[] { "LibOurs2" }, script: Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("LibOurs2", Beatrice, "\"Spoken like a man who has never had to buy his own oil. We'll see how long that lasts.\"", new[] { "#592" });

        t.Line("Part", Beatrice, "\"A ceasefire. How very Kyrutian of you. Half a province and a line of trenches. Still, it's half more than Smolak meant to give you.\"",
            new[] { "PartR1", "PartR2" }, condition: And(Part, Ceasefire));
        t.Line("PartArmistice", Beatrice, "\"So your army ran out of road halfway to the sea, and now there's a line of trenches through Zille. Still, it's half more than Smolak meant to give you.\"",
            new[] { "PartR1", "PartR2" }, condition: And(Part, $"not ({Ceasefire})"));
        t.Choice("PartR1", "\"Half is a beginning.\"", new[] { "PartR3" });
        t.Choice("PartR2", "\"Half is what we could hold.\"", new[] { "PartR3" });
        t.Line("PartR3", Beatrice, "\"Mm. Do let me know when you find out which of those it was.\"", new[] { "#592" });

        t.Line("LostHelped", Beatrice, "\"I lent you what Rumburg could spare, and more than my ministers liked, and you lost the city anyway. I won't be lending you another.\"",
            new[] { "LostR1", "LostR2" }, condition: And(Lost, RumburgHelped));
        t.Line("LostAlone", Beatrice, "\"You went to war without asking me and lost it without telling me. I read about both in the newspapers, Romus.\"",
            new[] { "LostR1", "LostR2" }, condition: And(Lost, $"not {RumburgHelped}", $"not {BeatriceCalled}"));
        // she did call during the war (SupplyBeatrice), and he took nothing
        t.Line("LostRefused", Beatrice, "\"I offered you Rumburg's help on the telephone, and you turned down every bit of it. Then you lost the city. I'm told that's called independence.\"",
            new[] { "LostR1", "LostR2" }, condition: And(Lost, $"not {RumburgHelped}", BeatriceCalled));
        t.Choice("LostR1", "\"I deserved that.\"", new[] { "LostR1b" });
        t.Line("LostR1b", Beatrice, "\"You did. Have some more tea.\"", new[] { "#592" });
        t.Choice("LostR2", "\"Rumburg didn't exactly empty its arsenals for us.\"", new[] { "LostR2b" }, script: Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("LostR2b", Beatrice, "\"Rumburg doesn't empty its arsenals for anyone, darling. That's why it still has them.\"", new[] { "#592" });

        // two wars at once
        Swap(t, Tea, "Wars189", 189, new[] { 191 }, Beatrice, new S(Ongoing: "\"So how are those wars of yours going? You seem to collect them.\""),
            new[] { "#852" }, when: PalesWar);
        Swap(t, Tea, "WarsAfter", 189, new[] { 191 }, Beatrice, new S(
                Lib: "\"First Zille, and now Pales. You do seem to collect wars, Romus. So how is this one going?\"",
                Part: "\"Half a war in Zille, and now a whole one in Pales. You do seem to collect them, Romus. So how is this one going?\"",
                Lost: "\"I'd have thought Zille had cured you of wars for a while. Evidently not. So how is this one going?\""),
            new[] { "#852" }, when: PalesWar);

        // the flight home
        foreach ((string sfx, string state, string text) in new[]
                 {
                     ("On", Ongoing, "The General Staff had forbidden the pilot to fly anywhere near the front, so we went home the long way, out over the sea. Somewhere to the north, the guns were still working."),
                     ("Lib", Lib, "The harbor was still scarred, but our flag flew over the customs house. Zille was ours again. Now came the hard part: keeping it."),
                     ("Part", Part, "I could make out the armistice line, a scar of trenches running from the hills to the sea. North of it, the cranes of Isor and Nanta still worked under the Wehzek flag."),
                     ("Lost", Lost, "It was close enough to make out the Wehzek flag over the lighthouse. The pilot, without being asked, banked away."),
                 })
        {
            t.Line("Flight" + sfx, Narr, text, new[] { "#351" }, condition: state, hooks: Talk.From(625));
        }

        t.Register();
    }

    // ------------------------------------------------------------------ c440 the return speech, after a war

    private static void RegisterSpeech()
    {
        var t = new Talk(Speech, "ZilleWar.Late.Speech.");
        t.Line("Bedroom", Estela, "\"They used my bedroom as an artillery lookout. Of course they did. It always had the best view of the harbor.\"",
            new[] { "Spared", "#37" }, condition: War, hooks: Talk.From(161));
        // the King's promise at the Turn 6 cemetery to spare the old harbor quarter (EarlyT6)
        t.Line("Spared", Estela, "\"But the old quarter is standing, and so is Grandmother's sanctuary on the hill. I asked the keeper. He said the shells went round it like water round a stone. You kept your word, darling. Don't tell your generals I noticed.\"",
            new[] { "#37" }, condition: WarFlags.L(EarlyT6.VarSpareOldQuarter));
        Swap(t, Speech, "Streets", 72, new[] { 71 }, Narr, S.All(
            "The streets were crowded for the festival. Scaffolding still covered half the harbor front, and here and there a roofless tenement stood among the freshly painted ones, waiting its turn."),
            new[] { "SignNamed", "SignFedo", "Sign" });
        // the King named the square at the victory parade (VictoryParade)
        string sq = $"Variable[\"{VictoryParade.VarSquare}\"]";
        t.Line("SignNamed", Narr, "At the corner of the square hung the new sign the city's painters had put up the night of the victory parade. The paint was barely dry, and already someone had left flowers under it.",
            new[] { "Mother" }, condition: $"{sq} == \"liberation\" or {sq} == \"romus\" or {sq} == \"fallen\"");
        t.Line("SignFedo", Narr, "At the corner of the square, the old enamel sign our soldiers had screwed back onto the wall in the first days of the war still hung there, a little crooked: Fedo Square. Someone had left flowers under it.",
            new[] { "MotherFedo" }, condition: $"{WarFlags.L(CityEvents.Flag("Zille_BannerRaised"))} or Variable[\"{VictoryParade.VarSquare}\"] == \"fedo\"");
        t.Line("MotherFedo", Estela, "\"Fedo Square. Papa would have wept, darling. He never once called it by the other name, not even in front of the Wehzek governor.\"", new[] { "#256" });
        t.Line("Sign", Narr, "The Smolak Square sign had come down the week our soldiers entered the city. In its place hung a plain board with no name on it at all, waiting for someone to decide.", new[] { "Mother" });
        t.Line("Mother", Estela, "\"Nobody dared choose a name without you, darling. Make it official before somebody puts the old one back up.\"", new[] { "#83", "Leave", "#79", "#80" });
        t.Choice("Leave", "\"Leave the board blank for now. Let the people of Zille name their own square.\"", new[] { "Leave2" });
        t.Line("Leave2", Estela, "\"How democratic of you, darling. They'll call it Fedo Square anyway. Everyone here always did.\"", new[] { "#256" });
        t.Line("Plaque", Narr, "Beside the memorial to the victims of the Friendship Day bombing, a second plaque had been raised overnight. It listed the regiments that had fought their way from the river to the sea.",
            new[] { "#315" }, condition: War, hooks: Talk.From(75));

        // the King already decided who governs Zille (Turn 7): the speech can only announce that decision
        string brenas = "Variable[\"ZilleWar.E7_ZilleAdmin\"] == \"brenas\"";
        string imperii = "Variable[\"ZilleWar.E7_ZilleAdmin\"] == \"imperii\"";
        foreach (int id in new[] { 195, 196, 201, 208, 209, 211 })
        {
            Vanilla.Require(Speech, id, $"not ({War} and {imperii})");
        }

        foreach (int id in new[] { 197, 198, 199, 200, 210, 212 })
        {
            Vanilla.Require(Speech, id, $"not ({War} and {brenas})");
        }

        // ownership was already set at the liberation: keep one ownership flag and one authority value
        Vanilla.AppendScript(Speech, 213, $"if {War} then Variable[\"RiziaDLC.ZilleOwnership_RiziaImperii\"] = false; Variable[\"RiziaDLC.Authority_Modifier_TerritorialReclaimation\"] = 1 end");
        Vanilla.AppendScript(Speech, 214, $"if {War} then Variable[\"RiziaDLC.ZilleOwnership_Brenas\"] = false; Variable[\"RiziaDLC.Authority_Modifier_TerritorialReclaimation\"] = 2 end");
        t.Register();
    }

    // ------------------------------------------------------------------ c436 Morella and Derdia

    private static void RegisterMediation()
    {
        var t = new Talk(Mediation, "ZilleWar.Late.Mediation.");
        Swap(t, Mediation, "Delicate", 166, new[] { 165 }, R, new S(
            Ongoing: "\"Rizia is at war with Wehlen. I won't open my sanctuaries to new crowds while Wehzek agents are crossing our border.\"",
            Lib: "\"We've only just stopped fighting Wehlen. I won't stir another pot.\"",
            Part: "\"The line with Wehlen in Zille is barely holding. I won't stir another pot.\"",
            Lost: "\"We've only just lost a war with Wehlen. I won't stir another pot.\""), new[] { "#161" });
        Swap(t, Mediation, "TwoFronts", 47, new[] { 164 }, R, new S(Ongoing: "\"Rizia is fighting on two fronts. I don't want to make any sudden policy changes.\""), new[] { "#161" });
        t.Register();
    }

    // ------------------------------------------------------------------ the Turn 9 call from Smolak (replaces c434)

    private static void RegisterCall()
    {
        var t = new Talk(CallConv, "ZilleWar.Late.Smolak9.");
        t.Logic("Start", new[] { "Phone" }, hooks: Talk.AtStart(CallConv), script: "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\");");
        t.Line("Phone", Narr, "I was on the balcony of Palas Toras, watching the sun go down over the port of Monqiz, when an aide came out with the blue telephone and an apologetic face. President Smolak was on the line. He said it was personal.",
            new[] { "Hello" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Hello", Smolak, "\"Your Majesty! I hope I'm not interrupting. I can hear the sea. How lovely. All I can hear here is my cabinet.\"", new[] { "Pipe" });
        t.Line("Pipe", Narr, "There was a long, wet puff on the line. The khisha pipe, I assumed. I hoped.", new[] { "OnRefusedBefore", "On1", "On1b", "TreatyLost", "TreatyPart", "Lost1", "Lib1T", "Lib1", "Part1", "Part1Gold" });
        // the Zille treaty (ZilleTreaty, the turn after the war) already settled what these calls used to offer
        string signed = $"{V(ZilleTreaty.VarKind)} == ";
        t.Line("TreatyLost", Smolak, "\"I'm calling to be magnanimous. It's a new hobby. Our treaty is two months old and you have not broken it yet. In Wraclawitz they are calling it a miracle.\"",
            new[] { "TreatyLost2" }, condition: And(Lost, $"{signed}\"recognized\""));
        t.Line("TreatyLost2", Smolak, "\"No, I want nothing. That is the beauty of winning. Good night, Your Majesty. Give my regards to the house in Zille. Oh, wait.\"", new[] { "Bye" });
        t.Line("TreatyPart", Smolak, "\"Our border is two months old, Your Majesty, and already my customs men have confiscated a goat. A Rizian goat. I'm calling to ask for instructions.\"",
            new[] { "TreatyPart2" }, condition: And(Part, $"{signed}\"border\""));
        t.Line("TreatyPart2", Smolak, "\"I'm joking. I called to hear whether you regret it. You don't have to answer. Nobody ever does.\"", new[] { "Bye" });
        // the ceasefire he offered during the war (SupplyVoices) and was refused
        t.Line("OnRefusedBefore", Smolak, "\"You have turned down every offer I ever made you, Your Majesty. I told a whole square in Wraclawitz that you preferred funerals, and still I call. You see what this war has done to my pride.\"",
            new[] { "On1", "On1b" }, condition: And(Ongoing, $"{V(SupplyWar.VarSmolakAnswer)} == \"refused\""));

        // ongoing
        t.Line("On1", Smolak, "\"Your little war in Pales, my little war in Zille. It seems unfair that you have two and I have only one. Unless, of course, we close one of them tonight.\"",
            new[] { "OnHub" }, condition: And(Ongoing, PalesWar));
        t.Line("On1b", Smolak, "\"We have been killing each other's boys for months over a gulf and some hills. I'm tired. You sound tired. Let us close it tonight.\"",
            new[] { "OnHub" }, condition: And(Ongoing, $"not ({PalesWar})"));
        t.Logic("OnHub", new[] { "OnYes", "OnNo", "OnLespia" });
        t.Choice("OnYes", "\"A ceasefire on the current line. Have your generals call mine.\"", new[] { "OnYes2" }, script: Set(WarFlow.VarCeasefire, "true"));
        t.Line("OnYes2", Smolak, "\"You see? Two reasonable men and a telephone. The Alliance should hire us.\"", new[] { "Bye" });
        t.Choice("OnNo", "\"Rizia does not negotiate with the man who shelled its children.\"", new[] { "OnNo2", "OnNo3" },
            script: $"if {PalesWar} then " + Add("RiziaDLC.War_Pales_Resistance", 1) + " end; ");
        t.Line("OnNo2", Smolak, "\"Then don't be surprised if a few Wehzek volunteers find their way to Pales. Young men get so restless in the spring.\"", new[] { "Bye" }, condition: PalesWar);
        t.Line("OnNo3", Smolak, "\"Then more mud, and more letters to more mothers. Yours and mine.\"", new[] { "Bye" }, condition: $"not ({PalesWar})");
        t.Choice("OnLespia", "\"The Lespian navy sends its regards.\"", new[] { "OnLespia2" },
            condition: "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 and Variable[\"ZilleWar.Lespia_Withdrawn\"] ~= true");
        t.Line("OnLespia2", Smolak, "\"Alvarez! Of course. You know he sells me fuel too? Ask him where my convoys fill their tanks. Good night, Your Majesty.\"", new[] { "Bye" });

        // lost
        t.Line("Lost1", Smolak, "\"I'm calling to be magnanimous. It's a new hobby. I won, you lost, and now we should both stop pretending otherwise.\"", new[] { "Lost2" }, condition: Lost);
        t.Line("Lost2", Smolak, "\"Recognize Wehzek Zille. Properly, on paper, for a hundred years. Like your father's lease, but this time with no expiry date. In return, Wehlen releases the Rizian accounts we froze in Zille. Six billion guilders, Your Majesty. I had them counted twice.\"",
            new[] { "LostYes", "LostNo" });
        t.Choice("LostYes", "\"Send the papers. Rizia needs the money more than it needs the argument.\"", new[] { "LostYes2" }, cost: "+2 Budget",
            script: Set(VarRecognized, "true") + Add("RiziaDLC.Resources_Budget", 2) + Add("RiziaDLC.Country_HouseSazon", -3) + Add("RiziaDLC.Country_PublicOpinion", -1));
        t.Line("LostYes2", Smolak, "\"Wonderful. History will call it wisdom. Your mother will call it something else, I imagine.\"", new[] { "Bye" });
        t.Choice("LostNo", "\"Rizia will never sign Zille away again.\"", new[] { "LostNo2", "LostNo3" });
        t.Line("LostNo2", Smolak, "\"Then good luck in Pales. My boys will be watching from across the river with popcorn.\"", new[] { "Bye" }, condition: PalesWar);
        t.Line("LostNo3", Smolak, "\"Then we will talk again next year. I am a patient man. I had Zille for twenty-five years.\"", new[] { "Bye" }, condition: $"not ({PalesWar})");

        // liberated
        t.Line("Lib1T", Smolak, "\"I signed your treaty, Your Majesty. Every word. It says Wehlen gives up Zille. It says nothing about what the Wehzeks in Nanta think of you, and I have a very good memory for what people think.\"",
            new[] { "Lib2" }, condition: And(Lib, $"{V(ZilleTreaty.VarSigned)} == true"));
        t.Line("Lib1", Smolak, "\"I lost a province and you gained a headache. Every Wehzek in Nanta is a vote against you, Your Majesty, and I have a very good memory for votes.\"",
            new[] { "Lib2" }, condition: And(Lib, $"{V(ZilleTreaty.VarSigned)} ~= true"));
        t.Line("Lib2", Smolak, "\"It would be a shame if those poor people felt abandoned by their motherland. A shame for you, I mean.\"", new[] { "LibDeal", "LibThreat", "LibHang" });
        t.Choice("LibDeal", "\"Keep your agents out of Nanta, and the last of your prisoners go home.\"", new[] { "LibDeal2" },
            condition: $"{V(LateT8.VarPrisoners)} == \"held\" or {V(LateT8.VarPrisoners)} == \"ransomed\"",
            script: Set(VarNanta, "\"deal\"") + Set(LateT8.VarPrisoners, "\"released\""));
        t.Line("LibDeal2", Smolak, "\"Done. You will find Nanta very quiet. Suspiciously quiet. I'm told that's what peace sounds like.\"", new[] { "Bye" });
        t.Choice("LibThreat", "\"Touch Nanta, and the next thing you hear will be Rizian guns at the strait.\"", new[] { "LibThreat2" }, script: Set(VarNanta, "\"threat\""));
        t.Line("LibThreat2", Smolak, "\"So much anger. You won, Your Majesty. Try to enjoy it.\"", new[] { "Bye" });
        t.Choice("LibHang", "Hang up.", new[] { "LibHang2" });
        t.Line("LibHang2", Narr, "I handed the receiver back to the aide while Smolak was still talking.", new[] { "End" });

        // divided
        t.Line("Part1", Smolak, "\"Let's make the line a border. You keep your city, I keep my hills, and in a hundred years our grandchildren can argue about it at the Alliance.\"",
            new[] { "PartYes", "PartNo" }, condition: And(Part, $"not ({Auremina})"));
        t.Line("Part1Gold", Smolak, "\"Let's make the line a border. You keep your city and your gold hills, I keep my two harbors, and in a hundred years our grandchildren can argue about it at the Alliance.\"",
            new[] { "PartYes", "PartNo" }, condition: And(Part, Auremina));
        t.Choice("PartYes", "\"The line becomes a border. Let the Alliance draw it up.\"", new[] { "PartYes2" },
            script: Set(VarLinePermanent, "true") + Add("RiziaDLC.Country_HouseSazon", -2));
        t.Line("PartYes2", Smolak, "\"Wonderful. I'll have a sign made. Something tasteful. In both languages.\"", new[] { "Bye" });
        t.Choice("PartNo", "\"Zille is one province. A line of trenches doesn't change that.\"", new[] { "PartNo2" });
        t.Line("PartNo2", Smolak, "\"It changes it a little, Your Majesty. Ask anyone who lives on it.\"", new[] { "Bye" });

        t.Line("Bye", Narr, "The line went dead. Down in the port, somebody was setting off fireworks for a wedding.", new[] { "End" });
        t.End();
        t.Register();
    }
}
