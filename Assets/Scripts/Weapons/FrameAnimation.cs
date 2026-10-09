using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 스킬 이펙트 프레임 재생 (설계서 7-D절 3). 활성화될 때마다 처음부터 재생한다 — 풀에서 다시 꺼낸 투사체,
    /// 새 콤보마다 켜지는 이펙트가 별도 호출 없이 처음 프레임부터 시작한다.
    /// Animator 대신 쓰는 이유: 클립·컨트롤러 에셋 없이 프레임 배열만으로 되고, 재생 길이를 무기 데이터(콤보 시간)에 맞출 수 있다.
    /// 게임 시간 기준이라 일시정지 중에는 멈춘다.
    /// </summary>
    public class FrameAnimation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [Tooltip("재생 순서대로. 같은 스프라이트를 반복해 넣으면 왕복 루프가 된다")]
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.1f)] float framesPerSecond = 12f;
        [SerializeField] bool loop = true;

        float time;

        public int CurrentFrame { get; private set; }
        public int FrameCount => frames == null ? 0 : frames.Length;

        /// <summary>한 번 재생에 걸리는 시간을 맞춘다 (화산십이검: 콤보 지속 시간 동안 1회).</summary>
        public void SetDuration(float seconds)
        {
            if (FrameCount > 0 && seconds > 0f)
                framesPerSecond = FrameCount / seconds;
        }

        void OnEnable()
        {
            time = 0f;
            Show(0);
        }

        void Update()
        {
            if (FrameCount == 0)
                return;
            time += Time.deltaTime;
            int index = (int)(time * framesPerSecond);
            Show(loop ? index % FrameCount : Mathf.Min(index, FrameCount - 1));
        }

        void Show(int index)
        {
            if (FrameCount == 0 || target == null)
                return;
            CurrentFrame = index;
            target.sprite = frames[index];
        }
    }
}
