using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>런타임/에디터 공용 능력 카드 선택 UI 생성.</summary>
    public static class AbilityPanelUiBuilder
    {
        public static AbilityPanelController Build(Transform parent, GameDatabase database, GameBattleBootstrap bootstrap)
        {
            VisualTheme theme = VisualThemeLocator.Load();

            var panelRoot = new GameObject("AbilityPanel", typeof(RectTransform), typeof(AbilityPanelController));
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            StretchFull(panelRect);

            // 전투 중에도 보이도록 SelectionOverlay 밖에 두고, 유닛 팝업과 겹치지 않게 상단 배치.
            Text owned = CreateText(
                panelRoot.transform,
                "AbilityOwnedListLabel",
                "보유 능력: 없음",
                22,
                Vector2.zero,
                new Vector2(1000f, 40f));
            owned.raycastTarget = false;
            PlaceOwnedHudLabel(owned.rectTransform);

            var selectionOverlay = new GameObject("SelectionOverlay", typeof(RectTransform), typeof(Image));
            var overlayRect = selectionOverlay.GetComponent<RectTransform>();
            overlayRect.SetParent(panelRoot.transform, false);
            StretchFull(overlayRect);
            Image overlayImage = selectionOverlay.GetComponent<Image>();
            if (theme?.PopupDim != null)
            {
                overlayImage.sprite = theme.PopupDim;
                overlayImage.type = Image.Type.Sliced;
                overlayImage.color = Color.white;
            }
            else
            {
                overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
            }

            Text title = CreateText(selectionOverlay.transform, "TitleLabel", "능력 카드 선택", 42, new Vector2(0f, 420f), new Vector2(800f, 60f));
            if (theme?.IconAbility != null)
            {
                AttachIcon(title.transform, theme.IconAbility, new Vector2(-360f, 0f));
            }

            Text status = CreateText(selectionOverlay.transform, "StatusLabel", "상태", 26, new Vector2(0f, 350f), new Vector2(900f, 40f));

            var offerButtons = new Button[3];
            var offerLabels = new Text[3];
            var offerDetails = new Text[3];
            float[] xs = { -320f, 0f, 320f };
            for (int i = 0; i < 3; i++)
            {
                offerButtons[i] = CreateCardButton(selectionOverlay.transform, $"Offer{i}", "능력", new Vector2(xs[i], 40f), new Vector2(280f, 280f), theme);
                offerLabels[i] = offerButtons[i].GetComponentInChildren<Text>();
                offerDetails[i] = CreateText(offerButtons[i].transform, "Detail", "설명", 22, new Vector2(0f, -90f), new Vector2(240f, 80f));
            }

            Button freeReroll = CreateThemedButton(selectionOverlay.transform, "FreeRerollButton", "무료 리롤", new Vector2(-180f, -220f), new Vector2(220f, 80f), theme);
            Button adReroll = CreateThemedButton(selectionOverlay.transform, "AdRerollButton", "광고 리롤", new Vector2(180f, -220f), new Vector2(220f, 80f), theme);
            if (theme?.IconReroll != null)
            {
                AttachIcon(freeReroll.transform, theme.IconReroll, new Vector2(-78f, 0f), 40f);
            }

            if (theme?.IconAd != null)
            {
                AttachIcon(adReroll.transform, theme.IconAd, new Vector2(-78f, 0f), 40f);
            }

            CenterLabel(freeReroll);
            CenterLabel(adReroll);

            var controller = panelRoot.GetComponent<AbilityPanelController>();
            controller.Configure(
                bootstrap,
                database,
                selectionOverlay,
                title,
                status,
                owned,
                offerButtons,
                offerLabels,
                offerDetails,
                freeReroll,
                adReroll);

            return controller;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void PlaceOwnedHudLabel(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            // 시너지 라벨 아래, 유닛 그리드/팝업 위쪽 — 전투 HUD로 항상 표시.
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -250f);
            rect.sizeDelta = new Vector2(1000f, 40f);
        }

        private static Button CreateCardButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, VisualTheme theme)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            if (theme?.CardFrame != null)
            {
                image.sprite = theme.CardFrame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.18f, 0.28f, 0.42f, 1f);
            }

            Text text = CreateText(go.transform, "Label", label, 26, new Vector2(0f, 40f), new Vector2(size.x - 20f, 80f));
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static Button CreateThemedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, VisualTheme theme)
        {
            Button button = CreateCardButton(parent, name, label, pos, size, null);
            UiButtonStyler.ApplyStandardTheme(button);
            return button;
        }

        private static void AttachIcon(Transform parent, Sprite sprite, Vector2 anchoredPos, float size = 48f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(size, size);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private static void CenterLabel(Button button)
        {
            Text label = button.GetComponentInChildren<Text>();
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(48f, 0f);
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GameFontProvider.Get();
            return text;
        }
    }
}
