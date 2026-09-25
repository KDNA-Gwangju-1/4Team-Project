const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
(async()=>{
 const dir=path.join(__dirname,'decoded'),files=fs.readdirSync(dir).filter(f=>f.endsWith('.png')).sort();
 const count=24,w=320,h=438;
 for(let p=0;p<Math.ceil(files.length/count);p++){
  const c=createCanvas(w*6,(h+24)*4),g=c.getContext('2d');g.fillStyle='#16191d';g.fillRect(0,0,c.width,c.height);g.font='16px sans-serif';
  for(let j=0;j<count&&p*count+j<files.length;j++){
   const i=p*count+j,im=await loadImage(path.join(dir,files[i])),x=j%6*w,y=Math.floor(j/6)*(h+24);
   g.drawImage(im,550,130,380,520,x,y,w,h);g.fillStyle='#fff';g.fillText(`${((i+.5)/12).toFixed(3)}s`,x+6,y+h+18);
  }
  fs.writeFileSync(path.join(__dirname,`review-${p+1}.jpg`),c.toBuffer('image/jpeg',93));
 }
 console.log(JSON.stringify({decodedFrames:files.length,pages:Math.ceil(files.length/count)}));
})();
