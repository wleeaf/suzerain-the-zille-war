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
    private readonly ConversationInjection _inj;

    public Talk(string conversation, string prefix)
    {
        _prefix = prefix;
        _inj = new ConversationInjection(conversation);
    }

    public string N(string name) => _prefix + name;

    public ConversationNodeSelector[] Next(params string[] names) =>
        names.Select(n => (ConversationNodeSelector)new ConversationNodeModdedNameSelector(_prefix + n)).ToArray();

    /// <summary>A spoken line. A custom sequence is ended with WaitForMessage(Continue) like vanilla's, so the line waits for the click.</summary>
    public Talk Line(string name, string speaker, string text, string[] next, string condition = null, string script = null,
        string sequence = null, bool conversant = false, ConversationNodeHook[] hooks = null)
    {
        if (conversant)
        {
            script = $"AddConversant(\"{speaker}\"); " + script;
        }

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

    /// <summary>A player choice. Quoted text is spoken by the King; anything else is an action.</summary>
    public Talk Choice(string name, string text, string[] next, string condition = null, string script = null, string menu = null)
    {
        _inj.AddNode(new ConversationNode(
            name: _prefix + name,
            text: text,
            menuText: menu,
            speakerSelector: new CharacterNameSelector(text.StartsWith("\"") ? Romus : RomusAction),
            nextNodes: Next(next),
            luaScript: script,
            luaCondition: condition));
        return this;
    }

    /// <summary>An empty logic node: runs a script and/or branches on conditions of its children.</summary>
    public Talk Logic(string name, string[] next, string condition = null, string script = null, ConversationNodeHook[] hooks = null)
    {
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

    public static string Wait(string sequence) =>
        string.IsNullOrEmpty(sequence) ? sequence : sequence.TrimEnd().TrimEnd(';') + "; WaitForMessage(Continue);";

    public static ConversationNodeHook[] AtStart(string conversation) =>
        new[] { new ConversationNodeHook(new ConversationStartNodeSelector(conversation)) };

    // ---- Lua snippets ----

    public static string V(string name) => $"Variable[\"{name}\"]";

    public static string Add(string name, int delta) => $"{V(name)} = {V(name)} + ({delta}); ";

    public static string Set(string name, string luaValue) => $"{V(name)} = {luaValue}; ";

    /// <summary>Weaken the Zille garrison by n steps, never below the floor of 3.</summary>
    public static string Weaken(int n)
    {
        string s = string.Empty;
        string v = V(ZilleCampaign.VarStrength);
        for (int i = 0; i < n; i++)
        {
            s += $"if {v} > 3 then {v} = {v} - 1 end; ";
        }

        return s;
    }
}
