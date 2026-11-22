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
        d.SceneIf("ChartC", LucitaGone,
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

        // ---- one surprise per meeting at most
        d.Branch("Surprise", "SurPhoto", "SurDefector", "Hub");
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
            F(BlockDrillship) + T("RiziaDLCSupport.Journal_Turn04_Pales_NavalWithdrawal") + T(VarAureusRecalled) + Inc(Authority, -1) +
            Inc(NpPales, 1) + Inc(NpLespia, 1) + Inc(Ships, 1), "AureusPull2");
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
        d.SceneIf("ByeC", LucitaGone, "\"Your Majesty.\" Carlos saluted, gathered his papers and went out to the waiting staff car.", null, null, "End");
        d.Finish();
    }
}
