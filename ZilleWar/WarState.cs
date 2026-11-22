using HarmonyLib;
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
/// - Newspapers and reports that assume peace with Wehlen are handled by <see cref="WorldPress"/>.
/// </summary>
internal static class WarState
{
    public const string VarActive = "ZilleWar.War_Active";
    // Vanilla-namespaced, so the game's expression converter reads it like its own conditions.
    private const string ActiveCondition = "RiziaDLC.CountryRelation_Wehlen_AtWar == true";

    private const string AtWarInstruction =
        "UpdateCountryRelationship(\"Rizia_Country_Wehlen\", \"AtWar\");\n" +
        "RiziaDLC.CountryRelation_Wehlen_AtWar = true;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Allied = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Friendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Neutral = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Unfriendly = false;\n" +
        "RiziaDLC.CountryRelation_Wehlen_Hostile = true;";

    /// <summary>True while the war is fought (mirrors <see cref="VarActive"/> for the Harmony patch).</summary>
    public static bool Active { get; private set; }

    /// <summary>A save is being loaded: the mirror belongs to the game left behind until the next story step sets it.</summary>
    internal static void ForgetSession() => Active = false;

    /// <summary>Called on every story step. <paramref name="atWar"/> is true while the war is being fought.</summary>
    public static void Apply(bool atWar)
    {
        Variables.Set(VarActive, atWar);
        Active = atWar;
        if (!atWar)
        {
            Variables.Set("RiziaDLC.CountryRelation_Wehlen_AtWar", false);
        }

        PatchRelationsInstruction();
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
}

/// <summary>
/// Every relationship change, from conversations, decrees or vanilla's per-fragment relation rule, goes
/// through this Lua function. While the war lasts, Wehlen stays At War whatever the caller asks for.
/// </summary>
[HarmonyPatch(typeof(TokenInformationPanel), nameof(TokenInformationPanel.UpdateCountryRelationship))]
internal static class Patch_UpdateCountryRelationship
{
    private static void Prefix(string countryNameInDatabase, ref string relationship)
    {
        if (countryNameInDatabase == "Rizia_Country_Wehlen" && relationship != "AtWar"
            && (WarState.Active || Variables.GetBool(WarState.VarActive)))
        {
            Core.Log.Msg($"Wehlen relation '{relationship}' requested during the war; keeping At War.");
            relationship = "AtWar";
        }
    }
}

/// <summary>
/// A loaded save (or a new game) must not inherit the war of the game left behind: until its first story step the at-war
/// mirror would keep Wehlen At War in a peace game, and a set-aside war map could be handed to the wrong save.
/// </summary>
[HarmonyPatch]
internal static class Patch_WarStateOnLoad
{
    private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
        new[]
        {
            AccessTools.Method(typeof(PersistenceManager), nameof(PersistenceManager.LoadSaveFile)),
            AccessTools.Method(typeof(PersistenceManager), nameof(PersistenceManager.LoadLastCheckpoint)),
            AccessTools.Method(typeof(PersistenceManager), nameof(PersistenceManager.CreateNewCampaign)),
        }.Where(m => m != null);

    private static void Prefix()
    {
        try
        {
            WarState.ForgetSession();
            WarSaveSlot.ForgetSession();
        }
        catch (Exception ex)
        {
            Core.Log.Warning($"Resetting the war state on load failed: {ex.Message}");
        }
    }
}
