using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Vanilla news and reports that a war with Wehlen contradicts.
/// - Hidden (<see cref="Vanilla.HideNews"/> / <see cref="Vanilla.HideReport"/>): items that only make sense if Rizia and Wehlen
///   are talking, trading or allied (the Turn 5 deal, the Turn 8 handover, Smolak joining the Pales war...). They come back
///   if the war was never declared.
/// - Rewritten in place: items whose event still happens in a war playthrough but whose wording assumes peace (the AN
///   announcement, the Aureus ruling, the intelligence hub, the Lespian evidence...).
/// Items EarlyT4..T7 and LateState already hide or replace (the Rumburg denunciation, the Aureus ruling, the Derdian
/// border, the intelligence hub, the Turn 8 occupation stories, the Turn 9 Wehlen reports...) are left to them.
/// </summary>
internal static class WorldPress
{
    private static bool Declared() => WorldSystems.Declared;
    private static bool NotLiberated() => WorldSystems.Declared && !WorldSystems.Liberated;

    /// <summary>Vanilla news written for a Rizia at peace with Wehlen (hidden once the war is declared).</summary>
    private static readonly string[] PeacetimeNews =
    {
        // the Turn 5 talks with Smolak never happen
        "Turn05_G_WehlenDeal", "Turn05_G_WehlenNoDeal", "Turn05_RH_WehlenDeal", "Turn05_RH_WehlenNoDeal", "Turn05_G_WehlenArbitrationSupport",
        // Wehzek medicine during the Pales war
        "Turn07_RH_War_WehlenDrugs_Civilian", "Turn07_RH_War_WehlenDrugs_Military",
        // the Turn 8 handover, the AN referendum, mediation between Derdia and Wehlen
        "Turn08_G_Decision_MediateDerdiaWehlen_Mediate", "Turn08_G_ZilleAN_NoReferendum", "Turn08_G_ZilleAN_Referendum", "Turn08_G_ZilleAN_Return",
        "Turn08_SecurityBrief_ZilleTransfer", "Turn08_ZilleNegociations_ZilleReturnFailure", "Turn08_ZilleNegociations_ZilleReturnSuccess",
        // Smolak joins the Pales war, the Wehlen-Derdia oil deal, Wehzek help in the Pales victory
        "Turn09_G_SmolakCall_WehlenJoinsWar", "Turn09_RH_SmolakCall_WehlenJoinsWar", "Turn09_V_SmolakCall_WehlenJoinsWar",
        "Turn10_RH_PalesVictory_Wehlen", "Turn10_V_PalesVictory_Wehlen",
        // timber bought from Wehlen for the Sallabes sanctuary
        "Floating_Decision_G_SallabesGrandsanctuary_UtilizeWehlenTimber", "Floating_Decision_RH_SallabesGrandsanctuary_UtilizeWehlenTimber",
        "Floating_V_Sallabes_SanctuaryExpansion_WehlenTimber",
    };

    // Turn 8-10 reports (Intermerkopum, the Smolak call, the Wehlen pipeline...) are hidden by LateState.
    private static readonly string[] PeacetimeReports = { "Turn_08_Zille_ReferendumPreparationsStart", "Turn10_Zille_WehzekExodus" };

    /// <summary>Turn 8+ rows of the occupation story (peacetime Zille under Wehzek rule).</summary>
    private static readonly (string ci, string row)[] OccupationRows =
    {
        // the referendum's exodus; the other Turn 8 occupation items are hidden by LateState (some only once Zille is ours)
        ("NarrativeSupport_Zille_Part3", "Reports_Turn10_Zille_WehzekExodus"),
    };

    public static void Register()
    {
        foreach (string n in PeacetimeNews)
        {
            Vanilla.HideNews(n, Declared);
        }

        foreach (string r in PeacetimeReports)
        {
            Vanilla.HideReport(r, Declared);
        }

        // "now that Zille is returned to Rizia" (Turn 10 birthday); the Turn 9 one is LateState's
        Vanilla.HideNews("Turn10_RH_Birthday_ZilleBrenasChange", NotLiberated);
        // A promise of Zille (and Nanta's port) to House Sazon cannot be kept unless all four cities are Rizian: after a lost or a
        // divided war the birthday handover (ownership flags, map tokens, journal, news) is skipped, as if no promise had been made.
        // Node 176 would otherwise hand Wehzek Nanta to House Sazon. A peace game never reaches this (War_Declared is false).
        Vanilla.AppendScript("Rizia/Turn 10/Birthday_Gathering", 169,
            "if Variable[\"ZilleWar.War_Declared\"] == true and Variable[\"ZilleWar.War_Zille_Liberated\"] ~= true then Variable[\"RiziaDLC.Zille_PromisedToHouseSazon\"] = false end");

        foreach ((string ci, string row) in OccupationRows)
        {
            Vanilla.GateInstruction(ci, row, WarFlags.XPeace);
        }
    }

    public static void OnStep()
    {
        bool declared = WorldSystems.Declared;
        Rewrite("news", "Turn04_G_AN_WehlenAnnoucement", declared, null,
            "At an eventful South Merkopa-focused meeting of the Alliance of Nations, Wehlen showed that it was willing to go to " +
            "extreme lengths to keep its hold on the Rizian region of Zille, invoking a little-cited AN protocol to delay the expected handover.\n\n" +
            "President Wiktor Smolak claimed that national security in Zille was still too compromised to allow for an orderly " +
            "transition of the region. Though highly unusual, the announcement was within his legal rights. It was also the last " +
            "thing said in the hall before King Romus Toras rose to answer it with a declaration of war, and the Alliance now has " +
            "a war between two of its members to answer for.");
        Rewrite("news", "Turn04_RH_WehlenAnnouncement", declared, "Zille Waits for Liberation",
            "Royal Herald correspondents in Zille say that the region's population was shocked by Wehlen president Wiktor Smolak's " +
            "announcement that the region would not be returned to Rizia on the agreed-upon handover date.\n\n" +
            "Local residents of Zille, many of whom are of Rizian descent and had been anticipating a reunion with the Kingdom, " +
            "listened to the rest of the broadcast from Konstantium in disbelief, then in tears, as His Majesty answered Smolak with " +
            "a declaration of war. By nightfall, Wehzek patrols were confiscating radios in the old town.\n\n" +
            "The people of Zille have waited twenty-five years. They will not wait much longer.");
        Rewrite("news", "Turn04_G_AN_Hegel", declared, null,
            "What should have been a calm, peaceful Alliance of Nations session exclusively focusing on the issues of the South " +
            "Merkopa region turned into a grandstanding platform for Valgslandian chancellor Emmerich Hegel, who used the opportunity " +
            "to make multiple interruptions.\n\n" +
            "Hegel was ostensibly present because of his country's new relationship with the Republic of Morella, whose leader Alma " +
            "Saltana spoke at the proceedings. However, he seemed more interested in weighing in on Palesian-Rizian relations, with " +
            "one particular outburst directed at Palesian Duke Axel Reinhart.\n\n" +
            "By the end of the day, his interruptions were the least of the session's scandals. After King Romus Toras declared war " +
            "on Wehlen from the podium, the Chancellor was among the first to leave the hall, telling reporters that Valgsland " +
            "\"will not forget who spoke for the King tonight.\"");
        Rewrite("news", "Turn08_G_LespiaEvidenceReleased", declared, null,
            "Rizia's Toras government has leveled serious allegations against Lespia in connection with the 1950 Friendship Day " +
            "bombing in Zille. Newly released evidence hints at links between the Alvarez administration and certain individuals and " +
            "companies proven to be connected to the tragic incident, which claimed the lives of three Wehzeks and which Smolak " +
            "cited when he refused to hand Zille back at the lease's expiry, a refusal that led in the end to a war that has since cost " +
            "thousands of lives.\n\n" +
            (WorldSystems.LespiaHelps
                ? "The allegations are all the more remarkable because Lespian warships patrolled the Strait of Kreleck on Rizia's behalf " +
                  "during the war. Prime Minister Alvarez's office has thus far chosen not to comment. "
                : "Prime Minister Alvarez's office has thus far chosen not to comment, awaiting all available facts before making a statement. ") +
            "In Wraclawitz, President Smolak has demanded an apology from Porte Drazon, \"and from the Alliance that would not listen.\"");
        Rewrite("news", "Turn08_Decision_RH_SurplusOfMedicine_DistributeToPublic", declared, null,
            "The Crown has taken a decisive step to prioritize public health by distributing surplus medical supplies to communities " +
            "in need. This positive development is the result of the Kingdom's earlier investments in the pharmaceutical sectors of " +
            "Bronaz and Valenqir.\n\n" +
            "With discounted access to essential treatments, citizens across our country can now benefit from improved healthcare " +
            "services. Medical experts praise the initiative as a landmark effort to reduce healthcare inequality, while residents " +
            "express gratitude for the government's swift action.\n\n" +
            "By turning surplus into support, the Crown continues to demonstrate its commitment to the well-being of every citizen.");
        Rewrite("news", "RD_RH_BuildMaveNortusIntelligenceHub", declared, null,
            "In a very vague communique issued by the Palace, plans were unveiled for new funds to be allocated to Rizia's " +
            "intelligence services. Mentions were made of a new base of operations, although the government withheld the exact " +
            "details due to national security considerations.\n\n" +
            (WorldSystems.Liberated
                ? "It was highlighted that the most important mission of the administration is now the defence of Zille, and that the "
                : "It was highlighted that the Zille question remains the most important matter of the administration, and that the ") +
            "expanded intelligence operations will save the lives of our soldiers at the front.\n\n" +
            "We at the Royal Herald are incredibly excited for all the spy thriller stories that surely will be written about the " +
            "exploits of Rizian operatives many years from now.");
    }

    private static void Rewrite(string kind, string name, bool when, string title, string text)
    {
        if (kind == "news")
        {
            if (when)
            {
                WorldText.News(name, title, text);
            }
            else
            {
                WorldText.RestoreNews(name);
            }
        }
        else if (when)
        {
            WorldText.Report(name, title, text);
        }
        else
        {
            WorldText.RestoreReport(name);
        }
    }
}
