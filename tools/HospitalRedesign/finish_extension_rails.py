import bpy, bmesh
from pathlib import Path
root = Path(__file__).resolve().parents[2]
scene = bpy.data.scenes['HospitalArchitecture']
bpy.context.window.scene = scene
owned = bpy.data.collections['HospitalArchitecture_Generated']
obj = bpy.data.objects['ExtendedCorridorDetails']
bm=bmesh.new(); bm.from_mesh(obj.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 1.2], context='VERTS')
bm.to_mesh(obj.data); bm.free()
parts=[obj]
for direction in (-1,1):
    for z in (-6.74,-3.785):
        cursor=9
        intervals=[]
        for bay in range(8):
            door=11.5+bay*4.8
            intervals.append((cursor,door-.7));cursor=door+.7
        intervals.append((cursor,49))
        for start,end in intervals:
            bpy.ops.mesh.primitive_cube_add(size=1, location=(-direction*(start+end)/2,-z,.87))
            rail=bpy.context.object;rail.dimensions=(end-start,.065,.065)
            bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
            rail.data.materials.append(bpy.data.materials['HA_Oak'])
            mod=rail.modifiers.new('Rounded rail','BEVEL');mod.width=.019;mod.segments=3
            bpy.ops.object.modifier_apply(modifier=mod.name)
            for coll in list(rail.users_collection):coll.objects.unlink(rail)
            owned.objects.link(rail);parts.append(rail)
bpy.ops.object.select_all(action='DESELECT')
for part in parts:part.select_set(True)
bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
bpy.ops.object.select_all(action='DESELECT')
for part in owned.objects:part.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'Assets/Art/Models/HospitalArchitecture/HospitalArchitecture.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False,path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/Hospital/HospitalArchitecture.blend'))
print('Door-clearance rails exported')
