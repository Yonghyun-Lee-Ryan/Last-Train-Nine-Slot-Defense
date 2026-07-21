using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>Game Scene에 전투 HUD(내구도·역·웨이브·준비·속도·일시정지)를 추가한다.</summary>
    public static class Unit10BattleHudSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string FloatingTextPrefabPath = "Assets/Prefabs/UI/FloatingCombatText.prefab";

        [MenuItem("Tools/막차 생존/개발 단위 10 전투 HUD 추가 (Game Scene)")]
        public static void BuildBattleHud()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 10 전투 HUD 추가",
                    "Game Scene에 전투 HUD, 승객 상세 팝업, 준비/속도/일시정지 UI를 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            EnsurePrefabFolder();
            FloatingCombatText floatingPrefab = LoadOrCreateFloatingTextPrefab();

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            SceneBuilderCleanup.CleanupGeneratedDuplicates(scene);
            SceneBuilderCleanup.DestroyAllComponents<BattleHudController>(scene);

            GridManager gridManager = SceneBuilderCleanup.FindFirstInScene<GridManager>(scene);
            GameBattleBootstrap bootstrap = SceneBuilderCleanup.FindFirstInScene<GameBattleBootstrap>(scene);
            if (bootstrap != null)
            {
                var bootstrapSo = new SerializedObject(bootstrap);
                bootstrapSo.FindProperty("autoStartFirstWave").boolValue = false;
                bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject hudRoot = CreateHud(parent, gridManager, bootstrap, floatingPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = hudRoot;
            EditorUtility.DisplayDialog("완료", "전투 HUD가 추가되었습니다.", "확인");
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }

                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        private static FloatingCombatText LoadOrCreateFloatingTextPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloatingCombatText>(FloatingTextPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("FloatingCombatText", typeof(RectTransform), typeof(Text), typeof(FloatingCombatText));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 60f);
            var text = go.GetComponent<Text>();
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GetFont();
            text.raycastTarget = false;

            var so = new SerializedObject(go.GetComponent<FloatingCombatText>());
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, FloatingTextPrefabPath);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<FloatingCombatText>(FloatingTextPrefabPath);
        }

        private static GameObject CreateHud(
            Transform parent,
            GridManager gridManager,
            GameBattleBootstrap bootstrap,
            FloatingCombatText floatingPrefab)
        {
            var root = new GameObject("BattleHud", typeof(RectTransform), typeof(BattleHudController));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            StretchFull(rootRect);

            // Top status bar (top-anchored for 1080×1920 bands)
            Text trainHpLabel = CreateTopText(root.transform, "TrainHpLabel", "객차 100/100", 30, new Vector2(-300f, -40f), new Vector2(360f, 40f));
            Slider hpSlider = CreateTopSlider(root.transform, "TrainHpSlider", new Vector2(-300f, -78f), new Vector2(360f, 24f));
            Text coinLabel = CreateTopText(root.transform, "CoinLabel", "코인 50", 30, new Vector2(300f, -40f), new Vector2(280f, 40f));
            Text stationLabel = CreateTopText(root.transform, "StationLabel", "역 1/5", 28, new Vector2(-300f, -120f), new Vector2(280f, 36f));
            Text waveLabel = CreateTopText(root.transform, "WaveLabel", "웨이브 1/1", 28, new Vector2(0f, -120f), new Vector2(280f, 36f));
            Text phaseLabel = CreateTopText(root.transform, "PhaseLabel", "준비", 28, new Vector2(300f, -120f), new Vector2(280f, 36f));
            Text statusLabel = CreateTopText(root.transform, "StatusLabel", "상태", 24, new Vector2(0f, -165f), new Vector2(900f, 36f));

            Button readyButton = CreateBottomButton(root.transform, "ReadyButton", "준비 완료", new Vector2(-190f, 220f), new Vector2(160f, 78f));
            Button speedButton = CreateBottomButton(root.transform, "SpeedButton", "1x", new Vector2(0f, 220f), new Vector2(160f, 78f));
            Button pauseButton = CreateBottomButton(root.transform, "PauseButton", "일시정지", new Vector2(190f, 220f), new Vector2(160f, 78f));

            // Pause overlay
            var pauseOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            var pauseRect = pauseOverlay.GetComponent<RectTransform>();
            pauseRect.SetParent(root.transform, false);
            StretchFull(pauseRect);
            pauseOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            CreateText(pauseOverlay.transform, "PauseTitle", "일시정지", 48, new Vector2(0f, 80f), new Vector2(400f, 60f));
            Button resumeButton = CreateButton(pauseOverlay.transform, "ResumeButton", "계속하기", new Vector2(0f, -40f), new Vector2(280f, 100f));
            pauseOverlay.SetActive(false);

            // Detail popup
            var detailRoot = new GameObject("PassengerDetailPopup", typeof(RectTransform), typeof(Image), typeof(PassengerDetailPopup));
            var detailRect = detailRoot.GetComponent<RectTransform>();
            detailRect.SetParent(root.transform, false);
            detailRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailRect.sizeDelta = new Vector2(640f, 520f);
            detailRect.anchoredPosition = new Vector2(0f, 80f);
            detailRoot.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.96f);

            Text nameLabel = CreateText(detailRoot.transform, "NameLabel", "승객", 36, new Vector2(0f, 180f), new Vector2(500f, 50f));
            Text starLabel = CreateText(detailRoot.transform, "StarLabel", "1★", 30, new Vector2(0f, 120f), new Vector2(200f, 40f));
            Text attackLabel = CreateText(detailRoot.transform, "AttackLabel", "공격", 28, new Vector2(0f, 60f), new Vector2(400f, 40f));
            Text intervalLabel = CreateText(detailRoot.transform, "IntervalLabel", "주기", 28, new Vector2(0f, 10f), new Vector2(400f, 40f));
            Text rangeLabel = CreateText(detailRoot.transform, "RangeLabel", "사거리", 28, new Vector2(0f, -40f), new Vector2(400f, 40f));
            Text sellPriceLabel = CreateText(detailRoot.transform, "SellPriceLabel", "판매가", 28, new Vector2(0f, -90f), new Vector2(400f, 40f));
            Button sellButton = CreateButton(detailRoot.transform, "SellButton", "판매", new Vector2(-140f, -180f), new Vector2(200f, 80f));
            Button closeButton = CreateButton(detailRoot.transform, "CloseButton", "닫기", new Vector2(140f, -180f), new Vector2(200f, 80f));

            var detailPopup = detailRoot.GetComponent<PassengerDetailPopup>();
            var detailSo = new SerializedObject(detailPopup);
            detailSo.FindProperty("root").objectReferenceValue = detailRoot;
            detailSo.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            detailSo.FindProperty("starLabel").objectReferenceValue = starLabel;
            detailSo.FindProperty("attackLabel").objectReferenceValue = attackLabel;
            detailSo.FindProperty("intervalLabel").objectReferenceValue = intervalLabel;
            detailSo.FindProperty("rangeLabel").objectReferenceValue = rangeLabel;
            detailSo.FindProperty("sellPriceLabel").objectReferenceValue = sellPriceLabel;
            detailSo.FindProperty("sellButton").objectReferenceValue = sellButton;
            detailSo.FindProperty("closeButton").objectReferenceValue = closeButton;
            detailSo.ApplyModifiedPropertiesWithoutUndo();
            detailRoot.SetActive(false);

            var floatingRoot = new GameObject("FloatingTextRoot", typeof(RectTransform));
            var floatingRect = floatingRoot.GetComponent<RectTransform>();
            floatingRect.SetParent(root.transform, false);
            StretchFull(floatingRect);

            var hud = root.GetComponent<BattleHudController>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            hudSo.FindProperty("battleBootstrap").objectReferenceValue = bootstrap;
            hudSo.FindProperty("detailPopup").objectReferenceValue = detailPopup;
            hudSo.FindProperty("floatingTextPrefab").objectReferenceValue = floatingPrefab;
            hudSo.FindProperty("floatingTextRoot").objectReferenceValue = floatingRect;
            hudSo.FindProperty("trainHpSlider").objectReferenceValue = hpSlider;
            hudSo.FindProperty("trainHpLabel").objectReferenceValue = trainHpLabel;
            hudSo.FindProperty("coinLabel").objectReferenceValue = coinLabel;
            hudSo.FindProperty("stationLabel").objectReferenceValue = stationLabel;
            hudSo.FindProperty("waveLabel").objectReferenceValue = waveLabel;
            hudSo.FindProperty("phaseLabel").objectReferenceValue = phaseLabel;
            hudSo.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            hudSo.FindProperty("readyButton").objectReferenceValue = readyButton;
            hudSo.FindProperty("speedButton").objectReferenceValue = speedButton;
            hudSo.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            hudSo.FindProperty("pauseOverlay").objectReferenceValue = pauseOverlay;
            hudSo.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Slider CreateTopSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            Slider slider = CreateSlider(parent, name, pos, size);
            var rect = slider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return slider;
        }

        private static Text CreateTopText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size)
        {
            Text text = CreateText(parent, name, content, fontSize, pos, size);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return text;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            StretchFull(bg.GetComponent<RectTransform>());
            bg.transform.SetParent(go.transform, false);
            bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.SetParent(go.transform, false);
            StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            StretchFull(fill.GetComponent<RectTransform>());
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.3f, 1f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;
            slider.interactable = false;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 1f);
            Text text = CreateText(go.transform, "Label", label, 28, Vector2.zero, size);
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static Button CreateBottomButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            Button button = CreateButton(parent, name, label, pos, size);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return button;
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
            text.font = GetFont();
            return text;
        }

        private static Font GetFont()
        {
            return GameFontProvider.Get();
        }
    }
}
