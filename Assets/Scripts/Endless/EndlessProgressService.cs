using System;
using System.Collections.Generic;
using System.Text;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Leaderboard;
using LastTrain.Release;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.Score;
using UnityEngine;

namespace LastTrain.Endless
{
    /// <summary>무한 모드 로컬 최고 기록·랭킹 제출·해금 판정.</summary>
    public static class EndlessProgressService
    {
        public static bool IsUnlocked(MetaSaveData meta)
        {
            if (meta?.difficultyRecords == null)
            {
                return false;
            }

            for (int i = 0; i < meta.difficultyRecords.Length; i++)
            {
                MetaDifficultyRecord record = meta.difficultyRecords[i];
                if (record != null && record.clearCount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetOrCreateAnonymousUserId(MetaSaveData meta)
        {
            if (meta == null)
            {
                return Guid.NewGuid().ToString("N");
            }

            meta.EnsureDefaults();
            if (string.IsNullOrWhiteSpace(meta.anonymousUserId))
            {
                meta.anonymousUserId = Guid.NewGuid().ToString("N");
            }

            return meta.anonymousUserId;
        }

        public static bool HasSubmittedRun(MetaSaveData meta, string runId)
        {
            if (meta?.endlessSubmittedRunIds == null || string.IsNullOrWhiteSpace(runId))
            {
                return false;
            }

            for (int i = 0; i < meta.endlessSubmittedRunIds.Length; i++)
            {
                if (string.Equals(meta.endlessSubmittedRunIds[i], runId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static int ComputeScore(RunResult result)
        {
            return ScoreCalculator.Calculate(ScoreInput.FromRunResult(result));
        }

        public static LeaderboardRunRecord BuildRecord(
            MetaSaveData meta,
            RunResult result,
            RunState runState,
            int score)
        {
            AppReleaseConfig release = AppReleaseConfigLocator.Load();
            string version = release != null ? release.VersionName : Application.version;

            var passengerIds = new List<string>();
            if (runState != null)
            {
                for (int i = 0; i < RunState.GridSlotCount; i++)
                {
                    PassengerRuntime p = runState.GetPassengerAtSlot(i);
                    if (p?.Data != null && !string.IsNullOrWhiteSpace(p.Data.Id))
                    {
                        passengerIds.Add(p.Data.Id);
                    }
                }
            }

            return new LeaderboardRunRecord
            {
                anonymousUserId = GetOrCreateAnonymousUserId(meta),
                gameVersion = version ?? string.Empty,
                runId = result?.RunId ?? string.Empty,
                score = score,
                reachedStation = result?.ReachedStationIndex ?? 0,
                difficultyId = result?.DifficultyId ?? DifficultyIds.Normal,
                playTimeSeconds = result?.ElapsedSeconds ?? 0f,
                finalPassengerIds = passengerIds.ToArray(),
                randomSeed = runState?.RandomSeed ?? 0,
                runSummary = BuildRunSummary(result, score),
                lineId = result?.LineId ?? RouteIds.Endless,
                submittedUtc = DateTime.UtcNow.ToString("o"),
            };
        }

        /// <summary>
        /// 로컬 최고 갱신 후 랭킹 서비스에 제출. 동일 Run ID는 한 번만 등록된다.
        /// </summary>
        public static LeaderboardSubmitResult TrySubmitRun(
            MetaSaveData meta,
            RunResult result,
            RunState runState,
            ILeaderboardService leaderboard,
            out int score,
            out bool localBestUpdated)
        {
            score = 0;
            localBestUpdated = false;
            if (meta == null || result == null || !result.IsEndlessRun)
            {
                return LeaderboardSubmitResult.InvalidRecord;
            }

            meta.EnsureDefaults();
            if (HasSubmittedRun(meta, result.RunId))
            {
                return LeaderboardSubmitResult.DuplicateRunId;
            }

            score = ComputeScore(result);
            if (score > meta.endlessBestScore
                || (score == meta.endlessBestScore
                    && result.ReachedStationIndex > meta.endlessBestStationReached))
            {
                meta.endlessBestScore = score;
                meta.endlessBestStationReached = result.ReachedStationIndex;
                meta.endlessBestRunId = result.RunId;
                localBestUpdated = true;
            }
            else if (result.ReachedStationIndex > meta.endlessBestStationReached)
            {
                meta.endlessBestStationReached = result.ReachedStationIndex;
                localBestUpdated = true;
            }

            LeaderboardRunRecord record = BuildRecord(meta, result, runState, score);
            ILeaderboardService service = leaderboard ?? new LocalLeaderboardService();
            LeaderboardSubmitResult submit = service.Submit(record);
            if (submit == LeaderboardSubmitResult.Success)
            {
                AppendSubmittedRunId(meta, result.RunId);
            }

            return submit;
        }

        private static void AppendSubmittedRunId(MetaSaveData meta, string runId)
        {
            var list = new List<string>(meta.endlessSubmittedRunIds ?? Array.Empty<string>());
            if (!HasSubmittedRun(meta, runId))
            {
                list.Add(runId);
            }

            meta.endlessSubmittedRunIds = list.ToArray();
        }

        private static string BuildRunSummary(RunResult result, int score)
        {
            if (result == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(128);
            sb.Append("score=").Append(score);
            sb.Append(";station=").Append(result.ReachedStationIndex);
            sb.Append(";kills=").Append(result.EnemiesKilled);
            sb.Append(";bosses=").Append(result.BossesKilled);
            sb.Append(";hp=").Append(result.RemainingTrainHp);
            sb.Append(';').Append(result.DifficultyId);
            sb.Append(';').Append(result.EndReason);
            return sb.ToString();
        }

        public static bool HasClaimedMilestone(MetaSaveData meta, string milestoneId)
        {
            if (meta?.endlessClaimedMilestoneIds == null || string.IsNullOrWhiteSpace(milestoneId))
            {
                return false;
            }

            for (int i = 0; i < meta.endlessClaimedMilestoneIds.Length; i++)
            {
                if (string.Equals(meta.endlessClaimedMilestoneIds[i], milestoneId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsMilestoneReached(MetaSaveData meta, EndlessMilestoneStep step)
        {
            if (meta == null || step == null)
            {
                return false;
            }

            if (step.requiredStation > 0 && meta.endlessBestStationReached >= step.requiredStation)
            {
                return true;
            }

            return step.requiredScore > 0 && meta.endlessBestScore >= step.requiredScore;
        }

        public static bool TryClaimMilestone(MetaSaveData meta, EndlessMilestoneStep step)
        {
            if (meta == null || step == null || string.IsNullOrWhiteSpace(step.id))
            {
                return false;
            }

            meta.EnsureDefaults();
            if (!IsMilestoneReached(meta, step) || HasClaimedMilestone(meta, step.id))
            {
                return false;
            }

            if (!MetaProgressionService.TryGrantLiveEventReward(
                    meta,
                    step.ticketFragments,
                    step.accountXp,
                    string.Empty))
            {
                return false;
            }

            var list = new List<string>(meta.endlessClaimedMilestoneIds ?? Array.Empty<string>());
            list.Add(step.id);
            meta.endlessClaimedMilestoneIds = list.ToArray();
            return true;
        }
    }
}
