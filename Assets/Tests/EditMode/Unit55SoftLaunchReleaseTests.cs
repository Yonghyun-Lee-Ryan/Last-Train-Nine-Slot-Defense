using LastTrain.Release;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit55SoftLaunchReleaseTests
    {
        [Test]
        public void AppReleaseConfig_IsSoftLaunchVersion()
        {
            AppReleaseConfig config = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(
                "Assets/Data/Release/AppReleaseConfig.asset");
            Assert.IsNotNull(config);
            Assert.AreEqual("0.5.0", config.VersionName);
            Assert.GreaterOrEqual(config.AndroidBundleVersionCode, 7);

            AppReleaseConfig resources = AssetDatabase.LoadAssetAtPath<AppReleaseConfig>(
                "Assets/Resources/AppReleaseConfig.asset");
            Assert.IsNotNull(resources);
            Assert.AreEqual(config.VersionName, resources.VersionName);
            Assert.AreEqual(config.AndroidBundleVersionCode, resources.AndroidBundleVersionCode);
        }

        [Test]
        public void PlayConsoleDoc_HasSoftLaunchUploadSection()
        {
            string root = Application.dataPath.Replace("\\", "/");
            if (root.EndsWith("/Assets"))
            {
                root = root.Substring(0, root.Length - "/Assets".Length);
            }

            string path = System.IO.Path.Combine(root, "Docs", "PLAY_CONSOLE_INTERNAL_TEST.md");
            Assert.IsTrue(System.IO.File.Exists(path));
            string text = System.IO.File.ReadAllText(path);
            StringAssert.Contains("Soft Launch", text);
            StringAssert.Contains("0.5.0", text);
            StringAssert.Contains("내부 테스트", text);
        }
    }
}
