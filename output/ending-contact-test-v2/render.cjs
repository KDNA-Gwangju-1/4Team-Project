// Original-painting texture, connected deformation. Short contact study, not final gait.
const fs=require('fs'),path=require('path'),{spawn}=require('child_process'),{once}=require('events');
const {createCanvas,loadImage}=require('C:/Users/Note-038/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const FF='C:/Ondukong/_backup_4Team_20260916/output/game-opening/render-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe',O=__dirname;
const clamp=t=>Math.max(0,Math.min(1,t)),ease=t=>{t=clamp(t);return t*t*(3-2*t)},mix=(a,b,t)=>a+(b-a)*t;
// Measured outline in original 1672 x 941 scene coordinates. Only used as animation matte.
const outline=[[766,200],[792,198],[867,219],[877,243],[894,252],[889,264],[859,270],[860,290],[846,301],[877,317],[893,339],[914,448],[927,500],[930,531],[927,551],[917,569],[900,571],[894,548],[902,659],[872,663],[862,765],[874,780],[871,795],[858,808],[833,815],[820,810],[820,782],[819,663],[805,660],[794,710],[785,774],[784,798],[790,821],[790,850],[748,853],[743,842],[746,817],[740,790],[738,770],[748,699],[754,660],[731,664],[709,657],[678,643],[691,605],[724,511],[733,474],[708,467],[698,444],[697,416],[708,371],[719,340],[745,325],[771,312],[785,295],[788,282],[780,270],[777,255],[765,250],[762,244],[791,239],[778,219]];
function tri(g,img,s,d){
 const [a,b,c]=s,[p,q,r]=d,det=(b[0]-a[0])*(c[1]-a[1])-(c[0]-a[0])*(b[1]-a[1]);
 const A=((q[0]-p[0])*(c[1]-a[1])-(r[0]-p[0])*(b[1]-a[1]))/det,B=((q[1]-p[1])*(c[1]-a[1])-(r[1]-p[1])*(b[1]-a[1]))/det;
 const C=((r[0]-p[0])*(b[0]-a[0])-(q[0]-p[0])*(c[0]-a[0]))/det,D=((r[1]-p[1])*(b[0]-a[0])-(q[1]-p[1])*(c[0]-a[0]))/det;
 const sx=Math.min(...s.map(v=>v[0])),sy=Math.min(...s.map(v=>v[1]));
 const cx=d.reduce((v,p)=>v+p[0],0)/3,cy=d.reduce((v,p)=>v+p[1],0)/3;
 const expanded=d.map(v=>{const n=Math.hypot(v[0]-cx,v[1]-cy);return [v[0]+.8*(v[0]-cx)/n,v[1]+.8*(v[1]-cy)/n]});
 g.save();g.beginPath();expanded.forEach((v,i)=>i?g.lineTo(...v):g.moveTo(...v));g.closePath();g.clip();g.setTransform(A,B,C,D,p[0]-A*a[0]-C*a[1],p[1]-B*a[0]-D*a[1]);g.drawImage(img,sx-1,sy-1,14,14,sx-1,sy-1,14,14);g.restore();
}
(async()=>{
 console.log('Loading source artwork');
 const src=await loadImage(path.resolve('output/ending-v1/pixel/corridor-exit-keyframe-v1.png')),bg=await loadImage(path.resolve('output/ending-walk-local-v1/corridor-clean.png'));
 console.log('Loaded',src.width,bg.width);
 const tex=createCanvas(1672,941),tg=tex.getContext('2d');tg.beginPath();outline.forEach((p,i)=>i?tg.lineTo(...p):tg.moveTo(...p));tg.closePath();tg.clip();tg.drawImage(src,0,0,1672,941);
 const texture=await loadImage(tex.toBuffer('image/png'));
 const stage=createCanvas(1672,941),g=stage.getContext('2d'),out=createCanvas(1920,1080),og=out.getContext('2d');
 const p=spawn(FF,['-v','error','-y','-f','rawvideo','-pix_fmt','rgba','-s','1920x1080','-r','30','-i','pipe:0','-an','-c:v','libx264','-crf','18','-preset','fast','-pix_fmt','yuv420p','-movflags','+faststart',path.join(O,'two-step-contact-study.mp4')],{windowsHide:true});
 let error='';p.stderr.on('data',d=>error+=d);p.stdin.on('error',()=>{});const done=new Promise((r,j)=>p.on('close',c=>c?j(Error(error)):r()));done.catch(()=>{});
 const logs=[],sheet=createCanvas(1440,810),sh=sheet.getContext('2d');
 for(let f=0;f<90;f++){
  if(f%10===0)console.log('Frame',f);
  const t=Math.min(2.4,Math.max(0,f/30-.3)),u=clamp(t/1.2),v=clamp((t-1.2)/1.2);
  const left=[42*ease(u),-38*ease(u)-18*Math.sin(Math.PI*u)],right=[42*ease(v),-38*ease(v)-18*Math.sin(Math.PI*v)];
  const root=[21*(ease(u)+ease(v)),-19*(ease(u)+ease(v))-2*Math.sin(Math.PI*(u+v))];
  function warp(x,y){
   const foot=x<809?left:right,blend=ease((y-642)/150);
   let dx=mix(root[0],foot[0],blend),dy=mix(root[1],foot[1],blend);
   if(x>889&&y>355&&y<575)dx+=Math.sin((u+v)*Math.PI)*3*ease((y-355)/150);
   return [x+dx,y+dy];
  }
  g.setTransform(1,0,0,1,0,0);g.clearRect(0,0,1672,941);g.imageSmoothingEnabled=false;g.drawImage(bg,0,0,1672,941);
  for(const [x,y,delta,planted] of [[766,845,left,t>=1.2],[849,804,right,t<=1.2]]){g.fillStyle=planted?'rgba(43,44,48,.22)':'rgba(43,44,48,.10)';g.beginPath();g.ellipse(x+delta[0],y+delta[1],17,4,0,0,Math.PI*2);g.fill();}
  // Shared grid vertices keep the drawing connected, no detached sprite limbs.
  for(let y=192;y<864;y+=12)for(let x=672;x<948;x+=12){const a=[x,y],b=[x+12,y],c=[x,y+12],d=[x+12,y+12];tri(g,texture,[a,b,c],[warp(...a),warp(...b),warp(...c)]);tri(g,texture,[b,d,c],[warp(...b),warp(...d),warp(...c)]);}
  og.imageSmoothingEnabled=false;og.drawImage(stage,0,0,1920,1080);
  logs.push({f,leftAnchor:warp(766,845),rightAnchor:warp(849,804),phase:t<1.2?'right_stance':'left_stance'});
  if(f%10===0){sh.drawImage(out,f/10%3*480,Math.floor(f/30)*270,480,270);fs.writeFileSync(path.join(O,`frame-${f}.png`),out.toBuffer('image/png'))}
  if(!p.stdin.write(Buffer.from(og.getImageData(0,0,1920,1080).data)))await Promise.race([once(p.stdin,'drain'),done]);
 }
 p.stdin.end();await done;fs.writeFileSync(path.join(O,'contact-sheet.png'),sheet.toBuffer('image/png'));fs.writeFileSync(path.join(O,'contacts.json'),JSON.stringify(logs));
 const err=Math.max(...logs.filter((_,i)=>i<=45).map(r=>Math.hypot(r.rightAnchor[0]-849,r.rightAnchor[1]-804)),...logs.filter((_,i)=>i>=45).map(r=>Math.hypot(r.leftAnchor[0]-808,r.leftAnchor[1]-807)));
 fs.writeFileSync(path.join(O,'validation.json'),JSON.stringify({frames:90,fps:30,duration:3,maxControlAnchorSlipPixels:err,limitations:'Control anchors only; not a claim of natural anatomical gait. Single-pose mesh cannot redraw soles or reveal hidden limb surfaces.'},null,2));console.log('anchor slip',err);
})().catch(e=>{console.error(e);process.exitCode=1;});
