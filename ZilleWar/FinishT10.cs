using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Turn 10, read line by line (the large conversations: reform talks, the three house conversations, the houses' gathering,
/// the Prime Minister's Zille report, the domestic affairs desk) for what the earlier sweep left:
/// - generic war lines that vanilla only plays in the Pales war also play while the Zille war is still fought
///   (the Azaros' stretched army, "we have a war to win", Valenqiris' patience, Taddeus leaving the front);
/// - Rusty's tobacco farm in Zille follows who got the Auremina mines and which side of the line his land is on;
/// - the new government's Zille report and the domestic desk remember that Zille came back by force, and its gold;
/// - the Prime Minister's election brief tells a ceasefire line or a lost war from a lost negotiation.
/// Manus Sazon's Turn 10 lines are in <see cref="FinishManus"/>.
/// </summary>
internal static class FinishT10
{
    private const string Reform = "Rizia/Turn 10/REFORM TALKS/REFORM TALKS Conversation";
    private const string Toras = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE TORAS CONVO";
    private const string Azaro = "Rizia/Turn 10/DEFAULT GATHERING SCENE/DEFAULT HOUSE AZARO CONVO";
    private const string Gather = "Rizia/Turn 10/HOUSES GATHER/HOUSES GATHER Conversation";
    private const string ZilleBranch = "Rizia/Turn 10/Zille_Reform_Branch/Zille_Reform_Branch Conversation";
    private const string Domestic = "Rizia/Turn 10/Domestic Affairs/Domestic Affairs Conversation";
    private const string Elections = "Rizia/Turn 10/Election_Issues/Election_Issues Conversation";

    private static string ZilleOnly => EarlyT7.ZilleOnly;

    public static void Register()
    {
        // Still fighting in Zille: vanilla's Pales-only war lines.
        var rf = new Talk(Reform, "ZilleWar.Fin.Reform10.");
        EarlyT7.Widen(rf, "Frontlines46", 42, 46, And(ZilleOnly, "Variable[\"RiziaDLC.Lucita_Arrested\"] ~= true")); // "I did not leave the frontlines..."
        rf.Register();

        var to = new Talk(Toras, "ZilleWar.Fin.Toras10.");
        EarlyT7.Widen(to, "WarToWin136", 137, 136, ZilleOnly);  // "We have a war to win, after all."
        EarlyT7.Widen(to, "Effort535", 531, 535, ZilleOnly);    // "Of course, we still support the war effort."
        EarlyT7.Widen(to, "Patience594", 590, 594, ZilleOnly);  // "Their patience with the war has begun to wear quite thin."
        to.Register();

        var az = new Talk(Azaro, "ZilleWar.Fin.Azaro10.");
        EarlyT7.Widen(az, "WinWar95", 92, 95, ZilleOnly);       // "I will be once we win the war."
        EarlyT7.Widen(az, "Embroiled131", 132, 131, ZilleOnly); // "with most of our house embroiled in the war"
        EarlyT7.Widen(az, "Embroiled196", 193, 196, ZilleOnly);
        EarlyT7.Widen(az, "Strain399", 396, 399, ZilleOnly);    // "The war continues to strain resources"
        az.Register();

        // Rusty's tobacco farm in Zille (the gift to the Queen Mother).
        var g = new Talk(Gather, "ZilleWar.Fin.Gather10.");
        string gold = $"Variable[\"{LateT8.VarGold}\"]";
        g.Variant("FarmCrown", 694, new[] { 691 }, And(Lib, $"{gold} == \"crown\""), "Russello Montoro",
            "\"Yeah. The Crown kept the Auremina pits for itself, so I bought up the land around them instead. Mined out, most of it -- but ripe for rehabilitation as farmland.\"",
            new[] { "#700" });
        g.Variant("FarmSazon", 694, new[] { 691 }, And(Lib, $"{gold} == \"sazon\""), "Russello Montoro",
            "\"Yeah. The Sazons got the Auremina pits, so I bought up the land around them instead. Mined out, most of it -- but ripe for rehabilitation as farmland.\"",
            new[] { "#700" });
        Swap(g, Gather, "Farm", 699, new[] { 698 }, "Russello Montoro", new S(
            Part: "\"Yeah. Some of RRG's old land ended up on our side of the armistice line. All mined out -- but perfect for farming.\"",
            PartBorder: "\"Yeah. Some of RRG's old land ended up on our side of the new border. All mined out -- but perfect for farming.\"",
            Lost: "\"Yeah. Turns out the Wehzek colonel running Zille now likes Rizian money as much as the last one did. Got back a patch of old RRG land that's all mined out -- but perfect for farming.\""),
            new[] { "#700" });
        g.Register();

        // The new government's report on Zille's elections: Zille came back at the point of a bayonet.
        var z = new Talk(ZilleBranch, "ZilleWar.Fin.ZilleRef10.");
        Swap(z, ZilleBranch, "SayManus", 6, new[] { 5 }, "Manus Sazon", new S(Lib:
            "\"However, Zille came back to us by force of arms, and many there feel the decision was made over their heads. It doesn't help that some of the polling stations are in buildings our own artillery shelled during the war.\""),
            new[] { "#8" });
        Swap(z, ZilleBranch, "SayDaria", 46, new[] { 42 }, "Daria de Rava", new S(Lib:
            "\"However, Zille came back to us at the point of a bayonet, and a good many Zilleans resent having had no say in it. That has led to some resistance.\""),
            new[] { "#8" });
        Swap(z, ZilleBranch, "SayAdarfo", 47, new[] { 7 }, "Adarfo Sotelato", new S(Lib:
            "\"Yet nobody asked the people of Zille anything. An army marched in, and now they get a ballot. You can guess how warmly that's been received.\""),
            new[] { "#8" });
        // Vanilla [5], [42] and [7] describe a finished reintegration; before and without a liberation the war's registers are in pieces.
        // ([3] and [4] need the referendum won, which a war game never has.)
        Swap(z, ZilleBranch, "RollsManus", 5, new[] { 40 }, "Manus Sazon", S.NotLib(
            "\"The war left Zille's registers in pieces, and we are building the voter rolls from whatever survived. Registration is slow, but those who have come forward vote in numbers.\""),
            new[] { "#6" });
        Swap(z, ZilleBranch, "RollsDaria", 42, new[] { 44 }, "Daria de Rava", S.NotLib(
            "\"The war left Zille's registers in pieces, so the commission is rebuilding the rolls from whatever survived. Registration is slow, but those who come forward turn out in numbers.\""),
            new[] { "#46" });
        Swap(z, ZilleBranch, "RollsAdarfo", 7, new[] { 45 }, "Adarfo Sotelato", S.NotLib(
            "\"The war left Zille's registers in pieces, and the commission is rebuilding the rolls from whatever survived. Registration is slow, but those who come forward turn out in numbers.\""),
            new[] { "#47" });
        z.Register();

        // The Prime Minister's election brief: vanilla's "losing Zille" is a lost negotiation; after a war it is a ceasefire line
        // or a lost war. "also" versions follow the Pales update.
        var e = new Talk(Elections, "ZilleWar.Fin.Election10.");
        // Part may hold Zille city alone or with any of Auremina, Isor and Nanta, so no share of the province is named; after a
        // signed border treaty it is a border, not an armistice line (and the Aure term can leave nothing on the far side of it).
        const string manusPart = "The armistice in Zille has {0}become a rallying point for voters. Part of a province won and the rest left behind the line, and many ask whether that was worth the price.";
        const string manusBorder = "The border treaty in Zille has {0}become a rallying point for voters, and many ask whether it was worth the price.";
        const string manusLost = "The lost war for Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change.";
        const string manusLib = "Zille's return has {0}become a rallying point for voters, and many ask why the Crown never asked them what they wanted.";
        const string dariaPart = "The unfinished war in Zille has {0}become a rallying point for voters. They wanted a victory or a peace, and got a line of trenches.";
        const string dariaBorder = "The border in Zille has {0}become a rallying point for voters. They wanted a victory or a peace, and got a border, with a treaty to keep it.";
        const string dariaLost = "The defeat in Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change.";
        const string dariaLib = "The return of Zille has {0}become a rallying point for voters, and their pride in the victory is tangled with anger at what it cost.";
        const string adarfoPart = "The armistice in Zille has {0}become a rallying point for voters. Their sons died for part of a province, and they want to know who decided that was enough.";
        const string adarfoBorder = "The border treaty in Zille has {0}become a rallying point for voters. Their sons died for that border, and they want to know who decided where it ran.";
        // lost: Zille city is Wehzek, but the army may still hold Auremina, Isor or Nanta
        const string adarfoLostAll = "The lost war in Zille has {0}become a rallying point for voters. Their sons died for nothing, and they know exactly whose war it was.";
        const string adarfoLostSome = "The lost war in Zille has {0}become a rallying point for voters. Their sons died, Zille city still flies the Wehzek flag, and they know exactly whose war it was.";
        const string adarfoLib = "Zille's return has {0}become a rallying point for voters. Their sons won it back, and they want to know whether the Crown will ever thank them.";
        string noneHeld = And($"not ({WarFlags.L(ZilleOperation.CapturedVar("Auremina"))})", $"not ({WarFlags.L(ZilleOperation.CapturedVar("Isor"))})",
            $"not ({WarFlags.L(ZilleOperation.CapturedVar("Nanta"))})");
        string Q(string text, string a) => "\"" + string.Format(text, a) + "\"";

        // the loss branch ([157], ZilleReturn_Success false): a divided or lost war
        foreach ((string name, int entry, int parent, string speaker, bool also, string part, string border, string lost) in new[]
                 {
                     ("Manus", 156, 171, "Manus Sazon", false, manusPart, manusBorder, manusLost),
                     ("ManusAlso", 172, 170, "Manus Sazon", true, manusPart, manusBorder, manusLost),
                     ("Daria", 163, 175, "Daria de Rava", false, dariaPart, dariaBorder, dariaLost),
                     ("DariaAlso", 176, 174, "Daria de Rava", true, dariaPart, dariaBorder, dariaLost),
                     ("Adarfo", 164, 167, "Adarfo Sotelato", false, adarfoPart, adarfoBorder, null),
                     ("AdarfoAlso", 168, 166, "Adarfo Sotelato", true, adarfoPart, adarfoBorder, null),
                 })
        {
            string a = also ? "also " : string.Empty;
            Swap(e, Elections, name, entry, new[] { parent }, speaker,
                new S(Part: Q(part, a), PartBorder: Q(border, a), Lost: lost == null ? null : Q(lost, a)), new[] { "#177" });
            if (lost == null)
            {
                Swap(e, Elections, name + "N", entry, new[] { parent }, speaker, new S(Lost: Q(adarfoLostAll, a)), new[] { "#177" }, when: noneHeld);
                Swap(e, Elections, name + "S", entry, new[] { parent }, speaker, new S(Lost: Q(adarfoLostSome, a)), new[] { "#177" },
                    when: $"not ({noneHeld})");
            }
        }

        // the return branch ([142], ZilleReturn_Success true, set by a liberation): Zille came back by force
        foreach ((string name, int entry, int parent, string speaker, bool also, string lib) in new[]
                 {
                     ("Manus_Ret", 81, 146, "Manus Sazon", false, manusLib),
                     ("ManusAlso_Ret", 147, 145, "Manus Sazon", true, manusLib),
                     ("Daria_Ret", 88, 150, "Daria de Rava", false, dariaLib),
                     ("DariaAlso_Ret", 151, 149, "Daria de Rava", true, dariaLib),
                     ("Adarfo_Ret", 89, 154, "Adarfo Sotelato", false, adarfoLib),
                     ("AdarfoAlso_Ret", 155, 153, "Adarfo Sotelato", true, adarfoLib),
                 })
        {
            Swap(e, Elections, name, entry, new[] { parent }, speaker, new S(Lib: Q(lib, also ? "also " : string.Empty)), new[] { "#177" });
        }

        e.Register();

        // The domestic desk: Zille's resources are the Auremina gold the Wehzeks flooded before they left.
        var d = new Talk(Domestic, "ZilleWar.Fin.Domestic10.");
        Swap(d, Domestic, "Gold", 631, new[] { 629 }, Talk.Narrator, new S(Lib:
            "\"This is not yet taking into account the gold in the Auremina hills. The Wehzeks flooded the pits before they withdrew, and the engineers are only now pumping the last of them dry.\""),
            new[] { "#190" });
        d.Register();
    }
}
