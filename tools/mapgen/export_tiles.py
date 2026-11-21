"""Export tiles to the game format: 512x512 RGBA masks (Pales encoding) + zille_tiles.json.

usage: export_tiles.py <tiles_dir with labels.npy + tiles.json> <out_dir> [layout.json] [towns.json]
Coordinates in the JSON are normalized to the whole map texture (u right, v down, 0..1),
so the runtime can place quads against the actual panel geometry.
"""
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

src, out = sys.argv[1:3]
layout = json.load(open(sys.argv[3])) if len(sys.argv) > 3 else None
os.makedirs(f"{out}/tiles", exist_ok=True)
labels = np.load(f"{src}/labels.npy")
spec = json.load(open(f"{src}/tiles.json"))
K = spec["scale"]
FW, FH = spec["frame_size"]
H, W = labels.shape
MASK = 512
QUAD_MARGIN = 1.55            # quad half-size / farthest tile pixel from the centre (room for the outline glow)
DILATE, ERODE, SIGMA = 30, 24, 10   # fitted against vanilla PalesTile_* masks (mask px)

def to_sheet_uv(u, v, size=False):
    """Map uv inside the map content to uv on the whole 12288x8192 sheet (Pales layout)."""
    if layout is None:
        return [u, v]
    x0, y0, x1, y1 = layout["content_box"]
    tw, th = layout["texture_size"]
    if size:
        return [u * (x1 - x0) / tw, v * (y1 - y0) / th]
    return [(x0 + u * (x1 - x0)) / tw, (y0 + v * (y1 - y0)) / th]


TOWN_PTS = {}
if len(sys.argv) > 4 and layout is not None:
    _fx0, _fy0 = layout["frame_crop"][:2]
    for _tw in json.load(open(sys.argv[4])):
        if _tw["x"] is not None:
            TOWN_PTS[_tw["name"]] = ((_tw["x"] - _fx0) * K, (_tw["y"] - _fy0) * K)

# label id per tile name, recovered by sampling the centroid region
name_to_label = {}
for t in spec["tiles"]:
    x0, y0, x1, y1 = [v * K for v in t["bbox"]]
    sub = labels[y0:y1, x0:x1]
    vals, cnt = np.unique(sub[sub > 0], return_counts=True)
    # the tile's own label is the one whose pixel count matches its area best
    best = min(zip(vals, cnt), key=lambda vc: abs(vc[1] - t["area"] * K * K))
    name_to_label[t["name"]] = int(best[0])

out_tiles = []
for t in spec["tiles"]:
    lid = name_to_label[t["name"]]
    x0, y0, x1, y1 = [v * K for v in t["bbox"]]
    tile = labels[y0:y1, x0:x1] == lid
    # quad centred on the tile's visual centre (deepest interior point): tokens stand at the quad centre
    dist = ndi.distance_transform_edt(np.pad(tile, 1))[1:-1, 1:-1]
    py, px = np.unravel_index(np.argmax(dist), dist.shape)
    cx, cy = x0 + px + 0.5, y0 + py + 0.5
    if t.get("town") in TOWN_PTS:                     # town tiles: the token stands on the town itself
        tx, ty = TOWN_PTS[t["town"]]
        if 0 <= int(ty) - y0 < tile.shape[0] and 0 <= int(tx) - x0 < tile.shape[1] and tile[int(ty) - y0, int(tx) - x0]:
            cx, cy = tx, ty
    ys_, xs_ = np.nonzero(tile)
    half = max(np.abs(xs_ + x0 + 0.5 - cx).max(), np.abs(ys_ + y0 + 0.5 - cy).max())
    side = 2 * half * QUAD_MARGIN
    half = side / 2
    sx0, sy0 = int(round(cx - half)), int(round(cy - half))
    n = int(round(side))
    crop = np.zeros((n, n), np.float32)
    ix0, iy0 = max(sx0, 0), max(sy0, 0)
    ix1, iy1 = min(sx0 + n, W), min(sy0 + n, H)
    crop[iy0 - sy0:iy1 - sy0, ix0 - sx0:ix1 - sx0] = labels[iy0:iy1, ix0:ix1] == lid
    shape = np.asarray(Image.fromarray((crop * 255).astype(np.uint8)).resize((MASK, MASK), Image.LANCZOS)).astype(np.float32) / 255
    hard = shape > 0.5
    pad = ndi.distance_transform_edt(~hard) <= DILATE
    inner = ndi.gaussian_filter((ndi.distance_transform_edt(hard) > ERODE).astype(np.float32), SIGMA)
    A = shape * 255
    B = np.where(pad, 0, 255)
    G = 255 - inner * 255
    R = np.where(pad, inner * 255, 255)
    rgba = np.stack([R, G, B, A], -1).clip(0, 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{out}/tiles/{t['name']}.png")

    rec = {k: t.get(k, False) for k in ("name", "type", "region", "neighbors", "shore", "border", "mountain", "fort")}
    for k in ("town", "defence", "fort_xy"):
        if k in t:
            rec[k] = t[k]
    rec["quad_center_uv"] = to_sheet_uv(cx / W, cy / H)
    s_u, s_v = to_sheet_uv(1, 1, size=True)
    rec["quad_size_uv"] = [side / W * s_u, side / H * s_v]
    rec["centroid_uv"] = to_sheet_uv(t["centroid"][0] / FW, t["centroid"][1] / FH)
    out_tiles.append(rec)

towns = []
if len(sys.argv) > 4:
    for tw in json.load(open(sys.argv[4])):
        tile = next((r for r in out_tiles if r.get("town") == tw["name"]), None)
        if tile is None:
            continue
        x, y = (tile["fort_xy"] if tw["role"] == "fort" else (tw["x"], tw["y"]))
        fx0, fy0, fx1, fy1 = layout["frame_crop"]
        towns.append({**tw, "x": x, "y": y, "tile": tile["name"],
                      "uv": to_sheet_uv((x - fx0) / (fx1 - fx0), (y - fy0) / (fy1 - fy0))})
json.dump({"version": 2, "map_size_px": [12288, 8192], "towns": towns, "tiles": out_tiles}, open(f"{out}/zille_tiles.json", "w"), indent=1)
print("exported", len(out_tiles), "tiles to", out)

# preview: composite of all tile alpha masks back onto a map-sized canvas (checks placement)
prev = np.zeros((800, 1200), np.float32)
for rec in out_tiles:
    m = np.asarray(Image.open(f"{out}/tiles/{rec['name']}.png").split()[3]).astype(np.float32) / 255
    s = int(round(rec["quad_size_uv"][0] * 1200))
    if s < 2:
        continue
    m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((s, s), Image.BILINEAR)).astype(np.float32) / 255
    cx, cy = rec["quad_center_uv"][0] * 1200, rec["quad_center_uv"][1] * 800
    x0, y0 = int(round(cx - s / 2)), int(round(cy - s / 2))
    xs0, ys0 = max(x0, 0), max(y0, 0)
    xs1, ys1 = min(x0 + s, 1200), min(y0 + s, 800)
    prev[ys0:ys1, xs0:xs1] += m[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0]
Image.fromarray((np.clip(prev, 0, 2) * 120).astype(np.uint8)).save(f"{out}/_placement_check.png")
