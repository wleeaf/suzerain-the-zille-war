using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// The Valgslandian and CSP embargo. Vanilla already has the line "-2 per turn from Reduced Trade from the CSP"
/// (Rizia_Budget_Modifier_EastTradeReduced, value RiziaDLC.Budget_Modifier_EastTradeReduced) for a Rizia at odds with
/// Valgsland. A declaration of war on Contana's client brings it on at once, as an embargo: the same line, so the two can
/// never stack, with its condition widened and its wording changed. Vanilla's own exclusions stay (Caleqabiz given to the
/// CSP, a communist-leaning King). Vanilla's Turn 5 notice of the same penalty is replaced by the embargo's news.
/// </summary>
internal static class WorldEmbargo
{
    private const string Line = "Rizia_Budget_Modifier_EastTradeReduced";
    private const string WarCondition =
        "ZilleWar.War_Declared == true && RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP == false && RiziaDLC.Trait_CommieKing == false";
    private const string VarAnnounced = "ZilleWar.World_EmbargoAnnounced";
    private static string _originalDescription;

    public static void Register()
    {
        Variables.Register(VarAnnounced);
        Vanilla.Gate("Turn05_Decision_EastTradeReduced_Penalty", WarFlags.XPeace);
        LuaLint.RecordExpresso("Embargo line", WarCondition);
    }

    private static bool Applies() =>
        Variables.GetBool(WarFlags.Declared) && !Variables.GetBool("RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP") &&
        !Variables.GetBool("RiziaDLC.Trait_CommieKing");

    public static void OnStep()
    {
        bool declared = Variables.GetBool(WarFlags.Declared);
        foreach (var list in new[] { EntityDataManager.hudPeriodicStatModifierData, EntityDataManager.HUDPeriodicStatModifierData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (HUDPeriodicStatModifierData m in list)
            {
                HUDPeriodicStatModifierProperties p = m?.HUDPeriodicStatModifierProperties;
                if (m?.NameInDatabase != Line || p == null)
                {
                    continue;
                }

                _originalDescription ??= p.Description;
                string marked = $"({WarCondition})";
                if (p.Condition != null && !p.Condition.Contains(marked, StringComparison.Ordinal))
                {
                    p.Condition = $"({p.Condition}) || {marked}";
                }

                p.Description = declared ? "{0} per turn from the Valgslandian and CSP Embargo" : _originalDescription;
            }
        }

        if (Applies() && !Variables.GetBool(VarAnnounced))
        {
            Variables.Set(VarAnnounced, true);
            NewsWire.Publish("Embargo", "G", "Valgsland and the CSP Embargo Rizia", NewsWire.P(
                "Valgsland and its partners in the Contanan Security Pact have suspended trade with the Kingdom of Rizia \"for as " +
                "long as Rizian troops threaten the Republic of Wehlen.\" Contanan freighters have been ordered not to call at Rizian " +
                "ports, and Valgslandian banks have frozen new credit to Rizian firms.",
                "The embargo stops short of a blockade, and goods still reach Rizia through third countries, late and at a premium. " +
                "Economists in Konstantium estimate the cost to the Rizian treasury at several million guilders a month."));
            NewsWire.Report("Embargo", SuzerainTokenName.RiziaCountryValgsland, "A Contanan Embargo", NewsWire.P(
                "Chancellor Hegel's government has announced, with the support of United Contana, that trade with Rizia is suspended " +
                "until the war in Zille ends. Shipments already at sea will be turned back; contracts in force are frozen.",
                "Councilor Werner estimates the loss at two units of budget every turn. Our embassy notes that the measure was drafted " +
                "before the King had finished his speech in Konstantium. Valgsland, it seems, had been waiting for a reason."));
        }
    }
}
