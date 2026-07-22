using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LastTrain.EditorTools;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// 개발 단위 15: Result(게임오버/성공) 씬에 통계 UI를 추가/연결한다.
    /// </summary>
    public static class Unit15ResultSceneBuilder
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";

        [MenuItem("Tools/막차 생존/개발 단위 15 결과 화면 UI 추가 (Result Scene)")]
        public static void BuildResultUi()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 15 결과 화면 UI",
                    "Result Scene에 성공/실패 + 통계를 표시하는 라벨을 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            var controller = SceneBuilderCleanup.FindFirstInScene<ResultPlaceholderController>(scene);
            if (controller == null)
            {
                EditorUtility.DisplayDialog(
                    "오류",
                    "ResultPlaceholderController를 찾지 못했습니다. 먼저 개발 단위 1 Scene 생성을 실행하세요.",
                    "확인");
                return;
            }

            Transform safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null)
            {
                EditorUtility.DisplayDialog("오류", "SafeArea를 찾지 못했습니다.", "확인");
                return;
            }

            Text titleText = GameObject.Find("Title")?.GetComponent<Text>();
            if (titleText == null)
            {
                EditorUtility.DisplayDialog("오류", "Title(Text)를 찾지 못했습니다.", "확인");
                return;
            }

            Button retryButton = GameObject.Find("RetryButton")?.GetComponent<Button>();
            Button mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
            if (retryButton == null || mainMenuButton == null)
            {
                EditorUtility.DisplayDialog("오류", "결과 화면 버튼을 찾지 못했습니다.", "확인");
                return;
            }

            SceneBuilderCleanup.DestroyAllNamed(scene, "MessageLabel");
            SceneBuilderCleanup.DestroyAllNamed(scene, "StatsLabel");

            SetRect(titleText.rectTransform, new Vector2(0, 590), new Vector2(900, 140));
            titleText.fontSize = 52;

            Text messageLabel = CreateText(safeArea, "MessageLabel", "", 42, new Vector2(0, 410), new Vector2(900, 110));
            Text statsLabel = CreateText(
                safeArea,
                "StatsLabel",
                "",
                30,
                new Vector2(0, 90),
                new Vector2(820, 420));

            SetRect(retryButton.GetComponent<RectTransform>(), new Vector2(0, -280), new Vector2(600, 140));
            SetRect(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0, -470), new Vector2(600, 140));

            Button doubleRewardButton = EnsureDoubleRewardButton(safeArea, retryButton);
            ResultUiLayout.EnsureButtonGroup(retryButton, doubleRewardButton, mainMenuButton);

            var so = new SerializedObject(controller);
            SerializedProperty titleProp = so.FindProperty("titleLabel");
            SerializedProperty msgProp = so.FindProperty("messageLabel");
            SerializedProperty statsProp = so.FindProperty("statsLabel");
            SerializedProperty adProp = so.FindProperty("doubleRewardAdButton");

            if (titleProp != null) titleProp.objectReferenceValue = titleText;
            if (msgProp != null) msgProp.objectReferenceValue = messageLabel;
            if (statsProp != null) statsProp.objectReferenceValue = statsLabel;
            if (adProp != null) adProp.objectReferenceValue = doubleRewardButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("완료", "결과 화면 UI를 추가했습니다.", "확인");
        }

        private static Button EnsureDoubleRewardButton(Transform safeArea, Button template)
        {
            Transform existing = safeArea.Find("DoubleRewardAdButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            GameObject clone = Object.Instantiate(template.gameObject, safeArea);
            clone.name = "DoubleRewardAdButton";
            Button button = clone.GetComponent<Button>();
            Text label = clone.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "광고로 보상 2배";
            }

            return button;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.font = GetBuiltinFont();
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font GetBuiltinFont()
        {
            return LastTrain.UI.GameFontProvider.Get();
        }
    }
}

