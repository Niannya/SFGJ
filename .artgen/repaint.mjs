// 道具风格统一重绘：nano-banana 图生图 → 洋红底抠图 → 裁剪 → 缩放到目标宽度。
// 用法：node .artgen/repaint.mjs <name> [<targetWidth>] [<extraPrompt>]
//   <name> 是 Assets/PNG/props/<name>.png
//   默认沿用 standardize 里的占地宽度；extraPrompt 可追加视角/风格指令
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "fs";
import pngjs from "pngjs";
const { PNG } = pngjs;

const KEY = process.env.OPENAI_NEXT_API_KEY;
const BASE = "https://api.openai-next.com/v1";
const DIR = "Assets/PNG/props";

const name = process.argv[2];
const targetW = Number(process.argv[3] || 100);
const extra = process.argv.slice(4).join(" ") || "";
if (!name) { console.error("用法: repaint.mjs <name> [targetWidth] [extraPrompt]"); process.exit(1); }

const STYLE = `16-bit retro RPG pixel art, top-down 3/4 view furniture (object stands upright facing the camera with a slight downward angle, like Stardew Valley furniture), warm palette, single-pixel dark-brown outline, flat colors with simple 2-3 step shading, no anti-aliasing, no gradients, crisp hard pixel edges, isolated on a solid pure magenta (#FF00FF) flat background, no shadow on background, single object only, no text, no labels, no sprite sheet`;

async function main() {
  // 优先用高分辨率原图重绘（backup_props），避免在 100px 低清图上重绘
  const backup = `.artgen/backup_props/${name}.png`;
  const src = existsSync(backup) ? backup : `${DIR}/${name}.png`;
  const png = PNG.sync.read(readFileSync(src));

  // 合成到洋红底（留边），保证图生图背景纯净
  const pad = 12;
  const cw = png.width + pad * 2, ch = png.height + pad * 2;
  const comp = new PNG({ width: cw, height: ch });
  for (let i = 0; i < cw * ch * 4; i += 4) { comp.data[i] = 255; comp.data[i + 1] = 0; comp.data[i + 2] = 255; comp.data[i + 3] = 255; }
  for (let y = 0; y < png.height; y++) for (let x = 0; x < png.width; x++) {
    const si = (y * png.width + x) * 4, di = ((y + pad) * cw + (x + pad)) * 4;
    comp.data[di] = png.data[si]; comp.data[di + 1] = png.data[si + 1];
    comp.data[di + 2] = png.data[si + 2]; comp.data[di + 3] = 255;
  }
  const tmpIn = `.artgen/_repaint_${name}_in.png`;
  const tmpOut = `.artgen/_repaint_${name}_out.png`;
  writeFileSync(tmpIn, PNG.sync.write(comp));

  const prompt = (extra ? extra + ". " : "") + STYLE;
  const fd = new FormData();
  fd.append("model", "gemini-nano-banana-2.1");
  fd.append("image", new Blob([readFileSync(tmpIn)], { type: "image/png" }), "in.png");
  fd.append("prompt", prompt);
  const r = await fetch(`${BASE}/images/edits`, { method: "POST", headers: { Authorization: `Bearer ${KEY}` }, body: fd });
  const j = await r.json();
  if (!r.ok) { console.error("HTTP", r.status, JSON.stringify(j).slice(0, 600)); process.exit(1); }
  const d = j.data?.[0];
  let buf;
  if (d.b64_json) buf = Buffer.from(d.b64_json, "base64");
  else if (d.url) buf = Buffer.from(await (await fetch(d.url)).arrayBuffer());
  else { console.error("no data"); process.exit(1); }
  writeFileSync(tmpOut, buf);

  // 解码（png 或 jpg）
  let out;
  if (buf[0] === 0x89) out = PNG.sync.read(buf);
  else { const jpg = await import("jpeg-js"); const raw = jpg.default.decode(buf, { useTArray: true });
    out = new PNG({ width: raw.width, height: raw.height });
    for (let i = 0; i < raw.width * raw.height; i++) { out.data[i * 4] = raw.data[i * 4]; out.data[i * 4 + 1] = raw.data[i * 4 + 1]; out.data[i * 4 + 2] = raw.data[i * 4 + 2]; out.data[i * 4 + 3] = 255; }
  }

  // 抠洋红
  for (let i = 0; i < out.width * out.height * 4; i += 4) {
    const rr = out.data[i], gg = out.data[i + 1], bb = out.data[i + 2];
    if (rr > 80 && bb > 80 && gg < Math.min(rr, bb) * 0.7 && (rr - gg) > 25 && (bb - gg) > 25) out.data[i + 3] = 0;
  }
  // 裁剪
  let minX = out.width, minY = out.height, maxX = -1, maxY = -1;
  for (let y = 0; y < out.height; y++) for (let x = 0; x < out.width; x++) if (out.data[(y * out.width + x) * 4 + 3] > 10) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
  if (maxX < 0) { console.error("crop: 无内容"); process.exit(1); }
  minX = Math.max(0, minX - 1); minY = Math.max(0, minY - 1); maxX = Math.min(out.width - 1, maxX + 1); maxY = Math.min(out.height - 1, maxY + 1);
  const cw2 = maxX - minX + 1, ch2 = maxY - minY + 1;
  const cropped = new PNG({ width: cw2, height: ch2 });
  for (let y = 0; y < ch2; y++) for (let x = 0; x < cw2; x++) { const si = ((minY + y) * out.width + (minX + x)) * 4, di = (y * cw2 + x) * 4; cropped.data[di] = out.data[si]; cropped.data[di + 1] = out.data[si + 1]; cropped.data[di + 2] = out.data[si + 2]; cropped.data[di + 3] = out.data[si + 3]; }
  // 缩放
  const th = Math.max(1, Math.round(cropped.height * targetW / cropped.width));
  const final = new PNG({ width: targetW, height: th });
  for (let y = 0; y < th; y++) { const sy = Math.min(cropped.height - 1, Math.floor(y * cropped.height / th)); for (let x = 0; x < targetW; x++) { const sx = Math.min(cropped.width - 1, Math.floor(x * cropped.width / targetW)); const si = (sy * cropped.width + sx) * 4, di = (y * targetW + x) * 4; final.data[di] = cropped.data[si]; final.data[di + 1] = cropped.data[si + 1]; final.data[di + 2] = cropped.data[si + 2]; final.data[di + 3] = cropped.data[si + 3]; } }
  writeFileSync(`${DIR}/${name}.png`, PNG.sync.write(final));
  console.log(`[REPAINTED] ${name} ${cropped.width}x${cropped.height} -> ${targetW}x${th}`);
}
main().catch(e => { console.error("FATAL", e.message); process.exit(1); });
