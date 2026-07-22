using LastTrain.Release;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class GameSettingsServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("lasttrain.settings.bgm");
            PlayerPrefs.DeleteKey("lasttrain.settings.sfx");
            PlayerPrefs.DeleteKey("lasttrain.settings.vibration");
            PlayerPrefs.DeleteKey("lasttrain.settings.notification");
            PlayerPrefs.Save();
        }

        [Test]
        public void Load_Defaults_AllEnabled()
        {
            var settings = new GameSettingsService();
            settings.Load();

            Assert.IsTrue(settings.BgmEnabled);
            Assert.IsTrue(settings.SfxEnabled);
            Assert.IsTrue(settings.VibrationEnabled);
            Assert.IsTrue(settings.NotificationsEnabled);
        }

        [Test]
        public void SetSfxEnabled_PersistsAcrossInstances()
        {
            var first = new GameSettingsService();
            first.Load();
            first.SetSfxEnabled(false);

            var second = new GameSettingsService();
            second.Load();

            Assert.IsFalse(second.SfxEnabled);
        }

        [Test]
        public void ResetToDefaults_RestoresEnabledState()
        {
            var settings = new GameSettingsService();
            settings.Load();
            settings.SetBgmEnabled(false);
            settings.ResetToDefaults();

            Assert.IsTrue(settings.BgmEnabled);
        }
    }
}
