using System.Linq;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Build Settings 씬 목록을 게임 흐름 순서(Title → InGame)로 맞춘다.</summary>
    static class BuildScenes
    {
        static readonly string[] Ordered =
        {
            $"Assets/Scenes/{SceneNames.Title}.unity",
            $"Assets/Scenes/{SceneNames.InGame}.unity",
        };

        [MenuItem("Tools/Project/Sync Build Scenes")]
        static void Sync()
        {
            var missing = Ordered.Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogError("[BuildScenes] 씬 파일이 없다: " + string.Join(", ", missing));
                return;
            }

            EditorBuildSettings.scenes = Ordered.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildScenes] " + string.Join(" → ", Ordered));
        }
    }
}
