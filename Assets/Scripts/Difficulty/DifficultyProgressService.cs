using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;

namespace LastTrain.Difficulty
{
    /// <summary>난이도 해금·기록·진행도 표시.</summary>
    public static class DifficultyProgressService
    {
        public static bool IsUnlocked(DifficultyData data, MetaSaveData meta)
        {
            if (data == null)
            {
                return false;
            }

            meta ??= new MetaSaveData();
            meta.EnsureDefaults();

            if (string.Equals(data.Id, DifficultyIds.Normal, StringComparison.Ordinal))
            {
                return true;
            }

            if (ContainsId(meta.unlockedDifficultyIds, data.Id))
            {
                return true;
            }

            DifficultyUnlockRequirement[] requirements = data.UnlockCondition?.Requirements
                ?? Array.Empty<DifficultyUnlockRequirement>();
            if (requirements.Length == 0 || data.UnlockCondition.IsAlwaysUnlocked())
            {
                return true;
            }

            for (int i = 0; i < requirements.Length; i++)
            {
                if (!EvaluateRequirement(requirements[i], meta))
                {
                    return false;
                }
            }

            return true;
        }

        public static DifficultyUnlockProgress GetUnlockProgress(DifficultyData data, MetaSaveData meta)
        {
            var progress = new DifficultyUnlockProgress();
            if (data == null)
            {
                progress.ProgressText = string.Empty;
                return progress;
            }

            meta ??= new MetaSaveData();
            meta.EnsureDefaults();

            progress.IsUnlocked = IsUnlocked(data, meta);
            if (progress.IsUnlocked)
            {
                progress.ProgressText = "해금됨";
                return progress;
            }

            DifficultyUnlockRequirement[] requirements = data.UnlockCondition?.Requirements
                ?? Array.Empty<DifficultyUnlockRequirement>();
            if (requirements.Length == 0)
            {
                progress.ProgressText = string.Empty;
                return progress;
            }

            var lines = new List<string>(requirements.Length);
            for (int i = 0; i < requirements.Length; i++)
            {
                lines.Add(DescribeRequirement(requirements[i], meta));
            }

            progress.ProgressText = string.Join("\n", lines);
            return progress;
        }

        public static MetaDifficultyRecord GetOrCreateRecord(MetaSaveData meta, string difficultyId)
        {
            meta.EnsureDefaults();
            string id = DifficultyService.ResolveSavedDifficultyId(difficultyId);

            for (int i = 0; i < meta.difficultyRecords.Length; i++)
            {
                MetaDifficultyRecord record = meta.difficultyRecords[i];
                if (record != null && string.Equals(record.difficultyId, id, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            var created = new MetaDifficultyRecord { difficultyId = id };
            AppendRecord(meta, created);
            return created;
        }

        public static void ApplyRunResult(MetaSaveData meta, RunResult result, int runScore, float elapsedSeconds, bool usedAds)
        {
            if (meta == null || result == null)
            {
                return;
            }

            meta.EnsureDefaults();
            string difficultyId = DifficultyService.ResolveSavedDifficultyId(result.DifficultyId);
            MetaDifficultyRecord record = GetOrCreateRecord(meta, difficultyId);

            record.highestStationReached = Math.Max(record.highestStationReached, result.ReachedStationIndex);

            if (result.IsVictory)
            {
                record.clearCount = Math.Max(0, record.clearCount) + 1;
                if (string.IsNullOrWhiteSpace(record.firstClearUtc))
                {
                    record.firstClearUtc = DateTime.UtcNow.ToString("o");
                }

                if (!usedAds)
                {
                    record.clearedWithoutAds = true;
                }
            }

            record.bestScore = Math.Max(record.bestScore, Math.Max(0, runScore));
            record.bestRemainingHp = Math.Max(record.bestRemainingHp, Math.Max(0, result.RemainingTrainHp));

            if (result.IsVictory && elapsedSeconds > 0f)
            {
                if (record.fastestClearSeconds <= 0f || elapsedSeconds < record.fastestClearSeconds)
                {
                    record.fastestClearSeconds = elapsedSeconds;
                }
            }

            RefreshUnlocks(meta, result);
        }

        public static string[] ConsumePendingUnlocks(MetaSaveData meta)
        {
            meta.EnsureDefaults();
            string[] pending = meta.pendingUnlockedDifficultyIds ?? Array.Empty<string>();
            meta.pendingUnlockedDifficultyIds = Array.Empty<string>();
            return pending;
        }

        public static bool HasFinalBossClear(MetaSaveData meta, string difficultyId)
        {
            MetaDifficultyRecord record = FindRecord(meta, difficultyId);
            return record != null && record.clearCount > 0;
        }

        private static void RefreshUnlocks(MetaSaveData meta, RunResult result)
        {
            if (!result.IsVictory || result.BossesKilled <= 0)
            {
                return;
            }

            GameDatabase database = GameDatabaseLocator.Load();
            if (database?.Difficulties == null)
            {
                return;
            }

            var newlyUnlocked = new List<string>();
            for (int i = 0; i < database.Difficulties.Count; i++)
            {
                DifficultyData data = database.Difficulties[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                if (ContainsId(meta.unlockedDifficultyIds, data.Id))
                {
                    continue;
                }

                if (!IsUnlocked(data, meta))
                {
                    continue;
                }

                AppendId(ref meta.unlockedDifficultyIds, data.Id);
                newlyUnlocked.Add(data.Id);
            }

            if (newlyUnlocked.Count > 0)
            {
                MergeIds(ref meta.pendingUnlockedDifficultyIds, newlyUnlocked);
            }
        }

        private static bool EvaluateRequirement(DifficultyUnlockRequirement requirement, MetaSaveData meta)
        {
            if (requirement == null)
            {
                return true;
            }

            return requirement.unlockType switch
            {
                DifficultyUnlockType.AlwaysUnlocked => true,
                DifficultyUnlockType.DefeatFinalBossOnDifficulty =>
                    HasFinalBossClear(meta, requirement.requiredDifficultyId),
                DifficultyUnlockType.AccountLevel =>
                    meta.accountLevel >= Math.Max(1, requirement.requiredAccountLevel),
                DifficultyUnlockType.UnlockedPassengerCount =>
                    (meta.unlockedPassengerIds?.Length ?? 0) >= Math.Max(0, requirement.requiredUnlockedPassengerCount),
                _ => true,
            };
        }

        private static string DescribeRequirement(DifficultyUnlockRequirement requirement, MetaSaveData meta)
        {
            if (requirement == null)
            {
                return string.Empty;
            }

            switch (requirement.unlockType)
            {
                case DifficultyUnlockType.DefeatFinalBossOnDifficulty:
                {
                    string requiredName = requirement.requiredDifficultyId;
                    bool done = HasFinalBossClear(meta, requirement.requiredDifficultyId);
                    return done
                        ? $"✓ {requiredName} 최종 보스 처치"
                        : $"○ {requiredName} 최종 보스 처치";
                }
                case DifficultyUnlockType.AccountLevel:
                {
                    int required = Math.Max(1, requirement.requiredAccountLevel);
                    return meta.accountLevel >= required
                        ? $"✓ 계정 레벨 {required} 이상 ({meta.accountLevel})"
                        : $"○ 계정 레벨 {required} 이상 ({meta.accountLevel})";
                }
                case DifficultyUnlockType.UnlockedPassengerCount:
                {
                    int required = Math.Max(0, requirement.requiredUnlockedPassengerCount);
                    int current = meta.unlockedPassengerIds?.Length ?? 0;
                    return current >= required
                        ? $"✓ 승객 {required}종 해금 ({current})"
                        : $"○ 승객 {required}종 해금 ({current})";
                }
                default:
                    return string.Empty;
            }
        }

        private static MetaDifficultyRecord FindRecord(MetaSaveData meta, string difficultyId)
        {
            if (meta?.difficultyRecords == null)
            {
                return null;
            }

            string id = DifficultyService.ResolveSavedDifficultyId(difficultyId);
            for (int i = 0; i < meta.difficultyRecords.Length; i++)
            {
                MetaDifficultyRecord record = meta.difficultyRecords[i];
                if (record != null && string.Equals(record.difficultyId, id, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        private static void AppendRecord(MetaSaveData meta, MetaDifficultyRecord record)
        {
            var list = new List<MetaDifficultyRecord>(meta.difficultyRecords ?? Array.Empty<MetaDifficultyRecord>())
            {
                record,
            };
            meta.difficultyRecords = list.ToArray();
        }

        private static bool ContainsId(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendId(ref string[] ids, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || ContainsId(ids, id))
            {
                return;
            }

            var list = new List<string>(ids ?? Array.Empty<string>()) { id };
            ids = list.ToArray();
        }

        private static void MergeIds(ref string[] target, IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                AppendId(ref target, source[i]);
            }
        }
    }
}
