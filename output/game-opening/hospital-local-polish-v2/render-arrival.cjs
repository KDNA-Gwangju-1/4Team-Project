// Reuse the original authored renderer without changing its source or assets.
const fs = require('node:fs');
const path = require('node:path');
const Module = require('node:module');
const original = path.resolve(__dirname, '../hospital-pre3d-v3/render-depth.cjs');
let code = fs.readFileSync(original, 'utf8');
const needle = 'function depth(t,end){return ease((t-2.55)/(end-2.55));}';
if (!code.includes(needle)) throw new Error('Original renderer changed; inspect before patching.');
// Extend approach acceleration: left leg 2.20s versus 0.90s; right 2.80s versus 1.50s.
// World centerline, final foot positions and footfall timing remain unchanged.
code = code.replace(needle, 'function depth(t,end){return ease((t-1.25)/(end-1.25));}');
code = code.replace("'hospital-depth-before-3d-1080p.mp4'", "'pixel-arrival-smooth.mp4'");
const derived = new Module(__filename, module);
derived.filename = __filename;
derived.paths = module.paths;
derived._compile(code, __filename);
