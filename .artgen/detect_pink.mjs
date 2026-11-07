// 检测图片中"粉色花朵"像素的包围盒（花坛定位用）。
// 用法：node detect_pink.mjs <in> [--threshold rMin] [--split cx]
// 输出：粉色像素总数、整体 bbox、按 x 中线左右两个 bbox。
import { readFileSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const f = process.argv[2];
const png = PNG.sync.read(readFileSync(f));
const { width: w, height: h, data } = png;

const isPink = (r, g, b, a, y) =>
  a > 200 && y > 600 && r > 150 && b > 90 && b > g && r > b + 20;

let minX = w, minY = h, maxX = -1, maxY = -1, count = 0;
for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
  const i = (y * w + x) * 4;
  if (isPink(data[i], data[i + 1], data[i + 2], data[i + 3], y)) {
    count++;
    if (x < minX) minX = x; if (x > maxX) maxX = x;
    if (y < minY) minY = y; if (y > maxY) maxY = y;
  }
}
console.log(`size ${w}x${h}, pink px=${count}`);
if (count === 0) process.exit(0);
console.log(`all bbox: x[${minX},${maxX}] y[${minY},${maxY}]`);

// 左右两个簇：按 x 中线切
const cx = Math.floor(w / 2);
let l = { minX: w, maxX: -1, minY: h, maxY: -1, n: 0 };
let r = { minX: w, maxX: -1, minY: h, maxY: -1, n: 0 };
for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
  const i = (y * w + x) * 4;
  const rr = data[i], gg = data[i + 1], bb = data[i + 2], aa = data[i + 3];
  if (!isPink(rr, gg, bb, aa, y)) continue;
  const c = x < cx ? l : r;
  c.n++;
  if (x < c.minX) c.minX = x; if (x > c.maxX) c.maxX = x;
  if (y < c.minY) c.minY = y; if (y > c.maxY) c.maxY = y;
}
console.log(`left  bbox: x[${l.minX},${l.maxX}] y[${l.minY},${l.maxY}] n=${l.n}`);
console.log(`right bbox: x[${r.minX},${r.maxX}] y[${r.minY},${r.maxY}] n=${r.n}`);
