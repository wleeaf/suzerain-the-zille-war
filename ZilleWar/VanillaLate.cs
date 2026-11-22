using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Turns 8 to 10, the endings and the summary, adjusted for the Zille war. The work is split by period:
/// <see cref="LateState"/> (outcome flags, Wehlen's standing hostility, news, the end summary and the war title),
/// <see cref="LateT8"/>, <see cref="LateT9"/> and <see cref="LateT10"/> (conversations, including the endings).
/// </summary>
internal static class VanillaLate
{
    /// <summary>Called once from OnInitializeMelon: register variables, conversations, injections and vanilla edits.</summary>
    public static void Register()
    {
        foreach ((string what, Action a) in new (string, Action)[]
                 {
                     ("state", LateState.Register), ("turn 8", LateT8.Register), ("turn 9", LateT9.Register), ("turn 10 and endings", LateT10.Register),
                 })
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Core.Log.Error($"VanillaLate {what} registration failed: {ex}");
            }
        }
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        foreach ((string what, Action a) in new (string, Action)[]
                 {
                     ("state", LateState.OnStep), ("turn 8", LateT8.OnStep), ("turn 9", LateT9.OnStep), ("turn 10", LateT10.OnStep),
                 })
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Core.Log.Error($"VanillaLate {what} step failed: {ex}");
            }
        }
    }
}
