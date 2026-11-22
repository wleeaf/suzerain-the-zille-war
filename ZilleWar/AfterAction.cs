using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one operation, compared with the map it started from. Feeds the after-action dispatch
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

    // Reads in "{x} enemy soldiers", "{x} of our soldiers" and "{x} men" alike.
    private static string Men(int units, int salt) => units <= 0 ? "a few dozen" : $"{units * 850 + (units * 137 + salt * 31) % 300:N0}";

    private const string General = "General Carlos Robles Azaro";

    /// <summary>
    /// The operation's result report, in the vanilla report tone: a third-person dispatch from the General Staff
    /// with the Commander of the Armed Forces quoted.
    /// </summary>
    public string FullReport(ObjectiveInfo info, bool won)
    {
        bool endWon = ZilleCampaign.Liberated, endLost = ZilleCampaign.Defeated;
        WarCouncil.Orders orders = WarCouncil.CurrentOrders();
        string name = ZilleCampaign.Title(info);
        bool plural = info.Towns.Length > 1;
        var paragraphs = new List<string>
        {
            won
                ? $"{name} has achieved its objective, the General Staff announced from its field headquarters. {info.Target} " +
                  $"{(plural ? "are" : "is")} in Rizian hands. {FrontSentence()}"
                : $"{name} has failed to achieve its objective, the General Staff has informed the Crown. {info.Target} " +
                  $"{(plural ? "remain" : "remains")} in Wehzek hands. {FrontSentence()}",
        };

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
            cities.Add($"{Join(CitiesLost)} {(CitiesLost.Count > 1 ? "have" : "has")} fallen to the enemy.");
        }

        if (plan.Length > 0 || cities.Count > 0)
        {
            paragraphs.Add(string.Join(" ", new[] { plan }.Concat(cities).Where(x => x.Length > 0)));
        }

        string losses = PlayerDead > 0
            ? $"Rizian losses are put at {PlayerDeadMen} men killed, wounded or missing. \"It was not cheap,\" {General}, Commander of " +
              "the Armed Forces, told the Royal Council. \"I have asked the chaplains to write to every family.\""
            : $"Rizian losses were light. {General}, Commander of the Armed Forces, credited \"the men who planned this as much as the " +
              "men who fought it.\"";
        string enemyLosses = EnemyDead > 0
            ? $" Wehzek losses are estimated at {EnemyDeadMen} men, and the prisoner cages at Dapalle are filling."
            : " The enemy gave ground without losing many men, and the Staff expects to meet the same units again.";
        paragraphs.Add(losses + enemyLosses);

        List<string> notes = EventNotes();
        if (notes.Count > 0)
        {
            paragraphs.Add(string.Join(" ", notes));
        }

        if (endWon)
        {
            paragraphs.Add("With this, the war in Zille is over. The last Wehzek garrisons have sent officers under a white flag, and their " +
                           "ships are carrying the survivors home across the Strait of Kreleck. \"For the first time since the lease, there " +
                           "is no foreign army on Rizian soil,\" General Robles Azaro said. \"The men are singing, and I have not had the heart " +
                           "to stop them.\"");
        }
        else if (endLost)
        {
            paragraphs.Add("The General Staff has advised the Royal Council that the army cannot mount another offensive. " +
                           $"{GarrisonSentence()} The Staff has urged a ceasefire before the fighting is carried onto the Rizian side of " +
                           "the border. \"The army has given everything it had,\" General Robles Azaro said. \"The failure is mine, and I will " +
                           "answer for it.\"");
        }
        else
        {
            string next = ZilleCampaign.Available().Count > 0 ? ZilleCampaign.Info(ZilleCampaign.NextObjective).Target : null;
            paragraphs.Add($"{GarrisonSentence()} " + (EnemyCitiesLeft.Count > 0 ? $"Wehlen still holds {Join(EnemyCitiesLeft)}. " : string.Empty) +
                           (next != null
                               ? $"The Staff is ready to move on {next} as soon as the King gives the order. \"I would not wait long,\" " +
                                 "General Robles Azaro said. \"Every week we wait, Wehlen brings more men across the Strait of Kreleck.\""
                               : "The Staff awaits the King's orders."));
        }

        return string.Join("\n\n", paragraphs);
    }

    private string FrontSentence()
    {
        string back = TilesLost > 0 ? ", though the enemy won some of it back elsewhere along the line" : string.Empty;
        return TilesGained switch
        {
            >= 8 => $"Rizian divisions carried the front much further than the Staff had dared to plan{back}.",
            >= 4 => $"Rizian divisions pushed the front well forward{back}.",
            > 0 => $"Rizian divisions gained ground, though less than the Staff had hoped{back}.",
            _ => TilesLost > 0 ? "Wehzek counterattacks drove the Rizian lines back." : "The front line barely moved.",
        };
    }

    /// <summary>How the King's orders from the War Council played out.</summary>
    private static string PlanSentence(WarCouncil.Orders o, ObjectiveInfo info, bool won)
    {
        var s = new List<string>();
        string by = o.Delegated ? "the General Staff" : "the King";
        if (o.Bombard)
        {
            s.Add(won
                ? $"The preparatory barrage ordered by {by} did its work: the Wehzek forward positions were rubble by the time the infantry reached them."
                : $"Not even the preparatory barrage ordered by {by} could break the Wehzek forward positions.");
        }

        switch (o.Approach)
        {
            case WarCouncil.Approach.Feint:
                s.Add(won
                    ? $"A feint drew the Wehzek reserves away from {info.Target}, as the Staff had hoped."
                    : "The enemy did not take the bait of the feint, and their reserves were waiting.");
                break;
            case WarCouncil.Approach.Landing:
                s.Add(won
                    ? "The landing went in behind the Wehzek lines and caught the garrison facing the wrong way."
                    : "The landing proved costly, and the men who reached the beach could not break out of it.");
                break;
            case WarCouncil.Approach.Armor:
                s.Add(won
                    ? "The armored columns went straight up the main road and were through the wall before its garrison knew where to turn."
                    : "The tanks broke through the wall, but the infantry could not keep pace, and the Wehzeks closed the gap behind them.");
                break;
            case WarCouncil.Approach.Airborne:
                s.Add(won
                    ? $"Paratroopers dropped behind the Wehzek lines and held their ground until the columns reached them outside {info.Target}."
                    : "The paratroopers came down where they were meant to, and were left there too long. Few of them came back.");
                break;
            case WarCouncil.Approach.Mountain:
                s.Add(won
                    ? "The mountain troops took the hill forts from above, where nobody had thought to dig in."
                    : "The mountain troops reached the ridges, but the hill forts held, and the valleys below became a killing ground.");
                break;
            default:
                s.Add(won
                    ? "The direct assault moved fast enough that the Wehzek armor never came up in time."
                    : "The direct assault ran straight into prepared Wehzek positions.");
                break;
        }

        switch (o.Navy)
        {
            case "Strait":
                s.Add(won
                    ? "At sea, the fleet kept Wehzek reinforcements from crossing the strait while the battle was fought."
                    : "The fleet held the strait, but what the garrison already had in Zille was enough.");
                break;
            case "Coast":
                s.Add(won
                    ? "Offshore, the fleet's guns silenced the Wehzek coastal batteries before the first wave went in."
                    : "The fleet shelled the coast for two days; the batteries it could not see did the rest.");
                break;
        }

        if (o.Reinforcements > 0)
        {
            s.Add("With nothing stopping their convoys, the Wehzeks had brought fresh troops across the strait the week before the attack.");
        }

        if (o.FleetRecalled)
        {
            s.Add("The squadron recalled from the Aureus field kept the Wehzek navy out of the gulf.");
        }

        return string.Join(" ", s);
    }

    private static string GarrisonSentence() => ZilleCampaign.Tier switch
    {
        "Strong" => "The Wehzek garrison remains strong. Its convoys still cross the strait every night with fresh men and shells.",
        "Default" => "The Wehzek garrison is holding its own. Its convoys still arrive, though no longer in growing numbers.",
        "Strained" => "The Wehzek garrison is running short of fuel and shells. Its convoys arrive late, and some do not arrive at all.",
        _ => "The Wehzek garrison is starving. Deserters speak of half rations, tanks without fuel and rifles without ammunition.",
    };

    /// <summary>Event flags already told in an earlier report (the flags stay set for the rest of the war).</summary>
    public const string VarNotesTold = "ZilleWar.State_ReportNotesTold";

    private static List<string> EventNotes()
    {
        var notes = new List<string>();
        var told = ZilleCampaign.Str(VarNotesTold).Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        void Note(string flag, string text)
        {
            if (!told.Contains(flag) && Variables.GetBool(CityEvents.Flag(flag)))
            {
                notes.Add(text);
                told.Add(flag);
            }
        }

        Note("Zille_BannerRaised", "In Zille, on the King's order, the Golden Banner was raised over the square that once bore Duke Fedo Sazon's name, and the crowd sang until dark.");
        Note("Zille_Restraint", "In Zille, Rizian troops are under royal orders to protect Wehzek residents, and foreign correspondents have taken note.");
        Note("Nanta_PortSeized", "At Nanta, the Wehzek supply ships were taken intact, and their stores are already feeding Rizian troops.");
        Note("Nanta_PortSabotaged", "At Nanta, the piers have been blown, and nothing will land there until the war is over.");
        Note("Isor_Fishermen", "The fishing families of Isor have hung the old colors from their boats.");
        Note("Auremina_MinesSecured", "The Auremina gold mines are under Rizian guard, on the King's order.");
        Note("Auremina_MinersFreed", "In Auremina, miners came up out of the pits to cheer the news that they were free men of Rizia.");
        Note("Evacuation", "Where Rizian troops had to give ground, civilians were brought out first.");
        Note("Counterattack", "Wherever a position was lost, Rizian commanders went straight back for it, as ordered.");
        Variables.Set(VarNotesTold, string.Join(",", told));
        return notes;
    }

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
