"""Build the divided-Zille map layer inputs (art/build/strategic).

usage: strategic.py <zille_brenas.png> <zille_ownstate.png> <tiles7 dir with labels.npy + tiles.json> <out_dir>
The two PNGs are vanilla map layer textures (maplayertextures/rizia_zille_brenas / _ownstate bundles).
Layer texture <-> zille_region_full: x_l = 0.584 * x_r + 122, y_l = 0.584 * y_r - 365 (fitted on the red road network).
zille_region_full -> war frame: frame = region - (300, 240 + 372); labels = frame * scale.
Output: zille_tile_index.png (RGB, R = 1-based index of the Zille land tile under each layer pixel, 0 = Brenas proper)
and zille_tile_index.json (tile names in index order).
"""
import json, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

brenas, ownstate, src, out = sys.argv[1:5]
labels = np.load(f"{src}/labels.npy"); spec = json.load(open(f"{src}/tiles.json")); K = spec["scale"]
name_to_label = {}
for t in spec["tiles"]:
    x0, y0, x1, y1 = [v * K for v in t["bbox"]]
    sub = labels[y0:y1, x0:x1]; vals, cnt = np.unique(sub[sub > 0], return_counts=True)
    name_to_label[t["name"]] = int(min(zip(vals, cnt), key=lambda vc: abs(vc[1] - t["area"] * K * K))[0])
zille = [t["name"] for t in spec["tiles"] if t["region"] == "zille" and t["type"] == "Land"]
lab2idx = np.zeros(labels.max() + 1, np.int16)
for t in spec["tiles"]:
    if t["type"] == "Land":
        lab2idx[name_to_label[t["name"]]] = -1
for i, n in enumerate(zille):
    lab2idx[name_to_label[n]] = i + 1
L = np.asarray(Image.open(brenas).convert("RGBA")); alpha = L[..., 3] > 0
S, BX, BY = 0.584, 122.0, -365.0
yy, xx = np.mgrid[0:L.shape[0], 0:L.shape[1]]
ix = np.round(((xx - BX) / S - 300) * K).astype(int); iy = np.round(((yy - BY) / S - 240 - 372) * K).astype(int)
inside = (ix >= 0) & (ix < labels.shape[1]) & (iy >= 0) & (iy < labels.shape[0])
lab = np.zeros(L.shape[:2], np.int32); lab[inside] = labels[iy[inside], ix[inside]]
idx = lab2idx[lab]
d, (ny, nx) = ndi.distance_transform_edt(idx == 0, return_indices=True)
filled = idx[ny, nx]; filled[~inside] = 0; filled[d > 40] = 0
res = np.where(filled > 0, filled, 0).astype(np.uint8); res[~alpha] = 0
Image.merge("RGB", [Image.fromarray(res)] * 3).save(f"{out}/zille_tile_index.png")
json.dump({"tiles": zille, "transform": {"scale": S, "bx": BX, "by": BY}}, open(f"{out}/zille_tile_index.json", "w"))
shutil.copy(brenas, f"{out}/zille_brenas.png"); shutil.copy(ownstate, f"{out}/zille_ownstate.png")
