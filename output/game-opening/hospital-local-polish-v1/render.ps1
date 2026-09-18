$ErrorActionPreference = 'Stop'
$ff = 'C:/Ondukong/4Team-Project/output/game-opening/render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$root = 'C:/Ondukong/4Team-Project/output/game-opening'
$out = "$root/hospital-local-polish-v1"
$source = "$root/hospital-higgsfield-v1/hospital-game3d-open-6s.mp4"
# Lift progressive midtone loss while attenuating the correction in highlights.
$grade = "eq=gamma='1+0.30*min(t,5.875)':gamma_weight=0.78:eval=frame"
& $ff -y -hide_banner -loglevel error -i $source -vf "$grade,scale=1920:1080:flags=lanczos,setsar=1" -an -c:v libx264 -crf 17 -preset medium -pix_fmt yuv420p -movflags +faststart "$out/3d-brightness-corrected.mp4"
if ($LASTEXITCODE) { throw 'Grade failed' }
# Pixel motion ends at 4.05s. Keep a short settled beat; dissolve only stationary frames.
# Hold the corrected 3D first frame for 0.2s, then play its original movement.
$filter = '[0:v]trim=duration=4.65,setpts=PTS-STARTPTS,fps=60,setsar=1,format=yuv420p[p];[1:v]fps=60,setsar=1,tpad=start_duration=0.20:start_mode=clone,format=yuv420p[g];[p][g]xfade=transition=fade:duration=0.20:offset=4.45[v];[0:a]atrim=duration=4.65,afade=t=out:st=4.2:d=0.45,apad[a]'
& $ff -y -hide_banner -loglevel error -i "$root/hospital-pre3d-v3/hospital-depth-before-3d-1080p.mp4" -i "$out/3d-brightness-corrected.mp4" -filter_complex $filter -map '[v]' -map '[a]' -t 10.525 -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -c:a aac -b:a 192k -movflags +faststart "$out/pixel-to-game3d-review.mp4"
if ($LASTEXITCODE) { throw 'Transition render failed' }
& $ff -y -hide_banner -loglevel error -i "$out/3d-brightness-corrected.mp4" -vf 'fps=1,scale=480:-1,tile=3x2' -frames:v 1 "$out/grade-review.png"
& $ff -y -hide_banner -loglevel error -ss 4.3 -i "$out/pixel-to-game3d-review.mp4" -t 0.7 -vf 'fps=10,scale=480:-1,tile=4x2' -frames:v 1 "$out/join-review.png"
