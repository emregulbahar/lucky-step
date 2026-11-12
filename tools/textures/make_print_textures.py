"""Lucky Step printed-space textures ("Printed Board" look).

Writes into <repo>/Assets/Art/Generated/:
    PrintSpace.png   white paper face with a printed ink border, inset from the edge
    PrintHazard.png  the same plus a red hazard band across the top (image top = far side of the board)
    TableFelt.png    grey felt for the card table: a soft lamp pool (bright centre, darker edges) and a faint fixed grain
    BoxLogo.png      printed "Lucky Step" logo for the game-box lid prop (look pass 2), full colour (material tint white)

The paper is pure white so the Unity material's colour tints it to the two paper tones (#FFF6E0 / #EDE0C2).
UV contract with tools/blender/make_models.py (INK_UV): the slab top maps 0..1 onto the whole image and the slab
sides sample the centre of the border's bottom run, UV (0.5, 8/256), so that spot must stay solid ink.
Drawn at 4x and box-filtered down for clean anti-aliased edges; no noise.

Run: python -I tools/textures/make_print_textures.py [<output_dir>]
"""

import os
import sys

import math
import random

from PIL import Image, ImageDraw, ImageFilter

SIZE = 256        # output resolution (pixels per side)
SUPER = 4         # supersampling factor
INSET = 5         # paper margin outside the ink border, output pixels
BORDER = 6        # ink border width, output pixels
BAND = 56         # hazard band height below the border, output pixels

PAPER = (255, 255, 255)
INK = (0x1C, 0x1A, 0x24)
HAZARD = (0xE2, 0x3B, 0x3B)


def output_dir():
    if len(sys.argv) > 1:
        return os.path.abspath(sys.argv[1])
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    return os.path.join(root, "Assets", "Art", "Generated")


def draw_space(hazard):
    """Returns the SIZE x SIZE printed space; hazard adds the red band under the top border."""
    s = SUPER
    img = Image.new("RGB", (SIZE * s, SIZE * s), PAPER)
    d = ImageDraw.Draw(img)
    outer, inner = INSET * s, (INSET + BORDER) * s
    last = SIZE * s - 1
    d.rectangle((outer, outer, last - outer, last - outer), fill=INK)
    d.rectangle((inner, inner, last - inner, last - inner), fill=PAPER)
    if hazard:
        d.rectangle((inner, inner, last - inner, inner + BAND * s - 1), fill=HAZARD)
    return img.resize((SIZE, SIZE), Image.Resampling.BOX)


FELT_SIZE = 512   # felt texture resolution
FELT_EDGE = 0.72  # brightness at the corners of the lamp pool (centre = 1.0)
FELT_GRAIN = 0.03 # grain amplitude (fraction of brightness); seeded, so the file is the same every run


def draw_felt():
    """Grey felt; the Unity material colour tints it green. Stretched over the whole table top by TableView."""
    rng = random.Random(7)
    grain = Image.new("L", (FELT_SIZE, FELT_SIZE))
    grain.putdata([rng.randint(0, 255) for _ in range(FELT_SIZE * FELT_SIZE)])
    grain = grain.filter(ImageFilter.GaussianBlur(0.7))
    img = Image.new("RGB", (FELT_SIZE, FELT_SIZE))
    c = (FELT_SIZE - 1) * 0.5
    px = img.load()
    for y in range(FELT_SIZE):
        for x in range(FELT_SIZE):
            r = min(1.0, (((x - c) / c) ** 2 + ((y - c) / c) ** 2) ** 0.5 / 1.4142)
            pool = 1.0 - (1.0 - FELT_EDGE) * (r * r * (3.0 - 2.0 * r))
            v = pool * (1.0 + FELT_GRAIN * (grain.getpixel((x, y)) / 127.5 - 1.0))
            g = max(0, min(255, round(255 * v)))
            px[x, y] = (g, g, g)
    return img


LOGO_SIZE = 512
CREAM = (0xFF, 0xF6, 0xE0)
CARD_RED = (0xA3, 0x26, 0x2E)
GOLD = (0xFF, 0xC2, 0x1A)
FONT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
                    "Assets", "Art", "UI", "Fonts", "Fredoka-Bold.ttf")


def draw_logo():
    """Box-lid logo: red field in a cream and ink frame, "LUCKY" / "STEP" in gold with an ink outline, a gold 7 star."""
    from PIL import ImageFont
    s = 2
    n = LOGO_SIZE * s
    img = Image.new("RGB", (n, n), CREAM)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((18 * s, 18 * s, n - 18 * s, n - 18 * s), radius=36 * s, fill=INK)
    d.rounded_rectangle((30 * s, 30 * s, n - 30 * s, n - 30 * s), radius=28 * s, fill=CARD_RED)
    d.rounded_rectangle((52 * s, 52 * s, n - 52 * s, n - 52 * s), radius=20 * s, outline=CREAM, width=8 * s)
    font = ImageFont.truetype(FONT, 118 * s)
    for text, cy in (("LUCKY", 0.36), ("STEP", 0.60)):
        d.text((n / 2, n * cy), text, font=font, fill=GOLD, anchor="mm", stroke_width=9 * s, stroke_fill=INK)
    cx, cy, r = n / 2, n * 0.80, 34 * s
    star = []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5
        rr = r if k % 2 == 0 else r * 0.45
        star.append((cx + rr * math.cos(a), cy - rr * math.sin(a)))
    d.polygon(star, fill=GOLD, outline=INK, width=4 * s)
    return img.resize((LOGO_SIZE, LOGO_SIZE), Image.Resampling.LANCZOS)


def check_ink_spot(img):
    """The slab sides sample UV (0.5, 8/256): image column SIZE/2, row SIZE-1-8 (rows run top-down)."""
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            px = img.getpixel((SIZE // 2 + dx, SIZE - 1 - 8 + dy))
            if px != INK:
                raise SystemExit("ink spot under INK_UV is not solid ink: " + str(px))


def main():
    out = output_dir()
    os.makedirs(out, exist_ok=True)
    for name, hazard in (("PrintSpace.png", False), ("PrintHazard.png", True)):
        img = draw_space(hazard)
        check_ink_spot(img)
        path = os.path.join(out, name)
        img.save(path, optimize=True)
        print("WROTE", path)
    path = os.path.join(out, "TableFelt.png")
    draw_felt().save(path, optimize=True)
    print("WROTE", path)
    path = os.path.join(out, "BoxLogo.png")
    draw_logo().save(path, optimize=True)
    print("WROTE", path)


main()
