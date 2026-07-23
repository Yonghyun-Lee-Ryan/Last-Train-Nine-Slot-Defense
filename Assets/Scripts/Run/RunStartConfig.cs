namespace LastTrain.Run
{
    /// <summary>새 회차 시작 시 적용할 초기값.</summary>
    public sealed class RunStartConfig
    {
        /// <summary>README MVP 객차 최대 내구도 기본값.</summary>
        public const int DefaultTrainMaxHp = 100;

        public int InitialTrainMaxHp { get; set; } = DefaultTrainMaxHp;
        public int InitialTrainCurrentHp { get; set; } = DefaultTrainMaxHp;
        public int InitialCoins { get; set; } = 50;
        public int InitialStationIndex { get; set; } = 1;
        public string LineId { get; set; } = "line1";
        public string DifficultyId { get; set; } = Difficulty.DifficultyIds.Normal;

        /// <summary>0이면 각 시스템이 자체 시드를 사용. 오늘의 막차는 고정 시드.</summary>
        public int RandomSeed { get; set; }

        /// <summary>오늘의 막차 모드. 이어하기·시드 변경 제한.</summary>
        public bool IsDailyRun { get; set; }

        /// <summary>무한 모드. 역이 끝나지 않으며 로컬 랭킹 대상.</summary>
        public bool IsEndlessRun { get; set; }

        /// <summary>라이브 이벤트 회차 ID. 비어 있으면 일반 회차.</summary>
        public string LiveEventId { get; set; } = string.Empty;

        /// <summary>이벤트 강화 승객 ID.</summary>
        public string[] LiveEventBoostedPassengerIds { get; set; }

        /// <summary>이벤트 제한 승객 ID(비어 있으면 제한 없음).</summary>
        public string[] LiveEventRestrictedPassengerIds { get; set; }

        /// <summary>강화 승객 공격력 배율.</summary>
        public float LiveEventBoostAttackMultiplier { get; set; } = 1f;

        /// <summary>무한 모드 깊이 Modifier 등 추가 규칙.</summary>
        public Difficulty.DifficultyModifierData[] ExtraDifficultyModifiers { get; set; }

        public static RunStartConfig CreateDefault()
        {
            return new RunStartConfig();
        }

        public static RunStartConfig CreateDailyRun(int seed)
        {
            return new RunStartConfig
            {
                IsDailyRun = true,
                RandomSeed = seed == 0 ? 1 : seed,
            };
        }

        public static RunStartConfig CreateEndlessRun(
            string difficultyId = null,
            Difficulty.DifficultyModifierData[] depthModifiers = null)
        {
            return new RunStartConfig
            {
                IsEndlessRun = true,
                LineId = Data.RouteIds.Endless,
                DifficultyId = string.IsNullOrWhiteSpace(difficultyId)
                    ? Difficulty.DifficultyIds.Normal
                    : difficultyId,
                ExtraDifficultyModifiers = depthModifiers,
            };
        }

        public static RunStartConfig CreateLiveEventRun(LiveOps.LiveEventData liveEvent)
        {
            var config = CreateDefault();
            if (liveEvent == null)
            {
                return config;
            }

            config.LiveEventId = liveEvent.Id ?? string.Empty;
            if (liveEvent.EventRoute != null && !string.IsNullOrWhiteSpace(liveEvent.EventRoute.Id))
            {
                config.LineId = liveEvent.EventRoute.Id;
            }

            if (liveEvent.EventDifficulty != null && !string.IsNullOrWhiteSpace(liveEvent.EventDifficulty.Id))
            {
                config.DifficultyId = liveEvent.EventDifficulty.Id;
            }

            Difficulty.DifficultyModifierData[] mods = liveEvent.EventModifiers;
            if (mods != null && mods.Length > 0)
            {
                config.ExtraDifficultyModifiers = mods;
            }

            config.LiveEventBoostedPassengerIds = liveEvent.BoostedPassengerIds;
            config.LiveEventRestrictedPassengerIds = liveEvent.RestrictedPassengerIds;
            config.LiveEventBoostAttackMultiplier = liveEvent.BoostedPassengerAttackMultiplier;
            return config;
        }
    }
}