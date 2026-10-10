using UnityEngine;

namespace Game.Enemies
{
    /// <summary>근거리 적: 대상(플레이어)을 향해 직선으로 계속 추적한다.</summary>
    public class ChaseBehaviour : EnemyBehaviour
    {
        public override void Tick(Transform target, float dt)
        {
            var toTarget = (Vector2)target.position - Body.position;
            Body.linearVelocity = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized * Owner.Data.moveSpeed : Vector2.zero;
        }
    }
}
