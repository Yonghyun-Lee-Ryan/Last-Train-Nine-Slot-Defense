using System;

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
            CompletedStationCount = 0;
            StationIndexChanged?.Invoke(CurrentStationIndex);
            WaveIndexChanged?.Invoke(CurrentWaveIndex);
        }

        public void SetCurrentStation(string stationId, int stationIndex)
        {
            CurrentStationId = stationId ?? string.Empty;
            CurrentStationIndex = stationIndex;
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
    }
}
