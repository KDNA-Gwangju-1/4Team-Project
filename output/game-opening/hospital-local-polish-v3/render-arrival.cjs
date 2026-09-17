const fs = require('node:fs');
const path = require('node:path');
const Module = require('node:module');
let code = fs.readFileSync(path.resolve(__dirname, '../hospital-pre3d-v3/render-depth.cjs'), 'utf8');
function replaceOnce(a,b) { if (!code.includes(a)) throw Error('Source mismatch: '+a); code=code.replace(a,b); }
replaceOnce('function depth(t,end){return ease((t-2.55)/(end-2.55));}',
  'function depth(t,end){const start=end<3.2?1.25:1.65;return ease((t-start)/(end-start));}');
replaceOnce('plant=left?3.45:4.05,start=plant-.52,q=clamp((t-start)/.52),lift=27*Math.sin(Math.PI*q)',
  'plant=left?3.45:4.05,start=left?1.25:1.65,q=clamp((t-start)/(plant-start)),lift=35*Math.sin(Math.PI*q)');
// Advance both plants by half a second. Foot lift and forward travel share a phase.
code=code.replaceAll('3.45','2.95').replaceAll('4.05','3.55');
replaceOnce("'-c:v','libx264'", "'-af','atrim=start=0.5,asetpts=PTS-STARTPTS,apad','-c:v','libx264'");
replaceOnce("'hospital-depth-before-3d-1080p.mp4'", "'pixel-arrival-smooth.mp4'");
replaceOnce('[0,150,160,170,180,195,207,225,243,270,330,383]', '[90,110,130,150,170,177,190,210,213,240,270,383]');
const derived=new Module(__filename,module);
derived.filename=__filename; derived.paths=module.paths; derived._compile(code,__filename);
