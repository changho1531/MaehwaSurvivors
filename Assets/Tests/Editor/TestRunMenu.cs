using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests
{
    /// <summary>
    /// 메뉴에서 테스트를 실행하고 결과 요약을 Library/TestResults/last-run.txt 에 남긴다.
    /// Test Runner 창을 열지 않고도(예: 메뉴·스크립트 호출) 결과를 확인하기 위한 도구.
    /// Library/TestResults/filter.txt 에 정규식을 적으면 해당 테스트만 실행한다.
    /// </summary>
    [InitializeOnLoad]
    static class TestRunMenu
    {
        const string ResultDir = "Library/TestResults";
        const string ResultPath = ResultDir + "/last-run.txt";
        const string FilterPath = ResultDir + "/filter.txt";

        static readonly TestRunnerApi Api;

        static TestRunMenu()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new ResultWriter());
        }

        [MenuItem("Tools/Tests/Run EditMode Tests")]
        static void RunEditMode() => Run(TestMode.EditMode);

        [MenuItem("Tools/Tests/Run PlayMode Tests")]
        static void RunPlayMode() => Run(TestMode.PlayMode);

        static void Run(TestMode mode)
        {
            Directory.CreateDirectory(ResultDir);
            File.WriteAllText(ResultPath, $"RUNNING {mode}\n");

            // 수정된 씬이 있으면 Test Runner가 "Scene(s) Have Been Modified" 모달을 띄워
            // 자동화로 실행할 때 에디터가 멈춘다. 미리 저장해 두고, 저장할 수 없으면 중단한다.
            if (!SaveDirtyScenes(out var error))
            {
                File.WriteAllText(ResultPath, $"DONE Aborted {error}\n");
                Debug.LogError("[TestRunMenu] " + error);
                return;
            }

            // filter.txt 가 있으면 그 정규식에 맞는 테스트만 실행한다 (예: "TitleSceneTests", "\.F9_").
            var filter = new Filter { testMode = mode };
            if (File.Exists(FilterPath))
            {
                var pattern = File.ReadAllText(FilterPath).Trim();
                if (pattern.Length > 0)
                    filter.groupNames = new[] { pattern };
            }

            Api.Execute(new ExecutionSettings(filter));
        }

        static bool SaveDirtyScenes(out string error)
        {
            error = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                    continue;
                if (string.IsNullOrEmpty(scene.path))
                {
                    error = "저장되지 않은 새 씬(Untitled)이 열려 있어 테스트를 시작할 수 없다. 먼저 저장하거나 닫을 것.";
                    return false;
                }
                if (!EditorSceneManager.SaveScene(scene))
                {
                    error = $"{scene.path} 저장 실패";
                    return false;
                }
            }
            return true;
        }

        class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"DONE {result.ResultState} pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount} inconclusive={result.InconclusiveCount} duration={result.Duration:F1}s");
                AppendLeaves(result, sb);
                Directory.CreateDirectory(ResultDir);
                File.WriteAllText(ResultPath, sb.ToString());
                Debug.Log($"[TestRunMenu] {sb}");
            }

            static void AppendLeaves(ITestResultAdaptor node, StringBuilder sb)
            {
                if (!node.HasChildren)
                {
                    sb.AppendLine($"{node.TestStatus,-12} {node.Test.FullName}");
                    if (node.TestStatus == TestStatus.Failed)
                    {
                        sb.AppendLine("    " + node.Message?.Trim().Replace("\n", "\n    "));
                        if (!string.IsNullOrEmpty(node.StackTrace))
                            sb.AppendLine("    " + node.StackTrace.Trim().Replace("\n", "\n    "));
                    }
                    return;
                }

                foreach (var child in node.Children)
                    AppendLeaves(child, sb);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
