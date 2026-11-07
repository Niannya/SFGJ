// 美术素材生成/编辑/读图工具（通过 openai-next 代理）。
// 用法：
//   node .artgen/artgen.mjs edit    --image <in> --prompt <text> --out <out> [--model <m>]
//   node .artgen/artgen.mjs gen     --prompt <text> --out <out> [--model <m>] [--size WxH] [--background transparent|opaque]
//   node .artgen/artgen.mjs describe --image <in> [--prompt <text>] [--model <m>] [--max <n>]
// 环境变量 OPENAI_NEXT_API_KEY 必须已设置。

import { readFileSync, writeFileSync, mkdirSync } from "fs";
import { dirname } from "path";

const KEY = process.env.OPENAI_NEXT_API_KEY;
const BASE = "https://api.openai-next.com/v1";

function arg(name) {
  const i = process.argv.indexOf(name);
  return i >= 0 ? process.argv[i + 1] : undefined;
}

function detectExt(buf, fallback) {
  if (buf.length > 3 && buf[0] === 0xff && buf[1] === 0xd8) return ".jpg";
  if (buf.length > 8 && buf.toString("ascii", 1, 4) === "PNG") return ".png";
  if (buf.length > 4 && buf.toString("ascii", 8, 12) === "WEBP") return ".webp";
  return fallback;
}

async function savePayload(data, out) {
  mkdirSync(dirname(out), { recursive: true });
  if (data.b64_json) {
    const buf = Buffer.from(data.b64_json, "base64");
    const p = out.endsWith(".jpg") || out.endsWith(".png") || out.endsWith(".webp")
      ? out : out + detectExt(buf, ".jpg");
    writeFileSync(p, buf);
    console.log("SAVED", p);
    return p;
  }
  if (data.url) {
    const r = await fetch(data.url);
    const buf = Buffer.from(await r.arrayBuffer());
    const p = out.endsWith(".jpg") || out.endsWith(".png") || out.endsWith(".webp")
      ? out : out + detectExt(buf, ".jpg");
    writeFileSync(p, buf);
    console.log("SAVED", p);
    return p;
  }
  console.error("NO DATA PAYLOAD:", JSON.stringify(data).slice(0, 400));
  process.exit(1);
}

async function cmdEdit() {
  const image = arg("--image");
  const prompt = arg("--prompt");
  const out = arg("--out");
  const model = arg("--model") || "gemini-nano-banana-2.1";
  const size = arg("--size");
  if (!image || !prompt || !out) { console.error("edit 需要 --image --prompt --out"); process.exit(1); }
  const img = readFileSync(image);
  const ext = image.toLowerCase().endsWith(".jpg") || image.toLowerCase().endsWith(".jpeg") ? "jpeg" : "png";
  const fd = new FormData();
  fd.append("model", model);
  fd.append("image", new Blob([img], { type: "image/" + ext }), "input." + ext);
  fd.append("prompt", prompt);
  if (size) fd.append("size", size);
  const r = await fetch(`${BASE}/images/edits`, {
    method: "POST",
    headers: { Authorization: `Bearer ${KEY}` },
    body: fd,
  });
  const j = await r.json();
  if (!r.ok) { console.error("HTTP", r.status, JSON.stringify(j).slice(0, 900)); process.exit(1); }
  await savePayload(j.data?.[0], out);
}

async function cmdGen() {
  const prompt = arg("--prompt");
  const out = arg("--out");
  const model = arg("--model") || "doubao-seedream-5-0-pro-260628";
  const size = arg("--size") || "1024x1024";
  const background = arg("--background");
  if (!prompt || !out) { console.error("gen 需要 --prompt --out"); process.exit(1); }
  const body = { model, prompt, n: 1, size };
  if (background) body.background = background;
  const r = await fetch(`${BASE}/images/generations`, {
    method: "POST",
    headers: { Authorization: `Bearer ${KEY}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const j = await r.json();
  if (!r.ok) { console.error("HTTP", r.status, JSON.stringify(j).slice(0, 900)); process.exit(1); }
  await savePayload(j.data?.[0], out);
}

async function cmdDescribe() {
  const image = arg("--image");
  const prompt = arg("--prompt") || "详细描述这张图片的内容、视角、美术风格、配色、布局和明显问题。";
  const model = arg("--model") || "gemini-2.5-flash";
  const max = Number(arg("--max") || 900);
  if (!image) { console.error("describe 需要 --image"); process.exit(1); }
  const img = readFileSync(image);
  const b64 = img.toString("base64");
  const mime = image.toLowerCase().endsWith(".webp") ? "image/webp"
    : image.toLowerCase().endsWith(".jpg") || image.toLowerCase().endsWith(".jpeg") ? "image/jpeg"
    : "image/png";
  const body = {
    model,
    messages: [{ role: "user", content: [
      { type: "text", text: prompt },
      { type: "image_url", image_url: { url: `data:${mime};base64,${b64}` } },
    ] }],
    max_tokens: max,
  };
  const r = await fetch(`${BASE}/chat/completions`, {
    method: "POST",
    headers: { Authorization: `Bearer ${KEY}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const j = await r.json();
  if (!r.ok) { console.error("HTTP", r.status, JSON.stringify(j).slice(0, 900)); process.exit(1); }
  console.log(j.choices?.[0]?.message?.content || JSON.stringify(j).slice(0, 800));
}

const cmd = process.argv[2];
if (cmd === "edit") cmdEdit();
else if (cmd === "gen") cmdGen();
else if (cmd === "describe") cmdDescribe();
else { console.error("用法: node artgen.mjs <edit|gen|describe> ..."); process.exit(1); }
