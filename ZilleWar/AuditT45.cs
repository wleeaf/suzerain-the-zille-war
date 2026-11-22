using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Audit of the vanilla story of Turn 4 (after the declaration at the AN) and Turn 5 against the Zille war (see <see cref="Audit"/>).
/// Turn 5 plays after Phase I of Operation Northern Crown (Zille city taken or not) and, from the War Council on, after
/// Phase II (Auremina), a hold or a ceasefire. The front conditions below tell those states apart in Lua, mid-step, from the
/// operation's own variables, so the scenes of the turn speak of the front as it really stands.
/// Edits here fill the gaps left by EarlyT4/EarlyT5 and the SweepEarlyT5* files (which use these conditions too).
/// </summary>
internal static class AuditT45
{
    // ------------------------------------------------------------------ the front, in Lua

    private static string V(string v) => $"Variable[\"{v}\"]";
    private static string Num(string v) => $"({V(v)} or 0)";

    /// <summary>Zille city / Auremina in Rizian hands right now (set the moment a phase ends).</summary>
    public static readonly string City = WarFlags.L(ZilleOperation.CapturedVar("Zille"));
    public static readonly string Auremina = WarFlags.L(ZilleOperation.CapturedVar("Auremina"));

    /// <summary>The war is still fought (and the King has not just sought a ceasefire at this turn's council).</summary>
    public static readonly string Fighting = $"{WarFlags.LActive} and {WarFlags.LNot(WarFlags.Ceasefire)}";

    /// <summary>The war is over (any outcome), or the King sought the ceasefire earlier in this step.</summary>
    public static readonly string Stopped = $"{WarFlags.LDeclared} and ({WarFlags.LNot(WarFlags.Active)} or {WarFlags.L(WarFlags.Ceasefire)})";

    /// <summary>Phase II has been fought (two phases on the books).</summary>
    public static readonly string PhaseTwoFought = $"{Num(ZilleCampaign.VarOpsDone)} >= 2";
    private static readonly string PhaseTwoWon = WarFlags.L(ZilleCampaign.ResultVar(2));

    /// <summary>At the Turn 5 council the King ordered the army to hold: no Phase II this turn.</summary>
    public static readonly string HeldThisTurn = $"{Num(WarCouncil.VarHeldTurn)} == 5";

    // Turn 5, while the war is fought: one of these holds at any time.
    public static readonly string FrontAuremina = $"{Fighting} and {PhaseTwoFought} and {Auremina} and {City}";
    public static readonly string FrontCitySecond = $"{Fighting} and {PhaseTwoFought} and {PhaseTwoWon} and {City} and not ({Auremina})";
    public static readonly string FrontHillsHeld = $"{Fighting} and {PhaseTwoFought} and not ({PhaseTwoWon}) and {City}";
    public static readonly string FrontCityHeldTwice = $"{Fighting} and {PhaseTwoFought} and not ({City})";
    public static readonly string FrontHolding = $"{Fighting} and not ({PhaseTwoFought}) and {HeldThisTurn}";
    public static readonly string FrontBeforeCity = $"{Fighting} and not ({PhaseTwoFought}) and not ({HeldThisTurn}) and {City}";
    public static readonly string FrontBeforeNoCity = $"{Fighting} and not ({PhaseTwoFought}) and not ({HeldThisTurn}) and not ({City})";

    private static readonly List<string> Latches = new();

    public static void Register()
    {
        RegisterDrinkingGame();
        RegisterHegelCall();
        RegisterMorella();
        RegisterFeedback();
        RegisterSecurity();
        RegisterPress();
    }

    public static void OnStep()
    {
    }

    /// <summary>A yes/no fact about the war, fixed the first time a rewritten report reads it (the archive keeps that version).</summary>
    private static bool Latch(string key, Func<bool> now)
    {
        string v = "ZilleWar.A45_L_" + key;
        string s = Variables.GetString(v);
        if (string.IsNullOrEmpty(s))
        {
            s = now() ? "y" : "n";
            Variables.Set(v, s);
        }

        return s == "y";
    }

    private static void RegisterLatch(string key)
    {
        string v = "ZilleWar.A45_L_" + key;
        if (!Latches.Contains(v))
        {
            Latches.Add(v);
            Variables.Register(v);
        }
    }

    private static bool CityNow() => Variables.GetBool(ZilleOperation.CapturedVar("Zille"));

    // ------------------------------------------------------------------ c318 the drinking game (Turn 4, the night after Konstantium)

    private static void RegisterDrinkingGame()
    {
        var t = new Talk("Rizia/Turn 4/Drinking Game", "ZilleWar.A45.Drinking.");
        // [668] after turning Lucita away: the same "slept more soundly than in months" the other endings already qualify.
        t.Variant("Sleep668", 668, new[] { 670, 765, 826, 827 }, WarFlags.LDeclared, Narrator,
            "I turned the key and lay awake a while, thinking of the trains. When I finally slept, it was badly.", new[] { "#2" });
        t.Register();
    }

    // ------------------------------------------------------------------ c322 Hegel's call in the night

    private static void RegisterHegelCall()
    {
        var t = new Talk("Rizia/Turn 5/FPnT_MITZHegelCall", "ZilleWar.A45.Hegel.");
        // [21] "...He informed me that Chancellor Emmerich Hegel was on the line." A king at war, woken at night.
        t.Line("Front", Narrator,
            "For a moment, before he said the name, I had been certain it was news from Zille.", new[] { "#153" },
            condition: Fighting, hooks: From(21));
        t.Register();
    }

    // ------------------------------------------------------------------ c323 Cavette

    private static void RegisterMorella()
    {
        var t = new Talk("Rizia/Turn 5/FPnT_MorellaNegotiations", "ZilleWar.A45.Morella.");

        // [127]/[155] "Neither of us want to resort to military measures." Said to a king who has just started a war.
        const string measures = "\"You have shown that you are not shy of military measures, Your Majesty. Morella prefers other kinds. So I " +
                                "will say this.\"";
        t.Variant("Measures127", 127, new[] { 126 }, WarFlags.LDeclared, "Alma Saltana", measures, new[] { "#128" });
        t.Variant("Measures155", 155, new[] { 154 }, WarFlags.LDeclared, "Alma Saltana", measures, new[] { "#156" });

        // [267] "...speak out against Wehlen's decision." The decision has become a war.
        t.Variant("Support", 267, new[] { 235, 249 }, WarFlags.LDeclared, Romus,
            "\"Only your diplomatic support. We need as many countries as possible to say in Konstantium that Wehlen chose this war " +
            "when it refused to give Zille back.\"", new[] { "#274" });
        t.Register();
    }

    // ------------------------------------------------------------------ c327 the debrief, after a ceasefire

    private static void RegisterFeedback()
    {
        var t = new Talk("Rizia/Turn 5/EnT_MorellaNegotiationFeedback", "ZilleWar.A45.Feedback.");
        const string esq = "Laurento Esquibel";
        // "Since we failed to reach an agreement with Wehlen..." once the guns have stopped (EarlyT5 covers the war itself).
        string stopped = $"{Stopped} and {WarFlags.LNot(WarFlags.Liberated)}";
        string liberated = $"{Stopped} and {WarFlags.L(WarFlags.Liberated)}";
        const string internalStopped = "Now that the guns are silent, pressure from inside Zille is the only lever we have left.";
        const string externalStopped = "With the guns silent and nothing signed, ";
        t.Variant("Internal", 376, new[] { 377 }, stopped, esq,
            "\"" + internalStopped + " Smolak will not give up another village at a conference table.\"", new[] { "#387" });
        t.Variant("External", 384, new[] { 383 }, stopped, esq,
            "\"" + externalStopped + "we need every friend we can find in Konstantium. Wehlen will tell the Alliance that the line of " +
            "contact is the border now.\"", new[] { "#387" });
        t.Variant("Crates", 629, new[] { 627 }, stopped, esq,
            "\"A wise request, Your Majesty. Smolak will not give up another village at a conference table, so the people inside the " +
            "line are the only ones who can move him.\"", new[] { "#636" });
        t.Variant("Neighbors", 635, new[] { 633 }, stopped, esq,
            "\"A wise request, Your Majesty. Wehlen's neighbors have been waiting for a reason to speak, and the armistice line " +
            "gives them one.\"", new[] { "#508" });

        // Zille liberated before the turn was out: the requests are about keeping it.
        const string internalFree = "Zille is ours now, Your Majesty. Whatever Morella ships across the Gulf will help the people " +
                                    "there rebuild, not resist.";
        const string externalFree = "With Zille ours, we need friends in Konstantium to recognize it. Wehlen is already calling it " +
                                    "an annexation.";
        t.Variant("InternalFree", 376, new[] { 377 }, liberated, esq, "\"" + internalFree + "\"", new[] { "#387" });
        t.Variant("ExternalFree", 384, new[] { 383 }, liberated, esq, "\"" + externalFree + "\"", new[] { "#387" });
        t.Variant("CratesFree", 629, new[] { 627 }, liberated, esq,
            "\"A wise request, Your Majesty. Whatever Morella ships across the Gulf will help the people there rebuild, not resist.\"",
            new[] { "#636" });
        t.Variant("PledgeStopped", 507, new[] { 505 }, stopped, esq,
            "\"Ms. Saltana has pledged to speak in our favor when the AN next convenes, if the guns stay silent until then.\"",
            new[] { "#630" });
        t.Variant("PledgeFree", 507, new[] { 505 }, liberated, esq,
            "\"Ms. Saltana has pledged to speak in our favor when the AN next convenes, should Wehlen ask the Alliance to undo what " +
            "our army has done.\"", new[] { "#630" });
        t.Variant("NeighborsFree", 635, new[] { 633 }, liberated, esq,
            "\"A wise request, Your Majesty. Ms. Saltana's voice will carry further now that Zille is ours, and Wehlen is already " +
            "calling it an annexation.\"", new[] { "#508" });
        t.Register();
    }

    // ------------------------------------------------------------------ c324 the late-night security council

    private static void RegisterSecurity()
    {
        var t = new Talk("Rizia/Turn 5/SnO_InternalSecurity", "ZilleWar.A45.Security.");
        // [97] -> [183] Hugo: "Perhaps we should have agreed on Mr. Smolak's Bludish ban after all." (in wartime [97] is replaced
        // by EarlyT5's boats; this is the ceasefire path).
        t.Variant("Ban", 183, new[] { 97 }, WarFlags.LDeclared, "Hugo Toras",
            "\"Taking advantage of our open borders, while every patrol boat we own is watching the Wehzek coast.\"",
            new[] { "#178", "#176", "#177", "#179" });
        t.Register();
    }

    // ------------------------------------------------------------------ reports and the press

    private static void RegisterPress()
    {
        // Turn 4 (after the AN): the rail crisis while the trains carry reservists north.
        AppendNews("Turn04_V_Decision_PublicTransportationWeak", _ =>
            "Not that the trains have much room for passengers these days. Since the declaration of war, military trains bound for " +
            "the north have priority on every line, and commuters have learned to wait on the platform while the reservists go first.",
            s => s == StActive);
        AppendReport("Turn04_Qalus_Decision_PublicTransportationWeak", _ =>
            "Military traffic bound for the northern front has been diverted onto slower lines while the switch is repaired, adding " +
            "the better part of a day to the journey of reservists and munitions.", s => s == StActive);

        // Turn 5: Morellan supplies for the resistance, landed at a Zille harbor that may already be ours.
        RegisterLatch("ATASupplies");
        RegisterLatch("SOSupplies");
        Report("Turn05_Zille_ATASuppliesReceived", (_, o) => !Latch("ATASupplies", CityNow) ? o : P(
                "To aid the Ariana Toras Association behind the Wehzek lines, we have organized a shipment of short-wave radios, printing " +
                "supplies and medical kits. With Zille harbor in our hands, the crates came in openly from the Morellan port of M'orn " +
                "and are being carried north to the Association's people in the towns Wehlen still holds."),
            when: s => s == StActive);
        Report("Turn05_Zille_SOSuppliesReceived", (_, o) => !Latch("SOSupplies", CityNow) ? o : P(
                "Morella has begun smuggling firearms and other supplies to the Su Omina cells behind the Wehzek lines. With Zille " +
                "harbor in our hands, the first shipment was unloaded there under a Morellan flag and taken north by night to the cells " +
                "still operating in the towns Wehlen holds."),
            when: s => s == StActive);

        // The Morellan aircraft, bought in wartime.
        AppendReport("Turn05_Terador_AirplaneDeal", _ =>
            "With the army fighting in Zille, the Air Staff has asked for the first of them to be sent north as soon as their crews " +
            "have been trained.", s => s == StActive);
    }
}
