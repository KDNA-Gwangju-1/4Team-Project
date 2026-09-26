"""Empty hospital corridor with localized outdoor foliage motion only.

No actor, door action, camera movement, exposure flicker or global deformation.
"""
import json, math, subprocess
import render_v9 as base
from render_v9 import np, cv2, HERE, ROOT, W, H

WORK=ROOT/'output/ending-v15'
WORK.mkdir(parents=True,exist_ok=True)
FPS=60
DURATION=13.4


def prepare():
    bg=base.load('corridor-clean.png')[:,:,:3]
    windows=np.zeros((H,W),np.uint8)
    # Glass interiors only, with clearance from frames, rails and indoor plants.
    panes=[[(0,0),(158,0),(158,445),(0,445)],
           [(232,7),(383,77),(383,453),(232,453)],
           [(431,115),(488,143),(488,451),(431,451)],
           [(667,260),(686,266),(686,377),(667,377)],
           [(713,283),(721,286),(721,380),(713,380)],
           [(946,279),(1038,279),(1038,436),(946,436)],
           [(946,474),(1038,474),(1038,536),(946,536)],
           [(1090,280),(1154,280),(1154,435),(1090,435)],
           [(1090,475),(1154,475),(1154,536),(1090,536)]]
    for pane in panes:cv2.fillPoly(windows,[np.int32(pane)],255)
    b,g,r=cv2.split(bg.astype(np.int16))
    # Reject the warm white clouds as well as blue/gray distant buildings.
    foliage=((g>b+12)&(g>=r)&(r>b+4)&(b<175)&(windows>0)).astype(np.uint8)*255
    # Include the few pixels around leaf edges so silhouettes can sway as well.
    support=cv2.dilate(foliage,np.ones((11,11),np.uint8))
    support=cv2.bitwise_and(support,windows)
    # Distance falloff anchors motion at the edge of the allowed region.
    weight=np.minimum(cv2.distanceTransform(support,cv2.DIST_L2,3)/6,1)
    yy,xx=np.mgrid[0:H,0:W].astype(np.float32)
    return bg,support,weight,xx,yy


def animate(bg,weight,xx,yy,t):
    phase=2*math.pi*t/DURATION
    # Broad slow branch motion with a smaller asynchronous leaf flutter.
    near=np.where(xx<600,1.0,.42).astype(np.float32)
    sway=(2.6*np.sin(phase*3+xx*.004+yy*.003)
          +.65*np.sin(phase*7+yy*.021+xx*.008))*near*weight
    lift=.65*np.sin(phase*3+xx*.006+yy*.004)*near*weight
    # Nearest sampling preserves the source's pixel edges. Non-foliage pixels
    # have exactly zero displacement, including corridor and architectural lines.
    moved=cv2.remap(bg,xx+sway,yy+lift,cv2.INTER_NEAREST,borderMode=cv2.BORDER_REPLICATE)
    return moved


def main():
    bg,support,weight,xx,yy=prepare()
    out=HERE/'hospital-corridor-wind-v15.mp4'
    encoder=subprocess.Popen([str(base.FF),'-hide_banner','-loglevel','error','-y',
        '-f','rawvideo','-pix_fmt','bgr24','-s',f'{W}x{H}','-r',str(FPS),'-i','-',
        '-an','-vf','scale=1920:1080:flags=neighbor,setsar=1','-c:v','libx264',
        '-crf','16','-preset','medium','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],stdin=subprocess.PIPE)
    checks=[];outsideMax=0;changed=[]
    for n in range(round(DURATION*FPS)):
        im=animate(bg,weight,xx,yy,n/FPS)
        difference=np.any(im!=bg,axis=2)
        outsideMax=max(outsideMax,int(np.count_nonzero(difference&(support==0))))
        changed.append(int(np.count_nonzero(difference)))
        encoder.stdin.write(im.tobytes())
        if n in (0,120,240,360,480,600):checks.append((n,im.copy()))
        if n%240==0:print('Rendered',n,flush=True)
    encoder.stdin.close()
    if encoder.wait()!=0:raise RuntimeError('Encoding failed')
    assert outsideMax==0 and max(changed)>0
    assert np.array_equal(animate(bg,weight,xx,yy,0),animate(bg,weight,xx,yy,DURATION))
    sheet=np.zeros((2*265,3*418,3),np.uint8)
    for i,(n,im) in enumerate(checks):
        x,y=i%3*418,i//3*265
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet,f'{n/FPS:.1f}s',(x+8,y+256),0,.5,(255,255,255),1)
    cv2.imwrite(str(HERE/'Storyboard-v15.jpg'),sheet)
    cv2.imwrite(str(WORK/'foliage-mask.png'),support)
    # A crop makes the small wind movement easy to judge, without changing it.
    subprocess.run([str(base.FF),'-hide_banner','-loglevel','error','-y','-i',str(out),
        '-t','6','-vf','crop=660:540:0:0,scale=990:810:flags=neighbor',
        '-an','-c:v','libx264','-crf','16',str(HERE/'Window-wind-detail-v15.mp4')],check=True)
    videos=[]
    for name,countExpected,size in [('hospital-corridor-wind-v15.mp4',804,(1920,1080)),
                                   ('Window-wind-detail-v15.mp4',360,(990,810))]:
        cap=cv2.VideoCapture(str(HERE/name));fps=cap.get(cv2.CAP_PROP_FPS);count=0
        while True:
            ok,im=cap.read()
            if not ok:break
            assert (im.shape[1],im.shape[0])==size
            count+=1
        cap.release()
        assert count==countExpected and fps==60
        videos.append(dict(file=name,frames=count,fps=fps,size=size))
    report=dict(outsideFoliageChangedPixels=outsideMax,minChangedFoliagePixels=min(changed),
        maxChangedFoliagePixels=max(changed),loopEndpointsIdentical=True,
        actor=False,exposureFlicker=False,cameraMovement=False,videos=videos)
    (HERE/'Validation-v15.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
