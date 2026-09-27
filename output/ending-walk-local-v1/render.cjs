// Local sprite-frame animation. No Higgsfield, optical flow or frame blending.
const fs=require('fs'),path=require('path'),{spawn}=require('child_process'),{once}=require('events');
const {createCanvas,loadImage}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const FF='C:/Ondukong/_backup_4Team_20260916/output/game-opening/render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe';
const O=__dirname,W=1920,H=1080,FPS=30,DUR=11;
const clamp=x=>Math.max(0,Math.min(1,x)),smooth=x=>{x=clamp(x);return x*x*(3-2*x)};
(async()=>{
 const bg=await loadImage(path.join(O,'corridor-clean.png')),sheet=await loadImage(path.join(O,'walk-sheet.png'));
 const sc=createCanvas(sheet.width,sheet.height),sg=sc.getContext('2d');sg.drawImage(sheet,0,0);
 const px=sg.getImageData(0,0,sheet.width,sheet.height).data,poses=[];
 for(let i=0;i<8;i++){
  const x0=Math.round(i%4*sheet.width/4),x1=Math.round((i%4+1)*sheet.width/4),y0=Math.round(Math.floor(i/4)*sheet.height/2),y1=Math.round((Math.floor(i/4)+1)*sheet.height/2);
  let l=x1,r=x0,t=y1,b=y0;
  for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++)if(px[(y*sheet.width+x)*4+3]>200){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y)}
  const c=createCanvas(240,460),g=c.getContext('2d');g.imageSmoothingEnabled=false;
  const scale=430/(b-t+1),ww=(r-l+1)*scale;
  g.drawImage(sheet,l,t,r-l+1,b-t+1,120-ww/2,12,ww,430);
 fs.writeFileSync(path.join(O,`pose-${i}.png`),c.toBuffer('image/png'));poses.push(c);
 }
 const action=await loadImage(path.join(O,'door-poses.png')),ac=createCanvas(action.width,action.height),ag=ac.getContext('2d');ag.drawImage(action,0,0);
 const ap=ag.getImageData(0,0,action.width,action.height).data,actions=[];
 for(let i=0;i<4;i++){
  const x0=Math.round(i%2*action.width/2),x1=Math.round((i%2+1)*action.width/2),y0=Math.round(Math.floor(i/2)*action.height/2),y1=Math.round((Math.floor(i/2)+1)*action.height/2);
  let l=x1,r=x0,t=y1,b=y0;
  for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++)if(ap[(y*action.width+x)*4+3]>200){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y)}
  let hl=r,hr=l;for(let y=t;y<t+(b-t)*.18;y++)for(let x=l;x<=r;x++)if(ap[(Math.floor(y)*action.width+x)*4+3]>200){hl=Math.min(hl,x);hr=Math.max(hr,x)}
  const c=createCanvas(300,460),g=c.getContext('2d'),s=430/(b-t+1);g.imageSmoothingEnabled=false;
  g.drawImage(action,l,t,r-l+1,b-t+1,150-((hl+hr)/2-l)*s,12,(r-l+1)*s,430);actions.push(c);
 }
 const opened=await loadImage(path.join(O,'door-opening-background.png'));
 const ratio=W/bg.width,dx=924*ratio,dy=250*ratio,dw=145*ratio,dh=332*ratio;
 const c=createCanvas(W,H),g=c.getContext('2d');
 const p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s',`${W}x${H}`,'-r',String(FPS),'-i','pipe:0','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',path.join(O,'corridor-exit-silent.mp4')],{windowsHide:true});
 let err='';p.stderr.on('data',d=>err+=d);const done=new Promise((r,j)=>{p.on('error',j);p.on('close',code=>code?j(Error(err)):r())});done.catch(()=>{});p.stdin.on('error',()=>{});
 const board=createCanvas(1440,810),bc=board.getContext('2d'),checks=[.5,2,4.8,5.6,6.3,7,7.6,8.4,9.8],states=[];
 for(let n=0;n<DUR*FPS;n++){
  const time=n/FPS,walk=clamp((time-.35)/5),dist=walk<.9?walk:(.9+.1*smooth((walk-.9)/.1));
  // Perspective on a fixed ground plane: size and location share one depth value.
  const z=1+1.02*dist,k=1/z;
  let x=1340+(930-1340)*k,y=360+(977-360)*k,h=730*k;
  const phase=Math.min(9.99,Math.max(0,time-.35)*2),step=Math.floor(phase),u=phase-step;
  const pi= time>=5.35?2:((step%2)*4+Math.min(3,Math.floor(u*4)));
  g.globalAlpha=1;g.imageSmoothingEnabled=false;g.drawImage(bg,0,0,W,H);
  const opening=smooth((time-6.1)/1.0),angle=opening*1.46;
  if(opening>0){
   // Only replace the door aperture; the rest of the background is immutable.
   g.drawImage(opened,924,250,145,332,dx,dy,dw,dh);
   // Project the original door leaf in strips about the fixed left hinge.
   for(let j=0;j<145;j++){
    const u=j/145,shift=Math.sin(angle)*42*ratio*u;
    g.drawImage(bg,924+j,250,1,332,dx+u*dw*Math.cos(angle),dy+shift,Math.max(1,ratio*Math.cos(angle)),dh-2*shift);
   }
  }
  if(time>=7.1){const q=clamp((time-7.1)/2.2);x=1137-155*smooth(q);y=665-70*q;h=361-65*q;}
  // Small contact shadow, never an accumulated previous frame.
  if(time<7.5){g.fillStyle='rgba(30,35,42,.18)';g.beginPath();g.ellipse(x,y-5,55*k,10*k,0,0,Math.PI*2);g.fill();}
  const s=h/430,bob=time<5.35?Math.sin(u*Math.PI)*3*k:0;
  let sprite=poses[pi],cx=120;
  if(time>=5.35&&time<7.3){sprite=actions[time<5.75?0:time<6.1?1:time<6.9?2:3];cx=150;}
  if(time>=7.3){sprite=poses[Math.floor((time-7.3)*8)%8];}
  g.save();
  if(time>=7.5){g.beginPath();g.rect(dx,dy,dw,dh);g.clip();}
  if(time<9.3)g.drawImage(sprite,Math.round(x-cx*s),Math.round(y-442*s-bob),Math.round(sprite.width*s),Math.round(460*s));
  g.restore();
  if(time>=10.4){g.fillStyle=`rgba(0,0,0,${smooth((time-10.4)/.6)})`;g.fillRect(0,0,W,H)}
  if(time<.35){g.fillStyle=`rgba(0,0,0,${1-smooth(time/.35)})`;g.fillRect(0,0,W,H)}
  states.push({frame:n,time,pose:pi,x,y,height:h});
  const ci=checks.findIndex(v=>Math.round(v*FPS)===n);if(ci>=0){fs.writeFileSync(path.join(O,`check-${ci}.png`),c.toBuffer('image/png'));bc.drawImage(c,ci%3*480,Math.floor(ci/3)*270,480,270)}
  if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await Promise.race([once(p.stdin,'drain'),done]);
 }
 p.stdin.end();await done;
 fs.writeFileSync(path.join(O,'review-sheet.png'),board.toBuffer('image/png'));
 fs.writeFileSync(path.join(O,'motion.json'),JSON.stringify(states));
 console.log('Rendered 330 frames. Local pixel exit blocking review.');
})();
