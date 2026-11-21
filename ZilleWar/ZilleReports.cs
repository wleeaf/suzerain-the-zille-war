using Il2Cpp;

namespace ZilleWar;

/// <summary>End-of-operation reports for Operation: Northern Crown (cloned from the Golden Shield ones).</summary>
internal static class ZilleReports
{
    public static (string won, string lost) Register(int k, ObjectiveInfo info, string vanillaWon, string vanillaLost)
    {
        ReportData vw = Find(vanillaWon);
        ReportData vl = Find(vanillaLost);
        string won = $"Operation_Zille_{k}_Result_Won", lost = $"Operation_Zille_{k}_Result_Lost";
        (string winText, string loseText) = info.Id switch
        {
            Objective.Harbor => (
                "Zille is ours. The Golden Banner flies over the harbor for the first time since the lease, and Wehlen has lost its " +
                "headquarters and its main port on our coast.",
                "The drive on Zille has failed. The city and its harbor remain in Wehzek hands."),
            Objective.Wall => (
                "Auremina and its gold hills are in Rizian hands, and the heart of Zille lies open.",
                "Our assault on Auremina has been thrown back with heavy losses."),
            _ => (
                "Isor and Nanta have fallen. Wehlen has nowhere left to land its convoys, and the last garrisons are surrendering.",
                "The northern ports still hold. Through Isor and Nanta, Wehlen keeps Zille supplied."),
        };
        Add(Clone(vw, won, $"{info.Codename}: Victory", winText));
        Add(Clone(vl ?? vw, lost, $"{info.Codename}: Failed", loseText));
        return (won, lost);
    }

    private static readonly Dictionary<string, ReportData> Created = new();

    /// <summary>Replace a report's body (used for the detailed after-action report at operation end).</summary>
    public static void SetDescription(string name, string text)
    {
        if (Created.TryGetValue(name, out ReportData r) && r?.ReportProperties != null)
        {
            r.ReportProperties.Description = text;
        }
    }

    private static string Trim(string s) => s == null ? null : s.Length > 80 ? s.Substring(0, 80) + "..." : s;

    private static ReportData Clone(ReportData v, string name, string title, string description)
    {
        ReportProperties vp = v?.ReportProperties;
        return new ReportData
        {
            Id = name.GetHashCode().ToString("X"),
            NameInDatabase = name,
            AppBundleProperties = v?.AppBundleProperties,
            AssignedTokenProperties = v?.AssignedTokenProperties ?? new AssignedTokenProperties { AssignedToken = ZilleOperation.TokenName },
            TagsProperties = v?.TagsProperties,
            ReportProperties = new ReportProperties
            {
                TurnNo = vp?.TurnNo ?? 0,
                IsEnabledVariable = string.Empty,
                Title = title,
                Description = description,
                IsDone = false,
                IsNotificationActive = vp?.IsNotificationActive ?? true,
            },
        };
    }

    private static void Add(ReportData r)
    {
        Created[r.NameInDatabase] = r;
        foreach (var list in new[] { EntityDataManager.reportsData, EntityDataManager.ReportsData })
        {
            if (list == null)
            {
                continue;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].NameInDatabase == r.NameInDatabase)
                {
                    list.RemoveAt(i);
                }
            }

            list.Add(r);
        }
    }

    private static ReportData Find(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (ReportData r in EntityDataManager.ReportsData)
        {
            if (r.NameInDatabase == name)
            {
                return r;
            }
        }

        return null;
    }
}
