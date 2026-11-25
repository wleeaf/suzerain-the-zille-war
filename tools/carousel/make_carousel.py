#!/usr/bin/env python3
"""Builds the Reddit/Instagram carousel (1080x1350 slides) from the gameplay screenshots.

Usage: tools/carousel/make_carousel.py [screenshot dir] [out dir]
Defaults: ~/Pictures/ZilleWar-reddit and ~/Pictures/ZilleWar-reddit/carousel.
Fonts (OFL, Google Fonts) are downloaded once into ~/.cache/zillewar-fonts.
"""
import os
import sys
import glob
import urllib.request
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageEnhance

SRC = os.path.expanduser(sys.argv[1] if len(sys.argv) > 1 else "~/Pictures/ZilleWar-reddit")
OUT = os.path.expanduser(sys.argv[2] if len(sys.argv) > 2 else os.path.join(SRC, "carousel"))
FONTS = os.path.expanduser("~/.cache/zillewar-fonts")
GF = "https://github.com/google/fonts/raw/main/ofl/"
FONT_URLS = {
    "Playfair.ttf": GF + "playfairdisplay/PlayfairDisplay%5Bwght%5D.ttf",
    "PlayfairItalic.ttf": GF + "playfairdisplay/PlayfairDisplay-Italic%5Bwght%5D.ttf",
    "Cinzel.ttf": GF + "cinzel/Cinzel%5Bwght%5D.ttf",
}

W, H = 1080, 1350
HEAD = 84                      # chapter band
CAP = 226                      # caption block
IMG_H = H - HEAD - CAP         # 1040: a 1121x1080 crop of a 1920x1080 shot, at about 1:1

MAROON = (52, 18, 22)
MAROON_DEEP = (30, 10, 13)
INK = (20, 15, 12)
GOLD = (246, 224, 156)
PARCHMENT = (226, 216, 192)
DIM_GOLD = (246, 224, 156, 150)


def font(name, size, variation=None):
    path = os.path.join(FONTS, name)
    if not os.path.exists(path):
        os.makedirs(FONTS, exist_ok=True)
        urllib.request.urlretrieve(FONT_URLS[name], path)
    f = ImageFont.truetype(path, size)
    if variation:
        f.set_variation_by_name(variation)
    return f


def shot(prefix):
    return Image.open(glob.glob(os.path.join(SRC, prefix + "*.png"))[0]).convert("RGB")


def spaced(draw, xy, text, fnt, fill, spacing, anchor="l"):
    """Letter-spaced small caps line; anchor l (left), m (centre) or r (right) on x, vertical middle on y."""
    widths = [draw.textlength(ch, font=fnt) for ch in text]
    total = sum(widths) + spacing * (len(text) - 1)
    x, y = xy
    if anchor == "m":
        x -= total / 2
    elif anchor == "r":
        x -= total
    for ch, w in zip(text, widths):
        draw.text((x, y), ch, font=fnt, fill=fill, anchor="lm")
        x += w + spacing
    return total


def wrap(draw, text, fnt, width):
    words, lines, line = text.split(), [], ""
    for w in words:
        trial = (line + " " + w).strip()
        if draw.textlength(trial, font=fnt) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = w
    lines.append(line)
    return lines


def diamond(draw, cx, cy, r, fill):
    draw.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=fill)


def ornament_rule(draw, y, half, fill=GOLD, width=2):
    cx = W // 2
    draw.line([(cx - half, y), (cx - 14, y)], fill=fill, width=width)
    draw.line([(cx + 14, y), (cx + half, y)], fill=fill, width=width)
    diamond(draw, cx, y, 6, fill)


def vertical_gradient(size, top, bottom):
    g = Image.new("RGB", (1, 256))
    for i in range(256):
        t = i / 255
        g.putpixel((0, i), tuple(int(top[k] + (bottom[k] - top[k]) * t) for k in range(3)))
    return g.resize(size)


def crop_for_slide(img, cx=960):
    w = round(1080 * W / IMG_H)              # 1121
    x0 = max(0, min(img.width - w, cx - w // 2))
    return img.crop((x0, 0, x0 + w, 1080)).resize((W, IMG_H), Image.LANCZOS)


def story_slide(img, chapter, caption, n, total, cx=960):
    canvas = Image.new("RGB", (W, H), INK)
    canvas.paste(vertical_gradient((W, HEAD), MAROON, MAROON_DEEP), (0, 0))
    canvas.paste(crop_for_slide(img, cx), (0, HEAD))
    canvas.paste(vertical_gradient((W, CAP), (24, 17, 14), (12, 9, 8)), (0, HEAD + IMG_H))

    d = ImageDraw.Draw(canvas, "RGBA")
    # Band: chapter on the left, slide count on the right, a gold hairline under it.
    spaced(d, (44, HEAD // 2 + 2), chapter, font("Cinzel.ttf", 27, "Bold"), GOLD, 3.2)
    spaced(d, (W - 44, HEAD // 2 + 2), f"{n:02d} / {total:02d}", font("Cinzel.ttf", 22), (226, 216, 192, 170), 2, anchor="r")
    d.line([(0, HEAD - 1), (W, HEAD - 1)], fill=(246, 224, 156, 140), width=2)
    # A soft shadow from the band onto the picture.
    for i in range(14):
        d.line([(0, HEAD + i), (W, HEAD + i)], fill=(0, 0, 0, int(90 * (1 - i / 14))))

    # Caption block: ornament rule, the caption, the mod's mark.
    top = HEAD + IMG_H
    d.line([(0, top), (W, top)], fill=(246, 224, 156, 160), width=2)
    for size in (40, 37, 34):
        f = font("Playfair.ttf", size)
        lines = wrap(d, caption, f, 960)
        if len(lines) <= 2:
            break
    lh = int(size * 1.38)
    block = lh * len(lines)
    y = top + 22 + (150 - block) / 2 + lh / 2
    for line in lines:
        d.text((W / 2, y), line, font=f, fill=PARCHMENT, anchor="mm")
        y += lh
    ornament_rule(d, top + 186, 90, fill=(246, 224, 156, 120), width=1)
    spaced(d, (W / 2, top + 207), "THE ZILLE WAR  ·  WLEEAF EDITION", font("Cinzel.ttf", 16), DIM_GOLD, 3, anchor="m")
    return canvas


def backdrop(img, darken=0.42, blur=14, cx=None):
    scale = H / img.height
    big = img.resize((round(img.width * scale), H), Image.LANCZOS)
    cx = big.width // 2 if cx is None else round(cx * scale)
    x0 = max(0, min(big.width - W, cx - W // 2))
    bg = big.crop((x0, 0, x0 + W, H)).filter(ImageFilter.GaussianBlur(blur))
    bg = ImageEnhance.Brightness(bg).enhance(darken)
    # vignette
    v = Image.new("L", (W, H), 0)
    vd = ImageDraw.Draw(v)
    for i in range(60):
        vd.rectangle([i * 6, i * 7, W - i * 6, H - i * 7], outline=int(170 * (1 - i / 60) ** 2))
    v = v.filter(ImageFilter.GaussianBlur(40))
    return Image.composite(Image.new("RGB", (W, H), (0, 0, 0)), bg, v)


def cover(menu, total):
    canvas = backdrop(menu, darken=0.5, cx=760)
    # The real main menu panel, with the mod's title under the logo, framed in gold.
    panel = menu.crop((1146, 36, 1794, 1076))
    pw = 560
    panel = panel.resize((pw, round(panel.height * pw / panel.width)), Image.LANCZOS)
    px, py = (W - pw) // 2, 64
    d = ImageDraw.Draw(canvas, "RGBA")
    shadow = Image.new("RGBA", (pw + 80, panel.height + 80), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle([40, 40, pw + 40, panel.height + 40], fill=(0, 0, 0, 200))
    shadow = shadow.filter(ImageFilter.GaussianBlur(22))
    canvas.paste(shadow, (px - 40, py - 30), shadow)
    canvas.paste(panel, (px, py))
    d = ImageDraw.Draw(canvas, "RGBA")
    d.rectangle([px - 1, py - 1, px + pw, py + panel.height], outline=(246, 224, 156, 150), width=2)

    # Bottom: the hook.
    for i in range(330):
        d.line([(0, H - 330 + i), (W, H - 330 + i)], fill=(8, 5, 4, int(235 * min(1, i / 120))))
    y = py + panel.height + 72
    spaced(d, (W / 2, y), "A STORY MOD FOR SUZERAIN: KINGDOM OF RIZIA", font("Cinzel.ttf", 24, "Bold"), GOLD, 3.5, anchor="m")
    d.text((W / 2, y + 74), "What if Romus took Zille back by force?", font=font("PlayfairItalic.ttf", 50, "Italic"),
           fill=PARCHMENT, anchor="mm")
    ornament_rule(d, y + 138, 150, fill=(246, 224, 156, 170), width=1)
    spaced(d, (W / 2, y + 176), f"SWIPE  ·  {total - 2} SCREENS FROM ONE WAR", font("Cinzel.ttf", 18), DIM_GOLD, 3, anchor="m")
    return canvas


def closing(mapshot, total):
    canvas = backdrop(mapshot, darken=0.34, blur=10)
    d = ImageDraw.Draw(canvas, "RGBA")
    d.rectangle([70, 90, W - 70, H - 90], fill=(14, 9, 8, 205), outline=(246, 224, 156, 150), width=2)
    d.rectangle([82, 102, W - 82, H - 102], outline=(246, 224, 156, 60), width=1)

    spaced(d, (W / 2, 196), "THE ZILLE WAR", font("Cinzel.ttf", 70, "Black"), GOLD, 10, anchor="m")
    d.text((W / 2, 272), "Wleeaf Edition", font=font("PlayfairItalic.ttf", 38, "Italic"), fill=PARCHMENT, anchor="mm")
    ornament_rule(d, 330, 200, fill=(246, 224, 156, 190), width=1)

    points = [
        "Declare war from the Alliance podium on Turn 4",
        "Three operations, each planned in a War Council",
        "The strait, the garrison and Wehlen's convoys",
        "Every capital, paper and councilor reacts",
        "Ceasefires, a treaty clause by clause",
        "Zille liberated, divided or lost",
        "Over 130,000 words in the game's own voice",
        "Peace games stay vanilla",
    ]
    f = font("Playfair.ttf", 34)
    y = 404
    for p in points:
        diamond(d, 160, y + 2, 7, GOLD)
        d.text((188, y), p, font=f, fill=PARCHMENT, anchor="lm")
        y += 72

    ornament_rule(d, y + 14, 200, fill=(246, 224, 156, 190), width=1)
    spaced(d, (W / 2, y + 66), "ONE ZIP TO INSTALL  ·  ONE CLICK TO REMOVE", font("Cinzel.ttf", 24, "Bold"), GOLD, 3, anchor="m")
    d.text((W / 2, y + 116), "Windows, Steam Deck and Linux. Download link in the post.",
           font=font("Playfair.ttf", 28), fill=PARCHMENT, anchor="mm")
    d.text((W / 2, H - 140), "A fan mod. Not affiliated with Torpor Games.", font=font("PlayfairItalic.ttf", 22, "Italic"),
           fill=(226, 216, 192, 150), anchor="mm")
    spaced(d, (W - 110, H - 110), f"{total:02d} / {total:02d}", font("Cinzel.ttf", 18), (226, 216, 192, 140), 2, anchor="r")
    return canvas


I, II, III, IV = "I  ·  THE DECLARATION", "II  ·  THE WORLD ANSWERS", "III  ·  THE CAMPAIGN", "IV  ·  AFTER THE WAR"
STORY = [
    ("01", I, "Turn 4, the Alliance of Nations. Smolak has spoken. Romus answers from the podium."),
    ("02", I, "Smolak answers. For the first time that day, the grin is gone."),
    ("03", I, "Every delegation reacts. Duke Reinhart has his drillship to think about."),
    ("04", I, "Alma Saltana still wants her answer on the Meftiem, war or no war."),
    ("05", I, "In the car, the Queen Mother calls. “Bring me home, Romus.”"),
    ("06", II, "The Royal Herald, the next morning."),
    ("07", II, "Geopolitico tells it a little differently."),
    ("08", II, "Every choice has a price. Valgsland and the CSP embargo Rizia."),
    ("09", II, "Reports come in from every capital. Wehlen mobilizes."),
    ("10", II, "United Contana condemns “royal aggression”."),
    ("11", II, "The Guild of Royal Allies stands with Rizia."),
    ("12", III, "Three operations on the war map, each one planned in a War Council."),
    ("13", III, "Wehlen supplies Zille across the strait. Cut the line, or face a stronger garrison."),
    ("14", IV, "One ending. Zille can also be divided, or the war lost."),
    ("15", IV, "The war is remembered. The Pales war can still come."),
    ("16", IV, "And Smolak still has to sign. The treaty is read clause by clause."),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in glob.glob(os.path.join(OUT, "*.jpg")):
        os.remove(old)
    total = len(STORY) + 2
    slides = [cover(shot("00"), total)]
    slides += [story_slide(shot(p), ch, cap, i + 2, total) for i, (p, ch, cap) in enumerate(STORY)]
    slides.append(closing(shot("13"), total))
    for i, s in enumerate(slides, 1):
        s.save(os.path.join(OUT, f"{i:02d}.jpg"), quality=93, subsampling=0, optimize=True)
    print(f"{len(slides)} slides in {OUT}")


if __name__ == "__main__":
    main()
