const fs=require('node:fs/promises'),path=require('node:path');
const {spawn}=require('node:child_process');const {once}=require('node:events');
const {createCanvas,loadImage}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const OUT=__dirname,OLD=path.resolve(OUT,'../hospital-arrival-v1');
const FF=path.resolve(OUT,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const FPS=60,N=72,S=512,W=1920,H=1080,D=11.6;
const clamp=x=>Math.max(0,Math.min(1,x)),smooth=x=>{x=clamp(x);return x*x*(3-2*x);},lerp=(a,b,t)=>a+(b-a)*t;
const canvas=(w,h)=>createCanvas(w,h);
// Coordinates in the normalized 512px sprite. Persistent landmark topology:
// hat, neck, shoulders, elbows, wrists, pelvis, knees, ankles, heels.
const common=[[256,45],[210,80],[302,80],[256,125],[196,156],[307,156],[256,218],[256,271],[227,303],[285,303]];
const rigs=[
 [...common,[187,220],[190,253],[327,253],[330,286],[237,356],[281,350],[236,421],[279,410],[238,457],[276,435]],
 [...common,[187,215],[189,244],[328,253],[329,285],[240,351],[282,364],[242,381],[281,428],[242,406],[281,458]],
 [...common,[181,250],[180,284],[326,214],[326,244],[235,353],[280,359],[236,414],[284,425],[236,439],[284,458]],
 [...common,[182,250],[182,283],[326,216],[325,244],[235,365],[283,350],[236,430],[279,382],[236,458],[279,406]]
];
function displaced(x,y,from,to){let wx=0,wy=0,ws=0;for(let k=0;k<from.length;k++){const dx=x-from[k][0],dy=y-from[k][1],q=1/Math.pow(dx*dx+dy*dy+110,1.8);wx+=(to[k][0]-from[k][0])*q;wy+=(to[k][1]-from[k][1])*q;ws+=q;}return[x+wx/ws,y+wy/ws];}
function triangle(ctx,img,a,b){
 const den=a[0][0]*(a[1][1]-a[2][1])+a[1][0]*(a[2][1]-a[0][1])+a[2][0]*(a[0][1]-a[1][1]);if(Math.abs(den)<.001)return;
 const coef=v=>[(v[0]*(a[1][1]-a[2][1])+v[1]*(a[2][1]-a[0][1])+v[2]*(a[0][1]-a[1][1]))/den,(v[0]*(a[2][0]-a[1][0])+v[1]*(a[0][0]-a[2][0])+v[2]*(a[1][0]-a[0][0]))/den,(v[0]*(a[1][0]*a[2][1]-a[2][0]*a[1][1])+v[1]*(a[2][0]*a[0][1]-a[0][0]*a[2][1])+v[2]*(a[0][0]*a[1][1]-a[1][0]*a[0][1]))/den];
 const X=coef(b.map(p=>p[0])),Y=coef(b.map(p=>p[1]));ctx.save();ctx.beginPath();ctx.moveTo(...b[0]);ctx.lineTo(...b[1]);ctx.lineTo(...b[2]);ctx.closePath();ctx.clip();ctx.setTransform(X[0],Y[0],X[1],Y[1],X[2],Y[2]);ctx.drawImage(img,0,0);ctx.restore();
}
function warp(img,from,to){
 const c=canvas(S,S),g=c.getContext('2d'),src=img.getContext('2d').getImageData(0,0,S,S).data,out=g.createImageData(S,S),d=out.data;
 // Rasterize one source silhouette directly: no alpha blends between poses,
 // no antialiased triangle clip edges, and no accumulated previous-frame pixels.
 function raster(a,b){
  const den=(b[1][1]-b[2][1])*(b[0][0]-b[2][0])+(b[2][0]-b[1][0])*(b[0][1]-b[2][1]);if(Math.abs(den)<1e-6)return;
  const x0=Math.max(0,Math.floor(Math.min(...b.map(p=>p[0])))),x1=Math.min(S-1,Math.ceil(Math.max(...b.map(p=>p[0]))));
  const y0=Math.max(0,Math.floor(Math.min(...b.map(p=>p[1])))),y1=Math.min(S-1,Math.ceil(Math.max(...b.map(p=>p[1]))));
  for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++){
   const px=x+.5,py=y+.5,u=((b[1][1]-b[2][1])*(px-b[2][0])+(b[2][0]-b[1][0])*(py-b[2][1]))/den,v=((b[2][1]-b[0][1])*(px-b[2][0])+(b[0][0]-b[2][0])*(py-b[2][1]))/den,w=1-u-v;
   if(u<-.00001||v<-.00001||w<-.00001)continue;
   const sx=Math.max(0,Math.min(S-1,Math.floor(u*a[0][0]+v*a[1][0]+w*a[2][0]))),sy=Math.max(0,Math.min(S-1,Math.floor(u*a[0][1]+v*a[1][1]+w*a[2][1]))),si=(sy*S+sx)*4,di=(y*S+x)*4;
   d[di]=src[si];d[di+1]=src[si+1];d[di+2]=src[si+2];d[di+3]=src[si+3]>=128?255:0;
  }
 }
 const step=16;for(let y=0;y<S;y+=step)for(let x=0;x<S;x+=step){const a=[[x,y],[x+step,y],[x+step,y+step],[x,y+step]],b=a.map(p=>displaced(...p,from,to));raster([a[0],a[1],a[2]],[b[0],b[1],b[2]]);raster([a[0],a[2],a[3]],[b[0],b[2],b[3]]);}
 g.putImageData(out,0,0);return c;
}
function polygon(g,p){g.beginPath();g.moveTo(...p[0]);for(let i=1;i<p.length;i++)g.lineTo(...p[i]);g.closePath();}
function quad(g,img,src,dst){triangle(g,img,[src[0],src[1],src[2]],[dst[0],dst[1],dst[2]]);triangle(g,img,[src[0],src[2],src[3]],[dst[0],dst[2],dst[3]]);}
async function audio(){const sr=48000,a=new Float32Array(Math.ceil(D*sr));let seed=34873;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};const event=(at,len,f)=>{for(let j=0;j<len*sr;j++){let i=Math.round(at*sr)+j;if(i<a.length)a[i]+=f(j/sr);}};
 for(let t=1.05;t<6.8;t+=.6){const volume=lerp(.19,.07,(t-1.05)/5.8);event(t,.17,s=>volume*(.6*Math.sin(2*Math.PI*85*s)+.4*rand())*Math.exp(-s*32));}
 for(const t of [8.48,9.08,9.68])event(t,.15,s=>.055*(Math.sin(2*Math.PI*95*s)+rand()*.3)*Math.exp(-s*35));
 event(7.45,.13,s=>.08*rand()*Math.exp(-s*40));event(7.52,.73,s=>.018*(Math.sin(2*Math.PI*(350*s-80*s*s))+rand()*.2)*Math.sin(Math.PI*s/.73));event(11,.17,s=>.1*(rand()+Math.sin(2*Math.PI*130*s))*Math.exp(-s*30));
 const b=Buffer.alloc(44+a.length*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(sr,24);b.writeUInt32LE(sr*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(a.length*2,40);for(let i=0;i<a.length;i++)b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,a[i]))*32767),44+i*2);await fs.writeFile(path.join(OUT,'arrival-foley.wav'),b);}
async function encode(name,duration,draw,sound){const c=canvas(W,H),g=c.getContext('2d');const args=['-y','-v','error','-f','rawvideo','-pix_fmt','rgba','-s',`${W}x${H}`,'-r',String(FPS),'-i','pipe:0'];if(sound)args.push('-i',path.join(OUT,'arrival-foley.wav'));args.push('-c:v','libx264','-crf','18','-preset','fast','-pix_fmt','yuv420p');if(sound)args.push('-c:a','aac','-b:a','160k');args.push('-t',String(duration),'-movflags','+faststart',path.join(OUT,name));const p=spawn(FF,args,{windowsHide:true});let err='';p.stderr.on('data',b=>err+=b);p.stdin.on('error',()=>{});const done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(err)):r());});done.catch(()=>{});
 for(let i=0;i<duration*FPS;i++){g.setTransform(1,0,0,1,0,0);g.globalAlpha=1;draw(g,i/FPS);if(sound&&[30,65,150,213,243,270,330].includes(i))await fs.writeFile(path.join(OUT,`frame-${i}.png`),c.toBuffer('image/png'));if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await once(p.stdin,'drain');}p.stdin.end();await done;}
(async()=>{
 const sheet=await loadImage(path.join(OLD,'detective-walk-study.png')),bg=await loadImage(path.join(OLD,'hospital-background.png')),op=await loadImage(path.join(OUT,'hospital-open.png')),reachIm=await loadImage(path.join(OUT,'detective-reach.png'));
 const key=[0,6,4,2].map(n=>{const c=canvas(S,S),g=c.getContext('2d');g.imageSmoothingEnabled=false;g.drawImage(sheet,n%4*sheet.width/4,Math.floor(n/4)*sheet.height/2,sheet.width/4,sheet.height/2,30,30,444,444);return c;});
 const cycle=[],cycleRigs=[];for(let i=0;i<N;i++){const pos=i/N*4,k=Math.floor(pos),t=smooth(pos-k),j=(k+1)%4,target=rigs[k].map((p,n)=>p.map((v,l)=>lerp(v,rigs[j][n][l],t)));cycleRigs.push(target);cycle.push(warp(key[0],rigs[0],target));if(i%18===0)console.log('Single silhouette',i,'/',N);}
 const reach=canvas(S,S),rg=reach.getContext('2d');rg.imageSmoothingEnabled=false;
 // Same head/body baseline as the accepted walk poses; generated source is 1260 square.
 const rs=422/(reachIm.height*.86);rg.drawImage(reachIm,256-reachIm.width*.475*rs,40-reachIm.height*.065*rs,reachIm.width*rs,reachIm.height*rs);
 // Align both silhouettes to shared landmarks BEFORE blending the stop/reach.
 const reachRig=[...common,[184,225],[185,268],[329,191],[355,177],[235,358],[280,358],[235,431],[280,431],[235,458],[280,458]];
 const tween=(a,ar,b,br,q)=>{const target=ar.map((p,n)=>p.map((v,l)=>lerp(v,br[n][l],q)));return warp(key[0],rigs[0],target);};
 const stop=Array.from({length:34},(_,i)=>tween(cycle[12],cycleRigs[12],reach,reachRig,smooth(i/33)));
 const release=Array.from({length:18},(_,i)=>{const k=i%N;return tween(reach,reachRig,cycle[k],cycleRigs[k],smooth(i/17));});
 const closed=canvas(W,H),cg=closed.getContext('2d');cg.drawImage(bg,0,0,W,H);
 const right=[[958,704],[1163,704],[1190,1023],[958,1023]],full=[[773,704],[1163,704],[1190,1023],[741,1023]];
 function door(g,amount){const ang=amount*Math.PI*.455,cs=Math.cos(ang),sn=Math.sin(ang);const dest=[[1163-205*cs,704+40*sn],[1163,704],[1190,1023],[1190-232*cs,1023-34*sn]];quad(g,closed,right,dest);}
 function actor(g,sprite,x,foot,h,dark=0){const f=h/422,dx=x-256*f,dy=foot-459*f;g.save();g.imageSmoothingEnabled=false;g.drawImage(sprite,dx,dy,512*f,512*f);if(dark){const shade=canvas(S,S),sg=shade.getContext('2d');sg.drawImage(sprite,0,0);sg.globalCompositeOperation='source-atop';sg.fillStyle=`rgba(20,30,45,${dark})`;sg.fillRect(0,0,S,S);g.drawImage(shade,dx,dy,512*f,512*f);}g.restore();}
 await audio();
 await encode('walk-smooth-review-1080p.mp4',4.8,(g,t)=>{g.fillStyle='#bbbdbb';g.fillRect(0,0,W,H);g.fillStyle='#afb2b0';g.fillRect(0,990,W,90);actor(g,cycle[Math.floor(t*FPS)%N],960,990,850);},false);
 await encode('hospital-entry-1080p.mp4',D,(g,t)=>{
  g.drawImage(closed,0,0);let open=t<7.5?0:t<8.25?smooth((t-7.5)/.75):t<10.2?1:1-smooth((t-10.2)/.8);
  if(open>0){g.save();polygon(g,right);g.clip();g.drawImage(op,0,0,W,H);g.restore();}
  if(t<.7)return;
  if(t<8.25){if(open>0)door(g,open);let walk=clamp((t-.7)/6.2),z=lerp(2.2,7.8,walk),h=2000/z,foot=920+800/z+(1-smooth((t-.7)/.55))*1050,x=lerp(960,928,smooth(walk));
   let sprite=cycle[Math.floor(Math.max(0,t-.7)*FPS)%N];if(t>=6.9){h=2000/7.8;foot=920+800/7.8;x=928;sprite=stop[Math.min(33,Math.round((t-6.9)*FPS))];}
   if(t>=7.65){const q=smooth((t-7.65)/.6);x=lerp(928,1024,q);const k=Math.floor((t-7.65)*FPS);sprite=k<18?release[k]:cycle[k%N];}
   actor(g,sprite,x,foot,h);
  }else{let q=clamp((t-8.25)/1.85),x=lerp(1024,1068,smooth(q*2)),h=lerp(2000/7.8,176,q),foot=lerp(920+800/7.8,976,q);
   g.save();polygon(g,full);g.clip();actor(g,cycle[Math.floor((t-7.65)*FPS)%N],x,foot,h,.48*smooth(q));g.restore();if(open>0)door(g,open);else g.drawImage(closed,0,0);
  }
 },true);
 const board=canvas(1280,720),b=board.getContext('2d');for(const [j,i] of [65,150,243,270].entries()){const im=await loadImage(path.join(OUT,`frame-${i}.png`));b.drawImage(im,(j%2)*640,Math.floor(j/2)*360,640,360);}await fs.writeFile(path.join(OUT,'contact-sheet.png'),board.toBuffer('image/png'));
 await fs.writeFile(path.join(OUT,'render-metadata.json'),JSON.stringify({width:W,height:H,fps:FPS,duration:D,cycleFrames:N,cycleSeconds:1.2,method:'landmark-guided mesh inbetweens from four approved poses; not 36 hand-drawn poses',door:'right leaf hinged inward; reaches before rotation; interior occlusion after threshold',outputs:['hospital-entry-1080p.mp4','walk-smooth-review-1080p.mp4']},null,2));console.log('Rendered hospital entry and smooth gait review.');
})();
