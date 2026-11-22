using SuzerainModdingKit;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Sweep of Turns 4 to 6: everything the Zille war changes that the first pass (EarlyT4/T5/T6) left out.
/// Conversations are split by scene into SweepEarly*.cs; shared helpers live in <see cref="SweepEarlyKit"/>. This file keeps
/// what belongs to no single scene:
/// - the Turn 4 newspaper, printed after the declaration at the AN, whose Pales and welfare stories still speak of peace;
/// - the Turn 5 election campaign (endorsements, closing rallies) held while the war is fought, and a campaign report.
/// </summary>
internal static class SweepEarly
{
    private const string ReportCampaign = "ZilleWar.SE_Report_WarCampaign";

    /// <summary>Called once from OnInitializeMelon: register variables, conversations, injections and vanilla edits.</summary>
    public static void Register()
    {
        Variables.Register(ReportCampaign);

        Run("religion", SweepEarlyReligion.Register);
        Run("t5 family", SweepEarlyT5Family.Register);
        Run("t5 security", SweepEarlyT5Security.Register);
        Run("t5 diplomacy", SweepEarlyT5Diplomacy.Register);
        Run("t6 council", SweepEarlyT6Council.Register);
        Run("t6 home", SweepEarlyT6Home.Register);
        Run("t6 personal", SweepEarlyT6Personal.Register);
        Run("t6 war", SweepEarlyT6War.Register);

        TurnFourPaper();
        ElectionPaper();
    }

    /// <summary>Called at the start of every Rizia story step, after WarFlags.Sync and Vanilla.Apply.</summary>
    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia))
        {
            return;
        }

        Run("religion step", SweepEarlyReligion.OnStep);
        Run("t5 family step", SweepEarlyT5Family.OnStep);
        Run("t5 security step", SweepEarlyT5Security.OnStep);
        Run("t5 diplomacy step", SweepEarlyT5Diplomacy.OnStep);
        Run("t6 council step", SweepEarlyT6Council.OnStep);
        Run("t6 home step", SweepEarlyT6Home.OnStep);
        Run("t6 personal step", SweepEarlyT6Personal.OnStep);
        Run("t6 war step", SweepEarlyT6War.OnStep);
        Run("press", ApplyPress);
        Run("decisions", ApplyDecisions);
        Run("campaign report", CampaignReport);
    }

    private static void Run(string what, Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Core.Log.Error($"SweepEarly {what} failed: {ex}");
        }
    }

    // ------------------------------------------------------------------ the Turn 4 newspaper

    /// <summary>
    /// The Turn 4 paper comes out after the AN session, in the same edition as the declaration of war. The stories filed
    /// before Konstantium (the Aureus arbitration, the Pales deal, the blockade, the welfare promise) learn what happened at
    /// the end of the turn.
    /// </summary>
    private static void TurnFourPaper()
    {
        News("Turn04_RH_Arbitration", (_, _) => P(
            "In a landmark move demonstrating its respect for international law, the Kingdom of Rizia has officially entered into " +
            "AN arbitration over the contested Aureus gas field, which it shares a claim over with the Grand Duchy of Pales.",
            "The arbitration process, set to unfold over the coming months, will witness both nations presenting their respective " +
            "arguments. Rizia, armed with strong historical, geographical, and legal evidence, is optimistic about its position. " +
            "Analysts speculate that an outcome favoring Rizia could usher in a period of economic prosperity and energy stability " +
            "for the kingdom.",
            "Those abroad who now accuse His Majesty of preferring the sword to the courtroom would do well to note the difference. " +
            "Where a fair hearing was offered, over Aureus, the Crown sent its lawyers to Konstantium. Where a lease ran out and " +
            "Wehlen refused, before the whole Alliance, to hand Zille back, the Crown was left no such choice. We hope for wisdom, " +
            "fairness, and justice to prevail in both."));

        News("Turn04_RH_PalesDeal", (_, _) => P(
            "The royal government of Rizia announced today that, following private talks between His Majesty King Romus and Duke " +
            "Axel Reinhart, an agreement has been reached with the Grand Duchy of Pales over the disputed Aureus gas field, marking " +
            "a decisive victory for diplomacy and regional stability.",
            "The exact nature of the agreement is not yet known. What is certain is that what could have turned into a nasty dispute " +
            "between our two nations has been resolved with typical Toras grace, and that the Kingdom's southern border is quiet at " +
            "the very moment our soldiers are needed in the north."));

        News("Turn04_G_RiziaPales_GasDeal", (_, _) => P(
            "A gas dispute between the nations of Rizia and Pales has been narrowly avoided, sources tell Geopolitico.",
            "Political experts feared a clash when Rizia challenged the Grand Duchy's claim on the newly discovered undersea gas " +
            "field known as Aureus. However, during a private meeting between Rizian monarch King Romus Toras and Palesian leader " +
            "Duke Axel Reinhart, the leaders were apparently able to reach a deal without the need for third-party arbitration.",
            "There has obviously been no love lost between the Kingdom of Rizia and the Grand Duchy of Pales since the former's " +
            "failed campaign to retake the latter by military force 30 years ago. The deal was first greeted as a sign of a more " +
            "peaceful direction for the two neighbors. In Konstantium it is now read differently: days after it was struck, King " +
            "Romus declared war on Wehlen, and a quiet southern border was exactly what he needed."));

        News("Turn04_G_RiziaPales_NavalBlockade", (_, _) => P(
            "A potentially violent gas dispute is brewing between the nations of Rizia and Pales.",
            "After the Grand Duchy staked a claim on Aureus, a large undersea natural gas field located roughly between the two " +
            "countries, the Rizian government opted for a military response, sending naval reinforcements to block the field before " +
            "the Palesians could begin exploratory drilling.",
            "There has obviously been no love lost between the Kingdom of Rizia and the Grand Duchy of Pales since the former's " +
            "failed campaign to retake the latter by military force 30 years ago. We fear it will not take much to rekindle old " +
            "tensions, all the more so since the same Rizian navy is now at war with Wehlen in the north. Naval observers in " +
            "Konstantium doubt that King Romus can keep ships at Aureus and off the Zille coast for long. Expect further updates as " +
            "the situation develops."));

        News("Turn04_V_Decision_WelfareProtest_PromisesMade", (_, _) => P(
            "In response to the tens of thousands that made their voice heard at the Monqiz Palace gates, King Romus has now " +
            "promised to increase healthcare and education spending, promising rapid improvements to our welfare system.",
            "Days later, the same King declared war on Wehlen. Wars have a way of eating hospital budgets first, and protest " +
            "leaders who left the palace gates hopeful are already asking whether the promise survived Konstantium.",
            "We at the Voice of Rizia will hold the King accountable, and will dutifully report whether the money goes to clinics " +
            "and classrooms, or to shells for the guns in Zille."));
    }

    // ------------------------------------------------------------------ the Turn 5 election in wartime

    private static void ElectionPaper()
    {
        News("Turn05_RH_ElectionEndorsement", (s, _) => P(
            "Dear Royal Herald readers,",
            "We live in tumultuous times. " +
            By(s, "Our sons are fighting in Zille as these lines go to print.",
                "Our sons have brought Zille home, and paid for it dearly.",
                "Our sons went to Zille, and not all of them came back.") +
            " It is only by the grace of the blessed Rizian monarchy that we are able to muddle through. And so this year, it is " +
            "more important than ever that the Rizian National Coalition take the helm in your provincial councils and the House of " +
            "Delegates.",
            "Only the RNC can be counted on to protect the values and traditions we hold dear, to stand behind our soldiers without " +
            "hesitation, and to safeguard the interests of hardworking landowners and the commoners who depend on them. The party's " +
            "vision aligns seamlessly with the enduring legacy of the Rubaz dynasty, ensuring that the bedrock of our society remains " +
            "firm amidst the storms of change.",
            By(s, "A House divided in wartime is a gift to Wraclawitz.",
                "Zille has come home. Now it must be governed by steady hands.",
                "In hard days, a House divided is a gift to Wraclawitz.") +
            " It is, therefore, with utmost conviction that The Royal Herald endorses the Rizian National Coalition for this year's " +
            "provincial elections and the House of Delegates. Stand with them, stand with our soldiers, stand with Rizia."));

        News("Turn05_V_ElectionEndorsement", (s, _) => P(
            "For the first time in forever, the Voice of Rizia has the opportunity to endorse a political party in the Rizian " +
            "provincial council elections. And this shouldn't come as a surprise, but we urge you to vote for the Rizian People's Party.",
            "Yes, King Romus has made some progressive decisions over the past several years -- including the decision to allow this " +
            "very paper to exist. But that's only because the RPP was there in Porte Drazon to remind him of the interests of the " +
            "common Rizian. And with this year's elections, we have the opportunity to make sure that those interests are broadcast " +
            "louder and clearer than ever before.",
            By(s,
                "And yes, the country is at war. That is not a reason to stay home. It is the reason to vote. The House of Delegates " +
                "may be the only place left where someone will stand up and ask how long the war in Zille will last, how many more " +
                "trains will go north, and who will pay for them. The RNC will not ask. The RPP will.",
                "The guns in Zille have fallen silent, and the Palace will want the victory counted as a vote for the King. It is not. " +
                "Someone must still ask what it cost, and who will pay the bill. The RNC will not ask. The RPP will.",
                "The war in Zille is over, and it was not won. Someone must ask how a war declared from a podium in Konstantium ended " +
                "like this, and who will pay for it. The RNC will not ask. The RPP will."),
            "If everyone who's reading this votes, we may even win a majority in the House of Delegates for the first time in history.",
            "So please: Vote RPP in '55!"));

        News("Turn05_RH_ElectionCampaignEnd", (s, _) => P(
            "Both the RNC and the RPP held their final events of the electoral campaign, as millions of Rizians prepare to head to " +
            "the polls to fulfill their democratic duty. While both parties held mass closing rallies, the traditional RNC " +
            "pre-election rally on Monqiz was by far the best attended, drawing hundreds of thousands of supporters to the city.",
            By(s,
                "Many came with the names of sons and brothers serving in Zille pinned to their coats, and the whole square fell " +
                "silent for a minute in honor of the fallen before RNC leader Daria de Rava took the stage.",
                "The rally became a victory celebration for Zille, with wounded veterans of the campaign given the front rows and " +
                "a standing ovation that lasted longer than any speech.",
                "The whole square fell silent for a minute for the soldiers who did not come home from Zille before RNC leader Daria " +
                "de Rava took the stage."),
            "We at the Royal Herald continue to root for the King's party and are convinced that the people of Rizia will make the " +
            "right choice at the ballot box."));

        News("Turn05_V_ElectionCampaignEnd", (s, _) => P(
            "Both the RNC and RPP have ended their electoral campaigns for upcoming elections, as the nation prepares to head for the polls.",
            By(s,
                "It was a campaign fought in the shadow of the war in Zille. RNC candidates wrapped themselves in the flag, while RPP " +
                "candidates who asked what the war is costing were heckled as defeatists at their own rallies.",
                "It was a campaign held in the glow of victory in Zille, and the RNC made the most of it, as if the soldiers had " +
                "marched north for the party and not for the country.",
                "It was a campaign held in the shadow of what happened in Zille, and the RNC did its very best to talk about anything else."),
            "We at the Voice of Rizia would like to congratulate the RPP on a successful campaign that brought inspiration and hope " +
            "for change to millions across the country, whatever the end result may be. We also hope the election will be held " +
            "fairly, that the Crown will respect the democratic result, and that nobody will be called a traitor for the way they voted."));

        // Election day, Turn 6: the papers go to print with the soldiers' ballots in mind.
        AppendNews("Turn06_RH_ElectionDay", s => By(s,
            "Special polling stations have been opened behind the lines in Zille, where soldiers of the northern army cast their " +
            "ballots between shifts at the front. Their votes, the Herald is told, will be flown to Porte Drazon in sealed sacks.",
            "Among the first in line in Porte Drazon were veterans of the Zille campaign, many still in uniform, cheered by the queue " +
            "as they walked to the front of it.",
            "Families of the soldiers who fell in Zille were given places of honor at polling stations across Brenas."));
        AppendNews("Turn06_V_ElectionDay", s => By(s,
            "Remember too the men who cannot vote today because they are in a trench outside Zille. Vote for them as well.",
            "The Palace will tell you that a vote for the King's party is a vote for the men who took Zille. It is not. They fought for " +
            "Rizia, not for the RNC.",
            "Remember too the men who did not come home from Zille. Ask yourself who sent them, and vote accordingly."));
    }

    /// <summary>A Porte Drazon report once the campaign reaches its last week (vanilla's HouseElection_Turn05_Step05).</summary>
    private static void CampaignReport()
    {
        if (!IsDeclared || Turn != 5 || Step < 5 || Variables.GetBool(ReportCampaign))
        {
            return;
        }

        Variables.Set(ReportCampaign, true);
        string s = State();
        string title = By(s, "The War Comes to the Campaign", "Zille Dominates the Campaign", "Zille Haunts the Campaign");
        NewsWire.Report("SE_WarCampaign", SuzerainTokenName.RiziaCityPorteDrazon, title, P(
            By(s,
                "With the army fighting in Zille, the election campaign has become a contest over the war. RNC candidates now close " +
                "their rallies with the royal anthem and a minute of silence for the fallen, and in Valenqir one candidate took the " +
                "stage in his reserve officer's uniform.",
                "Victory in Zille has turned the last weeks of the campaign into a procession for the RNC. Candidates share the stage " +
                "with returning soldiers, and in Valenqir one of them took the stage in his reserve officer's uniform.",
                "The outcome in Zille hangs over the last weeks of the campaign. RNC candidates speak of honor and sacrifice and very " +
                "little of the war itself, while widows' black is a common sight in the front rows of every rally."),
            By(s,
                "The RPP has been more careful. Manus Sazon told a crowd in Iza that his party \"stands with every soldier in Zille, " +
                "and with every mother who wants him home,\" but RPP canvassers in Porte Drazon report doors being shut in their faces.",
                "The RPP has tried to turn the conversation to the bill. Manus Sazon told a crowd in Iza that \"victory has a price, and " +
                "the Palace means to send it to your door,\" to applause that was polite rather than warm.",
                "The RPP smells an opening. Manus Sazon told a crowd in Iza that \"a war declared without a single vote was lost without a " +
                "single vote,\" and RPP canvassers report a hearing in neighborhoods that would not open their doors a month ago."),
            "Officials of the electoral commission have asked both parties to keep the war out of the polling stations, with little " +
            "hope of success."));
    }
}
