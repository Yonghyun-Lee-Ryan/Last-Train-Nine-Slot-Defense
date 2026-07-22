using System.IO;
using LastTrain.Core;
using LastTrain.Integrations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// Integration ScriptableObject와 Bootstrap AppRoot 참조를 Unity AssetDatabase로 생성한다.
    /// 손상된 .meta 파일이 있을 때 자동 복구한다.
    /// </summary>
    public static class IntegrationAssetsBuilder
    {
        private const string IntegrationFolder = "Assets/Data/Integration";
        private const string AdUnitConfigPath = IntegrationFolder + "/AdUnitConfig.asset";
        private const string RemoteConfigPath = IntegrationFolder + "/RemoteConfigDefaults.asset";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [MenuItem("Tools/막차 생존/Integration/Setup Integration Assets")]
        public static void SetupFromMenu()
        {
            Setup(showDialog: true);
        }

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (NeedsSetup())
                {
                    Setup(showDialog: false);
                }
            };
        }

        private static bool NeedsSetup()
        {
            if (!AssetDatabase.IsValidFolder(IntegrationFolder))
            {
                return true;
            }

            if (AssetDatabase.LoadAssetAtPath<AdUnitConfig>(AdUnitConfigPath) == null)
            {
                return true;
            }

            return AssetDatabase.LoadAssetAtPath<RemoteConfigDefaults>(RemoteConfigPath) == null;
        }

        private static void Setup(bool showDialog)
        {
            RemoveBrokenIntegrationMeta();

            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Integration");

            AdUnitConfig adUnits = LoadOrCreate<AdUnitConfig>(AdUnitConfigPath);
            RemoteConfigDefaults remoteDefaults = LoadOrCreate<RemoteConfigDefaults>(RemoteConfigPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireBootstrapScene(adUnits, remoteDefaults);

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Integration Assets",
                    "AdUnitConfig / RemoteConfigDefaults 생성 및 Bootstrap AppRoot 연결을 완료했습니다.",
                    "확인");
            }
            else
            {
                Debug.Log("[IntegrationAssetsBuilder] Integration assets restored.");
            }
        }

        private static void RemoveBrokenIntegrationMeta()
        {
            string metaPath = IntegrationFolder + ".meta";
            if (!File.Exists(metaPath))
            {
                return;
            }

            if (AssetDatabase.IsValidFolder(IntegrationFolder))
            {
                return;
            }

            File.Delete(metaPath);
            Debug.LogWarning("[IntegrationAssetsBuilder] Removed invalid Integration.meta. Reimporting folder.");
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        private static void WireBootstrapScene(AdUnitConfig adUnits, RemoteConfigDefaults remoteDefaults)
        {
            if (!File.Exists(BootstrapScenePath))
            {
                return;
            }

            Scene bootstrapScene = EditorSceneManager.GetSceneByPath(BootstrapScenePath);
            bool openedByUs = false;

            if (!bootstrapScene.isLoaded)
            {
                bootstrapScene = EditorSceneManager.sceneCount == 0
                    ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single)
                    : EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                openedByUs = EditorSceneManager.sceneCount > 1;
            }

            AppRoot appRoot = SceneBuilderCleanup.FindFirstInScene<AppRoot>(bootstrapScene);

            if (appRoot == null)
            {
                CloseBootstrapSceneIfNeeded(bootstrapScene, openedByUs);
                return;
            }

            SerializedObject serialized = new SerializedObject(appRoot);
            SerializedProperty adProp = serialized.FindProperty("adUnitConfig");
            SerializedProperty remoteProp = serialized.FindProperty("remoteConfigDefaults");

            if (adProp != null)
            {
                adProp.objectReferenceValue = adUnits;
            }

            if (remoteProp != null)
            {
                remoteProp.objectReferenceValue = remoteDefaults;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(bootstrapScene);
            EditorSceneManager.SaveScene(bootstrapScene);
            CloseBootstrapSceneIfNeeded(bootstrapScene, openedByUs);
        }

        private static void CloseBootstrapSceneIfNeeded(Scene bootstrapScene, bool openedByUs)
        {
            if (!openedByUs || !bootstrapScene.isLoaded || EditorSceneManager.sceneCount <= 1)
            {
                return;
            }

            EditorSceneManager.CloseScene(bootstrapScene, removeScene: true);
        }
    }
}
