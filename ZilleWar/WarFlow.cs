using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// The war between the declaration and the end: runs every module's per-step work in order, publishes the
/// mobilization and the end of the war, offers the supply-war calls, and hands over to the War Council
/// (<see cref="WarCouncil"/>), which holds the council and opens the war table once everything that belongs
/// before the operation has been played. One operation per story turn.
/// </summary>
internal static class WarFlow
{
    public const string VarEndingShown = "ZilleWar.War_EndingShown";
    public const string VarCouncilHeld = "ZilleWar.WarCouncilHeld";
    public const string VarMobilizationShown = "ZilleWar.MobilizationReportShown";
    public const string VarLastOperationTurn = "ZilleWar.LastOperationTurn";
    public const string VarOperationReady = "ZilleWar.OperationReady";
    public const string VarCeasefire = "ZilleWar.War_Zille_Ceasefire";

    public static void Register()
    {
        foreach (string v in new[] { VarCouncilHeld, VarMobilizationShown, VarLastOperationTurn, VarOperationReady, VarCeasefire, VarEndingShown })
        {
            Variables.Register(v);
        }

        Events.OnEvaluateStep += (_, _) => OnEvaluateStep();
    }

    public static bool AtWar => Variables.GetBool(AnDeclaration.VarDeclared) && !ZilleCampaign.Liberated && !ZilleCampaign.Defeated
                                && !Variables.GetBool(VarCeasefire);

    private static void OnEvaluateStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        // The war is over by Turn 6 at the latest (Phase III is its first scene); never let it run on.
        int turnNow = GameState.CurrentTurnNum ?? 0, stepNow = GameState.CurrentStepNum ?? 0;
        if (AtWar && (turnNow >= 7 || (turnNow == 6 && stepNow >= 6)))
        {
            Run("force the war's end", () => ZilleCampaign.ForceEnd($"at turn {turnNow} step {stepNow}"));
        }

        foreach ((string what, Action step) in new (string, Action)[]
                 {
                     ("war flags", WarFlags.Sync), ("war decrees", WarDecrees.OnStep), ("vanilla edits", Vanilla.Apply), ("lua check", LuaLint.RunOnce), ("turns 4-7", VanillaEarly.OnStep),
                     ("turns 8-10", VanillaLate.OnStep), ("world", WorldSystems.OnStep),
                     ("sweep 4-6", SweepEarly.OnStep), ("sweep 7-8", SweepMiddle.OnStep), ("sweep 9-10", SweepLate.OnStep), ("audit", Audit.OnStep), ("strategic map", StrategicMap.OnStep), ("finish", Finish.OnStep),
                 })
        {
            Run(what, step);
        }

        Run("war state", () => WarState.Apply(AtWar));
        if (Variables.GetBool(AnDeclaration.VarDeclared))
        {
            Run("operation reports", ZilleReports.Restore);
        }

        if (!AtWar && Variables.GetBool(AnDeclaration.VarDeclared) && !Variables.GetBool(VarEndingShown))
        {
            Variables.Set(VarEndingShown, true);
            Run("war end news", () => NewsWire.Batch(() => NewsWire.PublishWarEnd(ZilleCampaign.Liberated ? "victory"
                : ZilleCampaign.Defeated ? (Variables.GetBool(ZilleOperation.CapturedVar("Zille")) ? "divided" : "defeat") : "ceasefire")));
            Run("close the operation", WarSaveSlot.EndZilleOperation);
            Run("victory opinion", VictoryParade.OnWarEnded);
            // The war is over; vanilla's relation rule takes Wehlen from here, and it will not be friendly.
            Variables.Set("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", Math.Min(-6, Variables.GetInt("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower")));
        }

        Run("victory parade", VictoryParade.OnStep);
        if (!AtWar || (GameState.CurrentTurnNum == 1 && GameState.CurrentStepNum is null or < 2))
        {
            return;
        }

        Run("mobilization", () =>
        {
            if (!Variables.GetBool(VarMobilizationShown))
            {
                Variables.Set(VarMobilizationShown, true);
                SupplyWar.OnWarDeclared();
                NewsWire.Batch(NewsWire.PublishDeclaration);
            }
        });
        Run("supply war", () =>
        {
            SupplyWar.ReportNewLevers();
            if (Variables.GetBool(VarCouncilHeld))
            {
                SupplyWar.Offer();
            }
        });

        // Last, so every module's pre-war fragments for this step already exist when the council decides
        // whether the war table may open.
        Run("council", WarCouncil.OnStep);
        Run("scheduler", () => Scheduler.Flush());
    }

    private static void Run(string what, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WarFlow {what} failed: {ex}");
        }
    }

    /// <summary>Called when one of our operations finishes.</summary>
    public static void OnOperationFinished() => Variables.Set(VarOperationReady, false);
}
