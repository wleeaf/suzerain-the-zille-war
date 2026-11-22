namespace ZilleWar;

/// <summary>
/// Line-by-line audit of the vanilla story against the Zille war (second integration sweep, after the phased operation):
/// one module per turn range, each registering its own vanilla edits and dialogue variants.
/// </summary>
internal static class Audit
{
    private static readonly (string what, Action register, Action step)[] Modules =
    {
        ("T4-T5", AuditT45.Register, AuditT45.OnStep),
        ("T6", AuditT6.Register, AuditT6.OnStep),
        ("T7", AuditT7.Register, AuditT7.OnStep),
        ("T8", AuditT8.Register, AuditT8.OnStep),
        ("T9-T10", AuditT910.Register, AuditT910.OnStep),
    };

    public static void Register()
    {
        MenuFixes.Register();
        foreach ((string what, Action register, _) in Modules)
        {
            Run($"audit {what}", register);
        }
    }

    public static void OnStep()
    {
        foreach ((string what, _, Action step) in Modules)
        {
            Run($"audit {what} step", step);
        }
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a?.Invoke();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"{what} failed: {ex}");
        }
    }
}
