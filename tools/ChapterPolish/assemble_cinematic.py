"""Encode the six-cut RE:DREAM cinematic animatic from local Unity renders."""
from pathlib import Path
import subprocess, json
ROOT=Path(__file__).resolve().parents[2]
FF=next((ROOT/'output/cinematic-tools/imageio_ffmpeg/binaries').glob('*.exe'))
OUT=ROOT/'ArtSource/CinematicPrototype';OUT.mkdir(parents=True,exist_ok=True)
WORK=ROOT/'output/cinematic-edit';WORK.mkdir(parents=True,exist_ok=True)
def run(args):
    subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y']+args,cwd=ROOT,check=True)
for i in range(1,6):
    run(['-framerate','24','-i',f'output/cinematic-frames/{i:02d}/%04d.jpg','-vf',
         'drawbox=x=0:y=0:w=iw:h=64:color=black:t=fill,drawbox=x=0:y=ih-64:w=iw:h=64:color=black:t=fill,fade=t=in:st=0:d=0.25,fade=t=out:st=3.7:d=0.3',
         '-c:v','libx264','-preset','medium','-crf','19','-pix_fmt','yuv420p',str(WORK/f'{i:02d}.mp4')])
(WORK/'title.txt').write_text('RE:DREAM',encoding='utf-8')
(WORK/'tagline.txt').write_text('꿈의 틈 너머로',encoding='utf-8')
font="fontfile='C\\:/Windows/Fonts/malgun.ttf'"
run(['-f','lavfi','-i','color=c=0x111421:s=1280x720:r=24:d=3',
     '-vf',f"drawtext={font}:textfile=output/cinematic-edit/title.txt:fontsize=82:fontcolor=0xE7DFCC:x=(w-tw)/2:y=260,drawtext={font}:textfile=output/cinematic-edit/tagline.txt:fontsize=28:fontcolor=0xA6B7AA:x=(w-tw)/2:y=385,fade=t=in:st=0:d=0.4,fade=t=out:st=2.3:d=0.7",
     '-c:v','libx264','-pix_fmt','yuv420p',str(WORK/'06.mp4')])
(WORK/'concat.txt').write_text(''.join(f"file '{i:02d}.mp4'\n" for i in range(1,7)),encoding='utf-8')
run(['-f','concat','-safe','0','-i',str(WORK/'concat.txt'),'-c','copy',str(WORK/'picture.mp4')])
subs='''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 720
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Malgun Gothic,25,&H00EFE9DA,&H000000FF,&H80111421,&H80111421,0,0,0,0,100,100,1,0,1,1,0,2,60,60,22,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:00.60,0:00:03.40,Default,,0,0,0,,{\\fad(250,250)}기억이 피어나는 곳
Dialogue: 0,0:00:04.50,0:00:07.40,Default,,0,0,0,,{\\fad(250,250)}잊힌 이름을 따라
Dialogue: 0,0:00:16.50,0:00:19.50,Default,,0,0,0,,{\\fad(250,250)}꿈의 다른 얼굴을 마주하다
'''
(OUT/'captions.ass').write_text(subs,encoding='utf-8-sig')
run(['-i',str(WORK/'picture.mp4'),'-i','Assets/Resources/Audio/Hospital/WhispersOfNight.mp3',
     '-i','Assets/Audio/Tentacle_Rise.wav','-filter_complex',
     '[0:v]ass=ArtSource/CinematicPrototype/captions.ass[v];[1:a]atrim=0:23,asetpts=PTS-STARTPTS,volume=0.32,afade=t=in:d=1.2,afade=t=out:st=20:d=3[m];[2:a]volume=0.22,adelay=12000|12000[s];[m][s]amix=inputs=2:duration=first:normalize=0[a]',
     '-map','[v]','-map','[a]','-t','23','-c:v','libx264','-crf','18','-preset','medium','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart',str(OUT/'ReDream_Cinematic_Prototype_v1.mp4')])
for i in range(1,7):
    run(['-ss','1.8','-i',str(WORK/f'{i:02d}.mp4'),'-frames:v','1',str(OUT/f'cut-{i:02d}.jpg')])
run(['-i',str(OUT/'ReDream_Cinematic_Prototype_v1.mp4'),'-vf','fps=1/4,scale=426:240,tile=3x2','-frames:v','1',str(OUT/'Storyboard.jpg')])
print(str(OUT/'ReDream_Cinematic_Prototype_v1.mp4'))
