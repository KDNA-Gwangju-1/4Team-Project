const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),{spawnSync}=require('node:child_process');
const dir=__dirname,ff=path.resolve(dir,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const final=path.join(dir,'opening-final-1080p.mp4'),review=path.join(dir,'opening-review-under10MB.mp4');
function run(args){const r=spawnSync(ff,args,{windowsHide:true,encoding:'utf8',maxBuffer:10e6});if(r.status!==0)throw Error(r.stderr||String(r.error));return r;}
run(['-v','error','-i',final,'-f','null','NUL']);
const hashAudio=f=>run(['-v','error','-i',f,'-map','0:a:0','-c:a','copy','-f','hash','-hash','sha256','-']).stdout.trim();
const original=path.resolve(dir,'../opening-restored-v1/opening-restored-final-1080p.mp4');
const audioHash=hashAudio(final);if(audioHash!==hashAudio(original))throw Error('Audio stream changed');
console.log('Master full decode passed; original audio hash matches');
const common=['-v','error','-y','-i',final,'-map','0:v:0','-c:v','libx264','-preset','slow','-b:v','1950k','-pix_fmt','yuv420p','-passlogfile',path.join(dir,'review-pass')];
run([...common,'-pass','1','-an','-f','null','NUL']);console.log('Review pass 1 complete');
run([...common,'-map','0:a:0','-pass','2','-c:a','aac','-b:a','96k','-movflags','+faststart',review]);
run(['-v','error','-i',review,'-f','null','NUL']);
if(fs.statSync(review).size>=10000000)throw Error('Review over 10 MB');
const m=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json'),'utf8'));
m.validation={masterFullDecode:true,reviewFullDecode:true,audioHash,originalAudioIdentical:true};m.review={path:review,bytes:fs.statSync(review).size,sha256:crypto.createHash('sha256').update(fs.readFileSync(review)).digest('hex')};
fs.writeFileSync(path.join(dir,'manifest.json'),JSON.stringify(m,null,2));console.log(JSON.stringify(m,null,2));
