const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const O=__dirname,S=path.join(O,'assets');
const W=1672,H=941,FPS=20/3,DUR=3.45,CAM={x:1045,y:407,f:900,h:1.15},HUMAN=1.70;
const clamp=x=>Math.max(0,Math.min(1,x)),ease=x=>{x=clamp(x);return x*x*(3-2*x)},mix=(a,b,t)=>a+(b-a)*t;
const P=(x,y,z)=>[CAM.x+CAM.f*x/z,CAM.y+CAM.f*(CAM.h-y)/z];
const door={x0:924,x1:1068,y0:250,y1:582},DZ=CAM.f*CAM.h/(582-CAM.y),HX=(924-CAM.x)*DZ/CAM.f,DW=144*DZ/CAM.f,DH=332*DZ/CAM.f;
function extract(img,cols,rows,name){
 const cv=createCanvas(img.width,img.height),g=cv.getContext('2d');g.drawImage(img,0,0);const pixels=g.getImageData(0,0,img.width,img.height).data,frames=[];
 let transparent=0;for(let i=3;i<pixels.length;i+=4)if(pixels[i]<10)transparent++;
 if(transparent/(img.width*img.height)<.2)throw Error(name+': alpha missing');
 for(let n=0;n<cols*rows;n++){
  const sx=n%cols*img.width/cols,sy=Math.floor(n/cols)*img.height/rows,cw=img.width/cols,ch=img.height/rows;
  let l=cw,r=0,top=ch,bottom=0;
  const alpha=(x,y)=>pixels[((sy+y)*img.width+sx+x)*4+3];
  for(let y=0;y<ch;y++)for(let x=0;x<cw;x++)if(alpha(x,y)>180){l=Math.min(l,x);r=Math.max(r,x);top=Math.min(top,y);bottom=Math.max(bottom,y);}
  let sumX=0,ct=0;for(let y=top;y<top+55;y++)for(let x=l;x<=r;x++)if(alpha(x,y)>180){sumX+=x;ct++;}
  const centre=sumX/ct,feet={};
  for(const [side,a,b] of [['left',l,Math.floor(centre)],['right',Math.floor(centre),r]]){
   let fy=0;for(let y=Math.round(top+(bottom-top)*.76);y<=bottom;y++)for(let x=a;x<=b;x++)if(alpha(x,y)>180)fy=Math.max(fy,y);
   let fx=0,nf=0;for(let y=fy-9;y<=fy;y++)for(let x=a;x<=b;x++)if(alpha(x,y)>180){fx+=x;nf++;}
   feet[side]=[fx/nf,fy];
  }
  frames.push({img,sx,sy,cw,ch,top,bottom,centre,feet,box:[l,top,r,bottom],index:n});
 }
 const h=frames.map(f=>f.bottom-f.top).sort((a,b)=>a-b)[Math.floor(frames.length/2)];
 frames.forEach(f=>f.unitHeight=h);fs.writeFileSync(path.join(O,name+'-registration.json'),JSON.stringify(frames.map(({img,...rest})=>rest),null,2));return frames;
}
function travel(u,r=.07){
 u=clamp(u);const d=1-r;
 if(u<r)return u*u/(2*r*d);
 if(u>1-r)return 1-(1-u)*(1-u)/(2*r*d);
 return (u-r/2)/d;
}
function state(t){
 let mode='walk',z=2.45,x=-.56,phase=0,action=0,angle=0,foot=null;
 if(t<5.2){phase=clamp((t-.3)/4.9)*3.5;const progress=travel(clamp((t-.3)/4.9));z=mix(2.45,5.53,progress);x=mix(-.56,-.34,progress);const step=Math.floor(phase*2),side=step%2?'right':'left';foot={side,z:2.45+.22+step*.44,x:mix(-.56,-.34,clamp(step/7))+(side==='left'?-.09:.09)};}
 else if(t<7.4){mode='action';z=5.53;x=-.34;
  action=t<5.55?0:t<5.76?1:t<5.96?2:t<6.43?3:t<6.80?6:7;
  z+=.20*ease((t-6.50)/.9);
 }else if(t<8.8){mode='walk';const u=(t-7.4)/1.4;phase=3.5+u;z=5.73+.80*travel(u);x=-.34;}
 else if(t<9.25){mode='turn';const u=clamp((t-8.8)/.45);action=8+Math.min(2,Math.floor(u*3));z=6.53+.10*ease(u);x=-.34+.19*(-2*u*u*u+3*u*u)+.36*(u*u*u-u*u);}
 else {mode='side';phase=(t-9.25)/1.2;z=6.63;x=-.15+.80*(t-9.25);const step=Math.floor(phase*2);foot={side:Math.floor(phase*8)%4===3?'left':'right',x:-.15+.34+step*.48,z:6.63};}
 angle=t<6.42?.12*clamp((t-6.18)/.24)**2:.12+1.28*ease((t-6.42)/1.1);
 return{t,mode,z,x,phase,action,angle,foot,behind:z>DZ,visible:t<11.4};
}
function doorGeometry(g,angle){
 const q=(u,v)=>P(HX+DW*u*Math.cos(angle),DH*v,DZ+DW*u*Math.sin(angle));
 const polygon=(uv,fill,stroke,width=1)=>{g.beginPath();uv.map(([u,v])=>q(u,v)).forEach((p,i)=>i?g.lineTo(...p):g.moveTo(...p));g.closePath();if(fill){g.fillStyle=fill;g.fill()}if(stroke){g.strokeStyle=stroke;g.lineWidth=width;g.stroke()}};
 polygon([[0,0],[1,0],[1,1],[0,1]],'rgba(155,184,211,.06)',null);
 for(const rect of [[[0,0],[.075,0],[.075,1],[0,1]],[[.925,0],[1,0],[1,1],[.925,1]],[[0,0],[1,0],[1,.075],[0,.075]],[[0,.94],[1,.94],[1,1],[0,1]]])polygon(rect,'#46536c','#68788d',1.2);
 polygon([[.075,.385],[.925,.385],[.925,.405],[.075,.405]],'#a4a9a8',null);
 polygon([[.86,.32],[.905,.32],[.905,.60],[.86,.60]],'#c3c7c8','#444b58',2);
 polygon([[.89,.335],[.899,.335],[.899,.585],[.89,.585]],'#e4e7e4',null);
}
function place(frame,s){
 // One continuous body trajectory, independent of whichever foot/cel is active.
 // Normalize the drawn height so changes in cell margins cannot change stature.
 const scale=CAM.f*HUMAN/s.z/(frame.bottom-frame.top);
 const base=P(s.x,0,s.z);
 return{scale,tx:base[0]-frame.centre*scale,ty:base[1]-frame.bottom*scale,base,anchor:[frame.centre,frame.bottom]};
}

(async()=>{
 const bg=await loadImage(path.join(S,'corridor-clean.png')),opened=await loadImage(path.join(S,'door-opening-background.png'));
 const rawWalk=extract(await loadImage(path.join(O,'assets/walk-guided.png')),6,2,'walk'),walk=[0,1,2,3,4,5,6,8,7,9,10,11].map(i=>rawWalk[i]),action=extract(await loadImage(path.join(O,'assets/action.png')),6,2,'action'),side=extract(await loadImage(path.join(O,'assets/side.png')),4,2,'side');
 // Uniform scaling only. No textured bone deformation or stretched limbs.
 const gcanvas=createCanvas(W,H),g=gcanvas.getContext('2d'),out=createCanvas(1280,720),og=out.getContext('2d');
 const dir=path.join(O,'sidekeys');fs.mkdirSync(dir,{recursive:true});
 const stills=process.argv.includes('--stills'),checks=[.30,.88,1.58,2.28,3.70,5.18,5.76,6.18,6.66,7.40,8.40,8.85,9.20,9.65,10.30];
 const audit=[];
 // Registration sheet: inspect foot anchors before trusting the walk.
 const regs=createCanvas(1536,1024),rg=regs.getContext('2d');rg.fillStyle='#dde2e4';rg.fillRect(0,0,1536,1024);
 walk.forEach((f,i)=>{const xx=i%6*256,yy=Math.floor(i/6)*512;rg.drawImage(f.img,f.sx,f.sy,f.cw,f.ch,xx,yy,256,512);for(const [side,p] of Object.entries(f.feet)){rg.fillStyle=side==='left'?'#00a3a3':'#d73864';rg.beginPath();rg.arc(xx+p[0],yy+p[1],5,0,Math.PI*2);rg.fill();}});
 fs.writeFileSync(path.join(O,'foot-registration.png'),regs.toBuffer('image/png'));
 for(let n=0;n<Math.round(DUR*FPS);n++){
  const t=9.25+n/FPS,s=state(t),ci=checks.findIndex(v=>Math.round(v*FPS)===n);if(stills&&ci<0)continue;
  const fi=Math.floor(s.phase*12+1e-6)%12,f=s.mode==='walk'?walk[fi]:s.mode==='side'?side[Math.floor(s.phase*8+1e-6)%8]:action[s.action],a=place(f,s);
  g.globalAlpha=1;g.setTransform(1,0,0,1,0,0);g.imageSmoothingEnabled=true;g.drawImage(bg,0,0);
  if(s.angle>0){g.globalAlpha=clamp(s.angle/.10);g.drawImage(opened,924,250,144,332,924,250,144,332);g.globalAlpha=1;}
  const actor=()=>{if(!s.visible)return;g.save();if(s.behind){g.beginPath();g.rect(924,250,254,332);g.clip();}
   if(!s.behind){g.fillStyle='rgba(35,38,48,.12)';g.beginPath();g.ellipse(a.base[0]+10,a.base[1]+1,17*5/s.z,3*5/s.z,-.10,0,Math.PI*2);g.fill();}
   g.drawImage(f.img,f.sx,f.sy,f.cw,f.ch,a.tx,a.ty,f.cw*a.scale,f.ch*a.scale);g.restore();};
  if(s.behind){actor();if(s.angle>0)doorGeometry(g,s.angle);
   // Opaque frame, translucent glass: never redraw the opaque outdoor photo over the actor.
   g.drawImage(bg,1063,250,20,332,1063,250,20,332);
   g.fillStyle='rgba(177,203,220,.06)';g.fillRect(1083,274,77,279);
   g.drawImage(bg,1082,551,96,31,1082,551,96,31);
  }else {if(s.angle>0)doorGeometry(g,s.angle);actor();}
  const fade=0;if(fade>0){g.fillStyle=`rgba(0,0,0,${fade})`;g.fillRect(0,0,W,H);}
  og.imageSmoothingEnabled=true;og.drawImage(gcanvas,0,0,1280,720);
  if(ci>=0)fs.writeFileSync(path.join(O,`check-${String(ci).padStart(2,'0')}.png`),out.toBuffer('image/png'));
  if(!stills)fs.writeFileSync(path.join(dir,String(n).padStart(4,'0')+'.png'),out.toBuffer('image/png'));
  audit.push({...s,cel:fi,placement:a});
 }
 fs.writeFileSync(path.join(O,'side-motion.json'),JSON.stringify({camera:CAM,doorDepth:DZ,fps:FPS,duration:DUR,frames:audit},null,2));
 console.log('Rendered side',audit.length);
})();
