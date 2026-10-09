using Game.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 우측 상단 생존 시간 mm:ss (설계서 7-C 규칙 7). RunStats 값을 읽어 초가 바뀔 때만 다시 쓴다.
    /// 값이 게임 시간으로 누적되므로 일시정지·레벨업 중에는 표시도 멈춘다.
    /// </summary>
    public class SurvivalTimeView : MonoBehaviour
    {
        [SerializeField] RunStats stats;
        [SerializeField] Text text;

        int shownSecond = -1;

        public Text Text => text;

        void Update()
        {
            int second = Mathf.FloorToInt(stats.Elapsed);
            if (second == shownSecond)
                return;
            shownSecond = second;
            text.text = RunStats.Format(stats.Elapsed);
        }
    }
}
