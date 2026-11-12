"""Lucky Step board-piece generator (Story 005, visual slice; "Printed Board" look).

Builds the low-poly board pieces and exports one FBX per piece:
die, coin, spike, tile_floor, tile_gap, rim_bar, wall_cards, wall_chips, wall_box, mirror,
portal_frame, portal_disc, paw_print, mug, card_fan, rule_booklet (table props; and pawn, kept but no longer used by the game),
bead (evil-eye bead, World 5) and tile_wood (three nailed planks laid on a space, World 5), and the world table props
(world table themes): milk_saucer, yarn_ball, street_lantern, candlestick, hand_mirror, paper_lantern, tea_tray, nazar_charm.

Run headless (Blender 5.1):
    blender.exe --background --python tools/blender/make_models.py [-- <output_dir>]

Default output: <repo>/Assets/Art/Models. Units: 1 Blender metre = 1 Unity unit = 1 board tile.
Material slot names are the contract with Unity: Assets/Editor/BuildBoardScene.cs remaps each slot
name to a Unity material, so the colours here are only for Blender previews.
Origins: tiles have their TOP face at z=0; standing pieces have their BOTTOM at z=0; the coin is centred.
Spaces (tile_floor, tile_gap) are thin card slabs UV-mapped onto the printed textures written by
tools/textures/make_print_textures.py. Outlined pieces (build(outline=True)) carry their smoothed normals,
in Unity object space, in the vertex colour (see bake_smooth_normals).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

SEG = 20  # radial segments for turned pieces

TILE = 0.96  # footprint of a printed space; BoardView's score-track blocks assume this size
SLAB = 0.04  # card thickness of a space; the top face is at z=0
# Centre of the bottom run of the printed ink border (texture pixels 5..11 of 256 from the edge, see
# tools/textures/make_print_textures.py). Slab sides and bottoms collapse onto this point, so they show ink;
# a constant UV has zero screen derivatives, so the GPU samples mip 0 and paper never bleeds in.
INK_UV = (0.5, 8.0 / 256.0)

PREVIEW_COLOURS = {
    "PawnPaint": (0.80, 0.18, 0.15),
    "Ivory": (0.96, 0.93, 0.85),
    "PipBlack": (0.06, 0.06, 0.06),
    "Gold": (0.95, 0.75, 0.25),
    "Metal": (0.62, 0.66, 0.68),
    "SpikeBase": (0.55, 0.36, 0.22),
    "Tile": (0.90, 0.68, 0.45),
    "Hole": (0.08, 0.10, 0.20),
    "Rim": (0.15, 0.20, 0.36),
    "CardFace": (0.95, 0.94, 0.90),
    "CardBack": (0.64, 0.15, 0.18),
    "ChipRed": (0.75, 0.12, 0.10),
    "ChipWhite": (0.92, 0.90, 0.85),
    "ChipGreen": (0.18, 0.54, 0.36),
    "CardTrim": (1.00, 0.96, 0.88),
    "BoxLogo": (1.00, 0.96, 0.88),
    "ChipLine": (0.30, 0.33, 0.45),
    "BoxBody": (0.20, 0.32, 0.55),
    "BoxLid": (0.90, 0.72, 0.25),
    "MirrorFrame": (0.45, 0.30, 0.20),
    "MirrorGlass": (0.75, 0.85, 0.95),
    "MirrorCrack": (0.10, 0.10, 0.12),
    "PortalFrame": (0.55, 0.36, 0.22),
    "PortalStone": (0.55, 0.42, 0.18),
    "PortalSwirlA": (1.00, 0.72, 0.18),
    "PortalSwirlB": (1.00, 0.88, 0.50),
    "PawPrint": (0.22, 0.14, 0.09),
    "MugBody": (0.96, 0.93, 0.86),
    "Coffee": (0.30, 0.18, 0.10),
    "BeadBlue": (0.12, 0.31, 0.75),
    "BeadLight": (0.49, 0.78, 0.95),
    "Plank": (0.84, 0.62, 0.38),
    # World table props (world table themes 2026-11-12).
    "Milk": (0.97, 0.96, 0.92),
    "Yarn": (0.85, 0.51, 0.23),
    "YarnLine": (0.66, 0.37, 0.13),
    "LanternGlass": (1.00, 0.85, 0.50),
    "MetalDark": (0.36, 0.42, 0.46),
    "Brass": (0.71, 0.54, 0.23),
    "Wax": (0.94, 0.89, 0.78),
    "Flame": (1.00, 0.78, 0.35),
    "LanternPaper": (0.77, 0.20, 0.17),
    "Copper": (0.78, 0.45, 0.25),
    "Tea": (0.61, 0.23, 0.10),
    "Glass": (0.62, 0.72, 0.78),
}


def output_dir():
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        if args:
            return os.path.abspath(args[0])
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    return os.path.join(root, "Assets", "Art", "Models")


def slab_uv(x, y):
    """UV of a slab top-face point: 0..1 across the space, upright in Unity (image top towards Unity +Z).

    Unity sees Blender (x, y) as (-x, -y) on its ground plane (see unity_space), so both axes are flipped here.
    """
    return (0.5 - x / TILE, 0.5 - y / TILE)


def unity_space(n):
    """Blender object-space direction -> Unity object space, for this script's export settings.

    The FBX exporter (axis_forward='-Z', axis_up='Y', bake_space_transform=True) bakes Blender (x, y, z) into the
    file as FBX (x, z, -y) and writes the default FBX axis system (Y up, front +Z, coord +X, right-handed);
    Unity's importer then mirrors FBX X into its left-handed space. Net: Blender (x, y, z) -> Unity (-x, z, -y).
    """
    return Vector((-n.x, n.z, -n.y))


def bake_smooth_normals(mesh, merge=1e-4):
    """Writes an outline normal per vertex into colour attribute 'SmoothNormal' (POINT domain, float, linear).

    For every position (vertices closer than `merge` count as one, so split and coincident vertices agree) the
    corner-angle-weighted normals of all faces touching it are summed and normalised, converted to Unity object
    space and encoded as rgb = n * 0.5 + 0.5, a = 1. Exported with colors_type='LINEAR', so Unity's mesh.colors
    holds these values unchanged; the Toon shader decodes COLOR.rgb * 2 - 1 when _OutlineSmoothNormals is on.
    """
    keys = [tuple(round(c / merge) for c in v.co) for v in mesh.vertices]
    sums = {}
    for poly in mesh.polygons:
        idx = poly.vertices
        k = len(idx)
        for c in range(k):
            p = mesh.vertices[idx[c]].co
            a = mesh.vertices[idx[c - 1]].co - p
            b = mesh.vertices[idx[(c + 1) % k]].co - p
            if a.length < 1e-9 or b.length < 1e-9:
                continue
            key = keys[idx[c]]
            sums[key] = sums.get(key, Vector((0.0, 0.0, 0.0))) + poly.normal * a.angle(b)
    attr = mesh.color_attributes.new("SmoothNormal", 'FLOAT_COLOR', 'POINT')
    flat = []
    for v, key in zip(mesh.vertices, keys):
        n = sums.get(key, Vector((0.0, 0.0, 0.0)))
        n = n.normalized() if n.length > 1e-6 else v.normal.copy()
        u = unity_space(n)
        flat.extend((u.x * 0.5 + 0.5, u.y * 0.5 + 0.5, u.z * 0.5 + 0.5, 1.0))
    attr.data.foreach_set("color", flat)
    mesh.color_attributes.active_color = attr


def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*PREVIEW_COLOURS[name], 1.0)
    return mat


class Piece:
    """Accumulates primitives into one bmesh, tagging faces with a material slot."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.slots = []

    def _slot(self, mat_name):
        if mat_name not in self.slots:
            self.slots.append(mat_name)
        return self.slots.index(mat_name)

    def _tag(self, before, mat_name, bevel=0.0):
        new_faces = [f for f in self.bm.faces if f not in before]
        if bevel > 0.0:
            edges = list({e for f in new_faces for e in f.edges})
            bmesh.ops.bevel(self.bm, geom=edges, offset=bevel, segments=1, affect='EDGES',
                            clamp_overlap=True)
            new_faces = [f for f in self.bm.faces if f not in before]
        idx = self._slot(mat_name)
        for f in new_faces:
            f.material_index = idx

    def box(self, mat_name, size, centre, bevel=0.0, rot=None):
        """Box of the given size; rot is an optional 4x4 rotation applied about its centre."""
        before = set(self.bm.faces)
        r = rot if rot is not None else Matrix.Identity(4)
        m = Matrix.Translation(Vector(centre)) @ r @ Matrix.Diagonal((*size, 1.0))
        bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        self._tag(before, mat_name, bevel)

    def cone(self, mat_name, r_bottom, r_top, z0, z1, x=0.0, y=0.0, seg=SEG):
        before = set(self.bm.faces)
        m = Matrix.Translation((x, y, (z0 + z1) * 0.5))
        bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=seg,
                              radius1=r_bottom, radius2=r_top, depth=z1 - z0, matrix=m)
        self._tag(before, mat_name)

    def sphere(self, mat_name, radius, centre, u=16, v=8):
        before = set(self.bm.faces)
        bmesh.ops.create_uvsphere(self.bm, u_segments=u, v_segments=v, radius=radius,
                                  matrix=Matrix.Translation(Vector(centre)))
        self._tag(before, mat_name)

    def disc(self, mat_name, radius, depth, centre, axis, seg=12):
        """Cylinder whose axis is 'x', 'y' or 'z', centred at centre."""
        before = set(self.bm.faces)
        rot = {"x": Matrix.Rotation(math.radians(90), 4, 'Y'),
               "y": Matrix.Rotation(math.radians(90), 4, 'X'),
               "z": Matrix.Identity(4)}[axis]
        m = Matrix.Translation(Vector(centre)) @ rot
        bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=seg,
                              radius1=radius, radius2=radius, depth=depth, matrix=m)
        self._tag(before, mat_name)

    def ring(self, mat_name, major, minor, centre, major_seg=28, minor_seg=8, rot=None):
        """Torus standing upright in the XZ plane (axis along Y); rot (3x3) turns it about its centre."""
        before = set(self.bm.faces)
        before_verts = set(self.bm.verts)
        grid = []
        for i in range(major_seg):
            a = 2.0 * math.pi * i / major_seg
            row = []
            for j in range(minor_seg):
                b = 2.0 * math.pi * j / minor_seg
                rr = major + minor * math.cos(b)
                row.append(self.bm.verts.new((centre[0] + rr * math.cos(a), centre[1] + minor * math.sin(b),
                                              centre[2] + rr * math.sin(a))))
            grid.append(row)
        for i in range(major_seg):
            for j in range(minor_seg):
                i2, j2 = (i + 1) % major_seg, (j + 1) % minor_seg
                self.bm.faces.new((grid[i][j], grid[i2][j], grid[i2][j2], grid[i][j2]))
        if rot is not None:
            new_verts = [v for v in self.bm.verts if v not in before_verts]
            bmesh.ops.rotate(self.bm, verts=new_verts, cent=Vector(centre), matrix=rot)
        self._tag(before, mat_name)

    def lathe(self, profile, mats, top=None, x=0.0, y=0.0, seg=SEG):
        """Turned piece as ONE closed shell: profile = [(z, r), ...] bottom to top (r > 0), mats[k] colours the band
        from profile[k] to profile[k+1]; bottom cap takes mats[0], top cap `top` (default mats[-1]). No inner caps, so
        the baked outline normals stay smooth across bands (like banded_prism)."""
        bm = self.bm
        rings = [[bm.verts.new((x + r * math.cos(2 * math.pi * k / seg), y + r * math.sin(2 * math.pi * k / seg), z))
                  for k in range(seg)] for z, r in profile]
        for k, mat in enumerate(mats):
            idx = self._slot(mat)
            lo, hi = rings[k], rings[k + 1]
            for i in range(seg):
                bm.faces.new((lo[i], lo[(i + 1) % seg], hi[(i + 1) % seg], hi[i])).material_index = idx
        bm.faces.new(tuple(reversed(rings[0]))).material_index = self._slot(mats[0])
        bm.faces.new(tuple(rings[-1])).material_index = self._slot(top or mats[-1])

    def swirl_disc(self, mats, radius, centre, arms=6, rings=4, twist=1.6, seg=36):
        """Double-sided flat disc in the XZ plane with spiral arms alternating between two materials."""
        cx, cy, cz = centre
        idx = [self._slot(m) for m in mats]
        for side in (1, -1):
            y = cy + side * 0.004
            centre_v = self.bm.verts.new((cx, y, cz))
            grid = []
            for r in range(1, rings + 1):
                rr = radius * r / rings
                off = twist * (1.0 - r / rings)
                grid.append([self.bm.verts.new((cx + rr * math.cos(2 * math.pi * k / seg + off), y,
                                                cz + rr * math.sin(2 * math.pi * k / seg + off)))
                             for k in range(seg)])
            for k in range(seg):
                k2 = (k + 1) % seg
                arm = (k * arms // seg) % 2
                tri = (centre_v, grid[0][k], grid[0][k2]) if side > 0 else (centre_v, grid[0][k2], grid[0][k])
                self.bm.faces.new(tri).material_index = idx[arm]
                for r in range(rings - 1):
                    q = (grid[r][k], grid[r + 1][k], grid[r + 1][k2], grid[r][k2])
                    self.bm.faces.new(q if side > 0 else tuple(reversed(q))).material_index = idx[arm]

    def decal(self, mat_name, w, d, z):
        """Flat upward quad w x d at height z, UV 0..1 over the whole image, upright in Unity (see slab_uv)."""
        bm = self.bm
        uv = bm.loops.layers.uv.verify()
        verts = [bm.verts.new((x, y, z)) for x, y in ((-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2))]
        f = bm.faces.new(verts)
        f.material_index = self._slot(mat_name)
        for loop in f.loops:
            loop[uv].uv = (0.5 - loop.vert.co.x / w, 0.5 - loop.vert.co.y / d)

    def slab(self, cuts, slot_of):
        """Card slab TILE x TILE x SLAB, top at z=0, its top split on the grid `cuts` (same coordinates on x and y).

        slot_of(i, j) names the material of top cell (i, j). Top faces get slab_uv; sides and bottom take the
        outer cell's slot and collapse onto INK_UV.
        """
        bm = self.bm
        uv = bm.loops.layers.uv.verify()
        n = len(cuts)
        top = [[bm.verts.new((x, y, 0.0)) for x in cuts] for y in cuts]  # top[j][i]
        for j in range(n - 1):
            for i in range(n - 1):
                f = bm.faces.new((top[j][i], top[j][i + 1], top[j + 1][i + 1], top[j + 1][i]))
                f.material_index = self._slot(slot_of(i, j))
                for loop in f.loops:
                    loop[uv].uv = slab_uv(loop.vert.co.x, loop.vert.co.y)
        ring = ([top[0][i] for i in range(n)] + [top[j][n - 1] for j in range(1, n)]
                + [top[n - 1][i] for i in range(n - 2, -1, -1)] + [top[j][0] for j in range(n - 2, 0, -1)])
        low = [bm.verts.new((v.co.x, v.co.y, -SLAB)) for v in ring]
        edge = self._slot(slot_of(0, 0))
        m = len(ring)
        faces = [bm.faces.new((ring[k], low[k], low[(k + 1) % m], ring[(k + 1) % m])) for k in range(m)]
        faces.append(bm.faces.new(tuple(reversed(low))))
        for f in faces:
            f.material_index = edge
            for loop in f.loops:
                loop[uv].uv = INK_UV

    def banded_prism(self, outline, bands, x=0.0, y=0.0):
        """Vertical prism over the 2D polygon `outline` (counter-clockwise, centred on x, y) with side bands.

        bands = [(z0, z1, mat), ...] bottom to top, touching. One closed shell: side quads take their band's slot,
        the bottom cap the first band's, the top cap the last band's; no inner caps, so bake_smooth_normals gives
        the band rings pure sideways normals and the outline wraps the whole prism without lines across it.
        """
        bm = self.bm
        levels = [bands[0][0]] + [b[1] for b in bands]
        rings = [[bm.verts.new((x + px, y + py, z)) for px, py in outline] for z in levels]
        m = len(outline)
        for k, (_, _, mat) in enumerate(bands):
            idx = self._slot(mat)
            lo, hi = rings[k], rings[k + 1]
            for i in range(m):
                bm.faces.new((lo[i], lo[(i + 1) % m], hi[(i + 1) % m], hi[i])).material_index = idx
        bm.faces.new(tuple(reversed(rings[0]))).material_index = self._slot(bands[0][2])
        bm.faces.new(tuple(rings[-1])).material_index = self._slot(bands[-1][2])

    def build(self, sharp_angle_deg=35.0, outline=False):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        self.bm.free()
        for slot in self.slots:
            mesh.materials.append(material(slot))
        mesh.shade_smooth()
        mesh.set_sharp_from_angle(angle=math.radians(sharp_angle_deg))
        if outline:
            bake_smooth_normals(mesh)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        return obj


def make_pawn():
    p = Piece("pawn")
    p.cone("PawnPaint", 0.22, 0.21, 0.00, 0.05)
    p.cone("PawnPaint", 0.21, 0.16, 0.05, 0.10)
    p.cone("PawnPaint", 0.15, 0.075, 0.10, 0.40)
    p.cone("PawnPaint", 0.125, 0.125, 0.40, 0.44)
    p.sphere("PawnPaint", 0.10, (0.0, 0.0, 0.53))
    return p.build(outline=True)


PIP_LAYOUT = {
    1: [(0, 0)],
    2: [(-1, -1), (1, 1)],
    3: [(-1, -1), (0, 0), (1, 1)],
    4: [(-1, -1), (-1, 1), (1, -1), (1, 1)],
    5: [(-1, -1), (-1, 1), (0, 0), (1, -1), (1, 1)],
    6: [(-1, -1), (-1, 0), (-1, 1), (1, -1), (1, 0), (1, 1)],
}


def make_die():
    side = 0.45
    h = side * 0.5
    p = Piece("die")
    p.box("Ivory", (side, side, side), (0.0, 0.0, h), bevel=0.045)
    step = side * 0.26
    out = h + 0.002
    # (value, axis, sign): opposite faces sum to 7.
    faces = [(1, "z", 1), (6, "z", -1), (2, "x", 1), (5, "x", -1), (3, "y", 1), (4, "y", -1)]
    for value, axis, sign in faces:
        for a, b in PIP_LAYOUT[value]:
            u, v = a * step, b * step
            if axis == "z":
                c = (u, v, h + sign * out)
            elif axis == "x":
                c = (sign * out, u, h + v)
            else:
                c = (u, sign * out, h + v)
            p.disc("PipBlack", 0.034, 0.012, c, axis, seg=10)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_coin():
    p = Piece("coin")
    p.disc("Gold", 0.16, 0.035, (0, 0, 0), "z", seg=24)
    p.disc("Gold", 0.115, 0.047, (0, 0, 0), "z", seg=24)
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_spike():
    p = Piece("spike")
    p.box("SpikeBase", (0.74, 0.74, 0.05), (0, 0, 0.025), bevel=0.012)
    for x, y in [(0, 0), (-0.2, -0.2), (0.2, -0.2), (-0.2, 0.2), (0.2, 0.2)]:
        p.cone("Metal", 0.085, 0.0, 0.05, 0.34, x, y, seg=8)
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_tile_floor():
    """Printed space: one card slab (slot Tile), top face = the whole printed texture, ink sides."""
    p = Piece("tile_floor")
    h = TILE * 0.5
    p.slab([-h, h], lambda i, j: "Tile")
    return p.build()


def make_rim_bar():
    """Unit bar (1 x 1 x 1, bottom at z=0), square edges; BoardView scales it into the flat printed board border."""
    p = Piece("rim_bar")
    p.box("Rim", (1.0, 1.0, 1.0), (0, 0, 0.5))
    return p.build()


def make_wall_cards():
    """Blocking piece: one squared-up deck of playing cards, its red printed back on top (one clean silhouette).

    The deck is a banded prism (see Piece.banded_prism): its middle rings carry pure sideways outline normals, so
    the outline keeps its width along the tall sides (a plain box only has corner normals tilted up or down).
    """
    p = Piece("wall_cards")
    w, d = 0.28, 0.4
    p.banded_prism([(-w, -d), (w, -d), (w, d), (-w, d)], [(0.0, 0.1, "CardFace"), (0.1, 0.2, "CardFace"), (0.2, 0.3, "CardFace")])
    p.box("CardBack", (0.46, 0.7, 0.006), (0, 0, 0.3 + 0.003))
    # Printed back (look pass 2): cream inset border and a long diamond with a red core, flat on the red back.
    top, t, inset, line = 0.306, 0.0012, 0.04, 0.016
    hw, hd = 0.23 - inset, 0.35 - inset
    for size, centre in (((2 * hw, line, t), (0, hd, top)), ((2 * hw, line, t), (0, -hd, top)),
                         ((line, 2 * hd, t), (hw, 0, top)), ((line, 2 * hd, t), (-hw, 0, top))):
        p.box("CardTrim", size, centre)
    stretch = Matrix.Diagonal((1.0, 1.55, 1.0, 1.0)) @ Matrix.Rotation(math.radians(45), 4, 'Z')
    for side, mat, z in ((0.15, "CardTrim", top), (0.11, "CardBack", top + t), (0.05, "CardTrim", top + 2 * t)):
        p.box(mat, (side, side, t), (0, 0, z), rot=stretch)
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_wall_chips():
    """Blocking piece: three poker-chip stacks; one colour per stack, thin edge lines between chips.

    Each stack is one banded prism (see Piece.banded_prism), so the ink outline wraps the whole stack, not every chip.
    """
    p = Piece("wall_chips")
    chip, line, seg, r = 0.039, 0.006, 24, 0.16
    circle = [(r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg)) for k in range(seg)]
    stacks = [("ChipRed", -0.17, -0.14, 9), ("ChipWhite", 0.18, -0.1, 7), ("ChipGreen", 0.0, 0.2, 11)]
    for mat, x, y, n in stacks:
        bands, z = [], 0.0
        for i in range(n):
            bands.append((z, z + chip, mat))
            z += chip
            if i < n - 1:
                bands.append((z, z + line, "ChipLine"))
                z += line
        p.banded_prism(circle, bands, x, y)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_wall_box():
    """Blocking piece: a small closed board-game box with a lid."""
    p = Piece("wall_box")
    p.box("BoxBody", (0.78, 0.78, 0.26), (0, 0, 0.13), bevel=0.015)
    p.box("BoxLid", (0.82, 0.82, 0.1), (0, 0, 0.26 + 0.03), bevel=0.02)
    # Look pass 2: the lid carries the printed "Lucky Step" logo (BoxLogo.png, tools/textures/make_print_textures.py).
    p.decal("BoxLogo", 0.7, 0.7, 0.34 + 0.001)
    return p.build(sharp_angle_deg=30.0, outline=True)


def card_back(p, x, y, z, yaw):
    """One playing card lying face down at (x, y), bottom at z, turned by yaw degrees: red back, cream print."""
    turn = Matrix.Rotation(math.radians(yaw), 4, 'Z')

    def at(dx, dy, dz):
        v = turn @ Vector((dx, dy, 0.0))
        return (x + v.x, y + v.y, z + dz)

    p.box("CardFace", (0.46, 0.7, 0.012), at(0, 0, 0.006), rot=turn)
    top, t, inset, line = 0.0125, 0.0012, 0.04, 0.016
    hw, hd = 0.23 - inset, 0.35 - inset
    for size, (dx, dy) in (((2 * hw, line, t), (0, hd)), ((2 * hw, line, t), (0, -hd)),
                           ((line, 2 * hd, t), (hw, 0)), ((line, 2 * hd, t), (-hw, 0))):
        p.box("CardTrim", size, at(dx, dy, top + t), rot=turn)
    p.box("CardBack", (0.44, 0.68, t), at(0, 0, top), rot=turn)
    stretch = turn @ Matrix.Diagonal((1.0, 1.55, 1.0, 1.0)) @ Matrix.Rotation(math.radians(45), 4, 'Z')
    for side, mat, k in ((0.15, "CardTrim", 2), (0.11, "CardBack", 3), (0.05, "CardTrim", 4)):
        p.box(mat, (side, side, t), at(0, 0, top + k * t), rot=stretch)


def make_card_fan():
    """Table prop: four cards fanned out face down from a common corner; bottom at z=0."""
    p = Piece("card_fan")
    for k, yaw in enumerate((-33.0, -12.0, 9.0, 30.0)):
        v = Matrix.Rotation(math.radians(yaw), 4, 'Z') @ Vector((0.0, 0.3, 0.0))
        card_back(p, v.x, v.y, 0.013 * k, yaw)
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_rule_booklet():
    """Table prop: a closed rule booklet; cream cover with a red title band and printed lines; bottom at z=0."""
    p = Piece("rule_booklet")
    w, d, h = 0.62, 0.86, 0.05
    p.box("CardFace", (w, d, h), (0, 0, h * 0.5), bevel=0.01)
    # Blender -y is Unity +z (the far side), so the title band sits at -y to read as the top of the cover.
    p.box("CardBack", (w - 0.08, 0.2, 0.002), (0, -(d * 0.5 - 0.17), h + 0.001))
    for k in range(4):
        p.box("PipBlack", ((w - 0.16) * (0.9 if k % 2 else 1.0), 0.022, 0.002), (0, -0.05 + 0.09 * k, h + 0.001))
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_mug():
    """Table prop (not a board piece): a coffee mug with a side handle; bottom at z=0, about 0.4 tall."""
    p = Piece("mug")
    p.cone("MugBody", 0.19, 0.2, 0.0, 0.38, seg=24)
    p.cone("Coffee", 0.165, 0.165, 0.38, 0.386, seg=24)
    p.ring("MugBody", 0.1, 0.032, (0.21, 0.0, 0.2), major_seg=20, minor_seg=8)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_mirror():
    """Standing broken mirror: feet, wooden frame, pale glass and dark crack lines (visible from both sides)."""
    p = Piece("mirror")
    w, h, t, bottom = 0.56, 0.78, 0.05, 0.08
    fr = 0.05
    zc = bottom + h * 0.5
    p.box("MirrorFrame", (0.12, 0.3, 0.04), (-w * 0.35, 0, 0.02), bevel=0.01)
    p.box("MirrorFrame", (0.12, 0.3, 0.04), (w * 0.35, 0, 0.02), bevel=0.01)
    p.box("MirrorFrame", (fr, t, h), (-(w - fr) * 0.5, 0, zc), bevel=0.01)
    p.box("MirrorFrame", (fr, t, h), ((w - fr) * 0.5, 0, zc), bevel=0.01)
    p.box("MirrorFrame", (w, t, fr), (0, 0, bottom + fr * 0.5), bevel=0.01)
    p.box("MirrorFrame", (w, t, fr), (0, 0, bottom + h - fr * 0.5), bevel=0.01)
    gw, gh = w - 2 * fr, h - 2 * fr
    p.box("MirrorGlass", (gw, 0.016, gh), (0, 0, zc))
    # Crack lines radiating from an impact point, plus two branches (glass-relative u, v coordinates).
    impact = (0.06, 0.12)
    ends = [(0.21, 0.22), (0.13, 0.32), (-0.12, 0.31), (-0.21, 0.04), (-0.13, -0.22), (0.04, -0.32), (0.21, -0.08)]
    segments = [(impact, e) for e in ends] + [((-0.04, -0.05), (-0.18, -0.1)), ((0.12, 0.02), (0.2, -0.2))]
    for (u0, v0), (u1, v1) in segments:
        du, dv = u1 - u0, v1 - v0
        length = math.hypot(du, dv)
        p.box("MirrorCrack", (length, 0.024, 0.016), ((u0 + u1) * 0.5, 0, zc + (v0 + v1) * 0.5),
              rot=Matrix.Rotation(-math.atan2(dv, du), 4, 'Y'))
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_portal_frame():
    """Exit: oak plinth and an upright HORSESHOE arch (open end down, lucky); the glow disc is a separate model so it can spin."""
    p = Piece("portal_frame")
    p.box("PortalFrame", (0.8, 0.36, 0.08), (0, 0, 0.04), bevel=0.02)
    p.box("PortalFrame", (0.12, 0.2, 0.2), (-0.3, 0, 0.18), bevel=0.02)
    p.box("PortalFrame", (0.12, 0.2, 0.2), (0.3, 0, 0.18), bevel=0.02)
    radius, cz, segs = 0.34, 0.47, 16
    start, sweep = math.radians(-30.0), math.radians(240.0)
    step = sweep / segs
    chord = 2.0 * radius * math.sin(step * 0.5) * 1.12
    for i in range(segs):
        a = start + step * (i + 0.5)
        p.box("PortalStone", (chord, 0.09, 0.09), (radius * math.cos(a), 0, cz + radius * math.sin(a)),
              rot=Matrix.Rotation(-(a + math.pi * 0.5), 4, 'Y'))
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_paw_print():
    """Flat cat paw print (pad + four toes), toes pointing +Y, bottom at z=0; marks the cat's route."""
    p = Piece("paw_print")
    h = 0.006
    p.cone("PawPrint", 0.075, 0.075, 0.0, h, 0.0, -0.03, seg=12)
    for x, y in [(-0.085, 0.045), (-0.03, 0.095), (0.03, 0.095), (0.085, 0.045)]:
        p.cone("PawPrint", 0.032, 0.032, 0.0, h, x, y, seg=10)
    return p.build(sharp_angle_deg=30.0)


def make_portal_disc():
    """Emissive swirl disc, centred at the origin, facing along Y; spun at runtime."""
    p = Piece("portal_disc")
    p.swirl_disc(["PortalSwirlA", "PortalSwirlB"], 0.26, (0, 0, 0))
    return p.build(sharp_angle_deg=10.0)


def make_tile_gap():
    """Printed space with a flat dark hole: the same card slab, its top a paper ring (slot Tile, printed texture
    with its ink border) around a flush 0.70-wide square hole (slot Hole); ink sides like tile_floor."""
    p = Piece("tile_gap")
    h, hole = TILE * 0.5, 0.35
    p.slab([-h, -hole, hole, h], lambda i, j: "Hole" if (i, j) == (1, 1) else "Tile")
    return p.build()


def make_bead():
    """Evil-eye bead (nazar boncugu): an upright glass disc, cobalt / white / light blue / black rings on both
    faces, with a small gold loop on top. Disc axis along Y (faces front and back); bottom at z=0."""
    p = Piece("bead")
    r = 0.17
    c = (0.0, 0.0, r)
    p.disc("BeadBlue", r, 0.06, c, "y", seg=28)
    p.disc("Ivory", 0.115, 0.068, c, "y", seg=28)
    p.disc("BeadLight", 0.075, 0.076, c, "y", seg=24)
    p.disc("PipBlack", 0.038, 0.084, c, "y", seg=16)
    p.ring("Gold", 0.032, 0.011, (0.0, 0.0, 2 * r + 0.022), major_seg=16, minor_seg=6)
    return p.build(sharp_angle_deg=30.0, outline=True)


def make_tile_wood():
    """Knock-on-wood space: three nailed planks laid across a printed space; bottom at z=0 (the space's top)."""
    p = Piece("tile_wood")
    width, gap, thick = 0.26, 0.035, 0.05
    yaws = (-2.5, 1.5, -1.0)
    for k, (offset, yaw) in enumerate(zip((-(width + gap), 0.0, width + gap), yaws)):
        rot = Matrix.Rotation(math.radians(yaw), 4, 'Z')
        length = 0.86 - 0.03 * k
        p.box("Plank", (length, width, thick), (0.0, offset, thick * 0.5), bevel=0.012, rot=rot)
        for side in (-1, 1):
            nx = side * (length * 0.5 - 0.07)
            ny = offset + nx * math.tan(math.radians(yaw))
            p.disc("PipBlack", 0.018, 0.012, (nx, ny, thick + 0.002), "z", seg=8)
    return p.build(sharp_angle_deg=30.0, outline=True)


# ---- World table props (world table themes 2026-11-12): decoration beside the board, bottom at z=0. ----

def make_milk_saucer():
    """World 2: a cream saucer of milk for the alley cat."""
    p = Piece("milk_saucer")
    p.lathe([(0.0, 0.17), (0.02, 0.24), (0.045, 0.29)], ["MugBody", "MugBody"], top="Milk", seg=28)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_yarn_ball():
    """World 2: a ball of yarn with wound strands and a loose end."""
    p = Piece("yarn_ball")
    r = 0.2
    p.sphere("Yarn", r, (0.0, 0.0, r), u=16, v=10)
    for axis, deg in (("X", 0.0), ("X", 60.0), ("X", -60.0), ("Z", 90.0)):
        rot = Matrix.Rotation(math.radians(deg), 3, axis)
        p.ring("YarnLine", r - 0.002, 0.014, (0.0, 0.0, r), major_seg=24, minor_seg=6, rot=rot)
    p.box("YarnLine", (0.03, 0.26, 0.02), (0.05, -0.2, 0.01), rot=Matrix.Rotation(math.radians(20), 4, 'Z'))
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_street_lantern():
    """World 2: a small street lantern (dark iron post, warm glass box, pyramid roof); about 0.9 tall."""
    p = Piece("street_lantern")
    p.box("MetalDark", (0.24, 0.24, 0.04), (0, 0, 0.02), bevel=0.008)
    p.cone("MetalDark", 0.035, 0.028, 0.04, 0.56, seg=10)
    p.box("MetalDark", (0.2, 0.2, 0.025), (0, 0, 0.5725))
    p.box("LanternGlass", (0.15, 0.15, 0.19), (0, 0, 0.68))
    for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        p.box("MetalDark", (0.025, 0.025, 0.2), (sx * 0.08, sy * 0.08, 0.68))
    p.cone("MetalDark", 0.17, 0.0, 0.775, 0.88, seg=4)
    p.sphere("MetalDark", 0.025, (0, 0, 0.895), u=8, v=4)
    return p.build(sharp_angle_deg=35.0, outline=True)


def make_candlestick():
    """World 3: a brass candlestick with a lit wax candle."""
    p = Piece("candlestick")
    p.lathe([(0.0, 0.14), (0.03, 0.13), (0.06, 0.05), (0.28, 0.03), (0.3, 0.075), (0.32, 0.075)],
            ["Brass"] * 5, top="Brass", seg=20)
    p.lathe([(0.32, 0.036), (0.62, 0.036)], ["Wax"], seg=16)
    p.cone("Flame", 0.026, 0.0, 0.63, 0.72, seg=10)
    p.sphere("Flame", 0.026, (0, 0, 0.645), u=10, v=5)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_hand_mirror():
    """World 3: a hand mirror lying face up: brass oval frame and handle, pale glass."""
    p = Piece("hand_mirror")
    oval = Matrix.Diagonal((1.0, 1.25, 1.0, 1.0))
    p.disc("Brass", 0.2, 0.03, (0, 0.12, 0.015), "z", seg=24)
    p.disc("MirrorGlass", 0.17, 0.034, (0, 0.12, 0.017), "z", seg=24)
    bmesh.ops.transform(p.bm, matrix=Matrix.Translation((0, 0.12, 0)) @ oval @ Matrix.Translation((0, -0.12, 0)),
                        verts=list(p.bm.verts))
    p.box("Brass", (0.065, 0.34, 0.03), (0, -0.27, 0.015), bevel=0.008)
    p.sphere("Brass", 0.045, (0, -0.46, 0.03), u=10, v=5)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_paper_lantern():
    """World 4: a round red paper lantern standing on a gold cap, gold top and dark ribs."""
    p = Piece("paper_lantern")
    p.lathe([(0.0, 0.08), (0.05, 0.08)], ["Gold"], seg=16)
    p.lathe([(0.05, 0.1), (0.12, 0.18), (0.24, 0.215), (0.36, 0.18), (0.43, 0.1)], ["LanternPaper"] * 4, seg=16)
    p.lathe([(0.43, 0.08), (0.48, 0.07)], ["Gold"], seg=16)
    p.ring("Gold", 0.035, 0.01, (0, 0, 0.51), major_seg=12, minor_seg=6)
    for z, r in ((0.12, 0.18), (0.24, 0.215), (0.36, 0.18)):
        p.ring("PipBlack", r + 0.002, 0.007, (0, 0, z), major_seg=16, minor_seg=4,
               rot=Matrix.Rotation(math.radians(90), 3, 'X'))
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_tea_tray():
    """World 5: a round copper tray with three tulip tea glasses on small saucers."""
    p = Piece("tea_tray")
    p.lathe([(0.0, 0.4), (0.02, 0.42), (0.05, 0.44)], ["Copper", "Copper"], seg=32)
    for gx, gy in ((-0.17, -0.08), (0.17, -0.06), (0.0, 0.17)):
        p.lathe([(0.05, 0.07), (0.065, 0.09)], ["Ivory"], x=gx, y=gy, seg=16)
        p.lathe([(0.065, 0.035), (0.11, 0.05), (0.16, 0.032), (0.2, 0.045), (0.23, 0.055)],
                ["Tea", "Tea", "Glass", "Glass"], top="Tea", x=gx, y=gy, seg=16)
    return p.build(sharp_angle_deg=40.0, outline=True)


def make_nazar_charm():
    """World 5: a large evil-eye charm lying flat on the table with a gold loop (flat, unlike the upright bead)."""
    p = Piece("nazar_charm")
    for r, depth, mat in ((0.22, 0.04, "BeadBlue"), (0.15, 0.044, "Ivory"), (0.1, 0.048, "BeadLight"), (0.05, 0.052, "PipBlack")):
        p.disc(mat, r, depth, (0, 0, 0.02), "z", seg=28)
    p.ring("Gold", 0.045, 0.012, (0, 0.26, 0.012), major_seg=14, minor_seg=6,
           rot=Matrix.Rotation(math.radians(90), 3, 'X'))
    return p.build(sharp_angle_deg=30.0, outline=True)


def export(obj, out_dir):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(out_dir, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
        add_leaf_bones=False, bake_anim=False, path_mode='STRIP', colors_type='LINEAR')
    tris = sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
    print("EXPORTED", obj.name, tris, "tris ->", path)


def main():
    out_dir = output_dir()
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for make in (make_pawn, make_die, make_coin, make_spike, make_tile_floor, make_tile_gap, make_rim_bar,
                 make_wall_cards, make_wall_chips, make_wall_box, make_mirror, make_portal_frame,
                 make_portal_disc, make_paw_print, make_mug, make_bead, make_tile_wood, make_card_fan,
                 make_rule_booklet, make_milk_saucer, make_yarn_ball, make_street_lantern, make_candlestick,
                 make_hand_mirror, make_paper_lantern, make_tea_tray, make_nazar_charm):
        export(make(), out_dir)


main()
