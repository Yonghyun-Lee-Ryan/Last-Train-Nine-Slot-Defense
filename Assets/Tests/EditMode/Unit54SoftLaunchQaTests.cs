using LastTrain.Balance;
using LastTrain.Data;
using LastTrain.Performance;
using LastTrain.Release;
using LastTrain.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit54SoftLaunchQaTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("lasttrain.settings.lowFx");
        }

        [Test]
        public void FramePolicy_TargetsSixtyFpsBudget()
        {
            Assert.AreEqual(60, LowEndFramePolicy.TargetFrameRate);
            Assert.LessOrEqual(LowEndFramePolicy.FrameBudgetMilliseconds, 17);
            Assert.Greater(LowEndFramePolicy.FrameBudgetMilliseconds, 0);
            Assert.IsTrue(LowEndFramePolicy.ShouldRecommendLowFx(2048));
            Assert.IsFalse(LowEndFramePolicy.ShouldRecommendLowFx(8192));
        }

        [Test]
        public void FramePolicy_ApplySetsTargetFrameRate()
        {
            LowEndFramePolicy.ApplyFrameRate();
            Assert.AreEqual(LowEndFramePolicy.TargetFrameRate, Application.targetFrameRate);
        }

        [Test]
        public void FramePolicy_RecommendsLowFxOnlyWhenUnset()
        {
            PlayerPrefs.DeleteKey("lasttrain.settings.lowFx");
            var settings = new GameSettingsService();
            settings.Load();
            bool recommended = LowEndFramePolicy.TryRecommendLowFx(settings, 1024);
            Assert.IsTrue(recommended);
            Assert.IsTrue(settings.LowFxMode);
            PlayerPrefs.DeleteKey("lasttrain.settings.lowFx");
        }

        [Test]
        public void HeadlessScenarios_IncludeNewPassengersAndQuickRun()
        {
            BattleSimulationConfig content = SoftLaunchBalanceGate.CreateContentPackScenario();
            Assert.AreEqual("passenger_conductor", content.slots[0].passengerId);
            Assert.AreEqual("passenger_student", content.slots[3].passengerId);

            BattleSimulationConfig quick = SoftLaunchBalanceGate.CreateQuickRunScenario();
            Assert.AreEqual(RouteIds.Quick, quick.lineId);
            Assert.AreEqual(5, quick.maxStationIndex);
        }

        [Test]
        public void SoftLaunchGate_PassesWithCurrentCatalog()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            SoftLaunchGateResult result = SoftLaunchBalanceGate.Evaluate(database);
            Assert.IsTrue(result.ContentCatalogOk, result.Markdown);
            Assert.IsTrue(result.FramePolicyOk, result.Markdown);
            Assert.IsTrue(result.Passed, result.Markdown);
            Assert.AreEqual(3, result.Scenarios.Count);
            for (int i = 0; i < result.Scenarios.Count; i++)
            {
                Assert.IsTrue(result.Scenarios[i].Completed, result.Scenarios[i].Failure);
            }
        }

        [Test]
        public void ReleaseQaChecklist_HasSoftLaunchSection()
        {
            string path = PathCombine("Docs", "RELEASE_QA_CHECKLIST.md");
            Assume.That(System.IO.File.Exists(path), "RELEASE_QA_CHECKLIST.md missing");
            string text = System.IO.File.ReadAllText(path);
            StringAssert.Contains("Soft Launch", text);
            StringAssert.Contains("밸런스 게이트", text);
            StringAssert.Contains("저사양", text);
        }

        private static string PathCombine(string folder, string file)
        {
            string root = Application.dataPath.Replace("\\", "/");
            if (root.EndsWith("/Assets"))
            {
                root = root.Substring(0, root.Length - "/Assets".Length);
            }

            return System.IO.Path.Combine(root, folder, file);
        }
    }
}
