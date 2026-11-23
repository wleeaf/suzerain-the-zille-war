using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The one HUD line the supply war owns: "-1 per turn from Valgslandian Port Closures" while Chancellor Hegel keeps
/// Valgslandian ports closed to Rizian cargo (SupplyWar.VarValgslandPorts, value SupplyWar.ModValgsland).
/// Everything else (the Rumburgian credit repayment) is shown by WorldModifiers through its Mod_* variables.
/// </summary>
internal static class SupplyModifiers
{
    private const string Name = "Rizia_Budget_Modifier_ZilleWarValgslandPorts";
    private const string Stat = "Rizia_HUDStat_Budget";
    private static bool _added;

    /// <summary>The game rebuilt its entity lists (a save was loaded): add ours again on the next step.</summary>
    internal static void ResetAdded() => _added = false;

    public static void Install()
    {
        if (_added || EntityDataManager.HUDPeriodicStatModifierData == null || EntityDataManager.HUDStatData == null)
        {
            return;
        }

        HUDPeriodicStatModifierData template = null;
        foreach (HUDPeriodicStatModifierData m in EntityDataManager.HUDPeriodicStatModifierData)
        {
            if (m?.NameInDatabase == "Rizia_Budget_Modifier_WarExpenditures")
            {
                template = m;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return;     // data not loaded yet; try again next step
        }

        var data = new HUDPeriodicStatModifierData
        {
            Id = $"0x5A11{StableHash.Of(Name):X8}",
            NameInDatabase = Name,
            Path = "Rizia/HUD Panel/Modifiers/ZilleWar",
            IsWIP = false,
            Notes = string.Empty,
            AppBundleProperties = template.AppBundleProperties,
            HUDPeriodicStatModifierProperties = new HUDPeriodicStatModifierProperties
            {
                Variable = SupplyWar.ModValgsland,
                Condition = $"{SupplyWar.VarValgslandPorts} == true",
                Description = "{0} per turn from Valgslandian Port Closures",
            },
        };
        ZilleOperation.AddByName(EntityDataManager.hudPeriodicStatModifierData, data, x => x.NameInDatabase);
        ZilleOperation.AddByName(EntityDataManager.HUDPeriodicStatModifierData, data, x => x.NameInDatabase);

        foreach (var list in new[] { EntityDataManager.hudStatData, EntityDataManager.HUDStatData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (HUDStatData s in list)
            {
                var mods = s?.HUDStatProperties?.Modifiers;
                if (s?.NameInDatabase == Stat && mods != null && !mods.Contains(Name))
                {
                    mods.Add(Name);
                }
            }
        }

        _added = true;
    }
}
