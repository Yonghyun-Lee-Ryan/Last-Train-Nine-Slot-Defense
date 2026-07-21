using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>Game Scene BattleHud에 보스 체력 UI를 추가한다.</summary>
    public static class Unit14BossSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/막차 생존/개발 단위 14 보스 HP UI 추가 (Game Scene)")]
        public static void BuildBossHud()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 14 보스 HP UI",
                    "BattleHud에 보스 체력 바를 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            BattleHudController hud = SceneBuilderCleanup.FindFirstInScene<BattleHudController>(scene);
            if (hud == null)
            {
                EditorUtility.DisplayDialog("오류", "BattleHudController를 찾지 못했습니다. 먼저 Unit10을 적용하세요.", "확인");
                return;
            }

            Transform hudTransform = hud.transform;
            SceneBuilderCleanup.DestroyAllNamed(scene, "BossHpRoot");

            var root = new GameObject("BossHpRoot", typeof(RectTransform), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(hudTransform, false);
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -200f);
            rootRect.sizeDelta = new Vector2(720f, 90f);
            root.GetComponent<Image>().color = new Color(0.1f, 0.05f, 0.05f, 0.75f);

            Text nameLabel = CreateText(root.transform, "BossNameLabel", "보스", 26, new Vector2(0f, 28f), new Vector2(680f, 36f));
            Slider slider = CreateSlider(root.transform, "BossHpSlider", new Vector2(0f, -10f), new Vector2(640f, 28f));
            Text hpLabel = CreateText(root.transform, "BossHpLabel", "보스 0/0", 22, new Vector2(0f, -10f), new Vector2(640f, 28f));
            hpLabel.raycastTarget = false;

            var so = new SerializedObject(hud);
            so.FindProperty("bossHpRoot").objectReferenceValue = root;
            so.FindProperty("bossHpSlider").objectReferenceValue = slider;
            so.FindProperty("bossHpLabel").objectReferenceValue = hpLabel;
            so.FindProperty("bossNameLabel").objectReferenceValue = nameLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog(
                "완료",
                "보스 HP UI를 추가했습니다.\n5번째 역 보스 등장 시 표시됩니다.",
                "확인");
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
            text.font = LastTrain.UI.GameFontProvider.Get();
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
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.SetParent(rect, false);
            Stretch(bgRect);
            bg.GetComponent<Image>().color = new Color(0.25f, 0.1f, 0.1f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.SetParent(rect, false);
            Stretch(fillAreaRect);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            Stretch(fillRect);
            fill.GetComponent<Image>().color = new Color(0.85f, 0.2f, 0.25f, 1f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
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
