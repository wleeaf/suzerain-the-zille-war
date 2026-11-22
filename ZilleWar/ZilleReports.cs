using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The result report of each Zille operation, cloned from vanilla's Golden Shield ones. The text here is what the
/// report says until the operation ends; then <see cref="SetDescription"/> replaces it with the General Staff's after-action dispatch.
/// </summary>
internal static class ZilleReports
{
    public static (string won, string lost) Register(int k, ObjectiveInfo info, string vanillaWon, string vanillaLost)
    {
        ReportData vw = Find(vanillaWon);
        ReportData vl = Find(vanillaLost);
        string won = $"Operation_Zille_{k}_Result_Won", lost = $"Operation_Zille_{k}_Result_Lost";
        string name = ZilleCampaign.Title(info);
        (string winText, string loseText) = info.Id switch
        {
            Objective.Harbor => (
                $"{name}, the offensive to retake the city of Zille and its harbor, has been declared a success by the General Staff. " +
                "The Golden Banner flies over the harbor for the first time since the lease, and Wehlen has lost both its headquarters and " +
                "its main port on the Rizian coast.",
                $"{name}, the offensive to retake the city of Zille and its harbor, has not met its objectives. The General Staff reports " +
                "fierce resistance on the approaches to the city, which remains in Wehzek hands."),
            Objective.Wall => (
                $"{name}, the push into the hills around Auremina, has been declared a success by the General Staff. The town and its gold " +
                "mines are in Rizian hands, and the roads to the northern coast lie open.",
                $"{name}, the push into the hills around Auremina, has not met its objectives. The General Staff reports that Rizian " +
                "troops were thrown back from the ridges with heavy losses."),
            _ => (
                $"{name}, the final offensive against the ports of Isor and Nanta, has been declared a success by the General Staff. " +
                "With nowhere left to land its convoys, Wehlen's last garrisons in Zille are laying down their arms.",
                $"{name}, the final offensive against the ports of Isor and Nanta, has not met its objectives. Through the two harbors, " +
                "Wehlen continues to supply its garrison across the Strait of Kreleck."),
        };
        Add(Clone(vw, won, $"0x5A111E00000003{k:X2}", $"{name} Successful", winText));
        Add(Clone(vl ?? vw, lost, $"0x5A111E00000004{k:X2}", $"{name} Failed", loseText));
        return (won, lost);
    }

    /// <summary>
    /// After a reload the reports of finished operations are no longer registered (they live in memory only), but the
    /// save still refers to them: register them again, with the after-action text kept in the save.
    /// </summary>
    public static void Restore()
    {
        string[] hist = ZilleCampaign.Str(ZilleCampaign.VarHistory).Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int k = 1; k <= hist.Length && k <= ZilleCampaign.MaxOperations; k++)
        {
            string won = $"Operation_Zille_{k}_Result_Won";
            if (Created.ContainsKey(won) || !Enum.TryParse(hist[k - 1].Split(':')[0], out Objective o) ||
                !ZilleCampaign.Objectives.Any(x => x.Id == o))
            {
                continue;
            }

            Register(k, ZilleCampaign.Info(o), "Operation_1_Result_Won", "Operation_1_Result_Lost");
            string text = ZilleCampaign.Str(ZilleCampaign.ReportTextVar(k));
            if (text.Length > 0)
            {
                SetDescription(won, text);
                SetDescription($"Operation_Zille_{k}_Result_Lost", text);
            }
        }
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

    private static ReportData Clone(ReportData v, string name, string id, string title, string description)
    {
        ReportProperties vp = v?.ReportProperties;
        return new ReportData
        {
            Id = id,       // stable across sessions (string.GetHashCode is randomized per process)
            NameInDatabase = name,
            AppBundleProperties = v?.AppBundleProperties,
            // Our own war token: vanilla's is the Pales invasion token, which is not enabled during the Zille war.
            AssignedTokenProperties = new AssignedTokenProperties { AssignedToken = ZilleOperation.TokenName },
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
