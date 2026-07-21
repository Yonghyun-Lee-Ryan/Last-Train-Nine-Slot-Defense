using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>Game Scene에 시너지 목록 HUD를 배치한다.</summary>
    public static class Unit13SynergySceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/막차 생존/개발 단위 13 시너지 HUD 추가 (Game Scene)")]
        public static void BuildSynergyHud()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 13 시너지 HUD",
                    "Game Scene 상단에 활성 시너지 목록 라벨을 추가합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("오류", "Canvas를 찾지 못했습니다.", "확인");
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            // 중복 방지: 기존 SynergyHud / SynergyListLabel 제거
            SceneBuilderCleanup.DestroyAllComponents<SynergyHudController>(scene);
            SceneBuilderCleanup.DestroyAllNamed(scene, "SynergyListLabel");

            var hud = new GameObject("SynergyHud", typeof(RectTransform), typeof(SynergyHudController));
            var hudRect = hud.GetComponent<RectTransform>();
            hudRect.SetParent(parent, false);
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            Text label = CreateLabel(hud.transform);

            GameBattleBootstrap bootstrap = SceneBuilderCleanup.FindFirstInScene<GameBattleBootstrap>(scene);
            var so = new SerializedObject(hud.GetComponent<SynergyHudController>());
            so.FindProperty("synergyLabel").objectReferenceValue = label;
            if (bootstrap != null)
            {
                so.FindProperty("battleBootstrap").objectReferenceValue = bootstrap;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog(
                "완료",
                "시너지 HUD를 추가했습니다.\n승객 배치 시 활성 시너지가 상단에 표시됩니다.",
                "확인");
        }

        private static Text CreateLabel(Transform hudParent)
        {
            var go = new GameObject("SynergyListLabel", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(hudParent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = new Vector2(1000f, 40f);

            var text = go.GetComponent<Text>();
            text.text = "시너지: 없음";
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.92f, 0.55f, 1f);
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Font.CreateDynamicFontFromOSFont("Malgun Gothic", 24);
            return text;
        }
    }
}
