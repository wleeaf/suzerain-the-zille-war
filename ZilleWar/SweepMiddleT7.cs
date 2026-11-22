using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Sweep of the Turn 7 personal scenes, read line by line after EarlyT7Home's first pass:
/// Pabel's morning (c392), the family dinner (c394), Hugo and Manus (c395), Hugo's plea for Rico (c398), Iza University (c399).
/// Only what EarlyT7Home does not already cover: lines that are false in a war game, Pales-only war lines whose text also fits
/// Zille, and a few natural places where the war should be mentioned.
/// </summary>
internal static class SweepMiddleT7
{
    private const string Pabel = "Pabel Adria";
    private const string Estela = "Estela Toras";
    private const string Hugo = "Hugo Toras";
    private const string Rico = "Rico Toras";
    private const string R = Talk.Romus;

    private static string ZilleOnly => EarlyT7.ZilleOnly;
    private static string NoPales => EarlyT7.NoPales;

    public static void Register()
    {
        RegisterPabel();
        RegisterDinner();
        RegisterHugoManus();
        RegisterHugoPlea();
        RegisterIza();
    }

    // ------------------------------------------------------------------ c392 Pabel's morning visit

    private static void RegisterPabel()
    {
        const string conv = "Rizia/Turn 7/P_PabelPrivateTalk";
        var t = new Talk(conv, "ZilleWar.SM.T7.Pabel.");

        // "I won't sleep until we have Zille back." Not once it is back.
        // Sleeping like a baby goes to Pabel's "I'm glad to hear it", not to his remedy for a sleepless king.
        Swap(t, conv, "Sleep365", 365, new[] { 363 }, R, new S(Lib: "\"Like a baby. Zille is ours again.\""), new[] { "#368" });
        Swap(t, conv, "Sleep365b", 365, new[] { 363 }, R, new S(Part: "\"I won't sleep until the rest of Zille is ours.\""), new[] { "#584" });

        // The morning paper: what the household reads first.
        foreach ((string sfx, string state, string text) in new[]
                 {
                     ("On", Ongoing, "\"The front page is Zille again, I'm afraid. Cook reads the casualty lists before anything else now. Her nephew is with the infantry up there.\""),
                     ("Lib", Lib, "\"The front page is still Zille, Your Majesty. Cook cried into the porridge this morning. Her nephew came through the whole campaign without a scratch.\""),
                     ("Part", Part, "\"The front page is the armistice line again. Cook's nephew writes that the two sides wave at each other across it now. She doesn't know whether to laugh.\""),
                     ("Lost", Lost, "\"I would read it after breakfast, if I were you, Your Majesty. It's Zille again.\""),
                 })
        {
            // With the Pales war on, the front page is Pales: vanilla's line stands.
            t.Line("Paper" + sfx, Pabel, text, new[] { "#370" }, condition: And(state, NoPales), hooks: Talk.From(366));
        }

        // Derdian mercenaries "for use in Zille", once Zille is already ours.
        Swap(t, conv, "Mercs591", 591, new[] { 396 }, R,
            new S(Lib: "\"They are going well, thank you. I'm hoping to contract their mercenaries to help us hold Zille.\""), new[] { "#403" });
        Swap(t, conv, "Mercs644", 644, new[] { 637 }, R,
            new S(Lib: "\"As well as can be expected. I'm hoping to contract their mercenaries to help us hold Zille.\""), new[] { "#642" });
        t.Register();
    }

    // ------------------------------------------------------------------ c394 the Toras family dinner

    private static void RegisterDinner()
    {
        const string conv = "Rizia/Turn 7/P_TorasDinner";
        var t = new Talk(conv, "ZilleWar.SM.T7.Dinner.");

        // Pabel serves a Cantuavo bottled "days prior to the uprising". The Uprising is why Wehlen got Zille, and Estela is from Zille.
        t.Choice("Wine1926", "\"1926. The year Wehlen saved us from ourselves, and Zille paid the bill.\"", new[] { "WineOn", "WineLib", "WinePart", "WineLost" },
            condition: War, hooks: Talk.From(64));
        t.Line("WineOn", Estela,
            "\"And the bill keeps coming, doesn't it? Drink it slowly, dears. When this war is over, I want there to be something left to toast with.\"",
            new[] { "#94" }, condition: Ongoing);
        t.Line("WineLib", Estela,
            "\"Paid in full now, darling. Years late, but in full. Pabel, put a bottle aside for me. I mean to drink it in Zille.\"",
            new[] { "#94" }, condition: Lib);
        t.Line("WinePart", Estela,
            "\"Half paid, then. Zille's harbor is ours, and Isor and Nanta are still his. Twenty-five years, and my homeland still can't be in one piece.\"",
            new[] { "#94" }, condition: Part);
        t.Line("WineLost", Estela,
            "\"And now we've paid it twice. Pour me a large one, Pabel. I would rather not think about my homeland tonight.\"",
            new[] { "#94" }, condition: Lost);
        t.Register();
    }

    // ------------------------------------------------------------------ c395 Hugo and Manus

    private static void RegisterHugoManus()
    {
        const string conv = "Rizia/Turn 7/A_HugoManusPrivateTalk";
        var t = new Talk(conv, "ZilleWar.SM.T7.HugoManus.");

        // EarlyT7Home widens Hugo's "once Rizia wins the war" dinner invitation to the Zille war; the King's answer named Pales.
        t.Variant("Dinner705", 705, new[] { 700 }, ZilleOnly, R, "\"Dinner will be on me. In Zille.\"", new[] { "#503" });

        // "What does the favor of House Toras matter when we're at war?"
        EarlyT7.Widen(t, "Favor328", 322, 325, ZilleOnly);
        t.Register();
    }

    // ------------------------------------------------------------------ c398 Hugo's plea for Rico (the bombing branch)

    private static void RegisterHugoPlea()
    {
        const string conv = "Rizia/Turn 7/P_HugoConfronts";
        var t = new Talk(conv, "ZilleWar.SM.T7.HugoPlea.");
        t.Variant("Stain108", 108, new[] { 105 }, War, Hugo,
            "\"The actions of my son... I cannot fathom what led Rico to such darkness. The bombing, and now a war over Zille... It's a stain upon our House.\"",
            new[] { "#173" });
        t.Variant("Mercy127", 127, new[] { 126 }, War, R,
            "\"He showed no mercy when he planted that bomb. It cost lives, it cost us Zille's peaceful return, and in the end it cost us a war. I'm sorry.\"",
            new[] { "#132" });
        t.Register();
    }

    // ------------------------------------------------------------------ c399 Iza University

    private static void RegisterIza()
    {
        const string conv = "Rizia/Turn 7/A_IzaUniversity";
        var t = new Talk(conv, "ZilleWar.SM.T7.Iza.");

        // Rico's speech and the King's: generic war lines also fit the Zille war.
        EarlyT7.Widen(t, "Youths298", 299, 298, ZilleOnly);     // "the harsh reality of war"
        EarlyT7.Widen(t, "Battlefield378", 416, 378, ZilleOnly); // "on the battlefield or in the lecture hall"
        EarlyT7.Widen(t, "Treason579", 567, 579, ZilleOnly);    // "In times of war, displaying insubordination..."
        EarlyT7.Widen(t, "AtWar718", 715, 718, ZilleOnly);      // "But we are a kingdom at war."
        EarlyT7.Widen(t, "Structures1120", 1117, 1120, ZilleOnly);

        // The Pales-only lines that name Pales, in their Zille form.
        t.Line("Rico376", Rico, "\"If we are to win the war in Zille, we cannot allow the minds of our youths to become clouded by dissent and division.\"",
            new[] { "#406" }, condition: ZilleOnly, hooks: Talk.From(373));
        t.Choice("Rally818", "\"As your King, I am relying on your support to bring Zille home.\"", new[] { "#821", "#825", "#828", "#830" },
            condition: ZilleOnly, hooks: Talk.From(814));
        t.Choice("Iza1032", "\"Once Zille is ours again, I will turn my attention to the restructuring of Iza's government.\"", new[] { "#1052" },
            condition: ZilleOnly, hooks: Talk.From(1033));
        t.Register();
    }
}
