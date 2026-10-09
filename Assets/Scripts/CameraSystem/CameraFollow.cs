using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>플레이어를 화면 중앙에 고정하도록 카메라가 따라간다 (설계서 2절).</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;

        void LateUpdate()
        {
            if (target == null)
                return;

            var p = target.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
