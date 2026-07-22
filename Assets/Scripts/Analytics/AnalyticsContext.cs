using System;
using System.Collections.Generic;

namespace LastTrain.Analytics
{
    /// <summary>세션/런 공통 파라미터. SessionId와 RunId를 분리한다.</summary>
    public sealed class AnalyticsContext
    {
        public const string DefaultDifficultyId = Difficulty.DifficultyIds.Normal;

        public AnalyticsContext(string sessionId = null)
        {
            SessionId = string.IsNullOrWhiteSpace(sessionId)
                ? Guid.NewGuid().ToString("N")
                : sessionId;
            DifficultyId = DefaultDifficultyId;
        }

        public string SessionId { get; }
        public string RunId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public string DifficultyId { get; set; }
        public int StationIndex { get; set; }
        public int WaveIndex { get; set; }

        public void BindRun(string runId, string routeId, string difficultyId = null)
        {
            RunId = runId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(difficultyId))
            {
                DifficultyId = difficultyId;
            }

            StationIndex = 0;
            WaveIndex = 0;
        }

        public void ClearRun()
        {
            RunId = string.Empty;
            RouteId = string.Empty;
            StationIndex = 0;
            WaveIndex = 0;
        }

        public Dictionary<string, object> BuildBaseParameters()
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["session_id"] = SessionId ?? string.Empty,
                ["run_id"] = RunId ?? string.Empty,
                ["route_id"] = RouteId ?? string.Empty,
                ["difficulty_id"] = DifficultyId ?? DefaultDifficultyId,
                ["station_index"] = StationIndex,
                ["wave_index"] = WaveIndex,
            };
        }
    }
}
