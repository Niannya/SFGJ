using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可互动物体高亮器（挂在玩家身上）。
/// 每帧向玩家面朝方向发射射线，命中「指定层级」的物体后，在它背后放一个「轮廓外发光」光晕。
/// 光晕贴图在运行时按目标精灵的 alpha 轮廓生成（向轮廓外扩散+模糊），光贴着精灵形状走，
/// 不依赖任何 shader 对 UV 的假设，人物本体材质完全不动。
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    [Header("射线检测")]
    [Tooltip("命中层级 n：只高亮这一层的物体；0 表示自动复用 PlayerInteraction 的交互层")]
    public LayerMask targetLayer;

    [Tooltip("射线长度")]
    public float rayDistance = 1.5f;

    [Tooltip("射线起点；留空则用玩家身上的 InteractionOrigin，再没有就用玩家自己")]
    public Transform rayOrigin;

    [Header("光晕")]
    [Tooltip("光晕颜色")]
    public Color glowColor = new Color(1f, 0.92f, 0.4f, 0.9f);

    [Tooltip("光晕宽度（相对精灵短边的比例，0.08 = 短边的 8%）")]
    public float glowWidthRatio = 0.08f;

    [Tooltip("光晕模糊次数（越大越柔）")]
    public int glowBlur = 2;

    [Header("呼吸（可选）")]
    [Tooltip("是否呼吸闪烁")]
    public bool pulse = true;

    [Tooltip("呼吸速度")]
    public float pulseSpeed = 4f;

    private PlayerController _player;
    private GameObject _glowObject;
    private SpriteRenderer _glowRenderer;
    private Sprite _currentSourceSprite;

    // 光晕贴图缓存：同一个精灵只生成一次
    private static readonly Dictionary<Sprite, Sprite> GlowCache = new Dictionary<Sprite, Sprite>();

    private void Awake()
    {
        _player = GetComponent<PlayerController>();

        if (rayOrigin == null)
        {
            var t = transform.Find("InteractionOrigin");
            rayOrigin = t != null ? t : transform;
        }

        if (targetLayer.value == 0)
        {
            var pi = GetComponent<PlayerInteraction>();
            if (pi != null) targetLayer = pi.InteractionLayers;
        }
    }

    private void Update()
    {
        var sr = Detect();
        if (sr != null)
        {
            ShowGlow(sr);
            if (pulse) ApplyPulse();
        }
        else
        {
            HideGlow();
        }
    }

    private SpriteRenderer Detect()
    {
        if (_player == null || rayOrigin == null) return null;

        Vector2 origin = rayOrigin.position;
        Vector2 dir = _player.FacingDirection;

        bool prev = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = true;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, rayDistance, targetLayer);
        Physics2D.queriesHitTriggers = prev;

        if (hit.collider == null) return null;
        return hit.collider.GetComponent<SpriteRenderer>();
    }

    private void ShowGlow(SpriteRenderer target)
    {
        if (target.sprite == null) return;

        if (_glowRenderer == null)
        {
            _glowObject = new GameObject("InteractGlow");
            _glowRenderer = _glowObject.AddComponent<SpriteRenderer>();
        }

        var glowSprite = GetGlowSprite(target.sprite);
        if (glowSprite == null) return;

        // 光晕子对象：目标物体下、sortingOrder 低一层（人物背后），scale=1（贴图自带边距、PPU 已对齐）
        _glowObject.transform.SetParent(target.transform, false);
        _glowObject.transform.localPosition = Vector3.zero;
        _glowObject.transform.localScale = Vector3.one;

        _glowRenderer.sprite = glowSprite;
        _glowRenderer.sortingLayerID = target.sortingLayerID;
        _glowRenderer.sortingOrder = target.sortingOrder - 1;

        _currentSourceSprite = target.sprite;
        _glowObject.SetActive(true);
    }

    private void ApplyPulse()
    {
        if (_glowRenderer == null || !_glowObject.activeSelf) return;
        float a = 0.75f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.25f;
        _glowRenderer.color = new Color(1f, 1f, 1f, a);
    }

    private void HideGlow()
    {
        if (_glowObject != null && _glowObject.activeSelf)
            _glowObject.SetActive(false);
        _currentSourceSprite = null;
    }

    // ================= 光晕贴图生成 =================

    private Sprite GetGlowSprite(Sprite source)
    {
        if (GlowCache.TryGetValue(source, out var cached) && cached != null)
            return cached;

        var generated = BuildGlowSprite(source);
        GlowCache[source] = generated;
        return generated;
    }

    /// <summary>按源精灵 alpha 轮廓生成「轮廓外光晕」贴图：画布四周留边、轮廓向外扩散 + 模糊、只保留轮廓外部分。</summary>
    private Sprite BuildGlowSprite(Sprite source)
    {
        Rect rect = source.textureRect;
        int w = Mathf.Max(1, Mathf.RoundToInt(rect.width));
        int h = Mathf.Max(1, Mathf.RoundToInt(rect.height));

        Color[] src = GetPixelsSafe(source.texture, rect);
        if (src == null || src.Length != w * h) return null;

        // 1. alpha 数组
        var alpha = new float[w * h];
        for (int i = 0; i < alpha.Length; i++)
            alpha[i] = src[i].a;

        // 2. 画布四周留边（光晕宽度相对精灵短边，高分辨率精灵也有明显光晕）
        int spread = Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(w, h) * glowWidthRatio));
        int pad = spread + glowBlur * 2 + 2;
        int cw = w + pad * 2;
        int ch = h + pad * 2;
        var canvas = new float[cw * ch];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                canvas[(y + pad) * cw + (x + pad)] = alpha[y * w + x];

        // 3. 向外扩散（光晕宽度）+ 模糊（柔边）
        var spreaded = Dilate(canvas, cw, ch, spread);
        var soft = Blur(spreaded, cw, ch, Mathf.Max(1, glowBlur));

        // 4. 光晕 = 扩散结果 - 原轮廓（只留轮廓外），涂上光晕色
        var outPixels = new Color[cw * ch];
        for (int i = 0; i < outPixels.Length; i++)
        {
            float g = Mathf.Clamp01(soft[i] - canvas[i]);
            outPixels[i] = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * g);
        }

        // 5. 生成贴图与 Sprite（PPU 同比放大，保证人物区域和本体完全对齐）
        var tex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(outPixels);
        tex.Apply();

        float ppu = source.pixelsPerUnit * ((float)cw / w);
        return Sprite.Create(tex, new Rect(0, 0, cw, ch), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>读取纹理指定区域像素；纹理不可读时用 RenderTexture 中转。</summary>
    private static Color[] GetPixelsSafe(Texture2D tex, Rect rect)
    {
        int x = Mathf.FloorToInt(rect.x);
        int y = Mathf.FloorToInt(rect.y);
        int w = Mathf.CeilToInt(rect.width);
        int h = Mathf.CeilToInt(rect.height);

        try
        {
            return tex.GetPixels(x, y, w, h);
        }
        catch
        {
            // 纹理不可读：经 RenderTexture 回读
        }

        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0);
        var prev = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var result = readable.GetPixels(x, y, w, h);
        Object.Destroy(readable);
        return result;
    }

    /// <summary>把 alpha 向外扩散 iterations 次（3x3 最大值滤波）。</summary>
    private static float[] Dilate(float[] src, int w, int h, int iterations)
    {
        var cur = (float[])src.Clone();
        var next = new float[src.Length];

        for (int it = 0; it < iterations; it++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float m = cur[y * w + x];
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= w) continue;
                            float v = cur[ny * w + nx];
                            if (v > m) m = v;
                        }
                    }
                    next[y * w + x] = m;
                }
            }
            var tmp = cur; cur = next; next = tmp;
        }
        return cur;
    }

    /// <summary>盒式模糊（水平+垂直，重复 2 次）。</summary>
    private static float[] Blur(float[] src, int w, int h, int radius)
    {
        var a = (float[])src.Clone();
        var b = new float[src.Length];

        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= w) continue;
                        sum += a[y * w + nx]; n++;
                    }
                    b[y * w + x] = sum / n;
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= h) continue;
                        sum += b[ny * w + x]; n++;
                    }
                    a[y * w + x] = sum / n;
                }
            }
        }
        return a;
    }
}
