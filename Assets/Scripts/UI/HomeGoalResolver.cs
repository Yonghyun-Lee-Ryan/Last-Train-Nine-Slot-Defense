using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Mission;
using LastTrain.Save;

namespace LastTrain.UI
{
    public enum HomeGoalKind
    {
        MissionClaim = 0,
        MissionProgress = 1,
        DifficultyUnlock = 2,
        SeasonEvent = 3,
        ContinueRun = 4,
        StartRun = 5,
    }

    /// <summary>메인 홈 「오늘의 목표」 카드 스냅샷.</summary>
    public sealed class HomeGoalSnapshot
    {
        public HomeGoalKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public string CtaLabel { get; }

        public HomeGoalSnapshot(HomeGoalKind kind, string title, string body, string ctaLabel)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            CtaLabel = ctaLabel ?? string.Empty;
        }
    }

    /// <summary>
    /// 오늘의 목표 우선순위: 미션(수령/진행) → 난이도 해금 → 시즌 → 이어하기/시작.
    /// UI와 분리된 순수 로직(EditMode 테스트 가능).
    /// </summary>
    public static class HomeGoalResolver
    {
        public static HomeGoalSnapshot Resolve(
            IReadOnlyList<MissionProgressView> missions,
            IReadOnlyList<DifficultyData> difficulties,
            MetaSaveData meta,
            string activeSeasonDisplayName,
            bool hasContinueSave)
        {
            HomeGoalSnapshot mission = TryMissionGoal(missions);
            if (mission != null)
            {
                return mission;
            }

            HomeGoalSnapshot difficulty = TryDifficultyGoal(difficulties, meta);
            if (difficulty != null)
            {
                return difficulty;
            }

            if (!string.IsNullOrWhiteSpace(activeSeasonDisplayName))
            {
                return new HomeGoalSnapshot(
                    HomeGoalKind.SeasonEvent,
                    "시즌 목표",
                    activeSeasonDisplayName.Trim() + " 진행 중",
                    "시즌 열기");
            }

            if (hasContinueSave)
            {
                return new HomeGoalSnapshot(
                    HomeGoalKind.ContinueRun,
                    "오늘의 목표",
                    "저장된 회차가 있습니다. 이어서 탑승하세요.",
                    "이어하기");
            }

            return new HomeGoalSnapshot(
                HomeGoalKind.StartRun,
                "오늘의 목표",
                "난이도를 고르고 막차에 탑승하세요.",
                "게임 시작");
        }

        private static HomeGoalSnapshot TryMissionGoal(IReadOnlyList<MissionProgressView> missions)
        {
            if (missions == null || missions.Count == 0)
            {
                return null;
            }

            MissionProgressView claimable = null;
            MissionProgressView inProgress = null;
            for (int i = 0; i < missions.Count; i++)
            {
                MissionProgressView view = missions[i];
                if (view == null)
                {
                    continue;
                }

                if (view.CanClaim)
                {
                    claimable = view;
                    break;
                }

                if (!view.Completed && inProgress == null)
                {
                    inProgress = view;
                }
            }

            if (claimable != null)
            {
                string name = claimable.Data != null ? claimable.Data.DisplayName : "미션";
                return new HomeGoalSnapshot(
                    HomeGoalKind.MissionClaim,
                    "미션 보상",
                    name + " 보상을 받을 수 있습니다.",
                    "미션 열기");
            }

            if (inProgress != null)
            {
                string name = inProgress.Data != null ? inProgress.Data.DisplayName : "미션";
                return new HomeGoalSnapshot(
                    HomeGoalKind.MissionProgress,
                    "오늘의 미션",
                    $"{name}  {inProgress.Progress}/{inProgress.Target}",
                    "미션 열기");
            }

            return null;
        }

        private static HomeGoalSnapshot TryDifficultyGoal(
            IReadOnlyList<DifficultyData> difficulties,
            MetaSaveData meta)
        {
            if (difficulties == null || difficulties.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < difficulties.Count; i++)
            {
                DifficultyData data = difficulties[i];
                if (data == null || DifficultyProgressService.IsUnlocked(data, meta))
                {
                    continue;
                }

                DifficultyUnlockProgress progress = DifficultyProgressService.GetUnlockProgress(data, meta);
                string body = string.IsNullOrWhiteSpace(progress.ProgressText)
                    ? data.DisplayName + " 해금 조건 진행 중"
                    : data.DisplayName + " — " + progress.ProgressText;
                return new HomeGoalSnapshot(
                    HomeGoalKind.DifficultyUnlock,
                    "난이도 해금",
                    body,
                    "플레이로 이동");
            }

            return null;
        }
    }

    public enum MainMenuHomeSection
    {
        Play = 0,
        Growth = 1,
        Season = 2,
    }

    /// <summary>메인 홈 탭 선택 상태(레이아웃이 읽는다).</summary>
    public static class MainMenuHomeTabs
    {
        public static MainMenuHomeSection Active { get; set; } = MainMenuHomeSection.Play;

        /// <summary>이어하기 버튼이 저장본 때문에 보여야 하는지.</summary>
        public static bool ContinueAvailable { get; set; }

        /// <summary>활성 시즌 이벤트가 있는지(시즌 탭에서만 노출).</summary>
        public static bool LiveEventAvailable { get; set; }
    }
}
