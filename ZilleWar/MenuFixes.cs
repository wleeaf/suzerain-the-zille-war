namespace ZilleWar;

/// <summary>
/// Mod choices join vanilla menus at normal priority, so vanilla options that used to be hidden by accident now show in war games.
/// This hides the ones that are false or duplicate a mod choice in the state where the mod choice shows (war games only).
/// </summary>
internal static class MenuFixes
{
    public static void Register()
    {
        // The Zille front exists (war fought, or an armistice line through Zille): same test as the Manus choices in FinishManus.
        string front = $"({LK.Ongoing}) or ({LK.Part})";
        string sendOpen = "Variable[\"RiziaDLC.SuOmina_OrderedToKillManus\"] ~= true and Variable[\"RiziaDLC.Manus_Arrested\"] ~= true";

        // "Such as ordering him to fight in Pales?" duplicates the Zille choice "Such as ordering him to fight in Zille?".
        Vanilla.Require("Rizia/Turn 7/A_HugoManusPrivateTalk", 538, $"not ({front})");
        // "I could always ship him off to the frontline in Pales." duplicates "...to the front in Zille".
        Vanilla.Require("Rizia/Turn 7/A_HugoManusPrivateTalk", 1505, $"not ({front})");
        // "I am sending you to lead your province's battalion in Pales." duplicates the Zille call that sets the Manus-in-Zille state.
        Vanilla.Require("Rizia/Turn 7/A_HugoManusPrivateTalk", 513, $"not (({front}) and {sendOpen})");

        // "Valgsland will support Rizia getting a larger profit share": in a war Hegel only threatens (embargo, Lespia, a sanctions motion), he promises no support.
        Vanilla.Require("Rizia/Turn 5/FPnT_MITZHegelCall", 612, WarFlags.LPeace);

        // "I am hoping this will reflect well for peace in the region": Rizia is fighting Wehlen while the council sits.
        Vanilla.Require("Rizia/Turn 6/A_CouncilMeeting", 145, $"not ({LK.Ongoing})");
    }
}
