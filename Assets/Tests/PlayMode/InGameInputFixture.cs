using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// 가상 키보드 입력을 쓰는 InGame 테스트의 공통 베이스.
    /// InputTestFixture는 입력 시스템 상태를 통째로 바꿔 끼운다. UI 입력 모듈의 기본 액션은 static으로 공유되어
    /// 교체 후에도 사라진 상태를 참조해 예외(statePtr null)를 내므로, 교체 직전(Setup)과 복원 직전(TearDown)에
    /// UnassignActions()로 공유 액션을 폐기하고 모듈을 파괴한다. 테스트 씬은 매번 새로 로드하므로 영향 없음.
    /// </summary>
    public abstract class InGameInputFixture : InputTestFixture
    {
        bool projectActionsWereEnabled;

        public override void Setup()
        {
            DestroyUiInputModules();
            // 프로젝트 전역 액션(게임 코드는 쓰지 않음)이 켜진 채 입력 상태가 교체·복원되면, 복원 뒤 다른 테스트에서
            // "Map index out of range in ProcessControlStateChange" Assert가 간헐적으로 났다. 교체 동안만 꺼 둔다.
            projectActionsWereEnabled = InputSystem.actions != null && InputSystem.actions.enabled;
            if (projectActionsWereEnabled)
                InputSystem.actions.Disable();
            base.Setup();
        }

        public override void TearDown()
        {
            DestroyUiInputModules();
            base.TearDown();
            if (projectActionsWereEnabled && InputSystem.actions != null)
                InputSystem.actions.Enable();
        }

        static void DestroyUiInputModules()
        {
            foreach (var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                module.UnassignActions();
                Object.DestroyImmediate(module.gameObject);
            }
        }
    }
}
