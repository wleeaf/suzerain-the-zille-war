import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
src, out = sys.argv[1], sys.argv[2]
im = np.asarray(Image.open(src).convert('RGB')).astype(int)
# frame in zille_region_full coords (2400x1600, 3:2)
x0, y0, x1, y1 = 300, 240, 2700, 1840
im = im[y0:y1, x0:x1]
R, G, B = im[..., 0], im[..., 1], im[..., 2]
sea = (B - R > 25) & (B > 185)
# remove thin blue lines (rivers) and thin dark labels in the sea
P = 16                                           # pad with edge values so morphology doesn't invent land at the frame
sea = np.pad(sea, P, mode='edge')
sea = ndi.binary_opening(sea, iterations=4)     # kill rivers on land
sea = ndi.binary_closing(sea, iterations=6)     # fill labels / dotted lines in sea
sea = sea[P:-P, P:-P]
land = ~sea
lab, n = ndi.label(land)
sizes = ndi.sum(land, lab, range(1, n + 1))
keep = np.zeros(n + 1, bool); keep[1:] = sizes > 150  # drop specks, keep small islands
land = keep[lab]
# lakes: sea pixels not connected to the frame border
sl, sn = ndi.label(~land)
border = set(np.unique(np.concatenate([sl[0], sl[-1], sl[:, 0], sl[:, -1]])))
lakes = (~land) & ~np.isin(sl, list(border))
Image.fromarray((land * 255).astype(np.uint8)).save(f"{out}/land_mask.png")
Image.fromarray((lakes * 255).astype(np.uint8)).save(f"{out}/lakes_mask.png")
# guide render in Pales palette: white land, grey-blue sea, black coastline
edge = land ^ ndi.binary_erosion(land, iterations=3)
g = np.zeros(land.shape + (3,), np.uint8)
g[~land] = (185, 198, 200)
g[land] = (255, 255, 255)
g[lakes] = (185, 198, 200)
g[edge] = (20, 20, 20)
Image.fromarray(g).save(f"{out}/guide_coast.png")
print("islands/landmasses:", int((sizes > 150).sum()), "lake px:", int(lakes.sum()))
