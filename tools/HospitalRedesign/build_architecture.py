"""Author the hospital architecture in Blender; coordinates below are Unity metres."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Models/HospitalArchitecture'
inventory = json.loads((ROOT / 'ArtSource/Hospital/OriginalSceneInventory.json').read_text(encoding='utf-8-sig'))
scene = bpy.data.scenes.get('HospitalArchitecture')
if scene is None:
    scene = bpy.data.scenes.new('HospitalArchitecture')
bpy.context.window.scene = scene
# Only the collection owned by this authoring script is regenerated.
owned = bpy.data.collections.get('HospitalArchitecture_Generated')
if owned:
    for obj in list(owned.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(owned)
owned = bpy.data.collections.new('HospitalArchitecture_Generated')
scene.collection.children.link(owned)
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
materials = {}
groups = {}


def material(name, color, rough=0.55, metal=0, emission=0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = rough
    shader.inputs['Metallic'].default_value = metal
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = emission
    materials[name] = mat


material('HA_Porcelain', (0.88, 0.91, 0.90), 0.6)
material('HA_Sage', (0.55, 0.68, 0.64), 0.52)
material('HA_Vinyl', (0.67, 0.72, 0.70), 0.68)
material('HA_Trim', (0.78, 0.84, 0.82), 0.46)
material('HA_Aluminium', (0.55, 0.64, 0.66), 0.34, 0.65)
material('HA_Gasket', (0.12, 0.18, 0.19), 0.8)
material('HA_Oak', (0.64, 0.51, 0.35), 0.55)
material('HA_Ceiling', (0.84, 0.88, 0.87), 0.9)
material('HA_LED', (0.94, 0.98, 1.0), 0.35, emission=1.5)
material('HA_Blue', (0.12, 0.36, 0.44), 0.4)
material('HA_Oxygen', (0.24, 0.56, 0.42), 0.35)
material('HA_Amber', (0.83, 0.61, 0.20), 0.4)


def xyz(p):
    # Unity's FBX importer flips the exported X axis when baking this basis.
    return (-p[0], -p[2], p[1])


def own(obj, group, mat):
    for coll in list(obj.users_collection):
        coll.objects.unlink(obj)
    owned.objects.link(obj)
    obj.data.materials.append(materials[mat])
    groups.setdefault(group, []).append(obj)
    return obj


def box(name, p, s, mat='HA_Porcelain', bevel=0.012, group='Shell'):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz(p))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (s[0], s[2], s[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Soft manufactured edges', 'BEVEL')
        mod.width = min(bevel, min(s) * 0.3)
        mod.segments = 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
        norm = obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=norm.name)
    return own(obj, group, mat)


def cylinder(name, p, radius, depth, mat, group, axis='Y'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=depth, location=xyz(p))
    obj = bpy.context.object
    obj.name = name
    if axis == 'Z':
        obj.rotation_euler.x = math.pi / 2
    if axis == 'X':
        obj.rotation_euler.y = math.pi / 2
    return own(obj, group, mat)


# The existing shell colliders are retained in Unity. These are their detailed visual skins.
for item in inventory:
    path = item['path']
    if not item['active'] or not item['bounds']:
        continue
    if path.startswith('Room/Wall_') and 'Front' not in path:
        box(path.split('/')[-1], item['bounds'][:3], item['bounds'][3:], group='RoomShell')
    if path in ('Room/Ceiling', 'Corridor/Ceiling'):
        box(path.replace('/', '_'), item['bounds'][:3], item['bounds'][3:], 'HA_Ceiling', group=path.replace('/', '')+'Backing')
    if path in ('Room/Floor', 'Corridor/Floor'):
        box(path.replace('/', '_'), item['bounds'][:3], item['bounds'][3:], 'HA_Vinyl', bevel=0, group=path.replace('/', ''))
    if path.startswith('Corridor/Wall_') and 'RoomSide' not in path and 'Wall_End' not in path:
        box(path.replace('/', '_'), item['bounds'][:3], item['bounds'][3:], group='CorridorShell')
    if path.startswith('Room/Column_') or (path.startswith('Props/HeadUnit_') and path.endswith('/Backboard')):
        box(path.replace('/', '_'), item['bounds'][:3], item['bounds'][3:], 'HA_Oak', group='OakWallAccents')
    if path.startswith('Props/') and (path.endswith('/Shelf') or path.endswith('/Top')):
        obj = box(path.replace('/', '_'), item['pos'], item['scale'], 'HA_Oak', bevel=0.025, group='RoundedShelves')
        obj.rotation_euler.z = math.radians(-item['rot'][1])

# Room/corridor partition, with the existing 1.1 m doorway kept open.
for name, p, size in [
    ('PartitionLeft', (-3.24, 1.5, -3.60), (11.58, 3.0, 0.15)),
    ('PartitionRight', (6.33, 1.5, -3.60), (5.42, 3.0, 0.15)),
    ('DoorLintel', (3.1, 2.57, -3.60), (1.1, 0.86, 0.15)),
]:
    box(name, p, size, group='EntryPartition')

# Washable lower wall protection, continuous coved skirting and a fine top cap.
def wall_band(name, p, size):
    box(name, p, size, 'HA_Sage', group='WallProtection')
    along_x = size[0] > size[2]
    cap = (size[0], 0.028, 0.065) if along_x else (0.065, 0.028, size[2])
    base = (size[0], 0.135, 0.075) if along_x else (0.075, 0.135, size[2])
    box(name+'_Cap', (p[0], 1.055, p[2]), cap, 'HA_Trim', group='WallTrim')
    box(name+'_CovedBase', (p[0], 0.069, p[2]), base, 'HA_Trim', bevel=0.024, group='WallTrim')

wall_band('BackWall', (0, 0.52, 3.468), (9, 1.04, 0.06))
wall_band('RightWall', (4.468, 0.52, 0), (0.06, 1.04, 7))
wall_band('FrontLeft', (-0.975, 0.52, -3.49), (7.05, 1.04, 0.06))
wall_band('FrontRight', (4.06, 0.52, -3.49), (0.88, 1.04, 0.06))
box('UnderWindowPanel', (-4.464, 0.43, 0), (0.05, 0.86, 7), 'HA_Sage', group='WallProtection')
box('WindowBase', (-4.43, 0.07, 0), (0.07, 0.14, 7), 'HA_Trim', group='WallTrim')
for x0, x1 in [(-9, -6.65), (-5.41, -0.64), (0.60, 5.39), (6.63, 9)]:
    wall_band('CorridorFar', ((x0+x1)/2, 0.52, -6.816), (x1-x0, 1.04, 0.065))
for x0, x1 in [(-9, 2.55), (3.65, 9)]:
    wall_band('CorridorNear', ((x0+x1)/2, 0.52, -3.70), (x1-x0, 1.04, 0.065))
    box('SafetyRail', ((x0+x1)/2, 0.87, -3.79), (x1-x0, 0.065, 0.065), 'HA_Oak', bevel=0.02, group='CorridorRails')

# Cove cornice, a small shadow reveal rather than thick decorative beams.
for z in (-3.46, 3.46):
    box('CeilingPerimeter', (0, 2.955, z), (9, 0.085, 0.095), 'HA_Trim', group='CeilingGrid')
for x in (-4.46, 4.46):
    box('CeilingPerimeter', (x, 2.955, 0), (0.095, 0.085, 7), 'HA_Trim', group='CeilingGrid')
# Fine suspended ceiling seams; the geometry is joined before export.
for x in [i * 0.6 - 4.2 for i in range(15)]:
    box('CeilingTee', (x, 2.994, 0), (0.010, 0.008, 6.88), 'HA_Ceiling', 0.001, 'CeilingGrid')
for z in [i * 0.6 - 3.0 for i in range(11)]:
    box('CeilingTee', (0, 2.994, z), (8.88, 0.008, 0.010), 'HA_Ceiling', 0.001, 'CeilingGrid')

# Recessed LED luminaires at the original light locations.
for item in inventory:
    if 'CeilingPanel_' not in item['path'] or not item['bounds']:
        continue
    x, y, z = item['pos']
    fixture_group = 'CorridorLuminaires' if item['path'].startswith('Corridor/') else 'RoomLuminaires'
    box('LuminaireHousing', (x, 2.964, z), (1.23, 0.065, 0.42), 'HA_Porcelain', group=fixture_group)
    box('OpalDiffuser', (x, 2.926, z), (1.15, 0.018, 0.34), 'HA_LED', group=fixture_group)
    for dx in (-0.60, 0.60):
        for dz in (-0.18, 0.18):
            cylinder('Fixing', (x+dx, 2.926, z+dz), 0.009, 0.012, 'HA_Gasket', fixture_group)

# Double-depth window profiles, dark seals, handles and a solid-surface sill.
for y in (0.98, 2.43):
    box('WindowMainRail', (-4.49, y, 0), (0.18, 0.072, 5.25), 'HA_Aluminium', group='WindowProfiles')
    box('WindowSeal', (-4.387, y, 0), (0.012, 0.012, 5.13), 'HA_Gasket', group='WindowProfiles')
for z in (-2.6, -0.87, 0.87, 2.6):
    box('WindowMullion', (-4.49, 1.705, z), (0.18, 1.50, 0.068), 'HA_Aluminium', group='WindowProfiles')
    for dz in (-0.043, 0.043):
        box('WindowSeal', (-4.393, 1.705, z+dz), (0.012, 1.36, 0.012), 'HA_Gasket', group='WindowProfiles')
for z in (-1.70, 0, 1.70):
    box('WindowMidRail', (-4.425, 1.73, z), (0.095, 0.045, 1.64), 'HA_Aluminium', group='WindowProfiles')
    box('WindowLatchPlate', (-4.372, 1.54, z+0.66), (0.022, 0.09, 0.035), 'HA_Aluminium', group='WindowProfiles')
    cylinder('WindowLatch', (-4.35, 1.51, z+0.66), 0.013, 0.12, 'HA_Trim', 'WindowProfiles')
box('SolidSurfaceSill', (-4.38, 0.92, 0), (0.40, 0.07, 5.42), 'HA_Porcelain', 0.02, 'WindowProfiles')
box('BlindPelmet', (-4.34, 2.56, 0), (0.18, 0.15, 5.44), 'HA_Porcelain', group='WindowProfiles')
for i in range(10):
    box('StackedBlind', (-4.31, 1.72, 1.84 + i*0.075), (0.028, 1.45, 0.082), 'HA_Ceiling', 0.007, 'WindowProfiles')

# Bedhead medical service trunking, sockets, gas outlet rings and reading strips.
for index, x in enumerate((-2.26, -0.776, 2.2), 1):
    box('Bedhead_'+str(index), (x, 1.60, 3.408), (1.28, 0.30, 0.12), 'HA_Porcelain', 0.035, 'BedheadServices')
    box('ServiceRail', (x, 1.53, 3.337), (1.15, 0.022, 0.025), 'HA_Aluminium', group='BedheadServices')
    box('ReadingDiffuser', (x, 1.748, 3.388), (1.12, 0.022, 0.10), 'HA_LED', group='BedheadServices')
    for dx in (-0.40, -0.19):
        box('OutletPlate', (x+dx, 1.61, 3.336), (0.12, 0.14, 0.02), 'HA_Trim', group='BedheadServices')
        for hole in (-0.022, 0.022):
            cylinder('OutletPin', (x+dx+hole, 1.625, 3.322), 0.008, 0.006, 'HA_Gasket', 'BedheadServices', 'Z')
    for dx, mat in ((0.13, 'HA_Oxygen'), (0.33, 'HA_Amber')):
        cylinder('GasRing', (x+dx, 1.625, 3.332), 0.048, 0.03, mat, 'BedheadServices', 'Z')
        cylinder('GasSocket', (x+dx, 1.625, 3.312), 0.025, 0.014, 'HA_Aluminium', 'BedheadServices', 'Z')
    box('NurseCall', (x+0.52, 1.62, 3.327), (0.075, 0.13, 0.03), 'HA_Blue', group='BedheadServices')

# Return grilles with individually modelled fins, smoke sensors and sprinklers.
for x, z in ((-1.1, -1.0), (2.6, -1.8)):
    box('AirReturnFrame', (x, 2.965, z), (0.62, 0.055, 0.62), 'HA_Aluminium', group='Ventilation')
    box('AirReturnVoid', (x, 2.933, z), (0.54, 0.015, 0.54), 'HA_Gasket', group='Ventilation')
    for i in range(12):
        box('AirReturnFin', (x, 2.922, z-0.242+i*0.044), (0.53, 0.023, 0.018), 'HA_Porcelain', 0.003, 'Ventilation')
for x, z in ((-0.7, 0.6), (2.6, -2.5)):
    cylinder('SmokeDetectorBase', (x, 2.958, z), 0.095, 0.045, 'HA_Porcelain', 'SafetyFixtures')
    cylinder('SmokeDetectorVent', (x, 2.929, z), 0.069, 0.018, 'HA_Gasket', 'SafetyFixtures')
    cylinder('SmokeDetectorCap', (x, 2.914, z), 0.078, 0.018, 'HA_Porcelain', 'SafetyFixtures')
for x in (-3, 0, 3):
    cylinder('SprinklerEscutcheon', (x, 2.98, -0.1), 0.045, 0.018, 'HA_Aluminium', 'SafetyFixtures')
    cylinder('SprinklerHead', (x, 2.952, -0.1), 0.017, 0.044, 'HA_Aluminium', 'SafetyFixtures')

# Door leaves are existing models. Only jambs, reveals and threshold strips are new.
for x, z in ((3.1, -3.60), (-6.03, -6.84), (-0.02, -6.84), (6.01, -6.84)):
    for dx in (-0.59, 0.59):
        box('DoorJamb', (x+dx, 1.10, z), (0.085, 2.2, 0.24), 'HA_Trim', group='DoorSurrounds')
    box('DoorHeader', (x, 2.20, z), (1.27, 0.10, 0.24), 'HA_Trim', group='DoorSurrounds')
    box('Threshold', (x, 0.008, z), (1.12, 0.016, 0.24), 'HA_Aluminium', 0.004, 'DoorSurrounds')

# Continuous sightlines beyond the original invisible gameplay boundaries.
for direction in (-1, 1):
    centre = direction * 29
    box('ExtendedFloor', (centre, -0.05, -5.325), (40, .1, 3.35), 'HA_Vinyl', 0, 'ExtendedCorridor')
    box('ExtendedCeiling', (centre, 3.05, -5.325), (40, .1, 3.35), 'HA_Ceiling', 0, 'ExtendedCorridor')
    for z in (-6.925, -3.60):
        box('ExtendedWall', (centre, 1.5, z), (40, 3, .15), group='ExtendedCorridor')
        inside = z + (.105 if z < -5 else -.105)
        wall_band('ExtensionProtection', (centre, .52, inside), (40, 1.04, .06))
        cursor = 9.0
        for bay in range(8):
            door = 11.5 + bay*4.8
            end = door-.7
            box('ExtensionHandrail', (direction*(cursor+end)/2, .87, inside + (.08 if z < -5 else -.08)), (end-cursor, .065, .065), 'HA_Oak', .02, 'ExtendedCorridorDetails')
            cursor = door+.7
        box('ExtensionHandrail', (direction*(cursor+49)/2, .87, inside + (.08 if z < -5 else -.08)), (49-cursor, .065, .065), 'HA_Oak', .02, 'ExtendedCorridorDetails')
    box('DistantEnd', (direction*49, 1.5, -5.325), (.15, 3, 3.35), group='ExtendedCorridor')
    for bay in range(8):
        x = direction * (11.5 + bay*4.8)
        box('RecessedLightHousing', (x, 2.965, -5.325), (1.25,.065,.44), 'HA_Aluminium', .01, 'ExtendedCorridorDetails')
        box('LightDiffuser', (x, 2.925, -5.325), (1.17,.014,.36), 'HA_LED', .008, 'ExtendedCorridorDetails')
        for z, inward in [(-6.82,1),(-3.71,-1)]:
            box('DoorRecess', (x,1.08,z), (1.2,2.16,.035), 'HA_Gasket', .006, 'ExtendedDoors')
            box('DoorLeaf', (x,1.07,z+inward*.027), (1.03,2.08,.04), 'HA_Oak', .015, 'ExtendedDoors')
            for dx in (-.57,.57):
                box('DoorJamb', (x+dx,1.1,z+inward*.052), (.065,2.2,.09), 'HA_Trim', .01, 'ExtendedDoors')
            box('DoorHeader', (x,2.2,z+inward*.052), (1.2,.065,.09), 'HA_Trim', .01, 'ExtendedDoors')
            box('VisionFrame', (x,1.61,z+inward*.057), (.34,.61,.025), 'HA_Aluminium', .01, 'ExtendedDoors')
            box('VisionGlass', (x,1.61,z+inward*.074), (.285,.55,.01), 'HA_Gasket', .003, 'ExtendedDoors')
            box('KickPlate', (x,.2,z+inward*.061), (.98,.29,.02), 'HA_Aluminium', .006, 'ExtendedDoors')
            box('HandlePlate', (x+.38,1.03,z+inward*.065), (.075,.22,.025), 'HA_Aluminium', .008, 'ExtendedDoors')
            box('Lever', (x+.31,1.04,z+inward*.105), (.2,.025,.035), 'HA_Aluminium', .01, 'ExtendedDoors')
            box('WardPlaque', (x,2.42,z+inward*.045), (.42,.19,.035), 'HA_Blue', .012, 'ExtendedDoors')
        # Recessed air return with individual slots, alternating with lights.
        box('VentFrame', (x+1.8,2.976,-5.325), (.58,.035,.38), 'HA_Trim', .008, 'ExtendedCorridorDetails')
        for slot in range(7):
            box('VentSlot', (x+1.8,2.955,-5.47+slot*.046), (.49,.008,.015), 'HA_Gasket', .002, 'ExtendedCorridorDetails')

# Human-scale details near the playable doors: sanitizer dispensers, call plates,
# wall protection brackets, and suspended ceiling services.
for x in (-6.03,-.02,6.01):
    box('DispenserBack', (x+.94,1.32,-6.76), (.21,.34,.07), 'HA_Trim', .025, 'ClinicalDetails')
    box('SanitizerBody', (x+.94,1.34,-6.70), (.17,.27,.105), 'HA_Porcelain', .03, 'ClinicalDetails')
    box('SanitizerWindow', (x+.94,1.35,-6.639), (.08,.13,.012), 'HA_Blue', .01, 'ClinicalDetails')
    box('SanitizerNozzle', (x+.94,1.16,-6.68), (.045,.055,.055), 'HA_Aluminium', .008, 'ClinicalDetails')
    box('DripTray', (x+.94,1.05,-6.65), (.23,.025,.16), 'HA_Trim', .018, 'ClinicalDetails')
for x in (-7,-3.5,0,3.5,7):
    box('ReturnVent', (x+1.2,2.975,-5.325), (.55,.035,.38), 'HA_Trim', .008, 'ClinicalDetails')
    for slot in range(7):
        box('ReturnSlot', (x+1.2,2.952,-5.47+slot*.046), (.47,.008,.016), 'HA_Gasket', .002, 'ClinicalDetails')
for x in range(-8,9,2):
    box('RailBracket', (x,.83,-3.755), (.035,.12,.055), 'HA_Aluminium', .008, 'ClinicalDetails')

# World-metre UVs keep surface grain at a consistent size across long walls and small trims.
for obj in owned.objects:
    uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='SurfaceMetres')
    for poly in obj.data.polygons:
        axis = max(range(3), key=lambda i: abs(poly.normal[i]))
        axes = ((1,2), (0,2), (0,1))[axis]
        for loop_index in poly.loop_indices:
            p = obj.matrix_world @ obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
            uv.data[loop_index].uv = (p[axes[0]], p[axes[1]])

# Merge by assembly to avoid hundreds of runtime GameObjects while preserving material slots.
for name, objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    scene.cursor.location = (0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in owned.objects:
    obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'HospitalArchitecture.fbx'), use_selection=True,
    object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_space_transform=True, use_mesh_modifiers=True, add_leaf_bones=False, path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'ArtSource/Hospital/HospitalArchitecture.blend'))
print(json.dumps({'assemblies':len(owned.objects), 'vertices':sum(len(o.data.vertices) for o in owned.objects),
                  'export':str(OUT / 'HospitalArchitecture.fbx')}))
