const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const {spawn}=require('node:child_process'),{once}=require('node:events');
const {createCanvas,loadImage,GlobalFonts}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const OUT=__dirname,ROOT=path.dirname(OUT),OLD=path.join(ROOT,'opening-restored-v1');
const FF=path.join(ROOT,'render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const W=1920,H=1080,FPS=30,N=999,clamp=x=>Math.max(0,Math.min(1,x)),lerp=(a,b,t)=>a+(b-a)*t;
const ease=x=>{x=clamp(x);return x*x*x*(x*(x*6-15)+10);};
const load=f=>loadImage(path.join(ROOT,f)),pad=n=>String(n).padStart(3,'0');
const expertText='지금으로선 의학·과학적으로 설명할 수 있는 근거가 없어...';
function caption(g,img){
 // Replace only the text-bearing central ribbon with its own blank scanline texture.
 // No face, body, interview nameplate or ribbon edge is repainted.
 g.drawImage(img,300,932,8,92,560,932,810,92);
 g.font='bold 48px Korean';g.textAlign='center';g.textBaseline='middle';g.fillStyle='#173b55';
 if(g.measureText(expertText).width>1740)throw Error('Caption overflow');
 g.fillText(expertText,W/2,979);g.textAlign='start';
}
(async()=>{
 GlobalFonts.registerFromPath('C:/Windows/Fonts/malgunbd.ttf','Korean');
 const env=await loadImage(path.join(OUT,'window-environment.png')),sky=await load('scene04-reference-motion-v5/check-120.png'),title=await load('opening-integrated-v2/check-title.png');
 const timing=JSON.parse(await fs.readFile(path.join(ROOT,'scene04-reference-motion-v6/timing.json'),'utf8'));
 // These 25 source frames are already full-screen and share identical camera geometry.
 // Reuse the existing mouth animation, not the earlier frames containing a baked zoom.
 const report=await Promise.all(Array.from({length:25},(_,j)=>loadImage(path.join(OLD,`report-frames/${pad(65+j)}.png`))));
 const c=createCanvas(W,H),g=c.getContext('2d'),crt=createCanvas(W,H),cg=crt.getContext('2d');
 const lastExpert=await loadImage(path.join(OLD,'news-frames/179.png'));cg.drawImage(lastExpert,0,0);caption(cg,lastExpert);
 const preview=process.argv.includes('--stills');
 if(!preview&&!process.argv.includes('--transition'))throw Error('Integration paused by user: choose --stills or --transition only');
 const output=path.join(OUT,'tv-zoom-review-6s.mp4'),audio=path.join(OLD,'opening-restored-final-1080p.mp4');
 let p,done,errors='';
 if(!preview){p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s','1920x1080','-r','30','-i','pipe:0','-i',audio,'-map','0:v','-map','1:a:0','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-c:a','copy','-t','6','-movflags','+faststart',output],{windowsHide:true});p.stderr.on('data',x=>errors+=x);p.stdin.on('error',()=>{});done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(errors)):r());});done.catch(()=>{});}
 const shots=[0,54,75,90,105,120,129,135,143,144,234,270,323,328,335,429,449,470,490,515,535,552,765,965];
 const camera=[];
 for(let i=0;i<(preview?N:180);i++){
  if(preview&&!shots.includes(i))continue;
  const t=i/FPS;g.globalAlpha=1;g.imageSmoothingEnabled=true;g.imageSmoothingQuality='high';g.fillStyle='#000';g.fillRect(0,0,W,H);
  if(i<144){
   const lobby=await load(`scene01-02-softscreen-preview/lobby/${pad(Math.min(89,i)+1)}.png`);
   const news=report[i%25];
   const q=ease((t-1.8)/2.5),cw=W/Math.exp(Math.log(W/285)*q),ch=cw*H/W;
   const x=1519*(W-cw)/(W-285),y=600*(H-ch)/(H-285*H/W),s=W/cw;
   g.drawImage(lobby,x,y,cw,ch,0,0,W,H);
   // A single camera transform. Render the full-resolution screen directly to output,
   // avoiding a low-resolution 285px intermediate and any second zoom or size reset.
   const dx=(1519-x)*s,dy=(600-y)*s,dw=285*s,dh=285*H/W*s;
   g.save();g.beginPath();g.roundRect(dx,dy,dw,dh,6*s*(1-q));g.clip();g.drawImage(news,dx,dy,dw,dh);g.restore();
   camera.push({i,x,y,cw,ch,newsWidth:dw,newsHeight:dh});
  }else if(i<324){
   const img=await loadImage(path.join(OLD,`news-frames/${pad(i-144)}.png`));g.drawImage(img,0,0,W,H);if(i>=234)caption(g,img);
  }else if(i<339){
   const f=i-324;
   if(f<7){const h=Math.max(2,H*Math.pow(1-f/7,3));g.drawImage(crt,0,(H-h)/2,W,h);g.fillStyle=`rgba(215,235,255,${f/14})`;g.fillRect(0,(H-h)/2,W,h);}
   else if(f<12){g.fillStyle=`rgba(200,220,240,${.2*(12-f)/5})`;g.fillRect(W/2-W*(12-f)/10,H/2-1,W*(12-f)/5,2);}
  }else if(i<429){g.drawImage(await loadImage(path.join(OLD,`news-frames/${pad(i-144)}.png`)),0,0,W,H);}
  else if(i<909){
   const st=t-14.3,q=ease((st-.7)/3.3),cw=lerp(.60,.40,q)*env.width,x=lerp(.40,.005,q)*env.width,y=lerp(.34,.008,q)*env.height;
   g.drawImage(env,x,y,cw,cw*H/W,0,0,W,H);
   const sk=ease((st-3.5)/.5);if(sk>0){g.globalAlpha=sk;const drift=clamp((st-4)/12);g.drawImage(sky,lerp(0,sky.width*.018,drift),sky.height*.005,sky.width*.975,sky.height*.975,0,0,W,H);g.globalAlpha=1;}
   if(st>=9){const visible=timing.beats.filter(b=>b.time<=st).map(b=>b.char).join('');g.save();g.font='bold 46px Korean';g.textBaseline='middle';g.strokeStyle='rgba(21,44,70,.8)';g.lineWidth=5;g.fillStyle='#fff';const tx=(W-g.measureText(timing.text).width)/2;g.strokeText(visible,tx,950);g.fillText(visible,tx,950);g.restore();}
   const fade=ease((st-15.5)/.5);if(fade){g.fillStyle=`rgba(0,0,0,${fade})`;g.fillRect(0,0,W,H);}
  }else{g.globalAlpha=ease((t-30.3)/.35);g.drawImage(title,0,0,W,H);g.globalAlpha=1;}
  if(shots.includes(i))await fs.writeFile(path.join(OUT,`check-${i}.png`),c.toBuffer('image/png'));
  if(!preview){if(i%150===0)console.log('Frame',i,'of',N);if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await Promise.race([once(p.stdin,'drain'),done]);}
 }
 const board=createCanvas(1440,810),bg=board.getContext('2d');for(const [j,i]of [54,105,135,144,270,429,470,552,765].entries())bg.drawImage(await loadImage(path.join(OUT,`check-${i}.png`)),j%3*480,Math.floor(j/3)*270,480,270);await fs.writeFile(path.join(OUT,'review-sheet.png'),board.toBuffer('image/png'));
 if(preview)return;
 p.stdin.end();await done;
 for(let j=1;j<camera.length;j++)if(camera[j].newsWidth+1e-6<camera[j-1].newsWidth)throw Error('Zoom reversal');
 if(Math.abs(camera.at(-1).newsWidth-W)>1e-6)throw Error('Zoom endpoint mismatch');
 const bytes=await fs.readFile(output);await fs.writeFile(path.join(OUT,'manifest.json'),JSON.stringify({output,size:[W,H],fps:FPS,frames:180,duration:6,bytes:bytes.length,sha256:crypto.createHash('sha256').update(bytes).digest('hex'),audio:'First 6 seconds of existing AAC copied, not remixed',integration:'PAUSED BY USER — review cuts only, prior final untouched',changes:['One monotonic lobby camera; fixed-size news layout; full-screen hold at 4.3s','Expert caption still: one line, 48px (previous about40px)','Window still: reference-guided pixel-art; wider crop; preserved gaze direction'],source:audio,hospitalIncluded:false,cameraChecks:'Monotonic screen width; exact 1920px endpoint',caption:expertText},null,2));console.log(output);
})();
