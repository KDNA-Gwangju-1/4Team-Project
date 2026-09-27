"""V10: foot-contact driven rear walk and a shared 2.5D corridor camera.

Uses the local Python/FFmpeg dependencies documented for v9. No Unity changes.
Run: python render_v10.py [--preview] [--smooth-export]
"""
import argparse, json, math, subprocess
import render_v9 as old
from render_v9 import np, cv2, HERE, ROOT, SW, SH, TOP, FLOOR, CENTER, W, H, FPS

WORK=ROOT/'output/ending-v10'
WORK.mkdir(parents=True,exist_ok=True)
DURATION=13.4
clamp,smooth,mix=old.clamp,old.smooth,old.mix

def smoother(u):
    u=clamp(u)
    return u*u*u*(u*(u*6-15)+10)

class Camera:
    """Ray/plane reprojection of the painted corridor, not a full 3D set."""
    def __init__(self):
        yy,xx=np.mgrid[0:H:2,0:W:2]
        self.rx=(xx.astype(np.float32)-old.CX)/old.FOCAL
        self.ry=(old.CY-yy.astype(np.float32))/old.FOCAL

    def set_time(self,t):
        # The dolly continues softly through the stop; no looping camera shake.
        self.z=.38*smoother((t-.2)/5.5)+.08*smoother((t-7.1)/2.7)
        self.x=.05*smoother((t-9.15)/2.25)
        self.yaw=.020*smoother((t-9.10)/2.30)

    def project(self,x,y,z):
        x,z=x-self.x,z-self.z
        c,s=math.cos(self.yaw),math.sin(self.yaw)
        xx,zz=c*x-s*z,s*x+c*z
        return np.array([old.CX+old.FOCAL*xx/zz,old.CY+old.FOCAL*(old.EYE-y)/zz])

    def scale(self,x,z):
        depth=math.sin(self.yaw)*(x-self.x)+math.cos(self.yaw)*(z-self.z)
        return old.FOCAL*old.HUMAN/depth/(FLOOR-TOP)

    def background(self,plate):
        c,s=math.cos(self.yaw),math.sin(self.yaw)
        rx=c*self.rx+s;rz=c-s*self.rx;ry=self.ry
        far=(old.DZ-self.z)/rz
        floor=np.divide(-old.EYE,ry,out=np.full_like(ry,1e5),where=ry<-.0001)
        ceiling=np.divide(3.15-old.EYE,ry,out=np.full_like(ry,1e5),where=ry>.0001)
        left=np.divide(-2.08-self.x,rx,out=np.full_like(rx,1e5),where=rx<-.0001)
        right=np.divide(1.48-self.x,rx,out=np.full_like(rx,1e5),where=rx>.0001)
        depth=np.minimum.reduce([far,floor,ceiling,left,right])
        wx=self.x+depth*rx;wy=old.EYE+depth*ry;wz=self.z+depth*rz
        sx=(old.CX+old.FOCAL*wx/wz).astype(np.float32)
        sy=(old.CY+old.FOCAL*(old.EYE-wy)/wz).astype(np.float32)
        # Upsample a smooth coordinate map, not the background texture.
        gx,gy=np.meshgrid(np.arange(W,dtype=np.float32)/2,np.arange(H,dtype=np.float32)/2)
        self.mx=cv2.remap(sx,gx,gy,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
        self.my=cv2.remap(sy,gx,gy,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
        # A half-texel edge footprint is legal for the bilinear sampler.
        self.outside=int(np.count_nonzero((self.mx<-.5)|(self.mx>W-.5)|(self.my<-.5)|(self.my>H-.5)))
        return cv2.remap(plate,self.mx,self.my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)


def tps(cel,source,target):
    """Deform one drawing; never dissolve two different leg silhouettes."""
    dst=np.float64(target)/512;src=np.float64(source)/512;n=len(dst)
    r2=np.sum((dst[:,None]-dst[None,:])**2,axis=2)
    K=r2*np.log(r2+1e-12)+np.eye(n)*2e-7
    P=np.column_stack([np.ones(n),dst])
    weights=np.linalg.solve(np.block([[K,P],[P.T,np.zeros((3,3))]]),np.vstack([src,np.zeros((3,2))]))
    yy,xx=np.mgrid[0:SH:4,0:SW:4]
    q=np.column_stack([xx.ravel(),yy.ravel()])/512
    r2=np.sum((q[:,None]-dst[None,:])**2,axis=2)
    basis=np.column_stack([r2*np.log(r2+1e-12),np.ones(len(q)),q])
    coords=(basis@weights*512).reshape(SH//4,SW//4,2).astype(np.float32)
    mx=cv2.remap(coords[:,:,0],old.GRID_X/4,old.GRID_Y/4,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
    my=cv2.remap(coords[:,:,1],old.GRID_X/4,old.GRID_Y/4,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
    return cv2.remap(cel,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)


def knee_ik(hip,ankle,upper=.45,lower=.39):
    """Two-bone solution in the sagittal plane; positive Z is knee-forward."""
    delta=ankle[1:]-hip[1:];d=float(np.linalg.norm(delta))
    d=max(.001,min(d,upper+lower-.0001));axis=delta/np.linalg.norm(delta)
    along=(upper*upper-lower*lower+d*d)/(2*d)
    height=math.sqrt(max(0,upper*upper-along*along))
    bend=np.array([axis[1],-axis[0]])
    knee=hip.copy();knee[1:]=hip[1:]+along*axis+height*bend
    return knee


class Actor(old.Animator):
    def __init__(self):
        super().__init__()
        # Coordinates are calibrated against the registered idle drawing.
        self.fixed=[(0,0),(319,0),(0,511),(319,511),(160,30),(160,70),
                    (153,108),(111,126),(204,125),(150,190),(91,320),(220,321)]
        self.left=[(129,254),(128,374),(126,448),(113,468),(140,468)]
        self.right=[(183,254),(184,374),(186,448),(173,468),(200,468)]
        self.arms=[(95,198),(94,254),(220,199),(224,251)]
        self.source=np.float64(self.fixed+self.left+self.right+self.arms)
        self.errors=[]
        self.rear_body=self.cels['a0'].copy()
        self.rear_body*=np.clip((454-old.GRID_Y)/7,0,1)[:,:,None]
        self.rear_boots=[]
        for lo,hi in [(105,150),(163,211)]:
            boot=self.cels['a0'].copy()
            boot*=((old.GRID_X>=lo)&(old.GRID_X<hi))[:,:,None]
            boot*=np.clip((old.GRID_Y-442)/6,0,1)[:,:,None]
            self.rear_boots.append(boot)
        side=self.cels['s0']
        self.side_legs=[side.copy(),side.copy()]
        self.side_legs[0]*=((old.GRID_Y>=320)&(old.GRID_X<160))[:,:,None]
        self.side_legs[1]*=((old.GRID_Y>=320)&(old.GRID_X>=160))[:,:,None]
        self.side_body=side.copy()
        self.side_body*=np.clip((338-old.GRID_Y)/5,0,1)[:,:,None]

    def rear_walk(self,s,cam,which='approach'):
        if which=='approach':
            distance=s['z']-2.45;start=2.45;phase=distance/.88
            gain=smoother((s['t']-.42)/.6)*(1-smoother((s['t']-4.90)/.50))
        else:
            distance=s['z']-5.85;start=5.85;phase=.5+distance/.88
            gain=smoother((s['t']-7.65)/.28)*(1-smoother((s['t']-8.86)/.34))
        target=self.source.copy()
        base=cam.project(s['x'],0,s['z']);scale=cam.scale(s['x'],s['z'])
        def norm(world):return (cam.project(*world)-base)/scale+[CENTER,FLOOR]
        # A small loading drop followed by a rise during leg passage.
        f=(phase*2)%1
        bob=mix(-.010,.013,smooth(f/.68)) if f<.68 else mix(.013,-.010,smooth((f-.68)/.32))
        bob*=gain
        feet=[];boots=[]
        for j,side in enumerate([-1,1]):
            local=phase+(0 if j==0 else .5)
            cycle=math.floor(local);u=local-cycle
            initial_phase=0 if which=='approach' else .5
            origin=start+(cycle-(0 if j==0 else .5)-initial_phase)*.88
            if u<.6:
                fz=origin+.264;lift=0.;contact=True
            else:
                swing=(u-.6)/.4
                fz=origin+.264+.88*smoother(swing)
                lift=.085*math.sin(math.pi*swing)**1.3;contact=False
            fx=s['x']+side*.10
            # Keep lateral placement locked with the route during stance too.
            if which=='approach':fx=-.56+.22*clamp((origin+.264-2.45)/3.08)+side*.10
            else:fx=-.43+.09*clamp((origin+.264-5.85)/.68)+side*.10
            hip=np.array([s['x']+side*.10,.845+bob,s['z']])
            ankle=np.array([fx,.100+lift,fz])
            knee=knee_ik(hip,ankle)
            length_error=max(abs(np.linalg.norm(knee[1:]-hip[1:])-.45),abs(np.linalg.norm(ankle[1:]-knee[1:])-.39))
            self.errors.append(float(length_error))
            # Project hip/knee/ankle plus boot heel and toe, preserving topology.
            # Rear-view boot corners share a ground depth. Treating them as
            # heel/toe depth points incorrectly rotated the whole boot upright.
            world=[hip,knee,ankle,np.array([fx-.045,lift,fz]),np.array([fx+.045,lift,fz])]
            index=len(self.fixed)+j*5
            for k,w in enumerate(world):
                desired=norm(w)
                target[index+k]=self.source[index+k]*(1-gain)+desired*gain
            foot_for_shadow=[mix(s['x']+side*.10,fx,gain),.100+lift*gain,mix(s['z'],fz,gain)]
            feet.append(dict(side=side,contact=contact,gain=gain,world=foot_for_shadow))
            old_sole=np.array([126 if j==0 else 186,474.])
            sole=old_sole*(1-gain)+norm(np.array([fx,lift,fz]))*gain
            boot_scale=mix(1,cam.scale(fx,fz)/scale,gain)
            m=np.float32([[boot_scale,0,sole[0]-old_sole[0]*boot_scale],
                          [0,boot_scale,sole[1]-old_sole[1]*boot_scale]])
            boots.append(cv2.warpAffine(self.rear_boots[j],m,(SW,SH),flags=cv2.INTER_LINEAR))
        # Opposing arm action is modest in the rear view; no changing drawings.
        sway=math.sin(2*math.pi*phase)*gain
        target[-4]+=[-1*sway,0];target[-3]+=[-3*sway,5*sway]
        target[-2]+=[-1*sway,0];target[-1]+=[-3*sway,-5*sway]
        target[4:12,1]-=bob/old.HUMAN*(FLOOR-TOP)
        s['feet']=feet;s['gaitGain']=gain;s['gaitPhase']=phase
        result=tps(self.rear_body,self.source,target)
        for boot in boots:result=boot+result*(1-boot[:,:,3:4])
        return np.clip(result,0,1)

    def action(self,t):
        # Do not restart an ease-in/ease-out at every intermediate drawing.
        keys=[(5.40,'a0'),(5.74,'a1'),(6.05,'a2'),(6.32,'a3'),
              (6.59,'a4'),(6.89,'a5'),(7.17,'a6'),(7.42,'a7'),(7.68,'a0')]
        for (ta,a),(tb,b) in zip(keys,keys[1:]):
            if ta<=t<tb:return self.pair(a,b,(t-ta)/(tb-ta))
        return self.cels['a0']

    def side_walk(self,s,cam):
        """Independent near/far leg layers avoid silhouette folding at crossover."""
        gain=smoother((s['t']-10.22)/.35)
        phase=(s['x']+.15)/.96
        base=cam.project(s['x'],0,s['z']);scale=cam.scale(s['x'],s['z'])
        def norm(w):return (cam.project(*w)-base)/scale+[CENTER,FLOOR]
        result=np.zeros_like(self.cels['s0']);feet=[]
        source_points=[[(124,282),(99,380),(75,441),(51,451),(121,466)],
                       [(177,282),(210,385),(245,437),(229,455),(282,433)]]
        for j,layer in enumerate(self.side_legs):
            offset=.5 if j==0 else 0
            local=phase+offset;cycle=math.floor(local);u=local-cycle
            origin=-.15+(cycle-offset)*.96
            if u<.6:fx=origin+.288;lift=0.;contact=True
            else:
                swing=(u-.6)/.4;fx=origin+.288+.96*smoother(swing)
                lift=.075*math.sin(math.pi*swing)**1.3;contact=False
            fz=s['z']+(.035 if j==0 else -.035)
            # Solve in Y/X, then put the result back into the world X/Y plane.
            hip=np.array([fz,.845,s['x']]);ankle=np.array([fz,.06+lift,fx])
            knee=knee_ik(hip,ankle)
            self.errors.append(float(max(abs(np.linalg.norm(knee[1:]-hip[1:])-.45),abs(np.linalg.norm(ankle[1:]-knee[1:])-.39))))
            joints=[np.array([hip[2],hip[1],fz]),np.array([knee[2],knee[1],fz]),
                    np.array([fx,.06+lift,fz]),np.array([fx-.055,lift,fz]),np.array([fx+.14,.008+lift,fz])]
            fixed=[(0,0),(319,0),(0,511),(319,511)]
            src=np.float64(fixed+source_points[j]);dst=src.copy()
            for k,w in enumerate(joints):dst[k+4]=src[k+4]*(1-gain)+norm(w)*gain
            deformed=tps(layer,src,dst)
            result=deformed+result*(1-deformed[:,:,3:4])
            feet.append(dict(side=j,contact=contact,gain=gain,world=[fx,.06+lift,fz]))
        # A little opposite shoulder movement; the coat conceals the hip joins.
        sway=math.sin(phase*2*math.pi)*gain
        body=cv2.warpAffine(self.side_body,np.float32([[1,0,0],[0,1,-.8*sway]]),(SW,SH))
        result=body+result*(1-body[:,:,3:4])
        s['feet']=feet;s['gaitGain']=gain;s['gaitPhase']=phase
        return np.clip(result,0,1)

    def turn(self,t):
        keys=[(9.20,'a0'),(9.44,'a8'),(9.71,'a9'),(9.99,'a10'),(10.22,'s0')]
        for (ta,a),(tb,b) in zip(keys,keys[1:]):
            if ta<=t<tb:return self.pair(a,b,(t-ta)/(tb-ta))
        return self.cels['s0']


def state(t):
    p=old.travel((t-.42)/4.98,.18)
    if t<5.4:x,z,mode=mix(-.56,-.34,p),mix(2.45,5.53,p),'approach'
    elif t<7.68:
        push=smoother((t-6.32)/1.02)
        x,z,mode=mix(-.34,-.43,push),mix(5.53,5.85,push),'door'
    elif t<9.2:
        u=old.travel((t-7.68)/1.52,.20)
        x,z,mode=mix(-.43,-.34,u),mix(5.85,6.53,u),'threshold'
    elif t<10.22:
        u=smoother((t-9.20)/1.02)
        x,z,mode=mix(-.34,-.15,u),mix(6.53,6.63,u),'turn'
    else:
        elapsed=t-10.22;distance=.72*(elapsed-.18*(1-math.exp(-elapsed/.18)))
        x,z,mode=-.15+distance,6.63,'exit'
    return dict(t=t,x=x,z=z,angle=1.4*smoother((t-6.32)/1.5),mode=mode)


def compose(bg,opened,cel,s,cam):
    plate=bg.copy()
    if s['angle']>0:
        amount=clamp(s['angle']/.035)
        plate[250:582,924:1068]=cv2.addWeighted(opened[250:582,924:1068],amount,plate[250:582,924:1068],1-amount,0)
    clean=cam.background(plate);im=clean.copy()
    scale=cam.scale(s['x'],s['z']);base=cam.project(s['x'],0,s['z'])
    contact=smooth((s['t']-6.12)/.20)*(1-smooth((s['t']-6.86)/.27))
    if contact>0:
        ys,xs=np.where((cel[:,:,3]>.75)&(old.GRID_X>CENTER+15)&(old.GRID_Y>120)&(old.GRID_Y<230))
        if len(xs):
            tip=float(np.percentile(xs,99))
            handle=cam.project(old.HX+old.DW*.89*math.cos(s['angle']),old.DH*.57,old.DZ+old.DW*.89*math.sin(s['angle']))
            base[0]+=float(np.clip(handle[0]-(base[0]+(tip-CENTER)*scale),-28,28))*contact
    behind=s['z']>old.DZ
    old_project=old.project;old.project=cam.project
    def actor():
        if not behind:
            overlay=im.copy()
            if 'feet' in s:
                for foot in s['feet']:
                    fx,fy,fz=foot['world'];point=cam.project(fx,0,fz)
                    cv2.ellipse(overlay,tuple(np.int32(point)),(max(3,int(11*scale)),max(2,int(3*scale))),0,0,360,(45,40,35),-1,cv2.LINE_AA)
            else:
                cv2.ellipse(overlay,tuple(np.int32(base)),(max(4,int(26*scale)),max(2,int(4*scale))),0,0,360,(45,40,35),-1,cv2.LINE_AA)
            cv2.addWeighted(overlay,.13,im,.87,0,im)
        tx,ty=base[0]-CENTER*scale,base[1]-FLOOR*scale
        x0,y0=max(0,int(tx)-1),max(0,int(ty)-1)
        x1,y1=min(W,int(tx+SW*scale)+2),min(H,int(ty+SH*scale)+2)
        if x1<=x0 or y1<=y0:return
        rgba=cv2.warpAffine(cel,np.float32([[scale,0,tx-x0],[0,scale,ty-y0]]),(x1-x0,y1-y0),flags=cv2.INTER_LINEAR)
        if behind:
            # Opening mask follows the same camera as the jamb; no screen-fixed crop.
            mask=(cam.mx[y0:y1,x0:x1]>=924)&(cam.mx[y0:y1,x0:x1]<1178)&(cam.my[y0:y1,x0:x1]>=250)&(cam.my[y0:y1,x0:x1]<582)
            rgba*=mask[:,:,None]
        roi=im[y0:y1,x0:x1];roi[:]=np.uint8(np.clip(rgba[:,:,:3]*255+roi*(1-rgba[:,:,3:4]),0,255))
    try:
        if behind:
            actor();old.draw_door(im,s['angle'])
            jamb=(cam.mx>=1063)&(cam.mx<1083)&(cam.my>=250)&(cam.my<582)
            lower=(cam.mx>=1082)&(cam.mx<1178)&(cam.my>=551)&(cam.my<582)
            im[jamb|lower]=clean[jamb|lower]
            glass=(cam.mx>=1083)&(cam.mx<1160)&(cam.my>=274)&(cam.my<553)
            im[glass]=np.uint8(im[glass]*.94+np.array([220,203,177])*.06)
        else:
            if s['angle']>0:old.draw_door(im,s['angle'])
            actor()
    finally:old.project=old_project
    fade=smoother(s['t']/.35)*(1-smoother((s['t']-12.45)/.95))
    if fade<1:im=np.uint8(im.astype(np.float32)*fade)
    return im


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--preview',action='store_true');parser.add_argument('--smooth-export',action='store_true');args=parser.parse_args()
    actor=Actor();cam=Camera();bg=old.load('corridor-clean.png')[:,:,:3];opened=old.load('door-opening-background.png')[:,:,:3]
    checks=[.65,1.,1.4,2.,3.,4.5,5.35,6.4,7.,8.3,9.4,9.8,10.5,11.,11.7,12.35]
    check_frames={round(t*FPS):i for i,t in enumerate(checks)}
    output=HERE/('detective-exit-camera-v10-smooth.mp4' if args.smooth_export else 'detective-exit-camera-v10.mp4')
    if not args.preview:
        vf='scale=1920:1080:flags=lanczos,setsar=1' if args.smooth_export else 'scale=960:540:flags=area,scale=1920:1080:flags=neighbor,setsar=1'
        encoder=subprocess.Popen([str(old.FF),'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','bgr24','-s',f'{W}x{H}','-r',str(FPS),'-i','-','-an','-vf',vf,'-c:v','libx264','-crf','17','-preset','medium','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],stdin=subprocess.PIPE)
    audit=[];stills=[]
    for n in range(round(DURATION*FPS)):
        if args.preview and n not in check_frames:continue
        s=state(n/FPS);cam.set_time(s['t'])
        if s['mode'] in ('approach','threshold'):cel=actor.rear_walk(s,cam,s['mode'])
        elif s['mode']=='door':cel=actor.action(s['t'])
        elif s['mode']=='turn':cel=actor.turn(s['t'])
        else:
            cel=actor.side_walk(s,cam)
        im=compose(bg,opened,cel,s,cam)
        if n in check_frames:
            cv2.imwrite(str(WORK/f'check-{check_frames[n]:02d}.jpg'),im);stills.append((s['t'],im))
        if not args.preview:encoder.stdin.write(im.tobytes())
        audit.append({**s,'cameraZ':cam.z,'cameraX':cam.x,'cameraYaw':cam.yaw,'outsidePixels':cam.outside})
        if n%120==0:print('Rendered',n,flush=True)
    if not args.preview:
        encoder.stdin.close()
        if encoder.wait()!=0:raise RuntimeError('Encoding failed')
        (HERE/'Validation-v10-motion.json').write_text(json.dumps(dict(frames=len(audit),fps=FPS,duration=DURATION,maxIKLengthError=max(actor.errors),samples=audit[::12]),indent=2))
        (WORK/'full-motion.json').write_text(json.dumps(audit))
    sheet=np.zeros((4*265,4*418,3),np.uint8)
    for i,(t,im) in enumerate(stills):
        x,y=i%4*418,i//4*265;sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet,f'{t:.2f}s',(x+8,y+257),0,.5,(255,255,255),1,cv2.LINE_AA)
    cv2.imwrite(str(HERE/'Storyboard-v10.jpg'),sheet)
    print('Preview ready' if args.preview else str(output),flush=True)

if __name__=='__main__':main()
