using UnityEditor;
using UnityEngine;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 创建纯白材质并配置到 Player 预制体的 InteractableHighlighter.glowMaterial。
    /// 菜单：SFGJ → 场景 → 创建纯白描边材质并配置。
    /// </summary>
    public static class SolidWhiteMaterialSetup
    {
        private const string PlayerPrefabPath = "Assets/预制体/Player.prefab";

        [MenuItem("SFGJ/场景/创建纯白描边材质并配置", priority = 13)]
        public static void Setup()
        {
            var shader = Shader.Find("Custom/2D/SolidWhite");
            if (shader == null)
            {
                Debug.LogError("[高亮] 找不到 Custom/2D/SolidWhite shader（可能 shader 有编译错误）。");
                return;
            }

            const string matPath = "Assets/SFGJ/Materials/SolidWhite.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/SFGJ/Materials"))
                    AssetDatabase.CreateFolder("Assets/SFGJ", "Materials");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
                Debug.Log("[高亮] 已创建纯白材质：" + matPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
            }

            // 配置到 Player 预制体
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[高亮] 找不到 Player 预制体：" + PlayerPrefabPath);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var hl = instance.GetComponent<InteractableHighlighter>();
                if (hl == null) hl = instance.AddComponent<InteractableHighlighter>();
                hl.glowMaterial = mat;
                PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
                Debug.Log("[高亮] 已把纯白材质配置到 Player 的 InteractableHighlighter.glowMaterial。");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
