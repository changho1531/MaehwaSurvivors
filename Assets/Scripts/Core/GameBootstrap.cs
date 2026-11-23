using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 씬의 진입점: 게임 흐름 관리자(GameManager)를 만드는 유일한 곳. 타이틀 · 인게임 씬에 하나씩 둔다.
    /// 실행 순서를 가장 앞으로 당겨, 같은 씬의 다른 오브젝트가 OnEnable에서 GameManager.Current로 구독할 때 이미 있게 한다.
    /// 다른 코드는 매니저를 만들지 않고 읽기만 하므로, 씬 전환 중 잘못된 초기 상태의 매니저가 새로 생기는 일이 없다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake() => GameManager.EnsureCreated();
    }
}
