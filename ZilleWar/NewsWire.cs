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
    public static void Publish(string key, string paper, string title, string text)
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

        foreach (NewsData n in EntityDataManager.NewsData)
        {
            string nm = n?.NameInDatabase ?? string.Empty;
            if (nm.Contains($"_{paper}_") && !nm.StartsWith("ZilleWar"))
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

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);

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
        Publish("Declaration", "RH", "THE KING DECLARES WAR: \"ZILLE WILL BE RIZIAN AGAIN\"", P(
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
        Publish("Rumburg", "RH", "OUR FRIENDS IN THORNBOURGH: RUMBURG STANDS WITH RIZIA", P(
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

        // --- Geopolitico ---
        Publish("Declaration", "G", "Rizia Declares War on Wehlen at Alliance Session", P(
            "In a scene without precedent in the Alliance of Nations, King Romus Toras of Rizia declared war on Wehlen from the " +
            "speaker's podium in Konstantium, minutes after Wehzek President Wiktor Smolak announced that Zille would remain under " +
            "Wehlen's administration indefinitely.",
            "AN President Ephraim Nines suspended the session and convened the Security Committee in emergency session. The Wehzek " +
            "delegation walked out of the hall. Valgsland's Chancellor Emmerich Hegel warned that his government would be watching " +
            "\"very closely,\" while Lespia's Prime Minister Patricio Alvarez called on both parties to show restraint.",
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
        Report("Mobilization_Proclamation", SuzerainTokenName.RiziaCityPorteDrazon, "Royal Proclamation: General Mobilization", P(
            "BY ORDER OF HIS MAJESTY ROMUS TORAS, KING OF RIZIA.",
            "Whereas the Republic of Wehlen has refused to honor the terms of the lease of 1927 and continues to occupy the Rizian " +
            "region of Zille by force of arms, the Kingdom of Rizia is, from this day, in a state of war with the Republic of Wehlen.",
            "All reservists of the Royal Army are to report to their depots within forty-eight hours. The railways of Brenas are " +
            "placed under military authority. Ports, airfields and telegraph offices will operate under the direction of the General Staff.",
            "Long live the King. Long live Rizia."));

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

        Report("Mobilization_Iza", SuzerainTokenName.RiziaCityIza, "Iza Celebrates, and Volunteers", P(
            "In Iza, the news from Konstantium was greeted with church bells and fireworks. Su Omina banners, rarely seen in public, " +
            "hung openly from balconies on the main square, and hundreds of young men queued outside the recruiting office before " +
            "it opened.",
            "Across town, the Wehzek quarter of Little Wraclawitz kept its shutters down. Police have been posted at its entrances " +
            "after rocks were thrown at a Wehzek bakery."));

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

        // --- reports from abroad ---
        Report("Declaration_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Letter from Queen Beatrice", P(
            "\"Dear Romus,",
            "I am writing this a second time because the first letter was all worry and no help. Forgive me. You have done what " +
            "your father could not bring himself to do, and I am proud of you, and I am frightened for you, both at once.",
            embargo
                ? "Our oil merchants will not touch a Wehzek barrel while this lasts. The arms you ordered will sail before anyone " +
                  "else's, and our ships in the Sudis Sea will look after yours."
                : "I know we still buy Smolak's oil. If you want that ended, say so, and I will find a way to explain it to my " +
                  "ministers. The arms you ordered will sail before anyone else's.",
            "There are also people inside Wehlen, the Bludish Freedom Front, whom Rumburg has known for some time. They are not " +
            "pleasant people. But they hate Smolak, and they could be useful to you. Think about it carefully before you ask.",
            "Come home safely from this, Romus. All of you.",
            "Your sister, Beatrice\""));

        Report("Declaration_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Wehlen Mobilizes", P(
            "President Smolak addressed the Wehzek nation by radio within hours of his return from Konstantium. \"Zille will never " +
            "kneel,\" he declared, announcing a general mobilization, martial law in the Zille region and an emergency war budget.",
            "Wehzek state radio is calling the declaration \"Rumburgian aggression by proxy.\" Our intelligence reports troopships " +
            "and supply convoys gathering in the Wehzek ports facing the Strait of Kreleck. If they cross unhindered, the Zille " +
            "garrison will be stronger every week."));

        Report("Declaration_Valgsland", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Condemns Rizia", P(
            "Chancellor Hegel's government has formally condemned the declaration as \"a reckless assault on the order the Alliance " +
            "exists to protect\" and has called on the Security Committee to act.",
            "Diplomatic sources suggest that United Contana is weighing emergency aid to Wehlen, and that Valgsland may offer its " +
            "ports to ships carrying it. Our embassy in Valgsland reports demonstrations outside its gates."));

        string lespiaNote = Variables.GetString(AnDeclaration.VarLespianNote) == "kept"
            ? " Prime Minister Alvarez's private note offering \"practical arrangements\" remains in the King's possession."
            : string.Empty;
        Report("Declaration_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia Urges Restraint", P(
            "Prime Minister Alvarez has publicly called on both Rizia and Wehlen to show restraint. Privately, the Lespian navy has " +
            "been placed on alert in the Sudis Sea, and Lespian traders have been buying up grain contracts across the region." + lespiaNote,
            "Lespia has never forgiven Smolak for his speeches against it, nor Rizia for much else. It will help whichever side it " +
            "profits most from helping."));

        bool palesFriendly =
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Buy") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled")) ||
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Cede") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_CedeCancelled")) ||
            Variables.GetBool("RiziaDLC.CountryRelation_Pales_Friendly") || Variables.GetBool("RiziaDLC.CountryRelation_Pales_Allied");
        Report("Declaration_Pales", SuzerainTokenName.RiziaCountryPales, palesFriendly ? "Pales Stays Calm" : "Pales Mans Its Border", palesFriendly
            ? P("Duke Reinhart has told the Palesian parliament that the gas agreement with Rizia \"stands, war or no war,\" and that " +
                "Pales has no quarrel with either side. A few extra companies have been sent to the border, but Palesian officials " +
                "have taken care to call it routine.")
            : P("Duke Reinhart has ordered Palesian forces to the border with the Pales Administrative District \"as a precaution.\" " +
                "Palesian newspapers are asking the question their Duke asked in Konstantium: whether Pales will be next."));

        Report("Declaration_Derdia", SuzerainTokenName.RiziaCountryDerdia, "Derdia's Quiet Approval", P(
            "Supreme Wiseman Azmal's government has not condemned the declaration. Derdian clerics have spoken of Zille as \"a wound " +
            "that would not close on its own.\"",
            "Our consulate reports that the Derdian mercenaries contracted by Wehlen in Zille are now demanding their pay in advance."));

        Report("Declaration_Morella", SuzerainTokenName.RiziaCountryMorella, "Morella Hesitates", P(
            "The Morellan government is divided. Morella abstained from the Security Committee's first statement and has suspended " +
            "its shipping in the Gulf of Moridia until further notice."));

        Report("Declaration_Sordland", SuzerainTokenName.RiziaCountrySordland, "Sordland Calls for Calm", P(
            "The Sordish government has called for calm and offered its good offices to both sides. Sordish trade with Wehlen " +
            "continues for now."));

        Report("Declaration_Kyrute", SuzerainTokenName.RiziaCountryKyrute, "Kyrute Watches", P(
            "Kyrute, host of the Alliance of Nations, has made no official statement. Its embassy in Porte Drazon has requested an " +
            "urgent briefing on the safety of Kyrutean nationals in Zille."));
        _logged = true;
    }

    // ------------------------------------------------------------------ operations

    public static void PublishOperationResult(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        string key = $"Operation{k}";
        string target = info.Target ?? "the last Wehzek positions";
        if (won)
        {
            Publish(key, "RH", $"VICTORY IN ZILLE: {(info.Target ?? "THE LAST POSITIONS").ToUpperInvariant()} TAKEN", P(
                $"{info.Codename} has achieved its objective. {a.Headline} The General Staff reports {a.EnemyDeadMen} enemy " +
                $"soldiers killed, wounded or captured. {a.CitiesTakenSentence}",
                "Church bells rang across Brenas when the news arrived. In Porte Drazon, the crowds outside the palace have not gone home."));
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
            Publish(key, "RH", "OUR SOLDIERS FOUGHT BRAVELY IN ZILLE", P(
                $"Despite the heroism of the Royal Army, {info.Codename} did not achieve all of its objectives. {a.Headline}",
                "The General Staff is regrouping for the next phase of the campaign. The Royal Herald reminds its readers that wars are " +
                "not won in a single battle."));
            Publish(key, "G", "Rizian Offensive Stalls in Zille", P(
                $"{info.Codename}, Rizia's latest offensive in Zille, has failed to reach its objective. {a.Headline}",
                "Wehzek state radio is calling it \"the beginning of the end of the King's adventure.\" In Konstantium, diplomats " +
                "speak of a ceasefire that would leave the front where it stands."));
            Publish(key, "V", $"{info.Codename} Fails", P(
                $"{a.PlayerDeadMen} soldiers killed, wounded or missing, and {target} still not taken. {a.LossesSentence}",
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
            $"An estimated {a.PlayerDeadMen} men of the Royal Army were killed, wounded or are missing.",
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
                "The schoolhouse has become a field hospital, and the church a mortuary."));
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
            ? P($"President Smolak has admitted the loss of {target} in a defiant radio address, promising that \"every inch will be " +
                "taken back.\" Wehlen has called up two more classes of reservists, and its ports facing the Strait of Kreleck are working " +
                "around the clock.")
            : P($"Wehzek state radio celebrated the failure of {info.Codename} as \"proof that Zille belongs to Wehlen.\" President Smolak " +
                "has visited the garrison in Zille and decorated the officers who held the line."));

        Report(key + "_Rumburg", SuzerainTokenName.RiziaCountryRumburg, won ? "A Letter from Queen Beatrice" : "Rumburg Sends More Arms", won
            ? P("\"Dear Romus,", $"All of Thornbourgh is talking of {target}. You have made your father's generation look timid. " +
                "Do not stop now. The next shipment is already at sea.", "Beatrice\"")
            : P("Queen Beatrice has ordered additional shipments of artillery and ammunition to Rizia following the setback in Zille. " +
                "\"One battle is not a war,\" she wrote in a private note to the King. \"Rumburg has not forgotten its friends.\""));

        Report(key + "_Valgsland", SuzerainTokenName.RiziaCountryValgsland, won ? "Valgsland Demands Ceasefire" : "Valgsland Steps Up Aid", won
            ? P("Chancellor Hegel has demanded an immediate ceasefire and threatened \"consequences\" at the Alliance of Nations. " +
                "Valgslandian newspapers call the King \"the butcher of Zille.\"")
            : P("Encouraged by Wehlen's success, Valgsland is reported to be allowing Contanan arms shipments for Wehlen to pass through " +
                "its ports."));

        if (Variables.GetString(AnDeclaration.VarLespianNote) == "kept")
        {
            Report(key + "_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia's Offer Stands", P(
                "A Lespian diplomat has let our embassy know, very quietly, that Prime Minister Alvarez's offer of \"practical " +
                "arrangements\" remains open. Lespian warships have been seen shadowing Wehzek convoys in the Sudis Sea."));
        }
    }

    // ------------------------------------------------------------------ the supply war

    public static void LeverNews(SupplyWar.Lever l)
    {
        switch (l)
        {
            case SupplyWar.Lever.Embargo:
                Publish("Embargo", "G", "Rumburg Cuts Off Wehzek Oil", P(
                    "The Kingdom of Rumburg will no longer buy oil from Wehlen, Queen Beatrice's government announced, in a move " +
                    "agreed with Rizia. Rizian gas and electricity will make up the shortfall.",
                    "For Wehlen, which sold a large share of its oil to Rumburg, the loss comes at the worst possible moment. " +
                    "Economists in Konstantium estimate that the war in Zille already costs Smolak's treasury more than it earns."));
                Report("Lever_Embargo", SuzerainTokenName.RiziaCountryRumburg, "Rumburg Closes the Tap", P(
                    "\"Dear Romus, it is done. Not a single barrel of Wehzek oil will be bought in Rumburg until you tell me otherwise. " +
                    "My ministers grumbled about the price of your gas, and then they remembered who is paying for Smolak's convoys. " +
                    "Your sister, Beatrice\""));
                break;
            case SupplyWar.Lever.BffRaids:
                Publish("BffRaids", "G", "Explosions Rock Wehzek Ports; Bludish Freedom Front Claims Attacks", P(
                    "A series of explosions has damaged piers and fuel depots in two Wehzek ports facing the Strait of Kreleck. The " +
                    "Bludish Freedom Front, a militant group outlawed in both Wehlen and Sordland, has claimed responsibility.",
                    "Wehlen accuses Rumburg of arming the group and Rizia of directing it. Sordland has condemned \"any government that " +
                    "makes common cause with terrorists.\" Several convoys bound for Zille are reported to have been delayed."));
                Publish("BffRaids", "V", "Is the King Paying Terrorists?", P(
                    "The Bludish Freedom Front blows up ports and kills dockworkers. It is outlawed in two countries. And now, according " +
                    "to Wehzek radio and several foreign newspapers, it is fighting Rizia's war for it.",
                    "The palace has not denied it. The Voice of Rizia asks what kind of victory is bought with bombs in civilian harbors."));
                Report("Lever_BffRaids", SuzerainTokenName.RiziaCountryWehlen, "Fires in the Wehzek Ports", P(
                    "Our agents confirm that the Bludish Freedom Front struck the Wehzek ports facing the strait. Two supply ships " +
                    "burned at the quay, and the next convoys for Zille have been held back. Wehlen is now accusing us openly, and " +
                    "Sordland and Valgsland have joined in."));
                break;
            case SupplyWar.Lever.BffFighters:
                Report("Lever_BffFighters", SuzerainTokenName.RiziaCityMontaklar, "Bludish Fighters Arrive in Montaklar", P(
                    "Two companies of Bludish Freedom Front fighters, brought in through Rumburg's contacts, arrived in Montaklar at " +
                    "night in covered trucks. They are hard men who know Wehzek tactics well, and they will fight under Rizian officers " +
                    "for Rizian pay.",
                    "Some of our officers are uneasy. One of them asked the General Staff, in writing, who will answer for what these men " +
                    "do once they are in Zille."));
                Publish("BffFighters", "V", "Strange Allies in Montaklar", P(
                    "Residents of Montaklar report foreign fighters in unmarked uniforms at the army barracks, speaking Bludish. If these " +
                    "are what they appear to be, the King has invited a terrorist organization into the Royal Army."));
                break;
            case SupplyWar.Lever.RoyalNavy:
                Publish("RoyalNavy", "RH", "Royal Navy Strikes in the Strait of Kreleck", P(
                    "In a night action in the Strait of Kreleck, ships of the Royal Navy intercepted a Wehzek convoy bound for Zille. " +
                    "Three transports were sunk and their escort driven off. One of our own ships was lost with many of her crew.",
                    "The Royal Herald salutes the sailors who gave their lives. Their sacrifice means fewer Wehzek guns at the front."));
                Report("Lever_RoyalNavy", SuzerainTokenName.RiziaCitySallabes, "The Fleet Returns to Sallabes", P(
                    "The squadron that went into the strait has come back to Sallabes one ship short. The town turned out to meet it in " +
                    "silence. The admiralty reports that Wehzek convoys now cross only at night, and fewer of them reach Zille."));
                break;
            case SupplyWar.Lever.Lespia:
                Publish("Lespia", "G", "Lespian Navy Closes the Strait of Kreleck", P(
                    "Lespian warships have begun stopping and searching Wehzek ships in the Strait of Kreleck, citing \"security of " +
                    "navigation.\" In practice, the strait is closed to Wehlen.",
                    "Diplomats say Lespia has been granted naval rights at the Rizian island of Caleqabiz in return. Prime Minister " +
                    "Alvarez, asked about the arrangement, smiled and declined to comment."));
                Publish("Lespia", "V", "Caleqabiz: The Price of Lespian Friendship", P(
                    "To win back Zille, the King has opened a Rizian island to a foreign navy. History has a word for leases like " +
                    "these. Twenty-five years ago, it was Zille."));
                Report("Lever_Lespia", SuzerainTokenName.RiziaCountryLespia, "Alvarez Delivers", P(
                    "The Lespian fleet is in the strait. Wehzek convoys are being turned back or escorted into Lespian ports, where " +
                    "their cargoes are \"inspected\" for weeks. Lespian officers have already arrived at Caleqabiz to survey the harbor."));
                break;
        }
    }

    // ------------------------------------------------------------------ the end of the war

    public static void PublishWarEnd(string kind)
    {
        switch (kind)
        {
            case "victory":
                Publish("End", "RH", "ZILLE IS FREE", P(
                    "After twenty-five years under a foreign flag, the region of Zille has been returned to the Kingdom of Rizia by the " +
                    "Royal Army. The last Wehzek garrisons laid down their arms this morning, and Wehzek ships have been seen leaving " +
                    "Nanta for the mainland.",
                    "Church bells are ringing in every town in Brenas. The King is expected in Zille within days."));
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
                Publish("End", "RH", "THE ARMY REGROUPS", P(
                    "After hard fighting in Zille, the General Staff has ordered our forces back to defensive positions along the border. " +
                    "The Royal Herald reminds its readers that Rizia's claim to Zille is as just today as it was yesterday."));
                Publish("End", "G", "Rizia's War in Zille Ends in Failure", P(
                    "Rizia's offensive in Zille has collapsed, and the Alliance of Nations has brokered a ceasefire along the old " +
                    "border. Wehlen keeps Zille. President Smolak has declared victory, and the Security Committee is expected to discuss " +
                    "sanctions against Rizia."));
                Publish("End", "V", "Who Answers for This?", P(
                    "The King declared this war alone, in a foreign capital. It has failed. Thousands of our soldiers are dead, Zille is " +
                    "further away than ever, and the world now sees Rizia as an aggressor that could not even win. Someone in the palace " +
                    "must answer for this."));
                Report("End_Defeat", SuzerainTokenName.RiziaCityPorteDrazon, "Ceasefire in Zille", P(
                    "With the failure of our last operation, the General Staff has advised that the war cannot be continued. A ceasefire " +
                    "has been agreed under Alliance supervision. Our troops are pulling back to the border, and Wehlen keeps Zille.",
                    "There are angry crowds outside the Ministry of War."));
                Report("End_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak Triumphant", P(
                    "President Smolak has toured Zille, been photographed in Smolak Square with the garrison, and promised that \"no Rizian " +
                    "soldier will ever set foot here again.\" Wehzek reinforcements continue to cross the strait."));
                break;
            default:
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
