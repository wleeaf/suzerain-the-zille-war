using SuzerainModdingKit.Character;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.StoryFragments.Conversation.NodeSelectors;
using SuzerainModdingKit.Utils;

namespace ZilleWar;

/// <summary>Builds dialogue nodes the way vanilla Rizia conversations are built.</summary>
internal sealed class Talk
{
    public const string Romus = "Player_Romus";             // spoken choices
    public const string RomusAction = "Player_Romus_Italic"; // action choices
    public const string Narrator = "Narrator";
    public const string Technical = "Paskal Beniwoll";      // vanilla's empty logic nodes

    private readonly string _prefix;
    private readonly string _conversation;
    private readonly ConversationInjection _inj;

    public Talk(string conversation, string prefix)
    {
        _prefix = prefix;
        _conversation = conversation;
        _inj = new ConversationInjection(conversation);
    }

    public string N(string name) => _prefix + name;

    /// <summary>Next nodes: our node names, or "#123" for vanilla entry 123 of this conversation,
    /// or "#123@Rizia/Turn 9/Some Conversation" for an entry of another conversation.</summary>
    public ConversationNodeSelector[] Next(params string[] names) =>
        (names ?? Array.Empty<string>()).Select(Select).ToArray();

    private ConversationNodeSelector Select(string n)
    {
        if (n.StartsWith("#", StringComparison.Ordinal))
        {
            string[] parts = n.Substring(1).Split('@', 2);
            return new EntrySelector(int.Parse(parts[0]), parts.Length > 1 ? parts[1] : null);
        }

        return new ConversationNodeModdedNameSelector(_prefix + n);
    }

    /// <summary>Hooks for a node that branches off vanilla entries: when its condition holds it is taken before the
    /// vanilla continuation (high priority, condition-gated). For player choices it is added to the menu.</summary>
    public static ConversationNodeHook[] From(params int[] vanillaEntries) =>
        vanillaEntries.Select(id => new ConversationNodeHook(new EntrySelector(id), ConversationNodeHook.HookMode.ConditionGated,
            ConversationNodeHook.HookPriority.High)).ToArray();

    /// <summary>Hook that inserts a node right after a vanilla entry (the entry's old continuation follows our node).</summary>
    public static ConversationNodeHook[] After(int vanillaEntry) =>
        new[] { new ConversationNodeHook(new EntrySelector(vanillaEntry), ConversationNodeHook.HookMode.Split) };

    /// <summary>A spoken line. A custom sequence is ended with WaitForMessage(Continue) like vanilla's, so the line waits for the click.</summary>
    public Talk Line(string name, string speaker, string text, string[] next, string condition = null, string script = null,
        string sequence = null, bool conversant = false, ConversationNodeHook[] hooks = null)
    {
        if (conversant)
        {
            script = $"AddConversant(\"{speaker}\"); " + script;
        }

        condition = LuaFix.Rewrite(condition);
        script = LuaFix.Rewrite(script);
        LuaLint.Record(_prefix + name, condition, script);

        // Long replies read better as a few short lines, the way vanilla writes them: the first part keeps the node's
        // name, condition, script, hooks and sequence; the rest follow as plain lines by the same speaker.
        List<string> parts = SplitSpeech(text);
        for (int i = 0; i < parts.Count; i++)
        {
            bool first = i == 0, last = i == parts.Count - 1;
            string partName = first ? name : $"{name}_p{i + 1}";
            _inj.AddNode(new ConversationNode(
                name: _prefix + partName,
                text: parts[i],
                speakerSelector: new CharacterNameSelector(speaker),
                hooks: first ? hooks : null,
                nextNodes: last ? Next(next) : Next($"{name}_p{i + 2}"),
                luaScript: first ? script : null,
                luaCondition: first ? condition : null,
                sequence: first ? Wait(sequence) : null));
        }

        return this;
    }

    private static readonly string[] Abbreviations = { "Mr.", "Mrs.", "Dr.", "St.", "Gen.", "Lt.", "Col.", "No.", "Sgt.", "Capt." };

    /// <summary>
    /// Splits a long reply into parts of about two sentences. Only pure speech ("...", quotes at both ends only) or pure
    /// narration (no quotes) is split; mixed lines are kept whole. Speech parts are each re-wrapped in quotes.
    /// </summary>
    public static List<string> SplitSpeech(string text, int maxChars = 230)
    {
        var whole = new List<string> { text };
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
        {
            return whole;
        }

        string body = text;
        bool speech = text.Length > 2 && text[0] == '"' && text[^1] == '"' && text.IndexOf('"', 1) == text.Length - 1;
        if (speech)
        {
            body = text.Substring(1, text.Length - 2);
        }
        else if (text.Contains('"'))
        {
            return whole;
        }

        // sentence ends: . ! ? (optionally followed by a closing quote) then a space and an upper-case letter
        var sentences = new List<string>();
        int start = 0;
        for (int i = 0; i < body.Length - 2; i++)
        {
            char c = body[i];
            if ((c == '.' || c == '!' || c == '?') && body[i + 1] == ' ' && char.IsUpper(body[i + 2]))
            {
                string candidate = body.Substring(start, i + 1 - start);
                if (Abbreviations.Any(a => candidate.EndsWith(" " + a, StringComparison.Ordinal) || candidate == a))
                {
                    continue;
                }

                sentences.Add(candidate.Trim());
                start = i + 2;
            }
        }

        sentences.Add(body.Substring(start).Trim());
        if (sentences.Count < 3)
        {
            return whole;
        }

        var parts = new List<string>();
        string cur = string.Empty;
        int n = 0;
        foreach (string sentence in sentences)
        {
            if (n >= 2 || (n >= 1 && cur.Length + sentence.Length > maxChars))
            {
                parts.Add(cur);
                cur = string.Empty;
                n = 0;
            }

            cur = n == 0 ? sentence : cur + " " + sentence;
            n++;
        }

        parts.Add(cur);
        // never leave a lonely short tail: fold it into the previous part
        if (parts.Count > 1 && parts[^1].Length < 40)
        {
            parts[^2] += " " + parts[^1];
            parts.RemoveAt(parts.Count - 1);
        }

        return speech ? parts.Select(p => "\"" + p + "\"").ToList() : parts;
    }

    /// <summary>
    /// A player choice. Quoted text is spoken by the King; anything else is an action. <paramref name="cost"/> is shown
    /// after the option in the menu only, the way vanilla shows prices, e.g. "-2 Budget" becomes "(-2 Budget)".
    /// </summary>
    public Talk Choice(string name, string text, string[] next, string condition = null, string script = null, string menu = null,
        string cost = null, ConversationNodeHook[] hooks = null)
    {
        if (!string.IsNullOrEmpty(cost))
        {
            menu = $"{menu ?? text} ({cost})";
        }

        condition = LuaFix.Rewrite(condition);
        script = LuaFix.Rewrite(script);
        LuaLint.Record(_prefix + name, condition, script);

        _inj.AddNode(new ConversationNode(
            name: _prefix + name,
            text: text,
            menuText: menu,
            speakerSelector: new CharacterNameSelector(text.StartsWith("\"") ? Romus : RomusAction),
            hooks: hooks,
            nextNodes: Next(next),
            luaScript: script,
            luaCondition: condition));
        return this;
    }

    /// <summary>An empty logic node: runs a script and/or branches on conditions of its children.</summary>
    public Talk Logic(string name, string[] next, string condition = null, string script = null, ConversationNodeHook[] hooks = null)
    {
        condition = LuaFix.Rewrite(condition);
        script = LuaFix.Rewrite(script);
        LuaLint.Record(_prefix + name, condition, script);
        _inj.AddNode(new ConversationNode(
            name: _prefix + name,
            speakerSelector: new CharacterNameSelector(Technical),
            hooks: hooks,
            nextNodes: Next(next),
            luaScript: script,
            luaCondition: condition,
            sequence: "Continue()"));
        return this;
    }

    /// <summary>Ends the conversation like vanilla does: an empty node running End().</summary>
    public Talk End(string name = "End")
    {
        _inj.AddNode(new ConversationNode(
            name: _prefix + name,
            speakerSelector: new CharacterNameSelector(Technical),
            nextNodes: Array.Empty<ConversationNodeSelector>(),
            luaScript: "End();",
            sequence: "Continue()"));
        return this;
    }

    public void Register() => _inj.Register();

    /// <summary>
    /// Replace a vanilla line or choice while <paramref name="luaWhen"/> holds: our node hangs off the vanilla parents,
    /// continues where the vanilla one did, and the vanilla one is hidden under the same condition.
    /// </summary>
    public Talk Variant(string name, int vanillaEntry, int[] vanillaParents, string luaWhen, string speaker, string text, string[] next,
        string script = null, string menu = null, string cost = null)
    {
        Vanilla.Require(_conversation, vanillaEntry, $"({luaWhen}) ~= true");
        if (speaker == Romus || speaker == RomusAction || speaker == null)
        {
            Choice(name, text, next, luaWhen, script, menu, cost, From(vanillaParents));
        }
        else
        {
            Line(name, speaker, text, next, luaWhen, script, hooks: From(vanillaParents));
        }

        return this;
    }

    public static string Wait(string sequence) =>
        string.IsNullOrEmpty(sequence) ? sequence : sequence.TrimEnd().TrimEnd(';') + "; WaitForMessage(Continue);";

    public static ConversationNodeHook[] AtStart(string conversation) =>
        new[] { new ConversationNodeHook(new ConversationStartNodeSelector(conversation)) };

    // ---- Lua snippets ----

    public static string V(string name) => $"Variable[\"{name}\"]";

    public static string Add(string name, int delta) => $"{V(name)} = ({V(name)} or 0) + ({delta}); ";

    public static string Set(string name, string luaValue) => $"{V(name)} = {luaValue}; ";

    /// <summary>Weaken the Zille garrison by n steps, never below the floor of 3.</summary>
    public static string Weaken(int n)
    {
        string s = string.Empty;
        string v = V(ZilleCampaign.VarStrength);
        for (int i = 0; i < n; i++)
        {
            s += $"if ({v} or 8) > 3 then {v} = ({v} or 8) - 1 end; ";
        }

        return s;
    }
}
