using UnityEngine;

/// <summary>
/// 可互动物体高亮器（挂在玩家身上）。
/// 不依赖碰撞器/脚本：遍历场景中的 SpriteRenderer，只要「Order in Layer 等于指定值」
/// 且自身没有碰撞器（排除墙等障碍）、在玩家面朝方向、检测距离内，就复制一个它的精灵副本——
/// 副本用纯白 shader 渲染、放大一圈、放到排序层级 1（背后），形成白色描边光晕，并带呼吸闪烁。
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    [Header("检测")]
    [Tooltip("对应层级：只高亮 Order in Layer 等于这个值的精灵（填人物的 Order in Layer）")]
    public int targetSortingOrder = 3;

    [Tooltip("检测距离（世界单位）")]
    public float rayDistance = 1.5f;

    [Tooltip("面朝判定阈值（点积，0.5 ≈ 60° 内算在面前）")]
    [Range(0f, 1f)] public float faceDotThreshold = 0.5f;

    [Tooltip("判定起点；留空则用玩家身上的 InteractionOrigin")]
    public Transform rayOrigin;

    [Header("描边副本")]
    [Tooltip("纯白材质（引用 Custom/2D/SolidWhite shader）")]
    public Material glowMaterial;

    [Tooltip("副本放大倍数（>1 才会在目标周围露出白边）")]
    public float glowScale = 1.12f;

    [Tooltip("副本排序层级 sortingOrder（放 1 = 目标背后，目标 3、玩家更高）")]
    public int glowSortingOrder = 1;

    [Header("呼吸")]
    [Tooltip("是否呼吸闪烁")]
    public bool pulse = true;

    [Tooltip("呼吸速度")]
    public float pulseSpeed = 4f;

    [Tooltip("呼吸最暗 alpha")]
    public float minAlpha = 0.45f;

    [Tooltip("呼吸最亮 alpha")]
    public float maxAlpha = 1f;

    private PlayerController _player;
    private GameObject _glowCopy;
    private SpriteRenderer _glowRenderer;
    private SpriteRenderer[] _renderers;
    private float _refreshTimer;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();

        if (rayOrigin == null)
        {
            var t = transform.Find("InteractionOrigin");
            rayOrigin = t != null ? t : transform;
        }
    }

    private void Update()
    {
        // 定期刷新场景里的 SpriteRenderer 列表（房间切换会导致显隐变化）
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer <= 0f || _renderers == null)
        {
            _renderers = FindObjectsOfType<SpriteRenderer>();
            _refreshTimer = 0.5f;
        }

        var sr = Detect();
        if (sr != null)
            ShowGlow(sr);
        else
            HideGlow();

        if (pulse) ApplyPulse();
    }

    /// <summary>在缓存的精灵列表里找「对应层级 + 无碰撞器 + 距离内 + 在面前」的最近一个。</summary>
    private SpriteRenderer Detect()
    {
        if (_player == null || rayOrigin == null || _renderers == null) return null;

        Vector2 origin = rayOrigin.position;
        Vector2 dir = _player.FacingDirection;

        SpriteRenderer best = null;
        float bestDist = float.MaxValue;

        foreach (var sr in _renderers)
        {
            if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
            if (sr == _glowRenderer) continue;                        // 排除光晕副本自己
            if (sr.sortingOrder != targetSortingOrder) continue;      // 只认对应层级
            if (sr.sprite == null) continue;
            if (sr.GetComponent<Collider2D>() != null) continue;      // 排除有碰撞器的（墙/障碍）

            Vector2 toObj = (Vector2)sr.transform.position - origin;
            float dist = toObj.magnitude;
            if (dist < 0.05f || dist > rayDistance) continue;
            if (Vector2.Dot(toObj / dist, dir) < faceDotThreshold) continue;

            if (dist < bestDist) { bestDist = dist; best = sr; }
        }
        return best;
    }

    private void ShowGlow(SpriteRenderer target)
    {
        if (target.sprite == null || glowMaterial == null) return;

        if (_glowRenderer == null)
        {
            _glowCopy = new GameObject("InteractGlow");
            _glowRenderer = _glowCopy.AddComponent<SpriteRenderer>();
        }

        // 副本：同一个 sprite + 纯白材质，放大一圈，放到背后
        _glowCopy.transform.SetParent(target.transform, false);
        _glowCopy.transform.localPosition = Vector3.zero;
        _glowCopy.transform.localRotation = Quaternion.identity;
        _glowCopy.transform.localScale = Vector3.one * glowScale;

        _glowRenderer.sprite = target.sprite;
        _glowRenderer.material = glowMaterial;
        _glowRenderer.sortingLayerID = target.sortingLayerID;
        _glowRenderer.sortingOrder = glowSortingOrder;

        _glowCopy.SetActive(true);
    }

    private void ApplyPulse()
    {
        if (_glowRenderer == null || _glowCopy == null || !_glowCopy.activeSelf) return;
        float t = Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f;
        float a = Mathf.Lerp(minAlpha, maxAlpha, t);
        _glowRenderer.color = new Color(1f, 1f, 1f, a);
    }

    private void HideGlow()
    {
        if (_glowCopy != null && _glowCopy.activeSelf)
            _glowCopy.SetActive(false);
    }
}
