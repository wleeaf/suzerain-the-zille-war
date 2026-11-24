using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Sweep of the Turn 8 domestic and wedding scenes, read line by line:
/// the general council (hub c419, security c590, economy c591, garden c592), Manus's constitution (c407),
/// Hugo's resignation (c413), the night before the wedding (c414) and the wedding (c417).
/// LateT8 already covers the foreign-affairs briefing, Rusty's train, the House, the wedding mood line, Hugo's Rico lines
/// in c593 and Lucita's line on the night before; nothing here touches those entries.
/// </summary>
internal static class SweepMiddleT8
{
    private const string Hugo = "Hugo Toras";
    private const string Elena = "Elena Werner";
    private const string Manus = "Manus Sazon";
    private const string Lucita = "Lucita Azaro";
    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Rayne = "Anton Rayne (R)";
    private const string R = Talk.Romus;

    private static string ZilleOnly => EarlyT7.ZilleOnly;
    private static string NoPales => EarlyT7.NoPales;
    /// <summary>A divided end with every Zille city Rizian (only after the treaty's Auremina term).</summary>
    private static readonly string AllHeld = And(Part, Front.LWehzekExactly(Array.Empty<string>()));
    private const string LivingBad = "Variable[\"RiziaDLC.Situation_Welfare_LivingStandards_Bad\"] == true";

    /// <summary>Rumburg actually helped (oil deal, levers or credits), not merely rang.</summary>
    private const string BeatriceHelped = LateT9.LuaRumburgHelped;

    public static void Register()
    {
        RegisterCouncilHub();
        RegisterSecurity();
        RegisterEconomy();
        RegisterGarden();
        RegisterConstitution();
        RegisterHugoLeaves();
        RegisterNightBefore();
        RegisterWedding();
    }

    // ------------------------------------------------------------------ c419 the general council: Hugo's welcome and the unrest report

    private static void RegisterCouncilHub()
    {
        const string conv = "Rizia/Turn 8/A_GeneralCouncilMeeting";
        var t = new Talk(conv, "ZilleWar.SM.T8.Council.");
        // "Commanding the armed forces must be taking its toll on you. We've barely seen one another outside of the War Room lately."
        EarlyT7.Widen(t, "WarRoom13", 4, 13, ZilleOnly);
        // "the war doesn't make things any easier" / "the economic squeeze caused by the war" (Hugo and Vina): the Zille war's bill,
        // still being paid after the fighting stops.
        string squeeze = And(War, NoPales);
        EarlyT7.Widen(t, "Squeeze174", 171, 174, squeeze);
        EarlyT7.Widen(t, "Squeeze178", 175, 178, squeeze);
        EarlyT7.Widen(t, "Squeeze201", 198, 201, squeeze);
        EarlyT7.Widen(t, "Squeeze205", 202, 205, squeeze);
        t.Register();
    }

    // ------------------------------------------------------------------ c590 security: no peacetime army overview while the army fights

    private static void RegisterSecurity()
    {
        const string conv = "Rizia/Turn 8/SECURITY/SECURITY Conversation";
        var t = new Talk(conv, "ZilleWar.SM.T8.Security.");
        t.Logic("Front", C("Busy"), condition: ZilleOnly, hooks: Talk.From(3));
        Councilor(t, "Busy", "", new[] { "BusyR1", "BusyR2" },
            lucita: "\"With respect, Your Majesty, most of our divisions are in Zille, and the figures change by the hour. I would rather keep them for the war table, where the maps are.\"",
            taddeus: "\"Half the army is in Zille, Your Majesty, and the other half is packing to join it. Any figure I give you now will be wrong by supper. The war table will have them.\"",
            titus: "\"Sir, with most of our divisions committed in Zille, the general staff would rather brief you at the war table directly.\"");
        t.Choice("BusyR1", "\"Very well. At the war table, then.\"", new[] { "#2" });
        t.Choice("BusyR2", "\"Just tell me we're winning.\"", C("Win"));
        Councilor(t, "Win", "", new[] { "#2" },
            lucita: "\"We are, Your Majesty. Slowly, and at a price. I'll show you both.\"",
            taddeus: "\"We are, Your Majesty. Ask me again after the next push.\"",
            titus: "\"The general staff says so, sir. I'll believe it when the flags go up.\"");
        t.Register();
    }

    // ------------------------------------------------------------------ c591 economy: Elena counts the cost of the war

    private static void RegisterEconomy()
    {
        const string conv = "Rizia/Turn 8/ECONOMY/ECONOMY Conversation";
        var t = new Talk(conv, "ZilleWar.SM.T8.Economy.");
        // While the Zille war is fought: vanilla's war lines ("the war has influenced my department very negatively", the war
        // profits tax, "We are doing our best to end the war as quickly as possible").
        EarlyT7.Widen(t, "War59", 56, 59, ZilleOnly);
        // After it: the bill.
        foreach ((string sfx, string state, string text) in new[]
                 {
                     ("PartAll", AllHeld, "\"The treaty stopped the shelling, Your Majesty, but not the spending. We are paying to garrison all of Zille now, and the war pensions are a permanent line in my budget.\""),
                     ("Lib", Lib, "\"Zille has come home, Your Majesty, and the bill came with it. We are still paying for the shells and the fuel, and the pensions for the widows will be with us for a generation. The Auremina mines may pay some of it back, in a few years.\""),
                     ("Part", And(Part, $"not ({AllHeld})"), "\"The armistice stopped the shelling, Your Majesty, but not the spending. We are paying to garrison our side of the line in Zille, and the war pensions are now a permanent line in my budget.\""),
                     ("Lost", And(Lost, Front.LWehzekExactly(Front.Cities)), "\"I won't dress it up, Your Majesty. The Zille campaign emptied the war chest, and all we have to show for it are the pensions for the widows.\""),
                 })
        {
            t.Line("Bill" + sfx, Elena, text, new[] { "#68" }, condition: And(state, NoPales), hooks: Talk.From(56));
        }

        // Lost only means Zille city is Wehzek: Auremina, Isor or Nanta can still be Rizian (no recognized treaty handed them back)
        foreach (List<string> held in Front.Subsets("Auremina", "Isor", "Nanta"))
        {
            var wehzek = Front.Cities.Where(c => !held.Contains(c)).ToList();
            t.Line("BillLostH" + string.Concat(held.Select(c => c[0])), Elena,
                $"\"I won't dress it up, Your Majesty. The Zille campaign emptied the war chest. We still hold {Front.Join(held)}, and we pay to garrison {(held.Count > 1 ? "them" : "it")}, and the pensions for the widows will be with us for a generation.\"",
                new[] { "#68" }, condition: And(Lost, Front.LWehzekExactly(wehzek), NoPales), hooks: Talk.From(56));
        }

        t.Register();
    }

    // ------------------------------------------------------------------ c592 the garden: Manus and Sal on the war

    private static void RegisterGarden()
    {
        const string conv = "Rizia/Turn 8/Garden/Garden Conversation";
        var t = new Talk(conv, "ZilleWar.SM.T8.Garden.");
        string bad = And(ZilleOnly, LivingBad);
        EarlyT7.Widen(t, "Worsened100", 88, 100, And(War, NoPales, LivingBad)); // "especially since the war began"
        EarlyT7.Widen(t, "StopWar127", 122, 127, bad);                         // "stop the war as soon as possible"
        EarlyT7.Widen(t, "Pleading215", 210, 215, bad);                        // "pleading for an end to the war"
        EarlyT7.Widen(t, "Fighting565", 562, 565, ZilleOnly);                  // "I'm fighting a war, son."
        t.Register();
    }

    // ------------------------------------------------------------------ c407 Manus and the constitution

    private static void RegisterConstitution()
    {
        const string conv = "Rizia/Turn 8/A_Reform_Manus";
        var t = new Talk(conv, "ZilleWar.SM.T8.Reform.");
        t.Line("Rearview", Manus, "\"Of course, I'm hoping the war in Zille will be far in our rearview mirror before the reforms pass. A constitution shouldn't be born in a casualty list.\"",
            new[] { "#210" }, condition: ZilleOnly, hooks: Talk.From(814));
        t.Register();
    }

    // ------------------------------------------------------------------ c413 Hugo resigns: no negotiations, no extradition

    private static void RegisterHugoLeaves()
    {
        const string conv = "Rizia/Turn 8/A_HugoLeaves";
        var t = new Talk(conv, "ZilleWar.SM.T8.HugoLeaves.");
        // "I must... congratulate you on negotiating the successful return of Zille." / "...your negotiations with Wehlen did not result
        // in Zille's return." Then "Despite the terrible cost that has been paid."
        Swap(t, conv, "Congrats107", 107, new[] { 105 }, Hugo, new S(Lib: "\"I must... congratulate you on the liberation of Zille.\""), new[] { "#108" });
        Swap(t, conv, "Sorry109", 109, new[] { 106 }, Hugo, new S(
            Ongoing: "\"I was... sorry to hear that the fighting in Zille drags on.\"",
            Lib: "\"I was... glad to hear the guns have finally stopped in Zille.\""), new[] { "#110" });
        Swap(t, conv, "Sorry109P", 109, new[] { 106 }, Hugo, new S(Part: "\"I was... sorry to hear that Zille remains cut in two.\""), new[] { "#110" },
            when: $"not ({AllHeld})");
        // every city of Zille is Rizian, the last one by treaty
        Swap(t, conv, "Sorry109A", 109, new[] { 106 }, Hugo, new S(Part: "\"I was... glad to hear that all of Zille is ours again, at last.\""), new[] { "#110" },
            when: AllHeld);
        // a ceasefire without Zille city is no defeat on paper (War_Zille_Defeated stays false)
        Swap(t, conv, "Sorry109L", 109, new[] { 106 }, Hugo, new S(Lost: "\"I was... sorry to hear of our defeat in Zille.\""), new[] { "#110" },
            when: $"not ({Ceasefire})");
        Swap(t, conv, "Sorry109C", 109, new[] { 106 }, Hugo, new S(Lost: "\"I was... sorry to hear that the guns stopped with Zille city still in Wehzek hands.\""),
            new[] { "#110" }, when: Ceasefire);
        // The King's answers assumed Rico was traded for Zille.
        Swap(t, conv, "Sacrifice15", 15, new[] { 113 }, R, new S(
            Lib: "\"Our soldiers brought Zille home without him, Uncle. Rico stays where he is until I am sure of him.\""), new[] { "#116" },
            when: "Variable[\"RiziaDLC.Rico_Extradited\"] ~= true");
        Swap(t, conv, "Word115", 115, new[] { 114 }, R, new S(
            Ongoing: "\"Not while this war is being fought, Uncle. Rico stays where he is until it is over.\"",
            Lib: "\"Our soldiers brought Zille home without him, Uncle. Rico stays where he is until I am sure of him.\"",
            Part: "\"I did what I thought was right. With Wehlen still on our border, I cannot afford to be wrong about Rico.\"",
            Lost: "\"I did what I thought was right. After Zille, I cannot afford to be wrong about anyone.\""), new[] { "#116" },
            when: "Variable[\"RiziaDLC.Rico_Extradited\"] ~= true");
        // Rico was handed to Wehlen under the full ceasefire terms the King accepted during the war (the treaty table or LateT8)
        const string gaveWord = "\"I gave Smolak my word to end the war, Uncle. I could not take it back the moment the guns went quiet.\"";
        const string promised = "Variable[\"RiziaDLC.Diplomacy_Wehlen_PromiseRicoExtradition\"] == true";
        Swap(t, conv, "Word115X", 115, new[] { 114 }, R, new S(Part: gaveWord, Lost: gaveWord), new[] { "#116" },
            when: $"Variable[\"RiziaDLC.Rico_Extradited\"] == true and {promised}");
        // or the King granted Smolak's request for him at the treaty table, weeks after the guns went quiet
        Swap(t, conv, "Word115T", 115, new[] { 114 }, R, new S(
            Part: "\"Smolak asked for him at the table, Uncle, and I needed that treaty more than I needed my cousin.\"",
            Lost: "\"Wehlen held the stronger hand at the table, Uncle. Rico was part of the price of the treaty.\""), new[] { "#116" },
            when: $"Variable[\"RiziaDLC.Rico_Extradited\"] == true and not ({promised})");
        t.Register();
    }

    // ------------------------------------------------------------------ c414 the night before the wedding

    private static void RegisterNightBefore()
    {
        const string conv = "Rizia/Turn 8/P_NightBeforeWedding";
        var t = new Talk(conv, "ZilleWar.SM.T8.Night.");
        // "After an endless day of war briefings and strategy planning sessions..."
        EarlyT7.Widen(t, "Briefings189", 194, 189, ZilleOnly);

        // Lucita's proposal: "We've held our ground against Pales. But when it comes to the two of us... I surrender."
        string[] after148 = { "#794", "#88", "#150", "#793" };
        t.Line("Battle", Lucita, "\"Of course, that was before you and I found ourselves fighting an actual war.\"",
            new[] { "SurrOn", "SurrLib", "SurrPart", "SurrLost" }, condition: And(War, NoPales), hooks: Talk.From(145));
        t.Line("SurrOn", Lucita, "\"We've held our ground against the Wehzeks. But when it comes to the two of us... well, Your Majesty, I surrender.\"",
            after148, condition: Ongoing);
        t.Line("SurrLib", Lucita, "\"Zille never surrendered to us. We had to take it, town by town. But when it comes to the two of us... well, Your Majesty, I surrender.\"",
            after148, condition: Lib);
        t.Line("SurrPart", Lucita, "\"We took Zille city, and we held it. But when it comes to the two of us... well, Your Majesty, I surrender.\"",
            after148, condition: Part);
        t.Line("SurrLost", Lucita, "\"I am not used to losing, Your Majesty. Zille taught me how. So when it comes to the two of us... I surrender.\"",
            after148, condition: Lost);
        t.Register();
    }

    // ------------------------------------------------------------------ c417 the royal wedding

    private static void RegisterWedding()
    {
        const string conv = "Rizia/Turn 8/A_RoyalWedding";
        var t = new Talk(conv, "ZilleWar.SM.T8.Wedding.");

        // Generic war lines also fit a Zille war that is still being fought.
        EarlyT7.Widen(t, "Solemn83", 96, 83, ZilleOnly);        // "We're still at war. I've got to look solemn and determined."
        EarlyT7.Widen(t, "Honor254", 255, 254, ZilleOnly);      // Hugo: "in this time of war"
        EarlyT7.Widen(t, "Toast949", 950, 949, ZilleOnly);      // "In this time of war, it is especially important..."
        t.Logic("Troops", new[] { "#1074", "#1091" }, condition: ZilleOnly, hooks: Talk.From(1088)); // the troops in the toast
        EarlyT7.Widen(t, "Lucita1101", 1098, 1101, ZilleOnly);  // "invaluable counsel and support during the war"
        EarlyT7.Widen(t, "Victory1203", 1207, 1203, ZilleOnly); // "We shall lead our troops to victory, hand in hand!"

        // Beatrice, who has been paying for part of the war.
        Swap(t, conv, "Bea329", 329, new[] { 328 }, Beatrice, new S(
            Ongoing: "\"Why, I wouldn't miss it for anything in the world. Not even for your war, Romus, which my ministers now discuss more often than my own budget.\"",
            Lib: "\"Why, I wouldn't miss it for anything in the world. And congratulations on Zille. Do try to remember who helped you pay for it.\"",
            Part: "\"Why, I wouldn't miss it for anything in the world. Zille's harbor and a whole wedding. It's more than most kings manage in a reign.\"",
            Lost: "\"Why, I wouldn't miss it for anything in the world. Your family has had its fill of bad news from Zille, Romus. Let's make today a victory.\""),
            new[] { "#330" }, when: BeatriceHelped);

        // Rayne's toast to Wehlen's friends (a Sordland game with the Wehzek joint operation, and Zille "returned").
        Swap(t, conv, "Rayne687", 687, new[] { 685 }, Rayne,
            S.All("\"Sordland has always been a friend of Wehlen, Your Majesty. I would like to think we can be a friend of Rizia too, whatever happened in Zille.\""),
            new[] { "#692" });
        t.Register();
    }
}
