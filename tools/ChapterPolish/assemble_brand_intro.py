"""Prepend the official engine badge, 4TEAM ident and game wordmark to the trailer."""
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[2]
FF=next((ROOT/'output/cinematic-tools/imageio_ffmpeg/binaries').glob('*.exe'))
WORK=ROOT/'output/brand-intro';WORK.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'ArtSource/CinematicPrototype/V3';OUT.mkdir(parents=True,exist_ok=True)
font="fontfile='C\\:/Windows/Fonts/malgunbd.ttf'"
def run(args): subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y']+args,cwd=ROOT,check=True)
def encode(inputs,vf,name,complex=False):
    run(inputs+['-t','2','-an', '-filter_complex' if complex else '-vf',vf,'-r','24','-c:v','libx264','-crf','18','-pix_fmt','yuv420p',str(WORK/name)])
notice='RE:DREAM is not sponsored by or affiliated with Unity Technologies or its affiliates.'
notice2='Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.'
(WORK/'notice.txt').write_text(notice,encoding='utf-8');(WORK/'notice2.txt').write_text(notice2,encoding='utf-8')
encode(['-f','lavfi','-i','color=c=white:s=1280x720:r=24:d=2','-loop','1','-i','ArtSource/Branding/MadeWithUnity-official.png'],
       f"[0:v][1:v]overlay=(W-w)/2:(H-h)/2:shortest=1,drawtext={font}:textfile=output/brand-intro/notice.txt:fontsize=13:fontcolor=0x555555:x=(w-tw)/2:y=660,drawtext={font}:textfile=output/brand-intro/notice2.txt:fontsize=13:fontcolor=0x555555:x=(w-tw)/2:y=679",'engine.mp4',True)
tiles=','.join(f'drawbox=x={395+x*24}:y={307+y*24}:w=18:h=18:color=0x{c}:t=fill' for x,y,c in [(0,0,'EEE8DC'),(1,0,'B9D4D2'),(0,1,'C4B2DE'),(1,1,'EEE8DC')])
encode(['-f','lavfi','-i','color=c=0x101522:s=1280x720:r=24:d=2'],
       f"{tiles},drawtext={font}:text=4TEAM:fontsize=90:fontcolor=0xEEE8DC:x=475:y=270,drawtext={font}:text=DEVELOPED BY:fontsize=17:fontcolor=0x98ADB8:x=477:y=388,fade=t=in:d=0.2,fade=t=out:st=1.8:d=0.2",'team.mp4')
(WORK/'game.txt').write_text('RE:DREAM',encoding='utf-8');(WORK/'tagline.txt').write_text('꿈의 틈 너머로',encoding='utf-8')
encode(['-f','lavfi','-i','color=c=0x101522:s=1280x720:r=24:d=2'],
       f"drawtext={font}:textfile=output/brand-intro/game.txt:fontsize=88:fontcolor=0xEEE8DC:x=(w-tw)/2:y=270,drawbox=x=565:y=381:w=150:h=2:color=0xC4B2DE:t=fill,drawtext={font}:textfile=output/brand-intro/tagline.txt:fontsize=25:fontcolor=0xB9D4D2:x=(w-tw)/2:y=409,fade=t=in:d=0.2,fade=t=out:st=1.8:d=0.2",'game.mp4')
(WORK/'intro.txt').write_text("file 'engine.mp4'\nfile 'team.mp4'\nfile 'game.mp4'\n",encoding='utf-8')
run(['-f','concat','-safe','0','-i',str(WORK/'intro.txt'),'-i','Assets/Resources/Audio/Generated/Brand.wav','-filter_complex','[1:a]asplit=2[a][b];[a]volume=0.45,adelay=2000|2000[x];[b]volume=0.3,adelay=4000|4000[y];[x][y]amix=inputs=2:normalize=0,apad=whole_dur=6[m]', '-map','0:v','-map','[m]','-t','6','-c:v','copy','-c:a','aac','-ar','44100','-ac','2',str(OUT/'BrandIntro_6s.mp4')])
# Decode/re-encode the join so audio priming and timebases cannot leave a gap at six seconds.
# V2 contains source segments with differing color metadata. Keep the graph alive
# across those changes; reinitialization resets concat's timestamps mid-film.
run(['-reinit_filter','0','-i',str(OUT/'BrandIntro_6s.mp4'),'-reinit_filter','0','-i','ArtSource/CinematicPrototype/V2/ReDream_Cinematic_v2_60s.mp4','-filter_complex','[0:v]format=yuv420p,setpts=PTS-STARTPTS[v0];[1:v]format=yuv420p,setpts=PTS-STARTPTS[v1];[0:a]atrim=duration=6,asetpts=PTS-STARTPTS[a0];[1:a]atrim=duration=60,asetpts=PTS-STARTPTS[a1];[v0][a0][v1][a1]concat=n=2:v=1:a=1[v][a]','-map','[v]','-map','[a]','-r','24','-c:v','libx264','-crf','18','-preset','medium','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart',str(OUT/'ReDream_Cinematic_v3_66s.mp4')])
for name in ['engine','team','game']:
    run(['-ss','1','-i',str(WORK/(name+'.mp4')),'-frames:v','1',str(OUT/(name+'.png'))])
print(OUT/'ReDream_Cinematic_v3_66s.mp4')
