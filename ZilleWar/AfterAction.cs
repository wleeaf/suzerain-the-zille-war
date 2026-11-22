using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one phase of the operation, compared with the map it started from. Feeds the General Staff's
/// after-action dispatch (the phase's report, and the operation's result report after the last phase), the War Council's next
/// briefing, and the newspaper coverage.</summary>
internal sealed class AfterAction
{
    public int EnemyDead, PlayerDead, TilesGained, TilesLost;
    public List<string> CitiesTaken = new(), CitiesLost = new(), CitiesRetaken = new(), EnemyCitiesLeft = new();
    public string Headline = string.Empty, CitiesTakenSentence = string.Empty, LossesSentence = string.Empty;

    /// <summary>The four Wehzek-held cities of the Zille region.</summary>
    public static readonly string[] ZilleCities = { "Zille", "Auremina", "Isor", "Nanta" };

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

    /// <summary>0 light (a formation or none lost), 1 heavy (two or three), 2 severe (four or more).</summary>
    public int Cost => PlayerDead <= 1 ? 0 : PlayerDead <= 3 ? 1 : 2;

    // Reads in "{x} enemy soldiers", "{x} of our soldiers" and "{x} men" alike.
    private static string Men(int units, int salt) => units <= 0 ? "a few dozen" : $"{units * 850 + (units * 137 + salt * 31) % 300:N0}";

    private const string General = "General Carlos Robles Azaro";

    /// <summary>
    /// The phase's after-action dispatch, in the vanilla report tone: a third-person dispatch from the General Staff with the
    /// Commander of the Armed Forces quoted. Its opening, the general's words, the order of its paragraphs and its closing change
    /// with the phase, the result and the losses, so no two dispatches of one war read alike.
    /// </summary>
    public string FullReport(ObjectiveInfo info, bool won)
    {
        (int k, _, int phase) = ZilleCampaign.CurrentOperation();
        int p = Math.Clamp(phase <= 0 ? Math.Max(1, k) : phase, 1, ZilleCampaign.LastPhase);
        bool liberated = ZilleCampaign.Liberated, endLost = ZilleCampaign.Defeated;
        bool final = liberated || endLost;
        bool zilleHeld = Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
        WarCouncil.Orders orders = WarCouncil.CurrentOrders();
        string name = ZilleCampaign.PhaseName(p);
        bool plural = info.Towns.Length > 1;
        bool carried = p <= ZilleCampaign.Sequence.Length && ZilleCampaign.Sequence[p - 1] != info.Id;
        var extra = CitiesTaken.Where(c => !info.Towns.Contains(c)).ToList();

        string opening = Opening(p, won, final, liberated, zilleHeld, carried, info, name, plural, k) + " " + FrontSentence(p);
        string plan = PlanSentence(orders, info, won, p);
        string cities = CitiesParagraph(info, extra);
        string losses = LossesParagraph(p, won, final, k);
        List<string> notes = EventNotes();
        string landed = LandedSentence(p, orders);
        string closing = Closing(p, won, final, liberated, zilleHeld, carried, info);

        var paragraphs = new List<string>();
        void Add(params string[] parts)
        {
            string s = string.Join(" ", parts.Where(x => !string.IsNullOrEmpty(x)));
            if (s.Length > 0)
            {
                paragraphs.Add(s);
            }
        }

        switch (p)
        {
            case 1:
                // The first dispatch: result, how the plan worked, the cost, the towns, what comes next.
                Add(opening);
                Add(plan, cities);
                Add(losses);
                Add(string.Join(" ", notes));
                Add(closing);
                break;
            case 2:
                // The second leads with the result and the towns, then what Wehlen had sent across the strait, then the plan.
                Add(opening, cities);
                Add(landed, plan);
                Add(string.Join(" ", notes));
                Add(losses);
                Add(closing);
                break;
            default:
                // The last one is a summing-up: this phase, then the whole operation's cost, then where it leaves the war.
                Add(opening);
                Add(cities, landed);
                Add(plan);
                Add(string.Join(" ", notes));
                Add(losses);
                Add(closing);
                break;
        }

        return string.Join("\n\n", paragraphs);
    }

    private static string Is(bool plural) => plural ? "are" : "is";

    private string Opening(int p, bool won, bool final, bool liberated, bool zilleHeld, bool carried, ObjectiveInfo info, string name, bool plural,
        int k)
    {
        string target = info.Target;
        if (liberated && p < ZilleCampaign.LastPhase)
        {
            return p == 1
                ? $"{name} has done in a single month what the General Staff had planned to do in three. {target} {Is(plural)} in Rizian " +
                  "hands, the Wehzek line has broken behind it, and the garrisons still holding out have asked for terms."
                : $"{name} has finished the work the General Staff expected to leave to a third phase. {target} {Is(plural)} Rizian, and what " +
                  "remained of the Wehzek garrison has stopped fighting.";
        }

        if (final)
        {
            if (liberated)
            {
                return $"The final phase of {ZilleCampaign.OperationCodename} is over, and so is Wehlen's hold on Zille. {target} {Is(plural)} " +
                       "in Rizian hands, the General Staff reports, and no Wehzek unit on the mainland is still fighting.";
            }

            if (zilleHeld)
            {
                return won
                    ? $"The final phase of {ZilleCampaign.OperationCodename} has been fought to its end, and its last days gave the army " +
                      $"what the first two phases had not: {target} {Is(plural)} Rizian. The rest of the region is not."
                    : $"The final phase of {ZilleCampaign.OperationCodename} has been fought to its end without reaching {target}, the General " +
                      "Staff has reported to the Crown. The army holds what it took in the earlier phases, and no more.";
            }

            return $"The final phase of {ZilleCampaign.OperationCodename} has ended with the city of Zille still in Wehzek hands. " +
                   "The General Staff has informed the Crown that the army has nothing left with which to try again.";
        }

        switch (p)
        {
            case 1:
                return won
                    ? $"{name} has reached its aim, the General Staff reports from its field headquarters at Mave Nortus. {target} {Is(plural)} " +
                      "in Rizian hands."
                    : $"{name} has ended without taking {target}, the General Staff has informed the Crown. {target} {(plural ? "remain" : "remains")} " +
                      "in Wehzek hands, and the next phase will go for it again.";
            case 2:
                if (carried)
                {
                    return won
                        ? $"At the second attempt, {target} has fallen. The General Staff reports that {name}, sent against the objective the " +
                          "opening phase could not take, has carried it."
                        : $"For the second time, {target} has held. {name} went for the objective the opening phase had missed, and the " +
                          "General Staff reports that it has missed it too.";
                }

                return won
                    ? $"By the last evening of {name}, Rizian troops stood in {target}. The General Staff's dispatch from the front calls " +
                      "the aim of the phase reached."
                    : $"{name} has been halted short of {target}. The General Staff's dispatch speaks of hard fighting and ground taken, " +
                      "but not of the objective.";
            default:
                return won
                    ? $"{name} has taken {target}, the General Staff reports."
                    : $"{name} has not taken {target}, the General Staff reports.";
        }
    }

    private string FrontSentence(int p)
    {
        string back = TilesLost > 0
            ? p switch
            {
                1 => ", though the enemy won some of it back elsewhere along the line",
                2 => ", even if Wehzek counterattacks clawed part of it back",
                _ => ", though not all of it could be held",
            }
            : string.Empty;
        return TilesGained switch
        {
            >= 8 => p switch
            {
                1 => $"Rizian divisions carried the front much further than the Staff had dared to plan{back}.",
                2 => $"The front has moved further this month than in any month of the war{back}.",
                _ => $"In the last push the divisions covered more ground than in either phase before it{back}.",
            },
            >= 4 => p switch
            {
                1 => $"Rizian divisions pushed the front well forward{back}.",
                2 => $"The line now runs miles beyond where the phase began{back}.",
                _ => $"The divisions moved the line forward one last time{back}.",
            },
            > 0 => p switch
            {
                1 => $"Rizian divisions gained ground, though less than the Staff had hoped{back}.",
                2 => $"The ground gained was measured in villages, not in valleys{back}.",
                _ => $"What ground was gained came a field at a time{back}.",
            },
            _ => TilesLost > 0
                ? p switch
                {
                    1 => "Wehzek counterattacks drove the Rizian lines back.",
                    2 => "The Wehzeks came out of their positions and took ground from us.",
                    _ => "In the end it was the enemy who moved the line, and not in our favor.",
                }
                : p switch
                {
                    1 => "The front line barely moved.",
                    2 => "On the map the line is where it was a month ago.",
                    _ => "The line ended the phase almost where it began it.",
                },
        };
    }

    private string CitiesParagraph(ObjectiveInfo info, List<string> extra)
    {
        var s = new List<string>();
        if (extra.Count > 0)
        {
            s.Add($"Beyond the aim of the phase, the Golden Banner now flies over {Join(extra)} as well.");
        }

        var partOfAim = CitiesTaken.Where(info.Towns.Contains).ToList();
        if (partOfAim.Count > 0 && partOfAim.Count < info.Towns.Length)
        {
            s.Add($"{Join(partOfAim)} {(partOfAim.Count > 1 ? "were" : "was")} taken, but not the rest of the objective.");
        }

        if (CitiesRetaken.Count > 0)
        {
            s.Add($"{Join(CitiesRetaken)} {(CitiesRetaken.Count > 1 ? "are" : "is")} Rizian again.");
        }

        var backLost = CitiesLost.Where(ZilleCities.Contains).ToList();
        var ownLost = CitiesLost.Where(c => !ZilleCities.Contains(c)).ToList();
        if (backLost.Count > 0)
        {
            s.Add($"{Join(backLost)}, taken earlier in the operation, {(backLost.Count > 1 ? "have" : "has")} been lost to a Wehzek counterattack.");
        }

        if (ownLost.Count > 0)
        {
            s.Add($"On our own side of the old border, {Join(ownLost)} {(ownLost.Count > 1 ? "have" : "has")} fallen to the enemy.");
        }

        return string.Join(" ", s);
    }

    /// <summary>Rizian losses across every phase fought so far (for the last dispatch's summing-up).</summary>
    private static int OperationDead(int k)
    {
        int sum = 0;
        for (int i = 1; i <= Math.Max(1, k); i++)
        {
            sum += Variables.GetInt(ZilleCampaign.PlayerDeadVar(i));
        }

        return sum;
    }

    private string LossesParagraph(int p, bool won, bool final, int k)
    {
        string ours = Cost switch
        {
            0 => p switch
            {
                1 => $"Rizian losses were light. {General}, Commander of the Armed Forces, credited \"the men who planned this as much as " +
                     "the men who fought it.\"",
                2 => $"Our losses this month were the lightest of the war. \"I will not call it cheap,\" {General} told the Royal Council. " +
                     "\"I will call it well done, and leave it there.\"",
                _ => $"The last phase cost us few men. \"Of all the things I will report to His Majesty, that is the one I am proudest of,\" " +
                     $"{General} said.",
            },
            1 => p switch
            {
                1 => $"Rizian losses are put at {PlayerDeadMen} men killed, wounded or missing. \"It was not cheap,\" {General}, Commander " +
                     "of the Armed Forces, told the Royal Council. \"I have asked the chaplains to write to every family.\"",
                2 => $"The phase cost {PlayerDeadMen} men killed, wounded or missing. {General} has asked that the hospital trains be given " +
                     "priority over ammunition on the northern line for a week. \"The shells can wait,\" he said. \"The wounded cannot.\"",
                _ => $"This last phase cost {PlayerDeadMen} men killed, wounded or missing. {General} read the figure to the Royal Council " +
                     "without comment, and then asked for a minute's silence.",
            },
            _ => p switch
            {
                1 => $"Rizian losses are severe: {PlayerDeadMen} men killed, wounded or missing. \"We paid for every yard,\" {General} told " +
                     "the Royal Council. \"I will not pretend to His Majesty that we can pay at this rate for long.\"",
                2 => $"The losses are the heaviest of the war so far, {PlayerDeadMen} men killed, wounded or missing. Several battalions " +
                     $"have been merged with their neighbors because there were not enough men left to call them battalions. \"I have " +
                     $"broken good units this month,\" {General} said. \"I will have to answer for that, and I will.\"",
                _ => $"The last phase was the costliest: {PlayerDeadMen} men killed, wounded or missing. \"Nobody who was there will call " +
                     $"it anything but a slaughter,\" {General} said, \"and I will not ask them to.\"",
            },
        };

        if (p >= ZilleCampaign.LastPhase || final)
        {
            int all = OperationDead(k);
            if (k > 1 && all > PlayerDead)
            {
                ours += $" Across the whole operation, the Staff counts {Men(all, 3)} men killed, wounded or missing.";
            }
        }

        string theirs = EnemyDead > 0
            ? p switch
            {
                1 => $" Wehzek losses are estimated at {EnemyDeadMen} men, and the prisoner cages at Dapalle are filling.",
                2 => $" The enemy lost an estimated {EnemyDeadMen} men. A column of prisoners was marched through Mave Nortus on the way " +
                     "south, and the townspeople came out to watch it pass in silence.",
                _ => $" The Wehzek garrison lost an estimated {EnemyDeadMen} men in the last phase alone.",
            }
            : p switch
            {
                1 => " The enemy gave ground without losing many men, and the Staff expects to meet the same units again.",
                2 => " The Wehzeks pulled back in good order, which the Staff finds more worrying than if they had run.",
                _ => " The enemy's losses were slight, and its line is intact.",
            };
        return ours + theirs;
    }

    private static string LandedSentence(int p, WarCouncil.Orders o)
    {
        if (p < 2)
        {
            return string.Empty;
        }

        int landed = Variables.GetInt(PhaseResume.VarLanded);
        if (landed <= 0)
        {
            return o.Navy == "Strait" || WorldSystems.StraitClosedNow
                ? "Nothing reached the Wehzek garrison across the Strait of Kreleck between the phases, and its units went into this one " +
                  "with whatever they had left."
                : string.Empty;
        }

        string how = o.Navy == "Strait"
            ? "despite the fleet in the strait"
            : WorldSystems.StraitClosedNow ? "despite the patrols in the strait" : "with nothing at sea to stop them";
        return landed == 1
            ? $"Between the phases Wehlen landed a fresh formation near its ports, {how}, and it was waiting for us."
            : $"Between the phases Wehlen landed {landed} fresh formations near its ports, {how}, and they were waiting for us.";
    }

    private static string Noun(WarCouncil.Approach a) => a switch
    {
        WarCouncil.Approach.Feint => "feint",
        WarCouncil.Approach.Armor => "armored thrust",
        WarCouncil.Approach.Landing => "landing",
        WarCouncil.Approach.Airborne => "airborne drop",
        WarCouncil.Approach.Mountain => "move over the ridges",
        _ => "direct assault",
    };

    private static string Spent(WarCouncil.Approach a) => a switch
    {
        WarCouncil.Approach.Armor => "most of the tanks",
        WarCouncil.Approach.Landing => "the marines and their ships",
        WarCouncil.Approach.Airborne => "the paratroopers",
        _ => "the mountaineers",
    };

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    /// <summary>How the war plan the King set at the first council, and his other orders, played out in this phase.</summary>
    private static string PlanSentence(WarCouncil.Orders o, ObjectiveInfo info, bool won, int p)
    {
        var s = new List<string>();
        string by = o.Delegated ? "the General Staff" : "the King";
        if (o.Bombard)
        {
            s.Add(p switch
            {
                1 => won
                    ? $"The preparatory barrage ordered by {by} did its work: the Wehzek forward positions were rubble by the time the infantry reached them."
                    : $"Not even the preparatory barrage ordered by {by} could break the Wehzek forward positions.",
                2 => won
                    ? "The guns opened the phase again, and this time the Wehzeks had not had a month to dig deeper."
                    : "The guns opened the phase again, but the Wehzeks had spent the month digging, and the shells found mostly earth.",
                _ => won
                    ? "The last of the artillery's stock went into the opening barrage, and none of it was wasted."
                    : "The artillery fired the last of its stock in the opening barrage, and the positions it was meant to break were still manned when the infantry arrived.",
            });
        }

        string chosen = p == 1 ? $"the {Noun(o.Plan)} the King chose for the campaign" : $"the King's {Noun(o.Plan)}";
        string target = info.Target;
        switch (o.Adapt)
        {
            case "hills":
                s.Add($"In the hills the tanks could not lead, so the Staff carried on {chosen} as an infantry push up the valley road, with the " +
                      $"armor close behind. {(won ? "It was enough." : "It was not enough.")}");
                break;
            case "inland":
                s.Add($"{target} lies far from the sea, so the Staff carried on {chosen} by putting the marines ashore on the coast behind the hills" +
                      (won ? ", where they drew the Wehzek reserves down to meet them." : ", but the Wehzek reserves did not come down to meet them."));
                break;
            case "units":
                s.Add(p == 1
                    ? $"With {Spent(o.Plan)} already lost, the Staff fought the phase as a direct assault" +
                      (won ? ", and it was enough." : ", and it ran straight into prepared Wehzek positions.")
                    : $"With {Spent(o.Plan)} spent in the earlier fighting, {chosen} gave way to a plain assault" +
                      (won ? ", which the enemy had not expected from us any more." : ", which is what the enemy had been waiting for."));
                break;
            default:
                s.Add(p == 1 ? FirstPlan(o.Approach, chosen, target, won) : p == 2 ? SecondPlan(o.Approach, target, won) : LastPlan(o.Approach, target, won));
                break;
        }

        s.Add(NavySentence(o, info, won, p));
        if (o.FleetRecalled && p == 1)
        {
            s.Add("The squadron recalled from the Aureus field kept the Wehzek navy out of the gulf.");
        }

        return string.Join(" ", s.Where(x => x.Length > 0));
    }

    private static string FirstPlan(WarCouncil.Approach a, string chosen, string target, bool won) => a switch
    {
        WarCouncil.Approach.Feint => won
            ? $"{Cap(chosen)} drew the Wehzek reserves away from {target}."
            : $"The enemy did not take the bait of {chosen}, and their reserves were waiting.",
        WarCouncil.Approach.Landing => won
            ? $"{Cap(chosen)} went in behind the Wehzek lines and caught the garrison facing the wrong way."
            : $"{Cap(chosen)} proved costly, and the men who reached the beach could not break out of it.",
        WarCouncil.Approach.Armor => won
            ? $"{Cap(chosen)} went straight up the main road and was through the Wehzek line before its garrison knew where to turn."
            : $"The tanks of {chosen} broke through, but the infantry could not keep pace, and the Wehzeks closed the gap behind them.",
        WarCouncil.Approach.Airborne => won
            ? $"{Cap(chosen)} put paratroopers behind the Wehzek lines, and they held their ground until the columns reached them outside {target}."
            : $"{Cap(chosen)} put the paratroopers down where they were meant to be, and they were left there too long. Few of them came back.",
        WarCouncil.Approach.Mountain => won
            ? $"{Cap(chosen)} put the mountain troops on the high ground above the Wehzek positions, where nobody had thought to dig in."
            : $"{Cap(chosen)} put the mountain troops on the ridges, but the positions below held, and the valleys became a killing ground.",
        _ => won
            ? $"{Cap(chosen)} moved fast enough that the Wehzek armor never came up in time."
            : $"{Cap(chosen)} ran straight into prepared Wehzek positions.",
    };

    private static string SecondPlan(WarCouncil.Approach a, string target, bool won) => a switch
    {
        WarCouncil.Approach.Feint => won
            ? $"The Wehzeks had learned to expect a feint, so the Staff made the feint the real attack, and {target} fell to the blow they had been told to ignore."
            : "The Wehzek commanders had read the first phase too well, and this time they kept their reserves where they were.",
        WarCouncil.Approach.Landing => won
            ? $"The marines went ashore again, on a beach the Wehzeks had not thought worth mining, and took {target} from the side it did not face."
            : "The marines went ashore again, but the Wehzeks had mined every beach within a day's march, and the landing never got off the sand.",
        WarCouncil.Approach.Armor => won
            ? "The tanks led again, and the Wehzek anti-tank guns, moved up during the lull, were in the wrong valley."
            : "The tanks led again, into anti-tank guns the Wehzeks had moved up during the lull, and too many of them did not come back.",
        WarCouncil.Approach.Airborne => won
            ? "The paratroopers dropped a second time, closer to the objective and in better weather, and held the bridges until the columns came up."
            : "The second drop was scattered by the wind across the hills, and the columns could not reach the men in time.",
        WarCouncil.Approach.Mountain => won
            ? "The mountaineers took the ridges again, and the Wehzeks, who had expected them in the valleys this time, were outflanked once more."
            : "The Wehzeks had put their own men on the ridges this time, and the mountaineers fought them for the high ground for a week without taking it.",
        _ => won
            ? "The assault went in on a broad front and kept going, and the Wehzek line gave way in three places at once."
            : "The assault went in on a broad front, and the Wehzek line bent everywhere and broke nowhere.",
    };

    private static string LastPlan(WarCouncil.Approach a, string target, bool won) => a switch
    {
        WarCouncil.Approach.Feint => won
            ? "The last feint of the war worked as well as the first."
            : "By the last phase the Wehzeks no longer believed anything the army showed them, and the feint drew nobody away.",
        WarCouncil.Approach.Landing => won
            ? $"The marines made their last landing of the war on the beaches below {target}, and walked into the town by the harbor road."
            : $"The last landing went in under the guns of {target}, and the boats that came back were carrying wounded.",
        WarCouncil.Approach.Armor => won
            ? "What was left of the tanks led the final push, and it was enough."
            : "What was left of the tanks led the final push, and there was not enough left of them.",
        WarCouncil.Approach.Airborne => won
            ? "The paratroopers made their last drop of the war at dawn, and by evening the columns had reached them."
            : "The paratroopers made their last drop of the war, and the columns never reached them.",
        WarCouncil.Approach.Mountain => won
            ? "The mountaineers came down off the ridges for the last time, onto the coast road behind the Wehzek positions."
            : "The mountaineers held the ridges to the end, but the coast road below them stayed Wehzek.",
        _ => won
            ? "The final assault was fought by tired men, and it carried."
            : "The final assault was fought by tired men against men who were just as tired, and it stalled.",
    };

    /// <summary>The navy's part. Its orders carry over between phases, so each phase tells it in its own words.</summary>
    private static string NavySentence(WarCouncil.Orders o, ObjectiveInfo info, bool won, int p)
    {
        switch (o.Navy)
        {
            case "Strait":
                return p switch
                {
                    1 => won
                        ? "At sea, the fleet kept Wehzek reinforcements from crossing the strait while the battle was fought."
                        : "The fleet held the strait, but what the garrison already had in Zille was enough.",
                    2 => o.SubsOut
                        ? "The fleet stayed on the strait, and the submarines with it. Two Wehzek transports were sunk at night within sight of Nanta."
                        : "The fleet stayed on the strait through the phase, and the convoys that tried to cross turned back.",
                    _ => "For the last phase the fleet still held the strait, and the Wehzek garrison fought without hope of relief from across it.",
                };
            case "Coast":
                return p switch
                {
                    1 => won
                        ? "Offshore, the fleet's guns silenced the Wehzek coastal batteries before the first wave went in."
                        : "The fleet shelled the coast for two days; the batteries it could not see did the rest.",
                    2 => info.Id == Objective.Wall
                        ? "The fleet's guns were of little use so far from the sea, and the Staff kept the ships offshore to cover the coast road."
                        : won
                            ? $"The fleet came back inshore and shelled the batteries at {info.Target} again, and this time the guns stayed silent."
                            : $"The fleet came back inshore, but the batteries at {info.Target} had been moved since the first phase, and the ships shelled empty emplacements.",
                    _ => won
                        ? $"The fleet came close inshore for the last phase and shelled the harbor batteries at {info.Target} until they fell silent."
                        : $"The fleet came close inshore for the last phase, and a destroyer was hit by the batteries at {info.Target} before it could pull back.",
                };
            default:
                return p switch
                {
                    1 => string.Empty,
                    2 => "The fleet stayed in home waters, and the army fought the phase without it.",
                    _ => "The fleet never left home waters, and the last phase was fought by the army alone.",
                };
        }
    }

    private static string GarrisonSentence(int p) => (ZilleCampaign.Tier, p) switch
    {
        ("Strong", 1) => "The Wehzek garrison remains strong. Its convoys still cross the strait every night with fresh men and shells.",
        ("Strong", _) => "The garrison is as strong as it was at the start of the war. Wehlen has replaced every man it lost.",
        ("Default", 1) => "The Wehzek garrison is holding its own. Its convoys still arrive, though no longer in growing numbers.",
        ("Default", _) => "The garrison has made good its losses, but only just, and prisoners say the new men are boys and reservists.",
        ("Strained", 1) => "The Wehzek garrison is running short of fuel and shells. Its convoys arrive late, and some do not arrive at all.",
        ("Strained", _) => "Wehzek guns now fire by the dozen where they fired by the hundred, and their trucks are towed by horses.",
        (_, 1) => "The Wehzek garrison is starving. Deserters speak of half rations, tanks without fuel and rifles without ammunition.",
        _ => "Deserters now come over every night. They say the garrison eats once a day and has stopped expecting the convoys.",
    };

    private static string Closing(int p, bool won, bool final, bool liberated, bool zilleHeld, bool carried, ObjectiveInfo info)
    {
        if (liberated)
        {
            return p < ZilleCampaign.LastPhase
                ? "Officers of the Wehzek garrison have come through the lines under a white flag to ask how their men may lay down their " +
                  "arms. \"I had a third phase planned to the last detail,\" General Robles Azaro said. \"I am very happy to put it in a drawer.\""
                : "Officers of the last Wehzek garrisons have come through the lines under a white flag, and the Staff is arranging their " +
                  "surrender. \"For the first time since the lease, there is no foreign army on Rizian soil,\" General Robles Azaro said. " +
                  "\"The men are singing, and I have not had the heart to stop them.\"";
        }

        if (final)
        {
            return zilleHeld
                ? "The Staff has advised the Royal Council that the army has no fourth push in it this year. The men are exhausted, the " +
                  "stocks of shells are spent, and the Alliance's observers are already asking both armies where they stand. \"We hold the " +
                  "city and the harbor,\" General Robles Azaro said. \"The rest will be decided by people who were not there.\""
                : "The Staff has advised the Royal Council that the army cannot mount another offensive, and has urged that the fighting stop " +
                  "before it is carried onto the Rizian side of the border. \"The army has given everything it had,\" General Robles Azaro " +
                  "said. \"The failure is mine, and I will answer for it.\"";
        }

        string next = ZilleCampaign.Available().Count > 0 ? ZilleCampaign.Info(ZilleCampaign.NextObjective).Target : null;
        string garrison = GarrisonSentence(p);
        if (next == null)
        {
            return $"{garrison} The Staff awaits the King's orders.";
        }

        bool again = next == info.Target;
        return p switch
        {
            1 => $"{garrison} " + (again
                ? $"The Staff asks to go for {next} again in the second phase. \"We know their positions now,\" General Robles Azaro said. " +
                  "\"They know ours. Next month it comes down to who has used the time better.\""
                : $"The second phase, against {next}, can begin as soon as the King gives the order. \"The Wehzeks will use every night we " +
                  "give them,\" General Robles Azaro said. \"Their convoys do not keep office hours.\""),
            _ => $"{garrison} " + (again
                ? $"The final phase will have to go for {next} once more. \"One more try is all the army has in it,\" General Robles Azaro " +
                  "told the Royal Council, \"so it had better be the right one.\""
                : $"One phase remains, against {next}. \"The men know it is the last push,\" General Robles Azaro said. \"I have told " +
                  "them it is, and I mean to keep my word.\""),
        };
    }

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
        Note("Isor_Pilots", "Skippers from Isor are now guiding our patrol boats through the shoals of the western strait.");
        Note("Auremina_MinesSecured", "The Auremina gold mines are under Rizian guard, on the King's order.");
        Note("Auremina_MinersFreed", "In Auremina, miners came up out of the pits to cheer the news that they were free men of Rizia.");
        Note("Evacuation", "Where Rizian troops had to give ground, civilians were brought out first.");
        Note("Counterattack", "Wherever a position was lost, Rizian commanders went straight back for it, as ordered.");
        Note("Withdrawal", "Where the line could not be held, the army fell back in good order rather than be cut off.");
        Variables.Set(VarNotesTold, string.Join(",", told));
        return notes;
    }

    private static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
