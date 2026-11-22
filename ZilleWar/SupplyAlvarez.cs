namespace ZilleWar;

/// <summary>
/// Prime Minister Alvarez on the telephone. He sells two things, separately, and the King may buy either, both or neither,
/// in any order across calls:
/// - the Strait of Kreleck: the Lespian navy closes Wehlen's sea road to its Zille garrison. Priced in Aureus gas if Rizia
///   bought the field from Pales (Diplomacy_Pales_GasField_Buy, not cancelled): six units to LesPower every turn for the price
///   of two once the gas flows (SupplyWar.GasContract). Otherwise naval rights at Caleqabiz, or money (cheaper if he already
///   spoke for Rizia, Diplomacy_Lespia_WehlenHelp). A broken Turn 5 MITZ promise adds a surcharge to every price, until the King
///   gives him the final vote. The Ramiro file can get the strait for nothing.
/// - the Meftiem: Rizia's vote with Lespia on the MITZ board (vanilla Diplomacy_Lespia_PromiseITZSupport, or the _Late promise
///   for the final vote if the Turn 5 board has already voted). In return Lespia votes against sanctions on Rizia at the
///   Alliance, and the King picks a destroyer from the Lespian yards or Lespian naval intelligence on the Wehzek convoys.
/// Once the strait is closed it weakens the garrison every turn (SupplyWar.NewTurn). Later calls are invoices: his shipyards
/// arming Pales in the other war, a free gift, the expelled attaché and, if the consulate files were found, the confrontation
/// over Héctor Ramiro and the Zille bombing.
/// Each call opens, reads the front and closes differently: the telephone, his greeting (or his reminder that the King turned
/// him down last time), one line on the latest phase (Zille or Auremina taken, an aim missed, a quiet month), the bargains
/// "as before" on a second call, a later report from the strait, and a different goodbye and hang-up each time.
/// </summary>
internal static partial class SupplyWar
{
    /// <summary>Lespia promised to vote against sanctions on Rizia at the Alliance (paid for with Rizia's Meftiem vote).</summary>
    public const string VarLespiaAllianceVote = "ZilleWar.Lespia_AllianceVote";
    /// <summary>What Lespia gave for Rizia's Meftiem vote besides its Alliance vote: "destroyer" | "intel".</summary>
    public const string VarLespiaMitzReward = "ZilleWar.Lespia_MitzReward";
    private const string VarAlvarezPhotoSaid = "ZilleWar.Alvarez_PhotoSaid";

    /// <summary>Lua: Lespia already holds Rizia's next Meftiem vote.</summary>
    internal static readonly string LespiaHasVote =
        $"({Is(VarLespiaMitz)} or {Is(MitzPromiseLate)} or ({Is(MitzPromise)} and not ({Is(VoteNationalization)} or " +
        $"{Is(VoteRenegotiation)} or {Is(VoteStatusQuo)})))";

    /// <summary>Lua: Alvarez still has the Meftiem bargain to offer (no promise to Lespia or Valgsland, final vote not yet held).</summary>
    internal static readonly string MitzBargainOpen = $"(not {LespiaHasVote} and {Not(ValgslandMitz)} and {TurnLua} <= 8)";

    /// <summary>Lespia holds Caleqabiz on a fifty-year lease (the price of the strait when the gas was refused).</summary>
    public const string VarLespiaLease = "ZilleWar.Lespia_CaleqabizLease";

    private static void RegisterAlvarez()
    {
        SuzerainModdingKit.Variables.Register(VarLespiaLease);
        SuzerainModdingKit.Variables.Register(VarLespiaAllianceVote);
        SuzerainModdingKit.Variables.Register(VarLespiaMitzReward);
        SuzerainModdingKit.Variables.Register(VarAlvarezPhotoSaid);
        var d = new Dlg(AlvarezConv, "ZilleWar.Alvarez.");
        string calls = N(VarAlvarezCalls);
        string deal = $"({N(Count(Lever.Lespia))} > 0)";
        string noDeal = $"({N(Count(Lever.Lespia))} == 0)";
        string kept = Eq(AnDeclaration.VarLespianNote, "kept");
        string confront = $"({Eq(VarRamiro, "confront")} and {Not(VarRamiroConfronted)})";
        string leverage = Eq(VarRamiro, "leverage");
        string field = $"({Is(GasFieldBuy)} and {Not(GasFieldBuyCancelled)})";
        string voted = $"({Is(VoteNationalization)} or {Is(VoteRenegotiation)} or {Is(VoteStatusQuo)})";
        // A broken Turn 5 promise: the surcharge stays until the King gives him the final vote.
        string broke = $"(({Is(MitzPromise)} and {Is(VoteNationalization)}) or {Is(VarLespiaBetrayedT5)})";
        string grudge = $"({broke} and {Not(MitzPromiseLate)})";
        string friendly = $"(not {grudge})";
        string mitzOpen = MitzBargainOpen;
        string help = Is("RiziaDLC.Diplomacy_Lespia_WehlenHelp");
        string budget = N(Budget);
        // Lespia's rival Valgsland takes every Lespian favour to Rizia badly.
        string dealScript = $"{V(Count(Lever.Lespia))} = 1; " + T(VarLespiaStrait) + Talk.Weaken(2) + Inc(NpLespia, 1) + Inc(NpWehlen, -1) +
                            Inc(NpValgsland, -1) + SetTurn(VarLespiaDealTurn);
        string bindMitz = $"if {voted} then {T(MitzPromiseLate)} else {T(MitzPromise)} end; " + T(VarLespiaMitz) + T(VarLespiaAllianceVote) +
                          Inc(NpLespia, 1) + Inc(NpValgsland, -1);
        string enter = $"AddConversant(\"{Alvarez}\"); AddConversant(\"{Esquibel}\");";

        // He reads the front like a market report: one line on the latest phase, then on to business.
        void Front(string name, string exit)
        {
            d.Branch(name, name + "Held", name + "Extra", name + "ZilleR", name + "Zille", name + "Aur", name + "MissedW", name + "Missed", exit);
            d.SayIf(name + "Held", Is(VarCallHeld), Alvarez,
                "\"A quiet month at the front, I hear. Quiet months make my admirals nervous. They start asking what exactly they're being " +
                "paid for.\"", exit);
            d.SayIf(name + "Extra", $"{N(VarCallFought)} >= 1 and {Is(VarCallLastWon)} and {Is(VarCallExtra)}", Alvarez,
                "\"And two cities in a single phase. My admirals have started a sweepstake on the day Smolak asks for terms. I've put my " +
                "money on next month.\"", exit);
            d.SayIf(name + "ZilleR", $"{N(VarCallFought)} >= 1 and {Is(VarCallLastWon)} and {Eq(VarCallLastAim, "Harbor")} and {Not(VarRamiroFound)}",
                Alvarez, "\"And congratulations on Zille. I trust my consulate there was treated with every courtesy. Diplomats are such " +
                         "delicate creatures.\"", exit);
            d.SayIf(name + "Zille", $"{N(VarCallFought)} >= 1 and {Is(VarCallLastWon)} and {Eq(VarCallLastAim, "Harbor")}", Alvarez,
                "\"And congratulations on Zille. A beautiful harbor. I always said it was wasted on the Wehzeks.\"", exit);
            d.SayIf(name + "Aur", $"{N(VarCallFought)} >= 1 and {Is(VarCallLastWon)} and {Eq(VarCallLastAim, "Wall")}", Alvarez,
                "\"Auremina, I read. Gold. You'll forgive a Lespian for taking an interest. Gold has a way of becoming everybody's " +
                "business.\"", exit);
            d.SayIf(name + "MissedW", $"{N(VarCallFought)} >= 1 and {Not(VarCallLastWon)} and {Eq(VarCallLastAim, "Wall")}", Alvarez,
                "\"The hills held, I read. Hills do that. It's why nobody has ever built a navy for them.\"", exit);
            d.SayIf(name + "Missed", $"{N(VarCallFought)} >= 1 and {Not(VarCallLastWon)}", Alvarez,
                "\"I read about the river crossings. Unfortunate. Perla's markets dislike a stalemate, Your Majesty, and so do I. " +
                "Stalemates are very bad for business.\"", exit);
        }

        // The war since his last call (SupplyWar.PrepareCallState).
        string fought = $"{N(VarCallFought)} >= 1";
        string won = $"({fought} and {Is(VarCallLastWon)})";
        string held = Is(VarCallHeld);
        string missed = $"({fought} and {Not(VarCallLastWon)} and not {held})";
        string aim(string a) => Eq(VarCallLastAim, a);
        string invoices = N(VarAlvInvoices);

        d.Start(AlvarezConv, ResetAsked + Inc(VarAlvarezCalls, 1) + Music, "Phone");
        d.Branch("Phone", "PhoneFirst", "PhoneMissed", "PhoneLater3", "PhoneLater");
        d.SayIfDo("PhoneFirst", $"{calls} == 1", Narrator,
            "Laurento was waiting in my study with the blue telephone already on the desk. \"Prime Minister Alvarez, Your Majesty. " +
            "He says he has been very patient.\"", enter, "Open");
        d.SayIfDo("PhoneMissed", missed, Narrator,
            "Laurento brought in the blue telephone. \"Prime Minister Alvarez, Your Majesty. He asked after your health, which means he " +
            "has read the casualty lists.\"", enter, "Open");
        d.SayIfDo("PhoneLater3", $"{calls} >= 3", Narrator,
            "The blue telephone was already ringing when Laurento carried it in. \"Perla again, Your Majesty. I've stopped asking the " +
            "operator who it is.\"", enter, "Open");
        d.SayDo("PhoneLater", Narrator,
            "Laurento brought in the blue telephone and set it down as if it might bite. \"Prime Minister Alvarez again, Your Majesty. " +
            "He sounds pleased with himself, which is never good news.\"", enter, "Open");

        d.Branch("Open", "ConfrontOpen", "HelloKept", "HelloBurned", "HelloRefused", "HelloAgain3", "HelloAgain", "InvExpelled", "InvOpen1",
            "InvOpen2");
        d.SayIf("HelloKept", $"{calls} == 1 and {noDeal} and {kept}", Alvarez,
            "\"Your Majesty. I hope my little note didn't get lost in all the excitement in Konstantium. It was a very exciting day.\"", "FrontA");
        d.SayIf("HelloBurned", $"{calls} == 1 and {noDeal}", Alvarez,
            "\"Your Majesty. I understand my note met with an accident in Konstantium. No matter. I'm a patient man, and Lespia is a " +
            "practical country.\"", "FrontA");
        d.SayIf("HelloAgain", noDeal, Alvarez,
            "\"Your Majesty. Smolak's convoys are still crossing the strait, I see. I thought you might like to talk about that.\"", "FrontA");
        d.SayIfDo("HelloRefused", $"{noDeal} and {Is(VarAlvRefusedStrait)}", Alvarez,
            "\"Your Majesty. The last time we spoke, you told me Rizia had its pride. I've been reading your newspapers since, and I " +
            "wondered whether pride is still in stock.\"", F(VarAlvRefusedStrait), "FrontA");
        d.SayIf("HelloAgain3", $"{noDeal} and {calls} >= 3", Alvarez,
            "\"Your Majesty. Every night another Wehzek convoy crosses Kreleck, and every morning I read where it unloaded. I thought we " +
            "might try this conversation once more.\"", "FrontA");
        Front("FrontA", "Jab");
        Front("FrontI", "Inv");

        // His jabs about Aureus are made once, not on every call.
        d.Branch("Jab", "JabBlockade", "JabField", "Table");
        d.SayIfDo("JabBlockade", $"{Is(BlockDrillship)} and {Not(VarAlvJabbed)}", Alvarez,
            "\"Though I confess I find it amusing. Your warships are still circling my partner's drillship at Aureus, and you'd like " +
            "mine at Kreleck.\"", T(VarAlvJabbed), "Table");
        d.SayIfDo("JabField", $"{field} and {Not(VarAlvJabbed)}", Alvarez,
            "\"No hard feelings about Aureus, by the way. You outbid LesPower fair and square. Well. Square, at least.\"", T(VarAlvJabbed), "Table");

        // ---- what he has to sell today
        d.Branch("Table", "TableBothAgain", "TableBoth", "TableHegel", "TableFriend", "TableStraitAgain", "TableStrait");
        d.SayIf("TableBothAgain", $"{mitzOpen} and {calls} >= 2", Alvarez,
            "\"The same two bargains as before, Your Majesty. The strait and the Meftiem. The prices haven't changed. Your war has.\"", "Hub");
        d.SayIf("TableBoth", mitzOpen, Alvarez,
            "\"I have two things to discuss with you today, Your Majesty. The Strait of Kreleck, and the Meftiem.\"", "TableBoth2");
        d.Say("TableBoth2", Alvarez,
            "\"Two separate bargains. Take one, take both, take neither. I'm a modern man. I don't sell in bundles.\"", "Hub");
        d.SayIf("TableHegel", Is(ValgslandMitz), Alvarez,
            "\"I'd have liked to talk about the Meftiem too, but I hear Chancellor Hegel already has your vote. A pity. So let's " +
            "talk about ships.\"", "Hub");
        d.SayIf("TableFriend", LespiaHasVote, Alvarez,
            "\"Rizia already stands with Lespia at the Meftiem, so we can skip the tiresome part. I remember my friends, Your Majesty. " +
            "It's my enemies I write down.\"", "Hub");
        d.SayIf("TableStraitAgain", $"{calls} >= 2", Alvarez, "\"So. Ships, again. My favorite subject, and lately yours.\"", "Hub");
        d.Say("TableStrait", Alvarez, "\"So. Let's talk about ships.\"", "Hub");

        d.Branch("Hub", Gate("AskStrait"), Gate("AskMitz"), Gate("Why"), Gate("Bye"), "ByeLater");
        d.Opt("AskStrait", $"{noDeal} and {Not(Q(1))}", "\"Tell me about the strait.\"", null, T(Q(1)), "Strait");
        d.Opt("AskMitz", $"{mitzOpen} and {Not(Q(2))}", "\"What does Lespia want at the Meftiem?\"", null, T(Q(2)), "Mitz");
        d.Opt("Why", $"{noDeal} and {Not(VarAlvAskedWhy)}", "\"Why would Lespia help Rizia at all?\"", null, T(VarAlvAskedWhy), "Why2");
        d.Say("Why2", Alvarez,
            "\"Because President Smolak has called me an Arcasian puppet on the radio three times this year. A Wehlen without Zille " +
            "has fewer reasons to make speeches about Lespia.\"", "Why3");
        d.Say("Why3", Alvarez,
            "\"And because you will owe me. I'm being honest, you see. My lying days are behind me.\"", "Hub");
        d.Opt("Bye", $"{calls} == 1", "\"That will be all, Prime Minister.\"", null, null, "ByeBranch");
        d.Opt("ByeLater", $"{calls} ~= 1", "\"Good day, Prime Minister.\"", null, null, "ByeBranch");
        d.Branch("ByeBranch", "ByeNoDealAgain", "ByeNoDeal", "ByeDealAgain", "ByeDeal");
        d.SayIf("ByeNoDealAgain", $"{noDeal} and {calls} >= 2", Alvarez,
            "\"The offer stays on my desk, Your Majesty, beside the Wehzek shipping lists. Both are getting longer.\"", "Hangup");
        d.SayIf("ByeDealAgain", $"{deal} and {invoices} >= 2", Alvarez,
            "\"Until next month, Your Majesty. My accountants will be in touch, and so, I'm afraid, will I.\"", "Hangup");
        d.SayIf("ByeNoDeal", noDeal, Alvarez,
            "\"Of course. The strait isn't going anywhere. Neither, unfortunately, are Smolak's convoys.\"", "Hangup");
        d.Say("ByeDeal", Alvarez,
            "\"A pleasure, as always, Your Majesty. Do give my regards to Mr. Smolak, next time he's on the radio.\"", "Hangup");

        // ================================================================ (a) the Strait of Kreleck
        d.Say("Strait", Alvarez,
            "\"The Lespian navy has the ships to close the Strait of Kreleck. Every Wehzek convoy stopped, searched and held in a " +
            "Lespian port.\"", "Strait2");
        d.Say("Strait2", Alvarez,
            "\"For as long as the paperwork takes. And the paperwork, I promise you, will take a very long time.\"", "PriceLine");

        // With the Aureus field bought from Pales, Lespia wants its gas: -6 Energy for +2 Budget every turn, from the turn the gas
        // flows (Policy_Economy_AureusField_Extractions, set at the Turn 6 deal signing). Without it, Caleqabiz or money.
        string flowing = Is(Extractions);
        string pending = $"({field} and not {flowing} and {TurnLua} <= 6)";
        string now = $"({field} and {flowing})";
        d.Branch("PriceLine", "PriceGasNow", "PriceGasLater", "PriceHarbor");
        d.SayIf("PriceGasNow", now, Alvarez,
            "\"Now, the price. Let me walk you through it slowly, Your Majesty, since Rizia is new to this sort of thing.\"", "GasNow2");
        d.Say("GasNow2", Alvarez,
            "\"You took the Aureus field from under LesPower. Very bold. Now the gas is flowing, and Lespia would like its share.\"", "GasNow3");
        d.Say("GasNow3", Alvarez,
            "\"A steady share every quarter, and LesPower pays three Liras per thousand cubic meters. A fair price, between friends. For as long as you sit on that throne.\"", "GrudgeGas");
        d.SayIf("PriceGasLater", pending, Alvarez,
            "\"Now, the price. I'll keep it simple for you. Your pipeline to Aureus isn't finished. I know, I have engineers too.\"",
            "GasLater2");
        d.Say("GasLater2", Alvarez,
            "\"So my navy closes the strait now, on credit, because I'm a generous man. The day your gas flows, LesPower takes its " +
            "share every quarter, at three Liras per thousand cubic meters.\"", "GasLater3");
        d.Say("GasLater3", Alvarez, "\"You'll find that's how grown-up countries do business.\"", "GrudgeGas");
        d.Branch("GrudgeGas", "GrudgeGasSay", "EsqGas");
        d.SayIf("GrudgeGasSay", grudge, Alvarez,
            "\"Oh, and since you looked me in the eye at the Meftiem and then voted with the Malenyevists, a little something up " +
            "front. Three billion Liras, in cash. Call it the price of my disappointment.\"", "EsqGas");
        d.Say("EsqGas", Esquibel,
            "Laurento covered the mouthpiece. \"Three Liras, when the market pays ten. Most of the field, every quarter, for the rest " +
            "of your reign. It's not a price, Your Majesty. It's a tribute.\"", "EsqGas2");
        d.Say("EsqGas2", Esquibel, "\"But he can close that strait, and nobody else can.\"", "MenuGas");

        d.Branch("MenuGas", Gate("Free"), Gate("GasNow"), Gate("GasNowG"), Gate("GasLater"), Gate("GasLaterG"), "RefuseGas");

        // No gas? Then land: a fifty-year lease of Caleqabiz, with a token rent paid to Rizia.
        d.Opt("RefuseGas", null, "\"No gas. Not at that price.\"", null, null, "Lease1");
        d.Say("Lease1", Alvarez,
            "\"Then let's talk about land instead, since gas upsets you. Fifty years of Caleqabiz. A proper lease, signed and stamped: " +
            "our pier, our depot, our flag over the harbor.\"", "Lease2");
        d.Say("Lease2", Alvarez,
            "\"And because I'm a generous man, Lespia pays rent. A billion and a half Liras a quarter, on time. You'll hardly notice " +
            "the island is gone.\"", "EsqLease");
        d.Say("EsqLease", Esquibel,
            "Laurento covered the mouthpiece. \"Fifty years, Your Majesty. Your father signed twenty-five on Zille, and we are at war " +
            "to get it back.\"", "MenuLease");
        d.Branch("MenuLease", "Lease", "RefuseLease");
        d.Opt("Lease", null, "\"Fifty years. Draw up the lease.\"", "-2 Authority, +1 Budget Per Turn, Caleqabiz Leased to Lespia for 50 Years",
            dealScript + Talk.Weaken(1) + T(VarLespiaCaleqabiz) + T(VarLespiaLease) + Inc(Authority, -2) +
            $"{V(WorldModifiers.CaleqabizRent)} = 1; ", "DealLease");
        d.Say("DealLease", Alvarez,
            "\"Splendid. My lawyers have had the papers ready since Konstantium. My admirals will be in the strait by Sunday.\"", "Hub");
        d.Opt("RefuseLease", null, "\"No. Not the island, and not the gas.\"", null, T(VarAlvRefusedStrait), "NoDeal");
        const string gasNowText = "\"Six for two. LesPower gets its gas.\"";
        const string gasLaterText = "\"Close the strait. LesPower gets its gas when the pipeline opens.\"";
        string gasDeal = dealScript + Talk.Weaken(1) + T(VarLespiaGasDeal);
        d.Opt("GasNow", $"{now} and {friendly}", gasNowText, "-6 Energy Per Turn, +2 Budget Per Turn", gasDeal, "DealGas");
        d.Opt("GasNowG", $"{now} and {grudge} and {budget} >= 2", gasNowText, "-2 Budget, -6 Energy Per Turn, +2 Budget Per Turn",
            gasDeal + Inc(Budget, -2), "DealGas");
        d.Opt("GasLater", $"{pending} and {friendly}", gasLaterText, "-6 Energy Per Turn, +2 Budget Per Turn Once the Aureus Gas Flows",
            gasDeal, "DealGas");
        d.Opt("GasLaterG", $"{pending} and {grudge} and {budget} >= 2", gasLaterText,
            "-2 Budget, -6 Energy Per Turn, +2 Budget Per Turn Once the Aureus Gas Flows", gasDeal + Inc(Budget, -2), "DealGas");
        d.Say("DealGas", Alvarez,
            "\"There. That wasn't so painful, was it? My admirals will be in the strait by Sunday.\"", "DealGas2");
        d.Say("DealGas2", Alvarez,
            "\"Do give my regards to your treasury. It's about to become a very good customer of ours.\"", "Hub");

        d.SayIf("PriceHarbor", $"not {pending} and not {now}", Alvarez,
            "\"Now, the price. I'll keep it simple. My admirals like harbors, and Caleqabiz has a very nice one.\"", "Harbor2");
        d.Say("Harbor2", Alvarez,
            "\"A pier, a fuel depot, a few hundred sailors for as long as the war lasts. Rizia keeps the island, of course. We'd only " +
            "be guests.\"", "Harbor3");
        d.Say("Harbor3", Alvarez, "\"You understand the concept of guests, I'm sure. Zille had some.\"", "GrudgeHarbor");
        d.Branch("GrudgeHarbor", "GrudgeHarborSay", "EsqHarbor");
        d.SayIf("GrudgeHarborSay", grudge, Alvarez,
            "\"And a word about the Meftiem. You gave me your word there, and broke it. So whatever we settle on today costs a little " +
            "more than I'd charge a friend.\"", "EsqHarbor");
        d.Say("EsqHarbor", Esquibel,
            "Laurento covered the mouthpiece. \"Guests with a lease, Your Majesty. That's how Zille began. He'd take money instead, " +
            "if we have it.\"", "MenuHarbor");

        d.Branch("MenuHarbor", Gate("Free"), Gate("Harbor"), Gate("HarborG"), Gate("Money2"), Gate("Money3"), Gate("Money4"),
            Gate("Money5"), "Refuse");
        const string harborText = "\"Your ships may use Caleqabiz for as long as the war lasts.\"";
        string harborDeal = dealScript + Inc(Authority, -1) + T(VarLespiaCaleqabiz) + Inc(NpValgsland, -1);
        d.Opt("Harbor", friendly, harborText, "-1 Authority, Lespian Naval Rights at Caleqabiz", harborDeal, "Deal2");
        d.Opt("HarborG", $"{grudge} and {budget} >= 2", harborText, "-1 Authority, -2 Budget, Lespian Naval Rights at Caleqabiz",
            harborDeal + Inc(Budget, -2), "Deal2");
        const string money = "\"Not Caleqabiz. I'll pay for your ships instead.\"";
        string paid = dealScript + T(VarLespiaPaid);
        d.Opt("Money2", $"{help} and {friendly} and {budget} >= 2", money, "-2 Budget", paid + Inc(Budget, -2), "Money");
        d.Opt("Money3", $"not {help} and {friendly} and {budget} >= 3", money, "-3 Budget", paid + Inc(Budget, -3), "Money");
        d.Opt("Money4", $"{help} and {grudge} and {budget} >= 4", money, "-4 Budget", paid + Inc(Budget, -4), "Money");
        d.Opt("Money5", $"not {help} and {grudge} and {budget} >= 5", money, "-5 Budget", paid + Inc(Budget, -5), "Money");
        d.Say("Money", Alvarez,
            "\"Money. How refreshingly direct. Warships are expensive, Your Majesty, and Lespian admirals more so.\"", "Money2b");
        d.Say("Money2b", Alvarez,
            "\"Do try to pay on time. I'd hate for us to have one of those awkward conversations.\"", "Deal2");
        d.Say("Deal2", Alvarez,
            "\"Then it's settled. My admirals will be in the strait by Sunday.\"", "Hub");

        // the Ramiro file, kept as leverage, buys the strait for nothing
        d.Opt("Free", $"{leverage} and {Not(VarRamiroConfronted)}",
            "\"I think you'll close the strait for nothing, Prime Minister. Ask me about Héctor Ramiro.\"", null, null, "Free2");
        d.Say("Free2", Narrator, "There was a pause long enough for me to hear the line hum.", "Free3");
        d.Say("Free3", Alvarez,
            "\"I see Rizia's soldiers have been reading other people's mail in Zille. How very modern of them.\"", "Free4");
        d.Say("Free4", Alvarez,
            "\"Very well. Call it a gesture of friendship. And let's never speak of Mr. Ramiro again, on this line or any other.\"", "FreeDeal");
        d.Do("FreeDeal", dealScript + Inc(NpLespia, -2) + T(VarRamiroConfronted) + S(VarRamiroOutcome, "free"), "Deal2");

        d.Opt("Refuse", null, "\"No. Not at that price.\"", null, T(VarAlvRefusedStrait), "NoDeal");
        d.Branch("NoDeal", "NoDealAgain", "NoDeal1");
        d.SayIf("NoDealAgain", $"{calls} >= 2", Alvarez,
            "\"Still no. You're consistent, Your Majesty, I'll give you that. So are Smolak's convoys.\"", "Hub");
        d.Say("NoDeal1", Alvarez,
            "\"Ha. Pride. I had some once, before I had a navy. The offer stands, Your Majesty. For now.\"", "Hub");

        // ================================================================ (b) the Meftiem
        d.Branch("Mitz", "MitzBetrayed", "MitzAgain", "MitzLate", "MitzAsk");
        d.SayIf("MitzAgain", Is(VarAlvRefusedMitz), Alvarez,
            "\"Last time you told me Rizia's vote wasn't for sale. I've come back with the same price, because I find principles soften " +
            "as a war goes on. Shall we see whether yours have?\"", "MitzOffer");
        d.SayIf("MitzBetrayed", grudge, Alvarez,
            "\"You looked me in the eye at the Meftiem, and then you voted with the Malenyevists. I haven't forgotten.\"", "MitzBetrayed2");
        d.Say("MitzBetrayed2", Alvarez,
            "\"But there will be a final vote, and I'm a forgiving man when it suits me. Give me your word on that one. This time " +
            "I'll hold you to it.\"", "MitzOffer");
        d.SayIf("MitzLate", voted, Alvarez,
            "\"The board voted the way it voted. There will be a final vote, and Saltana and Hegel are already counting heads.\"", "MitzLate2");
        d.Say("MitzLate2", Alvarez,
            "\"When it comes, I want Rizia's vote with Lespia. And I'll pay for it, which is more than Hegel would do.\"", "MitzOffer");
        d.Say("MitzAsk", Alvarez,
            "\"Saltana and Hegel want to nationalize the Meftiem. Lespia wants it left exactly as it is.\"", "MitzAsk2");
        d.Say("MitzAsk2", Alvarez,
            "\"When the board votes, I want Rizia's vote with Lespia. And I'll pay for it, which is more than Hegel would do.\"", "MitzOffer");

        d.Say("MitzOffer", Alvarez,
            "\"First, Lespia votes against any motion to sanction Rizia at the Alliance. Hegel is drafting one already, I hear.\"", "MitzOffer2");
        d.Say("MitzOffer2", Alvarez,
            "\"And on top of that, take your pick. A destroyer from my yards in Perla, fresh paint and all. Or everything my naval " +
            "intelligence knows about the Wehzek convoys to Zille.\"", "MitzForgive");
        d.Branch("MitzForgive", "MitzForgiveSay", "MitzEsq");
        d.SayIf("MitzForgiveSay", $"{grudge} and {noDeal}", Alvarez,
            "\"And I'll forget what I said about charging you extra for the strait. Consider it a sign of good faith. Mine, for once.\"",
            "MitzEsq");
        d.Say("MitzEsq", Esquibel,
            "Laurento covered the mouthpiece. \"Our vote decides who owns the Meftiem, Your Majesty. Hegel and Saltana will hear of " +
            "it before dinner.\"", "MitzMenu");

        d.Do("MitzMenu", null, Gate("MitzWhy"), "MitzShip", "MitzIntel", "MitzNo");
        d.Opt("MitzWhy", Not(VarAlvAskedMitzWhy), "\"Why pay so much for one vote?\"", null, T(VarAlvAskedMitzWhy), "MitzWhy2");
        d.Say("MitzWhy2", Alvarez,
            "\"Because the Meftiem is worth a great deal more than one destroyer, Your Majesty. You'd see that, if you were in my " +
            "line of work.\"", "MitzWhy3");
        d.Say("MitzWhy3", Alvarez, "\"Fortunately for me, you're in yours.\"", "MitzMenu");
        d.Opt("MitzShip", null, "\"The destroyer. Rizia will vote with Lespia.\"", "+1 Battle Ship, Binds Rizia's MITZ Vote to Lespia",
            bindMitz + Inc(Ships, 1) + S(VarLespiaMitzReward, "destroyer"), "MitzShip2");
        d.Say("MitzShip2", Alvarez,
            "\"Excellent. She'll sail into Sallabes within the fortnight. Do try not to lose her. She was very expensive.\"", "Hub");
        d.Opt("MitzIntel", null, "\"The convoys. Rizia will vote with Lespia.\"", "Binds Rizia's MITZ Vote to Lespia",
            bindMitz + Talk.Weaken(2) + S(VarLespiaMitzReward, "intel") + T(VarAlvarezGiftDone), "MitzIntel2");
        d.Say("MitzIntel2", Alvarez,
            "\"A practical choice. My naval attaché will have the sailing times on your Admiralty's desk by morning. Accurate to the " +
            "hour, I'm told.\"", "Hub");
        d.Opt("MitzNo", null, "\"Rizia's vote at the Meftiem isn't for sale.\"", null, T(VarAlvRefusedMitz), "MitzNo2");
        d.Say("MitzNo2", Alvarez,
            "\"Everything is for sale, Your Majesty. You simply haven't heard the right price yet.\"", "Hub");

        // ================================================================ invoices: one topic per call once the strait is closed
        d.SayIfDo("InvOpen1", $"{invoices} == 0", Alvarez,
            "\"Your Majesty. My admirals send their regards from the strait. Wehzek captains have discovered the joys of Lespian " +
            "paperwork.\"", Inc(VarAlvInvoices, 1), "FrontI");
        d.SayDo("InvOpen2", Alvarez,
            "\"Your Majesty. A report from Kreleck, since your own admirals are too proud to send one. Every Wehzek ship this month " +
            "was stopped and searched, and most of them are still waiting for their papers.\"", Inc(VarAlvInvoices, 1), "FrontI");
        d.Branch("Inv", "InvMitz", "InvPales", "InvGift", "InvNothingHeld", "InvNothingMissed", "InvNothingWon", "InvNothing");
        // the attaché expelled over the Fort Ales photographs (SupplyWarRoom): he denies everything, smiling
        d.SayIfDo("InvExpelled", $"{Eq(VarPhotoPayoff, "expelled")} and {Not(VarAlvarezPhotoSaid)}", Alvarez,
            "\"Your Majesty. I'm told you expelled my naval attaché. A keen amateur photographer, Captain Olmedo. Harbors, " +
            "lighthouses, that sort of thing.\"", T(VarAlvarezPhotoSaid), "InvExpelled2");
        d.Say("InvExpelled2", Alvarez,
            "\"Whatever he photographed, I assure you he kept it to himself. I tell you so with a perfectly straight face.\"", "FrontI");

        // the Meftiem bargain, if the strait was bought first
        d.SayIfDo("InvMitz", mitzOpen, Alvarez,
            "\"And while I have you, a separate matter. The Meftiem. You didn't think I'd forgotten it?\"", T(Q(2)), "Mitz");

        // the other war: Lespian shipyards selling to Pales
        d.SayIf("InvPales", $"{Is(PalesWar)} and {N(LespianPalesSupport)} > 0 and {V(VarAlvarezPalesDone)} == \"\"", Alvarez,
            "\"You'll read in the papers that a Lespian shipyard has delivered two destroyers to Remaz. Business, Your Majesty.\"",
            "InvPales2");
        d.Say("InvPales2", Alvarez,
            "\"My shipyards sell to anyone who pays on time, and the Duke pays on time. You could always outbid him.\"",
            Gate("PalesPay"), Gate("PalesLeverage"), "PalesIgnore");
        d.Opt("PalesPay", $"{budget} >= 2", "\"Name the figure. Lespia stops arming Pales.\"", "-2 Budget",
            Inc(Budget, -2) + $"{V(LespianPalesSupport)} = 0; " + Inc(NpLespia, 1) + S(VarAlvarezPalesDone, "paid"), "PalesPay2");
        d.Say("PalesPay2", Alvarez,
            "\"Done. The Duke will find our order books suddenly very full. These things happen in shipbuilding.\"", "Hangup");
        d.Opt("PalesLeverage", leverage,
            "\"You'll stop arming Pales for nothing. Or the world reads about Héctor Ramiro.\"", null,
            $"{V(LespianPalesSupport)} = 0; " + Inc(NpLespia, -1) + S(VarAlvarezPalesDone, "leverage"), "PalesLeverage2");
        d.Say("PalesLeverage2", Alvarez,
            "\"You learn quickly, Your Majesty. I don't like it. But very well. Remaz will have to find its destroyers elsewhere.\"", "Hangup");
        d.Opt("PalesIgnore", null, "\"Sell Reinhart whatever you like. Just keep the strait shut.\"", null,
            S(VarAlvarezPalesDone, "ignored"), "PalesIgnore2");
        d.Say("PalesIgnore2", Alvarez,
            "\"Then we understand each other perfectly. It's so rare, in my line of work.\"", "Hangup");

        // a good surprise
        d.SayIf("InvGift", Not(VarAlvarezGiftDone), Alvarez,
            "\"A small gift between partners. My naval attaché has the sailing schedule of every Wehzek convoy leaving the northern " +
            "ports this month.\"", "InvGift2");
        d.Say("InvGift2", Alvarez, "\"Your admirals will find it in their morning post.\"", "GiftWhy", "GiftThanks");
        d.Opt("GiftWhy", null, "\"Why so generous all of a sudden?\"", null, T(VarAlvarezGiftDone) + Talk.Weaken(1), "GiftWhy2");
        d.Say("GiftWhy2", Alvarez,
            "\"Because a short war is cheaper than a long one, Your Majesty. Even for me. Especially for me.\"", "Hangup");
        d.Opt("GiftThanks", null, "\"Thank you, Prime Minister. Rizia won't forget it.\"", null, T(VarAlvarezGiftDone) + Talk.Weaken(1), "GiftThanks2");
        d.Say("GiftThanks2", Alvarez, "\"I'm counting on it.\"", "Hangup");
        d.SayIf("InvNothingHeld", held, Alvarez,
            "\"I only called to ask what my navy is blockading while your army sits still. A quiet front is a very expensive thing to " +
            "blockade for.\"", "Hangup");
        d.SayIf("InvNothingMissed", missed, Alvarez,
            "\"I only called to check on my investment. My parliament reads the same newspapers you do, Your Majesty, and it has " +
            "started asking what exactly it bought.\"", "Hangup");
        d.SayIf("InvNothingWon", won, Alvarez,
            "\"I only called to check on my investment. It seems to be doing nicely. Do finish soon, Your Majesty. My parliament has " +
            "started asking what else it could buy.\"", "Hangup");
        d.Say("InvNothing", Alvarez,
            "\"I only called to check on my investment. Do try to win soon, Your Majesty. My parliament keeps asking what it bought.\"", "Hangup");

        // ================================================================ the Ramiro confrontation
        d.SayIf("ConfrontOpen", confront, Alvarez,
            "\"Your Majesty. Mr. Esquibel tells me you wished to speak to me personally, and urgently. I'm flattered.\"", "C1");
        d.Opt("C1", null, "\"Héctor Ramiro. Your political officer in Zille. Tell me about him.\"", null, null, "C1a");
        d.Say("C1a", Alvarez,
            "\"An interesting conspiracy theory. Did Mr. Smolak tell you this? It sounds very much like his brand.\"", "C2");
        d.Opt("C2", null, "\"Smolak's own police tracked him. Their files were still in Zille when my soldiers walked in.\"", null, null, "C2a");
        d.Say("C2a", Narrator, "For once, the line went quiet. When Alvarez spoke again, the smile had gone out of his voice.", "C3");
        d.Say("C3", Alvarez,
            "\"The truth is a slippery concept, Your Majesty. Here is one that isn't. The enemy is President Smolak and his " +
            "militarist government. Not Lespia.\"", "C3b");
        d.Say("C3b", Alvarez, "\"So tell me what you want.\"", "CDeal", "CNoDeal");
        d.DoIf("CDeal", deal, null, "CRecall", "CPay", "CSendAway");
        d.Do("CNoDeal", null, "CFree", "CPay", "CNever");
        string done = T(VarRamiroConfronted);
        d.Opt("CRecall", null, "\"Recall Ramiro. Send him somewhere I'll never hear his name again.\"", null,
            done + Inc(NpLespia, -1) + S(VarRamiroOutcome, "recalled"), "CRecall2");
        d.Say("CRecall2", Alvarez,
            "\"Mr. Ramiro has been missing his family in Perla for some time. He'll be home by Friday, and he won't be traveling again " +
            "for a very long while.\"", "CEnd");
        d.Opt("CPay", null, "\"Compensation. For the families in Smolak Square, and for my war.\"", "+3 Budget",
            done + Inc(Budget, 3) + Inc(NpLespia, -2) + S(VarRamiroOutcome, "compensated"), "CPay2");
        d.Say("CPay2", Alvarez,
            "\"Lespia is always generous to the victims of terrorism, Your Majesty. Whoever the terrorists turn out to be.\"", "CEnd");
        d.Opt("CSendAway", null, "\"Take your ships out of the strait. I'll fight this war without Lespia.\"", "Ends the Lespian Blockade",
            done + F(VarLespiaStrait) + T(VarLespiaWithdrawn) + Inc(NpLespia, -3) + Strengthen(1) + S(VarRamiroOutcome, "sentaway"), "CSendAway2");
        d.Say("CSendAway2", Alvarez,
            "\"As you wish. I hope your navy is as proud as its king, Your Majesty. It will need to be.\"", "CEnd");
        d.Opt("CFree", null, "\"Close the strait. For nothing. Call it the apology Lespia will never have to make.\"", null,
            done + dealScript + Inc(NpLespia, -2) + S(VarRamiroOutcome, "free"), "CFree2");
        d.Say("CFree2", Alvarez,
            "\"An apology. What a quaint idea. My admirals will be in the strait by Sunday, Your Majesty, and Mr. Ramiro will be " +
            "somewhere very far away.\"", "CEnd");
        d.Opt("CNever", null, "\"I want nothing from you, Prime Minister. Ever.\"", null,
            done + T(VarLespiaWithdrawn) + Inc(NpLespia, -3) + S(VarRamiroOutcome, "sentaway"), "CNever2");
        d.Say("CNever2", Alvarez, "\"Never is a long time in Merkopa, Your Majesty. Good afternoon.\"", "CEnd");
        d.Say("CEnd", Esquibel,
            "Laurento took the receiver from me and set it down gently. \"Three people died in Smolak Square, Your Majesty. I keep " +
            "trying to remember their names.\"", "Hangup2");
        d.Say("Hangup2", Narrator, "He carried the telephone away without another word.", "End");

        d.Branch("Hangup", "Hang1", "Hang2", "Hang3");
        d.SayIf("Hang1", $"{calls} == 1", Narrator, "We said our goodbyes, and Laurento carried the telephone away.", "End");
        d.SayIf("Hang2", $"{calls} == 2", Narrator,
            "Alvarez rang off still chuckling at something he had said. Laurento carried the telephone away without comment.", "End");
        d.Say("Hang3", Narrator,
            "When the line went dead, Laurento wrote the date and the hour in his little notebook. He had started doing that after every " +
            "call from Perla.", "End");
        d.Finish();
    }
}
