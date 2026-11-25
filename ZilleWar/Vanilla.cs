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

    /// <summary>
    /// A hidden news item or report points its IsEnabledVariable at a variable of its own that is never true. Vanilla decrees
    /// switch items on with EnableNews/EnableReport, which may write true to whatever the item's enable variable is: with one
    /// shared variable, a single such call (the Mave Nortus hub, the war tax) would have re-enabled every hidden item at once.
    /// Each item gets its own, and every one that was written true is reset at each step.
    /// </summary>
    private static string NeverFor(string key) => Never + "_" + key.Replace(':', '_');

    private static bool IsNever(string v) => v != null && (v == Never || v.StartsWith(Never + "_", StringComparison.Ordinal));

    private static readonly HashSet<string> NeverVars = new(StringComparer.Ordinal);

    private static void ResetNevers()
    {
        foreach (string v in NeverVars)
        {
            try
            {
                if (SuzerainModdingKit.Variables.GetBool(v))
                {
                    SuzerainModdingKit.Variables.Set(v, false);
                    Core.Log.Msg($"Vanilla: {v} had been switched on (EnableNews/EnableReport on a hidden item); reset.");
                }
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"Vanilla: could not reset {v}: {ex.Message}");
            }
        }
    }

    // ------------------------------------------------------------------ variables in expresso

    private static readonly Dictionary<string, bool> Exposed = new(StringComparer.Ordinal);

    /// <summary>Make a ZilleWar variable usable in expresso conditions. Booleans read nil as false, numbers read nil as 0.</summary>
    public static void Expose(string variable, bool isBool = true) => Exposed[variable] = isBool;

    private static readonly HashSet<string> ExposedPlain = new(StringComparer.Ordinal);

    /// <summary>
    /// Make a ZilleWar variable writable from expresso instructions ("X = true;", "X += 2;"): mapped to plain Variable["x"]
    /// like a vanilla variable, since the nil-safe forms of <see cref="Expose"/> are not valid assignment targets. Reads nil
    /// as nil, so use it only for variables that are set before any expresso reads them.
    /// </summary>
    public static void ExposePlain(string variable)
    {
        Exposed.Remove(variable);
        ExposedPlain.Add(variable);
    }

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

            foreach (string v in ExposedPlain)
            {
                names.Add(v);
                map[v] = $"Variable[\"{v}\"]";
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
        LuaLint.RecordExpresso($"Gate {fragmentName}", expressoCondition);
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

    /// <summary>The vanilla enable variable of a news item or report ("news:Name" / "report:Name"), seen through a hide:
    /// null while it is hidden and its own variable is not known.</summary>
    internal static string UnhiddenEnableVariable(string key, string current) =>
        !IsNever(current) ? current : OriginalEnable.TryGetValue(key, out string original) ? original : null;

    private static void Toggle(string key, Func<string> get, Action<string> set, bool hide)
    {
        string current = get();
        if (hide)
        {
            string never = NeverFor(key);
            NeverVars.Add(never);
            if (current != never)
            {
                if (!IsNever(current))
                {
                    OriginalEnable[key] = current;
                }

                set(never);
            }
        }
        else if (IsNever(current) && OriginalEnable.TryGetValue(key, out string original))
        {
            set(original);
        }
    }

    // ------------------------------------------------------------------ conditional instructions

    private static readonly List<(string ci, string contains, string cond)> InstructionGates = new();

    /// <summary>Rows of conditional instruction <paramref name="ciName"/> whose instruction contains <paramref name="contains"/>
    /// (or every row when null) only run when <paramref name="expressoCondition"/> also holds.</summary>
    public static void GateInstruction(string ciName, string contains, string expressoCondition)
    {
        LuaLint.RecordExpresso($"GateInstruction {ciName}", expressoCondition);
        InstructionGates.Add((ciName, contains, expressoCondition));
    }

    // ------------------------------------------------------------------ dialogue entries

    private static readonly List<(string conv, int entry, string lua)> Requirements = new();
    private static readonly List<(string conv, int entry, string lua)> ScriptAppends = new();

    /// <summary>The vanilla dialogue entry only shows when the Lua condition also holds.</summary>
    public static void Require(string conversationTitle, int entryId, string luaCondition)
    {
        luaCondition = LuaFix.Rewrite(luaCondition);
        LuaLint.Record($"Require {conversationTitle} #{entryId}", luaCondition, null);
        Requirements.Add((conversationTitle, entryId, luaCondition));
    }

    /// <summary>Run extra Lua when a vanilla dialogue entry plays (appended to its script).</summary>
    public static void AppendScript(string conversationTitle, int entryId, string lua)
    {
        lua = LuaFix.Rewrite(lua);
        LuaLint.Record($"AppendScript {conversationTitle} #{entryId}", null, lua);
        ScriptAppends.Add((conversationTitle, entryId, lua));
    }

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
                string t = s.Trim();
                // Many vanilla logic nodes carry only a Lua comment as their script ("--; ", "-- Continue; "): code appended on the
                // same line would be commented out, so a comment-only script is replaced and a script with a comment gets a new line.
                e.userScript = string.IsNullOrWhiteSpace(s) || (t.StartsWith("--", StringComparison.Ordinal) && !t.Contains('\n')) ? lua
                    : s.Contains("--", StringComparison.Ordinal) ? s.TrimEnd() + "\n" + lua
                    : s.TrimEnd() + (s.TrimEnd().EndsWith(";") ? " " : "; ") + lua;
            }
        }
    }

    /// <summary>
    /// Mod choices join vanilla menus at the vanilla links' priority (<see cref="Talk.Choice"/>). Where a vanilla entry continues
    /// with a line or a logic node instead of a menu, the Dialogue System plays that line and never offers choices of the same
    /// priority, so a mod choice hooked there would never show. There the mod choices' links are raised to High: they show while
    /// their condition holds, and the vanilla line follows otherwise. Idempotent; run after the modding kit has linked our nodes.
    /// </summary>
    internal static void RaiseChoicesOverLines(DialogueConversation conv)
    {
        DialogueDatabase db = DialogueManager.MasterDatabase;
        if (conv?.dialogueEntries == null || db == null)
        {
            return;
        }

        var ours = new HashSet<int>();
        foreach (DialogueEntry e in conv.dialogueEntries)
        {
            if (e != null && IsMod(e) && !e.isGroup && db.IsPlayerID(e.ActorID))
            {
                ours.Add(e.id);
            }
        }

        if (ours.Count == 0)
        {
            return;
        }

        foreach (DialogueEntry e in conv.dialogueEntries)
        {
            var links = e?.outgoingLinks;
            if (links == null || links.Count < 2)
            {
                continue;
            }

            List<Link> raise = null;
            foreach (Link l in links)
            {
                if (l != null && l.priority == ConditionPriority.Normal && l.destinationConversationID == conv.id && ours.Contains(l.destinationDialogueID))
                {
                    (raise ??= new List<Link>()).Add(l);
                }
            }

            if (raise == null || raise.Count == links.Count || IsMod(e))
            {
                continue;
            }

            bool npc = false, pc = false;
            foreach (Link l in links)
            {
                if (l != null && l.priority == ConditionPriority.Normal && !(l.destinationConversationID == conv.id && ours.Contains(l.destinationDialogueID)))
                {
                    Classify(db, db.GetDialogueEntry(l.destinationConversationID, l.destinationDialogueID), ref npc, ref pc, 0);
                }
            }

            if (npc && !pc)
            {
                foreach (Link l in raise)
                {
                    l.priority = ConditionPriority.High;
                }
            }
        }
    }

    private static bool IsMod(DialogueEntry e) =>
        Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName")?.StartsWith("ZilleWar.", StringComparison.Ordinal) ?? false;

    /// <summary>Does the link lead to a line (npc) or a menu (pc)? Group nodes pass on to their children.</summary>
    private static void Classify(DialogueDatabase db, DialogueEntry d, ref bool npc, ref bool pc, int depth)
    {
        if (d == null)
        {
            return;
        }

        if (!d.isGroup)
        {
            if (db.IsPlayerID(d.ActorID))
            {
                pc = true;
            }
            else
            {
                npc = true;
            }

            return;
        }

        if (depth > 4 || d.outgoingLinks == null)
        {
            return;
        }

        foreach (Link l in d.outgoingLinks)
        {
            if (l != null)
            {
                Classify(db, db.GetDialogueEntry(l.destinationConversationID, l.destinationDialogueID), ref npc, ref pc, depth + 1);
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

        ApplyEntities();
    }

    /// <summary>
    /// The gates, hides and instruction gates on vanilla entities. Also run right before the game evaluates a step
    /// (<see cref="Patch_VanillaBeforeStepEvaluation"/>): loading a save rebuilds every entity list from the data files, and the
    /// step is evaluated before the mod's step event, so without this a gated vanilla scene could be offered on the step a war
    /// save is loaded.
    /// </summary>
    internal static void ApplyEntities()
    {
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

        ResetNevers();
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

    /// <summary>True once the vanilla story fragment has been played (or skipped) this playthrough.</summary>
    public static bool Done(string fragmentName)
    {
        var done = Managers.Instance?.GameFlowManager?.doneStoryFragmentsNames;
        return done != null && done.Contains(fragmentName);
    }

    /// <summary>Self-test: which declared edits found their vanilla target.</summary>
    internal static List<string> Audit()
    {
        var problems = new List<string>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        void Add<T>(Il2CppSystem.Collections.Generic.List<T> list, Func<T, string> name)
        {
            if (list == null)
            {
                return;
            }

            foreach (T x in list)
            {
                string n = x == null ? null : name(x);
                if (n != null)
                {
                    names.Add(n);
                }
            }
        }

        Add(EntityDataManager.conversationsData, d => d.NameInDatabase);
        Add(EntityDataManager.decisionsData, d => d.NameInDatabase);
        Add(EntityDataManager.AllDecisionsData, d => d.NameInDatabase);
        Add(EntityDataManager.billsData, d => d.NameInDatabase);
        foreach (string g in FragmentGates.Keys.Where(g => !names.Contains(g)))
        {
            problems.Add($"Gate target not found: {g}");
        }

        var news = new HashSet<string>(StringComparer.Ordinal);
        foreach (NewsData n in EntityDataManager.newsData ?? new Il2CppSystem.Collections.Generic.List<NewsData>())
        {
            if (n?.NameInDatabase != null)
            {
                news.Add(n.NameInDatabase);
            }
        }

        foreach (string n in NewsHides.Select(h => h.name).Distinct().Where(n => !news.Contains(n)))
        {
            problems.Add($"HideNews target not found: {n}");
        }

        var reports = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReportData r in EntityDataManager.reportsData ?? new Il2CppSystem.Collections.Generic.List<ReportData>())
        {
            if (r?.NameInDatabase != null)
            {
                reports.Add(r.NameInDatabase);
            }
        }

        foreach (string n in ReportHides.Select(h => h.name).Distinct().Where(n => !reports.Contains(n)))
        {
            problems.Add($"HideReport target not found: {n}");
        }

        foreach ((string ci, string contains, string cond) in InstructionGates)
        {
            bool found = false, rowFound = false;
            foreach (ConditionalInstructionData c in EntityDataManager.conditionalInstructionData ?? new Il2CppSystem.Collections.Generic.List<ConditionalInstructionData>())
            {
                if (c?.NameInDatabase != ci)
                {
                    continue;
                }

                found = true;
                foreach (ConditionalInstruction row in c.ConditionalInstructionProperties?.ConditionalInstructions ?? new Il2CppSystem.Collections.Generic.List<ConditionalInstruction>())
                {
                    rowFound |= contains == null || (row?.Instruction?.Contains(contains, StringComparison.Ordinal) ?? false);
                }
            }

            if (!found || !rowFound)
            {
                problems.Add($"GateInstruction target not found: {ci} / {contains}");
            }
        }

        foreach ((string title, int id, string _) in Requirements.Concat(ScriptAppends))
        {
            DialogueConversation conv = DialogueManager.MasterDatabase?.GetConversation(title);
            if (conv == null)
            {
                problems.Add($"Dialogue edit: conversation not found: {title}");
            }
            else if (Entry(conv, id) == null)
            {
                problems.Add($"Dialogue edit: entry {id} not found in {title}");
            }
        }

        problems.Insert(0, $"Vanilla edits declared: {FragmentGates.Count} gates, {NewsHides.Count} news hides, {ReportHides.Count} report hides, " +
                           $"{InstructionGates.Count} instruction gates, {Requirements.Count} node conditions, {ScriptAppends.Count} node scripts.");
        return problems;
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
    // after the modding kit's own Setup prefix, which injects and links our nodes
    [HarmonyPriority(Priority.Low)]
    private static void Prefix(ConversationData conversationData)
    {
        try
        {
            DialogueConversation conv = DialogueManager.MasterDatabase?.GetConversation(conversationData?.ConversationProperties?.Dialogue);
            LinkedPatch.PatchWithLinked(conv);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Vanilla dialogue patch failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Many vanilla scenes run on through other conversations by link: the balcony speech plays most of itself in "Turn 6/Intro
/// Section", the House of Delegates II debate in "Pales War Update And Tax" and "Zille Treaty Discussion". The modding kit
/// injects our nodes only into the conversation a panel opens, and remembers each title it has patched for the whole session,
/// even when the game's dialogue data is reloaded and our nodes are gone. A vanilla line our variant replaces is hidden by a
/// Require; if that hide reached a conversation whose variants were missing, the scene hit a dead end and closed. So when a
/// panel opens, this patches the opened conversation and every conversation reachable from it by links: our nodes first
/// (re-injected when they are missing), then the vanilla hides and the choice priorities. Every step is idempotent.
/// </summary>
internal static class LinkedPatch
{
    private static readonly System.Reflection.MethodInfo SmkPatch;
    private static readonly System.Collections.Generic.HashSet<string> SmkPatched;

    static LinkedPatch()
    {
        try
        {
            Type injector = typeof(SuzerainModdingKit.StoryFragments.Conversation.ConversationInjection).Assembly
                .GetType("SuzerainModdingKit.StoryFragments.Conversation.ConversationInjector");
            SmkPatch = injector?.GetMethod("PatchConversation",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            SmkPatched = injector?.GetField("_conversationsPatched",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null) as HashSet<string>;
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Linked conversation patching unavailable: {ex.Message}");
        }

        if (SmkPatch == null || SmkPatched == null)
        {
            Core.Log.Warning("Linked conversation patching: the modding kit's injector was not found.");
        }
    }

    public static void PatchWithLinked(DialogueConversation root)
    {
        DialogueDatabase db = DialogueManager.MasterDatabase;
        if (root == null || db == null)
        {
            return;
        }

        var seen = new HashSet<int>();
        var queue = new Queue<DialogueConversation>();
        queue.Enqueue(root);
        seen.Add(root.id);
        while (queue.Count > 0)
        {
            DialogueConversation conv = queue.Dequeue();
            if (EnsureInjected(conv))
            {
                Vanilla.PatchDialogue(conv);
                Vanilla.RaiseChoicesOverLines(conv);
            }
            else
            {
                // Hiding vanilla lines without our variants in place could leave a scene with nowhere to go: keep it vanilla.
                Core.Log.Warning($"'{conv.Title}': the mod's lines could not be injected; the conversation stays vanilla.");
            }

            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                foreach (Link l in e.outgoingLinks)
                {
                    if (l.destinationConversationID != conv.id && seen.Add(l.destinationConversationID))
                    {
                        DialogueConversation next = db.GetConversation(l.destinationConversationID);
                        if (next != null)
                        {
                            queue.Enqueue(next);
                        }
                    }
                }
            }
        }
    }

    /// <summary>Injects our nodes into the conversation through the kit, again if the kit believes it did but they are gone.
    /// True when the conversation has no injections or has them in place.</summary>
    private static bool EnsureInjected(DialogueConversation conv)
    {
        if (SmkPatch == null || SmkPatched == null)
        {
            return true;
        }

        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (SuzerainModdingKit.StoryFragments.Conversation.ConversationInjection inj in
                 SuzerainModdingKit.StoryFragments.Conversation.ConversationRegistry.Injections)
        {
            if (string.Equals(inj.ConversationTitle, conv.Title, StringComparison.Ordinal))
            {
                foreach (var n in inj.Nodes)
                {
                    if (!n.IsOverride)
                    {
                        expected.Add(n.Name);
                    }
                }
            }
        }

        if (expected.Count == 0)
        {
            return true;
        }

        if (Present(conv, expected))
        {
            return true;
        }

        if (SmkPatched.Remove(conv.Title))
        {
            Core.Log.Msg($"'{conv.Title}' had lost the mod's lines (dialogue data reloaded); injecting them again.");
        }

        try
        {
            SmkPatch.Invoke(null, new object[] { conv });
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Injecting into '{conv.Title}' failed: {ex.InnerException?.Message ?? ex.Message}");
        }

        return Present(conv, expected);
    }

    private static bool Present(DialogueConversation conv, HashSet<string> expected)
    {
        foreach (DialogueEntry e in conv.dialogueEntries)
        {
            string n = Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName");
            if (!string.IsNullOrEmpty(n) && expected.Contains(n))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Gates and hides are in place before the game evaluates the step (they are lost when a save is loaded).</summary>
[HarmonyPatch(typeof(GameFlowManager), nameof(GameFlowManager.EvaluateCurrentStep))]
internal static class Patch_VanillaBeforeStepEvaluation
{
    private static void Prefix()
    {
        try
        {
            if (SuzerainModdingKit.GameState.IsCurrentStoryPack(SuzerainModdingKit.VanillaData.SuzerainStoryPackInfo.Rizia))
            {
                Vanilla.ApplyEntities();
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Vanilla edits before the step evaluation failed: {ex.Message}");
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
