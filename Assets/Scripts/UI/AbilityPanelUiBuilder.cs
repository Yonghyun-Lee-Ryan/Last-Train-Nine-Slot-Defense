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
            var panelRoot = new GameObject("AbilityPanel", typeof(RectTransform), typeof(AbilityPanelController));
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            StretchFull(panelRect);

            Text owned = CreateText(panelRoot.transform, "AbilityOwnedListLabel", "보유 능력: 없음", 22, Vector2.zero, new Vector2(1000f, 40f));
            var ownedRect = owned.GetComponent<RectTransform>();
            ownedRect.anchorMin = new Vector2(0.5f, 0f);
            ownedRect.anchorMax = new Vector2(0.5f, 0f);
            ownedRect.pivot = new Vector2(0.5f, 0f);
            ownedRect.anchoredPosition = new Vector2(0f, 300f);

            var selectionOverlay = new GameObject("SelectionOverlay", typeof(RectTransform), typeof(Image));
            var overlayRect = selectionOverlay.GetComponent<RectTransform>();
            overlayRect.SetParent(panelRoot.transform, false);
            StretchFull(overlayRect);
            selectionOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            Text title = CreateText(selectionOverlay.transform, "TitleLabel", "능력 카드 선택", 42, new Vector2(0f, 420f), new Vector2(800f, 60f));
            Text status = CreateText(selectionOverlay.transform, "StatusLabel", "상태", 26, new Vector2(0f, 350f), new Vector2(900f, 40f));

            var offerButtons = new Button[3];
            var offerLabels = new Text[3];
            var offerDetails = new Text[3];
            float[] xs = { -320f, 0f, 320f };
            for (int i = 0; i < 3; i++)
            {
                offerButtons[i] = CreateButton(selectionOverlay.transform, $"Offer{i}", "능력", new Vector2(xs[i], 40f), new Vector2(280f, 280f));
                offerLabels[i] = offerButtons[i].GetComponentInChildren<Text>();
                offerDetails[i] = CreateText(offerButtons[i].transform, "Detail", "설명", 22, new Vector2(0f, -90f), new Vector2(240f, 80f));
            }

            Button freeReroll = CreateButton(selectionOverlay.transform, "FreeRerollButton", "무료 리롤", new Vector2(-180f, -220f), new Vector2(220f, 80f));
            Button adReroll = CreateButton(selectionOverlay.transform, "AdRerollButton", "광고 리롤", new Vector2(180f, -220f), new Vector2(220f, 80f));
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

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.42f, 1f);
            Text text = CreateText(go.transform, "Label", label, 26, new Vector2(0f, 40f), new Vector2(size.x - 20f, 80f));
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static void CenterLabel(Button button)
        {
            Text label = button.GetComponentInChildren<Text>();
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
            return text;
        }
    }
}
