using System.Text;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ZilleWar;

/// <summary>
/// Development-only diagnostics for the vanilla war system.
/// F8: dump war/map state. F9: load the war map. F10: inject Operation_1 war fragment. F11: direct SetupWarFragment.
/// </summary>
internal static class WarProbe
{
    private static MelonLogger.Instance Log => Core.Log;

    public static void DumpState()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==== ZilleWar probe: state dump ====");
        Managers m = Managers.Instance;
        if (m == null)
        {
            sb.AppendLine("Managers.Instance is null (main menu?)");
            Write(sb);
            return;
        }

        GameFlowManager gf = m.GameFlowManager;
        sb.AppendLine($"storyPack={gf?.currentGameFlowData?.StoryPack} turn={gf?.CurrentTurnNo} step={gf?.CurrentStepNo}");
        sb.AppendLine($"WarFlowManager={(m.WarFlowManager != null)} WarTileManager={(m.WarTileManager != null)} " +
                      $"WarOperationManager={(m.WarOperationManager != null)} WarTokenManager={(m.WarTokenManager != null)}");

        if (m.WarFlowManager != null)
        {
            WarFlowManager wf = m.WarFlowManager;
            sb.AppendLine($"  war state={wf.GetWarState()} setupFinished={wf.IsSetupFinished()} inOperation={wf.IsInOperationPhase()} " +
                          $"warTurn={wf.GetCurrentWarTurn()} AP={wf.GetCurrentActionPoints()}");
            sb.AppendLine($"  warConfiguration={(wf.warConfiguration != null ? wf.warConfiguration.name : "null")}");
        }

        if (m.WarOperationManager?.currentOperation != null)
        {
            sb.AppendLine($"  current operation={m.WarOperationManager.currentOperation.warOperationData?.operationNameInDatabase}");
        }

        if (m.operations != null)
        {
            sb.AppendLine($"operations ({m.operations.Count}):");
            foreach (WarOperation op in m.operations)
            {
                WarOperationData d = op?.warOperationData;
                sb.AppendLine($"  - go='{op?.gameObject?.name}' op='{d?.operationNameInDatabase}' tiles={d?.includedTileNames?.Count}");
            }
        }

        MapManager mm = m.MapManager;
        if (mm != null)
        {
            MapContainer cur = mm.CurrentlyLoadedMap;
            sb.AppendLine($"MapManager: currentType={mm.GetCurrentMapType()} currentConfig={mm.GetCurrentMapConfiguration()?.name} " +
                          $"loaded='{cur?.mapGameObject?.name}'");
        }

        if (m.mapContainers != null)
        {
            sb.AppendLine($"mapContainers ({m.mapContainers.Count}):");
            foreach (MapContainer c in m.mapContainers)
            {
                MapConfiguration cfg = c?.GetMapConfiguration();
                sb.AppendLine($"  - go='{c?.gameObject?.name}' cfg='{cfg?.name}' type={cfg?.mapType} loaded={c?.isLoaded} " +
                              $"mapGO='{c?.mapGameObject?.name}' bounds={cfg?.bounds} zoom={cfg?.zoomMin}..{cfg?.zoomMax}");
                if (c?.mapGameObject != null)
                {
                    DumpHierarchy(sb, c.mapGameObject.transform, 2, 3);
                }
            }
        }

        if (m.WarTileManager?.allTiles != null)
        {
            sb.AppendLine($"WarTileManager.allTiles={m.WarTileManager.allTiles.Count}");
        }

        DumpStoryData(sb, gf);
        Write(sb);
    }

    private static void DumpStoryData(StringBuilder sb, GameFlowManager gf)
    {
        var frags = EntityDataManager.AllWarFragmentsData;
        sb.AppendLine($"AllWarFragmentsData ({frags?.Count}):");
        if (frags != null)
        {
            foreach (WarFragmentData f in frags)
            {
                sb.AppendLine($"  - name='{f.NameInDatabase}' id={f.Id} token='{f.AssignedTokenProperties?.AssignedToken}' " +
                              $"op='{f.WarFragmentProperties?.Operation}' title='{f.WarFragmentProperties?.HubWarFragmentTitle}' " +
                              $"enabled={f.IsEnabled} done={f.IsDone} cond='{f.StoryFragmentCondition}' " +
                              $"begin='{f.StoryFragmentProperties?.OnStoryFragmentBeginInstruction}' end='{f.StoryFragmentProperties?.OnStoryFragmentEndInstruction}'");
            }
        }

        var ops = EntityDataManager.OperationData;
        sb.AppendLine($"OperationData ({ops?.Count}):");
        if (ops != null)
        {
            foreach (OperationData o in ops)
            {
                OperationProperties p = o.OperationProperties;
                string phases = p?.Phases != null ? string.Join("|", ToEnumerable(p.Phases)) : "";
                sb.AppendLine($"  - name='{o.NameInDatabase}' id={o.Id} opName='{p?.OperationName}' tutorial={p?.IsTutorial} " +
                              $"start='{p?.OperationStartCondition}' result='{p?.ResultVariable}' earlyFinish='{p?.IsOperationFinishedEarly}' " +
                              $"enemyDead='{p?.TotalEnemyDeadVariable}' playerDead='{p?.TotalPlayerDeadVariable}' " +
                              $"win='{p?.WinReport}' lose='{p?.LoseReport}' resultInstr='{p?.ResultConditionalInstruction}' phases=[{phases}]");
            }
        }

        if (gf?.currentStep?.Fragments != null)
        {
            sb.AppendLine($"current step fragments: {string.Join(", ", ToEnumerable(gf.currentStep.Fragments))}");
        }
    }

    private static IEnumerable<string> ToEnumerable(Il2CppSystem.Collections.Generic.List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            yield return list[i];
        }
    }

    /// <summary>F10: put the vanilla Operation_1 war fragment on the map like the story does.</summary>
    public static void InjectPalesWarFragment() => InjectWarFragment(FindOperation1Fragment());

    /// <summary>Add a war fragment to the current step, enable its token and show the indicator.</summary>
    public static void InjectWarFragment(WarFragmentData frag)
    {
        GameFlowManager gf = Managers.Instance?.GameFlowManager;
        if (frag == null || gf?.currentStep == null)
        {
            Log.Warning($"Cannot inject: fragment={(frag != null)} step={(gf?.currentStep != null)}");
            return;
        }

        string name = frag.NameInDatabase;
        string token = frag.AssignedTokenProperties?.AssignedToken;
        Log.Msg($"Injecting war fragment '{name}' on token '{token}' (op '{frag.WarFragmentProperties?.Operation}')");
        if (!string.IsNullOrEmpty(token))
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"EnableToken(\"{token}\")");
        }

        if (!gf.currentStep.Fragments.Contains(name))
        {
            gf.currentStep.Fragments.Add(name);
        }

        gf.EvaluateStoryFragment(true, false, frag, name);
        CreateTokenIndicator(token);
        Log.Msg("Injected. Look for the indicator on the map and click it.");
    }

    /// <summary>F11: hand the fragment straight to the war flow manager.</summary>
    public static void SetupWarFragmentDirect()
    {
        WarFragmentData frag = FindOperation1Fragment();
        WarFlowManager wf = Managers.Instance?.WarFlowManager;
        if (frag == null || wf == null)
        {
            Log.Warning($"Cannot set up: fragment={(frag != null)} warFlow={(wf != null)}");
            return;
        }

        Log.Msg($"WarFlowManager.SetupWarFragment('{frag.NameInDatabase}') ...");
        wf.SetupWarFragment(frag);
    }

    private static WarFragmentData FindOperation1Fragment()
    {
        var frags = EntityDataManager.AllWarFragmentsData;
        var ops = EntityDataManager.OperationData;
        if (frags == null)
        {
            return null;
        }

        string op1Id = null;
        if (ops != null)
        {
            foreach (OperationData o in ops)
            {
                if (o.NameInDatabase == "Operation_1")
                {
                    op1Id = o.Id;
                }
            }
        }

        WarFragmentData fallback = null;
        foreach (WarFragmentData f in frags)
        {
            string op = f.WarFragmentProperties?.Operation;
            if (op == "Operation_1" || (op1Id != null && op == op1Id))
            {
                return f;
            }

            if (fallback == null && f.NameInDatabase != null && f.NameInDatabase.Contains("1"))
            {
                fallback = f;
            }
        }

        return fallback;
    }

    // Mirrors SuzerainModdingKit.GameState.CreateTokenIndicator (private there).
    private static void CreateTokenIndicator(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        TokenIndicatorPanel panel = Panels.Instance?.TokenIndicatorPanel;
        if (panel == null)
        {
            Log.Warning("No TokenIndicatorPanel.");
            return;
        }

        var type = TokenIndicatorPanel.TokenIndicatorType.StoryFragment;
        TokenIndicatorPanel.TokenIndicator existing = FindIndicator(panel, token, type);
        if (existing != null)
        {
            return;
        }

        panel.TryAddTokenIndicator(token, type, panel.tokenIndicators);
        TokenIndicatorPanel.TokenIndicator ind = FindIndicator(panel, token, type);
        if (ind == null || panel.instantiatedTokenIndicators == null || panel.instantiatedTokenIndicators.Count < 1)
        {
            Log.Warning($"Could not create indicator for '{token}'.");
            return;
        }

        TemplateTokenIndicator template = panel.instantiatedTokenIndicators[0];
        TemplateTokenIndicator obj = UnityEngine.Object.Instantiate(template, template.transform.parent);
        obj.Setup(ind);
        panel.instantiatedTokenIndicators.Add(obj);
    }

    private static TokenIndicatorPanel.TokenIndicator FindIndicator(
        TokenIndicatorPanel panel, string token, TokenIndicatorPanel.TokenIndicatorType type)
    {
        foreach (TokenIndicatorPanel.TokenIndicator i in panel.tokenIndicators)
        {
            if (i.token?.tokenDataEntityName == token && i.tokenIndicatorType == type)
            {
                return i;
            }
        }

        return null;
    }

    public static void LoadWarMap()
    {
        MapManager mm = Managers.Instance?.MapManager;
        if (mm == null)
        {
            Log.Warning("No MapManager.");
            return;
        }

        Log.Msg("Loading MapType.War ...");
        mm.LoadMap(MapConfiguration.MapType.War);
    }

    public static void SetupDebugWar()
    {
        WarFlowManager wf = Managers.Instance?.WarFlowManager;
        if (wf == null)
        {
            Log.Warning("No WarFlowManager.");
            return;
        }

        Log.Msg("Calling WarFlowManager.SetupDebugWar(\"Operation_1\") ...");
        wf.SetupDebugWar("Operation_1");
    }

    private static void DumpHierarchy(StringBuilder sb, Transform t, int depth, int maxDepth)
    {
        int childCount = t.childCount;
        string comps = string.Join(",", t.GetComponents<Component>().Select(c => c.GetIl2CppType().Name));
        sb.AppendLine($"{new string(' ', depth * 2)}{t.name} [{comps}] children={childCount} pos={t.localPosition} scale={t.localScale}");
        if (depth - 2 >= maxDepth)
        {
            return;
        }

        int shown = 0;
        for (int i = 0; i < childCount && shown < 12; i++, shown++)
        {
            DumpHierarchy(sb, t.GetChild(i), depth + 1, maxDepth);
        }

        if (childCount > 12)
        {
            sb.AppendLine($"{new string(' ', (depth + 1) * 2)}... {childCount - 12} more");
        }
    }

    private static void Write(StringBuilder sb)
    {
        string text = sb.ToString();
        foreach (string line in text.Split('\n'))
        {
            Log.Msg(line.TrimEnd('\r'));
        }

        string dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ZilleWar");
        Directory.CreateDirectory(dir);
        File.AppendAllText(Path.Combine(dir, "probe.log"), $"[{DateTime.Now:HH:mm:ss}]\n{text}\n");
    }
}
