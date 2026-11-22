using Il2Cpp;
using SuzerainModdingKit;
using UnityEngine;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace ZilleWar;

/// <summary>
/// Builds one Zille operation from the campaign state: who holds which tile (carried over from the last
/// operation), the chosen objective, Wehlen's strength tier, and the one war plan the King set at the first War Council
/// (every operation continues the same campaign; the plan is adapted per objective, never re-picked). Registers it in the game's story data
/// (OperationData + WarFragmentData) and war data (WarOperation + WarOperationData), modelled on vanilla
/// Operation_1 ("Golden Shield").
/// </summary>
internal static class ZilleOperation
{
    public const string TokenName = "Rizia_Event_Zille";

    /// <summary>The operation and its three phases (one war fragment each), as vanilla's Golden Shield.</summary>
    public const string MainOperation = "Operation_Zille";
    public static string FragmentName(int phase) => $"WarFragment_Zille_{phase}";
    public static string CapturedVar(string town) => $"ZilleWar.War_Objective_Captured_{town.Replace(" ", string.Empty)}";

    /// <summary>The phase currently registered and its aim.</summary>
    public static int CurrentPhase { get; private set; }
    public static Objective CurrentObjective { get; private set; }

    /// <summary>
    /// The airborne drop zone of the phase last built when it is set up fresh (null otherwise): the map starts it in Rizian
    /// hands although the saved front does not, so the ownership check must leave it alone.
    /// </summary>
    public static string FreshDropZone { get; private set; }

    public static void RegisterVariables()
    {
        foreach (TownSpec t in (ZilleData.Towns ?? new List<TownSpec>()).Where(t => t.Role == "city"))
        {
            Variables.Register(CapturedVar(t.Name));
            Vanilla.Expose(CapturedVar(t.Name));      // read by IsOperationFinishedEarly (articy:expresso)
        }

        Variables.Register(ZilleCampaign.VarEnemyDeadTotal);
        Variables.Register(ZilleCampaign.VarPlayerDeadTotal);
        Vanilla.Expose(ZilleCampaign.VarEnemyDeadTotal, isBool: false);
        Vanilla.Expose(ZilleCampaign.VarPlayerDeadTotal, isBool: false);
    }

    public static bool IsOurFragmentName(string name) => name != null && name.StartsWith("WarFragment_Zille_");

    public static bool IsOurFragment(WarFragmentData f) =>
        f?.WarFragmentProperties?.Operation != null && f.WarFragmentProperties.Operation.StartsWith(MainOperation, StringComparison.Ordinal);

    /// <summary>
    /// Build and register this turn's phase from the campaign state. Phase I sets up the map fresh (deployment); Phases II and
    /// III resume it from the snapshot the game took when the previous phase ended, with every unit where it stood. If that
    /// snapshot is missing (an old save, or the game wiped it), the phase is set up fresh from the saved front instead.
    /// </summary>
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
            Core.Log.Warning("Nothing left to take: Zille is liberated.");
            return false;
        }

        int turn = SuzerainModdingKit.GameState.CurrentTurnNum ?? 4;
        int phase = ZilleCampaign.PhaseForTurn(turn);
        int k = ZilleCampaign.OperationsDone + 1;
        Objective objective = ZilleCampaign.NextObjective;
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        bool resume = phase >= 2 && WarSaveSlot.HoldsZilleSnapshot();
        string opName = phase == 1 || resume ? MainOperation : $"{MainOperation}_R{phase}";
        if (phase >= 2 && !resume)
        {
            Core.Log.Warning($"Phase {phase}: no map snapshot from the previous phase in the save; this phase is set up fresh from the saved front.");
        }

        CurrentPhase = phase;
        CurrentObjective = objective;
        Variables.Set(ZilleCampaign.VarCurrentOp, $"{k}:{objective}:{phase}");
        Variables.Set(ZilleCampaign.VarPhase, phase);
        ZilleCampaign.MarkOperationStart();

        DecisionData eventTemplate = WarEvents.FindVanilla("OP1_Decision_War_Played_FortAlesCaptured");
        foreach (WarEvent ev in CityEvents.All())
        {
            WarEvents.Register(ev, eventTemplate);
        }

        (string won, string lost) = ZilleReports.Register(vanillaOp.OperationProperties.WinReport, vanillaOp.OperationProperties.LoseReport);
        if (opName != MainOperation && phase < ZilleCampaign.LastPhase)
        {
            won = lost = ZilleReports.RegisterPhase(phase, vanillaOp.OperationProperties.WinReport);
        }

        // --- story layer: OperationData, its phases in order (the game resumes a phase from the save when it is not the first)
        var phases = new Il2CppStringList();
        if (opName == MainOperation)
        {
            for (int p = 1; p <= ZilleCampaign.LastPhase; p++)
            {
                phases.Add(FragmentName(p));
            }
        }
        else
        {
            phases.Add(FragmentName(phase));
        }

        string allTaken = string.Join(" && ", ZilleCampaign.Sequence.SelectMany(o => ZilleCampaign.Info(o).Towns).Select(t => $"{CapturedVar(t)} == true"));
        var op = new OperationData
        {
            Id = opName == MainOperation ? "0x5A111E0000000100" : $"0x5A111E00000001{0x10 + phase:X2}",
            NameInDatabase = opName,
            AppBundleProperties = vanillaOp.AppBundleProperties,
            OperationProperties = new OperationProperties
            {
                OperationName = ZilleCampaign.OperationTitle,
                // Vanilla's Golden Shield needs 8 land units; when the condition fails the engine silently finishes the
                // operation without our finish hook and the campaign would wait for it forever.
                OperationStartCondition = string.Empty,
                IsOperationFinishedEarly =
                    $"({allTaken}) || RiziaDLC.Army_Unit_GrandTotal_Deployed + RiziaDLC.Army_Unit_GrandTotal_Total <= 0",
                ResultVariable = ZilleCampaign.ResultVar(0),
                IsTutorial = false,
                WinReport = won,
                LoseReport = lost,
                ResultConditionalInstruction = string.Empty,
                TotalEnemyDeadVariable = ZilleCampaign.VarEnemyDeadTotal,
                TotalPlayerDeadVariable = ZilleCampaign.VarPlayerDeadTotal,
                Phases = phases,
            },
        };
        AddByName(EntityDataManager.operationData, op, o => o.NameInDatabase);
        AddByName(EntityDataManager.OperationData, op, o => o.NameInDatabase);

        // --- story layer: this phase's war fragment
        var frag = new WarFragmentData
        {
            Id = $"0x5A111E00000002{phase:X2}",
            NameInDatabase = FragmentName(phase),
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
                HubWarFragmentTitle = ZilleCampaign.PhaseTitle(phase),
                HubWarFragmentSubtitle = $"Objective: {info.Target}",
                Operation = opName,
            },
            TagsProperties = vanillaFrag.TagsProperties,
        };
        AddByName(EntityDataManager.AllWarFragmentsData, frag, f => f.NameInDatabase);
        AddByName(EntityDataManager.warFragmentData, frag, f => f.NameInDatabase);
        AddByName(EntityDataManager.WarFragmentsData, frag, f => f.NameInDatabase);

        // --- war layer
        RemoveOurWarOperations();
        WarOperation warOp = BuildWarOperation(opName, objective, vanillaWarOp.warOperationData, out string dropZone);
        // A resumed phase takes its tiles from the snapshot, so only a fresh setup starts with the paratroopers' ground.
        FreshDropZone = resume ? null : dropZone;
        Managers.Instance.operations.Add(warOp);
        fragment = frag;
        Core.Log.Msg($"Built {ZilleCampaign.PhaseTitle(phase)} ({opName}, {(resume ? "resumes the map" : "fresh setup")}), aim {info.Target}: {ZilleCampaign.Describe()}");
        return true;
    }

    private static WarOperation BuildWarOperation(string opName, Objective objective, WarOperationData vanilla, out string dropZone)
    {
        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        var by = ZilleData.ByName;
        bool Player(string n) => owners.TryGetValue(n, out bool p) && p;
        bool Enemy(string n) => owners.TryGetValue(n, out bool p) && !p;
        ObjectiveInfo info = ZilleCampaign.Info(objective);
        string targetTile = ZilleCampaign.FocusTile(info, owners);

        // The War Council's orders shape the map: the war plan chosen at the first council, carried onto this objective in its
        // nearest sensible form (WarCouncil.Form), and the fleet's standing orders.
        WarCouncil.Orders orders = WarCouncil.OrdersFor(objective);

        // Airborne: the paratroopers hold a patch of ground behind the Wehzek line when the operation opens.
        dropZone = orders.Approach == WarCouncil.Approach.Airborne ? Plan.DropZone(owners, targetTile) : null;
        if (dropZone != null)
        {
            owners[dropZone] = true;
        }

        var data = ScriptableObject.CreateInstance<WarOperationData>();
        data.name = opName;
        data.operationNameInDatabase = opName;
        data.includedTileNames = new Il2CppStringList();
        foreach (TileSpec t in ZilleData.Tiles)
        {
            data.includedTileNames.Add(t.Name);
        }

        UnitTypes ut = UnitTypes.FromVanilla(vanilla);
        Plan plan = Plan.Compute(owners, targetTile);
        if (orders.Approach == WarCouncil.Approach.Landing)
        {
            foreach (string sea in Plan.LandingWaters(targetTile, owners))
            {
                plan.Deployment.Add(sea);
            }
        }

        bool Barraged(string n) => orders.Bombard && (plan.Wall.Contains(n) || n == targetTile);

        // Armor: the wall is broken where the main road crosses it. Mountain: the mountaineers have the ridges, so the
        // wall's hill bunkers are taken before the battle. Coast: the fleet has silenced the coastal batteries.
        var breached = new HashSet<string>();
        if (orders.Approach == WarCouncil.Approach.Armor && plan.SupplyLines.Count > 0)
        {
            breached.UnionWith(plan.SupplyLines[0].Where(plan.Wall.Contains));
        }

        if (orders.Approach == WarCouncil.Approach.Mountain)
        {
            breached.UnionWith(plan.Wall.Where(n => by[n].Mountain));
        }

        bool batteriesSilenced = orders.Navy == "Coast";
        int enemyShips = Math.Max(0, ZilleCampaign.EnemyShips - (batteriesSilenced ? 1 : 0));

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
                fortLevel = t.Fort && Player(t.Name) ? 2 : plan.Wall.Contains(t.Name) && !Barraged(t.Name) && !breached.Contains(t.Name) ? 1 : 0,
                isEnemyAirfield = false,
                tileDefenceType = DefenceType(t),
                isAntiAirTile = t.Town == "Zille" && Enemy(t.Name) && !Barraged(t.Name),
                isCoastalArtilleryTile = (t.Town is "Zille" or "Nanta") && Enemy(t.Name) && !Barraged(t.Name) && !batteriesSilenced,
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
        var garrison = Garrison.Build(owners, plan, targetTile, ut, orders);
        foreach ((string tile, WarUnitTypeData type) in garrison)
        {
            enemy.tokens.Add(Token(tile, type, WarToken.WarAllegiance.Enemy));
        }

        foreach (string sea in Garrison.Fleet(owners, plan, enemyShips))
        {
            if (ut.Ship != null)
            {
                enemy.tokens.Add(Token(sea, ut.Ship, WarToken.WarAllegiance.Enemy));
            }
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

        var go = new GameObject(opName);
        UnityEngine.Object.DontDestroyOnLoad(go);
        WarOperation warOp = go.AddComponent<WarOperation>();
        warOp.warOperationData = data;
        Core.Log.Msg($"  orders: {orders}; enemy ships {enemyShips}; drop zone {dropZone ?? "-"}; breached [{string.Join(",", breached)}]");
        Core.Log.Msg($"  target={info.Target} tile={targetTile}; garrison {garrison.Count} " +
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
            if (o?.warOperationData?.operationNameInDatabase?.StartsWith(MainOperation, StringComparison.Ordinal) == true)
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

    /// <summary>
    /// Where the paratroopers come down: a Wehzek-held land tile in Zille, not a town with a garrison or the objective itself
    /// (its anti-aircraft guns), out of contact with our lines, as close to the objective as possible; inland if it can be.
    /// </summary>
    public static string DropZone(Dictionary<string, bool> owners, string targetTile)
    {
        if (targetTile == null || !ZilleData.ByName.ContainsKey(targetTile))
        {
            return null;
        }

        bool Player(string n) => owners.TryGetValue(n, out bool v) && v;
        bool Enemy(string n) => owners.TryGetValue(n, out bool v) && !v;
        var cityTiles = ZilleData.Towns.Where(t => t.Role == "city" && t.Tile != null).Select(t => t.Tile).ToHashSet();
        return ZilleData.Tiles
            .Where(t => t.Type == "Land" && t.Region == "zille" && Enemy(t.Name) && t.Name != targetTile && !cityTiles.Contains(t.Name) &&
                        !t.Neighbors.Any(Player))
            .OrderBy(t => t.Shore ? 1 : 0)
            .ThenBy(t => Dist(t.Name, targetTile))
            .FirstOrDefault()?.Name;
    }

    /// <summary>Sea tiles from which a landing can reach the objective: next to it, or next to the shore tiles around it.</summary>
    public static List<string> LandingWaters(string targetTile, Dictionary<string, bool> owners)
    {
        var by = ZilleData.ByName;
        if (targetTile == null || !by.TryGetValue(targetTile, out TileSpec target))
        {
            return new List<string>();
        }

        var shore = new List<TileSpec> { target };
        shore.AddRange(target.Neighbors.Where(n => by.TryGetValue(n, out TileSpec x) && x.Type == "Land" && x.Shore).Select(n => by[n]));
        return shore.SelectMany(t => t.Neighbors)
            .Where(n => by.TryGetValue(n, out TileSpec x) && x.Type == "Sea")
            .Distinct()
            .OrderBy(n => Dist(n, targetTile))
            .Take(3)
            .ToList();
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

    /// <summary>Sea tiles for Wehlen's ships: off the Wehzek-held coast, away from our own waters, spread out.</summary>
    public static List<string> Fleet(Dictionary<string, bool> owners, Plan plan, int count)
    {
        bool Enemy(string n) => owners.TryGetValue(n, out bool v) && !v;
        var candidates = ZilleData.Tiles.Where(t => t.Type == "Sea" && !plan.Deployment.Contains(t.Name) && t.Neighbors.Any(Enemy)).ToList();
        if (candidates.Count < count)
        {
            candidates.AddRange(ZilleData.Tiles.Where(t => t.Type == "Sea" && !plan.Deployment.Contains(t.Name) && !candidates.Contains(t)));
        }

        var chosen = new List<string>();
        while (chosen.Count < count && candidates.Count > 0)
        {
            TileSpec best = candidates.Where(t => !chosen.Contains(t.Name))
                .OrderByDescending(t => chosen.Count == 0 ? -t.Neighbors.Count(Enemy) : chosen.Min(c => Plan.Dist(t.Name, c)))
                .FirstOrDefault();
            if (best == null)
            {
                break;
            }

            chosen.Add(best.Name);
        }

        return chosen;
    }

    /// <summary>
    /// The council's orders: a barrage kills part of the forward garrison; a direct assault catches one tank battalion
    /// before it is up; a feint costs time (one more unit across the strait) but pulls the reserves and the armor away
    /// from the objective; a landing makes Wehlen man the border and leaves the coast behind the objective thinner; an
    /// airborne drop pulls men off the objective to ring the drop zone; mountaineers leave the hill positions unmanned.
    /// Whatever nobody stops in the strait arrives before the battle (<see cref="WarCouncil.Orders.Reinforcements"/>).
    /// </summary>
    public static List<(string tile, WarUnitTypeData type)> Build(Dictionary<string, bool> owners, Plan plan, string targetTile, UnitTypes ut,
        WarCouncil.Orders orders = null)
    {
        WarCouncil.Approach approach = orders?.Approach ?? WarCouncil.Approach.Direct;
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
        int total = (int)Math.Round(FullStrength * size * Math.Min(1f, enemyLand / (float)zilleTotal));
        total += approach == WarCouncil.Approach.Feint ? 1 : 0;
        total += orders?.Reinforcements ?? 0;
        total -= orders?.Bombard == true ? 2 : 0;
        total = Math.Clamp(total, 0, free.Count);
        tanks -= approach == WarCouncil.Approach.Direct ? 1 : 0;
        tanks = Math.Clamp(tanks, 0, Math.Max(0, total / 4));

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
        bool feint = approach == WarCouncil.Approach.Feint;
        bool mountaineers = approach == WarCouncil.Approach.Mountain;
        var contact = free.Where(t => t.Neighbors.Any(Player))
            .OrderBy(t => mountaineers && t.Mountain ? 1 : 0)
            .ThenByDescending(t => plan.Wall.Contains(t.Name))
            .ThenBy(t => targetTile == null ? 0 : (feint ? -1 : 1) * Plan.Dist(t.Name, targetTile)).ToList();
        int contactCap = (int)Math.Ceiling(total * (approach == WarCouncil.Approach.Landing ? 0.55 : 0.45));
        foreach (TileSpec t in contact.Take(contactCap))
        {
            Place(t.Name, ut.Infantry);
        }

        // 2. Tanks: reserves behind the line, next to the objective first, then other cities.
        var tankAnchors = new List<string>(cityTiles.Where(c => c != targetTile));
        if (targetTile != null && Enemy(targetTile))
        {
            if (feint && tankAnchors.Count > 0)
            {
                tankAnchors.Add(targetTile);        // the feint draws the armor to the other cities first
            }
            else
            {
                tankAnchors.Insert(0, targetTile);
            }
        }

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
        int nearTarget = targetTile == null ? 0 : approach switch
        {
            WarCouncil.Approach.Feint => remaining / 5,
            WarCouncil.Approach.Landing or WarCouncil.Approach.Airborne => remaining / 3,
            _ => remaining / 2,
        };
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
