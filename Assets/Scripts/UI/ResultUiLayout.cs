using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>결과 화면 헤더·통계 스크롤·하단 버튼 정렬.</summary>
    public static class ResultUiLayout
    {
        private const float ButtonHeight = UiButtonStyler.ResultButtonHeight;
        private const float ButtonSpacing = 20f;
        private const float BottomMargin = 120f;

        public static void ApplyContent(Text titleLabel, Text messageLabel, Text statsLabel)
        {
            if (titleLabel != null)
            {
                PinFromTop(titleLabel.rectTransform, -132f, 64f, 920f);
                titleLabel.alignment = TextAnchor.MiddleCenter;
                titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                titleLabel.verticalOverflow = VerticalWrapMode.Truncate;
                titleLabel.raycastTarget = false;
                titleLabel.resizeTextForBestFit = true;
                titleLabel.resizeTextMinSize = 28;
                titleLabel.resizeTextMaxSize = 48;
            }

            if (messageLabel != null)
            {
                PinFromTop(messageLabel.rectTransform, -204f, 52f, 920f);
                messageLabel.alignment = TextAnchor.MiddleCenter;
                messageLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageLabel.verticalOverflow = VerticalWrapMode.Truncate;
                messageLabel.raycastTarget = false;
                messageLabel.fontSize = 26;
            }

            if (statsLabel != null)
            {
                EnsureStatsScroll(statsLabel);
            }
        }

        public static void EnsureButtonGroup(Button retryButton, Button doubleRewardButton, Button mainMenuButton)
        {
            if (retryButton == null || mainMenuButton == null)
            {
                return;
            }

            Transform parent = retryButton.transform.parent;
            if (parent == null)
            {
                return;
            }

            float bottomMargin = BottomMargin;
            Rect safe = Screen.safeArea;
            if (Screen.height > 0 && safe.height > 0)
            {
                float bottomInset = safe.yMin;
                float scale = 1920f / Mathf.Max(1, Screen.height);
                bottomMargin = Mathf.Max(BottomMargin, 48f + (bottomInset * scale));
            }

            Transform group = parent.Find("ResultButtonGroup");
            if (group == null)
            {
                var groupGo = new GameObject("ResultButtonGroup", typeof(RectTransform));
                group = groupGo.transform;
                group.SetParent(parent, false);

                RectTransform groupRect = groupGo.GetComponent<RectTransform>();
                groupRect.anchorMin = new Vector2(0.5f, 0f);
                groupRect.anchorMax = new Vector2(0.5f, 0f);
                groupRect.pivot = new Vector2(0.5f, 0f);
                groupRect.anchoredPosition = new Vector2(0f, bottomMargin);
                groupRect.sizeDelta = new Vector2(UiButtonStyler.ResultButtonGroupWidth, 0f);

                VerticalLayoutGroup layout = groupGo.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = ButtonSpacing;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = groupGo.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else if (group is RectTransform existingGroup)
            {
                existingGroup.anchoredPosition = new Vector2(0f, bottomMargin);
                existingGroup.sizeDelta = new Vector2(UiButtonStyler.ResultButtonGroupWidth, existingGroup.sizeDelta.y);
            }

            retryButton.transform.SetParent(group, false);
            if (doubleRewardButton != null)
            {
                doubleRewardButton.transform.SetParent(group, false);
            }

            mainMenuButton.transform.SetParent(group, false);

            UiButtonStyler.EnsureLayoutElement(retryButton, ButtonHeight, UiButtonStyler.ResultButtonGroupWidth);
            if (doubleRewardButton != null)
            {
                UiButtonStyler.EnsureLayoutElement(doubleRewardButton, ButtonHeight, UiButtonStyler.ResultButtonGroupWidth);
            }

            UiButtonStyler.EnsureLayoutElement(mainMenuButton, ButtonHeight, UiButtonStyler.ResultButtonGroupWidth);

            UiButtonStyler.ApplyStandardTheme(retryButton);
            UiButtonStyler.ApplyStandardTheme(mainMenuButton);
            if (doubleRewardButton != null)
            {
                UiButtonStyler.ApplyStandardTheme(doubleRewardButton);
                UiButtonStyler.EnsureAdIcon(doubleRewardButton);
                UiButtonStyler.OffsetButtonLabel(doubleRewardButton);
            }
        }

        private static void PinFromTop(RectTransform rect, float yFromTop, float height, float width)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, yFromTop);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void EnsureStatsScroll(Text stats)
        {
            RectTransform statsRect = stats.rectTransform;
            Transform parent = statsRect.parent;
            if (parent == null)
            {
                return;
            }

            Transform scrollTransform = parent.Find("StatsScroll");
            RectTransform scrollRectTransform;
            if (scrollTransform == null)
            {
                var scrollGo = new GameObject("StatsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollRectTransform = scrollGo.GetComponent<RectTransform>();
                scrollRectTransform.SetParent(parent, false);
                Image bg = scrollGo.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.22f);
                bg.raycastTarget = true;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                RectTransform viewport = viewportGo.GetComponent<RectTransform>();
                viewport.SetParent(scrollRectTransform, false);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = new Vector2(16f, 12f);
                viewport.offsetMax = new Vector2(-16f, -12f);

                statsRect.SetParent(viewport, false);
                ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = statsRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
            }
            else
            {
                scrollRectTransform = scrollTransform as RectTransform;
                if (statsRect.parent == null || statsRect.parent.name != "Viewport")
                {
                    Transform viewport = scrollTransform.Find("Viewport");
                    if (viewport != null)
                    {
                        statsRect.SetParent(viewport, false);
                    }
                }
            }

            if (scrollRectTransform != null)
            {
                scrollRectTransform.anchorMin = new Vector2(0.5f, 0f);
                scrollRectTransform.anchorMax = new Vector2(0.5f, 1f);
                scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
                scrollRectTransform.offsetMin = new Vector2(-430f, 560f);
                scrollRectTransform.offsetMax = new Vector2(430f, -268f);
                Image bg = scrollRectTransform.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = MenuOverlayUi.OverlayFill;
                }
            }

            stats.alignment = TextAnchor.UpperLeft;
            stats.horizontalOverflow = HorizontalWrapMode.Wrap;
            stats.verticalOverflow = VerticalWrapMode.Overflow;
            stats.lineSpacing = 1.15f;
            stats.raycastTarget = false;
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta = new Vector2(0f, 8f);

            ContentSizeFitter fitter = stats.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = stats.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(statsRect);
        }
    }
}
