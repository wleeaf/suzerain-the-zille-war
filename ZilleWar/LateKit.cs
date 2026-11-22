namespace ZilleWar;

/// <summary>
/// Shared vocabulary of the late-game module (Turns 8 to 10, the endings): the war's outcome as Lua conditions,
/// and a helper that swaps one vanilla line or choice for per-outcome war versions.
/// Outcomes partition every game where war was declared:
/// - Ongoing: still being fought.
/// - Lib: Zille liberated (all objectives taken).
/// - Part: war over, not liberated, but Zille city is in Rizian hands (a ceasefire, or a lost war that kept the city).
/// - Lost: war over and Zille city is Wehzek again.
/// </summary>
internal static class LK
{
    public static readonly string War = WarFlags.LDeclared;
    public static readonly string Peace = WarFlags.LPeace;
    public static readonly string Ongoing = WarFlags.LActive;
    public static readonly string Lib = WarFlags.L(WarFlags.Liberated);
    public static readonly string Part = $"{WarFlags.L(WarFlags.Over)} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.L(WarFlags.ZilleCityHeld)}";
    public static readonly string Lost = $"{WarFlags.L(WarFlags.Over)} and {WarFlags.LNot(WarFlags.Liberated)} and {WarFlags.LNot(WarFlags.ZilleCityHeld)}";
    public static readonly string NotLib = $"{WarFlags.LDeclared} and {WarFlags.LNot(WarFlags.Liberated)}";
    public static readonly string Ceasefire = WarFlags.L(WarFlags.Ceasefire);
    public static readonly string PalesWar = WarFlags.L(WarFlags.PalesWar);
    public static readonly string Auremina = WarFlags.L(ZilleOperation.CapturedVar("Auremina"));

    // vanilla security councilor (Lucita can be arrested in Turn 6)
    public static readonly string CouncilLucita = "Variable[\"RiziaDLC.Council_Security_Lucita\"] == true and Variable[\"RiziaDLC.Lucita_Arrested\"] ~= true";
    public static readonly string CouncilTaddeus = $"Variable[\"RiziaDLC.Council_Security_Taddeus\"] == true and not ({CouncilLucita})";
    public static readonly string CouncilTitus = $"not ({CouncilLucita}) and not ({CouncilTaddeus})";

    public static string And(params string[] c) => string.Join(" and ", c.Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})"));

    public static string V(string name) => Talk.V(name);

    public static string Num(string name) => $"(Variable[\"{name}\"] or 0)";

    /// <summary>Lua: add to a vanilla number that may still be nil.</summary>
    public static string Add(string name, int delta) => $"Variable[\"{name}\"] = (Variable[\"{name}\"] or 0) + ({delta}); ";

    public static string Set(string name, string luaValue) => $"Variable[\"{name}\"] = {luaValue}; ";

    /// <summary>Per-outcome texts. A null text keeps the vanilla entry for that outcome.</summary>
    public sealed record S(string Ongoing = null, string Lib = null, string Part = null, string Lost = null)
    {
        public static S All(string text) => new(text, text, text, text);
        public static S NotLib(string text) => new(text, null, text, text);
    }

    /// <summary>
    /// Replace a vanilla line or choice with per-outcome war versions. Each version hangs off the vanilla parents
    /// (condition-gated, taken first) and continues to <paramref name="next"/>. The vanilla entry is hidden only for the
    /// outcomes that have a text, so a missing outcome falls back to vanilla instead of a dead end.
    /// </summary>
    public static void Swap(Talk t, string conversation, string name, int entry, int[] parents, string speaker, S s, string[] next,
        string script = null, string when = null, string cost = null)
    {
        var conds = new List<string>();

        void Add1(string suffix, string state, string text)
        {
            if (text == null)
            {
                return;
            }

            string c = And(state, when);
            conds.Add(c);
            if (speaker is Talk.Romus or Talk.RomusAction)
            {
                t.Choice(name + suffix, text, next, c, script, cost: cost, hooks: Talk.From(parents));
            }
            else
            {
                t.Line(name + suffix, speaker, text, next, c, script, hooks: Talk.From(parents));
            }
        }

        Add1("_On", Ongoing, s.Ongoing);
        Add1("_Lib", Lib, s.Lib);
        Add1("_Part", Part, s.Part);
        Add1("_Lost", Lost, s.Lost);
        if (conds.Count > 0)
        {
            Vanilla.Require(conversation, entry, $"not ({string.Join(" or ", conds.Select(x => $"({x})"))})");
        }
    }

    /// <summary>The same line spoken by whoever holds the security council seat.</summary>
    public static void Councilor(Talk t, string name, string text, string[] next, string condition = null, string script = null,
        string lucita = null, string taddeus = null, string titus = null)
    {
        t.Line(name + "_L", "Lucita Azaro", lucita ?? text, next, And(CouncilLucita, condition), script);
        t.Line(name + "_Ta", "Taddeus Azaro", taddeus ?? text, next, And(CouncilTaddeus, condition), script);
        t.Line(name + "_Ti", "Titus Gordion", titus ?? text, next, And(CouncilTitus, condition), script);
    }

    /// <summary>Node names of a <see cref="Councilor"/> line, for "next" lists.</summary>
    public static string[] C(string name) => new[] { name + "_L", name + "_Ta", name + "_Ti" };

    public static string[] Cat(params object[] parts) =>
        parts.SelectMany(p => p switch { string s => new[] { s }, string[] a => a, _ => Array.Empty<string>() }).ToArray();
}
