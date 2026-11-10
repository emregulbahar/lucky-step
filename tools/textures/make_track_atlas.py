"""Lucky Step score-track number atlas ("Printed Board" look, Story 005).

Writes <repo>/Assets/Art/Generated/TrackAtlas.png in the layout Assets/Scripts/Presentation/TrackAtlas.cs reads:
1024 x 2048 px, one 128 px cell per number 0..31 (8 per row), four blocks of 4 rows, one block per face, block 0 at
the BOTTOM of the image (Unity UVs start bottom-left): Plain, Deadly, Lucky, Start.

    Plain   ink number on white (the track material tints the white to paper #FFF6E0)
    Deadly  cream number and skull on ink
    Lucky   ink number and a cream star on gold (colours baked; the lucky material is untinted)
    Start   ink number and the word START on white

Numbers and START are Fredoka Bold (Assets/Art/UI/Fonts, SIL OFL), the same face as the UI. Star and skull are
drawn shapes with rounded corners to match it. Laid out in 64 px design cells, drawn at 8x and box-filtered down to
OUT_SCALE x the design size (128 px cells) so the numerals stay sharp on the tilted track.

Run: python -I tools/textures/make_track_atlas.py [<output.png>]
"""

import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

WIDTH, HEIGHT = 512, 1024   # design size; output is OUT_SCALE x this = TrackAtlas.Width / TrackAtlas.Height
CELL = 64                   # design pixels per cell
OUT_SCALE = 2               # output pixels per design pixel (TrackAtlas cell = 128 px)
COLUMNS = WIDTH // CELL
ROWS_PER_BLOCK = 4
MAX_NUMBER = 31             # must match TrackAtlas.MaxNumber
SUPER = 8                   # drawing pixels per design pixel (supersampling: 4x the output)

PAPER = (255, 255, 255)
INK = (0x1C, 0x1A, 0x24)
GOLD = (0xFF, 0xC2, 0x1A)
CREAM = (0xFF, 0xF6, 0xE0)

PLAIN, DEADLY, LUCKY, START = range(4)

# Glyph layout inside a 64 px design cell, design pixels (y down from the cell top). Number heights follow from the font
# size: Fredoka Bold digits are ~0.73 em tall, and "28" (the widest number) is ~1.11 em wide.
PLAIN_NUMBER_SIZE = 46      # em px: "28" ~51 px wide, digits ~34 px tall
SMALL_NUMBER_SIZE = 30      # deadly / lucky number above the icon, digits ~22 px tall
SMALL_NUMBER_Y = 17
START_NUMBER_SIZE = 34
START_NUMBER_Y = 24
START_WORD_SIZE = 16        # "START" ~50 px wide
START_WORD_Y = 48
ICON_Y = 44                 # centre of the skull / star
STAR_RADIUS = 14.5
STAR_INNER = 0.48           # inner radius / outer radius
ROUNDING = 1.1              # corner rounding radius of the drawn shapes, design pixels


def repo_root():
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def output_path():
    if len(sys.argv) > 1:
        return os.path.abspath(sys.argv[1])
    return os.path.join(repo_root(), "Assets", "Art", "Generated", "TrackAtlas.png")


def load_font(size):
    path = os.path.join(repo_root(), "Assets", "Art", "UI", "Fonts", "Fredoka-Bold.ttf")
    return ImageFont.truetype(path, size * SUPER)


def cell_origin(n, face):
    """Top-left of the cell in design pixels (image rows run top-down, atlas rows bottom-up)."""
    col = n % COLUMNS
    row = n // COLUMNS + face * ROWS_PER_BLOCK
    return col * CELL, HEIGHT - (row + 1) * CELL


def draw_text(d, text, font, cx, cy, colour):
    """Draws text with its ink bounding box centred on (cx, cy), design pixels."""
    l, t, r, b = d.textbbox((0, 0), text, font=font)
    d.text((cx * SUPER - (l + r) / 2, cy * SUPER - (t + b) / 2), text, font=font, fill=colour)


def rounded(mask):
    """Rounds the corners of a supersampled 1-bit-ish mask: blur, then threshold at half."""
    blurred = mask.filter(ImageFilter.GaussianBlur(ROUNDING * SUPER))
    return blurred.point(lambda v: 255 if v >= 128 else 0)


def stamp(img, cx, cy, half, colour, draw_shape):
    """Draws a rounded shape into img: draw_shape(draw, ox, oy) paints a local mask whose centre is (ox, oy)."""
    size = int(2 * half * SUPER)
    mask = Image.new("L", (size, size), 0)
    draw_shape(ImageDraw.Draw(mask), size / 2, size / 2)
    mask = rounded(mask)
    img.paste(colour, (int(cx * SUPER - size / 2), int(cy * SUPER - size / 2)), mask)


def star_shape(d, ox, oy):
    import math
    points = []
    for k in range(10):
        r = STAR_RADIUS * (1.0 if k % 2 == 0 else STAR_INNER) * SUPER
        a = math.radians(-90 + 36 * k)
        points.append((ox + r * math.cos(a), oy + r * math.sin(a) + 1.2 * SUPER))  # optical centre: star sits low
    d.polygon(points, fill=255)


def skull_shape(d, ox, oy):
    s = SUPER
    d.ellipse((ox - 10 * s, oy - 11 * s, ox + 10 * s, oy + 6 * s), fill=255)          # cranium
    d.rectangle((ox - 6 * s, oy + 1 * s, ox + 6 * s, oy + 10 * s), fill=255)          # jaw


def skull_holes(d, cx, cy):
    """Eye sockets, nose and teeth gaps cut in ink (drawn after the rounded silhouette)."""
    s = SUPER
    x, y = cx * s, cy * s
    for side in (-1, 1):
        ex = x + side * 4.6 * s
        d.ellipse((ex - 3.4 * s, y - 4.6 * s, ex + 3.4 * s, y + 2.0 * s), fill=INK)
    d.polygon(((x - 1.6 * s, y + 4.6 * s), (x + 1.6 * s, y + 4.6 * s), (x, y + 2.4 * s)), fill=INK)
    for tx in (-2.4, 2.4):
        d.rounded_rectangle((x + (tx - 0.7) * s, y + 7.0 * s, x + (tx + 0.7) * s, y + 10.6 * s), 0.6 * s, fill=INK)


def main():
    img = Image.new("RGB", (WIDTH * SUPER, HEIGHT * SUPER), PAPER)
    d = ImageDraw.Draw(img)
    plain_font = load_font(PLAIN_NUMBER_SIZE)
    small_font = load_font(SMALL_NUMBER_SIZE)
    start_font = load_font(START_NUMBER_SIZE)
    word_font = load_font(START_WORD_SIZE)
    background = {PLAIN: PAPER, DEADLY: INK, LUCKY: GOLD, START: PAPER}

    for n in range(MAX_NUMBER + 1):
        for face in (PLAIN, DEADLY, LUCKY, START):
            x0, y0 = cell_origin(n, face)
            d.rectangle((x0 * SUPER, y0 * SUPER, (x0 + CELL) * SUPER - 1, (y0 + CELL) * SUPER - 1), fill=background[face])
            cx = x0 + CELL / 2
            text = str(n)
            if face == PLAIN:
                draw_text(d, text, plain_font, cx, y0 + CELL / 2, INK)
            elif face == DEADLY:
                draw_text(d, text, small_font, cx, y0 + SMALL_NUMBER_Y, CREAM)
                stamp(img, cx, y0 + ICON_Y, 14, CREAM, skull_shape)
                skull_holes(d, cx, y0 + ICON_Y)
            elif face == LUCKY:
                draw_text(d, text, small_font, cx, y0 + SMALL_NUMBER_Y, INK)
                stamp(img, cx, y0 + ICON_Y, 17, CREAM, star_shape)
            else:
                draw_text(d, text, start_font, cx, y0 + START_NUMBER_Y, INK)
                draw_text(d, "START", word_font, cx, y0 + START_WORD_Y, INK)

    out = img.resize((WIDTH * OUT_SCALE, HEIGHT * OUT_SCALE), Image.Resampling.BOX)
    path = output_path()
    out.save(path, optimize=True)
    print("wrote", path)


if __name__ == "__main__":
    main()
