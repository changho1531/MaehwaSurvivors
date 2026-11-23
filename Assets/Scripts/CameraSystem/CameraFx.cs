using DG.Tweening;
using UnityEngine;

namespace Game.CameraSystem
{
    /// <summary>
    /// 카메라 연출 (DOTween): 흔들림 · 초점 이동 · 확대. 위치는 CameraFollow가 "플레이어 위치 + Offset"으로 정하고,
    /// 여기서는 더해 줄 값(Offset)과 화면 크기만 바꾼다 — 따라가기 규칙(플레이어 중앙)과 연출이 섞이지 않는다.
    /// 누가 부르는지는 모른다 (보스 연출 등이 이벤트를 받아 호출).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFx : MonoBehaviour
    {
        Camera cam;
        float baseSize;
        Vector3 shake;
        Vector3 focus;

        /// <summary>CameraFollow가 매 프레임 더하는 값</summary>
        public Vector3 Offset => shake + focus;
        public bool IsShaking => DOTween.IsTweening(ShakeId);
        public float BaseSize => baseSize;

        object ShakeId => this;
        string FocusId => nameof(focus) + GetInstanceID();

        void Awake()
        {
            cam = GetComponent<Camera>();
            baseSize = cam.orthographicSize;
        }

        void OnDestroy() => DOTween.Kill(ShakeId); // 씬 재시작 대비 (나머지는 SetLink로 정리)

        /// <summary>화면 흔들림 (월드 단위 세기). 이미 흔들리는 중이면 새로 시작.</summary>
        public void Shake(float strength, float seconds, bool unscaled = false)
        {
            DOTween.Kill(ShakeId);
            DOTween.Shake(() => shake, v => shake = v, seconds, strength, 18, 90f, true, true)
                .SetId(ShakeId).SetUpdate(unscaled).SetLink(gameObject)
                .OnKill(() => shake = Vector3.zero);
        }

        /// <summary>
        /// 플레이어 기준 offset 쪽으로 초점을 옮기며 sizeFactor배로 확대 (1보다 작으면 확대). 되돌리지 않는다 — 끝나는 장면(보스 처치)용.
        /// </summary>
        public void FocusAndZoom(Vector2 offset, float sizeFactor, float seconds, bool unscaled = true)
        {
            DOTween.Kill(FocusId);
            DOTween.To(() => focus, v => focus = v, (Vector3)offset, seconds).SetEase(Ease.OutCubic)
                .SetId(FocusId).SetUpdate(unscaled).SetLink(gameObject);
            cam.DOKill();
            cam.DOOrthoSize(baseSize * sizeFactor, seconds).SetEase(Ease.OutCubic).SetUpdate(unscaled).SetLink(gameObject);
        }

        /// <summary>잠깐 sizeFactor배로 당겼다가 원래대로 (페이즈 전환처럼 "기운이 모이는" 순간).</summary>
        public void ZoomPulse(float sizeFactor, float inSeconds, float outSeconds, bool unscaled = false)
        {
            cam.DOKill();
            cam.orthographicSize = baseSize;
            DOTween.Sequence()
                .Append(cam.DOOrthoSize(baseSize * sizeFactor, inSeconds).SetEase(Ease.OutQuad))
                .Append(cam.DOOrthoSize(baseSize, outSeconds).SetEase(Ease.InOutQuad))
                .SetTarget(cam).SetUpdate(unscaled).SetLink(gameObject);
        }
    }
}
