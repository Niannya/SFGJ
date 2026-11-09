// 收尾工具：
//   crop : 透明 PNG 裁剪到不透明包围盒（--margin 默认 0）
//   jpg2png : JPEG 解码后重编码为 PNG（修正误存成 .png 的 JPEG 数据）
// 用法：node .artgen/finalize.mjs crop <in> <out> [--margin N]
//       node .artgen/finalize.mjs jpg2png <in> <out>
import pngjs from "pngjs";
import jpeg from "jpeg-js";
import { readFileSync, writeFileSync } from "fs";
const { PNG } = pngjs;

function load(buf) {
  if (buf[0] === 0x89 && buf[1] === 0x50) return PNG.sync.read(buf);
  if (buf[0] === 0xff && buf[1] === 0xd8) {
    const raw = jpeg.decode(buf, { useTArray: true });
    const png = new PNG({ width: raw.width, height: raw.height });
    for (let i = 0; i < raw.width * raw.height; i++) {
      png.data[i * 4] = raw.data[i * 4];
      png.data[i * 4 + 1] = raw.data[i * 4 + 1];
      png.data[i * 4 + 2] = raw.data[i * 4 + 2];
      png.data[i * 4 + 3] = 255;
    }
    return png;
  }
  throw new Error("unsupported format");
}

const cmd = process.argv[2], inFile = process.argv[3], outFile = process.argv[4];
const marginArg = process.argv.indexOf("--margin");
const margin = marginArg >= 0 ? Number(process.argv[marginArg + 1]) : 0;

const png = load(readFileSync(inFile));

if (cmd === "crop") {
  let minX = png.width, minY = png.height, maxX = -1, maxY = -1;
  for (let y = 0; y < png.height; y++) for (let x = 0; x < png.width; x++) {
    if (png.data[(y * png.width + x) * 4 + 3] > 10) {
      if (x < minX) minX = x; if (x > maxX) maxX = x;
      if (y < minY) minY = y; if (y > maxY) maxY = y;
    }
  }
  if (maxX < 0) { console.error("no opaque content"); process.exit(1); }
  minX = Math.max(0, minX - margin); minY = Math.max(0, minY - margin);
  maxX = Math.min(png.width - 1, maxX + margin); maxY = Math.min(png.height - 1, maxY + margin);
  const w = maxX - minX + 1, h = maxY - minY + 1;
  const out = new PNG({ width: w, height: h });
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const si = ((minY + y) * png.width + (minX + x)) * 4;
    const di = (y * w + x) * 4;
    out.data[di] = png.data[si]; out.data[di + 1] = png.data[si + 1];
    out.data[di + 2] = png.data[si + 2]; out.data[di + 3] = png.data[si + 3];
  }
  writeFileSync(outFile, PNG.sync.write(out));
  console.log(`crop ${png.width}x${png.height} -> ${w}x${h} -> ${outFile}`);
} else if (cmd === "jpg2png") {
  writeFileSync(outFile, PNG.sync.write(png));
  console.log(`jpg2png ${png.width}x${png.height} -> ${outFile}`);
} else if (cmd === "resize") {
  // resize <in> <out> <W>x<H>   （最近邻，适配像素风）
  const dims = (process.argv[5] || "").split("x").map(Number);
  const tw = dims[0], th = dims[1];
  if (!tw || !th) { console.error("resize 需要 <W>x<H>"); process.exit(1); }
  const out = new PNG({ width: tw, height: th });
  for (let y = 0; y < th; y++) for (let x = 0; x < tw; x++) {
    const sx = Math.min(png.width - 1, Math.floor(x * png.width / tw));
    const sy = Math.min(png.height - 1, Math.floor(y * png.height / th));
    const si = (sy * png.width + sx) * 4;
    const di = (y * tw + x) * 4;
    out.data[di] = png.data[si]; out.data[di + 1] = png.data[si + 1];
    out.data[di + 2] = png.data[si + 2]; out.data[di + 3] = png.data[si + 3];
  }
  writeFileSync(outFile, PNG.sync.write(out));
  console.log(`resize ${png.width}x${png.height} -> ${tw}x${th} -> ${outFile}`);
} else {
  console.error("用法: node finalize.mjs <crop|jpg2png|resize> <in> <out> [--margin N | <W>x<H>]");
  process.exit(1);
}
