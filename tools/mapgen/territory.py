"""Split the playable landmass into Zille (Wehlen-held) and Rizia, from the national map's orange border."""
import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
src, landp, out = sys.argv[1:4]
ZILLE_XY, INSIDE_ZILLE_XY = (674, 1286), (800, 1100)      # frame coords (x, y)
im = np.asarray(Image.open(src).convert('RGB')).astype(int)[240:1840, 300:2700]
R, G, B = im[..., 0], im[..., 1], im[..., 2]
orange = (R > 215) & (G > 110) & (G < 190) & (B < 110)
land = np.asarray(Image.open(landp).convert('L')) > 127
lab, n = ndi.label(land)
main_id = lab[ZILLE_XY[1], ZILLE_XY[0]]
main = lab == main_id
band = ndi.binary_closing(orange, iterations=10) | orange
cut = main & ~ndi.binary_dilation(band, iterations=2)
cl, cn = ndi.label(cut)
sizes = ndi.sum(cut, cl, range(1, cn + 1))
zid = cl[INSIDE_ZILLE_XY[1], INSIDE_ZILLE_XY[0]]
z_core = cl == zid
r_core = (cl == cl[1400, 1402]) & (ndi.distance_transform_edt(~band) > 60)   # Balle side, minus coastal slivers by the band
dz = ndi.distance_transform_edt(~z_core); dr = ndi.distance_transform_edt(~r_core)
zille = main & (dz < dr)
# small islands: belong to Zille if they are closer to Zille than to Rizia and within reach
lsizes = ndi.sum(land, lab, range(1, n + 1))
for i in range(1, n + 1):
    if i == main_id or lsizes[i - 1] > 20000: continue
    comp = lab == i
    if dz[comp].min() < min(dr[comp].min(), 160): zille |= comp
rizia = main & ~zille
print('zille px', int(zille.sum()), 'rizia px', int(rizia.sum()))
v = np.zeros(land.shape + (3,), np.uint8); v[land] = (230, 230, 230); v[rizia] = (120, 170, 90); v[zille] = (240, 140, 60)
Image.fromarray(v).resize((1200, 800)).save(f'{out}/_territory.png')
Image.fromarray((zille * 255).astype(np.uint8)).save(f'{out}/zille_mask.png')
Image.fromarray((rizia * 255).astype(np.uint8)).save(f'{out}/rizia_mask.png')
