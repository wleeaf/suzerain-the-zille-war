#!/usr/bin/env python3
"""One poster of all 47 Royal Decrees the mod adds, with what each does and its effects as the decree panel lists them.

Usage: tools/carousel/make_decrees.py [out.png]   (default ~/Pictures/ZilleWar-reddit/decrees.png)
Titles and effects are read from the source (RoyalDecrees.cs, WarDecrees.cs, WarDecreesPostWar.cs); the one-line
summaries are below. Fonts come from make_carousel.py.
"""
import os
import re
import sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from make_carousel import font, backdrop, shot, diamond, spaced, wrap, GOLD, PARCHMENT  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.expanduser(sys.argv[1] if len(sys.argv) > 1 else "~/Pictures/ZilleWar-reddit/decrees.png")

SUMMARY = {
    # every Rizia game
    "Forward Gas Contracts with Lespian Utilities": "Sell future gas to Lespia at today's high prices: cash now, less energy every turn.",
    "Rumburgian Arms on GRACE Credit": "Rumburgian rifles and tanks on cheap Guild credit, paid off over four turns.",
    "Accept the Valgslandian Arms Package": "Cheap surplus arms from Valgsland. Rumburg and the Coalition won't like the company.",
    "Royal Arms Export Controls": "No Rizian arms sold abroad: a better name in the world, less money at home.",
    "Import Duties on Foreign Manufactures": "Tariffs fill the treasury and shelter local workshops; prices rise and partners object.",
    "Repeal the Import Duties": "Lifts the tariffs. Prices ease, and the revenue goes with them.",
    "Energy Conservation Ordinance": "Dark shop windows and quotas on power and gas: energy saved, industry and patience spent.",
    "Lift the Energy Conservation Ordinance": "Ends the quotas and lights the cities again.",
    "Royal Gold Bonds": "Borrow against the Crown's gold: a large sum now, coupons for six turns.",
    "Relax Labor Regulations [RNC Proposal]": "Scraps hour caps and inspections. Employers and the Coalition cheer; the RPP does not.",
    "Royal Tax Inspectorate for the Provinces": "Royal auditors in the ducal tax offices: steady revenue, offended Houses.",
    "Royal Old-Age Pensions [RPP Proposal]": "The first royal pension for workers over 65. Popular, and paid every turn.",
    "Shelters for the Refugees of Pales in Mermi": "Camps for families fleeing Pales, paid while that war lasts. The world notices.",
    "Develop the Montaklar Coast Resorts": "Hotels and a promenade on the Montaklar coast bring tourists; House Azaro takes the credit.",
    "Fund the Rizian Societies of Zille": "Quietly fund Zille's Rizian clubs, schools and papers. Wraclawitz knows who pays.",
    "Prohibit Reformist Assemblies [RNC Proposal]": "Bans reformist rallies. The crowds go, the grievances stay, and Brenas takes it personally.",
    "Royal Crackdown on Su Omina in Iza": "Arrests Su Omina's leaders and ends their attacks. House Toras resents soldiers in Iza.",
    "Repeal the Non-Wruhecist Religious Tax": "Ends the special tax on Dastnurists and Golcondists, at a cost every turn.",
    "Convene the Royal Interfaith Council": "A council of all faiths settles disputes before they reach the streets. The clergy grumbles.",
    # during the war
    "Call Up the Brenas Reserve Divisions [RNC Proposal]": "Brenas reservists refill the ranks; the Crown pays them while they serve.",
    "Stand Down the Brenas Reserves": "Sends the reservists home and ends their pay.",
    "Impose Wartime Censorship": "Every dispatch passes the censors: authority grows, the world frowns.",
    "Lift Wartime Censorship": "The papers may print everything again, including what the war cost.",
    "Requisition the Merchant Fleet": "Freighters carry troops to Zille: one more Marines unit, paid every turn of the war.",
    "Wartime Fuel Rationing": "Fuel coupons for civilians save energy for the front; queues at every pump.",
    "Safe Conduct for Wehzek Deserters": "Gold for every Wehzek soldier who lays down his rifle: a weaker garrison in Zille.",
    "Royal Protection of Wehzek Subjects": "Rizia's Wehzeks stay under the King's protection. Nationalists call it soft.",
    "Internment of Wehzek Nationals": "Camps for Wehzek men of fighting age: more authority, fewer miners, a stain abroad.",
    "Release the Wehzek Internees": "Closes the camps and returns the internees' property.",
    "Pensions for the Veterans of Zille": "Pensions and medals for the soldiers and widows of the campaign.",
    "War Loan of the Zille Campaign": "Bank money now, repaid for five turns after the war.",
    "Seize Wehzek State Assets": "Sequesters Wehlen's accounts and cargoes. Smolak will remember at the talks.",
    "Put the Iza Works on a War Footing": "Iza's arms works supply the front every turn; the town runs short of the rest.",
    "Raise the Zillean Legion": "Zillean volunteers form a new infantry division under the Rizian flag.",
    "Price Controls on Bread and Fuel [RPP Proposal]": "Freezes bread and fuel prices; the Treasury pays the difference.",
    # after the war
    "Civil Administration for Zille City": "Civil servants replace the army in Zille city, and the curfews end.",
    "Rebuild the Harbor of Zille": "Rebuilds the wrecked port, which pays its way once finished.",
    "Loyalty Oath for the Officials of Zille": "Every official swears to the Crown or leaves. Many Wehzeks leave.",
    "Wehzek Schools and Courts in Zille": "Guarantees Wehzek schools and courts. Brenas asks what the war was for.",
    "Release the Wehzek Prisoners of War": "Sends the prisoners home under Alliance escort, and with them the last hostage.",
    "Ransom Our Soldiers": "Pays Wraclawitz to bring the captured Rizian soldiers home.",
    "Court-Martial of the Zille Commanders": "Tries the generals of a failed campaign. House Azaro will not forgive it.",
    "Royal Commission of Inquiry into the Zille Campaign": "An inquiry instead of scapegoats: the King shares the blame, the army learns.",
    "Suspend Reparation Payments to Wehlen": "Stops paying Wehlen after a lost war: money kept, word broken.",
    "Repay the Rumburgian War Credits Early": "Pays off Rumburg's war credits in one sum.",
    "Settle Lespia's Accounts": "Discount gas and an apology to Perla to end the quarrel with Lespia.",
    "Redirect Trade Through the South": "Routes trade around the Valgish embargo through Derdian and Morellan ports.",
}

GOOD = (138, 196, 128)
BAD = (222, 120, 104)
NEUTRAL = (214, 196, 140)


def literal(s, i):
    out = ""
    while True:
        m = re.compile(r'\s*\$?"((?:[^"\\]|\\.)*)"').match(s, i)
        if not m:
            return out.replace("\\n", "\n").replace('\\"', '"'), i
        out += m.group(1)
        i = m.end()
        if re.compile(r'\s*\+\s*\$?"').match(s, i):
            i = re.compile(r"\s*\+").match(s, i).end()
        else:
            return out.replace("\\n", "\n").replace('\\"', '"'), i


def decrees():
    groups = []
    for f in ("RoyalDecrees", "WarDecrees", "WarDecreesPostWar"):
        s = open(os.path.join(ROOT, "ZilleWar", f + ".cs"), encoding="utf-8").read()
        items = []
        for m in re.finditer(r'new\((\w+), "(\w+)", \w+,\s*', s):
            title, i = literal(s, m.end())
            if not title:
                continue
            i = re.compile(r"\s*,\s*").match(s, i).end()
            cost, _ = literal(s, i)
            items.append((m.group(2), title, [c for c in cost.split("\n") if c]))
        groups.append(items)
    return groups


def chip_color(text):
    if "Garrison Strength" in text or text.startswith("Ends the Defeat"):
        return GOOD
    if text.startswith("+"):
        return GOOD
    if text.startswith("-"):
        return BAD
    return NEUTRAL


W, COLW, GUT, PAD = 2400, 740, 40, 70
F_TITLE = font("Playfair.ttf", 29, "Bold")
F_SUM = font("Playfair.ttf", 23)
F_CHIP = font("Playfair.ttf", 19, "SemiBold")
F_CAT = font("Cinzel.ttf", 21, "Bold")
F_SEC = font("Cinzel.ttf", 30, "Black")


def card_height(d, title, summary, chips):
    h = 22 + len(wrap(d, title, F_TITLE, COLW - 40)) * 38
    h += len(wrap(d, summary, F_SUM, COLW - 40)) * 31 + 10
    x, rows = 0, 1
    for c in chips:
        w = d.textlength(c, font=F_CHIP) + 26
        if x + w > COLW - 40:
            rows, x = rows + 1, 0
        x += w + 8
    return h + rows * 38 + 18


def draw_card(d, x, y, title, summary, chips):
    h = card_height(d, title, summary, chips)
    d.rounded_rectangle([x, y, x + COLW, y + h], 10, fill=(22, 16, 13, 225), outline=(246, 224, 156, 70), width=1)
    cy = y + 18
    for line in wrap(d, title, F_TITLE, COLW - 40):
        d.text((x + 20, cy), line, font=F_TITLE, fill=GOLD)
        cy += 38
    for line in wrap(d, summary, F_SUM, COLW - 40):
        d.text((x + 20, cy), line, font=F_SUM, fill=PARCHMENT)
        cy += 31
    cy += 10
    cx = x + 20
    for c in chips:
        w = d.textlength(c, font=F_CHIP) + 26
        if cx + w > x + COLW - 20:
            cx, cy = x + 20, cy + 38
        col = chip_color(c)
        d.rounded_rectangle([cx, cy, cx + w, cy + 30], 15, fill=col + (40,), outline=col + (170,), width=1)
        d.text((cx + 13, cy + 15), c, font=F_CHIP, fill=col, anchor="lm")
        cx += w + 8
    return h


def main():
    groups = decrees()
    missing = [t for g in groups for _, t, _ in g if t not in SUMMARY]
    assert not missing, missing
    sections = [("IN EVERY RIZIA GAME", "war or peace, Turns 2 to 10", groups[0]),
                ("DURING THE WAR", "after the declaration on Turn 4", groups[1]),
                ("AFTER THE WAR", "depending on how it ended", groups[2])]

    probe = ImageDraw.Draw(Image.new("RGB", (10, 10)))
    # One flowing sequence of blocks (section heads, category heads, cards), grouped by category inside each section,
    # poured into three columns of about equal height. A heading always travels with the card under it.
    blocks = []
    for name, sub, items in sections:
        order = []
        for c, _, _ in items:
            if c not in order:
                order.append(c)
        first = True
        for c in order:
            group = [(t, chips) for cc, t, chips in items if cc == c]
            for j, (t, chips) in enumerate(group):
                lead = []
                if first:
                    lead.append(("section", (name, sub, len(items)), 124))
                    first = False
                if j == 0:
                    lead.append(("cat", c, 48))
                h = card_height(probe, t, SUMMARY[t], chips) + 14
                blocks.append((lead, (t, chips, c), h + sum(x[2] for x in lead)))
    total = sum(b[2] for b in blocks)
    target = total / 3
    cols, cur, acc = [[]], 0, 0
    for blk in blocks:
        if acc + blk[2] / 2 > target and len(cols) < 3:
            cols.append([])
            acc = 0
        cols[-1].append(blk)
        acc += blk[2]
    top = 330
    H = top + 20 + max(sum(b[2] for b in col) + (48 if not col[0][0] else 0) for col in cols) + 190

    canvas = backdrop(shot("13"), darken=0.3, blur=12).resize((W, H))
    d = ImageDraw.Draw(canvas, "RGBA")
    d.rectangle([0, 0, W, H], fill=(10, 7, 6, 120))

    spaced(d, (W / 2, 110), "THE ZILLE WAR", font("Cinzel.ttf", 84, "Black"), GOLD, 12, anchor="m")
    d.text((W / 2, 200), "Royal Decrees", font=font("PlayfairItalic.ttf", 54, "Italic"), fill=PARCHMENT, anchor="mm")
    d.text((W / 2, 268), f"{sum(len(g) for g in groups)} new decrees in the Kingdom of Rizia's decree panel, with the effects the panel shows",
           font=font("Playfair.ttf", 28), fill=(226, 216, 192, 210), anchor="mm")

    for k, col in enumerate(cols):
        x = PAD + k * (COLW + GUT)
        y = top + 20
        for n_blk, (lead, (t, chips, c), _) in enumerate(col):
            if n_blk == 0 and not lead:
                lead = [("cat", c + "  (continued)", 48)]
            for kind, val, hh in lead:
                if kind == "section":
                    name, sub, n = val
                    d.line([(x, y + 4), (x + COLW, y + 4)], fill=GOLD + (160,), width=2)
                    spaced(d, (x, y + 40), f"{name}  ·  {n}", F_SEC, GOLD, 3)
                    d.text((x, y + 84), sub, font=font("PlayfairItalic.ttf", 24, "Italic"), fill=(226, 216, 192, 190), anchor="lm")
                else:
                    diamond(d, x + 7, y + 22, 6, GOLD)
                    spaced(d, (x + 24, y + 22), val.upper(), F_CAT, (246, 224, 156, 200), 3)
                y += hh
            y += draw_card(d, x, y, t, SUMMARY[t], chips) + 14

    fy = H - 130
    d.line([(PAD, fy), (W - PAD, fy)], fill=GOLD + (120,), width=1)
    legend = [(GOOD, "gain"), (BAD, "cost"), (NEUTRAL, "other")]
    lx = PAD
    for col, label in legend:
        d.rounded_rectangle([lx, fy + 32, lx + 40, fy + 58], 13, fill=col + (40,), outline=col + (170,))
        d.text((lx + 52, fy + 45), label, font=F_SUM, fill=PARCHMENT, anchor="lm")
        lx += 160
    d.text((lx + 30, fy + 45), "Most decrees also move House standing, councilors' approval, perception abroad and the news. "
           "[RNC] and [RPP] mark party proposals.", font=font("PlayfairItalic.ttf", 23, "Italic"), fill=(226, 216, 192, 200), anchor="lm")
    d.text((PAD, fy + 95), "Suzerain: The Zille War (Wleeaf Edition)  ·  github.com/wleeaf/suzerain-the-zille-war  ·  A fan mod, not affiliated with Torpor Games.",
           font=font("Playfair.ttf", 22), fill=(226, 216, 192, 160), anchor="lm")

    canvas.save(OUT, optimize=True)
    print(OUT, canvas.size)


if __name__ == "__main__":
    main()
