using Game.CameraSystem;
using UnityEngine;

namespace Game.Enemies.Boss
{
    /// <summary>
    /// 보스 카메라 연출 (연출만): 등장 → 화면 흔들림, 페이즈 전환 → 흔들림 + 카메라가 잠깐 당겨짐,
    /// 처치 → 슬로 모션 동안 보스 쪽으로 초점이 옮겨 가며 확대. BossSpawner · BossBehaviour 이벤트만 구독한다 (Observer) —
    /// 보스 로직과 판정은 이 클래스의 존재를 모른다. 처치 연출은 게임 속도가 느려진 동안이라 실시간 기준.
    /// </summary>
    public class BossPresentation : MonoBehaviour
    {
        [SerializeField] BossSpawner spawner;
        [SerializeField] CameraFx cameraFx;
        [Header("※ 예시값")]
        [SerializeField] float appearShake = 0.3f;
        [SerializeField, Min(0f)] float appearShakeSeconds = 0.7f;
        [SerializeField] float phaseShake = 0.4f;
        [SerializeField, Min(0f)] float phaseShakeSeconds = 0.6f;
        [Tooltip("페이즈 전환 때 화면 크기 배율 (1보다 작으면 당겨짐)")]
        [SerializeField, Range(0.5f, 1f)] float phaseZoom = 0.9f;
        [Tooltip("처치 순간 보스 쪽으로 옮길 초점 비율 (0 = 플레이어, 1 = 보스)")]
        [SerializeField, Range(0f, 1f)] float defeatFocus = 0.6f;
        [SerializeField, Range(0.4f, 1f)] float defeatZoom = 0.7f;
        [SerializeField, Min(0.05f)] float defeatSeconds = 0.45f;

        BossBehaviour behaviour;
        Enemy boss;

        void OnEnable()
        {
            if (spawner == null)
                return;
            spawner.BossSpawned += OnBossSpawned;
            spawner.BossDefeated += OnBossDefeated;
        }

        void OnDisable()
        {
            if (spawner != null)
            {
                spawner.BossSpawned -= OnBossSpawned;
                spawner.BossDefeated -= OnBossDefeated;
            }
            if (behaviour != null)
                behaviour.PhaseTransitionStarted -= OnPhaseTransition;
            behaviour = null;
        }

        void OnBossSpawned(Enemy spawned)
        {
            boss = spawned;
            behaviour = spawned.GetComponent<BossBehaviour>();
            if (behaviour != null)
                behaviour.PhaseTransitionStarted += OnPhaseTransition;
            if (cameraFx != null)
                cameraFx.Shake(appearShake, appearShakeSeconds); // 화면 밖에서 무언가 큰 것이 온다
        }

        void OnPhaseTransition()
        {
            if (cameraFx == null)
                return;
            cameraFx.Shake(phaseShake, phaseShakeSeconds);
            cameraFx.ZoomPulse(phaseZoom, phaseShakeSeconds * 0.4f, phaseShakeSeconds * 0.8f);
        }

        void OnBossDefeated()
        {
            if (cameraFx == null || boss == null)
                return;
            // 카메라는 플레이어 기준으로 따라가므로, 보스까지의 거리 × 비율만큼 초점을 옮긴다
            Vector2 toBoss = boss.transform.position - cameraFx.transform.position + (Vector3)cameraFx.Offset;
            cameraFx.FocusAndZoom(toBoss * defeatFocus, defeatZoom, defeatSeconds);
            cameraFx.Shake(appearShake, defeatSeconds, unscaled: true);
        }
    }
}
