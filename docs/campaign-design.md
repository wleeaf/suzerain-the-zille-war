# The Zille War: campaign design (v2)

Sources: `zille-war-hooks.md` (scenes and IDs), `zille-war-levers.md` (variables, thresholds, costs),
`zille-storyline.md`, `pales-war-flow.md`. Direction comes from the user, 2026-11-21.

## Pillars

1. **A third road to Zille.** Diplomacy and the referendum stay as they are; war is one more option,
   grown out of the scenes where vanilla already flirts with force.
2. **The goal is the whole territory.** Every Wehlen soldier leaves the Rizian mainland. No single
   operation can do that.
3. **The player sets each operation's objective** in council, the way the Pales briefing offers plans.
4. **Isolating Zille is possible but costly.** Navy, air force, diplomacy and the AN each have a price,
   and the payoff is a garrison that is considerably smaller and weaker, never trivial.
5. **The war feels real.** City captures and losses fire events, and the news, reports, characters and
   the AN all react.

## 1. Entry points (all three are kept)

| | When | Hook (exact nodes in `zille-war-hooks.md`) | Flavour |
|---|---|---|---|
| A | Turn 8, talks fail | c409 security brief: reply to the "not within our scope" refusal with *"Not Wehlen. Zille. Our mainland."* (Lucita's own Turn 2 logic). c424 airship: a war ultimatum beside the "final warning" `[543]`. | The ultimatum is delivered to Smolak; the declaration follows in the war council. |
| B | Turn 9, instead of the referendum | c448 menu `[198]`: a fourth option, "[Declare war]". Second chance after a lost vote `[268]`–`[270]`. | "We have all of the contingency plans ready." vs Esquibel's caution. |
| C | Turn 8 AN session | c404: after Smolak defies the AN vote, at Esquibel's *"The entire Alliance of Nations appears to be stacked against us."* `[324]`, add the declaration. | A public declaration of war in front of the AN. Only reachable on some paths, so A and B remain. |

Entering sets `ZilleWar.War_Declared`, `CountryRelation_Wehlen_AtWar`, and the entry route
(`ZilleWar.Entry_Ultimatum` / `_Referendum` / `_AN`). Every route leads into the **Zille War Council**.

## 2. The Zille War Council (new conversation, cloned from the Pales briefing c357)

**Present:** the security councilor (Lucita, Titus or Taddeus, per vanilla seat logic), Esquibel
(foreign affairs) and a general. Characters can drop in with their vanilla stances: Estela "Zille, not
Pales!", Manus "my province can handle it", Angelica wants Zille for Brenas, Rusty wants the mines,
Hugo warns about strengthening Smolak.

**Flow:**
1. **Intelligence hub.** Garrison tier (from the strength meter), Wehlen's backers, the strait's mines,
   coastal batteries and Contanan radar.
2. **Isolating Zille.** These are the supply levers. Each is optional, has a cost, and lowers
   `War_Zille_Strength` (see §4).
3. **"What is the objective of the first operation?"** The player picks one; the others become later
   operations:
   - **Break the Wall:** take the border wall and the Auremina gold hills. Seizes the inland heart and
     the mines.
   - **The Golden Harbor:** drive along the Gulf coast to Zille city and its harbor, Wehlen's main port.
   - **The Northern Ports:** push up the Sudis coast to Nanta, cutting the strait crossing at its end.
4. **Command.** Personal command or delegation to the generals, as in vanilla
   (`War_Control_Skipped` analogue).
5. **EnableToken("Rizia_Event_Zille").** The operation appears on the map.

**Between operations:** a "Next Steps" council (cloned from c412) reports what was taken and lost. The
player then chooses the next objective among those remaining, continues the isolation levers, or seeks
a ceasefire.

## 3. Operations: player-chosen, with real state carried over

- Each operation is built **at runtime from the actual end state of the previous one**: tiles held,
  cities taken, losses. No predefined fronts, so the war remembers what happened.
- The objective picks the operation's target cities (its win condition), the Wehlen deployment (a
  defence weighted toward the threatened sector, like vanilla's Balanced/Northern/Southern), and the
  events.
- **Final victory:** no Wehlen-held land tile on the mainland. The islands come along.
- **Pacing:** one operation per story turn, so entry at Turn 8 allows up to 3 operations (Turns 8–10)
  and entry at Turn 9 allows up to 2. An unfinished war at the end means a ceasefire along the current
  line (see §7).

## 4. The supply war

**One hidden meter:** `ZilleWar.War_Zille_Strength`. It starts at 8, can't drop below 3, and maps to
garrison tiers **Strong / Default / Strained / Starved**. The floor tier is about 35–45% smaller and
swaps infantry and tanks for militia. Zille city and Nanta always keep regular troops.

| Lever | Prerequisite (vanilla variables) | Cost to Rizia | Effect |
|---|---|---|---|
| **Naval blockade of the Strait of Kreleck** | partial: ships + subs ≥ 6 with ≥ 3 destroyers; full: ≥ 8 with ≥ 4 destroyers, ≥ 2 subs, minesweeping | ships lost to mines and coastal guns; Budget | −1 / −2 strength; on the war map, a strait blockade |
| **Air campaign** on the ports and convoys | bombers and fighters available | Budget, planes lost, civilian casualties → Authority and AN image | −1 strength; Wehlen units start weakened |
| **Isolate Smolak's backers** | United Contana (Hegel / Caleqabiz-style deal), Sordland (Rayne), Derdia (oil buyer) | concessions: bases, resources, trade | −1 strength per backer peeled away |
| **Condemn Wehlen at the AN** | Turn 8 "other urgent matters" motion; Rusty's bombing evidence flips Arcasia and Agnolia; challenge the Threat Mitigation Protocol | relations with Smolak's friends; political capital | lowers `War_Zille_ANSupport`, so Rizia faces fewer sanctions and Wehlen more |
| **Su Omina sabotage** of the Zille–Nanta railway | Su Omina active (vanilla Turn 3–7 funding) | AN image, House reactions, possible reprisals | −1 strength; on the map, a broken Wehzek supply line |

**On the war map:** Wehzek units are supplied only if they connect through Wehlen-held land to a
Wehlen-held port (Zille, Nanta, Isor) that isn't blockaded. Cut-off units get a visible "Cut Off"
status (−1 attack, −1 defence, no healing). Reinforcements stop once every port is taken or
blockaded.

## 5. Events during the war

- **City captured or lost:** one event per city, both directions (done). Their options get real
  consequences: Authority, AN image, House reactions, and Wehzek civilians' treatment, which matters
  for the post-war referendum mood and Smolak's propaganda.
- **War-flow events** by turn, like vanilla's OP events:
  - night convoys across the strait (if not blockaded);
  - a Su Omina uprising behind the lines;
  - a United Contana airlift;
  - Derdian mercenaries switching sides;
  - Lespia offering "help";
  - refugees at Mave Nortus.
- **Home front:** a war tax, news items, the House of Delegates, and Estela.

## 6. Concurrent with the Pales war (allowed, with penalties)

- One shared army; units committed to one front aren't available on the other.
- Wehlen exploits the distraction: the garrison starts one tier stronger.
- The AN reacts harder: sanctions risk rises, applied on top of the Pales sanctions
  (handled carefully, since vanilla *sets* sanction values rather than adding them).
- Smolak's "Zille for Pales" offer is gone. You can't trade with someone you're at war with.

## 7. Outcomes

| Outcome | Condition | Result |
|---|---|---|
| **Liberation** | no Wehzek unit on the mainland | `ZilleReturn_Success = true`, a war-victory variant of the c440 return ceremony, vanilla army-interference flags (so existing protest and "interference" texts fire), Wehlen hostile, AN reaction by `War_Zille_ANSupport` |
| **Armistice** | time runs out with Rizia holding part of Zille | a divided Zille under an AN-brokered ceasefire line; not a return; tension carries into the epilogue |
| **Defeat** | an operation lost with no ground gained, or the army broken | Wehlen keeps Zille, sanctions, Authority loss, Smolak triumphant |

Epilogue lines key off these outcomes, like vanilla's Pales lines. Liberation combined with Pales
annexation still reaches vanilla's Great Unification.

## Build order

1. **Engine.** Operations built from the carried-over war state; the objective choice drives targets
   and Wehlen deployment; strength tiers drive the garrison; city events with consequences; outcome
   variables.
2. **Story.** Hooks A, B and C injected into the vanilla scenes; the War Council and Next Steps
   conversations; reports and news.
3. **Supply war.** Port and blockade supply on the map, the cut-off status and war-flow events.
4. **Outcomes.** Return ceremony variant, armistice and defeat, epilogue, Pales concurrency penalties,
   balancing.
