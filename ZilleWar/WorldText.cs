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

    /// <summary>Where each name sits in the entity lists of one kind, valid while the lists are the same objects with the same counts.</summary>
    private sealed class NameIndex
    {
        public IntPtr[] Ptrs;
        public int[] Counts;
        public Dictionary<string, List<(int list, int i)>> Map;
    }

    private static readonly Dictionary<string, NameIndex> Indexes = new(StringComparer.Ordinal);

    /// <summary>
    /// The entities named <paramref name="name"/> in <paramref name="lists"/>. Scanning thousands of Il2Cpp entities on every call
    /// (dozens of calls per step evaluation) was the cost; the index is rebuilt when a list is replaced (a data reload) or its
    /// count changes, and every hit is checked against the live list, so a moved or replaced entity forces a rebuild.
    /// </summary>
    private static List<T> Lookup<T>(string kind, Il2CppSystem.Collections.Generic.List<T>[] lists, string name, Func<T, string> nameOf)
    {
        var found = new List<T>();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            found.Clear();
            if (!Indexes.TryGetValue(kind, out NameIndex ix) || !Current(ix, lists))
            {
                ix = Build(lists, nameOf);
                Indexes[kind] = ix;
            }

            if (!ix.Map.TryGetValue(name, out var hits))
            {
                return found;
            }

            bool stale = false;
            foreach ((int l, int i) in hits)
            {
                var list = lists[l];
                T e = list != null && i < list.Count ? list[i] : default;
                if (e != null && nameOf(e) == name)
                {
                    found.Add(e);
                }
                else
                {
                    stale = true;
                }
            }

            if (!stale)
            {
                return found;
            }

            Indexes.Remove(kind);
        }

        return found;
    }

    private static bool Current<T>(NameIndex ix, Il2CppSystem.Collections.Generic.List<T>[] lists)
    {
        if (ix.Ptrs.Length != lists.Length)
        {
            return false;
        }

        for (int l = 0; l < lists.Length; l++)
        {
            IntPtr p = lists[l]?.Pointer ?? IntPtr.Zero;
            if (p != ix.Ptrs[l] || (lists[l]?.Count ?? -1) != ix.Counts[l])
            {
                return false;
            }
        }

        return true;
    }

    private static NameIndex Build<T>(Il2CppSystem.Collections.Generic.List<T>[] lists, Func<T, string> nameOf)
    {
        var ix = new NameIndex { Ptrs = new IntPtr[lists.Length], Counts = new int[lists.Length], Map = new Dictionary<string, List<(int, int)>>(StringComparer.Ordinal) };
        for (int l = 0; l < lists.Length; l++)
        {
            var list = lists[l];
            ix.Ptrs[l] = list?.Pointer ?? IntPtr.Zero;
            ix.Counts[l] = list?.Count ?? -1;
            if (list == null)
            {
                continue;
            }

            for (int i = 0; i < list.Count; i++)
            {
                T e = list[i];
                string n = e == null ? null : nameOf(e);
                if (n == null)
                {
                    continue;
                }

                if (!ix.Map.TryGetValue(n, out var at))
                {
                    ix.Map[n] = at = new List<(int, int)>();
                }

                at.Add((l, i));
            }
        }

        return ix;
    }

    private static IEnumerable<(object entity, Func<(string title, string text)> get, Action<(string title, string text)> set)> Entities(string kind, string name)
    {
        switch (kind)
        {
            case "news":
                foreach (NewsData n in Lookup(kind, new[] { EntityDataManager.newsData, EntityDataManager.NewsData }, name, x => x.NameInDatabase))
                {
                    NewsProperties p = n.NewsProperties;
                    if (p != null)
                    {
                        yield return (n, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                    }
                }

                break;
            case "report":
                foreach (ReportData r in Lookup(kind, new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData }, name, x => x.NameInDatabase))
                {
                    ReportProperties p = r.ReportProperties;
                    if (p != null)
                    {
                        yield return (r, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                    }
                }

                break;
            case "codex":
                foreach (CodexEntryData c in Lookup(kind, new[] { EntityDataManager.codexEntriesData, EntityDataManager.CodexEntriesData }, name, x => x.NameInDatabase))
                {
                    CodexEntryProperties p = c.CodexEntryProperties;
                    if (p != null)
                    {
                        yield return (c, () => (p.Title, p.Description), v => { p.Title = v.title; p.Description = v.text; });
                    }
                }

                break;
        }
    }
}
