using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Turn 6 at home while the Zille war is fought:
/// - the polio charity match in Montaklar (c376): a minute's silence for the fallen, Azaro officers on leave, Titus's
///   Wehzek worry, Rusty's quip about workers at the front, and Rusty's offer to buy first claim on the Auremina mines;
/// - the House of Delegates results (c363, a sub-conversation of the council): Hugo reads the war into the vote;
/// - Elena's energy overview (c586, also a council sub-conversation): the army's fuel comes first;
/// - before the balcony speech (c373): Hugo's "our armies will not falter" knows where the best regiments are;
/// - the step 1 and 2 decisions (Argno strike, Konstantium media campaign, railway decline, MITZ checkpoint delays)
///   and the news and reports they enable.
/// </summary>
internal static class SweepEarlyT6Home
{
    /// <summary>"pledged" | "refused" | "later": what the King told Rusty Montoro about the Auremina mines at the match.
    /// "pledged" means RRG paid 2 Budget into the war chest for first claim on the Auremina concession.</summary>
    public const string VarRustyZilleGold = "ZilleWar.SE_Home_RustyZilleGold";

    private const string NewsRusty = "ZilleWar.SE_Home_News_RustyGold";

    private const string Football = "Rizia/Turn 6/A_Rusty_FootballGame";
    private const string Hod = "Rizia/Turn 6/HOD Results";
    private const string EnergyConv = "Rizia/Turn 6/ENERGY SITUATION/ENERGY SITUATION Conversation";
    private const string Intro = "Rizia/Turn 6/Intro Section";

    private const string Rusty = "Russello Montoro";
    private const string Titus = "Titus Gordion";
    private const string Hugo = "Hugo Toras";
    private const string Elena = "Elena Werner";

    private static readonly string AureminaHeld = WarFlags.L(ZilleOperation.CapturedVar("Auremina"));
    private static readonly string RustyUnasked = $"(Variable[\"{VarRustyZilleGold}\"] == nil or Variable[\"{VarRustyZilleGold}\"] == \"\")";

    public static void Register()
    {
        foreach (string v in new[] { VarRustyZilleGold, NewsRusty })
        {
            Variables.Register(v);
        }

        FootballMatch();
        HouseResults();
        EnergyOverview();
        BalconyIntro();
        Decisions();
        Press();
    }

    // ================================================================== c376 The charity match in Montaklar

    private static void FootballMatch()
    {
        var t = new Talk(Football, "ZilleWar.SE.Home.Football.");

        // "Any threats I should be aware of today, Titus?"
        t.Line("Titus", Titus,
            "\"With the war on, we've doubled the checks at the gates as well. A crowd this size would be a fine prize for a Wehzek agent.\"",
            new[] { "#48" }, condition: Active, hooks: From(43, 47));

        // Lucita and Taddeus among the Azaro nobles.
        t.Line("Officers", Narrator,
            "Several of the Azaro men around them wore uniform beneath their club scarves. By the end of the week, most of them would be " +
            "back with their regiments in the north.", new[] { "#108" }, condition: Active, hooks: From(64));

        // The opening ceremony.
        t.Line("Silence", Narrator,
            "Before she began, the announcer had asked the stadium to stand for a minute's silence for the soldiers who had fallen in " +
            "Zille. Thousands of people had risen as one, and for a full minute the only sound was the flags snapping above the stands. " +
            "Then she gave her speech.",
            new[] { "#485" }, condition: Declared, hooks: From(85));

        // "She spoke of rising polio numbers in Wehlen..."
        t.Variant("Polio", 499, new[] { 493 }, Active, Narrator,
            "She spoke of rising polio numbers in Wehlen and among the families fleeing the fighting in Zille, and of the importance of " +
            "vaccination in preventing a pandemic here in Rizia.", new[] { "#497" });

        // "A cause like a war? They can't protest if they're called to the front."
        t.Variant("Front", 1098, new[] { 1087, 1088 }, Active, Romus,
            "\"A cause like the war in Zille? They can't strike while they're at the front.\"", new[] { "FrontA" });
        t.Line("FrontA", Rusty,
            "\"Sure. Neither can they dig. Half my night shift in Argno got their call-up papers last month.\"", new[] { "#287" });

        // Rusty's deep pockets meet the war chest: first claim on the Auremina mines.
        t.Line("Gold1", Rusty, "\"Speaking of deep pockets. I hear that little war of yours up north isn't coming cheap.\"",
            new[] { "Gold2Held", "Gold2" }, condition: $"{Active} and {RustyUnasked}", hooks: From(1100, 1120, 1124, 1073));
        t.Line("Gold2Held", Rusty,
            "\"Our boys are in Auremina now. Those hills are what brought RRG north in the first place, before the Wehzeks took over the " +
            "pits. So here's my offer. RRG puts six billion guilders into the war chest today, and when the shooting stops, RRG gets " +
            "first claim on the Auremina concession.\"",
            new[] { "Pledge", "Refuse", "Later" }, condition: AureminaHeld);
        t.Line("Gold2", Rusty,
            "\"Those hills around Auremina are what brought RRG north in the first place, before the Wehzeks took over the pits. So " +
            "here's my offer. RRG puts six billion guilders into the war chest today, and when our boys plant the flag over Auremina, RRG " +
            "gets first claim on the concession.\"",
            new[] { "Pledge", "Refuse", "Later" }, condition: $"not ({AureminaHeld})");
        t.Choice("Pledge", "\"Rizian Royal Gold will have first claim on Auremina. Send the money.\"", new[] { "PledgeA" }, cost: "+2 Budget",
            script: Set(VarRustyZilleGold, "\"pledged\"") + Inc("RiziaDLC.Resources_Budget", 2));
        t.Line("PledgeA", Rusty,
            "\"Pleasure doing business, Your Majesty. My accountants will be at the Treasury by Monday. Ms. Werner's going to love that.\"",
            new[] { "#651" });
        t.Choice("Refuse", "\"Our soldiers are not dying for your shareholders, Rusty.\"", new[] { "RefuseA" },
            script: Set(VarRustyZilleGold, "\"refused\""));
        t.Line("RefuseA", Rusty,
            "\"Course not. They're dying for Rizia. I just figured Rizia could use somebody to pay for the pumps once they're done.\"",
            new[] { "#651" });
        t.Choice("Later", "\"Not today, Rusty. The Treasury will manage.\"", new[] { "LaterA" }, script: Set(VarRustyZilleGold, "\"later\""));
        t.Line("LaterA", Rusty, "\"Course it will. Ms. Werner always does. The pumps will wait. I'm a patient man. Not a very patient man, but a patient one.\"", new[] { "#651" });

        // "But the results didn't count. The proper season had yet to begin."
        t.Line("Boxes", Narrator,
            "On the way out, I passed the collection boxes by the gates. Beside the ones for polio research, someone had set out a second " +
            "row with hand-lettered signs: For the families of Zille. They were fuller.",
            new[] { "#1077" }, condition: Active, hooks: From(985));
        t.Register();
    }

    // ================================================================== c363 House of Delegates results (council)

    private static void HouseResults()
    {
        var t = new Talk(Hod, "ZilleWar.SE.Home.Hod.");

        // After "The RNC holds the power."
        t.Line("HoldsWar", Hugo,
            "\"The war helped, of course. A nation with its sons in Zille does not change its government in the middle of the fighting.\"",
            new[] { "#257" }, condition: Active, hooks: From(249));
        t.Line("HoldsWon", Hugo,
            "\"Zille helped, of course. Nothing fills a polling station like a victory.\"",
            new[] { "#257" }, condition: $"{Liberated} and Variable[\"RiziaDLC.HouseElections_RNCWinsSeats\"] ~= true", hooks: From(249));
        // the Coalition's landslide (vanilla [13], "over three-quarters of delegates") after a liberation
        t.Line("Landslide", Hugo,
            "\"Most of it is Zille, Your Majesty. The vote came with the church bells still ringing for the liberation, and the Coalition's posters carried a single picture: our soldiers in the great square of Zille.\"",
            new[] { "#83" }, condition: Liberated, hooks: From(13));
        t.Line("LandslidePart", Hugo,
            "\"Zille city carried it, I think. The voters have the harbor back and their sons home, and they thanked the Crown for both.\"",
            new[] { "#83" }, condition: AuditT6.Part, hooks: From(13));
        t.Line("HoldsLost", Hugo,
            "\"Mercifully, the voters did not punish us for Zille. Not yet.\"",
            new[] { "#257" }, condition: AuditT6.Lost, hooks: From(249));
        t.Line("HoldsPart", Hugo,
            "\"The armistice helped, I think. The voters have Zille city back and their sons coming home. For most of them, that is enough.\"",
            new[] { "#257" }, condition: AuditT6.Part, hooks: From(249));

        // After "The RPP has now become a real threat."
        t.Line("ThreatWar", Hugo,
            "\"Sazon's people went from village to village in Brenas reading out the casualty lists from Zille. It worked.\"",
            new[] { "#256" }, condition: Active, hooks: From(250));
        t.Line("ThreatWon", Hugo,
            "\"Not even Zille saved us. It seems the voters thank the soldiers, not the Crown.\"",
            new[] { "#256" }, condition: Liberated, hooks: From(250));
        t.Line("ThreatLost", Hugo,
            "\"And Zille did us no favors. The RPP has been asking every widow in Brenas what her husband died for.\"",
            new[] { "#256" }, condition: AuditT6.Lost, hooks: From(250));
        t.Line("ThreatPart", Hugo,
            "\"And the armistice did us no favors. The RPP has been asking every widow in Brenas why the line stopped where it did.\"",
            new[] { "#256" }, condition: AuditT6.Part, hooks: From(250));
        t.Register();
    }

    // ================================================================== c586 Elena's energy overview (council)

    private static void EnergyOverview()
    {
        var t = new Talk(EnergyConv, "ZilleWar.SE.Home.Energy.");
        t.Line("Fuel", Elena,
            "\"I should say at the outset that the war has changed every figure in this file. The army's fuel orders for the northern " +
            "front have nearly doubled since the declaration, and the General Staff has first call on every barrel.\"",
            new[] { "#167" }, condition: Active, hooks: From(50, 51));
        t.Line("FuelOver", Elena,
            "\"I should say at the outset that the war in Zille has left its mark on every figure in this file. The army burned through a " +
            "winter's worth of fuel in the north, and the depots still have to be refilled before anything else.\"",
            new[] { "#167" }, condition: AuditT6.Over, hooks: From(50, 51));
        t.Register();
    }

    // ================================================================== c373 Before the balcony speech

    private static void BalconyIntro()
    {
        var t = new Talk(Intro, "ZilleWar.SE.Home.Intro.");
        // "Look at how prepared we are, Madam Queen Mother. Our armies will not falter."
        t.Variant("Prepared", 103, new[] { 107 }, Active, Hugo,
            "\"Look at how prepared we are, Madam Queen Mother. Even with our best regiments in Zille, our armies will not falter.\"",
            new[] { "#110" });
        t.Register();
    }

    // ================================================================== decisions

    private static void Decisions()
    {
        Decision("Turn06_Decision_MalenyevistWorkersStrike", o => !IsActive ? null :
            "In Argno, workers across all industries have laid down their tools in solidarity, demanding an improvement of working " +
            "conditions for everyone, regardless of profession. With the army fighting in Zille, the strike has also stopped the railway " +
            "workshops and depots that keep the supply trains to the front moving. Reports indicate that former Rizian Royal Gold employee " +
            "Adarfo Sotelato is behind this effort, which has also impacted the RRG-run mines just outside the city. The General Staff " +
            "wants the trains running again; the strikers say a kingdom that can afford a war can afford to treat its workers better.");
        Decision("Turn06_Decision_PublicTransportationWorse_Penalty", o => !IsActive ? null : BeforeTag(o,
            "Since the war began, the army has requisitioned locomotives, carriages and crews for the northern front, and the civilian " +
            "timetable has paid the price."));
        Decision("Turn06_Decision_DelaysAtMITZCheckpoints_Penalty", o => !IsActive ? null : o.TrimEnd() +
            " Morellan officials have let it be known, off the record, that they are searching every truck for war materiel bound for " +
            "the Zille front.");
        Decision("Turn06_Decision_MediaCampaign", o =>
            "Since the King declared war on Wehlen from the podium of the Alliance of Nations, Konstantium has heard a great deal about " +
            "Rizia, most of it from Wehzek diplomats who call the war over Zille an unprovoked conquest. A significant media campaign " +
            "centered in Konstantium, Kyrute, known for being a hub of diplomatic activity and international lobbying, could put the " +
            "Rizian case for Zille before the world's embassies and press, and win back some of the goodwill the declaration cost us.");
    }

    // ================================================================== press

    private static void Press()
    {
        News("Turn06_G_Decision_MediaCampaign", (s, _) => P(
                "Rizian diplomats, aided by a small army of hired publicists, have spent the past weeks in Konstantium making the case for " +
                "King Romus' war over Zille. Glossy brochures recalling the Zille Agreement of 1927, under which Rizia lent the region to " +
                "Wehlen in gratitude for its help in crushing the uprising of 1926, now lie in the waiting rooms of half the embassies in " +
                "the city, alongside photographs of Rizian families who have lived in Zille for generations. \"Wehlen was given a loan, " +
                "not a gift,\" reads one. \"The loan has expired.\"",
                "Wehzek diplomats have dismissed the campaign as \"gold-plated propaganda\". Still, several delegations that condemned the " +
                "declaration of war from the floor of the Alliance of Nations are said to have softened their tone in private." +
                (s == StWon ? " Victory, as one Kyrutian official put it, has a way of making any brochure more persuasive." : "")),
            _ => "Rizia Makes Its Case in Konstantium");

        News("Turn06_V_Decision_PublicTransportationWorse", (_, _) => P(
            "Inexplicably, King Romus has managed to let the public transport crisis get even worse. Only a staggering 36% of trains now " +
            "depart on time, down from 74% at the start of Romus' reign, and an entire railway connecting millions of Rizian citizens in " +
            "Porte Drazon, Monqiz and between has been put out of service.",
            "The railway administration blames the army, which has taken the best locomotives and the most experienced crews north to " +
            "carry men and shells to Zille. We at the Voice would point out that the trains were late long before anyone marched on Zille.",
            "The question remains, when will our trains run on time!"), when: s => s == StActive);

        News("Turn06_RH_Decision_MalenyevistWorkerStrike_TroubleMakersDispersed", (_, _) => P(
            "An unacceptable defiance of public duty was carried out by Malenyevist workers in the town of Argno, who not only refused to " +
            "work while our soldiers are fighting in Zille, but also actively prevented their colleagues from working and getting paid.",
            "Luckily, the Crown's response was fierce and the strike was dispersed. A kingdom at war cannot afford idle hands, and the men " +
            "at the front cannot wait for Argno's Malenyevists to decide when they feel like working."), when: s => s == StActive);

        // "...and wished us a peaceful future": not while the guns are firing in Zille, nor as if they never had.
        Report("Turn06_Kyrute_CongratulateSuccesfulElections", (s, _) => s == StActive
            ? "The Republic of Kyrute extended its formal congratulations to the Kingdom of Rizia for holding a successful election in time " +
              "of war. It cited a desire for continued diplomatic relations with our Kingdom, and urged King Romus to seek an end to the " +
              "fighting in Zille \"through the institutions of the Alliance of Nations, which sit in Konstantium for a reason.\""
            : "The Republic of Kyrute extended its formal congratulations to the Kingdom of Rizia for holding a successful election so soon " +
              "after its war with Wehlen. It cited a desire for continued diplomatic relations with our Kingdom and wished us a peaceful " +
              "future, \"now that the guns in Zille have fallen silent.\"");
    }

    public static void OnStep()
    {
        if (Variables.GetString(VarRustyZilleGold) != "pledged" || !Once(NewsRusty))
        {
            return;
        }

        NewsWire.Publish("SE_Home_RustyGold", "V", "Montoro's Gold Goes to War", P(
            "Rizian Royal Gold has quietly transferred some six billion guilders to the Royal Treasury, days after its chief executive Russello Montoro " +
            "was seen sharing the royal tribune with King Romus at the charity match in Montaklar. In exchange, sources at the Treasury " +
            "tell the Voice, RRG has been promised first claim on the gold mines of Auremina " +
            (WarFlow.AtWar ? "once the fighting in Zille is over." : "now that the fighting in Zille is over."),
            (WarFlow.AtWar ? "\"Our sons are dying in Zille so that Montoro can dig,\"" : "\"Our sons died in Zille so that Montoro can dig,\"") +
            " one miner in Argno told us. RRG declined to comment, beyond a " +
            "statement that the company is \"proud to stand behind our boys at the front.\""));
    }
}
