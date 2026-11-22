using MelonLoader;
using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(ZilleWar.Core), "Zille War", "0.1.0", "wleeaf", null)]
[assembly: MelonGame("Torpor Games", "Suzerain")]

namespace ZilleWar;

internal sealed class Core : MelonMod
{
    internal static MelonLogger.Instance Log { get; private set; }

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        Events.OnEvaluateStep += OnEvaluateStep;
        // Order matters: the map data (towns, tiles) is needed by the variable registration below.
        // Each step is isolated so one failure cannot silently skip the rest (e.g. the conversation
        // injections, which must be registered during OnInitializeMelon).
        Step("load map data", () => ZilleData.LoadSpec());
        Step("register variables", () =>
        {
            ZilleOperation.RegisterVariables();
            ZilleCampaign.RegisterVariables();
            CityEvents.RegisterVariables();
        });
        Step("war flags", WarFlags.Register);
        Step("army carry-over", ArmyCarry.Register);
        Step("strategic map", StrategicMap.Register);
        Step("war events", WarEvents.Hook);
        Step("AN declaration", AnDeclaration.Register);
        Step("war flow", WarFlow.Register);
        Step("supply war", SupplyWar.Register);
        Step("war council", WarCouncil.Register);
        Step("vanilla turns 4-7", VanillaEarly.Register);
        Step("vanilla turns 8-10", VanillaLate.Register);
        Step("world systems", WorldSystems.Register);
        Step("war decrees", WarDecrees.Register);
        Step("sweep turns 4-6", SweepEarly.Register);
        Step("sweep turns 7-8", SweepMiddle.Register);
        Step("sweep turns 9-10", SweepLate.Register);
        Step("audit", Audit.Register);
        Step("finish", Finish.Register);
        Step("trace hooks", () => Trace.Install(HarmonyInstance));
        Step("self-test", SelfTest.Init);
        DevKeys = File.Exists(Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ZilleWar", "devkeys.flag"));
        LoggerInstance.Msg(DevKeys ? "Zille War initialized. Dev keys ON (UserData/ZilleWar/devkeys.flag): F6 take the objective, F7 last round, F8 dump, F9 load war map, F10 Pales war, F11 direct setup, F12 next Zille operation (Shift: cycle objective, Ctrl: reset), Shift+F11 Wehlen strength." : "Zille War initialized.");
    }

    /// <summary>The testing shortcuts (F6-F12) only work when UserData/ZilleWar/devkeys.flag exists, so a stray key press in a
    /// normal game can never end an operation or rebuild the map.</summary>
    internal static bool DevKeys { get; private set; }

    private void Step(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Init step '{what}' failed: {ex}");
        }
    }

    public override void OnUpdate()
    {
        SelfTest.Update();
        Keyboard kb = Keyboard.current;
        if (kb == null || !DevKeys)
        {
            return;
        }

        try
        {
            WarDebug.Track();
            if (kb.f6Key.wasPressedThisFrame)
            {
                WarDebug.ReachGoal();
            }
            else if (kb.f7Key.wasPressedThisFrame)
            {
                WarDebug.LastRound();
            }
            else if (kb.f8Key.wasPressedThisFrame)
            {
                WarProbe.DumpState();
                WarDebug.ProbeHud();
            }
            else if (kb.f9Key.wasPressedThisFrame)
            {
                WarProbe.LoadWarMap();
            }
            else if (kb.f10Key.wasPressedThisFrame)
            {
                WarProbe.InjectPalesWarFragment();
            }
            else if (kb.f11Key.wasPressedThisFrame && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            {
                ZilleCampaign.Strength = ZilleCampaign.Strength >= 10 ? 3 : ZilleCampaign.Strength + 1;
                LoggerInstance.Msg($"Wehlen strength {ZilleCampaign.Strength} ({ZilleCampaign.Tier})");
            }
            else if (kb.f11Key.wasPressedThisFrame)
            {
                WarProbe.SetupWarFragmentDirect();
            }
            else if (kb.f12Key.wasPressedThisFrame)
            {
                bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                if (ctrl)
                {
                    ZilleCampaign.Reset();
                }
                else if (shift)
                {
                    Objective o = ZilleCampaign.CycleObjective();
                    LoggerInstance.Msg($"Next objective: {o} - {ZilleCampaign.Info(o).Codename}: {ZilleCampaign.Info(o).Aim}");
                }
                else if (ZilleOperation.BuildNext(out var frag))
                {
                    WarProbe.InjectWarFragment(frag);
                }

                LoggerInstance.Msg(ZilleCampaign.Describe());
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Probe failed: {ex}");
        }
    }

    private void OnEvaluateStep(object sender, EventArgs e)
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        LoggerInstance.Msg($"Rizia turn {GameState.CurrentTurnNum} step {GameState.CurrentStepNum}");
        if (!_converterChecked)
        {
            _converterChecked = true;
            foreach (string c in new[] { "ZilleWar.War_Declared == true", "RiziaDLC.CountryRelation_Wehlen_AtWar == true", "ZilleWar.War_Declared ~= true" })
            {
                try
                {
                    LoggerInstance.Msg($"LuaConverter: '{c}' -> '{Il2Cpp.LuaConverter.ConvertExpression(c)}'");
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"LuaConverter '{c}' failed: {ex.Message}");
                }
            }
        }
    }

    private static bool _converterChecked;
}
