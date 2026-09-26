"""V12: held key poses and brief limb-only directional blur at pose changes.
No synthetic walking poses, background deformation, or temporal cross-dissolve.
Door/reach/turn staging is retained; this is an offline review video.
"""
import argparse, json, subprocess
import render_v9 as base
import render_v10 as staging
import render_v11 as previous
from render_v9 import np, cv2, HERE, ROOT, W, H, FPS, SW, SH

WORK = ROOT / 'output/ending-v12'
WORK.mkdir(parents=True, exist_ok=True)


class FixedCamera:
    def __init__(self):
        self.my, self.mx = np.mgrid[0:H, 0:W].astype(np.float32)

    def project(self, x, y, z):
        # Do not call base.project: compose temporarily replaces that symbol.
        return np.array([base.CX + base.FOCAL*x/z,
                         base.CY + base.FOCAL*(base.EYE-y)/z])

    def scale(self, x, z):
        return base.FOCAL*base.HUMAN/z/(base.FLOOR-base.TOP)

    def background(self, plate):
        return plate.copy()


def camera_matrix(t):
    # One uniform transform AFTER compositing. No depth map or local distortion.
    zoom = 1.006 + .018*staging.smoother((t-.4)/10.8)
    return np.float32([[zoom, 0, base.CX*(1-zoom)],
                       [0, zoom, base.CY*(1-zoom)]])


def pose_key(s):
    if s['mode'] == 'approach':
        if s['t'] < .42 or s['t'] >= 5.30:
            return 'a0'
        phase = (s['z']-2.45)/.88
    elif s['mode'] == 'threshold':
        phase = .5+(s['z']-5.85)/.88
    elif s['mode'] == 'exit':
        phase = (s['x']+.15)/.96
        # Drop nearly identical contact cels; preserve passing and raised knee.
        order, edges = ['s0','s2','s3','s4','s6','s7'], [.22,.36,.50,.72,.86,1.0]
        return order[int(np.searchsorted(edges, phase % 1, side='right'))]
    else:
        return None
    # Four distinct rear poses: raised left, planted, raised right, planted.
    # Give planted poses more time than the swing poses.
    order, edges = ['w0','w3','w6','w9'], [.18,.50,.68,1.0]
    return order[int(np.searchsorted(edges, phase % 1, side='right'))]


def transition_blur(actor, key, other_key, strength):
    """Short directional shutter on limbs; no pose morph or whole-frame blur."""
    cel, other = actor.cels[key], actor.cels[other_key]
    result = cel.copy()
    regions = [(65,120,145,285), (205,252,145,285),
               (45,160,330,480), (160,290,330,480)]
    for x0,x1,y0,y1 in regions:
        def center(im):
            alpha = im[y0:y1,x0:x1,3]
            yy,xx = np.mgrid[y0:y1,x0:x1]
            total = float(alpha.sum())
            return np.array([(xx*alpha).sum(),(yy*alpha).sum()])/max(total,.001)
        delta = center(other)-center(cel)
        length = float(np.linalg.norm(delta))
        if length < .25:
            continue
        delta *= min(5.0,length)/length
        mask = np.zeros((SH,SW),np.float32)
        mask[y0:y1,x0:x1] = 1
        if y0 == 330:
            # Keep a planted boot crisp. A lifted boot may receive a short blur.
            alpha = cel[y0:y1,x0:x1,3]
            rows = np.where(np.any(alpha > .7,axis=1))[0]
            if len(rows) and rows[-1]+y0 >= 468:
                mask[438:] = 0
        mask = cv2.GaussianBlur(mask,(7,7),1.2)*strength
        blurred = np.zeros_like(cel)
        for offset,weight in [(-.5,.1),(-.25,.2),(0,.4),(.25,.2),(.5,.1)]:
            matrix = np.float32([[1,0,delta[0]*offset],[0,1,delta[1]*offset]])
            blurred += cv2.warpAffine(cel,matrix,(SW,SH),flags=cv2.INTER_LINEAR)*weight
        result = result*(1-mask[:,:,None])+blurred*mask[:,:,None]
    return np.clip(result,0,1)


def drawn_walk(actor, s):
    key = pose_key(s)
    frame = round(s['t']*FPS)
    # At most four output frames around a real cel change (about 67 ms).
    for boundary in range(frame-1,frame+3):
        if boundary <= 0:
            continue
        before = pose_key(staging.state((boundary-1)/FPS))
        after = pose_key(staging.state(boundary/FPS))
        if before and after and before != after and key in (before,after):
            relative = frame-boundary
            strength = {-2:.25,-1:.70,0:.70,1:.25}.get(relative,0)
            if strength:
                other = after if key == before else before
                return transition_blur(actor,key,other,strength), key, strength
    return actor.cels[key], key, 0.0


def encoder(path, width, height, pixel_scale=False):
    filters = ('scale=960:540:flags=area,scale=1920:1080:flags=neighbor,setsar=1'
               if pixel_scale else 'setsar=1')
    return subprocess.Popen([str(base.FF), '-hide_banner', '-loglevel', 'error',
        '-y', '-f', 'rawvideo', '-pix_fmt', 'bgr24', '-s', f'{width}x{height}',
        '-r', str(FPS), '-i', '-', '-an', '-vf', filters, '-c:v', 'libx264',
        '-crf', '17', '-preset', 'medium', '-pix_fmt', 'yuv420p',
        '-movflags', '+faststart', str(path)], stdin=subprocess.PIPE)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview', action='store_true')
    args = parser.parse_args()
    actor = staging.Actor()
    cam = FixedCamera()
    bg = base.load('corridor-clean.png')[:, :, :3]
    opened = base.load('door-opening-background.png')[:, :, :3]
    checks = [1, 1.12, 1.24, 1.36, 1.48, 1.60, 1.72, 1.84,
              3, 5.35, 6.4, 7, 8.3, 9.8, 10.5, 11.5]
    check_frames = {round(t*FPS):i for i,t in enumerate(checks)}
    if not args.preview:
        movie = encoder(HERE/'detective-exit-keyposes-v12.mp4', W, H, True)
        gait = encoder(HERE/'Walk-isolated-v12.mp4', SW*2, SH*2)
        comparison = encoder(HERE/'Comparison-v11-v12.mp4', SW*4, SH*2)
    samples, stills, gait_stills = [], [], []
    for n in range(round(staging.DURATION*FPS)):
        if args.preview and n not in check_frames:
            continue
        s = staging.state(n/FPS)
        blur = 0.0
        if s['mode'] in ('approach', 'threshold', 'exit'):
            cel, key, blur = drawn_walk(actor, s)
        elif s['mode'] == 'door':
            cel, key = actor.action(s['t']), 'reach/push'
        else:
            cel, key = actor.turn(s['t']), 'turn'
        scene = staging.compose(bg, opened, cel, s, cam)
        matrix = camera_matrix(s['t'])
        scene = cv2.warpAffine(scene, matrix, (W, H), flags=cv2.INTER_LINEAR)
        # A neutral fixed camera/body-size view exposes what the gait actually does.
        isolated = np.full((SH, SW, 3), 48, np.uint8)
        isolated[476:478] = 110
        isolated = np.uint8(np.clip(cel[:, :, :3]*255 + isolated*(1-cel[:, :, 3:4]), 0, 255))
        if n in check_frames:
            stills.append((s['t'], scene))
            if s['t'] < 2:
                gait_stills.append(isolated)
            cv2.imwrite(str(WORK/f'check-{check_frames[n]:02d}.jpg'), scene)
        if not args.preview:
            movie.stdin.write(scene.tobytes())
            if n < 5*FPS:
                gait.stdin.write(cv2.resize(isolated, (SW*2, SH*2), interpolation=cv2.INTER_NEAREST).tobytes())
                old_cel,_ = previous.drawn_walk(actor,s)
                old_view = np.full((SH,SW,3),48,np.uint8)
                old_view[476:478] = 110
                old_view = np.uint8(np.clip(old_cel[:,:,:3]*255+old_view*(1-old_cel[:,:,3:4]),0,255))
                pair = np.concatenate([old_view,isolated],axis=1)
                cv2.putText(pair,'v11',(12,20),0,.5,(255,255,255),1)
                cv2.putText(pair,'v12 - key poses / short blur',(SW+12,20),0,.45,(255,255,255),1)
                comparison.stdin.write(cv2.resize(pair,(SW*4,SH*2),interpolation=cv2.INTER_NEAREST).tobytes())
        samples.append(dict(t=s['t'], mode=s['mode'], cel=key, blur=blur, matrix=matrix.tolist()))
        if n % 120 == 0:
            print('Rendered', n, flush=True)
    if not args.preview:
        for proc in (movie, gait, comparison):
            proc.stdin.close()
            if proc.wait() != 0:
                raise RuntimeError('Encoding failed')
        (WORK/'motion.json').write_text(json.dumps(samples, indent=2))
    sheet = np.zeros((4*265, 4*418, 3), np.uint8)
    for i,(t,im) in enumerate(stills):
        x,y=i%4*418,i//4*265
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet, f'{t:.2f}s', (x+8,y+257), 0, .5, (255,255,255), 1)
    cv2.imwrite(str(HERE/'Storyboard-v12.jpg'),sheet)
    cv2.imwrite(str(WORK/'gait.jpg'),np.concatenate(gait_stills,axis=1))


if __name__ == '__main__':
    main()
