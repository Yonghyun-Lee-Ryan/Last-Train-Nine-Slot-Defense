using System;
using LastTrain.Difficulty;
using LastTrain.Run;

namespace LastTrain.Score
{
    /// <summary>점수 계산 입력. 동일 입력이면 ScoreCalculator 결과가 항상 같다.</summary>
    public readonly struct ScoreInput
    {
        public ScoreInput(
            int reachedStationIndex,
            int completedStationCount,
            int enemiesKilled,
            int bossesKilled,
            int remainingTrainHp,
            string difficultyId,
            bool adsUsed)
        {
            ReachedStationIndex = Math.Max(0, reachedStationIndex);
            CompletedStationCount = Math.Max(0, completedStationCount);
            EnemiesKilled = Math.Max(0, enemiesKilled);
            BossesKilled = Math.Max(0, bossesKilled);
            RemainingTrainHp = Math.Max(0, remainingTrainHp);
            DifficultyId = string.IsNullOrWhiteSpace(difficultyId)
                ? DifficultyIds.Normal
                : difficultyId;
            AdsUsed = adsUsed;
        }

        public int ReachedStationIndex { get; }
        public int CompletedStationCount { get; }
        public int EnemiesKilled { get; }
        public int BossesKilled { get; }
        public int RemainingTrainHp { get; }
        public string DifficultyId { get; }
        public bool AdsUsed { get; }

        public static ScoreInput FromRunResult(RunResult result)
        {
            if (result == null)
            {
                return new ScoreInput(0, 0, 0, 0, 0, DifficultyIds.Normal, adsUsed: true);
            }

            return new ScoreInput(
                result.ReachedStationIndex,
                result.CompletedStationCount,
                result.EnemiesKilled,
                result.BossesKilled,
                result.RemainingTrainHp,
                result.DifficultyId,
                result.AdsUsed);
        }
    }

    public readonly struct ScoreBreakdown
    {
        public ScoreBreakdown(
            int stationScore,
            int killScore,
            int bossScore,
            int remainingHpScore,
            int difficultyBonus,
            int noAdsBonus,
            int total)
        {
            StationScore = stationScore;
            KillScore = killScore;
            BossScore = bossScore;
            RemainingHpScore = remainingHpScore;
            DifficultyBonus = difficultyBonus;
            NoAdsBonus = noAdsBonus;
            Total = total;
        }

        public int StationScore { get; }
        public int KillScore { get; }
        public int BossScore { get; }
        public int RemainingHpScore { get; }
        public int DifficultyBonus { get; }
        public int NoAdsBonus { get; }
        public int Total { get; }
    }

    /// <summary>결정적 점수 계산. 부동 연산만 사용한다.</summary>
    public static class ScoreCalculator
    {
        public const int PointsPerReachedStation = 100;
        public const int PointsPerCompletedStation = 25;
        public const int PointsPerKill = 5;
        public const int PointsPerBossKill = 250;
        public const int PointsPerRemainingHp = 2;
        public const int PointsPerDifficultyRankPerStation = 20;
        public const int PointsNoAdsPerCompletedStation = 15;

        public static int Calculate(ScoreInput input)
        {
            return CalculateBreakdown(input).Total;
        }

        public static ScoreBreakdown CalculateBreakdown(ScoreInput input)
        {
            int stationScore =
                input.ReachedStationIndex * PointsPerReachedStation
                + input.CompletedStationCount * PointsPerCompletedStation;
            int killScore = input.EnemiesKilled * PointsPerKill;
            int bossScore = input.BossesKilled * PointsPerBossKill;
            int remainingHpScore = input.RemainingTrainHp * PointsPerRemainingHp;
            int difficultyRank = DifficultyRank(input.DifficultyId);
            int difficultyBonus = input.CompletedStationCount * difficultyRank * PointsPerDifficultyRankPerStation;
            int noAdsBonus = input.AdsUsed
                ? 0
                : input.CompletedStationCount * PointsNoAdsPerCompletedStation;

            int total = stationScore + killScore + bossScore + remainingHpScore + difficultyBonus + noAdsBonus;
            return new ScoreBreakdown(
                stationScore,
                killScore,
                bossScore,
                remainingHpScore,
                difficultyBonus,
                noAdsBonus,
                total);
        }

        public static int DifficultyRank(string difficultyId)
        {
            if (string.Equals(difficultyId, DifficultyIds.NonstopHell, StringComparison.Ordinal))
            {
                return 3;
            }

            if (string.Equals(difficultyId, DifficultyIds.MidnightExpress, StringComparison.Ordinal))
            {
                return 2;
            }

            if (string.Equals(difficultyId, DifficultyIds.Express, StringComparison.Ordinal))
            {
                return 1;
            }

            return 0;
        }
    }
}
