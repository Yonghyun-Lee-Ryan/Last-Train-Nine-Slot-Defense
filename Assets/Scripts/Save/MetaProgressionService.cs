using System;
using System.Collections.Generic;
using LastTrain.Run;

namespace LastTrain.Save
{
    /// <summary>메타 보상 계산·적용. 중복 RunId 지급을 방지한다.</summary>
    public static class MetaProgressionService
    {
        public static bool HasRewardedRun(MetaSaveData meta, string runId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(runId) || meta.rewardedRunIds == null)
            {
                return false;
            }

            for (int i = 0; i < meta.rewardedRunIds.Length; i++)
            {
                if (string.Equals(meta.rewardedRunIds[i], runId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static MetaRewardBreakdown CalculateRewards(RunResult result, MetaSaveData metaBefore)
        {
            var breakdown = new MetaRewardBreakdown();
            if (result == null)
            {
                return breakdown;
            }

            MetaSaveData meta = metaBefore ?? new MetaSaveData();
            meta.EnsureDefaults();

            breakdown.StationTickets =
                Math.Max(0, result.CompletedStationCount) * MetaProgressionDefaults.TicketPerCompletedStation
                + Math.Max(0, result.ReachedStationIndex) * MetaProgressionDefaults.TicketPerReachedStationIndex;

            breakdown.KillTickets =
                Math.Max(0, result.EnemiesKilled) * MetaProgressionDefaults.TicketPerEnemyKill;

            breakdown.BossTickets =
                Math.Max(0, result.BossesKilled) * MetaProgressionDefaults.TicketPerBossKill;

            breakdown.RemainingHpTickets =
                Math.Max(0, result.RemainingTrainHp) * MetaProgressionDefaults.TicketPerRemainingHp;

            CollectNewDiscoveries(
                result.DiscoveredPassengerIds,
                meta.discoveredPassengerIds,
                breakdown.NewPassengerDiscoveries);
            CollectNewDiscoveries(
                result.DiscoveredEnemyIds,
                meta.discoveredEnemyIds,
                breakdown.NewEnemyDiscoveries);
            CollectNewDiscoveries(
                result.DiscoveredBossIds,
                meta.discoveredBossIds,
                breakdown.NewBossDiscoveries);

            int newDiscoveryCount =
                breakdown.NewPassengerDiscoveries.Count
                + breakdown.NewEnemyDiscoveries.Count
                + breakdown.NewBossDiscoveries.Count;
            breakdown.DiscoveryTickets =
                newDiscoveryCount * MetaProgressionDefaults.TicketPerNewDiscovery;

            CollectNewAchievements(result, meta, breakdown.NewlyUnlockedAchievements);
            breakdown.AchievementTickets =
                breakdown.NewlyUnlockedAchievements.Count * MetaProgressionDefaults.TicketPerAchievement;

            return breakdown;
        }

        public static MetaApplyResult TryApplyRunResult(MetaSaveData meta, RunResult result)
        {
            var applyResult = new MetaApplyResult
            {
                RunId = result?.RunId ?? string.Empty,
            };

            if (meta == null || result == null || string.IsNullOrWhiteSpace(result.RunId))
            {
                return applyResult;
            }

            meta.EnsureDefaults();

            if (HasRewardedRun(meta, result.RunId))
            {
                applyResult.WasDuplicate = true;
                applyResult.TicketFragmentsAfter = meta.ticketFragments;
                applyResult.AccountLevelAfter = meta.accountLevel;
                applyResult.AccountXpAfter = meta.accountXp;
                return applyResult;
            }

            MetaRewardBreakdown breakdown = CalculateRewards(result, meta);
            applyResult.Breakdown = breakdown;

            meta.ticketFragments = SaturatingAdd(meta.ticketFragments, breakdown.TotalTickets);
            meta.accountXp = SaturatingAdd(
                meta.accountXp,
                breakdown.TotalTickets * MetaProgressionDefaults.AccountXpPerTicketFragment);
            meta.accountLevel = CalculateAccountLevel(meta.accountXp);

            MergeIds(ref meta.discoveredPassengerIds, breakdown.NewPassengerDiscoveries);
            MergeIds(ref meta.discoveredEnemyIds, breakdown.NewEnemyDiscoveries);
            MergeIds(ref meta.discoveredBossIds, breakdown.NewBossDiscoveries);
            MergeIds(ref meta.unlockedAchievementIds, breakdown.NewlyUnlockedAchievements);

            var pending = new List<string>();
            pending.AddRange(breakdown.NewPassengerDiscoveries);
            pending.AddRange(breakdown.NewEnemyDiscoveries);
            pending.AddRange(breakdown.NewBossDiscoveries);
            pending.AddRange(breakdown.NewlyUnlockedAchievements);
            MergeIds(ref meta.pendingNewDiscoveryIds, pending);

            ApplyMastery(meta, result);
            ApplyLevelPassengerUnlocks(meta, breakdown.NewlyUnlockedPassengers);

            // 도감에는 이번 회차에서 본 모든 ID도 등록(이미 있던 것은 Merge에서 무시)
            MergeIds(ref meta.discoveredPassengerIds, result.DiscoveredPassengerIds);
            MergeIds(ref meta.discoveredEnemyIds, result.DiscoveredEnemyIds);
            MergeIds(ref meta.discoveredBossIds, result.DiscoveredBossIds);

            AppendId(ref meta.rewardedRunIds, result.RunId);

            applyResult.Applied = true;
            applyResult.TicketFragmentsAfter = meta.ticketFragments;
            applyResult.AccountLevelAfter = meta.accountLevel;
            applyResult.AccountXpAfter = meta.accountXp;
            return applyResult;
        }

        public static int CalculateAccountLevel(int accountXp)
        {
            int xp = Math.Max(0, accountXp);
            int perLevel = Math.Max(1, MetaProgressionDefaults.AccountXpPerLevel);
            return 1 + (xp / perLevel);
        }

        public static MetaProgressSnapshot CreateSnapshot(MetaSaveData meta)
        {
            meta ??= new MetaSaveData();
            meta.EnsureDefaults();

            return new MetaProgressSnapshot
            {
                TicketFragments = meta.ticketFragments,
                AccountLevel = meta.accountLevel,
                AccountXp = meta.accountXp,
                UnlockedPassengerCount = meta.unlockedPassengerIds?.Length ?? 0,
                DiscoveredPassengerCount = meta.discoveredPassengerIds?.Length ?? 0,
                DiscoveredEnemyCount = meta.discoveredEnemyIds?.Length ?? 0,
                DiscoveredBossCount = meta.discoveredBossIds?.Length ?? 0,
                PendingNewDiscoveryIds = meta.pendingNewDiscoveryIds ?? Array.Empty<string>(),
            };
        }

        public static bool IsPassengerUnlocked(MetaSaveData meta, string passengerId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.unlockedPassengerIds, passengerId);
        }

        private static void ApplyLevelPassengerUnlocks(MetaSaveData meta, List<string> newlyUnlocked)
        {
            if (meta.accountLevel >= MetaProgressionDefaults.DeveloperUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerDeveloperId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerDeveloperId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.GraduateUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerGraduateId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerGraduateId);
                }
            }
        }

        private static bool TryUnlockPassenger(MetaSaveData meta, string passengerId)
        {
            if (ContainsId(meta.unlockedPassengerIds, passengerId))
            {
                return false;
            }

            AppendId(ref meta.unlockedPassengerIds, passengerId);
            return true;
        }

        private static void ApplyMastery(MetaSaveData meta, RunResult result)
        {
            if (result.PassengerMasteries == null)
            {
                return;
            }

            var map = new Dictionary<string, MetaPassengerMasteryEntry>(StringComparer.Ordinal);
            if (meta.passengerMasteries != null)
            {
                for (int i = 0; i < meta.passengerMasteries.Length; i++)
                {
                    MetaPassengerMasteryEntry entry = meta.passengerMasteries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.passengerId))
                    {
                        continue;
                    }

                    map[entry.passengerId] = entry;
                }
            }

            for (int i = 0; i < result.PassengerMasteries.Length; i++)
            {
                RunPassengerMasterySnapshot snap = result.PassengerMasteries[i];
                if (snap == null || string.IsNullOrWhiteSpace(snap.PassengerId))
                {
                    continue;
                }

                if (!map.TryGetValue(snap.PassengerId, out MetaPassengerMasteryEntry entry))
                {
                    entry = new MetaPassengerMasteryEntry
                    {
                        passengerId = snap.PassengerId,
                        highestStar = 1,
                    };
                    map[snap.PassengerId] = entry;
                }

                entry.useCount = SaturatingAdd(entry.useCount, Math.Max(0, snap.UseCount));
                entry.highestStar = Math.Max(entry.highestStar, Math.Max(1, snap.HighestStar));
                entry.bossKillParticipations = SaturatingAdd(
                    entry.bossKillParticipations,
                    Math.Max(0, snap.BossKillParticipations));
            }

            var list = new List<MetaPassengerMasteryEntry>(map.Count);
            foreach (KeyValuePair<string, MetaPassengerMasteryEntry> pair in map)
            {
                list.Add(pair.Value);
            }

            meta.passengerMasteries = list.ToArray();
        }

        private static void CollectNewAchievements(
            RunResult result,
            MetaSaveData meta,
            List<string> output)
        {
            TryAddAchievement(meta, output, MetaProgressionDefaults.AchFirstVictory, result.IsVictory);
            TryAddAchievement(meta, output, MetaProgressionDefaults.AchFirstBossKill, result.BossesKilled > 0);
            TryAddAchievement(meta, output, MetaProgressionDefaults.AchKill10, result.EnemiesKilled >= 10);
            TryAddAchievement(meta, output, MetaProgressionDefaults.AchReachStation3, result.ReachedStationIndex >= 3);
            TryAddAchievement(
                meta,
                output,
                MetaProgressionDefaults.AchVictoryFullHp,
                result.IsVictory && result.TrainMaxHp > 0 && result.RemainingTrainHp >= result.TrainMaxHp);
        }

        private static void TryAddAchievement(
            MetaSaveData meta,
            List<string> output,
            string achievementId,
            bool condition)
        {
            if (!condition || ContainsId(meta.unlockedAchievementIds, achievementId))
            {
                return;
            }

            output.Add(achievementId);
        }

        private static void CollectNewDiscoveries(
            string[] runIds,
            string[] existingIds,
            List<string> output)
        {
            if (runIds == null)
            {
                return;
            }

            for (int i = 0; i < runIds.Length; i++)
            {
                string id = runIds[i];
                if (string.IsNullOrWhiteSpace(id) || ContainsId(existingIds, id) || output.Contains(id))
                {
                    continue;
                }

                output.Add(id);
            }
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

        private static void MergeIds(ref string[] target, IReadOnlyList<string> additions)
        {
            if (additions == null || additions.Count == 0)
            {
                target ??= Array.Empty<string>();
                return;
            }

            var list = new List<string>(target ?? Array.Empty<string>());
            for (int i = 0; i < additions.Count; i++)
            {
                string id = additions[i];
                if (string.IsNullOrWhiteSpace(id) || list.Contains(id))
                {
                    continue;
                }

                list.Add(id);
            }

            target = list.ToArray();
        }

        private static void MergeIds(ref string[] target, string[] additions)
        {
            if (additions == null || additions.Length == 0)
            {
                target ??= Array.Empty<string>();
                return;
            }

            MergeIds(ref target, (IReadOnlyList<string>)additions);
        }

        private static void AppendId(ref string[] target, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || ContainsId(target, id))
            {
                return;
            }

            int len = target?.Length ?? 0;
            var next = new string[len + 1];
            if (len > 0)
            {
                Array.Copy(target, next, len);
            }

            next[len] = id;
            target = next;
        }

        private static int SaturatingAdd(int a, int b)
        {
            long sum = (long)a + b;
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (sum < 0)
            {
                return 0;
            }

            return (int)sum;
        }
    }
}
