const fs=require('node:fs/promises'),path=require('node:path'),{spawn}=require('node:child_process'),{once}=require('node:events');
const {createCanvas,loadImage,GlobalFonts}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const OUT=__dirname,FF=path.resolve(OUT,'../render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const names=['head','torso','left-arm','right-arm','left-leg','right-leg'],parts={},C=(w,h)=>createCanvas(w,h);
const W=1920,H=1080,FPS=60,D=6,TAU=Math.PI*2;
function segment(g,im,sy,sh,a,b,width){const dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy);g.save();g.translate(...a);g.rotate(-Math.atan2(dx,dy));g.drawImage(im,0,sy,im.width,sh,-width/2,-2,width,len+4);g.restore();}
const gaitAudit=[];
function pose(t){const p=TAU*t/1.2,bob=2*Math.cos(2*p),sway=2*Math.sin(p),j={neck:[sway,141+bob],hips:[sway,355+bob]};
 // 3D joint coordinates are calculation only, rendered using separate 2D parts.
 // Stance 60%, swing 40%. Fixed femur/tibia lengths, solved analytically.
 const project=v=>[v[0],v[1]-.22*v[2]],length=(a,b)=>Math.hypot(...a.map((v,k)=>v-b[k]));
 for(const [side,sign,offset] of [['left',-1,0],['right',1,.5]]){
  const u=(t/1.2+offset)%1,stance=u<.6,sw=Math.max(0,(u-.6)/.4),q=sw*sw*(3-2*sw);
  const z=stance?48-96*u/.6:-48+96*q,lift=stance?0:34*Math.sin(Math.PI*sw),hy=355+bob,ay=588-lift,d=ay-hy,dist=Math.hypot(z,d),L=125;
  if(dist>=2*L)throw Error('Leg target out of reach');
  const bend=Math.sqrt(L*L-dist*dist/4),kz=z/2+bend*d/dist,ky=hy+d/2-bend*z/dist;
  const hip=[sign*37+sway,hy,0],knee=[sign*37+sway,ky,kz],ankle=[sign*37+sway,ay,z],foot=[sign*37+sway,610-lift,z];
  j[side+'Hip']=project(hip);j[side+'Knee']=project(knee);j[side+'Ankle']=project(ankle);j[side+'Foot']=project(foot);
  const theta=-.28*Math.cos(TAU*u),elbowFlex=.12+.10*Math.max(0,Math.sin(TAU*u));
  const shoulder=[sign*83+sway,176+bob,0],elbow=[sign*96+sway,176+bob+105*Math.cos(theta),105*Math.sin(theta)],wrist=[sign*104+sway,elbow[1]+100*Math.cos(theta+elbowFlex),elbow[2]+100*Math.sin(theta+elbowFlex)];
  j[side+'Shoulder']=project(shoulder);j[side+'Elbow']=project(elbow);j[side+'Wrist']=project(wrist);
  const error=Math.max(Math.abs(length(hip,knee)-L),Math.abs(length(knee,ankle)-L));if(error>1e-6)throw Error('Bone length changed');
  gaitAudit.push({t,side,stance,worldFootZ:133.3333333333333*t+z,footLift:lift,boneLengthError:error});
 }return j;
}
function drawRig(g,t,x,y,scale,debug=false,override=null){const j=override||pose(t);g.save();g.translate(x,y);g.scale(scale,scale);g.globalAlpha=1;g.imageSmoothingEnabled=false;
 // Each limb has its own rigid upper/lower pieces. The body image is never warped.
 for(const side of ['left','right']){const im=parts[side+'-leg'];segment(g,im,0,im.height*.49,j[side+'Hip'],j[side+'Knee'],72);segment(g,im,im.height*.47,im.height*.39,j[side+'Knee'],j[side+'Ankle'],66);segment(g,im,im.height*.84,im.height*.16,j[side+'Ankle'],j[side+'Foot'],74);}
 // Arms attach behind the coat's shoulder caps, masking cut boundaries.
 for(const side of ['left','right']){const im=parts[side+'-arm'];segment(g,im,0,im.height*.51,j[side+'Shoulder'],j[side+'Elbow'],70);segment(g,im,im.height*.49,im.height*.51,j[side+'Elbow'],j[side+'Wrist'],64);}
 g.drawImage(parts.torso,j.hips[0]-109,j.neck[1]-12,218,310);
 g.drawImage(parts.head,j.neck[0]-84,j.neck[1]-126,168,135);
 if(debug){g.strokeStyle='#56d6ff';g.fillStyle='#fff';g.lineWidth=2;for(const side of ['left','right'])for(const chain of [['Shoulder','Elbow','Wrist'],['Hip','Knee','Ankle','Foot']]){g.beginPath();chain.forEach((name,i)=>{const v=j[side+name];i?g.lineTo(...v):g.moveTo(...v);});g.stroke();}for(const v of Object.values(j)){g.beginPath();g.arc(...v,3,0,TAU);g.fill();}}
 g.restore();return j;
}
module.exports={pose,drawRig,init:async()=>{for(const name of names)parts[name]=await loadImage(path.resolve(OUT,'../detective-part-rig-v1',name+'.png'));}};
if(require.main===module)(async()=>{GlobalFonts.registerFromPath('C:/Windows/Fonts/malgun.ttf','Korean');const atlas=await loadImage(path.join(OUT,'parts-atlas.png')),ac=C(atlas.width,atlas.height),ag=ac.getContext('2d');ag.drawImage(atlas,0,0);const px=ag.getImageData(0,0,atlas.width,atlas.height).data,meta={};
 for(let n=0;n<6;n++){const x0=Math.round((n%3)*atlas.width/3),x1=Math.round((n%3+1)*atlas.width/3),y0=Math.round(Math.floor(n/3)*atlas.height/2),y1=Math.round((Math.floor(n/3)+1)*atlas.height/2);let bx=x1,by=y1,ex=x0,ey=y0,count=0;
 for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++)if(px[(y*atlas.width+x)*4+3]>128){bx=Math.min(bx,x);by=Math.min(by,y);ex=Math.max(ex,x);ey=Math.max(ey,y);count++;}
 if(!count)throw Error('Missing part '+names[n]);const c=C(ex-bx+1,ey-by+1);c.getContext('2d').drawImage(atlas,bx,by,c.width,c.height,0,0,c.width,c.height);parts[names[n]]=c;await fs.writeFile(path.join(OUT,names[n]+'.png'),c.toBuffer('image/png'));meta[names[n]]={crop:[bx,by,c.width,c.height],pixels:count};}
 await fs.writeFile(path.join(OUT,'parts.json'),JSON.stringify(meta,null,2));
 const board=C(W,H),bg=board.getContext('2d');bg.fillStyle='#343b46';bg.fillRect(0,0,W,H);const labels=['머리','몸통','왼팔','오른팔','왼다리','오른다리'];for(let n=0;n<6;n++){const im=parts[names[n]],s=Math.min(250/im.width,360/im.height),x=(n%3)*400+200,y=Math.floor(n/3)*510+75;bg.drawImage(im,x-im.width*s/2,y,im.width*s,im.height*s);bg.font='28px Korean';bg.textAlign='center';bg.fillStyle='#fff';bg.fillText(labels[n],x,y+400);}drawRig(bg,0,1540,160,1.2,true);await fs.writeFile(path.join(OUT,'parts-and-rig.png'),board.toBuffer('image/png'));
 const c=C(W,H),g=c.getContext('2d');const p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s','1920x1080','-r','60','-i','pipe:0','-an','-c:v','libx264','-crf','17','-preset','fast','-pix_fmt','yuv420p','-movflags','+faststart',path.join(OUT,'six-part-walk-review.mp4')],{windowsHide:true});let err='';p.stderr.on('data',b=>err+=b);const done=new Promise((r,j)=>{p.on('error',j);p.on('close',n=>n?j(Error(err)):r());});done.catch(()=>{});const audit=[];
 for(let i=0;i<D*FPS;i++){g.setTransform(1,0,0,1,0,0);g.globalAlpha=1;g.clearRect(0,0,W,H);g.fillStyle='#e0e3e5';g.fillRect(0,0,W,H);g.fillStyle='#c2c8cb';g.fillRect(0,965,W,115);g.font='32px Korean';g.textAlign='center';g.fillStyle='#263545';g.fillText('6개 부위 분리 · 보행 검수',620,75);g.fillText('관절 연결 확인',1470,75);const j=drawRig(g,i/FPS,620,145,1.32);drawRig(g,i/FPS,1470,145,1.32,true);audit.push({frame:i,joints:j});if([0,9,18,27,36,45,54,63].includes(i))await fs.writeFile(path.join(OUT,`pose-${i}.png`),c.toBuffer('image/png'));if(!p.stdin.write(Buffer.from(g.getImageData(0,0,W,H).data)))await once(p.stdin,'drain');}p.stdin.end();await done;await fs.writeFile(path.join(OUT,'joint-audit.json'),JSON.stringify({frames:audit.length,method:'Six separate parts, fixed-length leg IK in sagittal plane; no whole-body mesh; no temporal blending',maxBoneLengthError:Math.max(...gaitAudit.map(a=>a.boneLengthError)),stanceFraction:.6,swingFraction:.4,samples:audit,gait:gaitAudit},null,2));console.log('Rendered six-part rig review');})();
