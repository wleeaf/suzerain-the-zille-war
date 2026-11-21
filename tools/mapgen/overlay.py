"""Draw rivers, towns and labels onto the map content in the Pales war-map style.

usage: overlay.py content.png rivers.png zille_tiles.json layout.json font_dir out.png
- content.png: finished terrain/coast map (content-box size)
- rivers.png: river mask in cropped frame space
- zille_tiles.json: export with "towns" (full-frame x, y, role, side)
"""
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
from skimage.morphology import skeletonize

Image.MAX_IMAGE_PIXELS = None
content_p, rivers_p, tiles_p, layout_p, font_dir, out_p = sys.argv[1:7]
layout = json.load(open(layout_p))
fx0, fy0, fx1, fy1 = layout["frame_crop"]
img = Image.open(content_p).convert("RGB")
CW, CH = img.size
sx, sy = CW / (fx1 - fx0), CH / (fy1 - fy0)          # content px per frame px

RIVER = (64, 108, 130)
URBAN = (148, 148, 148)
URBAN_EDGE = (110, 110, 110)
INK = (12, 12, 12)


def to_px(x, y):
    return (x - fx0) * sx, (y - fy0) * sy


# --- rivers: skeleton of the frame-space mask, redrawn as smooth thin lines ---
riv = np.asarray(Image.open(rivers_p).convert("L")) > 127
sk = skeletonize(riv)
ys, xs = np.nonzero(sk)
canvas = np.zeros((CH, CW), np.uint8)
d = ImageDraw.Draw(Image.fromarray(canvas))
river_img = Image.new("L", (CW, CH), 0)
dr = ImageDraw.Draw(river_img)
R = 4.0
for x, y in zip(xs, ys):
    px, py = to_px(x + 0.5 + fx0, y + 0.5 + fy0)
    dr.ellipse([px - R, py - R, px + R, py + R], fill=255)
# join skeleton neighbours with line segments so the river is continuous at 4.9x
pts = set(zip(xs.tolist(), ys.tolist()))
for x, y in pts:
    for dx, dy in ((1, 0), (0, 1), (1, 1), (1, -1)):
        if (x + dx, y + dy) in pts:
            a = to_px(x + 0.5 + fx0, y + 0.5 + fy0)
            b = to_px(x + dx + 0.5 + fx0, y + dy + 0.5 + fy0)
            dr.line([a, b], fill=255, width=int(2 * R))
river_mask = np.asarray(river_img.filter(__import__("PIL.ImageFilter", fromlist=["GaussianBlur"]).GaussianBlur(1.2))) > 110
arr = np.asarray(img).copy()
arr[river_mask] = RIVER
img = Image.fromarray(arr)
draw = ImageDraw.Draw(img)

# --- towns ---
def serif(size, weight):
    f = ImageFont.truetype(f"{font_dir}/NotoSerif-Bold.ttf", size)      # variable font (wght axis)
    try:
        f.set_variation_by_axes([weight])
    except Exception:
        pass
    return f


font_bold = serif(118, 700)
font_capital = serif(150, 700)
font_town = serif(88, 500)
rng = np.random.default_rng(7)

ITALIC = f"{font_dir}/NotoSerif-Italic.ttf"


def italic(size, weight=450):
    f = ImageFont.truetype(ITALIC, size)
    try:
        f.set_variation_by_axes([weight])
    except Exception:
        pass
    return f


def spaced(text, spacing):
    return (" " * spacing).join(text)


def text_size(font, text):
    l, t, r, b = draw.textbbox((0, 0), text, font=font)
    return r - l, b - t


def label(text, x, y, font, fill, angle=0, anchor="mm"):
    """Draw text centred at (x, y) content px, optionally rotated (degrees, counter-clockwise)."""
    if angle == 0:
        draw.text((x, y), text, font=font, fill=fill, anchor=anchor)
        return
    w, h = text_size(font, text)
    layer = Image.new("RGBA", (int(w * 1.3) + 40, int(h * 2.2) + 40), (0, 0, 0, 0))
    ImageDraw.Draw(layer).text((layer.width / 2, layer.height / 2), text, font=font, fill=fill + (255,), anchor="mm")
    layer = layer.rotate(angle, resample=Image.BICUBIC, expand=True)
    img.paste(layer, (int(x - layer.width / 2), int(y - layer.height / 2)), layer)


# Town names sit centred above their marker (Pales style); a few go below where the coast is in the way.
BELOW = {"Isor", "Lusande", "Fort Moridia", "Jardim", "Sudiseri Maraton"}
towns = json.load(open(tiles_p))["towns"]
for t in towns:
    px, py = to_px(t["x"], t["y"])
    if t["role"] == "city":
        r = 105 if t.get("capital") else 72
        ang = np.linspace(0, 2 * np.pi, 40, endpoint=False)
        rad = r * (1 + 0.22 * np.sin(3 * ang + rng.uniform(0, 6)) + 0.12 * np.sin(5 * ang + rng.uniform(0, 6)))
        poly = [(px + np.cos(a) * q * 1.25, py + np.sin(a) * q * 0.85) for a, q in zip(ang, rad)]
        draw.polygon(poly, fill=URBAN, outline=URBAN_EDGE)
        draw.ellipse([px - 20, py - 20, px + 20, py + 20], fill=(52, 52, 52))
        text, font, gap = t["name"].upper(), (font_capital if t.get("capital") else font_bold), (r * 0.85 + 125)
    else:
        draw.ellipse([px - 19, py - 19, px + 19, py + 19], fill=(45, 45, 45))
        text, font, gap = t["name"], font_town, 85
    dy = gap if t["name"] in BELOW else -gap
    label(text, px, py + dy, font, INK)

# Territories, seas and islands (full-frame coords from the national map; angle in degrees)
REGION = (95, 95, 95)
SEA_INK = (78, 104, 116)
f_region, f_region_small = serif(210, 600), serif(150, 600)
f_sea, f_island = italic(118, 450), italic(80, 450)
for name, (x, y, ang, font, fill, sp) in {
    "ZILLE": (705, 905, 0, f_region, REGION, 3),
    "BRENAS": (1760, 1290, 0, f_region, REGION, 3),
    "WEHLEN": (175, 470, 0, f_region_small, REGION, 2),
    "LESPIA": (1980, 455, 0, f_region_small, REGION, 2),
    "GULF OF MORIDIA": (205, 1215, 0, f_sea, SEA_INK, 1),
    "SUDIS SEA": (1640, 735, -27, f_sea, SEA_INK, 2),
    "STRAIT OF KRELECK": (395, 560, 62, f_sea, SEA_INK, 1),
    "Manes": (931, 400, 0, f_island, SEA_INK, 0),
    "Riuz": (1080, 470, 0, f_island, SEA_INK, 0),
    "Sarke": (1360, 575, 0, f_island, SEA_INK, 0),
    "Wals": (311, 735, 0, f_island, SEA_INK, 0),
    "Rotsla": (236, 860, 0, f_island, SEA_INK, 0),
}.items():
    px, py = to_px(x, y)
    label(spaced(name, sp), px, py, font, fill, angle=ang)

img.save(out_p)
print("overlay:", len(towns), "towns,", int(river_mask.sum()), "river px ->", out_p)
