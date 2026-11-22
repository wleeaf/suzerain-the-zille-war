using Il2Cpp;

namespace ZilleWar;

/// <summary>
/// The result report of each Zille operation, cloned from vanilla's Golden Shield ones. The text here is what the
/// report says until the operation ends; then <see cref="SetDescription"/> replaces it with the General Staff's after-action dispatch.
/// </summary>
internal static class ZilleReports
{
    public const string WonName = "Operation_Zille_Result_Won";
    public const string LostName = "Operation_Zille_Result_Lost";

    /// <summary>The operation's two result reports (shown by the game when the operation ends, after its last phase).</summary>
    public static (string won, string lost) Register(string vanillaWon, string vanillaLost)
    {
        ReportData vw = Find(vanillaWon);
        ReportData vl = Find(vanillaLost);
        string name = ZilleCampaign.OperationTitle;
        Add(Clone(vw, WonName, "0x5A111E0000000301", $"{name} Successful",
            $"{name}, the campaign to return Zille to the Crown, has been declared a success by the General Staff. The Golden Banner " +
            "flies over Zille, Auremina, Isor and Nanta, and the last Wehzek garrisons are laying down their arms."));
        Add(Clone(vl ?? vw, LostName, "0x5A111E0000000401", $"{name} Ends Short of Its Objectives",
            $"{name}, the campaign to return Zille to the Crown, has ended short of its objectives. The front has settled where the " +
            "last phase left it, and Wehlen still holds part of the region."));
        return (WonName, LostName);
    }

    /// <summary>
    /// A phase set up on its own (no snapshot to resume from) is an operation of one phase for the game, which shows a result
    /// report when it ends: give it a phase report (the after-action dispatch), not the whole operation's verdict.
    /// </summary>
    public static string RegisterPhase(int phase, string vanillaWon)
    {
        string name = $"Operation_Zille_Phase{phase}_Report";
        Add(Clone(Find(vanillaWon), name, $"0x5A111E00000005{phase:X2}", $"{ZilleCampaign.PhaseTitle(phase)}: After-Action Report",
            $"The General Staff's report on {ZilleCampaign.PhaseWords(phase)} of {ZilleCampaign.OperationCodename} is being written."));
        return name;
    }

    public static bool IsRegistered(string name) => Created.ContainsKey(name);

    /// <summary>
    /// After a reload the operation's reports are no longer registered (they live in memory only), but the save may refer to
    /// them: register them again, with the after-action text of the last phase kept in the save.
    /// </summary>
    public static void Restore()
    {
        if (Created.ContainsKey(WonName) || ZilleCampaign.OperationsDone <= 0)
        {
            return;
        }

        Register("Operation_1_Result_Won", "Operation_1_Result_Lost");
        string text = ZilleCampaign.Str(ZilleCampaign.ReportTextVar(ZilleCampaign.OperationsDone));
        if (text.Length > 0 && (ZilleCampaign.Liberated || ZilleCampaign.Defeated))
        {
            SetDescription(WonName, text);
            SetDescription(LostName, text);
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
