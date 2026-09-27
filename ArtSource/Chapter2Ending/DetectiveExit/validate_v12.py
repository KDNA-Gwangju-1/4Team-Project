"""Validate effect scope and decode all exported frames; not perceptual approval."""
import json
import render_v12 as r

actor = r.staging.Actor()
blurred, held, rear = 0, 0, set()
examples = []
for n in range(804):
    s = r.staging.state(n/60)
    if s['mode'] not in ('approach', 'threshold', 'exit'):continue
    cel, key, strength = r.drawn_walk(actor, s)
    original = actor.cels[key]
    assert r.np.array_equal(cel[:140], original[:140]), 'Head was blurred'
    if s['mode'] == 'approach' and key != 'a0':rear.add(key)
    if strength:
        blurred += 1
        assert strength in (.25, .70)
        assert any(r.pose_key(r.staging.state((b-1)/60)) != r.pose_key(r.staging.state(b/60))
                   for b in range(n-1,n+3) if b>0)
        if len(examples)<4 and n>60:examples.append((original,cel))
    else:
        held += 1
        assert r.np.array_equal(cel, original), 'Held pose was modified'
assert rear == {'w0','w3','w6','w9'}
videos = []
for name, expected, dimensions in [
    ('detective-exit-keyposes-v12.mp4',804,(1920,1080)),
    ('Walk-isolated-v12.mp4',300,(640,1024)),
    ('Comparison-v11-v12.mp4',300,(1280,1024))]:
    cap=r.cv2.VideoCapture(str(r.HERE/name))
    fps=cap.get(r.cv2.CAP_PROP_FPS)
    count=0
    while True:
        ok,frame=cap.read()
        if not ok:break
        assert (frame.shape[1],frame.shape[0]) == dimensions
        count+=1
    cap.release()
    assert count==expected and fps==60
    videos.append(dict(file=name,frames=count,fps=fps,dimensions=dimensions))
rows=[]
for original,cel in examples:
    panels=[]
    for rgba in (original,cel):
        im=r.np.uint8(r.np.clip(rgba[:,:,:3]*255+48*(1-rgba[:,:,3:4]),0,255))
        panels.append(im[320:490,65:255])
    rows.append(r.np.concatenate(panels,axis=1))
r.cv2.imwrite(str(r.WORK/'transition-detail.jpg'),r.np.concatenate(rows,axis=0))
report=dict(rearPoses=sorted(rear),walkingFramesWithBlur=blurred,
            walkingFramesUnmodified=held,headPixelsUnmodified=True,
            holdPixelsUnmodified=True,transitionWindowFrames=4,videos=videos,
            scope='File and effect-scope checks; not perceptual approval of gait.')
(r.HERE/'Validation-v12.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
