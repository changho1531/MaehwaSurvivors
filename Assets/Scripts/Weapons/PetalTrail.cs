using Game.Visuals;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 매화보 꽃잎 연출 (설계서 7-H 연출). 새 그림 없이 기본 파티클: 움직일 때만 발밑에서 방출,
    /// 가속 중에는 방출량·수명을 늘려 꼬리처럼 길게 남긴다. 월드 좌표로 시뮬레이션해 지나간 자리에 남는다.
    /// 판정과 무관하며, 파티클 수치는 여기서 한 번에 설정해 인스펙터 값만으로 조정한다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PetalTrail : MonoBehaviour
    {
        [Header("※ 예시값")]
        [SerializeField] Color color = new(0.788f, 0.482f, 0.580f, 1f); // 매화분홍 #C97B94
        [SerializeField] Vector2 footOffset = new(0f, 0.05f);
        [SerializeField, Min(0f)] float spawnRadius = 0.25f;
        [SerializeField] Vector2 size = new(0.07f, 0.13f);
        [SerializeField] Vector2 speed = new(0.2f, 0.6f);
        [SerializeField] int sortingOrder = 3;

        [Header("평소")]
        [SerializeField, Min(0f)] float normalRate = 14f;
        [SerializeField, Min(0.05f)] float normalLifetime = 0.45f;
        [Header("가속 중")]
        [SerializeField, Min(0f)] float boostRate = 45f;
        [SerializeField, Min(0.05f)] float boostLifetime = 1.1f;

        ParticleSystem particles;

        public bool IsEmitting { get; private set; }
        public float CurrentRate => particles != null ? particles.emission.rateOverTime.constant : 0f;

        void Awake()
        {
            transform.localPosition = footOffset;
            particles = GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startLifetime = normalLifetime;
            main.gravityModifier = 0f;
            main.maxParticles = 300;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = spawnRadius;
            shape.rotation = Vector3.zero; // 2D 평면(XY)에서 퍼지게

            var emission = particles.emission;
            emission.rateOverTime = 0f;

            // 사라질 때 투명해지게
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.sortingOrder = sortingOrder;
            if (particleRenderer.sharedMaterial != null)
                particleRenderer.material.mainTexture = ProceduralCircle.Disk.texture;

            particles.Play();
        }

        /// <summary>moving: 움직이는 중이면 방출 / boosted: 가속 중이면 강하게·길게. 꺼지면 바로 평소로.</summary>
        public void SetState(bool moving, bool boosted)
        {
            IsEmitting = moving;
            var emission = particles.emission;
            emission.rateOverTime = moving ? (boosted ? boostRate : normalRate) : 0f;
            var main = particles.main;
            main.startLifetime = boosted ? boostLifetime : normalLifetime;
        }
    }
}
