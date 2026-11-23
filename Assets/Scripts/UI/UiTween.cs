using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 연출 공통 (DOTween). 여러 화면이 같은 "튀어나오기 · 통통 튀기 · 게이지 따라가기"를 쓰므로 한 곳에 모은다.
    /// - 같은 대상에 다시 걸면 이전 트윈을 끝까지 보내고(Complete) 새로 시작 → 연속 호출에도 크기가 누적되지 않는다
    /// - SetLink: 오브젝트가 꺼지면 멈추고 파괴되면 정리 → 씬 재시작(재도전) 때 남은 트윈이 없다
    /// - unscaled: 레벨업 카드 · 결과 화면처럼 게임 시간이 멈춘 동안 움직여야 하는 연출은 실시간 기준
    /// 연출만 담당하고 표시 여부 · 값(테스트가 보는 상태)은 각 화면이 그대로 정한다.
    /// </summary>
    public static class UiTween
    {
        /// <summary>from 배율에서 1로 커지며 나타남 (살짝 지나쳤다 돌아오는 OutBack).</summary>
        public static Tween PopIn(Transform target, float from = 0.6f, float seconds = 0.25f, float delay = 0f, bool unscaled = true)
        {
            target.DOComplete();
            target.localScale = Vector3.one * from;
            return target.DOScale(1f, seconds).SetEase(Ease.OutBack).SetDelay(delay)
                .SetUpdate(unscaled).SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>제자리에서 한 번 통통 (크기 펀치).</summary>
        public static Tween Punch(Transform target, float amount = 0.2f, float seconds = 0.3f, bool unscaled = true)
        {
            target.DOComplete();
            target.localScale = Vector3.one;
            return target.DOPunchScale(Vector3.one * amount, seconds, 6, 0.6f)
                .SetUpdate(unscaled).SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>가로 게이지의 오른쪽 끝(anchorMax.x)을 ratio로 옮긴다. seconds가 0이면 즉시.</summary>
        public static Tween BarTo(RectTransform bar, float ratio, float seconds, float delay = 0f)
        {
            bar.DOKill();
            if (seconds <= 0f)
            {
                SetRight(bar, ratio);
                return null;
            }
            return DOTween.To(() => bar.anchorMax.x, x => SetRight(bar, x), ratio, seconds)
                .SetEase(Ease.OutCubic).SetDelay(delay).SetTarget(bar)
                .SetUpdate(true).SetLink(bar.gameObject, LinkBehaviour.KillOnDestroy);
        }

        static void SetRight(RectTransform bar, float x)
        {
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(x, 1f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
        }
    }
}
