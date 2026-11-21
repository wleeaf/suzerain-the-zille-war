using SuzerainModdingKit;
using SuzerainModdingKit.Report;
using SuzerainModdingKit.StoryFragments.Decision;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// The war between the declaration and the war table: the mobilization report, the War Council where
/// the King sets the first operation's objective, and the "next steps" council after each operation
/// (one operation per story turn, like vanilla's Pales phases). Interim decisions: the full War Council
/// conversation replaces the first one in the story pass.
/// </summary>
internal static class WarFlow
{
    private const string CouncilDecision = "ZilleWar.Decision_WarCouncil";
    private const string NextStepsDecision = "ZilleWar.Decision_NextSteps";
    public const string VarEndingShown = "ZilleWar.War_EndingShown";
    private const string Ceasefire = "Seek a ceasefire through the Alliance of Nations. We hold what we have taken.";

    public const string VarCouncilHeld = "ZilleWar.WarCouncilHeld";
    public const string VarMobilizationShown = "ZilleWar.MobilizationReportShown";
    public const string VarLastOperationTurn = "ZilleWar.LastOperationTurn";
    public const string VarOperationReady = "ZilleWar.OperationReady";
    public const string VarCeasefire = "ZilleWar.War_Zille_Ceasefire";

    private static readonly Dictionary<string, Objective> OptionToObjective = new();

    public static void Register()
    {
        foreach (string v in new[] { VarCouncilHeld, VarMobilizationShown, VarLastOperationTurn, VarOperationReady, VarCeasefire, VarEndingShown })
        {
            Variables.Register(v);
        }

        Events.OnEvaluateStep += (_, _) => OnEvaluateStep();
        Events.OnDecisionShow += (_, _) => OnDecisionShow();
        Events.OnDecisionFinished += (_, e) => OnDecisionFinished(e.SelectedOptionInfo);
    }

    public static bool AtWar => Variables.GetBool(AnDeclaration.VarDeclared) && !ZilleCampaign.Liberated && !ZilleCampaign.Defeated
                                && !Variables.GetBool(VarCeasefire);

    private static void OnEvaluateStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        try
        {
            WarState.Apply(AtWar);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WarState failed: {ex}");
        }

        if (!AtWar && Variables.GetBool(AnDeclaration.VarDeclared) && !Variables.GetBool(VarEndingShown))
        {
            Variables.Set(VarEndingShown, true);
            NewsWire.PublishWarEnd(ZilleCampaign.Liberated ? "victory" : ZilleCampaign.Defeated ? "defeat" : "ceasefire");
            // The war is over; vanilla's relation rule takes Wehlen from here, and it will not be friendly.
            Variables.Set("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", Math.Min(-6, Variables.GetInt("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower")));
        }

        if (!AtWar || GameState.CurrentStepNum is null or < 2 && GameState.CurrentTurnNum == 1)
        {
            return;
        }

        try
        {
            if (!Variables.GetBool(VarMobilizationShown))
            {
                Variables.Set(VarMobilizationShown, true);
                SupplyWar.OnWarDeclared();
                NewsWire.PublishDeclaration();
            }

            if (Variables.GetBool(VarCouncilHeld))
            {
                SupplyWar.Offer();
            }

            int turn = GameState.CurrentTurnNum ?? 0;
            bool operationPending = Variables.GetBool(VarOperationReady);
            if (operationPending || Variables.GetInt(VarLastOperationTurn) >= turn)
            {
                return;     // one operation per story turn
            }

            bool firstCouncil = !Variables.GetBool(VarCouncilHeld);
            string name = firstCouncil ? CouncilDecision : NextStepsDecision;
            if (GameState.StoryFragmentExistsInCurrentStep(name) || ZilleCampaign.Available().Count == 0)
            {
                return;
            }

            ObjectiveInfo next = ZilleCampaign.Info(ZilleCampaign.NextObjective);
            CustomDecisionFragment decision = firstCouncil
                ? new CustomDecisionFragment(name, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                    "The Zille War Council",
                    "The map table in the Council Chambers has been cleared of everything but Zille. Generals and admirals stand " +
                    "around it in silence. Wehlen's border wall runs along the river line from Mave Nortus to the Gulf of Moridia. " +
                    "General Azaro lays his hand on the coast. \"Zille first, Your Majesty. The city, the harbor, and the Wehzek " +
                    "headquarters with them. Take Zille, and their whole southern front is cut off from the sea. Then Auremina and " +
                    "the hills. Then the northern ports, Isor and Nanta, and the war is over.\" " +
                    $"{next.Codename} is ready: {next.Aim}",
                    "The Zille War Council", "The General Staff awaits the King's orders.")
                : new CustomDecisionFragment(name, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                    "The War in Zille: Next Steps",
                    $"The General Staff has updated the map. {Situation()} The next phase of the plan is {next.Codename}: {next.Aim}",
                    "The War in Zille", "The General Staff awaits orders for the next operation.");
            GameState.AddCustomStoryFragment(decision);
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WarFlow step failed: {ex}");
        }
    }

    private static string Situation()
    {
        Dictionary<string, bool> o = ZilleCampaign.CurrentOwners();
        var held = ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "zille" && t.Tile != null && o.TryGetValue(t.Tile, out bool p) && p)
            .Select(t => t.Name).ToList();
        var lost = ZilleData.Towns.Where(t => t.Role == "city" && t.Side == "rizia" && t.Tile != null && o.TryGetValue(t.Tile, out bool p) && !p)
            .Select(t => t.Name).ToList();
        string s = held.Count == 0 ? "No Zille city is in our hands yet." : $"Our flag flies over {string.Join(", ", held)}.";
        if (lost.Count > 0)
        {
            s += $" The enemy still holds {string.Join(", ", lost)} on our side of the border.";
        }

        return s + $" Intelligence rates the Wehzek garrison as {ZilleCampaign.Tier.ToLowerInvariant()}.";
    }

    private static void OnDecisionShow()
    {
        string name = DecisionManager.CurrentDecisionName;
        if (name != CouncilDecision && name != NextStepsDecision)
        {
            return;
        }

        OptionToObjective.Clear();
        foreach (Objective o in ZilleCampaign.Available())
        {
            ObjectiveInfo info = ZilleCampaign.Info(o);
            string text = $"Give the order. {info.Codename} begins at dawn.";
            OptionToObjective[text] = o;
            DecisionManager.AddOption(text);
        }

        if (name == NextStepsDecision)
        {
            DecisionManager.AddOption(Ceasefire);
        }
    }

    private static void OnDecisionFinished(DecisionOptionInfo selected)
    {
        if (selected == null || (selected.DecisionName != CouncilDecision && selected.DecisionName != NextStepsDecision))
        {
            return;
        }

        if (selected.Text == Ceasefire)
        {
            Variables.Set(VarCeasefire, true);
            Core.Log.Msg("The King seeks a ceasefire in Zille.");
            return;
        }

        if (!OptionToObjective.TryGetValue(selected.Text, out Objective objective))
        {
            return;
        }

        ZilleCampaign.NextObjective = objective;
        Variables.Set(VarCouncilHeld, true);
        Variables.Set(VarLastOperationTurn, GameState.CurrentTurnNum ?? 0);
        if (ZilleOperation.BuildNext(out var frag))
        {
            Variables.Set(VarOperationReady, true);
            WarProbe.InjectWarFragment(frag);
        }
    }

    /// <summary>Called when one of our operations finishes.</summary>
    public static void OnOperationFinished() => Variables.Set(VarOperationReady, false);
}
