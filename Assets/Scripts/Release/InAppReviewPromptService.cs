using System;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Release
{
    /// <summary>
    /// 클리어 2회 이후 인앱 리뷰 요청. 재요청 쿨다운을 둔다.
    /// Google Play In-App Review SDK는 Soft Launch 시 LASTTRAIN_PLAY_REVIEW로 연결한다.
    /// 미연결 시에도 한도·쿨다운만 기록하고 게임 진행은 막지 않는다.
    /// </summary>
    public static class InAppReviewPromptService
    {
        public const int MinClearCount = 2;
        public static readonly TimeSpan Cooldown = TimeSpan.FromDays(14);

        private const string PrefLastPromptUtc = "lasttrain.review.last_prompt_utc";
        private const string PrefPromptCount = "lasttrain.review.prompt_count";

        /// <summary>테스트용 시각 주입. null이면 UtcNow.</summary>
        public static Func<DateTime> UtcNowProvider { get; set; }

        public static int GetTotalClearCount(MetaSaveData meta)
        {
            if (meta?.difficultyRecords == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < meta.difficultyRecords.Length; i++)
            {
                MetaDifficultyRecord record = meta.difficultyRecords[i];
                if (record != null)
                {
                    total += Math.Max(0, record.clearCount);
                }
            }

            return total;
        }

        public static bool CanPrompt(MetaSaveData meta)
        {
            if (GetTotalClearCount(meta) < MinClearCount)
            {
                return false;
            }

            string raw = PlayerPrefs.GetString(PrefLastPromptUtc, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime last))
            {
                return true;
            }

            return UtcNow() - last >= Cooldown;
        }

        public static bool TryPrompt(MetaSaveData meta)
        {
            if (!CanPrompt(meta))
            {
                return false;
            }

            MarkPrompted();
            BeginNativeReview();
            return true;
        }

        public static void ResetForTests()
        {
            PlayerPrefs.DeleteKey(PrefLastPromptUtc);
            PlayerPrefs.DeleteKey(PrefPromptCount);
            PlayerPrefs.Save();
        }

        private static void MarkPrompted()
        {
            PlayerPrefs.SetString(PrefLastPromptUtc, UtcNow().ToString("o"));
            PlayerPrefs.SetInt(PrefPromptCount, PlayerPrefs.GetInt(PrefPromptCount, 0) + 1);
            PlayerPrefs.Save();
        }

        private static DateTime UtcNow()
        {
            return UtcNowProvider?.Invoke() ?? DateTime.UtcNow;
        }

        private static void BeginNativeReview()
        {
#if LASTTRAIN_PLAY_REVIEW
            // Soft Launch: Google Play In-App Review (com.google.play.review) 연결 지점.
            // 패키지·define 준비 전 컴파일을 보호하기 위해 실제 API 호출은 SDK Import 후 채운다.
            Debug.Log("[InAppReview] LASTTRAIN_PLAY_REVIEW defined — wire Play Review SDK here.");
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[InAppReview] Prompt eligible — native review SDK not active (Editor/Dev NoOp).");
#endif
        }
    }
}
