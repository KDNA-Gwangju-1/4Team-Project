const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const {spawn}=require('node:child_process'),{once}=require('node:events');
const {createCanvas,loadImage,GlobalFonts}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const OUT=__dirname,ROOT=path.dirname(OUT),FF=path.join(ROOT,'render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const W=1920,H=1080,FPS=30,D=33.3,clamp=x=>Math.max(0,Math.min(1,x)),lerp=(a,b,t)=>a+(b-a)*t,ease=x=>{x=clamp(x);return x*x*x*(x*(x*6-15)+10);};
const load=f=>loadImage(path.join(ROOT,f));
(async()=>{
 GlobalFonts.registerFromPath('C:/Windows/Fonts/malgunbd.ttf','Korean');
 const env=await load('scene04-reference-motion-v6/environment.png'),sky=await load('scene04-reference-motion-v5/check-120.png'),title=await load('opening-integrated-v2/check-title.png');
 const timing=JSON.parse(await fs.readFile(path.join(ROOT,'scene04-reference-motion-v6/timing.json'),'utf8'));
 const c=createCanvas(W,H),g=c.getContext('2d'),l=createCanvas(W,H),lg=l.getContext('2d');
 const output=path.join(OUT,'opening-restored-33s-1080p.mp4'),audio=path.join(ROOT,'opening-soundmix-v1/opening-mix-premaster.wav');
 const p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s','1920x1080','-r','30','-i','pipe:0','-i',audio,'-map','0:v','-map','1:a','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-b:a','256k','-af','alimiter=limit=0.684:level=false','-t',String(D),'-movflags','+faststart',output],{windowsHide:true});let errors='';p.stderr.on('data',x=>errors+=x);p.stdin.on('error',()=>{});const done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(errors)):r());});done.catch(()=>{});
 const shotFrames=[0,54,105,143,180,270,334,380,430,470,515,552,690,765,909,965];
 for(let i=0;i<999;i++){
  const t=i/FPS;g.globalAlpha=1;g.imageSmoothingEnabled=false;g.fillStyle='#000';g.fillRect(0,0,W,H);
  if(i<144){
   const li=Math.min(89,i),lobby=await load(`scene01-02-softscreen-preview/lobby/${String(li+1).padStart(3,'0')}.png`),ri=Math.min(89,Math.max(0,i-54)),news=await loadImage(path.join(OUT,`report-frames/${String(ri).padStart(3,'0')}.png`));
   lg.imageSmoothingEnabled=false;lg.drawImage(lobby,0,0,W,H);
   // Report source begins with its own TV frame; its screen expands to full frame.
   const rq=ease(Math.min(1,ri/65)),rx=lerp(452,0,rq),ry=lerp(195,0,rq),rw=lerp(995,1920,rq),rh=lerp(597,1080,rq);
   lg.save();lg.beginPath();lg.roundRect(1519,600,285,158,6);lg.clip();lg.drawImage(news,rx,ry,rw,rh,1519,600,285,158);lg.restore();
   const q=ease((t-1.8)/3),scale=Math.exp(Math.log(W/285)*q),cw=W/scale,ch=H/scale,cx=1519*(W-cw)/(W-285)+cw/2,cy=600*(H-ch)/(H-285*H/W)+ch/2;
   g.drawImage(l,cx-cw/2,cy-ch/2,cw,ch,0,0,W,H);
  }else if(i<429){g.drawImage(await loadImage(path.join(OUT,`news-frames/${String(i-144).padStart(3,'0')}.png`)),0,0,W,H);}
  else if(i<909){
   const st=t-14.3,q=ease((st-.7)/3.3),cropW=lerp(.48,.40,q),cropX=lerp(.51,.005,q),cropY=lerp(.25,.008,q);
   g.drawImage(env,cropX*env.width,cropY*env.height,cropW*env.width,cropW*env.width*H/W,0,0,W,H);
   // Clouds-only handoff after the building has left the camera view.
   const sk=ease((st-3.5)/.5);if(sk>0){g.globalAlpha=sk;const drift=clamp((st-4)/12);g.drawImage(sky,lerp(0,sky.width*.018,drift),sky.height*.005,sky.width*.975,sky.height*.975,0,0,W,H);g.globalAlpha=1;}
   if(st>=9){const visible=timing.beats.filter(b=>b.time<=st).map(b=>b.char).join('');g.save();g.font='bold 46px Korean';g.textBaseline='middle';g.strokeStyle='rgba(21,44,70,.8)';g.lineWidth=5;g.fillStyle='#fff';const x=(W-g.measureText(timing.text).width)/2;g.strokeText(visible,x,950);g.fillText(visible,x,950);g.restore();}
   const fade=ease((st-15.5)/.5);if(fade){g.fillStyle=`rgba(0,0,0,${fade})`;g.fillRect(0,0,W,H);}
  }else{g.globalAlpha=ease((t-30.3)/.35);g.drawImage(title,0,0,W,H);g.globalAlpha=1;}
  if(shotFrames.includes(i))await fs.writeFile(path.join(OUT,`check-${i}.png`),c.toBuffer('image/png'));
  if(i%150===0)console.log('Frame',i,'of 999');
  if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await Promise.race([once(p.stdin,'drain'),done]);
 }p.stdin.end();await done;
 const board=createCanvas(1280,720),bg=board.getContext('2d');for(const [j,i] of [0,105,180,270,380,430,552,765,965].entries())bg.drawImage(await loadImage(path.join(OUT,`check-${i}.png`)),j%3*426,Math.floor(j/3)*240,426,240);await fs.writeFile(path.join(OUT,'restoration-contact-sheet.png'),board.toBuffer('image/png'));
 const data=await fs.readFile(output),sha256=crypto.createHash('sha256').update(data).digest('hex');await fs.writeFile(path.join(OUT,'manifest.json'),JSON.stringify({output,duration:D,fps:FPS,frames:999,size:[W,H],bytes:data.length,sha256,audioSource:audio,audioProcessing:'Only safety peak limiter; original 33.3-second premaster reused, no remix or new voice',restored:['lobby-to-TV continuous zoom from cached frames','single-environment camera move and sky drift','typewriter subtitle from preserved beat timings'],reused:['existing news/interview/CRT/static video','original premaster','approved environment and sky artwork','title frame'],notClaimed:'Not a byte-identical recovery of the lost final MP4; reconstructed camera paths'},null,2));console.log(output);
})();
