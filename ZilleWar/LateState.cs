using HarmonyLib;
using Il2Cpp;
using SuzerainModdingKit;
using ConditionalInstruction = Il2Cpp.ConditionalInstructionProperties.ConditionalInstruction;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The war's outcome written into vanilla state, once per outcome, plus the standing rules for the rest of the game:
/// - Liberated: RiziaDLC.ZilleReturn_Success = true, Zille under the Crown (ZilleOwnership_RiziaImperii) until the
///   Turn 9 return speech lets the King give it to Brenas, the Reclaimed Territories authority line (+2), the AN
///   protocol and the Zille lease cleared, every referendum flag false, the world map switched (MapChange_Zille).
/// - Lost / ceasefire: ZilleReturn_Success stays false; the handover talks, the referendum and Smolak's "Zille for Pales"
///   call are gated off for every war game (see Register), so the vanilla "Zille lost" lines play with war wording.
/// - After any outcome Wehlen stays Hostile: its negotiating power is held at -6 or lower, vanilla's relation rule gets a
///   Hostile row, and relation updates to anything friendlier are turned into Hostile. Wehlen never sits at the
///   Intermerkopum table.
/// - The end summary gets a Zille War line with casualty figures, and the epilogue a war title.
/// </summary>
internal static class LateState
{
    public const string VarOutcomeApplied = "ZilleWar.Late_OutcomeApplied";   // "liberated" | "partial" | "lost"
    public const string VarByForce = "ZilleWar.Outcome_ByForce";              // Zille returned by the sword
    public const string VarZilleLost = "ZilleWar.Outcome_ZilleLost";          // the war ended with Zille city Wehzek
    public const string VarDivided = "ZilleWar.Outcome_ZilleDivided";         // the war ended with Zille city Rizian, the rest Wehzek
    public const string VarTitleRestorer = "ZilleWar.Title_Restorer";

    private const string Wehlen = "Rizia_Country_Wehlen";

    private const string HostileInstruction =
        "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"Hostile\");\n" +
        "RiziaDLC.CountryRelation_Wehlen_Allied = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Friendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Neutral = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Unfriendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Hostile = true;";

    private static readonly string OverCondition = WarFlags.X(WarFlags.Over);

    public static bool Declared => Variables.GetBool(WarFlags.Declared);
    public static bool Over => Declared && !WarFlow.AtWar;
    public static bool Liberated => Declared && ZilleCampaign.Liberated;
    public static bool CityHeld => Declared && Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
    public static bool Partial => Over && !ZilleCampaign.Liberated && CityHeld;
    public static bool Lost => Over && !ZilleCampaign.Liberated && !CityHeld;

    public static void Register()
    {
        foreach (string v in new[] { VarOutcomeApplied, VarByForce, VarZilleLost, VarDivided, VarTitleRestorer })
        {
            Variables.Register(v);
        }

        foreach (string v in new[] { VarByForce, VarZilleLost, VarDivided, VarTitleRestorer })
        {
            Vanilla.Expose(v);
        }

        // The vanilla handover pipeline never runs in a war game: no airship talks, no referendum, no Wehzek
        // co-belligerence in Pales, no mediation between Smolak and Derdia.
        Vanilla.Gate("Turn08_FPnT_ZilleNegotiations", WarFlags.XPeace);
        Vanilla.Gate("Turn9_FPnT_ZilleReferendum", WarFlags.XPeace);
        Vanilla.Gate("Turn9_FPnT_ZilleReferendumInterference", WarFlags.XPeace);
        Vanilla.Gate("Turn9_FPnT_CallWithSmolak", WarFlags.XPeace);
        Vanilla.Gate("Turn08_Decision_MediateDerdiaWehlen", WarFlags.XPeace);
        Vanilla.Gate("Turn10_Decision_PalesReconstruction_Annex_NoReform_Wehlen", WarFlags.XPeace);

        // A king who declared war is never "the Peaceful".
        foreach (string ci in new[] { "LateTraits_Turn08_Step08", "LateTraits_Turn09_Step015", "LateTraits_Turn10_Step018" })
        {
            Vanilla.GateInstruction(ci, "Trait_PacifistKing = true", WarFlags.XPeace);
        }

        RegisterNews();
    }

    // ------------------------------------------------------------------ every step

    public static void OnStep()
    {
        bool declared = Declared;
        OverrideTexts(declared);
        if (!declared)
        {
            return;
        }

        PatchRelationsInstruction();
        // War_WehlenJoined is never touched (house rule): only the gated Turn 9 Smolak call sets it.
        Lua("Variable[\"RiziaDLC.Trait_PacifistKing\"] = false; " +
            "Variable[\"RiziaDLC.Turn08_Wehlen_SmolakInvitedToWedding\"] = false; " +
            "Variable[\"RiziaDLC.Diplomacy_Wehlen_IntermerkopumPotential\"] = false; " +
            "Variable[\"RiziaDLC.Diplomacy_Wehlen_IntermerkopumApproved\"] = false; " +
            "Variable[\"RiziaDLC.Policy_Diplomacy_TradeDeal_Wehlen\"] = false; " +
            "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ImportTobacco\"] = false; " +
            "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ExportGas\"] = false; " +
            "Variable[\"RiziaDLC.Policy_Economy_WehlenPipeline_Project\"] = false; " +
            "Variable[\"RiziaDLC.ZilleReferendum_Approved\"] = false; " +
            "Variable[\"RiziaDLC.ZilleReferendum_Disapproved\"] = false; " +
            "Variable[\"RiziaDLC.ZilleReferendum_NotStoppedByAN\"] = false; " +
            "Variable[\"RiziaDLC.ZilleReferendum_SmolakOffer_Accepted\"] = false; " +
            "Variable[\"RiziaDLCText.IntermerkopumCountries\"] = \"Pales, Derdia and Morella\"; " +
            $"if Variable[\"{VarTitleRestorer}\"] == nil then Variable[\"{VarTitleRestorer}\"] = false end; " +
            $"if Variable[\"{VarByForce}\"] == nil then Variable[\"{VarByForce}\"] = false end; " +
            $"if Variable[\"{VarZilleLost}\"] == nil then Variable[\"{VarZilleLost}\"] = false end; " +
            $"if Variable[\"{VarDivided}\"] == nil then Variable[\"{VarDivided}\"] = false end;");

        if (Over)
        {
            KeepWehlenHostile();
            ApplyOutcome();
        }

        UpdateSummary();
        AddWarTitle();
    }

    private static void ApplyOutcome()
    {
        string applied = Variables.GetString(VarOutcomeApplied) ?? string.Empty;
        if (Liberated && applied != "liberated")
        {
            Variables.Set(VarOutcomeApplied, "liberated");
            Variables.Set(VarByForce, true);
            Variables.Set(VarZilleLost, false);
            Variables.Set(VarDivided, false);
            // The Crown governs Zille (military government) until the Turn 9 return speech, where the King may hand
            // it to Brenas. The speech's own scripts are adjusted so the authority line is not counted twice.
            Lua("Variable[\"RiziaDLC.ZilleReturn_Success\"] = true; " +
                // the Turn 7 war debate (EarlyT7) may already have decided who administers Zille
                "if Variable[\"ZilleWar.E7_ZilleAdmin\"] == \"brenas\" then Variable[\"RiziaDLC.ZilleOwnership_Brenas\"] = true; Variable[\"RiziaDLC.ZilleOwnership_RiziaImperii\"] = false " +
                "elseif Variable[\"RiziaDLC.ZilleOwnership_Brenas\"] ~= true then Variable[\"RiziaDLC.ZilleOwnership_RiziaImperii\"] = true end; " +
                "if (Variable[\"RiziaDLC.Authority_Modifier_TerritorialReclaimation\"] or 0) < 1 then " +
                "  if Variable[\"RiziaDLC.ZilleOwnership_Brenas\"] == true then Variable[\"RiziaDLC.Authority_Modifier_TerritorialReclaimation\"] = 1 " +
                "  else Variable[\"RiziaDLC.Authority_Modifier_TerritorialReclaimation\"] = 2 end end; " +
                "Variable[\"RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol\"] = false; " +
                "Variable[\"RiziaDLC.Policy_Diplomacy_ZLLAActive\"] = false; " +
                "Variable[\"RiziaDLC.ZilleReferendum_StoppedByAN\"] = false; " +
                "Variable[\"RiziaDLC.ZilleReferendum_Won\"] = false; " +
                "Variable[\"RiziaDLC.ZilleReferendum_Lost\"] = false; " +
                "Variable[\"RiziaDLC.Turn08_ZilleNeg_GiveSmolakEstelasHome\"] = false; " +
                "Variable[\"RiziaDLC.Situation_Law_WehlenRights_Full\"] = false; " +
                "Variable[\"RiziaDLC.Situation_Law_WehlenRights_WorkersOnly\"] = false; " +
                "EvaluateConditionalInstruction(\"MapChange_Zille\");");
            Core.Log.Msg("Zille liberated: ZilleReturn_Success set, Zille under the Crown, map switched.");
        }
        else if (!ZilleCampaign.Liberated && CityHeld && applied != "partial" && applied != "liberated")
        {
            Variables.Set(VarOutcomeApplied, "partial");
            Variables.Set(VarDivided, true);
            Variables.Set(VarZilleLost, false);
            Lua("Variable[\"RiziaDLC.ZilleReturn_Success\"] = false;");
            Core.Log.Msg("Zille war over with Zille city held: the region is divided.");
        }
        else if (!ZilleCampaign.Liberated && !CityHeld && applied != "lost" && applied != "liberated")
        {
            Variables.Set(VarOutcomeApplied, "lost");
            Variables.Set(VarZilleLost, true);
            Variables.Set(VarDivided, false);
            Lua("Variable[\"RiziaDLC.ZilleReturn_Success\"] = false;");
            Core.Log.Msg("Zille war lost: Zille stays Wehzek.");
        }
    }

    // ------------------------------------------------------------------ Wehlen stays hostile

    private static void KeepWehlenHostile()
    {
        if (Variables.GetFloat("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower") > -6f)
        {
            Variables.Set("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -6);
        }

        Lua("Variable[\"RiziaDLC.CountryRelation_Wehlen_Allied\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Friendly\"] = false; " +
            "Variable[\"RiziaDLC.CountryRelation_Wehlen_Neutral\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Unfriendly\"] = false; " +
            "Variable[\"RiziaDLC.CountryRelation_Wehlen_Hostile\"] = true; " +
            "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"Hostile\");");
    }

    /// <summary>Vanilla recomputes Wehlen's relation after every fragment; after the war the Hostile row wins.</summary>
    private static void PatchRelationsInstruction()
    {
        foreach (var list in new[] { EntityDataManager.conditionalInstructionData, EntityDataManager.ConditionalInstructionData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (ConditionalInstructionData c in list)
            {
                var rows = c?.NameInDatabase == "Relations_Wehlen" ? c.ConditionalInstructionProperties?.ConditionalInstructions : null;
                if (rows == null)
                {
                    continue;
                }

                bool patched = false;
                foreach (ConditionalInstruction r in rows)
                {
                    patched |= r?.Condition == OverCondition;
                }

                if (!patched)
                {
                    rows.Insert(0, new ConditionalInstruction { Condition = OverCondition, Instruction = HostileInstruction });
                    rows.Add(new ConditionalInstruction { Condition = OverCondition, Instruction = HostileInstruction });
                }
            }
        }
    }

    // ------------------------------------------------------------------ news and reports

    private static void RegisterNews()
    {
        Func<bool> war = () => Declared;
        Func<bool> cityOurs = () => Declared && (ZilleCampaign.Liberated || CityHeld);
        Func<bool> notReturned = () => Declared && !ZilleCampaign.Liberated;

        // peacetime Zille-under-Wehlen flavor that a war makes false
        foreach (string n in new[]
                 {
                     "RD_G_BuildMaveNortusIntelligenceHub", "RD_V_BuildMaveNortusIntelligenceHub",
                     "Turn08_RH_ZilleSupportBoycott", "Turn08_V_DerdianMercenariesRevealed",
                     "Turn09_G_DerdiaWehlenOil", "Turn09_G_PrayerCorridorBoth", "Turn09_G_PrayerCorridorDerdia", "Turn09_G_PrayerSeaRoute",
                 })
        {
            Vanilla.HideNews(n, war);
        }

        Vanilla.HideNews("Turn08_G_ZilleTensionsRise", cityOurs);
        Vanilla.HideNews("Turn09_RH_ZilleReturn_GiveToRiziaImperii", notReturned);

        foreach (string r in new[]
                 {
                     "Turn08_PorteDrazon_InovitzConcertCancelled", "Turn08_Zille_ATAOrganiseBoycott",
                     "Turn09_Nanta_DiscardedBallotsDiscovered", "Turn09_Wehlen_Decision_LeaveAllianceOfNations_Leave",
                     "Turn09_Wehlen_Decision_NationalDestabilization_Violence", "Turn09_Wehlen_IntermerkopumAccept",
                     "Turn09_Wehlen_IntermerkopumRefusal_NoZille", "Turn09_Wehlen_IntermerkopumRefusal_Zille",
                     "Construction_Wehlen_PipelineCompleted", "Turn09_MaveNortus_SmolakCall_WehlenJoinsWar", "Turn10_Wehlen_ZilleTransferred",
                 })
        {
            Vanilla.HideReport(r, war);
        }

        Vanilla.HideReport("Turn08_Zille_SOOfficialAssassination", cityOurs);
        Vanilla.HideReport("Turn08_Zille_IntelligenceHubActivated", cityOurs);
    }

    private sealed record TextSwap(bool IsNews, string Name, Func<bool> When, string Title, string Text);

    private static readonly List<TextSwap> Swaps = new()
    {
        new(true, "Turn08_G_LespiaEvidenceReleased", () => Declared, "Revelations Unveiled: Lespia Linked to Zille Bombing",
            "Rizia's Toras government has leveled serious allegations against Lespia in connection with the 1950 Friendship Day bombing in " +
            "Zille. Newly released evidence links the Alvarez administration to the men who built and planted the device, among them a " +
            "Lespian political officer at the consulate in Zille, Héctor Ramiro.\n\nThe bombing gave President Smolak his reason to keep " +
            "Zille past the end of the lease, and King Romus his reason to go to war for it. Prime Minister Alvarez called the evidence " +
            "\"a Wehzek fairy tale told by a Rizian king,\" but did not say which part of it was untrue."),
        new(true, "Turn09_RH_ZilleReturnSpeech_Negociations", () => Liberated, "Zille Reunites with Rizia: A Triumph of Arms",
            "After twenty-five years under a foreign flag, Zille has formally rejoined the Kingdom. The ceremony in the city's main " +
            "square, its Wehzek name already gone from the street signs, sealed what the Royal Army won in the field.\n\nHis Majesty " +
            "honored the soldiers who fell between the river and the sea, and thousands of Zilleans cheered the King who would not " +
            "wait for Wehlen's permission to bring them home."),
        new(false, "Turn09_Nanta_StreetFestival", () => Liberated, "Nanta Holds Its First Festival Since The Shelling",
            "A hastily organized street festival filled Nanta with music for two days, the first since the fighting reached the city. " +
            "Rizian and Wehzek families shared the same tables, if not always the same songs, and the harbor district hung lanterns " +
            "from windows that still had no glass."),
        new(false, "Turn10_Report_NewGov_SmolakReef", () => Liberated, "Smolak Reef Initiative",
            "The decision to remove dozens of Smolak's statues and sink them in the Gulf of Moridia as artificial reefs has met wide " +
            "approval from the Zillean public, though the Wehzek minority in Nanta called it a victor's vandalism. Fishermen in Zille " +
            "have already started calling the spot Wiktor's Rest."),
    };

    private static readonly Dictionary<string, (string title, string text)> Originals = new(StringComparer.Ordinal);

    private static void OverrideTexts(bool declared)
    {
        foreach (TextSwap s in Swaps)
        {
            bool on = declared && Safe(s.When);
            if (s.IsNews)
            {
                foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
                {
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (NewsData n in list)
                    {
                        if (n?.NameInDatabase == s.Name && n.NewsProperties != null)
                        {
                            Apply("n:" + s.Name, on, s, () => (n.NewsProperties.Title, n.NewsProperties.Description),
                                (t, d) => { n.NewsProperties.Title = t; n.NewsProperties.Description = d; });
                        }
                    }
                }
            }
            else
            {
                foreach (var list in new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData })
                {
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (ReportData r in list)
                    {
                        if (r?.NameInDatabase == s.Name && r.ReportProperties != null)
                        {
                            Apply("r:" + s.Name, on, s, () => (r.ReportProperties.Title, r.ReportProperties.Description),
                                (t, d) => { r.ReportProperties.Title = t; r.ReportProperties.Description = d; });
                        }
                    }
                }
            }
        }
    }

    private static void Apply(string key, bool on, TextSwap s, Func<(string, string)> get, Action<string, string> set)
    {
        (string title, string text) = get();
        if (on)
        {
            if (text != s.Text)
            {
                Originals.TryAdd(key, (title, text));
                set(s.Title, s.Text);
            }
        }
        else if (text == s.Text && Originals.TryGetValue(key, out var o))
        {
            set(o.title, o.text);
        }
    }

    // ------------------------------------------------------------------ end summary and epilogue title

    private const string SummaryLib = "ZilleWar.War_Zille_Liberated == true";
    private static readonly string SummaryPart = $"{WarFlags.X(WarFlags.Over)} && {WarFlags.X(WarFlags.Liberated, false)} && {WarFlags.X(WarFlags.ZilleCityHeld)}";
    private static readonly string SummaryLost = $"{WarFlags.X(WarFlags.Over)} && {WarFlags.X(WarFlags.Liberated, false)} && {WarFlags.X(WarFlags.ZilleCityHeld, false)}";
    private static readonly string SummaryOngoing = WarFlags.X(WarFlags.Active);

    /// <summary>Estimated men killed, wounded or missing over all operations (same estimate as the after-action reports).</summary>
    public static (int ours, int theirs) Casualties()
    {
        int ours = 0, theirs = 0;
        for (int k = 1; k <= ZilleCampaign.MaxOperations; k++)
        {
            int p = Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)), e = Variables.GetInt(ZilleCampaign.EnemyDeadVar(k));
            ours += p <= 0 ? 0 : p * 850 + (p * 137 + 7 * 31) % 300;
            theirs += e <= 0 ? 0 : e * 850 + (e * 137 + 13 * 31) % 300;
        }

        return (Math.Max(ours, 312), Math.Max(theirs, 287));
    }

    private static void UpdateSummary()
    {
        (int ours, int theirs) = Casualties();
        string o = ours.ToString("N0"), t = theirs.ToString("N0");
        var lines = new (string cond, string text)[]
        {
            (SummaryLib, $"Zille returned to Rizia by force of arms after twenty-five years of Wehzek rule. {o} Rizian and {t} Wehzek soldiers were killed, wounded or went missing in the Zille War."),
            (SummaryPart, $"The Zille War left the region divided along a ceasefire line, with Zille city in Rizian hands. {o} Rizian and {t} Wehzek soldiers were killed, wounded or went missing."),
            (SummaryLost, $"{o} Rizian soldiers were killed, wounded or went missing in the failed war for Zille, and Wehlen tightened its hold on the region."),
            (SummaryOngoing, $"The Zille War was still being fought. {o} Rizian and {t} Wehzek soldiers had already been killed, wounded or gone missing."),
        };

        foreach (var list in new[] { EntityDataManager.summarySegmentData, EntityDataManager.SummarySegmentData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (SummarySegmentData d in list)
            {
                var rows = d?.NameInDatabase == "Rizia_SummarySegment_2" ? d.SummarySegmentProperties?.SegmentConditions : null;
                if (rows == null)
                {
                    continue;
                }

                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    SummarySegmentProperties.SummarySegmentCondition row = null;
                    foreach (SummarySegmentProperties.SummarySegmentCondition r in rows)
                    {
                        if (r?.Condition == lines[i].cond)
                        {
                            row = r;
                        }
                    }

                    if (row == null)
                    {
                        row = new SummarySegmentProperties.SummarySegmentCondition { Condition = lines[i].cond };
                        rows.Insert(0, row);
                    }

                    row.Text = lines[i].text;
                }
            }
        }
    }

    private static readonly string TitleCondition = WarFlags.X(VarTitleRestorer);

    private static void AddWarTitle()
    {
        foreach (var list in new[] { EntityDataManager.compassTitleData, EntityDataManager.CompassTitleData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (CompassTitleData d in list)
            {
                var rows = d?.NameInDatabase == "Rizia_Compass_Title" ? d.CompassTitleProperties?.TitleConditions : null;
                if (rows == null)
                {
                    continue;
                }

                bool has = false;
                foreach (CompassTitleProperties.CompassTitleCondition r in rows)
                {
                    has |= r?.Condition == TitleCondition;
                }

                if (!has)
                {
                    rows.Insert(Math.Min(3, rows.Count), new CompassTitleProperties.CompassTitleCondition { Text = "the Restorer", Condition = TitleCondition });
                }
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    public static void Lua(string code)
    {
        try
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(LuaFix.Rewrite(code));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Late Lua failed: {ex.Message}");
        }
    }

    private static bool Safe(Func<bool> f)
    {
        try
        {
            return f();
        }
        catch
        {
            return false;
        }
    }

    public static int Turn => GameState.CurrentTurnNum ?? 0;
    public static int Step => GameState.CurrentStepNum ?? 0;
}

/// <summary>After the war, any friendlier relation with Wehlen that a script asks for becomes Hostile.</summary>
[HarmonyPatch(typeof(TokenInformationPanel), nameof(TokenInformationPanel.UpdateCountryRelationship))]
internal static class Patch_WehlenStaysHostile
{
    private static void Prefix(string countryNameInDatabase, ref string relationship)
    {
        if (countryNameInDatabase == "Rizia_Country_Wehlen" && relationship is "Allied" or "Friendly" or "Neutral" or "Unfriendly"
            && Variables.GetBool(WarFlags.Declared) && !WarState.Active && !Variables.GetBool(WarState.VarActive))
        {
            relationship = "Hostile";
        }
    }
}
