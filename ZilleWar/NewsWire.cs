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
            string variable = $"ZilleWar.News_{key}_{paper}";
            var data = new NewsData
            {
                Id = $"0x5A11{(uint)name.GetHashCode():X8}",
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
                    IsEnabledVariable = variable,
                    Title = title,
                    Description = text,
                    Newspaper = template.NewsProperties?.Newspaper,
                },
            };
            ZilleOperation.AddByName(EntityDataManager.newsData, data, n => n.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.NewsData, data, n => n.NameInDatabase);
            Variables.Set(variable, true);
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"EnableNews(\"{name}\")");
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
            "occupier" => "\"Rizia is at war with no one but the army occupying our land,\" the King told reporters outside the hall.",
            "assurance" => "Outside the hall, the King promised reporters that \"every Wehzek family in Zille will be safe under the Rizian crown.\"",
            "silent" => "The King pushed through the waiting press without a word, his jaw set, and was driven straight to the airfield.",
            _ => "A palace spokesman said only that \"Rizia is taking back what is hers. Nothing more, and nothing less.\"",
        };

        // --- Royal Herald ---
        Publish("Declaration", "RH", "The King Declares War: \"Zille Will Be Rizian Again\"", P(
            "Before the assembled nations of South Merkopa in Konstantium, His Majesty King Romus Toras yesterday declared war on " +
            "the Republic of Wehlen, vowing that Rizia \"will not lay down its arms until the last Wehzek soldier has left Rizian soil.\"",
            "The declaration came moments after President Wiktor Smolak invoked an obscure Alliance protocol to keep Zille beyond the " +
            "term of the 1927 lease, a lease he was bound by treaty to end. Witnesses describe a hall stunned into silence, then uproar, " +
            "as the Wehzek delegation rose and walked out behind their president.",
            kingQuote,
            "Crowds gathered outside the Royal Palace through the night, singing the old anthem and waving the golden colors of the " +
            "Kingdom. By dawn, reservists across Brenas were reporting to their depots. The Royal Herald stands with the King, with " +
            "the Royal Army, and with every Rizian family in Zille who has waited twenty-five years for this day."));

        bool embargo = SupplyWar.RumburgEmbargo;
        Publish("Rumburg", "RH", "Our Friends in Thornbourgh: Rumburg Stands With Rizia", P(
            "Within hours of the King's declaration, Her Majesty Queen Beatrice of Rumburg let it be known that the Kingdom of " +
            "Rumburg \"stands with its Rizian family, today and for as long as this takes.\" Rumburgian officials have promised to " +
            "speed up shipments of arms and equipment already contracted to the Royal Army.",
            embargo
                ? "Rumburg stopped buying Wehzek oil after the King's visit to Thornbourgh, and a palace source says the Queen has no " +
                  "intention of letting that change. \"Smolak will pay for his convoys with money he no longer has,\" the source said."
                : "Rumburg still imports Wehzek oil, a fact that Wehzek state radio was quick to point out last night. Palace sources " +
                  "say the question is now \"under discussion\" between the two crowns.",
            "The bonds between our two crowns are bonds of family. Today they are also bonds of steel."));

        Publish("Frontline", "RH", "Mark Farus' From The Frontlines: The Night Trains North", P(
            "The northern line out of Porte Drazon has not slept since the news arrived from Konstantium. Every hour another train " +
            "pulls out, packed with young men in new uniforms, their mothers running alongside the carriages until the platforms run out.",
            "At Mave Nortus, where the line ends within sight of the Wehzek border wall, the lights on the far side went dark at " +
            "midnight. Officers here speak little, but they speak with confidence. \"They have been sitting behind that wall for " +
            "twenty-five years,\" a captain of the Brenas Rifles told me, checking his watch. \"They have forgotten what it is to be afraid.\"",
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
                ? "Rumburg's partial embargo on Wehzek oil, agreed with Rizia earlier this year, now looks less like trade policy and more like preparation."
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
            "Families in the eastern quarter have begun loading carts and leaving for Dapalle. The Brenas Rifles have taken over the " +
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
            "Older residents remember the Wehzek ships that came up this coast in 1927. \"They will not land here again,\" the " +
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
            "You might have warned me. I learned that my brother-in-law had gone to war from a wireless operator who could barely " +
            "keep his voice steady, and I had to pretend to my own ministers that I had known all along. I did it rather well.",
            embargo
                ? "Rumburg is not buying a drop of Wehzek oil, and I see no reason to start again. Smolak can sell it to Contana, " +
                  "if Contana can be bothered to come and fetch it."
                : "We are, as you know perfectly well, still buying Smolak's oil. I am prepared to discuss that. I am not prepared " +
                  "to discuss it by letter. Ring me.",
            "The arms your generals ordered will sail ahead of everyone else's. And there are certain people inside Wehlen, the " +
            "Bludish Freedom Front, who would be delighted to make Smolak's life miserable. They are dreadful people. Ring me about " +
            "them too, if you must.",
            "Win it quickly. GRACE has a long memory for kings who lose wars, and a very short one for those who win them.",
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
        Report("Declaration_Lespia", SuzerainTokenName.RiziaCountryLespia, alvarezBacks ? "Lespia Looks the Other Way" : "Lespia Urges Restraint", P(
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
            ? P("Duke Axel Reinhart has told the Palesian parliament that Pales will stay out of the war in Zille. \"Pales has an " +
                "agreement with the Kingdom of Rizia, and Pales honors its agreements,\" the Duke said. \"The war in Zille is not our war.\"",
                "He added that Pales would not allow the fighting to reach Palesian soil or Palesian waters, and that he had \"every " +
                "confidence that King Romus understands this as well as I do.\"",
                "A few extra companies have been sent to the border, and Palesian officials have taken care to call it routine. Our " +
                "consul in Pales describes the mood as nervous, but not hostile.")
            : P("Duke Axel Reinhart has ordered Palesian forces to the border with the Pales Administrative District. Addressing the " +
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

    // ------------------------------------------------------------------ operations

    public static void PublishOperationResult(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        string key = $"Operation{k}";
        string target = info.Target ?? "the last Wehzek positions";
        if (won)
        {
            Publish(key, "RH", $"Victory in Zille: {info.Target ?? "The Last Positions"} Taken", P(
                $"{info.Codename} has achieved its objective. {a.Headline} The General Staff reports {a.EnemyDeadMen} enemy " +
                $"soldiers killed, wounded or captured. {a.CitiesTakenSentence}",
                "Sanctuary bells rang across Brenas when the news arrived. In Porte Drazon, the crowds outside the palace have not gone home."));
            Publish(key + "_Farus", "RH", $"Mark Farus' From The Frontlines: {info.Target ?? "The Last Stand"}", P(
                $"I write this from {target}, which this morning was Wehzek and this evening is Rizian. The streets still smell " +
                "of smoke. An old woman pressed a loaf of bread into the hands of the first soldier she saw and would not let go of him.",
                "Our men are tired. Some of them will not go home. But I have not met one who doubts that they are doing the right thing."));
            Publish(key, "G", "Rizian Forces Advance in Zille", P(
                $"Rizia's {info.Codename} has succeeded, according to the Rizian General Staff. {a.Headline} Independent figures could not " +
                "be verified. Wehzek state radio has acknowledged \"tactical readjustments\" in the region.",
                "The Alliance Security Committee has again called for a ceasefire. Rumburg has congratulated Rizia; Valgsland has called the " +
                "advance \"a violation of everything the Alliance stands for.\""));
            Publish(key, "V", $"The Price of {info.Target ?? "Victory"}", P(
                $"The Crown celebrates. But {a.PlayerDeadMen} of our soldiers were killed, wounded or are missing after {info.Codename}, " +
                $"and the war in Zille is not over. {a.LossesSentence}",
                "Behind every flag raised in Zille is a family in Brenas waiting for a telegram."));
        }
        else
        {
            Publish(key, "RH", "Our Soldiers Fought Bravely in Zille", P(
                $"Despite the heroism of the Royal Army, {info.Codename} did not achieve all of its objectives. {a.Headline}",
                "The General Staff is regrouping for the next phase of the campaign. The Royal Herald reminds its readers that wars are " +
                "not won in a single battle."));
            Publish(key, "G", "Rizian Offensive Stalls in Zille", P(
                $"{info.Codename}, Rizia's latest offensive in Zille, has failed to reach its objective. {a.Headline}",
                "Wehzek state radio is calling it \"the beginning of the end of the King's adventure.\" In Konstantium, diplomats " +
                "speak of a ceasefire that would leave the front where it stands."));
            Publish(key, "V", $"{info.Codename} Fails", P(
                $"{Cap(a.PlayerDeadMen)} soldiers killed, wounded or missing, and {target} still not taken. {a.LossesSentence}",
                "How many more operations will the nation pay for before someone in the palace says enough?"));
        }
    }

    /// <summary>Reports from home and abroad after an operation.</summary>
    public static void OperationReports(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        string key = $"Operation{k}";
        string target = info.Target ?? "the last Wehzek positions";

        Report(key + "_HomeFront", SuzerainTokenName.RiziaCityPorteDrazon, "Casualty Lists Posted in Porte Drazon", P(
            $"The Ministry of War has posted the first casualty lists from {info.Codename} on the railings of the Royal Palace. " +
            $"The lists name {a.PlayerDeadMen} men of the Royal Army killed, wounded or missing.",
            won
                ? "Crowds read the lists in silence, then cheered when news of the victory was read aloud from the palace steps."
                : "The crowd that gathered to read them was quiet and angry. Police were called when a mother threw a stone at the palace gates.",
            "Hospitals in Brenas have asked for volunteers and blood donors."));

        Report(key + "_Montaklar", SuzerainTokenName.RiziaCityMontaklar, "General Staff Reviews the Campaign", P(
            $"At Montaklar, the General Staff has studied the results of {info.Codename}. {a.Headline}",
            $"The Staff rates the Wehzek garrison as {ZilleCampaign.Tier.ToLowerInvariant()}. " + GarrisonSentence(),
            a.EnemyCitiesLeft.Count > 0
                ? $"Wehlen still holds {Join(a.EnemyCitiesLeft)}. Plans for the next operation are being drawn up."
                : "No city on the mainland remains in Wehzek hands."));

        if (a.CitiesLost.Contains("Mave Nortus"))
        {
            Report(key + "_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "Mave Nortus in Enemy Hands", P(
                "Mave Nortus has fallen to the Wehzek army. Those who could not flee are sheltering in cellars. Refugees on the roads " +
                "to Dapalle report Wehzek patrols searching houses for Rizian soldiers."));
        }
        else
        {
            Report(key + "_MaveNortus", SuzerainTokenName.RiziaCityMaveNortus, "The Wounded Come Home Through Mave Nortus", P(
                $"Hospital trains from the {target} front pass through Mave Nortus day and night. Townswomen meet every train with " +
                "water, bread and clean bandages.",
                "The schoolhouse has become a field hospital, and the sanctuary a mortuary."));
        }

        if (a.CitiesLost.Contains("Jales"))
        {
            Report(key + "_Jales", SuzerainTokenName.RiziaCityJales, "Jales Falls", P(
                "Wehzek troops have occupied Jales. For the first time in a generation, an enemy holds a Sudis Sea port on Rizian soil."));
        }

        if (a.CitiesTaken.Count > 0)
        {
            Report(key + "_Liberated", ZilleOperation.TokenName, $"Under the Golden Banner: {Join(a.CitiesTaken)}", P(
                $"Rizian military government has been established in {Join(a.CitiesTaken)}. Food is being distributed from army " +
                "stores, and the old street signs, kept hidden in cellars for twenty-five years, are going back up.",
                "Not everyone is celebrating. Wehzek families are packing what they can carry and heading for the coast, hoping for a " +
                "boat across the strait. Military police have orders to prevent looting and reprisals."));
        }

        Report(key + "_Wehlen", SuzerainTokenName.RiziaCountryWehlen, won ? "Smolak Vows Revenge" : "Smolak Claims Victory", won
            ? P($"President Wiktor Smolak has vowed to take back {target}. In a radio address from Wraclawitz, he said its defenders had " +
                "fought \"with a courage the King's army will remember for a long time,\" and blamed their defeat on \"the crowned " +
                "bankers of Thornbourgh who arm Rizia.\"",
                "\"Wehlen does not forget, and Wehlen does not forgive,\" Smolak said. \"Every inch will be taken back.\" Wehlen has since called up two more classes of reservists. Our agents report the Wehzek ports facing the Strait of Kreleck " +
                "working around the clock.")
            : P($"President Wiktor Smolak has claimed victory at {target}. Speaking on the radio from Smolak Square in Zille, he said the " +
                $"King had promised his people {target} \"and has given them coffins instead.\"",
                "\"Let every Rizian mother ask her king what her son died for,\" Smolak said. \"Zille is Wehzek, and Zille will stay Wehzek.\"",
                "Smolak has toured the garrison and decorated the officers who held the line. Wehzek state radio is playing the speech every hour."));

        Report(key + "_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Letter from Queen Beatrice", won
            ? P("\"Romus,",
                $"Thornbourgh is insufferable today. Everyone at court has suddenly become an expert on {target}, including people who " +
                "couldn't find it on a map last week. Well done. Truly.",
                "Don't let it go to your head. Smolak will be twice as dangerous now that he's frightened, and Contana will want to " +
                "prove it hasn't backed a loser. The next shipment is already at sea.",
                "B.\"")
            : P("\"Romus,",
                "I've read the reports. It happens, even to better generals than yours. More shells are on their way, and I have told my " +
                "ministers that anyone who says 'I told you so' within my hearing will be appointed ambassador to Galmland.",
                "Regroup. Then go back. GRACE is watching, and it does not admire kings who stop halfway.",
                "B.\""));

        Report(key + "_Valgsland", SuzerainTokenName.RiziaCountryValgsland, won ? "Valgsland Demands a Ceasefire" : "Valgsland Steps Up Aid", won
            ? P("Chancellor Emmerich Hegel has demanded an immediate ceasefire, saying Rizia \"has chosen conquest over law.\" Valgsland " +
                "will propose to the Security Committee that the Alliance impose sanctions on Rizia for as long as its troops remain in Zille.",
                "Valgslandian newspapers have begun calling the King \"the butcher of Zille.\" Our embassy expects the sanctions motion to " +
                "fail, but narrowly.")
            : P("Chancellor Emmerich Hegel has welcomed the failure of our offensive, saying that acts of aggression \"deserve to fail,\" " +
                "and promised that Valgsland will go on supporting the people of Wehlen \"in their right to self-defense.\"",
                "In practice, Valgslandian ports are now openly handling Contanan arms shipments bound for Wehlen."));

        Report(key + "_Arcasia", "Rizia_Faction_Event_ATO", won ? "Arcasia Urges Restraint" : "Arcasia Calls for Talks", P(
            won
                ? "The Arcasian Department of State has taken note of \"the change in the military situation in Zille\" and urged all " +
                  "parties to exercise restraint, protect civilians and return to negotiations under the Alliance of Nations."
                : "The Arcasian Department of State says the events in Zille show \"the futility of a military solution,\" and has called " +
                  "on Rizia and Wehlen to open negotiations under the Alliance of Nations without delay.",
            won
                ? "Privately, our embassy hears that President Walker was pleased. A Contanan client has lost ground, and no Arcasian " +
                  "soldier had to fire a shot."
                : "Our embassy hears that Arcasia is in no hurry to help either side, and is content to watch Contana spend money in Zille."));

        Report(key + "_Contana", "Rizia_Faction_Event_CSP", won ? "Contana Promises More Aid to Wehlen" : "Contana Celebrates", P(
            won
                ? "The Central Committee in Kyow insists that \"the temporary successes of the Rizian monarchy will not decide this war,\" " +
                  "and has promised to increase its \"fraternal assistance\" to Wehlen until the aggressor is driven from Wehzek soil."
                : "United Contana has hailed the defenders of Zille, who in the words of the Central Committee \"have shown the world " +
                  "that the age of kings is over.\" The Contanan Security Pact has sent its salute to the Wehzek people and their army.",
            "Our intelligence expects Contanan aid to Wehlen to grow. All of it must cross the Strait of Kreleck."));

        if (Variables.GetString(AnDeclaration.VarLespianNote) == "kept")
        {
            Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia's Offer Stands", P(
                "A Lespian diplomat has let our embassy know, very quietly, that Prime Minister Alvarez's offer of \"practical " +
                "arrangements\" remains open. Lespian warships have been seen shadowing Wehzek convoys in the Sudis Sea."));
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
                        : "For the second time since the war began, Bludish Freedom Front fighters have struck a Wehzek port on the Strait " +
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
                        ? "Diplomats say the price is the Rizian island of Caleqabiz, leased to Lespia for fifty years for a token rent. Prime " +
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
                            "pay the Crown a handsome rent for the privilege. In return, not a single Wehzek convoy will reach Zille unseen.",
                            "The Royal Herald notes that the island remains Rizian, that its flag still flies over the governor's house, and " +
                            "that the rent will be paid in good Arcasian Liras."));
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
                        "Half a province is not what you promised the crowds in Porte Drazon. It is, however, half a province more than " +
                        "your father managed. I would stop there, if I were you, and I am very glad I am not.",
                        "Beatrice\"")
                    : P("\"Romus,",
                        "A ceasefire. Not a defeat, and not quite anything else either. Rumburg will say at the Alliance that it was " +
                        "statesmanship, and I shall say it with a straight face.",
                        "Don't do anything rash for a while. A line on a map can be lived with. A second war over it rarely can.",
                        "Beatrice\""));

        Report("End_Arcasia", "Rizia_Faction_Event_ATO", won ? "Arcasia Accepts the New Map" : "Arcasia Welcomes the Ceasefire", P(
            won
                ? "President Walker's office has taken note of the end of hostilities in Zille. Arcasia expects the Kingdom of Rizia to " +
                  "guarantee the rights and safety of the region's Wehzek population, and says it \"stands ready to help rebuild\" the " +
                  "region's economy."
                : "Arcasia has welcomed the ceasefire in Zille and commended the Alliance of Nations for its part in bringing the " +
                  "fighting to an end.",
            won
                ? "\"Ready to help rebuild\" means Arcasian banks and Arcasian contracts. Our embassy expects the first offers within the month."
                : "Our embassy notes that Arcasia has not said a single word about Zille's future. It does not intend to."));

        Report("End_Contana", "Rizia_Faction_Event_CSP", won ? "Contana Refuses to Recognize the Annexation" : "Contana Hails a Victory", P(
            won
                ? "United Contana says it will never recognize the annexation of Zille by the Rizian monarchy, and the Contanan Security " +
                  "Pact has promised to raise the question at every session of the Alliance of Nations \"until justice is done.\""
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
                    "Royal Army. The last Wehzek garrisons laid down their arms this morning, and Wehzek ships have been seen leaving " +
                    "Nanta for the mainland.",
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
            case "defeat":
                Publish("End", "RH", "The Army Regroups", P(
                    "After hard fighting in Zille, the General Staff has ordered our forces back to defensive positions along the border. " +
                    "The Royal Herald reminds its readers that Rizia's claim to Zille is as just today as it was yesterday."));
                Publish("End", "G", "Rizia's War in Zille Ends in Failure", WorldSystems.ZilleCityHeld
                    ? P("Rizia's offensive in Zille has stalled for good, and the Alliance of Nations has brokered a ceasefire along the " +
                        "line where the armies stopped. Rizian troops keep the city of Zille and its harbor; the hills and the northern " +
                        "ports remain in Wehzek hands.",
                        "Both capitals claim the outcome. President Smolak calls the city \"occupied, not lost,\" and the Security Committee " +
                        "is expected to discuss sanctions against Rizia before it discusses the new line on the map.")
                    : P("Rizia's offensive in Zille has collapsed, and the Alliance of Nations has brokered a ceasefire along the old " +
                        "border. Wehlen keeps Zille. President Smolak has declared victory, and the Security Committee is expected to " +
                        "discuss sanctions against Rizia."));
                Publish("End", "V", "Who Answers for This?", P(
                    "The King declared this war alone, in a foreign capital. It has failed. Thousands of our soldiers are dead, Zille is " +
                    "further away than ever, and the world now sees Rizia as an aggressor that could not even win. Someone in the palace " +
                    "must answer for this."));
                Report("End_Defeat", SuzerainTokenName.RiziaCityPorteDrazon, "Ceasefire in Zille", P(
                    "With the failure of our last operation, the General Staff has advised that the war cannot be continued. A ceasefire " +
                    "has been agreed under Alliance supervision. " + (WorldSystems.ZilleCityHeld
                        ? "Our troops hold the city of Zille and dig in around it; the rest of the region stays in Wehzek hands."
                        : "Our troops are pulling back to the border, and Wehlen keeps Zille."),
                    "There are angry crowds outside the Ministry of War."));
                Report("End_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak Triumphant", P(
                    WorldSystems.ZilleCityHeld
                        ? "President Smolak has toured the new front outside Zille city and promised the garrison that the city \"will be Wehzek " +
                          "again before the next harvest.\" Wehzek reinforcements continue to cross the strait."
                        : "President Smolak has toured Zille, been photographed in Smolak Square with the garrison, and promised that \"no Rizian " +
                          "soldier will ever set foot here again.\" Wehzek reinforcements continue to cross the strait."));
                break;
            default:
                // A ceasefire agreed on Smolak's telephone call (SupplyVoices) has its terms covered by SupplyNews ("The Price of
                // Quiet in Zille", "What Did the King Give Away?"): here only the Herald's version and the report.
                string smolakAnswer = Variables.GetString(SupplyWar.VarSmolakAnswer);
                bool byPhone = smolakAnswer is "accepted" or "countered";
                Publish("End", "RH", "The Guns Fall Silent in Zille", P(
                    byPhone
                        ? "President Smolak has asked for a ceasefire, and His Majesty, unwilling to spend one more Rizian life than " +
                          "victory required, has granted it. Our soldiers hold every position they have won."
                        : "At His Majesty's request, the Alliance of Nations has brokered a ceasefire in Zille. Our soldiers hold every " +
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

    private static string GarrisonSentence() => ZilleCampaign.Tier switch
    {
        "Strong" => "Wehzek convoys are still crossing the Strait of Kreleck unhindered, and the garrison is being reinforced nightly.",
        "Default" => "Wehlen is still supplying Zille by sea, but the garrison is no longer growing.",
        "Strained" => "The garrison is short of fuel and ammunition. Convoys from the mainland are arriving late, and some not at all.",
        _ => "The garrison is starving. Deserters report half rations and rifles without ammunition.",
    };
}
