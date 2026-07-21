using System;
using LastTrain.Run;

namespace LastTrain.Save
{
    [Serializable]
    public sealed class RunSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        // 저장 시점 검증용
        public int savedBattlePhase = (int)RunPhase.None;

        public int stationIndex = 1;
        public string stationId = string.Empty;
        public int currentWaveIndex = 0;
        public int completedStationCount = 0;

        public int trainHp = 100;
        public int trainMaxHp = 100;

        public int coinsCurrent = 0;
        public int coinsTotalEarned = 0;
        public int coinsTotalSpent = 0;

        public int enemiesKilled = 0;
        public int mergeCount = 0;
        public int passengersSummoned = 0;
        public int passengersSold = 0;
        public int highestPassengerStar = 1;
        public int abilityCardsSelected = 0;

        public SlotSave[] slots = new SlotSave[RunState.GridSlotCount];

        // 스택 수만큼 중복된 ability id를 포함한다.
        public string[] selectedAbilityIdsExpanded = Array.Empty<string>();

        public string lineId = string.Empty;

        [Serializable]
        public struct SlotSave
        {
            public string passengerId;
            public int starLevel;
        }
    }
}

