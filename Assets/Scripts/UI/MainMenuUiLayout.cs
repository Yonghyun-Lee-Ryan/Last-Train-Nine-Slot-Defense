using System.Collections.Generic;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 SafeArea 레이아웃을 일관되게 정렬한다.</summary>
    public static class MainMenuUiLayout
    {
        private const float TitleHeight = 100f;
        private const float MetaHeight = 88f;
        private const float GoalCardHeight = 136f;
        private const float TabBarHeight = 72f;
        private const float GrowthPlaceholderHeight = 120f;
        private const float DetailHeight = 72f;
        private const float SettingsInset = 48f;
        private const float SettingsButtonHeight = 72f;
        private const float ContentPadX = 80f;

        /// <summary>호스트가 속한 메인 메뉴 Canvas의 SafeArea. 출석·동의 오버레이 Canvas는 고르지 않는다.</summary>
        public static Transform FindOwnedSafeArea(Component host)
        {
            if (host == null)
            {
                return null;
            }

            Canvas canvas = host.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = host.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                return null;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            return safeArea != null ? safeArea : canvas.transform;
        }

        public static bool IsMainMenuSafeArea(Transform safeArea)
        {
            if (safeArea == null)
            {
                return false;
            }

            if (safeArea.GetComponentInParent<MainMenuController>() != null)
            {
                return true;
            }

            return FindNamed(safeArea, "StartButton") != null
                   || safeArea.Find("MainMenuScroll") != null;
        }

        public static void Apply(Transform safeArea)
        {
            if (!IsMainMenuSafeArea(safeArea))
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
            EnsureSpacer(root, "SpacerTop", ref index, flexible: 0.15f);

            Transform title = FindNamed(safeArea, "Title");
            if (title != null)
            {
                title.gameObject.SetActive(true);
                ConfigureTitle(title as RectTransform);
                Place(root, title, TitleHeight, index++);
            }

            EnsureSpacer(root, "SpacerAfterTitle", ref index, flexible: 0.08f);
            PlaceIfExists(root, safeArea, "MetaStatusLabel", MetaHeight, ref index);
            EnsureSpacer(root, "SpacerAfterMeta", ref index, flexible: 0.06f);

            PlaceIfExists(root, safeArea, "TodayGoalCard", GoalCardHeight, ref index);
            PlaceIfExists(root, safeArea, "HomeTabBar", TabBarHeight, ref index);
            EnsureSpacer(root, "SpacerAfterTabs", ref index, flexible: 0.08f);

            float difficultyHeight = ResolveDifficultyHeight(safeArea, root);
            PlaceIfExists(root, safeArea, "DifficultySelection", difficultyHeight, ref index);
            PlaceIfExists(root, safeArea, "DifficultyStatusLabel", DetailHeight, ref index);
            EnsureSpacer(root, "SpacerBeforeActions", ref index, flexible: 0.18f);

            PlaceIfExists(root, safeArea, "StartButton", UiButtonStyler.MenuPrimaryHeight, ref index);
            PlaceIfExists(root, safeArea, "ContinueButton", UiButtonStyler.MenuPrimaryHeight, ref index);
            PlaceIfExists(root, safeArea, "DailyRunButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "QuickRunButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "EndlessRunButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "GrowthPlaceholder", GrowthPlaceholderHeight, ref index);
            PlaceIfExists(root, safeArea, "CodexButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "AttendanceButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "EndlessMilestoneButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "AchievementButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            // 비활성 버튼도 Content에 둔다(VLG가 inactive 자식을 무시). SafeArea에 빼 두면 재활성 시 중앙에 뜬다.
            PlaceIfExists(root, safeArea, "LiveEventButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            PlaceIfExists(root, safeArea, "MissionButton", UiButtonStyler.MenuSecondaryHeight, ref index);
            EnsureSpacer(root, "SpacerBottom", ref index, flexible: 0.2f);

            ConfigureMetaLabel(root.Find("MetaStatusLabel") as RectTransform);
            ConfigureGoalCard(root.Find("TodayGoalCard") as RectTransform);
            ConfigureTabBar(root.Find("HomeTabBar") as RectTransform);
            ConfigureGrowthPlaceholder(root.Find("GrowthPlaceholder") as RectTransform);
            ConfigureDifficultyArea(root.Find("DifficultySelection") as RectTransform);
            ConfigureDetailLabel(root.Find("DifficultyStatusLabel"));
            ConfigureActionButton(FindNamed(root, "StartButton") as RectTransform, UiButtonStyler.MenuPrimaryHeight);
            ConfigureActionButton(FindNamed(root, "ContinueButton") as RectTransform, UiButtonStyler.MenuPrimaryHeight);
            ConfigureActionButton(FindNamed(root, "DailyRunButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "QuickRunButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "EndlessRunButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "LiveEventButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "MissionButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "CodexButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "AttendanceButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "EndlessMilestoneButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);
            ConfigureActionButton(FindNamed(root, "AchievementButton") as RectTransform, UiButtonStyler.MenuSecondaryHeight);

            ApplyHomeSectionVisibility(root);
            CleanupOrphanDifficultyButtons(safeArea, root);
            Canvas.ForceUpdateCanvases();
            RedistributeVerticalSpace(root);
            UiLayoutUtility.ForceRebuild(root);
            if (root.parent is RectTransform scrollContentParent
                && scrollContentParent.name == "Viewport"
                && scrollContentParent.parent is RectTransform scrollRoot)
            {
                UiLayoutUtility.ForceRebuild(scrollRoot);
            }
        }

        /// <summary>
        /// ContentSizeFitter(Preferred)는 flexibleHeight를 무시해 스페이서가 0이 된다.
        /// 뷰포트 남는 높이를 스페이서 preferredHeight로 나눠 화면 전체에 분산한다.
        /// </summary>
        private static void RedistributeVerticalSpace(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            RectTransform viewport = root.parent as RectTransform;
            float viewportHeight = viewport != null ? viewport.rect.height : 0f;
            if (viewportHeight < 8f && viewport != null && viewport.parent is RectTransform scrollRoot)
            {
                viewportHeight = scrollRoot.rect.height;
            }

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            float padding = 0f;
            float spacing = 0f;
            if (layout != null)
            {
                padding = layout.padding.top + layout.padding.bottom;
                spacing = layout.spacing;
            }

            float fixedHeight = 0f;
            int laidOutChildren = 0;
            float flexWeightTotal = 0f;
            var spacers = new List<LayoutElement>();

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                laidOutChildren++;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (child.name.StartsWith("Spacer", System.StringComparison.Ordinal))
                {
                    if (element == null)
                    {
                        element = child.gameObject.AddComponent<LayoutElement>();
                    }

                    float weight = element.flexibleHeight > 0f ? element.flexibleHeight : 1f;
                    // weight를 flexibleHeight에 잠시 보관했다가 preferred로 환산한다.
                    element.flexibleHeight = weight;
                    flexWeightTotal += weight;
                    spacers.Add(element);
                    continue;
                }

                float childHeight = element != null && element.preferredHeight > 0f
                    ? element.preferredHeight
                    : (child as RectTransform)?.sizeDelta.y ?? 0f;
                fixedHeight += Mathf.Max(0f, childHeight);
            }

            float spacingTotal = laidOutChildren > 1 ? spacing * (laidOutChildren - 1) : 0f;
            float contentMin = fixedHeight + spacingTotal + padding;
            float leftover = viewportHeight > contentMin ? viewportHeight - contentMin : 0f;

            if (spacers.Count == 0)
            {
                return;
            }

            if (flexWeightTotal <= 0f)
            {
                flexWeightTotal = spacers.Count;
            }

            for (int i = 0; i < spacers.Count; i++)
            {
                LayoutElement spacer = spacers[i];
                float weight = spacer.flexibleHeight > 0f ? spacer.flexibleHeight : 1f;
                float share = leftover > 0f ? leftover * (weight / flexWeightTotal) : 0f;
                spacer.minHeight = share;
                spacer.preferredHeight = share;
                spacer.flexibleHeight = 0f;
                spacer.flexibleWidth = 1f;
            }

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, Mathf.Max(viewportHeight, contentMin + leftover));
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

        private static float ResolveDifficultyHeight(Transform safeArea, RectTransform contentRoot)
        {
            Transform area = FindNamed(safeArea, "DifficultySelection") ?? contentRoot.Find("DifficultySelection");
            int buttonCount = 0;
            if (area != null)
            {
                for (int i = 0; i < area.childCount; i++)
                {
                    if (area.GetChild(i).gameObject.activeSelf)
                    {
                        buttonCount++;
                    }
                }
            }

            buttonCount = Mathf.Max(1, buttonCount);
            const float padding = 24f;
            const float spacing = 10f;
            return padding + (buttonCount * UiButtonStyler.MenuDifficultyHeight) + ((buttonCount - 1) * spacing);
        }

        private static void PlaceSettingsButton(Transform safeArea)
        {
            Transform settings = FindNamed(safeArea, "SettingsButton");
            if (settings is not RectTransform rect)
            {
                return;
            }

            rect.SetParent(safeArea, false);
            rect.SetAsLastSibling();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-SettingsInset, -SettingsInset);
            rect.sizeDelta = new Vector2(168f, SettingsButtonHeight);
            UiButtonStyler.ApplyStandardTheme(settings.GetComponent<Button>());
            CenterButtonLabel(settings.GetComponent<Button>());
        }

        private static void CleanupOrphanDifficultyButtons(Transform safeArea, RectTransform contentRoot)
        {
            if (safeArea == null)
            {
                return;
            }

            Transform scroll = safeArea.Find("MainMenuScroll");
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < safeArea.childCount; i++)
            {
                Transform child = safeArea.GetChild(i);
                if (child == contentRoot
                    || child == scroll
                    || child.name == "MainMenuBackground"
                    || child.name == "SettingsButton"
                    || child.name == "TitleArtwork"
                    || child.name == "MainMenuContent")
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
            if (!IsMainMenuSafeArea(safeArea))
            {
                return null;
            }

            RectTransform scrollRoot = EnsureScrollRoot(safeArea);
            Transform viewport = scrollRoot.Find("Viewport");
            if (viewport == null)
            {
                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewport = viewportGo.transform;
                viewport.SetParent(scrollRoot, false);
                Image viewportImage = viewportGo.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
                viewportImage.raycastTarget = true;
            }

            RectTransform viewportRect = viewport as RectTransform;
            Stretch(viewportRect);

            Transform existing = viewport.Find("MainMenuContent");
            if (existing == null)
            {
                // 이전 구조 호환: SafeArea 직계 MainMenuContent를 Viewport로 옮긴다.
                Transform legacy = safeArea.Find("MainMenuContent");
                if (legacy != null)
                {
                    legacy.SetParent(viewport, false);
                    existing = legacy;
                }
            }

            RectTransform root;
            if (existing != null)
            {
                root = existing as RectTransform;
            }
            else
            {
                var rootGo = new GameObject("MainMenuContent", typeof(RectTransform));
                root = rootGo.GetComponent<RectTransform>();
                root.SetParent(viewport, false);
            }

            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, 0f);
            root.offsetMin = new Vector2(0f, root.offsetMin.y);
            root.offsetMax = new Vector2(0f, root.offsetMax.y);

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset((int)ContentPadX, (int)ContentPadX, 24, 40);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.content = root;
            scroll.viewport = viewportRect;

            return root;
        }

        private static RectTransform EnsureScrollRoot(Transform safeArea)
        {
            Transform existing = safeArea.Find("MainMenuScroll");
            RectTransform scrollRoot;
            if (existing != null)
            {
                scrollRoot = existing as RectTransform;
            }
            else
            {
                var go = new GameObject(
                    "MainMenuScroll",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(ScrollRect));
                scrollRoot = go.GetComponent<RectTransform>();
                scrollRoot.SetParent(safeArea, false);
                Image bg = go.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0f);
                bg.raycastTarget = true;
            }

            Stretch(scrollRoot);
            // 설정 버튼과 겹치지 않도록 상단 여백
            scrollRoot.offsetMax = new Vector2(0f, -(SettingsInset + SettingsButtonHeight + 8f));
            scrollRoot.offsetMin = Vector2.zero;

            ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            return scrollRoot;
        }

        public static void ConfigureDifficultyContainer(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            float height = ResolveDifficultyHeight(rect, null);
            UiLayoutUtility.ResetForVerticalLayout(rect, height);
            UiLayoutUtility.EnsureLayoutElement(rect.gameObject, height);

            VerticalLayoutGroup layout = rect.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 12, 12);
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
            bool wasActive = child.gameObject.activeSelf;
            child.SetParent(root, false);
            child.SetSiblingIndex(siblingIndex);
            child.gameObject.SetActive(wasActive);
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
                title.fontSize = 36;
                title.color = Color.white;
                title.horizontalOverflow = HorizontalWrapMode.Wrap;
                title.verticalOverflow = VerticalWrapMode.Truncate;
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
                    UiButtonStyler.ApplySlicedPanel(bg, theme.Panel);
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
                label.fontSize = 22;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, MetaHeight);
            UiButtonStyler.CapMenuWidth(UiLayoutUtility.EnsureLayoutElement(rect.gameObject, MetaHeight));
        }

        private static void ConfigureGoalCard(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            EnsureGraphicTextSplit(rect, out Image bg, out Text label);
            VisualTheme theme = VisualThemeLocator.Load();
            if (bg != null)
            {
                if (theme?.Panel != null)
                {
                    UiButtonStyler.ApplySlicedPanel(bg, theme.Panel);
                }
                else
                {
                    bg.color = new Color(0.16f, 0.22f, 0.34f, 0.96f);
                }

                bg.raycastTarget = true;
            }

            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft;
                label.fontSize = 22;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, GoalCardHeight);
            UiButtonStyler.CapMenuWidth(UiLayoutUtility.EnsureLayoutElement(rect.gameObject, GoalCardHeight));
        }

        private static void ConfigureTabBar(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            HorizontalLayoutGroup layout = rect.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            UiLayoutUtility.ResetForVerticalLayout(rect, TabBarHeight);
            UiLayoutUtility.EnsureLayoutElement(rect.gameObject, TabBarHeight);

            for (int i = 0; i < rect.childCount; i++)
            {
                Transform child = rect.GetChild(i);
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    UiButtonStyler.ApplyStandardTheme(button);
                    CenterButtonLabel(button, useBestFit: false);
                    ApplyTabVisual(button, IsActiveHomeTab(child.name));
                }

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = child.gameObject.AddComponent<LayoutElement>();
                }

                element.flexibleWidth = 1f;
                element.minHeight = TabBarHeight - 8f;
                element.preferredHeight = TabBarHeight - 8f;
            }
        }

        private static void ConfigureGrowthPlaceholder(RectTransform rect)
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
                    UiButtonStyler.ApplySlicedPanel(bg, theme.Panel);
                }
                else
                {
                    bg.color = new Color(0.12f, 0.16f, 0.22f, 0.9f);
                }

                bg.raycastTarget = false;
            }

            if (label != null)
            {
                if (string.IsNullOrWhiteSpace(label.text))
                {
                    label.text = "성장\n도감·출석·업적에서 메타 보상을 확인하세요.";
                }

                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
            }

            UiLayoutUtility.ResetForVerticalLayout(rect, GrowthPlaceholderHeight);
            UiButtonStyler.CapMenuWidth(UiLayoutUtility.EnsureLayoutElement(rect.gameObject, GrowthPlaceholderHeight));
        }

        private static void ApplyHomeSectionVisibility(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            bool play = MainMenuHomeTabs.Active == MainMenuHomeSection.Play;
            bool growth = MainMenuHomeTabs.Active == MainMenuHomeSection.Growth;
            bool season = MainMenuHomeTabs.Active == MainMenuHomeSection.Season;

            SetNamedActive(root, "DifficultySelection", play);
            SetNamedActive(root, "DifficultyStatusLabel", play);
            SetNamedActive(root, "StartButton", play);
            SetNamedActive(root, "ContinueButton", play && MainMenuHomeTabs.ContinueAvailable);
            SetNamedActive(root, "DailyRunButton", play);
            SetNamedActive(root, "QuickRunButton", play);
            SetNamedActive(root, "EndlessRunButton", play);
            SetNamedActive(root, "GrowthPlaceholder", growth);
            SetNamedActive(root, "CodexButton", growth);
            SetNamedActive(root, "AttendanceButton", growth);
            SetNamedActive(root, "EndlessMilestoneButton", growth);
            SetNamedActive(root, "AchievementButton", growth);
            SetNamedActive(root, "LiveEventButton", season && MainMenuHomeTabs.LiveEventAvailable);
            SetNamedActive(root, "MissionButton", season);
        }

        private static void SetNamedActive(RectTransform root, string name, bool active)
        {
            Transform child = root.Find(name) ?? FindNamed(root, name);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

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
                label.fontSize = 22;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
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
            LayoutElement layout = UiLayoutUtility.EnsureLayoutElement(rect.gameObject, height, 0f, UiButtonStyler.MenuActionMaxWidth);
            UiButtonStyler.CapMenuWidth(layout);
        }

        private static bool IsActiveHomeTab(string childName)
        {
            return (childName == "TabPlay" && MainMenuHomeTabs.Active == MainMenuHomeSection.Play)
                   || (childName == "TabGrowth" && MainMenuHomeTabs.Active == MainMenuHomeSection.Growth)
                   || (childName == "TabSeason" && MainMenuHomeTabs.Active == MainMenuHomeSection.Season);
        }

        private static void ApplyTabVisual(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = active ? 28 : 24;
                label.color = active ? Color.white : new Color(0.75f, 0.8f, 0.88f, 1f);
                label.resizeTextForBestFit = false;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
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

        private static void CenterButtonLabel(Button button, bool useBestFit = true)
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
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);
            textRect.anchoredPosition = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (useBestFit)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 32;
                if (label.fontSize < 26)
                {
                    label.fontSize = 28;
                }
            }
            else
            {
                label.resizeTextForBestFit = false;
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
