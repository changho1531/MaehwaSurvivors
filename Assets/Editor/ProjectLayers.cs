using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 게임이 쓰는 레이어를 프로젝트 설정에 등록한다. 에디터가 로드될 때 자동 확인하고, 메뉴로도 실행할 수 있다.
    /// XP 레이어는 어떤 레이어와도 충돌하지 않게 해서, 구슬 수천 개가 물리 접촉 계산에 끼지 않게 한다
    /// (PlayerMagnet의 OverlapCircle 조회는 충돌 행렬과 무관하게 동작한다).
    /// </summary>
    [InitializeOnLoad]
    static class ProjectLayers
    {
        const int XpLayerIndex = 8;

        static ProjectLayers()
        {
            EditorApplication.delayCall += Ensure;
        }

        [MenuItem("Tools/Project/Ensure Layers")]
        static void Ensure()
        {
            int xp = EnsureLayer(GameLayers.Xp, XpLayerIndex);
            if (xp < 0)
                return;

            bool changed = false;
            for (int i = 0; i < 32; i++)
            {
                if (Physics2D.GetIgnoreLayerCollision(xp, i))
                    continue;
                Physics2D.IgnoreLayerCollision(xp, i, true);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[ProjectLayers] '{GameLayers.Xp}' 레이어 충돌을 모두 끔");
            }
        }

        /// <summary>이름의 레이어가 없으면 preferredIndex(비어 있을 때) 또는 첫 빈 칸에 만든다.</summary>
        static int EnsureLayer(string name, int preferredIndex)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0)
                return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var layers = tagManager.FindProperty("layers");

            int index = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue) ? preferredIndex : -1;
            for (int i = 8; index < 0 && i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    index = i;
            }

            if (index < 0)
            {
                Debug.LogError($"[ProjectLayers] 빈 레이어 칸이 없어 '{name}'을 만들 수 없다");
                return -1;
            }

            layers.GetArrayElementAtIndex(index).stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ProjectLayers] '{name}' 레이어를 {index}번에 등록");
            return index;
        }
    }
}
