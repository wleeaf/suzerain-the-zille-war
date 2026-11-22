using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one operation, compared with the map it started from. Feeds the after-action letter
/// shown as the operation's result report, the War Council's next briefing, and the newspaper coverage.</summary>
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

        a.Headline = a.HeadlineSentence();
        a.CitiesTakenSentence = a.CitiesTaken.Count > 0 ? $"The Golden Banner now flies over {Join(a.CitiesTaken)}." : string.Empty;
        a.LossesSentence = a.CitiesLost.Count > 0 ? $"{Join(a.CitiesLost)} {(a.CitiesLost.Count > 1 ? "have" : "has")} fallen to the enemy." : string.Empty;
        return a;
    }

    private string HeadlineSentence()
    {
        string back = TilesLost > 0 ? ", though Wehzek counterattacks won some of it back" : string.Empty;
        return TilesGained switch
        {
            >= 8 => $"Rizian forces have advanced deep into Zille{back}.",
            >= 4 => $"Rizian forces have pushed the front well forward{back}.",
            > 0 => $"Rizian forces have gained ground in Zille{back}.",
            _ => TilesLost > 0 ? "Wehzek counterattacks have driven the Rizian lines back." : "The front in Zille has barely moved.",
        };
    }

    /// <summary>Estimated men lost: a destroyed formation is roughly a battalion with its support.</summary>
    public string PlayerDeadMen => Men(PlayerDead, 7);
    public string EnemyDeadMen => Men(EnemyDead, 13);

    private static string Men(int units, int salt) => units <= 0 ? "a handful of" : $"{units * 850 + (units * 137 + salt * 31) % 300:N0}";

    /// <summary>The operation's after-action report: a letter to the King from General Carlos Robles Azaro.</summary>
    public string FullReport(ObjectiveInfo info, bool won)
    {
        bool endWon = ZilleCampaign.Liberated, endLost = ZilleCampaign.Defeated;
        WarCouncil.Orders orders = WarCouncil.CurrentOrders();
        string name = ZilleCampaign.Title(info);
        var paragraphs = new List<string> { "Your Majesty," };

        paragraphs.Add(won
            ? $"I have the honor to report that {name} has achieved its objective. {Taken(info)} {FrontSentence()}"
            : $"It is my painful duty to report that {name} has failed. {info.Target} {(info.Towns.Length > 1 ? "remain" : "remains")} in Wehzek hands. {FrontSentence()}");

        string plan = PlanSentence(orders, info, won);
        var cities = new List<string>();
        if (CitiesTaken.Count > 0 && !(CitiesTaken.Count == info.Towns.Length && CitiesTaken.All(info.Towns.Contains)))
        {
            cities.Add($"The Golden Banner now flies over {Join(CitiesTaken)}.");
        }

        if (CitiesRetaken.Count > 0)
        {
            cities.Add($"{Join(CitiesRetaken)} {(CitiesRetaken.Count > 1 ? "are" : "is")} Rizian again.");
        }

        if (CitiesLost.Count > 0)
        {
            cities.Add($"To my great regret, {Join(CitiesLost)} {(CitiesLost.Count > 1 ? "have" : "has")} fallen to the enemy.");
        }

        if (plan.Length > 0 || cities.Count > 0)
        {
            paragraphs.Add(string.Join(" ", new[] { plan }.Concat(cities).Where(x => x.Length > 0)));
        }

        string losses = PlayerDead > 0
            ? $"It was not cheap. Our losses come to some {PlayerDeadMen} men killed, wounded or missing, and I have asked the chaplains to write to every family."
            : "Our losses were light, for which I thank the men who planned this as much as the men who fought it.";
        string enemyLosses = EnemyDead > 0
            ? $" The enemy lost perhaps {EnemyDeadMen} men, and our cages at Dapalle are filling with prisoners."
            : " The enemy gave ground without losing many men, which means we will meet them again.";
        paragraphs.Add(losses + enemyLosses);

        List<string> notes = EventNotes();
        if (notes.Count > 0)
        {
            paragraphs.Add(string.Join(" ", notes));
        }

        if (endWon)
        {
            paragraphs.Add("With this, the war is over. The last Wehzek garrisons have sent officers under a white flag, and their ships are " +
                           "carrying the survivors home across the strait. For the first time since the lease, there is no foreign army on " +
                           "Rizian soil. I am told the men are singing, and I have not had the heart to stop them.");
        }
        else if (endLost)
        {
            paragraphs.Add("I must be plain with Your Majesty. The army has given everything it had, and it has nothing more to give. " +
                           $"{GarrisonSentence()} I have advised the Council to accept the Alliance's offer of a ceasefire before the enemy " +
                           "carries the fighting onto our side of the border. The failure is mine, and I will answer for it.");
        }
        else
        {
            string next = ZilleCampaign.Available().Count > 0 ? ZilleCampaign.Info(ZilleCampaign.NextObjective).Target : null;
            paragraphs.Add($"{GarrisonSentence()} " + (EnemyCitiesLeft.Count > 0 ? $"Wehlen still holds {Join(EnemyCitiesLeft)}. " : string.Empty) +
                           (next != null
                               ? $"The Staff is ready to move on {next} as soon as Your Majesty gives the order. I would not wait long. Every week we wait, Wehlen brings more men across the Strait of Kreleck."
                               : "The Staff awaits Your Majesty's orders."));
        }

        paragraphs.Add("Your obedient servant,\nGeneral Carlos Robles Azaro\nCommander of the Armed Forces, Field Headquarters, Dapalle");
        return string.Join("\n\n", paragraphs);
    }

    private static string Taken(ObjectiveInfo info) => info.Towns.Length > 1 ? $"{info.Target} are in our hands." : $"{info.Target} is in our hands.";

    private string FrontSentence()
    {
        string back = TilesLost > 0 ? ", though the enemy won some of it back elsewhere along the line" : string.Empty;
        return TilesGained switch
        {
            >= 8 => $"Our divisions carried the front much further than the Staff had dared to plan{back}.",
            >= 4 => $"Our divisions pushed the front well forward{back}.",
            > 0 => $"Our divisions gained ground, though not as much as I had hoped{back}.",
            _ => TilesLost > 0 ? "Wehzek counterattacks drove our lines back." : "The front line barely moved.",
        };
    }

    /// <summary>How the King's orders from the War Council played out.</summary>
    private static string PlanSentence(WarCouncil.Orders o, ObjectiveInfo info, bool won)
    {
        var s = new List<string>();
        if (o.Bombard)
        {
            s.Add(won
                ? "The barrage you ordered did its work: their forward positions were rubble by the time the infantry reached them."
                : "Not even the barrage you ordered could break their forward positions.");
        }

        switch (o.Approach)
        {
            case WarCouncil.Approach.Feint:
                s.Add(won
                    ? $"The feint drew their reserves away from {info.Target}, as the Staff had hoped."
                    : "The enemy did not take the bait of our feint, and their reserves were waiting for us.");
                break;
            case WarCouncil.Approach.Landing:
                s.Add(won
                    ? "The landing went in behind their lines and caught the garrison facing the wrong way."
                    : "The landing was costly, and the men who reached the beach could not break out of it.");
                break;
            default:
                s.Add(won
                    ? "The direct assault was fast enough that their armor never came up in time."
                    : "The direct assault ran straight into their prepared positions.");
                break;
        }

        if (o.FleetRecalled)
        {
            s.Add("The squadron you brought north from the Aureus field kept the Wehzek navy out of the gulf.");
        }

        return string.Join(" ", s);
    }

    private static string GarrisonSentence() => ZilleCampaign.Tier switch
    {
        "Strong" => "The Wehzek garrison is still strong. Their convoys cross the strait every night, and every night brings fresh men and shells.",
        "Default" => "The Wehzek garrison is holding its own. Their convoys still come, though no longer in growing numbers.",
        "Strained" => "The Wehzek garrison is running short of fuel and shells. Their convoys arrive late, and some do not arrive at all.",
        _ => "The Wehzek garrison is starving. Deserters speak of half rations, tanks without fuel and rifles without ammunition.",
    };

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
        Note("Nanta_PortSeized", "At Nanta we took the Wehzek supply ships intact. Their stores are already feeding our own men.");
        Note("Nanta_PortSabotaged", "At Nanta the piers have been blown, and nothing will land there until the war is over.");
        Note("Isor_Fishermen", "The fishing families of Isor have hung the old colors from their boats.");
        Note("Auremina_MinesSecured", "The Auremina gold mines are under guard, as you ordered.");
        Note("Auremina_MinersFreed", "The miners of Auremina came up out of the pits to cheer when they heard they were free men of Rizia.");
        Note("Evacuation", "Where we had to give ground, the civilians were brought out first.");
        Note("Counterattack", "Wherever we lost a position, our commanders went straight back for it, as you ordered.");
        return notes;
    }

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
