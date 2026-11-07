// 抠色脚本：把纯色背景（默认绿幕 #00FF00，也可 --color magenta/white）转为透明。
// 用法：node .artgen/remove_chroma.mjs <in> <out> [green|magenta|white]
import pngjs from "pngjs";
import { readFileSync, writeFileSync } from "fs";
const { PNG } = pngjs;

const inFile = process.argv[2];
const outFile = process.argv[3];
const mode = process.argv[4] || "green";

const png = PNG.sync.read(readFileSync(inFile));
let removed = 0;
for (let y = 0; y < png.height; y++) {
  for (let x = 0; x < png.width; x++) {
    const i = (png.width * y + x) << 2;
    const r = png.data[i], g = png.data[i + 1], b = png.data[i + 2];
    let hit = false;
    if (mode === "green") {
      hit = g > 60 && g > r * 1.6 && g > b * 1.6;
    } else if (mode === "magenta") {
      hit = r > 90 && b > 90 && g < r * 0.5 && g < b * 0.5;
    } else if (mode === "white") {
      hit = r > 235 && g > 235 && b > 235;
    }
    if (hit) { png.data[i + 3] = 0; removed++; }
  }
}
writeFileSync(outFile, PNG.sync.write(png));
console.log("transparent pixels:", removed, "->", outFile);
