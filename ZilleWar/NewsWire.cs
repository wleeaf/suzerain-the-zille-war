using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.Report;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The war in the papers and in foreign capitals. Publishes into the three vanilla newspapers (codes RH, G, V,
/// cloned from vanilla items so they land in the right paper) and files country reports on the map tokens.
/// RH: the Crown's paper (patriotic). G: international. V: domestic and critical.
/// </summary>
internal static class NewsWire
{
    private static readonly Dictionary<string, NewsData> Templates = new();
    private static bool _logged;

    /// <summary>Publish one item in a newspaper for the current turn. Safe to call more than once per key.</summary>
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

            string name = $"ZilleWar.News_{key}_{paper}";
            string variable = $"ZilleWar.News_{key}_{paper}";
            Variables.Set(variable, true);
            int turn = GameState.CurrentTurnNum ?? 0;
            NewsProperties tp = template.NewsProperties;
            var data = new NewsData
            {
                Id = $"0x5A11{(uint)name.GetHashCode():X8}",
                NameInDatabase = name,
                AppBundleProperties = template.AppBundleProperties,
                TagsProperties = template.TagsProperties,
                NewsProperties = new NewsProperties
                {
                    TurnNo = turn,
                    IsNotificationActive = true,
                    IsRead = false,
                    IsEnabledVariable = variable,
                    Title = title,
                    Description = text,
                    Newspaper = tp?.Newspaper,
                    Index = 0,
                },
                IsRead = false,
                IsNotificationActive = true,
                TurnNo = turn,
                Index = 0,
            };
            ZilleOperation.AddByName(EntityDataManager.newsData, data, n => n.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.NewsData, data, n => n.NameInDatabase);
            Core.Log.Msg($"News [{tp?.Newspaper}] turn {turn}: {title}");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Publishing news '{key}' failed: {ex}");
        }
    }

    /// <summary>A report filed on a country's map token.</summary>
    public static void CountryReport(string key, string token, string title, string text)
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
            Core.Log.Error($"Country report '{key}' failed: {ex}");
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
                    Core.Log.Msg($"  news template {paper}: '{nm}' paper='{n.NewsProperties?.Newspaper}' title='{n.NewsProperties?.Title}'");
                }

                return n;
            }
        }

        return null;
    }

    // ------------------------------------------------------------------ the declaration

    public static void PublishDeclaration()
    {
        string press = Variables.GetString(AnDeclaration.VarPressStatement);
        string kingQuote = press switch
        {
            "occupier" => "\"Rizia is at war with no one but the army occupying our land,\" the King told reporters outside the hall.",
            "assurance" => "Outside the hall, the King promised reporters that \"every Wehzek family in Zille will be safe under the Rizian crown.\"",
            "silent" => "The King left the building without answering reporters' questions.",
            _ => "A palace spokesman said only that \"Rizia is taking back what is hers.\"",
        };

        Publish("Declaration", "RH", "THE KING DECLARES WAR: \"ZILLE WILL BE RIZIAN AGAIN\"",
            "Before the assembled nations of South Merkopa in Konstantium, His Majesty King Romus Toras declared war on Wehlen, " +
            "vowing that Rizia \"will not lay down its arms until the last Wehzek soldier has left Rizian soil.\" The declaration came " +
            "moments after President Wiktor Smolak invoked an obscure Alliance protocol to keep Zille beyond the term of the 1927 lease. " +
            "Crowds gathered outside the Royal Palace through the night, and reservists across Brenas are already reporting to their depots. " +
            kingQuote);

        Publish("Declaration", "G", "Rizia Declares War on Wehlen at Alliance Session",
            "In a scene without precedent in the Alliance of Nations, King Romus Toras of Rizia declared war on Wehlen from the speaker's " +
            "podium in Konstantium. President Nines suspended the session and convened the Security Committee in emergency session. " +
            "The Wehzek delegation walked out of the hall. Valgsland's Chancellor Hegel warned that his government would be watching " +
            "\"very closely,\" while Lespia called on both parties to show restraint. Markets in Lespia and Valgsland fell sharply " +
            "on the news, and shipping insurers have suspended cover for vessels crossing the Strait of Kreleck.");

        Publish("Declaration", "V", "War by Declaration: King Takes Rizia to War Without a Vote",
            "No vote was taken. No delegate was consulted. With a single sentence in a foreign capital, the King has committed the " +
            "nation to a war with Wehlen and its backers in United Contana. Supporters call it the restoration of Rizia's honor; critics " +
            "ask who will pay for it, and who will bleed for it. In Zille, home to Rizians and Wehzeks alike, families who have lived " +
            "side by side for a generation now wait to learn which army will arrive first.");

        CountryReport("Declaration_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Wehlen Mobilizes",
            "President Smolak addressed the Wehzek nation by radio within hours of his return from Konstantium. \"Zille will never " +
            "kneel,\" he declared, announcing a general mobilization and martial law in the Zille territory. Wehzek state radio is " +
            "calling the declaration \"an act of Rumburgian aggression by proxy.\" Our intelligence reports convoys forming at Kratsna.");
        CountryReport("Declaration_Valgsland", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Condemns Rizia",
            "Chancellor Hegel's government has formally condemned the declaration as \"a reckless assault on the order the Alliance " +
            "exists to protect\" and has called on the Security Committee to act. Diplomatic sources suggest United Contana is weighing " +
            "emergency aid to Wehlen.");
        CountryReport("Declaration_Lespia", SuzerainTokenName.RiziaCountryLespia, "Lespia Urges Restraint",
            "Prime Minister Alvarez has publicly called on both Rizia and Wehlen to show restraint. Privately, the Lespian navy has been " +
            "placed on alert in the Sudis Sea, and Lespian traders have been seen buying up grain contracts across the region.");
        CountryReport("Declaration_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "Rumburg's Silence",
            "Rumburg's foreign ministry has issued a brief statement that it \"takes note\" of the King's declaration. Through private " +
            "channels, Rumburgian officials have let it be known that they expect the matter to be concluded quickly, and quietly.");
        CountryReport("Declaration_Pales", SuzerainTokenName.RiziaCountryPales, "Pales Mans Its Border",
            "Duke Reinhart has ordered Palesian forces to the border with the Pales Administrative District \"as a precaution.\" " +
            "Palesian newspapers are asking the question their Duke asked in Konstantium: whether Pales will be next.");
        CountryReport("Declaration_Derdia", SuzerainTokenName.RiziaCountryDerdia, "Derdia's Quiet Approval",
            "Supreme Wiseman Azmal's government has not condemned the declaration. Derdian clerics have spoken of Zille as \"a wound " +
            "that would not close on its own.\" Our consulate reports that the Derdian mercenaries contracted by Wehlen in Zille are " +
            "asking for their pay in advance.");
        CountryReport("Declaration_Morella", SuzerainTokenName.RiziaCountryMorella, "Morella Hesitates",
            "Prime Minister Saltana's government is divided. Morella has abstained from the Security Committee's first statement and " +
            "suspended its shipping in the Gulf of Moridia until further notice.");
        CountryReport("Declaration_Sordland", SuzerainTokenName.RiziaCountrySordland, "Sordland Calls for Calm",
            "The Sordish government has called for calm and offered its good offices to both sides. Sordish trade with Wehlen continues " +
            "for now, and Sordish newspapers are divided on whether the King has made a bold move or a terrible mistake.");
        CountryReport("Declaration_Kyrute", SuzerainTokenName.RiziaCountryKyrute, "Kyrute Watches",
            "Kyrute has made no official statement. Its embassy in Porte Drazon has requested an urgent briefing on the safety of " +
            "Kyrutean nationals in Zille.");
        _logged = true;
    }

    // ------------------------------------------------------------------ operations

    public static void PublishOperationResult(int k, ObjectiveInfo info, bool won, AfterAction a)
    {
        string key = $"Operation{k}";
        if (won)
        {
            Publish(key, "RH", $"VICTORY IN ZILLE: {info.Target?.ToUpperInvariant() ?? "THE LAST POSITIONS"} TAKEN",
                $"{info.Codename} has achieved its objective. {a.Headline} The Royal Army reports {a.EnemyDead} enemy casualties. " +
                $"{a.CitiesTakenSentence}");
            Publish(key, "G", $"Rizian Forces Advance in Zille",
                $"Rizia's {info.Codename} has succeeded, according to the Rizian General Staff. {a.Headline} Independent figures could not " +
                $"be verified. The Alliance Security Committee has again called for a ceasefire.");
            Publish(key, "V", $"The Price of {info.Target ?? "Victory"}",
                $"The Crown celebrates. But {a.PlayerDead} of our soldiers will not come home from {info.Codename}, and the war in Zille " +
                $"is not over. {a.LossesSentence}");
        }
        else
        {
            Publish(key, "RH", $"OUR SOLDIERS FOUGHT BRAVELY IN ZILLE",
                $"Despite the heroism of the Royal Army, {info.Codename} did not achieve all of its objectives. {a.Headline} " +
                $"The General Staff is regrouping for the next phase of the campaign.");
            Publish(key, "G", $"Rizian Offensive Stalls in Zille",
                $"{info.Codename}, Rizia's latest offensive in Zille, has failed to reach its objective. {a.Headline} Wehzek state radio " +
                $"is calling it \"the beginning of the end of the King's adventure.\"");
            Publish(key, "V", $"{info.Codename} Fails",
                $"{a.PlayerDead} soldiers lost, and {info.Target ?? "the last Wehzek positions"} still not taken. {a.LossesSentence} How many " +
                $"more operations will the nation pay for before someone in the palace says enough?");
        }
    }
}
