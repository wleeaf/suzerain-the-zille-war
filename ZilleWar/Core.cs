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
        ZilleOperation.RegisterVariables();
        ZilleCampaign.RegisterVariables();
        CityEvents.RegisterVariables();
        WarEvents.Hook();
        AnDeclaration.Register();
        WarFlow.Register();
        ZilleData.LoadSpec();
        Trace.Install(HarmonyInstance);
        LoggerInstance.Msg("Zille War initialized. Dev keys: F8 dump, F9 load war map, F10 Pales war, F11 direct setup, F12 next Zille operation (Shift: cycle objective, Ctrl: reset), Shift+F11 Wehlen strength.");
    }

    public override void OnUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        try
        {
            if (kb.f8Key.wasPressedThisFrame)
            {
                WarProbe.DumpState();
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
    }
}
