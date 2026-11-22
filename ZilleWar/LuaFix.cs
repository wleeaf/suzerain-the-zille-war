namespace ZilleWar;

/// <summary>
/// The game's Lua interpreter fails (NullReferenceException) on a unary "not" after a binary operator, e.g.
/// "A and not (B)". Every Lua string the mod hands to the game goes through <see cref="Rewrite"/>, which turns
/// "not (E)" into "((E) ~= true)": the same result for booleans and nil, without a unary "not".
/// </summary>
internal static class LuaFix
{
    public static string Rewrite(string lua)
    {
        if (string.IsNullOrEmpty(lua) || !lua.Contains("not", StringComparison.Ordinal))
        {
            return lua;
        }

        var sb = new System.Text.StringBuilder(lua.Length + 16);
        int i = 0;
        bool inString = false;
        while (i < lua.Length)
        {
            char c = lua[i];
            if (c == '"' && (i == 0 || lua[i - 1] != '\\'))
            {
                inString = !inString;
                sb.Append(c);
                i++;
                continue;
            }

            if (!inString && IsWord(lua, i, "not"))
            {
                int j = i + 3;
                while (j < lua.Length && char.IsWhiteSpace(lua[j]))
                {
                    j++;
                }

                if (j < lua.Length && lua[j] == '(')
                {
                    int close = Matching(lua, j);
                    if (close > j)
                    {
                        string inner = Rewrite(lua.Substring(j + 1, close - j - 1));
                        sb.Append("((").Append(inner).Append(") ~= true)");
                        i = close + 1;
                        continue;
                    }
                }
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    private static bool IsWord(string s, int i, string w) =>
        string.CompareOrdinal(s, i, w, 0, w.Length) == 0 &&
        (i == 0 || !(char.IsLetterOrDigit(s[i - 1]) || s[i - 1] == '_')) &&
        (i + w.Length >= s.Length || !(char.IsLetterOrDigit(s[i + w.Length]) || s[i + w.Length] == '_'));

    private static int Matching(string s, int open)
    {
        int depth = 0;
        bool inString = false;
        for (int k = open; k < s.Length; k++)
        {
            char c = s[k];
            if (c == '"' && s[k - 1] != '\\')
            {
                inString = !inString;
            }
            else if (!inString && c == '(')
            {
                depth++;
            }
            else if (!inString && c == ')' && --depth == 0)
            {
                return k;
            }
        }

        return -1;
    }
}
