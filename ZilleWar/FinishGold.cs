using SuzerainModdingKit;
using static ZilleWar.LK;

namespace ZilleWar;

/// <summary>
/// Rizian Royal Gold in the Turn 8 economy talk (c591). Vanilla only raises a wartime nationalization when the Pales war is
/// on, and argues it with Pales (ATO on Pales' side, Lespia supporting the Palesians). In a Zille war without the Pales war
/// Hugo raises it too, and the case is made with what the Zille war actually changed: the Auremina gold
/// (LateT8 "ZilleWar.Late_AureminaGold", Rusty's Turn 6 pledge), the war's bill, Arcasia's indifference to Smolak and what
/// Lespia did or did not do in the Strait of Kreleck. With both wars, the King's money lines count two wars.
/// A Herald piece follows a nationalization ordered in a Zille war.
/// </summary>
internal static class FinishGold
{
    private const string Conv = "Rizia/Turn 8/ECONOMY/ECONOMY Conversation";
    private const string Hugo = "Hugo Toras";
    private const string Laurento = "Laurento Esquibel";
    private const string R = Talk.Romus;
    private const string VarNews = "ZilleWar.Fin_RrgWarNews";

    private static readonly string NoPales = "Variable[\"RiziaDLC.Pales_War\"] ~= true";
    private static readonly string Zille = And(War, NoPales);
    private static readonly string ZilleOver = And(Zille, WarFlags.L(WarFlags.Over));
    private static readonly string ZilleOn = And(Zille, Ongoing);
    private static readonly string Two = WarFlags.L(WarFlags.TwoWars);
    private static string Gold(string v) => $"Variable[\"{LateT8.VarGold}\"] == \"{v}\"";
    private static readonly string NoGold = $"(Variable[\"{LateT8.VarGold}\"] == nil or Variable[\"{LateT8.VarGold}\"] == \"\")";
    private static readonly string Pledged = $"Variable[\"{SweepEarlyT6Home.VarRustyZilleGold}\"] == \"pledged\"";
    private static readonly string LespiaHelped = "((Variable[\"ZilleWar.Lever_Lespia\"] or 0) > 0 or Variable[\"ZilleWar.Lespia_StraitClosed\"] == true)";

    public static void Register()
    {
        Variables.Register(VarNews);

        var t = new Talk(Conv, "ZilleWar.Fin.Gold.");

        // Hugo raises it in a Zille war too (vanilla 279: Pales_War only).
        t.Logic("Open", new[] { "#281" }, condition: Zille, hooks: Talk.From(278));
        t.Variant("Powers", 283, new[] { 282 }, ZilleOver, Hugo,
            "\"The emergency powers the Crown took for the war in Zille have not yet been lifted. Under them, the Crown may nationalize any " +
            "corporation deemed essential to the defense and security of the realm.\"", new[] { "#286" });

        // The gold of Auremina.
        string[] menu = { "Ties", "#289", "#294" };
        t.Line("GoldRrg", Hugo,
            "\"Particularly now that we have handed it the Auremina concession. Mr. Montoro would be digging up the gold our soldiers paid for, and selling it to whoever pays best.\"",
            menu, condition: And(Zille, Gold("rrg")), hooks: Talk.From(286));
        t.Line("GoldCrown", Hugo,
            "\"The Crown already holds the Auremina mines. What it lacks are the engineers, the pumps and the refinery in Bronaz. All three belong to Mr. Montoro.\"",
            menu, condition: And(Zille, Gold("crown")), hooks: Talk.From(286));
        t.Line("GoldSazon", Hugo,
            "\"House Sazon has the Auremina mines now. Mr. Montoro has the pumps, the engineers and the only refinery in Rizia that can take Zille ore. One of the two, at least, ought to answer to the Crown.\"",
            menu, condition: And(Zille, Gold("sazon")), hooks: Talk.From(286));
        t.Line("GoldPledge", Hugo,
            "\"Mr. Montoro bought himself first claim on the Auremina mines with a check to the war chest. If his company belongs to the Crown, so does his claim.\"",
            menu, condition: And(Zille, NoGold, Pledged), hooks: Talk.From(286));
        t.Line("GoldWar", Hugo,
            "\"While our soldiers fight for Zille, Montoro's gold leaves Bronaz every week for vaults in Arcasia. Our war chest has not seen an ounce of it.\"",
            menu, condition: And(ZilleOn, NoGold, $"not ({Pledged})"), hooks: Talk.From(286));
        t.Line("GoldBill", Hugo,
            "\"We are still paying for Zille. Mr. Montoro, as far as I can tell, is not.\"",
            menu, condition: And(ZilleOver, NoGold, $"not ({Pledged})"), hooks: Talk.From(286));

        // The case against Montoro: Arcasia has no quarrel with us over Zille; Lespia is the question.
        t.Variant("Ties", 287, new[] { 286 }, Zille, R,
            "\"Rusty does have known ties to Arcasia. And nobody at this table can tell me where his gold went while we were paying for Zille.\"",
            new[] { "Laurento" });
        t.Line("Laurento", Laurento,
            "\"Arcasia has no love for President Smolak, Your Majesty. If anything, Walker would cheer to see Wehlen humbled. Mr. Montoro's Arcasian friends are bankers, not spies.\"",
            new[] { "Lespia_L", "Lespia_Ta", "Lespia_Ti", "Quiet_L", "Quiet_Ta", "Quiet_Ti" });
        Councilor(t, "Lespia",
            "\"Lespia closed the Strait of Kreleck for us, and sent us the bill. Everyone knows they're Arcasia's lapdog. I'd rather Montoro's gold sat in our vaults than became the next thing Mr. Alvarez asks for.\"",
            new[] { "#310" }, condition: LespiaHelped);
        Councilor(t, "Quiet",
            "\"Lespia hasn't lifted a finger for us in Zille, and everyone knows they're Arcasia's lapdog. Montoro has friends in both capitals. I'd rather his gold sat in our vaults.\"",
            new[] { "#310" }, condition: $"not ({LespiaHelped}) and (Variable[\"ZilleWar.Lever_Lespia\"] or 0) == 0");

        // Paying for the war: after it, or for two of them.
        t.Variant("PayOver", 288, new[] { 290 }, ZilleOver, R, "\"Someone has to pay for the war in Zille. It may as well be Rusty Montoro.\"", C("Funds"));
        t.Variant("PayTwo", 288, new[] { 290 }, Two, R, "\"If we want to keep funding two wars, such a measure may be necessary.\"", new[] { "#307" });
        Councilor(t, "Funds",
            "\"Thank you for bringing this up, Your Grace. The war in Zille emptied our arsenals, and the money from nationalization would help fill them again.\"",
            new[] { "#308" });
        t.Variant("CanOver", 292, new[] { 291 }, ZilleOver, R, "\"We can pay for Zille without resorting to such measures.\"", new[] { "#299" });
        t.Variant("CanTwo", 292, new[] { 291 }, Two, R, "\"We can fund both wars without resorting to such measures.\"", new[] { "#299" });

        // "We can't afford to pull people from the front lines" once the guns in Zille are quiet.
        Swap(t, Conv, "Manpower", 342, new[] { 330, 333 }, R, new S(
            Lib: "\"That will take manpower. We need every man we have to hold Zille.\"",
            Part: "\"That will take manpower. We need every man we have on the line in Zille.\"",
            Lost: "\"That will take manpower, and the army has not yet recovered from Zille.\""),
            new[] { "#346" }, when: NoPales);
        t.Register();
    }

    public static void OnStep()
    {
        if (!Variables.GetBool(WarFlags.Declared) || Variables.GetBool(VarNews) || Variables.GetBool("RiziaDLC.Pales_War") ||
            !Variables.GetBool("RiziaDLC.NationalizeRRG_War") ||
            !(Variables.GetBool("RiziaDLC.Decision_Turn08_RRGNationalization_ByForce") || Variables.GetBool("RiziaDLC.Decision_Turn08_RRGNationalization_ByCash")))
        {
            return;
        }

        Variables.Set(VarNews, true);
        bool active = WarFlow.AtWar;
        string gold = Variables.GetString(LateT8.VarGold);
        string mines = gold switch
        {
            "rrg" => "The Auremina concession, granted to the company only weeks ago, passes to the Crown along with it.",
            "crown" => "With the Auremina mines already in royal hands, the Crown now owns everything from the pits in Zille to the refinery in Bronaz.",
            "sazon" => "House Sazon, which runs the Auremina mines, is said to be watching closely: the company's refinery in Bronaz is the only one in the country that can take Zille ore.",
            _ => active
                ? "Officials at the Treasury say the company's proceeds will go first to the war in Zille."
                : "Officials at the Treasury say the company's proceeds will go first to the bills left by the war in Zille.",
        };
        NewsWire.Publish("Fin_RrgWar", "RH", active ? "Royal Gold Goes to War" : "Royal Gold to Pay the War's Bills", NewsWire.P(
            "The Palace has confirmed that Rizian Royal Gold is now fully under state control, under the emergency powers the Crown took when " +
            "the war in Zille began. " + mines,
            "\"Gold dug from Rizian soil will pay for Rizian soldiers,\" a Palace spokesman told the Herald. Bankers in Thornborough and in Arcasia " +
            "were less enthusiastic, and RRG bonds lost a third of their value by the end of the day."));
    }
}
