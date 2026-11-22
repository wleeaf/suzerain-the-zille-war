using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// What Valgsland and Lespia hold against a King at war.
/// - Valgsland: a war on a friend of Contana makes Chancellor Hegel's republic an enemy in all but name. At the declaration
///   its standing drops by four (Hostile from vanilla's starting point), and for the rest of the reign it never rises above
///   Unfriendly (vanilla: NegotiatingPower -1 or lower), whatever the King does at the Meftiem. A Malenyevist King is spared.
/// - Lespia: Alvarez keeps books. A broken Meftiem promise, or Lespia left with nothing at Cavette (vanilla
///   MITZ_ScrewedOverLespia), is a grievance until the King gives him the final vote (Diplomacy_Lespia_PromiseITZSupport_Late).
///   So is a King who turned every offer down after Alvarez warned him. A grievance costs Lespian standing once, cancels
///   Lespia's vote against sanctions, makes Lespia vote for them at the Alliance (LateT8), and, once Alvarez has said so on
///   the telephone: if his navy holds the strait, its patrols turn lax every turn; if it doesn't, Lespian harbors refuel
///   the Wehzek convoys (applied in the call itself).
/// </summary>
internal static partial class SupplyWar
{
    internal const string MitzScrewed = "RiziaDLC.MITZ_ScrewedOverLespia";
    private const string MitzTakeover = "RiziaDLC.Diplomacy_Valgsland_PromiseITZTakeover";

    /// <summary>Lespia holds a grievance against Rizia (kept up to date every step).</summary>
    public const string VarLespiaGrievance = "ZilleWar.Lespia_Grievance";
    /// <summary>Alvarez has told the King what the grievance will cost.</summary>
    private const string VarAlvGrievanceSaid = "ZilleWar.Call_Alvarez_GrievanceSaid";
    /// <summary>Alvarez warned the King that Perla might sell fuel to Wehzek tankers.</summary>
    private const string VarAlvWarned = "ZilleWar.Call_Alvarez_Warned";
    /// <summary>Lespian harbors refuel the Wehzek convoys (the garrison was strengthened once).</summary>
    public const string VarLespiaFuel = "ZilleWar.Lespia_FuelsWehlen";
    private const string VarLespiaGrievanceCounted = "ZilleWar.Lespia_GrievanceCounted";
    private const string VarValgslandHostileSet = "ZilleWar.Valgsland_HostileSet";

    /// <summary>Lua: Rizia broke faith with Lespia at the Meftiem (a broken Turn 5 promise, or nothing for Lespia at Cavette).</summary>
    internal static readonly string LuaBrokeFaith =
        $"((Variable[\"{MitzPromise}\"] == true and Variable[\"{VoteNationalization}\"] == true) or Variable[\"{EarlyT5.VarLespiaBetrayed}\"] == true " +
        $"or Variable[\"{MitzScrewed}\"] == true)";

    /// <summary>Lua: Valgsland got what it wanted from the King: Lespia excluded, as he promised Hegel.</summary>
    internal static readonly string LuaHegelServed = $"(Variable[\"{MitzTakeover}\"] == true and Variable[\"{MitzScrewed}\"] == true)";

    private static void RegisterGrudges()
    {
        foreach (string v in new[] { VarLespiaGrievance, VarAlvGrievanceSaid, VarAlvWarned, VarLespiaFuel, VarLespiaGrievanceCounted, VarValgslandHostileSet })
        {
            Variables.Register(v);
            Vanilla.Expose(v);
        }
    }

    private static bool BrokeFaith =>
        Variables.GetBool(MitzScrewed) || Variables.GetBool(EarlyT5.VarLespiaBetrayed) ||
        (Variables.GetBool(MitzPromise) && Variables.GetBool(VoteNationalization));

    /// <summary>Lespia's grievance stands: broken faith not yet made good with the final vote.</summary>
    private static bool Betrayed => BrokeFaith && !Variables.GetBool(MitzPromiseLate);

    /// <summary>Warned, and still nothing bought from Lespia.</summary>
    private static bool Spurned =>
        Variables.GetBool(VarAlvWarned) && Variables.GetInt(Count(Lever.Lespia)) == 0 && !Variables.GetBool(VarLespiaMitz) &&
        !Variables.GetBool(MitzPromiseLate) && !(Variables.GetBool(MitzPromise) && !BrokeFaith);

    /// <summary>A grievance Alvarez has not yet put to the King on the telephone.</summary>
    private static bool GrievanceUnsaid => Betrayed && !Variables.GetBool(VarAlvGrievanceSaid);

    /// <summary>Every step, idempotent.</summary>
    private static void ForeignGrudges()
    {
        if (!Variables.GetBool("RiziaDLC.Trait_CommieKing"))
        {
            int np = Variables.GetInt(NpValgsland);
            int want = np;
            if (!Variables.GetBool(VarValgslandHostileSet))
            {
                Variables.Set(VarValgslandHostileSet, true);
                want -= 4;
                Core.Log.Msg($"Valgsland turns against Rizia over the war: standing {np} -> {Math.Min(want, -1)}.");
            }

            want = Math.Min(want, -1);
            if (want != np)
            {
                Variables.Set(NpValgsland, want);
            }
        }

        bool betrayed = Betrayed;
        bool grievance = betrayed || Spurned;
        if (Variables.GetBool(VarLespiaGrievance) != grievance)
        {
            Variables.Set(VarLespiaGrievance, grievance);
        }

        if (grievance && !Variables.GetBool(VarLespiaGrievanceCounted))
        {
            Variables.Set(VarLespiaGrievanceCounted, true);
            Variables.Set(NpLespia, Variables.GetInt(NpLespia) - 2);
            Core.Log.Msg(betrayed ? "Lespia holds Rizia's Meftiem betrayal against it." : "Lespia gives up on Rizia after its offers were refused.");
        }

        if (betrayed && Variables.GetBool(VarLespiaAllianceVote))
        {
            Variables.Set(VarLespiaAllianceVote, false);
        }

        // Once Alvarez has said it: his captains in the strait stop looking closely, every turn the grievance lasts.
        if (betrayed && WarFlow.AtWar && Variables.GetBool(VarLespiaStrait) && Variables.GetBool(VarAlvGrievanceSaid) &&
            !Variables.GetBool(VarStraitThin))
        {
            Variables.Set(VarStraitThin, true);
        }
    }
}
