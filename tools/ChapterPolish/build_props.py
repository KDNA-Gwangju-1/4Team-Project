"""Blender-authored craft props; exports only the collection owned by this script."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/BrightDream/Art/Chapter1Props';OUT.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'ArtSource/Chapter1Props';SRC.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.get('Chapter1Props') or bpy.data.scenes.new('Chapter1Props')
bpy.context.window.scene=scene
col=bpy.data.collections.get('Chapter1Props_Generated')
if col:
    for o in list(col.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(col)
col=bpy.data.collections.new('Chapter1Props_Generated');scene.collection.children.link(col)
mats={};groups={}
for name,color in {'Oak':(.53,.32,.15),'Honey':(.70,.47,.25),'Endgrain':(.39,.22,.11),'Cream':(.84,.75,.56),'Mint':(.28,.53,.43),'Rose':(.63,.31,.34),'Brass':(.62,.45,.22),'Iron':(.16,.22,.22)}.items():
    m=bpy.data.materials.get('CProp_'+name) or bpy.data.materials.new('CProp_'+name)
    m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.7
    mats[name]=m
def xyz(p):return (-p[0],-p[2],p[1])
def own(o,name,g,m):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);o.data.materials.append(mats[m]);groups.setdefault(g,[]).append(o)
    return o
def box(g,name,p,size,m,bevel=.025,angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p))
    o=own(bpy.context.object,name,g,m);o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Rounded craft edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    o.rotation_euler[1]=angle
    return o
def tube(g,name,points,r,m,sides=10):
    pts=[Vector(xyz(p)) for p in points];vs=[]
    for i,p in enumerate(pts):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized();ref=Vector((0,0,1)) if abs(t.z)<.95 else Vector((0,1,0));u=t.cross(ref).normalized();v=t.cross(u).normalized()
        for j in range(sides):a=math.tau*j/sides;vs.append(p+r*(math.cos(a)*u+math.sin(a)*v))
    fs=[]
    for i in range(len(pts)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+j for j in range(sides))])
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);col.objects.link(o);me.materials.append(mats[m]);groups.setdefault(g,[]).append(o)
    for p in me.polygons:p.use_smooth=True
def stud(g,p,r=.024,m='Brass'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,radius=r,location=xyz(p));o=own(bpy.context.object,'Round fastener',g,m)
    for f in o.data.polygons:f.use_smooth=True
def grain(g,x,y,z,length,face='front'):
    for offset in [-.025,.018]:
        pts=[(x-length/2+i*length/8,y+offset+.004*math.sin(i*1.4),z) for i in range(9)]
        tube(g,'Inlaid wood grain',pts,.0025,'Endgrain',5)

# Joinery chest: individual boards, strengthened corners, handles and a clasp.
g='CraftCrate'
for z in [-.43,.43]:
    for j in range(4):
        y=.13+j*.21;box(g,'Side plank',(0,y,z),(1.05,.195,.08),'Honey' if j%2 else 'Oak');grain(g,0,y,z+(.044 if z>0 else -.044),.88)
for x in [-.51,.51]:
    for j in range(4):box(g,'End plank',(x,.13+j*.21,0),(.08,.195,.78),'Honey')
    tube(g,'Carry handle',[(x*1.09,.48,-.16),(x*1.15,.56,-.16),(x*1.15,.56,.16),(x*1.09,.48,.16)],.022,'Iron')
for x in [-.44,.44]:
    for z in [-.47,.47]:
        box(g,'Corner binding',(x,.44,z),(.105,.85,.065),'Cream',.018)
        for y in [.12,.40,.74]:stud(g,(x,y,z*1.075))
for i in range(5):box(g,'Lid board',(-.42+i*.21,.91,0),(.195,.12,.92),'Honey')
for x in [-.32,.32]:box(g,'Lid strap',(x,.98,0),(.10,.045,.98),'Mint',.018)
box(g,'Brass clasp',(0,.79,-.493),(.15,.23,.045),'Brass',.02)
box(g,'Clasp inset',(0,.79,-.522),(.038,.065,.018),'Iron',.01)
for x in [-.31,.31]:box(g,'Rear hinge',(x,.82,.488),(.17,.17,.04),'Brass',.014)

# Park bench with five seat slats, shaped armrests, back rails and bolts.
g='CraftBench'
for x in [-.70,.70]:
    for z in [-.25,.27]:box(g,'Tapered leg',(x,.25,z),(.12,.50,.13),'Mint',.04)
    box(g,'Seat support',(x,.48,0),(.13,.12,.85),'Mint')
    box(g,'Back upright',(x,.78,.33),(.10,.80,.11),'Mint')
    tube(g,'Rounded armrest',[(x,.49,-.29),(x,.78,-.28),(x,.81,-.18),(x,.81,.22),(x,.87,.31)],.05,'Cream',12)
for i in range(5):
    z=-.32+i*.16;box(g,'Seat slat',(0,.54,z),(1.70,.09,.145),'Honey',.025)
    for x in [-.7,.7]:stud(g,(x,.59,z),.018)
for i in range(3):
    y=.74+i*.155;box(g,'Back slat',(0,y,.34),(1.70,.135,.075),'Honey',.03);grain(g,0,y,.295,1.48)
    for x in [-.70,.70]:stud(g,(x,y,.29),.02)
box(g,'Name plaque',(0,.91,.287),(.30,.11,.022),'Cream',.015)
for x in [-.12,.12]:stud(g,(x,.91,.27),.012)

# Framed two-sided sign, geometric direction arrow and a little flower medallion.
g='CraftSign'
box(g,'Post',(0,.73,0),(.15,1.46,.16),'Oak',.04)
box(g,'Foot collar',(0,.13,0),(.28,.26,.28),'Mint',.04)
box(g,'Framed board',(0,1.35,0),(1.32,.68,.18),'Honey',.07)
for z in [-.105,.105]:
    box(g,'Recessed mint panel',(0,1.35,z),(1.17,.52,.035),'Mint',.07)
    side=1 if z>0 else -1
    zz=z+side*.031
    tube(g,'Direction arrow',[(-.30,1.35,zz),(.30,1.35,zz),(.14,1.50,zz)],.025,'Cream',8)
    tube(g,'Arrow lower',[ (.30,1.35,zz),(.14,1.20,zz)],.025,'Cream',8)
    for x in [-.53,.53]:
        for y in [1.15,1.55]:stud(g,(x,y,zz),.017)
box(g,'Top cap',(0,1.75,0),(.22,.16,.23),'Cream',.045)
for i in range(5):
    a=i*math.tau/5;stud(g,(.072*math.cos(a),1.75+.072*math.sin(a),-.13),.047,'Rose')
stud(g,(0,1.75,-.16),.038,'Brass')

# Arched plank bridge: walking surface matches the old shallow arch profile.
g='CraftBridge'
for i in range(12):
    z=-1.65+i*.30;t=i/11;y=.09+.33*math.sin(math.pi*t)
    box(g,'Individual deck plank',(0,y,z),(1.75,.12,.283),'Honey' if i%3 else 'Oak',.03)
    for x in [-.73,.73]:stud(g,(x,y+.061,z),.018)
for x in [-.89,.89]:
    for z in [-1.66,0,1.66]:
        y=.09+.33*math.sin(math.pi*((z+1.66)/3.32))
        box(g,'Rail post',(x,y+.48,z),(.16,.96,.16),'Oak',.045)
        box(g,'Post cap',(x,y+.98,z),(.24,.11,.24),'Cream',.04)
        for h in [.18,.71]:
            # Rope windings are actual geometry, not painted stripes.
            tube(g,'Post rope',[(x+.11*math.cos(j*math.tau/24),y+h+j*.0016,z+.11*math.sin(j*math.tau/24)) for j in range(25)],.018,'Cream',6)
    for height in [.57,.91]:
        tube(g,'Swept handrail',[(x,.09+.33*math.sin(math.pi*j/20)+height,-1.66+3.32*j/20) for j in range(21)],.048,'Honey',12)
    tube(g,'Arched support beam',[(x*.83,-.02+.33*math.sin(math.pi*j/20),-1.68+3.36*j/20) for j in range(21)],.065,'Endgrain',10)

report=[]
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False)
    report.append({'model':name,'vertices':len(o.data.vertices)})
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Chapter1Props.blend'))
(SRC/'BlenderBuild.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
