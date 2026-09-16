const fs=require('node:fs/promises'),path=require('node:path');
const {spawn}=require('node:child_process');
const {once}=require('node:events');
const {createCanvas,loadImage}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const ff=path.resolve(__dirname,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
// Selected compatible poses. Other generated poses are NOT accepted animation frames.
// Review only: no claim that in-place cycling verifies world-space foot planting.
const keys=[{cell:0,duration:.27},{cell:6,duration:.23},{cell:4,duration:.27},{cell:2,duration:.23}];
(async()=>{
 const im=await loadImage(path.join(__dirname,'detective-walk-study.png'));
 const c=createCanvas(1920,1080),g=c.getContext('2d');
 const p=spawn(ff,['-y','-v','error','-f','rawvideo','-pix_fmt','rgba','-s','1920x1080','-r','30','-i','pipe:0','-an','-c:v','libx264','-crf','18','-preset','fast','-pix_fmt','yuv420p','-movflags','+faststart',path.join(__dirname,'walk-review-draft.mp4')],{windowsHide:true});
 let err='';p.stderr.on('data',b=>err+=b);p.stdin.on('error',()=>{});
 const done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(err)):r());});done.catch(()=>{});
 for(let i=0;i<180;i++){
  let phase=(i/30)%1,pose=keys[0];for(const k of keys){pose=k;if(phase<k.duration)break;phase-=k.duration;}
  g.fillStyle='#bbbdbb';g.fillRect(0,0,1920,1080);
  g.fillStyle='#afb2b0';g.fillRect(0,990,1920,90);
  g.imageSmoothingEnabled=false;
  const sw=im.width/4,sh=im.height/2;
  g.drawImage(im,(pose.cell%4)*sw,Math.floor(pose.cell/4)*sh,sw,sh,510,110,900,900);
  if(i===0)await fs.writeFile(path.join(__dirname,'review-frame.png'),c.toBuffer('image/png'));
  if(!p.stdin.write(Buffer.from(g.getImageData(0,0,1920,1080).data)))await once(p.stdin,'drain');
 }
 p.stdin.end();await done;console.log('Rendered six-second anatomy review draft.');
})();
