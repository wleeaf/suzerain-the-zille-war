using HarmonyLib;
using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Keeps the mod's scenes through a save and load. The modding kit adds our story fragments to the running step only; the save
/// does not hold them, so a game saved while one of our scenes was waiting on the map came back without it (and the module that
/// offered it believed it had been offered). Every conversation fragment we add is recorded in a save variable with its turn and
/// step; it is struck off the moment its conversation opens. After a load, any recorded scene of the current step that has not
/// been played, is not on the map and is not waiting in the Scheduler's queue is added again. Records of earlier steps are
/// dropped: a step can't end with one of our scenes still pending.
/// </summary>
internal static class FragmentKeeper
{
    public const string Var = "ZilleWar.Pending_Fragments";
    private const string RecordSep = "~~";
    private const char FieldSep = '|';
    private static bool _restoring;

    public static void Register()
    {
        Variables.Register(Var);
        Events.OnEvaluateStep += (_, _) => Restore();
    }

    private sealed record Rec(int Turn, int Step, string Name, string Token, string Title, string Description, int Type, string Conversation)
    {
        public override string ToString() =>
            string.Join(FieldSep, Turn, Step, Name, Token, Clean(Title), Clean(Description), Type, Conversation);

        public static Rec Parse(string s)
        {
            string[] f = s.Split(FieldSep);
            return f.Length == 8 && int.TryParse(f[0], out int turn) && int.TryParse(f[1], out int step) && int.TryParse(f[6], out int type)
                ? new Rec(turn, step, f[2], f[3], f[4], f[5], type, f[7])
                : null;
        }

        private static string Clean(string s) => (s ?? string.Empty).Replace(FieldSep, '/').Replace(RecordSep, "-");
    }

    private static int Turn => GameState.CurrentTurnNum ?? 0;
    private static int Step => GameState.CurrentStepNum ?? 0;

    private static List<Rec> Load()
    {
        string raw = Variables.GetString(Var);
        if (string.IsNullOrEmpty(raw) || raw == "nil")
        {
            return new List<Rec>();
        }

        return raw.Split(RecordSep, StringSplitOptions.RemoveEmptyEntries).Select(Rec.Parse).Where(r => r != null).ToList();
    }

    private static void Save(List<Rec> recs) => Variables.Set(Var, string.Join(RecordSep, recs));

    /// <summary>Called for every fragment we add (queued or not): remember it for this step.</summary>
    public static void Record(CustomStoryFragmentData data)
    {
        if (_restoring || data is not CustomConversationFragment c || !GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        try
        {
            List<Rec> recs = Load().Where(r => r.Turn == Turn && r.Step == Step && r.Name != c.Name).ToList();
            recs.Add(new Rec(Turn, Step, c.Name, c.AssignedTokenName, c.HubTitle, c.HubDescription, (int)c.Type, c.ConversationName));
            Save(recs);
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not record scene '{data.Name}': {ex.Message}");
        }
    }

    /// <summary>A conversation opened: if it is one of ours, it no longer needs keeping.</summary>
    public static void Played(string fragmentName, string conversationTitle)
    {
        try
        {
            List<Rec> recs = Load();
            int before = recs.Count;
            recs.RemoveAll(r => r.Name == fragmentName || (conversationTitle != null && r.Conversation == conversationTitle && r.Turn == Turn && r.Step == Step));
            if (recs.Count != before)
            {
                Save(recs);
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Could not mark scene '{fragmentName}' played: {ex.Message}");
        }
    }

    /// <summary>Every step evaluation: forget earlier steps, put back what a load lost.</summary>
    private static void Restore()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || Turn <= 0)
        {
            return;
        }

        try
        {
            List<Rec> recs = Load();
            List<Rec> current = recs.Where(r => r.Turn == Turn && r.Step == Step).ToList();
            if (current.Count != recs.Count)
            {
                Save(current);
            }

            foreach (Rec r in current)
            {
                if (GameState.StoryFragmentExistsInCurrentStep(r.Name) || Scheduler.HasQueued(r.Name))
                {
                    continue;
                }

                _restoring = true;
                try
                {
                    GameState.AddCustomConversation(r.Conversation);
                    bool ok = GameState.AddCustomStoryFragment(new CustomConversationFragment(r.Name, SuzerainStoryPackInfo.Rizia, r.Token, r.Title,
                        r.Description, (ConversationData.ConversationType)r.Type, r.Conversation));
                    Core.Log.Msg($"Restored scene '{r.Name}' ({r.Title}) after a load{(ok ? string.Empty : " (queued or refused)")}.");
                }
                finally
                {
                    _restoring = false;
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Restoring scenes failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(GameState), nameof(GameState.AddCustomStoryFragment))]
internal static class Patch_RecordFragments
{
    private static void Postfix(CustomStoryFragmentData customStoryFragmentData) => FragmentKeeper.Record(customStoryFragmentData);
}

[HarmonyPatch(typeof(ConversationPanel), "Setup")]
internal static class Patch_MarkScenePlayed
{
    private static void Prefix(ConversationData conversationData)
    {
        if (conversationData != null)
        {
            FragmentKeeper.Played(conversationData.NameInDatabase, conversationData.ConversationProperties?.Dialogue);
        }
    }
}
