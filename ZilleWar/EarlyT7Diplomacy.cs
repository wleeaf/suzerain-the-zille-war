using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.EarlyT7;

namespace ZilleWar;

/// <summary>Turn 7 abroad: Queen Beatrice in Thornbourgh, Duke Reinhart in Porte Drazon, Supreme Wiseman Azmal in Derdia.</summary>
internal static class EarlyT7Diplomacy
{
    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Laurento = "Laurento Esquibel";
    private const string Axel = "Axel Reinhart";
    private const string Azmal = "Jorga Azmal";
    private const string Sal = "Sal Ignacius";

    public static void Register()
    {
        RegisterRumburg();
        RegisterDuke();
        RegisterDerdia();
        RegisterDerdiaSubs();
    }

    public static void OnStep()
    {
    }

    // ------------------------------------------------------------------ c389 Rumburg

    private static void RegisterRumburg()
    {
        const string conv = "Rizia/Turn 7/FPnT_RumburgMeeting";
        var t = new Talk(conv, "ZilleWar.E7.Rumburg.");
        string bffUsed = $"({Num(SupplyWar.Count(SupplyWar.Lever.BffRaids))} > 0 or {Num(SupplyWar.Count(SupplyWar.Lever.BffFighters))} > 0)";
        string fundBff = "Variable[\"RiziaDLC.Diplomacy_Wehlen_FundBFF\"] == true";

        // Romus defending "my agreement with Wiktor Smolak": there is no agreement, there is a war.
        Vanilla.Require(conv, 685, Peace);
        Vanilla.Require(conv, 1048, Peace);
        Vanilla.Require(conv, 1099, Peace);   // "If Smolak found out, he'd hold on to Zille forever."

        // Rumburg is only starving Smolak of oil money if it stopped buying Wehzek oil (Turn 3 deal, or the embargo on the telephone).
        const string rumburgEmbargo =
            "(Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true " +
            "or (Variable[\"ZilleWar.Lever_Embargo\"] or 0) > 0)";
        t.Variant("Pry868", 868, new[] { 867 }, $"({Act}) and {rumburgEmbargo}", Beatrice,
            "\"And what of Rumburg's help against Smolak? Whose oil embargo do you imagine is starving his garrison in Zille?\"",
            new[] { "#897", "#882", "#885", "#886", "#888" });
        t.Variant("Pry868b", 868, new[] { 867 }, $"({Act}) and not {rumburgEmbargo}", Beatrice,
            "\"And what of Rumburg's help against Smolak? Who do you imagine has been standing beside you at the Alliance while Valgsland howls?\"",
            new[] { "#897", "#882", "#885", "#886", "#888" });
        t.Variant("Contacts933", 933, new[] { 886, 934 }, Act, Beatrice,
            "\"We have certain contacts within Wehlen who could make your war in Zille a great deal shorter.\"", new[] { "#935" });
        t.Variant("Laurento1082", 1082, new[] { 1084 }, Act, Laurento,
            "\"I have never liked violence as a solution to the Zille question, Your Excellency. But with our soldiers already fighting " +
            "there, I can hardly object to the Bluds lending a hand.\"", new[] { "#1098", "#1094", "#1100", "#1351" });

        // After the war. Liberated: Zille is ours, so Rumburg's pitch is that the Bluds keep Smolak too busy to take it back.
        // Divided or lost: "the reclamation of Zille" still reads true (the rest of it, or all of it).
        t.Variant("Pry868Lib", 868, new[] { 867 }, Lib, Beatrice,
            "\"And who do you imagine was standing beside you while you pried Zille from the claws of the Wehzeks?\"",
            new[] { "#897", "#882", "#885", "#886", "#888" });
        t.Variant("Pry868Part", 868, new[] { 867 }, Part, Beatrice,
            "\"And what of our ability to help you pry the rest of Zille from the claws of the Wehzeks?\"",
            new[] { "#897", "#882", "#885", "#886", "#888" });
        t.Variant("Pry868Lost", 868, new[] { 867 }, Lost, Beatrice,
            "\"And what of our ability to help you get Zille back? It can still be done, with the right help.\"",
            new[] { "#897", "#882", "#885", "#886", "#888" });
        t.Variant("Contacts933Lost", 933, new[] { 886, 934 }, Lost, Beatrice,
            "\"We have certain contacts within Wehlen who could still make Smolak's hold on Zille uncomfortable. It is not too late for that.\"",
            new[] { "#935" });
        t.Variant("Contacts933Lib", 933, new[] { 886, 934 }, Lib, Beatrice,
            "\"We have certain contacts within Wehlen who could keep Smolak far too busy at home to ever think of taking Zille back.\"",
            new[] { "#935" });
        t.Variant("Contacts933Part", 933, new[] { 886, 934 }, Part, Beatrice,
            "\"We have certain contacts within Wehlen who could stir up trouble behind Smolak's line in Zille. Possibly enough to make him give up the rest of it.\"",
            new[] { "#935" });
        t.Variant("Monqiz908Lib", 908, new[] { 907 }, Lib, Laurento,
            "\"As you should, Your Majesty. Giving away another part of Rizia just as Zille has come home would be most unwise.\"", new[] { "#917" });
        t.Variant("Monqiz908Part", 908, new[] { 907 }, Part, Laurento,
            "\"As you should, Your Majesty. Giving away another part of Rizia while part of Zille is still Wehzek would be most unwise.\"",
            new[] { "#917" });
        const string busy = "\"I have never liked paying for violence, Your Majesty. But a Smolak busy with the Bluds is a Smolak who leaves Zille alone.\"";
        t.Variant("Laurento1081Lib", 1081, new[] { 1079 }, Lib, Laurento, busy, new[] { "#1098", "#1099", "#1100", "#1351" });
        t.Variant("Laurento1082Lib", 1082, new[] { 1084 }, Lib, Laurento, busy, new[] { "#1098", "#1094", "#1100", "#1351" });
        t.Variant("Laurento1082Over", 1082, new[] { 1084 }, $"{Over} and {WarFlags.LNot(WarFlags.Liberated)}", Laurento,
            "\"I am still loath to suggest violence as a solution to the Zille conundrum. But the army has done what it could, and Smolak " +
            "will never give back a single village at a table. An internal resistance may be our last hope.\"",
            new[] { "#1098", "#1094", "#1100", "#1351" });
        t.Variant("Bff1076Lib", 1076, new[] { 1069 }, Lib, Romus, "\"Hmm. Is that how you help us keep Zille, then?\"", new[] { "#1077" });
        t.Variant("Bff1088Lib", 1088, new[] { 1075 }, Lib, Beatrice,
            "\"And while they keep Smolak busy at home, he will not be thinking about taking Zille back.\"", new[] { "#1090" });
        t.Variant("Bff1089Lib", 1089, new[] { 1087 }, Lib, Beatrice,
            "\"And a Smolak busy with his own rebels has no soldiers to spare for Zille.\"", new[] { "#1090" });
        t.Variant("Bff1092Lib", 1092, new[] { 1091 }, Lib, Beatrice,
            "\"A struggle that keeps Smolak far too busy to think about Zille.\"", new[] { "#1090" });
        t.Variant("Bff1150Lib", 1150, new[] { 1077 }, Lib, Beatrice,
            "\"Up to this point, we have been aiding the Bluds' liberation efforts in the north of Wehlen. A Smolak fighting for his own " +
            "north has no soldiers to spare for Zille.\"", new[] { "#1090" });
        t.Variant("Bff1151Lib", 1151, new[] { 1090 }, Lib, Laurento,
            "\"If I may, Your Excellency... Zille is ours now. Why should Rizia pay for a war in the north of Wehlen?\"", new[] { "#1152" });
        Vanilla.Require(conv, 1093, WarFlags.LNot(WarFlags.Liberated));   // "our existing resistance plans" in a Zille that is ours
        t.Variant("Bff1097Lib", 1097, new[] { 1353 }, Lib, Romus,
            "\"The Bluds would keep Smolak busy whether we pay them or not. We'd just be giving them a nudge.\"", new[] { "#1103" });
        t.Variant("Bff1131Lib", 1131, new[] { 1128 }, Lib, Romus,
            "\"I'll co-fund the Bludish Freedom Front along with Rumburg, as long as they keep Smolak away from Zille.\"", new[] { "#1133" },
            cost: "-1 Budget");
        t.Variant("Bff1142Lib", 1142, new[] { 1140 }, Lib, Romus,
            "\"You don't have to twist my arm, Your Excellency. I'll fund the Bludish Freedom Front, if they keep Smolak too busy to look south.\"",
            new[] { "#1133" }, cost: "-1 Budget");

        // --- "I have not yet broached the subject of Zille." (the car, no Pales war)
        int[] zilleParents = { 1473, 1477, 1486, 1492 };
        t.Variant("ZilleAct", 1300, zilleParents, Act, Beatrice, "\"Now. Zille.\"", new[] { "RAct" });
        // Beatrice's verdict on the war, either unprompted in the car (1300, "And Zille. ...") or when the King raises it after
        // making amends (1326 answers "Now let's talk about Zille", which leads into the vanilla BFF offer in peacetime).
        string partCf = $"{Part} and {KingCf}", partArm = $"{Part} and {NoKingCf}";
        string lostCf = $"{Lost} and {KingCf}", lostArm = $"{Lost} and {NoKingCf}";
        foreach ((string sfx, int entry, int[] parents, string lead) in new[]
                 {
                     ("", 1300, zilleParents, "And Zille. "),
                     ("Ask", 1326, new[] { 1325 }, "Zille. "),
                 })
        {
            t.Variant("ZilleLib" + sfx, entry, parents, Lib, Beatrice,
                $"\"{lead}I confess I didn't think Smolak would fold so quickly. Congratulations, Romus. Truly.\"", new[] { "RLib" });
            t.Variant("ZillePartArm" + sfx, entry, parents, partArm, Beatrice,
                $"\"{lead}Half a loaf, I'm told. You have the city and its harbor, and Smolak sits behind an armistice line with the rest.\"",
                new[] { "RPart1", "RPart2", "RDef3" });
            t.Variant("ZillePartCf" + sfx, entry, parents, partCf, Beatrice,
                $"\"{lead}A ceasefire, with the harbor in your pocket and the rest still in Smolak's. Very diplomatic of you.\"",
                new[] { "RCf1", "RCf2", "RPart2" });
            t.Variant("ZilleLostArm" + sfx, entry, parents, lostArm, Beatrice,
                $"\"{lead}I told my ministers Smolak would not give it up easily. I did not expect him to keep all of it.\"",
                new[] { "RDef1", "RDef2", "RDef3" });
            t.Variant("ZilleLostCf" + sfx, entry, parents, lostCf, Beatrice,
                $"\"{lead}A ceasefire, before your army even reached the city. Smolak keeps what he holds and calls it peace, and you call it prudence.\"",
                new[] { "RCf1", "RCf2", "RDef3" });
        }

        t.Variant("Help1324", 1324, new[] { 1319 }, Over, Romus, "\"All right. Now let's talk about Zille.\"", new[] { "#1325" });
        t.Logic("Help1326", new[] { "RAct" }, condition: Act, hooks: From(1326));

        // the war is on: the Bludish Freedom Front, then the bill
        t.Logic("RAct", new[] { "BffDone", "BffUsed", "BffOffer" }, condition: Act);
        t.Line("BffDone", Beatrice,
            "\"I see our Bludish friends are on your payroll as well as mine. Good. Then let us talk about the rest of the bill.\"",
            new[] { "Fat" }, condition: fundBff);
        t.Line("BffUsed", Beatrice,
            "\"The Bludish Freedom Front has been setting fire to Smolak's ports on your behalf for weeks now, and Rumburg has been paying " +
            "for the matches. I would like Rizia to start sharing the bill. Three billion guilders would do, for a start. A trifle, between family.\"", new[] { "BffLaurento" }, condition: bffUsed);
        t.Line("BffOffer", Beatrice,
            "\"I suggest you take advantage of Rumburg's contacts within the Bludish Freedom Front. Your soldiers could use friends behind " +
            "Smolak's lines.\"", new[] { "BffLaurento" });
        t.Line("BffLaurento", Laurento,
            "\"I have never liked violence as a solution to the Zille question, Your Excellency. But our soldiers are already fighting " +
            "there. If the Bluds can keep Smolak looking over his shoulder, I won't object.\"", new[] { "Bff1", "Bff2", "Bff3" });
        t.Choice("Bff1", "\"Rizia will share the cost. Tell your friends to strike south, toward Zille.\"", new[] { "Bff1r" },
            condition: $"{Num("RiziaDLC.Resources_Budget")} >= 1", cost: "-1 Budget",
            script: Inc("RiziaDLC.Resources_Budget", -1) + Set("RiziaDLC.Diplomacy_Wehlen_FundBFF", "true") +
                    Inc("RiziaDLC.Zille_ViolentResistancePower", 1) + Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 1) + Weaken(1));
        t.Line("Bff1r", Beatrice,
            "\"Splendid. I'll have my people get in touch with theirs. Smolak is about to find his garrison in Zille very lonely.\"", new[] { "Fat" });
        t.Choice("Bff2", "\"The Bluds have their own war. I won't make them fight mine.\"", new[] { "Bff2r" });
        t.Line("Bff2r", Beatrice, "\"Scruples, at this stage of the war? Very well. It's your war.\"", new[] { "Fat" });
        t.Choice("Bff3", "\"Why do I get the feeling Rumburg would like this war to last, Your Excellency?\"", new[] { "Bff3r" });
        t.Line("Bff3r", Beatrice,
            "\"Because you are a suspicious man, Romus. It's the Toras in you. I want it won. I simply see no reason why only Rizia should " +
            "profit from the winning.\"", new[] { "BffMenu" });
        t.Logic("BffMenu", new[] { "Bff1", "Bff2" });

        t.Logic("Fat", new[] { "FatEmb", "FatPlain" });
        t.Line("FatEmb", Beatrice,
            "\"I'll be frank with you. My oil embargo, my rifles, my Bluds. Rumburg has been fighting your war in Zille at a discount, and " +
            "my ministers have begun to notice. There is a limit to Rumburg's generosity.\"",
            new[] { "Fat1", "Fat2", "Fat3" }, condition: rumburgEmbargo, script: Set(VarRumburgWarned, "true"));
        t.Line("FatPlain", Beatrice,
            "\"I'll be frank with you. My voice at the Alliance, my rifles, my Bluds. Rumburg has been fighting your war in Zille at a " +
            "discount, and my ministers have begun to notice. There is a limit to Rumburg's generosity.\"",
            new[] { "Fat1", "Fat2", "Fat3" }, script: Set(VarRumburgWarned, "true"));
        t.Choice("Fat1", "\"The next offensive will end it. You'll have your gas and your gold before winter.\"", new[] { "Fat1r" },
            script: Str(VarRumburgPromise, "quick"));
        t.Line("Fat1r", Beatrice, "\"I shall hold you to that, dear. I hold everyone to everything.\"", new[] { "#1370" });
        t.Choice("Fat2", "\"Rizia will repay every crate. You have my word.\"", new[] { "Fat2r" }, script: Str(VarRumburgPromise, "repay"));
        t.Line("Fat2r", Beatrice, "\"Your word is a fine thing, Romus. Rumburg prefers gas.\"", new[] { "#1370" });
        t.Choice("Fat3", "\"Rumburg has its own reasons to see Smolak humbled. Don't pretend this is charity.\"", new[] { "Fat3r" },
            script: Str(VarRumburgPromise, "none") + Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("Fat3r", Beatrice,
            "\"Of course it isn't charity. But it is a favor, and favors are remembered. In both directions.\"", new[] { "#1370" });

        // liberated: the gold
        t.Line("RLib", Beatrice,
            "\"Now. You have Zille back, but its gold is as good as useless if you have nobody to sell it to. Rumburg would like first " +
            "refusal on whatever comes out of those mines.\"", new[] { "Gold1", "Gold2", "Gold3" }, condition: Lib);
        t.Choice("Gold1", "\"Rumburg will have first claim on Zille's gold.\"", new[] { "Gold1r" },
            script: Str(VarZilleGold, "rumburg") + Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 2));
        t.Line("Gold1r", Beatrice, "\"Now that is the brother-in-law I remember.\"", new[] { "#1370" });
        t.Choice("Gold2", "\"Zille's gold will go to whoever pays best.\"", new[] { "Gold2r" },
            script: Str(VarZilleGold, "market") + Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("Gold2r", Beatrice, "\"Spoken like an Arcasian. I'll pretend I didn't hear it.\"", new[] { "#1370" });
        t.Choice("Gold3", "\"Ask me again once the mines are working.\"", new[] { "Gold3r" }, script: Str(VarZilleGold, "later"));
        t.Line("Gold3r", Beatrice, "\"I shall. Frequently.\"", new[] { "#1370" });

        // divided
        t.Choice("RPart1", "\"The line will not hold forever. We'll finish what we started.\"", new[] { "RPart1r" });
        t.Line("RPart1r", Beatrice,
            "\"Then do finish it, dear. My ministers have little patience for wars that stop halfway, and even less for paying for them.\"",
            new[] { "#1370" });
        t.Choice("RPart2", "\"Zille's harbor is Rizian again. My father couldn't say as much.\"", new[] { "RPart2r" });
        t.Line("RPart2r", Beatrice,
            "\"No, he was the one who lent it out. Part of it back is something, I suppose. My ministers will want to know when they can " +
            "expect the rest.\"", new[] { "#1370" });

        // lost
        t.Choice("RDef1", "\"We lost a campaign, Beatrice. Not the claim.\"", new[] { "RDef1r" });
        t.Line("RDef1r", Beatrice,
            "\"Then I hope the next campaign is better planned. Rumburg can't keep backing a horse that falls at every fence.\"", new[] { "#1370" });
        t.Choice("RDef2", "\"Your help came late, and it came expensive.\"", new[] { "RDef2r" },
            script: Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("RDef2r", Beatrice,
            "\"And yet it came, which is more than anyone else can say. Do remember that when GRACE next meets.\"", new[] { "#1370" });
        t.Choice("RDef3", "\"I would rather not speak of it.\"", new[] { "RDef3r" });
        t.Line("RDef3r", Beatrice, "\"No. I don't suppose you would.\"", new[] { "#1370" });

        // ceasefire
        t.Choice("RCf1", "\"Time, Beatrice. I've bought time.\"", new[] { "RCf1r" });
        t.Line("RCf1r", Beatrice,
            "\"Time is the one thing Smolak has more of than you. He has no elections to fear, and no heirs to disappoint.\"", new[] { "#1370" });
        t.Choice("RCf2", "\"Fewer funerals. That's what I bought.\"", new[] { "RCf2r" });
        t.Line("RCf2r", Beatrice,
            "\"A noble sentiment. My ministers will find it very touching, and entirely useless.\"", new[] { "#1370" });

        // --- two wars: the Pales war path, right after Beatrice promises the KA-74s
        t.Line("Two1", Beatrice,
            "\"Do understand one thing, Romus. Every crate that goes south to the Alca is a crate that does not go north to Zille. Rumburg " +
            "has been arming your war against Smolak for months. Now you want a second one paid for as well.\"",
            new[] { "Two2" }, condition: $"{Two} and ({V(VarRumburgPriority)} == nil or {V(VarRumburgPriority)} == \"\")", hooks: From(315));
        t.Line("Two2", Beatrice, "\"So you must decide. Zille or Pales?\"", new[] { "TwoZ", "TwoP", "TwoB" });
        t.Choice("TwoZ", "\"Zille. It was Rizia's long before anyone thought of the Aureus field.\"", new[] { "TwoZr" },
            script: Str(VarRumburgPriority, "zille"));
        t.Line("TwoZr", Beatrice, "\"Then the Alca gets what's left. I do hope the Duke is as weak as your generals say.\"", new[] { "TwoEnd" });
        t.Choice("TwoP", "\"Pales. Zille can hold without your rifles for a few months.\"", new[] { "TwoPr" },
            script: Str(VarRumburgPriority, "pales"));
        t.Line("TwoPr", Beatrice, "\"Bold. I'll tell my quartermasters. Smolak will be delighted.\"", new[] { "TwoEnd" });
        t.Choice("TwoB", "\"Both. You need a strong Rizia more than I need your rifles.\"", new[] { "TwoBr" },
            script: Str(VarRumburgPriority, "both") + Inc("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("TwoBr", Beatrice, "\"Do I? How flattering. Both, then. And both will cost you.\"", new[] { "TwoEnd" });
        t.Logic("TwoEnd", new[] { "#316", "#317", "#318" });

        t.Register();
    }

    // ------------------------------------------------------------------ c393 Duke Reinhart in Porte Drazon

    private static void RegisterDuke()
    {
        const string conv = "Rizia/Turn 7/FPnT_DukeVisit";
        var t = new Talk(conv, "ZilleWar.E7.Duke.");

        // A defense pact with a kingdom that is fighting, or has just fought, Wehlen: the Duke's verdict on the Rizian army
        // (vanilla 362 when it is strong, 366 when it is not) speaks of what that army did in Zille.
        t.Line("AllianceAct", Axel,
            "\"With a kingdom that is at war with Wehlen as we speak? If Smolak's navy ever comes south, my harbors are the first thing it will see.\"",
            new[] { "#351" }, condition: Act, hooks: From(361));
        int[] strong = { 364, 849 };
        t.Variant("Strong362Lib", 362, strong, Lib, Axel,
            "\"I must say, you took Zille from Smolak in a single campaign. Any other world leader would have to be a fool to mess with you.\"",
            new[] { "#398" });
        t.Variant("Strong362Part", 362, strong, Part, Axel,
            "\"I must say, your army took Zille's harbor from Smolak and held it. Any other world leader would think twice before messing with you.\"",
            new[] { "#398" });
        t.Variant("Strong362Lost", 362, strong, Lost, Axel,
            "\"Your army couldn't take Zille, I know. But it is still the strongest in the region, and any other world leader would think " +
            "twice before messing with you.\"", new[] { "#398" });
        t.Variant("Weak366Lib", 366, new[] { 850 }, Lib, Axel,
            "\"You took Zille, I'll grant you that. But your army is spent, Your Majesty. How can it protect Pales when it can barely hold Rizia?\"",
            new[] { "#367" });
        t.Variant("Weak366Part", 366, new[] { 850 }, Part, Axel,
            "\"Your army couldn't finish the job in Zille, Your Majesty. How can it protect Pales when it can barely protect Rizia?\"",
            new[] { "#367" });
        t.Variant("Weak366Lost", 366, new[] { 850 }, Lost, Axel,
            "\"Your army couldn't take Zille from Smolak, Your Majesty. How can it protect Pales when it can barely protect Rizia?\"",
            new[] { "#367" });

        // Intermerkopum: "Morella, Derdia and Wehlen."
        t.Variant("Members699", 699, new[] { 659, 662, 671, 682 }, Declared, Romus,
            "\"Morella and Derdia. Perhaps even Wehlen, once Smolak is gone.\"", new[] { "Bluff" });
        t.Logic("Bluff", new[] { "BluffAct", "BluffOver" });
        t.Line("BluffAct", Axel,
            "\"Wehlen? You're at war with Wehlen, Your Majesty. If I didn't know better, I'd say you were bluffing.\"", new[] { "#684" },
            condition: Act);
        t.Line("BluffOver", Axel,
            "\"Wehlen? You stopped shooting at each other not long ago. If I didn't know better, I'd say you were bluffing.\"", new[] { "#684" });
        t.Register();
    }

    // ------------------------------------------------------------------ c377 Derdia

    private static void RegisterDerdia()
    {
        const string conv = "Rizia/Turn 7/FPnT_DerdiaNegotiations";
        var t = new Talk(conv, "ZilleWar.E7.Derdia.");
        Widen(t, "Battle44", 40, 44, ZilleOnly);         // "We must always be prepared for battle."
        Widen(t, "Prosperity426", 427, 426, ZilleOnly);  // "Prosperity? Your kingdom is at war."
        Widen(t, "Peace654", 655, 654, ZilleOnly);       // "once my country is again at peace"
        t.Variant("Strife447", 447, new[] { 446 }, Act, Romus,
            "\"We face no strife we cannot overcome, Supreme Wiseman. When Zille is ours again, Rizia will be stronger and more prosperous than ever.\"",
            new[] { "#451" });
        t.Variant("Strife447Lib", 447, new[] { 446 }, Lib, Romus,
            "\"We face no strife we cannot overcome, Supreme Wiseman. Zille is ours again, and Rizia will be stronger and more prosperous than ever.\"",
            new[] { "#451" });
        t.Variant("Strife447Part", 447, new[] { 446 }, Part, Romus,
            "\"We face no strife we cannot overcome, Supreme Wiseman. When the rest of Zille is ours, Rizia will be stronger and more prosperous than ever.\"",
            new[] { "#451" });
        t.Variant("Strife447Lost", 447, new[] { 446 }, Lost, Romus,
            "\"We face no strife we cannot overcome, Supreme Wiseman. Zille will be ours one day, and Rizia will be stronger and more prosperous than ever.\"",
            new[] { "#451" });
        // "You are placating Wiktor Smolak": the King has just fought him.
        t.Variant("Placate368", 368, new[] { 358 }, Declared, Azmal,
            "\"Even worse. You went to war with Wiktor Smolak, and still you keep the ban he asked of you. He denies Golcondists the right to exist in his country.\"",
            new[] { "#383" });
        // "The ban is only in place as part of an agreement I made with Wehlen."
        Vanilla.Require(conv, 238, Peace);
        // "...worth the potential loss of Zille": the war is already paid for
        t.Variant("Cost970", 970, new[] { 237 }, Declared, Romus,
            "\"I'm willing to negotiate on this point. But only if what you're willing to offer is worth what Zille has already cost us.\"",
            new[] { "#971" });
        t.Variant("Sal249", 249, new[] { 244 }, Declared, Sal,
            "\"Please understand, Supreme Wiseman. There are good people in Zille, Wruhecists and Wehzeks alike, who will pay for every " +
            "promise this war breaks.\"", new[] { "#250" });
        t.Register();
    }

    private static void RegisterDerdiaSubs()
    {
        // Fighting Assistance (Friendly)
        var f = new Talk("Rizia/Turn 7/Fighting Assistance Friendly", "ZilleWar.E7.FightF.");
        f.Variant("Ask7", 7, new[] { 178 }, Act, Romus,
            "\"We are at war with Wehlen over Zille, Supreme Wiseman. I would humbly call upon your great warrior culture for assistance.\"",
            new[] { "#103" });
        f.Variant("Prayed77", 77, new[] { 76 }, Act, Azmal,
            "\"Rizia at war with Wiktor Smolak. I have prayed for this day, Your Majesty. Normally, Derdia would charge you for its services. " +
            "But my Disciples are eager to help you take back your land from the anti-Golcondist Wehzeks.\"",
            new[] { "#79", "#82", "#85" });
        f.Variant("Why119", 119, new[] { 118 }, Act, Azmal,
            "\"But I am curious what Rizia means to do with such technology. You are at war with Wehlen, after all. Do you intend to aim it at Wraclawitz?\"",
            new[] { "Aim", "#125", "#126" });
        // After the war: what the King asks Derdia for, and why Rizia wants missiles.
        S7 ask = new(
            "\"Zille is ours again, Supreme Wiseman, but Smolak will not forget it. I would humbly call upon your great warrior culture to help us hold it.\"",
            "\"Smolak still holds part of Zille behind an armistice line, Supreme Wiseman. I would humbly call upon your great warrior culture for assistance.\"",
            "\"Our army could not take Zille from Smolak, Supreme Wiseman. I would humbly call upon your great warrior culture for assistance.\"");
        ask.Apply(f, "Ask7", 7, new[] { 178 }, Romus, new[] { "#103" });
        f.Variant("Prayed77Lib", 77, new[] { 76 }, Lib, Azmal,
            "\"Normally, Derdia would charge you for their services. But my Disciples are eager to help Rizia hold its land against the anti-Golcondist Wehzeks.\"",
            new[] { "#79", "#82", "#85" });
        const string notAtWar = "\"But I am curious what Rizia means to do with such technology. Your war with Wehlen is over, after all. Or is it?\"";
        f.Variant("Why119Over", 119, new[] { 118 }, $"{Over} and {NoPales}", Azmal, notAtWar, new[] { "#120", "#125", "#121", "#126" });
        f.Choice("Aim", "\"At the Wehzek batteries above Zille's harbor. Nothing more.\"", new[] { "AimR" });
        f.Line("AimR", Azmal, "\"A war with limits. Derdia approves of limits, Your Majesty. They are so rarely respected.\"", new[] { "#144" });
        f.Register();

        // Fighting Assistance (Unfriendly)
        var u = new Talk("Rizia/Turn 7/Fighting Assistance Unfriendly", "ZilleWar.E7.FightU.");
        u.Variant("Ask42", 42, new[] { 155 }, Act, Romus,
            "\"We are at war with Wehlen over Zille, Supreme Wiseman. I would humbly call upon your great warrior culture for assistance.\"",
            new[] { "#104" });
        u.Variant("Why113", 113, new[] { 112 }, Act, Azmal,
            "\"But I am curious what Rizia means to do with such technology. You are at war with Wehlen, after all. Do you intend to aim it at Wraclawitz?\"",
            new[] { "Aim", "#128", "#115", "#119" });
        ask.Apply(u, "Ask42", 42, new[] { 155 }, Romus, new[] { "#104" });
        u.Variant("Why113Over", 113, new[] { 112 }, $"{Over} and {NoPales}", Azmal, notAtWar, new[] { "#114", "#128", "#115", "#119" });
        u.Variant("Free38Lib", 38, new[] { 49 }, Lib, Romus, "\"Together, we will make sure Smolak never sets foot in Zille again.\"", new[] { "#41" });
        u.Variant("Free38Part", 38, new[] { 49 }, Part, Romus, "\"Together, we will free the rest of Zille and humiliate Wehlen.\"", new[] { "#41" });
        u.Variant("Never70Lib", 70, new[] { 41, 71, 95 }, Lib, Romus,
            "\"Actually, please disregard my request. Zille needs peace now, not more soldiers.\"", new[] { "Appease52" });
        // "You would rather continue to appease Smolak?" after a war with him.
        u.Variant("Appease52", 52, new[] { 70 }, Declared, Azmal,
            "\"You would rather leave Smolak be? 'It is easier to calm a storm with whispers than satisfy the whims of a madman.'\"",
            new[] { "#96" });
        u.Choice("Aim", "\"At the Wehzek batteries above Zille's harbor. Nothing more.\"", new[] { "AimR" });
        u.Line("AimR", Azmal, "\"A war with limits. How unusual for a king.\"", new[] { "#140" });
        u.Register();

        // Energy partnership: the Gulf of Moridia is a war zone, and Wehzek oil is out of the question.
        const string gulf = "\"Your Gulf is a war zone now, Your Majesty. My tankers would need an escort, and I suspect your navy has other plans.\"";
        const string harbor = "\"Now that your Zille harbor is free of that madman, the distance between our ports is practically nothing.\"";
        var ef = new Talk("Rizia/Turn 7/Energy Partnership Friendly", "ZilleWar.E7.EnergyF.");
        Vanilla.Require("Rizia/Turn 7/Energy Partnership Friendly", 58, Peace);
        Widen(ef, "AtWar12", 9, 12, ZilleOnly);
        ef.Variant("Gulf60", 60, new[] { 12 }, Act, Azmal, gulf, new[] { "#89" });
        ef.Variant("Harbor90", 90, new[] { 85 }, Lib, Azmal, harbor, new[] { "#52", "#53" });
        // "Once your Zille harbor is recovered": it is, when the city is ours (liberated or divided).
        const string harborOurs = "\"Think about it. Now that your Zille harbor is free of that madman, the distance between our ports is practically nothing.\"";
        string cityOurs = $"({Lib}) or ({Part})";
        ef.Variant("Harbor89", 89, new[] { 60, 77 }, cityOurs, Azmal, harborOurs, new[] { "#52", "#53" });
        ef.Register();
        var eu = new Talk("Rizia/Turn 7/Energy Partnership Unfriendly", "ZilleWar.E7.EnergyU.");
        Vanilla.Require("Rizia/Turn 7/Energy Partnership Unfriendly", 62, Peace);
        Widen(eu, "AtWar15", 12, 15, ZilleOnly);
        eu.Variant("Gulf64", 64, new[] { 15 }, Act, Azmal, gulf, new[] { "#103" });
        eu.Variant("Harbor104", 104, new[] { 99 }, Lib, Azmal, harbor, new[] { "#56", "#57" });
        eu.Variant("Harbor103", 103, new[] { 64, 90 }, cityOurs, Azmal, harborOurs, new[] { "#56", "#57" });
        eu.Register();

        // Morella discussion: "My country's fighting a war, Supreme Wiseman."
        var mf = new Talk("Rizia/Turn 7/Morella Discussion Friendly", "ZilleWar.E7.MorellaF.");
        WidenByParent(mf, 378, "War41", 41);
        WidenByParent(mf, 378, "War53", 53);
        mf.Register();
        var mu = new Talk("Rizia/Turn 7/Morella Discussion Unfriendly", "ZilleWar.E7.MorellaU.");
        WidenByParent(mu, 379, "War32", 32);
        WidenByParent(mu, 379, "War79", 79);
        mu.Register();
    }

    /// <summary>One line in three post-war versions (liberated, divided, lost) replacing a vanilla line or choice.</summary>
    private sealed record S7(string Lib, string Part, string Lost)
    {
        public void Apply(Talk t, string name, int entry, int[] parents, string speaker, string[] next)
        {
            t.Variant(name + "Lib", entry, parents, EarlyT7.Lib, speaker, Lib, next);
            t.Variant(name + "Part", entry, parents, EarlyT7.Part, speaker, Part, next);
            t.Variant(name + "Lost", entry, parents, EarlyT7.Lost, speaker, Lost, next);
        }
    }

    // Hubs of the Morella discussion's Pales_War branches (checked with node.py: target <- condition <- hub).
    private static readonly Dictionary<(int conv, int target), int> MorellaHubs = new()
    {
        [(378, 41)] = 38, [(378, 53)] = 50, [(379, 32)] = 25, [(379, 79)] = 76,
    };

    private static void WidenByParent(Talk t, int conv, string name, int target) =>
        Widen(t, name, MorellaHubs[(conv, target)], target, ZilleOnly);
}
