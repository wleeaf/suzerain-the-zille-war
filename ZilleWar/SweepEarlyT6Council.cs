using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Turn 6 council and the world's reaction (EarlyT6 covers Wehlen's arbitration vote and the two-front warnings):
/// - the council meeting (c361): the King's opening quip, "the path of peace", Vina's "you decided to start a war" and the
///   "generous neighbor" image all know there is already a war in Zille;
/// - decisions: the Vendonesam expeditionary force, Su Omina's patrols and the superpower trade war windfall;
/// - the press: Geopolitico's "first armed conflict since Pales" is no longer true, the Herald's recruitment drive, Taddeus on
///   two wars, ATO and CSP on a second war in one year, and the Queen of Rumburg's congratulations.
/// </summary>
internal static class SweepEarlyT6Council
{
    private const string Council = "Rizia/Turn 6/A_CouncilMeeting";
    private const string Vina = "Vina Toras";

    public static void Register()
    {
        CouncilMeeting();
        Decisions();
        Press();
    }

    public static void OnStep()
    {
    }

    // ------------------------------------------------------------------ c361

    private static void CouncilMeeting()
    {
        var t = new Talk(Council, "ZilleWar.SE.Council.");

        // [59] "Good day everybody. I hope there's nothing too crazy to worry about today."
        t.Variant("Quip", 59, new[] { 18 }, Active, Romus,
            "\"Good day, everybody. Unless it comes from the front, I hope there's nothing too crazy to worry about today.\"", new[] { "#243" });

        // [143] "Thank you, Mr. Esquibel, for helping us stay on the path of peace."
        t.Variant("OneWar", 143, new[] { 137 }, Active, Romus,
            "\"Thank you, Mr. Esquibel. One war at a time is quite enough for this kingdom.\"", new[] { "#848" });

        // [690] Vina: "And I also know it was you who singlehandedly decided to start a war, Father."
        t.Variant("SecondWar", 690, new[] { 689 }, Active, Vina,
            "\"And I also know it was you who singlehandedly decided to start a second war, Father. Brenas hasn't finished burying the " +
            "dead of the first.\"", new[] { "#698" });

        // [873] "Rizia is a generous neighbor that negotiates peaceful compromises. That's the image I want to project to the world."
        t.Variant("Image", 873, new[] { 871, 872 }, Declared, Romus,
            "\"Rizia fights where it must and makes peace where it can. That's the image I want to project to the world.\"", new[] { "#875" });
        t.Register();
    }

    // ------------------------------------------------------------------ decisions

    private static void Decisions()
    {
        Decision("Turn06_Decision_VendonesamTroopRequest", o => !IsActive ? null : o.TrimEnd() +
            "\n\nThe general staff has objected in writing. With the northern army committed in Zille, it says, every division sent to " +
            "Vendonesam is a division taken from the front.");
        Decision("Turn06_Decision_SuOminaDestabilization_PenaltyWarning", o => !IsActive ? null : o.TrimEnd() +
            " Since the war began, the patrols claim to be hunting Wehzek spies. Several of the men they have detained turned out to be " +
            "Wehzek-speaking Rizian citizens.");
        Decision("Turn06_Decision_SuperpowerTradeWar_Penalty", o => !IsActive ? null : BeforeTag(o,
            "For a treasury paying for a war in Zille, the windfall could hardly have come at a better time."));
    }

    // ------------------------------------------------------------------ press

    private static void Press()
    {
        News("Turn06_G_Decision_VendonesamTroopRequest_Troops", (s, _) => P(
            "By committing troops to fight on the side of Vendonesam's besieged Prince Consort, Rizia has thrown itself into its second " +
            "armed conflict this year, " + By(s,
                "while its northern army is still at war with Wehlen over Zille.",
                "only weeks after taking Zille from Wehlen by force.",
                "only weeks after its war with Wehlen over Zille ended without victory."),
            "It remains to be seen whether or not the Rizian troops can turn the tide of the war in favor of its traditional ally, with " +
            "the situation becoming ever more dire for the Confederacy. At the very least it signals Rizia's commitment to GRACE, and, " +
            "observers in Konstantium note, a kingdom that has rediscovered its taste for sending soldiers abroad."));

        AppendNews("Turn06_V_Decision_VendonesamTroopRequest_Troops", s => By(s,
            "And all this while our sons are still dying in Zille. How many wars does one King need?",
            "Zille was not enough, it seems. The King has found another war to send our sons to.",
            "Zille taught the King nothing. He has found another war to send our sons to."));

        News("Turn06_RH_RecruitementDrive", (s, _) => P(
            "Citizens! Patriots!",
            By(s,
                "Our brave soldiers are fighting in Zille as you read these lines, and more and more actors question Rizia's sovereignty.",
                "Our soldiers have brought Zille home, but more and more actors question Rizia's sovereignty.",
                "Our soldiers gave their all in Zille, and more and more actors question Rizia's sovereignty.") +
            " We at the Royal Herald call upon any able bodied men willing to serve his King and country to enlist at their local " +
            "recruitment bureau! We must do our duty and ensure our country is able to defend itself, wherever it is challenged.",
            "A career in the military is a prestigious – and financially beneficial – endeavor. Large pensions are given to former " +
            "soldiers and a significant sign up bonus calculated based on age will be issued for any sign ups within the next week.",
            "Glovurius Axa Rizia!"));

        AppendNews("Turn06_RH_GeneralAzaroOnWar", _ =>
            "Asked about the war in Zille, the General was blunter still. \"Zille must be finished before Pales is begun,\" he told the " +
            "Herald. \"I served a king who tried to do too much at once. I would not see his son make the same mistake.\"",
            s => s == StActive);

        AppendReport("Turn06_ATO_CondemnsWar", s => AuditT6.ByP(s,
            "The statement notes that this is the second war the Kingdom has started this year, while its army is still fighting Wehlen " +
            "over Zille, and that several member states are reviewing their arms sales to Rizia.",
            "The statement notes that this is the second war the Kingdom has started this year, after its seizure of Zille from Wehlen, " +
            "and that several member states are reviewing their arms sales to Rizia.",
            "The statement notes that this is the second war the Kingdom has started this year, after a war with Wehlen that left Zille " +
            "divided along an armistice line, and that several member states are reviewing their arms sales to Rizia.",
            "The statement notes that this is the second war the Kingdom has started this year, after its failed war with Wehlen over " +
            "Zille, and that several member states are reviewing their arms sales to Rizia."));

        AppendReport("Turn06_CSP_CondemnsWar", s => By(s,
            "CSP has also renewed its demand that Rizia withdraw its forces from Zille, calling the Kingdom \"a danger to every one of " +
            "its neighbors.\"",
            "CSP also repeated that it does not recognize Rizian rule over Zille, calling the Kingdom \"a danger to every one of its " +
            "neighbors.\"",
            "CSP called the Kingdom, which has now attacked two of its neighbors in a single year, \"a danger to every one of them.\""));

        Report("Turn06_Rumburg_CongratulateRNCWin", (s, _) => P(
            "“Dear Romus,",
            "On behalf of the Kingdom of Rumburg, I would like to extend my official congratulations with the victory of the RNC in " +
            "your elections. It warms my heart to see just how much your people support you.",
            AuditT6.ByP(s,
                "Now do try to win the other thing as well. Rumburg's patience is considerable, but its oil is not free, and neither, " +
                "I am afraid, am I.",
                "And congratulations on Zille, of course. An election and a war in one season. Some would call that showing off. I " +
                "call it a good year.",
                "As for Zille, half a province is better than none, I suppose. Do hold on to the half you have, my dear. Maps are so " +
                "tiresome to redraw.",
                "As for Zille, I am told it was not your fault. I am told a great many things.") + "”"));
    }
}
