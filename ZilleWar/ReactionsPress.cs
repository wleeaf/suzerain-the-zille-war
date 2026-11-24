using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;

namespace ZilleWar;

/// <summary>
/// The papers react to two wartime policies that nothing else ever read back:
/// - RefugeeBoats: the King's Turn 5 answer on the Wehzek boats in the Gulf of Moridia (EarlyT5 B_R1 / B_R3,
///   <see cref="EarlyT5.VarRefugeePolicy"/> "corridor" / "closed"). At the first step of Turn 6 (before Phase III) the Voice of
///   Rizia reports a turned-back boat that capsized ("closed"), or Geopolitico reports the corridor's reception abroad
///   ("corridor"). Lucita pays off her Turn 5 warning on the way into Terador (T6 P_LucitaAzaroUpdate). "screen" is told at the
///   treaty (SmolakInterneesAtTreaty).
/// - VoiceZilleRegime: the Turn 8 occupation policy of a liberated Zille (<see cref="LateT8.VarOccupation"/> martial / amnesty /
///   generals), in the Voice of Rizia and the Royal Herald on the step after the security brief.
/// Perception effects are lasting rows in vanilla's perception recompute, switched by a value this module keeps for a two-turn
/// window (one-off changes would be erased by the recompute). Every item is war-only: both policies exist only in a war game.
/// </summary>
internal static class ReactionsPress
{
    // ---- once-keys (registered: saved with the game, cleared by a new game)
    private const string NewsBoats = "ZilleWar.News_Boats";
    private const string NewsOccupation = "ZilleWar.Late_News_Occupation";

    // ---- perception windows: -1, 0 or +1 while the window is open (exposed to expresso as numbers)
    private const string PerceptionBoats = "ZilleWar.Press_BoatsPerception";
    private const string PerceptionOccupation = "ZilleWar.Press_OccupationPerception";

    /// <summary>The policy turns: the boats are decided on Turn 5, the occupation on Turn 8. Each row lasts until policy turn + 2.</summary>
    private const int BoatsTurn = 5;
    private const int OccupationTurn = 8;

    private const string PerceptionRows = "Diplomacy_GlobalPerceptionMisc";

    private const string Lucita = "Lucita Azaro";

    public static void Register()
    {
        foreach (string v in new[] { NewsBoats, NewsOccupation, PerceptionBoats, PerceptionOccupation })
        {
            Variables.Register(v);
        }

        Vanilla.Expose(PerceptionBoats, isBool: false);
        Vanilla.Expose(PerceptionOccupation, isBool: false);
        foreach (string v in new[] { PerceptionBoats, PerceptionOccupation })
        {
            WarDecrees.AddRow(PerceptionRows, $"{v} <= -1", "RiziaDLC.Country_GlobalPerception -= 1;");
            WarDecrees.AddRow(PerceptionRows, $"{v} >= 1", "RiziaDLC.Country_GlobalPerception += 1;");
        }

        LucitaOnTheBoats();
    }

    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !Variables.GetBool(WarFlags.Declared))
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0;
        CloseWindow(PerceptionBoats, BoatsTurn, turn);
        CloseWindow(PerceptionOccupation, OccupationTurn, turn);
        // martial law lifted on Smolak's Turn 9 call (ReactionsForeign): the curfew story stops counting against Rizia
        if (Variables.GetBool(ReactionsForeign.VarMartialLifted) && Variables.GetInt(PerceptionOccupation) < 0)
        {
            Variables.Set(PerceptionOccupation, 0);
        }

        Boats(turn);
        Occupation(turn);
    }

    /// <summary>A perception row lasts while the turn is at most the policy turn + 2.</summary>
    private static void CloseWindow(string v, int policyTurn, int turn)
    {
        if (turn > policyTurn + 2 && Variables.GetInt(v) != 0)
        {
            Variables.Set(v, 0);
        }
    }

    private static bool Once(string guard)
    {
        if (Variables.GetBool(guard))
        {
            return false;
        }

        Variables.Set(guard, true);
        return true;
    }

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);

    // ================================================================== RefugeeBoats

    private static string Policy => Variables.GetString(EarlyT5.VarRefugeePolicy) ?? string.Empty;

    /// <summary>
    /// Turn 6 only (an older save loaded later does not get a stale story). The policy is set at T5 s4 only while the war is
    /// fought, and nothing ends the war between T5 s4 and Phase III (T6 s1), so the war is normally still on here; the closing
    /// line of the Voice still follows the state, for a save loaded mid-turn.
    /// </summary>
    private static void Boats(int turn)
    {
        string policy = Policy;
        if (turn != 6 || policy is not ("closed" or "corridor") || !Once(NewsBoats))
        {
            return;
        }

        if (policy == "closed")
        {
            Variables.Set(PerceptionBoats, -1);
            NewsWire.Publish("Boats", "V", "The Boat Off Sallabes", P(
                "A fishing boat carrying Wehzek families from the Zille coast capsized in the Gulf of Moridia last week, hours after a " +
                "Rizian navy patrol turned it back off Sallabes. Nine people drowned, all of them women and children. Fishermen " +
                "brought the bodies ashore the next morning.",
                "Wehzek radio has read out the name of the patrol boat every hour since. The Admiralty says only that its crew followed " +
                "orders. Nobody in Porte Drazon disputes that. The orders are the trouble.",
                "The Palace told the navy to turn the boats back because the navy had a war to fight. " +
                (WarFlow.AtWar
                    ? "The King has a war to fight. So, it seems, has the sea."
                    : "The guns in Zille have fallen silent. The sea, it seems, has not.")));
            return;
        }

        Variables.Set(PerceptionBoats, 1);
        NewsWire.Publish("Boats", "G", "Alliance Relief Officials Praise Rizia's Gulf Corridor", P(
            "PORTE DRAZON - Several thousand Wehzek civilians from the Zille coast have reached Rizia through the corridor across the " +
            "Gulf of Moridia that the Rizian government opened to families fleeing the fighting, Alliance of Nations relief officials " +
            "said this week.",
            "\"In a war, a government is rarely judged by what it does for the enemy's civilians,\" one official told Geopolitico. " +
            "\"Porte Drazon has given us one decision we can praise without reservation.\" The families are housed in reception " +
            "centers in Brenas, at the Rizian treasury's expense.",
            "Wraclawitz takes a different view. Wehzek state radio claims that Rizian intelligence officers walk the reception centers " +
            "recruiting informers among the refugees, and calls the corridor \"a door left open for spies.\" The Rizian government " +
            "dismissed the claim. Neither side's account could be independently verified."));
    }

    /// <summary>
    /// Terador, Turn 6 s9 (c375): on the way from the airbase into the city, Lucita pays off her Turn 5 warning
    /// ("remember that you gave the order... when the first one doesn't make it home"). Only after the Voice story ran, and only
    /// while she is free (the fragment itself also needs Lucita_Arrested == false).
    /// [778] "On the way back to the motorcade, she asked me to accompany her downtown." -> [8] (Terador's center).
    /// </summary>
    private static void LucitaOnTheBoats()
    {
        var t = new Talk("Rizia/Turn 6/P_LucitaAzaroUpdate", "ZilleWar.RP.Lucita.");
        string cond = $"{WarFlags.LDeclared} and Variable[\"{EarlyT5.VarRefugeePolicy}\"] == \"closed\" and " +
                      $"Variable[\"{NewsBoats}\"] == true and Variable[\"RiziaDLC.Lucita_Arrested\"] ~= true";
        t.Line("Boats", Narrator,
            "In the car, she took a folded newspaper from her coat and laid it on my knee, open at the Voice of Rizia's story about the boat off Sallabes.",
            new[] { "Boats2" }, condition: cond, hooks: From(778));
        t.Line("Boats2", Lucita,
            "\"I asked you to remember the order, Your Majesty, when the first one didn't make it home. Nine, this time.\"",
            new[] { "Boats3" });
        t.Line("Boats3", Narrator, "She said nothing more until the car stopped.", new[] { "#8" });
        t.Register();
    }

    // ================================================================== VoiceZilleRegime

    private static bool Decree(string key) => Variables.GetBool($"ZilleWar.Decree_{key}");

    /// <summary>
    /// Turn 8, on the first step after the security brief set the policy (it is set at T8 s1, which the step evaluation at s1
    /// precedes). Liberated games only: the choice is offered only there.
    /// </summary>
    private static void Occupation(int turn)
    {
        string occ = Variables.GetString(LateT8.VarOccupation) ?? string.Empty;
        if (turn != 8 || occ is not ("martial" or "amnesty" or "generals") || !ZilleCampaign.Liberated || !Once(NewsOccupation))
        {
            return;
        }

        bool schools = Decree("WehzekSchools"), oath = Decree("LoyaltyOath");
        NewsWire.Batch(() =>
        {
            switch (occ)
            {
                case "martial":
                    Variables.Set(PerceptionOccupation, -1);
                    NewsWire.Publish("Occupation", "V", "Zille Under Curfew", P(
                        "In the Wehzek streets of Zille's harbor district, the curfew now begins at eight. The shutters come down at half " +
                        "past seven, and the army patrols that follow them have orders to detain anyone still outdoors.",
                        (schools
                            ? "The King's own decree guarantees Wehzek schools in Zille. On Tuesday the army arrested a Wehzek schoolteacher " +
                              "anyway, for teaching his class a Wehzek song."
                            : oath
                            ? "The loyalty oath was meant to make the teachers of Zille the Crown's men. On Tuesday the army arrested one of " +
                              "them for teaching his class a Wehzek song."
                            : "On Tuesday the army arrested a schoolteacher. His offense, his neighbors say, was teaching his class a Wehzek song.") +
                        " He is held without charge under the emergency decrees.",
                        "The Palace says martial law will stay until the last Wehzek rifle is handed in. The Voice of Rizia would like to " +
                        "know how many songs that will take."));
                    NewsWire.Publish("Occupation", "RH", "Order in the North", P(
                        "Martial law will remain in force in Zille until the last Wehzek rifle has been handed in, the Palace confirmed this " +
                        "week. The army reports that the streets of the liberated city are quiet after dark, and that weapons hidden during " +
                        "the war are surrendered every day.",
                        "\"Zille has known enough disorder,\" an officer of the garrison told the Herald. \"The people of the city want to " +
                        "sleep at night. His Majesty has made sure they can.\""));
                    break;

                case "amnesty":
                    Variables.Set(PerceptionOccupation, 1);
                    NewsWire.Publish("Occupation", "RH", "An Amnesty for Zille", P(
                        "His Majesty has granted an amnesty to every Wehzek conscript in Zille who lays down his arms. No man who hands in " +
                        "his rifle will be prosecuted for having served under the Wehzek flag.",
                        "\"The King came to Zille as a liberator, not a jailer,\" a palace spokesman said. \"Those who wish to live in peace " +
                        "under the Crown of Rizia will be welcome to do so.\""));
                    NewsWire.Publish("Occupation", "V", "A Kinder Peace", P(
                        "The Voice of Rizia has not often had reason to praise the Palace's conduct in Zille. This week it has one. The " +
                        "amnesty for Wehzek conscripts is the right decision, and it was not the easy one: the generals wanted martial law.",
                        (Policy == "closed"
                            ? "The Crown that turned the Wehzek boats back in the Gulf during the war has found some mercy since, and we are glad of it. "
                            : "") +
                        (oath
                            ? "It sits oddly beside the oath now demanded of every teacher and clerk in Zille. But a kinder peace is still a peace worth having."
                            : "A kinder peace is still a peace worth having. We hope it lasts longer than the Palace's patience.")));
                    break;

                default:
                    string admin = Variables.GetString(EarlyT7.VarZilleAdmin) ?? string.Empty;
                    NewsWire.Publish("Occupation", "V", "Who Governs Zille?", P(
                        admin switch
                        {
                            "brenas" => "Zille is to return to the province of Brenas, the King told the House of Delegates. Until it does, the " +
                                        "army governs it, and the Palace now says it will leave the city to the generals for the time being.",
                            "imperii" => "Zille is to be governed from Porte Drazon, the King told the House of Delegates. Porte Drazon has yet " +
                                         "to send anyone to do it, and the Palace now says it will leave the city to the generals for the time being.",
                            "undecided" => "The House of Delegates argued over who should govern Zille and the King decided that it could wait. " +
                                           "It is still waiting. The Palace now says it will leave the city to the generals for the time being.",
                            _ => "Months after the liberation, no civilian official governs Zille. The Palace says it will leave the city to " +
                                 "the generals for the time being.",
                        },
                        "So colonels decide who may open a shop, which schools may teach, and whose house is billeted. They did not ask for " +
                        "the work, and the people of Zille were not asked at all.",
                        "A province is not a barracks. The Voice of Rizia asks the Palace a simple question: who governs Zille, and when " +
                        "will he arrive?"));
                    break;
            }
        });
    }
}
