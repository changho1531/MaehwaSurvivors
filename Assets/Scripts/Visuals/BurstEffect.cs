using System;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>원형으로 한 번 터지는 이펙트 (낙매진각 내려찍기 등). 그림 속 원이 판정 반경과 같도록 스케일해 "보이는 원 = 밀려나는 테두리"로 보여 주고, 한 번 재생 후 숨는다.</summary>
    public class BurstEffect : MonoBehaviour
    {
        [SerializeField] FrameAnimation art;
        [Tooltip("아트 스케일 1일 때 그림 속 원의 반경(월드 단위)")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 1.71f;
        [SerializeField, Min(0.05f)] float seconds = 0.6f;

        float remaining;
        Action<BurstEffect> release;

        public FrameAnimation Art => art;

        /// <summary>그림 크기 그대로 1회 재생 (저주 시전 · 소환 연기). 풀에서 꺼냈으면 끝날 때 releaseToPool로 돌려보낸다.</summary>
        public void Play(Vector2 position, Action<BurstEffect> releaseToPool = null) => Play(position, artRadiusAtScale1, releaseToPool);

        /// <param name="overrideSeconds">0보다 크면 프리팹 길이 대신 이 시간 동안 1회 재생 (혈풍선: 패턴 데이터의 경고 + 회전 시간에 맞춤)</param>
        public void Play(Vector2 position, float radius, Action<BurstEffect> releaseToPool = null, float overrideSeconds = 0f)
        {
            release = releaseToPool;
            transform.position = position;
            float duration = overrideSeconds > 0f ? overrideSeconds : seconds;
            remaining = duration;
            if (art != null)
            {
                art.transform.localScale = Vector3.one * (radius / artRadiusAtScale1);
                art.SetDuration(duration);
            }
            gameObject.SetActive(false); // 쿨타임을 아주 짧게 조정해도 처음 프레임부터 다시 재생되게
            gameObject.SetActive(true);
        }

        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining > 0f)
                return;
            if (release != null)
                release(this); // 풀이 비활성화까지 맡는다
            else
                gameObject.SetActive(false);
        }
    }
}
