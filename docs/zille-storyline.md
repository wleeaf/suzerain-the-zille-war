# The vanilla Zille storyline, mapped from game data

Source: the extracted Rizia dialogue database (one `.txt` per conversation, plus `_index.tsv` and `_variables.tsv`).
Notation:
- `File [N]` means entry N in `Rizia_<File>.txt`.
- `cNNN` is a conversation id from `_index.tsv`. In-file jumps look like `-> c404:1`.
- Variable names are copied exactly. The namespace prefix is always written out.

Every gate and DO that the outcome logic depends on was checked by hand against the files:
- negotiation thresholds
- the AN vote
- the referendum and interference logic
- the Smolak deal
- the Peace Treaty transfer
- the ZilleWarAlly tally
- the Security Brief "bomb the bastards" line

The narrative summaries and long tails of flavour lines were gathered by reading passes over each file. Those were not each re-verified.

> **What the dialogue DB does not contain.** It holds conversations only. Several key Zille variables are never assigned in any conversation:
> - **Engine or turn-logic outputs:** the `Situation_Diplomacy_Zille_*` resistance/movement tiers, `Situation_Diplomacy_PowerProjection_*` and `CountryRelation_Wehlen_*` (except the three set at Turn 9).
> - **Decision-card and panel outputs:** the Turn 3, 4, 5 and 7 decision cards (`Decision_Turn03_ZilleIncidentResponse_*`, `Decision_Turn04_WehlenInvestigation_Cooperate`, `Decision_Turn05_ZilleResistanceFunding_*`, `Decision_Turn07_ZilleResistanceFundingTwo_*`), the Turn 5 Wehlen promise panel (`ShowPagedDecisionsPanel("Panel_WehlenNegotiationPromises")`, which sets `RiziaDLCIsolated.Turn05_WehlenTreaty_Promise*`), the MITZ demand panel (`RiziaDLC.MITZ_Demand_Zille*`) and the Smolak-demand flags `RiziaDLC.Turn05_SmolakDemands_*`.
> - **Not in the DB at all:** turn scheduling, meaning which conversation fires when. Conversations have no entry-0 gate; the turn data decides.
>
> The mapping from `RiziaDLC.Zille_ReferendumPower` and `RiziaDLC.Zille_ViolentResistancePower` to the Situation tiers must live in turn or situation assets. Those are the next things to extract (see section 7).
>
> Data quirk: `_index.tsv` has two conversations titled `Rizia/Turn 7/BFF Convo (Friendly)` (c390, 106 entries; c391, 107 entries). The export kept only c391 (`Rizia_Turn_7_BFF_Convo_Friendly_.txt` has 107 entries). c390, which the RumburgMeeting enters at [1346]–[1349], is missing. The same title collision hit the two `Notes/Notes Conversation` files in Turns 8 and 10.

---

## 0. Cheat sheet

**Background.** Zille is a port region at Rizia's north-west tip with "millions of people", gold mines and oil. Valero leased it to Wehlen for 25 years from 1927, in return for Wehlen crushing the 1926 Sazon rebellion. The handover is due in 1952.

**Key master variables:**
- `RiziaDLC.ZilleReturn_Success`. This one flag decides "Zille is Rizian" for almost every Turn 10/11 and epilogue check.
- `RiziaDLC.ZilleReferendum_Independence` plus a flavour flag, for an independent Zille.
- `RiziaDLC.War_WehlenJoined`, meaning Smolak's "Zille for Pales" deal was accepted and Zille is ceded permanently.

**Two hidden power meters, both starting at 0:**
- `RiziaDLC.Zille_ReferendumPower`, the soft pro-Rizia movement (ATA, Lespia, Morella diplomacy).
- `RiziaDLC.Zille_ViolentResistancePower`, armed resistance (Su Omina, Derdian mercenaries, BFF, Lespia).

They feed the engine-side tiers. Starting values come from `_variables.tsv`.
- `Situation_Diplomacy_Zille_SmallRiziaMovement` (starts True), `_GrowingRiziaMovement`, `_MassiveRiziaMovement`.
- `Situation_Diplomacy_Zille_WeakArmedResistance` (starts True), `_GrowingArmedResistance`, `_StrongArmedResistance`.

The tiers decide the Turn 9 referendum.

**Arc:**

| Turn | What happens |
|---|---|
| 2 | Friendship Day visit, then the bombing. |
| 3 | Security debrief, Su Omina (SO) or ATA, the scapegoat deal. |
| 4 | Smolak invokes the AN Threat Mitigation Protocol and delays the handover. |
| 5 | Wehlen treaty (3-year probation) and resistance funding. |
| 7 | Derdian mercenaries, the BFF, the House of Delegates (HoD) debate. |
| 8 | Security brief and contingency, Smolak airship negotiation, AN vote. |
| 9 | Smolak's "Zille for Pales" call, referendum (with optional army or agent interference), the return ceremony. |
| 10 | Reports, epic choices, Pales peace treaty (the transfer, if the deal was taken). |
| 11 | Ending speech, epilogues. |

**There is no war with Wehlen in vanilla.** The closest things are:
- the Turn 9 army deployment to "protect the ballot boxes" (`RiziaDLC.ZilleReferendum_Interfere_Army`), where Wehzek troops simply fall back;
- dialogue-only threats that ministers shut down.

Turn naming: file turns are one ahead of some variable names. For example, `RiziaDLC.Turn01_ZilleVisit_HostileSpeech` is set in `Turn_2_FPnT_ZilleVisit`.

---

## 1. Turn-by-turn narrative

### Prologue and Turn 1: background

- **Prologue**
  - Wehlen was brought in by Valero during the rebellion. "For the next 25 years, Zille would belong to Wehlen."
  - Choices:
    - Childhood summers in Zille: `Prologue [69]` sets `RiziaDLC.Prologue_Summer_Zille = true` and `Diplomacy_Wehlen_NegotiatingPower +1`.
    - Joining Su Omina: `[215]` sets `RiziaDLC.Prologue_JoinedSuOmina = true`.
- **Turn 1, Economy** (Elena Werner)
  - Zille's gold mines were lost.
  - `[297]`: "Zille and all of the resources therein will return to Rizian ownership in 1952."
  - Rusty sued Valero's government over the handover.
- **Turn 1, Security/Military** (Lucita Azaro)
  - Valero pulled intelligence out of Zille and Pales.
  - The army never recovered from the Reclamation War and the 1926 rebellion. The navy is "well-trained and up to date" (`[75]`).
  - Against Pales, air power is the weakness: "they'd rain death on us from above" (`[292]`–`[300]`).

### Turn 2: Diplomacy briefing (setup) and the Zille Visit

**DiplomacyBriefing** (Laurento Esquibel, Lucita, Hugo, Elena)
- Esquibel's line is that "as long as Wehlen still holds Zille, diplomacy is key" (`[71]`/`[72]`). Lucita: "I see no point in remaining diplomatic... It's an industrial wasteland" (`[158]`).
- **Military-options exchange, `[780]`–`[795]`.** This is the only vanilla military assessment of Wehlen. Asking it sets `RiziaDLCIsolated.Turn02_Diplomacy_Wehlen_War = true` at `[781]`.
  - `[783]` Lucita: "President Smolak has spent years fortifying his coastline. The sea approaches are heavily mined, and reinforced with layered naval batteries. United Contana's aid allowed Wehlen to significantly expand those defenses. A naval invasion across the gulf would be operationally unimaginable."
  - `[786]`: "our navy lacks the necessary amphibious capacity for a sustained landing... [and minesweepers]."
  - `[787]`: "**Zille is a different matter. It lies on our mainland.** There, we can exert intelligence and limited military pressure with far greater feasibility."
  - `[793]`: "It is the only front where we can apply controlled military and intelligence pressure from our mainland."
  - `[791]`: a rapid strike would be broken by minefields and coastal guns. `[792]`: "Contanan radar and anti-air systems" would blunt air strikes.
  - `[784]` Esquibel: "An outright attack would push him firmly into United Contana's embrace." `[785]` Hugo: "If we move against Wehlen openly, we risk strengthening the very man we could wish to keep in check."
- **ATA decision.** This is the first Zille lever (see section 2, T2).

**FPnT_ZilleVisit** (Romus, Estela, Smolak, Titus)
- The party crosses a fortified border with a concrete barrier and barbed wire. Estela visits her ruined family home, which is optional and sets `RiziaDLCIsolated.Turn01_Zille_GrandparentsHouse`.
- At Smolak Square (formerly named for Duke Fedo Sazon) there are **three Wehzek tanks, a dozen soldiers in formation, and armed guards on every grandstand level** (`[67]`, `[71]`, `[86]`).
- Smolak: "Two dozen years ago, the glorious country of Wehlen was given custody over this land" (`[89]`). "Rizia isn't Wehlen's only friend" (`[80]`).
- The player gives a speech, either hostile or conciliatory.
- Bombing: someone shouts "For the King!" and the Wehzek stands are hit. Smolak: "If your people have anything to do with this, there will be a price to pay" (`[282]`).

### Turn 3: Security debrief and the Su Omina question

**SnO_ZilleSecurityDebrief** (Lucita, Hugo, Titus, Esquibel)
- 3 civilians were killed and 14 injured on December 8, 1950 (`[29]`, `[31]`). Wehlen blames Su Omina (`[41]`).
- Hugo suggests a Wehzek false flag (`[67]`). Esquibel says Smolak will publicly blame Rizian nationalists (`[104]`).
- Funding choice:
  - Lucita's foreign intelligence plus an investigation team in Wehlen, or
  - Titus's Golden Guard expanded to a division to investigate SO, or
  - neither.
- The ending always sets `RiziaDLC.ZilleSecurityDebriefFinished`, `Diplomacy_Wehlen_NegotiatingPower -5`, and `RiziaDLC.Enable_Policy_RD_Order_MaveNortusIntelHub = true` (`[443]`, `[492]`). The last one unlocks the **Mave Nortus intelligence hub** order on the Zille border.

**A_VisitToIza** (Duke Ricardus "Rico" Toras, Su Omina's patron)
- "Su Omina didn't plan the Zille attack" (`[312]`).
- He offers scapegoats: SO members confess as a "splinter cell".
- He offers to activate an SO branch in Zille: "We'll only get back what's ours if we put the screws to him" (`[434]`). "The region is teeming with Rizian loyalists anyway. All they need is a spark" (`[435]`).
- Public response to the bombing: the decision variables `RiziaDLC.Decision_Turn03_ZilleIncidentResponse_DenounceSO` / `_DefendSO` / `_Neutral` and `RiziaDLC.Decision_Turn03_ZilleBombApology` are set **outside the DB** (a decision card). They are read heavily in Turns 3–5.

**QueenBeatriceVisit** (Rumburg)
- "What will you do if Mr. Smolak fails to relinquish his hold on Zille?" The player can answer `[54]` "If it comes down to it, I'm prepared to fight." Beatrice replies `[58]` "Then you will need Rumburg's help."
- There is also an oil-embargo option that cuts Wehlen's leverage (`Diplomacy_Wehlen_NegotiatingPower -1/-2`).

### Turn 4: The handover is delayed

**FPnT_AlvarezCall** (Lespia). Alvarez offers help "with Zille" bundled with the Aureus gas deal. Accepting sets `Diplomacy_Lespia_WehlenHelp`, `Zille_ViolentResistancePower +3` and `Zille_ReferendumPower +2` (see section 2).
- Irony: Lespia turns out in Turn 8 to be behind the bombing.

**FPnT_ANSouthMerkopa** (AN session)
- Smolak invokes the "AN Threat Mitigation Protocol for Treaty Obligations to delay the return of Zille until that risk is neutralized" (`[132]`).
- He justifies it by Rumburg's presence to Wehlen's north and south and by the Wehzeks living in Zille (`[146]`).
- "It is not my intention to keep custody of the Zille region forever" (`[155]`).
- If Lespia is helping: `[221]` "Wehlen's actions in Zille should be considered an illegal occupation of Rizian territory."
- The player's reply ends in appeasement or aggression:
  - `[427]` sets `RiziaDLC.Diplomacy_Wehlen_ANSpeech_Appeasement` (+1 NegotiatingPower).
  - `[428]` sets `RiziaDLC.Diplomacy_Wehlen_ANSpeech_Aggression` (−1). Aggressive lines include `[383]` "we will be forced to take matters into our own hands" and `[376]` "I call on the rest of you to choose a side."

**P_EstelaUpdate.** Estela is the hawk: "And then stab the bastards in the back when they least expect it" (`[45]`). "Pales is not ours to control in the first place. Zille, on the other hand..." (`[192]`).

### Turn 5: Treaty with Smolak, resistance funding, Morella

**FPnT_WehlenNegotiations**

*Council briefing.*
- `[89]`/`[90]` Esquibel: Smolak "is in a position to hold Zille hostage until his demands are met".
- `[274]`: asking about fallback plans sets `RiziaDLCIsolated.Turn05_CouncilBriefing_BackupPlan`.
- `[283]` Lucita: "No state peacefully gives power away."
- `[294]`/`[310]` Lucita: "**Zille is cut off from the Wehzek mainland. Divert enough funding and weapons to the right groups, and we can make an overthrow happen without getting our hands dirty.**"
- `[295]`: "President Smolak may not respect a peaceful transition. But he will respect that."
- `[307]`: "Zille doesn't need a referendum, Mr. Esquibel. It needs a resistance." Esquibel replies `[309]`: "...Wehlen will have to give the region back. Or at the very least hold a referendum."
- Hugo may push activating Su Omina in Zille (`[1287]`–`[1306]`).

*With Smolak.*
- `[405]`: "Zille only became Wehzek territory in the first place because your father asked my predecessor to crush a people's rebellion."
- His demands:
  - a trade deal (oil for gold, gas, wine, arms);
  - a Bludish/Derdian border ban;
  - naval help against BFF insurgents fleeing south "towards Zille" (`[611]`–`[616]`);
  - full rights for Wehzeks;
  - a **three-year probationary period** before handover (`[767]`).
- If NegotiatingPower is at least 4, he adds a secret demand to extradite Rico (`[991]`–`[1119]`).
- On a threat, Smolak answers: "Wehlen's not very far away, after all" (`[1173]`).
- Outcome: a deal (`FairAgreement` or `LopsidedAgreement`) or a refusal (`RiziaDLC.Diplomacy_Wehlen_AgreementRefused = true`). Refusal is the master "no treaty" flag for Turns 6–8.

**SnO_InternalSecurity**
- The SO resistance update: SO focuses "on guerrilla warfare, sabotage of essential services" (`[206]`).
- If both groups are funded, the ATA and SO clash (`[733]`–`[742]`).
- The bombing investigation is redirected to Pales, Wehlen or SO, and Rico can be framed.

**FPnT_MorellaNegotiations / EnT_MorellaNegotiationFeedback**
- The player may ask Morella's PM Alma Saltana for help with Zille: diplomacy at the AN, or covertly shipping supplies across the Gulf of Moridia to the resistance.
- Saltana asks: "Is the resistance you're supporting violent, or non-violent?" (`[284]`).
- The supplies go to the ATA (+2 ReferendumPower) or to SO (+1 ViolentResistancePower).

**FPnT_MITZBoardMeeting.** Breaking a promise to Lespia costs `Zille_ViolentResistancePower -3`, `Zille_ReferendumPower -2` and `Diplomacy_Lespia_WehlenHelp = false` (`[388]`).

### Turn 6: Pales war breaks out; Zille in the background

- The Pales war starts: `RiziaDLC.Pales_War = true` and `RiziaDLC.War_Happened = true` (`Turn_6_SnO_WarEruptionBriefing [498]`).
- **P_Cemetary.** Estela: "my son is determined to embark on some military expedition when his attention should be focused on the rightful restoration of Zille" (`[77]`). The player's Zille vows have no DO.
- **A_ReligionBriefing.** The player can ask the Grand Wiseman about "reclaim[ing] Zille through less peaceful means" (`[741]`, `[1227]`). This is hidden when `Diplomacy_Pales_DeclareWar` or `_PrepareForWar` is set.
  - Sal answers: "the scriptures acknowledge just wars" (`[773]`).
  - Derdian warriors "would be an invaluable asset" (`[656]`), but this "could anger President Smolak" (`[680]`).

### Turn 7: Proxies (Derdia, BFF) and the HoD debate

**FPnT_DerdiaNegotiations, then Fighting Assistance (c381 Friendly / c380 Unfriendly)**
- Speaker: Supreme Wiseman Jorga Azmal.
- **Branching is on `RiziaDLC.Pales_War`.**
  - At war: you can buy missiles and/or mercenaries *for Pales*.
  - At peace: "We are at risk of losing the territory of Zille to Wehlen. I would humbly call upon your great warrior culture for assistance."
- Friendly `[77]`: "my Disciples are eager to help Rizia reclaim its land from the anti-Golcondist Wehzeks" (free mercenaries).
- If Rizia is in Wehlen's anti-BFF "Operation Bear Trap", Jorga demands that Rizia quit first: "My men will not fight for a kingdom that is complicit in the persecution of the Bludish people" (`[68]`).
- Unfriendly `[38]`: "Together, we will emancipate Zille and humiliate Wehlen."

**FPnT_RumburgMeeting, then BFF Convo** (Queen Beatrice, Esquibel)
- Rumburg offers its Bludish Freedom Front contacts. `[933]`: "We have certain contacts within Wehlen who could stir up trouble in Zille. Possibly enough to overthrow the government."
- In the war branch, Beatrice demands that the player break the Wehlen deals. `[686]`: "**Then you must decide. Zille or Pales?**"

**A_HouseOfDelegatesII, then Zille_Treaty_Discussion (c386).** The chair is Manus Sazon (RPP) or Daria de Rava.
- `[19]` Daria: "A peaceful transfer might be our goal, but it was never President Smolak's. My party supports the reclamation of Zille **by any means necessary**."
- `[186]`/`[190]` Manus: "*If* Wehlen tries anything, my province can handle it."
- The debate is about whether Zille goes back to Brenas or Rizia Imperii.
- **Pales_War_Update_And_Tax (c385).** If at war, it comes first. `[18]` Manus: "It is difficult for my party to support an all-out attack on the Grand Duchy, especially when the future of Zille is still in doubt."

### Turn 8: Evidence, contingency, the airship, the AN

**SnO_SecurityBriefAndZille** (Lucita, Taddeus or Titus as security councillor)
- Smolak's "three-year probationary period" is ending (`[857]`).
- **The investigation finds a Lespian spy** at Lespia's Zille consulate (Héctor Ramiro or Viktor Karnov). This sets `RiziaDLC.Zille_Discovered_LespianInvolvement = true` (`[97]`). The player may then publicise it.
- If `Zille_ViolentResistancePower >= 4`, SO sabotage kills the courier and the lead is lost (`[1498]`–`[1548]`).
- **Contingency hub.** `[59]`: "we can either approach this diplomatically or, if necessary, take a more... *aggressive* stance."
  - The hostile briefing reports the armed-resistance tier: "reaching a boiling point" / "gaining momentum" / none.
  - It also covers Morellan weapons shipments, and "a number of Golcondist mercenaries in the region, awaiting our command" (`[1963]`).
  - If PowerProjection is Strong or Overwhelming: "Smolak respects a powerful army. If we play our cards right, we may be able to bluff him into submission" (`[1999]`).
  - **`[2122]` Player: "Who said anything about bluffing? Let's bomb the bastards."** This is gated on `Situation_Diplomacy_PowerProjection_Strong` or `_Overwhelming` (`[2116]`).
    - `[2127]` Lucita (Taddeus and Titus have the same line): "As much as our armed forces have improved, **a direct military intervention in Wehlen is not within our current scope.**"
    - If `Pales_War`: `[2138]` Titus: "Besides, we have our hands full with Pales."
    - No DO and no follow-up.
- The final decision is the ATA/SO contingency menu (section 2).

**RustyCall / FPnT_AlvarezVisit.** These are alternative routes to the Lespia evidence: discover, destroy or publish it. Publishing (`RiziaDLC.Zille_Publicized_LespianInvolvement`) is worth +5 negotiation points, or −5 if the scapegoats were used.

**FPnT_ZilleNegotiations** (Smolak's airship over Zille). This is the main peaceful resolution. It is scored silently (section 3.1).
- `[160]` Player: "I'm here to ensure the removal of **Wehlen forces** from the territory of Zille."
- On success: `[427]` "As far as I'm considered, Zille is yours." He may ask for Estela's childhood home first (`[449]`).
- On failure, Smolak proposes a referendum. `[505]`: "It should be up to the people of Zille to decide."
- With very low points: `[792]`/`[793]` "a deal's a deal... If I stop following through with my threats -- sorry, promises -- no one will take me seriously."
- If the player refuses the referendum:
  - Smolak: "I will present my case for a referendum on Zille to the Alliance of Nations" (`[539]`).
  - The player can threaten, `[543]` "I'm prepared to pursue other avenues to achieve our objectives. Consider this your one and final warning." That line is gated at `[541]` on strong or overwhelming PowerProjection, or Derdian/BFF mercenaries, or SO/ATA active, or `Diplomacy_Wehlen_FundBFF`. It sets no variable.

**FPnT_ANSession, then Zille_Territory_Issue (c404).** This runs only if `ZilleReturn_Success` or `ZilleReferendum_Disapproved` (`ANSession [30]`).
- After a successful negotiation, Smolak announces the transfer and revokes the Threat Mitigation Protocol.
- Otherwise he demands a referendum.
  - `[121]`–`[123]` (if Strong armed resistance): "My government has found direct links to a massive underground armed resistance... We found Rizian weapons in the hands of these terrorists!"
  - The player speech includes `[203]` "If he makes a single move to prevent the return of Zille, we will respond", `[207]` (asking for AN peacekeepers) and `[208]` "With or without the help of Alliance of Nations, we will defend our territory." All are flavour only.
  - Then the AN votes (section 3.2).

### Turn 9: The Smolak call, the referendum, the return

**FPnT_CallWithSmolak** (Romus in a pool at Palas Toras).
- `[44]`/`[45]` "I use my army... to help you win your war in Pales."
- `[53]` "**I'm offering a trade. Zille for Pales.**"
- `[189]`/`[172]` "After we help you win the war, Rizia and Wehlen will sign a land transfer agreement, just like your father did... this time, our ownership of Zille will be permanent."
- If you threaten him after accepting: `[82]` "Should you betray me, I'll make sure both you and your nation suffer the consequences."
- If you reject, you can say `[196]` "I'll see you at the Zille return ceremony."

**FPnT_ZilleReferendum** (TV night in Jales with the security councillor and Esquibel)
- Armed militants in ski masks take over the polling stations. If Strong: `[124]` "A massive number of armed militants had taken over the streets of Zille... The police were nowhere to be found."
- The councillor: `[127]` "We must act, Your Majesty... We have all of the contingency plans ready to launch at your command." Esquibel: `[182]` "I really would caution against doing anything rash."
- The decision at `[198]` is army, agents or nothing (section 2, T9).

**FPnT_ZilleReferendumInterference** gives the army or agent result.
- Army: `[83]`/`[98]` "Our troops entered Zille immediately after I gave the order." `[84]`/`[99]` "**The Wehzek troops fell back as soon as we entered.**"
- Army failure: `[85]` "The citizens themselves started to push our soldiers back. There was heavy fighting in some areas." `[86]` "As the Wehzek troops were falling back, **they intentionally gave all of their equipment to the civilians of Zille.**"
- Afterwards Smolak gives a state address blaming Rizia (`[231]`).

**A_ZilleReturnedSpeech.** The ceremony is in Smolak Square, with the memorial where the grandstand stood. The player chooses whether Zille goes to Brenas/House Sazon or to Rizia Imperii.

**Others**
- `Wehlen_Derdia_Meditation`: if returned, Smolak is "up to my elbows in paperwork for the Zille transition" (`[343]`).
- `IntermerkopumRoundtable` `[682]`: "Just don't ask us to go sending our military into Pales."
- `GRACERumburgMeeting` `[546]` (requires BFF): "Thanks to Rumburg, **we have the power to take the region back by force if the referendum goes south**." This is dialogue only.

**TalkWithManus (c442).** Its `_index.tsv` description is "Talk with titus, plan violent takeover". **It has nothing to do with Zille** (no Zille, Wehlen or Smolak mentions). It is the domestic reform plan: gated on `RiziaDLC.Reform_Aggressive`, the Golden Guard, Rusty's PMC or Adarfo's revolutionaries surround the noble Houses at the Queen Mother's birthday gathering. The variables are `RiziaDLC.BirthdayGathering_ViolentAction*`. `TalkWithTitus` is the counter-coup warning; its only Brenas hit is about violent protests.

### Turn 10–11 and epilogues: reporting

- **Turn 10 Foreign Affairs.** A Zille report, then "epic" choices (section 4).
- **Zille_Reform_Branch.** A PM briefing on reintegration. It runs only if returned.
- **House gatherings.** The Sazons ask for Zille. `Zille_PromisedToHouseSazon` leads to the Birthday_Gathering flip to Brenas.
- **War_Pales_PeaceTreaty.** On the deal path, Smolak is co-victor, takes half the Aureus field and the Port of Pales, and collects Zille (`[545]`, `[546]`, `[550]`, `[552]`).
- **Turn 11 Ending_Speech.** It celebrates Zille only if returned.
- **Epilogue titles.**
- **Civil_War.** "The people of Zille fought by your side" (`RiziaDLCText.ZilleWarAlly`).

---

## 2. Every player decision that affects Zille

Only Zille-relevant DOs are listed. Abbreviations: VRP is `RiziaDLC.Zille_ViolentResistancePower`, RP is `RiziaDLC.Zille_ReferendumPower`, NP is `RiziaDLC.Diplomacy_Wehlen_NegotiatingPower`.

### All writes to the two power meters

| Where | Choice | VRP | RP |
|---|---|---|---|
| `Turn_2_FPnT_DiplomacyBriefing [604]`, `[606]` | Fund the ATA in Zille / in both regions | | +1 |
| `Turn_3_A_VisitToIza [471]` | Accept Rico's SO branch in Zille [-2 Budget, -250 Equipment] | +1 | |
| `Turn_4_FPnT_AlvarezCall [253]`, `[305]`, `[359]`, `[493]` | Accept Lespia's help with Zille (`Diplomacy_Lespia_WehlenHelp = true`) | +3 | +2 |
| `Turn_5_EnT_MorellaNegotiationFeedback [1040]` | Morellan supplies go to the ATA (or no SO active) | | +2 |
| `Turn_5_EnT_MorellaNegotiationFeedback [1208]` | Morellan supplies go to SO | +1 | |
| `Turn_5_FPnT_MITZBoardMeeting [388]` | Broken promise to Lespia (also `Diplomacy_Lespia_WehlenHelp = false`) | −3 | −2 |
| `Turn_7_Fighting_Assistance_Friendly [81]`, `[99]` | Derdian mercenaries for Zille | +1 | |
| `Turn_7_Fighting_Assistance_Unfriendly [98]` | Hire Derdian mercenaries (fee) | +1 | |
| `Turn_7_BFF_Convo_Friendly_ [100]`, `Turn_7_FPnT_RumburgMeeting [1133]` | Fund the BFF and want them in Zille | +1 | |
| `Turn_8_A_RoyalWedding [1431]` | Vina-escape branch (unrelated to Zille) | −2 | |
| `Turn_8_SnO_SecurityBriefAndZille [2410]`/`[2414]`/`[2418]` | Start / increase / cut the ATA | | +1/+1/−1 |
| `Turn_8_SnO_SecurityBriefAndZille [2422]`/`[2426]`/`[2430]` | Start / increase / cut SO | +1/+1/−1 | |

Reads:
- RP is only read as `> 0` in `Turn_7_Zille_Treaty_Discussion [12]`.
- VRP is read as `> 0` (Turn 7 HoD; `MITZBoardMeeting [977]`; `War_Pales_PeaceTreaty [550]`), as `> 2` (with `BeginSOFund`, the Hegel accusation in `Turn_8_Sanctions [12]`) and as `>= 4` (`SecurityBriefAndZille`, the lost Lespia lead).
- The referendum reads only the **Situation tiers**, which the engine derives.

### T2: DiplomacyBriefing (ATA)

- `[598]` "Then let's fund a branch of the Ariana Toras Association in Zille." [-2 Budget], leading to `[604]`:
  - `Situation_Diplomacy_Zille_ATAActive = true`, `Resources_Budget -2`, `Zille_ReferendumPower +1`, `NegotiatingPower +1`, `RiziaDLCSupport.Journal_Turn02_ATA_Zille = true`.
- `[603]` "Let's expand the ATA into both Zille and Pales." [-2 Authority, -4 Budget], leading to `[606]`. This sets the same plus the Pales ATA, `Diplomacy_Pales_NegotiatingPower +1` and `War_Pales_PublicWarSupport -1`.
- `[599]` (Pales only) sets `Situation_Diplomacy_Pales_ATAActive`.
- `[780]` (military options) sets only `RiziaDLCIsolated.Turn02_Diplomacy_Wehlen_War = true`.

### T2: ZilleVisit

- Hostile speech (`[214]` "...If Zille is not returned, he will face Rizia's full retaliation.", or `[227]`) leads to `[223]`:
  - `RiziaDLC.Turn01_ZilleVisit_HostileSpeech = true`, `RiziaDLCSupport.Journal_Turn02_Zille_HostileSpeech = true`, `NP -2`. Also `[224]` `PersonalValue_Pacifist -1`.
- Conciliatory speech sets `[299]` `RiziaDLCSupport.Journal_Turn02_Zille_ConciliatorySpeech = true`.
- After the bombing, always: `[289]` `NP -2`.

### T3

- **Debrief funding.**
  - `[181]` (Lucita) leads to `[186]`: `RiziaDLC.Policy_Order_RizianForeignIntelligence_Expanded = true`, `RiziaDLC.Turn03_Security_LucitaLeadsInvestigation = true`.
  - `[182]` (Titus) leads to `[187]`: `RiziaDLC.Policy_Order_GoldenGuard_Expanded = true`, `RiziaDLC.Turn03_Security_TitusLeadsInvestigation = true`, `Army_Unit_GoldenGuard_Total +1`, `RiziaDLC.Security_GoldenGuard_InvestigateSO = true`.
  - `[183]` (neither) leads to `[445]`: `RiziaDLC.Security_FundNone = true`, `NP -1`.
- **Iza scapegoats.** `[376]`/`[387]`: `RiziaDLC.Diplomacy_Wehlen_Scapegoats = true`, `NP +2`, `RiziaDLCSupport.Journal_Turn03_Zille_Scapegoats = true`.
- **Iza SO branch.** `[463]`/`[464]`, leading to `[471]`:
  - `RiziaDLC.Situation_Diplomacy_Zille_SuOminaActive = true`, VRP +1, `NP -2`, `Resources_Military_Equipment -250`, `Resources_Budget -2`, `RiziaDLC.FundedSO +1`, `RiziaDLC.DeployedSO +1`.
- **Outside the DB:** `RiziaDLC.Decision_Turn03_ZilleIncidentResponse_DenounceSO`/`_DefendSO`/`_Neutral` and `RiziaDLC.Decision_Turn03_ZilleBombApology`.

### T4

- **AlvarezCall**, e.g. `[248]` "Sell me 25% of the gas rights and help me out with Zille...", leading to `[253]`: `RiziaDLC.Diplomacy_Lespia_WehlenHelp = true`, VRP +3, RP +2.
- **ANSouthMerkopa.** `[427]` `RiziaDLC.Diplomacy_Wehlen_ANSpeech_Appeasement` (NP +1). `[428]` `RiziaDLC.Diplomacy_Wehlen_ANSpeech_Aggression` (NP −1).
- **Outside the DB:** `RiziaDLC.Decision_Turn04_WehlenInvestigation_Cooperate` (intelligence sharing with Wehlen; it auto-publicises the Lespia evidence in Turn 8).

### T5

**WehlenNegotiations**

| Entry | Effect |
|---|---|
| `[1080]` | `RiziaDLC.Diplomacy_Wehlen_FairAgreement = true`, NP +1 |
| `[1397]` | `RiziaDLC.Policy_Diplomacy_TradeDeal_Wehlen = true`, `RiziaDLC.Diplomacy_Wehlen_LopsidedAgreement = true`, NP +2 |
| `[1156]`/`[1155]` | `RiziaDLC.Diplomacy_Wehlen_PromiseRicoExtradition = true` (`[1155]` also sets `RiziaDLCIsolated.Turn05_WehlenTreaty_PromiseBFFAid = true`) |
| `[1021]`/`[1160]` | **`RiziaDLC.Diplomacy_Wehlen_AgreementRefused = true`** (`[1021]` also NP −5) |
| `[1472]` | Anti-BFF aid ("Operation Bear Trap"): `RiziaDLC.Situation_Diplomacy_Wehlen_Terrorism_AntiBFF = true`, `Army_Unit_Ship_Total -2`, `Resources_Budget +5` |
| `[1476]` | `RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishBan = true` |
| `[1480]` | `RiziaDLC.Situation_Order_Wehlen_Terrorism_BludishDerdianBan = true` |
| `[1484]` | `RiziaDLC.Situation_Law_WehlenRights_Full = true` |
| `[1488]` | `RiziaDLC.Situation_Law_WehlenRights_WorkersOnly = true` |
| `[1438]`–`[1468]` | Trade: `Situation_Diplomacy_Wehlen_OilImport`, `_ImportDrugs`, `_ImportTobacco`, `_ImportTimber`, `_ExportArms`, `_ExportGas`, `_ExportGold`, `_ExportWine` |

**MorellaNegotiations**
- `[225]` "We would ask for your aid in taking it back from Wehlen" leads to `[985]` `RiziaDLCIsolated.Turn05_MorellaITZ_NationalizationDemands_Wehlen = true`.
- The demand flags `RiziaDLC.MITZ_Demand_ZilleDiplomacy` and `RiziaDLC.MITZ_Demand_ZilleResistance` are set by the panel, outside the DB, and can be cancelled in Feedback (`[441]`/`[460]` set them to `false`).
- Feedback `[1201]`/`[1207]`: `RiziaDLCIsolated.MorellaZilleResistance_ChooseATASupport = true`, then `[1040]` (RP +2) or `[1208]` (VRP +1).

**InternalSecurity**
- `[219]`: `RiziaDLC.Decision_Turn05_ZilleResistanceFunding_BeginSOFund = true`, `RiziaDLC.Decision_Turn05_ZilleResistanceFunding_IncreasedSOFund = true`. This is bookkeeping set on the exit; the real choice is a decision card.
- `[755]`: `RiziaDLC.Decision_Turn05_ZilleResistanceFunding_FundBothSOATA = true`.
- `[769]`/`[767]`: `RiziaDLC.Turn05_Security_ResistanceATASODialogueYes` / `No`.
- `[540]`: `RiziaDLC.Turn05_Security_InvestigateBombingWehlen = true`. `[539]`: `RiziaDLC.Turn05_Security_InvestigateSuOmina = true`.
- `[928]`: `RiziaDLC.Rico_FrameForWehlenBombing = true`.

### T7

**Fighting Assistance, Friendly (peace branch)**
- `[79]`/`[82]` lead to `[81]`: VRP +1, `RiziaDLC.Resources_Military_DerdianMercenaries = true`, `RiziaDLC.Resources_Military_DerdianMercenaries_Zille = true`.
- `[84]` "Rizia will withdraw from Wehlen's anti-BFF operations in exchange for... Derdian mercenaries in the Zille resistance movement" leads to `[99]`. This sets the same as `[81]`, plus `Situation_Diplomacy_Wehlen_Terrorism_AntiBFF = false`, `Army_Unit_Ship_Total +2` and `RiziaDLC.Diplomacy_Wehlen_PromisesBroken_AntiBFF = true`.

**Fighting Assistance, Unfriendly**
- `[58]` leads to `[102]`: the mercenary flags and `PromisesBroken_AntiBFF`, but **no VRP** (an inconsistency).
- The hire `[66]` leads to `[98]`: VRP +1, Budget −1, but **no mercenary flags** (an inconsistency).

**Fighting Assistance, war branch** (Pales only)
- `[50]`: `Resources_Military_DerdianMercenaries_Pales`, `Army_Unit_Mercenary_Total +1`.

**DerdiaNegotiations**
- `[272]`: `RiziaDLC.Diplomacy_Wehlen_PromisesBroken_DerdianBorderBan = true`.
- `[276]`: `RiziaDLC.Diplomacy_Wehlen_PromisesBroken_BludBorderBan = true`.

**RumburgMeeting**
- `[571]`/`[577]` (lift the Blud ban): NP −3, `PromisesBroken_BludBorderBan`.
- `[572]`/`[578]` (quit Bear Trap): NP −4, `PromisesBroken_AntiBFF`.
- The non-war twins `[1027]`/`[1033]` omit the PromisesBroken flags.
- `[1130]`/`[1132]`: `RiziaDLC.Diplomacy_Wehlen_FundBFF = true` (BFF stays north).
- `[1131]`/`[1133]`: `Diplomacy_Wehlen_FundBFF = true`, VRP +1, `RiziaDLCSupport.News_Turn07_G_BFFZilleActivitySpike = true`.
- `[667]`/`[668]` (BFF in Pales): `RiziaDLC.Resources_Military_RumburgianBFFMercenaries = true`, `Army_Unit_Mercenary_Total +1`.

**BFF Convo** `[41]`/`[98]` lead to `[100]`: VRP +1, `Diplomacy_Wehlen_FundBFF = true`.

**Outside the DB:** `RiziaDLC.Decision_Turn07_ZilleResistanceFundingTwo_*` (`BeginATAFund`, `IncreasedATAFund`, `BeginSOFund`, `IncreasedSOFund`, `FundBothSOATA`, `NoFund`).

### T8: SecurityBriefAndZille contingency menu (`[2327]`)

The Isolated flags are spelled `Contigency`, with the "n" missing. The availability flag is spelled `Contingency`.

| Choice | Flags (`RiziaDLCIsolated.Turn08_ZilleContigency_*`) |
|---|---|
| `[2329]` / `[2432]` wait / do nothing | `RiziaDLC.Turn08_Security_Brief_ATASODoNothing = true` |
| `[2347]` increase ATA | `IncreaseATA` |
| `[2354]` SO out, money to ATA | `IncreaseATA`, `CutSO` |
| `[2355]` cut ATA, more SO | `IncreaseSO`, `CutATA` |
| `[2362]` both up | `IncreaseSO`, `IncreaseATA` |
| `[2369]` ATA up plus SO in | `StartSO`, `IncreaseATA` |
| `[2370]` swap ATA for SO | `CutATA`, `StartSO` |
| `[2378]` send ATA | `StartATA` |
| `[2386]` both in | `StartATA`, `StartSO` |
| `[2328]` support SO | `IncreaseSO` |
| `[2374]` stop SO | `CutSO` |
| `[2436]` cut SO, send ATA | `StartATA`, `CutSO` |
| `[2382]` "Talk to Ricardus about activating Su Omina in Zille" | `StartSO` (needs `RiziaDLCIsolated.Turn08_ZilleContingency_SuOminaAvailable`) |

Executors:
- `[2410]` StartATA: Budget −2, `ATAActive = true`, RP +1.
- `[2414]` IncreaseATA: Budget −1, RP +1.
- `[2418]` CutATA: Budget +1, RP −1, `ATAActive = false`.
- `[2422]` StartSO: Budget −2, `SuOminaActive = true`, VRP +1, Equipment −500.
- `[2426]` IncreaseSO: Budget −1, VRP +1, Equipment −250.
- `[2430]` CutSO: Budget +1, VRP −1, `SuOminaActive = false`.

The Lespia evidence:
- `[1026]`/`[1223]` lead to `[1207]`/`[1224]`, and `[1088]` sets it automatically when cooperation was on: all set `RiziaDLC.Zille_Publicized_LespianInvolvement = true`.
- Other routes: `Turn_8_FPnT_AlvarezVisit [1039]` (publish) and `[610]`/`[822]`/`[989]` (`RiziaDLC.Zille_Destroyed_LespianEvidence`); `Turn_8_FPnT_ANSession [154]` (`RiziaDLC.Zille_Publicized_LespianInvolvement_Rusty`).

### T8: ZilleNegotiations

The flow is in section 3.1.
- Concessions:
  - `[729]` oil: `RiziaDLC.Turn08_OilDealSmolak = true`, ReturnPoints +2, NP +1, Budget −3.
  - `[759]` gold: `RiziaDLC.Turn08_GoldDealSmolak = true`, same costs.
  - `[760]` arms: `RiziaDLC.Turn08_ArmsDealSmolak = true`, ReturnPoints +2, NP +1, Equipment −1000.
- `[781]` give Estela's home: `RiziaDLC.Turn08_ZilleNeg_GiveSmolakEstelasHome = true`.
- `[1094]` share the Lespia evidence: `RiziaDLC.Wehlen_Zille_SharedEvidence = true`, `Zille_Publicized_LespianInvolvement = true`. This comes after scoring, so it adds no points.
- `[1110]` wedding invitation: `RiziaDLC.Turn08_Wehlen_SmolakInvitedToWedding = true`.
- `[1009]`/`[1010]`/`[1015]` keep Rico (refuse extradition, he marries Vina, he becomes police chief): ReturnPoints −1 each.
- `[526]`/`[531]` agree to a referendum: **`RiziaDLC.ZilleReferendum_Approved = true`**.
- `[1153]` refuse it (or it is forced): **`RiziaDLC.ZilleReferendum_Disapproved = true`**.

### T8: Zille_Territory_Issue (AN)

- Interrupting Smolak (`[396]`) or attacking him (`[397]`): NP −2 each.
- The speech-topic flags are `RiziaDLCIsolated.ANZilleSpeech_Agreement` / `_Smolak` / `_PeopleOfZille`. **The speech does not move the vote.**

### T9

- **CallWithSmolak.** Accept `[61]`, leading to `[67]`:
  ```
  RiziaDLC.War_Pales_Won = true
  RiziaDLC.War_WehlenJoined = true
  RiziaDLC.ZilleReturn_Success = false
  RiziaDLC.ZilleReferendum_SmolakOffer_Accepted = true
  RiziaDLCSupport.Journal_Turn09_CallWithSmolak_OfferAccepted / News_Turn09_{G,RH,V}_SmolakCall_WehlenJoinsWar / Reports_Turn09_MaveNortus_SmolakCall_WehlenJoinsWar = true
  ```
  Reject `[87]`/`[204]`: `RiziaDLC.ZilleReferendum_SmolakOffer_Rejected = true`.
- **ZilleReferendum `[198]`.**
  - `[199]` "We will interfere with the Rizian Armed Forces. Our troops will enter Zille to protect the ballot boxes and make sure we get what is ours back." leads to `[208]` **`RiziaDLC.ZilleReferendum_Interfere_Army = true`**.
  - `[207]` "Activate all of our agents in Zille..." leads to `[209]` `RiziaDLC.ZilleReferendum_Interfere_Agents = true`.
  - `[200]` "We will not interfere with the referendum in any way." sets nothing.
- **ZilleReturnedSpeech.**
  - `[213]` Brenas: `RiziaDLC.ZilleOwnership_Brenas = true`, `Authority_Modifier_TerritorialReclaimation +1`, codex `Locations_Regions_ZilleTerritory_Brenas`.
  - `[214]` Imperii: `RiziaDLC.ZilleOwnership_RiziaImperii = true`, `Authority_Modifier_TerritorialReclaimation +2`.
  - Both continue to `[235]`: `ZilleReturn_Success = true`, `RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol = false`, `RiziaDLC.Policy_Diplomacy_ZLLAActive = false` (initial value True; the lease agreement).
  - Map tokens: `[236]`–`[239]` call `AddTokenStatusImmediately("Rizia_City_Zille"/"Rizia_City_Nanta", "Rizia_City_HouseSazon" | "Rizia_City_HouseToras")`.
  - Wehzek protesters (interference only): `[130]` remove them (Democratic −1) or `[163]` ignore them (+1).

### T10

- **Sazon conversations** `[555]`/`[1168]`/`[1259]` and REFORM_TALKS `[881]`: `RiziaDLC.Zille_PromisedToHouseSazon = true`. Birthday_Gathering `[174]` then flips ownership to Brenas.
- **Foreign Affairs** choices are in section 4.

---

## 3. Outcomes and the exact gates

### 3.1 Turn 8 airship negotiation (`Turn_8_FPnT_ZilleNegotiations`)

**Silent pre-pass** scoring `RiziaDLC.Turn08_ZilleNegotiations_ReturnPoints` (starts at 0).

Treaty audit. This runs only if `Diplomacy_Wehlen_AgreementRefused == false`. Each broken promise adds 1 to `RiziaDLC.Turn08_ZilleNegotiations_TreatyPromisesBroken` (entries 61–68):
- gas commitment failed
- wine damaged
- gold not deliverable
- arms (equipment ≤ 249)
- `PromisesBroken_BludBorderBan`
- `PromisesBroken_DerdianBorderBan`
- `PromisesBroken_AntiBFF`
- Rico promised but still free

The result at `[58]`:

| Broken promises | Effect |
|---|---|
| 0 | `[69]` `RiziaDLC.Turn08_ZilleNegotiations_TreatyKept = true`, +5 |
| 1 | +3 |
| 2 | +2 |
| 3 or more | +0 |

Separately, `Rico_Deposed` gives `[57]` +1 points and +1 broken.

Modifiers (always):

| Condition | Effect |
|---|---|
| `Zille_Publicized_LespianInvolvement`, without scapegoats | +5 |
| `Zille_Publicized_LespianInvolvement`, with `Diplomacy_Wehlen_Scapegoats` | −5 |
| PowerProjection Strong or Overwhelming | ±1 |
| `Situation_Diplomacy_CulturalProjection_Influential` | ±1 |
| Rumburg Unfriendly or Hostile | ±1 |
| NP ≥ 5 | +1 |
| NP < 0 | −1 |
| During `Pales_War`: `War_Pales_Operation1_Won` | ±1 |
| `Resources_Military_RumburgianBFFMercenaries` or `Diplomacy_Wehlen_FundBFF` | −2 |

Then NP +1 if points ≥ 5, and NP +3 if points ≥ 9.

**Gate after the tour** (`[880]`):
- Treaty signed (`AgreementRefused == false`), at `[412]`:
  - points **≥ 8**: Smolak yields.
  - **3–7**: the concession hub (oil, gold, arms, +2 each). Reaching ≥ 5 makes him yield; otherwise it goes to the referendum branch.
  - **≤ 2** (`[787]`): forced no-return, leading to `[1153]` Disapproved.
- Treaty refused, at `[883]`:
  - `NP <= 4`: straight to the referendum branch (`[886]`).
  - `NP >= 5`: the same 3/8 test as above.

**"Smolak yields" path** (`[1075]`):
- Points **≥ 9**: free return.
- Points **≤ 8**: he asks for Estela's home (`[429]`).
  - Give it (`[781]`): return.
  - `Trait_FunnyKing` insult (`[777]`): return.
  - Overwhelming-power bluff (`[1185]`–`[1190]`, gated on `Situation_Diplomacy_PowerProjection_Overwhelming`): "we now command one of the strongest armies on the Merkopan continent". Smolak laughs it off and returns Zille. **This is the only place where military strength mechanically wins Zille.**
  - Refuse: one deal is retracted (points −2), then the referendum branch.

**Success:** `[1082]` `RiziaDLC.ZilleReturn_Success = true` (plus the `Rizia_ZilleReturn` achievement).

**Referendum branch** (`[1217]`):
- Accept: `ZilleReferendum_Approved` (no AN item).
- Refuse: `ZilleReferendum_Disapproved`, then the AN item.

### 3.2 Turn 8 AN vote (`Turn_8_Zille_Territory_Issue`)

It runs if `ZilleReturn_Success or ZilleReferendum_Disapproved` (`ANSession [30]`). After a return it is ceremonial (`[342]` `Situation_Diplomacy_AN_ThreatMitigationProtocol = false`).

Otherwise every delegate votes, changing `RiziaDLCIsolated.ANZilleReferendum_Vote` (Aye +1 means "allow the referendum"). The votes depend only on relations, `Situation_Diplomacy_GlobalImage_Bad` and `Zille_Publicized_LespianInvolvement_Rusty`:
- Smolak always Aye.
- Beatrice always Nay.
- Morella always Nay if `MITZ_Demand_ZilleDiplomacy`.
- Pales Aye if `Pales_War`, and absent if `War_Pales_Won`.

Result:
- **Vote > 0** (`[320]`): `RiziaDLC.ZilleReferendum_NotStoppedByAN = true`. The referendum happens in Turn 9.
- **Vote ≤ 0** (`[48]`): `RiziaDLC.ZilleReferendum_StoppedByAN = true` **and `RiziaDLC.ZilleReturn_Success = true`**. `[423]` adds "Peacekeeping troops shall be sent to ensure the smooth transition of Zille to Rizia."

### 3.3 Turn 9 Smolak deal ("Zille for Pales")

`CallWithSmolak [67]`, quoted in section 2. It **forces `ZilleReturn_Success = false`, even if Zille was already returned** (`[167]`–`[174]`: "It's never too late for another agreement").

It is paid off in `Turn_10_War_Pales_PeaceTreaty [543]`–`[552]`:
- Smolak: "The paperwork for the permanent transfer of Zille to Wehlen is already on its way to Porte Drazon."
- `[550]` (if VRP > 0): "any attempt at - ahem - *grassroots* resistance will be met with the same firepower that just won you Pales."
- `[552]` runs `DisableToken("Events_Zille")` and sets `RiziaDLCSupport.Reports_Turn10_Wehlen_ZilleTransferred`, `Authority_Modifier_TerritorialIntegrityLost -1`, `Situation_Diplomacy_AN_ThreatMitigationProtocol = false` and `Policy_Diplomacy_ZLLAActive = false`.

How the referendum conversation behaves after the deal is turn-data driven, and so unknown.

### 3.4 Turn 9 referendum (`Turn_9_FPnT_ZilleReferendum`)

It presumably runs when Approved or NotStoppedByAN (scheduling is outside the DB).

Entry flags:
- `[107]` (`MassiveRiziaMovement or GrowingRiziaMovement`) leads to `[109]` `RiziaDLC.ZilleReferendum_EarlyResult_Win`.
- `[111]` (`StrongArmedResistance or GrowingArmedResistance`) leads to `[113]` `RiziaDLC.ZilleReferendum_ArmedResistance`.
- `[115]` leads to `[117]` `RiziaDLC.ZilleReferendum_BothSidesClash`. The condition, verbatim:
  ```
  (Strong AND Massive) or (GrowingArmed AND GrowingRizia) and Reform_Path == true
  ```
  Lua precedence makes this `(Strong∧Massive) ∨ ((GrowingArmed∧GrowingRizia) ∧ Reform_Path)`.

**No interference:**
- Massive (68%) or Growing (51%) is a win.
  - With BothSidesClash, the result is **independence** (`[310]`).
  - Otherwise `[342]` `ZilleReferendum_Won` and `[322]` `ZilleReturn_Success = true`.
- Neither (32%) is a loss: `[262]` `RiziaDLC.ZilleReferendum_Lost = true`, `Situation_Diplomacy_AN_ThreatMitigationProtocol = false`, `Policy_Diplomacy_ZLLAActive = false`. Zille stays Wehzek.

**Independence flavour** (`[301]`, then `[294]`, then `[297]`):
- Both `Resources_Military_DerdianMercenaries` and `RumburgianBFFMercenaries`: Golcondist.
- Else `DerdianMercenaries_Zille`: Wruhecist.
- Else BFF: Bludish.
- Else Liberation (ZLF).

Each sets `RiziaDLC.ZilleReferendum_Independence` plus `_Golcondist` / `_Wruhecist` / `_Bludish` / `_Liberation`. It sets the string `RiziaDLCText.IndependentZille`, runs `EnableToken("Rizia_Country_Zille")` and `UpdateCountryRelationship("Rizia_Country_Zille","Neutral")`, and unlocks `Rizia_FreeZille`.

### 3.5 Turn 9 interference (`Turn_9_FPnT_ZilleReferendumInterference`)

**Army** (`[12]` and `[13]`, verbatim conditions):
```
[12] IF Pales_War == true or (PowerProjection_Weak == true or PowerProjection_Average == true)
      -> [289] IF Situation_Diplomacy_Zille_StrongArmedResistance == true and Situation_Diplomacy_Zille_SuOminaActive == true -> [15] SUCCESS else [14] FAIL
[13] otherwise (no Pales war AND projection Strong/Overwhelming) -> [15] SUCCESS
```
- `[15]` `RiziaDLC.ZilleReferendum_Interfere_Army_Success`. `[14]` `RiziaDLC.ZilleReferendum_Interfere_Army_Fail`.
- **This is the vanilla rule for "can Rizia's army take Zille": in peacetime with a strong army it always works; during the Pales war it works only if Su Omina and a strong armed resistance carry it.**

**Agents:**
- If `Situation_Order_ForeignIntelligence_Ineffective`, success needs `StrongArmedResistance and (DerdianMercenaries_Zille or RumburgianBFFMercenaries)`.
- Otherwise they succeed.

**Results:**

| Outcome | Entry | Vote | Sets |
|---|---|---|---|
| Army success | `[228]` | 82% | `ZilleReferendum_Won` |
| Agents success | `[230]` | 72% | `ZilleReferendum_Won` |
| Army fail | `[227]` | 64% for Wehlen | `ZilleReferendum_Lost` |
| Agents fail | `[229]` | 92% for Wehlen | `ZilleReferendum_Lost` |

- Agents fail also leads to `[250]`: `ZilleReferendum_Independence = true` and `RiziaDLC.CountryRelation_Wehlen_Unfriendly = true`, with independence flavour `[273]`–`[276]` and `UpdateCountryRelationship(...,"Unfriendly")`.
- After a win, `[287]` sets `ZilleReturn_Success = true` and **`RiziaDLC.CountryRelation_Wehlen_Hostile = true`**.
- After a non-independence loss, `[278]` sets `CountryRelation_Wehlen_Hostile = true`.
- Wehlen never escalates beyond relations.

### 3.6 Outcome table, and where each is reported

| Outcome | Defining flags | Reported in |
|---|---|---|
| Negotiated return (T8) | `ZilleReturn_Success` (`ZilleNegotiations [1082]`) | `ZilleReturnedSpeech` (news `..._ZilleReturnSpeech_Negotiations`); T10 Foreign Affairs `[265]`+; Zille_Reform_Branch; T11 speech; Epilogue titles; Civil War ally |
| AN blocks referendum (T8) | `ZilleReferendum_StoppedByAN` + `ZilleReturn_Success` | as above; the speech shows AN peacekeepers `[302]`; Sazon convo "AN forces around to keep the peace" `[975]` |
| Referendum won (T9) | `ZilleReferendum_Won` + `ZilleReturn_Success` | as above; news `..._RefVote` |
| Won by interference | + `ZilleReferendum_Interfere_Army_Success` / `_Agents_Success`, `CountryRelation_Wehlen_Hostile` | protesters at the ceremony `[128]`; T10 report `[219]` "continued protests"; Reform branch "accusations of interference" |
| Referendum lost | `ZilleReferendum_Lost`, `ZilleReturn_Success == false` | T10 FA `[227]` "remains unchanged under Wehlen's control" + `[205]` crackdown; Election_Issues "Losing Zille..."; Estela `RelationshipAnnouncement [353]`; Epilogue `Title_Frailer` |
| Independent Zille | `ZilleReferendum_Independence` + one of `_Golcondist` / `_Wruhecist` / `_Bludish` / `_Liberation` | T10 FA `[223]`–`[226]` (border clashes, alliance offer `RiziaDLCIsolated.Turn10_NewZille_AllianceOffered`); Continued_Reign `[188]`+; Exile and Peaceful epilogues (Estela's last wish) |
| Permanent transfer (Smolak deal) | `ZilleReferendum_SmolakOffer_Accepted`, `War_WehlenJoined`, `War_Pales_Won`, `ZilleReturn_Success = false` | Pales PeaceTreaty `[543]`–`[552]`; Victory Parade (Wehzek troops `[204]`); T10 FA `[919]` "Our permanent transfer of the territory..."; Azaro and Sazon convos |
| Negotiation failed, Approved, no referendum result | none of the above | falls into the "unchanged" text (the report cannot tell lost from failed) |

**Turn 11 `Ending_Speech`** has Zille lines only for `ZilleReturn_Success`:
- `[179]` "despite President Smolak's best attempts to stop us, we finally managed to bring Zille back to Rizia"
- `[181]` "Zille has returned to our kingdom after a long 30 years"
- "Great Unification" if Pales is also annexed or unified
- `NewGov_Epic_GulfOfRizia` changes one line (`[961]`)

**`Epilogue`:**
- `Title_Liberator` / `Title_Great` need `ZilleReturn_Success` together with Pales annexation or unification.
- `Title_Peaceful` needs `War_Happened == false and Situation_Diplomacy_Zille_WeakArmedResistance and ZilleReferendum_Interfere_Army == false and ZilleReferendum_Interfere_Agents == false`.
- `Title_Frailer` is triggered by `ZilleReferendum_Lost` combined with a Pales loss.

**`Civil_War [380]`–`[382]`.** If `ZilleReturn_Success and (Country_HouseSazon_Enemy == false or ZilleOwnership_RiziaImperii)`, it sets `RiziaDLC.Epilogue_CivilWar_ZilleAlly` (never read) and `Epilogue_CivilWar_WinPoints +1`, plus the text `RiziaDLCText.ZilleWarAlly = "The people of Zille fought by your side as well."`.
- **`ZilleWarAlly` is not about a Zille war.** It is the "who fought for you" tally in the revolution epilogue.

**Variables that exist but are never set or read in any conversation:** `RiziaDLC.MapLayer_Zille_Brenas`, `RiziaDLC.MapLayer_Zille_OwnState`, `RiziaDLC.MapLayer_World_Zille_Rizia`, `RiziaDLC.MapLayer_World_Flag_Zille`. They are presumably engine and map driven, and almost certainly what repaints the map, so they must be set on a mod victory.

---

## 4. Turn 10 epic choices (Foreign Affairs)

**If returned** (`[690]` hub):

| Entry | Choice | Sets |
|---|---|---|
| `[705]` | Zille development zone [-5 Budget] | `RiziaDLC.NewGov_Epic_DrillResources_Zille` (read by Domestic Affairs `[626]`) |
| `[701]` | Smolak statues become a reef | `RiziaDLC.NewGov_Epic_SmolakReef` |
| `[707]` | "The Great Resettlement" | `RiziaDLC.NewGov_Epic_GreatResettlement` |
| `[706]` | Gulf of Rizia | `RiziaDLC.NewGov_Epic_GulfOfRizia` |

**If not returned** (`[756]` hub):
- Gulf of Rizia.
- `[732]` border loudspeakers: `RiziaDLC.NewGov_Epic_GreatWallOfSound`.
- `[740]` (independent only) currency chaos: `RiziaDLC.NewGov_Epic_CurrencyChaos`.
- `[741]` "Launch a campaign to aid those fleeing Zille and **support the resistance of those who remained**": `RiziaDLC.NewGov_Epic_OperationSanctuary`.
- `[764]` alliance with the independent Zille: `RiziaDLC.NewGov_Epic_IndependentZilleAlliance`.
- `[746]` nothing: `RiziaDLCIsolated.NewGov_Epic_NoAction_Zille`.

---

## 5. Existing hints of military options

1. **Feasibility assessment** (`Turn_2_FPnT_DiplomacyBriefing [780]`–`[795]`).
   - A sea invasion of Wehlen proper is impossible: mines, naval batteries, Contanan radar and air defence, no amphibious lift.
   - **Zille, on the mainland, is "the only front"** for "limited military pressure".
   - The ministers' concern is political: an attack pushes Smolak into United Contana's arms.
2. **Wehlen's presence in Zille, as stated in dialogue.**
   - Three tanks, a dozen soldiers and armed guards at Smolak Square (`ZilleVisit [67]`, `[71]`, `[86]`).
   - "Wehlen forces" in the territory (`ZilleNegotiations [160]`).
   - Wehzek troops present during the referendum, who fall back on contact and arm civilians (`Interference [84]`, `[86]`).
   - Zille police identify Rizian agents (`[112]`). During the militant takeover, "The police were nowhere to be found."
   - Zille is "cut off from the Wehzek mainland" (Lucita, T5 `[294]`).
   - Wehlen has an amphibious capability ("Operation Moonlight Sonata", Wehzek boats landing in Pales, `PeaceTreaty [58]`).
   - The Wehzek army has a "legendary reputation" (`CallWithSmolak [108]`).
   - **No troop numbers, garrison size or naval order of battle appear anywhere.**
   - The cut Turn 8 Notes draft has Wehlen soldiers in formation at the airport.
3. **Threats by the player.**
   - "full retaliation" (T2 `[214]`)
   - "prepared to fight" (T3 Beatrice `[54]`)
   - "take matters into our own hands" (T4 `[383]`)
   - "by *any* means necessary" (T5 `[1025]`)
   - "other avenues... final warning" (T8 `[543]`)
   - "we will defend our territory" (AN `[208]`)
   - None sets a variable.
4. **Threats by Smolak.**
   - "there will be a price to pay" (T2 `[282]`)
   - "Wehlen's not very far away" (T5 `[1173]`)
   - "it is my right to take Zille's safety into my own hands" (T5 `[1007]`)
   - "suffer the consequences" if betrayed (T9 `[82]`)
   - "same firepower that just won you Pales" (T10 `[550]`)
5. **Ministers and others on war with Wehlen.**
   - Shut down: Lucita, Taddeus and Titus at T8 `[2127]`: "a direct military intervention in Wehlen is not within our current scope"; Titus adds "hands full with Pales". Esquibel is cautious throughout ("I knew we could solve this without violence", T9 `[250]`).
   - Hawks: Estela; Daria and the RNC ("by any means necessary"); Lucita on proxies ("overthrow without getting our hands dirty"); the Turn 9 security councillor ("contingency plans ready to launch").
   - Allies: Beatrice (Rumburg, the BFF) and Jorga Azmal (Derdian Disciples) want Wehlen humiliated.
   - Manus: "my province can handle it".
   - Sal Ignacius: "the scriptures acknowledge just wars".
6. **Armed-resistance escalation.** The report variables are set outside the DB. They show a ladder a war mod can reuse:
   - `Reports_Turn02_Zille_BombingReport`
   - `Reports_Turn03_Zille_PostBombingArrests`
   - `Reports_Turn04_Zille_TravelRestrictions` / `_LargeGatheringBan`
   - `Reports_Turn05_Zille_SOSuppliesReceived`
   - `Reports_Turn06_Zille_SORailwayAttack` / `_SORailwayAttackFail`
   - `Reports_Turn07_Zille_SOHarborExplosion`
   - `Reports_Turn07_Zille_DerdianMercenaries`
   - `Reports_Turn08_Zille_SOOfficialAssassination`
   - `News_Turn08_G_ZilleTensionsRise`
   - `Reports_Turn08_Zille_IntelligenceHubActivated` (Mave Nortus)
   - `Reports_Turn09_Nanta_DiscardedBallotsDiscovered`
   - `Reports_Turn10_Zille_WehzekExodus`
   - `Reports_Turn10_Nanta_WehzeksProtest`
7. **Mechanical military levers that already exist:**
   - `RiziaDLC.ZilleReferendum_Interfere_Army` (section 3.5)
   - the T8 Overwhelming-power bluff
   - Derdian mercenaries (`Resources_Military_DerdianMercenaries_Zille`), the BFF and Su Omina as feeders for `Zille_ViolentResistancePower`
   - the Mave Nortus intelligence hub (`RiziaDLC.Policy_RD_Order_BuildMaveNortusIntelHub_Completed`)
8. **"Plan violent takeover" (Turn 9 TalkWithManus).** This is the domestic reform coup against the noble Houses, not Zille (section 1, Turn 9).

---

## 6. Interaction with the Pales war

The two arcs run concurrently. The Pales war starts in Turn 6 (`Pales_War`) and ends in Turn 10 (PeaceTreaty, VictoryParade or Loss). The Zille climax is in Turns 8–9.

Vanilla repeatedly frames them as a **trade-off**:
- **Turn 7, Derdia.** Derdian mercenaries can go to Pales (`Pales_War == true`) or to Zille (`Pales_War == false`), never to Zille during the war.
- **Turn 7, Rumburg.** "Zille or Pales?" (`[686]`). Rumburg's war aid requires breaking the Wehlen promises, which hurts the Turn 8 score.
- **Turn 7, HoD.** The RPP resists the Pales war because Zille is unresolved.
- **Turn 8, Security brief.** "hands full with Pales". The Lespia evidence can be withheld as Pales leverage.
- **Turn 8, airship.** `Pales_War` with `War_Pales_Operation1_Won` gives ±1 point. Arms exports are refused because "We have our own war" (`[596]`).
- **Turn 8, AN vote.** Pales votes Aye during the war, and is absent if `War_Pales_Won`.
- **Turn 9, army interference.** It fails automatically if `Pales_War` unless `StrongArmedResistance` and `SuOminaActive`. **This is the clearest vanilla rule: the army cannot do both fronts.**
- **Turn 9, Smolak.** "Zille for Pales" literally trades them: `War_Pales_Won` and `War_WehlenJoined` in exchange for Zille.
- **Turn 9, Intermerkopum.** Smolak: "Just don't ask us to go sending our military into Pales."

**Can both happen?** Zille can be returned (T8/T9) while the Pales war continues. The Pales war outcome is independent unless the Smolak deal is taken. Nothing blocks the Zille *diplomatic* outcomes during the war. Only the military interference and the Derdian mercenaries are war-gated.

**Shared resources:**
- Army units are a single pool: `RiziaDLC.Army_Unit_*_Total` / `_Deployed` / `_Dead` / `_InTraining`, plus `Army_Unit_GrandTotal_*`. Examples: `Army_Unit_Ship_Total` drops by 2 for Bear Trap; `Army_Unit_GoldenGuard_Deployed` is used in Pales.
- `Resources_Military_Equipment` is spent on Su Omina (−250/−500) and on Smolak's arms deal (−1000).
- `Resources_Budget` and `Resources_Authority`.
- `Situation_Diplomacy_PowerProjection_*` is engine-derived and gates both the army interference and Smolak's bluff.
- `War_Control_Skipped` means the player skipped the war map (`Turn_6_SnO_WarEruptionBriefing [264]`).

---

## 7. Recommended insertion points for a mod "Zille War" branch

### 7.1 Where to hook

**A. Turn 8 `SnO_SecurityBriefAndZille`, at `[2122]` "Let's bomb the bastards" (best place to plan the war).**
- It is already gated on `Situation_Diplomacy_PowerProjection_Strong`/`_Overwhelming` (`[2116]`), and it currently dead-ends in a refusal (`[2127]`, then `[2135]`/`[2136]` and the Pales line `[2138]`).
- Mod change: let the player overrule the councillor, or add a sibling option at `[2115]` such as "Prepare a military contingency for Zille".
- The new option sets a mod flag (for example `ZilleWar.PlanApproved`). It costs Authority, and optionally `Resources_Military_Equipment`.
- If `Pales_War == true`, keep Titus's objection and add a "two-front" risk.
- Natural companions:
  - Lucita's T5 "overthrow" line (`[294]`) as foreshadowing.
  - The T2 feasibility exchange (`[787]`, a mainland land war only, no naval invasion of Wehlen).

**B. Turn 8 `FPnT_ZilleNegotiations`, the failure ultimatum at `[541]`/`[543]` (casus belli).**
- `[543]` "...Consider this your one and final warning" is already gated on exactly the assets a war needs: power projection, Derdian or BFF mercenaries, SO or ATA, `Diplomacy_Wehlen_FundBFF`.
- Mod change: set `ZilleWar.UltimatumIssued` here. The forced-failure path at `[787]` (points ≤ 2) also goes to `[1153]` (Disapproved), which is a good "Smolak refuses outright" trigger.

**C. Turn 9 `FPnT_ZilleReferendum` decision at `[198]` (the main battlefield entry).**
- Add a fourth option beside `[199]`/`[207]`/`[200]`: "Order a full military operation to liberate Zille." It sets the mod flag `ZilleWar.Started`.
- Then route to a new war conversation or war map instead of `ZilleReferendumInterference`.
- Reuse the vanilla success rule (`Interference [12]`/`[13]`/`[289]`) as the baseline for battle odds:
  - easy: no `Pales_War` and PowerProjection Strong or Overwhelming;
  - hard: during `Pales_War`, which needs `StrongArmedResistance` and `SuOminaActive`.
- Make Wehzek resistance stronger than vanilla's "fell back".

**D. Post-outcome war (Turn 9 end or Turn 10).** This catches the outcomes where no referendum choice is offered. Gate it on any of:
- `ZilleReturn_Success == false and ZilleReferendum_SmolakOffer_Accepted == false` (lost referendum, failed negotiation, Approved but lost), or
- `ZilleReferendum_Independence == true` (war against the new state; the T10 report already mentions border "clashes"), or
- `ZilleReferendum_SmolakOffer_Accepted == true` after the Pales treaty: the **betrayal war** that Smolak threatens in `PeaceTreaty [550]` and `CallWithSmolak [82]`.

A good host is a new Turn 10 conversation fired from Foreign Affairs. Add an option to the not-returned hub `[756]` next to `[741]` (Operation Sanctuary, "support the resistance of those who remained"), or a new SnO briefing.

**E. Turn 9 `FPnT_CallWithSmolak` reject path (`[87]`/`[204]`).** Optional escalation: Smolak warns or mobilises, and the mod sets a "Wehlen hostile" flag that raises war difficulty later.

### 7.2 Variables to check (war availability and strength)

**State:**
- `RiziaDLC.ZilleReturn_Success`, `RiziaDLC.ZilleReferendum_Approved` / `_Disapproved` / `_StoppedByAN` / `_NotStoppedByAN` / `_Won` / `_Lost` / `_Independence` (+ flavours), `RiziaDLC.ZilleReferendum_SmolakOffer_Accepted`, `RiziaDLC.War_WehlenJoined`.

**Two-front constraints:**
- `RiziaDLC.Pales_War`, `RiziaDLC.War_Pales_Won`, `RiziaDLC.War_Pales_Operation1_Won`, `RiziaDLC.War_Control_Skipped`, `RiziaDLC.Army_Unit_*_Total` / `_Deployed` / `_Dead`.

**Strength:**
- `RiziaDLC.Situation_Diplomacy_PowerProjection_Weak` / `_Average` / `_Strong` / `_Overwhelming`, `RiziaDLC.Resources_Military_Equipment`, `Resources_Budget`, `Resources_Authority`.

**Inside help:**
- `RiziaDLC.Zille_ViolentResistancePower`
- `RiziaDLC.Situation_Diplomacy_Zille_WeakArmedResistance` / `_GrowingArmedResistance` / `_StrongArmedResistance`
- `RiziaDLC.Situation_Diplomacy_Zille_SuOminaActive`, `RiziaDLC.Situation_Diplomacy_Zille_ATAActive`
- `RiziaDLC.Resources_Military_DerdianMercenaries_Zille`, `RiziaDLC.Resources_Military_RumburgianBFFMercenaries`, `RiziaDLC.Diplomacy_Wehlen_FundBFF`
- `RiziaDLC.MITZ_Demand_ZilleResistance` (Morellan arms)
- `RiziaDLC.Policy_RD_Order_BuildMaveNortusIntelHub_Completed`

**Popular support:**
- `RiziaDLC.Zille_ReferendumPower`, `RiziaDLC.Situation_Diplomacy_Zille_SmallRiziaMovement` / `_GrowingRiziaMovement` / `_MassiveRiziaMovement`.

**Diplomatic fallout:**
- `RiziaDLC.Diplomacy_Wehlen_AgreementRefused`, `RiziaDLC.Turn08_ZilleNegotiations_TreatyKept`
- `RiziaDLC.Zille_Publicized_LespianInvolvement` (casus belli and AN sympathy)
- `RiziaDLC.ZilleReferendum_StoppedByAN` (AN peacekeepers in Zille; attacking would hit the AN)
- `RiziaDLC.CountryRelation_Rumburg_*` (likely ally), `CountryRelation_Lespia_*`, `Situation_Diplomacy_GlobalImage_Bad`

### 7.3 Vanilla variables to set for consistent downstream content

**Victory (Zille liberated).** Reuse as much of the return pipeline as possible.
- **Required:**
  - `RiziaDLC.ZilleReturn_Success = true`. Turn 10/11, epilogues, Civil War ally, election text and the Reform branch all key off it.
  - `RiziaDLC.Situation_Diplomacy_AN_ThreatMitigationProtocol = false`, `RiziaDLC.Policy_Diplomacy_ZLLAActive = false`.
- **Ownership:** run the vanilla `Rizia_Turn_9_A_ZilleReturnedSpeech` (best: it sets everything below) or replicate its DOs `[213]`/`[214]` and `[235]`–`[255]`:
  - `RiziaDLC.ZilleOwnership_Brenas` or `RiziaDLC.ZilleOwnership_RiziaImperii`
  - `Authority_Modifier_TerritorialReclaimation` +1 or +2
  - codex `Locations_Regions_ZilleTerritory_Brenas` / `_RiziaImperii`, and `RiziaDLCSupport.Codex_Locations_Regions_ZilleTerritory = false`
  - `AddTokenStatusImmediately` for `Rizia_City_Zille` and `Rizia_City_Nanta`
  - the income-tax modifiers
  - journal and news flags
  - The speech's news picks `RiziaDLCSupport.News_Turn09_RH_ZilleReturnSpeech_RefVote` vs `_Negotiations` by `ZilleReferendum_Won` (`[156]`–`[158]`). Add a war variant or accept the "Negotiations" text.
- **Relations:** `RiziaDLC.CountryRelation_Wehlen_Hostile = true`, as vanilla interference does.
- **Flavour reuse:** consider `RiziaDLC.ZilleReferendum_Interfere_Army = true` + `RiziaDLC.ZilleReferendum_Interfere_Army_Success = true`. That gets the existing "Wehzek protesters", "accusations of interference" and "continued protests" texts in the T9 speech, the T10 report and the Sazon conversation. It also (correctly) blocks `Title_Peaceful`.
- `RiziaDLC.War_Happened = true` should already be true if the Pales war happened. Set it otherwise, since it blocks `Title_Peaceful`.
- **Map:** `RiziaDLC.MapLayer_World_Zille_Rizia` (and probably the `MapLayer_Zille_*` layers). These are not touched by any dialogue; find the engine consumer.
- **Do not set** `RiziaDLC.War_WehlenJoined` or `War_Pales_Won` (they mean Wehlen is a Pales co-belligerent).
- If the war follows the Smolak deal, you may also need to undo the Turn 10 transfer effects (`DisableToken("Events_Zille")`, `Authority_Modifier_TerritorialIntegrityLost`).

**Defeat.**
- Keep `ZilleReturn_Success = false` and set `CountryRelation_Wehlen_Hostile = true`.
- Optionally apply `Authority_Modifier_TerritorialIntegrityLost -1` (the vanilla "lost Zille" modifier from `PeaceTreaty [552]`) and `Situation_Diplomacy_AN_ThreatMitigationProtocol = false`.
- Setting `RiziaDLC.ZilleReferendum_Lost = true` makes the T10 report, Election_Issues, Rusty's tobacco line and `Title_Frailer` treat it as "lost". Otherwise these default to the generic "unchanged under Wehlen's control" text.
- Leave `War_WehlenJoined = false`, or the report says "permanent transfer".

**Independence-style outcome.** If the war shatters Wehzek control but the resistance takes over, set `RiziaDLC.ZilleReferendum_Independence` and one flavour flag, plus the `RiziaDLCText.IndependentZille` string. Add `EnableToken("Rizia_Country_Zille")` and `UpdateCountryRelationship("Rizia_Country_Zille", ...)` (see `Referendum [317]`–`[320]`).

### 7.4 Still missing (extract next)

1. **The turn and conversation scheduling** (which conversation fires in which turn under which conditions). This is needed to place the new war conversation and to know whether `ZilleReferendum` is skipped after the Smolak deal.
2. **The engine rules** that turn `Zille_ReferendumPower` / `Zille_ViolentResistancePower` into the `Situation_Diplomacy_Zille_*` tiers, and that compute `Situation_Diplomacy_PowerProjection_*`.
3. **The decision-card assets:** `Decision_Turn03_*`, `Decision_Turn04_*`, `Decision_Turn05_ZilleResistanceFunding_*`, `Decision_Turn07_ZilleResistanceFundingTwo_*`, the `Journal_Floating_Decision_ZilleResistanceFunding_*` floating decision, `Panel_WehlenNegotiationPromises` and the MITZ demand panel.
4. **The Pales war-map system:** the `War_Objective_*`, `War_AttackPlan_*` and `Army_Unit_*` handling. A Zille war map (see `docs/zille-geography.md`) would need to reuse or clone it.
5. **The consumers of the `MapLayer_*` variables** and the `Events_Zille` / `Rizia_Country_Zille` / `Rizia_City_Zille` / `Rizia_City_Nanta` tokens.
6. **Conversation c390** (the second "BFF Convo (Friendly)"), lost to a filename collision in the export.
