namespace ZilleWar;

/// <summary>
/// Prime Minister Alvarez on the telephone. Lespia's navy can close the Strait of Kreleck, Wehlen's sea road to its
/// Zille garrison. His price follows the vanilla ledger:
/// - the MITZ first: no ships unless Rizia votes with Lespia at the Meftiem (vanilla Diplomacy_Lespia_PromiseITZSupport,
///   or the _Late promise for the final vote if the Turn 5 board has already voted; a broken promise costs extra);
/// - then a currency: Aureus gas at his price if Rizia bought the field from Pales (Diplomacy_Pales_GasField_Buy and not
///   cancelled), naval rights at Caleqabiz, or money (cheaper if he already spoke for Rizia, Diplomacy_Lespia_WehlenHelp).
/// Once the strait is closed it weakens the garrison every turn (SupplyWar.NewTurn). Later calls are invoices: another
/// tanker of gas, his shipyards arming Pales in the other war, a free gift, and, if the consulate files were found,
/// the confrontation over Héctor Ramiro and the Zille bombing.
/// </summary>
internal static partial class SupplyWar
{
    private const string VarLespiaMarkup = "ZilleWar.Lespia_Markup";
    private const string VarAlvarezPhotoSaid = "ZilleWar.Alvarez_PhotoSaid";

    private static void RegisterAlvarez()
    {
        SuzerainModdingKit.Variables.Register(VarLespiaMarkup);
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
        string betrayed = $"((({Is(MitzPromise)} and {Is(VoteNationalization)}) or {Is(VarLespiaBetrayedT5)}) and {Not(MitzPromiseLate)})";
        string mitzOk = $"(({Is(MitzPromise)} and not {betrayed}) or {Is(VoteStatusQuo)} or {Is(MitzPromiseLate)} or {Is(VarLespiaMitz)})";
        string help = Is("RiziaDLC.Diplomacy_Lespia_WehlenHelp");
        string markup = Is(VarLespiaMarkup);
        string budget = N(Budget);
        // Lespia's rival Valgsland takes every Lespian favour to Rizia badly.
        string dealScript = $"{V(Count(Lever.Lespia))} = 1; " + T(VarLespiaStrait) + Talk.Weaken(2) + Inc(NpLespia, 1) + Inc(NpWehlen, -1) +
                            Inc(NpValgsland, -1) + SetTurn(VarLespiaDealTurn);
        string bindMitz = $"if {voted} then {T(MitzPromiseLate)} else {T(MitzPromise)} end; " + T(VarLespiaMitz);
        string enter = $"AddConversant(\"{Alvarez}\"); AddConversant(\"{Esquibel}\");";

        d.Start(AlvarezConv, ResetAsked + Inc(VarAlvarezCalls, 1) + Music, "Phone");
        d.Branch("Phone", "PhoneFirst", "PhoneLater");
        d.SayIfDo("PhoneFirst", $"{calls} == 1", Narrator,
            "Laurento was waiting in my study with the blue telephone already on the desk. \"Prime Minister Alvarez, Your Majesty. " +
            "He says he has been very patient.\"", enter, "Open");
        d.SayDo("PhoneLater", Narrator,
            "Laurento brought in the blue telephone and set it down as if it might bite. \"Prime Minister Alvarez again, Your Majesty. " +
            "He sounds pleased with himself, which is never good news.\"", enter, "Open");

        d.Branch("Open", "ConfrontOpen", "HelloKept", "HelloBurned", "HelloAgain", "InvExpelled", "InvOpen");
        d.SayIf("HelloKept", $"{calls} == 1 and {noDeal} and {kept}", Alvarez,
            "\"Your Majesty. I hope my little note didn't get lost in all the excitement in Konstantium. It was a very exciting day.\"", "Jab");
        d.SayIf("HelloBurned", $"{calls} == 1 and {noDeal}", Alvarez,
            "\"Your Majesty. I understand my note met with an accident in Konstantium. No matter. I'm a patient man, and Lespia is a " +
            "practical country.\"", "Jab");
        d.SayIf("HelloAgain", noDeal, Alvarez,
            "\"Your Majesty. Smolak's convoys are still crossing the strait, I see. Have you given any more thought to my offer?\"", "Jab");

        d.Branch("Jab", "JabBlockade", "JabField", "Offer");
        d.SayIf("JabBlockade", Is(BlockDrillship), Alvarez,
            "\"Though I confess I find it amusing. Your warships are still circling my partner's drillship at Aureus, and you'd like " +
            "mine at Kreleck. Rizia has a great many ships in a great many places, for a country that is short of them.\"", "Offer");
        d.SayIf("JabField", field, Alvarez,
            "\"No hard feelings about Aureus, by the way. You outbid LesPower fair and square. Well. Square, at least.\"", "Offer");
        d.Say("Offer", Alvarez,
            "\"Here is my offer. The Lespian navy has the ships to close the Strait of Kreleck. Every Wehzek convoy stopped, searched, " +
            "and held in a Lespian port for as long as the paperwork takes. And the paperwork, I promise you, will take a very long time.\"",
            "OfferHub");
        d.Branch("OfferHub", Gate("Why"), "Price", "NotToday");
        d.Opt("Why", Not(Q(1)), "\"Why would Lespia help Rizia?\"", null, T(Q(1)), "Why2");
        d.Say("Why2", Alvarez,
            "\"Because President Smolak has called me an Arcasian puppet on the radio three times this year, and a Wehlen without " +
            "Zille is a Wehlen with fewer reasons to make speeches about Lespia. And because you will owe me. I'm being honest, you see. " +
            "My lying days are behind me.\"", "OfferHub");
        d.Opt("NotToday", null, "\"Not today, Prime Minister.\"", null, null, "NotToday2");
        d.Say("NotToday2", Alvarez,
            "\"Of course. The strait isn't going anywhere. Neither, unfortunately, are Smolak's convoys.\"", "Hangup");

        // ---- the MITZ comes first
        d.Opt("Price", null, "\"And what does Lespia want in return?\"", null, null, "Mitz");
        d.Branch("Mitz", "MitzBetrayed", "MitzKnown", "MitzAskLate", "MitzAsk");
        d.SayIf("MitzBetrayed", betrayed, Alvarez,
            "\"You looked me in the eye at the Meftiem and voted with the Malenyevists. And now you'd like my navy. Very well. Two " +
            "things, then. Your word on the final vote, and this time I'll hold you to it. And a little more money than I'd charge a friend.\"",
            "LateBetrayedYes", "MitzNo");
        d.Opt("LateBetrayedYes", null, "\"You'll have Rizia's vote when the Meftiem votes again.\"", "Binds Rizia's MITZ Vote to Lespia",
            bindMitz + T(VarLespiaMarkup), "PriceLine");
        d.SayIf("MitzKnown", mitzOk, Alvarez,
            "\"Rizia already stands with Lespia at the Meftiem, so we can skip the tiresome part. I remember my friends, Your Majesty. " +
            "It's my enemies I write down.\"", "PriceLine");
        d.SayIf("MitzAskLate", voted, Alvarez,
            "\"The Meftiem vote went the way it went. There will be another one, and Saltana and Hegel are counting heads already. " +
            "When it comes, Rizia votes with Lespia. That's my first condition. Agreed?\"", "MitzYes", "MitzNo");
        d.Say("MitzAsk", Alvarez,
            "\"Before we talk about ships, we talk about the Meftiem. Saltana and Hegel want to nationalize it, and Lespia wants it " +
            "left exactly as it is. When the board votes, Rizia votes with Lespia. That's my first condition. Agreed?\"", "MitzYes", "MitzNo");
        d.Opt("MitzYes", null, "\"Agreed. Rizia will vote with Lespia.\"", "Binds Rizia's MITZ Vote to Lespia", bindMitz, "PriceLine");
        d.Opt("MitzNo", null, "\"Rizia's vote at the Meftiem isn't for sale.\"", null, null, "MitzNo2");
        d.Say("MitzNo2", Alvarez,
            "\"Everything is for sale, Your Majesty. You simply haven't heard the right price yet. Until then, my admirals stay home.\"", "Hangup");

        // ---- then the price. With the Aureus field bought from Pales, Lespia wants its gas: -6 Energy for +2 Budget every turn,
        //      from the turn the gas flows (Policy_Economy_AureusField_Extractions, set at the Turn 6 deal signing). Without it,
        //      Caleqabiz or money.
        string flowing = Is(Extractions);
        string pending = $"({field} and not {flowing} and {TurnLua} <= 6)";
        string gasPrice = $"({field} and ({flowing} or {TurnLua} <= 6))";
        d.Branch("PriceLine", "PriceGasNow", "PriceGasLater", "PriceHarbor");
        d.SayIf("PriceGasNow", $"{field} and {flowing}", Alvarez,
            "\"Now, the price, and do let me walk you through it slowly, Your Majesty, because I know Rizia is new to this sort of " +
            "thing. You took the Aureus field from under LesPower. Very bold. Now the gas is flowing, and Lespia would like its share. " +
            "Six units every turn, for which LesPower will pay you, oh, the price of two. For as long as you sit on that throne.\"", "EsqGas");
        d.SayIf("PriceGasLater", pending, Alvarez,
            "\"Now, the price. I'll keep it simple for you. Your pipeline to Aureus isn't finished, I know, I have engineers too. " +
            "So my navy closes the strait now, on credit, because I am a generous man. The day your gas starts flowing, six units of " +
            "it come to LesPower every turn, at the price of two. You'll find that's how grown-up countries do business.\"", "EsqGas");
        d.Say("EsqGas", Esquibel,
            "Laurento covered the mouthpiece. \"Six for two, every turn, for the rest of your reign. It's not a price, Your Majesty. " +
            "It's a tribute. But he can close that strait, and nobody else can.\"", "PriceMenuGas");
        d.Say("PriceHarbor", Alvarez,
            "\"Now, the price. I'll keep it simple. My admirals like harbors, and Caleqabiz has a very nice one. A pier, a fuel depot, " +
            "a few hundred sailors for as long as the war lasts. Rizia keeps the island, of course. We would only be guests. You " +
            "understand the concept of guests, I'm sure. Zille had some.\"", "EsqHarbor");
        d.Say("EsqHarbor", Esquibel,
            "Laurento covered the mouthpiece. \"Guests with a lease, Your Majesty. That's how Zille began.\"", "PriceMenu");

        // the gas price: take it, call his bluff with the Ramiro file, or refuse
        d.Branch("PriceMenuGas", Gate("Free"), Gate("GasNow"), Gate("GasLater"), "Refuse");
        d.Opt("GasNow", $"{field} and {flowing}", "\"Six for two. LesPower gets its gas.\"", "-6 Energy Per Turn, +2 Budget Per Turn",
            dealScript + Talk.Weaken(1) + T(VarLespiaGasDeal), "DealGas");
        d.Opt("GasLater", pending, "\"Close the strait. LesPower gets its gas when the pipeline opens.\"",
            "-6 Energy Per Turn, +2 Budget Per Turn Once the Aureus Gas Flows",
            dealScript + Talk.Weaken(1) + T(VarLespiaGasDeal), "DealGas");
        d.Say("DealGas", Alvarez,
            "\"There. That wasn't so painful, was it? My admirals will be in the strait by Sunday, and the Wehzeks will learn what a " +
            "real navy looks like. Do give my regards to your treasury. It's about to become a very good customer of ours.\"", "Hangup");

        // the other currencies
        d.Branch("PriceMenu", Gate("Free"), "Harbor", Gate("Money2"), Gate("Money3"), Gate("Money4"), Gate("Money5"), "Refuse");
        d.Opt("Free", $"{leverage} and {Not(VarRamiroConfronted)}",
            "\"I think you'll close the strait for nothing, Prime Minister. Ask me about Héctor Ramiro.\"", null, null, "Free2");
        d.Say("Free2", Narrator, "There was a pause long enough for me to hear the line hum.", "Free3");
        d.Say("Free3", Alvarez,
            "\"I see Rizia's soldiers have been reading other people's mail in Zille. How very modern of them. Very well, Your Majesty. " +
            "Call it a gesture of friendship. And let's never speak of Mr. Ramiro again, on this line or any other.\"", "FreeDeal");
        d.Do("FreeDeal", dealScript + Inc(NpLespia, -2) + T(VarRamiroConfronted) + S(VarRamiroOutcome, "free"), "Deal2");
        d.Opt("Harbor", null, "\"Your ships may use Caleqabiz for as long as the war lasts.\"", "-1 Authority, Lespian Naval Rights at Caleqabiz",
            dealScript + Inc(Authority, -1) + T(VarLespiaCaleqabiz) + Inc(NpValgsland, -1), "Deal2");
        string money = "\"Not Caleqabiz. I'll pay for your ships instead.\"";
        string paid = dealScript + T(VarLespiaPaid);
        d.Opt("Money2", $"{help} and not {markup} and {budget} >= 2", money, "-2 Budget", paid + Inc(Budget, -2), "Money");
        d.Opt("Money3", $"not {help} and not {markup} and {budget} >= 3", money, "-3 Budget", paid + Inc(Budget, -3), "Money");
        d.Opt("Money4", $"{help} and {markup} and {budget} >= 4", money, "-4 Budget", paid + Inc(Budget, -4), "Money");
        d.Opt("Money5", $"not {help} and {markup} and {budget} >= 5", money, "-5 Budget", paid + Inc(Budget, -5), "Money");
        d.Say("Money", Alvarez,
            "\"Money. How refreshingly direct. Warships are expensive, Your Majesty, and Lespian admirals more so, but I think we can " +
            "make it work. Do try to pay on time. I'd hate for us to have one of those awkward conversations.\"", "Deal2");
        d.Opt("Refuse", null, "\"No. Not at that price.\"", null, null, "NoDeal");
        d.Say("NoDeal", Alvarez,
            "\"Ha. Pride. I remember pride, I had some once, before I had a navy. The offer stands, Your Majesty. For now.\"", "Hangup");
        d.Say("Deal2", Alvarez,
            "\"A pleasure, Your Majesty. My admirals will be in the strait by Sunday. I suspect Mr. Smolak will be on the radio by Monday.\"",
            "Hangup");

        // ---- invoices: one topic per call once the strait is closed
        d.Say("InvOpen", Alvarez,
            "\"Your Majesty. My admirals send their regards from the strait. Wehzek captains have discovered the joys of Lespian " +
            "paperwork, and some of them have taken up fishing instead.\"", "Inv");
        d.Branch("Inv", "InvPales", "InvGas", "InvGift", "InvNothing");
        // the attaché expelled over the Fort Ales photographs (SupplyWarRoom): he denies everything, smiling
        d.SayIfDo("InvExpelled", $"{Eq(VarPhotoPayoff, "expelled")} and {Not(VarAlvarezPhotoSaid)}", Alvarez,
            "\"Your Majesty. I'm told you expelled my naval attaché. A keen amateur photographer, Captain Olmedo. Harbors, lighthouses, " +
            "that sort of thing. Whatever he photographed, I assure you he kept it to himself, and I tell you so with a perfectly straight " +
            "face. Now. My admirals send their regards from the strait, a few less of them than last month.\"", T(VarAlvarezPhotoSaid), "Inv");

        // the other war: Lespian shipyards selling to Pales
        d.SayIf("InvPales", $"{Is(PalesWar)} and {N(LespianPalesSupport)} > 0 and {V(VarAlvarezPalesDone)} == \"\"", Alvarez,
            "\"You'll read in the papers that a Lespian shipyard has delivered two destroyers to Remaz. Business, Your Majesty. " +
            "My shipyards sell to anyone who pays on time, and the Duke pays on time. You could always outbid him.\"",
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

        // the gas, again
        d.SayIf("InvGas", "false", Alvarez,   // the gas is a standing per-turn contract now; no extra shipments
            "\"LesPower's engineers tell me the Aureus wells produce rather more than you admitted. One more shipment at the same " +
            "friendly price, and my patrols stay exactly as thick as they are.\"", Gate("GasYes"), "GasNo");
        d.Opt("GasYes", $"{N(Energy)} >= 3", "\"One more shipment. Then we're square.\"", "-3 Energy, +1 Budget",
            Inc(Energy, -3) + Inc(Budget, 1) + S(VarAlvarezGasDone, "paid"), "GasYes2");
        d.Say("GasYes2", Alvarez, "\"Square. What a lovely word. I'll have it engraved on something.\"", "Hangup");
        d.Opt("GasNo", null, "\"The deal was six units, Prime Minister. Not six and then some.\"", null,
            S(VarAlvarezGasDone, "refused") + T(VarStraitThin) + Inc(NpLespia, -1), "GasNo2");
        d.Say("GasNo2", Alvarez,
            "\"Then my patrols will be a little thinner this month. Ships need fuel, Your Majesty. Ironic, isn't it?\"", "Hangup");

        // a good surprise
        d.SayIf("InvGift", Not(VarAlvarezGiftDone), Alvarez,
            "\"A small gift between partners. My naval attaché has the sailing schedule of every Wehzek convoy leaving the northern " +
            "ports this month. Your admirals will find it in their morning post.\"", "GiftWhy", "GiftThanks");
        d.Opt("GiftWhy", null, "\"Why so generous all of a sudden?\"", null, T(VarAlvarezGiftDone) + Talk.Weaken(1), "GiftWhy2");
        d.Say("GiftWhy2", Alvarez,
            "\"Because a short war is cheaper than a long one, Your Majesty. Even for me. Especially for me.\"", "Hangup");
        d.Opt("GiftThanks", null, "\"Thank you, Prime Minister. Rizia won't forget it.\"", null, T(VarAlvarezGiftDone) + Talk.Weaken(1), "GiftThanks2");
        d.Say("GiftThanks2", Alvarez, "\"I'm counting on it.\"", "Hangup");
        d.Say("InvNothing", Alvarez,
            "\"I only called to check on my investment. Do try to win soon, Your Majesty. My parliament keeps asking what it bought.\"", "Hangup");

        // ---- the Ramiro confrontation
        d.SayIf("ConfrontOpen", confront, Alvarez,
            "\"Your Majesty. Mr. Esquibel tells me you wished to speak to me personally, and urgently. I'm flattered.\"", "C1");
        d.Opt("C1", null, "\"Héctor Ramiro. Your political officer in Zille. Tell me about him.\"", null, null, "C1a");
        d.Say("C1a", Alvarez,
            "\"An interesting conspiracy theory. Did Mr. Smolak tell you this? It sounds very much like his brand.\"", "C2");
        d.Opt("C2", null, "\"Smolak's own police tracked him. Their files were still in Zille when my soldiers walked in.\"", null, null, "C2a");
        d.Say("C2a", Narrator, "For once, the line went quiet. When Alvarez spoke again, the smile had gone out of his voice.", "C3");
        d.Say("C3", Alvarez,
            "\"The truth is a slippery concept, Your Majesty. Here is one that isn't: the enemy is President Smolak and his militarist " +
            "government. Not Lespia. So tell me what you want.\"", "CDeal", "CNoDeal");
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

        d.Say("Hangup", Narrator, "We said our goodbyes, and Laurento carried the telephone away.", "End");
        d.Finish();
    }
}
