using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Run;

namespace LastTrain.Battle
{
    public static class StationBriefingBuilder
    {
        public static StationBriefing Build(
            StationData station,
            DifficultyRuntime difficulty,
            RunState runState = null)
        {
            var briefing = new StationBriefing();
            if (station == null)
            {
                return briefing;
            }

            briefing.StationName = station.DisplayName;
            briefing.StationTypeLabel = ToTypeLabel(station.StationType);
            briefing.RewardCoins = DifficultyCalculator.ApplyStationReward(
                station.RewardCoins,
                difficulty);
            briefing.BossPatternHint = station.BossPatternHint ?? string.Empty;
            briefing.ModifierHint = BuildModifierHint(difficulty, runState);

            if (StationTypeRules.UsesWaveManager(station.StationType))
            {
                CollectEnemyPreview(station, briefing);
            }
            else
            {
                briefing.EnemyTags = StationTypeRules.GetNonCombatDescription(station.StationType);
            }

            return briefing;
        }

        private static void CollectEnemyPreview(StationData station, StationBriefing briefing)
        {
            var tags = new HashSet<string>();
            IReadOnlyList<WaveData> waves = station.Waves;
            if (waves == null)
            {
                return;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                WaveData wave = waves[i];
                if (wave?.Spawns == null)
                {
                    continue;
                }

                for (int j = 0; j < wave.Spawns.Count; j++)
                {
                    EnemyData enemy = wave.Spawns[j].enemy;
                    if (enemy == null)
                    {
                        continue;
                    }

                    tags.Add(EnemyTagLabel(enemy.EnemyType));
                    if (enemy.EnemyType == EnemyType.Fast)
                    {
                        briefing.HasFastEnemy = true;
                    }

                    if (enemy.EnemyType == EnemyType.Tank)
                    {
                        briefing.HasTankEnemy = true;
                    }

                    if (enemy.EnemyType == EnemyType.Elite)
                    {
                        briefing.HasEliteEnemy = true;
                    }

                    if (enemy.EnemyType == EnemyType.Boss && string.IsNullOrWhiteSpace(briefing.BossPatternHint))
                    {
                        briefing.BossPatternHint = "강력한 보스가 등장합니다.";
                    }
                }
            }

            briefing.EnemyTags = string.Join(", ", tags);
            if (station.StationType == StationType.Elite)
            {
                briefing.HasEliteEnemy = true;
            }
        }

        private static string EnemyTagLabel(EnemyType type)
        {
            return type switch
            {
                EnemyType.Fast => "빠른 적",
                EnemyType.Tank => "방어형",
                EnemyType.Boss => "보스",
                EnemyType.Elite => "정예",
                _ => "일반",
            };
        }

        private static string ToTypeLabel(StationType type)
        {
            return type switch
            {
                StationType.Tutorial => "튜토리얼",
                StationType.Normal => "일반 전투",
                StationType.Elite => "정예 전투",
                StationType.Event => "이벤트",
                StationType.Shop => "상점",
                StationType.Boss => "보스",
                StationType.Rest => "휴식",
                _ => type.ToString(),
            };
        }

        private static string BuildModifierHint(DifficultyRuntime difficulty, RunState runState)
        {
            string difficultyHint = difficulty != null && difficulty.Id != DifficultyIds.Normal
                ? $"난이도: {difficulty.DisplayName}"
                : string.Empty;
            string dailyHint = string.Empty;
            if (runState != null && runState.IsDailyRun && !string.IsNullOrWhiteSpace(runState.DailyRuleDisplayName))
            {
                dailyHint = $"오늘 규칙: {runState.DailyRuleDisplayName}";
                if (runState.LockedSlotIndex >= 0)
                {
                    dailyHint += $" ({runState.LockedSlotIndex + 1}번 칸 잠김)";
                }

                if (runState.DailyEnemySpeedMultiplier > 1.01f)
                {
                    int percent = UnityEngine.Mathf.RoundToInt((runState.DailyEnemySpeedMultiplier - 1f) * 100f);
                    dailyHint += $" (이속 +{percent}%)";
                }

                if (UnityEngine.Mathf.Abs(runState.DailySummonCostMultiplier - 1f) > 0.01f)
                {
                    int percent = UnityEngine.Mathf.RoundToInt((runState.DailySummonCostMultiplier - 1f) * 100f);
                    dailyHint += percent < 0
                        ? $" (소환 {percent}%)"
                        : $" (소환 +{percent}%)";
                }
            }

            if (string.IsNullOrEmpty(difficultyHint))
            {
                return dailyHint;
            }

            if (string.IsNullOrEmpty(dailyHint))
            {
                return difficultyHint;
            }

            return difficultyHint + " / " + dailyHint;
        }
    }
}
