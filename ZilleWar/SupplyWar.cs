using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// Cutting Zille off from the Wehzek mainland, through three recurring conversations offered each war turn:
/// - A call with Queen Beatrice: Rumburg's oil embargo (vanilla: her Turn 3 visit, Situation_Diplomacy_Rumburg_*),
///   Bludish Freedom Front raids on the Wehzek ports, and BFF fighters for the Royal Army (vanilla Mercenary units,
///   Resources_Military_RumburgianBFFMercenaries). Raids and fighters can be asked for again on later calls.
/// - A call with Prime Minister Alvarez (the note from Konstantium): the Lespian navy closes the strait.
/// - The war room with Lucita Azaro: the Royal Navy and the submarines go after the convoys, turn after turn.
/// Each lever weakens the garrison (War_Zille_Strength, floor 3) and costs something.
/// </summary>
internal static class SupplyWar
{
    public enum Lever { Embargo, BffRaids, BffFighters, RoyalNavy, Submarines, Lespia }

    private const string BeatriceConv = "ZilleWar/Calls/Beatrice";
    private const string AlvarezConv = "ZilleWar/Calls/Alvarez";
    private const string WarRoomConv = "ZilleWar/Calls/WarRoom";
    private const string Beatrice = "Beatrice Livingston (R)";
    private const string Alvarez = "Patricio Alvarez (R)";
    private const string Lucita = "Lucita Azaro";
    private const string Esquibel = "Laurento Esquibel";

    private const string VarEmbargoCounted = "ZilleWar.SupplyWar_EmbargoCounted";
    private const string VarBeatriceCalls = "ZilleWar.Call_Beatrice_Count";
    private const string VarAlvarezCalls = "ZilleWar.Call_Alvarez_Count";
    private const string VarWarRoomMeetings = "ZilleWar.Call_WarRoom_Count";
    private const string VarLespiaCaleqabiz = "ZilleWar.Lespia_Caleqabiz";
    private const string VarLastOfferTurn = "ZilleWar.Calls_LastTurn";
    private const string VarSubPatrols = "ZilleWar.Call_SubPatrolsSinceLoss";

    // Asked during the current call (reset when a call starts).
    private const string AskedEmbargo = "ZilleWar.Call_Asked_Embargo";
    private const string AskedRaids = "ZilleWar.Call_Asked_Raids";
    private const string AskedFighters = "ZilleWar.Call_Asked_Fighters";
    private const string AskedFleet = "ZilleWar.Call_Asked_Fleet";
    private const string AskedSubs = "ZilleWar.Call_Asked_Subs";
    private const string AskedWhy = "ZilleWar.Call_Asked_Why";
    private const string AskedState = "ZilleWar.Call_Asked_State";

    /// <summary>Times each lever was pulled (Embargo and Lespia: 0 or 1).</summary>
    public static string Count(Lever l) => $"ZilleWar.Lever_{l}";

    private static readonly Dictionary<Lever, int> Reported = new();

    public static bool RumburgEmbargo =>
        Variables.GetBool("RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport") ||
        Variables.GetBool("RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport") ||
        Variables.GetInt(Count(Lever.Embargo)) > 0;

    private const string EmbargoLua =
        "(Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true or Variable[\"ZilleWar.Lever_Embargo\"] > 0)";

    public static void Register()
    {
        foreach (Lever l in Enum.GetValues<Lever>())
        {
            Variables.Register(Count(l));
        }

        foreach (string v in new[]
                 {
                     VarEmbargoCounted, VarBeatriceCalls, VarAlvarezCalls, VarWarRoomMeetings, VarLespiaCaleqabiz, VarLastOfferTurn, VarSubPatrols,
                     AskedEmbargo, AskedRaids, AskedFighters, AskedFleet, AskedSubs, AskedWhy, AskedState,
                 })
        {
            Variables.Register(v);
        }

        foreach (string c in new[] { BeatriceConv, AlvarezConv, WarRoomConv })
        {
            ConversationRegistry.RegisterConversation(c);
        }

        RegisterBeatrice();
        RegisterAlvarez();
        RegisterWarRoom();
    }

    /// <summary>At the declaration: make the strength meter explicit, and an embargo already in force bites at once.</summary>
    public static void OnWarDeclared()
    {
        EnsureNumbers();

        if (RumburgEmbargo && !Variables.GetBool(VarEmbargoCounted))
        {
            Variables.Set(VarEmbargoCounted, true);
            ZilleCampaign.Strength -= 1;
        }
    }

    /// <summary>Lua cannot do arithmetic on unset variables, so every counter the calls touch gets a number first.</summary>
    public static void EnsureNumbers()
    {
        ZilleCampaign.Strength = ZilleCampaign.Strength;
        foreach (string v in Enum.GetValues<Lever>().Select(Count)
                     .Concat(new[] { VarBeatriceCalls, VarAlvarezCalls, VarWarRoomMeetings, VarSubPatrols })
                     .Concat(new[] { "RiziaDLC.Army_Unit_Ship_Total", "RiziaDLC.Army_Unit_Submarine_Total", "RiziaDLC.Army_Unit_Mercenary_Total",
                                     "RiziaDLC.Resources_Budget", "RiziaDLC.Resources_Energy", "RiziaDLC.Diplomacy_Wehlen_NegotiatingPower",
                                     "RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", "RiziaDLC.Diplomacy_Sordland_NegotiatingPower",
                                     "RiziaDLC.Diplomacy_Valgsland_NegotiatingPower" }))
        {
            // Only fill in missing values; vanilla resources can hold fractions that must not be rounded.
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = 0 end");
        }
    }

    /// <summary>Once per war turn, after the first War Council: the calls that still have something to offer.</summary>
    public static void Offer()
    {
        EnsureNumbers();
        int turn = GameState.CurrentTurnNum ?? 0;
        if (Variables.GetInt(VarLastOfferTurn) >= turn)
        {
            return;
        }

        Variables.Set(VarLastOfferTurn, turn);
        AddCall(BeatriceConv, $"ZilleWar.Call_Beatrice_T{turn}", SuzerainTokenName.RiziaCityPorteDrazon,
            "A Call from Thornbourgh", "Royal Palace, Porte Drazon");
        if (Variables.GetInt(Count(Lever.Lespia)) == 0)
        {
            AddCall(AlvarezConv, $"ZilleWar.Call_Alvarez_T{turn}", SuzerainTokenName.RiziaCityMonqiz,
                "Call With Prime Minister Alvarez", "Palas Toras, Monqiz");
        }

        if (Variables.GetInt("RiziaDLC.Army_Unit_Ship_Total") > 0 || Variables.GetInt("RiziaDLC.Army_Unit_Submarine_Total") > 0)
        {
            AddCall(WarRoomConv, $"ZilleWar.Call_WarRoom_T{turn}", SuzerainTokenName.RiziaCityPorteDrazon,
                "The Strait of Kreleck", "War Room, Porte Drazon");
        }
    }

    private static void AddCall(string conversation, string fragment, string token, string title, string subtitle)
    {
        try
        {
            if (GameState.StoryFragmentExistsInCurrentStep(fragment))
            {
                return;
            }

            GameState.AddCustomConversation(conversation);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(fragment, SuzerainStoryPackInfo.Rizia, token, title, subtitle,
                ConversationData.ConversationType.ForeignPolicyAndTrade, conversation));
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding '{fragment}' failed: {ex}");
        }
    }

    /// <summary>On each step: news and reports for levers pulled since the last check.</summary>
    public static void ReportNewLevers()
    {
        foreach (Lever l in Enum.GetValues<Lever>())
        {
            int now = Variables.GetInt(Count(l));
            Reported.TryGetValue(l, out int seen);
            if (seen == 0 && now > 0 && !Reported.ContainsKey(l))
            {
                seen = now - 1;     // after a reload, report only the latest pull
            }

            for (int i = seen + 1; i <= now; i++)
            {
                NewsWire.LeverNews(l, i);
            }

            Reported[l] = now;
        }
    }

    private const string ResetAsked = "Variable[\"ZilleWar.Call_Asked_Embargo\"] = false; Variable[\"ZilleWar.Call_Asked_Raids\"] = false; " +
                                      "Variable[\"ZilleWar.Call_Asked_Fighters\"] = false; Variable[\"ZilleWar.Call_Asked_Fleet\"] = false; " +
                                      "Variable[\"ZilleWar.Call_Asked_Subs\"] = false; Variable[\"ZilleWar.Call_Asked_Why\"] = false; " +
                                      "Variable[\"ZilleWar.Call_Asked_State\"] = false; ";

    private static string Not(string v) => $"{V(v)} ~= true";

    // ------------------------------------------------------------------ Beatrice

    private static void RegisterBeatrice()
    {
        var t = new Talk(BeatriceConv, "ZilleWar.Beatrice.");
        string first = $"{V(VarBeatriceCalls)} == 1";
        string later = $"{V(VarBeatriceCalls)} > 1";

        t.Logic("Start", new[] { "Phone" }, hooks: AtStart(BeatriceConv),
            script: ResetAsked + Add(VarBeatriceCalls, 1) + "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\");");
        t.Line("Phone", Narrator,
            "An assistant brought in the blue telephone that was used for calls with heads of state. The line to Thornbourgh " +
            "crackled, clicked twice, and then the Queen of Rumburg was on it.",
            new[] { "Hello1", "HelloLater" }, script: $"AddConversant(\"{Beatrice}\");");

        t.Line("Hello1", Beatrice,
            "\"Romus. I learned that my brother-in-law had gone to war from a wireless operator who could barely contain himself. " +
            "One does like to be told these things before the newspapers.\"", new[] { "R1", "R2", "R3" }, condition: first);
        t.Choice("R1", "\"There wasn't time to ask anyone's permission, Beatrice.\"", new[] { "R1a" });
        t.Line("R1a", Beatrice, "\"Clearly. Well, it's done, and Rumburg doesn't abandon family halfway through a fight. What do you need?\"", new[] { "Hub" });
        t.Choice("R2", "\"I didn't think you'd need telling. You've wanted Smolak humbled for years.\"", new[] { "R2a" });
        t.Line("R2a", Beatrice, "\"I have. I'd simply hoped to be consulted on the method. Never mind. Go on, then. What do you need?\"", new[] { "Hub" });
        t.Choice("R3", "\"Are you calling to scold me, or to help?\"", new[] { "R3a" });
        t.Line("R3a", Beatrice, "\"Can't I do both? I usually manage. But yes, Romus, I'm calling to help. Tell me what you need.\"", new[] { "Hub" });

        t.Line("HelloLater", Beatrice,
            "\"Romus. Still at war, I see. Thornbourgh talks of nothing else, which is a mercy, because it means they've stopped " +
            "talking about my hats. What can I do for you this time?\"", new[] { "Hub" }, condition: later);

        // the hub
        t.Logic("Hub", new[] { "AskEmbargo", "AskRaids", "AskFighters", "Bye" });

        // 1. the oil
        t.Choice("AskEmbargo", "\"Stop buying Wehzek oil. Every barrel you buy pays for another convoy to Zille.\"", new[] { "Emb1" },
            condition: $"not {EmbargoLua} and {Not(AskedEmbargo)}", script: Set(AskedEmbargo, "true"));
        t.Line("Emb1", Beatrice,
            "\"You do realize that oil heats half of Thornbourgh? If I cut Smolak off, my ministers will want your gas instead, at " +
            "a very friendly price. And by friendly, I mean ruinous for you.\"", new[] { "EmbYes", "EmbNo" });
        t.Choice("EmbYes", "\"Done. Rizia will cover the difference.\"", new[] { "Emb2" },
            script: Add(Count(Lever.Embargo), 1) + Weaken(1) + Add("RiziaDLC.Resources_Energy", -3) +
                    Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -2) + Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 2) +
                    Set("RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport", "true"));
        t.Line("Emb2", Beatrice,
            "\"Then it's settled. Smolak will find his treasury considerably lighter by spring, and I shall find my ministers " +
            "considerably louder. Don't make me regret it.\"", new[] { "Hub" });
        t.Choice("EmbNo", "\"Not at that price. Not yet.\"", new[] { "EmbNo2" });
        t.Line("EmbNo2", Beatrice, "\"Sensible. Dull, but sensible. The offer won't vanish.\"", new[] { "Hub" });

        // 2. the BFF on the ports
        t.Choice("AskRaids", "\"Your friends in the Bludish Freedom Front. Can they hit Wehlen's ports?\"", new[] { "Raid1", "RaidAgain" },
            condition: Not(AskedRaids), script: Set(AskedRaids, "true"));
        t.Line("Raid1", Beatrice,
            "\"Friends is a strong word. They're violent, unreliable and outlawed in half of Merkopa. They are also remarkably good " +
            "at setting fire to things.\"", new[] { "Raid2" }, condition: $"{V(Count(Lever.BffRaids))} < 1");
        t.Line("Raid2", Beatrice,
            "\"I can have a quiet word. But understand this: if it ever comes out that Rizia asked, Sordland will scream the house " +
            "down, and Valgsland will hand them the megaphone.\"", new[] { "RaidYes", "RaidNo" });
        t.Line("RaidAgain", Beatrice,
            "\"Again? They do enjoy the work, I'll give them that. Wehzek radio already blames you for the last time, so I doubt " +
            "your reputation can get much worse. Same arrangement?\"", new[] { "RaidYes", "RaidNo" },
            condition: $"{V(Count(Lever.BffRaids))} >= 1");
        t.Choice("RaidYes", "\"Have them strike the ports.\"", new[] { "Raid3" },
            script: Add(Count(Lever.BffRaids), 1) + Weaken(1) + Add("RiziaDLC.Diplomacy_Sordland_NegotiatingPower", -1) +
                    Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1));
        t.Line("Raid3", Beatrice,
            "\"Consider it done. And Romus, should anyone ask, this call was about my niece's wedding plans.\"", new[] { "Hub" });
        t.Choice("RaidNo", "\"No. Not like that.\"", new[] { "RaidNo2" });
        t.Line("RaidNo2", Beatrice, "\"Scruples. How very Rizian of you. As you wish.\"", new[] { "Hub" });

        // 3. BFF fighters
        t.Choice("AskFighters", "\"I could use more men. Would the BFF fight for Rizia?\"", new[] { "Fig1" },
            condition: Not(AskedFighters), script: Set(AskedFighters, "true"));
        t.Line("Fig1", Beatrice,
            "\"For money, they'd fight for anyone who isn't Smolak. Two companies, perhaps. Hard men who know how Wehzeks fight. " +
            "They'll want paying in gold, not gratitude, and your officers will loathe them.\"", new[] { "FigYes", "FigNo" });
        t.Choice("FigYes", "\"Send them. The treasury will pay.\"", new[] { "Fig2" },
            script: Add(Count(Lever.BffFighters), 1) + Add("RiziaDLC.Army_Unit_Mercenary_Total", 2) + Add("RiziaDLC.Resources_Budget", -1) +
                    Set("RiziaDLC.Resources_Military_RumburgianBFFMercenaries", "true") + Add("RiziaDLC.Diplomacy_Sordland_NegotiatingPower", -1));
        t.Line("Fig2", Beatrice, "\"They'll be in Montaklar within the week. Do keep them on a short leash. They bite.\"", new[] { "Hub" });
        t.Choice("FigNo", "\"Rizia will fight its own war.\"", new[] { "FigNo2" });
        t.Line("FigNo2", Beatrice, "\"Admirable. Let's hope it's also enough.\"", new[] { "Hub" });

        // goodbye
        t.Choice("Bye", "\"That's all for now, Beatrice.\"", new[] { "Bye1", "Bye2" });
        t.Line("Bye1", Beatrice,
            "\"Then go and win your war. And do try not to get yourself shot. Your mother would never let me hear the end of it.\"",
            new[] { "Hangup" }, condition: $"{V(VarBeatriceCalls)} <= 1");
        t.Line("Bye2", Beatrice,
            "\"Ring me when you've taken something worth bragging about. I've a dinner on Thursday and I'd love to ruin the " +
            "Valgslandian ambassador's evening.\"", new[] { "Hangup" }, condition: $"{V(VarBeatriceCalls)} > 1");
        t.Line("Hangup", Narrator, "The line went dead. Beatrice never said goodbye on the telephone. She considered it common.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ Alvarez

    private static void RegisterAlvarez()
    {
        var t = new Talk(AlvarezConv, "ZilleWar.Alvarez.");
        string kept = $"{V(AnDeclaration.VarLespianNote)} == \"kept\"";
        string first = $"{V(VarAlvarezCalls)} == 1";

        t.Logic("Start", new[] { "Phone" }, hooks: AtStart(AlvarezConv),
            script: ResetAsked + Add(VarAlvarezCalls, 1) + "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\");");
        t.Line("Phone", Narrator,
            "Laurento was waiting in my study with the blue telephone already on the desk. \"Prime Minister Alvarez, Your Majesty. " +
            "He says he has been very patient.\"", new[] { "HelloKept", "HelloBurned", "HelloLater" }, script: $"AddConversant(\"{Alvarez}\");");
        t.Line("HelloKept", Alvarez,
            "\"Your Majesty. I hope my little note didn't get lost in all the excitement in Konstantium. It was a very exciting day.\"",
            new[] { "Offer" }, condition: $"{first} and {kept}");
        t.Line("HelloBurned", Alvarez,
            "\"Your Majesty. I understand my note met with an accident in Konstantium. No matter. I'm a patient man, and Lespia is a " +
            "practical country.\"", new[] { "Offer" }, condition: $"{first} and not ({kept})");
        t.Line("HelloLater", Alvarez,
            "\"Your Majesty. Smolak's convoys are still crossing the strait, I see. Have you given any more thought to our arrangement?\"",
            new[] { "Offer" }, condition: $"{V(VarAlvarezCalls)} > 1");
        t.Line("Offer", Alvarez,
            "\"Here is my offer. The Lespian navy has the ships to close the Strait of Kreleck. Every Wehzek convoy stopped, searched, " +
            "and held in a Lespian port for as long as the paperwork takes. And the paperwork, I promise you, will take a very long time.\"",
            new[] { "Price", "Why", "NotToday" });
        t.Choice("Why", "\"Why would Lespia help Rizia?\"", new[] { "Why2" }, condition: Not(AskedWhy), script: Set(AskedWhy, "true"));
        t.Line("Why2", Alvarez,
            "\"Because President Smolak has called me an Arcasian puppet on the radio three times this year, and a Wehlen without " +
            "Zille is a Wehlen with fewer reasons to make speeches about Lespia. And because you will owe me. I'm being honest, you see.\"",
            new[] { "Price", "NotToday" });
        t.Choice("Price", "\"And what does Lespia want in return?\"", new[] { "Price2" });
        t.Line("Price2", Alvarez,
            "\"Very little. Naval rights at Caleqabiz. A harbor, a fuel depot, a few hundred sailors. Rizia keeps the island, of course. " +
            "We would only be guests.\"", new[] { "Esq" });
        t.Line("Esq", Esquibel,
            "Laurento covered the mouthpiece. \"Guests with a lease, Your Majesty. That's how Zille began.\"", new[] { "Deal", "Money", "Refuse" });
        t.Choice("Deal", "\"Agreed. Close the strait, Prime Minister.\"", new[] { "Deal2" },
            script: Set(Count(Lever.Lespia), "1") + Set(VarLespiaCaleqabiz, "true") + Weaken(2));
        t.Line("Deal2", Alvarez,
            "\"A pleasure, Your Majesty. My admirals will be in the strait by Sunday. I suspect Mr. Smolak will be on the radio by Monday.\"",
            new[] { "Hangup" });
        t.Choice("Money", "\"Not Caleqabiz. I'll pay for your ships instead.\"", new[] { "Money2" }, condition: kept);
        t.Line("Money2", Alvarez,
            "\"Money. How refreshingly direct. Very well, Your Majesty, but warships are expensive, and Lespian admirals more so.\"",
            new[] { "MoneyYes", "MoneyNo" });
        t.Choice("MoneyYes", "\"Name the figure and close the strait.\"", new[] { "Deal2" },
            script: Set(Count(Lever.Lespia), "1") + Add("RiziaDLC.Resources_Budget", -2) + Weaken(2));
        t.Choice("MoneyNo", "\"Then we have nothing to agree on today.\"", new[] { "NoDeal" });
        t.Choice("Refuse", "\"No. Rizia has already lost one region to a lease.\"", new[] { "NoDeal" });
        t.Line("NoDeal", Alvarez,
            "\"Ha. I suppose I walked into that one. The offer stands, Your Majesty. For now.\"", new[] { "Hangup" });
        t.Choice("NotToday", "\"Not today, Prime Minister.\"", new[] { "NotToday2" });
        t.Line("NotToday2", Alvarez,
            "\"Of course. The strait isn't going anywhere. Neither, unfortunately, are Smolak's convoys.\"", new[] { "Hangup" });
        t.Line("Hangup", Narrator, "We said our goodbyes, and Laurento carried the telephone away.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ the war room

    private static void RegisterWarRoom()
    {
        var t = new Talk(WarRoomConv, "ZilleWar.WarRoom.");
        string s = V(ZilleCampaign.VarStrength);
        string ships = V("RiziaDLC.Army_Unit_Ship_Total");
        string subs = V("RiziaDLC.Army_Unit_Submarine_Total");

        t.Logic("Start", new[] { "Chart" }, hooks: AtStart(WarRoomConv),
            script: ResetAsked + Add(VarWarRoomMeetings, 1));
        t.Line("Chart", Narrator,
            "Lucita Azaro spread a naval chart of the Strait of Kreleck across the war room table. Red pencil marks traced the convoy " +
            "routes from the Wehzek ports to Nanta and Isor.", new[] { "L1" }, sequence: "PlaySceneMusic(Security);", script: $"AddConversant(\"{Lucita}\");");
        t.Line("L1", Lucita,
            "\"They run the convoys at night now, Your Majesty. Four or five transports at a time, with a destroyer for escort. " +
            "The Admiralty wants your permission to go after them.\"", new[] { "Hub" });
        t.Logic("Hub", new[] { "AskFleet", "AskSubs", "AskState", "Bye" });

        t.Choice("AskFleet", "\"Send the fleet into the strait.\"", new[] { "Fleet1" },
            condition: $"{ships} > 0 and {Not(AskedFleet)}", script: Set(AskedFleet, "true"));
        t.Line("Fleet1", Lucita,
            "\"We'll sink transports, and probably one of their warships with them. We'll also lose at least one of ours. The strait " +
            "is narrow, and their coastal guns are good.\"", new[] { "FleetYes", "FleetNo" });
        t.Choice("FleetYes", "\"Do it.\"", new[] { "Fleet2" },
            script: Add(Count(Lever.RoyalNavy), 1) + Add("RiziaDLC.Army_Unit_Ship_Total", -1) + Weaken(1));
        t.Line("Fleet2", Lucita, "\"I'll send the order to Sallabes tonight. You'll have the butcher's bill by morning.\"", new[] { "Hub" });
        t.Choice("FleetNo", "\"Not at that price.\"", new[] { "Hub" });

        t.Choice("AskSubs", "\"What about the submarines?\"", new[] { "Subs1" },
            condition: $"{subs} > 0 and {Not(AskedSubs)}", script: Set(AskedSubs, "true"));
        t.Line("Subs1", Lucita,
            "\"Quieter, and cheaper in lives. They won't stop a convoy outright, but every captain who crosses will spend the night " +
            "staring at the water. Sooner or later, we'll lose a boat.\"", new[] { "SubsYes", "SubsNo" });
        t.Choice("SubsYes", "\"Send the submarines.\"", new[] { "Subs2" },
            script: Add(Count(Lever.Submarines), 1) + Add(VarSubPatrols, 1) + Weaken(1) +
                    $"if {V(VarSubPatrols)} >= 2 then {Add("RiziaDLC.Army_Unit_Submarine_Total", -1)}{Set(VarSubPatrols, "0")} end; ");
        t.Line("Subs2", Lucita, "\"They'll sail on the evening tide.\"", new[] { "Hub" });
        t.Choice("SubsNo", "\"Keep them in port for now.\"", new[] { "Hub" });

        t.Choice("AskState", "\"How are they holding up over there?\"", new[] { "State1", "State2", "State3", "State4" },
            condition: Not(AskedState), script: Set(AskedState, "true"));
        t.Line("State1", Lucita, "\"Well, I'm afraid. The convoys come every night, and the Zille garrison is still growing.\"", new[] { "Hub" },
            condition: $"{s} >= 8");
        t.Line("State2", Lucita, "\"They're holding. The convoys still come, but the garrison has stopped growing.\"", new[] { "Hub" },
            condition: $"{s} >= 6 and {s} < 8");
        t.Line("State3", Lucita, "\"They're feeling it. Prisoners talk about rationed shells, and tanks parked for want of fuel.\"", new[] { "Hub" },
            condition: $"{s} >= 4 and {s} < 6");
        t.Line("State4", Lucita, "\"They're starving, Your Majesty. Deserters come over every night now, most of them without rifles.\"", new[] { "Hub" },
            condition: $"{s} < 4");

        t.Choice("Bye", "\"That will be all, Lucita.\"", new[] { "Bye2" });
        t.Line("Bye2", Lucita, "\"Your Majesty.\" She rolled up the chart and was gone before I had sat down.", new[] { "End" });
        t.End();
        t.Register();
    }
}
