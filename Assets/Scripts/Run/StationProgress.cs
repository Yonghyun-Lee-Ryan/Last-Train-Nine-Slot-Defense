using System;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>현재 역·웨이브 진행 상태.</summary>
    public sealed class StationProgress
    {
        public event Action<int> StationIndexChanged;
        public event Action<int> WaveIndexChanged;

        public int CurrentStationIndex { get; private set; }
        public int CurrentWaveIndex { get; private set; }
        public string CurrentStationId { get; private set; } = string.Empty;
        public StationType CurrentStationType { get; private set; } = StationType.Normal;
        public int CompletedStationCount { get; private set; }

        public void Initialize(int startingStationIndex)
        {
            if (startingStationIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingStationIndex));
            }

            CurrentStationIndex = startingStationIndex;
            CurrentWaveIndex = 0;
            CurrentStationId = string.Empty;
            CurrentStationType = StationType.Normal;
            CompletedStationCount = 0;
            StationIndexChanged?.Invoke(CurrentStationIndex);
            WaveIndexChanged?.Invoke(CurrentWaveIndex);
        }

        public void SetCurrentStation(string stationId, int stationIndex, StationType stationType = StationType.Normal)
        {
            CurrentStationId = stationId ?? string.Empty;
            CurrentStationIndex = stationIndex;
            CurrentStationType = stationType;
            CurrentWaveIndex = 0;
            StationIndexChanged?.Invoke(CurrentStationIndex);
            WaveIndexChanged?.Invoke(CurrentWaveIndex);
        }

        public void SetWaveIndex(int waveIndex)
        {
            if (waveIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIndex));
            }

            CurrentWaveIndex = waveIndex;
            WaveIndexChanged?.Invoke(CurrentWaveIndex);
        }

        public void AdvanceToNextStation(int nextStationIndex, string nextStationId)
        {
            CompletedStationCount++;
            SetCurrentStation(nextStationId, nextStationIndex);
        }

        /// <summary>최종 역을 클리어했을 때 다음 역이 없을 때 호출한다.</summary>
        public void MarkCurrentStationCompleted()
        {
            CompletedStationCount++;
        }

        /// <summary>
        /// 저장된 회차 상태로 복원한다. (필드 누락/오류 방지를 위해 기본값을 보정한다.)
        /// </summary>
        public void RestoreFromSave(
            int currentStationIndex,
            string currentStationId,
            int currentWaveIndex,
            int completedStationCount)
        {
            if (currentStationIndex < 1)
            {
                currentStationIndex = 1;
            }

            CurrentStationIndex = currentStationIndex;
            CurrentStationId = currentStationId ?? string.Empty;
            CurrentStationType = InferStationType(CurrentStationId);

            if (currentWaveIndex < 0)
            {
                currentWaveIndex = 0;
            }

            CurrentWaveIndex = currentWaveIndex;

            if (completedStationCount < 0)
            {
                completedStationCount = 0;
            }

            CompletedStationCount = completedStationCount;

            StationIndexChanged?.Invoke(CurrentStationIndex);
            WaveIndexChanged?.Invoke(CurrentWaveIndex);
        }

        private static StationType InferStationType(string stationId)
        {
            if (string.IsNullOrWhiteSpace(stationId))
            {
                return StationType.Normal;
            }

            if (stationId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StationType.Boss;
            }

            if (stationId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StationType.Elite;
            }

            if (stationId.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StationType.Shop;
            }

            if (stationId.IndexOf("rest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StationType.Rest;
            }

            if (stationId.IndexOf("event", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StationType.Event;
            }

            return StationType.Normal;
        }
    }
}
