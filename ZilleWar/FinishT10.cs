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
            Part: "\"Yeah. Some of RRG's old land ended up on our side of the ceasefire line. All mined out -- but perfect for farming. The Wehzek side I'm still working on.\"",
            Lost: "\"Yeah. Turns out the Wehzek colonel running Zille now likes Rizian money as much as the last one did. Got back a patch of old RRG land that's all mined out -- but perfect for farming.\""),
            new[] { "#700" });
        g.Register();

        // The new government's report on Zille's elections: Zille came back at the point of a bayonet.
        var z = new Talk(ZilleBranch, "ZilleWar.Fin.ZilleRef10.");
        Swap(z, ZilleBranch, "SayManus", 6, new[] { 5 }, "Manus Sazon", new S(Lib:
            "\"However, Zille came back to us by force of arms, and many there feel the decision was made over their heads. It doesn't help that some of the polling stations are in buildings our own artillery shelled a few months ago.\""),
            new[] { "#8" });
        Swap(z, ZilleBranch, "SayDaria", 46, new[] { 42 }, "Daria de Rava", new S(Lib:
            "\"However, Zille came back to us at the point of a bayonet, and a good many Zilleans resent having had no say in it. That has led to some resistance.\""),
            new[] { "#8" });
        Swap(z, ZilleBranch, "SayAdarfo", 47, new[] { 7 }, "Adarfo Sotelato", new S(Lib:
            "\"Yet nobody asked the people of Zille anything. An army marched in, and now they get a ballot. You can guess how warmly that's been received.\""),
            new[] { "#8" });
        z.Register();

        // The Prime Minister's election brief: vanilla's "losing Zille" is a lost negotiation; after a war it is a ceasefire line
        // or a lost war. "also" versions follow the Pales update.
        var e = new Talk(Elections, "ZilleWar.Fin.Election10.");
        foreach ((string name, int entry, int parent, string speaker, bool also, string part, string lost) in new[]
                 {
                     ("Manus", 156, 171, "Manus Sazon", false,
                         "The ceasefire in Zille has {0}become a rallying point for voters. Half a province won and half given up, and many feel the price was worth neither.",
                         "The lost war for Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change."),
                     ("ManusAlso", 172, 170, "Manus Sazon", true,
                         "The ceasefire in Zille has {0}become a rallying point for voters. Half a province won and half given up, and many feel the price was worth neither.",
                         "The lost war for Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change."),
                     ("Daria", 163, 175, "Daria de Rava", false,
                         "The unfinished war in Zille has {0}become a rallying point for voters. They wanted a victory or a peace, and got a line of trenches.",
                         "The defeat in Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change."),
                     ("DariaAlso", 176, 174, "Daria de Rava", true,
                         "The unfinished war in Zille has {0}become a rallying point for voters. They wanted a victory or a peace, and got a line of trenches.",
                         "The defeat in Zille has {0}become a rallying point for voters, whose anger and deepening frustrations with the current leadership fuel their desire for change."),
                     ("Adarfo", 164, 167, "Adarfo Sotelato", false,
                         "The ceasefire in Zille has {0}become a rallying point for voters. Their sons died for half a province, and they want to know who decided that was enough.",
                         "The lost war in Zille has {0}become a rallying point for voters. Their sons died for nothing, and they know exactly whose war it was."),
                     ("AdarfoAlso", 168, 166, "Adarfo Sotelato", true,
                         "The ceasefire in Zille has {0}become a rallying point for voters. Their sons died for half a province, and they want to know who decided that was enough.",
                         "The lost war in Zille has {0}become a rallying point for voters. Their sons died for nothing, and they know exactly whose war it was."),
                 })
        {
            string a = also ? "also " : string.Empty;
            Swap(e, Elections, name, entry, new[] { parent }, speaker,
                new S(Part: "\"" + string.Format(part, a) + "\"", Lost: "\"" + string.Format(lost, a) + "\""), new[] { "#177" });
        }

        e.Register();

        // The domestic desk: Zille's resources are the Auremina gold the Wehzeks flooded before they left.
        var d = new Talk(Domestic, "ZilleWar.Fin.Domestic10.");
        Swap(d, Domestic, "Gold", 631, new[] { 629 }, Talk.Narrator, new S(Lib:
            "\"This is not yet taking into account the gold in the Auremina hills. The Wehzeks flooded the pits before they withdrew, but the engineers expect them pumped dry within the year.\""),
            new[] { "#190" });
        d.Register();
    }
}
