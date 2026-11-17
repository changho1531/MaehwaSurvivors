using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적을 꺼내 주는 쪽 (스포너). 저주술사 소환처럼 적이 다른 적을 부를 때, 적은 스포너 클래스를 몰라도
    /// 이 인터페이스로 풀 · 동시 활성 상한 · 웨이브 속도 규칙을 그대로 따르는 생성을 요청한다.
    /// </summary>
    public interface IEnemySource
    {
        /// <summary>center 주변 radius 안에 prefab 1마리. 동시 활성 상한에 닿으면 null.</summary>
        Enemy SpawnNear(Enemy prefab, Vector2 center, float radius);
    }
}
