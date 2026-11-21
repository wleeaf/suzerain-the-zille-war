using Il2Cpp;
using SuzerainModdingKit;
using ConditionalInstruction = Il2Cpp.ConditionalInstructionProperties.ConditionalInstruction;

namespace ZilleWar;

/// <summary>
/// Keeps the rest of the game consistent with the war while it lasts.
/// - Wehlen's map relation: vanilla's "Relations_Wehlen" conditional instruction runs after every story
///   fragment and recomputes the relation from Wehlen's negotiating power, which overwrote "At War" with
///   "Unfriendly". We add an at-war entry first and last in its list, so it wins whichever way the list
///   is evaluated (first match or all matches in order).
/// - Newspapers: vanilla stories that assume peace with Wehlen (the handover talks, Wehzek medicine,
///   Smolak joining the Pales war...) are switched off while the war is on, and restored if it ends.
/// </summary>
internal static class WarState
{
    public const string VarActive = "ZilleWar.War_Active";
    private const string ActiveCondition = "ZilleWar.War_Active == true";
    private const string NeverVariable = "ZilleWar.Never";

    private const string AtWarInstruction =
        "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"AtWar\");\n" +
        "RiziaDLC.CountryRelation_Wehlen_AtWar = true;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Allied = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Friendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Neutral = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Unfriendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Hostile = true;";

    /// <summary>Vanilla news written for a Rizia at peace with Wehlen.</summary>
    private static readonly HashSet<string> PeacetimeNews = new(StringComparer.Ordinal)
    {
        "Turn04_G_AN_WehlenAnnoucement", "Turn04_RH_WehlenAnnouncement", "Turn04_G_AN_Hegel",
        "Turn05_G_WehlenDeal", "Turn05_G_WehlenNoDeal", "Turn05_RH_WehlenDeal", "Turn05_RH_WehlenNoDeal",
        "Turn05_G_WehlenArbitrationSupport",
        "Turn07_RH_War_WehlenDrugs_Civilian", "Turn07_RH_War_WehlenDrugs_Military",
        "Turn08_G_Decision_MediateDerdiaWehlen_Mediate", "Turn08_G_ZilleAN_NoReferendum", "Turn08_G_ZilleAN_Referendum",
        "Turn08_G_ZilleAN_Return", "Turn08_SecurityBrief_ZilleTransfer",
        "Turn08_ZilleNegociations_ZilleReturnFailure", "Turn08_ZilleNegociations_ZilleReturnSuccess",
        "Turn09_G_SmolakCall_WehlenJoinsWar", "Turn09_RH_SmolakCall_WehlenJoinsWar", "Turn09_V_SmolakCall_WehlenJoinsWar",
        "Turn10_RH_PalesVictory_Wehlen", "Turn10_V_PalesVictory_Wehlen",
    };

    private static readonly Dictionary<string, string> OriginalNewsVariables = new(StringComparer.Ordinal);

    public static void RegisterVariables() => Variables.Register(VarActive);

    /// <summary>Called on every story step. <paramref name="atWar"/> is true while the war is being fought.</summary>
    public static void Apply(bool atWar)
    {
        Variables.Set(VarActive, atWar);
        PatchRelationsInstruction();
        SuppressPeacetimeNews(atWar);
        if (atWar)
        {
            try
            {
                Il2CppPixelCrushers.DialogueSystem.Lua.Run("UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"AtWar\")");
                Variables.Set("RiziaDLC.CountryRelation_Wehlen_AtWar", true);
                Variables.Set("RiziaDLC.CountryRelation_Wehlen_Hostile", true);
                foreach (string r in new[] { "Allied", "Friendly", "Neutral", "Unfriendly" })
                {
                    Variables.Set($"RiziaDLC.CountryRelation_Wehlen_{r}", false);
                }
            }
            catch (Exception ex)
            {
                Core.Log.Warning($"Could not set Wehlen to At War: {ex.Message}");
            }
        }
    }

    private static void PatchRelationsInstruction()
    {
        foreach (var list in new[] { EntityDataManager.conditionalInstructionData, EntityDataManager.ConditionalInstructionData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (ConditionalInstructionData c in list)
            {
                if (c?.NameInDatabase != "Relations_Wehlen")
                {
                    continue;
                }

                var entries = c.ConditionalInstructionProperties?.ConditionalInstructions;
                if (entries == null)
                {
                    continue;
                }

                bool patched = false;
                foreach (ConditionalInstruction e in entries)
                {
                    patched |= e?.Condition == ActiveCondition;
                }

                if (!patched)
                {
                    entries.Insert(0, new ConditionalInstruction { Condition = ActiveCondition, Instruction = AtWarInstruction });
                    entries.Add(new ConditionalInstruction { Condition = ActiveCondition, Instruction = AtWarInstruction });
                    Core.Log.Msg("Patched Relations_Wehlen: Wehlen stays At War while the Zille war lasts.");
                }
            }
        }
    }

    private static void SuppressPeacetimeNews(bool atWar)
    {
        foreach (var list in new[] { EntityDataManager.newsData, EntityDataManager.NewsData })
        {
            if (list == null)
            {
                continue;
            }

            foreach (NewsData n in list)
            {
                string name = n?.NameInDatabase;
                NewsProperties p = n?.NewsProperties;
                if (name == null || p == null || !PeacetimeNews.Contains(name))
                {
                    continue;
                }

                if (atWar)
                {
                    if (p.IsEnabledVariable != NeverVariable)
                    {
                        OriginalNewsVariables[name] = p.IsEnabledVariable;
                        p.IsEnabledVariable = NeverVariable;
                    }
                }
                else if (OriginalNewsVariables.TryGetValue(name, out string original) && p.IsEnabledVariable == NeverVariable)
                {
                    p.IsEnabledVariable = original;
                }
            }
        }
    }
}
