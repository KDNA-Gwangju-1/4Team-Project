"""Continuous 60 Hz actor/door animation using the existing registered cels.

python render_v9.py [--preview]
Dependencies: numpy, opencv-python-headless. Optional local install:
python -m pip install --target output/cinematic-tools opencv-python-headless
No generated artwork, Unity scene edits, or remote rendering is required.
"""
from pathlib import Path
import sys, json, math, subprocess, argparse

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'output/cinematic-tools'))
import cv2
import numpy as np

cv2.setNumThreads(4)
ASSETS = HERE / 'assets'
WORK = ROOT / 'output/ending-v9'
WORK.mkdir(parents=True, exist_ok=True)
FF = next((ROOT / 'output/cinematic-tools/imageio_ffmpeg/binaries').glob('*.exe'))
W, H, FPS, DURATION = 1672, 941, 60, 12.4
CX, CY, FOCAL, EYE, HUMAN = 1045., 407., 900., 1.15, 1.70
DZ = FOCAL * EYE / (582-CY)
HX, DW, DH = (924-CX)*DZ/FOCAL, 144*DZ/FOCAL, 332*DZ/FOCAL
SW, SH, TOP, FLOOR, CENTER = 320, 512, 28., 476., 160.
GRID_X, GRID_Y = np.meshgrid(np.arange(SW, dtype=np.float32), np.arange(SH, dtype=np.float32))

def clamp(v): return max(0., min(1., v))
def smooth(v):
    v = clamp(v)
    return v*v*(3.-2.*v)
def mix(a,b,t): return a+(b-a)*t
def travel(u, ramp=.14):
    """Continuous velocity with a real acceleration and braking interval."""
    u=clamp(u)
    if u<ramp: return u*u/(2*ramp*(1-ramp))
    if u>1-ramp: return 1-(1-u)**2/(2*ramp*(1-ramp))
    return (u-ramp/2)/(1-ramp)
def project(x,y,z): return np.array([CX+FOCAL*x/z, CY+FOCAL*(EYE-y)/z])

def load(name):
    im=cv2.imread(str(ASSETS/name), cv2.IMREAD_UNCHANGED)
    if im is None: raise RuntimeError(name)
    return im

def register(name, cols, rows):
    img=load(name)
    assert img.shape[2]==4, 'Source alpha required'
    cw,ch=img.shape[1]//cols,img.shape[0]//rows
    result=[]
    for i in range(cols*rows):
        cell=img[(i//cols)*ch:(i//cols+1)*ch,(i%cols)*cw:(i%cols+1)*cw]
        yy,xx=np.where(cell[:,:,3]>180)
        top,bottom=int(yy.min()),int(yy.max())
        head_x=float(xx[yy<top+55].mean())
        scale=(FLOOR-TOP)/(bottom-top)
        matrix=np.float32([[scale,0,CENTER-head_x*scale],[0,scale,TOP-top*scale]])
        # Premultiplied alpha prevents dark fringes when sampling the transparent cels.
        pm=cell.astype(np.float32)/255
        pm[:,:,:3]*=pm[:,:,3:4]
        result.append(cv2.warpAffine(pm,matrix,(SW,SH),flags=cv2.INTER_LINEAR))
    return result

class Animator:
    def __init__(self):
        self.cels={}
        for prefix,file,cols,rows in [('w','walk-guided.png',6,2),('a','action.png',6,2),('s','side.png',4,2)]:
            for i,cel in enumerate(register(file,cols,rows)): self.cels[prefix+str(i)]=cel
        self.flows={}
        self.dis=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
        self.dis.setUseSpatialPropagation(True)
        self.dis.setVariationalRefinementIterations(8)

    def gray(self, im):
        # Flow sees both garment details and the silhouette on neutral grey.
        rgb=im[:,:,:3]+.55*(1-im[:,:,3:4])
        return cv2.cvtColor(np.uint8(np.clip(rgb*255,0,255)),cv2.COLOR_BGR2GRAY)

    def pair(self, a,b,t):
        t=clamp(t)
        if a==b or t<=.00001: return self.cels[a]
        if t>=.99999: return self.cels[b]
        if (a,b)==('a9','a10'):
            return self.guided_turn(t)
        if (a,b) not in self.flows:
            ga,gb=self.gray(self.cels[a]),self.gray(self.cels[b])
            self.flows[a,b]=(self.dis.calc(ga,gb,None),self.dis.calc(gb,ga,None))
        ab,ba=self.flows[a,b]
        # Invert each forward displacement to sample both cels at a common time.
        # Three iterations reduce error at a moving wrist/heel; this is not a rig.
        def warp(im,flow,amount):
            mx,my=GRID_X.copy(),GRID_Y.copy()
            for _ in range(3):
                displacement=cv2.remap(flow,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
                mx=GRID_X-amount*displacement[:,:,0]
                my=GRID_Y-amount*displacement[:,:,1]
            return cv2.remap(im,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
        return warp(self.cels[a],ab,t)*(1-t)+warp(self.cels[b],ba,1-t)*t

    def guided_turn(self,t):
        """Hand-authored screen landmarks for the large 3/4-to-profile step.

        Generic optical flow confuses the stationary shin with the new forward
        shin. A smooth landmark warp establishes that correspondence explicitly.
        This is a 2D drawing deformation, not a 3D anatomical rig.
        """
        fixed=[(0,0),(319,0),(0,511),(319,511),(160,35),(160,70),
               (148,106),(137,137),(93,192),(86,241),(68,310),(190,321)]
        a=np.float64(fixed+[(184,202),(199,243),(202,260),(116,325),
                           (90,375),(66,420),(62,444),(93,457),
                           (145,330),(144,390),(141,441),(140,466),(180,460)])
        b=np.float64(fixed+[(189,193),(218,226),(226,241),(112,329),
                           (85,380),(58,430),(53,447),(103,465),
                           (168,330),(194,381),(230,432),(234,455),(273,422)])
        target=a*(1-t)+b*t
        def warp(cel,source):
            # Thin-plate spline inverse map, fitted in normalized coordinates.
            dst=target/512;src=source/512;n=len(dst)
            delta=dst[:,None,:]-dst[None,:,:]
            r2=np.sum(delta*delta,axis=2)
            K=r2*np.log(r2+1e-12)+np.eye(n)*1e-7
            P=np.column_stack([np.ones(n),dst])
            system=np.block([[K,P],[P.T,np.zeros((3,3))]])
            weights=np.linalg.solve(system,np.vstack([src,np.zeros((3,2))]))
            yy,xx=np.mgrid[0:SH:4,0:SW:4]
            q=np.column_stack([xx.ravel(),yy.ravel()])/512
            r2=np.sum((q[:,None,:]-dst[None,:,:])**2,axis=2)
            basis=np.column_stack([r2*np.log(r2+1e-12),np.ones(len(q)),q])
            coords=(basis@weights*512).reshape(SH//4,SW//4,2).astype(np.float32)
            # Sampling coordinates are aligned to the coarse grid's endpoints.
            mx=cv2.remap(coords[:,:,0],GRID_X/4,GRID_Y/4,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
            my=cv2.remap(coords[:,:,1],GRID_X/4,GRID_Y/4,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
            return cv2.remap(cel,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
        return warp(self.cels['a9'],a)*(1-t)+warp(self.cels['a10'],b)*t

    def cycle(self,prefix,order,phase):
        f=(phase%1)*len(order); i=int(f)
        return prefix+str(order[i]),prefix+str(order[(i+1)%len(order)]),f-i

    def pose(self,t,progress):
        walk=[0,1,2,3,4,5,6,8,7,9,10,11]
        if t<5.25:
            phase=3.5*progress
            a,b,f=self.cycle('w',walk,phase)
            # Explicit start/settle poses, rather than a hard cut to an idle cel.
            if t<.58: return 'a0','w0',smooth((t-.12)/.46)
            if t>4.91: return 'w6','a0',smooth((t-4.91)/.34)
            return a,b,f
        # All reach/push/release cels are now used, including formerly skipped 4/5.
        keys=[(5.25,'a0'),(5.58,'a1'),(5.90,'a2'),(6.18,'a3'),
              (6.45,'a4'),(6.76,'a5'),(7.05,'a6'),(7.34,'a7'),
              (7.56,'w6')]
        if t<7.56:
            for (ta,a),(tb,b) in zip(keys,keys[1:]):
                if ta<=t<tb: return a,b,smooth((t-ta)/(tb-ta))
        if t<8.92:
            return self.cycle('w',walk,.5+travel((t-7.56)/1.36))
        turn=[(8.92,'w6'),(9.10,'a8'),(9.34,'a9'),(9.57,'a10'),(9.76,'s0')]
        if t<9.76:
            for (ta,a),(tb,b) in zip(turn,turn[1:]):
                if ta<=t<tb:return a,b,smooth((t-ta)/(tb-ta))
        # Remove the near-duplicate contact drawings from the lateral cycle.
        elapsed=max(0,t-9.76)
        distance=.80*(elapsed-.12*(1-math.exp(-elapsed/.12)))
        return self.cycle('s',[0,2,3,4,6,7],distance/.96)

def state(t):
    p=travel((t-.58)/4.67)
    phase=3.5*p
    if t<5.25:
        x,z=mix(-.56,-.34,p),mix(2.45,5.53,p)
        movement=min(smooth((t-.3)/.4),1-smooth((t-4.82)/.43))
        bob=.010*(1-math.cos(4*math.pi*phase))*movement
        x+=.006*math.sin(2*math.pi*phase)*movement
        mode='approach'
    elif t<7.56:
        # The short push step happens with the drawn leg action, not after it.
        push=smooth((t-6.18)/.95)
        x,z=mix(-.34,-.43,push),mix(5.53,5.85,push)
        bob=0.; mode='door'
    elif t<8.92:
        u=travel((t-7.56)/1.36)
        x,z=mix(-.43,-.34,u),mix(5.85,6.53,u)
        bob=.007*(1-math.cos(4*math.pi*u))*math.sin(math.pi*u)
        mode='threshold'
    elif t<9.76:
        u=smooth((t-8.92)/.84)
        x,z=mix(-.34,-.15,u),mix(6.53,6.63,u)
        bob=0.; mode='turn'
    else:
        elapsed=t-9.76
        distance=.8*(elapsed-.12*(1-math.exp(-elapsed/.12)))
        x,z=-.15+distance,6.63
        bob=.006*(1-math.cos(4*math.pi*distance/.96))*smooth(elapsed/.25)
        mode='exit'
    angle=1.40*smooth((t-6.18)/1.45)
    return dict(t=t,x=x,z=z,bob=bob,angle=angle,progress=p,mode=mode)

def poly(im,points,color,alpha=1.):
    pts=np.int32(np.round(points))
    if alpha==1:cv2.fillConvexPoly(im,pts,color,lineType=cv2.LINE_AA)
    else:
        overlay=im.copy();cv2.fillConvexPoly(overlay,pts,color,lineType=cv2.LINE_AA)
        cv2.addWeighted(overlay,alpha,im,1-alpha,0,im)

def draw_door(im,angle):
    def q(u,v):return project(HX+DW*u*math.cos(angle),DH*v,DZ+DW*u*math.sin(angle))
    def rect(u0,v0,u1,v1,color,opacity=1):
        poly(im,[q(u0,v0),q(u1,v0),q(u1,v1),q(u0,v1)],color,opacity)
    rect(0,0,1,1,(211,184,155),.06)
    for r in [(0,0,.075,1),(.925,0,1,1),(0,0,1,.075),(0,.94,1,1)]:rect(*r,(108,83,70))
    rect(.075,.385,.925,.405,(168,169,164))
    rect(.86,.32,.905,.60,(200,199,195))
    rect(.89,.335,.899,.585,(228,231,228))

def compose(bg,opened,cel,s):
    im=bg.copy()
    if s['angle']>0:
        opacity=clamp(s['angle']/.035)
        im[250:582,924:1068]=cv2.addWeighted(opened[250:582,924:1068],opacity,
                                           im[250:582,924:1068],1-opacity,0)
    scale=FOCAL*HUMAN/s['z']/(FLOOR-TOP)
    base=project(s['x'],s['bob'],s['z'])
    # During the push, match the actual drawn fingertips to the moving handle's
    # horizontal position. Move the body with the small push step rather than
    # stretching the arm. Blend out before releasing the handle.
    contact=smooth((s['t']-5.98)/.20)*(1-smooth((s['t']-6.72)/.27))
    s['handCorrectionPixels']=0.
    if contact>0:
        ys,xs=np.where((cel[:,:,3]>.75)&(GRID_X>CENTER+15)&(GRID_Y>120)&(GRID_Y<230))
        if len(xs):
            fingertip=float(np.percentile(xs,99))
            handle=project(HX+DW*.89*math.cos(s['angle']),DH*.57,DZ+DW*.89*math.sin(s['angle']))
            correction=float(np.clip(handle[0]-(base[0]+(fingertip-CENTER)*scale),-28,28))*contact
            base[0]+=correction
            s['handCorrectionPixels']=correction
    tx,ty=base[0]-CENTER*scale,base[1]-FLOOR*scale
    behind=s['z']>DZ
    def actor():
        nonlocal im
        if not behind:
            overlay=im.copy()
            ground=project(s['x'],0,s['z'])
            cv2.ellipse(overlay,tuple(np.int32(ground)),(int(26*5/s['z']),max(2,int(3*5/s['z']))),0,0,360,(45,40,35),-1,cv2.LINE_AA)
            cv2.addWeighted(overlay,.13,im,.87,0,im)
        # Only sample the actor's small bounding rectangle, not the whole screen.
        x0,y0=max(0,int(tx)-1),max(0,int(ty)-1)
        x1,y1=min(W,int(tx+SW*scale)+2),min(H,int(ty+SH*scale)+2)
        if x1<=x0 or y1<=y0:return
        M=np.float32([[scale,0,tx-x0],[0,scale,ty-y0]])
        rgba=cv2.warpAffine(cel,M,(x1-x0,y1-y0),flags=cv2.INTER_LINEAR)
        if behind:
            yy,xx=np.mgrid[y0:y1,x0:x1]
            rgba*=((xx>=924)&(xx<1178)&(yy>=250)&(yy<582))[:,:,None]
        roi=im[y0:y1,x0:x1]
        roi[:]=np.uint8(np.clip(rgba[:,:,:3]*255+roi*(1-rgba[:,:,3:4]),0,255))
    if behind:
        actor()
        draw_door(im,s['angle'])
        im[250:582,1063:1083]=bg[250:582,1063:1083]
        glass=im[274:553,1083:1160]
        glass[:]=np.uint8(glass*.94+np.array([220,203,177])*.06)
        im[551:582,1082:1178]=bg[551:582,1082:1178]
    else:
        if s['angle']>0:draw_door(im,s['angle'])
        actor()
    fade=smooth(t/.3)*(1-smooth((t-11.65)/.75)) if (t:=s['t'])>=0 else 1
    if fade<1: im=np.uint8(im.astype(np.float32)*fade)
    return im,base

def main():
    preview=argparse.ArgumentParser();preview.add_argument('--preview',action='store_true');args=preview.parse_args()
    anim=Animator();bg=load('corridor-clean.png')[:,:,:3];opened=load('door-opening-background.png')[:,:,:3]
    times=[.8,2.,4.7,5.2,5.7,6.2,6.5,6.85,7.3,8.4,9.1,9.5,9.8,10.2,10.8,11.3]
    check_frames={round(t*FPS):i for i,t in enumerate(times)}
    video=HERE/'detective-exit-continuous-v9.mp4'
    if not args.preview:
        command=[str(FF),'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','bgr24',
                 '-s',f'{W}x{H}','-r',str(FPS),'-i','-','-an','-vf','scale=1920:1080:flags=lanczos,setsar=1',
                 '-c:v','libx264','-crf','17','-preset','medium','-pix_fmt','yuv420p','-movflags','+faststart',str(video)]
        encoder=subprocess.Popen(command,stdin=subprocess.PIPE)
    audit=[];stills=[]
    for n in range(round(FPS*DURATION)):
        if args.preview and n not in check_frames:continue
        s=state(n/FPS);a,b,u=anim.pose(s['t'],s['progress'])
        cel=anim.pair(a,b,u)
        im,base=compose(bg,opened,cel,s)
        if n in check_frames:
            cv2.imwrite(str(WORK/f'check-{check_frames[n]:02d}.jpg'),im)
            stills.append((s['t'],im))
        if not args.preview:encoder.stdin.write(im.tobytes())
        audit.append({**s,'poseA':a,'poseB':b,'blend':u,'base':base.tolist()})
        if n%120==0:print('Rendered',n,flush=True)
    if not args.preview:
        encoder.stdin.close()
        if encoder.wait()!=0:raise RuntimeError('Video encoding failed')
        (HERE/'Validation-v9-motion.json').write_text(json.dumps(dict(fps=FPS,frames=len(audit),duration=DURATION,
            method='Actor-only bidirectional cel warping; native per-frame scene geometry; no temporal averaging',
            samples=audit[::12]),indent=2))
    sheet=np.zeros((4*265,4*418,3),np.uint8)
    for i,(t,im) in enumerate(stills):
        x,y=i%4*418,i//4*265
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet,f'{t:.2f}s',(x+8,y+257),cv2.FONT_HERSHEY_SIMPLEX,.5,(255,255,255),1,cv2.LINE_AA)
    cv2.imwrite(str(HERE/'Storyboard-v9.jpg'),sheet)
    print('Preview ready' if args.preview else str(video),flush=True)

if __name__=='__main__':main()
