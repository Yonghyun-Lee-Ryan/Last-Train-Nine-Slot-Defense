using System;
using LastTrain.Difficulty;
using LastTrain.Run;

namespace LastTrain.Save
{
    [Serializable]
    public sealed class RunSaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

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

        public string[] selectedAbilityIdsExpanded = Array.Empty<string>();

        public string lineId = string.Empty;
        public string difficultyId = DifficultyIds.Normal;
        public bool isDailyRun;
        public bool isEndlessRun;
        public int randomSeed;

        public bool shopActive;
        public bool shopResolved;
        public string shopStationId = string.Empty;
        public int shopStationIndex;
        public ShopOfferSave[] shopOffers = Array.Empty<ShopOfferSave>();

        public bool eventActive;
        public bool eventResolved;
        public string eventStationId = string.Empty;
        public string eventId = string.Empty;
        public int eventChoiceIndex = -1;

        public string[] relicIds = Array.Empty<string>();
        public bool emergencyAutoHealUsed;
        public int freeSummonCharges;
        public int summonCostReductionStacks;
        public float nextEnemyHealthMultiplier = 1f;
        public float nextRewardCoinMultiplier = 1f;

        /// <summary>슬롯이 가득 차 대기 중인 지급 승객.</summary>
        public SlotSave[] pendingPassengers = Array.Empty<SlotSave>();

        [Serializable]
        public struct SlotSave
        {
            public string passengerId;
            public int starLevel;
        }
    }
}
