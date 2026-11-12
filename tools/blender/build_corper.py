"""
Builds the corps member character and exports Assets/Resources/Models/corper.fbx.
Run:  python3 tools/blender/build_corper.py [--render]

The character faces -Y in Blender. Parts are named so the game can customise them:
  - material "Skin"           -> tinted to the chosen skin tone
  - objects containing "Kit"   -> shown after kit collection (khaki uniform, face cap)
  - objects containing "Mufti" -> shown before (ankara shirt, jeans)
  - HairM / HairF              -> hairstyle by gender
  - empties LegL, LegR, ArmL, ArmR, Body -> pivots for the walk animation
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from common import (reset_scene, mat, empty, box, cylinder, sphere, export_fbx, stats, setup_render, render)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Models", "corper.fbx")

reset_scene()

SKIN = mat("Skin", (99, 64, 42), 0.55)
KHAKI = mat("Khaki", (184, 160, 106), 0.85)
KHAKI_DARK = mat("KhakiDark", (150, 128, 82), 0.85)
WHITE = mat("KitWhite", (240, 240, 236), 0.8)
DENIM = mat("Denim", (52, 74, 110), 0.85)
ANKARA = mat("Ankara", (214, 98, 34), 0.8)
ANKARA2 = mat("AnkaraTeal", (20, 128, 128), 0.8)
HAIR = mat("Hair", (20, 16, 14), 0.9)
BOOT = mat("Boot", (30, 26, 22), 0.5)
EYE = mat("EyeWhite", (235, 232, 225), 0.3)
PUPIL = mat("Pupil", (22, 16, 12), 0.2)
LIPS = mat("Lips", (80, 44, 38), 0.6)
GREEN = mat("CapGreen", (0, 120, 70), 0.8)

root = empty("Corper")

# ---------------- Legs (pivot at the hips) ----------------
for side, sx in (("L", 0.1), ("R", -0.1)):
    leg = empty("Leg" + side, (sx, 0, 0.93), root)
    # Children are positioned relative to the pivot.
    cylinder(f"Leg{side}_Kit", 0.088, 0.82, (0, 0, -0.43), KHAKI, leg, segments=12, radius_top=0.095)
    cylinder(f"Leg{side}_Mufti", 0.086, 0.82, (0, 0, -0.43), DENIM, leg, segments=12, radius_top=0.093)
    box(f"Boot{side}", (0.12, 0.27, 0.12), (0, -0.05, -0.87), BOOT, leg, bevel=0.03)
    cylinder(f"BootTop{side}_Kit", 0.075, 0.16, (0, 0, -0.76), BOOT, leg, segments=10)

# ---------------- Body (bobs while walking) ----------------
body = empty("Body", (0, 0, 0), root)

# Hips
cylinder("Hips_Kit", 0.17, 0.2, (0, 0, 0.95), KHAKI, body, segments=16).scale = (1, 0.68, 1)
cylinder("Hips_Mufti", 0.168, 0.2, (0, 0, 0.95), DENIM, body, segments=16).scale = (1, 0.68, 1)
cylinder("Belt_Kit", 0.172, 0.05, (0, 0, 1.05), KHAKI_DARK, body, segments=16).scale = (1, 0.69, 1)

# Torso: tapered, flattened front-to-back
t_kit = cylinder("Torso_Kit", 0.165, 0.5, (0, 0, 1.3), KHAKI, body, segments=16, radius_top=0.2)
t_kit.scale = (1, 0.68, 1)
t_muf = cylinder("Torso_Mufti", 0.162, 0.5, (0, 0, 1.3), ANKARA, body, segments=16, radius_top=0.197)
t_muf.scale = (1, 0.68, 1)
# Ankara pattern stripes
for i in range(4):
    z = 1.12 + i * 0.11
    r = 0.165 + (z - 1.05) / 0.5 * 0.035 + 0.004   # follow the torso taper
    band = cylinder(f"AnkaraBand{i}_Mufti", r, 0.035, (0, 0, z), ANKARA2, body, segments=16)
    band.scale = (1, 0.68, 1)
# Khaki details: breast pockets, collar, buttons
for px in (-0.08, 0.08):
    box(f"Pocket{px}_Kit", (0.1, 0.02, 0.09), (px, -0.142, 1.38), KHAKI_DARK, body)
box("Collar_Kit", (0.22, 0.16, 0.05), (0, -0.03, 1.55), KHAKI_DARK, body, bevel=0.015)
for i in range(4):
    sphere(f"Button{i}_Kit", 0.011, (0, -0.138, 1.12 + i * 0.1), KHAKI_DARK, body, subdiv=1)
# Shoulders
for sx in (-0.19, 0.19):
    sphere(f"Shoulder{sx}_Kit", 0.085, (sx, 0, 1.49), KHAKI, body, scale=(1, 0.8, 0.75))
    sphere(f"Shoulder{sx}_Mufti", 0.083, (sx, 0, 1.49), ANKARA, body, scale=(1, 0.8, 0.75))

# Neck and head
cylinder("Neck", 0.056, 0.12, (0, 0, 1.6), SKIN, body, segments=12)
sphere("Head", 0.112, (0, 0, 1.73), SKIN, body, scale=(0.88, 0.96, 1.14), subdiv=3)
sphere("Jaw", 0.08, (0, -0.02, 1.665), SKIN, body, scale=(0.95, 0.95, 0.8), subdiv=2)
for sx in (-0.1, 0.1):
    sphere(f"Ear{sx}", 0.026, (sx, 0.005, 1.73), SKIN, body, scale=(0.5, 1, 1.3), subdiv=1)
    sphere(f"Eye{sx}", 0.017, (sx * 0.38, -0.093, 1.75), EYE, body, scale=(1.2, 0.6, 0.8), subdiv=1)
    sphere(f"Pupil{sx}", 0.009, (sx * 0.38, -0.103, 1.75), PUPIL, body, subdiv=1)
    box(f"Brow{sx}", (0.04, 0.012, 0.008), (sx * 0.38, -0.1, 1.778), HAIR, body)
sphere("Nose", 0.022, (0, -0.108, 1.715), SKIN, body, scale=(1.1, 0.8, 1), subdiv=1)
box("Mouth", (0.045, 0.012, 0.012), (0, -0.098, 1.672), LIPS, body)

# Hair: low cut (male) and a bun (female)
sphere("HairM", 0.112, (0, 0.024, 1.768), HAIR, body, scale=(0.93, 0.95, 1.0), subdiv=3)
sphere("HairF", 0.116, (0, 0.026, 1.77), HAIR, body, scale=(0.95, 0.97, 1.02), subdiv=3)
sphere("HairFBun", 0.072, (0, 0.1, 1.85), HAIR, body, subdiv=2)

# Face cap (kit)
cylinder("Cap_Kit", 0.118, 0.07, (0, 0.005, 1.83), GREEN, body, segments=16, radius_top=0.105)
box("CapBrim_Kit", (0.17, 0.1, 0.012), (0, -0.11, 1.8), GREEN, body, bevel=0.01)

# ---------------- Arms (pivot at the shoulders) ----------------
for side, sx in (("L", 0.215), ("R", -0.215)):
    arm = empty("Arm" + side, (sx, 0, 1.47), body)
    off = 0.02 if side == "L" else -0.02
    # Kit: long khaki sleeve covering the arm
    cylinder(f"Arm{side}_SleeveKit", 0.052, 0.56, (off, 0, -0.29), KHAKI, arm, segments=12, radius_top=0.062)
    # Mufti: short sleeve + bare forearm
    cylinder(f"Arm{side}_SleeveMufti", 0.058, 0.24, (off, 0, -0.12), ANKARA, arm, segments=12, radius_top=0.064)
    cylinder(f"Arm{side}_Skin", 0.042, 0.5, (off, 0, -0.3), SKIN, arm, segments=12, radius_top=0.05)
    sphere(f"Hand{side}", 0.048, (off, -0.01, -0.6), SKIN, arm, scale=(0.7, 0.9, 1.2), subdiv=2)

print("Corper triangles:", stats())
os.makedirs(os.path.dirname(OUT), exist_ok=True)
export_fbx(OUT)
print("Exported", OUT)

if "--render" in sys.argv:
    import bpy
    prev = os.path.join(ROOT, "docs", "previews")
    os.makedirs(prev, exist_ok=True)
    ground = box("RenderGround", (8, 8, 0.02), (0, 0, -0.01), mat("Laterite", (166, 104, 70), 0.95))
    setup_render((900, 900), 32)

    def show(kit, female):
        for o in bpy.data.objects:
            n = o.name
            if "Kit" in n:
                o.hide_render = not kit
            if "Mufti" in n:
                o.hide_render = kit
            if n.startswith("HairM"):
                o.hide_render = female
            if n.startswith("HairF"):
                o.hide_render = not female

    show(True, False)
    render(os.path.join(prev, "corper_kit.png"), (1.2, -3.0, 1.6), (0, 0, 1.0), lens=50)
    show(False, True)
    render(os.path.join(prev, "corper_mufti.png"), (1.2, -3.0, 1.6), (0, 0, 1.0), lens=50)
