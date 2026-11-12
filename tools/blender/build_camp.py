"""
Builds the orientation camp and exports Assets/Resources/Models/camp.fbx.
Run:  python3 tools/blender/build_camp.py [--render]

Layout (metres; X = east, Y = north):
  gate south (0,-80) -> office -> parade ground centre (0,0) -> multipurpose hall north (0,58)
  hostels west, Mammy Market south-east, clinic + mosque/chapel east, kitchen north-west,
  SAED tents north-east, sports field south-west.
Location markers are empties named LOC_<id>__r<radius>. The game reads them at runtime.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(__file__))
from common import (reset_scene, mat, empty, box, cylinder, sphere, gable_roof, text_mesh,
                    export_fbx, stats, setup_render, render)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Models", "camp.fbx")
random.seed(42)

reset_scene()

# ---------- Materials ----------
M = {
    "laterite": mat("Laterite", (166, 104, 70), 0.95),
    "grass": mat("Grass", (104, 132, 64), 0.95),
    "grass_dry": mat("GrassDry", (150, 145, 80), 0.95),
    "concrete": mat("Concrete", (178, 174, 166), 0.9),
    "paint_white": mat("PaintWhite", (232, 228, 214), 0.85),
    "paint_cream": mat("PaintCream", (226, 211, 170), 0.85),
    "paint_green": mat("PaintGreen", (34, 120, 72), 0.8),
    "paint_blue": mat("PaintBlue", (60, 110, 160), 0.8),
    "zinc": mat("Zinc", (150, 155, 158), 0.45, 0.6),
    "zinc_rust": mat("ZincRust", (140, 92, 64), 0.7, 0.3),
    "wood": mat("Wood", (110, 74, 45), 0.8),
    "window": mat("Window", (40, 52, 60), 0.3),
    "door": mat("Door", (92, 60, 38), 0.7),
    "metal_dark": mat("MetalDark", (52, 54, 56), 0.5, 0.7),
    "red": mat("Red", (190, 40, 40), 0.7),
    "trunk": mat("Trunk", (84, 64, 48), 0.9),
    "leaf": mat("Leaf", (58, 104, 46), 0.9),
    "leaf2": mat("LeafDark", (44, 86, 40), 0.9),
    "tent": mat("Canvas", (230, 226, 210), 0.9),
    "line_white": mat("LineWhite", (240, 240, 235), 0.8),
    "sign_green": mat("SignGreen", (0, 120, 70), 0.7),
    "black": mat("Black", (25, 25, 25), 0.6),
    "plastic_blue": mat("PlasticBlue", (40, 90, 170), 0.6),
    "plastic_black": mat("PlasticBlack", (30, 32, 35), 0.6),
}
CANOPY = [mat("CanopyRed", (196, 52, 46)), mat("CanopyYellow", (230, 182, 40)), mat("CanopyBlue", (40, 104, 180)),
          mat("CanopyGreen", (40, 150, 90)), mat("CanopyOrange", (226, 120, 40)), mat("CanopyStripe", (220, 220, 210))]

camp = empty("Camp")


# ---------- Helpers for buildings ----------
def building(name, cx, cy, lx, ly, h, wall, roof, door_side="S", windows=True, roof_h=2.0, rust=False):
    """Simple block building with windows, a door and a pitched zinc roof."""
    g = empty(name, (0, 0, 0), camp)
    box(name + "_Walls", (lx, ly, h), (cx, cy, h / 2), wall, g)
    box(name + "_Plinth", (lx + 0.3, ly + 0.3, 0.4), (cx, cy, 0.2), M["concrete"], g)
    along = "X" if lx >= ly else "Y"
    gable_roof(name + "_Roof", lx if along == "X" else ly, ly if along == "X" else lx, roof_h,
               (cx, cy, h), M["zinc_rust"] if rust else M["zinc"], g, along=along)
    if windows:
        step = 3.2
        n = int((lx if along == "X" else ly) // step) - 1
        for i in range(n):
            off = -((n - 1) * step) / 2 + i * step
            for side in (-1, 1):
                if along == "X":
                    box(f"{name}_Win{i}{side}_NOCOL", (1.4, 0.06, 1.1),
                        (cx + off, cy + side * (ly / 2 + 0.02), h * 0.55), M["window"], g)
                else:
                    box(f"{name}_Win{i}{side}_NOCOL", (0.06, 1.4, 1.1),
                        (cx + side * (lx / 2 + 0.02), cy + off, h * 0.55), M["window"], g)
    dx, dy = {"S": (0, -ly / 2 - 0.03), "N": (0, ly / 2 + 0.03), "E": (lx / 2 + 0.03, 0), "W": (-lx / 2 - 0.03, 0)}[door_side]
    dsize = (1.4, 0.08, 2.3) if door_side in "NS" else (0.08, 1.4, 2.3)
    box(name + "_Door_NOCOL", dsize, (cx + dx, cy + dy, 1.15), M["door"], g)
    return g


def tree(name, x, y, s=1.0, kind=0):
    g = empty(name, (0, 0, 0), camp)
    h = random.uniform(3.2, 4.6) * s
    cylinder(name + "_Trunk", 0.22 * s, h, (x, y, h / 2), M["trunk"], g, segments=8, radius_top=0.15 * s)
    leaf = M["leaf"] if kind == 0 else M["leaf2"]
    for i in range(4):
        sphere(f"{name}_Leaf{i}", random.uniform(1.4, 2.1) * s,
               (x + random.uniform(-1, 1) * s, y + random.uniform(-1, 1) * s, h + random.uniform(0.2, 1.4) * s),
               leaf, g, scale=(1, 1, 0.75), subdiv=1)


def streetlight(name, x, y):
    g = empty(name, (0, 0, 0), camp)
    cylinder(name + "_Pole", 0.07, 6, (x, y, 3), M["metal_dark"], g, segments=8)
    box(name + "_Arm_NOCOL", (0.9, 0.12, 0.08), (x + 0.4, y, 6), M["metal_dark"], g)
    box(name + "_Lamp_NOCOL", (0.5, 0.25, 0.12), (x + 0.8, y, 5.92), M["paint_white"], g)


def loc_marker(lid, x, y, r, rot=0.0):
    rs = str(r).replace(".", "_")
    e = empty(f"LOC_{lid}__r{rs}", (x, y, 0), camp)
    e.rotation_euler = (0, 0, rot)
    return e


# ---------- Ground ----------
box("Ground", (260, 220, 0.2), (0, 0, -0.1), M["laterite"], camp)
for i in range(26):
    x, y = random.uniform(-115, 115), random.uniform(-95, 95)
    if abs(x) < 40 and abs(y) < 28:
        continue
    box(f"GrassPatch{i}_NOCOL", (random.uniform(8, 22), random.uniform(6, 16), 0.04), (x, y, 0.02),
        M["grass"] if i % 3 else M["grass_dry"], camp)

# Concrete footpaths linking the main areas
for i, (size, loc) in enumerate([
    ((4, 58, 0.06), (0, -53, 0.03)),        # gate -> parade
    ((4, 22, 0.06), (0, 37, 0.03)),         # parade -> hall
    ((36, 3, 0.06), (-30, -66, 0.03)),      # gate path -> office
    ((10, 3, 0.06), (-43, 2, 0.03)),        # parade -> hostels
    ((3, 70, 0.06), (-47, 2, 0.03)),        # walkway along hostel doors
    ((8, 3, 0.06), (-42, 51, 0.03)),        # hostels -> kitchen
    ((22, 3, 0.06), (49, -34, 0.03)),       # parade corner -> market
    ((20, 3, 0.06), (52, 42, 0.03)),        # -> clinic
    ((24, 3, 0.06), (56, 6, 0.03)),         # -> mosque and chapel
]):
    box(f"Path{i}_NOCOL", size, loc, M["concrete"], camp)

# Perimeter wall with a gap for the gate
W, H = 110, 90
for name, size, loc in [
    ("WallN", (2 * W, 0.3, 2.2), (0, H, 1.1)),
    ("WallE", (0.3, 2 * H, 2.2), (W, 0, 1.1)),
    ("WallW", (0.3, 2 * H, 2.2), (-W, 0, 1.1)),
    ("WallSW", (W - 5, 0.3, 2.2), (-(W + 5) / 2, -H, 1.1)),
    ("WallSE", (W - 5, 0.3, 2.2), ((W + 5) / 2, -H, 1.1)),
]:
    box(name, size, loc, M["paint_cream"], camp)

# Gate: pillars, arch sign, guard post
gate = empty("Gate", (0, 0, 0), camp)
for sx in (-5, 5):
    box(f"GatePillar{sx}", (1.2, 1.2, 5.2), (sx, -H, 2.6), M["paint_green"], gate)
box("GateArch", (11.2, 0.6, 1.4), (0, -H, 5.5), M["paint_white"], gate)
text_mesh("GateSign_NOCOL", "STATE ORIENTATION CAMP", (0, -H - 0.33, 5.5), 0.62, M["sign_green"], gate)
building("GuardPost", 9, -H + 4, 3.2, 3.2, 2.8, M["paint_green"], M["zinc"], door_side="W", windows=False, roof_h=0.8)
box("Barrier_NOCOL", (8, 0.12, 0.12), (0, -H + 1.5, 1.0), M["red"], gate)
loc_marker("gate", 0, -H + 6, 8)
spawn = empty("SPAWN_player", (0, -H + 9, 0), camp)

# ---------- Parade ground ----------
box("ParadeSlab", (76, 48, 0.12), (0, 0, 0.06), M["concrete"], camp)
for i in range(-3, 4):  # marching lines
    box(f"ParadeLine{i}_NOCOL", (70, 0.15, 0.02), (0, i * 6, 0.13), M["line_white"], camp)
# Dais with canopy on the north side
box("Dais", (16, 6, 1.2), (0, 27, 0.6), M["concrete"], camp)
for i in range(3):
    box(f"DaisStep{i}", (6, 0.6, 0.4 * (i + 1)), (0, 23.6 + i * 0.6, 0.2 * (i + 1)), M["concrete"], camp)
for sx in (-7.5, 7.5):
    for sy in (25, 29.5):
        cylinder(f"DaisPost{sx}_{sy}", 0.12, 3.5, (sx, sy, 2.95), M["metal_dark"], camp, segments=8)
gable_roof("DaisCanopy", 17, 6, 1.2, (0, 27.2, 4.7), M["paint_green"], camp, overhang=0.3)
for i, sx in enumerate((-4, 0, 4)):
    cylinder(f"Flagpole{i}", 0.06, 10, (sx, 33, 5), M["metal_dark"], camp, segments=8)
    box(f"Flag{i}_NOCOL", (0.04, 1.8, 1.1) if False else (1.8, 0.04, 1.1), (sx + 0.95, 33, 9.3),
        [M["paint_green"], M["paint_white"], M["paint_green"]][i] if i != 1 else M["paint_white"], camp)
# Bleachers along the east side
for r in range(4):
    box(f"Bleacher{r}", (2.0, 30, 0.45 + r * 0.45), (42 + r * 1.6, 0, (0.45 + r * 0.45) / 2), M["concrete"], camp)
loc_marker("parade", 0, 0, 30)

# ---------- Multipurpose hall ----------
building("Hall", 0, 60, 40, 22, 7.5, M["paint_white"], M["zinc"], door_side="S", roof_h=4.0)
box("HallPorch", (14, 4, 0.3), (0, 47.5, 0.15), M["concrete"], camp)
gable_roof("HallPorchRoof", 14, 4, 0.6, (0, 47.5, 4.2), M["zinc"], camp, overhang=0.2)
for sx in (-6.5, 6.5):
    cylinder(f"HallPorchPost{sx}", 0.15, 4.2, (sx, 46, 2.1), M["paint_white"], camp, segments=10)
text_mesh("HallSign_NOCOL", "MULTIPURPOSE HALL", (0, 48.95, 6.2), 1.0, M["sign_green"], camp)
loc_marker("hall", 0, 44, 11)

# ---------- Hostels (west) ----------
for i, (cy, label) in enumerate([(32, "HOSTEL A"), (12, "HOSTEL B"), (-8, "HOSTEL C"), (-28, "HOSTEL D")]):
    building(f"Hostel{i}", -72, cy, 42, 10, 4.0, M["paint_cream"], M["zinc"], door_side="E", roof_h=2.2,
             rust=(i % 2 == 1))
    text_mesh(f"HostelSign{i}_NOCOL", label, (-50.9, cy, 3.3), 0.7, M["sign_green"], camp,
              rot=(math.pi / 2, 0, math.pi / 2))
    # water tank stand
    for sx in (-0.9, 0.9):
        for sy in (-0.9, 0.9):
            cylinder(f"TankLeg{i}_{sx}_{sy}", 0.06, 3, (-48 + sx, cy + 4 + sy, 1.5), M["metal_dark"], camp, segments=6)
    box(f"TankBase{i}", (2.2, 2.2, 0.15), (-48, cy + 4, 3.05), M["metal_dark"], camp)
    cylinder(f"Tank{i}", 1.0, 1.8, (-48, cy + 4, 4.0), M["plastic_black"], camp, segments=14)
loc_marker("hostel", -46, 2, 20)

# ---------- Kitchen (north-west) ----------
building("Kitchen", -55, 62, 20, 12, 4.2, M["paint_white"], M["zinc_rust"], door_side="S", roof_h=1.8)
box("KitchenChimney", (1.2, 1.2, 3), (-60, 65, 6.5), M["concrete"], camp)
for i in range(3):
    cylinder(f"Pot{i}_NOCOL", 0.55, 0.7, (-52 + i * 2.2, 53, 0.55), M["metal_dark"], camp, segments=12)
box("ServingTable", (8, 1.2, 0.9), (-55, 54.5, 0.45), M["wood"], camp)
loc_marker("kitchen", -55, 51, 9)

# ---------- Camp office (near gate) ----------
building("Office", -24, -66, 16, 9, 3.8, M["paint_green"], M["zinc"], door_side="E", roof_h=1.5)
text_mesh("OfficeSign_NOCOL", "CAMP OFFICE", (-15.9, -66, 3.0), 0.7, M["paint_white"], camp,
          rot=(math.pi / 2, 0, math.pi / 2))
box("NoticeBoard", (0.12, 3, 1.6), (-14, -60, 1.6), M["wood"], camp)
loc_marker("office", -12, -66, 7)

# ---------- Mammy Market (south-east) ----------
mk = empty("MammyMarket", (0, 0, 0), camp)
for row in range(4):
    for col in range(6):
        x = 46 + col * 5.2
        y = -40 - row * 7
        canopy = CANOPY[(row * 6 + col) % len(CANOPY)]
        for sx in (-2, 2):
            for sy in (-1.5, 1.5):
                cylinder(f"StallPost{row}{col}{sx}{sy}_NOCOL", 0.05, 2.6, (x + sx, y + sy, 1.3), M["wood"], mk, segments=6)
        gable_roof(f"StallRoof{row}{col}_NOCOL", 4.4, 3.4, 0.5, (x, y, 2.6), canopy, mk, overhang=0.2)
        box(f"StallTable{row}{col}", (3.6, 1.0, 0.85), (x, y - 0.8, 0.43), M["wood"], mk)
        # goods on the table
        for k in range(3):
            box(f"Goods{row}{col}{k}_NOCOL", (0.5, 0.4, 0.3), (x - 1.1 + k * 1.1, y - 0.8, 1.0),
                random.choice(CANOPY[:5]), mk)
text_mesh("MarketSign_NOCOL", "MAMMY MARKET", (58.5, -33.5, 3.4), 1.0, M["paint_white"], camp)
box("MarketSignBoard", (12, 0.15, 1.5), (58.5, -33.35, 3.4), M["sign_green"], camp)
for sx in (52.5, 64.5):
    cylinder(f"MarketSignPost{sx}", 0.1, 4.1, (sx, -33.3, 2.05), M["metal_dark"], camp, segments=8)
loc_marker("market", 59, -50, 17)

# ---------- Clinic and worship (east) ----------
building("Clinic", 72, 42, 14, 9, 3.6, M["paint_white"], M["zinc"], door_side="W", roof_h=1.4)
box("ClinicCrossV_NOCOL", (0.1, 0.5, 1.6), (64.9, 42, 3.0), M["red"], camp)
box("ClinicCrossH_NOCOL", (0.1, 1.6, 0.5), (64.9, 42, 3.0), M["red"], camp)
loc_marker("clinic", 62, 42, 6)

# Mosque: block with dome and small minaret
box("MosqueBody", (11, 11, 4.5), (86, 14, 2.25), M["paint_white"], camp)
sphere("MosqueDome", 4.0, (86, 14, 4.5), M["paint_green"], camp, scale=(1, 1, 0.85), subdiv=3)
cylinder("Minaret", 0.8, 11, (92.5, 19.5, 5.5), M["paint_white"], camp, segments=12)
cylinder("MinaretTop", 1.0, 1.6, (92.5, 19.5, 11.6), M["paint_green"], camp, segments=12, radius_top=0.1)
# Chapel: gable building with a cross
building("Chapel", 86, -4, 9, 14, 4.5, M["paint_white"], M["paint_blue"], door_side="W", roof_h=3.0)
box("ChapelCrossV_NOCOL", (0.25, 0.25, 2.2), (86, -4 - 7.6, 8.4), M["paint_white"], camp)
box("ChapelCrossH_NOCOL", (1.3, 0.25, 0.25), (86, -4 - 7.6, 8.9), M["paint_white"], camp)
loc_marker("worship", 76, 6, 10)

# ---------- SAED tents (north-east) ----------
for i in range(4):
    x = 32 + (i % 2) * 12
    y = 48 + (i // 2) * 12
    for sx in (-4, 4):
        for sy in (-3, 3):
            cylinder(f"TentPole{i}{sx}{sy}_NOCOL", 0.06, 2.6, (x + sx, y + sy, 1.3), M["metal_dark"], camp, segments=6)
    gable_roof(f"Tent{i}_NOCOL", 8, 6, 1.6, (x, y, 2.6), M["tent"], camp, overhang=0.3)
    for k in range(3):
        box(f"TentTable{i}{k}", (2.4, 0.9, 0.8), (x - 2.6 + k * 2.6, y, 0.4), M["wood"], camp)
loc_marker("saed", 38, 54, 13)

# ---------- Sports field (south-west) ----------
box("Pitch_NOCOL", (54, 32, 0.05), (-62, -52, 0.03), M["grass"], camp)
for name, size, loc in [("PitchLineN", (54, 0.15, 0.02), (-62, -36, 0.07)), ("PitchLineS", (54, 0.15, 0.02), (-62, -68, 0.07)),
                        ("PitchLineE", (0.15, 32, 0.02), (-35, -52, 0.07)), ("PitchLineW", (0.15, 32, 0.02), (-89, -52, 0.07)),
                        ("PitchLineMid", (0.15, 32, 0.02), (-62, -52, 0.07))]:
    box(name + "_NOCOL", size, loc, M["line_white"], camp)
for gx, side in ((-88.6, 1), (-35.4, -1)):
    for gy in (-55.5, -48.5):
        cylinder(f"GoalPost{gx}{gy}", 0.07, 2.4, (gx, gy, 1.2), M["paint_white"], camp, segments=8)
    cylinder(f"GoalBar{gx}", 0.07, 7, (gx, -52, 2.4), M["paint_white"], camp, segments=8, axis="Y")
# Man O' War obstacles beside the pitch
for i in range(5):
    box(f"Obstacle{i}", (0.3, 4, 1.2 + (i % 2) * 0.8), (-80 + i * 5, -76, 0.6 + (i % 2) * 0.4), M["wood"], camp)
loc_marker("field", -62, -52, 22)

# ---------- Generator house, borehole, lights, trees ----------
building("GeneratorHouse", -96, 72, 6, 5, 3, M["concrete"], M["zinc_rust"], door_side="S", windows=False, roof_h=0.6)
cylinder("Borehole", 0.3, 1.0, (-40, 22, 0.5), M["plastic_blue"], camp, segments=10)

for i, (x, y) in enumerate([(-42, -20), (-42, 20), (42, 30), (42, -30), (-20, 40), (20, 40), (-20, -40), (20, -40),
                            (0, -60), (-40, -70), (40, -70), (70, 25), (-80, 50), (60, 60)]):
    streetlight(f"Light{i}", x, y)

tree_spots = [(-100, 80), (-90, 30), (-100, -10), (-100, -70), (-30, 80), (25, 80), (55, 80), (100, 70), (102, 30),
              (102, -40), (95, -80), (30, -80), (-30, -82), (-55, -84), (20, -25), (-25, 30), (-35, -40), (30, 28),
              (52, 20), (64, 0), (-40, 75), (75, 70), (-15, -50), (15, -55), (-96, -40), (100, -15)]
for i, (x, y) in enumerate(tree_spots):
    tree(f"Tree{i}", x + random.uniform(-2, 2), y + random.uniform(-2, 2), random.uniform(0.9, 1.35), i % 2)

print("Camp triangles:", stats())
os.makedirs(os.path.dirname(OUT), exist_ok=True)
export_fbx(OUT)
print("Exported", OUT)

if "--render" in sys.argv:
    prev = os.path.join(ROOT, "docs", "previews")
    os.makedirs(prev, exist_ok=True)
    setup_render((1280, 720), 20)
    render(os.path.join(prev, "camp_overview.png"), (-95, -125, 75), (0, -5, 0), lens=30)
    render(os.path.join(prev, "camp_parade.png"), (14, -32, 5), (0, 26, 3), lens=28)
    render(os.path.join(prev, "camp_market.png"), (40, -22, 4.5), (60, -48, 1), lens=28)
