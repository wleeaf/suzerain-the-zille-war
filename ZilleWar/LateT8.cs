using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.LK;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Turn 8 for a Rizia that went to war over Zille:
/// - the airship talks with Smolak (c424) are replaced by one meeting per outcome: a back channel in a railway car,
///   a prisoner exchange on the Kreleck ferry, an armistice commission, or Smolak's terms in Wraclawitz;
/// - the security brief (c409) skips the peacetime Zille contingency plan for a war brief: the front, the Lespia
///   evidence, Héctor Ramiro in the Lespian consulate of a Zille city we hold, the Auremina gold, the occupation;
/// - the Alliance of Nations session (c401) gets its Zille item back as Smolak's resolution against the war, with a
///   roll-call vote; Smolak can open the sanctions debate (c402); Van Hoorten's Heljiland plea (c403) becomes a mirror;
/// - lines that assume a treaty or a handover are swapped in Alvarez's visit (c423), the council (c589), Rusty's call
///   (c416), the House of Delegates (c422), Hugo (c413, c593), Lucita (c414) and the wedding (c417).
/// </summary>
internal static class LateT8
{
    private const string AN = "Rizia/Turn 8/FPnT_ANSession";
    private const string Sanctions = "Rizia/Turn 8/Sanctions";
    private const string Heljiland = "Rizia/Turn 8/Heljiland Dispute";
    private const string Brief = "Rizia/Turn 8/SnO_SecurityBriefAndZille";
    private const string AlvarezVisit = "Rizia/Turn 8/FPnT_AlvarezVisit";
    private const string Council = "Rizia/Turn 8/FOREIGNAFFAIRS/FOREIGNAFFAIRS Conversation";
    private const string Rusty = "Rizia/Turn 8/RustyCall";
    private const string Hod = "Rizia/Turn 8/A_HouseOfDelegates3";
    private const string Wedding = "Rizia/Turn 8/A_RoyalWedding";
    private const string HugoLeaves = "Rizia/Turn 8/A_HugoLeaves";
    private const string HugoConfronts = "Rizia/Turn 8/HugoConfronts/HugoConfronts Conversation";
    private const string NightBefore = "Rizia/Turn 8/P_NightBeforeWedding";
    private const string SmolakConv = "ZilleWar/Late/SmolakT8";

    private const string Smolak = "Wiktor Smolak (R)";
    private const string Nines = "Ephraim Nines (R)";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Laurento = "Laurento Esquibel";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Hugo = "Hugo Toras";
    private const string Narr = Talk.Narrator;
    private const string R = Talk.Romus;

    public const string VarAgnolia = "ZilleWar.Late_AgnoliaSympathy";
    public const string VarLespiaAccused = "ZilleWar.Late_LespiaAccused";
    public const string VarRamiro = "ZilleWar.Late_Ramiro";                 // "seized" | "released" | "sealed"
    public const string VarGold = "ZilleWar.Late_AureminaGold";             // "rrg" | "crown" | "sazon"
    public const string VarOccupation = "ZilleWar.Late_Occupation";         // "martial" | "amnesty" | "generals"
    public const string VarANResult = "ZilleWar.Late_ANResult";             // "passed" | "failed"
    public const string VarPrisoners = "ZilleWar.Late_Prisoners";           // "released" | "ransomed" | "held" | "exchanged"
    public const string VarTerms = "ZilleWar.Late_SmolakTerms";             // "paid" | "refused" | "line" | "rebuild"
    public const string VarEstelaHome = "ZilleWar.Late_EstelaHomeTaken";
    public const string VarSmolakT8 = "ZilleWar.Late_SmolakT8Offered";
    private const string VarAsked = "ZilleWar.Late_Asked";
    private const string VarVote = "ZilleWar.Late_ANVote";

    public static void Register()
    {
        foreach (string v in new[] { VarAgnolia, VarLespiaAccused, VarRamiro, VarGold, VarOccupation, VarANResult, VarPrisoners, VarTerms, VarEstelaHome, VarSmolakT8 })
        {
            Variables.Register(v);
        }

        ConversationRegistry.RegisterConversation(SmolakConv);
        RegisterAnSession();
        RegisterSanctions();
        RegisterHeljiland();
        RegisterBrief();
        RegisterAlvarez();
        RegisterCouncil();
        RegisterRusty();
        RegisterHod();
        RegisterSmallScenes();
        RegisterSmolak();
    }

    public static void OnStep()
    {
        if (LateState.Turn != 8 || LateState.Step < 11 || !LateState.Declared || Variables.GetBool(VarSmolakT8))
        {
            return;
        }

        string frag = "ZilleWar.Late_SmolakT8";
        if (GameState.StoryFragmentExistsInCurrentStep(frag))
        {
            return;
        }

        (string title, string sub) = WarFlow.AtWar ? ("A Line to Wraclawitz", "Railway siding, Mave Nortus")
            : LateState.Liberated ? ("The Kreleck Ferry", "Strait of Kreleck")
            : LateState.Partial ? ("The Armistice Commission", "Schoolhouse north of Mave Nortus")
            : ("Summoned to Wraclawitz", "Presidential Palace, Wraclawitz");
        try
        {
            GameState.AddCustomConversation(SmolakConv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(frag, SuzerainStoryPackInfo.Rizia, "Rizia_Country_Wehlen", title, sub,
                Il2Cpp.ConversationData.ConversationType.ForeignPolicyAndTrade, SmolakConv));
            Variables.Set(VarSmolakT8, true);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the Turn 8 Smolak meeting failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ c401 the Alliance of Nations session

    private static string Rel(string country, params string[] states) =>
        "(" + string.Join(" or ", states.Select(s => $"Variable[\"RiziaDLC.CountryRelation_{country}_{s}\"] == true")) + ")";

    private static string Vote(string who) => $"ZilleWar.Late_V_{who}";

    private static void RegisterAnSession()
    {
        var t = new Talk(AN, "ZilleWar.Late.AN.");

        // the arrival
        Swap(t, AN, "Foyer", 9, new[] { 7 }, Narr, new S(
            Ongoing: "I looked around the foyer. The last time I stood in this building, I had declared war from its podium. The delegations remembered. Several of them found something fascinating to study on the ceiling as we passed.",
            Lib: "I looked around the foyer. Of all the leaders gathered here, none had made a louder entrance into the news this year than I had. Half the room wanted to shake my hand. The other half wanted to be photographed refusing to.",
            Part: "I looked around the foyer. The last time I stood in this building, I had declared war from its podium. Now the war had stopped where it stood, and nobody here seemed sure what to call what was left.",
            Lost: "I looked around the foyer. The last time I stood in this building, I had declared war from its podium. The Wehzek delegation was laughing about something by the coat check. I did not ask what."),
            new[] { "#56" });

        // the Lespia evidence, before the session
        Swap(t, AN, "Evidence98", 98, new[] { 100 }, Laurento, S.All(
            "\"If this comes to light, the Alliance may yet call our war what it was. A reckoning that Lespia arranged.\""),
            new[] { "#104", "#106", "Justify" });
        t.Choice("Justify", "\"Then it justifies the war. Let them hear it.\"", new[] { "#108" }, condition: War);
        Swap(t, AN, "Evidence102", 102, new[] { 101 }, Laurento, new S(Lib:
            "\"Not that we need it to keep Zille. But it would make President Smolak's speech today a great deal harder to deliver.\""),
            new[] { "#104", "#106", "#105" });
        Swap(t, AN, "Evidence120", 120, new[] { 122 }, R, S.All(
            "\"We can use it to stop the sanctions and silence Smolak at the same time. Two victories for the price of one.\""), new[] { "#124" });
        Swap(t, AN, "Speech169", 169, new[] { 168 }, R, new S(Lib:
            "\"Lespia lit the fuse in Zille. Wehlen used the smoke to keep what was never theirs. Rizia has finished what both of them started.\""),
            new[] { "#186" }, script: Set(VarLespiaAccused, "true"));
        Swap(t, AN, "Speech172", 172, new[] { 167 }, R, S.NotLib(
            "\"Lespia's bomb made this war inevitable. Every grave in Zille, Rizian and Wehzek alike, belongs on Patricio Alvarez's ledger.\""),
            new[] { "#186" }, script: Set(VarLespiaAccused, "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -1));

        // the agenda always includes Zille in a war game
        t.Logic("Agenda1", new[] { "#139" }, condition: War, hooks: Talk.From(136));
        t.Logic("Agenda2", new[] { "#142" }, condition: War, hooks: Talk.From(143));

        // the Zille item: replaces "Zille Territory Issue" (c404)
        string conv = "AddConversant(\"Wiktor Smolak (R)\"); ";
        t.Logic("Item", new[] { "Open" }, condition: War, hooks: Talk.From(29),
            script: conv + Set(VarAsked, "false") + "PlayLoopedMusic(\"R_Impasse\",\"R_Impasse_Loop\");");
        t.Line("Open", Nines, "\"We now turn to the last item on today's agenda. The situation in the Zille region. The Republic of Wehlen has the floor.\"",
            new[] { "On1", "Lib1", "Part1", "Lost1" });

        // Smolak's speech, one per outcome
        t.Line("On1", Narr, "President Smolak was at the podium before Nines had finished. He had brought no notes.", new[] { "On2" }, condition: Ongoing);
        t.Line("On2", Smolak, "\"Dear fellow leaders! Last year, in this very hall, the King of Rizia stood where I am standing and declared war. On a stage built for peace!\"", new[] { "On3" });
        t.Line("On3", Smolak, "\"Since that day, Wehzek boys have been dying in Zille. A province Wehlen protected for twenty-five years, while Rizia could not even protect its own King at a parade.\"", new[] { "On4" });
        t.Line("On4", Smolak, "\"Wehlen asks this Alliance for an immediate ceasefire on the line where our armies stand tonight, and for observers to keep it. I am a man who respects the rule of law. Let us see whether the King of Rizia is one too.\"", new[] { "Reply" });

        t.Line("Lib1", Narr, "President Smolak walked to the podium slowly, like a man attending a funeral he had paid for.", new[] { "Lib2" }, condition: Lib);
        t.Line("Lib2", Smolak, "\"Dear fellow leaders. You know me. I am not a man who complains.\"", new[] { "Lib3" });
        t.Line("Lib3", Smolak, "\"But a province of Wehlen has been taken by tanks, its Wehzek families driven from their homes, and this Alliance has said nothing. Nothing! What is this building for, if not for this?\"", new[] { "Lib4" });
        t.Line("Lib4", Smolak, "\"Wehlen asks the Alliance to condemn the Rizian occupation of Zille, and to restore the lawful administration until a fair settlement is found.\"", new[] { "Reply" });

        t.Line("Part1", Narr, "President Smolak took the podium with a map of Zille under his arm. Someone had drawn the ceasefire line on it in red pencil.", new[] { "Part2" }, condition: Part);
        t.Line("Part2", Smolak, "\"The King of Rizia sits in Zille city behind a line of trenches and calls it a homecoming. Everything north of that line is Wehzek, and it will stay Wehzek.\"", new[] { "Part3" });
        t.Line("Part3", Smolak, "\"Wehlen asks this Alliance to place observers on the line, and to declare that no border in South Merkopa has been changed by force. Not one.\"", new[] { "Reply" });

        t.Line("Lost1", Narr, "President Smolak climbed to the podium to applause from the Wehzek benches. He let it run for a while before he raised his hand.", new[] { "Lost2" }, condition: Lost);
        t.Line("Lost2", Smolak, "\"Dear fellow leaders! I once said I would have to drag King Romus to the bargaining table. In the end I had to drag him to the battlefield. But it was all worth it.\"", new[] { "Lost3" });
        t.Line("Lost3", Smolak, "\"Wehlen asks this Alliance to confirm what our soldiers already have. Zille is Wehzek. And it asks that Rizia pay for the war it started.\"", new[] { "Reply" });

        // the King's reply
        t.Logic("Reply", new[] { "Rep1", "Rep2", "Rep3", "Rep4", "Rep5", "Rep6" });
        t.Choice("Rep1", "\"The lease expired. Wehlen kept Zille anyway. Rizia only came to collect what was ours.\"", new[] { "AfterReply" }, condition: NotLib);
        t.Choice("Rep2", "\"Every grave in Zille was dug by Wehlen's refusal to leave.\"", new[] { "AfterReply" }, condition: And(NotLib, $"not ({Lost})"));
        t.Choice("Rep3", "\"Zille is Rizian, not Wehzek. The lease ran out, and Wehlen chose not to leave.\"", new[] { "AfterReply" }, condition: Lib);
        t.Choice("Rep4", "\"Holding a territory for a time does not make it yours. Wehlen held Zille long enough to forget that.\"", new[] { "AfterReply" }, condition: Lib);
        t.Choice("Rep5", "\"Rizia lost a battle. The lease is still expired, and it will be expired tomorrow.\"", new[] { "AfterReply" }, condition: Lost);
        t.Choice("Rep6", "\"Ask Lespia who planted the bomb in Smolak Square. This war was arranged long before I declared it.\"", new[] { "Rep6a" },
            condition: "Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement_Rusty\"] == true",
            script: Set(VarLespiaAccused, "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2));
        t.Line("Rep6a", Narr, "Across the hall, Prime Minister Alvarez examined his fingernails. The Lespian delegation did not applaud anything for the rest of the afternoon.", new[] { "AfterReply" });
        t.Line("AfterReply", Laurento, "\"Whatever happens now, Your Majesty, they will vote on the war, not on the speech.\"", new[] { "Tally" });

        // the roll call
        string hegel = $"if {Rel("Valgsland", "Friendly", "Allied")} then {V(Vote("Hegel"))} = 0 else {V(Vote("Hegel"))} = 1 end; ";
        string angry = $"(Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement_Rusty\"] == true " +
                       $"or {V(VarLespiaAccused)} == true or {V(VarRamiro)} == \"seized\" or Variable[\"ZilleWar.Ramiro_Choice\"] == \"publish\" " +
                       $"or Variable[\"ZilleWar.Lespia_Withdrawn\"] == true or {Rel("Lespia", "Hostile", "Unfriendly")})";
        string helped = $"((Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 or {Rel("Lespia", "Friendly", "Allied")})";
        string lespia = $"if {angry} then {V(Vote("Alvarez"))} = 1; {V(Vote("Walker"))} = 0 elseif {helped} then {V(Vote("Alvarez"))} = -1; {V(Vote("Walker"))} = -1 " +
                        $"else {V(Vote("Alvarez"))} = 0; {V(Vote("Walker"))} = 0 end; ";
        string rumburg = $"if {Rel("Rumburg", "Hostile", "Unfriendly")} or (Variable[\"ZilleWar.Call_Beatrice_Count\"] or 0) >= 3 then {V(Vote("Beatrice"))} = 0 else {V(Vote("Beatrice"))} = -1 end; ";
        string derdia = $"if {Rel("Derdia", "Hostile")} then {V(Vote("Azmal"))} = 0 else {V(Vote("Azmal"))} = -1 end; ";
        string bad = "Variable[\"RiziaDLC.Situation_Diplomacy_GlobalImage_Bad\"] == true";
        string morella = $"if {Rel("Morella", "Hostile", "Unfriendly")} or {bad} then {V(Vote("Saltana"))} = 1 elseif {Rel("Morella", "Friendly", "Allied")} then {V(Vote("Saltana"))} = -1 else {V(Vote("Saltana"))} = 0 end; ";
        string pales = $"if Variable[\"RiziaDLC.War_Pales_Won\"] == true then {V(Vote("Reinhart"))} = 9 " +
                       $"elseif Variable[\"RiziaDLC.Pales_War\"] == true or {Rel("Pales", "Hostile", "Unfriendly", "AtWar")} then {V(Vote("Reinhart"))} = 1 " +
                       $"elseif {Rel("Pales", "Friendly", "Allied")} then {V(Vote("Reinhart"))} = -1 else {V(Vote("Reinhart"))} = 0 end; ";
        string sordland = $"if {Rel("Sordland", "Friendly", "Allied")} and not ({bad}) then {V(Vote("Rayne"))} = 0 else {V(Vote("Rayne"))} = 1 end; ";
        string agnolia = $"if {V(VarAgnolia)} == true then {V(Vote("VanHoorten"))} = -1 else {V(Vote("VanHoorten"))} = 0 end; ";
        string sum = $"{V(VarVote)} = 1 + {V(Vote("Hegel"))} + 1 + {V(Vote("Azmal"))} + {V(Vote("Alvarez"))} + {V(Vote("Walker"))} + {V(Vote("VanHoorten"))} + " +
                     $"{V(Vote("Rayne"))} + {V(Vote("Saltana"))} + {V(Vote("Beatrice"))}; " +
                     $"if {V(Vote("Reinhart"))} ~= 9 then {V(VarVote)} = {V(VarVote)} + {V(Vote("Reinhart"))} end; ";
        string addAll = "AddConversant(\"Jorga Azmal\"); AddConversant(\"Patricio Alvarez (R)\"); AddConversant(\"Dwight Walker (R)\"); " +
                        "AddConversant(\"Maartin Van Hoorten (R)\"); AddConversant(\"Anton Rayne (R)\"); AddConversant(\"Axel Reinhart\"); " +
                        "AddConversant(\"Alma Saltana\"); AddConversant(\"Beatrice Livingston (R)\"); AddConversant(\"Emmerich Hegel (R)\"); " +
                        "AddConversant(\"Leon Malenyev (R)\"); ";
        t.Logic("Tally", new[] { "VoteIntro" }, script: hegel + lespia + rumburg + derdia + morella + pales + sordland + agnolia + sum + addAll);
        t.Line("VoteIntro", Narr, "President Nines opened the floor. One by one, the delegations announced their votes on Wehlen's resolution.", new[] { "V_Smolak" });
        t.Line("V_Smolak", Smolak, "\"Wehlen votes aye, obviously.\"", Roll("Hegel"));
        Delegate(t, "Hegel", Hegel, new[] { "V_Malenyev_Aye" });
        Delegate(t, "Malenyev", "Leon Malenyev (R)", Roll("Azmal"), alwaysAye: true);
        Delegate(t, "Azmal", "Jorga Azmal", Roll("Alvarez"));
        Delegate(t, "Alvarez", Alvarez, Roll("Walker"));
        Delegate(t, "Walker", "Dwight Walker (R)", Roll("VanHoorten"));
        Delegate(t, "VanHoorten", "Maartin Van Hoorten (R)", Roll("Rayne"));
        Delegate(t, "Rayne", "Anton Rayne (R)", Cat(Roll("Reinhart"), "V_Reinhart_skip"));
        Delegate(t, "Reinhart", "Axel Reinhart", Roll("Saltana"));
        t.Logic("V_Reinhart_skip", Roll("Saltana"), condition: $"{V(Vote("Reinhart"))} == 9");
        Delegate(t, "Saltana", "Alma Saltana", Roll("Beatrice"));
        Delegate(t, "Beatrice", "Beatrice Livingston (R)", new[] { "Count" });

        string removeAll = addAll.Replace("AddConversant", "RemoveConversant") + "RemoveConversant(\"Wiktor Smolak (R)\"); ";
        string passed = $"{V(VarVote)} > 0";
        t.Line("Count", Narr, "The tally board above the stage clicked over, row by row. The hall went quiet before the last number turned.", new[] { "Pass", "Fail" });
        t.Line("Pass", Nines, "\"The resolution is adopted.\"", new[] { "PassOn", "PassLib", "PassPart", "PassLost" }, condition: passed, script: Set(VarANResult, "\"passed\""));
        t.Line("Fail", Nines, "\"The resolution is not adopted.\"", new[] { "FailOn", "FailLib", "FailPart", "FailLost" }, condition: $"not ({passed})", script: Set(VarANResult, "\"failed\""));

        string weakSanctions = "Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Weak\"] = Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Weak\"] == true or Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Heavy\"] ~= true; ";

        // ongoing: a ceasefire call
        t.Line("PassOn", Nines, "\"The Alliance of Nations calls on the Kingdom of Rizia and the Republic of Wehlen to cease fire at once, on the present line of contact, under the supervision of Alliance observers.\"",
            new[] { "PassOnHub" }, condition: Ongoing);
        t.Logic("PassOnHub", new[] { "AcceptCease", "Defy" });
        t.Choice("AcceptCease", "\"Rizia will honor the Alliance's call. Tell the generals to hold their fire.\"", new[] { "AcceptCease2" },
            script: Set(WarFlow.VarCeasefire, "true"));
        t.Line("AcceptCease2", Laurento, "\"A wise choice, Your Majesty. And for once, the newspapers will agree with me.\"", new[] { "Close" });
        t.Choice("Defy", "\"Rizia will finish what it started.\"", new[] { "Defy2" }, cost: "-1 Budget Per Turn",
            script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -1) + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1));
        t.Line("Defy2", Laurento, "\"Then the Alliance will make us pay for every week of it. The finance ministers in this room have already started counting.\"", new[] { "Close" });
        t.Line("FailOn", Smolak, "\"Then Wehlen will defend itself, as it always has. Without anyone's help.\"", new[] { "Close" }, condition: Ongoing);

        // liberated: condemnation or silence
        t.Line("PassLib", Nines, "\"The Alliance of Nations condemns the use of force in the Zille region and calls on the Kingdom of Rizia to restore the previous administration pending a negotiated settlement.\"",
            new[] { "PassLib2" }, condition: Lib, script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -1));
        t.Line("PassLib2", Laurento, "\"A resolution with no army behind it, Your Majesty. Nobody will come for Zille. But it will follow us into every room for years, and it will cost us in trade.\"", new[] { "Close" });
        t.Line("FailLib", Nines, "\"The Alliance of Nations takes note of the situation in the Zille region.\"", new[] { "FailLib2" }, condition: Lib,
            script: Set("RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol", "false"));
        t.Line("FailLib2", Laurento, "\"Takes note. In this building, Your Majesty, that is what recognition sounds like.\"", new[] { "Close" });

        // divided: observers on the line
        t.Line("PassPart", Nines, "\"Alliance observers will be deployed along the ceasefire line in the Zille region. No change of borders by force will be recognized by this Alliance.\"",
            new[] { "PassPart2" }, condition: Part);
        t.Line("PassPart2", Laurento, "\"Observers on the line, and Zille city on our side of it. We can live with that, Your Majesty. Smolak will have to.\"", new[] { "Close" });
        t.Line("FailPart", Laurento, "\"No observers, then. The line is ours to guard, and his to probe. I would not cut the garrison in Zille city just yet.\"", new[] { "Close" }, condition: Part);

        // lost: reparations
        t.Line("PassLost", Nines, "\"The Alliance of Nations affirms the administration of the Zille region by the Republic of Wehlen and directs financial measures against the Kingdom of Rizia until compensation is agreed.\"",
            new[] { "PassLost2" }, condition: Lost, script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -2));
        t.Line("PassLost2", Smolak, "\"Thank you, thank you. Wehlen is a forgiving nation. It will accept payment in installments.\"", new[] { "Close" });
        t.Line("FailLost", Smolak, "\"No? Then Wehlen will collect on its own. It has had practice.\"", new[] { "Close" }, condition: Lost);

        t.Logic("Close", new[] { "#6" }, script: removeAll);
        t.Register();
    }

    private static string[] Roll(string who) => new[] { $"V_{who}_Aye", $"V_{who}_Nay", $"V_{who}_Abs" };

    private static void Delegate(Talk t, string who, string speaker, string[] next, bool alwaysAye = false)
    {
        string v = V(Vote(who));
        if (alwaysAye)
        {
            t.Line($"V_{who}_Aye", speaker, "\"Aye.\"", next);
            return;
        }

        t.Line($"V_{who}_Aye", speaker, "\"Aye.\"", next, condition: $"{v} == 1");
        t.Line($"V_{who}_Nay", speaker, "\"Nay.\"", next, condition: $"{v} == -1");
        t.Line($"V_{who}_Abs", speaker, "\"Abstain.\"", next, condition: $"{v} == 0");
    }

    // ------------------------------------------------------------------ c402 sanctions: Smolak speaks first, Hegel names the war

    private static void RegisterSanctions()
    {
        // In a war game the Valgsland accusation also stands on the war alone (vanilla needs a bad image and a
        // hostile Valgsland; the Lespian one needs the Pales war).
        Vanilla.AppendScript(AN, 24,
            $"if {War} and ({Ongoing} or {Lib} or {WarFlags.L(WarFlags.ZilleCityHeld)}) and not {Rel("Valgsland", "Friendly", "Allied")} then " +
            "Variable[\"RiziaDLCIsolated.ANSanctionAccusation_Valgsland_GlobalImage\"] = true end");

        var t = new Talk(Sanctions, "ZilleWar.Late.Sanctions.");
        t.Line("Smolak1", Narr, "Before the Chancellor could reach the podium, President Smolak rose from the Wehzek bench and asked to be heard first. Hegel yielded the floor with a small, satisfied nod.",
            new[] { "Smolak2On", "Smolak2Lib", "Smolak2Part", "Smolak2Lost" }, condition: War, script: "AddConversant(\"Wiktor Smolak (R)\");", hooks: Talk.From(269));
        t.Line("Smolak2On", Smolak, "\"Guilty of invading a peaceful province. Guilty of shelling a city full of people who never asked to be liberated. I second every word the Chancellor is about to say.\"",
            new[] { "Smolak3", "Smolak3Skip" }, condition: Ongoing);
        t.Line("Smolak2Lib", Smolak, "\"Guilty of occupation. Guilty of driving Wehzek families out of homes they lived in for twenty-five years. I second every word the Chancellor is about to say.\"",
            new[] { "Smolak3", "Smolak3Skip" }, condition: Lib);
        t.Line("Smolak2Part", Smolak, "\"Guilty of starting a war it could not finish, and of sitting in Zille city as if it had. I second every word the Chancellor is about to say.\"",
            new[] { "Smolak3", "Smolak3Skip" }, condition: Part);
        t.Line("Smolak2Lost", Smolak, "\"Guilty of starting a war and losing it, which in my country is the worse crime. I second every word the Chancellor is about to say.\"",
            new[] { "Smolak3", "Smolak3Skip" }, condition: Lost);
        t.Line("Smolak3", Smolak, "\"And if Rizian money paid Bludish terrorists to burn my ports, let the record show that too.\"", new[] { "Smolak4" },
            condition: "(Variable[\"ZilleWar.Lever_BffRaids\"] or 0) > 0 or Variable[\"RiziaDLC.Resources_Military_RumburgianBFFMercenaries\"] == true");
        t.Logic("Smolak4", new[] { "#111" }, script: "RemoveConversant(\"Wiktor Smolak (R)\");");
        // the Smolak3 line is optional; when it does not apply, go straight on
        t.Logic("Smolak3Skip", new[] { "Smolak4" }, condition: "not ((Variable[\"ZilleWar.Lever_BffRaids\"] or 0) > 0 or Variable[\"RiziaDLC.Resources_Military_RumburgianBFFMercenaries\"] == true)");

        t.Line("HegelWar", Hegel, "\"Rizia did not wait for a vote, or a treaty, or this Alliance. It sent its army into Zille, in the name of a lease that expired on paper while the people who live there were never asked.\"",
            new[] { "#12", "#13" }, condition: War, hooks: Talk.From(11));
        t.Line("HegelFleet", Hegel, "\"The Democratic Republic of Valgsland will not watch a monarchy redraw borders by force. Our fleet in the Antacean has been placed on alert.\"",
            new[] { "#26" }, condition: And(War, $"not ({Lost})"), hooks: Talk.From(132));
        t.Register();
    }

    // ------------------------------------------------------------------ c403 Heljiland, a mirror

    private static void RegisterHeljiland()
    {
        var t = new Talk(Heljiland, "ZilleWar.Late.Heljiland.");
        // after Van Hoorten's "Would you not defend it?" ([26]); Laurento's line replaces vanilla [31] "He has a point."
        t.Line("Mirror", Narr, "He did not need to say the name of Zille. Half the hall turned to look at me anyway.", new[] { "Laurento" },
            condition: War, hooks: Talk.From(26));
        t.Line("Laurento", Laurento, "\"He has a point. And so do we. That is exactly why nobody here will say it aloud.\"", new[] { "#33", "#32", "#34", "#83" });
        Vanilla.Require(Heljiland, 31, Peace);
        Vanilla.AppendScript(Heljiland, 56, $"if {War} then {V(VarAgnolia)} = true end");
        t.Register();
    }

    // ------------------------------------------------------------------ c409 the security brief

    private static void RegisterBrief()
    {
        var t = new Talk(Brief, "ZilleWar.Late.Brief.");

        // investigation half: lines that expect the investigation to win Zille back by treaty
        Swap(t, Brief, "Intel1082", 1082, new[] { 1058 }, "Lucita Azaro", S.All(
            "\"Our intelligence-sharing arrangement with Wehlen died with the declaration of war. Whatever Smolak learns about this, he learns from us, or from the newspapers.\""),
            new[] { "#1088" });
        Swap(t, Brief, "Intel1089", 1089, new[] { 1088 }, R, S.All("\"Good. Let's see how Smolak explains that to his own people.\""), new[] { "#1116" });
        Swap(t, Brief, "Intel1101", 1101, new[] { 1085 }, "Lucita Azaro", S.All(
            "\"If we publish it, the world learns who really started this war, and President Smolak loses his martyrdom.\""), new[] { "#1102" });
        Swap(t, Brief, "Intel1112", 1112, new[] { 1095 }, "Titus Gordion", S.All(
            "\"Proving Rizia's innocence will take the sting out of every speech Smolak gives in Konstantium.\""), new[] { "#1114" });
        Swap(t, Brief, "Intel1134", 1134, new[] { 1124, 1129, 1131 }, R, S.All("\"Good. Let the Wehzeks read about it in their own papers.\""), new[] { "#1144" });

        // the Zille half: the peacetime contingency plan becomes a war brief, then the vanilla farewells
        t.Logic("Start", new[] { "Front_L", "Front_Ta", "Front_Ti" }, condition: War, hooks: Talk.From(891),
            script: "PlayLoopedMusic(\"R_Uncertainty\",\"R_Uncertainty_Loop\");");
        string[] afterFront = { "Bastards", "Evidence", "Consulate", "State" };
        FrontReport(t, afterFront);

        t.Line("Bastards", "Lucita Azaro", "\"A year ago you sat in that chair and told me to bomb the bastards. I thought you were joking, Your Majesty.\"",
            new[] { "BastardsR1", "BastardsR2" }, condition: And(CouncilLucita, Ongoing));
        t.Choice("BastardsR1", "\"I never joke about artillery.\"", new[] { "BastardsR3" });
        t.Choice("BastardsR2", "\"So did I, for a while.\"", new[] { "BastardsR3" });
        t.Logic("BastardsR3", new[] { "Buried", "Evidence", "Consulate", "State" });

        // the Lespia evidence, if found and not yet published
        string found = "Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true and Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] ~= true";
        // the consulate files the King had burned during the war (SupplyVoices "Bury"): the truth comes back by another road
        t.Logic("Buried", new[] { "Bur_L", "Bur_Ta", "Bur_Ti" }, condition: And(found, "Variable[\"ZilleWar.Ramiro_Choice\"] == \"bury\""));
        Councilor(t, "Bur", "\"Your Majesty had the consulate files burned in Zille. The name did not burn with them. Our own investigation has reached Héctor Ramiro by another road, and this time the evidence is ours, not Smolak's.\"",
            new[] { "Evidence" });
        t.Logic("Evidence", new[] { "Ev_L", "Ev_Ta", "Ev_Ti" }, condition: found);
        Councilor(t, "Ev", "\"There is still the matter of the bombing. We know now who planted that device under the grandstand. If we publish, the world learns who started this war, and Smolak loses his best speech.\"",
            new[] { "EvPublish", "EvKeep", "EvLater" });
        t.Choice("EvPublish", "\"Publish it. The world should know who lit the fuse in Smolak Square.\"", new[] { "EvPublish2" },
            script: Set("RiziaDLC.Zille_Publicized_LespianInvolvement", "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2) +
                    $"if {Ongoing} and (Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 and (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) < 10 then " +
                    $"Variable[\"{ZilleCampaign.VarStrength}\"] = (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) + 1 end; ");
        t.Line("EvPublish2", Narr, "It would be in every newspaper in Merkopa by the morning. Somewhere in Perla, I imagined, a telephone was already ringing.", new[] { "Consulate", "State" });
        t.Choice("EvKeep", "\"Keep it in the safe. Alvarez's ships are worth more to us than his shame.\"", new[] { "Consulate", "State" },
            condition: "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0");
        t.Choice("EvLater", "\"Not yet. We keep it until we need it.\"", new[] { "Consulate", "State" });

        // Héctor Ramiro, in a consulate inside a city we hold
        // (the supply-war module may already have searched the consulate during the war: ZilleWar.Ramiro_Found)
        string ramiro = And("Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement_Rusty\"] == true",
            WarFlags.L(WarFlags.ZilleCityHeld), "Variable[\"ZilleWar.Ramiro_Found\"] ~= true");
        t.Logic("Consulate", new[] { "Cons_L", "Cons_Ta", "Cons_Ti" }, condition: ramiro);
        Councilor(t, "Cons", "\"One more thing. The Lespian consulate on the harbor front in Zille city. Our men have had it surrounded since the city fell. Héctor Ramiro is still inside, burning paper in the fireplace, by the smell of it.\"",
            new[] { "ConsStorm", "ConsFree", "ConsSeal" },
            titus: "\"One more thing, sir. The Lespian consulate on the harbor front in Zille city. The Golden Guard has had it surrounded since the city fell. Héctor Ramiro is still inside, burning paper in the fireplace, by the smell of it.\"");
        t.Choice("ConsStorm", "\"Storm it. Immunity ends where the bombs began.\"", new[] { "ConsStorm2" },
            script: Set(VarRamiro, "\"seized\"") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -3) + Add("RiziaDLC.LeaningValue_Democratic", -1));
        t.Line("ConsStorm2", Narr, "By nightfall, Ramiro was in a cell in Porte Drazon and his safe was on my desk, its door blown off its hinges. Lespia recalled its ambassador before breakfast.", new[] { "State" });
        t.Choice("ConsFree", "\"Let him sail home. Alvarez will know exactly who let him go.\"", new[] { "ConsFree2" },
            script: Set(VarRamiro, "\"released\"") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", 2));
        t.Line("ConsFree2", Narr, "Ramiro left Zille on a Lespian freighter two days later, waving from the rail to the soldiers on the quay. Prime Minister Alvarez sent a case of wine. There was no card.", new[] { "State" });
        t.Choice("ConsSeal", "\"Seal the building and let him sit there.\"", new[] { "State" }, script: Set(VarRamiro, "\"sealed\""));

        // the state of the war: one decision each
        t.Logic("State", new[] { "OnPartisans_L", "OnPartisans_Ta", "OnPartisans_Ti", "LibOcc_L", "LibOcc_Ta", "LibOcc_Ti", "PartLine_L", "PartLine_Ta", "PartLine_Ti", "LostRef_L", "LostRef_Ta", "LostRef_Ti" });

        Councilor(t, "OnPartisans", "\"Su Omina's cells behind the Wehzek lines are asking for rifles and money. They know every goat track in those hills. Their methods are extreme, and Wehlen will call every one of them a war crime at the Alliance.\"",
            new[] { "OnArm", "OnNoArm" }, condition: Ongoing);
        t.Choice("OnArm", "\"Arm them. They know the ground better than our generals do.\"", new[] { "Gold" }, cost: "-1 Budget",
            script: Add("RiziaDLC.Resources_Budget", -1) + Talk.Weaken(1));
        t.Choice("OnNoArm", "\"No. This war is fought by soldiers.\"", new[] { "Gold" });

        Councilor(t, "LibOcc", "\"Zille needs governing until the Crown decides how. Wehzek stragglers in the hills, prisoners in our camps, and a city that does not yet know whose it is. The generals want martial law. Laurento wants an amnesty.\"",
            new[] { "LibMartial", "LibAmnesty", "LibGenerals" }, condition: Lib);
        t.Choice("LibMartial", "\"Martial law, until the last Wehzek rifle is handed in.\"", new[] { "Gold" },
            script: Set(VarOccupation, "\"martial\"") + Add("RiziaDLC.LeaningValue_Democratic", -1));
        t.Choice("LibAmnesty", "\"An amnesty for every conscript who lays down his arms. We are not here to punish Zille.\"", new[] { "Gold" },
            script: Set(VarOccupation, "\"amnesty\"") + Add("RiziaDLC.LeaningValue_Democratic", 1) + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", 1));
        t.Choice("LibGenerals", "\"Leave it to the generals for now.\"", new[] { "Gold" }, script: Set(VarOccupation, "\"generals\""));

        Councilor(t, "PartLine", "\"The line holds, barely. The Wehzeks probe it every night. With money for concrete and wire, the engineers can make Zille city very expensive to take back.\"",
            new[] { "PartFortify", "PartObservers" }, condition: Part);
        t.Choice("PartFortify", "\"Fortify the line.\"", new[] { "Gold" }, cost: "-1 Budget", script: Add("RiziaDLC.Resources_Budget", -1));
        t.Choice("PartObservers", "\"Let the Alliance's observers do the watching. We have spent enough.\"", new[] { "Gold" });

        Councilor(t, "LostRef", "\"Refugees from Zille are crossing at Mave Nortus, a few hundred every night. Families who cheered our columns when they came, and who will not wait for Smolak's police to remember their faces.\"",
            new[] { "LostCamps", "LostClose" }, condition: Lost);
        t.Choice("LostCamps", "\"Open camps at Mave Nortus. They came because of us.\"", new[] { "Gold" }, cost: "-1 Budget",
            script: Add("RiziaDLC.Resources_Budget", -1) + Add("RiziaDLC.Country_PublicOpinion", 1));
        t.Choice("LostClose", "\"Close the crossing. We cannot feed half of Zille.\"", new[] { "Gold" }, script: Add("RiziaDLC.LeaningValue_Democratic", -1));

        // who gets the Auremina gold
        t.Logic("Gold", new[] { "Gold_L", "Gold_Ta", "Gold_Ti", "ToFarewell" });
        Councilor(t, "Gold", "\"And Auremina. The mines are flooded, but the gold is still in the hills. Rizian Royal Gold has already written to the War Ministry. Montoro wants the concession back, and he is offering to pay for the privilege.\"",
            new[] { "GoldRrg", "GoldCrown", "GoldSazon" }, condition: And(Auremina, $"not ({Lost})", $"not ({Ongoing})",
                "Variable[\"ZilleWar.E7_ZilleGold\"] == nil or Variable[\"ZilleWar.E7_ZilleGold\"] == \"\" or Variable[\"ZilleWar.E7_ZilleGold\"] == \"later\""));
        t.Choice("GoldRrg", "\"Give Rizian Royal Gold the concession. Montoro can pay for the pumps.\"", new[] { "ToFarewell" }, cost: "+2 Budget",
            script: Set(VarGold, "\"rrg\"") + Add("RiziaDLC.Resources_Budget", 2));
        t.Choice("GoldCrown", "\"The mines stay with the Crown. Rizian soldiers paid for them.\"", new[] { "ToFarewell" }, script: Set(VarGold, "\"crown\""));
        t.Choice("GoldSazon", "\"Let House Sazon run them. Brenas lost those hills in 1927.\"", new[] { "ToFarewell" },
            script: Set(VarGold, "\"sazon\"") + Add("RiziaDLC.Country_HouseSazon", 1));
        t.Logic("ToFarewell", new[] { "#278" });

        // farewell narration and the one farewell line that assumes Zille is still to be won
        Swap(t, Brief, "Close428", 428, new[] { 427, 1309, 2442 }, Narr, new S(
            Ongoing: "The war in Zille loomed over everything, and I was left alone with a map that had not changed in weeks.",
            Lib: "Zille was ours. Keeping it, I was beginning to understand, would be the harder part.",
            Part: "Half of Zille was ours, and the other half watched it through field glasses. I was left to wonder how long a line drawn in mud could hold.",
            Lost: "Zille was gone again, and this time no treaty had taken it. I sat with that for a long while."), new[] { "#1285" });
        Swap(t, Brief, "Close434", 434, new[] { 433 }, Narr, new S(
            Ongoing: "The war in Zille loomed over everything, and I was left alone with a map that had not changed in weeks.",
            Lib: "Zille was ours. Keeping it, I was beginning to understand, would be the harder part.",
            Part: "Half of Zille was ours, and the other half watched it through field glasses. I was left to wonder how long a line drawn in mud could hold.",
            Lost: "Zille was gone again, and this time no treaty had taken it. I sat with that for a long while."), new[] { "#1285" });
        Swap(t, Brief, "Lucita309", 309, new[] { 308 }, "Lucita Azaro", new S(Lib: "\"Agreed. And this time, we keep it.\""), new[] { "#101" });

        // the newspaper about an "imminent transfer of governance" never runs in a war game
        Vanilla.AppendScript(Brief, 1032, $"if {War} then Variable[\"RiziaDLCSupport.News_Turn08_SecurityBrief_ZilleTransfer\"] = false end");
        t.Register();
    }

    private static void FrontReport(Talk t, string[] next)
    {
        string on = "\"The front has hardly moved since the last operation. The Wehzeks are dug in above the river, and every night their boats still try the strait. The General Staff wants the next push before the autumn rains.\"";
        string lib = "\"Zille is ours from the river to the sea, Your Majesty. What is left is the occupation. Stragglers in the hills, thousands of prisoners in our camps, and a harbor that will take a year to clear.\"";
        string part = "\"The guns have stopped along the line. We hold Zille city and its harbor. The Wehzeks hold the hills and the northern ports, and both sides fire flares at night to remind the other they are awake.\"";
        string lost = "\"Our last units crossed back over the river at Mave Nortus a fortnight ago. Wehlen holds all of Zille again, and Smolak's police are going door to door for anyone who flew our flag.\"";
        foreach ((string sfx, string state, string text) in new[] { ("On", Ongoing, on), ("Lib", Lib, lib), ("Part", Part, part), ("Lost", Lost, lost) })
        {
            t.Line("Front_L" + sfx, "Lucita Azaro", text, next, And(CouncilLucita, state));
            t.Line("Front_Ta" + sfx, "Taddeus Azaro", text.Replace("Your Majesty", "Lieutenant"), next, And(CouncilTaddeus, state));
            t.Line("Front_Ti" + sfx, "Titus Gordion", text.Replace("Your Majesty", "sir"), next, And(CouncilTitus, state));
        }

        t.Logic("Front_L", new[] { "Front_LOn", "Front_LLib", "Front_LPart", "Front_LLost" }, condition: CouncilLucita);
        t.Logic("Front_Ta", new[] { "Front_TaOn", "Front_TaLib", "Front_TaPart", "Front_TaLost" }, condition: CouncilTaddeus);
        t.Logic("Front_Ti", new[] { "Front_TiOn", "Front_TiLib", "Front_TiPart", "Front_TiLost" }, condition: CouncilTitus);
    }

    // ------------------------------------------------------------------ c423 Alvarez's visit

    private static void RegisterAlvarez()
    {
        var t = new Talk(AlvarezVisit, "ZilleWar.Late.Alvarez.");
        string naval = "Variable[\"ZilleWar.Lespia_Caleqabiz\"] == true";

        // the naval rights at Caleqabiz already exist: the lease becomes an extension of them
        foreach ((string name, int entry, int parent, string[] next) in new[] { ("Lease846", 846, 843, new[] { "#847" }), ("Lease869", 869, 868, new[] { "#870" }) })
        {
            Swap(t, AlvarezVisit, name, entry, new[] { parent }, Alvarez, S.All(
                "\"The naval rights you gave us for the war. ATO would like them to outlast it. Lease Caleqabiz to ATO, through Lespia, until the Valgish threat in the Antacean ceases.\""),
                next, when: naval);
        }

        Swap(t, AlvarezVisit, "Lease941", 941, new[] { 940 }, Alvarez, S.All(
            "\"Unless you turn something we already have into something ATO would really like. Our sailors are already at Caleqabiz, Your Majesty.\""),
            new[] { "#942" }, when: naval);
        Swap(t, AlvarezVisit, "Lease875", 875, new[] { 847, 870 }, R, S.All("\"Deal. The naval rights become a lease.\""), new[] { "#877" }, when: naval);

        // "the Zille agreement" never existed in this war
        Swap(t, AlvarezVisit, "Collapse575", 575, new[] { 234 }, R, S.All("\"Your bomb started my war, Mr. Alvarez.\""), new[] { "#578" });
        Swap(t, AlvarezVisit, "Collapse645", 645, new[] { 657 }, R, S.All("\"Your bomb started my war, Mr. Alvarez.\""), new[] { "#648" });
        Swap(t, AlvarezVisit, "Collapse495", 495, new[] { 497 }, R, S.All(
            "\"Please, Mr. Alvarez. Stop. Your people planted that bomb, Smolak used it as his excuse, and my soldiers paid for both of you in Zille.\""),
            new[] { "#502" });
        Swap(t, AlvarezVisit, "Hostage237", 237, new[] { 93 }, R, S.All(
            "\"Who's the enemy here? Your bomb gave President Smolak his excuse to keep Zille, and me my reason to take it.\""), new[] { "#244" });

        // the war in the room, before business
        t.Line("War1", Alvarez, "\"Two of my neighbors at war, and I am the one who has to explain the price of oil to my parliament.\"",
            new[] { "War2", "#41" }, condition: And(War, $"not ({PalesWar})"), hooks: Talk.From(63));
        t.Line("War2", Alvarez, "\"My navy closed the Strait of Kreleck for you, Your Majesty. Lespia is a democracy. My parliament would like to know what it bought.\"",
            new[] { "War3" }, condition: "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0");
        t.Logic("War3", new[] { "WarMitz", "WarLater" });
        t.Choice("WarMitz", "\"Lespia will have my vote on the trade zone. The Meftiem stays as it is.\"", new[] { "WarMitz2" },
            condition: "Variable[\"RiziaDLC.Diplomacy_Lespia_PromiseITZSupport_Late\"] ~= true and Variable[\"RiziaDLC.Diplomacy_Lespia_PromiseITZSupport\"] ~= true",
            script: Set("RiziaDLC.Diplomacy_Lespia_PromiseITZSupport_Late", "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", 2));
        t.Line("WarMitz2", Alvarez, "\"Then we understand each other. I will remind you of it in Meftiem, of course. Politely.\"", new[] { "#41" });
        t.Choice("WarLater", "\"It bought a friend in South Merkopa. Let's talk about what else that friend can do.\"", new[] { "WarLater2" });
        t.Line("WarLater2", Alvarez, "\"Friendship. My favorite currency. It is so hard to spend.\"", new[] { "#41" });
        t.Register();
    }

    // ------------------------------------------------------------------ c589 the council: foreign affairs

    private static void RegisterCouncil()
    {
        var t = new Talk(Council, "ZilleWar.Late.Council.");
        Swap(t, Council, "Smolak96", 96, new[] { 44, 82, 83, 84, 95 }, Laurento, new S(
            Ongoing: "\"President Smolak has agreed to talk, through the Sordish foreign ministry. It is not a negotiation, Your Majesty. Not yet. But it is a telephone line, and we have not had one since Konstantium.\"",
            Lib: "\"Wehlen has asked for talks on the prisoners. And the Alliance will take up Zille in Konstantium. Smolak will want a resolution against us. We should want one that says nothing at all.\"",
            Part: "\"The armistice commission meets this month. President Smolak will be there in person, which tells me he wants Zille city back more than he wants to be seen asking for it.\"",
            Lost: "\"President Smolak has summoned Your Majesty to Wraclawitz. I would not call it an invitation. I would also not refuse it.\""),
            new[] { "R96a", "R96b", "R96c" });
        t.Choice("R96a", "\"I'll hear him out. Whatever he says, the Alliance will hear our answer.\"", new[] { "#118" });
        t.Choice("R96b", "\"Let him talk. Zille is not his to bargain with.\"", new[] { "#118" }, condition: $"not ({Lost})");
        t.Choice("R96c", "\"Then I will go, and I will not bow.\"", new[] { "#118" }, condition: Lost);
        t.Register();
    }

    // ------------------------------------------------------------------ c416 Rusty on the train

    private static void RegisterRusty()
    {
        var t = new Talk(Rusty, "ZilleWar.Late.Rusty.");
        var trip = new S(
            Ongoing: "I was scheduled for a brief stop in the border town before going up to the front, where the Zille campaign was being run from a requisitioned railway hotel.",
            Lib: "I was scheduled for a brief stop in the border town before crossing into Zille. It would be the first Toras train over the river since 1927.",
            Part: "I was scheduled for a brief stop in the border town before meeting President Smolak at the armistice commission, a few miles north of the station.",
            Lost: "I was scheduled for a brief stop in the border town, now full of refugees from Zille, before traveling to Wraclawitz to hear President Smolak's terms.");
        Swap(t, Rusty, "Trip4", 4, new[] { 10 }, Narr, trip, new[] { "#5" });
        Swap(t, Rusty, "Trip11", 11, new[] { 9 }, Narr, trip, new[] { "#5" });
        var papers = new S(
            Ongoing: "Seated on a leather divan in my private salon carriage, I looked once more at the casualty lists from Zille. Names and numbers swam before my eyes.",
            Lib: "Seated on a leather divan in my private salon carriage, I looked once more at the General Staff's plans for the occupation of Zille. Names and numbers swam before my eyes.",
            Part: "Seated on a leather divan in my private salon carriage, I looked once more at a map of the ceasefire line. Names and numbers swam before my eyes.",
            Lost: "Seated on a leather divan in my private salon carriage, I looked once more at the casualty lists from Zille. Names and numbers swam before my eyes.");
        Swap(t, Rusty, "Papers12", 12, new[] { 7 }, Narr, papers, new[] { "#14" });
        Swap(t, Rusty, "Papers13", 13, new[] { 6 }, Narr, papers, new[] { "#14" });
        Swap(t, Rusty, "Mines", 109, new[] { 107 }, "Russello Montoro", new S(
            Ongoing: "\"Heard you're off to the front? Bring me back a souvenir. Preferably a gold mine.\"",
            Lib: "\"Heard Auremina's ours again. Do me a favor and don't let the army blow up any more of my shafts.\"",
            Part: "\"Heard you're off to see Smolak. Ask him nicely for my mines, would you? He's not using them.\"",
            Lost: "\"Heard Smolak's boys are sitting on my old mines again. Shame. I'd just ordered new pumps.\""),
            new[] { "#140", "#141", "#149", "#153" });
        Swap(t, Rusty, "Bye", 480, new[] { 478, 892 }, "Russello Montoro", new S(
            Ongoing: "\"Sorry to disturb you. And good luck at the front.\"",
            Lib: "\"Sorry to disturb you. And good luck up in Zille.\"",
            Part: "\"Sorry to disturb you. And good luck with Smolak.\"",
            Lost: "\"Sorry to disturb you. And good luck in Wraclawitz. You'll need it.\""), new[] { "#704" });
        t.Register();
    }

    // ------------------------------------------------------------------ c422 the House of Delegates

    private static void RegisterHod()
    {
        var t = new Talk(Hod, "ZilleWar.Late.Hod.");
        Swap(t, Hod, "Hugo27", 27, new[] { 26 }, Hugo, new S(
            Ongoing: "\"The war in Zille is the talk of the capital, Your Majesty. Not all of it kind.\"",
            Part: "\"The ceasefire in Zille is the talk of the capital, Your Majesty. Half of them call it a victory. The other half are Delegates.\"",
            Lost: "\"The capital talks of nothing but Zille, Your Majesty. I would keep my head high in there.\""),
            new[] { "#87", "#48", "#80", "#53" });

        // openings: Zille welcomed home by the sword, or the war mourned
        t.Line("OpenAdarfo", "Adarfo Sotelato", "\"We open this session by welcoming Zille home. Not by treaty, but on the backs of the miners and farmhands who fought for it.\"",
            new[] { "#141" }, condition: Lib, hooks: Talk.From(137));
        t.Line("OpenManus", "Manus Sazon", "\"We open this session by welcoming Zille home, returned to Rizia by the courage of our soldiers.\"",
            new[] { "#216" }, condition: Lib, hooks: Talk.From(212));
        t.Line("OpenDaria", "Daria de Rava", "\"We open this session by welcoming Zille home. Not by treaty, but by the sacrifice of our soldiers.\"",
            new[] { "#633" }, condition: Lib, hooks: Talk.From(617));
        Swap(t, Hod, "Adarfo136", 136, new[] { 135 }, "Adarfo Sotelato", new S(
            Ongoing: "\"We open this session by counting our dead in Zille. The list is long, and it is still growing.\"",
            Part: "\"We open this session by acknowledging a war that has ended without a peace, and a Zille cut in two.\"",
            Lost: "\"We open this session in mourning, for the soldiers who died in Zille and for the Zille they died for.\""), new[] { "#405" });
        Swap(t, Hod, "Manus488", 488, new[] { 211 }, "Manus Sazon", new S(
            Ongoing: "\"We open this session with a prayer for our soldiers in Zille, and for an end to the fighting.\"",
            Part: "\"We open this session acknowledging that the war in Zille has ended with only half of what was promised.\"",
            Lost: "\"We open this session in mourning. Zille is lost, and many of our sons with it.\""), new[] { "#489" });
        Swap(t, Hod, "Daria621", 621, new[] { 616 }, "Daria de Rava", new S(
            Ongoing: "\"We open this session in support of our soldiers in Zille, and of the Crown that sent them.\"",
            Part: "\"We open this session acknowledging that the war in Zille has ended where it stood, with Zille city in Rizian hands.\"",
            Lost: "\"We open this session acknowledging, with regret, that the war in Zille has not achieved its aims.\""), new[] { "#624" });
        t.Register();
    }

    // ------------------------------------------------------------------ smaller Turn 8 scenes

    private static void RegisterSmallScenes()
    {
        var w = new Talk(Wedding, "ZilleWar.Late.Wedding.");
        foreach ((string sfx, string state, string text) in new[]
                 {
                     ("On", Ongoing, "The mood in the country was jubilant, though black armbands for the fallen of Zille dotted the crowds."),
                     ("Lib", Lib, "The mood in the country was jubilant, buoyed even further by the liberation of Zille. Zillean flags hung from the balconies beside our own."),
                     ("Part", Part, "The mood in the country was jubilant, though the ceasefire line through Zille lent solemn undertones to the celebrations."),
                     ("Lost", Lost, "The mood in the country was jubilant, though the defeat in Zille hung over the celebrations like smoke."),
                 })
        {
            w.Line("Mood" + sfx, Narr, text, new[] { "#18" }, condition: state, hooks: Talk.From(13));
        }

        w.Register();

        var h = new Talk(HugoLeaves, "ZilleWar.Late.HugoLeaves.");
        Swap(h, HugoLeaves, "Return3", 3, new[] { 84 }, Narr, S.All("A few days after my meeting with President Smolak, I was told that my uncle wanted to speak to me."), new[] { "#82" });
        h.Register();

        var c = new Talk(HugoConfronts, "ZilleWar.Late.HugoConfronts.");
        Swap(c, HugoConfronts, "Rico36", 36, new[] { 11 }, R, S.All("\"I will be honest with you. Rico stays where he is until this war is settled.\""), new[] { "#37" });
        Swap(c, HugoConfronts, "Proof38", 38, new[] { 37 }, Hugo, S.All(
            "\"We have proven to the world that Lespia was the true culprit. You went to war over a bombing my son did not commit, and still you keep him in a cell.\""),
            new[] { "#39", "#25", "#40" });
        c.Register();

        var n = new Talk(NightBefore, "ZilleWar.Late.Night.");
        Swap(n, NightBefore, "Lucita204", 204, new[] { 206 }, "Lucita Azaro", new S(
            Ongoing: "\"I was just reviewing the casualty lists from Zille.\"",
            Lib: "\"I was just reviewing the garrison rosters for Zille.\"",
            Part: "\"I was just reviewing the reports from the ceasefire line.\"",
            Lost: "\"I was just reviewing the casualty lists from Zille. Again.\""), new[] { "#213", "#215", "#218", "#220" });
        n.Register();
    }

    // ------------------------------------------------------------------ the Turn 8 meeting with Smolak (replaces c424)

    private static void RegisterSmolak()
    {
        var t = new Talk(SmolakConv, "ZilleWar.Late.Smolak8.");
        t.Logic("Start", new[] { "On1", "Lib1", "Part1", "Lost1" }, hooks: Talk.AtStart(SmolakConv),
            script: Set(VarAsked, "false") + "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\");");

        // ongoing: a back channel in a railway car
        t.Line("On1", Narr, "The meeting took place in a dining car on a siding outside Mave Nortus, its windows taped against blast. The Sordish foreign ministry had arranged it, and a Sordish attaché sat at the far end of the table pretending to read a newspaper.",
            new[] { "On2" }, condition: Ongoing);
        t.Line("On2", Narr, "President Smolak arrived twenty minutes late, in a greatcoat too heavy for the season, and sat down across from me without offering his hand.",
            new[] { "On3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("On3", Smolak, "\"No airship today, Your Majesty. Your artillery has opinions about airships.\"", new[] { "On4" });
        t.Line("On4", Smolak, "\"I'll be brief, because every hour we sit here, your boys and mine are lying in the same mud.\"", new[] { "On5" });
        t.Line("On5", Narr, "He slid a paper napkin across the table. On it, in his looping hand, were three words: line, observers, prisoners.", new[] { "On6" });
        t.Line("On6", Smolak, "\"A ceasefire on the line where our armies stand tonight. Alliance observers on both sides. Prisoners home by winter. Nobody wins, nobody loses, and we both go home and tell our people how brave we were.\"",
            new[] { "OnAgain", "OnTwo", "OnHub" });
        // the same offer he made on the telephone during the war (SupplyVoices), refused then
        t.Line("OnAgain", Smolak, "\"You will recognize it. It is the offer I made you on the telephone, the night you told me Zille was not for sale. The mud has not improved since. Neither has the offer, but neither has your army.\"",
            new[] { "OnTwo", "OnHub" }, condition: $"{V(SupplyWar.VarSmolakAnswer)} == \"refused\"");
        t.Line("OnTwo", Smolak, "\"And forgive me, but two wars at once? Even I only fight one war at a time, and I have eight wives.\"", new[] { "OnHub" }, condition: PalesWar);
        t.Logic("OnHub", new[] { "OnCease", "OnRefuse", "OnPrisoners" });
        t.Choice("OnCease", "\"Freeze the line. Let the Alliance count the shells instead of us.\"", new[] { "OnCease2" }, script: Set(WarFlow.VarCeasefire, "true"));
        t.Line("OnCease2", Smolak, "\"Wonderful! I knew you were a man of reason. My people will call your people, and then we will both have something to sign.\"", new[] { "OnBye" });
        t.Choice("OnRefuse", "\"No ceasefire while a single Wehzek soldier stands on Rizian soil.\"", new[] { "OnRefuse2" },
            script: Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -1));
        t.Line("OnRefuse2", Smolak, "\"Rizian soil. You keep saying it, as if saying it makes it so. Very well. More mud, then.\"", new[] { "OnBye" });
        t.Choice("OnPrisoners", "\"Withdraw from Zille, and you'll have every one of your prisoners back by the weekend.\"", new[] { "OnPrisoners2" },
            condition: $"{V(VarAsked)} ~= true and (Variable[\"{ZilleCampaign.VarOpsDone}\"] or 0) >= 1", script: Set(VarAsked, "true"));
        t.Line("OnPrisoners2", Smolak, "\"My prisoners? Your Majesty, half of them have already asked for Rizian citizenship. They say the food in your camps is better than in my army. I am not sure I want them back.\"", new[] { "OnHub" });
        t.Line("OnBye", Narr, "He put his hat back on and left the way he had come. The Sordish attaché folded his newspaper, stood, and followed him out without a word.", new[] { "End" });

        // liberated: the Kreleck ferry
        t.Line("Lib1", Narr, "The Wehzek ferry rode at anchor in the middle of the Strait of Kreleck, exactly halfway between Nanta and the Wehzek shore. Measuring the halfway point had taken three days of negotiation.",
            new[] { "Lib2" }, condition: Lib);
        t.Line("Lib2", Narr, "President Smolak was waiting in the passenger lounge, beneath a portrait of himself that someone had forgotten to take down. He did not get up.",
            new[] { "Lib3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Lib3", Smolak, "\"Your Majesty. You took my villa in Zill-a. I would like my khisha pipes back. They were a present from my son.\"", new[] { "Lib4" });
        t.Line("Lib4", Smolak, "\"I am not here to beg. Wehlen does not beg. I am here for my soldiers. You have thousands of them in your camps, eating your bread and learning your songs.\"", new[] { "Lib5" });
        t.Line("Lib5", Smolak, "\"I want them home. All of them, the officers too. In return, Wehlen will consider the matter of Zille closed. For this year.\"", new[] { "LibHub" });
        t.Logic("LibHub", new[] { "LibFree", "LibRansom", "LibHold" });
        t.Choice("LibFree", "\"They can go home, all of them. Let it be the first thing about this war that ends well.\"", new[] { "LibFree2" },
            script: Set(VarPrisoners, "\"released\"") + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", 1));
        t.Line("LibFree2", Smolak, "\"How very regal of you. I will tell their mothers it was my idea.\"", new[] { "Lib6" });
        t.Choice("LibRansom", "\"Your officers stay until Wehlen pays for what its garrison did to Zille.\"", new[] { "LibRansom2" }, cost: "+2 Budget",
            script: Set(VarPrisoners, "\"ransomed\"") + Add("RiziaDLC.Resources_Budget", 2) + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1));
        t.Line("LibRansom2", Smolak, "\"Ransom! From a king! Fine. Wehlen will pay for its officers. Think of it as the rent your father was too polite to charge.\"", new[] { "Lib6" });
        t.Choice("LibHold", "\"Your prisoners go home when Wehlen recognizes the border at the strait.\"", new[] { "LibHold2" }, script: Set(VarPrisoners, "\"held\""));
        t.Line("LibHold2", Smolak, "\"Then they will grow old in Rizia. I hope your cooks are patient.\"", new[] { "Lib6" });
        t.Line("Lib6", Smolak, "\"One more thing. In Konstantium, I will ask the Alliance to condemn you. It is nothing personal. A man must be seen to try.\"", new[] { "Lib7" });
        t.Line("Lib7", Smolak, "\"And tell your mother I kept the garden. Mostly.\"", new[] { "Lib8" });
        t.Line("Lib8", Narr, "The ferry's horn sounded twice. By the time I reached the deck, his launch was already halfway to the Wehzek shore.", new[] { "End" });

        // divided: the armistice commission
        t.Line("Part1", Narr, "The armistice commission met in a village schoolhouse north of Mave Nortus, under an Alliance of Nations flag that had been ironed for the occasion. An observer from President Nines' office sat between the two delegations with a stopwatch.",
            new[] { "Part2" }, condition: Part);
        t.Line("Part2", Narr, "President Smolak had come in person. So had I. Neither of us had planned to, and neither of us would admit it.",
            new[] { "Part3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Part3", Smolak, "\"Your Majesty. Look at us. Two grown men and a schoolroom. If my teachers could see me now.\"", new[] { "Part4" });
        t.Line("Part4", Smolak, "\"Here is the problem. You sit in Zille city. I sit in the hills above it. My men can watch your men shaving in the morning. This cannot go on forever.\"", new[] { "Part4b", "Part5" });
        // the King himself floated a vote in the House of Delegates (EarlyT7)
        t.Line("Part4b", Smolak, "\"And I read in your own newspapers that the King of Rizia told his House of Delegates a vote might be the only way this ends. You see? We agree already. Only you said it first.\"",
            new[] { "Part5" }, condition: WarFlags.L(EarlyT7.VarReferendumFloated));
        t.Line("Part5", Smolak, "\"So, my offer. You hand Zille city to the Alliance's observers, I keep the hills and the ports, and in five years the Zilleans vote. Very democratic. Very modern.\"", new[] { "PartHub" });
        t.Logic("PartHub", new[] { "PartLine", "PartRebuild", "PartSwap" });
        t.Choice("PartLine", "\"Zille city stays Rizian. Draw the line where it stands.\"", new[] { "PartLine2" }, script: Set(VarTerms, "\"line\""));
        t.Line("PartLine2", Smolak, "\"Then it is a border with no name, and you will spend a fortune guarding it. So will I. How wonderful for the concrete industry.\"", new[] { "PartBye" });
        t.Choice("PartRebuild", "\"Rizia will rebuild the city it took. Wehlen can keep its hills, for now.\"", new[] { "PartRebuild2" }, cost: "-2 Budget",
            script: Set(VarTerms, "\"rebuild\"") + Add("RiziaDLC.Resources_Budget", -2) + Add("RiziaDLC.Country_PublicOpinion", 1));
        t.Line("PartRebuild2", Smolak, "\"For now! He says for now. Write that down, observer.\"", new[] { "PartBye" });
        t.Choice("PartSwap", "\"Exchange the prisoners first. The rest can wait.\"", new[] { "PartSwap2" }, script: Set(VarPrisoners, "\"exchanged\""));
        t.Line("PartSwap2", Smolak, "\"Agreed. The rest can always wait. It is the one thing Zille has always been good at.\"", new[] { "PartBye" });
        t.Line("PartBye", Narr, "The observer stopped his watch and wrote down the time. Outside, a child was selling apples to the soldiers of both armies.", new[] { "End" });

        // lost: terms in Wraclawitz
        t.Line("Lost1", Narr, "The message from Wraclawitz had been phrased as an invitation. Laurento advised me to treat it as one, and to go anyway.", new[] { "Lost2" }, condition: Lost);
        t.Line("Lost2", Narr, "Smolak received me in the presidential palace, in a room hung with photographs of his army in Zille. He had chosen the chairs himself. Mine was lower.",
            new[] { "Lost3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Lost3", Smolak, "\"Your Majesty! Welcome to Wehlen. I wanted you to see it for yourself. You were so eager to visit last season.\"", new[] { "Lost4" });
        t.Line("Lost4", Smolak, "\"I won't keep you. The war you started cost Wehlen dearly, and Wehlen expects to be paid. Then, a border at Mave Nortus without a single Rizian gun within twenty kilometers of it.\"", new[] { "Lost5" });
        t.Line("Lost5", Smolak, "\"And a souvenir. Your mother's old house in the harbor district of Zille. My son has taken a liking to it.\"", new[] { "LostHub" });
        t.Logic("LostHub", new[] { "LostPay", "LostRefuse" });
        t.Choice("LostPay", "\"Rizia will pay. Not because Wehlen won, but so that my soldiers' families don't see this war dragged through the courts for years.\"", new[] { "LostPay2" },
            cost: "-3 Budget", script: Set(VarTerms, "\"paid\"") + Add("RiziaDLC.Resources_Budget", -3));
        t.Line("LostPay2", Smolak, "\"Spoken like a man who has learned something. Wehlen accepts.\"", new[] { "Lost6" });
        t.Choice("LostRefuse", "\"Not one guilder. Take it to the Alliance, if you like.\"", new[] { "LostRefuse2" }, script: Set(VarTerms, "\"refused\""));
        t.Line("LostRefuse2", Smolak, "\"Oh, I will. Konstantium is lovely this time of year.\"", new[] { "Lost6" });
        t.Line("Lost6", Smolak, "\"And the house?\"", new[] { "LostHouse", "LostNoHouse" });
        t.Choice("LostHouse", "\"Take the house. And leave my mother out of your speeches.\"", new[] { "LostHouse2" }, script: Set(VarEstelaHome, "true"));
        t.Line("LostHouse2", Smolak, "\"A deal! You see? We can still do business, you and I.\"", new[] { "LostBye" });
        t.Choice("LostNoHouse", "\"You'll have to take it the way you took the rest.\"", new[] { "LostNoHouse2" });
        t.Line("LostNoHouse2", Smolak, "\"I already did, Your Majesty. I am only asking for the deed.\"", new[] { "LostBye" });
        t.Line("LostBye", Narr, "On the drive to the airfield, Wraclawitz was hung with Wehzek flags. On the wall of the railway station, someone had painted a donkey wearing a crown.", new[] { "End" });

        t.End();
        t.Register();
    }
}
