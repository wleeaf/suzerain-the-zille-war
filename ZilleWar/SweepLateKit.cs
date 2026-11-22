using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// In-memory edits of vanilla entities that have no helper elsewhere: royal decrees, policies, situations, token status
/// effects and decisions (owned by the Turn 9-10 sweep, usable by any part of it). Every edit is declared once in Register() and applied idempotently on every story step by
/// <see cref="Apply"/> (called from SweepLate.OnStep). Text edits switch back to the vanilla text when their condition
/// stops holding, so loading a peacetime save in the same session shows vanilla.
/// Conditions are C# predicates for texts (evaluated each step) and expresso strings for anything the game evaluates
/// itself (decree availability, error rows, decision option conditions, instructions).
/// </summary>
internal static class SLK
{
    // ------------------------------------------------------------------ declarations

    private sealed record TextEdit(string Kind, string Name, Func<bool> When, string Title, string Text, string Cost, bool Append = false);

    private sealed record OptionEdit(string Decision, int Index, Func<bool> When, string Text);

    private static readonly List<TextEdit> TextEdits = new();
    private static readonly List<OptionEdit> OptionEdits = new();
    private static readonly List<(string decree, string message, string expresso)> DecreeErrors = new();
    private static readonly List<(string decree, string expresso)> DecreeEnables = new();
    private static readonly List<(string decree, string instruction)> DecreeEnacts = new();
    private static readonly List<(string decision, int index, string expresso)> OptionGates = new();
    private static readonly List<(string decision, int index, string instruction)> OptionInstructions = new();

    /// <summary>Royal decree title/description/cost while <paramref name="when"/> holds (null keeps vanilla). First match wins.</summary>
    public static void Decree(string name, Func<bool> when, string title = null, string description = null, string cost = null) =>
        TextEdits.Add(new TextEdit("decree", name, when, title, description, cost));

    /// <summary>Policy title/description while <paramref name="when"/> holds.</summary>
    public static void Policy(string name, Func<bool> when, string title = null, string description = null) =>
        TextEdits.Add(new TextEdit("policy", name, when, title, description, null));

    /// <summary>Situation title/description while <paramref name="when"/> holds.</summary>
    public static void Situation(string name, Func<bool> when, string title = null, string description = null) =>
        TextEdits.Add(new TextEdit("situation", name, when, title, description, null));

    /// <summary>
    /// A paragraph added after the vanilla description of a decree, policy, situation, token status effect or decision while
    /// <paramref name="when"/> holds (kind: "decree", "policy", "situation", "tse", "decision"). Declared after the full
    /// rewrites of the same entity, it only shows when none of them applies.
    /// </summary>
    public static void Append(string kind, string name, Func<bool> when, string paragraph) =>
        TextEdits.Add(new TextEdit(kind, name, when, null, paragraph, null, true));

    /// <summary>Token status effect title/subtitle while <paramref name="when"/> holds.</summary>
    public static void Status(string name, Func<bool> when, string title = null, string subtitle = null) =>
        TextEdits.Add(new TextEdit("tse", name, when, title, subtitle, null));

    /// <summary>Decision title/description while <paramref name="when"/> holds.</summary>
    public static void Decision(string name, Func<bool> when, string title = null, string description = null) =>
        TextEdits.Add(new TextEdit("decision", name, when, title, description, null));

    /// <summary>Text of option <paramref name="index"/> (0-based, vanilla order) of a decision while <paramref name="when"/> holds.</summary>
    public static void Option(string decision, int index, Func<bool> when, string text) =>
        OptionEdits.Add(new OptionEdit(decision, index, when, text));

    /// <summary>Option <paramref name="index"/> of a decision is only offered while the expresso condition also holds.</summary>
    public static void OptionGate(string decision, int index, string expresso)
    {
        LuaLint.RecordExpresso($"OptionGate {decision}#{index}", expresso);
        OptionGates.Add((decision, index, expresso));
    }

    /// <summary>Extra expresso instructions run when option <paramref name="index"/> is chosen (e.g. "ZilleWar.X = true;").</summary>
    public static void OptionDo(string decision, int index, string instruction) => OptionInstructions.Add((decision, index, instruction));

    /// <summary>The decree cannot be enacted while <paramref name="expresso"/> holds; the panel shows <paramref name="message"/>.</summary>
    public static void DecreeError(string decree, string message, string expresso)
    {
        LuaLint.RecordExpresso($"DecreeError {decree}", expresso);
        DecreeErrors.Add((decree, message, expresso));
    }

    /// <summary>The decree is only listed while <paramref name="expresso"/> also holds.</summary>
    public static void DecreeEnabled(string decree, string expresso)
    {
        LuaLint.RecordExpresso($"DecreeEnabled {decree}", expresso);
        DecreeEnables.Add((decree, expresso));
    }

    /// <summary>Extra expresso instructions run when the decree is enacted.</summary>
    public static void DecreeOnEnact(string decree, string instruction) => DecreeEnacts.Add((decree, instruction));

    // ------------------------------------------------------------------ apply (every story step)

    private static readonly Dictionary<string, (string a, string b, string c)> Originals = new(StringComparer.Ordinal);

    public static void Apply()
    {
        Run("decrees", ApplyDecrees);
        Run("policies", ApplyPolicies);
        Run("situations", ApplySituations);
        Run("statuses", ApplyStatuses);
        Run("decisions", ApplyDecisions);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"SweepLate kit ({what}) failed: {ex}");
        }
    }

    private static TextEdit Pick(string kind, string name)
    {
        foreach (TextEdit e in TextEdits)
        {
            if (e.Kind == kind && e.Name == name && Safe(e.When))
            {
                return e;
            }
        }

        return null;
    }

    private static bool Has(string kind, string name) => TextEdits.Any(e => e.Kind == kind && e.Name == name);

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

    /// <summary>Remember the vanilla triple once, then set the chosen edit (or vanilla) if it differs.</summary>
    private static void Swap(string key, (string a, string b, string c) current, TextEdit e, Action<(string a, string b, string c)> set)
    {
        if (!Originals.TryGetValue(key, out var o))
        {
            Originals[key] = o = current;
        }

        var wanted = e == null ? o
            : e.Append ? (o.a, string.IsNullOrEmpty(o.b) ? e.Text : o.b.TrimEnd() + "\n\n" + e.Text, o.c)
            : (e.Title ?? o.a, e.Text ?? o.b, e.Cost ?? o.c);
        if (current != wanted)
        {
            set(wanted);
        }
    }

    private static string And(string current, string add)
    {
        string marked = $"({add})";
        if ((current ?? string.Empty).Contains(marked, StringComparison.Ordinal))
        {
            return current;
        }

        return string.IsNullOrWhiteSpace(current) ? marked : $"({current}) && {marked}";
    }

    private static string Append(string current, string instruction)
    {
        string ins = instruction.Trim();
        if ((current ?? string.Empty).Contains(ins, StringComparison.Ordinal))
        {
            return current;
        }

        string c = (current ?? string.Empty).TrimEnd();
        return string.IsNullOrEmpty(c) ? ins : (c.EndsWith(";") ? c : c + ";") + "\n" + ins;
    }

    private static void ApplyDecrees()
    {
        foreach (var list in new[] { EntityDataManager.decreesData, EntityDataManager.DecreesData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecreeData d in list)
            {
                DecreeProperties p = d?.DecreeProperties;
                string name = d?.NameInDatabase;
                if (p == null || name == null)
                {
                    continue;
                }

                if (Has("decree", name))
                {
                    Swap("decree:" + name, (p.Title, p.Description, p.CostText), Pick("decree", name), v =>
                    {
                        p.Title = v.a;
                        p.Description = v.b;
                        p.CostText = v.c;
                    });
                }

                foreach (var (_, expresso) in DecreeEnables.Where(x => x.decree == name))
                {
                    p.IsEnabled = And(p.IsEnabled, expresso);
                }

                foreach (var (_, instruction) in DecreeEnacts.Where(x => x.decree == name))
                {
                    p.OnDecreeEnact = Append(p.OnDecreeEnact, instruction);
                }

                foreach (var (_, message, expresso) in DecreeErrors.Where(x => x.decree == name))
                {
                    var rows = p.ErrorValidationDatas;
                    if (rows == null)
                    {
                        continue;
                    }

                    bool present = false;
                    foreach (DecreeProperties.ErrorValidationData r in rows)
                    {
                        if (r?.ErrorMessage == message)
                        {
                            present = true;
                            break;
                        }
                    }

                    if (!present)
                    {
                        // ours first, so a war reason is shown before vanilla's generic ones
                        rows.Insert(0, new DecreeProperties.ErrorValidationData { ErrorMessage = message, ErrorCondition = expresso });
                    }
                }
            }
        }
    }

    private static void ApplyPolicies()
    {
        foreach (var list in new[] { EntityDataManager.policiesData, EntityDataManager.PoliciesData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (PolicyData d in list)
            {
                PolicyProperties p = d?.PolicyProperties;
                string name = d?.NameInDatabase;
                if (p != null && name != null && Has("policy", name))
                {
                    Swap("policy:" + name, (p.Title, p.Description, null), Pick("policy", name), v =>
                    {
                        p.Title = v.a;
                        p.Description = v.b;
                    });
                }
            }
        }
    }

    private static void ApplySituations()
    {
        foreach (var list in new[] { EntityDataManager.situationsData, EntityDataManager.SituationsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (SituationData d in list)
            {
                SituationProperties p = d?.SituationProperties;
                string name = d?.NameInDatabase;
                if (p != null && name != null && Has("situation", name))
                {
                    Swap("situation:" + name, (p.Title, p.Description, null), Pick("situation", name), v =>
                    {
                        p.Title = v.a;
                        p.Description = v.b;
                    });
                }
            }
        }
    }

    private static void ApplyStatuses()
    {
        foreach (var list in new[] { EntityDataManager.tokenStatusEffectsData, EntityDataManager.TokenStatusEffectsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (TokenStatusEffectData d in list)
            {
                TokenStatusEffectProperties p = d?.TokenStatusEffectProperties;
                string name = d?.NameInDatabase;
                if (p != null && name != null && Has("tse", name))
                {
                    Swap("tse:" + name, (p.Title, p.Subtitle, null), Pick("tse", name), v =>
                    {
                        p.Title = v.a;
                        p.Subtitle = v.b;
                    });
                }
            }
        }
    }

    private static void ApplyDecisions()
    {
        foreach (var list in new[] { EntityDataManager.decisionsData, EntityDataManager.DecisionsData, EntityDataManager.AllDecisionsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (DecisionData d in list)
            {
                DecisionProperties p = d?.DecisionProperties;
                string name = d?.NameInDatabase;
                if (p == null || name == null)
                {
                    continue;
                }

                if (Has("decision", name))
                {
                    Swap("decision:" + name, (p.Title, p.Description, null), Pick("decision", name), v =>
                    {
                        p.Title = v.a;
                        p.Description = v.b;
                    });
                }

                if (p.Options == null)
                {
                    continue;
                }

                foreach (int i in OptionEdits.Where(o => o.Decision == name).Select(o => o.Index).Distinct())
                {
                    if (i < 0 || i >= p.Options.Count)
                    {
                        continue;
                    }

                    DecisionProperties.DecisionOption opt = p.Options[i];
                    string key = $"option:{name}#{i}";
                    if (!Originals.TryGetValue(key, out var o))
                    {
                        Originals[key] = o = (opt.Text, null, null);
                    }

                    OptionEdit e = OptionEdits.FirstOrDefault(x => x.Decision == name && x.Index == i && Safe(x.When));
                    string wanted = e?.Text ?? o.a;
                    if (opt.Text != wanted)
                    {
                        opt.Text = wanted;
                    }
                }

                foreach (var (_, i, expresso) in OptionGates.Where(x => x.decision == name))
                {
                    if (i >= 0 && i < p.Options.Count)
                    {
                        p.Options[i].Condition = And(p.Options[i].Condition, expresso);
                    }
                }

                foreach (var (_, i, instruction) in OptionInstructions.Where(x => x.decision == name))
                {
                    if (i >= 0 && i < p.Options.Count)
                    {
                        p.Options[i].Instruction = Append(p.Options[i].Instruction, instruction);
                    }
                }
            }
        }
    }
}
