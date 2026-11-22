using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// The Pales crisis of Turn 6 read through the Zille war that came just before it. Every vanilla news item about the border
/// incident, the clash at Aureus, the King's journey to Remaz and the talks that followed gets a closing paragraph in its
/// paper's own voice: the Royal Herald proud or defiant, the Voice of Rizia counting the cost, Geopolitico weighing what
/// Konstantium makes of a kingdom that has just fought one war. Each one follows the outcome in Zille (won, divided, lost) and,
/// when the war somehow still runs, says so.
/// </summary>
internal static class PalesIncidentPress
{
    private static string By(string s, string active, string won, string part, string lost) => AuditT6.ByP(s, active, won, part, lost);

    /// <summary>
    /// Whether Lespia arms Pales (vanilla War_Pales_LespianSupport, decided in the Turn 6 war-eruption briefing at entry 48).
    /// A Lespia that buys Rizia's Aureus gas wants Rizia to hold the field: it stays out (or keeps it small, if it holds a
    /// grievance). A Lespia with naval rights or a lease at Caleqabiz wants Rizia weak at sea: it keeps arming Pales, fully if it
    /// holds a grievance. In a war game only.
    /// </summary>
    private static void LespianSupport()
    {
        const string v = "Variable[\"RiziaDLC.War_Pales_LespianSupport\"]";
        string gas = $"(Variable[\"{SupplyWar.VarLespiaGasDeal}\"] == true or Variable[\"RiziaDLC.Diplomacy_Lespia_GasField_25PercentDeal\"] == true)";
        string caleq = $"(Variable[\"{SupplyWar.VarLespiaCaleqabiz}\"] == true or Variable[\"{SupplyWar.VarLespiaLease}\"] == true or " +
                       "Variable[\"RiziaDLC.Policy_Diplomacy_Caleqabiz_LespiaATO\"] == true)";
        string grudge = $"Variable[\"{SupplyWar.VarLespiaGrievance}\"] == true";
        Vanilla.AppendScript("Rizia/Turn 6/SnO_WarEruptionBriefing", 48,
            $"if {WarFlags.LDeclared} then " +
            $"if {caleq} then if {grudge} then {v} = 2 elseif ({v} or 0) < 1 then {v} = 1 end " +
            $"elseif {gas} then if {grudge} then {v} = 1 else {v} = 0 end end end");
    }

    public static void Register()
    {
        LespianSupport();

        // ---- the land incident at Fort Ales
        AppendNews("Turn06_RH_LandIncident", s => By(s,
            "Reinhart has chosen his moment with a coward's care, striking in the south while our army is still engaged in Zille. He will find that Rizia can fight with both hands.",
            "Perhaps the Duke believed an army that has just liberated Zille would be too tired to answer him. Perhaps he should ask Wraclawitz how that worked out.",
            "Perhaps the Duke believed an army still standing guard on the Zille line would have no strength to spare. The men who took back Zille city would be glad to show him otherwise.",
            "The Duke has mistaken a hard season in Zille for weakness. Rizia has buried its sons this year; it has not forgotten how to defend its borders."));
        AppendNews("Turn06_V_LandIncident", s => By(s,
            "And all of this while our army is still fighting in Zille. A country cannot keep a war at each end of its map without someone deciding it looks like a target.",
            "We are told the victory in Zille has made Rizia feared. It seems it has also made Rizia a country that everyone now expects to fight, and that every neighbor now watches for the next war.",
            "The ink on the Zille armistice is hardly dry. Brenas is still counting its dead. If this incident becomes a second war, the same families will pay for it twice.",
            "Rizia has just lost a war in Zille. If the Palace now looks for a victory in the south to make up for it, the families of Brenas will pay for that, too."));
        AppendNews("Turn06_G_LandIncident", s => By(s,
            "The incident comes while Rizia is still at war with Wehlen over Zille. Diplomats in Konstantium are asking whether Remaz hoped to catch the Rizian army looking north.",
            "The incident comes only weeks after Rizia retook Zille from Wehlen by force. \"After Zille, nobody in Konstantium will believe this ends with a protest note,\" one diplomat said.",
            "The incident comes only weeks after the armistice that divided Zille between Rizia and Wehlen. Diplomats note that the Rizian army has rarely been so large, or so close to anyone's border.",
            "The incident comes only weeks after Rizia's war with Wehlen over Zille ended in failure. \"A wounded king is a dangerous neighbor,\" one diplomat in Konstantium said."));

        // ---- the clash at Aureus
        AppendNews("Turn06_RH_PalesNavalAgression", s => By(s,
            "Remaz has struck at Aureus while our fleet is also covering the Zille coast. It will not catch the Royal Navy divided for long.",
            "The same navy that kept Wehzek convoys from reaching Zille now guards Aureus. Pales would do well to remember how Wehlen's convoys fared.",
            "The navy that stood guard off Zille all through the war now guards Aureus as well. It has had a great deal of practice this year.",
            "Whatever was lost in Zille, the Royal Navy has lost none of its nerve, and Pales has just discovered it."));
        AppendNews("Turn06_V_SailorsLeftToDie", s => By(s,
            "A kingdom already at war in Zille now lets foreign sailors drown off Aureus. How many wars does the Palace think it can fight before someone stops counting the dead?",
            "Victory in Zille has done nothing for the Palace's conscience. Drowning sailors are drowning sailors, whatever flag they serve.",
            "The men who died in Zille this year died for half a province. The Palesian sailors left in the water off Aureus died for nothing at all.",
            "We lost Zille this year. Must we lose our decency in the Antacean as well?"));
        AppendNews("Turn06_G_NavalIncident", s => By(s,
            "The Rizian navy is already stretched between Zille and the Antacean; analysts doubt it can hold both the strait and the blockade for long.",
            "Rizia's navy, fresh from closing the Strait of Kreleck to Wehlen, has shown it is ready to use force at sea again.",
            "Rizia's navy, which spent the Zille war shadowing Wehzek convoys, now has a second front at sea to guard.",
            "Rizia's navy, which could not change the outcome in Zille, seems determined not to lose at sea as well."));

        // ---- the King goes to Remaz, and the talks that follow
        AppendNews("Turn06_RH_KingToTravelToPales", s => By(s,
            "His Majesty leaves the war in Zille in the hands of his generals for the length of a journey. The kingdom is in good hands on both fronts.",
            "The King who brought Zille home now goes to Remaz in person. He goes as a victor, and the Duke will know it.",
            "The King who brought Zille city home now goes to Remaz in person, to show that Rizia wants peace on this border, and can enforce it.",
            "After a hard year in the north, the King goes to Remaz in person to keep the south at peace. Rizia needs no second war, and His Majesty knows it."));
        foreach (string n in new[] { "Turn06_RH_PostIncidentNoDeal", "Turn06_RH_PostIncidentTalkBreakdown" })
        {
            AppendNews(n, s => By(s,
                "Reinhart seems to have counted on an army busy in Zille. He has counted wrong.",
                "The Duke has refused to see reason. After Zille, the Herald trusts he will not mistake His Majesty's patience for weakness.",
                "The Duke has refused to see reason. The army that held Zille city stands ready to hold the south as well.",
                "The Duke seems to think Zille has weakened us. He would do well not to test that belief."));
        }

        foreach (string n in new[] { "Turn06_RH_PostIncidentDealFieldCeded", "Turn06_RH_PostIncidentDealFieldCededPartial" })
        {
            AppendNews(n, s => By(s,
                "With one war still to finish in the north, His Majesty has wisely refused to begin another in the south.",
                "A king who has just won a war has nothing to prove. His Majesty chose peace in Remaz as a victor chooses it: freely.",
                "With soldiers still guarding the Zille line, His Majesty has wisely kept the south at peace.",
                "After a hard year in Zille, His Majesty has given the kingdom what it needs most: a border without a war on it."));
        }

        foreach (string n in new[] { "Turn06_G_PostIncidentNoDeal", "Turn06_G_PostIncidentTalkBreakdown", "Turn06_G_PostIncidentDealFieldCeded",
                     "Turn06_G_PostIncidentDealFieldCededPartial" })
        {
            AppendNews(n, s => By(s,
                "Observers note that the Rizian delegation negotiated with half its army still committed in Zille.",
                "Observers note that Rizia negotiated from the strength of its victory over Wehlen in Zille, and that Remaz negotiated with that victory very much in mind.",
                "Observers note that Rizia negotiated with a fresh armistice line through Zille behind it and a great many soldiers still under arms.",
                "Observers note that Rizia negotiated weeks after losing its war with Wehlen over Zille, and that Remaz knew it."));
        }

        // ---- the build-up on the border, read after the war
        AppendNews("Turn06_G_RiziaMilitaryBuildup", s => By(s, "",
            "The army massing on the border is the same one that took Zille from Wehlen this year, and Konstantium is watching it with the same anxiety.",
            "The army massing on the border is the same one that fought Wehlen to an armistice in Zille this year, and few in Konstantium believe it has had enough.",
            "The army massing on the border is the one that lost in Zille this year. \"A defeat in the north is no guarantee of peace in the south,\" one diplomat said."),
            s => s != StActive);
    }
}
