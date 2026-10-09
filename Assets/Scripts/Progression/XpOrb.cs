using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// 경험치 구슬 1개. 값과 색만 가지고 Update가 없다 — 스스로 아무것도 계산하지 않는다.
    /// 구슬이 몇 천 개 쌓여도 비용이 들지 않게, 탐지·이동은 플레이어 쪽(PlayerMagnet)이 반경 안 구슬만 처리한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class XpOrb : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        Action<XpOrb> releaseToPool;

        public int Value { get; private set; }

        /// <summary>PlayerMagnet이 끌어당기는 중인지. 같은 구슬을 두 번 등록하지 않기 위한 표시.</summary>
        public bool IsPulled { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnDisable()
        {
            IsPulled = false;
            releaseToPool = null;
        }

        public void Init(int value, Color color, Action<XpOrb> release)
        {
            Value = value;
            spriteRenderer.color = color;
            releaseToPool = release;
            IsPulled = false;
        }

        public void BeginPull()
        {
            IsPulled = true;
        }

        /// <summary>습득 또는 일괄 제거 시 풀로 돌아간다.</summary>
        public void Collect()
        {
            var release = releaseToPool;
            if (release != null)
                release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
