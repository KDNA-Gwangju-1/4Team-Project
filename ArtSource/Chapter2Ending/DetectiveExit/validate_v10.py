"""Check the final render and the full local motion trace, not perceived quality."""
import json
import render_v10 as r

trace=json.loads((r.WORK/'full-motion.json').read_text())
motion=json.loads((r.HERE/'Validation-v10-motion.json').read_text())
cap=r.cv2.VideoCapture(str(r.HERE/'detective-exit-camera-v10.mp4'))
report=dict(width=int(cap.get(3)),height=int(cap.get(4)),fps=cap.get(5),frames=0,
            maxIKLengthError=motion['maxIKLengthError'],maxContactDrift=0.,checkedContactIntervals=0,
            maxOutsideBackgroundPixels=max(s['outsidePixels'] for s in trace))
while True:
    ok,frame=cap.read()
    if not ok:break
    report['frames']+=1
cap.release()
for a,b in zip(trace,trace[1:]):
    if a['mode']!=b['mode']:continue
    for fa,fb in zip(a.get('feet',[]),b.get('feet',[])):
        if fa['contact'] and fb['contact'] and min(fa['gain'],fb['gain'])>.999999:
            delta=float(r.np.linalg.norm(r.np.array(fb['world'])-fa['world']))
            report['maxContactDrift']=max(report['maxContactDrift'],delta)
            report['checkedContactIntervals']+=1
report['seconds']=report['frames']/report['fps']
cam=r.Camera();cam.set_time(0)
points=[(-1.43,.50,2.),(-.30,.25,r.old.DZ)]
before=[cam.project(*p) for p in points]
cam.set_time(5.70)
report['nearFeatureTravelPixels']=float(r.np.linalg.norm(cam.project(*points[0])-before[0]))
report['farFeatureTravelPixels']=float(r.np.linalg.norm(cam.project(*points[1])-before[1]))
assert report['frames']==804 and report['fps']==60
assert (report['width'],report['height'])==(1920,1080)
assert report['maxIKLengthError']<1e-5,report
assert report['checkedContactIntervals']>100 and report['maxContactDrift']<1e-5,report
assert report['nearFeatureTravelPixels']>report['farFeatureTravelPixels']
report['scope']='Analytical sagittal two-bone lengths and ankle targets during full-gain stance; not rendered foot-pixel tracking, motion capture, or a perceptual quality score.'
(r.HERE/'Validation-v10-file.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
