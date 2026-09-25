"""Build the 60-second trailer from approved opening footage and Unity captures."""
from pathlib import Path
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
FF = next((ROOT / 'output/cinematic-tools/imageio_ffmpeg/binaries').glob('*.exe'))
OUT = ROOT / 'ArtSource/CinematicPrototype/V2'
WORK = ROOT / 'output/cinematic-edit-v2'
OUT.mkdir(parents=True, exist_ok=True)
WORK.mkdir(parents=True, exist_ok=True)

def run(args):
    subprocess.run([str(FF), '-hide_banner', '-loglevel', 'error', '-y'] + args, cwd=ROOT, check=True)

# Every tuple is (name, seconds, source, source start seconds). No synthetic gameplay frames.
cuts = [
    ('opening-news', 5, 'opening', 2),
    ('opening-arrival', 3, 'opening', 36),
    ('corridor', 3, 'frames', 0),
    ('hospital', 4, 'frames', 0),
    ('garden', 5, 'frames', 0),
    ('clue', 3, 'v1-clue', 0),
    ('bossOrbit', 7, 'frames', 0),
    ('abductionB', 5, 'frames', 1.1),
    ('stage1', 6, 'frames', .3),
    ('stage2final', 6, 'frames', .3),
    ('boss1', 4, 'frames', .7),
    ('boss2', 4, 'frames', .7),
    ('title', 5, 'title', 0),
]
font = "fontfile='C\\:/Windows/Fonts/malgun.ttf'"
(WORK / 'title.txt').write_text('RE:DREAM', encoding='utf-8')
(WORK / 'tagline.txt').write_text('꿈의 틈 너머로', encoding='utf-8')
manifest = []
elapsed = 0
for index, (name, seconds, source, start) in enumerate(cuts):
    if source == 'opening':
        inputs = ['-ss', str(start), '-i', 'Assets/StreamingAssets/Cinematics/OpeningAndHospital.mp4']
    elif source == 'title':
        inputs = ['-f', 'lavfi', '-i', f'color=c=0x0C101B:s=1280x720:r=24:d={seconds}']
    else:
        folder = 'output/cinematic-frames/02' if source == 'v1-clue' else f'output/cinematic-v2-frames/{name}'
        inputs = ['-framerate', '24', '-start_number', str(round(start * 24)), '-i', f'{folder}/%04d.jpg']
    filters = 'fps=24,scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar=1'
    if name not in ('stage1', 'stage2final', 'boss1', 'boss2'):
        filters += ',drawbox=x=0:y=0:w=iw:h=45:color=black:t=fill,drawbox=x=0:y=ih-45:w=iw:h=45:color=black:t=fill'
    if source == 'title':
        filters += f",drawtext={font}:textfile=output/cinematic-edit-v2/title.txt:fontsize=84:fontcolor=0xE7DFCC:x=(w-tw)/2:y=270"
        filters += f",drawtext={font}:textfile=output/cinematic-edit-v2/tagline.txt:fontsize=28:fontcolor=0xA6B7AA:x=(w-tw)/2:y=390"
    # Short editorial fades only on changes of world, with hard cuts inside action sequences.
    if index in (0, 2, 4, 8, 12):
        filters += ',fade=t=in:st=0:d=0.2'
    if index in (1, 3, 5, 7, 11, 12):
        filters += f',fade=t=out:st={seconds - .22}:d=0.22'
    run(inputs + ['-t', str(seconds), '-an', '-vf', filters, '-c:v', 'libx264', '-crf', '18', '-preset', 'medium', '-pix_fmt', 'yuv420p', str(WORK / f'{index:02}.mp4')])
    manifest.append(dict(cut=index+1, name=name, start=elapsed, end=elapsed+seconds, source=source, source_start=start))
    elapsed += seconds
    run(['-ss', str(seconds / 2), '-i', str(WORK / f'{index:02}.mp4'), '-frames:v', '1', str(OUT / f'cut-{index+1:02}.jpg')])
assert elapsed == 60
(OUT / 'EditDecisionList.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
(WORK / 'concat.txt').write_text(''.join(f"file '{i:02}.mp4'\n" for i in range(len(cuts))), encoding='utf-8')
run(['-f', 'concat', '-safe', '0', '-i', str(WORK / 'concat.txt'), '-c', 'copy', str(WORK / 'picture.mp4')])

audio_inputs = [
    'Assets/Resources/Audio/Hospital/WhispersOfNight.mp3',
    'Assets/Audio/BGM_Boss_Phase2.mp3',
    'Assets/StreamingAssets/Cinematics/OpeningAndHospital.mp4',
    'Assets/Audio/Tentacle_Rise.wav',
    'Assets/Audio/LightShot_1.wav',
    'Assets/Audio/Player_Dash.wav',
]
args = ['-i', str(WORK / 'picture.mp4')]
for source in audio_inputs:
    args += ['-i', source]
mix = (
    '[1:a]atrim=0:60,asetpts=PTS-STARTPTS,volume=0.23,afade=t=in:d=2,afade=t=out:st=51:d=9[a1];'
    '[2:a]atrim=0:36,asetpts=PTS-STARTPTS,volume=0.30,afade=t=in:d=5,afade=t=out:st=29:d=7,adelay=23000|23000[a2];'
    '[3:a]atrim=2:7,asetpts=PTS-STARTPTS,volume=0.85,afade=t=in:d=0.1,afade=t=out:st=4.8:d=0.2[a3];'
    '[4:a]volume=0.32,adelay=30800|30800[a4];'
    '[5:a]asplit=3[s1][s2][s3];'
    '[s1]volume=0.28,adelay=35800|35800[a5];'
    '[s2]volume=0.28,adelay=42800|42800[a6];'
    '[s3]volume=0.28,adelay=48600|48600[a7];'
    '[6:a]volume=0.26,adelay=36800|36800[a8];'
    '[a1][a2][a3][a4][a5][a6][a7][a8]amix=inputs=8:duration=longest:normalize=0,alimiter=limit=0.90,afade=t=out:st=56:d=4[a]'
)
run(args + ['-filter_complex', mix, '-map', '0:v', '-map', '[a]', '-t', '60', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', str(OUT / 'ReDream_Cinematic_v2_60s.mp4')])
run(['-framerate', '1', '-start_number', '1', '-i', str(OUT / 'cut-%02d.jpg'), '-vf', 'scale=426:240,tile=4x4', '-frames:v', '1', str(OUT / 'Storyboard.jpg')])
print(OUT / 'ReDream_Cinematic_v2_60s.mp4')
