// 俯视角(纯鸟瞰·平铺)场景装饰批量生成：seedream 生成 → 洋红底抠图 → 裁剪 → 适配占地尺寸。
// 分房间匹配风格。用法：node .artgen/make_topdown.mjs [name|all] [room]
import { readFileSync, writeFileSync, mkdirSync, existsSync, copyFileSync } from "fs";
import pngjs from "pngjs";
import jpeg from "jpeg-js";
const { PNG } = pngjs;

const KEY = process.env.OPENAI_NEXT_API_KEY;
const BASE = "https://api.openai-next.com/v1";
const DIR = "Assets/PNG/props";
const BACKUP34 = ".artgen/backup_props_34";

// 各房间风格（对应现有背景）
const ROOMS = {
  classroom: "dark muted palette (charcoal gray, deep brown, cool gray), flat pixel art with fine grain, hard single-pixel dark outline, cool dark dim atmosphere",
  bedroom: "warm palette (pink walls, light brown wood), classic 16-bit pixel art, single-pixel dark-brown outline, warm cozy soft light",
  clinic: "muted morandi palette (pink-blue walls, beige floor, soft gray), 16-bit pixel art, single-pixel dark gray-blue outline, soft top light",
};

// name: 文件名; room: 所属房间; ft: 占地[宽,高]格子数; obj: 俯视物体描述
const PROPS = [
  // 教室（冷灰/炭灰，暗色）
  { name: "flowerpot", room: "classroom", ft: [1, 1], obj: "a small round flower pot with a leafy green plant, pure top-down bird's-eye view seen from directly above, leaves radiating outward from the pot center, lying flat on the floor" },
  { name: "bookshelf", room: "classroom", ft: [1, 2], obj: "a wooden bookshelf, pure top-down bird's-eye view seen from directly above, showing the top opening with colorful book spines arranged in rows inside" },
  { name: "desk", room: "classroom", ft: [1, 1], obj: "a wooden school desk, pure top-down bird's-eye view seen from directly above, showing the rectangular tabletop with an open book on it" },
  { name: "desk_fallen", room: "classroom", ft: [1, 2], obj: "a wooden school desk knocked over and lying flat on the ground, top-down floor plan diagram seen from directly above, the desk appears as a flat rectangular panel with four thin rectangular legs extending outward flat on the ground, flat 2D silhouette, no perspective, no depth, no 3D shading, no elevation" },
  { name: "chair", room: "classroom", ft: [1, 1], obj: "a wooden chair, STRICTLY orthographic top-down plan view seen from directly overhead at 90 degrees, showing ONLY the square seat surface and the thin top edge of the backrest, absolutely no side elevation, no height, no 3/4 angle, flat 2D floor plan" },
  { name: "trashbin", room: "classroom", ft: [1, 1], obj: "a round trash bin, pure top-down bird's-eye view seen from directly above, showing the circular opening" },
  { name: "blackboard", room: "classroom", ft: [2, 1], wall: true, obj: "a dark green chalkboard in a wooden frame, front view facing the camera, mounted flat on a wall" },
  // 卧室（暖色 16bit）
  { name: "table", room: "bedroom", ft: [1, 1], obj: "a small wooden bedside table, pure top-down bird's-eye view seen from directly above, showing the tabletop surface" },
  { name: "wardrobe", room: "bedroom", ft: [1, 2], obj: "a large wooden wardrobe closet, pure top-down bird's-eye view seen from directly above, showing the rectangular top surface" },
  { name: "chair_bedroom", room: "bedroom", ft: [1, 1], obj: "a wooden chair, STRICTLY orthographic top-down plan view seen from directly overhead at 90 degrees, showing ONLY the square seat surface and the thin top edge of the backrest, absolutely no side elevation, no height, no 3/4 angle, flat 2D floor plan" },
  { name: "lamp", room: "bedroom", ft: [1, 1], obj: "a table lamp, pure top-down bird's-eye view seen from directly above, showing the circular lampshade" },
  { name: "window", room: "bedroom", ft: [2, 1], wall: true, obj: "a wooden-framed window showing blue sky and white clouds, front view facing the camera, mounted flat on a wall" },
  // 诊疗室（莫兰迪）
  { name: "bed", room: "clinic", ft: [2, 1], obj: "a single bed, pure top-down bird's-eye view seen from directly above, showing the mattress with a pillow and a folded blanket" },
  { name: "desk_office_v", room: "clinic", ft: [1, 2], obj: "a long rectangular office desk, pure top-down bird's-eye view seen from directly above, showing the long tabletop surface with papers" },
  { name: "desk_office_h", room: "clinic", ft: [2, 1], obj: "a long rectangular office desk, pure top-down bird's-eye view seen from directly above, showing the long tabletop surface with papers" },
  { name: "water_dispenser", room: "clinic", ft: [1, 1], obj: "a water dispenser with a large water bottle on top, pure top-down bird's-eye view seen from directly above, showing the circular bottle top" },
];

function genPrompt(p) {
  const wall = p.wall ? "" : "pure top-down bird's-eye view, viewed from directly above, flat on the floor, no perspective foreshortening";
  return `${p.obj}, pixel art, ${ROOMS[p.room]}, isolated on a solid pure magenta (#FF00FF) flat background, no shadow on background, single object only, no text, no labels, no sprite sheet, crisp pixel edges`;
}

function decode(buf) {
  if (buf[0] === 0x89 && buf[1] === 0x50) return PNG.sync.read(buf);
  if (buf[0] === 0xff && buf[1] === 0xd8) {
    const raw = jpeg.decode(buf, { useTArray: true });
    const png = new PNG({ width: raw.width, height: raw.height });
    for (let i = 0; i < raw.width * raw.height; i++) {
      png.data[i * 4] = raw.data[i * 4]; png.data[i * 4 + 1] = raw.data[i * 4 + 1];
      png.data[i * 4 + 2] = raw.data[i * 4 + 2]; png.data[i * 4 + 3] = 255;
    }
    return png;
  }
  throw new Error("unsupported format");
}

function chromaKey(png) {
  const { width: w, height: h, data } = png;
  for (let i = 0; i < w * h * 4; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    if (r > 80 && b > 80 && g < Math.min(r, b) * 0.7 && (r - g) > 25 && (b - g) > 25) data[i + 3] = 0;
  }
}

function crop(png) {
  const { width: w, height: h, data } = png;
  let minX = w, minY = h, maxX = -1, maxY = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (data[(y * w + x) * 4 + 3] > 10) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
  }
  if (maxX < 0) return null;
  const m = 1;
  minX = Math.max(0, minX - m); minY = Math.max(0, minY - m);
  maxX = Math.min(w - 1, maxX + m); maxY = Math.min(h - 1, maxY + m);
  const nw = maxX - minX + 1, nh = maxY - minY + 1;
  const out = new PNG({ width: nw, height: nh });
  for (let y = 0; y < nh; y++) for (let x = 0; x < nw; x++) {
    const si = ((minY + y) * w + (minX + x)) * 4, di = (y * nw + x) * 4;
    out.data[di] = data[si]; out.data[di + 1] = data[si + 1]; out.data[di + 2] = data[si + 2]; out.data[di + 3] = data[si + 3];
  }
  return out;
}

function resizeNN(src, tw, th) {
  const out = new PNG({ width: tw, height: th });
  for (let y = 0; y < th; y++) {
    const sy = Math.min(src.height - 1, Math.floor(y * src.height / th));
    for (let x = 0; x < tw; x++) {
      const sx = Math.min(src.width - 1, Math.floor(x * src.width / tw));
      const si = (sy * src.width + sx) * 4, di = (y * tw + x) * 4;
      out.data[di] = src.data[si]; out.data[di + 1] = src.data[si + 1]; out.data[di + 2] = src.data[si + 2]; out.data[di + 3] = src.data[si + 3];
    }
  }
  return out;
}

// 适配占地(px)：contain 居中，透明填充
function fitToFootprint(src, ftW, ftH) {
  const scale = Math.min(ftW / src.width, ftH / src.height);
  const w = Math.max(1, Math.round(src.width * scale));
  const h = Math.max(1, Math.round(src.height * scale));
  const resized = resizeNN(src, w, h);
  const out = new PNG({ width: ftW, height: ftH });
  const ox = Math.floor((ftW - w) / 2), oy = Math.floor((ftH - h) / 2);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const si = (y * w + x) * 4, di = ((oy + y) * ftW + (ox + x)) * 4;
    out.data[di] = resized.data[si]; out.data[di + 1] = resized.data[si + 1];
    out.data[di + 2] = resized.data[si + 2]; out.data[di + 3] = resized.data[si + 3];
  }
  return out;
}

async function download(url, out) {
  const r = await fetch(url);
  const buf = Buffer.from(await r.arrayBuffer());
  writeFileSync(out, buf);
  return buf;
}

async function makeProp(p) {
  mkdirSync(DIR, { recursive: true });
  const out = `${DIR}/${p.name}.png`;
  const body = { model: "doubao-seedream-5-0-pro-260628", prompt: genPrompt(p), n: 1, size: "1024x1024" };
  const r = await fetch(`${BASE}/images/generations`, {
    method: "POST", headers: { Authorization: `Bearer ${KEY}`, "Content-Type": "application/json" }, body: JSON.stringify(body),
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
  if (!cropped) { console.log(`[FAIL ${p.name}] crop empty`); return false; }
  const ftW = p.ft[0] * 100, ftH = p.ft[1] * 100;
  const final = fitToFootprint(cropped, ftW, ftH);
  writeFileSync(out, PNG.sync.write(final));
  console.log(`[OK ${p.name}] ${cropped.width}x${cropped.height} -> ${ftW}x${ftH} (${p.room})`);
  return true;
}

const target = process.argv[2] || "all";
const roomFilter = process.argv[3];
(async () => {
  mkdirSync(BACKUP34, { recursive: true });
  let list = target === "all" ? PROPS : PROPS.filter(p => p.name === target);
  if (roomFilter) list = list.filter(p => p.room === roomFilter);
  if (list.length === 0) { console.error("未知道具:", target, roomFilter); process.exit(1); }
  // 备份当前(3/4)版本
  for (const p of list) {
    const cur = `${DIR}/${p.name}.png`;
    if (existsSync(cur) && !existsSync(`${BACKUP34}/${p.name}.png`)) copyFileSync(cur, `${BACKUP34}/${p.name}.png`);
  }
  for (const p of list) {
    try { await makeProp(p); } catch (e) { console.log(`[ERR ${p.name}]`, e.message); }
  }
  console.log("DONE");
})().catch(e => { console.error("FATAL", e.message); process.exit(1); });
