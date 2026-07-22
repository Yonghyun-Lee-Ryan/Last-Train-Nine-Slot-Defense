using System;
using System.Collections.Generic;
using System.Text;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Analytics
{
    /// <summary>공통 Context를 병합해 IAnalyticsService로 전달하는 진입점.</summary>
    public sealed class AnalyticsCoordinator
    {
        private readonly IAnalyticsService _service;

        public AnalyticsCoordinator(IAnalyticsService service, AnalyticsContext context = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Context = context ?? new AnalyticsContext();
        }

        public AnalyticsContext Context { get; }

        public IAnalyticsService Service => _service;

        public void BindRun(RunState runState, string difficultyId = null)
        {
            if (runState == null)
            {
                Context.ClearRun();
                return;
            }

            Context.BindRun(runState.RunId, runState.LineId, difficultyId);
            Context.StationIndex = runState.Station?.CurrentStationIndex ?? 0;
            Context.WaveIndex = runState.Station?.CurrentWaveIndex ?? 0;
        }

        public void ClearRun()
        {
            Context.ClearRun();
        }

        public void Track(string eventName, IDictionary<string, object> extra = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            try
            {
                Dictionary<string, object> merged = Context.BuildBaseParameters();
                if (extra != null)
                {
                    foreach (KeyValuePair<string, object> pair in extra)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Key))
                        {
                            continue;
                        }

                        merged[pair.Key] = pair.Value;
                    }
                }

                _service.Track(eventName, merged);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Track failed: {e.Message}");
            }
        }

        public void TrackError(string message, string source = null)
        {
            var extra = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["message"] = message ?? string.Empty,
            };
            if (!string.IsNullOrWhiteSpace(source))
            {
                extra["source"] = source;
            }

            Track(AnalyticsEventNames.Error, extra);
        }

        public void TrackRunEnded(RunResult result, RunState snapshotState = null)
        {
            if (result == null)
            {
                return;
            }

            Context.RunId = result.RunId ?? Context.RunId;
            Context.RouteId = result.LineId ?? Context.RouteId;
            Context.StationIndex = result.ReachedStationIndex;

            var extra = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["end_reason"] = result.EndReason.ToString(),
                ["is_victory"] = result.IsVictory,
                ["reached_station_index"] = result.ReachedStationIndex,
                ["completed_station_count"] = result.CompletedStationCount,
                ["train_hp"] = result.RemainingTrainHp,
                ["train_max_hp"] = result.TrainMaxHp,
                ["enemies_killed"] = result.EnemiesKilled,
                ["bosses_killed"] = result.BossesKilled,
                ["merge_count"] = result.MergeCount,
                ["final_coins"] = result.FinalCoins,
            };

            if (!result.IsVictory)
            {
                if (snapshotState != null)
                {
                    extra["passenger_composition"] = BuildPassengerComposition(snapshotState);
                    extra["active_synergies"] = BuildActiveSynergies(snapshotState);
                }

                Track(AnalyticsEventNames.RunFailed, extra);
            }
            else
            {
                Track(AnalyticsEventNames.RunCompleted, extra);
            }
        }

        private static string BuildPassengerComposition(RunState runState)
        {
            if (runState?.AllPassengers == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < runState.AllPassengers.Count; i++)
            {
                PassengerRuntime p = runState.AllPassengers[i];
                if (p?.Data == null)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(',');
                }

                sb.Append(p.Data.Id).Append(':').Append(p.StarLevel);
            }

            return sb.ToString();
        }

        private static string BuildActiveSynergies(RunState runState)
        {
            if (runState?.Synergies?.Active == null || runState.Synergies.Active.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < runState.Synergies.Active.Count; i++)
            {
                SynergyData data = runState.Synergies.Active[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(',');
                }

                sb.Append(data.Id);
            }

            return sb.ToString();
        }
    }
}
