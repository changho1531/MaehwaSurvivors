using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 화산십이검 이펙트. 스킬 아트(검들이 모이는 지점 = 플레이어 위치)를 콤보 지속 시간 동안 한 번 재생하고,
    /// 부채꼴 길이가 판정 반경과 같도록 스케일한다 (7-D절 3).
    /// 판정과 같은 크기의 부채꼴 메시는 "보이는 범위 = 맞는 범위" 확인용으로 남겨 두고 showHitArea로 켠다. 타격마다 잠깐 밝아진다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SweepEffect : MonoBehaviour
    {
        [Header("스킬 아트")]
        [SerializeField] FrameAnimation art;
        [Tooltip("아트 스케일 1일 때 그림 속 부채꼴 길이(월드 단위). 판정 반경에 맞춰 스케일한다")]
        [SerializeField, Min(0.01f)] float artRadiusAtScale1 = 3f;
        [Tooltip("판정 확인용 부채꼴 메시 표시")]
        [SerializeField] bool showHitArea;

        [Header("판정 확인용 부채꼴")]
        [SerializeField] Color baseColor = new(0.788f, 0.482f, 0.580f, 0.25f); // 매화분홍 #C97B94
        [SerializeField] Color flashColor = new(1f, 1f, 1f, 0.55f);
        [SerializeField, Min(0.01f)] float flashSeconds = 0.06f;
        [SerializeField] int sortingOrder = 15;
        [SerializeField, Min(3)] int segments = 24;

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        float flashRemaining;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = sortingOrder;
            block = new MaterialPropertyBlock();
            ApplyColor(baseColor);
        }

        public FrameAnimation Art => art;

        /// <summary>+X 방향을 중심으로 한 부채꼴 메시 생성 + 아트 크기·재생 시간을 판정 반경·콤보 시간에 맞춤.</summary>
        public void Build(float radius, float angle, float comboDuration)
        {
            meshRenderer.enabled = showHitArea || art == null;
            if (art != null)
            {
                art.transform.localScale = Vector3.one * (radius / artRadiusAtScale1);
                art.SetDuration(comboDuration);
            }

            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            float half = angle * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)segments);
                vertices[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { name = "SweepFan", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void Face(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Flash()
        {
            flashRemaining = flashSeconds;
            ApplyColor(flashColor);
        }

        void Update()
        {
            if (flashRemaining <= 0f)
                return;
            flashRemaining -= Time.deltaTime;
            if (flashRemaining <= 0f)
                ApplyColor(baseColor);
        }

        void ApplyColor(Color color)
        {
            if (meshRenderer == null)
                return;
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
