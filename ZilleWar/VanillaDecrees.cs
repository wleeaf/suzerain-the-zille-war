using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Vanilla royal decrees in a war game: edited (cost, text, condition, effect) or hidden (by their IsEnabled condition) while
/// war is declared, and restored to their all-games values when it is not. The all-games edits (<see cref="VanillaDecreesAll"/>)
/// are composed here too, field by field from the remembered vanilla values; a war-only edit wins over an all-games edit.
/// The edits are applied to the decree entities every step (<see cref="OnStep"/>, from WarDecrees), which also re-applies them
/// after the game reloads its entity data. Only the fields an edit names are touched: SweepLateKit swaps the title, text and
/// cost of a few vanilla decrees itself, and those fields are left to it.
/// Effects that must depend on the war at the moment they happen (a tax signed during the fighting, a hub completed while the
/// army is in Zille) are watchers on the vanilla flags, run once on a false-to-true change while war is declared.
/// </summary>
internal static class VanillaDecrees
{
    private sealed class Edit
    {
        public string Title, Description, Append, Cost, EnabledOr, EnabledAnd, EnactAppend;
        public readonly List<(string message, string condition)> Errors = new();
        public readonly List<(string from, string to)> Replace = new();
    }

    private sealed record Original(string Title, string Description, string Cost, string Enabled, string Enact,
        List<(string message, string condition)> Errors);

    private sealed record Watcher(string Flag, string Seen, Action Effect);

    private static readonly Dictionary<string, Edit> Edits = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Original> Originals = new(StringComparer.Ordinal);
    private static readonly List<Watcher> Watchers = new();

    private const string VarInit = "ZilleWar.VDec_Init";
    private const string WarTaxFlag = "RiziaDLC.Policy_RD_Economy_WarProfitsTax";
    private const string IntelHubFlag = "RiziaDLC.Policy_RD_Order_BuildMaveNortusIntelHub_Completed";
    private const string VarEarlyT7WarTaxNews = "ZilleWar.E7_News_WarTax";    // EarlyT7's Turn 7 war tax story
    private const string Active = "ZilleWar.War_Active == true";
    private const string NotActive = "ZilleWar.War_Active == false";

    internal static IReadOnlyList<string> SalesKeys => Sales;

    private static readonly string[] Sales =
    {
        "SellBomberPlanesOne", "SellBomberPlanesTwo", "SellBomberPlanesThree", "SellFighterPlanesOne", "SellFighterPlanesTwo",
        "SellFighterPlanesThree", "SellMilitaryEquipmentOne", "SellMilitaryEquipmentTwo", "SellMilitaryEquipmentThree",
        "SellMilitaryEquipmentFour", "SellShipsOne", "SellShipsTwo", "SellSubmarinesOne", "SellSubmarinesTwo",
        "SellSupportVehiclesOne", "SellSupportVehiclesTwo", "SellSupportVehiclesThree", "SellTanksOne", "SellTanksTwo",
        "SellTanksThree",
    };

    // ------------------------------------------------------------------ the edits

    public static void Register()
    {
        VanillaDecreesAll.Register();

        // V1: the war tax can be imposed while the army fights, and the Houses that build its weapons pay for it (House Azaro's
        // -1 is a recompute row, E35 in VanillaDecreesAll: a script change to a House is erased at the next scene)
        const string warTax = "Economy_Taxes_WarProfitsTax";
        EnabledOr(warTax, Active);
        Append(warTax, "The tax will fall hardest on the arms works of House Azaro and the construction firms of the de Ravas, and " +
                       "both will remember it.");

        // V2: no arms sales abroad while the army fights in Zille
        foreach (string s in Sales)
        {
            EnabledAnd($"Order_Military_{s}", NotActive);
        }

        // V3: the provincial levies cannot be disbanded while they fight
        EnabledAnd("Order_ConscriptionLevy_RemoveProvincialLevies", NotActive);

        // V4: more levies in a war game cost the Houses their sons (the -1 each is a recompute row, E28 in VanillaDecreesAll)
        const string levies = "Order_ConscriptionLevy_IncreaseProvincialLevies";
        Append(levies, "With the Kingdom at war, the dukes know their levies will not drill in the provinces but fight in Zille, and " +
                       "House Sazon and House Azaro will count every son they send.");

        // V5, V6: nothing is built in a town the enemy holds
        foreach (string d in new[] { "Economy_Resources_BuildMaveNortusOilfield", "Order_SecurityIntel_BuildMaveNortusIntelligenceHub" })
        {
            ErrorFirst(d, "Mave Nortus is under Wehzek occupation", "ZilleWar.MaveNortus_Occupied == true");
        }

        foreach (string d in new[]
                 {
                     "Order_Military_BuildBomberPlaneFactory", "Order_Military_BuildFighterPlaneFactory", "Religion_JalesGolcondistSanctuary",
                     "Religion_JalesWruhecistSanctuary",
                 })
        {
            ErrorFirst(d, "Jales is under Wehzek occupation", "ZilleWar.Jales_Occupied == true");
        }

        // V7: the military intelligence hub in Mave Nortus serves the war, and the armistice line after it
        Watchers.Add(new(IntelHubFlag, "ZilleWar.VDec_IntelHub_Seen", OnIntelHub));
        WarDecrees.AddRow("PowerProjection_War_Outcome",
            "ZilleWar.War_Over == true && ZilleWar.War_Zille_Liberated == false && RiziaDLC.Policy_RD_Order_BuildMaveNortusIntelHub_Completed == true",
            "RiziaDLC.Country_Military_PowerProjection += 1;");

        // V8, V9, V10: the Wehzek market is closed by the war, so these no longer win Wraclawitz's favor
        const string pharma = "Economy_InvestInPharmaceuticals";
        Cost(pharma, "-2 Authority\n-3 Budget\n+250 Military Manpower");
        EnactAppend(pharma, "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower -= 2;\nRiziaDLC.Resources_Military_Manpower += 250;");
        Append(pharma, "With Wehzek buyers gone, the first output will go to the army's field hospitals, and the soldiers it saves " +
                       "will return to the ranks.");
        const string timber = "Economy_InvestInTimberIndustry";
        EnactAppend(timber, "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower -= 1;");
        Append(timber, "The Wehzek market that once took Rizian timber is closed by the war.");
        EnactAppend("Welfare_RightsFreedoms_LaborHandsLegalized", "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower -= 2;");

        // V11: Wehzek oil under the treaty replaces the Wehzek import the war removed
        EnabledOr("Welfare_FuelSubsidiesCheaper", "ZilleWar.Mod_TreatyOil >= 1");

        // V12: the emergency sale of the royal gold stake is open in the war's deficit turns
        ReplaceError("Economy_Resources_LowerRRGStake", "RiziaDLC.Resources_BudgetModifiersTotal <= 1",
            "RiziaDLC.Resources_BudgetModifiersTotal <= -4");

        // V13: no energy subsidies while the Crown rations fuel for the war
        foreach (string d in new[] { "Economy_Interventionism_GasHeatingSubsidiesForCitizens", "Economy_Interventionism_EnergySubsidiesForIndustries" })
        {
            ErrorFirst(d, "Wartime fuel rationing is in force", "ZilleWar.Decree_FuelRationing == true && ZilleWar.War_Active == true");
        }

        // V1's watcher: a war tax signed during the fighting is reported as a wartime measure
        Watchers.Add(new(WarTaxFlag, "ZilleWar.VDec_WarTax_Seen", OnWarTax));

        Variables.Register(VarInit);
        foreach (Watcher w in Watchers)
        {
            Variables.Register(w.Seen);
        }
    }

    private static Edit For(string name)
    {
        if (!Edits.TryGetValue(name, out Edit e))
        {
            Edits[name] = e = new Edit();
        }

        return e;
    }

    private static void EnabledOr(string name, string expresso)
    {
        For(name).EnabledOr = expresso;
        LuaLint.RecordExpresso($"Vanilla decree {name} IsEnabled (or)", expresso);
    }

    private static void EnabledAnd(string name, string expresso)
    {
        For(name).EnabledAnd = expresso;
        LuaLint.RecordExpresso($"Vanilla decree {name} IsEnabled (and)", expresso);
    }

    private static void EnactAppend(string name, string instructions) => For(name).EnactAppend = instructions;

    private static void Append(string name, string paragraph) => For(name).Append = paragraph;

    private static void Cost(string name, string cost) => For(name).Cost = cost;

    private static void ErrorFirst(string name, string message, string expresso)
    {
        For(name).Errors.Add((message, expresso));
        LuaLint.RecordExpresso($"Vanilla decree {name} error '{message}'", expresso);
    }

    private static void ReplaceError(string name, string from, string to)
    {
        For(name).Replace.Add((from, to));
        LuaLint.RecordExpresso($"Vanilla decree {name} error (replaced)", to);
    }

    // ------------------------------------------------------------------ every step

    public static void OnStep()
    {
        Run("apply", Apply);
        Run("approvals", VanillaDecreesAll.ApplyApprovals);
        Run("watchers", Watch);
        Run("all-games watchers", VanillaDecreesAll.Watch);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"VanillaDecrees {what} failed: {ex}");
        }
    }

    private static void Apply()
    {
        bool war = Variables.GetBool(WarFlags.Declared);
        foreach (var list in new[] { EntityDataManager.decreesData, EntityDataManager.DecreesData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecreeData d in list)
            {
                DecreeProperties p = d?.DecreeProperties;
                string name = d?.NameInDatabase;
                if (p == null || name == null)
                {
                    continue;
                }

                Edits.TryGetValue(name, out Edit e);
                VanillaDecreesAll.Edits.TryGetValue(name, out VanillaDecreesAll.AEdit a);
                if (e == null && a == null)
                {
                    continue;
                }

                // the vanilla values, remembered the first time the decree is seen (never overwritten)
                if (!Originals.TryGetValue(name, out Original o))
                {
                    var errors = new List<(string message, string condition)>();
                    if (p.ErrorValidationDatas != null)
                    {
                        foreach (DecreeProperties.ErrorValidationData r in p.ErrorValidationDatas)
                        {
                            errors.Add((r?.ErrorMessage, r?.ErrorCondition));
                        }
                    }

                    Originals[name] = o = new Original(p.Title, p.Description, p.CostText, p.IsEnabled, p.OnDecreeEnact, errors);
                }

                ApplyFields(p, e, a, o, war);
                if (e != null)
                {
                    ApplyErrors(p, e, war);
                }

                if (a != null)
                {
                    ApplyAllErrors(p, a, o);
                    ApplyApprovalSwaps(p, a);
                }
            }
        }
    }

    private static string AppendScript(string script, string add)
    {
        if (string.IsNullOrWhiteSpace(add))
        {
            return script;
        }

        string c = (script ?? string.Empty).TrimEnd();
        return string.IsNullOrEmpty(c) ? add : (c.EndsWith(";") ? c : c + ";") + "\n" + add;
    }

    /// <summary>Compose the vanilla value, the all-games edit and (in a war game) the war-only edit, field by field. Only the
    /// fields some edit names are written.</summary>
    private static void ApplyFields(DecreeProperties p, Edit e, VanillaDecreesAll.AEdit a, Original o, bool war)
    {
        if (e?.Title != null || a?.Title != null)
        {
            string want = war && e?.Title != null ? e.Title : a?.Title ?? o.Title;
            if (p.Title != want)
            {
                p.Title = want;
            }
        }

        if (e?.Description != null || e?.Append != null || a?.Description != null || a?.Append != null)
        {
            string want;
            if (war && e?.Description != null)
            {
                want = e.Description;
            }
            else
            {
                want = a?.Description ?? o.Description;
                if (a?.Append != null)
                {
                    want = (want ?? string.Empty).TrimEnd() + "\n\n" + a.Append;
                }

                if (war && e?.Append != null)
                {
                    want = (want ?? string.Empty).TrimEnd() + "\n\n" + e.Append;
                }
            }

            if (p.Description != want)
            {
                p.Description = want;
            }
        }

        if (e?.Cost != null || a?.Cost != null || a?.CostFn != null)
        {
            string want = war && e?.Cost != null ? e.Cost : a?.CostFn?.Invoke() ?? a?.Cost ?? o.Cost;
            if (p.CostText != want)
            {
                p.CostText = want;
            }
        }

        if (e?.EnabledOr != null || e?.EnabledAnd != null || a?.EnabledOr != null || a?.EnabledReplace != null || a?.EnabledAnd.Count > 0)
        {
            string en = a?.EnabledReplace ?? o.Enabled ?? string.Empty;
            // an empty condition is always true: OR-ing to it changes nothing
            if (a?.EnabledOr != null && !string.IsNullOrWhiteSpace(en))
            {
                en = $"({en}) || ({a.EnabledOr})";
            }

            if (war && e?.EnabledOr != null && !string.IsNullOrWhiteSpace(en))
            {
                en = $"({en}) || ({e.EnabledOr})";
            }

            var ands = new List<string>();
            if (a != null)
            {
                ands.AddRange(a.EnabledAnd);
            }

            if (war && e?.EnabledAnd != null)
            {
                ands.Add(e.EnabledAnd);
            }

            foreach (string and in ands)
            {
                en = string.IsNullOrWhiteSpace(en) ? $"({and})" : $"({en}) && ({and})";
            }

            // nothing changed the condition: keep vanilla's own string (null stays null)
            string want = en == (o.Enabled ?? string.Empty) ? o.Enabled : en;
            if (p.IsEnabled != want)
            {
                p.IsEnabled = want;
            }
        }

        if (e?.EnactAppend != null || a?.EnactAppend != null || a?.EnactFn != null)
        {
            string want = o.Enact;
            if (a != null)
            {
                want = AppendScript(want, a.EnactAppend);
                want = AppendScript(want, a.EnactFn?.Invoke());
            }

            if (war && e?.EnactAppend != null)
            {
                want = AppendScript(want, e.EnactAppend);
            }

            if (p.OnDecreeEnact != want)
            {
                p.OnDecreeEnact = want;
            }
        }
    }

    /// <summary>The all-games error edits: inserted first, replaced statically, or rewritten every step from the vanilla
    /// condition (found through its message, so a price-indexed row can move both ways).</summary>
    private static void ApplyAllErrors(DecreeProperties p, VanillaDecreesAll.AEdit a, Original o)
    {
        if (a.Errors.Count == 0 && a.Replace.Count == 0 && a.ReplaceFn.Count == 0)
        {
            return;
        }

        var rows = p.ErrorValidationDatas;
        if (rows == null)
        {
            if (a.Errors.Count == 0)
            {
                return;
            }

            rows = new Il2CppSystem.Collections.Generic.List<DecreeProperties.ErrorValidationData>();
            p.ErrorValidationDatas = rows;
        }

        foreach ((string message, string condition) in a.Errors)
        {
            bool present = false;
            for (int i = 0; i < rows.Count && !present; i++)
            {
                present = rows[i]?.ErrorMessage == message;
            }

            if (!present)
            {
                rows.Insert(0, new DecreeProperties.ErrorValidationData { ErrorMessage = message, ErrorCondition = condition });
            }
        }

        foreach ((string from, string to) in a.Replace)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null && rows[i].ErrorCondition == from)
                {
                    rows[i].ErrorCondition = to;
                }
            }
        }

        foreach ((string from, Func<string> to) in a.ReplaceFn)
        {
            string want = to();
            foreach ((string message, string condition) in o.Errors)
            {
                if (condition != from || message == null)
                {
                    continue;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i] != null && rows[i].ErrorMessage == message && rows[i].ErrorCondition != want)
                    {
                        rows[i].ErrorCondition = want;
                    }
                }
            }
        }
    }

    private static void ApplyApprovalSwaps(DecreeProperties p, VanillaDecreesAll.AEdit a)
    {
        var list = p.DecreeApprovals;
        if (a.ApprovalSwaps.Count == 0 || list == null)
        {
            return;
        }

        foreach ((string from, string to) in a.ApprovalSwaps)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == from)
                {
                    list[i] = to;
                }
            }
        }
    }

    private static void ApplyErrors(DecreeProperties p, Edit e, bool war)
    {
        if (e.Errors.Count == 0 && e.Replace.Count == 0)
        {
            return;
        }

        var rows = p.ErrorValidationDatas;
        if (rows == null)
        {
            if (!war || e.Errors.Count == 0)
            {
                return;
            }

            rows = new Il2CppSystem.Collections.Generic.List<DecreeProperties.ErrorValidationData>();
            p.ErrorValidationDatas = rows;
        }

        foreach ((string message, string condition) in e.Errors)
        {
            int at = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i]?.ErrorMessage == message)
                {
                    at = i;
                    break;
                }
            }

            if (war && at < 0)
            {
                // ours first, so the war's reason is shown before vanilla's generic ones
                rows.Insert(0, new DecreeProperties.ErrorValidationData { ErrorMessage = message, ErrorCondition = condition });
            }
            else if (!war && at >= 0)
            {
                rows.RemoveAt(at);
            }
        }

        foreach ((string from, string to) in e.Replace)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                DecreeProperties.ErrorValidationData r = rows[i];
                if (r == null)
                {
                    continue;
                }

                if (war && r.ErrorCondition == from)
                {
                    r.ErrorCondition = to;
                }
                else if (!war && r.ErrorCondition == to)
                {
                    r.ErrorCondition = from;
                }
            }
        }
    }

    // ------------------------------------------------------------------ watchers

    /// <summary>
    /// Record each watched vanilla flag; on a false-to-true change while war is declared, run its effect once. The first step
    /// of a game (or of a save made before this build) only records, so a flag already set is not taken for a new one.
    /// </summary>
    private static void Watch()
    {
        bool init = Variables.GetBool(VarInit);
        bool declared = Variables.GetBool(WarFlags.Declared);
        foreach (Watcher w in Watchers)
        {
            bool now = Variables.GetBool(w.Flag);
            if (Variables.GetBool(w.Seen) == now)
            {
                continue;
            }

            Variables.Set(w.Seen, now);
            if (now && init && declared)
            {
                Run($"effect of {w.Flag}", w.Effect);
            }
        }

        if (!init)
        {
            Variables.Set(VarInit, true);
        }
    }

    private static bool FreePressRevoked => Variables.GetBool("RiziaDLC.Policy_RD_Welfare_FreePressRevoked");

    /// <summary>V1: the war tax signed while the army fights (the Turn 7 story in EarlyT7 is then not printed again).</summary>
    private static void OnWarTax()
    {
        if (!Variables.GetBool(WarFlags.Active) || Variables.GetBool("RiziaDLC.Pales_War") || Variables.GetBool(VarEarlyT7WarTaxNews))
        {
            return;
        }

        Variables.Set(VarEarlyT7WarTaxNews, true);
        // vanilla's own Royal Herald item (RD_RH_WarProfitsTax) is generic and still runs; its Voice of Rizia item is about Pales
        // and hidden in a war game (EarlyT7), so only the Voice's wartime version is ours
        if (FreePressRevoked)
        {
            return;
        }

        NewsWire.Publish("VDec_WarTax", "V", "War Profiteers Taxed", NewsWire.P(
            "If the war in Zille must be fought, then at the very least our corporations should not grow fat on it. His Majesty " +
            "has decreed a heavy tax on war profits, and for once the Palace and this newspaper agree on something.",
            "The arms works of Cardesse-Montaklar have run day and night since Konstantium, and the families of Brenas pay for " +
            "this war in sons. We can only hope His Majesty is ready for the howls of House Azaro, which will be heard from " +
            "here to the Alca."));
    }

    /// <summary>V7: the hub completed while the army fights in Zille: its agents find the garrison's weak points.</summary>
    private static void OnIntelHub()
    {
        if (!Variables.GetBool(WarFlags.Active))
        {
            return;
        }

        int before = ZilleCampaign.Strength;
        if (before <= 3)
        {
            return;
        }

        ZilleCampaign.Strength = before - 1;
        Core.Log.Msg($"VanillaDecrees: Wehzek garrison strength {before} -> {ZilleCampaign.Strength} (Mave Nortus intelligence hub).");
        NewsWire.Publish("VDec_IntelHub", "RH", "Eyes Behind the Wehzek Lines", NewsWire.P(
            "The new military intelligence hub in Mave Nortus has opened its doors, and its first reports are already on the desks " +
            "of the war room. The General Staff says they have located Wehzek supply depots and gun positions in Zille that the " +
            "garrison believed hidden.",
            "\"The enemy has fewer secrets this week than he had last week,\" an officer told the Royal Herald. \"He will have fewer " +
            "still next week.\""));
    }
}
