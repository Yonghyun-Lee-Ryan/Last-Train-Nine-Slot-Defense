namespace LastTrain.Analytics
{
    /// <summary>분석 이벤트 이름 상수 (snake_case).</summary>
    public static class AnalyticsEventNames
    {
        public const string AppStarted = "app_started";
        public const string TutorialStarted = "tutorial_started";
        public const string TutorialStepCompleted = "tutorial_step_completed";
        public const string TutorialSkipped = "tutorial_skipped";
        public const string TutorialCompleted = "tutorial_completed";
        public const string TutorialPostSkipGuideShown = "tutorial_post_skip_guide_shown";
        public const string TutorialPostSkipGuideCompleted = "tutorial_post_skip_guide_completed";
        public const string RunStarted = "run_started";
        public const string StationStarted = "station_started";
        public const string StationCompleted = "station_completed";
        public const string WaveStarted = "wave_started";
        public const string WaveCompleted = "wave_completed";
        public const string PassengerOfferShown = "passenger_offer_shown";
        public const string PassengerSelected = "passenger_selected";
        public const string PassengerPlaced = "passenger_placed";
        public const string PassengerMoved = "passenger_moved";
        public const string PassengerMerged = "passenger_merged";
        public const string PassengerSold = "passenger_sold";
        public const string AbilityOfferShown = "ability_offer_shown";
        public const string AbilitySelected = "ability_selected";
        public const string SynergyActivated = "synergy_activated";
        public const string BossStarted = "boss_started";
        public const string BossPhaseChanged = "boss_phase_changed";
        public const string BossDefeated = "boss_defeated";
        public const string RunFailed = "run_failed";
        public const string RunCompleted = "run_completed";
        public const string RewardedAdOffered = "rewarded_ad_offered";
        public const string RewardedAdStarted = "rewarded_ad_started";
        public const string RewardedAdCompleted = "rewarded_ad_completed";
        public const string RewardedAdCancelled = "rewarded_ad_cancelled";
        public const string RewardedAdFailed = "rewarded_ad_failed";
        public const string MetaRewardReceived = "meta_reward_received";
        public const string PassengerUnlocked = "passenger_unlocked";
        public const string AchievementUnlocked = "achievement_unlocked";
        public const string DifficultyUnlocked = "difficulty_unlocked";
        public const string SaveRecovered = "save_recovered";
        public const string Error = "error";
    }
}
