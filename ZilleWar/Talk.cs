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

        LuaLint.Record(_prefix + name, condition, script);

        _inj.AddNode(new ConversationNode(
            name: _prefix + name,
            text: text,
            speakerSelector: new CharacterNameSelector(speaker),
            hooks: hooks,
            nextNodes: Next(next),
            luaScript: script,
            luaCondition: condition,
            sequence: Wait(sequence)));
        return this;
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
        Vanilla.Require(_conversation, vanillaEntry, $"not ({luaWhen})");
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
