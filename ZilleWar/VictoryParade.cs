using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// What a won war feels like.
/// - The end of the war moves public opinion: a liberation is a national celebration (+6), a divided Zille a relief (+2), a lost
///   war a grievance (-3, or -1 when the King sought the ceasefire himself). Each phase that reaches its aim lifts it by one.
/// - The Turn 6 House of Delegates election (vanilla c366, run inside the royal council) counts the war: a liberation gives the
///   Rizian National Coalition three more points, a divided Zille one; a lost war gives the RPP two (one after a ceasefire).
///   The council's results (SweepEarlyT6Home) say so.
/// - The turn after a liberation, the King rides into Zille in a victory parade (R_ForThePeople, then R_Fruition), names the
///   square that bore Smolak's name, and speaks to the crowd. After a divided war, a smaller homecoming to Zille city.
/// </summary>
internal static class VictoryParade
{
    private const string Conv = "ZilleWar/Late/VictoryParade";
    private const string Frag = "ZilleWar.Late_VictoryParade";
    public const string VarOffered = "ZilleWar.Victory_ParadeOffered";
    public const string VarEndTurn = "ZilleWar.Victory_EndTurn";
    public const string VarOpinionApplied = "ZilleWar.Victory_OpinionApplied";
    /// <summary>"fedo" | "liberation" | "romus" | "fallen" | "" (the square once called Smolak Square)</summary>
    public const string VarSquare = "ZilleWar.Victory_SquareName";
    public const string VarSpeech = "ZilleWar.Victory_Speech";

    private const string Opinion = "RiziaDLC.Country_PublicOpinion";
    private const string Estela = "Estela Toras";

    public static void Register()
    {
        foreach (string v in new[] { VarOffered, VarEndTurn, VarOpinionApplied, VarSquare, VarSpeech })
        {
            Variables.Register(v);
        }

        Vanilla.Expose(VarSquare, isBool: false);
        ConversationRegistry.RegisterConversation(Conv);

        // The election tally (vanilla c366 node 1 starts it): the war counts.
        Vanilla.AppendScript("Rizia/Turn 6/Calculations", 1,
            $"if {LK.Lib} then Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RNC\"] = (Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RNC\"] or 0) + 3 " +
            $"elseif {LK.Part} then Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RNC\"] = (Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RNC\"] or 0) + 1 " +
            $"elseif {LK.Lost} and not ({LK.Ceasefire}) then Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RPP\"] = (Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RPP\"] or 0) + 2 " +
            $"elseif {LK.Lost} then Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RPP\"] = (Variable[\"RiziaDLCIsolated.Turn06_VotingCalc_RPP\"] or 0) + 1 end");

        RegisterTalk();
    }

    /// <summary>Called once, when the war ends (WarFlow).</summary>
    public static void OnWarEnded()
    {
        if (Variables.GetBool(VarOpinionApplied))
        {
            return;
        }

        Variables.Set(VarOpinionApplied, true);
        Variables.Set(VarEndTurn, GameState.CurrentTurnNum ?? 0);
        bool lib = ZilleCampaign.Liberated;
        bool city = Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
        bool cease = Variables.GetBool(WarFlow.VarCeasefire);
        int delta = lib ? 6 : city ? 2 : cease ? -1 : -3;
        Variables.Set(Opinion, Variables.GetInt(Opinion) + delta);
        Core.Log.Msg($"The end of the war moves public opinion by {delta:+0;-0}.");
    }

    /// <summary>A phase reached its aim (or missed it): the country feels it.</summary>
    public static void OnPhaseResult(bool won) => Variables.Set(Opinion, Variables.GetInt(Opinion) + (won ? 1 : 0));

    /// <summary>Every step: offer the parade on the turn after a liberation (or the homecoming after a divided war).</summary>
    public static void OnStep()
    {
        if (!Variables.GetBool(WarFlags.Declared) || WarFlow.AtWar || Variables.GetBool(VarOffered))
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0, step = GameState.CurrentStepNum ?? 0;
        int end = Variables.GetInt(VarEndTurn);
        bool lib = ZilleCampaign.Liberated;
        bool part = !lib && Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
        if ((!lib && !part) || end <= 0 || turn <= end || step < 1 || turn > end + 2)
        {
            return;
        }

        try
        {
            GameState.AddCustomConversation(Conv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(Frag, SuzerainStoryPackInfo.Rizia, "Rizia_City_Zille",
                lib ? "Victory Parade in Zille" : "Homecoming in Zille City", lib ? "Fedo Square, Zille" : "Zille City",
                Il2Cpp.ConversationData.ConversationType.Personal, Conv));
            Variables.Set(VarOffered, true);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the victory parade failed: {ex}");
        }
    }

    private static void RegisterTalk()
    {
        var t = new Talk(Conv, "ZilleWar.Victory.");
        string lib = LK.Lib, part = LK.Part;
        string fedoUp = WarFlags.L(CityEvents.Flag("Zille_BannerRaised"));
        const string march = "PlayLoopedMusic(\"R_ForThePeople\",\"R_ForThePeople_Loop\"); ";
        const string climax = "PlayLoopedMusic(\"R_Fruition\",\"R_Fruition_Loop\"); ";
        t.Logic("Start", new[] { "Lib1", "Part1" }, hooks: AtStart(Conv));

        // ---------------------------------------------------------------- the parade
        t.Line("Lib1", Narr, "Zille had been dressed for the day as if for a wedding. The Wehzek street signs were gone, the shutters along the harbor front had been painted in the Crown's colors overnight, and every balcony on the way from the station flew a flag that had spent twenty-five years at the bottom of a drawer.",
            new[] { "Lib2" }, condition: lib, script: march);
        t.Line("Lib2", Narr, "I rode at the head of the column with General Carlos a length behind me. The men who had taken the city marched after us in their field uniforms, as I had ordered. Their boots were still the boots of the Zille front.",
            new[] { "Lib3" });
        t.Line("Lib3", Narr, "The crowd was thicker than the band was loud. Old women held up photographs of the city from before the lease. A boy on his father's shoulders saluted every company that passed, one after another, until his arm gave out.",
            new[] { "Lib4E", "Lib4" });
        t.Line("Lib4E", Narr, "My mother rode in the open car behind the guard of honor. She did not wave. She looked up at every window of the harbor district, one by one, as though counting them.",
            new[] { "Lib4M" }, condition: $"{Not(LateT8.VarEstelaHome)}");
        t.Line("Lib4M", Estela, "\"Twenty-five years, Romus. I had begun to think I would only ever see it again in my sleep.\"", new[] { "Lib4" });
        t.Line("Lib4", Narr, "At the end of the harbor road the column turned into the great square, the one Wehlen had named after its president. The Wehzek plaque had been pried off the wall the week our soldiers arrived. The square was waiting for a name.",
            new[] { "NameFedo", "NameMenu" });
        t.Line("NameFedo", Narr, "Someone had already screwed the old enamel sign back onto the wall, a little crooked: Fedo Square. The crowd cheered it as we passed, as if the sign itself had marched with us.",
            new[] { "Speech" }, condition: fedoUp, script: Set(VarSquare, "\"fedo\""));
        t.Logic("NameMenu", new[] { "Name1", "Name2", "Name3", "Name4" }, condition: $"not ({fedoUp})");
        t.Choice("Name1", "\"Give it back its old name. Fedo Square, as it was before the lease.\"", new[] { "Named" }, script: Set(VarSquare, "\"fedo\"") + Add("RiziaDLC.Country_HouseSazon", 1));
        t.Choice("Name2", "\"Liberation Square. Let every Wehzek who walks through it remember why.\"", new[] { "Named" }, script: Set(VarSquare, "\"liberation\"") + Add(Opinion, 1));
        t.Choice("Name3", "\"Romus Square. A king should not be shy about the things he gets right.\"", new[] { "NamedRomus" },
            script: Set(VarSquare, "\"romus\"") + Add("RiziaDLC.Resources_Authority", 1) + Add("RiziaDLC.LeaningValue_Democratic", -1));
        t.Choice("Name4", "\"The Square of the Fallen. It belongs to the men who didn't march in today.\"", new[] { "Named" }, script: Set(VarSquare, "\"fallen\""));
        t.Line("Named", Narr, "An aide wrote it down. By the evening the city's sign painters had it on the wall, and by the next morning half the city was already using it.", new[] { "Speech", "PartEnd" });
        t.Line("NamedRomus", Narr, "An aide wrote it down. By the evening the sign painters had my name on the wall, a little larger than I had asked for. Nobody in the crowd seemed to mind.", new[] { "Speech", "PartEnd" });

        t.Line("Speech", Narr, "They had built a wooden stand at the foot of the old customs house. When I climbed it, the band stopped, and the whole square went quiet at once, the way a church does.",
            new[] { "S1", "S2", "S3" }, condition: lib, script: climax);
        t.Choice("S1", "\"This victory belongs to the soldiers who won it. Every one of them who marched today, and every one who could not.\"", new[] { "S1a" },
            script: Set(VarSpeech, "\"soldiers\"") + Add(Opinion, 1));
        t.Line("S1a", Narr, "The soldiers in the square stood straighter. Somewhere in the crowd, a woman started to cry, and then she started to cheer, and then the whole square was cheering with her.", new[] { "Close" });
        t.Choice("S2", "\"People of Zille, you are home. You were always home. It only took the rest of us twenty-five years to come and get you.\"", new[] { "S2a" },
            script: Set(VarSpeech, "\"home\"") + Add(Opinion, 2));
        t.Line("S2a", Narr, "The roar came up off the square like heat off a road. The bells of the harbor sanctuary started ringing on their own, and nobody ever found out who had climbed the tower.", new[] { "Close" });
        t.Choice("S3", "\"Let the world remember what this Crown promised, and what this Crown delivered.\"", new[] { "S3a" },
            script: Set(VarSpeech, "\"crown\"") + Add("RiziaDLC.Resources_Authority", 1));
        t.Line("S3a", Narr, "The cheering came a beat late, and then it came all at once. From the stand I could see the strait beyond the harbor, and beyond it, very faint, the Wehzek shore.", new[] { "Close" });
        t.Line("Close", Narr, "The parade lasted until dusk and the celebrations until morning. When I left the city, the street sweepers were still filling carts with paper flags.",
            new[] { "CloseE", "End" });
        t.Line("CloseE", Estela, "\"You kept your word, darling. Now go back to Porte Drazon and keep the rest of them.\"", new[] { "End" }, condition: $"{Not(LateT8.VarEstelaHome)}");

        // ---------------------------------------------------------------- the homecoming (divided)
        t.Line("Part1", Narr, "There was no parade. There were not enough streets left in Zille city for one, and the armistice line ran along the hills above the harbor, close enough to see the Wehzek flags on the far ridge.",
            new[] { "Part2" }, condition: part, script: "PlayLoopedMusic(\"R_Sunrise\",\"R_Sunrise_Loop\"); ");
        t.Line("Part2", Narr, "So I walked. From the station to the harbor, with the Golden Guard and a handful of officers, through crowds that did not cheer so much as press closer, to touch a sleeve or hold up a child.",
            new[] { "Part3" });
        t.Line("Part3", Narr, "In the great square, the Wehzek plaque had been pried off the wall. The square was waiting for a name.", new[] { "NameFedoP", "NameMenu" });
        t.Line("NameFedoP", Narr, "Someone had already screwed the old enamel sign back onto the wall: Fedo Square. An old man stood under it with his hat in his hands, as if at a grave.",
            new[] { "PartEnd" }, condition: fedoUp, script: Set(VarSquare, "\"fedo\""));
        t.Line("PartEnd", Narr, "I stayed until dark. On the ridge across the line, someone was watching us through field glasses. Half of Zille was home. The other half was close enough to wave.",
            new[] { "End" }, condition: part);
        t.End();
        t.Register();
    }

    private static string Narr => Narrator;

    private static string Not(string v) => $"Variable[\"{v}\"] ~= true";
}
