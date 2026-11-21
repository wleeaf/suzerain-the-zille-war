using Il2Cpp;
using SuzerainModdingKit;
using UnityEngine;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace ZilleWar;

/// <summary>
/// Builds one Zille operation from the campaign state: who holds which tile (carried over from the last
/// operation), the chosen objective, and Wehlen's strength tier. Registers it in the game's story data
/// (OperationData + WarFragmentData) and war data (WarOperation + WarOperationData), modelled on vanilla
/// Operation_1 ("Golden Shield").
/// </summary>
internal static class ZilleOperation
{
    public const string TokenName = "Rizia_Event_Zille";
    private const string Prefix = "Operation_Zille_";

    public static string OperationName(int k) => $"{Prefix}{k}";
    public static string FragmentName(int k) => $"WarFragment_Zille_{k}";
    public static string CapturedVar(string town) => $"ZilleWar.War_Objective_Captured_{town.Replace(" ", string.Empty)}";

    /// <summary>The operation currently registered (index and objective).</summary>
    public static int CurrentIndex { get; private set; }
    public static Objective CurrentObjective { get; private set; }

    public static void RegisterVariables()
    {
        foreach (TownSpec t in ZilleData.Towns.Where(t => t.Role == "city"))
        {
            Variables.Register(CapturedVar(t.Name));
        }
    }

    public static bool IsOurFragment(WarFragmentData f) =>
        f?.WarFragmentProperties?.Operation != null && f.WarFragmentProperties.Operation.StartsWith(Prefix);

    /// <summary>Build and register the next operation from the campaign state.</summary>
    public static bool BuildNext(out WarFragmentData fragment)
    {
        fragment = null;
        OperationData vanillaOp = FindStoryOperation("Operation_1");
        WarFragmentData vanillaFrag = FindFragment("WarFragment_1_1");
        WarOperation vanillaWarOp = FindWarOperation("Operation_1");
        if (vanillaOp == null || vanillaFrag == null || vanillaWarOp == null || ZilleData.Tiles == null)
        {
            Core.Log.Error($"Cannot build Zille operation: op={vanillaOp != null} frag={vanillaFrag != null} " +
                           $"warOp={vanillaWarOp != null} tiles={ZilleData.Tiles != null}");
            return false;
        }

        List<Objective> available = ZilleCampaign.Available();
        if (available.Count == 0)
        {
            Core.Log.Warning("Nothing left to take: Zille is liberated. Ctrl+F12 resets the campaign.");
            return false;
        }

        int k = ZilleCampaign.OperationsDone + 1;
        Objective objective = ZilleCampaign.NextObjective;
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        CurrentIndex = k;
        CurrentObjective = objective;
        Variables.Set(ZilleCampaign.VarCurrentOp, $"{k}:{objective}");

        DecisionData eventTemplate = WarEvents.FindVanilla("OP1_Decision_War_Played_FortAlesCaptured");
        foreach (WarEvent ev in CityEvents.All())
        {
            WarEvents.Register(ev, eventTemplate);
        }

        (string won, string lost) = ZilleReports.Register(k, info, vanillaOp.OperationProperties.WinReport, vanillaOp.OperationProperties.LoseReport);

        // --- story layer: OperationData ---
        OperationProperties vp = vanillaOp.OperationProperties;
        var phases = new Il2CppStringList();
        phases.Add(FragmentName(k));
        string targetVar = info.Target != null ? CapturedVar(info.Target) : ZilleCampaign.VarLiberated;
        var op = new OperationData
        {
            Id = $"0x5A111E00000001{k:X2}",
            NameInDatabase = OperationName(k),
            AppBundleProperties = vanillaOp.AppBundleProperties,
            OperationProperties = new OperationProperties
            {
                OperationName = info.Codename,
                OperationStartCondition = vp.OperationStartCondition,
                IsOperationFinishedEarly =
                    $"{targetVar} == true || RiziaDLC.Army_Unit_GrandTotal_Deployed + RiziaDLC.Army_Unit_GrandTotal_Total <= 0",
                ResultVariable = ZilleCampaign.ResultVar(k),
                IsTutorial = false,
                WinReport = won,
                LoseReport = lost,
                ResultConditionalInstruction = string.Empty,
                TotalEnemyDeadVariable = ZilleCampaign.EnemyDeadVar(k),
                TotalPlayerDeadVariable = ZilleCampaign.PlayerDeadVar(k),
                Phases = phases,
            },
        };
        AddByName(EntityDataManager.operationData, op, o => o.NameInDatabase);
        AddByName(EntityDataManager.OperationData, op, o => o.NameInDatabase);

        // --- story layer: the war fragment (one phase per operation) ---
        var frag = new WarFragmentData
        {
            Id = $"0x5A111E00000002{k:X2}",
            NameInDatabase = FragmentName(k),
            AppBundleProperties = vanillaFrag.AppBundleProperties,
            AssignedTokenProperties = new AssignedTokenProperties { AssignedToken = TokenName },
            StoryFragmentProperties = new StoryFragmentProperties
            {
                IsDone = false,
                StoryFragmentCondition = string.Empty,
                OnStoryFragmentBeginInstruction = string.Empty,
                OnStoryFragmentEndInstruction = string.Empty,
            },
            WarFragmentProperties = new WarFragmentProperties
            {
                HubWarFragmentTitle = info.Codename,
                HubWarFragmentSubtitle = info.Aim,
                Operation = OperationName(k),
            },
            TagsProperties = vanillaFrag.TagsProperties,
        };
        AddByName(EntityDataManager.AllWarFragmentsData, frag, f => f.NameInDatabase);
        AddByName(EntityDataManager.warFragmentData, frag, f => f.NameInDatabase);
        AddByName(EntityDataManager.WarFragmentsData, frag, f => f.NameInDatabase);

        // --- war layer ---
        RemoveOurWarOperations();
        WarOperation warOp = BuildWarOperation(k, objective, vanillaWarOp.warOperationData);
        Managers.Instance.operations.Add(warOp);
        fragment = frag;
        Core.Log.Msg($"Built {OperationName(k)} '{info.Codename}' ({objective}): {ZilleCampaign.Describe()}");
        return true;
    }

    private static WarOperation BuildWarOperation(int k, Objective objective, WarOperationData vanilla)
    {
        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        var by = ZilleData.ByName;
        bool Player(string n) => owners.TryGetValue(n, out bool p) && p;
        bool Enemy(string n) => owners.TryGetValue(n, out bool p) && !p;
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        string targetTile = info.Target != null ? ZilleCampaign.TownTile(info.Target) : null;

        var data = ScriptableObject.CreateInstance<WarOperationData>();
        data.name = OperationName(k);
        data.operationNameInDatabase = OperationName(k);
        data.includedTileNames = new Il2CppStringList();
        foreach (TileSpec t in ZilleData.Tiles)
        {
            data.includedTileNames.Add(t.Name);
        }

        UnitTypes ut = UnitTypes.FromVanilla(vanilla);
        Plan plan = Plan.Compute(owners, targetTile);

        var tileGroup = new ConditionalWarTileData { condition = string.Empty, tiles = new Il2CppSystem.Collections.Generic.List<WarTileData>() };
        foreach (TileSpec t in ZilleData.Tiles)
        {
            bool land = t.Type == "Land";
            tileGroup.tiles.Add(new WarTileData
            {
                tileName = t.Name,
                tileType = land ? WarTile.TileType.Land : WarTile.TileType.Sea,
                groundType = t.Mountain ? WarTile.GroundType.Mountain : WarTile.GroundType.Default,
                startingAllegiance = !land ? WarToken.WarAllegiance.EmptySea : Player(t.Name) ? WarToken.WarAllegiance.Player : WarToken.WarAllegiance.Enemy,
                fortLevel = t.Fort && Player(t.Name) ? 2 : plan.Wall.Contains(t.Name) ? 1 : 0,
                isEnemyAirfield = false,
                tileDefenceType = DefenceType(t),
                isAntiAirTile = t.Town == "Zille" && Enemy(t.Name),
                isCoastalArtilleryTile = (t.Town is "Zille" or "Nanta") && Enemy(t.Name),
                isInitialDeploymentTile = plan.Deployment.Contains(t.Name),
                isBorderTile = plan.Front.Contains(t.Name),
                isShoreTile = t.Shore,
                isSupplyLineConnectionBroken = false,
                warTileStatChanges = new Il2CppSystem.Collections.Generic.List<WarTile.WarTileStatChange>(),
            });
        }

        data.tileData = new Il2CppSystem.Collections.Generic.List<ConditionalWarTileData>();
        data.tileData.Add(tileGroup);

        var enemy = new ConditionalWarTokenData { condition = string.Empty, tokens = new Il2CppSystem.Collections.Generic.List<WarTokenData>() };
        var garrison = Garrison.Build(owners, plan, targetTile, ut);
        foreach ((string tile, WarUnitTypeData type) in garrison)
        {
            enemy.tokens.Add(Token(tile, type, WarToken.WarAllegiance.Enemy));
        }

        data.enemyTokens = Wrap(enemy);

        // Cities: whoever holds the tile holds the city.
        var enemyCities = new ConditionalWarTokenData { condition = string.Empty, tokens = new Il2CppSystem.Collections.Generic.List<WarTokenData>() };
        var playerCities = new ConditionalWarTokenData { condition = string.Empty, tokens = new Il2CppSystem.Collections.Generic.List<WarTokenData>() };
        foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null))
        {
            if (Enemy(town.Tile))
            {
                enemyCities.tokens.Add(Token(town.Tile, UnitTypes.City(ut.EnemyCityTemplate, town.Name, CapturedVar(town.Name)), WarToken.WarAllegiance.Enemy));
            }
            else if (ut.PlayerCityTemplate != null)
            {
                playerCities.tokens.Add(Token(town.Tile, UnitTypes.City(ut.PlayerCityTemplate, town.Name, null), WarToken.WarAllegiance.Player));
            }
        }

        data.enemyCityTokens = Wrap(enemyCities);
        data.playerCityTokens = Wrap(playerCities);

        data.supplyLines = new Il2CppSystem.Collections.Generic.List<WarSupplyLine>();
        foreach (List<string> line in plan.SupplyLines)
        {
            var sl = new WarSupplyLine { tileNames = new Il2CppStringList(), tiles = new Il2CppSystem.Collections.Generic.List<WarTile>() };
            foreach (string n in line)
            {
                sl.tileNames.Add(n);
            }

            data.supplyLines.Add(sl);
        }

        data.tileAllegianceChangeEvents = new Il2CppSystem.Collections.Generic.List<ConditionalTileAllegianceChangeEvent>();
        data.tileAllegianceChangeEvents.Add(CityChangeEvents());
        data.warFlowEvents = new Il2CppSystem.Collections.Generic.List<ConditionalWarFlowEvent>();
        data.onStartBattleEvents = new Il2CppSystem.Collections.Generic.List<ConditionalWarStartBattleEvent>();
        data.tileUnitActionEvents = new Il2CppSystem.Collections.Generic.List<ConditionalTileUnitActionEvent>();
        data.tileDataOverrides = new Il2CppSystem.Collections.Generic.List<ConditionalWarTileData>();
        data.enemyTokenOverrides = new Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData>();
        data.playerCityTokenOverrides = new Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData>();
        data.enemyCityTokenOverrides = new Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData>();

        var go = new GameObject(OperationName(k));
        UnityEngine.Object.DontDestroyOnLoad(go);
        WarOperation warOp = go.AddComponent<WarOperation>();
        warOp.warOperationData = data;
        Core.Log.Msg($"  target={info.Target ?? "(clear)"} tile={targetTile}; garrison {garrison.Count} " +
                     $"({string.Join(", ", garrison.GroupBy(g => g.type.unitName).Select(g => $"{g.Count()} {g.Key}"))}), tier {ZilleCampaign.Tier}; " +
                     $"front [{string.Join(",", plan.Front)}], deploy {plan.Deployment.Count} " +
                     $"({plan.Deployment.Count(n => by[n].Type == "Sea")} sea), supply lines {plan.SupplyLines.Count}, " +
                     $"cities enemy {enemyCities.tokens.Count} / player {playerCities.tokens.Count}");
        foreach (List<string> l in plan.SupplyLines)
        {
            Core.Log.Msg($"  supply line: {string.Join(" -> ", l)}");
        }

        return warOp;
    }

    private static void RemoveOurWarOperations()
    {
        var ops = Managers.Instance.operations;
        for (int i = ops.Count - 1; i >= 0; i--)
        {
            WarOperation o = ops[i];
            if (o?.warOperationData?.operationNameInDatabase?.StartsWith(Prefix) == true)
            {
                ops.RemoveAt(i);
                UnityEngine.Object.Destroy(o.gameObject);
            }
        }
    }

    /// <summary>One capture and one loss event per city tile, on both sides.</summary>
    private static ConditionalTileAllegianceChangeEvent CityChangeEvents()
    {
        var group = new ConditionalTileAllegianceChangeEvent
        {
            condition = string.Empty,
            events = new Il2CppSystem.Collections.Generic.List<TileAllegianceChangeEvent>(),
        };
        foreach (TownSpec town in ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null))
        {
            group.events.Add(new TileAllegianceChangeEvent
            {
                tileName = town.Tile,
                onAllegianceChangedTo = WarToken.WarAllegiance.Player,
                storyFragmentNameInDatabase = CityEvents.CapturedName(town.Name),
                isOneTimeEvent = true,
            });
            group.events.Add(new TileAllegianceChangeEvent
            {
                tileName = town.Tile,
                onAllegianceChangedTo = WarToken.WarAllegiance.Enemy,
                storyFragmentNameInDatabase = CityEvents.LostName(town.Name),
                isOneTimeEvent = true,
            });
        }

        return group;
    }

    private static WarTile.TileDefenceType DefenceType(TileSpec t)
    {
        if (t.Fort)
        {
            return WarTile.TileDefenceType.FortifiedDefencePoint;
        }

        if (t.Defence == "City")
        {
            return WarTile.TileDefenceType.City;
        }

        if (t.Defence == "KeyLocation")
        {
            return WarTile.TileDefenceType.KeyLocation;
        }

        bool nextToCity = t.Neighbors.Any(n => ZilleData.ByName.TryGetValue(n, out TileSpec nb) && nb.Defence == "City");
        return nextToCity && t.Type == "Land" ? WarTile.TileDefenceType.CitySurroundings : WarTile.TileDefenceType.None;
    }

    private static WarTokenData Token(string tile, WarUnitTypeData type, WarToken.WarAllegiance allegiance) => new()
    {
        startingTileName = tile,
        unitType = type,
        allegiance = allegiance,
        guid = string.Empty,
        isDeployedWithInstruction = false,
    };

    private static Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData> Wrap(ConditionalWarTokenData d)
    {
        var l = new Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData>();
        l.Add(d);
        return l;
    }

    internal static void AddByName<T>(Il2CppSystem.Collections.Generic.List<T> list, T item, Func<T, string> name)
    {
        if (list == null)
        {
            return;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (name(list[i]) == name(item))
            {
                list.RemoveAt(i);
            }
        }

        list.Add(item);
    }

    public static OperationData FindStoryOperation(string name)
    {
        foreach (OperationData o in EntityDataManager.OperationData)
        {
            if (o.NameInDatabase == name)
            {
                return o;
            }
        }

        return null;
    }

    public static WarFragmentData FindFragment(string name)
    {
        foreach (WarFragmentData f in EntityDataManager.AllWarFragmentsData)
        {
            if (f.NameInDatabase == name)
            {
                return f;
            }
        }

        return null;
    }

    public static WarOperation FindWarOperation(string name)
    {
        foreach (WarOperation o in Managers.Instance.operations)
        {
            if (o?.warOperationData?.operationNameInDatabase == name)
            {
                return o;
            }
        }

        return null;
    }
}

/// <summary>Front, deployment, wall and supply lines for an operation, derived from current ownership.</summary>
internal sealed class Plan
{
    public HashSet<string> Front = new();        // player tiles that must be held at deployment (vanilla: 5-8)
    public HashSet<string> Wall = new();         // Wehlen's original border wall tiles, still Wehlen-held
    public HashSet<string> Deployment = new();   // all player land + sea next to player land
    public List<List<string>> SupplyLines = new();

    private const int MaxFront = 5;

    public static Plan Compute(Dictionary<string, bool> owners, string targetTile)
    {
        var p = new Plan();
        var by = ZilleData.ByName;
        bool Player(string n) => owners.TryGetValue(n, out bool v) && v;
        bool Enemy(string n) => owners.TryGetValue(n, out bool v) && !v;
        int EnemyNeighbours(TileSpec t) => t.Neighbors.Count(Enemy);

        foreach (TileSpec t in ZilleData.Tiles.Where(t => t.Region == "zille" && t.Type == "Land" && Enemy(t.Name)))
        {
            if (t.Neighbors.Any(n => by.TryGetValue(n, out TileSpec x) && x.Region == "rizia" && x.Type == "Land"))
            {
                p.Wall.Add(t.Name);
            }
        }

        // Supply lines: from the rear through the nearest player-held city to each remaining Wehlen city,
        // the objective first.
        var enemyCities = ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null && Enemy(t.Tile)).Select(t => t.Tile)
            .OrderByDescending(t => t == targetTile).Take(3).ToList();
        var playerCities = ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null && Player(t.Tile)).Select(t => t.Tile).ToList();
        foreach (string target in enemyCities)
        {
            string from = playerCities.OrderBy(c => Path(c, target, owners, null)?.Count ?? 999).FirstOrDefault();
            if (from == null)
            {
                continue;
            }

            string rear = ZilleData.Tiles.Where(t => t.Type == "Land" && Player(t.Name) && t.Name != from && Dist(t.Name, target) > Dist(from, target))
                .OrderBy(t => Dist(t.Name, from)).FirstOrDefault()?.Name ?? from;
            List<string> a = Path(rear, from, owners, onlyPlayer: true) ?? new List<string> { from };
            List<string> b = Path(from, target, owners, null);
            if (b == null)
            {
                continue;
            }

            List<string> line = a.Concat(b.Skip(1)).Distinct().ToList();
            p.SupplyLines.Add(line);
            for (int i = 0; i + 1 < line.Count; i++)
            {
                if (Player(line[i]) && Enemy(line[i + 1]))
                {
                    p.Front.Add(line[i]);
                    break;
                }
            }
        }

        foreach (TileSpec t in ZilleData.Tiles.Where(t => t.Type == "Land" && Player(t.Name) && EnemyNeighbours(t) > 0)
                     .OrderByDescending(EnemyNeighbours).ThenBy(t => targetTile == null ? 0 : Dist(t.Name, targetTile)))
        {
            if (p.Front.Count >= MaxFront)
            {
                break;
            }

            p.Front.Add(t.Name);
        }

        foreach (TileSpec t in ZilleData.Tiles)
        {
            if (t.Type == "Land" && Player(t.Name))
            {
                p.Deployment.Add(t.Name);
            }
            else if (t.Type == "Sea" && t.Neighbors.Any(Player))
            {
                p.Deployment.Add(t.Name);
            }
        }

        return p;
    }

    public static float Dist(string a, string b)
    {
        float[] x = ZilleData.ByName[a].CentroidUv, y = ZilleData.ByName[b].CentroidUv;
        return MathF.Sqrt((x[0] - y[0]) * (x[0] - y[0]) + (x[1] - y[1]) * (x[1] - y[1]));
    }

    /// <summary>BFS over land tiles; onlyPlayer restricts to player-held tiles.</summary>
    public static List<string> Path(string from, string to, Dictionary<string, bool> owners, bool? onlyPlayer)
    {
        var by = ZilleData.ByName;
        var prev = new Dictionary<string, string> { [from] = null };
        var q = new Queue<string>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            string cur = q.Dequeue();
            if (cur == to)
            {
                break;
            }

            foreach (string nb in by[cur].Neighbors)
            {
                if (prev.ContainsKey(nb) || !by.TryGetValue(nb, out TileSpec s) || s.Type != "Land" ||
                    (onlyPlayer == true && !(owners.TryGetValue(nb, out bool pl) && pl)))
                {
                    continue;
                }

                prev[nb] = cur;
                q.Enqueue(nb);
            }
        }

        if (!prev.ContainsKey(to))
        {
            return null;
        }

        var path = new List<string>();
        for (string c = to; c != null; c = prev[c])
        {
            path.Add(c);
        }

        path.Reverse();
        return path;
    }
}

/// <summary>
/// Wehlen's garrison, sized and composed by the supply-war tier and by how much of Zille Wehlen still
/// holds; concentrated toward the operation's objective, spread over the rest.
/// </summary>
internal static class Garrison
{
    private const int FullStrength = 17;      // Default tier, all of Zille held (playtested)

    public static List<(string tile, WarUnitTypeData type)> Build(Dictionary<string, bool> owners, Plan plan, string targetTile, UnitTypes ut)
    {
        var by = ZilleData.ByName;
        bool Enemy(string n) => owners.TryGetValue(n, out bool v) && !v;
        bool Player(string n) => owners.TryGetValue(n, out bool v) && v;

        (float size, float militiaShare, int tanks) = ZilleCampaign.Tier switch
        {
            "Strong" => (1.15f, 0.25f, 4),
            "Default" => (1.0f, 0.4f, 3),
            "Strained" => (0.8f, 0.6f, 2),
            _ => (0.6f, 0.75f, 1),
        };
        int zilleTotal = ZilleData.Tiles.Count(t => t.Region == "zille" && t.Type == "Land");
        int enemyLand = ZilleData.Tiles.Count(t => t.Type == "Land" && Enemy(t.Name));
        var cityTiles = ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null && Enemy(t.Tile)).Select(t => t.Tile).ToHashSet();
        var free = ZilleData.Tiles.Where(t => t.Type == "Land" && Enemy(t.Name) && !cityTiles.Contains(t.Name)).ToList();
        int total = Math.Min(free.Count, (int)Math.Round(FullStrength * size * Math.Min(1f, enemyLand / (float)zilleTotal)));
        tanks = Math.Min(tanks, Math.Max(0, total / 4));

        var result = new List<(string, WarUnitTypeData)>();
        var used = new HashSet<string>(cityTiles);
        var placed = new List<string>(cityTiles);

        void Place(string tile, WarUnitTypeData type)
        {
            used.Add(tile);
            placed.Add(tile);
            result.Add((tile, type));
        }

        // 1. Contact line: Wehlen tiles touching Rizian-held land, infantry first (the wall, then any front).
        var contact = free.Where(t => t.Neighbors.Any(Player))
            .OrderByDescending(t => plan.Wall.Contains(t.Name))
            .ThenBy(t => targetTile == null ? 0 : Plan.Dist(t.Name, targetTile)).ToList();
        int contactCap = (int)Math.Ceiling(total * 0.45);
        foreach (TileSpec t in contact.Take(contactCap))
        {
            Place(t.Name, ut.Infantry);
        }

        // 2. Tanks: reserves behind the line, next to the objective first, then other cities.
        var tankAnchors = new List<string>();
        if (targetTile != null && Enemy(targetTile))
        {
            tankAnchors.Add(targetTile);
        }

        tankAnchors.AddRange(cityTiles.Where(c => c != targetTile));
        for (int i = 0; i < tanks && tankAnchors.Count > 0; i++)
        {
            string anchor = tankAnchors[i % tankAnchors.Count];
            TileSpec spot = free.Where(t => !used.Contains(t.Name) && !t.Neighbors.Any(Player))
                .OrderBy(t => Plan.Dist(t.Name, anchor)).FirstOrDefault();
            if (spot != null)
            {
                Place(spot.Name, ut.Tank);
            }
        }

        // 3. The rest: half concentrated on the objective, half spread (farthest-point) over Zille.
        int remaining = total - result.Count;
        int nearTarget = targetTile == null ? 0 : remaining / 2;
        int militia = (int)Math.Round(remaining * militiaShare);
        int placedRest = 0;
        WarUnitTypeData NextType() => placedRest++ < remaining - militia || ut.Militia == null ? ut.Infantry : ut.Militia;

        foreach (TileSpec t in free.Where(t => !used.Contains(t.Name)).OrderBy(t => Plan.Dist(t.Name, targetTile ?? t.Name)).Take(nearTarget).ToList())
        {
            Place(t.Name, NextType());
        }

        while (result.Count < total)
        {
            TileSpec best = free.Where(t => !used.Contains(t.Name))
                .OrderByDescending(t => placed.Count == 0 ? 0 : placed.Min(p => Plan.Dist(t.Name, p)) * (t.Town != null ? 1.4f : 1f))
                .FirstOrDefault();
            if (best == null)
            {
                break;
            }

            Place(best.Name, NextType());
        }

        return result;
    }
}
