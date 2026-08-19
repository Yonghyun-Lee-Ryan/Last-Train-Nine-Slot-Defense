using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 설정·동의 오버레이용 최소 UI 헬퍼.</summary>
    internal static class MenuOverlayUi
    {
        public static GameObject CreateRoot(string name, int sortingOrder = 4000)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        /// <summary>오버레이 Canvas 안에 SafeArea 호스트를 만들고 반환한다.</summary>
        public static RectTransform EnsureSafeAreaHost(Transform overlayRoot)
        {
            if (overlayRoot == null)
            {
                return null;
            }

            Transform existing = overlayRoot.Find("SafeArea");
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(overlayRoot, false);
            Stretch(rect);
            return rect;
        }

        /// <summary>
        /// 전체 화면 Dim(노치·홈 인디케이터 밖까지). SafeArea 밖에 두면 메뉴 버튼이 비치지 않는다.
        /// </summary>
        public static GameObject CreateFullScreenDim(
            Transform overlayRoot,
            Color color,
            System.Action onClick = null,
            bool placeBehindContent = true)
        {
            GameObject dim = CreatePanel(overlayRoot, "Dim", color);
            Stretch(dim.GetComponent<RectTransform>());
            if (placeBehindContent)
            {
                dim.transform.SetAsFirstSibling();
            }
            else
            {
                dim.transform.SetAsLastSibling();
            }

            if (onClick != null)
            {
                Button dimButton = dim.AddComponent<Button>();
                dimButton.transition = Selectable.Transition.None;
                dimButton.onClick.AddListener(() => onClick.Invoke());
            }

            return dim;
        }

        public static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Font font = GameFontProvider.Get();
            if (font != null)
            {
                text.font = font;
            }

            text.raycastTarget = false;
            return text;
        }

        public static bool IsOverlayOpen(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            GameObject go = GameObject.Find(name);
            return go != null && go.activeInHierarchy;
        }

        public static void DestroyRoot(GameObject root)
        {
            UiLayoutUtility.DestroyRoot(root);
        }

        public static void EnableClipping(GameObject panel)
        {
            if (panel != null && panel.GetComponent<RectMask2D>() == null)
            {
                panel.AddComponent<RectMask2D>();
            }
        }

        public static readonly Color OverlayFill = new Color(0.14f, 0.18f, 0.26f, 0.98f);
        public const float OverlayPad = 40f;
        public const float OverlayTitleHeight = 64f;
        public const float OverlayCloseHeight = 64f;
        public static readonly Vector2 OverlaySizeStandard = new Vector2(920f, 1320f);
        public static readonly Vector2 OverlaySizeTall = new Vector2(920f, 1480f);
        public static readonly Vector2 OverlaySizeCompact = new Vector2(920f, 1100f);

        public static GameObject CreateCenteredPanel(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject go = CreatePanel(parent, name, color);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return go;
        }

        public static GameObject CreateOverlayBox(Transform host, Vector2 size)
        {
            GameObject box = CreateCenteredPanel(host, "Box", size, OverlayFill);
            EnableClipping(box);
            return box;
        }

        public static Text CreateOverlayTitle(Transform box, string text, int fontSize = 36)
        {
            Text title = CreateText(box, "Title", text, fontSize, TextAnchor.MiddleCenter);
            title.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = title.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-(OverlayPad * 2f), OverlayTitleHeight);
            rect.anchoredPosition = new Vector2(0f, -OverlayPad);
            return title;
        }

        public static Button CreateOverlayClose(Transform box, System.Action onClose, float width = UiButtonStyler.OverlayActionWidth)
        {
            Button close = CreateLayoutButton(box, "Close", "닫기", OverlayCloseHeight, onClose, 30, width);
            UiButtonStyler.ApplyStandardTheme(close);
            RectTransform rect = close.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, OverlayCloseHeight);
            rect.anchoredPosition = new Vector2(0f, OverlayPad);
            LayoutElement layout = close.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.ignoreLayout = true;
            }

            return close;
        }

        public static OverlayScroll CreateOverlayScroll(Transform box, float extraTop = 0f, float extraBottom = 0f)
        {
            float bottom = OverlayPad + OverlayCloseHeight + 16f + extraBottom;
            float top = OverlayPad + OverlayTitleHeight + 12f + extraTop;
            return CreateVerticalScroll(
                box,
                "Scroll",
                new Vector2(OverlayPad, bottom),
                new Vector2(-OverlayPad, -top));
        }

        public static RectTransform PinOverlayFooter(Transform box, float height)
        {
            RectTransform footer = PinBottom(box, "Footer", height, OverlayPad);
            footer.sizeDelta = new Vector2(-(OverlayPad * 2f), height);
            return footer;
        }

        public static RectTransform PinTop(Transform parent, string name, float height, float topInset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topInset);
            rect.sizeDelta = new Vector2(-48f, height);
            return rect;
        }

        public static RectTransform PinBottom(Transform parent, string name, float height, float bottomInset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottomInset);
            rect.sizeDelta = new Vector2(-48f, height);
            return rect;
        }

        public static OverlayScroll CreateVerticalScroll(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            Stretch(scrollRect);
            scrollRect.offsetMin = offsetMin;
            scrollRect.offsetMax = offsetMax;
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.12f);
            scrollBg.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);

            var contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return new OverlayScroll(scrollRect, content);
        }

        public static Button CreateFixedSizeButton(
            Transform parent,
            string name,
            string label,
            Vector2 size,
            System.Action onClick,
            int fontSize = 26)
        {
            GameObject go = CreatePanel(parent, name, Color.white);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            LayoutElement layout = UiLayoutUtility.EnsureLayoutElement(go, size.y);
            layout.minWidth = Mathf.Min(160f, size.x);
            layout.preferredWidth = size.x;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;

            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UiButtonStyler.ApplyStandardTheme(button);

            Text text = CreateText(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        internal readonly struct OverlayScroll
        {
            public readonly RectTransform Root;
            public readonly RectTransform Content;

            public OverlayScroll(RectTransform root, RectTransform content)
            {
                Root = root;
                Content = content;
            }
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            System.Action onClick)
        {
            GameObject go = CreatePanel(parent, name, Color.white);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UiButtonStyler.ApplyStandardTheme(button);

            Text text = CreateText(go.transform, "Text", label, 34, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static Button CreateLayoutButton(
            Transform parent,
            string name,
            string label,
            float height,
            System.Action onClick,
            int fontSize = 30,
            float preferredWidth = -1f)
        {
            GameObject go = CreatePanel(parent, name, Color.white);
            RectTransform rect = go.GetComponent<RectTransform>();
            UiLayoutUtility.ResetForVerticalLayout(rect, height);
            float flex = preferredWidth >= 0f ? 0f : 1f;
            UiLayoutUtility.EnsureLayoutElement(go, height, flex, preferredWidth);

            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UiButtonStyler.ApplyStandardTheme(button);

            Text text = CreateText(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static Toggle CreateToggle(
            Transform parent,
            string label,
            bool value,
            Vector2 anchoredPosition,
            System.Action<bool> onChanged)
        {
            GameObject row = CreateToggleRow(parent, label, value, onChanged);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(760, 72);
            rowRect.anchoredPosition = anchoredPosition;
            return row.GetComponentInChildren<Toggle>();
        }

        public static Toggle CreateLayoutToggle(
            Transform parent,
            string label,
            bool value,
            System.Action<bool> onChanged)
        {
            GameObject row = CreateToggleRow(parent, label, value, onChanged);
            UiLayoutUtility.ResetForVerticalLayout(row.GetComponent<RectTransform>(), 72f);
            UiLayoutUtility.EnsureLayoutElement(row, 72f);
            return row.GetComponentInChildren<Toggle>();
        }

        private static GameObject CreateToggleRow(
            Transform parent,
            string label,
            bool value,
            System.Action<bool> onChanged)
        {
            VisualTheme theme = VisualThemeLocator.Load();

            GameObject row = new GameObject(label + "Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            Image rowBg = row.GetComponent<Image>();
            if (theme?.Panel != null)
            {
                rowBg.sprite = theme.Panel;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = new Color(1f, 1f, 1f, 0.55f);
            }
            else
            {
                rowBg.color = new Color(0.18f, 0.24f, 0.32f, 0.9f);
            }

            GameObject toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleGo.transform.SetParent(row.transform, false);
            RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 0.5f);
            toggleRect.anchorMax = new Vector2(0f, 0.5f);
            toggleRect.sizeDelta = new Vector2(56, 56);
            toggleRect.anchoredPosition = new Vector2(36f, 0f);

            Image toggleBg = toggleGo.GetComponent<Image>();
            if (theme?.ButtonDisabled != null)
            {
                toggleBg.sprite = theme.ButtonDisabled;
                toggleBg.type = Image.Type.Sliced;
                toggleBg.color = Color.white;
            }
            else
            {
                toggleBg.color = new Color(0.25f, 0.3f, 0.38f, 1f);
            }

            GameObject check = CreatePanel(toggleGo.transform, "Checkmark", Color.white);
            Stretch(check.GetComponent<RectTransform>());
            Image checkImage = check.GetComponent<Image>();
            if (theme?.ButtonPressed != null)
            {
                checkImage.sprite = theme.ButtonPressed;
                checkImage.type = Image.Type.Sliced;
                checkImage.color = Color.white;
            }
            else
            {
                checkImage.color = new Color(0.35f, 0.85f, 0.45f, 1f);
            }

            Toggle toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.graphic = checkImage;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(onChanged.Invoke);

            Text text = CreateText(row.transform, "Label", label, 30, TextAnchor.MiddleLeft);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(88f, 0f);
            textRect.offsetMax = new Vector2(-16f, 0f);
            return row;
        }
    }
}
