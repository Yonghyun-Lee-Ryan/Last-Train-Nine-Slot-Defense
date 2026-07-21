using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene에 소환 UI와 SummonEconomyConfig를 추가한다.
    /// </summary>
    public static class Unit9SummonSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";
        private const string EconomyConfigPath = "Assets/Data/SummonEconomyConfig.asset";

        [MenuItem("Tools/막차 생존/개발 단위 9 소환 UI 추가 (Game Scene)")]
        public static void BuildSummonUi()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 9 소환 UI 추가",
                    "Game Scene에 소환/판매 패널과 SummonEconomyConfig를 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            SummonEconomyConfig config = LoadOrCreateEconomyConfig();
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
            SceneBuilderCleanup.DestroyAllComponents<SummonPanelController>(scene);

            GridManager gridManager = SceneBuilderCleanup.FindFirstInScene<GridManager>(scene);
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);

            GameObject root = CreateSummonPanel(parent, gridManager, database, config);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = root;
            EditorUtility.DisplayDialog("완료", "소환 UI가 추가되었습니다.", "확인");
        }

        private static SummonEconomyConfig LoadOrCreateEconomyConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>(EconomyConfigPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            var config = ScriptableObject.CreateInstance<SummonEconomyConfig>();
            AssetDatabase.CreateAsset(config, EconomyConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static GameObject CreateSummonPanel(
            Transform parent,
            GridManager gridManager,
            GameDatabase database,
            SummonEconomyConfig config)
        {
            var root = new GameObject("SummonPanel", typeof(RectTransform), typeof(SummonPanelController));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 8f);
            rootRect.sizeDelta = new Vector2(0f, 200f);

            Text coinLabel = CreateBottomText(root.transform, "CoinLabel", "코인 50", 32, new Vector2(-320f, 140f), new Vector2(280f, 40f));
            Text costLabel = CreateBottomText(root.transform, "CostLabel", "소환 10", 28, new Vector2(320f, 140f), new Vector2(280f, 40f));
            Text statusLabel = CreateBottomText(root.transform, "StatusLabel", "상태", 24, new Vector2(0f, 140f), new Vector2(400f, 40f));

            Button summonButton = CreateBottomButton(root.transform, "SummonButton", "소환", new Vector2(0f, 45f), new Vector2(240f, 80f));

            GameObject offerPanel = CreateOfferPanel(root.transform, out Button[] offerButtons, out Text[] offerLabels,
                out Button cancelButton, out Button freeReroll, out Button adReroll);

            var controller = root.GetComponent<SummonPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("gridManager").objectReferenceValue = gridManager;
            so.FindProperty("gameDatabase").objectReferenceValue = database;
            so.FindProperty("economyConfig").objectReferenceValue = config;
            so.FindProperty("coinLabel").objectReferenceValue = coinLabel;
            so.FindProperty("costLabel").objectReferenceValue = costLabel;
            so.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            so.FindProperty("summonButton").objectReferenceValue = summonButton;
            so.FindProperty("offerPanel").objectReferenceValue = offerPanel;

            SerializedProperty offerButtonsProp = so.FindProperty("offerButtons");
            offerButtonsProp.arraySize = offerButtons.Length;
            for (int i = 0; i < offerButtons.Length; i++)
            {
                offerButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = offerButtons[i];
            }

            SerializedProperty offerLabelsProp = so.FindProperty("offerLabels");
            offerLabelsProp.arraySize = offerLabels.Length;
            for (int i = 0; i < offerLabels.Length; i++)
            {
                offerLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = offerLabels[i];
            }

            so.FindProperty("cancelOfferButton").objectReferenceValue = cancelButton;
            so.FindProperty("freeRerollButton").objectReferenceValue = freeReroll;
            so.FindProperty("adRerollButton").objectReferenceValue = adReroll;
            so.ApplyModifiedPropertiesWithoutUndo();

            offerPanel.SetActive(false);
            return root;
        }

        private static GameObject CreateOfferPanel(
            Transform parent,
            out Button[] offerButtons,
            out Text[] offerLabels,
            out Button cancelButton,
            out Button freeReroll,
            out Button adReroll)
        {
            var panel = new GameObject("OfferPanel", typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 520f);
            rect.anchoredPosition = new Vector2(0f, 520f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

            CreateText(panel.transform, "Title", "승객 소환", 40, new Vector2(0f, 200f), new Vector2(400f, 50f));

            offerButtons = new Button[3];
            offerLabels = new Text[3];
            float[] xPositions = { -280f, 0f, 280f };
            for (int i = 0; i < 3; i++)
            {
                offerButtons[i] = CreateButton(panel.transform, $"Offer{i}", $"후보 {i + 1}",
                    new Vector2(xPositions[i], 40f), new Vector2(240f, 160f));
                offerLabels[i] = offerButtons[i].GetComponentInChildren<Text>();
            }

            cancelButton = CreateButton(panel.transform, "CancelButton", "취소", new Vector2(-280f, -160f), new Vector2(200f, 70f));
            freeReroll = CreateButton(panel.transform, "FreeRerollButton", "무료 리롤", new Vector2(0f, -160f), new Vector2(200f, 70f));
            adReroll = CreateButton(panel.transform, "AdRerollButton", "광고 리롤", new Vector2(280f, -160f), new Vector2(200f, 70f));
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f, 1f);

            Text text = CreateText(go.transform, "Label", label, 28, Vector2.zero, size);
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static Button CreateBottomButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            Button button = CreateButton(parent, name, label, anchoredPos, size);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return button;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GameFontProvider.Get();
            return text;
        }

        private static Text CreateBottomText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 size)
        {
            Text text = CreateText(parent, name, content, fontSize, anchoredPos, size);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return text;
        }
    }
}
