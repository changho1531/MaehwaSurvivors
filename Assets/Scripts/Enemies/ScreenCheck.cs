using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 화면 안에 있는지 (8-B 규칙 3, 8-C 규칙 7). 보이지 않는 곳에서 맞는 불공정을 막기 위해
    /// 적의 특수 공격은 화면 안에 있을 때만 시작한다. 카메라가 없으면(테스트 등) 항상 화면 안으로 본다.
    /// </summary>
    public static class ScreenCheck
    {
        public static bool IsVisible(Vector2 worldPosition, float margin = 0f)
        {
            var cam = Camera.main;
            if (cam == null)
                return true;
            var v = cam.WorldToViewportPoint(worldPosition);
            return v.x >= -margin && v.x <= 1f + margin && v.y >= -margin && v.y <= 1f + margin;
        }
    }
}
