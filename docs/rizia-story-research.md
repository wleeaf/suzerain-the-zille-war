# Suzerain: Kingdom of Rizia — story research for the Zille War mod

Researched 2026-11-21 against the installed game (Suzerain 3.1.0.1.175, Steam/Proton) plus the
official codex, the fan wiki and community sources.

## Verdict

The mod fits the canon better than the brief assumes. Vanilla Rizia does **not** keep Zille
purely diplomatic. The game already lets the king (1) ask his War Councilor about military
options against Wehlen in Turn 2, where the canon answer is "outside of Zille, very few that are
realistic" and "Zille ... lies on our mainland. There, we can exert intelligence and limited
military pressure"; (2) send the Rizian Armed Forces into Zille during the Turn 9 referendum,
a text-only operation that succeeds or fails on power projection and whether Rizia is already
at war with Pales (achievement `Rizia_FreeZille`); and (3) trade Zille to Wehlen for Wehzek
troops in the Pales war (Turn 9 phone call). So a playable Zille war is an **expansion of an
existing branch**, not an invention. The cleanest canon hook is the Turn 9 "interfere with the
Rizian Armed Forces" choice, or a failed Turn 8 negotiation when Rizia's power projection is
Strong/Overwhelming. The canon also sets hard limits the mod should respect: the war stays a
**limited land war on the Rizian mainland** (no crossing the Strait of Kreleck into Wehlen
proper: mined sea approaches, layered coastal batteries, Contanan radar and anti-air), Wehzek
troops are canonically willing to fall back and arm Zillean civilians, and the international
fallout runs through the Alliance of Nations (Threat Mitigation Protocol, peacekeepers,
sanctions votes) and United Contana's backing of Smolak. Torpor itself has polled players on
"a Zille-related Wehlen war expansion" (official survey), so demand is real but unmet.

**Sources, by weight.** The strongest source here is the game's own dialogue database
(articy:draft export, 129 MB, extracted locally from
`database_assets_all_*.bundle`; conversation paths and variable names are quoted so they can be
found again). Second is the official Torpor codex (codex.torporgames.com). The fan wiki
(suzerain.fandom.com) is labelled (fan wiki) and is incomplete for Rizia (its "Turn" page has no
Rizia content; its Rizia endings section is marked WIP). "CONFIRMED (game data)" below means I
read the line or the variable in the shipped dialogue database.

---

## Tables

### A. Rizia turn structure (game data)

Conversation paths in the database run `Rizia/Prologue`, `Rizia/Turn 1` … `Rizia/Turn 10`,
`Rizia/Turn 11` (ending scenes) and `Rizia/Epilogue` plus ending branches (`Civil War`,
`Continued Reign`, `Exile`, `Immediate Death`, `Peaceful Power Transition`, `Prison`,
`Survived`). Conversation counts per turn: T1 9, T2 7, T3 7, T4 9, T5 10, T6 23, T7 23,
T8 32, T9 20, T10 60, T11 8 (only titled conversations; untitled ones exist too).

| Turn | Zille / Wehlen | Pales / war | Other arcs (selection) |
|---|---|---|---|
| Prologue | Romus's childhood summers in Zille (`Prologue_Summer_Zille`), 1926 uprising, Wehzek rescue | Reclamation War backstory | Valero's death (18 Nov 1949) |
| 1 | Econ briefing: "Zille and all of the resources therein will return to Rizian ownership in 1952" | Foreign-policy briefing on Pales | Coronation, religious ceremony, houses, Golden Guard |
| 2 | Diplomacy briefing (Wehlen options: Bargain / Internal / Pressure / Intimidate / **War**); ATA cultural program in Zille or Pales; **Friendship Day visit to Zille → bombing** (`FPnT_ZilleVisit`) | Aureus gas field first raised | Sordland trade visit, Valero poisoning talk |
| 3 | `SnO_ZilleSecurityDebrief`: response to the bombing (defend / denounce / neutral on Su Omina), apology, Rusty on Zille | | Visits to Iza, Montaklar, Sallabes; Queen Beatrice visit |
| 4 | AN South Merkopa session: **Smolak invokes the "AN Threat Mitigation Protocol for Treaty Obligations"**, delaying the handover | Duke Axel's visit, council talk on Pales, Aureus arbitration | House of Delegates, Alvarez call |
| 5 | **Wraclawitz negotiations** (`FPnT_WehlenNegotiations`): treaty addendum, 3-year "probationary period"; council chooses whether to fund Su Omina and/or ATA in Zille | MITZ board, Morella negotiations (can ask Morella to back Rizia on Zille) | Internal security, Titus/Lucita |
| 6 | Border investigation results | **Aureus incident (naval or border) → post-incident talks → war** (`SnO_Incident`, `SnO_WarEruptionBriefing`, `FPnT_DukeWarPhoneCall`) | Balcony war speech, elections, energy |
| 7 | `Zille Treaty Discussion` in the House of Delegates; Derdia (mercenaries, missiles), Rumburg (BFF mercenaries) | **Operation 1** (Ziqes, Arces, Navar), war update and war tax | Derdia negotiations, Toras dinner |
| 8 | **Blimp negotiations with Smolak** (`FPnT_ZilleNegotiations`): gold / oil / arms / Estela's house / referendum; **AN session** on the referendum; who governs a returned Zille | Operation 1 results, AN **sanctions** vote, Operation 2 planning | Royal wedding, constitution, RRG nationalization |
| 9 | **Referendum** (`FPnT_ZilleReferendum`), **interference** (`FPnT_ZilleReferendumInterference`), Zille return speech, **Smolak's Zille-for-Pales call** | Operation 2 | Intermerkopum roundtable, mediations |
| 10 | New-government report and "epic choice" for Zille; Zille reform branch | Victory parade / peace treaty / loss | Strike scenes, coups, reform, succession |
| 11 | | | Endings: coup fallout, revolution, theocracy, forced abdication, end speech |

The fan wiki dates the first AN session to about 1954 and the second to about 1956
([fan wiki: Alliance of Nations](https://suzerain.fandom.com/wiki/Alliance_of_Nations)); the
database itself does not carry turn dates (they live in game code). Exact turn years: UNVERIFIED.

### B. Zille outcomes and how you reach them (game data)

| Outcome | Key variables | How it happens |
|---|---|---|
| Peaceful return by treaty | `ZilleReturn_Success`, `Journal_Turn08_ZilleNeg_ZilleReturnSuccess`, `Turn08_ZilleNegotiations_TreatyKept` | Turn 8 blimp talks succeed; Smolak revokes the Threat Mitigation Protocol at the AN ("As agreed with King Romus Toras, we will begin the transfer of the province of Zille back to the Kingdom of Rizia"). Achievement `Rizia_ZilleReturn`. |
| Return via AN stopping the referendum | `ZilleReferendum_StoppedByAN` → `ZilleReturn_Success`; news `ZilleAN_NoReferendum` | Talks fail, Smolak asks the AN for a referendum, Romus asks for **AN peacekeepers**; member-state votes (`ANZilleReferendum_Vote`) driven by relations with Derdia, Lespia etc., Rizia's global image, and whether Lespia's role in the bombing was publicised. Text: "Peacekeeping troops shall be sent to ensure the smooth transition of Zille to Rizia." |
| Referendum won fairly | `ZilleReferendum_Won` | Turn 9; strength of the pro-Rizia movement (`Situation_Diplomacy_Zille_Small/Growing/MassiveRiziaMovement`) sets early results (e.g. "around 62% … voted no to joining Wehlen"). |
| Referendum won by **army interference** | `ZilleReferendum_Interfere_Army_Success`, achievement `Rizia_FreeZille` | Choice: "We will interfere with the Rizian Armed Forces. Our troops will enter Zille to protect the ballot boxes". Fails if `Pales_War == true` or power projection is Weak/Average. Result line: "Votes are 82% in favor of joining Rizia." Relations with Wehlen set to Hostile. |
| Referendum won by **covert agents** | `ZilleReferendum_Interfere_Agents_Success` | Depends on foreign intelligence (`Situation_Order_ForeignIntelligence_*`). "Votes are 72% in favor of joining Rizia." |
| Referendum lost | `ZilleReferendum_Lost` | Zille stays Wehzek. Failed interference makes it a landslide ("64%" or "92% in favor of joining Wehlen"). |
| **Zille independence** | `ZilleReferendum_Independence_{Liberation, Bludish, Wruhecist, Golcondist}`, `RiziaDLCText.IndependentZille` | Armed resistance plus a lost or annulled vote: a masked man declares independence from both countries. Flavour follows who armed the resistance (Derdian mercenaries, Rumburg-backed BFF mercenaries, both, or neither = "Zille Liberation Front"). Turn 10 can open alliance talks with the new state (`NewGov_Epic_IndependentZilleAlliance`). |
| **Permanent transfer to Wehlen** (Zille for Pales) | `ZilleReferendum_SmolakOffer_Accepted`, `War_WehlenJoined`, `Reports_Turn10_Wehlen_ZilleTransferred` | Turn 9, during the Pales war: Smolak offers "a trade. Zille for Pales". If accepted, Wehzek troops join the war; after a Pales victory Zille is transferred ("just like your father did"). "If Pales is lost, our agreement will be void." |
| Return, then whose land? | `ZilleOwnership_Brenas` / `ZilleOwnership_RiziaImperii`, `Zille_PromisedToHouseSazon` | Give Zille back to Brenas (House Sazon) or centralize it under Rizia Imperii. Angelica Sazon's coup is tied to refusing to return Zille (fan wiki). |

### C. Who matters if Rizia fights Wehlen over Zille

| Actor | Canon position | Source |
|---|---|---|
| Wehlen (Smolak) | One-party state under the Wehzek Nationalist Party of Nurist Socialism; oil/diamond rich but a weak economy; member of OMEC and AN | [codex: Republic of Wehlen](https://codex.torporgames.com/republic-of-wehlen) |
| United Contana / CSP | Backs Smolak financially ("financial aid coming from the CSP"); funded his tanks and coastal defences; Lucita: an attack "would push him firmly into United Contana's embrace" | [codex: Wiktor Smolak](https://codex.torporgames.com/wiktor-smolak); game data, `Rizia/Turn 2/FPnT_DiplomacyBriefing` |
| Alliance of Nations | Threat Mitigation Protocol, arbitration, referendum votes, peacekeepers, sanctions votes; ATO and CSP blocs plus a neutral faction | [codex: Alliance of Nations](https://codex.torporgames.com/alliance-of-nations); game data |
| Lespia (ATO since 1950) | Organized the Friendship Day bombing (revealed by Rusty's evidence); arms Pales; leads the sanctions call against Rizia over Pales | [codex: Republic of Lespia](https://codex.torporgames.com/republic-of-lespia); game data |
| Rumburg (GRACE) | Rizia's patron; hostile to Wehlen (oil embargo on Wehzek oil, accused of arming the BFF); may back Rizia in a civil war | game data (`News_Turn04_G_RumburgDenounceWehlen_Embargo`, Turn 5 talks) |
| Derdia | Golcondist theocracy; sells mercenaries "for use in Zille" and missiles | game data (`Rizia/Turn 7/FPnT_DerdiaNegotiations`) |
| Morella | Can promise AN support or "ship supplies across the Gulf of Moridia to support the Zille resistance" in the Turn 5 MITZ talks | game data (`MITZ_Demand_ZilleDiplomacy`, `MITZ_Demand_ZilleResistance`) |

### D. Rizian Armed Forces: what the game tracks (game data variable names)

| Group | Variables / decrees |
|---|---|
| Resources | `Resources_Military_Manpower`, `_Equipment`, `_Tanks`, `_Ships`, `_Submarines`, `_FighterPlanes`, `_BomberPlanes`, `_Trucks`; air strike / air support cooldowns; `NavalBombardmentCapability`; `DerdianMissiles`; `RumburgianNerveGas`; `DerdianMercenaries(_Zille/_Pales)`; `RumburgianBFFMercenaries`; `RumburgBasicAid` |
| War-map units | Infantry, Tank, Support, Mountaineer, Marine, Paratrooper, Mercenary, Militia, Golden Guard, house levies (Azaro, Sazon, Toras), Ship, Submarine, Flagship, Fighter, Bomber (`Army_Unit_*_Total/Deployed/Dead`) |
| Military decrees | Tank / fighter / bomber / support-vehicle / military-equipment factories; buy or sell tanks, ships, submarines, planes, equipment; lower conscription age; longer conscription; raise or remove provincial levies; expand border guards; expand Fort Ales naval base; expand Terador airbase; strengthen Pales district border forts; **Mave Nortus Intelligence Hub**; Monqiz shipyard expansion |
| Derived state | `Situation_Diplomacy_PowerProjection_{Weak, Average, Strong, Overwhelming}` (gates Smolak's respect and the referendum army interference) |
| War map engine types | `WarOperationData`, `WarObjectiveData`, `WarNavalBlockade`, `WarSupplyLine`, `WarTileFortLevelChange`, `WarTileAntiAirChange`, `WarTileCoastalArtilleryChange`, `WarEnemyAirFieldChange`, `War_ActionPoints` (from `Assembly-CSharp` interop) |

---

## Details

### 1. Overall plot

- **Premise.** Romus Toras (born 1905) becomes king after Valero "the Frail" dies in 1949.
  Valero's dying message tells him "to retake Zille and Pales"
  ([fan wiki: Romus Toras](https://suzerain.fandom.com/wiki/Romus_Toras); death date matches the
  game's timeline data). The Steam page sells it as "Reclaim lost territories through diplomacy
  or force" and promises a "War Mechanic & Military Build-up … train units to intimidate
  neighbors" ([Steam store, DLC 2352340](https://store.steampowered.com/app/2352340/), released
  25 Mar 2024).
- **Main arcs** (game data conversation list): Zille/Wehlen; Pales and the Aureus gas field
  (arbitration, incident, war); MITZ and Morella; Derdia and Golcondists; Rumburg and GRACE;
  Lespia; the Intermerkopum regional alliance (turn 9 roundtable); royal decrees and the
  economy/energy/authority resources; the three noble houses (Toras, Azaro, Sazon); the
  constitution question (absolutism, status quo, reform with Manus Sazon/RPP or Adarfo
  Sotelato); theocracy; family (Vina, Estela, Hugo, Rico, Lucita romance, Pabel).
- **Endings.** Turn 10 and 11 hold the strike/coup scenes, reform constitution signing,
  succession, and endings. Epilogue branches in the database: Continued Reign, Peaceful Power
  Transition, Exile, Prison, Immediate Death, Civil War, Survived. The fan wiki lists Status Quo,
  Absolutism, coups by each house (Azaro, Toras, Sazon), Reform Coup, Golden Guard coup (Titus
  betrayal), Reform (radical / moderate), Revolution, Exile, Abdication and Civil War
  ([fan wiki: Endings](https://suzerain.fandom.com/wiki/Endings), marked WIP). Torpor's own survey
  names the epilogue families as "Continued Reign; Peaceful Power Transition; Death &
  Imprisonment; Civil War; Exile" ([Torpor survey](https://docs.google.com/forms/d/e/1FAIpQLSckNd9DQIZ2TlA08oAJ4zcUWrfvzROngw6QaGFqQIyDHMoXHg/viewform)).
- **Civil War epilogue.** Text prompts where Romus can let "Wehlen send troops to Rizia once
  more", after which "thousands of Wehzek soldiers were deployed to Rizia" and Lespia answers by
  sending its army; Rumburg may intervene or be tied up in a war with Sordland (game data).

### 2. The Zille storyline

**Background (codex, CONFIRMED).**
- Zille sits at "the northernmost tip of Rizia by the strait of Kreleck"; fishing, then spas and
  resorts, then gold mining ([codex: Territory of Zille](https://codex.torporgames.com/the-territory-of-zille)).
- The Zille Agreement (1927) gave Wehlen the territory for 25 years with full authority over
  governance and resources, plus protections for Rizian residents and cross-border movement.
  Officially thanks for Wehlen's help against the 1926 uprising; unofficially a punishment of
  Brenas and House Sazon ([codex: The Zille Agreement](https://codex.torporgames.com/the-zille-agreement)).
- In 1926 Valero, urged by Estela, asked Wehlen for troops; Wehlen demanded the Zille gold
  mines. Wehzek foot soldiers and Taddeus Azaro's special forces retook Montaklar
  ([codex: Uprising of 1926](https://codex.torporgames.com/uprising-of-1926)).
- Wehlen civil war 1930–1940; Arcasia backed the south, United Contana the north; Smolak
  president since 1943 ([codex: Republic of Wehlen](https://codex.torporgames.com/republic-of-wehlen)).
- Fan wiki: Territory ~3.7 million people; Zille city 2.8 million (Rizian 52.5%, Wehzek 30.4%,
  Bludish 10.7%); Nanta 0.86 million; Estela was born in Zille
  ([fan wiki: Territory of Zille](https://suzerain.fandom.com/wiki/Territory_of_Zille),
  [Zille](https://suzerain.fandom.com/wiki/Zille), [Nanta](https://suzerain.fandom.com/wiki/Nanta)) (fan wiki, unsourced figures).

**Deadline and delays (game data, CONFIRMED).**
- Turn 1: return "in 1952"; Turn 2: "scheduled for the end of 1952".
- Bombing: "At 1605 hours on December 8, 1950, an explosive object detonated in Smolak Square
  in the city of Zille." The banner reads "24 Years Of Wehzek-Rizian Friendship"; Smolak calls
  it "the second-to-last Wehzek-Rizian Friendship Day". Three Wehzek civilians died per the fan
  wiki timeline ([fan wiki: Timeline](https://suzerain.fandom.com/wiki/Timeline)). Note the
  in-game arithmetic is loose (1927 + 24 ≠ 1950).
- Turn 4 AN session: Smolak: "I have decided to invoke the AN Threat Mitigation Protocol for
  Treaty Obligations to delay the return of Zille until that risk is neutralized." Laurento: "No
  nation has invoked this protocol since the signing of the AN Charter nearly a century ago."
  Later text refers to the protocol as "instituted in 1952".
- Turn 5: Smolak asks for "a three-year probationary period"; Laurento warns "he is in a
  position to hold Zille hostage until his demands are met."
- Turn 8 takes place "three years" later ("We've waited three years for this moment").

**Who bombed Friendship Day (game data, CONFIRMED).** Wehlen blames Su Omina; Rico Toras can be
framed for it; the truth available through Rusty Montoro is that **Lespia organized it**
("We now possess conclusive proof that Lespia organized the Friendship Day bombing"). Romus can
publish, withhold or trade that evidence with Alvarez (stop arming Pales, block sanctions).

**Players and pressure tools.**
- **Su Omina**: nationalist group founded 1932 in Brenas, aims to restore pre-Pales/Zille
  borders, tolerated by Rico Toras ([codex: Su Omina](https://codex.torporgames.com/su-omina)).
  In game it is the armed resistance option (`Decision_Turn05_ZilleResistanceFunding_BeginSOFund`).
- **ATA**: Ariana Toras Association, a government-funded cultural body
  ([codex: ATA](https://codex.torporgames.com/ariana-toras-association)); in game the non-violent
  pro-Rizia movement in Zille (`Situation_Diplomacy_Zille_ATAActive`).
- Council funds SO, ATA or both in Turns 5, 7 and 8; these set `Zille_ViolentResistancePower`
  and `Zille_ReferendumPower` and the Weak/Growing/Strong armed resistance and
  Small/Growing/Massive Rizia movement states.
- **Derdian mercenaries** (Turn 7): "Rizia will withdraw from Wehlen's anti-BFF operations in
  exchange for the aid of Derdian mercenaries in the Zille resistance movement."
- **Rumburg-backed BFF mercenaries**: "an internal resistance effort from the BFF could work in
  perfect concert with the Derdian mercenaries already present in the region."
- **Morella** (correction to the brief): Morella does not make Zille demands. In the Turn 5
  MITZ talks *Rizia* can ask Morella for help: diplomatic backing at the AN, or supplies across
  the Gulf of Moridia for the resistance.

**Negotiations.**
- Turn 5 (Wraclawitz): Smolak's addendum bundles a trade deal (Wehzek oil surplus, "trade
  differential"), a migration/working-rights clause for Wehzeks in Zille, and counter-terrorism
  cooperation (ban Bludish and Derdian entrants, aid against the BFF, Rizia joining
  "Operation Bear Trap", extradite Rico). Refusing everything rules out a diplomatic handover.
- Turn 8 (aboard Smolak's blimp): terms are gold exports or a one-off gold payment, oil
  imports ("+2 Oil for -3 Budget"), arms exports for Wehlen's war on the BFF, and handing over
  Estela's childhood house. If enough is not met, Smolak announces a referendum. Smolak openly
  reacts to Rizian power ("The amount of military strength you've managed to amass since your
  reign began is truly impressive"; "Wehlen likes winners").

**Military options already in vanilla (game data, CONFIRMED).**
1. Turn 2 briefing, option "Do we have military options to use against Wehlen?" (sets
   `Turn02_Diplomacy_Wehlen_War`). Lucita (or the sitting War Councilor): "Outside of Zille,
   very few that are realistic"; mined sea approaches, "layered naval batteries", "A naval
   invasion across the gulf would be operationally unimaginable"; "our navy lacks the necessary
   amphibious capacity"; "Contanan radar and anti-air systems would blunt our strikes"; "Zille
   is a different matter. It lies on our mainland. There, we can exert intelligence and limited
   military pressure with far greater feasibility." Romus can answer "I have no desire for war.
   Wehlen stood by us in a dire time." This exchange mentions United Contana radar and reads
   like a recent addition; patch it came in: UNVERIFIED.
2. Turn 8 AN speech lines: "If he makes a single move to prevent the return of Zille, we will
   respond"; "With or without the help of Alliance of Nations, we will defend our territory."
3. Turn 9 interference. Armed militants in ski masks occupy election centres; the War
   Councilor: "We have all of the contingency plans ready to launch at your command." Army
   result text: "Our troops entered Zille immediately after I gave the order. The Wehzek troops
   fell back as soon as we entered." On failure: "the citizens themselves started to push our
   soldiers back … As the Wehzek troops were falling back, they intentionally gave all of their
   equipment to the civilians of Zille." The king waits in "the Jales mansion".
4. Turn 9 Smolak call: Wehlen's army joins the Pales war in exchange for Zille.

**The border and Mave Nortus.** The Turn 2 motorcade crosses a "concrete barrier … strung with
barbed wire"; on the Zille side there are shanty towns, factory smokestacks, tenements
"hastily repainted to hide the damage from the war 20 years ago", and Smolak Square (formerly
named after Duke Fedo Sazon) with three Wehzek tanks on display. Mave Nortus is the Rizian
border town and main entry point for Wehzek migrants, with customs offices and temporary housing
([codex: Mave Nortus](https://codex.torporgames.com/mave-nortus)); the royal train runs there,
and decrees can build a **Mave Nortus Intelligence Hub** and a **Mave Nortus oilfield**.
No vanilla border clash or skirmish at Mave Nortus was found in the database (searched for
war, troops, army and attack near Zille and Wehlen).

### 3. The Pales war

- **Background.** The 1923–1926 Reclamation War failed against Lespian-armed Pales and sparked
  the 1926 uprising ([codex: The Reclamation War](https://codex.torporgames.com/the-reclamation-war)).
  In game the new one is called "the Second Pales War".
- **Trigger (game data).** The Aureus gas field claim (Rizia can blockade the field or buy gas
  rights in Turn 4; the AN Arbitration Committee's ruling is reported at the Turn 6 council
  meeting, achievement `Rizia_Arbitration`) leads to a Turn 6 incident, either at sea
  near the field (`War_Incident_Naval`) or at the southern border (`War_Incident_Border`).
  Post-incident talks with Axel can end in a deal (cede the field, split 50/50) or in war
  ("If you don't pull your ships back from the field... War it is, Your Grace."). Preparing for
  or declaring war through diplomacy also sets `Pales_War`.
- **War plan (Turn 6 briefing).** Objectives: Ziqes and Arces, trapping Palesian forces at the
  Solles chokepoint; anti-air in the mountains south of Ziqes; Navar by Fort Ales; River Alca
  forts in the north; mountaineers in the western mountains; Lespian tanks and destroyers.
  Choices: feign north, feign south, or direct assault; blockade the Gulf of Ales, Gulf of
  Pales or Gulf of Remaz, or keep the navy on standby; plan it yourself or delegate to the
  General Staff. Taddeus Azaro returns in a wheelchair for the briefing.
- **Mechanic.** Turn-based tile map with operations (Operation 1 around Turn 7 with results
  debriefed in Turn 8, Operation 2 in Turns 8–9; exact placement approximate), action points, supply lines, forts, anti-air, coastal artillery, airfields, naval
  blockades, air strikes and air support with cooldowns, special units, and optional Derdian
  missile strikes on Port Pales or Remaz, and Rumburgian "experimental weapons" (the variable
  is `Resources_Military_RumburgianNerveGas`; their use, exposed by Palesian journalists, feeds
  the AN case against Rizia).
  The existing map doc counts 100 land and 33 sea tiles on `Map_Rizia_PalesWar`.
- **International framing.** Lespia sends manpower and equipment depending on your leverage;
  at the AN Alvarez (and Hegel of Valgsland) accuse Rizia of a "scorched earth policy" and call
  for "heavy sanctions" (achievement `Rizia_ANSanctions`); Rumburg can request a ceasefire;
  the House of Delegates can propose peace; Manus: "It is difficult for my party to support an
  all-out attack on the Grand Duchy, especially when the future of Zille is still in doubt."
- **Outcomes (Turn 10 variables).** Victory parade; peace treaty with annexation, Pales divided,
  pre-war borders, or Pales Administrative District lost; side outcomes for the Aureus field,
  the Port of Pales and Palesian neutrality; keep fighting; loss or sue for peace
  (`Rizia_PalesWarLost`); Axel captured/tortured variant. If Wehlen joined, Zille transfers
  to Wehlen after victory.

### 4. Wehlen

- Government: presidential republic in name, one-party state in practice under the Wehzek
  Nationalist Party of Nurist Socialism; capital Wraclawitz; Smolak elected 1943
  ([codex: Republic of Wehlen](https://codex.torporgames.com/republic-of-wehlen)). The fan wiki
  draws the Ba'athist Iraq parallel ([fan wiki: WNPNS](https://suzerain.fandom.com/wiki/Wehzek_Nationalist_Party_of_Nurist_Socialism)).
- Alliances: OMEC and AN member; not listed as a CSP member by the codex, but financed by the
  CSP/United Contana ([codex: Wiktor Smolak](https://codex.torporgames.com/wiktor-smolak)).
  Wehlen's armed forces strength in numbers: UNVERIFIED (not stated anywhere I found). Canon
  descriptors: new tanks bought with oil money and Contanan funding; Romus can sneer at "the
  sloppy way they're holding their guns"; Wehzek coastal defences are strong.
- Relationship with Rizia: close allies under Valero; cordial after Smolak's rise; Rizia admits
  Wehzek "labor hand" migrants ([fan wiki: Wehlen](https://suzerain.fandom.com/wiki/Wehlen)).
  Base-game links: `BaseGame.Wehlen_TradeDeal` is checked in Rizia (save transfer from Sordland).
- **Likely interveners if Rizia attacks Zille** (inference from canon, not a scripted event):
  United Contana (diplomatic/material backing for Smolak; nuclear-age superpower), the AN
  (protocol, sanctions, peacekeepers), Lespia (anti-Rizian and anti-Smolak at once), Rumburg
  (pro-Rizia, anti-Wehlen), Derdia and Morella (can arm or supply the resistance). No canon
  text has any of them declaring war over Zille.

### 5. Rizian Armed Forces

- Codex: HQ Montaklar; General Staff, Land, Naval and Air Forces; led by General Carlos Azaro;
  equal conscription from the provinces; Montaklar, Brenas and Valenqiris keep provincial
  forces; "one of the smallest and weakest militaries on the Merkopan continent"
  ([codex: Rizian Armed Forces](https://codex.torporgames.com/rizian-armed-forces)). Navy
  weakened by the 1923–26 war and mutiny ([codex: Rizian Navy](https://codex.torporgames.com/rizian-navy));
  air force mauled by Lespian aircraft ([codex: RRAF](https://codex.torporgames.com/rizian-royal-air-force)).
- Fan wiki adds: army mostly infantry with few tanks/APCs, reliance on Rumburgian equipment, a
  Mountaineer Corps, and options to form more mountaineers, marines or paratroopers
  ([fan wiki: Rizian Armed Forces](https://suzerain.fandom.com/wiki/Rizian_Armed_Forces)) (fan wiki).
- Resources: Budget, Authority, Energy are the national resources (Torpor survey question
  "most lacking resource": Authority / Energy / Budget). Military build-up runs through Order
  decrees (table D) and foreign deals (Wehzek arms exports are an `Equipment_Modifier`).

### 6. Characters and their Zille stances (game data unless linked)

| Character | Role | Stance on Zille / force |
|---|---|---|
| Lucita Azaro | War & Security Councilor, Duchess of Montaklar ([codex](https://codex.torporgames.com/lucita-azaro)) | Realist hawk: Zille is "the only front where we can apply controlled military and intelligence pressure"; runs the referendum interference |
| Taddeus Azaro | General, Lucita's father, stroke in 1949 ([codex](https://codex.torporgames.com/taddeus-azaro)) | Can hold the War seat instead of Lucita; same interference lines |
| Carlos Azaro | Commander of the RAF ([codex](https://codex.torporgames.com/carlos-robles-azaro)) | No Zille lines found |
| Titus Gordion | Captain of the Golden Guard ([codex](https://codex.torporgames.com/titus-gordion)) | Can hold the security seat; wants Golden Guard expansion after the bombing; can frame Rico |
| Laurento Esquibel | Foreign Affairs Councilor ([codex](https://codex.torporgames.com/laurento-esquibel)) | Diplomacy first: "as long as Wehlen still holds Zille, diplomacy is paramount"; warns aggression gives Smolak an excuse |
| Estela Toras | Queen Mother, born in Zille | Personal: wants her childhood house back; scolds Romus for a "military expedition" in Pales instead of Zille |
| Hugo Toras | Grand Vizier, Romus's uncle | Keep good relations with Wehlen to regain Zille; Sazons expect Zille back |
| Rico Toras | Duke of Iza, Su Omina sympathiser ([codex](https://codex.torporgames.com/rico-toras)) | Denies Su Omina did the bombing; Wehlen wants him extradited |
| Angelica / Manus Sazon | House Sazon matriarch / RPP leader | Angelica: support "as long as Zille is returned to the province of Brenas"; Manus worries Su Omina undermines a peaceful transfer |
| Daria de Rava | RNC leader | "My party supports the reclamation of Zille by any means necessary." |
| Vina Toras | Princess | Questions the extra three years of Wehzek rule for Zilleans |
| Russello "Rusty" Montoro | RRG chief | Sells the Lespia-bombing evidence; RRG reclaims old mine sites |

### 7. Community sentiment, patch history and existing mods

The Reddit numbers below come from the Arctic Shift archive API, because Reddit returned 403
even through the proxies. Scores are snapshots taken when each post was archived, so treat them
as minimums. The Zille title search is complete: 361 posts from 2024-02-20 to 2026-11-13.

**Demand is strong and long-running.**
- I hand-counted 30 Reddit posts asking for or discussing a Zille or Wehlen war. Together they
  have about 2,625 upvotes and 437 comments. The biggest:
  - "Rizia players if the game gave them a war option with wehlen", Jan 2026, 487 upvotes
    ([reddit](https://www.reddit.com/r/suzerain/comments/1qnc4as))
  - "Smolak the second Torpor decides to add war with Wehlen", 378 upvotes
    ([reddit](https://www.reddit.com/r/suzerain/comments/1qrbqxl))
  - "Would a Romus with overwhelming power projection…", 287 upvotes
    ([reddit](https://www.reddit.com/r/suzerain/comments/1q3a7nt))
  - "Mark my words, I expect 3.1 to have Zille war", 192 upvotes and 39 comments
    ([reddit](https://www.reddit.com/r/suzerain/comments/1jkhfr6))
  - "There will not be a Zille war", 181 upvotes and 52 comments
    ([reddit](https://www.reddit.com/r/suzerain/comments/1jn6ksh))
  - "Why can't we invade ZILLE?", Feb 2026 ([reddit](https://www.reddit.com/r/suzerain/comments/1quu1yi))
  - "Once again begging Torpor for the Wehlen war DLC", Jun 2026
    ([reddit](https://www.reddit.com/r/suzerain/comments/1uitd08))
- Fans also ran a hand-drawn "Zille war" on the subreddit. Its map shows Zille, Nanta,
  "Port Smolak" and Mave Nortus ([post 1](https://www.reddit.com/r/suzerain/comments/1t9z7mp),
  141 upvotes; [update](https://www.reddit.com/r/suzerain/comments/1tcziue), 257 upvotes).
- On Steam, "How do I invade Zille? Or is the Pales War presently the only one?" (Apr 2024) got
  one reply: "There is no Zille war"
  ([Steam](https://steamcommunity.com/app/1207650/discussions/0/4361247379739930870/)).
- Torpor's own survey asked: "[Rizia] Would like there to be a Zille-related Wehlen war
  expansion?" ([Torpor survey](https://docs.google.com/forms/d/e/1FAIpQLSckNd9DQIZ2TlA08oAJ4zcUWrfvzROngw6QaGFqQIyDHMoXHg/viewform)).
  The form says the answers are "not commitments".

**Torpor's position.**
- There is no official promise of a Zille war.
- A May 2025 post titled "Its official. Devs wont add zille war" is a misreading: its
  screenshot is a feedback-form question about the next DLC.
- A Discord quote has been reposted on Reddit; I could not find the original. It reads: "A full
  war like Pales is not doable for us… initially we wanted to do both. But we just are too few
  with too little resources to pull it off." (FAN-REPORTED.)
- Torpor's war-system work has gone into **Wars of Suzerain**, a standalone game announced
  15 Jul 2026 and set on the Rumburg vs Agnolia front, not Zille
  ([Steam news](https://store.steampowered.com/news/app/1207650/view/1838407329254369)).

**Patch notes relevant to the mod** (Steam news for app 1207650):
- 3.0.0 (25 Mar 2024) added the "War mechanic", the "Pales local war map" and "Dynamic map
  layer changes for Pales, Zille, Iza"
  ([news](https://store.steampowered.com/news/app/1207650/view/5686430301733119284)).
- 3.0.4/3.0.5 (Apr 2024) "Factored funded militias into Zille referendum interference results"
  and "Fixed Wehlen leaving war when BFF is destroyed"
  ([3.0.5](https://store.steampowered.com/news/app/1207650/view/5756237364312229036)).
- 3.0.7 "Factored power projection more into Zille return outcome"
  ([news](https://store.steampowered.com/news/app/1207650/view/5762993398273336145)).
- 3.0.8 "Adjusted violent and peaceful resistance balance and thresholds for Zille"
  ([news](https://store.steampowered.com/news/app/1207650/view/5768625435396613327)).
- 3.0.9 "Added feedback on Wehlen war intervention deal", "Added authority modifiers for
  permanent loss of Zille" and "Added experimental weapons to war operation 2"
  ([news](https://store.steampowered.com/news/app/1207650/view/5842939267310052802)).
- 3.1 "Sovereign" (7 May 2025) added "a full airforce system and smarter enemy AI", fighter and
  bomber units, a guided war tutorial, an "independent Zille token/flag", "Reworked
  Zille-related calculations", an "Updated Zille Referendum threshold", Turn 11 and the
  civil-war epilogue
  ([news](https://store.steampowered.com/news/app/1207650/view/1799088287803382)).
- Build 175 (9 Jun 2026, our installed version) fixed "Zille negotiations not correctly
  accounting for BFF outcomes in non-Pales war paths"
  ([news](https://store.steampowered.com/news/app/1207650/view/1834602721201723)).

**Player-reported mechanics** (FAN-REPORTED, consistent with the game data):
- The military route at the referendum needs "overwhelming power projection" plus Su Omina
  ([reddit](https://www.reddit.com/r/suzerain/comments/1cbvedj)).
- Independence follows from funding the ATA, placing the Derdians and the BFF in Zille, and
  winning the vote ([reddit](https://www.reddit.com/r/suzerain/comments/1krrbht)).
- The Rotsla and Wals isles stay Wehzek even after Zille returns, Nov 2026, 361 upvotes
  ([reddit](https://www.reddit.com/r/suzerain/comments/1xpc4hq)).
- Smolak's war-for-Zille offer, 246 upvotes ([reddit](https://www.reddit.com/r/suzerain/comments/1tw8d9l)).
- The Pales war starts from a gas-field blockade, a lost AN vote, a refusal to withdraw, then a
  ship collision ([Steam](https://steamcommunity.com/app/1207650/discussions/0/4358994946502331564/)).
- The war mechanic draws criticism too: "The battle system is terrible", 51 replies
  ([Steam](https://steamcommunity.com/app/1207650/discussions/0/4295944344105964527/)).

**Existing mods.**
- No mod adds a Zille war.
- The Suzerain Modding Kit ([GitHub](https://github.com/suzerain-modding/suzerain-modding-kit),
  8 stars, v2.4) covers bills, decisions, reports, journal entries, dialogue and events for
  Sordland and Rizia. It has no war or battle-map module, so a playable map is new ground.
- Nexus has 18 Suzerain mods (found through its GraphQL API). The only war-related one is
  "Better Call Beatrice", a Rumburg-war trigger tweak with about 50 downloads.
- There is no Steam Workshop.
- The popular "Suzerain mods" on r/suzerain are Hearts of Iron IV total conversions, not mods
  for Suzerain itself. Examples: an expanded-Merkopa map, Aug 2023, 868 upvotes
  ([reddit](https://www.reddit.com/r/suzerain/comments/15ihljb)); "Heart of Sordland", whose
  teaser (Feb 2026) has 859 upvotes ([reddit](https://www.reddit.com/r/suzerain/comments/1qynbnq)).
  Whether any of them stages a Rizia–Wehlen war: UNVERIFIED.
- The count of 30 Zille/Wehlen war posts is a minimum. The archive's "war" search capped at
  200 results, and its "smolak" and "invade" searches came back empty.

**YouTube.** Sa Game's "Suzerain Rizia war path is VERY easy" has about 32K views
([video](https://www.youtube.com/watch?v=bjhMnsPH3ZE)). Greyscales' "Rizia, but I Seize Absolute
Power" has about 161K ([video](https://www.youtube.com/watch?v=WKzgQuswIto)). Zille-specific
videos are small (Anthnwam, "The Zille Referendum Results", 691 views). I found no video
showing a Zille war.

---

## How the game-data quotes were extracted

The dialogue database is one 129 MB MonoBehaviour named `Suzerain` inside
`Suzerain_Data/StreamingAssets/aa/StandaloneWindows64/database_assets_all_*.bundle`
(articy:draft 4.1.2 export, stamped 6/4/2026). It has no type tree (IL2CPP build), so I dumped
its raw bytes with UnityPy (`obj.get_raw_data()`) and scanned for Unity's aligned,
length-prefixed UTF-8 strings. That yields every field in order: actor names, `Title`, the `en`
line text, condition strings (`Variable["RiziaDLC.X"] == true`), scripts (`RiziaDLC.X = true;`)
and conversation paths such as `Rizia/Turn 9/FPnT_ZilleReferendumInterference`. Speakers come
from the truncated `Name: "..."` titles. The same approach works for checking any line or
variable the mod needs to hook (SMK reads and writes these same `RiziaDLC.*` variables).

## UNVERIFIED

- Exact in-game year of each turn (dates live in game code, not the dialogue DB).
- Whether the Turn 9 army-interference choice is always offered. The entry carries no
  condition string in the database, so it looks unconditional once the referendum scene
  plays, but I did not confirm this in a live playthrough.
- Exact thresholds behind `Situation_Diplomacy_PowerProjection_*` (computed in code).
- Wehlen's army size or composition in numbers.
- Which patch added the Turn 2 "military options against Wehlen" exchange.
- Mapping of independence flavours to who armed the resistance (inferred from condition order).
- Fan-wiki population and ethnicity figures for Zille and Nanta.
- The Discord dev quote ("A full war like Pales is not doable for us…"): seen only as a
  repost in a Reddit comment, original not found.
- The Reddit claim that the Bombing culprit can be "Sal" (contradicts the game data, which
  points to Lespia, with Rico as a possible frame-up).
- Discord town hall content (not mirrored publicly).
