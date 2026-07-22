#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Ads
{
    /// <summary>Play Mode Mock 광고 팝업. 성공/취소/실패를 수동 선택한다.</summary>
    public static class MockAdPopup
    {
        private static GameObject _active;

        public static void Show(AdRequest request, Action<AdResult> onFinished)
        {
            Close();

            var root = new GameObject("MockAdPopup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            GameObject panel = CreateUi(root.transform, "Panel", new Color(0f, 0f, 0f, 0.72f));
            Stretch(panel.GetComponent<RectTransform>());

            GameObject box = CreateUi(panel.transform, "Box", new Color(0.12f, 0.16f, 0.22f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(720, 420);

            Text title = CreateText(box.transform, "Title", $"Mock Ad\n{request.Placement}", 36);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchoredPosition = new Vector2(0f, 120f);
            titleRect.sizeDelta = new Vector2(680, 120);

            CreateButton(box.transform, "Complete", "완료(보상)", new Vector2(0f, 20f), () =>
            {
                Finish(AdResult.Completed, onFinished);
            });
            CreateButton(box.transform, "Cancel", "취소", new Vector2(0f, -80f), () =>
            {
                Finish(AdResult.Cancelled, onFinished);
            });
            CreateButton(box.transform, "Fail", "실패", new Vector2(0f, -180f), () =>
            {
                Finish(AdResult.Failed, onFinished);
            });

            _active = root;
        }

        private static void Finish(AdResult result, Action<AdResult> onFinished)
        {
            Close();
            onFinished?.Invoke(result);
        }

        private static void Close()
        {
            if (_active == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(_active);
            _active = null;
        }

        private static GameObject CreateUi(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            return text;
        }

        private static void CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPos,
            Action onClick)
        {
            GameObject go = CreateUi(parent, name, new Color(0.2f, 0.55f, 0.75f, 1f));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(420, 80);

            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(go.transform, "Label", label, 32);
            Stretch(text.rectTransform);
        }
    }
}
#endif
