const fs=require('node:fs'),path=require('node:path'),{execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'..'),ff=path.join(root,'render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const rate=48000,duration=10.675,n=Math.ceil(rate*duration),samples=new Float32Array(n);
let seed=91826,low=0;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296*2-1;};
// Subdued outdoor noise, no indoor reverb or door sounds. Fade gently at clip boundaries.
for(let i=0;i<n;i++){const t=i/rate;low=.997*low+.003*rand();samples[i]=low*.045*Math.min(1,t/.6,(duration-t)/.25)*(1+.15*Math.sin(t*.7));}
// Approximate foot contacts selected from the generated 3D footage, relative to its moving start.
const contacts=[.50,1.25,2.00,2.67,3.33,4.00,4.67,5.33];
const events=contacts.map((t,j)=>({time:4.8+t,gain:.23*Math.exp(-j*.17)}));
for(const {time,gain} of events){let noise=0;for(let k=0;k<rate*.31;k++){const t=k/rate,i=Math.round(time*rate)+k;if(i>=n)break;noise=.68*noise+.32*rand();const heel=Math.sin(2*Math.PI*(88*t-37*t*t))*Math.exp(-t*31),crunch=noise*Math.exp(-t*21),sole=Math.sin(2*Math.PI*125*t)*Math.exp(-Math.abs(t-.065)*60);samples[i]+=gain*(heel*.56+crunch*.55+sole*.18)*(1-Math.exp(-t*700));}}
const b=Buffer.alloc(44+n*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(rate,24);b.writeUInt32LE(rate*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(n*2,40);samples.forEach((v,i)=>b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,v))*32767),44+i*2));
const wav=path.join(__dirname,'3d-footsteps-ambience.wav');fs.writeFileSync(wav,b);
const output=path.join(__dirname,'hospital-pixel-to-3d-with-sound.mp4');
execFileSync(ff,['-y','-v','error','-i',path.join(root,'hospital-local-polish-v3/pixel-to-game3d-review-v3.mp4'),'-i',wav,'-filter_complex','[0:a][1:a]amix=inputs=2:normalize=0:duration=first,alimiter=limit=0.95:level=false[a]','-map','0:v:0','-map','[a]','-c:v','copy','-c:a','aac','-b:a','192k','-t',String(duration),'-movflags','+faststart',output],{windowsHide:true,stdio:'inherit'});
execFileSync(ff,['-v','error','-i',output,'-f','null','NUL'],{windowsHide:true,stdio:'inherit'});
fs.writeFileSync(path.join(__dirname,'manifest.json'),JSON.stringify({duration,events,video:'Stream copied unchanged from hospital-local-polish-v3',method:'Local synthetic foley matching earlier pixel footsteps; approximate visually selected contacts, decreasing gain, quiet outdoor ambience. No generation credits, no indoor entry added.'},null,2));
console.log(output);
