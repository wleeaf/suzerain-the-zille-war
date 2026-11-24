using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.LK;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Reactions at home: the people around the King answer what he did to them, or what his signature did to their province.
/// - General Carlos asks for an audience after the court-martial of the Zille commanders (or the royal commission of inquiry):
///   a short scene in the King's study at the first story step after the decree's turn, never next to a Pales war table.
/// - Leona Sazon, at the Turn 10 gathering, names the Brenas towns still under the Wehzek flag; a lasting House Sazon -1 row
///   runs from the war's end for as long as any of them is occupied.
/// - The Queen Mother answers Wehzek Zille recognized on paper (the Terms of Wraclawitz, or Smolak's Turn 9 call): at the royal
///   wedding when the treaty did it, at the Turn 10 gathering when the call did.
/// - Vanilla's Caleqabiz lines (Romus to Leona at Turn 10, Beatrice at the Turn 9 tea) learn the war-time Lespian lease when
///   the ATO airfield was refused at Turn 8.
/// - The War Council minutes after the King refused Smolak's ceasefire: the lines and conditions are here, for WarCouncil.
/// </summary>
internal static class ReactionsHome
{
    // ------------------------------------------------------------------ variables

    /// <summary>General Carlos's audience was offered (latch).</summary>
    public const string VarCarlosAudience = "ZilleWar.Late_CarlosAudience";
    /// <summary>The King told Carlos to defend his officers in open court (for later army lines).</summary>
    public const string VarCarlosDefended = "ZilleWar.Late_CarlosDefended";
    /// <summary>The King told Carlos the court tries the men who lost the battles: House Azaro -1 while the court-martial stands.</summary>
    public const string VarCarlosCourt = "ZilleWar.Late_CarlosCourt";
    /// <summary>The Queen Mother has spoken about Wehzek Zille recognized on paper (latch).</summary>
    public const string VarEstelaSaid = "ZilleWar.Late_EstelaRecognitionSaid";

    private const string DecCourt = "ZilleWar.Decree_CourtMartial";
    private const string DecInquiry = "ZilleWar.Decree_Inquiry";
    private const string Authority = "RiziaDLC.Resources_Authority";

    // ------------------------------------------------------------------ conversations and speakers

    private const string CarlosConv = "ZilleWar/Late/CarlosAudience";
    private const string CarlosFrag = "ZilleWar.Late_CarlosAudienceScene";
    private const string SazonT10 = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE SAZON CONVO";
    private const string Gathering = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT GATHERING SCENE Conversation";
    private const string Wedding = "Rizia/Turn 8/A_RoyalWedding";
    private const string Tea = "Rizia/Turn 9/FPnT_GRACERumburgMeeting";

    private const string Carlos = "Carlos Robles Azaro";
    private const string Lucita = "Lucita Azaro";
    private const string Leona = "Leona Sazon";
    private const string Estela = "Estela Toras";
    private const string Narr = Talk.Narrator;

    // ------------------------------------------------------------------ the War Council after a refusal (read by WarCouncil)

    /// <summary>Lua: the King refused Smolak's ceasefire on the telephone this very turn (the call is tier 3, the council tier 4).</summary>
    public static readonly string LRefusedToday =
        $"Variable[\"{SupplyWar.VarSmolakAnswer}\"] == \"refused\" and (Variable[\"ZilleWar.Smolak_LastTurn\"] or 0) == {SupplyWar.TurnLua}";

    /// <summary>Lua: a Zille city is in Rizian hands now.</summary>
    public static readonly string LTookCity =
        string.Join(" or ", AfterAction.ZilleCities.Select(c => $"(Variable[\"{ZilleOperation.CapturedVar(c)}\"] == true)"));

    /// <summary>Carlos, opening the council the day the King refused Smolak, when a Zille city is ours.</summary>
    public const string CouncilRefusalTook =
        "\"Wraclawitz offered terms today, I'm told, and you sent them back. The men will hear it by nightfall. They'll fight like men " +
        "whose King won't sell what they took.\"";

    /// <summary>The same, when no Zille city is ours yet.</summary>
    public const string CouncilRefusalNone =
        "\"Wraclawitz offered terms today, I'm told, and you sent them back. The men will hear it by nightfall. They'll fight like men " +
        "whose King won't bargain with Wraclawitz.\"";

    /// <summary>Laurento's answer to a ceasefire asked for in the same council (replaces "Smolak will refuse the first offer").</summary>
    public const string LaurentoAfterRefusal =
        "\"He offered terms today, Your Majesty, and you refused them. I'll speak to the Secretary-General tonight, but if you ask now, " +
        "he will name a higher price.\"";

    // ------------------------------------------------------------------ registration

    public static void Register()
    {
        foreach (string v in new[] { VarCarlosAudience, VarCarlosDefended, VarCarlosCourt, VarEstelaSaid })
        {
            Variables.Register(v);
        }

        // House standing is rebuilt from vanilla's recompute after every fragment: these are lasting rows, not one-off changes.
        Vanilla.Expose(VarCarlosCourt);
        Vanilla.Expose(DecCourt);
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{VarCarlosCourt} == true && {DecCourt} == true", "RiziaDLC.Country_HouseAzaro -= 1;");
        // Brenas towns still under the Wehzek flag once the fighting has stopped (a treaty that returns one cedes it back)
        string anyOccupied = string.Join(" || ", WarFlags.RizianTowns.Select(t => $"{WarFlags.Occupied(t)} == true"));
        WarDecrees.AddRow("HouseOpinions_DialoguesTwelve", $"{WarFlags.Over} == true && ({anyOccupied})", "RiziaDLC.Country_HouseSazon -= 1;");

        ConversationRegistry.RegisterConversation(CarlosConv);
        RegisterCarlos();
        RegisterSazon();
        RegisterEstela();
        RegisterTea();

        Events.OnEvaluateStep += (_, _) => OnStep();
    }

    private static void OnStep()
    {
        try
        {
            if (GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
            {
                OfferCarlos();
            }
        }
        catch (Exception ex)
        {
            Core.Log.Error($"ReactionsHome step failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ General Carlos and the court-martial

    /// <summary>A vanilla Pales war table is pending in this step (its fragments are named WarFragment_*; the Zille war is over).</summary>
    private static bool PalesTablePending()
    {
        var names = Managers.Instance?.GameFlowManager?.enabledNotDoneStoryFragmentsNames;
        if (names == null)
        {
            return false;
        }

        foreach (string n in names)
        {
            if (n != null && n.StartsWith("WarFragment", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void OfferCarlos()
    {
        if (!LateState.Declared || !LateState.Over || LateState.Liberated || Variables.GetBool(VarCarlosAudience))
        {
            return;
        }

        bool court = Variables.GetBool(DecCourt), inquiry = Variables.GetBool(DecInquiry);
        if (!court && !inquiry)
        {
            return;
        }

        int turn = LateState.Turn;
        int decreeTurn = Variables.GetInt((court ? DecCourt : DecInquiry) + "_Turn");
        if (turn <= decreeTurn || turn > 10 || Variables.GetBool("RiziaDLC.Carlos_Arrested") || Variables.GetBool("RiziaDLC.Carlos_Deranked") ||
            PalesTablePending() || GameState.StoryFragmentExistsInCurrentStep(CarlosFrag))
        {
            return;
        }

        try
        {
            GameState.AddCustomConversation(CarlosConv);
            if (GameState.AddCustomStoryFragment(new CustomConversationFragment(CarlosFrag, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                    "An Audience with General Carlos", "Palas Rezna, Porte Drazon", ConversationData.ConversationType.SecurityAndOrder, CarlosConv)))
            {
                Variables.Set(VarCarlosAudience, true);
            }
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding General Carlos's audience failed: {ex}");
        }
    }

    private static void RegisterCarlos()
    {
        var t = new Talk(CarlosConv, "ZilleWar.RH.Carlos.");
        string court = $"{V(DecCourt)} == true";
        string inquiry = $"{V(DecCourt)} ~= true and {V(DecInquiry)} == true";
        string palesFiring = $"{PalesWar} and Variable[\"RiziaDLC.War_Pales_Won\"] ~= true and Variable[\"RiziaDLC.War_Pales_Loss\"] ~= true";
        t.Logic("Start", new[] { "C1", "I1" }, hooks: Talk.AtStart(CarlosConv),
            script: $"AddConversant(\"{Carlos}\"); if {court} and ({CouncilLucita}) then AddConversant(\"{Lucita}\") end; PlaySceneMusic(Security);");

        // the court-martial
        t.Line("C1", Narr, "General Carlos asked to see me in my study. He came in full uniform, with the charge sheet folded under his arm, " +
                           "and did not take the chair I offered him.", new[] { "C2" }, condition: court);
        t.Line("C2", Carlos, "\"The charge sheet names six of my officers, Your Majesty. Every order they carried bore my signature, and some bore yours.\"",
            new[] { "C3", "C4", "Menu" });
        t.Line("C3", Carlos, "\"And you are taking them from their regiments while the Duke's guns are still firing.\"", new[] { "C4", "Menu" },
            condition: palesFiring);
        t.Line("C4", Lucita, "\"Half of Montaklar has a son or a cousin in that army, Your Majesty. They are asking me who will be next.\"",
            new[] { "Menu" }, condition: CouncilLucita);
        t.Logic("Menu", new[] { "Court", "Defend" });
        t.Choice("Court", "\"The court tries the men who lost the battles, General. Not you, and not me.\"", new[] { "Court2" },
            cost: "-1 House Azaro While the Court-Martial Stands", script: Set(VarCarlosCourt, "true"));
        t.Line("Court2", Carlos, "\"Then I'll sit in the front row every day of it.\"", new[] { "Court3" });
        t.Line("Court3", Narr, "He saluted and left. The charge sheet stayed on my desk, where he had put it down.", new[] { "End" });
        t.Choice("Defend", "\"Defend them yourself, in open court. I'll hear every word.\"", new[] { "Defend2" }, cost: "-1 Authority",
            script: Add(Authority, -1) + Set(VarCarlosDefended, "true"));
        t.Line("Defend2", Carlos, "\"I will, Your Majesty. And when I read out the orders, I'll read all of them.\"", new[] { "Defend3" });
        t.Line("Defend3", Narr, "He saluted, picked up the charge sheet and left without another word.", new[] { "End" });

        // the royal commission of inquiry
        t.Line("I1", Narr, "General Carlos asked to see me in my study. He had brought his own copies of every order he gave in Zille, " +
                           "bound in three volumes, and set them down on my desk.", new[] { "I2" }, condition: inquiry);
        t.Line("I2", Carlos, "\"A commission. Good. Let them read my orders, and yours, and decide.\"", new[] { "I3" });
        t.Line("I3", Narr, "He saluted and left the volumes where they were.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ House Sazon at the Turn 10 gathering

    private static void RegisterSazon()
    {
        var s = new Talk(SazonT10, "ZilleWar.RH.Sazon10.");

        // Leona, right after the greeting (#18 follows every opening): the Brenas towns Wehlen still holds, by name
        string[] towns = WarFlags.RizianTowns;
        for (int mask = 1; mask < 1 << towns.Length; mask++)
        {
            var held = new List<string>();
            var conds = new List<string> { WarFlags.LDeclared, WarFlags.L(WarFlags.Over) };
            for (int i = 0; i < towns.Length; i++)
            {
                bool on = (mask & (1 << i)) != 0;
                if (on)
                {
                    held.Add(towns[i]);
                }

                conds.Add(on ? WarFlags.L(WarFlags.Occupied(towns[i])) : WarFlags.LNot(WarFlags.Occupied(towns[i])));
            }

            string list = ZilleCampaign.JoinNames(held);
            s.Line($"Towns{mask}", Leona,
                $"\"{list} still {(held.Count > 1 ? "fly" : "flies")} Smolak's flag, Your Majesty. Brenas sent its sons north to win back Zille, " +
                "and lost ground of its own doing it.\"",
                new[] { "TownsSwear" }, condition: And(conds.ToArray()), hooks: Talk.From(18));
        }

        s.Choice("TownsSwear", "\"Brenas will have every inch of it back. I swear it on my father's grave.\"", new[] { "TownsSwear2" });
        s.Line("TownsSwear2", Leona, "\"Your father swore things about Zille too.\"", new[] { "#19", "#20" });

        // After Angelica's pardon: the island is Lespia's on the war-time lease, though the ATO airfield was refused at Turn 8.
        s.Choice("Caleq1234", "\"I'm sure Alvarez's sailors were getting sick of a disgraced Rizian noble on their pier, anyway.\"", new[] { "#1235" },
            condition: LeaseNoAto, hooks: Talk.From(1233));
        s.Register();
    }

    /// <summary>Lua: Lespia holds Caleqabiz on the fifty-year war lease, and vanilla's own ATO lease (Turn 8) was refused. The naval
    /// rights alone were given for the war and lapsed with it, so they do not count.</summary>
    private static string LeaseNoAto => And(WarFlags.LDeclared, $"{V(SupplyWar.VarLespiaLease)} == true", "Variable[\"ZilleWar.Lespia_LeaseRevoked\"] ~= true",
        "Variable[\"RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO\"] ~= true");

    // ------------------------------------------------------------------ Beatrice at the Turn 9 tea

    private static void RegisterTea()
    {
        var t = new Talk(Tea, "ZilleWar.RH.Tea.");
        // "Just like they have pounced on Caleqabiz..." (#533): vanilla only knows its own Turn 8 leases
        t.Logic("Caleq533", new[] { "#533" }, condition: LeaseNoAto, hooks: Talk.From(530));
        t.Register();
    }

    // ------------------------------------------------------------------ the Queen Mother and Wehzek Zille on paper

    private static void RegisterEstela()
    {
        string due = And(WarFlags.LDeclared, WarFlags.L(LateT9.VarRecognized), WarFlags.LNot(VarEstelaSaid));
        string home = WarFlags.L(LateT8.VarEstelaHome);
        const string signed = "\"You signed it, darling. Wehzek Zille, on paper, with no expiry date. My father never signed anything about Zille " +
                              "in his life, and I grew up three streets from that harbor.\"";
        const string signedHome = "\"You signed it, darling. Wehzek Zille, on paper, with no expiry date, and my father's house thrown in. I grew up " +
                                  "in that house, three streets from the harbor.\"";
        const string promise = "\"Don't promise me, darling. Promise the boys who'll have to do it.\"";

        // Recognized at the treaty (the Terms of Wraclawitz, Turns 5 to 7): at the royal wedding, as the family takes the head table.
        var w = new Talk(Wedding, "ZilleWar.RH.Wedding.");
        w.Line("Rec0", Narr, "Before I could sit down, my mother caught my sleeve and leaned close.", new[] { "Rec1", "Rec1H" },
            condition: due, script: Set(VarEstelaSaid, "true"), hooks: Talk.From(127));
        w.Line("Rec1", Estela, signed, new[] { "War", "Back" }, condition: $"not ({home})");
        w.Line("Rec1H", Estela, signedHome, new[] { "War", "Back" }, condition: home);
        w.Choice("War", "\"It was that or another war, Mother.\"", new[] { "War2" }, script: Add("RiziaDLC.Relations_Estela", -1));
        w.Line("War2", Narr, "She let go of my sleeve and sat down without another word.", new[] { "#128", "#129" });
        w.Choice("Back", "\"I'll get it back. Not this year.\"", new[] { "Back2" });
        w.Line("Back2", Estela, promise, new[] { "#128", "#129" });
        w.Register();

        // Recognized on Smolak's Turn 9 call: at the end of the Turn 10 gathering, over her cigarette.
        var g = new Talk(Gathering, "ZilleWar.RH.Gather10.");
        g.Line("Rec1", Estela, signed, new[] { "War", "Back" }, condition: And(due, $"not ({home})"), script: Set(VarEstelaSaid, "true"),
            hooks: Talk.From(293, 299));
        g.Line("Rec1H", Estela, signedHome, new[] { "War", "Back" }, condition: And(due, home), script: Set(VarEstelaSaid, "true"),
            hooks: Talk.From(293, 299));
        g.Choice("War", "\"It was that or another war, Mother.\"", new[] { "War2" }, script: Add("RiziaDLC.Relations_Estela", -1));
        g.Line("War2", Narr, "She drew on her cigarette and looked away, toward the window.", new[] { "#361" });
        g.Choice("Back", "\"I'll get it back. Not this year.\"", new[] { "Back2" });
        g.Line("Back2", Estela, promise, new[] { "#361" });
        g.Register();
    }
}
