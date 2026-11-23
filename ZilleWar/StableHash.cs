namespace ZilleWar;

/// <summary>Stable FNV-1a hash: string.GetHashCode is randomized per process, so entity ids changed every launch.</summary>
internal static class StableHash
{
    internal static uint Of(string s)
    {
        uint h = 2166136261;
        foreach (char ch in s)
        {
            h = (h ^ ch) * 16777619;
        }

        return h;
    }
}
