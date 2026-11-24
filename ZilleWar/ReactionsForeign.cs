using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Foreign leaders react when a deal's premise changes, or when the King does something big to their people. Every reaction is
/// war-only (its trigger is war state) and complements what SupplyAlvarez, ZilleTreaty, SupplyVoices, LateT8/T9 and
/// EarlyT7Diplomacy already say. Nodes that continue another module's dialogue are named "RF_*" inside that module's own
/// prefix, so the owner links them with a one-word change to a next list (see the branchmap requests_foreign.md).
/// - AlvarezRentAfterRecall: the war-time and post-war re-pricing itself is SupplyAlvarez's (LiftOpen, MitzLift, the AlvarezRent
///   call). Here only the reverse: the King's warships go back to the Aureus drillship after Lespia started paying rent, and at
///   the Turn 8 visit Alvarez stops paying it.
/// - AlvarezSurchargeLifted: a price that was dear for both reasons (the Meftiem and Aureus): the final vote forgives only the
///   Meftiem part, and Alvarez says so when he asks for the vote (with or without a strait deal).
/// - CaleqabizAfterWithdrawal: Lespia's navy leaves the strait, its fifty-year lease of Caleqabiz stands; the King may revoke
///   it, ask for rent, or keep the contract. At the confrontation call, at the cold call after the files are published, or at
///   the Turn 8 visit (whichever comes first).
/// - SmolakInterneesAtTreaty: Smolak asks for the Wehzeks held behind Rizian wire (the internment decree, or the men screened
///   off the boats at Sallabes), or thanks the King for the protection decree.
/// - LegionFamilies: Smolak names the families of the Zillean Legion in the Wehzek-held cities; an amnesty in the treaty, or
///   the Voice of Rizia the next turn.
/// - HegelIsorGuaranteeBroken: the King guaranteed Hegel the safety of the miners of Isor, then shelled Isor, interned
///   Wehzek miners, or made Isor's teachers swear the oath. Hegel says so at his Turn 6 call, or at the Turn 9 side talk.
/// - SmolakOnZilleWehzeks: at the Turn 9 call after a liberation, Smolak reads how Rizia governs its new Wehzeks, and the King
///   may answer with the schools, the amnesty, or by lifting martial law (Late_NantaWarning "trust").
/// - BeatriceEndDateRefused: Beatrice asked for an end date; Smolak offered one and the King refused it.
/// </summary>
internal static class ReactionsForeign
{
    // ---- speakers
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Laurento = "Laurento Esquibel";
    private const string Smolak = "Wiktor Smolak (R)";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Narr = Talk.Narrator;
    private const string R = Talk.Romus;

    // ---- conversations (the mod's own calls are private constants of their modules)
    private const string AlvarezConv = "ZilleWar/Calls/Alvarez";
    private const string HegelConv = "ZilleWar/Calls/Hegel";
    private const string SmolakT9Conv = "ZilleWar/Late/SmolakT9";
    private const string AlvarezVisit = "Rizia/Turn 8/FPnT_AlvarezVisit";
    private const string RumburgMeeting = "Rizia/Turn 7/FPnT_RumburgMeeting";
    private const string HegelSide = "Rizia/Turn 9/Hegel Side Convo";

    // ---- vanilla and other modules' state
    private const string Budget = "RiziaDLC.Resources_Budget";
    private const string Authority = "RiziaDLC.Resources_Authority";
    private const string Opinion = "RiziaDLC.Country_PublicOpinion";
    private const string Democratic = "RiziaDLC.LeaningValue_Democratic";
    private const string ValgslandCable = "ZilleWar.Valgsland_CableAnswer";
    private const string ValgslandStageTurn = "ZilleWar.Valgsland_StageTurn";
    private const string SmolakAnswer = "ZilleWar.Smolak_CeasefireAnswer";
    private const string RumburgWantsCeasefire = "ZilleWar.Rumburg_WantsCeasefire";
    private const string Decree = "ZilleWar.Decree_";
    private static string Dec(string key) => Decree + key;
    private static string DecTurn(string key) => Decree + key + "_Turn";

    // ---- this module's state (registered: saved with the game, cleared by a new game)
    /// <summary>Alvarez stopped the Caleqabiz rent at the Turn 8 visit because the warships went back to Aureus.</summary>
    public const string VarRentWithdrawn = "ZilleWar.Lespia_RentWithdrawn";
    /// <summary>The King answered for the Caleqabiz lease after Lespia left the strait: "revoked" | "rent" | "kept".</summary>
    public const string VarIslandSettled = "ZilleWar.Lespia_IslandSettled";
    /// <summary>The King revoked the Caleqabiz lease after Lespia left the strait (lease and naval rights are false).</summary>
    public const string VarLeaseRevoked = "ZilleWar.Lespia_LeaseRevoked";
    /// <summary>The Wehzeks interned (decree) or screened (Sallabes) at the treaty table: "released" | "kept".</summary>
    public const string VarInterneesAtTable = "ZilleWar.Late_InterneesAtTable";
    private const string VarInterneesNews = "ZilleWar.Late_InterneesNews";
    private const string VarRevokedNews = "ZilleWar.Lespia_LeaseRevokedNews";
    /// <summary>The Zillean Legion's families at the treaty table: "amnesty" | "refused".</summary>
    public const string VarLegionFamilies = "ZilleWar.Late_LegionFamilies";
    private const string VarLegionTurn = "ZilleWar.Late_LegionFamilies_Turn";
    private const string VarLegionNews = "ZilleWar.Late_LegionNews";
    /// <summary>Hegel has named the broken Isor guarantee (once).</summary>
    public const string VarGuaranteeBroken = "ZilleWar.Valgsland_GuaranteeBroken";
    /// <summary>How the guarantee was broken, kept current for the Lua conditions: "" | "barrage" | "intern" | "oath".</summary>
    private const string VarBreach = "ZilleWar.Valgsland_Breach";
    /// <summary>Whether the internment decree was already in force when the guarantee was given: "yes" | "no".</summary>
    private const string VarInternAtGuarantee = "ZilleWar.Valgsland_InternAtGuarantee";
    /// <summary>A phase whose aim held Isor was opened with the preparatory barrage, and the turn of it.</summary>
    public const string VarBarrageIsor = "ZilleWar.Barrage_Isor";
    private const string VarBarrageIsorTurn = "ZilleWar.Barrage_Isor_Turn";
    private const string VarBarrageChecked = "ZilleWar.Barrage_CheckedPhase";
    /// <summary>The King lifted martial law in Zille on the Turn 9 call to answer Smolak.</summary>
    public const string VarMartialLifted = "ZilleWar.Late_MartialLifted";

    private static readonly string[] Registered =
    {
        VarRentWithdrawn, VarIslandSettled, VarLeaseRevoked, VarInterneesAtTable, VarInterneesNews, VarRevokedNews, VarLegionFamilies, VarLegionTurn,
        VarLegionNews, VarGuaranteeBroken, VarBreach, VarInternAtGuarantee, VarBarrageIsor, VarBarrageIsorTurn, VarBarrageChecked,
        VarMartialLifted, ZilleTreaty.Term("LegionAmnesty"),
    };

    // ---- Lua shortcuts
    private static string V(string v) => Talk.V(v);
    private static string Is(string v) => $"({V(v)} == true)";
    private static string Not(string v) => $"({V(v)} ~= true)";
    private static string Eq(string v, string s) => $"({V(v)} == \"{s}\")";
    private static string N(string v) => $"({V(v)} or 0)";
    private static string Inc(string v, int d) => Talk.Add(v, d);
    private static string T(string v) => $"{V(v)} = true; ";
    private static string F(string v) => $"{V(v)} = false; ";
    private static string S(string v, string s) => $"{V(v)} = \"{s}\"; ";
    private static string And(params string[] c) => LK.And(c);

    public static void Register()
    {
        foreach (string v in Registered)
        {
            Variables.Register(v);
        }

        RegisterAlvarezCall();
        RegisterAlvarezVisit();
        RegisterTreaty();
        RegisterHegel();
        RegisterSmolakT9();
        RegisterBeatrice();
        Events.OnEvaluateStep += (_, _) => OnStep();
    }

    // ================================================================== Alvarez: the Caleqabiz lease and the surcharge

    // the lease as SupplyAlvarez keeps it
    private static readonly string Lease = Is(SupplyWar.VarLespiaLease);
    private static readonly string Rentless = $"({Lease} and {N(WorldModifiers.CaleqabizRent)} == 0)";
    private static readonly string Rented = $"({Lease} and {N(WorldModifiers.CaleqabizRent)} >= 1)";
    private static readonly string Withdrawn = Is(SupplyWar.VarLespiaWithdrawn);
    private static readonly string Settled = $"({V(VarIslandSettled)} ~= nil and {V(VarIslandSettled)} ~= \"\")";
    /// <summary>Lespia left the strait and still holds the island on its lease; the King has not answered for it yet.</summary>
    private static readonly string IslandOpen = $"({Lease} and {Withdrawn} and not {Settled})";

    /// <summary>
    /// The King's answer once Lespia's navy has left the strait and its flag stays on Caleqabiz. <paramref name="aside"/> is
    /// Laurento's line (on the telephone) or Alvarez's (in person); the replies continue to <paramref name="exit"/>.
    /// </summary>
    private static void Island(Talk t, string p, string asideSpeaker, string aside, string exit)
    {
        t.Line(p, asideSpeaker, aside, new[] { p + "Revoke", p + "RevokeR", p + "Rent", p + "Keep" }, condition: IslandOpen);
        const string revoke = "\"The lease paid for your navy. Your navy has gone home, and so does your flag.\"";
        string revokeScript = F(SupplyWar.VarLespiaLease) + F(SupplyWar.VarLespiaCaleqabiz) + $"{V(WorldModifiers.CaleqabizRent)} = 0; " +
                              Inc(SupplyWar.NpLespia, -2) + T(VarLeaseRevoked) + S(VarIslandSettled, "revoked");
        t.Choice(p + "Revoke", revoke, new[] { p + "Revoke2" }, condition: Rentless, script: revokeScript,
            cost: "Ends the Caleqabiz Lease, Ends -1 Authority Per Turn");
        t.Choice(p + "RevokeR", revoke, new[] { p + "Revoke2" }, condition: Rented, script: revokeScript,
            cost: "Ends the Caleqabiz Lease and Its Rent, Ends -1 Authority Per Turn");
        t.Line(p + "Revoke2", Alvarez,
            "\"Tear up a signed lease? As you wish. My sailors will be home by Sunday, and my parliament will remember this longer than " +
            "you will, Your Majesty.\"", new[] { exit });
        t.Choice(p + "Rent", "\"Then pay rent for it, like any tenant.\"", new[] { p + "Rent2" }, condition: Rentless,
            script: $"{V(WorldModifiers.CaleqabizRent)} = 1; " + Inc(SupplyWar.NpLespia, -1) + S(VarIslandSettled, "rent"),
            cost: "+1 Budget Per Turn");
        t.Line(p + "Rent2", Alvarez,
            "\"Rent, then. Three billion a turn, and not a guilder more. Lespia keeps its pier, and you keep your pride.\"", new[] { exit });
        t.Choice(p + "Keep", "\"A contract is a contract. Keep it.\"", new[] { p + "Keep2" }, script: S(VarIslandSettled, "kept"));
        t.Line(p + "Keep2", Alvarez, "\"Spoken like a banker, Your Majesty.\"", new[] { exit });
    }

    private static void RegisterAlvarezCall()
    {
        // Same prefix as SupplyAlvarez, so its nodes and ours link by name (ours all start with RF_).
        var t = new Talk(AlvarezConv, "ZilleWar.Alvarez.");

        // ---- CaleqabizAfterWithdrawal on the telephone: after CSendAwayL2 (he says his sailors stay) or OutLease (the files)
        const string covered = "Laurento covered the mouthpiece. \"The lease paid for ships in the strait, Your Majesty. There are no " +
                               "ships in the strait.\"";
        Island(t, "RF_IslandC", Laurento, covered, "CEnd");
        Island(t, "RF_IslandO", Laurento, covered, "OutEnd");
        // the owner's link: CSendAwayL2 -> RF_IslandCGo, OutLease -> RF_IslandOGo (each falls through when already settled)
        t.Logic("RF_IslandCGo", new[] { "RF_IslandC", "CEnd" });
        t.Logic("RF_IslandOGo", new[] { "RF_IslandO", "OutEnd" });

        // ---- AlvarezSurchargeLifted, the "both" case: the vote forgives the Meftiem part only, while the warships stay at Aureus
        string grudge = $"({SupplyWar.LuaBrokeFaith} and {Not(SupplyWar.MitzPromiseLate)})";
        string blockade = Is(SupplyWar.BlockDrillship);
        string deal = $"({N(SupplyWar.Count(SupplyWar.Lever.Lespia))} > 0)";
        string surcharged = $"({Rentless} or {N(WorldModifiers.LespiaEscort)} == -4)";
        t.Line("RF_MitzBothDeal", Alvarez,
            "\"Give me your word, and I'll forgive the Meftiem part of my surcharge. The Aureus part stays until your warships leave my " +
            "partner's drillship.\"", new[] { "MitzMenu" },
            condition: And(grudge, blockade, surcharged, deal, Not(SupplyWar.VarSurchargeLifted)));
        t.Line("RF_MitzBothNoDeal", Alvarez,
            "\"Give me your word, and I'll forget what the Meftiem added to my price for the strait. What Aureus adds stays until your " +
            "warships leave my partner's drillship.\"", new[] { "MitzMenu" },
            condition: And(grudge, blockade, $"not {deal}"));
        t.Register();
    }

    private static void RegisterAlvarezVisit()
    {
        // Same prefix as LateT8's Alvarez visit.
        var t = new Talk(AlvarezVisit, "ZilleWar.Late.Alvarez.");

        // ---- AlvarezRentAfterRecall, the reverse premise: the rent began because the warships left Aureus; they are back
        string dearForShips = $"({Eq(SupplyWar.VarDearFor, "blockade")} or {Eq(SupplyWar.VarDearFor, "both")})";
        t.Line("RF_Back", Alvarez,
            "\"Your warships are back at my partner's drillship at Aureus, I see. So is my old price. Lespia stops paying rent on " +
            "Caleqabiz from this month.\"", new[] { "WarPaid" },
            condition: And(LK.War, Is(SupplyWar.VarSurchargeLifted), Rented, dearForShips, Is(SupplyWar.BlockDrillship), $"not {Withdrawn}",
                Not(VarRentWithdrawn)),
            script: $"{V(WorldModifiers.CaleqabizRent)} = 0; " + T(VarRentWithdrawn));

        // ---- CaleqabizAfterWithdrawal in person (the files were published, so Alvarez never rang again): after WarOut
        Island(t, "RF_IslandV", Alvarez,
            "\"The island, of course, is a separate contract. Fifty years, signed and stamped. Lespia keeps its pier on Caleqabiz.\"",
            "WarRamiro");
        t.Register();
    }

    // ================================================================== Smolak at the treaty table

    private static void RegisterTreaty()
    {
        // Same prefix as ZilleTreaty. The opening's last lines (Lib5, Part5, Lost4, CeaseL, CeaseS, PalesL, RicoGive2, RicoKeep2)
        // go to RF_Open instead of Hub.
        var t = new Talk(ZilleTreaty.Conv, "ZilleWar.Treaty.");
        string lev = V(ZilleTreaty.VarLeverage);
        string AddLev(int n) => $"{lev} = ({lev} or 0) + ({n}); ";
        string intern = And(Is(Dec("InternWehzeks")), Not(Dec("ReleaseInternees")));
        string screen = And(Eq(EarlyT5.VarRefugeePolicy, "screen"), Not(Dec("ReleaseInternees")), $"not ({intern})");
        string protect = Is(Dec("ProtectWehzeks"));

        t.Logic("RF_Open", new[] { "RF_Intern", "RF_Screen", "RF_Protect", "RF_Legion" });

        // ---- SmolakInterneesAtTreaty
        t.Line("RF_Intern", Smolak,
            "\"And the Wehzeks you put behind wire. Miners from Brenas, shopkeepers from Iza, a pastry cook, I'm told. They go home before " +
            "my pen touches anything.\"", new[] { "RF_InFree", "RF_InKeep", "RF_InKeepL" }, condition: intern);
        // the decree's own effect, without the decree panel's count of decrees passed
        string release = T(Dec("ReleaseInternees")) + $"{V(DecTurn("ReleaseInternees"))} = {SupplyWar.TurnLua}; " + Inc(Authority, -1) +
                         Inc(Opinion, -1) + Inc(Democratic, 1) + AddLev(1) + S(VarInterneesAtTable, "released");
        t.Choice("RF_InFree", "\"They go home this week. The camps close.\"", new[] { "RF_Freed" }, script: release,
            cost: "-1 Authority, +1 Negotiation Leverage, Ends the Internment Camps");
        const string keepText = "\"They stay. Wehlen started this war, and Wehlen does not decide whom Rizia trusts.\"";
        string keep = Inc(SupplyWar.NpWehlen, -1) + S(VarInterneesAtTable, "kept");
        t.Choice("RF_InKeep", keepText, new[] { "RF_Kept" }, condition: $"not ({LK.Lost})", script: keep + AddLev(-1),
            cost: "-1 Negotiation Leverage");
        t.Choice("RF_InKeepL", keepText, new[] { "RF_Kept" }, condition: LK.Lost, script: keep + AddLev(-2), cost: "-2 Negotiation Leverage");

        t.Line("RF_Screen", Smolak,
            "\"And the fishermen you took off their boats at Sallabes, until the war is over, your order said. It is over. I would like " +
            "them on the next boat home.\"", new[] { "RF_ScFree", "RF_InKeep", "RF_InKeepL" }, condition: screen);
        t.Choice("RF_ScFree", "\"They sail on the next boat.\"", new[] { "RF_Freed" },
            script: AddLev(1) + Inc(Democratic, 1) + S(VarInterneesAtTable, "released"), cost: "+1 Negotiation Leverage");

        t.Line("RF_Freed", Smolak, "\"Good. That is one paragraph I do not have to write.\"", new[] { "RF_Legion" });
        t.Line("RF_Kept", Smolak, "\"Then I write it in, and you pay for it. Every man behind your wire is a line in this draft.\"",
            new[] { "RF_Legion" });
        // the protection decree already counts in the opening leverage (WarDecrees, Leverage +1): this is only his word on it
        t.Line("RF_Protect", Smolak,
            "\"You protected Wehzeks in your cities while my radio said you hanged them. My radio was wrong. Don't tell it I said so.\"",
            new[] { "RF_Legion" }, condition: protect);

        // ---- LegionFamilies: the cities still Wehzek, named
        var legion = new List<string>();
        foreach (List<string> w in Front.Subsets(Front.Cities))
        {
            string id = "RF_Leg" + string.Concat(w);
            legion.Add(id);
            string place = w.Count == Front.Cities.Length ? "Zille" : Front.Join(w.Select(c => c == "Zille" ? "Zille city" : c));
            t.Line(id, Smolak,
                $"\"Your Zillean Legion. I have its enlistment lists from Sallabes, Your Majesty, and my police have the addresses that go " +
                $"with them, in {place}.\"", new[] { "RF_LegAmnesty", "RF_LegRefuse" },
                condition: And(Is(Dec("ZilleanLegion")), WarFlags.L(WarFlags.Over), WarFlags.LNot(WarFlags.Liberated), Front.LWehzekExactly(w)));
        }

        legion.Add("Hub");
        t.Logic("RF_Legion", legion.ToArray());
        string turn = $"{V(VarLegionTurn)} = {SupplyWar.TurnLua}; ";
        t.Choice("RF_LegAmnesty", "\"No reprisals against their families. Write it into the treaty.\"", new[] { "RF_LegAmnesty2" },
            script: T(ZilleTreaty.Term("LegionAmnesty")) + AddLev(-1) + S(VarLegionFamilies, "amnesty") + turn, cost: "-1 Negotiation Leverage");
        t.Line("RF_LegAmnesty2", Smolak,
            "\"Written. No questions for their mothers. Their sons may visit, if they come through customs like everybody else.\"", new[] { "Hub" });
        t.Choice("RF_LegRefuse", "\"They chose their side. So did their families.\"", new[] { "RF_LegRefuse2" },
            script: S(VarLegionFamilies, "refused") + turn);
        t.Line("RF_LegRefuse2", Smolak, "\"Then my police will knock on doors, and your Legion can read about it in your newspapers.\"",
            new[] { "Hub" });
        t.Register();
    }

    // ================================================================== Hegel and the miners of Isor

    /// <summary>
    /// How the King's guarantee to Hegel (the cable, "the miners of Isor will be safe") was broken since it was given, or "".
    /// Live, so the call's offer (SupplyWar.HegelCallReady) sees it the step it happens; also written to <see cref="VarBreach"/>.
    /// </summary>
    internal static string Breach()
    {
        string breach = string.Empty;
        if (Variables.GetString(ValgslandCable) == "guarantee" && Variables.GetBool(WarFlags.Declared))
        {
            int stageTurn = Variables.GetInt(ValgslandStageTurn);
            bool interned = Variables.GetBool(Dec("InternWehzeks"));
            string before = Variables.GetString(VarInternAtGuarantee);
            if (before is not ("yes" or "no"))
            {
                // first seen: a decree already in force when the guarantee was given is no breach of it
                before = interned && Variables.GetInt(DecTurn("InternWehzeks")) <= stageTurn ? "yes" : "no";
                Variables.Set(VarInternAtGuarantee, before);
            }

            if (Variables.GetBool(VarBarrageIsor) && Variables.GetInt(VarBarrageIsorTurn) > stageTurn)
            {
                breach = "barrage";
            }
            else if (interned && (before == "no" || Variables.GetInt(DecTurn("InternWehzeks")) > stageTurn))
            {
                breach = "intern";
            }
            else if (Variables.GetBool(Dec("LoyaltyOath")) && Variables.GetBool(ZilleOperation.CapturedVar("Isor")))
            {
                breach = "oath";
            }
        }

        if ((Variables.GetString(VarBreach) ?? string.Empty) != breach)
        {
            Variables.Set(VarBreach, breach);
        }

        return breach;
    }

    /// <summary>For SupplyWar.HegelCallReady: Hegel has a broken guarantee to raise and has not raised it yet.</summary>
    internal static bool GuaranteeBreachDue => Breach().Length > 0 && !Variables.GetBool(VarGuaranteeBroken);

    private static void RegisterHegel()
    {
        string breach = $"({Not(VarGuaranteeBroken)} and {V(VarBreach)} ~= nil and {V(VarBreach)} ~= \"\")";
        // the internment decree already cost Valgslandian goodwill when it was enacted
        string said = T(VarGuaranteeBroken) + $"if {V(VarBreach)} ~= \"intern\" then {Inc(SupplyWar.NpValgsland, -1)}end; ";

        void Build(Talk t, string p, string condition, ConversationNodeHook[] hooks, string exit)
        {
            t.Line(p, Hegel, "\"Your government wrote me that the miners of Isor would be safe, and my observers welcome.\"",
                new[] { p + "Shell", p + "Camp", p + "Oath" }, condition: condition, script: said, hooks: hooks);
            t.Line(p + "Shell", Hegel, "\"Since then your guns have shelled the pitheads at Isor.\"", new[] { p + "Unions" },
                condition: Eq(VarBreach, "barrage"));
            t.Line(p + "Camp", Hegel, "\"Since then you have put Wehzek miners from Brenas behind wire, by royal decree.\"", new[] { p + "Unions" },
                condition: Eq(VarBreach, "intern"));
            t.Line(p + "Oath", Hegel,
                "\"Since then the schoolteachers of Isor must swear to your crown or lose their posts. Miners have children, Mr. Toras.\"",
                new[] { p + "Unions" });
            t.Line(p + "Unions", Hegel, "\"Valgslandian unions read the same newspapers I do.\"", new[] { p + "Pay", p + "No" });
            t.Choice(p + "Pay", "\"Rizia will compensate every family, Chancellor, and your observers may count every guilder.\"",
                new[] { p + "Pay2" }, condition: $"{N(Budget)} >= 2", script: Inc(Budget, -2) + Inc(SupplyWar.NpValgsland, 1), cost: "-2 Budget");
            t.Line(p + "Pay2", Hegel, "\"Then I will tell the unions so, and my observers will hold you to every guilder.\"", new[] { exit });
            t.Choice(p + "No", "\"Rizia answers to its own people, Chancellor. Not to Valgslandian unions.\"", new[] { p + "No2" },
                script: Inc(SupplyWar.NpValgsland, -2));
            t.Line(p + "No2", Hegel, "\"I will remember you said so, Mr. Toras. So will the unions.\"", new[] { exit });
        }

        // the Turn 6 call: the owner puts RF_Guar first in its "Why" branch; once said, "Why" goes on to his other reasons
        var call = new Talk(HegelConv, "ZilleWar.Hegel.");
        Build(call, "RF_Guar", breach, null, "Why");
        call.Register();

        // otherwise the Turn 9 side talk, before his topics (vanilla 3 -> 192)
        var side = new Talk(HegelSide, "ZilleWar.RF.Hegel9.");
        Build(side, "Guar", And(LK.War, breach), Talk.From(3), "#192");
        side.Register();
    }

    // ================================================================== Smolak on the Turn 9 telephone (liberated)

    private static void RegisterSmolakT9()
    {
        // Same prefix as LateT9's call. The owner sends Lib1 / Lib1T to RF_Regime, and adds RF_LibTrust, RF_LibTrustA and
        // RF_LibEase to Lib2's menu.
        var t = new Talk(SmolakT9Conv, "ZilleWar.Late.Smolak9.");
        string occ = V(LateT8.VarOccupation);
        string martial = $"({occ} == \"martial\")";
        string amnesty = $"({occ} == \"amnesty\")";
        string oath = Is(Dec("LoyaltyOath"));
        string schools = Is(Dec("WehzekSchools"));
        const string speeches = " You are writing my speeches for me, Your Majesty.\"";
        const string fine = " My cabinet is furious. It is very hard to rescue people who keep saying they are fine.\"";
        const string both = " My cabinet cannot decide whether to be furious or grateful, Your Majesty, so it is both.\"";
        t.Logic("RF_Regime", new[] { "RF_RegMS", "RF_RegAO", "RF_RegMO", "RF_RegM", "RF_RegO", "RF_RegAS", "RF_RegA", "RF_RegS", "RF_RegG", "Lib2" });
        t.Line("RF_RegMS", Smolak, "\"Wehzek schools under a Rizian curfew." + both, new[] { "Lib2" }, condition: And(martial, schools));
        t.Line("RF_RegAO", Smolak, "\"An amnesty for my conscripts and an oath for the schoolteachers." + both, new[] { "Lib2" },
            condition: And(amnesty, oath));
        t.Line("RF_RegMO", Smolak, "\"Curfews for grandmothers in Nanta, and oaths for the schoolteachers." + speeches, new[] { "Lib2" },
            condition: And(martial, oath));
        t.Line("RF_RegM", Smolak, "\"Curfews for grandmothers in Nanta." + speeches, new[] { "Lib2" }, condition: martial);
        t.Line("RF_RegO", Smolak, "\"Oaths for the schoolteachers of Zille, or no pension." + speeches, new[] { "Lib2" }, condition: oath);
        t.Line("RF_RegAS", Smolak, "\"Wehzek schools in Zille. An amnesty." + fine, new[] { "Lib2" }, condition: And(amnesty, schools));
        t.Line("RF_RegA", Smolak, "\"An amnesty for my conscripts in Zille." + fine, new[] { "Lib2" }, condition: amnesty);
        t.Line("RF_RegS", Smolak, "\"Wehzek schools in Zille." + fine, new[] { "Lib2" }, condition: schools);
        t.Line("RF_RegG", Smolak, "\"Your generals govern Zille, I hear. Generals make excellent governors, for about a month.\"", new[] { "Lib2" },
            condition: $"({occ} == \"generals\")");

        // LateT10 reads "trust" as it reads "deal": Nanta stays quiet
        string trust = S(LateT9.VarNanta, "trust") + Inc(SupplyWar.NpWehlen, -1);
        t.Choice("RF_LibTrust", "\"Ask Nanta's Wehzeks who gave them their schools, Mr. President.\"", new[] { "RF_LibTrust2" },
            condition: And(schools, $"not {martial}"), script: trust);
        t.Choice("RF_LibTrustA", "\"Ask Nanta's Wehzeks who sent their sons home, Mr. President.\"", new[] { "RF_LibTrust2" },
            condition: And(amnesty, $"not {schools}", $"not {oath}"), script: trust);
        t.Line("RF_LibTrust2", Smolak, "\"Gratitude. A poor weapon, Your Majesty, but a very hard one to aim leaflets at. Good night.\"", new[] { "Bye" });
        t.Choice("RF_LibEase", "\"Martial law in Zille ends this month. Nanta can make up its own mind.\"", new[] { "RF_LibEase2" },
            condition: And(martial, $"not {oath}"), script: trust + Inc(Authority, -1) + T(VarMartialLifted), cost: "-1 Authority, Ends -1 Global Perception");
        t.Line("RF_LibEase2", Smolak,
            "\"You would lift a curfew just to spite me? I am flattered. My leaflets will have to find another subject.\"", new[] { "Bye" });
        t.Register();
    }

    // ================================================================== Beatrice at Turn 7: the end date

    private static void RegisterBeatrice()
    {
        // Same prefix as EarlyT7Diplomacy's Rumburg meeting. The owner sends the post-war replies (Gold1r-3r, RPart1r-2r, RDef1r-3r,
        // RCf1r-2r) to RF_EndDate instead of #1370.
        var t = new Talk(RumburgMeeting, "ZilleWar.E7.Rumburg.");
        string refused = And(Is(RumburgWantsCeasefire), Eq(SmolakAnswer, "refused"));
        string kingCf = WarFlags.L(WarFlow.VarCeasefire);
        t.Logic("RF_EndDate", new[] { "RF_EDLib", "RF_EDCf", "RF_EDPart", "RF_EDLost", "#1370" });
        t.Line("RF_EDLib", Beatrice, "\"One more thing. You refused Smolak's end date and found a better one. I'll allow it, since it worked.\"",
            new[] { "#1370" }, condition: And(refused, LK.Lib));
        string[] menu = { "RF_ED1", "RF_ED2" };
        t.Line("RF_EDCf", Beatrice,
            "\"One more thing, Romus. I asked for an end date, and Smolak offered you one on the telephone. You sent him away, then asked " +
            "him for one yourself. I did enjoy that.\"", menu, condition: And(refused, $"not ({LK.Lib})", kingCf));
        t.Line("RF_EDPart", Beatrice,
            "\"One more thing, Romus. I asked you for an end date. Smolak offered you one on the telephone, and you sent him away. You " +
            "still came home with only part of Zille.\"", menu, condition: And(refused, LK.Part));
        t.Line("RF_EDLost", Beatrice,
            "\"One more thing, Romus. I asked you for an end date. Smolak offered you one on the telephone, and you sent him away. He keeps " +
            "Zille city all the same.\"", menu, condition: And(refused, LK.Lost));
        t.Choice("RF_ED1", "\"His terms were an insult to the men in the line.\"", new[] { "RF_ED1r" },
            script: Inc(SupplyWar.NpRumburg, -1));
        t.Line("RF_ED1r", Beatrice, "\"The men in the line might have taken the insult and come home, dear. Nobody asked them.\"", new[] { "#1370" });
        t.Choice("RF_ED2", "\"You were right, Beatrice. I should have taken it.\"", new[] { "RF_ED2r" }, script: Inc(Authority, -1),
            cost: "-1 Authority");
        t.Line("RF_ED2r", Beatrice, "\"Do say it in front of my ministers some time.\"", new[] { "#1370" });
        t.Register();
    }

    // ================================================================== every step: the barrage on Isor, the papers

    public static void OnStep()
    {
        try
        {
            if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !Variables.GetBool(WarFlags.Declared))
            {
                return;
            }

            int turn = GameState.CurrentTurnNum ?? 0;
            BookBarrage(turn);
            Breach();
            InterneesNews();
            LegionNews(turn);
            RevokedNews();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"ReactionsForeign.OnStep failed: {ex}");
        }
    }

    /// <summary>
    /// Plan_Bombard is cleared at every War Council, so it is booked the first step after its phase ended: a phase whose aim held
    /// Isor and opened with the barrage shelled Isor.
    /// </summary>
    private static void BookBarrage(int turn)
    {
        int k = Variables.GetInt(ZilleCampaign.VarPhaseDone);
        if (k <= 0 || Variables.GetInt(VarBarrageChecked) >= k)
        {
            return;
        }

        Variables.Set(VarBarrageChecked, k);
        string aim = Variables.GetString(ZilleCampaign.AimVar(k)) ?? string.Empty;
        if (Variables.GetBool(WarCouncil.VarBombard) && aim.Contains("Isor", StringComparison.Ordinal) && !Variables.GetBool(VarBarrageIsor))
        {
            Variables.Set(VarBarrageIsor, true);
            Variables.Set(VarBarrageIsorTurn, turn);
        }
    }

    private static void RevokedNews()
    {
        if (!Variables.GetBool(VarLeaseRevoked) || Variables.GetBool(VarRevokedNews))
        {
            return;
        }

        Variables.Set(VarRevokedNews, true);
        NewsWire.Publish("Lespia_LeaseRevoked", "G", "Rizia Revokes Lespia's Caleqabiz Lease", NewsWire.P(
            "MONQIZ - Rizia has revoked the fifty-year lease of Caleqabiz it granted Lespia in exchange for the Lespian navy's help in " +
            "the Strait of Kreleck. Rizian officials say the lease paid for ships that are no longer in the strait.",
            "Lespia's foreign ministry called the decision \"a breach of a signed contract\" and said its sailors would leave the island " +
            "under protest. Lawyers in Perla are said to be studying the text."));
    }

    private static void InterneesNews()
    {
        if (Variables.GetString(VarInterneesAtTable) != "released" || Variables.GetBool(VarInterneesNews))
        {
            return;
        }

        Variables.Set(VarInterneesNews, true);
        bool camps = Variables.GetBool(Dec("InternWehzeks"));
        NewsWire.Publish("Late_InterneesFreed", "G", camps ? "Rizia Closes Its Internment Camps" : "Wehzek Fishermen Sent Home from Sallabes",
            camps
                ? NewsWire.P(
                    "MONQIZ - Rizia has begun closing the camps it opened for Wehzek nationals during the war over Zille. The decision follows " +
                    "the talks with Wehlen, where President Smolak is understood to have made the internees' release a condition.",
                    "The first trains have left for Iza and the mining towns of Brenas, carrying men with a railway warrant and a week's pay.")
                : NewsWire.P(
                    "SALLABES - The Wehzek men taken off refugee boats in the Gulf of Moridia and held at Sallabes during the war are being " +
                    "sent home. The release follows the talks with Wehlen, where President Smolak raised their case.",
                    "A Wehzek ferry is expected in Sallabes this week. The Rizian navy will escort it to the strait."));
    }

    /// <summary>
    /// The Legion's families, the turn after the treaty table: refused an amnesty, or asked for one in a treaty that was never
    /// signed. The Voice of Rizia prints their letters; public opinion pays once.
    /// </summary>
    private static void LegionNews(int turn)
    {
        string answer = Variables.GetString(VarLegionFamilies) ?? string.Empty;
        bool unsigned = answer == "amnesty" && !Variables.GetBool(ZilleTreaty.VarSigned);
        if (!(answer == "refused" || unsigned) || Variables.GetBool(VarLegionNews) || turn <= Variables.GetInt(VarLegionTurn))
        {
            return;
        }

        Variables.Set(VarLegionNews, true);
        var wehzek = Front.Wehzek;
        string where = wehzek.Count == Front.Cities.Length
            ? "Zille"
            : wehzek.Count > 0 ? Front.WehzekWords : "the Wehzek side of the line";
        NewsWire.Publish("Late_LegionMothers", "V", "The Legion's Mothers", NewsWire.P(
            $"Every week the Voice of Rizia receives letters from {where}. They are written by the mothers and wives of the men who " +
            "enlisted in the King's Zillean Legion, and they tell the same story: a knock at night, a search, a ration card withdrawn, a " +
            "son's photograph taken down from the wall.",
            unsigned
                ? "At the talks with Wehlen, the King asked for an amnesty for these families. It was written into a treaty that was never " +
                  "signed."
                : "At the talks with Wehlen, President Smolak raised the matter of the Legion's families himself. The King's delegation " +
                  "would not write their safety into the treaty.",
            "The Legion was raised to fight for the streets of Zille. Its families are still living in them."));
        Variables.Set(Opinion, Variables.GetInt(Opinion) - 1);
    }
}
