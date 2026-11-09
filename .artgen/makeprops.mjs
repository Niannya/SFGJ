// 道具批量工厂：seedream 生成(洋红底) → 抠洋红→透明 → 裁剪到包围盒。
// 用法：node makeprops.mjs <name>      # 只做单个
//       node makeprops.mjs all         # 做全部
import { readFileSync, writeFileSync, mkdirSync } from "fs";
import pngjs from "pngjs";
import jpeg from "jpeg-js";
const { PNG } = pngjs;

const KEY = process.env.OPENAI_NEXT_API_KEY;
const BASE = "https://api.openai-next.com/v1";
const DIR = "Assets/PNG/props";

const PROPS = [
  { name: "flowerpot", view: "top-down", zh: "花盆", prompt: "a single flower pot with a leafy green plant" },
  { name: "bookshelf", view: "top-down", zh: "书架", prompt: "a wooden bookshelf filled with colorful books" },
  { name: "desk", view: "top-down", zh: "课桌", prompt: "a wooden school desk with an open book on top" },
  { name: "desk_fallen", view: "top-down", zh: "倒下的课桌", prompt: "an overturned fallen wooden school desk lying on its side" },
  { name: "chair", view: "top-down", zh: "椅子", prompt: "a wooden chair with a very short backrest (backrest only one-third the total height) and four legs, low and compact" },
  { name: "table", view: "top-down", zh: "卧室桌子", prompt: "a small wooden bedside table with a drawer" },
  { name: "wardrobe", view: "top-down", zh: "衣柜", prompt: "a large wooden wardrobe closet" },
  { name: "chair_bedroom", view: "top-down", zh: "卧室椅子", prompt: "a simple wooden chair with a backrest" },
  { name: "window", view: "front", zh: "窗", prompt: "a window with wooden frame showing blue sky and white clouds" },
  { name: "bed", view: "top-down", zh: "床", prompt: "a single bed with a pillow and a folded blanket" },
  { name: "desk_office_v", view: "top-down", zh: "长条办公桌(竖)", prompt: "a long rectangular office desk (vertical orientation) with a computer monitor" },
  { name: "desk_office_h", view: "top-down", zh: "长条办公桌(横)", prompt: "a long rectangular office desk showing the tabletop and the front panel, with papers on top" },
  { name: "water_dispenser", view: "top-down", zh: "饮水机", prompt: "a water dispenser with a large water bottle on top" },
  { name: "blackboard", view: "front", zh: "黑板", prompt: "a blackboard with a dark green chalkboard surface and wooden frame" },
  { name: "lamp", view: "top-down", zh: "台灯", prompt: "a simple plain table lamp with a cone lampshade and a straight base, minimal, no decoration" },
  { name: "trashbin", view: "top-down", zh: "垃圾桶", prompt: "a round trash bin" },
];

const VIEW_HINT = {
  "top-down": "three-quarter view from a high front angle, centered, classic 16-bit RPG object sprite (showing the front and a little of the top, like Zelda or Stardew Valley furniture, NOT a flat bird's-eye view)",
  "front": "front view facing the camera, centered",
};

function genPrompt(p) {
  return `${p.prompt}, pixel art, ${VIEW_HINT[p.view]}, isolated on a solid pure magenta (#FF00FF) flat background, no shadow on background, 16-bit retro RPG style, single object only, no text, no labels, no multiple views, no sprite sheet, crisp pixel edges`;
}

async function download(url, out) {
  const r = await fetch(url);
  const buf = Buffer.from(await r.arrayBuffer());
  writeFileSync(out, buf);
  return buf;
}

function decode(buf) {
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

// 抠洋红 -> 透明（含边缘洋红噪声清理）
function chromaKey(png) {
  const { width: w, height: h, data } = png;
  for (let i = 0; i < w * h * 4; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    // 洋红/粉紫：R 高、B 高、G 明显低
    if (r > 80 && b > 80 && g < Math.min(r, b) * 0.7 && (r - g) > 25 && (b - g) > 25) {
      data[i + 3] = 0;
    }
  }
}

// 裁剪到不透明包围盒
function crop(png) {
  const { width: w, height: h, data } = png;
  let minX = w, minY = h, maxX = -1, maxY = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (data[(y * w + x) * 4 + 3] > 10) {
      if (x < minX) minX = x; if (x > maxX) maxX = x;
      if (y < minY) minY = y; if (y > maxY) maxY = y;
    }
  }
  if (maxX < 0) return png;
  const m = 2;
  minX = Math.max(0, minX - m); minY = Math.max(0, minY - m);
  maxX = Math.min(w - 1, maxX + m); maxY = Math.min(h - 1, maxY + m);
  const nw = maxX - minX + 1, nh = maxY - minY + 1;
  const out = new PNG({ width: nw, height: nh });
  for (let y = 0; y < nh; y++) for (let x = 0; x < nw; x++) {
    const si = ((minY + y) * w + (minX + x)) * 4;
    const di = (y * nw + x) * 4;
    out.data[di] = data[si]; out.data[di + 1] = data[si + 1];
    out.data[di + 2] = data[si + 2]; out.data[di + 3] = data[si + 3];
  }
  return out;
}

async function makeProp(p) {
  mkdirSync(DIR, { recursive: true });
  const out = `${DIR}/${p.name}.png`;
  const body = { model: "doubao-seedream-5-0-pro-260628", prompt: genPrompt(p), n: 1, size: "1024x1024" };
  const r = await fetch(`${BASE}/images/generations`, {
    method: "POST",
    headers: { Authorization: `Bearer ${KEY}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const j = await r.json();
  if (!r.ok) { console.log(`[FAIL ${p.name}] HTTP ${r.status} ${JSON.stringify(j).slice(0, 200)}`); return false; }
  const d = j.data?.[0];
  let buf;
  if (d.b64_json) buf = Buffer.from(d.b64_json, "base64");
  else if (d.url) buf = await download(d.url, `${DIR}/_raw_${p.name}.jpg`);
  else { console.log(`[FAIL ${p.name}] no data`); return false; }
  const png = decode(buf);
  chromaKey(png);
  const cropped = crop(png);
  writeFileSync(out, PNG.sync.write(cropped));
  console.log(`[OK ${p.name}] ${cropped.width}x${cropped.height} -> ${out}`);
  return true;
}

const target = process.argv[2] || "all";
(async () => {
  const list = target === "all" ? PROPS : PROPS.filter(p => p.name === target);
  if (list.length === 0) { console.error("未知道具:", target); process.exit(1); }
  for (const p of list) {
    try { await makeProp(p); } catch (e) { console.log(`[ERR ${p.name}]`, e.message); }
  }
  console.log("DONE");
})().catch(e => { console.error("FATAL", e.message); process.exit(1); });
