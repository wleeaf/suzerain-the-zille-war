namespace ZilleWar;

/// <summary>
/// Integration sweep of Turns 9 and 10, the endings and the turn-independent systems: what the Zille war changes that the
/// first pass (LateState, LateT9, LateT10, WorldModifiers...) left untouched.
/// - <see cref="SweepLateSystems"/>: situations, policies, token badges, royal decrees and the Turn 9 decisions.
/// - <see cref="SweepLateTalk"/>: dialogue lines in Turns 9-11 and the endings.
/// Entity text edits go through <see cref="SLK"/>, applied on every story step.
/// </summary>
internal static class SweepLate
{
    private static readonly (string name, Action register, Action step)[] Parts =
    {
        ("systems", SweepLateSystems.Register, SweepLateSystems.OnStep),
        ("talk", SweepLateTalk.Register, null),
    };

    /// <summary>Called once from OnInitializeMelon: register variables, conversations, injections and vanilla edits.</summary>
    public static void Register()
    {
        foreach (var (name, register, _) in Parts)
        {
            Run($"register {name}", register);
        }
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        foreach (var (name, _, step) in Parts)
        {
            if (step != null)
            {
                Run($"step {name}", step);
            }
        }

        SLK.Apply();
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"SweepLate {what} failed: {ex}");
        }
    }
}
