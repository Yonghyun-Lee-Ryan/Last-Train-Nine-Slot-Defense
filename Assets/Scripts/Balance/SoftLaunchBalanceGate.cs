using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Performance;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.Simulation;
using UnityEngine;

namespace LastTrain.Balance
{
    public sealed class SoftLaunchScenarioResult
    {
        public string Name;
        public string LineId;
        public int Iterations;
        public float WinRate;
        public float AvgSimulatedSeconds;
        public int PlacedPassengerHits;
        public bool Completed;
        public string Failure = string.Empty;
    }

    public sealed class SoftLaunchGateResult
    {
        public bool Passed;
        public readonly List<SoftLaunchScenarioResult> Scenarios = new();
        public bool FramePolicyOk;
        public bool ContentCatalogOk;
        public string Markdown = string.Empty;
    }

    /// <summary>Unit 54 Soft Launch 밸런스·콘텐츠 스모크 게이트.</summary>
    public static class SoftLaunchBalanceGate
    {
        public static SoftLaunchGateResult Evaluate(GameDatabase database)
        {
            var result = new SoftLaunchGateResult();
            result.FramePolicyOk = LowEndFramePolicy.TargetFrameRate == 60
                                   && LowEndFramePolicy.FrameBudgetMilliseconds <= 17
                                   && LowEndFramePolicy.FrameBudgetMilliseconds > 0;
            result.ContentCatalogOk = HasRequiredContent(database);

            if (database == null)
            {
                result.Passed = false;
                result.Markdown = "# Soft Launch Gate\n\nGameDatabase 없음\n";
                return result;
            }

            BattleSimulationConfig[] scenarios =
            {
                CreateBaselineScenario(),
                CreateContentPackScenario(),
                CreateQuickRunScenario(),
            };

            var sim = new HeadlessCombatSimulator();
            for (int i = 0; i < scenarios.Length; i++)
            {
                result.Scenarios.Add(RunScenario(sim, database, scenarios[i]));
            }

            bool scenariosOk = true;
            for (int i = 0; i < result.Scenarios.Count; i++)
            {
                if (!result.Scenarios[i].Completed)
                {
                    scenariosOk = false;
                    break;
                }
            }

            result.Passed = result.FramePolicyOk && result.ContentCatalogOk && scenariosOk;
            result.Markdown = ToMarkdown(result);
            return result;
        }

        public static BattleSimulationConfig CreateBaselineScenario()
        {
            return CreateConfig(
                "line1_baseline",
                RouteIds.Default,
                1,
                2,
                "passenger_office_worker",
                "passenger_delivery",
                "passenger_trainer");
        }

        public static BattleSimulationConfig CreateContentPackScenario()
        {
            return CreateConfig(
                "line1_content_pack",
                RouteIds.Default,
                1,
                2,
                MetaProgressionDefaults.PassengerConductorId,
                MetaProgressionDefaults.PassengerBaristaId,
                MetaProgressionDefaults.PassengerSecurityId,
                MetaProgressionDefaults.PassengerStudentId,
                "passenger_office_worker");
        }

        public static BattleSimulationConfig CreateQuickRunScenario()
        {
            var config = CreateConfig(
                "quick_run",
                RouteIds.Quick,
                1,
                5,
                MetaProgressionDefaults.PassengerConductorId,
                "passenger_office_worker",
                MetaProgressionDefaults.PassengerBaristaId);
            config.maxSimulatedSeconds = 90f;
            return config;
        }

        private static BattleSimulationConfig CreateConfig(
            string name,
            string lineId,
            int startStation,
            int maxStation,
            params string[] passengerIds)
        {
            var slots = new BattleSimulationSlotConfig[RunState.GridSlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            int count = Math.Min(passengerIds.Length, slots.Length);
            for (int i = 0; i < count; i++)
            {
                slots[i] = new BattleSimulationSlotConfig
                {
                    passengerId = passengerIds[i],
                    starLevel = 1,
                };
            }

            return new BattleSimulationConfig
            {
                baseSeed = 54,
                iterations = 3,
                deltaTime = 0.2f,
                maxSimulatedSeconds = 60f,
                startingStationIndex = startStation,
                maxStationIndex = maxStation,
                difficultyMultiplier = 0.6f,
                difficultyId = DifficultyIds.Normal,
                lineId = lineId,
                initialTrainHp = 160,
                initialCoins = 50,
                slots = slots,
                abilityIds = Array.Empty<string>(),
                autoContinueAbilityRewards = true,
            };
        }

        private static SoftLaunchScenarioResult RunScenario(
            HeadlessCombatSimulator sim,
            GameDatabase database,
            BattleSimulationConfig config)
        {
            var row = new SoftLaunchScenarioResult
            {
                Name = Describe(config),
                LineId = config.lineId,
                Iterations = config.iterations,
            };

            try
            {
                BattleSimulationAggregate aggregate = sim.RunBatch(config, database);
                row.WinRate = aggregate.WinRate;
                row.AvgSimulatedSeconds = aggregate.AvgSimulatedSeconds;
                row.PlacedPassengerHits = CountPassengerHits(aggregate, config);
                bool timed = aggregate.AvgSimulatedSeconds > 0.01f && aggregate.Runs.Count == config.iterations;
                bool placed = row.PlacedPassengerHits > 0;
                row.Completed = timed && placed;
                if (!timed)
                {
                    row.Failure = "시뮬 시간이 0입니다.";
                }
                else if (!placed)
                {
                    row.Failure = "배치 승객 타격이 없습니다.";
                }
            }
            catch (Exception ex)
            {
                row.Completed = false;
                row.Failure = ex.Message;
            }

            return row;
        }

        private static int CountPassengerHits(BattleSimulationAggregate aggregate, BattleSimulationConfig config)
        {
            if (aggregate?.AvgDamageByPassengerId == null || config?.slots == null)
            {
                return 0;
            }

            int hits = 0;
            for (int i = 0; i < config.slots.Length; i++)
            {
                string id = config.slots[i]?.passengerId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (aggregate.AvgDamageByPassengerId.TryGetValue(id, out float dmg) && dmg > 0f)
                {
                    hits++;
                    continue;
                }

                if (aggregate.AvgSkillTicksByPassengerId != null
                    && aggregate.AvgSkillTicksByPassengerId.TryGetValue(id, out float ticks)
                    && ticks > 0f)
                {
                    hits++;
                }
            }

            return hits;
        }

        private static bool HasRequiredContent(GameDatabase database)
        {
            if (database == null)
            {
                return false;
            }

            string[] ids =
            {
                MetaProgressionDefaults.PassengerConductorId,
                MetaProgressionDefaults.PassengerBaristaId,
                MetaProgressionDefaults.PassengerSecurityId,
                MetaProgressionDefaults.PassengerStudentId,
            };

            for (int i = 0; i < ids.Length; i++)
            {
                if (!database.TryGetPassenger(ids[i], out _))
                {
                    return false;
                }
            }

            return database.TryGetRoute(RouteIds.Quick, out RouteData quick)
                   && quick != null
                   && database.GetRouteStationCount(RouteIds.Quick) == 5;
        }

        private static string Describe(BattleSimulationConfig config)
        {
            if (string.Equals(config.lineId, RouteIds.Quick, StringComparison.Ordinal))
            {
                return "quick_run";
            }

            if (config.slots != null
                && config.slots.Length > 0
                && config.slots[0] != null
                && string.Equals(
                    config.slots[0].passengerId,
                    MetaProgressionDefaults.PassengerConductorId,
                    StringComparison.Ordinal))
            {
                return "line1_content_pack";
            }

            return "line1_baseline";
        }

        private static string ToMarkdown(SoftLaunchGateResult result)
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine("# Soft Launch Balance Gate");
            sb.AppendLine();
            sb.AppendLine($"- Passed: {(result.Passed ? "YES" : "NO")}");
            sb.AppendLine($"- Frame policy 60 FPS / {LowEndFramePolicy.FrameBudgetMilliseconds}ms: {(result.FramePolicyOk ? "OK" : "FAIL")}");
            sb.AppendLine($"- Content catalog (승객 +4 · Quick 5역): {(result.ContentCatalogOk ? "OK" : "FAIL")}");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Line | Iter | WinRate | AvgSec | Hits | Result |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|---|");
            for (int i = 0; i < result.Scenarios.Count; i++)
            {
                SoftLaunchScenarioResult row = result.Scenarios[i];
                sb.Append("| ").Append(row.Name)
                    .Append(" | ").Append(row.LineId)
                    .Append(" | ").Append(row.Iterations)
                    .Append(" | ").Append(row.WinRate.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(row.AvgSimulatedSeconds.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(row.PlacedPassengerHits)
                    .Append(" | ").Append(row.Completed ? "PASS" : row.Failure)
                    .AppendLine(" |");
            }

            return sb.ToString();
        }
    }
}
