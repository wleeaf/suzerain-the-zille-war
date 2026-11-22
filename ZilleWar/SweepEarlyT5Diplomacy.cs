using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Turn 4 (after the AN) and Turn 5 diplomacy, money and the generals, in wartime (EarlyT5 covers the MITZ board, Hegel's
/// call, Cavette and the debrief):
/// - the Morella debrief (c327): Saltana's AN pledge comes with a wartime condition;
/// - decisions whose premise the war changes: energy prices, the RNC's campaign money, the Esqiris field, the Gasom stake,
///   Derdia's AN support, ATO clearance delays;
/// - the press: General Carlos Azaro's visits (Taddeus, the Lespian arms fair) and Valgsland's submarines.
/// </summary>
internal static class SweepEarlyT5Diplomacy
{
    private const string Feedback = "Rizia/Turn 5/EnT_MorellaNegotiationFeedback";
    private const string Esquibel = "Laurento Esquibel";

    public static void Register()
    {
        var t = new Talk(Feedback, "ZilleWar.SE.Dip.");
        // [507] "Ms. Saltana has pledged that she will speak in our favor if Zille is not returned to Rizia by the next AN."
        t.Variant("Pledge", 507, new[] { 505 }, Active, Esquibel,
            "\"Ms. Saltana has pledged to speak in our favor when the AN next convenes. On one condition, she says: that by then the " +
            "war has not spread beyond Zille.\"", new[] { "#630" });
        t.Register();

        Decisions();
        Press();
    }

    public static void OnStep()
    {
    }

    private static void Decisions()
    {
        Decision("Turn04_Decision_XinaConflictEndsEnergyDrops_Penalty", o => !IsActive ? null : o.TrimEnd() +
            " With the treasury already paying for a war, the loss of export income could hardly come at a worse time.");
        Decision("Turn05_Decision_RNCElectionFundRequest", o => !IsActive ? null : o.TrimEnd() +
            " With the country at war, Baroness de Rava argues that a strong RNC result will be read abroad as a vote of confidence in " +
            "the war itself, and that the RPP will make the war its campaign if the Crown does not make it ours.");
        Decision("Turn05_Decision_EsqirisFieldShutsDown_Penalty", o => !IsActive ? null : BeforeTag(o,
            "It could hardly come at a worse moment: the army's fuel orders for the northern front have doubled since the declaration of war."));
        Decision("Turn05_Decision_GasomInvestOffer", o => !IsActive ? null : o.TrimEnd() +
            " Sordish fuel would also reach our army in the north without passing through a single Wehzek port, a point the general " +
            "staff has not failed to make.");
        Decision("Turn05_Decision_DerdiaANSupport", o => o.TrimEnd() + (IsActive && !Variables.GetBool(WarFlags.Ceasefire)
            ? " Wehlen, at war with us, will lobby the committee against our claim, and Derdia's voice would count for more than ever."
            : " Wehlen, still hostile since the fighting in Zille stopped, will lobby the committee against our claim, and Derdia's " +
              "voice would count for more than ever."));
        Decision("Turn05_Decision_WestTradeReduced_PenaltyWarning", o => !IsActive ? null : o.TrimEnd() +
            " Officials in the ATO capitals let it be known, off the record, that a kingdom at war over Zille should expect its " +
            "paperwork to be read more carefully.");
    }

    private static void Press()
    {
        News("Turn05_RH_CarlosVisitsTaddeus", (_, _) => P(
            "Several years after the stroke that left him unfit for office, General Taddeus Azaro has been discharged from the hospital " +
            "to his family estate in Cardesse-Montaklar. Among his visitors was General Carlos Robles Azaro, who left the general staff " +
            "for an afternoon to meet with him privately.",
            "Sources suggest that the old general asked for a full briefing on the war in Zille, and that the two men spent the " +
            "afternoon bent over a map of the northern front. As the sole survivor of the famous Battle of Ziqes and a decorated war " +
            "hero, General Carlos' presence at the head of the army in this hour reassures many about Rizia's future security."),
            when: s => s == StActive);

        Report("Turn05_Montaklar_CarlosHomeVisit", (_, _) => P(
            "General Carlos Robles Azaro briefly left the general staff to visit the Azaro family estate in Cardesse-Montaklar and meet " +
            "with General Taddeus Azaro, who is still recovering from his stroke.",
            "Sources indicate that the two generals spent most of the visit on the war in Zille. Despite his fragile health, General " +
            "Taddeus remains an influential figure in military affairs, and is said to have offered his advice on the Wehzek defenses " +
            "he once studied as King Valero's war councilor."),
            when: s => s == StActive);

        Report("Turn05_Montaklar_ArmsExhibitionInLespia", (_, _) => P(
            "General Carlos Azaro, accompanied by a select group, attended the Arms Industry and Equipment Exhibition at the Canavi " +
            "Exhibition Center in Lespia, where the latest advances in military technology were showcased.",
            "The visit drew more attention than usual. With Rizia at war in Zille, Lespian manufacturers lined up to meet the General, " +
            "and he is said to have returned with a list of what can be delivered before the next offensive, and at what price. His " +
            "report for His Majesty is expected shortly."),
            when: s => s == StActive);
    }
}
