"""Draws the "Printed Board" UI sprites (Story 006; art direction in design/game-brief.md).

1. Card and button faces: cream (#FFF6E0) or gold (#FFC21A) rounded rectangles with a 3 px ink (#1C1A24) border
   and a hard 4 px ink shadow offset down-right (no blur). Drawn at 2x (6 px border, 8 px shadow, 24 px corner
   radius) for UiStyle.slicedPixelsPerUnitMultiplier = 2, so they show at the stated sizes at 1080p. File names
   and sizes match the Kenney sprites they replace, so the 9-slice borders in BuildBoardScene.UiSpriteBorders
   (32 px; 40 px at the bottom of buttons) still fit: every corner, border and shadow lies inside 32 px.
2. White 128x128 icons: a lock (locked level), a pawn (Golden pawn reward), a figure base (rank) and an up arrow (HUD arrow-key
   caps, rotated for the other directions); the UI tints them.
Everything is drawn at 4x and box-downsampled for smooth edges.

Usage: python -I tools/ui/make_ui_sprites.py Assets/Art/UI/Sprites [base]
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

SS = 4  # supersampling factor
BIG, OUT = 512, 128
WHITE, CLEAR = (255, 255, 255, 255), (0, 0, 0, 0)
INK = (0x1C, 0x1A, 0x24, 255)
CREAM = (0xFF, 0xF6, 0xE0, 255)
GOLD = (0xFF, 0xC2, 0x1A, 255)
BORDER, SHADOW, RADIUS = 6, 8, 24  # sprite px (2x of the on-screen 3 px border, 4 px shadow, 12 px radius)
CARDS = {
    "cream_panel.png": ((128, 128), CREAM),
    "cream_button_rectangle_depth_flat.png": ((384, 128), CREAM),
    "cream_button_square_depth_flat.png": ((128, 128), CREAM),
    "yellow_button_rectangle_depth_flat.png": ((384, 128), GOLD),
    "yellow_button_square_depth_flat.png": ((128, 128), GOLD),
}


def card(path, size, face):
    w, h = size[0] * SS, size[1] * SS
    s, b, r = SHADOW * SS, BORDER * SS, RADIUS * SS
    img = Image.new("RGBA", (w, h), INK[:3] + (0,))  # ink-coloured transparency: no dark or light fringe
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((s, s, w - 1, h - 1), radius=r, fill=INK)  # hard shadow
    d.rounded_rectangle((0, 0, w - 1 - s, h - 1 - s), radius=r, fill=INK)  # ink border
    d.rounded_rectangle((b, b, w - 1 - s - b, h - 1 - s - b), radius=r - b, fill=face)
    img.resize(size, Image.BOX).save(path)


def lock(path):
    img = Image.new("RGBA", (BIG, BIG), CLEAR)
    d = ImageDraw.Draw(img)
    d.ellipse((136, 40, 376, 330), outline=WHITE, width=56)
    d.rounded_rectangle((88, 220, 424, 480), radius=48, fill=WHITE)
    d.ellipse((220, 290, 292, 362), fill=CLEAR)
    d.rectangle((240, 330, 272, 420), fill=CLEAR)
    img.resize((OUT, OUT), Image.LANCZOS).save(path)


def pawn(path):
    img = Image.new("RGBA", (BIG, BIG), CLEAR)
    d = ImageDraw.Draw(img)
    d.ellipse((180, 32, 332, 184), fill=WHITE)
    d.rounded_rectangle((164, 184, 348, 224), radius=20, fill=WHITE)
    d.polygon([(212, 220), (300, 220), (364, 420), (148, 420)], fill=WHITE)
    d.rounded_rectangle((104, 400, 408, 480), radius=28, fill=WHITE)
    img.resize((OUT, OUT), Image.LANCZOS).save(path)


def arrow(path):
    img = Image.new("RGBA", (BIG, BIG), CLEAR)
    d = ImageDraw.Draw(img)
    d.polygon([(256, 64), (448, 280), (328, 280), (328, 448), (184, 448), (184, 280), (64, 280)], fill=WHITE)
    img.resize((OUT, OUT), Image.LANCZOS).save(path)


def base(path):
    """Figure base disc seen from above at an angle: white top, light grey rim (the rank colour tints both)."""
    rim = (196, 196, 196, 255)
    img = Image.new("RGBA", (BIG, BIG), CLEAR)
    d = ImageDraw.Draw(img)
    d.ellipse((40, 196, 472, 412), fill=rim)  # bottom edge of the rim
    d.rectangle((40, 250, 472, 304), fill=rim)
    d.ellipse((40, 142, 472, 358), fill=WHITE)  # top face
    img.resize((OUT, OUT), Image.LANCZOS).save(path)


# New-mechanic card icons (spec tutorial-guide-and-tips-addition-2026-11-13): full colour, ink outline, drawn at
# 512 and downsampled to 128. Colours follow the board: hazard red, lucky gold, evil-eye blue.
RED = (0xE2, 0x3B, 0x3B, 255)
GREEN = (0x5C, 0x9E, 0x6B, 255)
PAPER = (0xFF, 0xF6, 0xE0, 255)
BLUE = (0x1F, 0x4F, 0xBF, 255)
SKY = (0x7D, 0xC7, 0xF2, 255)
GLASS = (0xB8, 0xD6, 0xF2, 255)
WOOD = (0xD6, 0x9E, 0x61, 255)
WOOD_DARK = (0x8C, 0x61, 0x3D, 255)
CAT = (0x24, 0x21, 0x29, 255)
HOLE = (0x12, 0x14, 0x26, 255)
LINE = 18  # ink outline width at 512


def _icon(path, draw):
    img = Image.new("RGBA", (BIG, BIG), CLEAR)
    draw(ImageDraw.Draw(img))
    img.resize((OUT, OUT), Image.LANCZOS).save(path)


def tip_spike(d):
    d.rounded_rectangle((60, 380, 452, 450), radius=16, fill=WOOD_DARK, outline=INK, width=LINE)
    for x in (80, 196, 312):
        d.polygon([(x, 390), (x + 60, 90), (x + 120, 390)], fill=RED, outline=INK, width=LINE)


def tip_flowerpot(d):
    d.ellipse((150, 50, 270, 190), fill=GREEN, outline=INK, width=LINE)
    d.ellipse((240, 40, 360, 180), fill=GREEN, outline=INK, width=LINE)
    d.rounded_rectangle((110, 180, 402, 250), radius=14, fill=RED, outline=INK, width=LINE)
    d.polygon([(140, 245), (372, 245), (330, 460), (182, 460)], fill=RED, outline=INK, width=LINE)


def tip_dice(d):
    d.rounded_rectangle((70, 70, 442, 442), radius=70, fill=PAPER, outline=INK, width=LINE)
    for cx, cy in ((160, 160), (352, 160), (256, 256), (160, 352), (352, 352)):
        d.ellipse((cx - 34, cy - 34, cx + 34, cy + 34), fill=INK)


def tip_bridge(d):
    d.ellipse((60, 60, 452, 452), fill=GOLD, outline=INK, width=LINE)
    d.ellipse((130, 130, 382, 382), outline=(0xB8, 0x86, 0x0B, 255), width=22)
    d.polygon([(256, 170), (282, 236), (350, 240), (296, 282), (316, 350), (256, 310), (196, 350), (216, 282),
               (162, 240), (230, 236)], fill=(0xB8, 0x86, 0x0B, 255))


def tip_gap(d):
    d.ellipse((50, 150, 462, 400), fill=PAPER, outline=INK, width=LINE)
    d.ellipse((100, 190, 412, 360), fill=HOLE)


def tip_cat(d):
    d.polygon([(110, 230), (130, 60), (240, 160)], fill=CAT, outline=INK, width=LINE)
    d.polygon([(402, 230), (382, 60), (272, 160)], fill=CAT, outline=INK, width=LINE)
    d.ellipse((90, 130, 422, 450), fill=CAT, outline=INK, width=LINE)
    for cx in (190, 322):
        d.ellipse((cx - 40, 250, cx + 40, 320), fill=GOLD)
        d.ellipse((cx - 10, 256, cx + 10, 314), fill=INK)
    d.polygon([(236, 350), (276, 350), (256, 372)], fill=(0xF2, 0x9A, 0xA8, 255))


def tip_mirror(d):
    d.rounded_rectangle((226, 380, 286, 480), radius=20, fill=WOOD_DARK, outline=INK, width=LINE)
    d.ellipse((110, 30, 402, 400), fill=WOOD, outline=INK, width=LINE)
    d.ellipse((150, 70, 362, 360), fill=GLASS)
    d.line([(200, 110), (262, 210), (230, 250), (300, 330)], fill=INK, width=14)


def tip_wood(d):
    for y in (110, 290):
        d.rounded_rectangle((50, y, 462, y + 130), radius=20, fill=WOOD, outline=INK, width=LINE)
        d.line([(110, y + 45), (300, y + 45)], fill=WOOD_DARK, width=12)
        d.line([(200, y + 88), (400, y + 88)], fill=WOOD_DARK, width=12)
        d.ellipse((380, y + 30, 420, y + 70), fill=INK)


def tip_bead(d):
    d.ellipse((60, 60, 452, 452), fill=BLUE, outline=INK, width=LINE)
    d.ellipse((150, 150, 362, 362), fill=PAPER)
    d.ellipse((190, 190, 322, 322), fill=SKY)
    d.ellipse((226, 226, 286, 286), fill=INK)


TIPS = [tip_spike, tip_flowerpot, tip_dice, tip_bridge, tip_gap, tip_cat, tip_mirror, tip_wood, tip_bead]


if __name__ == "__main__":
    out = Path(sys.argv[1])
    only = sys.argv[2] if len(sys.argv) > 2 else None  # "base" or "tips": draw just those icons
    if only is None:
        for name, (size, face) in CARDS.items():
            card(out / name, size, face)
        lock(out / "icon_lock.png")
        pawn(out / "icon_pawn.png")
        arrow(out / "icon_arrow.png")
    if only in (None, "base"):
        base(out / "icon_base.png")
    if only in (None, "tips"):
        for draw in TIPS:
            _icon(out / (draw.__name__ + ".png"), draw)
