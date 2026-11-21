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
/// vanilla at [755] (news flags, End()).
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
    public const string VarEntryAN = "ZilleWar.Entry_AN";
    public const string VarDeclaredTurn = "ZilleWar.War_DeclaredTurn";
    public const string VarPressStatement = "ZilleWar.AN_PressStatement";
    public const string VarLespianNote = "ZilleWar.AN_LespianNote";          // "kept" | "burned"
    public const string VarEstelaCall = "ZilleWar.AN_EstelaCall";
    public const string VarRumburgCable = "ZilleWar.AN_RumburgCable";      // "grateful" | "careful"

    private const string Smolak = "Wiktor Smolak (R)";
    private const string Esquibel = "Laurento Esquibel";
    private const string Nines = "Ephraim Nines (R)";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Hegel = "Emmerich Hegel (R)";
    private const string Azmal = "Jorga Azmal";
    private const string Reinhart = "Axel Reinhart";
    private const string Narrator = "Narrator";
    private const string Estela = "Estela Toras";
    private const string Romus = "Player_Romus";             // spoken choices
    private const string RomusAction = "Player_Romus_Italic"; // action choices ("Say nothing.")

    public static void Register()
    {
        foreach (string v in new[] { VarDeclared, VarEntryAN, VarDeclaredTurn, VarPressStatement, VarLespianNote, VarEstelaCall, VarRumburgCable, WarState.VarActive })
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
            text: "\"Twenty-five years ago, my father signed Zille away to save his throne. I will not sign away its future. " +
                  "Let this Alliance bear witness: in the name of the Rizian Crown, I hereby declare war on Wehlen. Rizia will not " +
                  "lay down its arms until the last Wehzek soldier has left Rizian soil.\"",
            luaScript:
                $"Variable[\"{VarDeclared}\"] = true; Variable[\"{VarEntryAN}\"] = true; " +
                $"Variable[\"{VarDeclaredTurn}\"] = 4; Variable[\"{WarState.VarActive}\"] = true; " +
                "Variable[\"RiziaDLC.Diplomacy_Wehlen_ANSpeech_Aggression\"] = true; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Neutral\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Unfriendly\"] = false; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Friendly\"] = false; Variable[\"RiziaDLC.CountryRelation_Wehlen_Allied\"] = false; " +
                "Variable[\"RiziaDLC.CountryRelation_Wehlen_Hostile\"] = true; Variable[\"RiziaDLC.CountryRelation_Wehlen_AtWar\"] = true; " +
                "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"AtWar\");",
            sequence: Wait(new ConversationNodeSequenceBuilder().PlaySceneMusic("Critical").ToString()),
            nextNodes: Next("Silence")));

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
        inj.AddNode(Choice("L1", "\"What my father should have done twenty-five years ago.\"", Next("SmolakRises")));
        inj.AddNode(Choice("L2", "\"What had to be done, Laurento. Now help me see it through.\"", Next("SmolakRises")));
        inj.AddNode(Choice("L3", "I said nothing and kept my eyes on Smolak.", Next("SmolakRises"), menu: "Say nothing."));

        // --- Smolak ---
        inj.AddNode(Line("SmolakRises", Narrator,
            "Wiktor Smolak rose slowly from his seat. For the first time that day, the grin was gone from his face.", Next("Smolak1")));
        inj.AddNode(Line("Smolak1", Smolak,
            "\"President Nines, I ask that the Alliance record this moment. The Kingdom of Rizia has declared war on a member state, from this very podium.\"",
            Next("Smolak2"), conversant: true));
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
        inj.AddNode(Line("Delegations", Narrator,
            "The other delegations were already turning to each other, and to their aides, and to the telephones at the back of the hall.",
            Next("Alvarez1")));
        inj.AddNode(Line("Alvarez1", Alvarez,
            "\"Lespia calls on both parties to show restraint.\"", Next("Alvarez2"), conversant: true));
        inj.AddNode(Line("Alvarez2", Narrator,
            "Alvarez's lips twitched as he sat back down. I could not tell whether he was hiding a frown or a smile.", Next("Hegel1")));
        inj.AddNode(Line("Hegel1", Hegel,
            "\"Valgsland will be watching this very closely, Your Majesty. Very closely indeed.\"", Next("Azmal1"), conversant: true));
        inj.AddNode(Line("Azmal1", Azmal,
            "\"Derdia has long said that the occupation of Zille was a wound that would not close on its own.\"", Next("ReinhartFriendly", "Reinhart1"), conversant: true));
        // Reinhart's tone follows Rizia's dealings with Pales: a gas deal or good relations soften him.
        const string palesFriendly =
            "(Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Buy\"] == true and Variable[\"RiziaDLC.Diplomacy_Pales_GasField_BuyCancelled\"] ~= true) or " +
            "(Variable[\"RiziaDLC.Diplomacy_Pales_GasField_Cede\"] == true and Variable[\"RiziaDLC.Diplomacy_Pales_GasField_CedeCancelled\"] ~= true) or " +
            "Variable[\"RiziaDLC.CountryRelation_Pales_Friendly\"] == true or Variable[\"RiziaDLC.CountryRelation_Pales_Allied\"] == true";
        inj.AddNode(Line("ReinhartFriendly", Reinhart,
            "\"Pales has made its peace with Rizia, King Romus, and I intend to keep it. I only ask that this war stays where it " +
            "belongs. In Zille, and nowhere near my border.\"", Next("R1", "R2", "R3"), conversant: true, condition: palesFriendly));
        inj.AddNode(Line("Reinhart1", Reinhart,
            "\"And when Rizia is finished with Zille, King Romus? Should Pales start counting its days as well?\"", Next("R1", "R2", "R3"),
            conversant: true, condition: $"not ({palesFriendly})"));
        inj.AddNode(Choice("R1", "\"Pales has nothing to fear from us, Duke Reinhart.\"", Next("Suspend")));
        inj.AddNode(Choice("R2", "\"That depends entirely on you.\"", Next("Suspend")));
        inj.AddNode(Choice("R3", "I ignored him.", Next("Suspend"), menu: "Ignore him."));

        // --- suspension and the press ---
        inj.AddNode(Line("Suspend", Nines,
            "\"This session is suspended. The Security Committee will convene in emergency session within the hour. Delegations, please clear the floor.\"",
            Next("SideDoor")));
        inj.AddNode(Line("SideDoor", Narrator,
            "Guards were already moving through the aisles. Laurento took me by the arm and steered me toward a side door before anyone could reach me.",
            Next("Note"), sequence: new ConversationNodeSequenceBuilder().PlaySceneMusic("Suspense").ToString()));
        inj.AddNode(Line("Note", Narrator,
            "In the corridor, a young Lespian aide brushed past us. When he was gone, Laurento was holding a folded slip of paper.",
            Next("NoteRead")));
        inj.AddNode(Line("NoteRead", Esquibel,
            "\"From Prime Minister Alvarez.\" He read it twice before handing it to me. \"'Lespia stands ready to discuss practical " +
            "arrangements. Discreetly.' That man never does anything for free, Your Majesty.\"", Next("N1", "N2")));
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
        inj.AddNode(Choice("Q1", "\"Rizia is at war with no one but the army occupying our land.\"", Next("Car"),
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
            "\"The General Staff will be waiting for you in Porte Drazon, Your Majesty. They're already drawing up plans.\"", Next("Receiver")));
        inj.AddNode(Line("Receiver", Narrator,
            "The telephone rang again. Laurento listened for a moment, then held the receiver out to me. \"The Queen Mother, Your Majesty.\"",
            Next("Estela1")));
        inj.AddNode(Line("Estela1", Estela,
            "\"I heard it on the radio, darling. The whole palace heard it. The maids are crying in the kitchens.\"", Next("Estela2"), conversant: true));
        inj.AddNode(Line("Estela2", Estela,
            "\"Your father never dared. Twenty-five years, and he never once dared.\"", Next("Estela3")));
        inj.AddNode(Line("Estela3", Estela,
            "\"Bring me home, Romus. Bring all of them home.\"", Next("E1", "E2", "E3")));
        inj.AddNode(Choice("E1", "\"I will, Mother. I promise.\"", Next("Cable"), script: $"Variable[\"{VarEstelaCall}\"] = \"promise\";"));
        inj.AddNode(Choice("E2", "\"This isn't about our old house, Mother. It's about Rizia.\"", Next("Cable"), script: $"Variable[\"{VarEstelaCall}\"] = \"rizia\";"));
        inj.AddNode(Choice("E3", "\"Pray that it ends quickly.\"", Next("Cable"), script: $"Variable[\"{VarEstelaCall}\"] = \"pray\";"));
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
            // Skip vanilla [755]: it switches on the peacetime AN news ("Wehlen Refuses to Relinquish Zille"...).
            // Keep the rest of the vanilla ending: the Zille protest reports ([805]-[812]) and the protocol flag ([815]).
            luaScript: "PlayCustomMapMusic(\"Rizia_Map_Music_Tense\"); " +
                       "if Variable[\"RiziaDLC.Situation_Diplomacy_Zille_ATAActive\"] == true then Variable[\"RiziaDLCSupport.Reports_Turn04_Zille_ATAProtest\"] = true end; " +
                       "if Variable[\"RiziaDLC.Situation_Diplomacy_Zille_SuOminaActive\"] == true then Variable[\"RiziaDLCSupport.Reports_Turn04_Zille_SOProtest\"] = true end;",
            nextNodes: new ConversationNodeSelector[] { new ConversationNodeArticyIDSelector(VanillaProtocolAndEnd) }));

        inj.Register();
        Core.Log.Msg("Registered the AN declaration-of-war branch (Turn 4, Konstantium).");
    }

    // [815] sets RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol and leads to End().
    private const string VanillaProtocolAndEnd = "0x0100006500009E70";

    private static ConversationNodeHook Gate(string articyId) =>
        new(new ConversationNodeArticyIDSelector(articyId), HookMode.ConditionGated);

    private static ConversationNodeSelector[] Next(params string[] names) =>
        names.Select(n => (ConversationNodeSelector)new ConversationNodeModdedNameSelector(P + n)).ToArray();

    private static ConversationNode Line(string name, string speaker, string text, ConversationNodeSelector[] next,
        bool conversant = false, string sequence = null, string condition = null) =>
        new(
            name: P + name,
            text: text,
            speakerSelector: new CharacterNameSelector(speaker),
            sequence: Wait(sequence),
            luaScript: conversant ? $"AddConversant(\"{speaker}\");" : null,   // vanilla Rizia adds conversants in Lua, quoted
            luaCondition: condition,
            nextNodes: next);

    // A node with its own sequence replaces the default one, which is what waits for the player to click on.
    // Vanilla ends such sequences with WaitForMessage(Continue); without it the next line shows at once.
    private static string Wait(string sequence) =>
        string.IsNullOrEmpty(sequence) ? sequence : sequence.TrimEnd().TrimEnd(';') + "; WaitForMessage(Continue);";

    // The conversation's default actor here is a technical actor, not the King, so choices must name him
    // explicitly or the game plays them as ordinary lines. Action choices use the italic narration actor.
    private static ConversationNode Choice(string name, string text, ConversationNodeSelector[] next, string menu = null, string script = null) =>
        new(name: P + name, text: text, menuText: menu, luaScript: script, nextNodes: next,
            speakerSelector: new CharacterNameSelector(menu != null && !text.StartsWith("\"") ? RomusAction : Romus));
}
