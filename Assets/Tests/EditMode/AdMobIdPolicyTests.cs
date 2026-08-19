using System.IO;
using LastTrain.Integrations;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public sealed class AdMobIdPolicyTests
    {
        [Test]
        public void GoogleSampleIds_AreAccepted()
        {
            Assert.IsTrue(AdMobIdPolicy.IsGoogleSampleAdMobId(AdMobIdPolicy.GoogleSampleAppId));
            Assert.IsTrue(AdMobIdPolicy.IsGoogleSampleAdMobId(AdMobIdPolicy.GoogleSampleRewardedAndroid));
            Assert.IsTrue(AdMobIdPolicy.IsGoogleSampleAdMobId(AdMobIdPolicy.GoogleSampleInterstitialAndroid));
            Assert.IsTrue(AdMobIdPolicy.IsGoogleSampleAdMobId(string.Empty));
        }

        [Test]
        public void NonSamplePublisher_IsRejected()
        {
            Assert.IsFalse(AdMobIdPolicy.IsGoogleSampleAdMobId("ca-app-pub-0000000000000000/1234567890"));
            Assert.IsTrue(AdMobIdPolicy.TryFindNonSampleAdMobId(
                "id=ca-app-pub-0000000000000000~111",
                out string found));
            Assert.AreEqual("ca-app-pub-0000000000000000~111", found);
        }

        [Test]
        public void TrackedAssets_ContainNoNonSampleAdMobIds()
        {
            string root = Application.dataPath;
            Assert.IsTrue(Directory.Exists(root));
            string[] files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
            var leaks = new System.Collections.Generic.List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".psd" or ".tga"
                    or ".wav" or ".mp3" or ".ogg" or ".dll" or ".so" or ".bin")
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch
                {
                    continue;
                }

                if (AdMobIdPolicy.TryFindNonSampleAdMobId(text, out string found))
                {
                    leaks.Add(path.Replace('\\', '/') + " -> " + found);
                }
            }

            Assert.IsEmpty(leaks, "GitHub에 올라가면 안 되는 AdMob ID:\n" + string.Join("\n", leaks));
        }
    }
}
