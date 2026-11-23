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
            " Since the fighting began, the unrest has found a new target: Wehzek shopkeepers in Little Wraclawitz report broken " +
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
        // The security council is Turn 5 step 4, after the War Council (which briefs Phase II): by now Phase II has been fought,
        // held or stopped (AuditT45's front states). FrontFree cannot occur on Turn 5 (Zille is liberated only in Phase III); it is a guard.
        const string first = "\"The front first, Your Majesty, since everything else in this room depends on it. ";
        var front = new List<string>();
        string fought = $"{AuditT45.Fighting} and {AuditT45.PhaseTwoFought}";
        void Read(string name, string cond, string text)
        {
            front.Add(name);
            t.Line(name, Lucita, first + text + "\"", new[] { "FrontHugo" }, condition: $"{fought} and {cond}");
        }

        string Kind(Front.Kind k) => Front.LastKind(k);
        string Aim(string aim) => Front.Last(k => Front.LAim(k, aim));

        // Phase II took its aim.
        Read("FrontCitySecond", $"{Kind(Front.Kind.Won)} and {Aim("Zille")}",
            "Zille city is ours at the second attempt. It cost us more than the first, but our flag is over the harbor.");
        Read("FrontAuremina", $"{Kind(Front.Kind.Won)} and {Aim("Auremina")} and not {Front.LHeld("Isor")} and not {Front.LHeld("Nanta")}",
            "Auremina is ours, and the gold hills with it. The Wehzeks have fallen back on Isor and Nanta to defend the ports.");
        foreach ((string port, string ours) in new[] { ("Isor", "Nanta"), ("Nanta", "Isor") })
        {
            Read("FrontAuremina" + port, $"{Kind(Front.Kind.Won)} and {Aim("Auremina")} and not {Front.LHeld(port)} and {Front.LHeld(ours)}",
                $"Auremina is ours, and the gold hills with it. With {ours} ours too, the Wehzeks have fallen back on {port}, the last port they hold.");
        }

        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            Read("FrontPorts" + aim.Replace(" ", ""), $"{Kind(Front.Kind.Won)} and {Aim(aim)}",
                $"{aim} {(aim.Contains(" and ") ? "are" : "is")} ours, and Smolak's convoys have nowhere left to land.");
        }

        // One port of the two: a gain, not a failure.
        foreach ((string got, string other) in new[] { ("Nanta", "Isor"), ("Isor", "Nanta") })
        {
            Read("FrontPart" + got, $"{Kind(Front.Kind.Partial)} and {Front.LHeld(got)} and not {Front.LHeld(other)}",
                $"{got} is ours, and Smolak's convoys have one port fewer to land at. {other} held, and the Wehzeks are pouring everything they " +
                "have left into it.");
        }

        // The aim missed, another city taken.
        string[] aims = { "Zille", "Auremina", "Isor and Nanta", "Isor", "Nanta" };
        foreach (string aim in aims)
        {
            foreach (string c in Front.Cities.Concat(WarFlags.RizianTowns).Where(c => !aim.Contains(c)))
            {
                Read("FrontOther" + aim.Replace(" ", "") + c.Replace(" ", ""),
                    $"{Kind(Front.Kind.Other)} and {Aim(aim)} and {Front.Last(k => Front.LGainedOnly(k, c))}",
                    Array.IndexOf(Front.Cities, c) >= 0
                        ? $"We did not take {aim}, but {c} is ours. The Wehzeks were watching the wrong road."
                        : $"We did not take {aim}, but {c} is ours again. The Wehzeks will not sit in Rizian houses this winter.");
            }

            Read("FrontOther" + aim.Replace(" ", ""), $"{Kind(Front.Kind.Other)} and {Aim(aim)}",
                $"We did not take {aim}, but the army took ground and towns elsewhere. The Wehzeks were watching the wrong road.");
        }

        // A city lost: with the aim taken, with other gains, or with nothing gained.
        foreach (string c in Front.Cities.Concat(WarFlags.RizianTowns))
        {
            string id = c.Replace(" ", "");
            string lost = Front.Last(k => Front.LLostOnly(k, c));
            string where = Array.IndexOf(Front.Cities, c) >= 0 ? c : $"{c}, on our side of the border";
            Read("FrontWonLoss" + id, $"{Kind(Front.Kind.WonLoss)} and {lost}",
                $"We took what we went for, but a Wehzek counterattack took {where}. Wraclawitz will call it a draw.");
            Read("FrontMixed" + id, $"{Kind(Front.Kind.Mixed)} and {lost}",
                $"We gained ground in one place and lost {where} in another. The Wehzeks are counterattacking wherever we are thin.");
            Read("FrontLost" + id, $"{Kind(Front.Kind.Lost)} and {lost}",
                $"The Wehzeks counterattacked and took {where}. Every disturbance at home will be read in Wraclawitz as a sign that we are tiring.");
        }

        Read("FrontLossMany", Front.LastKind(Front.Kind.WonLoss, Front.Kind.Mixed, Front.Kind.Lost),
            "The Wehzeks counterattacked, and we lost ground we had taken. Every disturbance at home will be read in Wraclawitz as a sign that we are tiring.");

        // Phase II took nothing and lost nothing.
        Read("FrontCityTwice", $"{Kind(Front.Kind.Missed)} and {Aim("Zille")}",
            "The Wehzek garrison has held Zille city a second time. Every disturbance at home will be read in Wraclawitz as a sign " +
            "that we are tiring.");
        Read("FrontHills", $"{Kind(Front.Kind.Missed)} and {Aim("Auremina")}",
            "Zille city is still ours, but the hills held. The attack on Auremina has stalled, and the Wehzeks are digging in above the mines.");
        foreach (string aim in new[] { "Isor and Nanta", "Isor", "Nanta" })
        {
            Read("FrontPortsHeld" + aim.Replace(" ", ""), $"{Kind(Front.Kind.Missed)} and {Aim(aim)}",
                $"Zille city and Auremina are still ours, but {aim} held. The Wehzeks are digging in along the coast.");
        }

        Read("FrontStill", Kind(Front.Kind.Missed),
            "The front has barely moved since the last attack. Both sides are digging in.");

        t.Line("Front", Narrator,
            "Lucita laid one more folder on the table before she sat down. It bore the seal of the war council.",
            front.Concat(new[] { "FrontHold", "FrontHeld", "FrontNot", "FrontFree", "FrontStopped" }).ToArray(),
            condition: Declared, hooks: From(278, 956));
        t.Line("FrontHold", Lucita, first +
            "It has been quiet since you ordered the army to hold. Both sides are using the time to dig.\"",
            new[] { "FrontHugo" }, condition: AuditT45.FrontHolding);
        t.Line("FrontHeld", Lucita, first +
            "Our flag is still over Zille city, and the Wehzeks have not tried to take it back since the first attack.\"",
            new[] { "FrontHugo" }, condition: AuditT45.FrontBeforeCity);
        t.Line("FrontNot", Lucita, first +
            "The Wehzek garrison is still holding Zille city. Every disturbance at home will be read in Wraclawitz as a sign that we " +
            "are tiring.\"", new[] { "FrontHugo" }, condition: AuditT45.FrontBeforeNoCity);
        t.Line("FrontFree", Lucita, first +
            "There is no front anymore. The last Wehzek garrisons have surrendered, and our flag is over every town in Zille.\"",
            new[] { "FrontHugo" }, condition: $"{AuditT45.Stopped} and {WarFlags.L(WarFlags.Liberated)}");
        t.Line("FrontStopped", Lucita, first +
            "The guns have been silent since the fighting stopped. The Wehzeks are fortifying their side of the line, and so are we.\"",
            new[] { "FrontHugo" }, condition: $"{AuditT45.Stopped} and {WarFlags.LNot(WarFlags.Liberated)}");
        t.Line("FrontHugo", Hugo, "\"Then let us give Wraclawitz nothing to read.\"", new[] { "#1288" });

        // [490] Titus: "...confirming Wehlen's suspicions, we would no doubt have an edge in securing the return of Zille."
        t.Variant("TitusEdge", 490, new[] { 1362 }, Declared, Titus,
            "\"And if we found the organization guilty ourselves, we would take the bombing out of Smolak's hands, Your Majesty. His " +
            "envoys in Konstantium talk of little else.\"", new[] { "#486" });

        // [520]/[526] "...granting us leverage in future Zille negotiations." There are no negotiations left, only the Alliance.
        t.Variant("Leverage520Stopped", 520, new[] { 519 }, AuditT45.Stopped, Romus,
            "\"Continuing the investigation into Su Omina shows Konstantium that we police our own, whatever Smolak claims. With Zille " +
            "still unsettled, that matters more than ever.\"", new[] { "#1184" });
        t.Variant("Leverage520", 520, new[] { 519 }, Active, Romus,
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
            "\"Sowing discord between Rizia and Wehlen. If that was the aim, it has cost us a war in the north.\"",
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
        // Neutral about the front (Lucita may say a moment later that it is quiet tonight).
        t.Line("North", "Player_Romus_Italic",
            "Somewhere far to the north my soldiers were spending another night in their trenches. From the balcony, all I could hear " +
            "were crickets.", new[] { "#666" }, condition: Active, hooks: From(664));

        // [680] Lucita: "Good evening, Your Majesty." -> the King's replies.
        t.Line("Quiet", Lucita,
            "\"The front is quiet tonight. I checked before I came up, so you needn't ask.\"",
            new[] { "#407", "#408", "#409" }, condition: $"{Active} and {AuditT45.HeldThisTurn}", hooks: From(680));
        t.Line("QuietFought", Lucita,
            "\"Nothing new from the front since the afternoon. I checked before I came up, so you needn't ask.\"",
            new[] { "#407", "#408", "#409" }, condition: $"{Active} and not ({AuditT45.HeldThisTurn})", hooks: From(680));
        t.Register();
    }
}
