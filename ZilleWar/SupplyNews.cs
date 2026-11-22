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
    private static string Str(string v) => Variables.GetString(v) ?? string.Empty;

    public static void Register()
    {
        foreach (string k in new[]
                 {
                     "Credit1", "Credit2", "Dossier", "RumburgNo", "LespiaGas", "LespiaMitz", "PalesArms", "PalesArmsStopped",
                     "RamiroPublish", "RamiroRecalled", "RamiroComp", "LespiaGone", "StraitThin", "Gift", "Aureus", "Defector",
                     "Hegel1", "Hegel2", "SmolakRefused", "SmolakAccepted", "Saltana",
                 })
        {
            Keys.Add(k);
            Variables.Register(Flag(k));
        }
    }

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
    }

    // ------------------------------------------------------------------ Rumburg

    private static void Rumburg()
    {
        int credits = Variables.GetInt(SupplyWar.VarRumburgCredits);
        Once("Credit1", credits >= 1, () =>
        {
            Paper("Credit1", "G", "Rumburg Opens a War Credit for Rizia",
                "The Exchequer of Rumburg has opened a line of credit in the name of the Kingdom of Rizia, Thornbourgh confirmed " +
                "yesterday, to be repaid in monthly installments. Queen Beatrice's government described it as \"a family arrangement " +
                "between members of GRACE.\"",
                "Economists in Konstantium note that the loan will keep Rizia's war in Zille funded through the season, and keep " +
                "Rizia's treasury tied to Thornbourgh for a good deal longer. It remains to be seen what else the arrangement includes.");
            Rep("Credit1", SuzerainTokenName.RiziaCountryRumburg, "The Exchequer's Terms",
                "The first tranche of the Rumburgian war credit has reached the Royal Treasury. Repayments begin at once, a little " +
                "every month, until the debt is cleared.",
                "Minister Werner has read the small print twice. She reports that the terms are generous, and that Rumburg will be in " +
                "a position to remind us of that generosity for years to come.");
        });
        Once("Credit2", credits >= 2, () =>
            Rep("Credit2", SuzerainTokenName.RiziaCountryRumburg, "A Second Loan from Thornbourgh",
                "Thornbourgh has extended a second war credit to the Crown. The Rumburgian Exchequer, our embassy notes, drafted the " +
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
                "If Wehlen's dossier is even half true, the Bluds blowing up Wehzek harbors in our name are armed by Rumburg and paid, " +
                "at least in part, by us. The palace has not said a word.",
                "The Voice of Rizia asks a simple question. Who decided this, and when were the people of Rizia going to be told?");
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
                "Diplomats read the statement as a message to Porte Drazon as much as to Wraclawitz. Rumburg's existing commitments, " +
                "including its embargo on Wehzek oil, are understood to remain in force.");
            Rep("RumburgNo", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Closes Its Purse",
                "Queen Beatrice has told the King herself that Rumburg's help with the war in Zille has reached its end. Her ministers " +
                "have added up the cost, and Sordland's restlessness on Rumburg's own border has done the rest.",
                B(SupplyWar.VarRumburgWantsCeasefire)
                    ? "Her Majesty let it be known that a swift peace would find Thornbourgh \"very generous.\" Our ambassador believes she means it."
                    : "Our ambassador advises patience. The Queen's moods pass. Her ledgers do not.");
        });
    }

    // ------------------------------------------------------------------ Lespia

    private static void Lespia()
    {
        Once("LespiaGas", B(SupplyWar.VarLespiaGasDeal), () =>
        {
            Paper("LespiaGas", "G", "LesPower to Receive Aureus Gas at a \"Friendship Price\"",
                "Lespia's state energy company LesPower will receive a large delivery of gas from the Aureus field at a fraction of " +
                "the market price, according to documents seen by this newspaper. The sale is said to be part of an arrangement under " +
                "which the Lespian navy patrols the Strait of Kreleck.",
                "Prime Minister Alvarez called the deal \"a sensible exchange between neighbors.\" Analysts note that Lespia, which lost " +
                "the Aureus field to Rizia, has now obtained its gas anyway, and at a better price than it ever offered Pales.");
            Paper("LespiaGas", "V", "Our Gas, Their Price",
                "The Aureus field was supposed to keep the lights on in Rizia. Instead, its gas is sailing to Perla at a price no Rizian " +
                "family will ever be offered. The palace calls it the cost of the war. The Voice of Rizia calls it what it is.");
        });

        Once("LespiaMitz", B(SupplyWar.VarLespiaMitz), () =>
            Paper("LespiaMitz", "G", "Rizia to Side with Lespia on the Meftiem",
                "The Kingdom of Rizia will vote with Lespia against any change to the ownership of the Meftiem International Trade " +
                "Zone, diplomats in Konstantium say, in what is widely seen as the price of Lespian help in the Strait of Kreleck.",
                "In Cavette, Prime Minister Saltana's office called the reported agreement \"gold traded for gunboats.\" Chancellor " +
                "Hegel of Valgsland has not yet commented, which observers consider a comment in itself."));

        string pales = Str("ZilleWar.Alvarez_PalesTopic");
        Once("PalesArms", pales == "ignored", () =>
            Paper("PalesArms", "G", "Lespian Shipyards Deliver Destroyers to Pales Days After Closing Kreleck",
                "Two destroyers built in Lespian yards were handed over to the Palesian navy at Remaz this week, only days after the " +
                "Lespian navy closed the Strait of Kreleck to Wehzek shipping on Rizia's behalf.",
                "Asked whether Lespia was arming both sides of Rizia's wars, a spokesman for Prime Minister Alvarez said that \"Lespian " +
                "shipyards honor their contracts.\" In Porte Drazon, officials declined to comment."));
        Once("PalesArmsStopped", pales is "paid" or "leverage", () =>
            Rep("PalesArmsStopped", SuzerainTokenName.RiziaCountryLespia, "Remaz Order Books Closed",
                "Lespian shipyards have quietly stopped taking Palesian orders. The official explanation is a backlog of work. Our " +
                "embassy in Perla notes that the backlog appeared the same week the King spoke to Prime Minister Alvarez.",
                "The Duke's admirals will have to look elsewhere for their ships. Arcasia is the obvious place, and Arcasia is in no hurry."));

        Once("RamiroPublish", Str(SupplyWar.VarRamiro) == "publish", () =>
        {
            Paper("RamiroPublish", "RH", "The Truth About Smolak Square",
                "Documents recovered by the Royal Army in Zille prove that the Friendship Day bombing in Smolak Square was organized by " +
                "Héctor Ramiro, a Lespian intelligence officer posing as a diplomat at the Lespian consulate in Zille. The components " +
                "were supplied through CircuitRise Systems, a firm in Pales.",
                "For years, Wehlen used that bomb to accuse innocent Rizians and to cling to Zille. The Royal Herald demands an apology " +
                "from Perla, and from Wraclawitz, which knew the truth and buried it.");
            Paper("RamiroPublish", "G", "Rizia Accuses Lespian Diplomat over Zille Bombing",
                "The Rizian government has published files, reportedly seized from the Wehzek police headquarters in Zille, naming " +
                "Héctor Ramiro of the Lespian consulate as the organizer of the bombing in Smolak Square. Mr. Ramiro holds diplomatic " +
                "immunity and could not be reached for comment.",
                "Prime Minister Alvarez called the accusation \"an interesting conspiracy theory\" and announced that Lespian warships " +
                "would no longer patrol the Strait of Kreleck. In Wraclawitz, President Smolak said only that he had \"told the " +
                "Alliance it was not Rizian peasants who did this.\" It remains to be seen why he never said so before.");
            Paper("RamiroPublish", "V", "A War Over a Lie?",
                "So the bomb that gave Smolak his excuse was Lespian. The Wehzeks knew. Perla knew. Was the King the last to find out? " +
                "And if the bombing was never Rizian, what exactly are our sons dying for in Zille?");
            Rep("RamiroPublishLespia", SuzerainTokenName.RiziaCountryLespia, "Alvarez Recalls His Ships",
                "The Lespian navy has left the Strait of Kreleck. The order came within hours of the publication, and the first Wehzek " +
                "convoy crossed unchallenged the same night.",
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
                "Mr. Ramiro's departure follows weeks of fighting around the consulate. Diplomats in Konstantium note, without " +
                "elaborating, that Prime Minister Alvarez spoke to the King of Rizia by telephone the day before."));
        Once("RamiroComp", outcome == "compensated", () =>
            Rep("RamiroComp", SuzerainTokenName.RiziaCountryLespia, "A Quiet Payment from Perla",
                "A sum has arrived at the Royal Treasury from an account in Perla, described in the transfer papers as a contribution " +
                "to \"the victims of terrorism in Zille.\"",
                "Nobody in Perla will say who authorized it. Nobody in Porte Drazon has asked."));
        Once("LespiaGone", B(SupplyWar.VarLespiaWithdrawn) && Str(SupplyWar.VarRamiro) != "publish", () =>
            Paper("LespiaGone", "G", "Lespia Ends Its Patrols in the Strait of Kreleck",
                "The Lespian navy has withdrawn its ships from the Strait of Kreleck, ending the patrols that had kept Wehzek shipping " +
                "out of Zille's harbors. Neither Perla nor Porte Drazon has explained the decision.",
                "Shipping agents in Wraclawitz report that Wehzek convoys began loading for Zille within hours of the news."));
        Once("StraitThin", Str("ZilleWar.Alvarez_GasTopic") == "refused", () =>
            Rep("StraitThin", SuzerainTokenName.RiziaCountryLespia, "Thinner Patrols in the Strait",
                "The Lespian navy has pulled several patrol boats out of the Strait of Kreleck \"for refueling,\" according to Perla. " +
                "Our naval attaché counts fewer Lespian ships on station than at any time since the closure began.",
                "Wehzek convoys have noticed. Two of them crossed this week without being stopped."));
        Once("Gift", B("ZilleWar.Alvarez_GiftDone"), () =>
            Rep("Gift", SuzerainTokenName.RiziaCitySallabes, "Convoy Schedules from Perla",
                "An envelope from the Lespian naval attaché reached the Admiralty in Sallabes this week. Inside were the sailing times " +
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
                "In Fort Ales, the Grand Duchy called the withdrawal \"a victory for international law.\" In Perla, Prime Minister Alvarez " +
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
                "at a city my grandmother was born in.\" His Majesty has granted him asylum. Axa Victoria!"));
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
            if (call == "defied")
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
            string terms = Str(SupplyWar.VarCeasefireTerms) switch
            {
                "full" => "the extradition of Ricardus Toras to Wehlen to answer for the Smolak Square bombing, the closure of Rizia's " +
                          "border to Bludish migrants, and full civil rights for Wehzeks living in Rizia",
                "reduced" => "the closure of Rizia's border to Bludish migrants and full civil rights for Wehzeks living in Rizia",
                _ => "full civil rights for Wehzeks living in Rizia",
            };
            Paper("SmolakAccepted", "G", "The Price of Quiet in Zille",
                $"The ceasefire between Rizia and Wehlen was agreed by telephone, both governments confirmed. Each army keeps the ground " +
                $"it holds. In return, according to the text circulated in Konstantium, Rizia has accepted {terms}.",
                "President Smolak called it \"a peace that Wehlen can live with.\" It remains to be seen whether Rizia can.");
            Paper("SmolakAccepted", "V", "What Did the King Give Away?",
                $"The guns are silent, and the Voice of Rizia is glad of it. But a peace made over the telephone deserves to be read " +
                $"aloud, so here it is: {terms}. The people of Rizia were not asked about any of it.");
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
