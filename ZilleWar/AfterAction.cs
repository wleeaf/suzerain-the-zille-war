using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>What happened in one phase of the operation, compared with the map it started from. Feeds the General Staff's
/// after-action dispatch (the phase's report, and the operation's result report after the last phase), the War Council's next
/// briefing, and the newspaper coverage.</summary>
internal sealed class AfterAction
{
    public int K, EnemyDead, PlayerDead, TilesGained, TilesLost;
    public List<string> CitiesTaken = new(), CitiesLost = new(), CitiesRetaken = new(), EnemyCitiesLeft = new();
    public string Headline = string.Empty, CitiesTakenSentence = string.Empty, LossesSentence = string.Empty;

    /// <summary>The Zille cities that were Rizian when the phase began, and the Rizian towns still in Wehzek hands at its end.</summary>
    public List<string> ZilleHeldBefore = new(), RizianHeld = new();

    // ---- the phase against its aim (set by Classify): what every report and paper tells
    /// <summary>The aim's towns still Wehzek when the phase began ("Isor" once Nanta is ours), those taken, those missed.</summary>
    public List<string> AimStart = new(), AimTaken = new(), AimMissed = new();
    /// <summary>Zille cities taken beyond the aim; everything won (Zille cities and Rizian towns); losses split by side; the aim's
    /// cities that were ours when the phase began and were lost in it (Nanta, while the phase went for Isor).</summary>
    public List<string> Extra = new(), Gained = new(), BackLost = new(), OwnLost = new(), AimLost = new();
    /// <summary>Won: every city of the aim is Rizian at the end (ZilleCampaign's result). Partial: part of the aim taken, not all
    /// of it held (ZilleCampaign's partial flag). Gain / Loss: any city won / lost. The same rules as <see cref="Front.Phase"/>.</summary>
    public bool Won, Partial, Gain, Loss;
    /// <summary>The aim's name as it stood when the phase began: "Isor and Nanta", or "Isor" once Nanta is held.</summary>
    public string AimName = string.Empty;
    public Objective AimId;

    /// <summary>The four Wehzek-held cities of the Zille region.</summary>
    public static readonly string[] ZilleCities = { "Zille", "Auremina", "Isor", "Nanta" };

    public static AfterAction Compute(int k, Dictionary<string, bool> before, Dictionary<string, bool> after)
    {
        var a = new AfterAction
        {
            K = k,
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

            if (was && t.Side == "zille")
            {
                a.ZilleHeldBefore.Add(t.Name);
            }

            if (!now && t.Side != "zille")
            {
                a.RizianHeld.Add(t.Name);
            }
        }

        a.Headline = a.HeadlineSentence();
        a.CitiesTakenSentence = a.CitiesTaken.Count > 0 ? $"The Golden Banner now flies over {Join(a.CitiesTaken)}." : string.Empty;
        a.LossesSentence = a.CitiesLost.Count > 0 ? $"{Join(a.CitiesLost)} {Has(a.CitiesLost)} fallen to the enemy." : string.Empty;
        return a;
    }

    /// <summary>Tell the phase against its aim: the aim as it stood, what of it was taken and missed, and what else was won or lost.</summary>
    public AfterAction Classify(ObjectiveInfo info)
    {
        AimId = info.Id;
        AimStart = info.Towns.Where(t => !ZilleHeldBefore.Contains(t)).ToList();
        if (AimStart.Count == 0)
        {
            AimStart = info.Towns.ToList();
        }

        AimTaken = AimStart.Where(CitiesTaken.Contains).ToList();
        AimMissed = AimStart.Where(t => !CitiesTaken.Contains(t)).ToList();
        AimLost = info.Towns.Where(CitiesLost.Contains).ToList();
        // As ZilleCampaign books it: the aim is reached when all of its cities are Rizian at the end (so taking Isor while losing
        // Nanta is part of the aim, not all of it), and partial when some of it was taken without that.
        Won = !info.Towns.Any(EnemyCitiesLeft.Contains);
        Partial = !Won && AimTaken.Count > 0;
        Extra = CitiesTaken.Where(c => !info.Towns.Contains(c)).ToList();
        Gained = CitiesTaken.Concat(CitiesRetaken).ToList();
        BackLost = CitiesLost.Where(ZilleCities.Contains).ToList();
        OwnLost = CitiesLost.Where(c => !ZilleCities.Contains(c)).ToList();
        Gain = Gained.Count > 0;
        Loss = CitiesLost.Count > 0;
        AimName = AimStart.Count == info.Towns.Length ? info.Target : Join(AimStart);
        return this;
    }

    // ---- earlier phases, from the history ("Harbor:won;Ports:lost") and the per-phase lists (older saves have only the history)

    /// <summary>The objective of the j-th phase fought and whether it reached it, or null when the history has no such entry.</summary>
    public static (Objective obj, bool won)? HistoryEntry(int j)
    {
        string[] hist = ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (j < 1 || j > hist.Length)
        {
            return null;
        }

        string[] h = hist[j - 1].Split(':');
        return h.Length == 2 && Enum.TryParse(h[0], out Objective o) ? (o, h[1] == "won") : null;
    }

    /// <summary>The phases fought in a row against this objective, ending with the k-th (1 the first try, 2 the second...).</summary>
    public static int Tries(int k, Objective o)
    {
        int n = 1;
        for (int j = k - 1; j >= 1 && HistoryEntry(j) is { } h && h.obj == o; j--)
        {
            n++;
        }

        return n;
    }

    private static bool Captured(string c) => Variables.GetBool(ZilleOperation.CapturedVar(c));

    /// <summary>The j-th phase won a city (Front reads older saves the same way as every other reader).</summary>
    public static bool PhaseGained(int j) => Front.Of(j) is { } ph && ph.Gained.Count > 0;

    /// <summary>The j-th phase took part of a two-city aim.</summary>
    public static bool PhasePartial(int j) => Front.Of(j) is { } ph && ph.Partial;

    /// <summary>The city was won by a phase before the k-th (so taking it now is winning it back).</summary>
    public static bool HeldBefore(string city, int k)
    {
        for (int j = 1; j < k; j++)
        {
            if (Front.Of(j) is { } ph && ph.Gained.Contains(city))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The Zille cities in Rizian hands now.</summary>
    public static List<string> HeldNow() => ZilleCities.Where(Captured).ToList();

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
    private static string Men(int units, int salt) => units <= 0 ? "a few dozen" : (units * 850 + (units * 137 + salt * 31) % 300).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private const string General = "General Carlos Robles Azaro";

    /// <summary>
    /// The phase's after-action dispatch, in the vanilla report tone: a third-person dispatch from the General Staff with the
    /// Commander of the Armed Forces quoted. Its opening, the general's words, the order of its paragraphs and its closing change
    /// with the phase, the result and the losses, so no two dispatches of one war read alike.
    /// </summary>
    public string FullReport(ObjectiveInfo info, bool won)
    {
        Classify(info);
        (int k, _, int phase) = ZilleCampaign.CurrentOperation();
        int p = Math.Clamp(phase <= 0 ? Math.Max(1, k) : phase, 1, ZilleCampaign.LastPhase);
        bool liberated = ZilleCampaign.Liberated, endLost = ZilleCampaign.Defeated;
        bool final = liberated || endLost;
        bool zilleHeld = Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
        WarCouncil.Orders orders = WarCouncil.CurrentOrders();
        string name = ZilleCampaign.PhaseName(p);
        int tries = Tries(Math.Max(1, k), info.Id);
        bool wonBack = AimStart.Any(t => HeldBefore(t, k));

        string opening = Opening(p, k, final, liberated, zilleHeld, tries, wonBack, name) + " " + FrontSentence(p);
        string plan = PlanSentence(orders, Won || Partial, p);
        string cities = CitiesParagraph(liberated);
        string losses = LossesParagraph(p, Won, final, k);
        List<string> notes = EventNotes(_retell);
        string landed = LandedSentence(p, orders);
        string closing = Closing(p, final, liberated, zilleHeld, info);

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

    public static string Is(List<string> l) => l.Count > 1 ? "are" : "is";
    public static string Has(List<string> l) => l.Count > 1 ? "have" : "has";
    public static string Was(List<string> l) => l.Count > 1 ? "were" : "was";
    public static string Nth(int n) => n switch { <= 1 => "first", 2 => "second", 3 => "third", _ => "fourth" };

    /// <summary>A city as prose: "the city of Zille", "Auremina", "the port of Nanta", or a Rizian town's name.</summary>
    public static string Place(string c) => c switch
    {
        "Zille" => "the city of Zille",
        "Isor" or "Nanta" => $"the port of {c}",
        _ => c,
    };

    /// <summary>Cities as prose, the two ports together: "the city of Zille and the ports of Isor and Nanta".</summary>
    public static string Places(List<string> cities)
    {
        bool bothPorts = cities.Contains("Isor") && cities.Contains("Nanta");
        var parts = new List<string>();
        foreach (string c in cities)
        {
            if (bothPorts && c is "Isor" or "Nanta")
            {
                if (!parts.Contains("the ports of Isor and Nanta"))
                {
                    parts.Add("the ports of Isor and Nanta");
                }
            }
            else
            {
                parts.Add(Place(c));
            }
        }

        return Join(parts);
    }

    /// <summary>
    /// The dispatch's first sentences: what the phase did against its aim (all of it, part of it, none of it but other cities, or
    /// nothing), or how the war ended. Gains outside the aim and losses are named in <see cref="CitiesParagraph"/>.
    /// </summary>
    private string Opening(int p, int k, bool final, bool liberated, bool zilleHeld, int tries, bool wonBack, string name)
    {
        string aim = AimName;
        string has = Has(AimStart), isAre = Is(AimStart);
        string taken = CitiesTaken.Count > 0 ? Join(CitiesTaken) : aim;
        string takenIs = CitiesTaken.Count > 1 || (CitiesTaken.Count == 0 && AimStart.Count > 1) ? "are" : "is";
        string gained = TookWords();
        string struck = Loss ? " Elsewhere on the line, the enemy struck back." : string.Empty;
        if (liberated && p < ZilleCampaign.LastPhase)
        {
            return p == 1
                ? $"{name} has done in a single month what the General Staff had planned to do in three. {taken} {takenIs} in Rizian " +
                  "hands, the Wehzek line has broken behind them, and the garrisons still holding out have asked for terms."
                : $"{name} has finished the work the General Staff expected to leave to a third phase. {taken} {takenIs} Rizian, and what " +
                  "remained of the Wehzek garrison has stopped fighting.";
        }

        if (final)
        {
            if (liberated)
            {
                return $"The final phase of {ZilleCampaign.OperationCodename} is over, and so is Wehlen's hold on Zille. {taken} {takenIs} " +
                       "in Rizian hands, the General Staff reports, and no Wehzek unit in the region is still fighting.";
            }

            string last = p >= ZilleCampaign.LastPhase ? $"The final phase of {ZilleCampaign.OperationCodename}" : name;
            if (zilleHeld)
            {
                if (Won)
                {
                    string rest = $"{Cap(Places(EnemyCitiesLeft))} {Is(EnemyCitiesLeft)} still in Wehzek hands.";
                    return wonBack
                        ? $"{last} has been fought to its end, and it has won back {aim}. {rest}{struck}"
                        : $"{last} has been fought to its end, and its last days gave the army what the earlier phases had not: {aim} " +
                          $"{isAre} Rizian. {rest}{struck}";
                }

                if (Partial)
                {
                    return $"{last} has been fought to its end. It took {Join(AimTaken)} {ButNot()}, the General Staff has " +
                           $"reported to the Crown.{struck}";
                }

                if (Gain)
                {
                    return $"{last} has been fought to its end without reaching {aim}, the General Staff has reported to the Crown, though " +
                           $"it {gained} on the way.{struck}";
                }

                return $"{last} has been fought to its end without reaching {aim}, the General Staff has reported to the Crown. " +
                       (Loss || TilesLost > TilesGained
                           ? "The army still holds the city of Zille, and less around it than when the phase began."
                           : "The army holds what it took in the earlier phases, and no more.");
            }

            return $"{last} has ended with the city of Zille {(BackLost.Contains("Zille") ? "back" : "still")} in Wehzek hands. " +
                   (Gain
                       ? $"The army {gained} on the way, but the General Staff has informed the Crown that it has nothing " +
                         "left with which to go for the city again."
                       : "The General Staff has informed the Crown that the army has nothing left with which to try again.");
        }

        if (p == 1)
        {
            // Phase I always goes for the city of Zille.
            if (Won)
            {
                return $"{name} has reached its aim, the General Staff reports from its field headquarters behind the front. {aim} {isAre} " +
                       $"in Rizian hands.{struck}";
            }

            return Gain
                ? $"{name} has ended without taking {aim}, the General Staff has informed the Crown, though the army {gained} on " +
                  $"the way. {aim} {(AimStart.Count > 1 ? "remain" : "remains")} in Wehzek hands, and the next phase will go for it again.{struck}"
                : $"{name} has ended without taking {aim}, the General Staff has informed the Crown. {aim} {(AimStart.Count > 1 ? "remain" : "remains")} " +
                  "in Wehzek hands, and the next phase will go for it again.";
        }

        if (tries > 1)
        {
            // The same objective as the phase before: the aim it missed (or the part of it still Wehzek).
            string before = tries > 2 ? "every phase before it" : k == 2 ? "the opening phase" : "the last phase";
            if (Won)
            {
                return $"At the {Nth(tries)} attempt, {aim} {has} fallen. The General Staff reports that {name}, sent against the objective " +
                       $"{before} could not take, has carried it.{struck}";
            }

            if (Partial)
            {
                return $"At the {Nth(tries)} attempt, {Join(AimTaken)} {Has(AimTaken)} fallen, but " +
                       (AimMissed.Count > 0 ? $"{Join(AimMissed)} {Has(AimMissed)} held. " : $"{Join(AimLost)} {Has(AimLost)} been lost. ") +
                       $"{name} went for the objective {before} had missed, and has carried part of it.{struck}";
            }

            if (Gain)
            {
                return $"{aim} {has} held again, the General Staff reports, but {name} did not come back empty-handed: it {gained} " +
                       $"instead.{struck}";
            }

            return $"For the {Nth(tries)} time, {aim} {has} held. {name} went for the objective {before} had missed, and the General " +
                   "Staff reports that it has missed it too.";
        }

        if (Won)
        {
            return wonBack
                ? $"By the last evening of {name}, Rizian troops were back in {aim}. The General Staff's dispatch from the front calls the " +
                  $"aim of the phase reached.{struck}"
                : $"By the last evening of {name}, Rizian troops stood in {aim}. The General Staff's dispatch from the front calls the aim " +
                  $"of the phase reached.{struck}";
        }

        if (Partial)
        {
            return $"{name} has taken {Join(AimTaken)}, {ButNot()}, the General Staff reports. Its dispatch calls the aim " +
                   $"of the phase half reached.{struck}";
        }

        if (Gain)
        {
            return $"{name} has been halted short of {aim}, the General Staff reports, but not before it {gained}.{struck}";
        }

        return $"{name} has been halted short of {aim}. The General Staff's dispatch speaks of hard fighting" +
               (TilesGained > 0 ? " and ground taken" : string.Empty) + ", but not of the objective.";
    }

    /// <summary>"but not Isor", or, when the rest of the aim was ours and fell in the same phase, "but lost Nanta".</summary>
    private string ButNot() => AimMissed.Count > 0 ? $"but not {Join(AimMissed)}" : $"but lost {Join(AimLost)}";

    /// <summary>What the phase won, as a verb phrase: "took Nanta", "won back Mave Nortus", "took Isor and won back Zille".</summary>
    public string TookWords(bool places = false)
    {
        bool Back(string c) => CitiesRetaken.Contains(c) || HeldBefore(c, K);
        List<string> fresh = Gained.Where(c => !Back(c)).ToList(), back = Gained.Where(Back).ToList();
        Func<List<string>, string> names = places ? Places : Join;
        return fresh.Count == 0 ? $"won back {names(back)}"
            : back.Count == 0 ? $"took {names(fresh)}"
            : $"took {names(fresh)} and won back {names(back)}";
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

    /// <summary>
    /// The cities beyond what the opening told: cities taken beyond an aim that was reached (in part or in full), Rizian towns won
    /// back, and every city lost. When the aim was missed, the opening already names what was won instead.
    /// </summary>
    private string CitiesParagraph(bool liberated)
    {
        var s = new List<string>();
        bool aimTold = Won || Partial;
        if (aimTold && !liberated && Extra.Count > 0)
        {
            s.Add($"Beyond the aim of the phase, the Golden Banner now flies over {Join(Extra)} as well.");
        }

        if ((aimTold || liberated) && CitiesRetaken.Count > 0)
        {
            s.Add($"{Join(CitiesRetaken)} {Is(CitiesRetaken)} Rizian again.");
        }

        if (BackLost.Count > 0)
        {
            s.Add($"{Join(BackLost)}, taken earlier in the operation, {Has(BackLost)} been lost to a Wehzek counterattack.");
        }

        if (OwnLost.Count > 0)
        {
            s.Add($"On our own side of the old border, {Join(OwnLost)} {Has(OwnLost)} fallen to the enemy.");
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
        // "the heaviest", "the costliest", "the lightest" only when they hold against the phases fought before this one
        var earlier = Enumerable.Range(1, Math.Max(0, k - 1)).Select(i => Variables.GetInt(ZilleCampaign.PlayerDeadVar(i))).ToList();
        bool heaviest = earlier.Count == 0 || PlayerDead > earlier.Max();
        bool lightest = earlier.Count == 0 || PlayerDead < earlier.Min();
        string ours = Cost switch
        {
            0 => p switch
            {
                1 => $"Rizian losses were light. {General}, Commander of the Armed Forces, credited \"the men who planned this as much as " +
                     "the men who fought it.\"",
                2 => $"Our losses this month were {(lightest ? "the lightest of the war" : "light")}. \"I will not call it cheap,\" {General} told the Royal Council. " +
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
                2 => $"The losses are {(heaviest ? "the heaviest of the war so far" : "severe again")}, {PlayerDeadMen} men killed, wounded or missing. Several battalions " +
                     $"have been merged with their neighbors because there were not enough men left to call them battalions. \"I have " +
                     $"broken good units this month,\" {General} said. \"I will have to answer for that, and I will.\"",
                _ => $"{(heaviest ? "The last phase was the costliest:" : "The last phase cost")} {PlayerDeadMen} men killed, wounded or missing. \"Nobody who was there will call " +
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
                1 => $" Wehzek losses are estimated at {EnemyDeadMen} men, and the prisoner cages at {RearTown(CitiesLost)} are filling.",
                2 => $" The enemy lost an estimated {EnemyDeadMen} men. A column of prisoners was marched south through the Rizian lines, " +
                     "and the townspeople of the border villages came out to watch it pass in silence.",
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

    /// <summary>
    /// The first Rizian town behind the line still in Rizian hands, for prisoner cages and field hospitals: Dapalle, else Balle,
    /// else Iza (which Wehlen never reaches). <paramref name="lost"/> adds towns lost this phase but not yet flagged.
    /// </summary>
    internal static string RearTown(ICollection<string> lost = null) =>
        new[] { "Dapalle", "Balle" }.FirstOrDefault(t => (lost == null || !lost.Contains(t)) && !Variables.GetBool(WarFlags.Occupied(t))) ?? "Iza";

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
        string at = ZilleCampaign.Str(PhaseResume.VarLandedAt);
        string where = at.Length > 0 ? $"at {at}" : "near its ports";   // older saves do not know the port
        return landed == 1
            ? $"Between the phases Wehlen landed a fresh formation {where}, {how}, and it was waiting for us."
            : $"Between the phases Wehlen landed {landed} fresh formations {where}, {how}, and they were waiting for us.";
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
    private string PlanSentence(WarCouncil.Orders o, bool won, int p)
    {
        // won: the plan carried at least part of the aim; a partial success says where it ran out of strength.
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
        string target = Partial ? Join(AimTaken) : AimName;
        bool plural = (Partial ? AimTaken : AimStart).Count > 1;
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
                      (Partial ? ", and it carried part of the way." : won ? ", and it was enough." : ", and it ran straight into prepared Wehzek positions.")
                    : $"With {Spent(o.Plan)} spent in the earlier fighting, {chosen} gave way to a plain assault" +
                      (won ? ", which the enemy had not expected from us any more." : ", which is what the enemy had been waiting for."));
                break;
            default:
                s.Add(p == 1 ? FirstPlan(o.Approach, chosen, target, won) : p == 2 ? SecondPlan(o.Approach, target, won, plural, Partial) : LastPlan(o.Approach, target, won, plural, Partial));
                break;
        }

        if (Partial && AimMissed.Count > 0)
        {
            s.Add($"It ran out of strength short of {Join(AimMissed)}.");
        }

        s.Add(NavySentence(o, AimId, AimStart, won, p));
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

    // partial: part of the Ports aim taken (target names it); the next sentence says where the push ran out of strength
    private static string SecondPlan(WarCouncil.Approach a, string target, bool won, bool plural, bool partial) => a switch
    {
        WarCouncil.Approach.Feint => won
            ? $"The Wehzeks had learned to expect a feint, so the Staff made the feint the real attack, and {target} fell to the blow they had been told to ignore."
            : "The Wehzek commanders had read the first phase too well, and this time they kept their reserves where they were.",
        WarCouncil.Approach.Landing => won
            ? $"The marines went ashore again, on a beach the Wehzeks had not thought worth mining, and took {target} from the side {(plural ? "they" : "it")} did not face."
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
        _ => partial
            ? $"The assault went in on a broad front and broke through in front of {target}, but nowhere else."
            : won
            ? "The assault went in on a broad front and kept going, and the Wehzek line gave way in three places at once."
            : "The assault went in on a broad front, and the Wehzek line bent everywhere and broke nowhere.",
    };

    private static string LastPlan(WarCouncil.Approach a, string target, bool won, bool plural, bool partial) => a switch
    {
        WarCouncil.Approach.Feint => partial
            ? $"The last feint of the war drew enough of the garrison away to take {target}."
            : won
            ? "The last feint of the war worked as well as the first."
            : "By the last phase the Wehzeks no longer believed anything the army showed them, and the feint drew nobody away.",
        WarCouncil.Approach.Landing => won
            ? $"The marines made their last landing of the war on the beaches below {target}, and walked into {(plural ? "both towns" : "the town")} by the harbor road."
            : $"The last landing went in under the guns of {target}, and the boats that came back were carrying wounded.",
        WarCouncil.Approach.Armor => partial
            ? $"What was left of the tanks led the final push, and it was enough for {target}."
            : won
            ? "What was left of the tanks led the final push, and it was enough."
            : "What was left of the tanks led the final push, and there was not enough left of them.",
        WarCouncil.Approach.Airborne => won
            ? "The paratroopers made their last drop of the war at dawn, and by evening the columns had reached them."
            : "The paratroopers made their last drop of the war, and the columns never reached them.",
        WarCouncil.Approach.Mountain => won
            ? "The mountaineers came down off the ridges for the last time, onto the coast road behind the Wehzek positions."
            : "The mountaineers held the ridges to the end, but the coast road below them stayed Wehzek.",
        _ => partial
            ? $"The final assault was fought by tired men, and it carried {target}."
            : won
            ? "The final assault was fought by tired men, and it carried."
            : "The final assault was fought by tired men against men who were just as tired, and it stalled.",
    };

    /// <summary>
    /// The navy's part. Its orders carry over between phases, so each phase tells it in its own words. Only Zille and Nanta have
    /// coastal batteries (ZilleOperation's coastal artillery tiles): an aim of Isor alone gets the fleet off its quays instead.
    /// </summary>
    private static string NavySentence(WarCouncil.Orders o, Objective aimId, List<string> aimCities, bool won, int p)
    {
        string aim = Join(aimCities.Where(c => c is "Zille" or "Nanta").ToList());
        string port = Join(aimCities);
        switch (o.Navy)
        {
            case "Strait":
                return p switch
                {
                    1 => won
                        ? "At sea, the fleet kept Wehzek reinforcements from crossing the strait while the battle was fought."
                        : "The fleet held the strait, but what the garrison already had in Zille was enough.",
                    2 => o.SubsOut
                        ? "The fleet stayed on the strait, and the submarines with it. Two Wehzek transports were sunk at night within sight of " +
                          $"{(!ZilleCampaign.IsEnemy(ZilleCampaign.OwnersAtStart(), ZilleCampaign.TownTile("Nanta")) ? !ZilleCampaign.IsEnemy(ZilleCampaign.OwnersAtStart(), ZilleCampaign.TownTile("Isor")) ? "the Zille coast" : "Isor" : "Nanta")}."
                        : "The fleet stayed on the strait through the phase, and the convoys that tried to cross turned back.",
                    _ => "For the last phase the fleet still held the strait, and the Wehzek garrison fought without hope of relief from across it.",
                };
            case "Coast":
                return p switch
                {
                    1 => won
                        ? "Offshore, the fleet's guns silenced the Wehzek coastal batteries before the first wave went in."
                        : "The fleet shelled the coast for two days; the batteries it could not see did the rest.",
                    2 => aimId == Objective.Wall
                        ? "The fleet's guns were of little use so far from the sea, and the Staff kept the ships offshore to cover the coast road."
                        : aim.Length == 0
                        ? won
                            ? $"The fleet stood in off {port} and kept the Wehzek transports away from its quays while the army went in."
                            : $"The fleet stood in off {port} and kept the Wehzek transports away from its quays, but the garrison already had what it needed."
                        : won
                            ? $"The fleet came inshore and shelled the batteries at {aim} until their guns stayed silent."
                            : $"The fleet came inshore, but the batteries at {aim} had been moved out of sight, and the ships shelled empty emplacements.",
                    _ => aimId == Objective.Wall
                        ? "The fleet's guns could not reach the hills, and for the last phase the ships covered the coast road instead."
                        : aim.Length == 0
                        ? won
                            ? $"For the last phase the fleet stood in off {port} and kept the Wehzek transports away from its quays."
                            : $"For the last phase the fleet stood in off {port}, but the garrison no longer needed the transports it turned away."
                        : won
                        ? $"The fleet came close inshore for the last phase and shelled the harbor batteries at {aim} until they fell silent."
                        : $"The fleet came close inshore for the last phase, and a destroyer was hit by the batteries at {aim} before it could pull back.",
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

    private string Closing(int p, bool final, bool liberated, bool zilleHeld, ObjectiveInfo info)
    {
        if (liberated)
        {
            string stragglers = RizianHeld.Count > 0
                ? $" The Wehzek units still in {Join(RizianHeld)} have been ordered by their own commanders to lay down their arms as well."
                : string.Empty;
            return p < ZilleCampaign.LastPhase
                ? "Officers of the Wehzek garrison have come through the lines under a white flag to ask how their men may lay down their " +
                  $"arms.{stragglers} \"I had {(p == 1 ? "two more phases" : "a third phase")} planned to the last detail,\" General Robles Azaro " +
                  "said. \"I am very happy to put them in a drawer.\""
                : "Officers of the last Wehzek garrisons have come through the lines under a white flag, and the Staff is arranging their " +
                  $"surrender.{stragglers} \"For the first time since the lease, Zille has no foreign army in it,\" General Robles Azaro said. " +
                  "\"The men are singing, and I have not had the heart to stop them.\"";
        }

        if (final)
        {
            return zilleHeld
                ? "The Staff has advised the Royal Council that the army has no further push in it this year. The men are exhausted, the " +
                  "stocks of shells are spent, and the Alliance's observers are already asking both armies where they stand. \"We hold the " +
                  "city and the harbor,\" General Robles Azaro said. \"The rest will be decided by people who were not there.\""
                : "The Staff has advised the Royal Council that the army cannot mount another offensive, and has urged that the fighting stop " +
                  $"before it is carried {(RizianHeld.Count > 0 ? "any further onto" : "onto")} the Rizian side of the border. \"The army has given " +
                  "everything it had,\" General Robles Azaro said. \"The failure is mine, and I will answer for it.\"";
        }

        // The next aim as it stands now ("Isor" once Nanta is ours); a city lost this phase comes back as the aim.
        bool any = ZilleCampaign.Available().Count > 0;
        ObjectiveInfo nextInfo = any ? ZilleCampaign.Info(ZilleCampaign.NextObjective) : null;
        string next = nextInfo == null ? null : ZilleCampaign.AimName(nextInfo);
        string garrison = GarrisonSentence(p);
        if (next == null)
        {
            return $"{garrison} The Staff awaits the King's orders.";
        }

        bool again = nextInfo.Id == info.Id && !BackLost.Any(nextInfo.Towns.Contains);
        bool winBack = BackLost.Any(nextInfo.Towns.Contains);
        return p switch
        {
            1 => $"{garrison} " + (winBack
                ? $"The second phase must first win back {next}. \"We took {(next.Contains(" and ") ? "them" : "it")} once,\" General Robles Azaro said. \"The men know the way.\""
                : again
                ? $"The Staff asks to go for {next} again in the second phase. \"We know their positions now,\" General Robles Azaro said. " +
                  "\"They know ours. Next month it comes down to who has used the time better.\""
                : $"The second phase, against {next}, can begin as soon as the King gives the order. \"The Wehzeks will use every night we " +
                  "give them,\" General Robles Azaro said. \"Their convoys do not keep office hours.\""),
            _ => $"{garrison} " + (winBack
                ? $"One phase remains, and it must win back {next}. \"The men know it is the last push,\" General Robles Azaro said. " +
                  "\"I have told them it is, and I mean to keep my word.\""
                : again && Partial
                ? $"The final phase will go for {next}, the part of the objective still in Wehzek hands. \"One more try is all the army has " +
                  "in it,\" General Robles Azaro told the Royal Council, \"so it had better be the right one.\""
                : again
                ? $"The final phase will have to go for {next} once more. \"One more try is all the army has in it,\" General Robles Azaro " +
                  "told the Royal Council, \"so it had better be the right one.\""
                : $"One phase remains, against {next}. \"The men know it is the last push,\" General Robles Azaro said. \"I have told " +
                  "them it is, and I mean to keep my word.\""),
        };
    }

    /// <summary>Event flags already told in an earlier report (the flags stay set for the rest of the war).</summary>
    public const string VarNotesTold = "ZilleWar.State_ReportNotesTold";

    /// <summary>The event notes not yet told (or, when an older dispatch is written again, the notes that dispatch told).</summary>
    private static List<string> EventNotes(string retell)
    {
        var notes = new List<string>();
        var told = ZilleCampaign.Str(VarNotesTold).Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        void Note(string flag, string text)
        {
            if (retell != null)
            {
                if (retell.Contains(text))
                {
                    notes.Add(text);
                }

                return;
            }

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
        if (retell == null)
        {
            Variables.Set(VarNotesTold, string.Join(",", told));
        }

        return notes;
    }

    /// <summary>The older dispatch being written again (<see cref="Retell"/>), or null.</summary>
    private string _retell;

    /// <summary>
    /// An older build wrote the k-th phase's dispatch reading a part of the aim as a miss ("Isor and Nanta has held" after Nanta
    /// fell). Write it again from the map that phase started from, which is kept until the next phase is set up, with the same
    /// event notes, and put it back on the phase's report.
    /// </summary>
    public static void Retell(int k)
    {
        (int ck, Objective objective, int phase) = ZilleCampaign.CurrentOperation();
        string old = ZilleCampaign.Str(ZilleCampaign.ReportTextVar(k));
        if (ck != k || old.Length == 0)
        {
            return;
        }

        AfterAction a = Compute(k, ZilleCampaign.OwnersAtStart(), ZilleCampaign.CurrentOwners());
        a._retell = old;
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        string report = a.FullReport(info, Variables.GetBool(ZilleCampaign.ResultVar(k)));
        Variables.Set(ZilleCampaign.ReportTextVar(k), report);
        if (ZilleCampaign.Liberated || ZilleCampaign.Defeated)
        {
            ZilleReports.SetDescription(ZilleReports.WonName, report);
            ZilleReports.SetDescription(ZilleReports.LostName, report);
        }

        ZilleReports.SetDescription($"Operation_Zille_Phase{phase}_Report", report);
        Core.Log.Msg($"Phase {phase}'s dispatch written again for an older save:\n{report}");
    }

    public static string Join(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];
}
