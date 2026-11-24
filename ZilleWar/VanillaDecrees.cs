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

    /// <summary>
    /// The papers on more levies and on arms purchases are written for a peacetime build-up ("sooner or later it will lead to
    /// conflict", the AN "advocating de-escalation"). In a war game they are rewritten for the war being fought or just ended,
    /// latched at the state of the moment they ran (SweepEarlyKit.News). Peace games keep vanilla's text.
    /// </summary>
    private static void DecreePress()
    {
        static string When(string s, string active, string after) => s == SweepEarlyKit.StActive ? active : after;

        SweepEarlyKit.News("RD_V_IncreaseProvincialLevies", (s, _) => SweepEarlyKit.P(
            When(s,
                "The King has demanded more levies from every province, and nobody in Brenas pretends they will spend the year drilling on the " +
                "parade ground. The new recruits are going north, to Zille, to a war the King declared alone in a foreign capital.",
                "The war in Zille is over, and the King has demanded more levies from every province all the same. Somebody in the palace " +
                "has decided that the army that went to Zille must be made bigger, not sent home."),
            "The Palace has left it to the provinces to fill the new quotas, with volunteers preferred. We do not encourage anyone to sign " +
            "up. If the Crown wants more sons, let it ask the families who have already buried one.",
            "Some hope remains that certain nobles will stand up to this demand. Our readers are counting on you, House Sazon."));

        SweepEarlyKit.News("RD_G_Buy_TanksOne", (s, _) => SweepEarlyKit.P(
            "Rizia has confirmed the acquisition of a new fleet of battle tanks. " +
            When(s, "The purchase comes while the Royal Army is fighting Wehlen for Zille, and the first battalions are expected at the front within weeks.",
                "The purchase comes after the war over Zille, and officials describe it as replacing the armor lost there."),
            "Diplomats in Konstantium note that Rizia already stands accused at the Alliance of changing a border by force, and that its " +
            "neighbors will read the order in that light."));
        SweepEarlyKit.News("RD_V_Buy_TanksOne", (s, _) => SweepEarlyKit.P(
            When(s, "News of King Romus ordering several battalions of battle tanks has ignited outrage. The army already has a war in Zille, " +
                    "and the King's answer to its casualty lists is to buy more machines to send after them.",
                "News of King Romus ordering several battalions of battle tanks has ignited outrage. The war in Zille is barely over, and " +
                "the King is already buying the armor for the next one."),
            "While the monarch sits in his palace, it is the people who bear the cost of his militarism. How long will Rizians remain silent " +
            "while their future is gambled away?"));
        SweepEarlyKit.News("RD_G_Buy_FighterPlanesOne", (s, _) => SweepEarlyKit.P(
            "The Kingdom of Rizia has acquired a squadron of modern fighter aircraft. " +
            When(s, "Officials say the planes are needed over Zille, where Wehzek aircraft have contested the skies since the war began.",
                "Officials say the planes replace aircraft lost in the war over Zille and will guard the kingdom's northern skies."),
            "Skeptics argue that the purchase diverts money from domestic reforms, and that Rizia's neighbors have reason to watch its air force closely."));
        SweepEarlyKit.News("RD_V_Buy_FighterPlanesOne", (s, _) => SweepEarlyKit.P(
            When(s, "As the government pours money into new fighter planes for the war in Zille, one must ask how many more machines the King " +
                    "will buy before he asks what diplomacy could have done.",
                "The war in Zille is over, and the government is still pouring money into fighter planes. One must ask why the King keeps " +
                "buying weapons when the war he chose has ended."),
            "The message is clear: while the kingdom pours money into its military, meaningful dialogue and peace-building are shamefully neglected!"));
        SweepEarlyKit.News("RD_G_Buy_BomberPlanesOne", (s, _) => SweepEarlyKit.P(
            "The Kingdom of Rizia has acquired a fleet of modern bomber aircraft. " +
            When(s, "The purchase comes in the middle of Rizia's war with Wehlen, and analysts expect the bombers over Zille before the next operation.",
                "The purchase comes after Rizia's war with Wehlen over Zille, and regional analysts caution that it projects force toward " +
                "neighbors who already have reason to fear it."),
            "Officials herald the purchase as crucial for national defense. Critics note that domestic prosperity and global trade make " +
            "for a strange companion to a growing bomber fleet."));
        SweepEarlyKit.News("RD_G_Buy_SupportVehiclesOne", (s, _) => SweepEarlyKit.P(
            "The Kingdom's Royal Defense Department has announced the procurement of a fleet of military support vehicles from " +
            "international manufacturers, to strengthen the army's logistics and supply lines.",
            When(s, "The trucks are expected on the roads to Zille within weeks, where the Royal Army has struggled to keep its front supplied.",
                "The order follows Rizia's war with Wehlen over Zille, which showed how far the army's supply lines could be stretched.")));
        SweepEarlyKit.News("RD_V_Buy_SupportVehiclesOne", (s, _) => SweepEarlyKit.P(
            When(s, "The King's latest purchase of military support vehicles is for the war in Zille, and every truck bought is a truck that " +
                    "will carry our sons to the front.",
                "The war in Zille is over, and the King is buying military support vehicles as if it were not. Despite repeated calls for " +
                "negotiation, Romus' answer to every problem is still a bigger army."),
            "Critics warn that this unchecked militarization risks dragging the nation into yet another costly conflict, endangering both " +
            "lives and the economy."));

        // Free press while the General Staff's wartime censorship still reads every line about Zille (WarDecrees)
        Func<string, bool> censored = _ => Latched(LatchCensored, () =>
            Variables.GetBool("ZilleWar.Decree_WarCensorship") && !Variables.GetBool("ZilleWar.Decree_LiftCensorship"));
        SweepEarlyKit.AppendNews("RD_RH_AllowFreePress", _ =>
            "Reporting on Zille remains under the review of the General Staff until His Majesty lifts the wartime censorship.", censored);
        SweepEarlyKit.AppendNews("RD_V_AllowFreePress", _ =>
            "Not every censor has left our newsroom. The officers of the General Staff still read every line we print about Zille, and the " +
            "white columns will stay until His Majesty lifts his wartime censorship too.", censored);

        // Permanent permits for Wehzek workers, during or just after a war with Wehlen
        SweepEarlyKit.AppendNews("RD_RH_LaborHandsLegalized", s => When(s,
            "That the permits extend to Wehzek miners while our soldiers face their countrymen in Zille has raised eyebrows in Brenas, but " +
            "His Majesty has judged that the pits must keep running.",
            "That the permits extend to Wehzek workers so soon after the war in Zille has raised eyebrows in Brenas, but His Majesty has " +
            "judged that the pits must keep running."));
        SweepEarlyKit.AppendNews("RD_V_LaborHandsLegalized", s => When(s,
            "And yes, that includes Wehzeks. The war in Zille is Smolak's, not that of the miners who have dug Brenas coal for twenty years.",
            "And yes, that includes Wehzeks. The war in Zille was Smolak's, not that of the miners who have dug Brenas coal for twenty years."));

        // The oil field begun before the war, completed with Mave Nortus behind the Wehzek line (its energy is withheld meanwhile)
        SweepEarlyKit.News("RD_RH_BuildMaveNortusOilField_Completed", (_, _) => SweepEarlyKit.P(
                "His Majesty's engineers have completed the Mave Nortus Oil Field, a project begun before Wehzek soldiers reached the town. " +
                "Today it stands behind the Wehzek line. The derricks are idle, and not a barrel will reach Rizia until Mave Nortus is ours again.",
                "The Royal Herald is confident that day will come, and that the field will then repay every guilder the Crown has put into it."),
            _ => "Mave Nortus Oil Field Completed Behind the Line",
            _ => Latched(LatchMaveOil, () => Variables.GetBool(WarFlags.Occupied("Mave Nortus"))));
    }

    private const string LatchCensored = "ZilleWar.VDec_L_FreePressCensored";
    private const string LatchMaveOil = "ZilleWar.VDec_L_MaveOilOccupied";

    /// <summary>A yes/no fixed the first time it is asked (the step a news item is latched), so the printed item does not change
    /// when the state does later.</summary>
    private static bool Latched(string variable, Func<bool> now)
    {
        string v = Variables.GetString(variable);
        if (string.IsNullOrEmpty(v) || v == "nil")
        {
            v = now() ? "yes" : "no";
            Variables.Set(variable, v);
        }

        return v == "yes";
    }

    /// <summary>
    /// The Mave Nortus oil field yields nothing while the town is Wehzek-occupied: its vanilla energy line gets the occupation as a
    /// condition (nil-safe Expresso, so peace games, where the town is never occupied, are unchanged).
    /// </summary>
    private const string MaveOilLine = "Rizia_Energy_Modifier_MaveNortusOilfield";
    private const string MaveFree = "ZilleWar.MaveNortus_Occupied == false";

    private static void PatchMaveOilLine()
    {
        foreach (var list in new[] { EntityDataManager.hudPeriodicStatModifierData, EntityDataManager.HUDPeriodicStatModifierData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (HUDPeriodicStatModifierData m in list)
            {
                HUDPeriodicStatModifierProperties p = m?.HUDPeriodicStatModifierProperties;
                if (p == null || m.NameInDatabase != MaveOilLine || (p.Condition ?? string.Empty).Contains(MaveFree, StringComparison.Ordinal))
                {
                    continue;
                }

                p.Condition = string.IsNullOrWhiteSpace(p.Condition) ? MaveFree : $"({p.Condition}) && {MaveFree}";
            }
        }
    }

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

        DecreePress();

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
            "ZilleWar.War_Over == true && ZilleWar.War_Zille_Liberated == false && RiziaDLC.Policy_RD_Order_BuildMaveNortusIntelHub_Completed == true && " +
            MaveFree,
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
        Variables.Register(LatchCensored);
        Variables.Register(LatchMaveOil);
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
        Run("Mave Nortus oil line", PatchMaveOilLine);
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
        // a hub finished in a town the Wehzeks have since taken serves no one
        if (!Variables.GetBool(WarFlags.Active) || Variables.GetBool(WarFlags.Occupied("Mave Nortus")))
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
