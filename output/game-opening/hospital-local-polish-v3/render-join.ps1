$ErrorActionPreference = 'Stop'
$root = 'C:/Ondukong/4Team-Project/output/game-opening'
$ff = "$root/render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
$out = "$root/hospital-local-polish-v3"
$filter = '[0:v]trim=duration=4.8,setpts=PTS-STARTPTS,fps=60,setsar=1,format=yuv420p[p];[1:v]fps=60,setsar=1,tpad=start_duration=0.35:start_mode=clone,format=yuv420p[g];[p][g]xfade=transition=fade:duration=0.35:offset=4.45[v];[0:a]atrim=duration=4.8,afade=t=out:st=4.2:d=0.6,apad[a]'
& $ff -y -v error -i "$out/pixel-arrival-smooth.mp4" -i "$root/hospital-local-polish-v1/3d-brightness-corrected.mp4" -filter_complex $filter -map '[v]' -map '[a]' -t 10.675 -c:v libx264 -crf 18 -preset medium -pix_fmt yuv420p -c:a aac -b:a 192k -movflags +faststart "$out/pixel-to-game3d-review-v3.mp4"
if ($LASTEXITCODE) { throw 'Render failed' }
& $ff -y -v error -ss 1.5 -i "$out/pixel-arrival-smooth.mp4" -t 2.7 -vf 'fps=3,scale=480:-1,tile=3x3' -frames:v 1 "$out/arrival-review.png"
& $ff -v error -i "$out/pixel-to-game3d-review-v3.mp4" -f null NUL
if ($LASTEXITCODE) { throw 'Decode failed' }
