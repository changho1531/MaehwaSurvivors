using Game.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>레벨업 카드 1장 UI. 내용을 그리기만 하고, 클릭 처리는 LevelUpCardView가 연결한다.</summary>
    public class LevelUpCardSlot : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text nameText;
        [SerializeField] Text kindText;
        [SerializeField] Text descriptionText;

        public Button Button => button;
        public string NameLabel => nameText.text;
        public string KindLabel => kindText.text;

        public void Show(LevelUpCard card)
        {
            nameText.text = card.Weapon.displayName;
            kindText.text = card.IsNew ? "신규 획득" : $"강화  Lv{card.NextLevel - 1} → Lv{card.NextLevel}";
            descriptionText.text = card.Weapon.description;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
