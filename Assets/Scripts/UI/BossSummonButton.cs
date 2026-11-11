using Game.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// [보스 소환] 버튼 (설계서 9-B 규칙 7). 조기 소환 가능 시각부터 보이고, 보스 단계가 되면 숨는다.
    /// 마우스 클릭으로만 누르며(단축키 없음) 확인창 없이 바로 보스 단계로 넘어간다. 별도 알림은 없다 — 버튼이 생기는 것 자체가 표시.
    /// 일시정지 중에는 일시정지 화면이 덮어 눌리지 않고, WaveDirector도 게임 시간이 멈춘 동안의 소환을 거절한다.
    /// </summary>
    public class BossSummonButton : MonoBehaviour
    {
        [SerializeField] WaveDirector waves;
        [SerializeField] Button button;

        public Button Button => button;
        public bool IsVisible => button != null && button.gameObject.activeSelf;

        void Awake()
        {
            button.onClick.AddListener(OnClick);
            button.gameObject.SetActive(false);
        }

        void Update()
        {
            bool visible = waves != null && waves.CanSummonEarly;
            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }

        void OnClick()
        {
            if (waves != null)
                waves.SummonBossEarly();
        }
    }
}
