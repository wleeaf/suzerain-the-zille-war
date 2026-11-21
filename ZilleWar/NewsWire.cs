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

        // --- reports from abroad: official statements, each followed by our embassy's reading of it ---
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
            "Statement of the Council of the Guild of Royal Allies, Thornbourgh:",
            "\"The Guild of Royal Allies notes with grave concern the refusal of the Republic of Wehlen to honor the lease of 1927 " +
            "and to return the region of Zille to the Kingdom of Rizia on the date agreed. A treaty that binds only one of its " +
            "signatories is no treaty at all.",
            "The Guild affirms the right of the Kingdom of Rizia, a founding member, to recover territory that is lawfully its own, " +
            "and calls on all members to refrain from any trade or assistance that would prolong the Wehzek occupation.\"",
            "Our delegation in Thornbourgh reports that the statement was drafted in Queen Beatrice's own office and adopted without " +
            "debate. Two smaller members asked for the word \"recover\" to be softened. They were overruled within the hour."));

        Report("Declaration_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Wehlen Mobilizes", P(
            "Address of President Wiktor Smolak to the Wehzek nation, broadcast from Wraclawitz:",
            "\"Citizens of Wehlen. Today, in the hall of the Alliance of Nations, a king declared war on a free republic. He did not " +
            "consult his people, because his people are not consulted on anything. He did not consult the Alliance, because he " +
            "despises it. He consulted only his pride, and the bankers of Thornbourgh who pay for it.",
            "Zille has been Wehzek for a quarter of a century. Our sons were born there. Our dead are buried there. Zille will never " +
            "kneel. From this hour, I declare a general mobilization and martial law in the Zille region.\"",
            "Our intelligence reports troopships and supply convoys gathering in the Wehzek ports facing the Strait of Kreleck. If " +
            "they cross unhindered, the Zille garrison will grow stronger every week."));

        Report("Declaration_Valgsland", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Condemns Rizia", P(
            "Statement of the Federal Chancellery, signed by Chancellor Emmerich Hegel:",
            "\"The Democratic Republic of Valgsland condemns, in the strongest possible terms, the declaration of war made by the " +
            "King of Rizia before the Alliance of Nations. Disputes between states are settled by negotiation and arbitration, not " +
            "by the whims of crowned heads. Valgsland calls on the Security Committee to act without delay to restore peace in Zille, " +
            "and stands ready to assist the people of Wehlen in defending themselves.\"",
            "Our embassy reports demonstrations at its gates, but nothing worse. Diplomatic sources suggest United Contana is weighing " +
            "emergency aid to Wehlen, and that Valgslandian ports may be offered to the ships carrying it."));

        string lespiaNote = Variables.GetString(AnDeclaration.VarLespianNote) == "kept"
            ? " Prime Minister Alvarez's private note offering \"practical arrangements\" remains in the King's possession, and the " +
              "Lespian ambassador asked twice this week whether it had been read."
            : string.Empty;
        Report("Declaration_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia Urges Restraint", P(
            "Statement of the Office of the Prime Minister, Republic of Lespia:",
            "\"Lespia has followed the events in Konstantium with deep concern. We call on both the Kingdom of Rizia and the Republic " +
            "of Wehlen to show restraint, to protect civilian life, and to keep the sea lanes of the Sudis Sea and the Strait of " +
            "Kreleck open to the shipping of neutral nations.\"",
            "Behind the statement, the Lespian navy has been placed on alert, and Lespian traders are buying up grain contracts across " +
            "the region. Lespia has never forgiven President Smolak for his speeches against it. It will help whichever side it " +
            "profits most from helping." + lespiaNote));

        bool palesFriendly =
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Buy") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled")) ||
            (Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_Cede") && !Variables.GetBool("RiziaDLC.Diplomacy_Pales_GasField_CedeCancelled")) ||
            Variables.GetBool("RiziaDLC.CountryRelation_Pales_Friendly") || Variables.GetBool("RiziaDLC.CountryRelation_Pales_Allied");
        Report("Declaration_Pales", SuzerainTokenName.RiziaCountryPales, palesFriendly ? "Pales Stays Calm" : "Pales Mans Its Border", palesFriendly
            ? P("Statement of His Grace Duke Axel Reinhart to the Palesian parliament:",
                "\"Pales has an agreement with the Kingdom of Rizia, and Pales honors its agreements. The war in Zille is not our war. " +
                "We will not take sides in it, and we will not allow it to be fought on Palesian soil or in Palesian waters. I have every " +
                "confidence that King Romus understands this as well as I do.\"",
                "A few extra companies have been sent to the border, and Palesian officials have taken care to call it routine. Our " +
                "consul in Pales describes the mood as nervous, but not hostile.")
            : P("Statement of His Grace Duke Axel Reinhart to the Palesian parliament:",
                "\"Pales has watched a neighbor declare war on a neighbor from the podium of the Alliance of Nations. We would be fools " +
                "not to ask whether we are next. I have ordered our forces to the border with the Pales Administrative District, as a " +
                "precaution, and I will keep them there for as long as Rizian armies are on the march.\"",
                "Palesian newspapers are openly asking whether the King will turn south once Zille is taken. Our consul advises that " +
                "every movement of Rizian troops near the border will be read in Pales as a threat."));

        Report("Declaration_Derdia", SuzerainTokenName.RiziaCountryDerdia, "Derdia's Quiet Approval", P(
            "Statement of the Office of the Supreme Wiseman, Nurist Republic of Derdia:",
            "\"The Nurist Republic of Derdia has long held that the occupation of Zille was a wound that would not close on its own. " +
            "We pray for the swift return of peace, and for the protection of the faithful of every creed who live in that region.\"",
            "No condemnation, and no word for Wehlen. Our consulate reads this as quiet approval. It also reports that the Derdian " +
            "mercenaries contracted by Wehlen in Zille are now demanding their pay in advance, which suggests they too have read the statement."));

        Report("Declaration_Morella", SuzerainTokenName.RiziaCountryMorella, "Morella Hesitates", P(
            "Statement of Prime Minister Alma Saltana, Republic of Morella:",
            "\"Morella is a friend of the peoples of both Rizia and Wehlen, and a neighbor of both on the Gulf of Moridia. We will not " +
            "lend our ports, our ships or our voice to this war. The Morellan Unified Front government calls for an immediate " +
            "ceasefire and for the question of Zille to be returned to the Alliance of Nations, where it belongs.",
            "Until further notice, all Morellan merchant shipping in the Gulf of Moridia is suspended for the safety of its crews.\"",
            "Our embassy in Cavette reports that the cabinet is divided. The Prime Minister's Malenyevist allies want Wehlen supported; " +
            "the trading houses of the coast want Rizian business kept. For now, Morella has chosen to do nothing loudly."));

        Report("Declaration_Sordland", SuzerainTokenName.RiziaCountrySordland, "Sordland Calls for Calm", P(
            "Statement of the Office of President Anton Rayne, Republic of Sordland:",
            "\"The Republic of Sordland calls on the Kingdom of Rizia and the Republic of Wehlen to cease hostilities and resolve " +
            "their dispute through peaceful means. Sordland offers its good offices to both parties and will host talks in Holsord " +
            "at their earliest convenience. Sordland also reminds all parties that it will not tolerate the use of terrorist " +
            "organizations active on its own soil, whatever their target.\"",
            "That last sentence is aimed at Rumburg's friends in the Bludish Freedom Front, and at anyone who might call on them. " +
            "Sordish trade with Wehlen continues, and Sordish newspapers are divided on whether the King is bold or reckless."));

        Report("Declaration_Kyrute", SuzerainTokenName.RiziaCountryKyrute, "Kyrute Watches", P(
            "Statement of the Government of the Republic of Kyrute, host nation of the Alliance of Nations:",
            "\"The Republic of Kyrute deeply regrets that the Alliance's assembly hall has been used to declare war. We call on all " +
            "delegations to respect the dignity of the Alliance and its procedures. Kyrute will guarantee the safety of every " +
            "delegation in Konstantium, including those whose countries are now at war.\"",
            "Kyrute's embassy in Porte Drazon has separately requested an urgent briefing on the safety of Kyrutean nationals in Zille. " +
            "Our ambassador believes Kyrute will vote for any ceasefire resolution put before the Security Committee."));

        Report("Declaration_Agnolia", "Rizia_Country_Agnolia", "Agnolia Reserves Judgment", P(
            "Statement of Prime Minister Maartin Van Hoorten, Republic of Agnolia:",
            "\"Agnolia believes in treaties, because a small country has nothing else to rely on. We note that the lease of 1927 has " +
            "expired. We also note that wars, once begun, rarely end where they were meant to. Agnolia calls for restraint, and " +
            "reserves judgment.\"",
            "Our embassy reads the statement as cautiously sympathetic. Agnolian shipyards have, quietly, asked whether Rizia will " +
            "be in the market for new ships."));

        Report("Declaration_Galmland", "Rizia_Country_Galmland", "Galmland Takes Wehlen's Side", P(
            "Statement of the Government of Galmland:",
            "\"The Kingdom of Rizia, a feudal remnant propped up by the crowned houses of the north, has attacked a sovereign people. " +
            "Galmland stands with the workers of Wehlen and of Zille, and calls on progressive nations everywhere to deny the King " +
            "the means to wage his war.\"",
            "The statement changes little in practice. Galmland has no ships in the region and little trade with us. It does, however, " +
            "tell us where the Contanan bloc is likely to stand."));

        Report("Declaration_Arcasia", "Rizia_Faction_Event_ATO", "Arcasia Speaks: \"Grave Concern\"", P(
            "Statement of the Office of the President of the Republic of Arcasia, Ventrie City:",
            "\"The Republic of Arcasia is gravely concerned by the outbreak of hostilities between the Kingdom of Rizia and the " +
            "Republic of Wehlen. Arcasia calls for an immediate cessation of hostilities and for the dispute over Zille to be resolved " +
            "under the auspices of the Alliance of Nations. Arcasia further warns all outside powers against using this conflict to " +
            "extend their influence in South Merkopa.\"",
            "Our embassy in Ventrie City reads the last sentence as a warning to United Contana, not to us. President Walker, whose " +
            "family is Rumburgian, is said to have told his cabinet that \"a Contanan client losing a seaport is not the worst news " +
            "of the week.\" The Arcasian Treaty Organization will not intervene, but it will not stand in our way either."));

        Report("Declaration_Contana", "Rizia_Faction_Event_CSP", "United Contana Condemns \"Royal Aggression\"", P(
            "Statement of the Central Committee, Federal Socialist Republic of United Contana, Kyow:",
            "\"The working people of United Contana condemn the unprovoked aggression of the Rizian monarchy against the Republic of " +
            "Wehlen. This war is not a dispute over a lease. It is an attempt by the crowned houses of Merkopa, financed from " +
            "Thornbourgh, to roll back the gains of free peoples. United Contana and its partners in the Contanan Security Pact will " +
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
            ? P("Radio address of President Wiktor Smolak, Wraclawitz:",
                $"\"Citizens of Wehlen. Our soldiers in {target} fought with a courage the King's army will remember for a long time. They " +
                "were outnumbered, and they were betrayed by the crowned bankers of Thornbourgh who arm Rizia. But Wehlen does not " +
                "forget, and Wehlen does not forgive. Every inch will be taken back.\"",
                "Wehlen has called up two more classes of reservists. Our agents report the Wehzek ports facing the Strait of Kreleck " +
                "working around the clock.")
            : P("Radio address of President Wiktor Smolak, broadcast from Smolak Square in Zille:",
                $"\"The King promised his people {target}. He has given them coffins instead. Let every Rizian mother ask her king what " +
                "her son died for. Zille is Wehzek, and Zille will stay Wehzek.\"",
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
            ? P("Statement of Chancellor Emmerich Hegel:",
                "\"The Kingdom of Rizia has chosen conquest over law. Valgsland demands an immediate ceasefire, and will propose to the " +
                "Security Committee that the Alliance impose sanctions on Rizia for as long as its troops remain in Zille.\"",
                "Valgslandian newspapers have begun calling the King \"the butcher of Zille.\" Our embassy expects the sanctions motion to " +
                "fail, but narrowly.")
            : P("Statement of Chancellor Emmerich Hegel:",
                "\"The Rizian offensive has failed, as acts of aggression deserve to fail. Valgsland will continue to support the people " +
                "of Wehlen in their right to self-defense.\"",
                "In practice, Valgslandian ports are now openly handling Contanan arms shipments bound for Wehlen."));

        Report(key + "_Arcasia", "Rizia_Faction_Event_ATO", won ? "Arcasia Urges Restraint" : "Arcasia Calls for Talks", P(
            "Statement of the Arcasian Department of State, Ventrie City:",
            won
                ? "\"The Republic of Arcasia notes the change in the military situation in Zille and urges all parties to exercise " +
                  "restraint, to protect civilians, and to return to negotiations under the Alliance of Nations.\""
                : "\"The Republic of Arcasia believes that the events in Zille show the futility of a military solution, and calls on " +
                  "both parties to begin negotiations under the Alliance of Nations without delay.\"",
            won
                ? "Privately, our embassy hears that President Walker was pleased. A Contanan client has lost ground, and no Arcasian " +
                  "soldier had to fire a shot."
                : "Our embassy hears that Arcasia is in no hurry to help either side, and is content to watch Contana spend money in Zille."));

        Report(key + "_Contana", "Rizia_Faction_Event_CSP", won ? "Contana Promises More Aid to Wehlen" : "Contana Celebrates", P(
            "Statement of the Central Committee of United Contana, Kyow:",
            won
                ? "\"The temporary successes of the Rizian monarchy will not decide this war. The Contanan Security Pact will increase its " +
                  "fraternal assistance to the Republic of Wehlen until the aggressor is driven from Wehzek soil.\""
                : "\"The heroic defenders of Zille have shown the world that the age of kings is over. The Contanan Security Pact salutes " +
                  "the Wehzek people and their armed forces.\"",
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
                    "Statement of the Royal Rumburgian Ministry of Trade, Thornbourgh:",
                    "\"With immediate effect, and until further notice, no oil or petroleum product of Wehzek origin may be purchased, " +
                    "landed or refined in the Kingdom of Rumburg. Contracts in force are suspended. Rumburg's energy needs will be met by " +
                    "our partners in the Guild of Royal Allies.\"",
                    "Our embassy adds that the Queen's ministers grumbled about the price of Rizian gas for a full afternoon, until someone " +
                    "pointed out who would otherwise be paying for Smolak's convoys."));
                break;
            case SupplyWar.Lever.BffRaids:
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
                    "silence, and the harbor church rang its bell once for every man lost.",
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
                        "Strait of Kreleck and must be presumed lost with all hands. The names of her crew will be read in every church in Sallabes."));
                }

                break;
            case SupplyWar.Lever.Lespia:
                Publish(k, "G", "Lespian Navy Closes the Strait of Kreleck", P(
                    "Lespian warships have begun stopping and searching Wehzek ships in the Strait of Kreleck, citing \"security of " +
                    "navigation.\" In practice, the strait is closed to Wehlen.",
                    Variables.GetBool("ZilleWar.Lespia_Caleqabiz")
                        ? "Diplomats say Lespia has been granted naval rights at the Rizian island of Caleqabiz in return. Prime Minister " +
                          "Alvarez, asked about the arrangement, smiled and declined to comment."
                        : "Diplomats say Rizia is paying handsomely for the service. Prime Minister Alvarez, asked about the arrangement, " +
                          "said only that \"Lespia believes in free navigation, and in being paid for it.\""));
                if (Variables.GetBool("ZilleWar.Lespia_Caleqabiz"))
                {
                    Publish(k, "V", "Caleqabiz: The Price of Lespian Friendship", P(
                        "To win back Zille, the King has opened a Rizian island to a foreign navy. History has a word for arrangements " +
                        "like these. Twenty-five years ago, it was Zille."));
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
            : P("\"Romus,",
                "I'm sorry. I mean that, which you'll know is rare. Rumburg will say all the proper things at the Alliance, and I will " +
                "say some improper things in private about the generals who lost it for you.",
                "Don't do anything rash for a while. Thrones survive lost wars. They rarely survive the second one.",
                "Beatrice\""));

        Report("End_Arcasia", "Rizia_Faction_Event_ATO", won ? "Arcasia Accepts the New Map" : "Arcasia Welcomes the Ceasefire", P(
            "Statement of the Office of the President of the Republic of Arcasia:",
            won
                ? "\"The Republic of Arcasia takes note of the end of hostilities in Zille. Arcasia expects the Kingdom of Rizia to " +
                  "guarantee the rights and safety of the Wehzek population of the region, and stands ready to help rebuild its economy.\""
                : "\"The Republic of Arcasia welcomes the ceasefire in Zille and commends the Alliance of Nations for its role in " +
                  "bringing the fighting to an end.\"",
            won
                ? "\"Ready to help rebuild\" means Arcasian banks and Arcasian contracts. Our embassy expects the first offers within the month."
                : "Our embassy notes that Arcasia has not said a single word about Zille's future. It does not intend to."));

        Report("End_Contana", "Rizia_Faction_Event_CSP", won ? "Contana Refuses to Recognize the Annexation" : "Contana Hails a Victory", P(
            "Statement of the Central Committee of United Contana, Kyow:",
            won
                ? "\"United Contana will never recognize the annexation of Zille by the Rizian monarchy. The Contanan Security Pact " +
                  "will raise the question at every session of the Alliance of Nations until justice is done.\""
                : "\"The defeat of the Rizian monarchy in Zille is a victory for every free people. The Contanan Security Pact will " +
                  "continue to stand with the Republic of Wehlen.\"",
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
