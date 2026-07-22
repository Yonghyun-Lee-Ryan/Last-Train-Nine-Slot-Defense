namespace LastTrain.Save
{
    /// <summary>메타 성장 기본값·보상 계수. 전투 능력치 버프는 넣지 않는다.</summary>
    public static class MetaProgressionDefaults
    {
        public static readonly string[] DefaultUnlockedPassengerIds =
        {
            "passenger_office_worker",
            "passenger_delivery",
            "passenger_trainer",
            "passenger_nurse",
        };

        public const string PassengerDeveloperId = "passenger_developer";
        public const string PassengerGraduateId = "passenger_graduate";
        public const string PassengerPoliceId = "passenger_police";
        public const string PassengerCatId = "passenger_cat";

        public const int TicketPerCompletedStation = 8;
        public const int TicketPerReachedStationIndex = 2;
        public const int TicketPerEnemyKill = 1;
        public const int TicketPerBossKill = 25;
        public const int TicketPerRemainingHp = 1;
        public const int TicketPerNewDiscovery = 5;
        public const int TicketPerAchievement = 15;

        public const int AccountXpPerTicketFragment = 1;
        public const int AccountXpPerLevel = 100;

        public const int DeveloperUnlockAccountLevel = 2;
        public const int GraduateUnlockAccountLevel = 3;
        public const int PoliceUnlockAccountLevel = 4;
        public const int CatUnlockAccountLevel = 5;

        public const string AchFirstVictory = "ach_first_victory";
        public const string AchFirstBossKill = "ach_first_boss_kill";
        public const string AchKill10 = "ach_kill_10";
        public const string AchReachStation3 = "ach_reach_station_3";
        public const string AchVictoryFullHp = "ach_victory_full_hp";
    }
}
