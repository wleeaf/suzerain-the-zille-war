using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Vanilla Turn 5 ("Meditations") during the Zille war:
/// - Smolak's state visit to sign a trade pact (Turn05_FPnT_WehlenNegotiations) cannot happen. It is gated off; its
///   bookkeeping that has nothing to do with Wehlen is replayed, and its slot is taken by a council in Palas Rezna on the
///   morning after Konstantium (the AN fallout, the money, the House, Vina's question, Lucita's chess queen).
/// - The MITZ board, the Hegel call, the Morella talks and their debrief, the late-night security council, the royal
///   yacht and the flight home all acknowledge the war instead of a "preliminary agreement with Wehlen".
/// </summary>
internal static class EarlyT5
{
    private const string Mitz = "Rizia/Turn 5/FPnT_MITZBoardMeeting";
    private const string HegelCall = "Rizia/Turn 5/FPnT_MITZHegelCall";
    private const string Morella = "Rizia/Turn 5/FPnT_MorellaNegotiations";
    private const string Feedback = "Rizia/Turn 5/EnT_MorellaNegotiationFeedback";
    private const string Security = "Rizia/Turn 5/SnO_InternalSecurity";
    private const string Yacht = "Rizia/Turn 5/P_ManusVinaTalk";
    private const string Flight = "Rizia/Turn 5/FPnT_MorellaReturnFlight";
    private const string CouncilConv = "ZilleWar/Early/CouncilAfterKonstantium";

    private const string Esquibel = "Laurento Esquibel";
    private const string Elena = "Elena Werner";
    private const string Hugo = "Hugo Toras";
    private const string Vina = "Vina Toras";
    private const string Lucita = "Lucita Azaro";
    private const string Sal = "Sal Ignacius";
    private const string Saltana = "Alma Saltana";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Manus = "Manus Sazon";

    /// <summary>The King swore MITZ support to Alvarez at T4 and then voted for nationalization (vanilla c321 [388]).</summary>
    public const string VarLespiaBetrayed = "ZilleWar.E5_LespiaBetrayed";
    /// <summary>"corridor" | "screen" | "closed": how the King answered the boats crossing the Gulf of Moridia.</summary>
    public const string VarRefugeePolicy = "ZilleWar.E5_RefugeePolicy";
    /// <summary>The King issued war bonds at the T5 council (+2 Budget). A liability if the war is lost.</summary>
    public const string VarWarBonds = "ZilleWar.E5_WarBonds";
    /// <summary>What the King told Vina about the people of Zille: "rizian" | "protect" | "smolak".</summary>
    public const string VarVinaAnswer = "ZilleWar.E5_VinaAnswer";

    private const string VarBookkept = "ZilleWar.E5_WehlenTalksBookkept";
    private const string VarCouncilOffered = "ZilleWar.E5_CouncilOffered";
    private const string VarQueenGiven = "ZilleWar.E5_QueenGiven";
    private const string VarWatchtower = "ZilleWar.E5_WatchtowerPublished";
    private const string VarSaltanaReport = "ZilleWar.E5_SaltanaReportFiled";
    private const string CouncilFragment = "ZilleWar.Pre_EarlyCouncil_T5";

    private static readonly string War = WarFlags.LDeclared;
    private static readonly string Active = WarFlags.LActive;

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarLespiaBetrayed, VarRefugeePolicy, VarWarBonds, VarVinaAnswer, VarBookkept, VarCouncilOffered, VarQueenGiven, VarWatchtower,
                     VarSaltanaReport,
                 })
        {
            Variables.Register(v);
        }

        // Smolak's visit to Porte Drazon to sign a trade, migration and counter-terror pact.
        Vanilla.Gate("Turn05_FPnT_WehlenNegotiations", WarFlags.XPeace);
        // Saltana's communique "urging the speedy return of Zille" (c327 [1040]): replaced by a wartime one.
        Vanilla.HideReport("Turn05_Morella_DemandsZilleReturn", () => Variables.GetBool(WarFlags.Declared));
        // The flag that starts the wartime communique is set only on vanilla's ATA branch ([1040]); the Su Omina-only supply branch
        // ([1208]) must raise it too in a war game.
        Vanilla.AppendScript(Feedback, 1208, $"if {War} then Variable[\"RiziaDLCSupport.Reports_Turn05_Morella_DemandsZilleReturn\"] = true end;");

        ConversationRegistry.RegisterConversation(CouncilConv);
        RegisterCouncil();
        RegisterMitz();
        RegisterHegel();
        RegisterMorella();
        RegisterFeedback();
        RegisterSecurity();
        RegisterYacht();
        RegisterFlight();
    }

    // ------------------------------------------------------------------ the council after Konstantium (replaces c326's slot)

    private static void RegisterCouncil()
    {
        var t = new Talk(CouncilConv, "ZilleWar.E5.Council.");
        string rumburgGas = "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
                            "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true";
        string alvarezBacks = $"{V(AnDeclaration.VarAlvarezStance)} == \"backs\"";

        t.Logic("Start", new[] { "Room" }, hooks: AtStart(CouncilConv),
            script: "SetRichPresence(\"Holding a war council\",\"Palace Rezna\",\"Porte Drazon\"); " +
                    "PlayLoopedMusic(\"R_GridlockLight\",\"R_GridlockLight_Loop\");");
        t.Line("Room", Narrator,
            "For the first time since my coronation, the Royal Council met under a map of Zille. Someone had pinned it over the " +
            "fireplace and drawn the front line on it in red pencil.", new[] { "RoomWon", "RoomLost", "Room2" });
        // The first phase of the offensive has been fought since Konstantium: the red line says how it went.
        t.Line("RoomWon", Narrator,
            "Since the battle, the red line ran north of Zille city. The harbor was ours, and most of the faces around the table looked " +
            "as if they had finally slept.", new[] { "Room2" }, condition: AuditT45.City);
        t.Line("RoomLost", Narrator,
            "The red line still ran south of Zille city. Our first attack had not taken it, and nobody in the room needed reminding.",
            new[] { "Room2" }, condition: $"not ({AuditT45.City}) and (Variable[\"{ZilleCampaign.VarOpsDone}\"] or 0) >= 1");
        t.Line("Room2", Narrator,
            "Laurento looked as if he had not slept since Konstantium. Perhaps he hadn't.", new[] { "AN1" });

        // the Alliance
        t.Line("AN1", Esquibel,
            "\"The Security Committee sat until three in the morning after Konstantium, Your Majesty. It could not agree on Smolak's " +
            "protocol, and it has not agreed on our declaration since.\"", new[] { "AN2Backs", "AN2" }, conversant: true);
        t.Line("AN2Backs", Esquibel,
            "\"Lespia blocked a censure. Prime Minister Alvarez reminded the room that he had already called Wehlen's presence in Zille " +
            "an illegal occupation. Valgsland wanted us condemned. Rumburg and Derdia stood by us.\"", new[] { "AN3" }, condition: alvarezBacks);
        t.Line("AN2", Esquibel,
            "\"Lespia abstained, which with Mr. Alvarez means he is waiting for an offer. Valgsland wanted us condemned. Rumburg and " +
            "Derdia stood by us.\"", new[] { "AN3" }, condition: $"not ({alvarezBacks})");
        t.Line("AN3", Esquibel,
            "\"So there will be no resolution. For now.\"", new[] { "AN_R1", "AN_R2", "AN_R3" });
        t.Choice("AN_R1", "\"For now is all I need.\"", new[] { "AN_R1a" });
        t.Line("AN_R1a", Esquibel,
            "\"Then I suggest we use it well. The Alliance has a long memory and a short temper, Your Majesty.\"", new[] { "Money1" });
        t.Choice("AN_R2", "\"And United Contana?\"", new[] { "AN_R2a" });
        t.Line("AN_R2a", Esquibel,
            "\"Silent, which worries me more than any speech. Their ambassador spent the night at the Wehzek embassy.\"", new[] { "Money1" });
        t.Choice("AN_R3", "\"You disapprove, Laurento. Say it.\"", new[] { "AN_R3w", "AN_R3a" });
        // the King's confession in the Turn 4 drinking game ("a big, bloody war, just so I can prove myself")
        t.Line("AN_R3w", Esquibel,
            "\"At the winery you told us you wished for a big, bloody war. Between the second bottle and the third. " +
            "I decided it was the wine talking.\" He straightened his cuffs. \"Forgive me.\"",
            new[] { "AN_R3a" }, condition: WarFlags.L(EarlyT4.VarConfessedWar));
        t.Line("AN_R3a", Esquibel,
            "\"I am a son of Brenas, Your Majesty. The Esquibels have estates in Zille, and so do the Sazons. I have wanted that land " +
            "back for as long as you have. I only wish we were taking it back with a pen.\"", new[] { "Money1" });

        // the money
        t.Line("Money1", Elena,
            "\"We were going to sell Wehlen our gold and buy its oil. Now we are paying for a war and buying our fuel from whoever " +
            "will sell it to us.\"", new[] { "Money2Gas", "Money3" }, conversant: true);
        t.Line("Money2Gas", Elena,
            "\"And our gas still goes north to Thornbourgh every day under the Rumburg contract, war or no war.\"", new[] { "Money3" },
            condition: rumburgGas);
        t.Line("Money3", Elena,
            "\"I can carry the cost for a season, Your Majesty. Not for a year.\"", new[] { "M_R1", "M_R2", "M_R3" });
        t.Choice("M_R1", "\"Issue war bonds. Let the people who cheered in the streets buy a share of the victory.\"", new[] { "M_R1a" },
            script: Set(VarWarBonds, "true") + "Variable[\"RiziaDLC.Resources_Budget\"] = (Variable[\"RiziaDLC.Resources_Budget\"] or 0) + 2;",
            cost: "+2 Budget");
        t.Line("M_R1a", Elena,
            "\"Six billion guilders, if they sell as well as the newspapers. Patriotism is the cheapest money there is, until the war goes badly. Then it becomes the most expensive.\"",
            new[] { "House1" });
        t.Choice("M_R2", "\"Then find the money, Ms. Werner. That is your job.\"", new[] { "M_R2a" });
        t.Line("M_R2a", Elena,
            "\"It is. I'll find it. I would simply like it on record that I warned you.\"", new[] { "House1" });
        t.Choice("M_R3", "\"It won't last a year. Zille will be ours long before that.\"", new[] { "M_R3a" });
        t.Line("M_R3a", Elena,
            "\"Every minister of war in history has said that, Your Majesty. I'll budget for the year anyway.\"", new[] { "House1" });

        // the House
        t.Line("House1", Hugo,
            "\"The House of Delegates is in uproar. Baroness de Rava's people cheered for ten minutes when the first guns sounded in Zille.\"",
            new[] { "HouseCity", "House2" }, conversant: true);
        t.Line("HouseCity", Hugo, "\"When word came that Zille city had fallen, they cheered all over again.\"", new[] { "House2" },
            condition: AuditT45.City);
        t.Line("House2", Hugo,
            "\"The Brenas members did not. Half of them have cousins on the other side of the Zille line, and the other half have " +
            "sons in the reserve.\"", new[] { "Sal1" });
        t.Line("Sal1", Sal,
            "\"If I may, Your Majesty. Wehlen's radio is already calling this a war of Wruhecists against Dastnurists. I would ask " +
            "that we never give them a reason to be right.\"", new[] { "Sal2" }, conversant: true);
        t.Line("Sal2", Sal,
            "\"There are Dastnurist families in Iza and Sallabes who have been loyal Rizians all their lives. They are frightened this " +
            "week. They should not be.\"", new[] { "Sal_R1", "Sal_R2" });
        t.Choice("Sal_R1", "\"They are my subjects, Grand Wiseman, and God has put them in my care. Anyone who touches them answers to me.\"", new[] { "Sal_R1a" },
            script: "Variable[\"RiziaDLC.PersonalValue_Religious\"] = (Variable[\"RiziaDLC.PersonalValue_Religious\"] or 0) + 1;");
        t.Line("Sal_R1a", Sal, "\"Halaita. Thank you, Your Majesty.\"", new[] { "Vina1" });
        t.Choice("Sal_R2", "\"This is a war about land, not faith. Smolak can say what he likes.\"", new[] { "Sal_R2a" });
        t.Line("Sal_R2a", Sal, "\"Men rarely die for land alone, Your Majesty. Someone always tells them God wants it too.\"", new[] { "Vina1" });

        // Vina's question
        t.Line("Vina1", Vina,
            "\"What of the people living in Zille? Has anyone asked them?\"", new[] { "V_R1", "V_R2", "V_R3" }, conversant: true);
        t.Choice("V_R1", "\"They will be Rizian again, Vina. That is the answer.\"", new[] { "Vina2R" }, script: Set(VarVinaAnswer, "\"rizian\""));
        t.Choice("V_R2", "\"We will protect them. Rizians and Wehzeks alike.\"", new[] { "Vina2P" },
            script: Set(VarVinaAnswer, "\"protect\"") + "Variable[\"RiziaDLC.PersonalValue_Pacifist\"] = (Variable[\"RiziaDLC.PersonalValue_Pacifist\"] or 0) + 1;");
        t.Choice("V_R3", "\"Wehlen had twenty-five years to ask them. It never did.\"", new[] { "Vina2S" }, script: Set(VarVinaAnswer, "\"smolak\""));
        // the shelling leaves them no other way to be asked: they answer from their cellars
        t.Line("Vina2R", Vina,
            "\"Some of them will not want to be Rizian, Father. They can hear the guns from their cellars. Remember that when they answer.\"",
            new[] { "Adjourn" });
        t.Line("Vina2P", Vina,
            "\"Then say that to them, Father. They can hear the guns from their cellars, and they are listening for it.\"", new[] { "Adjourn" });
        t.Line("Vina2S", Vina,
            "\"Then do not let Wehlen's silence become ours, Father. They can hear the guns from their cellars.\"", new[] { "Adjourn" });

        // Lucita stays behind: the chess queen (vanilla c326 [343]-[378], EnableCollectionItem at [1249])
        t.Line("Adjourn", Narrator,
            "The council filed out one by one. Lucita took her time collecting her papers, and when the door closed behind Hugo, she " +
            "was still there.", new[] { "Q1" }, conversant: false, script: $"AddConversant(\"{Lucita}\");");
        t.Line("Q1", Lucita,
            "\"A moment, Your Majesty. Before the General Staff takes the rest of your day.\"", new[] { "Q2" });
        t.Line("Q2", Narrator,
            "She held out her hand. In it was a chess piece, a queen, made of marble with golden details. A pair of crossed swords was " +
            "carved into its base. The Azaro sigil.", new[] { "Q3" }, script: "EnableCollectionItem(\"CollectionItem_ChessPiece\");");
        t.Line("Q3", Lucita,
            "\"My uncle's. The mutineers ransacked his country house after they killed him. They took the set, but this one must have " +
            "fallen out in the chaos.\"", new[] { "Q_R1", "Q_R2", "Q_R3" });
        t.Choice("Q_R1", "\"A queen. The most valuable piece on the chessboard.\"", new[] { "Q_R1a" });
        t.Line("Q_R1a", Lucita,
            "\"She's a versatile attacker and an invaluable defender. All in the service of protecting her king.\"", new[] { "Q4" });
        t.Choice("Q_R2", "\"I can't take this. It must mean a great deal to you.\"", new[] { "Q_R2a" });
        t.Line("Q_R2a", Lucita, "\"It does. That's why I'm giving it to you.\"", new[] { "Q4" });
        t.Choice("Q_R3", "\"Twenty-five years ago, Wehlen's soldiers hunted those mutineers for my father. Now we are at war with them.\"",
            new[] { "Q_R3a" });
        t.Line("Q_R3a", Lucita,
            "\"Smolak's predecessor did us a favor, and Wehlen has been sending us the bill ever since. Now we have stopped paying it.\"",
            new[] { "Q4" });
        t.Line("Q4", Lucita,
            "\"Keep it as a reminder of what we have to lose. And what we've already lost.\"", new[] { "Q5" });
        t.Line("Q5", Lucita,
            "\"Blood for blood. And I'm afraid there's more yet to be spilled.\"", new[] { "Q6" });
        t.Line("Q6", Narrator,
            "I slipped the queen into my pocket. When I looked up, she was already at the door, her papers under her arm.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ the MITZ board (c321)

    private static void RegisterMitz()
    {
        var t = new Talk(Mitz, "ZilleWar.E5.Mitz.");

        // [576] "I was sorry to hear you were unable to reach an agreement with Wehlen..."
        t.Variant("Saltana", 576, new[] { 575 }, War, Saltana,
            "\"I won't pretend I'm sorry to see someone stand up to Wiktor Smolak. But you came to the Meftiem straight from a war. " +
            "Forgive my ministers if they count the exits.\"", new[] { "S_R1", "S_R2", "S_R3" });
        t.Choice("S_R1", "\"Zille is Rizian land. I am only taking back what was ours.\"", new[] { "S_R1a" });
        t.Line("S_R1a", Saltana,
            "\"Every army that ever crossed a border in this region said the same. Shall we talk about the gold instead?\"", new[] { "#49" });
        t.Choice("S_R2", "\"Let's keep Wehlen out of today's meeting.\"", new[] { "S_R2a" });
        t.Line("S_R2a", Saltana,
            "\"Gladly. It's the one thing in this building nobody is asking me to nationalize.\"", new[] { "#49" });
        t.Choice("S_R3", "\"Smolak chose this when he refused to return Zille.\"", new[] { "S_R3a" });
        t.Line("S_R3a", Saltana,
            "\"Perhaps he did. I would still have preferred a solution that didn't involve bloodshed. Our region has had enough of it.\"",
            new[] { "#49" });

        // "...not since my talk with President Smolak" / "My recent talk with President Smolak..."
        t.Variant("NoTalk1", 555, new[] { 31 }, War, Romus,
            "\"No. Not while we are still paying for this war. We cannot afford to lose any more of our gold.\"", new[] { "#566" });
        t.Variant("NoTalk2", 562, new[] { 27 }, War, Romus,
            "\"An understatement, Ms. Werner. With a war still to pay for, I have no gold to spare.\"", new[] { "#569" });

        // Elena on gold demand: the war is why the royalties matter.
        t.Variant("Gold", 255, new[] { 254 }, Active, Elena,
            "\"At the same time, Rumburg's demand for our gold is as high as ever, and the war is draining the treasury. The Meftiem " +
            "royalties are one income we cannot afford to lose.\"", new[] { "#257" });

        // Betraying the MITZ promise to Alvarez (vanilla also cancels Diplomacy_Lespia_WehlenHelp here).
        Vanilla.AppendScript(Mitz, 388, $"if {War} then " + Set(VarLespiaBetrayed, "true") + " end;");

        // Alvarez's parting jab, if his navy is holding the strait for us.
        t.Line("AlvarezSea", Alvarez,
            "\"And don't forget whose ships are keeping Wehzek tankers out of your war, Your Majesty. Friends in high places. Friends at " +
            "sea, too.\"", new[] { "#853" }, condition: $"{Active} and Variable[\"{SupplyWar.VarLespiaStrait}\"] == true and Variable[\"ZilleWar.Lespia_StraitThin\"] ~= true", hooks: From(852));
        t.Register();
    }

    // ------------------------------------------------------------------ Chancellor Hegel's call (c322)

    private static void RegisterHegel()
    {
        var t = new Talk(HegelCall, "ZilleWar.E5.Hegel.");

        // "I don't know. Go to war?" -> "Rizia is not in any position to start a war with anyone."
        t.Variant("AnotherWar", 464, new[] { 440 }, War, Romus, "\"I don't know. Start another war?\"", new[] { "AnotherWar2", "AnotherWar3" });
        t.Line("AnotherWar2", Hegel,
            "\"Another one? You have your hands full in Zille already, Mr. Toras. I would rather the Meftiem did not become your " +
            "second front.\"", new[] { "#459" }, condition: Active);
        t.Line("AnotherWar3", Hegel,
            "\"After Zille, I would not put it past you. Which is exactly why I called.\"", new[] { "#459" }, condition: $"not ({Active})");

        // "As a member of GRACE, Rizia is obliged to stay uninvolved."
        const string retort = "\"Uninvolved? Your army is at war in Zille as we speak, Mr. Toras. Spare me the GRACE catechism.\"";
        t.Variant("Uninvolved1", 230, new[] { 222 }, Active, Hegel, retort, new[] { "#233" });
        t.Variant("Uninvolved2", 436, new[] { 432 }, Active, Hegel, retort, new[] { "#438" });

        // In a war game Valgsland sells Rizia nothing: no naval upgrade (vanilla Military_Valgsland_AcceptNavalAid, which would
        // bring Valgslandian submarines after Cavette). Hegel still wants Lespia out of the Meftiem, and his price is a threat:
        // deliver, and Valgsland keeps its name off the sanctions motion at the Alliance (LateT8 reads LuaHegelServed).
        const string embargo =
            "\"Valgsland will not sell so much as a rivet to a kingdom making war on a friend of Contana, Mr. Toras. Our embargo " +
            "stands until your soldiers come home.\"";
        const string embargoStopped =
            "\"Valgsland will not sell so much as a rivet to a kingdom that made war on a friend of Contana, Mr. Toras. Our embargo " +
            "stands until the line is settled.\"";
        const string stick =
            "\"But a war can be made cheaper or dearer. Make sure Lespia walks away from the Meftiem with nothing, and Valgsland " +
            "will keep its name off the motion against you at the Alliance. Refuse, and I will draft it myself.\"";
        void Stick(string k, string intro, string introStopped, string introNoEmbargo, int vanilla, int[] parents, string ask)
        {
            t.Variant(k, vanilla, parents, $"{War} and {SupplyWar.EmbargoOn} and {Active}", Hegel, intro, new[] { k + "S" });
            t.Variant(k + "Q", vanilla, parents, $"{War} and {SupplyWar.EmbargoOn} and not ({Active})", Hegel, introStopped, new[] { k + "S" });
            t.Variant(k + "N", vanilla, parents, $"{War} and not {SupplyWar.EmbargoOn}", Hegel, introNoEmbargo, new[] { k + "S" });
            t.Line(k + "S", Hegel, stick, new[] { k + "1", k + "2", k + "3" });
            t.Choice(k + "1", "\"You are threatening a king in the middle of the night, Mr. Hegel?\"", new[] { k + "1a" });
            t.Line(k + "1a", Hegel, "\"I am describing the weather, Mr. Toras. You chose the season when your army crossed the border.\"",
                new[] { ask });
            t.Choice(k + "2", "\"Lespian ships are keeping Wehzek convoys out of Zille. You ask me to knife my only friend at sea.\"",
                new[] { k + "2a" }, condition: $"Variable[\"{SupplyWar.VarLespiaStrait}\"] == true");
            t.Line(k + "2a", Hegel,
                "\"A friend who bills you by the hour. Mr. Alvarez would sell your strait to Wraclawitz the day the price was right, " +
                "and you know it.\"", new[] { ask });
            t.Choice(k + "3", "\"And if Lespia gets nothing, Valgsland simply looks away from Zille?\"", new[] { k + "3a" });
            t.Line(k + "3a", Hegel,
                "\"Valgsland never looks away, Mr. Toras. It decides what is worth a vote. Lespia's share of the Meftiem is worth " +
                "one. Your war is not.\"", new[] { ask });
        }

        Stick("StickA", embargo, embargoStopped,
            "\"Valgsland does not reward a kingdom that makes war on a friend of Contana, Mr. Toras. Do not mistake this call for " +
            "friendship.\"", 282, new[] { 276, 277, 278, 279, 281 }, "#412");
        Stick("StickB",
            "\"Valgsland cannot sell anything to a kingdom at war with a friend of Contana, Mr. Toras. Our embargo stands until your soldiers come home.\"",
            "\"Valgsland cannot sell anything to a kingdom that made war on a friend of Contana, Mr. Toras. Our embargo stands until the line is settled.\"",
            "\"Valgsland does not reward a kingdom at war with a friend of Contana, Mr. Toras.\"", 517, new[] { 515, 587 }, "#550");

        // Agreeing: the promise without the aid (vanilla 386 sets PromiseITZTakeover and its journal entry).
        string promise = Set("RiziaDLC.Diplomacy_Valgsland_PromiseITZTakeover", "true") +
                         Set("RiziaDLCSupport.Journal_Turn05_Valgsland_PromiseLespiaOutofMITZ", "true");
        t.Variant("Yes1", 334, new[] { 315, 326, 412, 413, 414 }, War, Romus,
            "\"Yes. Lespia will walk away from the Meftiem with nothing. See that Valgsland remembers it at the Alliance.\"", new[] { "Yes1a" });
        t.Line("Yes1a", Hegel, "\"Valgsland remembers everything, Mr. Toras. I will be watching Cavette.\"", new[] { "#386" });
        Vanilla.Require(HegelCall, 335, $"({War}) ~= true");
        t.Variant("Yes2", 611, new[] { 549, 550, 559, 610 }, War, Romus,
            "\"You can. Lespia will walk away from the Meftiem with nothing.\"", new[] { "Yes2a" });
        t.Logic("Yes2a", new[] { "#386" }, script: promise);
        // Not yes, not no (vanilla [612] counted on Valgslandian support, which a war game does not get).
        t.Variant("Maybe2", 612, new[] { 549, 550, 559, 610 }, War, Romus,
            "\"I cannot promise anything, Mr. Hegel. We'll see what is left of Lespia's share when Cavette is done.\"", new[] { "Maybe2a" });
        t.Line("Maybe2a", Hegel, "\"Then I will see it too, Mr. Toras. So will the Alliance.\"", new[] { "#656" });

        // In a war game Valgsland does not back Rizia in the Pales arbitration either: Hegel will not support a kingdom that
        // redraws borders by force. The dialogue skips vanilla's thanks and goes straight on; the news item about Valgsland's
        // support (vanilla [762]) is switched off below.
        const string arbitration =
            "\"As for your arbitration case against Pales, do not count on Valgsland. We do not back a kingdom that redraws " +
            "its borders by force, and I will be quoting that principle to you over Zille.\"";
        const string arbitrationAlso =
            "\"And Valgsland will not back you in your arbitration case against Pales. Borders are not redrawn by whoever has the " +
            "bigger guns, and I intend to say so on the record, with Zille in mind.\"";
        t.Variant("Arb1", 649, new[] { 647 }, War, Hegel, arbitration, new[] { "#359" });
        t.Variant("Arb2", 659, new[] { 657 }, War, Hegel, arbitrationAlso, new[] { "#348" });
        t.Variant("Arb3", 669, new[] { 667 }, War, Hegel, arbitrationAlso, new[] { "#394" });
        t.Variant("Arb4", 679, new[] { 677 }, War, Hegel, arbitration, new[] { "#596" });
        Vanilla.AppendScript(HegelCall, 762, $"if {War} then Variable[\"RiziaDLCSupport.News_Turn05_G_ValgslandANSupport\"] = false end;");
        t.Register();
    }

    // ------------------------------------------------------------------ Cavette (c323)

    private static void RegisterMorella()
    {
        var t = new Talk(Morella, "ZilleWar.E5.Morella.");

        t.Variant("Ask", 225, new[] { 983 }, Active, Romus,
            "\"You know we are at war with Wehlen over Zille. We would ask for your aid in finishing it.\"", new[] { "#985" });
        t.Variant("Peaceful", 240, new[] { 234 }, War, Romus,
            "\"Wehlen's intervention was my father's idea. Ending it is mine, by whatever means it takes.\"", new[] { "#255" });
        // [268] "So that's your backup plan now that diplomatic efforts have failed."
        t.Variant("Partisans", 268, new[] { 270 }, Active, Saltana,
            "\"Supplies to partisans behind a front line? That isn't covert shipping, that's joining your war. Morella has buried " +
            "enough sons for the quarrels of House Toras.\"", new[] { "P_R1", "P_R2", "P_R3" });
        t.Choice("P_R1", "\"It was always going to come to this. Wiktor Smolak is not a man who can be reasoned with.\"", new[] { "P_R1a" });
        t.Line("P_R1a", Saltana, "\"You may be right about that. Radios and medicine can cross the Gulf on a Morellan manifest. Rifles cannot, and I don't want my ships anywhere near the fighting.\"", new[] { "#284" });
        t.Choice("P_R2", "\"Nobody is asking Morella to fight. Only to look the other way at sea.\"", new[] { "P_R2a" });
        t.Line("P_R2a", Saltana,
            "\"Our coast guard can be very slow to answer Wehzek distress calls. That is not the same as helping you.\"", new[] { "#284" });
        t.Choice("P_R3", "\"Then don't think of it as helping House Toras. Think of it as hurting Smolak.\"", new[] { "P_R3a" });
        t.Line("P_R3a", Saltana,
            "\"Now that is an argument I can take to my cabinet.\"", new[] { "#284" });

        // Why do you want our aircraft?
        t.Choice("Planes", "\"We are at war with Wehlen. I won't send Rizian pilots up in machines older than they are.\"", new[] { "Planes2" },
            condition: Active, hooks: From(417));
        t.Line("Planes2", Saltana,
            "\"So you mean to keep going. My aircraft, then. Not my pilots, and not my name on the invoice.\"", new[] { "#444" });
        t.Register();
    }

    // ------------------------------------------------------------------ the debrief with Elena and Laurento (c327)

    private static void RegisterFeedback()
    {
        var t = new Talk(Feedback, "ZilleWar.E5.Feedback.");

        t.Variant("Internal", 376, new[] { 377 }, Active, Esquibel,
            "\"With our soldiers already in Zille, Your Majesty, pressure from inside the region is worth more than ever. Every family " +
            "that hides a Rizian patrol is worth a platoon.\"", new[] { "#387" });
        t.Variant("External", 384, new[] { 383 }, Active, Esquibel,
            "\"With our army in the field, we need every friend we can find in Konstantium. And we have fewer than I would like.\"", new[] { "#387" });
        t.Variant("Crates", 629, new[] { 627 }, Active, Esquibel,
            "\"A wise request, Your Majesty. With our army already in Zille, every crate that reaches the partisans is worth a " +
            "battalion. I withdraw my objections. For now.\"", new[] { "#636" });
        t.Variant("Neighbors", 635, new[] { 633 }, Active, Esquibel,
            "\"A wise request, Your Majesty. Ms. Saltana will never applaud a war. But she may yet say, loudly, that Wehlen started " +
            "this one in 1927 and refused to end it this year.\"", new[] { "#508" });
        t.Variant("ArbitrationStopped", 999, new[] { 997 }, AuditT45.Stopped, Esquibel,
            "\"And Wehlen, needless to say, will speak against us. Smolak's envoy has already told the committee that a kingdom that " +
            "has just fought a war cannot be trusted with an economic zone.\"", new[] { "#1002" });
        t.Variant("Arbitration", 999, new[] { 997 }, Active, Esquibel,
            "\"And Wehlen, needless to say, will speak against us. Smolak's envoy has already told the committee that a kingdom at war " +
            "cannot be trusted with an economic zone.\"", new[] { "#1002" });
        t.Variant("Planes1", 341, new[] { 337 }, Active, Elena,
            "\"With Wehzek aircraft over Zille, I can hardly say we have no use for planes. But will they arrive before the fighting " +
            "is over, or only in time for the victory parade?\"", new[] { "#352", "#355", "#356", "#357" });
        t.Variant("Planes2", 343, new[] { 342 }, Active, Elena,
            "\"Besides, with a war to pay for, I would rather spend what little we have on shells than on aircraft that may arrive " +
            "after the fighting is over.\"", new[] { "#352", "#355", "#356", "#357" });
        t.Register();
    }

    // ------------------------------------------------------------------ the late-night security council (c324)

    private static void RegisterSecurity()
    {
        var t = new Talk(Security, "ZilleWar.E5.Security.");

        // The consulate files from Zille (SupplyVoices: Héctor Ramiro, a Lespian officer, behind the Friendship Day bombing) were
        // read before this meeting: the Palesian parts are a footnote, not a lead. Skip the "investigate Pales" debate.
        string found = $"{War} and Variable[\"{SupplyWar.VarRamiroFound}\"] == true";
        string lucitaLeads = "Variable[\"RiziaDLC.Turn03_Security_LucitaLeadsInvestigation\"] == true";
        Vanilla.Require(Security, 21, $"({found}) ~= true");
        Vanilla.Require(Security, 22, $"({found}) ~= true");
        t.Logic("Files", new[] { "FilesL", "FilesT" }, condition: found, hooks: From(20));
        t.Line("FilesL", Narrator, "Lucita had brought her dossier on the Friendship Day bombing. She laid it on the table and, for once, did not open it.",
            new[] { "FilesL2" }, condition: lucitaLeads);
        t.Line("FilesL2", Lucita,
            "\"My team traced parts of the device to a Palesian electronics firm, Your Majesty. Before Zille, I would have asked you to turn the " +
            "whole investigation on Pales. Then our soldiers opened the Lespian consulate.\"", new[] { "FilesL3" });
        t.Line("FilesL3", Lucita,
            "\"Héctor Ramiro bought those parts through a trading house in Remaz, with Lespian money. Pales sold the wire. Lespia lit the fuse.\"",
            new[] { "FilesHugo" });
        t.Line("FilesT", Narrator, "Titus had the bombing file under his arm. He set it down beside the papers our soldiers had brought back from the Lespian consulate in Zille.",
            new[] { "FilesT2" }, condition: $"not ({lucitaLeads})");
        t.Line("FilesT2", "Titus Gordion",
            "\"The Palesian parts we found came through a trading house in Remaz, sir, paid for by Héctor Ramiro with Lespian money. There is " +
            "no Palesian connection worth your evening. The consulate papers name the man who planned it, and he answers to Perla.\"", new[] { "FilesHugo" });
        t.Line("FilesHugo", Hugo,
            "\"So Wehlen was wrong about Su Omina. I suppose that makes it a good evening for nobody.\"",
            new[] { "FilesC1", "FilesC2" });
        string closed = Set("RiziaDLC.Turn05_Security_InvestigateEndInvestigation", "true");
        t.Choice("FilesC1", "\"Close the Palesian line. We know who did it.\"", new[] { "FilesC1L", "FilesC1T" }, script: closed);
        t.Line("FilesC1L", Lucita, "\"Closed. The Ramiro file stays open in my drawer, Your Majesty. Lespia has a long reach, and I'd like to know where it ends.\"",
            new[] { "#96" }, condition: lucitaLeads);
        t.Line("FilesC1T", "Titus Gordion", "\"Closed, sir. Ramiro's file stays on my desk.\"", new[] { "#96" }, condition: $"not ({lucitaLeads})");
        t.Choice("FilesC2", "\"Keep one man on the Palesian supplier. Someone in Remaz sold a bomb to a stranger.\"", new[] { "FilesC2L", "FilesC2T" });
        t.Line("FilesC2L", Lucita, "\"One man, then. Quietly. Pales has enough reasons to dislike us this year.\"", new[] { "#96" }, condition: lucitaLeads);
        t.Line("FilesC2T", "Titus Gordion", "\"One man, sir. Quietly.\"", new[] { "#96" }, condition: $"not ({lucitaLeads})");

        // The royal yacht (c320): the King asks Vina, bound for Pales, about the bombing. With the files read, he knows who planned it.
        var y = new Talk(Yacht, "ZilleWar.E5.YachtFiles.");
        y.Variant("Ask", 180, new[] { 182 }, found, Romus,
            "\"A Lespian officer planned the Friendship Day bombing, but the parts for the bomb came from a Palesian factory. If you hear who in Remaz sold them, I want to know.\"",
            new[] { "#186" });
        y.Register();

        // The Turn 6 council (c361): Lucita's "ties between Pales and the bombing" after the files named Ramiro.
        var c = new Talk("Rizia/Turn 6/A_CouncilMeeting", "ZilleWar.E5.CouncilFiles.");
        c.Variant("Ties", 406, new[] { 817 }, found, Lucita,
            "\"Are we forgetting that a Palesian factory supplied the parts for the bomb in Zille? Lespia planned it, Your Majesty, but Reinhart's export controls looked the other way.\"",
            new[] { "#157" });
        c.Register();

        // [1425] "...especially after our failure to come to a diplomatic agreement" (and [471] "despite the agreement we signed").
        t.Variant("Casus", 1425, new[] { 1424 }, War, Lucita,
            "\"If we accuse Su Omina now, without concrete leads, we hand Smolak his casus belli after the fact. He'll tell the " +
            "Alliance we went to war to cover for our own bombers.\"", new[] { "#53" });
        Vanilla.Require(Security, 1423, $"not ({War})");
        // Hugo [19]/[23]: "tread carefully... the last thing we need is an escalation of violence."
        t.Variant("SonsStopped", 19, new[] { 18 }, AuditT45.Stopped, Hugo,
            "\"I share your concerns, Your Majesty. But whoever lit that fuse, it was our sons who fought and died in Zille, and their " +
            "families are still counting them. This investigation matters only if it wins us votes in Konstantium.\"", new[] { "#65" });
        t.Variant("Sons", 19, new[] { 18 }, Active, Hugo,
            "\"I share your concerns, Your Majesty. But whoever lit that fuse, our sons are dying in Zille now. This only matters if " +
            "it wins us votes in Konstantium.\"", new[] { "#65" });

        // Border security: in wartime the boats are not only Bluds.
        t.Variant("Boats", 97, new[] { 1468 }, Active, Lucita,
            "\"There's been a surge of boats crossing the Gulf of Moridia. Not only Bluds anymore. Since the fighting began, whole " +
            "Wehzek families from the Zille coast are putting to sea.\"", new[] { "Boats2" });
        t.Line("Boats2", Hugo,
            "\"And how many of them are Smolak's agents? In wartime, Your Majesty, every fishing boat is a question.\"", new[] { "B_R1", "B_R2", "B_R3" });
        t.Choice("B_R1", "\"Open a corridor for the families fleeing Zille. We are fighting Smolak's army, not his people.\"", new[] { "B_R1a" },
            condition: "Variable[\"RiziaDLC.Resources_Budget\"] >= 1", script: Set(VarRefugeePolicy, "\"corridor\"") +
                    "Variable[\"RiziaDLC.Resources_Budget\"] = (Variable[\"RiziaDLC.Resources_Budget\"] or 0) - 1; " +
                    "Variable[\"RiziaDLC.LeaningValue_Democratic\"] = (Variable[\"RiziaDLC.LeaningValue_Democratic\"] or 0) + 1;",
            cost: "-1 Budget");
        t.Line("B_R1a", Lucita,
            "\"It will cost us, and some of Smolak's people will walk through it with the rest. But it will look very good in " +
            "Konstantium. I'll see to it.\"", new[] { "#186" });
        t.Choice("B_R2", "\"Screen every boat. Intern the men of fighting age until the war is over.\"", new[] { "B_R2a" },
            script: Set(VarRefugeePolicy, "\"screen\"") +
                    "Variable[\"RiziaDLC.LeaningValue_Democratic\"] = (Variable[\"RiziaDLC.LeaningValue_Democratic\"] or 0) - 1;");
        t.Line("B_R2a", Lucita,
            "\"That I can do. The camps at Sallabes will need more guards, and the Wehzek radio will need no more material.\"", new[] { "#186" });
        t.Choice("B_R3", "\"We have no room for them. The navy has a war to fight.\"", new[] { "B_R3a" },
            script: Set(VarRefugeePolicy, "\"closed\""));
        t.Line("B_R3a", Lucita,
            "\"Then the patrols will turn them back. I'd ask you to remember that you gave the order, Your Majesty, when the first one " +
            "doesn't make it home.\"", new[] { "#186" });

        // Su Omina in a war zone.
        t.Variant("SuOmina", 204, new[] { 203 }, Active, Lucita,
            "\"Since the declaration, they've stopped preparing. Su Omina cells cut the rail line between Zille and Nanta and have been " +
            "passing our officers the positions of Wehzek batteries. Whatever you think of their methods, they're earning their money now.\"",
            new[] { "#209", "#205", "#210", "#320" });
        t.Register();
    }

    // ------------------------------------------------------------------ the royal yacht (c320)

    private static void RegisterYacht()
    {
        var t = new Talk(Yacht, "ZilleWar.E5.Yacht.");

        t.Variant("Escort", 13, new[] { 7, 11, 12 }, Active, Narrator,
            "Two corvettes kept a respectful distance; Lucita had refused to let me sail until I agreed to the escort.", new[] { "#14" });

        const string wehzeks = "I know how that sounds while we are at war with Wraclawitz. But these Wehzeks fled Smolak's kind decades ago. " +
                               "Win their votes, and you prove this war is against a government, not a people.";
        const string wehzeks2 = "I know how that sounds with our soldiers in the north. Win their votes, and you prove this war is against a " +
                                "government, not a people.";
        const string wehzeks3 = "Our soldiers are fighting a government, not a people. Let these Wehzeks vote for that, and we win the " +
                                "elections as well.";
        t.Variant("Base1", 880, new[] { 877 }, Active, Manus,
            "\"Rumburgians and ethnic Valgs would lean monarchist. But if we mobilize the Wehzek, Derdian and Morellan base now, we " +
            "should gain enough votes to come out on top. " + wehzeks + "\"", new[] { "#886" });
        t.Variant("Base2", 891, new[] { 890 }, Active, Manus,
            "\"But if we start mobilizing the Wehzek, Derdian and Morellan base now, we should gain enough votes to come out on top. " +
            wehzeks2 + "\"", new[] { "#893", "#895" });
        t.Variant("Base3", 1098, new[] { 1095, 1096 }, Active, Manus,
            "\"Rumburgians and ethnic Valgs will lean monarchist. But if we mobilize the Wehzek, Derdian and Morellan base now, we " +
            "should have a significant edge compared to the last elections. " + wehzeks3 + "\"", new[] { "#915" });

        t.Variant("Fuel", 585, new[] { 587 }, Active, Romus,
            "\"See, Vina? This is why the Pales deal matters. Our own gas no longer covers the tanks in Zille. If Reinhart turns off " +
            "the tap, the war ends for us whatever Smolak does.\"", new[] { "#678" });
        t.Register();
    }

    // ------------------------------------------------------------------ the flight home from Morella (c329)

    private static void RegisterFlight()
    {
        var t = new Talk(Flight, "ZilleWar.E5.Flight.");
        t.Variant("North", 32, new[] { 31 }, Active, Narrator,
            "The future of our relationship with Morella and the MITZ should have filled my head. Instead, my thoughts kept drifting " +
            "north, to the soldiers waiting for word from Zille.", new[] { "#33" });
        t.Register();
    }

    // ------------------------------------------------------------------ steps

    public static void OnStep()
    {
        if (GameState.CurrentTurnNum != 5 || !Variables.GetBool(WarFlags.Declared))
        {
            return;
        }

        if (!Variables.GetBool(VarBookkept))
        {
            Variables.Set(VarBookkept, true);
            ReplayWehlenTalksBookkeeping();
        }

        // A war already lost (or stopped) at the first operation has no council to hold about bonds, fronts and a season's costs.
        if (!Variables.GetBool(VarCouncilOffered) && WarFlow.AtWar && !GameState.StoryFragmentExistsInCurrentStep(CouncilFragment))
        {
            try
            {
                GameState.AddCustomConversation(CouncilConv);
                GameState.AddCustomStoryFragment(new CustomConversationFragment(CouncilFragment, SuzerainStoryPackInfo.Rizia,
                    SuzerainTokenName.RiziaCityPorteDrazon, "The Council After Konstantium", "Council Chambers, Palas Rezna",
                    ConversationData.ConversationType.Administration, CouncilConv));
                Variables.Set(VarCouncilOffered, true);
            }
            catch (Exception ex)
            {
                Core.Log.Error($"Adding the T5 council failed: {ex}");
            }
        }

        // Lucita's chess queen is a collectible granted at the council; with no council (the war is already over), grant it quietly.
        if (!Variables.GetBool(VarCouncilOffered) && !WarFlow.AtWar && !Variables.GetBool(VarQueenGiven))
        {
            Variables.Set(VarQueenGiven, true);
            try
            {
                Il2CppPixelCrushers.DialogueSystem.Lua.Run("EnableCollectionItem(\"CollectionItem_ChessPiece\")");
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"Chess queen grant failed: {ex.Message}");
            }
        }

        int step = GameState.CurrentStepNum ?? 0;
        if (step >= 3 && !Variables.GetBool(VarWatchtower))
        {
            Variables.Set(VarWatchtower, true);
            bool city = Variables.GetBool(ZilleOperation.CapturedVar("Zille"));
            NewsWire.Publish("E5_Watchtower", "G", "WHR Reports Abuses on Both Sides of the Zille Front", P(
                "The Watchtower of Human Rights has published its first report on the war in Zille. On the Wehzek side, it lists the " +
                "internment of Rizian-speaking men of fighting age " +
                (city ? "in the towns Wehlen still holds" : "in the region") + ", held, in the words of one witness, " +
                "\"without charge or a lawyer.\"",
                city
                    ? "The report is no kinder to Rizia. It documents the shelling of the harbor district during the battle for Zille " +
                      "city, the detention of Wehzek municipal officials since the city came under Rizian military administration, and " +
                      "attacks on Wehzek police posts by irregulars it describes as \"armed and directed from across the border.\""
                    : "The report is no kinder to Rizia. It documents shelling near residential streets on the outskirts of Zille city " +
                      "and attacks on Wehzek police posts by irregulars it describes as \"armed and directed from across the border.\"",
                "WHR has called on both governments to allow observers into the region and to open safe corridors for civilians. " +
                "Neither government has responded."));
        }

        if (!Variables.GetBool(VarSaltanaReport) && Variables.GetBool("RiziaDLCSupport.Reports_Turn05_Morella_DemandsZilleReturn"))
        {
            Variables.Set(VarSaltanaReport, true);
            // The King may already have sought the ceasefire at this turn's War Council.
            bool stopped = !WarFlow.AtWar || Variables.GetBool(WarFlags.Ceasefire);
            NewsWire.Report("E5_SaltanaCommunique", SuzerainTokenName.RiziaCountryMorella,
                stopped ? "Morella Calls for Talks Over Zille" : "Morella Calls for Ceasefire Talks Over Zille", P(
                stopped
                    ? "In a diplomatic communique sent to both Porte Drazon and Wraclawitz, Prime Minister Alma Saltana has welcomed the " +
                      "end of the fighting in Zille and called for talks under the auspices of the Alliance of Nations before the line " +
                      "of contact hardens into a border. The note places the blame for the war squarely on Wehlen's refusal to honor " +
                      "the 1927 lease."
                    : "In a diplomatic communique sent to both Porte Drazon and Wraclawitz, Prime Minister Alma Saltana has called for a " +
                      "ceasefire in Zille and talks under the auspices of the Alliance of Nations. The note places the blame for the war " +
                      "squarely on Wehlen's refusal to honor the 1927 lease.",
                "Morella's Prime Minister also warned that her government \"will not accept the transformation of the Gulf of Moridia " +
                "into a battlefield,\" and that any Wehzek attempt to annex the region outright would be met with heavy sanctions."));
        }
    }

    /// <summary>What Turn05_FPnT_WehlenNegotiations does that has nothing to do with Wehlen (c326 [1398], [1205], [1314],
    /// [1329], [1399]). [1403] "Wehlen will once again provide vital support to safeguard the Rizian Crown" is left out.</summary>
    private static void ReplayWehlenTalksBookkeeping()
    {
        string lua =
            "Variable[\"RiziaDLCSupport.Codex_Characters_ForeignLeaders_PatricioAlvarez\"] = false; " +
            "Variable[\"RiziaDLCSupport.Codex_Characters_ForeignLeaders_PatricioAlvarez_1953\"] = true; " +
            "Variable[\"RiziaDLCSupport.Reports_Turn05_Montaklar_TaddeusReturnsHome\"] = true; " +
            "if Variable[\"RiziaDLC.Diplomacy_Pales_Military_BlockDrillship\"] == true then Variable[\"RiziaDLCSupport.News_Turn05_G_ATOConcernNavalBlockade\"] = true end; " +
            "if Variable[\"RiziaDLC.Situation_Diplomacy_Pales_ANDORArbitration\"] == true then Variable[\"RiziaDLCSupport.News_Turn05_G_RumburgANSupport\"] = true end; " +
            "Variable[\"RiziaDLCSupport.News_Turn05_G_MITZMorellaDemandsNationalization\"] = true;";
        foreach (string code in new[] { lua, "UpdateProgress(\"Progress_Construction_PalesPipeline\", 50)" })
        {
            try
            {
                Il2CppPixelCrushers.DialogueSystem.Lua.Run(LuaFix.Rewrite(code));
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"T5 bookkeeping failed: {ex.Message}");
            }
        }

        Core.Log.Msg("Replayed the non-Wehlen bookkeeping of Turn05_FPnT_WehlenNegotiations.");
    }

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs);
}
