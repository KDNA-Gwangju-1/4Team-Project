const fs=require('node:fs/promises'),path=require('node:path'),{spawn}=require('node:child_process'),{once}=require('node:events');
const {createCanvas,loadImage}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const W=1920,H=1080,FPS=60,D=6.4,FF=path.resolve(__dirname,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const clamp=x=>Math.max(0,Math.min(1,x)),ease=x=>{x=clamp(x);return x*x*(3-2*x)};
(async()=>{const bg=await loadImage(path.resolve(__dirname,'../hospital-pre3d-v1/hospital-background.png')),art=await loadImage(path.join(__dirname,'detective-coat-hem.png')),ref=require('./reflections.cjs').build(bg);
const sprite=createCanvas(art.width,art.height),sg=sprite.getContext('2d');sg.drawImage(art,0,0);const sd=sg.getImageData(0,0,art.width,art.height);for(let i=3;i<sd.data.length;i+=4)sd.data[i]=sd.data[i]<180?0:255;sg.putImageData(sd,0,0);
const c=createCanvas(W,H),g=c.getContext('2d'),s=.82,sw=art.width,sh=art.height,baseX=W/2-sw*s/2,baseY=1012-sh*.938*s;
const out=path.join(__dirname,'hospital-central-before-3d-1080p.mp4');const p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s',`${W}x${H}`,'-r',String(FPS),'-i','pipe:0','-i',path.resolve(__dirname,'../hospital-pre3d-v1/approach-foley.wav'),'-c:v','libx264','-crf','18','-preset','fast','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-t',String(D),'-movflags','+faststart',out],{windowsHide:true});let err='';p.stderr.on('data',b=>err+=b);p.stdin.on('error',()=>{});const done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(err)):r())});done.catch(()=>{});
function leg(t,start,end,left){const q=clamp((t-start)/(end-start)),body=-1550*(1-ease((t-2.83)/.62)),offset=(left?24:-65)*(1-ease(q)),dx=body+offset,lift=35*Math.sin(Math.PI*q),cut=Math.floor(sw*.54),sx=left?0:cut,width=left?cut:sw-cut,ycut=Math.floor(sh*.255);g.save();g.imageSmoothingEnabled=false;g.globalAlpha=1;
const fx=baseX+(left?sw*.38:sw*.72)*s+dx,fy=baseY+sh*.938*s;g.fillStyle=`rgba(26,27,37,${.2*q})`;g.beginPath();g.ellipse(fx,fy+6,88,13,0,0,Math.PI*2);g.fill();
for(let sy=ycut;sy<sh;sy+=4){const bh=Math.min(4,sh-sy),weight=Math.min(1,(sy-ycut)/(sh*.68));g.drawImage(sprite,sx,sy,width,bh,baseX+sx*s+body+offset*weight,baseY+sy*s-lift*weight,width*s,bh*s+.8);}g.restore();return {dx,lift,q};}
const frames=[];for(let i=0;i<D*FPS;i++){const t=i/FPS;g.setTransform(1,0,0,1,0,0);g.globalAlpha=1;g.clearRect(0,0,W,H);g.drawImage(ref.render(t),0,0,W,H);
// Lighting is stationary spatially; very mild intensity change only, no geometry movement.
g.fillStyle=`rgba(255,232,194,${.008+.006*Math.sin(t*.8)})`;g.fillRect(0,0,W,H);
if(t>=2.83){const a=leg(t,2.83,3.45,true),b=leg(t,3.43,4.05,false),body=-1550*(1-ease((t-2.83)/.62)),settle=t>4.05?Math.sin((t-4.05)*10)*Math.exp(-(t-4.05)*5)*4:0;
// Overlapping upper cloth covers the leg roots. Only the free hem gets a small settle.
g.save();g.imageSmoothingEnabled=false;const hemEnd=Math.floor(sh*.268),band=4;for(let y=0;y<hemEnd;y+=band){const h=Math.min(band,hemEnd-y),weight=(y/hemEnd)**3;g.drawImage(sprite,0,y,sw,h,baseX+body+settle*weight,baseY+y*s,sw*s,h*s+.35);}g.restore();
frames.push({frame:i,t,leftFootOffset:a.dx,rightFootOffset:b.dx,coatOffset:body});}
if([0,160,180,195,207,225,243,270,330,383].includes(i))await fs.writeFile(path.join(__dirname,`central-${i}.png`),c.toBuffer('image/png'));
if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await Promise.race([once(p.stdin,'drain'),done]);}
p.stdin.end();await done;await fs.writeFile(path.join(__dirname,'central-manifest.json'),JSON.stringify({duration:D,fps:FPS,size:[W,H],higgsfieldUsed:false,transition3D:false,integratedWithOpening:false,center:W/2,footPlants:[3.45,4.05],hold:2.35,reflectionAudit:[0,1,2,3,4,5,6].map(t=>ref.audit(t)),motion:'Two opaque lower-leg parts, single overlapping coat hem; no frame blending or motion blur',frames},null,2));console.log(out);})();
