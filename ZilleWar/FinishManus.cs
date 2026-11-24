using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.LK;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Manus Sazon at the Zille front. Vanilla's Turn 7 talk with Hugo (c395) can only send Manus to Pales. While the Zille
/// war is fought (or Zille city sits on an armistice line), Hugo can suggest Zille instead and the King can send him there
/// with the Brenas levies: Zille was taken from Brenas and House Sazon in 1927. The vanilla machinery for a Manus at war
/// is reused (Manus_SendToWar, Manus_Gone, the protest decision, the codex, the funeral), with its Pales wording rewritten;
/// his fate follows the next Zille operation instead of the Pales war:
/// as in vanilla, he dies if the House Sazon unit is destroyed (Army_Unit_Sazon_Dead rises after he left: "killed",
/// vanilla Manus_DiesInWar); otherwise won lightly "decorated", won or lost at a heavy cost "wounded", lost lightly or the
/// war over without another battle "line" (holding the armistice line, or having covered the retreat).
/// Vina reacts at the dinner and again when the news comes; Leona, Angelica and the endings remember where he served.
/// </summary>
internal static class FinishManus
{
    public const string VarCall = "ZilleWar.Fin_ManusCall";            // the King chose Zille on the phone (c395)
    public const string VarZille = "ZilleWar.Fin_ManusZille";          // Manus went to the Zille front (set when he accepts)
    public const string VarMissing = "ZilleWar.Fin_ManusMissing";      // fate "missing" (expresso)
    public const string VarOps = "ZilleWar.Fin_ManusOpsAtSend";        // operations done when he left
    public const string VarSazonDeadAtSend = "ZilleWar.Fin_SazonDeadAtSend"; // vanilla Army_Unit_Sazon_Dead when he left
    private const string SazonDead = "RiziaDLC.Army_Unit_Sazon_Dead";
    public const string VarFate = "ZilleWar.Fin_ManusFate";            // "" | decorated | wounded | killed | missing | line
    public const string VarFateTurn = "ZilleWar.Fin_ManusFateTurn";
    /// <summary>Manus came back decorated or wounded: House Sazon's +1 (a House row).</summary>
    private const string VarHonored = "ZilleWar.Fin_ManusHonored";
    /// <summary>The King promised Vina a medal for Manus, or the royal train to his bedside: House Sazon's +1 (a House row).</summary>
    private const string VarVinaSazon = "ZilleWar.Fin_ManusVinaSazon";
    private const string VarFateReported = "ZilleWar.Fin_ManusFateReported";
    private const string VarPresumedReported = "ZilleWar.Fin_ManusPresumedReported";
    private const string VarSentNews = "ZilleWar.Fin_ManusSentNews";
    private const string VarVinaDone = "ZilleWar.Fin_ManusVinaDone";
    private const string VarVinaOffered = "ZilleWar.Fin_ManusVinaOffered";
    private const string VarVinaAnswer = "ZilleWar.Fin_ManusVinaAnswer"; // "proud" | "duty" | "visit" | "stay" | "sorry" | "hero" | "kyrute" | "search"

    private const string HugoManus = "Rizia/Turn 7/A_HugoManusPrivateTalk";
    private const string Dinner = "Rizia/Turn 7/P_TorasDinner";
    private const string SazonT10 = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE SAZON CONVO";
    private const string Strike = "Rizia/Turn 10/STRIKE SCENE/INTRO_NONREFORM/INTRO_NONREFORM Conversation";
    private const string Parade = "Rizia/Turn 10/War_Pales_VictoryParade";
    private const string Rescue = "Rizia/Turn 10/VinaRescueOutcome";
    private const string Continued = "Rizia/Continued Reign";
    private const string VinaConv = "ZilleWar/Finish/VinaManus";

    private const string Hugo = "Hugo Toras";
    private const string Manus = "Manus Sazon";
    private const string Vina = "Vina Toras";
    private const string Leona = "Leona Sazon";
    private const string Angelica = "Angelica Sazon";
    private const string R = Talk.Romus;
    private const string Narr = Talk.Narrator;

    // ------------------------------------------------------------------ Lua

    private static readonly string Call = WarFlags.L(VarCall);
    private static readonly string ZM = WarFlags.L(VarZille);
    private static readonly string NotDead = "Variable[\"RiziaDLC.Manus_DiesInWar\"] ~= true";
    private static string Fate(string f) => $"Variable[\"{VarFate}\"] == \"{f}\"";
    private static readonly string Serving = $"(Variable[\"{VarFate}\"] == nil or Variable[\"{VarFate}\"] == \"\")";
    private static readonly string Missing = And(ZM, Fate("missing"), NotDead);
    private static readonly string Alive = And(ZM, NotDead, $"not ({Fate("missing")})", $"not ({Fate("killed")})");
    private static readonly string Wounded = And(Alive, Fate("wounded"));
    private static readonly string AliveWhole = And(Alive, $"not ({Fate("wounded")})");
    private static readonly string Over = WarFlags.L(WarFlags.Over);

    /// <summary>There is a front in Zille to send him to: the war is fought, or an armistice line runs through Zille.</summary>
    private static readonly string Front = $"({Ongoing}) or ({Part})";

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarCall, VarZille, VarMissing, VarOps, VarSazonDeadAtSend, VarFate, VarFateTurn, VarFateReported, VarPresumedReported, VarSentNews,
                     VarVinaDone, VarVinaOffered, VarVinaAnswer, VarHonored, VarVinaSazon,
                 })
        {
            Variables.Register(v);
        }

        Vanilla.Expose(VarZille);
        Vanilla.Expose(VarMissing);
        Vanilla.Expose(VarHonored);
        Vanilla.Expose(VarVinaSazon);
        // House Sazon's standing is rebuilt from scratch at every story fragment (Calculation_Relations_Houses), so the House
        // effects of sending Manus, of his fate and of Vina's answers are rows keyed on saved flags, not one-off script changes.
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarZille} == true", "RiziaDLC.Country_HouseSazon += 1;");
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarHonored} == true", "RiziaDLC.Country_HouseSazon += 1;");
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarMissing} == true", "RiziaDLC.Country_HouseSazon -= 1;");
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarVinaSazon} == true", "RiziaDLC.Country_HouseSazon += 1;");

        // The Pales war's rules for Manus's death do not apply to a Manus in Zille; "missing" keeps vanilla's Turn 10
        // presumed-dead rule (its Pales_Won test is false in a game without a won Pales war).
        Vanilla.GateInstruction("War_IsManusDead", null, WarFlags.X(VarZille, false));
        Vanilla.GateInstruction("War_ManusMIA_Turn10_Step7", null, $"({WarFlags.X(VarZille, false)} || {WarFlags.X(VarMissing)})");
        // Vanilla's reports are filed on the Pales token; ours go on Zille.
        Vanilla.HideReport("Reports_Floating_ManusDiesInWar", () => Variables.GetBool(VarZille));
        Vanilla.HideReport("Reports_Floating_ManusPresumedDead", () => Variables.GetBool(VarZille));

        RegisterHugoManus();
        RegisterDinner();
        RegisterTurn10();
        RegisterVinaScene();
        RegisterAdarfo();
    }

    // ------------------------------------------------------------------ c445 Turn 9: Adarfo on what became of Manus

    private static void RegisterAdarfo()
    {
        // Vanilla [276] reads Manus_SendToWar as the Pales war: "You sent him to die in your unjustified war." A Manus sent to the
        // Zille line after the fighting is alive and standing guard.
        const string conv = "Rizia/Turn 9/TalkWithAdarfo";
        var t = new Talk(conv, "ZilleWar.Fin.Adarfo9.");
        t.Variant("Guard", 279, new[] { 276 }, And(Alive, $"not ({Lost})"), "Adarfo Sotelato",
            "\"You sent him to Zille to stand guard over what your war left behind, as far from Porte Drazon as you could put him. Because he advocated for people's rights.\"",
            new[] { "#281" });
        t.Variant("GuardLost", 279, new[] { 276 }, And(Alive, Lost), "Adarfo Sotelato",
            "\"You sent him to stand guard on the border for your war, as far from Porte Drazon as you could put him. Because he advocated for people's rights.\"",
            new[] { "#281" });
        t.Register();
    }

    // ------------------------------------------------------------------ c395 Turn 7: Hugo, then the call

    private static void RegisterHugoManus()
    {
        var t = new Talk(HugoManus, "ZilleWar.Fin.Manus7.");

        // Hugo's ways to deal with Manus: vanilla only thinks of Pales.
        t.Choice("HugoAsk", "\"Such as ordering him to fight in Zille?\"", new[] { "HugoZ" }, condition: Front, hooks: Talk.From(535));
        t.Choice("HugoShip", "\"I could always ship him off to the front in Zille.\"", new[] { "HugoZ" }, condition: Front, hooks: Talk.From(1502));
        t.Line("HugoZ", Hugo,
            "\"A most elegant solution. Zille was taken from his family, after all. Let the Sazon heir go and win it back. I doubt he's much " +
            "of a fighter, but his charisma will work wonders on the Brenas troops.\"", new[] { "#540", "#541", "#986" });

        // The call.
        t.Choice("Send", "\"The time has come for you to do your patriotic duty. I am sending you to lead your province's battalion in Zille.\"",
            new[] { "#553" },
            condition: And(Front, "Variable[\"RiziaDLC.SuOmina_OrderedToKillManus\"] ~= true", "Variable[\"RiziaDLC.Manus_Arrested\"] ~= true"),
            script: Set(VarCall, "true"), hooks: Talk.From(550));
        t.Variant("M555", 555, new[] { 554 }, Call, Manus,
            "\"Zille. My family hasn't set foot there since my father's... since 1927. Every Sazon in Brenas will tell you it was stolen from us.\"",
            new[] { "M555b" });
        t.Line("M555b", Manus, "\"I am honored, of course. But I have no military training.\"", new[] { "#556", "#562", "#561", "#558" });

        // Pales named in the rest of the call; Vina's rescue is not his mission in Zille.
        Vanilla.Require(HugoManus, 557, $"not ({Call})");
        t.Variant("R576", 576, new[] { 574 }, Call, R, "\"All right. If you would rather leave Zille to the Wehzeks, you don't have to go.\"", new[] { "#587" });
        t.Variant("R571", 571, new[] { 600, 605 }, Call, R, "\"You are going to Zille, Mr. Sazon. That's a royal order.\"", new[] { "#581" });
        t.Variant("M589", 589, new[] { 588 }, Call, Manus, "\"I would not dare to disobey my king. I will go to Zille.\"", new[] { "#583" });
        t.Variant("M586", 586, new[] { 584 }, Call, Manus, "\"And I will do everything I can to bring Zille home.\"", new[] { "#592" });
        t.Variant("R609", 609, new[] { 608 }, Call, R,
            "\"One battle at a time, Mr. Sazon. When you return victorious from Zille, we will turn back to our reforms.\"", new[] { "#612" });
        t.Variant("R655", 655, new[] { 653 }, Call, R, "\"Say farewell to Vina tonight. I want you in Zille as soon as possible.\"", new[] { "#661" });
        t.Register();

        // He accepted (vanilla 1174 sets Manus_SendToWar): he goes north, and the Brenas levies with him.
        Vanilla.AppendScript(HugoManus, 1174,
            $"if {Call} then " + Set(VarZille, "true") +
            $"Variable[\"{VarOps}\"] = Variable[\"{ZilleCampaign.VarOpsDone}\"] or 0; " +
            $"Variable[\"{VarSazonDeadAtSend}\"] = Variable[\"{SazonDead}\"] or 0; " +
            $"if {Ongoing} then {Talk.Weaken(1)} end " +
            "end");
    }

    // ------------------------------------------------------------------ c394 Turn 7: Vina at dinner

    private static void RegisterDinner()
    {
        var t = new Talk(Dinner, "ZilleWar.Fin.Dinner.");
        t.Line("Vina", Vina,
            "\"You sent him to Zille to keep him away from me. He's no soldier, Father. He could die at any time, and you would call it his family's duty.\"",
            new[] { "#257", "#234", "#239", "#238" }, condition: ZM, hooks: Talk.From(230));
        Vanilla.Require(Dinner, 233, $"not ({ZM})");
        Vanilla.Require(Dinner, 58, $"not ({ZM})");
        t.Register();
    }

    // ------------------------------------------------------------------ Turn 10 and the endings

    private static void RegisterTurn10()
    {
        // The Sazons at the birthday gathering.
        var s = new Talk(SazonT10, "ZilleWar.Fin.Sazon10.");
        s.Variant("Front", 171, new[] { 169 }, And(ZM, Serving, NotDead, $"not ({Lost})"), Leona, "\"He writes from Zille now and again.\"", new[] { "#172" });
        // Zille city is Wehzek after a lost war: he writes from the border, not from Zille.
        s.Variant("FrontLost", 171, new[] { 169 }, And(ZM, Serving, NotDead, Lost), Leona, "\"He writes from the border now and again.\"", new[] { "#172" });
        s.Variant("Medal", 171, new[] { 169 }, And(Alive, Fate("decorated"), $"not ({Lost})"), Leona,
            "\"He writes from Zille now and again. They pinned a medal on him after the last offensive, and he hasn't mentioned it once.\"",
            new[] { "#172" });
        s.Variant("MedalLost", 171, new[] { 169 }, And(Alive, Fate("decorated"), Lost), Leona,
            "\"He writes from the border now and again. They pinned a medal on him after the last offensive, and he hasn't mentioned it once.\"",
            new[] { "#172" });
        s.Variant("Hand", 171, new[] { 169 }, And(Wounded, $"not ({Lost})"), Leona,
            "\"He writes from Zille now and again. His left hand still shakes from the shrapnel, though you'd never know it from his letters.\"",
            new[] { "#172" });
        s.Variant("HandLost", 171, new[] { 169 }, And(Wounded, Lost), Leona,
            "\"He writes from the border now and again. His left hand still shakes from the shrapnel, though you'd never know it from his letters.\"",
            new[] { "#172" });
        s.Variant("Line", 171, new[] { 169 }, And(Alive, Fate("line"), $"not ({Lost})"), Leona,
            "\"He writes from the line in Zille now and again. Mud, sentries and Wehzek loudspeakers, mostly.\"", new[] { "#172" });
        s.Variant("LineLost", 171, new[] { 169 }, And(Alive, Fate("line"), Lost), Leona,
            "\"He writes from the border now and again. Mud, sentries and Wehzek loudspeakers, mostly.\"", new[] { "#172" });
        s.Variant("NoWord", 171, new[] { 169 }, Missing, Leona,
            "\"We haven't had a letter since the fighting stopped. The army lists him as missing, and the Wehzeks say they have no record of him.\"",
            new[] { "#93" });

        string[] after879 = { "Hero_On", "Hero_Over", "#891", "#892", "#893" };
        s.Variant("Lineage", 879, new[] { 881 }, Alive, Angelica,
            "\"Zille was taken from this family before Manus was born, Your Majesty. If this war has a silver lining, it's that my son has learned the true meaning of responsibility to his kingdom.\"",
            after879);
        s.Variant("LostA", 879, new[] { 881 }, Missing, Angelica,
            "\"The army says my son is missing, Your Majesty. Missing. As if he were an umbrella someone left on a train.\"", new[] { "#929" });
        s.Variant("LostL", 883, new[] { 882 }, Missing, Leona,
            "\"We still have no word of Manus, Your Majesty. Every morning I tell myself that no news is good news.\"", new[] { "#929" });
        Vanilla.Require(SazonT10, 888, $"not ({Alive})");
        s.Choice("Hero_On", "\"Manus and the Sazon troops have fought admirably. When Zille is ours, he will get a hero's welcome.\"", new[] { "#921" },
            condition: And(Alive, Ongoing), hooks: Talk.From(879, 883));
        s.Choice("Hero_Over", "\"Manus and the Sazon troops fought admirably in Zille. When he comes home, he will get a hero's welcome.\"", new[] { "#921" },
            condition: And(Alive, $"not ({Ongoing})"), hooks: Talk.From(879, 883));
        Swap(s, SazonT10, "Proud", 925, new[] { 927 }, Angelica, new S(
            Ongoing: "\"The war will go on for as long as it has to, Leona. Our family lost Zille once. I'm proud that we are doing our part to win it back.\"",
            Lib: "\"And now it's over, Leona. Our family lost Zille in 1927, and a Sazon helped win it back. I am proud of that.\"",
            Lost: "\"We have lost Zille twice now, Leona. But no one will ever say a Sazon didn't fight for it.\""),
            new[] { "#929" }, when: Alive);
        // divided: the cities won back, by name; on an armistice line or on the border the Mave Nortus Treaty drew
        SweepEarlyKit.ForDivided((id, cond, ours, _, _) => Swap(s, SazonT10, "Proud" + id, 925, new[] { 927 }, Angelica, new S(
            Part: $"\"{ours} {(ours.Contains(" and ") ? "are" : "is")} ours again, Leona, and a Sazon stands guard on the line. I am proud of that.\"",
            PartBorder: $"\"{ours} {(ours.Contains(" and ") ? "are" : "is")} ours again, Leona, and a Sazon stands guard on the new border. I am proud of that.\""),
            new[] { "#929" }, when: And(Alive, cond)));
        Swap(s, SazonT10, "ProudAll", 925, new[] { 927 }, Angelica, new S(PartBorder:
            "\"All of Zille is ours again, Leona, by arms and by treaty, and a Sazon helped win it back. I am proud of that.\""),
            new[] { "#929" }, when: And(Alive, ZilleWar.Front.LWehzekExactly(Array.Empty<string>())));
        // Manus fell in Zille and the war is over: no "when the war is over".
        string fellAfter = And(ZM, "Variable[\"RiziaDLC.Manus_DiesInWar\"] == true", $"not ({Ongoing})", "Variable[\"RiziaDLC.Pales_War\"] ~= true");
        s.Variant("Minds", 1394, new[] { 1392 }, fellAfter, R,
            "\"It's good that he's out of harm's way. Rizia's going to need bright young minds now that the war is over.\"", new[] { "#1402" });
        Vanilla.Require(SazonT10, 1399, $"not ({fellAfter})");
        s.Register();

        // Leona at the strike: "our heir is fighting to reclaim Pales for you".
        var k = new Talk(Strike, "ZilleWar.Fin.Strike10.");
        k.Variant("Heir_On", 328, new[] { 331 }, And(Alive, Ongoing), Leona, "\"As we speak, our heir is fighting to reclaim Zille for you.\"", new[] { "#43" });
        k.Variant("Heir_Over", 328, new[] { 331 }, And(Alive, $"not ({Ongoing})", $"not ({Lost})"), Leona,
            "\"As we speak, our heir is standing guard in Zille, on your orders.\"", new[] { "#43" });
        k.Variant("Heir_Lost", 328, new[] { 331 }, And(Alive, Lost), Leona,
            "\"As we speak, our heir is home from Zille, on your orders, and not saying much about it.\"", new[] { "#43" });
        k.Variant("Heir_Missing", 328, new[] { 331 }, Missing, Leona,
            "\"Our heir has been missing since the fighting in Zille. You sent him there, Your Majesty.\"", new[] { "#43" });
        k.Register();

        // The Pales victory parade: Manus is not among the Brenas troops.
        var p = new Talk(Parade, "ZilleWar.Fin.Parade10.");
        // He is not there: skip the medals, the achievement and his return home (vanilla [372] -> [161] -> [363]...), to the tanks ([39]).
        p.Variant("Absent", 160, new[] { 158 }, And(Alive, $"not ({Lost})"), Narr,
            "The Brenas levies marched without their commander. Manus Sazon had stayed behind with his battalion in Zille.", new[] { "#39" });
        p.Variant("AbsentLost", 160, new[] { 158 }, And(Alive, Lost), Narr,
            "The Brenas levies marched without their commander. Manus Sazon had stayed behind with his battalion on the border.", new[] { "#39" });
        p.Variant("Saddle", 160, new[] { 158 }, Missing, Narr,
            "The Brenas levies marched behind a riderless horse. Their commander, Manus Sazon, had been missing since Zille.", new[] { "#39" });
        p.Register();

        // Vina freed from Pales asks after Manus.
        var r = new Talk(Rescue, "ZilleWar.Fin.Rescue10.");
        r.Variant("Still", 127, new[] { 125 }, And(Alive, $"not ({Lost})"), R, "\"Manus is still in Zille, sweetheart. We'll have this conversation when he returns.\"", new[] { "#142" });
        r.Variant("StillLost", 127, new[] { 125 }, And(Alive, Lost), R, "\"Manus is on his way home from Zille, sweetheart. We'll have this conversation when he gets here.\"", new[] { "#142" });
        r.Variant("Missing", 127, new[] { 125 }, Missing, R, "\"Manus is missing, sweetheart. We'll have this conversation if he comes back to us.\"", new[] { "#142" });
        r.Register();

        // Continued Reign: his medals.
        var c = new Talk(Continued, "ZilleWar.Fin.Continued.");
        string[] reign = { "#280", "#281", "#633" };
        c.Variant("Medals", 278, new[] { 276 }, And(AliveWhole, Fate("decorated")), Narr,
            "You spotted Manus Sazon at the edge of the crowd. His medals from the Zille campaign gleamed on his chest.\n\nYou...", reign);
        // sent after the fighting (T7 s7): he held the line, and no campaign medal is his
        c.Variant("MedalsLine", 278, new[] { 276 }, And(AliveWhole, $"not ({Fate("decorated")})"), Narr,
            "You spotted Manus Sazon at the edge of the crowd, still in the uniform he had worn on the line in Zille.\n\nYou...", reign);
        c.Variant("MedalsArm", 278, new[] { 276 }, Wounded, Narr,
            "You spotted Manus Sazon at the edge of the crowd. His medals from the Zille campaign gleamed on his chest, and he still favored his left arm.\n\nYou...",
            reign);
        c.Variant("MedalsMissing", 278, new[] { 276 }, Missing, Narr,
            "You looked for Manus Sazon at the edge of the crowd. The army still listed him as missing from Zille, and nobody in the hall had an answer for his family.\n\nYou...", reign);
        c.Register();
    }

    // ------------------------------------------------------------------ Vina hears the news

    private static void RegisterVinaScene()
    {
        ConversationRegistry.RegisterConversation(VinaConv);
        var t = new Talk(VinaConv, "ZilleWar.Fin.VinaManus.");
        t.Logic("Start", new[] { "Open" }, hooks: Talk.AtStart(VinaConv), script: "PlaySceneMusic(Suspense);");
        t.Line("Open", Narr, "I was still reading the dispatch from Zille when the door of my study opened without a knock.",
            new[] { "Dec1", "Wou1", "Kil1", "Mis1" }, script: $"AddConversant(\"{Vina}\");");

        // decorated
        t.Line("Dec1", Vina, "\"They gave him a medal. Did you know? His aunt read it in the Herald before anyone thought to tell me.\"",
            new[] { "Dec2" }, condition: Fate("decorated"));
        t.Line("Dec2", Vina, "\"He led a charge, apparently. Manus. Who used to apologize to waiters.\"", new[] { "DecA", "DecB" });
        t.Choice("DecA", "\"He earned it. I will pin the next one on him myself.\"", new[] { "DecAr" },
            script: Set(VarVinaAnswer, "\"proud\"") + Set(VarVinaSazon, "true"));
        t.Line("DecAr", Vina, "\"Then bring him home to get it. That's all I'm asking.\"", new[] { "Close" });
        t.Choice("DecB", "\"He did his duty. So did thousands of others.\"", new[] { "DecBr" }, script: Set(VarVinaAnswer, "\"duty\""));
        t.Line("DecBr", Vina, "\"Thousands of others aren't the man I love. But I suppose that's never mattered much in this house.\"", new[] { "Close" });

        // wounded
        t.Line("Wou1", Vina, "\"He's in the field hospital at Dapalle. Shrapnel, they said. They said it the way you'd talk about the weather.\"",
            new[] { "Wou2" }, condition: Fate("wounded"));
        t.Line("Wou2", Vina, "\"I want to go to him. I'm not asking for permission, Father. I'm asking for a car.\"", new[] { "WouA", "WouB" });
        t.Choice("WouA", "\"Take the royal train. And take him whatever he needs.\"", new[] { "WouAr" },
            script: Set(VarVinaAnswer, "\"visit\"") + Set(VarVinaSazon, "true"));
        t.Line("WouAr", Narr, "For a moment she looked as if she might hug me. Then she was gone, and I heard her calling for her coat halfway down the corridor.",
            new[] { "End" });
        t.Choice("WouB", "\"A princess has no place in a field hospital. The surgeons will look after him.\"", new[] { "WouBr" },
            script: Set(VarVinaAnswer, "\"stay\""));
        t.Line("WouBr", Vina, "\"No. Of course not. A princess's place is wherever she looks best in the photographs.\"", new[] { "Close" });

        // killed
        t.Line("Kil1", Narr, "Her eyes were red. She did not sit down.", new[] { "Kil2" }, condition: Fate("killed"));
        t.Line("Kil2", Vina, "\"You sent him there. You sent him to the one place his family could never refuse.\"", new[] { "KilA", "KilB" });
        t.Choice("KilA", "\"I'm sorry, sweetheart. I am so sorry.\"", new[] { "KilAr" }, script: Set(VarVinaAnswer, "\"sorry\""));
        t.Line("KilAr", Vina, "\"Don't. Not today. Tomorrow you can be sorry in front of the cameras.\"", new[] { "Close" });
        t.Choice("KilB", "\"He died a hero. Brenas will never forget him.\"", new[] { "KilBr" }, script: Set(VarVinaAnswer, "\"hero\""));
        t.Line("KilBr", Vina, "\"Brenas can keep its heroes. I wanted him.\"", new[] { "Close" });

        // missing
        t.Line("Mis1", Vina, "\"Missing. Do you know what that word means in a report? It means nobody went back to look.\"",
            new[] { "MisA", "MisB" }, condition: Fate("missing"));
        t.Choice("MisA", "\"I will ask Kyrute to press Wraclawitz for its list of prisoners. If he is alive, we will find him.\"", new[] { "MisAr" },
            script: Set(VarVinaAnswer, "\"kyrute\""));
        t.Line("MisAr", Vina, "\"Then ask today. Not after the next council meeting. Today.\"", new[] { "Close" });
        t.Choice("MisB", "\"Our soldiers are searching. That is all I can promise.\"", new[] { "MisBr" }, script: Set(VarVinaAnswer, "\"search\""));
        t.Line("MisBr", Vina, "\"Our soldiers are the ones who left him there.\"", new[] { "Close" });

        t.Line("Close", Narr, "She left without another word. The dispatch was still in my hand.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ every step

    public static void OnStep()
    {
        bool zm = Variables.GetBool(VarZille);
        ApplyTexts(zm);
        if (!zm)
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0;
        if (!Variables.GetBool(VarSentNews))
        {
            Variables.Set(VarSentNews, true);
            NewsWire.Report("Fin_ManusSent", WorldSystems.ZilleToken, "Sazon Heir Joins the Brenas Levies",
                "Manus Sazon, heir to House Sazon and until this week leader of the Rizian People's Party, has left Porte Drazon to command the " +
                "Brenas provincial battalion on the Zille front. Officers report that several hundred volunteers from Iza and Sallabes followed " +
                "him north within days, many of them the grandsons of men who lost their land in Zille in 1927. The General Staff has attached " +
                "the battalion to the forces facing the Wehzek garrison.");
        }

        ResolveFate(turn);
        ReportPresumedDead();
        OfferVina(turn);
    }

    private static void ResolveFate(int turn)
    {
        // an unset variable reads "nil", which is no fate yet
        if (ZilleCampaign.Str(VarFate).Length > 0)
        {
            return;
        }

        int sent = Variables.GetInt(VarOps);
        int done = ZilleCampaign.OperationsDone;
        string fate = null;
        string objective = null;
        if (done > sent)
        {
            int k = sent + 1;
            // a phase that took a city and lost none counts as a success here, even if it missed part of its aim (Nanta, not Isor)
            ZilleWar.Front.Phase phase = ZilleWar.Front.Of(k);
            bool won = Variables.GetBool(ZilleCampaign.ResultVar(k)) && (phase == null || phase.Lost.Count == 0) || phase is { Gain: true };
            int dead = Variables.GetInt(ZilleCampaign.PlayerDeadVar(k));
            // As in vanilla, Manus lives and dies with the House Sazon unit: if it was destroyed, so was he. Otherwise the
            // operation decides: a victory decorates him (or wounds him if it was costly), a defeat sends him back wounded
            // from a bloody retreat or leaves him holding the new line.
            bool sazonLost = Variables.GetInt(SazonDead) > Variables.GetInt(VarSazonDeadAtSend);
            fate = sazonLost ? "killed" : won ? (dead >= 3 ? "wounded" : "decorated") : (dead >= 3 ? "wounded" : "line");
            string[] hist = ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (hist.Length >= k && Enum.TryParse(hist[k - 1].Split(':')[0], out Objective o))
            {
                objective = ZilleCampaign.Info(o).Codename;
            }
        }
        else if (!WarFlow.AtWar)
        {
            fate = "line";
        }

        if (fate == null)
        {
            return;
        }

        Variables.Set(VarFate, fate);
        Variables.Set(VarFateTurn, turn);
        Variables.Set(VarMissing, fate == "missing");
        // House Sazon's +1 / -1 for these fates are rows keyed on VarHonored and VarMissing (see Register)
        Variables.Set(VarHonored, fate is "decorated" or "wounded");
        switch (fate)
        {
            case "killed":
                Variables.Set("RiziaDLC.Manus_DiesInWar", true);
                Variables.Set("RiziaDLCSupport.News_Floating_RH_ManusFuneral", true);
                break;
        }

        PublishFate(fate, objective);
    }

    private static void PublishFate(string fate, string objective)
    {
        if (Variables.GetBool(VarFateReported))
        {
            return;
        }

        Variables.Set(VarFateReported, true);
        string op = string.IsNullOrEmpty(objective) ? "the last offensive" : objective;
        string token = WorldSystems.ZilleToken;
        try
        {
            switch (fate)
            {
                case "decorated":
                    NewsWire.Report("Fin_ManusDecorated", token, "Sazon Heir Decorated",
                        $"Manus Sazon has been awarded the Cross of Valor for his conduct during {op}. According to his battalion's officers, he led " +
                        "the Brenas levies forward on foot when their company commander fell, and held the position until relieved. Mr. Sazon, who " +
                        "had no military training before the war, is said to have asked that the citation name his men rather than himself.");
                    NewsWire.Publish("Fin_ManusDecorated", "RH", "Manus Sazon Decorated for Bravery",
                        $"The heir to House Sazon has returned the Crown's faith in him with interest. During {op}, Manus Sazon led the Brenas " +
                        "levies through the Wehzek line after their commander was struck down, and has been awarded the Cross of Valor for it.\n\n" +
                        "\"I only went where my men were already going,\" Mr. Sazon told a reporter from the front. In Iza, crowds gathered outside " +
                        "Palas Sazon to cheer a name that, for a quarter of a century, the city spoke only in whispers.");
                    break;
                case "wounded":
                    NewsWire.Report("Fin_ManusWounded", token, "Sazon Heir Wounded",
                        $"Manus Sazon was wounded by shell fragments during {op} while leading the Brenas battalion forward. He was evacuated to the " +
                        "field hospital at Dapalle, where surgeons describe his injuries to the arm and shoulder as serious but not life-threatening. " +
                        "He has been recommended for the Cross of Valor and has asked to return to his battalion as soon as he can hold a rifle.");
                    break;
                case "missing":
                    NewsWire.Report("Fin_ManusMissing", token, "Manus Sazon Missing",
                        $"Manus Sazon has not been accounted for since the collapse of our line during {op}. Survivors of the Brenas battalion last " +
                        "saw him organizing the withdrawal of the wounded. Wraclawitz has neither confirmed nor denied holding him prisoner.");
                    NewsWire.Publish("Fin_ManusMissing", "V", "Where Is Manus Sazon?",
                        "The King sent the leader of the opposition to the front, and now nobody can tell us where he is. Manus Sazon has been missing " +
                        $"since {op}, and the army that ordered him north answers every question with the same three words: inquiries are ongoing.\n\n" +
                        "His supporters in Iza have started holding vigils outside the provincial barracks. We ask what they ask: who will answer " +
                        "for him?");
                    break;
                case "killed":
                    NewsWire.Report("Fin_ManusKilled", token, "Manus Sazon Killed in Action",
                        $"Manus Sazon, heir to House Sazon and former leader of the Rizian People's Party, was killed in action during {op}. He fell " +
                        "with much of the Brenas battalion he commanded. His body has been recovered and will be returned to Sallabes.");
                    break;
                case "line":
                    NewsWire.Report("Fin_ManusLine", token, "Sazon Battalion Holds the Line", Variables.GetBool(WarFlags.ZilleCityHeld)
                        ? "With the guns in Zille silent, the Brenas battalion under Manus Sazon has been posted to the armistice line north of Zille " +
                          "city. Its officers report nightly Wehzek patrols and loudspeaker broadcasts urging Rizian soldiers to go home. Mr. Sazon is " +
                          "said to have answered the broadcasts by ordering his men to sing louder."
                        : "When the offensive broke down, the Brenas battalion under Manus Sazon covered the retreat to the old border " +
                          "and came back in good order, one of the few units that did. Mr. Sazon is unhurt. His officers say he walked the last miles " +
                          "beside the stretchers and refused a car.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Manus fate news failed: {ex.Message}");
        }
    }

    /// <summary>Vanilla's Turn 10 rule declares a missing Manus presumed dead; its report is filed on Pales, so ours replaces it.</summary>
    private static void ReportPresumedDead()
    {
        if (Variables.GetString(VarFate) != "missing" || !Variables.GetBool("RiziaDLC.Manus_DiesInWar") || Variables.GetBool(VarPresumedReported))
        {
            return;
        }

        Variables.Set(VarPresumedReported, true);
        NewsWire.Report("Fin_ManusPresumedDead", WorldSystems.ZilleToken, "Manus Sazon Presumed Dead",
            "The General Staff has declared Manus Sazon, heir to House Sazon and former leader of the Rizian People's Party, missing in action " +
            "and presumed dead. No trace of him has been found since the fighting in Zille, and Wraclawitz still lists no Rizian officer by that " +
            "name among its prisoners.");
    }

    private static void OfferVina(int turn)
    {
        string fate = Variables.GetString(VarFate);
        if (fate is not ("decorated" or "wounded" or "killed" or "missing") || Variables.GetBool(VarVinaDone) ||
            Variables.GetInt(VarVinaOffered) >= turn || turn < Variables.GetInt(VarFateTurn) || turn > 10 || (GameState.CurrentStepNum ?? 0) < 2)
        {
            return;
        }

        foreach (string away in new[] { "RiziaDLC.Vina_Hostage", "RiziaDLC.Vina_Escapes", "RiziaDLC.Vina_EarlyExile", "RiziaDLC.Vina_TravelsAbroad",
                                         "RiziaDLC.Vina_FuneralHappened" })
        {
            if (Variables.GetBool(away))
            {
                return;
            }
        }

        string frag = $"ZilleWar.Late_ManusVina_T{turn}";
        if (GameState.StoryFragmentExistsInCurrentStep(frag))
        {
            return;
        }

        try
        {
            GameState.AddCustomConversation(VinaConv);
            if (GameState.AddCustomStoryFragment(new CustomConversationFragment(frag, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                "News from Zille", "The King's study, Palas Rezna", ConversationData.ConversationType.Personal, VinaConv)))
            {
                Variables.Set(VarVinaOffered, turn);
                Variables.Set(VarVinaDone, true);
            }
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding Vina's scene about Manus failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ vanilla texts that name Pales

    private static readonly Dictionary<string, string> OriginalJournal = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> OriginalDecision = new(StringComparer.Ordinal);

    private static void ApplyTexts(bool zm)
    {
        try
        {
            if (!zm)
            {
                WorldText.RestoreNews("Turn07_RH_ManusSentToWar");
                WorldText.RestoreNews("Turn07_V_ManusToWar");
                WorldText.RestoreNews("Floating_RH_ManusFuneral");
                WorldText.RestoreCodex("Characters_PartyLeaders_ManusSazon_Commander");
                WorldText.RestoreCodex("Characters_PartyLeaders_ManusSazon_DiedInWar");
                Journal("Turn07_Manus_SendToWar", null);
                DecisionDescription("Turn07_Decision_Manus_Gone_SentToWar", null);
                return;
            }

            WorldText.News("Turn07_RH_ManusSentToWar", "Manus Sazon Sent to Zille Front", NewsWire.P(
                "It was announced yesterday that Manus Sazon, heir to House Sazon, would be departing immediately for Zille to lead his province's " +
                "battalion in the war. In a statement, he said that no Sazon could stay home while Rizian soldiers fight for land his family lost " +
                "in 1927, and that he would serve the troops in any capacity he can.",
                "Despite his lack of battlefield experience, Mr. Sazon's departure has been met with a mixture of surprise, skepticism and cautious " +
                "optimism. In Brenas, where the loss of Zille has never been forgotten, supporters of House Sazon hope that his presence will " +
                "bring a renewed vigor to the levies at the front.",
                "No immediate replacement has been announced for his position in the House of Delegates."));
            WorldText.News("Turn07_V_ManusToWar", "Manus Sazon Sent to the Zille Meatgrinder", NewsWire.P(
                "Is Manus Sazon's sudden mobilization a way for the King to dispose of him? Many were surprised to hear that Manus is taking a " +
                "break from politics to serve in Zille, in a war that was declared without a single vote in the House he led.",
                "The Palace knows exactly which string it pulled. No Sazon could refuse to fight for Zille, and no opposition leader can lead " +
                "from a trench. It sounds convenient for the Crown: the possible death of its loudest critic, in a place where no one will ask " +
                "questions. No one should ever die in a war fought to settle a king's family business!"));
            WorldText.News("Floating_RH_ManusFuneral", "House Sazon Holds Funeral for War Hero Heir", NewsWire.P(
                "In a solemn ceremony befitting the dignity of Rizia's third noble family, House Sazon today laid to rest its valiant young heir, " +
                "Manus Sazon, who fell in the war for Zille. The streets of Sallabes were lined with citizens and dignitaries alike, paying their " +
                "respects to a hero whose life was cut short in the service of his country, on the soil his family lost a quarter of a century ago.",
                "His Majesty Romus Toras, visibly moved, offered his deepest condolences to the Sazon family, pledging that Manus's ultimate " +
                "sacrifice would not be in vain."));
            ReplaceCodex("Characters_PartyLeaders_ManusSazon_Commander",
                ("the Second Reclamation War against Pales", "the war to liberate Zille, the land his family lost in 1927"));
            ReplaceCodex("Characters_PartyLeaders_ManusSazon_DiedInWar",
                ("during the Invasion of Pales", "in the war for Zille"));
            Journal("Turn07_Manus_SendToWar", "I sent Manus Sazon to the front lines in Zille.");
            DecisionDescription("Turn07_Decision_Manus_Gone_SentToWar", d => d
                .Replace("to the front lines in the war against the Grand Duchy of Pales", "to the front lines in the war for Zille")
                .Replace("in the war against the Grand Duchy of Pales", "in the war for Zille"));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Manus texts failed: {ex.Message}");
        }
    }

    private static void ReplaceCodex(string name, params (string from, string to)[] swaps)
    {
        string text = WorldText.OriginalText("codex", name);
        if (text == null)
        {
            return;
        }

        foreach ((string from, string to) in swaps)
        {
            text = text.Replace(from, to);
        }

        WorldText.Codex(name, null, text);
    }

    private static void Journal(string name, string text)
    {
        foreach (var list in new[] { EntityDataManager.journalEntriesData, EntityDataManager.JournalEntriesData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (JournalEntryData j in list)
            {
                JournalEntryProperties p = j?.JournalEntryProperties;
                if (p == null || j.NameInDatabase != name)
                {
                    continue;
                }

                if (!OriginalJournal.ContainsKey(name))
                {
                    OriginalJournal[name] = p.Description;
                }

                string wanted = text ?? OriginalJournal[name];
                if (p.Description != wanted)
                {
                    p.Description = wanted;
                }
            }
        }
    }

    private static void DecisionDescription(string name, Func<string, string> rewrite)
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
                if (p == null || d.NameInDatabase != name)
                {
                    continue;
                }

                if (!OriginalDecision.ContainsKey(name))
                {
                    OriginalDecision[name] = p.Description ?? string.Empty;
                }

                string wanted = rewrite != null ? rewrite(OriginalDecision[name]) : OriginalDecision[name];
                if (p.Description != wanted)
                {
                    p.Description = wanted;
                }
            }
        }
    }
}
