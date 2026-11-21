"""Generate Zille war-map tiles that follow the geography.

Land is split per territory (Zille / Rizia) with a compact watershed over a terrain cost surface
(elevation-band edges + rivers), so tile borders run along ridges, valleys and rivers while tiles
stay compact. Towns get their own tiles; a small fort tile sits at FORT_XY. Near-coast sea is tiled too.

usage: tiles.py <work_dir> <terrain.png> <out_dir> <frame_y_offset> <towns.json>
work_dir: land_mask.png, zille_mask.png, rizia_mask.png, rivers.png (all the same size).
"""
import json
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi
from scipy.spatial import cKDTree
from skimage.segmentation import watershed

work, terrain_p, out = sys.argv[1:4]
Y_OFFSET = int(sys.argv[4]) if len(sys.argv) > 4 else 0
K = 2
FW, FH = Image.open(f"{work}/land_mask.png").size
W, H = FW * K, FH * K
rng = np.random.default_rng(1950)

TOWNS_JSON = json.load(open(sys.argv[5])) if len(sys.argv) > 5 else []
ROLE_DEFENCE = {"city": "City", "town": "KeyLocation"}
TOWNS = {t["name"]: (t["x"], t["y"], t["side"], ROLE_DEFENCE[t["role"]])
         for t in TOWNS_JSON if t["role"] in ROLE_DEFENCE}
TOWNS = {k: (x, y - Y_OFFSET, r, d) for k, (x, y, r, d) in TOWNS.items()}
LAND_TILE_AREA = 21000 * K * K
SEA_TILE_AREA = 23000 * K * K      # same scale as land: ships move a few tiles per turn, not across the map
SEA_REACH = 210 * K
FORT_AREA = 5500 * K * K          # deliberately small
COMPACTNESS = 1.8                 # cost per px of distance from the seed; tiles ~r 150px, rivers cost ~900


def load(name, mode=Image.NEAREST):
    return np.asarray(Image.open(f"{work}/{name}").convert("L").resize((W, H), mode)) > 127


land = load("land_mask.png")
zille = load("zille_mask.png")
rizia = load("rizia_mask.png")
rivers = load("rivers.png", Image.BILINEAR)
playable = zille | rizia
sea = ~land
sea_play = sea & (ndi.distance_transform_edt(~playable) < SEA_REACH)
sl, sn = ndi.label(sea_play)
sea_play = sl == (np.argmax(ndi.sum(sea_play, sl, range(1, sn + 1))) + 1)

# --- terrain cost surface: band edges (contours/ridges) + rivers ---
terrain = np.asarray(Image.open(terrain_p).convert("L").resize((W, H), Image.BILINEAR)).astype(np.float32)
terrain = ndi.gaussian_filter(terrain, 3 * K)          # terrain_p = levels.png: elevation bands, lines removed
grad = ndi.gaussian_gradient_magnitude(terrain, 3.0 * K)
grad = np.clip(grad / (np.percentile(grad[land], 98) + 1e-6), 0, 1)
riv = ndi.gaussian_filter(rivers.astype(np.float32), 2 * K)
riv = np.clip(riv / (riv.max() + 1e-6) * 2.5, 0, 1)
elev = (grad * 250 + riv * 900).astype(np.float32)

# --- fort: small tile at the bottom-left of Rizian land, a little inland and back from the border ---
d_sea = ndi.distance_transform_edt(land)
d_zil = ndi.distance_transform_edt(~zille)
cand = rizia & (d_sea > 22 * K) & (d_zil > 18 * K)
ys, xs = np.nonzero(cand)
i = np.argmin(xs - 1.0 * ys)
FORT_XY = (int(xs[i]), int(ys[i]))
fort_mask = np.zeros((H, W), bool)        # the fort is a normal-sized tile: marked after segmentation


def poisson(mask, area, fixed):
    r = np.sqrt(area / np.pi) * 1.55
    ys, xs = np.nonzero(mask)
    pts = [np.array(p, float) for p in fixed]
    for i in rng.permutation(len(xs))[:300000]:
        p = np.array([xs[i], ys[i]], float)
        if pts and cKDTree(pts).query(p)[0] < r:
            continue
        pts.append(p)
    return np.array(pts)


def relax(mask, seeds, n_fixed, iters=5):
    ys, xs = np.nonzero(mask)
    q = np.stack([xs, ys], 1)
    for _ in range(iters):
        _, idx = cKDTree(seeds).query(q)
        cnt = np.bincount(idx, minlength=len(seeds))
        ok = cnt > 0
        new = seeds.copy()
        new[ok, 0] = np.bincount(idx, weights=xs, minlength=len(seeds))[ok] / cnt[ok]
        new[ok, 1] = np.bincount(idx, weights=ys, minlength=len(seeds))[ok] / cnt[ok]
        new[:n_fixed] = seeds[:n_fixed]
        # keep seeds inside the mask
        snap = cKDTree(q).query(new)[1]
        seeds = q[snap].astype(float)
    return seeds


def segment(mask, seeds):
    markers = np.zeros((H, W), np.int32)
    for k, (x, y) in enumerate(seeds.astype(int), 1):
        markers[max(0, min(H - 1, y)), max(0, min(W - 1, x))] = k
    markers = ndi.grey_dilation(markers, size=5) * mask
    return watershed(elev, markers, mask=mask, compactness=COMPACTNESS)


def clean(lab):
    out = lab.copy()
    for l in np.unique(lab):
        if l == 0:
            continue
        m = lab == l
        cl, cn = ndi.label(m)
        if cn <= 1:
            continue
        keep = np.argmax(ndi.sum(m, cl, range(1, cn + 1))) + 1
        for c in range(1, cn + 1):
            if c == keep:
                continue
            frag = cl == c
            nb = out[ndi.binary_dilation(frag, iterations=2) & ~frag]
            nb = nb[(nb != 0) & (nb != l)]
            out[frag] = np.bincount(nb).argmax() if len(nb) else l
    return out


labels = np.zeros((H, W), np.int32)
meta = []
nid = 0
for region, mask in (("zille", zille), ("rizia", rizia & ~fort_mask)):
    fixed = [(x * K, y * K) for (x, y, r, _) in TOWNS.values() if r == region]
    seeds = relax(mask, poisson(mask, LAND_TILE_AREA, fixed), len(fixed))
    lab = clean(segment(mask, seeds))
    for l in np.unique(lab):
        if l:
            nid += 1
            labels[lab == l] = nid
            meta.append({"id": nid, "region": region, "type": "Land"})
fort_label = labels[FORT_XY[1], FORT_XY[0]]
for t in meta:
    if t["id"] == fort_label:
        t["fort"] = True

seeds = relax(sea_play, poisson(sea_play, SEA_TILE_AREA, []), 0, iters=4)
lab = clean(segment(sea_play, seeds))
for l in np.unique(lab):
    if l:
        nid += 1
        labels[lab == l] = nid
        meta.append({"id": nid, "region": "sea", "type": "Sea"})

# merge slivers into a same-region neighbour (never the fort)
for t in list(meta):
    if t.get("fort"):
        continue
    m = labels == t["id"]
    target = SEA_TILE_AREA if t["type"] == "Sea" else LAND_TILE_AREA
    if m.sum() < 0.3 * target:
        nb = labels[ndi.binary_dilation(m, iterations=2) & ~m]
        ok = [n for n in nb if n and not next(x for x in meta if x["id"] == n).get("fort")
              and next(x for x in meta if x["id"] == n)["region"] == t["region"]]
        if ok:
            labels[m] = np.bincount(ok).argmax()
            meta.remove(t)

# keep town tiles (Zille side first) at the front so they get low numbers
town_at = {}
for name, (x, y, r, kind) in TOWNS.items():
    l = labels[y * K, x * K]
    if l:
        town_at[l] = (name, kind)
land_meta = sorted([t for t in meta if t["type"] == "Land"],
                   key=lambda t: (t["region"] != "zille", t["id"] not in town_at, t["id"]))
sea_meta = [t for t in meta if t["type"] == "Sea"]
for i, t in enumerate(land_meta, 1):
    t["name"] = str(i)
for i, t in enumerate(sea_meta, 1):
    t["name"] = f"S{i}"
by_id = {t["id"]: t for t in meta}

contact = {}
for a, b in ((labels[:, 1:], labels[:, :-1]), (labels[1:, :], labels[:-1, :])):
    m = (a != b) & (a > 0) & (b > 0)
    keys = np.minimum(a[m], b[m]).astype(np.int64) * 100000 + np.maximum(a[m], b[m])
    u, c = np.unique(keys, return_counts=True)
    for k, n in zip(u, c):
        contact[int(k)] = contact.get(int(k), 0) + int(n)
for t in meta:
    t["neighbors"] = []
for k, n in contact.items():
    a, b = divmod(k, 100000)
    if n >= 3 * K and a in by_id and b in by_id:          # any real shared edge (corner touches have 0 contact)
        by_id[a]["neighbors"].append(by_id[b]["name"])
        by_id[b]["neighbors"].append(by_id[a]["name"])

coast_sea = ndi.binary_dilation(sea, iterations=3 * K)
near_z = ndi.binary_dilation(zille, iterations=3 * K)
near_r = ndi.binary_dilation(rizia, iterations=3 * K)
objs = ndi.find_objects(labels)
for t in meta:
    sl_ = objs[t["id"] - 1]
    m = labels[sl_] == t["id"]
    ys, xs = np.nonzero(m)
    ys = ys + sl_[0].start
    xs = xs + sl_[1].start
    t["centroid"] = [float(xs.mean() / K), float(ys.mean() / K)]
    t["bbox"] = [int(xs.min() / K), int(ys.min() / K), int(xs.max() / K + 1), int(ys.max() / K + 1)]
    t["area"] = int(m.sum() / K / K)
    land_t = t["type"] == "Land"
    t["shore"] = bool(land_t and coast_sea[ys, xs].any())
    t["border"] = bool(land_t and ((t["region"] == "rizia" and near_z[ys, xs].any()) or
                                   (t["region"] == "zille" and near_r[ys, xs].any())))
    t["mountain"] = bool(land_t and np.mean(terrain[ys, xs] < 175) > 0.45)     # levels.png: 255 - 40*level
    t["fort"] = bool(t.get("fort", False))
    if t["id"] in town_at:
        t["town"], t["defence"] = town_at[t["id"]]
    if t["fort"]:
        t["town"], t["defence"] = "Fort Moridia", "Fort"
        t["fort_xy"] = [FORT_XY[0] / K, FORT_XY[1] / K + Y_OFFSET]
    t["neighbors"] = sorted(set(t["neighbors"]), key=lambda s: (s[0] == "S", int(s.lstrip("S"))))
    del t["id"]

np.save(f"{out}/labels.npy", labels)
json.dump({"frame_size": [FW, FH], "scale": K, "tiles": land_meta + sea_meta}, open(f"{out}/tiles.json", "w"), indent=1)

# preview: tiles over a light map with rivers, towns and the fort marked
rgb = np.zeros((H, W, 3), np.uint8)
rgb[land] = (228, 228, 228)
rgb[sea] = (187, 199, 200)
pal = rng.integers(70, 230, (labels.max() + 1, 3)).astype(np.uint8)
tp = labels > 0
rgb[tp] = (0.6 * rgb[tp] + 0.4 * pal[labels[tp]]).astype(np.uint8)
rgb[rivers & land] = (40, 90, 200)
edge = np.zeros((H, W), bool)
edge[:, 1:] |= labels[:, 1:] != labels[:, :-1]
edge[1:, :] |= labels[1:, :] != labels[:-1, :]
rgb[ndi.binary_dilation(edge) & ndi.binary_dilation(tp)] = (35, 35, 35)
zb = ndi.binary_dilation(zille, iterations=2) & ~zille & rizia
rgb[ndi.binary_dilation(zb, iterations=2)] = (200, 40, 40)
img = Image.fromarray(rgb)
from PIL import ImageDraw
dr = ImageDraw.Draw(img)
for name, (x, y, r, kind) in TOWNS.items():
    dr.ellipse([x * K - 9, y * K - 9, x * K + 9, y * K + 9], fill=(0, 0, 0))
fx, fy = FORT_XY
dr.rectangle([fx - 12, fy - 12, fx + 12, fy + 12], outline=(160, 0, 0), width=5)
img.resize((1600, int(1600 * H / W)), Image.LANCZOS).save(f"{out}/_tiles_preview.png")

cnt = {r: sum(1 for t in meta if t["region"] == r) for r in ("zille", "rizia", "sea")}
deg = [len(t["neighbors"]) for t in meta]
print("tiles:", cnt, "total", len(meta), "| neighbours min/mean/max", min(deg), round(float(np.mean(deg)), 1), max(deg))
print("towns:", {t["town"]: t["name"] for t in meta if "town" in t}, "| fort:", [t["name"] for t in meta if t["fort"]],
      "at frame", (FORT_XY[0] // K, FORT_XY[1] // K + Y_OFFSET))
print("mountain:", sum(t["mountain"] for t in meta), "shore:", sum(t["shore"] for t in meta), "border:", sum(t["border"] for t in meta))
