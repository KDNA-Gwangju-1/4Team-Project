"""Author a deforming nightmare hand in Blender: 16 joints, dorsal seams and a closing grip."""
from pathlib import Path
import math, json
import bpy
from mathutils import Vector, Quaternion

src = Path(__file__).with_name('build_chapter1.py').read_text(encoding='utf-8-sig')
exec(src.split('# Axis-aligned source dimensions')[0].replace('Chapter1Polish', 'NightmareGrip'))
mat('NG_Velvet', (.105,.065,.13))
mat('NG_Knuckle', (.17,.11,.18))
mat('NG_Claw', (.31,.21,.27))
mat('NG_Thread', (.57,.37,.28))
mat('NG_Ribbon', (.32,.025,.065))
mat('NG_Patch', (.18,.095,.14))

# All source coordinates use Unity axes: +Y is the back of the hand, +Z reaches forward.
ellipsoid('Metacarpal body',(0,0,.63),(.76,.29,.73),'NG_Velvet','Skin',32,20)
ellipsoid('Thenar pad',(.48,-.10,.49),(.35,.28,.44),'NG_Velvet','Skin',24,16)
ellipsoid('Wrist',(0,0,.06),(.35,.25,.34),'NG_Velvet','Skin',24,16)
bones=[('Palm',(0,0,0),(0,0,1.06),None)]
finger_names=['Little','Ring','Middle','Index']
for i,(x,total) in enumerate(zip([-.57,-.22,.18,.54],[1.27,1.57,1.68,1.48])):
    pts=[(x,0,1.05)]
    for fraction in [.43,.78,1]:
        pts.append((x*(1+.16*fraction),-.025*fraction,1.05+total*fraction))
    tube(finger_names[i]+' flesh',pts,[.175,.16,.133,.095],'NG_Velvet','Skin',20)
    for j in range(3):
        bn=finger_names[i]+'_'+str(j+1)
        bones.append((bn,pts[j],pts[j+1],'Palm' if j==0 else finger_names[i]+'_'+str(j)))
        p=pts[j]
        ellipsoid('Articular cushion',p,(.18-.02*j,.16-.018*j,.20-.015*j),'NG_Velvet','Skin',20,12)
        # Three distinct dorsal creases at each joint, plus saddle-shaped knuckle stitching.
        for offset in [-.035,0,.035]:
            tube('Knuckle crease',[(p[0]-.10,p[1]+.135-j*.013,p[2]+offset),
                 (p[0],p[1]+.165-j*.016,p[2]+offset+.014),
                 (p[0]+.10,p[1]+.135-j*.013,p[2]+offset)],[.008]*3,'NG_Knuckle','Detail',6)
    end=pts[-1]
    nail=ellipsoid('Layered nail',(end[0],end[1]+.065,end[2]-.04),(.108,.065,.19),'NG_Claw','Detail',24,12)
    tube('Hooked nail tip',[end,(end[0],end[1]-.035,end[2]+.17),(end[0],end[1]-.12,end[2]+.24)],[.082,.04,.004],'NG_Claw','Detail',12)
    for j in range(11):
        z=1.13+total*j/12
        xx=x*(1+.16*(z-1.05)/total)
        tube('Finger stitch',[(xx-.13,.06,z),(xx-.115,.09,z+.05)],[.011,.011],'NG_Thread','Detail',6)

thumb=[(.56,-.04,.38),(.98,-.075,.65),(1.15,-.10,1.03),(1.16,-.10,1.40)]
tube('Opposable thumb',thumb,[.25,.205,.16,.10],'NG_Velvet','Skin',20)
for j in range(3):
    bones.append(('Thumb_'+str(j+1),thumb[j],thumb[j+1],'Palm' if j==0 else 'Thumb_'+str(j)))
    ellipsoid('Thumb articulation',thumb[j],(.22-j*.035,.18-j*.025,.21-j*.025),'NG_Velvet','Skin',20,12)
end=thumb[-1]
ellipsoid('Thumb nail',(end[0],end[1]+.065,end[2]-.03),(.115,.065,.18),'NG_Claw','Detail',24,12)
tube('Thumb claw hook',[end,(end[0]-.015,end[1]-.04,end[2]+.16),(end[0]-.045,end[1]-.14,end[2]+.22)],[.08,.043,.004],'NG_Claw','Detail',12)

# Raised metacarpal tendons, sewn scars and a curved patch on the dorsum.
for x in [-.5,-.18,.17,.48]:
    tube('Dorsal tendon',[(x*.42,.235,.18),(x*.75,.285,.55),(x,.22,1.02)],[.025,.039,.024],'NG_Knuckle','Detail',10)
ellipsoid('Dorsal leather patch',(-.20,.278,.57),(.29,.028,.25),'NG_Patch','Detail',28,12)
for i in range(20):
    a=math.tau*i/20
    p=(-.20+.255*math.cos(a),.305,.57+.215*math.sin(a))
    q=(-.20+.29*math.cos(a),.299,.57+.25*math.sin(a))
    tube('Patch hand sewing',[p,q],[.010,.010],'NG_Thread','Detail',6)
for i in range(13):
    z=.20+i*.06
    tube('Dorsal diagonal stitches',[(.13,.29,z),(.24,.295,z+.055)],[.012,.012],'NG_Thread','Detail',6)

# Continuous flesh, with surface detail left distinct and bound to the same skeleton.
bpy.ops.object.select_all(action='DESELECT')
for o in groups['Skin']:o.select_set(True)
bpy.context.view_layer.objects.active=groups['Skin'][0];bpy.ops.object.join();skin=bpy.context.object
remesh=skin.modifiers.new('Continuous anatomical skin','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.026
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=skin.modifiers.new('Soft joint folds','SMOOTH');smooth.factor=.8;smooth.iterations=3
bpy.ops.object.modifier_apply(modifier=smooth.name)
for p in skin.data.polygons:p.use_smooth=True
bpy.ops.object.select_all(action='DESELECT')
skin.select_set(True)
for o in groups['Detail']:o.select_set(True)
bpy.context.view_layer.objects.active=skin;bpy.ops.object.join();skin=bpy.context.object;skin.name='NightmareHandSkin'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# Smart UV islands supply material coordinates that stay attached during deformation.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')

armature=bpy.data.armatures.new('NightmareAnatomy');rig=bpy.data.objects.new('GripSkeleton',armature);owned.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);skin.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail,parent in bones:
    bone=armature.edit_bones.new(name);bone.head=xyz(head);bone.tail=xyz(tail)
    if parent:bone.parent=armature.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
segments=[(name,Vector(xyz(a)),Vector(xyz(b))) for name,a,b,_ in bones]
vg={name:skin.vertex_groups.new(name=name) for name,_,_,_ in bones}
for v in skin.data.vertices:
    ds=[]
    for name,a,b in segments:
        t=max(0,min(1,(v.co-a).dot(b-a)/(b-a).length_squared))
        dist=(v.co-(a+(b-a)*t)).length_squared
        ds.append((dist,name))
    ds.sort(); ds=ds[:4];weights=[math.exp(-(d-ds[0][0])/.018) for d,n in ds];total=sum(weights)
    for (_,n),w in zip(ds,weights):vg[n].add([v.index],w/total,'REPLACE')
mod=skin.modifiers.new('Anatomical skinning','ARMATURE');mod.object=rig;skin.parent=rig

scene.render.fps=30;scene.frame_start=1;scene.frame_end=31
for frame,p in [(1,0),(10,.18),(22,.82),(31,1)]:
    for name,_,_,_ in bones:
        bone=rig.pose.bones[name];bone.rotation_mode='QUATERNION'
        if name=='Palm':q=Quaternion()
        else:
            j=int(name[-1])-1
            angle=([52,68,38] if name.startswith('Thumb') else [62,66,32])[j]*p
            # xyz includes a handedness reflection; rotation axes use its negative.
            world=Quaternion(Vector((1,0,0)),math.radians(angle))
            if name=='Thumb_1':
                world=Quaternion(Vector((0,0,1)),math.radians(35*p)) @ Quaternion(Vector((0,-1,0)),math.radians(32*p)) @ world
            rest=bone.bone.matrix_local.to_quaternion();q=rest.inverted() @ world @ rest
        bone.rotation_quaternion=q;bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
rig.animation_data.action.name='OpenToGrip'
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);skin.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'NightmareHandRig.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)

# Separate forearm can extend out of the rift without stretching fingers or their rig.
tube('Tapered forearm',[(0,0,0),(0,0,.6),(0,0,1.4),(0,0,2.1)],[.54,.50,.38,.32],'NG_Velvet','Arm',32)
for i in range(7):
    a=i*math.tau/7
    tube('Forearm cord',[(math.cos(a)*r,math.sin(a)*r,z) for r,z in [(.54,0),(.50,.6),(.38,1.4),(.32,2.1)]],[.017]*4,'NG_Knuckle','Arm',8)
for z in [1.68,1.76]:
    for i in range(48):
        a=i*math.tau/48;b=a+math.tau/48
        tube('Crimson wrist binding',[(.35*math.cos(a),.35*math.sin(a),z),(.35*math.cos(b),.35*math.sin(b),z)],[.048,.048],'NG_Ribbon','Arm',8)
for i in range(32):
    a=i*math.tau/32
    tube('Cuff stitches',[(.39*math.cos(a),.39*math.sin(a),1.66),(.39*math.cos(a+.03),.39*math.sin(a+.03),1.80)],[.013,.013],'NG_Thread','Arm',6)
bpy.ops.object.select_all(action='DESELECT')
for o in groups['Arm']:o.select_set(True)
bpy.context.view_layer.objects.active=groups['Arm'][0];bpy.ops.object.join();arm=bpy.context.object;arm.name='Forearm'
bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.fbx(filepath=str(OUT/'Forearm.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,bake_anim=False)
scene.frame_set(31)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'NightmareGrip.blend'))
(SOURCE/'Build.json').write_text(json.dumps({'bones':len(bones),'vertices':len(skin.data.vertices),'action':'OpenToGrip','frames':31},indent=2))
print('Rigged hand saved:',len(bones),'bones',len(skin.data.vertices),'vertices')
