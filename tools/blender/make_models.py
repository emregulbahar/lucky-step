"""Lucky Step toy-piece generator (Story 005, visual slice).

Builds the low-poly painted-wood board pieces and exports one FBX per piece:
die, coin, spike, tile_floor, tile_gap, rim_bar, wall_cards, wall_chips, wall_box, mirror,
portal_frame, portal_disc, paw_print (and pawn, kept but no longer used by the game).

Run headless (Blender 5.1):
    blender.exe --background --python tools/blender/make_models.py [-- <output_dir>]

Default output: <repo>/Assets/Art/Models. Units: 1 Blender metre = 1 Unity unit = 1 board tile.
Material slot names are the contract with Unity: Assets/Editor/BuildBoardScene.cs remaps each slot
name to a painted-wood URP/Lit material, so the colours here are only for Blender previews.
Origins: tiles have their TOP face at z=0; standing pieces have their BOTTOM at z=0; the coin is centred.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

SEG = 20  # radial segments for turned pieces

PREVIEW_COLOURS = {
    "PawnPaint": (0.80, 0.18, 0.15),
    "Ivory": (0.96, 0.93, 0.85),
    "PipBlack": (0.06, 0.06, 0.06),
    "Gold": (0.95, 0.75, 0.25),
    "Metal": (0.62, 0.66, 0.68),
    "SpikeBase": (0.55, 0.36, 0.22),
    "Tile": (0.90, 0.68, 0.45),
    "Hole": (0.04, 0.03, 0.03),
    "Rim": (0.40, 0.25, 0.15),
    "CardFace": (0.95, 0.94, 0.90),
    "CardBack": (0.65, 0.12, 0.12),
    "ChipRed": (0.75, 0.12, 0.10),
    "ChipWhite": (0.92, 0.90, 0.85),
    "ChipBlue": (0.15, 0.28, 0.62),
    "BoxBody": (0.20, 0.32, 0.55),
    "BoxLid": (0.90, 0.72, 0.25),
    "MirrorFrame": (0.45, 0.30, 0.20),
    "MirrorGlass": (0.75, 0.85, 0.95),
    "MirrorCrack": (0.10, 0.10, 0.12),
    "PortalFrame": (0.55, 0.36, 0.22),
    "PortalStone": (0.30, 0.25, 0.45),
    "PortalSwirlA": (0.55, 0.35, 1.00),
    "PortalSwirlB": (0.25, 0.85, 1.00),
    "PawPrint": (0.22, 0.14, 0.09),
}


def output_dir():
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        if args:
            return os.path.abspath(args[0])
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    return os.path.join(root, "Assets", "Art", "Models")


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

    def ring(self, mat_name, major, minor, centre, major_seg=28, minor_seg=8):
        """Torus standing upright in the XZ plane (axis along Y)."""
        before = set(self.bm.faces)
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
        self._tag(before, mat_name)

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

    def build(self, sharp_angle_deg=35.0):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        self.bm.free()
        for slot in self.slots:
            mesh.materials.append(material(slot))
        mesh.shade_smooth()
        mesh.set_sharp_from_angle(angle=math.radians(sharp_angle_deg))
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
    return p.build()


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
    return p.build(sharp_angle_deg=40.0)


def make_coin():
    p = Piece("coin")
    p.disc("Gold", 0.16, 0.035, (0, 0, 0), "z", seg=24)
    p.disc("Gold", 0.115, 0.047, (0, 0, 0), "z", seg=24)
    return p.build(sharp_angle_deg=30.0)


def make_spike():
    p = Piece("spike")
    p.box("SpikeBase", (0.74, 0.74, 0.05), (0, 0, 0.025), bevel=0.012)
    for x, y in [(0, 0), (-0.2, -0.2), (0.2, -0.2), (-0.2, 0.2), (0.2, 0.2)]:
        p.cone("Metal", 0.085, 0.0, 0.05, 0.34, x, y, seg=8)
    return p.build(sharp_angle_deg=30.0)


def make_tile_floor():
    p = Piece("tile_floor")
    p.box("Tile", (0.96, 0.96, 0.12), (0, 0, -0.06), bevel=0.025)
    return p.build()


def make_rim_bar():
    """Unit bar (1 x 1 x 1, bottom at z=0); BoardView scales it into the board's raised edge."""
    p = Piece("rim_bar")
    p.box("Rim", (1.0, 1.0, 1.0), (0, 0, 0.5), bevel=0.06)
    return p.build()


def make_wall_cards():
    """Blocking piece: a squared-up stack of playing cards with a few loose cards on top."""
    p = Piece("wall_cards")
    p.box("CardFace", (0.56, 0.8, 0.3), (0, 0, 0.15), bevel=0.01)
    for i, (ang, dx) in enumerate([(8, 0.02), (-12, -0.03), (21, 0.04)]):
        z = 0.3 + 0.006 + i * 0.012
        p.box("CardFace", (0.56, 0.8, 0.01), (dx, 0, z), rot=Matrix.Rotation(math.radians(ang), 4, 'Z'))
    p.box("CardBack", (0.5, 0.74, 0.004), (0.04, 0, 0.3 + 0.006 + 2 * 0.012 + 0.007),
          rot=Matrix.Rotation(math.radians(21), 4, 'Z'))
    return p.build(sharp_angle_deg=30.0)


def make_wall_chips():
    """Blocking piece: three stacks of poker chips."""
    p = Piece("wall_chips")
    colours = ["ChipRed", "ChipWhite", "ChipBlue"]
    stacks = [(-0.17, -0.14, 9), (0.18, -0.1, 7), (0.0, 0.2, 11)]
    for s, (x, y, n) in enumerate(stacks):
        for i in range(n):
            mat = colours[(i + s) % 3]
            p.cone(mat, 0.16, 0.16, i * 0.045, i * 0.045 + 0.042, x + 0.006 * ((i * 7) % 3 - 1), y, seg=16)
    return p.build(sharp_angle_deg=40.0)


def make_wall_box():
    """Blocking piece: a small closed board-game box with a lid."""
    p = Piece("wall_box")
    p.box("BoxBody", (0.78, 0.78, 0.26), (0, 0, 0.13), bevel=0.015)
    p.box("BoxLid", (0.82, 0.82, 0.1), (0, 0, 0.26 + 0.03), bevel=0.02)
    p.box("BoxBody", (0.4, 0.4, 0.006), (0, 0, 0.34 + 0.003))
    return p.build(sharp_angle_deg=30.0)


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
    return p.build(sharp_angle_deg=30.0)


def make_portal_frame():
    """Exit portal: wooden plinth and an upright ring; the swirl disc is a separate model so it can spin."""
    p = Piece("portal_frame")
    p.box("PortalFrame", (0.8, 0.36, 0.08), (0, 0, 0.04), bevel=0.02)
    p.box("PortalFrame", (0.12, 0.2, 0.2), (-0.3, 0, 0.18), bevel=0.02)
    p.box("PortalFrame", (0.12, 0.2, 0.2), (0.3, 0, 0.18), bevel=0.02)
    p.ring("PortalStone", 0.34, 0.055, (0, 0, 0.47))
    return p.build(sharp_angle_deg=40.0)


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
    p.swirl_disc(["PortalSwirlA", "PortalSwirlB"], 0.3, (0, 0, 0))
    return p.build(sharp_angle_deg=10.0)


def make_tile_gap():
    p = Piece("tile_gap")
    outer, inner, t = 0.96, 0.66, 0.12
    rim = (outer - inner) * 0.5
    off = (inner + rim) * 0.5
    p.box("Tile", (outer, rim, t), (0, off, -t * 0.5), bevel=0.02)
    p.box("Tile", (outer, rim, t), (0, -off, -t * 0.5), bevel=0.02)
    p.box("Tile", (rim, inner, t), (off, 0, -t * 0.5), bevel=0.02)
    p.box("Tile", (rim, inner, t), (-off, 0, -t * 0.5), bevel=0.02)
    # Dark pit floor just above the board plate (plate top is at z = -0.12).
    p.box("Hole", (inner, inner, 0.01), (0, 0, -0.105))
    return p.build()


def export(obj, out_dir):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(out_dir, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
        add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    tris = sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
    print("EXPORTED", obj.name, tris, "tris ->", path)


def main():
    out_dir = output_dir()
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for make in (make_pawn, make_die, make_coin, make_spike, make_tile_floor, make_tile_gap, make_rim_bar,
                 make_wall_cards, make_wall_chips, make_wall_box, make_mirror, make_portal_frame,
                 make_portal_disc, make_paw_print):
        export(make(), out_dir)


main()
