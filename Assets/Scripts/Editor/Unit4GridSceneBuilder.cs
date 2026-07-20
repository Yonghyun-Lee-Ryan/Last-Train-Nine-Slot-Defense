using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Run;
using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Game Scene에 3×3 Grid UI, PassengerView Prefab, GameGridBootstrap를 추가한다.
    /// </summary>
    public static class Unit4GridSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string PassengerViewPrefabPath = "Assets/Prefabs/UI/PassengerView.prefab";

        [MenuItem("Tools/막차 생존/개발 단위 4 Grid UI 추가 (Game Scene)")]
        public static void BuildGridUi()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 4 Grid UI 추가",
                    "Game Scene에 3×3 Grid, PassengerView Prefab, GameGridBootstrap를 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            EnsurePrefabFolder();
            PassengerView viewPrefab = LoadOrCreatePassengerViewPrefab();

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Game Scene에서 Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            GridManager existing = Object.FindAnyObjectByType<GridManager>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            (GridManager gridManager, GridSlot[] slots) = CreateGridPanel(parent, canvas, viewPrefab);
            AddBootstrap(canvas.gameObject, gridManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeObject = gridManager.gameObject;
            EditorUtility.DisplayDialog("완료", "Game Scene에 Grid UI가 추가되었습니다.", "확인");
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

        private static PassengerView LoadOrCreatePassengerViewPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PassengerView>(PassengerViewPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("PassengerView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(PassengerView));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(280, 280);

            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

            Image portrait = CreateChildImage(root.transform, "Portrait", new Color(0.7f, 0.7f, 0.7f, 1f), new Vector2(0, 20), new Vector2(220, 160));
            Text nameLabel = CreateChildText(root.transform, "NameLabel", "승객", 28, new Vector2(0, -70));
            Text starLabel = CreateChildText(root.transform, "StarLabel", "1★", 24, new Vector2(0, -120));

            var view = root.GetComponent<PassengerView>();
            AssignViewFields(view, portrait, nameLabel, starLabel);

            PrefabUtility.SaveAsPrefabAsset(root, PassengerViewPrefabPath);
            Object.DestroyImmediate(root);

            return AssetDatabase.LoadAssetAtPath<PassengerView>(PassengerViewPrefabPath);
        }

        private static (GridManager, GridSlot[]) CreateGridPanel(Transform parent, Canvas canvas, PassengerView viewPrefab)
        {
            var panelGo = new GameObject("PassengerGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(GridManager));
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 320f);
            panelRect.sizeDelta = new Vector2(960, 960);

            var layout = panelGo.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.cellSize = new Vector2(300, 300);
            layout.spacing = new Vector2(12, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;

            var slots = new GridSlot[RunState.GridSlotCount];
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                slots[i] = CreateGridSlot(panelGo.transform, i);
            }

            GridManager manager = panelGo.GetComponent<GridManager>();
            AssignManagerFields(manager, canvas, slots, viewPrefab);
            return (manager, slots);
        }

        private static GridSlot CreateGridSlot(Transform parent, int index)
        {
            var slotGo = new GameObject($"GridSlot_{index}", typeof(RectTransform), typeof(Image), typeof(GridSlot));
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.SetParent(parent, false);

            var bg = slotGo.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);

            var highlightGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.SetParent(slotGo.transform, false);
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            var highlightImage = highlightGo.GetComponent<Image>();
            highlightImage.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            highlightImage.enabled = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.SetParent(slotGo.transform, false);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(8, 8);
            contentRect.offsetMax = new Vector2(-8, -8);

            var slot = slotGo.GetComponent<GridSlot>();
            AssignSlotFields(slot, index, contentRect, highlightImage);
            return slot;
        }

        private static void AddBootstrap(GameObject canvasGo, GridManager gridManager)
        {
            GameGridBootstrap bootstrap = canvasGo.GetComponent<GameGridBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = canvasGo.AddComponent<GameGridBootstrap>();
            }

            AssignBootstrapFields(bootstrap, gridManager);
        }

        private static Image CreateChildImage(Transform parent, string name, Color color, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        private static Text CreateChildText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(260, 50);
            rect.anchoredPosition = anchoredPos;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.font = GetBuiltinFont();
            return label;
        }

        private static Font GetBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 16);
            }

            return font;
        }

        private static void AssignViewFields(PassengerView view, Image portrait, Text nameLabel, Text starLabel)
        {
            var so = new SerializedObject(view);
            so.FindProperty("portraitImage").objectReferenceValue = portrait;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("starLabel").objectReferenceValue = starLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSlotFields(GridSlot slot, int index, RectTransform content, Image highlight)
        {
            var so = new SerializedObject(slot);
            so.FindProperty("slotIndex").intValue = index;
            so.FindProperty("contentAnchor").objectReferenceValue = content;
            so.FindProperty("highlightImage").objectReferenceValue = highlight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignManagerFields(GridManager manager, Canvas canvas, GridSlot[] slots, PassengerView prefab)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("rootCanvas").objectReferenceValue = canvas;
            SerializedProperty slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            so.FindProperty("passengerViewPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBootstrapFields(GameGridBootstrap bootstrap, GridManager gridManager)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("gridManager").objectReferenceValue = gridManager;

            LoadDebugPassengers(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LoadDebugPassengers(SerializedObject bootstrapSo)
        {
            string[] guids = AssetDatabase.FindAssets("t:PassengerData", new[] { "Assets/Data/Passengers" });
            SerializedProperty passengersProp = bootstrapSo.FindProperty("debugPassengers");
            int count = Mathf.Min(guids.Length, 4);
            passengersProp.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                passengersProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Data.PassengerData>(path);
            }
        }
    }
}
