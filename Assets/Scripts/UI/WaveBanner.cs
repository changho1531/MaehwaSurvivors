using Game.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 웨이브 전환 문구 (설계서 9-B 규칙 2): WaveChanged를 받으면 상단 중앙에 "제 N파"를 잠깐 띄운다. 게임은 멈추지 않는다.
    /// 마지막 0.5초는 서서히 사라진다. 게임 시간 기준이라 일시정지 중에는 표시도 그대로 멈춘다.
    /// </summary>
    public class WaveBanner : MonoBehaviour
    {
        [SerializeField] WaveDirector waves;
        [SerializeField] Text text;
        [Tooltip("※ {0} = 웨이브 번호")]
        [SerializeField] string format = "제 {0}파";
        [SerializeField, Min(0f)] float fadeSeconds = 0.5f;

        float remaining;

        public bool IsShowing => text != null && text.enabled;
        public string Label => text != null ? text.text : "";

        void Awake()
        {
            if (text != null)
                text.enabled = false;
        }

        void OnEnable()
        {
            if (waves != null)
                waves.WaveChanged += Show;
        }

        void OnDisable()
        {
            if (waves != null)
                waves.WaveChanged -= Show;
        }

        void Show(int wave) => ShowText(string.Format(format, wave), waves.Table != null ? waves.Table.bannerSeconds : 2f);

        /// <summary>상단 중앙 문구를 seconds 동안 (보스 등장 "혈각마존 출현"도 같은 자리, 8-C 연출).</summary>
        public void ShowText(string message, float seconds)
        {
            remaining = seconds;
            text.text = message;
            text.enabled = true;
            SetAlpha(1f);
            UiTween.PopIn(text.transform, 1.5f, 0.35f, unscaled: false); // 크게 나타났다 제자리로. 게임 시간 기준 (일시정지 중 멈춤)
        }

        void Update()
        {
            if (!IsShowing)
                return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                text.enabled = false;
                return;
            }
            SetAlpha(fadeSeconds > 0f ? Mathf.Clamp01(remaining / fadeSeconds) : 1f);
        }

        void SetAlpha(float alpha)
        {
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
