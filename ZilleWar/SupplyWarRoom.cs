namespace ZilleWar;

/// <summary>
/// The war room, once per war turn: Lucita Azaro at the chart of the Strait of Kreleck, or General Carlos Robles Azaro
/// if she has been arrested (vanilla Lucita_Arrested). Every question is asked once per meeting.
/// - The Aureus squadron (vanilla Diplomacy_Pales_Military_BlockDrillship): pull it north, or fight with half a fleet.
/// - The fleet into the strait (-1 Battle Ship, the convoys suffer), the submarines (every second patrol loses a boat,
///   and the option says so), unless Hegel was promised they would stay south.
/// - "How are they holding up over there?": the garrison from War_Zille_Strength, our fleet from Army_Unit_* totals.
/// - Surprises: Lespian patrol boats photographing our ports; a Wehzek captain defecting with his cargo of shells.
/// </summary>
internal static partial class SupplyWar
{
    private static void RegisterWarRoom()
    {
        var d = new Dlg(WarRoomConv, "ZilleWar.WarRoom.");
        string s = N(ZilleCampaign.VarStrength);
        string ships = N(Ships);
        string subs = N(Subs);
        string meetings = N(VarWarRoomMeetings);
        string strait = Is(VarLespiaStrait);
        string block = Is(BlockDrillship);

        d.Start(WarRoomConv, ResetAsked + Inc(VarWarRoomMeetings, 1), "Chart");
        d.Branch("Chart", "ChartL", "ChartC");
        d.SceneIf("ChartL", LucitaHere,
            "Lucita Azaro had spread a naval chart of the Strait of Kreleck across the war room table. Red pencil marks traced the " +
            "convoy routes from the Wehzek ports to Nanta and Isor.", "PlaySceneMusic(Security);", $"AddConversant(\"{Lucita}\");", "Brief");
        d.Scene("ChartC",
            "General Carlos Robles Azaro had the chart of the Strait of Kreleck pinned flat on the war room table. Since his cousin's " +
            "arrest he had been doing her work as well as his own, and it showed in the shadows under his eyes.",
            "PlaySceneMusic(Security);", $"AddConversant(\"{Carlos}\");", "Brief");

        d.Branch("Brief", "BriefFirst", "BriefGone", "BriefLespia", "BriefLater");
        d.StaffIf("BriefFirst", $"{meetings} == 1",
            "\"They run the convoys at night, Your Majesty. Four or five transports at a time, with a destroyer for escort. The " +
            "Admiralty wants your permission to go after them.\"",
            "\"The convoys come at night, Your Majesty. Four or five transports at a time, with a destroyer for escort. The Admiralty " +
            "is asking for orders, and with the Duchess gone, they are asking me.\"", "Surprise");
        d.StaffIf("BriefGone", Is(VarLespiaWithdrawn),
            "\"The Lespian ships are gone. The convoys came back the same week, as if someone had sent them a telegram.\"",
            "\"The Lespian ships have left the strait, Your Majesty, and the convoys came back within the week.\"", "Surprise");
        d.StaffIf("BriefLespia", strait,
            "\"The Lespians are stopping most of what crosses the strait. Most. The rest creeps along the Wehzek coast at night, and " +
            "that part is ours to deal with.\"",
            "\"The Lespians stop most of what crosses the strait, Your Majesty. What slips past them hugs the coast at night. That " +
            "part is ours.\"", "Surprise");
        d.Staff("BriefLater", "\"The convoys still come, Your Majesty. Fewer than before, but they come.\"",
            "\"The convoys still come, Your Majesty. Fewer than before, but they come.\"", "Surprise");

        // ---- one surprise per meeting at most. The Bludish companies and the Lespian cameras pay off what was set up earlier
        //      (Beatrice's "they bite", the photographs of our harbors).
        d.Branch("Surprise", "SurBluds", "SurRaid", "SurCaught", "SurPhoto", "SurDefectorTwo", "SurDefector", "Hub");

        string bluds = $"{N(Count(Lever.BffFighters))} > 0 and {N(VarBffFirstTurn)} > 0 and {N(VarBffFirstTurn)} < {TurnLua} and " +
                       $"{N(ZilleCampaign.VarOpsDone)} >= 1 and {V(VarBludsIncident)} == \"\"";
        string dropMercs = $"if {N(Mercs)} >= 2 then {V(Mercs)} = {N(Mercs)} - 2 else {V(Mercs)} = 0 end; ";
        d.StaffIf("SurBluds", bluds,
            "\"Before we start, Your Majesty. The Bludish companies. A Wehzek sniper killed two of them near a village in the hills above " +
            "Zille, and that night they went back to find him. They found the village instead. Eleven dead, and not one of them a " +
            "soldier. A Geopolitico stringer was in the next valley. He has photographs.\"",
            "\"Your Majesty, before anything else. The Bludish companies. A Wehzek sniper killed two of their men near a village above " +
            "Zille, and that night they went back for him. They found the village instead. Eleven dead, none of them soldiers. A " +
            "foreign reporter was close enough to take photographs.\"", "BludsMenu");
        d.Branch("BludsMenu", "BludsTrial", "BludsHome", "BludsKeep");
        d.Opt("BludsTrial", null, "\"Court-martial the men who did it. In Zille, in public, with that reporter in the front row.\"",
            "Loses the Bludish Companies",
            dropMercs + S(VarBludsIncident, "trial") + Inc(NpValgsland, 1) + Inc(NpRumburg, -1) + Inc(VarRumburgPatience, -1), "BludsTrial2");
        d.Staff("BludsTrial2",
            "\"The rest of them will desert before the verdict. Good riddance. Queen Beatrice will take it personally, of course. They " +
            "were her Bluds before they were ours.\"",
            "\"The rest of them will be gone before the verdict, Your Majesty. And the Queen of Rumburg will take it personally.\"", "Hub");
        d.Opt("BludsHome", null, "\"Put them on a train to the border tonight. Quietly.\"", "Loses the Bludish Companies",
            dropMercs + S(VarBludsIncident, "home"), "BludsHome2");
        d.Staff("BludsHome2",
            "\"Quietly. The photographs won't be quiet, Your Majesty, but the trains will be.\"",
            "\"The trains will be quiet, Your Majesty. I can't promise the same of the photographs.\"", "Hub");
        d.Opt("BludsKeep", null, "\"They stay at the front. Wehlen has shot more Bluds than that in a single winter.\"", "+1 Authority",
            S(VarBludsIncident, "kept") + Inc(Authority, 1) + Inc(NpValgsland, -2) + Inc(NpSordland, -1), "BludsKeep2");
        d.Staff("BludsKeep2",
            "\"As you wish. I'll keep them where the reporters aren't. That is the best I can do.\"",
            "\"As you wish, Your Majesty. I'll keep them away from the villages. That is the best I can promise.\"", "Hub");

        // the photographs of our harbors, one meeting after the Lespian cameras were reported
        string photoOpen = $"{meetings} >= 3 and {V(VarPhotoPayoff)} == \"\"";
        d.StaffIf("SurRaid", $"{photoOpen} and {Eq(VarPhotoBoats, "ignore")}",
            "\"Wehzek aircraft came over Fort Ales at first light. Six of them. They went straight for the fuel depot and the shell stores, " +
            "and they never once circled to look. They knew exactly where to go, Your Majesty. Somebody gave them a map, and I can think " +
            "of only one navy that has photographed every berth in that harbor.\"",
            "\"Wehzek aircraft hit Fort Ales at first light, Your Majesty. The fuel depot and the shell stores, and nothing else. They " +
            "knew where to go. Someone gave them the pictures, and only the Lespians have been taking them.\"",
            "RaidMenu");
        d.Do("RaidMenu", S(VarPhotoPayoff, "raid") + $"if {N("RiziaDLC.Resources_Military_Equipment")} >= 1 then " +
                         Inc("RiziaDLC.Resources_Military_Equipment", -1) + "end; ", "RaidExpel", "RaidSilent");
        d.Opt("RaidExpel", null, "\"Expel the Lespian naval attaché. Today.\"", "+1 Authority",
            S(VarPhotoPayoff, "expelled") + Inc(Authority, 1) + Inc(NpLespia, -2) + T(VarStraitThin), "RaidExpel2");
        d.Staff("RaidExpel2",
            "\"With pleasure. Alvarez will thin out his patrols to show us he's offended. Let him. At least the next photographs will be " +
            "taken from farther away.\"",
            "\"I'll see to it, Your Majesty. Expect fewer Lespian ships in the strait for a while. Alvarez will want us to notice.\"", "Hub");
        d.Opt("RaidSilent", null, "\"Say nothing. Let Alvarez think we haven't noticed.\"", null, S(VarPhotoPayoff, "silent"), "RaidSilent2");
        d.Staff("RaidSilent2",
            "\"Then I'll move the ships every night, and his cameras can photograph empty water.\"",
            "\"Then the ships move every night, Your Majesty, and the cameras get empty water.\"", "Hub");

        d.StaffIf("SurCaught", $"{photoOpen} and {Eq(VarPhotoBoats, "shadow")}",
            "\"Our destroyers followed one of the Lespian patrol boats to the Isor banks at dawn, as you asked. Politely. It met a Wehzek " +
            "trawler there and handed over a canvas bag. We stopped the trawler on her way home. The bag held forty rolls of film, every " +
            "one of them of our harbors.\"",
            "\"Our destroyers followed a Lespian patrol boat to the Isor banks at dawn, Your Majesty. It handed a canvas bag to a Wehzek " +
            "trawler. We stopped the trawler. Forty rolls of film, every one of them of our harbors.\"", "CaughtMenu");
        d.Branch("CaughtMenu", "CaughtReturn", "CaughtKeep");
        d.Opt("CaughtReturn", null, "\"Send the film back to Perla, with my compliments.\"", null,
            S(VarPhotoPayoff, "returned") + Inc(NpLespia, -1), "CaughtReturn2");
        d.Staff("CaughtReturn2", "\"With a ribbon on it, if you like. He'll understand the ribbon.\"",
            "\"It goes to Perla tonight, Your Majesty. He will understand the message.\"", "Hub");
        d.Opt("CaughtKeep", null, "\"Keep the film, and the trawler's crew. Find out what else they carry.\"", null,
            S(VarPhotoPayoff, "kept") + Talk.Weaken(1), "CaughtKeep2");
        d.Staff("CaughtKeep2",
            "\"The skipper is already talking. He knows the night rendezvous of every convoy out of Isor this month, and he would " +
            "rather tell us than a Wehzek court.\"",
            "\"The skipper is talking already, Your Majesty. He knows this month's convoy rendezvous off Isor.\"", "Hub");
        d.StaffIf("SurPhoto", $"{strait} and {meetings} >= 2 and {V(VarPhotoBoats)} == \"\"",
            "\"One more thing. The Lespian patrol boats in the strait carry cameras. Long lenses, pointed south. They have " +
            "photographed every ship we have at Fort Ales and Sallabes, twice.\"",
            "\"Something else, Your Majesty. The Lespian patrol boats carry long-lens cameras, and they point them south. Every ship " +
            "we have at Fort Ales and Sallabes has had its portrait taken.\"", "PhotoShadow", "PhotoIgnore");
        d.Opt("PhotoShadow", null, "\"Have our destroyers shadow them. Politely.\"", null, S(VarPhotoBoats, "shadow") + Inc(NpLespia, -1), "PhotoShadow2");
        d.Staff("PhotoShadow2", "\"Politely. Of course. I'll tell the captains to wave.\"",
            "\"Politely, Your Majesty. I'll tell the captains to keep their distance, and their guns covered.\"", "Hub");
        d.Opt("PhotoIgnore", null, "\"Let Alvarez have his pictures. We need his ships more.\"", null, S(VarPhotoBoats, "ignore"), "PhotoIgnore2");
        d.Staff("PhotoIgnore2", "\"As you wish. I'll make sure they photograph the right ships.\"",
            "\"As you wish, Your Majesty. I'll make sure they photograph the right ones.\"", "Hub");

        d.StaffIf("SurDefectorTwo", $"{s} <= 5 and {meetings} >= 2 and {Not(VarDefector)} and {Is(WorldVoices.VarDefector)}",
            "\"Good news, for a change. A second deserter, after the major at Mave Nortus. A Wehzek transport captain sailed into Sallabes " +
            "last night with his lights on and his flag down. He has asked for asylum, and he brought eleven hundred tons of artillery " +
            "shells with him. Wehzek officers seem to have started reading the same newspapers.\"",
            "\"Good news, Your Majesty, for once. Another one, after the major at Mave Nortus. A Wehzek transport captain sailed into " +
            "Sallabes last night with his flag down. He wants asylum, and he brought eleven hundred tons of artillery shells.\"", "Defector");
        d.StaffIf("SurDefector", $"{s} <= 5 and {meetings} >= 2 and {Not(VarDefector)}",
            "\"Good news, for a change. A Wehzek transport captain sailed into Sallabes last night with his lights on and his flag " +
            "down. He has asked for asylum, and he brought eleven hundred tons of artillery shells with him.\"",
            "\"Good news, Your Majesty, for once. A Wehzek transport captain sailed into Sallabes last night with his lights on and " +
            "his flag down. He wants asylum, and he brought eleven hundred tons of artillery shells.\"", "Defector");
        d.Opt("Defector", null, "\"Give him asylum. And a medal, if he wants one.\"", null, T(VarDefector) + Talk.Weaken(1), "Defector2");
        d.Staff("Defector2", "\"He wants a house by the sea and a Rizian passport. The medal he'll take as well, I think.\"",
            "\"He asked for a house by the sea, Your Majesty. I think he'll take the medal too.\"", "Hub");

        // ---- the hub
        d.Branch("Hub", Gate("AskAureus"), Gate("AskFleet"), Gate("AskSubs"), Gate("AskState"), "Bye");

        d.Opt("AskAureus", $"{block} and {Not(Q(1))}", "\"What about the squadron at Aureus?\"", null, T(Q(1)), "Aureus1");
        d.Staff("Aureus1",
            "\"Half the fleet is babysitting a Palesian drillship off Caleqabiz. Either we pull it north for the strait, or we fight " +
            "Wehlen with what's left. I would rather not fight anyone with what's left.\"",
            "\"Half the fleet is still off Caleqabiz, watching a Palesian drillship. Either we bring it north, Your Majesty, or the " +
            "strait gets what's left.\"", "AureusPull", "AureusKeep");
        d.Opt("AureusPull", null, "\"Bring the squadron north. Zille matters more than a drillship.\"", "-1 Authority",
            F(BlockDrillship) + T("RiziaDLCSupport.Journal_Turn04_Pales_NavalWithdrawal") + T(VarAureusRecalled) + T(WarCouncil.VarFleetRecalled) +
            Inc(Authority, -1) +
            Inc(NpPales, 1) + Inc(NpLespia, 1), "AureusPull2");
        d.Staff("AureusPull2",
            "\"Reinhart will call it a victory. Let him. The squadron will be off Zille by Thursday.\"",
            "\"The Duke will call it a victory, Your Majesty. Let him. The ships will be off Zille by Thursday.\"", "Hub");
        d.Opt("AureusKeep", null, "\"The squadron stays at Aureus. Pales must not think we've forgotten it.\"", null, null, "AureusKeep2");
        d.Staff("AureusKeep2", "\"Then the strait gets what's left. I'll make it count.\"",
            "\"Then the strait gets what's left, Your Majesty.\"", "Hub");

        d.Opt("AskFleet", $"{ships} > 0 and not {block} and {Not(Q(2))}", "\"Send the fleet into the strait.\"", null, T(Q(2)), "Fleet1");
        d.Branch("Fleet1", "FleetLast", "FleetMany");
        d.StaffIf("FleetLast", $"{ships} <= 1",
            "\"We have one ship left that can fight, Your Majesty. If we lose her, the Royal Navy is a museum.\"",
            "\"We have one ship left that can fight, Your Majesty. If she goes down, there is no navy.\"", "FleetYes", "FleetNo");
        d.Staff("FleetMany",
            "\"We'll sink transports, and probably one of their escorts. We'll also lose a ship of our own. The strait is narrow, and " +
            "their coastal guns are good.\"",
            "\"We'll sink transports, Your Majesty, and likely an escort. We'll also lose a ship. Their coastal guns are good.\"",
            "FleetYes", "FleetNo");
        d.Opt("FleetYes", null, "\"Do it.\"", "-1 Battle Ship", Inc(Count(Lever.RoyalNavy), 1) + Inc(Ships, -1) + Talk.Weaken(1), "Fleet2");
        d.Staff("Fleet2", "\"I'll send the order to Sallabes tonight. You'll have the butcher's bill by morning.\"",
            "\"The order goes to Sallabes tonight. I'll bring you the losses in the morning.\"", "Hub");
        d.Opt("FleetNo", null, "\"Not at that price.\"", null, null, "Hub");

        d.Opt("AskSubs", $"{subs} > 0 and {Not(VarSubsGrounded)} and {Not(Q(3))}", "\"What about the submarines?\"", null, T(Q(3)), "Subs1");
        d.Branch("Subs1", "SubsLucky", "SubsFresh");
        d.StaffIf("SubsLucky", $"{N(VarSubPatrols)} >= 1",
            "\"The boats were lucky last time. The Wehzeks have put destroyers on the convoys since. If they go again, we'll lose one.\"",
            "\"The boats were lucky last time, Your Majesty. The Wehzeks have escorts on the convoys now. If they sail again, we'll lose one.\"",
            Gate("SubsYesLoss"), "SubsNo");
        d.Staff("SubsFresh",
            "\"Quieter than the fleet, and cheaper in lives. They won't stop a convoy outright, but every captain who crosses will " +
            "spend the night staring at the water.\"",
            "\"Quieter than the fleet, and cheaper in lives, Your Majesty. Every Wehzek captain who crosses will spend the night " +
            "staring at the water.\"", Gate("SubsYes"), "SubsNo");
        d.Opt("SubsYes", $"{N(VarSubPatrols)} < 1", "\"Send the submarines.\"", null,
            Inc(Count(Lever.Submarines), 1) + Inc(VarSubPatrols, 1) + Talk.Weaken(1), "Subs2");
        d.Opt("SubsYesLoss", $"{N(VarSubPatrols)} >= 1", "\"Send them anyway.\"", "-1 Submarine",
            Inc(Count(Lever.Submarines), 1) + Inc(Subs, -1) + $"{V(VarSubPatrols)} = 0; " + Talk.Weaken(1), "Subs2");
        d.Staff("Subs2", "\"They'll sail on the evening tide.\"", "\"They sail on the evening tide, Your Majesty.\"", "Hub");
        d.Opt("SubsNo", null, "\"Keep them in port for now.\"", null, null, "Hub");

        d.Opt("AskState", Not(Q(4)), "\"How are they holding up over there?\"", null, T(Q(4)), "State");
        d.Branch("State", "State1", "State2", "State3", "State4");
        d.StaffIf("State1", $"{s} >= 8",
            "\"Well, I'm afraid. The convoys come every night, and the Zille garrison is still growing.\"",
            "\"Well, Your Majesty. The convoys come every night, and the garrison is still growing.\"", "Fleet");
        d.StaffIf("State2", $"{s} >= 6",
            "\"They're holding. The convoys still come, but the garrison has stopped growing.\"",
            "\"They're holding, Your Majesty. The garrison has stopped growing, but it hasn't started shrinking.\"", "Fleet");
        d.StaffIf("State3", $"{s} >= 4",
            "\"They're feeling it. Prisoners talk about rationed shells, and tanks parked for want of fuel.\"",
            "\"They're feeling it, Your Majesty. Prisoners talk about rationed shells, and tanks standing idle for want of fuel.\"", "Fleet");
        d.Staff("State4",
            "\"They're starving, Your Majesty. Deserters come over every night now, most of them without rifles.\"",
            "\"They're starving, Your Majesty. Deserters come over every night, most of them without rifles.\"", "Fleet");
        d.Branch("Fleet", "FleetNone", "FleetFew", "FleetGood");
        d.StaffIf("FleetNone", $"{ships} + {subs} <= 0",
            "\"As for us, we have nothing left at sea that I would send into the strait. Whatever happens there now, Lespia or the " +
            "weather will decide it.\"",
            "\"As for us, Your Majesty, we have nothing left at sea that could fight in the strait.\"", "Hub");
        d.StaffIf("FleetFew", $"{ships} + {subs} <= 2",
            "\"As for us, what's left of the fleet is tired and short of spares. I can send it once more, perhaps twice.\"",
            "\"As for us, what's left of the fleet is worn out, Your Majesty. Once more, perhaps twice.\"", "Hub");
        d.Staff("FleetGood",
            "\"As for us, the fleet is in good order, and the crews want another go at them.\"",
            "\"As for us, the fleet is in good order, Your Majesty, and the crews are eager.\"", "Hub");

        d.Opt("Bye", null, "\"That will be all.\"", null, null, "Bye2");
        d.Branch("Bye2", "ByeL", "ByeC");
        d.SceneIf("ByeL", LucitaHere, "\"Your Majesty.\" Lucita rolled up the chart and was gone before I had sat down.", null, null, "End");
        d.Scene("ByeC", "\"Your Majesty.\" Carlos saluted, gathered his papers and went out to the waiting staff car.", null, null, "End");
        d.Finish();
    }
}
