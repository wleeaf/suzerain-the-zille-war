using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.NewsWire;

namespace ZilleWar;

/// <summary>
/// The world's voice between the operations: the home front every war turn, the papers' editorials, the reactions abroad
/// as the war drags on (Valgsland's threats, Contana's convoys, Arcasia, Agnolia, the markets), good news as well as bad
/// (volunteers, diaspora money, a Wehzek defector, a liberated town's first market day), the two wars at once, and the
/// first turns after the war, whatever its outcome. Every item is published once (keys kept in the save).
/// </summary>
internal static class WorldVoices
{
    private const string VarZilleHeldTurn = "ZilleWar.World_ZilleHeldTurn";
    internal const string VarDefector = "ZilleWar.World_Defector";

    public static void Register()
    {
        Variables.Register(VarZilleHeldTurn);
        Variables.Register(VarDefector);
    }

    public static void OnStep()
    {
        // Reports cannot be filed on the first step of the game; news and reports also read better once a turn is under way.
        if (!WorldSystems.Declared || WorldSystems.Step < 2)
        {
            return;
        }

        int w = WorldSystems.WarTurn;
        if (WorldSystems.Active)
        {
            if (w == 1)
            {
                OnDeclaration();
            }

            HomeFront(w);
            if (WorldSystems.Step >= 3)
            {
                Editorials(w);
                GoodNews(w);
            }

            // later in the turn, so the calls of the supply war have been taken first
            if (WorldSystems.Step >= 5)
            {
                Abroad(w);
            }

            if (WorldSystems.TwoWars)
            {
                TwoWars();
            }
        }
        else if (WorldSystems.WarEnded && WorldSystems.EndTurn > 0)
        {
            AfterWar(WorldSystems.TurnsSinceEnd);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static int Turn => WorldSystems.Turn;

    private static void News(string key, string paper, string title, string text)
    {
        if (WorldSystems.Once($"N_{key}_{paper}"))
        {
            Publish($"W_{key}", paper, title, text);
        }
    }

    private static void Dispatch(string key, string token, string title, string text)
    {
        if (WorldSystems.Once($"R_{key}"))
        {
            Report($"W_{key}", token, title, text);
        }
    }

    private static Front.Phase FirstPhase() => Front.Of(1);

    private static bool Taken(string town) => Variables.GetBool(ZilleOperation.CapturedVar(town));
    private static bool Flag(string f) => Variables.GetBool(CityEvents.Flag(f));
    private static int Lever(SupplyWar.Lever l) => Variables.GetInt(SupplyWar.Count(l));
    private static string Tier => ZilleCampaign.Tier;

    // ------------------------------------------------------------------ the declaration's first consequences

    private static void OnDeclaration()
    {
        bool cooperated = Variables.GetBool("RiziaDLC.Decision_Turn04_WehlenInvestigation_Cooperate");
        Dispatch("Legation", SuzerainTokenName.RiziaCountryWehlen, "Our Legation Leaves Wraclawitz", P(
            "The staff of our legation in Wraclawitz crossed into Sordland this morning under police escort, after the Wehzek " +
            "foreign ministry gave them twelve hours to leave. The Wehzek embassy in Porte Drazon has been closed in turn, and its " +
            "flag taken down in front of a silent crowd.",
            cooperated
                ? "Our security liaison officers, sent to Wraclawitz only days ago to help with the bombing investigation, were held at " +
                  "the border for questioning before being released. Wehzek radio has called them \"the King's spies in borrowed suits.\""
                : "The legation's archives were burned before the staff left. Nothing of value, our ambassador assures us, was left behind."));

        Dispatch("TreasuryCost", SuzerainTokenName.RiziaCityPorteDrazon, "The Treasury Counts the Cost of War", P(
            "Councilor Elena Werner's office has sent the palace its first estimate of what the war will cost. Pay, fuel, shells and " +
            "the requisitioned trains of the northern line will cost the treasury six billion guilders a quarter, and nine billion if " +
            "the fighting goes badly. Trade across the Strait of Kreleck is slowing, and insurers have raised the price of every cargo " +
            "that still sails.",
            "The memorandum ends with a sentence underlined twice: \"The treasury can pay for a short war. It cannot pay for a long one.\""));
    }

    // ------------------------------------------------------------------ the home front, every war turn

    private static void HomeFront(int w)
    {
        if (w < 2)
        {
            return;
        }

        string key = $"HomeFront_T{Turn}";
        switch (w)
        {
            case 2:
                bool rationed = Variables.GetBool("ZilleWar.Decree_FuelRationing");
                Dispatch(key, SuzerainTokenName.RiziaCityPorteDrazon, rationed ? "Ration Cards and Recruiting Posters" : "Petrol Queues and Recruiting Posters", P(
                    (rationed
                        ? "Fuel ration cards were handed out in Porte Drazon this week, and taxi drivers queued for them outside the town hall " +
                          "before dawn."
                        : "Petrol has doubled in price since the army began buying every drop it can find, and the queues at the pumps of " +
                          "Porte Drazon now start before dawn.") +
                    (Variables.GetBool(ZilleOperation.CapturedVar("Zille"))
                        ? " Recruiting posters showing a soldier raising the golden banner over Zille harbor are pasted on every wall."
                        : " Recruiting posters showing a soldier pointing north, toward Zille, are pasted on every wall."),
                    "Shopkeepers report that coffee, sugar and imported cloth have doubled in price since trade across the strait " +
                    "slowed. For now, most people grumble and pay. \"It is for Zille,\" one woman told a reporter, counting her coins."));
                Dispatch($"Refugees_T{Turn}", SuzerainTokenName.RiziaCityMaveNortus, "Refugees on the Road to " + SafeTown(), P(
                    "Families from the Zille border villages, Rizian and Wehzek alike, have been arriving in " + SafeTown() + " on foot and by cart. " +
                    "The army has opened relief stations in the grain warehouses by the station, and the sanctuaries of Brenas are collecting " +
                    "blankets.",
                    "Relief officials say the money set aside for the refugees will be spent within weeks, and they have asked the Crown " +
                    "for more."));
                break;
            case 3:
                Dispatch(key, SuzerainTokenName.RiziaCityBronaz, "The Long Shifts of Bronaz", P(
                    "The factories of Bronaz have now worked twelve-hour shifts for months. Accidents on the night shift have doubled, and " +
                    "union leaders who agreed to suspend strikes \"for a short war\" are asking, in public, how long a short war lasts.",
                    "The first women's brigades have started work on the shell lines. Their foremen, a little to their own surprise, report " +
                    "that output has risen."));
                Dispatch($"Mothers_T{Turn}", SuzerainTokenName.RiziaCityPorteDrazon, "Mothers at the Ministry of War", P(
                    "A few dozen women in black have stood outside the Ministry of War every morning this week, holding photographs of sons " +
                    "missing in Zille. They do not chant and they do not carry banners. Clerks have taken to using the side door.",
                    "Police have orders to leave them alone. Our informants say that the Voice of Rizia intends to put one of the photographs " +
                    "on its front page."));
                break;
            default:
                Dispatch(key, SuzerainTokenName.RiziaCityIza, "A Nation Tired of War", P(
                    "In Iza, where the declaration was greeted with sanctuary bells, the recruiting office now closes at noon for lack of " +
                    "volunteers. Black armbands are a common sight in the market, and the Su Omina banners have quietly come down from " +
                    "the balconies.",
                    "Police informants report that the war is now discussed openly in the cafes, and not kindly. The word most often " +
                    "overheard is \"enough.\""));
                break;
        }

        if (WorldSystems.ZilleCityHeld)
        {
            Dispatch($"Occupation_T{Turn}", WorldSystems.ZilleToken, "Under Military Administration", P(
                "The army's administrators in Zille report that bread, water and electricity are flowing again in most districts of the " +
                "city. Wehzek officials who stayed behind are being questioned and, for the most part, released.",
                Flag("Zille_Restraint")
                    ? "The order to protect Wehzek residents has been kept. Foreign correspondents have been allowed to walk the old town " +
                      "freely, and their dispatches have been kinder than the palace expected."
                    : "There have been beatings of Wehzek residents in the harbor district, and at least one house was burned. Military " +
                      "police have made arrests, but Wehzek radio already speaks of \"pogroms under the golden banner.\""));
        }
    }

    // ------------------------------------------------------------------ the papers, every war turn

    private static void Editorials(int w)
    {
        if (w < 2)
        {
            return;
        }

        string key = $"Editorial_T{Turn}";
        bool strained = Tier is "Strained" or "Starved";
        if (w == 2)
        {
            News(key, "RH", "Rizia Answers the Call", P(
                "From the mountain villages of Montaklar to the fishing ports of Valenqiris, the sons of Rizia have answered their " +
                "King's call. Recruiting offices report more volunteers than they can equip, and in Iza the queue stretched around the " +
                "Grandsanctuary twice.",
                strained
                    ? "At the front, the news is good. Wehzek prisoners speak of half rations and empty magazines, and our officers say " +
                      "the garrison will not hold out much longer."
                    : "Our soldiers face a stubborn enemy, supplied across the strait by Wraclawitz and its friends in Kyow. They face it " +
                      "with courage, and with the whole nation behind them.",
                "Axa Victoria!"));
            News(key, "V", "The Bill Arrives", P(
                "The Voice of Rizia promised to count the cost of the King's war. Here is the first instalment. " +
                (Variables.GetBool("ZilleWar.Decree_FuelRationing") ? "Fuel is rationed. " : "Fuel has doubled in price and the pumps run dry by noon. ") +
                (Variables.GetBool("ZilleWar.Decree_PriceControls")
                    ? "Bread is held at its old price, and the Treasury pays the bakers the difference. "
                    : "Bread costs more than it did a month ago. ") + "The northern railway carries soldiers instead of freight, and the farmers of Brenas " +
                "watch their harvest rot at the sidings.",
                Variables.GetBool("ZilleWar.Decree_WarCensorship")
                    ? "Meanwhile the censors decide which figures from the front we may print, and they have not yet decided on any. We would " +
                      "like to see the cost of the war and the cost of the shells side by side. So, we suspect, would the censors."
                    : FirstPhase() is { Won: true }
                        ? "Meanwhile the palace prints its victory in Zille on the front page and its casualty lists on its railings. We would " +
                          "like to see both on the same page, next to the price of bread."
                        : FirstPhase() is { CitiesGained.Count: > 0 } first
                            ? $"Meanwhile the palace prints the capture of {Front.Join(first.CitiesGained)} on the front page and says little of Zille " +
                              "city, which the first phase was sent to take. We would like to see the casualty lists on the same page."
                            : "Meanwhile the palace explains that the first phase was never meant to take Zille in a single stroke. We would like " +
                              "to see the plan it was meant to follow, next to the bill for it."));
        }
        else if (w == 3)
        {
            News(key, "RH", "Steadfast", P(
                "Wars are not won in a season, and no Rizian ever believed this one would be. The Royal Herald salutes the families who " +
                "have given their sons, the workers of Bronaz who build their shells, and the women of Porte Drazon who knit their socks.",
                "Those who now whisper that the war has gone on long enough should ask themselves who benefits from their whispering. " +
                "The answer is in Wraclawitz."));
            News(key, "V", "How Long?", P(
                "The war that was to be over by the harvest is still with us. The casualty lists on the palace railings now run to " +
                "several sheets, and nobody in the government will say when the last one will be posted.",
                "The Voice of Rizia does not ask the King to give up Zille. We ask him to tell the nation how this ends, and what it " +
                "will cost. A king who cannot answer that question should not have asked it in Konstantium."));
        }
        else
        {
            News(key, "V", "Enough", P(
                "Every week of this war, another village in Brenas buries a son. Every week, the palace promises that the next " +
                "operation will be the last one.",
                "The Voice of Rizia calls on the King to seek an honorable peace through the Alliance of Nations, before the war " +
                "consumes what is left of his credit, at home and abroad."));
        }
    }

    // ------------------------------------------------------------------ abroad, as the war goes on

    private static void Abroad(int w)
    {
        if (w == 2)
        {
            bool turnedBack = WorldSystems.StraitClosedNow;
            News("ContanaConvoys", "G", turnedBack ? "Contanan Freighters Turned Back in the Strait" : "Contanan Aid Reaches Wehlen", P(
                "Three freighters chartered by United Contana and flying the Valgish flag have sailed for Wehlen carrying what the " +
                "Central Committee in Kyow calls \"humanitarian and fraternal assistance.\" Western intelligence sources say the holds " +
                "also carry artillery shells and aircraft parts.",
                turnedBack
                    ? "Lespian warships stopped the convoy in the Strait of Kreleck and escorted it to a Lespian port for inspection, where " +
                      "it remains. Kyow has called the inspection \"piracy in the service of a king.\""
                    : "The convoy is reported to have unloaded at the Wehzek ports facing the Strait of Kreleck. Analysts expect part of " +
                      "its cargo to reach the Zille garrison within days."));
            Dispatch("ContanaConvoys", "Rizia_Faction_Event_CSP", "Contana Arms Wehlen", P(
                "Our intelligence confirms that United Contana has begun shipping arms to Wehlen under Valgish flags. Contanan advisers " +
                "have been seen in Wraclawitz for the first time since the peacekeeping brigade left in 1943.",
                "Chairman Malenyev will not send soldiers. He does not need to, as long as the ships get through."));

            Dispatch("ValgslandFleet", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Rattles Its Sabres", P(
                "Chancellor Emmerich Hegel has announced naval exercises in the Markian Sea \"for as long as aggression continues in " +
                "South Merkopa.\" Our naval attaché reports two cruiser squadrons at sea and the Valgish submarine flotilla on alert.",
                "Our embassy believes the Chancellor wants to frighten us, not to fight us. It cannot be entirely sure, and it has asked " +
                "the Navy to keep its ships out of the Markian Sea."));
            News("ValgslandFleet", "G", "Valgish Fleet Exercises as Zille War Continues", P(
                "Valgsland has put to sea in force, announcing exercises in the Markian Sea that observers read as a warning to Rizia. " +
                "Chancellor Hegel told the People's Congress that the war in Zille is \"the oldest kind of crime, committed by the " +
                "oldest kind of government.\"",
                "Lespian and Rumburgian naval officials say they are watching the exercises closely. Nobody in Konstantium expects " +
                "Valgish ships to fire on anyone. Nobody is quite willing to rule it out, either."));

            Dispatch("Agnolia", "Rizia_Country_Agnolia", "Agnolia Offers Its Good Offices", P(
                "Prime Minister Maartin Van Hoorten has offered to host talks between Rizia and Wehlen \"at any time, on any terms " +
                "the parties can agree.\" Agnolia, he said, knows better than most what it means to share a coast with a larger neighbor.",
                "Our ambassador reports that the offer is sincere, and that Agnolian shipyards are, at the same time, sincerely " +
                "interested in Rizian orders."));
        }

        if (w == 3)
        {
            Dispatch("Arcasia", SuzerainTokenName.RiziaFactionEventATO, "Arcasia Weighs In", P(
                "President Dwight Walker's special envoy has toured Konstantium and Thornbourgh, urging \"a negotiated settlement that " +
                "respects the lawful expiry of the Zille lease.\" Our embassy notes that the envoy did not visit Wraclawitz.",
                "Arcasia will not fight for Zille. But Ventrie City has let it be known that Arcasian banks would look kindly on a " +
                "Rizian loan request, once the war is over and the region's gold is flowing again."));

            bool yielded = Variables.GetString("ZilleWar.Valgsland_CallAnswer") == "yielded";
            bool softCable = Variables.GetString("ZilleWar.Valgsland_CableAnswer") is "guarantee" or "mitz";
            if (WorldSystems.LespiaCaleqabiz && !yielded && !Variables.GetBool(SupplyWar.VarLespiaWithdrawn))
            {
                Dispatch("ValgslandQabiz", SuzerainTokenName.RiziaCountryValgsland, "Valgish Destroyers Off Qabiz", P(
                    "Two Valgish destroyers have anchored just outside our territorial waters off Caleqabiz, within sight of the Lespian " +
                    "warships berthed at the refueling center. Chancellor Hegel calls it \"a courtesy visit to international waters.\"",
                    "Our naval commander at Qabiz describes the visit as anything but courteous. He has asked for orders, and for patience " +
                    "from the Lespian captains, who have little of it."));
                News("ValgslandQabiz", "V", "Caleqabiz: A Repeat Of Zille?", P(
                    "Foreign warships berth in a Rizian harbor by royal arrangement, and now a second foreign navy has come to watch them. " +
                    "Twenty-five years ago Zille was signed away to save a throne. The Voice of Rizia hopes the present King has read the " +
                    "history books more carefully than his advisers have."));
            }
            else if (!yielded && !softCable)
            {
                Dispatch("ValgslandArms", SuzerainTokenName.RiziaCountryValgsland, "Valgish Ports Serve Wehlen", P(
                    "Valgish ports have openly begun handling Contanan cargoes bound for Wehlen, and Valgish crews are reported to be " +
                    "sailing some of the freighters. Chancellor Hegel calls it \"solidarity with a people under attack.\"",
                    "Our embassy believes Valgsland will go no further than this. It also notes that Valgsland has gone further than it " +
                    "said it would at every step so far."));
            }

            if (WorldSystems.RumburgFavors >= 2 && !Variables.GetBool("ZilleWar.Rumburg_SaidNo"))
            {
                Dispatch("RumburgFatigue", SuzerainTokenName.RiziaCountryRumburg, "Grumbling in Thornbourgh", P(
                    "Questions have been asked in the Rumburgian parliament about the cost of the Rizian war to Rumburg's own treasury, " +
                    (SupplyWar.RumburgOilCut ? "and about the price of the gas that replaced Wehzek oil. " : SupplyWar.RumburgOilHalved ? "and about the price of the gas that half-replaced Wehzek oil. " : "and about the price of the credits and the rifles. ") +
                    "The Queen's ministers answered them, but briefly.",
                    "Our ambassador was told over dinner, by a minister who had drunk just enough, that Her Majesty's patience is \"a " +
                    "renewable resource, but not an infinite one.\""));
            }
        }
    }

    // ------------------------------------------------------------------ good news

    private static void GoodNews(int w)
    {
        if (w == 2)
        {
            // The Turn 5 council may or may not have issued war bonds (EarlyT5.VarWarBonds).
            bool bonds = Variables.GetBool(EarlyT5.VarWarBonds);
            Dispatch("WarBonds", SuzerainTokenName.RiziaCityPorteDrazon, "The Diaspora Sends Its Savings", P(
                bonds
                    ? "Families who left Zille during the lease and the Wehlen Civil War, now settled in Thornbourgh, Ventrie City and the " +
                      "ports of Agnolia, have been buying Rizian war bonds in numbers that surprised the treasury. Some of the letters that " +
                      "come with the money are addressed simply to \"the King, for Zille.\""
                    : "Families who left Zille during the lease and the Wehlen Civil War, now settled in Thornbourgh, Ventrie City and the " +
                      "ports of Agnolia, have been sending money to the palace in plain envelopes, addressed simply to \"the King, for Zille.\" " +
                      "The Crown issued no war bonds, so the treasury has had to open a special account for gifts.",
                bonds
                    ? "Councilor Werner's office expects the bonds to bring in three billion guilders a quarter for as long as the war lasts. It has " +
                      "not forgotten that every one of them must one day be paid back."
                    : "Councilor Werner's office calls the sums modest, and the letters the best news it has had since the declaration."));
            News("Volunteers", "RH", "They Came Home to Fight", P(
                "Among the volunteers at the " + SafeTown() + " depot this week were forty young men who had never set foot in Rizia. Born " +
                "in Thornbourgh and Perla to families who fled Zille, they paid their own passage to enlist.",
                "\"My grandmother kept the key to her house in Zille for twenty-five years,\" one of them told the Royal Herald. \"I " +
                "promised her I would open the door.\""));
        }

        // A Wehzek officer crosses the lines with the garrison's maps: a real blow to the garrison.
        if (w >= 2 && !Variables.GetBool(VarDefector) && (w >= 3 || ZilleCampaign.Strength >= 6))
        {
            Variables.Set(VarDefector, true);
            if (ZilleCampaign.Strength > 3)
            {
                ZilleCampaign.Strength -= 1;
            }

            Dispatch("Defector", SuzerainTokenName.RiziaCityMontaklar, "A Wehzek Officer Crosses the Lines", P(
                "A major of the Wehzek coastal artillery walked across the lines at " + SafeTown() + " under a white handkerchief two nights " +
                "ago, carrying a briefcase full of maps. He says he is a Bludish Golcondist who has watched his men starve on half " +
                "rations while officers from Wraclawitz sold the garrison's fuel on the black market.",
                "The General Staff has spent two days with his maps, and believes them genuine. They show every gun position on the " +
                "Zille coast and the timetable of the night convoys. \"He has saved us a month,\" one staff officer said."));
            News("Defector", "G", "Wehzek Officer Defects to Rizia", P(
                "A Wehzek artillery officer has defected to Rizia, according to the Rizian General Staff, which says he brought detailed " +
                "plans of the Zille defenses with him. Wehzek state radio called the man \"a traitor and a drunk\" before the station's " +
                "announcer had finished reading his name.",
                "The defection, if confirmed, will raise questions in Wraclawitz about the morale of the Zille garrison."));
        }

        int heldTurn = Variables.GetInt(VarZilleHeldTurn);
        if (WorldSystems.ZilleCityHeld && heldTurn == 0)
        {
            Variables.Set(VarZilleHeldTurn, Turn);
            // Foreshadows the consulate files (SupplyVoices "The Consulate Files", or the Turn 8 security brief).
            Dispatch("DiplomaticQuarter", WorldSystems.ZilleToken, "The Diplomatic Quarter", P(
                "The consulates on Zille's harbor front came through the fighting with little more than broken windows. Most of their " +
                "staff left on the last ferries before our columns arrived. The Lespian consulate is the exception: its people stayed, " +
                "kept their shutters closed, and burned paper in the courtyard for two days.",
                "Next door, the Wehzek police headquarters was set alight by its own officers as they withdrew. The fire went out in the " +
                "rain before it reached the basement archives. Our engineers have been told to box up whatever is left and send it to " +
                "Porte Drazon unread."));
        }
        else if (WorldSystems.ZilleCityHeld && Turn > heldTurn)
        {
            Dispatch("MarketDay", WorldSystems.ZilleToken, "Market Day in Zille", P(
                "For the first time since the fighting began, the fish market on the Zille waterfront opened this week. Rizian and Wehzek " +
                "fishermen sold side by side, as they did before the war, under the eyes of a few bored military policemen.",
                "An old woman told our officers that she had kept a Rizian flag folded in her linen chest since 1927. She hung it from " +
                "her window on the morning the troops arrived, and has not taken it down since."));
            // Not under Mark Farus' byline: his column follows the phases of the operation, and one runs this turn as well.
            News("MarketDay", "RH", "Zille Remembers", P(
                "The children of Zille have learned the Rizian anthem faster than our soldiers have learned the streets. They sing it " +
                "at the harbor every evening, a little out of tune, while the fishing boats come in.",
                "There are shuttered windows in the Wehzek quarter, and some of the people behind them are afraid. But the city is " +
                "alive again, and it is alive under our flag."));
        }
    }

    // ------------------------------------------------------------------ two wars at once

    /// <summary>The first of Mave Nortus, Dapalle and Balle that Wehlen does not hold (a front town that is Rizian hands).</summary>
    private static string SafeTown()
    {
        foreach (string t in new[] { "Mave Nortus", "Dapalle", "Balle" })
        {
            if (!Variables.GetBool(WarFlags.Occupied(t)))
            {
                return t;
            }
        }

        return "Mave Nortus";
    }

    private static void TwoWars()
    {
        int first = Variables.GetInt(WorldSystems.VarTwoWarsTurn);
        bool lespia = WorldSystems.LespiaHelps;
        News("TwoFronts", "G", "The Golden Kingdom on Two Fronts", P(
            "With its army already fighting Wehlen in Zille, the Kingdom of Rizia has gone to war with the Grand Duchy of Pales. " +
            "Few states in Merkopa have tried to fight two neighbors at once, and military analysts doubt that Rizia has the " +
            "divisions, the fuel or the ships to do it for long.",
            "In Konstantium, diplomats who had grown used to calling the Zille war \"a local dispute\" have stopped doing so. " +
            (lespia && Variables.GetInt("RiziaDLC.War_Pales_LespianSupport") > 0
                ? "Lespia, which patrols the Strait of Kreleck on Rizia's behalf while arming Pales, has declined to say which of the two wars it supports."
                : lespia
                    ? "Lespia, whose navy patrols the Strait of Kreleck on Rizia's behalf, has said nothing at all about the second war."
                    : "The Alliance's Security Committee is expected to meet in emergency session.")));
        News("TwoFronts", "RH", "Axa Victoria, North and South", P(
            "Twice in one year, the enemies of Rizia have refused to give back what is ours, and twice His Majesty has answered them. " +
            "Our soldiers march south to Pales and north to Zille under the same golden banner.",
            "Let the chancelleries of Merkopa count our divisions. The Royal Herald counts our hearts, and there are enough of them " +
            "for every front."));
        News("TwoFronts", "V", "Two Wars, One Treasury", P(
            "The King went to war with Wehlen in a foreign capital. Now he has gone to war with Pales as well. The same farm boys " +
            "who took the night trains north are now being sent south, and the treasury that could not pay for one war is being asked " +
            "to pay for two.",
            "\"Butcher Romus,\" the crowds in Bronaz have started to call him. We wish we could tell them they were wrong."));

        Dispatch("TwoFronts_Staff", SuzerainTokenName.RiziaCityMontaklar, "Two Fronts, One Army", P(
            "The General Staff in Montaklar has divided its maps between two tables, one for Pales and one for Zille. Every division, " +
            "every fuel train and every ship committed to one front is now argued over by the officers of the other.",
            "The staff's first assessment is blunt: the army can attack on one front and hold on the other, but not attack on both " +
            "at once for long."));
        // Beatrice may already have told him on the telephone that Rumburg is done paying for Zille (SupplyBeatrice).
        bool saidNo = Variables.GetBool(SupplyWar.VarRumburgSaidNo);
        Dispatch("TwoFronts_Rumburg", SuzerainTokenName.RiziaCountryRumburg, "A Letter from Queen Beatrice", P(
            "\"Romus,",
            "Two wars. My ministers asked me this morning whether my brother-in-law had a plan, and for once I had nothing clever to " +
            "say to them.",
            saidNo
                ? "I told you on the telephone that Rumburg could do no more for Zille. I did not imagine you would take it as an " +
                  "invitation to start a second war. Rumburg will honor what it owes GRACE in Pales, and not one crate more. Finish one " +
                  "of them. Soon."
                : "Rumburg will stand by you in Pales, as it has in Zille. But I am not made of money, whatever they say in Wraclawitz, and " +
                  "I am not going to pay for both of your adventures twice over. Finish one of them. Soon.",
            "Beatrice\""));
        Dispatch("TwoFronts_Wehlen", SuzerainTokenName.RiziaCountryWehlen, "Smolak Salutes Pales", P(
            "President Smolak has sent a public message of solidarity to Grand Duke Axel Reinhart, calling Pales and Wehlen \"two " +
            "small nations who have learned what a Rizian king's friendship is worth.\"",
            "Our agents report no Wehzek troops moving toward Pales. Wehlen has no men to spare. But Smolak's message was read out on " +
            "Palesian radio within the hour."));

        if (Turn > first)
        {
            News($"TwoFronts_T{Turn}", "V", "Which War Is This?", P(
                "A mother in Dapalle told our reporter that her elder son is in Zille and her younger in Pales, and that she no longer " +
                "reads the communiques, because she cannot tell which of them to be afraid of.",
                "The palace should not need a newspaper to tell it that a country has only so many sons."));
        }
    }

    // ------------------------------------------------------------------ after the war

    private static void AfterWar(int since)
    {
        if (since < 1)
        {
            return;     // the war's end itself is in the papers already
        }

        if (WorldSystems.Liberated)
        {
            if (since == 1)
            {
                Dispatch("Reconstruction", SuzerainTokenName.RiziaCityPorteDrazon, "Rebuilding Zille", P(
                    "Councilor Werner's office has presented the bill for rebuilding Zille: the harbor piers, the rail line to Nanta and " +
                    "the power stations that serve the mines. The work will weigh on the treasury for some time.",
                    "The good news is in the second half of the memorandum. Engineers expect the Auremina mines to reopen within months, " +
                    "and the port of Zille is already handling the first Rizian cargoes in a generation."));
                News("GoldReturns", "G", "Zille Gold Returns to Rizian Hands", P(
                    "With the Zille region back under Rizian sovereignty, analysts expect its gold mines to return to production within " +
                    "the year. Rizian Royal Gold has announced plans to move its headquarters back to the city of Zille.",
                    "Wehlen has not recognized the change, and the AN has not been asked to. For the markets, the question has already been " +
                    "answered."));
                Dispatch("HegelPrecedent", SuzerainTokenName.RiziaCountryValgsland, "Hegel Calls Our Victory a Precedent", P(
                    "Chancellor Emmerich Hegel has called our victory in Zille \"a precedent Valgsland will not allow to be repeated,\" and " +
                    "has proposed that the Contanan Security Pact guarantee the borders of its partners in South Merkopa.",
                    "Our embassy reads the proposal as a message for Pales and Morella as much as for us."));
                if (Taken("Nanta"))
                {
                    News("Inovitz", "RH", "Inovitz Stays in Nanta", P(
                        "Marzcel Inovitz, the Wehzek singer whose seaside mansion outside Nanta has been his home for twenty years, has " +
                        "announced that he will not leave for Wehlen. \"My house is in Nanta, my audience is in Nanta, and my mother is " +
                        "buried in Nanta,\" he told reporters. \"Whose flag is on the town hall is a matter for kings.\"",
                        "He will give his first concert as a resident of the Kingdom in Porte Drazon next month. Tickets sold out in an hour."));
                    News("Inovitz", "V", "A Song for Both Sides", P(
                        "For once, the Voice of Rizia and the Royal Herald agree on something: Marzcel Inovitz should stay. If the Wehzeks " +
                        "of Zille are to become Rizians, they will need more welcomes like the one his concert will get, and fewer like the " +
                        "ones some of them received in the harbor district."));
                }
            }
            else if (since == 2)
            {
                Dispatch("WehzekResidents", WorldSystems.ZilleToken, "The Wehzeks Who Stayed", P(
                    "A delegation of Wehzek residents of Zille and Nanta has asked the military administration for written guarantees of " +
                    "their property, their schools and their right to worship. Most of them were born in the region during the lease.",
                    "The administration has promised an answer from Porte Drazon. Su Omina has promised an answer of its own, and the " +
                    "delegation took the second promise more seriously than the first."));
            }

            return;
        }

        // Elena Werner's warning at the Turn 5 council: patriotism is the cheapest money there is, until the war goes badly.
        if ((WorldSystems.Defeated || WorldSystems.Ceasefire) && since == 1 && Variables.GetBool(EarlyT5.VarWarBonds) &&
            WorldSystems.Once("R_WarBondsDue"))
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run(
                "Variable[\"RiziaDLC.Resources_Budget\"] = (Variable[\"RiziaDLC.Resources_Budget\"] or 0) - 1");
            Report("W_WarBondsDue", SuzerainTokenName.RiziaCityPorteDrazon, "The War Bonds Fall Due", P(
                WorldSystems.Defeated
                    ? "The war bonds issued after Konstantium now change hands on the Porte Drazon exchange at a third of their face value. " +
                      "Since the news from Zille, small holders have queued outside the Royal Treasury every morning to sell them back."
                    : "The war bonds issued after Konstantium were sold on the promise of a Rizian Zille. With a ceasefire line through the " +
                      "province, they now change hands at half their face value, and small holders have begun queueing at the Royal Treasury.",
                "Councilor Werner has bought back the first of them to stop the panic, at a cost of three billion guilders. Her covering note " +
                "to the palace is a single line: \"I said patriotism would become the most expensive money there is. I take no pleasure in " +
                "being right.\""));
        }

        // A lost war that still holds Zille city (the "divided" outcome): the army falls back on the city, not across the river.
        if (WorldSystems.Defeated && WorldSystems.ZilleCityHeld)
        {
            if (since == 1)
            {
                bool hills = Taken("Auremina");
                Dispatch("ArmyHolds", WorldSystems.ZilleToken, "Digging In on the Armistice Line", P(
                    hills
                        ? "The Royal Army is turning the line it reached into a frontier: trenches on the ridges north of Auremina, wire across " +
                          $"the coast road, and observation posts from which Wehzek sentries can be seen changing the guard outside {Front.WehzekWords}."
                        : "The Royal Army is turning the line it reached into a frontier: a ring of trenches beyond the last tram stop of Zille " +
                          "city, wire across the valley road, and observation posts from which the Wehzek positions in the hills can be watched.",
                    "Refugees from the villages beyond the line, who cheered our columns a few weeks ago, now crowd the warehouses on the " +
                    "waterfront. Most of them say they cannot go back."));
                Dispatch("ReprisalsHills", WorldSystems.ZilleToken, "Reprisals in the Hills", P(
                    "Our sources behind the Wehzek lines report arrests in every village that welcomed our troops. The Ariana Toras " +
                    "Association's former organizers are being sought by name, and the families of Su Omina members have been ordered to leave.",
                    "In Zille city itself, people have begun asking our officers, quietly, what will happen to them if the army leaves."));
                News("DefeatHeld", "V", "Half a Victory, Whole Graves", P(
                    "The King still holds Zille city, and the palace would like us to call that a success. It is a city with a Wehzek army " +
                    "a few miles up the road, supplied by sea, and claimed by a president who has promised to be back for it.",
                    "The King owes the nation an explanation. He also owes the people of the hill villages, who hung out our flag and are " +
                    "now paying for it, something better than silence."));
            }

            return;
        }

        if (WorldSystems.Defeated)
        {
            if (since == 1)
            {
                Dispatch("ArmyHome", SuzerainTokenName.RiziaCityMaveNortus, "The Reservists Come Home", P(
                    "The first battalions stood down from the Zille front crossed back over the river at " + SafeTown() + " this week, leaving " +
                    "the armistice line to the regulars. There were no bands and no crowds. Townspeople brought water to the trucks and " +
                    "did not ask questions.",
                    "Refugees from the Zille villages who followed the army south now fill the warehouses by the station. Most say they " +
                    "cannot go back."));
                Dispatch("Reprisals", WorldSystems.ZilleToken, "Reprisals in Zille", P(
                    "Our sources inside Zille report arrests in every district that welcomed our troops. The Ariana Toras Association's " +
                    "former organizers are being sought by name, and the families of Su Omina members have been ordered out of the region.",
                    "Smolak Square has been cleaned and repainted, and the President's portrait hangs over it again."));
                News("Defeat", "V", "The Price of Pride", P(
                    (Front.Held.Count > 0
                        ? $"Our soldiers hold {Front.HeldWords}, but Zille city, the prize the King promised, is further away today than it " +
                          "was before the war. Wehlen calls the lease void, the Alliance calls us aggressors, "
                        : "Zille is further away today than it was before the war. Wehlen calls the lease void, the Alliance calls us aggressors, ") +
                    "and the families of Zille who hung out our flag are paying for it in Wehzek prisons.",
                    "The King owes the nation an explanation. He also owes it to the people of Zille not to forget them."));
            }

            return;
        }

        if (WorldSystems.Ceasefire && since == 1)
        {
            Dispatch("ArmisticeLine", WorldSystems.ZilleToken, "Life on the Armistice Line", P(
                "AN observers in blue armbands now patrol the ceasefire line in Zille. Farmers on both sides need a pass to reach their " +
                "own fields, and a wedding in a border village was held in two halves, one on each side of the wire.",
                "Both armies are still dug in. Neither government calls this peace."));
            Dispatch("SmolakLine", SuzerainTokenName.RiziaCountryWehlen, "Smolak Calls the Line a Wound", P(
                "President Smolak has told the Wehzek parliament that the ceasefire line in Zille is \"a wound, not a border,\" and " +
                "that Wehlen will raise the question at every session of the Alliance of Nations until it is closed.",
                "Our embassy in Sordland reports that Wehlen is buying artillery from anyone who will sell it."));
        }
    }
}
