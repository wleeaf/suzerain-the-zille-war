"""Place the finished map into the Pales paper sheet (margins, frame, transparency) and split into 6 panels.

usage: compose.py pales_rgba.npy map_content.png layout.json out_dir
"""
import json
import sys

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None
pales_p, content_p, layout_p, out = sys.argv[1:5]
layout = json.load(open(layout_p))
x0, y0, x1, y1 = layout["content_box"]
sheet = np.load(pales_p).copy()                          # 8192 x 12288 x 4, the Pales paper
content = Image.open(content_p).convert("RGB")
if content.size != (x1 - x0, y1 - y0):
    content = content.resize((x1 - x0, y1 - y0), Image.LANCZOS)
sheet[y0:y1, x0:x1, :3] = np.asarray(content)
sheet[y0:y1, x0:x1, 3] = 255
for i in range(6):
    px, py = (i % 3) * 4096, (i // 3) * 4096
    Image.fromarray(sheet[py:py + 4096, px:px + 4096], "RGBA").save(f"{out}/map_0{i + 1}.png")
Image.fromarray(sheet).resize((1536, 1024), Image.LANCZOS).save(f"{out}/_sheet_preview.png")
print("composed into", out)
