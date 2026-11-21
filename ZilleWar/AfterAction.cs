using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one operation, compared with the map it started from. Feeds the detailed
/// after-action report shown at the end of the operation and the newspaper coverage.</summary>
internal sealed class AfterAction
{
    public int EnemyDead, PlayerDead, TilesGained, TilesLost;
    public List<string> CitiesTaken = new(), CitiesLost = new(), CitiesRetaken = new(), EnemyCitiesLeft = new();
    public string Headline = string.Empty, CitiesTakenSentence = string.Empty, LossesSentence = string.Empty;

    public static AfterAction Compute(int k, Dictionary<string, bool> before, Dictionary<string, bool> after)
    {
        var a = new AfterAction
        {
            EnemyDead = Variables.GetInt(ZilleCampaign.EnemyDeadVar(k)),
            PlayerDead = Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)),
        };
        foreach ((string tile, bool nowPlayer) in after)
        {
            bool wasPlayer = before.TryGetValue(tile, out bool w) && w;
            if (nowPlayer && !wasPlayer)
            {
                a.TilesGained++;
            }
            else if (!nowPlayer && wasPlayer)
            {
                a.TilesLost++;
            }
        }

        foreach (TownSpec t in ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null))
        {
            bool was = before.TryGetValue(t.Tile, out bool w) && w;
            bool now = after.TryGetValue(t.Tile, out bool n) && n;
            if (now && !was)
            {
                (t.Side == "zille" ? a.CitiesTaken : a.CitiesRetaken).Add(t.Name);
            }
            else if (!now && was)
            {
                a.CitiesLost.Add(t.Name);
            }

            if (!now && t.Side == "zille")
            {
                a.EnemyCitiesLeft.Add(t.Name);
            }
        }

        a.Headline = a.TilesGained > 0
            ? $"Rizian forces advanced across {a.TilesGained} districts of Zille{(a.TilesLost > 0 ? $" while ceding {a.TilesLost} elsewhere" : string.Empty)}."
            : a.TilesLost > 0 ? $"Wehzek counterattacks pushed our lines back across {a.TilesLost} districts." : "The front line barely moved.";
        a.CitiesTakenSentence = a.CitiesTaken.Count > 0 ? $"Rizian flags now fly over {Join(a.CitiesTaken)}." : string.Empty;
        a.LossesSentence = a.CitiesLost.Count > 0 ? $"And {Join(a.CitiesLost)} has been lost to the enemy." : string.Empty;
        return a;
    }

    /// <summary>Estimated men lost: a destroyed formation is roughly a battalion with its support.</summary>
    public string PlayerDeadMen => Men(PlayerDead, 7);
    public string EnemyDeadMen => Men(EnemyDead, 13);

    private static string Men(int units, int salt) => units <= 0 ? "a handful of" : $"{units * 850 + (units * 137 + salt * 31) % 300:N0}";

    /// <summary>The operation's after-action report, written as the Chief of the General Staff's letter to the King.</summary>
    public string FullReport(ObjectiveInfo info, bool won)
    {
        bool endWon = ZilleCampaign.Liberated, endLost = ZilleCampaign.Defeated;
        var paragraphs = new List<string> { "Your Majesty," };

        string opening = won
            ? $"I have the honor to report that {info.Codename} has achieved its objective. {Taken(info)} {FrontSentence()}"
            : $"It is my duty to report that {info.Codename} has failed. {info.Target} remains in Wehzek hands. {FrontSentence()}";
        paragraphs.Add(opening.Trim());

        var cities = new List<string>();
        if (CitiesTaken.Count > 0)
        {
            cities.Add($"Our flag now flies over {Join(CitiesTaken)}");
        }

        if (CitiesRetaken.Count > 0)
        {
            cities.Add($"{Join(CitiesRetaken)} {(CitiesRetaken.Count > 1 ? "are" : "is")} Rizian again");
        }

        if (CitiesLost.Count > 0)
        {
            cities.Add($"to my regret, {Join(CitiesLost)} {(CitiesLost.Count > 1 ? "have" : "has")} fallen to the enemy");
        }

        string losses = PlayerDead > 0
            ? $"It was not cheap. We lost {PlayerDead} {(PlayerDead == 1 ? "formation" : "formations")} in the fighting, some {PlayerDeadMen} men killed, wounded or missing."
            : "Our losses were light, for which I thank the men who planned this as much as those who fought it.";
        string enemyLosses = EnemyDead > 0
            ? $" The enemy lost {EnemyDead} {(EnemyDead == 1 ? "formation" : "formations")}, perhaps {EnemyDeadMen} men, and our cages are filling with prisoners."
            : " The enemy gave ground without losing many men, which means we will meet them again.";
        paragraphs.Add((cities.Count > 0 ? Capitalize(string.Join("; ", cities)) + ". " : string.Empty) + losses + enemyLosses);

        List<string> notes = EventNotes();
        if (notes.Count > 0)
        {
            paragraphs.Add(string.Join(" ", notes));
        }

        if (endWon)
        {
            paragraphs.Add("With this, the war is over. The last Wehzek garrisons have sent officers under a white flag, and their " +
                           "ships are taking the survivors back across the strait. For the first time in twenty-five years, there is " +
                           "no foreign army on Rizian soil. I am told the men are singing.");
        }
        else if (endLost)
        {
            paragraphs.Add("I must be plain with Your Majesty. The army has given everything it had, and it cannot give more. " +
                           $"{GarrisonSentence()} I have advised the Council that we accept the Alliance's offer of a ceasefire " +
                           "before the enemy carries the fighting onto our side of the border. I take full responsibility for this failure.");
        }
        else
        {
            string next = ZilleCampaign.Available().Count > 0 ? ZilleCampaign.Info(ZilleCampaign.NextObjective).Target : null;
            paragraphs.Add($"{GarrisonSentence()} " + (EnemyCitiesLeft.Count > 0 ? $"Wehlen still holds {Join(EnemyCitiesLeft)}. " : string.Empty) +
                           (next != null
                               ? $"The Staff is ready to move on {next} as soon as Your Majesty gives the order, and I would not wait long: every week we wait, Wehlen brings more men across the Strait of Kreleck."
                               : "The Staff awaits Your Majesty's orders."));
        }

        paragraphs.Add("Your obedient servant,\nGeneral Carlos Azaro\nChief of the General Staff, Montaklar");
        return string.Join("\n\n", paragraphs);
    }

    private string Taken(ObjectiveInfo info) => info.Towns.Length > 1 ? $"{info.Target} are in our hands." : $"{info.Target} is in our hands.";

    private string FrontSentence() => TilesGained > 0
        ? $"Our divisions advanced across {TilesGained} {(TilesGained == 1 ? "district" : "districts")} of Zille{(TilesLost > 0 ? $", though the enemy took back {TilesLost} elsewhere" : string.Empty)}."
        : TilesLost > 0 ? $"Wehzek counterattacks pushed our lines back across {TilesLost} {(TilesLost == 1 ? "district" : "districts")}." : "The front line barely moved.";

    private static string GarrisonSentence() => ZilleCampaign.Tier switch
    {
        "Strong" => "The Wehzek garrison is still strong. Their convoys cross the strait every night, and every night brings fresh men and shells.",
        "Default" => "The Wehzek garrison is holding its own. Their convoys still come, though no longer in growing numbers.",
        "Strained" => "The Wehzek garrison is running short of fuel and shells. Their convoys arrive late, and some do not arrive at all.",
        _ => "The Wehzek garrison is starving. Deserters speak of half rations, tanks without fuel and rifles without ammunition.",
    };

    private static string Capitalize(string x) => string.IsNullOrEmpty(x) ? x : char.ToUpperInvariant(x[0]) + x[1..];

    private static List<string> EventNotes()
    {
        var notes = new List<string>();
        void Note(string flag, string text)
        {
            if (Variables.GetBool(CityEvents.Flag(flag)))
            {
                notes.Add(text);
            }
        }

        Note("Zille_BannerRaised", "In Zille, as you ordered, the Golden Banner was raised over the square that once bore Duke Fedo Sazon's name, and the crowd sang until dark.");
        Note("Zille_Restraint", "In Zille, our men have kept your order to protect the Wehzek residents, and the foreign correspondents have noticed.");
        Note("Nanta_PortSeized", "At Nanta we took the Wehzek supply ships intact; their stores are already feeding our own men.");
        Note("Nanta_PortSabotaged", "At Nanta the piers have been blown, and nothing will land there until the war is over.");
        Note("Isor_Fishermen", "The fishing families of Isor have hung the old colors from their boats.");
        Note("Auremina_MinesSecured", "The Auremina gold mines are under guard, as you ordered.");
        Note("Auremina_MinersFreed", "The miners of Auremina came out of the pits to cheer when they heard they were free men of Rizia.");
        Note("Evacuation", "Where we had to give ground, the civilians were brought out first.");
        Note("Counterattack", "Wherever we lost a position, our commanders went straight back for it, as you ordered.");
        return notes;
    }

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
