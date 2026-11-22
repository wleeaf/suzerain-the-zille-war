using SuzerainModdingKit;
using static ZilleWar.Talk;
using static ZilleWar.SweepEarlyKit;

namespace ZilleWar;

/// <summary>
/// Turn 5 security scenes in wartime (EarlyT5 already covers the casus belli, Hugo's "escalation", the boats and Su Omina):
/// - the late-night security council (c324): Lucita opens with the front; the Su Omina investigation is argued as a matter
///   of standing in Konstantium, not of "future Zille negotiations"; Hugo knows the army is already undoing the occupation;
///   Reinhart's motive is read in the light of the war it helped start; "peace" and "peaceful exterior" become wartime words;
/// - the night briefing on the balcony (c328): the King thinks of the north, Lucita brings the front before the murder case;
/// - the Iza welfare decision and the migrant boat tragedy learn about the war.
/// </summary>
internal static class SweepEarlyT5Security
{
    private const string Security = "Rizia/Turn 5/SnO_InternalSecurity";
    private const string Briefing = "Rizia/Turn 5/SnO_TitusLucitaBriefing";

    private const string Lucita = "Lucita Azaro";
    private const string Hugo = "Hugo Toras";
    private const string Titus = "Titus Gordion";

    public static void Register()
    {
        SecurityCouncil();
        Balcony();

        Decision("Turn05_Decision_IzaSocialWelfareFunding", o => !IsActive ? null : o.TrimEnd() +
            " Since the declaration of war, the unrest has found a new target: Wehzek shopkeepers in Little Wraclawitz report broken " +
            "windows every night, and Su Omina speaks openly of a fifth column.");

        AppendNews("Turn05_G_SecurityMigrantBoatTragedy", s =>
        {
            string policy = Variables.GetString(EarlyT5.VarRefugeePolicy) ?? string.Empty;
            string tail = policy switch
            {
                "corridor" => " Rizia has since opened a corridor for families fleeing the fighting, a decision Wraclawitz calls \"a " +
                              "recruiting office for traitors.\"",
                "screen" => " Rizia now screens every boat, and interns men of fighting age in camps near Sallabes until the war is over.",
                "closed" => " The Rizian navy has orders to turn the boats back. Its ships, the Admiralty says, have a war to fight.",
                _ => string.Empty,
            };
            return "Since war broke out over Zille, the Gulf has grown more crowded still, as Wehzek families from the Zille coast take " +
                   "to the same boats as the Bluds." + tail;
        }, s => s == StActive);
    }

    public static void OnStep()
    {
    }

    // ------------------------------------------------------------------ c324 the late-night security council

    private static void SecurityCouncil()
    {
        var t = new Talk(Security, "ZilleWar.SE.Sec.");

        // The seating ([278] with Titus, [956] without) -> [1288]: the war council's folder comes first.
        t.Line("Front", Narrator,
            "Lucita laid one more folder on the table before she sat down. It bore the seal of the war council.",
            new[] { "FrontHeld", "FrontNot" }, condition: Active, hooks: From(278, 956));
        t.Line("FrontHeld", Lucita,
            "\"The front first, Your Majesty, since everything else in this room depends on it. Our flag is still over Zille city, and " +
            "the Wehzeks have stopped trying to take it back by daylight.\"", new[] { "FrontHugo" }, condition: CityHeld);
        t.Line("FrontNot", Lucita,
            "\"The front first, Your Majesty, since everything else in this room depends on it. The Wehzek garrison is still holding " +
            "Zille city. Every disturbance at home will be read in Wraclawitz as a sign that we are tiring.\"",
            new[] { "FrontHugo" }, condition: $"not ({CityHeld})");
        t.Line("FrontHugo", Hugo, "\"Then let us give Wraclawitz nothing to read.\"", new[] { "#1288" });

        // [490] Titus: "...confirming Wehlen's suspicions, we would no doubt have an edge in securing the return of Zille."
        t.Variant("TitusEdge", 490, new[] { 1362 }, Declared, Titus,
            "\"And if we found the organization guilty ourselves, we would take the bombing out of Smolak's hands, Your Majesty. His " +
            "envoys in Konstantium talk of little else.\"", new[] { "#486" });

        // [520]/[526] "...granting us leverage in future Zille negotiations." There are no negotiations left, only the Alliance.
        t.Variant("Leverage520", 520, new[] { 519 }, Declared, Romus,
            "\"Continuing the investigation into Su Omina shows Konstantium that we police our own, whatever Smolak claims. With our " +
            "army in Zille, that matters more than ever.\"", new[] { "#1184" });
        t.Variant("Leverage526", 526, new[] { 529 }, Declared, Romus,
            "\"Further investigation of Su Omina would show the Alliance that this war is about Zille, and not about covering for bombers.\"",
            new[] { "#533" });

        // [1251]/[1420] Hugo: "Our primary concern should be Wehlen's de-occupation of Zille..."
        const string hugoActive = "\"The army is already seeing to Wehlen's de-occupation of Zille. But proving Wehzek involvement in the " +
                                  "bombing would win us the Alliance as well.\"";
        const string hugoOver = "\"Whatever happened in Zille, Smolak still tells the world we went to war to cover for bombers. Proving " +
                                "Wehzek involvement in the bombing would silence him.\"";
        t.Variant("Hugo1251", 1251, new[] { 469, 470, 1413 }, Active, Hugo, hugoActive, new[] { "#1421" });
        t.Variant("Hugo1251Over", 1251, new[] { 469, 470, 1413 }, Over, Hugo, hugoOver, new[] { "#1421" });
        t.Variant("Hugo1420", 1420, new[] { 468 }, Active, Hugo, hugoActive, new[] { "#1421" });
        t.Variant("Hugo1420Over", 1420, new[] { 468 }, Over, Hugo, hugoOver, new[] { "#1421" });

        // [1306] Lucita on Reinhart's motive: "Sowing discord between Rizia and Wehlen..."
        t.Variant("Discord", 1306, new[] { 409, 413, 1305, 1308 }, Declared, Lucita,
            "\"Sowing discord between Rizia and Wehlen. If that was the aim, it has worked better than anyone in Alca could have hoped.\"",
            new[] { "#1310", "#1328", "#1330" });

        // [867] Hugo on the protesters: "...the peace and prosperity we've built for our people."
        t.Variant("Order", 867, new[] { 866, 961 }, Active, Hugo,
            "\"It's a shame that they don't appreciate the order and prosperity we've built for our people. With our soldiers at the " +
            "front, no less.\"", new[] { "#898" });

        // [1540] "...beneath Rizia's peaceful exterior?"
        t.Variant("FlagWaving", 1540, new[] { 1523 }, Active, Romus,
            "\"Are you absolutely sure there's no resentment simmering beneath all this wartime flag-waving?\"", new[] { "#1541" });
        t.Register();
    }

    // ------------------------------------------------------------------ c328 the balcony at night

    private static void Balcony()
    {
        var t = new Talk(Briefing, "ZilleWar.SE.Brief.");

        // [664] "Amid this tranquil backdrop, I found a brief respite..." -> [666]
        t.Line("North", Narrator,
            "Somewhere beyond the hills to the north, our guns were firing on Wehzek positions around Zille. From the balcony, all I " +
            "could hear were crickets.", new[] { "#666" }, condition: Active, hooks: From(664));

        // [680] Lucita: "Good evening, Your Majesty." -> the King's replies.
        t.Line("Quiet", Lucita,
            "\"The front is quiet tonight. I checked before I came up, so you needn't ask.\"",
            new[] { "#407", "#408", "#409" }, condition: Active, hooks: From(680));
        t.Register();
    }
}
