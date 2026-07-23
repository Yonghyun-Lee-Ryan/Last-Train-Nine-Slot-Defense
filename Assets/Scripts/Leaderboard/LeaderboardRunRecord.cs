using System;
using LastTrain.Data;

namespace LastTrain.Leaderboard
{
    /// <summary>서버 전송용 랭킹 회차 기록 DTO. 실제 API 페이로드와 1:1로 맞춘다.</summary>
    [Serializable]
    public sealed class LeaderboardRunRecord
    {
        public string anonymousUserId = string.Empty;
        public string gameVersion = string.Empty;
        public string runId = string.Empty;
        public int score;
        public int reachedStation;
        public string difficultyId = string.Empty;
        public float playTimeSeconds;
        public string[] finalPassengerIds = Array.Empty<string>();
        public int randomSeed;
        public string runSummary = string.Empty;
        public string lineId = RouteIds.Endless;
        public string submittedUtc = string.Empty;
    }

    public enum LeaderboardSubmitResult
    {
        Success = 0,
        DuplicateRunId = 1,
        InvalidRecord = 2,
        NotReady = 3,
        Failed = 4,
    }

    /// <summary>비동기 랭킹 전송 인터페이스. 서버 연동 전 Mock만 사용한다.</summary>
    public interface ILeaderboardService
    {
        LeaderboardSubmitResult Submit(LeaderboardRunRecord record);
    }

    /// <summary>서버 없이 성공/검증만 수행하는 Mock.</summary>
    public sealed class MockLeaderboardService : ILeaderboardService
    {
        public int SubmitCount { get; private set; }
        public LeaderboardRunRecord LastSubmitted { get; private set; }

        public LeaderboardSubmitResult Submit(LeaderboardRunRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.runId))
            {
                return LeaderboardSubmitResult.InvalidRecord;
            }

            SubmitCount++;
            LastSubmitted = record;
            return LeaderboardSubmitResult.Success;
        }
    }
}
