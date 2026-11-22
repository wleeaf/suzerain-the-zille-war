using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Turns 4 to 7 of the vanilla story, adjusted for the Zille war. The work is split per turn:
/// EarlyBookkeeping (what the declaration ends), EarlyT4, EarlyT5, EarlyT6, EarlyT7.
/// </summary>
internal static class VanillaEarly
{
    /// <summary>Called once from OnInitializeMelon: register variables, conversations, injections and vanilla edits.</summary>
    public static void Register()
    {
        Run("bookkeeping", EarlyBookkeeping.Register);
        Run("T4", EarlyT4.Register);
        Run("T5", EarlyT5.Register);
        Run("T6", EarlyT6.Register);
        Run("T7", EarlyT7.Register);
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        Run("bookkeeping step", EarlyBookkeeping.OnStep);
        Run("T4 step", EarlyT4.OnStep);
        Run("T5 step", EarlyT5.OnStep);
        Run("T6 step", EarlyT6.OnStep);
        Run("T7 step", EarlyT7.OnStep);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"VanillaEarly {what} failed: {ex}");
        }
    }
}
