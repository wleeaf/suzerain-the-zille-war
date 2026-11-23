using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.LK;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Turn 8 for a Rizia that went to war over Zille. The war is always over by now (liberated, divided or lost); the "Ongoing"
/// texts below are kept for completeness but never play.
/// - the airship talks with Smolak (c424) are replaced by one meeting per outcome: a prisoner exchange on the Kreleck ferry,
///   an armistice commission (Zille city ours, with or without Auremina), or Smolak's terms in Wraclawitz;
/// - the security brief (c409) skips the peacetime Zille contingency plan for a post-war brief: the armistice line or the
///   occupation, the Lespia evidence, Héctor Ramiro in the Lespian consulate of a Zille city we hold, the Auremina gold;
///   it starts both where vanilla's Zille half starts (891) and where the evidence discussion joins it (878);
/// - the Alliance of Nations session (c401) gets its Zille item back as Smolak's resolution, with a roll-call vote (no
///   reparations demanded if the King already paid them in Wraclawitz); Smolak can open the sanctions debate (c402); Van
///   Hoorten's Heljiland plea (c403) becomes a mirror;
/// - Alvarez (c423) remembers what Lespia did in the war and what it was paid, and the Caleqabiz offers follow the island's
///   war-time status (naval rights or a fifty-year lease);
/// - lines that assume a treaty or a handover are swapped in the council (c589), Rusty's call (c416), the House of
///   Delegates (c422), Hugo (c413, c593), Lucita (c414) and the wedding (c417). AuditT8 has the rest of the line-by-line pass.
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
    /// <summary>The Zille share of the AN financial sanctions (a negative per-turn Budget amount inside vanilla's
    /// Budget_Modifier_ANSanctions), so it can be added back when vanilla's sanctions debate (c402) assigns that variable.</summary>
    private const string VarANZille = "ZilleWar.Late_ANZilleSanction";
    /// <summary>House Sazon was given the Auremina mines (a House row: a script change to a House is erased at the next scene).</summary>
    private const string VarGoldSazon = "ZilleWar.Late_GoldSazon";
    /// <summary>Rusty's Turn 6 pledge: RRG paid six billion for first claim on the Auremina concession.</summary>
    private static readonly string Pledged = $"Variable[\"{SweepEarlyT6Home.VarRustyZilleGold}\"] == \"pledged\"";

    /// <summary>Mave Nortus, the border town, is still held by Wehlen after the armistice.</summary>
    private static readonly string MaveHeld = WarFlags.L(WarFlags.Occupied("Mave Nortus"));
    private static readonly string MaveOurs = WarFlags.LNot(WarFlags.Occupied("Mave Nortus"));
    /// <summary>Divided, and every Zille city Rizian: the Mave Nortus Treaty handed over the last one (Auremina).</summary>
    private static readonly string AllHeld = And(Part, Front.LWehzekExactly(Array.Empty<string>()));

    public static void Register()
    {
        foreach (string v in new[] { VarAgnolia, VarLespiaAccused, VarRamiro, VarGold, VarOccupation, VarANResult, VarPrisoners, VarTerms, VarEstelaHome, VarSmolakT8, VarANZille, VarGoldSazon })
        {
            Variables.Register(v);
        }

        Vanilla.Expose(VarGoldSazon);
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarGoldSazon} == true", "RiziaDLC.Country_HouseSazon += 1;");

        ConversationRegistry.RegisterConversation(SmolakConv);
        ZilleTreaty.Register();
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
        ZilleTreaty.OnStep();
        // After the war the treaty table is scheduled by ZilleTreaty (the turn after the war ends); this is only the
        // back-channel meeting for a war still running on Turn 8, which the state model rules out.
        if (!WarFlow.AtWar || LateState.Turn != 8 || LateState.Step < 11 || !LateState.Declared || Variables.GetBool(VarSmolakT8))
        {
            return;
        }

        string frag = "ZilleWar.Late_SmolakT8";
        if (GameState.StoryFragmentExistsInCurrentStep(frag))
        {
            return;
        }

        // After the war every outcome goes to the treaty table (ZilleTreaty); the old back-channel meeting only for a war still on.
        string conv = WarFlow.AtWar ? SmolakConv : ZilleTreaty.Conv;
        (string title, string sub) = WarFlow.AtWar ? ("A Line to Wraclawitz", "Railway siding, Mave Nortus")
            : LateState.Liberated ? ("The Treaty of Kreleck", "A Wehzek ferry, Strait of Kreleck")
            : LateState.Partial ? ("The Mave Nortus Treaty", Variables.GetBool(WarFlags.Occupied("Mave Nortus")) ? "Schoolhouse on the armistice line" : "Schoolhouse north of Mave Nortus")
            : ("The Terms of Wraclawitz", "Presidential Palace, Wraclawitz");
        try
        {
            GameState.AddCustomConversation(conv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(frag, SuzerainStoryPackInfo.Rizia, "Rizia_Country_Wehlen", title, sub,
                Il2Cpp.ConversationData.ConversationType.ForeignPolicyAndTrade, conv));
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
        string published = "(Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement_Rusty\"] == true)";
        Swap(t, AN, "Evidence98", 98, new[] { 100 }, Laurento, S.All(
            "\"If this comes to light, the Alliance may yet call our war what it was. A reckoning that Lespia arranged.\""),
            new[] { "#104", "#106", "Justify" }, when: "not " + published);
        Swap(t, AN, "Evidence98P", 98, new[] { 100 }, Laurento, S.All(
            "\"Now that it has come to light, the Alliance may yet call our war what it was. A reckoning that Lespia arranged.\""),
            new[] { "#104", "#106", "Justify" }, when: published);
        t.Choice("Justify", "\"Then it justifies the war. Let them hear it.\"", new[] { "#108" }, condition: War);
        Swap(t, AN, "Evidence102", 102, new[] { 101 }, Laurento, new S(Lib:
            "\"Not that we need it to keep Zille. But it would make President Smolak's speech today a great deal harder to deliver.\""),
            new[] { "#104", "#106" });
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
            new[] { "On1", "Lib1", "Part1", "Part1B", "Lost1" });

        // Smolak's speech, one per outcome
        t.Line("On1", Narr, "President Smolak was at the podium before Nines had finished. He had brought no notes.", new[] { "On2" }, condition: Ongoing);
        t.Line("On2", Smolak, "\"Dear fellow leaders! Last year, in this very hall, the King of Rizia stood where I am standing and declared war. On a stage built for peace!\"", new[] { "On3" });
        t.Line("On3", Smolak, "\"Since that day, Wehzek boys have been dying in Zille. A province Wehlen protected for twenty-five years, while Rizia could not even protect its own King at a parade.\"", new[] { "On4" });
        t.Line("On4", Smolak, "\"Wehlen asks this Alliance for an immediate ceasefire on the line where our armies stand tonight, and for observers to keep it. I am a man who respects the rule of law. Let us see whether the King of Rizia is one too.\"", new[] { "Reply" });

        t.Line("Lib1", Narr, "President Smolak walked to the podium slowly, like a man attending a funeral he had paid for.", new[] { "Lib2" }, condition: Lib);
        t.Line("Lib2", Smolak, "\"Dear fellow leaders. You know me. I am not a man who complains.\"", new[] { "Lib3", "Lib3T" });
        t.Line("Lib3", Smolak, "\"But a province of Wehlen has been taken by tanks, its Wehzek families driven from their homes, and this Alliance has said nothing. Nothing! What is this building for, if not for this?\"", new[] { "Lib4" },
            condition: $"{V(ZilleTreaty.VarPeace)} ~= true");
        t.Line("Lib3T", Smolak, "\"But a province that was Wehzek for twenty-five years was taken by tanks, and this Alliance said nothing. Nothing! Now it has a treaty to read instead.\"", new[] { "Lib4T", "Lib4G", "Lib4Gas" },
            condition: $"{V(ZilleTreaty.VarPeace)} == true");
        t.Line("Lib4", Smolak, "\"Wehlen asks the Alliance to condemn the Rizian occupation of Zille, and to restore the lawful administration until a fair settlement is found.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} ~= true");
        // a treaty was signed on the Kreleck ferry before this session (ZilleTreaty): Wehlen keeps its word, and asks for a record
        t.Line("Lib4T", Smolak, "\"Wehlen has signed a treaty, and Wehlen keeps its word. I ask the Alliance only to condemn the war that made that treaty necessary, so that no other small country learns from it.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} == true and {V(ZilleTreaty.VarThreat)} ~= \"gas\" and {V(ZilleTreaty.VarThreat)} ~= \"missiles\"");
        t.Line("Lib4G", Smolak, "\"Wehlen has signed a treaty, and Wehlen keeps its word. But I will tell this Alliance how it was signed. Across a table, with the King of Rizia promising to drop " +
            "Derdian missiles on Wraclawitz. I ask you to condemn that, if you condemn anything.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} == true and {V(ZilleTreaty.VarThreat)} == \"missiles\"");
        t.Line("Lib4Gas", Smolak, "\"Wehlen has signed a treaty, and Wehlen keeps its word. But I will tell this Alliance how it was signed. The King of Rizia sat across a table from me and promised " +
            "to put Rumburgian nerve agent in Derdian missiles and drop them on Wraclawitz. Hundreds of thousands of people. Condemn that, if this building condemns anything.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} == true and {V(ZilleTreaty.VarThreat)} == \"gas\"");

        t.Line("Part1", Narr, "President Smolak took the podium with a map of Zille under his arm. Someone had drawn the ceasefire line on it in red pencil.", new[] { "Part2", "Part2B" },
            condition: And(Part, $"not ({BorderTreaty})"));
        // the Mave Nortus Treaty made the line a border before this session
        t.Line("Part1B", Narr, "President Smolak took the podium with a map of Zille under his arm. Someone had drawn the new border on it in red pencil.", new[] { "Part2", "Part2B" },
            condition: And(Part, BorderTreaty));
        // the King threatened Wraclawitz at the treaty table (ZilleTreaty): Smolak tells the Alliance before he asks for anything
        string missiles = $"{V(ZilleTreaty.VarThreat)} == \"missiles\"";
        string gas = $"{V(ZilleTreaty.VarThreat)} == \"gas\"";
        string noThreat = $"{V(ZilleTreaty.VarThreat)} ~= \"missiles\" and {V(ZilleTreaty.VarThreat)} ~= \"gas\"";
        t.Line("PartThM", Smolak, "\"And this Alliance should know how the King of Rizia bargains. He sat across a table from me at Mave Nortus and promised to drop Derdian missiles on Wraclawitz.\"",
            new[] { "Part3", "Part3T" }, condition: missiles);
        t.Line("PartThG", Smolak, "\"And this Alliance should know how the King of Rizia bargains. He sat across a table from me at Mave Nortus and promised to put Rumburgian nerve agent in Derdian missiles " +
            "and drop them on Wraclawitz.\"", new[] { "Part3", "Part3T" }, condition: gas);
        t.Logic("PartDemand", new[] { "Part3", "Part3T" }, condition: noThreat);
        t.Line("Part2", Smolak, "\"The King of Rizia sits in Zille city behind a line of trenches and calls it a homecoming. Everything on the other side of that line is Wehzek, and it will stay Wehzek.\"", new[] { "PartThM", "PartThG", "PartDemand" },
            condition: $"not ({BorderTreaty})");
        t.Line("Part2B", Smolak, "\"The King of Rizia sits in Zille city behind a border drawn with his tanks, and calls it a homecoming. Everything on the other side of it is Wehzek, and it will stay Wehzek.\"", new[] { "PartThM", "PartThG", "PartDemand" },
            condition: BorderTreaty);
        t.Line("Part3", Smolak, "\"Wehlen asks this Alliance to place observers on the line, and to declare that no border in South Merkopa has been changed by force. Not one.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} ~= true");
        t.Line("Part3T", Smolak, "\"We have signed a treaty, he and I, and drawn a border through Zille. Wehlen asks this Alliance only to place observers on it, so that nobody moves it again by force. Including him.\"", new[] { "Reply" },
            condition: $"{V(ZilleTreaty.VarPeace)} == true");

        t.Line("Lost1", Narr, "President Smolak climbed to the podium to applause from the Wehzek benches. He let it run for a while before he raised his hand.", new[] { "Lost2", "Lost2C", "Lost2S" }, condition: Lost);
        string paid = $"{V(VarTerms)} == \"paid\"";
        string unpaid = $"{V(VarTerms)} ~= \"paid\"";
        t.Line("Lost2", Smolak, "\"Dear fellow leaders! I once said I would have to drag King Romus to the bargaining table. In the end I had to drag him to the battlefield. But it was all worth it.\"", new[] { "LostThM", "LostThG", "LostDemand" },
            condition: $"not ({Ceasefire})");
        // the King sought the ceasefire himself: nobody dragged him anywhere (SupplyVoices sets Ceasefire_Terms only when the King
        // accepted Smolak's own offer, on Smolak's terms)
        string smolakCease = "Variable[\"ZilleWar.Ceasefire_Terms\"] == \"full\" or Variable[\"ZilleWar.Ceasefire_Terms\"] == \"reduced\" " +
                             "or Variable[\"ZilleWar.Ceasefire_Terms\"] == \"rights\"";
        t.Line("Lost2C", Smolak, "\"Dear fellow leaders! I once said I would have to drag King Romus to the bargaining table. In the end he came to it himself, with his hat in his hand. Zille is Wehzek, and I think we can all agree it was worth it.\"", new[] { "LostThM", "LostThG", "LostDemand" },
            condition: And(Ceasefire, $"not ({smolakCease})"));
        t.Line("Lost2S", Smolak, "\"Dear fellow leaders! I once said I would have to drag King Romus to the bargaining table. In the end I offered him peace, and he paid my price for it. Zille is Wehzek, and I think we can all agree it was worth it.\"", new[] { "LostThM", "LostThG", "LostDemand" },
            condition: And(Ceasefire, smolakCease));
        t.Line("LostThM", Smolak, "\"And this Alliance should know how the King of Rizia bargains. He sat across a table from me in Wraclawitz and promised to drop Derdian missiles on my capital.\"",
            new[] { "Lost3", "Lost3Paid", "Lost3Rec" }, condition: missiles);
        t.Line("LostThG", Smolak, "\"And this Alliance should know how the King of Rizia bargains. He sat across a table from me in Wraclawitz and promised to put Rumburgian nerve agent in Derdian missiles " +
            "and drop them on my capital.\"", new[] { "Lost3", "Lost3Paid", "Lost3Rec" }, condition: gas);
        t.Logic("LostDemand", new[] { "Lost3", "Lost3Paid", "Lost3Rec" }, condition: noThreat);
        t.Line("Lost3", Smolak, "\"Wehlen asks this Alliance to confirm what our soldiers already have. Zille is Wehzek. And it asks that Rizia pay for the war it started.\"", new[] { "Reply" },
            condition: $"{unpaid} and {V(ZilleTreaty.VarKind)} ~= \"recognized\"");
        // the King paid Smolak's bill in Wraclawitz (the Turn 8 meeting, before this session)
        t.Line("Lost3Rec", Smolak, "\"Wehlen asks this Alliance only to register what the King of Rizia has already signed in Wraclawitz. Zille is Wehzek, and Rizia says so in writing. I have the pen he used. I am thinking of having it framed.\"",
            new[] { "Reply" }, condition: $"{V(ZilleTreaty.VarKind)} == \"recognized\"");
        t.Line("Lost3Paid", Smolak, "\"Wehlen asks this Alliance only to confirm what our soldiers already have. Zille is Wehzek. As for the bill, His Majesty has agreed to settle it, and I thank him for his promptness.\"",
            new[] { "Reply" }, condition: $"{paid} and {V(ZilleTreaty.VarKind)} ~= \"recognized\"");

        // the King's reply
        t.Logic("Reply", new[] { "Rep1", "Rep2", "Rep3", "Rep4", "Rep5", "Rep5C", "Rep7", "Rep6" });
        t.Choice("Rep1", "\"The lease expired. Wehlen kept Zille anyway. Rizia only came to collect what was ours.\"", new[] { "AfterReply" }, condition: And(NotLib, $"{V(ZilleTreaty.VarKind)} ~= \"recognized\""));
        t.Choice("Rep2", "\"Every grave in Zille was dug by Wehlen's refusal to leave.\"", new[] { "AfterReply" }, condition: And(NotLib, $"not ({Lost})"));
        t.Choice("Rep3", "\"Zille is Rizian, not Wehzek. The lease ran out, and Wehlen chose not to leave.\"", new[] { "AfterReply" }, condition: Lib);
        t.Choice("Rep4", "\"Holding a territory for a time does not make it yours. Wehlen held Zille long enough to forget that.\"", new[] { "AfterReply" }, condition: Lib);
        t.Choice("Rep5", "\"Rizia lost a battle. The lease is still expired, and it will be expired tomorrow.\"", new[] { "AfterReply" }, condition: And(Lost, $"not ({Ceasefire})", $"{V(ZilleTreaty.VarKind)} ~= \"recognized\""));
        t.Choice("Rep5C", "\"Rizia stopped fighting because it chose to. The lease is still expired, and it will be expired tomorrow.\"", new[] { "AfterReply" }, condition: And(Lost, Ceasefire, $"{V(ZilleTreaty.VarKind)} ~= \"recognized\""));
        t.Choice("Rep7", "\"I signed what Wraclawitz required. The lease is still expired, and it will be expired tomorrow.\"", new[] { "AfterReply" },
            condition: And(Lost, $"{V(ZilleTreaty.VarKind)} == \"recognized\""));
        t.Choice("Rep6", "\"Ask Lespia who planted the bomb in Smolak Square. This war was arranged long before I declared it.\"", new[] { "Rep6a" },
            condition: "Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement_Rusty\"] == true",
            script: Set(VarLespiaAccused, "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2));
        t.Line("Rep6a", Narr, "Across the hall, Prime Minister Alvarez examined his fingernails. The Lespian delegation did not applaud anything for the rest of the afternoon.", new[] { "AfterReply" });
        t.Line("AfterReply", Laurento, "\"Whatever happens now, Your Majesty, they will vote on the war, not on the speech.\"", new[] { "Tally" });

        // the roll call
        // Hegel keeps Valgsland's name off the motion only if the King delivered what he promised him: Lespia out of the Meftiem.
        string hegel = $"if {Rel("Valgsland", "Friendly", "Allied")} or {SupplyWar.LuaHegelServed} then {V(Vote("Hegel"))} = 0 else {V(Vote("Hegel"))} = 1 end; ";
        string angry = $"(Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement_Rusty\"] == true " +
                       $"or {V(VarLespiaAccused)} == true or {V(VarRamiro)} == \"seized\" or Variable[\"ZilleWar.Ramiro_Choice\"] == \"publish\" " +
                       $"or Variable[\"ZilleWar.Lespia_Withdrawn\"] == true or Variable[\"{SupplyWar.VarLespiaGrievance}\"] == true " +
                       $"or {Rel("Lespia", "Hostile", "Unfriendly")})";
        string helped = $"((Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 or Variable[\"ZilleWar.Lespia_AllianceVote\"] == true or {Rel("Lespia", "Friendly", "Allied")})";
        string lespia = $"if {angry} then {V(Vote("Alvarez"))} = 1; {V(Vote("Walker"))} = 0 elseif {helped} then {V(Vote("Alvarez"))} = -1; {V(Vote("Walker"))} = -1 " +
                        $"else {V(Vote("Alvarez"))} = 0; {V(Vote("Walker"))} = 0 end; ";
        // Beatrice's last call promised "the proper things at the Alliance": only the insult about her sister (Rumburg_Insulted) or a
        // cold relation costs her vote; a plain refusal of her terms keeps it.
        string rumburg = $"if {Rel("Rumburg", "Hostile", "Unfriendly")} or Variable[\"ZilleWar.Rumburg_Insulted\"] == true then {V(Vote("Beatrice"))} = 0 else {V(Vote("Beatrice"))} = -1 end; ";
        string derdia = $"if {Rel("Derdia", "Hostile")} then {V(Vote("Azmal"))} = 1 else {V(Vote("Azmal"))} = -1 end; ";
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
        t.Line("Pass", Nines, "\"The resolution is adopted.\"", new[] { "PassOn", "PassLib", "PassLibT", "PassPart", "PassPartB", "PassLost", "PassLostPaid" }, condition: passed, script: Set(VarANResult, "\"passed\""));
        t.Line("Fail", Nines, "\"The resolution is not adopted.\"", new[] { "FailOn", "FailLib", "FailPart", "FailLost", "FailLostPaid" }, condition: $"not ({passed})", script: Set(VarANResult, "\"failed\""));

        string weakSanctions = "Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Weak\"] = Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Weak\"] == true or Variable[\"RiziaDLC.Situation_Diplomacy_ANSanctions_Heavy\"] ~= true; ";

        // ongoing: a ceasefire call
        t.Line("PassOn", Nines, "\"The Alliance of Nations calls on Rizia and Wehlen to cease fire at once, on the present line of contact, under the supervision of Alliance observers.\"",
            new[] { "PassOnHub" }, condition: Ongoing);
        t.Logic("PassOnHub", new[] { "AcceptCease", "Defy" });
        t.Choice("AcceptCease", "\"Rizia will honor the Alliance's call. Tell the generals to hold their fire.\"", new[] { "AcceptCease2" },
            script: Set(WarFlow.VarCeasefire, "true"));
        t.Line("AcceptCease2", Laurento, "\"A wise choice, Your Majesty. And for once, the newspapers will agree with me.\"", new[] { "Close" });
        t.Choice("Defy", "\"Rizia will finish what it started.\"", new[] { "Defy2" }, cost: "-1 Budget Per Turn",
            script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -1) + Add(VarANZille, -1) + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1));
        t.Line("Defy2", Laurento, "\"Then the Alliance will make us pay for every week of it. The finance ministers in this room have already started counting.\"", new[] { "Close" });
        t.Line("FailOn", Smolak, "\"Then Wehlen will defend itself, as it always has. Without anyone's help.\"", new[] { "Close" }, condition: Ongoing);

        // liberated: condemnation or silence
        t.Line("PassLib", Nines, "\"The Alliance condemns the use of force in Zille and calls on Rizia to restore the previous administration pending a settlement.\"",
            new[] { "PassLib2" }, condition: And(Lib, $"{V(ZilleTreaty.VarPeace)} ~= true"), script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -1) + Add(VarANZille, -1));
        t.Line("PassLibT", Nines, "\"The Alliance condemns the use of force in Zille, and takes note of the treaty signed on the Kreleck ferry.\"",
            new[] { "PassLib2" }, condition: And(Lib, $"{V(ZilleTreaty.VarPeace)} == true"), script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -1) + Add(VarANZille, -1));
        t.Line("PassLib2", Laurento, "\"A resolution with no army behind it, Your Majesty. Nobody will come for Zille. But it will follow us into every room for years, and it will cost us in trade.\"", new[] { "Close" });
        t.Line("FailLib", Nines, "\"The Alliance of Nations takes note of the situation in the Zille region.\"", new[] { "FailLib2" }, condition: Lib,
            script: Set("RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol", "false"));
        t.Line("FailLib2", Laurento, "\"Takes note. In this building, Your Majesty, that is what recognition sounds like.\"", new[] { "Close" });

        // divided: observers on the line
        t.Line("PassPart", Nines, "\"Alliance observers will be deployed along the ceasefire line in the Zille region. No change of borders by force will be recognized by this Alliance.\"",
            new[] { "PassPart2" }, condition: And(Part, $"{V(ZilleTreaty.VarKind)} ~= \"border\""));
        t.Line("PassPartB", Nines, "\"Alliance observers will be deployed along the border in the Zille region. No change of borders by force will be recognized by this Alliance.\"",
            new[] { "PassPart2" }, condition: And(Part, $"{V(ZilleTreaty.VarKind)} == \"border\""));
        t.Line("PassPart2", Laurento, "\"Observers on the line, and Zille city on our side of it. We can live with that, Your Majesty. Smolak will have to.\"", new[] { "Close" });
        t.Line("FailPart", Laurento, "\"No observers, then. The line is ours to guard, and his to probe. I would not cut the garrison in Zille city just yet.\"", new[] { "Close" }, condition: Part);

        // lost: reparations
        t.Line("PassLost", Nines, "\"The Alliance affirms Wehlen's administration of Zille and directs financial measures against Rizia until compensation is agreed.\"",
            new[] { "PassLost2" }, condition: And(Lost, unpaid), script: weakSanctions + Add("RiziaDLC.Budget_Modifier_ANSanctions", -2) + Add(VarANZille, -2));
        t.Line("PassLost2", Smolak, "\"Thank you, thank you. Wehlen is a forgiving nation. It will accept payment in installments.\"", new[] { "Close" });
        t.Line("FailLost", Smolak, "\"No? Then Wehlen will collect on its own. It has had practice.\"", new[] { "Close" }, condition: And(Lost, unpaid));
        t.Line("PassLostPaid", Nines, "\"The Alliance of Nations affirms the administration of the Zille region by the Republic of Wehlen.\"", new[] { "PassLostPaid2" },
            condition: And(Lost, paid));
        t.Line("PassLostPaid2", Laurento, "\"We paid Smolak in Wraclawitz, Your Majesty, and he has spent it on this. At least the Alliance did not send a bill of its own.\"", new[] { "Close" });
        t.Line("FailLostPaid", Smolak, "\"No? No matter. Wehlen has already been paid.\"", new[] { "Close" }, condition: And(Lost, paid));

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
        // A King who grounded his submarines when Hegel asked (the war-time call: "or Valgsland moves for sanctions") was promised
        // no motion; only the gas threat at the treaty table brings it back.
        string yielded = $"{V(SupplyWar.VarValgslandCall)} == \"yielded\"";
        Vanilla.AppendScript(AN, 24,
            $"if {War} and ((not {Rel("Valgsland", "Friendly", "Allied")} " +
            $"and not {SupplyWar.LuaHegelServed} and not ({yielded})) or {V(ZilleTreaty.VarThreat)} == \"gas\") then " +
            "Variable[\"RiziaDLCIsolated.ANSanctionAccusation_Valgsland_GlobalImage\"] = true end");
        // Vanilla's verdict assigns the sanctions outright (-3 or -6): the Zille share from the Alliance's resolution stays on top.
        string zilleShare = $"if {War} and (Variable[\"{VarANZille}\"] or 0) ~= 0 then Variable[\"RiziaDLC.Budget_Modifier_ANSanctions\"] = " +
                            $"(Variable[\"RiziaDLC.Budget_Modifier_ANSanctions\"] or 0) + Variable[\"{VarANZille}\"] end";
        Vanilla.AppendScript(Sanctions, 240, zilleShare);
        Vanilla.AppendScript(Sanctions, 241, zilleShare);

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
            new[] { "#26" }, condition: And(War, $"not ({Lost})", $"not ({SupplyWar.LuaHegelServed})", $"not ({yielded}) or {V(ZilleTreaty.VarThreat)} == \"gas\""),
            hooks: Talk.From(132));
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

        // investigation half: lines that expect the investigation to win Zille back by treaty (the rest of the first half,
        // including the intelligence-sharing path, is AuditT8's)
        Swap(t, Brief, "Intel1101", 1101, new[] { 1085 }, "Lucita Azaro", S.All(
            "\"If we publish it, the world learns who really lit the fuse, and President Smolak loses his martyrdom.\""), new[] { "#1102" });
        foreach ((string name, int entry, int parent) in new[] { ("Intel1112", 1112, 1095), ("Intel1113", 1113, 1098) })
        {
            Swap(t, Brief, name, entry, new[] { parent }, "Titus Gordion", S.All(
                "\"Proving Rizia's innocence will take the sting out of every speech Smolak gives in Konstantium.\""), new[] { "#1114" });
        }

        // the Zille half: the peacetime contingency plan becomes a war brief, then the vanilla farewells
        t.Logic("Start", new[] { "Front_L", "Front_Ta", "Front_Ti" }, condition: War, hooks: Talk.From(891, 878),
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
        Councilor(t, "Ev", "\"There is still the matter of the bombing. We know now who planted that device under the grandstand. If we publish, the world learns who lit the fuse under this war, and Smolak loses his best speech.\"",
            new[] { "EvPublish", "EvKeep", "EvLater" });
        t.Choice("EvPublish", "\"Publish it. The world should know who lit the fuse in Smolak Square.\"", new[] { "EvPublish2" },
            script: Set("RiziaDLC.Zille_Publicized_LespianInvolvement", "true") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2) +
                    $"if {Ongoing} and (Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 and (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) < 10 then " +
                    $"Variable[\"{ZilleCampaign.VarStrength}\"] = (Variable[\"{ZilleCampaign.VarStrength}\"] or 8) + 1 end; ");
        t.Line("EvPublish2", Narr, "It would be in every newspaper in Merkopa by the morning. Somewhere in Perla, I imagined, a telephone was already ringing.", new[] { "Consulate", "State" });
        t.Choice("EvKeep", "\"Keep it in the safe. Alvarez kept his side of the strait. I can keep a secret, for a price.\"", new[] { "Consulate", "State" },
            condition: "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 and Variable[\"ZilleWar.Lespia_Withdrawn\"] ~= true and " +
                       "Variable[\"ZilleWar.Lespia_StraitThin\"] ~= true and " +
                       $"Variable[\"{SupplyWar.VarLespiaGrievance}\"] ~= true and Variable[\"RiziaDLC.MITZ_ScrewedOverLespia\"] ~= true");
        t.Choice("EvLater", "\"Not yet. We keep it until we need it.\"", new[] { "Consulate", "State" });

        // Héctor Ramiro, in a consulate inside a city we hold
        // (the supply-war module may already have searched the consulate during the war: ZilleWar.Ramiro_Found)
        string ramiro = And("Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement_Rusty\"] == true",
            WarFlags.L(WarFlags.ZilleCityHeld), "Variable[\"ZilleWar.Ramiro_Found\"] ~= true");
        t.Logic("Consulate", new[] { "Cons_L", "Cons_Ta", "Cons_Ti" }, condition: ramiro);
        Councilor(t, "Cons", "\"One more thing. The Lespian consulate on the harbor front in Zille city. Lespia kept it open after the city fell, and Héctor Ramiro never left. Our men have watched the door ever since. Now that we know his name, he has started burning paper in the fireplace, by the smell of it.\"",
            new[] { "ConsStorm", "ConsFree", "ConsSeal" },
            titus: "\"One more thing, sir. The Lespian consulate on the harbor front in Zille city. Lespia kept it open after the city fell, and Héctor Ramiro never left. The Golden Guard has watched the door ever since. Now he has started burning paper in the fireplace, by the smell of it.\"");
        t.Choice("ConsStorm", "\"Storm it. Immunity ends where the bombs began.\"", new[] { "ConsStorm2" },
            script: Set(VarRamiro, "\"seized\"") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -3) + Add("RiziaDLC.LeaningValue_Democratic", -1));
        t.Line("ConsStorm2", Narr, "By nightfall, Ramiro was in a cell in Porte Drazon and his safe was on my desk, its door blown off its hinges. Lespia recalled its ambassador before breakfast.", new[] { "State" });
        t.Choice("ConsFree", "\"Let him sail home. Alvarez will know exactly who let him go.\"", new[] { "ConsFree2" },
            script: Set(VarRamiro, "\"released\"") + Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", 2));
        t.Line("ConsFree2", Narr, "Ramiro left Zille on a Lespian freighter two days later, waving from the rail to the soldiers on the quay. Prime Minister Alvarez sent a case of wine. There was no card.", new[] { "State" });
        t.Choice("ConsSeal", "\"Seal the building and let him sit there.\"", new[] { "State" }, script: Set(VarRamiro, "\"sealed\""));

        // the state of the war: one decision each
        t.Logic("State", new[] { "OnPartisans_L", "OnPartisans_Ta", "OnPartisans_Ti", "LibOcc_L", "LibOcc_Ta", "LibOcc_Ti", "PartLine_L", "PartLine_Ta", "PartLine_Ti", "PartLineB_L", "PartLineB_Ta", "PartLineB_Ti", "LostRef_L", "LostRef_Ta", "LostRef_Ti", "LostRefH_L", "LostRefH_Ta", "LostRefH_Ti" });

        Councilor(t, "OnPartisans", "\"Su Omina's cells behind the Wehzek lines are asking for rifles and money. They know every goat track in those hills. Their methods are extreme, and Wehlen will call every one of them a war crime at the Alliance.\"",
            new[] { "OnArm", "OnNoArm" }, condition: Ongoing);
        t.Choice("OnArm", "\"Arm them. They know the ground better than our generals do.\"", new[] { "Gold" }, cost: "-1 Budget",
            script: Add("RiziaDLC.Resources_Budget", -1) + Talk.Weaken(1));
        t.Choice("OnNoArm", "\"No. This war is fought by soldiers.\"", new[] { "Gold" });

        Councilor(t, "LibOcc", "\"The army has governed Zille since the war under emergency decrees. The generals want to keep martial law until the last Wehzek rifle is handed in. Laurento wants an amnesty, so the Wehzek families who stayed can stop sleeping in their clothes.\"",
            new[] { "LibMartial", "LibAmnesty", "LibGenerals" }, condition: Lib);
        t.Choice("LibMartial", "\"Martial law stays, until the last Wehzek rifle is handed in.\"", new[] { "Gold" },
            script: Set(VarOccupation, "\"martial\"") + Add("RiziaDLC.LeaningValue_Democratic", -1));
        t.Choice("LibAmnesty", "\"An amnesty for every conscript who lays down his arms. We are not here to punish Zille.\"", new[] { "Gold" },
            script: Set(VarOccupation, "\"amnesty\"") + Add("RiziaDLC.LeaningValue_Democratic", 1));
        t.Choice("LibGenerals", "\"Leave it to the generals for now.\"", new[] { "Gold" }, script: Set(VarOccupation, "\"generals\""));

        Councilor(t, "PartLine", "\"The line holds. The Wehzeks still probe it most nights. With money for concrete and wire, the engineers can make Zille city very expensive to take back.\"",
            new[] { "PartFortify", "PartObservers" }, condition: And(Part, $"not ({BorderTreaty})"));
        // the Mave Nortus Treaty made the line a border
        Councilor(t, "PartLineB", "\"The new border holds. The Wehzeks keep to their side of it, mostly. With money for concrete and wire, the engineers can make sure it stays that way.\"",
            new[] { "PartFortify", "PartObservers" }, condition: And(Part, BorderTreaty));
        t.Choice("PartFortify", "\"Fortify the line.\"", new[] { "Gold" }, cost: "-1 Budget", script: Add("RiziaDLC.Resources_Budget", -1));
        t.Choice("PartObservers", "\"Let the Alliance's observers do the watching. We have spent enough.\"", new[] { "Gold" });

        Councilor(t, "LostRef", "\"Refugees from Zille are still crossing at Mave Nortus, a few dozen every night. Families who cheered our army when it came, and who will not wait for Smolak's police to remember their faces.\"",
            new[] { "LostCamps", "LostClose" }, condition: And(Lost, MaveOurs));
        Councilor(t, "LostRefH", "\"Refugees from Zille are still slipping across the armistice line, a few dozen every night, through Wehzek-held Mave Nortus. Families who cheered our army when it came, and who will not wait for Smolak's police to remember their faces.\"",
            new[] { "LostCamps", "LostClose" }, condition: And(Lost, MaveHeld));
        t.Choice("LostCamps", "\"Give them proper housing in Brenas. They came because of us.\"", new[] { "Gold" }, cost: "-1 Budget",
            script: Add("RiziaDLC.Resources_Budget", -1) + Add("RiziaDLC.Country_PublicOpinion", 1));
        t.Choice("LostClose", "\"Close the crossing. We cannot feed all of Zille.\"", new[] { "Gold" }, script: Add("RiziaDLC.LeaningValue_Democratic", -1));


        // who gets the Auremina gold
        string rustyFree = "Variable[\"RiziaDLC.Rusty_Arrested\"] ~= true and Variable[\"RiziaDLC.Rusty_Fled\"] ~= true";
        // Beatrice's Turn 7 answer (EarlyT7Diplomacy) settles who buys the gold; Rusty's paid pledge on the concession must still be
        // honored or refunded, whatever she was told
        string goldOpen = And(Auremina, $"not ({Lost})", $"not ({Ongoing})",
            "Variable[\"ZilleWar.E7_ZilleGold\"] == nil or Variable[\"ZilleWar.E7_ZilleGold\"] == \"\" or Variable[\"ZilleWar.E7_ZilleGold\"] == \"later\" " +
            $"or {Pledged}");
        t.Logic("Gold", Cat(C("Gold"), C("GoldB"), C("GoldP"), C("GoldPB"), "ToFarewell"));
        string open = And(goldOpen, $"not ({Pledged})");
        Councilor(t, "Gold", "\"And Auremina. The mines are flooded, but the gold is still in the hills. Rizian Royal Gold has already written to the War Ministry. Montoro wants the concession back, and he is offering six billion guilders for the privilege.\"",
            new[] { "GoldRrg", "GoldCrown", "GoldSazon" }, condition: And(open, rustyFree));
        Councilor(t, "GoldB", "\"And Auremina. The mines are flooded, but the gold is still in the hills. The board of Rizian Royal Gold has written to the War Ministry. With Montoro gone, they are eager to please, and they are offering six billion guilders for the concession.\"",
            new[] { "GoldRrg", "GoldCrown", "GoldSazon" }, condition: And(open, $"not ({rustyFree})"));
        // Rusty already paid six billion for first claim during the war (SweepEarlyT6Home/AuditT6): he collects, or gets his money back
        string pledgedOpen = And(goldOpen, Pledged);
        Councilor(t, "GoldP", "\"And Auremina. The mines are flooded, but the gold is still in the hills. Montoro paid six billion guilders into the war chest for first claim on the concession, and he has written to collect.\"",
            new[] { "GoldRrgP", "GoldCrownP", "GoldSazonP" }, condition: And(pledgedOpen, rustyFree));
        Councilor(t, "GoldPB", "\"And Auremina. The mines are flooded, but the gold is still in the hills. Montoro paid six billion guilders into the war chest for first claim on the concession. With him gone, his board has written to collect.\"",
            new[] { "GoldRrgP", "GoldCrownP", "GoldSazonP" }, condition: And(pledgedOpen, $"not ({rustyFree})"));
        t.Choice("GoldRrg", "\"Give Rizian Royal Gold the concession. They can pay for the pumps.\"", new[] { "ToFarewell" }, cost: "+2 Budget",
            script: Set(VarGold, "\"rrg\"") + Add("RiziaDLC.Resources_Budget", 2));
        t.Choice("GoldCrown", "\"The mines stay with the Crown. Rizian soldiers paid for them.\"", new[] { "ToFarewell" }, script: Set(VarGold, "\"crown\""));
        t.Choice("GoldSazon", "\"Let House Sazon run them. Brenas lost those hills in 1927.\"", new[] { "ToFarewell" },
            script: Set(VarGold, "\"sazon\"") + Set(VarGoldSazon, "true"));
        t.Choice("GoldRrgP", "\"Rizian Royal Gold paid for first claim. The concession is theirs.\"", new[] { "ToFarewell" },
            script: Set(VarGold, "\"rrg\""));
        t.Choice("GoldCrownP", "\"The mines stay with the Crown. Send Montoro his six billion back.\"", new[] { "ToFarewell" }, cost: "-2 Budget",
            script: Set(VarGold, "\"crown\"") + Add("RiziaDLC.Resources_Budget", -2));
        t.Choice("GoldSazonP", "\"Let House Sazon run them, and send Montoro his money back.\"", new[] { "ToFarewell" }, cost: "-2 Budget",
            script: Set(VarGold, "\"sazon\"") + Set(VarGoldSazon, "true") + Add("RiziaDLC.Resources_Budget", -2));
        t.Logic("ToFarewell", new[] { "#278" });

        // farewell narration and the one farewell line that assumes Zille is still to be won
        foreach ((string name, int entry, int[] parents) in new[] { ("Close428", 428, new[] { 427, 1309, 2442 }), ("Close434", 434, new[] { 433 }) })
        {
            Swap(t, Brief, name, entry, parents, Narr, new S(
                Ongoing: "The war in Zille loomed over everything, and I was left alone with a map that had not changed in weeks.",
                Lib: "Zille was ours. Keeping it, I was beginning to understand, would be the harder part.",
                Lost: "Zille was gone again, and this time it would not come back with a lease. I sat with that for a long while."), new[] { "#1285" });
            // divided: the cities on each side of the line, by name (an armistice line, or the border the Mave Nortus Treaty drew)
            string n = name;
            SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) => Swap(t, Brief, n + "P" + id, entry, parents, Narr, new S(
                Part: $"{ours} {(ours.Contains(" and ") ? "were" : "was")} ours, and {theirs} watched us through field glasses. I was left to wonder how long a line drawn in mud could hold.",
                PartBorder: $"{ours} {(ours.Contains(" and ") ? "were" : "was")} ours, and {theirs} watched us across the new border. I was left to wonder how long a treaty signed in a schoolhouse could hold."),
                new[] { "#1285" }, when: cond));
            // the treaty handed over the last Wehzek city: all of Zille is ours
            Swap(t, Brief, n + "All", entry, parents, Narr, new S(PartBorder:
                "Zille was ours, all of it, by treaty if not entirely by arms. Keeping it, I was beginning to understand, would be the harder part."),
                new[] { "#1285" }, when: AllHeld);
        }
        Swap(t, Brief, "Lucita309", 309, new[] { 308 }, "Lucita Azaro", new S(Lib: "\"Agreed. And this time, we keep it.\""), new[] { "#101" });

        // the newspaper about an "imminent transfer of governance" never runs in a war game
        Vanilla.AppendScript(Brief, 1032, $"if {War} then Variable[\"RiziaDLCSupport.News_Turn08_SecurityBrief_ZilleTransfer\"] = false end");
        t.Register();
    }

    private static void FrontReport(Talk t, string[] next)
    {
        string on = "\"The front has hardly moved since the last operation. The Wehzeks are dug in above the river, and every night their boats still try the strait. The General Staff wants the next push before the autumn rains.\"";
        string lib = "\"Zille is ours from the river to the sea, Your Majesty. A few Wehzek holdouts still hide in the hills, and the engineers are still pulling wrecks out of the harbor.\"";
        string lost = "\"Wehlen still holds Zille city and the whole region, Your Majesty. Smolak's police have spent the months since going door to door for anyone who cheered our army.\"";
        var texts = new List<(string sfx, string state, string text)> { ("On", Ongoing, on), ("Lib", Lib, lib) };
        // divided: name the cities on each side, of an armistice line or of the border the Mave Nortus Treaty drew
        SweepEarlyKit.ForDivided((id, cond, ours, theirs, be) =>
        {
            texts.Add(("Part" + id, And(cond, $"not ({BorderTreaty})"),
                $"\"The armistice line still holds, Your Majesty. We have {ours}. The Wehzeks still have {theirs}, and both sides fire flares at night to remind the other they are awake.\""));
            texts.Add(("PartB" + id, And(cond, BorderTreaty),
                $"\"The new border holds, Your Majesty. We have {ours}, and the treaty leaves {theirs} to Wehlen. Both sides still fire flares at night to remind the other they are awake.\""));
        });
        texts.Add(("PartAll", And(Part, AllHeld),
            "\"The new border holds, Your Majesty. The treaty gave us Auremina, and with it the whole of Zille. Both sides still fire flares across the strait at night to remind the other they are awake.\""));
        // lost: Zille city is Wehzek, though the army may still hold other cities on its side of the line
        texts.Add(("Lost", And(Lost, $"not ({WarFlags.L(ZilleOperation.CapturedVar("Auremina"))})", $"not ({WarFlags.L(ZilleOperation.CapturedVar("Isor"))})",
            $"not ({WarFlags.L(ZilleOperation.CapturedVar("Nanta"))})"), lost));
        foreach (List<string> held in Front.Subsets("Auremina", "Isor", "Nanta"))
        {
            var wehzek = Front.Cities.Where(c => !held.Contains(c)).ToList();
            texts.Add(("LostH" + string.Concat(held), And(Lost, Front.LWehzekExactly(wehzek)),
                $"\"Wehlen still holds Zille city, Your Majesty. Our troops hold {Front.Join(held)} on our side of the line. Smolak's police have spent the months since going door to door in the city for anyone who cheered our army.\""));
        }

        foreach ((string sfx, string state, string text) in texts)
        {
            t.Line("Front_L" + sfx, "Lucita Azaro", text, next, And(CouncilLucita, state));
            t.Line("Front_Ta" + sfx, "Taddeus Azaro", text.Replace("Your Majesty", "Lieutenant"), next, And(CouncilTaddeus, state));
            t.Line("Front_Ti" + sfx, "Titus Gordion", text.Replace("Your Majesty", "sir"), next, And(CouncilTitus, state));
        }

        t.Logic("Front_L", texts.Select(x => "Front_L" + x.sfx).ToArray(), condition: CouncilLucita);
        t.Logic("Front_Ta", texts.Select(x => "Front_Ta" + x.sfx).ToArray(), condition: CouncilTaddeus);
        t.Logic("Front_Ti", texts.Select(x => "Front_Ti" + x.sfx).ToArray(), condition: CouncilTitus);
    }

    // ------------------------------------------------------------------ c423 Alvarez's visit

    private static void RegisterAlvarez()
    {
        var t = new Talk(AlvarezVisit, "ZilleWar.Late.Alvarez.");
        // Lespia's navy at Caleqabiz for the war (SupplyAlvarez): naval rights only, or a fifty-year lease of the island
        string lease = $"{V(SupplyWar.VarLespiaLease)} == true";
        string naval = $"{V(SupplyWar.VarLespiaCaleqabiz)} == true and not ({lease})";

        // the naval rights already exist: the ATO lease becomes an extension of them
        foreach ((string name, int entry, int parent, string[] next) in new[] { ("Lease846", 846, 843, new[] { "#847" }), ("Lease869", 869, 868, new[] { "#870" }) })
        {
            Swap(t, AlvarezVisit, name, entry, new[] { parent }, Alvarez, S.All(
                "\"The naval rights you gave us for the war. ATO would like them to outlast it. Lease Caleqabiz to ATO, through Lespia, until the Valgish threat in the Antacean ceases.\""),
                next, when: naval);
            Swap(t, AlvarezVisit, name + "L", entry, new[] { parent }, Alvarez, S.All(
                "\"Lespia already holds Caleqabiz on your lease. Let ATO build an airfield there, under our flag, until the Valgish threat in the Antacean ceases.\""),
                next, when: lease);
        }

        Swap(t, AlvarezVisit, "Lease941", 941, new[] { 940 }, Alvarez, S.All(
            "\"Unless you turn something we already have into something ATO would really like. Our sailors are already at Caleqabiz, Your Majesty.\""),
            new[] { "#942" }, when: naval);
        // with the lease, there is nothing left to buy
        Swap(t, AlvarezVisit, "Lease941L", 941, new[] { 940 }, Alvarez, S.All(
            "\"Caleqabiz? No, we already have that for fifty years, and the Arcasians are already pouring concrete on it. I'm afraid Lespia has bought everything from you it needed this year.\""),
            new[] { "#686" }, when: lease);
        Swap(t, AlvarezVisit, "Lease875", 875, new[] { 847, 870 }, R, S.All("\"Deal. The naval rights become a lease.\""), new[] { "#877" }, when: naval);
        Swap(t, AlvarezVisit, "Lease875L", 875, new[] { 847, 870 }, R, S.All("\"Deal. ATO can build on Caleqabiz.\""), new[] { "#877" }, when: lease);
        CaleqabizPress();

        // "the Zille agreement" never existed in this war
        string knowsBomb = "(Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement_Rusty\"] == true)";
        string noBomb = "not " + knowsBomb;
        foreach ((string sfx, string w) in new[] { ("", knowsBomb), ("N", noBomb) })
        {
            bool k = sfx == "";
            Swap(t, AlvarezVisit, "Collapse575" + sfx, 575, new[] { 234 }, R, S.All(k ? "\"Your bomb started my war, Mr. Alvarez.\"" : "\"A bomb in Smolak Square started my war, Mr. Alvarez.\""), new[] { "#578" }, when: w);
            Swap(t, AlvarezVisit, "Collapse645" + sfx, 645, new[] { 657 }, R, S.All(k ? "\"Your bomb started my war, Mr. Alvarez.\"" : "\"A bomb in Smolak Square started my war, Mr. Alvarez.\""), new[] { "#648" }, when: w);
            Swap(t, AlvarezVisit, "Collapse495" + sfx, 495, new[] { 497 }, R, S.All(k
                ? "\"Please, Mr. Alvarez. Stop. Your people planted that bomb, Smolak used it as his excuse, and my soldiers paid for both of you in Zille.\""
                : "\"Please, Mr. Alvarez. Stop. Somebody's bomb gave Smolak his excuse, and my soldiers paid for it in Zille.\""), new[] { "#502" }, when: w);
            Swap(t, AlvarezVisit, "Hostage237" + sfx, 237, new[] { 93 }, R, S.All(k
                ? "\"Who's the enemy here? Your bomb gave President Smolak his excuse to keep Zille, and me my reason to take it.\""
                : "\"Who's the enemy here? A bomb in Smolak Square gave President Smolak his excuse to keep Zille, and me my reason to take it.\""), new[] { "#244" }, when: w);
        }

        // the war with Wehlen, in the room before business (it is over by now): what Lespia did in it and what it was paid
        string lever = "(Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0";
        string withdrawn = $"{V(SupplyWar.VarLespiaWithdrawn)} == true";
        string gasAny = $"{V(SupplyWar.VarLespiaGasDeal)} == true";
        string gas = $"{gasAny} and {V(SupplyWar.Extractions)} == true";   // the deal was signed and the field is producing
        string gasWait = $"{gasAny} and {V(SupplyWar.Extractions)} ~= true";
        string money = $"{V(SupplyWar.VarLespiaPaid)} == true";
        t.Line("War1Lib", Alvarez, "\"The king who took Zille back with tanks. My generals still talk about it, Your Majesty. Mostly about the bill.\"",
            new[] { "WarPaid" }, condition: And(War, $"not ({PalesWar})", Lib), hooks: Talk.From(63));
        t.Line("War1", Alvarez, "\"Your war with Wehlen has stopped, if not quite ended. The Strait of Kreleck is quiet again, and my admirals are bored.\"",
            new[] { "WarPaid" }, condition: And(War, $"not ({PalesWar})", $"not ({Lib})"), hooks: Talk.From(63));
        // Lespia keeps books: a broken Meftiem promise or an unanswered Cavette grievance sours every line that praises the deal
        string grievance = $"Variable[\"{SupplyWar.VarLespiaGrievance}\"] == true or Variable[\"RiziaDLC.MITZ_ScrewedOverLespia\"] == true";
        string thin = "Variable[\"ZilleWar.Lespia_StraitThin\"] == true";
        t.Logic("WarPaid", new[] { "WarGrievance", "WarOut", "WarLease", "WarNaval", "WarGas", "WarGasWait", "WarMoney", "WarMitz", "WarThin", "WarSpurned", "WarCold", "WarRefused", "WarNone", "WarRamiro" });
        t.Line("WarGrievance", Alvarez, "\"You gave my board the Meftiem in writing, Your Majesty, and then you gave Lespia nothing at Cavette. I have written that down too. Lespia will not forget it at the next vote.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", grievance));
        t.Line("WarOut", Alvarez, "\"My navy left the strait before your war was over. I know you have not forgotten. Neither has my parliament.\"",
            new[] { "WarReply" }, condition: And(lever, withdrawn));
        t.Line("WarLease", Alvarez, "\"Fifty years of Caleqabiz for a few months of my navy. My parliament thinks I robbed you, Your Majesty. I tell them it was a fair price for a king in a hurry.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({grievance})", lease));
        t.Line("WarNaval", Alvarez, "\"My sailors still refuel at Caleqabiz, you know. The fighting has stopped, and they show no sign of wanting to leave. Neither, to be honest, do I.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({grievance})", naval));
        t.Line("WarGas", Alvarez, "\"And the gas from Aureus arrives every month, at a price my parliament still cannot believe. Lespia remembers who keeps its lights on.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({grievance})", $"not ({lease})", $"not ({naval})", gas));
        t.Line("WarGasWait", Alvarez, "\"And the gas from Aureus has not come yet, you know. LesPower is a patient customer, Your Majesty. My parliament is less so.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({grievance})", $"not ({lease})", $"not ({naval})", gasWait));
        t.Line("WarMoney", Alvarez, "\"Your guilders paid for every shell my navy fired in the strait. Lespia is a democracy, Your Majesty. My parliament likes a war that pays its bills.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({lease})", $"not ({naval})", $"not ({grievance})", $"not ({gasAny})", money));
        t.Line("WarMitz", Alvarez, "\"And Lespia has your vote in Meftiem, for what my navy did in the strait. I have not forgotten. Neither will the MITZ board.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({lease})", $"not ({naval})", $"not ({gasAny})", $"not ({money})",
                $"not ({grievance})", $"not ({thin})", $"{V(SupplyWar.VarLespiaMitz)} == true"));
        t.Line("WarThin", Alvarez, "\"Lespia's navy still patrols the strait, Your Majesty, but it does so on our money.\"",
            new[] { "WarReply" }, condition: And(lever, $"not ({withdrawn})", $"not ({lease})", $"not ({naval})", $"not ({gasAny})", $"not ({money})", $"not ({grievance})", thin));
        // nothing bought from Lespia: Perla fuelled Smolak's tankers out of spite, or Alvarez keeps a grievance, or neither
        string fuel = $"{V(SupplyWar.VarLespiaFuel)} == true";
        t.Line("WarSpurned", Alvarez, "\"You turned down every offer I made you, Your Majesty, so Perla sold fuel to whoever paid. Smolak paid. Promptly, I might add.\"",
            new[] { "WarReply" }, condition: And($"not ({lever})", fuel));
        t.Line("WarCold", Alvarez, "\"Lespia stayed out of your war with Wehlen, Your Majesty. After the Meftiem, you could hardly have expected otherwise. I have written that down too.\"",
            new[] { "WarReply" }, condition: And($"not ({lever})", $"not ({fuel})", grievance, SupplyWar.LuaBrokeFaith));
        t.Line("WarRefused", Alvarez, "\"Lespia stayed out of your war with Wehlen, Your Majesty. You turned down every offer I made, and my parliament took it personally.\"",
            new[] { "WarReply" }, condition: And($"not ({lever})", $"not ({fuel})", grievance, $"not ({SupplyWar.LuaBrokeFaith})"));
        t.Line("WarNone", Alvarez, "\"Lespia stayed out of your war with Wehlen, Your Majesty. I like to think that was a favor too.\"",
            new[] { "WarReply" }, condition: And($"not ({lever})", $"not ({fuel})", $"not ({grievance})"));
        // the consulate in Zille (the security brief earlier this turn)
        t.Line("WarReply", R, "\"What is done is done, Mr. Alvarez. Let us talk about what comes next.\"", new[] { "WarRamiro" });
        t.Logic("WarRamiro", new[] { "RamiroShown", "RamiroSeized", "RamiroFreed", "#41" });
        // the King bought Alvarez's silence on Ramiro during the war, then showed Smolak the file at the treaty table (ZilleTreaty)
        t.Line("RamiroShown", Alvarez, "\"You showed Smolak my officer's file, Your Majesty. We had an arrangement. Lespia keeps its arrangements, and remembers who does not.\"",
            new[] { "RamiroSeized", "RamiroFreed", "#41" }, condition: $"{V(ZilleTreaty.VarRamiroShown)} == true");
        t.Line("RamiroSeized", Alvarez, "\"You stormed my consulate in Zille and put one of my diplomats in a cell. I am receiving you today only because my ministers insisted.\"",
            new[] { "#41" }, condition: $"{V(VarRamiro)} == \"seized\"", script: "Variable[\"RiziaDLCIsolated.Lespia_Alvarez_Hostile\"] = true;");
        t.Line("RamiroFreed", Alvarez, "\"And thank you for sending Mr. Ramiro home. He tells me your soldiers were very polite.\"",
            new[] { "#41" }, condition: $"{V(VarRamiro)} == \"released\"");
        t.Register();
    }

    /// <summary>
    /// Node 877's papers announce the island's lease as news, but Lespia has held Caleqabiz since the war (naval rights, or a
    /// fifty-year lease the papers reported then): the Geopolitico and Voice items say what changes now. Kyrute's and the CSP's
    /// reports still fit.
    /// </summary>
    private static void CaleqabizPress()
    {
        bool Leased() => Variables.GetBool(SupplyWar.VarLespiaLease);
        bool Held(string _) => Variables.GetBool(SupplyWar.VarLespiaLease) || Variables.GetBool(SupplyWar.VarLespiaCaleqabiz);
        string Pales() => Variables.GetBool(WarFlags.PalesWar)
            ? " In return, Lespia and ATO have reportedly withdrawn their support for Pales."
            : " In return, Lespia and ATO have reportedly offered seven billion Arcasian Liras.";
        string G() => NewsWire.P(
            Leased()
                ? "After King Romus' meeting with Prime Minister Alvarez, Rizia has agreed to let ATO build an airfield on Caleqabiz. The island has " +
                  "been in Lespian hands since the war over Zille, under the fifty-year lease Rizia signed for Lespia's help in the Strait of Kreleck."
                : "After King Romus' meeting with Prime Minister Alvarez, the naval rights Lespia was given on Caleqabiz during the war over Zille " +
                  "have become a lease. The island is to host ATO, with Lespia taking over its administration.",
            "The airfield is to stay \"until the Valgish threat in the Antacean ceases\", and no date is given." + Pales(),
            "Valgsland and the CSP have reacted with disgust. Chancellor Hegel said that Alvarez \"sees ghosts and is imagining a threat that does not exist\".");
        string GTitle(string _) => Leased() ? "ATO Comes to Lespian Caleqabiz" : "Lespia's War-Time Foothold on Caleqabiz Becomes a Lease";
        SweepEarlyKit.News("Turn08_G_AlvarezVisit_CaleqabizToLespiaWar", (_, _) => G(), GTitle, Held);
        SweepEarlyKit.News("Turn08_G_AlvarezVisit_CaleqabizToLespia", (_, _) => G(), GTitle, Held);
        SweepEarlyKit.News("Turn08_V_AlvarezVisit_CaleqabizToLespia", (_, _) => NewsWire.P(
                Leased()
                    ? "During the war in Zille, the King leased Caleqabiz to Lespia for fifty years to buy himself a navy. Now Lespia's tenants have " +
                      "invited their Arcasian friends to build an airfield on it. The island will be an Arcasian aircraft carrier in all but name."
                    : "During the war in Zille, the King let Lespian sailors into Caleqabiz as guests. Now the guests have the island on a lease with " +
                      "no end date, and an Arcasian airfield to go with it.",
                "The Zille deal was a disaster for our country. Caleqabiz is turning into its sequel.",
                "Deals with shady oligarch-run nations won't save you from the revolution, Romus."),
            _ => Leased() ? "Caleqabiz: Fifty Years Were Not Enough" : "Caleqabiz: A Repeat Of Zille?", Held);
    }

    // ------------------------------------------------------------------ c589 the council: foreign affairs

    private static void RegisterCouncil()
    {
        var t = new Talk(Council, "ZilleWar.Late.Council.");
        Swap(t, Council, "Smolak96", 96, new[] { 44, 82, 83, 84, 95 }, Laurento, new S(
            Ongoing: "\"President Smolak has agreed to talk, through the Sordish foreign ministry. It is not a negotiation, Your Majesty. Not yet. But it is a telephone line, and we have not had one since Konstantium.\"",
            Lib: "\"The Alliance will take up Zille in Konstantium. Smolak will want a resolution against us. We should want one that says nothing at all.\"",
            Part: "\"President Smolak will take the line to the Alliance, Your Majesty. He will ask for observers and a record of every violation, which tells me he wants Zille city back more than he wants to be seen asking for it.\"",
            Lost: "\"President Smolak will take Zille to the Alliance in Konstantium, Your Majesty, and he will not be gentle about it.\""),
            new[] { "R96a", "R96b", "R96c" });
        t.Choice("R96a", "\"I'll hear him out. Whatever he says, the Alliance will hear our answer.\"", new[] { "#118" });
        t.Choice("R96b", "\"Let him talk. Zille is not his to bargain with.\"", new[] { "#118" }, condition: $"not ({Lost})");
        t.Choice("R96c", "\"Then I will answer him, and I will not bow.\"", new[] { "#118" }, condition: Lost);
        t.Register();
    }

    // ------------------------------------------------------------------ c416 Rusty on the train

    private static void RegisterRusty()
    {
        var t = new Talk(Rusty, "ZilleWar.Late.Rusty.");
        var trip = new S(
            Ongoing: "I was scheduled for a brief stop in the border town before going up to the front, where the Zille campaign was being run from a requisitioned railway hotel.",
            Lib: "I was scheduled for a brief stop in the border town, on the road through Zille to Nanta and the strait beyond it.",
            Part: "I was scheduled for a brief stop in the border town, a few miles south of the armistice line.",
            PartBorder: "I was scheduled for a brief stop in the border town, a few miles south of the new border through Zille.",
            Lost: "I was scheduled for a brief stop in the border town, still full of refugees from Zille.");
        Swap(t, Rusty, "Trip4", 4, new[] { 10 }, Narr, trip, new[] { "#5" }, when: MaveOurs);
        Swap(t, Rusty, "Trip11", 11, new[] { 9 }, Narr, trip, new[] { "#5" }, when: MaveOurs);
        // Mave Nortus is on the Wehzek side of the armistice line (11 is the visit to the intelligence hub there)
        var tripHeld = new S(
            Lib: "I was scheduled for a brief stop at the last Rizian station before the old border, on the road through Zille to Nanta and the strait beyond it.",
            Part: "I was scheduled for a brief stop at the last Rizian station before the armistice line, where the Alliance observers kept their offices.",
            PartBorder: "I was scheduled for a brief stop at the last Rizian station before the new border, where the Alliance observers kept their offices.",
            Lost: "I was scheduled for a brief stop at the last Rizian station before the armistice line, which I was told not to cross.");
        Swap(t, Rusty, "Trip4H", 4, new[] { 10 }, Narr, tripHeld, new[] { "#5" }, when: MaveHeld);
        Swap(t, Rusty, "Trip11H", 11, new[] { 9 }, Narr, tripHeld, new[] { "#5" }, when: MaveHeld);
        var papers = new S(
            Ongoing: "Seated on a leather divan in my private salon carriage, I looked once more at the casualty lists from Zille. Names and numbers swam before my eyes.",
            Lib: "Seated on a leather divan in my private salon carriage, I looked once more at the garrison rosters for Zille. Names and numbers swam before my eyes.",
            Part: "Seated on a leather divan in my private salon carriage, I looked once more at a map of the armistice line. Names and numbers swam before my eyes.",
            PartBorder: "Seated on a leather divan in my private salon carriage, I looked once more at a map of the new border. Names and numbers swam before my eyes.");
        string signedLost = $"{V(ZilleTreaty.VarKind)} == \"recognized\" or {V(ZilleTreaty.VarKind)} == \"terms\"";
        var papersSigned = new S(Lost: "Seated on a leather divan in my private salon carriage, I looked once more at the terms I had signed in Wraclawitz. Letters and numbers swam before my eyes.");
        var papersRefused = new S(Lost: "Seated on a leather divan in my private salon carriage, I looked once more at the terms Smolak had put to me in Wraclawitz, the ones I would not sign. Letters and numbers swam before my eyes.");
        foreach ((string name, int entry, int parent) in new[] { ("Papers12", 12, 7), ("Papers13", 13, 6) })
        {
            Swap(t, Rusty, name, entry, new[] { parent }, Narr, papers, new[] { "#14" });
            Swap(t, Rusty, name + "S", entry, new[] { parent }, Narr, papersSigned, new[] { "#14" }, when: signedLost);
            Swap(t, Rusty, name + "R", entry, new[] { parent }, Narr, papersRefused, new[] { "#14" }, when: $"not ({signedLost})");
        }

        // Rusty's old mines in Auremina: ours (and maybe already promised to him in the security brief), or behind Wehzek lines
        string aur = Auremina;
        string gold = V(VarGold);
        Swap(t, Rusty, "MinesRrg", 109, new[] { 107 }, "Russello Montoro", new S(
            Lib: "\"Heard the Auremina concession came through. I owe you a drink, Your Majesty. Several. Don't let the army blow up any more of my shafts.\"",
            Part: "\"Heard the Auremina concession came through. I owe you a drink, Your Majesty. Several. Pity about the armistice line, half my geologists' maps are on the wrong side of it.\"",
            PartBorder: "\"Heard the Auremina concession came through. I owe you a drink, Your Majesty. Several. Pity about the new border, half my geologists' maps are on the wrong side of it.\""),
            new[] { "#140", "#141", "#149", "#153" }, when: And(aur, $"{gold} == \"rrg\""));
        Swap(t, Rusty, "MinesCrown", 109, new[] { 107 }, "Russello Montoro", S.All(
            "\"Heard the Crown kept Auremina's mines for itself. I won't pretend I'm not hurt, Your Majesty. I'll get over it. Eventually.\""),
            new[] { "MinesHurt", "#149", "#153" }, when: And(aur, $"{gold} == \"crown\"", $"not ({Pledged})"));
        Swap(t, Rusty, "MinesSazon", 109, new[] { 107 }, "Russello Montoro", S.All(
            "\"Heard House Sazon got Auremina's mines. A family of Brenas aristocrats running a gold mine. I give it a year before they call me.\""),
            new[] { "MinesHurt", "#149", "#153" }, when: And(aur, $"{gold} == \"sazon\"", $"not ({Pledged})"));
        // he paid six billion for first claim during the war, and got his money back instead of the mines
        Swap(t, Rusty, "MinesCrownP", 109, new[] { 107 }, "Russello Montoro", S.All(
            "\"Heard the Crown kept Auremina's mines after all. The Treasury sent my six billion back, I'll give you that. A man pays for first claim, Your Majesty. Not for a refund.\""),
            new[] { "MinesHurt", "#149", "#153" }, when: And(aur, $"{gold} == \"crown\"", Pledged));
        Swap(t, Rusty, "MinesSazonP", 109, new[] { 107 }, "Russello Montoro", S.All(
            "\"Heard House Sazon got the mines I paid for first claim on. I got my money back, at least. Brenas aristocrats running a gold mine. I give it a year.\""),
            new[] { "MinesHurt", "#149", "#153" }, when: And(aur, $"{gold} == \"sazon\"", Pledged));
        t.Choice("MinesHurt", "\"Rizian soldiers paid for those hills, Rusty.\"", new[] { "MinesHurt2" });
        t.Line("MinesHurt2", "Russello Montoro", "\"And Rizian Royal Gold paid for the soldiers' boots, but who's counting?\"", new[] { "#123" });
        Swap(t, Rusty, "Mines", 109, new[] { 107 }, "Russello Montoro", new S(
            Ongoing: "\"Heard you're off to the front? Bring me back a souvenir. Preferably a gold mine.\"",
            Lib: "\"Heard Auremina's ours again. Do me a favor and don't let the army blow up any more of my shafts.\"",
            Part: "\"Heard Auremina's ours again. Pity about the armistice line, half my geologists' maps are on the wrong side of it.\"",
            PartBorder: "\"Heard Auremina's ours again. Pity about the new border, half my geologists' maps are on the wrong side of it.\""),
            new[] { "#140", "#141", "#149", "#153" }, when: And(aur, $"{gold} ~= \"rrg\" and {gold} ~= \"crown\" and {gold} ~= \"sazon\""));
        Swap(t, Rusty, "MinesLost", 109, new[] { 107 }, "Russello Montoro", new S(
            Part: "\"Heard Smolak is still sitting on my old mines behind the line. Ask him nicely for them, would you? He's not using them.\"",
            Lost: "\"Heard Smolak's boys are still sitting on my old mines. Shame. I'd just ordered new pumps.\""),
            new[] { "MinesNo", "#149", "#153" }, when: And($"not ({aur})", $"not ({Pledged})"));
        // six billion paid for first claim on hills that stayed Wehzek
        Swap(t, Rusty, "MinesLostP", 109, new[] { 107 }, "Russello Montoro", new S(
            Part: "\"Heard Smolak is still sitting on my old mines behind the line. Six billion for first claim on hills I can see through binoculars. Ask him nicely, would you?\"",
            Lost: "\"Heard Smolak's boys are still sitting on my old mines. Six billion guilders for first claim on a Wehzek hillside. Next time I read the small print.\""),
            new[] { "MinesNo", "#149", "#153" }, when: And($"not ({aur})", Pledged));
        t.Choice("MinesNo", "\"Don't hold your breath.\"", new[] { "MinesNo2" });
        t.Line("MinesNo2", "Russello Montoro", "\"Never do. Bad for the complexion.\"", new[] { "#123" });
        Swap(t, Rusty, "Bye", 480, new[] { 478, 892 }, "Russello Montoro", new S(
            Ongoing: "\"Sorry to disturb you. And good luck at the front.\"",
            Lib: "\"Sorry to disturb you. And good luck with the Alliance in Konstantium.\"",
            Part: "\"Sorry to disturb you. And good luck with the Alliance in Konstantium.\"",
            Lost: "\"Sorry to disturb you. And good luck in Konstantium. You'll need it.\""), new[] { "#704" });
        t.Register();
    }

    // ------------------------------------------------------------------ c422 the House of Delegates

    private static void RegisterHod()
    {
        var t = new Talk(Hod, "ZilleWar.Late.Hod.");
        Swap(t, Hod, "Hugo27", 27, new[] { 26 }, Hugo, new S(
            Ongoing: "\"The war in Zille is the talk of the capital, Your Majesty. Not all of it kind.\"",
            Part: "\"Your talks with Smolak at Mave Nortus are still the talk of the capital, Your Majesty. Half of them call it statesmanship. The other half are Delegates.\"",
            Lost: "\"Your trip to Wraclawitz is still the talk of the capital, Your Majesty. I would keep my head high in there.\""),
            new[] { "#87", "#48", "#80", "#53" });

        // openings: Zille welcomed home by the sword, or the war mourned
        t.Line("OpenAdarfo", "Adarfo Sotelato", "\"We open this session by welcoming Zille home. Not by treaty alone, but on the backs of the miners and farmhands who fought for it.\"",
            new[] { "#141" }, condition: Lib, hooks: Talk.From(137));
        t.Line("OpenManus", "Manus Sazon", "\"We open this session by welcoming Zille home, returned to Rizia by the courage of our soldiers.\"",
            new[] { "#216" }, condition: Lib, hooks: Talk.From(212));
        t.Line("OpenDaria", "Daria de Rava", "\"We open this session by welcoming Zille home. Not by treaty alone, but by the sacrifice of our soldiers.\"",
            new[] { "#633" }, condition: Lib, hooks: Talk.From(617));
        Swap(t, Hod, "Adarfo136", 136, new[] { 135 }, "Adarfo Sotelato", new S(
            Ongoing: "\"We open this session by counting our dead in Zille. The list is long, and it is still growing.\"",
            Part: "\"We open this session by acknowledging a war that ended in an armistice, and a Zille cut in two.\"",
            PartBorder: "\"We open this session by acknowledging a war that ended in a treaty, and a Zille cut in two by a border.\"",
            Lost: "\"We open this session in mourning, for the soldiers who died in Zille and for the Zille they died for.\""), new[] { "#405" });
        Swap(t, Hod, "Manus488", 488, new[] { 211 }, "Manus Sazon", new S(
            Ongoing: "\"We open this session with a prayer for our soldiers in Zille, and for an end to the fighting.\"",
            Part: "\"We open this session acknowledging that the war in Zille has ended with only part of what was promised.\"",
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
                     ("Part", And(Part, $"not ({BorderTreaty})"), "The mood in the country was jubilant, though the armistice line through Zille lent solemn undertones to the celebrations."),
                     ("PartB", And(Part, BorderTreaty), "The mood in the country was jubilant, though the new border through Zille lent solemn undertones to the celebrations."),
                     ("Lost", Lost, "The mood in the country was jubilant, though the defeat in Zille hung over the celebrations like smoke."),
                 })
        {
            w.Line("Mood" + sfx, Narr, text, new[] { "#18" }, condition: state, hooks: Talk.From(13));
        }

        w.Register();

        var h = new Talk(HugoLeaves, "ZilleWar.Late.HugoLeaves.");
        Swap(h, HugoLeaves, "Return3", 3, new[] { 84 }, Narr, S.All("A few weeks after my last meeting with President Smolak, I was told that my uncle wanted to speak to me."), new[] { "#82" });
        h.Register();

        var c = new Talk(HugoConfronts, "ZilleWar.Late.HugoConfronts.");
        string ricoGone = "Variable[\"RiziaDLC.Rico_Extradited\"] == true";
        string ricoCell = "Variable[\"RiziaDLC.Rico_Arrested\"] == true and Variable[\"RiziaDLC.Rico_Extradited\"] ~= true";
        string pubbed = "(Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] == true or Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement_Rusty\"] == true)";
        Swap(c, HugoConfronts, "Rico36", 36, new[] { 11 }, R, S.All(
            "\"I will be honest with you, Uncle. Rico stays where he is. After everything Zille has cost us, I will not take chances with Su Omina's men.\""), new[] { "#37" },
            when: "not (" + ricoGone + ")");
        Swap(c, HugoConfronts, "Rico36X", 36, new[] { 11 }, R, S.All(
            "\"I will be honest with you, Uncle. Rico is in Wehlen's hands, and he went there for a treaty. After everything Zille has cost us, I would not take that back.\""), new[] { "#37" },
            when: ricoGone);
        // "I will order his release", "not sure he's innocent", "freed when I say": not while Wehlen holds him (the treaty's Rico clause)
        foreach (int e in new[] { 12, 13, 14 })
        {
            Vanilla.Require(HugoConfronts, e, $"not ({ricoGone})");
        }

        Swap(c, HugoConfronts, "Proof38", 38, new[] { 37 }, Hugo, S.All(
            "\"We have proven to the world that Lespia was the true culprit. You went to war over a bombing my son did not commit, and still you keep him in a cell.\""),
            new[] { "#39", "#25", "Affair40" }, when: pubbed + " and (" + ricoCell + ")");
        Swap(c, HugoConfronts, "Proof38B", 38, new[] { 37 }, Hugo, S.All(
            "\"We have proven to the world that Lespia was the true culprit. You went to war over a bombing my son did not commit.\""),
            new[] { "#39", "#25", "Affair40" }, when: pubbed + " and not (" + ricoCell + ")");
        Swap(c, HugoConfronts, "Proof38C", 38, new[] { 37 }, Hugo, S.All(
            "\"We have the proof that Lespia was the true culprit, Your Majesty. The Alliance will hear it, if you will not.\""),
            new[] { "#39", "#25", "Affair40" }, when: "not " + pubbed);
        c.Choice("Affair40", "\"The war is my affair, not yours. Do not attempt to intervene in it.\"", new[] { "#18" });
        c.Register();

        var n = new Talk(NightBefore, "ZilleWar.Late.Night.");
        Swap(n, NightBefore, "Lucita204", 204, new[] { 206 }, "Lucita Azaro", new S(
            Ongoing: "\"I was just reviewing the casualty lists from Zille.\"",
            Lib: "\"I was just reviewing the garrison rosters for Zille.\"",
            Part: "\"I was just reviewing the reports from the armistice line.\"",
            PartBorder: "\"I was just reviewing the reports from the new border.\"",
            Lost: "\"I was just reviewing what is left of our networks in Zille. Smolak's police have been busy.\""), new[] { "#213", "#215", "#218", "#220" });
        n.Register();
    }

    // ------------------------------------------------------------------ the Turn 8 meeting with Smolak (replaces c424)

    private static void RegisterSmolak()
    {
        var t = new Talk(SmolakConv, "ZilleWar.Late.Smolak8.");
        t.Logic("Start", new[] { "On1", "Lib1", "Part1", "Part1H", "Lost1" }, hooks: Talk.AtStart(SmolakConv),
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
        t.Line("Lib3", Smolak, "\"Your Majesty. You took my villa in Zille. I would like my khisha pipes back. They were a present from my son.\"", new[] { "Lib4" });
        t.Line("Lib4", Smolak, "\"I am not here to beg. Wehlen does not beg. I am here for my soldiers. You have thousands of them in your camps, eating your bread and learning your songs.\"", new[] { "Lib5" });
        t.Line("Lib5", Smolak, "\"I want them home. All of them, the officers too. In return, Wehlen will consider the matter of Zille closed. For this year.\"", new[] { "LibHub" });
        t.Logic("LibHub", new[] { "LibFree", "LibRansom", "LibHold" });
        t.Choice("LibFree", "\"They can go home, all of them. Let it be the first thing about this war that ends well.\"", new[] { "LibFree2" },
            script: Set(VarPrisoners, "\"released\""));
        t.Line("LibFree2", Smolak, "\"How very regal of you. I will tell their mothers it was my idea.\"", new[] { "Lib6" });
        t.Choice("LibRansom", "\"Your officers stay until Wehlen pays for what its garrison did to Zille.\"", new[] { "LibRansom2" }, cost: "+2 Budget",
            script: Set(VarPrisoners, "\"ransomed\"") + Add("RiziaDLC.Resources_Budget", 2) + Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1));
        t.Line("LibRansom2", Smolak, "\"Ransom! From a king! Fine. Six billion guilders for my officers, and not one more. Think of it as the rent your father was too polite to charge.\"", new[] { "Lib6" });
        t.Choice("LibHold", "\"Your prisoners go home when Wehlen recognizes the border at the strait.\"", new[] { "LibHold2" }, script: Set(VarPrisoners, "\"held\""));
        t.Line("LibHold2", Smolak, "\"Then they will grow old in Rizia. I hope your cooks are patient.\"", new[] { "Lib6" });
        t.Line("Lib6", Smolak, "\"One more thing. In Konstantium, I will ask the Alliance to condemn you. It is nothing personal. A man must be seen to try.\"", new[] { "Lib7" });
        t.Line("Lib7", Smolak, "\"And tell your mother I kept the garden. Mostly.\"", new[] { "Lib8" });
        t.Line("Lib8", Narr, "The ferry's horn sounded twice. By the time I reached the deck, his launch was already halfway to the Wehzek shore.", new[] { "End" });

        // divided: the armistice commission
        t.Line("Part1", Narr, "The armistice commission met in a village schoolhouse north of Mave Nortus, under an Alliance of Nations flag that had been ironed for the occasion. An observer from President Nines' office sat between the two delegations with a stopwatch.",
            new[] { "Part2" }, condition: And(Part, MaveOurs));
        t.Line("Part1H", Narr, "The armistice commission met in a village schoolhouse on the armistice line, within sight of Wehzek-held Mave Nortus. An Alliance of Nations flag, ironed for the occasion, hung over the door. An observer from President Nines' office sat between the two delegations with a stopwatch.",
            new[] { "Part2" }, condition: And(Part, MaveHeld));
        t.Line("Part2", Narr, "President Smolak had come in person. So had I. Neither of us had planned to, and neither of us would admit it.",
            new[] { "Part3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Part3", Smolak, "\"Your Majesty. Look at us. Two grown men and a schoolroom.\"", new[] { "Part4", "Part4A" });
        t.Line("Part4", Smolak, "\"Here is the problem. You sit in Zille city. I sit in the hills above it. My men can watch your men shaving in the morning. This cannot go on forever.\"",
            new[] { "Part4b", "Part5", "Part5A" }, condition: $"not ({Auremina})");
        t.Line("Part4A", Smolak, "\"Here is the problem. You sit in Zille city and in my gold hills. I sit in Isor and Nanta, and my gunboats watch your men shave on the beach. This cannot go on forever.\"",
            new[] { "Part4b", "Part5", "Part5A" }, condition: Auremina);
        // the King himself floated a vote in the House of Delegates (EarlyT7)
        t.Line("Part4b", Smolak, "\"And I read in your own newspapers that the King of Rizia told his House of Delegates a vote might be the only way this ends. You see? We agree already. Only you said it first.\"",
            new[] { "Part5", "Part5A" }, condition: WarFlags.L(EarlyT7.VarReferendumFloated));
        t.Line("Part5", Smolak, "\"So, my offer. You hand Zille city to the Alliance's observers, I keep the hills and the ports, and in five years the Zilleans vote. Very democratic. Very modern.\"",
            new[] { "RicoAsk", "PartHub" }, condition: $"not ({Auremina})");
        t.Line("Part5A", Smolak, "\"So, my offer. You hand Zille city and Auremina to the Alliance's observers, I keep Isor and Nanta, and in five years the Zilleans vote. Very democratic. Very modern.\"",
            new[] { "RicoAsk", "PartHub" }, condition: Auremina);
        t.Logic("PartHub", new[] { "PartLine", "PartRebuild", "PartSwap" });
        t.Choice("PartLine", "\"What Rizia holds stays Rizian. Draw the line where it stands.\"", new[] { "PartLine2" }, script: Set(VarTerms, "\"line\""));
        t.Line("PartLine2", Smolak, "\"Then it is a border with no name, and you will spend a fortune guarding it. So will I. How wonderful for the concrete industry.\"", new[] { "PartBye" });
        t.Choice("PartRebuild", "\"Rizia will rebuild the city it took. Wehlen can keep what it holds, for now.\"", new[] { "PartRebuild2" }, cost: "-2 Budget",
            script: Set(VarTerms, "\"rebuild\"") + Add("RiziaDLC.Resources_Budget", -2) + Add("RiziaDLC.Country_PublicOpinion", 1));
        t.Line("PartRebuild2", Smolak, "\"For now! He says for now. Write that down, observer.\"", new[] { "PartBye" });
        t.Choice("PartSwap", "\"Exchange the prisoners first. The rest can wait.\"", new[] { "PartSwap2" }, script: Set(VarPrisoners, "\"exchanged\""));
        t.Line("PartSwap2", Smolak, "\"Agreed. The rest can always wait. It is the one thing Zille has always been good at.\"", new[] { "PartBye" });
        t.Line("PartBye", Narr, "The observer stopped his watch and wrote down the time. Outside, a child was selling apples to the soldiers of both armies.", new[] { "End" });

        // the King accepted Smolak's full ceasefire terms on the telephone during the war (SupplyVoices): Ricardus was part of them
        string arrested = "Variable[\"RiziaDLC.Rico_Arrested\"] == true";
        t.Line("RicoAsk", Smolak, "\"And one small matter. On the telephone, the night our guns went quiet, you promised me your cousin Ricardus, to answer for Smolak Square. My police have been waiting at the station ever since. They are getting cold.\"",
            new[] { "RicoGive", "RicoKeep", "RicoFree" }, condition: $"{V(SupplyWar.VarCeasefireTerms)} == \"full\" and Variable[\"RiziaDLC.Rico_Extradited\"] ~= true");
        t.Choice("RicoGive", "\"A promise is a promise. Ricardus goes to Wraclawitz.\"", new[] { "RicoGive2" }, condition: arrested,
            script: Set("RiziaDLC.Rico_Extradited", "true") + Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", 1) +
                    "EnableCodexEntry(\"Characters_Family_RicoToras_Extradited\"); ");
        t.Line("RicoGive2", Smolak, "\"You see? A man of his word. My police will be so relieved. Now, where were we?\"", new[] { "RicoBack" });
        t.Choice("RicoKeep", "\"Ricardus stays in Rizia. That promise died with the armistice.\"", new[] { "RicoKeep2" }, condition: arrested,
            script: Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -1));
        t.Choice("RicoFree", "\"Ricardus is a free man in Rizia, and he will stay one.\"", new[] { "RicoKeep2" }, condition: $"not ({arrested})",
            script: Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -1));
        t.Line("RicoKeep2", Smolak, "\"Then the armistice has one more grievance in it. I collect grievances, Your Majesty. Ask anyone in Wraclawitz.\"", new[] { "RicoBack" });
        t.Logic("RicoBack", new[] { "RicoBackPart", "RicoBackLost" });
        t.Logic("RicoBackPart", new[] { "PartHub" }, condition: Part);
        t.Logic("RicoBackLost", new[] { "LostHub" }, condition: Lost);

        // lost: terms in Wraclawitz
        t.Line("Lost1", Narr, "The message from Wraclawitz had been phrased as an invitation. Laurento advised me to treat it as one, and to go anyway.", new[] { "Lost2" }, condition: Lost);
        t.Line("Lost2", Narr, "Smolak received me in the presidential palace, in a room hung with photographs of his army in Zille. He had chosen the chairs himself. Mine was lower.",
            new[] { "Lost3" }, script: "AddConversant(\"Wiktor Smolak (R)\");");
        t.Line("Lost3", Smolak, "\"Your Majesty! Welcome to Wehlen. I wanted you to see it for yourself. You were once so eager to visit.\"", new[] { "Lost4", "Lost4H" });
        t.Line("Lost4", Smolak, "\"I won't keep you. The war you started cost Wehlen dearly, and Wehlen expects to be paid. Nine billion guilders, in Rizian money, since you are so fond of it. Then, a border at Mave Nortus without a single Rizian gun within twenty kilometers of it.\"", new[] { "Lost5" }, condition: MaveOurs);
        t.Line("Lost4H", Smolak, "\"I won't keep you. The war you started cost Wehlen dearly, and Wehlen expects to be paid. Nine billion guilders, in Rizian money, since you are so fond of it. Until then, Mave Nortus stays where it is. My soldiers have grown fond of the place.\"",
            new[] { "Lost5" }, condition: MaveHeld);
        t.Line("Lost5", Smolak, "\"And a souvenir. Your mother's old house in the harbor district of Zille. My son has taken a liking to it.\"", new[] { "RicoAsk", "LostHub" });
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
        t.Line("LostBye", Narr, "On the drive to the airfield, Wraclawitz was hung with Wehzek flags. On the wall of the railway station, someone had chalked the names of the men who did not come home from Zille.", new[] { "End" });

        t.End();
        t.Register();
    }
}
