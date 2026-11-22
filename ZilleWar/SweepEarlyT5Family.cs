using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Decision;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The royal family's evenings in the first weeks of the war (gaps left by EarlyT4/EarlyT5):
/// - the Cantuavo winery (c317) and the drinking game (c318), the night of Konstantium: Duke Hubertus mentions the
///   reservists, the King's "big, bloody war" confession becomes a confession of fact, Lucita's "old army trick" gains a
///   war, and the King no longer sleeps soundly without a thought for the north;
/// - the royal yacht (c320): Manus Sazon on Zille (his father's rebellion cost Brenas that land), Vina on the yacht's upkeep
///   while soldiers sleep in the mud, Vina's answer to a tasteless joke, Hegel's latest letter, the war named as the reason
///   democracy has to wait;
/// - decisions: the rail crisis (T4) and the royal yacht (T5) reworded while the war is fought; the yacht can be handed to
///   the navy as a hospital ship. Rufus Gragg's gossip on the winery and the yacht, and Carlos Azaro's memorial visit, learn
///   about the war.
/// </summary>
internal static class SweepEarlyT5Family
{
    private const string Winery = "Rizia/Turn 4/P_PabelLucitaRomance";
    private const string Drinking = "Rizia/Turn 4/Drinking Game";
    private const string Yacht = "Rizia/Turn 5/P_ManusVinaTalk";

    private const string Hubertus = "Hubertus Toras";
    private const string Lucita = "Lucita Azaro";
    private const string Vina = "Vina Toras";
    private const string Manus = "Manus Sazon";

    private const string Transport = "Turn04_Decision_PublicTransportationWeak_PenaltyWarning";
    private const string YachtDecision = "Turn05_Decision_RoyalYachtRestoration";
    private const string HospitalOption = "Hand the Rizania to the navy as a hospital ship (-1 Budget)";

    /// <summary>The royal yacht Rizania was handed to the navy as a hospital ship for the Zille front (instead of restored or scrapped).</summary>
    public const string VarYachtHospital = "ZilleWar.SE_Fam_YachtHospital";

    // c317/c318 play right after the declaration (Turn 4 steps 6-7), so "declared" is the war; c320 is Turn 5.
    private static readonly string War = WarFlags.LDeclared;

    public static void Register()
    {
        foreach (string v in new[] { VarYachtHospital })
        {
            Variables.Register(v);
        }

        RegisterWinery();
        RegisterDrinkingGame();
        RegisterYacht();
        RegisterDecisions();
        RegisterPress();

        SuzerainModdingKit.Events.OnDecisionShow += (_, _) => OnDecisionShow();
        SuzerainModdingKit.Events.OnDecisionFinished += (_, e) => OnDecisionFinished(e.SelectedOptionInfo);
    }

    public static void OnStep()
    {
    }

    // ------------------------------------------------------------------ c317 the Cantuavo winery

    private static void RegisterWinery()
    {
        var t = new Talk(Winery, "ZilleWar.SE.Fam.Winery.");

        // [584] Hubertus: "...we don't take your appearance for granted. You've been very busy lately."
        t.Variant("Hubertus", 584, new[] { 577 }, War, Hubertus,
            "\"Nonetheless, we don't take your appearance for granted. Not with the country at war. I saw our reservists off at the " +
            "station tonight, Your Majesty. Half of them had been picking these very grapes.\"", new[] { "#590" });

        // [371]/[427] "I slept more soundly than I had in months." on the night the trains went north.
        const string sleep = "I slept more soundly than I had in months. It only struck me in the morning that, somewhere in the north, " +
                             "my soldiers had not.";
        t.Variant("Sleep371", 371, new[] { 370 }, War, Narrator, sleep, new[] { "#463" });
        t.Variant("Sleep427", 427, new[] { 426 }, War, Narrator, sleep, new[] { "#463" });
        t.Register();
    }

    // ------------------------------------------------------------------ c318 the drinking game

    private static void RegisterDrinkingGame()
    {
        var t = new Talk(Drinking, "ZilleWar.SE.Fam.Drinking.");

        // [245] "I wish Rizia would get into a big, bloody war. Just so I can prove myself." That same night, after Konstantium, the wish
        // is granted; EarlyT4's Laurento ("...After Konstantium, I'm no longer sure") answers this line. [716] still records it.
        t.Variant("WantWar", 245, new[] { 244 }, War, Romus,
            "\"Part of me wanted a big, bloody war. Just so I could prove myself. Now I have one.\"", new[] { "#716" });

        t.Variant("Team", 585, new[] { 584 }, War, Lucita,
            "\"The more intimately we know each other, the better we can fight as a team. It's an old army trick. And this council " +
            "has a war to fight now.\"", new[] { "#681" });
        t.Variant("Defend", 616, new[] { 615 }, War, Lucita,
            "\"The more intimately we know about each other, the better we can defend each other. It's an old army trick. We'll need " +
            "it, now that we're at war.\"", new[] { "#692" });

        t.Variant("Sleep527", 527, new[] { 526, 535, 573, 691 }, War, Narrator,
            "I fell asleep at last, though not as soundly as I would have liked, with the telephone on the nightstand where I could reach it.",
            new[] { "#2" });
        t.Register();
    }

    // ------------------------------------------------------------------ c320 the royal yacht

    private static void RegisterYacht()
    {
        var t = new Talk(Yacht, "ZilleWar.SE.Fam.Yacht.");

        // Vina on the yacht's upkeep ([42]/[45]/[48]).
        t.Variant("Upkeep42", 42, new[] { 39 }, Active, Vina,
            "\"Our country shouldn't be paying for the upkeep of the King's luxury vessel. Least of all while our soldiers are " +
            "sleeping in the mud up north. Don't you think so, Mr. Sazon?\"", new[] { "#43" });
        t.Variant("Upkeep45", 45, new[] { 44 }, Active, Vina,
            "\"Our country shouldn't have to pay for the upkeep of the King's luxury vessel anyway. Not while our soldiers are " +
            "sleeping in the mud up north.\"", new[] { "#46" });
        t.Variant("Upkeep48", 48, new[] { 47 }, Active, Vina,
            "\"But pay for a ship? With our soldiers in the mud up north, Mr. Sazon?\"", new[] { "#49" });

        // Settled at the table ([50] -> [54]): the heir of House Sazon on Zille, the land his father's rebellion cost Brenas.
        t.Line("Zille", Manus,
            "\"Before we begin, Your Majesty. I wanted Zille back without a single shot fired. My father's rebellion cost Brenas " +
            "that land, and I had hoped my generation could win it back with signatures. Instead, Brenas is sending its sons north. " +
            "All I ask is that you bring as many of them home as you can.\"", new[] { "ZilleHand" }, condition: AuditT45.Fighting, hooks: From(50));
        // The King sought the ceasefire at this turn's War Council.
        t.Line("ZilleCeasefire", Manus,
            "\"Before we begin, Your Majesty. Thank you for stopping the fighting. I wanted Zille back without a single shot fired, " +
            "and Brenas has buried enough of its sons this month to remember why.\"", new[] { "ZilleHand" },
            condition: $"{WarFlags.L(WarFlags.Ceasefire)} and {WarFlags.LNot(WarFlags.Liberated)}", hooks: From(50));
        t.Line("ZilleHand", Narrator, "Vina found Manus's hand under the table and held it.", new[] { "#54" });

        // Vina's correspondence with Hegel ([806]/[807] -> [809]): the Chancellor's latest letter.
        t.Line("Hegel", Vina,
            "\"Konstantium changed nothing. He wrote again: Valgsland will not sit back while two governments spend workers' " +
            "lives over an expired lease, though it will send no ship. I think he meant for me to show you.\"", new[] { "#809" },
            condition: Active, hooks: From(806, 807));

        // [990] "Cheer up. It's not like anyone died." -> [1014] -> [1015] "Please, Father. You're making it worse."
        t.Variant("Died", 1015, new[] { 1014 }, Active, Vina,
            "\"People are dying in the north every day, Father. Choose your jokes more carefully.\"", new[] { "#1007" });

        // [1251] "I can't solve our myriad domestic and international crises while shepherding Rizia through the transition..."
        t.Variant("Pressure", 1251, new[] { 1243, 1247, 1249 }, Active, Romus,
            "\"I have an army in the field and a dozen crises at home. I can't shepherd Rizia to democracy in the middle of a war. " +
            "Maybe in another decade.\"",
            new[] { "#1252" });
        t.Register();
    }

    // ------------------------------------------------------------------ decisions

    private static void RegisterDecisions()
    {
        Decision(Transport, o => !IsActive ? null :
            "The public transportation system is grappling with an efficiency crisis, strained by burgeoning demands and passenger " +
            "numbers. Since the declaration of war, military trains carrying reservists, munitions and fuel to the north have priority " +
            "on every line, and civilian timetables have all but collapsed. Transportation officials are pushing for urgent action, " +
            "warning that a network this worn could soon fail the army as well as the public.\n\nThe outcome will crucially impact the " +
            "daily lives of the citizens, and how quickly our soldiers reach the front.");
        Decision(YachtDecision, o => !IsActive ? null :
            "The monarchy's grand yacht Rizania, commissioned in 1901, now shows signs of wear and tear. Its restoration holds deep " +
            "symbolic value, but it would occupy the Monqiz yards and their craftsmen at a time when the navy needs every berth for the " +
            "war in the north. The alternative is to sell the antique piece. The navy, for its part, has asked whether a fast ship with " +
            "large state rooms might carry the wounded home from the front.");
    }

    private static void OnDecisionShow()
    {
        if (DecisionManager.CurrentDecisionName != YachtDecision || !IsActive || Variables.GetInt("RiziaDLC.Resources_Budget") < 1)
        {
            return;
        }

        DecisionManager.AddOption(HospitalOption);
    }

    private static void OnDecisionFinished(DecisionOptionInfo o)
    {
        if (o?.DecisionName != YachtDecision || o.Text != HospitalOption)
        {
            return;
        }

        try
        {
            Lua("Variable[\"RiziaDLC.Resources_Budget\"] = (Variable[\"RiziaDLC.Resources_Budget\"] or 0) - 1; " +
                "Variable[\"RiziaDLC.Country_PublicOpinion\"] = (Variable[\"RiziaDLC.Country_PublicOpinion\"] or 0) + 1;");
            Variables.Set(VarYachtHospital, true);
            NewsWire.Publish("SE_Fam_YachtHospital", "RH", "Royal Yacht Rizania Joins the Fleet as a Hospital Ship", P(
                "The RN Rizania, the royal yacht commissioned in 1901, will not be repainted after all. By order of His Majesty, the old " +
                "vessel has been handed to the Royal Navy and is being refitted at Monqiz as a hospital ship, to carry wounded soldiers " +
                "home from the northern front.",
                "Her state rooms are to become wards and her dining saloon an operating theater. \"The Rizania has carried kings for half " +
                "a century,\" a palace spokesman said. \"It is only right that she should now carry the men who fight for them.\""));
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Royal yacht hospital option failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ press

    private static void RegisterPress()
    {
        // Rufus Gragg's party column, written as if nothing had happened in Konstantium.
        foreach (string name in new[] { "Turn04_RH_Gossip_Winery", "Turn04_RH_Gossip_Winery_Lucita" })
        {
            bool lucita = name.EndsWith("Lucita", StringComparison.Ordinal);
            News(name, (_, _) => WineryGossip(lucita),
                _ => lucita ? "Rufus Gragg’s Royal Gossip: King, Duchess Pop The Cork as the Country Goes to War"
                            : "Rufus Gragg’s Royal Gossip: War or No War, Valenqiris Uncorks");
        }

        // "Which color will the King paint his boat?" while the navy fights.
        News("Turn05_RH_Decision_RoyalYachtRestoration_Gossip", (_, _) => P(
                "Now that the Treasury has announced funds will be allocated to restoring the King's Royal Yacht, the RN Rizania, your " +
                "humble columnist has been flooded with letters. Not about the color of the new paint, dear readers, though gold would " +
                "still be the most royal. About the timing.",
                "With our boys up north and the Monqiz yards working day and night for the navy, some of you wonder whether the old " +
                "girl could not have waited a few more months. The palace insists the restoration is a matter of national heritage and " +
                "will not delay a single warship. Your columnist will take their word for it, and keep counting the ships."),
            _ => "Rufus Gragg’s Royal Gossip: A Fresh Coat of Paint, in Wartime?", s => s == StActive);

        // The Commander of the Armed Forces at a southern memorial in the middle of the northern war.
        Report("Turn05_Montaklar_CarlosVisitsMemorial", (_, _) => P(
                "General Carlos Robles Azaro paid a brief visit to the mausoleum near the southern border to honor the fallen soldiers " +
                "of the Reclamation War, before returning to the general staff and the war in Zille. As the sole surviving officer of " +
                "the encircled brigade at Ziqes, the moment carried deep personal significance.",
                "The royal band played a mournful hymn as he placed a wreath at the memorial. Officers who accompanied him say the General " +
                "stood in silence before the names of his fallen comrades for a long time, and that on the drive back to the airfield he " +
                "asked only for the latest casualty lists from the north."),
            when: s => s == StActive);
    }

    private static string WineryGossip(bool lucita)
    {
        var paragraphs = new List<string>
        {
            "If you weren't at the \"Romus the Red\" uncorking gala in Qalus last night, you were probably glued to the wireless like the " +
            "rest of us. For their big bash, winemaking power couple Lord Eduard and Lady Sephina opened their doors to the Royal Family, " +
            "the Royal Council and a who's who of the Valenqiris elite, from estate owner and noted wine patron Fabien Toras to " +
            "heavyweight investor Gustav Montrel.",
            "Naturally, the talk of the tent was Konstantium. Your humble columnist counted three viscounts boasting of sons in the " +
            "reserve, and one baroness who wanted it known that she had already knitted her first pair of socks for the front.",
        };
        if (Variables.GetBool(EarlyT4.VarToastSoldiers))
        {
            paragraphs.Add("And the King? His Majesty raised the very first glass of his own vintage to the soldiers marching north. " +
                           "There wasn't a dry eye in the tent, and only partly because of the wine.");
        }

        if (lucita)
        {
            paragraphs.Add("But the real guest of honor was Duchess and Councilor Lucita Azaro, whose gorgeous gown was turning heads " +
                           "even before His Majesty invited her onstage to pour the first glass together. Who was minding the war while our " +
                           "War Councilor was minding the wine? Don't write in, dear readers. It was a rhetorical question.");
        }

        paragraphs.Add("A fabulous night of dancing, hobnobbing and -- of course -- sampling the wares ensued, though unfortunately we " +
                       "members of the press were kicked out of the venue before anything juicy could happen. You'll just have to use " +
                       "your imagination.");
        return P(paragraphs.ToArray());
    }
}
