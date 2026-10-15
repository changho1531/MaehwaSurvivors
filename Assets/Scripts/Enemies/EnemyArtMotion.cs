using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 정지 그림 1장뿐인 적을 걷는 것처럼 보이게 한다 (설계서 7-D절 4): 이동 방향으로 좌우 반전 + 걸을 때 위아래로 통통.
    /// 그림(자식)만 움직이고 몸통(물리·판정)은 건드리지 않는다. 궁수의 떨림(가로)과는 축이 달라 겹쳐도 된다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyArtMotion : MonoBehaviour
    {
        [SerializeField] SpriteRenderer art;
        [Tooltip("원본 그림이 오른쪽을 보고 있으면 체크. 왼쪽으로 갈 때 반전한다")]
        [SerializeField] bool artFacesRight = true;
        [Tooltip("튀는 높이 (칸) ※")]
        [SerializeField, Min(0f)] float bobHeight = 0.06f;
        [Tooltip("초당 튀는 횟수 ※")]
        [SerializeField, Min(0f)] float bobsPerSecond = 4f;
        [Tooltip("이 속도보다 느리면 멈춘 것으로 본다 (반전·튀기 안 함)")]
        [SerializeField, Min(0f)] float moveThreshold = 0.1f;

        Rigidbody2D body;
        float walkTime;

        void Awake() => body = GetComponent<Rigidbody2D>();

        void OnDisable()
        {
            walkTime = 0f;
            SetBob(0f);
        }

        void Update()
        {
            if (art == null)
                return;
            var v = body.linearVelocity;
            bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;
            walkTime = moving ? walkTime + Time.deltaTime : 0f;
            SetBob(BobOffset(walkTime, bobHeight, bobsPerSecond));
            // 세로로만 움직일 때는 직전 방향 유지 (매 프레임 뒤집혀 깜빡이지 않게)
            if (Mathf.Abs(v.x) > moveThreshold)
                art.flipX = ShouldFlip(v.x, artFacesRight);
        }

        /// <summary>|sin| 곡선: 바닥(0)에서 튀어 올랐다 내려오기를 반복. 걷기 시작 순간은 0이라 튀지 않는다.</summary>
        public static float BobOffset(float walkTime, float height, float bobsPerSecond) =>
            Mathf.Abs(Mathf.Sin(walkTime * bobsPerSecond * Mathf.PI)) * height;

        public static bool ShouldFlip(float velocityX, bool artFacesRight) => artFacesRight ? velocityX < 0f : velocityX > 0f;

        void SetBob(float y)
        {
            if (art == null)
                return;
            var p = art.transform.localPosition;
            art.transform.localPosition = new Vector3(p.x, y, p.z);
        }
    }
}
