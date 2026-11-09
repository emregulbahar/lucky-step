"""Add the Turkish letters missing from Kenney Future (Kenney UI Pack, CC0) and write a patched copy.

Kenney Future is an all-caps font: its lowercase glyphs are the capital shapes, and 'i' has no dot.
Added glyphs (Story 006), each built from outlines already in the font so the style matches:
  U+0131 dotlessi    = the 'i' stem (undotted, same as I)
  U+0130 Idotaccent  = I + one dieresis dot (from Udieresis) centred over the stem
  U+015F scedilla    = s + the cedilla tail taken from ccedilla
  U+015E Scedilla    = S + the cedilla tail taken from Ccedilla
  U+011F gbreve      = g + a breve cup (drawn here, rounded like the font's corners, sitting where dots sit)
  U+011E Gbreve      = G + the same breve
Because the font is caps-only, Turkish text renders correctly only when written in Turkish upper case
(i -> I with dot, ı -> I), which is how strings_tr.json is written.

Usage (from the repo root):  python -I tools/fonts/patch_kenney_future.py [source_font_dir]
Requires fontTools. Writes Assets/Art/UI/Fonts/KenneyFuture-TR.ttf and KenneyFutureNarrow-TR.ttf.
"""
import os
import sys

from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._g_l_y_f import Glyph, GlyphCoordinates

DEFAULT_SOURCE = "D:/GameDev/Assets/kenney_ui-pack/Font"
FONTS = [("Kenney Future.ttf", "KenneyFuture-TR.ttf"), ("Kenney Future Narrow.ttf", "KenneyFutureNarrow-TR.ttf")]
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(REPO_ROOT, "Assets", "Art", "UI", "Fonts")

# Breve cup, in font units (unitsPerEm 1024). The dieresis dots occupy y 768..896 and are 128 wide.
BREVE_BOTTOM = 768
BREVE_TOP = 896
BREVE_HALF_WIDTH = 160
BREVE_STROKE = 64


def contours(glyf, name):
    """Return a glyph's contours as lists of (x, y, on_curve)."""
    coords, ends, flags = glyf[name].getCoordinates(glyf)
    result, start = [], 0
    for end in ends:
        result.append([(int(coords[i][0]), int(coords[i][1]), bool(flags[i] & 1)) for i in range(start, end + 1)])
        start = end + 1
    return result


def ink_bounds(glyf, name):
    g = glyf[name]
    g.recalcBounds(glyf)
    return g.xMin, g.xMax


def make_glyph(contour_list):
    """Build a simple TrueType glyph from contours of (x, y, on_curve)."""
    pen = TTGlyphPen(None)
    for contour in contour_list:
        if not contour[0][2]:
            raise ValueError("contour must start on-curve")
        last = contour[0][:2]
        pen.moveTo(last)
        pending = []
        for x, y, on in contour[1:]:
            if not on:
                pending.append((x, y))
            elif pending:
                pen.qCurveTo(*pending, (x, y))
                pending, last = [], (x, y)
            elif (x, y) != last:
                pen.lineTo((x, y))
                last = (x, y)
        if pending:
            pen.qCurveTo(*pending, contour[0][:2])
        pen.closePath()
    return pen.glyph()


def cedilla_tail(ccedilla):
    """Points of the tail in a merged C-cedilla outline: from the y=0 point before the y<0 run to the one after."""
    pts = ccedilla
    n = len(pts)
    below = [i for i in range(n) if pts[i][1] < 0]
    if not below:
        raise ValueError("no cedilla tail found")
    # Find the start of the (possibly wrapping) run of y<0 points.
    start = next(i for i in below if pts[(i - 1) % n][1] >= 0)
    run = []
    i = start
    while pts[i % n][1] < 0:
        run.append(pts[i % n])
        i += 1
    tail = [pts[(start - 1) % n]] + run + [pts[i % n]]
    deduped = [tail[0]]
    for p in tail[1:]:
        if p[:2] != deduped[-1][:2]:
            deduped.append(p)
    return deduped


def add_tail(base, tail):
    """Insert the tail into the base outline's bottom edge that runs right-to-left through the tail's x range."""
    contour = list(base)
    n = len(contour)
    x_from, x_to = tail[0][0], tail[-1][0]
    for i in range(n):
        a, b = contour[i], contour[(i + 1) % n]
        if a[1] == 0 and b[1] == 0 and a[2] and b[2] and a[0] >= x_from > x_to >= b[0]:
            inner = [p for p in tail if p[:2] != a[:2] and p[:2] != b[:2]]
            return contour[: i + 1] + inner + contour[i + 1:]
    raise ValueError("no bottom edge to attach the cedilla to")


def breve(cx):
    """A cup-shaped breve centred on cx, clockwise (outer contour), with rounded outer bottom corners."""
    x0, x1 = cx - BREVE_HALF_WIDTH, cx + BREVE_HALF_WIDTH
    t, yb, yt = BREVE_STROKE, BREVE_BOTTOM, BREVE_TOP
    return [
        (x0, yt, True), (x0 + t, yt, True), (x0 + t, yb + t, True), (x1 - t, yb + t, True), (x1 - t, yt, True),
        (x1, yt, True), (x1, yb + t, True), (x1, yb, False), (x1 - t, yb, True), (x0 + t, yb, True),
        (x0, yb, False), (x0, yb + t, True),
    ]


def shift(contour, dx):
    return [(x + dx, y, on) for x, y, on in contour]


def patch(src_path, out_path):
    font = TTFont(src_path)
    glyf, hmtx = font["glyf"], font["hmtx"]

    new = {}
    new["dotlessi"] = ("i", contours(glyf, "i"))

    dot = contours(glyf, "Udieresis")[1]
    dot_min = min(x for x, _, _ in dot)
    dot_width = max(x for x, _, _ in dot) - dot_min
    i_min, i_max = ink_bounds(glyf, "I")
    dot_dx = (i_min + i_max - dot_width) // 2 - dot_min
    new["Idotaccent"] = ("I", contours(glyf, "I") + [shift(dot, dot_dx)])

    for base, ced, name in (("s", "ccedilla", "scedilla"), ("S", "Ccedilla", "Scedilla")):
        base_contours = contours(glyf, base)
        if len(base_contours) != 1 or len(contours(glyf, ced)) != 1:
            raise ValueError(base + "/" + ced + " is not a single outline")
        new[name] = (base, [add_tail(base_contours[0], cedilla_tail(contours(glyf, ced)[0]))])

    for base, name in (("g", "gbreve"), ("G", "Gbreve")):
        g_min, g_max = ink_bounds(glyf, base)
        new[name] = (base, contours(glyf, base) + [breve((g_min + g_max) // 2)])

    codepoints = {"dotlessi": 0x0131, "Idotaccent": 0x0130, "scedilla": 0x015F, "Scedilla": 0x015E,
                  "gbreve": 0x011F, "Gbreve": 0x011E}
    order = font.getGlyphOrder()
    for name, (metrics_from, contour_list) in new.items():
        if name in order:
            raise ValueError(name + " already exists in " + src_path)
        glyph = make_glyph(contour_list)
        glyph.recalcBounds(glyf)
        order.append(name)
        glyf.glyphs[name] = glyph
        hmtx[name] = (hmtx[metrics_from][0], glyph.xMin)
    font.setGlyphOrder(order)
    glyf.glyphOrder = order
    for table in font["cmap"].tables:
        if table.isUnicode():
            for name, cp in codepoints.items():
                table.cmap[cp] = name

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    font.save(out_path)
    print("wrote", out_path, "glyphs:", len(order))


def main():
    source = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_SOURCE
    for src_name, out_name in FONTS:
        patch(os.path.join(source, src_name), os.path.join(OUT_DIR, out_name))


if __name__ == "__main__":
    main()
