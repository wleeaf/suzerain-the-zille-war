using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Audit of the vanilla story of Turn 6 against the Zille war (see <see cref="Audit"/>).
/// Turn 6 is the hinge: the War Council and Phase III play in step 1, so from step 2 on the war is over (liberated,
/// divided or lost). The earlier sweeps wrote most Turn 6 lines for a war still being fought and for a lost war; this pass
/// gives every post-war scene its liberated / divided / lost reading:
/// - shared Lua and C# state helpers (<see cref="Lib"/>, <see cref="Part"/>, <see cref="Lost"/>, <see cref="ByP"/>) used by the
///   other Turn 6 files, which now tell a divided Zille (Zille city ours, the rest behind an armistice line) from a lost one;
/// - the council meeting (c361): "the path of peace", Vina's "start a war", Lucita's "pay any price to avoid conflict" and the
///   northern divisions after the war;
/// - the night council (c359), the Pales meeting (c358), the border investigation (c371), the Terador date (c375), the deal
///   signing (c369), the general staff briefing (c357), the family before the balcony (c373) and the speech (c372): peacetime
///   lines ("our peaceful nation", "the path of peaceful conflict resolution", "I hoped it would never come to this",
///   "It's been some time" to the general who ran the Zille councils) get post-war versions;
/// - the royal yacht's extra costs (no Wehlen timber in a war; no restoration at all once the yacht became a hospital ship),
///   the Vendonesam troop request and the trade-war windfall after the war, and the Herald's football report.
/// </summary>
internal static class AuditT6
{
    // ------------------------------------------------------------------ Lua state (Turn 6 and later)

    public static readonly string On = WarFlags.LActive;
    public static readonly string Over = WarFlags.L(WarFlags.Over);
    public static readonly string Lib = $"{WarFlags.L(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.Active)}";
    /// <summary>Divided: the war is over, Zille city is Rizian, the rest of the province is Wehzek behind an armistice line.</summary>
    public static readonly string Part = $"{Over} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.L(WarFlags.ZilleCityHeld)}";
    /// <summary>Lost: the war is over and Zille city is still Wehzek.</summary>
    public static readonly string Lost = $"{Over} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.ZilleCityHeld)}";
    /// <summary>Over without liberation: divided or lost.</summary>
    public static readonly string NotLib = $"{Over} and {WarFlags.LNot(WarFlags.Liberated)}";
    private static readonly string AureminaHeld = WarFlags.L(ZilleOperation.CapturedVar("Auremina"));

    // ------------------------------------------------------------------ C# state

    /// <summary>Zille city is in Rizian hands now (after the war: divided or liberated).</summary>
    public static bool CityHeldNow => Variables.GetBool(WarFlags.Declared) && Variables.GetBool(ZilleOperation.CapturedVar("Zille"));

    /// <summary>Picks the text for a latched press state: fought now, won, divided (Zille city ours) or lost.</summary>
    public static string ByP(string state, string active, string won, string part, string lost) =>
        state == StActive ? active : state == StWon ? won : CityHeldNow ? part : lost;

    private static bool IsOverNow => IsDeclared && !WarFlow.AtWar;

    // ------------------------------------------------------------------ speakers and guards

    private const string Hugo = "Hugo Toras";
    private const string Lucita = "Lucita Azaro";
    private const string Laurento = "Laurento Esquibel";
    private const string Vina = "Vina Toras";
    private const string Titus = "Titus Gordion";
    private const string Axel = "Axel Reinhart";
    private const string Carlos = "Carlos Robles Azaro";
    private const string Estela = "Estela Toras";
    private const string Rusty = "Russello Montoro";

    private const string AskedWehlen = "ZilleWar.A6_Asked_WehlenSuspect";
    private const string YachtHospital = "ZilleWar.SE_Fam_YachtHospital";   // SweepEarlyT5Family.VarYachtHospital

    public static void Register()
    {
        Variables.Register(AskedWehlen);
        Vanilla.Expose(YachtHospital);

        CouncilMeeting();
        NightCouncil();
        BorderInvestigation();
        Terador();
        DealSigning();
        GeneralStaff();
        Cemetery();
        BalconyIntro();
        Balcony();
        Football();
        Decisions();
    }

    public static void OnStep()
    {
        // The yacht gate reads this flag in expresso: never leave it nil.
        Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{YachtHospital}\"] == nil then Variable[\"{YachtHospital}\"] = false end");
    }

    // ================================================================== c361 Council meeting (step 3, after the war)

    private static void CouncilMeeting()
    {
        const string conv = "Rizia/Turn 6/A_CouncilMeeting";
        var t = new Talk(conv, "ZilleWar.A6.Council.");

        // [143] "Thank you, Mr. Esquibel, for helping us stay on the path of peace."
        t.Variant("PeaceOver", 143, new[] { 137 }, Over, Romus,
            "\"Thank you, Mr. Esquibel. After Zille, a bargain struck at a table makes a welcome change.\"", new[] { "#848" });

        // [690] Vina: "And I also know it was you who singlehandedly decided to start a war, Father."
        t.Variant("SecondWarOver", 690, new[] { 689 }, Over, Vina,
            "\"And I also know it was you who singlehandedly decided to start a second war, Father. Brenas has barely buried the dead " +
            "of the first.\"", new[] { "#698" });

        // [862] Lucita: "...a signal to our neighbors that Rizia is willing to pay any price to avoid conflict."
        t.Variant("AnyPrice", 862, new[] { 861 }, WarFlags.LDeclared, Lucita,
            "\"If I may speak honestly, I was against this purchase from the start. We fought Wehlen for every hill in Zille, and now we " +
            "pay Pales for gas that is half ours. Our neighbors will not know which Rizia to expect.\"", new[] { "#863" });

        // [747] "I will signal the general staff immediately." The army has only just come out of Zille.
        string[] after747 = { "#748", "#749", "#750" };
        t.Line("NorthLib", Lucita,
            "\"The northern divisions can come south, Your Majesty. Zille's garrison will be thinner for it, but Smolak is in no state to " +
            "test it this year.\"", after747, condition: Lib, hooks: From(747));
        t.Line("NorthPart", Lucita,
            "\"Most of the northern army stays on the armistice line, Your Majesty. Smolak's divisions are still across it, and I will not " +
            "leave Zille city to their good manners.\"", after747, condition: Part, hooks: From(747));
        t.Line("NorthLost", Lucita,
            "\"Though our army has only just come out of Zille, Your Majesty. The northern divisions are still rebuilding. I will need " +
            "time, or I will need money.\"", after747, condition: Lost, hooks: From(747));
        t.Register();
    }

    // ================================================================== c359 Night council, c358 meeting with the Duke

    private static void NightCouncil()
    {
        var t = new Talk("Rizia/Turn 6/SnO_Incident", "ZilleWar.A6.Night.");

        // [19] -> [9]/[10]/[11]: a summons at night, weeks after an armistice.
        t.Choice("TitusPart", "\"Titus? Is it the north? Has Wehlen broken the armistice?\"", new[] { "TitusA" },
            condition: NotLib, hooks: From(19));
        t.Choice("TitusLib", "\"Titus? Is it Zille? Has Wehlen tried to take it back?\"", new[] { "TitusA" },
            condition: Lib, hooks: From(19));
        t.Line("TitusA", Titus, "\"I don't believe so, Your Majesty. They said the south. That's all they would tell me.\"", new[] { "#142" });

        // [23] Hugo: "I'm afraid we'll be facing our biggest crisis yet."
        t.Variant("Crisis", 23, new[] { 17, 22 }, WarFlags.LDeclared, Hugo,
            "\"I'm afraid we have another crisis on our hands, and not a small one. I must stress -- this is the time everybody needs a " +
            "calm and resilient leadership.\"", new[] { "#25" });

        // The suspects, after the war.
        t.Choice("Wehlen", "\"What if it's Wehlen? A second front is exactly what Smolak would want.\"", new[] { "WehlenLib", "WehlenNotLib" },
            condition: $"{Over} and {NotSet(AskedWehlen)}", script: Set(AskedWehlen, "true"), hooks: From(175));
        t.Line("WehlenLib", Lucita,
            "\"It would be like him. He has just lost Zille, and he would dearly like to see us bleed somewhere else.\"",
            new[] { "Wehlen2" }, condition: Lib);
        t.Line("WehlenNotLib", Lucita,
            "\"It would be like him. His divisions are still across the armistice line, and nothing would please him more than to see " +
            "ours marching south.\"", new[] { "Wehlen2" }, condition: NotLib);
        t.Line("Wehlen2", Hugo,
            "\"Wehzek soldiers at the River Alca? I doubt it. But Smolak will toast whoever did this, you may be sure of that.\"",
            new[] { "Wehlen3" });
        t.Line("Wehlen3", Laurento,
            "\"Or Pales is counting on us being too tired after Zille to answer. Either way, Your Majesty, we are being measured.\"",
            new[] { "#175" });

        // [440]-[442] Laurento: "Is this not reckless?" -> [418]
        t.Line("RecklessLib", Laurento,
            "\"We have barely finished one war, Your Majesty. The ink on the Zille proclamations is not yet dry.\"",
            new[] { "#418" }, condition: Lib, hooks: From(442));
        t.Line("RecklessNotLib", Laurento,
            "\"We have barely signed an armistice in the north, Your Majesty. Wehzek guns still face ours across it.\"",
            new[] { "#418" }, condition: NotLib, hooks: From(442));
        t.Register();
    }

    // ================================================================== c371 The border investigation

    private static void BorderInvestigation()
    {
        var t = new Talk("Rizia/Turn 6/A_BorderInvestigationResults", "ZilleWar.A6.Arrest.");

        // [36] "I always did suspect Duchess Azaro of being disingenuous in her pacifism." She sat on the Zille war councils.
        t.Variant("Pacifism", 36, new[] { 33 }, WarFlags.LDeclared, Romus,
            "\"She planned one war for me already. Perhaps she wanted another.\"", new[] { "#40" });

        // [152] Reinhart: "We defended ourselves against Rizia once. We are ready to do so again." -> [153]
        t.Line("ReinhartLib", Axel, "\"You took Zille from Smolak, I know. We are not Smolak.\"", new[] { "#153" }, condition: Lib, hooks: From(152));
        t.Line("ReinhartPart", Axel, "\"Your army has only just come back from Zille, Your Majesty. Smolak still holds half of it.\"",
            new[] { "#153" }, condition: Part, hooks: From(152));
        t.Line("ReinhartLost", Axel, "\"Your army could not even take Zille from Smolak, Your Majesty.\"",
            new[] { "#153" }, condition: Lost, hooks: From(152));
        t.Register();
    }

    // ================================================================== c375 The date in Terador

    private static void Terador()
    {
        var t = new Talk("Rizia/Turn 6/P_LucitaAzaroUpdate", "ZilleWar.A6.Terador.");

        // [116] "I can't believe we came so close to war. It would've been a terrible mistake."
        t.Variant("CloseOver", 116, new[] { 94 }, Over, Romus,
            "\"I can't believe we came so close to another war. Zille was enough for one year.\"", new[] { "#117" });

        // [279] "What's love without a little frisson?"
        t.Variant("FrissonOver", 279, new[] { 278 }, Over, Lucita,
            "\"Says the man who has just fought a war over Zille. Still, what's love without a little frisson?\"", new[] { "#284" });

        // [674] "Peace between Rizia and Pales, and not a single drop of blood shed. Who would have thought."
        t.Variant("PeaceOver", 674, new[] { 541 }, Over, Lucita,
            "\"Peace with Pales, and not a single drop of blood shed in the south. After Zille, I'll take it.\"",
            new[] { "#690", "#696", "#693", "#682" });

        // [808] the toast menu.
        t.Choice("Toast", "\"To the men who didn't come back from Zille.\"", new[] { "ToastA" }, condition: Over, hooks: From(808));
        t.Line("ToastA", Narrator, "She looked me in the eye as our glasses clinked.", new[] { "ToastB" });
        t.Line("ToastB", Lucita, "\"To them. And to the ones who did.\"", new[] { "#815" });
        t.Register();
    }

    // ================================================================== c369 The Aureus deal signing

    private static void DealSigning()
    {
        var t = new Talk("Rizia/Turn 6/SnO_PalesDealSigning", "ZilleWar.A6.Signing.");

        // [270] "...we live in a volatile region... Let Rizia and Pales serve as an example to all those around us."
        t.Variant("Volatile", 270, new[] { 268 }, Over, Romus,
            "\"I know how Rizia is spoken of in Konstantium since Zille. But let Rizia and Pales show the region that where a fair " +
            "bargain can be struck, it need not be struck with guns.\"", new[] { "#273" });

        // [271] "It is my intention that Rizia continue on the path of peaceful conflict resolution."
        t.Variant("Path", 271, new[] { 268 }, Over, Romus,
            "\"What had to be settled by arms in Zille has been settled. Everything else, Rizia means to settle at the table. We will " +
            "remain one of the most reliable trading partners in Merkopa.\"", new[] { "#273" });

        // [534] "My kingdom believes that our partnerships with our neighbors are our greatest asset."
        t.Variant("Partners", 534, new[] { 533 }, WarFlags.LDeclared, Romus,
            "\"Whatever our quarrel with Wehlen, my kingdom believes that its partnerships with its other neighbors are its greatest " +
            "asset. We shall continue to foster them, even at the expense of our own wealth.\"", new[] { "#543" });
        t.Register();
    }

    // ================================================================== c357 The general staff briefing

    private static void GeneralStaff()
    {
        var t = new Talk("Rizia/Turn 6/SnO_WarEruptionBriefing", "ZilleWar.A6.Staff.");

        // [512] "The last time I had seen the room this full was at the start of the Reclamation War."
        t.Variant("RoomOver", 512, new[] { 510 }, Over, Narrator,
            "The last time I had seen the room this full was at the war council for Zille, and before that, at the start of the " +
            "Reclamation War. Every single top-ranking general and admiral in Rizia was in attendance.", new[] { "#102" });

        // [544] "General Azaro. It's been some time." -> [553] "Too long, Your Majesty." Carlos ran the Zille councils with the King.
        t.Variant("Again", 544, new[] { 548, 549, 550 }, WarFlags.LDeclared, Romus, "\"General. We meet over a map again.\"", new[] { "AgainA" });
        t.Line("AgainA", Carlos, "\"So we do, Your Majesty. Only the map has changed.\"", new[] { "#127" });

        // After Lucita's "our forces are already mobilized" ([131]): the northern army after a divided or lost war.
        t.Line("NorthPart", Carlos,
            "\"One caveat, Your Majesty. The northern army group stays on the armistice line in Zille. Smolak's divisions are still across " +
            "it, and I will not strip it bare for Pales.\"", new[] { "#261" }, condition: Part, hooks: From(131));
        t.Line("NorthLost", Carlos,
            "\"One caveat, Your Majesty. The northern divisions are still rebuilding after Zille, and they stay on the armistice line. " +
            "Smolak would like nothing better than an empty border.\"", new[] { "#261" }, condition: Lost, hooks: From(131));
        t.Register();
    }

    // ================================================================== c370 The cemetery

    private static void Cemetery()
    {
        var t = new Talk("Rizia/Turn 6/P_Cemetary", "ZilleWar.A6.Estela.");

        // [255] and [623] Estela: "your attention should be on the rightful restoration of Zille, not Pales!"
        t.Variant("Zille255Lib", 255, new[] { 622 }, Lib, Estela, "\"You have Zille back, Romus! Isn't one war enough for a lifetime?\"", new[] { "#259" });
        t.Variant("Zille255Part", 255, new[] { 622 }, Part, Estela, "\"Half of Zille is still behind Smolak's line, and you want Pales now?\"", new[] { "#259" });
        t.Variant("Zille255Lost", 255, new[] { 622 }, Lost, Estela, "\"You could not take Zille, Romus, and now you want Pales?\"", new[] { "#259" });
        t.Variant("Zille623Lib", 623, new[] { 98 }, Lib, Estela,
            "\"Relying solely on divine intervention is misguided. And you have Zille back already. Is one war not enough for you?\"", new[] { "#259" });
        t.Variant("Zille623Part", 623, new[] { 98 }, Part, Estela,
            "\"Relying solely on divine intervention is misguided. Speaking of which, half of Zille is still in Smolak's hands, and you are " +
            "looking at Pales.\"", new[] { "#259" });
        t.Variant("Zille623Lost", 623, new[] { 98 }, Lost, Estela,
            "\"Relying solely on divine intervention is misguided. Speaking of which, Zille is still Wehzek, and you are looking at Pales.\"",
            new[] { "#259" });

        // The vows at Valero's tomb (EarlyT6 has the war being fought and the liberation).
        t.Variant("Part182", 182, new[] { 82 }, Part, Romus,
            "\"Zille city is ours again. I will not rest until the rest of his mistake is undone.\"", new[] { "#502" });
        t.Variant("Lost182", 182, new[] { 82 }, Lost, Romus,
            "\"I went to war to undo his mistake of giving Zille away so freely. I am not finished.\"", new[] { "#502" });
        t.Variant("Part261", 261, new[] { 266 }, Part, Romus,
            "\"Zille city flies our flag again. That is how I honor his memory, and it is not the end of it.\"", new[] { "#271" });
        t.Variant("Lost261", 261, new[] { 266 }, Lost, Romus,
            "\"I fought for Zille and did not win it back. One day I will finish what he gave away.\"", new[] { "#271" });
        t.Variant("Part262", 262, new[] { 265 }, Part, Romus,
            "\"Zille city flies our flag again, God be thanked. The rest will follow.\"", new[] { "#273" });
        t.Variant("Lost262", 262, new[] { 265 }, Lost, Romus,
            "\"I fought for Zille, and God did not grant it to us. Not yet.\"", new[] { "#273" });
        t.Variant("Part263", 263, new[] { 269 }, Part, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. I went to war to take it back, and half of it is ours again.\"",
            new[] { "#272" });
        t.Variant("Lost263", 263, new[] { 269 }, Lost, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. I went to war to take it back, and I failed. That is not the end of it.\"",
            new[] { "#272" });
        t.Variant("Part270", 270, new[] { 268 }, Part, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. I took Zille city back by force, and I would do it again.\"",
            new[] { "#274" });
        t.Variant("Lost270", 270, new[] { 268 }, Lost, Romus,
            "\"Surrendering Zille was one of Rizia's greatest tragedies. I went to war to take it back. I will find another way.\"",
            new[] { "#274" });
        t.Register();
    }

    // ================================================================== c373 Before the balcony speech

    private static void BalconyIntro()
    {
        var t = new Talk("Rizia/Turn 6/Intro Section", "ZilleWar.A6.Intro.");

        // [4] Hugo: "War is a crucible. If you can face it head-on and survive, you will be unbreakable."
        t.Variant("Crucible", 4, new[] { 1 }, Over, Hugo,
            "\"War is a crucible, Romus. You have already passed through one in Zille. Come through this one as well, and no one will " +
            "ever question House Toras again.\"", new[] { "#5", "#6", "#31", "#42", "#9" });

        // [9] "I hoped it would never come to this." The King declared the first war himself.
        t.Variant("Hoped", 9, new[] { 4 }, WarFlags.LDeclared, Romus,
            "\"I hoped one war would be enough. But if we must weather the storm again, I shall do my best to steer Rizia through it.\"",
            new[] { "#54" });

        // [21] Vina: "It was your choice to go to war, Father. You've got to bear the burden."
        t.Variant("Burden", 21, new[] { 23 }, Over, Vina,
            "\"It was your choice to go to war again, Father. You've got to bear the burden.\"", new[] { "#74" });

        // [103] Hugo: "Look at how prepared we are, Madam Queen Mother. Our armies will not falter."
        t.Variant("Prepared", 103, new[] { 107 }, Lost, Hugo,
            "\"Look at how prepared we are, Madam Queen Mother. Our armies learned hard lessons in Zille. They will not falter twice.\"",
            new[] { "#110" });

        // [116] Vina: "I just hope that less blood will be spilled than during the Reclamation War."
        t.Variant("Blood", 116, new[] { 111 }, Over, Vina, "\"I just hope that less blood will be spilled than in Zille.\"", new[] { "#119" });
        t.Register();
    }

    // ================================================================== c372 The balcony speech

    private static void Balcony()
    {
        var t = new Talk("Rizia/Turn 6/A_PalaceBalconySpeech", "ZilleWar.A6.Balcony.");

        // [88] "Our peaceful nation now faces war, imposed by our southern neighbor."
        t.Variant("PeacefulLib", 88, new[] { 90, 91, 92, 93, 94 }, Lib, Romus,
            "\"Our nation, which has only just brought Zille home, now faces war again, imposed by our southern neighbor.\"", new[] { "#148" });
        t.Variant("PeacefulNotLib", 88, new[] { 90, 91, 92, 93, 94 }, NotLib, Romus,
            "\"Our nation, still mourning the soldiers it lost in Zille, now faces war again, imposed by our southern neighbor.\"", new[] { "#148" });

        // [27] "They tried to attack me in Zille but failed. Now, a new threat rises from the south." (EarlyT6: war fought, liberated)
        t.Variant("ZillePart", 27, new[] { 160 }, Part, Romus,
            "\"They tried to kill me in Zille and failed. Today Zille city flies our flag again. Now a new threat rises from the south, " +
            "and we will meet it the same way.\"", new[] { "#453" });
        t.Register();
    }

    // ================================================================== c376 The charity match (step 2, after the war)

    private static void Football()
    {
        const string conv = "Rizia/Turn 6/A_Rusty_FootballGame";
        var t = new Talk(conv, "ZilleWar.A6.Football.");

        // [1098] "A cause like a war? They can't protest if they're called to the front." -> [1099] "Yeah, but neither can they work."
        t.Variant("Front", 1098, new[] { 1087, 1088 }, Over, Romus,
            "\"A cause like a war? We've only just finished one in Zille.\"", new[] { "FrontA" });
        t.Line("FrontA", Rusty,
            "\"And half my night shift in Argno came home from it wanting a raise. Funny how that works.\"", new[] { "#287" });

        // Rusty's offer for the Auremina mines, once the gold hills are Rizian (liberated, or divided with Auremina taken).
        string unasked = $"(Variable[\"{SweepEarlyT6Home.VarRustyZilleGold}\"] == nil or Variable[\"{SweepEarlyT6Home.VarRustyZilleGold}\"] == \"\")";
        t.Line("Gold1", Rusty, "\"Speaking of deep pockets. Our boys are holding Auremina now, and I hear that war of yours didn't come cheap.\"",
            new[] { "Gold2" }, condition: $"{Over} and {AureminaHeld} and {unasked}", hooks: From(1100, 1120, 1124, 1073));
        t.Line("Gold2", Rusty,
            "\"Those hills are what brought RRG north in the first place, before the Wehzeks took over the pits. So here's my offer. RRG " +
            "puts six billion guilders into the Treasury today, toward the war's bill, and RRG gets first claim on the Auremina concession.\"",
            new[] { "Pledge", "Refuse", "Later" });
        t.Choice("Pledge", "\"Rizian Royal Gold will have first claim on Auremina. Send the money.\"", new[] { "PledgeA" }, cost: "+2 Budget",
            script: Set(SweepEarlyT6Home.VarRustyZilleGold, "\"pledged\"") + Inc("RiziaDLC.Resources_Budget", 2));
        t.Line("PledgeA", Rusty,
            "\"Pleasure doing business, Your Majesty. My accountants will be at the Treasury by Monday. Ms. Werner's going to love that.\"",
            new[] { "#651" });
        t.Choice("Refuse", "\"Our soldiers did not die in those hills for your shareholders, Rusty.\"", new[] { "RefuseA" },
            script: Set(SweepEarlyT6Home.VarRustyZilleGold, "\"refused\""));
        t.Line("RefuseA", Rusty,
            "\"Course not. They died for Rizia. I just figured Rizia could use somebody to pay for the pumps.\"", new[] { "#651" });
        t.Choice("Later", "\"Ask me again once the Treasury has counted what the war cost.\"", new[] { "LaterA" },
            script: Set(SweepEarlyT6Home.VarRustyZilleGold, "\"later\""));
        t.Line("LaterA", Rusty, "\"I'll hold you to that. I'm a patient man. Not a very patient man, but a patient one.\"", new[] { "#651" });

        // [985] "But the results didn't count." The collection boxes by the gates.
        t.Line("Boxes", Narrator,
            "On the way out, I passed the collection boxes by the gates. Beside the ones for polio research, someone had set out a second " +
            "row with hand-lettered signs: For the families of our fallen in Zille. They were fuller.",
            new[] { "#1077" }, condition: Over, hooks: From(985));
        t.Register();

        // The Herald's match report learns about the minute's silence (SweepEarlyT6Home adds it to the ceremony).
        foreach (string name in new[] { "Turn06_RH_Football_Default", "Turn06_RH_Football_KingGoal", "Turn06_RH_Football_RustyKickoff" })
        {
            AppendNews(name, s => By(s,
                "Before kickoff, the whole stadium stood for a minute's silence for the soldiers fighting in Zille, and collection boxes " +
                "for their families were set out at every gate.",
                "Before kickoff, the whole stadium stood for a minute's silence for the soldiers who fell bringing Zille home, and " +
                "collection boxes for their families were set out at every gate.",
                "Before kickoff, the whole stadium stood for a minute's silence for the soldiers who fell in Zille, and collection boxes " +
                "for their families were set out at every gate."));
        }
    }

    // ================================================================== decisions

    private static void Decisions()
    {
        // The royal yacht: no Wehlen timber in a war, and no restoration at all once the yacht serves as a hospital ship.
        Vanilla.Gate("Turn06_Decision_RoyalYachtRestorationExtraCosts", $"{WarFlags.XPeace} || {WarFlags.X(YachtHospital, false)}");
        Decision("Turn06_Decision_RoyalYachtRestorationExtraCosts", _ =>
            "The restoration of the monarchy's grand yacht, a symbol of national pride since 1901, encounters unforeseen expenses. The " +
            "Wehzek mills that supplied her original timber have been closed to Rizian buyers since the war, so the wood must be bought " +
            "elsewhere at a premium or found at home. This choice has budget implications and will influence the future of this " +
            "historic vessel.");

        Decision("Turn06_Decision_VendonesamTroopRequest", o => !IsOverNow ? null : o.TrimEnd() + "\n\n" + (ZilleCampaign.Liberated
            ? "The general staff has objected in writing. With part of the northern army still garrisoning Zille, it says, a division sent " +
              "to Vendonesam is a division the kingdom can ill spare."
            : "The general staff has objected in writing. With the northern army still holding the armistice line in Zille, it says, a " +
              "division sent to Vendonesam is a division taken from the one border that is not yet quiet."));

        Decision("Turn06_Decision_SuperpowerTradeWar_Penalty", o => !IsOverNow ? null : BeforeTag(o,
            "For a treasury still paying for the war in Zille, the windfall could hardly have come at a better time."));
    }
}
