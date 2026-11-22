using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// Rewrites the text of vanilla news, reports and codex entries in place, keeping the original so it can be put back
/// (when the condition stops holding, or in a Sordland game for Shared entries). Every call is idempotent.
/// </summary>
internal static class WorldText
{
    private static readonly Dictionary<string, (string title, string text)> Originals = new(StringComparer.Ordinal);

    // ------------------------------------------------------------------ news

    public static void News(string name, string title, string text) => Apply("news", name, title, text);

    public static void RestoreNews(string name) => Restore("news", name);

    // ------------------------------------------------------------------ reports

    public static void Report(string name, string title, string text) => Apply("report", name, title, text);

    public static void RestoreReport(string name) => Restore("report", name);

    // ------------------------------------------------------------------ codex

    public static void Codex(string name, string title, string text) => Apply("codex", name, title, text);

    public static void RestoreCodex(string name) => Restore("codex", name);

    /// <summary>The vanilla description (before any rewrite), or null if the entity is unknown.</summary>
    public static string OriginalText(string kind, string name)
    {
        if (Originals.TryGetValue($"{kind}:{name}", out var o))
        {
            return o.text;
        }

        foreach (var (_, get, _) in Entities(kind, name))
        {
            return get().text;
        }

        return null;
    }

    public static void RestoreAll()
    {
        foreach (string key in Originals.Keys.ToList())
        {
            string[] parts = key.Split(':', 2);
            Restore(parts[0], parts[1]);
        }
    }

    // ------------------------------------------------------------------ plumbing

    private static void Apply(string kind, string name, string title, string text)
    {
        string key = $"{kind}:{name}";
        foreach (var (_, get, set) in Entities(kind, name))
        {
            var current = get();
            if (!Originals.ContainsKey(key))
            {
                Originals[key] = current;
            }

            var wanted = (title ?? Originals[key].title, text ?? Originals[key].text);
            if (current.title != wanted.Item1 || current.text != wanted.Item2)
            {
                set(wanted);
            }
        }
    }

    private static void Restore(string kind, string name)
    {
        string key = $"{kind}:{name}";
        if (!Originals.TryGetValue(key, out var original))
        {
            return;
        }

        foreach (var (_, get, set) in Entities(kind, name))
        {
            if (get() != original)
            {
                set(original);
            }
        }
    }

    private static IEnumerable<(object entity, Func<(string title, string text)> get, Action<(string title, string text)> set)> Entities(string kind, string name)
    {
        switch (kind)
        {
            case "news":
                foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
                {
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (NewsData n in list)
                    {
                        NewsProperties p = n?.NewsProperties;
                        if (p != null && n.NameInDatabase == name)
                        {
                            yield return (n, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                        }
                    }
                }

                break;
            case "report":
                foreach (var list in new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData })
                {
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (ReportData r in list)
                    {
                        ReportProperties p = r?.ReportProperties;
                        if (p != null && r.NameInDatabase == name)
                        {
                            yield return (r, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                        }
                    }
                }

                break;
            case "codex":
                foreach (var list in new[] { EntityDataManager.codexEntriesData, EntityDataManager.CodexEntriesData })
                {
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (CodexEntryData c in list)
                    {
                        CodexEntryProperties p = c?.CodexEntryProperties;
                        if (p != null && c.NameInDatabase == name)
                        {
                            yield return (c, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                        }
                    }
                }

                break;
        }
    }
}
