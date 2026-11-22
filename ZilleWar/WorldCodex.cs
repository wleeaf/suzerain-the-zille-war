using Il2Cpp;
using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// The codex during and after the war, in vanilla's encyclopedic tone.
/// - Existing entries get a war paragraph (or a reworded sentence) appended to their vanilla text, recomputed every step
///   from the war's state and put back when there is no war (and in Sordland games for the Shared entries).
/// - New entries (the Zille War, the Strait of Kreleck, the Bludish Freedom Front from Rizia's side, Auremina, Isor) are
///   added like vanilla's: an IsEnabledVariable switched on at the declaration, then EnableCodexEntry for the notification.
/// </summary>
internal static class WorldCodex
{
    private sealed record Entry(string Name, string Title, string Topic, string Category, string Image, string[] Keywords);

    private static readonly Entry[] NewEntries =
    {
        new("History_ImportantEvents_ZilleWar", "The Zille War", "CodexTopic_History_ImportantEvents", "CodexCategory_History", "Codex Entries/c_zille",
            new[] { "Zille War" }),
        new("Locations_Regions_StraitOfKreleck", "Strait of Kreleck", "CodexTopic_Locations_Regions", "CodexCategory_Locations",
            "Codex Entries/c_riziacitycoastalgeneric", new[] { "Strait of Kreleck", "Kreleck" }),
        new("Organisations_Political_BludishFreedomFront_Rizia", "Bludish Freedom Front", "CodexTopic_Organisations_Political",
            "CodexCategory_Organisations", "Codex Entries/c_bff", new[] { "Bludish Freedom Front", "BFF" }),
        new("Locations_Cities_Auremina", "Auremina", "CodexTopic_Locations_Cities", "CodexCategory_Locations", "Codex Entries/c_zille",
            new[] { "Auremina" }),
        new("Locations_Cities_Isor", "Isor", "CodexTopic_Locations_Cities", "CodexCategory_Locations", "Codex Entries/c_riziacitycoastalgeneric",
            new[] { "Isor" }),
    };

    private static string EnabledVar(string name) => $"ZilleWar.Codex_{name}";

    /// <summary>Vanilla entries we append to.</summary>
    private static readonly string[] Edited =
    {
        "Locations_Regions_ZilleTerritory", "Locations_Regions_ZilleTerritory_Brenas", "Locations_Regions_ZilleTerritory_RiziaImperii",
        "Locations_Cities_Zille", "Locations_Cities_Nanta", "History_ImportantEvents_ZilleAgreement", "Locations_Countries_Wehlen",
        "Characters_ForeignLeaders_WiktorSmolak", "Characters_ForeignLeaders_PatricioAlvarez_1950", "Characters_ForeignLeaders_PatricioAlvarez_1953",
        "Characters_ForeignLeaders_BeatriceLivingston", "Characters_ForeignLeaders_EmmerichHegel", "Locations_Cities_MaveNortus",
        "Locations_Cities_Qabiz", "Organisations_Political_SuOmina", "Organisations_Political_ArianaTorasAssociation",
        "Organisations_Governmental_RizianNavy", "Locations_Countries_UnitedContana", "Locations_Rizia_Countries_Lespia",
        "Locations_Rizia_Countries_Lespia_1950", "Locations_Countries_Rumburg", "Locations_Countries_RumburgOMECExpelled",
    };

    public static void Register()
    {
        foreach (Entry e in NewEntries)
        {
            Variables.Register(EnabledVar(e.Name));
            Vanilla.Expose(EnabledVar(e.Name));
        }
    }

    public static void OnStep()
    {
        EnsureEntries();
        if (!WorldSystems.Declared)
        {
            foreach (string n in Edited)
            {
                WorldText.RestoreCodex(n);
            }

            return;
        }

        foreach (Entry e in _added ? NewEntries : Array.Empty<Entry>())
        {
            WorldText.Codex(e.Name, null, NewText(e.Name));
            if (!Variables.GetBool(EnabledVar(e.Name)))
            {
                Variables.Set(EnabledVar(e.Name), true);
                try
                {
                    Il2CppPixelCrushers.DialogueSystem.Lua.Run($"EnableCodexEntry(\"{e.Name}\")");
                }
                catch (Exception ex)
                {
                    Core.Log.Warning($"EnableCodexEntry {e.Name}: {ex.Message}");
                }
            }
        }

        foreach (string n in Edited)
        {
            string original = WorldText.OriginalText("codex", n);
            if (original == null)
            {
                continue;
            }

            string text = Edit(n, original);
            if (text == null || text == original)
            {
                WorldText.RestoreCodex(n);
            }
            else
            {
                WorldText.Codex(n, null, text);
            }
        }
    }

    // ------------------------------------------------------------------ entities

    private static bool _added;

    /// <summary>The game rebuilt its entity lists (a save was loaded): add ours again on the next step.</summary>
    internal static void ResetAdded() => _added = false;

    private static void EnsureEntries()
    {
        if (_added || EntityDataManager.CodexEntriesData == null)
        {
            return;
        }

        CodexEntryData template = null;
        foreach (CodexEntryData c in EntityDataManager.CodexEntriesData)
        {
            if (c?.NameInDatabase == "History_ImportantEvents_ZilleAgreement")
            {
                template = c;
                break;
            }
        }

        if (template?.AppBundleProperties == null)
        {
            return;     // codex not loaded yet; try again next step
        }

        foreach (Entry e in NewEntries)
        {
            var keywords = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (string k in e.Keywords)
            {
                keywords.Add(k);
            }

            var data = new CodexEntryData
            {
                Id = $"0x5A11{(uint)e.Name.GetHashCode():X8}",
                NameInDatabase = e.Name,
                Path = "Rizia/Codex Entries/ZilleWar",
                IsWIP = false,
                Notes = string.Empty,
                AppBundleProperties = template?.AppBundleProperties,
                TagsProperties = template?.TagsProperties,
                CodexEntryProperties = new CodexEntryProperties
                {
                    Title = e.Title,
                    Topic = e.Topic,
                    Category = e.Category,
                    Keywords = keywords,
                    ImagePath = e.Image,
                    Description = NewText(e.Name),
                    IsEnabledVariable = EnabledVar(e.Name),
                    IsNotificationActive = false,
                },
            };
            ZilleOperation.AddByName(EntityDataManager.codexEntriesData, data, x => x.NameInDatabase);
            ZilleOperation.AddByName(EntityDataManager.CodexEntriesData, data, x => x.NameInDatabase);
        }

        _added = true;
    }

    // ------------------------------------------------------------------ state, in words


    private static bool Taken(string town) => Variables.GetBool(ZilleOperation.CapturedVar(town));
    private static bool Flag(string f) => Variables.GetBool(CityEvents.Flag(f));
    private static string Hunters()
    {
        bool ships = Lever(SupplyWar.Lever.RoyalNavy) > 0;
        bool subs = Lever(SupplyWar.Lever.Submarines) > 0;
        return ships && subs ? "Rizian ships and submarines hunted" : ships ? "Rizian ships hunted" : subs ? "Rizian submarines hunted"
            : "the Rizian fleet, which stayed in port, did not hunt";
    }

    private static int Lever(SupplyWar.Lever l) => Variables.GetInt(SupplyWar.Count(l));

    /// <summary>The Turn 8 treaty kind (renounced, border, accord, recognized, terms), or empty before or without a signed treaty.</summary>
    private static string TreatyKind => Variables.GetBool(ZilleTreaty.VarSigned) ? Variables.GetString(ZilleTreaty.VarKind) ?? string.Empty : string.Empty;

    private static string NotRecognized => TreatyKind == "recognized"
        ? "a claim Rizia has since accepted in the treaty signed at Wraclawitz"
        : "a claim Rizia does not recognize";

    private static string Pending => TreatyKind == "border"
        ? "the line having been made the border by the Mave Nortus Treaty."
        : "pending a final settlement.";

    private static string P(params string[] paragraphs) => string.Join("\n\n", paragraphs.Where(p => !string.IsNullOrEmpty(p)));

    /// <summary>An armistice after the last phase with Zille city held is "divided" (its own words; the ceasefire text when none is given), not a defeat.</summary>
    private static string Outcome(string active, string liberated, string defeated, string ceasefire, string divided = null) =>
        WorldSystems.Liberated ? liberated
        : WorldSystems.Defeated ? (WorldSystems.ZilleCityHeld ? divided ?? ceasefire : defeated)
        : WorldSystems.Ceasefire ? ceasefire : active;

    /// <summary>The phases fought, in order, from the campaign history ("Harbor:won;Wall:lost"): phase, aim, result.</summary>
    private static List<(int phase, string target, bool won)> History()
    {
        var list = new List<(int, string, bool)>();
        string[] items = (Variables.GetString(ZilleCampaign.VarHistory) ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int k = 1; k <= items.Length; k++)
        {
            string[] kv = items[k - 1].Split(':');
            if (kv.Length >= 2 && Enum.TryParse(kv[0], out Objective o))
            {
                int p = Variables.GetInt(ZilleCampaign.PhaseOfVar(k));
                list.Add((p > 0 ? p : k, ZilleCampaign.Info(o).Target, kv[1] == "won"));
            }
        }

        return list;
    }

    // ------------------------------------------------------------------ new entries

    private static string NewText(string name) => name switch
    {
        "History_ImportantEvents_ZilleWar" => ZilleWarText(),
        "Locations_Regions_StraitOfKreleck" => P(
            "The Strait of Kreleck is the narrow sea passage that separates the Zille region from mainland Wehlen. Since the " +
            "dissolution of the Markian Empire in the 1600s, Rizia and Wehlen have jointly controlled boat traffic through it, and " +
            "for centuries it carried most of the trade between the two countries.",
            "After the Zille Agreement of 1927, the strait became the lifeline between Wehlen and its administration in Zille. " +
            "Troops, officials and supplies crossed it from the Wehzek ports on the mainland, as did the refugees of the Wehlen Civil War.",
            WorldSystems.Declared
                ? (WorldSystems.LespiaHelps
                    ? "During the Zille War, the strait was the decisive theater at sea. Lespian warships patrolling it under the banner " +
                      "of free navigation turned back Wehzek convoys bound for Zille, while " + Hunters() + " those that slipped through."
                    : "During the Zille War, the strait was the decisive theater at sea. Wehzek convoys crossed it at night to supply the " +
                      "Zille garrison, and " + Hunters() + " them, while marine insurers suspended cover for every merchant " +
                      "ship in the passage.")
                : null),
        "Organisations_Political_BludishFreedomFront_Rizia" => P(
            "The Bludish Freedom Front (BFF) is a Bludish separatist militant organization active in Sordland and in the northeast " +
            "of Wehlen, where it maintains bases in the mountains around Vernen. Founded in 1933 after the Izzam Incident, it seeks " +
            "an independent state for the Bludish people.",
            "Wehlen and Sordland designate the group a terrorist organization, and President Smolak's government has waged repeated " +
            "military campaigns against it. The BFF is widely believed to receive money and weapons from the Kingdom of Rumburg and " +
            "from Derdia, an accusation Wehlen makes loudly and Thornbourgh has never confirmed.",
            Lever(SupplyWar.Lever.BffRaids) > 0 || Lever(SupplyWar.Lever.BffFighters) > 0
                ? "During the Zille War, BFF raids on the Wehzek ports facing the Strait of Kreleck tied down units that might otherwise " +
                  "have reinforced Zille." + (Lever(SupplyWar.Lever.BffFighters) > 0
                      ? " Bludish fighters brought in through Rumburg's contacts also served under Rizian officers, a decision criticized at " +
                        "home and abroad."
                      : string.Empty)
                : WorldSystems.Declared
                    ? "Wehzek officials have accused Rizia of making common cause with the group since the outbreak of the Zille War."
                    : null),
        "Locations_Cities_Auremina" => P(
            "Auremina is a mining town in the hills at the heart of the Zille region. It grew around the gold deposits discovered in " +
            "the region after 1918, when the open-pit mines and refinery of Rizian Royal Gold made it the richest town in Zille, " +
            "until the 1927 Zille Agreement placed its mines under Wehzek administration.",
            "Many of Auremina's miners are ethnic Rizians whose families stayed on through the years of the lease, working the same " +
            "pits under Wehzek overseers.",
            Taken("Auremina")
                ? "During the Zille War, Auremina was taken by Rizian forces in Operation Northern Crown." +
                  (Flag("Auremina_MinersFreed") ? " The miners came out of the pits to greet the soldiers." : string.Empty) +
                  (Flag("Auremina_MinesSecured") ? " The mines were placed under military guard until the end of the fighting." : string.Empty)
                : WorldSystems.Declared ? "During the Zille War, the town and its mines were the objective of the second phase of Operation Northern Crown." : null),
        "Locations_Cities_Isor" => P(
            "Isor is a fishing port on the western coast of the Zille region, facing the Strait of Kreleck. Its fishing families are " +
            "among the oldest Rizian communities in the region, and many kept Rizian names and customs throughout the years of " +
            "Wehzek administration.",
            "The port's small boats have long carried passengers, fish and, by reputation, contraband across the strait.",
            Taken("Isor")
                ? "Isor was taken by Rizian forces during Operation Northern Crown." +
                  (Flag("Isor_Fishermen") ? " Its fishermen hung the old Rizian colors from their boats on the day the troops arrived." : string.Empty)
                : WorldSystems.Declared ? "During the Zille War, Isor and Nanta were the objectives of the last phase of Operation Northern Crown." : null),
        _ => null,
    };

    private static string ZilleWarText()
    {
        bool over = WorldSystems.WarEnded;
        string opening =
            $"The Zille War {(over ? "was" : "is")} an armed conflict between the Kingdom of Rizia and the Republic of Wehlen over the " +
            "Zille region, which Wehlen had administered since the Zille Agreement of 1927. It began when King Romus Toras declared " +
            "war at a session of the Alliance of Nations in Konstantium, moments after President Wiktor Smolak announced that Wehlen " +
            "would not return the territory at the end of its lease.";

        string plan =
            "The Rizian General Staff fought the war as a single operation, Operation Northern Crown, in three phases a month apart: the " +
            "assault on the city of Zille and its harbor, the advance into the gold-mining hills around Auremina, and the push to the " +
            "northern ports of Isor and Nanta. The Wehzek garrison depended on supplies shipped across the Strait of Kreleck, which made " +
            "the strait a decisive theater of the war.";

        var allies = new List<string>();
        if (SupplyWar.RumburgEmbargo)
        {
            allies.Add(SupplyWar.RumburgOilCut ? "Rumburg kept its ports closed to Wehzek oil" : "Rumburg bought only half its old share of Wehzek oil");
        }

        if (Lever(SupplyWar.Lever.BffRaids) > 0)
        {
            allies.Add("fighters of the Bludish Freedom Front struck the Wehzek ports on the strait");
        }

        if (WorldSystems.LespiaHelps)
        {
            allies.Add($"the Lespian navy closed the strait to Wehzek shipping, {WorldSystems.LespiaPrice}");
        }

        if (Lever(SupplyWar.Lever.RoyalNavy) > 0 || Lever(SupplyWar.Lever.Submarines) > 0)
        {
            allies.Add("the Royal Navy fought a campaign against the Wehzek convoys");
        }

        string support = allies.Count == 0
            ? "Rizia fought largely alone. United Contana and Valgsland condemned the war and supported Wehlen with money and arms."
            : $"During the war, {JoinAnd(allies)}. United Contana and Valgsland condemned the war and supported Wehlen with money and arms.";

        var ops = History();
        string course = ops.Count == 0
            ? null
            : string.Join(" ", ops.Select(o => o.won
                ? $"Phase {ZilleCampaign.Roman(o.phase)} took {o.target}."
                : $"Phase {ZilleCampaign.Roman(o.phase)} failed to take {o.target}."));

        string end = Outcome(
            WorldSystems.ZilleCityHeld ? "The city of Zille is under Rizian military administration, and fighting continues in the rest of the region." : null,
            "The war ended with the surrender of the last Wehzek garrisons and the return of the entire region to Rizian sovereignty, " +
            "ending twenty-five years of Wehzek administration.",
            "The war ended in a Rizian defeat. Wehlen continues to administer the region and has since declared the Zille Agreement void, " +
            NotRecognized + ".",
            "The war ended in a ceasefire supervised by the Alliance of Nations, with the region divided along the line of contact" +
            (WorldSystems.ZilleCityHeld ? " and the city of Zille in Rizian hands." : ". Wehlen continues to administer most of the region."),
            "The war ended in an armistice under Alliance supervision, with Zille city in Rizian hands and the rest of the region in Wehzek hands.");

        string twoWars = Variables.GetInt(WorldSystems.VarTwoWarsTurn) > 0
            ? "For part of the war, Rizia was also at war with the Grand Duchy of Pales, and the Royal Army fought on two fronts at once."
            : null;

        return P(opening, plan, support, course, twoWars, end);
    }

    private static string JoinAnd(List<string> l) =>
        l.Count <= 1 ? string.Join("", l) : string.Join(", ", l.Take(l.Count - 1)) + " and " + l[^1];

    // ------------------------------------------------------------------ war paragraphs for vanilla entries

    private const string Declaration =
        "When the twenty-five-year term of the Zille Agreement expired, President Wiktor Smolak invoked the AN Threat Mitigation " +
        "Protocol to postpone the handover, citing the Friendship Day bombing. King Romus Toras answered from the floor of the " +
        "Alliance of Nations in Konstantium with a declaration of war.";

    private static string Edit(string name, string original)
    {
        bool lespiaPublic = WorldSystems.LespiaExposed;
        switch (name)
        {
            case "Locations_Regions_ZilleTerritory":
                return P(original, Declaration, Outcome(
                    "The territory has since become a front line, with Wehzek garrisons supplied across the Strait of Kreleck and " +
                    "Rizian forces advancing from Mave Nortus." +
                    (WorldSystems.ZilleCityHeld ? " The city of Zille has been under Rizian military administration since its capture." : string.Empty),
                    "The war ended with the surrender of the last Wehzek garrisons, and the territory returned to Rizian sovereignty for " +
                    "the first time since 1927.",
                    "The Rizian offensive failed, and Wehlen continues to administer the territory. The Wehzek government has since " +
                    "declared the Zille Agreement void, arguing that Rizia forfeited its claim by resorting to war. " +
                    (TreatyKind == "recognized"
                        ? "Rizia has since recognized Wehzek sovereignty over Zille in a treaty signed at Wraclawitz."
                        : "Rizia does not recognize the claim."),
                    "Fighting ended with a ceasefire supervised by the Alliance of Nations. The line of contact now runs through the " +
                    "region" + (WorldSystems.ZilleCityHeld ? ", with the city of Zille under Rizian control," : string.Empty) +
                    " and the rest of the territory remains under Wehzek administration, " + Pending,
                    "Fighting ended in an armistice under Alliance supervision. The line of contact now runs through the " +
                    "region, with the city of Zille under Rizian control, and the rest of the territory remains under Wehzek " +
                    "administration, " + Pending));
            case "Locations_Regions_ZilleTerritory_Brenas":
            case "Locations_Regions_ZilleTerritory_RiziaImperii":
                return WorldSystems.Liberated
                    ? original.Replace("Since the 1957 return of Zille to Rizia,", "Since the Zille War returned the territory to Rizia by force of arms,")
                    : null;
            case "Locations_Cities_Zille":
                return WorldSystems.Liberated
                    ? original.Replace(
                        "Recently the city was reincorporated back into Rizia after decades of Wehzek occupation, leading to a rapid growth " +
                        "as many ethnic Rizians from other parts of the country returned to their ancestral home.",
                        "The city was retaken by Rizian forces during the Zille War, after a battle for the harbor that left parts of the " +
                        "waterfront in ruins for the second time in a generation. Many Wehzek residents left on the last boats across the " +
                        "Strait of Kreleck, while ethnic Rizians from other parts of the country have begun returning to their ancestral home." +
                        (Flag("Zille_BannerRaised") ? " The square the Wehzek administration had named after President Smolak has taken back its old name." : string.Empty))
                    : null;
            case "Locations_Cities_Nanta":
                return WorldSystems.Liberated
                    ? original.Replace(
                        "Like the rest of the area, Nanta was only recently reunified with the rest of Rizia, after spending several " +
                        "decades under Wehzek occupation, a result of the 1927 Zille Agreement.",
                        "Like the rest of the area, Nanta was returned to Rizia by force during the Zille War, after several decades under " +
                        "Wehzek administration that began with the 1927 Zille Agreement. Its large Wehzek and Bludish communities make it " +
                        "the most closely watched city in the reclaimed territory." +
                        (Flag("Nanta_PortSabotaged") ? " Its piers, demolished during the fighting, are still being rebuilt." : string.Empty))
                    : null;
            case "History_ImportantEvents_ZilleAgreement":
                return P(original,
                    "The agreement's term expired without a handover. President Smolak's invocation of the AN Threat Mitigation Protocol " +
                    "was answered the same day by Rizia's declaration of war, making the Zille Agreement the only lease in the history " +
                    "of the Alliance of Nations to end on a battlefield.",
                    Outcome("The war over its terms continues.",
                        TreatyKind is "renounced" or "border"
                            ? "Rizia regards the agreement as fulfilled by force of arms. Wehlen has since renounced all claim to the region in a treaty signed with Rizia."
                            : "Rizia regards the agreement as fulfilled by force of arms. Wehlen regards it as broken.",
                        TreatyKind == "recognized"
                            ? "Wehlen cited Rizia's declaration of war as grounds for voiding the agreement altogether, and Rizia has since recognized that in a treaty signed at Wraclawitz."
                            : "Wehlen now cites Rizia's declaration of war as grounds for voiding the agreement altogether.",
                        TreatyKind == "border"
                            ? "The Mave Nortus Treaty settled the question by making the line of contact the border."
                            : "Under the ceasefire, the question of the agreement has been returned to the Alliance of Nations, where it remains unresolved.",
                        TreatyKind == "border"
                            ? "The Mave Nortus Treaty settled the question by making the armistice line the border."
                            : "Under the armistice, the question of the agreement has been returned to the Alliance of Nations, where it remains unresolved."));
            case "Locations_Countries_Wehlen":
                return P(original,
                    "After refusing to return the Zille region at the end of its lease, Wehlen found itself at war with the Kingdom of " +
                    "Rizia. The conflict strained an economy still recovering from the civil war and coincided with renewed operations " +
                    "against the Bludish Freedom Front in the northeast." +
                    Outcome(string.Empty, " It ended with the loss of Zille.", " Wehlen emerged from it holding Zille more firmly than before.",
                        " It ended in a ceasefire that left the region divided.", " It ended in an armistice that left Zille divided."));
            case "Characters_ForeignLeaders_WiktorSmolak":
                return P(original,
                    "Smolak refused to return the Zille region to Rizia at the end of its lease, invoking the AN Threat Mitigation " +
                    "Protocol after the Friendship Day bombing. The decision led directly to the Zille War. In his speeches he has " +
                    "presented the conflict as a defense of the Wehzeks of Zille against \"Rumburg and its royal clients.\"" +
                    Outcome(string.Empty, " The loss of Zille was the first territorial defeat of his presidency.",
                        " Victory in Zille has strengthened his position at home, where the war is commemorated as the Defense of the Strait.",
                        TreatyKind == "border" ? " He signed the Mave Nortus Treaty, which made the line of contact the border." : " He has called the ceasefire line \"a wound, not a border.\"",
                        TreatyKind == "border" ? " He signed the Mave Nortus Treaty, which made the armistice line the border." : " He has called the armistice line \"a wound, not a border.\""));
            case "Characters_ForeignLeaders_PatricioAlvarez_1950":
            case "Characters_ForeignLeaders_PatricioAlvarez_1953":
                return P(original,
                    WorldSystems.LespiaHelps
                        ? "During the Zille War, Alvarez's government deployed the Lespian navy to stop Wehzek shipping in the Strait of " +
                          "Kreleck, " + WorldSystems.LespiaPrice + ". Lespian officials described the operation as a contribution to regional stability."
                        : Variables.GetBool(SupplyWar.VarLespiaFuel)
                            ? "Lespian harbors refueled Wehzek tankers bound for Zille during the Zille War, after Alvarez held Rizia's conduct at the Meftiem against it."
                            : "Lespia remained formally neutral in the Zille War, calling on both parties to keep the sea lanes open to neutral shipping.",
                    lespiaPublic
                        ? "Rizian investigators have since linked a Lespian intelligence officer stationed in Zille to the Friendship Day " +
                          "bombing that preceded the war. Alvarez has denied any involvement."
                        : null);
            case "Locations_Rizia_Countries_Lespia":
            case "Locations_Rizia_Countries_Lespia_1950":
                return WorldSystems.LespiaHelps
                    ? P(original, "During the Zille War, the Lespian navy patrolled the Strait of Kreleck and turned back Wehzek shipping, " +
                                  "a service Lespia provided to Rizia at a price.")
                    : null;
            case "Characters_ForeignLeaders_BeatriceLivingston":
                return SupplyWar.RumburgEmbargo || Lever(SupplyWar.Lever.BffRaids) > 0 || Lever(SupplyWar.Lever.BffFighters) > 0
                    ? P(original, "During the Zille War she was Rizia's most important foreign backer" +
                                  (SupplyWar.RumburgOilCut ? ", keeping Rumburg's ports closed to Wehzek oil" : SupplyWar.RumburgOilHalved ? ", cutting Rumburg's purchases of Wehzek oil by half" : string.Empty) +
                                  (Lever(SupplyWar.Lever.BffRaids) > 0 || Lever(SupplyWar.Lever.BffFighters) > 0
                                      ? ", and she was widely believed to have arranged Bludish Freedom Front raids in northern Wehlen, though Thornbourgh has never confirmed it."
                                      : "."))
                    : P(original, "During the Zille War she declared Rumburg's support for her brother-in-law's claim to Zille.");
            case "Characters_ForeignLeaders_EmmerichHegel":
                return P(original,
                    "Hegel condemned Rizia's declaration of war on Wehlen in the strongest terms, calling for sanctions against the Rizian " +
                    "monarchy and promising that Valgsland would stand ready to assist the Wehzek people in their self-defense.");
            case "Locations_Cities_MaveNortus":
                return P(original,
                    "During the Zille War, the town served as the main staging area for Rizian forces. Its customs halls were converted " +
                    "into barracks and its schoolhouse into a field hospital.");
            case "Locations_Cities_Qabiz":
                return WorldSystems.LespiaCaleqabiz
                    ? P(original, "During the Zille War, as part of an agreement securing Lespian naval support in the Strait of Kreleck, " +
                                  "Lespian warships were granted berthing and refueling rights at the island's marine refueling center.")
                    : null;
            case "Organisations_Political_SuOmina":
                return P(original,
                    "During the Zille War, Su Omina members volunteered for the Royal Army in large numbers, and its cells inside the region " +
                    "carried out sabotage behind Wehzek lines. Its role has been praised by nationalists and condemned by human rights " +
                    "organizations, which accuse its members of reprisals against Wehzek civilians.");
            case "Organisations_Political_ArianaTorasAssociation":
                return P(original,
                    "During the Zille War, the association's Zille branch was banned by the Wehzek authorities and several of its organizers " +
                    "were detained. Its volunteers later served the advancing Rizian army as guides and interpreters, and ran shelters for " +
                    "families displaced by the fighting.");
            case "Organisations_Governmental_RizianNavy":
            {
                var parts = new List<string>();
                if (Lever(SupplyWar.Lever.RoyalNavy) > 0)
                {
                    parts.Add("fought night actions against Wehzek convoys in the Strait of Kreleck, at the cost of ships and crews");
                }

                if (Lever(SupplyWar.Lever.Submarines) > 0)
                {
                    parts.Add("sent its submarines to hunt Wehzek transports on the crossing to Zille");
                }

                string deeds = parts.Count == 0 ? "watched the approaches to Zille harbor from Sallabes" : JoinAnd(parts);
                return P(original,
                    $"The Zille War was the navy's first major campaign since the mutiny of 1926. The fleet {deeds}." +
                    (WorldSystems.LespiaHelps ? " Much of the work of closing the strait, however, was done by the Lespian navy." : string.Empty));
            }
            case "Locations_Countries_UnitedContana":
                return P(original,
                    "During the Zille War, United Contana condemned what it called the aggression of the Rizian monarchy against Wehlen and " +
                    "sent money, arms and advisers to the Wehzek government, most of it by sea across the Strait of Kreleck.");
            case "Locations_Countries_Rumburg":
            case "Locations_Countries_RumburgOMECExpelled":
                return P(original,
                    "Rumburg was the Kingdom of Rizia's principal foreign backer in the Zille War" +
                    (SupplyWar.RumburgOilCut ? ", and it kept its ports closed to Wehzek oil for the duration of the fighting."
                        : SupplyWar.RumburgOilHalved ? ", and it bought only half its old share of Wehzek oil while the fighting lasted." : "."));
            default:
                return null;
        }
    }
}
