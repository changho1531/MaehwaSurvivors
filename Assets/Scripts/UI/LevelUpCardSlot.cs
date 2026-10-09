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

        readonly List<string> lines = new();

        public Button Button => button;
        public string NameLabel => nameText.text;
        public string KindLabel => kindText.text;
        public string DescriptionLabel => descriptionText.text;

        public void Show(LevelUpCard card)
        {
            nameText.text = card.Weapon.displayName;
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
