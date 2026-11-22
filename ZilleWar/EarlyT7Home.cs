using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.EarlyT7;

namespace ZilleWar;

/// <summary>Turn 7 at home: the new security councilor, Rico's fate, Iza University, Hugo and Manus, the family dinner, Pabel.</summary>
internal static class EarlyT7Home
{
    private const string Hugo = "Hugo Toras";
    private const string Rumburg = LateT9.LuaRumburgHelped;

    public static void Register()
    {
        RegisterCouncilor();
        RegisterRico();
        RegisterIza();
        RegisterHugoManus();
        RegisterDinner();
        RegisterPabel();
    }

    public static void OnStep()
    {
    }

    // ------------------------------------------------------------------ c396 New Security Councilor Talk

    private static void RegisterCouncilor()
    {
        var t = new Talk("Rizia/Turn 7/New Security Councilor Talk", "ZilleWar.E7.Councilor.");
        // "Lord willing, we will not be entering another war." / "But then again, we are not at war."
        t.Variant("NoHead9", 9, new[] { 8 }, Act, Hugo,
            "\"We have avoided a second Pales conflict. But the war in Zille is still being fought, and it has been without a head since the Duchess's arrest.\"",
            new[] { "#13" });
        t.Variant("Silent9", 9, new[] { 8 }, Over, Hugo,
            "\"We have avoided a second Pales conflict, and the guns in Zille have fallen silent. Lord willing, we will not be entering another war.\"",
            new[] { "#13" });
        t.Variant("Titus108", 108, new[] { 101 }, Act, Hugo,
            "\"It's true that he has no experience with military operations. In the middle of the Zille campaign, that is not a small thing.\"",
            new[] { "#111" });
        // "But then again, we are not at war": the Zille war has only just ended.
        t.Variant("Titus108Over", 108, new[] { 101 }, Over, Hugo,
            "\"It's true that he has no experience with military operations. But the fighting in Zille is over, and Lord willing, it will stay that way.\"",
            new[] { "#111" });
        t.Register();
    }

    // ------------------------------------------------------------------ c398 Hugo confronts the King about Rico

    private static void RegisterRico()
    {
        var t = new Talk("Rizia/Turn 7/P_HugoConfronts", "ZilleWar.E7.HugoRico.");
        t.Variant("Plea133", 133, new[] { 132 }, Declared, Hugo,
            "\"Let him live, even if in exile or imprisonment. And whatever Smolak demands, do not hand my son to the Wehzeks.\"",
            new[] { "#135" });
        t.Variant("Price138", 138, new[] { 134 }, $"{Declared} and {WarFlags.LNot(WarFlags.Liberated)}", Romus,
            "\"I cannot promise that, Uncle. Smolak wants his head as the price of any peace.\"", new[] { "#140" });
        // Liberated: Wehlen conceded Zille, there is no peace left to buy with Rico.
        t.Variant("Price138Lib", 138, new[] { 134 }, Lib, Romus,
            "\"I cannot promise that, Uncle. Smolak has lost Zille, and he still wants Rico's head for the bombing. One day I may need something from Wehlen.\"",
            new[] { "#140" });
        t.Variant("Court139", 139, new[] { 134 }, Declared, Romus,
            "\"No Wehzek court will ever touch him. But a Rizian one will, and I will not stand in its way.\"", new[] { "#140" });
        t.Register();
    }

    // ------------------------------------------------------------------ c399 Iza University

    private static void RegisterIza()
    {
        var t = new Talk("Rizia/Turn 7/A_IzaUniversity", "ZilleWar.E7.Iza.");
        // Rico cannot be extradited to a country Rizia is at war with.
        t.Variant("Trial545", 545, new[] { 530, 570, 617 }, Declared, Romus,
            "\"He will stand trial before the Royal Tribunal for the blood he spilled in Zille.\"", new[] { "#539" });
        t.Variant("NoWar617", 617, new[] { 554 }, Declared, Romus,
            "\"I will tell you the truth. Rico Toras is a criminal and a conspirator. Were it not for him, Zille would have come home without a war.\"",
            new[] { "Trial545", "#543", "#537", "#555" });
        // The students protest the Zille war.
        t.Line("Shouts", Narrator, "I heard shouts of \"Rights for all!\" and \"Bring them home from Zille!\"", new[] { "#134" },
            condition: ZilleOnly, hooks: From(175));
        t.Logic("Blood", new[] { "#740" }, condition: ZilleOnly, hooks: From(741));
        t.Line("Chant", Narrator,
            "Chanting \"Bring them home from Zille!\", they began filing out of the auditorium. The students and teachers in the other rows followed.",
            new[] { "#768", "#745", "#751" }, condition: ZilleOnly, hooks: From(754));
        t.Variant("Enemy705", 705, new[] { 708 }, Act, Romus,
            "\"You turn your blame on myself and my cousin while ignoring the real enemy. The Wehzek army sitting in Zille.\"", new[] { "#710" });
        t.Variant("Enemy728", 728, new[] { 726 }, Act, Romus,
            "\"I urge you to put aside your grievances with myself and my cousin and focus on the real enemy: the Wehzek army sitting in Zille.\"",
            new[] { "#710" });
        t.Register();
    }

    // ------------------------------------------------------------------ c395 Hugo and Manus

    private static void RegisterHugoManus()
    {
        var t = new Talk("Rizia/Turn 7/A_HugoManusPrivateTalk", "ZilleWar.E7.HugoManus.");
        Widen(t, "Attention13", 14, 13, ZilleOnly);       // "Of course the war deserves your full attention."
        t.Variant("Both13", 13, new[] { 15 }, Two, Hugo,
            "\"Of course the wars deserve your full attention. Both of them. I will make this brief.\"", new[] { "#26" });
        Widen(t, "Battlefield123", 120, 123, ZilleOnly);  // "they will not support you on the battlefield"
        Widen(t, "Harping475", 476, 475, ZilleOnly);
        Widen(t, "Harping487", 488, 487, ZilleOnly);
        Widen(t, "Dinner700", 701, 700, ZilleOnly);       // "once Rizia wins the war"
        Widen(t, "Execute755", 756, 755, ZilleOnly);      // "Especially while we're at war."
        Widen(t, "TitusWar1075", 1076, 1075, ZilleOnly);  // "The war has been weighing on my mind"
        Widen(t, "TitusWar990", 991, 990, $"{ZilleOnly} and {Num("RiziaDLC.Army_Unit_GoldenGuard_Total")} > 0");
        // Rico's Su Omina "sowing chaos in Zille" and Titus' "the fate of Zille", once the war has settled it.
        t.Variant("Chaos722Lib", 722, new[] { 720 }, Lib, "Rico Toras",
            "\"They're already hard at work in Zille, rooting out Smolak's collaborators. What else would you ask?\"",
            new[] { "#723", "#724", "#725" });
        t.Variant("Chaos722Part", 722, new[] { 720 }, Part, "Rico Toras",
            "\"They're already hard at work behind the line in Zille. What else would you ask?\"", new[] { "#723", "#724", "#725" });
        t.Variant("Fate1079Lib", 1079, new[] { 1078 }, Lib, "Titus Gordion",
            "\"Holding Zille has been weighing on my mind, as I'm sure it has yours. Smolak will not forget it.\"",
            new[] { "#1080", "#1085", "#1082", "#1370" });
        t.Variant("Fate1079Part", 1079, new[] { 1078 }, Part, "Titus Gordion",
            "\"The line in Zille has been weighing on my mind, as I'm sure it has yours.\"", new[] { "#1080", "#1085", "#1082", "#1370" });
        t.Register();
    }

    // ------------------------------------------------------------------ c394 the Toras family dinner

    private static void RegisterDinner()
    {
        var t = new Talk("Rizia/Turn 7/P_TorasDinner", "ZilleWar.E7.Dinner.");
        Widen(t, "Demands13", 14, 13, ZilleOnly);                     // "Even among the myriad demands of war..."
        Widen(t, "Rumburg112", 113, 112, $"{ZilleOnly} and {Rumburg}"); // "They're giving us sorely needed aid."
        Widen(t, "AtWar327", 324, 327, ZilleOnly);                    // "We are at war. Every action we take as the Royal Family..."
        t.Variant("TwoWars327", 327, new[] { 325 }, Two, Romus,
            "\"We are at war on two borders. Every action we take as the Royal Family, no matter how personal, must be done with the kingdom's stability in mind.\"",
            new[] { "#328" });
        Widen(t, "Peace368", 369, 368, ZilleOnly);                    // "once peace has been restored"
        Widen(t, "Sazon765", 766, 765, ZilleOnly);                    // "House Sazon's support in the war"
        t.Register();
    }

    // ------------------------------------------------------------------ c392 Pabel

    private static void RegisterPabel()
    {
        var t = new Talk("Rizia/Turn 7/P_PabelPrivateTalk", "ZilleWar.E7.Pabel.");
        Widen(t, "Middle220", 217, 220, ZilleOnly);   // "we're in the middle of a war"
        Widen(t, "Priority296", 297, 296, ZilleOnly); // "I know the war is your highest priority right now."
        Widen(t, "Middle501", 495, 501, ZilleOnly);
        Widen(t, "Image602", 603, 602, ZilleOnly);    // "It could greatly improve our image in the war."
        t.Register();
    }
}
