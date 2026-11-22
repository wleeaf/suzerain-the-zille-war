using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// The war's state as conditions, for vanilla gating and for our own dialogue.
/// Expresso (story fragment conditions, conditional instructions): use the X* strings.
/// Lua (dialogue node conditions and scripts): use the L* strings.
/// Every flag is recomputed at the start of each story step by <see cref="Sync"/>.
/// </summary>
internal static class WarFlags
{
    public const string Declared = AnDeclaration.VarDeclared;            // war declared at the Turn 4 AN (stays true after the war)
    public const string Active = WarState.VarActive;                     // being fought now
    public const string Over = "ZilleWar.War_Over";                      // declared and finished, any outcome
    public const string Liberated = ZilleCampaign.VarLiberated;          // all objectives taken
    public const string Defeated = ZilleCampaign.VarDefeated;            // an operation failed
    public const string Ceasefire = WarFlow.VarCeasefire;                // the King sought a ceasefire
    public const string ZilleCityHeld = "ZilleWar.ZilleCity_Held";       // Zille city in Rizian hands now
    public const string TwoWars = "ZilleWar.TwoWars";                    // the Pales war and the Zille war at the same time
    public const string PalesWar = "RiziaDLC.Pales_War";                 // vanilla

    private static readonly string[] Bools = { Declared, Active, Over, Liberated, Defeated, Ceasefire, ZilleCityHeld, TwoWars };

    /// <summary>Rizian border towns Wehlen holds now (true while occupied; after an armistice they can stay occupied).</summary>
    public static readonly string[] RizianTowns = { "Mave Nortus", "Dapalle", "Balle", "Jales" };
    public static string Occupied(string town) => $"ZilleWar.{town.Replace(" ", string.Empty)}_Occupied";

    // ---- expresso
    public static string X(string v, bool value = true) => $"{v} == {(value ? "true" : "false")}";
    public static readonly string XPeace = X(Declared, false);           // vanilla scene untouched when there was never a war
    public static readonly string XDeclared = X(Declared);
    public static readonly string XActive = X(Active);

    // ---- Lua
    public static string L(string v) => $"Variable[\"{v}\"] == true";
    public static string LNot(string v) => $"Variable[\"{v}\"] ~= true";
    public static readonly string LDeclared = L(Declared);
    public static readonly string LPeace = LNot(Declared);
    public static readonly string LActive = L(Active);

    public static void Register()
    {
        foreach (string v in new[] { Over, ZilleCityHeld, TwoWars })
        {
            Variables.Register(v);
        }

        foreach (string v in Bools)
        {
            Vanilla.Expose(v);
        }

        foreach (string town in RizianTowns)
        {
            Variables.Register(Occupied(town));
            Vanilla.Expose(Occupied(town));
        }
    }

    /// <summary>Recompute derived flags and make sure none is nil.</summary>
    public static void Sync()
    {
        bool declared = Variables.GetBool(Declared);
        bool active = WarFlow.AtWar;
        Variables.Set(Over, declared && !active);
        Variables.Set(ZilleCityHeld, declared && Variables.GetBool(ZilleOperation.CapturedVar("Zille")));
        Variables.Set(TwoWars, active && Variables.GetBool(PalesWar));
        Dictionary<string, bool> owners = declared ? ZilleCampaign.CurrentOwners() : null;
        foreach (string town in RizianTowns)
        {
            Variables.Set(Occupied(town), owners != null && ZilleCampaign.IsEnemy(owners, ZilleCampaign.TownTile(town)));
        }


        // Smolak's AN Threat Mitigation Protocol ("Zille stays Wehzek until the threat is over") is answered by the declaration of
        // war; the vanilla AN ending switches it on after our declaration, so it is cleared on every step from then on.
        if (declared)
        {
            Variables.Set("RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol", false);
        }
        foreach (string v in Bools)
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = false end");
        }
    }
}
