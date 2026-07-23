using System.IO;
using LastTrain.Balance;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Simulation;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit33BalanceValidationMenu
    {
        [MenuItem("Tools/막차 생존/개발 단위 33 밸런스 검증 실행")]
        public static void Run()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            if (database == null)
            {
                EditorUtility.DisplayDialog("실패", "GameDatabase를 로드하지 못했습니다.", "확인");
                return;
            }

            BalanceTargetData targets = BalanceTargetData.CreateDefaultRuntime();
            var sim = new HeadlessCombatSimulator();
            var config = CreateConfig(DifficultyIds.Normal, difficultyMult: 1f);
            BattleSimulationAggregate aggregate = sim.RunBatch(config, database);
            BalanceReport report = BalanceReportBuilder.FromAggregate(aggregate, "sim_normal");
            // 고정 슬롯 스모크 시뮬에서는 픽률 100% 경고가 노이즈이므로 대상에서 제외
            BalanceValidator.ApplyTargets(report, targets, ignoreFixedLoadoutPickRates: true);

            string dir = Path.Combine(Application.dataPath, "../BalanceReports");
            string csv = BalanceReportExporter.WriteFiles(report, dir, "balance_normal");

            // 간단한 before/after diff (같은 리포트 복제 후 HP 지표만 변형)
            BalanceReport after = BalanceReportBuilder.FromAggregate(aggregate, "sim_normal_b");
            after.AddMetric(BalanceMetricIds.AvgRemainingHp, aggregate.AvgRemainingHp + 1f, aggregate.DifficultyId);
            BalanceDiffReport diff = BalanceDiffBuilder.Compare(report, after);
            File.WriteAllText(Path.Combine(dir, "balance_diff.md"), BalanceDiffBuilder.ToMarkdown(diff));

            Object.DestroyImmediate(targets);
            EditorUtility.DisplayDialog(
                "완료",
                "밸런스 리포트 생성\n" +
                $"경고 {report.Warnings.Count}건 (스모크 시뮬 고정 덱·쉬운 난이도라 정상적으로 많이 뜹니다)\n" +
                "자세한 내용은 BalanceReports/balance_normal.md 를 확인하세요.\n\n" +
                csv,
                "확인");
            if (!string.IsNullOrEmpty(csv))
            {
                EditorUtility.RevealInFinder(csv);
            }
        }

        private static BattleSimulationConfig CreateConfig(string difficultyId, float difficultyMult)
        {
            var slots = new BattleSimulationSlotConfig[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            slots[0] = new BattleSimulationSlotConfig { passengerId = "passenger_office_worker", starLevel = 1 };
            slots[1] = new BattleSimulationSlotConfig { passengerId = "passenger_delivery", starLevel = 1 };
            slots[2] = new BattleSimulationSlotConfig { passengerId = "passenger_trainer", starLevel = 1 };

            return new BattleSimulationConfig
            {
                baseSeed = 42,
                iterations = 8,
                deltaTime = 0.1f,
                maxSimulatedSeconds = 180f,
                startingStationIndex = 1,
                maxStationIndex = 3,
                difficultyMultiplier = difficultyMult,
                difficultyId = difficultyId,
                initialCoins = 50,
                initialTrainHp = 100,
                slots = slots,
                autoContinueAbilityRewards = true,
            };
        }
    }
}
