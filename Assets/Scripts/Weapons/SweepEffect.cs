using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 화산십이검 임시 이펙트: 판정 범위와 같은 부채꼴 메시. 무기 데이터의 반경·각도로 만들어
    /// "보이는 범위 = 맞는 범위"가 되게 한다. 타격마다 잠깐 밝아진다. (최종 아트는 스프라이트 이펙트로 교체)
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SweepEffect : MonoBehaviour
    {
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

        /// <summary>+X 방향을 중심으로 한 부채꼴 메시 생성.</summary>
        public void Build(float radius, float angle)
        {
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
