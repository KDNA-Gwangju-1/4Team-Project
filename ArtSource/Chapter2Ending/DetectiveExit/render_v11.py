"""Repair v10: immutable corridor plate, rigid whole-shot camera, drawn gait.

Walking uses original cels, without TPS, optical-flow synthesis or frame blending.
The existing door/reach/turn staging is retained. No Unity assets are changed.
"""
import argparse, json, subprocess
import render_v9 as base
import render_v10 as staging
from render_v9 import np, cv2, HERE, ROOT, W, H, FPS, SW, SH

WORK = ROOT / 'output/ending-v11'
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


def drawn_walk(actor, s):
    if s['mode'] == 'approach':
        distance = s['z']-2.45
        if s['t'] < .42 or s['t'] >= 5.30:
            return actor.cels['a0'], 'a0'
        phase = distance/.88
        order = [0, 1, 2, 3, 4, 5, 6, 8, 7, 9, 10, 11]
        key = 'w'+str(order[int((phase % 1)*len(order))])
    elif s['mode'] == 'threshold':
        phase = .5+(s['z']-5.85)/.88
        order = [0, 1, 2, 3, 4, 5, 6, 8, 7, 9, 10, 11]
        key = 'w'+str(order[int((phase % 1)*len(order))])
    else:
        phase = (s['x']+.15)/.96
        order = [0, 2, 3, 4, 6, 7]
        key = 's'+str(order[int((phase % 1)*len(order))])
    return actor.cels[key], key


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
        movie = encoder(HERE/'detective-exit-rigid-v11.mp4', W, H, True)
        gait = encoder(HERE/'Walk-isolated-v11.mp4', SW*2, SH*2)
    samples, stills, gait_stills = [], [], []
    for n in range(round(staging.DURATION*FPS)):
        if args.preview and n not in check_frames:
            continue
        s = staging.state(n/FPS)
        if s['mode'] in ('approach', 'threshold', 'exit'):
            cel, key = drawn_walk(actor, s)
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
        samples.append(dict(t=s['t'], mode=s['mode'], cel=key, matrix=matrix.tolist()))
        if n % 120 == 0:
            print('Rendered', n, flush=True)
    if not args.preview:
        for proc in (movie, gait):
            proc.stdin.close()
            if proc.wait() != 0:
                raise RuntimeError('Encoding failed')
        (WORK/'motion.json').write_text(json.dumps(samples, indent=2))
    sheet = np.zeros((4*265, 4*418, 3), np.uint8)
    for i,(t,im) in enumerate(stills):
        x,y=i%4*418,i//4*265
        sheet[y:y+235,x:x+418]=cv2.resize(im,(418,235))
        cv2.putText(sheet, f'{t:.2f}s', (x+8,y+257), 0, .5, (255,255,255), 1)
    cv2.imwrite(str(HERE/'Storyboard-v11.jpg'),sheet)
    cv2.imwrite(str(WORK/'gait.jpg'),np.concatenate(gait_stills,axis=1))


if __name__ == '__main__':
    main()
