"""Blender source for metre-scale felt scenery and the nightmare doll's grasp."""
import bpy
import math
import random
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/BrightDream/Art/Chapter1Polish'
SOURCE = ROOT / 'ArtSource/Chapter1Polish'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.get('Chapter1Polish') or bpy.data.scenes.new('Chapter1Polish')
bpy.context.window.scene = scene
owned = bpy.data.collections.get('Chapter1Polish_Generated')
if owned:
    for obj in list(owned.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(owned)
owned = bpy.data.collections.new('Chapter1Polish_Generated')
scene.collection.children.link(owned)
scene.unit_settings.system = 'METRIC'
mats = {}
groups = {}
rng = random.Random(731)

def mat(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = .87
    mats[name] = m

for name, color in {
    'CP_FeltPine':(.09,.30,.21), 'CP_FeltMint':(.28,.52,.36),
    'CP_FeltSage':(.42,.58,.36), 'CP_FeltMoss':(.29,.43,.17),
    'CP_Trunk':(.38,.22,.12), 'CP_ThreadGold':(.79,.64,.30),
    'CP_ThreadCream':(.82,.73,.53), 'CP_StoneLavender':(.53,.48,.60),
    'CP_StoneCream':(.78,.72,.58), 'CP_StoneRose':(.67,.52,.55),
    'CP_StoneBlue':(.46,.58,.60), 'CP_Earth':(.28,.23,.20),
    'CP_DollSkin':(.035,.026,.045), 'CP_DollKnuckle':(.085,.055,.09),
    'CP_DollClaw':(.16,.12,.15), 'CP_DollRibbon':(.31,.035,.065),
}.items(): mat(name,color)

def xyz(p):
    return (-p[0], -p[2], p[1])

def own(obj, name, group, material):
    obj.name = name
    for c in list(obj.users_collection): c.objects.unlink(obj)
    owned.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(mats[material])
    groups.setdefault(group, []).append(obj)
    return obj

def ellipsoid(name, center, size, material, group, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=xyz(center))
    o=own(bpy.context.object,name,group,material)
    o.scale=(size[0],size[2],size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for p in o.data.polygons: p.use_smooth=True
    return o

def tube(name, points, radii, material, group, sides=8):
    if len(points) > 2:
        original=[Vector(p) for p in points]; rr=list(radii); points=[]; radii=[]
        for i in range(len(original)-1):
            p0=original[max(0,i-1)];p1=original[i];p2=original[i+1];p3=original[min(len(original)-1,i+2)]
            for step in range(4):
                t=step/4
                points.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
                radii.append(rr[i]*(1-t)+rr[i+1]*t)
        points.append(original[-1]);radii.append(rr[-1])
    pts=[Vector(xyz(p)) for p in points]
    verts=[]
    for i,p in enumerate(pts):
        tangent=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
        reference=Vector((0,0,1)) if abs(tangent.z)<.95 else Vector((0,1,0))
        u=tangent.cross(reference).normalized(); v=tangent.cross(u).normalized()
        for j in range(sides):
            a=j*math.tau/sides
            verts.append(p+radii[i]*(math.cos(a)*u+math.sin(a)*v))
    faces=[]
    for i in range(len(pts)-1):
        for j in range(sides):
            a=i*sides+j; b=i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+j for j in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);owned.objects.link(o)
    mesh.materials.append(mats[material]);groups.setdefault(group,[]).append(o)
    for p in mesh.polygons:p.use_smooth=True
    return o

def stitch_ring(group, center, rx, ry, z, material, count=30):
    for i in range(count):
        a=i*math.tau/count; b=a+.055
        tube('Running stitch',[(center[0]+rx*math.cos(a),center[1]+ry*math.sin(a),z),
                              (center[0]+rx*math.cos(b),center[1]+ry*math.sin(b),z)],[.013,.013],material,group,5)

def tree(group,x,z,h,rx=.7):
    tube('Bark trunk',[(x,0,z),(x,.5*h,z),(x+.05,.77*h,z)], [.22,.16,.09], 'CP_Trunk',group,12)
    for j in range(3):
        y=h*(.55+j*.12)
        xx=x+(-.26 if j==0 else .24 if j==1 else 0)
        rad=rx*(1 if j<2 else .85)
        color=['CP_FeltPine','CP_FeltMint','CP_FeltSage'][j]
        ellipsoid('Padded leaf', (xx,y,z), (rad,h*.27,.48),color,group)
        # Running stitches live on the front and back padded faces.
        for sign in (-1,1):
            stitch_ring(group,(xx,y),rad*.84,h*.23,z+sign*.27,'CP_ThreadGold',26)
    for j in (-1,0,1):
        tube('Bark thread',[(x+j*.085,.1,z-.18),(x+j*.075,.9,z-.14),(x+j*.05,1.4,z-.1)], [.013]*3,'CP_ThreadGold',group,5)

# Axis-aligned source dimensions match the old wall assets (metres).
for i in range(8): tree('Treeline',0,-5.15+i*1.47,4.7+(i%3)*.35,.74)
tree('HalfTree',0,0,5.65,1.55)
for i in range(7):
    x=-5.1+i*1.7
    ellipsoid('Layered felt hill',(x,.55,0),(1.7,1.1+(i%3)*.32,1.1),['CP_FeltMoss','CP_FeltSage','CP_FeltMint'][i%3],'Hills')
    stitch_ring('Hills',(x,.55),1.47,.93+(i%3)*.27,-.68,'CP_ThreadCream',28)
for i in range(6):
    x=-1.6+i*.64
    ellipsoid('Padded hedge',(x,.72,0),(.57,.65+(i%2)*.17,.47),['CP_FeltPine','CP_FeltMint'][i%2],'BushRow')
    stitch_ring('BushRow',(x,.72),.46,.56,-.28,'CP_ThreadGold',20)

# Arena: individually bevelled paving stones, retaining the existing top height.
def wedge(name,r0,r1,a0,a1,top,material):
    n=5
    outline=[(r*math.cos(a),r*math.sin(a)) for r,angles in [(r1,[a0+(a1-a0)*i/n for i in range(n+1)]),(r0,[a1-(a1-a0)*i/n for i in range(n+1)])] for a in angles]
    verts=[xyz((x,y,z)) for y in (top-.22,top) for x,z in outline]
    k=len(outline); faces=[tuple(reversed(range(k))),tuple(range(k,2*k))]
    faces += [(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);owned.objects.link(o);mesh.materials.append(mats[material]);groups.setdefault('Arena',[]).append(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bevel=o.modifiers.new('Soft sewn stone edges','BEVEL');bevel.width=.07;bevel.segments=3
    bpy.ops.object.modifier_apply(modifier=bevel.name);o.select_set(False)
    for p in o.data.polygons:p.use_smooth=True
    normal=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    bpy.ops.object.modifier_apply(modifier=normal.name)

for ring,(r0,r1,n) in enumerate([(0.001,2.1,8),(2.18,4.9,16),(4.98,7.75,24),(7.83,10.6,32),(10.68,13.5,40),(13.58,15.8,48)]):
    for i in range(n):
        a=(i+(.5 if ring%2 else 0))*math.tau/n
        wedge('Paving_%d_%02d'%(ring,i),r0,r1,a+.003,a+math.tau/n-.003,.87-rng.random()*.015,
              ['CP_StoneLavender','CP_StoneCream','CP_StoneRose','CP_StoneBlue'][(i+ring)%4])
ellipsoid('Raised moss border',(0,-.20,0),(17.96,1.03,16.19),'CP_FeltMoss','Arena',64,16)
# Radius on the centre stones is kept flatter than the underlying cushion.
for i in range(144):
    a=i*math.tau/144
    tube('Border running stitch',[(17.15*math.cos(a),.46,15.48*math.sin(a)),(17.15*math.cos(a+.016),.46,15.48*math.sin(a+.016))],[.028,.028],'CP_ThreadCream','Arena',6)

# Nightmare hand: a padded palm and articulated, curled fingers, not wire bones.
ellipsoid('Palm flesh',(0,-.48,2.55),(.72,.32,.72),'CP_DollSkin','Hand')
ellipsoid('Thumb mound',(.54,-.30,2.47),(.31,.31,.44),'CP_DollSkin','Hand')
for i,x in enumerate([-.56,-.20,.19,.52]):
    length=[1.30,1.57,1.49,1.19][i]
    points=[(x,-.45,2.76),(x*1.18,-.28,3.12),(x*1.30,.12,3.12+length*.48),
            (x*1.15,.57,3.12+length*.67),(x*.88,.79,3.05+length*.40)]
    tube('Fleshed finger',points,[.21,.20,.165,.13,.085],'CP_DollSkin','Hand',12)
    for p,rad in zip(points[1:4],[.21,.17,.14]):ellipsoid('Knuckle',p,(rad,rad,rad),'CP_DollKnuckle','Hand',12,8)
    end=points[-1]
    tube('Curved claw',[end,(end[0]*.9,.72,end[2]-.25),(end[0]*.83,.56,end[2]-.46)],[.091,.065,.003],'CP_DollClaw','Hand',10)
    tube('Finger embroidered seam',[(p[0]-.035,p[1]+.07,p[2]) for p in points],[.011]*5,'CP_ThreadCream','Hand',5)
thumb=[(.58,-.32,2.46),(.93,-.16,2.80),(1,.18,3.21),(.72,.45,3.43),(.48,.46,3.32)]
tube('Opposing thumb',thumb,[.25,.23,.19,.13,.045],'CP_DollSkin','Hand',12)
for p in thumb[1:4]:ellipsoid('Thumb joint',p,(.17,.17,.18),'CP_DollKnuckle','Hand',12,8)
tube('Thumb claw',[thumb[-1],(.31,.39,3.15),(.23,.24,3.01)],[.08,.045,.003],'CP_DollClaw','Hand',10)
# Woven arm, tapered wrist, visible tendons and red cloth cuff.
tube('Forearm flesh',[(0,0,0),(.02,-.11,.55),(0,-.23,1.22),(0,-.38,2.10)],[.61,.54,.40,.34],'CP_DollSkin','Arm',20)
for i in range(7):
    a=i*math.tau/7
    tube('Stitched tendon',[(math.cos(a)*r,math.sin(a)*r-y,z) for z,r,y in [(0,.62,0),(.55,.55,.11),(1.22,.41,.23),(2.1,.35,.38)]],[.018]*4,'CP_DollKnuckle','Arm',6)
for i in range(30):
    a=i*math.tau/30
    tube('Wrist blanket stitch',[(.37*math.cos(a),-.38+.37*math.sin(a),1.94),(.37*math.cos(a+.05),-.38+.37*math.sin(a+.05),2.07)],[.018,.018],'CP_ThreadCream','Hand',6)
for i in range(3):
    a=i*math.tau/3
    ellipsoid('Crimson cuff patch',(.31*math.cos(a),-.38+.31*math.sin(a),2.10),(.16,.10,.25),'CP_DollRibbon','Hand',12,8)
for i in range(9):
    x=-.5+i*.125
    tube('Palm cross stitch',[(x,-.765,2.33),(x+.08,-.79,2.47)],[.016,.016],'CP_ThreadCream','Hand',5)
    tube('Palm cross stitch',[(x+.08,-.77,2.33),(x,-.79,2.47)],[.016,.016],'CP_ThreadCream','Hand',5)

# Merge assemblies, generate a metre UV set and export each asset independently.
# Fuse the padded anatomy into continuous flesh while leaving embroidery separate.
flesh=[o for o in groups['Hand'] if o.data.materials[0].name in ('CP_DollSkin','CP_DollKnuckle')]
bpy.ops.object.select_all(action='DESELECT')
for o in flesh:o.select_set(True)
bpy.context.view_layer.objects.active=flesh[0]
bpy.ops.object.join();skin=bpy.context.object
remesh=skin.modifiers.new('Continuous padded anatomy','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.035
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=skin.modifiers.new('Soft cloth surface','SMOOTH');smooth.factor=1;smooth.iterations=4
bpy.ops.object.modifier_apply(modifier=smooth.name)
for p in skin.data.polygons:p.use_smooth=True
groups['Hand']=[o for o in owned.objects if o not in groups['Arm'] and (o==skin or o.name.startswith(('Curved claw','Finger embroidered seam','Thumb claw','Wrist blanket stitch','Crimson cuff patch','Palm cross stitch'))) ]
report=[]
for name, objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    uv=o.data.uv_layers.new(name='SurfaceMetres')
    for p in o.data.polygons:
        axis=max(range(3),key=lambda k:abs(p.normal[k])); axes=((1,2),(0,2),(0,1))[axis]
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(co[axes[0]],co[axes[1]])
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False)
    report.append({'name':name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons)})
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Chapter1Polish.blend'))
(SOURCE/'BlenderBuild.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
