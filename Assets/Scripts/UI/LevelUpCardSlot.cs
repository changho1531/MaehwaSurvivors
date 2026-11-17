using System.Collections.Generic;
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
        [Tooltip("스킬 아이콘 (7-J절, 슬롯과 같은 그림). 비어 있거나 무기에 아이콘이 없으면 숨김")]
        [SerializeField] Image iconImage;

        readonly List<string> lines = new();

        public Button Button => button;
        public string NameLabel => nameText.text;
        public string KindLabel => kindText.text;
        public string DescriptionLabel => descriptionText.text;
        public bool IsIconShown => iconImage != null && iconImage.enabled;

        public void Show(LevelUpCard card)
        {
            nameText.text = card.Weapon.displayName;
            if (iconImage != null)
            {
                iconImage.sprite = card.Weapon.icon;
                iconImage.enabled = card.Weapon.icon != null;
            }
            // 신규: 한 줄 설명 / 강화: 수치표에서 계산한 변화 목록 (7-D절)
            if (card.IsNew)
            {
                kindText.text = "신규";
                descriptionText.text = card.Weapon.description;
            }
            else
            {
                kindText.text = $"강화  Lv {card.NextLevel - 1} → {card.NextLevel}";
                card.Weapon.DescribeUpgrade(card.NextLevel - 1, lines);
                descriptionText.text = string.Join("\n", lines);
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
