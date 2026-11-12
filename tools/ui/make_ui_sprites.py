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


if __name__ == "__main__":
    out = Path(sys.argv[1])
    only = sys.argv[2] if len(sys.argv) > 2 else None  # e.g. "base": draw just that icon
    if only is None:
        for name, (size, face) in CARDS.items():
            card(out / name, size, face)
        lock(out / "icon_lock.png")
        pawn(out / "icon_pawn.png")
        arrow(out / "icon_arrow.png")
    if only in (None, "base"):
        base(out / "icon_base.png")
