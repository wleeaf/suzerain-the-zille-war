"""Codex terrain (any size, 3:2) + exact land mask -> Pales-style map at 12288x8192.
usage: finalize.py gen.png land_mask.png lakes_mask.png out.png [min_area] [WxH]"""
import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
gen_p, mask_p, lakes_p, out_p = sys.argv[1:5]
MIN_AREA = int(sys.argv[5]) if len(sys.argv) > 5 else 600     # px at the 1/4 working scale
W, H = (int(v) for v in sys.argv[6].split('x')) if len(sys.argv) > 6 else (12288, 8192)
W, H = W - W % 4, H - H % 4
WW, WH = W // 4, H // 4                                        # cleanup working scale
LEVELS = np.array([255, 244, 234, 226, 216, 204], np.uint8)
SHARE = np.array([0.39, 0.22, 0.11, 0.13, 0.08, 0.07])
SEA = np.array([187, 199, 200], np.uint8)

def load_mask(p, w, h, sigma=1.0):
    m = np.asarray(Image.open(p).convert('L')).astype(np.float32) / 255
    m = ndi.gaussian_filter(m, sigma)
    return ndi.zoom(m, (h / m.shape[0], w / m.shape[1]), order=3)[:h, :w] > 0.5

rgb = np.asarray(Image.open(gen_p).convert('RGB').resize((WW, WH), Image.LANCZOS)).astype(np.float32)
g = rgb.mean(-1)
land_w = load_mask(mask_p, WW, WH)
# anything the model painted as water (or near-black lines) inside our land -> treat as white lowland
seaish = (np.abs(rgb - SEA.astype(np.float32)).sum(-1) < 40) | (g < 120)
g[seaish & land_w] = 255
g = ndi.grey_closing(g, size=9)                                # erase thin contour lines
g = ndi.gaussian_filter(g, 4.0)

core = ndi.binary_erosion(land_w, iterations=20)
thr = np.quantile(g[core], 1 - np.cumsum(SHARE)[:-1])
print("thresholds:", np.round(thr, 1))

def clean(m):
    lab, n = ndi.label(m)
    if n:
        sz = ndi.sum(m, lab, range(1, n + 1))
        m = np.isin(lab, np.nonzero(sz >= MIN_AREA)[0] + 1)
    lab, n = ndi.label(~m)
    if n:
        sz = ndi.sum(~m, lab, range(1, n + 1))
        m = m | np.isin(lab, np.nonzero(sz < MIN_AREA)[0] + 1)
    return m

levels = np.zeros((H, W), np.uint8)
prev = None
rng = np.random.default_rng(7)
for t in thr:
    m = clean((g < t) & land_w)
    if prev is not None:
        m &= prev
    prev = m
    f = ndi.gaussian_filter(m.astype(np.float32), 1.5)
    f += ndi.gaussian_filter(rng.standard_normal(f.shape).astype(np.float32), 2.0) * 0.08  # hand-drawn wobble
    levels += (ndi.zoom(f, 4, order=1) > 0.5)
del prev

land = load_mask(mask_p, W, H)
img = np.empty((H, W, 3), np.uint8); img[:] = SEA
img[land] = LEVELS[levels[land]][:, None]
edge = np.zeros((H, W), bool)
edge[:, 1:] |= levels[:, 1:] != levels[:, :-1]
edge[1:, :] |= levels[1:, :] != levels[:-1, :]
img[ndi.binary_dilation(edge, iterations=1) & land] = 150
del edge, levels
padded = np.pad(land, 8, mode='edge')                          # no coastline along the frame
coast = (padded ^ ndi.binary_erosion(padded, iterations=4))[8:-8, 8:-8]
img[coast] = 0
Image.fromarray(img).save(out_p)
print("saved", out_p)
