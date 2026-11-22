namespace ZilleWar;

/// <summary>
/// Audit of the vanilla story of Turn 7 against the Zille war (see <see cref="Audit"/>). The war is always over by Turn 7
/// (liberated, divided or lost; by the King's ceasefire or by the armistice when the offensive ran out), so the Turn 7
/// edits are written per outcome. They live next to the earlier passes on the same scenes, so one vanilla entry is never
/// edited from two places: <see cref="EarlyT7"/> (House of Delegates, Turn 7 news and reports), <see cref="EarlyT7Diplomacy"/>
/// (Rumburg, Duke Reinhart, Derdia), <see cref="EarlyT7Home"/> and <see cref="SweepMiddleT7"/> (the personal scenes).
/// </summary>
internal static class AuditT7
{
    public static void Register()
    {
    }

    public static void OnStep()
    {
    }
}
