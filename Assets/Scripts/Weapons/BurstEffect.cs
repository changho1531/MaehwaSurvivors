using UnityEngine;

namespace Game.Weapons
{
    /// <summary>원형으로 한 번 터지는 이펙트 (낙매진각 내려찍기 등). 그림 속 원이 판정 반경과 같도록 스케일해 "보이는 원 = 밀려나는 테두리"로 보여 주고, 한 번 재생 후 숨는다.</summary>
    public class BurstEffect : MonoBehaviour
    {
        [SerializeField] FrameAnimation art;
        [Tooltip("아트 스케일 1일 때 그림 속 원의 반경(월드 단위)")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 1.71f;
        [SerializeField, Min(0.05f)] float seconds = 0.6f;

        float remaining;

        public FrameAnimation Art => art;

        public void Play(Vector2 position, float radius)
        {
            transform.position = position;
            remaining = seconds;
            if (art != null)
            {
                art.transform.localScale = Vector3.one * (radius / artRadiusAtScale1);
                art.SetDuration(seconds);
            }
            gameObject.SetActive(false); // 쿨타임을 아주 짧게 조정해도 처음 프레임부터 다시 재생되게
            gameObject.SetActive(true);
        }

        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                gameObject.SetActive(false);
        }
    }
}
