namespace ZilleWar;

/// <summary>
/// Queen Beatrice on the blue telephone, once per war turn until she says no. A greeting, then the King's requests, each
/// with a short exchange and a clearly priced answer:
/// - The oil embargo, priced in Rizian gas exactly like her Turn 3 offer; never offered if Turn 3 already cut Wehlen off.
/// - One Rumburgian war credit: +6 Budget now, 8 Budget repaid over the turns left (-2 Budget per turn for 4 turns).
///   Shown through WorldModifiers' repayment line.
/// - Bludish Freedom Front raids and fighters (vanilla Diplomacy_Wehlen_FundBFF, BFF mercenaries).
/// Every favour costs patience (VarRumburgPatience); Wehlen's "Thornbourgh dossier" and a second war cost more.
/// At zero she refuses and asks for an end date.
/// No two calls run alike: the telephone, her opening and the King's answer follow what the front did since the last call
/// (Zille or Auremina taken, an aim missed, more cities than planned, a month held), then she brings up what came of the
/// last call (the favour she granted, the King's promise at the goodbye, the Bluds' massacre), asks again in a different way
/// for what he once refused, and closes and hangs up differently each time.
/// </summary>
internal static partial class SupplyWar
{
    private static void RegisterBeatrice()
    {
        var d = new Dlg(BeatriceConv, "ZilleWar.Beatrice.");
        string calls = N(VarBeatriceCalls);
        string pat = N(VarRumburgPatience);
        string first = $"{calls} == 1";
        string high = Is(HighExport);
        string avg = Is(AverageExport);
        string failed = Is(ExportFailed);
        string dossier = Is(VarDossier);
        string twoWars = Is(PalesWar);
        string credits = N(VarRumburgCredits);
        string raids = N(Count(Lever.BffRaids));
        string fighters = N(Count(Lever.BffFighters));
        string turn = TurnLua;

        // ---- the war since her last call (SupplyWar.PrepareCallState) and what was said on it
        string won = CallKind("won");            // the aim taken, nothing lost
        string missed = CallKind("missed");      // nothing taken, nothing lost
        string held = Is(VarCallHeld);
        string aim(string a) => Eq(VarCallLastAim, a);
        string zille = Is(ZilleOperation.CapturedVar("Zille"));
        string again = $"{N(VarCallFought)} >= 2";
        string gave(string g) => Eq(VarBeaGavePrev, g);
        string promised(string p) => Eq(VarBeaPromisePrev, p);
        string remember = $"{V(VarBeaGavePrev)} = {V(VarBeaGave)} or \"\"; {V(VarBeaGave)} = \"none\"; " +
                          $"{V(VarBeaPromisePrev)} = {V(VarBeaPromise)} or \"\"; {V(VarBeaPromise)} = \"\"; ";

        d.Start(BeatriceConv, ResetAsked + Inc(VarBeatriceCalls, 1) + Music + remember, "Phone");
        d.Branch("Phone", "Phone1", "Phone2", "Phone3");
        d.SceneIf("Phone1", first,
            "An assistant brought in the blue telephone. The line to Thornbourgh crackled twice, and then the Queen of Rumburg was on it.",
            null, $"AddConversant(\"{Beatrice}\");", "Open");
        d.SceneIf("Phone2", $"{calls} == 2",
            "Thornbourgh rang before I could ring Thornbourgh. The assistant who brought in the blue telephone looked as if he had been " +
            "told to run, and had.", null, $"AddConversant(\"{Beatrice}\");", "Open");
        d.Scene("Phone3",
            "The blue telephone again. The operators in Thornbourgh knew my voice by now, and the Queen knew to the second how long to " +
            "let it ring.", null, $"AddConversant(\"{Beatrice}\");", "Open");

        // ---- openings: the first call, then whatever the front did since the last one
        // Every other outcome of the last phase, naming the city: one port of two, another city than the aim, a city lost.
        var outcomes = new List<string>();
        foreach ((string got, string other) in new[] { ("Nanta", "Isor"), ("Isor", "Nanta") })
        {
            outcomes.Add("HelloPart" + got);
            d.SayIf("HelloPart" + got, $"{CallKind("partial")} and {Eq(VarCallGain, got)}", Beatrice,
                $"\"Romus. {got}, my Exchequer tells me, and not {other}. They asked whether half a coastline counts as a victory. I told " +
                "them it counts as half of one.\"", "GreetWon");
        }

        foreach (string c in CallPlaces)
        {
            string id = c.Replace(" ", "");
            bool city = IsZilleCity(c);
            outcomes.Add("HelloOther" + id);
            d.SayIf("HelloOther" + id, $"{CallKind("other")} and {Eq(VarCallGain, c)}", Beatrice, city
                ? $"\"Romus. The papers say your army went for one city and came home with {c}. My ministers call it luck. I call it initiative.\""
                : $"\"Romus. {c} is Rizian again, I read. Good. A king should never have to ask twice for his own towns.\"", "GreetWon");
            outcomes.Add("HelloWonLoss" + id);
            d.SayIf("HelloWonLoss" + id, $"{CallKind("wonLoss")} and {Eq(VarCallLoss, c)}", Beatrice,
                $"\"Romus. You took what you went for, and lost {c} the same week. My ministers have stopped reading the war news past the " +
                "first column.\"", "GreetMissed");
            outcomes.Add("HelloMixed" + id);
            d.SayIf("HelloMixed" + id, $"{CallKind("mixed")} and {Eq(VarCallLoss, c)}", Beatrice,
                $"\"Romus. Ground won in one place and {c} lost in another. My Exchequer drew me a chart of your war. It looks like a seesaw.\"",
                "GreetMissed");
            outcomes.Add("HelloLost" + id);
            d.SayIf("HelloLost" + id, $"{CallKind("lost")} and {Eq(VarCallLoss, c)}", Beatrice, city
                ? $"\"Romus. The Wehzek radio says Smolak has {c} back. I'd rather have heard it from you, and rather not have heard it at all.\""
                : $"\"Romus. Wehzek soldiers in {c}. On Rizian soil. My ministers ask whether you can still keep what you already had.\"",
                "GreetMissed");
        }

        d.Branch("Open", new[] { "NoMore", "Hello1", "HelloHeld", "HelloExtra", "HelloZilleLate", "HelloZille", "HelloAuremina" }
            .Concat(outcomes)
            .Concat(new[] { "HelloDossier", "HelloMissedZ2", "HelloMissedZ", "HelloMissedA", "HelloMissedP", "HelloMissed", "HelloTwoWars", "HelloLater" })
            .ToArray());
        d.SayIf("Hello1", first, Beatrice,
            "\"Romus. I was beginning to think you'd lost my number. Then I remembered you've started a war, and I forgave you.\"",
            "Greet1", "Greet2", "Greet3");
        d.Opt("Greet1", null, "\"It's good to hear your voice, Beatrice.\"", null, null, "Greet1a");
        d.Say("Greet1a", Beatrice, "\"Is it? Then the war must be going worse than the newspapers say.\"", "Ask1");
        d.Opt("Greet2", null, "\"I haven't much time, Beatrice.\"", null, null, "Greet2a");
        d.Say("Greet2a", Beatrice, "\"Nobody at war ever does. Very well, I'll be brief. It's one of my talents.\"", "Ask1");
        d.Opt("Greet3", null, "\"How is Thornbourgh taking the news?\"", null, null, "Greet3a");
        d.Say("Greet3a", Beatrice, "\"With tea, as it takes everything. My ministers are less calm.\"", "Greet3b");
        d.Say("Greet3b", Beatrice, "\"They've started asking what Rumburg will be asked to pay for.\"", "Ask1");
        d.Branch("Ask1", "Ask1Embargo", "Ask1Plain");
        d.SayIf("Ask1Embargo", high, Beatrice,
            "\"Rumburg stopped buying Wehzek oil months ago, by the way. Smolak is paying for his war out of his own pocket. You're welcome.\"",
            "Ask1Plain");
        d.Say("Ask1Plain", Beatrice, "\"So. Tell me what you need, and I'll tell you what it costs.\"", "Hub");

        d.SayIf("HelloHeld", held, Beatrice,
            "\"Romus. A whole month without a shot fired. My ministers were beginning to hope you'd come to your senses. I told them not " +
            "to be silly.\"", "GreetHeld");
        d.SayIf("HelloExtra", $"{won} and {Is(VarCallExtra)}", Beatrice,
            "\"Romus. I'm told your army didn't stop where it was told to. More cities than it was sent for. Even my Exchequer was " +
            "impressed, and my Exchequer is never impressed.\"", "GreetWon");
        d.SayIf("HelloZilleLate", $"{won} and {aim("Harbor")} and {again}", Beatrice,
            "\"Zille, at the second attempt. The Thornbourgh Gazette printed your flag over the harbor this morning, and my ministers " +
            "have stopped calling the whole thing a gamble. They call it an investment now. It's how they apologize.\"", "GreetWon");
        d.SayIf("HelloZille", $"{won} and {aim("Harbor")}", Beatrice,
            "\"Well, well. Zille. Your father's lease on the harbor, and Smolak sitting on it. I had the Gazette's photograph of " +
            "your flag over the harbor cut out for my desk. Don't let it go to your head.\"", "GreetWon");
        d.SayIf("HelloAuremina", $"{won} and {aim("Wall")}", Beatrice,
            "\"Auremina, Romus. The gold hills. My Exchequer sent me a note this morning asking whether Rizia's credit had just improved. " +
            "I told them it depends entirely on whether you keep them.\"", "GreetWon");
        d.SayIf("HelloMissedZ2", $"{missed} and {aim("Harbor")} and {again}", Beatrice,
            "\"Twice, Romus. Twice your army has gone down that river, and Zille still flies Smolak's flag. My ministers have " +
            "stopped asking when it will fall. They ask whether.\"", "GreetMissed");
        d.SayIf("HelloMissedZ", $"{missed} and {aim("Harbor")}", Beatrice,
            "\"Romus. I read that Zille is still flying Smolak's flag. The papers here call it a setback. My ministers call it " +
            "something shorter.\"", "GreetMissed");
        d.SayIf("HelloMissedA", $"{missed} and {aim("Wall")}", Beatrice,
            "\"Romus. I hear the hills around Auremina held. The Wehzek radio has been unbearable about it all week.\"", "GreetMissed");
        d.SayIf("HelloMissedP", $"{missed} and {aim("Ports")}", Beatrice,
            "\"Romus. I hear the ports held. Smolak's ships are still unloading at them, and the Wehzek radio has made a song of it.\"", "GreetMissed");
        d.SayIf("HelloMissed", missed, Beatrice,
            "\"Romus. The Wehzek radio is very cheerful this week, which I take personally.\"", "GreetMissed");
        d.SayIfDo("HelloDossier", $"{dossier} and {Not(VarBeaToldDossier)}", Beatrice,
            "\"I assume you've read Wraclawitz's little pamphlet. Every newspaper in GRACE has. Twice.\"", T(VarBeaToldDossier), "GreetL");
        d.SayIf("HelloTwoWars", twoWars, Beatrice,
            "\"Two wars, Romus. My ministers have started asking which one you mean to finish first, and they've stopped being sure it's the one you're winning.\"", "GreetL");
        d.Say("HelloLater", Beatrice, "\"Romus. Still at war, I see.\"", "GreetL");

        // The King's answer to whatever she opened with.
        d.Do("GreetWon", null, "GreetW1", "GreetW2");
        d.Opt("GreetW1", null, "\"It was the army's doing, not mine.\"", null, null, "GreetW1a");
        d.Say("GreetW1a", Beatrice, "\"Modesty. You must be exhausted. Take the credit, Romus, before one of your generals does.\"", "Recall");
        d.Opt("GreetW2", null, "\"It isn't over yet.\"", null, null, "GreetW2a");
        d.Say("GreetW2a", Beatrice, "\"They never are, at this stage. That's what makes them so expensive.\"", "Recall");
        d.Do("GreetMissed", null, "GreetM1", "GreetM2");
        d.Opt("GreetM1", null, "\"A setback. The next phase goes in on the same ground.\"", null, null, "GreetM1a");
        d.Say("GreetM1a", Beatrice,
            "\"Good. Say it exactly like that to the newspapers, and leave out the part where you sound tired.\"", "Recall");
        d.Opt("GreetM2", null, "\"Smolak's radio is celebrating too early.\"", null, null, "GreetM2a");
        d.Say("GreetM2a", Beatrice, "\"Radio always does. It's the only thing Wraclawitz ever delivers on time.\"", "Recall");
        d.Do("GreetHeld", null, "GreetH1", "GreetH2");
        d.Opt("GreetH1", null, "\"The men needed the rest.\"", null, null, "GreetH1a");
        d.Say("GreetH1a", Beatrice, "\"So did Smolak's. I do hope you both enjoyed it.\"", "Recall");
        d.Opt("GreetH2", null, "\"It was a pause, Beatrice. Not a change of heart.\"", null, null, "GreetH2a");
        d.Say("GreetH2a", Beatrice, "\"I never suspected you of a change of heart, my dear. Only of a change of plan.\"", "Recall");
        d.Do("GreetL", null, "GreetL1", "GreetL2");
        d.Opt("GreetL1", null, "\"For a little while longer.\"", null, null, "GreetL1a");
        d.Say("GreetL1a", Beatrice, "\"I read the newspapers, Romus. Even the Wehzek ones, when I want a laugh.\"", "Recall");
        d.Opt("GreetL2", null, "\"How are your ministers?\"", null, null, "GreetL2a");
        d.Say("GreetL2a", Beatrice, "\"Counting. They're always counting. Lately, they count Rizia.\"", "Recall");

        // What came of the last call: a promise the King made, or the favour she granted.
        d.Branch("Recall", "RecZilleKept", "RecZilleForgiven", "RecZilleBroken", "RecEnd", "RecBludsTrial", "RecBludsKept", "RecBludsHome", "RecEmbargo",
            "RecCredit", "RecRaids", "RecFighters", "RecNone", "AskL");
        d.SayIf("RecZilleKept", $"{promised("zille")} and {zille} and {calls} == 2", Beatrice,
            "\"You promised to ring me yourself the day Zille was yours. I notice it was I who rang you. I'll let it pass, this once.\"",
            "AskL");
        d.SayIf("RecZilleForgiven", $"{promised("zille")} and {zille}", Beatrice,
            "\"You promised to ring me the day Zille was yours. Now I hear it from the Gazette. I'll let it pass, this once.\"",
            "AskL");
        d.SayIf("RecZilleBroken", $"{promised("zille")} and not {zille}", Beatrice,
            "\"You told me you'd ring the day Zille was yours. It still isn't, Romus. I'll hold you to it.\"", "AskL");
        d.SayIf("RecEnd", promised("end"), Beatrice,
            "\"And you promised me the end of it last time. This doesn't sound like the end of it. It sounds like the middle.\"", "AskL");
        d.SayIfDo("RecBludsTrial", $"{Eq(VarBludsIncident, "trial")} and {Not(VarBeaToldBluds)}", Beatrice,
            "\"And you put my Bluds on trial in front of the entire foreign press. I recommended those men, Romus. I feel recommended " +
            "against.\"", T(VarBeaToldBluds), "AskL");
        d.SayIfDo("RecBludsKept", $"{Eq(VarBludsIncident, "kept")} and {Not(VarBeaToldBluds)}", Beatrice,
            "\"I've seen the photographs from that village above Zille. So has everyone. I'd have sent the men home, if only to stop " +
            "the newspapers taking a second look at who sent them.\"", T(VarBeaToldBluds), "AskL");
        d.SayIfDo("RecBludsHome", $"{Eq(VarBludsIncident, "home")} and {Not(VarBeaToldBluds)}", Beatrice,
            "\"I hear you sent my Bluds home on a night train, with the blinds down. Quietly. You're learning.\"", T(VarBeaToldBluds), "AskL");
        d.SayIf("RecEmbargo", $"{gave("embargo")} and {SupplyWar.LuaOilCut}", Beatrice,
            "\"Smolak's tankers are turning back in the Markian Sea, by the way. I'm told he's rationing petrol in Wraclawitz. I sleep " +
            "beautifully.\"", "AskL");
        d.SayIf("RecCredit", gave("credit"), Beatrice,
            "\"Your eighteen billion arrived safely, I'm told. My Exchequer would like to believe it wasn't all spent on shells. I've " +
            "told them not to be naive.\"", "AskL");
        d.SayIf("RecRaids", gave("raids"), Beatrice,
            "\"The Bluds kept their side of it, by the way. Half a Wehzek harbor is still smoking, and Wehzek radio blames Rizia, as " +
            "arranged.\"", "AskL");
        d.SayIf("RecFighters", gave("fighters"), Beatrice,
            "\"And how are my Bluds behaving? No, don't tell me. I'd rather read it in the newspapers, where at least it's well written.\"",
            "AskL");
        d.SayIf("RecNone", gave("none"), Beatrice,
            "\"You asked me for nothing last time. I spent a week wondering what you were hiding.\"", "AskL");
        d.Branch("AskL", "AskL2", "AskL3");
        d.SayIf("AskL2", $"{calls} <= 2", Beatrice, "\"So. What does Rizia need from me this month?\"", "Hub");
        d.Say("AskL3", Beatrice, "\"Go on, then. I can hear the shopping list from here.\"", "Hub");

        // ---- the hub: every request once per call, and only while she still has patience for favours
        d.Branch("Hub", Gate("AskEmbargo"), Gate("AskCredit"), Gate("AskRaids"), Gate("AskFighters"), Gate("ByeZille"), Gate("ByeEnd"), Gate("Bye"),
            "ByeLater");
        // After each favour: if that was the last one she'll grant, she says so once.
        d.Branch("Back", "Spent", "Hub");
        d.SayIfDo("Spent", $"{pat} <= 0 and {Not(Q(9))}", Beatrice,
            "\"And that, Romus, is the last favour you'll have from Rumburg for a while. Do spend it wisely.\"", T(Q(9)), "Hub");

        // 1. the oil, if the Turn 3 deal did not already cut Wehlen off
        d.Opt("AskEmbargo", $"not {high} and not {failed} and {pat} > 0 and {Not(Q(1))}",
            "\"Stop buying Wehzek oil. Every barrel pays for another convoy to Zille.\"", null, T(Q(1)), "EmbAgain", "EmbAvg", "EmbNone");
        d.SayIf("EmbAgain", Is(VarBeaNoEmbargo), Beatrice,
            "\"Back to my oil, are we? The terms haven't changed since you turned them down. My furnaces haven't grown any less hungry.\"",
            "EmbMenu");
        d.SayIf("EmbAvg", avg, Beatrice,
            "\"We've already halved it. If I cut Smolak off entirely, I'll need more of your gas to keep the Steelfield lit.\"",
            "EmbMenu");
        d.Say("EmbNone", Beatrice,
            "\"That deal was on the table in your own stateroom, and nothing came of it. If memory serves. And it does.\"", "EmbNone2");
        d.Say("EmbNone2", Beatrice,
            "\"The terms haven't changed. Your gas keeps my furnaces lit, and Smolak gets nothing from me.\"", "EmbMenu");
        d.Do("EmbMenu", null, Gate("EmbWhy"), Gate("EmbYes"), Gate("EmbAvgYes"), "EmbNo");
        d.Opt("EmbWhy", Not(VarBeaAskedEmbWhy), "\"Couldn't Rumburg do it for Rizia's sake?\"", null, T(VarBeaAskedEmbWhy), "EmbWhy2");
        d.Say("EmbWhy2", Beatrice,
            "\"Rumburg does a great many things for Rizia's sake, Romus. It does them for Rumburg's sake first.\"", "EmbMenu");
        d.Opt("EmbYes", $"not {avg}", "\"Done. Rizia's gas for your oil.\"", "-3 Energy, -2 Energy Per Turn, +2 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -3) + T("RiziaDLC.Policy_Diplomacy_TradeDeal_Rumburg") +
            Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -2) + Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 2) + T(HighExport) +
            Inc(NpWehlen, -2) + Inc(NpRumburg, 1) + Inc(VarRumburgPatience, -1) + S(VarBeaGave, "embargo"), "Emb2");
        d.Opt("EmbAvgYes", avg, "\"Cut him off completely. Rizia will make up the difference.\"", "-1 Energy, -1 Energy Per Turn, +1 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -1) + Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -1) +
            Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 1) + F(AverageExport) + T(HighExport) + Inc(NpWehlen, -1) +
            Inc(VarRumburgPatience, -1) + S(VarBeaGave, "embargo"), "Emb2");
        d.Say("Emb2", Beatrice, "\"Settled. Smolak's treasury will be lighter by spring.\"", "Emb3");
        d.Say("Emb3", Beatrice, "\"And my ministers louder. Don't make me regret it.\"", "Back");
        d.Opt("EmbNo", null, "\"Not at that price.\"", null, T(VarBeaNoEmbargo) + Inc(VarBeaNoEmbargoCount, 1), "EmbNo2");
        d.Branch("EmbNo2", "EmbNoAgain", "EmbNoFirst");
        d.SayIf("EmbNoAgain", $"{N(VarBeaNoEmbargoCount)} >= 2", Beatrice, "\"Twice now. I'll stop offering, Romus. Eventually.\"", "Hub");
        d.Say("EmbNoFirst", Beatrice, "\"Sensible. Dull, but sensible.\"", "Hub");

        // 2. one war credit: +6 Budget now, 8 repaid over the turns that are left
        d.Opt("AskCredit", $"{credits} == 0 and {turn} <= 6 and {pat} > 0 and {Not(Q(2))}",
            "\"The treasury is bleeding, Beatrice. Could Rumburg lend us money?\"", null, T(Q(2)), "CreditAgain", "Credit");
        d.SayIf("CreditAgain", Is(VarBeaNoCredit), Beatrice,
            "\"Changed your mind about the money? My Exchequer said you would. They're insufferable when they're right. Eighteen billion " +
            "guilders, same as before.\"", "CreditTerms");
        d.Say("Credit", Beatrice, "\"Lend. What a charming word.\"", "Credit2");
        d.Say("Credit2", Beatrice,
            "\"Very well. A war credit in Rizia's name. Eighteen billion guilders now, from my Exchequer to yours. Pocket money, between family.\"", "CreditTerms");
        d.Branch("CreditTerms", "TermsMid", "TermsShort");
        d.SayIf("TermsMid", $"{turn} <= 6", Beatrice,
            "\"Repaid over the next few seasons. My Exchequer insists on being paid before your reign gets interesting in other ways.\"",
            "CreditMenu");
        d.Say("TermsShort", Beatrice,
            "\"Repaid quickly, I'm afraid. Your reign is getting on, and my Exchequer has a nasty habit of reading calendars.\"",
            "CreditMenu");
        d.Do("CreditMenu", null, Gate("CreditWhy"), Gate("Credit4"), "CreditNo");
        d.Opt("CreditWhy", Not(VarBeaAskedInterest), "\"And the interest?\"", null, T(VarBeaAskedInterest), "CreditWhy2");
        d.Say("CreditWhy2", Beatrice,
            "\"Family rates, Romus. Twenty-four billion back for every eighteen lent. Which is to say, higher than you'd like and lower than you deserve.\"", "CreditMenu");
        string creditScript = Inc(Budget, 6) + Inc(VarRumburgCredits, 1) + $"{V(VarRumburgDebt)} = 8; " + Inc(VarRumburgPatience, -1) +
                              S(VarBeaGave, "credit");
        string Repay(int rate, int turns) =>
            creditScript + $"{V(ModRumburgCredit)} = {-rate}; {V(ModRumburgCredit + "_Turns")} = {turns}; ";
        const string drawUp = "\"Draw it up.\"";
        d.Opt("Credit4", $"{turn} <= 6", drawUp, "+6 Budget, -2 Budget Per Turn for 4 Turns", Repay(2, 4), "CreditDone");
        d.Say("CreditDone", Beatrice, "\"The papers will be in Porte Drazon by Friday.\"", "CreditDone2");
        d.Say("CreditDone2", Beatrice, "\"Do read the small print. Rumburg always collects.\"", "Back");
        d.Opt("CreditNo", null, "\"Rizia doesn't borrow to fight.\"", null, T(VarBeaNoCredit), "CreditNo2");
        d.Say("CreditNo2", Beatrice, "\"Then Rizia is richer than its treasury looks. Good for Rizia.\"", "Hub");

        // 3. the Bludish Freedom Front on the Wehzek ports
        d.Opt("AskRaids", $"{raids} < 2 and not {dossier} and {pat} > 0 and {Not(Q(3))}",
            "\"Your friends in the Bludish Freedom Front. Can they hit Wehlen's ports?\"", null, T(Q(3)), "RaidRefused", "Raid1", "RaidAgain");
        d.SayIf("RaidRefused", $"{raids} == 0 and {Is(VarBeaNoRaids)}", Beatrice,
            "\"Back to the Bluds? I thought your scruples had settled that. Scruples so rarely last a whole war.\"", "RaidMenu");
        d.SayIf("Raid1", $"{raids} == 0", Beatrice,
            "\"Friends is a strong word. They're violent, unreliable and outlawed in half of Merkopa.\"", "Raid1b");
        d.Say("Raid1b", Beatrice, "\"They're also very good at setting fire to things.\"", "RaidMenu");
        d.Say("RaidAgain", Beatrice,
            "\"Again? Wehzek radio already blames you for the last time. Same arrangement?\"", "RaidMenu");
        d.Do("RaidMenu", null, Gate("RaidWhy"), "RaidYes", "RaidNo");
        d.Opt("RaidWhy", Not(VarBeaAskedRaidWhy), "\"And if it comes out that Rizia asked?\"", null, T(VarBeaAskedRaidWhy), "RaidWhy2");
        d.Say("RaidWhy2", Beatrice, "\"Then Sordland will scream the house down.\"", "RaidWhy3");
        d.Say("RaidWhy3", Beatrice, "\"And I shall be shocked. Simply shocked, along with everyone else.\"", "RaidMenu");
        d.Opt("RaidYes", null, "\"Have them strike the ports.\"", "-1 Budget",
            Inc(Count(Lever.BffRaids), 1) + Talk.Weaken(1) + Inc(Budget, -1) + T("RiziaDLC.Diplomacy_Wehlen_FundBFF") +
            Inc(NpSordland, -1) + Inc(NpValgsland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; " + S(VarBeaGave, "raids"), "Raid3");
        d.Branch("Raid3", "Raid3Again", "Raid3First");
        d.SayIf("Raid3Again", $"{raids} >= 2", Beatrice,
            "\"Done. If anyone asks, this call was about the weather in Thornbourgh. It's always dreadful, so nobody ever checks.\"", "Back");
        d.Say("Raid3First", Beatrice, "\"Consider it done. Should anyone ask, this call was about my niece's birthday.\"", "Back");
        d.Opt("RaidNo", null, "\"No. Not like that.\"", null, T(VarBeaNoRaids), "RaidNo2");
        d.Say("RaidNo2", Beatrice, "\"Scruples. How very Rizian of you.\"", "Hub");

        // 4. Bludish fighters for the Royal Army
        d.Opt("AskFighters", $"{fighters} < 1 and not {dossier} and {pat} > 0 and {Not(Q(4))}",
            "\"I could use more men. Would the Bluds fight for Rizia?\"", null, T(Q(4)), "FigAgain", "Fig1");
        d.SayIf("FigAgain", Is(VarBeaNoFighters), Beatrice,
            "\"The Bluds again? They're still for hire. They always are. Two companies, gold up front, and your officers will still " +
            "loathe them.\"", "FigMenu");
        d.Say("Fig1", Beatrice, "\"For money, they'd fight for anyone who isn't Smolak.\"", "Fig1b");
        d.Say("Fig1b", Beatrice, "\"Two companies. Hard men, paid in gold. Your officers will loathe them.\"", "FigMenu");
        d.Do("FigMenu", null, Gate("FigWhy"), "FigYes", "FigNo");
        d.Opt("FigWhy", Not(VarBeaAskedFigWhy), "\"Will they take orders?\"", null, T(VarBeaAskedFigWhy), "FigWhy2");
        d.Say("FigWhy2", Beatrice, "\"From whoever pays them, on the days they feel like it.\"", "FigWhy3");
        d.Say("FigWhy3", Beatrice, "\"Keep a Rizian officer near them at all times. Preferably a large one.\"", "FigMenu");
        d.Opt("FigYes", null, "\"Send them.\"", "-2 Budget",
            Inc(Count(Lever.BffFighters), 1) + Inc(Mercs, 2) + Inc(Budget, -2) + T("RiziaDLC.Resources_Military_RumburgianBFFMercenaries") +
            T("RiziaDLC.Diplomacy_Wehlen_FundBFF") + Inc(NpSordland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; " + S(VarBeaGave, "fighters"), "Fig2");
        d.Say("Fig2", Beatrice, "\"They'll be in Montaklar within the week. Keep them on a short leash. They bite.\"", "Back");
        d.Opt("FigNo", null, "\"Rizia will fight its own war.\"", null, T(VarBeaNoFighters), "FigNo2");
        d.Say("FigNo2", Beatrice, "\"Admirable. Let's hope it's also enough.\"", "Hub");

        // goodbye: the King may leave her a promise, which she remembers next time
        d.Opt("ByeZille", $"not {zille} and {pat} > 0", "\"I'll ring you myself the day Zille is ours.\"", null, S(VarBeaPromise, "zille"),
            "ByeZille2");
        d.Say("ByeZille2", Beatrice, "\"I'll hold you to that. I hold everyone to everything. It saves a great deal of time.\"", "Hangup");
        d.Opt("ByeEnd", $"{zille} and {pat} > 0 and {calls} >= 2", "\"The next time I ring, it will be to tell you it's over.\"", null,
            S(VarBeaPromise, "end"), "ByeEnd2");
        d.Say("ByeEnd2", Beatrice, "\"Then I'll make a point of answering it myself.\"", "Hangup");
        d.Opt("Bye", first, "\"That's all for now, Beatrice.\"", null, null, "ByeBranch");
        d.Opt("ByeLater", $"not ({first})", "\"Thank you, Beatrice. That will be all.\"", null, null, "ByeBranch");
        d.Branch("ByeBranch", "ByeLast", "Bye1", "ByeHeld", "ByeMissed", "Bye2", "Bye3");
        d.SayIf("ByeLast", $"{pat} <= 0", Beatrice, "\"Don't ring me again unless it's to tell me it's over.\"", "Hangup");
        d.SayIf("Bye1", first, Beatrice,
            "\"Go and win your war. And try not to get shot. Rumburg has far too much invested in you.\"", "Hangup");
        d.SayIf("ByeHeld", held, Beatrice,
            "\"Then go and wake your army up. A month's rest is a holiday. Two is a habit.\"", "Hangup");
        d.SayIf("ByeMissed", missed, Beatrice,
            "\"Then go and do better this month, Romus. And do eat something. You sound like a man living on coffee and casualty lists.\"",
            "Hangup");
        d.SayIf("Bye2", $"{calls} == 2", Beatrice,
            "\"Then I'll leave you to your maps. Give Vina my love. She's the only one in that palace who writes to me.\"", "Hangup");
        d.Say("Bye3", Beatrice,
            "\"Finish it, Romus. Wars are like dinner parties. The whole trick is knowing when to leave.\"", "Hangup");
        d.Branch("Hangup", "Hangup1", "Hangup2", "Hangup3");
        d.SayIf("Hangup1", first, Narrator, "The line went dead. Beatrice never said goodbye on the telephone. She considered it common.", "End");
        d.SayIf("Hangup2", $"{calls} == 2", Narrator,
            "She rang off before I could answer. Somewhere in Thornbourgh, I suspected, a minister was already being told what the call " +
            "had cost him.", "End");
        d.Say("Hangup3", Narrator,
            "The line clicked and went quiet. I sat for a while with the receiver in my hand, listening to the hum where Thornbourgh had been.",
            "End");

        // ---- the call where she says no
        d.SayIf("NoMore", $"{pat} <= 0", Beatrice, "\"Romus. Before you ask, the answer is no.\"", "NoAsk");
        d.Opt("NoAsk", null, "\"You haven't heard the question.\"", null, null, "NoAsk2");
        d.Say("NoAsk2", Beatrice, "\"I don't need to. It's the same question it's been for months. The answer is what's changed.\"", "NoRecall");
        // What the King promised her in Thornbourgh (EarlyT7Diplomacy) is remembered.
        d.Branch("NoRecall", "NoQuick", "NoRepay", "NoWhy");
        d.SayIf("NoQuick", promised("end"), Beatrice,
            "\"At the last call you promised me it would be over. I wrote the date in my diary.\"",
            "NoQuick2");
        d.Say("NoQuick2", Beatrice, "\"I've had to turn the page twice since.\"", "NoWhy");
        d.SayIf("NoRepay", gave("credit"), Beatrice,
            "\"You had your eighteen billion. My Exchequer would like to see some of it back.\"", "NoRepay2");
        d.Say("NoRepay2", Beatrice, "\"Exchequers are very good at counting.\"", "NoWhy");
        d.Branch("NoWhy", "NoPales", "NoDossier", "NoTwoWars", "NoTired");
        d.SayIf("NoPales", gave("raids"), Beatrice,
            "\"I sent rifles for your raids on the ports, and the dossiers came back with them. There's nothing left over for more.\"",
            "NoFavour");
        d.SayIf("NoDossier", dossier, Beatrice,
            "\"Wraclawitz has photographs of Rumburgian rifles in Bludish hands, and Rayne has had them framed.\"", "NoDossier2");
        d.Say("NoDossier2", Beatrice, "\"I cannot be seen arming anyone's war this season. Not even yours.\"", "NoFavour");
        d.SayIf("NoTwoWars", twoWars, Beatrice,
            "\"You are fighting two wars, and Rumburg is paying for a share of both.\"", "NoTwoWars2");
        d.Say("NoTwoWars2", Beatrice, "\"GRACE has asked me to stop. For once, GRACE is right.\"", "NoFavour");
        d.Say("NoTired", Beatrice,
            "\"My ministers have added up what your war has cost us, and I can't find a reason to tell them yes.\"", "NoTired2");
        d.Say("NoTired2", Beatrice, "\"Rumburg has its own worries, Romus. Sordland is one of them.\"", "NoFavour");
        d.Say("NoFavour", Beatrice,
            "\"I'll keep saying the proper things at the Alliance. That's the last favour Rumburg can do you for now.\"",
            "No1", "No2", "No3");
        d.Opt("No1", null, "\"Then I'll finish it without Rumburg.\"", null, T(VarRumburgSaidNo), "No1a");
        d.Say("No1a", Beatrice, "\"I expect you'll try. You always were a stubborn boy.\"", "Hangup");
        d.Opt("No2", null, "\"Lena would never leave me to fight alone.\"", null, T(VarRumburgSaidNo) + T(VarRumburgInsulted) + Inc(NpRumburg, -1), "No2a");
        d.Say("No2a", Beatrice, "\"Don't you dare bring my sister into this over a telephone line.\"", "No2b");
        d.Say("No2b", Narrator, "There was a long pause.", "No2c");
        d.Say("No2c", Beatrice, "\"Goodbye, Romus.\"", "No2d");
        d.Say("No2d", Narrator, "The line went dead. In all the years I had known her, it was the first time she had said goodbye.", "End");
        d.Opt("No3", null, "\"What would change your mind?\"", null, T(VarRumburgSaidNo) + T(VarRumburgWantsCeasefire), "No3a");
        d.Say("No3a", Beatrice, "\"An end date.\"", "No3b");
        d.Say("No3b", Beatrice,
            "\"Take what you've won, and sit down with the Wehzeks before the winter does it for you.\"", "Hangup");

        d.Finish();
    }
}
