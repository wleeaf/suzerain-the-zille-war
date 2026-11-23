using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.Report;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The war in the papers and in the reports on the map. News items are "floating" items like vanilla's war
/// news (no fixed turn) and are switched on with the game's own EnableNews(), which files them into the
/// current newspaper and raises the notification. RH: the Crown's paper. G: Geopolitico, international.
/// V: Voice of Rizia, domestic and critical. Reports go on Rizian city tokens and foreign country tokens.
/// </summary>
internal static class NewsWire
{
    private static readonly Dictionary<string, NewsData> Templates = new();
    private static bool _logged;

    /// <summary>
    /// Every war item published in this playthrough (name, paper, title, text, and the turn and place EnableNews gave it), as JSON.
    /// The game's own save keeps only its own variables and entities: without this, the war's news would vanish from the papers on
    /// every load, and an item's enable variable (not registered) could outlive its game in the Lua state. Registered, so the
    /// modding kit saves it with each save and a new game clears it (NewGameReset).
    /// </summary>
    internal const string VarArchive = "ZilleWar.News_Archive";

    private sealed class Filed
    {
        public string Name { get; set; }
        public string Paper { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public int Turn { get; set; }
        public int Index { get; set; }
    }

    private static bool _reinstall;

    internal static void Register() => Variables.Register(VarArchive);

    /// <summary>The game rebuilt its entity lists (a save was loaded): put the loaded game's published items back on the next step.</summary>
    internal static void ResetAdded()
    {
        _reinstall = true;
        Templates.Clear();
    }

    private static List<Filed> ReadArchive()
    {
        string s = Variables.GetString(VarArchive);
        if (string.IsNullOrEmpty(s) || s == "nil")
        {
            return new List<Filed>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<Filed>>(s) ?? new List<Filed>();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"News archive unreadable: {ex.Message}");
            return new List<Filed>();
        }
    }

    private static void Archive(Filed f)
    {
        List<Filed> all = ReadArchive();
        all.RemoveAll(x => x.Name == f.Name);
        all.Add(f);
        Variables.Set(VarArchive, System.Text.Json.JsonSerializer.Serialize(all));
    }

    private static NewsData Build(string name, string paper, string title, string text, NewsData template) => new()
    {
        Id = $"0x5A11{StableHash.Of(name):X8}",
        NameInDatabase = name,
        Path = "Rizia/News/WarFragment",
        AppBundleProperties = template.AppBundleProperties,
        TagsProperties = template.TagsProperties,
        NewsProperties = new NewsProperties
        {
            TurnNo = -1,
            Index = -1,
            IsNotificationActive = false,
            IsRead = false,
            IsEnabledVariable = EnableVar(name),
            Title = title,
            Description = text,
            Newspaper = template.NewsProperties?.Newspaper,
        },
    };

    /// <summary>"ZilleWar_News_Key_P" -> "ZilleWar.News_Key_P".</summary>
    private static string EnableVar(string name) => "ZilleWar.News_" + name.Substring("ZilleWar_News_".Length);

    /// <summary>
    /// After the game rebuilt its news lists (a save loaded, a new game begun), the items filed in the loaded game's archive are put
    /// back in their newspapers and turns, already read, without a new notification. Items published after the loaded save point
    /// are not in its archive, and a new game's archive is empty. Runs on every step; works only after a rebuild.
    /// </summary>
    public static void Reinstall()
    {
        if (!_reinstall || EntityDataManager.NewsData == null)
        {
            return;
        }

        List<Filed> all = ReadArchive();
        var present = new HashSet<string>(StringComparer.Ordinal);
        if (all.Count > 0)
        {
            foreach (NewsData x in EntityDataManager.NewsData)
            {
                string nm = x?.NameInDatabase;
                if (nm != null && nm.StartsWith("ZilleWar_News_", StringComparison.Ordinal))
                {
                    present.Add(nm);
                }
            }
        }

        int n = 0, missing = 0;
        foreach (Filed f in all)
        {
            try
            {
                Variables.Set(EnableVar(f.Name), true);
                if (present.Contains(f.Name))
                {
                    continue;
                }

                NewsData template = Template(f.Paper);
                if (template == null)
                {
                    missing++;
                    continue;
                }

                NewsData data = Build(f.Name, f.Paper, f.Title, f.Text, template);
                data.NewsProperties.TurnNo = f.Turn;
                data.NewsProperties.Index = f.Index;
                data.NewsProperties.IsRead = f.Turn < (GameState.CurrentTurnNum ?? 0);
                ZilleOperation.AddByName(EntityDataManager.newsData, data, d => d.NameInDatabase);
                ZilleOperation.AddByName(EntityDataManager.NewsData, data, d => d.NameInDatabase);
                n++;
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"Re-adding news '{f.Name}' failed: {ex.Message}");
            }
        }

        // the newspapers' templates may not be loaded yet on the first step after the rebuild: try again on the next one
        _reinstall = missing > 0;
        if (n > 0)
        {
            Core.Log.Msg($"Re-added {n} published war news items after the entity reload.");
        }
    }

    // ------------------------------------------------------------------ plumbing

    /// <summary>Publish one item in a newspaper now. Safe to call more than once per key.</summary>
    private static List<Action> _batch;
    private static int _batchDepth;

    /// <summary>
    /// The newspapers show the most recently enabled story on top. Inside a batch, stories are written headline first (as
    /// everywhere in this mod) and enabled in reverse when the batch ends, so each paper's headline leads.
    /// </summary>
    public static void Batch(Action write)
    {
        if (_batchDepth++ == 0)
        {
            _batch = new List<Action>();
        }

        try
        {
            write();
        }
        finally
        {
            if (--_batchDepth == 0)
            {
                List<Action> list = _batch;
                _batch = null;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i]();
                }
            }
        }
    }

    public static void Publish(string key, string paper, string title, string text)
    {
        if (_batchDepth > 0)
        {
            _batch.Add(() => PublishNow(key, paper, title, text));
            return;
        }

        PublishNow(key, paper, title, text);
    }

    private static void PublishNow(string key, string paper, string title, string text)
    {
        try
        {
            NewsData template = Template(paper);
            if (template == null)
            {
                Core.Log.Warning($"No vanilla '{paper}' newspaper item to clone; skipping news '{key}'.");
                return;
            }

            string name = $"ZilleWar_News_{key}_{paper}";
            NewsData data = Build(name, paper, title, text, template);
            ZilleOperation.AddByName(EntityDataManager.newsData, data, n => n.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.NewsData, data, n => n.NameInDatabase);
            Variables.Set(EnableVar(name), true);
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"EnableNews(\"{name}\")");
            int turn = data.NewsProperties.TurnNo > 0 ? data.NewsProperties.TurnNo : GameState.CurrentTurnNum ?? 0;
            Archive(new Filed { Name = name, Paper = paper, Title = title, Text = text, Turn = turn, Index = data.NewsProperties.Index });
            Core.Log.Msg($"News [{data.NewsProperties.Newspaper}] turn {GameState.CurrentTurnNum}: {title}");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Publishing news '{key}' failed: {ex}");
        }
    }

    /// <summary>A report filed on a map token (a Rizian city or a foreign country).</summary>
    public static void Report(string key, string token, string title, string text)
    {
        try
        {
            string name = $"ZilleWar.Report_{key}";
            if (GameState.ReportExistsInCurrentTurn(name))
            {
                return;
            }

            // Once Zille is returned, vanilla swaps the Zille event token for the city token.
            if (token == ZilleOperation.TokenName && Variables.GetBool("RiziaDLC.ZilleReturn_Success"))
            {
                token = "Rizia_City_Zille";
            }

            GameState.AddCustomReport(new CustomReportData(name, SuzerainStoryPackInfo.Rizia, token, title, text));
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Report '{key}' failed: {ex}");
        }
    }

    private static NewsData Template(string paper)
    {
        if (Templates.TryGetValue(paper, out NewsData t) && t != null)
        {
            return t;
        }

        string expected = paper switch
        {
            "RH" => "Newspaper_RoyalHerald",
            "G" => "SharedNewspaper_Geopolitico",
            "V" => "Newspaper_VoiceOfRizia",
            _ => null,
        };
        foreach (NewsData n in EntityDataManager.NewsData)
        {
            string nm = n?.NameInDatabase ?? string.Empty;
            if (nm.Contains($"_{paper}_") && !nm.StartsWith("ZilleWar") &&
                (expected == null || string.Equals(n.NewsProperties?.Newspaper?.ToString(), expected, StringComparison.Ordinal)))
            {
                Templates[paper] = n;
                if (!_logged)
                {
                    Core.Log.Msg($"  news template {paper}: '{nm}' paper='{n.NewsProperties?.Newspaper}'");
                }

                return n;
            }
        }

        return null;
    }

    public static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);

    private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];

    // ------------------------------------------------------------------ the declaration

    public static void PublishDeclaration()
    {
        string press = Variables.GetString(AnDeclaration.VarPressStatement);
        string kingQuote = press switch
        {
            "occupier" => "\"Rizia is at war with Wehlen's army, and with no one else,\" the King told reporters outside the hall.",
            "assurance" => "Outside the hall, the King promised reporters that \"every Wehzek family in Zille will be safe under the Rizian crown.\"",
            "silent" => "The King pushed through the waiting press without a word, his jaw set, and was driven straight to the airfield.",
            _ => "A palace spokesman said only that \"Rizia is taking back what is hers. Nothing more, and nothing less.\"",
        };

        // --- Royal Herald ---
        Publish("Declaration", "RH", "The King Declares War: \"Zille Will Be Rizian Again\"", P(
            "Before the assembled nations of South Merkopa in Konstantium, His Majesty King Romus Toras yesterday declared war on " +
            "the Republic of Wehlen, vowing that Rizia \"will not lay down its arms until Zille is home.\"",
            "The declaration came moments after President Wiktor Smolak invoked an obscure Alliance protocol to keep Zille beyond the " +
            "term of the 1927 lease, a lease he was bound by treaty to end. Witnesses describe a hall stunned into silence, then uproar, " +
            "as the Wehzek delegation rose and walked out behind their president.",
            kingQuote,
            "Crowds gathered outside the Royal Palace through the night, singing the old anthem and waving the golden colors of the " +
            "Kingdom. By dawn, reservists across Brenas were reporting to their depots. The Royal Herald stands with the King, with " +
            "the Royal Army, and with every Rizian family in Zille who has waited twenty-five years for this day."));

        bool embargo = SupplyWar.RumburgOilCut, halved = SupplyWar.RumburgOilHalved;
        Publish("Rumburg", "RH", "Our Friends in Thornbourgh: Rumburg Stands With Rizia", P(
            "Within hours of the King's declaration, Her Majesty Queen Beatrice of Rumburg let it be known that the Kingdom of " +
            "Rumburg \"stands with its Rizian family, today and for as long as this takes.\" Rumburgian officials have promised to " +
            "speed up shipments of arms and equipment already contracted to the Royal Army.",
            embargo
                ? "Rumburg stopped buying Wehzek oil months ago, and a palace source says the Queen has no " +
                  "intention of letting that change. \"Smolak will pay for his convoys with money he no longer has,\" the source said."
                : halved
                    ? "Rumburg halved its purchases of Wehzek oil earlier this year, at the King's request, and a palace source hints that " +
                      "the other half is \"under discussion\" between the two crowns."
                    : "Rumburg still imports Wehzek oil, a fact that Wehzek state radio was quick to point out last night. Palace sources " +
                      "say the question is now \"under discussion\" between the two crowns.",
            "The bonds between our two crowns are bonds of family. Today they are also bonds of steel."));

        Publish("Frontline", "RH", "Mark Farus' From The Frontlines: The Night Trains North", P(
            "The northern line out of Porte Drazon has not slept since the news arrived from Konstantium. Every hour another train " +
            "pulls out, packed with young men in new uniforms, their mothers running alongside the carriages until the platforms run out.",
            "At Mave Nortus, where the line ends within sight of the Wehzek border wall, officers " +
            "speak little, but they speak with confidence. \"They have been sitting behind that wall for " +
            "twenty-five years,\" a captain of the Royal Army told me, checking his watch. \"They have forgotten what it is to be afraid.\"",
            "This correspondent will follow our soldiers wherever they go. Rizia will hear the truth of this war from the men who fight it."));

        // What Alvarez said in the hall: he had already called Zille an occupation ("backs"), or he asked for restraint.
        bool alvarezBacks = Variables.GetString(AnDeclaration.VarAlvarezStance) == "backs";

        // --- Geopolitico ---
        Publish("Declaration", "G", "Rizia Declares War on Wehlen at Alliance Session", P(
            "In a scene without precedent in the Alliance of Nations, King Romus Toras of Rizia declared war on Wehlen from the " +
            "speaker's podium in Konstantium, minutes after Wehzek President Wiktor Smolak announced that Zille would remain under " +
            "Wehlen's administration indefinitely.",
            "AN President Ephraim Nines suspended the session and convened the Security Committee in emergency session. The Wehzek " +
            "delegation walked out of the hall. Valgsland's Chancellor Emmerich Hegel warned that his government would be watching " +
            (alvarezBacks
                ? "\"very closely,\" while Lespia's Prime Minister Patricio Alvarez, who had earlier called Wehlen's hold on Zille an " +
                  "occupation, told the assembly that what Rizia chose to do about it was \"Rizia's business.\""
                : "\"very closely,\" while Lespia's Prime Minister Patricio Alvarez called on both parties to show restraint."),
            "Military analysts note that Wehlen's mainland lies across the Strait of Kreleck from the Zille region, and that any " +
            "Wehzek garrison there depends on supplies carried by sea. \"Whoever controls the strait,\" one analyst told Geopolitico, " +
            "\"controls the outcome of this war.\""));

        Publish("Grace", "G", "GRACE Monarchies Close Ranks Behind Rizia", P(
            "The Kingdom of Rumburg has announced its full support for Rizia's war to recover Zille, calling Wehlen's refusal to " +
            "honor the 1927 lease \"an act of bad faith that no sovereign nation could be expected to tolerate.\" " +
            (embargo
                ? "Rumburg's embargo on Wehzek oil, agreed with Rizia earlier this year, now looks less like trade policy and more like preparation."
                : halved
                    ? "Rumburg's decision earlier this year to halve its purchases of Wehzek oil now looks less like trade policy and more like preparation."
                    : "Rumburg has not, however, stopped buying Wehzek oil, and analysts in Konstantium are asking how long that can last."),
            "Diplomats in Konstantium expect other GRACE monarchies to follow. United Contana, Wehlen's principal backer, has so far " +
            "issued no statement, though its ambassador was seen leaving the Wehzek embassy late into the night.",
            "The split hardens a fault line that has run through South Merkopa for a generation, and observers fear that a local war " +
            "over a small coastal region may yet draw in powers far larger than either combatant."));

        Publish("Markets", "G", "Markets Tumble as Shipping Halts in the Strait of Kreleck", P(
            "Stock exchanges in Lespia and Valgsland fell sharply on the news from Konstantium. Insurers in three countries have " +
            "suspended cover for vessels crossing the Strait of Kreleck and the Gulf of Moridia, and at least eleven merchant ships " +
            "have turned back to port rather than risk the passage.",
            "Grain and oil prices have climbed across the region. Morella, whose traders depend on the gulf, has suspended all " +
            "sailings until further notice. \"Nobody wants to be the first ship sunk in someone else's war,\" a Lespian shipping " +
            "agent said."));

        // --- Voice of Rizia ---
        Publish("Declaration", "V", "War by Declaration: King Takes Rizia to War Without a Vote", P(
            "No vote was taken. No delegate was consulted. With a single sentence in a foreign capital, the King has committed the " +
            "nation to a war with Wehlen and, quite possibly, with Wehlen's friends in United Contana.",
            "Supporters call it the restoration of Rizia's honor. We ask a simpler question: who will pay for it, and who will " +
            "bleed for it? It will not be the men who cheered in Konstantium. It will be the farm boys of Brenas on the night trains north.",
            "In Zille, Rizians and Wehzeks have lived side by side for a generation. Tonight those families sit by their radios and " +
            "wait to learn which army will reach their street first."));

        Publish("Cost", "V", "Who Pays for the King's War?", P(
            "Economists consulted by the Voice of Rizia estimate that a campaign in Zille will cost the treasury more in a single " +
            "month than the entire national education budget for a year. A war tax is already being discussed in the palace. " +
            "Conscription notices are being printed in Montaklar as these words go to press.",
            "The Royal Herald speaks of glory. The Voice of Rizia will count the cost, line by line, and print it. Somebody must."));

        Publish("Wehzeks", "V", "Fear in Little Wraclawitz", P(
            "In the Wehzek quarter of Iza that locals call Little Wraclawitz, shutters stayed down this morning. Shopkeepers who " +
            "have lived in Rizia for thirty years told our reporter that they were afraid to open their doors.",
            "\"We are Rizian taxpayers,\" one baker said. \"My sons were born here. Now people look at us like we are Smolak's " +
            "soldiers.\" Police patrols have been doubled in the quarter. The Voice of Rizia asks the King to remember that his " +
            "war is with an army, not with his own citizens."));

        // --- reports at home ---
        Report("Mobilization_Proclamation", SuzerainTokenName.RiziaCityPorteDrazon, "General Mobilization Proclaimed", P(
            "A royal proclamation read from the steps of the palace this morning declares that the Kingdom of Rizia is, \"from this " +
            "day,\" in a state of war with the Republic of Wehlen. It cites Wehlen's refusal to honor the lease of 1927 and its " +
            "continued occupation of Zille by force of arms.",
            "All reservists of the Royal Army have been ordered to report to their depots within forty-eight hours. The railways of " +
            "Brenas have been placed under military authority, and ports, airfields and telegraph offices will now operate under the " +
            "direction of the General Staff. The crowd that heard the proclamation read answered it with \"Long live the King.\""));

        Report("Mobilization_Nation", SuzerainTokenName.RiziaCityPorteDrazon, "The Nation Mobilizes", P(
            "News of the King's declaration reached Porte Drazon before his plane did. By nightfall, crowds had filled the square " +
            "before the Royal Palace, and recruiting offices across the capital had to turn volunteers away.",
            "Trains are being requisitioned for the northern line, schools in Brenas are being turned into barracks, and the " +
            "Ministry of the Interior has declared a curfew in border districts. The General Staff awaits the King's orders."));

        Report("Mobilization_Montaklar", SuzerainTokenName.RiziaCityMontaklar, "General Staff Activates the Zille Plans", P(
            "At Royal Army headquarters in Montaklar, the plans for a campaign in Zille, drawn up and shelved by three successive " +
            "chiefs of staff, have been taken out of the safe. Officers have been sleeping at their desks.",
            "The General Staff's first assessment is sober. The Wehzek garrison in Zille is dug in behind a fortified border wall " +
            "that runs along the river line from Mave Nortus to the Gulf of Moridia, and is supplied by sea from the Wehzek mainland. " +
            "Every week it is allowed to resupply, it will be harder to break.",
            "The Staff will present its options for the first operation to His Majesty at the earliest opportunity."));

        Report("Mobilization_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "Mave Nortus: The Border Goes Dark", P(
            "The Wehzek border posts facing Mave Nortus went dark shortly after midnight. Our sentries report the sound of engines " +
            "behind the wall and floodlights sweeping the river line.",
            "Families in the eastern quarter have begun loading carts and leaving for Dapalle. Royal Army troops have taken over the " +
            "railway station, and sandbags are going up around the post office. Mave Nortus will be the first Rizian town the enemy " +
            "sees, and the town knows it."));

        Report("Mobilization_Sallabes", SuzerainTokenName.RiziaCitySallabes, "The Fleet Puts to Sea from Sallabes", P(
            "Before dawn, the Royal Navy squadron at Sallabes slipped its moorings and steamed out into the Gulf of Moridia with its " +
            "lights darkened. Dockworkers lined the harbor wall in silence to watch them go.",
            "Naval officers say the squadron's task is to watch the approaches to Zille harbor and report every Wehzek convoy " +
            "attempting the crossing from the mainland. The harbor has been closed to all foreign shipping."));

        Report("Mobilization_Jales", SuzerainTokenName.RiziaCityJales, "Blackout on the Sudis Coast", P(
            "Jales has been ordered to observe a full blackout from sunset. Fishing boats have been requisitioned by the Royal Navy " +
            "to patrol the Sudis Sea coast, and coastal batteries built in the last century are being cleared of brambles and re-armed.",
            "Older residents remember the Wehzek ships that came up this coast in the troubles of 1926. \"They will not land here again,\" the " +
            "harbormaster told the town council."));

        Report("Mobilization_Monqiz", SuzerainTokenName.RiziaCityMonqiz, "Wehzek Cargo Impounded at Monqiz", P(
            "Customs officers and royal marines boarded four Wehzek-flagged merchant ships in Monqiz harbor this morning. Their " +
            "cargoes, including machine parts and over two hundred tons of fuel, have been impounded for the duration of the war.",
            "The crews will be repatriated through a neutral port. Port authorities expect trade to collapse in the short term, and " +
            "several Monqiz trading houses with Wehzek partners have closed their doors."));

        // Iza: the city of the 1926 Uprising, democratic and wary of the Crown, where Su Omina is strong. Its welcome for a
        // king's war depends on where Su Omina stands.
        bool suAllied = Variables.GetBool("RiziaDLC.Situation_Order_SuOmina_AlliedToCrown");
        bool suHostile = Variables.GetBool("RiziaDLC.Situation_Order_SuOmina_Destabilizing") || Variables.GetBool("RiziaDLC.Iza_Outcome_SuOminaAgainstCrown");
        string wehzekQuarter =
            "Across town, the Wehzek quarter of Little Wraclawitz kept its shutters down. Police have been posted at its entrances " +
            "after rocks were thrown at a Wehzek bakery.";
        if (suAllied)
        {
            Report("Mobilization_Iza", SuzerainTokenName.RiziaCityIza, "Su Omina Rallies in Iza; the Democrats Stay Silent", P(
                "Su Omina turned out in force on Iza's main square, banners and all, and its young men queued at the recruiting office " +
                "before it opened. Its leaders called the war \"the debt of 1926 finally paid.\"",
                "The rest of Iza was quieter. The democratic clubs and the dockworkers' union held a silent vigil outside the town hall, " +
                "under a single placard: \"A war declared without a vote.\"",
                wehzekQuarter));
        }
        else if (suHostile)
        {
            Report("Mobilization_Iza", SuzerainTokenName.RiziaCityIza, "Iza Divided Over the King's War", P(
                "Su Omina banners went up in Iza within the hour, but for Zille, not for the King. \"We will free Zille,\" one of its " +
                "speakers told the square, \"and then we will see about Porte Drazon.\"",
                "Students from the university and the dockworkers' union marched the other way, against a war declared in a foreign " +
                "capital without a word to the House. Police kept the two crowds apart until nightfall.",
                wehzekQuarter));
        }
        else
        {
            Report("Mobilization_Iza", SuzerainTokenName.RiziaCityIza, "A Cool Welcome in Iza", P(
                "Iza, which has never forgiven the Crown for 1926, met the news from Konstantium without bells. A few hundred volunteers " +
                "came forward, many of them families with relatives in Zille; the cafés on the main square argued late into the night.",
                "The city's democratic clubs issued a short statement: Zille belongs to Rizia, and a war belongs to the people who will " +
                "fight it. They want the House of Delegates to vote on it.",
                wehzekQuarter));
        }

        Report("Mobilization_Bronaz", SuzerainTokenName.RiziaCityBronaz, "Bronaz Factories Switch to War Production", P(
            "The workshops of Bronaz have been placed on a war footing by royal decree. Shifts have been extended to twelve hours, " +
            "and factories that built tractors last week are being retooled to repair trucks and armored cars for the Royal Army.",
            "Union leaders have agreed to suspend strikes for the duration of the war, \"so long as the war lasts weeks, not years.\""));

        Report("Mobilization_Zille", ZilleOperation.TokenName, "Inside Zille: Martial Law", P(
            "Our sources inside the Zille region report that the Wehzek authorities declared martial law within an hour of the King's " +
            "speech. A curfew is in force from dusk to dawn, radios are being confiscated, and men of military age with Rizian " +
            "surnames are being rounded up for questioning.",
            "Wehzek troops are digging in along the border wall and around the harbor of Zille. Supply ships from the Wehzek mainland " +
            "were seen unloading at Nanta through the night. Smolak Square, once the square of Duke Fedo Sazon, is ringed with " +
            "sandbags and machine-gun posts."));

        // --- reports from abroad: what each government said, in report tone, then our embassy's reading ---
        Report("Declaration_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Letter from Queen Beatrice", P(
            "\"Dear Romus,",
            "You might have warned me. I learned that my brother-in-law had gone to war from a wireless operator, and I had to " +
            "pretend to my own ministers that I had known all along.",
            embargo
                ? "Rumburg is not buying a drop of Wehzek oil, and I see no reason to start again. Smolak can sell it to Contana, " +
                  "if Contana can be bothered to come and fetch it."
                : halved
                    ? "We buy half the oil from Smolak that we used to, as you know, since you asked so nicely in the spring. The other half " +
                      "I am prepared to discuss. Not by letter. Ring me."
                    : "We are, as you know perfectly well, still buying Smolak's oil. I am prepared to discuss that. I am not prepared " +
                      "to discuss it by letter. Ring me.",
            "Your generals' arms will sail ahead of everyone else's. Win it quickly. GRACE has a long memory for kings who lose wars, " +
            "and a very short one for those who win them.",
            "Beatrice\""));

        Report("Declaration_Grace", "Rizia_Faction_Event_GRACE", "The Guild of Royal Allies Speaks", P(
            "The Council of the Guild of Royal Allies met in emergency session in Thornbourgh last night and came out firmly on our " +
            "side. Its members condemned Wehlen's refusal to return Zille on the date agreed in the lease of 1927, declaring that " +
            "\"a treaty that binds only one of its signatories is no treaty at all.\"",
            "The Guild affirmed the right of the Kingdom of Rizia, one of its founding members, \"to recover territory that is " +
            "lawfully its own,\" and called on every member to refuse any trade or assistance that would prolong the Wehzek occupation.",
            "Our delegation in Thornbourgh reports that the statement was drafted in Queen Beatrice's own office and adopted without " +
            "debate. Two smaller members asked for the word \"recover\" to be softened. They were overruled within the hour."));

        Report("Declaration_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Wehlen Mobilizes", P(
            "President Wiktor Smolak has declared a general mobilization and placed the Zille region under martial law. In a radio " +
            "address from Wraclawitz, broadcast within hours of the King's speech, he told the Wehzek nation that a king had declared " +
            "war on a free republic without consulting his people, \"because his people are not consulted on anything,\" and had " +
            "consulted \"only his pride, and the bankers of Thornbourgh who pay for it.\"",
            "\"Zille has been Wehzek for a quarter of a century,\" Smolak said. \"Our sons were born there. Our dead are buried " +
            "there. Zille will never kneel.\"",
            "Our intelligence reports troopships and supply convoys gathering in the Wehzek ports facing the Strait of Kreleck. If " +
            "they cross unhindered, the Zille garrison will grow stronger every week."));

        Report("Declaration_Valgsland", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Condemns Rizia", P(
            "Chancellor Emmerich Hegel has condemned the King's declaration of war \"in the strongest possible terms.\" Disputes " +
            "between states, the Chancellor said, \"are settled by negotiation and arbitration, not by the whims of crowned heads.\" " +
            "He has called on the Security Committee to act without delay to restore peace in Zille.",
            "Hegel went further than any other leader in Merkopa, promising that Valgsland \"stands ready to assist the people of " +
            "Wehlen in defending themselves.\"",
            "Our embassy reports demonstrations at its gates, but nothing worse. Diplomatic sources suggest United Contana is weighing " +
            "emergency aid to Wehlen, and that Valgslandian ports may be offered to the ships carrying it."));

        string lespiaNote = Variables.GetString(AnDeclaration.VarLespianNote) == "kept"
            ? " Prime Minister Alvarez's private note offering \"practical arrangements\" remains in the King's possession, and the " +
              "Lespian ambassador asked twice this week whether it had been read."
            : string.Empty;
        Report("Declaration_Lespia", SuzerainTokenName.RiziaCountryLespia, alvarezBacks ? "Lespia Says Its Piece on Zille" : "Lespia Urges Restraint", P(
            alvarezBacks
                ? "Prime Minister Patricio Alvarez has not condemned our declaration. In the hall he told the assembly that Lespia had " +
                  "already said what it thought of Wehlen's occupation of Zille, and that what Rizia chose to do about it was \"Rizia's " +
                  "business.\" His office has since asked only that the sea lanes of the Sudis Sea and the Strait of Kreleck stay open " +
                  "to neutral shipping."
                : "Prime Minister Patricio Alvarez says Lespia has followed the events in Konstantium \"with deep concern.\" His office has " +
                  "called on both Rizia and Wehlen to show restraint and protect civilian life, and above all to keep the sea lanes of the " +
                  "Sudis Sea and the Strait of Kreleck open to neutral shipping.",
            "Behind the polite words, the Lespian navy has been placed on alert, and Lespian traders are buying up grain contracts across " +
            "the region. Lespia has never forgiven President Smolak for his speeches against it. It will help whichever side it " +
            "profits most from helping." + lespiaNote));

        bool palesFriendly =
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Buy") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled")) ||
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Cede") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_CedeCancelled")) ||
            Variables.GetBool("RiziaDLC.CountryRelation_Pales_Friendly") || Variables.GetBool("RiziaDLC.CountryRelation_Pales_Allied");
        Report("Declaration_Pales", SuzerainTokenName.RiziaCountryPales, palesFriendly ? "Pales Stays Calm" : "Pales Mans Its Border", palesFriendly
            ? P("Grand Duke Axel Reinhart has told the Palesian parliament that Pales will stay out of the war in Zille. \"Pales has an " +
                "agreement with the Kingdom of Rizia, and Pales honors its agreements,\" the Duke said. \"The war in Zille is not our war.\"",
                "He added that Pales would not allow the fighting to reach Palesian soil or Palesian waters, and that he had \"every " +
                "confidence that King Romus understands this as well as I do.\"",
                "A few extra companies have been sent to the border, and Palesian officials have taken care to call it routine. Our " +
                "consul in Pales describes the mood as nervous, but not hostile.")
            : P("Grand Duke Axel Reinhart has ordered Palesian forces to the border with the Pales Administrative District. Addressing the " +
                "Palesian parliament, he said Pales had watched \"a neighbor declare war on a neighbor from the podium of the Alliance " +
                "of Nations\" and would be foolish not to ask whether it was next.",
                "The Duke called the deployment a precaution, and said the troops would stay on the border \"for as long as Rizian " +
                "armies are on the march.\"",
                "Palesian newspapers are openly asking whether the King will turn south once Zille is taken. Our consul advises that " +
                "every movement of Rizian troops near the border will be read in Pales as a threat."));

        Report("Declaration_Derdia", SuzerainTokenName.RiziaCountryDerdia, "Derdia's Quiet Approval", P(
            "The office of the Supreme Wiseman has answered the war with a short and carefully chosen message. Derdia, it says, " +
            "\"has long held that the occupation of Zille was a wound that would not close on its own,\" and it prays for a swift " +
            "return of peace and for the protection of the faithful of every creed in the region.",
            "There was no condemnation, and no word of sympathy for Wehlen. Our consulate reads this as quiet approval. It also reports that the Derdian " +
            "mercenaries contracted by Wehlen in Zille are now demanding their pay in advance, which suggests they too have read the message."));

        Report("Declaration_Morella", SuzerainTokenName.RiziaCountryMorella, "Morella Hesitates", P(
            "Prime Minister Alma Saltana has announced that Morella will stay out of the war, and has suspended all Morellan merchant " +
            "shipping in the Gulf of Moridia \"for the safety of its crews.\" Morella, she told the press in Cavette, is \"a friend " +
            "of the peoples of both Rizia and Wehlen\" and will not lend its ports, its ships or its voice to the fighting.",
            "Speaking for the Morellan Unified Front government, the Prime Minister called for an immediate ceasefire and for the " +
            "question of Zille to be returned to the Alliance of Nations, \"where it belongs.\"",
            "Our embassy in Cavette reports that the cabinet is divided. The Prime Minister's Malenyevist allies want Wehlen supported; " +
            "the trading houses of the coast want Rizian business kept. For now, Morella has chosen to do nothing loudly."));

        Report("Declaration_Sordland", SuzerainTokenName.RiziaCountrySordland, "Sordland Calls for Calm", P(
            "President Anton Rayne has called on Rizia and Wehlen to cease hostilities, and has offered to host peace talks in Holsord " +
            "\"at their earliest convenience.\" His government says it offers its good offices to both parties and hopes the dispute " +
            "can still be settled by peaceful means.",
            "The President's office ended on a sharper note, warning that Sordland \"will not tolerate the use of terrorist " +
            "organizations active on its own soil, whatever their target.\"",
            "That last sentence is aimed at Rumburg's friends in the Bludish Freedom Front, and at anyone who might call on them. " +
            "Sordish trade with Wehlen continues, and Sordish newspapers are divided on whether the King is bold or reckless."));

        Report("Declaration_Kyrute", SuzerainTokenName.RiziaCountryKyrute, "Kyrute Watches", P(
            "The government of Kyrute, host of the Alliance of Nations, says it deeply regrets that the Alliance's assembly hall was " +
            "used to declare war, and has asked every delegation to respect \"the dignity of the Alliance and its procedures.\" It " +
            "has promised to guarantee the safety of all delegations in Konstantium, including those whose countries are now at war.",
            "Kyrute's embassy in Porte Drazon has separately requested an urgent briefing on the safety of Kyrutean nationals in Zille. " +
            "Our ambassador believes Kyrute will vote for any ceasefire resolution put before the Security Committee."));

        Report("Declaration_Agnolia", "Rizia_Country_Agnolia", "Agnolia Reserves Judgment", P(
            "Prime Minister Maartin Van Hoorten has called for restraint, but stopped short of condemning Rizia. \"Agnolia believes in " +
            "treaties, because a small country has nothing else to rely on,\" he told reporters. \"We note that the lease of 1927 " +
            "has expired. We also note that wars, once begun, rarely end where they were meant to.\"",
            "Agnolia, he said, reserves judgment. Our embassy reads his words as cautiously sympathetic. Agnolian shipyards have, quietly, asked whether Rizia will " +
            "be in the market for new ships."));

        Report("Declaration_Galmland", "Rizia_Country_Galmland", "Galmland Takes Wehlen's Side", P(
            "The government of Galmland has declared its support for Wehlen, calling the Kingdom of Rizia \"a feudal remnant propped " +
            "up by the crowned houses of the north.\" It says it stands with \"the workers of Wehlen and of Zille\" and has called " +
            "on progressive nations everywhere to deny the King the means to wage his war.",
            "The declaration changes little in practice. Galmland has no ships in the region and little trade with us. It does, however, " +
            "tell us where the Contanan bloc is likely to stand."));

        Report("Declaration_Arcasia", "Rizia_Faction_Event_ATO", "Arcasia Speaks: \"Grave Concern\"", P(
            "President Dwight Walker's office has expressed \"grave concern\" at the outbreak of hostilities between Rizia and Wehlen, " +
            "and has called for an immediate ceasefire and for the dispute over Zille to be settled under the auspices of the Alliance " +
            "of Nations.",
            "Ventrie City closed with a warning to \"all outside powers\" against using the conflict to extend their influence in " +
            "South Merkopa. Our embassy reads that sentence as a warning to United Contana, not to us. President Walker, whose " +
            "family is Rumburgian, is said to have told his cabinet that \"a Contanan client losing a seaport is not the worst news " +
            "of the week.\" The Arcasian Treaty Organization will not intervene, but it will not stand in our way either."));

        Report("Declaration_Contana", "Rizia_Faction_Event_CSP", "United Contana Condemns \"Royal Aggression\"", P(
            "The Central Committee in Kyow has condemned what it calls \"the unprovoked aggression of the Rizian monarchy\" against " +
            "Wehlen. The war, Contana claims, is not a dispute over a lease at all, but an attempt by the crowned houses of Merkopa, " +
            "\"financed from Thornbourgh,\" to roll back the gains of free peoples.",
            "Chairman Malenyev's government has promised that United Contana and its partners in the Contanan Security Pact \"will " +
            "not stand idly by while a fraternal republic is attacked.\"",
            "Our intelligence takes \"will not stand idly by\" to mean money, arms and advisers rather than troops. United Contana has " +
            "financed Wehlen for years. If its aid reaches Zille, it will reach it by sea, through the Strait of Kreleck."));
        _logged = true;
    }

    // ------------------------------------------------------------------ operations: the coverage of each phase

    /// <summary>
    /// One phase of Operation Northern Crown as the papers see it: which phase, what it aimed at and what it really did (the whole
    /// aim, part of it, other cities instead, or nothing; and what it lost), what came before it, and the state of the war around
    /// it. Every piece branches on the phase first, then on that outcome: a phase that won a city is never told as a setback, and
    /// one that lost a city is never told as a clean success.
    /// </summary>
    private sealed class PhaseNews
    {
        public int K, P, Cost, Landed, PrevPhase, Tries;
        public bool Final, Liberated, Divided, Lost, PrevExists, PrevGain, Held, Censored, StraitShut, ZilleHeld, WonBack;
        public string Target = string.Empty, Tier = string.Empty, Next;
        public ObjectiveInfo Info;
        public AfterAction A;
        public WarCouncil.Orders O;

        public int HarborTries;         // phases fought against the city of Zille, this one included
        public bool HarborEverWon;      // the city of Zille was Rizian at some point

        public static bool Decree(string key) => Variables.GetBool($"ZilleWar.Decree_{key}");
        public bool Early => Liberated && P < ZilleCampaign.LastPhase;
        public bool Won => A.Won;                       // the aim, as it stood when the phase began, fully taken
        public bool Partial => A.Partial;               // part of a two-city aim taken
        public bool Gain => A.Gain;                     // any city won, in the aim or not (Rizian towns won back too)
        public bool Loss => A.Loss;                     // any city lost
        public bool Taken => A.CitiesTaken.Count > 0;   // a Zille city taken
        public bool Carried => Tries > 1;               // the same objective as the phase before
        public List<string> Extra => A.Extra;
        public List<string> OwnLost => A.OwnLost;
        public List<string> BackLost => A.BackLost;
        public bool Plural => A.AimStart.Count > 1;
        public bool ZilleAim => Info.Id == Objective.Harbor;   // the phase fought for the city of Zille itself
        public string Is => Plural ? "are" : "is";
        public string Has => Plural ? "have" : "has";
        public string It => Plural ? "them" : "it";
        public string Toll => A.PlayerDeadMen;
        public string AimPlaces => AfterAction.Places(A.AimStart);
        /// <summary>"The final phase of Operation Northern Crown", or the phase's own name when the war ended before Phase III.</summary>
        public string LastName => P >= ZilleCampaign.LastPhase ? $"The final phase of {ZilleCampaign.OperationCodename}" : ZilleCampaign.PhaseName(P);
    }

    private static PhaseNews ReadPhase(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        int p = Variables.GetInt(ZilleCampaign.PhaseOfVar(k));
        if (p <= 0)
        {
            p = ZilleCampaign.CurrentOperation().phase;
        }

        p = Math.Clamp(p <= 0 ? k : p, 1, ZilleCampaign.LastPhase);
        a.Classify(info);
        bool zilleHeld = Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
        var x = new PhaseNews
        {
            K = k, P = p, Info = info, A = a, O = WarCouncil.CurrentOrders(),
            Target = a.AimName.Length > 0 ? a.AimName : info.Target ?? "the last Wehzek positions", Tier = ZilleCampaign.Tier,
            Liberated = ZilleCampaign.Liberated, ZilleHeld = zilleHeld,
        };
        x.Final = x.Liberated || ZilleCampaign.Defeated;
        x.Divided = ZilleCampaign.Defeated && zilleHeld;
        x.Lost = ZilleCampaign.Defeated && !zilleHeld;
        x.Cost = a.Cost;
        x.Tries = AfterAction.Tries(k, info.Id);
        x.WonBack = a.AimStart.Any(t => AfterAction.HeldBefore(t, k));
        if (k >= 2 && AfterAction.HistoryEntry(k - 1) is { } prev)
        {
            x.PrevExists = true;
            x.PrevGain = AfterAction.PhaseGained(k - 1);
            int pp = Variables.GetInt(ZilleCampaign.PhaseOfVar(k - 1));
            x.PrevPhase = pp > 0 ? pp : k - 1;
            x.Held = p - x.PrevPhase > 1;
        }

        string[] hist = ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);
        x.HarborTries = Math.Max(1, hist.Count(h => h.StartsWith("Harbor:", StringComparison.Ordinal)));
        x.HarborEverWon = zilleHeld || hist.Contains("Harbor:won") || AfterAction.HeldBefore("Zille", k + 1);
        x.Landed = p >= 2 ? Variables.GetInt(PhaseResume.VarLanded) : 0;
        x.Censored = PhaseNews.Decree("WarCensorship");
        x.StraitShut = WorldSystems.StraitClosedNow || x.O.Navy == "Strait";
        x.Next = !x.Final && ZilleCampaign.Available().Count > 0 ? ZilleCampaign.AimName(ZilleCampaign.Info(ZilleCampaign.NextObjective)) : null;
        return x;
    }

    // The places in the region that Wehlen still holds after an armistice, as prose: "the ports of Isor and Nanta", "the port of Nanta and the hills around Auremina".
    private static string HeldByWehlen()
    {
        var ports = new List<string>();
        if (!Variables.GetBool(ZilleOperation.CapturedVar("Isor"))) ports.Add("Isor");
        if (!Variables.GetBool(ZilleOperation.CapturedVar("Nanta"))) ports.Add("Nanta");
        var parts = new List<string>();
        if (!Variables.GetBool(ZilleOperation.CapturedVar("Zille"))) parts.Add("the city of Zille");
        if (ports.Count == 2) parts.Add("the northern ports of Isor and Nanta");
        else if (ports.Count == 1) parts.Add($"the northern port of {ports[0]}");
        if (!Variables.GetBool(ZilleOperation.CapturedVar("Auremina"))) parts.Add("the hills around Auremina");
        return parts.Count == 0 ? "the rest of the region" : Join(parts);
    }

    /// <summary>What Rizia holds in the region, as prose: "the city of Zille, its harbor and the port of Nanta".</summary>
    private static string HeldByRizia()
    {
        List<string> held = AfterAction.HeldNow();
        var parts = new List<string>();
        if (held.Contains("Zille"))
        {
            parts.Add("the city of Zille");
            parts.Add("its harbor");
        }

        if (held.Contains("Auremina")) parts.Add("the gold hills of Auremina");
        var ports = held.Where(c => c is "Isor" or "Nanta").ToList();
        if (ports.Count > 0) parts.Add(AfterAction.Places(ports));
        return Join(parts);
    }

    /// <summary>" Wehzek troops also remain in Mave Nortus, on the Rizian side of the old border.", or nothing.</summary>
    private static string OccupiedLine()
    {
        List<string> occ = RizianOccupied();
        return occ.Count > 0 ? $" Wehzek troops also remain in {Join(occ)}, on the Rizian side of the old border." : string.Empty;
    }

    /// <summary>The Rizian towns Wehzek troops hold now.</summary>
    private static List<string> RizianOccupied()
    {
        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        return ZilleData.Towns.Where(t => t.Side != "zille" && t.Role == "city" && ZilleCampaign.IsEnemy(owners, t.Tile)).Select(t => t.Name).ToList();
    }

    private static string Words(int n) => n switch { <= 1 => "one", 2 => "two", 3 => "three", 4 => "four", _ => n.ToString() };
    private static string Times(int n) => n switch { <= 1 => "once", 2 => "twice", 3 => "three times", _ => $"{n} times" };
    private static string Nth(int n) => AfterAction.Nth(n);
    private static string Places(List<string> l) => AfterAction.Places(l);
    private static string Place(string c) => AfterAction.Place(c);

    private static string Was(List<string> l) => l.Count > 1 ? "have" : "has";
    private static string IsAre(List<string> l) => l.Count > 1 ? "are" : "is";
    private static string WasWere(List<string> l) => l.Count > 1 ? "were" : "was";
    private static string ItThem(List<string> l) => l.Count > 1 ? "them" : "it";

    /// <summary>A soldier's home town for the Voice's letters: Dapalle, unless the Wehzeks hold it.</summary>
    private static string HomeTown(PhaseNews x) => x.OwnLost.Contains("Dapalle") || RizianOccupied().Contains("Dapalle") ? "Balle" : "Dapalle";

    public static void PublishOperationResult(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        PhaseNews x = ReadPhase(k, info, won, a);
        string key = $"Operation{k}";
        (string title, string text) rh = HeraldPiece(x);
        Publish(key, "RH", rh.title, rh.text);
        (string title, string text) farus = FarusColumn(x);
        Publish(key + "_Farus", "RH", farus.title, farus.text);
        (string title, string text) g = GeopoliticoPiece(x);
        Publish(key, "G", g.title, g.text);
        (string title, string text) v = VoicePiece(x);
        Publish(key, "V", v.title, v.text);
    }

    // ---- the Royal Herald: royalist, triumphant in victory and stoic in a setback

    private static (string, string) HeraldPiece(PhaseNews x)
    {
        AfterAction a = x.A;
        string own = x.OwnLost.Count > 0
            ? $" On our side of the old border, {Join(x.OwnLost)} {Was(x.OwnLost)} fallen to a Wehzek counterattack, and the reserves are " +
              $"already moving up to take {ItThem(x.OwnLost)} back."
            : string.Empty;
        string back = x.BackLost.Count > 0
            ? $" {Cap(Places(x.BackLost))} {Was(x.BackLost)} been lost to a Wehzek counterattack, and our soldiers mean to have " +
              $"{ItThem(x.BackLost)} back."
            : string.Empty;
        string lost = back + own;
        string retaken = a.CitiesRetaken.Count > 0 ? $" {Join(a.CitiesRetaken)} {IsAre(a.CitiesRetaken)} free again." : string.Empty;
        string extra = x.Extra.Count > 0 ? $" Our soldiers took {Places(x.Extra)} besides." : string.Empty;
        string next = x.Next != null
            ? $"One phase remains, against {x.Next}. The Royal Herald does not expect {(x.Next.Contains(" and ") ? "them" : "it")} to hold for long."
            : "The General Staff is preparing its next orders.";

        if (x.Early)
        {
            return (x.P == 1 ? "The Wehzek Line Breaks in a Single Month" : "The Wehzek Line Breaks Before the Final Phase", P(
                $"Nobody in Montaklar expected it so soon. {ZilleCampaign.PhaseName(x.P)} has not only taken {Places(a.CitiesTaken)} but broken " +
                $"the Wehzek garrison behind {ItThem(a.CitiesTaken)}, and the General Staff reports {a.EnemyDeadMen} enemy soldiers killed, " +
                "wounded or captured." + retaken,
                "Wehzek officers are crossing the lines under white flags to ask for terms. His Majesty has ordered that they be received " +
                "with every courtesy due to brave men who have lost."));
        }

        if (x.Final)
        {
            if (x.Liberated)
            {
                bool ports = a.CitiesTaken.Any(c => c is "Isor" or "Nanta");
                return (x.Info.Id switch { Objective.Ports => "The Ports Are Ours", Objective.Wall => "The Gold Hills Are Ours", _ => "Zille City Is Ours" }, P(
                    $"The final phase of {ZilleCampaign.OperationCodename} has ended with the Golden Banner over {Places(a.CitiesTaken)}." +
                    (ports ? " Our soldiers went into the ports from the land and the sea." : string.Empty) +
                    $" The General Staff reports {a.EnemyDeadMen} Wehzek soldiers killed, wounded or captured in the last fighting of the campaign." + retaken,
                    "The Royal Herald has printed many reports from the front in these months. It is proud to print this one. Axa Victoria!"));
            }

            if (x.Divided)
            {
                if (x.Won && x.ZilleAim)
                {
                    return x.WonBack
                        ? ("Zille Is Ours Again", P(
                            $"The city of Zille, lost to a Wehzek counterattack, is Rizian again. The final push of {ZilleCampaign.OperationCodename} " +
                            "carried our soldiers back into the harbor, and the General Staff reports that the old town was cleared street by " +
                            "street." + extra + retaken + lost,
                            "His Majesty's army did not take everything it set out to take. It took back what mattered most."))
                        : ("Zille Taken in the Last Hour", P(
                            $"{(x.HarborTries > 1 ? $"It took {Words(x.HarborTries)} attempts" : "It took until the last phase")}, but the city of " +
                            $"Zille is Rizian. The final push of {ZilleCampaign.OperationCodename} carried our soldiers into the harbor" +
                            $"{(x.HarborTries > 1 ? $" that had defied them {Times(x.HarborTries - 1)}" : string.Empty)}, and the General Staff " +
                            "reports that the old town was cleared street by street." + extra + retaken + lost,
                            "His Majesty's army did not take everything it set out to take. It took what mattered most."));
                }

                if (x.Won || x.Partial)
                {
                    List<string> took = x.Won ? a.AimStart : a.AimTaken;
                    return ($"The Last Phase {(x.WonBack ? "Wins Back" : "Takes")} {Join(took)}", P(
                        $"With the city of Zille already Rizian, the final push of {ZilleCampaign.OperationCodename} took {Places(took)}" +
                        (x.Partial ? $", though {Place(a.AimMissed[0])} held out to the end" : string.Empty) +
                        $". The General Staff reports {a.EnemyDeadMen} Wehzek soldiers killed, wounded or captured." + extra + retaken + lost,
                        "His Majesty's army did not take everything it set out to take. It took a great deal."));
                }

                if (x.Gain)
                {
                    return ($"{Join(a.Gained)} Taken in the Last Phase", P(
                        $"The final phase of {ZilleCampaign.OperationCodename} has ended short of {x.AimPlaces}, but not before our soldiers took " +
                        $"{Places(a.Gained)}. The Golden Banner still flies over the city of Zille and its harbor." + lost,
                        "The Royal Herald salutes the men of the last phase, who fought knowing it was the last, and fought anyway."));
                }

                return ("Our Soldiers Hold What They Won", P(
                    $"The final phase of {ZilleCampaign.OperationCodename} has ended short of {x.AimPlaces}" +
                    (!x.Loss && a.TilesLost == 0
                        ? ", but not one yard of what the Royal Army took in the earlier phases has been given up."
                        : ".") +
                    " The Golden Banner still flies over the city of Zille and its harbor." + lost,
                    "The Royal Herald salutes the men of the last phase, who fought knowing it was the last, and fought anyway."));
            }

            string gainsLost = x.Gain
                ? $" Our soldiers took {Places(a.Gained)} in the last fighting, and {(a.Gained.Count > 1 ? "hold them" : "hold it")}."
                : string.Empty;
            return ("The Last Phase Ends Before Zille", P(
                $"{x.LastName} has ended, and the city of Zille is {(x.BackLost.Contains("Zille") ? "in Wehzek hands again" : "still in Wehzek hands")}. " +
                (x.HarborEverWon
                    ? "Our soldiers took it once, and could not keep it against the counterattack."
                    : $"Our soldiers fought for it a {Nth(x.HarborTries)} time, and a {Nth(x.HarborTries)} time the garrison held.") + gainsLost + own,
                "The Royal Herald will not dress this up. It will only say that the men who went north did everything that was asked of " +
                "them, and that Rizia's claim to Zille has not grown weaker for being paid for in blood."));
        }

        if (x.P == 1)
        {
            // Phase I always goes for the city of Zille.
            if (x.Won)
            {
                return (x.Extra.Count > 0 ? $"Zille and {Join(x.Extra)} in One Stroke" : "The Golden Banner Returns to Zille", P(
                    "Twenty-five years after the lease, the Royal Army has entered the city of Zille. The opening phase of " +
                    $"{ZilleCampaign.OperationCodename} ended this week with the harbor, the river forts and the old town in Rizian hands" +
                    (x.Extra.Count > 0 ? $", and {Places(x.Extra)} taken besides" : string.Empty) +
                    $". The General Staff reports {a.EnemyDeadMen} Wehzek soldiers killed, wounded or captured." + own,
                    x.O.Bombard
                        ? "Our guns opened the way, and our infantry walked through it. Wehzek prisoners say the barrage was the worst hour of their lives."
                        : "Our soldiers went in without waiting for the guns, and the Wehzek garrison never recovered from the surprise.",
                    "His Majesty has ordered the bells of every sanctuary in Brenas to ring at noon. Axa Victoria!"));
            }

            return (x.Gain ? $"The Golden Banner Over {Join(a.Gained)}" : "Before the Walls of Zille", P(
                x.Gain
                    ? $"The opening phase of {ZilleCampaign.OperationCodename} has ended short of the city of Zille, but our soldiers took " +
                      $"{Places(a.Gained)} on the way. {a.Headline}" + own
                    : $"The opening phase of {ZilleCampaign.OperationCodename} has ended short of the city of Zille. {a.Headline}" + own,
                (x.Gain ? string.Empty : "The Royal Herald will not pretend that every battle is a victory. ") +
                "The garrison of Zille has had twenty-five years to dig in, and it has used them. The General Staff is already preparing the " +
                "second phase, and it will go for the city again.",
                "Zille has waited a quarter of a century for us. It can wait a few weeks more, and it will not wait in vain."));
        }

        // Phase II
        if (x.Carried && !x.Gain)
        {
            // The aim the last phase missed has held again, and nothing else was won.
            string held = !x.Loss && a.TilesLost == 0
                ? "They have not been driven back."
                : "They have given some ground, and they mean to take it back.";
            return (x.ZilleAim ? "The Army Holds Its Ground Before Zille" : $"The Army Holds Its Ground Before {x.Target}", P(
                $"Our soldiers have gone for {x.AimPlaces} a {Nth(x.Tries)} time, and a {Nth(x.Tries)} time the garrison has held. {held} " +
                (x.ZilleAim
                    ? "They are dug in within sight of the harbor cranes, and they are not going anywhere."
                    : $"They are dug in within sight of {x.Target}, and they are not going anywhere.") + lost,
                "The final phase of the operation is being planned. The Royal Herald says what every soldier at the front is saying: " +
                "third time lucky."));
        }

        if (x.Carried && x.Won && x.ZilleAim)
        {
            return ("Zille Is Ours at the Second Attempt", P(
                "The city of Zille, which held out against the opening phase of the operation, has fallen to the second. Our soldiers " +
                "went back up the same river road they had been driven from, and this time they reached the harbor." + extra + retaken + lost,
                $"The General Staff reports {a.EnemyDeadMen} Wehzek soldiers killed, wounded or captured. One phase of the operation " +
                "remains, and the Royal Herald has never been more confident of its outcome."));
        }

        if (x.Info.Id == Objective.Wall && x.Won)
        {
            return (x.Extra.Count > 0 ? $"Auremina, and {Join(x.Extra)} Besides" : "The Gold Hills Under the Golden Banner", P(
                $"The gold hills of Auremina, worked for twenty-five years for Wehzek masters, are Rizian again. The second phase of " +
                $"{ZilleCampaign.OperationCodename} broke into the hills from the valley road and took the town and its mines" +
                (x.Extra.Count > 0 ? $", and went on to take {Places(x.Extra)} as well" : string.Empty) + "." + retaken + lost,
                x.Next == "Isor and Nanta"
                    ? "One phase remains. Wehlen is left with the ports of Isor and Nanta, and our soldiers can see the sea from the hills."
                    : next));
        }

        if (x.Info.Id == Objective.Wall && !x.Gain)
        {
            return ("Hard Fighting in the Gold Hills", P(
                $"The second phase of {ZilleCampaign.OperationCodename} has ended in the hills short of Auremina, after the hardest " +
                $"fighting of the war. {a.Headline}" + lost,
                x.ZilleHeld
                    ? "Zille city and its harbor, won in the first phase, are as firmly ours as ever. The hills will be taken in their turn, " +
                      "and the Royal Herald has no doubt that the men who took the harbor will take them."
                    : "The hills will be taken in their turn, and the city of Zille with them. The Royal Herald has no doubt of it."));
        }

        if (x.Won || x.Partial)
        {
            List<string> took = x.Won ? a.AimStart : a.AimTaken;
            return ($"The Golden Banner Over {Join(took)}", P(
                $"{Cap(Places(took))} {IsAre(took)} Rizian again. The second phase of {ZilleCampaign.OperationCodename} took " +
                $"{ItThem(took)} from the Wehzek garrison, and the General Staff reports {a.EnemyDeadMen} enemy soldiers killed, wounded or " +
                "captured." + (x.Partial ? $" {Cap(Place(a.AimMissed[0]))} held out, for now." : string.Empty) + extra + retaken + lost,
                next));
        }

        if (x.Gain)
        {
            return ($"The Golden Banner Over {Join(a.Gained)}", P(
                $"The second phase of {ZilleCampaign.OperationCodename} did not reach {x.AimPlaces}, but our soldiers did not come back " +
                $"empty-handed: {Places(a.Gained)} {IsAre(a.Gained)} ours. {a.Headline}" + lost,
                next));
        }

        List<string> heldNow = AfterAction.HeldNow();
        return ($"Hard Fighting Before {x.Target}", P(
            $"The second phase of {ZilleCampaign.OperationCodename} has ended short of {x.AimPlaces}, after the hardest fighting of the war. " +
            a.Headline + lost,
            heldNow.Count > 0
                ? $"{Cap(Places(heldNow))}, won earlier in the war, {(heldNow.Count > 1 ? "remain" : "remains")} ours. {x.Target} will be " +
                  "taken in turn, and the Royal Herald has no doubt of it."
                : "The General Staff is preparing the final phase, and the Royal Herald has no doubt of its outcome."));
    }

    // ---- Mark Farus' column: one series, each piece following on from the last

    private static (string, string) FarusColumn(PhaseNews x)
    {
        const string By = "Mark Farus' From The Frontlines: ";
        string bastos = "Captain Ilario Bastos of the Royal Army";
        AfterAction a = x.A;
        string fate = x.Cost switch
        {
            2 => "Bastos was carried out of the line on a stretcher two days ago, a shell splinter in his shoulder. He asked me to say that " +
                 "he will be back. The surgeons say he will not, and I have decided to believe him rather than them.",
            1 => "Bastos lost his sergeant in the last week, a man who had served with him since they were boys in Sallabes. He told me so " +
                 "in one sentence, and then talked about the weather.",
            _ => "Bastos has been promoted. He took the news the way he takes everything, with a nod, and went back to his maps.",
        };
        string reservists = PhaseNews.Decree("BrenasReserves")
            ? " The reservists called up from Brenas have joined his company: miners and farmers in their thirties, who complain about " +
              "everything and never once about the fighting."
            : string.Empty;
        // Cities won elsewhere on the line while Bastos' company was held up.
        string elsewhere = x.Gain && !x.Won && !x.Partial
            ? $" Further along the line, other companies took {Places(a.Gained)}, and the men here talk about it more than they should."
            : string.Empty;
        string lostLine = x.Loss
            ? $" The news that {Join(a.CitiesLost)} {Was(a.CitiesLost)} fallen reached us on the field telephone, and nobody said anything for a while."
            : string.Empty;

        if (x.Early)
        {
            return (By + "Faster Than Anyone Planned", P(
                $"The officers I eat with had {(x.P == 1 ? "two more phases" : "a third phase")} drawn on their maps in red pencil. This morning " +
                "they rolled the maps up. The Wehzeks in front of us have stopped shooting, and their officers are walking toward our lines " +
                "with a white tablecloth tied to a broom handle.",
                $"{bastos}, whom I first met on the platform at Mave Nortus the night the war began, watched them come with his hands in his " +
                "pockets. \"I told you they had forgotten how to be afraid,\" he said. \"They have remembered.\""));
        }

        if (x.P == 1)
        {
            return x.Won
                ? (By + "The Harbor at Dawn", P(
                    "I last wrote from the platform at Mave Nortus, where a captain of the Royal Army checked his watch and told me the " +
                    "Wehzeks had forgotten what it is to be afraid. His name is Ilario Bastos, and this morning I watched him walk down to " +
                    "the harbor of Zille with his helmet in his hand.",
                    "The streets of the old town still smell of smoke. An old woman pressed a loaf of bread into the hands of the first " +
                    "soldier she saw and would not let go of him. Behind the shutters of the Wehzek houses, nobody moved." + lostLine,
                    "Bastos says his men have earned a night's sleep and will get half of one. The army moves on, and so will I."))
                : (By + "Within Sight of the Harbor", P(
                    "From the hill where I write this, I can see the cranes of Zille harbor. I have been looking at them for a week. " +
                    $"{bastos}, whom I met on the platform at Mave Nortus the night the war began, says they are closer than they look. " +
                    "He has said it every morning.",
                    (x.Cost >= 1
                        ? "The river forts held. His company went into them twice, and came back with fewer men each time. Nobody here " +
                          "talks about it, and everybody thinks about it."
                        : "The river forts held, and the Staff called the attack off before it could become a massacre. The men grumble " +
                          "that they were stopped too soon. Their officers, who saw the forts up close, do not.") + elsewhere + lostLine,
                    "Nobody here talks of going home. They talk of the next time."));
        }

        if (x.P == 2)
        {
            if (x.Carried && x.ZilleAim)
            {
                return x.Won
                    ? (By + "Zille, at Last", P(
                        "The cranes I watched for a month from the hill above the river are behind me now. I walked under them this " +
                        $"morning with {bastos}, who said nothing at all for the length of the quay and then laughed out loud.",
                        "The old town took the shells of two attacks, and it shows. A boy followed us down the waterfront asking every " +
                        "soldier if he had seen his father, who joined the Rizian volunteers in the spring." + reservists,
                        "Readers of this column will remember that I promised them Zille. I am glad to have kept the promise late rather " +
                        "than not at all."))
                    : (By + "The Same Hill", P(
                        "I am writing from the same hill as last month, above the same river, looking at the same cranes. The men call it " +
                        "the balcony, because it has the best view of the war and none of the comforts." + elsewhere,
                        fate + reservists + lostLine,
                        "There will be a third attempt. The men know it, and the Wehzeks across the river know it, and every night the two " +
                        "of them listen to each other digging."));
            }

            if (x.Info.Id == Objective.Wall)
            {
                return x.Won
                    ? (By + "The Miners Come Up", P(
                        $"When I left Zille harbor, {bastos} promised me the hills would be harder. He was right, and he was wrong: harder to " +
                        "climb, but when we reached the top of the valley road, Auremina simply opened its gates.",
                        Variables.GetBool(CityEvents.Flag("Auremina_MinersFreed"))
                            ? "The miners came up out of the pits in their helmets and lamps to see us, blinking in the daylight, and some of " +
                              "them asked for rifles before they asked for bread."
                            : "Soldiers now stand guard at the refinery gate. The miners watch them from the pit heads, waiting to see what kind " +
                              "of masters we will be.",
                        fate + reservists + lostLine,
                        x.Next != null && x.Next.Contains("Nanta")
                            ? "Bastos has a new map folded inside his helmet. It shows the coast road to Nanta."
                            : "Bastos has a new map folded inside his helmet. He has not shown it to me yet."))
                    : (By + "Mud in the Gold Hills", P(
                        $"The valley road to Auremina is a river of mud, and we have been standing in it for three weeks. {bastos}, who " +
                        "walked down to Zille harbor with me last month, has not been dry since." + elsewhere,
                        fate + reservists + lostLine,
                        "From the last ridge we hold, you can see the smoke of the refinery chimneys at Auremina. It is the closest thing to a " +
                        "promise this column has to offer."));
            }

            // The ports (or any other aim): the coast road.
            if (x.Won || x.Partial)
            {
                List<string> took = x.Won ? a.AimStart : a.AimTaken;
                return (By + "The Coast Road", P(
                    $"When I left the hills, {bastos} promised me the coast would be easier. It was not. We came down the coast road to " +
                    $"{Places(took)} under fire from the shore batteries, and walked onto the quays at dusk." +
                    (x.Partial ? $" Across the bay, {Place(a.AimMissed[0])} is still Wehzek. We can see its lights at night." : string.Empty),
                    fate + reservists + lostLine,
                    "Bastos says the sea air will do the men good. He has not slept in three days."));
            }

            return (By + "Salt Wind on the Coast Road", P(
                $"The coast road to {x.Target} runs between the sea and the cliffs, and the Wehzek guns have it covered from both ends. " +
                $"{bastos} has spent the phase in a ditch beside it." + elsewhere,
                fate + reservists + lostLine,
                $"From the last headland we hold, you can see the harbor of {(x.A.AimMissed.Count > 0 ? x.A.AimMissed[0] : x.Target)}. It is " +
                "the closest thing to a promise this column has to offer."));
        }

        // the final phase
        string lull = x.Held
            ? "The front was quiet for a month while His Majesty held the army back, and Bastos spent it writing letters home. "
            : string.Empty;
        if (x.Liberated)
        {
            string port = a.CitiesTaken.Contains("Nanta") ? "Nanta" : a.CitiesTaken.Contains("Isor") ? "Isor" : null;
            return port != null
                ? (By + $"The Last Ferry from {port}", P(
                    lull + $"I watched the last Wehzek ferry leave {port} this afternoon, low in the water, its decks crowded with soldiers who " +
                    "did not look back. On the quay, our men stood and watched it go, and nobody cheered until it had cleared the harbor mouth.",
                    fate,
                    $"I began this column on a railway platform at Mave Nortus. I end it on a quay at {port}. In between, {bastos} and his men " +
                    "walked the length of Zille, and I followed them as I promised I would."))
                : (By + "The Last Garrison", P(
                    lull + $"I watched the last Wehzek garrison march out of {Places(a.CitiesTaken)} this afternoon, rifles slung upside down, " +
                    "to the trucks that will take them to the ports. Our men lined the road and watched them go, and nobody cheered until the " +
                    "last truck had turned the corner.",
                    fate,
                    $"I began this column on a railway platform at Mave Nortus. I end it in {Join(a.CitiesTaken)}. In between, {bastos} and his " +
                    "men walked the length of Zille, and I followed them as I promised I would."));
        }

        if (x.Divided)
        {
            if (x.Won && x.ZilleAim)
            {
                return (By + "Zille, in the Last Hour", P(
                    lull + (x.WonBack
                        ? "We took the city of Zille once and lost it. The last attack went in at night to take it back, and by dawn there " +
                          "were Rizian flags on the harbor cranes again."
                        : $"There were {Words(Math.Max(2, x.HarborTries))} attacks on the city of Zille, and I watched all of them. The last " +
                          "went in at night, and by dawn there were Rizian flags on the harbor cranes.") +
                    " The men were too tired to celebrate. Most of them slept where they sat.",
                    fate,
                    "The rest of the region is still on the other side of the line. That is a story for another column, and perhaps " +
                    "another writer."));
            }

            if (x.Won || x.Partial)
            {
                List<string> took = x.Won ? a.AimStart : a.AimTaken;
                return (By + "The Last Push", P(
                    lull + $"Zille was ours before the last phase began. The last phase took {Places(took)}, and I watched it from the road " +
                    "behind the lines. The men were too tired to celebrate. Most of them slept where they sat." + lostLine,
                    fate,
                    "The rest of the region is still on the other side of the line. That is a story for another column, and perhaps " +
                    "another writer."));
            }

            return (By + "Where the Line Runs", P(
                lull + $"The last phase is over. {bastos} showed me where it ended: a stone wall across a field outside " +
                $"{a.EnemyCitiesLeft.FirstOrDefault(x.Info.Towns.Contains) ?? a.EnemyCitiesLeft.FirstOrDefault() ?? x.Info.Towns[0]}, " +
                "with our men on one side of it and theirs on the other, close enough to hear each other's cooking pots." + elsewhere,
                fate + lostLine,
                "Behind us is the city of Zille, which is ours. In front of us is the rest of the region, which is not. The men talk " +
                "about the first and do not talk about the second."));
        }

        return (By + "The River Again", P(
            lull + "I am back on the hill above the river, where I watched the first attack on Zille. The cranes of the harbor are still " +
            "there, and still Wehzek. " + (x.HarborEverWon
                ? "Our men walked under them once. They could not stay."
                : $"The {Nth(x.HarborTries)} attempt failed in the same streets as the {(x.HarborTries >= 3 ? "first two" : "first")}.") +
            elsewhere,
            fate,
            "I promised to tell the truth of this war from the men who fight it. The truth is that they could not have fought harder, " +
            "and that it was not enough."));
    }

    // ---- Geopolitico: foreign and dry; the front, then the markets, then the sea lanes

    private static (string, string) GeopoliticoPiece(PhaseNews x)
    {
        AfterAction a = x.A;
        string censor = x.Censored
            ? " Rizia has released no casualty figures of its own since wartime censorship came into force."
            : string.Empty;
        string deserters = PhaseNews.Decree("DeserterSafeConduct")
            ? " Rizian radio broadcasts promising Wehzek soldiers safe conduct and gold appear to be having some effect."
            : string.Empty;
        string losses = (x.BackLost.Count > 0 ? $" Wehzek counterattacks have retaken {Join(x.BackLost)}." : string.Empty) +
                        (x.OwnLost.Count > 0 ? $" Wehzek troops have crossed into Rizia and taken {Join(x.OwnLost)}." : string.Empty);
        string retaken = a.CitiesRetaken.Count > 0 ? $" Rizian troops have also retaken {Join(a.CitiesRetaken)}." : string.Empty;
        var portsLeft = new[] { "Isor", "Nanta" }.Where(c => !Variables.GetBool(ZilleOperation.CapturedVar(c))).ToList();
        string seaLanes = portsLeft.Count == 0
            ? "Shipping agents in Agnolia report that Wehzek ferries no longer have a harbor to make for in the region."
            : portsLeft.Count == 1
                ? $"Shipping agents in Agnolia report that Wehzek ferries now have a single harbor left in the region, at {portsLeft[0]}."
                : "Shipping agents in Agnolia report that Wehzek ferries are still crossing the Strait of Kreleck at night.";

        if (x.Early)
        {
            return ("Wehzek Garrison in Zille Collapses", P(
                $"Wehlen's forces in Zille have stopped fighting after a Rizian offensive took {Places(a.CitiesTaken)}, according to Rizian " +
                "and neutral sources alike. Wehzek officers are reported to be negotiating the surrender of the remaining garrisons." + deserters,
                "The speed of the collapse has surprised military observers in Konstantium, most of whom had expected the campaign to last " +
                "into the summer."));
        }

        if (x.Final)
        {
            if (x.Liberated)
            {
                var ports = a.CitiesTaken.Where(c => c is "Isor" or "Nanta").ToList();
                return ports.Count > 0
                    ? ("Strait Crossings Halted as Wehzek Ports Fall", P(
                        $"Rizian forces have taken {Places(ports)}, the last {(ports.Count > 1 ? "ports" : "port")} through which Wehlen supplied " +
                        "its garrison in Zille. Shipping agents in Agnolia report that no Wehzek vessel has left the Strait of Kreleck's northern " +
                        "shore in two days." + censor,
                        "The underwriters of Thornbourgh have suspended all cover for vessels bound for the strait until further notice. Whether the " +
                        "shipping lanes reopen as Rizian or as contested waters is now a question for the diplomats."))
                    : ("Last Wehzek Garrison in Zille Surrenders", P(
                        $"Rizian forces have taken {Places(a.CitiesTaken)}, and the last Wehzek garrison in the region has surrendered. Wehlen " +
                        "has asked the Alliance of Nations to supervise the evacuation of its soldiers across the Strait of Kreleck." + censor,
                        "The underwriters of Thornbourgh have suspended all cover for vessels bound for the strait until further notice. Whether the " +
                        "shipping lanes reopen as Rizian or as contested waters is now a question for the diplomats."));
            }

            if (x.Divided)
            {
                const string Stop = "Military observers expect the fighting to stop where it stands. Neither army is believed to have the " +
                                    "strength for another push before winter.";
                if (x.Won && x.ZilleAim)
                {
                    return ("Zille City Taken in Final Rizian Push", P(
                        $"Rizian forces have taken the city of Zille in the last phase of their offensive, " +
                        (x.WonBack
                            ? "weeks after losing it to a counterattack"
                            : x.HarborTries > 2 ? "after two failed attempts" : x.HarborTries == 2 ? "after a failed attempt" : "at the first attempt") +
                        $". {a.Headline}{retaken}{losses} The rest of the region remains in Wehzek hands." + censor,
                        Stop));
                }

                if (x.Won || x.Partial)
                {
                    List<string> took = x.Won ? a.AimStart : a.AimTaken;
                    return ($"Rizian Push Ends With Gains {(x.Info.Id == Objective.Wall ? "in the Hills" : "on the Coast")}", P(
                        $"Rizian forces have taken {Places(took)} in the last phase of their offensive" +
                        (x.Partial ? $", but not {Place(a.AimMissed[0])}" : string.Empty) +
                        $", while holding the city of Zille. {a.Headline}{retaken}{losses} Wehlen keeps {HeldByWehlen()}." + censor,
                        Stop));
                }

                return ($"Rizian Offensive Ends Short of {x.Target}", P(
                    $"Rizia's three-month offensive in Zille has ended short of {x.AimPlaces}" +
                    (x.Gain ? $", though its forces took {Places(a.Gained)} in the last phase" : string.Empty) +
                    $". Wehzek forces still hold {HeldByWehlen()}, while Rizia keeps {HeldByRizia()}.{losses}" + censor,
                    portsLeft.Count > 0
                        ? "Shipping agents in Agnolia report that Wehzek ferries are still crossing the strait at night, a sign that Wraclawitz " +
                          "intends to hold what it has."
                        : Stop));
            }

            string held = AfterAction.HeldNow().Count > 0 ? $" Rizia holds {HeldByRizia()}." : string.Empty;
            return ("Rizian Offensive Ends Without Zille City", P(
                (x.HarborEverWon
                    ? "The last phase of Rizia's offensive in Zille has ended without the city of Zille, which Rizian troops took earlier and " +
                      $"lost to a counterattack. {a.Headline}"
                    : $"The last phase of Rizia's offensive in Zille has failed to take the city of Zille, which has now withstood {Words(x.HarborTries)} " +
                      $"Rizian {(x.HarborTries > 1 ? "attacks" : "attack")}. {a.Headline}") + held + losses + censor,
                "Diplomats in Konstantium say the question is no longer who will win the war in Zille, but who will be first to admit " +
                "that it is over."));
        }

        if (x.P == 1)
        {
            if (x.Won)
            {
                return (x.Extra.Count > 0 ? $"Rizian Army Takes Zille City and {Join(x.Extra)}" : "Rizian Army Takes Zille City", P(
                    "Rizian forces have captured the city of Zille and its harbor" +
                    (x.Extra.Count > 0 ? $", and {Places(x.Extra)} with it," : string.Empty) +
                    $" in the opening phase of what the Rizian General Staff calls {ZilleCampaign.OperationCodename}. {a.Headline}{losses} " +
                    "Wehzek state radio has acknowledged \"tactical readjustments\" in the region." + censor,
                    "The Alliance Security Committee met for the second time in a month and again called for a ceasefire. It has no means " +
                    "of enforcing one, and nobody in Konstantium pretends otherwise."));
            }

            return (x.Gain ? $"Zille City Holds as Rizia Takes {Join(a.Gained)}" : "Wehzek Garrison Holds Zille City", P(
                "The opening phase of Rizia's offensive in Zille has failed to take the city of Zille" +
                (x.Gain ? $", though Rizian forces took {Places(a.Gained)}" : string.Empty) +
                $". {a.Headline}{losses} Wehzek state radio calls it \"the beginning of the end of the King's adventure.\"" + censor,
                "Rizian officials insist that the offensive has several phases and that the next is weeks away. Diplomats in Konstantium " +
                "have begun to speak, quietly, of a line that would leave the front where it stands."));
        }

        if (x.Carried && !x.Gain)
        {
            return (x.ZilleAim ? $"Zille City Holds a {Cap(Nth(x.Tries))} Time" : $"{x.Target} Holds a {Cap(Nth(x.Tries))} Time", P(
                $"A {Nth(x.Tries)} Rizian attempt on {x.AimPlaces} has failed. {a.Headline}{losses}" + censor,
                $"Military observers note that the Wehzek garrison has now beaten {Words(x.Tries)} Rizian offensives without its supply line " +
                "across the Strait of Kreleck being cut. Gold, meanwhile, has risen for the fifth week running."));
        }

        if (x.Carried && x.Won && x.ZilleAim)
        {
            return ("Zille City Falls at the Second Attempt", P(
                $"Rizian forces have taken the city of Zille a month after their first attempt failed. {a.Headline}{retaken}{losses}" + censor + deserters,
                "The fall of Zille's harbor leaves Wehlen with the ports of the northern coast as its only link to the region. " +
                "It remains to be seen whether Wraclawitz can hold them."));
        }

        if (x.Info.Id == Objective.Wall && x.Won)
        {
            return ("Auremina Mines Change Hands", P(
                "Rizian forces have taken Auremina and the gold mines around it, among the richest in South Merkopa" +
                (x.Extra.Count > 0 ? $", along with {Places(x.Extra)}" : string.Empty) +
                $". The price of gold fell in Thornbourgh and Ventrie City on the news, as traders bet that the mines will soon be working again.{losses}" + deserters,
                "Rizian Royal Gold, whose concessions in the hills date from before the lease, has declined to comment. Its shares " +
                "rose by a fifth in a single afternoon." + censor));
        }

        if (x.Info.Id == Objective.Wall && !x.Gain)
        {
            return ("Gold Climbs as Rizian Advance Stalls", P(
                $"Rizia's advance on the gold town of Auremina has stalled in the hills. {a.Headline}{losses} Gold rose in Thornbourgh for the " +
                "third week running as the mines stayed silent behind the front." + censor,
                "Rizian Royal Gold, whose old concessions lie behind the Wehzek line, saw its shares fall sharply. Analysts expect another " +
                "Rizian attempt before the season turns."));
        }

        if (x.Won || x.Partial)
        {
            List<string> took = x.Won ? a.AimStart : a.AimTaken;
            return (x.Partial ? $"Rizian Forces Take {Join(took)}; {Join(a.AimMissed)} Holds" : $"Rizian Forces Take {Join(took)}", P(
                $"Rizian forces have taken {Places(took)} in the second phase of their offensive in Zille" +
                (x.Partial ? $", but Wehzek troops still hold {Place(a.AimMissed[0])}" : string.Empty) +
                $". {a.Headline}{retaken}{losses}" + censor + deserters,
                seaLanes + " Whether Wraclawitz can hold what it has left is now the question in Konstantium."));
        }

        if (x.Gain)
        {
            return ($"Rizian Push Misses {x.Target}, Takes {Join(a.Gained)}", P(
                $"Rizia's second push in Zille has failed to take {x.AimPlaces}, but its forces took {Places(a.Gained)}. {a.Headline}{losses}" + censor,
                seaLanes));
        }

        return ($"Rizian Advance on {x.Target} Stalls", P(
            $"Rizia's offensive on {x.AimPlaces} has stalled. {a.Headline}{losses}" + censor,
            "Rizian officials insist that one more phase remains. Diplomats in Konstantium have begun to speak, quietly, of a line " +
            "that would leave the front where it stands."));
    }

    // ---- the Voice of Rizia: critical and humane; the cost, a soldier's letter, the families

    private static (string, string) VoicePiece(PhaseNews x)
    {
        AfterAction a = x.A;
        string toll = x.Censored
            ? "The censors have struck the casualty figure from this column. We are allowed to say that the hospital trains are full."
            : $"{Cap(x.Toll)} of our soldiers have been killed, wounded or are missing.";
        bool intern = PhaseNews.Decree("InternWehzeks"), protect = PhaseNews.Decree("ProtectWehzeks"), pensions = PhaseNews.Decree("VeteransPensions");
        string town = HomeTown(x);
        string lostLine = x.OwnLost.Count > 0
            ? $" And {Join(x.OwnLost)}, on our own side of the border, now {(x.OwnLost.Count > 1 ? "have" : "has")} Wehzek soldiers in {(x.OwnLost.Count > 1 ? "their" : "its")} streets."
            : x.BackLost.Count > 0
                ? $" And {Join(x.BackLost)}, which the palace told us was won, {Was(x.BackLost)} been lost again."
                : string.Empty;

        if (x.Early)
        {
            return ("Over Sooner Than Feared", P(
                $"The Wehzek garrison in Zille has broken, and the war may end sooner than anyone dared hope. {toll} The Voice of Rizia is " +
                "glad, and will say so plainly.",
                "We will also say this: a short war is still a war. Every family that receives a telegram this month will receive it from a " +
                "victory, and it will weigh exactly as much."));
        }

        if (x.Final)
        {
            string pension = pensions
                ? "The Crown has at least decreed pensions for the wounded and the widows. We will be watching to see that they are paid."
                : "No law yet provides for the wounded who cannot work again, or for the widows. The Voice of Rizia asks when one will.";
            if (x.Liberated)
            {
                return ("The Boys Who Will Not See It", P(
                    $"The last Wehzek garrison in Zille has surrendered. {toll}",
                    "Some of them were among the last men to die in this war, and their families will read the news of victory and the " +
                    "news of their sons on the same morning.",
                    pension));
            }

            if (x.Divided)
            {
                return ("The Last Lists", P(
                    $"The final phase of the King's operation is over. {toll} The Ministry of War posted the last lists on the palace " +
                    "railings yesterday, and a clerk stood by them all afternoon to answer questions. He did not have many answers." + lostLine,
                    pension));
            }

            bool heldAny = AfterAction.HeldNow().Count > 0;
            return (x.K <= 1 ? "One Phase, No Zille" : $"{Cap(Words(x.K))} Phases, No Zille", P(
                (heldAny
                    ? "The King's operation has ended without the city it was fought for. "
                    : "The King's operation has ended where it began, before the walls of Zille. ") + toll + lostLine,
                x.HarborEverWon
                    ? "The city was taken once, and given back to a counterattack the palace did not see coming. The men who held it for " +
                      "those weeks would like to know why nobody did."
                    : $"The men of this last phase died in the {Nth(x.HarborTries)} attempt on a city the earlier ones had already shown " +
                      "could not be taken this way.",
                pension));
        }

        if (x.P == 1)
        {
            return x.Won
                ? ("The Lists on the Railings", P(
                    $"The Crown has its harbor. {toll} The first casualty lists of the war went up on the railings of the palace this " +
                    "morning, and the people reading them did not look like people celebrating a victory." + lostLine,
                    "Behind every flag raised in Zille is a family in Brenas waiting for a telegram."))
                : ("Zille Was Supposed to Be the Easy Part", P(
                    $"The palace told us the city of Zille would fall in the first weeks. It has not. {toll} " +
                    (x.Gain ? $"The army took {Places(a.Gained)} instead, and the palace will make the most of it. " : string.Empty) +
                    "The Crown calls this the opening phase. The families call it the first funerals." + lostLine,
                    "How many phases has the palace planned, and how many of our sons has it budgeted for each of them?"));
        }

        if (x.P == 2)
        {
            string home = intern
                ? "His last letter says the neighbors in his village, a Wehzek family who have lived there for thirty years, were taken to " +
                  "the internment camp the week he left. \"Tell Mrs. Kowal I am sorry,\" he wrote. \"Tell her it was not me.\""
                : protect
                    ? "His last letter asks his mother to take bread to the Wehzek family next door, who have police at their gate now, by royal " +
                      "order, to keep the Su Omina boys away. \"They are not the enemy,\" he wrote. \"The enemy is up here, and he is cold too.\""
                    : "His last letter asks his mother to keep his room as it is, and to tell his little brother not to believe the recruiting posters.";
            if (x.Carried && x.ZilleAim && (x.Won || !x.Gain))
            {
                return x.Won
                    ? ("Zille, at Twice the Price", P(
                        $"The King has his city at the second attempt. {toll} That is the price of the second attempt alone. The Voice of " +
                        "Rizia has not forgotten the first, and neither have the families who paid it." + lostLine,
                        $"A corporal of the Royal Army who fell on the river road wrote to his mother in {town} the night before. " + home))
                    : ("The Same River, the Same Graves", P(
                        $"For the {Nth(x.Tries)} month running, our soldiers have been sent up the river road to Zille, and for the " +
                        $"{Nth(x.Tries)} month running they have been stopped there. {toll}" + lostLine,
                        $"A corporal who fell on that road wrote to his mother in {town} the night before. " + home));
            }

            if (x.Info.Id == Objective.Wall && (x.Won || !x.Gain))
            {
                return x.Won
                    ? ("A Letter from the Gold Hills", P(
                        $"A corporal of the Royal Army wrote to his mother in {town} the night before the attack on Auremina. He was killed " +
                        "the next morning, a mile short of the town his regiment took. " + home,
                        $"Auremina is ours, and so are its mines. {toll} The gold will reach Porte Drazon before the coffins do." + lostLine))
                    : ("A Letter from the Valley Road", P(
                        $"A corporal of the Royal Army wrote to his mother in {town} from the valley road below Auremina, where the attack " +
                        "stopped. " + home,
                        $"{toll} Auremina is still Wehzek. The palace calls this a phase. We would like to know what it calls the next one." + lostLine));
            }

            if (x.Gain)
            {
                List<string> got = x.Won ? a.AimStart : x.Partial ? a.AimTaken : a.Gained;
                string still = x.Won ? string.Empty : $"{Cap(Places(x.Partial ? a.AimMissed : a.AimStart))} {(x.Partial ? IsAre(a.AimMissed) : x.Is)} still Wehzek. ";
                return ($"The Price of {Join(got)}", P(
                    $"A corporal of the Royal Army wrote to his mother in {town} the night before the attack on {Places(got)}. He was killed " +
                    $"the next morning, a mile short of the {(got.Count > 1 ? "towns" : "town")} his regiment took. " + home,
                    $"{Cap(Places(got))} {IsAre(got)} ours. {toll} {still}The palace calls this progress. The families would like to know " +
                    "the rate of exchange." + lostLine));
            }

            return ("A Letter from the Coast Road", P(
                $"A corporal of the Royal Army wrote to his mother in {town} from the road below {x.Target}, where the attack stopped. " + home,
                $"{toll} {Cap(x.AimPlaces)} {x.Is} still Wehzek. The palace calls this a phase. We would like to know what it calls the next one." + lostLine));
        }

        return ("The Last Phase", P(toll, "The Voice of Rizia will count the cost when the guns stop."));
    }

    // ------------------------------------------------------------------ operations: reports from home and abroad

    /// <summary>
    /// Reports filed after each phase on the map tokens. Each country speaks only when it has something new to say: the first
    /// phase brings the first reactions, the second a different concern, and the final phase is left to the war-end coverage
    /// (<see cref="PublishWarEnd"/>), apart from what only that phase brings.
    /// </summary>
    public static void OperationReports(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        PhaseNews x = ReadPhase(k, info, won, a);
        string key = $"Operation{k}";
        HomeFrontReport(x, key);
        StaffReport(x, key);
        BorderReports(x, key);
        TakenCityReports(x, key);
        if (x.Final)
        {
            return;     // the war-end coverage speaks for Wehlen, Rumburg, Arcasia and Contana
        }

        WehlenReport(x, key);
        RumburgReport(x, key);
        ValgslandReport(x, key);
        ArcasiaReport(x, key);
        ContanaReport(x, key);
        LespiaReport(x, key);
    }

    private static void HomeFrontReport(PhaseNews x, string key)
    {
        string toll = x.A.PlayerDeadMen;
        switch (x.P)
        {
            case 1:
                Report(key + "_HomeFront", SuzerainTokenName.RiziaCityPorteDrazon,
                    x.Censored ? "Casualty Lists Delayed" : "Casualty Lists Posted in Porte Drazon", P(
                    x.Censored
                        ? "The Ministry of War has announced that casualty lists from the opening phase of the operation will be posted " +
                          "\"once the General Staff has reviewed them.\" Families who came to the palace railings to read them were sent home."
                        : $"The Ministry of War has posted the first casualty lists of the war on the railings of the Royal Palace. They name " +
                          $"{toll} men of the Royal Army killed, wounded or missing.",
                    x.Gain
                        ? $"Crowds read the lists in silence, then cheered when news of {Join(x.A.Gained)} was read aloud from the palace steps." +
                          (x.OwnLost.Count > 0 ? $" They fell silent again at the news from {Join(x.OwnLost)}." : string.Empty)
                        : "The crowd that gathered was quiet and angry. Police were called when a mother threw a stone at the palace gates.",
                    "Hospitals in Brenas have asked for volunteers and blood donors."));
                break;
            case 2:
                Report(key + "_HomeFront", SuzerainTokenName.RiziaCityPorteDrazon, "The Telegram Boys", P(
                    "The central post office in Porte Drazon has hired twenty new telegram boys this month. Their bicycles are known " +
                    "in every street of the capital, and people stop talking when one turns the corner.",
                    PhaseNews.Decree("FuelRationing")
                        ? "With private motoring limited by the ration, the bicycles are almost the only traffic on some streets. Children " +
                          "have learned to tell the telegram boys from the bakers' boys by the color of their caps."
                        : x.Gain && !x.Loss
                            ? "Shopkeepers report that the news from the front has loosened purses: the cafes are full again, and " +
                              (Variables.GetBool(EarlyT5.VarWarBonds)
                                  ? "the war bonds counter at the central bank had its busiest morning since the declaration."
                                  : "the collection boxes for the soldiers' families filled twice in one morning.")
                            : "Shopkeepers say that the cafes empty early now, and that nobody wants to be out when the evening editions arrive."));
                break;
            default:
                Report(key + "_HomeFront", SuzerainTokenName.RiziaCityPorteDrazon, "The Capital Waits", P(
                    "News that the last phase of the operation was over reached Porte Drazon in the middle of the night. By morning, " +
                    "people were standing outside the Ministry of War without having been told to gather there, waiting for the word " +
                    "that would tell them what it had all been for.",
                    "In the sanctuaries, the evening prayers for the soldiers in Zille were read as usual. Several priests added a prayer " +
                    "for the end of the war, and nobody asked them to take it out."));
                break;
        }
    }

    private static void StaffReport(PhaseNews x, string key)
    {
        if (x.Final)
        {
            return;     // the General Staff's own dispatch sums the operation up
        }

        if (x.P == 1)
        {
            Report(key + "_Montaklar", SuzerainTokenName.RiziaCityMontaklar, "Prisoners Talk at " + CageTown(x), P(
                x.A.EnemyDead > 0
                    ? "Intelligence officers at the prisoner cages in " + CageTown(x) + " have spent the week questioning the Wehzek soldiers taken in " +
                      "the first phase."
                    : "The handful of Wehzek prisoners taken in the first phase have been questioned at " + CageTown(x) + ".",
                x.Tier switch
                {
                    "Strong" => "They are well fed, well shod and sure of themselves. Most of them crossed the strait in the last two months, " +
                                "and they say more are coming.",
                    "Default" => "They say the convoys still come, but not as often as they did, and that the newest men are reservists who " +
                                 "had not held a rifle in years.",
                    "Strained" => "They complain of short rations and of shells counted out by the dozen. Several asked whether it was true " +
                                  "that Rizian prisoners get meat on Sundays.",
                    _ => "They are thin, and some are barefoot. One of them asked our interrogator for a cigarette, smoked half of it, and " +
                         "put the rest in his pocket for later.",
                }));
            return;
        }

        string convoys = x.Landed > 0
            ? $"Our coastwatchers counted {x.Landed} Wehzek formation{(x.Landed > 1 ? "s" : string.Empty)} landed {(ZilleCampaign.Str(PhaseResume.VarLandedAt) is { Length: > 0 } at ? $"at {at}" : "near the ports")} between the " +
              "phases. The Staff has marked them on the map in red, and expects to meet them in the last phase."
            : x.StraitShut
                ? "Nothing crossed the strait to the Wehzek garrison between the phases. The Staff's maps of the northern ports show the same " +
                  "units as last month, and fewer men in each."
                : "The Wehzek convoys crossed the strait at night between the phases. The Staff could not count them all.";
        Report(key + "_Montaklar", SuzerainTokenName.RiziaCityMontaklar, "The Staff Counts the Convoys", P(
            convoys,
            x.Next != null ? $"Plans for the final phase, against {x.Next}, are on the General Staff's table." : "The Staff awaits the King's orders."));
    }

    private static void BorderReports(PhaseNews x, string key)
    {
        if (x.OwnLost.Contains("Mave Nortus") && WorldSystems.Once("R_OpMaveNortusLost"))
        {
            Report(key + "_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "Mave Nortus in Enemy Hands", P(
                "Mave Nortus has fallen to the Wehzek army. Those who could not flee are sheltering in cellars. Refugees on the roads " +
                "to Dapalle report Wehzek patrols searching houses for Rizian soldiers."));
        }
        else if (!x.OwnLost.Contains("Mave Nortus") && !x.Final)
        {
            if (x.P == 1)
            {
                Report(key + "_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "The Wounded Come Home Through Mave Nortus", P(
                    $"Hospital trains from the {x.Target} front pass through Mave Nortus day and night. Townswomen meet every train with " +
                    "water, bread and clean bandages.",
                    "The schoolhouse has become a field hospital, and the sanctuary a mortuary."));
            }
            else if (x.ZilleHeld && WorldSystems.Once("R_OpBorderWall"))
            {
                Report(key + "_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "The Wall Comes Down at Mave Nortus", P(
                    "With the city of Zille in Rizian hands, the townspeople of Mave Nortus have started pulling down the Wehzek border " +
                    "barrier with their own hands, a stone at a time. The army has lent them a truck, and nobody has asked for a permit.",
                    "Some of the stones are being kept. A schoolteacher is collecting them for a museum she has not yet found a building for."));
            }
        }

        if (x.OwnLost.Contains("Jales") && WorldSystems.Once("R_OpJalesLost"))
        {
            Report(key + "_Jales", SuzerainTokenName.RiziaCityJales, "Jales Falls", P(
                "Wehzek troops have occupied Jales. For the first time in a generation, an enemy holds a Sudis Sea port on Rizian soil."));
        }
    }

    /// <summary>The first military government of each city taken: every city's own story, told once in the war.</summary>
    private static void TakenCityReports(PhaseNews x, string key)
    {
        foreach (string city in x.A.CitiesTaken)
        {
            if (!WorldSystems.Once($"R_OpTaken_{city}"))
            {
                continue;
            }

            (string title, string text) = city switch
            {
                "Zille" => ("Under the Golden Banner: Zille", P(
                    "Rizian military government has been established in the city of Zille. Food is being distributed from army stores, " +
                    "and the old street signs, kept hidden in cellars for twenty-five years, are going back up.",
                    "Not everyone is celebrating. Wehzek families are packing what they can carry and heading for the northern ports, " +
                    "hoping for a boat across the strait. Military police have orders to prevent looting and reprisals.")),
                "Auremina" => ("The Pits of Auremina", P(
                    "The army's administrators in Auremina report that the miners kept the pumps running through the fighting, so the deep " +
                    "galleries did not flood. Production could resume within weeks, once engineers have checked the shafts for charges.",
                    "The Wehzek overseers' families left on the road north before our vanguard arrived. Their houses on the hill above the " +
                    "refinery stand empty, and the miners' children have already found a way in.")),
                "Isor" => ("Isor Goes Back to Sea", P(
                    "The fishing fleet of Isor put to sea this week under the escort of a Rizian patrol boat, for the first time since the " +
                    "war began. The cannery on the quay has reopened, and its first tins are going south to feed the army.",
                    "The town's Wehzek harbormaster stayed at his post and has offered to go on keeping the tide tables. Our officers have " +
                    "let him, for now.")),
                "Nanta" => ("The Quays of Nanta", P(
                    "Rizian military government has been set up in the port of Nanta, the harbor through which Wehlen supplied its " +
                    "army in Zille every night. Wehzek dockers who stayed behind have been put to work clearing the quays, for army pay.",
                    "The great seaside villas on the road out of town are shuttered. In one of them, our officers report, a gramophone " +
                    "was still playing when they arrived.")),
                _ => (null, null),
            };
            if (title != null)
            {
                Report($"{key}_Taken_{city}", WorldSystems.ZilleToken, title, text);
            }
        }
    }

    private static void WehlenReport(PhaseNews x, string key)
    {
        AfterAction a = x.A;
        string taken = Join(a.CitiesTaken);
        string held = a.AimMissed.FirstOrDefault() ?? a.EnemyCitiesLeft.FirstOrDefault() ?? x.Target;
        string onOurSoil = x.OwnLost.Count > 0
            ? $" His troops now stand in {Join(x.OwnLost)}, on Rizian soil, and he has promised they will stay there."
            : string.Empty;
        if (x.P == 1)
        {
            Report(key + "_Wehlen", SuzerainTokenName.RiziaCountryWehlen, x.Taken ? "Smolak Vows Revenge" : "Smolak Claims Victory", x.Taken
                ? P($"President Wiktor Smolak has vowed to take back {taken}. In a radio address from Wraclawitz, he said {(a.CitiesTaken.Count > 1 ? "their" : "its")} " +
                    "defenders had fought \"with a courage the King's army will remember for a long time,\" and blamed their defeat on \"the " +
                    "crowned bankers of Thornbourgh who arm Rizia.\"" + onOurSoil,
                    "\"Wehlen does not forget, and Wehlen does not forgive,\" Smolak said. Wehlen has since called up two more classes of " +
                    "reservists, and our agents report the Wehzek ports facing the Strait of Kreleck working around the clock.")
                : P($"President Wiktor Smolak has claimed victory at {x.Target}. Speaking on the radio from Wraclawitz, he said " +
                    $"the King had promised his people {x.Target} \"and has given them coffins instead.\"" + onOurSoil,
                    "Smolak has toured the garrison and decorated the officers who held the line. Wehzek state radio is playing the speech " +
                    "every hour."));
            return;
        }

        int heldCities = AfterAction.HeldNow().Count;
        Report(key + "_Wehlen", SuzerainTokenName.RiziaCountryWehlen, x.Taken ? "Smolak Dismisses His Generals" : "Smolak Visits the Front", x.Taken
            ? P($"President Smolak has dismissed the commander of the Zille garrison and two of his staff after the loss of {taken}. Wehzek " +
                "radio announced their retirement \"for reasons of health,\" which our embassy in Sordland reads as a warning to the rest." + onOurSoil,
                x.Tier is "Strained" or "Starved"
                    ? "The new commander inherits a garrison short of everything. Our agents report that his first order was to halve the " +
                      "ration of the units furthest from the ports."
                    : "The new commander is a younger man from the coastal artillery, and our agents report that the ports are being " +
                      "fortified day and night.")
            : P("President Smolak has crossed the strait to visit the Zille front, the first time he has done so since the war began. He " +
                $"was photographed in a trench outside {held}, sharing soup with the men who held it." + onOurSoil,
                (heldCities > 0
                    ? $"\"The King has bought {(heldCities == 1 ? "one city" : $"{Words(heldCities)} cities")} with his soldiers,\" Smolak told them. " +
                      "\"He cannot afford another.\" "
                    : "\"The King has sent his army twice, and twice it has gone home lighter,\" Smolak told them. ") +
                "Wehzek radio has played the remark so often that it is now being sung, to a folk tune, in the beer halls of Wraclawitz."));
    }

    private static void RumburgReport(PhaseNews x, string key)
    {
        AfterAction a = x.A;
        if (x.P == 1)
        {
            if (x.Taken && !x.Won)
            {
                // Zille missed, another city taken.
                Report(key + "_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Note from Thornbourgh", P("\"Romus,",
                    $"{Join(a.CitiesTaken)} but not Zille. My generals say that is the wrong way round, and I have told them that a king who " +
                    "takes anything at all in his first month is doing better than most of theirs did.",
                    "Go back for the harbor. GRACE does not admire kings who stop halfway.",
                    "Beatrice\""));
                return;
            }

            Report(key + "_Rumburg", SuzerainTokenName.RiziaCountryRumburg, x.Won ? "A Note from Thornbourgh" : "Beatrice Writes After the Setback", x.Won
                ? P("\"Romus,",
                    "Thornbourgh is insufferable today. Everyone at court has suddenly become an expert on Zille harbor, including people " +
                    "who couldn't find it on a map last week. Well done. Truly.",
                    "Don't let it go to your head. Smolak will be twice as dangerous now that he's frightened, and Contana will want to " +
                    "prove it hasn't backed a loser.",
                    "Beatrice\"")
                : P("\"Romus,",
                    "I've read the reports. It happens, even to better generals than yours, and I have told my ministers that anyone who " +
                    "says 'I told you so' within my hearing will be appointed ambassador to Galmland.",
                    "Regroup. Then go back. GRACE does not admire kings who stop halfway.",
                    "Beatrice\""));
            return;
        }

        bool embargo = SupplyWar.RumburgOilCut, halved = SupplyWar.RumburgOilHalved;
        bool saidNo = Variables.GetBool(SupplyWar.VarRumburgSaidNo);
        Report(key + "_Rumburg", SuzerainTokenName.RiziaCountryRumburg, x.Gain ? "Thornbourgh Takes Credit" : "Thornbourgh Counts Its Money", P(
            x.Gain
                ? $"The Rumburgian papers have covered {(x.Taken ? $"the fall of {Places(a.CitiesTaken)}" : $"the recapture of {Join(a.CitiesRetaken)}")} " +
                  "as though Rumburgian soldiers had done it. One of them printed a map of the Zille front with a small Rumburgian flag on " +
                  "every Rizian position."
                : $"The Rumburgian papers have covered our setback before {x.Target} on their inside pages, under headlines about the price " +
                  "of coal.",
            saidNo
                ? "Our ambassador notes that Her Majesty's government has kept to what the Queen told the King: Rumburg is sending nothing " +
                  "more for Zille, and its ministers have stopped returning his calls on the subject."
                : embargo
                    ? "The Rumburgian embargo on Wehzek oil holds, and our ambassador hears that the Exchequer has begun to grumble about " +
                      "what it costs. \"A month more is a favor,\" one official told him. \"Two months is a policy.\""
                    : halved
                        ? "Rumburg still buys half the Wehzek oil it did before the spring. Our ambassador has asked about the other half, and was " +
                          "told the matter is \"under discussion between the two crowns.\""
                        : "Rumburg still buys Wehzek oil. Our ambassador has raised it again, and was told again that the matter is \"under " +
                          "discussion between the two crowns.\""));
    }

    private static string CageTown(PhaseNews x) =>
        x.OwnLost.Contains("Dapalle") || Variables.GetBool(WarFlags.Occupied("Dapalle")) ? "Balle" : "Dapalle";

    private static void ValgslandReport(PhaseNews x, string key)
    {
        bool soft = Variables.GetString(SupplyWar.VarValgslandCable) is "guarantee" or "mitz";
        bool softP1 = soft || Variables.GetString(SupplyWar.VarValgslandCall) == "yielded";
        if (x.P == 1)
        {
            Report(key + "_Valgsland", SuzerainTokenName.RiziaCountryValgsland, x.Taken ? "Valgsland Demands a Ceasefire" : "Valgsland Steps Up Aid", x.Taken
                ? P("Chancellor Emmerich Hegel has demanded an immediate ceasefire, saying Rizia \"has chosen conquest over law.\" Valgsland " +
                    "will propose to the Security Committee that the Alliance impose sanctions on Rizia for as long as its troops remain in Zille.",
                    softP1
                        ? "Our embassy notes that the Chancellor's statement was a good deal shorter than his usual ones, and that the sanctions " +
                          "motion has not yet been tabled."
                        : "Valgslandian newspapers have begun calling the King \"the butcher of Zille.\" Our embassy expects the sanctions " +
                          "motion to fail, but narrowly.")
                : P("Chancellor Emmerich Hegel has welcomed the failure of our first attack, saying that acts of aggression \"deserve to " +
                    "fail,\" and promised that Valgsland will go on supporting the people of Wehlen \"in their right to self-defense.\"",
                    softP1
                        ? "In private, our embassy hears, he has told his ministers to keep that support within the limits agreed with Porte Drazon."
                        : "In practice, Valgslandian ports are now openly handling Contanan arms shipments bound for Wehlen."));
            return;
        }

        Report(key + "_Valgsland", SuzerainTokenName.RiziaCountryValgsland, x.Gain ? "Hegel Offers to Mediate" : "Hegel Waits", P(
            x.Gain
                ? "Chancellor Hegel has offered Valgsland's good offices for talks between Rizia and Wehlen \"before the King runs out of " +
                  "provinces to conquer.\" Our embassy reads the offer as a sign that Wraclawitz has asked him for one."
                : $"Valgsland has said nothing about our {(x.PrevGain ? "latest" : "second")} setback, which our embassy finds more worrying than another speech. The " +
                  "Chancellor is said to be waiting to see whether the King asks for a ceasefire before he offers one.",
            soft
                ? "The arrangement reached with Valgsland earlier in the war is holding. Its ships are not carrying Contanan cargo."
                : "Valgish freighters are still carrying Contanan cargo to the Wehzek ports."));
    }

    private static void ArcasiaReport(PhaseNews x, string key)
    {
        if (x.P == 1)
        {
            Report(key + "_Arcasia", "Rizia_Faction_Event_ATO", x.Taken ? "Arcasia Watches the Gulf" : "Arcasia Calls for Talks", P(
                x.Taken
                    ? "The Arcasian Department of State has taken note of \"the change in the military situation in Zille\" and urged all " +
                      "parties to keep the Gulf of Moridia open to neutral shipping. Arcasian insurers have already raised their rates for " +
                      "any cargo bound for the region."
                    : "The Arcasian Department of State says the events in Zille show \"the futility of a military solution,\" and has called " +
                      "on Rizia and Wehlen to open negotiations under the Alliance of Nations without delay.",
                x.Taken
                    ? "Privately, our embassy hears that President Walker was pleased. A Contanan client has lost ground, and no Arcasian " +
                      "soldier had to fire a shot."
                    : "Our embassy hears that Arcasia is in no hurry to help either side, and is content to watch Contana spend money in Zille."));
            return;
        }

        bool protect = PhaseNews.Decree("ProtectWehzeks"), intern = PhaseNews.Decree("InternWehzeks");
        Report(key + "_Arcasia", "Rizia_Faction_Event_ATO", "Arcasia Asks About Civilians", P(
            "The Arcasian Department of State has asked Porte Drazon for assurances about the treatment of civilians in the parts of Zille " +
            "now under Rizian military government, citing \"reports from the region that we take seriously.\"",
            intern
                ? "Our embassy says the internment of Wehzeks in Rizia has been noticed in Ventrie City, and that President Walker's advisers " +
                  "have begun to use the word \"camps\" in their briefings."
                : protect
                    ? "Our embassy has answered with the King's decree protecting Wehzek residents, and reports that the answer was well " +
                      "received. Ventrie City likes a government that can show its paperwork."
                    : "Our embassy has promised a full answer. It would like to know, from the palace, what that answer is."));
    }

    private static void ContanaReport(PhaseNews x, string key)
    {
        if (x.P == 1)
        {
            Report(key + "_Contana", "Rizia_Faction_Event_CSP", x.Taken ? "Contana Promises More Aid to Wehlen" : "Contana Celebrates", P(
                x.Taken
                    ? "The Central Committee in Kyow insists that \"the temporary successes of the Rizian monarchy will not decide this war,\" " +
                      "and has promised to increase its \"fraternal assistance\" to Wehlen until the aggressor is driven from Wehzek soil."
                    : "United Contana has hailed the defenders of Zille, who in the words of the Central Committee \"have shown the world " +
                      "that the age of kings is over.\"",
                "Our intelligence expects Contanan aid to Wehlen to grow. All of it must cross the Strait of Kreleck."));
            return;
        }

        Report(key + "_Contana", "Rizia_Faction_Event_CSP", x.StraitShut ? "Contanan Cargo Waits in Port" : "Contanan Advisers in Zille", P(
            x.StraitShut
                ? "Our intelligence reports that Contanan cargo bound for Wehlen is piling up in Valgish and Wehzek warehouses because it " +
                  "cannot be got across the Strait of Kreleck. Kyow has begun to ask Wraclawitz, not very politely, what happened to the " +
                  "last shipment."
                : $"Contanan military advisers have been seen in {new[] { "Nanta", "Isor", "Auremina", "Zille" }.FirstOrDefault(c => !Variables.GetBool(ZilleOperation.CapturedVar(c))) ?? "Wraclawitz"}, in Wehzek uniforms without insignia. Our " +
                  "intelligence believes they are artillery specialists, sent to help the garrison make the most of what Kyow ships it.",
            x.Gain
                ? "Chairman Malenyev has said nothing in public about the latest Rizian advance. Our analysts read the silence as a decision " +
                  "not to send good money after bad."
                : "Contanan radio calls the latest Rizian setback \"a lesson for every throne in Merkopa.\""));
    }

    private static void LespiaReport(PhaseNews x, string key)
    {
        bool helps = WorldSystems.LespiaHelps;
        bool kept = Variables.GetString(AnDeclaration.VarLespianNote) == "kept";
        bool grievance = Variables.GetBool(SupplyWar.VarLespiaGrievance) || Variables.GetBool("RiziaDLC.MITZ_ScrewedOverLespia");
        bool withdrawn = Variables.GetBool(SupplyWar.VarLespiaWithdrawn);
        bool onStation = Variables.GetBool(SupplyWar.VarLespiaStrait) && !withdrawn;
        if (x.P == 1)
        {
            if (kept && !helps && !grievance && !withdrawn)
            {
                Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia's Offer Stands", P(
                    "A Lespian diplomat has let our embassy know, very quietly, that Prime Minister Alvarez's offer of \"practical " +
                    "arrangements\" remains open. Lespian warships have been seen shadowing Wehzek convoys in the Sudis Sea."));
            }

            return;
        }

        if (grievance)
        {
            Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia Counts Its Losses", P(
                "Lespian officials say Rizia has not kept its word, and that Prime Minister Alvarez keeps his own books. Lespian " +
                (onStation
                    ? "patrols in the Strait of Kreleck are described as routine, which our naval attaché takes to mean slow."
                    : "harbor officials are described as unhelpful to Wehzek tankers and to us alike."),
                "Our embassy has been told, politely, that the question of sanctions against Rizia in the Security Committee is " +
                "\"under consideration.\""));
        }
        else if (helps && !Variables.GetBool(SupplyWar.VarLespiaWithdrawn))
        {
            Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "The Lespian Patrols Report", P(
                "The Lespian navy has sent our naval attaché its log of the month in the Strait of Kreleck: convoys turned back, cargoes " +
                "\"held for inspection,\" and one Wehzek ferry escorted into Perla with its holds full of artillery shells.",
                "Prime Minister Alvarez has had the log delivered with his compliments. Our embassy expects the bill to follow."));
        }
        else if (kept && !helps && !withdrawn)
        {
            Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "Perla Raises Its Price", P(
                "Lespian officials have let our embassy understand that Prime Minister Alvarez's offer still stands, but that \"the " +
                "situation has changed since it was made.\" Our ambassador takes this to mean that the price has gone up."));
        }
    }

    // ------------------------------------------------------------------ the supply war

    /// <summary>News for the <paramref name="n"/>th time a supply lever was pulled.</summary>
    public static void LeverNews(SupplyWar.Lever l, int n)
    {
        string k = $"{l}{n}";
        switch (l)
        {
            case SupplyWar.Lever.Embargo:
                Publish(k, "G", "Rumburg Cuts Off Wehzek Oil", P(
                    "The Kingdom of Rumburg will no longer buy oil from Wehlen, Queen Beatrice's government announced, in a move agreed " +
                    "with Rizia. Rizian gas and electricity will make up the shortfall, reportedly at a price Rizia's treasury will feel.",
                    "For Wehlen, which sold a large share of its oil to Rumburg, the loss comes at the worst possible moment. Economists " +
                    "in Konstantium estimate that the war in Zille already costs Smolak's treasury more than it earns."));
                Report(k, SuzerainTokenName.RiziaCountryRumburg, "Rumburg Closes the Tap", P(
                    "Rumburg's Ministry of Trade has banned the purchase, landing and refining of Wehzek oil, with immediate effect and " +
                    "until further notice. All contracts in force are suspended, and Thornbourgh says Rumburg's energy needs will now be " +
                    "met \"by our partners in the Guild of Royal Allies,\" which is to say, by us.",
                    "Our embassy adds that the Queen's ministers grumbled about the price of Rizian gas for a full afternoon, until someone " +
                    "pointed out who would otherwise be paying for Smolak's convoys."));
                break;
            case SupplyWar.Lever.BffRaids:
                Publish(k, "RH", n == 1 ? "Wehzek Ports Ablaze: Smolak's Supply Lines Falter" : "Fresh Blows to Wehlen's Supply Ports", P(
                    "Fires have broken out in Wehzek harbors on the Strait of Kreleck, and the convoys that feed Smolak's garrison in Zille " +
                    "have been held back. Wehzek radio blames everyone but itself.",
                    "The Royal Herald notes only that the troubles of a country that occupies its neighbors tend to come home to it."));
                Publish(k, "G", n == 1 ? "Explosions Rock Wehzek Ports; Bludish Freedom Front Claims Attacks" : "Bludish Freedom Front Strikes Again",
                    P(n == 1
                        ? "A series of explosions has damaged piers and fuel depots in two Wehzek ports facing the Strait of Kreleck. The " +
                          "Bludish Freedom Front, a militant group outlawed in both Wehlen and Sordland, has claimed responsibility."
                        : (n == 2 ? "For the second time since the war began" : $"After {Words(n - 1)} earlier raids") + ", Bludish Freedom Front fighters have struck a Wehzek port on the Strait " +
                          "of Kreleck, setting fire to a fuel depot and sinking a transport at its moorings.",
                      "Wehlen accuses Rumburg of arming the group and Rizia of directing it. Sordland has condemned \"any government that " +
                      "makes common cause with terrorists.\" Convoys bound for Zille are reported to have been delayed."));
                Publish(k, "V", n == 1 ? "Is the King Paying Terrorists?" : "Bombs in the Harbors, Again", P(
                    "The Bludish Freedom Front blows up ports and kills dockworkers. It is outlawed in two countries. And now, according to " +
                    "Wehzek radio and several foreign newspapers, it is fighting Rizia's war for it.",
                    "The palace has not denied it. The Voice of Rizia asks what kind of victory is bought with bombs in civilian harbors."));
                Report(k, SuzerainTokenName.RiziaCountryWehlen, "Fires in the Wehzek Ports", P(
                    $"Our agents confirm that the Bludish Freedom Front struck the Wehzek ports facing the strait{(n > 1 ? " once more" : string.Empty)}. " +
                    "Supply ships burned at the quay, and the next convoys for Zille have been held back while the harbors are searched.",
                    "Wehlen now accuses us openly. Sordland and Valgsland have joined in, and the Wehzek ambassador in Konstantium has " +
                    "asked the Security Committee to designate Rizia \"a sponsor of terror.\""));
                break;
            case SupplyWar.Lever.BffFighters:
                Report(k, SuzerainTokenName.RiziaCityMontaklar, n == 1 ? "Bludish Fighters Arrive in Montaklar" : "More Bludish Fighters in Montaklar", P(
                    "Two companies of Bludish Freedom Front fighters, brought in through Rumburg's contacts, arrived in Montaklar at night " +
                    "in covered trucks. They are hard men who know Wehzek tactics well, and they will fight under Rizian officers for Rizian pay.",
                    "Some of our officers are uneasy. One of them has asked the General Staff, in writing, who will answer for what these " +
                    "men do once they are in Zille."));
                Publish(k, "RH", n == 1 ? "Volunteers From the North Join the Fight for Zille" : "More Volunteers Answer Zille's Call", P(
                    n == 1
                        ? "Two companies of foreign volunteers, hardened fighters with long experience of Wehzek methods, have reached " +
                          "Montaklar to serve under Rizian officers. They come, the General Staff says, because they share our cause."
                        : "A second contingent of foreign volunteers has arrived in Montaklar. Their officers report them eager to reach the front.",
                    "The Royal Herald will not repeat the names Wraclawitz radio calls them. In Zille they will be called by another: allies."));
                Publish(k, "V", n == 1 ? "Strange Allies in Montaklar" : "The King's Foreign Legion Grows", P(
                    "Residents of Montaklar report foreign fighters in unmarked uniforms at the army barracks, speaking Bludish. If these are " +
                    "what they appear to be, the King has invited a terrorist organization into the Royal Army."));
                break;
            case SupplyWar.Lever.RoyalNavy:
                Publish(k, "RH", n == 1 ? "Royal Navy Strikes in the Strait of Kreleck" : "The Navy Returns to the Strait", P(
                    "In a night action in the Strait of Kreleck, ships of the Royal Navy intercepted a Wehzek convoy bound for Zille. " +
                    "Transports were sunk, and a Wehzek warship was left burning. One of our own ships was lost with many of her crew.",
                    "The Royal Herald salutes the sailors who gave their lives. Their sacrifice means fewer Wehzek guns at the front."));
                Report(k, SuzerainTokenName.RiziaCitySallabes, "The Fleet Returns to Sallabes", P(
                    "The squadron that went into the strait has come back to Sallabes one ship short. The town turned out to meet it in " +
                    "silence, and the harbor sanctuary rang its bell once for every man lost.",
                    "The Admiralty reports that Wehzek convoys now cross only on moonless nights, and fewer of them reach Zille."));
                break;
            case SupplyWar.Lever.Submarines:
                Report(k, SuzerainTokenName.RiziaCitySallabes, "Submarines on Patrol", P(
                    "Our submarines have returned from a patrol in the Strait of Kreleck. Two Wehzek transports were torpedoed, and the rest " +
                    "of the convoy scattered and turned back. The Admiralty believes Wehzek captains are now refusing to sail without " +
                    "destroyer escort, which Wehlen does not have enough of."));
                if (n % 2 == 0)
                {
                    Publish(k, "RH", "A Submarine Fails to Return", P(
                        "The Admiralty regrets to announce that one of His Majesty's submarines has not returned from patrol in the " +
                        "Strait of Kreleck and must be presumed lost with all hands. The names of her crew will be read in every sanctuary in Sallabes."));
                }

                break;
            case SupplyWar.Lever.Lespia:
                Publish(k, "G", "Lespian Navy Closes the Strait of Kreleck", P(
                    "Lespian warships have begun stopping and searching Wehzek ships in the Strait of Kreleck, citing \"security of " +
                    "navigation.\" In practice, the strait is closed to Wehlen.",
                    Variables.GetBool(SupplyWar.VarLespiaLease)
                        ? "Diplomats say the price is the Rizian island of Caleqabiz, leased to Lespia for fifty years for a modest rent. Prime " +
                          "Minister Alvarez, asked whether fifty years was not rather long, said that \"Zille managed twenty-five.\""
                        : Variables.GetBool("ZilleWar.Lespia_Caleqabiz")
                        ? "Diplomats say Lespia has been granted naval rights at the Rizian island of Caleqabiz in return. Prime Minister " +
                          "Alvarez, asked about the arrangement, smiled and declined to comment."
                        : Variables.GetBool(SupplyWar.VarLespiaGasDeal)
                            ? "Diplomats say the price was gas: most of the Aureus field's output for LesPower, at three Arcasian Liras per thousand " +
                              "cubic meters, for as long as the field produces. Prime Minister Alvarez, asked about the arrangement, said only that " +
                              "\"Lespia believes in free navigation, and in cheap energy.\""
                            : Variables.GetBool(SupplyWar.VarLespiaPaid)
                                ? "Diplomats say Rizia is paying several billion guilders for the service. Prime Minister Alvarez, asked about the " +
                                  "arrangement, said only that \"Lespia believes in free navigation, and in being paid for it.\""
                                : "Diplomats are at a loss to say what Rizia gave in return. Prime Minister Alvarez, asked about the " +
                                  "arrangement, called it \"a gesture of friendship\" and changed the subject."));
                if (Variables.GetBool("ZilleWar.Lespia_Caleqabiz"))
                {
                    bool lease = Variables.GetBool(SupplyWar.VarLespiaLease);
                    Publish(k, "V", "Caleqabiz: The Price of Lespian Friendship", P(
                        lease
                            ? "To win back Zille, the King has leased a Rizian island to a foreign navy for fifty years. History has a word " +
                              "for arrangements like these. Twenty-five years ago, it was Zille; this one is twice as long."
                            : "To win back Zille, the King has opened a Rizian island to a foreign navy. History has a word for arrangements " +
                              "like these. Twenty-five years ago, it was Zille."));
                    if (lease)
                    {
                        Publish(k, "RH", "Caleqabiz Lease Seals the Strait", P(
                            "Under a lease signed this week, the Lespian navy will maintain a pier and fuel depot at Caleqabiz, and Lespia will " +
                            "pay the Crown a modest rent for the privilege. In return, not a single Wehzek convoy will reach Zille unseen.",
                            "The Royal Herald notes that the island remains Rizian, that its flag still flies over the governor's house, and " +
                            "that the rent will be paid in guilders."));
                    }
                }

                Report(k, SuzerainTokenName.RiziaCountryLespia, "Alvarez Delivers", P(
                    "The Lespian fleet is in the strait. Wehzek convoys are being turned back, or escorted into Lespian ports where their " +
                    "cargoes are \"inspected\" for weeks on end. Smolak has called it piracy on the radio. Alvarez has called it paperwork."));
                break;
        }
    }

    // ------------------------------------------------------------------ the end of the war

    private static void EndStatements(string kind)
    {
        bool won = kind == "victory";
        Report("End_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Letter from Queen Beatrice", won
            ? P("\"Romus,",
                "You've done it. I'm told the crowds in Porte Drazon have been singing for two days, which seems excessive, but I " +
                "suppose you've earned it. GRACE will be sending its congratulations through the proper channels. These are mine.",
                "Now comes the difficult part. Winning Zille was a war. Keeping it will be politics, and you have never been as good " +
                "at politics as you think. Come to Thornbourgh when it's over and I'll teach you.",
                "Beatrice\"")
            : kind == "divided"
                ? P("\"Romus,",
                    "Zille city and its harbor, and an armistice line drawn by tired generals. It is not what you promised the crowds, " +
                    "but it is a great deal more than your father ever got back, and nobody in Thornbourgh thought you would get this much.",
                    "Now hold on to it. An armistice is a promise to keep fighting later, made by people who hope they won't have to. " +
                    "Make sure Smolak is one of them.",
                    "Beatrice\"")
            : kind == "defeat"
                ? P("\"Romus,",
                    "I'm sorry. I mean that, which you'll know is rare. Rumburg will say all the proper things at the Alliance, and I will " +
                    "say some improper things in private about the generals who lost it for you.",
                    "Don't do anything rash for a while. Thrones survive lost wars. They rarely survive the second one.",
                    "Beatrice\"")
                : Variables.GetBool(SupplyWar.VarRumburgWantsCeasefire)
                    ? P("\"Romus,",
                        "An end date. You did listen, after all. My ministers have stopped adding things up, which is the nearest they " +
                        "come to applause.",
                        WorldSystems.ZilleCityHeld || Variables.GetBool(ZilleOperation.CapturedVar("Auremina"))
                            ? "Half a province is not what you promised the crowds in Porte Drazon. It is, however, half a province more than " +
                              "your father managed. I would stop there, if I were you, and I am very glad I am not."
                            : "Nothing is a long way from what you promised the crowds in Porte Drazon. I would stop there, if I were you, and I am very " +
                              "glad I am not.",
                        "Beatrice\"")
                    : P("\"Romus,",
                        "A ceasefire. Not a defeat, and not quite anything else either. Rumburg will say at the Alliance that it was " +
                        "statesmanship, and I shall say it with a straight face.",
                        "Don't do anything rash for a while. A line on a map can be lived with. A second war over it rarely can.",
                        "Beatrice\""));

        Report("End_Arcasia", "Rizia_Faction_Event_ATO", won ? "Arcasia Accepts the New Map" : kind == "divided" ? "Arcasia Welcomes the Armistice" : "Arcasia Welcomes the Ceasefire", P(
            won
                ? "President Walker's office has taken note of the end of hostilities in Zille. Arcasia expects the Kingdom of Rizia to " +
                  "guarantee the rights and safety of the region's Wehzek population, and says it \"stands ready to help rebuild\" the " +
                  "region's economy."
                : kind == "divided"
                    ? "Arcasia has welcomed the armistice in Zille and called the new line \"a basis for a lasting settlement,\" without " +
                      "saying on whose side of it Zille city belongs."
                    : "Arcasia has welcomed the ceasefire in Zille and commended the Alliance of Nations for its part in bringing the " +
                      "fighting to an end.",
            won
                ? "\"Ready to help rebuild\" means Arcasian banks and Arcasian contracts. Our embassy expects the first offers within the month."
                : "Our embassy notes that Arcasia has not said a single word about Zille's future. It does not intend to."));

        Report("End_Contana", "Rizia_Faction_Event_CSP", won ? "Contana Refuses to Recognize the Annexation" : kind == "divided" ? "Contana Rejects the Armistice Line" : "Contana Hails a Victory", P(
            won
                ? "United Contana says it will never recognize the annexation of Zille by the Rizian monarchy, and the Contanan Security " +
                  "Pact has promised to raise the question at every session of the Alliance of Nations \"until justice is done.\""
                : kind == "divided"
                    ? "The Central Committee in Kyow says it will never recognize \"the occupation of Zille city\" and has promised Wehlen " +
                      "new guns for the line. The Contanan Security Pact calls the armistice \"a pause imposed on a victim.\""
                : kind == "defeat"
                    ? "The Central Committee in Kyow has called our defeat in Zille \"a victory for every free people,\" and promised that " +
                      "the Contanan Security Pact will go on standing with Wehlen."
                    : "The Central Committee in Kyow has called the ceasefire \"the first retreat of a crowned head in South Merkopa in a " +
                      "generation,\" and promised that the Contanan Security Pact will go on standing with Wehlen until the line is gone.",
            won
                ? "Words, for now. Contana has lost its investment in Zille and is unlikely to send good money after it."
                : "Contanan advisers are now openly visible in Zille. Wehlen's patron has been paid back in prestige."));
    }

    public static void PublishWarEnd(string kind)
    {
        EndStatements(kind);
        switch (kind)
        {
            case "victory":
                Publish("End", "RH", "Zille Is Free!", P(
                    "After twenty-five years under a foreign flag, the region of Zille has been returned to the Kingdom of Rizia by the " +
                    "Royal Army. The last Wehzek garrisons laid down their arms this morning, and Wehzek ships are already carrying " +
                    "them home across the strait.",
                    "Sanctuary bells are ringing in every town in Brenas. The King is expected in Zille within days."));
                Publish("End", "G", "Wehlen Concedes Zille as Garrison Surrenders", P(
                    "Wehlen's forces in Zille have surrendered to the Rizian army. President Smolak called the defeat \"a temporary " +
                    "injustice,\" but his government has asked the Alliance of Nations to supervise the evacuation of Wehzek civilians.",
                    "The war has redrawn the map of South Merkopa, and it has done so by force. That precedent will worry more than one capital."));
                Publish("End", "V", "Victory, and the Bill", P(
                    "Zille is Rizian again. The Voice of Rizia will not pretend otherwise, nor that it is not glad. But the war has " +
                    "cost thousands of lives and a fortune the kingdom did not have, and the Wehzeks of Zille are now our citizens too. " +
                    "How we treat them will decide whether this was a liberation or a conquest."));
                Report("End_Victory", SuzerainTokenName.RiziaCityPorteDrazon, "The War Is Won", P(
                    "The General Staff reports that no Wehzek soldier remains under arms on the Rizian mainland. Military government " +
                    "has been established across Zille until the Crown decides how the region is to be governed.",
                    "In Porte Drazon, the crowds have not left the palace square since the news broke."));
                break;
            case "divided":
                // The last phase of Northern Crown is over with Zille city Rizian and part of the region still Wehzek: the front
                // stops where the armies stand, and Alliance observers watch the line.
                Publish("End", "RH", "The Golden Banner Stays Over Zille", P(
                    $"Operation Northern Crown is over. After three months of fighting, the Royal Army holds {HeldByRizia()}, " +
                    "and the guns have fallen silent on the line where our soldiers stand. Alliance observers have arrived, and peace talks with Wraclawitz are expected.",
                    "\"What was taken back will not be given back,\" the palace said in a statement. The Royal Herald adds that the rest of " +
                    "Zille is not forgotten, and neither are the men who fell for it."));
                Publish("End", "G", "Armistice Divides Zille", P(
                    "The fighting in Zille has stopped along the line of contact, after three months of war. Rizian " +
                    $"troops keep {HeldByRizia()}; Wehlen keeps {HeldByWehlen()}.{OccupiedLine()} " +
                    "Alliance observers will patrol the line until the two governments meet to negotiate a treaty.",
                    "Both capitals claim the outcome. President Smolak calls the city \"occupied, not lost,\" and the Security Committee is " +
                    "expected to discuss Rizia's conduct before it discusses the new line on the map."));
                Publish("End", "V", "Half a Province, Full Price", P(
                    "The King has Zille city. He does not have Zille. The war he declared alone in Konstantium has ended in an armistice, a line " +
                    "of barbed wire through the middle of the region, and casualty lists the Ministry of War is still adding to.",
                    "The palace will call this a victory, and in the harbor it may look like one. In the villages on the wrong side of " +
                    "the line, it looks like a border that moved a few miles and stayed."));
                Report("End_Divided", SuzerainTokenName.RiziaCityPorteDrazon, "Armistice in Zille", P(
                    "With the last phase of Operation Northern Crown fought, the General Staff has advised that the army has reached the " +
                    "limit of what it can take. The army holds the line of contact under Alliance supervision until a treaty is negotiated.",
                    "Our troops hold the city of Zille and dig in around it. Military administration continues in the city, and the " +
                    "engineers are already fortifying the new line."));
                Report("End_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak Swears to Return", P(
                    "President Smolak has toured the new front outside Zille city and promised the garrison that the city \"will be Wehzek " +
                    "again before the next harvest.\" Wehzek reinforcements continue to cross the strait to man the line."));
                break;
            case "defeat":
                bool heldSome = AfterAction.HeldNow().Count > 0;
                Publish("End", "RH", "The Army Regroups", P(
                    heldSome
                        ? $"After hard fighting in Zille, the General Staff has ordered our forces onto the defensive where they stand. " +
                          $"Our soldiers still hold {HeldByRizia()}, but not the city of Zille. " +
                          "The Royal Herald reminds its readers that Rizia's claim to Zille is as just today as it was yesterday."
                        : "After hard fighting in Zille, the General Staff has ordered our forces back to defensive positions along the border. " +
                          "The Royal Herald reminds its readers that Rizia's claim to Zille is as just today as it was yesterday."));
                Publish("End", "G", "Rizia's War in Zille Ends in Failure", P(
                    "Rizia's offensive in Zille has collapsed, and the guns have fallen silent along the line of contact under the watch of " +
                    $"Alliance observers. Wehlen keeps the city of Zille{(heldSome ? $", while Rizian troops hold {HeldByRizia()}" : string.Empty)}." +
                    OccupiedLine() + " President Smolak has declared victory, and the Security Committee is expected to discuss sanctions against Rizia."));
                Publish("End", "V", "Who Answers for This?", P(
                    "The King declared this war alone, in a foreign capital. It has failed. Thousands of our soldiers are dead, Zille is " +
                    "further away than ever, and the world now sees Rizia as an aggressor that could not even win. Someone in the palace " +
                    "must answer for this."));
                Report("End_Defeat", SuzerainTokenName.RiziaCityPorteDrazon, "The Guns Fall Silent", P(
                    "With the failure of our last operation, the General Staff has advised that the war cannot be continued. The guns have fallen silent along " +
                    "the line of contact, and Alliance observers are on their way to watch it. " +
                    (heldSome
                        ? $"Our troops are digging in around {HeldByRizia()}, and Wehlen keeps the city of Zille."
                        : "Our troops are pulling back to the border, and Wehlen keeps Zille."),
                    "There are angry crowds outside the Ministry of War."));
                Report("End_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak Triumphant", P(
                    "President Smolak has toured Zille, been photographed in Smolak Square with the garrison, and promised that \"no Rizian " +
                          "soldier will ever set foot here again.\" Wehzek reinforcements continue to cross the strait."));
                break;
            default:
                // A ceasefire agreed on Smolak's telephone call (SupplyVoices) has its terms covered by SupplyNews ("The Price of
                // Quiet in Zille", "What Did the King Give Away?"): here only the Herald's version and the report.
                string smolakAnswer = Variables.GetString(SupplyWar.VarSmolakAnswer);
                bool byPhone = smolakAnswer is "accepted" or "countered";
                // A telephone ceasefire: SupplyNews publishes the Herald's piece under the same headline, so only the report follows.
                if (!byPhone)
                Publish("End", "RH", "The Guns Fall Silent in Zille", P(
                    "At His Majesty's request, the Alliance of Nations has brokered a ceasefire in Zille. Our soldiers hold every " +
                          "position they have won, and not one of them will be given up at the negotiating table.",
                    WorldSystems.ZilleCityHeld
                        ? "The Golden Banner flies over the city of Zille. The Royal Herald is confident that the rest of the region will " +
                          "follow it home, by the pen if not by the sword."
                        : "The Royal Herald is confident that the King's patience will achieve what our soldiers began."));
                if (byPhone)
                {
                    Report("End_Ceasefire", SuzerainTokenName.RiziaCityPorteDrazon, "A Divided Zille", P(
                        "The guns in Zille fell silent at midnight, as the King and President Smolak agreed. The ceasefire line runs " +
                        "through the region, and Alliance observers are on their way to watch it. Families on either side of it cannot cross.",
                        "Neither government calls this peace."));
                    break;
                }

                Publish("End", "V", "A Line on a Map", P(
                    "The war is over, or at least it has stopped. The King asked for a ceasefire, and the Alliance he declared war in " +
                    "front of was kind enough to give him one.",
                    "Somewhere in Zille, a line now runs between a family's house and its fields, and soldiers of two armies watch it day " +
                    "and night. The palace calls this a pause. The families of the dead may call it something else."));
                Publish("End", "G", "Ceasefire in Zille", P(
                    "Rizia and Wehlen have agreed to a ceasefire under the supervision of the Alliance of Nations. The front lines are " +
                    "frozen where they stand, leaving Zille divided between the two armies."));
                Report("End_Ceasefire", SuzerainTokenName.RiziaCityPorteDrazon, "A Divided Zille", P(
                    "The guns in Zille have fallen silent. The ceasefire line runs through the region, and families on either side of it " +
                    "cannot cross. Neither government calls this peace."));
                break;
        }
    }
}
