using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Turn 9, Turn 10, Turn 11 and ending lines that the Zille war makes false, or where a character obviously would mention
/// it, on top of what LateT9/LateT10 already change:
/// - Turn 9: Chancellor Hegel's "boot on the necks" speech names the war of conquest in Zille (and both wars when the
///   Pales war runs too); at the MITZ board the King no longer says "there's a war going on" after it ended.
/// - Turn 10: Lucita's announcement remembers the war room, Taddeus toasts the troops who went home to Zille, the petition
///   hearing (Taddeus's claim, Titus's reproach), Lucita's new headquarters after a war that cost or lost Zille, House
///   Azaro's spare unit, Daria on the Zille handover, the reform mural, the Foreign Affairs folder (Rumburg's favor in
///   Zille, the war damage in a liberated Zille, the military reputation), and the Pales victory parade.
/// - Endings: the stratocracy jubilee, the dying memory of the Zille beach, a guard's news in prison, and the Turn 11
///   speech when Zille was not won.
/// </summary>
internal static class SweepLateTalk
{
    private const string Hegel = "Rizia/Turn 9/Hegel Side Convo";
    private const string Mitz = "Rizia/Turn 9/FPnT_MITZ_BoardMeeting";
    private const string Announcement = "Rizia/Turn 10/RelationshipAnnouncement";
    private const string Gather = "Rizia/Turn 10/HOUSES GATHER/HOUSES GATHER Conversation";
    private const string Petitions = "Rizia/Turn 10/A_HearingOfPetitions";
    private const string Hod = "Rizia/Turn 10/HODShutdown";
    private const string Azaro = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE AZARO CONVO";
    private const string Reform = "Rizia/Turn 10/REFORM TALKS/REFORM TALKS Conversation";
    private const string PrimeMinister = "Rizia/Turn 10/TalkWithPrimeMinister";
    private const string ForeignAffairs = "Rizia/Turn 10/Foreign_Affairs/Foreign_Affairs Conversation";
    private const string Parade = "Rizia/Turn 10/War_Pales_VictoryParade";
    private const string Continued = "Rizia/Continued Reign";
    private const string Death = "Rizia/Immediate Death";
    private const string Prison = "Rizia/Prison";
    private const string Speech = "Rizia/Turn 11/Ending_Speech";

    private const string Narr = Talk.Narrator;
    private const string R = Talk.Romus;

    private const string NoPalesWar = "Variable[\"RiziaDLC.Pales_War\"] ~= true";
    private const string NoPalesWarEver = "Variable[\"RiziaDLC.War_Happened\"] ~= true";
    private const string PalesNotWon = "Variable[\"RiziaDLC.War_Pales_Won\"] ~= true";
    private const string PalesNotLost = "not (Variable[\"RiziaDLC.Pales_War\"] == true and Variable[\"RiziaDLC.War_Pales_Won\"] ~= true)";

    private const string RumburgHelped = LateT9.LuaRumburgHelped;

    /// <summary>A divided end with every Zille city Rizian (only after the treaty's Auremina term).</summary>
    private static readonly string AllHeld = And(Part, Front.LWehzekExactly(Array.Empty<string>()));
    private static readonly string BorderKind = $"Variable[\"{ZilleTreaty.VarKind}\"] == \"border\"";

    public static void Register()
    {
        RegisterTurn9();
        RegisterTurn10Court();
        RegisterTurn10Desk();
        RegisterEndings();
    }

    // ------------------------------------------------------------------ Turn 9

    private static void RegisterTurn9()
    {
        // Hegel's verdict: vanilla 65 speaks of "violent actions in Zille" (the referendum), 64 of the Pales war only.
        var h = new Talk(Hegel, "ZilleWar.SL.T9.Hegel.");
        Swap(h, Hegel, "Boot65", 65, new[] { 63 }, "Emmerich Hegel (R)", new S(
            Ongoing: "\"And with your war of conquest in Zille, Rizia is wearing the biggest boot of all.\"",
            Lib: "\"And with your war of conquest in Zille, Rizia is wearing the biggest boot of all.\"",
            Lost: "\"And with your failed war of conquest in Zille, Rizia has shown everyone the boot it would like to wear.\""),
            new[] { "#86", "#81", "#82", "#94" });
        // A divided end holds one to four of the Zille cities (all four after the treaty's Auremina term): no fraction.
        Swap(h, Hegel, "Boot65Some", 65, new[] { 63 }, "Emmerich Hegel (R)", new S(
            Part: "\"And with your war of conquest in Zille, Rizia is wearing the biggest boot of all. Part of a province is still a conquest.\""),
            new[] { "#86", "#81", "#82", "#94" }, when: $"not ({AllHeld})");
        Swap(h, Hegel, "Boot65All", 65, new[] { 63 }, "Emmerich Hegel (R)", new S(
            Part: "\"And with your war of conquest in Zille, Rizia is wearing the biggest boot of all. A province taken by war and kept by treaty is still a conquest.\""),
            new[] { "#86", "#81", "#82", "#94" }, when: AllHeld);
        Swap(h, Hegel, "Boot64", 64, new[] { 62 }, "Emmerich Hegel (R)",
            new S(
                Ongoing: "\"And with two wars of conquest, one in Pales and one in Zille, Rizia is wearing the biggest boot of all.\"",
                Lib: "\"And with two wars of conquest, one in Pales and one in Zille, Rizia is wearing the biggest boot of all.\"",
                Part: "\"And with two wars of conquest, one in Pales and one in Zille, Rizia is wearing the biggest boot of all.\"",
                Lost: "\"And with two wars, one in Pales and one in Zille that you lost, Rizia is still wearing the biggest boot of all.\""),
            new[] { "#86", "#78", "#81", "#82", "#94" });

        // The strait was bought with a fifty-year lease of Caleqabiz (SupplyAlvarez): there is no island left to offer the CSP.
        // Vanilla only knows its own ATO lease; every road to "Your island, Caleqabiz" (108, 141, 145) ends as its refusals do.
        string leased = $"{War} and Variable[\"{SupplyWar.VarLespiaLease}\"] == true";
        foreach (int gate in new[] { 108, 141, 145 })
        {
            Vanilla.Require(Hegel, gate, $"not ({leased})");
        }

        h.Line("Leased106", "Emmerich Hegel (R)", "\"You expect me to believe that, after you leased your island to Lespia for fifty years?\"",
            new[] { "#110" }, condition: And(leased, "Variable[\"RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO\"] ~= true"), hooks: Talk.From(106));
        h.Logic("Leased140", new[] { "#76" }, condition: And(leased, "Variable[\"RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO\"] ~= true"), hooks: Talk.From(140));
        h.Line("Leased144", "Emmerich Hegel (R)", "\"You leased your own island to Lespia for fifty years. No offer under the sun would convince me.\"",
            new[] { "#129" }, condition: And(leased, "Variable[\"RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO\"] ~= true"), hooks: Talk.From(144));
        h.Register();

        // At the MITZ board, without a Pales war, vanilla has the King say "There's a war going on." That holds while Zille is
        // fought; once it is over he has other things on his mind.
        var m = new Talk(Mitz, "ZilleWar.SL.T9.Mitz.");
        Swap(m, Mitz, "Waste", 285, new[] { 284 }, R, new S(
            Lib: "\"I'm ready for Morella to stop wasting my time. I have a province to rebuild.\"",
            Part: "\"I'm ready for Morella to stop wasting my time. I have a garrison in Zille to answer for.\"",
            Lost: "\"I'm ready for Morella to stop wasting my time. I have enough to answer for at home.\""),
            new[] { "#290" });
        m.Register();
    }

    // ------------------------------------------------------------------ Turn 10, the court and the houses

    private static void RegisterTurn10Court()
    {
        // Lucita's news: vanilla only remembers the Pales war or a referendum fight in Zille. (Only while Zille is fought, which
        // can no longer happen in Turn 10: the war ended years before her pregnancy.)
        var a = new Talk(Announcement, "ZilleWar.SL.T10.Announce.");
        a.Line("ZilleBlur", "Lucita Azaro", "\"Time's been a blur since the war in Zille began. I thought I was throwing up from nerves.\"",
            new[] { "#37" }, condition: And(Ongoing, NoPalesWar), hooks: Talk.From(46));
        a.Register();

        // Taddeus's birthday toast to the Queen Mother, who was born in Zille (vanilla 510 "men and women in uniform"). Vanilla
        // reaches 510 only in a Pales war (Rumburg asked for a ceasefire), and it leads on to Taddeus "out of Pales" (41) and
        // Carlos "handling operations in Pales" (828): the toasts replace it only there. Without a Pales war Carlos gives the
        // toast (vanilla 514, "Camp Domus").
        var g = new Talk(Gather, "ZilleWar.SL.T10.Gather.");
        string noPalesVictory = "Variable[\"RiziaDLC.Pales_War\"] == true and Variable[\"RiziaDLC.War_Objective_Captured_Pales\"] ~= true and Variable[\"RiziaDLC.War_WehlenJoined\"] ~= true";
        g.Line("ToastOn", "Taddeus Azaro", "\"Happy birthday, Your Royal Highness. Your strength is an inspiration to our men and women at the Zille front.\"",
            new[] { "#41", "#39", "#533" }, condition: And(Ongoing, noPalesVictory), hooks: Talk.From(506));
        g.Line("ToastLib", "Taddeus Azaro", "\"Happy birthday, Your Royal Highness. Your strength carried our troops all the way home to Zille.\"",
            new[] { "#41", "#39", "#533" }, condition: And(Lib, noPalesVictory), hooks: Talk.From(506));
        g.Line("ToastPart", "Taddeus Azaro", "\"Happy birthday, Your Royal Highness. Your strength is an inspiration to our men and women serving in Zille.\"",
            new[] { "#41", "#39", "#533" }, condition: And(Part, noPalesVictory), hooks: Talk.From(506));
        Swap(g, Gather, "Carlos514", 514, new[] { 513 }, "Carlos Robles Azaro", new S(
                Lib: "\"Happy birthday, Your Royal Highness. Your strength is an inspiration to the men and women who brought Zille home.\"",
                Part: "\"Happy birthday, Your Royal Highness. Your strength is an inspiration to our men and women serving in Zille.\""),
            new[] { "#38", "#530", "#515" }, when: NoPalesWar);
        g.Register();

        // The Hearing of Petitions: the Zille war is part of every soldier's case.
        var p = new Talk(Petitions, "ZilleWar.SL.T10.Petitions.");
        p.Line("TaddeusZille", "Taddeus Azaro", "\"Do not forget that it was my soldiers who carried your banner into Zille.\"",
            new[] { "#333" }, condition: And(Lib, PalesNotWon), hooks: Talk.From(343));
        p.Line("TitusZille", "Titus Gordion", "\"You trusted House Azaro, and they led our army to defeat in Zille. I swear that would never have happened under my watch.\"",
            new[] { "#355" }, condition: And(Lost, PalesNotLost, "Variable[\"RiziaDLC.Council_Security_Titus\"] ~= true"), hooks: Talk.From(351));
        Swap(p, Petitions, "Someday", 276, new[] { 439 }, "Taddeus Azaro", new S(
                Part: "\"The line in Zille will not hold forever, sire. When it breaks, I am ready to die a thousand deaths for my proud kingdom.\"",
                Lost: "\"Zille is not finished with us, sire, nor we with it. When war comes again, I am ready to die a thousand deaths for my proud kingdom.\""),
            new[] { "#333" });
        p.Register();

        // House Azaro's spare unit: vanilla says "no wars on the horizon" whenever the Pales war is not on.
        var az = new Talk(Azaro, "ZilleWar.SL.T10.Azaro.");
        Swap(az, Azaro, "Unit", 273, new[] { 134 }, "Taddeus Azaro", new S(
            Ongoing: "\"We can offer a unit from our provincial army. The rest of our men are in Zille, but these have been begging for something to do.\"",
            Lib: "\"We can offer a unit from our provincial army. They came home from Zille with their blood still up, and they've been looking for a fight.\"",
            Part: "\"We can offer a unit from our provincial army. Many of our regiments are still garrisoned in Zille, but I can spare one.\"",
            Lost: "\"We can offer a unit from our provincial army. They came back from Zille with something to prove.\""),
            new[] { "#117", "#268", "#213" });
        az.Register();

        // Lucita's case for the new military headquarters. Vanilla 1145 ("Losing the war") needs a lost Pales war; without any
        // Pales war, a weak army hears 1144 and a strong one 1140, neither of which knows about Zille.
        var hod = new Talk(Hod, "ZilleWar.SL.T10.Hod.");
        Swap(hod, Hod, "Setback", 1144, new[] { 1142 }, "Lucita Azaro", new S(
            Lib: "\"Zille cost us more men and machines than we admit in public. But with the establishment of this headquarters, we could rebuild our military strength and set us on that path.\"",
            Part: "\"Zille cost us more men and machines than we admit in public. But with the establishment of this headquarters, we could rebuild our military strength and set us on that path.\"",
            Lost: "\"Losing Zille set us back considerably. But with the establishment of this headquarters, we could rebuild our military strength and set us on that path.\""),
            new[] { "#1151" }, when: NoPalesWarEver);
        Swap(hod, Hod, "Strong", 1140, new[] { 1138 }, "Lucita Azaro", new S(
            Lost: "\"Zille did not go our way, but the Rizian Armed Forces are still strong, and the Delegates are out of our way... we could make it happen tomorrow.\""),
            new[] { "#1132" });
        hod.Register();

        // Daria on giving Zille to Brenas, months after the army took it.
        var rf = new Talk(Reform, "ZilleWar.SL.T10.Reform.");
        Swap(rf, Reform, "Unstable", 875, new[] { 878 }, "Daria de Rava", new S(Lib:
            "\"That's not something I can promise either, Your Majesty. Zille is still far too unstable for yet another ownership change. The scars of the war have barely healed.\""),
            new[] { "#876" });
        rf.Register();

        // The reform mural on the motorcade route: someone added a soldier to its Zille.
        var pm = new Talk(PrimeMinister, "ZilleWar.SL.T10.PM.");
        pm.Line("Soldier", Narr, "Over the painted harbor of Zille, someone had added a single Rizian soldier. Someone else had already painted over his rifle.",
            new[] { "#272" }, condition: Lib, hooks: Talk.From(271));
        pm.Register();

        // The Pales victory parade: the regiments back from Zille march too (vanilla 204 shows Wehzek troops instead).
        var pa = new Talk(Parade, "ZilleWar.SL.T10.Parade.");
        pa.Line("ZilleWon", Narr, "Behind them marched the regiments that had fought in Zille, new battle honors stitched onto their colors. The crowd cheered them even louder.",
            new[] { "#60" }, condition: Lib, hooks: Talk.From(203));
        pa.Line("ZillePart", Narr, "Behind them marched the regiments back from Zille, fewer than had gone. Their colors were still stained from the line, and the crowd cheered them quietly.",
            new[] { "#60" }, condition: Part, hooks: Talk.From(203));
        pa.Line("ZilleLost", Narr, "Behind them marched the regiments that had fought in Zille. The cheering faltered for a moment, then picked up again, a little too loudly.",
            new[] { "#60" }, condition: Lost, hooks: Talk.From(203));
        pa.Register();
    }

    // ------------------------------------------------------------------ Turn 10, the new government's Foreign Affairs folder

    private static void RegisterTurn10Desk()
    {
        var f = new Talk(ForeignAffairs, "ZilleWar.SL.T10.FA.");

        // Carlos Azaro's recommendation on the Rumburg-Sordland war: vanilla only remembers Rumburgian help in Pales.
        Swap(f, ForeignAffairs, "Rumburg", 804, new[] { 802 }, Narr, S.All(
            "\"Queen Beatrice's help in Zille came when no one else would lift a finger, and Datefort has not forgotten it. It is my recommendation that we return the favor before she has to ask.\""),
            new[] { "#309" }, when: RumburgHelped);

        // Zille liberated: the report on its reintegration says nothing about the war that brought it back.
        f.Line("Damage", Narr,
            "\"The harbor district of Zille city is still being rebuilt after the fighting, and Wraclawitz has yet to accept that the province is gone. Wehzek radio still calls our administration an occupation.\"",
            new[] { "Prisoners", "PrisonersTreaty", "#228" }, condition: Lib, hooks: Talk.From(217, 218));
        f.Line("Prisoners", Narr, "\"The Wehzek prisoners of war in our camps still wait on an exchange that President Smolak refuses to discuss.\"",
            new[] { "#228" }, condition: $"{V(LateT8.VarPrisoners)} == \"held\" and {V(ZilleTreaty.VarSigned)} ~= true");
        f.Line("PrisonersTreaty", Narr, "\"The Wehzek prisoners in our camps will go home as Wehlen pays what it owes under the Treaty of Kreleck.\"",
            new[] { "#228" }, condition: $"{V(LateT8.VarPrisoners)} == \"held\" and {V(ZilleTreaty.VarSigned)} == true");

        // The military report.
        Swap(f, ForeignAffairs, "Buildup", 341, new[] { 344 }, Narr, new S(Lib:
            "\"At the same time, your reign has overseen a formidable military buildup that has solidified Rizia’s position as a regional power. The speed of the Zille campaign has not gone unnoticed in Valgsland or Lespia.\""),
            new[] { "#347" });
        Swap(f, ForeignAffairs, "Weak", 342, new[] { 345 }, Narr, new S(Lost:
            "\"At the same time, Rizia’s diminished military capabilities, and the failure in Zille, have led to a perception of vulnerability. Few nations still believe in our peaceful intentions, and fewer in our ability to defend our interests.\""),
            new[] { "#357" });
        Swap(f, ForeignAffairs, "Intentions", 348, new[] { 342 }, Narr, new S(
            Ongoing: "\"After Zille, none of our neighbors doubts our intentions. They doubt our ability to defend our interests.\"",
            Lib: "\"After Zille, our neighbors take our army seriously. Some still doubt our intentions, and none can ignore our guns.\"",
            Part: "\"After Zille, none of our neighbors doubts our intentions. They doubt our ability to defend our interests.\""),
            new[] { "#357" });
        f.Register();
    }

    // ------------------------------------------------------------------ the endings

    private static void RegisterEndings()
    {
        var c = new Talk(Continued, "ZilleWar.SL.End.Continued.");
        const string baton = "\nThe orchestra conductor lifted his baton, awaiting your signal.";
        Swap(c, Continued, "Praise", 625, new[] { 623 }, Narr, new S(
            Ongoing: "The hall filled with silence as Lucita stepped forward and praised Rizia's troops for their bravery and service, and asked the hall to remember the men still fighting in Zille." + baton,
            Lib: "The hall filled with silence as Lucita stepped forward and praised Rizia's troops for their bravery and service, and named the regiments that took Zille harbor, one by one." + baton,
            Part: "The hall filled with silence as Lucita stepped forward and praised Rizia's troops for their bravery and service, and named the regiments that held Zille harbor, one by one." + baton,
            Lost: "The hall filled with silence as Lucita stepped forward and praised Rizia's troops for their bravery and service, and asked the hall to remember the men who never came back from Zille." + baton),
            new[] { "#621" });
        c.Register();

        var d = new Talk(Death, "ZilleWar.SL.End.Death.");
        const string last = "\nIn your last moments, you thought it was...";
        Swap(d, Death, "Beach", 93, new[] { 91 }, Narr, new S(
            Ongoing: "You blinked and you were a child again, running on the Zille beach, a beach your soldiers were still fighting for. You were holding up a Rizian flag, its colors bold against the blue sky." + last,
            Lib: "You blinked and you were a child again, running on the Zille beach, the same beach your soldiers would one day storm. You were holding up a Rizian flag, its colors bold against the blue sky." + last,
            Part: "You blinked and you were a child again, running on the Zille beach, the same beach your soldiers would one day storm. You were holding up a Rizian flag, its colors bold against the blue sky." + last,
            Lost: "You blinked and you were a child again, running on the Zille beach, a beach your soldiers would one day die trying to reach. You were holding up a Rizian flag, its colors bold against the blue sky." + last),
            new[] { "#40", "#41", "#42" });
        d.Register();

        var p = new Talk(Prison, "ZilleWar.SL.End.Prison.");
        p.Line("ZilleLib", Narr, "A guard let slip that the new government had kept Zille. Your one victory had outlived your reign.",
            new[] { "#304" }, condition: Lib, hooks: Talk.From(340));
        p.Line("ZillePart", Narr, "A guard let slip that the armistice line in Zille still held, and that the new government had kept Zille city. Nobody told you what they had promised Wehlen for it.",
            new[] { "#304" }, condition: And(Part, $"Variable[\"{ZilleTreaty.VarKind}\"] ~= \"border\""), hooks: Talk.From(340));
        p.Line("ZillePartBorder", Narr, "A guard let slip that the border you signed at Mave Nortus still held, and that the new government had kept Zille city. Nobody had asked your opinion of it.",
            new[] { "#304" }, condition: And(Part, $"Variable[\"{ZilleTreaty.VarKind}\"] == \"border\""), hooks: Talk.From(340));
        string kind = $"Variable[\"{ZilleTreaty.VarKind}\"]";
        string signedT = $"Variable[\"{ZilleTreaty.VarSigned}\"] == true";
        p.Line("ZilleLost", Narr, "A guard let slip that the new government had recognized Wehzek sovereignty over Zille. Nobody in the prison had been surprised.",
            new[] { "#304" }, condition: And(Lost, WarFlags.L(LateT9.VarRecognized), $"{kind} ~= \"recognized\""), hooks: Talk.From(340));
        p.Line("ZilleLostTreaty", Narr, "A guard let slip that the new government had recognized Wehzek sovereignty over Zille. The terms were signed in Wraclawitz, in a palace hung with Wehzek flags, and nobody in the prison had been surprised.",
            new[] { "#304" }, condition: And(Lost, $"{kind} == \"recognized\""), hooks: Talk.From(340));
        p.Line("ZilleLostTerms", Narr, "A guard let slip that the King had signed Wehzek terms in Wraclawitz and kept his claim to Zille on paper. Nobody in the prison believed the claim would last.",
            new[] { "#304" }, condition: And(Lost, WarFlags.LNot(LateT9.VarRecognized), $"{kind} == \"terms\""), hooks: Talk.From(340));
        p.Line("ZilleLostNo", Narr, "A guard let slip that the new government had stopped talking about Zille. Nobody had signed anything, and nobody had asked for the province back.",
            new[] { "#304" }, condition: And(Lost, WarFlags.LNot(LateT9.VarRecognized), $"not ({signedT})"), hooks: Talk.From(340));
        p.Register();

        // The Turn 11 speech has a Zille line only when Zille came back.
        var s = new Talk(Speech, "ZilleWar.SL.End.Speech.");
        s.Choice("LostHonest", "\"I will not pretend that Zille was won. But I will never pretend that it was not worth fighting for.\"",
            new[] { "#203" }, condition: Lost, hooks: Talk.From(163));
        s.Choice("LostOwn", "\"We tried to bring Zille home, and we failed. That failure is mine, not our soldiers'.\"",
            new[] { "#203" }, condition: Lost, hooks: Talk.From(163));
        // Part: an armistice line, or a border signed at the treaty; every city Rizian after the treaty's Auremina term.
        string partSome = And(Part, $"not ({AllHeld})");
        string partLine = And(partSome, $"not ({BorderKind})");
        string partBorder = And(Part, BorderKind);
        const string cityText = "\"Zille city is Rizian again. The rest of Zille will follow, in time.\"";
        const string lineText = "\"Our soldiers brought Zille city home. The armistice line will not last forever.\"";
        const string borderText = "\"Our soldiers brought Zille city home, and I signed the border that keeps it ours.\"";
        const string allText = "\"Every city of Zille is Rizian again, won by our soldiers and kept at the table.\"";
        s.Choice("PartCity", cityText, new[] { "#203" }, condition: partSome, hooks: Talk.From(163));
        s.Choice("PartLine", lineText, new[] { "#203" }, condition: partLine, hooks: Talk.From(163));
        s.Choice("PartBorder", borderText, new[] { "#203" }, condition: partBorder, hooks: Talk.From(163));
        s.Choice("AllHeld", allText, new[] { "#203" }, condition: AllHeld, hooks: Talk.From(163));
        foreach ((string sfx, int from) in new[] { ("B", 177), ("C", 668) })
        {
            s.Choice("LostHonest" + sfx, "\"I will not pretend that Zille was won. But I will never pretend that it was not worth fighting for.\"",
                new[] { "#178" }, condition: Lost, hooks: Talk.From(from));
            s.Choice("LostOwn" + sfx, "\"We tried to bring Zille home, and we failed. That failure is mine, not our soldiers'.\"",
                new[] { "#178" }, condition: Lost, hooks: Talk.From(from));
            s.Choice("PartCity" + sfx, cityText, new[] { "#178" }, condition: partSome, hooks: Talk.From(from));
            s.Choice("PartLine" + sfx, lineText, new[] { "#178" }, condition: partLine, hooks: Talk.From(from));
            s.Choice("PartBorder" + sfx, borderText, new[] { "#178" }, condition: partBorder, hooks: Talk.From(from));
            s.Choice("AllHeld" + sfx, allText, new[] { "#178" }, condition: AllHeld, hooks: Talk.From(from));
        }

        s.Register();
    }
}
