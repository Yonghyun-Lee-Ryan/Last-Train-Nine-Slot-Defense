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
    }
}
