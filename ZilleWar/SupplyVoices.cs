namespace ZilleWar;

/// <summary>
/// The other capitals during the war, each with its own interest:
/// - Valgsland (Chancellor Hegel): first a cable read out by Laurento (the Wehzek miners in Isor, Contanan arms through
///   Valgslandian ports, the fleet in the Markian Sea), then, if Rizia escalated, a call: ground the submarines or lose
///   Valgslandian ports ("Rizia_Budget_Modifier_ZilleWarValgslandPorts").
/// - Wehlen (President Smolak): a ceasefire on the present line, priced in vanilla's own currency (Rico's extradition,
///   the Bludish border ban, full Wehzek rights). Accepting ends the war through WarFlow.VarCeasefire.
/// - Morella (Prime Minister Saltana): unmarked aircraft over the Gulf of Moridia, priced by Rizia's MITZ stance.
/// - The consulate files: found once Zille city is in Rizian hands, they name Héctor Ramiro of the Lespian consulate as
///   the organiser of the Friendship Day bombing. Publish, confront Alvarez, keep it as leverage, or burn it.
/// </summary>
internal static partial class SupplyWar
{
    private const string RicoExtradition = "RiziaDLC.Diplomacy_Wehlen_PromiseRicoExtradition";
    private const string BludishBan = "RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishBan";
    private const string WehzekRights = "RiziaDLC.Situation_Law_WehlenRights_Full";
    private const string ValgslandMitz = "RiziaDLC.Diplomacy_Valgsland_PromiseITZTakeover";
    private const string Discovered = "RiziaDLC.Zille_Discovered_LespianInvolvement";
    private const string Publicized = "RiziaDLC.Zille_Publicized_LespianInvolvement";

    /// <summary>The Valgslandian and CSP embargo of WorldEmbargo is in force (same exclusions as its HUD line).</summary>
    internal static readonly string EmbargoOn =
        $"({Is(AnDeclaration.VarDeclared)} and {Not("RiziaDLC.Policy_Diplomacy_Caleqabiz_ValgslandCSP")} and {Not("RiziaDLC.Trait_CommieKing")})";

    // ------------------------------------------------------------------ Valgsland

    private static void RegisterHegel()
    {
        var d = new Dlg(HegelConv, "ZilleWar.Hegel.");
        string stage = N(VarValgslandStage);
        string stageDone = SetTurn(VarValgslandStageTurn);

        d.Start(HegelConv, ResetAsked, "Which");
        d.Branch("Which", "CableIn", "CallIn");

        // ---- stage 1: the cable
        d.SceneIf("CableIn", $"{stage} == 0",
            "Laurento came in without knocking, which he never did, holding a cable form between two fingers as if it might stain them.",
            "PlaySceneMusic(Diplomacy);", $"AddConversant(\"{Esquibel}\");", "Cable1");
        d.Say("Cable1", Esquibel,
            "\"From Chancellor Hegel, Your Majesty. His ambassador delivered it in person, with the face of a man reading out a will.\"", "Cable2");
        d.Say("Cable2", Esquibel,
            "\"'The Federal Republic of Valgsland notes with grave concern the war waged by the Kingdom of Rizia against a fraternal " +
            "people.' He goes on like that for a page. Then he gets to the point. Three points, in fact.\"", "Cable2b");
        // The Valgslandian and CSP embargo (WorldEmbargo) has been in force since the declaration; the cable builds on it.
        d.Branch("Cable2b", "Cable2E", "Cable3");
        d.SayIf("Cable2E", EmbargoOn, Esquibel,
            "\"He begins by reminding us that his embargo stays in force until the last Rizian soldier has left Wehzek soil. As if " +
            "Councilor Werner would let us forget it. Then he moves on to what Valgsland might do next.\"", "Cable3");
        d.Say("Cable3", Esquibel,
            "\"First, the Wehzek miners around Isor. Valgslandian unions count hundreds of them as brothers, he says, and if any harm " +
            "comes to them, Valgsland will raise it at the Alliance of Nations and move for sanctions.\"", "Cable4");
        d.Say("Cable4", Esquibel,
            "\"Second, ships carrying Contanan cargo to Wehlen will go on calling at Valgslandian ports, because Valgsland does not " +
            "interfere with lawful commerce. By lawful commerce, he means rifles.\"", "Cable5");
        d.Say("Cable5", Esquibel,
            "\"Third, the Valgslandian fleet will continue its exercises in the Markian Sea. No explanation offered. None needed, I " +
            "suppose.\"", "Cable6");
        d.Say("Cable6", Esquibel,
            "\"He wants an answer, Your Majesty. Whatever we send, he'll read it aloud somewhere.\"",
            "Guarantee", "Defy", Gate("OfferMitz"), "Stall");
        d.Opt("Guarantee", null,
            "\"Give him his guarantees. The miners of Isor will be safe, and his observers may come and see it for themselves.\"",
            "-1 Authority", Inc(Authority, -1) + Talk.Weaken(1) + S(VarValgslandCable, "guarantee") +
                            $"{V(VarValgslandStage)} = 1; " + stageDone, "Guarantee2");
        d.Say("Guarantee2", Esquibel,
            "\"He'll accept it as his due. He may hold a few Contanan freighters in port for inspection, just to remind you who is " +
            "watching. Hegel does not thank anyone.\"", "CableEnd");
        d.Opt("Defy", null, "\"Rizia takes no lectures from Valgsland. Tell him so.\"", "+1 Authority",
            Inc(Authority, 1) + Inc(NpValgsland, -2) + Strengthen(1) + S(VarValgslandCable, "defied") + $"{V(VarValgslandStage)} = 1; " +
            stageDone, "Defy2");
        d.Say("Defy2", Esquibel,
            "\"I'll find a more diplomatic way of putting it. He'll understand it anyway. And I'm afraid the Contanan freighters will now " +
            "sail exactly on time.\"", "CableEnd");
        d.Opt("OfferMitz", $"{Not(VarLespiaMitz)} and {Not(MitzPromise)} and {Not(MitzPromiseLate)} and {Not(ValgslandMitz)}",
            "\"Tell him Rizia might see the Meftiem his way, if Valgsland's ports see this war ours.\"",
            "Promises Rizia's MITZ Vote to Valgsland",
            T(ValgslandMitz) + Inc(NpLespia, -1) + Talk.Weaken(1) + S(VarValgslandCable, "mitz") +
            $"{V(VarValgslandStage)} = 1; " + stageDone, "Mitz2");
        d.Say("Mitz2", Esquibel,
            "\"A dangerous promise, Your Majesty. Alvarez will hear of it before dinner. But I daresay the Contanan freighters will " +
            "find Valgslandian harbors suddenly very crowded.\"", "CableEnd");
        d.Opt("Stall", null, "\"Thank the Chancellor for his concern, and say nothing else.\"", null,
            S(VarValgslandCable, "stalled") + $"{V(VarValgslandStage)} = 1; " + stageDone, "Stall2");
        d.Say("Stall2", Esquibel, "\"A non-answer. Hegel is fluent in those. He'll write again, or he'll ring.\"", "CableEnd");
        d.Say("CableEnd", Narrator, "Laurento folded the cable along its old creases and slipped it into his breast pocket.", "End");

        // ---- stage 2: Hegel rings himself
        d.Scene("CallIn",
            "An assistant brought in the blue telephone. The operator announced Valgsland, and the line went so quiet I could hear " +
            "the Chancellor breathing before he spoke.", null, Music + $"AddConversant(\"{Hegel}\");", "Call1");
        d.Say("Call1", Hegel, "\"Mr. Toras. I will be brief, since you are busy.\"", "Front");
        d.Branch("Front", "FrontAur", "FrontZille", "Why");
        d.SayIf("FrontAur", Is(ZilleOperation.CapturedVar("Auremina")), Hegel,
            "\"Your army is in the Auremina hills now. The miners of Isor can hear its guns from their pitheads, and their unions can " +
            "hear them in Valgsland.\"", "Why");
        d.SayIf("FrontZille", Is(WarFlags.ZilleCityHeld), Hegel,
            "\"You hold Zille. Then you hold its Wehzeks too, Mr. Toras, and Valgsland is counting every one of them.\"", "Why");
        d.Branch("Why", "WhyVillage", "WhyCaleqabiz", "WhyLespia", "WhyBff", "WhySubs", "WhyCable");
        d.SayIf("WhyVillage", Eq(VarBludsIncident, "kept"), Hegel,
            "\"Your Bludish mercenaries shot eleven villagers in the hills above Zille, and you kept them at the front. The photographs " +
            "are pinned to the wall of my cabinet room, Mr. Toras. I have asked my ministers to look at them every morning.\"", "Demand");
        d.SayIf("WhyCaleqabiz", Is(VarLespiaCaleqabiz), Hegel,
            "\"Lespian warships now refuel at Caleqabiz. You have invited ATO into the Antacean, Mr. Toras. Do not be surprised when " +
            "the Contanan Security Pact answers.\"", "Demand");
        d.SayIf("WhyLespia", Is(VarLespiaStrait), Hegel,
            "\"Lespian warships are stopping ships in the Strait of Kreleck on your behalf. Alvarez has wanted a hand on that strait for " +
            "years, and you have given him both. Valgsland will not sail its merchants past Lespian inspectors, Mr. Toras.\"", "Demand");
        d.SayIf("WhyBff", $"{N(Count(Lever.BffRaids))} > 0", Hegel,
            "\"You have hired the Bludish Freedom Front to blow up harbors full of dockworkers. Valgsland has a word for that, and so " +
            "will the Alliance.\"", "Demand");
        d.SayIf("WhySubs", $"{N(Count(Lever.Submarines))} >= 2", Hegel,
            "\"Your submarines have been sighted twice near our patrol lines in the Markian approaches. Twice is not an accident.\"", "Demand");
        d.Say("WhyCable", Hegel, "\"My ambassador's cable received an answer in no language I recognize.\"", "Demand");
        // Rizia sold its Meftiem vote to Lespia for Lespia's vote at the Alliance (SupplyAlvarez): Hegel knows.
        const string veto = "\"And do not think Mr. Alvarez's vote in Konstantium protects you. He can kill a motion. He cannot open " +
                            "a Valgslandian port.\"";
        d.Branch("Demand", "DemandE", "DemandPlain");
        d.SayIf("DemandE", EmbargoOn, Hegel,
            "\"Valgslandian destroyers are in the Markian Sea, and they will stay there. Here is how this ends quietly: your submarines " +
            "stay south of the strait. Otherwise Valgsland will move for sanctions at the Alliance. And our embargo, which has so far " +
            "let your cargo reach you through Agnolian and Sordish holds, will close that door as well. Any ship that has called at a " +
            "Rizian port will be turned away from ours.\"", "VetoE");
        d.Branch("VetoE", "VetoESay", "MenuE");
        d.SayIf("VetoESay", Is(VarLespiaAllianceVote), Hegel, veto, "MenuE");
        d.Do("MenuE", null, "Yield", "RefuseE");
        d.Say("DemandPlain", Hegel,
            "\"Valgslandian destroyers are in the Markian Sea, and they will stay there. Here is how this ends quietly: your submarines " +
            "stay south of the strait. Otherwise Valgsland will move for sanctions at the Alliance, and in the meantime our ports will " +
            "be closed to Rizian cargo.\"", "Veto");
        d.Branch("Veto", "VetoSay", "Menu");
        d.SayIf("VetoSay", Is(VarLespiaAllianceVote), Hegel, veto, "Menu");
        d.Do("Menu", null, "Yield", "Refuse");
        d.Opt("Yield", null, "\"Very well. The submarines stay south.\"",
            "No More Submarine Patrols", T(VarSubsGrounded) + S(VarValgslandCall, "yielded") +
            $"{V(VarValgslandStage)} = 2; " + stageDone, "Yield2");
        d.Say("Yield2", Hegel,
            "\"Good. You see, Mr. Toras, socialists and kings can agree on one thing at least. Fewer funerals.\"", "CallEnd");
        string refuse = T(VarValgslandPorts) + $"{V(ModValgsland)} = -1; " + Inc(NpValgsland, -2) + S(VarValgslandCall, "defied") +
                        $"{V(VarValgslandStage)} = 2; " + stageDone;
        d.Opt("Refuse", null, "\"Close your ports, then. Rizia will manage without Valgslandian cargo.\"", "-1 Budget Per Turn", refuse, "Refuse2");
        d.Opt("RefuseE", null, "\"Tighten your embargo, then. Rizia has managed so far.\"", "-1 Budget Per Turn", refuse, "Refuse2");
        d.Say("Refuse2", Hegel, "\"Then we will see how well. Good day, Mr. Toras.\"", "CallEnd");
        d.Say("CallEnd", Narrator, "The line clicked and went dead. Hegel had never once called me Your Majesty.", "End");
        d.Finish();
    }

    // ------------------------------------------------------------------ Wehlen

    private static void RegisterSmolak()
    {
        var d = new Dlg(SmolakConv, "ZilleWar.Smolak.");
        string s = N(ZilleCampaign.VarStrength);
        string held = Is(WarFlags.ZilleCityHeld);
        string calls = N(VarSmolakCalls);
        string reduced = $"{s} <= 4";
        string full = $"{s} > 4";
        string ceasefire = T(WarFlow.VarCeasefire) + Inc(NpWehlen, 2);

        d.Start(SmolakConv, ResetAsked + Inc(VarSmolakCalls, 1) + SetTurn(VarSmolakTurn) + Music, "Phone");
        d.SayDo("Phone", Esquibel,
            "\"President Smolak, Your Majesty. Through the Kyrutian embassy. He asked for you by name, and then by several other names.\"",
            $"AddConversant(\"{Esquibel}\"); AddConversant(\"{Smolak}\");", "Open");
        d.Branch("Open", "OpenAgain", "OpenQuiet", "OpenAuremina", "OpenHeldFedo", "OpenHeld", "OpenMissed", "OpenStrong", "OpenPlain");
        d.SayIf("OpenQuiet", Is(VarCallHeld), Smolak,
            "\"Your Majesty. Your guns have been quiet for a month. Either you are tired or you are thinking, and I am calling in case it " +
            "is the second.\"", "Offer");
        d.SayIf("OpenAuremina", Is(ZilleOperation.CapturedVar("Auremina")), Smolak,
            "\"Your Majesty. You have my gold hills, and the miners of Auremina are learning your anthem with a Wehzek accent. Enjoy " +
            "them. Gold is heavy to carry home.\"", "Offer");
        d.SayIf("OpenMissed", $"not {held} and {N(VarCallFought)} >= 1 and {Not(VarCallLastWon)}", Smolak,
            "\"Your Majesty. Your army came down the river, looked at Zille, and went back up it again. I listened on the radio. I " +
            "thought that now, while you are counting, you might be in the mood to listen.\"", "Offer");
        d.SayIf("OpenHeldFedo", $"{held} and {Is(CityEvents.Flag("Zille_BannerRaised"))}", Smolak,
            "\"Your Majesty. Your soldiers are sleeping in my harbor, in my hotels, and in the square that had my name on it until they " +
            "unscrewed it. Fedo Square. How very old-fashioned. I hope the beds are comfortable.\"", "Offer");
        d.SayIf("OpenAgain", $"{calls} >= 2", Smolak,
            "\"Your Majesty. Last month you told me Zille was not for sale. I have since learned that everything in Zille is for sale, " +
            "beginning with my soldiers' rifles. So I will be shorter this time. My offer has improved, because my situation has not.\"", "Offer");
        d.SayIf("OpenHeld", held, Smolak,
            "\"Your Majesty. Your soldiers are quartered in my harbor and my hotels. I hope the beds " +
            "are comfortable.\"", "Offer");
        d.SayIf("OpenStrong", $"{s} >= 7", Smolak,
            "\"Your Majesty. My generals tell me your army has been very brave and very unlucky. I thought I would call before it " +
            "becomes very dead.\"", "Offer");
        d.Say("OpenPlain", Smolak,
            "\"Your Majesty. You have made your point, and the newspapers in Wraclawitz have made theirs. Perhaps now the grown-ups can talk.\"",
            "Offer");
        d.Branch("Offer", "OfferAgain", "Offer1");
        d.SayIf("OfferAgain", $"{calls} >= 2", Smolak,
            "\"The same offer, then, at a better price. A ceasefire on the line where our armies stand tonight, and in return...\"", "Terms");
        d.Say("Offer1", Smolak,
            "\"Here is my offer. A ceasefire on the line where our armies stand tonight. The Alliance draws a pretty map, everybody " +
            "goes home, and in return...\"", "Terms");
        // Each demand is voiced only while it is still open: Ricardus while he is held and not yet handed over, the border ban
        // and the Wehzek rights until the King has already granted them.
        string rico = $"(Variable[\"RiziaDLC.Rico_Arrested\"] == true and Variable[\"RiziaDLC.Rico_Extradited\"] ~= true and " +
                      $"Variable[\"{RicoExtradition}\"] ~= true)";
        string ban = $"(Variable[\"{BludishBan}\"] ~= true)";
        string rights = $"(Variable[\"{WehzekRights}\"] ~= true)";
        d.Branch("Terms", "TermsNone", "TRico", "TBan");
        d.SayIf("TermsNone", $"not {ban} and not {rights} and ({reduced} or not {rico})", Smolak,
            "\"...nothing, Your Majesty, that you have not already given me. A ceasefire on the line where we stand, and an end to it. " +
            "I find I have less appetite than I used to.\"", "Aside");
        d.SayIf("TRico", $"{full} and {rico}", Smolak,
            "\"...your cousin Ricardus comes to Wraclawitz to answer for Smolak Square.\"", "TBan");
        d.Branch("TBan", "TBanSay", "TRights");
        d.SayIf("TBanSay", ban, Smolak,
            "\"Your border closes to the Bludish bandits Thornbourgh keeps arming.\"", "TRights");
        d.Branch("TRights", "TRightsSay", "TEnd");
        d.SayIf("TRightsSay", rights, Smolak,
            "\"And every Wehzek living in Rizia gets the same rights as a Rizian.\"", "TEnd");
        d.Branch("TEnd", "TEndFull", "TEndReduced");
        d.SayIf("TEndFull", full, Smolak,
            "\"The very terms my ministers drafted before you decided to shoot instead.\"", "Aside");
        d.Say("TEndReduced", Smolak,
            "\"Keep your cousin. I find I have less appetite than I used to.\"", "Aside");
        d.Branch("Aside", "AsideBeatrice", "AsideAgain", "AsidePlain");
        d.SayIf("AsideAgain", $"{calls} >= 2", Esquibel,
            "Laurento leaned in. \"He is asking for less than he did last month, Your Majesty. That is what a garrison on half rations " +
            "sounds like. But the line where we stand tonight is still the line we would keep.\"", "Answer");
        d.SayIf("AsideBeatrice", Is(VarRumburgWantsCeasefire), Esquibel,
            "Laurento leaned in and spoke quietly. \"A ceasefire on the present line means we keep what we hold, Your Majesty. And it " +
            "would give Queen Beatrice the end date she asked for.\"", "Answer");
        d.Say("AsidePlain", Esquibel,
            "Laurento leaned in and spoke quietly. \"A ceasefire on the present line means we keep what we hold, Your Majesty. Whether " +
            "that is enough is for you to decide.\"", "Answer");

        d.Branch("Answer", Gate("AcceptFull"), Gate("AcceptReduced"), Gate("Counter"), "Refuse");
        d.Opt("AcceptFull", full, "\"Rizia accepts. The guns go quiet at midnight.\"", "-2 Authority",
            ceasefire + Inc(Authority, -2) + T(RicoExtradition) + T(BludishBan) + T(WehzekRights) + S(VarSmolakAnswer, "accepted") +
            S(VarCeasefireTerms, "full"), "Accepted");
        d.Opt("AcceptReduced", reduced, "\"Rizia accepts. The guns go quiet at midnight.\"", "-1 Authority",
            ceasefire + Inc(Authority, -1) + T(BludishBan) + T(WehzekRights) + S(VarSmolakAnswer, "accepted") +
            S(VarCeasefireTerms, "reduced"), "Accepted");
        d.Say("Accepted", Smolak,
            "\"Midnight, then. I will tell my generals. You may tell your cousin.\"", "Close");
        d.Opt("Counter", $"{full} and {Not(Q(1))}", "\"Rights for your Wehzeks, and nothing else. That is my only offer.\"", null, T(Q(1)),
            "CounterYes", "CounterNo");
        d.SayIf("CounterYes", $"{s} <= 5", Smolak,
            "\"You drive a hard bargain for a man with mud on his boots.\" A long breath crackled down the line. \"Fine. Rights for " +
            "my Wehzeks. Midnight.\"", "CounterSeal");
        d.Opt("CounterSeal", null, "\"Midnight.\"", "-1 Authority",
            ceasefire + Inc(Authority, -1) + T(WehzekRights) + S(VarSmolakAnswer, "countered") + S(VarCeasefireTerms, "rights"), "Close");
        d.Say("CounterNo", Smolak,
            "\"No. You are not winning enough to dictate, Your Majesty. My terms, or none.\"", "Answer");
        d.Opt("Refuse", null, "\"Zille isn't for sale, Mr. President. Not even for peace.\"", "+1 Authority",
            Inc(Authority, 1) + Inc(NpWehlen, -1) + S(VarSmolakAnswer, "refused") + $"if {s} >= 5 then {Strengthen(1)} end; ",
            "Refused");
        d.Branch("Refused", "RefusedReserves", "RefusedStarving");
        d.SayIf("RefusedReserves", $"{s} >= 6", Smolak,
            "\"Then we keep burying boys. I have called up the reserves from the eastern provinces this morning, Your Majesty. You " +
            "will be meeting them shortly.\"", "Close");
        d.Say("RefusedStarving", Smolak,
            "\"Then we keep burying boys. Mine are hungry and yours are dying. I hope the history books are kind to you. They are " +
            "rarely kind to me.\"", "Close");
        d.Branch("Close", "CloseAgain", "Close1");
        d.SayIf("CloseAgain", $"{calls} >= 2", Narrator,
            "This time Smolak did not wait to hear how I took it. The line was dead while I still had the receiver at my ear.", "End");
        d.Say("Close1", Narrator, "The line went dead before Laurento could take the receiver from me.", "End");
        d.Finish();
    }

    // ------------------------------------------------------------------ Morella

    private static void RegisterSaltana()
    {
        var d = new Dlg(SaltanaConv, "ZilleWar.Saltana.");
        string budget = N(Budget);
        string cheap = $"({Is(VoteNationalization)} or {Is(ValgslandMitz)})";
        string dear = $"(not {cheap} and ({Is(VarLespiaMitz)} or {Is(MitzPromise)} or {Is(MitzPromiseLate)} or {Is(VoteStatusQuo)}))";
        string fair = $"(not {cheap} and not {dear})";
        string buy = T(VarMorellaAircraft) + Inc(Fighters, 1) + Talk.Weaken(1) + Inc(NpMorella, 1);

        d.Start(SaltanaConv, ResetAsked + T(VarSaltanaCalled) + Music, "Phone");
        d.Scene("Phone",
            "The blue telephone rang a little after nine. The operator announced Cavette, and then Prime Minister Saltana was on the " +
            "line, brisk as a ledger.", null, $"AddConversant(\"{Saltana}\");", "S1");
        d.Say("S1", Saltana,
            "\"Your Majesty. I'll be direct, since you've stopped pretending to be anything other than a king at war.\"", "S2");
        d.Branch("S2", "S2Road", "S2Zille");
        d.SayIf("S2Road", Not(WarFlags.ZilleCityHeld), Saltana,
            "\"Wehzek fighters have been crossing the Gulf of Moridia to strafe your columns on the Zille road. Morella doesn't care for " +
            "anyone's fighters over our gulf. Least of all Wiktor Smolak's.\"", "S3");
        d.Say("S2Zille", Saltana,
            "\"Wehzek fighters have been crossing the Gulf of Moridia to strafe your positions in Zille. Morella doesn't care for " +
            "anyone's fighters over our gulf. Least of all Wiktor Smolak's.\"", "S3");
        d.Branch("S3", "S3More", "S3First");
        d.SayIf("S3More", Is("RiziaDLCSupport.Reports_Turn05_Terador_AirplaneDeal"), Saltana,
            "\"I have more aircraft. Good ones, built in Morella, the same as the two dozen already at Terador. A second squadron, " +
            "without markings, without Morellan pilots, and with no paperwork that anyone in Konstantium could trace.\"", "Price");
        d.Say("S3First", Saltana,
            "\"I have aircraft. Good ones, built in Morella. I could sell you a squadron, without markings, without Morellan pilots, " +
            "and with no paperwork that anyone in Konstantium could trace.\"", "Price");
        d.Branch("Price", "PriceCheap", "PriceDear", "PriceFair");
        d.SayIf("PriceCheap", cheap, Saltana,
            "\"Rizia stood with the workers on the Meftiem, so you'll have the price I would give a friend. Three billion guilders for the squadron.\"", "Menu");
        d.SayIf("PriceDear", dear, Saltana,
            "\"I hear you've sided with Mr. Alvarez at the Meftiem. Then you can pay Alvarez prices. Nine billion guilders for the squadron.\"", "Menu");
        d.Say("PriceFair", Saltana, "\"Six billion guilders for the squadron. The price is fair. Ask anyone who has bought from us.\"", "Menu");

        d.Branch("Menu", Gate("Buy1"), Gate("Buy2"), Gate("Buy3"), Gate("Gulf"), "Decline");
        const string send = "\"Send the planes.\"";
        d.Opt("Buy1", $"{cheap} and {budget} >= 1", send, "-1 Budget, +1 Fighter Jet", buy + Inc(Budget, -1), "Bought");
        d.Opt("Buy2", $"{fair} and {budget} >= 2", send, "-2 Budget, +1 Fighter Jet", buy + Inc(Budget, -2), "Bought");
        d.Opt("Buy3", $"{dear} and {budget} >= 3", send, "-3 Budget, +1 Fighter Jet", buy + Inc(Budget, -3), "Bought");
        d.Say("Bought", Saltana,
            "\"They'll arrive at night, painted grey, at whichever airfield your General Staff names. If one of them is shot down, " +
            "Morella has never heard of it. Good evening, Your Majesty, and do keep your war on your side of the gulf.\"", "Hangup");
        d.Opt("Gulf", Not(Q(1)), "\"Could Morella close the gulf to Wehzek shipping instead?\"", null,
            T(Q(1)) + $"if {Not(VarSaltanaGulf)} then {Talk.Weaken(1)} end; " + T(VarSaltanaGulf), "Gulf2");
        d.Say("Gulf2", Saltana,
            "\"Close it? With what, Your Majesty, my coast guard's rowing boats? No. But I'll tell you this much: our coast guard can " +
            "be very slow to answer Wehzek distress calls.\"", "Menu");
        d.Opt("Decline", null, "\"Rizia's pilots will manage.\"", null, null, "Decline2");
        d.Say("Decline2", Saltana, "\"Then I'll pray for them, in my own fashion. Good evening, Your Majesty.\"", "Hangup");
        d.Say("Hangup", Narrator, "Saltana rang off without waiting for my reply.", "End");
        d.Finish();
    }

    // ------------------------------------------------------------------ the consulate files

    private static void RegisterConsulate()
    {
        var d = new Dlg(ConsulateConv, "ZilleWar.Consulate.");
        string strait = Is(VarLespiaStrait);
        string found = T(VarRamiroFound);

        d.Start(ConsulateConv, ResetAsked, "Room");
        d.Branch("Room", "RoomL", "RoomC");
        d.SceneIf("RoomL", LucitaHere,
            "Laurento and Lucita were waiting in my study with a cardboard box on the desk between them. It smelled of smoke and wet paper.",
            "PlaySceneMusic(Suspense);", $"AddConversant(\"{Esquibel}\"); AddConversant(\"{Lucita}\");", "F1");
        d.Scene("RoomC",
            "Laurento and General Robles Azaro were waiting in my study with a cardboard box on the desk between them. It smelled of " +
            "smoke and wet paper.", "PlaySceneMusic(Suspense);", $"AddConversant(\"{Esquibel}\"); AddConversant(\"{Carlos}\");", "F1");
        d.Staff("F1",
            "\"These came out of the Wehzek police headquarters in Zille. They burned what they could before our men got there. They " +
            "didn't burn this.\"",
            "\"These came out of the Wehzek police headquarters in Zille, Your Majesty. They burned what they could before we got " +
            "there. Not this.\"", "F2");
        d.Staff("F2",
            "\"Smolak's own investigators traced the Friendship Day bomb. Not to Su Omina. The components came through a firm in Pales " +
            "called CircuitRise Systems, and they were handed to a man named Héctor Ramiro.\"",
            "\"Smolak's own investigators traced the Friendship Day bomb, Your Majesty. Not to Su Omina. The parts came through a Palesian " +
            "firm, CircuitRise Systems, and went to a man named Héctor Ramiro.\"", "F3");
        d.Say("F3", Esquibel,
            "\"Ramiro is a political officer at the Lespian consulate in Zille, Your Majesty. In the diplomatic quarter, everyone knows " +
            "what that title means. He's a spy, and he has diplomatic immunity.\"", "F3b");
        d.Branch("F3b", "F3Kept", "F3Burned", "F4");
        d.SayIf("F3Kept", Eq(AnDeclaration.VarLespianNote, "kept"), Esquibel,
            "He touched his breast pocket, where I knew he still kept Alvarez's note from Konstantium. \"'Practical arrangements. " +
            "Discreetly.' He must have had it written before you stood up to speak, Your Majesty. I wondered at the time how he could be " +
            "so well prepared.\"", "F4");
        d.SayIf("F3Burned", Eq(AnDeclaration.VarLespianNote, "burned"), Esquibel,
            "\"You burned his note in Konstantium so that Lespia's fingerprints would not be on this war, Your Majesty. It seems " +
            "they were on it before it began.\"", "F4");
        d.Branch("F4", "F4Deal", "F4NoDeal");
        d.SayIf("F4Deal", $"{N(Count(Lever.Lespia))} > 0", Esquibel,
            "\"If this is true, Prime Minister Alvarez lit the fuse that started this war, and now he is selling us the bucket of water.\"", "F5");
        d.Say("F4NoDeal", Esquibel,
            "\"If this is true, Prime Minister Alvarez lit the fuse that started this war, and he has been offering to sell us the water " +
            "ever since.\"", "F5");
        d.Staff("F5",
            "\"Smolak sat on it. A Lespian bomb was no use to him. A Rizian one kept him in Zille.\"",
            "\"Smolak sat on it, Your Majesty. A Lespian bomb was no use to him. A Rizian one kept him in Zille.\"", "Choose");

        d.Branch("Choose", Gate("Who"), Gate("PublishStrait"), Gate("PublishFree"), "Confront", "Leverage", "Bury");
        d.Opt("Who", Not(Q(1)), "\"Who else has seen these?\"", null, T(Q(1)), "Who2");
        d.Staff("Who2",
            "\"Whoever in Wraclawitz read them and decided they were inconvenient. And now the three of us.\"",
            "\"Whoever in Wraclawitz found them inconvenient, Your Majesty. And now the three of us.\"", "Choose");
        string publish = found + T(Discovered) + T(Publicized) + S(VarRamiro, "publish") + Inc(NpLespia, -3) +
                         Inc("RiziaDLC.Country_GlobalPerception", 1);
        const string publishText = "\"Publish it. The world should know who started this war.\"";
        d.Opt("PublishStrait", strait, publishText, "Ends the Lespian Blockade",
            publish + F(VarLespiaStrait) + T(VarLespiaWithdrawn) + Strengthen(1), "Published");
        d.Opt("PublishFree", $"not {strait}", publishText, null, publish + T(VarLespiaWithdrawn), "Published");
        d.Staff("Published",
            "\"Smolak will say he knew all along. He did. The difference is that now everyone else knows it too.\"",
            "\"Smolak will say he knew all along, Your Majesty. He did. Now everyone else will know it too.\"", "Close");
        d.Opt("Confront", null, "\"I'll put it to Alvarez myself. On the telephone, where he can't smile at me.\"", null,
            found + T(Discovered) + S(VarRamiro, "confront"), "Confront2");
        d.Say("Confront2", Esquibel,
            "\"I'll arrange the call, Your Majesty. And I'll keep a copy of everything in my own safe, just in case.\"", "Close");
        d.Opt("Leverage", null, "\"Lock it in the safe. Alvarez will want it to stay there, and he'll pay to keep it there.\"", null,
            found + T(Discovered) + S(VarRamiro, "leverage"), "Leverage2");
        d.Say("Leverage2", Esquibel,
            "\"Blackmail is such an ugly word, Your Majesty. We'll call it diplomacy. It usually is.\"", "Close");
        d.Opt("Bury", null, "\"Burn it. We need Lespia's ships more than we need the truth.\"", null, found + S(VarRamiro, "bury"), "Bury2");
        d.Say("Bury2", Esquibel,
            "\"Three people died in Smolak Square, Your Majesty. I hope Lespia's ships are worth them.\"", "Close");
        d.Say("Close", Narrator, "Nobody touched the box again. It sat on my desk until long after dark.", "End");
        d.Finish();
    }
}
