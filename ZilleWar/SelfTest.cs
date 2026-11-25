using System.Reflection;
using System.Text;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using MelonLoader.Utils;
using SuzerainModdingKit.StoryFragments.Conversation;
using UnityEngine;

namespace ZilleWar;

/// <summary>
/// Developer self-test, run inside the real game. Armed only by the file UserData/ZilleWar/selftest.flag (deleted as soon
/// as it is read). At the main menu it checks the expression converter, compiles every Lua string the mod writes, applies
/// and audits every vanilla edit, injects every vanilla-conversation patch through SMK, writes UserData/ZilleWar/selftest.txt
/// and quits the game.
/// </summary>
internal static class SelfTest
{
    private static string Dir => Path.Combine(MelonEnvironment.UserDataDirectory, "ZilleWar");
    private static bool _armed, _done;
    private static float _start, _readyAt;

    public static void Init()
    {
        string flag = Path.Combine(Dir, "selftest.flag");
        if (File.Exists(flag))
        {
            File.Delete(flag);
            _armed = true;
            Core.Log.Msg("Self-test armed: it will run at the main menu and quit the game.");
        }
    }

    public static void Update()
    {
        if (!_armed || _done)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (_start == 0)
        {
            _start = now;
        }

        bool ready = EntityDataManager.conversationsData != null && EntityDataManager.conversationsData.Count > 0 &&
                     DialogueManager.MasterDatabase != null;
        if (!ready)
        {
            if (now - _start > 600)
            {
                Finish(new List<string> { "FAIL: game data never became available (10 min)." });
            }

            return;
        }

        if (_readyAt == 0)
        {
            _readyAt = now;
            return;
        }

        if (now - _readyAt < 15)
        {
            return;
        }

        var r = new List<string>();
        Section(r, "converter", Converter);
        Section(r, "nil safety", NilSafety);
        Section(r, "lua", Lua_);
        Section(r, "linked conversations", Linked);
        Section(r, "vanilla edits", VanillaEdits);
        Section(r, "conversation injections", Injections);
        Finish(r);
    }

    private static void Section(List<string> r, string name, Action<List<string>> f)
    {
        r.Add($"== {name}");
        try
        {
            f(r);
        }
        catch (Exception ex)
        {
            r.Add($"FAIL: {name} threw {ex}");
        }
    }

    private static string Convert(string e) => LuaConverter.ConvertExpression(e);

    private static void Converter(List<string> r)
    {
        if (LuaConverter._variableNames == null)
        {
            LuaConverter.InitializeVariables();
        }

        Vanilla.AddExposedToConverter();
        foreach (string e in new[]
                 {
                     "ZilleWar.War_Declared == true", "ZilleWar.War_Declared == false",
                     "RiziaDLC.CountryRelation_Wehlen_AtWar == true && ZilleWar.War_Zille_Liberated == false",
                     "RiziaDLCSetup.CurrentTurn == 5 || ZilleWar.War_Over == true",
                 })
        {
            string c = Convert(e);
            bool ok = c != null && !System.Text.RegularExpressions.Regex.IsMatch(c, @"(?<!"")ZilleWar\.");
            r.Add($"{(ok ? "ok  " : "FAIL")} '{e}' -> '{c}'");
        }
    }

    private static void NilSafety(List<string> r)
    {
        string v = WarFlags.Declared;
        string saved = Lua.Run($"return Variable[\"{v}\"]").asString;
        try
        {
            void Check(string setup, string expr, bool expected)
            {
                Lua.Run(setup);
                bool got = Lua.IsTrue(Convert(expr));
                r.Add($"{(got == expected ? "ok  " : "FAIL")} with {setup}: '{expr}' is {got} (expected {expected})");
            }

            Check($"Variable[\"{v}\"] = nil", "ZilleWar.War_Declared == false", true);
            Check($"Variable[\"{v}\"] = nil", "ZilleWar.War_Declared == true", false);
            Check($"Variable[\"{v}\"] = true", "ZilleWar.War_Declared == false", false);
            Check($"Variable[\"{v}\"] = true", "ZilleWar.War_Declared == true", true);
            Check($"Variable[\"{v}\"] = false", "ZilleWar.War_Declared == false", true);
        }
        finally
        {
            Lua.Run($"Variable[\"{v}\"] = nil");
            r.Add($"(restored {v}; was '{saved}')");
        }
    }

    private static void Lua_(List<string> r)
    {
        LuaLint.RunOnce();
        r.Add($"{LuaLint.Checked} strings checked, {LuaLint.Problems.Count} problem(s)");
        r.AddRange(LuaLint.Problems.Select(p => "FAIL " + p));
    }

    /// <summary>
    /// Opens, the way a scene's panel does, every conversation that links into another conversation the mod injects into, and
    /// checks that the linked one received the mod's nodes. Runs before anything else patches the database.
    /// </summary>
    private static void Linked(List<string> r)
    {
        DialogueDatabase db = DialogueManager.MasterDatabase;
        var injected = new HashSet<string>(ConversationRegistry.Injections.Select(i => i.ConversationTitle)
            .Where(t => !t.StartsWith("ZilleWar/", StringComparison.Ordinal)), StringComparer.Ordinal);
        int roots = 0, checkedLinks = 0;
        foreach (Il2CppPixelCrushers.DialogueSystem.Conversation conv in db.conversations)
        {
            var targets = new HashSet<int>();
            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                foreach (Link l in e.outgoingLinks)
                {
                    if (l.destinationConversationID != conv.id)
                    {
                        targets.Add(l.destinationConversationID);
                    }
                }
            }

            var into = targets.Select(id => db.GetConversation(id)).Where(c => c != null && injected.Contains(c.Title)).ToList();
            if (into.Count == 0)
            {
                continue;
            }

            roots++;
            LinkedPatch.PatchWithLinked(conv);
            foreach (Il2CppPixelCrushers.DialogueSystem.Conversation t in into)
            {
                checkedLinks++;
                var want = ConversationRegistry.Injections.Where(i => i.ConversationTitle == t.Title).SelectMany(i => i.Nodes)
                    .Where(n => !n.IsOverride).Select(n => n.Name).ToHashSet();
                int have = t.dialogueEntries.ToArray().Count(e => want.Contains(Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName") ?? ""));
                r.Add($"{(have == want.Count ? "ok  " : "FAIL")} {conv.Title} -> {t.Title}: {have}/{want.Count} mod nodes");
            }
        }

        r.Add($"{roots} conversations link into {checkedLinks} injected conversations");
    }

    private static void VanillaEdits(List<string> r)
    {
        Vanilla.Apply();
        List<string> a = Vanilla.Audit();
        r.Add(a[0]);
        r.AddRange(a.Skip(1).Select(p => "FAIL " + p));
        if (a.Count == 1)
        {
            r.Add("ok   every vanilla edit found its target");
        }
    }

    private static void Injections(List<string> r)
    {
        Type injector = typeof(ConversationInjection).Assembly.GetType("SuzerainModdingKit.StoryFragments.Conversation.ConversationInjector");
        MethodInfo patch = injector?.GetMethod("PatchConversation", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (patch == null)
        {
            r.Add("FAIL SMK ConversationInjector.PatchConversation not found");
            return;
        }

        int convs = 0, expected = 0, got = 0;
        foreach (IGrouping<string, ConversationInjection> g in ConversationRegistry.Injections.GroupBy(i => i.ConversationTitle))
        {
            if (g.Key.StartsWith("ZilleWar/", StringComparison.Ordinal))
            {
                continue;   // custom conversations are created when the story offers them
            }

            Il2CppPixelCrushers.DialogueSystem.Conversation conv = DialogueManager.MasterDatabase.GetConversation(g.Key);
            if (conv == null)
            {
                r.Add($"FAIL conversation not found: {g.Key}");
                continue;
            }

            int want = g.SelectMany(i => i.Nodes).Count(n => !n.IsOverride);
            var before = new HashSet<string>();
            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                before.Add(Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName") ?? string.Empty);
            }

            Dump(conv, "before");
            patch.Invoke(null, new object[] { conv });
            Vanilla.PatchDialogue(conv);
            Dump(conv, "after");
            var mine = new HashSet<string>(StringComparer.Ordinal);
            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                string n = Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName");
                if (!string.IsNullOrEmpty(n))
                {
                    mine.Add(n);
                }
            }

            int have = g.SelectMany(i => i.Nodes).Count(n => !n.IsOverride && mine.Contains(n.Name));
            convs++;
            expected += want;
            got += have;
            if (have < want)
            {
                foreach (ConversationNode n in g.SelectMany(i => i.Nodes).Where(n => !n.IsOverride && !mine.Contains(n.Name)))
                {
                    r.Add($"FAIL not injected: {n.Name} in {g.Key}");
                }
            }

            // Every injected node must be reachable: some entry links to it.
            var linked = new HashSet<int>();
            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                foreach (Link l in e.outgoingLinks)
                {
                    if (l.destinationConversationID == conv.id)
                    {
                        linked.Add(l.destinationDialogueID);
                    }
                }
            }

            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                string n = Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName");
                if (!string.IsNullOrEmpty(n) && g.Any(i => i.Nodes.Any(x => x.Name == n)) && !linked.Contains(e.id))
                {
                    r.Add($"FAIL injected but nothing links to it: {n} in {g.Key}");
                }
            }
        }

        r.Add($"{convs} vanilla conversations patched: {got}/{expected} nodes injected");
    }

    /// <summary>Writes a conversation's entries (speaker, text, condition, script, links with priorities) to
    /// UserData/ZilleWar/convdump/{before|after}/, so a patched vanilla conversation can be read and walked offline.</summary>
    private static void Dump(Il2CppPixelCrushers.DialogueSystem.Conversation conv, string stage)
    {
        try
        {
            DialogueDatabase db = DialogueManager.MasterDatabase;
            string dir = Path.Combine(Dir, "convdump", stage);
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            sb.AppendLine($"# {conv.Title} (c{conv.id})");
            foreach (DialogueEntry e in conv.dialogueEntries)
            {
                string who = db.GetActor(e.ActorID)?.Name ?? e.ActorID.ToString();
                string node = Field.LookupValue(e.fields, "SuzerainModdingKit.NodeName");
                string text = Field.LookupValue(e.fields, "en");
                if (string.IsNullOrEmpty(text))
                {
                    text = e.DialogueText;
                }

                string menu = Field.LookupValue(e.fields, "Menu Text en");
                sb.Append($"[{e.id}]{(string.IsNullOrEmpty(node) ? "" : " {" + node + "}")} {who}: {text?.Replace("\n", " ")}\n");
                if (!string.IsNullOrEmpty(menu)) sb.Append($"    MENU: {menu}\n");
                if (!string.IsNullOrEmpty(e.conditionsString)) sb.Append($"    IF: {e.conditionsString}\n");
                if (!string.IsNullOrEmpty(e.userScript)) sb.Append($"    DO: {e.userScript}\n");
                if (e.isGroup) sb.Append("    GROUP\n");
                var links = new List<string>();
                foreach (Link l in e.outgoingLinks)
                {
                    string dest = l.destinationConversationID == conv.id ? l.destinationDialogueID.ToString() : $"c{l.destinationConversationID}:{l.destinationDialogueID}";
                    links.Add(l.priority == ConditionPriority.Normal ? dest : $"{dest}({l.priority})");
                }

                if (links.Count > 0) sb.Append($"    -> {string.Join(",", links)}\n");
            }

            string file = System.Text.RegularExpressions.Regex.Replace(conv.Title, "[^A-Za-z0-9_.-]+", "_") + ".txt";
            File.WriteAllText(Path.Combine(dir, file), sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Dump {conv?.Title}: {ex.Message}");
        }
    }

    private static void Finish(List<string> r)
    {
        _done = true;
        int fails = r.Count(l => l.StartsWith("FAIL", StringComparison.Ordinal));
        r.Insert(0, $"Zille War self-test {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {fails} failure(s)");
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Path.Combine(Dir, "selftest.txt"), string.Join("\n", r) + "\n", Encoding.UTF8);
        Core.Log.Msg($"Self-test finished with {fails} failure(s); quitting.");
        Application.Quit();
    }
}
