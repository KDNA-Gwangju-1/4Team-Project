"""Exposure flicker synchronized with v13's stepped motion/shutter frames.

Uses the completed v13 picture as input; no geometric background manipulation.
"""
import json
import subprocess
import render_v13 as source
from render_v13 import np, cv2, HERE, ROOT, base, staging

WORK=ROOT/'output/ending-v14'
WORK.mkdir(parents=True,exist_ok=True)


def exposure(frame):
    t=frame/60
    # Fade the treatment in/out around walking; keep dialogue/reach readable.
    strength=max(staging.smoother((t-.5)/.3)*(1-staging.smoother((t-5.05)/.3)),
                 staging.smoother((t-7.65)/.2)*(1-staging.smoother((t-9.03)/.2)),
                 staging.smoother((t-10.2)/.2)*(1-staging.smoother((t-11.9)/.3)))
    # Anticipation, shutter dip, recovery. No fully black/white frames.
    dip={7:.09,8:.22,9:.10}.get(frame%10,0)
    return 1-dip*strength


def encode(path,size):
    return subprocess.Popen([str(base.FF),'-hide_banner','-loglevel','error','-y',
        '-f','rawvideo','-pix_fmt','bgr24','-s',size,'-r','60','-i','-',
        '-an','-c:v','libx264','-crf','17','-preset','medium','-pix_fmt','yuv420p',
        '-movflags','+faststart',str(path)],stdin=subprocess.PIPE)


def main():
    cap=cv2.VideoCapture(str(HERE/'detective-exit-stepped-v13.mp4'))
    movie=encode(HERE/'detective-exit-flicker-v14.mp4','1920x1080')
    comparison=encode(HERE/'Comparison-v13-v14.mp4','1920x540')
    frames=0;affected=0;stills=[]
    while True:
        ok,im=cap.read()
        if not ok:break
        gain=exposure(frames)
        result=np.uint8(np.rint(im.astype(np.float32)*gain)) if gain<1 else im
        movie.stdin.write(result.tobytes())
        pair=np.concatenate([cv2.resize(im,(960,540)),cv2.resize(result,(960,540))],axis=1)
        cv2.putText(pair,'v13: stepped motion',(20,28),0,.6,(255,255,255),1)
        cv2.putText(pair,'v14: synchronized exposure flicker',(980,28),0,.6,(255,255,255),1)
        comparison.stdin.write(pair.tobytes())
        if gain<1:affected+=1
        if frames in (66,67,68,69,70,71):
            stills.append((frames,gain,result.copy()))
        frames+=1
        if frames%240==0:print('Rendered',frames,flush=True)
    cap.release()
    for proc in (movie,comparison):
        proc.stdin.close()
        if proc.wait()!=0:raise RuntimeError('Encoding failed')
    assert frames==804
    sheet=np.zeros((2*295,3*480,3),np.uint8)
    for i,(frame,gain,im) in enumerate(stills):
        x,y=i%3*480,i//3*295
        sheet[y:y+270,x:x+480]=cv2.resize(im,(480,270))
        cv2.putText(sheet,f'frame {frame} / brightness {gain:.2f}',(x+8,y+288),0,.5,(255,255,255),1)
    cv2.imwrite(str(HERE/'Storyboard-v14.jpg'),sheet)
    report=dict(inputFrames=frames,affectedFrames=affected,minExposure=min(exposure(n) for n in range(804)),
                effect='3-frame exposure pulse tied to 6 Hz position changes; walking only',videos=[])
    for name,size in [('detective-exit-flicker-v14.mp4',(1920,1080)),('Comparison-v13-v14.mp4',(1920,540))]:
        check=cv2.VideoCapture(str(HERE/name));count=0;fps=check.get(cv2.CAP_PROP_FPS)
        while True:
            ok,im=check.read()
            if not ok:break
            assert (im.shape[1],im.shape[0])==size
            count+=1
        check.release()
        assert count==804 and fps==60
        report['videos'].append(dict(file=name,frames=count,fps=fps,size=size))
    (HERE/'Validation-v14.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
