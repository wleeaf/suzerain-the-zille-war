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
            Objective.Wall => (
                "Our divisions have broken through the Wehzek border wall. Auremina and its gold hills are in Rizian hands, " +
                "and the road into the heart of Zille lies open.",
                "The Wehzek border wall held. Our assault on the Auremina hills has been thrown back with heavy losses."),
            Objective.Harbor => (
                "Zille is ours. The Golden Banner flies over the harbor for the first time since the lease, and Wehlen has " +
                "lost its main port on our coast. The world is watching how we hold the city.",
                "The drive on Zille has stalled. The harbor remains in Wehzek hands, and Smolak's ships still come and go."),
            Objective.Ports => (
                "Nanta has fallen. Wehlen's last convoys across the Strait of Kreleck have nowhere left to land.",
                "The push up the Sudis coast has failed. Nanta still stands, and through it Wehlen keeps Zille supplied."),
            _ => (
                "The last Wehzek positions have surrendered. For the first time in a quarter of a century, there is no foreign " +
                "army on the Rizian mainland.",
                "Wehzek holdouts still cling to the mainland. The war in Zille is not yet over."),
        };
        Add(Clone(vw, won, $"{info.Codename} - Victory", winText));
        Add(Clone(vl ?? vw, lost, $"{info.Codename} - Failed", loseText));
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
