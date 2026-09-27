"""Reunion montage: recognition, reach insert, contact insert, held embrace.

Art-directed cuts replace body/hand morphing. The camera uses uniform cropping,
not room deformation. This is an illustrated cutscene, not full-body animation.
"""
from pathlib import Path
import sys,json,subprocess,importlib.util
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
spec=importlib.util.spec_from_file_location('awakening',HERE.parent/'SisterAwakening/render.py')
awakening=importlib.util.module_from_spec(spec);spec.loader.exec_module(awakening)
np,cv2,FF=awakening.np,awakening.cv2,awakening.FF
WORK=ROOT/'output/sisters-reunion';WORK.mkdir(parents=True,exist_ok=True)
FPS=60;DURATION=10.5


def shot(image,center,width):
    # One rigid rectangular camera crop preserves architecture and anatomy.
    height=width*9/16
    x,y=center
    matrix=np.float32([[1920/width,0,960-x*1920/width],
                       [0,1080/height,540-y*1080/height]])
    return cv2.warpAffine(image,matrix,(1920,1080),flags=cv2.INTER_NEAREST)


def main():
    load=lambda name:cv2.imread(str(HERE/'assets'/name))
    recognition=load('01-eye-contact.png')
    reach=load('02-hand-reaching.png')
    contact=load('03-hand-contact.png')
    embrace=load('04-embrace.png')
    assert all(im is not None for im in (recognition,reach,contact,embrace))
    encoder=subprocess.Popen([str(FF),'-hide_banner','-loglevel','error','-y',
        '-f','rawvideo','-pix_fmt','bgr24','-s','1920x1080','-r','60','-i','-',
        '-an','-c:v','libx264','-crf','16','-preset','medium','-pix_fmt','yuv420p',
        '-movflags','+faststart',str(HERE/'sisters-reunion-v1.mp4')],stdin=subprocess.PIPE)
    frames=[]
    checks={0,120,180,204,222,240,270,324,348,420,510,624}
    for n in range(round(DURATION*FPS)):
        t=n/FPS
        if t<3:
            u=awakening.smooth(t/3)
            frame=shot(recognition,(836,465),1672-42*u)
            name='recognition'
        elif t<3.85:
            frame=shot(reach,(865,646),700-18*awakening.smooth((t-3)/.85))
            name='reach insert'
        elif t<5.8:
            frame=shot(contact,(839,668),565-18*awakening.smooth((t-3.85)/1.95))
            name='hand contact'
        else:
            # Cut on the completed reassuring contact; hold the final embrace.
            # A gentle pullback gives the following empty corridor room to breathe.
            u=awakening.smooth((t-5.8)/4.7)
            frame=shot(embrace,(836,455),1440+110*u)
            name='embrace'
        encoder.stdin.write(frame.tobytes())
        if n in checks:
            frames.append((t,name,frame.copy()))
            cv2.imwrite(str(WORK/f'frame-{n:03d}.jpg'),frame)
        if n%180==0:print('Rendered',n,flush=True)
    encoder.stdin.close()
    if encoder.wait()!=0:raise RuntimeError('Encoding failed')
    sheet=np.zeros((3*295,4*480,3),np.uint8)
    for i,(t,name,frame) in enumerate(frames):
        x,y=i%4*480,i//4*295
        sheet[y:y+270,x:x+480]=cv2.resize(frame,(480,270))
        cv2.putText(sheet,f'{t:.2f}s / {name}',(x+8,y+288),0,.48,(255,255,255),1)
    cv2.imwrite(str(HERE/'Storyboard-v1.jpg'),sheet)
    # Context preview only: preserves each independently reviewable source clip.
    inputs=[HERE.parent/'SisterAwakening/sister-awakening-v1.mp4',
            HERE/'sisters-reunion-v1.mp4',
            HERE.parent/'DetectiveExit/hospital-corridor-wind-v15.mp4']
    listing=WORK/'sequence.txt'
    listing.write_text('\n'.join("file '"+p.as_posix()+"'" for p in inputs))
    subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y','-f','concat',
        '-safe','0','-i',str(listing),'-an','-c','copy','-movflags','+faststart',
        str(HERE/'Ending-sequence-preview-v1.mp4')],check=True)
    report=dict(cuts=[dict(start=0,end=3,shot='recognition'),
                     dict(start=3,end=3.85,shot='reach insert'),
                     dict(start=3.85,end=5.8,shot='hand contact'),
                     dict(start=5.8,end=10.5,shot='embrace')],
                     poseInterpolation=False,bodyWarp=False,videos=[])
    for name,expected in [('sisters-reunion-v1.mp4',630),('Ending-sequence-preview-v1.mp4',1974)]:
        cap=cv2.VideoCapture(str(HERE/name));fps=cap.get(cv2.CAP_PROP_FPS);count=0
        while True:
            ok,frame=cap.read()
            if not ok:break
            assert frame.shape[:2]==(1080,1920)
            count+=1
        cap.release()
        assert count==expected and fps==60,(name,count,fps)
        report['videos'].append(dict(file=name,frames=count,fps=fps,duration=count/fps))
    (HERE/'Validation-v1.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
