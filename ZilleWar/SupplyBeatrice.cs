namespace ZilleWar;

/// <summary>
/// Queen Beatrice on the blue telephone, once per war turn until she says no. A greeting, then the King's requests, each
/// with a short exchange and a clearly priced answer:
/// - The oil embargo, priced in Rizian gas exactly like her Turn 3 offer; never offered if Turn 3 already cut Wehlen off.
/// - One Rumburgian war credit: +6 Budget now, 8 Budget repaid over the turns left (-1 for 8 turns when there is time,
///   -2 for 4, -3 for 3 or -4 for 2 later in the reign; none in the last turns). Shown through WorldModifiers' repayment line.
/// - Bludish Freedom Front raids and fighters (vanilla Diplomacy_Wehlen_FundBFF, BFF mercenaries).
/// Every favour costs patience (VarRumburgPatience); Wehlen's "Thornbourgh dossier" and a second war cost more.
/// At zero she refuses and asks for an end date.
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

        d.Start(BeatriceConv, ResetAsked + Inc(VarBeatriceCalls, 1) + Music, "Phone");
        d.Scene("Phone",
            "An assistant brought in the blue telephone. The line to Thornbourgh crackled twice, and then the Queen of Rumburg was on it.",
            null, $"AddConversant(\"{Beatrice}\");", "Open");

        // ---- openings
        d.Branch("Open", "NoMore", "Hello1", "HelloTwoWars", "HelloDossier", "HelloLater");
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

        d.SayIf("HelloTwoWars", twoWars, Beatrice,
            "\"Two wars, Romus. My ministers have started calling you the Lion of the Antacean, and they don't mean it kindly.\"",
            "GreetL1", "GreetL2");
        d.SayIf("HelloDossier", dossier, Beatrice,
            "\"I assume you've read Wraclawitz's little pamphlet. Every newspaper in GRACE has. Twice.\"", "GreetL1", "GreetL2");
        d.Say("HelloLater", Beatrice, "\"Romus. Still at war, I see.\"", "GreetL1", "GreetL2");
        d.Opt("GreetL1", null, "\"Still at war, I'm afraid.\"", null, null, "GreetL1a");
        d.Say("GreetL1a", Beatrice, "\"I read the newspapers, Romus. Even the Wehzek ones, when I want a laugh.\"", "AskL");
        d.Opt("GreetL2", null, "\"How are your ministers?\"", null, null, "GreetL2a");
        d.Say("GreetL2a", Beatrice, "\"Counting. They're always counting. Lately, they count Rizia.\"", "AskL");
        d.Say("AskL", Beatrice, "\"Well? What is it this time?\"", "Hub");

        // ---- the hub: every request once per call, and only while she still has patience for favours
        d.Branch("Hub", Gate("AskEmbargo"), Gate("AskCredit"), Gate("AskRaids"), Gate("AskFighters"), "Bye");
        // After each favour: if that was the last one she'll grant, she says so once.
        d.Branch("Back", "Spent", "Hub");
        d.SayIfDo("Spent", $"{pat} <= 0 and {Not(Q(9))}", Beatrice,
            "\"And that, Romus, is the last favour you'll have from Rumburg for a while. Do spend it wisely.\"", T(Q(9)), "Hub");

        // 1. the oil, if the Turn 3 deal did not already cut Wehlen off
        d.Opt("AskEmbargo", $"not {high} and not {failed} and {pat} > 0 and {Not(Q(1))}",
            "\"Stop buying Wehzek oil. Every barrel pays for another convoy to Zille.\"", null, T(Q(1)), "EmbAvg", "EmbNone");
        d.SayIf("EmbAvg", avg, Beatrice,
            "\"We've already halved it. If I cut Smolak off entirely, I'll need more of your gas to keep the Steelfield lit.\"",
            "EmbMenu");
        d.Say("EmbNone", Beatrice,
            "\"You turned that very deal down in your own stateroom, if memory serves. And it does.\"", "EmbNone2");
        d.Say("EmbNone2", Beatrice,
            "\"The terms haven't changed. Your gas keeps my furnaces lit, and Smolak gets nothing from me.\"", "EmbMenu");
        d.Do("EmbMenu", null, Gate("EmbWhy"), Gate("EmbYes"), Gate("EmbAvgYes"), "EmbNo");
        d.Opt("EmbWhy", Not(Q(5)), "\"Couldn't Rumburg do it for Rizia's sake?\"", null, T(Q(5)), "EmbWhy2");
        d.Say("EmbWhy2", Beatrice,
            "\"Rumburg does a great many things for Rizia's sake, Romus. It does them for Rumburg's sake first.\"", "EmbMenu");
        d.Opt("EmbYes", $"not {avg}", "\"Done. Rizia's gas for your oil.\"", "-3 Energy, -2 Energy Per Turn, +2 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -3) + T("RiziaDLC.Policy_Diplomacy_TradeDeal_Rumburg") +
            Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -2) + Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 2) + T(HighExport) +
            Inc(NpWehlen, -2) + Inc(NpRumburg, 1) + Inc(VarRumburgPatience, -1), "Emb2");
        d.Opt("EmbAvgYes", avg, "\"Cut him off completely. Rizia will make up the difference.\"", "-1 Energy, -1 Energy Per Turn, +1 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -1) + Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -1) +
            Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 1) + F(AverageExport) + T(HighExport) + Inc(NpWehlen, -1) +
            Inc(VarRumburgPatience, -1), "Emb2");
        d.Say("Emb2", Beatrice, "\"Settled. Smolak's treasury will be lighter by spring.\"", "Emb3");
        d.Say("Emb3", Beatrice, "\"And my ministers louder. Don't make me regret it.\"", "Back");
        d.Opt("EmbNo", null, "\"Not at that price.\"", null, null, "EmbNo2");
        d.Say("EmbNo2", Beatrice, "\"Sensible. Dull, but sensible.\"", "Hub");

        // 2. one war credit: +6 Budget now, 8 repaid over the turns that are left
        d.Opt("AskCredit", $"{credits} == 0 and {turn} <= 8 and {pat} > 0 and {Not(Q(2))}",
            "\"The treasury is bleeding, Beatrice. Could Rumburg lend us money?\"", null, T(Q(2)), "Credit");
        d.Say("Credit", Beatrice, "\"Lend. What a charming word.\"", "Credit2");
        d.Say("Credit2", Beatrice,
            "\"Very well. A war credit in Rizia's name. Eighteen billion guilders now, from my Exchequer to yours. Pocket money, between family.\"", "CreditTerms");
        d.Branch("CreditTerms", "TermsLong", "TermsMid", "TermsShort");
        d.SayIf("TermsLong", $"{turn} <= 2", Beatrice,
            "\"Repaid in small installments over a good many seasons. My Exchequer is in a patient mood.\"", "CreditMenu");
        d.SayIf("TermsMid", $"{turn} <= 6", Beatrice,
            "\"Repaid over the next few seasons. My Exchequer insists on being paid before your reign gets interesting in other ways.\"",
            "CreditMenu");
        d.Say("TermsShort", Beatrice,
            "\"Repaid quickly, I'm afraid. Your reign is getting on, and my Exchequer has a nasty habit of reading calendars.\"",
            "CreditMenu");
        d.Do("CreditMenu", null, Gate("CreditWhy"), Gate("Credit8"), Gate("Credit4"), Gate("Credit3"), Gate("Credit2x"), "CreditNo");
        d.Opt("CreditWhy", Not(Q(6)), "\"And the interest?\"", null, T(Q(6)), "CreditWhy2");
        d.Say("CreditWhy2", Beatrice,
            "\"Family rates, Romus. Twenty-four billion back for every eighteen lent. Which is to say, higher than you'd like and lower than you deserve.\"", "CreditMenu");
        string creditScript = Inc(Budget, 6) + Inc(VarRumburgCredits, 1) + $"{V(VarRumburgDebt)} = 8; " + Inc(VarRumburgPatience, -1);
        string Repay(int rate, int turns) =>
            creditScript + $"{V(ModRumburgCredit)} = {-rate}; {V(ModRumburgCredit + "_Turns")} = {turns}; ";
        const string drawUp = "\"Draw it up.\"";
        d.Opt("Credit8", $"{turn} <= 2", drawUp, "+6 Budget, -1 Budget Per Turn for 8 Turns", Repay(1, 8), "CreditDone");
        d.Opt("Credit4", $"{turn} >= 3 and {turn} <= 6", drawUp, "+6 Budget, -2 Budget Per Turn for 4 Turns", Repay(2, 4), "CreditDone");
        d.Opt("Credit3", $"{turn} == 7", drawUp, "+6 Budget, -3 Budget Per Turn for 3 Turns", Repay(3, 3), "CreditDone");
        d.Opt("Credit2x", $"{turn} == 8", drawUp, "+6 Budget, -4 Budget Per Turn for 2 Turns", Repay(4, 2), "CreditDone");
        d.Say("CreditDone", Beatrice, "\"The papers will be in Porte Drazon by Friday.\"", "CreditDone2");
        d.Say("CreditDone2", Beatrice, "\"Do read the small print. Rumburg always collects.\"", "Back");
        d.Opt("CreditNo", null, "\"Rizia doesn't borrow to fight.\"", null, null, "CreditNo2");
        d.Say("CreditNo2", Beatrice, "\"Then Rizia is richer than its treasury looks. Good for Rizia.\"", "Hub");

        // 3. the Bludish Freedom Front on the Wehzek ports
        d.Opt("AskRaids", $"{raids} < 2 and not {dossier} and {pat} > 0 and {Not(Q(3))}",
            "\"Your friends in the Bludish Freedom Front. Can they hit Wehlen's ports?\"", null, T(Q(3)), "Raid1", "RaidAgain");
        d.SayIf("Raid1", $"{raids} == 0", Beatrice,
            "\"Friends is a strong word. They're violent, unreliable and outlawed in half of Merkopa.\"", "Raid1b");
        d.Say("Raid1b", Beatrice, "\"They're also very good at setting fire to things.\"", "RaidMenu");
        d.Say("RaidAgain", Beatrice,
            "\"Again? Wehzek radio already blames you for the last time. Same arrangement?\"", "RaidMenu");
        d.Do("RaidMenu", null, Gate("RaidWhy"), "RaidYes", "RaidNo");
        d.Opt("RaidWhy", Not(Q(7)), "\"And if it comes out that Rizia asked?\"", null, T(Q(7)), "RaidWhy2");
        d.Say("RaidWhy2", Beatrice, "\"Then Sordland will scream the house down.\"", "RaidWhy3");
        d.Say("RaidWhy3", Beatrice, "\"And I shall be shocked. Simply shocked, along with everyone else.\"", "RaidMenu");
        d.Opt("RaidYes", null, "\"Have them strike the ports.\"", "-1 Budget",
            Inc(Count(Lever.BffRaids), 1) + Talk.Weaken(1) + Inc(Budget, -1) + T("RiziaDLC.Diplomacy_Wehlen_FundBFF") +
            Inc(NpSordland, -1) + Inc(NpValgsland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; ", "Raid3");
        d.Say("Raid3", Beatrice, "\"Consider it done. Should anyone ask, this call was about my niece's birthday.\"", "Back");
        d.Opt("RaidNo", null, "\"No. Not like that.\"", null, null, "RaidNo2");
        d.Say("RaidNo2", Beatrice, "\"Scruples. How very Rizian of you.\"", "Hub");

        // 4. Bludish fighters for the Royal Army
        d.Opt("AskFighters", $"{fighters} < 1 and {pat} > 0 and {Not(Q(4))}",
            "\"I could use more men. Would the Bluds fight for Rizia?\"", null, T(Q(4)), "Fig1");
        d.Say("Fig1", Beatrice, "\"For money, they'd fight for anyone who isn't Smolak.\"", "Fig1b");
        d.Say("Fig1b", Beatrice, "\"Two companies. Hard men, paid in gold. Your officers will loathe them.\"", "FigMenu");
        d.Do("FigMenu", null, Gate("FigWhy"), "FigYes", "FigNo");
        d.Opt("FigWhy", Not(Q(8)), "\"Will they take orders?\"", null, T(Q(8)), "FigWhy2");
        d.Say("FigWhy2", Beatrice, "\"From whoever pays them, on the days they feel like it.\"", "FigWhy3");
        d.Say("FigWhy3", Beatrice, "\"Keep a Rizian officer near them at all times. Preferably a large one.\"", "FigMenu");
        d.Opt("FigYes", null, "\"Send them.\"", "-2 Budget",
            Inc(Count(Lever.BffFighters), 1) + Inc(Mercs, 2) + Inc(Budget, -2) + T("RiziaDLC.Resources_Military_RumburgianBFFMercenaries") +
            T("RiziaDLC.Diplomacy_Wehlen_FundBFF") + Inc(NpSordland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; ", "Fig2");
        d.Say("Fig2", Beatrice, "\"They'll be in Montaklar within the week. Keep them on a short leash. They bite.\"", "Back");
        d.Opt("FigNo", null, "\"Rizia will fight its own war.\"", null, null, "FigNo2");
        d.Say("FigNo2", Beatrice, "\"Admirable. Let's hope it's also enough.\"", "Hub");

        // goodbye
        d.Opt("Bye", null, "\"That's all for now, Beatrice.\"", null, null, "ByeBranch");
        d.Branch("ByeBranch", "ByeLast", "Bye1");
        d.SayIf("ByeLast", $"{pat} <= 0", Beatrice, "\"Don't ring me again unless it's to tell me it's over.\"", "Hangup");
        d.Say("Bye1", Beatrice, "\"Go and win your war.\"", "Bye2");
        d.Say("Bye2", Beatrice, "\"And try not to get shot. Rumburg has far too much invested in you.\"", "Hangup");
        d.Say("Hangup", Narrator, "The line went dead. Beatrice never said goodbye on the telephone. She considered it common.", "End");

        // ---- the call where she says no
        d.SayIf("NoMore", $"{pat} <= 0", Beatrice, "\"Romus. Before you ask, the answer is no.\"", "NoAsk");
        d.Opt("NoAsk", null, "\"You haven't heard the question.\"", null, null, "NoAsk2");
        d.Say("NoAsk2", Beatrice, "\"I don't need to. It's the same question it's been for weeks. The answer is what's changed.\"", "NoRecall");
        // What the King promised her in Thornbourgh (EarlyT7Diplomacy) is remembered.
        d.Branch("NoRecall", "NoQuick", "NoRepay", "NoWhy");
        d.SayIf("NoQuick", Eq(VarRumburgPromise, "quick"), Beatrice,
            "\"You flew all the way to Thornbourgh to promise me the next offensive would end it. I wrote the date in my diary.\"",
            "NoQuick2");
        d.Say("NoQuick2", Beatrice, "\"I've had to turn the page twice since.\"", "NoWhy");
        d.SayIf("NoRepay", Eq(VarRumburgPromise, "repay"), Beatrice,
            "\"You gave me your word you'd repay every crate. My quartermasters have started counting them.\"", "NoRepay2");
        d.Say("NoRepay2", Beatrice, "\"They are very good at counting.\"", "NoWhy");
        d.Branch("NoWhy", "NoPales", "NoDossier", "NoTwoWars", "NoTired");
        d.SayIf("NoPales", Eq(VarRumburgPriority, "pales"), Beatrice,
            "\"You told me to send my rifles south, to the Pales front. I took you at your word. There's nothing left over for Zille.\"",
            "NoFavour");
        d.SayIf("NoDossier", dossier, Beatrice,
            "\"Wraclawitz has photographs of Rumburgian rifles in Bludish hands, and Rayne has had them framed.\"", "NoDossier2");
        d.Say("NoDossier2", Beatrice, "\"I cannot be seen arming anyone's war this season. Not even yours.\"", "NoFavour");
        d.SayIf("NoTwoWars", twoWars, Beatrice,
            "\"You are fighting two wars, and Rumburg is paying for a share of both.\"", "NoTwoWars2");
        d.Say("NoTwoWars2", Beatrice, "\"GRACE has asked me to stop. For once, GRACE is right.\"", "NoFavour");
        d.Say("NoTired", Beatrice,
            "\"My ministers have added up what your war has cost us, and I can't find a reason to tell them no.\"", "NoTired2");
        d.Say("NoTired2", Beatrice, "\"Rumburg has its own worries, Romus. Sordland is one of them.\"", "NoFavour");
        d.Say("NoFavour", Beatrice,
            "\"I'll keep saying the proper things at the Alliance. That's the last favour Rumburg can do you for now.\"",
            "No1", "No2", "No3");
        d.Opt("No1", null, "\"Then I'll finish it without Rumburg.\"", null, T(VarRumburgSaidNo), "No1a");
        d.Say("No1a", Beatrice, "\"I expect you'll try. You always were a stubborn boy.\"", "Hangup");
        d.Opt("No2", null, "\"Lena would never leave me to fight alone.\"", null, T(VarRumburgSaidNo) + Inc(NpRumburg, -1), "No2a");
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
