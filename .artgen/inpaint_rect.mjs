// 矩形内修复：把一块矩形区域用其边缘外侧的干净纹理镜像覆盖，
// 用于抹除前景物体（花坛/杂物）。用法：
//   node inpaint_rect.mjs <in> <out> <x0> <y0> <x1> <y1> left|right
// left : 以矩形左边缘为轴，向左侧干净区取镜像；right : 以右边缘为轴向右取。
import { readFileSync, writeFileSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const [inF, outF] = [process.argv[2], process.argv[3]];
const x0 = Number(process.argv[4]);
const y0 = Number(process.argv[5]);
const x1 = Number(process.argv[6]);
const y1 = Number(process.argv[7]);
const mode = process.argv[8] || "left";

const png = PNG.sync.read(readFileSync(inF));
const { width: w, height: h, data } = png;
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));

for (let y = y0; y <= y1 && y < h; y++) {
  for (let x = x0; x <= x1 && x < w; x++) {
    let sx;
    if (mode === "right") sx = clamp(2 * (x1 + 1) - 1 - x, 0, w - 1);
    else sx = clamp(2 * x0 - 1 - x, 0, w - 1); // left
    const di = (y * w + x) * 4;
    const si = (y * w + sx) * 4;
    data[di] = data[si];
    data[di + 1] = data[si + 1];
    data[di + 2] = data[si + 2];
    data[di + 3] = data[si + 3];
  }
}
writeFileSync(outF, PNG.sync.write(png));
console.log(`inpaint_rect [${x0},${y0}]-[${x1},${y1}] ${mode} -> ${outF}`);
