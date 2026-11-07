// 矩形裁剪工具：crop_rect <in> <out> <x0> <y0> <w> <h>
// x0/y0 为左上角，w/h 为保留区域宽高；缺省 x0=0,y0=0,w=全宽,h=指定。
import { readFileSync, writeFileSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const [inF, outF] = [process.argv[2], process.argv[3]];
const png = PNG.sync.read(readFileSync(inF));
const x0 = Number(process.argv[4] ?? 0);
const y0 = Number(process.argv[5] ?? 0);
const w = Number(process.argv[6] ?? png.width - x0);
const h = Number(process.argv[7] ?? png.height - y0);

const out = new PNG({ width: w, height: h });
for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const si = ((y0 + y) * png.width + (x0 + x)) * 4;
    const di = (y * w + x) * 4;
    out.data[di] = png.data[si];
    out.data[di + 1] = png.data[si + 1];
    out.data[di + 2] = png.data[si + 2];
    out.data[di + 3] = png.data[si + 3];
  }
}
writeFileSync(outF, PNG.sync.write(out));
console.log(`crop_rect ${png.width}x${png.height} -> ${w}x${h} @(${x0},${y0}) -> ${outF}`);
