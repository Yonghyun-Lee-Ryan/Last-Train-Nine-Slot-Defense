using System;
using LastTrain.Difficulty;
using LastTrain.Run;

namespace LastTrain.Save
{
    [Serializable]
    public sealed class RunSaveData
    {
        public const int CurrentVersion = 3;

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

        /// <summary>회차 시작 시 고정된 라이브 이벤트. 카탈로그/시계가 지나도 이어하기 배율을 유지한다.</summary>
        public string liveEventId = string.Empty;
        public string[] liveEventBoostedPassengerIds = Array.Empty<string>();
        public string[] liveEventRestrictedPassengerIds = Array.Empty<string>();
        public float liveEventBoostAttackMultiplier = 1f;

        [Serializable]
        public struct SlotSave
        {
            public string passengerId;
            public int starLevel;
        }
    }
}
