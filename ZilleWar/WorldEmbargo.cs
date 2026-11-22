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
        "ZilleWar.War_Declared == true && (ZilleWar.War_Active == true || ZilleWar.ZilleCity_Held == true || ZilleWar.War_Zille_Liberated == true) && ZilleWar.Treaty_Peace == false && RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP == false && RiziaDLC.Trait_CommieKing == false";
    private const string VarAnnounced = "ZilleWar.World_EmbargoAnnounced";
    private const string VarLifted = "ZilleWar.World_EmbargoLifted";
    private static string _originalDescription;

    public static void Register()
    {
        Variables.Register(VarAnnounced);
        Variables.Register(VarLifted);
        Vanilla.Gate("Turn05_Decision_EastTradeReduced_Penalty", WarFlags.XPeace);
        LuaLint.RecordExpresso("Embargo line", WarCondition);
    }

    /// <summary>Rizian troops hold some of Zille: the war is on, or Zille city is held, or the province is liberated.</summary>
    private static bool Holding() =>
        Variables.GetBool(WarFlags.Declared) && (WorldSystems.Active || WorldSystems.ZilleCityHeld || WorldSystems.Liberated) &&
        !Variables.GetBool(ZilleTreaty.VarPeace);

    /// <summary>The peace talks have been held (a treaty signed or turned down): the embargo is not lifted before them.</summary>
    private static bool TalksConcluded() =>
        Variables.GetBool(ZilleTreaty.VarPeace) || Variables.GetString(ZilleTreaty.VarKind) is { Length: > 0 } kind && kind != "nil";

    private static bool Applies() =>
        Holding() && !Variables.GetBool("RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP") &&
        !Variables.GetBool("RiziaDLC.Trait_CommieKing");

    public static void OnStep()
    {
        bool declared = Holding();
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
                "Valgsland and its partners in the Contanan Security Pact have suspended trade with the Kingdom of Rizia \"until " +
                "Rizian troops leave Zille.\" Contanan freighters have been ordered not to call at Rizian " +
                "ports, and Valgslandian banks have frozen new credit to Rizian firms.",
                "The embargo stops short of a blockade, and goods still reach Rizia through third countries, late and at a premium. " +
                "Economists in Porte Drazon estimate the cost to the Rizian treasury at about six billion guilders a quarter."));
            NewsWire.Report("Embargo", SuzerainTokenName.RiziaCountryValgsland, "A Contanan Embargo", NewsWire.P(
                "Chancellor Hegel's government has announced, for the Contanan Security Pact, that trade with Rizia is suspended " +
                "until Rizian troops leave Zille. Shipments already at sea will be turned back; contracts in force are frozen.",
                "Councilor Werner puts the loss at six billion guilders a quarter, for as long as the embargo lasts. Our embassy notes that the measure was drafted " +
                "before the King had finished his speech in Konstantium. Valgsland, it seems, had been waiting for a reason."));
        }

        if (Variables.GetBool(VarAnnounced) && !Variables.GetBool(VarLifted) && WorldSystems.WarEnded && !Holding() && TalksConcluded())
        {
            Variables.Set(VarLifted, true);
            bool treaty = Variables.GetBool(ZilleTreaty.VarPeace);
            bool terms = !treaty && Variables.GetBool(ZilleTreaty.VarSigned);
            if (treaty)
            {
                NewsWire.Publish("EmbargoLifted", "G", "Contana Ends Its Embargo after the Zille Treaty", NewsWire.P(
                    "With Wehlen's signature on a treaty settling Zille, Valgsland and its partners in the Contanan Security Pact have " +
                    "let their embargo on Rizia lapse. \"Wraclawitz has spoken for itself,\" a Contanan spokesman said. \"We will not be " +
                    "more Wehzek than the Wehzeks.\"",
                    "Chancellor Hegel's office issued no statement at all. Bankers in Konstantium expect credit to Rizian firms to return " +
                    "slowly, and Valgslandian credit not at all."));
                return;
            }

            NewsWire.Publish("EmbargoLifted", "G", "Contana Lifts the Embargo on Rizia", NewsWire.P(
                (terms ? "With the Wraclawitz treaty signed and no Rizian soldier left in Zille," : "With no Rizian soldier left in Zille,") + " Valgsland and its partners in the Contanan Security Pact have let the embargo on " +
                "trade with Rizia lapse. Contanan freighters are expected back in Rizian ports within the week.",
                "Chancellor Hegel's office called the decision \"a recognition of facts on the ground.\" Bankers in Konstantium were less " +
                "formal, and said credit to Rizian firms would return slowly, if it returned at all."));
        }
    }
}
