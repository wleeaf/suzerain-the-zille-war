using SuzerainModdingKit.Character;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.StoryFragments.Conversation.NodeSelectors;
using SuzerainModdingKit.Utils;
using static SuzerainModdingKit.StoryFragments.Conversation.ConversationNodeHook;

namespace ZilleWar;

/// <summary>
/// Entry point C: the Turn 4 Alliance of Nations session in Konstantium (Rizia/Turn 4/FPnT_ANSouthMerkopa).
/// Smolak has just invoked the AN Threat Mitigation Protocol to delay Zille's return. In his reply,
/// after accusing Wehlen ([332] / [357] / [362]), the King can stop threatening and declare war from the
/// podium. The branch plays out the uproar, Smolak's response, the delegations, the press, and rejoins
/// vanilla at [815] (protocol flag, End()). The news flags of [755] are set in the Rain node instead.
/// </summary>
internal static class AnDeclaration
{
    private const string Conversation = "Rizia/Turn 4/FPnT_ANSouthMerkopa";
    private const string P = "ZilleWar.AN.";

    // vanilla nodes (articy IDs from the game data)
    private const string AfterLegalChillingImplications = "0x0100005200014325"; // [332] "...chilling implications..."
    private const string AfterUnitedContana = "0x0100005200014420";             // [357] "...financial backing ... United Contana."
    private const string AfterPeopleOfZille = "0x01000052000144DE";             // [362] "The people of Zille yearn for their home..."
    private const string TakeMattersIntoOwnHands = "0x01000052000145A2";        // [383]

    public const string VarDeclared = "ZilleWar.War_Declared";
    public const string VarDeclaredTurn = "ZilleWar.War_DeclaredTurn";
    public const string VarPressStatement = "ZilleWar.AN_PressStatement";
    public const string VarLespianNote = "ZilleWar.AN_LespianNote";          // "kept" | "burned"
    public const string VarRumburgCable = "ZilleWar.AN_RumburgCable";      // "grateful" | "careful"
    public const string VarAlvarezStance = "ZilleWar.AN_AlvarezStance";    // "backs" (he had called Zille an illegal occupation) | "restraint"

    private const string Smolak = "Wiktor Smolak (R)";
    private const string Esquibel = "Laurento Esquibel";
    private const string Nines = "Ephraim Nines (R)";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Azmal = "Jorga Azmal";
    private const string Reinhart = "Axel Reinhart";
    private const string Narrator = "Narrator";
    private const string Estela = "Estela Toras";
    private const string Saltana = "Alma Saltana";
    private const string Romus = "Player_Romus";             // spoken choices
    private const string RomusAction = "Player_Romus_Italic"; // action choices ("Say nothing.")

    public static void Register()
    {
        foreach (string v in new[] { VarDeclared, VarDeclaredTurn, VarPressStatement, VarLespianNote, VarRumburgCable, VarAlvarezStance, WarState.VarActive })
        {
            SuzerainModdingKit.Variables.Register(v);
        }

        var inj = new ConversationInjection(Conversation);

        // --- the choice, offered right after each accusation ---
        inj.AddNode(new ConversationNode(
            name: P + "Escalate",
            speakerSelector: new CharacterNameSelector(Romus),
            menuText: "Declare war on Wehlen.",
            text: "\"This assembly has listened to Wehlen's excuses long enough.\"",
            luaCondition: $"Variable[\"{VarDeclared}\"] ~= true",
            hooks: new[]
            {
                Gate(AfterLegalChillingImplications),
                Gate(AfterUnitedContana),
                Gate(AfterPeopleOfZille),
            },
            nextNodes: Next("Weight")));
        inj.AddNode(Line("Weight", Narrator,
            "I gripped the edges of the podium. Every eye in the hall was on me. Once spoken, these words could never be taken back.",
            Next("Declare", "BackOff")));
        inj.AddNode(new ConversationNode(
            name: P + "BackOff",
            speakerSelector: new CharacterNameSelector(Romus),
            menuText: "Hold back.",
            text: "\"...and Rizia's patience is not without limits.\"",
            nextNodes: new ConversationNodeSelector[] { new ConversationNodeArticyIDSelector(TakeMattersIntoOwnHands) }));
        inj.AddNode(new ConversationNode(
            name: P + "Declare",
            speakerSelector: new CharacterNameSelector(Romus),
            menuText: "\"I hereby declare war on Wehlen.\"",
            text: "\"Twenty-five years ago, in a dark hour, my father lent Zille to a friend who had stood by him. The lease has ended, " +
                  "and the friend has decided to keep what was only ever borrowed, and to call it security.\"",
            luaScript:
                $"Variable[\"{VarDeclared}\"] = true; " +
                $"Variable[\"{VarDeclaredTurn}\"] = 4; Variable[\"{WarState.VarActive}\"] = true; " +
                "Variable[\"RiziaDLC.Diplomacy_Wehlen_ANSpeech_Aggression\"] = true; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Neutral\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Unfriendly\"] = false; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Friendly\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Allied\"] = false; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Hostile\"] = true; Variable[\"RiziaDLC.CountryRelation_Wehlen_AtWar\"] = true; " +
                "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"AtWar\"); " + EarlyBookkeeping.DeclarationLua,
            sequence: Wait(new ConversationNodeSequenceBuilder().PlaySceneMusic("Critical").ToString()),
            nextNodes: Next("Declare_p2")));

        inj.AddNode(Choice("Declare_p2", "\"Let this Alliance bear witness: in the name of the Rizian Crown, I hereby declare war on Wehlen. " +
            "Rizia will not lay down its arms until Zille is home.\"", Next("Silence")));

        // --- the hall ---
        inj.AddNode(Line("Silence", Narrator,
            "For a moment, the only sound in the assembly hall was the rain drumming on the dome.", Next("Uproar")));
        inj.AddNode(Line("Uproar", Narrator,
            "Then the hall erupted. Delegates were on their feet, shouting over one another in a dozen languages. Somewhere behind me, a glass shattered on the marble floor.",
            Next("Gavel")));
        inj.AddNode(Line("Gavel", Nines,
            "\"Order! Order in the assembly! I will have order!\"", Next("Laurento"), conversant: true));
        inj.AddNode(Line("Laurento", Esquibel,
            "\"Your Majesty...\" Laurento had gone pale. \"What have you done?\"", Next("L1", "L2", "L3")));
        inj.AddNode(Choice("L1", "\"I'm setting right my father's mistake, Laurento. Someone had to.\"", Next("SmolakRises")));
        inj.AddNode(Choice("L2", "\"What had to be done, Laurento. Now help me see it through.\"", Next("SmolakRises")));
        inj.AddNode(Choice("L3", "I said nothing and kept my eyes on Smolak.", Next("SmolakRises"), menu: "Say nothing."));

        // --- Smolak ---
        inj.AddNode(Line("SmolakRises", Narrator,
            "Wiktor Smolak rose slowly from his seat. For the first time that day, the grin was gone from his face.", Next("Smolak1")));
        inj.AddNode(Line("Smolak1", Smolak,
            "\"President Nines, I ask that the Alliance record this moment. The Kingdom of Rizia has declared war on a member state, from this very podium.\"",
            Next("JabScapegoats", "JabApology", "JabDenounce", "JabCooperate", "Smolak2"), conversant: true));
        // Smolak throws the King's own Turn 3 and 4 choices back at him (first matching line plays).
        const string scapegoats = "Variable[\"RiziaDLC.Diplomacy_Wehlen_Scapegoats\"] == true";
        const string apology = "Variable[\"RiziaDLC.Decision_Turn03_ZilleBombApology\"] == true";
        const string denounce = "Variable[\"RiziaDLC.Decision_Turn03_ZilleIncidentResponse_DenounceSO\"] == true";
        const string cooperate = "Variable[\"RiziaDLC.Decision_Turn04_WehlenInvestigation_Cooperate\"] == true";
        inj.AddNode(Line("JabScapegoats", Smolak,
            "\"A month ago, His Majesty sent us two of his countrymen in chains and called them the bombers of Smolak Square. We accepted " +
            "them in good faith. Now we see what his good faith was worth.\"", Next("Smolak2"), condition: scapegoats));
        inj.AddNode(Line("JabApology", Smolak,
            "\"Only weeks ago, His Majesty apologized to me for the words he spoke in Zille. I see now what the apology was for. He was " +
            "sorry I had heard them too early.\"", Next("Smolak2"), condition: $"not ({scapegoats}) and {apology}"));
        inj.AddNode(Line("JabDenounce", Smolak,
            "\"His Majesty condemned his own nationalists after the bombing. Today he marches with them. The Alliance may decide which of " +
            "his two faces it prefers.\"", Next("Smolak2"), condition: $"not ({scapegoats}) and not ({apology}) and {denounce}"));
        inj.AddNode(Line("JabCooperate", Smolak,
            "\"Only hours ago his security men sat across the table from mine and swore we were partners in finding the bombers. I wonder now " +
            "how many of their questions were about the bombing, and how many were about our barracks.\"", Next("Smolak2"),
            condition: $"not ({scapegoats}) and not ({apology}) and not ({denounce}) and {cooperate}"));
        inj.AddNode(Line("Smolak2", Smolak,
            "\"Wehlen did not seek this war. But Wehlen will defend every inch of Zille, and every Wehzek who calls it home.\"", Next("Smolak3")));
        inj.AddNode(Line("Smolak3", Smolak,
            "\"I hope your army is as eloquent as you are, Your Majesty. You will need it to be.\"", Next("S1", "S2", "S3")));
        inj.AddNode(Choice("S1", "\"My army will answer you at the border, Mr. President.\"", Next("Walkout")));
        inj.AddNode(Choice("S2", "\"Zille was never yours to defend.\"", Next("Walkout")));
        inj.AddNode(Choice("S3", "\"Enjoy your last days in Smolak Square. Its old name is coming back.\"", Next("Walkout")));
        inj.AddNode(Line("Walkout", Narrator,
            "Smolak said nothing more. He turned his back on the podium, and one by one the Wehzek delegation rose and followed him " +
            "out of the hall, leaving their flag standing beside a row of empty chairs.", Next("Delegations")));

        // --- the delegations ---
        // If Alvarez already called Zille an illegal occupation in his own speech ([221]), he cannot call for restraint now.
        const string lespiaSpoke = "Variable[\"RiziaDLC.Diplomacy_Lespia_WehlenHelp\"] == true";
        inj.AddNode(new ConversationNode(
            name: P + "Delegations",
            text: "The other delegations were already turning to each other, and to their aides, and to the telephones at the back of the hall.",
            speakerSelector: new CharacterNameSelector(Narrator),
            sequence: Wait(null),
            nextNodes: Next("AlvarezBacks", "Alvarez1")));
        inj.AddNode(Line("AlvarezBacks", Alvarez,
            "\"Lespia has already told this assembly what it thinks of Wehlen's occupation of Zille. What Rizia chooses to do about it is " +
            "Rizia's business.\"", Next("AlvarezBacks2"), conversant: true, condition: lespiaSpoke,
            script: $"Variable[\"{VarAlvarezStance}\"] = \"backs\";"));
        inj.AddNode(Line("AlvarezBacks2", Narrator,
            "He sat back down without looking at Smolak. Somewhere behind him, a Wehzek aide was writing down every word.", Next("Hegel1")));
        inj.AddNode(Line("Alvarez1", Alvarez,
            "\"Lespia calls on both parties to show restraint.\"", Next("Alvarez2"), conversant: true, condition: $"not ({lespiaSpoke})",
            script: $"Variable[\"{VarAlvarezStance}\"] = \"restraint\";"));
        inj.AddNode(Line("Alvarez2", Narrator,
            "Alvarez's lips twitched as he sat back down. I could not tell whether he was hiding a frown or a smile.", Next("Hegel1")));
        inj.AddNode(Line("Hegel1", Hegel,
            "\"Valgsland has heard enough to know what it thinks of this, Your Majesty. It will say so when the time comes.\"", Next("Azmal1"), conversant: true));
        inj.AddNode(Line("Azmal1", Azmal,
            "\"Derdia has long said that the occupation of Zille was a wound that would not close on its own.\"", Next("ReinhartBlockade", "ReinhartFriendly", "Reinhart1"), conversant: true));
        // Reinhart's tone follows Rizia's dealings with Pales: a gas deal or good relations soften him.
        const string palesFriendly =
            "(Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Buy\"] == true and Variable[\"RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled\"] ~= true) or " +
            "(Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Cede\"] == true and Variable[\"RiziaDLC.Diplomacy_Pales_GasField_CedeCancelled\"] ~= true) or " +
            "Variable[\"RiziaDLC.CountryRelation_Pales_Friendly\"] == true or Variable[\"RiziaDLC.CountryRelation_Pales_Allied\"] == true";
        const string blockade = "Variable[\"RiziaDLC.Diplomacy_Pales_Military_BlockDrillship\"] == true";
        inj.AddNode(Line("ReinhartBlockade", Reinhart,
            "\"Your warships are sitting around my drillship as we speak, King Romus. Forgive me if I don't take your word on where this " +
            "war of yours stops.\"", Next("R1", "R1Blockade", "R2", "R3"), conversant: true, condition: blockade));
        inj.AddNode(Line("ReinhartFriendly", Reinhart,
            "\"Pales has made its peace with Rizia, King Romus, and I intend to keep it. I only ask that this war stays where it " +
            "belongs. In Zille, and nowhere near my border.\"", Next("R1", "R2", "R3"), conversant: true, condition: $"not ({blockade}) and ({palesFriendly})"));
        inj.AddNode(Line("Reinhart1", Reinhart,
            "\"And when Rizia is finished with Zille, King Romus? Should Pales start counting its days as well?\"", Next("R1", "R2", "R3"),
            conversant: true, condition: $"not ({blockade}) and not ({palesFriendly})"));
        inj.AddNode(Choice("R1", "\"Pales has nothing to fear from us, Duke Reinhart.\"", Next("Suspend"), condition: $"not ({blockade})"));
        inj.AddNode(Choice("R1Blockade", "\"Our ships are guarding a lawful claim, Duke Reinhart. They are not an army of invasion.\"", Next("R1Blockade2"),
            condition: blockade));
        inj.AddNode(Line("R1Blockade2", Narrator,
            "Reinhart gave a short, joyless laugh. Beside me, Laurento studied the ceiling as if the dome had suddenly become very interesting.",
            Next("Suspend")));
        inj.AddNode(Choice("R2", "\"That depends entirely on you.\"", Next("Suspend")));
        inj.AddNode(Choice("R3", "I ignored him.", Next("Suspend"), menu: "Ignore him."));

        // --- suspension and the press ---
        inj.AddNode(Line("Suspend", Nines,
            "\"This session is suspended. The Security Committee will convene in emergency session within the hour. Delegations, please clear the floor.\"",
            Next("SideDoor")));
        inj.AddNode(Line("SideDoor", Narrator,
            "Guards were already moving through the aisles. Laurento took me by the arm and steered me toward a side door before anyone could reach me.",
            Next("Saltana0", "Note"), sequence: new ConversationNodeSequenceBuilder().PlaySceneMusic("Suspense").ToString()));
        // If the King's speech never reached the Trade Zone, Saltana asks him in the corridor, so Turn 5's MITZ board has its answer.
        const string mitzUnspoken = "Variable[\"RiziaDLCIsolated.Turn04_ANSpeech_Morella\"] ~= true and Variable[\"RiziaDLC.Diplomacy_Morella_ANSpeech_SideWith\"] ~= true " +
                                    "and Variable[\"RiziaDLC.Diplomacy_Morella_ANSpeech_SideAgainst\"] ~= true";
        inj.AddNode(Line("Saltana0", Narrator,
            "Someone had reached the side door before us. Alma Saltana stood in the corridor with her arms folded, a single aide at her shoulder.",
            Next("Saltana1"), condition: mitzUnspoken));
        inj.AddNode(Line("Saltana1", Saltana,
            "\"I won't keep you. I imagine you have a great deal to do now.\"", Next("Saltana2"), conversant: true));
        inj.AddNode(Line("Saltana2", Saltana,
            "\"But I came here to ask the Alliance about the Meftiem, and you never answered me. Your war hasn't made my question go away. " +
            "Where does Rizia stand on the Trade Zone?\"", Next("M1", "M2", "M3")));
        inj.AddNode(Choice("M1", "\"With Morella. The Meftiem's wealth belongs to the people who dig it out of the ground.\"", Next("M1a"),
            script: "Variable[\"RiziaDLC.Diplomacy_Morella_ANSpeech_SideWith\"] = true; " +
                    "Variable[\"RiziaDLC.Diplomacy_Morella_NegotiatingPower\"] = (Variable[\"RiziaDLC.Diplomacy_Morella_NegotiatingPower\"] or 0) + 2; " +
                    "Variable[\"RiziaDLCIsolated.Turn04_ANSpeech_Morella\"] = true;"));
        inj.AddNode(Line("M1a", Saltana,
            "\"Then perhaps you are more than a king who settles his borders with tanks. We shall see at the Meftiem.\"", Next("SaltanaGone")));
        inj.AddNode(Choice("M2", "\"Rizia will not tear up the Trade Zone, Prime Minister. Not for Morella, and not for anyone.\"", Next("M2a"),
            script: "Variable[\"RiziaDLC.Diplomacy_Morella_ANSpeech_SideAgainst\"] = true; " +
                    "Variable[\"RiziaDLC.Diplomacy_Lespia_NegotiatingPower\"] = (Variable[\"RiziaDLC.Diplomacy_Lespia_NegotiatingPower\"] or 0) + 1; " +
                    "Variable[\"RiziaDLCIsolated.Turn04_ANSpeech_Morella\"] = true;"));
        inj.AddNode(Line("M2a", Saltana,
            "\"So you will take back your land by force and keep a hand in ours. At least you are consistent.\"", Next("SaltanaGone")));
        inj.AddNode(Choice("M3", "\"Ask me again when the guns have stopped.\"", Next("M3a"),
            script: "Variable[\"RiziaDLCIsolated.Turn04_ANSpeech_Morella\"] = true;"));
        inj.AddNode(Line("M3a", Saltana,
            "\"The board meets next month, war or no war. I'll expect an answer by then.\"", Next("SaltanaGone")));
        inj.AddNode(Line("SaltanaGone", Narrator,
            "She stepped aside to let us pass. Her aide was already scribbling in a notebook.", Next("Note")));
        inj.AddNode(Line("Note", Narrator,
            "In the corridor, a young Lespian aide brushed past us. When he was gone, Laurento was holding a folded slip of paper.",
            Next("NoteRead")));
        inj.AddNode(Line("NoteRead", Esquibel,
            "\"From Prime Minister Alvarez.\" He read it twice before handing it to me. \"'Lespia stands ready to discuss practical " +
            "arrangements. Discreetly.' That man never does anything for free, Your Majesty.\"", Next("NoteGas", "NoteMitz", "NoteFavor")));
        const string ownsAureus = "Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Buy\"] == true and Variable[\"RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled\"] ~= true";
        const string noItzPromise = "Variable[\"RiziaDLC.Diplomacy_Lespia_PromiseITZSupport\"] ~= true";
        inj.AddNode(Line("NoteGas", Esquibel,
            "\"And I can guess the price. We outbid LesPower for the Aureus field. He has wanted that gas ever since the Duke came to us.\"",
            Next("N1", "N2"), condition: ownsAureus));
        inj.AddNode(Line("NoteMitz", Esquibel,
            "\"I can guess the price. Lespia still has no promise from us on the Meftiem, and the board meets next month.\"",
            Next("N1", "N2"), condition: $"not ({ownsAureus}) and {noItzPromise}"));
        inj.AddNode(Line("NoteFavor", Esquibel,
            "\"He already has his share of the gas and your word on the Meftiem. Whatever he asks for next, he hasn't thought of it yet.\"",
            Next("N1", "N2"), condition: $"not ({ownsAureus}) and not ({noItzPromise})"));
        inj.AddNode(Choice("N1", "\"Burn it. I won't have Lespia's fingerprints on this war.\"", Next("PressWarning"),
            script: $"Variable[\"{VarLespianNote}\"] = \"burned\";"));
        inj.AddNode(Choice("N2", "\"Keep it. We may need friends we don't trust.\"", Next("PressWarning"),
            script: $"Variable[\"{VarLespianNote}\"] = \"kept\";"));
        inj.AddNode(Line("PressWarning", Esquibel,
            "\"Every camera in Konstantium will be on you in thirty seconds, Your Majesty. What do I tell them?\"", Next("Press1", "Press2")));
        inj.AddNode(Choice("Press1", "\"Nothing. The King will speak for himself.\"", Next("Flashbulbs")));
        inj.AddNode(Choice("Press2", "\"Tell them Rizia is taking back what is hers. Nothing more.\"", Next("Car"),
            script: $"Variable[\"{VarPressStatement}\"] = \"brief\";"));
        inj.AddNode(Line("Flashbulbs", Narrator,
            "The doors opened onto a wall of flashbulbs. \"Your Majesty! Is Rizia at war?\" \"Did Rumburg approve this?\" \"What happens to the Wehzeks in Zille?\"",
            Next("Q1", "Q2", "Q3")));
        inj.AddNode(Choice("Q1", "\"Rizia is at war with Wehlen's army, and with no one else.\"", Next("Car"),
            script: $"Variable[\"{VarPressStatement}\"] = \"occupier\";"));
        inj.AddNode(Choice("Q2", "\"Every Wehzek family in Zille will be safe under the Rizian crown. You have my word.\"", Next("Car"),
            script: $"Variable[\"{VarPressStatement}\"] = \"assurance\";"));
        inj.AddNode(Choice("Q3", "I pushed through the crowd without a word.", Next("Car"), menu: "Push through without a word.",
            script: $"Variable[\"{VarPressStatement}\"] = \"silent\";"));

        // --- the ride out ---
        inj.AddNode(Line("Car", Narrator,
            "In the car, Laurento was already on the telephone. I caught only fragments. \"Mobilization.\" \"The border.\" \"Mave Nortus.\"",
            Next("GeneralStaff")));
        inj.AddNode(Line("GeneralStaff", Esquibel,
            "\"The General Staff will be waiting for you in Porte Drazon, Your Majesty. They're already drawing up plans.\"", Next("Envoys", "Receiver")));
        inj.AddNode(Line("Envoys", Esquibel,
            "He lowered the receiver for a moment. \"My people in Wraclawitz, Your Majesty. The mission we sent this winter. They were " +
            "still at the Foreign Ministry when you spoke. I'm told they are being driven to the border as we speak.\"", Next("Receiver"),
            condition: "Variable[\"RiziaDLC.Decision_Turn03_DiplomaticEnvoy_Wehlen\"] == true"));
        inj.AddNode(Line("Receiver", Narrator,
            "The telephone rang again. Laurento listened for a moment, then held the receiver out to me. \"The Queen Mother, Your Majesty.\"",
            Next("Estela1")));
        inj.AddNode(Line("Estela1", Estela,
            "\"I heard it on the radio, darling. The whole palace heard it. The maids are crying in the kitchens.\"", Next("EstelaStab", "Estela2"), conversant: true));
        inj.AddNode(Line("EstelaStab", Estela,
            "\"I told you to smile, preach unity and stab them in the back. I didn't mean in front of the entire Alliance. But I won't " +
            "pretend I'm not glad.\"", Next("Estela2"), condition: "Variable[\"RiziaDLCIsolated.Turn04_EstelaTalk_Wehlen\"] == true"));
        inj.AddNode(Line("Estela2", Estela,
            "\"Your father lent Zille away to buy a friend's loyalty, and spent twenty-five years telling himself it was only a loan. Someone finally had to collect it.\"", Next("Estela3")));
        inj.AddNode(Line("Estela3", Estela,
            "\"Bring me home, Romus. Bring all of them home.\"", Next("E1", "E2", "E3")));
        inj.AddNode(Choice("E1", "\"I will, Mother. I promise.\"", Next("Cable")));
        inj.AddNode(Choice("E2", "\"This isn't about our old house, Mother. It's about Rizia.\"", Next("Cable")));
        inj.AddNode(Choice("E3", "\"Pray that it ends quickly.\"", Next("Cable")));
        inj.AddNode(Line("Cable", Esquibel,
            "\"One more, Your Majesty.\" Laurento held up a telegram form. \"From Thornbourgh. It must have been sent the minute the news reached the Queen.\"",
            Next("Cable2", "Cable2NoEmbargo")));
        const string embargo =
            "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true";
        inj.AddNode(Line("Cable2", Narrator,
            "He read it aloud, and by the second sentence he was trying not to smile. \"Romus. I gather from the wireless that " +
            "you have declared war. One does like to hear these things before the newspapers do. Rumburg stands with you, naturally, " +
            "and we stopped buying Smolak's oil months ago, so you needn't ask. Do try to win it quickly. Long wars are ruinous for " +
            "treasuries, and worse for thrones. Beatrice.\"", Next("Cable3"), condition: embargo));
        inj.AddNode(Line("Cable2NoEmbargo", Narrator,
            "He read it aloud, and by the second sentence he was trying not to smile. \"Romus. I gather from the wireless that " +
            "you have declared war. One does like to hear these things before the newspapers do. Rumburg stands with you, naturally. " +
            "We are still buying Smolak's oil, which I suspect you'll want to discuss. Ring me when you've finished making speeches. " +
            "Beatrice.\"", Next("Cable3"), condition: $"not ({embargo})"));
        inj.AddNode(Line("Cable3", Esquibel,
            "\"Her Majesty's sense of humor survives even a war, it seems.\" Laurento folded the telegram and handed it to me. " +
            "\"But she's with us, and Valgsland will know it by morning. That matters more than anything Smolak said today.\"",
            Next("C1", "C2")));
        inj.AddNode(Choice("C1", "\"Cable her back. Tell her Rizia will never forget this.\"", Next("Rain"),
            script: $"Variable[\"{VarRumburgCable}\"] = \"grateful\";"));
        inj.AddNode(Choice("C2", "\"Tell her I'll ring her from Porte Drazon. We have a great deal to discuss.\"", Next("Rain"),
            script: $"Variable[\"{VarRumburgCable}\"] = \"careful\";"));
        inj.AddNode(new ConversationNode(
            name: P + "Rain",
            text: "Outside, the rain had started again. By the time we reached the airfield, it was pouring.",
            speakerSelector: new CharacterNameSelector(Narrator),
            // Skip vanilla [755] but keep what it does: it enables the three AN news items of the day. In a war game their texts are
            // rewritten (WorldPress.OnStep), so the flags are set here on purpose. Then the protocol flag ([815]) and vanilla's End().
            // The vanilla protest reports ([805]-[812], "in the weeks since Smolak's AN announcement") are stale on the day war is
            // declared; EarlyT4 files wartime reports on the ATA and Su Omina in Zille instead.
            luaScript: "PlayCustomMapMusic(\"Rizia_Map_Music_Tense\"); " +
                       "Variable[\"RiziaDLCSupport.News_Turn04_RH_ZilleAnnouncement\"] = true; " +
                       "Variable[\"RiziaDLCSupport.News_Turn04_G_AN_WehlenAnnouncement\"] = true; " +
                       "Variable[\"RiziaDLCSupport.News_Turn04_G_AN_Hegel\"] = true;",
            nextNodes: new ConversationNodeSelector[] { new ConversationNodeArticyIDSelector(VanillaProtocolAndEnd) }));

        foreach (ConversationNode n in Extra)
        {
            inj.AddNode(n);
        }

        foreach (ConversationNode n in inj.Nodes)
        {
            LuaLint.Record(n.Name, n.LuaCondition, n.LuaScript);
        }

        inj.Register();
        Core.Log.Msg("Registered the AN declaration-of-war branch (Turn 4, Konstantium).");
    }

    // [815] sets RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol and leads to End().
    private const string VanillaProtocolAndEnd = "0x0100006500009E70";

    private static ConversationNodeHook Gate(string articyId) =>
        new(new ConversationNodeArticyIDSelector(articyId), HookMode.ConditionGated);

    private static ConversationNodeSelector[] Next(params string[] names) =>
        names.Select(n => (ConversationNodeSelector)new ConversationNodeModdedNameSelector(P + n)).ToArray();

    /// <summary>Continuation nodes of long lines (split like Talk.Line does), added to the injection in Register.</summary>
    private static readonly List<ConversationNode> Extra = new();

    private static ConversationNode Line(string name, string speaker, string text, ConversationNodeSelector[] next,
        bool conversant = false, string sequence = null, string condition = null, string script = null)
    {
        List<string> parts = Talk.SplitSpeech(text);
        for (int i = 1; i < parts.Count; i++)
        {
            Extra.Add(new ConversationNode(
                name: $"{P}{name}_p{i + 1}",
                text: parts[i],
                speakerSelector: new CharacterNameSelector(speaker),
                nextNodes: i == parts.Count - 1 ? next : Next($"{name}_p{i + 2}")));
        }

        return new(
            name: P + name,
            text: parts[0],
            speakerSelector: new CharacterNameSelector(speaker),
            sequence: Wait(sequence),
            luaScript: LuaFix.Rewrite((conversant ? $"AddConversant(\"{speaker}\"); " : string.Empty) + script),   // vanilla Rizia adds conversants in Lua, quoted
            luaCondition: LuaFix.Rewrite(condition),
            nextNodes: parts.Count > 1 ? Next($"{name}_p2") : next);
    }

    // A node with its own sequence replaces the default one, which is what waits for the player to click on.
    // Vanilla ends such sequences with WaitForMessage(Continue); without it the next line shows at once.
    private static string Wait(string sequence) =>
        string.IsNullOrEmpty(sequence) ? sequence : sequence.TrimEnd().TrimEnd(';') + "; WaitForMessage(Continue);";

    // The conversation's default actor here is a technical actor, not the King, so choices must name him
    // explicitly or the game plays them as ordinary lines. Action choices use the italic narration actor.
    private static ConversationNode Choice(string name, string text, ConversationNodeSelector[] next, string menu = null, string script = null,
        string condition = null) =>
        new(name: P + name, text: text, menuText: menu, luaScript: LuaFix.Rewrite(script), nextNodes: next, luaCondition: LuaFix.Rewrite(condition),
            speakerSelector: new CharacterNameSelector(menu != null && !text.StartsWith("\"") ? RomusAction : Romus));
}
