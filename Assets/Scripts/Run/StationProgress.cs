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
    }
}
