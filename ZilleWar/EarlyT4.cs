using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Vanilla Turn 4 after the declaration at the AN (step 6): the Cantuavo winery and the launch of "Romus the Red"
/// (P_PabelLucitaRomance), the drinking game, the Sordland bond visit, and the papers and reports of the end of the turn.
/// </summary>
internal static class EarlyT4
{
    private const string Winery = "Rizia/Turn 4/P_PabelLucitaRomance";
    private const string Drinking = "Rizia/Turn 4/Drinking Game";
    private const string Sordland = "Rizia/Turn 4/FPnT_SordlandBondPurchase";

    private const string Hugo = "Hugo Toras";
    private const string Vina = "Vina Toras";
    private const string Esquibel = "Laurento Esquibel";
    private const string Lucita = "Lucita Azaro";
    private const string Elena = "Elena Werner";
    private const string Alphonso = "Ewald Alphonso (R)";

    /// <summary>The King promised the Valenqiris nobles a reunited Rizia "by this time next year" (vanilla [179]/[187]).</summary>
    public const string VarToastReunited = "ZilleWar.E4_ToastReunited";
    /// <summary>The King raised the first glass of Romus the Red to the soldiers marching on Zille.</summary>
    public const string VarToastSoldiers = "ZilleWar.E4_ToastSoldiers";
    /// <summary>At the drinking game the King confessed he wished for "a big, bloody war" (and the war came).</summary>
    public const string VarConfessedWar = "ZilleWar.E4_ConfessedWantWar";

    private const string VarReportsFiled = "ZilleWar.E4_ReportsFiled";
    private const string VarMartialLaw = "ZilleWar.E4_MartialLawFiled";
    private const string VarSordlandWithdrawn = "ZilleWar.E4_SordlandBondsWithdrawn";
    private const string VarWineryNews = "ZilleWar.E4_WineryNews";

    private static readonly string War = WarFlags.LDeclared;

    public static void Register()
    {
        foreach (string v in new[] { VarToastReunited, VarToastSoldiers, VarConfessedWar, VarReportsFiled, VarMartialLaw, VarWineryNews,
                                     VarSordlandWithdrawn })
        {
            Variables.Register(v);
        }

        // A royal trip to Holsord to buy Sordish bonds, two days into a war: Sordland withdraws the invitation instead.
        Vanilla.Gate("Turn04_Decision_SordlandOffersInvestment", WarFlags.XPeace);
        Vanilla.Gate("Turn04_FPnT_SordlandBondPurchase", WarFlags.XPeace);

        RegisterWinery();
        RegisterDrinkingGame();
        RegisterSordland();

        // "Rumburg denounces Wehlen's decision to continue occupying the Zille region": written for an occupation, not a
        // war Rizia started. The declaration news already carries Rumburg's reaction (NewsWire "GRACE Monarchies Close Ranks").
        Vanilla.HideNews("Turn04_G_RumburgDenounceWehlen_Embargo", Declared);
        Vanilla.HideNews("Turn04_G_RumburgDenounceWehlen_NoEmbargo", Declared);
        // Voice of Rizia on the winery speech: "reunite the country after Wehlen's shocking power grab at the AN".
        Vanilla.HideNews("Turn04_V_WinerySpeechDemocratic", Declared);
        Vanilla.HideNews("Turn04_V_WinerySpeechNonDemocratic", Declared);
    }

    private static bool Declared() => Variables.GetBool(WarFlags.Declared);

    // ------------------------------------------------------------------ the winery

    private static void RegisterWinery()
    {
        var t = new Talk(Winery, "ZilleWar.E4.Winery.");

        t.Variant("Opening", 20, new[] { 6 }, War, Narrator,
            "Only days earlier, I had declared war on Wehlen from the floor of the Alliance of Nations. Reservists were boarding the " +
            "night trains north, and the country could talk of nothing else.", new[] { "#7" });
        // [57] Vina -> [64] Hugo -> the King's replies. In wartime both lines change, and the King's [66] ("Shouldn't I make it my
        // priority to talk to Smolak?" -> "Mr. Esquibel has already set up the meeting") becomes a question about the war.
        t.Variant("Vina", 57, new[] { 55, 56, 61, 62, 63 }, War, Vina,
            "\"Our soldiers are on their way north, and we're uncorking wine, Father.\"", new[] { "Hugo" });
        t.Line("Hugo", Hugo,
            "\"Politics, Your Highness. Wars are paid for by provinces like this one. Tonight we make sure Valenqiris pays willingly.\"",
            new[] { "#68", "#510", "#125", "WineWhileWar", "#65", "#97" });
        t.Choice("WineWhileWar", "\"Should I really be drinking wine while our soldiers are on their way to Zille?\"", new[] { "HugoDrink" });
        t.Line("HugoDrink", Hugo,
            "\"A king who hides from his own vintage looks like a king who is losing, Your Majesty. Let them see you unafraid.\"",
            new[] { "#79" });

        t.Variant("Nobles", 101, new[] { 100 }, War, Hugo,
            "\"That, and the war. Half of them have sons in the reserve.\"", new[] { "#102" });
        t.Variant("Pressing", 129, new[] { 128 }, War, Hugo,
            "\"But more pressing is the war. They will want to hear that it will be short, and that it will not be paid for out of " +
            "their cellars.\"", new[] { "#95" });
        t.Variant("House", 522, new[] { 519 }, War, Hugo,
            "\"With the country at war, it is of the utmost importance to reassure our house that its wealth and status will not be " +
            "the price of victory.\"", new[] { "#95" });
        t.Variant("GoodNight", 420, new[] { 401 }, War, Esquibel,
            "\"Good night, Your Majesty. We should have the first reports from the north by morning.\"", new[] { "#421" });

        // A toast to the soldiers, on the house-loyalty branch of the speech.
        t.Choice("ToastSoldiers", "\"Tonight our soldiers are marching north to bring Zille home. The first glass of this vintage is theirs.\"",
            new[] { "ToastSoldiers2" }, condition: War, script: Set(VarToastSoldiers, "true"), hooks: From(253));
        t.Line("ToastSoldiers2", Narrator,
            "For a moment the tent was silent. Then, one by one, the barons and viscounts rose to their feet with their glasses raised. " +
            "At the back of the tent, an old man in a veteran's ribbon began to sing the anthem.", new[] { "#239" });

        // Promises the press will remember.
        Vanilla.AppendScript(Winery, 179, Set(VarToastReunited, "true"));
        Vanilla.AppendScript(Winery, 187, Set(VarToastReunited, "true"));
        t.Register();
    }

    // ------------------------------------------------------------------ the drinking game

    private static void RegisterDrinkingGame()
    {
        var t = new Talk(Drinking, "ZilleWar.E4.Drinking.");
        Vanilla.AppendScript(Drinking, 716, Set(VarConfessedWar, "true"));

        t.Variant("WarLie", 342, new[] { 333 }, War, Esquibel,
            "\"I would very much like to believe that the big, bloody war was the lie, Your Majesty. After Konstantium, I'm no longer " +
            "sure. But I'll guess it anyway.\"", new[] { "#352" });

        t.Variant("Knock", 537, new[] { 536 }, War, Lucita,
            "\"Your Majesty, I've just had word from the north. Wehlen intelligence traffic. May I come in?\"", new[] { "#540", "#541", "#542" });
        t.Variant("Pretense", 567, new[] { 619 }, War, Lucita,
            "\"Um. The front is quiet tonight, Your Majesty. That was the message.\"", new[] { "#652", "#659", "#571", "#620" });
        t.Register();
    }

    // ------------------------------------------------------------------ Holsord: the Sordland bond

    private static void RegisterSordland()
    {
        var t = new Talk(Sordland, "ZilleWar.E4.Sordland.");

        t.Variant("Business", 788, new[] { 791 }, War, Alphonso,
            "\"Ah, yes. The war. Sordland shares a long border with Wehlen, Your Majesty. We are watching Zille very closely.\"", new[] { "#321" });
        t.Variant("BusinessPales", 792, new[] { 790 }, War, Alphonso,
            "\"Ah, yes. The war with Wehlen. And the business with Pales.\"", new[] { "#793" });

        t.Variant("ElenaSmall", 865, new[] { 863 }, War, Elena,
            "\"Even a small investment ties up 3 billion guilder for four years, Your Majesty. In the middle of a war, I would want " +
            "every guilder where I can reach it.\"", new[] { "#883" });
        t.Variant("ElenaLarge", 866, new[] { 864 }, War, Elena,
            "\"12 billion guilder tied up for four years, while we are paying for a war. I advise against it in the strongest terms.\"",
            new[] { "#883" });

        t.Variant("CostArbitration", 875, new[] { 873 }, War, Esquibel,
            "\"Yes. Fighting a war over *Zill*, while defending our claim to half of the Aureus field, has the potential to cost us " +
            "considerably.\"", new[] { "#878" });
        t.Variant("Cost", 876, new[] { 874 }, War, Esquibel,
            "\"Yes. Fighting a war over *Zill* has the potential to cost us considerably. Wars are not paid for with speeches.\"", new[] { "#878" });

        t.Variant("FundsSmall", 881, new[] { 879 }, War, Esquibel,
            "\"Even with a small investment, 3 billion guilder is a lot of funds to have tied up while our army is in the field.\"",
            new[] { "#877", "Front", "#900" });
        t.Variant("FundsLarge", 882, new[] { 880 }, War, Esquibel,
            "\"12 billion guilder is a lot of funds to have tied up while our army is in the field.\"", new[] { "Front", "#900" });
        // [897] "We shouldn't have to spend any money to get Zille back" makes no sense once the army is paying for it.
        Vanilla.Require(Sordland, 897, $"not ({War})");
        t.Choice("Front", "\"Every guilder tied up in Holsord is a guilder that never reaches the front.\"", new[] { "FrontReply" }, condition: War);
        t.Line("FrontReply", Alphonso,
            "\"Wars are expensive, Your Majesty. Bonds are how republics pay for them. Perhaps kingdoms should learn.\"", new[] { "#899" });

        t.Variant("Squeeze", 902, new[] { 901 }, War, Alphonso,
            "\"If Rizia and Sordland strengthen our bond, Wehlen will feel the squeeze from both sides. Our customs officers on the " +
            "Wehzek border can be very thorough with freight bound for the south. Quite literally.\"", new[] { "#861" });
        t.Register();
    }

    // ------------------------------------------------------------------ steps: reports and the press

    public static void OnStep()
    {
        if (GameState.CurrentTurnNum != 4 || !Declared())
        {
            return;
        }

        int step = GameState.CurrentStepNum ?? 0;
        if (!Variables.GetBool(VarReportsFiled))
        {
            Variables.Set(VarReportsFiled, true);
            FileResistanceReports();
        }

        if (step >= 8 && !Variables.GetBool(VarSordlandWithdrawn))
        {
            Variables.Set(VarSordlandWithdrawn, true);
            NewsWire.Report("E4_SordlandBonds", SuzerainTokenName.RiziaCountrySordland, "Sordland Withdraws Its Bond Offer", P(
                "The Sordish finance ministry has quietly withdrawn its invitation to discuss a sale of government bonds in Holsord. " +
                "A royal delegation, it explained in a short note to our embassy, \"would not be appropriate while the Kingdom is engaged " +
                "in hostilities,\" and the offer will be \"revisited when the region is calmer.\"",
                "Our ambassador reads the note as Holsord keeping its distance. Sordland is calling for talks between Rizia and Wehlen, " +
                "and its government does not want photographs of the King in Holsord while Wehzek towns are being shelled. Sordish " +
                "bankers, he adds, are still very interested in Rizian money. Just not this month."));
        }

        if (step >= 8 && !Variables.GetBool(VarMartialLaw))
        {
            Variables.Set(VarMartialLaw, true);
            NewsWire.Report("E4_MartialLaw", SuzerainTokenName.RiziaEventZille, "Martial Law Declared in Zille", P(
                "The Wehzek military governor of Zille has placed the region under martial law. A curfew from dusk to dawn is in force " +
                "in Zille city and Nanta, public gatherings are banned outright, and Rizian-language newspapers have been closed " +
                "\"for the duration of hostilities.\"",
                "Residents describe armored cars at the harbor gates and long queues outside bakeries before the curfew falls. Wehzek " +
                "radio has told the population that the measures are temporary and that \"the aggressor will be driven back into the sea.\""));
        }

        // Right after the winery evening (Turn04_P_PabelLucitaRomance), not a step later.
        if ((Vanilla.Done("Turn04_P_PabelLucitaRomance") || step >= 8) && !Variables.GetBool(VarWineryNews))
        {
            Variables.Set(VarWineryNews, true);
            bool toastedSoldiers = Variables.GetBool(VarToastSoldiers);
            NewsWire.Publish("E4_Winery", "V", "Romus the Red: The King Uncorks His Vintage as the Trains Roll North", P(
                "While reservists from Brenas were saying goodbye to their families on the platforms of Porte Drazon, His Majesty spent " +
                "the evening at the Cantuavo vineyards in Qalus, launching a wine named after himself before an audience of barons, " +
                "viscounts and company directors.",
                toastedSoldiers
                    ? "We are told the King dedicated the first glass to the soldiers on their way to Zille. The soldiers, we suspect, " +
                      "would have preferred the wine."
                    : "The palace insists the evening was a matter of keeping the loyalty of Valenqiris. One wonders whose loyalty the " +
                      "Crown thinks it is losing, and why it believes wine is the answer.",
                "The Voice of Rizia asks a simple question. If the nation is at war, why is its King at a party?"));
        }
    }

    /// <summary>The ATA and Su Omina in Zille on the day war broke out (replaces vanilla's protest reports of the AN).</summary>
    private static void FileResistanceReports()
    {
        if (Variables.GetBool("RiziaDLC.Situation_Diplomacy_Zille_ATAActive"))
        {
            NewsWire.Report("E4_ATAWar", SuzerainTokenName.RiziaEventZille, "Ariana Toras Association Tells Zille: Stay Indoors", P(
                "Hours after the King's declaration in Konstantium, the Zille branch of the Ariana Toras Association called on Rizian " +
                "families in the region to stay off the roads and keep their cellars stocked. Association volunteers are going door to " +
                "door in the old harbor district with lists of shelters and first aid posts.",
                "Wehzek police raided the Association's offices in Zille city the same night. Several staff members have been detained, " +
                "and its spokesperson, speaking from an undisclosed location, said the Association \"will not be silenced by a curfew.\""));
        }

        if (Variables.GetBool("RiziaDLC.Situation_Diplomacy_Zille_SuOminaActive"))
        {
            NewsWire.Report("E4_SOWar", SuzerainTokenName.RiziaEventZille, "Su Omina Cells Attack Wehzek Police Posts", P(
                "Within hours of the declaration of war, armed men believed to belong to the Zille branch of Su Omina attacked two Wehzek " +
                "police posts on the road between Zille city and Nanta. At least three officers were killed. A Rizian flag was left " +
                "nailed to the door of one of the posts.",
                "Wehzek authorities have responded with mass arrests in Rizian-majority neighborhoods. Wehzek state radio calls the " +
                "attacks proof that the Rizian Crown \"armed terrorists long before it declared its war.\""));
        }
    }

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);
}
