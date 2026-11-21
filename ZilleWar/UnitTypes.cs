using Il2Cpp;
using UnityEngine;

namespace ZilleWar;

/// <summary>Unit types picked from a vanilla operation.</summary>
internal sealed class UnitTypes
{
    public WarUnitTypeData EnemyBasic;
    public WarUnitTypeData EnemyStrong;
    public WarUnitTypeData Infantry;
    public WarUnitTypeData Tank;
    public WarUnitTypeData Militia;
    public WarUnitTypeData EnemyCityTemplate;
    public WarUnitTypeData PlayerCityTemplate;

    private static readonly Dictionary<string, WarUnitTypeData> _cities = new();

    public static UnitTypes FromVanilla(WarOperationData v)
    {
        var r = new UnitTypes();
        var enemyTypes = new List<WarUnitTypeData>();
        foreach (ConditionalWarTokenData g in v.enemyTokens)
        {
            foreach (WarTokenData t in g.tokens)
            {
                if (t.unitType != null && !enemyTypes.Any(e => e.Pointer == t.unitType.Pointer))
                {
                    enemyTypes.Add(t.unitType);
                }
            }
        }

        foreach (WarUnitTypeData e in enemyTypes)
        {
            Core.Log.Msg($"  vanilla enemy unit type: '{e.unitName}' type={e.unitType} atk={e.attack} def={e.defence} org={e.organization}");
        }

        WarConfiguration cfg = Managers.Instance?.WarFlowManager?.warConfiguration;
        var pool = new List<WarUnitTypeData>(enemyTypes);
        foreach (var list in new[] { cfg?.allWarUnitTypeDatas, cfg?.resistanceUnitTypeDatas, cfg?.reinforcementsUnitTypeDatas })
        {
            if (list == null)
            {
                continue;
            }

            foreach (WarUnitTypeData u in list)
            {
                if (u != null && !pool.Any(e => e.Pointer == u.Pointer))
                {
                    pool.Add(u);
                }
            }
        }

        Core.Log.Msg($"  unit types available: {string.Join(", ", pool.Select(u => $"{u.name}/'{u.unitName}'/{u.unitType}/{u.unitAlignment}"))}");
        r.Militia = pool.FirstOrDefault(e => e.unitName == "Militia") ?? pool.FirstOrDefault(e => e.name.Contains("Militia"));
        r.Infantry = enemyTypes.FirstOrDefault(e => e.unitName == "Infantry") ?? enemyTypes.OrderBy(e => e.attack + e.defence).First();
        r.Tank = enemyTypes.FirstOrDefault(e => e.unitName == "Tank") ?? enemyTypes.OrderByDescending(e => e.attack + e.defence).First();
        r.EnemyBasic = enemyTypes.OrderBy(e => e.attack + e.defence).FirstOrDefault();
        r.EnemyStrong = enemyTypes.OrderByDescending(e => e.attack + e.defence).FirstOrDefault();
        r.EnemyCityTemplate = FirstToken(v.enemyCityTokens)?.unitType;
        r.PlayerCityTemplate = FirstToken(v.playerCityTokens)?.unitType;
        Core.Log.Msg($"  city templates: enemy='{r.EnemyCityTemplate?.unitName}' player='{r.PlayerCityTemplate?.unitName}'");
        return r;
    }

    private static WarTokenData FirstToken(Il2CppSystem.Collections.Generic.List<ConditionalWarTokenData> groups)
    {
        if (groups == null)
        {
            return null;
        }

        foreach (ConditionalWarTokenData g in groups)
        {
            foreach (WarTokenData t in g.tokens)
            {
                return t;
            }
        }

        return null;
    }

    /// <summary>Clone a vanilla city unit type under a new name.</summary>
    public static WarUnitTypeData City(WarUnitTypeData template, string name, string capturedVar)
    {
        string key = $"{template.Pointer}:{name}";
        if (_cities.TryGetValue(key, out WarUnitTypeData c) && c != null)
        {
            return c;
        }

        c = UnityEngine.Object.Instantiate(template);
        c.name = $"City_{name}";
        c.unitName = name;
        c.unitNameLocaId = string.Empty;
        if (capturedVar != null)
        {
            c.isCityCapturedVariableName = capturedVar;
        }

        c.hideFlags = HideFlags.DontUnloadUnusedAsset;
        _cities[key] = c;
        return c;
    }
}

