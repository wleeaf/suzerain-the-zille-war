using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// Turn-independent systems the Zille war makes false, plus the Turn 9 decisions whose premise it changes:
/// - situations: the six Zille movement/resistance cards (a peacetime movement before the war; partisans, loyalty or
///   occupation after it), Lost / Partially Restored Territorial Integrity, global image, power projection, AN sanctions,
///   Rumburg Suzerain, ATO and CSP influence, the treatment of Wehzek workers, waning combat experience, naval interdiction;
/// - policies and token badges: the Mave Nortus intelligence hub, the Labor Hands permits;
/// - royal decrees: the Mave Nortus hub, the two religious decrees that still treat Wehlen as a partner to keep happy,
///   the Labor Hands permits;
/// - Turn 9 decisions: the tourism campaign and the stock exchange feel the war (a one-time nudge of their vanilla
///   scores before the results are read), and the Alliance of Nations debate remembers Konstantium.
/// Everything goes through <see cref="SLK"/>; vanilla text comes back whenever the condition stops holding.
/// </summary>
internal static class SweepLateSystems
{
    private const string VarTourismNudged = "ZilleWar.SL_Sys_TourismNudged";
    private const string VarStockNudged = "ZilleWar.SL_Sys_StockNudged";

    private static bool War => LateState.Declared;
    private static bool Ongoing => LateState.Declared && WarFlow.AtWar;
    private static bool Lib => LateState.Liberated;
    private static bool Part => LateState.Partial;
    private static bool Lost => LateState.Lost;
    private static bool Fought => LateState.Declared && ZilleCampaign.OperationsDone >= 1;

    /// <summary>Rumburg actually helped in the Zille war (Queen Beatrice's levers or war credits).</summary>
    private static bool RumburgHelped => LateT9.RumburgHelpedNow;

    public static void Register()
    {
        Variables.Register(VarTourismNudged);
        Variables.Register(VarStockNudged);
        RegisterZilleCards();
        RegisterSituations();
        RegisterIntelHub();
        RegisterDecrees();
        RegisterDecisions();
    }

    public static void OnStep()
    {
        if (!War || LateState.Turn != 9)
        {
            return;
        }

        // Turn 9 step 7 reads the tourism campaign, step 8 the stock exchange: the war moves their scores once, beforehand.
        if (LateState.Step >= 6 && !Variables.GetBool(VarTourismNudged))
        {
            Variables.Set(VarTourismNudged, true);
            if (Variables.GetBool("RiziaDLC.Policy_RD_Economy_PorteDrazonTourismCampaign"))
            {
                LateState.Lua("Variable[\"RiziaDLC.TourismCampaign_Value\"] = (Variable[\"RiziaDLC.TourismCampaign_Value\"] or 0) - 2");
            }
        }

        if (LateState.Step >= 7 && !Variables.GetBool(VarStockNudged))
        {
            Variables.Set(VarStockNudged, true);
            int delta = Lib ? 2 : Part ? -1 : -2;
            if (Variables.GetBool("RiziaDLC.Policy_RD_Economy_FormPorteDrazonStockExchange"))
            {
                LateState.Lua($"Variable[\"RiziaDLC.PDSE_StockOpeningValue\"] = (Variable[\"RiziaDLC.PDSE_StockOpeningValue\"] or 0) + ({delta})");
            }
        }
    }

    // ------------------------------------------------------------------ the Zille movement and resistance cards

    private static void Card(string name, Func<bool> when, string title, string text) => SLK.Situation(name, when, title, text);

    private static void RegisterZilleCards()
    {
        const string small = "Diplomacy_Zille_SmallRiziaMovement", growing = "Diplomacy_Zille_GrowingRiziaMovement",
            massive = "Diplomacy_Zille_MassiveRiziaMovement", weak = "Diplomacy_Zille_WeakArmedResistance",
            growingArmed = "Diplomacy_Zille_GrowingArmedResistance", strong = "Diplomacy_Zille_StrongArmedResistance";

        // the war is being fought
        Card(small, () => Ongoing, "Few Zilleans Rally to Rizia",
            "Behind the Wehzek lines, only a small number of Zilleans have openly sided with the Rizian advance. Most keep their " +
            "shutters closed and wait to see which army will still be standing when the fighting ends.\n\nWehzek military police make " +
            "an example of anyone caught flying Rizian colors.");
        Card(growing, () => Ongoing, "Zilleans Warm to the Rizian Cause",
            "Support for Rizia is growing among the people of Zille as the Royal Army advances. Rizian flags appear in windows the " +
            "night before our columns arrive, and the bolder families bring water to the roadside.\n\nThe Wehzek garrison treats every " +
            "such gesture as treason, which only feeds the sentiment.");
        Card(massive, () => Ongoing, "Zille Rises for Rizia",
            "Across Zille, crowds greet Rizian soldiers as liberators. Strikes have paralyzed the Wehzek administration, and local " +
            "officials refuse orders from Wraclawitz.\n\nThe Wehzek garrison holds the streets by force alone, and it knows it.");
        Card(weak, () => Ongoing, "Scattered Partisans in Zille",
            "A handful of pro-Rizian partisans operate behind the Wehzek lines, cutting telephone wires and guiding our scouts through " +
            "the hills. They are too few and too poorly armed to do more.\n\nThe Royal Army cannot count on them to open the road.");
        Card(growingArmed, () => Ongoing, "Partisans Behind Wehzek Lines",
            "Armed pro-Rizian bands are striking Wehzek supply columns and depots in Zille, tying down garrison troops that would " +
            "otherwise face our army.\n\nWehzek reprisals against the villages that shelter them have only brought them more recruits.");
        Card(strong, () => Ongoing, "Zillean Partisans Fight Beside the Army",
            "Pro-Rizian partisans have become a real fighting force in Zille, ambushing Wehzek convoys and holding villages until our " +
            "columns arrive. Their knowledge of the hills is worth a regiment.\n\nWehlen calls them terrorists. Zilleans call them sons.");

        // Zille liberated: the same scores now measure the province's loyalty to the Crown
        Card(small, () => Lib, "Cold Welcome in Zille",
            "Zille has been returned to Rizia by force of arms, but few of its people celebrate. A generation grew up under Wehzek " +
            "rule, and many families have relatives on the other side of the new border.\n\nThe Crown's administrators find doors " +
            "closed and questions unanswered.");
        Card(growing, () => Lib, "Zille Warms to the Crown",
            "Rizian flags hang from more windows in Zille every month. The families who longed for reunification are finding their " +
            "voice, while the doubters keep their heads down.\n\nThe province is not yet won over, but it has stopped waiting for the " +
            "Wehzeks to come back.");
        Card(massive, () => Lib, "Zille Celebrates Its Return",
            "Zille has embraced its return to Rizia. Old men who remember the province before the lease march beside schoolchildren " +
            "on feast days, and the Crown's officials are welcomed in towns that cursed Porte Drazon a generation ago.\n\nThe " +
            "province's loyalty is the best guarantee that it will stay Rizian.");
        Card(weak, () => Lib, "Few Zillean Volunteers",
            "Few Zilleans have taken up arms for the Crown since the liberation. Policing the province and watching the Wehzek " +
            "border falls entirely to the Royal Army.\n\nThe garrison is stretched thin, and Wehzek agents find it easy to move around.");
        Card(growingArmed, () => Lib, "Zillean Volunteers Join the Garrison",
            "Local volunteers who fought beside our army during the war now patrol the hills and the harbor with the garrison. They " +
            "know every path to the border and every family that still keeps a Wehzek flag in the attic.\n\nTheir help has made the " +
            "occupation lighter, and the occupied more watchful.");
        Card(strong, () => Lib, "Zillean Militias Stand With the Crown",
            "The partisans who fought for Zille's liberation have become a loyal militia, guarding the border and rooting out Wehzek " +
            "holdouts in Isor and Nanta.\n\nSome in Porte Drazon worry about armed men who answer to their own captains before they " +
            "answer to the King.");

        // war over, Zille city ours, the rest behind an armistice line
        Card(small, () => Part, "Divided Zille, Divided Loyalties",
            "Zille city flies the Rizian flag again, but beyond the armistice line few Zilleans dare to show where they stand. The " +
            "Wehzek authorities in the occupied towns watch for anyone who looks south.\n\nEven in Zille city, many wait to see " +
            "whether the line will hold before they choose a side.");
        Card(growing, () => Part, "Zilleans Look Across the Line",
            "On the Wehzek side of the armistice line, support for reunification is growing. Families slip across at night to visit " +
            "relatives in Zille city, and come back with Rizian newspapers.\n\nThe Wehzek garrison has doubled its checkpoints, and " +
            "the Alliance's observers have started to take notes.");
        Card(massive, () => Part, "Zille Demands Reunification",
            "On both sides of the armistice line, Zilleans demand to live under one flag again. Strikes and protests in the " +
            "Wehzek-held towns have become a daily occurrence.\n\nWehlen accuses Porte Drazon of breaking the spirit of the armistice. " +
            "Porte Drazon has not tried very hard to deny it.");
        Card(weak, () => Part, "Quiet Beyond the Armistice Line",
            "Few partisans remain active in the Wehzek-held part of Zille. Those who fought with our army have come south of the line " +
            "or laid down their arms.\n\nThe armistice holds, if only because no one on the other side has the means to break it.");
        Card(growingArmed, () => Part, "Partisans Beyond the Armistice Line",
            "Armed pro-Rizian bands still strike Wehzek patrols in the occupied hills of Zille. Every raid risks the armistice, and " +
            "Wehlen blames Porte Drazon for each one.\n\nThe Alliance's observers have asked the Crown, politely, to rein them in.");
        Card(strong, () => Part, "Partisan War Beyond the Line",
            "The partisans of Zille have not accepted the armistice. Well armed and well hidden, they keep the Wehzek garrison in " +
            "Isor and Nanta under siege in all but name.\n\nWehlen threatens to resume the war if the raids continue, and the " +
            "partisans seem to hope it will.");

        // Zille city lost
        Card(small, () => Lost, "Zille Falls Silent",
            "Since the failed war, the pro-Rizian movement in Zille has all but vanished. Those who welcomed our soldiers have been " +
            "arrested, have fled south, or have learned to keep quiet.\n\nWehlen's hold on the region is tighter than it has been in " +
            "a generation.");
        Card(growing, () => Lost, "Zilleans Keep the Faith",
            "Despite the defeat, support for reunification lives on in Zille, quietly. Rizian songs are sung behind closed doors, " +
            "and the families who fled south still send letters home.\n\nThe Wehzek police know it, and wait for an excuse.");
        Card(massive, () => Lost, "Zille Refuses Defeat",
            "The failed war has not broken Zille's will. Mass protests demand the region's return to Rizia even under the guns of the " +
            "Wehzek garrison, and whole districts have become ungovernable.\n\nWehlen accuses Porte Drazon of stirring the unrest. For " +
            "once, it hardly needs to.");
        Card(weak, () => Lost, "Partisans Crushed in Zille",
            "The partisans who rose with our army have been hunted down since the defeat. Those still alive are in Wehzek prisons or " +
            "in hiding across the border.\n\nNo one in Zille will take up arms for Rizia again soon.");
        Card(growingArmed, () => Lost, "Partisans Fight On in Zille",
            "Some of the partisans who fought beside our army refused to lay down their arms after the defeat. They still strike at " +
            "Wehzek patrols in the hills, and the Wehzek garrison answers with curfews and arrests.\n\nWehlen holds Porte Drazon " +
            "responsible for every shot.");
        Card(strong, () => Lost, "Zille's Partisan War",
            "Rizia's army has gone home, but the partisans of Zille have not. Well armed and well hidden in the hills, they keep the " +
            "Wehzek garrison on edge, and they have not stopped asking when the Royal Army is coming back.\n\nWehlen holds Porte " +
            "Drazon responsible for every ambush.");
    }

    // ------------------------------------------------------------------ other situations and policies

    private static void RegisterSituations()
    {
        const string lost = "Diplomacy_TerritorialIntegrity_Lost";
        SLK.Situation(lost, () => Ongoing, null,
            "The country faces a significant setback with the loss of its ancestral lands of Pales and the vital territory of Zille. " +
            "The Royal Army is now fighting to take Zille back, and the nation's pride rides on the outcome.\n\nUntil the war is " +
            "decided, the loss continues to weigh on strategic positioning and the allocation of resources.");
        SLK.Situation(lost, () => Part, null,
            "The country faces a significant setback with the loss of its ancestral lands of Pales and much of the territory of " +
            "Zille. Zille city flies the Rizian flag again, but the rest of the region lies beyond an armistice line, in Wehzek hands." +
            "\n\nA border drawn by the guns satisfies no one, and the unfinished war weighs on national pride and on the treasury.");
        SLK.Situation(lost, () => Lost, null,
            "The country faces a significant setback with the loss of its ancestral lands of Pales and the vital territory of Zille. " +
            "The attempt to retake Zille by force has failed, and Wehlen now holds the region more firmly than before.\n\nThe defeat " +
            "has deepened the wound to national pride and left the kingdom's strategic position weaker than it was before the war.");
        SLK.Situation("Diplomacy_TerritorialIntegrity_PartiallyRestored",
            () => Lib && !Variables.GetBool("RiziaDLC.War_BorderOutcome_Peace_Annexation") && !Variables.GetBool("RiziaDLC.War_BorderOutcome_Peace_PalesDivided"),
            null,
            "The nation has taken back Zille by force of arms, after decades under the Wehzek flag. Its return has lifted national " +
            "sentiment and put the gold of Auremina back within reach of the treasury.\n\nPales, however, remains beyond the border, " +
            "and a province won by conquest rather than handed back will need years of careful governance to stay Rizian.");

        SLK.Append("situation", "Diplomacy_GlobalImage_Good", () => War,
            "Remarkably, this standing has survived the controversy of the war in Zille, though Rizia's critics in Konstantium have " +
            "not stopped raising it.");
        SLK.Append("situation", "Diplomacy_GlobalImage_Bad", () => War,
            "The war in Zille, declared from the floor of the Alliance of Nations, is the first thing foreign editors mention.");
        SLK.Append("situation", "Diplomacy_PowerProjection_Weak", () => Lost,
            "The failure in Zille has made this weakness plain for every neighbor to see.");
        SLK.Append("situation", "Diplomacy_PowerProjection_Weak", () => Ongoing,
            "The war in Zille has drawn every eye in the region to the Royal Army, and not every observer is impressed.");
        SLK.Append("situation", "Diplomacy_PowerProjection_Strong", () => Lib,
            "The speed of the Zille campaign has not gone unnoticed in Wehlen, Lespia or Valgsland.");
        SLK.Append("situation", "Diplomacy_PowerProjection_Overwhelming", () => Lib,
            "The Zille campaign has shown the region what the Royal Army can do, and no neighbor has forgotten it.");
        SLK.Append("situation", "Diplomacy_ANSanctions_Weak", () => War,
            "The war in Zille weighs heavily on the Alliance's view of Rizia.");
        SLK.Append("situation", "Diplomacy_ANSanctions_Heavy", () => War,
            "The war in Zille weighs heavily on the Alliance's view of Rizia.");
        SLK.Append("situation", "Diplomacy_Rumburg_Suzerain", () => War && RumburgHelped,
            "The help Queen Beatrice lent Rizia in the Zille war was never a gift, and Datefort has begun to collect.");
        SLK.Append("situation", "Diplomacy_GlobalInfluence_ATO", () => War && WorldSystems.LespiaHelps,
            "Lespian warships in the Strait of Kreleck, sent to help Rizia's war in Zille, have given the Arcasian alliance a " +
            "foothold it did not have before.");
        SLK.Append("situation", "Diplomacy_GlobalInfluence_CSP",
            () => War && (Variables.GetString(SupplyWar.VarValgslandCable) is "guarantee" or "mitz" || Variables.GetString(SupplyWar.VarValgslandCall) == "yielded"),
            "Chancellor Hegel's threats over the Zille war cost the Crown concessions, and the Pact has not forgotten them.");
        SLK.Append("situation", "Military_NavalInterdictionCapacity_Lacking", () => Ongoing,
            "Wehzek raiders in the Strait of Kreleck have made the gap painfully clear.");
        SLK.Situation("Military_CombatExperience_Waning", () => Fought, null,
            "The Zille campaign gave a few regiments a taste of real fighting, but most of the army has still never heard a shot " +
            "fired in anger. The veterans of Zille are too few to carry the rest.\n\nNewer recruits remain dependent on simulated " +
            "drills, a poor substitute for the real crucible of war.");

        // Wehzek workers in a kingdom at war with Wehlen
        SLK.Append("situation", "Law_TreatmentOfForeigners_Equal", () => War,
            "Even through the war with Wehlen, the Crown has kept the rights of Wehzek workers intact, to the surprise of many on " +
            "both sides of the border.");
        SLK.Append("situation", "Law_TreatmentOfForeigners_Unequal", () => War,
            "Since the war with Wehlen began, Wehzek workers in particular have faced suspicion on the factory floor.");
        SLK.Append("situation", "Law_TreatmentOfForeigners_Unfair", () => War,
            "Since the war with Wehlen began, Wehzek workers have been watched by the police and shunned by their neighbors, and " +
            "many have quietly gone home.");
        SLK.Append("policy", "Law_LaborHands_Freed", () => War,
            "The permits have stayed valid through the war with Wehlen, though Wehzek miners in Brenas say the looks they get have changed.");
        SLK.Append("policy", "Law_LaborHands_Restricted", () => War,
            "Since the war with Wehlen began, Wehzek guest workers have faced suspicion as well as restrictions, and many have chosen " +
            "to go home.");
    }

    // ------------------------------------------------------------------ the Mave Nortus intelligence hub

    private static void RegisterIntelHub()
    {
        const string decree = "Order_SecurityIntel_BuildMaveNortusIntelligenceHub";
        SLK.Decree(decree, () => Ongoing, "Build Military Intelligence Hub in Mave Nortus",
            "With the Royal Army fighting in Zille, we hereby sanction the establishment of the Mave Nortus Foreign Intelligence " +
            "Hub. Its officers will run agents behind the Wehzek lines, question the refugees crossing the border, and pass what " +
            "they learn to the war room within hours.\n\nIt will also keep in touch with the pro-Rizian partisans of Zille, and help " +
            "them strike where the Wehzek garrison is weakest.");
        SLK.Decree(decree, () => Lib, null,
            "Zille is Rizian again, but Wehlen has not given up on it. We hereby sanction the establishment of the Mave Nortus " +
            "Foreign Intelligence Hub, to watch the new border and root out the Wehzek agents left behind in the liberated province." +
            "\n\nIt will also help win over the Zilleans who still doubt the Crown, before someone in Wraclawitz wins them back.");
        SLK.Decree(decree, () => Part, null,
            "The armistice line still cuts Zille in two. We hereby sanction the establishment of the Mave Nortus Foreign " +
            "Intelligence Hub, to watch the line, run agents into the Wehzek-held towns and warn the army of any buildup on the " +
            "other side.\n\nIt will also keep the hope of reunification alive among the Zilleans who were left beyond the line.");
        SLK.Decree(decree, () => Lost, null,
            "Zille remains under Wehzek occupation after the failed war. We hereby sanction the establishment of the Mave Nortus " +
            "Foreign Intelligence Hub, to watch the Wehzek garrison and shelter the partisans who escaped south.\n\nIt will keep in " +
            "touch with those in Zille who still hope for reunification, and prepare the ground for the day Rizia tries again.");

        const string policy = "Order_MaveNortusIntelligenceHub";
        SLK.Policy(policy, () => Ongoing, null,
            "The Mave Nortus Intelligence Hub runs agents behind the Wehzek lines in Zille, questions refugees crossing the border " +
            "and keeps the war room supplied with fresh intelligence. It also stays in touch with the pro-Rizian partisans of Zille.");
        SLK.Policy(policy, () => Lib, null,
            "The Mave Nortus Intelligence Hub watches the new border with Wehlen and hunts the Wehzek agents left behind in liberated " +
            "Zille, while shaping public sentiment in a province that is still getting used to Rizian rule.");
        SLK.Policy(policy, () => Part, null,
            "The Mave Nortus Intelligence Hub watches the armistice line in Zille, runs agents into the Wehzek-held towns and keeps " +
            "in touch with the Zilleans who were left on the other side.");
        SLK.Policy(policy, () => Lost, null,
            "The Mave Nortus Intelligence Hub watches the Wehzek garrison in occupied Zille, shelters the partisans who escaped " +
            "south after the war and keeps alive the networks Rizia will need if it ever tries again.");

        const string badge = "Rizia_City_IntelHub";
        SLK.Status(badge, () => Ongoing, null,
            "The hub runs agents behind the Wehzek lines in Zille and feeds the war room with intelligence on the garrison, its " +
            "supply routes and the partisans who fight it.");
        SLK.Status(badge, () => Lib, null,
            "The hub watches the new border with Wehlen and roots out Wehzek agents in liberated Zille, while shaping public " +
            "sentiment in the province.");
        SLK.Status(badge, () => Part, null,
            "The hub watches the armistice line in Zille and runs agents into the Wehzek-held towns on the other side.");
        SLK.Status(badge, () => Lost, null,
            "The hub watches the Wehzek garrison in occupied Zille and shelters the partisans who escaped south after the war.");
    }

    // ------------------------------------------------------------------ decrees that treat Wehlen as a partner

    private static void RegisterDecrees()
    {
        SLK.Decree("Religion_NonWruhecistReligiousTax", () => War, null,
            "Enacting the Non-Wruhecist Tribute Decree, Rizia imposes a 20% VAT explicitly on non-Wruhecist communities, channeling " +
            "additional fiscal resources into the kingdom's treasury each turn. Dastnurists, Zaranists, and other religious " +
            "adherents within the realm may interpret this measure as a selective, adverse fiscal imposition on their spiritual " +
            "observances, potentially inciting internal opposition. \n\nFurthermore, it will strain relations with Rumburg, whose " +
            "substantial Dastnurist population will take it as an affront, and hand Wehlen, our enemy over Zille, another grievance " +
            "to air in Konstantium.");
        SLK.Decree("Religion_RemoveGolcondistBlasphemyLaw", () => War, null,
            "Embarking on a path toward spiritual inclusivity, the kingdom is granting Golcondists unrestricted access to pivotal " +
            "religious sites such as the Archsanctuaries of Plavo, Benievon, and Jales. \n\nWhile this shift is likely to be " +
            "welcomed by Derdians and other Golcondists dwelling in Rizia, it may also raise concerns within the Dastnurist and " +
            "mainstream Wruhecist communities.\n\nWehlen will no doubt add it to the list of grievances it airs in Konstantium, " +
            "though relations with Wraclawitz can hardly get any worse after Zille.");
        SLK.Decree("Welfare_RightsFreedoms_LaborHandsLegalized", () => War, null,
            "The Kingdom proposes extending permanent work permits to Labor Hands from Wehlen and Derdia, acknowledging their " +
            "significant contributions particularly in mining, gas, and manufacturing industries since the 1930s. Despite the war " +
            "with Wehlen, the Wehzek miners of Brenas have kept the pits running, and this decree would tell them that the Crown " +
            "has noticed. \n\nWhile it fortifies the workforce in critical industrial sectors, many subjects will ask why the King " +
            "rewards the countrymen of the enemy, and the cost of integrating these communities remains.");
    }

    // ------------------------------------------------------------------ Turn 9 decisions

    private static void RegisterDecisions()
    {
        SLK.Append("decision", "Turn09_Decision_TourismCampaignResults", () => War,
            "The war in Zille is still the first thing foreign newspapers mention about Rizia, and travel agents report that many " +
            "visitors chose other destinations.");
        SLK.Append("decision", "Turn09_Decision_StockExchangeResults", () => Lib,
            "Brokers expect the gold of liberated Zille to lift mining shares, and RRG's prospectors are already talking about Auremina.");
        SLK.Append("decision", "Turn09_Decision_StockExchangeResults", () => Part,
            "Investors are wary of a kingdom whose border in Zille is an armistice line, and the brokers know it.");
        SLK.Append("decision", "Turn09_Decision_StockExchangeResults", () => Lost,
            "The defeat in Zille has made foreign investors nervous, and the brokers fear they will keep their money at home.");
        SLK.Append("decision", "Turn09_Decision_StockExchangeResults", () => Ongoing,
            "Foreign investors are reluctant to buy into a kingdom at war, and the brokers fear the war in Zille will scare them off.");
        SLK.Append("decision", "Turn09_Decision_LeaveAllianceOfNations", () => War,
            "The Alliance's condemnation of the war in Zille, declared from its own floor in Konstantium, has not been forgotten at court.");
    }
}
