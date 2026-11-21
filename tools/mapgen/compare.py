import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
gen, landmask, out = sys.argv[1:4]
g = Image.open(gen).convert('RGB').resize((1536, 1024))
m = np.asarray(Image.open(landmask).convert('L').resize((1536, 1024))) > 127
edge = m ^ ndi.binary_erosion(m, iterations=2)
o = np.asarray(g).copy(); o[edge] = (230, 30, 30)
# land/sea agreement: classify generated sea by colour closeness to #B9C6C8
a = np.asarray(g).astype(int)
gsea = (np.abs(a - np.array([185, 198, 200])).sum(-1) < 45)
gsea = ndi.binary_opening(gsea, iterations=2)
agree = ((~gsea) == m).mean()
print(f"land/sea agreement: {agree*100:.1f}%  (land in guide {m.mean()*100:.1f}%, generated land {(~gsea).mean()*100:.1f}%)")
W = Image.new('RGB', (1536, 2048 + 10), 'white')
W.paste(g, (0, 0)); W.paste(Image.fromarray(o), (0, 1034))
W.resize((960, 1286)).save(out)
