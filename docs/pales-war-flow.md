# Vanilla Pales war: story and variable layer

Source: the extracted Rizia dialogue database (one file per conversation, plus `_variables.tsv`).
Entry IDs are written `[n]` and refer to the conversation named in the same section. Variable
names are copied from `_variables.tsv`. Unless another prefix is shown, the namespace is
`RiziaDLC.`, so `Pales_War` means `RiziaDLC.Pales_War`.

The dialogue database does not hold each conversation's trigger condition (every file starts
`[0] -> 1` with an empty `desc:`). Where this document says when a scene plays, that is inferred
from what the scene reads and from who sets those variables.

**The most important structural fact:** the battles themselves are not in the dialogue layer.
Dialogue sets up the war, and `EnableToken("Rizia_Event_InvasionOfPales")` (WarEruptionBriefing
[486]) hands control to the war-map mechanic. That mechanic (game code, not dialogue) writes the
results that later scenes read. Section 3 lists which variables are written by dialogue and which
only by the engine.

---

## 1. Narrative flow: how war becomes possible, how it starts, and the alternatives

### 1.1 Timeline

| Turn | Conversation | War-relevant role |
|---|---|---|
| 1 | `Rizia/Turn 1/SnO_SecurityMilitaryMeeting` | Sets `Panel_WarProduction = true` ([425], [539], [542]); unlocks factory decrees ([423]: `Enable_Policy_RD_Order_TankFactory`, `_SupportVehicleFactory`, `_MilitaryEquipmentFactory`, `_IncreaseProvincialLevies`, `_RemoveProvincialLevies`, `_LowerConscriptionAge`, `_IncreaseConscriptionDuration`). Branch focus is discussed ([429]–[441] read `Focus_MilitaryBranch_Airforce/Army/Navy`); the matching special division (paratrooper, mountaineer or marine) is announced at [452]–[454]. |
| 2 | decision panel | `Decision_Turn02_ArmySpecialForces_Marine/_Paratrooper/_Mountaineer/_None`. `FPnT_DiplomacyBriefing` changes `War_Pales_PublicWarSupport` by -1. |
| 4 | `A_CouncilTalkAboutPales` | Drillship crisis. The optional blockade sets `Diplomacy_Pales_Military_BlockDrillship = true` ([379]/[378], +1 Authority). [411] enables `Enable_Policy_RD_Order_ExpandFortAlesNavalBase`, `_ExpandTeradorAirbase` and `_PalesAdminDistrict_StrengthenedBorderFort`. [413] runs `EnableToken("Rizia_Event_Aureus")`. |
| 4 | `FPnT_PalesDukeVisit` | The Aureus outcome: Buy (`Diplomacy_Pales_GasField_Buy`), Cede (`Diplomacy_Pales_GasField_Cede`) or Arbitration (`Situation_Diplomacy_Pales_ANDORArbitration`). |
| 6 | `A_CouncilMeeting` | AN arbitration verdict, early-invasion option, and the incident selector. |
| 6 | `SnO_Incident` | Naval or border incident; the player can choose "Prepare for war". |
| 6 | `SnO_PalesPostIncidentMeeting`, `A_BorderInvestigationResults`, `SnO_PalesDealSigning`, `P_LucitaAzaroUpdate`, `FPnT_DukeWarPhoneCall` | Peace deal or war (`Pales_War = true`). |
| 6 | `A_PalaceBalconySpeech`, `A_ReligionBriefing`, `P_Cemetary` | War speech and justification flavour (`Turn06_WarSpeech_*`, `Turn06_WarSpeech_Impact`, `Turn06_Briefing_Religion_WarJustifed*`). |
| 6 | `SnO_WarEruptionBriefing` | Strategy, deployment, the delegate option, then `EnableToken("Rizia_Event_InvasionOfPales")`. |
| 6–7 | war map | **Operation 1 "Golden Shield"**, resolved by the engine. No Turn 7 conversation reads its results. |
| 7 | `Pales War Update And Tax` | House of Delegates debate and the war profits tax. |
| 7 | `FPnT_RumburgMeeting`, `Fighting_Assistance_Friendly/_Unfriendly` (Derdia) | Foreign military aid (section 5.3). |
| 7–9 | decision panel | War events `Decision_Turn07..09_War_Both_*` / `_War_Skipped_*`. |
| 8 | `SnO_War_Pales_PostOperation1` → `Operation 1 Results` → `Operation 2 Next Steps` → back to PostOperation1 [174] | Operation 1 debrief and the Operation 2 briefing. |
| 8 | `FPnT_AlvarezVisit`, `FPnT_ANSession` → `Sanctions` | Lespian support and sanctions; the AN sanctions vote. |
| 8–9 | war map | **Operation 2**: the peninsula and the capital. Turn 9 scenes already read the Remaz/Solles captures. |
| 9 | `FPnT_GRACERumburgMeeting`, `FPnT_CallWithSmolak`, `FPnT_MITZ_BoardMeeting` | Rumburg ceasefire offer; Wehlen's "Zille for Pales" offer; Lespian support. |
| 10 | `Reform_ConstitutionSigning`, decision panel | `War_ParliamentProposesPeace`; `Decision_Turn10_Ceasefire_Accepted`. |
| 10 | `War_Pales_VictoryParade` / `War_Pales_Loss` → `War_Pales_PeaceTreaty` | Outcome scenes. |
| 10–11 | `Pales_Reform_Branch`, `Turn_11_Ending_Speech`, `Continued_Reign`, `Epilogue` | Aftermath lines. |

### 1.2 Turn 4: the Aureus crisis (sets up the casus belli)

- `A_CouncilTalkAboutPales` sets no deal. Its only lasting war-relevant output is the blockade
  (`Diplomacy_Pales_Military_BlockDrillship`). [361]/[362] read
  `Resources_Military_NavalBombardmentCapability > 0`, but only to choose a line.
- `FPnT_PalesDukeVisit` has three outcomes:
  - **Buy** ([463]): sets `Diplomacy_Pales_GasField_Buy`, `Policy_Diplomacy_TradeDeal_Pales` and
    `Policy_Economy_PalesPipeline_Project`. Only possible without a blockade. The price depends on
    power projection: Weak needs Budget ≥ 10, Average ≥ 8, Strong or Overwhelming ≥ 6.
  - **Cede** ([484], [601], [866]): sets `Diplomacy_Pales_GasField_Cede`. On the blockade path,
    [866] also does `War_Pales_PublicWarSupport + 1`.
  - **Arbitration** ([488], [620], [886]): sets `Situation_Diplomacy_Pales_ANDORArbitration`.
    The blockade can be kept, or sent later ([474]/[619] → `BlockDrillship = true`).

### 1.3 Turn 6 `A_CouncilMeeting`: verdict, early invasion and the incident selector

- **AN verdict:** points are summed into `Arbitration_TotalSupport` ([5]–[44], [803]–[811]).
  A blockade costs one point ([807]). At ≥ 5, [28] sets
  `Diplomacy_Pales_DiplomaticArbitration_WonHalf`.
- **Navy options:**
  - Redeploy after the verdict: sets `Diplomacy_Pales_Military_BlockDrillship_DeployedAfterAN`
    and `BlockDrillship`, +2 Authority.
  - Cancel a cession and blockade instead ([586]): sets `Diplomacy_Pales_GasField_CedeCancelled`
    and `BlockDrillship`.
- **"Declare war if they fire" ([595]):** sets `Intention_Pales_DeclareWar`. It always comes with
  an active blockade.
- **Early invasion ([539]/[541] → [758]):** sets `Diplomacy_Pales_PrepareForWar = true`
  (description: "Early Invasion Option"). This path skips the incident selector.
- **Incident selector ([303] → [591]):**

  | Condition (checked in order) | Result |
  |---|---|
  | `Diplomacy_Pales_PrepareForWar` | no incident |
  | `Diplomacy_Pales_Military_BlockDrillship` | [590] `War_Incident_Naval = true` |
  | `Diplomacy_Pales_GasField_Buy and not Diplomacy_Pales_GasField_BuyCancelled` | [594] `War_Incident_Border = true` |
  | otherwise | no incident, no war |

  "Otherwise" covers a peaceful cession, arbitration without a blockade, and a cancelled buy.

### 1.4 Turn 6 `SnO_Incident`

- **Naval** (`War_Incident_Naval`): a Palesian ship rams the flagship RRN Valero.
  - Choices: [358] `Incident_Naval_NoRescue`, [355] `Incident_Naval_FocusAidOnValero`,
    [356] `Incident_Naval_RescueBoth` ("ships escape / valero damaged"),
    [357] `Incident_Naval_FreeShips`.
  - Or [349]/[353] "Prepare for war".
- **Border** (`War_Incident_Border`): six soldiers are killed and Navar Castle is occupied. The
  dialogue hints this is a false flag against the Buy deal.
  - Choices: [188]/[189] `Incident_Border_KillSoldiers`, [178]/[182] `Incident_Border_NoAction`.
  - Or [183]/[187] "Prepare for war".
- **"Prepare for war"** leads to [413]: `Diplomacy_Pales_DeclareWar = true` (description:
  "Second invasion option"). If `Diplomacy_Pales_Vina_LeadsDeal` is set, [444] sets
  `Vina_Hostage = true`.

### 1.5 Where `Pales_War = true` is actually set

The "declare war" choices carry **no IF conditions**. There is no army-strength, budget,
authority, alliance or relations gate anywhere on the decision to go to war.
`Situation_Diplomacy_PowerProjection_*` only changes threat lines. Budget gates only the Buy deal
and reparations, and authority is only a reward for blockades.

| Conversation | Entry | Gate | What it sets |
|---|---|---|---|
| `P_LucitaAzaroUpdate` | [6] | `Diplomacy_Pales_PrepareForWar or Diplomacy_Pales_DeclareWar` | `Pales_War = true; Situation_Military_PalesianWarDeployment = true; Diplomacy_Pales_NegotiatingPower = -100` |
| `SnO_PalesPostIncidentMeeting` | [580] | player picks war: [545], [576], [604] (naval); [644], [666], [671] (border) | `Pales_War`, `Situation_Military_PalesianWarDeployment`, `Diplomacy_Pales_NegotiatingPower = -100`, `News_Turn06_*_PostIncidentTalkBreakdown`; [700] `Vina_Hostage = true` if Vina led the deal |
| `A_BorderInvestigationResults` | [164] | the player refuses to hand over Lucita and declares war ([142]/[143]) | `Pales_War = true; Pales_PostIncident_Peace_Deal = false; Diplomacy_Pales_NegotiatingPower = -100; Policy_Diplomacy_TradeDeal_Pales = false` |
| `FPnT_DukeWarPhoneCall` | [90] | always at the end of the call ("And just like that, the Second Pales War had begun.", [78]) | `Pales_War = true; Diplomacy_Pales_NegotiatingPower = -100; Policy_Diplomacy_TradeDeal_Pales = false` |

In `FPnT_DukeWarPhoneCall`, [11]/[14] read the incident flags and [51] reads
`Situation_Diplomacy_PowerProjection_Strong/Overwhelming`; both only choose lines. The player can
announce the invasion ([37]/[62]) or demand surrender: [59] fealty, [60] the Aureus field, or [61]
all territory except the city of Pales.

### 1.6 Alternatives to war

- **Peaceful Aureus outcomes with no incident.** A cession, arbitration without a blockade, or a
  cancelled buy leads to `SnO_PalesDealSigning` for the original deal.
- **Post-incident deal** (`SnO_PalesPostIncidentMeeting` [581]): sets
  `Pales_PostIncident_Peace_Deal = true`. The terms are one of `Pales_PostIncident_Deal_Cede`,
  `Pales_PostIncident_Deal_25` or `Pales_PostIncident_Deal_50`. If no `Deal_*` flag is set, the
  original deal stands.
  - Border path: releasing the soldiers sets `PostIncident_Border_ReleasedSoldiers` ([334]). A
    joint investigation sets `PostIncident_Border_JointInvestigation` ([738]) and leads to
    `A_BorderInvestigationResults`; there, arresting Lucita keeps the deal ([204]).
  - Data quirks: `Deal_50` is never read by DealSigning, and the "[-2 Budget]" option at
    [527]/[528] deducts nothing.
- **Peace without a deal** ([579]): sets `Pales_PostIncident_Peace_NoDeal = true` and runs
  `DisableToken("Rizia_Event_Aureus")` ([819]).
- **`SnO_PalesDealSigning`** signs the deal. It sets extraction and trade flags
  (`Situation_Economy_AureusField_High/Medium/LowExtraction`, `Energy_Modifier_AureusGasField`
  8/4/2, `Policy_Economy_AureusField_Extractions`, `Policy_Diplomacy_TradeDeal_Pales`) and the
  pipeline flags. Where Rizia gets nothing, [360] disables the Aureus token. There is no explicit
  war lock: `Pales_War` simply stays false.
- **Turn 7 `FPnT_DukeVisit`** (the peace branch) offers:
  - GRACE: [407] `Diplomacy_Pales_GRACEApplication`.
  - Military alliance: [400]/[537] `Policy_Diplomacy_Pales_MilitaryAlliance` and
    `CountryRelation_Pales_Allied`. This needs `Situation_Diplomacy_PowerProjection_Overwhelming`,
    or Strong plus `CountryRelation_Pales_Friendly`; otherwise Axel says "the state of your armed
    forces leaves something to be desired".
  - An Intermerkopum partnership.
  - Marriage talks, which lead to `Pales_Rizia_Unification` (set later, in Turn 10
    `AxelVina_Unity` [761]).
- **During the war:** the Rumburg ceasefire offer (Turn 9 `War_RumburgCeasefireRequested`), a
  parliamentary peace (`War_ParliamentProposesPeace`), suing for peace after a loss, and Wehlen
  joining in exchange for Zille (section 4).

---

## 2. Structure of the war in the story layer

### 2.1 War Eruption Briefing (Turn 6, `SnO_WarEruptionBriefing`)

This is the setup pass. Before the player sees anything, the hidden logic computes how strong
Pales will be.

- **AN support for Pales, `War_Pales_ANSupport`** (initial 0, "Determines AN's support of
  Pales"):

  | Situation | Change |
  |---|---|
  | early invasion | +5 ([26]) |
  | naval war, cession cancelled | +3 ([27]) |
  | naval war, deployed after the AN verdict | +3 ([28]) |
  | naval war, deployed before the verdict | +2 ([29]) |
  | Valero-first or no-rescue naval choice | +1 ([83]) |
  | other naval rescue choices | -1 ([84]) |
  | joint investigation implicated Lucita but she was kept | +2 ([25]) |
  | border soldiers killed | +2 ([35]) |
  | `War_Media_Geopolitico_Support` | -1 ([36], [476]) |

- **Lespian support, `War_Pales_LespianSupport`** ("0: none 1: default 2: direct"):
  - 2 if `CountryRelation_Lespia_Hostile or MITZ_ScrewedOverLespia` ([185]).
  - 1 if Lespia is neither Friendly nor Allied ([181]).
  - Otherwise it stays 0.
- **Pales strength, `War_Pales_Strength`** (initial 1, "Determines Pales War Strength"):
  - AN support below 3 adds nothing, below 5 adds +1 ([55]), 5 or more adds +2 ([56]).
  - Lespian support 1 adds +3 ([47]); 2 adds +5 ([46]).
- **Forts:**
  - `Policy_RD_Order_PalesAdminDistrict_StrengthenedBorderForts_Completed` →
    `RiziaDLCSetup.War_TileOverride_Rizia_StrongerBorderForts` ([78]).
  - Pales gets level-1 forts if Rizia was friendly in the prologue with no blockade or fort
    project ([68]), if Vina led the deal ([71]), or on an early invasion when the prologue was not
    hostile ([74]). Otherwise it gets level-2 forts:
    `RiziaDLCSetup.War_TileOverride_Pales_StrongerBorderForts` ([79]).
- **Flagship:** if `Incident_Naval_RescueBoth`, [91] sets `Army_Unit_Flagship_Total = 0` and
  [502] sets `Situation_Military_LostPrideOfNavy`.
- **Pales naval deployment for Operation 1:** a Valero-first or no-rescue choice gives
  `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_Naval_Default` ([95]); otherwise
  `..._Naval_Strong` ([96]).
- **News:** [496] `News_Turn06_*_PalesWarStart`; [495] `News_Turn06_RH_VinaHeldInPales` if
  `Vina_Hostage`; [497] `Reports_Turn06_Terador_AirbaseStormed` if
  `Policy_RD_Order_TeradorAirforceBaseExpansion_Completed`; [538] sets
  `RiziaDLCText.News_RD_IncreasedLevies_PalesWar`; **[498] `War_Happened = true`**.

**Status briefing (all reads, no gates):**

| Unit | Thresholds | Entries |
|---|---|---|
| `Army_Unit_Infantry_Total` | ≥10 / ≥8 / >5 / else | [373]–[385] |
| `Army_Unit_Support_Total` | ≥7 / ≥5 / >3 / else | [387]–[398] |
| `Army_Unit_Tank_Total` ≥ 6, then `Army_Unit_Support_Total` ≥ 4 / > 2 | — | [402]–[413] (the code checks Support where Tank is expected) |
| `Focus_MilitaryBranch_Airforce` | air force line | [417] |
| `Army_Unit_Marine_Total` ≥ 1 | marine line | [421] |
| `Army_Unit_Flagship_Total == 1` | flagship line | [425] |
| `Focus_MilitaryBranch_Navy` | navy line | [432] |
| `Army_Unit_Ship_Total` | ≥5 / ≥3 / else | [435]–[442] |
| `Army_Unit_Submarine_Total` | ≥4 | [444] |

House levies: [559] gates the House-units hub on
`Enable_Policy_RD_Order_RemoveProvincialLevies == false`; [568] reads
`Enable_Policy_RD_Order_IncreaseProvincialLevies` ("House army divisions doubled").

**Objectives as briefed:**
- Operation 1 takes **Ziqes and Arces** and traps Palesian forces at the **Solles** chokepoint
  ([262], [276]–[283]). Arces is "the most important goal".
- Secondary: Navar, which has the air-defence belt near Fort Ales ([279], [280], [455]).

**Player orders:**

| Order | Entry | Sets |
|---|---|---|
| Feint north | [267] → [209] | `War_AttackPlan_FeignNorth` ("Feigned attack in north") |
| Feint south | [268] → [210] | `War_AttackPlan_FeignSouth` |
| Direct assault | [266] | nothing |
| Navy on standby | [300] / [371] | nothing |
| Blockade Gulf of Ales | [301] → [206] | `War_AttackPlan_BlockadeGulfofAles` |
| Blockade Gulf of Pales | [302] → [207] | `War_AttackPlan_BlockadePales` |
| Blockade Gulf of Remaz | [303] → [208] | `War_AttackPlan_BlockadeRemaz` |

**Who commands:**
- [270]/[272] "I'll plan it personally" → [501] `Situation_Military_Monarch_OverseesWar`. The
  player plays the war map.
- [273] "grant full authority to the General Staff" → [264]
  `War_Control_Skipped = true` ("Player opts out of war gameplay") and
  `Situation_Military_HighCommand_OverseesWar = true`.
  - [499] saves every `Army_Unit_<X>_Total` into `War_Control_Skipped_<X>`.
  - [500] replaces the army with a placeholder: Infantry/Tank/Support = 2, everything else 0.

**Hidden resolution after the orders** ([508] → [336] → [329] → [311] → [355] → [486]):

- **Pales deployment:**
  - `FeignNorth` → `RiziaDLCSetup.War_Operation1_Deployment_Pales_Northern` ([346]).
  - `FeignSouth` → `..._Southern` ([354]).
  - Neither → `..._Balanced` ([347]).
- **Pales strength override:** skipped on an early invasion ([335]). Otherwise:
  - `War_Pales_Strength <= 3`: no override.
  - `<= 6`: `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_Default` ([348]).
  - Higher: `..._Pales_Strong` ([349]).
- **Blockade resolution** (`Army_Unit_Ship_Total + Army_Unit_Submarine_Total`):

  | Blockade | Needs | Success | Failure |
  |---|---|---|---|
  | Pales | ≥ 6 | `War_AttackPlan_BlockadePales_Success`, lose 2 ships ([359], [362]) | lose 3 ships ([363]) |
  | Gulf of Ales | ≥ 5 | `War_AttackPlan_BlockadeGulfofAles_Success`, lose 1 ship, Pales naval override switched to `..._AlesBlockaded` ([360], [506]/[507]) | lose 2 ([365]) |
  | Remaz | ≥ 4 | `War_AttackPlan_BlockadeRemaz_Success`, no loss ([361]) | lose 1 ([366]) |

  Losses are applied as `Army_Unit_Ship_Total -= n` and `Army_Unit_Ship_Dead += n`.
- **Always:** [355] sets `RiziaDLCSetup.War_TileOverride_Pales_ExtraAirDefence` and
  `..._ExtraNavalDefence`.
- **[486] `EnableToken("Rizia_Event_InvasionOfPales")`**, then [470] `End()`.

### 2.2 Between operations (Turn 7)

**`Pales War Update And Tax`** (House of Delegates, Manus Sazon or Daria de Rava):
- Incident flags choose "justified" or "suspicious" lines ([7], [49]).
- The tax debate reads `Resources_Budget > 3` / `> 4`.
- **War profits tax:**
  - [153]/[156] set `Enable_Policy_RD_Economy_WarProfitsTax = true` (a decree for later).
  - [154]/[157] run `EnactDecree("Economy_Taxes_WarProfitsTax")`. These are gated by
    `Resources_Authority >= 3` ([161], [168]).
  - The [135] menu says "[-3 Authority]", but the DO code at [154] adds +3 and [157] changes
    nothing. That is a data quirk.
  - The decree uses `Policy_RD_Economy_WarProfitsTax` and `Budget_Modifier_WarProfitsTax` (2).
- [2] continues to `c386:1`, the Zille Treaty Discussion.

**Foreign aid:** section 5.3.

**War events** are decision-panel items, not dialogue. "Both" means they appear in both control
modes; "Skipped" means only when `War_Control_Skipped`.
- Turn 6: `Decision_Turn06_War_Both_AlertDrills_*`, `_UAP_*`, `Decision_Turn06_WarEnlistments_*`,
  `Decision_Turn06_SabotagePales_Air/_Naval`.
- Turn 7: `Decision_Turn07_War_Both_EconomicTurmoil_*`, `_BlockedTradeShips_*`,
  `_PalesPropaganda_*`, `_WehlenDrugs_*`, `_RefugeeStory_*`, `_IndependentJournalism_*`,
  `_Lespia_SpyBalloon_*`.
- Turn 8: `Decision_Turn08_War_Both_LaborShortage_*`, `_MothersProtest_*`,
  `_NavarCastleAttack_*`, `_PalesResistance_*`, `_RemembranceCeremony_*`, `_JetFuel_*`,
  `_Pipeline_Sabotage_*`, `Decision_Turn08_War_Skipped_ExperimentalWeapons1_*`.
- Turn 9: `Decision_Turn09_War_Both_InternalDissent_*`, `_PalesInfrastructureCollapse_*`,
  `Decision_Turn09_War_Skipped_ExperimentalWeapons2_*`.
- Turn 10: `Decision_Turn10_War_Skipped_VinaRescue_DirectAssault/_CovertOperation`.

### 2.3 Operation 1 "Golden Shield": debrief (Turn 8)

The chain is `SnO_War_Pales_PostOperation1` → `c411` `Operation 1 Results` → `c412`
`Operation 2 Next Steps` → `c410:174` (back in PostOperation1) → `End()`.

**`SnO_War_Pales_PostOperation1`:**
- [193]: if `War_Control_Skipped`, run the auto-resolve at [219]; otherwise go straight to
  [137]. The auto-resolve is a dialogue-only operation model and the best template for a mod; see
  the table below.
- Setting: [7] `War_Operation1Briefing_Frontline` (set by the engine or panel) puts the scene at
  a forward operating base: FOB Bull Charge if Operation 1 was won, FOB Iron Gate if it was lost.
  Otherwise the scene is the War Room ([311]).
- Morale speeches add `Resources_Military_Manpower` +250 or +100 ([60], [61], [124], [125]).
- **Operation 2 deployment (always runs, [137]):**
  - `War_Pales_LespianSupport >= 1` → `RiziaDLCSetup.War_Operation2_DeploymentOverride_Pales_Strong`;
    otherwise `..._Pales_Default`.
  - Then the matching naval override,
    `RiziaDLCSetup.War_Operation2_DeploymentOverride_Pales_Naval_{Default|Strong}_{AlesBlockaded|PalesBlockaded|RemazBlockaded|NoBlockade}`,
    is chosen from the `War_AttackPlan_Blockade*_Success` flags ([142]–[167]).
- Lucita's private talk afterwards sets `Turn08_PalesWarPostOp_DismissedAzaro` or
  `Turn08_PalesWarPostOp_DisgracedSorry`.

**Auto-resolve when `War_Control_Skipped` ([219]–[217]).** `War_Pales_Operation1_Won` is still
read here, not set, so the engine decides it in this mode too. Dialogue then fills in losses and
captures:

| `War_Pales_Operation1_Won` | Power projection | Losses ([215]–[217], [210]) | `War_Operation1_TotalEnemy_Dead` | Captured (Terador / FortAles / Ziqes / Arces) |
|---|---|---|---|---|
| true | `Situation_Diplomacy_PowerProjection_Overwhelming` | each unit total minus 1/5 | 30 | T / T / T / T ([205]) |
| true | otherwise | minus 1/4 | 20 | T / T / F / T ([204]) |
| false | `..._Average` | minus 1/3 | 10 | T / F / T / F ([203]) |
| false | otherwise | minus 1/2 | 5 | F / F / F / F ([202]) |

Losses are computed from the saved `War_Control_Skipped_<X>`. Afterwards [211]/[213] save the
new totals and [212]/[214] reset the placeholder army to 1/1/1/0…

**`Operation 1 Results`** (Taddeus, Carlos and Lucita; all reads except where noted):
- [5]: `War_Pales_Operation1_Won` → "We are winning" or "not going well". Journal:
  `RiziaDLCSupport.Journal_Turn08_PostOperation1_OperationWon/_OperationLost`.
- Numbers: `Army_Unit_GrandTotal_Total` ≥20 / ≥15 / ≥10; `War_Operation1_TotalPlayer_Dead`
  ≥20 / ≥15 / ≥10; `War_Operation1_TotalEnemy_Dead` ≥20 / ≥15 / ≥10.
- Objectives: `War_Objective_Captured_Arces`, `War_Objective_Captured_Ziqes`. Losses at home:
  `War_Objective_Captured_Terador` and `War_Objective_Captured_FortAles` (both start **True**,
  meaning still Rizian; false means lost).
- Resistance: `War_Pales_Resistance` ≥2 / ≥1 (initial 1, "Determines if Militia spawn event gets
  triggered").
- Public opinion: `War_Pales_PublicWarSupport > 0`. Its description says "Palesian internal
  support", but this scene uses it for Rizian opinion.
- House units: `Army_Unit_Sazon_Dead`, `Army_Unit_Toras_Dead`, `Army_Unit_Azaro_Dead` (0 / 1 /
  more).
- Supplies: `Resources_Military_Manpower < 1000` and `Resources_Military_Equipment < 1000` lead
  to "we need more".
- On a win, the player seals the results. On a loss, Taddeus kneels:
  - [234] slap or rip his medals → `Taddeus_Disgraced = true` and
    `RiziaDLCIsolated.Taddeus_MedalsRipped`.
  - Otherwise forgiveness.

**`Operation 2 Next Steps`:**
- If Operation 1 was won: "encircle the peninsula", pushing past Arces toward **Mandana**
  ([6], [34]–[37]).
- If it was lost: "back where we started" ([22]). Choices:
  - [40] attack anyway, with [53]/[59] → [61] `War_Operation2Loss_Charge = true`.
  - [41] hold the line.
- Intel: Palesian strength from `RiziaDLCSetup.War_Operation2_DeploymentOverride_Pales_Strong`.
  Their navy's position from the `..._Naval_*_Blockaded` flags, visible only with
  `Situation_Order_ForeignIntelligence_Effective` (Adequate gives a vague line).
- Lespia: `War_Pales_LespianSupport >= 2` → [248] `Resources_Military_EnemyAirStrikeCount = 5`
  (initial 1), meaning Lespian bombers.
- If `Zille_Discovered_LespianInvolvement and not Zille_Publicized_LespianInvolvement`, Lucita
  points to leverage over Lespia ([262], [267]).
- **Operation 2 objective:** the peninsula and the capital, **Pales** ([177], [212]–[214]).
  Options discussed: a naval invasion past their fleet, or air strikes at the corridor entrance.

### 2.4 Operation 2 (Turn 8–10)

Operation 2 is run by the engine. In dialogue its state shows up as:
- `War_Objective_Captured_Remaz`, `War_Objective_Captured_Solles`, `War_Objective_Captured_Pales`
  (Turn 9 `CallWithSmolak` [399] already reads Remaz/Solles).
- `War_Operation2_TotalPlayer_Dead`, `War_Operation2_TotalEnemy_Dead`.
- Final flags: `War_Pales_Won` and `War_Pales_Loss`.
- Engine side effects: `War_PalesOfficial_Escaped` ("Result of decision choice 2 in Remaz capture
  WarEvent"), `Vina_Rescue_Success`, `Vina_CorrectIntel` / `Vina_WrongIntel`,
  `RescueSpy_*`, `War_Used_ExperimentalWeapons` and `War_Used_ExperimentalWeapons_<City>`,
  `War_Airfields_Captured`, `Country_WarRefugees`,
  `Situation_Military_War_Pales_Destruction_Light/_Moderate/_Heavy`,
  `Situation_Military_Strong/Growing/WeakPalesResistance`.

### 2.5 International reaction (Turn 8–9)

**`FPnT_AlvarezVisit` (Lespia):**
- Sets `Situation_Diplomacy_Lespia_SanctionsRizia = true` ([781], [2078], [2115]), or clears it
  through deals.
- Changes `War_Pales_LespianSupport` with:
  - Captures: Alvarez is impressed if Arces or Ziqes is taken.
  - Casualties: `War_Operation1_TotalPlayer_Dead + War_Operation2_TotalPlayer_Dead >= 10`.
  - The deals: destroy the Zille-bombing evidence (`Zille_Destroyed_LespianEvidence`, support -1);
    pay -2 Budget; lease Caleqabiz (`Policy_Diplomacy_Caleqabiz_LespiaATO`, support 0, sanctions
    off).
- Can also deliver aircraft to Rizia ([1072]): `Resources_Military_BomberPlanes` +100,
  `_FighterPlanes` +200, `_Tanks` +150.

**`FPnT_ANSession` → `Sanctions` (c402):**
- [21]: `Situation_Diplomacy_Lespia_SanctionsRizia and not Zille_Publicized_LespianInvolvement and Pales_War`
  → Lespia brings the accusation (`RiziaDLCIsolated.ANSanctionAccusation_Lespia_GlobalImage`).
  Alternatively Valgsland, if `GlobalImage_Bad` and Valgsland is Unfriendly.
- Each speaker adds ±1 to `RiziaDLCIsolated.ANSanctionAccusation_Vote`, based on
  `CountryRelation_<X>_*` and `Situation_Diplomacy_GlobalImage_Bad`. For Pales the condition
  includes `CountryRelation_Pales_AtWar` ([185]).
- The charges read: `War_Used_ExperimentalWeapons` (>0, >1), `Country_WarRefugees > 2`,
  `Decision_Turn07_War_Both_RefugeeStory_TransportRefugeesToMorella`,
  `Decision_Turn08_War_Both_PalesResistance_ScorchedEarthPolicy`.
- **Verdict:**

  | Vote | Result | Effects |
  |---|---|---|
  | > 2 | [240] heavy | `Situation_Diplomacy_ANSanctions_Heavy = true; Budget_Modifier_ANSanctions = -6; Energy_Modifier_ANSanctions = -5`; achievement `UnlockAchievement("Rizia_ANSanctions")` ([323]) |
  | > 0 | [241] weak | `Situation_Diplomacy_ANSanctions_Weak = true; Budget_Modifier_ANSanctions = -3; Energy_Modifier_ANSanctions = -2` |
  | otherwise | [242] none | `RiziaDLCSupport.Journal_Turn08_ANSession_NoSanctions` |

  `Sordland_RiziaSanction_Aye` is set at [298].
- Turn 9 `FPnT_MITZ_BoardMeeting` can set `War_Pales_LespianSupport = 2` and
  `Budget_Modifier_LespiaSanctions - 1` (initial -2).

**Casualties and war tax:** casualties come from `War_OperationN_TotalPlayer_Dead` and
`Army_Unit_*_Dead`. The war profits tax is described in 2.2.

---

## 3. Variables

**Legend:** (D) written by dialogue. (E) never written by any dialogue, so it is set by the
war-map engine, a decision panel or code. Values are the initial values from `_variables.tsv`;
descriptions are quoted where one exists.

### 3.1 War state and casus belli

| Variable | Init | Notes |
|---|---|---|
| `Pales_War` | False | (D) The war switch. Reset to false by PeaceTreaty [460]; set back to true on "keep fighting" ([251]). |
| `War_Happened` | False | (D) WarEruptionBriefing [498]. Read by the epilogue and the Turn 10 scenes. |
| `War_Incident_Naval` | False | (D) "Naval Incident Happens". |
| `War_Incident_Border` | False | (D) "Border Incident Happens". |
| `Intention_Pales_DeclareWar` | False | (D) Turn 6 council [595]. |
| `Diplomacy_Pales_PrepareForWar` | False | (D) "Early Invasion Option". |
| `Diplomacy_Pales_DeclareWar` | False | (D) "Second invasion option". |
| `CountryRelation_Pales_AtWar` | False | (E) Read by Sanctions [185]. |
| `CountryRelation_Wehlen_AtWar` | False | (E) **Exists but is read and written nowhere in dialogue.** A free hook for the mod. |
| `Situation_Military_PalesianWarDeployment` | False | (D) |
| `Diplomacy_Pales_NegotiatingPower` | -3 | (D) Set to -100 on war. |
| `Situation_Military_Monarch_OverseesWar`, `Situation_Military_HighCommand_OverseesWar` | False | (D) Control mode. |
| `War_Control_Skipped` | False | (D) "Player opts out of war gameplay". |
| `War_Control_Skipped_<Unit>` | 0 | (D) Army snapshot for each unit type, used by the auto-resolve. |
| `War_Tutorial_OptIn` | False | (E) |
| `RiziaDLCSetup.IsAnyOperationInProgress` | False | (E) |

### 3.2 Strength modifiers and setup overrides

| Variable | Init | Notes |
|---|---|---|
| `War_Pales_Strength` | 1 | "Determines Pales War Strength". |
| `War_Pales_ANSupport` | 0 | "Determines AN's support of Pales". |
| `War_Pales_LespianSupport` | 0 | "0: none 1: default 2: direct". |
| `War_Pales_PublicWarSupport` | 0 | "Determines Palesian internal support of war". |
| `War_Pales_RizianWarSupport` | 0 | (E) "Determines Rizias internal war support"; not read in dialogue. |
| `War_Media_Geopolitico_Support` | False | (E) "Geopolitico writes pro-Rizian articles". |
| `War_Sabotage_AirDefences`, `War_Sabotage_NavalDefences` | False | (E) |
| `Decision_Turn06_SabotagePales_Air/_Naval` | False | (E) |
| `War_EnemyResistanceChance` | 20 | (E) |
| `War_Pales_Resistance` | 1 | (E) "Determines if Militia spawn event gets triggered". |
| `RiziaDLCSetup.War_Operation1_Deployment_Pales_Balanced/_Northern/_Southern` | False | (D) |
| `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_Default/_Strong` | False | (D) |
| `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_NorthernDefault/_SouthernDefault` | False | (E) Unused in dialogue. |
| `RiziaDLCSetup.War_Operation1_DeploymentOverride_Pales_Naval_Default/_Strong` and `..._Naval_Default_AlesBlockaded/_Strong_AlesBlockaded` | False | (D) |
| `RiziaDLCSetup.War_Operation2_DeploymentOverride_Pales_Default/_Strong` | False | (D) |
| `RiziaDLCSetup.War_Operation2_DeploymentOverride_Pales_Naval_{Default,Strong}_{NoBlockade,PalesBlockaded,RemazBlockaded,AlesBlockaded}` | False | (D) |
| `RiziaDLCSetup.War_TileOverride_Rizia_StrongerBorderForts`, `..._Pales_StrongerBorderForts`, `..._Pales_ExtraNavalDefence`, `..._Pales_ExtraAirDefence` | False | (D) |
| `War_AttackPlan_FeignNorth` / `_FeignSouth` | False | (D) "Feigned attack in north/south". |
| `War_AttackPlan_BlockadePales` / `_BlockadeRemaz` / `_BlockadeGulfofAles`, each with `_Success` | False | (D) |

### 3.3 Operations, objectives and casualties

| Variable | Init | Notes |
|---|---|---|
| `War_Pales_Operation1_Won` | False | (E) |
| `War_Pales_Won`, `War_Pales_Loss` | False | (E). `War_Pales_Won` is also set by Turn 9 `CallWithSmolak` [67]. |
| `War_Objective_Captured_Ziqes` / `_Arces` / `_Remaz` / `_Solles` / `_Pales` | False | (E); dialogue writes them only in skipped mode. |
| `War_Objective_Captured_Terador` / `_FortAles` | **True** | Rizian cities. False means captured by Pales. |
| `War_CapturedCityCount` | 0 | (E) Not read in dialogue. |
| `War_Airfields_Captured` | 0 | (E) "number of airfields captured during war mechanics". |
| `War_Operation1_TotalPlayer_Dead`, `War_Operation2_TotalPlayer_Dead` | 0 | (E) Divisions lost. |
| `War_Operation1_TotalEnemy_Dead` | 0 | Written by dialogue in skipped mode, otherwise (E). |
| `War_Operation2_TotalEnemy_Dead` | 0 | (E) |
| `War_Operation1Briefing_Frontline` | False | (E) |
| `War_Operation2Loss_Charge` | False | (D) |
| `War_PalesOfficial_Escaped`, `Vina_Rescue_Success`, `Vina_CorrectIntel`, `Vina_WrongIntel`, `RescueSpy_CorrectIntel/_WrongIntel/_Rescued/_RescueFailed` | False | (E) War-map events. |
| `War_Used_ExperimentalWeapons` | 0 | (E) "number of times nerve gas was used in war"; also `_Pales/_Remaz/_Solles/_Ziqes` (False). |
| `Country_WarRefugees` | 0 | (E) "Counts intensity of refugee crisis". |
| `Situation_Military_War_Pales_Destruction_Light/_Moderate/_Heavy` | False | (E) Drive reconstruction cost. |
| `Situation_Military_Strong/Growing/WeakPalesResistance` | False | (E) |

### 3.4 Turn-level war engine resources (all (E) unless noted)

| Variable | Init | Notes |
|---|---|---|
| `War_ActionPoints` | 4 | Action points per war turn. Not read in dialogue. |
| `Resources_Military_AirStrikeCooldown` | 5 | (D) -1 from Derdian missiles. |
| `Resources_Military_AirSupportCooldown` | 5 | (D) -1 from Morellan planes. |
| `Resources_Military_AirStrikeRefillPerFragment` | 1 | |
| `Resources_Military_AirSupportRefillPerFragment` | 1 | |
| `Resources_Military_AirSupportUpgrade` | False | |
| `Resources_Military_BombUpgrade` | False | |
| `Resources_Military_EnemyAirStrikeCount` | 1 | (D) Set to 5 when Lespia supports directly. |
| `Resources_Military_NavalBombardmentCapability` | 0 | |
| `Resources_Military_DerdianMissiles` | False | (D) |
| `Resources_Military_RumburgianNerveGas` | False | (D) "Experimental Rumburg weapon for one-time use". |
| `Army_NumberOfMarinesInShip` | 3 | "Number of marines in one ship". |

### 3.5 Armed forces: units

Each type has `Army_Unit_<Type>_Total`, `_Deployed`, `_Dead` and `_OnMission`. Trainable types
also have `_InTraining`.

| Type | `_Total` init | `_InTraining` | Notes |
|---|---|---|---|
| Infantry | 5 | yes | |
| Support (mechanized) | 1 | yes | |
| Tank | 0 | yes | |
| Ship (destroyer) | 1 | yes | |
| Submarine | 1 | yes | |
| Flagship (RRN Valero) | 1 | — | |
| Fighter | 1 | yes | `_Dead` init is False (sic) |
| Bomber | 1 | yes | |
| Marine | 0 | — | special force |
| Paratrooper | 0 | — | special force |
| Mountaineer | 1 | — | special force |
| Azaro / Sazon / Toras (House levies) | 1 / 1 / 1 | — | |
| GoldenGuard | 0 | — | |
| Mercenary | 0 | — | |
| Militia | 2 | — | |
| GrandTotal | 13 | yes | aggregate |

### 3.6 Armed forces: stockpiles, production and posture

**Stockpiles** (`Resources_Military_*`): Equipment 1500, Manpower 3250, Tanks 25, Trucks 75,
Ships 2, Submarines 1, FighterPlanes 0, BomberPlanes 0. Each has a `..ModifiersTotal`, initial 0.

**Factory modifiers:** `Tank_Modifier_Factory` 25, `Equipment_Modifier_Factory`,
`SupportVehicle_Modifier_Factory`, `FighterPlane_Modifier_Factory`,
`BomberPlane_Modifier_Factory`, `Ship_Modifier_Dockyards`, `Submarines_Modifier_Dockyards`
(all 0). Also `Equipment_Modifier_WehlenArmsExports` -250.

**Decrees:** `Policy_RD_Order_{TankFactory,SupportVehicleFactory,MilitaryEquipmentFactory,FighterPlaneFactory,BomberPlaneFactory}_Project/_Completed`
and their `Enable_` flags; `MilitaryIndustriesConstructed` 0;
`Policy_RD_Order_PalesAdminDistrict_StrengthenedBorderForts_*`;
`Policy_RD_Order_TeradorAirforceBaseExpansion_*`; `Policy_RD_Order_ExpandFortAlesNavalBase_*`;
`Policy_RD_Economy_MonqizPortShipyardExpansion_*`.

**Arms trade counters:** `Policy_RD_Order_Bought/Sold{MilitaryEquipment,Ships,Tanks,SupportVehicles,Submarines}Count`,
`Policy_RD_Order_BoughtFighterCount`, `_BoughtBomberCount`, `_SoldFighterPlaneCount`,
`_SoldBomberPlaneCount`, `ImportArmsCount`, `ExportArmsCount`, `Title_Armslord`,
`Situation_Economy_ArmsImporter/_ArmsExporter`.

**Manpower policy:** `Policy_Military_Conscription_Default` (True), `_18_1Year`, `_18_2Year`,
`_24_2Year`; `Policy_RD_Order_LowerConscriptionAge`, `_IncreaseConscriptionDuration`,
`_IncreaseProvincialLevies`, `_RemoveProvincialLevies`;
`Policy_Military_ProvincialDivisions_Default/_HighObligation`;
`Decision_Turn06_WarEnlistments_Massive/_Moderate/_Low/_Lowest`.

**Focus:** `Focus_MilitaryBranch_Army/_Navy/_Airforce/_None`;
`Decision_Turn02_ArmySpecialForces_Marine/_Paratrooper/_Mountaineer/_None`; `Panel_WarProduction`.

**Power projection:** `Country_Military_PowerProjection` (-2),
`Situation_Diplomacy_PowerProjection_Weak` (True) / `_Average` / `_Strong` / `_Overwhelming`.
These are (E), derived by code. They are the only "army strength" readout dialogue ever gates on.

**Situation readouts** (all (E), mostly read by VictoryParade):
- `Situation_Military_Manpower_*`, `_Stockpile_*`, `_MobileForces_*`, `_RiziaFleet_*`,
  `_Marines_*`, `_Paratroopers_*`, `_Mountaineers_*`, `_Airforce_*`, `_OfficerCorps_*`,
  `_EquipmentReliance_*`, `_NavalInterdictionCapacity_*`.
- `Situation_Military_LostPrideOfNavy` (D), `Situation_Military_MorellanAirPlanes` (D).

**Budget and energy:** `Budget_Modifier_WarExpenditures` -3, `Budget_Modifier_MilitaryExpenditures`
-2, `Energy_Modifier_ArmyRefuelCosts` -1, `_NavyRefuelCosts` -2, `_AirforceRefuelCosts` -1.

### 3.7 Sanctions, tax and morale

| Variable | Init | Notes |
|---|---|---|
| `Situation_Diplomacy_ANSanctions_Heavy` / `_Weak` | False | |
| `Budget_Modifier_ANSanctions` | 0 | -6 heavy, -3 weak. |
| `Energy_Modifier_ANSanctions` | 0 | -5 heavy, -2 weak. |
| `Situation_Diplomacy_Lespia_SanctionsRizia` | False | |
| `Budget_Modifier_LespiaSanctions` | -2 | |
| `Sordland_RiziaSanction_Aye` | False | |
| `RiziaDLCIsolated.ANSanctionAccusation_Vote` | — | |
| `RiziaDLCIsolated.ANSanctionAccusation_Lespia_GlobalImage` / `_Valgsland_GlobalImage` | — | |
| `Policy_RD_Economy_WarProfitsTax` | False | |
| `Enable_Policy_RD_Economy_WarProfitsTax` | False | |
| `Budget_Modifier_WarProfitsTax` | 2 | |
| `Turn06_WarSpeech_Impact` | 0 | "Above 10 is impassionate. 5 is average. 0 and below is low." |
| `Turn06_WarSpeech_*` | False | Who stood beside the king. |
| `Taddeus_Disgraced` | False | |
| `Taddeus_Deranked` | False | |
| `Carlos_Deranked` | False | |
| `Taddeus_Arrested` | False | |
| `Lucita_Arrested` | False | |

### 3.8 Outcomes

| Variable | Init | Notes |
|---|---|---|
| `War_Pales_Loss_SueForPeace` | False | |
| `War_Outcome_NoPeace_KeepFighting` | False | |
| `War_ParliamentProposesPeace` | False | (D) Turn 10 `Reform_ConstitutionSigning` [50]. |
| `Decision_Turn10_Ceasefire_Accepted` | False | (E) Decision panel. |
| `War_RumburgCeasefireRequested` | False | (D) Turn 9 `GRACERumburgMeeting` [3]. |
| `War_WehlenJoined` | False | (D) Turn 9 `CallWithSmolak` [67]. |
| `War_BorderOutcome_Peace_PreWarBorders` / `_Annexation` / `_PalesDivided` / `_PalesAdminDistrictLost` | False | |
| `War_Outcome_Aureus` / `_PortOfPales` / `_Neutrality` | False | |
| `PalesOwnership_RiziaImperii` / `_Azaros` | False | |
| `Pales_Reconstruction_EnoughFunds` / `_Installments` / `_NoFunds` | False | |
| `Budget_Modifier_Pales_Reconstruction` | 0 | |
| `Pales_Reparations_EnoughFunds` / `_InstallmentsPossible` / `_Agreed` | False | |
| `ReparationsAccepted_Immediate` / `_Installments` / `_NoFunds` | False | |
| `Budget_Modifier_Pales_Reparation` | -2 | |
| `Pales_DidntRatifyTreaty` | False | |
| `War_Pales_Won_AxelTortured` | False | |
| `Axel_Arrested` | False | |
| `Win_Renamed_CastlePales` / `_CastleRomus` / `_CastleToras` / `_CastleVictory` | False | |
| `Authority_Modifier_TerritorialReclaimation` | — | |
| `Authority_Modifier_TerritorialIntegrityLost` | — | |
| `MapLayer_*Pales*` | — | Applied through `EvaluateConditionalInstruction("MapChange_Pales")` ([459]). |

---

## 4. War outcomes, gates and consequences

### 4.1 Victory: `War_Pales_Won == true`

**`War_Pales_VictoryParade`:**
- The castle in Pales is renamed: `Win_Renamed_CastleRomus/_Toras/_Victory/_Pales` ([129]–[132]),
  or `..._CastleNotRenamed`.
- Duke Axel's fate: arrested ([190] `Axel_Arrested`) or tortured ([124]/[309]
  `War_Pales_Won_AxelTortured` and `Axel_Arrested`; [315] reverses the torture flag).
- Chemical cleanup reports for each `War_Used_ExperimentalWeapons_<City>`.
- Achievement `UnlockAchievement("Rizia_Conqueror")` ([153]), **only if `War_Control_Skipped == false`** ([155]).
- `EnableCollectionItem("CollectionItem_PalesCrown")` ([358]); `Rizia_OfficerSazon` ([373]).
- Manus Sazon war-hero branch ([161]–[175]).
- Readouts of `Situation_Military_*` (manpower, fleet, marines, air force, `MorellanAirPlanes`,
  `LostPrideOfNavy`).
- Wehlen lines if `War_WehlenJoined`.

**`War_Pales_PeaceTreaty`, victory branch** ([6] → [4] → the renamed castle → [45]):
- If `Reform_Success`, the reform government has already negotiated ([923]).
- If `War_WehlenJoined`, Smolak (as co-belligerent) reads the terms: shared Port of Pales,
  Aureus split with Wehlen, territory to Rizia ([363]–[368] → [790]). Wehlen's landing is
  called "Operation Moonlight Sonata" ([58]).
- Otherwise the king announces the terms ([362]):

  | Choice | Then | Sets |
  |---|---|---|
  | [412] full annexation | [822] | `War_BorderOutcome_Peace_Annexation` (+1 or +2 `Authority_Modifier_TerritorialReclaimation`), `War_Outcome_Aureus`, `Policy_Economy_AureusField_Extractions`, codex "Pales_Conquered" ([816], [844], [865], [1045]) |
  | [413] annex the eastern territories | [880] → [416] | `War_BorderOutcome_Peace_PalesDivided`, +1 reclamation authority |
  | [414] withdraw | [417] | `War_BorderOutcome_Peace_PreWarBorders`, plus any of [415] `War_Outcome_Aureus`, [418] `War_Outcome_PortOfPales`, [419] `War_Outcome_Neutrality` |

  The withdrawal route can only close ("These are our terms.", [440]) once at least one demand
  is chosen: [438] requires `not PreWarBorders`, or `PreWarBorders` plus one of the three
  outcomes.
- **Reconstruction:** depends on `Situation_Military_War_Pales_Destruction_*`. Annexation costs
  5, 3 or 1 Budget (Heavy, Moderate, Light); a divided Pales costs 4, 2 or 1. The player pays now
  (`Pales_Reconstruction_EnoughFunds`), pays in installments (`Pales_Reconstruction_Installments`
  plus `Budget_Modifier_Pales_Reconstruction` -1 to -3), or cannot pay
  (`Pales_Reconstruction_NoFunds`).
- **Ownership:** [454] `PalesOwnership_RiziaImperii`, or [455] `PalesOwnership_Azaros` (governed
  by House Azaro). Token statuses are added to `Rizia_City_Pales/Remaz/Ziqes/Solles` ([465]–[472]).
- Income tax bonus: `Budget_Modifier_IncomeTax` +1 to +5 ([479]–[509]).
- Aureus extraction: `Energy_Modifier_AureusGasField` 8 or 4 ([516]/[517]); post-war extraction
  level set by destruction.
- [459] `EvaluateConditionalInstruction("MapChange_Pales")`; [460] `Pales_War = false`.
- **Achievement `Rizia_GreatUnification`** ([523]) if
  `War_BorderOutcome_Peace_Annexation and ZilleReturn_Success`.
- **If `War_WehlenJoined`** ([552]): `DisableToken("Events_Zille")`,
  `Reports_Turn10_Wehlen_ZilleTransferred`, `Authority_Modifier_TerritorialIntegrityLost - 1`,
  `Policy_Diplomacy_ZLLAActive = false`. Rizia gives Zille away.

### 4.2 Loss: `War_Pales_Loss == true`

**`War_Pales_Loss`:**
- [16]: `War_Pales_Operation1_Won` chooses the situation. If true, "Pales still stands…
  retaking Arces… equal footing". If false, "pushed back… lost Terador and Fort Ales. The
  frontline is broken."
- The player decides:
  - [76] sue for peace → `War_Pales_Loss_SueForPeace = true`.
  - [77] keep fighting → `War_Outcome_NoPeace_KeepFighting = true`.
- If `Reform_Success`, the government forces peace ([397]).
- `War_ParliamentProposesPeace` and `Reform_Monarch_Veto` change the lines.
- Punishments:
  - `Taddeus_Deranked` or `Carlos_Deranked`.
  - Arrest Taddeus, or Taddeus and Lucita together. This sets `Taddeus_Arrested`,
    `Lucita_Arrested`, `Council_Security_*`, ends the romance and moves the House Azaro faction
    to Carlos.
  - `AssignConnection("Page_Council","Position_WarInternalSecurity","")`.

**`War_Pales_PeaceTreaty`, loss and ceasefire branches** ([7] → [13]):

| Route | Gate | Terms |
|---|---|---|
| Rumburg-mediated ceasefire, Thornborough | `Decision_Turn10_Ceasefire_Accepted and not War_ParliamentProposesPeace` ([14]) | Beatrice reads the terms; pre-war borders ([285] `War_BorderOutcome_Peace_PreWarBorders`) |
| Parliament-negotiated, Konstantium | `Decision_Turn10_Ceasefire_Accepted and War_ParliamentProposesPeace` ([18]) | **If Operation 1 was won:** withdrawal plus half of Aureus ([184], [202]) → [249]/[250] `PreWarBorders`, `War_Outcome_Aureus`, `Situation_Economy_AureusField_MediumExtractionPostWar`. **If it was lost:** Rizia cedes the Pales Administrative District ([153]). |
| Duke's terms after suing for peace, Operation 1 won | [11] → [369] | Withdrawal, but Pales cedes Aureus. The player may push for more: [385] keep occupied land → [400] → [588], where Axel demands reparations scaled by destruction level, leading to PalesDivided with reparations ([401]/[696]); [386] Port of Pales ([398]), [387] neutrality ([399]), or accept ([402]: `PreWarBorders`, `War_Outcome_Aureus`, `HighExtraction`). |
| Duke's terms, Operation 1 lost ("Palesian forces destroyed our defenses in Fort Ales", [25]) | — | [706]/[752] `War_BorderOutcome_Peace_PalesAdminDistrictLost`, `Authority_Modifier_TerritorialIntegrityLost - 1`, codex Terador and Fort Ales "PartOfPales", plus reparations (`Pales_Reparations_*`, `ReparationsAccepted_*`, `Budget_Modifier_Pales_Reparation` -1 to -3) |

Refusing any treaty ([259], [260], [268] → [251]) sets `War_Outcome_NoPeace_KeepFighting = true`
and `Pales_War = true`: the war goes on past the end of the game. Refusing to ratify a
government treaty sets `Pales_DidntRatifyTreaty` ([1137]). Achievement
`UnlockAchievement("Rizia_PalesWarLost")` ([524]) fires on the "put the bloodshed behind us"
path ([356]). `Rizia_DividedRizia` ([1145]) requires `PalesAdminDistrictLost`,
`not ZilleReturn_Success`, `Situation_Diplomacy_Rumburg_SellPortOfMonqiz` and a Caleqabiz lease.

### 4.3 Stalemate and continued war

There is no dedicated stalemate flag. The data expresses it two ways:
- `War_Outcome_NoPeace_KeepFighting` (the Loss scene or a refused treaty).
- Ending Speech: `War_Pales_Operation1_Won and not War_Pales_Won and not War_Outcome_NoPeace_KeepFighting`,
  meaning Operation 1 won, Operation 2 failed and peace was negotiated.

### 4.4 Wehlen-assisted victory

Turn 9 `FPnT_CallWithSmolak`: Smolak offers "Zille for Pales" ([53]). Accepting ([61]) leads to
[67]:

```
War_Pales_Won = true; War_WehlenJoined = true; ZilleReturn_Success = false;
ZilleReferendum_SmolakOffer_Accepted = true; (+ News_Turn09_*_SmolakCall_WehlenJoinsWar)
```

Refusing sets `ZilleReferendum_SmolakOffer_Rejected`. **For this mod, that makes a Zille war
mutually exclusive with `War_WehlenJoined`.**

### 4.5 Epilogue and ending lines

- **Turn 11 `Ending_Speech`:**
  - [91] `War_Pales_Won`; [97]/[102] which incident; [111] `War_Control_Skipped` (thanks General
    Azaro).
  - [116] Annexation: "full reunification of Pales". [119] PalesDivided: "Arces and Ziqes were
    finally restored".
  - [16]/[19]/[614] `Pales_Rizia_Unification or Annexation`, plus `ZilleReturn_Success` for the
    "Great Unification".
  - [272]/[497] `War_Outcome_NoPeace_KeepFighting`.
- **`Continued_Reign`:** [42] `War_Pales_Won` ("congratulated House Azaro"); [46] `War_Pales_Loss`
  ("the day for Pales would yet come"); [168]/[630] KeepFighting ("extremely costly").
- **`Epilogue`:**
  - [35] `(Annexation or Pales_Rizia_Unification) and ZilleReturn_Success`.
  - [39] Annexation.
  - [63] `PalesAdminDistrictLost and ZilleReferendum_Lost and PowerProjection_Weak` (the
    worst-case line).
  - [31] the golden-age ending, which also requires Annexation or Unification plus
    `ZilleReturn_Success`.
  - The pacifist line requires `War_Happened == false`.
- **Turn 10 `Pales_Reform_Branch`:** [24]/[25] `Pales_War`; [35] `Pales_Rizia_Unification`; [32]
  `War_Pales_Won`; reparations and reconstruction readouts.

---

## 5. The armed forces mechanic as seen in the data

### 5.1 Unit types

| Category | Units |
|---|---|
| Land | Infantry, Support (mechanized support divisions), Tank, Mountaineer, Paratrooper, Marine |
| House levies | Azaro, Sazon, Toras |
| Other land | GoldenGuard, Mercenary, Militia |
| Sea | Ship (destroyers), Submarine, Flagship (RRN Valero) |
| Air | Fighter, Bomber, plus the abstract air-strike and air-support abilities |

House doctrine ([562]–[566]): Azaro is offensive, Sazon is organised support, Toras is balanced
and defensive. Marines ride ships (`Army_NumberOfMarinesInShip` = 3); without marines no
meaningful naval invasion is possible ([226]–[229]).

### 5.2 How units are acquired

- **Production panel:** `Panel_WarProduction` is enabled in Turn 1. The panel turns
  `Resources_Military_Manpower/Equipment/Tanks/Trucks/Ships/Submarines/FighterPlanes/BomberPlanes`
  into units over time; `_InTraining` exists for Infantry, Support, Tank, Ship, Submarine,
  Fighter and Bomber. This is engine work: no dialogue writes `_InTraining`.
- **Decrees:** factories (Tank, SupportVehicle, MilitaryEquipment, FighterPlane, BomberPlane,
  feeding `*_Modifier_Factory`), the Monqiz shipyard expansion (dockyard modifiers), the Terador
  airbase, the Fort Ales naval base, stronger Pales Administrative District border forts,
  conscription decrees and provincial levies.
- **Arms buying and selling:** the `Bought*/Sold*Count` counters and `ImportArmsCount` /
  `ExportArmsCount`.
- **Starting posture:** `Focus_MilitaryBranch_*` and the Turn 2 special-force decision.
  The prologue [331] adds Support +2, Tank +1, Ships +1, Equipment +1000 and Manpower +500 when
  the player chooses "strengthening the armed forces" ([330]).

### 5.3 Foreign military aid written by dialogue

| Source | Entry | Effect |
|---|---|---|
| Morella (Turn 5 `EnT_MorellaNegotiationFeedback`) | [1033] | `Situation_Military_MorellanAirPlanes`, `AirSupportCooldown - 1` |
| Morella | [1050] | Ships +1, Submarines +1, Equipment +1000 |
| Wehlen (Turn 5 `FPnT_WehlenNegotiations`) | [1472] | `Army_Unit_Ship_Total - 2` |
| Rumburg (Turn 7 `FPnT_RumburgMeeting`) | [578]/[1033] | Ships +2 |
| Rumburg | [594], [597] | Tanks +50, Trucks +150 |
| Rumburg | [668] | Mercenary +1, `Resources_Military_RumburgianBFFMercenaries` |
| Rumburg | [1599]/[1621] | `Resources_Military_RumburgBasicAid`, Equipment +1500 |
| Rumburg | [356]/[363] | `Resources_Military_RumburgianNerveGas` |
| Derdia (Turn 7 `Fighting_Assistance_Friendly/_Unfriendly`) | various | Mercenary +1 with `Resources_Military_DerdianMercenaries_Pales`, or `_DerdianMercenaries_Zille` (which also raises `Zille_ViolentResistancePower`); `Resources_Military_DerdianMissiles` with `AirStrikeCooldown - 1`; Ships +2 |
| Lespia (Turn 8 `AlvarezVisit`) | [1072] | BomberPlanes +100, FighterPlanes +200, Tanks +150 |
| Turn 10 House gathering | [496] | Azaro +1, Sazon +1, Equipment and Manpower +250 |
| Turn 3 Zille debrief | [187] | GoldenGuard +1 |

### 5.4 Limits

- Blockades need `Ship_Total + Submarine_Total` ≥ 6 (Pales), ≥ 5 (Ales) or ≥ 4 (Remaz).
- Briefing tiers (thresholds only; nothing blocks the war): Infantry ≥ 10, Support ≥ 7,
  Tank ≥ 6, Ship ≥ 5, Submarine ≥ 4.
- The only hard capacity is marines per ship (3).
- Diplomacy gates on armed strength through `Situation_Diplomacy_PowerProjection_*` only: the
  Pales alliance, Lespian support, the Duke's tone, and the skipped-mode loss rates.
- Equipment and manpower are drained by many unrelated deals, for example Intermerkopum
  Equipment -5000 / -1000 and Manpower -500, and the Turn 9 mediations, Manpower -250 to -500.

---

## 6. Template: a second war (Wehlen, for Zille)

### 6.1 Reuse as-is (shared with the Pales war)

- **The army.** `Army_Unit_*` (Total, Deployed, Dead, OnMission, InTraining), the
  `Resources_Military_*` stockpiles and cooldowns, `War_ActionPoints`, the factories and
  decrees, `Panel_WarProduction`, and `Situation_Diplomacy_PowerProjection_*`. Pales-war losses
  should carry into the Zille war; check `Army_Unit_*_Dead` and the Pales outcome.
- **Hooks that already exist and are unused or barely used:**
  - `CountryRelation_Wehlen_AtWar` (never touched in dialogue).
  - `RiziaDLCText.ZilleWarAlly`.
  - `Resources_Military_DerdianMercenaries_Zille`.
  - `ZilleOwnership_RiziaImperii` / `ZilleOwnership_Brenas` (the analogue of
    `PalesOwnership_RiziaImperii` / `_Azaros`).
  - `Zille_PromisedToHouseSazon` (the analogue of `Pales_PromisedToHouseAzaro`).
  - `MapLayer_World_Zille_Rizia`, `MapLayer_Zille_Brenas`, `MapLayer_Zille_OwnState`,
    `MapLayer_World_Flag_Zille`.
  - `Epilogue_CivilWar_ZilleAlly`.
  - Zille resistance state: `Zille_ViolentResistancePower`,
    `Situation_Diplomacy_Zille_*ArmedResistance`, `Situation_Diplomacy_Zille_SuOminaActive` /
    `_ATAActive`.
- **`ZilleReturn_Success`.** Setting it on victory automatically lights up `Rizia_GreatUnification`
  (with Pales annexation), the "Great Unification" lines in `Ending_Speech` [19]/[614], and the
  epilogue's [35] and golden-age [31] lines.
- **Sanctions machinery.** `Situation_Diplomacy_ANSanctions_Heavy/_Weak`,
  `Budget_Modifier_ANSanctions`, `Energy_Modifier_ANSanctions`. If both wars happen, decide
  whether to stack or reuse (vanilla sets them to fixed values, not increments).
- **Shared aftermath flags.** `War_Happened` (pacifist epilogue), `War_Used_ExperimentalWeapons`,
  `Country_WarRefugees`, `Situation_Military_Monarch_OverseesWar` / `_HighCommand_OverseesWar`.
- **Scenes to copy as structural templates:**
  - `SnO_WarEruptionBriefing`: hidden strength calculation, status briefing, orders, delegation,
    then `EnableToken`.
  - `SnO_War_Pales_PostOperation1` with `Operation 1 Results` and `Operation 2 Next Steps`, using
    the same `c<id>:<entry>` chaining.
  - `FPnT_ANSession` / `Sanctions`.
  - `War_Pales_VictoryParade`, `War_Pales_Loss`, `War_Pales_PeaceTreaty`.

### 6.2 Exclusions and interactions to handle

- **Wehlen alliance.** If `War_WehlenJoined` or `ZilleReferendum_SmolakOffer_Accepted` is set,
  Zille has been traded away (`DisableToken("Events_Zille")` at PeaceTreaty [552]). Block the
  war, or turn it into a betrayal scenario.
- **Peaceful Zille routes.** Block or reconcile with `ZilleReturn_Success` (negotiations: Turn 8
  `FPnT_ZilleNegotiations`, `Turn08_ZilleNegotiations_*`), `ZilleReferendum_Won/_Lost/_Independence*`,
  and the vanilla army option `ZilleReferendum_Interfere_Army` / `_Success` / `_Fail` (Turn 9
  `FPnT_ZilleReferendumInterference`).
- **Two simultaneous wars.** The war-map token, `RiziaDLCSetup.IsAnyOperationInProgress`,
  `War_Control_Skipped` and the `War_Control_Skipped_*` snapshot are shared and singular. Give
  Zille its own skipped flag and snapshot, or forbid overlap.

### 6.3 New variables to create (proposed names mirroring vanilla)

**War state:**
- `Zille_War` (the switch; analogue of `Pales_War`).
- `War_Zille_Incident_*`.
- `Intention_Wehlen_DeclareWar`.
- `Diplomacy_Wehlen_PrepareForWar` / `Diplomacy_Wehlen_DeclareWar`.
- `War_Zille_Control_Skipped`, plus its own `War_Zille_Control_Skipped_<Unit>` snapshot.

**Enemy strength:**
- `War_Zille_Strength` (start 1, with the same thresholds: 3 for no override, 6 for Default).
- `War_Zille_ANSupport`.
- `War_Zille_ForeignSupport`: who backs Wehlen. The data ties Wehlen to Operation Bear Trap and
  to Smolak, and Lespia to the Zille bombing (`Zille_Discovered_LespianInvolvement`).

**Plans and setup overrides:**
- `War_Zille_AttackPlan_*`: feints and blockades of Zille harbour, Nanta and the Strait of
  Kreleck. Copy the ship-count thresholds (≥ 4/5/6) and losses.
- `RiziaDLCSetup.War_Zille_Operation{1,2}_Deployment*` and `..._DeploymentOverride_*` (naval
  overrides keyed by blockade success).
- `RiziaDLCSetup.War_Zille_TileOverride_*`.

**Operations and objectives:**
- `War_Zille_Operation1_Won`.
- `War_Zille_Won` / `War_Zille_Loss`.
- `War_Zille_Operation{1,2}_TotalPlayer_Dead` / `_TotalEnemy_Dead`.
- `War_Zille_Objective_Captured_<City>`: Zille, Nanta, Isor start False. Rizian Mave Nortus and
  Dapalle start **True**, like Fort Ales and Terador.
- `War_Zille_Resistance`.
- `Situation_Military_War_Zille_Destruction_Light/_Moderate/_Heavy`.

**Outcomes:**
- `War_Zille_Loss_SueForPeace`, `War_Zille_Outcome_NoPeace_KeepFighting`.
- `War_Zille_BorderOutcome_Peace_Annexation` / `_PreWarBorders` / `_Divided` /
  `_BrenasLandLost` (loss of Mave Nortus or Dapalle).
- `War_Zille_Outcome_*`: demands such as the gold mines, the harbour, or Wehlen neutrality.
- `Zille_Reconstruction_*`, `Zille_Reparations_*`, `Budget_Modifier_Zille_Reconstruction` /
  `_Reparation`.

### 6.4 Scene list (turn placement suggestion; vanilla Zille diplomacy peaks in Turns 7–9)

1. **Setup** (Turn 1–5, optional): Zille resistance funding and Derdian mercenaries already
   exist. Add a pre-war option, analogous to the drillship blockade, that creates the casus
   belli.
2. **Casus belli council and incident** (copy `A_CouncilMeeting` [591] and `SnO_Incident`): an
   incident selector writes `War_Zille_Incident_*`; an early-invasion option skips it. Note that
   vanilla gates nothing on army strength. If the mod wants a gate, use
   `Situation_Diplomacy_PowerProjection_*` and `Army_Unit_*` explicitly.
3. **Post-incident meeting with Smolak:** a peace deal versus war (`Zille_War = true`,
   `Diplomacy_Wehlen_NegotiatingPower = -100`, `CountryRelation_Wehlen_AtWar = true`).
   Optionally add an ultimatum phone call like `FPnT_DukeWarPhoneCall`.
4. **War eruption briefing:**
   - Hidden enemy strength from AN and foreign support; forts; naval overrides.
   - Army status readout using the same `Army_Unit_*` thresholds.
   - Orders: feint, blockade or standby; personal command or delegation (snapshot the army);
     marines enable an amphibious option.
   - `EnableToken("<new Zille invasion event>")`, plus news and a `War_Happened = true` flag.
5. **Home front** (next turn): copy `Pales War Update And Tax` (opinion and a war tax; reuse the
   decree or a new one), and add war events as decision-panel items.
6. **Operation 1 debrief → Operation 2 briefing:** copy the c410 → c411 → c412 chain. Include
   the skipped-mode auto-resolve table from section 2.3, keyed to power projection: losses of
   1/5, 1/4, 1/3 or 1/2, and fixed captures.
7. **International reaction:** an AN accusation and vote (reuse the Sanctions pattern with a
   `Vote` counter and Heavy/Weak results), a patron's support or sanctions deal, and optional
   ceasefire mediation.
8. **Outcome scenes:**
   - Victory parade.
   - Loss scene: sue for peace or keep fighting; punish the generals.
   - Peace treaty: annex Zille (`ZilleReturn_Success = true`, `ZilleOwnership_*`, map layers),
     partial gains, pre-war borders with demands, or the loss of Brenas border towns with
     reparations; reconstruction cost by destruction level and budget.
   - Set `Zille_War = false` on peace, and keep it true on "keep fighting".
9. **Aftermath:** make sure `ZilleReturn_Success` and the new outcome flags reach the Turn 10–11
   ending scenes. Add your own lines alongside the vanilla `War_Pales_*` checks in
   `Ending_Speech`, `Continued_Reign` and `Epilogue`. Vanilla lines read only Pales variables,
   apart from `ZilleReturn_Success`.

### 6.5 Engine caveat

Everything marked (E) in section 3 is written by the war-map code driven by the
`Rizia_Event_InvasionOfPales` token: operation wins, captures, casualties, destruction,
resistance, action points and cooldowns. A Zille war either needs:
- an equivalent war-map event and map (see `docs/zille-geography.md`), or
- a dialogue-only resolution in which the mod sets those result variables itself. The
  `War_Control_Skipped` branch in PostOperation1 is the vanilla precedent, but even there
  `War_Pales_Operation1_Won` comes from the engine.
