using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// Events fired when a city changes hands during the Zille war. Grounded in vanilla lore:
/// Smolak Square was Fedo Square before the Wehzek administration renamed it; the Queen Mother grew up by Zille's harbor;
/// Auremina's gold mines; the Wehzek border barrier at Mave Nortus.
/// Every option has a consequence: vanilla resources (shown in parentheses, as on vanilla decisions), public opinion,
/// foreign goodwill, the Wehzek garrison's strength, and a story in the papers. The flags are read by the after-action
/// report, the codex and the world's news (CityEvents.Flag).
/// </summary>
internal static class CityEvents
{
    public static string CapturedName(string city) => $"ZW_Decision_War_{Key(city)}_Captured";
    public static string LostName(string city) => $"ZW_Decision_War_{Key(city)}_Lost";
    public static string Flag(string what) => $"ZilleWar.Event_{what}";

    private static string Key(string city) => city.Replace(" ", string.Empty);

    private static readonly string[] Flags =
    {
        "Zille_BannerRaised", "Zille_Restraint", "Nanta_PortSeized", "Nanta_PortSabotaged", "Isor_Fishermen", "Isor_Pilots",
        "Auremina_MinesSecured", "Auremina_MinersFreed", "Reprisals", "Evacuation", "Counterattack", "Withdrawal",
    };

    public static void RegisterVariables()
    {
        foreach (string f in Flags)
        {
            Variables.Register(Flag(f));
        }
    }

    // ------------------------------------------------------------------ consequences

    private static void Lua(string code)
    {
        try
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(LuaFix.Rewrite(code));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"City event script failed: {ex.Message}");
        }
    }

    /// <summary>Add to a vanilla number without rounding it (vanilla resources can hold fractions).</summary>
    private static void Add(string variable, int delta) =>
        Lua($"Variable[\"{variable}\"] = (Variable[\"{variable}\"] or 0) + ({delta})");

    private const string Budget = "RiziaDLC.Resources_Budget";
    private const string Authority = "RiziaDLC.Resources_Authority";
    private const string Manpower = "RiziaDLC.Resources_Military_Manpower";
    private const string Equipment = "RiziaDLC.Resources_Military_Equipment";
    private const string Opinion = "RiziaDLC.Country_PublicOpinion";
    private const string Valgsland = "RiziaDLC.Diplomacy_Valgsland_NegotiatingPower";
    private const string Rumburg = "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower";

    private static void Weaken()
    {
        if (ZilleCampaign.Strength > 3)
        {
            ZilleCampaign.Strength -= 1;
        }
    }

    private static Action Do(string flag, Action more) => () =>
    {
        if (flag != null)
        {
            Variables.Set(Flag(flag), true);
        }

        more?.Invoke();
    };

    // A city can change hands more than once: every story is told once per playthrough.
    private static void News(string key, string paper, string title, params string[] paragraphs)
    {
        if (WorldSystems.Once($"CityNews_{key}_{paper}"))
        {
            NewsWire.Publish($"City_{key}", paper, title, NewsWire.P(paragraphs));
        }
    }

    private static void Dispatch(string key, string token, string title, params string[] paragraphs)
    {
        if (WorldSystems.Once($"CityReport_{key}"))
        {
            NewsWire.Report($"City_{key}", token, title, NewsWire.P(paragraphs));
        }
    }

    // ------------------------------------------------------------------ the events

    public static IEnumerable<WarEvent> All()
    {
        // --- Wehlen-held cities taken by Rizia ---
        yield return new WarEvent(CapturedName("Zille"), "The Golden Banner over Zille",
            "Our columns are in the old town. The Wehzek garrison has fallen back from the harbor, and crowds are gathering in " +
            "Smolak Square, which was Fedo Square before the lease. Some cheer. Many watch in silence from behind shuttered " +
            "windows. The foreign correspondents have their cameras ready, and the world is waiting to see what Rizia does next in Zille.",
            new[]
            {
                new EventOption("Raise the Golden Banner over the square and give it back its old name. (+1 Authority)", Do("Zille_BannerRaised", () =>
                {
                    Add(Authority, 1);
                    Add(Opinion, 2);
                    Add(Valgsland, -1);
                    News("ZilleBanner", "RH", "The Golden Banner Flies Over Fedo Square",
                        "At noon yesterday, soldiers of the Royal Army raised the Golden Banner over the main square of Zille, and the city " +
                        "council's old enamel sign, hidden in a cellar since 1927, was screwed back onto the wall: Fedo Square.",
                        "The crowd sang the anthem twice, the second time louder. Axa Victoria!");
                    News("ZilleBanner", "G", "Rizia Celebrates in Zille as Wehzek Residents Stay Indoors",
                        "Rizian troops celebrated the capture of Zille with a flag-raising in the city's main square, which they have renamed " +
                        "after its pre-war name. Wehzek residents largely stayed indoors.",
                        "Valgsland's foreign ministry called the ceremony \"a victory parade over a civilian population.\"");
                })),
                new EventOption("No celebrations yet. Order the troops to protect the Wehzek residents. (-1 Authority)", Do("Zille_Restraint", () =>
                {
                    Add(Authority, -1);
                    Add(Valgsland, 1);
                    Add(Rumburg, 1);
                    News("ZilleRestraint", "G", "Rizian Troops Guard Wehzek Quarter in Zille",
                        "Foreign correspondents in Zille report that Rizian soldiers have been posted outside Wehzek homes and sanctuaries in the " +
                        "harbor district, on the King's orders. Looting has been rare, and the military police have made arrests among " +
                        "Rizian civilians as well.",
                        "Even Valgish radio, which had promised its listeners a massacre, conceded that the city was calm.");
                    News("ZilleRestraint", "V", "No Parade in Zille, and the Nationalists Are Furious",
                        "Su Omina's newspaper calls it betrayal. The Voice of Rizia calls it the first sensible order of this war. Soldiers " +
                        "who guard their enemy's grandmothers win more than streets.");
                })),
            });

        yield return new WarEvent(CapturedName("Nanta"), "The Port of Nanta",
            "Nanta's harbor is ours. Wehzek supply ships still sit at the piers, half unloaded. The harbormaster reports that " +
            "Wehlen's convoys across the Strait of Kreleck came through here every night.",
            new[]
            {
                new EventOption("Seize the ships and the stores. They will feed our own men. (+1 Budget, +250 Military Equipment)", Do("Nanta_PortSeized", () =>
                {
                    Add(Budget, 1);
                    Add(Equipment, 250);
                    Dispatch("NantaPrizes", SuzerainTokenName.RiziaCityMontaklar, "Prizes of Nanta",
                        "The Wehzek ships taken at Nanta have been sailed south under Rizian crews. Their holds held fuel, flour, boots and " +
                        "enough artillery shells to supply a division for a week.",
                        "The quartermasters are delighted. The Wehzek convoys, our naval officers warn, will simply find another harbor.");
                })),
                new EventOption("Blow up the piers. Nothing will land here again until this war is over.", Do("Nanta_PortSabotaged", () =>
                {
                    Weaken();
                    Dispatch("NantaPiers", SuzerainTokenName.RiziaCityMontaklar, "The Piers of Nanta Are Down",
                        "Army engineers have blown the piers and the harbor cranes of Nanta. No Wehzek convoy can unload there now, and the " +
                        "garrison's last supply line across the strait has been cut in half.",
                        "The harbormaster watched the cranes fall without a word. Rebuilding them, he told our officers, will take years.");
                })),
            });

        yield return new WarEvent(CapturedName("Isor"), "Isor Taken",
            "Isor has fallen with little resistance. The fishing families of the west coast, Rizian before the lease and Rizian " +
            "still in their own minds, have hung the old colors from their boats. Their skippers know every shoal and channel of the " +
            "strait, and they are already asking what they can do.",
            new[]
            {
                new EventOption("Let them be seen. Word will travel up the coast.", Do("Isor_Fishermen", () =>
                {
                    Add(Opinion, 1);
                    News("IsorColors", "RH", "The Old Colors of Isor",
                        "Every fishing boat in Isor flew the Rizian colors this morning. Some of the flags were older than the men who raised " +
                        "them. The Royal Herald salutes the people of Isor, who never forgot whose shore they live on.");
                })),
                new EventOption("Hire the skippers as pilots for the Royal Navy. (-1 Budget)", Do("Isor_Pilots", () =>
                {
                    Add(Budget, -1);
                    Weaken();
                    Dispatch("IsorPilots", SuzerainTokenName.RiziaCitySallabes, "The Pilots of Isor",
                        "Fishermen from Isor have begun guiding our patrol boats through the shoals of the western strait, where the Wehzek " +
                        "convoys hid at night. Two transports were caught at anchor on the first night.",
                        "The skippers are paid in silver, and they asked for it in advance. Wehzek radio has promised to hang every one of them.");
                })),
            });

        yield return new WarEvent(CapturedName("Auremina"), "The Gold Hills of Auremina",
            "Auremina is ours, and with it the mines that drew Rizian Royal Gold north decades ago. The Wehzek overseers fled " +
            "before our vanguard arrived, leaving the strongroom of the refinery locked behind them. The miners, many of them " +
            "Rizian, wait at the pit heads to see what kind of masters we are.",
            new[]
            {
                new EventOption("Secure the mines and open the strongroom for the treasury. (+1 Budget)", Do("Auremina_MinesSecured", () =>
                {
                    Add(Budget, 1);
                    Add(Opinion, -1);
                    News("AureminaGold", "V", "The Army Finds the Gold First",
                        "The first thing the Royal Army secured in Auremina, according to the miners we spoke to, was not the hospital or " +
                        "the school but the refinery's strongroom. Its contents, some three billion guilders in gold bars, are on their way to Porte Drazon. The miners are still " +
                        "waiting to be paid for the last month they worked under the Wehzeks.");
                })),
                new EventOption("Open the gates. Tell the miners they are free men of Rizia. (+250 Manpower)", Do("Auremina_MinersFreed", () =>
                {
                    Add(Manpower, 250);
                    Add(Opinion, 1);
                    News("AureminaMiners", "RH", "The Miners of Auremina Take Up Arms",
                        "Within a day of their liberation, more than two hundred miners of Auremina marched down to the army's depot and " +
                        "asked for rifles. \"We have dug Wehlen's gold for twenty-five years,\" their foreman said. \"Now we will dig its " +
                        "trenches.\"");
                })),
            });

        // --- Wehlen retakes a Zille city ---
        foreach ((string city, string title, string text) in new[]
                 {
                     ("Zille", "Zille Lost",
                      "A Wehzek counterattack from the hills has driven our forces out of Zille. The harbor district fell first, then the old " +
                      "town, and the Wehzek flag is back over the square we gave its old name. People who hung our colors from their windows " +
                      "are taking them down in a hurry. Our officers ask for orders."),
                     ("Nanta", "Nanta Lost",
                      "Wehzek troops landed from the strait at night have retaken the port of Nanta. Our garrison was pushed back along the " +
                      "coast road, and the dockers who worked for us are being rounded up on the quays. The garrison commander asks what he is to do."),
                     ("Isor", "Isor Lost",
                      "A Wehzek column has come down the coast and retaken Isor. The fishing boats that flew our colors are being searched one " +
                      "by one, and the skippers have fled out to sea. Our officers want to know whether they are to go back."),
                     ("Auremina", "Auremina Lost",
                      "A Wehzek counterattack has driven our forces out of Auremina and back down the valley road. The miners who cheered us " +
                      "have gone back underground, where, they say, nobody can find them. The battalion commanders on the valley road ask for orders."),
                 })
        {
            string c = city;
            yield return new WarEvent(LostName(c), title, text,
                new[]
                {
                    new EventOption("Regroup and retake it. We do not abandon those who stood with us. (-250 Military Equipment)", Do("Counterattack", () =>
                    {
                        Add(Equipment, -250);
                        Add(Opinion, 1);
                    })),
                    new EventOption("Pull back to a defensible line. We will return.", Do("Withdrawal", () =>
                    {
                        Variables.Set(Flag("Reprisals"), true);
                        Add(Opinion, -1);
                        Dispatch($"Reprisals_{Key(c)}", WorldSystems.ZilleToken, $"Arrests in {c}",
                            $"Wehzek military police have returned to {c} with lists of names. Families who hung out Rizian flags when our " +
                            "troops arrived have been taken away for questioning, and some of them have not come back.");
                    })),
                });
        }

        // --- Rizian cities taken by Wehlen ---
        yield return new WarEvent(LostName("Mave Nortus"), "Mave Nortus Falls",
            "Wehzek armor has rolled through the gap in the border wall and into Mave Nortus. The royal railway station " +
            "where the King's train once stopped is burning. Refugees are streaming south toward Brenas.",
            new[]
            {
                new EventOption("Organize the evacuation. Get the civilians out first. (-1 Budget)", Do("Evacuation", () =>
                {
                    Add(Budget, -1);
                    Add(Opinion, 1);
                    News("MaveNortusEvacuation", "RH", "The Last Train from Mave Nortus",
                        "Under Wehzek shellfire, the railwaymen of Mave Nortus ran trains south through the night until every civilian who " +
                        "wanted to leave had left. The Royal Herald will print their names, every one of them.");
                })),
                new EventOption("No retreat. Every man to the line. (-250 Manpower)", Do("Counterattack", () =>
                {
                    Add(Manpower, -250);
                    Add(Opinion, -2);
                    News("MaveNortusHold", "V", "Mave Nortus: Held by Whom?",
                        "The order from the palace was to hold Mave Nortus at any cost. The cost is being paid by the townspeople, who were " +
                        "told to stay in their cellars while the army fought in their streets.");
                })),
            });

        foreach ((string city, string flavour) in new[]
                 {
                     ("Dapalle", "The river crossing at Dapalle is in Wehzek hands, and with it the road into the heart of Brenas. The prisoner " +
                                 "cages outside the town were emptied in the night, and the Wehzek prisoners walked out to meet their own army."),
                     ("Balle", "Balle has fallen. Wehzek patrols are on the shores of Lake Jardim, deep inside Brenas, and the fishermen have " +
                               "pulled their boats up on the beach. Nobody in Porte Drazon thought the war could come this far south."),
                     ("Jales", "Jales has fallen. The enemy holds a Sudis Sea port on Rizian soil, the first in a generation, and Wehzek " +
                               "ships can now unload on our side of the strait."),
                 })
        {
            string c = city;
            yield return new WarEvent(LostName(c), $"{c} Falls", flavour,
                new[]
                {
                    new EventOption("Rally the reserves and counterattack. (-250 Manpower, -250 Military Equipment)", Do("Counterattack", () =>
                    {
                        Add(Manpower, -250);
                        Add(Equipment, -250);
                    })),
                    new EventOption("Order a fighting withdrawal. Save the army. (-1 Authority)", Do("Withdrawal", () =>
                    {
                        Add(Authority, -1);
                        Add(Opinion, -1);
                        News($"{Key(c)}Withdrawal", "V", $"The Army Leaves {c}",
                            $"The people of {c} watched the Royal Army march out of their town yesterday, and the Wehzek army march in a few " +
                            "hours later. The palace calls it a fighting withdrawal. In the town, they call it something else.");
                    })),
                });
        }

        foreach ((string city, string text) in new[]
                 {
                     ("Mave Nortus", "Our troops have driven the Wehzek forces out of Mave Nortus. The railway station is a shell, but the " +
                                     "stationmaster was on the platform before the smoke had cleared, chalking up the times of trains that do not yet run."),
                     ("Dapalle", "Our troops have retaken the river crossing at Dapalle. The townspeople are coming out of their cellars, and " +
                                 "the first of them are already clearing the rubble from the bridge approach."),
                     ("Balle", "Our troops have driven the Wehzek patrols away from Lake Jardim and back out of Balle. The fishermen are " +
                               "pushing their boats back into the water, and the sanctuary bell, silent for weeks, rang at noon."),
                     ("Jales", "Our troops have retaken Jales and its harbor. The Wehzek ships that unloaded there have slipped away across " +
                               "the Sudis Sea, leaving the quays piled with crates nobody has yet dared to open."),
                 })
        {
            string c = city;
            yield return new WarEvent(CapturedName(c), $"{c} Liberated", text,
                new[]
                {
                    new EventOption("Restore order and press on.", () => Add(Opinion, 1)),
                    new EventOption("Send army engineers and supplies to rebuild the town. (-1 Budget)", () =>
                    {
                        Add(Budget, -1);
                        Add(Opinion, 2);
                        News($"{Key(c)}Rebuilt", "RH", $"{c} Rises Again",
                            $"Army engineers and relief trains have reached {c} days after its liberation. Roofs are going back on, the bakery " +
                            "has reopened, and the schoolchildren have been promised their classrooms back before the month is out.");
                    }),
                });
        }
    }
}
