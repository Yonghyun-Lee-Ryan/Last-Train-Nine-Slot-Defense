using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>결과 화면 하단 버튼 정렬.</summary>
    public static class ResultUiLayout
    {
        private const float ButtonHeight = 120f;
        private const float ButtonSpacing = 24f;
        private const float BottomMargin = 120f;

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
                groupRect.sizeDelta = new Vector2(600f, 0f);

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
            }

            retryButton.transform.SetParent(group, false);
            if (doubleRewardButton != null)
            {
                doubleRewardButton.transform.SetParent(group, false);
            }

            mainMenuButton.transform.SetParent(group, false);

            UiButtonStyler.EnsureLayoutElement(retryButton, ButtonHeight);
            if (doubleRewardButton != null)
            {
                UiButtonStyler.EnsureLayoutElement(doubleRewardButton, ButtonHeight);
            }

            UiButtonStyler.EnsureLayoutElement(mainMenuButton, ButtonHeight);

            UiButtonStyler.ApplyStandardTheme(retryButton);
            UiButtonStyler.ApplyStandardTheme(mainMenuButton);
            if (doubleRewardButton != null)
            {
                UiButtonStyler.ApplyStandardTheme(doubleRewardButton);
                UiButtonStyler.EnsureAdIcon(doubleRewardButton);
                UiButtonStyler.OffsetButtonLabel(doubleRewardButton);
            }
        }
    }
}
