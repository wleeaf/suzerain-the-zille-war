using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Shared helpers of the Turn 4-6 sweep (SweepEarly*.cs):
/// - the war's state as Lua conditions (the outcome conditions exclude Active, so a group of variants never shows twice);
/// - <see cref="News"/>/<see cref="Report"/>: a vanilla news item or report rewritten in place once it has been enabled. The
///   war's state at that moment is latched in a variable, so the archive keeps the version the King read that turn;
/// - <see cref="Decision"/>: a vanilla decision description reworded while a condition holds (vanilla text otherwise).
/// </summary>
internal static class SweepEarlyKit
{
    // ------------------------------------------------------------------ Lua conditions

    public static readonly string Active = WarFlags.LActive;
    public static readonly string Declared = WarFlags.LDeclared;
    public static readonly string Over = WarFlags.L(WarFlags.Over);
    public static readonly string Liberated = $"{WarFlags.L(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.Active)}";
    public static readonly string LostOrStopped =
        $"({WarFlags.L(WarFlags.Defeated)} or {WarFlags.L(WarFlags.Ceasefire)}) and {WarFlags.LNot(WarFlags.Active)} and {WarFlags.LNot(WarFlags.Liberated)}";
    public static readonly string Defeated =
        $"{WarFlags.L(WarFlags.Defeated)} and {WarFlags.LNot(WarFlags.Active)} and {WarFlags.LNot(WarFlags.Liberated)}";
    public static readonly string CeasefireOnly =
        $"{WarFlags.L(WarFlags.Ceasefire)} and {WarFlags.LNot(WarFlags.Defeated)} and {WarFlags.LNot(WarFlags.Active)} and {WarFlags.LNot(WarFlags.Liberated)}";
    public static readonly string CityHeld = WarFlags.L(WarFlags.ZilleCityHeld);

    public static string NotSet(string v) => $"Variable[\"{v}\"] ~= true";

    /// <summary>Nil-safe increment of a vanilla number.</summary>
    public static string Inc(string v, int d) => $"Variable[\"{v}\"] = (Variable[\"{v}\"] or 0) + ({d}); ";

    // ------------------------------------------------------------------ C# state

    public const string StActive = "active";
    public const string StWon = "won";
    public const string StDefeated = "defeated";
    public const string StCeasefire = "ceasefire";
    public const string StDivided = "divided";     // over, not liberated, Zille city Rizian (armistice or ceasefire)

    public static bool IsDeclared => Variables.GetBool(WarFlags.Declared);
    public static bool IsActive => IsDeclared && WarFlow.AtWar;
    public static bool IsWon => IsDeclared && !WarFlow.AtWar && ZilleCampaign.Liberated;
    public static int Turn => GameState.CurrentTurnNum ?? 0;
    public static int Step => GameState.CurrentStepNum ?? 0;

    /// <summary>"" before the declaration, else active | won | defeated | ceasefire.</summary>
    public static string State()
    {
        if (!IsDeclared)
        {
            return string.Empty;
        }

        if (WarFlow.AtWar)
        {
            return StActive;
        }

        if (ZilleCampaign.Liberated)
        {
            return StWon;
        }

        if (Variables.GetBool(ZilleOperation.CapturedVar("Zille")))
        {
            return StDivided;
        }

        return ZilleCampaign.Defeated ? StDefeated : StCeasefire;
    }

    /// <summary>Picks the text for a state: fought now, won, or lost / stopped.</summary>
    public static string By(string state, string active, string won, string lost) =>
        state == StActive ? active : state == StWon ? won : lost;

    /// <summary>As <see cref="By(string,string,string,string)"/>, with its own text for a divided Zille.</summary>
    public static string By(string state, string active, string won, string lost, string divided) =>
        state == StDivided ? divided : By(state, active, won, lost);

    public static string P(params string[] paragraphs) => NewsWire.P(paragraphs.Where(p => !string.IsNullOrEmpty(p)).ToArray());

    /// <summary>Inserts a sentence before a trailing vanilla tag such as " [-1 Budget]" (or appends it).</summary>
    public static string BeforeTag(string original, string sentence)
    {
        original ??= string.Empty;
        int i = original.LastIndexOf(" [", StringComparison.Ordinal);
        if (i >= 0 && original.TrimEnd().EndsWith("]", StringComparison.Ordinal))
        {
            return original.Substring(0, i) + " " + sentence + original.Substring(i);
        }

        return original.TrimEnd() + " " + sentence;
    }

    public static bool Once(string guard)
    {
        if (Variables.GetBool(guard))
        {
            return false;
        }

        Variables.Set(guard, true);
        return true;
    }

    public static void Lua(string code)
    {
        try
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(LuaFix.Rewrite(code));
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"SweepEarly Lua failed ({code?.Substring(0, Math.Min(60, code.Length))}...): {ex.Message}");
        }
    }

    // ------------------------------------------------------------------ press: in-place rewrites, latched

    private sealed class PressItem
    {
        public string Kind;
        public string Name;
        public string Latch;
        public Func<string, bool> When;
        public Func<string, string> Title;
        public Func<string, string, string> Text;
    }

    private static readonly List<PressItem> Press = new();
    private static readonly Dictionary<string, string> EnableVars = new(StringComparer.Ordinal);

    /// <summary>
    /// Rewrites vanilla news <paramref name="name"/> once it is enabled in a declared war. <paramref name="text"/> gets the
    /// latched state and the vanilla text; <paramref name="when"/> (default: any war state) decides whether to rewrite at all.
    /// </summary>
    public static void News(string name, Func<string, string, string> text, Func<string, string> title = null, Func<string, bool> when = null) =>
        AddPress("news", name, text, title, when);

    public static void Report(string name, Func<string, string, string> text, Func<string, string> title = null, Func<string, bool> when = null) =>
        AddPress("report", name, text, title, when);

    /// <summary>Appends a paragraph to vanilla news while <paramref name="when"/> holds for the latched state.</summary>
    public static void AppendNews(string name, Func<string, string> paragraph, Func<string, bool> when = null) =>
        AddPress("news", name, (s, o) => P(o, paragraph(s)), null, when);

    public static void AppendReport(string name, Func<string, string> paragraph, Func<string, bool> when = null) =>
        AddPress("report", name, (s, o) => P(o, paragraph(s)), null, when);

    private static void AddPress(string kind, string name, Func<string, string, string> text, Func<string, string> title, Func<string, bool> when)
    {
        string latch = "ZilleWar.SE_L_" + name;
        Variables.Register(latch);
        Press.Add(new PressItem { Kind = kind, Name = name, Latch = latch, Text = text, Title = title, When = when ?? (s => s.Length > 0) });
    }

    public static void ApplyPress()
    {
        foreach (PressItem it in Press)
        {
            try
            {
                // an unset variable reads "nil": that is no latched state (a peace game, or not yet enabled)
                string state = Variables.GetString(it.Latch);
                if (string.IsNullOrEmpty(state) || state == "nil")
                {
                    state = string.Empty;
                }

                if (state.Length == 0 && IsDeclared && Enabled(it.Kind, it.Name))
                {
                    state = State();
                    Variables.Set(it.Latch, state);
                }

                if (state.Length == 0 || !it.When(state))
                {
                    Restore(it);
                    continue;
                }

                string original = WorldText.OriginalText(it.Kind, it.Name) ?? string.Empty;
                string title = it.Title?.Invoke(state);
                string text = it.Text(state, original);
                if (it.Kind == "news")
                {
                    WorldText.News(it.Name, title, text);
                }
                else
                {
                    WorldText.Report(it.Name, title, text);
                }
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"SweepEarly press '{it.Name}' failed: {ex.Message}");
            }
        }
    }

    private static void Restore(PressItem it)
    {
        if (it.Kind == "news")
        {
            WorldText.RestoreNews(it.Name);
        }
        else
        {
            WorldText.RestoreReport(it.Name);
        }
    }

    /// <summary>The vanilla item has been switched on (its IsEnabledVariable is true).</summary>
    private static bool Enabled(string kind, string name)
    {
        string key = kind + ":" + name;
        if (!EnableVars.TryGetValue(key, out string v))
        {
            v = FindEnableVar(kind, name);
            if (v == null)
            {
                return false;
            }

            EnableVars[key] = v;
        }

        return v.Length > 0 && Variables.GetBool(v);
    }

    private static string FindEnableVar(string kind, string name)
    {
        if (kind == "news")
        {
            foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (NewsData n in list)
                {
                    if (n?.NameInDatabase == name && n.NewsProperties != null)
                    {
                        // a news item hidden by Vanilla.HideNews reads ZilleWar.Never: look through the hide
                        return Vanilla.UnhiddenEnableVariable("news:" + name, n.NewsProperties.IsEnabledVariable ?? string.Empty);
                    }
                }
            }
        }
        else
        {
            foreach (var list in new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (ReportData r in list)
                {
                    if (r?.NameInDatabase == name && r.ReportProperties != null)
                    {
                        return Vanilla.UnhiddenEnableVariable("report:" + name, r.ReportProperties.IsEnabledVariable ?? string.Empty);
                    }
                }
            }
        }

        return null;
    }

    // ------------------------------------------------------------------ decisions: descriptions reworded

    private static readonly Dictionary<string, List<Func<string, string>>> DecisionRewrites = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> DecisionOriginals = new(StringComparer.Ordinal);

    /// <summary>
    /// Rewords a vanilla decision's description. <paramref name="rewrite"/> gets the vanilla text and returns the new one,
    /// or null to keep vanilla. Several rewrites of one decision are tried in order; the first non-null wins.
    /// </summary>
    public static void Decision(string name, Func<string, string> rewrite)
    {
        if (!DecisionRewrites.TryGetValue(name, out var list))
        {
            DecisionRewrites[name] = list = new List<Func<string, string>>();
        }

        list.Add(rewrite);
    }

    public static void ApplyDecisions()
    {
        if (DecisionRewrites.Count == 0)
        {
            return;
        }

        bool rizia = GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia);
        foreach (var list in new[] { EntityDataManager.decisionsData, EntityDataManager.DecisionsData, EntityDataManager.AllDecisionsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecisionData d in list)
            {
                string name = d?.NameInDatabase;
                DecisionProperties p = d?.DecisionProperties;
                if (name == null || p == null || !DecisionRewrites.TryGetValue(name, out var rewrites))
                {
                    continue;
                }

                if (!DecisionOriginals.ContainsKey(name))
                {
                    DecisionOriginals[name] = p.Description ?? string.Empty;
                }

                string original = DecisionOriginals[name];
                string text = null;
                if (rizia && IsDeclared)
                {
                    foreach (var r in rewrites)
                    {
                        try
                        {
                            text = r(original);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Warning($"SweepEarly decision '{name}' failed: {ex.Message}");
                        }

                        if (text != null)
                        {
                            break;
                        }
                    }
                }

                text ??= original;
                if (p.Description != text)
                {
                    p.Description = text;
                }
            }
        }
    }
}
