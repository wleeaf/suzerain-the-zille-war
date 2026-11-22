using HarmonyLib;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using SuzerainModdingKit.StoryFragments.Conversation.NodeSelectors;
using ConditionalInstruction = Il2Cpp.ConditionalInstructionProperties.ConditionalInstruction;
using DialogueConversation = Il2CppPixelCrushers.DialogueSystem.Conversation;

namespace ZilleWar;

/// <summary>
/// Edits to vanilla content, declared once at start-up and re-applied on every story step (all edits are
/// idempotent):
/// - <see cref="Expose"/>: lets the game's expression converter read our variables (story fragment
///   conditions and conditional instructions are written in "articy:expresso", which the converter only
///   translates for variables it knows; unknown names silently fail).
/// - <see cref="Gate"/>: ANDs a condition into a vanilla conversation, decision or bill.
/// - <see cref="HideNews"/>/<see cref="HideReport"/>: switches vanilla news/reports off while a predicate holds.
/// - <see cref="GateInstruction"/>: ANDs a condition into matching rows of a vanilla conditional instruction.
/// - <see cref="Require"/>: ANDs a Lua condition into a vanilla dialogue entry (used to hide a vanilla line
///   or choice when one of our variants replaces it).
/// Expresso conditions use "Name == true/false" with &amp;&amp; and ||. Our exposed booleans are nil-safe there.
/// </summary>
internal static class Vanilla
{
    private const string Never = "ZilleWar.Never";

    // ------------------------------------------------------------------ variables in expresso

    private static readonly Dictionary<string, bool> Exposed = new(StringComparer.Ordinal);

    /// <summary>Make a ZilleWar variable usable in expresso conditions. Booleans read nil as false, numbers read nil as 0.</summary>
    public static void Expose(string variable, bool isBool = true) => Exposed[variable] = isBool;

    internal static void AddExposedToConverter()
    {
        try
        {
            var names = LuaConverter._variableNames;
            var map = LuaConverter._variableToLuaMap;
            if (names == null || map == null)
            {
                return;
            }

            foreach ((string v, bool isBool) in Exposed)
            {
                names.Add(v);
                map[v] = isBool ? $"(Variable[\"{v}\"] == true)" : $"(Variable[\"{v}\"] or 0)";
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not expose variables to the expression converter: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------ story fragments

    private static readonly Dictionary<string, List<string>> FragmentGates = new(StringComparer.Ordinal);

    /// <summary>The vanilla conversation/decision/bill only plays when <paramref name="expressoCondition"/> also holds.</summary>
    public static void Gate(string fragmentName, string expressoCondition)
    {
        if (!FragmentGates.TryGetValue(fragmentName, out var list))
        {
            FragmentGates[fragmentName] = list = new List<string>();
        }

        list.Add(expressoCondition);
    }

    private static void ApplyGate(string name, StoryFragmentProperties p)
    {
        if (p == null || name == null || !FragmentGates.TryGetValue(name, out var conds))
        {
            return;
        }

        string c = p.StoryFragmentCondition ?? string.Empty;
        foreach (string add in conds)
        {
            string marked = $"({add})";
            if (c.Contains(marked, StringComparison.Ordinal))
            {
                continue;
            }

            c = string.IsNullOrWhiteSpace(c) ? marked : $"({c}) && {marked}";
        }

        p.StoryFragmentCondition = c;
    }

    // ------------------------------------------------------------------ news and reports

    private static readonly List<(string name, Func<bool> when)> NewsHides = new();
    private static readonly List<(string name, Func<bool> when)> ReportHides = new();
    private static readonly Dictionary<string, string> OriginalEnable = new(StringComparer.Ordinal);

    /// <summary>The vanilla news item never appears while <paramref name="when"/> is true (checked every step).</summary>
    public static void HideNews(string name, Func<bool> when) => NewsHides.Add((name, when));

    /// <summary>The vanilla report never appears while <paramref name="when"/> is true (checked every step).</summary>
    public static void HideReport(string name, Func<bool> when) => ReportHides.Add((name, when));

    private static void Toggle(string key, Func<string> get, Action<string> set, bool hide)
    {
        string current = get();
        if (hide)
        {
            if (current != Never)
            {
                OriginalEnable[key] = current;
                set(Never);
            }
        }
        else if (current == Never && OriginalEnable.TryGetValue(key, out string original))
        {
            set(original);
        }
    }

    // ------------------------------------------------------------------ conditional instructions

    private static readonly List<(string ci, string contains, string cond)> InstructionGates = new();

    /// <summary>Rows of conditional instruction <paramref name="ciName"/> whose instruction contains <paramref name="contains"/>
    /// (or every row when null) only run when <paramref name="expressoCondition"/> also holds.</summary>
    public static void GateInstruction(string ciName, string contains, string expressoCondition) =>
        InstructionGates.Add((ciName, contains, expressoCondition));

    // ------------------------------------------------------------------ dialogue entries

    private static readonly List<(string conv, int entry, string lua)> Requirements = new();
    private static readonly List<(string conv, int entry, string lua)> ScriptAppends = new();

    /// <summary>The vanilla dialogue entry only shows when the Lua condition also holds.</summary>
    public static void Require(string conversationTitle, int entryId, string luaCondition) =>
        Requirements.Add((conversationTitle, entryId, luaCondition));

    /// <summary>Run extra Lua when a vanilla dialogue entry plays (appended to its script).</summary>
    public static void AppendScript(string conversationTitle, int entryId, string lua) =>
        ScriptAppends.Add((conversationTitle, entryId, lua));

    internal static void PatchDialogue(DialogueConversation conv)
    {
        if (conv == null)
        {
            return;
        }

        foreach ((string title, int id, string lua) in Requirements)
        {
            if (!string.Equals(conv.Title, title, StringComparison.Ordinal))
            {
                continue;
            }

            DialogueEntry e = Entry(conv, id);
            if (e == null)
            {
                Core.Log.Warning($"Require: entry {id} not found in '{title}'.");
                continue;
            }

            string marked = $"({lua})";
            string c = e.conditionsString ?? string.Empty;
            if (!c.Contains(marked, StringComparison.Ordinal))
            {
                e.conditionsString = string.IsNullOrWhiteSpace(c) ? marked : $"({c}) and {marked}";
            }
        }

        foreach ((string title, int id, string lua) in ScriptAppends)
        {
            if (!string.Equals(conv.Title, title, StringComparison.Ordinal))
            {
                continue;
            }

            DialogueEntry e = Entry(conv, id);
            if (e == null)
            {
                Core.Log.Warning($"AppendScript: entry {id} not found in '{title}'.");
                continue;
            }

            string s = e.userScript ?? string.Empty;
            if (!s.Contains(lua, StringComparison.Ordinal))
            {
                e.userScript = string.IsNullOrWhiteSpace(s) ? lua : s.TrimEnd() + (s.TrimEnd().EndsWith(";") ? " " : "; ") + lua;
            }
        }
    }

    public static DialogueEntry Entry(DialogueConversation conv, int id)
    {
        foreach (DialogueEntry e in conv.dialogueEntries)
        {
            if (e.id == id)
            {
                return e;
            }
        }

        return null;
    }

    // ------------------------------------------------------------------ apply everything (every story step)

    public static void Apply()
    {
        AddExposedToConverter();
        foreach (string title in Requirements.Select(r => r.conv).Concat(ScriptAppends.Select(r => r.conv)).Distinct())
        {
            PatchDialogue(DialogueManager.MasterDatabase?.GetConversation(title));
        }

        foreach (var list in new[] { EntityDataManager.conversationsData, EntityDataManager.ConversationsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (ConversationData d in list)
            {
                ApplyGate(d?.NameInDatabase, d?.StoryFragmentProperties);
            }
        }

        foreach (var list in new[] { EntityDataManager.decisionsData, EntityDataManager.DecisionsData, EntityDataManager.AllDecisionsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecisionData d in list)
            {
                ApplyGate(d?.NameInDatabase, d?.StoryFragmentProperties);
            }
        }

        foreach (var list in new[] { EntityDataManager.billsData, EntityDataManager.BillsData, EntityDataManager.AllBillsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (BillData d in list)
            {
                ApplyGate(d?.NameInDatabase, d?.StoryFragmentProperties);
            }
        }

        if (NewsHides.Count > 0)
        {
            var hides = NewsHides.GroupBy(h => h.name).ToDictionary(g => g.Key, g => g.Any(h => Safe(h.when)), StringComparer.Ordinal);
            foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (NewsData n in list)
                {
                    NewsProperties p = n?.NewsProperties;
                    if (p != null && n.NameInDatabase != null && hides.TryGetValue(n.NameInDatabase, out bool hide))
                    {
                        Toggle("news:" + n.NameInDatabase, () => p.IsEnabledVariable, v => p.IsEnabledVariable = v, hide);
                    }
                }
            }
        }

        if (ReportHides.Count > 0)
        {
            var hides = ReportHides.GroupBy(h => h.name).ToDictionary(g => g.Key, g => g.Any(h => Safe(h.when)), StringComparer.Ordinal);
            foreach (var list in new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (ReportData r in list)
                {
                    ReportProperties p = r?.ReportProperties;
                    if (p != null && r.NameInDatabase != null && hides.TryGetValue(r.NameInDatabase, out bool hide))
                    {
                        Toggle("report:" + r.NameInDatabase, () => p.IsEnabledVariable, v => p.IsEnabledVariable = v, hide);
                    }
                }
            }
        }

        if (InstructionGates.Count > 0)
        {
            foreach (var list in new[] { EntityDataManager.conditionalInstructionData, EntityDataManager.ConditionalInstructionData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (ConditionalInstructionData c in list)
                {
                    var rows = c?.ConditionalInstructionProperties?.ConditionalInstructions;
                    if (rows == null)
                    {
                        continue;
                    }

                    foreach ((string ci, string contains, string cond) in InstructionGates)
                    {
                        if (c.NameInDatabase != ci)
                        {
                            continue;
                        }

                        string marked = $"({cond})";
                        foreach (ConditionalInstruction row in rows)
                        {
                            if (row == null || (contains != null && (row.Instruction == null || !row.Instruction.Contains(contains, StringComparison.Ordinal))))
                            {
                                continue;
                            }

                            string rc = row.Condition ?? string.Empty;
                            if (!rc.Contains(marked, StringComparison.Ordinal))
                            {
                                row.Condition = string.IsNullOrWhiteSpace(rc) ? marked : $"({rc}) && {marked}";
                            }
                        }
                    }
                }
            }
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
}

/// <summary>Selects a vanilla dialogue entry by its entry id (Articy ids are not unique inside a conversation).</summary>
internal sealed class EntrySelector : ConversationNodeSelector
{
    private readonly int _id;
    private readonly string _conversation;

    public EntrySelector(int id, string conversation = null)
    {
        _id = id;
        _conversation = conversation;
    }

    public override DialogueEntry Resolve(DialogueConversation currentConversation)
    {
        DialogueConversation conv = _conversation != null ? DialogueManager.MasterDatabase?.GetConversation(_conversation) : currentConversation;
        return conv == null ? null : Vanilla.Entry(conv, _id);
    }
}

[HarmonyPatch(typeof(ConversationPanel), nameof(ConversationPanel.Setup))]
internal static class Patch_VanillaDialogue
{
    private static void Prefix(ConversationData conversationData)
    {
        try
        {
            Vanilla.PatchDialogue(DialogueManager.MasterDatabase?.GetConversation(conversationData?.ConversationProperties?.Dialogue));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Vanilla dialogue patch failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(LuaConverter), nameof(LuaConverter.InitializeVariables))]
internal static class Patch_LuaConverterInit
{
    private static void Postfix() => Vanilla.AddExposedToConverter();
}

[HarmonyPatch(typeof(LuaConverter), nameof(LuaConverter.RefreshVariables))]
internal static class Patch_LuaConverterRefresh
{
    private static void Postfix() => Vanilla.AddExposedToConverter();
}
