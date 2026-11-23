using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>
    /// 플레이어를 화면 중앙에 고정하도록 카메라가 따라간다 (설계서 2절).
    /// 연출(CameraFx)이 있으면 그 Offset(흔들림 · 초점 이동)만 더한다 — 따라가기 자체는 바뀌지 않는다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("(선택) 카메라 연출. 비우면 같은 오브젝트에서 찾는다")]
        [SerializeField] CameraFx fx;

        void Awake()
        {
            if (fx == null)
                fx = GetComponent<CameraFx>();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            var p = target.position;
            var offset = fx != null ? fx.Offset : Vector3.zero;
            transform.position = new Vector3(p.x + offset.x, p.y + offset.y, transform.position.z);
        }
    }
}
