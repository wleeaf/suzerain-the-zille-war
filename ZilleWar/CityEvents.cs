using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Events fired when a city changes hands during the Zille war. Grounded in vanilla lore:
/// Smolak Square was once Duke Fedo Sazon's square; the Queen Mother grew up by Zille's harbor;
/// Auremina's gold mines; the Wehzek border barrier at Mave Nortus.
/// Options only set flags for now; their wider consequences are wired in the campaign pass.
/// </summary>
internal static class CityEvents
{
    public static string CapturedName(string city) => $"ZW_Decision_War_{Key(city)}_Captured";
    public static string LostName(string city) => $"ZW_Decision_War_{Key(city)}_Lost";
    public static string Flag(string what) => $"ZilleWar.Event_{what}";

    private static string Key(string city) => city.Replace(" ", string.Empty);

    private static readonly string[] Flags =
    {
        "Zille_BannerRaised", "Zille_Restraint", "Nanta_PortSeized", "Nanta_PortSabotaged", "Isor_Fishermen",
        "Auremina_MinesSecured", "Auremina_MinersFreed", "Reprisals", "Evacuation", "Counterattack",
    };

    public static void RegisterVariables()
    {
        foreach (string f in Flags)
        {
            Variables.Register(Flag(f));
        }
    }

    private static Action Set(string flag) => () => Variables.Set(Flag(flag), true);

    public static IEnumerable<WarEvent> All()
    {
        // --- Wehlen-held cities taken by Rizia ---
        yield return new WarEvent(CapturedName("Zille"), "The Golden Banner over Zille",
            "Our columns are in the old town. The Wehzek garrison has fallen back from the harbor, and crowds are gathering " +
            "in Smolak Square — the square that, before the lease, bore the name of Duke Fedo Sazon. Some cheer. Many watch " +
            "in silence from behind shuttered windows. The world is watching what Rizia does next in Zille.",
            new[]
            {
                new EventOption("Raise the Golden Banner over the square and restore its old name.", Set("Zille_BannerRaised")),
                new EventOption("No celebrations yet. Order the troops to protect the Wehzek residents.", Set("Zille_Restraint")),
            });

        yield return new WarEvent(CapturedName("Nanta"), "The Port of Nanta",
            "Nanta's harbor is ours. Wehzek supply ships still sit at the piers, half unloaded. The harbormaster reports that " +
            "Wehlen's convoys across the Strait of Kreleck came through here every night.",
            new[]
            {
                new EventOption("Seize the ships and the stores. They will feed our own men.", Set("Nanta_PortSeized")),
                new EventOption("Scuttle the piers. Nothing will land here again until this war is over.", Set("Nanta_PortSabotaged")),
            });

        yield return new WarEvent(CapturedName("Isor"), "Isor Taken",
            "Isor has fallen with little resistance. The fishing families of the west coast — Rizian before the lease, and " +
            "Rizian still in their own minds — have hung the old colors from their boats.",
            new[] { new EventOption("Let them be seen. Word will travel up the coast.", Set("Isor_Fishermen")) });

        yield return new WarEvent(CapturedName("Auremina"), "The Gold Hills of Auremina",
            "Auremina is ours, and with it the mines that drew Rusty Montoro north decades ago. The Wehzek overseers fled " +
            "before our vanguard arrived. The miners, many of them Rizian, wait at the pit heads to see what kind of masters we are.",
            new[]
            {
                new EventOption("Secure the mines. The Crown's gold stays in the ground until the war is won.", Set("Auremina_MinesSecured")),
                new EventOption("Open the gates. Tell the miners they are free men of Rizia.", Set("Auremina_MinersFreed")),
            });

        // --- Wehlen retakes a Zille city ---
        foreach (string city in new[] { "Zille", "Nanta", "Isor", "Auremina" })
        {
            yield return new WarEvent(LostName(city), $"{city} Lost",
                $"A Wehzek counterattack has driven our forces out of {city}. Reports from the town speak of arrests among " +
                "those who welcomed us. Our officers ask for orders.",
                new[]
                {
                    new EventOption("Regroup and retake it. We do not abandon those who stood with us.", Set("Counterattack")),
                    new EventOption("Pull back to a defensible line. We will return.", null),
                });
        }

        // --- Rizian cities taken by Wehlen ---
        yield return new WarEvent(LostName("Mave Nortus"), "Mave Nortus Falls",
            "Wehzek armor has rolled through the gap in the border wall and into Mave Nortus. The royal railway station " +
            "where the King's train once stopped is burning. Refugees are streaming south toward Brenas.",
            new[]
            {
                new EventOption("Organize the evacuation. Get the civilians out first.", Set("Evacuation")),
                new EventOption("No retreat. Every man to the line.", Set("Counterattack")),
            });

        foreach ((string city, string flavour) in new[]
                 {
                     ("Dapalle", "The river crossing at Dapalle is in Wehzek hands, and with it the road into the heart of Brenas."),
                     ("Balle", "Balle has fallen. Wehzek patrols are on the shores of Lake Jardim, deep inside Brenas."),
                     ("Jales", "Jales has fallen. The enemy holds a Sudis Sea port on Rizian soil — the first in a generation."),
                 })
        {
            yield return new WarEvent(LostName(city), $"{city} Falls",
                flavour + " The war has come to Rizia proper, and the nation will hold the Crown responsible.",
                new[]
                {
                    new EventOption("Rally the reserves and counterattack.", Set("Counterattack")),
                    new EventOption("Order a fighting withdrawal. Save the army.", null),
                });
        }

        foreach (string city in new[] { "Mave Nortus", "Dapalle", "Balle", "Jales" })
        {
            yield return new WarEvent(CapturedName(city), $"{city} Liberated",
                $"Our troops have driven the Wehzek forces out of {city}. The townspeople are coming out of their cellars.",
                new[] { new EventOption("Restore order and press on.", null) });
        }
    }
}
