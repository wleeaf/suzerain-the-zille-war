using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Audit of the vanilla story of Turns 9 and 10, the endings and the epilogue against the Zille war (see <see cref="Audit"/>).
/// The war is always over by Turn 9 (liberated, divided or lost), so most fixes live in the files that already own those
/// scenes (LateT9, LateT10, FinishT10, SweepLateTalk, SweepLateSystems); this class holds the edits no earlier pass made:
/// - Turn 9 blackout (c449): the councilors' advice to "keep the culprit vague" remembers that, after Zille, every Rizian
///   already knows where to look.
/// </summary>
internal static class AuditT910
{
    private const string NatDes = "Rizia/Turn 9/Pipeline_NatDesMeeting";

    public static void Register()
    {
        var t = new Talk(NatDes, "ZilleWar.A910.NatDes.");
        t.Variant("VagueHugo", 1638, new[] { 1636 }, War, "Hugo Toras",
            "\"Keeping it vague will prevent any diplomatic fallout. After Zille, the people will supply the culprit's name themselves.\"",
            new[] { "#1653" });
        t.Variant("VagueLucita", 1639, new[] { 1634 }, War, "Lucita Azaro",
            "\"This lets us control the narrative without handing Wehlen a fresh grievance to air in Konstantium. After Zille, the people will draw their own conclusions.\"",
            new[] { "#1653" });
        t.Variant("VagueTitus", 1640, new[] { 1637 }, War, "Titus Gordion",
            "\"Keeping it vague will prevent diplomatic fallout. After Zille, nobody will need to be told where to look.\"",
            new[] { "#1653" });
        t.Variant("VagueTaddeus", 1645, new[] { 1635 }, War, "Taddeus Azaro",
            "\"This avoids retaliation from abroad. And after Zille, the public will look north without being told to.\"",
            new[] { "#1653" });
        t.Register();
    }

    public static void OnStep()
    {
    }
}
