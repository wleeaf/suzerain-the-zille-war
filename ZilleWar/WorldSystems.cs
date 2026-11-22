using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// The world around the war: the HUD's per-turn lines and the overview situations (<see cref="WorldModifiers"/>),
/// the codex (<see cref="WorldCodex"/>), vanilla news and reports that the war contradicts (<see cref="WorldPress"/>),
/// and the papers, letters and dispatches that follow the war from turn to turn (<see cref="WorldVoices"/>).
/// </summary>
internal static class WorldSystems
{
    /// <summary>Keys of one-off items already published, as "|key|key|".</summary>
    public const string VarDone = "ZilleWar.World_Done";
    /// <summary>Turn in which the war ended (0 while it lasts or before it starts).</summary>
    public const string VarEndTurn = "ZilleWar.World_EndTurn";
    /// <summary>Turn in which the Pales war and the Zille war were first fought together (0 if never).</summary>
    public const string VarTwoWarsTurn = "ZilleWar.World_TwoWarsTurn";

    private static bool _registered;

    public static void Register()
    {
        foreach (string v in new[] { VarDone, VarEndTurn, VarTwoWarsTurn })
        {
            Variables.Register(v);
        }

        Run("modifiers", WorldModifiers.Register);
        Run("codex", WorldCodex.Register);
        Run("press", WorldPress.Register);
        Run("voices", WorldVoices.Register);
        Run("embargo", WorldEmbargo.Register);

        if (!_registered)
        {
            _registered = true;
            // Shared codex entries (Wehlen, Smolak, Beatrice) are also read in Sordland games: put their vanilla text back there.
            Events.OnEvaluateStep += (_, _) =>
            {
                if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
                {
                    Run("restore", WorldText.RestoreAll);
                }
            };
        }
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        if (Declared && WarEnded && Variables.GetInt(VarEndTurn) == 0)
        {
            Variables.Set(VarEndTurn, Turn);
        }

        if (TwoWars && Variables.GetInt(VarTwoWarsTurn) == 0)
        {
            Variables.Set(VarTwoWarsTurn, Turn);
        }

        Run("modifiers step", WorldModifiers.OnStep);
        Run("codex step", WorldCodex.OnStep);
        Run("press step", WorldPress.OnStep);
        Run("voices step", WorldVoices.OnStep);
        Run("embargo step", WorldEmbargo.OnStep);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"WorldSystems {what} failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ shared state, read the same way by every World* file

    public static int Turn => GameState.CurrentTurnNum ?? 0;
    public static int Step => GameState.CurrentStepNum ?? 0;

    public static bool Declared => Variables.GetBool(AnDeclaration.VarDeclared);
    /// <summary>Being fought now (computed, so it is right even before WarState stores it for this step).</summary>
    public static bool Active => WarFlow.AtWar;
    public static bool WarEnded => Declared && !WarFlow.AtWar;
    public static bool Liberated => ZilleCampaign.Liberated;
    public static bool Defeated => ZilleCampaign.Defeated;
    public static bool Ceasefire => Declared && !Liberated && !Defeated && Variables.GetBool(WarFlow.VarCeasefire);
    public static bool PalesWar => Variables.GetBool("RiziaDLC.Pales_War");
    public static bool TwoWars => Active && PalesWar;
    public static bool ZilleCityHeld => Declared && Variables.GetBool(ZilleOperation.CapturedVar("Zille"));

    public static int DeclaredTurn
    {
        get
        {
            int t = Variables.GetInt(AnDeclaration.VarDeclaredTurn);
            return t > 0 ? t : 4;
        }
    }

    /// <summary>1 in the turn of the declaration, 2 in the next, and so on.</summary>
    public static int WarTurn => Declared ? Turn - DeclaredTurn + 1 : 0;

    /// <summary>Turn the war ended in, or 0.</summary>
    public static int EndTurn => Variables.GetInt(VarEndTurn);

    /// <summary>Turns since the war ended (0 in the turn it ended), or -1 while it lasts.</summary>
    public static int TurnsSinceEnd => EndTurn > 0 ? Turn - EndTurn : -1;

    // SupplyWar state, read by name so this module does not depend on SupplyWar's internals.
    private const string LespiaStraitVar = "ZilleWar.Lespia_StraitClosed";
    private const string LespiaWithdrawnVar = "ZilleWar.Lespia_Withdrawn";

    /// <summary>Lespia closed the Strait of Kreleck for us at some point in the war.</summary>
    public static bool LespiaHelps => Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 || Variables.GetBool(LespiaStraitVar);

    /// <summary>The strait is closed to Wehzek shipping right now (Lespian patrols, or another module says so).</summary>
    public static bool StraitClosedNow =>
        Variables.GetBool(LespiaStraitVar) || Variables.GetBool(WorldModifiers.StraitClosed) ||
        (Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0 && !Variables.GetBool(LespiaWithdrawnVar));

    public static bool LespiaCaleqabiz => Variables.GetBool("ZilleWar.Lespia_Caleqabiz");

    /// <summary>What Rizia gave Lespia for the strait, as the end of a sentence ("..., in exchange for ...").</summary>
    public static string LespiaPrice =>
        LespiaCaleqabiz ? "in exchange for naval rights at the Rizian island of Caleqabiz"
        : Variables.GetBool("ZilleWar.Lespia_GasDeal") ? "in exchange for Aureus gas sold to LesPower at a discount"
        : Variables.GetBool("ZilleWar.Lespia_PaidBudget") ? "for a fee paid by the Rizian treasury"
        : Variables.GetString("ZilleWar.Ramiro_ConfrontOutcome") == "free" ? "for nothing at all, which in Lespia's case is its own kind of price"
        : "at a price neither government has made public";

    /// <summary>Rizia has made Lespia's part in the Friendship Day bombing public.</summary>
    public static bool LespiaExposed =>
        Variables.GetBool("RiziaDLC.Zille_Publicized_LespianInvolvement") || Variables.GetString("ZilleWar.Ramiro_Choice") == "publish";
    public static int RumburgFavors =>
        Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Embargo)) + Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffRaids)) +
        Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffFighters)) + Variables.GetInt("ZilleWar.Rumburg_Credits") +
        (Variables.GetInt(WorldModifiers.RumburgCredit) != 0 ? 1 : 0);

    /// <summary>The token reports about Zille go on: the region while it is foreign, the city once it is Rizian again.</summary>
    public static string ZilleToken => Variables.GetBool("RiziaDLC.ZilleReturn_Success") ? "Rizia_City_Zille" : ZilleOperation.TokenName;

    /// <summary>True the first time it is called for <paramref name="key"/> in this playthrough (stored in the save).</summary>
    public static bool Once(string key)
    {
        string done = DoneList;
        string marked = $"|{key}|";
        if (done.Contains(marked, StringComparison.Ordinal))
        {
            return false;
        }

        Variables.Set(VarDone, (done.Length == 0 ? "|" : done) + key + "|");
        return true;
    }

    public static bool Seen(string key) => DoneList.Contains($"|{key}|", StringComparison.Ordinal);

    private static string DoneList
    {
        get
        {
            string s = Variables.GetString(VarDone);
            return string.IsNullOrEmpty(s) || s == "nil" ? string.Empty : s;
        }
    }
}
