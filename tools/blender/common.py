"""Shared helpers for building Corper Life 3D models in Blender (headless, via the bpy module)."""
import math
import bpy
import bmesh

_mats = {}


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()


def mat(name, rgb, rough=0.8, metal=0.0):
    """Flat-colour PBR material. Name is kept so Unity can find e.g. 'Skin' materials."""
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*srgb_to_linear(rgb), 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*srgb_to_linear(rgb), 1.0)
    _mats[name] = m
    return m


def srgb_to_linear(rgb):
    def c(v):
        v = v / 255.0 if v > 1 else v
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return tuple(c(v) for v in rgb)


def link(obj, parent=None):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def empty(name, loc=(0, 0, 0), parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.3
    e.location = loc
    return link(e, parent)


def _mesh_obj(name, bm, material, parent=None, smooth=False):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    obj = bpy.data.objects.new(name, me)
    if material is not None:
        obj.data.materials.append(material)
    return link(obj, parent)


def box(name, size, loc, material, parent=None, bevel=0.0, rot_z=0.0):
    """Axis-aligned box. size=(x,y,z), loc = centre."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    if rot_z:
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0),
                         matrix=__import__("mathutils").Matrix.Rotation(rot_z, 3, "Z"))
    bmesh.ops.translate(bm, vec=loc, verts=bm.verts)
    obj = _mesh_obj(name, bm, material, parent)
    if bevel > 0:
        mod = obj.modifiers.new("bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
    return obj


def cylinder(name, radius, depth, loc, material, parent=None, segments=16, radius_top=None, smooth=True,
             axis="Z"):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments,
                          radius1=radius, radius2=radius if radius_top is None else radius_top, depth=depth)
    import mathutils
    if axis == "X":
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=mathutils.Matrix.Rotation(math.pi / 2, 3, "Y"))
    elif axis == "Y":
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=mathutils.Matrix.Rotation(math.pi / 2, 3, "X"))
    bmesh.ops.translate(bm, vec=loc, verts=bm.verts)
    return _mesh_obj(name, bm, material, parent, smooth)


def sphere(name, radius, loc, material, parent=None, scale=(1, 1, 1), subdiv=2):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    bmesh.ops.scale(bm, vec=scale, verts=bm.verts)
    bmesh.ops.translate(bm, vec=loc, verts=bm.verts)
    return _mesh_obj(name, bm, material, parent, smooth=True)


def gable_roof(name, length, width, height, loc, material, parent=None, overhang=0.6, along="X"):
    """Pitched (zinc) roof. loc = centre of the eave line."""
    L = length / 2 + overhang
    W = width / 2 + overhang
    bm = bmesh.new()
    if along == "X":
        v = [(-L, -W, 0), (L, -W, 0), (L, W, 0), (-L, W, 0), (-L, 0, height), (L, 0, height)]
        faces = [(0, 1, 5, 4), (2, 3, 4, 5), (0, 4, 3), (1, 2, 5)]
    else:
        v = [(-W, -L, 0), (W, -L, 0), (W, L, 0), (-W, L, 0), (0, -L, height), (0, L, height)]
        faces = [(3, 0, 4, 5), (1, 2, 5, 4), (0, 1, 4), (2, 3, 5)]
    verts = [bm.verts.new((x + loc[0], y + loc[1], z + loc[2])) for x, y, z in v]
    for f in faces:
        bm.faces.new([verts[i] for i in f])
    obj = _mesh_obj(name, bm, material, parent)
    sol = obj.modifiers.new("thick", "SOLIDIFY")
    sol.thickness = 0.08
    return obj


def text_mesh(name, body, loc, size, material, parent=None, rot=(math.pi / 2, 0, 0), extrude=0.03):
    curve = bpy.data.curves.new(name, "FONT")
    curve.body = body
    curve.size = size
    curve.extrude = extrude
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    obj = bpy.data.objects.new(name, curve)
    obj.location = loc
    obj.rotation_euler = rot
    link(obj, parent)
    obj.data.materials.append(material)
    # Convert to mesh so FBX exports it.
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    return bpy.context.view_layer.objects.active


def export_fbx(path, root_objects=None):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="AUTO",
    )


def stats():
    tris = 0
    deps = bpy.context.evaluated_depsgraph_get()
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            ev = o.evaluated_get(deps)
            me = ev.to_mesh()
            tris += sum(len(p.vertices) - 2 for p in me.polygons)
            ev.to_mesh_clear()
    return tris


def setup_render(res=(1280, 720), samples=24):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("Sky")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (*srgb_to_linear((150, 190, 225)), 1)
    bg.inputs["Strength"].default_value = 0.9
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 4.0
    sun_data.angle = math.radians(3)
    sun = bpy.data.objects.new("RenderSun", sun_data)
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
    sc.collection.objects.link(sun)
    sc.view_settings.view_transform = "AgX"
    return sun


def render(path, cam_loc, look_at, lens=35):
    import mathutils
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = lens
    cam_data.clip_end = 1000
    cam = bpy.data.objects.new("RenderCam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = cam_loc
    d = mathutils.Vector(look_at) - mathutils.Vector(cam_loc)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
