using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 공용 확인창 (설계서 7-C절): 문구 + [확인]/[취소]. 일시정지의 타이틀 이동, 타이틀의 나가기에서 같은 프리팹을 재사용한다.
    /// 무엇을 확인하는지는 호출하는 쪽이 문구와 콜백으로 넘기므로, 확인창은 쓰임새를 모른다.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text messageText;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        Action onConfirm;
        Action onCancel;

        public bool IsOpen => panel.activeSelf;
        public Text MessageText => messageText;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Cancel);
            panel.SetActive(false);
        }

        public void Show(string message, Action confirm, Action cancel = null)
        {
            messageText.text = message;
            onConfirm = confirm;
            onCancel = cancel;
            panel.SetActive(true);
        }

        public void Confirm() => Close(onConfirm);

        /// <summary>[취소] 버튼, 또는 확인창이 떠 있을 때 ESC (7-C 예외).</summary>
        public void Cancel() => Close(onCancel);

        void Close(Action callback)
        {
            if (!IsOpen)
                return; // 연타로 두 번 실행되지 않게
            panel.SetActive(false);
            onConfirm = null;
            onCancel = null;
            callback?.Invoke();
        }
    }
}
