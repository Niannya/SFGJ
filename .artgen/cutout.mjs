// 泛洪填充抠图：从图片边缘填充去掉"背景"，保留暖色前景物体（如棕色讲台），
// 并只保留最大连通块（去噪点孤立块）。输入 PNG/JPEG 均可，输出透明 PNG。
// 用法：node .artgen/cutout.mjs <in> <out> [--preview <previewOut>]
import pngjs from "pngjs";
import jpeg from "jpeg-js";
import { readFileSync, writeFileSync } from "fs";
const { PNG } = pngjs;

const arg = (n) => process.argv.indexOf(n) >= 0 ? process.argv[process.argv.indexOf(n) + 1] : undefined;
const inFile = process.argv[2];
const outFile = process.argv[3];
const previewFile = arg("--preview");

const buf = readFileSync(inFile);
let w, h, rgba;
if (buf[0] === 0x89 && buf[1] === 0x50) {
  const png = PNG.sync.read(buf);
  w = png.width; h = png.height; rgba = png.data;
} else if (buf[0] === 0xff && buf[1] === 0xd8) {
  const raw = jpeg.decode(buf, { useTArray: true });
  w = raw.width; h = raw.height;
  rgba = new Uint8Array(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    rgba[i * 4] = raw.data[i * 4];
    rgba[i * 4 + 1] = raw.data[i * 4 + 1];
    rgba[i * 4 + 2] = raw.data[i * 4 + 2];
    rgba[i * 4 + 3] = 255;
  }
} else { throw new Error("unsupported format"); }

const idx = (x, y) => (y * w + x) * 4;
const isWarm = (x, y) => {
  const i = idx(x, y);
  const r = rgba[i], g = rgba[i + 1], b = rgba[i + 2];
  // 暖色：红色通道最大且带一定色度
  return r >= g && r >= b && (r - Math.min(g, b)) >= 8;
};

// 1) 从四边泛洪填充，标记"背景"（非暖色）为透明
const bg = new Uint8Array(w * h);
const stack = [];
for (let x = 0; x < w; x++) { stack.push(x, 0); stack.push(x, h - 1); }
for (let y = 0; y < h; y++) { stack.push(0, y); stack.push(w - 1, y); }
while (stack.length) {
  const y = stack.pop(), x = stack.pop();
  if (x < 0 || y < 0 || x >= w || y >= h) continue;
  const p = y * w + x;
  if (bg[p]) continue;
  if (isWarm(x, y)) continue; // 前景，不进入
  bg[p] = 1;
  stack.push(x + 1, y, x - 1, y, x, y + 1, x, y - 1);
}
let removed = 0;
for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
  if (bg[y * w + x]) { rgba[idx(x, y) + 3] = 0; removed++; }
}

// 2) 保留最大连通块
const visited = new Uint8Array(w * h);
let bestComp = [], bestCount = 0;
for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
  const p = y * w + x;
  if (visited[p] || rgba[idx(x, y) + 3] === 0) continue;
  const comp = [], st = [x, y];
  visited[p] = 1;
  while (st.length) {
    const cy = st.pop(), cx = st.pop();
    comp.push(cx, cy);
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = cx + dx, ny = cy + dy;
      if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
      const np = ny * w + nx;
      if (visited[np] || rgba[idx(nx, ny) + 3] === 0) continue;
      visited[np] = 1;
      st.push(nx, ny);
    }
  }
  const count = comp.length / 2;
  if (count > bestCount) { bestCount = count; bestComp = comp; }
}
if (bestCount > 0) {
  const keep = new Uint8Array(w * h);
  for (let i = 0; i < bestComp.length; i += 2) keep[bestComp[i + 1] * w + bestComp[i]] = 1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (!keep[y * w + x]) rgba[idx(x, y) + 3] = 0;
  }
}

const out = new PNG({ width: w, height: h });
out.data = rgba;
writeFileSync(outFile, PNG.sync.write(out));
console.log(`cutout done ${w}x${h}, bg removed=${removed}, kept component=${bestCount}px -> ${outFile}`);

if (previewFile) {
  // 在白底上合成预览，便于肉眼/视觉校验
  const pv = new PNG({ width: w, height: h });
  for (let i = 0; i < w * h * 4; i += 4) {
    const a = rgba[i + 3] / 255;
    pv.data[i] = Math.round(rgba[i] * a + 255 * (1 - a));
    pv.data[i + 1] = Math.round(rgba[i + 1] * a + 255 * (1 - a));
    pv.data[i + 2] = Math.round(rgba[i + 2] * a + 255 * (1 - a));
    pv.data[i + 3] = 255;
  }
  writeFileSync(previewFile, PNG.sync.write(pv));
  console.log("preview ->", previewFile);
}
