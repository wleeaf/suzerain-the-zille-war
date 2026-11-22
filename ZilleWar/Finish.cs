using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>Items the sweep left open: Manus at the Zille front, Rizian Royal Gold in a Zille war, Turn 10 line-by-line.</summary>
internal static class Finish
{
    private static readonly (string name, Action register, Action step)[] Parts =
    {
        ("manus", FinishManus.Register, FinishManus.OnStep),
        ("gold", FinishGold.Register, FinishGold.OnStep),
        ("turn 10", FinishT10.Register, null),
    };

    public static void Register()
    {
        foreach (var (name, register, _) in Parts)
        {
            Run($"register {name}", register);
        }
    }

    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainModdingKit.VanillaData.SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        foreach (var (name, _, step) in Parts)
        {
            if (step != null)
            {
                Run($"step {name}", step);
            }
        }
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Finish {what} failed: {ex}");
        }
    }
}
