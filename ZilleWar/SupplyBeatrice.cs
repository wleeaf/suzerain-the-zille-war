namespace ZilleWar;

/// <summary>
/// Queen Beatrice on the blue telephone, once per war turn until she says no.
/// - The oil embargo, priced in Rizian gas exactly like her Turn 3 offer (vanilla Rumburg export situations and
///   per-turn modifiers); never offered again if the Turn 3 deal already cut Wehlen off.
/// - Rumburgian war credits: Budget now, repaid every turn through "Rizia_Budget_Modifier_ZilleWarRumburgCredit".
/// - Bludish Freedom Front raids and fighters (vanilla Diplomacy_Wehlen_FundBFF, BFF mercenaries).
/// Every favour costs patience (VarRumburgPatience); Wehlen's "Thornbourgh dossier" and a second war cost more.
/// At zero she refuses, names her reasons, and asks for an end date.
/// </summary>
internal static partial class SupplyWar
{
    private static void RegisterBeatrice()
    {
        var d = new Dlg(BeatriceConv, "ZilleWar.Beatrice.");
        string calls = N(VarBeatriceCalls);
        string pat = N(VarRumburgPatience);
        string first = $"{calls} == 1";
        string later = $"{calls} > 1";
        string high = Is(HighExport);
        string avg = Is(AverageExport);
        string failed = Is(ExportFailed);
        string dossier = Is(VarDossier);
        string twoWars = Is(PalesWar);
        string credits = N(VarRumburgCredits);
        string raids = N(Count(Lever.BffRaids));
        string fighters = N(Count(Lever.BffFighters));

        d.Start(BeatriceConv, ResetAsked + Inc(VarBeatriceCalls, 1) + Music, "Phone");
        d.Scene("Phone",
            "An assistant brought in the blue telephone that was used for calls with heads of state. The line to Thornbourgh " +
            "crackled, clicked twice, and then the Queen of Rumburg was on it.", null, $"AddConversant(\"{Beatrice}\");",
            "Open");

        // ---- openings: the first one whose condition holds
        d.Branch("Open", "NoMore", "HelloCareful", "HelloEmbargo", "Hello1", "HelloTwoWars", "HelloDossier", "HelloLater");
        d.SayIf("HelloCareful", $"{first} and {Eq(AnDeclaration.VarRumburgCable, "careful")}", Beatrice,
            "\"Romus. You told Mr. Esquibel you would ring me from Porte Drazon, and I have been sitting by this telephone like a " +
            "debutante waiting for a dance card. Well. You've rung. Let's talk about your war.\"", "First");
        d.SayIf("HelloEmbargo", $"{first} and {high}", Beatrice,
            "\"Romus. My ministers stopped buying Wehzek oil months ago because you asked nicely, and now Mr. Smolak is paying for " +
            "his war out of his own pocket. You're welcome, by the way. What else do you need?\"", "First");
        d.SayIf("Hello1", first, Beatrice,
            "\"Romus. You've gone and done it, then. Half of Thornbourgh is delighted, and the other half is checking the " +
            "price of oil. Tell me what you need, and I'll tell you what it costs.\"", "First");
        d.SayIf("HelloTwoWars", $"{later} and {twoWars}", Beatrice,
            "\"Two wars, Romus. Two. My ministers have started calling you the Lion of the Antacean, and they don't mean it kindly. " +
            "What is it this time?\"", "Hub");
        d.SayIf("HelloDossier", $"{later} and {dossier}", Beatrice,
            "\"I assume you've read Wraclawitz's little pamphlet. Photographs of crates, invoices with my Exchequer's stamp on them. " +
            "Very thorough. Every newspaper in GRACE has read it twice. So. What do you want?\"", "Hub");
        d.Say("HelloLater", Beatrice,
            "\"Romus. Still at war, I see. Thornbourgh talks of nothing else, which is a mercy, because it means they've stopped " +
            "talking about my hats. What can I do for you this time?\"", "Hub");

        d.Opt("First1", null, "\"I'll need more than good wishes, Beatrice.\"", null, null, "First1a");
        d.Say("First1a", Beatrice, "\"Of course you will. Nobody rings Thornbourgh for good wishes. Go on.\"", "Hub");
        d.Opt("First2", null, "\"You told me once that Rizia would need Rumburg if it came to this.\"", null, null, "First2a");
        d.Say("First2a", Beatrice,
            "\"I did. I also told you it wouldn't come cheap. I may be family, Romus, but my Exchequer isn't.\"", "Hub");
        d.Branch("First", "First1", "First2");

        // ---- the hub: every question once per call, and only while she still has patience for favours
        d.Branch("Hub", Gate("AskEmbargo"), Gate("AskCredit"), Gate("AskRaids"), Gate("AskFighters"), Gate("AskMood"), "Bye");

        // 1. the oil, if the Turn 3 deal did not already cut Wehlen off
        d.Opt("AskEmbargo", $"not {high} and not {failed} and {pat} > 0 and {Not(Q(1))}",
            "\"Stop buying Wehzek oil. Every barrel you buy pays for another convoy to Zille.\"", null, T(Q(1)), "EmbAvg", "EmbNone");
        d.SayIf("EmbAvg", avg, Beatrice,
            "\"We've already halved it, as you'll recall. The other half keeps the lights on in the Steelfield. If I cut Smolak off " +
            "entirely, I'll need more of your gas to replace it, and rather sooner than we agreed.\"", "EmbAvgYes", "EmbNo");
        d.Say("EmbNone", Beatrice,
            "\"You turned that very deal down in your own stateroom, if you remember. I don't hold grudges, Romus. I simply keep " +
            "excellent notes. The terms haven't changed: your gas for my oil, and Smolak gets nothing.\"", "EmbYes", "EmbNo");
        d.Opt("EmbYes", null, "\"Done. Rumburg buys Rizian gas, and Smolak gets nothing.\"", "-3 Energy, -2 Energy Per Turn, +2 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -3) + T("RiziaDLC.Policy_Diplomacy_TradeDeal_Rumburg") +
            Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -2) + Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 2) + T(HighExport) +
            Inc(NpWehlen, -2) + Inc(NpRumburg, 1) + Inc(VarRumburgPatience, -1), "Emb2");
        d.Opt("EmbAvgYes", null, "\"Cut him off completely. Rizia will make up the difference.\"", "-1 Energy, -1 Energy Per Turn, +1 Budget Per Turn",
            Inc(Count(Lever.Embargo), 1) + Talk.Weaken(1) + Inc(Energy, -1) + Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -1) +
            Inc("RiziaDLC.Budget_Modifier_RumburgTrade", 1) + F(AverageExport) + T(HighExport) + Inc(NpWehlen, -1) +
            Inc(VarRumburgPatience, -1), "Emb2");
        d.Say("Emb2", Beatrice,
            "\"Then it's settled. Smolak will find his treasury considerably lighter by spring, and I shall find my ministers " +
            "considerably louder. Don't make me regret it.\"", "Hub");
        d.Opt("EmbNo", null, "\"Not at that price. Not yet.\"", null, null, "EmbNo2");
        d.Say("EmbNo2", Beatrice, "\"Sensible. Dull, but sensible. The offer won't vanish.\"", "Hub");

        // 2. a war credit, repaid every turn
        d.Opt("AskCredit", $"{credits} < 2 and {pat} > 0 and {Not(Q(2))}",
            "\"The treasury is bleeding, Beatrice. Could Rumburg lend us money?\"", null, T(Q(2)), "Credit1", "Credit2Gas", "Credit2");
        d.SayIf("Credit1", $"{credits} == 0", Beatrice,
            "\"Lend. What a charming word. Very well: the Exchequer will open a war credit in Rizia's name. A handsome sum now, and " +
            "modest installments every month until it's repaid, with interest so reasonable my bankers have taken to their beds.\"",
            "CreditYes", "CreditNo");
        d.SayIf("Credit2Gas", $"{credits} == 1 and ({high} or {avg}) and not {failed}", Beatrice,
            "\"Again? The ink on the first one isn't dry. I can find you a second credit, but my ministers will want something they " +
            "can hold in their hands. More of your gas through Monqiz, for a start.\"", "CreditGasYes", "CreditNo");
        d.Say("Credit2", Beatrice,
            "\"Again? The ink on the first one isn't dry. I can find you a second credit, but not at the family rate. My bankers " +
            "have stopped taking to their beds.\"", "Credit2Yes", "CreditNo");
        d.Opt("CreditYes", null, "\"Draw it up.\"", "+4 Budget, -1 Budget Per Turn",
            Inc(Budget, 4) + Inc(VarRumburgCredits, 1) + Inc(VarRumburgDebt, 6) + $"{V(ModRumburgCredit)} = -1; " + SetTurn(VarDebtTurn) +
            Inc(VarRumburgPatience, -1), "CreditDone");
        d.Opt("CreditGasYes", null, "\"Draw it up. You'll have the gas.\"", "+3 Budget, -1 Budget Per Turn, -1 Energy Per Turn",
            Inc(Budget, 3) + Inc(VarRumburgCredits, 1) + Inc(VarRumburgDebt, 5) + $"{V(ModRumburgCredit)} = -2; " + SetTurn(VarDebtTurn) +
            Inc("RiziaDLC.Energy_Modifier_RumburgEnergyExport", -1) + Inc(VarRumburgPatience, -1), "CreditDone");
        d.Opt("Credit2Yes", null, "\"Draw it up, then. Whatever the rate.\"", "+3 Budget, -1 Budget Per Turn",
            Inc(Budget, 3) + Inc(VarRumburgCredits, 1) + Inc(VarRumburgDebt, 7) + $"{V(ModRumburgCredit)} = -2; " + SetTurn(VarDebtTurn) +
            Inc(VarRumburgPatience, -1), "CreditDone");
        d.Say("CreditDone", Beatrice,
            "\"The papers will be in Porte Drazon by Friday. Do read the small print. Rumburg always collects.\"", "Hub");
        d.Opt("CreditNo", null, "\"Rizia doesn't borrow to fight.\"", null, null, "CreditNo2");
        d.Say("CreditNo2", Beatrice, "\"Then Rizia must be considerably richer than its treasury looks. Good for Rizia.\"", "Hub");

        // 3. the Bludish Freedom Front on the Wehzek ports
        d.Opt("AskRaids", $"{raids} < 2 and not {dossier} and {pat} > 0 and {Not(Q(3))}",
            "\"Your friends in the Bludish Freedom Front. Can they hit Wehlen's ports?\"", null, T(Q(3)), "Raid1", "RaidAgain");
        d.SayIf("Raid1", $"{raids} == 0", Beatrice,
            "\"Friends is a strong word. They're violent, unreliable and outlawed in half of Merkopa. They are also remarkably good " +
            "at setting fire to things. We cannot control them, Romus, we can only provide them with the tools.\"", "Raid2");
        d.Say("Raid2", Beatrice,
            "\"I can have a quiet word. They'll want paying, of course. And understand this: if it ever comes out that Rizia asked, " +
            "Sordland will scream the house down, and Valgsland will hand them the megaphone.\"", "RaidYes", "RaidNo");
        d.Say("RaidAgain", Beatrice,
            "\"Again? They do enjoy the work, I'll give them that. Wehzek radio already blames you for the last time, so I doubt " +
            "your reputation can get much worse. Same arrangement?\"", "RaidYes", "RaidNo");
        d.Opt("RaidYes", null, "\"Have them strike the ports.\"", "-1 Budget",
            Inc(Count(Lever.BffRaids), 1) + Talk.Weaken(1) + Inc(Budget, -1) + T("RiziaDLC.Diplomacy_Wehlen_FundBFF") +
            Inc(NpSordland, -1) + Inc(NpValgsland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; ", "Raid3");
        d.Say("Raid3", Beatrice,
            "\"Consider it done. And Romus, should anyone ask, this call was about my niece's birthday.\"", "Hub");
        d.Opt("RaidNo", null, "\"No. Not like that.\"", null, null, "RaidNo2");
        d.Say("RaidNo2", Beatrice, "\"Scruples. How very Rizian of you. As you wish.\"", "Hub");

        // 4. Bludish fighters for the Royal Army
        d.Opt("AskFighters", $"{fighters} < 1 and {pat} > 0 and {Not(Q(4))}",
            "\"I could use more men. Would the Bluds fight for Rizia?\"", null, T(Q(4)), "Fig1");
        d.Say("Fig1", Beatrice,
            "\"For money, they'd fight for anyone who isn't Smolak. Two companies, perhaps. Hard men who know how Wehzeks fight. " +
            "They'll want paying in gold, not gratitude, and your officers will loathe them.\"", "FigYes", "FigNo");
        d.Opt("FigYes", null, "\"Send them. The treasury will pay.\"", "-2 Budget",
            Inc(Count(Lever.BffFighters), 1) + Inc(Mercs, 2) + Inc(Budget, -2) + T("RiziaDLC.Resources_Military_RumburgianBFFMercenaries") +
            T("RiziaDLC.Diplomacy_Wehlen_FundBFF") + Inc(NpSordland, -1) + Inc(VarRumburgPatience, -1) +
            $"if {N(VarBffFirstTurn)} == 0 then {SetTurn(VarBffFirstTurn)} end; ", "Fig2");
        d.Say("Fig2", Beatrice, "\"They'll be in Montaklar within the week. Do keep them on a short leash. They bite.\"", "Hub");
        d.Opt("FigNo", null, "\"Rizia will fight its own war.\"", null, null, "FigNo2");
        d.Say("FigNo2", Beatrice, "\"Admirable. Let's hope it's also enough.\"", "Hub");

        // 5. the mood in Thornbourgh: no cost, asked once per call
        d.Opt("AskMood", Not(Q(5)), "\"How is Thornbourgh taking the war?\"", null, T(Q(5)), "Mood");
        d.Branch("Mood", "MoodDossier", "MoodTwoWars", "MoodTired", "MoodFine");
        d.SayIf("MoodDossier", dossier, Beatrice,
            "\"Badly, since the pamphlet. The Sordish ambassador has asked my Foreign Secretary, in writing, whether Rumburg means to " +
            "arm every bandit in Merkopa or only the Bludish ones. Rayne smells weakness, Romus. So does GRACE.\"", "Hub");
        d.SayIf("MoodTwoWars", twoWars, Beatrice,
            "\"The Exchequer says Rumburg is paying for one war and a half. The half is yours in Pales. Nahrburk has asked me, very " +
            "politely, whether Rizia means to stop at some point.\"", "Hub");
        d.SayIf("MoodTired", $"{pat} <= 1", Beatrice,
            "\"Wearily. GRACE exists so that its kings stop marching on their neighbors, and here you are. My ministers have started " +
            "asking me when it ends. I'd like an answer I can repeat in public.\"", "Hub");
        d.Say("MoodFine", Beatrice,
            "\"With a great deal of enthusiasm and very little information, the way Rumburgians take everything. The papers have you " +
            "riding into Zille on a white horse. Do try not to disappoint them.\"", "Hub");

        // goodbye
        d.Opt("Bye", null, "\"That's all for now, Beatrice.\"", null, null, "ByeBranch");
        d.Branch("ByeBranch", "ByeLast", "Bye1", "Bye2");
        d.SayIf("ByeLast", $"{pat} <= 0", Beatrice,
            "\"A word of advice, Romus, from one sovereign to another. Don't ring me again unless it's to tell me it's over.\"", "Hangup");
        d.SayIf("Bye1", $"{calls} <= 1", Beatrice,
            "\"Then go and win your war. And do try not to get yourself shot. Rumburg has far too much invested in you.\"", "Hangup");
        d.Say("Bye2", Beatrice,
            "\"Ring me when you've taken something worth bragging about. I've a dinner on Thursday and I'd love to ruin the " +
            "Valgslandian ambassador's evening.\"", "Hangup");
        d.Say("Hangup", Narrator, "The line went dead. Beatrice never said goodbye on the telephone. She considered it common.", "End");

        // ---- the call where she says no
        d.SayIf("NoMore", $"{pat} <= 0", Beatrice, "\"Romus. Before you ask, the answer is no.\"", "NoWhy");
        d.Branch("NoWhy", "NoPales", "NoDossier", "NoTwoWars", "NoTired");
        d.SayIf("NoPales", Eq(VarRumburgPriority, "pales"), Beatrice,
            "\"You told me in Thornbourgh to send my rifles south, to the Alca. I took you at your word. Every crate Rumburg can spare " +
            "is on its way to the Pales front, and there is nothing left over for Zille.\"", "NoFavour");
        d.SayIf("NoDossier", dossier, Beatrice,
            "\"Wraclawitz has photographs of Rumburgian rifles in Bludish hands, and Anton Rayne has had them enlarged and framed. " +
            "Sordland is moving police up to our border and calling it a customs exercise. I cannot be seen arming anyone's war this " +
            "season. Not even yours.\"", "NoFavour");
        d.SayIf("NoTwoWars", twoWars, Beatrice,
            "\"You are fighting two wars, and Rumburg is paying for a generous share of both. GRACE has asked me to stop, and for once " +
            "GRACE is right.\"", "NoFavour");
        d.Say("NoTired", Beatrice,
            "\"My ministers have added up what Rumburg has spent on your war, and they've asked me to stop, and I can't find a reason " +
            "to tell them no. Rumburg has its own worries, Romus. Sordland is one of them, and it isn't going anywhere.\"", "NoFavour");
        d.Branch("NoFavour", "NoFavourCredit", "NoFavourPlain");
        d.SayIf("NoFavourCredit", $"{credits} > 0", Beatrice,
            "\"The embargo stays, and I won't call in your credit early or tell the newspapers what it cost. That is the last favour " +
            "Rumburg can do you for now.\"", "No1", "No2", "No3");
        d.Say("NoFavourPlain", Beatrice,
            "\"I'll keep saying all the proper things at the Alliance. That is the last favour Rumburg can do you for now.\"", "No1", "No2", "No3");
        d.Opt("No1", null, "\"Then I'll finish it without Rumburg.\"", null, T(VarRumburgSaidNo), "No1a");
        d.Say("No1a", Beatrice, "\"I expect you'll try. You always were a stubborn boy.\"", "Hangup");
        d.Opt("No2", null, "\"Lena would never have left me to fight alone.\"", null, T(VarRumburgSaidNo) + Inc(NpRumburg, -1), "No2a");
        d.Say("No2a", Beatrice,
            "\"Don't. Don't you dare bring my sister into this over a telephone line.\" There was a long pause. \"Goodbye, Romus.\"", "Hangup");
        d.Opt("No3", null, "\"What would change your mind?\"", null, T(VarRumburgSaidNo) + T(VarRumburgWantsCeasefire), "No3a");
        d.Say("No3a", Beatrice,
            "\"An end date. Take what you've won and sit down with the Wehzeks before the winter does it for you. Do that, and you'll " +
            "find Thornbourgh very generous with the peace.\"", "Hangup");

        d.Finish();
    }
}
