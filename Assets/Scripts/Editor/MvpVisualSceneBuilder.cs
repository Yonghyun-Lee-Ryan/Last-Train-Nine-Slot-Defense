using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>VisualTheme 스프라이트를 MainMenu/Game/Result 씬과 프리팹에 멱등 적용한다.</summary>
    public static class MvpVisualSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/막차 생존/MVP Visual/4. Apply Visual Skin To Scenes")]
        public static void ApplyToScenes()
        {
            VisualTheme theme = AssetDatabase.LoadAssetAtPath<VisualTheme>(VisualThemeLocator.AssetPath);
            if (theme == null)
            {
                EditorUtility.DisplayDialog("오류", "VisualTheme.asset을 먼저 생성하세요.", "확인");
                return;
            }

            ApplyMainMenu(theme);
            ApplyGame(theme);
            ApplyResult(theme);
            UpdatePrefabs();

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("완료", "3개 Scene과 핵심 Prefab에 비주얼 스킨을 적용했습니다.", "확인");
        }

        [MenuItem("Tools/막차 생존/MVP Visual/Build MVP Visual Assets (All)")]
        public static void BuildAll()
        {
            MvpFlatVectorArtGenerator.GenerateAllInternal(showDialog: false);
            MvpArtImporter.ImportAllInternal(showDialog: false);
            MvpVisualDataBuilder.BuildAllInternal(showDialog: false);
            ApplyToScenes();
            EditorUtility.DisplayDialog("완료", "MVP Visual Assets 전체 파이프라인이 완료되었습니다.", "확인");
        }

        private static void ApplyMainMenu(VisualTheme theme)
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            Transform safeArea = FindSafeArea(scene);
            if (safeArea == null)
            {
                return;
            }

            EnsureBackground(safeArea, theme.MainMenuBackground, "MainMenuBackground", stretch: true, alpha: 0.55f);
            ApplySpriteToNamedImage(safeArea, "TitleArtwork", theme.MainMenuTitle, preserveAspect: true);
            ApplySpriteToNamedImage(safeArea, "Title", theme.MainMenuTitle, preserveAspect: true);
            EnsureContinueButton(scene, safeArea);
            SkinButtons(safeArea, theme);
            GameFontProvider.ApplyTo(safeArea.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyGame(VisualTheme theme)
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            SceneBuilderCleanup.CleanupGeneratedDuplicates(scene);
            SceneBuilderCleanup.DestroyAllNamed(scene, "ExitToResultButton");
            RemoveGamePlaceholderControllers(scene);

            Transform safeArea = FindSafeArea(scene);
            if (safeArea == null)
            {
                return;
            }

            EnsureBackground(safeArea, theme.SubwayBackground, "SubwayBackground", stretch: true, alpha: 1f);
            for (int i = 0; i < BattleConstants.LegacyEnemyLaneDecorNames.Length; i++)
            {
                Transform legacy = safeArea.Find(BattleConstants.LegacyEnemyLaneDecorNames[i]);
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy.gameObject);
                }
            }

            for (int i = 0; i < BattleConstants.EnemyLaneDecors.Length; i++)
            {
                BattleConstants.LaneDecorSpec decor = BattleConstants.EnemyLaneDecors[i];
                EnsureBackground(
                    safeArea,
                    theme.SpawnLane,
                    decor.Name,
                    stretch: false,
                    alpha: 0.72f,
                    anchoredPosition: decor.AnchoredPosition,
                    size: decor.Size);
            }


            EnemyPathDirectionView.Ensure(safeArea as RectTransform);
            PassengerRangeOverlay.Ensure(safeArea as RectTransform);

            SkinButtons(safeArea, theme);
            SkinSliders(scene, theme);
            SkinGridSlots(scene, theme);
            SkinBattleMarkers(safeArea, theme);
            EnsureVfxInstaller(scene);
            GameFontProvider.ApplyTo(safeArea.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyResult(VisualTheme theme)
        {
            Scene scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            Transform safeArea = FindSafeArea(scene);
            if (safeArea == null)
            {
                return;
            }

            EnsureBackground(safeArea, theme.SubwayBackground, "ResultBackground", stretch: true, alpha: 0.45f);
            SkinButtons(safeArea, theme);
            GameFontProvider.ApplyTo(safeArea.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SkinBattleMarkers(Transform safeArea, VisualTheme theme)
        {
            Transform spawn = FindDeepChild(safeArea, "SpawnPoint");
            Image spawnImage = spawn != null ? spawn.GetComponent<Image>() : null;
            if (spawnImage != null)
            {
                spawnImage.color = Color.clear;
                spawnImage.raycastTarget = false;
            }

            Transform target = FindDeepChild(safeArea, "TrainTarget");
            if (target == null)
            {
                return;
            }

            RectTransform targetRect = target as RectTransform;
            if (targetRect != null)
            {
                targetRect.sizeDelta = new Vector2(160f, 140f);
            }

            Image targetImage = target.GetComponent<Image>();
            if (targetImage != null && theme.TrainTarget != null)
            {
                targetImage.sprite = theme.TrainTarget;
                targetImage.type = Image.Type.Simple;
                targetImage.preserveAspect = true;
                targetImage.color = Color.white;
                targetImage.raycastTarget = false;
            }
        }

        private static void EnsureContinueButton(Scene scene, Transform safeArea)
        {
            MainMenuController controller = SceneBuilderCleanup.FindFirstInScene<MainMenuController>(scene);
            if (controller == null)
            {
                return;
            }

            Transform existing = FindDeepChild(safeArea, "ContinueButton");
            Button continueButton = existing != null ? existing.GetComponent<Button>() : null;
            if (continueButton == null)
            {
                Transform startTransform = FindDeepChild(safeArea, "StartButton");
                Button startButton = startTransform != null ? startTransform.GetComponent<Button>() : null;
                if (startButton == null)
                {
                    return;
                }

                GameObject continueObject = Object.Instantiate(startButton.gameObject, startButton.transform.parent);
                continueObject.name = "ContinueButton";
                continueButton = continueObject.GetComponent<Button>();
                continueButton.onClick.RemoveAllListeners();

                RectTransform rect = continueObject.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -260f);

                Text label = continueObject.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = "이어하기";
                }
            }

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("continueButton").objectReferenceValue = continueButton;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void RemoveGamePlaceholderControllers(Scene scene)
        {
            System.Collections.Generic.List<GamePlaceholderController> controllers =
                SceneBuilderCleanup.FindAllInScene<GamePlaceholderController>(scene);
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                if (controllers[i] != null)
                {
                    Object.DestroyImmediate(controllers[i]);
                }
            }
        }

        private static void UpdatePrefabs()
        {
            UpdatePassengerViewPrefab();
            UpdateEnemyPrefab();
            UpdateProjectilePrefab();
        }

        private static void UpdatePassengerViewPrefab()
        {
            string path = "Assets/Prefabs/UI/PassengerView.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            GameFontProvider.ApplyTo(root);
            Image portrait = FindChildImage(root.transform, "Portrait");
            if (portrait != null)
            {
                EnsureComponent<UiSpriteAnimator>(portrait.gameObject);
            }

            Transform starFrame = root.transform.Find("StarFrame");
            if (starFrame == null)
            {
                var starGo = new GameObject("StarFrame", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(root.transform, false);
                RectTransform rect = starGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                starFrame = starGo.transform;
            }

            Image starImage = starFrame.GetComponent<Image>();
            starImage.raycastTarget = false;
            PassengerView view = root.GetComponent<PassengerView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("starFrameImage").objectReferenceValue = starImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateEnemyPrefab()
        {
            string path = "Assets/Prefabs/Enemies/BasicEnemy.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            GameFontProvider.ApplyTo(root);
            Image body = root.GetComponent<Image>();
            if (body != null)
            {
                EnsureComponent<UiSpriteAnimator>(body.gameObject);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateProjectilePrefab()
        {
            string path = "Assets/Prefabs/Projectiles/BasicProjectile.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Image image = root.GetComponent<Image>();
            if (image != null)
            {
                image.preserveAspect = true;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsureVfxInstaller(Scene scene)
        {
            Canvas canvas = SceneBuilderCleanup.FindFirstInScene<Canvas>(scene);
            if (canvas == null)
            {
                return;
            }

            UiVfxInstaller existing = canvas.GetComponentInChildren<UiVfxInstaller>(true);
            if (existing == null)
            {
                UiVfxInstaller.InstallIfMissing(canvas);
            }
        }

        private static void SkinGridSlots(Scene scene, VisualTheme theme)
        {
            System.Collections.Generic.List<GridSlot> slots = SceneBuilderCleanup.FindAllInScene<GridSlot>(scene);
            for (int i = 0; i < slots.Count; i++)
            {
                GridSlot slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                Image frame = slot.GetComponent<Image>();
                if (frame != null && theme.SeatFrame != null)
                {
                    frame.sprite = theme.SeatFrame;
                    frame.type = Image.Type.Sliced;
                    frame.color = Color.white;
                }
            }
        }

        private static void SkinSliders(Scene scene, VisualTheme theme)
        {
            System.Collections.Generic.List<Slider> sliders = SceneBuilderCleanup.FindAllInScene<Slider>(scene);
            for (int i = 0; i < sliders.Count; i++)
            {
                Slider slider = sliders[i];
                if (slider == null)
                {
                    continue;
                }

                Image background = slider.transform.Find("Background")?.GetComponent<Image>();
                Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
                if (background != null && theme.HpBarBackground != null)
                {
                    background.sprite = theme.HpBarBackground;
                    background.color = Color.white;
                }

                if (fill == null)
                {
                    continue;
                }

                bool boss = slider.gameObject.name.Contains("Boss", System.StringComparison.OrdinalIgnoreCase);
                Sprite fillSprite = boss && theme.BossHpBarFill != null ? theme.BossHpBarFill : theme.HpBarFill;
                if (fillSprite != null)
                {
                    fill.sprite = fillSprite;
                    fill.color = Color.white;
                }
            }
        }

        private static void SkinButtons(Transform root, VisualTheme theme)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                Image image = button.GetComponent<Image>();
                if (image == null || theme.ButtonNormal == null)
                {
                    continue;
                }

                image.sprite = theme.ButtonNormal;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                button.transition = Selectable.Transition.SpriteSwap;

                var spriteState = button.spriteState;
                spriteState.highlightedSprite = theme.ButtonNormal;
                spriteState.pressedSprite = theme.ButtonPressed != null ? theme.ButtonPressed : theme.ButtonNormal;
                spriteState.disabledSprite = theme.ButtonDisabled != null ? theme.ButtonDisabled : theme.ButtonNormal;
                button.spriteState = spriteState;
            }
        }

        private static void EnsureBackground(
            Transform parent,
            Sprite sprite,
            string objectName,
            bool stretch,
            float alpha,
            Vector2? anchoredPosition = null,
            Vector2? size = null)
        {
            if (sprite == null)
            {
                return;
            }

            Transform existing = parent.Find(objectName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(Image));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
                go.transform.SetAsFirstSibling();
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;

            if (stretch)
            {
                go.transform.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                go.transform.SetSiblingIndex(Mathf.Min(1, parent.childCount - 1));
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition ?? Vector2.zero;
                rect.sizeDelta = size ?? new Vector2(800f, 200f);
            }
        }

        private static void ApplySpriteToNamedImage(Transform root, string objectName, Sprite sprite, bool preserveAspect)
        {
            if (sprite == null)
            {
                return;
            }

            Transform target = FindDeepChild(root, objectName);
            if (target == null)
            {
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                // Text와 Image는 모두 Graphic이므로 같은 GameObject에 함께 추가할 수 없다.
                // 기존 레이아웃과 Text를 유지하면서 바로 뒤에 별도 이미지 형제를 만든다.
                Transform parent = target.parent;
                if (parent == null)
                {
                    return;
                }

                string artworkName = $"{objectName}Artwork";
                Transform artwork = parent.Find(artworkName);
                if (artwork == null)
                {
                    var artworkObject = new GameObject(artworkName, typeof(RectTransform), typeof(Image));
                    artwork = artworkObject.transform;
                    artwork.SetParent(parent, false);
                }

                RectTransform sourceRect = target as RectTransform;
                RectTransform artworkRect = artwork as RectTransform;
                if (sourceRect != null && artworkRect != null)
                {
                    artworkRect.anchorMin = sourceRect.anchorMin;
                    artworkRect.anchorMax = sourceRect.anchorMax;
                    artworkRect.pivot = sourceRect.pivot;
                    artworkRect.anchoredPosition = sourceRect.anchoredPosition;
                    artworkRect.sizeDelta = sourceRect.sizeDelta;
                    artworkRect.localRotation = sourceRect.localRotation;
                    artworkRect.localScale = sourceRect.localScale;
                }

                artwork.SetSiblingIndex(target.GetSiblingIndex());
                image = artwork.GetComponent<Image>();
                image.raycastTarget = false;
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
        }

        private static Transform FindSafeArea(Scene scene)
        {
            GameObject[] all = scene.GetRootGameObjects();
            for (int i = 0; i < all.Length; i++)
            {
                Transform safe = FindDeepChild(all[i].transform, "SafeArea");
                if (safe != null)
                {
                    return safe;
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Image FindChildImage(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }
    }
}
