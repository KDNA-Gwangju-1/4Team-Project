"""Centred stepping stones and a grounded base for the existing perimeter hedge."""
from pathlib import Path
base=(Path(__file__).parent/'build_props.py').read_text(encoding='utf-8')
exec(base.split('# Joinery chest:')[0].replace('Chapter1Props','Chapter1Wayfinding'))
for i in range(3):
    g='PathStone'+str(i)
    o=box(g,'Bevelled stone',(0,.032,0),(.40-i*.012,.064,.34+i*.01),'Cream',.045)
    # Irregular outline, kept centred at its own ground pivot.
    for v in o.data.vertices:
        v.co.x *= 1+.06*math.sin(v.co.y*13+i)
    o.data.update()
g='HedgeFoot'
box(g,'Grounded moss bank',(0,.64,0),(12,1.28,3.2),'Mint',.30)
report=[]
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:bpy.ops.object.join()
    o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False)
    report.append({'name':name,'vertices':len(o.data.vertices)})
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Chapter1Wayfinding.blend'))
(SRC/'BlenderBuild.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
