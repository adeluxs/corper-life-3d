"""
Builds the service-year town and exports Assets/Resources/Models/town.fbx.
Run:  python3 tools/blender/build_town.py [--render]

A Nigerian state-capital neighbourhood: main road with danfos and kekes, the corpers' lodge,
four kinds of PPA (school, tech company, general hospital, local government secretariat),
the LGI office for monthly clearance, a CDS community hall, buka, bank, market,
motor park, mosque, church and houses. All key buildings can be entered.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
from common import (reset_scene, mat, empty, box, cylinder, sphere, gable_roof, text_mesh,
                    export_fbx, stats, setup_render, render)
from buildings import (hollow_building, finish_interior, bunk, plastic_chair, table, cabinet, hospital_bed,
                       pew, ceiling_fan, FZ)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Models", "town.fbx")
random.seed(7)

reset_scene()

M = {
    "ground": mat("Laterite", (166, 104, 70), 0.95),
    "grass": mat("Grass", (104, 132, 64), 0.95),
    "asphalt": mat("Asphalt", (58, 58, 60), 0.9),
    "kerb": mat("Kerb", (200, 196, 186), 0.85),
    "line_yellow": mat("LineYellow", (230, 190, 40), 0.7),
    "line_white": mat("LineWhite", (240, 240, 235), 0.8),
    "concrete": mat("Concrete", (178, 174, 166), 0.9),
    "paint_white": mat("PaintWhite", (232, 228, 214), 0.85),
    "paint_cream": mat("PaintCream", (226, 211, 170), 0.85),
    "paint_peach": mat("PaintPeach", (230, 180, 150), 0.85),
    "paint_blue": mat("PaintBlue", (60, 110, 160), 0.8),
    "paint_skyblue": mat("PaintSkyBlue", (150, 195, 220), 0.85),
    "paint_green": mat("PaintGreen", (34, 120, 72), 0.8),
    "paint_yellow": mat("PaintYellow", (230, 200, 90), 0.85),
    "paint_grey": mat("PaintGrey", (150, 152, 155), 0.8),
    "glass": mat("Glass", (60, 90, 110), 0.15, 0.3),
    "zinc": mat("Zinc", (150, 155, 158), 0.45, 0.6),
    "zinc_rust": mat("ZincRust", (140, 92, 64), 0.7, 0.3),
    "roof_red": mat("RoofRed", (150, 60, 45), 0.7),
    "roof_blue": mat("RoofBlue", (50, 80, 130), 0.6, 0.3),
    "wood": mat("Wood", (110, 74, 45), 0.8),
    "metal_dark": mat("MetalDark", (52, 54, 56), 0.5, 0.7),
    "red": mat("Red", (190, 40, 40), 0.7),
    "danfo": mat("DanfoYellow", (240, 190, 20), 0.5),
    "black": mat("Black", (25, 25, 25), 0.6),
    "tyre": mat("Tyre", (20, 20, 20), 0.9),
    "window": mat("Window", (40, 52, 60), 0.3),
    "floor_tile": mat("FloorTile", (205, 200, 188), 0.4),
    "floor_cement": mat("FloorCement", (150, 146, 140), 0.85),
    "blackboard": mat("Blackboard", (30, 60, 45), 0.8),
    "monitor": mat("Monitor", (20, 22, 26), 0.3),
    "plant": mat("Leaf", (58, 104, 46), 0.9),
    "leaf2": mat("LeafDark", (44, 86, 40), 0.9),
    "trunk": mat("Trunk", (84, 64, 48), 0.9),
    "plastic_blue": mat("PlasticBlue", (40, 90, 170), 0.6),
    "plastic_red": mat("PlasticRed", (180, 40, 40), 0.6),
    "mattress": mat("MattressBlue", (40, 90, 150), 0.9),
    "mattress2": mat("MattressFloral", (180, 70, 90), 0.9),
    "fence": mat("FenceWall", (200, 190, 160), 0.85),
    "sign_green": mat("SignGreen", (0, 120, 70), 0.7),
}
CANOPY = [mat("CanopyRed", (196, 52, 46)), mat("CanopyYellow", (230, 182, 40)), mat("CanopyBlue", (40, 104, 180)),
          mat("CanopyGreen", (40, 150, 90)), mat("CanopyOrange", (226, 120, 40))]
HOUSE_COLS = [M["paint_cream"], M["paint_peach"], M["paint_skyblue"], M["paint_yellow"], M["paint_white"]]

town = empty("Town")
_loc_count = [0]


def loc_box(lid, x, y, hx, hy):
    _loc_count[0] += 1
    return empty(f"LOC_{lid}__b{float(hx)}x{float(hy)}__i{_loc_count[0]}", (x, y, 0), town)


def loc_circle(lid, x, y, r):
    _loc_count[0] += 1
    return empty(f"LOC_{lid}__r{int(r)}__i{_loc_count[0]}", (x, y, 0), town)


def sign(text, x, y, z, size, color, rot_z=0.0, board=None):
    if board is not None:
        w = len(text) * size * 0.62 + 0.6
        box("SignBoard_NOCOL", (w if abs(math.sin(rot_z)) < 0.5 else 0.12, 0.12 if abs(math.sin(rot_z)) < 0.5 else w, size * 1.6),
            (x, y + (0.08 if abs(math.sin(rot_z)) < 0.5 else 0), z), board, town)
    text_mesh("Sign_NOCOL", text, (x, y - 0.02 if abs(math.sin(rot_z)) < 0.5 else y, z), size, color, town,
              rot=(math.pi / 2, 0, rot_z))


def solid_house(name, x, y, lx, ly, wall, roof):
    g = empty(name, (0, 0, 0), town)
    box(name + "_Walls", (lx, ly, 3.2), (x, y, 1.6), wall, g)
    gable_roof(name + "_Roof_NOCOL", max(lx, ly), min(lx, ly), 1.6, (x, y, 3.2), roof, g,
               along="X" if lx >= ly else "Y")
    box(name + "_Door_NOCOL", (1.2, 0.06, 2.2), (x, y - ly / 2 - 0.03, 1.1), M["wood"], g)
    for sx in (-lx / 3, lx / 3):
        box(name + "_Win_NOCOL", (1.2, 0.06, 1.0), (x + sx, y - ly / 2 - 0.03, 1.7), M["window"], g)


def tree(name, x, y, s=1.0):
    h = random.uniform(3.2, 4.6) * s
    cylinder(name + "_Trunk", 0.22 * s, h, (x, y, h / 2), M["trunk"], town, segments=8, radius_top=0.15 * s)
    for i in range(3):
        sphere(f"{name}_Leaf{i}_NOCOL", random.uniform(1.4, 2.1) * s,
               (x + random.uniform(-1, 1) * s, y + random.uniform(-1, 1) * s, h + random.uniform(0.2, 1.2) * s),
               M["plant"] if i % 2 else M["leaf2"], town, scale=(1, 1, 0.75), subdiv=1)


def danfo(name, x, y, rot=0.0):
    """Yellow commercial minibus with black stripes."""
    lx, ly = (2.0, 4.8) if rot == 0 else (4.8, 2.0)
    box(name, (lx, ly, 1.9), (x, y, 1.35), M["danfo"], town, bevel=0.12)
    if rot == 0:
        for sx in (-1, 1):
            box(name + "_Stripe_NOCOL", (0.02, ly - 0.2, 0.18), (x + sx * 1.01, y, 1.1), M["black"], town)
            box(name + "_Win_NOCOL", (0.02, ly - 1.0, 0.6), (x + sx * 1.01, y - 0.2, 1.85), M["glass"], town)
        box(name + "_Wind_NOCOL", (1.8, 0.02, 0.7), (x, y + 2.41, 1.8), M["glass"], town)
        for sy in (-1.6, 1.6):
            for sx in (-0.95, 0.95):
                cylinder(name + "_Tyre_NOCOL", 0.38, 0.25, (x + sx, y + sy, 0.38), M["tyre"], town, segments=12, axis="X")
    else:
        for sy in (-1, 1):
            box(name + "_Stripe_NOCOL", (lx - 0.2, 0.02, 0.18), (x, y + sy * 1.01, 1.1), M["black"], town)
            box(name + "_Win_NOCOL", (lx - 1.0, 0.02, 0.6), (x - 0.2, y + sy * 1.01, 1.85), M["glass"], town)
        box(name + "_Wind_NOCOL", (0.02, 1.8, 0.7), (x + 2.41, y, 1.8), M["glass"], town)
        for sx in (-1.6, 1.6):
            for sy in (-0.95, 0.95):
                cylinder(name + "_Tyre_NOCOL", 0.38, 0.25, (x + sx, y + sy, 0.38), M["tyre"], town, segments=12, axis="Y")


def keke(name, x, y):
    """Tricycle taxi (keke napep)."""
    box(name, (1.3, 2.4, 1.5), (x, y, 1.05), M["danfo"], town, bevel=0.15)
    box(name + "_Roof_NOCOL", (1.4, 2.0, 0.08), (x, y - 0.1, 1.85), M["black"], town)
    box(name + "_Wind_NOCOL", (1.1, 0.02, 0.6), (x, y + 1.21, 1.4), M["glass"], town)
    for sx, sy in ((0, 1.0), (-0.6, -0.8), (0.6, -0.8)):
        cylinder(name + "_Tyre_NOCOL", 0.28, 0.18, (x + sx, y + sy, 0.28), M["tyre"], town, segments=10, axis="X")


def stall(name, x, y, canopy):
    for sx in (-2, 2):
        for sy in (-1.5, 1.5):
            cylinder(f"{name}_Post_NOCOL", 0.05, 2.6, (x + sx, y + sy, 1.3), M["wood"], town, segments=6)
    gable_roof(f"{name}_Roof_NOCOL", 4.4, 3.4, 0.5, (x, y, 2.6), canopy, town, overhang=0.2)
    box(f"{name}_Table", (3.6, 1.0, 0.85), (x, y - 0.8, 0.43), M["wood"], town)
    for k in range(3):
        box(f"{name}_Goods_NOCOL", (0.5, 0.4, 0.3), (x - 1.1 + k * 1.1, y - 0.8, 1.0), random.choice(CANOPY), town)


# ================= Ground and roads =================
box("Ground", (300, 260, 0.2), (0, 0, -0.1), M["ground"], town)
for i in range(18):
    x, y = random.uniform(-140, 140), random.uniform(-120, 120)
    if abs(y) < 14 or abs(x + 50) < 8 or abs(x - 60) < 8:
        continue
    box(f"GrassPatch{i}_NOCOL", (random.uniform(8, 20), random.uniform(6, 14), 0.04), (x, y, 0.02), M["grass"], town)

box("RoadMain_NOCOL", (290, 10, 0.06), (0, 0, 0.03), M["asphalt"], town)
box("RoadWest_NOCOL", (8, 240, 0.06), (-50, 0, 0.032), M["asphalt"], town)
box("RoadEast_NOCOL", (8, 240, 0.06), (60, 0, 0.032), M["asphalt"], town)
for i in range(-14, 15):
    if abs(i * 10 + 50) < 6 or abs(i * 10 - 60) < 6:
        continue
    box(f"RoadLine{i}_NOCOL", (5, 0.18, 0.01), (i * 10, 0, 0.065), M["line_yellow"], town)
for sx in (-1, 1):
    box(f"KerbMain{sx}_NOCOL", (290, 0.4, 0.18), (0, sx * 5.2, 0.09), M["kerb"], town)
    box(f"Walk{sx}_NOCOL", (290, 2.5, 0.08), (0, sx * 6.7, 0.04), M["concrete"], town)

# Electric poles with wires along the main road (a familiar Nigerian sight)
for i in range(-6, 7):
    x = i * 22 + 5
    cylinder(f"ElecPole{i}", 0.12, 9, (x, 8.4, 4.5), M["wood"], town, segments=8)
    box(f"ElecArm{i}_NOCOL", (0.1, 1.6, 0.1), (x, 8.4, 8.6), M["wood"], town)
box("Wire1_NOCOL", (290, 0.02, 0.02), (0, 7.8, 8.6), M["black"], town)
box("Wire2_NOCOL", (290, 0.02, 0.02), (0, 9.0, 8.6), M["black"], town)

# Street locations for wandering
for i, x in enumerate((-110, -75, -20, 20, 90, 120)):
    loc_circle("street", x, 7 if i % 2 else -7, 6)

# ================= North side =================
# --- Local Government Secretariat (PPA) ---
lga = hollow_building(town, "LGASecretariat", -95, 32, 32, 16, 4.5, M["paint_cream"], M["roof_red"], M["floor_tile"],
                      door_side="S", door_w=2.6, roof_h=2.2)
items = []
items.append(box("counter", (10, 0.8, 1.05), (-95, 28, FZ + 0.52), M["wood"]))
for k in range(6):
    x = -108 + k * 5
    table(items, x, 35, 1.6, 0.8)
    plastic_chair(items, x, 35.8, facing=1)
    items.append(box("files", (0.6, 0.4, 0.3), (x - 0.3, 35, FZ + 0.93), M["paint_cream"]))
    cabinet(items, x + 1.5, 38.8)
for x in (-104, -95, -86):
    ceiling_fan(items, x, 32, 4.3)
finish_interior(lga, "LGASecretariat", items)
sign("LOCAL GOVERNMENT SECRETARIAT", -95, 23.85, 3.7, 0.7, M["paint_white"], board=M["sign_green"])
loc_box("ppa_lga", -95, 32, 15.5, 7.5)

# --- LGI office (monthly clearance) ---
lgi = hollow_building(town, "LGIOffice", -64, 28, 12, 9, 3.6, M["paint_green"], M["zinc"], M["floor_tile"],
                      door_side="S", door_w=2.0, roof_h=1.4)
items = []
table(items, -64, 30, 2.4, 1.0)
items.append(box("register", (0.6, 0.45, 0.06), (-64, 30, FZ + 0.84), M["red"]))
plastic_chair(items, -64, 31.2, facing=1)
for k in range(5):
    plastic_chair(items, -68 + k * 1.1, 25.5, color=M["plastic_blue"], facing=-1)
cabinet(items, -59, 31.5)
finish_interior(lgi, "LGIOffice", items)
sign("NYSC LGI OFFICE", -64, 23.35, 3.0, 0.5, M["paint_white"])
loc_box("lgi", -64, 28, 5.5, 4.0)

# --- General Hospital (PPA) ---
hos = hollow_building(town, "Hospital", -12, 38, 36, 16, 4.2, M["paint_white"], M["roof_blue"], M["floor_tile"],
                      door_side="S", door_w=3.0, roof_h=1.8)
items = []
for k in range(8):
    hospital_bed(items, -26 + k * 3.6, 43.5)
items.append(box("station", (6, 1.0, 1.05), (-12, 33, FZ + 0.52), M["paint_white"]))
for k in range(6):
    plastic_chair(items, -27 + k * 1.1, 32, color=M["plastic_blue"], facing=-1)
cabinet(items, -2, 31.5, color=M["paint_white"])
finish_interior(hos, "Hospital", items)
box("HospCrossV_NOCOL", (0.5, 0.1, 1.8), (6.5, 29.88, 3.0), M["red"], town)
box("HospCrossH_NOCOL", (1.8, 0.1, 0.5), (6.5, 29.88, 3.0), M["red"], town)
sign("GENERAL HOSPITAL", -12, 29.85, 3.4, 0.7, M["red"])
loc_box("ppa_hospital", -12, 38, 17.5, 7.5)

# --- Tech company office (PPA) ---
tec = hollow_building(town, "TechHub", 30, 30, 22, 13, 4.0, M["paint_grey"], M["metal_dark"], M["floor_tile"],
                      door_side="S", door_w=2.4, roof_h=0.5)
for k in range(6):  # big glass front panels
    if abs(-8 + k * 3.2) < 2.2:
        continue
    box(f"TechGlass{k}_NOCOL", (2.6, 0.24, 2.6), (30 - 8 + k * 3.2, 23.6, 1.6), M["glass"], town)
items = []
for r in range(2):
    for c in range(4):
        x, y = 22 + c * 5, 31 + r * 4
        table(items, x, y, 2.4, 1.0, top=M["paint_white"], legs=M["metal_dark"])
        for sx in (-0.6, 0.6):
            items.append(box("monitor", (0.6, 0.06, 0.4), (x + sx, y + 0.25, FZ + 1.05), M["monitor"]))
            plastic_chair(items, x + sx, y - 0.8, color=M["black"], facing=1)
for x in (19.5, 40.5):
    items.append(cylinder("pot", 0.3, 0.5, (x, 25, FZ + 0.25), M["paint_white"], segments=10))
    items.append(sphere("plant", 0.5, (x, 25, FZ + 0.9), M["plant"], subdiv=1))
finish_interior(tec, "TechHub", items)
sign("TECHHUB LIMITED", 30, 23.45, 3.4, 0.7, M["paint_white"], board=M["black"])
loc_box("ppa_office", 30, 30, 10.5, 6.0)

# --- CDS community hall ---
cds = hollow_building(town, "CommunityHall", 98, 34, 24, 14, 4.5, M["paint_skyblue"], M["zinc"], M["floor_cement"],
                      door_side="S", door_w=2.6, roof_h=2.0)
items = []
table(items, 98, 39.5, 4, 1.0, top=M["paint_white"])
for r in range(5):
    for c in range(-5, 6):
        if c == 0:
            continue
        plastic_chair(items, 98 + c * 1.0, 30 + r * 1.3, color=M["plastic_red"] if r % 2 else None, facing=-1)
finish_interior(cds, "CommunityHall", items)
sign("COMMUNITY HALL", 98, 26.85, 3.6, 0.7, M["paint_white"], board=M["paint_blue"])
loc_box("cds", 98, 34, 11.5, 6.5)

# --- Worship ---
box("TownMosque_Walls", (12, 12, 4.5), (95, 78, 2.25), M["paint_white"], town)
sphere("TownMosque_Dome_NOCOL", 4.4, (95, 78, 4.6), M["paint_green"], town, scale=(1, 1, 0.85), subdiv=3)
cylinder("TownMinaret", 0.8, 12, (102, 84, 6), M["paint_white"], town, segments=12)
box("TownChurch_Walls", (10, 16, 5), (70, 80, 2.5), M["paint_white"], town)
gable_roof("TownChurch_Roof_NOCOL", 16, 10, 3.5, (70, 80, 5), M["roof_red"], town, along="Y")
box("TownChurchCross_NOCOL", (1.4, 0.25, 0.25), (70, 71.7, 9.5), M["paint_white"], town)
box("TownChurchCrossV_NOCOL", (0.25, 0.25, 2.4), (70, 71.7, 9.0), M["paint_white"], town)
loc_circle("worship", 95, 68, 7)
loc_circle("worship", 70, 69, 6)

# ================= South side =================
# --- Market ---
for row in range(3):
    for col in range(6):
        stall(f"Stall{row}{col}", -118 + col * 5.2, -24 - row * 7, CANOPY[(row + col) % len(CANOPY)])
loc_circle("market", -105, -31, 16)

# --- Buka (local restaurant) ---
buk = hollow_building(town, "Buka", -22, -24, 12, 9, 3.4, M["paint_yellow"], M["zinc_rust"], M["floor_cement"],
                      door_side="N", door_w=2.0, roof_h=1.2)
items = []
items.append(box("counter", (6, 0.8, 1.0), (-22, -27.5, FZ + 0.5), M["wood"]))
for k in range(3):
    items.append(cylinder("pot", 0.4, 0.5, (-24 + k * 2, -27.6, FZ + 1.25), M["metal_dark"], segments=12))
for k in range(3):
    table(items, -26 + k * 4, -22.5, 1.4, 0.9, top=M["plastic_red"])
    plastic_chair(items, -26 + k * 4 - 0.8, -22.5, color=M["plastic_red"])
    plastic_chair(items, -26 + k * 4 + 0.8, -22.5, color=M["plastic_red"])
finish_interior(buk, "Buka", items)
sign("MAMA T BUKA", -22, -19.45, 2.9, 0.5, M["red"], board=M["paint_white"])
loc_box("buka", -22, -24, 5.5, 4.0)

# --- Bank ---
bnk = hollow_building(town, "Bank", 4, -26, 14, 10, 4.0, M["paint_white"], M["roof_blue"], M["floor_tile"],
                      door_side="N", door_w=2.0, roof_h=0.6)
items = []
items.append(box("counter", (10, 0.8, 1.15), (4, -29, FZ + 0.57), M["wood"]))
items.append(box("glass", (10, 0.05, 0.9), (4, -29.3, FZ + 1.6), M["glass"]))
for k in range(5):
    plastic_chair(items, -1 + k * 1.1, -23.5, color=M["plastic_blue"], facing=-1)
finish_interior(bnk, "Bank", items)
box("ATM", (1.0, 0.8, 1.8), (13.5, -20.5, 0.9), M["paint_blue"], town)
box("ATMScreen_NOCOL", (0.6, 0.05, 0.4), (13.5, -20.08, 1.35), M["monitor"], town)
sign("FIRSTCITY BANK", 4, -20.95, 3.4, 0.6, M["paint_white"], board=M["paint_blue"])
loc_box("bank", 4, -26, 6.5, 4.5)
loc_circle("bank", 13.5, -18.5, 3)

# --- Corpers' lodge (compound with fence and gate) ---
for name, size, loc in [("LodgeFenceS_W", (9, 0.25, 2.0), (17.5, -55, 1.0)), ("LodgeFenceS_E", (9, 0.25, 2.0), (42.5, -55, 1.0)),
                        ("LodgeFenceN", (34, 0.25, 2.0), (30, -88, 1.0)), ("LodgeFenceW", (0.25, 33, 2.0), (13, -71.5, 1.0)),
                        ("LodgeFenceE", (0.25, 33, 2.0), (47, -71.5, 1.0))]:
    box(name, size, loc, M["fence"], town)
lod = hollow_building(town, "Lodge", 30, -72, 24, 11, 3.4, M["paint_cream"], M["zinc_rust"], M["floor_cement"],
                      door_side="N", door_w=2.0, roof_h=1.6)
items = []
# Partition walls make four rooms off a corridor along the north side
for k, x in enumerate((24, 30, 36)):
    items.append(box("partition", (0.15, 6.5, 3.2), (x, -74.2, FZ + 1.6), M["paint_cream"]))
items.append(box("corridorwall_a", (4.5, 0.15, 3.2), (20.5, -70.9, FZ + 1.6), M["paint_cream"]))
items.append(box("corridorwall_b", (3.5, 0.15, 3.2), (27.25, -70.9, FZ + 1.6), M["paint_cream"]))
items.append(box("corridorwall_c", (3.5, 0.15, 3.2), (33.25, -70.9, FZ + 1.6), M["paint_cream"]))
items.append(box("corridorwall_d", (4.5, 0.15, 3.2), (39.5, -70.9, FZ + 1.6), M["paint_cream"]))
for k, x in enumerate((21, 27, 33, 39)):
    items.append(box("mattress", (1.4, 2.0, 0.22), (x - 1.2, -75.5, FZ + 0.11), M["mattress"] if k % 2 else M["mattress2"]))
    items.append(box("box", (0.6, 0.4, 0.35), (x + 1.3, -76.6, FZ + 0.18), M["black"]))
    items.append(box("bucket", (0.3, 0.3, 0.35), (x + 1.6, -72.2, FZ + 0.18), M["plastic_blue"]))
    items.append(box("lamp", (0.15, 0.15, 0.3), (x - 0.2, -76.8, FZ + 0.15), M["paint_white"]))
finish_interior(lod, "Lodge", items)
box("LodgeGenerator", (1.0, 0.7, 0.8), (44, -60, 0.4), M["red"], town)
for k in range(3):
    cylinder(f"LodgeTank{k}", 0.3, 0.6, (16 + k * 0.7, -58, 0.3), M["paint_white"], town, segments=10)
sign("CORPERS LODGE", 30, -54.8, 2.4, 0.5, M["sign_green"])
loc_box("lodge", 30, -72, 11.5, 5.0)
spawn = empty("SPAWN_lodge", (30, -64, 0), town)

# --- Government Secondary School (PPA) ---
sch = hollow_building(town, "School", -30, -78, 32, 11, 3.6, M["paint_yellow"], M["zinc"], M["floor_cement"],
                      door_side="N", door_w=2.2, roof_h=1.6)
items = []
items.append(box("blackboard", (6, 0.1, 1.6), (-30, -83.3, FZ + 1.8), M["blackboard"]))
table(items, -30, -81.5, 1.6, 0.8)
for r in range(4):
    for c in range(8):
        x, y = -42 + c * 3.2, -79.5 + r * 1.6
        if abs(x + 30) < 1.4:
            continue
        table(items, x, y, 1.2, 0.5, h=0.7)
        items.append(box("bench", (1.2, 0.3, 0.05), (x, y + 0.5, FZ + 0.42), M["wood"]))
finish_interior(sch, "School", items)
sign("GOVT. SECONDARY SCHOOL", -30, -72.35, 3.0, 0.6, M["paint_white"], board=M["sign_green"])
box("SchoolField_NOCOL", (30, 16, 0.04), (-30, -100, 0.02), M["grass"], town)
for gx in (-44.5, -15.5):
    cylinder(f"SchoolGoal{gx}", 0.06, 6, (gx, -100, 2.0), M["paint_white"], town, segments=8, axis="Y")
loc_box("ppa_school", -30, -78, 15.5, 5.0)

# --- Motor park ---
box("ParkSlab_NOCOL", (40, 26, 0.05), (100, -40, 0.03), M["concrete"], town)
for i in range(4):
    danfo(f"Danfo{i}", 86 + i * 5, -36)
for i in range(3):
    keke(f"Keke{i}", 92 + i * 3.5, -48)
box("ParkShed_NOCOL", (12, 4, 0.1), (112, -48, 3.2), M["zinc"], town)
for sx in (106.5, 117.5):
    cylinder(f"ParkShedPost{sx}", 0.1, 3.2, (sx, -47, 1.6), M["metal_dark"], town, segments=8)
sign("MOTOR PARK", 112, -27.2, 3.0, 0.7, M["paint_white"], board=M["sign_green"])
loc_circle("park", 100, -40, 16)
empty("SPAWN_player", (108, -44, 0), town)

# Traffic on the main road
danfo("RoadDanfo1", -70, -2.4, rot=1)
danfo("RoadDanfo2", 40, 2.4, rot=1)
keke("RoadKeke1", 10, -2.2)

# ================= Houses (homes for townspeople) =================
house_spots = [(-120, 70), (-95, 75), (-70, 72), (-25, 75), (5, 72), (30, 70), (-120, -70), (-90, -95), (75, -80),
               (100, -85), (125, -70), (125, 55)]
for i, (x, y) in enumerate(house_spots):
    solid_house(f"House{i}", x, y, random.choice((10, 12)), random.choice((8, 9)), HOUSE_COLS[i % len(HOUSE_COLS)],
                random.choice((M["zinc"], M["zinc_rust"], M["roof_red"])))
    if i % 3 == 0:
        loc_circle("home", x, y - 7, 4)

# ================= Trees and street lights =================
for i, (x, y) in enumerate([(-135, 20), (-80, 20), (-45, 55), (10, 18), (50, 50), (80, 18), (130, 20), (-135, -15),
                            (-60, -50), (-5, -45), (55, -60), (80, -15), (130, -15), (-10, -100), (40, -100), (110, 100),
                            (-60, 100), (0, 100)]):
    tree(f"Tree{i}", x + random.uniform(-2, 2), y + random.uniform(-2, 2), random.uniform(0.9, 1.3))

print("Town triangles:", stats())
os.makedirs(os.path.dirname(OUT), exist_ok=True)
export_fbx(OUT)
print("Exported", OUT)

if "--render" in sys.argv:
    import bpy
    prev = os.path.join(ROOT, "docs", "previews")
    os.makedirs(prev, exist_ok=True)
    setup_render((1280, 720), 20)
    render(os.path.join(prev, "town_overview.png"), (-120, -150, 90), (0, -10, 0), lens=30)
    render(os.path.join(prev, "town_street.png"), (-40, -10, 4), (30, 20, 2), lens=28)
    for o in bpy.data.objects:
        if "_Roof_NOCOL" in o.name:
            o.hide_render = True
    render(os.path.join(prev, "town_lodge_school.png"), (0, -50, 22), (0, -76, 0), lens=26)
