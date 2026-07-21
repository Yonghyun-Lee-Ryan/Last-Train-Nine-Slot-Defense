using LastTrain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTrain.EditorTools
{
    /// <summary>씬·프리팹에 직렬화된 legacy Text 폰트를 Jua(OFL)로 일괄 교체한다.</summary>
    public static class GameFontApplier
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Result.unity",
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/UI/PassengerView.prefab",
            "Assets/Prefabs/UI/FloatingCombatText.prefab",
            "Assets/Prefabs/Enemies/BasicEnemy.prefab",
        };

        [MenuItem("Tools/막차 생존/UI/Apply Game Font To Scenes And Prefabs")]
        public static void ApplyToAll()
        {
            Font font = GameFontProvider.Get();
            if (font == null)
            {
                EditorUtility.DisplayDialog("오류", "Jua-Regular.ttf를 Resources/Fonts에서 찾지 못했습니다.", "확인");
                return;
            }

            int updatedTexts = 0;
            for (int i = 0; i < ScenePaths.Length; i++)
            {
                updatedTexts += ApplyToScene(ScenePaths[i], font);
            }

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                updatedTexts += ApplyToPrefab(PrefabPaths[i], font);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "완료",
                $"Jua 폰트를 {updatedTexts}개 Text 컴포넌트에 적용했습니다.",
                "확인");
        }

        private static int ApplyToScene(string scenePath, Font font)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int updated = ApplyToSceneRoots(scene, font);
            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return updated;
        }

        private static int ApplyToSceneRoots(Scene scene, Font font)
        {
            int updated = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                updated += ApplyToHierarchy(roots[i], font);
            }

            return updated;
        }

        private static int ApplyToPrefab(string prefabPath, Font font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int updated = ApplyToHierarchy(root, font);
            if (updated > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
            return updated;
        }

        private static int ApplyToHierarchy(GameObject root, Font font)
        {
            int updated = 0;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].font == font)
                {
                    continue;
                }

                texts[i].font = font;
                updated++;
            }

            return updated;
        }
    }
}
