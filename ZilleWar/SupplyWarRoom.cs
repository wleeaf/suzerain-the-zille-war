using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// The naval side of the War Council (no separate war-room meeting any more): built into the council's own
/// conversation by <see cref="WarCouncil"/>, spoken by Lucita Azaro or, when she is not on the council, by General
/// Carlos Robles Azaro.
/// - <see cref="CouncilSurprise"/>: at most one disruption per council, right after the intelligence read-out: the
///   Bludish companies' massacre, the Lespian cameras and their payoff (the Fort Ales raid or the film caught on the
///   Isor banks), a Wehzek captain defecting with his shells.
/// - <see cref="CouncilNavy"/>: the strait (closed by Lespia, thinly watched, or open), the state of our fleet, and the
///   King's naval orders. They are standing orders: the first council sets the opening posture (the fleet into the strait,
///   -1 Battle Ship; off the coast against the batteries; or in port), and every later council hears where the fleet is
///   and keeps or changes it (another week in the strait costs another ship). Then the submarines: sent out, kept out or
///   called home (every second patrol loses a boat, and the option says so) unless Hegel was promised they would stay
///   south. "What good is our fleet if Lespia is already closing the strait?" can be asked once per war.
/// The orders are read by <see cref="WarCouncil.CurrentOrders"/> and shape the map in ZilleOperation.
/// </summary>
internal static partial class SupplyWar
{
    /// <summary>Lua run once per council (before any surprise is chosen): counts the councils that reached the strait.</summary>
    internal static string CouncilMeetingScript => Inc(VarWarRoomMeetings, 1);

    /// <summary>
    /// Extra Wehzek units brought across the Strait of Kreleck before the operation: two while nobody watches the
    /// strait, one while the Lespian patrols are thin or our submarines alone are out, none while Lespia closes it or
    /// our fleet is hunting there.
    /// </summary>
    internal static int StraitReinforcements(string navy, bool subsOut)
    {
        if (navy == "Strait")
        {
            return 0;
        }

        int r = Variables.GetBool(VarLespiaStrait) ? (Variables.GetBool(VarStraitThin) ? 1 : 0) : 2;
        return Math.Max(0, r - (subsOut ? 1 : 0));
    }

    /// <summary>The war room is no longer offered; this keeps an old save that still has its fragment completable.</summary>
    private static void RegisterWarRoom()
    {
        var d = new Dlg(WarRoomConv, "ZilleWar.WarRoom.");
        d.Start(WarRoomConv, null, "Chart");
        d.Scene("Chart",
            "The chart of the Strait of Kreleck was still pinned to the table. The orders had gone out from the Council Chambers.", "PlaySceneMusic(Security);", null, "End");
        d.Finish();
    }

    /// <summary>A line by Lucita, or by Carlos when she is not on the council; only when cond holds (null: always).</summary>
    private static void Staff(Talk t, string lucitaHere, string name, string cond, string lucita, string carlos, params string[] next)
    {
        t.Logic(name, new[] { name + "_L", name + "_C" }, condition: cond);
        t.Line(name + "_L", Lucita, lucita, next, condition: lucitaHere);
        t.Line(name + "_C", Carlos, carlos ?? lucita, next, condition: $"not ({lucitaHere})");
    }

    /// <summary>At most one disruption per council, then on to <paramref name="exit"/>.</summary>
    internal static void CouncilSurprise(Talk t, string entry, string exit, string lucitaHere)
    {
        void Say(string name, string cond, string lucita, string carlos, params string[] next) =>
            Staff(t, lucitaHere, name, cond, lucita, carlos, next);

        string s = N(ZilleCampaign.VarStrength);
        string meetings = N(VarWarRoomMeetings);
        string strait = Is(VarLespiaStrait);

        t.Logic(entry, new[] { "WRBluds", "WRRaid", "WRCaught", "WRPhoto", "WRDefectorTwo", "WRDefector", exit });

        // The Bludish companies (Beatrice's "they bite").
        string bluds = $"{N(Count(Lever.BffFighters))} > 0 and {N(VarBffFirstTurn)} > 0 and {N(VarBffFirstTurn)} < {TurnLua} and " +
                       $"{N(ZilleCampaign.VarOpsDone)} >= 1 and ({V(VarBludsIncident)} == nil or {V(VarBludsIncident)} == \"\")";
        string dropMercs = $"if {N(Mercs)} >= 2 then {V(Mercs)} = {N(Mercs)} - 2 else {V(Mercs)} = 0 end; ";
        Say("WRBluds", bluds,
            "\"Before the briefing, Your Majesty. The Bludish companies. A Wehzek sniper killed two of them near a village above Zille, and " +
            "that night they went back to find him.\"",
            "\"Before the briefing, Your Majesty. The Bludish companies. A Wehzek sniper killed two of their men near a village above " +
            "Zille, and that night they went back for him.\"", "WRBluds2");
        Say("WRBluds2", null,
            "\"They found the village instead. Eleven dead, and not one of them a soldier. A Geopolitico stringer was in the next valley, " +
            "and he has photographs.\"",
            "\"They found the village instead. Eleven dead, none of them soldiers. A foreign reporter was close enough to take photographs.\"",
            "WRBludsTrial", "WRBludsHome", "WRBludsKeep");
        t.Choice("WRBludsTrial", "\"Court-martial the men who did it. In Palas Rezna, in public, with that reporter in the front row.\"", new[] { "WRBludsTrial2" },
            cost: "Loses the Bludish Companies",
            script: dropMercs + S(VarBludsIncident, "trial") + Inc(NpRumburg, -1) + Inc(VarRumburgPatience, -1));
        Say("WRBludsTrial2", null,
            "\"The rest of them will desert before the verdict. Good riddance. Queen Beatrice will take it personally, of course. They were " +
            "her Bluds before they were ours.\"",
            "\"The rest of them will be gone before the verdict, Your Majesty. And the Queen of Rumburg will take it personally.\"", exit);
        t.Choice("WRBludsHome", "\"Put them on a train to the border tonight. Quietly.\"", new[] { "WRBludsHome2" },
            cost: "Loses the Bludish Companies", script: dropMercs + S(VarBludsIncident, "home"));
        Say("WRBludsHome2", null, "\"Quietly. The photographs won't be quiet, Your Majesty, but the trains will be.\"",
            "\"The trains will be quiet, Your Majesty. I can't promise the same of the photographs.\"", exit);
        t.Choice("WRBludsKeep", "\"They stay at the front. Wehlen has shot more Bluds than that in a single winter.\"", new[] { "WRBludsKeep2" },
            cost: "+1 Authority", script: S(VarBludsIncident, "kept") + Inc(Authority, 1) + Inc(NpValgsland, -2) + Inc(NpSordland, -1));
        Say("WRBludsKeep2", null, "\"As you wish. I'll keep them where the reporters aren't. That is the best I can do.\"",
            "\"As you wish, Your Majesty. I'll keep them away from the villages. That is the best I can promise.\"", exit);

        // The photographs of our harbors pay off one council after the Lespian cameras were reported.
        string photoOpen = $"{strait} and {meetings} >= 3 and ({V(VarPhotoPayoff)} == nil or {V(VarPhotoPayoff)} == \"\")";
        Say("WRRaid", $"{photoOpen} and {Eq(VarPhotoBoats, "ignore")}",
            "\"Wehzek aircraft came over Fort Ales at first light. Six of them, straight for the fuel depot and the shell stores. They never " +
            "once circled to look.\"",
            "\"Wehzek aircraft hit Fort Ales at first light, Your Majesty. The fuel depot and the shell stores, and nothing else.\"", "WRRaid2");
        Say("WRRaid2", null,
            "\"Somebody gave them a map, Your Majesty. And only one navy has photographed every berth in that harbor.\"",
            "\"They knew exactly where to go. Someone gave them the pictures, and only the Lespians have been taking them.\"", "WRRaidMenu");
        t.Logic("WRRaidMenu", new[] { "WRRaidExpel", "WRRaidSilent" },
            script: S(VarPhotoPayoff, "raid") + $"if {N("RiziaDLC.Resources_Military_Equipment")} >= 1 then " +
                    Inc("RiziaDLC.Resources_Military_Equipment", -1) + "end; ");
        t.Choice("WRRaidExpel", "\"Expel the Lespian naval attaché. Today.\"", new[] { "WRRaidExpel2" }, cost: "+1 Authority",
            script: S(VarPhotoPayoff, "expelled") + Inc(Authority, 1) + Inc(NpLespia, -2) + T(VarStraitThin));
        Say("WRRaidExpel2", null,
            "\"With pleasure. Alvarez will thin out his patrols to show us he's offended. Let him. The next photographs will be taken from " +
            "farther away.\"",
            "\"I'll see to it, Your Majesty. Expect fewer Lespian ships in the strait for a while. Alvarez will want us to notice.\"", exit);
        t.Choice("WRRaidSilent", "\"Say nothing. Let Alvarez think we haven't noticed.\"", new[] { "WRRaidSilent2" },
            script: S(VarPhotoPayoff, "silent"));
        Say("WRRaidSilent2", null, "\"Then I'll move the ships every night, and his cameras can photograph empty water.\"",
            "\"Then the ships move every night, Your Majesty, and the cameras get empty water.\"", exit);

        Say("WRCaught", $"{photoOpen} and {Eq(VarPhotoBoats, "shadow")}",
            "\"Our destroyers followed one of the Lespian patrol boats to the Isor banks at dawn, as you asked. Politely. It met a Wehzek " +
            "trawler there and handed over a canvas bag.\"",
            "\"Our destroyers followed a Lespian patrol boat to the Isor banks at dawn, Your Majesty. It handed a canvas bag to a Wehzek " +
            "trawler.\"", "WRCaught2");
        Say("WRCaught2", null,
            "\"We stopped the trawler on her way home. The bag held forty rolls of film, every one of them of our harbors.\"",
            "\"We stopped the trawler. Forty rolls of film, every one of them of our harbors.\"", "WRCaughtReturn", "WRCaughtKeep");
        t.Choice("WRCaughtReturn", "\"Send the film back to Perla, with my compliments.\"", new[] { "WRCaughtReturn2" },
            script: S(VarPhotoPayoff, "returned") + Inc(NpLespia, -1));
        Say("WRCaughtReturn2", null, "\"With a ribbon on it, if you like. He'll understand the ribbon.\"",
            "\"It goes to Perla tonight, Your Majesty. He will understand the message.\"", exit);
        t.Choice("WRCaughtKeep", "\"Keep the film, and the trawler's crew. Find out what else they carry.\"", new[] { "WRCaughtKeep2" },
            script: S(VarPhotoPayoff, "kept") + Talk.Weaken(1));
        Say("WRCaughtKeep2", null,
            "\"The skipper is already talking. He knows the night rendezvous of every convoy out of Isor this month, and he would rather tell " +
            "us than a Wehzek court.\"",
            "\"The skipper is talking already, Your Majesty. He knows this month's convoy rendezvous off Isor.\"", exit);

        Say("WRPhoto", $"{strait} and {meetings} >= 2 and ({V(VarPhotoBoats)} == nil or {V(VarPhotoBoats)} == \"\")",
            "\"One more thing. The Lespian patrol boats in the strait carry cameras. Long lenses, pointed south. They have photographed every " +
            "ship we have at Fort Ales and Sallabes, twice.\"",
            "\"Something else, Your Majesty. The Lespian patrol boats carry long-lens cameras, and they point them south. Every ship we have " +
            "at Fort Ales and Sallabes has had its portrait taken.\"", "WRPhotoShadow", "WRPhotoIgnore");
        t.Choice("WRPhotoShadow", "\"Have our destroyers shadow them. Politely.\"", new[] { "WRPhotoShadow2" },
            script: S(VarPhotoBoats, "shadow") + Inc(NpLespia, -1));
        Say("WRPhotoShadow2", null, "\"Politely. Of course. I'll tell the captains to wave.\"",
            "\"Politely, Your Majesty. I'll tell the captains to keep their distance, and their guns covered.\"", exit);
        t.Choice("WRPhotoIgnore", "\"Let Alvarez have his pictures. We need his ships more.\"", new[] { "WRPhotoIgnore2" },
            script: S(VarPhotoBoats, "ignore"));
        Say("WRPhotoIgnore2", null, "\"As you wish. I'll make sure they photograph the right ships.\"",
            "\"As you wish, Your Majesty. I'll make sure they photograph the right ones.\"", exit);

        Say("WRDefectorTwo", $"{s} <= 5 and {meetings} >= 2 and {Not(VarDefector)} and {Is(WorldVoices.VarDefector)}",
            "\"Good news, for a change. Another deserter, after the major at Mave Nortus. A Wehzek transport captain sailed into Sallabes " +
            "last night with his lights on and his flag down.\"",
            "\"Good news, Your Majesty, for once. Another one, after the major at Mave Nortus. A Wehzek transport captain sailed into " +
            "Sallabes last night with his flag down.\"", "WRDefector2");
        Say("WRDefector", $"{s} <= 5 and {meetings} >= 2 and {Not(VarDefector)}",
            "\"Good news, for a change. A Wehzek transport captain sailed into Sallabes last night with his lights on and his flag down.\"",
            "\"Good news, Your Majesty, for once. A Wehzek transport captain sailed into Sallabes last night with his flag down.\"", "WRDefector2");
        Say("WRDefector2", null,
            "\"He has asked for asylum, and he brought eleven hundred tons of artillery shells with him.\"",
            "\"He wants asylum, and he brought eleven hundred tons of artillery shells.\"", "WRDefectorYes");
        t.Choice("WRDefectorYes", "\"Give him asylum. And a medal, if he wants one.\"", new[] { "WRDefector3" },
            script: T(VarDefector) + Talk.Weaken(1));
        Say("WRDefector3", null, "\"He wants a house by the sea and a Rizian passport. The medal he'll take as well, I think.\"",
            "\"He asked for a house by the sea, Your Majesty. I think he'll take the medal too.\"", exit);
    }

    /// <summary>
    /// The strait and the King's naval orders, then on to <paramref name="exit"/>. Sets the standing orders
    /// <see cref="WarCouncil.VarNavy"/> ("Strait" | "Coast" | "Home") and <see cref="WarCouncil.VarSubsOut"/>; <paramref name="later"/>
    /// (Lua) holds at the progress meetings, where the current orders are heard first and kept or changed.
    /// </summary>
    internal static void CouncilNavy(Talk t, string entry, string exit, string lucitaHere, string coastal, string harbor, string ports,
        string landing, string askedWhy, string later)
    {
        void Say(string name, string cond, string lucita, string carlos, params string[] next) =>
            Staff(t, lucitaHere, name, cond, lucita, carlos, next);

        string ships = N(Ships), subs = N(Subs);
        string closed = Is(VarLespiaStrait);
        string open = Not(VarLespiaStrait);
        string navy = WarCouncil.VarNavy;
        string inStrait = Eq(navy, "Strait"), offCoast = Eq(navy, "Coast");
        string inPort = $"not {inStrait} and not {offCoast}";
        string subsOut = Is(WarCouncil.VarSubsOut);

        // ---- where the strait stands
        // A month after the last council, the strait is told as what changed (or didn't), not word for word again.
        string again = $"{N(VarWarRoomMeetings)} >= 3";
        // The convoys land at the first port Wehlen still holds: Nanta, then Isor, then Zille (as PhaseResume.Reinforce lands them).
        string nantaOurs = Is(ZilleOperation.CapturedVar("Nanta")), isorOurs = Is(ZilleOperation.CapturedVar("Isor"));
        var landings = new[]
        {
            ("Nanta", $"not {nantaOurs}"), ("Isor", $"{nantaOurs} and not {isorOurs}"), ("Zille", $"{nantaOurs} and {isorOurs}"),
        };
        string[] Each(string name) => landings.Select(l => name + l.Item1).ToArray();
        t.Logic(entry, new[] { "WRGone", "WRThin", "WRClosedAgain", "WRClosed" }.Concat(Each("WROpenFleet")).Concat(Each("WROpenAgain"))
            .Concat(Each("WROpen")).ToArray());
        Say("WRClosedAgain", $"{closed} and {again}",
            "\"The strait, then. Another month of Lespian inspections. Their captains get slower every week, and the Wehzek night runners " +
            "bolder.\"",
            "\"The strait, Your Majesty. Another month of Lespian inspections, slower every week, and the Wehzek night runners bolder.\"",
            "WRState");
        foreach ((string port, string lands) in landings)
        {
            Say("WROpenFleet" + port, $"{open} and {inStrait} and {lands}",
                $"\"Now the strait. Nobody guards it but us, Your Majesty. What crosses to {port} tonight crosses under our guns, and it costs them.\"",
                $"\"Now the strait, Your Majesty. The Lespians are not there, and we are. What crosses to {port} tonight crosses under our guns.\"",
                "WRState");
            Say("WROpenAgain" + port, $"{open} and not {inStrait} and {again} and {lands}",
                $"\"The strait is still open, Your Majesty. Every night we leave it alone, {port} gets another trainload of men off the ships.\"",
                $"\"The strait is still open, Your Majesty. Every night we leave it alone, {port} takes in another battalion.\"", "WROpen2");
            Say("WROpen" + port, $"not {inStrait} and {lands}",
                $"\"Now the strait. Nobody is closing it, Your Majesty. Not Lespia, not us. Wehlen brings men and shells across to {port} every night.\"",
                $"\"Now the strait, Your Majesty. Nobody is closing it. Wehlen brings men and shells across to {port} every night.\"", "WROpen2");
        }

        Say("WRGone", $"{open} and not {inStrait} and {Is(VarLespiaWithdrawn)} and {N(Count(Lever.Lespia))} > 0",
            "\"Now the strait. The Lespian ships are gone, and the convoys came back the same week. As if someone had sent them a telegram.\"",
            "\"Now the strait, Your Majesty. The Lespian ships have left, and the convoys came back within the week.\"", "WROpen2");
        Say("WRThin", $"{closed} and {Is(VarStraitThin)}",
            "\"Now the strait. The Lespians are still out there, but fewer of them since Perla took offense. More gets through every week.\"",
            "\"Now the strait, Your Majesty. The Lespian patrols have thinned out, and more gets through every week.\"", "WRState");
        Say("WRClosed", closed,
            "\"Now the strait. The Lespians stop and search most of what crosses it. Most. The rest runs at night along the Wehzek coast, " +
            "with a destroyer for escort.\"",
            "\"Now the strait, Your Majesty. The Lespians stop most of what crosses. What slips past them runs at night along the Wehzek " +
            "coast, under escort.\"", "WRState");
        Say("WROpen2", null,
            "\"If our ships stay in port, we will meet more of them in Zille than we counted today.\"",
            "\"If our ships stay in port, we will find more of them in Zille than we counted today.\"", "WRState");

        // ---- what we have at sea
        t.Logic("WRState", new[] { "WRNone", "WRSubsOnly", "WRLast", "WRFew", "WRGoodAgain", "WRGoodLater", "WRGood" });
        Say("WRGoodAgain", $"{later} and {again}",
            "\"The fleet has come through another month in fair shape, and the crews keep asking when they get to finish the job.\"",
            "\"The fleet has come through another month in fair shape, Your Majesty.\"", "WROrders");
        Say("WRNone", $"{ships} + {subs} <= 0",
            "\"And we have nothing left at sea that could fight there. Whatever happens in the strait now, Lespia or the weather will decide it.\"",
            "\"And we have nothing left at sea that could fight there, Your Majesty.\"", exit);
        Say("WRSubsOnly", $"{ships} <= 0",
            "\"Our surface fleet is gone. What we have left is under the water.\"",
            "\"The surface fleet is gone, Your Majesty. What we have left is under the water.\"", "WRSubs");
        Say("WRLast", $"{ships} <= 1",
            "\"We have one ship left that can fight. If we lose her, the Royal Navy is a museum.\"",
            "\"We have one ship left that can fight, Your Majesty. If she goes down, there is no navy.\"", "WROrders");
        Say("WRFew", $"{ships} + {subs} <= 2",
            "\"What's left of the fleet is tired and short of spares. I can send it once more, perhaps twice.\"",
            "\"What's left of the fleet is worn out, Your Majesty. Once more, perhaps twice.\"", "WROrders");
        Say("WRGoodLater", later, "\"The fleet is in good order, and the crews want another go at them.\"",
            "\"The fleet is in good order, Your Majesty, and the crews are eager.\"", "WROrders");
        Say("WRGood", null,
            "\"The fleet is in good order, and the crews want another go at them. Where do you want it?\"",
            "\"The fleet is in good order, Your Majesty, and the crews are eager. Where do you want it?\"", "WROrders");

        // ---- later councils: the standing orders, to keep or change
        t.Logic("WROrders", new[] { "WROrdStraitAgain", "WROrdStrait", "WROrdCoastInlandNanta", "WROrdCoastInlandIsor", "WROrdCoastInland", "WROrdCoastAgain", "WROrdCoast", "WROrdHomeAgain",
            "WROrdHome", "WRMenu" });
        Say("WROrdStraitAgain", $"{later} and {inStrait} and {again}",
            "\"The fleet has been hunting in the strait for two months now. It's thinner for it, and so are their convoys. One more month " +
            "out there, or do we bring it home?\"",
            "\"The fleet has been in the strait two months now, Your Majesty. Thinner for it, and so are their convoys. One more month?\"",
            "WRMenu");
        Say("WROrdCoastAgain", $"{later} and {offCoast} and ({coastal}) and {again}",
            "\"The destroyers are still off the coast. Their batteries have learned to fire once and move. Do they stay?\"",
            "\"The destroyers are still off the coast, Your Majesty. Their batteries fire once and move now. Do they stay?\"", "WRMenu");
        Say("WROrdHomeAgain", $"{later} and {again}",
            "\"The fleet is still at Sallabes. The crews have started painting things that don't need painting. Do you want it out this time?\"",
            "\"The fleet is still at Sallabes, Your Majesty. Do you want it out this time?\"", "WRMenu");
        Say("WROrdStrait", $"{later} and {inStrait}",
            "\"It's still in the strait on your orders, hunting their convoys. Every month out there costs us a ship. Do we keep it there?\"",
            "\"It's still in the strait on your orders, Your Majesty. Every month out there costs us a ship. Do we keep it there?\"", "WRMenu");
        foreach ((string port, string waits) in new[] { ("Nanta", $"not {nantaOurs}"), ("Isor", $"{nantaOurs} and not {isorOurs}") })
        {
            Say("WROrdCoastInland" + port, $"{later} and {offCoast} and not ({coastal}) and {waits}",
                $"\"The destroyers are still off the coast, but Auremina is far out of reach of their guns. They can wait there for {port}, or go " +
                "somewhere more useful.\"",
                $"\"The destroyers are still off the coast, Your Majesty, but Auremina is out of their reach. They can wait for {port}, or go elsewhere.\"",
                "WRMenu");
        }

        Say("WROrdCoastInland", $"{later} and {offCoast} and not ({coastal})",
            "\"The destroyers are still off the coast, but Auremina is far out of reach of their guns. They would be more use somewhere else.\"",
            "\"The destroyers are still off the coast, Your Majesty, but Auremina is out of their reach. They would be more use elsewhere.\"",
            "WRMenu");
        Say("WROrdCoast", $"{later} and {offCoast}",
            "\"The destroyers are still off the coast on your orders. Their batteries have learned to keep their heads down.\"",
            "\"The destroyers are still off the coast on your orders, Your Majesty. Their batteries keep their heads down.\"", "WRMenu");
        Say("WROrdHome", later,
            "\"The fleet is still in port at Sallabes, as you ordered. Do you want it out?\"",
            "\"The fleet is still in port at Sallabes, Your Majesty. Do you want it out?\"", "WRMenu");

        // ---- the King's naval order
        t.Logic("WRMenu", new[] { "WRWhy", "WRKeepStrait", "WRSendOpen", "WRSendClosed", "WRKeepCoast", "WRCoast", "WRKeepHome", "WRBringHome", "WRHome" });
        t.Choice("WRWhy", "\"If the Lespians are already closing the strait, what good is our fleet up there?\"", new[] { "WRWhy1" },
            condition: $"{closed} and {Not(askedWhy)}", script: T(askedWhy));
        Say("WRWhy1", null,
            "\"The Lespians stop and search, Your Majesty. They don't shoot. So the Wehzeks run their transports inside their own waters at " +
            "night, with a destroyer alongside, and Perla's captains let them pass.\"",
            "\"The Lespians stop and search, Your Majesty. They don't shoot. So the Wehzeks run their transports at night inside their own " +
            "waters, under escort, and the Lespian captains let them pass.\"", "WRWhy2");
        Say("WRWhy2", null, "\"Our captains won't. We can hunt those escorts, and whatever they're escorting.\"",
            "\"Our captains won't. We can hunt those escorts, and whatever they're escorting.\"", "WRWhy3");
        t.Logic("WRWhy3", new[] { "WRWhyHarbor", "WRWhyPortsIsor", "WRWhyPorts", "WRWhyHome" });
        Say("WRWhyHarbor", harbor,
            "\"Or the fleet stands off Zille and shells the coastal batteries, so the army doesn't have to take them with bayonets.\"",
            null, "WRWhyHome");
        // Isor has no coastal battery: with Nanta ours, the fleet can only keep the transports off its quays.
        Say("WRWhyPortsIsor", $"({ports}) and {nantaOurs}",
            "\"Or the fleet stands off Isor and keeps their transports away from the quays, so the garrison fights on what it already has.\"",
            null, "WRWhyHome");
        Say("WRWhyPorts", ports,
            "\"Or the fleet stands off Nanta and shells the coastal batteries, so the army doesn't have to take them with bayonets.\"",
            null, "WRWhyHome");
        Say("WRWhyHome", null,
            "\"Or we keep it home and let Alvarez do the work for free. He'll send us the bill either way.\"",
            "\"Or we keep it in port and let Prime Minister Alvarez do the work. He will send us the bill either way.\"", "WRMenu");

        string sendScript = S(navy, "Strait") + Inc(Count(Lever.RoyalNavy), 1) + Inc(Ships, -1) + Talk.Weaken(1);
        t.Choice("WRKeepStrait", "\"Keep it in the strait.\"", new[] { "WRKept" },
            condition: $"{later} and {inStrait} and {ships} >= 1", cost: "-1 Battle Ship", script: sendScript);
        Say("WRKept", null, "\"Then it stays. We'll go on sinking what crosses, and go on paying for it.\"",
            "\"Then it stays, Your Majesty. We'll go on sinking what crosses, and paying for it.\"", "WRSentLanding");
        t.Choice("WRSendOpen", "\"Send the fleet into the strait. Sink their convoys.\"", new[] { "WRSent" },
            condition: $"{open} and {ships} >= 1 and not {inStrait}", cost: "-1 Battle Ship", script: sendScript);
        t.Choice("WRSendClosed", "\"Send the fleet after their escorts.\"", new[] { "WRSent" },
            condition: $"{closed} and {ships} >= 1 and not {inStrait}", cost: "-1 Battle Ship", script: sendScript);
        Say("WRSent", null,
            "\"The order goes to Sallabes tonight. We'll sink transports, and likely an escort. We'll lose a ship of our own too. Their coastal " +
            "guns are good.\"",
            "\"The order goes to Sallabes tonight, Your Majesty. We'll sink transports, and we'll lose a ship. Their coastal guns are good.\"",
            "WRSentLanding");
        t.Logic("WRSentLanding", new[] { "WRSentLanding2", "WRSubs" });
        Say("WRSentLanding2", landing,
            "\"And the landing goes in without the fleet behind it. Their batteries will still be firing.\"",
            "\"And the landing goes in without the fleet behind it, Your Majesty. Their batteries will still be firing.\"", "WRSubs");

        t.Choice("WRKeepCoast", "\"Keep the destroyers off the coast.\"", new[] { "WRCoastKept" },
            condition: $"{later} and {offCoast} and {ships} >= 2", script: S(navy, "Coast"));
        Say("WRCoastKept", null, "\"They stay on station. The batteries won't enjoy it.\"",
            "\"They stay on station, Your Majesty.\"", "WRCoast3");
        t.Choice("WRCoast", "\"Put the fleet off the coast. Their batteries are the target.\"", new[] { "WRCoast2" },
            condition: $"{coastal} and {ships} >= 2 and not {offCoast}", script: S(navy, "Coast"));
        Say("WRCoast2", null,
            "\"The destroyers will be on station by dawn. Once those guns are quiet, our boats can come into the bay.\"",
            "\"The destroyers will be on station by dawn, Your Majesty. Once those guns are quiet, the bay is ours.\"", "WRCoast3");
        t.Logic("WRCoast3", new[] { "WRCoastOpen", "WRSubs" });
        Say("WRCoastOpen", open, "\"The convoys will have the strait to themselves while we're at it.\"",
            "\"The convoys will have the strait to themselves while we're at it, Your Majesty.\"", "WRSubs");

        t.Choice("WRKeepHome", "\"Leave it in port.\"", new[] { "WRHome2" }, condition: $"{later} and {inPort}", script: S(navy, "Home"));
        t.Choice("WRBringHome", "\"Bring the fleet back to Sallabes.\"", new[] { "WRHome2" }, condition: $"{later} and not ({inPort})",
            script: S(navy, "Home"));
        t.Choice("WRHome", "\"Keep the fleet in port. We may need it later.\"", new[] { "WRHome2" }, condition: $"not ({later})", script: S(navy, "Home"));
        t.Logic("WRHome2", new[] { "WRHomeOpen", "WRHomeClosed" });
        Say("WRHomeOpen", open, "\"As you wish. Then expect fresh Wehzek battalions at the front by the time we get there.\"",
            "\"As you wish, Your Majesty. Then expect fresh Wehzek battalions at the front.\"", "WRSubs");
        Say("WRHomeClosed", null, "\"Then the strait is Perla's business. Let's hope he keeps minding it.\"",
            "\"Then the strait is the Lespians' business, Your Majesty. Let's hope they keep minding it.\"", "WRSubs");

        // ---- the submarines (not while Hegel has their word that they stay south)
        string patrol = T(WarCouncil.VarSubsOut) + Inc(Count(Lever.Submarines), 1) + Inc(VarSubPatrols, 1) + Talk.Weaken(1);
        string patrolLoss = T(WarCouncil.VarSubsOut) + Inc(Count(Lever.Submarines), 1) + Inc(Subs, -1) + $"{V(VarSubPatrols)} = 0; " + Talk.Weaken(1);
        t.Logic("WRSubs", new[] { "WRSubsOutLucky", "WRSubsOut", "WRSubsLucky", "WRSubsFresh", exit });
        Say("WRSubsOutLucky", $"{subsOut} and {subs} > 0 and {Not(VarSubsGrounded)} and {N(VarSubPatrols)} >= 1",
            "\"The submarines are still out. They've been lucky so far, but the convoys have escorts now. Another month out there will cost us a boat.\"",
            "\"The submarines are still out, Your Majesty. The convoys have escorts now. Another month will cost us a boat.\"",
            "WRSubsKeepLoss", "WRSubsRecall");
        Say("WRSubsOut", $"{subsOut} and {subs} > 0 and {Not(VarSubsGrounded)}",
            "\"The submarines are still out in the strait, and the Wehzek captains have learned to dread the dark.\"",
            "\"The submarines are still out in the strait, Your Majesty. The Wehzek captains have learned to dread the dark.\"",
            "WRSubsKeep", "WRSubsRecall");
        t.Choice("WRSubsKeep", "\"Keep them out.\"", new[] { "WRSubsKept" }, script: patrol);
        t.Choice("WRSubsKeepLoss", "\"Keep them out anyway.\"", new[] { "WRSubsKept" }, cost: "-1 Submarine", script: patrolLoss);
        Say("WRSubsKept", null, "\"Then they stay on patrol. I'll have the depot ship sent out to meet them.\"",
            "\"Then they stay on patrol, Your Majesty.\"", exit);
        t.Choice("WRSubsRecall", "\"Bring them home.\"", new[] { "WRSubsRecalled" }, script: $"{V(WarCouncil.VarSubsOut)} = false; ");
        Say("WRSubsRecalled", null, "\"They'll be back in Sallabes by the end of the week.\"",
            "\"They'll be back in Sallabes by the end of the week, Your Majesty.\"", exit);
        Say("WRSubsLucky", $"{subs} > 0 and {Not(VarSubsGrounded)} and {N(VarSubPatrols)} >= 1",
            "\"The submarines were lucky last time. The Wehzeks have put destroyers on the convoys since. If they go again, we'll lose one.\"",
            "\"The submarines were lucky last time, Your Majesty. The convoys have escorts now. If they sail again, we'll lose one.\"",
            "WRSubsYesLoss", "WRSubsNo");
        Say("WRSubsFresh", $"{subs} > 0 and {Not(VarSubsGrounded)}",
            "\"And the submarines. Quieter than the fleet, and cheaper in lives. Every Wehzek captain who crosses will spend the night " +
            "staring at the water.\"",
            "\"And the submarines, Your Majesty. Every Wehzek captain who crosses will spend the night staring at the water.\"",
            "WRSubsYes", "WRSubsNo");
        t.Choice("WRSubsYes", "\"Send the submarines.\"", new[] { "WRSubs2" }, script: patrol);
        t.Choice("WRSubsYesLoss", "\"Send them anyway.\"", new[] { "WRSubs2" }, cost: "-1 Submarine", script: patrolLoss);
        Say("WRSubs2", null, "\"They'll sail on the evening tide.\"", "\"They sail on the evening tide, Your Majesty.\"", exit);
        t.Choice("WRSubsNo", "\"Keep them in port for now.\"", new[] { exit });
    }
}
