using Il2CppPixelCrushers.DialogueSystem;

namespace ZilleWar;

/// <summary>
/// Self-check run once per game session: every Lua condition and script the mod wrote is compiled (not executed)
/// by the game's Lua, and every expresso condition is converted and compiled. Problems go to the log as
/// "Lua check:" lines, so one broken string cannot silently disable a scene.
/// </summary>
internal static class LuaLint
{
    private static readonly List<(string where, string lua, bool condition)> Lua_ = new();
    private static readonly List<(string where, string expresso)> Expresso = new();
    private static bool _done;
    public static readonly List<string> Problems = new();
    public static int Checked { get; private set; }

    public static void Record(string where, string condition, string script)
    {
        if (!string.IsNullOrWhiteSpace(condition))
        {
            Lua_.Add((where, condition, true));
        }

        if (!string.IsNullOrWhiteSpace(script))
        {
            Lua_.Add((where, script, false));
        }
    }

    public static void RecordExpresso(string where, string expresso)
    {
        if (!string.IsNullOrWhiteSpace(expresso))
        {
            Expresso.Add((where, expresso));
        }
    }

    public static void RunOnce()
    {
        if (_done)
        {
            return;
        }

        _done = true;
        int bad = 0;
        foreach ((string where, string lua, bool condition) in Lua_)
        {
            string wrapped = condition ? $"local __zw = function() return ({lua}) end" : $"local __zw = function() {lua} end";
            if (!Compiles(wrapped, out string error))
            {
                bad++;
                Problems.Add($"{(condition ? "condition" : "script")} of {where}: {error} | {lua}");
                Core.Log.Error($"Lua check: {(condition ? "condition" : "script")} of {where} does not compile: {error}\n    {lua}");
            }
        }

        foreach ((string where, string expresso) in Expresso)
        {
            string lua;
            try
            {
                lua = Il2Cpp.LuaConverter.ConvertExpression(expresso);
            }
            catch (Exception ex)
            {
                bad++;
                Problems.Add($"{where}: expresso '{expresso}' cannot be converted: {ex.Message}");
                Core.Log.Error($"Lua check: {where}: expresso '{expresso}' cannot be converted: {ex.Message}");
                continue;
            }

            if (lua == null || lua.Contains("ZilleWar.", StringComparison.Ordinal) && !lua.Contains("Variable[\"ZilleWar.", StringComparison.Ordinal)
                || !Compiles($"local __zw = function() return ({lua}) end", out string error2))
            {
                bad++;
                Problems.Add($"{where}: expresso '{expresso}' converts to '{lua}'");
                Core.Log.Error($"Lua check: {where}: expresso '{expresso}' converts to '{lua}', which is not valid Lua.");
            }
        }

        Checked = Lua_.Count + Expresso.Count;
        Core.Log.Msg($"Lua check: {Lua_.Count} Lua strings and {Expresso.Count} expresso conditions checked, {bad} problem(s).");
    }

    private static bool Compiles(string code, out string error)
    {
        error = null;
        try
        {
            Lua.Run(code, false, true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
