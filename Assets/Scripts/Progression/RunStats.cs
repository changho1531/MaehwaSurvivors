using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 이번 런의 생존 시간. 게임 시간(deltaTime)으로 누적하므로 Pause·LevelUp·GameOver 중에는 자동으로 멈춘다
    /// (TimeScalePolicy가 시간 정지를 한곳에서 관리). HUD와 이후 결과 화면이 읽는다.
    /// </summary>
    public class RunStats : MonoBehaviour
    {
        public float Elapsed { get; private set; }

        void Update()
        {
            Elapsed += Time.deltaTime;
        }

        /// <summary>"mm:ss" (60분 이상이면 분이 그대로 늘어난다).</summary>
        public static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
