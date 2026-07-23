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
    }
}