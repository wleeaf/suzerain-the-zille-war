# Zille war: supply-war levers grounded in game data

Source: the extracted Rizia dialogue database (`convs/*.txt`, `_index.tsv`, `_variables.tsv`).
Notation follows the other docs: `cNNN` is a conversation id, `[n]` an entry in that conversation,
and the namespace is `RiziaDLC.` unless another prefix is written. Mod variables use the
`ZilleWar.` namespace the plugin already registers (`ZilleWar/ZilleOperation.cs`).

Already covered elsewhere and not repeated here: the Pales war flow
(`docs/pales-war-flow.md`), the Zille storyline and its gates (`docs/zille-storyline.md`), and
canon and community research (`docs/rizia-story-research.md`).

**Design goal.** Cutting Wehlen's supply across the Strait of Kreleck must be possible but
costly, and it must make the Zille garrison considerably, not completely, smaller and weaker.

---

## 0. Summary

- **The navy can only carry a blockade if it is built up.** Rizia starts with 1 destroyer, 1
  submarine and the flagship (`Army_Unit_Ship_Total` 1, `Army_Unit_Submarine_Total` 1,
  `Army_Unit_Flagship_Total` 1). Vanilla blockades need `Ship + Submarine` of at least 4, 5 or
  6, and they lose 1 to 3 destroyers. The Strait of Kreleck faces Contanan-funded mines, coastal
  batteries, radar and anti-air (c296 [783]–[792]). It should therefore sit **at or above the
  hardest Pales blockade**: 6 for a partial blockade, 8 for a full one (section 1.5).
- **Rizia's navy already patrols the strait in vanilla.** "Operation Bear Trap" (c326 [1472])
  sends 2 destroyers to stop BFF insurgents "crossing the strait"
  (`Situation_Diplomacy_Wehlen_Terrorism_AntiBFF`, `Army_Unit_Ship_Total - 2`). In Turn 8 the
  security councillor reports: "Our navy is still engaged in blockading ships carrying potential
  BFF terrorists from Wehlen's southern mainland." (c409 [872]). That gives a ready-made
  "turn the patrol into a blockade" hook.
- **Wehlen's backers in the data:**
  - United Contana and the CSP: "Eastern benefactors", tanks, coastal defences, radar and
    anti-air, "still backing Wehlen through all this".
  - Sordland, through President Anton Rayne: base-game Wehlen trade and joint operation.
  - Derdia, as an oil customer only.
  - Wehlen is already "suffering the effects of the GRACE embargo on us, among other sanctions"
    (c437 [131]).
  - Lespia, Rumburg, Morella and Derdia are all anti-Smolak in the dialogue.
- **The AN is the strongest lever and is already wired.**
  - Votes are a ±1 counter per delegate, keyed to `CountryRelation_<X>_*` and
    `Situation_Diplomacy_GlobalImage_Bad`.
  - The Rusty evidence flips the ATO votes. That is Arcasia and Agnolia, which otherwise follow
    Lespia (c404).
  - Smolak's Threat Mitigation Protocol "has been challenged, but the AN failed to meet a
    consensus" (c326 [90]), so a re-challenge fits canon.
  - Sanctions are worth `Budget_Modifier_ANSanctions` -3/-6 and `Energy_Modifier_ANSanctions`
    -2/-5 (c402).
- **There are no Wehzek troop numbers in the data.** Only flavour: tanks, "a dozen soldiers",
  troops who "fell back" and armed civilians, and an amphibious capability. The garrison lives in
  the mod (`ZilleOperation.Garrison()`).

---

## 1. Rizian military strength model

### 1.1 Units (`Army_Unit_<Type>_{Total,Deployed,Dead,OnMission}`, plus `_InTraining` where trainable)

| Branch | Type | `_Total` start | Notes |
|---|---|---|---|
| Navy | `Ship` (destroyers) | 1 | `_InTraining` yes. The Bear Trap patrol removes 2 (c326 [1472]); quitting it returns 2 (c381 [99], c389 [578]/[1033]). |
| Navy | `Submarine` | 1 | `_InTraining` yes |
| Navy | `Flagship` (RRN Valero) | 1 | Not counted in vanilla blockade sums. Lost by `Incident_Naval_RescueBoth` (c357 [91]) → `Situation_Military_LostPrideOfNavy`. |
| Navy | `Marine` | 0 | `Army_NumberOfMarinesInShip` = 3 |
| Air | `Fighter` | 1 | `_InTraining` yes. `Army_Unit_Fighter_Dead` initial is `False` (sic). |
| Air | `Bomber` | 1 | `_InTraining` yes |
| Land | `Infantry` 5, `Support` 1, `Tank` 0, `Mountaineer` 1, `Paratrooper` 0, `Militia` 2, `Mercenary` 0, `GoldenGuard` 0, House `Azaro`/`Sazon`/`Toras` 1/1/1 | | `GrandTotal_Total` 13 |

**Stockpiles that the production panel turns into units** (`Resources_Military_*`):
- `Ships` 2, `Submarines` 1, `FighterPlanes` 0, `BomberPlanes` 0.
- `Tanks` 25, `Trucks` 75, `Equipment` 1500, `Manpower` 3250.
- Each has a `*ModifiersTotal`.

Production is engine work; no dialogue writes `_InTraining`.

**Production modifiers** (initial values):
- `Ship_Modifier_Dockyards` 0, `Submarines_Modifier_Dockyards` 0.
- `FighterPlane_Modifier_Factory` 0, `BomberPlane_Modifier_Factory` 0.
- `Tank_Modifier_Factory` 25, `Equipment_Modifier_Factory` 0, `SupportVehicle_Modifier_Factory` 0.

**Decrees** (`Policy_RD_Order_*_Project/_Completed`, with matching `Enable_` flags):
- `FighterPlaneFactory`, `BomberPlaneFactory`, `TankFactory`, `SupportVehicleFactory`,
  `MilitaryEquipmentFactory`.
- `ExpandFortAlesNavalBase`, `TeradorAirforceBaseExpansion` (Terador is on the Pales front, not
  the Zille front).
- `Policy_RD_Economy_MonqizPortShipyardExpansion_*`.
- `BuildMaveNortusIntelHub` (on the Zille border; unlocked by c304 [443]/[492]).
- `ExpandBorderGuards`.

**Arms trade counters:**
- `Policy_RD_Order_Bought/Sold{MilitaryEquipment,Ships,Tanks,SupportVehicles,Submarines}Count`.
- `Policy_RD_Order_BoughtFighterCount`, `_BoughtBomberCount`, `_SoldFighterPlaneCount`,
  `_SoldBomberPlaneCount`.
- `ImportArmsCount`, `ExportArmsCount`, `Title_Armslord`,
  `Situation_Economy_ArmsImporter/_ArmsExporter`.

**Arms trade with Wehlen itself:**
- `Situation_Diplomacy_Wehlen_ExportArms` (c326 trade deal).
- `Equipment_Modifier_WehlenArmsExports` -250 per turn.
- `Turn08_ArmsDealSmolak` (c424 [760], Equipment -1000).

A Rizia that sells arms to Wehlen is literally equipping the garrison. Ending those exports is a
cheap, lore-true first lever.

### 1.2 Unit gains written by dialogue (naval and air)

| Source | Where | Effect |
|---|---|---|
| Prologue "strengthen the armed forces" | Prologue [331] | `Resources_Military_Ships` +1 (plus land units) |
| Morella | c327 [1050] | `Resources_Military_Ships` +1, `_Submarines` +1, Equipment +1000 |
| Morella | c327 [1033] | `Situation_Military_MorellanAirPlanes`, `Resources_Military_AirSupportCooldown` -1 |
| Quit Bear Trap (Derdia) | c381 [99] / c380 | `Army_Unit_Ship_Total` +2 (the detached ships come home) |
| Quit Bear Trap (Rumburg) | c389 [578] / [1033] | `Army_Unit_Ship_Total` +2 (the same ships; `pales-war-flow.md` 5.3 lists this as a Rumburg gift, which it isn't) |
| Lespia (if `GRACETrade_Weakened` or `Diplomacy_Rumburg_NegotiatingPower <= 0`) | c423 [1072] | `Resources_Military_BomberPlanes` +100, `_FighterPlanes` +200, `_Tanks` +150 |
| Derdia | c381 / c380 | `Resources_Military_DerdianMissiles`, `AirStrikeCooldown` -1 |
| Pilgrim sea route | c437 [198] | `Resources_Military_Ships` -1 (a cost) |

### 1.3 Readouts and thresholds dialogue uses (naval and air)

**War briefing** (c357 [434]–[447]):
- `Army_Unit_Ship_Total` ≥ 5: "a large number of destroyers -- which will surely help us gain
  superiority at sea" ([440]).
- ≥ 3: "enough destroyers to be able to contest the Palesian navy at critical locations"
  ([441]).
- Below 3: "we do not have enough fleets to be able to counter Palesian ships in any direct
  confrontation" ([442]).
- `Army_Unit_Submarine_Total` ≥ 4: "enough of them to be able to wreak havoc" ([446]).

**Turn 8 general-staff readout** (c590 [115]–[127]):
- Ship ≥ 5: "considerable number of destroyers -- pivotal for projecting power in the Antacean
  Sea".
- Ship ≥ 3: "capable of contesting our immediate neighbors at critical locations, if
  necessary".
- Submarine ≥ 4: "enough to effectively defend our primary trade routes".
- The flagship line reads `Army_Unit_Flagship_Total == 1` ([106]).
- "despite significant investments in our navy, we still lack access to cutting-edge Valgish
  tech" ([110]).
- Air: only `Focus_MilitaryBranch_Airforce`. "fully capable of challenging regional powers"
  versus "barely capable of contesting Palesian air superiority" ([100]/[104]).

**Pales Navy comparison:** "while we can theoretically hold our own against the Palesian navy,
they boast both superior technology and larger numbers" (c357 [430]).

**Engine-derived situations** (never written by dialogue):
- `Situation_Military_NavalInterdictionCapacity_Lacking` (starts **True**) / `_Strong`. Exactly
  the right name for a strait blockade gate. Its formula is in game code.
- `Situation_Military_RiziaFleet_Small/_Medium/_Large`.
- `Situation_Military_Airforce_Total_Small/_Medium/_Large`, `_Offence_Strong`,
  `_Defence_Strong`, `_Balanced`, `_Efficiency_Weak/_Medium/_Strong`.
- `Situation_Military_ShortMediumMissiles_Lacking` (True), `Situation_Military_ModernBombers_Lacking`
  (True).

Readers: VictoryParade c333 [162]/[170]/[851]; balcony speech c372 [612].

### 1.4 Power projection

- **Variables:** `Country_Military_PowerProjection` is numeric, starting at -2.
  `PowerProjection_from_Decisions` starts at 0. `Authority_Modifier_PowerProjection` starts at 0.
  The tiers are `Situation_Diplomacy_PowerProjection_Weak` (starts True), `_Average`, `_Strong`
  and `_Overwhelming`, and they are derived in code.
- **Numeric boundaries seen in dialogue:**
  - T10 Foreign Affairs c577 [96] `<= 6` and [102] `<= 10`.
  - T10 Notes c549 [207]/[210]: during the Pales war, `<= 6` reads "weakening" and `<= 10`
    reads "edging toward a stalemate".
  - So roughly: above 10 is strong, 7–10 middling, 6 or less weak. The exact tier cut points
    are UNVERIFIED (in code).
- **Dialogue that adds to `PowerProjection_from_Decisions`:**

  | Choice | Scene | Cost | Gain |
  |---|---|---|---|
  | National Defense HQ | c345 [154] etc. | -1 to -3 Budget or -3/-5 Authority | +2 |
  | Rizian Armada (`NewGov_Epic_Armada`) | c577 [597] | menu says -7 Budget, DO applies -6 | +2 |
  | Military-industrial complex (`NewGov_Epic_MilitaryIndComplex`) | c576 [84] | -5 Budget | +2 |
  | Stratocracy | c422 [1846] | | +1 |

  A Caleqabiz base lease also "has enhanced our power projection" (c577 [355]/[356]).
- **Strong or Overwhelming gates in the Zille thread:**
  - c409 [2116] "Let's bomb the bastards" (refused at [2127]: "a direct military intervention in
    Wehlen is not within our current scope").
  - c424 [541] the final-warning threat; [1185] the Overwhelming bluff that wins Zille.
  - c441 [13] the army interference succeeds outright.
  - c393 the Pales alliance.
  - c433 [500] Rumburg's tone.
- Smolak on why it matters: "With Arcasia breathing down my neck -- let alone Rumburg -- power
  projection is everything." (c424 [585]); "The amount of military strength you've managed to
  amass since your reign began is truly impressive." (c424 [961]).

### 1.5 Vanilla blockade rules, and a strait threshold

Pales blockades are resolved in c357 on `Army_Unit_Ship_Total + Army_Unit_Submarine_Total`; the
flagship is not counted.

| Vanilla blockade | Briefing text | Needs | Success | Failure |
|---|---|---|---|---|
| Gulf of Remaz ([303]) | "disrupting their trade without a risky confrontation at sea" | ≥ 4 ([323]) | `War_AttackPlan_BlockadeRemaz_Success`, no loss | lose 1 ship |
| Gulf of Ales ([301]) | "their most heavily guarded area but the closest to our coastline" | ≥ 5 ([320]) | `_GulfofAles_Success`, lose 1, enemy naval override `_AlesBlockaded` | lose 2 |
| Gulf of Pales ([302]) | "a direct confrontation with their strongest fleet" | ≥ 6 ([306]) | `_BlockadePales_Success`, lose 2 | lose 3 |

Losses are applied as `Army_Unit_Ship_Total -= n; Army_Unit_Ship_Dead += n` ([362]–[366]).

**Why the Strait of Kreleck should be harder than the Gulf of Pales:**
- c296 [783] (Lucita): "The sea approaches are heavily mined, and reinforced with layered naval
  batteries. United Contana's aid allowed Wehlen to significantly expand those defenses."
- c296 [786]: "We also do not possess enough advanced vessels capable of reliably detecting and
  clearing minefields in the Gulf."
- c296 [792]: "Contanan radar and anti-air systems would blunt our strikes long before their
  batteries are neutralized."
- Wehlen has a real amphibious arm: "Operation Moonlight Sonata ... Wehzek boats arrived on the
  shores of Pales at midnight" (c354 [58]).
- Smolak to Derdia: "Have you seen our arsenal?" (c437 [152]).

**Where the strait is easier.** A blockade of Zille does not have to approach Wehlen's mined
coast. Lucita: "Zille is cut off from the Wehzek mainland" (c326 [294]/[310]). The Zille harbour
sits on the south-west bay, and Nanta is on the north coast (`zille-geography.md`). Rizia can
interdict the Zille-side approaches under its own coastal cover from Mave Nortus.

**Proposed strait tiers** (all on `Army_Unit_*_Total` at the war briefing):

| Tier | Gate | Meaning |
|---|---|---|
| Patrol / harassment | `Ship + Submarine >= 4` | Submarines raid convoys; Remaz-like. Low risk. |
| Partial blockade | `Ship + Submarine >= 6` **and** `Ship >= 3` | Gulf-of-Pales tier. Surface ships are needed to stop and board merchant traffic, which submarines can only sink. |
| Full blockade | `Ship + Submarine >= 8` **and** `Ship >= 4` **and** `Submarine >= 2` **and** a mine-countermeasure flag | Beyond anything vanilla asks; justified by Lucita [786]. |

- **Mine-countermeasure flag** (any one of):
  - `Policy_RD_Order_ExpandFortAlesNavalBase_Completed`;
  - `Policy_RD_Economy_MonqizPortShipyardExpansion_Completed`;
  - a new purchase `ZilleWar.Navy_Minesweepers`.
- **Optional alternatives:** let `Situation_Military_NavalInterdictionCapacity_Strong` stand in
  for the `Ship` sub-gate, and let `Army_Unit_Flagship_Total == 1` count +1 toward the sum.

**Is this reachable?** From a start of 1 + 1, a full blockade needs about +6 hulls. Possible
sources:
- the prologue ship;
- Morella's +1/+1;
- getting the 2 Bear Trap ships back;
- dockyard decrees and purchases.

So it needs a navy-focused reign (`Focus_MilitaryBranch_Navy`) and several costly choices.
That is the desired "possible but costly".

---

## 2. Wehlen's backers and allies

Relation flags `CountryRelation_<X>_{Allied,Friendly,Neutral,Unfriendly,Hostile}` exist for
Wehlen, Pales, Morella, Derdia, Rumburg, Lespia, Valgsland and Sordland.

- **Plus:** `CountryRelation_Pales_AtWar` and `CountryRelation_Wehlen_AtWar`. The latter is never
  touched in dialogue: a free hook.
- **Who sets them.** Dialogue writes these flags only 7 times (c383 Derdia Friendly; c393 Pales
  Allied; c377 Derdia Unfriendly; c441 Wehlen Unfriendly/Hostile). The engine sets them
  otherwise, probably from `Diplomacy_<X>_NegotiatingPower`, though this is UNVERIFIED.
- **Writing caution.** Dialogue writes set one tier without clearing the others. A mod should
  clear the sibling flags.
- **`Diplomacy_<X>_NegotiatingPower` starting values:** Wehlen 3, Rumburg 5, Derdia -3,
  Lespia -1, Morella -1, Valgsland -1, Sordland -1, Pales -3.
- **There is no United Contana or CSP relation variable.** The CSP shows up only as
  `Situation_Diplomacy_GlobalInfluence_CSP`, `Policy_Diplomacy_Caleqabiz_ValgslandCSP`,
  `NewGov_Epic_ApplyCSP` and news/codex flags. In the AN, United Contana (Leon Malenyev) votes
  with Valgsland (c402 [293]–[297], c404).

### 2.1 United Contana / CSP ("Eastern benefactors"): the real patron

**Evidence:**
- Estela at the Friendship Day tanks: "We can thank his Eastern benefactors, I presume." (c298
  [139]).
- Lucita: "all the shiny new tanks Wehlen's oil money and United Contanan funding has bought him"
  (c296 [78]).
- Lucita: "The funding Wehlen has received from United Contana may also have Smolak believing he
  can act with impunity." (c296 [172]).
- Lucita, the coastal defences, [783]; Contanan radar and anti-air, [792].
- Esquibel: "An outright attack would push him firmly into United Contana's embrace, perhaps
  beyond retrieval." (c296 [784]).
- Esquibel at the AN: "he himself has well-documented ties to United Contana" (c316 [129]).
- Alvarez: "President Smolak has the long arm of Malenyev guiding him." (c314 [136]).
- Saltana: "United Contana is still backing Wehlen through all this." (c323 [306]).
- Beatrice: "the Republic of Wehlen and their backers in the CSP" (c305 [42]).
- Historical: United Contana backed the north in the 1930–40 Wehzek civil war (codex), and
  "Arcasia and United Contana nearly destroyed it by proxy in a rush to gain access to its oil"
  (c309 [505]).

**Scenes where Rizia can touch the CSP:**
- **c451 `Hegel Side Convo` (T9).** Hegel: "There is precisely one thing you can offer. Your
  island, Caleqabiz. Allow Valgsland and the CSP to use it as a base of operations." ([114]).
  Accepting ([154]/[159]/[161], +4 Budget) sets `Policy_Diplomacy_Caleqabiz_ValgslandCSP` →
  "Then my vote is yours." ([170]). Refused outright if `Policy_Diplomacy_Caleqabiz_LespiaATO`
  ([125]/[147]).
- **c316 [357]:** the player may call out Smolak's Contanan backing at the AN.
- **c589 [66]–[69]** (Esquibel): "the CSP has been behaving pragmatically so far."
- **c577 [820]:** applying for CSP candidacy (`NewGov_Epic_ApplyCSP`, Turn 10, too late for the
  war).

**Lever.** Convince the CSP that Wehlen is not worth defending, or pay it off: a Caleqabiz
basing deal, or an energy or nationalisation alignment through Morella and Valgsland.

- **Effect on the garrison:** no Contanan resupply of anti-air and radar, and no new tanks. This
  is the biggest single "quality" lever.
- **Cost:**
  - alienates Rumburg (Beatrice: "The CSP already has its hooks in Morella", c451 [160]) and
    ATO/Lespia;
  - Taddeus or Sal objections (c589 [59]/[63]);
  - `PersonalValue_Malenyevist` +1.

### 2.2 Sordland (President Anton Rayne)

- **Ties:** `BaseGame.Policy_Diplomacy_Wehlen_Trade` / `_JointOperation` /
  `BaseGame.Wehlen_JointOperation` / `BaseGame.Policy_Diplomacy_Wehlen_DefendBorder` (from the
  Sordland save).
- **Smolak:** "My friend Anton would solve this in an instant." (c437 [56]).
- **Alvarez:** "And now Smolak seems to be getting closer to Sordland." (c423 [482]).
- **Rizia's relation:** `CountryRelation_Sordland_*`. Scenes: c299 `FPnT_SordlandTradeVisit`
  (T2), c319 `FPnT_SordlandBondPurchase` (T4, `Policy_Diplomacy_Sordland_BondPurchase`), c401
  [61] (Sordish foreign minister Deivid Wisci tips Esquibel off).
- **Lever:** Sordland is a land neighbour of Wehlen, not on the strait, so it matters for AN
  votes and Wehlen's overall economy, not for strait supply. Keep it to the AN vote.

### 2.3 Derdia (Supreme Wiseman Jorga Azmal): anti-Wehlen, but buys Wehzek oil

- **Hostility:**
  - "my Disciples are eager to help Rizia reclaim its land from the anti-Golcondist Wehzeks"
    (c381 [77]).
  - "Together, we will emancipate Zille and humiliate Wehlen." (c380 [38]).
  - But "Yet not enough for you to cut ties with Wehlen." (c437 [36]) while Rizia stays in Bear
    Trap.
- **Oil link:** Smolak wants exclusive Intermerkopum oil deals "Along the lines of what we
  already have with Derdia" (c439 [271]). In c437 he pushes Derdia to buy Wehzek oil because of
  "the GRACE embargo on us, among other sanctions" ([131]).
- **Variables:** `CountryRelation_Derdia_*`, `Diplomacy_Derdia_NegotiatingPower` (-3),
  `Policy_Diplomacy_TradeDeal_Derdia`, `Situation_Diplomacy_EnergyCommitment_Derdia_Failed`,
  `Resources_Military_DerdianMercenaries(_Zille/_Pales)`, `Resources_Military_DerdianMissiles`,
  `Diplomacy_Derdia_ANSpeech_MakeHappy/_MakeMad`, `Diplomacy_Derdia_Arbitration_Support`.
- **Scenes:**
  - c316 [364] AN appeal: "Wehlen's occupation of Zille represents an invasion of Dastnurity
    into the land of St. Wruhec." Jorga applauds ([367]).
  - c377 `FPnT_DerdiaNegotiations` (T7). Price precedent: -4 Authority to lift the blasphemy laws
    for aid ([1027]).
  - c380/c381 Fighting Assistance: missiles -2 Budget, mercenaries -1, both -3.
  - c436/c437 Turn 9 mediations.
- **Lever:**
  - Replace Wehzek oil sales to Derdia with Rizian energy, which starves Wehlen of hard currency.
  - Derdian missiles to suppress Contanan anti-air.
  - Derdia's AN vote.
  - The prerequisite in all three is quitting Bear Trap.

### 2.4 Lespia (PM Patricio Alvarez): hostile to both, holds the ATO swing votes

- **Anti-Wehlen:**
  - "Wehlen's actions in Zille should be considered an illegal occupation of Rizian territory."
    (c316 [221]).
  - "The Wehzeks seized the assets of our companies and sent a wave of refugees towards our
    borders." (c423 [475]).
  - "He's a slippery man, Smolak. You won't get your territory back unless you tighten your grip
    on him." (c314 [177]).
- **Anti-Rizia:**
  - organised the Friendship Day bombing (c401 [114]);
  - leads the T8 sanctions call (c402 [44]–[101]);
  - arms Pales (`War_Pales_LespianSupport`).
- **AN weight.** Arcasia (President Dwight Walker) and Agnolia (Maartin Van Hoorten) vote the
  way Lespia votes (c402 [278]–[292]). In c404 both flip to Rizia's side when
  `Zille_Publicized_LespianInvolvement_Rusty` ([694], [725]). Walker after the reveal: "Let the
  record show Arcasia had no knowledge of Lespia's actions." (c401 [185]).
- **Variables:**
  - `CountryRelation_Lespia_*`, `Diplomacy_Lespia_NegotiatingPower` (-1),
    `Diplomacy_Lespia_WehlenHelp` (c314: Violent resistance +3, Referendum power +2).
  - `Situation_Diplomacy_Lespia_SanctionsRizia`, `Budget_Modifier_LespiaSanctions` (-2).
  - `Policy_Diplomacy_Caleqabiz_LespiaATO`, `MITZ_ScrewedOverLespia`.
  - The evidence chain: `Zille_Discovered_LespianInvolvement(_Rusty)`,
    `Zille_Publicized_LespianInvolvement(_Rusty)`, `Zille_Destroyed_LespianEvidence`.
- **Scenes:** c314 `FPnT_AlvarezCall` (T4), c321 MITZ (T5), c423 `FPnT_AlvarezVisit` (T8), c401/c402
  AN (T8), c450 MITZ board (T9).
- **Lever.** Lespia does not supply Wehlen, but it decides 3 AN votes. Two options:
  - **Blackmail:** vote for condemning Wehlen, or the bombing evidence goes public.
  - **Publish:** that destroys Smolak's whole justification for the protocol. Esquibel: "If this
    comes to light, the other AN states will never agree to Smolak's referendum" (c401 [98]).
    Cost: "At the expense of our relations with Lespia" ([124]).

### 2.5 Morella (PM Alma Saltana): wants to condemn Smolak, fears United Contana

- **Saltana:** "There is nothing I would like more than to condemn Wiktor Smolak and his one-man
  reign of terror on an international stage. But we'd be taking a risk. United Contana is still
  backing Wehlen through all this." (c323 [305]–[306]).
- **The player can push back:**
  - "To hell with United Contana. Partner more closely with Rizia..." ([307]);
  - "You are not in the CSP, Ms. Saltana." ([308]);
  - "Much as I respect Comrade Malenyev, he is in the wrong regarding Wehlen and you know it."
    ([309]).
- **Variables:**
  - `CountryRelation_Morella_*`, `Diplomacy_Morella_NegotiatingPower` (-1).
  - `MITZ_Demand_ZilleDiplomacy`: the AN backing. Morella then always votes against Wehlen in
    c404 [81]/[197]/[747], as does `CountryRelation_Morella_Allied`.
  - `MITZ_Demand_ZilleResistance`: supplies "across the Gulf of Moridia". c327 [1040] gives
    Referendum power +2 to the ATA; [1208] gives Violent resistance +1 to Su Omina. The Turn 8
    brief says: "with the addition of military equipment shipments from Morella, their
    capabilities have been greatly enhanced" (c409 [1948]).
  - `Diplomacy_Morella_ANSpeech_SideWith/_SideAgainst`, `Diplomacy_Morella_Arbitration_Support`.
  - Sabotage leverage: `Morella_Sabotage_Plans_Discovered` (c401 [196]–[233] forces "You will
    vote in Rizia's best interests today").
- **Lever:**
  - Morella's AN vote.
  - Morellan shipping to the resistance.
  - **A design extension, not vanilla:** Morellan ports on the Gulf of Moridia refuse Wehzek
    hulls. The Zille harbour opens onto that gulf.

### 2.6 Rumburg (Queen Beatrice): Rizia's patron, already embargoing Wehlen

- **Lines:**
  - Beatrice: "What will you do if Mr. Smolak fails to relinquish his hold on Zille?" The player
    can reply "If it comes down to it, I'm prepared to fight." and she answers "Then you will need
    Rumburg's help." (c305 [52]/[54]/[58]).
  - Beatrice: "Wiktor Smolak has managed to alienate Wehlen from most of its neighbors." ([178]).
  - Esquibel: "Smolak has made nothing but enemies in the international arena, Rumburg chief
    among them." (c296 [72]).
- **Embargo** (c305; the energy partnership replaces Wehzek oil):

  | Choice | Rizia gives | Rizia gets | Wehlen | Flags |
  |---|---|---|---|---|
  | Partial ([165]) | `Energy_Modifier_RumburgEnergyExport` -1, `Resources_Energy` -2 | `Budget_Modifier_RumburgTrade` +1 | `Diplomacy_Wehlen_NegotiatingPower` -1 | `Situation_Diplomacy_Rumburg_AverageEnergyExport` |
  | Full ([184], [457]): "Cut off Wehlen completely." [-3 Energy, -2 Energy Per Turn, +2 Budget Per Turn] | `Energy_Modifier_RumburgEnergyExport` -2, `Resources_Energy` -3 | `Budget_Modifier_RumburgTrade` +2 | `Diplomacy_Wehlen_NegotiatingPower` -2 | `Situation_Diplomacy_Rumburg_HighEnergyExport` |

  Smolak in Turn 9 confirms the damage: "We are still suffering the effects of the GRACE embargo"
  (c437 [131]).
- **Variables:** `CountryRelation_Rumburg_*`, `Diplomacy_Rumburg_NegotiatingPower` (5),
  `Diplomacy_Rumburg_SellPortOfMonqiz` / `Situation_Diplomacy_Rumburg_SellPortOfMonqiz`,
  `Diplomacy_Wehlen_FundBFF`, `Resources_Military_RumburgianBFFMercenaries`,
  `Resources_Military_RumburgBasicAid`, `Situation_Diplomacy_EnergyCommitment_Rumburg_Failed`.
- **Scenes:**
  - c305 `FPnT_QueenBeatriceVisit` (T3).
  - c389 `FPnT_RumburgMeeting` (T7). "Then you must decide. Zille or Pales?" ([686]); the Port of
    Monqiz sale [+2 Budget, +2 Budget Per Turn] ([1220]).
  - c433 `FPnT_GRACERumburgMeeting` (T9). "it's so important that you keep Wiktor Smolak from
    seizing Zille" ([541]); "Thanks to Rumburg, we have the power to take the region back by
    force" ([546], needs the BFF).
- **Lever:**
  - Rumburg is the obvious source of a naval squadron or minesweepers for the strait, and of AN
    "Nay" cover. The price has to be real: Monqiz port access, the energy debt ("I am still
    expecting Rizia to repay its energy debt", c433 [561]), and quitting Bear Trap and the
    Bludish ban.
  - The embargo flag `Situation_Diplomacy_Rumburg_HighEnergyExport` already shrinks Wehlen's
    purse, so count it as a small supply lever.

### 2.7 Pales, Valgsland and Intermerkopum

- **Pales (Duke Axel):**
  - Votes against Rizia while `CountryRelation_Pales_AtWar`, `_Hostile` or `_Unfriendly`, or
    when the image is bad (c402 [185]). Votes Aye on Smolak's referendum during the war (c404).
  - If `CountryRelation_Pales_Allied` (c393), he supports Rizia.
  - A Zille war during the Pales war gets one more hostile vote.
- **Valgsland (Chancellor Hegel):**
  - CSP member in Merkopa. United Contana's vote follows him.
  - Hostile to the monarchy (c402 [127]–[130]).
  - Already frames Zille against Rizia: "with your violent actions in Zille, Rizia is wearing the
    biggest boot of all" (c451 [65]).
  - Accuses Rizia of backing Su Omina when `Decision_Turn05_ZilleResistanceFunding_BeginSOFund
    and Zille_ViolentResistancePower > 2` (c402 [12]→[132]).
- **Intermerkopum (c439, T9; members Pales, Derdia, Wehlen, Morella):**
  - Smolak joins only with an exclusive oil deal ([258]–[280]). Variables:
    `Diplomacy_Wehlen_IntermerkopumPotential/_Approved`, `Turn09_IntermerkopumIndividual_Wehlen`.
  - "Just don't ask us to go sending our military into Pales." ([682]).
  - If Wehlen is a member (`Diplomacy_Wehlen_IntermerkopumApproved`), attacking it should cost
    `Authority_Modifier_Intermerkopum` and Intermerkopum partners' goodwill.
  - If it is not, Derdia, Morella and Pales can be asked for a regional oil boycott of Wehlen.

### 2.8 Who supplies Zille, according to the data

| Route | Evidence | Who can cut it |
|---|---|---|
| Sea, Kratsna to Zille/Nanta across the strait | "Zille is cut off from the Wehzek mainland" (c326 [294]); BFF fleeing "south towards Zille" across "the strait" (c326 [612], [1548]) | Rizian navy; Bear Trap patrol; Morella/Derdia gulf ports (extension) |
| Air (airship and transports) | Smolak negotiates aboard his airship over Zille (c424); Contanan radar and anti-air (c296 [792]) | Rizian fighters plus anti-air suppression |
| Money and equipment from United Contana | c296 [78], [172], [783]; c323 [306] | Diplomacy with the CSP; AN sanctions |
| Oil revenue | Rumburg embargo (c305); Derdia sales (c437, c439) | Rumburg embargo, Derdia energy substitution, AN sanctions |
| Rizia itself | `Situation_Diplomacy_Wehlen_ExportArms`, `Equipment_Modifier_WehlenArmsExports` (-250), `Turn08_ArmsDealSmolak` | End the arms exports |

---

## 3. The AN machinery

### 3.1 Threat Mitigation Protocol (the "emergency clause")

- **Set:** c316 [815] `Situation_Diplomacy_AN_ThreatMitigationProtocol = true`.
  - Smolak: "This is why I have decided to invoke the AN Threat Mitigation Protocol for Treaty
    Obligations to delay the return of Zille until that risk is neutralized." ([132]).
  - Esquibel: "No nation has invoked this protocol since the signing of the AN Charter nearly a
    century ago." ([196]).
- **Player can contest it:** "The AN Threat Mitigation Protocol can only be invoked under extreme
  circumstances. In my opinion, the Zille incident is not one of them." ([323]).
- **Status by Turn 5:** "President Smolak's invocation of the Threat Mitigation Protocol has been
  challenged, but the AN failed to meet a consensus. Essentially, he is in a position to hold
  Zille hostage until his demands are met." (c326 [90]).
- **Cleared** (set false) on every Zille resolution: c404 [342]/[821], c440 [235], c441, c448,
  c354 [552].
- **Mod reading:** while the protocol is true, Wehlen holds Zille with AN cover. Attacking then
  is aggression against an AN process. Getting it revoked first is the cleanest diplomatic
  prerequisite.

### 3.2 The Turn 8 session (c401 `FPnT_ANSession`)

- Accusers are chosen before the session:
  - `ANSanctionAccusation_Lespia_GlobalImage` if `Situation_Diplomacy_Lespia_SanctionsRizia and
    not Zille_Publicized_LespianInvolvement and Pales_War`.
  - `ANSanctionAccusation_Valgsland_GlobalImage` if `GlobalImage_Bad and
    CountryRelation_Valgsland_Unfriendly`.
- Agenda (Nines, [139]–[146]): "a proposal of sanctions on the Kingdom of Rizia, a vote regarding
  the disputed territory of Heljiland, and a discussion on the region of Zille". Then [140]: "I
  additionally invite all member states to share any other urgent matters you believe deserve our
  collective attention." **This is the vanilla slot for a Rizian motion against Wehlen.**
- The Rusty evidence reveal ([147]–[185]) sets `Zille_Publicized_LespianInvolvement_Rusty`, plus
  `News_Turn08_G_LespiaEvidenceReleased` and `Journal_Turn08_AN_LespiaEvidenceReveal`.
- Side deals: Morella's sabotage leverage ([196]–[233]); the Saltana tip-off ([240]–[290]).

### 3.3 Sanctions vote (c402 `Sanctions`): the template to reuse

- **Counter:** `RiziaDLCIsolated.ANSanctionAccusation_Vote` (starts at 0). +1 means Aye (sanction
  Rizia); -1 means Nay.
- **Speeches.** Alvarez ([42]–[101]) and Hegel ([111]–[159]) build the charge sheet from:
  - `War_Used_ExperimentalWeapons` (>0, >1), `Country_WarRefugees > 2`,
    `Decision_Turn07_War_Both_RefugeeStory_TransportRefugeesToMorella`,
    `Decision_Turn08_War_Both_PalesResistance_ScorchedEarthPolicy`,
    `Decision_Turn07_War_Both_IndependentJournalism_DenyAccess`.
  - `Country_QuelledProtests > 5`, `Vendonesam_SupportLevel > 2`.
  - Su Omina in Zille (`Decision_Turn05_ZilleResistanceFunding_BeginSOFund and
    Zille_ViolentResistancePower > 2`).
  - The Bludish ban or Bear Trap, `Situation_Law_Media_Censored`,
    `Decision_Turn06_MediaCampaign_Massive`, `HouseOfDelegates_Abolished`.

**Voters:**

| Delegate | Aye (+1) if | Nay (-1) if |
|---|---|---|
| Jorga Azmal (Derdia) | Derdia Hostile or Unfriendly, or `GlobalImage_Bad` ([36]) | Derdia Friendly or Allied with a non-bad image |
| Abstain rule for Derdia ([39]) | Neutral without the Rusty reveal; Friendly with a bad image; Unfriendly with the Rusty reveal | |
| Alvarez (Lespia) | Lespia Hostile or Unfriendly, or bad image, or `Situation_Diplomacy_Lespia_SanctionsRizia` ([300]) | Caleqabiz leased to ATO or Lespian pipeline access ([307]) |
| Anton Rayne (Sordland) | Sordland Hostile or Unfriendly, or bad image | otherwise; also sets `Sordland_RiziaSanction_Aye` ([298]) |
| Axel (Pales) | Pales Hostile, Unfriendly or **AtWar**, or bad image ([185]) | Friendly or Allied |
| Saltana (Morella) | Morella Hostile or Unfriendly, or bad image | otherwise |
| Beatrice (Rumburg) | Rumburg Hostile or Unfriendly **and** bad image | `Rumburg_Allied`, or Friendly with a non-bad image ([303]) |
| Hegel (Valgsland) | Valgsland Hostile or Unfriendly, or bad image | otherwise |
| Smolak (Wehlen) | Wehlen Hostile or Unfriendly, or bad image ([223]) | otherwise |
| Walker (Arcasia), Van Hoorten (Agnolia) | copy Lespia's rule ([278]–[292]) | Nay if `Policy_Diplomacy_Caleqabiz_LespiaATO` |
| Malenyev (United Contana) | copies Valgsland's rule ([293]–[297]) | |

**Verdict** ([231]–[242]):

| Vote | Result | Effects |
|---|---|---|
| > 2 | heavy | `Situation_Diplomacy_ANSanctions_Heavy`, `Budget_Modifier_ANSanctions = -6`, `Energy_Modifier_ANSanctions = -5`, `UnlockAchievement("Rizia_ANSanctions")` |
| > 0 | weak | `Situation_Diplomacy_ANSanctions_Weak`, `Budget_Modifier_ANSanctions = -3`, `Energy_Modifier_ANSanctions = -2` |
| otherwise | none | `Journal_Turn08_ANSession_NoSanctions` |

The modifiers are **set**, not incremented. A second set of sanctions for Zille must either
raise them (for example -3 → -6) or use a new modifier. Whether the engine sums arbitrarily named
new `Budget_Modifier_*` variables is UNVERIFIED; reuse the existing names to be safe.

### 3.4 Zille referendum vote (c404 `Zille Territory Issue`): the AN already moves Zille

- **Counter:** `RiziaDLCIsolated.ANZilleReferendum_Vote`. Aye means allow Smolak's referendum.
- **Votes:**
  - Smolak always Aye; Beatrice always Nay.
  - Morella Nay if `MITZ_Demand_ZilleDiplomacy` or `CountryRelation_Morella_Allied`.
  - Arcasia and Agnolia go Nay when `Zille_Publicized_LespianInvolvement_Rusty`.
  - The others follow the relation and image pattern above.
- **Result:**
  - Vote ≤ 0 → `ZilleReferendum_StoppedByAN` **and `ZilleReturn_Success = true`**, with "AN
    peacekeepers" ([423]: "Peacekeeping troops shall be sent to ensure the smooth transition of
    Zille to Rizia.").
  - The player asks for them at [207]: "I ask for your votes to send Alliance of Nations
    peacekeepers to Zille to stop this illegal referendum."

### 3.5 Arbitration support (Turn 6, c361 / c364): a "lobbying" precedent

`Arbitration_TotalSupport` (starts at 1) collects lobbying points:
- Morella +1 (`Diplomacy_Morella_Arbitration_Support`);
- Derdia +1;
- Wehlen +1;
- Rumburg +2 if Beatrice appears in person, +1 if she sends a statement;
- -1 for a blockade.

At ≥ 5 Rizia wins (`Diplomacy_Pales_DiplomaticArbitration_WonHalf`).

### 3.6 What the AN does to the aggressor in the Pales war (models Rizia's cost)

- **`War_Pales_ANSupport`** (c357) raises enemy strength:
  - +5 early invasion;
  - +2/+3 naval aggression;
  - +2 killed border soldiers;
  - +1/-1 for rescue choices;
  - -1 with `War_Media_Geopolitico_Support`.
  - Thresholds: below 3 nothing; below 5 gives `War_Pales_Strength` +1; 5 or more gives +2.
- **Sanctions** -3/-2 or -6/-5 per turn (3.3); Lespian sanctions -2 Budget per turn.

---

## 4. Costs available to charge the player

Starting national resources: `Resources_Budget` 8, `Resources_Authority` 6,
`Resources_Energy` 11.

**How often dialogue writes each change** (counted across all Rizia conversations):

| Variable | Typical changes seen (count × value) | Notes |
|---|---|---|
| `Resources_Budget` | -1 ×59, -2 ×24, -3 ×22, -4 ×13, -5 ×16, -6 ×4, -8 ×2, -10 ×4 | The workhorse cost. -1/-2 for a gesture, -3 to -5 for a deal, -6+ for epic projects. |
| `Resources_Authority` | +1 ×21, +2 ×17; -1 ×12, -2 ×7, -3 ×2, -4 ×1, -5 ×2, -6 ×1 | Gains are common (blockade +1/+2). Costs of -2 to -4 sting: the blasphemy laws were -4 (c377 [1027]); the ATA in both regions -2. |
| `Resources_Energy` | -1, -2, -3 ×3, -8, -10; +2 ×4 | Rare and therefore heavy. The Rumburg embargo was -3 plus -2/turn. |
| `Resources_Military_Equipment` | ±250, ±500, ±1000, +1500, -5000 | Su Omina -250/-500; Smolak arms -1000. |
| `Resources_Military_Manpower` | ±100, ±250, ±500 | Corridor guard -250. |
| `Country_PublicOpinion` (-3) | ±1, ±2 (most common -2), ±3, ±5, -10 | "Baseline stability indicator". |
| `Diplomacy_<X>_NegotiatingPower` | ±1 (most), ±2, ±3, -4, -5 | Relation currency. |
| `Country_HouseSazon` (-2), `_HouseAzaro` (-1), `_HouseToras` (2) | ±1, rarely written | House loyalty. Sazon wants Zille for Brenas. |
| `PersonalValue_*` | ±1 | Pacifist, Tyrant, Malenyevist and so on: trait drift. |

**Per-turn modifiers** (the "costly over time" tool):
- Sanctions: `Budget_Modifier_ANSanctions` (-3/-6), `Energy_Modifier_ANSanctions` (-2/-5),
  `Budget_Modifier_LespiaSanctions` (-2).
- Standing military costs: `Budget_Modifier_WarExpenditures` (-3), `Budget_Modifier_MilitaryExpenditures`
  (-2), `Energy_Modifier_ArmyRefuelCosts` (-1), `Energy_Modifier_NavyRefuelCosts` (-2),
  `Energy_Modifier_AirforceRefuelCosts` (-1).
- **Lost Wehzek trade on war:** `Budget_Modifier_WehlenTrade` (starts +1; c326 deals move it
  -2 to +1), `Energy_Modifier_WehlenOilImport` (+1 to +3 from c326 [3279]–[3293]),
  `Energy_Modifier_WehlenGasExport` (-2), `Equipment_Modifier_WehlenArmsExports` (-250).
- Territory: `Authority_Modifier_TerritorialReclaimation` (+1/+2 on return) and
  `Authority_Modifier_TerritorialIntegrityLost` (-1 on loss).

**Unit losses:** `Army_Unit_<X>_Total -= n; Army_Unit_<X>_Dead += n`. Vanilla blockades cost 1
to 3 ships. The skipped-mode war costs 1/5 to 1/2 of every unit type (c410 [215]–[217]).

**Other costs available in the data:**
- **Relations:** write a `CountryRelation_<X>_*` tier, or move `Diplomacy_<X>_NegotiatingPower`.
- **AN and image:** `Situation_Diplomacy_GlobalImage_Bad/_Mixed/_Good` (engine-derived; Mixed
  at start), and the accusation counters.
- **War support:** `War_Pales_RizianWarSupport` (engine, unused in dialogue),
  `Turn06_WarSpeech_Impact`. A Zille copy should be `ZilleWar.War_Zille_RizianWarSupport`.

---

## 5. Wehlen's military in Zille (all the data has)

- **Friendship Day:**
  - "a trio of gleaming tanks" (c298 [67]); "A dozen uniformed soldiers" ([71]);
  - "armed guards standing on every level" ([86]);
  - a tank fires gold smoke ([134]);
  - "A Wiktor Smolak special." / "We can thank his Eastern benefactors" ([138]–[139]).
- **Fortified border:** a concrete barrier with barbed wire (c298; see `rizia-story-research.md`).
- **Wehlen's own coast:** mined approaches, "layered naval batteries", Contanan radar and
  anti-air (c296 [783], [791], [792]). This describes Wehlen's mainland coast, not Zille. Using it
  for the Kratsna end of the strait is fair.
- **Zille is isolated:** "cut off from the Wehzek mainland" (c326 [294]).
- **Smolak's forces:**
  - "Wehlen forces" in Zille (c424 [160]); "Wehzek troops boast a legendary reputation" (c434
    [108]);
  - Operation Moonlight Sonata amphibious landing (c354 [58]); "Have you seen our arsenal?"
    (c437 [152]).
- **Behaviour under pressure:**
  - "The Wehzek troops fell back as soon as we entered."
  - On failure: "As the Wehzek troops were falling back, they intentionally gave all of their
    equipment to the civilians of Zille." (c441 [84]/[86]).
  - **Canon precedent for "a starved garrison turns into militia".**
- **Police and occupation measures** (report flags, set outside the DB):
  - `Reports_Turn03_Zille_PostBombingArrests`;
  - `Reports_Turn04_Zille_TravelRestrictions` / `_LargeGatheringBan`;
  - `Reports_Turn05_Zille_WehzekCitizensPriviliged`;
  - `Reports_Turn06_Zille_WehlenEncouragesMigration`;
  - `Reports_Turn09_Nanta_DiscardedBallotsDiscovered`.
- **No numbers anywhere** for garrison size, ships, aircraft or batteries.
- **Current mod garrison** (`ZilleWar/ZilleOperation.cs` `Garrison()`):
  - infantry on every border-wall tile;
  - `GarrisonTanks = 3` (by Zille, Nanta and Auremina);
  - `SpreadUnits = 8`, of which `SpreadInfantry = 1` and the rest militia.
  - `data.enemyTokenOverrides` exists and is empty. It is a conditional token list, the same
    mechanism vanilla uses for `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_*`.

---

## 6. Proposed design: the supply war

### 6.1 One meter, like `War_Pales_Strength`

The mod copies the vanilla pattern: a hidden integer computed in the war briefing, then threshold
overrides.

**`ZilleWar.War_Zille_Strength`**
- **Baseline at the start of the briefing: 8**, two tiers above what Pales gets without Lespia:
  - +4 for Contanan backing;
  - +2 for the protocol's legitimacy and open sea lanes;
  - +2 for the oil purse.
- Each lever below subtracts from it.
- **Floor: 3.** The garrison can never be starved below "Weakened"; that is the "not completely"
  part.

| `War_Zille_Strength` | Deployment override (new `ZilleWar.Setup_*` condition flags read by `enemyTokenOverrides`) | Garrison vs today's baseline |
|---|---|---|
| ≥ 8 | `ZilleWar.Setup_Garrison_Strong` | baseline plus 2 infantry (reinforced by sea), `ExtraAirDefence` and `ExtraNavalDefence` tile overrides on Zille and Nanta, enemy air strikes 3 |
| 6–7 | `ZilleWar.Setup_Garrison_Default` | today's garrison (wall infantry, 3 tanks, 1 + 7 spread), enemy air strikes 2 |
| 4–5 | `ZilleWar.Setup_Garrison_Strained` | spread units 8 → 5, tanks 3 → 2, enemy air strikes 1 |
| 3 (floor) | `ZilleWar.Setup_Garrison_Starved` | spread units 8 → 4 (all militia), tanks 3 → 1, half the wall infantry becomes militia, no anti-air override, enemy air strikes 0; plus 1–2 extra militia in Zille city and Nanta ("gave all of their equipment to the civilians", c441 [86]) |

The Starved tier comes to roughly a 35–45% smaller and much weaker garrison. Zille city and Nanta
always keep infantry, so the war is still a war.

- **Enemy air strikes** mirror `Resources_Military_EnemyAirStrikeCount` (vanilla 1; 5 with direct
  Lespian support).
- **Tile-defence overrides** copy `RiziaDLCSetup.War_TileOverride_Pales_ExtraAirDefence/_ExtraNavalDefence`
  as `ZilleWar.TileOverride_Zille_ExtraAirDefence/_ExtraNavalDefence`, through the engine's
  `WarTileAntiAirChange` and `WarTileCoastalArtilleryChange`.

**Mirror meter for Rizia's exposure: `ZilleWar.War_Zille_ANSupport`.** This is AN sympathy for
Wehlen, like `War_Pales_ANSupport`.
- **Raised by:**
  - attacking while `Situation_Diplomacy_AN_ThreatMitigationProtocol` is true: +3;
  - blockading international waters: +1;
  - bombing populated Zille: +2;
  - Su Omina killings: +1;
  - `Diplomacy_Wehlen_Scapegoats` exposed: +1.
- **Lowered by:**
  - the protocol revoked by vote: -3;
  - Lespia evidence public: -2;
  - repression dossier: -1;
  - `War_Media_Geopolitico_Support`: -1.
- **Feeds:** the post-war AN sanctions vote against Rizia (a copy of c402). Thresholds: ≥ 5 starts
  the vote with +2, ≥ 3 with +1.
- **Why this matters:** making Wehlen look bad before the war is cheaper than the sanctions after
  it.

### 6.2 Lever A: naval blockade of the Strait of Kreleck

- **Where:**
  - Plan it in Turn 8 `SnO_SecurityBriefAndZille` (c409), at the contingency hub beside "Let's
    bomb the bastards" ([2116]–[2138]). The Turn 9 or war briefing resolves it, copying c357
    [301]–[303] and [306]–[366].
  - Lucita, Taddeus or Titus present it.
  - New order line, in the style of c357 [302]: "Furthermore, we will blockade the Strait of
    Kreleck. Not one Wehzek hull reaches Zille."
- **Prerequisites** (section 1.5):

  | Order | Gate | Garrison effect (`War_Zille_Strength`) |
  |---|---|---|
  | Submarine patrol | `Ship + Submarine >= 4` and `Submarine >= 1` | -1 |
  | Partial blockade | `Ship + Submarine >= 6` and `Ship >= 3` | -2 |
  | Full blockade | `Ship + Submarine >= 8`, `Ship >= 4`, `Submarine >= 2`, and a mine flag (`Policy_RD_Order_ExpandFortAlesNavalBase_Completed`, `Policy_RD_Economy_MonqizPortShipyardExpansion_Completed` or `ZilleWar.Navy_Minesweepers`) | -3, plus `ZilleWar.Supply_StraitBlockaded`: no mid-operation reinforcements |

- **Bear Trap twist.** If `Situation_Diplomacy_Wehlen_Terrorism_AntiBFF` is still true, the 2
  detached ships are already in the strait (c409 [872]).
  - Option 1, turn them: they count +2 toward the gate and give a surprise bonus (-1 extra
    Strength).
  - Cost of turning them: `Diplomacy_Wehlen_PromisesBroken_AntiBFF = true`,
    `ZilleWar.War_Zille_ANSupport +1` (betrayal of a joint operation), and Derdia and the BFF
    approve.
  - Option 2, recall them: `Army_Unit_Ship_Total +2`, as c381 [99] does.

**Cost (vanilla-scale):**

| Order | On success | On failure | Fuel | Also |
|---|---|---|---|---|
| Patrol | 0–1 ship (`Army_Unit_Ship_Total/-Dead` ±1) | 1 sub (`Army_Unit_Submarine_*`) | | |
| Partial | 1 ship (2 without air cover) | 3 ships | `Energy_Modifier_NavyRefuelCosts -1` for the war | `ZilleWar.War_Zille_ANSupport +1` |
| Full | 2 ships | 3 ships and the flagship at risk (`Army_Unit_Flagship_Total = 0` and `Situation_Military_LostPrideOfNavy` if `Army_Unit_Ship_Total < 4` after losses) | `Energy_Modifier_NavyRefuelCosts -1` | `ZilleWar.War_Zille_ANSupport +1`; Authority +1 if it succeeds (as the Pales blockade gave) |

On top of that, the investment to reach the gate: dockyard decrees, ship and submarine purchases,
and the Monqiz shipyard. Each is a Budget cost of -2 to -5 in decree terms.

### 6.3 Lever B: air campaign against the air bridge and the harbours

- **Where:**
  - The same c409 hub, with Carlos Robles Azaro as RRAF commander. He briefs air defences in c357
    [453]–[455].
  - War briefing order: "Strike Nanta harbour and the airfield at Zille".
- **Prerequisites:**
  - Air cover: `Army_Unit_Fighter_Total >= 3`, or `Focus_MilitaryBranch_Airforce` and
    `Army_Unit_Fighter_Total >= 2`.
  - Harbour raids: `Army_Unit_Bomber_Total >= 3`.
  - Contanan anti-air must be dealt with (c296 [792]). Any one of:
    - `Resources_Military_DerdianMissiles`;
    - `ZilleWar.UC_SupportWithdrawn` (Lever C);
    - `Policy_RD_Order_BuildMaveNortusIntelHub_Completed`, for targeting.
  - Without that, bomber losses double.
- **Effect:**
  - Air-bridge interdiction (fighters): -1 Strength, and enemy air strikes -1.
  - Harbour raids (bombers): -1 Strength, and the Zille/Nanta harbour tiles start the operation
    "damaged" (no unit healing for 2 turns). This matches the campaign doc's "Air raids on Nanta
    harbor: Wehlen units start weakened".
  - Cap: -2 total from air.
- **Cost:**
  - Losses: `Army_Unit_Bomber_Total -1` (or -2 against intact Contanan anti-air);
    `Army_Unit_Fighter_Total -1` on failure.
  - Fuel: `Energy_Modifier_AirforceRefuelCosts -1` for the war.
  - Collateral when bombing the Zille harbour, a city of "millions": `Country_WarRefugees +1`,
    `ZilleWar.War_Zille_ANSupport +2`, `Zille_ReferendumPower -1` (Zilleans turn against the
    "liberator").
  - Bombing only Nanta's port and the airfield costs `War_Zille_ANSupport +1` and nothing on
    referendum power.

### 6.4 Lever C: diplomatic isolation of named backers

Each item is a separate deal with its own price. Together they can take off -3 at most.

| Backer | Where | Ask | Gate | Rizia pays | Effect |
|---|---|---|---|---|---|
| **United Contana / CSP** via Hegel | c451 `Hegel Side Convo` (T9) or a new Turn 8 side talk; Saltana as go-between (c323 [305]–[309]) | "Withdraw Contanan support from Smolak's occupation" | not `Policy_Diplomacy_Caleqabiz_LespiaATO` (c451 [125]); not (`GlobalImage_Bad` and `CountryRelation_Valgsland_Unfriendly`) ([117]) | `Policy_Diplomacy_Caleqabiz_ValgslandCSP` (the CSP base, without the vanilla +4 Budget; or keep it and add the costs that follow), `Diplomacy_Rumburg_NegotiatingPower -3`, Rumburg relation one tier down, `Resources_Authority -2` (Taddeus and Sal: "Once you enter from that door, there is no turning back", c589 [59]), `PersonalValue_Malenyevist +1` | `ZilleWar.UC_SupportWithdrawn`: -2 Strength, no anti-air override, Malenyev and Hegel abstain on the AN motion |
| **Rumburg** (deepen) | c389 / c433 | Strait squadron or minesweepers; keep the GRACE oil embargo | `CountryRelation_Rumburg_Friendly/_Allied` | Quit Bear Trap and the Bludish ban (`PromisesBroken_*`); `Diplomacy_Rumburg_SellPortOfMonqiz` or repay the energy debt (`Resources_Energy -3`); if `Pales_War`, Beatrice's "Zille or Pales?" (c389 [686]): Rumburg's Pales aid is forfeited | sets the mine flag for Lever A; +1 to the naval sum; `Situation_Diplomacy_Rumburg_HighEnergyExport` gives -1 Strength (already true in many runs) |
| **Derdia** | c377 / c381 / c437 | Stop buying Wehzek oil; missiles against Contanan anti-air | `Situation_Diplomacy_Wehlen_Terrorism_AntiBFF == false` (Jorga, c437 [36]); `Diplomacy_Derdia_NegotiatingPower >= 3` | Energy substitution: `Policy_Diplomacy_TradeDeal_Derdia` with `Energy_Modifier_DerdiaEnergyExport -1`; missiles -2 Budget (c381 [105]); blasphemy-law concession -4 Authority (c377 [1027]) if relations are poor | -1 Strength (Wehlen's oil purse); unlocks Lever B's anti-air suppression; Derdia votes Aye on the AN motion |
| **Morella** | c323 / c327 (T5), c401 [240]+ | AN condemnation; close Gulf of Moridia ports to Wehzek ships; arms to the resistance | `MITZ_Demand_ZilleDiplomacy` or `CountryRelation_Morella_Friendly/_Allied` | the MITZ concessions already demanded (energy at a set price); or use `Morella_Sabotage_Plans_Discovered` as blackmail (relation falls to Unfriendly afterwards) | Morella Aye; with `MITZ_Demand_ZilleResistance`, the SO lever is stronger; port denial gives -1 Strength only if Lever A has at least a patrol |
| **Lespia** | c423 `FPnT_AlvarezVisit` (T8), c401 | AN vote against Wehlen | `Zille_Discovered_LespianInvolvement(_Rusty)` and not `Zille_Destroyed_LespianEvidence` | Blackmail: Lespia relation one tier down, `Situation_Diplomacy_Lespia_SanctionsRizia` stays, `War_Pales_LespianSupport` 2 if at war. Or publish (below). | Lespia, Arcasia and Agnolia Aye (3 votes) |
| **Sordland** | c401 [61] (Wisci), a new note | Stay out | `CountryRelation_Sordland_Friendly` | `Policy_Diplomacy_Sordland_BondPurchase`-scale deal, -2 Budget | Rayne abstains instead of voting Nay |
| **Stop arming Wehlen** (Rizia itself) | c326 / c424 consequence, or a decree | end `Situation_Diplomacy_Wehlen_ExportArms` | none | lose the trade line: `Budget_Modifier_WehlenTrade -1` | -1 Strength if Rizia had been exporting arms or `Turn08_ArmsDealSmolak` |

### 6.5 Lever D: AN condemnation of Wehlen and revocation of the protocol

- **Where:** Turn 8 `FPnT_ANSession` (c401), using Nines' open call at [140] ("any other urgent
  matters"). It runs after the Rusty reveal ([147]–[185]) and before or alongside c404.
- **New conversation:** `ZilleWar.AN_WehlenMotion`, built by copying c402's vote scaffolding.
  - Counter: `ZilleWar.ANWehlenMotion_Vote`. +1 means Aye (condemn Wehlen and revoke the
    protocol); -1 means Nay.
- **Charge sheet** (each item is a gated line, like Alvarez's in c402):
  - **Bombing:** "Lespia's act of terrorism killed civilians and directly impeded the return of
    the Zille region" (c401 [172]). Needs `Zille_Publicized_LespianInvolvement(_Rusty)`. The
    protocol rested on the bombing risk (c316 [132]), so with the bomber exposed as Lespia the
    justification collapses. **This is the core charge.**
  - **Occupation:** "Smolak's iron grip on Zille, enforced by dictatorial command rather than any
    real respect for international law or the rights of the local population" (the Rizian
    press, read out by Smolak, c326 [400]). Saltana's "one-man reign of terror" (c323 [305]).
  - **Wehzek privilege and repression:** `Reports_Turn05_Zille_WehzekCitizensPriviliged`,
    `Reports_Turn04_Zille_TravelRestrictions` / `_LargeGatheringBan`,
    `Reports_Turn03_Zille_PostBombingArrests`, `Reports_Turn06_Zille_WehlenEncouragesMigration`.
  - **Persecution of Bluds:** "the persecution of the Bludish people" (Jorga, c380 [80]). Only
    if Rizia quit Bear Trap; otherwise Hegel throws it back (c402 [149]).
  - **Treaty:** "Wehlen's actions in Zille should be considered an illegal occupation of Rizian
    territory" (Alvarez, c316 [221]).
- **Charges that backfire** (Smolak's rebuttal adds Nay pressure, like Hegel's SO line c402
  [132]):
  - `Decision_Turn05_ZilleResistanceFunding_BeginSOFund and Zille_ViolentResistancePower > 2`,
    with `StrongArmedResistance` ("We found Rizian weapons in the hands of these terrorists!",
    c404 [121]–[123]);
  - `Diplomacy_Wehlen_Scapegoats` (vanilla already scores publication after scapegoats at -5,
    c424);
  - `Rico_FrameForWehlenBombing`, `Situation_Law_Media_Censored`, `Country_QuelledProtests > 5`.
- **Proposed voter rules:**

  | Delegate | Aye if | Nay if | otherwise |
  |---|---|---|---|
  | Smolak | never | always | |
  | Malenyev | | `ZilleWar.UC_SupportWithdrawn == false` | abstain |
  | Hegel | `Policy_Diplomacy_Caleqabiz_ValgslandCSP` | Valgsland Unfriendly or Hostile, or `GlobalImage_Bad` | abstain |
  | Beatrice | Rumburg Friendly or Allied | Rumburg Hostile | abstain |
  | Alvarez | evidence held as blackmail, or Lespia Neutral or better | Lespia Hostile and evidence not held | abstain if shamed by publication |
  | Walker, Van Hoorten | Lespia Aye, or `Zille_Publicized_LespianInvolvement_Rusty` | Lespia Nay without the reveal | abstain |
  | Saltana | `MITZ_Demand_ZilleDiplomacy` or Morella Friendly or Allied | Morella Hostile | abstain (fears United Contana, c323 [306]) |
  | Jorga | Derdia Neutral or better and not `Situation_Diplomacy_Wehlen_Terrorism_AntiBFF` | Derdia Hostile | abstain |
  | Axel | Pales Allied | `CountryRelation_Pales_AtWar` or Hostile | abstain |
  | Rayne | Sordland Friendly or Allied and no `BaseGame.Policy_Diplomacy_Wehlen_Trade/_JointOperation` | the Wehlen ties above | abstain |

  Global modifiers:
  - `Situation_Diplomacy_GlobalImage_Bad` turns every abstention into a Nay, as in vanilla.
  - `_Good` adds +1.
- **Verdict** (mirrors c402's thresholds):

  | Vote | Result | Sets | Garrison effect | Rizia's AN exposure |
  |---|---|---|---|---|
  | > 2 | Heavy: condemnation, protocol revoked, sanctions on Wehlen | `Situation_Diplomacy_AN_ThreatMitigationProtocol = false`, `ZilleWar.AN_WehlenSanctions_Heavy` | -2 Strength (neutral shipping stops carrying for Wehlen; the purse shrinks) | `War_Zille_ANSupport -3` (the protocol revoked); the blockade counts as enforcing AN sanctions, so its +1 is waived |
  | > 0 | Weak: protocol revoked, no sanctions | `ThreatMitigationProtocol = false`, `ZilleWar.AN_WehlenSanctions_Weak` | -1 | `War_Zille_ANSupport -3` |
  | ≤ 0 | Fails | `ZilleWar.AN_WehlenMotion_Failed` | none | `War_Zille_ANSupport +1` (Smolak's "they tried to railroad us" line) |

- **Cost (lobbying, vanilla-scale):**
  - Laurento's campaign: `Resources_Budget -2`. A Massive media campaign is -3, with Hegel's
    "bribed participants" risk (c402 [152]).
  - `Resources_Authority -1` if Romus interrupts or the motion fails.
  - Publishing the evidence: Lespia relation to Hostile; `Situation_Diplomacy_Lespia_SanctionsRizia`
    and `Budget_Modifier_LespiaSanctions -1` (to -3), as c450 does.
  - Each delegate deal from 6.4 costs what that row says.
  - Note: a heavy result also ends any vanilla path where the AN stops the referendum and hands
    back Zille (c404 ≤ 0 → `ZilleReturn_Success`). If that happens, there is no war. Treat that
    as the "won without firing a shot" outcome.

### 6.6 Lever E: Su Omina and Derdian sabotage of the supply chain

- **Where:**
  - Turn 8 c409 contingency menu ([2327]), using its `StartSO` / `IncreaseSO` executors
    ([2422]/[2426]).
  - Then a war-briefing order: "Activate Su Omina against the Zille–Nanta railway and the
    harbour".
  - Hosts: Rico Toras (c307 `A_VisitToIza` precedent) or Lucita.
- **Prerequisites:**
  - `Situation_Diplomacy_Zille_SuOminaActive`, plus
    `Situation_Diplomacy_Zille_GrowingArmedResistance` or `_StrongArmedResistance` (fed by
    `Zille_ViolentResistancePower`).
  - Stronger with `Resources_Military_DerdianMercenaries_Zille`, `MITZ_Demand_ZilleResistance`
    (Morellan arms) or `Diplomacy_Wehlen_FundBFF`.
- **Effect:**
  - Rail sabotage cuts the mod's inland supply line Zille–Nanta for Operation 1. The canon report
    is `Reports_Turn06_Zille_SORailwayAttack`. Effect: -1 Strength.
  - Harbour explosion: canon `Reports_Turn07_Zille_SOHarborExplosion`. -1 more only if Strong
    armed resistance, or if the Derdians join. Cap -2.
  - With `_StrongArmedResistance`, the canon interference rule (c441 [289]: Strong plus SO
    succeeds even during the Pales war) justifies letting this lever work while `Pales_War` is
    true. The navy and air levers should be harder during `Pales_War`, because ships and planes
    are committed. Titus: "we have our hands full with Pales" (c409 [2138]).
- **Cost:**
  - `Resources_Budget -1/-2` and `Resources_Military_Equipment -250/-500` (the vanilla SO
    executors).
  - `ZilleWar.War_Zille_ANSupport +1`, and it arms Hegel's and Smolak's Su Omina charge at the AN.
  - `Zille_ReferendumPower -1` if civilians die.
  - Politics: Manus (RPP) objects that Su Omina undermines a peaceful transfer; `Country_HouseSazon +1`
    (Brenas wants Zille back by any means); Daria (RNC): "by any means necessary" (c386 [19]).
  - Smolak's reprisal: a Turn-10-style crackdown report (`Reports_*`), and the canon "gave all of
    their equipment to the civilians" turns into +1 militia in the Starved tier.

### 6.7 How the levers add up (example runs)

| Run | Levers | Strength | Tier |
|---|---|---|---|
| Vanilla-ish reign, no preparation | none | 8 | Strong garrison; the war is a slog |
| Navy-focus reign | Full blockade (-3), Rumburg minesweepers (mine flag), Derdia oil substitution (-1) | 4 | Strained. Cost: about 2 ships lost, -2 Energy over the war, several Budget hits, Rumburg concessions |
| Diplomatic reign | Lespia evidence plus a Heavy AN result (-2), the CSP pays off over Caleqabiz (-2), stop arming Wehlen (-1) | 3 | Starved. Cost: CSP base, Rumburg and Lespia relations, Authority -2 |
| Everything | all levers | 3 (floor) | Starved, never less. Wehzek civilians armed as militia |

### 6.8 Implementation notes

- **Order of play.** Preparation happens in Turns 5–8. Resolution happens in the war briefing,
  as a hidden pass like c357 [508] → [336] → [329] → [311] → [355] → [486]:
  1. compute `War_Zille_Strength` and `War_Zille_ANSupport`;
  2. resolve naval and air orders against the unit gates, applying losses;
  3. set `ZilleWar.Setup_*` flags;
  4. call `EnableToken`.
- **Garrison overrides.** Implement them in `ZilleOperation.cs` through `data.enemyTokenOverrides`
  with condition strings such as `ZilleWar.Setup_Garrison_Starved == true`. Keep `Garrison()` as
  the Default tier. Air strikes go through the war data's enemy air-strike field, mirroring
  `Resources_Military_EnemyAirStrikeCount`.
- **Delegated mode.** Snapshot the units as `War_Control_Skipped_*` does, but under the Zille
  namespace, so the naval losses from Lever A are applied before the snapshot.
- **Two wars at once.** If `Pales_War` is true at the Zille war briefing:
  - require the blockade gates on units not deployed to Pales;
  - Lever A's gates add +2 to every naval threshold;
  - Rumburg's price includes "Zille or Pales?";
  - Pales votes Nay at the AN.
- **Exclusion.** Never offer the levers if `War_WehlenJoined` or
  `ZilleReferendum_SmolakOffer_Accepted` (Zille is traded away; see `pales-war-flow.md` 6.2).

---

## 7. UNVERIFIED and open questions

- The exact cut points for `Situation_Diplomacy_PowerProjection_*` and
  `Situation_Military_NavalInterdictionCapacity_*` (computed in code).
- Whether the `CountryRelation_<X>_*` tiers are recomputed by the engine from
  `Diplomacy_<X>_NegotiatingPower` each turn, which would overwrite dialogue writes.
- Whether the engine sums new `Budget_Modifier_*` / `Energy_Modifier_*` names, or only the known
  list (affects stacking Zille sanctions on top of Pales sanctions).
- Geography of Wehzek supply. That the Zille harbour's sea lane passes into the Gulf of Moridia,
  where Morella and Derdia could deny ports, is a design inference from the map, not dialogue.
- Wehlen's order of battle: no numbers exist. The tier table in 6.1 is a design choice scaled to
  the mod's current garrison.
