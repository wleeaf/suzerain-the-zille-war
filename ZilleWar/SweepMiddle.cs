using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Sweep of Turns 7 and 8: everything the war changes that the first pass missed.
/// - This file: the Turn 7-8 decisions whose premise the war changes (descriptions reworded in memory while the war's
///   state calls for it, the original put back otherwise), the surplus medicine that can now go to the Zille front, and
///   the news, report and journal lines that follow from it.
/// - SweepMiddleT7: the Turn 7 personal scenes (Pabel, the family dinner, Hugo and Manus, Hugo's plea for Rico, Iza University).
/// - SweepMiddleT8: the Turn 8 council (hub, security, economy, garden), Manus's constitution, Hugo's resignation, the wedding.
/// </summary>
internal static class SweepMiddle
{
    private const string VarMedicineFront = "ZilleWar.SM_MedicineFront";   // "zille" | "both": where the surplus medicine went

    private const string Medicine = "Turn08_Decision_SurplusOfMedicine";
    private const string MedicineWarCondition = "RiziaDLC.Pales_War == true";
    private static readonly string MedicineWarConditionWide = $"(RiziaDLC.Pales_War == true || {WarFlags.X(WarFlags.Active)})";

    /// <summary>Called once from OnInitializeMelon: register variables, conversations, injections and vanilla edits.</summary>
    public static void Register()
    {
        Variables.Register(VarMedicineFront);
        RegisterDecisionTexts();
        SweepMiddleT7.Register();
        SweepMiddleT8.Register();
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        try
        {
            ApplyDecisionTexts();
            ApplyMedicine();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"SweepMiddle step failed: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------ war state, read in C#

    private static bool Declared => Variables.GetBool(WarFlags.Declared);
    private static bool Active => Variables.GetBool(WarFlags.Active);
    private static bool PalesWar => Variables.GetBool(WarFlags.PalesWar);
    private static bool Two => Active && PalesWar;
    private static bool Liberated => Variables.GetBool(WarFlags.Liberated);
    private static bool YachtHospital => Variables.GetBool(SweepEarlyT5Family.VarYachtHospital);
    private static bool Over => Variables.GetBool(WarFlags.Over);
    private static bool Lost => Over && !Liberated && !Variables.GetBool(WarFlags.ZilleCityHeld);

    // ------------------------------------------------------------------ decision descriptions

    private sealed record DecisionText(string Name, Func<bool> When, Func<string, string> Rewrite);

    private static readonly List<DecisionText> Texts = new();
    private static readonly Dictionary<string, string> OriginalDescriptions = new(StringComparer.Ordinal);

    /// <summary>First matching rule wins, so list the most specific state first.</summary>
    private static void Text(string name, Func<bool> when, Func<string, string> rewrite) => Texts.Add(new DecisionText(name, when, rewrite));

    /// <summary>Adds a sentence (or a paragraph) before vanilla's trailing cost tag, e.g. "[-2 Budget]", if there is one.</summary>
    private static Func<string, string> Append(string sentence, bool paragraph = false) => original =>
    {
        string body = original.TrimEnd();
        string tag = string.Empty;
        int open = body.LastIndexOf(" [", StringComparison.Ordinal);
        if (body.EndsWith("]", StringComparison.Ordinal) && open > 0)
        {
            tag = body.Substring(open);
            body = body.Substring(0, open).TrimEnd();
        }

        return body + (paragraph ? "\n\n" : " ") + sentence + tag;
    };

    /// <summary>Replaces a phrase of the vanilla description; if vanilla's wording changed, appends instead.</summary>
    private static Func<string, string> Replace(string from, string to, string orAppend) => original =>
        original.Contains(from, StringComparison.Ordinal) ? original.Replace(from, to) : Append(orAppend)(original);

    private static void RegisterDecisionTexts()
    {
        // ---- Turn 7

        // Derdia: Azmal's Golcondists and Smolak's Wehlen.
        Text("Turn07_Decision_DerdiaVisit", () => Active, Append(
            "The Golcondists of Derdia have no love for President Smolak, and the Supreme Wiseman will know that our army is fighting " +
            "the Wehzeks in Zille. A friend at Wehlen's back would be worth having, if his price is right."));
        Text("Turn07_Decision_DerdiaVisit", () => Over, Append(
            "The Golcondists of Derdia have no love for President Smolak. After the fighting in Zille, the Supreme Wiseman may see " +
            "Rizia as a useful neighbor in his own quarrel with Wehlen, or as a dangerous one."));

        // The Morellan aircraft arrive in the middle of a war.
        Text("Turn07_Decision_NameRoyalJet", () => Active, Append(
            "With the army fighting in Zille, the Air Staff has asked that every one of the new aircraft be sent north instead."));

        // The Pales war decisions, when the Zille war is still being fought (or has just been).
        Text("Turn07_Decision_OP1_MilitiaJoinsWar", () => Active, Append(
            "Much of the regular army is still committed in Zille, and the reserve has been scraped thin."));
        Text("Turn07_Decision_War_Both_EconomicTurmoil", () => Two, Replace(
            "The invasion of Pales has triggered",
            "The invasion of Pales, coming while the war in Zille is still being fought, has triggered",
            "Coming on top of the war in Zille, the strain on ordinary households has doubled."));
        Text("Turn07_Decision_War_Both_RefugeeStory", () => Declared && !Liberated && Variables.GetBool(WarFlags.Occupied("Mave Nortus")), Append(
            "The shelters of Brenas are already full of families who fled the fighting in Zille and in Wehzek-held Mave Nortus, and " +
            "the relief services are stretched thin."));
        Text("Turn07_Decision_War_Both_RefugeeStory", () => Declared && !Liberated, Append(
            "The shelters of Mave Nortus are already full of families who fled the fighting in Zille, and the relief services are " +
            "stretched thin."));
        Text("Turn07_Decision_War_Both_IndependentJournalism", () => Two, Replace(
            "the war-torn areas of Pales", "the war-torn areas of Pales and Zille",
            "Some of them are also asking to be let into Zille."));

        // A second foreign war while the first is fought.
        Text("Turn07_Decision_VendonesamCivilWarEscalates", () => Two, Append(
            "The General Staff has reminded us that every unit sent to Vendonesam is one fewer for the fronts in Zille and Pales.", true));
        Text("Turn07_Decision_VendonesamCivilWarEscalates", () => Active, Append(
            "The General Staff has reminded us that every unit sent to Vendonesam is one fewer for the Zille front.", true));

        // Brenas carries the war on its doorstep (Mave Nortus lies on the Zille border).
        Text("Turn07_Decision_ProvincialTaxSubsidyTwo", () => Active, Replace(
            "and Brenas highlights rising unemployment.",
            "and Brenas, its roads full of soldiers bound for Zille and families fleeing it, highlights rising unemployment and the " +
            "cost of a war on its doorstep.",
            "Brenas adds that it is carrying the cost of the Zille war on its doorstep."));
        Text("Turn07_Decision_ProvincialTaxSubsidyTwo", () => Over, Replace(
            "and Brenas highlights rising unemployment.",
            "and Brenas highlights rising unemployment and the burden the Zille war has left on its border towns.",
            "Brenas adds that its border towns are still paying for the Zille war."));

        Text("Turn07_Decision_Protest_LivingStandards", () => Active, Append(
            "Many of the marchers carry ration cards, and a few carry photographs of sons serving in Zille."));

        // A royal party on the water while soldiers die, or after a defeat.
        Text("Turn07_Decision_RoyalYachtRestorationFinal", () => Active && !YachtHospital, Append(
            "A celebration on the water while our soldiers are fighting in Zille would not go unnoticed. The Royal Herald would call it " +
            "a show of confidence. The Voice of Rizia would call it something else.", true));
        Text("Turn07_Decision_RoyalYachtRestorationFinal", () => Lost && !YachtHospital, Append(
            "With the defeat in Zille still fresh, a celebration on the water would not go unnoticed, least of all by the families of " +
            "the fallen.", true));
        // divided: Zille city is ours, the rest of the province behind an armistice line
        Text("Turn07_Decision_RoyalYachtRestorationFinal", () => Over && !Liberated && !Lost && !YachtHospital, Append(
            "With half of Zille still behind the armistice line and the casualty lists still fresh in every parish, a celebration on the water " +
            "would not go unnoticed, least of all by the families of the fallen.", true));

        // the Rizania went to the Monqiz yards as a hospital ship at Turn 5: nobody is celebrating on her deck
        Text("Turn07_Decision_RoyalYachtRestorationFinal", () => Declared && YachtHospital, Append(
            "The Rizania is still in the Monqiz yards with wounded men aboard, and a celebration on the water would be a strange thing " +
            "to hold. The Voice of Rizia would say so.", true));

        // ---- Turn 8

        Text("Turn08_Decision_OP2_MilitiaJoinsWar", () => Active, Append(
            "The divisions still fighting in Zille cannot be spared for Pales."));
        // From Turn 6 on the Zille war is over, but the army that fought it is the army now fighting Pales.
        Text("Turn08_Decision_OP2_MilitiaJoinsWar", () => Over, Append(
            "Many of the regular divisions have not yet recovered from the fighting in Zille."));
        Text("Turn08_Decision_War_Both_ConscriptionLaborShortages", () => Two, Append(
            "With two wars to man, in Zille and in Pales, the shortage is felt twice over."));
        Text("Turn08_Decision_War_Both_ConscriptionLaborShortages", () => Over, Append(
            "Many of the men called up for the war in Zille were never released back to their fields and factories."));
        Text("Turn08_Decision_War_Both_MothersWarProtest", () => Declared && PalesWar, Replace(
            "as their children are sent to war.",
            "as their children are sent to war, first to Zille and now to Pales.",
            "Many of them have already buried sons who fell in Zille."));
        Text("Turn08_Decision_War_Both_RemembranceCeremony", () => Declared, Append(
            "Families of the soldiers who fell in Zille have asked that their sons be remembered alongside him."));

        Text("Turn08_Decision_War_UpgradeProductionLinesThree", () => Active, Replace(
            "either by preparing for potential conflicts",
            "either by supplying the war in Zille",
            "The General Staff would like the new output sent to the Zille front."));
        Text("Turn08_Decision_War_UpgradeProductionLinesThree", () => Over && !Liberated, Replace(
            "either by preparing for potential conflicts",
            "either by rearming the divisions that face Wehlen across the armistice line in Zille",
            "The General Staff would like the new output sent to the divisions facing Wehlen across the armistice line."));

        // The army offered to the House of Delegates is the army fighting in Zille.
        Text("Turn08_Decision_HODMilitaryIntervention", () => Active, Append(
            "With most of the army committed in Zille, the General Staff is less than pleased at the thought of sending soldiers to " +
            "stand guard in the House."));

        Text("Turn08_Decision_Vendonesam_TroopsReturn", () => Active, Append(
            "The General Staff means to send them north to Zille within weeks of their return."));

        // The reform rallies take up the war.
        foreach (string rally in new[] { "Turn08_Decision_ReformMovement_Rally", "Turn08_Decision_ReformMovement_Rally_Radical" })
        {
            Text(rally, () => Active, Append("Among the banners are new ones calling for an end to the war in Zille."));
            Text(rally, () => Lost, Append("Many of the marchers also want to know who will answer for the defeat in Zille."));
            Text(rally, () => Over && !Liberated, Append("Among the banners are some asking what the war in Zille was for."));
        }

        // The Auremina concession granted to RRG in the Turn 8 security brief comes with the company.
        foreach (string rrg in new[] { "Turn08_Decision_RRGNationalization", "Turn08_Decision_RRGNationalization_RustyFled" })
        {
            Text(rrg, () => Declared && Variables.GetString(LateT8.VarGold) == "rrg", Append(
                "The concession for the Auremina gold mines in Zille, granted to the company after the fighting, would pass to the Crown " +
                "along with it."));
        }

        // South Merkopa's new alliance, founded without Wehlen.
        Text("Turn08_Decision_IntermerkopumFounding", () => Declared, Append(
            "Whatever form it takes, the alliance will be founded without Wehlen. In Wraclawitz, President Smolak has already called it " +
            "a club for Rizia's accomplices."));

        // The wedding committee, in a country at war (or just out of one).
        Text("Turn08_Decision_WeddingCommittee", () => Active, Append(
            "Some at court have asked whether a lavish wedding is fitting while the war in Zille goes on. Others say the country needs " +
            "a reason to celebrate now more than ever.", true));
        Text("Turn08_Decision_WeddingCommittee", () => Liberated, Append(
            "The liberation of Zille has only added to the mood, and the committee would be expected to find room among the guests " +
            "for a delegation from Zille.", true));
        Text("Turn08_Decision_WeddingCommittee", () => Lost, Append(
            "With the defeat in Zille still on everyone's mind, the committee would have to strike a careful balance between " +
            "celebration and mourning.", true));
    }

    private static void ApplyDecisionTexts()
    {
        foreach (var list in new[] { EntityDataManager.decisionsData, EntityDataManager.DecisionsData, EntityDataManager.AllDecisionsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecisionData d in list)
            {
                string name = d?.NameInDatabase;
                DecisionProperties p = d?.DecisionProperties;
                if (name == null || p == null)
                {
                    continue;
                }

                if (name == Medicine)
                {
                    WidenMedicineOption(p);
                }

                DecisionText rule = null;
                bool known = false;
                foreach (DecisionText t in Texts)
                {
                    if (t.Name != name)
                    {
                        continue;
                    }

                    known = true;
                    if (rule == null && t.When())
                    {
                        rule = t;
                    }
                }

                if (!known)
                {
                    continue;
                }

                if (!OriginalDescriptions.TryGetValue(name, out string original))
                {
                    OriginalDescriptions[name] = original = p.Description ?? string.Empty;
                }

                string wanted = rule != null ? rule.Rewrite(original) : original;
                if (p.Description != wanted)
                {
                    p.Description = wanted;
                }
            }
        }
    }

    // ------------------------------------------------------------------ the surplus medicine goes to Zille

    /// <summary>Vanilla's "Send to the war effort" option only exists in the Pales war; it also opens while the Zille war is fought.</summary>
    private static void WidenMedicineOption(DecisionProperties p)
    {
        if (p.Options == null)
        {
            return;
        }

        foreach (var o in p.Options)
        {
            if (o?.Condition != null && o.Condition.Trim() == MedicineWarCondition)
            {
                o.Condition = MedicineWarConditionWide;
            }
        }
    }

    private static void ApplyMedicine()
    {
        // Record where the medicine went on the step after the choice (the war cannot change in between).
        string front = Variables.GetString(VarMedicineFront);
        if (string.IsNullOrEmpty(front) && Variables.GetBool("RiziaDLC.Decision_Turn08_SurplusOfMedicine_SendToWarEffort") && Active)
        {
            front = PalesWar ? "both" : "zille";
            Variables.Set(VarMedicineFront, front);
        }

        // Before the choice, the texts follow the current state; after it, the recorded front.
        if (string.IsNullOrEmpty(front) && !Variables.GetBool("RiziaDLC.Decision_Turn08_SurplusOfMedicine_SendToWarEffort"))
        {
            front = Active ? (PalesWar ? "both" : "zille") : null;
        }

        if (front == "zille")
        {
            WorldText.News("Turn08_Decision_RH_SurplusOfMedicine_SendToWar", null,
                "To support our armed forces, the Crown has directed surplus medical supplies to the front lines in Zille. The decision " +
                "follows previous healthcare investments that boosted production in Bronaz and Valenqir, ensuring that our troops have " +
                "access to essential treatments.\n\n" +
                "Army surgeons in the field hospitals behind Mave Nortus report that wounded men who would have been sent home are " +
                "returning to their units within weeks. With casualty rates decreasing, the Kingdom's commitment to the well-being of its " +
                "brave soldiers is evident.\n\n" +
                "By turning surplus into strength, the Crown demonstrates its resolve to bring Zille home, and to bring our soldiers home " +
                "with it.");
            WorldText.News("Turn08_Decision_V_SurplusOfMedicine_SendToWar", null,
                "Sending surplus medical supplies to the troops in Zille may strengthen military morale, but it leaves some citizens " +
                "wondering why local healthcare wasn't prioritized. With clinics in Porte Drazon still facing shortages, it's fair to ask " +
                "whether the government is placing too much emphasis on its war.\n\n" +
                "Nobody begrudges a wounded soldier his bandages. But the war in Zille was the King's choice, and the patients waiting in " +
                "the corridors of our public hospitals never had one.\n\n" +
                "Instead of solely focusing on maintaining combat strength, shouldn't the Crown also ensure that ordinary citizens receive " +
                "the healthcare they deserve?");
            WorldText.Report("Turn08_PorteDrazon_Decision_SurplusOfMedicine_SendToWar", null,
                "The recent allocation of surplus medical supplies to the war effort in Zille has significantly improved healthcare for " +
                "Rizian soldiers. Field hospitals behind Mave Nortus report fewer deaths among the wounded and faster recovery times, " +
                "allowing troops to return to duty more swiftly.\n\n" +
                "Military leaders commend the Crown's decision, noting that the improved medical support strengthens morale on the front " +
                "lines.");
            Journal("Turn08_Decision_SurplusOfMedicine_SendToWar", "I have decided to allocate surplus medicine to support our troops in Zille.");
        }
        else if (front == "both")
        {
            WorldText.News("Turn08_Decision_RH_SurplusOfMedicine_SendToWar", null,
                "To support our armed forces, the Crown has directed surplus medical supplies to the front lines in Pales and in Zille. " +
                "The decision follows previous healthcare investments that boosted production in Bronaz and Valenqir, ensuring that " +
                "troops on both fronts have access to essential treatments.\n\n" +
                "Military leaders are praising the swift allocation of resources, emphasizing that the improved medical support is saving " +
                "lives and raising morale among the soldiers. With casualty rates decreasing, the Kingdom's commitment to the well-being " +
                "of its brave defenders is evident.\n\n" +
                "By leveraging surplus for the greater good, the Crown demonstrates both foresight and dedication to securing our " +
                "country's interests on the battlefield.");
            WorldText.News("Turn08_Decision_V_SurplusOfMedicine_SendToWar", null,
                "Sending surplus medical supplies to the troops in Pales and Zille may strengthen military morale, but it leaves some " +
                "citizens wondering why local healthcare wasn't prioritized. With clinics in Porte Drazon still facing shortages, it's " +
                "fair to ask whether a country fighting two wars at once has any medicine left for anyone else.\n\n" +
                "Boosting troop readiness is important, but does it always have to come at the expense of public health back home? The " +
                "decision may be strategic, but it leaves some questioning the balance between military needs and civilian care.\n\n" +
                "Instead of solely focusing on maintaining combat strength, shouldn't the Crown also ensure that ordinary citizens receive " +
                "the healthcare they deserve?");
            WorldText.Report("Turn08_PorteDrazon_Decision_SurplusOfMedicine_SendToWar", null,
                "The recent allocation of surplus medical supplies to the war effort in Pales and Zille has significantly improved " +
                "healthcare for Rizian soldiers on both fronts. Field hospitals report fewer casualties and faster recovery times, " +
                "allowing troops to return to duty more swiftly.\n\n" +
                "Military leaders commend the Crown's decision, noting that the improved medical support strengthens morale on the front " +
                "lines.");
            Journal("Turn08_Decision_SurplusOfMedicine_SendToWar", "I have decided to allocate surplus medicine to support our troops in Pales and Zille.");
        }
        else
        {
            WorldText.RestoreNews("Turn08_Decision_RH_SurplusOfMedicine_SendToWar");
            WorldText.RestoreNews("Turn08_Decision_V_SurplusOfMedicine_SendToWar");
            WorldText.RestoreReport("Turn08_PorteDrazon_Decision_SurplusOfMedicine_SendToWar");
            Journal("Turn08_Decision_SurplusOfMedicine_SendToWar", null);
        }
    }

    // ------------------------------------------------------------------ journal entries

    private static readonly Dictionary<string, string> OriginalJournal = new(StringComparer.Ordinal);

    /// <summary>Rewrites a vanilla journal entry, or puts the original back when <paramref name="text"/> is null.</summary>
    private static void Journal(string name, string text)
    {
        foreach (var list in new[] { EntityDataManager.journalEntriesData, EntityDataManager.JournalEntriesData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (JournalEntryData j in list)
            {
                JournalEntryProperties p = j?.JournalEntryProperties;
                if (p == null || j.NameInDatabase != name)
                {
                    continue;
                }

                if (!OriginalJournal.ContainsKey(name))
                {
                    OriginalJournal[name] = p.Description;
                }

                string wanted = text ?? OriginalJournal[name];
                if (p.Description != wanted)
                {
                    p.Description = wanted;
                }
            }
        }
    }
}
