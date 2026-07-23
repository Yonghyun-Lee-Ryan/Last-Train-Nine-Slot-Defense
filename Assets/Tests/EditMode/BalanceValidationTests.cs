using LastTrain.Balance;
using LastTrain.Difficulty;
using LastTrain.Simulation;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class BalanceValidationTests
    {
        [Test]
        public void ReportBuilder_FromAggregate_IncludesCoreMetrics()
        {
            var aggregate = new BattleSimulationAggregate
            {
                Iterations = 10,
                Wins = 4,
                WinRate = 0.4f,
                DifficultyId = DifficultyIds.Normal,
                AvgRemainingHp = 30f,
                AvgRemainingCoins = 12f,
                AvgSimulatedSeconds = 90f,
                ReachStation5Rate = 0.8f,
            };
            aggregate.PassengerPickRate["passenger_a"] = 0.5f;
            aggregate.AvgDamageByPassengerId["passenger_a"] = 120f;
            aggregate.SurvivalCurveByStation[1] = 1f;
            aggregate.SurvivalCurveByStation[2] = 0.7f;

            BalanceReport report = BalanceReportBuilder.FromAggregate(aggregate, "t1");
            Assert.AreEqual("simulation", report.Source);
            Assert.IsTrue(report.TryGetMetric(BalanceMetricIds.WinRate, DifficultyIds.Normal, null, out float win));
            Assert.AreEqual(0.4f, win, 0.0001f);
            Assert.AreEqual(0.5f, report.PassengerPickRate["passenger_a"], 0.0001f);
            Assert.AreEqual(120f, report.PassengerDamage["passenger_a"], 0.0001f);
            Assert.AreEqual(0.7f, report.SurvivalCurveByStation[2], 0.0001f);
        }

        [Test]
        public void Validator_MarksCritical_WhenOutsideWarningBand()
        {
            var report = new BalanceReport { DifficultyId = DifficultyIds.Normal };
            report.AddMetric(BalanceMetricIds.WinRate, 0.05f, DifficultyIds.Normal);
            var targets = BalanceTargetData.CreateDefaultRuntime();
            BalanceValidator.ApplyTargets(report, targets);

            Assert.Greater(report.Warnings.Count, 0);
            bool hasCritical = false;
            for (int i = 0; i < report.Warnings.Count; i++)
            {
                if (report.Warnings[i].Severity == BalanceSeverity.Critical
                    && report.Warnings[i].MetricId == BalanceMetricIds.WinRate)
                {
                    hasCritical = true;
                }
            }

            Assert.IsTrue(hasCritical);
            UnityEngine.Object.DestroyImmediate(targets);
        }

        [Test]
        public void AnalyticsCsv_ImportsSameShape()
        {
            const string csv =
                "metric_id,difficulty_id,subject_id,value\n" +
                "win_rate,normal,,0.42\n" +
                "passenger_avg_damage,normal,passenger_a,55.5\n";

            BalanceReport report = BalanceAnalyticsCsvImporter.FromCsv(csv, "analytics");
            Assert.AreEqual("analytics_csv", report.Source);
            Assert.IsTrue(report.TryGetMetric(BalanceMetricIds.WinRate, DifficultyIds.Normal, null, out float win));
            Assert.AreEqual(0.42f, win, 0.0001f);
            Assert.AreEqual(55.5f, report.PassengerDamage["passenger_a"], 0.0001f);
        }

        [Test]
        public void DiffReport_DetectsMetricDelta()
        {
            var before = new BalanceReport { VersionLabel = "A" };
            before.AddMetric(BalanceMetricIds.WinRate, 0.4f, DifficultyIds.Normal);
            var after = new BalanceReport { VersionLabel = "B" };
            after.AddMetric(BalanceMetricIds.WinRate, 0.5f, DifficultyIds.Normal);

            BalanceDiffReport diff = BalanceDiffBuilder.Compare(before, after);
            Assert.AreEqual(1, diff.Entries.Count);
            Assert.AreEqual(0.1f, diff.Entries[0].Delta, 0.0001f);

            string md = BalanceDiffBuilder.ToMarkdown(diff);
            StringAssert.Contains("win_rate", md);
            string csv = BalanceReportExporter.ToCsv(after);
            StringAssert.Contains("win_rate", csv);
            string reportMd = BalanceReportExporter.ToMarkdown(after);
            StringAssert.Contains("Balance Report", reportMd);
        }

        [Test]
        public void Classify_WarningInsideBand_CriticalOutside()
        {
            Assert.AreEqual(BalanceSeverity.None, BalanceValidator.Classify(0.4f, 0.35f, 0.5f, 0.05f));
            Assert.AreEqual(BalanceSeverity.Warning, BalanceValidator.Classify(0.32f, 0.35f, 0.5f, 0.05f));
            Assert.AreEqual(BalanceSeverity.Critical, BalanceValidator.Classify(0.20f, 0.35f, 0.5f, 0.05f));
        }
    }
}
