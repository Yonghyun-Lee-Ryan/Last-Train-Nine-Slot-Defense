using System;

namespace LastTrain.Run
{
    /// <summary>회차 통계 기록. 결과 화면·분석 이벤트에 사용한다.</summary>
    public sealed class RunHistory
    {
        public int EnemiesKilled { get; private set; }
        public int MergeCount { get; private set; }
        public int PassengersSummoned { get; private set; }
        public int PassengersSold { get; private set; }
        public int HighestPassengerStar { get; private set; } = 1;
        public int AbilityCardsSelected { get; private set; }

        public void RecordEnemyKill()
        {
            EnemiesKilled++;
        }

        public void RecordMerge(int resultingStarLevel)
        {
            MergeCount++;
            UpdateHighestStar(resultingStarLevel);
        }

        public void RecordSummon(int starLevel = 1)
        {
            PassengersSummoned++;
            UpdateHighestStar(starLevel);
        }

        public void RecordSell()
        {
            PassengersSold++;
        }

        public void RecordAbilitySelected()
        {
            AbilityCardsSelected++;
        }

        public void UpdateHighestStar(int starLevel)
        {
            if (starLevel > HighestPassengerStar)
            {
                HighestPassengerStar = starLevel;
            }
        }

        /// <summary>저장 데이터로부터 상태를 복원한다.</summary>
        public void RestoreFromSave(
            int enemiesKilled,
            int mergeCount,
            int passengersSummoned,
            int passengersSold,
            int highestPassengerStar,
            int abilityCardsSelected)
        {
            EnemiesKilled = Math.Max(0, enemiesKilled);
            MergeCount = Math.Max(0, mergeCount);
            PassengersSummoned = Math.Max(0, passengersSummoned);
            PassengersSold = Math.Max(0, passengersSold);
            HighestPassengerStar = Math.Max(1, highestPassengerStar);
            AbilityCardsSelected = Math.Max(0, abilityCardsSelected);
        }
    }
}
