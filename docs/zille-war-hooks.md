# Zille War: vanilla hook map (node-level)

This document maps where the war can be injected into vanilla Rizia conversations, at the level of single articy entries.
It builds on `zille-storyline.md` (outcome logic and gates) and `pales-war-flow.md` (the Pales war variable layer).
It does not repeat their tables. Read those first for the full outcome rules.

Source: the extracted dialogue database (`scratchpad/convs/*.txt`). Notation follows the other docs:
- `c424 [543]` means entry 543 of conversation id 424.
- `-> a,b,c` is the entry's outgoing link list in the order the file stores it. When the targets are player lines, that is the menu.
- A Paskal Beniwoll entry with an empty text is a hidden logic node (IF/DO/hub).
- Every quote below was copied from the files.
- Lines marked *(proposed)* are new text written for the mod, in the speaker's voice, and are not in the game.
- Mod variable names such as `ZilleWar.Declared` are placeholders. Rename them to whatever namespace the Modding Kit gives you.

Name check for the cast list in the brief:
- "Titus" and "Gordion" are one person: **Titus Gordion**, Captain of the Golden Guard. He can also hold the security seat (`RiziaDLC.Council_Security_Titus`).
- "Azaro (Duchess)" is **Lucita Azaro**, Duchess of Montaklar and War & Security Councilor (`Council_Security_Lucita`, which is True by default).
- Two other Azaros appear in war scenes. **General Taddeus Azaro** is her father, can hold the seat (`Council_Security_Taddeus`) and leads the Pales briefing. **General Carlos Robles Azaro** is second-in-command and reads out the army's strength in the Pales briefing.
- `_variables.tsv` also has `Council_Security_Rico` and `Council_Security_Empty`. Every security-councilor switch in the Zille scenes is written "Lucita, else Taddeus, else Titus", so a Rico or empty seat falls through to Titus's lines.

---

## 0. Recommendation at a glance

| Hook | Conversation | Inject at | Fires when | Best new choice |
|---|---|---|---|---|
| (a) planning seed | `Rizia/Turn 8/SnO_SecurityBriefAndZille` c409 | after the refusal at `[2127]`/`[2132]`/`[2134]`, i.e. at `[2135]` (or a sibling option in hub `[2114]`) | Turn 8, before the airship. "Bomb the bastards" needs PowerProjection Strong or Overwhelming | "Then prepare a plan for Zille. Our mainland, our soldiers, nothing across the strait." This sets `ZilleWar.PlanPrepared` |
| (a) casus belli | `Rizia/Turn 8/FPnT_ZilleNegotiations` c424 | the menu after `[1118]` ("consider Zille's return on hold"). Primary spot is the gated sub-menu `[541]` (sibling of `[543]`). Secondary is the forced-failure menu `[798]` | Talks failed and the player refused the referendum (`[1153]` → `ZilleReferendum_Disapproved`), or points ≤ 2 forced it | "Then hear me, Wiktor: every Wehzek soldier leaves Zille, or Rizia removes them." This sets `ZilleWar.UltimatumIssued` |
| (a) declaration | new custom conversation fired right after c424 ends at `[1148]` → `[1114]` → `[1117]` → `[435]` End | | `ZilleReturn_Success == false` and `ZilleReferendum_Disapproved == true` (or `ZilleWar.UltimatumIssued`) | A Zille war council (section 4) |
| (b) instead of the referendum | `Rizia/Turn 9/FPnT_ZilleReferendum` c448 | decision menu `[198]` → `200, 199, 207`. Add a fourth option | Turn 9 referendum night, with Esquibel and the security councilor present | "Enough half-measures. Order the army across the border. We take Zille, not its ballot boxes." [Declare War] |
| (b) second chance | c448 `[268]`/`[269]`/`[270]` "We could've turned this around... I know it." (referendum lost) and c441 `[235]` menu after a failed army interference | | `ZilleReferendum_Lost` | "Then turn it around, Duchess. With the whole army this time." |
| (c) the AN refusal | `Rizia/Turn 8/Zille Territory Issue` c404, entered from `FPnT_ANSession` c401 `[30]` | Smolak's "Zille wants to remain part of Wehlen" (`[98]`) and "The Zille referendum will happen!" (`[134]`). Inject in the speech close `[203]`/`[208]` (menu `[207, 208]`) and, best, the post-vote menu `[324]` → `391, 326, 325, 327` | only if `ZilleReturn_Success or ZilleReferendum_Disapproved` (c401 `[30]`). The war hook only makes sense on the Disapproved branch after an Aye majority (`[320]`) | "Then Rizia will not wait for permission. Laurento, summon the council." [Declare War] |

**Template for the war council:** clone `Rizia/Turn 6/SnO_WarEruptionBriefing` c357. It runs:
1. hidden strength calculation
2. salutes
3. a status/objectives/front-details hub
4. "What are your initial orders?"
5. feint or direct menu `[248]`
6. navy menu
7. personal command or delegation
8. hidden resolution
9. `EnableToken`

For between-operation objective picks, use `Rizia/Turn 8/Operation 2 Next Steps` c412. See section 4.

---

## 1. Hook (a): after the Turn 8 Zille talks fail

Turn 8 runs in this order (inferred from the text):
1. **`SnO_SecurityBriefAndZille`.** The probation is "coming to an end", and the player can say "I suggest we await Wehlen's response".
2. **`RustyCall`.** On the royal train to Mave Nortus: "Maybe not in time for your chat with Smolak".
3. **`FPnT_ZilleNegotiations`.** The airship.
4. **`FPnT_ANSession`.** This reads `ZilleReferendum_Disapproved`, which only the airship sets.

So the security brief is the planning seed, the airship is the casus belli, and the AN is the international stage.

### 1.1 `Rizia/Turn 8/SnO_SecurityBriefAndZille` (c409): "Let's bomb the bastards"

**Who is present.**
- `[837]` runs `AddConversant("Lucita Azaro"); AddConversant("Titus Gordion")`.
- `[835]`/`[4]` add Titus only.
- `[1289]` branches when `Turn03_Security_LucitaLeadsInvestigation and Pales_War`.
- The security voice is Lucita, Taddeus or Titus according to `Council_Security_*`. Titus is usually in the room as well.

**Lead-in.**
```
[57]  Titus Gordion: "With all that in mind, I suggest reviewing the contingency plan we made for the return of Zille in lieu of an agreement with Wehlen."   -> 99,58,1895,98
[59]  Lucita Azaro: "The way I see it, we can either approach this diplomatically or, if necessary, take a more... *aggressive* stance."   -> 100,60
[100] Player_Romus: "Aggression is the only language Wehlen understands. Let's discuss that option first."   -> 994
[60]  Player_Romus: "Let's begin with the diplomatic approach. Thoughts?"   -> 993
```

**The hostile briefing.** Before the hook, this branch:
- reads the armed-resistance tier: `[1940]` "reaching a boiling point" / `[1937]` "gaining momentum" / `[1939]` "little indication";
- reads Su Omina (`[1947]` "Our Su Omina cell there has drawn increasing numbers of ethnic Rizians.");
- reads the Morellan arms (`[1948]`);
- reads the mercenaries (`[1963]` "But let's not forget that we still have a number of Golcondist mercenaries in the region, awaiting our command.").

Then the power-projection line:
```
[1995]/[1996] IF PowerProjection_Strong or _Overwhelming -> [1997] else -> [2114]
[1997] Lucita Azaro: "One other thing to consider is that our military has grown significantly stronger since the Zille incident."
[1998] Narrator: She smiled with pride.
[1999] Lucita Azaro: "Smolak respects a powerful army. If we play our cards right, we may be able to bluff him into submission."   -> 2114
```
The Taddeus twin is `[2043]` and the Titus twin is `[2097]`/`[2098]`.

**The hub and the refusal (the exact hook):**
```
[2114] (hub) DO: --;   -> 2111, 2115, 2140
   [2111] -> [2112] IF Turn08_Briefing_Security_DiplomaticOption == false -> [2118] "I see. What about our diplomatic options?"
                    else -> [2121] "I see. Well, both of the approaches you've just laid out have their merits."
   [2115] -> [2116] IF PowerProjection_Strong or _Overwhelming -> [2122]
                    else  [2117] -> [2139] "It sounds like strengthening the armed resistance in Zille this close to the handover date would be unwise."
   [2140] Player_Romus: "As much as I don't want to see Zille descend into violence, we do need to consider all our options at this point."   -> 2142

[2122] Player_Romus: "Who said anything about bluffing? Let's bomb the bastards."   -> 2123
[2123] -> [2124] IF Council_Security_Lucita -> [2126] Narrator: Lucita shook her head uneasily.
                 [2127] Lucita Azaro: "As much as our armed forces have improved, a direct military intervention in Wehlen is not within our current scope."   -> 2135
       -> [2129] IF Council_Security_Taddeus -> [2131]/[2132] (same line, Taddeus)   -> 2135
       -> else [2133]/[2134] (same line, Titus)   -> 2135
[2135] -> [2136] IF Pales_War -> [2138] Titus Gordion: "Besides, we have our hands full with Pales."   -> 2143
       -> [2137] else -> 2143
[2143] -> [2144] IF Turn08_Briefing_Security_DiplomaticOption == false -> 2153 (diplomacy section next)
       -> [2145] else -> [2146] -> 273 (on to the funding decision)
```
The councilor's objection is about **Wehlen**: "intervention in Wehlen". It is not about Zille, which Lucita called "our mainland" in Turn 2 (`c296 [787]`). That gap is the in-character opening.

**Recommended injection.** Turn `[2135]` into a menu, or add a player node between `[2127]`/`[2132]`/`[2134]` and `[2135]`:
- *(proposed)* `Player_Romus: "Not Wehlen. Zille. It is Rizian soil, Duchess. Draw up a plan for our own mainland: the border, the hills, the harbor. Nothing crosses the strait."`
  - It sets `ZilleWar.PlanPrepared = true`. Cost suggestion: `-1 Authority`.
  - Gate it on the same PowerProjection test as `[2116]`, or open it wider (see the alternative below).
- *(proposed)* Lucita reply, in her T2/T5 voice: "Zille is a different matter. It lies on our mainland." (`c296 [787]`) and "President Smolak may not respect a peaceful transition. But he will respect that." (`c326 [295]`). Taddeus and Titus need twins.
- If `Pales_War`, keep `[2138]` and follow it with a *(proposed)* councilor warning about two fronts. The vanilla basis is `c441 [12]`: during the Pales war the army only succeeds in Zille with `StrongArmedResistance` and `SuOminaActive`.

**Alternative.** Add a fourth link to hub `[2114]` that is not gated on power projection: "Prepare a military contingency for Zille." Weak or Average players then get the plan too, with Lucita warning that it is not yet feasible. That mirrors `c296 [169]`: "It will take a great deal of time and effort before our firepower matches his."

**Variables in play here:**
- `Situation_Diplomacy_PowerProjection_*`, `Pales_War`, `Council_Security_*`
- `Situation_Diplomacy_Zille_{Weak,Growing,Strong}ArmedResistance`, `_SuOminaActive`, `_ATAActive`
- `MITZ_Demand_ZilleResistance`, `Resources_Military_DerdianMercenaries_Zille`, `Resources_Military_RumburgianBFFMercenaries`
- `RiziaDLCIsolated.Turn08_Briefing_Security_HostileOption` (set at `[2119]`)

**The funding menu that follows** (`[2327]`; executors `[2407]`–`[2431]`, quoted in `zille-storyline.md` §2 T8) is a ready-made template for a "prepare the war" spending menu: options with `[-N Budget, -N Military Equipment]` costs, then the executor nodes. Example:
```
[2386] MENU: "We need all hands on deck. Let's activate both ATA and Su Omina in Zille." [-4 Budget, -500 Military Equipment]
```

### 1.2 `Rizia/Turn 8/FPnT_ZilleNegotiations` (c424): the airship

**Who is present.** Only Romus and Smolak, aboard Smolak's airship over Wehlen and Zille. The speaker counts are 382 Smolak lines and 223 player lines, with no councilor. Golden Guardsmen escort the player (`[810]`). Smolak's son "Little Wiky" is asleep in the next room (`[790]`).

Opening framing:
```
[809] Narrator: Our agreement's three-year probationary period had come to an end. It was finally time to discuss the return of Zille. However, my host was nowhere in sight.
[160] Player_Romus: "Then allow me to be direct and to the point. I'm here to ensure the removal of Wehlen forces from the territory of Zille. I hope this effort hasn't been in vain."
```

**Failure route A: the player refuses the referendum.** The scored path (§3.1 of `zille-storyline.md`) leads into `[505]`/`[1217]`.
```
[505]  Wiktor Smolak (R): "It should be up to the people of Zille to decide. Who are we to dictate the destiny of millions without their consent?"   -> 522,518,507,516
[1217] Wiktor Smolak (R): "Having a choice in the matter is crucial."   -> 525,527,513,514
[513]  Player_Romus: "We both know that the outcome would be predetermined long before a single vote is cast. My answer is no."   -> 716 -> 534
[514]  Player_Romus: "If you think I'd ever agree to such a proposal, you're greatly mistaken."   -> 537
[537]  Narrator: Smolak smiled in a way that hinted at concealed intentions.
[538]  Wiktor Smolak (R): "You've got fiery spirit, Your Majesty. And for that, you have my respect,  even if it blinds you to the best interests of our nations."   -> 774 -> 1153
[534]  Wiktor Smolak (R): "I presented you an opportunity that was more than fair, but if you're unwilling to compromise, then there's no point in continuing this discussion."   -> 1153
[1153] DO: RiziaDLC.ZilleReferendum_Disapproved = true; RiziaDLCSupport.Journal_Turn08_ZilleNeg_OpposeReferendum = true;   -> 539
[539]  Wiktor Smolak (R): "I will present my case for a referendum on Zille to the Alliance of Nations."   -> 1207
[1207] Wiktor Smolak (R): "Some of our compatriots still have respect for the democratic process, you see."   -> 1118
[1118] Wiktor Smolak (R): "Until then, consider Zille's return on hold."   -> 1122, 1119, 540      <== MENU (hook point 1)
   [1122] Player_Romus: "My people would vote for their return. The Rizians long to come back to their homeland."  -> [1123] "Then let the vote decide. We've been granted this fair process by Arcasian revolutionaries of old, and I intend to use it." -> [1121]
   [1119] Player_Romus: "A referendum on this land is not within your right to call. It's illegal, and I will challenge it in the Alliance of Nations."  -> [1120] "Challenge accepted. We Wehzeks have a history of resilience. I've stood against a superpower; your kingdom will be no different." -> [1121] "Zarptek Smolny!" -> 559
   [540]  (gate)
      [541] IF (PowerProjection_Overwhelming or _Strong) or DerdianMercenaries_Zille or RumburgianBFFMercenaries or Zille_SuOminaActive or Zille_ATAActive or Diplomacy_Wehlen_FundBFF
            -> 556, 1155, 543                                                                     <== MENU (hook point 2, best)
         [556]  Player_Romus: "Bravado like yours tends to falter against overwhelming determination. And believe me, Rizia is stronger and more capable than we've ever been."  -> [561] "Then I suppose we're not the friends I thought we were, which will certainly make for an uncomfortable return journey." -> 1219
         [1155] -> [1156] IF Trait_ReligiousKing -> [555] "Wehlen's delay only prolongs what's destined. I'm prepared to seek divine guidance to achieve our goals." -> [560] -> 1219
         [543]  Player_Romus: "You're only prolonging the inevitable. I'm prepared to pursue other avenues to achieve our objectives. Consider this your one and final warning."  -> [544] "Well, this has all the making for an awkward return flight." -> 1219
      [542] else -> [557] Narrator: The moment Smolak stepped out that door he left behind an unspoken understanding of the strained ties between our nations. -> 558 -> 559
[1219] Wiktor Smolak (R): "I'll take my leave so that you may reflect upon the implications of your decisions."   -> 545
[545]  Wiktor Smolak (R): "Unless you change your mind, I very much doubt I'll see you for the rest of our trip."   -> 546 -> 547
[547]/[559] Narrator: "...that I was struck by the full weight of what had transpired."   -> 1148
[1148] DO: RiziaDLCSupport.Journal_Turn08_ZilleNeg_ZilleReturnUnsuccessful = true; RiziaDLC.Diplomacy_Wehlen_IntermerkopumPotential = false;   -> 1114
[1114] -> [1116] IF ZilleReturn_Success == false -> [1117] DO: RiziaDLCSupport.News_Turn08_ZilleNegotiations_ZilleReturnFailure = true;   -> 435 End()
```

**Failure route B: forced no-return** (points ≤ 2; also reached after a refused Estela's-house request `[783]`–`[785]`). This is Smolak's most explicit "I'm keeping it":
```
[787] Wiktor Smolak (R): "I must say... the sight of Zille takes my breath away every time I see it... As I hope it does for you too."
[788] Wiktor Smolak (R): "Get a good look, Your Majesty, because, I'm sorry to say, you won't get many opportunities to do so in the future."   -> 798
[798] Narrator: Taken aback, I paused for a moment to process what I'd just heard.   -> 789, 791, 795        <== MENU (hook point 3)
   [789] Player_Romus: "You mean you're not giving it back? Like hell, you won't!"   -> [790] "Please keep your voice down, Your Majesty. Little Wiky is asleep in the other room." -> 792
   [791] Player_Romus: "I'm going to treasure this moment. After all, it's not every day I witness someone digging their own grave."   -> [799] "Come now, Your Majesty. Empty threats are beneath someone of your stature." -> 800,801,803
   [795] Player_Romus: "I beg your pardon, Mr. President? Are you saying you plan to extend the Threat Mitigation Protocol indefinitely?"   -> [805] "I'm saying that I'm not yet fully confident in your kingdom's ability to govern this province I have come to know and love." -> 792
[800] Player_Romus: "Believe me, there was nothing empty about them."   -> 792
[801] Player_Romus: "Empty threats? You underestimate me, Smolak."   -> [802] "That's cute." -> 792
[792] Wiktor Smolak (R): "I wish I could help you. I really do. But a deal's a deal. I have a reputation to uphold."
[793] Wiktor Smolak (R): "If I stop following through with my threats -- sorry, promises -- no one will take me seriously."   -> 1206
[1206] Wiktor Smolak (R): "And the last thing I'd want is for people to think I can be taken advantage of."   -> 794 -> 796 -> 797 -> 774 -> 1153 (Disapproved) -> 539 ...
```
Route B always ends in route A's `[1153]` → `[1118]` menu. So hook point 2 covers both routes.

**Recommended injection (c424).**
1. **Hook point 2, primary: a new sibling of `[543]` under `[541]`.** It keeps vanilla's gate: you can only credibly threaten if you have the army or the proxies. Proposed text:
   *(proposed)* `Player_Romus: "Then let me be plain, Wiktor. Every Wehzek soldier leaves Zille, or Rizia will remove them. Zille is our mainland, and I don't need your airship to reach it."`
   - It sets `ZilleWar.UltimatumIssued = true`, and `Diplomacy_Wehlen_NegotiatingPower -= 2` (as the AN interruption does at `c404 [396]`).
   - *(proposed)* Smolak reply, in his voice: "Empty threats are beneath someone of your stature." (`[799]`). "We Wehzeks have a history of resilience. I've stood against a superpower; your kingdom will be no different." (`[1120]`). "Wehlen's not very far away, after all." (`c326 [1173]`).
   - Then rejoin at `[1219]`.
2. **Hook point 3: a fourth link at `[798]`.** Use it for the forced-failure players who never reach `[541]`'s gate before `[1153]`. Route B still passes through `[1118]` → `[540]`, so this is optional flavour only.
3. **The war declaration itself.** Do it in a new custom conversation fired after c424 ends (`[435]` End). Gate it on `ZilleReturn_Success == false and ZilleReferendum_Disapproved == true`, and also offer it when `ZilleWar.UltimatumIssued` or `ZilleWar.PlanPrepared` is set. That conversation is the Zille war council (section 4).
   - Declaring war in Smolak's own airship would break the scene's tone: he is the host, and vanilla always has him leave the room.
   - If the mod wants the war to skip the AN, set `ZilleWar.Declared` there. Then the AN Zille item (hook c) becomes the international reaction scene instead of the vote.
4. **Do not hook the success path** (`[1082]` `ZilleReturn_Success = true`) or the Approved path (`[526]`/`[531]` `ZilleReferendum_Approved = true`). The Approved path goes to hook (b) in Turn 9.

**Variables in play at `[541]`:**
- `Situation_Diplomacy_PowerProjection_Strong/_Overwhelming`
- `Resources_Military_DerdianMercenaries_Zille`, `Resources_Military_RumburgianBFFMercenaries`
- `Situation_Diplomacy_Zille_SuOminaActive`, `_ATAActive`
- `Diplomacy_Wehlen_FundBFF`
- Upstream: `Turn08_ZilleNegotiations_ReturnPoints`, `Diplomacy_Wehlen_AgreementRefused`, `Diplomacy_Wehlen_NegotiatingPower`

**Side effect to respect.** `[1148]` already sets `Diplomacy_Wehlen_IntermerkopumPotential = false`, so Wehlen will not join the Turn 9 Intermerkopum. That is consistent with a war.

---

## 2. Hook (b): instead of the Turn 9 referendum

### 2.1 `Rizia/Turn 9/FPnT_ZilleReferendum` (c448)

**When.** The referendum runs if `ZilleReferendum_Approved` (airship) or `ZilleReferendum_NotStoppedByAN` (c404 `[320]`). The scheduling itself is outside the DB.

**Where.** The Jales mansion in Brenas, watching TV.

**Who is present.**
- `[17]` adds Laurento Esquibel. `[18]`/`[19]`/`[20]` add the security councilor (Lucita / Taddeus / Titus), chosen by `[12]`/`[15]`/`[16]`.
- Lucita may kiss his cheek if married (`[335]`). Pabel's champagne and golden saber are on the table (`[7]`).

**Lead-up lines:**
```
[35]  Laurento Esquibel: "What are your thoughts, Your Majesty?"   -> 37,36,38
[38]  Player_Romus: "I will see Zille return to Rizia. By any means possible."   -> [41] Laurento Esquibel: "There are options. I am sure."
[124] Narrator: A massive number of armed militants had taken over the streets of Zille, their faces covered in ski masks.     (StrongArmedResistance)
[87]  Narrator: They were carrying machine guns and wearing black and red patches on their arms. The police were nowhere to be found.
[122] Lucita Azaro: "We found agents trying to intimidate voters and instigate violence. The interrogations showed that they are directly linked to Wehlen."   (ForeignIntelligence Effective/Adequate)
[99]  Lucita Azaro: "Initial numbers show that we are going to lose the referendum. We must act."   (no EarlyResult_Win)
[127] Lucita Azaro: "We must act, Your Majesty. And with haste. We have all of the contingency plans ready to launch at your command."   -> 182
      ([154] Taddeus identical; [181] Titus Gordion: "We must act, Your Majesty. And with haste. We have all of the options prepared and ready to begin.")
[182] Laurento Esquibel: "I really would caution against doing anything rash at this point, Your Majesty."   -> 201
   [204] Laurento Esquibel: "We are already winning, are we not?"  [205] "Unless something changes in the next few hours..."   (EarlyResult_Win)
[183] Laurento Esquibel: "But it is your decision. If we are going to interfere, I suggest we act quickly."
[185]/[192]/[193] Narrator: The ballot boxes were 43% open. So far, around 62% / 53% / 35% of residents had voted no to joining Wehlen.
[196] Laurento Esquibel: "Your Majesty? We need a decision."
[197] Narrator: Everyone was staring at me, awaiting my decision.
[198] Narrator: Suddenly...clarity. I knew what I had to do.   -> 200, 199, 207          <== THE HOOK MENU
   [199] Player_Romus: "We will interfere with the Rizian Armed Forces. Our troops will enter Zille to protect the ballot boxes and make sure we get what is ours back."   -> [208] DO: ZilleReferendum_Interfere_Army = true; Journal_Turn09_ZilleReferendum_Interfere_Army = true;
   [207] Player_Romus: "Activate all of our agents in Zille to try to make use of the chaos and steal the votes from our opponents. Make sure that they are undercover."   -> [209] DO: ZilleReferendum_Interfere_Agents = true; ...
   [200] Player_Romus: "We will not interfere with the referendum in any way."   -> 222
[210] Lucita Azaro: "Understood, Your Majesty."  [291] "I will come back with the results in a few hours before all the votes are counted."
```

**Recommended injection.** Add a fourth link to `[198]`:
- *(proposed)* `MENU: "Enough half-measures. Order a full military operation. We liberate Zille itself, not its ballot boxes." [Declare War]`. The `[Declare War]` tag copies vanilla `c371 [142]`.
- DO: `ZilleWar.Declared = true; ZilleWar.Trigger_Referendum = true; RiziaDLC.CountryRelation_Wehlen_AtWar = true`. That last variable exists and is never touched by vanilla dialogue.
- Optionally also set `ZilleReferendum_Interfere_Army = true`, so that downstream "interference" texts fire. See section 6.
- Route to the war council conversation (or `End()` and let the mod schedule it). Do **not** route to `[213]`, because that would show the "I will come back with the results" lines.
- **Recommended gating.** Make it available always, or at least whenever `ZilleWar.PlanPrepared or ZilleWar.UltimatumIssued`. If `Pales_War == true`, show a councilor warning first; the basis is `c409 [2138]`.

**Reactions** (vanilla structure, new lines):
- The councilor's yes-line is the existing `[210]`/`[211]`/`[212]` ("Understood, Your Majesty." / "At once, my liege." / "Consider it done, Your Majesty.").
- Esquibel's objection follows `[182]`. After a war win, his vanilla victory line `[250]` "A historic win! I knew we could solve this without violence." must be suppressed or replaced.

**Second-chance hook (referendum lost).** In the no-interference branch:
```
[243] Narrator: The final referendum result was 'Yes', with only 32% of Zille residents voting for the transfer of the region back to Rizia.   -> 248
[248] Narrator: It had been done. Smolak had defeated me.   -> 262 (ZilleReferendum_Lost, TMP=false, ZLLAActive=false) -> 247
[247] Laurento Esquibel: "I am so sorry, Your Majesty. I never would have expected Smolak to be capable of such treachery."   -> 271
[323] Narrator: Lucita's fists were clenched tight.
[268] Lucita Azaro: "We could've turned this around. I know it."     ([269] Taddeus / [270] Titus: "...Your Majesty. I know it.")   -> 277 (she leaves)
[343] Laurento Esquibel: "I must go as well, Your Majesty. The sooner we begin preparing the official concession of the region to Wehlen, the better."
```
Insert a player menu after `[268]`/`[269]`/`[270]` and before `[277]`/`[281]`/`[282]`:
- *(proposed)* "Then turn it around. The ballots failed us; the army will not." This sets `ZilleWar.Declared` and `ZilleWar.Trigger_LostReferendum`.
- Note that `[262]` has already set `Situation_Diplomacy_AN_ThreatMitigationProtocol = false` and `Policy_Diplomacy_ZLLAActive = false`: the lease is legally over and Zille is now Wehzek by vote. Declaring war here is a harder casus belli, and AN reaction should be harsher.

### 2.2 `Rizia/Turn 9/FPnT_ZilleReferendumInterference` (c441): the vanilla army rule and "fell back"

The resolution is hidden at the top (full gates in `zille-storyline.md` §3.5):
```
[12] IF Pales_War or PowerProjection_Weak or _Average -> [288] -> [289] IF StrongArmedResistance and SuOminaActive -> [15] SUCCESS else [290] -> [14] FAIL
[13] else (no Pales war AND Strong/Overwhelming)      -> [15] SUCCESS
```
The army report:
```
[83] Lucita Azaro: "Our troops entered Zille immediately after I gave the order."
[84] Lucita Azaro: "The Wehzek troops fell back as soon as we entered. We stationed soldiers at each of the election centers to protect the votes and oversee the process."
  fail:    [85] "Then the unthinkable happened. The citizens themselves started to push our soldiers back. There was heavy fighting in some areas."
           [86] "As the Wehzek troops were falling back, they intentionally gave all of their equipment to the civilians of Zille."
           [87] "Our soldiers had to fall back from the election centers to regroup. Unfortunately, we were no longer able to shield the referendum from Wehlen's interference."
  success: [100] "We are currently stationed at every single voting center. Our votes are protected. We can be sure of that."
           [101] "We will get Zille no matter what. I assure you."
```
(Taddeus `[88]`–`[92]`, `[102]`–`[104]`; Titus `[93]`–`[97]`, `[105]`–`[108]`.)

Player reactions are `[139]`–`[147]`, for example `[140]` "You are all incompetent! You can't even do anything right!" and `[146]` "We will get it. I am sure of it."

Then Laurento:
- `[214]`/`[215]`: "Unfortunately, we lost the referendum by a landslide." / "Votes are 64% in favor of joining Wehlen."
- `[220]`: "It seems like people reacted very strongly against the army coming in."

Then `[231]` Smolak's state address. The final menu:
```
[235] Laurento Esquibel: "This will impact our public image heavily, Your Majesty."   -> 236, 238, 279
   [236] "I trust that you will handle it accordingly, Laurento."
   [238] "He will know my vengeance. I swear it."
   [279] -> [237] "Who cares about Smolak? Zille is ours again." (Won) / [282] "Who cares about Smolak? Zille is lost."
[278] (lost)  DO: Reports_Turn09_Nanta_DiscardedBallotsDiscovered; CountryRelation_Wehlen_Hostile = true; Situation_Diplomacy_AN_ThreatMitigationProtocol = false
[287] (won)   DO: ...; ZilleReturn_Success = true; ...; CountryRelation_Wehlen_Hostile = true
```

**Hooks in c441:**
- After an army **fail**, `[238]` "He will know my vengeance. I swear it." is the ready-made line. Add a sibling or follow-up *(proposed)*: "Recall the generals. If the Wehzeks want to arm civilians and hide behind ballots, we will finish this properly." It sets `ZilleWar.Declared`, `ZilleWar.Trigger_FailedInterference`.
- **War canon from c441 for the design.**
  - Vanilla's own model of a Rizian incursion is that **Wehzek regulars withdraw and hand weapons to Zillean civilians**. A real war should have them fight. The Zille war can treat c441 `[86]` as Wehlen's doctrine: Op I meets armed militia, not just the garrison.
  - The vanilla two-front rule is the `[12]` gate: during `Pales_War` the army only succeeds with `StrongArmedResistance and SuOminaActive`. Use the same inputs as difficulty modifiers.

### 2.3 Foreshadowing in Turn 9 before the referendum

**`Rizia/Turn 9/FPnT_CallWithSmolak` (c434).** It happens before the referendum: `[184]` "Do you own a crystal ball, Your Majesty? Nothing's decided until the people cast their vote." On rejection:
- `[171]` Player_Romus: "Listen here, Smolak. Your puny little troops will only interfere with the unstoppable juggernaut that is the Rizian Armed Forces. Consider your offer declined."
- `[196]` "I'll see you at the Zille return ceremony." → `[200]` Smolak: "I wouldn't count on it."
- `[124]`/`[131]`/`[132]` "Screw Smolak... The Rizian flag *will* fly over Pales and Zille, once more."

A reject sets `ZilleReferendum_SmolakOffer_Rejected`. Good for a `ZilleWar.SmolakSpurned` modifier (harder war, or Smolak reinforces).

**`Rizia/Turn 9/FPnT_GRACERumburgMeeting` (c433).** `[541]` Beatrice: "Which is why it's so important that you keep Wiktor Smolak from seizing Zille." It is gated on `NotStoppedByAN or Approved`, and offers:
- `[546]` (`Diplomacy_Wehlen_FundBFF or RumburgianBFFMercenaries`): "I agree. Thanks to Rumburg, we have the power to take the region back by force if the referendum goes south." → `[552]` "I am counting on that."
- `[549]`: "Leave Zille to us. We will get it back, no matter what it takes."

This is the best place to set a `ZilleWar.RumburgBacking` flag: Rumburg is the natural co-belligerent or arms supplier.

---

## 3. Hook (c): Smolak at the Alliance of Nations

### 3.1 Where it is and when it fires

**Turn 8 `Rizia/Turn 8/FPnT_ANSession` (c401), Konstantium AN HQ.** The chain is:
1. `[3]` → `[4]` `c402:1` Sanctions (if a Lespia/Valgsland accusation) or `[5]` `c403:1` Heljiland Dispute.
2. Then `c403 [2] -> c401:29` (the Heljiland item's exit).
3. Then:
```
[30] IF RiziaDLC.ZilleReturn_Success == true or RiziaDLC.ZilleReferendum_Disapproved == true -> c404:1
[31] else -> [6] End()
```
So the Zille item **does not run** if the player agreed to the referendum on the airship (`Approved`). When the talks succeeded, Smolak announces the transfer (`c404 [6]`, `[52]`–`[54]`) and revokes the protocol (`[342]`). **The refusal speech only plays when the player refused the referendum (`ZilleReferendum_Disapproved`).**

**Who is present.**
- Laurento Esquibel, seated next to the player.
- President Ephraim Nines, presiding.
- The voters at `[276]`: Jorga Azmal, Patricio Alvarez, Dwight Walker, Maartin Van Hoorten, Anton Rayne, Axel Reinhart, Alma Saltana, Beatrice Livingston, Emmerich Hegel, Leon Malenyev and Smolak. Axel is skipped via `[386]` if `War_Pales_Won`.

**The refusal speech (Disapproved path), in order:**
```
[49]  Narrator: Wiktor Smolak, President of Wehlen, got up from his seat to speak.
[51]  Narrator: He held his arms up in the air and shouted.
[50]  Wiktor Smolak (R): "Alliance of Nations! Hear me!"
[89]  Laurento Esquibel: "He can't help making a dramatic entrance, can he?"
[7]   Wiktor Smolak (R): "I come to you today with dire news from our southeastern border."
[100] Wiktor Smolak (R): "The Kingdom of Rizia's continued demand for the transfer of Zille back to Rizian territory."
[94]  Wiktor Smolak (R): "Under Wehlen, the Zilleans have enjoyed democratic freedoms that they have never experienced before."
[95]  Wiktor Smolak (R): "To lose these freedoms by joining a monarchy... nobody wants that! Would you? I wouldn't!"
[96]  Wiktor Smolak (R): "I have heard representatives from Zille --personally, I might add. They were clear in their request."
[97]  Wiktor Smolak (R): "They asked good old Wiktor to help them out. And I promised that I would."
[98]  Wiktor Smolak (R): "Zille wants to remain part of Wehlen."                                    <== the refusal
[109] Wiktor Smolak (R): "The will of the people is paramount in a democracy, King Romus, although you might not know that."
[110] Wiktor Smolak (R): "A democratic referendum will take place in Zille so that the people themselves can decide with whom they will align."   -> 101, 102   <== MENU 1
   [101] MENU: Interrupt him.  -> 105,104,103,403
       [103] "This is illegal! You are going against international law!"  -> [115] "Rich, coming from a man who thinks his rule is law."
       [104] "Zille rightfully belongs to Rizia as per the agreement between our nations!"  -> [112] "You dare disregard the will of people? I can't say I am surprised."
       [105] "Wehlen talking about democracy? You must be kidding me!"  -> [111] "Wehlen is a full democracy, unlike your kingdom. I myself have been democratically elected four times!"
       [403] -> [106] "You will not utter my name so easily, scoundrel!" (or [406] the Lespia retort, with Publicized_Rusty)
       -> [107] Narrator: President Nines hit the gavel a number of times. -> [396] DO: Diplomacy_Wehlen_NegotiatingPower -= 2 -> [108] "The monarch can't even keep himself composed in a democratic arena!" -> [114] "Alright. Alright. I am calm." -> 26
   [102] MENU: Let him continue. -> 26
[26]  -> [27] IF ZilleReferendum_Disapproved -> [353] -> [354] IF AgreementRefused == false -> [356] else -> [116]
[356] Wiktor Smolak (R): "I myself personally presented him with the idea of the referendum when it became clear that he had not taken our amended agreement seriously."
[116] Wiktor Smolak (R): "I myself personally presented him with the idea of the referendum when it became clear that he was not interested in building trust between Rizia and Wehlen."
[120] Wiktor Smolak (R): "He almost spat in my face. What do you expect from a monarch?"
[119] Wiktor Smolak (R): "Of course, Rizia has not sat idly by in the years since the Friendship Day Attack."
[99]  Wiktor Smolak (R): "King Romus Toras is knowingly, intentionally, sowing dissent among the populace of Zille!"
   [117]/[118] (Massive/Growing movement) "With underhanded tactics, he has attempted to garner a pro-Rizia movement in the region..."
   [121] (StrongArmedResistance) "My government has found direct links to a massive underground armed resistance. Enough men to destabilize the entire region!"
   [124] (GrowingArmedResistance) "My government has found direct links to an increasingly popular armed resistance."
   [122] "We found Rizian weapons in the hands of these terrorists! Where are these weapons coming from? Answer me!"   [123] "From Rizia itself, of course!"
   [125] (Diplomacy_Wehlen_FundBFF) "Not just in Zille! We also found Rizian weapons in the hands of BFF terrorists in Wehlen!"  [126] "How deep does this go?"
[127] Wiktor Smolak (R): "There are big games being played with Wehlen. Everyone is against us!"
   [128]/[129] (Lespia publicized) "...who's to say they were not working in tandem?"
   [357]/[362] + [361] (Lespia not publicized) "...Why? Because the attack was masterminded by Rizia itself!"
   [338]->[131]/[130]/[132] (MITZ_Demand_ZilleDiplomacy or Morella Allied) Alma Saltana: "Are you going to believe the words of a dictator? This madman? Really?" ... [133] Smolak: "I will not be silenced!"
[134] Wiktor Smolak (R): "The Zille referendum will happen!"
[135] Wiktor Smolak (R): "As a leading force for justice in the world, it is the AN's obligation to rescue these innocents from the clutches of their would-be King and deliver them to freedom!"
[136] Wiktor Smolak (R): "That is all I will say!"
[137] Narrator: He left the podium. President Nines motioned forward.
[138] Narrator: I was given several minutes to defend Rizia's position. I made my way to the podium.   -> 139, 47, 140
```

**Player speech** (topic hub `[141]` → `[145]` agreement, `[149]` Smolak, `[153]` people of Zille, `[192]` the referendum):
```
[195]/[196]/[197] "This referendum is a completely bogus attempt..." / "...instigated by President Smolak himself..." / "...completely illegal, unjust..."
[198] "It should and will never come to pass."   [199] "...the results should not be recognized..."   [200] "...no way their government will respect the actual will of the people."
   [206] "Rizia will not be silent against the threat of Wehzek territorial ambitions."   -> 203,204,205
   [201] "I want you to put yourselves in Rizia's shoes. What would do if someone was trying to steal your land?"   -> 203,204,205
[203] Player_Romus: "I am warning President Smolak. If he makes a single move to prevent the return of Zille, we will respond."   -> 207, 208
[204] "No matter what happens, I know that Rizia will come on the top. I have complete faith."   -> 207, 208
[205] "If we abide by the rule of law, what must be done is clear. Zille must be returned to Rizia as per the agreement."   -> 207, 208
   [207] Player_Romus: "I ask for your votes to send Alliance of Nations peacekeepers to Zille to stop this illegal referendum."   -> 209      <== MENU 2 (closing line)
   [208] Player_Romus: "With or without the help of Alliance of Nations, we will defend our territory. You can be sure of that."     -> 209
[209] Narrator: The audience applauded as I stepped down from the podium.
```
None of these lines set a variable. `[397]` (NP −2) fires only on the insult path `[178]`/`[179]`/`[180]`.

**The vote and the post-vote menu:**
```
[217] Narrator: After much deliberation, President Nines opened the floor. Each leader now had to vote whether to allow the referendum or not.
[317] Narrator: The votes were counted.  -> [315] IF ANZilleReferendum_Vote > 0 -> [318] / else -> [319]
[318] Ephraim Nines (R): "The majority has spoken. The Alliance of Nations will allow the referendum on Zille to proceed."
[320] DO: ZilleReferendum_NotStoppedByAN = true; Journal_Turn08_ANSession_ZilleReferendumNotStopped; Reports_Turn08_Zille_ReferendumPreparationsStart; News_Turn08_G_ZilleAN_Referendum
[321] Laurento Esquibel: "God be damned!"
[323] Laurento Esquibel: "Oh. Pardon my language, Your Majesty."
[324] Laurento Esquibel: "The entire Alliance of Nations appears to be stacked against us."   -> 391, 326, 325, 327      <== MENU 3 (best hook)
   [391] -> [394] (ReligiousKing) "That doesn't excuse you taking the Lord's name in vain."
   [326] Player_Romus: "They will all pay for this. All of them."
   [325] Player_Romus: "It does look like it."
   [327] Player_Romus: "We will get Zille back, Laurento. No matter what Wehlen or anyone else wants."
   -> [332] Laurento Esquibel: "Yes, Your Majesty." -> [88] Narrator: An aide leaned over to my ear to let us know that it was lunchtime. -> [340] -> [2] -> c401:6 End()
[319] Ephraim Nines (R): "The majority has spoken. The Alliance of Nations will not allow the Zille referendum to proceed."
[423] Ephraim Nines (R): "Peacekeeping troops shall be sent to ensure the smooth transition of Zille to Rizia."   -> [430] -> [48] DO: ZilleReferendum_StoppedByAN = true; ...; ZilleReturn_Success = true;
```

### 3.2 Recommended injection (c404)

1. **Menu 3 at `[324]` (primary).** Add a fourth link:
   *(proposed)* `MENU: "Then the Alliance can watch. Laurento, have the council convene the moment we land." [Declare War]`
   - It sets `ZilleWar.Declared = true; ZilleWar.Trigger_AN = true` and `RiziaDLC.CountryRelation_Wehlen_AtWar = true`.
   - Optionally set `Situation_Diplomacy_GlobalImage_Bad = true` or an AN-sanctions counter, since the war defies an AN decision.
   - *(proposed)* Laurento reply, with vanilla cadence: "Your Majesty... I really would caution against doing anything rash." (`c448 [182]`), then "Yes, Your Majesty." (`[332]`).
   - Then link to `[88]` (lunch) → `[340]` exit, so the session closes normally.
   - The Turn 9 referendum must then be suppressed for this route (scheduler condition `not ZilleWar.Declared`), or replaced by Op I.
2. **Menu 2 at `[203]`–`[208]` (public ultimatum, flavour plus modifier).** Add a third closing line beside `[207]`/`[208]`:
   *(proposed)* "If this Alliance lets Wehlen keep my mother's homeland by vote of its own occupiers, Rizia will take it back itself. Consider this the only warning the assembly will hear."
   - It sets `ZilleWar.PublicUltimatum = true`.
   - Suggested side effect: shift the AN vote, e.g. `ANZilleReferendum_Vote += 1`, before `[217]`. Threats make the AN more willing to allow the referendum, which makes the war *more* likely and is narratively coherent.
3. **Menu 1 at `[101]`/`[110]` (interrupt).** Optional extra interrupt line for a hawkish king, such as *(proposed)* "Then come and keep it, Wiktor." Run it through the existing `[396]` NP −2 node.
4. **Never offered here.** The StoppedByAN branch (`[319]`/`[423]`/`[48]`) returns Zille peacefully with AN peacekeepers. No war hook is needed there. Attacking afterwards would mean fighting AN peacekeepers (`c440 [302]`: "AN peacekeeping forces were dispersed throughout the crowd").

### 3.3 The Turn 4 precursor: `Rizia/Turn 4/FPnT_ANSouthMerkopa` (c316)

This is the first AN scene where Smolak announces he is **not returning Zille on schedule**. It is too early for a war, but it is the right place to seed a flag.
```
[131] Wiktor Smolak (R): "I do not blame the great Kingdom of Rizia for its decision. But neither can I ignore that for the past 20 years, Rumburg has maintained a presence to both our north and south."
[132] Wiktor Smolak (R): "This is why I have decided to invoke the AN Threat Mitigation Protocol for Treaty Obligations to delay the return of Zille until that risk is neutralized."
[135] Narrator: Gasps and murmurs rippled through the assembly. I felt many pairs of eyes on me.   -> 133, 134, 195, 419
   [133] Player_Romus: "Bullshit!"  -> [136] Laurento Esquibel: "You'll have to wait until your own speech to raise your objections." -> [149] "But yes, Your Majesty. It is indeed bullshit."
   [134] MENU: Pound your fist on the table in front of you.
[141] Wiktor Smolak (R): "I am a man who respects the rule of law. Yet I cannot put my country-people at risk by abandoning the territory while these questions still hang in the air."
[146] Wiktor Smolak (R): "What is to stop such violence from reoccurring? What will happen to the Wehzeks who have made Zille their home once Rizia -- and by proxy Rumburg -- regains control of the region?"
[155] Wiktor Smolak (R): "It is not my intention to keep custody of the Zille region forever. The sooner that safety and security are restored, the sooner we may begin preparing for an orderly transition."
```
In the player's speech, `[379]` and `[383]` are the aggressive lines. `[428]` sets `Diplomacy_Wehlen_ANSpeech_Aggression`, which the mod can read later as "the king warned the world in 1952":
- `[379]` "I will give him another chance to honor the terms of our agreement. If that fails to happen, I will have to take matters into my own hands."
- `[383]` "If President Smolak does not honor the terms of our agreement, we will be forced to take matters into our own hands."

---

## 4. War-council template

### 4.1 `Rizia/Turn 6/SnO_WarEruptionBriefing` (c357): node flow

**Cast.**
- Lucita Azaro, Taddeus Azaro, Carlos Robles Azaro.
- The Council Chambers are "packed with military staff. Every single top-ranking general and admiral in Rizia" (`[99]`).
- `[577]` adds Taddeus; `[487]` adds Lucita. Carlos speaks without an AddConversant in the export. `[576]` removes all three at the end.

**Flow:**
1. **Hidden setup.** `[1]` → `[578]` SetRichPresence → `[496]` news `News_Turn06_{V,RH,G}_PalesWarStart` → `[538]` → `[492]`/`[489]` (Terador airbase report, Vina hostage news) → `[498]` `War_Happened = true` → `[3]`.
   Then the hidden enemy-strength calculation `[3]`–`[96]`:
   - casus-belli branch comments `[6]`/`[10]`/`[11]`/`[15]`/`[19]`/`[20]`;
   - `War_Pales_ANSupport` (`[25]`–`[36]`, `[83]`, `[84]`), `War_Pales_LespianSupport`, `War_Pales_Strength` (`[46]`, `[47]`, `[55]`, `[56]`);
   - forts (`[78]`/`[79]`), flagship (`[91]`), naval overrides (`[95]`/`[96]`).
   - Then `[488]` music → `[105]` narration.
2. **Arrival and greetings.**
   - `[105]` "I arrived at the Council Chambers to discuss our war plans with the highest-ranking members of the Rizian military." → `[509]` (`Prologue_StayedOnCouncil` variant) → `[99]`/`[512]` → `[102]` salute.
   - `[577]` Taddeus → `[106]` "They were standing over a massive table, arranging golden and silver tokens atop a map of Pales."
   - `[107]` Lucita → `[101]` salute menu `109, 108, 103, 100, 104`.
   - `[539]` → `[124]` Lucita introduces her father → `[121]`/`[122]` Taddeus salute → `[524]`–`[534]` (health / "You answer to me now").
   - `[541]`/`[542]` Carlos → `[548]`–`[552]`.
   - `[127]` "Shall we start, Your Majesty?"
3. **Opening status.**
   - `[471]` → `[481]`/`[480]`, flagship news (`[484]` if RRN Valero is out of service).
   - `[131]` Lucita: "Your Majesty, our forces are already mobilized. Our navy and air forces are prepared and awaiting your orders. And our ground forces will be ready to cross the border within 48 hours."
   - `[261]` Lucita: "...our strategy will have to rely on an attack from either the north or the southwest."
4. **Hub 1, the strategic discussion** (`[192]` → `[147]`/`[150]`/`[153]`, flags `RiziaDLCIsolated.Turn06_WarBriefing_Hub1_Option1..3`):
   - intel (`[132]` → `[135]`/`[145]`/`[160]`/`[161]` enemy tanks "of Lespian origin");
   - plan for the initial strike (`[133]`/`[134]` → `[537]` → `[262]` Taddeus: "Our first objective is to take control of the cities of Ziqes and Arces and trap their forces at the chokepoint at Solles." → `[168]`/`[169]` two-pronged plan → `[171]`/`[172]` Carlos);
   - axis debate (`[196]` north, `[197]` "What do the generals suggest?", `[198]` west, `[218]` naval invasion → `[226]`–`[229]` Lucita: "it is not possible to initiate a massive naval invasion" / marines "a quite limited assault");
   - `[191]` "Okay. Let's talk about our deployment and general strategy."
5. **Briefing offer.** `[239]`/`[240]` Taddeus: "As the King, you have total control... I've prepared a briefing... Would you like to hear it?" → `[242]` yes / `[241]`/`[254]` no / `[255]` "Do not interrupt me ever again." (Taddeus humiliated at `[256]`).
6. **Hub 2** (`[243]` → `[258]` → `286, 289, 292, 558, 295, 265`, flags `Turn06_WarBriefing_Hub2_Option1..3`):
   - `[259]` "What's the status of our military?" → `[375]` Carlos and Lucita read the unit thresholds (table in `pales-war-flow.md` §2.1);
   - `[260]` "What are our strategic objectives?" → `[276]`–`[283]` (Taddeus and Carlos on Ziqes, Navar, Arces). `[283]`: "By cutting that city from their network, we can crush their border defenses and create a new frontline from Navar to the sea.";
   - `[263]` "Give me the details of each front and what to expect." → `[452]`–`[462]` (Carlos on the River Alca forts, Navar anti-air, Ziqes counter-offensive, mountaineers);
   - `[265]` "I think I have enough information." → `[299]` Taddeus: "What are your initial orders, Your Majesty?"
7. **Orders menu 1** (`[248]` → `267, 268, 266`):
   - `[267]` "Before the invasion, we will feign an attack from the north..." → `[209]` `War_AttackPlan_FeignNorth`
   - `[268]` "...feign an attack from the south..." → `[210]` `War_AttackPlan_FeignSouth`
   - `[266]` "We will lead a direct assault without any precursors..." → no flag
   - → `[269]` "The generals in the room gave me a military salute while shouting 'Yes, sir.'"
8. **Orders menu 2, the navy** (`[269]` → `368, 300, 301, 302, 303`):
   - `[371]` (marines ≥ 0) navy and marines on standby
   - `[300]` navy on standby
   - `[301]` blockade the Gulf of Ales → `[206]` `War_AttackPlan_BlockadeGulfofAles`
   - `[302]` blockade the Gulf of Pales → `[207]` `War_AttackPlan_BlockadePales`
   - `[303]` blockade the Gulf of Remaz → `[208]` `War_AttackPlan_BlockadeRemaz`
   - → `[304]` "Understood, Your Majesty."
9. **Command** (`[271]` Taddeus offers a detailed plan → `272, 270, 273`):
   - `[272]` "I'll plan the assault and the deployment of our forces personally." → `[501]` `Situation_Military_Monarch_OverseesWar`
   - `[270]` "I will look into it in detail, General." → `[274]` → `[501]`
   - `[273]` `MENU: "...I will grant full authority for the invasion to the General Staff and to you, General Azaro." [OPT OUT OF WAR GAMEPLAY]` → `[264]` `War_Control_Skipped = true; Situation_Military_HighCommand_OverseesWar = true` → `[499]` snapshot every `Army_Unit_<X>_Total` into `War_Control_Skipped_<X>` → `[500]` placeholder army.
10. **Close.**
    - `[467]` salute → `[466]` Lucita: "We will relay your orders immediately, Your Majesty." → `[468]` "I will return to you as soon as we have an update." → `[469]` "And there it was. The Invasion of Pales was about to begin."
    - `[576]` remove the conversants → `[508]` `PlayCustomMapMusic("Rizia_Map_Music_Tense")`.
11. **Hidden resolution.**
    - `[336]` deployment (`RiziaDLCSetup.War_Operation1_Deployment_Pales_Northern/_Southern/_Balanced`) → `[329]` strength override → `[311]` blockade resolution (ship + sub thresholds 6/5/4, losses) → `[355]` `War_TileOverride_Pales_ExtraAirDefence/_ExtraNavalDefence`.
    - → **`[486]` `EnableToken("Rizia_Event_InvasionOfPales")`** → `[470]` `End()`.

**Variables c357 sets.** The full list is in `pales-war-flow.md` §2.1:
- `War_Happened`
- `War_Pales_ANSupport`, `War_Pales_LespianSupport`, `War_Pales_Strength`
- `RiziaDLCSetup.War_TileOverride_*`, `RiziaDLCSetup.War_Operation1_Deployment*`, `..._DeploymentOverride_*`
- `War_AttackPlan_FeignNorth/_FeignSouth/_BlockadeGulfofAles/_BlockadePales/_BlockadeRemaz` and their `_Success`
- `Army_Unit_Ship_Total/_Dead` deltas, `Army_Unit_Flagship_Total`
- `Situation_Military_LostPrideOfNavy`, `Situation_Military_Monarch_OverseesWar` / `_HighCommand_OverseesWar`
- `War_Control_Skipped` with the `War_Control_Skipped_<Unit>` snapshot
- `RiziaDLCIsolated.Turn06_WarBriefing_Hub{1,2}_Option{1,2,3}`

### 4.2 Mapping to a "Zille War Council"

Keep the cast (Lucita or Taddeus, Taddeus, Carlos; add Titus if the Golden Guard is used). Keep the room, the table map (Zille instead of Pales) and the salute ritual. Reuse the c357 skeleton node-for-node.

| c357 node | Zille council equivalent | Suggested mod variable |
|---|---|---|
| hidden strength `[3]`–`[96]` | Wehlen garrison strength from: `Pales_War`; `Diplomacy_Wehlen_AgreementRefused`; `ZilleReferendum_SmolakOffer_Rejected`; `Zille_Publicized_LespianInvolvement` (AN sympathy); resistance tiers; Derdian or BFF mercenaries; `MITZ_Demand_ZilleResistance` (Morellan arms); and `Policy_RD_Order_BuildMaveNortusIntelHub_Completed` (intel on garrison positions) | `War_Zille_Strength`, `War_Zille_ANSupport`, `War_Zille_ContanaSupport` |
| `[131]` status | "Our ground forces will be ready to cross at Mave Nortus within 48 hours." | – |
| `[261]` constraint | Lucita's T2 constraint verbatim: "A naval invasion across the gulf would be operationally unimaginable" (`c296 [783]`); "Zille is a different matter. It lies on our mainland." (`[787]`) | – |
| `[262]`/`[276]`–`[283]` objectives | **Op I "Iron Gate"**: breach the border wall at Mave Nortus and take the Auremina hills and the Dapalle river line | `War_Zille_Op1_Objective_*` |
| orders menu 1 `[248]` (feint N / feint S / direct) | **Objective menu** (one of): (a) "Strike the border wall and the Auremina hills" (cautious; secures gold mines); (b) "Drive straight for Zille harbor" (decapitation; high cost); (c) "Push up the coast for Nanta port" (cuts the main rail and road, isolating the city) | `War_Zille_AttackPlan_BorderHills` / `_Harbor` / `_Nanta` |
| orders menu 2 `[269]` (navy) | (a) "Keep the navy on standby"; (b) "Blockade the Strait of Kreleck" (cuts reinforcement, risks the mined approaches and Contanan coastal guns); (c) "Blockade Zille harbor only"; (d) marines variant if `Army_Unit_Marine_Total` ≥ 1, limited landing at Isor | `War_Zille_AttackPlan_BlockadeKreleck` / `_BlockadeZilleHarbor` / `_MarinesIsor`, plus `_Success` resolved by `Army_Unit_Ship_Total + Army_Unit_Submarine_Total` thresholds (copy 6/5/4) |
| command `[271]`–`[273]` | the same three lines; the delegate option sets its own skipped flag and snapshot | `War_Zille_Control_Skipped`, `War_Zille_Control_Skipped_<Unit>` |
| `[486]` EnableToken | a new event token for the Zille map | `EnableToken("Rizia_Event_InvasionOfZille")` *(new)* |

For **Operation II/III objective picks between turns**, mirror `Rizia/Turn 8/Operation 2 Next Steps` (c412):
- After a win: `[6]` Carlos: "We are in a good position to encircle the peninsula."; `[34]` "After the capture of both Arces and Ziqes, we pushed further past Arces, close to Mandana."
- After a loss: `[22]` "We are back where we started." → player `[40]` "Simply holding the line is unacceptable. We will push with whatever we have. Even if that means more casualties." vs `[41]` "Then be the wall that protects the mainland. Do not let them pass under any condition."
  - `[51]` Lucita: "Your Majesty... if we push harder and fail, we'll find ourselves in an even more precarious situation."
  - `[52]` Taddeus: "...We can't overextend."
  - `[53]` "Cowards, all of you! Attack or I will have you lashed." / `[59]` "I order you to attack."
  - → `[61]` `War_Operation2Loss_Charge = true`.
- Intel section: `[62]` Lucita: "We won't win this war without information." → enemy navy position by the blockade flags (`[79]`–`[82]`), and foreign help (`[114]` "Lespia is another matter.").
  - For Zille, replace Lespia with **United Contana**: "It is the CSP that worries me."
  - The Q&A menu `[140]`–`[143]` ("Was this all a mistake?", "Can we win this war?", ...) is reusable verbatim.
- Debrief: `Rizia/Turn 8/SnO_War_Pales_PostOperation1` (c410) and `Operation 1 Results` (c411). Their auto-resolve table for `War_Control_Skipped` is in `pales-war-flow.md` §2.3.

**Two more council templates:**
- `Rizia/Turn 6/A_CouncilMeeting` (c361) is the casus-belli council where the full council (with Vina) argues for and against war. Vina: `[668]` "And I don't agree with how you're fuelling the fires of war." `[672]` "How does our situation call for an invasion?"
- The `c409 [2327]` funding menu is the template for a pre-war "arm the resistance / pay the mercenaries" menu.

---

## 5. Zille's military situation, in vanilla text

**No troop numbers, garrison size or Wehzek order of battle exist anywhere in the DB.** Everything below is the full set of concrete facts. The design can extrapolate from it but must not contradict it.

**Border and garrison:**
- **The border wall.** `c298 [4]` Narrator: "Our motorcade slowed as it approached the Zille border. A concrete barrier rose up in front of us, its top strung with barbed wire." The crossing is on the Mave Nortus side; the royal train runs there (`c416 [3]` "...as we chugged toward Mave Nortus."). Mave Nortus also hosts the intelligence hub (`Enable_Policy_RD_Order_MaveNortusIntelHub`, unlocked in `c304 [443]`/`[492]`).
- **Tanks and troops in Zille.**
  - `c298 [67]` "...a colorful banner reading '24 Years Of Wehzek-Rizian Friendship' hung above a trio of gleaming tanks."
  - `[71]` "A dozen uniformed soldiers hurriedly got into formation around us and the limo."
  - `[86]` "Looking up into the grandstands, I noticed armed guards standing on every level."
  - `[99]` Player: "Look at the sloppy way they're holding their guns. They have nothing on our recruits."
- **Who pays for them.**
  - `c298 [139]` Estela Toras: "We can thank his Eastern benefactors, I presume." This is the "Eastern benefactors" line; it answers `[136]` "Those really are some impressive tanks."
  - `c296 [78]` Lucita: "Since Smolak took over, he's used the occasion to trot out all the shiny new tanks Wehlen's oil money and United Contanan funding has bought him."
  - `c296 [172]` Lucita: "The funding Wehlen has received from United Contana may also have Smolak believing he can act with impunity."
  - `c316 [357]` Player: "...his own country has received from United Contana."
  - `c323 [306]` Alma Saltana: "United Contana is still backing Wehlen through all this."
- **Relative strength.** `c296 [169]` Lucita: "Smolak is not as weak as he seems. It will take a great deal of time and effort before our firepower matches his. He may act bold."

**Coast, navy and air** (why the war stays on the mainland), from `c296`:
- `[783]` Lucita: "President Smolak has spent years fortifying his coastline. The sea approaches are heavily mined, and reinforced with layered naval batteries. United Contana's aid allowed Wehlen to significantly expand those defenses. A naval invasion across the gulf would be operationally unimaginable."
- `[786]` "Additionally, our navy lacks the necessary amphibious capacity for a sustained landing. We also do not possess enough advanced vessels capable of reliably detecting and clearing minefields in the Gulf."
- `[791]` "The minefields and overlapping coastal guns would break any rapid assault before it reaches land."
- `[792]` "Contanan radar and anti-air systems would blunt our strikes long before their batteries are neutralized."
- `[787]` "Zille is a different matter. It lies on our mainland. There, we can exert intelligence and limited military pressure with far greater feasibility."
- `[793]` "It is the only front where we can apply controlled military and intelligence pressure from our mainland."
- `[784]` Esquibel: "For all his rhetoric, Smolak seeks security and legitimacy. An outright attack would push him firmly into United Contana's embrace, perhaps beyond retrieval."
- `[785]` Hugo: "If we move against Wehlen openly, we risk strengthening the very man we could wish to keep in check."
- **Wehzek amphibious capability.** `c354 [58]` Smolak: "Operation Moonlight Sonata was such a great success. Wehzek boats arrived on the shores of Pales at midnight... The Palesians had no idea what was coming." This supports Wehlen reinforcing Zille by sea across the Strait of Kreleck, which is the reason for the blockade option.
- **Wehzek army reputation.**
  - `c434 [106]` "The unmatched glory that is the army of the Republic of Wehlen."
  - `[108]` "As you're undoubtedly aware, Wehzek troops boast a legendary reputation."
  - `c431 [25]` (Civil War epilogue): "In the following weeks, thousands of Wehzek soldiers were deployed to Rizia."
  - `c354 [550]`: "And any attempt at - ahem - *grassroots* resistance will be met with the same firepower that just won you Pales."
- **Zille is isolated.** `c326 [294]` Lucita: "Zille is cut off from the Wehzek mainland. Divert enough funding and weapons to the right groups, and we can make an overthrow happen without getting our hands dirty."

**How the Wehzek garrison behaves under attack** (`c441`):
- `[84]` "The Wehzek troops fell back as soon as we entered."
- `[86]` "As the Wehzek troops were falling back, they intentionally gave all of their equipment to the civilians of Zille."
- `[85]` "The citizens themselves started to push our soldiers back. There was heavy fighting in some areas."
- `[112]` (agents): "the police in Zille managed to identify some of our agents who led them to other agents, uncovering the web."

**Su Omina and the ATA (armed and unarmed resistance):**
- `c324 [202]`–`[206]` Lucita: "...funding to Su Omina has commenced without a hitch." "They've been using the resources wisely, primarily for procuring weapons and establishing supply lines." "Su Omina is focusing on guerrilla warfare, sabotage of essential services, and slowly building support among the local population."
- `c307 [434]` Rico Toras: "Smolak was never going to hand Zille over on a silver platter. We'll only get back what's ours if we put the screws to him." `[435]` "The region is teeming with Rizian loyalists anyway. All they need is a spark."
- `c409 [1940]` "Thanks to your efforts, the armed resistance movement in Zille is reaching a boiling point." `[1947]` "Our Su Omina cell there has drawn increasing numbers of ethnic Rizians."
- `c409 [1948]` Titus: "And with the addition of military equipment shipments from Morella, their capabilities have been greatly enhanced." (`MITZ_Demand_ZilleResistance`)
- `c409 [1976]` Lucita: "...the simultaneous operations of the Ariana Toras Association have become problematic. While ATA is focused on extending Rizian goodwill to Wehzek citizens, Su Omina does... the opposite."
- `c448 [124]`/`[87]` (Turn 9): militants in ski masks with machine guns and "black and red patches", with no police.
- Report-only escalation (variables set outside the DB): `Reports_Turn06_Zille_SORailwayAttack` / `_SORailwayAttackFail`, `Reports_Turn07_Zille_SOHarborExplosion`, `Reports_Turn08_Zille_SOOfficialAssassination`. These map onto the design's "SO cuts the Zille–Nanta railway" and harbor-sabotage modifiers.
- Cut Turn 8 draft `Notes` (c588) `[363]` Titus: "Su Omina militants had planned an attack on a convoy of Wehlen supporters who want Zille to stay with Wehlen."

**Derdian and BFF mercenaries:**
- `c381 [73]` Jorga Azmal: "You saw my Disciples earlier. Imagine a larger team, just as trained, wielding sophisticated weaponry and stealth technology."
- `[77]` "Normally, Derdia would charge you for their services. But my Disciples are eager to help Rizia reclaim its land from the anti-Golcondist Wehzeks."
- `[72]` "Our mercenaries have been helping win wars since the days of the Empire of Moridia."
- `c380 [38]` Player: "Together, we will emancipate Zille and humiliate Wehlen."
- `c409 [1963]` Lucita: "...a number of Golcondist mercenaries in the region, awaiting our command." `[1964]` Titus: "Despite the Derdians' rather unconventional practices, their skills and expertise could prove invaluable in this endeavor." (`[1968]` is the Bluds variant.)
- `c390`/`c391 [15]` Esquibel: "...an internal resistance effort from the BFF could work in perfect concert with the Derdian mercenaries already present in the..."
- **Constraint.** In Turn 7, Derdian mercenaries go to Zille only when `Pales_War == false` (`c381`/`c380` branch on `Pales_War`).

**Morella.** `c327 [511]` Esquibel: "The Morellan foreign ministry has promised to ship supplies across the Gulf of Moridia to support the Zille resistance."

**Rumburg.**
- `c433 [546]` "Thanks to Rumburg, we have the power to take the region back by force if the referendum goes south."
- Turn 3 QueenBeatriceVisit (quoted in `zille-storyline.md`): "Then you will need Rumburg's help."

**Geography terms from the dialogue.** Zille, Nanta, Mave Nortus and Jales appear in dialogue. **Auremina, Dapalle, Isor and the Strait of Kreleck do not appear in any dialogue line**; they come from the national map and the codex (`zille-geography.md`). New lines can name them, but none can be quoted from vanilla. Nanta appears only in variable names and map tokens: `Reports_Turn09_Nanta_DiscardedBallotsDiscovered`, `Reports_Turn09_Nanta_StreetFestival`, `Reports_Turn10_Nanta_WehzeksProtest`, and `AddTokenStatusImmediately("Rizia_City_Nanta", ...)`.

---

## 6. Where a war victory should flow: the vanilla return pipeline

### 6.1 `Rizia/Turn 9/A_ZilleReturnedSpeech` (c440): the ceremony to reuse

**Entry and cast.**
- `[1]` → `[318]` SetRichPresence("Proclaiming the return of Zille","Smolak Square","Zille") → `[45]` music → `[41]`.
- `[42]` IF `Turn08_ZilleNeg_GiveSmolakEstelasHome` → `[119]`/`[103]`, the house is now Smolak's. Otherwise `[43]` → `[44]` AddConversant Estela → `[257]`, Estela's childhood home: `[262]` "Zille." `[273]` "Personally, I had no doubt in my mind that Zille would be ours again."
- `[63]` "I'm proud of you, son. If not for your leadership, this house and so many others would have been lost forever."

**The square:**
- `[74]` "Soldiers from the Rizian army were dispersed throughout the crowd, watchful for any sign of trouble."
- `[302]` (StoppedByAN) "AN peacekeeping forces were dispersed throughout the crowd..."
- `[315]` "Where the former grandstand once stood, there was now a memorial to the victims of the bombing."

**Interference-aware gates, which a war should trigger:**
- `[126]` IF `ZilleReferendum_Interfere_Army_Success or _Agents_Success` → `[128]` "...the distant shouts of Wehzek protesters at the back of the crowd." `[316]` "They were shouting that the will of the people of Zille had been ignored." → menu `[129]` remove them (`[130]` Democratic −1) / `[162]` ignore (`[163]` +1).
- `[132]` → `[134]` Estela: "Even if you had to resort to rather... unconventional tactics."
- `[141]` → `[143]` Estela: "Ah, yes, the power of this so-called 'democracy at work' I keep hearing about. No extra persuasion necessary, I'm sure..."
- `[136]`/`[150]`/`[157]` IF `ZilleReferendum_Won` → `[138]` / `[153]` "scanning the referendum results" / `[155]` news `..._RefVote`; else `[154]` news `News_Turn09_RH_ZilleReturnSpeech_Negotiations`. **A war victory has no referendum.** Leave `ZilleReferendum_Won` false and add a war news variant beside `[154]`/`[155]`.

**Speech lines that already fit a war victory** (opening menu `[181]` → `17, 3, 10, 27`):
- `[10]` "Citizens of Zille, let us revel in the glory of our triumph over the iron fist of Wehlen."
- `[11]` "For it was the Crown's strength and determination that secured this victory."
- `[20]` "While the road to liberation was fraught with challenges, overcoming those hardships has helped us emerge stronger than ever before."
- `[21]` "So let us honor the memories of those we have lost and the dreams that have been shattered along the way."
- `[22]` "May the sacrifices of those who fought for freedom guide us forward with resilience and hope."
- `[16]` "As one, we shall crush all who would dare to defy us and cement our legacy as the unquestioned rulers of this land."

**Ownership decision** (`[186]` → `187, 188, 189, 191`):
- `[195]`/`[196]`/`[211]` → `[213]` `ZilleOwnership_Brenas`. `[197]`/`[198]`/`[199]`/`[210]`/`[212]` → `[214]` `ZilleOwnership_RiziaImperii`.
- Angelica reacts at `[219]` "Angelica Sazon angrily rose from her seat." / `[224]` "she turned and left" / `[228]` "Angelica Sazon stood and applauded fiercely." / `[233]` "Glovurius axa Sazoni!"
- Then `[235]` `ZilleReturn_Success = true`; `[236]`–`[239]` map tokens for `Rizia_City_Zille` and `Rizia_City_Nanta` (HouseSazon or HouseToras); income-tax modifiers `[241]`–`[255]`; `[169]` `Reports_Turn09_Zille_CelebratesReturn`; `[170]` `Reports_Turn09_Nanta_StreetFestival`; `[102]` End.

**Recommendation.** After a war victory, run c440 as-is in Turn 10 (or late Turn 9). Set beforehand:
- `ZilleWar.Won`
- `ZilleReferendum_Interfere_Army = true` and `ZilleReferendum_Interfere_Army_Success = true`. This reuses the protester, "unconventional tactics" and reform-branch "accusations of interference" texts and blocks `Title_Peaceful`.
- `CountryRelation_Wehlen_Hostile = true`, `Situation_Diplomacy_AN_ThreatMitigationProtocol = false`, `Policy_Diplomacy_ZLLAActive = false`.

Add war-specific lines with new conditions next to `[74]` (soldiers in the crowd) and `[315]` (memorial).

### 6.2 Turn 10 and after

- **`Rizia/Turn 10/TalkWithPrimeMinister` (c348).** `[129]`/`[130]` IF `NewGov_Reform_ZilleUpdate == false and ZilleReturn_Success` → `c547:1` **Zille_Reform_Branch**. Its speakers are Manus Sazon, Daria de Rava and Adarfo Sotelato, depending on `Reform_PrimeMinister_*`. It reads `ZilleReferendum_Won`, `ZilleReferendum_Interfere_Army_Success`/`_Agents_Success` and `Situation_Law_WehlenRights_Full`.
  - `[51]`/`[52]`/`[56]`: "These fears have only grown since the referendum, as accusations of interference have fueled lingering distrust."
  - `[6]` Manus: "However, without a say in Zille's return to Rizia, many feel disconnected from the decision, leading to suspicions and resistance." This is the no-referendum branch and fits a war.
  - `[99]` Manus: "To make matter worse, some Su Omina members have taken matters into their own hands, violently evicting Wehzek families from their homes, claiming the land belongs to 'real' Rizians."
- **Turn 10 Foreign Affairs (c577).**
  - `[217]`/`[218]` "The mood is happy, but Zille's reintegration into Rizia under direct control of the Crown / into the province of Brenas has brought with it significant challenges."
  - `[227]` "The situation in Zille remains unchanged under Wehlen's control." (the defeat text)
  - Epic choices (hub `[690]` if returned, `[756]` if not) are listed in `zille-storyline.md` §4.
- **House Sazon (c353) and Angelica.**
  - `[1256]` "Zille was taken from all of Brenas, even from those who didn't participate in the uprising."
  - `[1257]` "You showed great faith in House Sazon by returning Iza to us. With Zille, all of Brenas will finally be whole again."
  - `[1038]` "But the local leadership is optimistic. Now that it's been freed from Wehlen's tyranny, Zille will flourish once more."
  - `[979]` "We're still seeing clashes between monarchists and Smolak loyalists, even with the AN forces around to keep the peace."
  - `[486]` "If we had reclaimed our ancestral lands in Zille, perhaps we could amass the strength necessary to defeat the revolution."
  - `c551 [830]` (REFORM_TALKS): "...as long as Zille is returned to the province of Brenas."
  - `Zille_PromisedToHouseSazon` (Sazon convos `[555]`/`[1168]`/`[1259]`, REFORM_TALKS `[881]`) → `Birthday_Gathering [174]` flips ownership to Brenas.
- **Rusty Montoro (RRG mines).**
  - `c573 [694]` "Yeah. When RRG got back our old sites in Zille, a bunch of them were all mined out -- but ripe for rehabilitation as farmland."
  - `c573 [695]` "Took advantage of the mess in Zille to get back some of RRG's old land there." This variant fits a war aftermath.
- **Ending lines.** `Turn_11_Ending_Speech [179]` "despite President Smolak's best attempts to stop us, we finally managed to bring Zille back to Rizia". `Epilogue` titles (§3.6 of `zille-storyline.md`).

---

## 7. Timing: Turns 8–10 alongside the Pales war

Scheduling is not in the DB. The order below is inferred from what each scene reads and says.

| Turn | Pales war (vanilla) | Zille (vanilla) | Zille war (proposed) |
|---|---|---|---|
| 6 | `SnO_WarEruptionBriefing` → `EnableToken("Rizia_Event_InvasionOfPales")`; Operation 1 on the war map (T6–7) | (background) Estela: "Your attention should be on the rightful restoration of Zille, not Pales!" (`c370 [255]`) | – |
| 7 | `Pales_War_Update_And_Tax` (c385) → `c386` Zille Treaty Discussion. Derdia and Rumburg aid branch on `Pales_War` | Zille HoD debate; Derdian mercenaries for Zille only if no Pales war | optional: mercenaries and BFF feed `War_Zille_Strength` |
| 8 | `SnO_War_Pales_PostOperation1` → `Operation 1 Results` → `Operation 2 Next Steps`; AN Sanctions (c402); Operation 2 on the map (T8–9) | 1. `SnO_SecurityBriefAndZille` (c409); 2. `RustyCall` (train to Mave Nortus); 3. `FPnT_ZilleNegotiations` (c424); 4. `FPnT_ANSession` → c402/c403 → **c404** (only if Success/Disapproved) | **(a)** plan seed at c409; ultimatum at c424 `[541]`; **(c)** declaration at c404 `[324]`; **Zille War Council** as a new scene at the end of T8 (or first thing in T9) |
| 9 | Operation 2 continues (Turn 9 scenes already read `War_Objective_Captured_Remaz/_Solles`, `c434 [399]`); `GRACERumburgMeeting` ceasefire offer | 1. `FPnT_CallWithSmolak` ("Zille for Pales"; before the vote); 2. `FPnT_ZilleReferendum` (c448); 3. `FPnT_ZilleReferendumInterference` (c441); 4. `A_ZilleReturnedSpeech` (c440). Also `Wehlen_Derdia_Meditation` (c437) and `IntermerkopumRoundtable` (c439) with Smolak present | **(b)** declaration at c448 `[198]` or after a loss; **Op I "Iron Gate"** (border wall, Auremina hills); **Op II "Golden Harbor"** if the timeline is compressed |
| 10 | `War_Pales_VictoryParade` / `War_Pales_Loss` → `War_Pales_PeaceTreaty`; `Decision_Turn10_Ceasefire_Accepted` | Foreign Affairs report and epic choice; Zille_Reform_Branch (via c348); House gatherings (Sazon) | **Op II/III** (Zille harbor, Nanta); Zille peace treaty; c440 ceremony (if not yet run); reform branch picks up `ZilleReturn_Success` |

**Two-front rules to inherit from vanilla:**
1. **Army split.** `c441 [12]`: during `Pales_War` the army fails in Zille unless `StrongArmedResistance and SuOminaActive`. Use this as a strength malus, not a hard block.
2. **Titus's warning.** `c409 [2138]` Titus: "Besides, we have our hands full with Pales."
3. **Rumburg forces the choice.** `c389 [686]` Beatrice: "Then you must decide. Zille or Pales?"
4. **The RPP.** `c385 [18]` Manus: "It is difficult for my party to support an all-out attack on the Grand Duchy, especially when the future of Zille is still in doubt." The RPP supports Zille over Pales, so a Zille war gets **more** Brenas/RPP support than the Pales war did.
5. **The war map is singular.** It has one event token and `RiziaDLCSetup.IsAnyOperationInProgress`. Pales Op 2 occupies T8–9. Either run Zille Op I in T9 only after Pales Op 2 resolves, or keep Zille operations dialogue-resolved while Pales is on the map. The `War_Control_Skipped` auto-resolve precedent is in `c410 [219]`.
6. **Conflicting T9 scenes.**
   - `Wehlen_Derdia_Meditation` (c437) and `IntermerkopumRoundtable` (c439) seat a friendly Smolak at the table. Gate them on `not ZilleWar.Declared`. c439 already uses `Diplomacy_Wehlen_IntermerkopumPotential`, which c424 `[1148]` sets false on failure.
   - `CallWithSmolak` (c434): if the war is already declared in T8, skip it or replace it with a ceasefire or "Zille for Pales" counter-offer. Smolak offering his army for Pales while fighting Rizia in Zille makes no sense.
   - If `ZilleReferendum_SmolakOffer_Accepted` (the deal was taken before the war), the war becomes a betrayal war. See `zille-storyline.md` §7.1 D.

---

## 8. Character stances and voice (for writing reactions)

Each entry gives the stance, verbatim anchor lines, and the likely reaction to a war declaration (*inference*).

**Lucita Azaro** (Duchess of Montaklar, War & Security Councilor). A realist hawk: she favours proxies first and the army on the mainland only.
- "Zille is a different matter. It lies on our mainland." (`c296 [787]`)
- "Zille doesn't need a referendum, Mr. Esquibel. It needs a resistance." (`c326 [307]`)
- "President Smolak may not respect a peaceful transition. But he will respect that." (`c326 [295]`)
- "Considering the state of Zille since it landed in Wehlen's hands, I see no point in remaining diplomatic." (`c296 [158]`)
- On the Turn 8 bombing request: "a direct military intervention in Wehlen is not within our current scope." (`c409 [2127]`)
- After the lost vote: "We could've turned this around. I know it." (`c448 [268]`)
- *Reaction:* she backs a Zille-only campaign and vetoes crossing the strait. With `Pales_War` she warns about overextension, as in `c412 [51]`: "if we push harder and fail, we'll find ourselves in an even more precarious situation."

**Taddeus Azaro** (General). An old soldier who wants revenge on the meddlers.
- "Death spared me for a single reason: to help Rizia retake Pales once and for all." (`c357 [534]`)
- "Those goddamn meddlers again... Someday it will be their turn as well." (`c412 [127]`)
- He shares Lucita's lines in c409, c448 and c441.
- *Reaction:* he relishes it, but if Pales is ongoing he says "We can't overextend." (`c412 [52]`)

**Carlos Robles Azaro** (second-in-command). No Zille lines. He is the operational voice that reads the forces, as in `c357 [383]`: "We can field approximately eight to nine fully equipped infantry divisions...". Use him for the Zille order-of-battle readout.

**Titus Gordion** (Captain of the Golden Guard; sometimes the security seat). Cautious and procedural.
- "With all that in mind, I suggest we review our contingency plan for getting Zille back should our agreement fail." (`c409 [1235]`)
- "I'm concerned that achieving a peaceful transfer of Zille is highly improbable." (`c409 [234]`)
- "Bold, yes, however, in this case, the drawbacks may outweigh the benefits." (`c409 [1992]`)
- "Besides, we have our hands full with Pales." (`c409 [2138]`)
- "We must act, Your Majesty. And with haste. We have all of the options prepared and ready to begin." (`c448 [181]`)
- *Reaction:* he obeys ("Consider it done, Your Majesty.", `c448 [212]`) and offers the Golden Guard for the border wall assault.

**Laurento Esquibel** (Foreign Affairs). A dove and legalist.
- "as long as Wehlen still holds Zille, diplomacy is paramount." (`c296 [72]`)
- "An outright attack would push him firmly into United Contana's embrace, perhaps beyond retrieval." (`c296 [784]`)
- "In the meantime, any perceived aggression on our part may only give him an excuse to break it." (`c296 [171]`)
- "I really would caution against doing anything rash at this point, Your Majesty." (`c448 [182]`)
- "I am loathe to suggest violence as a solution to the Zille conundrum." (`c391 [20]`)
- "God be damned!" (`c404 [321]`)
- "A historic win! I knew we could solve this without violence." (`c448 [250]`, to be suppressed after a war)
- *Reaction:* he objects once, then loyally manages the AN fallout: "This will impact our public image heavily, Your Majesty." (`c441 [235]`)

**Hugo Toras** (Grand Vizier). Cautious, but sees force as a last card.
- "If we move against Wehlen openly, we risk strengthening the very man we could wish to keep in check." (`c296 [785]`)
- "At some point, it may become necessary to call Wehlen's bluff." (`c326 [131]`)
- "I am certain that if we give up this gas field, the Wehzeks will be even more emboldened to keep their grip on Zille." (`c313 [244]`)
- "If Wiktor Smolak doesn't return Zille in the face of that proof, the Alliance of Nations will surely force him to." (`c593 [38]`)
- "Compassion is noble, but it must not overshadow the need for decisive action in times of war." (Turn 6 Intro, `c373 [63]`)
- *Reaction:* he accepts the war once the AN has failed, worries about the House levies and Brenas, and reminds the king that "the Sazons lost the right to call Zille 'theirs'..." (`c326 [247]`).

**Manus Sazon** (RPP, Brenas; sometimes HoD chair or PM). Prefers peace, but Zille is his province.
- "We worry that the growing presence of Su Omina in Zille... risks undermining our goal of a peaceful transfer." (`c386 [17]`)
- "At this stage, Wehlen will not want to risk damaging our agreement. And if they do, my province can handle it." (`c386 [186]`)
- "It is difficult for my party to support an all-out attack on the Grand Duchy, especially when the future of Zille is still in doubt." (`c385 [18]`)
- "It is a simple fact that war is a source of suffering for many and enormous financial windfalls for an elite few." (`c385 [40]`)
- *Reaction:* he backs a Zille war more readily than the Pales war, provided Zille goes to Brenas, and asks for veterans' welfare (`c385 [35]`).

**Daria de Rava** (RNC). The parliamentary hawk on Zille.
- "A peaceful transfer might be our goal, but it was never President Smolak's. My party supports the reclamation of Zille by any means necessary." (`c386 [19]`)
- "How can you say that after the Friendship Day incident? There is no telling what Wehlen will do, even if Zille is returned without a fight." (`c386 [185]`)
- *Reaction:* full support, and she argues for central (Imperii) governance afterwards (`c422 [290]`).

**Estela Toras** (Queen Mother, born in Zille). Wants Zille back by any means and resents the Pales detour.
- "We need our land back. No matter what kind of announcement Wiktor Smolak has up his sleeve." (`c315 [35]`)
- "Pales is not ours to control in the first place. Zille, on the other hand..." (`c315 [192]`)
- "Your attention should be on the rightful restoration of Zille, not Pales!" (`c370 [255]`)
- "We can thank his Eastern benefactors, I presume." (`c298 [139]`)
- "To add insult to injury, we lost Zille. If I may say so, dear... you had one job!" (`c346 [353]`)
- In Turn 2 she counselled against provoking Wehlen "while our own country was still weak": "And made outright enemies of Wehlen while our own country was still weak? Don't be foolish." (`c298 [153]`)
- *Reaction:* approval, with the "unconventional tactics" barb (`c440 [134]`).

**Vina Toras** (Princess). The anti-war conscience.
- "What of the people living in Zille? Has anyone asked them how they feel about an extra three years of Wehzek rule?" (`c326 [729]`)
- "'Real' Rizians, Father? What do you call the children of Wehzek civil war refugees who have never known any other home?'" (`c320 [881]`)
- On Pales: "How does our situation call for an invasion?" (`c361 [672]`); "And I don't agree with how you're fuelling the fires of war." (`c361 [668]`)
- *Reaction:* opposes the war and worries about Wehzek Zilleans. If married to Manus, she is torn because of Brenas.

**Angelica Sazon** (House Sazon matriarch). Wants Zille for Brenas; the war is acceptable if it ends that way.
- "Zille was taken from all of Brenas, even from those who didn't participate in the uprising." (`c353 [1256]`)
- "With Zille, all of Brenas will finally be whole again." (`c353 [1257]`)
- "But sacrificing Zille to regain Pales? It sends a rather strong message to those of us in Brenas." (`c353 [1279]`)
- "...as long as Zille is returned to the province of Brenas." (`c551 [830]`)
- *Reaction:* she offers Sazon levies (`Army_Unit_Sazon_*` exist) in exchange for Brenas ownership. This is a natural pre-war bargain that sets `Zille_PromisedToHouseSazon`.

**Russello "Rusty" Montoro** (RRG). A profiteer.
- "RRG already almost went belly-up after we lost Zille." (`c321 [238]`)
- "Can't wait to see what state our mines are in." (`c416 [109]`)
- "Do me a favor, at least. Take a picture of old Smolak's face when he signs Zille back to us." (`c416 [540]`)
- *Reaction:* he backs it and wants the Auremina gold sites; post-war he "took advantage of the mess in Zille" (`c573 [695]`).

**Wiktor Smolak** (President of Wehlen). Theatrical, mocking, quick to threaten.
- "Zille wants to remain part of Wehlen." (`c404 [98]`)
- "The Zille referendum will happen!" (`c404 [134]`)
- "I will not be silenced!" (`c404 [133]`)
- "Get a good look, Your Majesty, because, I'm sorry to say, you won't get many opportunities to do so in the future." (`c424 [788]`)
- "If I stop following through with my threats -- sorry, promises -- no one will take me seriously." (`c424 [793]`)
- "Come now, Your Majesty. Empty threats are beneath someone of your stature." (`c424 [799]`)
- "Challenge accepted. We Wehzeks have a history of resilience. I've stood against a superpower; your kingdom will be no different." (`c424 [1120]`)
- "Wehlen's not very far away, after all." (`c326 [1173]`)
- "That is your right. As it is my right to take Zille's safety into my own hands." (`c326 [1007]`)
- "As you're undoubtedly aware, Wehzek troops boast a legendary reputation." (`c434 [108]`)
- "...any attempt at - ahem - *grassroots* resistance will be met with the same firepower that just won you Pales." (`c354 [550]`)
- *Reaction:* he goes to the AN and to his CSP patrons. He frames the attack as a monarchy crushing Zillean democracy ("it is the AN's obligation to rescue these innocents from the clutches of their would-be King", `c404 [135]`). In the field he follows c441 doctrine: withdraw regulars and arm civilians.

**Others worth a line:**
- **Beatrice Livingston** (Rumburg): "I am counting on that." (`c433 [552]`); "Then you must decide. Zille or Pales?" (`c389 [686]`).
- **Jorga Azmal** (Derdia): "Together, we will emancipate Zille and humiliate Wehlen." (player line, `c380 [38]`); "My men will not fight for a kingdom that is complicit in the persecution of the Bludish people." (`c381 [68]`).
- **Sal Ignacius**: "In simple terms, the scriptures acknowledge just wars -- conflicts fought in defense of the divine message or to protect the pillars of enlightenment." (`c360 [773]`); "Keep in mind, this could anger President Smolak, who may see it as betrayal and further complicate Zille's return." (`c360 [680]`).
- **Rico Toras** (Su Omina patron): "We'll only get back what's ours if we put the screws to him." (`c307 [434]`).
- **Alma Saltana** (Morella; an AN ally if `MITZ_Demand_ZilleDiplomacy`): "Are you going to believe the words of a dictator? This madman? Really?" (`c404 [130]`).

---

## 9. Quick reference: insertion nodes

| # | Conv (id) | Insert after / into | Existing links | Type |
|---|---|---|---|---|
| a1 | SnO_SecurityBriefAndZille (409) | between `[2127]`/`[2132]`/`[2134]` and `[2135]`, or a new link in hub `[2114]` | `[2114] -> 2111,2115,2140` | player option + councilor reply; sets `ZilleWar.PlanPrepared` |
| a2 | FPnT_ZilleNegotiations (424) | gated menu `[541]` | `-> 556,1155,543` | player ultimatum; sets `ZilleWar.UltimatumIssued`, NP −2; rejoin `[1219]` |
| a2' | FPnT_ZilleNegotiations (424) | menu `[1118]` | `-> 1122,1119,540` | ungated variant (weaker wording) |
| a2'' | FPnT_ZilleNegotiations (424) | menu `[798]` | `-> 789,791,795` | flavour for the forced-failure route |
| a3 | new conversation | after c424 `[435]` End | – | Zille war council, gated on `ZilleReturn_Success == false and ZilleReferendum_Disapproved` |
| b1 | FPnT_ZilleReferendum (448) | menu `[198]` | `-> 200,199,207` | [Declare War]; sets `ZilleWar.Declared`, `CountryRelation_Wehlen_AtWar` |
| b2 | FPnT_ZilleReferendum (448) | after `[268]`/`[269]`/`[270]` | `-> 277` / `-> 281` / `-> 282` | war after a lost vote |
| b3 | FPnT_ZilleReferendumInterference (441) | menu `[235]` (sibling of `[238]`) | `-> 236,238,279` | war after a failed army interference |
| c1 | Zille Territory Issue (404) | menu `[324]` | `-> 391,326,325,327` | [Declare War] after the AN allows the referendum; rejoin `[88]` |
| c2 | Zille Territory Issue (404) | closing menu after `[203]`/`[204]`/`[205]` | `-> 207,208` | public ultimatum; sets `ZilleWar.PublicUltimatum` (optional vote shift) |
| c3 | Zille Territory Issue (404) | interrupt menu `[101]` | `-> 105,104,103,403` | flavour; route through `[396]` |
| t | SnO_WarEruptionBriefing (357) | clone | – | council skeleton (section 4.1) |
| t2 | Operation 2 Next Steps (412) | clone | – | between-operations objective choice |
