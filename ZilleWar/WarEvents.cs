using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Decision;

namespace ZilleWar;

/// <summary>An option on a war event decision: shown text + what happens when chosen.</summary>
internal sealed record EventOption(string Text, Action OnChosen = null, Func<bool> IsAvailable = null);

/// <summary>A war event shown in the decision panel during an operation (vanilla style: OP1_Decision_War_Played_*).</summary>
internal sealed record WarEvent(string Name, string Title, string Text, IReadOnlyList<EventOption> Options);

/// <summary>
/// Registers war-event decisions in the game's story data and drives their options through the
/// Suzerain Modding Kit decision events. The war engine fires them by name from tile events.
/// </summary>
internal static class WarEvents
{
    private static readonly Dictionary<string, WarEvent> Events = new();
    private static bool _hooked;

    public static void Hook()
    {
        if (_hooked)
        {
            return;
        }

        SuzerainModdingKit.Events.OnDecisionShow += (_, _) => OnShow();
        SuzerainModdingKit.Events.OnDecisionFinished += (_, e) => OnFinished(e.SelectedOptionInfo);
        _hooked = true;
    }

    public static void Register(WarEvent ev, DecisionData template)
    {
        Events[ev.Name] = ev;
        var data = new DecisionData
        {
            Id = $"0x5A11{StableHash.Of(ev.Name):X8}",
            NameInDatabase = ev.Name,
            AppBundleProperties = template?.AppBundleProperties,
            TagsProperties = template?.TagsProperties,
            AssignedTokenProperties = new AssignedTokenProperties { AssignedToken = ZilleOperation.TokenName },
            StoryFragmentProperties = new StoryFragmentProperties
            {
                IsDone = false,
                StoryFragmentCondition = string.Empty,
                OnStoryFragmentBeginInstruction = string.Empty,
                OnStoryFragmentEndInstruction = string.Empty,
            },
            DecisionProperties = new DecisionProperties
            {
                Title = ev.Title,
                Description = ev.Text,
                HubTitle = ev.Title,
                HubDescription = ev.Text,
                Options = new Il2CppSystem.Collections.Generic.List<DecisionProperties.DecisionOption>(),
            },
        };

        foreach (var list in new[] { EntityDataManager.AllDecisionsData, EntityDataManager.decisionsData, EntityDataManager.DecisionsData })
        {
            if (list == null)
            {
                continue;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].NameInDatabase == ev.Name)
                {
                    list.RemoveAt(i);
                }
            }

            list.Add(data);
        }
    }

    /// <summary>Find a vanilla decision (used as a template and to log vanilla tone/format).</summary>
    public static DecisionData FindVanilla(string name)
    {
        foreach (DecisionData d in EntityDataManager.AllDecisionsData)
        {
            if (d.NameInDatabase == name)
            {
                return d;
            }
        }

        return null;
    }

    public static void LogVanilla(params string[] names)
    {
        foreach (string n in names)
        {
            DecisionData d = FindVanilla(n);
            if (d == null)
            {
                Core.Log.Msg($"  [vanilla decision] {n}: not found");
                continue;
            }

            DecisionProperties p = d.DecisionProperties;
            Core.Log.Msg($"  [vanilla decision] {n}: token='{d.AssignedTokenProperties?.AssignedToken}' title='{p?.Title}'");
            Core.Log.Msg($"      description='{p?.Description}'");
            Core.Log.Msg($"      hub='{p?.HubTitle}' / '{p?.HubDescription}'");
            if (p?.Options != null)
            {
                foreach (DecisionProperties.DecisionOption o in p.Options)
                {
                    Core.Log.Msg($"      option='{o.Text}' cond='{o.Condition}' instr='{o.Instruction}'");
                }
            }
        }
    }

    private static void OnShow()
    {
        string name = DecisionManager.CurrentDecisionName;
        if (name == null || !Events.TryGetValue(name, out WarEvent ev))
        {
            return;
        }

        Core.Log.Msg($"War event shown: {name}");
        foreach (EventOption o in ev.Options)
        {
            if (o.IsAvailable == null || o.IsAvailable())
            {
                DecisionManager.AddOption(o.Text);
            }
        }
    }

    private static void OnFinished(DecisionOptionInfo selected)
    {
        if (selected == null || !Events.TryGetValue(selected.DecisionName, out WarEvent ev))
        {
            return;
        }

        EventOption o = ev.Options.FirstOrDefault(x => x.Text == selected.Text);
        Core.Log.Msg($"War event '{ev.Name}' -> '{selected.Text}'");
        try
        {
            o?.OnChosen?.Invoke();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"War event option failed: {ex}");
        }
    }
}
