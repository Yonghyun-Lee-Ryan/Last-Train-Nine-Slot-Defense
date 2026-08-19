using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// EDM Android Resolver가 mainTemplate.gradle을 복사하기 전에
    /// Assets/Plugins/Android 폴더가 존재하도록 보장한다.
    /// </summary>
    public static class AndroidGradleBootstrap
    {
        private const string AndroidPluginsPath = "Assets/Plugins/Android";
        private const string MainTemplatePath = AndroidPluginsPath + "/mainTemplate.gradle";

        [InitializeOnLoadMethod]
        private static void EnsureOnLoad()
        {
            EditorApplication.delayCall += EnsureAndroidPluginFolder;
        }

        [MenuItem("Tools/막차 생존/Integration/Ensure Android Gradle Folder")]
        public static void EnsureFromMenu()
        {
            EnsureAndroidPluginFolder();
            EditorUtility.DisplayDialog(
                "Android Gradle",
                File.Exists(MainTemplatePath)
                    ? "Assets/Plugins/Android 폴더가 준비되어 있습니다.\n" +
                      "External Dependency Manager → Android Resolver → Force Resolve를 실행하세요."
                    : "Assets/Plugins/Android 폴더를 생성했습니다.\n" +
                      "Player Settings → Publishing Settings에서 Custom Main Gradle Template 등을 켠 뒤\n" +
                      "External Dependency Manager → Android Resolver → Force Resolve를 실행하세요.",
                "확인");
        }

        private static void EnsureAndroidPluginFolder()
        {
            EnsureFolder("Assets", "Plugins");
            EnsureFolder("Assets/Plugins", "Android");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
            Debug.Log("[AndroidGradleBootstrap] Created " + path);
        }
    }
}
