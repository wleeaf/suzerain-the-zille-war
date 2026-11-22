using Il2Cpp;
using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using static ZilleWar.Talk;
using GameState = SuzerainModdingKit.GameState;

namespace ZilleWar;

/// <summary>
/// The Zille War Council: one conversation in the Council Chambers each war turn, modelled on vanilla's
/// "Briefing on the Invasion of Pales" (c357) and "Operation 2 Next Steps" (c412).
/// The first council (Turn 4, right after Konstantium) is the briefing; later ones open with the last
/// operation's result. The objective is announced in the fixed campaign order; the King decides how it is
/// fought (approach, the Aureus squadron, a preparatory barrage, his own plan or the Staff's), and whether
/// to push on, hold for a turn or seek a ceasefire. Those orders are read by <see cref="ZilleOperation"/>.
/// The war table only opens once the council has given its order and no other "ZilleWar.Pre_" fragment
/// (or supply-war call) is pending in the current step, so everything that belongs before the war comes first.
/// </summary>
internal static class WarCouncil
{
    public const string Conv = "ZilleWar/Council/War";
    public const string FragmentPrefix = "ZilleWar.Pre_Council_T";
    private static readonly string[] PrePrefixes = { "ZilleWar.Pre_", "ZilleWar.Call_" };

    // ---- the King's orders (read by ZilleOperation.BuildWarOperation)
    public const string VarApproach = "ZilleWar.Plan_Approach";            // "Direct" | "Feint" | "Landing"
    public const string VarBombard = "ZilleWar.Plan_Bombard";              // preparatory barrage ordered
    public const string VarDelegated = "ZilleWar.Plan_Delegated";          // the General Staff drew up the plan
    public const string VarFleetRecalled = "ZilleWar.Plan_FleetRecalled";  // Aureus squadron brought north (stays true)

    // ---- council bookkeeping
    public const string VarDoneTurn = "ZilleWar.Council_DoneTurn";         // last turn a council was completed
    public const string VarOrderTurn = "ZilleWar.Council_OrderTurn";       // last turn the council ordered an operation
    public const string VarHeldTurn = "ZilleWar.Council_HeldTurn";         // last turn the King chose to hold

    // ---- set by C# right before the council opens (Lua reads them)
    private const string VarTurn = "ZilleWar.Council_Turn";
    private const string VarKind = "ZilleWar.Council_Kind";                // 1 = first briefing, 2 = next steps
    private const string VarObjective = "ZilleWar.Council_Objective";      // Objective enum name
    private const string VarCoastal = "ZilleWar.Council_Coastal";
    private const string VarLastObjective = "ZilleWar.Council_LastObjective";
    private const string VarLossTier = "ZilleWar.Council_LossTier";        // 0 light, 1 heavy, 2 severe
    private const string VarSetback = "ZilleWar.Council_Setback";
    private const string VarWaited = "ZilleWar.Council_Waited";
    private const string VarExtraTaken = "ZilleWar.Council_ExtraTaken";
    private const string VarVinaAsked = "ZilleWar.Council_VinaAsked";
    private static string LostVar(string city) => $"ZilleWar.Council_Lost_{city.Replace(" ", string.Empty)}";
    private static readonly string[] RizianCities = { "Mave Nortus", "Dapalle", "Balle", "Jales" };

    // ---- asked this council (reset at start)
    private const string AskedForces = "ZilleWar.Council_Asked_Forces";
    private const string AskedGarrison = "ZilleWar.Council_Asked_Garrison";
    private const string AskedFronts = "ZilleWar.Council_Asked_Fronts";
    private const string HubVisited = "ZilleWar.Council_HubVisited";

    // ---- speakers
    private const string Lucita = "Lucita Azaro";
    private const string Carlos = "Carlos Robles Azaro";
    private const string Taddeus = "Taddeus Azaro";
    private const string Laurento = "Laurento Esquibel";
    private const string Elena = "Elena Werner";
    private const string Hugo = "Hugo Toras";
    private const string Vina = "Vina Toras";

    public enum Approach { Direct, Feint, Landing }

    /// <summary>The orders of the latest council.</summary>
    public sealed record Orders(Approach Approach, bool Bombard, bool Delegated, bool FleetRecalled, bool FleetSplit);

    public static Orders CurrentOrders()
    {
        Approach a = Enum.TryParse(Variables.GetString(VarApproach) ?? string.Empty, out Approach x) ? x : Approach.Direct;
        bool recalled = Variables.GetBool(VarFleetRecalled);
        bool split = !recalled && Variables.GetBool("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship");
        return new Orders(a, Variables.GetBool(VarBombard), Variables.GetBool(VarDelegated), recalled, split);
    }

    private static readonly HashSet<int> Reinjected = new();
    private static Talk t;

    public static void Register()
    {
        foreach (string v in new[]
                 {
                     VarApproach, VarBombard, VarDelegated, VarFleetRecalled, VarDoneTurn, VarOrderTurn, VarHeldTurn,
                     VarTurn, VarKind, VarObjective, VarCoastal, VarLastObjective, VarLossTier, VarSetback, VarWaited,
                     VarExtraTaken, VarVinaAsked, AskedForces, AskedGarrison, AskedFronts, HubVisited,
                 }.Concat(RizianCities.Select(LostVar)))
        {
            Variables.Register(v);
        }

        ConversationRegistry.RegisterConversation(Conv);
        BuildConversation();
    }

    // ------------------------------------------------------------------ runtime

    /// <summary>Each step while the war is on: offer the council, then open the war table when nothing else should come first.</summary>
    public static void OnStep()
    {
        if (!GameState.IsCurrentStoryPack(SuzerainStoryPackInfo.Rizia) || !WarFlow.AtWar)
        {
            return;
        }

        int turn = GameState.CurrentTurnNum ?? 0;
        if (turn <= 0 || (turn == 1 && (GameState.CurrentStepNum ?? 0) < 2))
        {
            return;
        }

        EnsureNumbers();
        Reinject();
        OfferCouncil(turn);
        OpenWarTable(turn);
    }

    /// <summary>True while one of our pre-war fragments (council, calls, other modules' "ZilleWar.Pre_*") is still to be played.</summary>
    public static bool PrePending()
    {
        var names = Managers.Instance?.GameFlowManager?.enabledNotDoneStoryFragmentsNames;
        if (names == null)
        {
            return false;
        }

        foreach (string n in names)
        {
            if (n != null && PrePrefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static void OfferCouncil(int turn)
    {
        string name = FragmentPrefix + turn;
        if (Variables.GetBool(WarFlow.VarOperationReady) || Variables.GetInt(WarFlow.VarLastOperationTurn) >= turn ||
            Variables.GetInt(VarDoneTurn) >= turn || GameState.StoryFragmentExistsInCurrentStep(name) || ZilleCampaign.Available().Count == 0)
        {
            return;
        }

        bool first = !Variables.GetBool(WarFlow.VarCouncilHeld) && ZilleCampaign.OperationsDone == 0;
        ObjectiveInfo next = ZilleCampaign.Info(ZilleCampaign.NextObjective);
        Prepare(turn, first, next);
        try
        {
            GameState.AddCustomConversation(Conv);
            GameState.AddCustomStoryFragment(new CustomConversationFragment(name, SuzerainStoryPackInfo.Rizia, SuzerainTokenName.RiziaCityPorteDrazon,
                first ? "Briefing on the Liberation of Zille" : $"{ZilleCampaign.Title(next)} Briefing",
                "Council Chambers, Palas Rezna", ConversationData.ConversationType.SecurityAndOrder, Conv));
            Core.Log.Msg($"War Council offered: {name} ({(first ? "briefing" : "next steps")}, objective {next.Codename}).");
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the War Council failed: {ex}");
        }
    }

    /// <summary>Everything the council's lines branch on, computed from the campaign state.</summary>
    private static void Prepare(int turn, bool first, ObjectiveInfo next)
    {
        Variables.Set(VarTurn, turn);
        Variables.Set(VarKind, first ? 1 : 2);
        Variables.Set(VarObjective, next.Id.ToString());
        Variables.Set(VarCoastal, next.Id != Objective.Wall);
        Variables.Set(VarWaited, Variables.GetInt(VarHeldTurn) > 0 && Variables.GetInt(VarHeldTurn) == turn - 1);

        Dictionary<string, bool> owners = ZilleCampaign.CurrentOwners();
        foreach (string city in RizianCities)
        {
            Variables.Set(LostVar(city), ZilleCampaign.IsEnemy(owners, ZilleCampaign.TownTile(city)));
        }

        int k = ZilleCampaign.OperationsDone;
        string[] hist = (Variables.GetString(ZilleCampaign.VarHistory) ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries);
        string lastObjective = hist.Length > 0 ? hist[^1].Split(':')[0] : string.Empty;
        Variables.Set(VarLastObjective, lastObjective);
        int playerDead = k > 0 ? Variables.GetInt(ZilleCampaign.PlayerDeadVar(k)) : 0;
        int enemyDead = k > 0 ? Variables.GetInt(ZilleCampaign.EnemyDeadVar(k)) : 0;
        int tier = playerDead >= 6 || (playerDead >= 3 && playerDead > enemyDead) ? 2 : playerDead >= 3 || playerDead > enemyDead ? 1 : 0;
        Variables.Set(VarLossTier, tier);
        bool lostOurOwn = RizianCities.Any(c => Variables.GetBool(LostVar(c)));
        Variables.Set(VarSetback, !first && (lostOurOwn || tier == 2));
        bool extra = Enum.TryParse(lastObjective, out Objective lo) &&
                     ZilleData.Towns.Any(tw => tw.Role == "city" && tw.Side == "zille" && !ZilleCampaign.Info(lo).Towns.Contains(tw.Name) &&
                                               Variables.GetBool(ZilleOperation.CapturedVar(tw.Name)) &&
                                               ZilleCampaign.Sequence.SkipWhile(o => o != lo).Skip(1).Any(o => ZilleCampaign.Info(o).Towns.Contains(tw.Name)));
        Variables.Set(VarExtraTaken, extra);
    }

    /// <summary>The council has given its order and nothing that belongs before the war is left: open the war table.</summary>
    private static void OpenWarTable(int turn)
    {
        if (Variables.GetInt(VarOrderTurn) != turn || Variables.GetInt(WarFlow.VarLastOperationTurn) >= turn ||
            Variables.GetBool(WarFlow.VarOperationReady) || PrePending() || ZilleCampaign.Available().Count == 0)
        {
            return;
        }

        if (ZilleOperation.BuildNext(out WarFragmentData frag))
        {
            Variables.Set(WarFlow.VarLastOperationTurn, turn);
            Variables.Set(WarFlow.VarOperationReady, true);
            Reinjected.Add(ZilleCampaign.OperationsDone + 1);
            WarProbe.InjectWarFragment(frag);
        }
    }

    /// <summary>An operation was ordered but its fragment is gone (the game was reloaded): put it back once.</summary>
    private static void Reinject()
    {
        int k = ZilleCampaign.OperationsDone + 1;
        if (!Variables.GetBool(WarFlow.VarOperationReady) || Reinjected.Contains(k) ||
            GameState.StoryFragmentExistsInCurrentStep(ZilleOperation.FragmentName(k)) || ZilleCampaign.Available().Count == 0)
        {
            return;
        }

        Reinjected.Add(k);
        if (ZilleOperation.BuildNext(out WarFragmentData frag))
        {
            Core.Log.Msg($"Re-opening the war table for operation {k} after a reload.");
            WarProbe.InjectWarFragment(frag);
        }
    }

    private static void EnsureNumbers()
    {
        foreach (string v in new[]
                 {
                     "RiziaDLC.Army_Unit_Infantry_Total", "RiziaDLC.Army_Unit_Support_Total", "RiziaDLC.Army_Unit_Tank_Total",
                     "RiziaDLC.Army_Unit_Marine_Total", "RiziaDLC.Army_Unit_Ship_Total", "RiziaDLC.Army_Unit_Submarine_Total",
                     "RiziaDLC.Resources_Military_Equipment", VarDoneTurn, VarOrderTurn, VarHeldTurn, WarFlow.VarLastOperationTurn,
                     ZilleCampaign.VarStrength,
                 })
        {
            Il2CppPixelCrushers.DialogueSystem.Lua.Run($"if Variable[\"{v}\"] == nil then Variable[\"{v}\"] = 0 end");
        }

        if (Variables.GetInt(ZilleCampaign.VarStrength) <= 0)
        {
            ZilleCampaign.Strength = ZilleCampaign.DefaultStrength;
        }
    }

    // ------------------------------------------------------------------ Lua conditions

    private static string Num(string v) => $"({V(v)} or 0)";
    private static string Is(string v) => $"{V(v)} == true";
    private static string IsNot(string v) => $"{V(v)} ~= true";
    private static string And(params string[] c) => string.Join(" and ", c.Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})"));
    private static string Not(string c) => $"not ({c})";

    private const string LucitaHere = "Variable[\"RiziaDLC.Lucita_Arrested\"] ~= true and Variable[\"RiziaDLC.Lucita_Executed\"] ~= true " +
                                      "and Variable[\"RiziaDLC.Council_Security_Lucita\"] ~= false";
    private const string HugoHere = "Variable[\"RiziaDLC.Hugo_Resigned\"] ~= true and Variable[\"RiziaDLC.Hugo_Arrested\"] ~= true " +
                                    "and Variable[\"RiziaDLC.Hugo_Executed\"] ~= true";
    private const string LaurentoHere = "Variable[\"RiziaDLC.Council_Foreign_Vina\"] ~= true and Variable[\"RiziaDLC.Council_Foreign_Empty\"] ~= true";
    private const string ElenaHere = "Variable[\"RiziaDLC.Council_Treasury_Rusty\"] ~= true and Variable[\"RiziaDLC.Council_Treasury_Daria\"] ~= true " +
                                     "and Variable[\"RiziaDLC.Council_Treasury_Empty\"] ~= true";
    private const string VinaHere = "Variable[\"RiziaDLC.Vina_OnCouncil\"] == true";

    // Taddeus only sits in once vanilla has brought him back (the Pales briefing) or made him war councilor.
    private const string TaddeusHere = "(Variable[\"RiziaDLC.War_Happened\"] == true or Variable[\"RiziaDLC.Council_Security_Taddeus\"] == true) " +
                                       "and Variable[\"RiziaDLC.Taddeus_Arrested\"] ~= true and Variable[\"RiziaDLC.Taddeus_Executed\"] ~= true";

    private static string First => $"{V(VarKind)} == 1";
    private static string Later => $"{V(VarKind)} == 2";
    private static string Obj(Objective o) => $"{V(VarObjective)} == \"{o}\"";
    private static string Equip(int n) => $"{Num("RiziaDLC.Resources_Military_Equipment")} >= {n}";
    private static string Lever(SupplyWar.Lever l) => $"{Num(SupplyWar.Count(l))} > 0";

    private const string Embargo =
        "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
        "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true or (Variable[\"ZilleWar.Lever_Embargo\"] or 0) > 0";

    private static string Strength => Num(ZilleCampaign.VarStrength);
    private static string FleetSplit => And(Is("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship"), IsNot(VarFleetRecalled));
    private const int BarrageCost = 500;

    // ------------------------------------------------------------------ helpers

    /// <summary>A line that only plays when <paramref name="cond"/> holds; otherwise the conversation moves straight on.</summary>
    private static void Opt(string name, string speaker, string text, string cond, string next, string script = null)
    {
        t.Logic(name, new[] { name + "_Say", next });
        t.Line(name + "_Say", speaker, text, new[] { next }, condition: cond, script: script);
    }

    /// <summary>Lucita's line, spoken by General Robles Azaro when she is not on the council (arrested, replaced).</summary>
    private static void Intel(string name, string text, string[] next, string cond = null, string carlosText = null)
    {
        t.Logic(name, new[] { name + "_L", name + "_C" }, condition: cond);
        t.Line(name + "_L", Lucita, text, next, condition: LucitaHere);
        t.Line(name + "_C", Carlos, carlosText ?? text, next, condition: Not(LucitaHere));
    }

    /// <summary>Optional Lucita/Carlos line: plays when cond holds, otherwise skipped.</summary>
    private static void IntelOpt(string name, string text, string cond, string next, string carlosText = null)
    {
        t.Logic(name, new[] { name + "_L", name + "_C", next });
        t.Line(name + "_L", Lucita, text, new[] { next }, condition: And(cond, LucitaHere));
        t.Line(name + "_C", Carlos, carlosText ?? text, new[] { next }, condition: And(cond, Not(LucitaHere)));
    }

    /// <summary>The first variant whose condition holds (the last one should have none).</summary>
    private static void Pick(string name, string speaker, string next, params (string cond, string text)[] variants)
    {
        t.Logic(name, variants.Select((_, i) => $"{name}_{i}").ToArray());
        for (int i = 0; i < variants.Length; i++)
        {
            t.Line($"{name}_{i}", speaker, variants[i].text, new[] { next }, condition: variants[i].cond);
        }
    }

    // ------------------------------------------------------------------ the conversation

    private static void BuildConversation()
    {
        t = new Talk(Conv, "ZilleWar.Council.");
        string reset = Set(AskedForces, "false") + Set(AskedGarrison, "false") + Set(AskedFronts, "false") + Set(HubVisited, "false") +
                       Set(VarApproach, "\"Direct\"") + Set(VarBombard, "false") + Set(VarDelegated, "false");
        string conversants =
            $"AddConversant(\"{Carlos}\"); " +
            $"if {LucitaHere} then AddConversant(\"{Lucita}\") end; " +
            $"if {TaddeusHere} then AddConversant(\"{Taddeus}\") end; " +
            $"if ({First} or {V(VarTurn)} == 5) then " +
            $"if {HugoHere} then AddConversant(\"{Hugo}\") end; if {ElenaHere} then AddConversant(\"{Elena}\") end; " +
            $"if {VinaHere} then AddConversant(\"{Vina}\") end; end; " +
            $"if {LaurentoHere} then AddConversant(\"{Laurento}\") end; ";
        t.Logic("Start", new[] { "Truce", "Open1", "Open2", "Open2Setback" }, hooks: AtStart(Conv), script: reset + conversants);

        // The war ended earlier this turn (a ceasefire was accepted on the telephone): the briefing becomes a short stand-down.
        t.Line("Truce", Narrator,
            "The generals had already heard. The maps were still on the table, but nobody reached for the pointers.",
            new[] { "Truce2" }, condition: $"Variable[\"{WarState.VarActive}\"] ~= true or Variable[\"{WarFlow.VarCeasefire}\"] == true");
        t.Line("Truce2", Carlos,
            "\"The orders went out an hour ago, Your Majesty. Every unit holds where it stands and fires only if fired upon. If the " +
            "Wehzeks keep their word, the guns stay quiet tonight.\"",
            new[] { "Truce3" });
        t.Choice("Truce3", "\"See that they do. And see that ours do too.\"", new[] { "End" });

        Arrival();
        PoliticsFirst();
        ResultsAndPolitics();
        PushOrHold();
        IntelReadout();
        Briefing();
        Objectives();
        HowToFight();
        Closing();

        t.End();
        t.Register();
    }

    private static void Arrival()
    {
        // The first council: straight back from Konstantium.
        t.Line("Open1", Narrator,
            "The flight back from Kyrute had been a quiet one. Nobody aboard wanted to be the first to say out loud what we had done.",
            new[] { "Open1b" }, condition: First, sequence: "PlaySceneMusic(Security);");
        t.Line("Open1b", Narrator,
            "By the time I reached Palas Rezna, the Council Chambers had been turned into a war room. My councilors sat along one wall. " +
            "The rest of the room belonged to the generals, bent over a map of Zille covered in golden and silver tokens.",
            new[] { "Salute" });

        // Later councils: the map has changed.
        t.Line("Open2", Narrator,
            "The map in the Council Chambers had been redrawn since I last saw it. The golden tokens had crossed the border, and the " +
            "silver ones had been pushed back toward the sea.",
            new[] { "Open2b" }, condition: And(Later, IsNot(VarSetback)), sequence: "PlaySceneMusic(Security);");
        t.Line("Open2Setback", Narrator,
            "The map in the Council Chambers had been redrawn since I last saw it. Some of the golden tokens had crossed the border. " +
            "Others had been swept off the table, and a few silver ones now stood on our side of the line.",
            new[] { "Open2b" }, condition: And(Later, Is(VarSetback)), sequence: "PlaySceneMusic(Security);",
            hooks: null);
        t.Logic("Open2b", new[] { "Open2Laurento", "Salute" });
        t.Line("Open2Laurento", Narrator,
            "Laurento Esquibel sat at the far end of the table with a folder of cables from Konstantium, looking as if he would rather be anywhere else.",
            new[] { "Salute" }, condition: LaurentoHere);

        t.Line("Salute", Narrator, "As soon as I entered, the chatter stopped and every officer in the room came to attention.",
            new[] { "SaluteA", "SaluteB", "SaluteC" });
        t.Choice("SaluteA", "I saluted.", new[] { "SaluteA2" }, menu: "Salute.");
        t.Line("SaluteA2", Narrator, "The room returned the salute with full force.", new[] { "Taddeus" });
        t.Choice("SaluteB", "\"At ease.\"", new[] { "SaluteB2" });
        t.Line("SaluteB2", Narrator, "Shoulders relaxed, slightly. Nobody sat down.", new[] { "Taddeus" });
        t.Choice("SaluteC", "\"Let's not waste any time.\"", new[] { "SaluteC2" });
        t.Line("SaluteC2", Carlos, "\"Your Majesty.\"", new[] { "Taddeus" });

        Opt("Taddeus", Narrator,
            "General Taddeus Azaro had been wheeled to the head of the table. He raised a trembling hand to his brow, and his eyes never left the map.",
            TaddeusHere, "Carlos");

        t.Logic("Carlos", new[] { "Carlos1", "Pol2" });
        t.Line("Carlos1", Narrator,
            "General Carlos Robles Azaro stepped forward. With Taddeus Azaro still recovering in Montaklar, the second-in-command of " +
            "the Armed Forces had been running the mobilization from this room since the first cable arrived from Konstantium.",
            new[] { "Carlos2" }, condition: First);
        t.Line("Carlos2", Carlos,
            "\"Your Majesty. The northern army group is mobilized. Our divisions are at Dapalle and Mave Nortus, and the fleet is putting to sea " +
            "from Sallabes. We can cross the border within forty-eight hours of your order.\"",
            new[] { "Pol1" });
    }

    /// <summary>The first council, the morning after Konstantium: the Royal Council has its say before the generals.</summary>
    private static void PoliticsFirst()
    {
        string anyone = $"({HugoHere}) or ({LaurentoHere}) or ({ElenaHere}) or ({VinaHere})";
        t.Logic("Pol1", new[] { "Pol1Hugo", "Pol1HugoNone", "Intel" });
        t.Line("Pol1Hugo", Hugo,
            "\"Before the generals begin, Your Majesty. The news reached Porte Drazon before you did. There were crowds at the palace gates by " +
            "nightfall, singing the anthem. I don't believe I've heard it sung like that since your father's day.\"",
            new[] { "Pol1Laur" }, condition: HugoHere);
        t.Line("Pol1HugoNone", Narrator, "My councilors had waited up for me. They did not wait for the generals to finish saluting.",
            new[] { "Pol1Laur" }, condition: And(Not(HugoHere), anyone));

        Opt("Pol1Laur", Laurento,
            "\"Konstantium was rather less musical, Your Grace. President Smolak walked out of the session, and the Security Committee sat until " +
            "midnight without agreeing on a censure. That is the good news. The bad news is that they will meet again next week, and the week " +
            "after that, for as long as this war lasts.\"",
            LaurentoHere, "Pol1Elena");
        Opt("Pol1Elena", Elena,
            "\"And for as long as it lasts, it will cost us. The mobilization alone has eaten what I had set aside for the winter. I'll find the " +
            "money, Your Majesty. I would only ask the generals to remember that every shell has a price.\"",
            ElenaHere, "Pol1Vina");
        Opt("Pol1Vina", Vina,
            "\"Has anyone asked the people of Zille what they want? The Rizians there, and the Wehzeks too. They're the ones who will be under the shells.\"",
            VinaHere, "Pol1HugoVina", script: Set(VarVinaAsked, "true"));
        Opt("Pol1HugoVina", Hugo,
            "\"They have been asking since the lease expired, Your Highness. His Majesty has finally answered them.\"",
            And(VinaHere, HugoHere), "Pol1ReplyGate");
        // A node offers either lines or choices, never both: Dialogue System plays a valid line before any choice.
        t.Logic("Pol1ReplyGate", new[] { "Pol1Reply", "Intel" });
        t.Logic("Pol1Reply", new[] { "Pol1R1", "Pol1R2", "Pol1R3" }, condition: anyone);
        t.Choice("Pol1R1", "\"Wehlen made this choice when it refused to give back what was ours.\"", new[] { "Pol1R1b" });
        t.Line("Pol1R1b", Narrator, "Nobody argued. Around the table, a few heads nodded slowly.", new[] { "Intel" });
        t.Choice("Pol1R2", "\"Your concerns are heard. The best answer to all of them is a short war.\"", new[] { "Pol1R2b" });
        t.Line("Pol1R2b", Carlos, "\"Then let us make it a short one, Your Majesty.\"", new[] { "Intel" });
        t.Choice("Pol1R3", "\"The decision is made. General, proceed.\"", new[] { "Pol1R3b" });
        t.Line("Pol1R3b", Narrator, "The councilors fell silent. Carlos cleared his throat and turned back to the map.", new[] { "Intel" });
    }

    /// <summary>Later councils: the last operation's result, and (Turn 5) the Royal Council's view of the war.</summary>
    private static void ResultsAndPolitics()
    {
        // Turn 5: the council sits in. This is what the player sees where vanilla had the Wehlen trade talks.
        string t5 = $"{V(VarTurn)} == 5 and Variable[\"ZilleWar.E5_CouncilOffered\"] ~= true";   // EarlyT5 has its own fuller council scene
        t.Logic("Pol2", new[] { "Pol2Open", "Res" });
        t.Line("Pol2Open", Narrator,
            "The Royal Council had asked to sit in on the briefing. They took the chairs along the wall, and the generals pretended not to mind.",
            new[] { "Pol2Laur" }, condition: And(t5, $"({HugoHere}) or ({LaurentoHere}) or ({ElenaHere}) or ({VinaHere})"));
        Opt("Pol2Laur", Laurento,
            "\"The Security Committee met again on Tuesday, Your Majesty. Valgsland tabled a censure of our declaration, and it failed, just as " +
            "Smolak's protocol failed before it. Konstantium has now failed to agree on Zille twice in one month. I am not yet sure whether to " +
            "call that a victory.\"",
            LaurentoHere, "Pol2Elena");
        Opt("Pol2Elena", Elena,
            "\"Before all this, Smolak was going to offer us his oil for our gold. Now we buy our oil from someone else at twice the price, and " +
            "the army burns it faster than the rest of the country put together.\"",
            ElenaHere, "Pol2Elena2");
        Opt("Pol2Elena2", Elena,
            "\"And we are still sending our own energy north to Thornbourgh under the Rumburg contract. I'd like that remembered the next time " +
            "the Queen calls to ask for a favor.\"",
            And(ElenaHere, "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport\"] == true or " +
                           "Variable[\"RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport\"] == true"), "Pol2Hugo");
        Opt("Pol2Hugo", Hugo,
            "\"The House of Delegates met yesterday. Baroness de Rava and her party are delighted. The Brenas members are not. Half of them " +
            "have family on the other side of the Zille line.\"",
            HugoHere, "Pol2Vina");
        t.Logic("Pol2Vina", new[] { "Pol2Vina1", "Pol2Vina2", "Pol2Reply" });
        t.Line("Pol2Vina1", Vina,
            "\"I asked whether anyone had asked the people of Zille. They're answering now, Father. From their cellars.\"",
            new[] { "Pol2Reply" }, condition: And(VinaHere, Is(VarVinaAsked)));
        t.Line("Pol2Vina2", Vina,
            "\"The people of Zille are in their cellars, Father. Rizians and Wehzeks both. Does anyone in this room intend to ask them what they want?\"",
            new[] { "Pol2Reply" }, condition: And(VinaHere, IsNot(VarVinaAsked)));
        t.Logic("Pol2Reply", new[] { "Pol2R1", "Pol2R2" });
        t.Choice("Pol2R1", "\"Every day we argue in this room is another day in those cellars. Let's end it quickly.\"", new[] { "Pol2R1b" });
        t.Line("Pol2R1b", Narrator, "Hugo murmured his agreement. Vina looked away.", new[] { "Res" });
        t.Choice("Pol2R2", "\"Thank you all. Now let the generals speak.\"", new[] { "Res" });

        // The result of the last operation.
        t.Line("Res", Carlos, "\"Your Majesty. The latest from the front.\"", new[] { "ResHarbor", "ResWall", "ResOther" });
        t.Line("ResHarbor", Carlos,
            "\"Zille is ours. The harbor, the railway station and the Wehzek headquarters. Their whole southern front has lost its port.\"",
            new[] { "ResExtra" }, condition: $"{V(VarLastObjective)} == \"Harbor\"");
        t.Line("ResWall", Carlos,
            "\"Auremina has fallen, and the gold hills with it. The roads north to Isor and Nanta are open.\"",
            new[] { "ResExtra" }, condition: $"{V(VarLastObjective)} == \"Wall\"");
        t.Line("ResOther", Carlos, "\"Our last offensive took everything we asked of it.\"", new[] { "ResExtra" });
        Opt("ResExtra", Carlos,
            "\"And we took more than the Staff had planned for. Some of the towns beyond the objective gave up as soon as they saw our flags.\"",
            Is(VarExtraTaken), "ResLostMave");
        Opt("ResLostMave", Carlos,
            "\"But while we were pushing west, the enemy took Mave Nortus. Their flag is flying over our own border town.\"",
            Is(LostVar("Mave Nortus")), "ResLostDapalle");
        Opt("ResLostDapalle", Carlos,
            "\"They have also broken into Dapalle. Our headquarters had to fall back to Balle.\"",
            Is(LostVar("Dapalle")), "ResLostBalle");
        Opt("ResLostBalle", Carlos, "\"Balle is in Wehzek hands. They are closer to the heart of Brenas than they have ever been.\"",
            Is(LostVar("Balle")), "ResLostJales");
        Opt("ResLostJales", Carlos, "\"And they have landed at Jales. I don't yet know how they got past the fleet.\"",
            Is(LostVar("Jales")), "ResLosses");
        Pick("ResLosses", Carlos, "ResWaited",
            ($"{V(VarLossTier)} == 2", "\"It cost us dearly. The casualty lists are the longest this army has seen since the Reclamation War.\""),
            ($"{V(VarLossTier)} == 1", "\"It was not cheap. The hospitals in Dapalle are full, and the trains south are full of wounded.\""),
            (null, "\"Our losses were light, thank God. The men are tired, but in good spirits.\""));
        Opt("ResWaited", Lucita,
            "\"The front has been quiet since we halted. Our men have rested. I'm afraid so have theirs.\"",
            And(Is(VarWaited), LucitaHere), "ResTwoWars");
        Opt("ResTwoWars", Taddeus,
            "\"Two wars, Lieutenant. Your father tried that once, and I was there to see it. We will not repeat it. We sequence them. " +
            "Zille holds, Pales breaks, then we turn north again.\"",
            And(TaddeusHere, "Variable[\"RiziaDLC.Pales_War\"] == true"), "Push");
    }

    /// <summary>Push on, hold for a turn, or seek a ceasefire. On a setback the generals push back, as in c412.</summary>
    private static void PushOrHold()
    {
        t.Logic("Push", new[] { "PushSetback", "PushGood" });
        t.Line("PushGood", Carlos, "\"The troops are waiting for their orders, Your Majesty. The Staff is ready to move on the next objective.\"",
            new[] { "PushTad" }, condition: IsNot(VarSetback));
        Opt("PushTad", Taddeus, "\"A complete victory is in sight, Lieutenant.\"", TaddeusHere, "PushMenu");
        t.Logic("PushMenu", new[] { "PushGo", "PushWait", "Cease1", "Cease2" });
        t.Choice("PushGo", "\"Then we keep moving. Smolak doesn't get a week to recover.\"", new[] { "Intel" });
        t.Choice("PushWait", "\"We'll hold for now. Let the men rest, and let the blockade do its work.\"", new[] { "PushWait2" });
        Intel("PushWait2", "\"Wehlen will use the time too, Your Majesty. Every quiet week is another week of convoys across the strait.\"",
            new[] { "PushWaitYes", "PushWaitNo" });
        t.Choice("PushWaitYes", "\"I know. We hold.\"", new[] { "Hold" });
        t.Choice("PushWaitNo", "\"You're right. We go on.\"", new[] { "Intel" });

        t.Line("PushSetback", Carlos,
            "\"We are not in a good position, Your Majesty. The divisions that went in last time are exhausted, and we have lost ground of our own.\"",
            new[] { "SetbackMenu" }, condition: Is(VarSetback));
        t.Logic("SetbackMenu", new[] { "SbPush", "SbHold", "Cease1", "Cease2" });
        t.Choice("SbPush", "\"Simply holding the line is unacceptable. We push on, whatever it costs.\"", new[] { "SbObj1" });
        t.Line("SbObj1", Carlos, "\"Our hands are tied, Your Majesty. Those men are not ready to go in again.\"", new[] { "SbObj2" });
        Intel("SbObj2", "\"If we push now and fail, Your Majesty, we lose the war. And everything we've taken with it.\"",
            new[] { "SbObj3" }, carlosText: "\"If we push now and fail, we lose the war, Your Majesty. And everything we've taken with it.\"");
        Opt("SbObj3", Taddeus, "\"Please listen to them, Lieutenant. We can't overextend.\"", TaddeusHere, "SbMenu2");
        t.Logic("SbMenu2", new[] { "SbCoward", "SbRespect", "SbYield" });
        t.Choice("SbCoward", "\"Cowards, all of you! You will attack, or I will find generals who will.\"", new[] { "SbCoward2" });
        t.Line("SbCoward2", Narrator, "Everyone seemed taken aback, but nobody dared to protest.", new[] { "SbAsYouWish" });
        t.Choice("SbRespect", "\"Respectfully, General, I disagree. I order you to attack.\"", new[] { "SbAsYouWish" });
        t.Line("SbAsYouWish", Carlos, "\"As you wish, Your Majesty.\"", new[] { "Intel" });
        t.Choice("SbYield", "\"You may have a point. Very well. We hold, for now.\"", new[] { "SbYield2" });
        t.Line("SbYield2", Carlos, "\"Agreed. We should wait for better conditions.\"", new[] { "Hold" });
        t.Choice("SbHold", "\"Then be the wall that protects Brenas. Hold the line, and let nothing through.\"", new[] { "Hold" });

        t.Line("Hold", Carlos, "\"Then we dig in and wait. I'll see that the men get their rest.\"", new[] { "Hold2" });
        t.Line("Hold2", Narrator, "The generals saluted. For this week, at least, the war table would stay quiet.", new[] { "HoldEnd" });
        t.Logic("HoldEnd", new[] { "End" },
            script: Set(VarDoneTurn, V(VarTurn)) + Set(VarHeldTurn, V(VarTurn)) + Set(WarFlow.VarCouncilHeld, "true") +
                    $"if {Strength} < 10 then {V(ZilleCampaign.VarStrength)} = {Strength} + 1 end; ");

        // The ceasefire, spoken. Laurento takes it to Konstantium when he's here.
        t.Choice("Cease1", "\"Enough. Mr. Esquibel, tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseLaur" },
            condition: LaurentoHere);
        t.Choice("Cease2", "\"Enough. Tell Konstantium we are ready to talk. We will hold what we have taken.\"", new[] { "CeaseCarlos" },
            condition: Not(LaurentoHere));
        t.Line("CeaseLaur", Laurento,
            "\"I'll speak to the Secretary-General tonight, Your Majesty. Smolak will refuse the first offer on principle. He'll accept the " +
            "second, because his treasury will make him.\"",
            new[] { "CeaseCarlos" });
        t.Line("CeaseCarlos", Carlos, "\"Understood, Your Majesty. The army will hold its positions and fire only if fired upon.\"", new[] { "CeaseTad" });
        Opt("CeaseTad", Taddeus, "\"We had them, Lieutenant. We had them.\"", TaddeusHere, "Cease3");
        t.Line("Cease3", Narrator, "The generals saluted, slower than before. The tokens on the map stayed exactly where they were.", new[] { "CeaseEnd" });
        t.Logic("CeaseEnd", new[] { "End" },
            script: Set(VarDoneTurn, V(VarTurn)) + Set(WarFlow.VarCeasefire, "true") + Set(WarFlow.VarCouncilHeld, "true"));
    }

    /// <summary>What we know: the garrison's strength and the supply war so far.</summary>
    private static void IntelReadout()
    {
        t.Logic("Intel", new[] { "IntelOpenL", "IntelOpenC" });
        t.Line("IntelOpenL", Narrator, "Lucita stepped up to the map and tapped the Strait of Kreleck with her pointer.", new[] { "Garr" }, condition: LucitaHere);
        t.Line("IntelOpenC", Narrator, "Carlos picked up a pointer and tapped the Strait of Kreleck.", new[] { "Garr" }, condition: Not(LucitaHere));
        t.Logic("Garr", new[] { "Garr1", "Garr2", "Garr3", "Garr4" });
        Intel("Garr1", "\"The Wehzek garrison in Zille is strong, and it is still growing. Their convoys cross the strait every night, and they have had " +
                       "months to dig in.\"", new[] { "SupEmbargo" }, $"{Strength} >= 8");
        Intel("Garr2", "\"The Wehzek garrison is holding its numbers. Their convoys still cross the strait, but the reinforcements have slowed.\"",
            new[] { "SupEmbargo" }, $"{Strength} >= 6 and {Strength} < 8");
        Intel("Garr3", "\"The garrison is feeling the squeeze. Their convoys arrive late or not at all, and prisoners talk about rationed shells and " +
                       "tanks parked for want of fuel.\"", new[] { "SupEmbargo" }, $"{Strength} >= 4 and {Strength} < 6");
        Intel("Garr4", "\"The garrison is starving, Your Majesty. Fuel, shells, bread. Deserters come over every night now, most of them without rifles.\"",
            new[] { "SupEmbargo" }, $"{Strength} < 4");

        IntelOpt("SupEmbargo", "\"Rumburg no longer buys Wehzek oil. Smolak's treasury feels it, and so do his fuel depots in Zille.\"",
            Embargo, "SupRaids");
        IntelOpt("SupRaids",
            "\"The Bludish Freedom Front has been busy in the north of Wehlen. Every battalion Smolak keeps at home to guard his ports is one he " +
            "can't send to Zille.\"", Lever(SupplyWar.Lever.BffRaids), "SupLespia");
        IntelOpt("SupLespia",
            "\"And the Lespian navy is stopping and searching every Wehzek ship in the Strait of Kreleck. Prime Minister Alvarez is keeping his " +
            "word. For now.\"", Lever(SupplyWar.Lever.Lespia), "SupNavy");
        IntelOpt("SupNavy",
            "\"Our own ships have been sinking transports in the strait. Their captains cross at night now and hug the coast.\"",
            $"({Lever(SupplyWar.Lever.RoyalNavy)}) or ({Lever(SupplyWar.Lever.Submarines)})", "SupSplit");
        IntelOpt("SupSplit",
            "\"One more thing. Part of our fleet is still tied down off the Aureus field, watching the Palesian drillship. That gives the " +
            "Wehzek navy more room in the gulf than I would like.\"", FleetSplit, "SupNone");
        IntelOpt("SupNone",
            "\"Nobody is stopping their convoys yet, Your Majesty. Not Rumburg, not Lespia, not us. Until somebody does, every man we kill in " +
            "Zille will be replaced within the week.\"",
            And(Not(Embargo), Not(Lever(SupplyWar.Lever.BffRaids)), Not(Lever(SupplyWar.Lever.Lespia)),
                Not(Lever(SupplyWar.Lever.RoyalNavy)), Not(Lever(SupplyWar.Lever.Submarines)), $"{Strength} >= 6"), "Brief");
    }

    /// <summary>"Would you like to hear it?" and the questions hub.</summary>
    private static void Briefing()
    {
        t.Line("Brief", Carlos,
            "\"Your Majesty, you have the final word on how this war is fought. But the Staff has prepared a briefing. Would you like to hear it?\"",
            new[] { "BriefYes", "BriefNo" });
        t.Choice("BriefYes", "\"Please, General.\"", new[] { "Hub" });
        t.Choice("BriefNo", "\"No. I've read the reports. Tell me the objective.\"", new[] { "BriefNo2" });
        t.Line("BriefNo2", Carlos, "\"Of course, Your Majesty.\"", new[] { "Obj" });

        t.Logic("Hub", new[] { "HubQ1", "HubQ2" });
        t.Line("HubQ1", Carlos, "\"What would you like to hear about first?\"", new[] { "HubMenu" }, condition: IsNot(HubVisited),
            script: Set(HubVisited, "true"));
        t.Line("HubQ2", Carlos, "\"What else would you like to know?\"", new[] { "HubMenu" });
        t.Logic("HubMenu", new[] { "AskForces", "AskGarrison", "AskFronts", "AskDone" });
        t.Choice("AskForces", "\"What's the status of our forces?\"", new[] { "Forces" }, condition: IsNot(AskedForces), script: Set(AskedForces, "true"));
        t.Choice("AskGarrison", "\"What do we know about the Wehzek garrison?\"", new[] { "Garrison" }, condition: IsNot(AskedGarrison),
            script: Set(AskedGarrison, "true"));
        t.Choice("AskFronts", "\"Give me the details of each front.\"", new[] { "Fronts" }, condition: IsNot(AskedFronts), script: Set(AskedFronts, "true"));
        t.Choice("AskDone", "\"I think I have enough information.\"", new[] { "Obj" });

        // Our forces, read from the army the player actually has (thresholds as in c357).
        string inf = Num("RiziaDLC.Army_Unit_Infantry_Total"), sup = Num("RiziaDLC.Army_Unit_Support_Total"), tank = Num("RiziaDLC.Army_Unit_Tank_Total");
        string marine = Num("RiziaDLC.Army_Unit_Marine_Total"), ships = Num("RiziaDLC.Army_Unit_Ship_Total"), subs = Num("RiziaDLC.Army_Unit_Submarine_Total");
        Pick("Forces", Carlos, "ForcesSup",
            ($"{inf} >= 8", "\"We can field eight or nine fully equipped infantry divisions, Your Majesty. More than enough to hold the line and carry the attack.\""),
            ($"{inf} > 5", "\"We can field six or seven infantry divisions. Enough, if we don't waste them.\""),
            (null, "\"We can field five infantry divisions at most. We will have to choose our battles carefully.\""));
        Pick("ForcesSup", Carlos, "ForcesTank",
            ($"{sup} >= 7", "\"We have a strong body of mechanized support divisions to back them up.\""),
            ($"{sup} >= 4", "\"We have a fair number of mechanized support divisions to back them up.\""),
            (null, "\"Our mechanized support is thin. We should not risk losing what we have.\""));
        Pick("ForcesTank", Carlos, "ForcesMarine",
            ($"{tank} >= 6", "\"And our tank divisions outnumber anything Wehlen has in Zille. They will lead the assault.\""),
            ($"{tank} >= 3", "\"And we have enough tanks to lead an assault on one front, if not on two.\""),
            (null, "\"As for tanks, I won't pretend. We can barely field two divisions, and the Wehzeks know it.\""));
        IntelOpt("ForcesMarine", "\"Our marines and their landing ships are ready at Sallabes.\"", $"{marine} >= 1", "ForcesShips");
        t.Logic("ForcesShips", new[] { "ForcesShips1", "ForcesShips2", "ForcesSubs" });
        Intel("ForcesShips1", "\"The fleet has enough destroyers to rule the Gulf of Moridia.\"", new[] { "ForcesSubs" }, $"{ships} >= 5");
        Intel("ForcesShips2", "\"The fleet can contest the gulf, but I wouldn't ask much more of it.\"", new[] { "ForcesSubs" }, $"{ships} >= 1 and {ships} < 5");
        t.Logic("ForcesSubs", new[] { "ForcesSubs1", "ForcesSubs2", "ForcesBff" });
        Intel("ForcesSubs1", "\"The submarines are already out in the strait, hunting their convoys.\"", new[] { "ForcesBff" },
            And($"{subs} >= 1", Lever(SupplyWar.Lever.Submarines)));
        Intel("ForcesSubs2", "\"The submarines are in port at Sallabes, waiting for orders.\"", new[] { "ForcesBff" },
            And($"{subs} >= 1", Not(Lever(SupplyWar.Lever.Submarines))));
        Opt("ForcesBff", Carlos,
            "\"And the Bludish volunteers Rumburg sent us have arrived at Dapalle. My officers can't stand them. They fight well, I'll give them that.\"",
            Lever(SupplyWar.Lever.BffFighters), "Hub");

        // The garrison: composition by tier (matches what Garrison.Build puts on the map).
        Pick("Garrison", Carlos, "GarrisonWall",
            ($"{Strength} >= 8", "\"Close to a full corps of regulars, with four tank battalions in reserve and hardly any militia. They have had months to prepare.\""),
            ($"{Strength} >= 6", "\"Regular infantry along the line, three or so tank battalions in reserve, and militia filling the gaps.\""),
            ($"{Strength} >= 4", "\"Two tank battalions, and more militia than regulars now. The regulars are the ones they can no longer resupply.\""),
            (null, "\"One tank battalion, if it has fuel. The rest are militia and police with rifles.\""));
        t.Logic("GarrisonWall", new[] { "GarrisonWall1", "GarrisonWall2" });
        t.Line("GarrisonWall1", Carlos,
            "\"Along the border they've built a wall of concrete and wire from Mave Nortus down to the Gulf of Moridia, with a bunker covering every crossing.\"",
            new[] { "Hub" }, condition: First);
        t.Line("GarrisonWall2", Carlos, "\"Their border wall is broken where we went through. What is left of it won't stop us.\"", new[] { "Hub" });

        // The fronts, per objective.
        t.Logic("Fronts", new[] { "FrontHarbor", "FrontWall", "FrontPorts" });
        t.Line("FrontHarbor", Carlos,
            "\"Zille sits on its bay. The main road and the railway come down the river valley from Dapalle. The harbor is covered by coastal " +
            "guns, and the city has the only anti-aircraft batteries in the territory. Until those are silenced, our planes will stay away.\"",
            new[] { "FrontHarbor2" }, condition: Obj(Objective.Harbor));
        Intel("FrontHarbor2", "\"From the sea, the bay is wide open. But those guns will be waiting for any ship that comes in.\"", new[] { "FrontHome" });
        t.Line("FrontWall", Carlos,
            "\"Auremina is up in the hills. Mining country: narrow roads, ridges, and old tunnels where they can hide a battalion. Our tanks won't " +
            "be much use up there. It will be infantry work.\"", new[] { "FrontHome" }, condition: Obj(Objective.Wall));
        t.Line("FrontPorts", Carlos,
            "\"Isor is on the west coast, Nanta on the north. Nanta is where their convoys land, and it has coastal guns of its own. Take both, " +
            "and Wehlen has nowhere left to bring its men ashore.\"", new[] { "FrontHome" }, condition: Obj(Objective.Ports));
        string anyLost = string.Join(" or ", RizianCities.Select(c => $"({Is(LostVar(c))})"));
        Opt("FrontHome", Carlos, "\"And before anything else, we must take back our own towns behind the line. Brenas will not forgive us otherwise.\"",
            anyLost, "Hub");
    }

    /// <summary>The objective is announced in the fixed order, not chosen.</summary>
    private static void Objectives()
    {
        t.Logic("Obj", new[] { "ObjHarbor", "ObjWall", "ObjPorts" });
        t.Line("ObjHarbor", Carlos,
            "\"The objective is the city of Zille itself: the harbor, the railway station, and the Wehzek headquarters. Take Zille, and every " +
            "Wehzek soldier south of the hills is cut off from the sea.\"", new[] { "ObjHarbor2" }, condition: Obj(Objective.Harbor));
        t.Line("ObjHarbor2", Carlos,
            "\"After that, the hills and the gold mines at Auremina. Then the northern ports, Isor and Nanta, and the war is won. The Staff has " +
            "named the first operation Golden Harbor.\"", new[] { "Command" });
        t.Line("ObjWall", Carlos,
            "\"Our next objective is Auremina. Take the hills and the mines, and their garrison in the north is cut off from everything south of " +
            "it. The Staff calls it Operation Lodestone.\"", new[] { "Command" }, condition: Obj(Objective.Wall));
        t.Line("ObjPorts", Carlos,
            "\"The last objective is the northern coast: Isor and Nanta. Every Wehzek convoy lands at one or the other. Take both, and their " +
            "garrison has nowhere left to go but home. Operation Northern Crown.\"", new[] { "Command" }, condition: Obj(Objective.Ports));
    }

    /// <summary>How the operation is fought. Every choice is read by ZilleOperation when the map is built.</summary>
    private static void HowToFight()
    {
        t.Line("Command", Carlos,
            "\"The General Staff has drawn up a plan, Your Majesty. Or, if you prefer, you can lay out the operation yourself.\"",
            new[] { "CmdSelf", "CmdStaffCost", "CmdStaff" });
        t.Choice("CmdSelf", "\"I'll plan this one myself, General. Walk me through the options.\"", new[] { "Appr" });
        t.Choice("CmdStaffCost", "\"The General Staff knows this ground better than I do. Carry out your plan.\"", new[] { "Staff" },
            condition: Equip(BarrageCost), cost: $"-{BarrageCost} Military Equipment",
            script: Set(VarDelegated, "true") + Set(VarApproach, "\"Feint\"") + Set(VarBombard, "true") +
                    $"{V("RiziaDLC.Resources_Military_Equipment")} = {Num("RiziaDLC.Resources_Military_Equipment")} - {BarrageCost}; ");
        t.Choice("CmdStaff", "\"The General Staff knows this ground better than I do. Carry out your plan.\"", new[] { "Staff" },
            condition: Not(Equip(BarrageCost)), script: Set(VarDelegated, "true") + Set(VarApproach, "\"Feint\""));
        t.Line("Staff", Carlos,
            "\"Thank you, Your Majesty. Then we feint first, to draw their reserves away from the objective, and go in where they've thinned out.\"",
            new[] { "Staff2" });
        t.Logic("Staff2", new[] { "Staff2a", "Staff2b" });
        t.Line("Staff2a", Carlos, "\"The artillery opens up the night before. By morning there won't be much left of their forward positions.\"",
            new[] { "Fleet" }, condition: Is(VarBombard));
        t.Line("Staff2b", Carlos, "\"We haven't the shells for a proper barrage, so the infantry will have to do it the hard way.\"", new[] { "Fleet" });

        // The approach.
        string coastal = And(Is(VarCoastal), $"{Num("RiziaDLC.Army_Unit_Ship_Total")} >= 1");
        t.Line("Appr", Carlos, "\"There are three ways to go about it, Your Majesty. Straight at them, around them, or from the sea.\"",
            new[] { "ApprDirect", "ApprFeint", "ApprLanding" });
        t.Choice("ApprDirect", "\"We go straight at them. Everything on the main road, as fast as it will go.\"", new[] { "ApprDirect2" },
            script: Set(VarApproach, "\"Direct\""));
        t.Line("ApprDirect2", Carlos,
            "\"A direct assault, then. They'll see us coming, but if we're quick, their armor won't be up in time to meet us.\"", new[] { "Fleet" });
        t.Choice("ApprFeint", "\"We feint first. Make them think we're coming somewhere else.\"", new[] { "ApprFeint2" },
            script: Set(VarApproach, "\"Feint\""));
        t.Logic("ApprFeint2", new[] { "FeintHarbor", "FeintWall", "FeintPorts" });
        t.Line("FeintHarbor", Carlos,
            "\"We'll make a show toward Auremina and the hills. If they take the bait, their armor goes north, and Zille is left with its garrison.\"",
            new[] { "FeintCost" }, condition: Obj(Objective.Harbor));
        t.Line("FeintWall", Carlos, "\"We'll threaten the coast road toward Isor. They'll have to pull men out of the hills to cover it.\"",
            new[] { "FeintCost" }, condition: Obj(Objective.Wall));
        t.Line("FeintPorts", Carlos, "\"We'll make a great deal of noise in front of Nanta, and see how much they leave behind in Isor.\"",
            new[] { "FeintCost" }, condition: Obj(Objective.Ports));
        t.Line("FeintCost", Carlos, "\"It will cost us a day or two. They'll use it to bring a few more men across the strait.\"", new[] { "Fleet" });
        t.Choice("ApprLanding", "\"We come from the sea. Put men ashore behind their lines.\"", new[] { "ApprLanding2" }, condition: coastal,
            script: Set(VarApproach, "\"Landing\""));
        Intel("ApprLanding2", "\"The landing ships go in while the army comes down the road. If their coastal guns are still firing when the boats " +
                              "go in, we will lose ships.\"", new[] { "ApprLanding3" });
        IntelOpt("ApprLanding3", "\"And we have no marines to speak of. It will be ordinary infantry in landing boats. Brave men, and not many of them.\"",
            $"{Num("RiziaDLC.Army_Unit_Marine_Total")} < 1", "Fleet");

        // The Aureus squadron.
        t.Logic("Fleet", new[] { "FleetAsk", "Barrage" });
        Intel("FleetAsk",
            "\"Then there is the fleet. Half our destroyers are still sitting on the Aureus field, babysitting a Palesian drillship. Bring them north, " +
            "and the Wehzek navy loses the gulf. Leave them where they are, and the Duke's drillship stays idle.\"",
            new[] { "FleetNorth", "FleetStay" }, And(FleetSplit, $"{Num("RiziaDLC.Army_Unit_Ship_Total")} >= 1"));
        t.Choice("FleetNorth", "\"Bring the squadron north. Zille comes first.\"", new[] { "FleetNorth2" },
            script: Set(VarFleetRecalled, "true") + Set("RiziaDLC.Diplomacy_Pales_Military_BlockDrillship", "false"));
        Intel("FleetNorth2", "\"I'll send the signal tonight. Reinhart will have his drillship working by the end of the week, and he'll thank no one for it.\"",
            new[] { "Barrage" });
        t.Choice("FleetStay", "\"The blockade stays. We fight Wehlen with what we have.\"", new[] { "FleetStay2" });
        Intel("FleetStay2", "\"As you wish. Their ships will have a little more room in the gulf.\"", new[] { "Barrage" });

        // The barrage (the Staff's plan already decided it).
        t.Logic("Barrage", new[] { "BarrageAsk", "Close" });
        t.Line("BarrageAsk", Carlos,
            "\"Last of all, the artillery. We can give their forward positions a full night's barrage before the infantry goes in. It will cost " +
            "us shells we may want later, and it won't be kind to the villages along the line.\"",
            new[] { "BarrageYes", "BarrageNo" }, condition: And(IsNot(VarDelegated), Equip(BarrageCost)));
        t.Choice("BarrageYes", "\"Open fire the night before. Spare nothing.\"", new[] { "BarrageYes2" }, cost: $"-{BarrageCost} Military Equipment",
            script: Set(VarBombard, "true") +
                    $"{V("RiziaDLC.Resources_Military_Equipment")} = {Num("RiziaDLC.Resources_Military_Equipment")} - {BarrageCost}; ");
        t.Line("BarrageYes2", Carlos, "\"Understood. The guns will be in position by nightfall.\"", new[] { "Close" });
        t.Choice("BarrageNo", "\"No barrage. We may need those shells, and the people in those villages are ours.\"", new[] { "BarrageNo2" });
        t.Line("BarrageNo2", Carlos, "\"Very well. The infantry will do it the hard way.\"", new[] { "Close" });
    }

    private static void Closing()
    {
        t.Line("Close", Carlos, "\"We will relay your orders immediately, Your Majesty.\"", new[] { "Close2" });
        t.Line("Close2", Narrator, "The generals in the room gave me a military salute and answered as one. \"Yes, sir.\"",
            new[] { "CloseA", "CloseB", "CloseC" });
        t.Choice("CloseA", "\"Good luck to us all.\"", new[] { "CloseA2" });
        Intel("CloseA2", "\"Good luck to all of us, Your Majesty.\"", new[] { "Close3" });
        t.Choice("CloseB", "\"For king and country.\"", new[] { "CloseB2" });
        t.Line("CloseB2", Carlos, "\"For king and country.\"", new[] { "Close3" });
        t.Choice("CloseC", "\"Do not fail me, General.\"", new[] { "CloseC2" });
        t.Line("CloseC2", Carlos, "\"We won't, Your Majesty.\"", new[] { "Close3" });
        Intel("Close3", "\"I will come to you as soon as we have news from the front.\"", new[] { "Close4", "Close5" });
        t.Line("Close4", Narrator, "And there it was. The war for Zille was about to begin.", new[] { "Order" }, condition: First);
        t.Line("Close5", Narrator, "Somewhere north of Dapalle, the guns were already being moved forward.", new[] { "Order" });
        t.Logic("Order", new[] { "End" },
            script: Set(VarOrderTurn, V(VarTurn)) + Set(VarDoneTurn, V(VarTurn)) + Set(WarFlow.VarCouncilHeld, "true"));
    }
}
