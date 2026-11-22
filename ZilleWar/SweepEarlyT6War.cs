using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// The Turn 6 Pales war scenes, filling what EarlyT6 left out:
/// - the general staff briefing (c357): the Council Chambers have been a war room since Konstantium, Carlos has been
///   running the Zille war from it, the opt-out hands the south to the Staff but the north stays with the King, and the
///   closing line knows about Zille;
/// - the Duke's war call (c368): the closing line after a won or lost Zille war;
/// - the Aureus deal signing (c369): Reinhart's remark, Vina's question, Laurento's "one war is enough", the speech option;
/// - the balcony speech (c372): the Zille line becomes present (or past) tense;
/// - the war decisions of Turns 4 and 6 (production lines, alert drills, enlistments, the simulation) are reworded while
///   the Zille war colours them;
/// - the papers' Pales war headlines, the simulation, the speech reactions and the arms factories learn about Zille.
/// </summary>
internal static class SweepEarlyT6War
{
    private const string AskedNorthCommand = "ZilleWar.SE_War_Asked_NorthCommand";
    private const string ReportVolunteers = "ZilleWar.SE_War_Report_Volunteers";

    private const string Taddeus = "Taddeus Azaro";
    private const string Laurento = "Laurento Esquibel";
    private const string Vina = "Vina Toras";
    private const string Axel = "Axel Reinhart";

    public static void Register()
    {
        foreach (string v in new[] { AskedNorthCommand, ReportVolunteers })
        {
            Variables.Register(v);
        }

        GeneralStaff();
        DukeCall();
        DealSigning();
        Balcony();
        Decisions();
        Press();
        PalesIncidentPress.Register();
    }

    // ================================================================== c357 The general staff briefing

    private static void GeneralStaff()
    {
        const string conv = "Rizia/Turn 6/SnO_WarEruptionBriefing";
        var t = new Talk(conv, "ZilleWar.SE.War.Staff.");

        // "I had only ever seen members of the Royal Council in this room before." The room has been a war room since Konstantium.
        t.Variant("RoomActive", 99, new[] { 511 }, Active, Narrator,
            "Since Konstantium, the Council Chambers had belonged to the generals. Today there were twice as many of them. Every " +
            "single top-ranking general and admiral in Rizia was in attendance, and the map of Zille had been pushed to the far end " +
            "of the table to make room for Pales.", new[] { "#102" });
        t.Variant("RoomOver", 99, new[] { 511 }, Over, Narrator,
            "The Council Chambers had never quite gone back to being a council room after Zille. Today it was packed with military " +
            "staff again. Every single top-ranking general and admiral in Rizia was in attendance.", new[] { "#102" });
        t.Variant("RoomReclamation", 512, new[] { 510 }, Active, Narrator,
            "The last time I had seen the room this full was at the start of the Reclamation War. Since Konstantium it had rarely " +
            "been empty, but today even the map of Zille had been pushed aside for Pales. Every single top-ranking general and " +
            "admiral in Rizia was in attendance.", new[] { "#102" });

        // "...best known for his defense of Porte Drazon during the Uprising." He has run the Zille war from this room.
        t.Variant("CarlosActive", 541, new[] { 543 }, Active, Narrator,
            "Standing next to them was General Carlos Robles Azaro, best known for his defense of Porte Drazon during the Uprising. " +
            "Since Konstantium he had been directing the war in Zille from this very room.", new[] { "#542" });
        t.Variant("CarlosOver", 541, new[] { 543 }, Over, Narrator,
            "Standing next to them was General Carlos Robles Azaro, best known for his defense of Porte Drazon during the Uprising, " +
            "and lately for the campaign in Zille.", new[] { "#542" });

        // The King hands the invasion of Pales to the Staff ([273]). The north is another matter.
        t.Line("North1", Taddeus,
            "\"The south, then. And the north, Your Majesty? Carlos tells me you have been moving the tokens on the Zille map yourself.\"",
            new[] { "North2" }, condition: $"{Active} and {NotSet(AskedNorthCommand)}", script: Set(AskedNorthCommand, "true"),
            hooks: From(273));
        t.Choice("North2", "\"Zille stays with me, General.\"", new[] { "North3" });
        t.Line("North3", Taddeus,
            "\"As you wish.\" He inclined his head. \"The General Staff takes the south, and the King keeps the north. I have seen " +
            "worse arrangements.\"", new[] { "#264" });

        // "And there it was. The Invasion of Pales was about to begin."
        t.Variant("BeginActive", 469, new[] { 468 }, Active, Narrator,
            "And there it was. The Invasion of Pales was about to begin, and the war in Zille was not yet over.", new[] { "#576" });
        t.Variant("BeginWon", 469, new[] { 468 }, Liberated, Narrator,
            "And there it was. Zille was ours, and the Invasion of Pales was about to begin.", new[] { "#576" });
        t.Variant("BeginLost", 469, new[] { 468 }, AuditT6.Lost, Narrator,
            "And there it was. The Invasion of Pales was about to begin, before the country had finished mourning Zille.", new[] { "#576" });
        t.Variant("BeginPart", 469, new[] { 468 }, AuditT6.Part, Narrator,
            "And there it was. The Invasion of Pales was about to begin, with Zille city ours again and the rest of the province " +
            "still behind the armistice line.", new[] { "#576" });
        t.Register();
    }

    // ================================================================== c368 The Duke's war call

    private static void DukeCall()
    {
        const string conv = "Rizia/Turn 6/FPnT_DukeWarPhoneCall";
        var t = new Talk(conv, "ZilleWar.SE.War.Duke.");

        // EarlyT6 has the Active version of [78].
        t.Variant("BegunWon", 78, new[] { 88 }, Liberated, Narrator,
            "And just like that, the Second Pales War had begun. The guns in Zille had barely fallen silent.", new[] { "#90" });
        t.Variant("BegunLost", 78, new[] { 88 }, $"{AuditT6.Lost} and {WarFlags.LNot(WarFlags.Ceasefire)}", Narrator,
            "And just like that, the Second Pales War had begun. I had already lost one war this year. I did not intend to lose another.",
            new[] { "#90" });
        t.Variant("BegunCeasefire", 78, new[] { 88 }, $"{AuditT6.Lost} and {WarFlags.L(WarFlags.Ceasefire)}", Narrator,
            "And just like that, the Second Pales War had begun. I had already stopped one war this year short of its aim. I did not " +
            "intend to stop another.", new[] { "#90" });
        t.Variant("BegunPart", 78, new[] { 88 }, AuditT6.Part, Narrator,
            "And just like that, the Second Pales War had begun. The war in Zille had ended on an armistice line. This one, I meant to finish.",
            new[] { "#90" });
        t.Register();
    }

    // ================================================================== c369 The Aureus deal signing

    private static void DealSigning()
    {
        const string conv = "Rizia/Turn 6/SnO_PalesDealSigning";
        var t = new Talk(conv, "ZilleWar.SE.War.Signing.");

        // Reinhart, after his opening words on the "thaw between Pales and Rizia".
        string[] afterDuke = { "#55", "#54", "#113", "#56" };
        t.Line("DukeActive", Axel,
            "\"And between us, Your Majesty, it is a relief to sign something with Rizia that does not involve artillery. My generals " +
            "have been reading the dispatches from Zille with great interest.\"", afterDuke, condition: Active, hooks: From(51, 52));
        t.Line("DukeWon", Axel,
            "\"I confess my generals followed every dispatch from Zille, Your Majesty. Today, I am very glad to be signing papers " +
            "with you instead.\"", afterDuke, condition: Liberated, hooks: From(51, 52));
        t.Line("DukeLost", Axel,
            "\"I was sorry to read the news from Zille, Your Majesty. Truly. On a day like this, it is good to see that Rizia still " +
            "has friends in the south.\"", afterDuke, condition: AuditT6.Lost, hooks: From(51, 52));
        t.Line("DukePart", Axel,
            "\"I was sorry to read of the dead in Zille, Your Majesty. Truly. I hear Zille city flies your flag again. On a day like " +
            "this, it is good to see that Rizia still has friends in the south.\"", afterDuke, condition: AuditT6.Part, hooks: From(51, 52));

        // Vina has spent the war in Pales as liaison.
        t.Line("Vina", Vina,
            "\"Everyone here asks me about Zille, Father. The Duke's ministers, the journalists, even the man who polishes the " +
            "banisters. I tell them I know no more than they read in the papers. It's mostly true.\"",
            new[] { "#362", "#44", "#45", "#46" }, condition: Active, hooks: From(43));

        // "No time to dwell on that now. We are here to celebrate."
        const string oneWar = "\"No time to dwell on that now. We are here to celebrate. And frankly, Your Majesty, one war is quite " +
                              "enough for any kingdom.\"";
        t.Variant("Laurento174", 174, new[] { 171 }, Active, Laurento, oneWar, new[] { "#339" });
        t.Variant("Laurento181", 181, new[] { 178 }, Active, Laurento, oneWar, new[] { "#6" });

        // Speech option: "we live in a volatile region... Let Rizia and Pales serve as an example."
        t.Variant("Volatile", 270, new[] { 268 }, Active, Romus,
            "\"I know how Rizia is spoken of in Konstantium these days. " +
            "But let Rizia and Pales show the region that where a fair bargain can be struck, it need not be struck with guns.\"",
            new[] { "#273" });
        t.Register();
    }

    // ================================================================== c372 The balcony speech

    private static void Balcony()
    {
        const string conv = "Rizia/Turn 6/A_PalaceBalconySpeech";
        var t = new Talk(conv, "ZilleWar.SE.War.Balcony.");

        // "They tried to attack me in Zille but failed. Now, a new threat rises from the south."
        t.Variant("ZilleActive", 27, new[] { 160 }, Active, Romus,
            "\"They tried to kill me in Zille and failed. Now our soldiers are in Zille, and a new threat rises from the south. We will " +
            "fight our enemies on every front, and we will win.\"", new[] { "#453" });
        t.Variant("ZilleWon", 27, new[] { 160 }, Liberated, Romus,
            "\"They tried to kill me in Zille and failed. Today, Zille is ours again. Now a new threat rises from the south, and we " +
            "will meet it the same way.\"", new[] { "#453" });
        t.Register();
    }

    // ================================================================== decisions

    private static void Decisions()
    {
        foreach (string name in new[] { "Turn04_Decision_War_UpgradeProductionLinesOne", "Turn06_Decision_War_UpgradeProductionLinesTwo" })
        {
            Decision(name, _ => !Variables.GetBool(WarFlags.TwoWars) ? null :
                "The kingdom's new military factories are running day and night, and it is not enough. The army is fighting in Zille " +
                "and on the Palesian border at once, and both fronts are using up shells, vehicles and spare parts faster than the " +
                "depots can replace them. The General Staff wants at least one production line expanded without delay. Every upgrade " +
                "will also draw more power from a grid that is already strained.");
            Decision(name, _ => !IsActive ? null :
                "The kingdom's new military factories have been running day and night since the declaration of war in Konstantium. " +
                "The army in the north is using up shells, vehicles and spare parts faster than the depots can replace them, and the " +
                "General Staff wants at least one production line expanded before the next offensive. Every upgrade will also draw " +
                "more power from a grid that is already strained.");
            Decision(name, _ => !IsWon ? null :
                "The war in Zille is won, but it emptied the army's depots. The kingdom's modern military factories offer a solid " +
                "foundation to replace what was lost and scale up production of Land and Air Units, either to keep the army ready " +
                "for the next conflict or to sell what Rizia has shown it can build on the international arms market.");
            Decision(name, _ => !AuditT6.CityHeldNow || WarFlow.AtWar ? null :
                "The war in Zille ended in an armistice, with Zille city won back and the rest of the province still in Wehzek hands, and " +
                "the army must replace the tanks, aircraft and equipment it spent in the north. The kingdom's modern military factories " +
                "offer a solid foundation to rebuild, and the General Staff wants the armistice line held by an army that is never short " +
                "of anything.");
            Decision(name, _ =>
                "The war in Zille ended without the victory the kingdom was promised, and the army must replace the tanks, aircraft " +
                "and equipment it left behind in the north. The kingdom's modern military factories offer a solid foundation to " +
                "rebuild, and the General Staff is adamant that Rizia never again goes to war short of anything.");
        }

        Decision("Turn06_Decision_War_Both_AlertDrills", _ => !IsActive ? null :
            "With the kingdom fighting Pales in the south and Wehlen in Zille, our fighter squadrons are stretched between the " +
            "Palesian border and the skies over the northern front. Implementing frequent quick-reaction alert drills could ensure " +
            "that pilots and aircraft are always prepared to scramble at a moment's notice, increasing potential air support " +
            "frequency. However, the constant state of alert will strain crews who have already been flying over Zille for weeks.");

        Decision("Turn06_Decision_WarEnlistments", _ => !IsActive ? null :
            "In the wake of the war declaration, citizens have responded in varying degrees to the call to arms. For many families it " +
            "is the second call this season: the northern army is still fighting in Zille, and recruitment officers report that some " +
            "volunteers ask to be sent north rather than south. The official war recruitment report reflects the nation's commitment " +
            "to the war effort and guides the kingdom's manpower pool for the Palesian campaign.");
        Decision("Turn06_Decision_WarEnlistments", _ => !IsWon ? null :
            "In the wake of the war declaration, citizens have responded in varying degrees to the call to arms. Coming so soon after " +
            "the victory in Zille, the call has found a confident nation, and veterans of the northern campaign are among the first in " +
            "line to enlist again. The official war recruitment report reflects the nation's commitment to the war effort and guides " +
            "the kingdom's manpower pool for the start of the conflict.");
        Decision("Turn06_Decision_WarEnlistments", _ => !AuditT6.CityHeldNow || WarFlow.AtWar ? null :
            "In the wake of the war declaration, citizens have responded in varying degrees to the call to arms. Coming so soon after " +
            "the armistice in Zille, the call has met a tired but steady nation: veterans who took Zille city are among the first in line, " +
            "while families with sons still on the armistice line are slower to send more. The official war recruitment report reflects " +
            "the nation's commitment to the war effort and guides the kingdom's manpower pool for the start of the conflict.");
        Decision("Turn06_Decision_WarEnlistments", _ =>
            "In the wake of the war declaration, citizens have responded in varying degrees to the call to arms. Coming so soon after " +
            "the war in Zille ended without victory, the call has met a wary nation, and many families who sent sons north are slow to " +
            "send them south. The official war recruitment report reflects the nation's commitment to the war effort and guides the " +
            "kingdom's manpower pool for the start of the conflict.");

        Decision("Turn06_Decision_OperationTutorial", _ =>
            "General Azaro has requested the participation of the King in a strategic simulation for the upcoming invasion of Pales. " +
            "The King has already directed operations in Zille, but the General insists that Pales is a different war: a fortified " +
            "river line, mountain passes in the west, and an enemy that has spent years preparing for us.");
    }

    // ================================================================== press

    private static void Press()
    {
        Func<string, bool> active = s => s == StActive;

        News("Turn06_RH_PalesWarStart", (s, _) => s == StActive
                ? P("As the first reports of fighting on the Palesian border started coming in last night, one thing has become clear. " +
                    "Rizia and Pales are at war once again, even as our brave soldiers in the north press on to bring Zille home.",
                    "While the Palace hasn't commented yet on these early reports, King Romus has announced he will give a speech later " +
                    "today. The Royal Herald declares its commitment to our troops on both fronts and prays for their victory, from the " +
                    "harbor of Zille to the banks of the Alca.",
                    "Glovurius Axa Rizia!")
                : P("As the first reports of fighting on the Palesian border started coming in last night, one thing has become clear. " +
                    "Rizia and Pales are at war once again! " + AuditT6.ByP(s, "",
                        "The same army that brought Zille home this year now marches south.",
                        "The same army that brought Zille city home this year now marches south.",
                        "The same army that gave its all in Zille this year now marches south, determined to finish what it starts."),
                    "While the Palace hasn't commented yet on these early reports, King Romus has announced he will give a speech later " +
                    "today. For now, the Royal Herald declares its commitment to our brave troops and prays for their victory on the battlefield.",
                    "Glovurius Axa Rizia!"),
            s => s == StActive ? "Glory To The Troops! Rizia Now Fights On Two Fronts" : null);

        News("Turn06_V_PalesWarStart", (s, o) => s != StActive
                ? P(o, AuditT6.ByP(s, "",
                    "The King's war in Zille is not yet a season old, and already he has found another. The families who buried their " +
                    "sons for Zille will now watch the trains leave for the south.",
                    "The guns in Zille have barely fallen silent, half of the province is still behind Wehzek lines, and already the King has " +
                    "found another war. The families who buried their sons for Zille city will now watch the trains leave for the south.",
                    "The King lost one war this year and has decided to start another. The families who buried their sons for Zille, for " +
                    "nothing, will now watch the trains leave for the south."))
                : P(
                "The unthinkable has happened twice in a single season. With Rizian soldiers still dying in Zille, the King has opened " +
                "a second war, this time against Pales.",
                "Palesian civilians living in the border areas woke up terrified as artillery shelling commenced in the middle of the " +
                "night, with border posts being targeted first. Reports of the first Rizian troops entering Palesian territory have " +
                "surfaced, in what is starting to look like a full scale invasion.",
                "We at the Voice of Rizia condemn this barbaric act. Families in Brenas who have not yet buried their sons from the " +
                "northern front will now watch the trains leave for the south. Our thoughts are with our brother population in Pales, " +
                "and with everyone forced to serve in either of the King's wars."),
            s => s == StActive ? "Not One War, But Two" : null);

        News("Turn06_G_PalesWarStart", (s, o) => s != StActive
                ? P(o, AuditT6.ByP(s, "",
                    "The invasion comes only weeks after Rizia took Zille from Wehlen by force. \"Konstantium had hoped one war would " +
                    "satisfy the King,\" one diplomat told Geopolitico. \"It seems to have given him a taste for it.\"",
                    "The invasion comes only weeks after Rizia's war with Wehlen over Zille ended in an armistice that left Zille city " +
                    "in Rizian hands and the rest of the province with Wehlen. In Wraclawitz, President Wiktor Smolak's office said only " +
                    "that Wehlen \"takes note\" of where the Rizian army is going.",
                    "The invasion comes only weeks after Rizia's war with Wehlen over Zille ended without the Kingdom taking the city. " +
                    "\"A king who could not win in the north is looking for a victory in the south,\" one diplomat in Konstantium said."))
                : P(
                "Last night, first reports started emerging of Palesian border posts being shelled by Rizian troops, as well as " +
                "violations of Palesian airspace and the first tank columns entering border areas. Everything indicates a full scale " +
                "invasion, launched while the Rizian army is still at war with Wehlen over the Zille region.",
                "King Romus is expected to formally declare the war in a speech scheduled for this afternoon. CSP and ATO " +
                "representatives have already condemned the aggression and called for an immediate retreat of Rizian troops. In " +
                "Wraclawitz, the office of President Wiktor Smolak said only that Wehlen \"takes note\" of Rizia's decision to divide " +
                "its army."),
            s => s == StActive ? "Rizia Opens Second Front, Invades Pales" : null);

        News("Turn06_V_Decision_OperationTutorial", (_, _) => P(
                "Military exercises held by the Rizian armed forces, coordinated by King Romus himself with the help of General Taddeus " +
                "Azaro, have led to an embarrassing situation as the King threw wave after wave of troops into well fortified positions.",
                "We had hoped that one real war in Zille would be enough to cure the King of his appetite for them. Clearly, it was not."),
            _ => "The King Plays at War, Again");

        foreach (string name in new[] { "Turn06_G_WarSpeech_CautiousPublic", "Turn06_G_WarSpeech_GalvanizedPublic", "Turn06_G_WarSpeech_UnitedPublic" })
        {
            AppendNews(name, s => s == StActive
                ? "Abroad, the speech was read alongside the news from Zille, where the same army is still at war with Wehlen. \"Two wars " +
                  "and one treasury,\" a diplomat in Konstantium remarked. \"The applause will be the cheapest part.\""
                : "Abroad, the speech was read as the second declaration of war from Porte Drazon in a single year, weeks after the guns " +
                  "fell silent in Zille. \"The applause will be the cheapest part,\" a diplomat in Konstantium remarked.");
        }

        AppendReport("Turn06_FolceDator_MilitaryIndustryOutputIncrease", _ =>
            "Much of the first output, factory managers say, is already promised to the northern army in Zille.", active);
        AppendReport("Turn06_Mermi_TroopMovements", _ =>
            "Officers note that the columns are thinner than planned. The best-equipped divisions remain committed in Zille.", active);
    }

    public static void OnStep()
    {
        bool enlisted = Variables.GetBool("RiziaDLC.Decision_Turn06_WarEnlistments_Massive") ||
                        Variables.GetBool("RiziaDLC.Decision_Turn06_WarEnlistments_Moderate") ||
                        Variables.GetBool("RiziaDLC.Decision_Turn06_WarEnlistments_Low");
        if (!enlisted || !IsActive || Turn > 7 || !Once(ReportVolunteers))
        {
            return;
        }

        NewsWire.Report("SE6_Volunteers", "Rizia_City_MaveNortus", "Mave Nortus Volunteers Ask to Be Sent North", P(
            "Recruitment offices in Mave Nortus have been overwhelmed since the King's balcony speech, though not quite in the way the " +
            "General Staff intended. Many of the young men queuing outside asked to join the northern army in Zille rather than the " +
            "divisions bound for Pales, and several recruiting officers admitted they had stopped asking.",
            "\"My brother is outside Zille,\" one volunteer told reporters. \"I am not going to the Alca while he is still up there.\""));
    }
}
