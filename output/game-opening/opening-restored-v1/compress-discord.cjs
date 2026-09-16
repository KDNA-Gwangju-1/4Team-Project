const fs=require('node:fs'),path=require('node:path'),{spawnSync}=require('node:child_process');
const root=__dirname,ff=path.resolve(root,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const input=path.join(root,'opening-restored-final-1080p.mp4'),output=path.join(root,'opening-discord-review-under10MB.mp4');
const common=['-v','error','-y','-i',input,'-map','0:v:0','-c:v','libx264','-preset','slow','-b:v','1950k','-pix_fmt','yuv420p','-passlogfile',path.join(root,'discord-pass')];
function run(args){const r=spawnSync(ff,args,{windowsHide:true,encoding:'utf8'});if(r.status!==0)throw Error(r.stderr||String(r.error));}
run([...common,'-pass','1','-an','-f','null','NUL']);
run([...common,'-map','0:a:0','-pass','2','-c:a','aac','-b:a','96k','-movflags','+faststart',output]);
run(['-v','error','-i',output,'-f','null','NUL']);
const bytes=fs.statSync(output).size;if(bytes>=10000000)throw Error('Over 10MB: '+bytes);
console.log(JSON.stringify({output,bytes,megabytes:bytes/1e6,validated:'Full decode passed',sourcePreserved:true}));
