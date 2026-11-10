"""Make static Fredoka Bold/SemiBold with the Turkish letters the Google Fonts release lacks (Story 006 UI font).

Fredoka (OFL 1.1, no Reserved Font Name; google/fonts ofl/fredoka, "Fredoka[wdth,wght].ttf" v2.001) has
ı ç Ç ö Ö ü Ü but not ğ Ğ ş Ş İ. This script instances the variable font at wdth 100 and wght 700 / 600, then adds
each missing letter as a composite of glyphs already in the font, placed the way the font places its own accents:
  U+011F gbreve     = g + uni0306 (breve) at the GPOS mark-to-base offset (as the font's odieresis/Odieresis are)
  U+011E Gbreve     = G + uni0306 at the GPOS offset
  U+0130 Idotaccent = I + uni0307.case (cap dot above) at the GPOS offset
  U+015F scedilla   = s + uni0327 (cedilla), copied from ccedilla and shifted by s's top anchor minus c's
  U+015E Scedilla   = S + uni0327, copied from Ccedilla the same way
Each new glyph keeps its base glyph's advance width. A copy of the OFL goes next to the fonts.

Usage (from the repo root):
  python -I tools/fonts/patch_fredoka.py <Fredoka[wdth,wght].ttf> <OFL.txt> Assets/Art/UI/Fonts
Writes Fredoka-Bold.ttf, Fredoka-SemiBold.ttf and Fredoka-OFL.txt. Requires fontTools.
"""
import shutil
import sys
from pathlib import Path

from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

WEIGHTS = {"Bold": 700, "SemiBold": 600}
# new glyph -> (codepoint, base, mark, how the mark is placed)
NEW = {
    "gbreve": (0x011F, "g", "uni0306", "anchor"),
    "Gbreve": (0x011E, "G", "uni0306", "anchor"),
    "Idotaccent": (0x0130, "I", "uni0307.case", "anchor"),
    "scedilla": (0x015F, "s", "uni0327", "ccedilla"),
    "Scedilla": (0x015E, "S", "uni0327", "Ccedilla"),
}


def top_offsets(font):
    """(base, mark) -> mark offset from the GPOS mark-to-base lookups."""
    offsets = {}
    for lookup in font["GPOS"].table.LookupList.Lookup:
        for sub in lookup.SubTable:
            if lookup.LookupType == 9:
                sub = sub.ExtSubTable
            if sub.LookupType != 4:
                continue
            bases, marks = sub.BaseCoverage.glyphs, sub.MarkCoverage.glyphs
            for m, record in zip(marks, sub.MarkArray.MarkRecord):
                for b, base_record in zip(bases, sub.BaseArray.BaseRecord):
                    anchor = base_record.BaseAnchor[record.Class]
                    if anchor is not None:
                        offsets[(b, m)] = (anchor.XCoordinate - record.MarkAnchor.XCoordinate,
                                           anchor.YCoordinate - record.MarkAnchor.YCoordinate)
    return offsets


def component_offset(glyf, composite, name):
    for c in glyf[composite].components:
        if c.glyphName == name:
            return c.x, c.y
    raise KeyError(f"{name} not in {composite}")


def patch(font):
    glyf, hmtx = font["glyf"], font["hmtx"]
    offsets = top_offsets(font)
    order = font.getGlyphOrder()
    for name, (codepoint, base, mark, how) in NEW.items():
        if name in glyf:
            raise SystemExit(f"{name} already exists; the source font changed")
        if how == "anchor":
            dx, dy = offsets[(base, mark)]
        else:  # move the reference cedilla by the difference of the bases' top anchors (both have one for uni0306)
            ref_base = how[0]
            rx, ry = component_offset(glyf, how, mark)
            dx = rx + offsets[(base, "uni0306")][0] - offsets[(ref_base, "uni0306")][0]
            dy = ry
        dx, dy = round(dx), round(dy)  # glyf component offsets are integers (instancing can leave halves)
        pen = TTGlyphPen(glyf)
        pen.addComponent(base, (1, 0, 0, 1, 0, 0))
        pen.addComponent(mark, (1, 0, 0, 1, dx, dy))
        glyph = pen.glyph()
        glyph.components[0].flags |= 0x200  # USE_MY_METRICS: take the base's metrics
        order.append(name)
        glyf[name] = glyph
        glyph.recalcBounds(glyf)
        hmtx[name] = (hmtx[base][0], glyph.xMin)
        for table in font["cmap"].tables:
            if table.isUnicode():
                table.cmap[codepoint] = name
        print(f"  {name} U+{codepoint:04X} = {base} + {mark} at ({dx}, {dy}), width {hmtx[base][0]}")
    font.setGlyphOrder(order)
    if "GDEF" in font and font["GDEF"].table.GlyphClassDef is not None:
        for name in NEW:
            font["GDEF"].table.GlyphClassDef.classDefs[name] = 1  # base glyph


if __name__ == "__main__":
    source, licence, out = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
    for style, weight in WEIGHTS.items():
        font = instancer.instantiateVariableFont(TTFont(source), {"wght": weight, "wdth": 100}, updateFontNames=True)
        print(f"Fredoka-{style} ({font['name'].getDebugName(4)}):")
        patch(font)
        font.save(out / f"Fredoka-{style}.ttf")
    shutil.copyfile(licence, out / "Fredoka-OFL.txt")
