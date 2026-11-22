using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// What the declaration of war ends in vanilla's bookkeeping, so the rest of the game reads "the talks with Wehlen
/// failed, and everything we had with Wehlen is over":
/// - <see cref="DeclarationLua"/> runs in the declaration node at the AN (and on the next step if it did not run, e.g.
///   older saves): Diplomacy_Wehlen_AgreementRefused, a copy of vanilla's WehlenTrade_End_* scripts, Wehlen out of
///   Intermerkopum, the 1927 agreement void, no anti-BFF partnership, no promises to Smolak.
/// - The Turn 3 diplomatic mission to Wraclawitz is recalled (its progress bar and its "strengthened" badge).
/// - The vanilla Zille occupation reports of Turns 4 to 7 (NarrativeSupport_Zille_Part1/2) that assume peace are switched
///   off; EarlyT4/T5 file wartime ones.
/// - The Zille resistance funding decisions (T5, T7) are reworded for a war and pay off on the war table.
/// </summary>
internal static class EarlyBookkeeping
{
    public const string VarDone = "ZilleWar.E_DeclarationBookkept";

    /// <summary>Lua run once when the war is declared. Never sets War_Happened or War_WehlenJoined.</summary>
    public const string DeclarationLua =
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_AgreementRefused\"] = true; " +
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_FairAgreement\"] = false; " +
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_LopsidedAgreement\"] = false; " +
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_PromiseRicoExtradition\"] = false; " +
        // vanilla WehlenTrade_End_Turn8_Step17, row 2
        "Variable[\"RiziaDLC.Policy_Diplomacy_TradeDeal_Wehlen\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ImportDrugs\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_OilImport\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ImportTobacco\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ImportTimber\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ExportArms\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ExportGas\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ExportGold\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_ExportWine\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Wehlen_Terrorism_AntiBFF\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishBan\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishDerdianBan\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Law_WehlenRights_Full\"] = false; " +
        "Variable[\"RiziaDLC.Situation_Law_WehlenRights_WorkersOnly\"] = false; " +
        "Variable[\"RiziaDLC.Policy_Economy_WehlenPipeline_Completed\"] = false; " +
        "Variable[\"RiziaDLC.ReminderPanel_WehlenNegotiationPromises\"] = false; " +
        // Intermerkopum, the lease, the envoys
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_IntermerkopumPotential\"] = false; " +
        "Variable[\"RiziaDLC.Diplomacy_Wehlen_IntermerkopumApproved\"] = false; " +
        "Variable[\"RiziaDLC.Policy_Diplomacy_ZLLAActive\"] = false; " +
        $"Variable[\"{VarDone}\"] = true;";
    // The envoys' tokens are removed on the next story step (OnStep), outside the declaration node's script.

    // ---- the resistance funding decisions

    private const string Funding5 = "Turn05_Decision_ZilleResistanceFunding";
    private const string Funding7 = "Turn07_Decision_SecondZilleResistanceFunding";

    private sealed class Rewrite
    {
        public string Description;
        public string[] Options;    // same order as vanilla's options
    }

    private static readonly Dictionary<string, Rewrite> WarText = new()
    {
        [Funding5] = new Rewrite
        {
            Description =
                "Our army is fighting in Zille, and the Ariana Toras Association and the Zille branch of Su Omina are still at work " +
                "behind the Wehzek lines. Their help has a new value now. Su Omina's cells can cut rail lines and telephone wires ahead " +
                "of our next operation, while the Association can hide our wounded, guide our patrols and keep families off the roads. " +
                "Arming either of them in the middle of a war will not go unnoticed abroad, and whatever they do, they will do in our name.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association's shelters and guides (-1 Budget)",
                "Fund the Ariana Toras Association to shelter civilians and guide our patrols (-2 Budget)",
                "Arm the Su Omina cells to sabotage Wehzek lines (-1 Budget, -500 Military Equipment)",
                "Arm a Su Omina branch in Zille to sabotage Wehzek lines (-2 Budget, -250 Military Equipment)",
                "Fund both organizations behind the front (-2 Authority, -4 Budget, -500 Military Equipment)",
                "Let the army do its work",
            },
        },
        [Funding7] = new Rewrite
        {
            Description =
                "The war in Zille has turned the resistance movements into something else: guides, informers, saboteurs and nurses " +
                "working behind the Wehzek lines. Their leaders are asking for more money and more rifles. More support could shorten " +
                "the fighting, but every reprisal against a village that hid our partisans will be laid at the Crown's door.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association's shelters and guides (-2 Budget)",
                "Fund the Ariana Toras Association to shelter civilians and guide our patrols (-2 Budget)",
                "Arm the Su Omina cells for a new sabotage campaign (-2 Budget, -1000 Military Equipment)",
                "Arm a Su Omina branch in Zille to sabotage Wehzek lines (-4 Budget, -500 Military Equipment)",
                "Fund both organizations behind the front (-2 Authority, -4 Budget, -1000 Military Equipment)",
                "Let the army do its work",
            },
        },
    };

    /// <summary>After a lost war or a ceasefire line: the resistance goes underground in a Zille that stayed Wehzek.</summary>
    private static readonly Dictionary<string, Rewrite> LostText = new()
    {
        [Funding5] = new Rewrite
        {
            Description =
                "The guns have fallen silent in Zille, but the region is still in Wehzek hands. Wehlen's security police are hunting " +
                "everyone who helped our soldiers, and the Ariana Toras Association and the Zille branch of Su Omina have gone " +
                "underground. Without our money they will be broken within months. With it, they keep the claim alive, and give " +
                "Wiktor Smolak every reason to tighten his grip on the region.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association underground (-1 Budget)",
                "Fund the Ariana Toras Association to hide those who helped us (-2 Budget)",
                "Keep the Su Omina cells armed and waiting (-1 Budget, -500 Military Equipment)",
                "Arm a Su Omina branch in occupied Zille (-2 Budget, -250 Military Equipment)",
                "Fund both organizations underground (-2 Authority, -4 Budget, -500 Military Equipment)",
                "Fund neither organization",
            },
        },
        [Funding7] = new Rewrite
        {
            Description =
                "Since the fighting ended, Wehlen has filled Zille's prisons with anyone suspected of helping our army. What is left of " +
                "the resistance asks for money to keep its people hidden and its networks alive. Supporting them keeps the question of " +
                "Zille open; it also hands Wehlen proof that Rizia has not given up on taking the region by force.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association underground (-2 Budget)",
                "Fund the Ariana Toras Association to hide those who helped us (-2 Budget)",
                "Keep the Su Omina cells armed and waiting (-2 Budget, -1000 Military Equipment)",
                "Arm a Su Omina branch in occupied Zille (-4 Budget, -500 Military Equipment)",
                "Fund both organizations underground (-2 Authority, -4 Budget, -1000 Military Equipment)",
                "Fund none",
            },
        },
    };

    /// <summary>After an armistice that left Zille city Rizian: the resistance lives on behind the line, in Isor and Nanta.</summary>
    private static readonly Dictionary<string, Rewrite> DividedText = new()
    {
        [Funding5] = new Rewrite
        {
            Description =
                "The guns have fallen silent along the new line in Zille. The city and its harbor are ours, but beyond the wire Wehlen's " +
                "security police are hunting everyone who helped our soldiers. The Ariana Toras Association and the Zille branch of " +
                "Su Omina have gone underground in Isor and Nanta. Without our money they will be broken within months. With it, they " +
                "keep the rest of the claim alive, and give Wiktor Smolak every reason to fortify the line.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association beyond the line (-1 Budget)",
                "Fund the Ariana Toras Association to hide those who helped us (-2 Budget)",
                "Keep the Su Omina cells armed and waiting (-1 Budget, -500 Military Equipment)",
                "Arm a Su Omina branch beyond the line (-2 Budget, -250 Military Equipment)",
                "Fund both organizations beyond the line (-2 Authority, -4 Budget, -500 Military Equipment)",
                "Fund neither organization",
            },
        },
        [Funding7] = new Rewrite
        {
            Description =
                "Since the armistice, Wehlen has filled the prisons of Isor and Nanta with anyone suspected of helping our army, and " +
                "families who crossed into our half of Zille cannot go back. What is left of the resistance on the far side of the line " +
                "asks for money to keep its people hidden and its networks alive. Supporting them keeps the rest of Zille's question " +
                "open; it also hands Wehlen proof that Rizia means to move the line again.",
            Options = new[]
            {
                "Increase funding to the Ariana Toras Association beyond the line (-2 Budget)",
                "Fund the Ariana Toras Association to hide those who helped us (-2 Budget)",
                "Keep the Su Omina cells armed and waiting (-2 Budget, -1000 Military Equipment)",
                "Arm a Su Omina branch beyond the line (-4 Budget, -500 Military Equipment)",
                "Fund both organizations beyond the line (-2 Authority, -4 Budget, -1000 Military Equipment)",
                "Fund none",
            },
        },
    };

    private static readonly Dictionary<string, (string desc, string[] options)> Original = new(StringComparer.Ordinal);

    public static void Register()
    {
        Variables.Register(VarDone);

        // Peacetime Zille occupation reports that fire on a timer.
        // T4 step 8: "officials have guaranteed Rizia that the measures are merely intended to increase the safety..."
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn04_Zille_LargeGatheringBan", WarFlags.XPeace);
        // T5 step 2: fires because the declaration sets AgreementRefused; segregation reads oddly under a shooting war.
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn05_Zille_WehzekCitizensPriviliged", WarFlags.XPeace);
        // T5 step 3: WHR "refrained from condemning the delay of the region's return".
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "News_Turn05_G_WatchtowerConcernedZille", WarFlags.XPeace);
        // T6 step 1: Wehlen paying settlers to move into a war zone.
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn06_Zille_WehlenEncouragesMigration", WarFlags.XPeace);
        // T6 step 4/5, T7 step 2/3/4: resistance reports written for an occupation (posters, failed raids, a waning ATA).
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn06_Zille_ATAFlyerCampaign", WarFlags.XPeace);
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn06_Zille_SORailwayAttackFail", WarFlags.XPeace);
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn07_ATAAssembliesForm", WarFlags.XPeace);
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part2", "Reports_Turn07_Zille_ATAActivityWaning", WarFlags.XPeace);
        // Sabotage behind Wehzek lines still fits while Wehlen holds the region; not once it is ours.
        string notOurs = $"{WarFlags.X(WarFlags.Liberated, false)} && {WarFlags.X(WarFlags.ZilleCityHeld, false)}";
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part1", "Reports_Turn06_Zille_SORailwayAttack = true", notOurs);
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part2", "Reports_Turn07_Zille_SOHarborExplosion", notOurs);
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part2", "Reports_Turn07_Zille_DerdianMercenaries", WarFlags.X(WarFlags.Liberated, false));
        Vanilla.GateInstruction("NarrativeSupport_Zille_Part2", "Reports_Turn07_MaveNortus_IntelligenceInsertion", notOurs);

        // Funding the resistance makes no sense once Zille is ours.
        Vanilla.Gate(Funding5, WarFlags.X(WarFlags.Liberated, false));
        Vanilla.Gate(Funding7, WarFlags.X(WarFlags.Liberated, false));
        Events.OnDecisionFinished += (_, e) => OnFundingChosen(e.SelectedOptionInfo);
    }

    public static void OnStep()
    {
        bool declared = Variables.GetBool(WarFlags.Declared);
        if (declared && !Variables.GetBool(VarDone))
        {
            Lua(DeclarationLua);
            Core.Log.Msg("Declaration bookkeeping applied (Wehlen trade, Intermerkopum, envoys, the 1927 agreement).");
        }

        // The Turn 3 mission completes on a schedule; if it finishes after the declaration, recall it again.
        if (declared && (GameState.CurrentTurnNum ?? 0) <= 7 && Variables.GetBool("RiziaDLC.Decision_Turn03_DiplomaticEnvoy_Wehlen"))
        {
            Lua("RemoveTokenStatusImmediately(\"Rizia_Country_Wehlen\", \"Progress_RiziaDiplomaticMission_Wehlen\")");
            Lua("RemoveTokenStatusImmediately(\"Rizia_Country_Wehlen\", \"Country_Wehlen_DiplomaticMissionStrengthened\")");
        }

        RewriteFundingDecisions(!declared || Variables.GetBool(WarFlags.Liberated) ? null
            : Variables.GetBool(WarFlags.Active) ? WarText : Variables.GetBool(WarFlags.ZilleCityHeld) ? DividedText : LostText);
    }

    private static void RewriteFundingDecisions(Dictionary<string, Rewrite> texts)
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
                if (name == null || p?.Options == null || !WarText.TryGetValue(name, out Rewrite w) || p.Options.Count != w.Options.Length)
                {
                    continue;
                }

                if (!Original.ContainsKey(name))
                {
                    var vanillaTexts = new string[p.Options.Count];
                    for (int i = 0; i < vanillaTexts.Length; i++)
                    {
                        vanillaTexts[i] = p.Options[i].Text;
                    }

                    Original[name] = (p.Description, vanillaTexts);
                }

                (string desc, string[] options) = texts != null ? (texts[name].Description, texts[name].Options) : Original[name];
                p.Description = desc;
                for (int i = 0; i < options.Length; i++)
                {
                    p.Options[i].Text = options[i];
                }
            }
        }
    }

    /// <summary>Su Omina sabotage weakens the garrison before the next operation; the ATA's shelters make the news.</summary>
    private static void OnFundingChosen(SuzerainModdingKit.StoryFragments.Decision.DecisionOptionInfo o)
    {
        if (o == null || !WarText.TryGetValue(o.DecisionName ?? string.Empty, out Rewrite w))
        {
            return;
        }

        bool afterWar = Variables.GetBool(WarFlags.Declared) && !Variables.GetBool(WarFlags.Active) && !Variables.GetBool(WarFlags.Liberated);
        if (afterWar)
        {
            // The war is over and Zille (or its hinterland) is still Wehzek: the money keeps the networks alive underground.
            var after = Variables.GetBool(WarFlags.ZilleCityHeld) ? DividedText : LostText;
            int j = Array.IndexOf(after[o.DecisionName].Options, o.Text);
            if (j is >= 0 and <= 4)
            {
                int t = GameState.CurrentTurnNum ?? 0;
                NewsWire.Report($"E_Underground_T{t}", SuzerainTokenName.RiziaEventZille, "Zille's Resistance Survives on Crown Money",
                    "The networks that helped our army in Zille have not been rolled up. Shelters run by the Ariana Toras Association and " +
                    "Su Omina cells waiting in the villages are still receiving money from the Crown, passed through intermediaries in " +
                    "Mave Nortus. Wehzek security police have made arrests in the region, but the General Staff believes the networks " +
                    "will be intact if the line is ever tested again.");
            }

            return;
        }

        if (!Variables.GetBool(WarFlags.Active))
        {
            return;
        }

        int i = Array.IndexOf(w.Options, o.Text);
        if (i < 0)
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0;
        bool so = i is 2 or 3 or 4;
        bool ata = i is 0 or 1 or 4;
        if (so)
        {
            ZilleCampaign.Strength -= 1;
            NewsWire.Report($"E_SOSabotage_T{turn}", SuzerainTokenName.RiziaEventZille, "Su Omina Cells Strike Behind Wehzek Lines",
                "Within days of receiving new funds and weapons, Su Omina cells in the Zille region blew up a stretch of the railway " +
                "between Zille and Nanta and cut the field telephone lines of at least two Wehzek battalions. Wehzek military police " +
                "have answered with house searches and curfews in the villages along the line, and several men of fighting age have " +
                "been taken away for questioning. The General Staff expects the garrison to be slower to reinforce its positions ahead " +
                "of our next operation.");
        }

        if (ata)
        {
            NewsWire.Report($"E_ATAShelters_T{turn}", SuzerainTokenName.RiziaEventZille, "Ariana Toras Association Opens Shelters in Zille",
                "Volunteers of the Ariana Toras Association have opened cellars, sanctuary halls and school basements across the Zille " +
                "region to families caught near the fighting. Association members are also guiding Royal Army patrols along back roads " +
                "and passing on what they see of Wehzek movements. \"We are not soldiers,\" one volunteer said. \"But we know every " +
                "lane and every family here, and that is worth something.\"");
        }
    }

    private static void Lua(string code)
    {
        try
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(LuaFix.Rewrite(code));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Lua failed ({code.Substring(0, Math.Min(60, code.Length))}...): {ex.Message}");
        }
    }
}
