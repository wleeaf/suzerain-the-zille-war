using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// News and reports for what happens on the supply calls, published on the step after the call, once each
/// (persistent flags, so a reload never repeats them). Vanilla tone: Geopolitico opens with the place or institution and
/// ends on a measured forecast, the Royal Herald salutes, the Voice of Rizia asks the awkward question; reports are
/// dispatches from our embassies with the quotes woven in.
/// </summary>
internal static class SupplyNews
{
    private static readonly List<string> Keys = new();

    private static string Flag(string key) => $"ZilleWar.SupNews_{key}";

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);

    private static void Paper(string key, string paper, string title, params string[] paragraphs) =>
        NewsWire.Publish("Sup" + key, paper, title, P(paragraphs));

    private static void Rep(string key, string token, string title, params string[] paragraphs) =>
        NewsWire.Report("Sup" + key, token, title, P(paragraphs));

    private static bool B(string v) => Variables.GetBool(v);
    private static string Str(string v) => SupplyWar.Str(v);

    public static void Register()
    {
        foreach (string k in new[]
                 {
                     "Credit1", "Credit2", "Dossier", "RumburgNo", "LespiaGas", "ValgslandLespia", "LespiaMitz", "PalesArms", "PalesArmsStopped",
                     "RamiroPublish", "RamiroRecalled", "RamiroComp", "LespiaGone", "StraitThin", "Gift", "Aureus", "Defector",
                     "Hegel1", "Hegel2", "SmolakRefused", "SmolakAccepted", "Saltana", "RumburgCease",
                     "Bluds", "PhotoRaid", "PhotoExpelled", "PhotoCaught", "SaltanaDowned", "LespiaFuel", "LespiaLax",
                 })
        {
            Keys.Add(k);
            Variables.Register(Flag(k));
        }

        Variables.Register(VarSaltanaTurn);
    }

    private const string VarSaltanaTurn = "ZilleWar.SupNews_SaltanaTurn";

    private static void Once(string key, bool when, Action publish)
    {
        if (!when || Variables.GetBool(Flag(key)))
        {
            return;
        }

        Variables.Set(Flag(key), true);
        try
        {
            publish();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Supply news '{key}' failed: {ex}");
        }
    }

    public static void OnStep()
    {
        Rumburg();
        Lespia();
        Navy();
        Others();
        Surprises();
    }

    // ------------------------------------------------------------------ the surprises of the war room, and Saltana's planes

    private static void Surprises()
    {
        string bluds = Str(SupplyWar.VarBludsIncident);
        Once("Bluds", bluds.Length > 0, () =>
        {
            Paper("Bluds", "G", "Villagers Killed Near Zille; Bludish Fighters in Rizian Service Accused",
                "Eleven Wehzek villagers, among them two women and a boy of fourteen, were killed in a night raid on a hamlet in the hills " +
                "above Zille, according to a correspondent for this newspaper who reached the village the following morning. Survivors " +
                "named the attackers as Bludish fighters serving with the Royal Army of Rizia.",
                bluds switch
                {
                    "trial" => "Porte Drazon has announced that the men responsible will be tried by court-martial in Zille, in public. Wehzek " +
                               "radio calls the trial \"theater.\" Foreign correspondents have nonetheless been promised seats.",
                    "home" => "The Bludish companies have since been withdrawn from the front. The Rizian General Staff says only that their " +
                              "contracts have ended, and declined to answer questions about the village.",
                    _ => "The Rizian General Staff says the incident is under investigation. The Bludish companies remain at the front.",
                });
            Paper("Bluds", "V", bluds == "trial" ? "A Trial, at Least" : "Whose War Is This?",
                bluds == "trial"
                    ? "The King hired Bludish gunmen and they behaved like Bludish gunmen. To his credit, he has not pretended otherwise. A " +
                      "court-martial will not bring eleven people back, but it may yet save the name of every Rizian soldier in Zille."
                    : "Eleven villagers are dead in the hills above Zille, killed by men the Crown paid to fight for it. The Voice of Rizia " +
                      "asks the palace a question it will not answer: who will stand trial for this, and in whose name were they fighting?");
            Paper("Bluds", "RH", bluds switch
                {
                    "trial" => "The Crown Orders a Public Court-Martial",
                    "home" => "Bludish Companies Released from Royal Service",
                    _ => "General Staff Examines Wehzek Claims",
                },
                bluds switch
                {
                    "trial" => "His Majesty has ordered that the foreign fighters accused of the killings in the hills above Zille be tried " +
                               "by court-martial, in public, before the foreign press. \"The Royal Army has nothing to hide,\" a palace " +
                               "spokesman said.",
                    "home" => "The Bludish companies hired for the campaign in Zille have been released from royal service. The Royal " +
                              "Army will hold its lines with Rizian soldiers, as it always preferred to.",
                    _ => "The General Staff is examining Wehzek reports of a night raid on a village above Zille. The Bludish companies " +
                         "who have fought alongside our soldiers in the hills remain at their posts.",
                },
                bluds == "trial"
                    ? "No Rizian soldier stands accused. The Royal Herald trusts that justice will be swift, and notes that Wraclawitz has " +
                      "never once tried its own soldiers in front of anybody."
                    : "Wehzek radio has made much of the affair. The Royal Herald reminds its readers that Wehzek radio also told them " +
                      "the Royal Army would never reach Zille.");
            if (bluds == "trial")
            {
                Rep("BludsRumburg", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Is Not Amused",
                    "The Rumburgian Foreign Office has asked, through our ambassador, why Rizia found it necessary to try Bludish fighters " +
                    "\"in front of the entire foreign press.\" The Bluds were Rumburg's recommendation, and Rumburg feels recommended against.",
                    "Our ambassador expects the Queen to raise it herself, at the time of her choosing.");
            }
        });

        string photo = Str(SupplyWar.VarPhotoPayoff);
        Once("PhotoRaid", photo is "raid" or "expelled" or "silent", () =>
            Paper("PhotoRaid", "RH", "Wehzek Raiders Strike Fort Ales",
                "Wehzek aircraft attacked the naval base at Fort Ales at dawn, setting fire to a fuel depot and part of the shell stores " +
                "before the base's guns drove them off. Sailors and dockworkers fought the fires side by side until noon.",
                "The Admiralty praises the garrison's courage. It has not explained how the raiders found their targets so easily on their " +
                "first pass."));
        Once("PhotoExpelled", photo == "expelled", () =>
            Paper("PhotoExpelled", "G", "Rizia Expels Lespian Naval Attaché",
                "The Kingdom of Rizia has declared the Lespian naval attaché in Porte Drazon persona non grata and given him forty-eight " +
                "hours to leave, days after a Wehzek air raid on the Rizian naval base at Fort Ales. Neither government will say whether " +
                "the two events are connected.",
                "In Perla, a spokesman for Prime Minister Alvarez regretted \"a misunderstanding between friends.\" Lespian patrols in " +
                "the Strait of Kreleck were seen thinning out the same week."));
        Once("PhotoCaught", photo is "returned" or "kept", () =>
            Rep("PhotoCaught", SuzerainTokenName.RiziaCitySallabes, "Forty Rolls of Film",
                "Our destroyers have stopped a Wehzek trawler off the Isor banks with forty rolls of film aboard, all of them photographs of " +
                "Rizian harbors taken from Lespian patrol boats. Whether Perla sold them or merely mislaid them, nobody will ever prove.",
                photo == "returned"
                    ? "The film has gone back to Perla in a diplomatic bag, with the King's compliments. No reply has been received."
                    : "The film is in the Admiralty's safe, and the trawler's skipper is in Sallabes, talking freely about the convoy routes out of Isor."));

        // Saltana's grey fighters: the turn after they arrive, one of them comes down on the wrong side of the line.
        if (B(SupplyWar.VarMorellaAircraft) && Variables.GetInt(VarSaltanaTurn) == 0)
        {
            Variables.Set(VarSaltanaTurn, WorldSystems.Turn);
        }

        int bought = Variables.GetInt(VarSaltanaTurn);
        Once("SaltanaDowned", bought > 0 && WorldSystems.Turn > bought && WarFlow.AtWar, () =>
        {
            Paper("SaltanaDowned", "G", "Grey Fighter Downed Over Zille",
                "Wehzek anti-aircraft gunners have shot down one of the unmarked grey fighters that have flown over the Zille front for " +
                "weeks. Wehzek newsreels show the wreck and its pilot, a young Rizian lieutenant, alive and in handcuffs. The engine plates, " +
                "Wehlen says, were stamped in Cavette.",
                "\"Morella sells engines to half of Merkopa,\" a spokesman for Prime Minister Saltana said. \"We have never heard of this " +
                "aircraft.\" President Smolak has asked the Alliance to name the country that built it. The Alliance has not.");
            Rep("SaltanaDowned", SuzerainTokenName.RiziaCountryMorella, "Cavette Keeps Its Word",
                "Prime Minister Saltana has done exactly what she promised: Morella has never heard of the aircraft. Through her ambassador, " +
                "she asks that the pilot's family be told nothing that would embarrass anyone, and that the next squadron, should there be " +
                "one, will cost more.",
                "The lieutenant is held in Wraclawitz. Wehzek radio interviews him every evening, and every evening he gives his name, his " +
                "rank and nothing else.");
        });
    }

    // ------------------------------------------------------------------ Rumburg

    private static void Rumburg()
    {
        int credits = Variables.GetInt(SupplyWar.VarRumburgCredits);
        Once("Credit1", credits >= 1, () =>
        {
            Paper("Credit1", "G", "Rumburg Opens a War Credit for Rizia",
                "The Exchequer of Rumburg has opened a line of credit worth eighteen billion guilders in the name of the Kingdom of Rizia, " +
                "Thornbourgh confirmed yesterday, to be repaid in quarterly installments. Queen Beatrice's government described it as \"a family arrangement " +
                "between members of GRACE.\"",
                "Economists in Konstantium note that the loan will keep Rizia's war in Zille funded through the season, and keep " +
                "Rizia's treasury tied to Thornbourgh for a good deal longer. It remains to be seen what else the arrangement includes.");
            Rep("Credit1", SuzerainTokenName.RiziaCountryRumburg, "The Exchequer's Terms",
                "Eighteen billion guilders of Rumburgian war credit have reached the Royal Treasury. Twenty-four billion must go back, " +
                "a little every quarter, starting at once.",
                "Councilor Werner has read the small print twice. She reports that the terms are generous, and that Rumburg will be in " +
                "a position to remind us of that generosity for years to come.");
        });
        Once("Credit2", credits >= 2, () =>
            Rep("Credit2", SuzerainTokenName.RiziaCountryRumburg, "A Second Loan from Thornbourgh",
                "Thornbourgh has extended a second war credit to the Crown, another eighteen billion guilders. The Rumburgian Exchequer, our embassy notes, drafted the " +
                "papers in a single afternoon and left the interest rate for last.",
                "Our repayments to Rumburg have doubled. A senior official at the Rumburgian Foreign Office remarked to our ambassador " +
                "that \"Rizia now owes Thornbourgh more than gratitude,\" and appeared to mean it as a compliment."));

        Once("Dossier", B(SupplyWar.VarDossier), () =>
        {
            Paper("Dossier", "G", "Wehlen Publishes \"Thornbourgh Dossier\" on Bludish Arms",
                "The Wehzek government has published a report of more than two hundred pages, which it calls the Thornbourgh Dossier, " +
                "accusing the Kingdom of Rumburg of supplying rifles, explosives and money to the Bludish Freedom Front. Photographs " +
                "show crates with Rumburgian army markings in a BFF camp in northern Wehlen.",
                "President Smolak said that Rizia's war in Zille is \"fought with Thornbourgh's guns and Bludish hands.\" Rumburg's " +
                "Foreign Office has dismissed the report as a forgery. Sordland has asked the Alliance of Nations to look into it, " +
                "and several GRACE governments have asked Thornbourgh, privately, for reassurance.");
            Paper("Dossier", "V", "The Queen's Rifles",
                (Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.BffRaids)) > 0
                    ? "If Wehlen's dossier is even half true, the Bluds blowing up Wehzek harbors in our name are armed by Rumburg and paid, "
                    : "If Wehlen's dossier is even half true, the Bludish fighters serving under Rizian officers in Montaklar are armed by Rumburg and paid, ") +
                "at least in part, by us. The palace has not said a word.",
                "The Voice of Rizia asks a simple question. Who decided this, and when were the people of Rizia going to be told?");
            Paper("Dossier", "RH", "Wraclawitz Turns to Forgery",
                (new[] { "Zille", "Auremina", "Isor", "Nanta" }.Any(c => Variables.GetBool(ZilleOperation.CapturedVar(c)))
                    ? "With its army giving ground in Zille, the Smolak government has turned to paper. "
                    : "Unable to win the argument any other way, the Smolak government has turned to paper. ") +
                "Its so-called Thornbourgh Dossier, two hundred " +
                "pages of photographs of crates, has been dismissed by Rumburg as a forgery, and the Royal Herald sees no reason to " +
                "doubt the word of Her Majesty Queen Beatrice.",
                "A crate proves only that Wehzek propagandists own crates. The soldiers of the Royal Army need no help from anyone to " +
                "beat Mr. Smolak's" + (new[] { "Zille", "Auremina", "Isor", "Nanta" }.Any(c => Variables.GetBool(ZilleOperation.CapturedVar(c)))
                    ? ", and they are proving it every day." : ", and they will prove it."));
            Rep("Dossier", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Embarrassed",
                "The dossier has landed badly in Thornbourgh. Opposition members are demanding to know how much of the Exchequer's money " +
                "has gone to the Bludish Freedom Front, and the Sordish ambassador has been seen leaving the Foreign Office twice in a day.",
                "Our embassy expects Queen Beatrice to keep her distance from our war for a while. She does not enjoy being embarrassed " +
                "in public, and she remembers who embarrassed her.");
        });

        Once("RumburgNo", B(SupplyWar.VarRumburgSaidNo), () =>
        {
            Paper("RumburgNo", "G", "Rumburg Signals Limits to Its Support for Rizia",
                "Rumburg will send no further military aid to Rizia \"for the time being,\" a spokesman for Queen Beatrice told " +
                "reporters in Thornbourgh, adding that the Kingdom \"remains a loyal friend of the Rizian Crown and a firm believer " +
                "in a negotiated peace.\"",
                SupplyWar.RumburgOilCut
                    ? "Diplomats read the statement as a message to Porte Drazon as much as to Wraclawitz. Rumburg's existing commitments, " +
                      "including its embargo on Wehzek oil, are understood to remain in force."
                    : SupplyWar.RumburgOilHalved
                    ? "Diplomats read the statement as a message to Porte Drazon as much as to Wraclawitz. Rumburgian tankers still load " +
                      "Wehzek oil, if only half as often as they did before the spring."
                    : "Diplomats read the statement as a message to Porte Drazon as much as to Wraclawitz. Rumburgian tankers, they note, " +
                      "have never stopped loading Wehzek oil.");
            Rep("RumburgNo", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Closes Its Purse",
                "Queen Beatrice has told the King herself that Rumburg's help with the war in Zille has reached its end. Her ministers " +
                "have added up the cost, and Sordland's restlessness on Rumburg's own border has done the rest.",
                B(SupplyWar.VarRumburgWantsCeasefire)
                    ? "Her Majesty let it be known that a swift peace would find Thornbourgh \"very generous.\" Our ambassador believes she means it."
                    : "Our ambassador advises patience. The Queen's moods pass. Her ledgers do not.");
        });

        // she asked for an end date, and the ceasefire gave her one (SupplyWar adds Thornbourgh's goodwill)
        Once("RumburgCease", B(SupplyWar.VarRumburgCeaseThanked), () =>
            Rep("RumburgCease", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Welcomes the Quiet",
                "Queen Beatrice has sent the King a short note on the ceasefire in Zille, in her own hand. It thanks him for \"giving a " +
                "tired old Queen the end date she asked for.\"",
                "Her ministers have since been noticeably warmer to our ambassador. Rumburg, it seems, keeps a ledger of favors as well as debts."));
    }

    // ------------------------------------------------------------------ Lespia

    private static void Lespia()
    {
        Once("LespiaGas", B(SupplyWar.VarLespiaGasDeal), () =>
        {
            Paper("LespiaGas", "G", "Rizia Signs Away Aureus Gas for Lespian Gunboats",
                "Lespia's state energy company LesPower will receive most of the Aureus field's gas at three Arcasian Liras per thousand " +
                "cubic meters, less than half what any other buyer pays, according to documents seen by this newspaper. The contract, open-ended and payable from the day the field's " +
                "pipeline opens, is said to be the price of the Lespian navy's patrols in the Strait of Kreleck.",
                "Prime Minister Alvarez called the arrangement \"a sensible exchange between neighbors.\" Analysts note that Lespia, which " +
                "lost the Aureus field to Rizia, has now obtained its gas anyway, and at a better price than it ever offered Pales.");
            Paper("LespiaGas", "V", "Our Gas, Their Price",
                "The Aureus field was supposed to keep the lights on in Rizia. Instead, most of its gas will sail to Lespia at a price no " +
                "Rizian family will ever be offered, year after year, for as long as anyone can see. The palace calls it the cost of the war. " +
                "The Voice of Rizia calls it what it is: a tribute.");
            Paper("LespiaGas", "RH", "A Shrewd Bargain at Sea",
                "Lespian warships now stand between Wehlen and its garrison in Zille, and every Wehzek convoy they turn back is a " +
                "battle our soldiers will not have to fight. The price is paid in Aureus gas, gas Rizia has to sell only because His " +
                "Majesty had the foresight to buy the field.",
                "The critics count cubic meters of gas. The Royal Herald counts the Rizian sons who will come home because of it.");
        });

        // Valgsland and Lespia are rivals (the Meftiem, the Antacean): any Lespian help in the strait draws a reaction.
        Once("ValgslandLespia", B(SupplyWar.VarLespiaStrait), () =>
        {
            Paper("ValgslandLespia", "G", "Valgsland Denounces \"Lespian Piracy\" in the Strait of Kreleck",
                "The government of Chancellor Emmerich Hegel has condemned the Lespian navy's stop-and-search patrols in the Strait of " +
                "Kreleck as \"piracy under a royal flag,\" and warned that Valgslandian merchant ships will not submit to Lespian " +
                "inspections.",
                "Diplomats in Konstantium say Valgsland is less troubled by Wehlen's convoys than by the sight of Lespian warships " +
                "working for Rizia. The two countries have spent years on opposite sides of the Meftiem dispute, and Hegel's ministers " +
                "now speak openly of a Lespian-Rizian bloc in the Antacean.");
            Rep("ValgslandLespia", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Reacts to the Lespian Patrols",
                "Chancellor Hegel's government has summoned our ambassador to protest what it calls Rizia's \"invitation to Lespian " +
                "piracy\" in the Strait of Kreleck. A Valgslandian destroyer flotilla has been ordered to escort the country's merchant " +
                "ships through the strait, and Valgslandian ports are said to be offering Wehzek convoys repairs at cost.",
                "Our embassy reads the protest as aimed at Lespia more than at us. Still, every Lespian favour now carries a Valgslandian " +
                "price, and Hegel has a long memory for both.");
        });

        // Lespia's grievance: fuel for the Wehzek convoys in Perla, or lax patrols in the strait it was paid to close.
        Once("LespiaFuel", B(SupplyWar.VarLespiaFuel), () =>
        {
            bool betrayed = B("RiziaDLC.MITZ_ScrewedOverLespia") || B(EarlyT5.VarLespiaBetrayed);
            Paper("LespiaFuel", "G", "Wehzek Tankers Refuel in Perla",
                "Wehzek tankers bound for the Strait of Kreleck have begun taking on fuel in Perla, port records show, and Lespian " +
                "harbor officials have stopped asking where they are headed. Prime Minister Alvarez's office calls it \"ordinary " +
                "commerce with a neighbor.\"",
                betrayed
                    ? "Diplomats in Konstantium link the change to the Meftiem, where Rizia left Lespia empty-handed. \"Perla does not " +
                      "get angry,\" one said. \"It sends an invoice.\""
                    : "Diplomats in Konstantium say Lespia offered Rizia its navy more than once this war and was turned down every " +
                      "time. \"Lespia sells to whoever buys,\" one said. \"Rizia declined to be a customer.\"");
            Paper("LespiaFuel", "V", "Our Enemies Fill Up in Perla",
                "The fuel that carries Wehzek reinforcements to Nanta now comes, in part, from Lespian depots. This paper has never " +
                "trusted Patricio Alvarez, but it is fair to ask what the palace did to turn a merchant into a supplier of our enemy.",
                "Somewhere in Zille, a Rizian soldier will face a Wehzek battalion that crossed the strait on Lespian fuel. He will not " +
                "be told why.");
            Rep("LespiaFuel", SuzerainTokenName.RiziaCountryLespia, "Lespian Ports Serve Wehzek Convoys",
                "Our naval attaché in Perla reports Wehzek tankers loading fuel at the commercial docks, unhindered and in daylight.",
                "Admiralty staff estimate the convoys to Nanta will run more often for it, and the garrison in Zille will feel the " +
                "difference before the month is out.");
        });
        Once("LespiaLax", WarFlow.AtWar && B(SupplyWar.VarLespiaGrievance) && B("ZilleWar.Call_Alvarez_GrievanceSaid") && B(SupplyWar.VarLespiaStrait), () =>
            Rep("LespiaLax", SuzerainTokenName.RiziaCountryLespia, "Lespian Patrols Grow Lax",
                "The Lespian warships in the Strait of Kreleck are still on station, but their inspections have become strangely " +
                "brief. Our attaché watched a Wehzek freighter pass a Lespian destroyer at noon without being boarded.",
                "Perla insists its navy honors every agreement with Rizia. It does, to the letter. The Admiralty notes that the " +
                "letter never said how closely to look."));

        Once("LespiaMitz", B(SupplyWar.VarLespiaMitz), () =>
        {
            bool destroyer = Str(SupplyWar.VarLespiaMitzReward) == "destroyer";
            Paper("LespiaMitz", "G", "Rizia to Side with Lespia on the Meftiem",
                "The Kingdom of Rizia will vote with Lespia against any change to the ownership of the Meftiem International Trade " +
                "Zone, diplomats in Konstantium say. In return, Lespia is said to have promised to vote down any motion to sanction " +
                "Rizia at the Alliance of Nations" +
                (destroyer
                    ? ", and a destroyer has left the yards of Perla under the Rizian flag."
                    : ", and to share what its navy knows about Wehzek shipping."),
                "In Cavette, Prime Minister Saltana's office called the reported agreement \"gold traded for gunboats.\" Chancellor " +
                "Hegel of Valgsland has not yet commented, which observers consider a comment in itself.");
            if (destroyer)
            {
                Rep("LespiaMitzShip", SuzerainTokenName.RiziaCitySallabes, "A Lespian Destroyer at Sallabes",
                    "The destroyer promised by Prime Minister Alvarez has reached Sallabes. Her Lespian crew handed her over at the " +
                    "quayside and took the evening train home.",
                    "The Admiralty reports that she is fast, well armed and a little short of spare parts, which can only be bought " +
                    "in Perla.");
            }
        });

        string pales = Str("ZilleWar.Alvarez_PalesTopic");
        Once("PalesArms", pales == "ignored", () =>
            Paper("PalesArms", "G", "Lespian Shipyards Deliver Destroyers to Pales",
                "Two destroyers built in Lespian yards were handed over to the Palesian navy at Remaz this week, while Lespia keeps " +
                "selling warships to the Grand Duchy.",
                "Asked whether Lespia was arming both sides of Rizia's wars, a spokesman for Prime Minister Alvarez said that \"Lespian " +
                "shipyards honor their contracts.\" In Porte Drazon, officials declined to comment."));
        Once("PalesArmsStopped", pales is "paid" or "leverage", () =>
            Rep("PalesArmsStopped", SuzerainTokenName.RiziaCountryLespia, "Remaz Order Books Closed",
                "Lespian shipyards have quietly stopped taking Palesian orders. The official explanation is a backlog of work. Our " +
                "embassy in Perla notes that the backlog appeared the same week the King spoke to Prime Minister Alvarez.",
                "The Duke's admirals will have to look elsewhere for their ships. Arcasia is the obvious place, and Arcasia is in no hurry."));

        bool lespiaPatrolled = Variables.GetInt(SupplyWar.Count(SupplyWar.Lever.Lespia)) > 0;
        Once("RamiroPublish", Str(SupplyWar.VarRamiro) == "publish", () =>
        {
            Paper("RamiroPublish", "RH", "The Truth About Smolak Square",
                "Documents recovered by the Royal Army in Zille prove that the Friendship Day bombing in Smolak Square was organized by " +
                "Héctor Ramiro, a Lespian intelligence officer posing as a diplomat at the Lespian consulate in Zille. The components " +
                "were supplied through CircuitRise Systems, a firm in Pales.",
                "For more than a year, Wehlen used that bomb to accuse innocent Rizians and to cling to Zille. The Royal Herald demands an apology " +
                "from Perla, and from Wraclawitz, which knew the truth and buried it.");
            Paper("RamiroPublish", "G", "Rizia Accuses Lespian Diplomat over Zille Bombing",
                "The Rizian government has published files, reportedly seized from the Wehzek police headquarters in Zille, naming " +
                "Héctor Ramiro of the Lespian consulate as the organizer of the bombing in Smolak Square. Mr. Ramiro holds diplomatic " +
                "immunity and could not be reached for comment.",
                (lespiaPatrolled
                    ? "Prime Minister Alvarez called the accusation \"an interesting conspiracy theory\" and announced that Lespian warships " +
                      "would no longer patrol the Strait of Kreleck. "
                    : "Prime Minister Alvarez called the accusation \"an interesting conspiracy theory\" and recalled his ambassador for " +
                      "consultations. ") +
                "In Wraclawitz, President Smolak said only that he had \"told the " +
                "Alliance it was not Rizian peasants who did this.\" It remains to be seen why he never said so before.");
            Paper("RamiroPublish", "V", "A War Over a Lie?",
                "So the bomb that gave Smolak his excuse was Lespian. The Wehzeks knew. Perla knew. Was the King the last to find out? " +
                "And if the bombing was never Rizian, what exactly are our sons dying for in Zille?");
            Rep("RamiroPublishLespia", SuzerainTokenName.RiziaCountryLespia,
                lespiaPatrolled ? "Alvarez Recalls His Ships" : "Perla Recalls Its Ambassador",
                lespiaPatrolled
                    ? "The Lespian navy has left the Strait of Kreleck. The order came within hours of the publication, and the first " +
                      "Wehzek convoy crossed unchallenged the same night."
                    : "Prime Minister Alvarez has recalled his ambassador from Porte Drazon for consultations. Whatever Lespia was " +
                      "prepared to offer for the strait is no longer on the table.",
                "Our embassy in Perla is being watched. The Lespian press calls the files a Wehzek forgery, but Lespian ministers have " +
                "stopped repeating that line, which our ambassador considers telling.");
            Rep("RamiroPublishWehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak: \"I Told the Alliance\"",
                "Wehzek radio has spent two days reminding its listeners that President Smolak never blamed the Rizian Crown directly. " +
                "This is not true, but it is broadcast often.",
                "More useful to us: several Wehzek officers captured in Zille have told our interrogators that they were sent to defend " +
                "the city against \"Rizian bombers.\" Some of them now ask what they were really sent to do.");
        });

        string outcome = Str("ZilleWar.Ramiro_ConfrontOutcome");
        Once("RamiroRecalled", outcome == "recalled", () =>
            Paper("RamiroRecalled", "G", "Lespian Diplomat Recalled from Zille",
                "Lespia has recalled Héctor Ramiro, a political officer at its consulate in Zille, \"for family reasons,\" the Lespian " +
                "Foreign Ministry said. No successor has been named.",
                "Mr. Ramiro's departure follows weeks of fighting in Zille. Diplomats in Konstantium note, without " +
                "elaborating, that Prime Minister Alvarez spoke to the King of Rizia by telephone the day before."));
        Once("RamiroComp", outcome == "compensated", () =>
            Rep("RamiroComp", SuzerainTokenName.RiziaCountryLespia, "A Quiet Payment from Perla",
                "Nine billion guilders have arrived at the Royal Treasury from an account in Perla, described in the transfer papers as a contribution " +
                "to \"the victims of terrorism in Zille.\"",
                "Nobody in Perla will say who authorized it. Nobody in Porte Drazon has asked."));
        Once("LespiaGone", B(SupplyWar.VarLespiaWithdrawn) && lespiaPatrolled && Str(SupplyWar.VarRamiro) != "publish", () =>
            Paper("LespiaGone", "G", "Lespia Ends Its Patrols in the Strait of Kreleck",
                "The Lespian navy has withdrawn its ships from the Strait of Kreleck, ending the patrols that had kept Wehzek shipping " +
                "out of Zille's harbors. Neither Perla nor Porte Drazon has explained the decision.",
                "Shipping agents in Wraclawitz report that Wehzek convoys began loading for Zille within hours of the news."));
        Once("StraitThin", Str(EarlyT6.VarAlvarezGasPrice) == "refused" && B(SupplyWar.VarLespiaStrait) &&
                           !B(SupplyWar.VarLespiaWithdrawn), () =>
            Rep("StraitThin", SuzerainTokenName.RiziaCountryLespia, "Thinner Patrols in the Strait",
                "The Lespian navy has pulled several patrol boats out of the Strait of Kreleck \"for refueling,\" according to Perla. " +
                "Our naval attaché counts fewer Lespian ships on station than at any time since the closure began.",
                "Wehzek convoys have noticed. Two of them crossed this week without being stopped."));
        Once("Gift", B("ZilleWar.Alvarez_GiftDone"), () =>
            Rep("Gift", SuzerainTokenName.RiziaCitySallabes, "Convoy Schedules from Perla",
                "An envelope from the Lespian " + (Str(SupplyWar.VarPhotoPayoff) == "expelled" ? "embassy" : "naval attaché") +
                " reached the Admiralty in Sallabes this week. Inside were the sailing times " +
                "of every Wehzek convoy leaving for Zille this month, accurate to the hour.",
                "Our captains used it well. Two convoys were caught in open water, and the rest turned back. The Admiralty would like " +
                "to know how Lespia came by the schedules. Nobody expects to find out."));
    }

    // ------------------------------------------------------------------ the navy

    private static void Navy()
    {
        Once("Aureus", B(SupplyWar.VarAureusRecalled), () =>
        {
            Paper("Aureus", "G", "Rizian Navy Leaves the Aureus Field",
                "Rizian warships have lifted their blockade of the Aureus gas field and sailed north, Palesian officials confirmed. " +
                "The ships are believed to have joined the Rizian fleet in the Strait of Kreleck.",
                "In Pales, the Grand Duchy called the withdrawal \"a victory for international law.\" In Perla, Prime Minister Alvarez " +
                "welcomed \"a sign of good sense.\" Neither mentioned that Rizia has simply found a war it wants more.");
            Rep("Aureus", SuzerainTokenName.RiziaCountryPales, "Reinhart Claims a Victory",
                "The Duke's ministers are celebrating the departure of our squadron as a triumph of Palesian diplomacy. Palesian " +
                "drillships are already moving back towards the field.",
                "Our embassy advises that the Grand Duchy has been reminded, very publicly, that Rizia's navy can be in only one place at a time.");
        });

        Once("Defector", B("ZilleWar.WarRoom_Defector"), () =>
            Paper("Defector", "RH", "Wehzek Captain Brings His Ship to Sallabes",
                "In a dramatic night crossing, the captain of a Wehzek supply ship bound for Zille sailed instead into the harbor of " +
                "Sallabes, his flag lowered and his lights burning, and asked for the protection of the Rizian Crown. His cargo of " +
                "artillery shells, meant for the guns of the Wehzek garrison, is now in the hands of the Royal Army.",
                "\"I have sailed for Wehlen for twenty years,\" the captain told our correspondent. \"I will not carry shells to be fired " +
                "at a city my grandmother was born in.\" His Majesty has granted him asylum."));
    }

    // ------------------------------------------------------------------ Valgsland, Wehlen, Morella

    private static void Others()
    {
        string cable = Str(SupplyWar.VarValgslandCable);
        Once("Hegel1", cable.Length > 0, () =>
        {
            string answer = cable switch
            {
                "guarantee" => "Porte Drazon is understood to have answered with guarantees for the miners and an invitation to " +
                               "Valgslandian observers. Port officials in Valgsland have since begun lengthy inspections of Contanan freighters.",
                "defied" => "According to diplomats, the Rizian reply was short and did not please its reader. Contanan freighters bound " +
                            "for Wehlen continue to call at Valgslandian ports on schedule.",
                "mitz" => "Diplomats say the Rizian reply mentioned the Meftiem International Trade Zone, where Valgsland has long sought " +
                          "Rizia's vote. Contanan freighters have since found Valgslandian harbors unusually crowded.",
                _ => "Porte Drazon has acknowledged the note and said nothing further. Valgslandian officials describe the silence as \"noted.\"",
            };
            Paper("Hegel1", "G", "Valgsland Warns Rizia over Wehzek Miners",
                "Chancellor Hegel of Valgsland has sent a formal note to the Kingdom of Rizia warning that any harm to the Wehzek miners " +
                "of the Zille region would be raised at the Alliance of Nations. The note also defends the right of Contanan ships to " +
                "call at Valgslandian ports on their way to Wehlen.",
                answer);
            if (cable == "defied")
            {
                Rep("Hegel1", "Rizia_Faction_Event_CSP", "Contana Thanks Valgsland",
                    "The Central Committee in Kyow has publicly thanked Valgsland for \"keeping the sea lanes open to a fraternal people " +
                    "under attack.\" Contanan freighters are reported to be carrying more than food.",
                    "Our embassy reads this as the Contanan Security Pact taking a closer interest in Zille than it has so far admitted.");
            }
        });

        string call = Str(SupplyWar.VarValgslandCall);
        Once("Hegel2", call.Length > 0, () =>
        {
            bool embargo = Variables.GetBool(AnDeclaration.VarDeclared) && !Variables.GetBool("RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP") &&
                           !Variables.GetBool("RiziaDLC.Trait_CommieKing");
            if (call == "defied" && embargo)
            {
                Paper("Hegel2", "G", "Valgsland Tightens Its Embargo on Rizia",
                    "Valgslandian ports will now refuse any ship that has called at a Rizian port, Chancellor Hegel announced, closing the " +
                    "Agnolian and Sordish back door through which Rizian goods had kept moving since the embargo began. Valgslandian " +
                    "destroyers continue their exercises in the Markian Sea.",
                    "Shipping agents in Porte Drazon expect freight rates to rise again within the month. In Konstantium, Valgsland's " +
                    "delegation has begun circulating a draft sanctions motion, though diplomats doubt it can find a majority while Rumburg " +
                    "and Lespia oppose it.");
            }
            else if (call == "defied")
            {
                Paper("Hegel2", "G", "Valgsland Closes Its Ports to Rizian Cargo",
                    "Valgsland has closed its ports to ships carrying Rizian cargo \"until the Kingdom of Rizia returns to the path of " +
                    "peace,\" Chancellor Hegel announced. Valgslandian destroyers continue their exercises in the Markian Sea.",
                    "Rizian exporters expect losses within the month. In Konstantium, Valgsland's delegation has begun circulating a " +
                    "draft sanctions motion, though diplomats doubt it can find a majority while Rumburg and Lespia oppose it.");
            }
            else
            {
                Rep("Hegel2", SuzerainTokenName.RiziaCountryValgsland, "Hegel Satisfied, for Now",
                    "Our submarines have been withdrawn south of the Strait of Kreleck, as the King promised Chancellor Hegel. The " +
                    "Valgslandian destroyers in the Markian Sea have moved their exercises further north.",
                    "Valgslandian officials describe relations with Rizia as \"correct.\" From Hegel's ministers, our ambassador notes, " +
                    "that is almost warm.");
            }
        });

        string smolak = Str(SupplyWar.VarSmolakAnswer);
        Once("SmolakRefused", smolak == "refused", () =>
            Paper("SmolakRefused", "G", "Smolak's Ceasefire Offer Rejected",
                "President Smolak of Wehlen offered the Kingdom of Rizia a ceasefire on the present front line, Wehzek radio announced, " +
                "and was refused. \"The King of Rizia prefers funerals,\" Mr. Smolak told a crowd in Wraclawitz.",
                ZilleCampaign.Strength >= 6
                    ? "Wehlen has since called up reservists from its eastern provinces. Military observers expect them in Zille within weeks."
                    : "Observers note that Wehlen's garrison in Zille is short of food and ammunition, and that the offer may not be repeated on the same terms."));

        Once("SmolakAccepted", smolak is "accepted" or "countered", () =>
        {
            var heldTowns = new[] { "Zille", "Auremina", "Isor", "Nanta" }
                .Where(c => Variables.GetBool(ZilleOperation.CapturedVar(c))).ToList();
            Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
            var occupied = ZilleData.Towns.Where(t => t.Side != "zille" && t.Role == "city" && ZilleCampaign.IsEnemy(owners, t.Tile))
                .Select(t => t.Name).ToList();
            // only the demands Smolak actually made: each is voiced on the telephone only while it is still open (SupplyVoices)
            string kind = Str(SupplyWar.VarCeasefireTerms);
            bool snapped = B(SupplyWar.VarPreSnapped);
            var asked = new List<string>();
            if (kind == "full" && snapped && B(SupplyWar.VarPreRico))
            {
                asked.Add("the extradition of Ricardus Toras to Wehlen to answer for the Smolak Square bombing");
            }

            if (kind is "full" or "reduced" && !(snapped && B(SupplyWar.VarPreBan)))
            {
                asked.Add("the closure of Rizia's border to Bludish migrants");
            }

            if (kind == "rights" || !(snapped && B(SupplyWar.VarPreRights)))
            {
                asked.Add("full civil rights for Wehzeks living in Rizia");
            }

            bool nothing = asked.Count == 0;
            string terms = nothing ? "nothing Rizia had not already conceded"
                : asked.Count == 1 ? asked[0]
                : string.Join(", ", asked.Take(asked.Count - 1)) + (asked.Count > 2 ? ", and " : " and ") + asked[^1];
            Paper("SmolakAccepted", "G", "The Price of Quiet in Zille",
                $"The ceasefire between Rizia and Wehlen was agreed by telephone, both governments confirmed. Each army keeps the ground " +
                (smolak == "countered"
                    ? $"it holds. Rizia offered {terms} in return, and Wraclawitz agreed, though nothing has been signed."
                    : nothing
                    ? "it holds. Wraclawitz asked for nothing Rizia had not already conceded, which diplomats read as a sign of how badly it wanted the quiet."
                    : $"it holds. In return, Wraclawitz has demanded {terms}, and the Royal Army has not confirmed what it agreed to."),
                "President Smolak called it \"a peace that Wehlen can live with.\" It remains to be seen whether Rizia can.");
            Paper("SmolakAccepted", "V", "What Did the King Give Away?",
                nothing
                    ? "The guns are silent, and the Voice of Rizia is glad of it. A ceasefire made over the telephone deserves to be read aloud, " +
                      "and for once there is little to read: Smolak asked for nothing the Crown had not already given him."
                    : $"The guns are silent, and the Voice of Rizia is glad of it. But a ceasefire made over the telephone deserves to be read " +
                      $"aloud, so here it is: {terms}. The people of Rizia were not asked about any of it.");
            Paper("SmolakAccepted", "RH", "The Guns Fall Silent in Zille",
                heldTowns.Count == 0
                    ? "At His Majesty's word, the Royal Army has ceased fire in Zille. The offensive took no city, and the Herald asks " +
                      "only that the terms be read in full."
                    : "At His Majesty's word, the Royal Army has ceased fire in Zille. It holds every inch of the ground it won, and Rizian " +
                      "flags fly over " + (heldTowns.Count == 1 ? heldTowns[0] : string.Join(", ", heldTowns.Take(heldTowns.Count - 1)) +
                      " and " + heldTowns[^1]) + ", which had not seen one in a generation." +
                      (occupied.Count > 0 ? $" Wehzek troops remain in {string.Join(" and ", occupied)}, and the Herald trusts they will not stay long." : string.Empty),
                nothing
                    ? "Wraclawitz asked no new price for the quiet. The Royal Herald notes that a beaten garrison rarely haggles."
                    : (smolak == "countered" ? $"His Majesty named the price for the quiet: {terms}. Wraclawitz agreed. " : $"Wraclawitz asked a price for the quiet: {terms}. ") +
                      "The Royal Herald will not pretend the price was nothing. It was less than the lives it spares.");
        });

        Once("Saltana", B(SupplyWar.VarMorellaAircraft), () =>
        {
            Paper("Saltana", "G", "Unmarked Aircraft Over the Gulf of Moridia",
                "Fishermen on the Gulf of Moridia report grey fighter aircraft without national markings flying north towards Zille " +
                "at dawn. Wehzek officials accuse Morella of supplying them. Cavette denies all knowledge.",
                "\"Morella has no aircraft over the gulf that it is aware of,\" a spokesman for Prime Minister Saltana said. Several " +
                "military attachés in Konstantium found the phrasing very precise.");
            Rep("Saltana", SuzerainTokenName.RiziaCountryMorella, "Planes Without Flags",
                "The Morellan aircraft arrived at night, as promised, painted grey and without a single marking. Our pilots report that " +
                "they fly well and that their manuals have had every Morellan word cut out with scissors.",
                "Prime Minister Saltana keeps her word, and her distance. Our embassy expects her to remember the favour when the " +
                "Meftiem votes.");
        });
    }
}
