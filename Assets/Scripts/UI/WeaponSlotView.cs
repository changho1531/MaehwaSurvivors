using Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// HUD 무기 슬롯 한 칸 (설계서 7-J절). 아이콘(없으면 첫 글자), 레벨 글자, 원형 쿨타임 덮개, 테두리 게이지 · 색, 반짝임.
    /// 그리기만 한다: 무엇을 그릴지는 WeaponSlotBar가 무기의 WeaponSlotState로 알려 준다.
    /// 칸의 부품은 코드로 만든다 — 6칸이 모두 같은 모양이라 씬에 6벌을 손으로 두지 않는다.
    /// </summary>
    public class WeaponSlotView : MonoBehaviour
    {
        // ※ 색은 7-J 배치표 기준 예시값
        static readonly Color BackgroundColor = new(0f, 0f, 0f, 0.45f);
        static readonly Color EmptyBorderColor = new(0.55f, 0.55f, 0.55f, 0.8f);
        static readonly Color FilledBorderColor = new(0.85f, 0.85f, 0.85f, 0.9f);
        static readonly Color ActiveColor = new(0.788f, 0.635f, 0.294f, 1f); // 금색 #C9A24B
        static readonly Color HighlightColor = new(0.788f, 0.482f, 0.580f, 1f); // 매화분홍 #C97B94
        static readonly Color CoverColor = new(0f, 0f, 0f, 0.65f);

        Image icon;
        Text letter;
        Image cover;
        Image border;
        Image gauge;
        Text levelText;
        Image flash;
        float flashRemaining;
        float flashSeconds = 0.3f;

        public Weapon Weapon { get; private set; }
        public bool IsEmpty => Weapon == null;
        public float CoverAmount => cover.enabled ? cover.fillAmount : 0f;
        public bool IsGaugeShown => gauge.enabled;
        public float GaugeAmount => gauge.enabled ? gauge.fillAmount : 0f;
        public bool IsHighlighted => gauge.enabled && gauge.color == HighlightColor;
        public string LevelLabel => levelText.text;
        public string LetterLabel => letter.enabled ? letter.text : "";
        public bool HasIcon => icon.enabled;
        public bool IsFlashing => flashRemaining > 0f;

        public void Build(float size, Font font, float flashDuration)
        {
            flashSeconds = flashDuration;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(size, size);

            CreateImage("Background", UiSprites.Solid, BackgroundColor, 0f);
            icon = CreateImage("Icon", null, Color.white, size * 0.06f);
            icon.preserveAspect = true;
            letter = CreateText("Letter", font, Mathf.RoundToInt(size * 0.5f), TextAnchor.MiddleCenter, Color.white);

            // 12시 방향부터 시계 방향으로 걷힌다 = 남은 덮개는 12시부터 반시계로 (1 − 진행률)만큼
            cover = CreateImage("Cover", UiSprites.Solid, CoverColor, 0f);
            cover.type = Image.Type.Filled;
            cover.fillMethod = Image.FillMethod.Radial360;
            cover.fillOrigin = (int)Image.Origin360.Top;
            cover.fillClockwise = false;

            border = CreateImage("Border", UiSprites.Outline, EmptyBorderColor, 0f);
            // 지속 중 게이지: 테두리를 따라 12시부터 남은 비율만큼
            gauge = CreateImage("Gauge", UiSprites.Outline, ActiveColor, -2f);
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Radial360;
            gauge.fillOrigin = (int)Image.Origin360.Top;
            gauge.fillClockwise = true;

            levelText = CreateText("Level", font, Mathf.RoundToInt(size * 0.27f), TextAnchor.LowerRight, Color.white);
            levelText.rectTransform.offsetMin = new Vector2(2f, 1f);
            levelText.rectTransform.offsetMax = new Vector2(-3f, -1f);
            levelText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            flash = CreateImage("Flash", UiSprites.Solid, new Color(1f, 1f, 1f, 0f), 0f);
            SetEmpty();
        }

        /// <summary>빈 칸: 회색 테두리만, 반투명 어두운 바탕</summary>
        public void SetEmpty()
        {
            Weapon = null;
            icon.enabled = false;
            letter.enabled = false;
            cover.enabled = false;
            gauge.enabled = false;
            levelText.text = "";
            border.color = EmptyBorderColor;
        }

        public void Assign(Weapon weapon)
        {
            Weapon = weapon;
            var data = weapon.Data;
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
            // 아이콘이 비어 있으면 스킬 이름 첫 글자 (7-J 예외)
            letter.text = data.SlotLetter;
            letter.enabled = data.icon == null;
            border.color = FilledBorderColor;
            RefreshLevel();
            Flash();
        }

        public void RefreshLevel()
        {
            if (Weapon == null)
                return;
            levelText.text = Weapon.Level >= Weapon.Data.maxLevel ? "MAX" : Weapon.Level.ToString();
        }

        public void Flash()
        {
            flashRemaining = flashSeconds;
            UiTween.Punch(transform, 0.18f, flashSeconds, unscaled: false); // 얻거나 강화한 칸이 통통 (게임 시간 — 카드 선택 뒤 재개될 때 보인다)
        }

        /// <summary>매 프레임. dt는 게임 시간이라 일시정지 · 카드 선택 중에는 반짝임도 멈춘다 (규칙 6).</summary>
        public void Refresh(in WeaponSlotState state, float dt)
        {
            if (Weapon == null)
                return;

            float coverAmount = state.CooldownCover;
            cover.enabled = coverAmount > 0.001f;
            cover.fillAmount = coverAmount;

            if (state.IsActive)
            {
                gauge.enabled = true;
                gauge.color = ActiveColor;
                gauge.fillAmount = state.ActiveRemaining;
            }
            else if (state.IsHighlighted)
            {
                gauge.enabled = true;
                gauge.color = HighlightColor;
                gauge.fillAmount = 1f;
            }
            else
                gauge.enabled = false;

            if (flashRemaining > 0f)
                flashRemaining = Mathf.Max(0f, flashRemaining - dt);
            var c = flash.color;
            c.a = flashSeconds > 0f ? 0.7f * flashRemaining / flashSeconds : 0f;
            flash.color = c;
        }

        Image CreateImage(string name, Sprite sprite, Color color, float inset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            Stretch(rect, inset);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text CreateText(string name, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            Stretch(rect, 0f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
