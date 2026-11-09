// 分析图片边缘/角落像素颜色分布，辅助抠图阈值判断。
// 用法：node .artgen/analyze_edges.mjs <in>
import pngjs from "pngjs";
import { readFileSync } from "fs";
const { PNG } = pngjs;
const f = process.argv[2];
const png = PNG.sync.read(readFileSync(f));
const { width: w, height: h, data } = png;
const at = (x, y) => {
  const i = (w * y + x) << 2;
  return [data[i], data[i + 1], data[i + 2], data[i + 3]];
};
console.log("size:", w, "x", h);
console.log("corners:");
for (const [x, y] of [[0, 0], [w - 1, 0], [0, h - 1], [w - 1, h - 1]]) {
  console.log(`  (${x},${y}) =`, at(x, y));
}
// 统计边缘一圈的颜色聚类（量化到 16 档）
const cnt = new Map();
const scan = (x, y) => {
  const [r, g, b, a] = at(x, y);
  if (a < 10) return;
  const key = `${r >> 5},${g >> 5},${b >> 5}`;
  cnt.set(key, (cnt.get(key) || 0) + 1);
};
for (let x = 0; x < w; x++) { scan(x, 0); scan(x, h - 1); }
for (let y = 0; y < h; y++) { scan(0, y); scan(w - 1, y); }
const sorted = [...cnt.entries()].sort((a, b) => b[1] - a[1]).slice(0, 12);
console.log("edge color clusters (r>>5,g>>5,b>>5 => count):");
for (const [k, c] of sorted) console.log("  ", k, "=>", c);
