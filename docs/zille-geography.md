# Zille war map: geography source notes

Canonical source: the in-game national map of Rizia (`T_Map_Rizia_01..04`, 4096² each,
in the `riziamaptextures` Addressables bundles). Zille is the orange-bordered enclave at
Rizia's north-west tip. The codex page (codex.torporgames.com/the-territory-of-zille)
adds only lore: "northernmost tip of Rizia by the Strait of Kreleck", fishing hub, then
spas and resorts, then gold mining.

## Places (from the national map)

**Zille territory (Wehlen-held)**
- Zille: main city and harbor, south-west of the territory, on a bay with small islands.
- Nanta: north coast port; railway and main road run Nanta to Zille.
- Isor: west coast; road to Zille and a coastal road to Nanta.
- A river runs from near Nanta south to the land border by Dapalle.

**Rizian side (Brenas province)**
- Mave Nortus: on the Zille border, north-east coast. A fortified border crossing ("a
  concrete barrier ... strung with barbed wire"), and the royal train runs there.
- Dapalle: just across the land border, east of Zille.
- Balle, with Lake Jardim beside it; Sudiseri Maraton (Sudis Sea coast); Jales (coastal
  city, east); Qatrol; Elle Nors (rail junction); Sallabes (Gulf of Moridia coast).
- Rail: Zille to Elle Nors to Delam to Porte Drazon. Red main roads: Zille to Sallabes,
  Zille to Elle Nors to Bronaz, Jales to Porte Drazon.

**Seas, islands and neighbours**
- Gulf of Moridia (west), Sudis Sea (north-east), Strait of Kreleck (north-west).
- Islands: Kreleck Isles (in the strait), Manes and Riuz (off Nanta), Rozanada, Sarke, Wals,
  Rotsla.
- Wehlen: Kratsna port, directly across the strait. Derdia: Yol, to the west across the gulf.
  Lespia: Veno Robin, San Montua and Pelmo, on the far shore of the Sudis Sea.

## Story facts from the dialogue database (useful for the war design)
- Zille has a "rocky coast, beachside resorts, and mining towns", gold mines, a harbor, and
  "millions of people". It is governed by Wehlen under President Smolak.
- The resistance is Su Omina (armed) and the ATA (protest movement). There are Derdian
  mercenaries in the region, and Morella ships supplies across the Gulf of Moridia.
- Relevant variables and fragments are under `RiziaDLCIsolated.*Zille*` and
  `RiziaDLCSupport.*Zille*` (Turn01 to Turn10), and `RiziaDLC.ZilleReturn_Success`.

## Comparison with the Pales war map
- `Map_Rizia_PalesWar`: 6 panels of 4096² (12288×8192 total), 100 land and 33 sea tiles.
  It covers about 2700 px of national-map width, roughly 4× magnification.
- Style: flat stepped grey elevation bands with thin contours, a doubled black coastline,
  sea #B9C6C8, dark-grey urban blobs, blue-grey rivers, black roads, ticked railways and
  serif labels.
