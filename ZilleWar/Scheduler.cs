using HarmonyLib;
using Il2Cpp;
using SuzerainModdingKit.StoryFragments;

namespace ZilleWar;

/// <summary>
/// Orders the story fragments the mod adds within a step, so a scene never appears next to the scene it depends on.
/// Every fragment added through SMK passes through here. A fragment with a tier waits (queued, invisible) while a
/// fragment of a lower tier, or a vanilla fragment of the same step, is still pending, and is added the moment they
/// are done. The queue is flushed right before the game evaluates the step, so a waiting scene keeps the step open.
/// Order within a step: vanilla scenes; the post-Konstantium council and the consulate files; Beatrice, Alvarez and
/// Saltana; the war room, Hegel and Smolak (they react to the calls); the War Council (it briefs on all of it);
/// then the war table (WarCouncil waits for every queued or pending "ZilleWar.Pre_" fragment).
/// </summary>
internal static class Scheduler
{
    private static readonly (string prefix, int tier)[] Tiers =
    {
        ("ZilleWar.Pre_EarlyCouncil_", 0), ("ZilleWar.Pre_Call_Consulate_", 0),
        ("ZilleWar.Pre_Call_Beatrice_", 1), ("ZilleWar.Pre_Call_Alvarez_", 1), ("ZilleWar.Pre_Call_Saltana_", 1),
        ("ZilleWar.Pre_Call_WarRoom_", 2), ("ZilleWar.Pre_Call_Hegel_", 2), ("ZilleWar.Pre_Call_Smolak_", 2),
        ("ZilleWar.Pre_Council_", 3),
        ("ZilleWar.Late_", 1),
    };

    private sealed record Item(CustomStoryFragmentData Data, AddStoryFragmentOptions Options, string Name, int Tier);

    private static readonly List<Item> Queue = new();
    private static bool _bypass;

    private static int? TierOf(string name)
    {
        if (name == null)
        {
            return null;
        }

        foreach ((string p, int t) in Tiers)
        {
            if (name.StartsWith(p, StringComparison.Ordinal))
            {
                return t;
            }
        }

        return null;
    }

    private static IEnumerable<string> Pending()
    {
        var names = Managers.Instance?.GameFlowManager?.enabledNotDoneStoryFragmentsNames;
        if (names == null)
        {
            yield break;
        }

        foreach (string n in names)
        {
            if (n != null)
            {
                yield return n;
            }
        }
    }

    private static bool IsOurs(string n) => n.StartsWith("ZilleWar", StringComparison.Ordinal) || n.StartsWith("ZW_", StringComparison.Ordinal);

    private static bool Blocked(Item item)
    {
        foreach (string p in Pending())
        {
            if (p == item.Name)
            {
                continue;
            }

            int? t = TierOf(p);
            if (t.HasValue && t.Value < item.Tier)
            {
                return true;
            }

            // Our scenes react to what the player just did: let the vanilla scenes of this step go first (war tables aside).
            if (item.Tier >= 1 && !IsOurs(p) && !p.StartsWith("WarFragment", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return Queue.Any(q => q != item && q.Tier < item.Tier);
    }

    /// <summary>True while a queued fragment's name starts with one of the prefixes (it will appear later in this step).</summary>
    public static bool HasQueued(params string[] prefixes) =>
        Queue.Any(q => prefixes.Any(p => q.Name.StartsWith(p, StringComparison.Ordinal)));

    /// <summary>Harmony prefix on SMK's AddCustomStoryFragment. Returns false (skip) when the fragment was queued.</summary>
    internal static bool Intercept(CustomStoryFragmentData data, AddStoryFragmentOptions options, ref bool result)
    {
        if (_bypass || data == null)
        {
            return true;
        }

        int? tier = TierOf(data.Name);
        if (!tier.HasValue)
        {
            return true;
        }

        if (Queue.Any(q => q.Name == data.Name))
        {
            result = true;
            return false;
        }

        var item = new Item(data, options, data.Name, tier.Value);
        if (!Blocked(item))
        {
            return true;
        }

        Queue.Add(item);
        Core.Log.Msg($"Scheduler: '{data.Name}' waits for the scenes it follows.");
        result = true;
        return false;
    }

    /// <summary>Add every queued fragment whose predecessors are done.</summary>
    public static void Flush()
    {
        if (Queue.Count == 0)
        {
            return;
        }

        bool progress = true;
        while (progress)
        {
            progress = false;
            foreach (Item item in Queue.OrderBy(q => q.Tier).ToList())
            {
                if (Blocked(item))
                {
                    continue;
                }

                Queue.Remove(item);
                try
                {
                    _bypass = true;
                    bool ok = SuzerainModdingKit.GameState.AddCustomStoryFragment(item.Data, item.Options);
                    Core.Log.Msg($"Scheduler: '{item.Name}' {(ok ? "added" : "could not be added")}.");
                }
                catch (Exception ex)
                {
                    Core.Log.Error($"Scheduler: adding '{item.Name}' failed: {ex.Message}");
                }
                finally
                {
                    _bypass = false;
                }

                progress = true;
                break;
            }
        }
    }

    /// <summary>A save was loaded or a new game started: nothing queued belongs to it.</summary>
    public static void Clear() => Queue.Clear();
}

[HarmonyPatch(typeof(SuzerainModdingKit.GameState), nameof(SuzerainModdingKit.GameState.AddCustomStoryFragment))]
internal static class Patch_ScheduleFragments
{
    private static bool Prefix(CustomStoryFragmentData customStoryFragmentData, AddStoryFragmentOptions options, ref bool __result) =>
        Scheduler.Intercept(customStoryFragmentData, options, ref __result);
}

/// <summary>Before the game decides whether the step is complete, add what has become ready.</summary>
[HarmonyPatch(typeof(GameFlowManager), nameof(GameFlowManager.EvaluateCurrentStep))]
internal static class Patch_FlushBeforeStepEvaluation
{
    private static void Prefix()
    {
        try
        {
            Scheduler.Flush();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Scheduler flush failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.LoadSaveFile))]
internal static class Patch_ClearScheduleOnLoad
{
    private static void Prefix() => Scheduler.Clear();
}
