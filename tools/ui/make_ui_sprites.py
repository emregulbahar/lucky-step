"""Makes the UI sprites the Kenney UI Pack does not have in the warm board-game palette (Story 006).

1. Recolours Kenney's grey "Double" sprites to cream/brown (per-channel linear map: grey face -> cream,
   blue-grey depth -> wood brown, white highlight stays near white). Kenney originals are not modified.
2. Draws two white 128x128 icons the pack lacks: a lock (locked level) and a pawn (Golden pawn reward);
   the UI tints them. Drawn at 4x and downsampled for smooth edges.

Usage: python -I tools/ui/make_ui_sprites.py <kenney_ui-pack/PNG dir> Assets/Art/UI/Sprites
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

BIG, OUT = 512, 128
WHITE, CLEAR = (255, 255, 255, 255), (0, 0, 0, 0)
# Grey sprite colours (face 218,220,231; depth 102,104,128) -> cream (250,238,214) and brown (150,105,70).
WARM = [((102, 218), (150, 250)), ((104, 220), (105, 238)), ((128, 231), (70, 214))]
RECOLOUR = {
    "Grey/Double/button_rectangle_depth_flat.png": "cream_button_rectangle_depth_flat.png",
    "Grey/Double/button_square_depth_flat.png": "cream_button_square_depth_flat.png",
    "Grey/Double/button_square_flat.png": "cream_panel.png",
}


def warm(src, dst):
    img = Image.open(src).convert("RGBA")
    bands = list(img.split())
    for i, ((x0, x1), (y0, y1)) in enumerate(WARM):
        k = (y1 - y0) / (x1 - x0)
        bands[i] = bands[i].point(lambda v, k=k, x0=x0, y0=y0: max(0, min(255, round(y0 + k * (v - x0)))))
    Image.merge("RGBA", bands).save(dst)


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


if __name__ == "__main__":
    kenney, out = Path(sys.argv[1]), Path(sys.argv[2])
    for src, dst in RECOLOUR.items():
        warm(kenney / src, out / dst)
    lock(out / "icon_lock.png")
    pawn(out / "icon_pawn.png")
