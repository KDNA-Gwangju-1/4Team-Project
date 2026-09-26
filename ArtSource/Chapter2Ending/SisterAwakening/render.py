"""Locked bedside close-up: localized eyelid animation, no whole-face morph."""
from pathlib import Path
import sys,json,subprocess
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
sys.path.insert(0,str(ROOT/'output/cinematic-tools'))
import cv2
import numpy as np
cv2.setNumThreads(4)
FF=next((ROOT/'output/cinematic-tools/imageio_ffmpeg/binaries').glob('*.exe'))
WORK=ROOT/'output/sister-awakening';WORK.mkdir(parents=True,exist_ok=True)
FPS=60;DURATION=9


def smooth(x):
    x=max(0,min(1,x));return x*x*(3-2*x)


def openness(t):
    if t<2.05:return 0.
    if t<2.28:return .09*smooth((t-2.05)/.23)
    if t<2.52:return .09*(1-smooth((t-2.28)/.24))
    if t<2.85:return 0.
    if t<4.65:return smooth((t-2.85)/1.8)
    if t<6.3:return 1.
    if t<6.43:return 1-smooth((t-6.3)/.13)
    if t<6.50:return 0.
    if t<6.78:return smooth((t-6.50)/.28)
    return 1.


class Eye:
    def __init__(self,images,box):
        self.box=box
        x0,y0,x1,y1=box
        self.frames=[im[y0:y1,x0:x1].copy() for im in images]
        h,w=self.frames[0].shape[:2]
        self.yy,self.xx=np.mgrid[0:h,0:w].astype(np.float32)
        # The crop boundary cannot move; the entire face remains the closed plate.
        edge=np.minimum.reduce([self.xx,self.yy,w-1-self.xx,h-1-self.yy])
        self.mask=np.clip(edge/7,0,1)[:,:,None]
        self.flow=[]
        for a,b in zip(self.frames,self.frames[1:]):
            gray=lambda im:cv2.cvtColor(im,cv2.COLOR_BGR2GRAY)
            dis=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
            self.flow.append((dis.calc(gray(a),gray(b),None),dis.calc(gray(b),gray(a),None)))
        self.cache={}

    def at(self,amount):
        key=round(max(0,min(1,amount))*80)
        if key in self.cache:return self.cache[key]
        value=key/80*2
        pair=min(int(value),1);u=value-pair
        a,b=self.frames[pair:pair+2]
        ab,ba=self.flow[pair]
        def warp(im,flow,t):
            mx,my=self.xx.copy(),self.yy.copy()
            for _ in range(3):
                sampled=cv2.remap(flow,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
                mx=self.xx-t*sampled[:,:,0];my=self.yy-t*sampled[:,:,1]
            return cv2.remap(im,mx,my,cv2.INTER_LINEAR,borderMode=cv2.BORDER_REPLICATE)
        result=warp(a,ab,u)*(1-u)+warp(b,ba,1-u)*u
        result=np.uint8(np.clip(np.rint(result*self.mask+self.frames[0]*(1-self.mask)),0,255))
        self.cache[key]=result
        return result


def main():
    images=[cv2.imread(str(HERE/'assets'/f'eyes-{state}.png')) for state in ('closed','half','open')]
    assert all(im is not None for im in images)
    assert all(im.shape==images[0].shape for im in images)
    bg=images[0];h,w=bg.shape[:2]
    eyes=[Eye(images,(768,384,881,458)),Eye(images,(962,357,1070,433))]
    mask=np.zeros((h,w),bool)
    for eye in eyes:
        x0,y0,x1,y1=eye.box;mask[y0:y1,x0:x1]=True
    movie=subprocess.Popen([str(FF),'-hide_banner','-loglevel','error','-y',
        '-f','rawvideo','-pix_fmt','bgr24','-s',f'{w}x{h}','-r',str(FPS),'-i','-',
        '-an','-vf','scale=1920:1080:flags=neighbor,setsar=1','-c:v','libx264',
        '-crf','16','-preset','medium','-pix_fmt','yuv420p','-movflags','+faststart',
        str(HERE/'sister-awakening-v1.mp4')],stdin=subprocess.PIPE)
    stills=[];outside=0;trace=[]
    times=[0,2.25,2.9,3.15,3.5,3.9,4.25,4.65,6.3,6.45,6.65,8.5]
    checks={round(t*FPS) for t in times}
    for n in range(round(DURATION*FPS)):
        t=n/FPS;frame=bg.copy()
        amounts=[openness(t),openness(max(0,t-.045))]
        for eye,amount in zip(eyes,amounts):
            x0,y0,x1,y1=eye.box;frame[y0:y1,x0:x1]=eye.at(amount)
        outside=max(outside,int(np.count_nonzero(np.any(frame!=bg,axis=2)&~mask)))
        if n in checks:
            stills.append((t,frame.copy()))
            cv2.imwrite(str(WORK/f'frame-{n:03d}.png'),frame)
        movie.stdin.write(frame.tobytes())
        trace.append(dict(t=t,openness=amounts))
        if n%180==0:print('Rendered',n,flush=True)
    movie.stdin.close()
    if movie.wait()!=0:raise RuntimeError('Encode failed')
    assert outside==0
    sheet=np.zeros((3*265,4*418,3),np.uint8)
    eye_sheet=np.zeros((3*170,4*360,3),np.uint8)
    for i,(t,im) in enumerate(stills):
        x,y=i%4*418,i//4*265
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet,f'{t:.2f}s',(x+8,y+256),0,.5,(255,255,255),1)
        x,y=i%4*360,i//4*170
        eye_sheet[y:y+135,x:x+360]=cv2.resize(im[340:465,755:1085],(360,135))
        cv2.putText(eye_sheet,f'{t:.2f}s',(x+8,y+160),0,.5,(255,255,255),1)
    cv2.imwrite(str(HERE/'Storyboard-v1.jpg'),sheet)
    cv2.imwrite(str(HERE/'Eye-motion-review-v1.jpg'),eye_sheet)
    cap=cv2.VideoCapture(str(HERE/'sister-awakening-v1.mp4'));count=0
    while True:
        ok,im=cap.read()
        if not ok:break
        assert im.shape[:2]==(1080,1920);count+=1
    cap.release();assert count==540
    (HERE/'Validation-v1.json').write_text(json.dumps(dict(frames=count,fps=FPS,duration=DURATION,
        pixelsChangedOutsideEyes=outside,eyeBoxes=[e.box for e in eyes],
        uniqueEyeStates=[len(e.cache) for e in eyes],samples=trace[::15]),indent=2))
    print('Validated 540 frames; non-eye pixels unchanged.',flush=True)


if __name__=='__main__':main()
