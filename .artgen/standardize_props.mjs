// 道具标准化：按占地尺寸统一缩放(最近邻,保持像素风) + 更新 .meta 底部对齐枢轴。
// 用法：node .artgen/standardize_props.mjs  （默认处理全部，写回 Assets/PNG/props/，先备份到 .artgen/backup_props/）
import { readFileSync, writeFileSync, mkdirSync, existsSync, copyFileSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const DIR = "Assets/PNG/props";
const BACKUP = ".artgen/backup_props";

// 占地分类：w = 目标宽度(px)，footprint 用于记录
// 1 格 = 100px；2 格宽 = 200px
const PROPS = [
  { name: "flowerpot", w: 100, footprint: "1x1" },
  { name: "chair", w: 100, footprint: "1x1" },
  { name: "chair_bedroom", w: 100, footprint: "1x1" },
  { name: "lamp", w: 100, footprint: "1x1" },
  { name: "trashbin", w: 100, footprint: "1x1" },
  { name: "table", w: 100, footprint: "1x1" },
  { name: "desk", w: 100, footprint: "1x1" },
  { name: "water_dispenser", w: 100, footprint: "1x1" },
  { name: "desk_fallen", w: 100, footprint: "1x1" },
  { name: "bed", w: 200, footprint: "2x1" },
  { name: "desk_office_h", w: 200, footprint: "2x1" },
  { name: "bookshelf", w: 100, footprint: "1x2" },
  { name: "wardrobe", w: 100, footprint: "1x2" },
  { name: "desk_office_v", w: 100, footprint: "1x2" },
  { name: "blackboard", w: 200, footprint: "wall-2" },
  { name: "window", w: 100, footprint: "wall-1" },
];

function resizeNN(src, tw) {
  const th = Math.max(1, Math.round(src.height * tw / src.width));
  const out = new PNG({ width: tw, height: th });
  for (let y = 0; y < th; y++) {
    const sy = Math.min(src.height - 1, Math.floor(y * src.height / th));
    for (let x = 0; x < tw; x++) {
      const sx = Math.min(src.width - 1, Math.floor(x * src.width / tw));
      const si = (sy * src.width + sx) * 4;
      const di = (y * tw + x) * 4;
      out.data[di] = src.data[si];
      out.data[di + 1] = src.data[si + 1];
      out.data[di + 2] = src.data[si + 2];
      out.data[di + 3] = src.data[si + 3];
    }
  }
  return out;
}

function setPivotBottom(metaPath) {
  if (!existsSync(metaPath)) return "no-meta";
  let s = readFileSync(metaPath, "utf8");
  if (/spritePivot:\s*\{x: 0\.5, y: 0\}/.test(s)) return "already";
  s = s.replace(/spritePivot:\s*\{x: 0\.5, y: 0\.5\}/, "spritePivot: {x: 0.5, y: 0}");
  s = s.replace(/spritePivot:\s*\{x: 0\.5, y: 0\.5\}/g, "spritePivot: {x: 0.5, y: 0}");
  writeFileSync(metaPath, s);
  return "pivot-bottom";
}

mkdirSync(BACKUP, { recursive: true });
for (const p of PROPS) {
  const src = `${DIR}/${p.name}.png`;
  if (!existsSync(src)) { console.log(`[SKIP] ${p.name} 不存在`); continue; }
  copyFileSync(src, `${BACKUP}/${p.name}.png`); // 备份原始
  const png = PNG.sync.read(readFileSync(src));
  const out = resizeNN(png, p.w);
  writeFileSync(src, PNG.sync.write(out));
  const meta = setPivotBottom(`${src}.meta`);
  console.log(`[OK] ${p.name} ${png.width}x${png.height} -> ${out.width}x${out.height}  footprint=${p.footprint}  meta=${meta}`);
}
console.log("DONE");
