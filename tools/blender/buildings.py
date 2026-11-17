"""
Enterable buildings and interior furniture, shared by the camp and the town.

Each building exports:
  <name>_Floor        floor slab (the game reads its bounds to know when you are inside)
  <name>_Walls*       walls with a real door opening (collide)
  <name>_Roof_NOCOL   roof, hidden by the game while you are inside
  <name>_Interior     all furniture joined into one mesh (fewer draw calls on phones)
  DOOR_<name>         empty just outside the door; NPCs walk through it
"""
import random
import bpy
from common import mat, empty, box, cylinder, sphere, gable_roof

WALL_T = 0.2
DOOR_H = 2.4


def _join(objs, name):
    """Apply modifiers and join objects into one mesh (one object, a submesh per material)."""
    objs = [o for o in objs if o is not None]
    if not objs:
        return None
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.convert(target="MESH")
    if len(objs) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.select_set(False)
    return joined


def hollow_building(parent, name, cx, cy, lx, ly, h, wall, roof, floor, door_side="S", door_w=2.0,
                    roof_h=2.0, windows=True, door_offset=0.0):
    """A building you can walk into. Returns (group, interior_items_list, door_outside_xy)."""
    g = empty(name, (0, 0, 0), parent)
    box(name + "_Plinth", (lx + 0.4, ly + 0.4, 0.12), (cx, cy, 0.06), mat("Concrete", (178, 174, 166), 0.9), g)
    box(name + "_Floor", (lx, ly, 0.06), (cx, cy, 0.15), floor, g)

    hx, hy = lx / 2, ly / 2
    walls = []
    # side -> (wall length, centre of wall, along axis)
    for side in "NSEW":
        if side in "NS":
            length, wy = lx, cy + (hy - WALL_T / 2) * (1 if side == "N" else -1)
            if side == door_side:
                d0 = cx + door_offset - door_w / 2
                d1 = cx + door_offset + door_w / 2
                left = d0 - (cx - hx)
                right = (cx + hx) - d1
                walls.append(box(f"{name}_Walls{side}a", (left, WALL_T, h), (cx - hx + left / 2, wy, h / 2), wall, g))
                walls.append(box(f"{name}_Walls{side}b", (right, WALL_T, h), (d1 + right / 2, wy, h / 2), wall, g))
                walls.append(box(f"{name}_Walls{side}c", (door_w, WALL_T, h - DOOR_H), (cx + door_offset, wy, DOOR_H + (h - DOOR_H) / 2), wall, g))
            else:
                walls.append(box(f"{name}_Walls{side}", (length, WALL_T, h), (cx, wy, h / 2), wall, g))
        else:
            length, wx = ly, cx + (hx - WALL_T / 2) * (1 if side == "E" else -1)
            if side == door_side:
                d0 = cy + door_offset - door_w / 2
                d1 = cy + door_offset + door_w / 2
                low = d0 - (cy - hy)
                high = (cy + hy) - d1
                walls.append(box(f"{name}_Walls{side}a", (WALL_T, low, h), (wx, cy - hy + low / 2, h / 2), wall, g))
                walls.append(box(f"{name}_Walls{side}b", (WALL_T, high, h), (wx, d1 + high / 2, h / 2), wall, g))
                walls.append(box(f"{name}_Walls{side}c", (WALL_T, door_w, h - DOOR_H), (wx, cy + door_offset, DOOR_H + (h - DOOR_H) / 2), wall, g))
            else:
                walls.append(box(f"{name}_Walls{side}", (WALL_T, length, h), (wx, cy, h / 2), wall, g))

    along = "X" if lx >= ly else "Y"
    gable_roof(name + "_Roof_NOCOL", lx if along == "X" else ly, ly if along == "X" else lx, roof_h,
               (cx, cy, h), roof, g, along=along)

    deco = []
    win = mat("Window", (40, 52, 60), 0.3)
    if windows:
        step = 3.4
        for side in "NSEW":
            if side in "NS":
                n = int(lx // step) - 1
                y = cy + (hy - WALL_T / 2) * (1 if side == "N" else -1)
                for i in range(n):
                    x = cx - ((n - 1) * step) / 2 + i * step
                    if side == door_side and abs(x - (cx + door_offset)) < door_w / 2 + 0.9:
                        continue
                    deco.append(box(f"{name}_Win{side}{i}", (1.3, WALL_T + 0.05, 1.1), (x, y, h * 0.55), win, g))
            else:
                n = int(ly // step) - 1
                x = cx + (hx - WALL_T / 2) * (1 if side == "E" else -1)
                for i in range(n):
                    y = cy - ((n - 1) * step) / 2 + i * step
                    if side == door_side and abs(y - (cy + door_offset)) < door_w / 2 + 0.9:
                        continue
                    deco.append(box(f"{name}_Win{side}{i}", (WALL_T + 0.05, 1.3, 1.1), (x, y, h * 0.55), win, g))
    _join(deco, name + "_Windows_NOCOL")
    if deco:
        bpy.data.objects[name + "_Windows_NOCOL"].parent = g

    # Door marker just outside, used by NPCs
    out = {"S": (cx + door_offset, cy - hy - 1.8), "N": (cx + door_offset, cy + hy + 1.8),
           "E": (cx + hx + 1.8, cy + door_offset), "W": (cx - hx - 1.8, cy + door_offset)}[door_side]
    empty("DOOR_" + name, (out[0], out[1], 0), g)
    # An open door leaf swung against the inside wall
    door_mat = mat("Door", (92, 60, 38), 0.7)
    if door_side in "NS":
        sgn = 1 if door_side == "S" else -1
        y = cy + (-hy if door_side == "S" else hy) + sgn * (WALL_T + 0.55)
        box(name + "_DoorLeaf_NOCOL", (0.06, 1.0, 2.25), (cx + door_offset - door_w / 2 + 0.05, y, 1.3), door_mat, g)
    else:
        sgn = -1 if door_side == "E" else 1
        x = cx + (hx if door_side == "E" else -hx) + sgn * (WALL_T + 0.55)
        box(name + "_DoorLeaf_NOCOL", (1.0, 0.06, 2.25), (x, cy + door_offset - door_w / 2 + 0.05, 1.3), door_mat, g)
    return g


def finish_interior(group, name, items):
    """Join all furniture of a building into one collidable mesh."""
    obj = _join(items, name + "_Interior")
    if obj is not None:
        obj.parent = group
    return obj


# ---------------- Furniture ----------------
FZ = 0.18  # floor top


def bunk(items, x, y, rot90=False, mattress=None):
    """Double bunk bed: metal frame and two foam mattresses."""
    metal = mat("BunkMetal", (70, 90, 80), 0.5, 0.6)
    foam = mattress or mat("Mattress", (40, 90, 150), 0.9)
    lx, ly = (2.0, 0.95) if not rot90 else (0.95, 2.0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            items.append(cylinder("p", 0.03, 1.8, (x + sx * (lx / 2 - 0.03), y + sy * (ly / 2 - 0.03), FZ + 0.9), metal, segments=6))
    for z in (0.42, 1.38):
        items.append(box("f", (lx, ly, 0.05), (x, y, FZ + z), metal))
        items.append(box("m", (lx - 0.08, ly - 0.08, 0.14), (x, y, FZ + z + 0.1), foam))
    items.append(box("pillow", (0.25 if not rot90 else 0.6, 0.6 if not rot90 else 0.25, 0.08),
                     (x - (lx / 2 - 0.2) if not rot90 else x, y if not rot90 else y - (ly / 2 - 0.2), FZ + 0.6),
                     mat("Pillow", (235, 235, 230), 0.9)))


def plastic_chair(items, x, y, color=None, facing=1):
    c = color or mat("PlasticWhite", (235, 235, 232), 0.5)
    items.append(box("seat", (0.42, 0.42, 0.04), (x, y, FZ + 0.45), c))
    items.append(box("back", (0.42, 0.04, 0.42), (x, y - facing * 0.2, FZ + 0.68), c))
    for sx in (-0.18, 0.18):
        for sy in (-0.18, 0.18):
            items.append(box("leg", (0.035, 0.035, 0.45), (x + sx, y + sy, FZ + 0.22), c))


def table(items, x, y, lx, ly, h=0.78, top=None, legs=None):
    t = top or mat("Wood", (110, 74, 45), 0.8)
    l = legs or t
    items.append(box("top", (lx, ly, 0.05), (x, y, FZ + h), t))
    for sx in (-1, 1):
        for sy in (-1, 1):
            items.append(box("leg", (0.05, 0.05, h), (x + sx * (lx / 2 - 0.05), y + sy * (ly / 2 - 0.05), FZ + h / 2), l))


def cabinet(items, x, y, lx=0.5, ly=0.6, h=1.4, color=None):
    items.append(box("cab", (lx, ly, h), (x, y, FZ + h / 2), color or mat("MetalGrey", (120, 125, 128), 0.5, 0.4)))


def hospital_bed(items, x, y):
    metal = mat("BedMetal", (200, 205, 210), 0.4, 0.5)
    items.append(box("frame", (0.95, 2.0, 0.08), (x, y, FZ + 0.55), metal))
    items.append(box("sheet", (0.9, 1.95, 0.14), (x, y, FZ + 0.66), mat("Sheet", (240, 245, 250), 0.9)))
    items.append(box("head", (0.95, 0.05, 0.6), (x, y + 1.0, FZ + 0.85), metal))
    for sx in (-0.42, 0.42):
        for sy in (-0.92, 0.92):
            items.append(cylinder("leg", 0.025, 0.55, (x + sx, y + sy, FZ + 0.27), metal, segments=6))


def pew(items, x, y, length=3.0):
    w = mat("Wood", (110, 74, 45), 0.8)
    items.append(box("seat", (length, 0.45, 0.06), (x, y, FZ + 0.45), w))
    items.append(box("back", (length, 0.06, 0.5), (x, y + 0.22, FZ + 0.75), w))
    for sx in (-length / 2 + 0.1, length / 2 - 0.1):
        items.append(box("side", (0.06, 0.5, 0.9), (x + sx, y, FZ + 0.45), w))


def ceiling_fan(items, x, y, h):
    m = mat("MetalDark", (52, 54, 56), 0.5, 0.7)
    items.append(cylinder("rod", 0.02, 0.6, (x, y, h - 0.3), m, segments=6))
    for i in range(3):
        import math
        a = i * 2.094
        items.append(box("blade", (0.7, 0.12, 0.02), (x + math.cos(a) * 0.38, y + math.sin(a) * 0.38, h - 0.62), m,
                         rot_z=a))
