using System.Collections.Generic;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 SafeArea 레이아웃을 일관되게 정렬한다.</summary>
    public static class MainMenuUiLayout
    {
        private const float TitleHeight = 170f;
        private const float MetaHeight = 110f;
        private const float DifficultyHeight = 280f;
        private const float DetailHeight = 100f;
        private const float ActionHeight = 112f;

        public static void Apply(Transform safeArea)
        {
            if (safeArea == null)
            {
                return;
            }

            PlaceSettingsButton(safeArea);

            RectTransform root = EnsureContentRoot(safeArea);
            if (root == null)
            {
                return;
            }

            // TitleArtwork는 장식용 PNG(텍스트 없음)라 숨기고, 실제 제목 Text만 사용한다.
            Transform titleArt = FindNamed(safeArea, "TitleArtwork");
            if (titleArt != null)
            {
                titleArt.gameObject.SetActive(false);
            }

            int index = 0;
            EnsureSpacer(root, "SpacerTop", ref index, flexible: 0.6f);

            Transform title = FindNamed(safeArea, "Title");
            if (title != null)
            {
                title.gameObject.SetActive(true);
                ConfigureTitle(title as RectTransform);
                Place(root, title, TitleHeight, index++);
            }

            EnsureSpacer(root, "SpacerAfterTitle", ref index, flexible: 0.35f);
            PlaceIfExists(root, safeArea, "MetaStatusLabel", MetaHeight, ref index);
            EnsureSpacer(root, "SpacerAfterMeta", ref index, flexible: 0.35f);
            PlaceIfExists(root, safeArea, "DifficultySelection", DifficultyHeight, ref index);
            PlaceIfExists(root, safeArea, "DifficultyStatusLabel", DetailHeight, ref index);
            EnsureSpacer(root, "SpacerBeforeActions", ref index, flexible: 0.8f);
            PlaceIfExists(root, safeArea, "StartButton", ActionHeight, ref index);
            PlaceIfExists(root, safeArea, "ContinueButton", ActionHeight, ref index);
            EnsureSpacer(root, "SpacerBottom", ref index, flexible: 0.5f);

            ConfigureMetaLabel(root.Find("MetaStatusLabel") as RectTransform);
            ConfigureDifficultyArea(root.Find("DifficultySelection") as RectTransform);
            ConfigureDetailLabel(root.Find("DifficultyStatusLabel"));
            ConfigureActionButton(root.Find("StartButton") as RectTransform, ActionHeight);
            ConfigureActionButton(root.Find("ContinueButton") as RectTransform, ActionHeight);

            CleanupOrphanDifficultyButtons(safeArea, root);
            UiLayoutUtility.ForceRebuild(root);
        }

        /// <summary>메타 진행 텍스트 컴포넌트(자식 Label 우선).</summary>
        public static Text ResolveMetaStatusText(Transform metaRoot)
        {
            if (metaRoot == null)
            {
                return null;
            }

            Transform label = metaRoot.Find("Label");
            if (label != null)
            {
                Text childText = label.GetComponent<Text>();
                if (childText != null)
                {
                    return childText;
                }
            }

            return metaRoot.GetComponent<Text>();
        }

        private static void PlaceSettingsButton(Transform safeArea)
        {
            Transform settings = FindNamed(safeArea, "SettingsButton");
            if (settings is not RectTransform rect)
            {
                return;
            }

            rect.SetParent(safeArea, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(168f, 68f);
            UiButtonStyler.ApplyStandardTheme(settings.GetComponent<Button>());
            CenterButtonLabel(settings.GetComponent<Button>());
        }

        private static void CleanupOrphanDifficultyButtons(Transform safeArea, RectTransform contentRoot)
        {
            if (safeArea == null)
            {
                return;
            }

            var toDestroy = new List<GameObject>();
            for (int i = 0; i < safeArea.childCount; i++)
            {
                Transform child = safeArea.GetChild(i);
                if (child == contentRoot
                    || child.name == "MainMenuBackground"
                    || child.name == "SettingsButton"
                    || child.name == "TitleArtwork")
                {
                    continue;
                }

                if (child.name.StartsWith("Difficulty_") || child.name == "DifficultySelection")
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                Object.Destroy(toDestroy[i]);
            }
        }

        public static RectTransform EnsureContentRoot(Transform safeArea)
        {
            Transform existing = safeArea.Find("MainMenuContent");
            RectTransform root;
            if (existing != null)
            {
                root = existing as RectTransform;
            }
            else
            {
                var rootGo = new GameObject("MainMenuContent", typeof(RectTransform));
                root = rootGo.GetComponent<RectTransform>();
                root.SetParent(safeArea, false);
            }

            Stretch(root);

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 18f;
            layout.padding = new RectOffset(40, 40, 72, 56);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return root;
        }

        public static void ConfigureDifficultyContainer(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, DifficultyHeight);
            UiLayoutUtility.EnsureLayoutElement(rect.gameObject, DifficultyHeight);

            VerticalLayoutGroup layout = rect.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void PlaceIfExists(
            RectTransform root,
            Transform safeArea,
            string childName,
            float height,
            ref int siblingIndex)
        {
            Transform child = FindNamed(safeArea, childName);
            if (child == null)
            {
                child = root.Find(childName);
            }

            if (child == null)
            {
                return;
            }

            Place(root, child, height, siblingIndex++);
        }

        private static void Place(RectTransform root, Transform child, float height, int siblingIndex)
        {
            child.SetParent(root, false);
            child.SetSiblingIndex(siblingIndex);
            child.gameObject.SetActive(true);
            UiLayoutUtility.ResetForVerticalLayout(child as RectTransform, height);
            UiLayoutUtility.EnsureLayoutElement(child.gameObject, height);
        }

        private static void EnsureSpacer(RectTransform root, string name, ref int siblingIndex, float flexible)
        {
            Transform existing = root.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(root, false);
            }
            else
            {
                go = existing.gameObject;
            }

            go.transform.SetSiblingIndex(siblingIndex++);
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 0f;
            layout.preferredHeight = 0f;
            layout.flexibleHeight = flexible;
            layout.flexibleWidth = 1f;
        }

        private static void ConfigureDifficultyArea(RectTransform area)
        {
            ConfigureDifficultyContainer(area);
        }

        private static void ConfigureTitle(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            // Title에 잘못 붙은 Image(장식 PNG)가 텍스트를 가리면 제거
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                Object.Destroy(image);
            }

            Text title = rect.GetComponent<Text>();
            if (title != null)
            {
                title.enabled = true;
                title.alignment = TextAnchor.MiddleCenter;
                title.fontSize = 48;
                title.color = Color.white;
                title.horizontalOverflow = HorizontalWrapMode.Wrap;
                title.verticalOverflow = VerticalWrapMode.Overflow;
                if (string.IsNullOrWhiteSpace(title.text))
                {
                    title.text = "막차 생존: 9칸 디펜스";
                }
            }
        }

        private static void ConfigureMetaLabel(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            EnsureGraphicTextSplit(rect, out Image bg, out Text label);

            if (bg != null)
            {
                VisualTheme theme = VisualThemeLocator.Load();
                if (theme?.Panel != null)
                {
                    bg.sprite = theme.Panel;
                    bg.type = Image.Type.Sliced;
                    bg.color = Color.white;
                }
                else
                {
                    bg.color = new Color(0.12f, 0.18f, 0.28f, 0.92f);
                }

                bg.raycastTarget = false;
            }

            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 26;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, MetaHeight);
        }

        /// <summary>
        /// Graphic은 GameObject당 하나이므로 Background(Image)+Label(Text) 자식 구조로 분리한다.
        /// </summary>
        private static void EnsureGraphicTextSplit(RectTransform root, out Image background, out Text label)
        {
            background = null;
            label = null;

            Transform bgTransform = root.Find("Background");
            if (bgTransform == null)
            {
                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgTransform = bgGo.transform;
                bgTransform.SetParent(root, false);
            }

            bgTransform.SetAsFirstSibling();
            RectTransform bgRect = bgTransform as RectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background = bgTransform.GetComponent<Image>();

            Transform labelTransform = root.Find("Label");
            Text rootText = root.GetComponent<Text>();
            if (labelTransform == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelTransform = labelGo.transform;
                labelTransform.SetParent(root, false);
                label = labelGo.GetComponent<Text>();
                if (rootText != null)
                {
                    label.text = rootText.text;
                    label.font = rootText.font;
                    label.fontSize = rootText.fontSize;
                    label.color = rootText.color;
                    label.alignment = rootText.alignment;
                    Object.Destroy(rootText);
                }
                else
                {
                    Font font = GameFontProvider.Get();
                    if (font != null)
                    {
                        label.font = font;
                    }
                }
            }
            else
            {
                label = labelTransform.GetComponent<Text>();
                if (rootText != null)
                {
                    if (label != null && string.IsNullOrWhiteSpace(label.text))
                    {
                        label.text = rootText.text;
                    }

                    Object.Destroy(rootText);
                }
            }

            labelTransform.SetAsLastSibling();
            RectTransform labelRect = labelTransform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);
        }

        private static void ConfigureDetailLabel(Transform labelTransform)
        {
            if (labelTransform is not RectTransform rect)
            {
                return;
            }

            Text label = labelTransform.GetComponent<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, DetailHeight);
        }

        private static void ConfigureActionButton(RectTransform rect, float height)
        {
            if (rect == null)
            {
                return;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, height);
            Button button = rect.GetComponent<Button>();
            UiButtonStyler.ApplyStandardTheme(button);
            StripStrayIcons(rect);
            CenterButtonLabel(button);
        }

        private static void StripStrayIcons(Transform buttonRoot)
        {
            for (int i = buttonRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = buttonRoot.GetChild(i);
                if (child.name == "AdIcon" || child.name == "ThemeIcon" || child.name == "Icon")
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private static void CenterButtonLabel(Button button)
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
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            if (label.fontSize < 32)
            {
                label.fontSize = 34;
            }
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
