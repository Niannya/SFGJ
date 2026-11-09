// 对称镜像修复：把画面右下角一块区域用其水平镜像（左下角）覆盖，
// 用于抹除角落水印/污渍。用法：
//   node inpaint_mirror.mjs <in> [out] [x0] [y0]
// 默认修复 x>=2450, y>=1050（约整幅宽高 2688x1152 时右下角）
import { readFileSync, writeFileSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const f = process.argv[2];
const out = process.argv[3] || f;
const x0 = Number(process.argv[4] ?? 2450);
const y0 = Number(process.argv[5] ?? 1050);

const png = PNG.sync.read(readFileSync(f));
const { width: w, height: h, data } = png;

for (let y = y0; y < h; y++) {
  for (let x = x0; x < w; x++) {
    const sx = w - 1 - x; // 水平镜像源
    const di = (y * w + x) * 4;
    const si = (y * w + sx) * 4;
    data[di] = data[si];
    data[di + 1] = data[si + 1];
    data[di + 2] = data[si + 2];
    data[di + 3] = data[si + 3];
  }
}
writeFileSync(out, PNG.sync.write(png));
console.log(`mirror-inpaint x>=${x0}, y>=${y0} (${w}x${h}) -> ${out}`);
