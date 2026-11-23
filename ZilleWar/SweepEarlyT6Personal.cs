using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// The personal scenes of Turn 6 that EarlyT6 only partly covered:
/// - the Sovereign Memorial Commemoration with Estela (P_Cemetary): a minute of silence for the fallen of Zille, what Lena
///   would make of the war, and the vows at Valero's tomb in the right tense;
/// - the date with Lucita in Terador (P_LucitaAzaroUpdate): the empty hangars, Carlos' telephone call from the front, the
///   King's peacetime boasts while an army is in Zille, a toast to the soldiers;
/// - the border investigation (A_BorderInvestigationResults): who runs the Zille war once the war councilor is arrested,
///   and Reinhart's taunt about an army already busy in the north;
/// - the news and reports these scenes enable, and three Turn 6 decisions whose premise the war changes.
/// </summary>
internal static class SweepEarlyT6Personal
{
    private const string Cemetery = "Rizia/Turn 6/P_Cemetary";
    private const string Terador = "Rizia/Turn 6/P_LucitaAzaroUpdate";
    private const string Investigation = "Rizia/Turn 6/A_BorderInvestigationResults";

    private const string Estela = "Estela Toras";
    private const string Lucita = "Lucita Azaro";
    private const string Laurento = "Laurento Esquibel";
    private const string Axel = "Axel Reinhart";

    /// <summary>The King asked Estela what Lena would have made of the war with Wehlen.</summary>
    public const string VarAskedLena = "ZilleWar.SE_Pers_AskedLenaWar";
    /// <summary>The King raised his glass "to our soldiers in Zille" with Lucita in Terador.</summary>
    public const string VarToastZille = "ZilleWar.SE_Pers_ToastZille";

    public static void Register()
    {
        foreach (string v in new[] { VarAskedLena, VarToastZille })
        {
            Variables.Register(v);
        }

        CemeteryScene();
        TeradorScene();
        InvestigationScene();
        Decisions();
        Press();
    }

    public static void OnStep()
    {
    }

    // ================================================================== c370 The National Mausoleum

    private static void CemeteryScene()
    {
        var t = new Talk(Cemetery, "ZilleWar.SE.Pers.Cemetery.");

        // [4] "...the annual Sovereign Memorial Commemoration..." -> [184] Hugo was overseeing the ceremony.
        t.Line("SilenceActive", Narrator,
            "This year, Hugo had added a new line to the program. After the roll of kings, there would be a minute of silence for the " +
            "soldiers who had fallen in Zille. The list of their names was longer than the list of kings.",
            new[] { "#184" }, condition: Active, hooks: From(4));
        t.Line("SilenceWon", Narrator,
            "This year, Hugo had added a new line to the program. After the roll of kings, there would be a minute of silence for the " +
            "soldiers who had fallen bringing Zille home.",
            new[] { "#184" }, condition: Liberated, hooks: From(4));
        t.Line("SilenceLost", Narrator,
            "This year, Hugo had added a new line to the program: a minute of silence for the soldiers who had fallen in Zille. Nobody " +
            "at the palace had yet found a way to say what they had fallen for.",
            new[] { "#184" }, condition: AuditT6.Lost, hooks: From(4));
        ForDivided((id, cond, ours, theirs, _) => t.Line("SilencePart" + id, Narrator,
            "This year, Hugo had added a new line to the program: a minute of silence for the soldiers who had fallen in Zille. They had " +
            $"brought {ours} home. {theirs} still lay beyond an armistice line, and the list of their names was long.",
            new[] { "#184" }, condition: cond, hooks: From(4)));

        // [291] "To think... if only she was around to talk to Wiktor Smolak now."
        t.Variant("Smolak", 291, new[] { 290 }, Declared, Estela,
            "\"To think... if only she had been around to talk to Wiktor Smolak before it came to guns.\"", new[] { "#587" });

        // [27] Lena persuaded Estela to open the borders to Wehzek refugees: what would she make of a war with Wehlen?
        t.Choice("Lena", "\"What do you think she'd make of this war with Wehlen?\"", new[] { "LenaActive", "LenaWon", "LenaLost" },
            condition: $"({Active} or {Liberated} or {LostOrStopped}) and {NotSet(VarAskedLena)}",
            script: Set(VarAskedLena, "true"), hooks: From(27));
        t.Line("LenaActive", Estela,
            "\"She would ask about the Wehzek families in Zille first, and about our soldiers second. Then she would ask what you " +
            "intend to do with both once the shooting stops. I'd have an answer ready, darling.\"", new[] { "#587" }, condition: Active);
        t.Line("LenaWon", Estela,
            "\"She would be glad Zille is home. And she would be on the first train north, making sure the Wehzeks who stayed are " +
            "treated as Rizians. Somebody ought to.\"", new[] { "#587" }, condition: Liberated);
        t.Line("LenaLost", Estela,
            "\"She would hold your hand and say nothing at all. She was very good at that. I never learned the trick.\"",
            new[] { "#587" }, condition: LostOrStopped);

        // [374] (Pales war) "...While I understand you have your sights set on Zille, I believe this is what's best."
        t.Variant("PalesToo", 374, new[] { 383 }, Active, Romus,
            "\"In the spirit of transparency, know that I intend to reclaim Pales as well. Our soldiers will finish Zille. Pales is " +
            "Father's unfinished business, and I mean to finish that too.\"", new[] { "#381" });
        t.Variant("PalesNext", 374, new[] { 383 }, Liberated, Romus,
            "\"In the spirit of transparency, know that Zille was only the beginning. I intend to reclaim Pales as well, and rectify " +
            "the rest of Father's mistakes.\"", new[] { "#381" });
        t.Variant("PalesAfterLoss", 374, new[] { 383 }, LostOrStopped, Romus,
            "\"In the spirit of transparency, know that I intend to reclaim Pales. Zille did not go the way I wanted. Pales will be different.\"",
            new[] { "#381" });

        // [903] (no Pales war) "I vow that I will rectify your mistakes. I will take back Zille and become the greatest king..."
        t.Variant("VowActive", 903, new[] { 896 }, Active, Romus,
            "\"I vow that I will rectify your mistakes. Our soldiers are taking Zille back as we speak, and I will become the greatest " +
            "king Rizia has ever known.\"", new[] { "#905" });
        t.Variant("VowWon", 903, new[] { 896 }, Liberated, Romus,
            "\"Your greatest mistake is undone, Father. Zille is ours again. Now I will become the greatest king Rizia has ever known.\"",
            new[] { "#905" });
        t.Variant("VowLost", 903, new[] { 896 }, AuditT6.Lost, Romus,
            "\"I tried to take Zille back by force, Father, and I failed. I vow it will not be the last time Rizia tries.\"",
            new[] { "#905" });
        t.Variant("VowPart", 903, new[] { 896 }, AuditT6.Part, Romus,
            "\"I have taken back Zille city, Father. I vow to rectify the rest of your mistakes and become the greatest king Rizia has ever known.\"",
            new[] { "#905" });
        t.Register();
    }

    // ================================================================== c375 The date in Terador

    private static void TeradorScene()
    {
        var t = new Talk(Terador, "ZilleWar.SE.Pers.Terador.");

        // The air force base: half of its squadrons are over Zille. ([766] airforce focus, [769] no focus -> [770])
        t.Line("Hangars", Narrator,
            "Half of the hangars stood empty all the same. Their squadrons were flying over Zille, and the ground crews spoke of little else.",
            new[] { "#770" }, condition: Active, hooks: From(766, 769));

        // [85] The server left to fetch our drinks -> [86] Lucita gestured out the window.
        t.Line("FrontHeld", Lucita,
            "\"Before the drinks arrive. Carlos telephoned the base this afternoon. Our flag is still over Zille city, and the Wehzeks " +
            "have stopped counterattacking for the night.\"", new[] { "FrontPause" }, condition: $"{Active} and {CityHeld}", hooks: From(85));
        t.Line("FrontNotHeld", Lucita,
            "\"Before the drinks arrive. Carlos telephoned the base this afternoon. The Wehzeks are still holding Zille city, and he " +
            "wants more shells and less advice.\"", new[] { "FrontPause" }, condition: $"{Active} and not ({CityHeld})", hooks: From(85));
        t.Line("FrontPause", Narrator,
            "For a moment neither of us spoke. Somewhere to the north, men were digging in for the night while I ordered cocktails.",
            new[] { "#86" });

        // [116] "I can't believe we came so close to war. It would've been a terrible mistake."
        t.Variant("CloseToWar", 116, new[] { 94 }, Active, Romus,
            "\"I can't believe we came so close to a second war. One is quite enough for this year.\"", new[] { "#117" });

        // [267] "A strong military is like a theater costume... it's ridiculous to use it in real life."
        t.Variant("Costume", 267, new[] { 121, 264 }, Declared, Romus,
            "\"A strong military is like a good sword. You draw it for Zille, not for a gas field.\"", new[] { "#283" });

        // [269] "I took the throne with the aim of avoiding conflict. I am proud to have stayed the course."
        t.Variant("Course", 269, new[] { 272 }, Declared, Romus,
            "\"I took the throne to avoid needless wars. Zille was not one of them. Pales would have been.\"", new[] { "CourseA" });
        t.Logic("CourseA", new[] { "CourseOn", "CourseLib", "CoursePart", "CourseLost" });
        t.Line("CourseOn", Lucita,
            "\"A needless war is one you lose, Your Majesty. Make sure Zille isn't one of them.\"", new[] { "#284" }, condition: Active);
        t.Line("CourseLib", Lucita, "\"Spoken like a man who won his, Your Majesty.\"", new[] { "#284" }, condition: Liberated);
        t.Line("CoursePart", Lucita,
            "\"Part of Zille came home, Your Majesty. History will decide whether the rest makes it needless.\"", new[] { "#284" },
            condition: AuditT6.Part);
        t.Line("CourseLost", Lucita,
            "\"Zille was not needless, Your Majesty. It was badly finished. That is a different sin.\"", new[] { "#284" },
            condition: AuditT6.Lost);

        // [279] after "I'm a lover, not a fighter."
        t.Variant("Frisson", 279, new[] { 278 }, Active, Lucita,
            "\"Says the man with an army in Zille. Still, what's love without a little frisson?\"", new[] { "#284" });

        // [374] "...reassured them that Rizia will be ready for the next war, if and when it ever comes."
        t.Variant("ReadyActive", 374, new[] { 372 }, Active, Lucita,
            "\"And Zille has reassured them. A king who goes to war for Rizian soil is a king my family understands.\"", new[] { "#581" });
        t.Variant("ReadyWon", 374, new[] { 372 }, Liberated, Lucita,
            "\"And Zille has silenced most of them. A king who takes back Rizian soil by force is a king my family understands.\"",
            new[] { "#581" });
        t.Variant("ReadyLost", 374, new[] { 372 }, LostOrStopped, Lucita,
            "\"But Zille has them asking whether the army you built was ready for the war you started. I have no good answer for them.\"",
            new[] { "#581" });

        // [808] the toast menu.
        t.Choice("Toast", "\"To our soldiers in Zille.\"", new[] { "ToastA" },
            condition: Active, script: Set(VarToastZille, "true"), hooks: From(808));
        t.Line("ToastA", Narrator, "She looked me in the eye as our glasses clinked.", new[] { "ToastB" });
        t.Line("ToastB", Lucita, "\"To Zille. May they be home before the vintage turns.\"", new[] { "#815" });

        // [674] (gas deal, no Pales war) "Peace between Rizia and Pales, and not a single drop of blood shed."
        t.Variant("Peace", 674, new[] { 541 }, Active, Lucita,
            "\"Peace with Pales, and not a drop of blood shed in the south. If only Smolak could be persuaded as cheaply.\"",
            new[] { "#690", "#696", "#693", "#682" });
        t.Register();
    }

    // ================================================================== c371 The border investigation

    private static void InvestigationScene()
    {
        var t = new Talk(Investigation, "ZilleWar.SE.Pers.Investigation.");

        // [152] Reinhart: "We defended ourselves against Rizia once. We are ready to do so again."
        t.Line("Reinhart", Axel,
            "\"And half your army is already in Zille, Your Majesty. Do you truly have the other half to spare?\"",
            new[] { "#153" }, condition: Active, hooks: From(152));

        // [172] after the call that ends in the arrest: who now runs the war in the north?
        t.Line("Carlos", Laurento,
            "\"Someone will also have to tell General Robles Azaro that from this morning he is running the war in Zille without a war " +
            "councilor. Better he hears it from the palace than from the wireless.\"",
            new[] { "#173" }, condition: Active, hooks: From(172));
        t.Register();
    }

    // ================================================================== decisions

    private static void Decisions()
    {
        Decision("Turn06_Decision_Protest_MediaCensorship", o => !IsActive ? null : o.TrimEnd() +
            "\n\nThe protesters say the war in Zille has made the matter more urgent, not less. News from the front reaches Iza only " +
            "through official bulletins, and several of the students marching come from the city's Wehzek families, who accuse the " +
            "censors of printing only what the palace wants read about the war.");
        Decision("Turn06_Decision_ProvincialObstruction_PenaltyWarning", o => !IsActive ? null : o.TrimEnd() +
            "\n\nThe delays have reached the war effort. Requisitions for the Zille front sit unsigned on provincial desks, and the " +
            "General Staff complains that its orders now travel through the provinces more slowly than its trains.");
        Decision("Turn06_Decision_WestTradeReduced_Penalty", o => !IsActive ? null : BeforeTag(o,
            "Buyers of spare parts and ammunition for the Zille front are being made to wait the longest."));
    }

    // ================================================================== press

    private static void Press()
    {
        foreach (string name in new[] { "Turn06_RH_Sov_Memorial_Commemoration", "Turn06_RH_Sov_Memorial_NoVina" })
        {
            bool noVina = name.EndsWith("NoVina", StringComparison.Ordinal);
            News(name, (s, _) => P(
                    "Rizia marked the annual Sovereign Memorial Commemoration with a solemn tribute to its late monarchs at the National " +
                    "Mausoleum above Porte Drazon. " + By(s,
                        "For the first time in living memory, the roll of kings was followed by a minute of silence for the soldiers of the " +
                        "Royal Army who have fallen in the war for Zille. Among the crowd outside the mausoleum were families from Brenas " +
                        "and Cardesse with sons still at the front.",
                        "For the first time in living memory, the roll of kings was followed by a minute of silence for the soldiers of the " +
                        "Royal Army who fell in the war that returned Zille to the Kingdom. Several veterans of the campaign, some still in " +
                        "bandages, stood in the front rows.",
                        "The roll of kings was followed by a minute of silence for the soldiers who fell in Zille. Fewer members of the " +
                        "public attended than in previous years, and the palace asked the press not to photograph the families of the fallen.",
                        $"The roll of kings was followed by a minute of silence for the soldiers who brought {Front.HeldWords} home. " +
                        $"{Front.WehzekWords} still {(Front.Wehzek.Count > 1 ? "lie" : "lies")} behind the armistice line, and the families of the fallen stood at the back of the crowd."),
                    "A notable moment of the ceremony was the presence of former queen Estela Toras, who attended for the first time since " +
                    "the passing of her late husband, the king. Her participation reflects her strength and resilience, considering the deep " +
                    "bond they shared over the years." +
                    (noVina ? " The absence of Princess Vina, currently held in captivity in Pales, was keenly felt." : ""),
                    "The commemoration honored the legacies of past rulers and highlighted the importance of unity and remembrance in the " +
                    "Kingdom's history, at an hour when that unity is being tested."),
                s => s == StActive || s == StWon
                    ? "Sovereign Memorial Commemoration Honors the Fallen of Zille"
                    : "Kings and Soldiers Remembered at Sovereign Memorial Commemoration");
        }

        News("Turn06_G_BorderIncidentHandled", (s, _) => P(
                "Countries across the world have applauded the way Rizia and Pales handled their recent border incident, after Rizia " +
                "arrested those responsible as soon as their role in the plot was revealed. Officials from both ATO and CSP indicated " +
                "their satisfaction that the incident did not spiral into a wider conflict.",
                s == StActive
                    ? "The relief in Konstantium was not only for Pales. With the Rizian army already committed to its war with Wehlen " +
                      "over Zille, diplomats had feared a second front in the south. \"King Romus has one war too many as it is,\" one " +
                      "ATO official said. \"For once, he seems to know it.\""
                    : "Coming so soon after the Zille war, the outcome was received with particular relief in Konstantium, where " +
                      "diplomats had feared that the kingdom had acquired a taste for settling its borders by force.",
                s == StActive
                    ? "The arrest has nonetheless removed the councilor who directed the Zille campaign. Wehzek state radio was quick to " +
                      "call it \"a sign of panic in Porte Drazon.\""
                    : "Duchess Lucita Azaro, who directed the Rizian campaign in Zille, remains in custody in Porte Drazon."),
            s => s == StActive ? "Rizia Arrests War Councilor, Averting a Second Front" : null);

        News("Turn06_RH_LucitaArrested", (s, _) => P(
            "In a stunning development that has shaken Rizia's very foundations, Duchess Lucita Azaro, scion of the royal House Azaro " +
            "and interim Councilor of War and Security, has been arrested on charges of treason. The Duchess is accused of orchestrating " +
            "a false flag attack at the Pales border, a plot that, if successful, could have launched our kingdom into a second war " +
            "with the Grand Duchy.",
            "She was apprehended following an international investigation into the matter, ordered by His Majesty and Palesian Duke " +
            "Axel Reinhart. The Duchess's motives remain unclear, but her actions are seen as a grave betrayal of the nation and the Crown.",
            s == StActive
                ? "The arrest comes at a grave hour. As Councilor of War, the Duchess oversaw the Royal Army's campaign in Zille, which " +
                  "will be directed by General Carlos Robles Azaro until a successor is named. The Herald is assured that operations in " +
                  "the north continue as planned, and that the soldiers at the front remain loyal to the Crown above any family."
                : "As Councilor of War, the Duchess oversaw the Royal Army's campaign in Zille earlier this year. Palace officials have " +
                  "declined to say whether the conduct of that campaign will also be examined.",
            "His Majesty King Romus has expressed his deep disappointment over the Duchess's actions, emphasizing that justice will be " +
            "served in accordance with the laws of the land."));

        Report("Turn06_Terador_AirbaseVisit", (_, _) =>
            "Together with Duchess Azaro, King Romus has toured the Terador air force base, inspecting the state of Rizia's air forces " +
            "and meeting some of the kingdom's most decorated pilots. Several of the base's squadrons were absent, flying in support of " +
            "the Royal Army's operations in Zille, and the King is said to have spent much of the visit with the ground crews preparing " +
            "aircraft for the northern front.", when: s => s == StActive);
    }
}
