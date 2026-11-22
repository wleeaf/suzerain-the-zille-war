using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Conversation;
using SuzerainModdingKit.VanillaData;
using Il2Cpp;
using static ZilleWar.Talk;


namespace ZilleWar;

/// <summary>
/// The peace treaty with Wehlen (the turn after the war ends, late in it; replaces vanilla's airship talks). Every outcome
/// ends at the negotiating table, built on vanilla's own negotiation panel: the paged decisions panel that the Turn 5 Wehlen
/// talks use for the Zille Agreement amendment, with a Negotiation Leverage bar and pages of terms.
/// - Victory (the Treaty of Kreleck): Rizia starts with leverage and spends it forcing terms on Wehlen: renunciation of Zille,
///   war reparations, the Auremina gold, oil, a disarmed strait. Leverage can be raised at the table first: reminding Smolak who
///   won, the Lespian consulate files, or threats: a landing on the Wehzek coast, Derdian missiles over Wraclawitz, and, with
///   Rumburg's nerve agent in the warheads, worse.
/// - Armistice (the Mave Nortus Treaty): Rizia starts at nothing. The line can become a border, Auremina can be bought back, but
///   only by granting what Wehlen asks for.
/// - Defeat (the Terms of Wraclawitz): Rizia starts in debt and must satisfy Wehlen's demands (recognition, reparations, the
///   Queen Mother's house, Ricardus) before Smolak will let it keep anything (Mave Nortus, its prisoners).
/// Smolak signs a draft that leaves the leverage at zero or above. Below zero he refuses; the King can strike out his own
/// demands until Smolak can live with the rest, raise the stakes with a threat he hasn't made yet, or walk away without a treaty.
/// A treaty that settles Zille (renounced, border, recognized) makes peace: Wehlen is Unfriendly instead of Hostile, and the
/// Valgslandian and CSP embargo ends.
/// </summary>
internal static class ZilleTreaty
{
    public const string Conv = "ZilleWar/Late/Treaty";
    public const string VarLeverage = "ZilleWar.Treaty_Leverage";
    public const string VarSigned = "ZilleWar.Treaty_Signed";
    /// <summary>"renounced" | "border" | "accord" | "recognized" | "terms" | "none"</summary>
    public const string VarKind = "ZilleWar.Treaty_Kind";
    /// <summary>The treaty settles Zille: Wehlen at peace with Rizia (Unfriendly), embargo over.</summary>
    public const string VarPeace = "ZilleWar.Treaty_Peace";
    /// <summary>"" | "invasion" | "missiles" | "gas"</summary>
    public const string VarThreat = "ZilleWar.Treaty_Threat";
    public const string VarTrimmed = "ZilleWar.Treaty_Trimmed";
    private const string VarMapApplied = "ZilleWar.Treaty_MapApplied";
    private const string VarAskedWon = "ZilleWar.Treaty_AskedWon";
    private const string VarAskedFiles = "ZilleWar.Treaty_AskedFiles";
    private const string VarRefused = "ZilleWar.Treaty_Refused";
    private const string VarNewsDone = "ZilleWar.Treaty_NewsDone";
    public const string ModReparations = "ZilleWar.Mod_TreatyReparations";
    public const string ModOil = "ZilleWar.Mod_TreatyOil";
    public const string ModStrait = "ZilleWar.Mod_TreatyStrait";

    private const string Narr = Narrator;
    private const string Smolak = "Wiktor Smolak (R)";
    private const string Laurento = "Laurento Esquibel";

    private const string PanelLib = "Panel_ZilleWar_TreatyKreleck";
    private const string PanelPart = "Panel_ZilleWar_TreatyMaveNortus";
    private const string PanelLost = "Panel_ZilleWar_TreatyWraclawitz";

    public static string Term(string key) => $"ZilleWar.Treaty_T_{key}";

    private static string Is(string v) => $"Variable[\"{v}\"] == true";
    private static string Not(string v) => $"Variable[\"{v}\"] ~= true";
    private static string Lev => V(VarLeverage);
    private static string AddLev(int n) => $"{Lev} = ({Lev} or 0) + ({n}); ";

    // vanilla state
    private const string Missiles = "Variable[\"RiziaDLC.Resources_Military_DerdianMissiles\"] == true";
    private const string NerveGas = "Variable[\"RiziaDLC.Resources_Military_RumburgianNerveGas\"] == true";
    private const string PalesBusy = "(Variable[\"RiziaDLC.Pales_War\"] == true and Variable[\"RiziaDLC.War_Pales_Won\"] ~= true)";
    private const string RicoHeld = "Variable[\"RiziaDLC.Rico_Arrested\"] == true and Variable[\"RiziaDLC.Rico_Extradited\"] ~= true";
    private const string BludishBan = "RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishBan";
    private const string WehzekRights = "RiziaDLC.Situation_Law_WehlenRights_Full";
    private const string GlobalImage = "RiziaDLC.Country_GlobalPerception";
    private const string Budget = "RiziaDLC.Resources_Budget";
    private const string Authority = "RiziaDLC.Resources_Authority";
    private const string Opinion = "RiziaDLC.Country_PublicOpinion";
    // The files are in hand: found and not burned, not already public (Smolak knows), or the security brief produced the evidence.
    private static readonly string Files =
        "(Variable[\"RiziaDLC.Zille_Publicized_LespianInvolvement\"] ~= true and Variable[\"ZilleWar.Ramiro_Choice\"] ~= \"publish\" and " +
        "((Variable[\"ZilleWar.Ramiro_Found\"] == true and Variable[\"ZilleWar.Ramiro_Choice\"] ~= \"bury\") or " +
        "Variable[\"RiziaDLC.Zille_Discovered_LespianInvolvement\"] == true))";

    private static readonly string Lib = LK.Lib;
    private static readonly string Part = LK.Part;
    private static readonly string Lost = LK.Lost;
    private static readonly string Cease = LK.Ceasefire;

    // ------------------------------------------------------------------ the panels

    private sealed record Opt(string Key, string Title, int Lev, string Condition = null);

    private sealed record PageSpec(string Key, string Title, string Subtitle, int Min, int Max, Opt[] Options);

    private sealed record PanelSpec(string Name, string Title, string Description, PageSpec[] Pages);

    private static readonly PanelSpec[] Panels =
    {
        new(PanelLib, "The Treaty of Kreleck",
            "Wehlen has come to the table beaten. Every term Rizia demands costs leverage; every request of Wehlen's that Rizia grants " +
            "buys some back. President Smolak will not sign a draft that leaves him with less than nothing.",
            new PageSpec[]
            {
                new("LibZille", "Zille", "What Wehlen gives up in Zille.", 1, 1, new Opt[]
                {
                    new("Renounce", "Wehlen renounces every claim to Zille, now and forever", -2),
                    new("Strait", "Wehlen recognizes the border at the Strait of Kreleck and says nothing more", -1),
                }),
                new("LibRep", "War Reparations", "What Wehlen pays for the war its refusal to leave Zille began.", 1, 1, new Opt[]
                {
                    new("RepNone", "No reparations", 0),
                    new("Rep1", "Reparations [+1 Budget Per Turn]", -2),
                    new("Rep2", "Heavy reparations [+2 Budget Per Turn]", -4),
                }),
                new("LibMore", "Concessions", "Further terms Rizia may demand of a beaten Wehlen.", 0, -1, new Opt[]
                {
                    new("Gold", "The Auremina gold the garrison shipped north is returned [+3 Budget]", -1),
                    new("Assets", "Wehzek state property in Zille passes to the Crown [+2 Budget]", -1),
                    new("Oil", "Wehzek oil sold to Rizia at a discount [+1 Energy Per Turn]", -2),
                    new("Demil", "No Wehzek warship in the Strait of Kreleck [+1 Authority]", -2),
                }),
                new("LibPow", "Prisoners of War", "Thousands of Wehzek soldiers are in Rizian camps. Smolak wants them home.", 1, 1, new Opt[]
                {
                    new("PowFree", "Every prisoner goes home the day the treaty is signed", 2),
                    new("PowPaid", "Prisoners go home as the reparations are paid", 0),
                    new("PowRansom", "The officers stay until Wehlen pays for them [+2 Budget]", -1),
                }),
                new("LibAsks", "Wehlen's Requests", "Smolak's own wishes. Each one granted makes the rest easier for him to swallow.", 0, -1, new Opt[]
                {
                    new("Rico", "Ricardus Toras is handed over to Wraclawitz to answer for Smolak Square", 2, "RiziaDLC.Rico_Arrested == true && RiziaDLC.Rico_Extradited == false"),
                    new("Rights", "Wehzek families in Zille keep their homes and their citizenship", 1),
                    new("Ban", "Rizia's border is closed to the Bludish Freedom Front", 1, $"{BludishBan} == false"),
                    new("Villa", "President Smolak's villa in Zille is returned to him", 1),
                }),
            }),
        new(PanelPart, "The Mave Nortus Treaty",
            "Neither army won. Smolak will trade the line through Zille for what Wehlen wants from Rizia: every Rizian demand costs " +
            "leverage, every Wehzek request granted earns it back. He will not sign a draft that leaves him with less than nothing.",
            new PageSpec[]
            {
                new("PartLine", "The Line", "What becomes of the armistice line through Zille.", 1, 1, new Opt[]
                {
                    new("Border", "The line of contact becomes the border between Rizia and Wehlen", -1),
                    new("Aure", "Wehlen withdraws from Auremina and the gold hills, and the new line becomes the border", -4, "ZilleWar.War_Objective_Captured_Auremina == false"),
                    new("Reserve", "Rizia keeps its claim to all of Zille, and the line stays an armistice line", 0),
                }),
                new("PartDam", "War Damages", "Who pays for the ruins on either side of the line.", 1, 1, new Opt[]
                {
                    new("DamNone", "Neither side pays", 0),
                    new("DamThem", "Wehlen pays for the shelling of Zille city [+1 Budget Per Turn]", -3),
                    new("DamUs", "Rizia rebuilds the Wehzek villages along the line [-2 Budget]", 2),
                }),
                new("PartTrade", "Trade and Passage", "What crosses the line and the strait once the guns are gone.", 0, -1, new Opt[]
                {
                    new("Passage", "Rizian ships pass the Strait of Kreleck free of Wehzek tolls [+1 Budget Per Turn]", -2),
                    new("Oil", "Wehzek oil is sold to Rizia again [+1 Energy Per Turn]", -1),
                    new("Families", "Zilleans may cross the line to visit their families [+1 Public Opinion]", -1),
                    new("MaveBack", "Wehzek troops leave Mave Nortus, the Rizian city behind the line", -2, "ZilleWar.MaveNortus_Occupied == true"),
                }),
                new("PartPow", "Prisoners of War", "Both sides hold prisoners from the last offensive.", 1, 1, new Opt[]
                {
                    new("Exchange", "All prisoners are exchanged at once", 1),
                    new("Hold", "Rizia holds its prisoners until Wehlen keeps its word", -1),
                }),
                new("PartAsks", "Wehlen's Requests", "Smolak's own wishes. Each one granted makes the rest easier for him to swallow.", 0, -1, new Opt[]
                {
                    new("Rico", "Ricardus Toras is handed over to Wraclawitz to answer for Smolak Square", 2, "RiziaDLC.Rico_Arrested == true && RiziaDLC.Rico_Extradited == false"),
                    new("Rights", "Wehzeks in Zille city keep their homes and their citizenship", 1),
                    new("Ban", "Rizia's border is closed to the Bludish Freedom Front", 1, $"{BludishBan} == false"),
                    new("Dmz", "No soldier of either side within twenty kilometers of the line [-1 Authority]", 2),
                }),
            }),
        new(PanelLost, "The Terms of Wraclawitz",
            "Wehlen won, and President Smolak knows it. Rizia starts in his debt: only by granting what Wehlen demands can the King buy " +
            "back the terms Rizia cannot live without. Smolak will not sign a draft that leaves Wehlen with less than it is owed.",
            new PageSpec[]
            {
                new("LostZille", "Zille", "Wehlen wants its victory written down.", 1, 1, new Opt[]
                {
                    new("Recognize", "Rizia recognizes Wehzek sovereignty over Zille", 4),
                    new("Reserve", "Rizia keeps its claim to Zille", 0),
                }),
                new("LostRep", "Reparations to Wehlen", "Smolak wants nine billion guilders for the war Rizia started.", 1, 1, new Opt[]
                {
                    new("PayNow", "Nine billion guilders, paid at once [-3 Budget]", 3),
                    new("PayLater", "Paid in installments [-1 Budget Per Turn]", 2),
                    new("PayNone", "Not one guilder", -1),
                }),
                new("LostOurs", "Rizia's Terms", "What Rizia needs back from the war it lost.", 0, -1, new Opt[]
                {
                    new("Mave", "Wehzek troops leave Mave Nortus", -3, "ZilleWar.MaveNortus_Occupied == true"),
                    new("Pow", "Rizian prisoners of war come home before winter", -1),
                    new("Leave", "Rizian families in Zille may leave with their property [+1 Public Opinion]", -1),
                }),
                new("LostAsks", "Wehlen's Demands", "Smolak's further demands. Each one granted makes the rest easier for him to swallow.", 0, -1, new Opt[]
                {
                    new("House", "The Queen Mother's old house in Zille goes to Smolak's son", 1),
                    new("Rico", "Ricardus Toras is handed over to Wraclawitz to answer for Smolak Square", 2, "RiziaDLC.Rico_Arrested == true && RiziaDLC.Rico_Extradited == false"),
                    new("Ban", "Rizia's border is closed to the Bludish Freedom Front", 1, $"{BludishBan} == false"),
                    new("Rights", "Every Wehzek living in Rizia has the same rights as a Rizian", 1, $"{WehzekRights} == false"),
                    new("Dmz", "No Rizian gun within twenty kilometers of the border [-1 Authority]", 2),
                }),
            }),
    };

    /// <summary>The order in which the King strikes his own demands when Smolak refuses: dearest first.</summary>
    private static readonly (string Key, int Refund)[] TrimLib =
        { ("Rep2", 4), ("Demil", 2), ("Oil", 2), ("Rep1", 2), ("PowRansom", 1), ("Gold", 1), ("Assets", 1), ("Renounce", 2) };
    private static readonly (string Key, int Refund)[] TrimPart =
        { ("Aure", 4), ("DamThem", 3), ("Passage", 2), ("Oil", 1), ("Families", 1), ("Hold", 1), ("Border", 1), ("MaveBack", 2) };
    private static readonly (string Key, int Refund)[] TrimLost = { ("Mave", 3), ("Pow", 1), ("Leave", 1), ("PayNone", 1) };

    private static bool _added;

    internal static void ResetAdded() => _added = false;

    public static IEnumerable<string> AllTermVars() =>
        Panels.SelectMany(p => p.Pages).SelectMany(g => g.Options).Select(o => Term(o.Key)).Distinct();

    /// <summary>Adds the three panels, their pages and options to the game's entity lists (after every data reload).</summary>
    public static void Install()
    {
        if (_added || EntityDataManager.PagedDecisionPanelsData == null || EntityDataManager.MultipleChoiceOptionData == null)
        {
            return;
        }

        PagedDecisionPanelData template = null;
        foreach (PagedDecisionPanelData p in EntityDataManager.PagedDecisionPanelsData)
        {
            if (p?.NameInDatabase == "Panel_WehlenNegotiationPromises")
            {
                template = p;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return;
        }

        int n = 0;
        foreach (PanelSpec panel in Panels)
        {
            var pages = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (PageSpec page in panel.Pages)
            {
                string pageName = $"Page_ZilleWar_Treaty_{page.Key}";
                pages.Add(pageName);
                var options = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (Opt o in page.Options)
                {
                    string optName = $"Option_ZilleWar_Treaty_{page.Key}_{o.Key}";
                    options.Add(optName);
                    string sign = o.Lev >= 0 ? $"+{o.Lev}" : o.Lev.ToString();
                    string title = o.Lev == 0 ? o.Title : $"{o.Title} [{sign} Leverage]";
                    var opt = new MultipleChoiceOptionData
                    {
                        Id = Id(optName),
                        NameInDatabase = optName,
                        Path = $"Rizia/Paged Decisions Panel/ZilleWar Treaty/{page.Key}",
                        IsWIP = false,
                        Notes = string.Empty,
                        AppBundleProperties = template.AppBundleProperties,
                        MultipleChoiceOptionProperties = new MultipleChoiceOptionProperties
                        {
                            Title = title,
                            Condition = o.Condition ?? string.Empty,
                            Instruction = $"{Term(o.Key)} = true;" + (o.Lev == 0 ? string.Empty : $"\n{VarLeverage} {(o.Lev > 0 ? "+=" : "-=")} {Math.Abs(o.Lev)};"),
                            PanelBarIncrement = o.Lev,
                            PanelCounterIncrement = 0,
                        },
                    };
                    ZilleOperation.AddByName(EntityDataManager.multipleChoiceOptionData, opt, x => x.NameInDatabase);
                    ZilleOperation.AddByName(EntityDataManager.MultipleChoiceOptionData, opt, x => x.NameInDatabase);
                    n++;
                }

                var pageData = new MultipleChoicePageData
                {
                    Id = Id(pageName),
                    NameInDatabase = pageName,
                    Path = $"Rizia/Paged Decisions Panel/ZilleWar Treaty/{page.Key}",
                    IsWIP = false,
                    Notes = string.Empty,
                    AppBundleProperties = template.AppBundleProperties,
                    MultipleChoicePageProperties = new MultipleChoicePageProperties
                    {
                        Title = page.Title,
                        Subtitle = page.Subtitle,
                        Options = options,
                        MaximumChoiceCount = page.Max,
                        MinimumChoiceCount = page.Min,
                    },
                };
                ZilleOperation.AddByName(EntityDataManager.multipleChoicePageData, pageData, x => x.NameInDatabase);
                ZilleOperation.AddByName(EntityDataManager.MultipleChoicePageData, pageData, x => x.NameInDatabase);
            }

            var panelData = new PagedDecisionPanelData
            {
                Id = Id(panel.Name),
                NameInDatabase = panel.Name,
                Path = "Rizia/Paged Decisions Panel/ZilleWar Treaty",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template.AppBundleProperties,
                PagedDecisionPanelProperties = new PagedDecisionPanelProperties
                {
                    Pages = pages,
                    Title = panel.Title,
                    Description = panel.Description,
                    PanelBarMax = 10,
                    PanelBarMin = -10,
                    PanelBarText = "Negotiation Leverage",
                    PanelBarVariable = VarLeverage,
                    PanelCounterText = string.Empty,
                    ConversationOnFinish = string.Empty,
                    PanelCounterVariable = string.Empty,
                    PreventFinishOnNoChange = false,
                    PagedDecisionPanelOnFinish = string.Empty,
                },
            };
            ZilleOperation.AddByName(EntityDataManager.pagedDecisionPanelsData, panelData, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.PagedDecisionPanelsData, panelData, x => x.NameInDatabase);
        }

        _added = true;
        Core.Log.Msg($"Treaty panels installed ({Panels.Length} panels, {n} terms).");
    }

    private static string Id(string name) => $"0x5A12{(uint)name.GetHashCode():X8}";

    // ------------------------------------------------------------------ registration

    public static void Register()
    {
        foreach (string v in new[] { VarLeverage, VarSigned, VarKind, VarPeace, VarThreat, VarTrimmed, VarMapApplied, VarAskedWon, VarAskedFiles,
                     VarRefused, VarNewsDone, ModReparations, ModOil, ModStrait })
        {
            Variables.Register(v);
        }

        // Written by the panel options' expresso instructions ("X = true;", "X += n;"), so plain Variable["x"]: the nil-safe
        // read forms are not assignable. Start() sets every term and the leverage before the panel opens.
        foreach (string v in AllTermVars())
        {
            Variables.Register(v);
            Vanilla.ExposePlain(v);
        }

        foreach (string v in new[] { VarSigned, VarPeace })
        {
            Vanilla.Expose(v);
        }

        Vanilla.ExposePlain(VarLeverage);
        ConversationRegistry.RegisterConversation(Conv);
        RegisterTalk();
    }

    /// <summary>
    /// The treaty table opens the turn after the war ends, once that turn's Derdian talks and Rumburg meeting are behind
    /// (step 9 on: missiles and nerve agent may be in hand by then), or at once on any later turn.
    /// </summary>
    private static void Schedule()
    {
        if (!Variables.GetBool(WarFlags.Declared) || WarFlow.AtWar || Variables.GetBool(LateT8.VarSmolakT8))
        {
            return;
        }

        int turn = SuzerainModdingKit.GameState.CurrentTurnNum ?? 0, step = SuzerainModdingKit.GameState.CurrentStepNum ?? 0;
        int end = Variables.GetInt(VictoryParade.VarEndTurn);
        int target = end > 0 ? end + 1 : 7;
        if (turn < target || (turn == target && step < 9))
        {
            return;
        }

        const string frag = "ZilleWar.Late_SmolakT8";
        if (SuzerainModdingKit.GameState.StoryFragmentExistsInCurrentStep(frag))
        {
            return;
        }

        (string title, string sub) = LateState.Liberated ? ("The Treaty of Kreleck", "A Wehzek ferry, Strait of Kreleck")
            : LateState.Partial ? ("The Mave Nortus Treaty", Variables.GetBool(WarFlags.Occupied("Mave Nortus")) ? "Schoolhouse on the armistice line" : "Schoolhouse north of Mave Nortus")
            : ("The Terms of Wraclawitz", "Presidential Palace, Wraclawitz");
        try
        {
            SuzerainModdingKit.GameState.AddCustomConversation(Conv);
            if (SuzerainModdingKit.GameState.AddCustomStoryFragment(new CustomConversationFragment(frag, SuzerainStoryPackInfo.Rizia, "Rizia_Country_Wehlen", title, sub,
                    Il2Cpp.ConversationData.ConversationType.ForeignPolicyAndTrade, Conv)))
            {
                Variables.Set(LateT8.VarSmolakT8, true);
                Core.Log.Msg($"The peace talks with Wehlen open (turn {turn}, step {step}).");
            }
        }
        catch (Exception ex)
        {
            Core.Log.Error($"Adding the treaty talks failed: {ex}");
        }
    }

    public static void OnStep()
    {
        Install();
        Schedule();
        // Unset, the kind reads "nil": the talks are offered but not yet held.
        if (Variables.GetBool(LateT8.VarSmolakT8) && !Variables.GetBool(VarNewsDone) && Variables.GetString(VarKind) is { Length: > 0 } kind && kind != "nil")
        {
            Variables.Set(VarNewsDone, true);
            try
            {
                NewsWire.Batch(() => Publish(kind));
            }
            catch (Exception ex)
            {
                Core.Log.Error($"Treaty news failed: {ex}");
            }
        }

        if (!Variables.GetBool(VarSigned) || Variables.GetBool(VarMapApplied))
        {
            return;
        }

        Variables.Set(VarMapApplied, true);
        if (Variables.GetBool(Term("Aure")))
        {
            ZilleCampaign.CedeTown("Auremina", toRizia: true);
        }

        if ((Variables.GetBool(Term("Mave")) && Variables.GetString(VarKind) is "recognized" or "terms") || Variables.GetBool(Term("MaveBack")))
        {
            ZilleCampaign.CedeTown("Mave Nortus", toRizia: true);
        }
    }

    // ------------------------------------------------------------------ the conversation

    private static string Trim((string Key, int Refund)[] order)
    {
        string s = string.Empty;
        foreach ((string key, int refund) in order)
        {
            s += $"if ({Lev} or 0) < 0 and {Is(Term(key))} then {V(Term(key))} = false; {AddLev(refund)}{V(VarTrimmed)} = true end; ";
        }

        return s;
    }

    private static string Start()
    {
        // Reset every term (the panel sets the chosen ones), then the opening leverage.
        string reset = string.Concat(AllTermVars().Select(v => $"{V(v)} = false; "));
        return reset + $"{V(VarThreat)} = \"\"; {V(VarTrimmed)} = false; {V(VarAskedWon)} = false; {V(VarAskedFiles)} = false; {V(VarRefused)} = false; " +
               $"if {Lib} then {Lev} = 3 elseif {Part} then {Lev} = 0 else {Lev} = -4 end; " +
               $"if {Cease} and not ({Lib}) then {AddLev(-1)}end; " +
               "if Variable[\"RiziaDLC.CountryRelation_Rumburg_Friendly\"] == true or Variable[\"RiziaDLC.CountryRelation_Rumburg_Allied\"] == true then " + AddLev(1) + "end; " +
               "if Variable[\"RiziaDLC.CountryRelation_Valgsland_Hostile\"] == true then " + AddLev(-1) + "end; " +
               $"if {PalesBusy} then {AddLev(-1)}end; " +
               $"{Lev} = ({Lev} or 0) + (Variable[\"ZilleWar.Decree_TreatyLeverage\"] or 0); " +
               "PlayLoopedMusic(\"R_Negotiations\",\"R_Negotiations_Loop\"); AddConversant(\"Wiktor Smolak (R)\");";
    }

    private static void RegisterTalk()
    {
        var t = new Talk(Conv, "ZilleWar.Treaty.");
        t.Logic("Start", new[] { "Lib1", "Part1", "Part1H", "Lost1" }, hooks: AtStart(Conv), script: Start());

        string maveHeld = WarFlags.L(WarFlags.Occupied("Mave Nortus"));
        string maveOurs = WarFlags.LNot(WarFlags.Occupied("Mave Nortus"));

        // ---- victory: the Kreleck ferry
        t.Line("Lib1", Narr, "The Wehzek ferry rode at anchor in the middle of the Strait of Kreleck, exactly halfway between Nanta and the Wehzek shore. Measuring the halfway point had taken three days of negotiation.",
            new[] { "Lib2" }, condition: Lib);
        t.Line("Lib2", Narr, "President Smolak was waiting in the passenger lounge, beneath a portrait of himself that someone had forgotten to take down. He did not get up. Laurento laid the draft treaty on the table between us.",
            new[] { "Lib3" });
        t.Line("Lib3", Smolak, "\"Your Majesty. You took my villa in Zille. I would like my khisha pipes back. They were a present from my son.\"", new[] { "Lib4" });
        t.Line("Lib4", Smolak, "\"I am not here to beg. Wehlen does not beg. I am here because my soldiers are in your camps, eating your bread and learning your songs, and their mothers write to me every day.\"", new[] { "Lib5" });
        t.Line("Lib5", Smolak, "\"So. You have a treaty. I have a pen. Tell me what you want, and I will tell you what it costs.\"", new[] { "Hub" });

        // ---- armistice: the schoolhouse commission
        const string commission = "under an Alliance of Nations flag that had been ironed for the occasion. An observer from President Nines' office sat between the two delegations with a stopwatch.";
        t.Line("Part1", Narr, "The armistice commission met in a village schoolhouse north of Mave Nortus, " + commission, new[] { "Part2" }, condition: LK.And(Part, maveOurs));
        t.Line("Part1H", Narr, "The armistice commission met in a village schoolhouse on the armistice line, within sight of Wehzek-held Mave Nortus. " + commission.Replace("under an Alliance of Nations flag that had been ironed for the occasion.", "An Alliance of Nations flag, ironed for the occasion, hung over the door."),
            new[] { "Part2" }, condition: LK.And(Part, maveHeld));
        t.Line("Part2", Narr, "President Smolak had come in person. So had I. Neither of us had planned to, and neither of us would admit it.", new[] { "Part3" });
        t.Line("Part3", Smolak, "\"Your Majesty. Look at us. Two grown men and a schoolroom.\"", new[] { "Part4", "Part4A" });
        t.Line("Part4", Smolak, "\"You sit in Zille city. I sit in the hills above it. My men watch your men shaving in the morning. Either we make that line a border, or we keep paying for it.\"",
            new[] { "Part5" }, condition: $"not ({LK.Auremina})");
        t.Line("Part4A", Smolak, "\"You sit in Zille city and in my gold hills. I sit in Isor and Nanta, and my gunboats watch your men on the beach. Either that line becomes a border, or we keep paying for it.\"",
            new[] { "Part5" }, condition: LK.Auremina);
        t.Line("Part5", Smolak, "\"I came with a list. You came with a list. Let us see whose list is longer.\"", new[] { "CeaseL", "PalesL", "Hub" });
        t.Line("CeaseL", Smolak, "\"You asked for a ceasefire before the guns were quiet. We remember who asked.\"", new[] { "PalesL", "Hub" }, condition: Cease);
        t.Line("PalesL", Smolak, "\"And half your army is in Pales. I read the newspapers too.\"", new[] { "Hub" }, condition: PalesBusy);

        // ---- defeat: summoned to Wraclawitz
        t.Line("Lost1", Narr, "The message from Wraclawitz had been phrased as an invitation. Laurento advised me to treat it as one, and to go anyway.", new[] { "Lost2" }, condition: Lost);
        t.Line("Lost2", Narr, "Smolak received me in the presidential palace, in a room hung with photographs of his army in Zille. He had chosen the chairs himself. Mine was lower.", new[] { "Lost3" });
        t.Line("Lost3", Smolak, "\"Your Majesty! Welcome to Wehlen. I wanted you to see it for yourself. You were once so eager to visit.\"", new[] { "Lost4" });
        t.Line("Lost4", Smolak, "\"I have written the terms myself. Recognition, reparations, a few small kindnesses. Sign what I need, and perhaps there will be room for what you need. That is how peace works, when one side has won.\"",
            new[] { "CeaseL", "PalesL", "Hub" });

        // ---- the hub: what the King brings to the table before the draft
        t.Logic("Hub", new[] { "AskWon", "AskHeld", "AskFiles", "ThreatMenu", "Draft" });
        t.Choice("AskWon", "\"Your garrison surrendered, Mr. Smolak. Wehlen is in no position to haggle.\"", new[] { "AskWon2" },
            condition: LK.And(Lib, Not(VarAskedWon)), script: Set(VarAskedWon, "true") + AddLev(1));
        t.Line("AskWon2", Smolak, "\"No. But I am in a position to be stubborn, and you would be amazed how far that goes. Still. Point taken.\"", new[] { "Hub" });
        t.Choice("AskHeld", "\"My flag flies over Zille city, and it is staying there. Start from that.\"", new[] { "AskHeld2" },
            condition: LK.And(Part, Not(VarAskedWon)), script: Set(VarAskedWon, "true") + AddLev(1));
        t.Line("AskHeld2", Smolak, "\"Over half a city. I have flown flags over worse. Very well, we start from that.\"", new[] { "Hub" });
        t.Choice("AskFiles", "\"Before we begin, read this. Smolak Square was not Rizia's doing. It was a Lespian officer's.\"", new[] { "AskFiles2" },
            condition: LK.And(Files, Not(VarAskedFiles)), script: Set(VarAskedFiles, "true") + AddLev(1));
        t.Line("AskFiles2", Narr, "Smolak read the file twice. When he looked up, something in his face had shifted, though not very far.", new[] { "AskFiles3" });
        t.Line("AskFiles3", Smolak, "\"Alvarez. Of course it was Alvarez. Everybody's friend.\" He folded the pages into his coat. \"I will remember who showed me this. It does not change who sent the tanks.\"", new[] { "Hub" });

        // threats, one per negotiation
        t.Choice("ThreatMenu", "\"Let me explain what happens if there is no treaty.\"", new[] { "Th" }, condition: $"{V(VarThreat)} == \"\" and not ({Lost} and not ({Missiles}))");
        t.Logic("Th", new[] { "ThInvade", "ThMissile", "ThGas", "ThBack" });
        t.Choice("ThInvade", "\"My army is a day's sail from your coast. The next landing will be on your beaches, not mine.\"", new[] { "ThInvadeBusy", "ThInvade2" },
            condition: $"not ({Lost})");
        t.Line("ThInvadeBusy", Smolak, "\"With what army, Your Majesty? Half of it is in Pales, and the other half is guarding the half that is in Pales. Try again.\"", new[] { "Hub" },
            condition: PalesBusy);
        t.Line("ThInvade2", Smolak, "\"Beaches. He threatens me with beaches.\" He drummed his fingers on the table. \"My coast is long and my navy is short. You know it, and I know you know it. Fine.\"", new[] { "Hub" },
            script: Set(VarThreat, "\"invasion\"") + $"if {Lib} then {AddLev(2)}else {AddLev(1)}end; " + Add(GlobalImage, -1));
        t.Choice("ThMissile", "\"Rizia has Derdian missiles now. From our side of the border they reach Wraclawitz. I would rather not find out how accurately.\"", new[] { "ThMissile2" },
            condition: Missiles, script: Set(VarThreat, "\"missiles\"") + $"if {Lib} then {AddLev(3)}else {AddLev(2)}end; " + Add(GlobalImage, -1));
        t.Line("ThMissile2", Narr, "For a moment Smolak said nothing at all. Then he laughed, too loudly, and the laugh did not reach his eyes.", new[] { "ThMissile3" });
        t.Line("ThMissile3", Smolak, "\"Azmal's toys. He sells them to anyone who hates me, which is a very large market. Very well, Your Majesty. You have my attention.\"", new[] { "Hub" });
        t.Choice("ThGas", "\"And Queen Beatrice was kind enough to fill some of the warheads with something other than explosives.\"", new[] { "ThGas2" },
            condition: $"{Missiles} and {NerveGas}",
            script: Set(VarThreat, "\"gas\"") + $"if {Lib} then {AddLev(5)}else {AddLev(4)}end; " + Add(GlobalImage, -3) +
                    Add("RiziaDLC.LeaningValue_Democratic", -1) + Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("ThGas2", Narr, "The observer's pen stopped moving. Laurento looked at me as though he had never seen me before.", new[] { "ThGas3" });
        t.Line("ThGas3", Smolak, "\"You would gas my capital.\" It was not a question. \"Eight hundred thousand people. My wives. My son.\"", new[] { "ThGas4" });
        t.Line("ThGas4", Smolak, "\"I believe you, Your Majesty. I have never said worse to a king. Name your terms.\"", new[] { "ThGas5" });
        t.Line("ThGas5", Laurento, "Laurento leaned close, his voice barely a breath. \"Whatever he signs today, Your Majesty, the whole world will know by Sunday how it was signed.\"", new[] { "Hub" });
        t.Choice("ThBack", "\"On second thought, let us keep this civil.\"", new[] { "Hub" }, script: Set(VarThreat, "\"\""));

        // ---- the draft
        string panelFor = $"if {Lib} then ShowPagedDecisionsPanel(\"{PanelLib}\") elseif {Part} then ShowPagedDecisionsPanel(\"{PanelPart}\") else ShowPagedDecisionsPanel(\"{PanelLost}\") end;";
        t.Choice("Draft", "\"Let us go through the draft.\"", new[] { "Draft2" });
        t.Line("Draft2", Narr, "Laurento uncapped his pen and turned the draft toward me. We went through it clause by clause.", new[] { "Panel" });
        t.Logic("Panel", new[] { "Read" }, script: panelFor);
        t.Line("Read", Narr, "Laurento noted each decision in the margin. When we had finished, he slid the draft across the table, and Smolak read it slowly, moving his lips.", new[] { "ReadRico", "Verdict" });
        t.Line("ReadRico", Smolak, "\"Keep your files. I want the man who spoke for the bombers.\"", new[] { "Verdict" },
            condition: $"{Is(Term("Rico"))} and {Is(VarAskedFiles)}");

        // ---- the verdict
        t.Logic("Verdict", new[] { "Accept", "Refuse" });
        t.Logic("Accept", new[] { "Sign" }, condition: $"({Lev} or 0) >= 0");
        t.Line("Refuse", Smolak, "\"No.\" He pushed the draft back across the table. \"I cannot go home with this. My generals would hang me with it.\"", new[] { "R1", "R2", "R3" },
            condition: $"({Lev} or 0) < 0", script: Set(VarRefused, "true"));
        t.Choice("R1", "\"Then show me which clauses you can't swallow, and I'll strike them.\"", new[] { "R1b" },
            script: $"if {Lib} then {Trim(TrimLib)}elseif {Part} then {Trim(TrimPart)}else {Trim(TrimLost)}end; ");
        t.Line("R1b", Narr, "We went back through the draft line by line. Laurento struck out what Smolak refused, and wrote the date beside each deletion.", new[] { "R1c", "R1d" });
        t.Logic("R1c", new[] { "Sign" }, condition: $"({Lev} or 0) >= 0");
        t.Line("R1d", Smolak, "\"Even now, it is not enough. Whatever is left on this paper is still more than Wehlen can give.\"", new[] { "R2", "R3" }, condition: $"({Lev} or 0) < 0");
        t.Choice("R2", "\"Then let me put it another way.\"", new[] { "Th2" }, condition: $"{V(VarThreat)} == \"\" and ((not ({Lost}) and not ({PalesBusy})) or {Missiles})");
        t.Logic("Th2", new[] { "Th2Invade", "Th2Missile", "Th2Gas" });
        t.Choice("Th2Invade", "\"If you won't sign, my army will make the case on your coast.\"", new[] { "ThInvade2b" },
            condition: $"not ({Lost}) and not ({PalesBusy})",
            script: Set(VarThreat, "\"invasion\"") + $"if {Lib} then {AddLev(2)}else {AddLev(1)}end; " + Add(GlobalImage, -1));
        t.Line("ThInvade2b", Smolak, "\"On my coast.\" He looked at the map, not at me. \"Let me read it again.\"", new[] { "Verdict2" });
        t.Choice("Th2Missile", "\"Derdian missiles reach Wraclawitz from our side of the line. Sign.\"", new[] { "ThMissile2b" }, condition: Missiles,
            script: Set(VarThreat, "\"missiles\"") + $"if {Lib} then {AddLev(3)}else {AddLev(2)}end; " + Add(GlobalImage, -1));
        t.Line("ThMissile2b", Smolak, "\"Azmal's toys.\" He was quiet for a long moment. \"Very well. Let me read it again.\"", new[] { "Verdict2" });
        t.Choice("Th2Gas", "\"Rumburgian nerve agent, in Derdian warheads, over Wraclawitz. Sign.\"", new[] { "ThGas2b" }, condition: $"{Missiles} and {NerveGas}",
            script: Set(VarThreat, "\"gas\"") + $"if {Lib} then {AddLev(5)}else {AddLev(4)}end; " + Add(GlobalImage, -3) +
                    Add("RiziaDLC.LeaningValue_Democratic", -1) + Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", -1));
        t.Line("ThGas2b", Narr, "Smolak went white. The observer set down his stopwatch. Laurento closed his eyes.", new[] { "ThGas3b" });
        t.Line("ThGas3b", Smolak, "\"You would really do it. God help us both.\"", new[] { "Verdict2" });
        t.Logic("Verdict2", new[] { "Sign2", "Refuse2" });
        t.Logic("Sign2", new[] { "Sign" }, condition: $"({Lev} or 0) >= 0");
        t.Line("Refuse2", Smolak, "\"Threaten all you like. A man cannot sign what his country cannot pay.\"", new[] { "R1", "R3" }, condition: $"({Lev} or 0) < 0");
        t.Choice("R3", "\"Then there is no treaty.\"", new[] { "NoDeal" });

        // ---- no treaty
        string noDeal = Set(VarSigned, "false") + Set(VarKind, "\"none\"") + Set(VarPeace, "false") +
                        $"if {Lib} then {V(LateT8.VarPrisoners)} = \"held\" end; " +
                        $"if {Lost} then {V(LateT8.VarTerms)} = \"refused\" end; " + Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -1);
        t.Line("NoDeal", Smolak, "\"Then we go home with nothing, and our grandchildren can try again. I will see you in Konstantium, Your Majesty. Bring a better lawyer.\"",
            new[] { "ByeLib", "ByePart", "ByeLost" }, script: noDeal);

        // ---- signing
        t.Logic("Sign", new[] { "SignGas", "SignMissile", "SignInvade", "SignLib", "SignPart", "SignLost" }, script: SignScript());
        t.Line("SignGas", Smolak, "\"There. My name, under a gun. I hope it makes you happy, Your Majesty. I will remember this for the rest of my life, which you have just made shorter.\"",
            new[] { "SignNote" }, condition: $"{V(VarThreat)} == \"gas\"");
        t.Line("SignMissile", Smolak, "\"Signed. Tell Azmal his missiles have made their first sale without leaving the ground. He will want a commission.\"",
            new[] { "SignNote" }, condition: $"{V(VarThreat)} == \"missiles\"");
        t.Line("SignInvade", Smolak, "\"Signed. Keep your boats on your side of the water, Your Majesty. My beaches are for my children.\"",
            new[] { "SignNote" }, condition: $"{V(VarThreat)} == \"invasion\"");
        t.Line("SignLib", Smolak, "\"Signed. I do this for my soldiers, not for you. Tell your mother I kept the garden. Mostly.\"", new[] { "SignNote" }, condition: Lib);
        t.Line("SignPart", Smolak, "\"Signed. A border through the middle of a province. Our grandfathers would have called it a war. We call it a treaty. That is progress, I suppose.\"",
            new[] { "SignNote" }, condition: Part);
        t.Line("SignLost", Smolak, "\"Signed. You see? We can still do business, you and I. It is only that I do it better.\"", new[] { "SignNote" }, condition: Lost);
        t.Line("SignNote", Laurento, "\"It is done, Your Majesty.\" Laurento blotted the signatures carefully. \"Whatever it cost, there is a treaty.\"",
            new[] { "ByeLib", "ByePart", "ByeLost" });

        // ---- goodbyes
        t.Line("ByeLib", Narr, "The ferry's horn sounded twice. By the time I reached the deck, his launch was already halfway to the Wehzek shore.", new[] { "End" }, condition: Lib);
        t.Line("ByePart", Narr, "The observer stopped his watch and wrote down the time. Outside, a child was selling apples to the soldiers of both armies.", new[] { "End" }, condition: Part);
        t.Line("ByeLost", Narr, "On the drive to the airfield, Wraclawitz was hung with Wehzek flags. On the wall of the railway station, someone had chalked the names of the men who did not come home from Zille.", new[] { "End" });
        t.End();
        t.Register();
    }

    // ------------------------------------------------------------------ the press

    private static bool On(string key) => Variables.GetBool(Term(key));

    /// <summary>The towns of the province Wehlen still holds under a border treaty (Auremina changes hands only if the treaty says so).</summary>
    private static string WehzekTowns()
    {
        var left = new List<string>();
        foreach (string town in new[] { "Auremina", "Isor", "Nanta" })
        {
            bool rizian = Variables.GetBool(ZilleOperation.CapturedVar(town)) || (town == "Auremina" && On("Aure"));
            if (!rizian)
            {
                left.Add(town);
            }
        }

        return left.Count switch
        {
            0 => string.Empty,
            1 => left[0],
            2 => left[0] + " and " + left[1],
            _ => left[0] + ", " + left[1] + " and " + left[2],
        };
    }

    private static void Publish(string kind)
    {
        string threat = Variables.GetString(VarThreat) ?? string.Empty;
        int rep = Variables.GetInt(ModReparations);
        string repText = rep >= 2 ? " Wehlen will pay heavy reparations every month for years to come." : rep == 1 ? " Wehlen will pay reparations every month." : string.Empty;
        string extras = string.Join(" ", new[]
        {
            On("Gold") ? "The Auremina gold shipped north by the Wehzek garrison is to be returned." : null,
            On("Oil") ? "Wehzek oil will flow south again, at a price Wraclawitz did not choose." : null,
            On("Demil") ? "No Wehzek warship may enter the Strait of Kreleck." : null,
            On("Rico") ? "Ricardus Toras, the King's cousin, is to be handed over to Wehzek justice." : null,
            On("Villa") ? "President Smolak's villa in Zille is returned to him." : null,
            On("Rights") && !LateState.Lost ? "Wehzek families in Zille keep their homes and their citizenship." : null,
        }.Where(x => x != null));

        switch (kind)
        {
            case "renounced":
            case "border" when Variables.GetBool(ZilleCampaign.VarLiberated):
                NewsWire.Publish("Treaty", "RH", kind == "renounced" ? "Wehlen Renounces Zille Forever" : "Wehlen Recognizes Rizia's Border at the Strait",
                    NewsWire.P(
                        "On a Wehzek ferry anchored in the middle of the Strait of Kreleck, President Wiktor Smolak has put his name to the Treaty " +
                        "of Kreleck. " + (kind == "renounced"
                            ? "Wehlen renounces every claim to Zille, now and forever, and the province is Rizian by law as well as by arms."
                            : "Wehlen recognizes the strait as the border between the two countries, and Zille is Rizian by law as well as by arms.") + repText,
                        extras.Length > 0 ? extras : "His Majesty's terms were firm, and they were met.",
                        "Thirty years after a lease that should never have been signed, the last word on Zille was written by a Rizian King."));
                NewsWire.Publish("Treaty", "G", "Wehlen Signs the Treaty of Kreleck", NewsWire.P(
                    "Wehlen and Rizia have signed a treaty ending the war over Zille. Wraclawitz gives up the province" + (rep > 0 ? " and pays reparations" : string.Empty) +
                    "; prisoners of war are to go home " + (On("PowFree") ? "at once." : On("PowRansom") ? "once Wehlen ransoms its officers." : "as the treaty is honored."),
                    "Diplomats in Konstantium call the terms the harshest imposed on any Merkopan state since the Reclamation War. President Smolak told " +
                    "reporters on the quay that he had signed \"for the mothers of Wehlen, and for no one else.\""));
                break;
            case "border":
                NewsWire.Publish("Treaty", "RH", "A Border at Last Through Zille", NewsWire.P(
                    "In a schoolhouse near Mave Nortus, under the flag of the Alliance of Nations, His Majesty and President Smolak have signed " +
                    "the Mave Nortus Treaty. The armistice line through Zille becomes the border of the Kingdom" +
                    (On("Aure") ? ", and Wehzek troops are to leave Auremina and the gold hills." : ".") + (On("MaveBack") ? " Wehzek troops also leave Mave Nortus." : string.Empty) + repText,
                    (WehzekTowns().Length > 0 ? "It is not all of Zille. " : "It is nearly all of Zille. ") + "It is Zille city, its harbor and its people, home for good, and a border no Wehzek general may cross again."));
                NewsWire.Publish("Treaty", "V", "Half a Province, Signed and Sealed", NewsWire.P(
                    "The line our soldiers held when the guns stopped is a border now. On one side of it, families who have been Rizian for a generation " +
                    "will be Rizian in law. On the other, families who wanted the same thing will live under Wehzek law." +
                    (On("Families") ? " At least the treaty lets them cross to see each other." : string.Empty),
                    "Treaties are written for maps. This one will be read in kitchens on both sides of a schoolhouse."));
                NewsWire.Publish("Treaty", "G", "Rizia and Wehlen Turn the Armistice Line into a Border", NewsWire.P(
                    "The Mave Nortus Treaty fixes the border between Rizia and Wehlen along the line of contact in Zille" +
                    (On("Aure") ? ", moved north to give Auremina and its gold hills to Rizia." : ".") +
                    (WehzekTowns().Length > 0 ? " " + WehzekTowns() + (WehzekTowns().Contains(" and ") ? " remain Wehzek." : " remains Wehzek.") : string.Empty) +
                    " Alliance observers will watch the new border for its first year."));
                break;
            case "accord":
                NewsWire.Publish("Treaty", "G", "Rizia and Wehlen Sign an Accord, but Leave Zille Unsettled", NewsWire.P(
                    "The commission at Mave Nortus has produced an accord on prisoners and damages, but no border. Rizia keeps its claim to all of " +
                    "Zille, and the line through the province remains an armistice line, watched by two armies and an Alliance observer with a stopwatch."));
                break;
            case "recognized":
                NewsWire.Publish("Treaty", "RH", "The King Signs for Peace", NewsWire.P(
                    "In Wraclawitz, His Majesty has signed the treaty that ends the war with Wehlen. Rizia recognizes Wehzek sovereignty over Zille, " +
                    "and in return the Kingdom has peace on its northern border" + (On("Mave") ? " and Mave Nortus back from Wehzek hands." : "."),
                    "It is a hard peace. It is also the end of the funerals, and the Herald is glad of that, and makes no apology for it."));
                NewsWire.Publish("Treaty", "V", "The Price of Wraclawitz", NewsWire.P(
                    "Zille is Wehzek, and the King has said so in writing. " + (On("PayNow") ? "Nine billion guilders went with the signature. " :
                        On("PayLater") ? "The bill will arrive every month for years. " : string.Empty) +
                    "The war that was to bring Zille home has instead made its loss permanent.",
                    "Somewhere in Zille, a family that hung a Rizian flag in their window last spring is taking it down tonight."));
                NewsWire.Publish("Treaty", "G", "Rizia Recognizes Wehzek Zille", NewsWire.P(
                    "Rizia has formally recognized Wehzek sovereignty over Zille in a treaty signed in Wraclawitz, ending the war on Wehlen's terms. " +
                    "Contanan diplomats called it \"a return to the facts.\""));
                break;
            case "terms":
                NewsWire.Publish("Treaty", "G", "Rizia Signs Terms in Wraclawitz but Keeps Its Claim", NewsWire.P(
                    (On("PayNone")
                        ? "Rizia has signed Wehzek terms in Wraclawitz but refused to pay a guilder in reparations, and refused to recognize Wehzek sovereignty over Zille. "
                        : "Rizia has accepted Wehzek terms on prisoners and reparations in Wraclawitz, but refused to recognize Wehzek sovereignty over Zille. ") +
                    "President Smolak called the claim \"a king's hobby, like stamps.\"" + (On("Mave") ? " Wehzek troops leave Mave Nortus." : string.Empty)));
                break;
            default:
                NewsWire.Publish("Treaty", "G", "Talks on Zille Collapse", NewsWire.P(
                    "Negotiations between Rizia and Wehlen have ended without a treaty. President Smolak said he would take the matter to the " +
                    "Alliance of Nations; the Rizian delegation left without comment."));
                break;
        }

        if (threat == "gas")
        {
            NewsWire.Publish("TreatyThreat", "G", "Rizia Threatened Gas Attack on Wraclawitz, Diplomats Say", NewsWire.P(
                "The King of Rizia threatened to strike Wraclawitz with nerve agent carried by Derdian missiles during the Zille negotiations, " +
                "according to diplomats briefed by the Wehzek delegation. Rizian officials have not denied it.",
                "The agent is said to be of Rumburgian origin. Thornbourgh has declined to comment. In Konstantium, three delegations have asked " +
                "for an emergency session."));
            NewsWire.Report("TreatyThreat", SuzerainTokenName.RiziaCountryValgsland, "Valgsland Calls for Action over the Gas Threat", NewsWire.P(
                "Chancellor Hegel has called the threat against Wraclawitz \"the moment the Rizian monarchy showed the world what it is,\" " +
                "and will propose sanctions at the next session of the Alliance.",
                "Our embassy reports that the Valgslandian press now refers to His Majesty as \"the Gas King.\""));
            NewsWire.Report("TreatyThreat", SuzerainTokenName.RiziaCountryRumburg, "Thornbourgh Is Embarrassed", NewsWire.P(
                "Queen Beatrice's government has let it be known, privately, that Rumburg did not supply Rizia with a nerve agent so that it " +
                "could be named across a negotiating table. Our ambassador was received at the Exchequer for eleven minutes."));
        }
        else if (threat == "missiles")
        {
            NewsWire.Publish("TreatyThreat", "G", "Derdian Missiles at the Negotiating Table", NewsWire.P(
                "Rizia's negotiators reminded Wehlen during the Zille talks that Derdian missiles in Rizian hands can reach Wraclawitz, " +
                "diplomats say. Supreme Wiseman Jorga Azmal of Derdia called it \"a fine advertisement.\""));
        }
    }

    /// <summary>What a signed treaty does: every chosen term, the kind of peace it makes, and the old meeting's variables.</summary>
    private static string SignScript()
    {
        string on(string key) => Is(Term(key));
        return Set(VarSigned, "true") +
               // the kind of treaty
               $"if {Lib} then if {on("Renounce")} then {V(VarKind)} = \"renounced\" else {V(VarKind)} = \"border\" end; {V(VarPeace)} = true " +
               $"elseif {Part} then if {on("Reserve")} then {V(VarKind)} = \"accord\"; {V(VarPeace)} = false else {V(VarKind)} = \"border\"; {V(VarPeace)} = true end " +
               $"else if {on("Recognize")} then {V(VarKind)} = \"recognized\"; {V(VarPeace)} = true else {V(VarKind)} = \"terms\"; {V(VarPeace)} = false end end; " +
               // money and goods
               $"if {on("Rep2")} then {V(ModReparations)} = 2 elseif {on("Rep1")} or {on("DamThem")} then {V(ModReparations)} = 1 end; " +
               $"if {on("Gold")} then {Add(Budget, 3)}end; if {on("Assets")} then {Add(Budget, 2)}end; if {on("PowRansom")} then {Add(Budget, 2)}end; " +
               $"if {on("Oil")} then {V(ModOil)} = 1 end; if {on("Passage")} then {V(ModStrait)} = 1 end; " +
               $"if {on("DamUs")} then {Add(Budget, -2)}end; if {on("PayNow")} then {Add(Budget, -3)}end; " +
               $"if {on("PayLater")} then {V(WorldModifiers.WehlenReparations)} = -1 end; " +
               $"if {on("Demil")} then {Add(Authority, 1)}end; if {on("Dmz")} then {Add(Authority, -1)}end; " +
               $"if {on("Families")} or {on("Leave")} then {Add(Opinion, 1)}end; " +
               // Wehlen's requests
               $"if {on("Rico")} then Variable[\"RiziaDLC.Rico_Extradited\"] = true; EnableCodexEntry(\"Characters_Family_RicoToras_Extradited\") end; " +
               $"if {on("Ban")} then Variable[\"{BludishBan}\"] = true end; " +
               $"if {on("Rights")} and {Lost} then Variable[\"{WehzekRights}\"] = true end; " +
               // the old meeting's variables, read by the Alliance session, Turn 9 and the endings
               $"if {on("PowFree")} then {V(LateT8.VarPrisoners)} = \"released\" elseif {on("PowPaid")} then {V(LateT8.VarPrisoners)} = \"held\" " +
               $"elseif {on("PowRansom")} then {V(LateT8.VarPrisoners)} = \"ransomed\" elseif {on("Exchange")} then {V(LateT8.VarPrisoners)} = \"exchanged\" " +
               $"elseif {on("Hold")} then {V(LateT8.VarPrisoners)} = \"held\" end; " +
               $"if {Lost} then if {on("Pow")} then {V(LateT8.VarPrisoners)} = \"released\" else {V(LateT8.VarPrisoners)} = \"held\" end end; " +
               $"if {on("PayNow")} or {on("PayLater")} then {V(LateT8.VarTerms)} = \"paid\" elseif {on("PayNone")} then {V(LateT8.VarTerms)} = \"refused\" " +
               $"elseif {Part} then {V(LateT8.VarTerms)} = \"line\" end; " +
               $"if {on("House")} then {V(LateT8.VarEstelaHome)} = true end; " +
               $"if {V(VarKind)} == \"recognized\" then {V(LateT9.VarRecognized)} = true end; " +
               $"if {Part} and {V(VarKind)} == \"border\" then {V(LateT9.VarLinePermanent)} = true end; " +
               // peace with Wehlen: Unfriendly, not Hostile (LateState holds it there)
               $"if {V(VarPeace)} == true then Variable[\"RiziaDLC.Diplomacy_Wehlen_NegotiatingPower\"] = -3 end; ";
    }
}
