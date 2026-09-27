import json
import render_v13 as r

def frame(n):return r.cv2.imread(str(r.WORK/f'frame-{n:03d}.png'))
a=frame(60)
assert r.np.array_equal(a,frame(64))
assert r.np.array_equal(a,frame(67))
assert not r.np.array_equal(a,frame(68))
assert not r.np.array_equal(frame(69),frame(70))
for n in (64,67,68,69,70):
    assert r.np.array_equal(a[:,:500],frame(n)[:,:500]), 'Background changed'
results=[]
for name,dimensions in [('detective-exit-stepped-v13.mp4',(1920,1080)),
                        ('Comparison-v12-v13.mp4',(1920,540))]:
    cap=r.cv2.VideoCapture(str(r.HERE/name))
    fps=cap.get(r.cv2.CAP_PROP_FPS);count=0
    while True:
        ok,im=cap.read()
        if not ok:break
        assert (im.shape[1],im.shape[0])==dimensions
        count+=1
    cap.release()
    assert count==804 and fps==60
    results.append(dict(file=name,frames=count,fps=fps,size=dimensions))
report=dict(characterHz=6,holdFramesPerStep=8,shutterFramesPerStep=2,
            sampledHoldImagesIdentical=True,sampledTransitionImagesDiffer=True,
            sampledBackgroundRegionIdentical=True,videos=results)
(r.HERE/'Validation-v13.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
