"""Continuous, bordered garden paving and a grounded elliptical boss terrace."""
from pathlib import Path
import bpy, math, json
from mathutils import Vector
base=Path(__file__).with_name('build_props.py').read_text(encoding='utf-8-sig')
exec(base.split('# Joinery chest:')[0].replace('Chapter1Props','GardenPaving'))
for name,color in {'Ivory':(.72,.65,.49),'Limestone':(.66,.60,.47),'Sage':(.36,.49,.41),'Rose':(.57,.38,.36),'Grout':(.29,.32,.24)}.items():
    m=bpy.data.materials.new('GP_'+name);m.diffuse_color=(*color,1);m.use_nodes=True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1)
    mats[name]=m

def slab(g,name,outline,bottom,top,material,bevel=.015):
    # Outline is counterclockwise in Unity XZ; reflection in xyz is accounted by normals.
    verts=[xyz((x,y,z)) for y in [bottom,top] for x,z in outline]
    n=len(outline);faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);col.objects.link(o);me.materials.append(mats[material]);groups.setdefault(g,[]).append(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    if bevel:
        m=o.modifiers.new('Dressed stone arris','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('Planar stone normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=m.name)
    o.select_set(False);return o

def curve(control):
    pts=[Vector(p) for p in control];samples=[]
    for i in range(len(pts)-1):
        a,b,c,d=pts[max(i-1,0)],pts[i],pts[i+1],pts[min(i+2,len(pts)-1)]
        for j in range(30):
            t=j/30
            samples.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
    samples.append(pts[-1]);lengths=[0]
    for i in range(1,len(samples)):lengths.append(lengths[-1]+(samples[i]-samples[i-1]).length)
    def at(s):
        s=max(0,min(s,lengths[-1]));k=1
        while k<len(lengths)-1 and lengths[k]<s:k+=1
        t=(s-lengths[k-1])/max(.00001,lengths[k]-lengths[k-1])
        p=samples[k-1].lerp(samples[k],t);direction=(samples[k]-samples[k-1]).normalized()
        return p,Vector((direction.y,-direction.x))
    return at,lengths[-1]

routes=[
    [(-.1,1),(-.4,8),(-.1,11),(.7,14),(2.3,17),(5,20),(8.2,21.8),(12.7,22.5),(15.5,22.8),(17.08,22.87)],
    [(20.66,26.74),(21.7,29),(22.1,33),(22,35),(20.7,39),(20.7,42),(21.5,46),(22,50),(22,52.6),(20,55.7)],
]
arenaCenter=Vector((8.07,67.61));rx,rz=17.96,16.19
def inside(p):return ((p.x-arenaCenter.x)/rx)**2+((p.y-arenaCenter.y)/rz)**2<1.002
for route,control in enumerate(routes):
    at,length=curve(control)
    if route==1:
        while inside(at(length)[0]):length-=.02
    def quad(s0,s1,l,r):
        p,n=at(s0);q,m=at(s1)
        return [tuple(p+n*l),tuple(q+m*l),tuple(q+m*r),tuple(p+n*r)]
    steps=math.ceil(length/.76);step=length/steps
    for row in range(steps):
        s0=row*step;s1=(row+1)*step
        slab('GardenPath','Continuous mortar bed',quad(s0,s1,-1.04,1.04),0,.026,'Grout',0)
        for lane in range(2):
            left=-.87+lane*.87+.018;right=left+.834
            slab('GardenPath','Aligned limestone paver',quad(s0+.018,s1-.018,left,right),.012,.062,'Ivory' if (row+lane)%5 else 'Limestone')
        for side in [-1,1]:
            lo,hi=sorted([side*.89,side*1.035])
            # Leave an intentional mouth toward the stage-2 arena.
            z=at((s0+s1)*.5)[0].y
            if route==1 and side==1 and 36.5<z<38.5:continue
            slab('GardenPath','Sage border course',quad(s0+.012,s1-.012,lo,hi),.005,.067,'Sage',.012)
# Short branch to the stage-2 entrance, stopping at the main path edge.
for row in range(3):
    x=22.55+row*.72
    for lane in range(2):
        z=36.64+lane*.87
        slab('GardenPath','Arena branch',[(x,z),(x+.69,z),(x+.69,z+.83),(x,z+.83)],0,.062,'Ivory')

# Elliptical terrace: a solid base fills all space below paving down to the ground.
outline=[(rx*math.cos(i*math.tau/192),rz*math.sin(i*math.tau/192)) for i in range(192)]
slab('ArenaTerrace','Grounded plinth',outline,0,.118,'Grout',.025)
def sector(r0,r1,a0,a1,material,top=.16):
    points=[(rx*r*math.cos(a),rz*r*math.sin(a)) for r,angles in [(r1,[a0+(a1-a0)*i/5 for i in range(6)]),(r0,[a1-(a1-a0)*i/5 for i in range(6)])] for a in angles]
    slab('ArenaTerrace','Garden stone course',points,.09,top,material,.012)
for ring,(r0,r1,count) in enumerate([(.09,.29,16),(.292,.49,24),(.492,.69,32),(.692,.87,40),(.872,.964,48)]):
    for i in range(count):
        a=(i+(.5 if ring%2 else 0))*math.tau/count
        material='Sage' if ring==4 else ('Limestone' if (i+ring)%6==0 else 'Ivory')
        sector(r0,r1,a+.001,a+math.tau/count-.001,material)
for i in range(64):
    a=i*math.tau/64;sector(.967,.997,a+.001,a+math.tau/64-.001,'Ivory',.165)
for i in range(8):
    a=i*math.tau/8;sector(.002,.087,a+.004,a+math.tau/8-.004,'Rose' if i%2 else 'Sage')

report=[]
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False)
    report.append({'name':name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons)})
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'GardenPaving.blend'))
(SRC/'Build.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
