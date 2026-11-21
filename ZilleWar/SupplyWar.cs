using SuzerainModdingKit;
using SuzerainModdingKit.StoryFragments.Decision;
using SuzerainModdingKit.VanillaData;

namespace ZilleWar;

/// <summary>
/// Cutting Zille off from the Wehzek mainland. Once per story turn while the war lasts, the King can pull
/// one lever; each lever weakens the garrison (War_Zille_Strength) and has a price.
/// - Rumburg's oil embargo (vanilla: Queen Beatrice's Turn 3 visit, Situation_Diplomacy_Rumburg_*EnergyExport).
///   Already in force at the declaration, it counts from the start.
/// - The Bludish Freedom Front, Rumburg's contacts inside Wehlen (vanilla: Turn 7 Rumburg meeting): raids on
///   the Wehzek ports, or fighters lent to the Royal Army (vanilla Mercenary units, RumburgianBFFMercenaries).
/// - The Royal Navy in the Strait of Kreleck: costs a ship, sinks convoys and one Wehzek warship.
/// - Alvarez's "practical arrangements" (only if the King kept the note in Konstantium): the Lespian fleet
///   closes the strait, in return for naval rights at Caleqabiz.
/// </summary>
internal static class SupplyWar
{
    public enum Lever { Embargo, BffRaids, BffFighters, RoyalNavy, Lespia }

    private const string DecisionName = "ZilleWar.Decision_SupplyWar";
    private const string VarLastTurn = "ZilleWar.SupplyWar_LastTurn";
    private const string VarEmbargoCounted = "ZilleWar.SupplyWar_EmbargoCounted";
    private const string NotNow = "Not now. The army needs no more enemies than it already has.";

    public static string Flag(Lever l) => $"ZilleWar.Lever_{l}";

    public static bool RumburgEmbargo =>
        Variables.GetBool("RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport") ||
        Variables.GetBool("RiziaDLC.Situation_Diplomacy_Rumburg_AverageEnergyExport") ||
        Variables.GetBool(Flag(Lever.Embargo));

    private static readonly Dictionary<string, Lever> OptionToLever = new();

    public static void Register()
    {
        foreach (Lever l in Enum.GetValues<Lever>())
        {
            Variables.Register(Flag(l));
        }

        Variables.Register(VarLastTurn);
        Variables.Register(VarEmbargoCounted);
        Events.OnDecisionShow += (_, _) => OnDecisionShow();
        Events.OnDecisionFinished += (_, e) => OnDecisionFinished(e.SelectedOptionInfo);
    }

    /// <summary>At the declaration: an embargo already in force starts to bite at once.</summary>
    public static void OnWarDeclared()
    {
        if (RumburgEmbargo && !Variables.GetBool(VarEmbargoCounted))
        {
            Variables.Set(VarEmbargoCounted, true);
            ZilleCampaign.Strength -= 1;
        }
    }

    private static bool Available(Lever l) => !Variables.GetBool(Flag(l)) && l switch
    {
        Lever.Embargo => !RumburgEmbargo,
        Lever.BffFighters => true,
        Lever.BffRaids => true,
        Lever.RoyalNavy => Variables.GetInt("RiziaDLC.Resources_Military_Ships") > 0,
        Lever.Lespia => Variables.GetString(AnDeclaration.VarLespianNote) == "kept",
        _ => false,
    };

    /// <summary>Offer the decision once per story turn (called from the war flow on each step).</summary>
    public static void Offer()
    {
        int turn = GameState.CurrentTurnNum ?? 0;
        if (Variables.GetInt(VarLastTurn) >= turn || GameState.StoryFragmentExistsInCurrentStep(DecisionName) ||
            !Enum.GetValues<Lever>().Any(Available))
        {
            return;
        }

        Variables.Set(VarLastTurn, turn);
        string embargo = RumburgEmbargo
            ? "Rumburg has stopped buying Wehzek oil, and Smolak's treasury feels it."
            : "Rumburg still buys Wehzek oil, and every barrel pays for another convoy.";
        GameState.AddCustomStoryFragment(new CustomDecisionFragment(DecisionName, SuzerainStoryPackInfo.Rizia,
            SuzerainTokenName.RiziaCityPorteDrazon, "Cutting Zille Off",
            "Every night, Wehzek convoys cross the Strait of Kreleck and unload at Nanta and Isor: men, shells, fuel, food. " +
            $"As long as they keep coming, every town we take in Zille will have to be taken twice. {embargo} " +
            $"Naval intelligence rates the Zille garrison as {ZilleCampaign.Tier.ToLowerInvariant()}. How do we cut them off?",
            "Cutting Zille Off", "The strait must be closed."));
    }

    private static string Text(Lever l) => l switch
    {
        Lever.Embargo => "Ask Queen Beatrice to stop buying Wehzek oil. Rizia will cover Rumburg's shortfall with its own energy.",
        Lever.BffRaids => "Ask Rumburg to set the Bludish Freedom Front on Wehlen's ports. They hate Smolak more than we do.",
        Lever.BffFighters => "Ask Rumburg for Bludish Freedom Front fighters to fight alongside the Royal Army. The treasury will pay them.",
        Lever.RoyalNavy => "Send the Royal Navy into the Strait of Kreleck to sink the convoys. We will lose ships doing it.",
        Lever.Lespia => "Accept Alvarez's offer. The Lespian fleet closes the strait, and Lespia gets naval rights at Caleqabiz.",
        _ => string.Empty,
    };

    private static void OnDecisionShow()
    {
        if (DecisionManager.CurrentDecisionName != DecisionName)
        {
            return;
        }

        OptionToLever.Clear();
        foreach (Lever l in Enum.GetValues<Lever>().Where(Available))
        {
            OptionToLever[Text(l)] = l;
            DecisionManager.AddOption(Text(l));
        }

        DecisionManager.AddOption(NotNow);
    }

    private static void OnDecisionFinished(DecisionOptionInfo selected)
    {
        if (selected?.DecisionName != DecisionName || !OptionToLever.TryGetValue(selected.Text, out Lever l))
        {
            return;
        }

        Variables.Set(Flag(l), true);
        int before = ZilleCampaign.Strength;
        void Add(string v, int d) => Variables.Set(v, Variables.GetInt(v) + d);
        switch (l)
        {
            case Lever.Embargo:
                ZilleCampaign.Strength -= 1;
                Add("RiziaDLC.Resources_Energy", -3);
                Add("RiziaDLC.Diplomacy_Wehlen_NegotiatingPower", -2);
                Add("RiziaDLC.Diplomacy_Rumburg_NegotiatingPower", 2);
                Variables.Set("RiziaDLC.Situation_Diplomacy_Rumburg_HighEnergyExport", true);
                NewsWire.LeverNews(l);
                break;
            case Lever.BffRaids:
                ZilleCampaign.Strength -= 1;
                Add("RiziaDLC.Diplomacy_Valgsland_NegotiatingPower", -1);
                Add("RiziaDLC.Diplomacy_Sordland_NegotiatingPower", -1);
                NewsWire.LeverNews(l);
                break;
            case Lever.BffFighters:
                Add("RiziaDLC.Army_Unit_Mercenary_Total", 2);
                Variables.Set("RiziaDLC.Resources_Military_RumburgianBFFMercenaries", true);
                Add("RiziaDLC.Resources_Budget", -1);
                Add("RiziaDLC.Diplomacy_Sordland_NegotiatingPower", -1);
                NewsWire.LeverNews(l);
                break;
            case Lever.RoyalNavy:
                ZilleCampaign.Strength -= 1;
                Add("RiziaDLC.Resources_Military_Ships", -1);
                NewsWire.LeverNews(l);
                break;
            case Lever.Lespia:
                ZilleCampaign.Strength -= 2;
                Add("RiziaDLC.Resources_Budget", -1);
                Add("RiziaDLC.Diplomacy_Lespia_NegotiatingPower", -2);
                NewsWire.LeverNews(l);
                break;
        }

        Core.Log.Msg($"Supply war: {l}. Wehlen strength {before} -> {ZilleCampaign.Strength} ({ZilleCampaign.Tier}).");
    }
}
