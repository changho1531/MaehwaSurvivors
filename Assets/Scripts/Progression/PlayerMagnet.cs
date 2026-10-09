using System.Collections.Generic;
using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 플레이어 쪽에서 반경 안 구슬만 조회해 끌어당기고, 닿으면 습득한다 (설계서 6-A절 규칙 3~4).
    /// 구슬은 계산하지 않으므로 맵 전체 구슬 수와 무관하게 비용은 "반경 안 구슬 수"에만 비례한다.
    /// 조회는 XP 전용 레이어 + 미리 할당한 버퍼(NonAlloc)라 매 프레임 GC가 생기지 않는다.
    /// </summary>
    [RequireComponent(typeof(PlayerLevel))]
    public class PlayerMagnet : MonoBehaviour
    {
        [SerializeField] XpSettings settings;

        readonly Collider2D[] hits = new Collider2D[256];
        readonly List<XpOrb> pulling = new();
        ContactFilter2D filter;
        PlayerLevel level;
        PlayerMovement movement;

        public XpSettings Settings => settings;
        public int PullingCount => pulling.Count;

        /// <summary>끌림 속도는 항상 플레이어 최고 속도보다 빨라야 구슬이 따라붙는다 (6-A절 데이터).</summary>
        public float PullSpeed => movement != null ? Mathf.Max(settings.pullSpeed, movement.MoveSpeed * 1.5f) : settings.pullSpeed;

        void Awake()
        {
            level = GetComponent<PlayerLevel>();
            movement = GetComponent<PlayerMovement>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(GameLayers.Xp));
        }

        void Update()
        {
            // 일시정지(LevelUp·Pause) 중에는 조회·이동 모두 하지 않는다.
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            CaptureOrbsInRadius();
            PullAndCollect(dt);
        }

        void CaptureOrbsInRadius()
        {
            int count = Physics2D.OverlapCircle(transform.position, settings.magnetRadius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent(out XpOrb orb) || orb.IsPulled || !orb.isActiveAndEnabled)
                    continue;
                orb.BeginPull();
                pulling.Add(orb);
            }
        }

        void PullAndCollect(float dt)
        {
            Vector2 center = transform.position;
            float step = PullSpeed * dt;
            float pickupSqr = settings.pickupDistance * settings.pickupDistance;

            for (int i = pulling.Count - 1; i >= 0; i--)
            {
                var orb = pulling[i];
                // 성장 완료 등으로 이미 풀에 돌아간 구슬은 목록에서만 뺀다.
                if (orb == null || !orb.isActiveAndEnabled || !orb.IsPulled)
                {
                    pulling.RemoveAt(i);
                    continue;
                }

                var next = Vector2.MoveTowards(orb.transform.position, center, step);
                orb.transform.position = next;
                if ((next - center).sqrMagnitude > pickupSqr)
                    continue;

                pulling.RemoveAt(i);
                int value = orb.Value;
                orb.Collect();
                level.AddXp(value);
            }
        }
    }
}
