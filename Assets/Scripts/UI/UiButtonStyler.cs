using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>VisualTheme 기반 공통 버튼 스타일.</summary>
    public static class UiButtonStyler
    {
        public const float MenuActionMaxWidth = 720f;
        public const float MenuPrimaryHeight = 100f;
        public const float MenuSecondaryHeight = 88f;
        public const float MenuDifficultyHeight = 72f;
        public const float OverlayActionWidth = 640f;
        public const float ResultButtonHeight = 100f;
        public const float ResultButtonGroupWidth = 640f;
        public const float SlicePixelsPerUnitMultiplier = 1.15f;

        public static void ApplyStandardTheme(Button button)
        {
            if (button == null)
            {
                return;
            }

            VisualTheme theme = VisualThemeLocator.Load();
            Image image = button.GetComponent<Image>();
            if (theme == null || image == null || theme.ButtonNormal == null)
            {
                return;
            }

            image.sprite = theme.ButtonNormal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SlicePixelsPerUnitMultiplier;
            image.fillCenter = true;
            image.color = Color.white;
            button.transition = Selectable.Transition.SpriteSwap;

            SpriteState state = button.spriteState;
            state.highlightedSprite = theme.ButtonNormal;
            state.pressedSprite = theme.ButtonPressed != null ? theme.ButtonPressed : theme.ButtonNormal;
            state.disabledSprite = theme.ButtonDisabled != null ? theme.ButtonDisabled : theme.ButtonNormal;
            button.spriteState = state;
        }

        public static void ApplySlicedPanel(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SlicePixelsPerUnitMultiplier;
            image.fillCenter = true;
            image.color = Color.white;
        }

        public static void CapMenuWidth(LayoutElement layout, float maxWidth = MenuActionMaxWidth)
        {
            if (layout == null)
            {
                return;
            }

            layout.preferredWidth = maxWidth;
            layout.minWidth = 0f;
            layout.flexibleWidth = 0f;
        }

        public static void EnsureLayoutElement(Button button, float preferredHeight = 120f, float preferredWidth = -1f)
        {
            if (button == null)
            {
                return;
            }

            float flex = preferredWidth >= 0f ? 0f : 1f;
            UiLayoutUtility.EnsureLayoutElement(button.gameObject, preferredHeight, flex, preferredWidth);
            UiLayoutUtility.ResetForVerticalLayout(button.GetComponent<RectTransform>(), preferredHeight);
        }

        public static Image EnsureAdIcon(Button button, VisualTheme theme = null)
        {
            if (button == null)
            {
                return null;
            }

            theme ??= VisualThemeLocator.Load();
            Transform existing = button.transform.Find("AdIcon");
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var iconGo = new GameObject("AdIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);

            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(28f, 0f);
            iconRect.sizeDelta = new Vector2(56f, 56f);

            Image icon = iconGo.GetComponent<Image>();
            if (theme?.IconAd != null)
            {
                icon.sprite = theme.IconAd;
                icon.preserveAspect = true;
                icon.color = Color.white;
            }

            icon.raycastTarget = false;
            return icon;
        }

        public static void OffsetButtonLabel(Button button, float leftPadding = 88f)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            RectTransform textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(leftPadding, 0f);
            textRect.offsetMax = Vector2.zero;
        }
    }
}
