"""6 Hz pose AND world-position holds; two exposure frames bridge each jump.

The corridor/camera stays fixed. Only the character is sampled along its jump
path during the shutter interval; no limb morph or background distortion.
"""
import json
import render_v12 as old
from render_v12 import np, cv2, HERE, ROOT, W, H, FPS, staging, base

WORK = ROOT/'output/ending-v13'
WORK.mkdir(parents=True,exist_ok=True)
STEP = 10  # 60 Hz output / 6 Hz character updates


def cel_at(actor,s):
    if s['mode'] in ('approach','threshold','exit'):
        key=old.pose_key(s)
        return actor.cels[key]
    if s['mode']=='door':return actor.action(s['t'])
    return actor.turn(s['t'])


def main():
    actor=staging.Actor();cam=old.FixedCamera()
    bg=base.load('corridor-clean.png')[:,:,:3]
    opened=base.load('door-opening-background.png')[:,:,:3]
    movie=old.encoder(HERE/'detective-exit-stepped-v13.mp4',W,H,True)
    comparison=old.encoder(HERE/'Comparison-v12-v13.mp4',1920,540)
    previous=cv2.VideoCapture(str(HERE/'detective-exit-keyposes-v12.mp4'))
    trace=[];stills=[];hold=None
    for n in range(804):
        index,slot=divmod(n,STEP)
        t0=index*STEP/FPS;t1=(index+1)*STEP/FPS
        start,end=staging.state(t0),staging.state(t1)
        if slot==0:
            cel=cel_at(actor,start)
            # Fades are applied after the held frame; never quantize the fade.
            hold_state={**start,'t':max(.4,min(12.4,start['t']))}
            hold=staging.compose(bg,opened,cel,hold_state,cam)
        if slot<8:
            scene=hold.copy();phase='hold'
        else:
            phase='shutter'
            # Two short shutter exposures span the position gap. Samples are
            # dense so this is a directional smear, not two ghost silhouettes.
            lo=(slot-8)/2;hi=(slot-7)/2
            accumulator=np.zeros((H,W,3),np.float32)
            moving_cel=cel if slot==8 else cel_at(actor,end)
            for u in np.linspace(lo,hi,9):
                s={**start,'x':start['x']+(end['x']-start['x'])*u,
                   'z':start['z']+(end['z']-start['z'])*u,
                   't':max(.4,min(12.4,start['t']))}
                # Keep the door and plate identical for all exposure samples.
                accumulator+=staging.compose(bg,opened,moving_cel,s,cam).astype(np.float32)/9
            scene=np.uint8(np.clip(np.rint(accumulator),0,255))
        fade=staging.smoother(n/FPS/.35)*(1-staging.smoother((n/FPS-12.45)/.95))
        if fade<1:scene=np.uint8(scene.astype(np.float32)*fade)
        movie.stdin.write(scene.tobytes())
        ok,prior=previous.read()
        if not ok:raise RuntimeError('Previous review video is incomplete')
        pair=np.concatenate([cv2.resize(prior,(960,540)),cv2.resize(scene,(960,540))],axis=1)
        cv2.putText(pair,'v12: continuous position',(20,28),0,.6,(255,255,255),1)
        cv2.putText(pair,'v13: 6 Hz position holds + shutter',(980,28),0,.6,(255,255,255),1)
        comparison.stdin.write(pair.tobytes())
        trace.append(dict(frame=n,poseTime=t0,phase=phase,x=start['x'],z=start['z']))
        if n in (60,64,67,68,69,70,120,240,360,480,600,720):
            cv2.imwrite(str(WORK/f'frame-{n:03d}.png'),scene)
            stills.append((n,scene))
        if n%120==0:print('Rendered',n,flush=True)
    for proc in (movie,comparison):
        proc.stdin.close()
        if proc.wait()!=0:raise RuntimeError('Encode failed')
    previous.release()
    (WORK/'motion.json').write_text(json.dumps(trace,indent=2))
    sheet=np.zeros((3*260,4*418,3),np.uint8)
    for i,(n,im) in enumerate(stills):
        x,y=i%4*418,i//4*260
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet,f'{n/60:.3f}s / {trace[n]["phase"]}',(x+8,y+253),0,.45,(255,255,255),1)
    cv2.imwrite(str(HERE/'Storyboard-v13.jpg'),sheet)


if __name__=='__main__':main()
