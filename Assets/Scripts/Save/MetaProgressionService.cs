using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Integrations;
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

            ApplyResultRewardMultiplier(breakdown, result);
            return breakdown;
        }

        private static void ApplyResultRewardMultiplier(MetaRewardBreakdown breakdown, RunResult result)
        {
            if (breakdown == null || result == null)
            {
                return;
            }

            float multiplier = result.DifficultyRewardMultiplier * RemoteConfigRuntime.Current.ResultRewardMultiplier;
            if (string.Equals(result.LineId, RouteIds.Quick, StringComparison.Ordinal))
            {
                float routeMul = 1f;
                GameDatabase database = GameDatabaseLocator.Load();
                if (database != null && database.TryGetRoute(RouteIds.Quick, out RouteData quick) && quick != null)
                {
                    routeMul = quick.RewardMultiplier;
                }

                multiplier *= routeMul * RemoteConfigRuntime.Current.QuickRunRewardMultiplier;
            }
            if (Math.Abs(multiplier - 1f) < 0.001f)
            {
                return;
            }

            breakdown.StationTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.StationTickets, multiplier);
            breakdown.KillTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.KillTickets, multiplier);
            breakdown.BossTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.BossTickets, multiplier);
            breakdown.RemainingHpTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.RemainingHpTickets, multiplier);
            breakdown.DiscoveryTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.DiscoveryTickets, multiplier);
            breakdown.AchievementTickets = Difficulty.DifficultyCalculator.ApplyMetaReward(breakdown.AchievementTickets, multiplier);
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

            int runScore = breakdown.TotalTickets;
            Difficulty.DifficultyProgressService.ApplyRunResult(
                meta,
                result,
                runScore,
                result.ElapsedSeconds,
                usedAds: false);

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

        public static bool IsPassengerDiscovered(MetaSaveData meta, string passengerId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.discoveredPassengerIds, passengerId);
        }

        public static bool IsEnemyDiscovered(MetaSaveData meta, string enemyId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(enemyId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.discoveredEnemyIds, enemyId);
        }

        public static bool IsBossDiscovered(MetaSaveData meta, string bossId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(bossId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.discoveredBossIds, bossId);
        }

        /// <summary>유물 도감 해금. RunResult 파이프라인 연동 전까지 unlockedRelicIds를 사용한다.</summary>
        public static bool IsRelicDiscovered(MetaSaveData meta, string relicId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(relicId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.unlockedRelicIds, relicId);
        }

        public static bool IsAchievementUnlocked(MetaSaveData meta, string achievementId)
        {
            if (meta == null || string.IsNullOrWhiteSpace(achievementId))
            {
                return false;
            }

            meta.EnsureDefaults();
            return ContainsId(meta.unlockedAchievementIds, achievementId);
        }

        public static bool TryGetPassengerMastery(
            MetaSaveData meta,
            string passengerId,
            out MetaPassengerMasteryEntry entry)
        {
            entry = null;
            if (meta == null || string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            meta.EnsureDefaults();
            if (meta.passengerMasteries == null)
            {
                return false;
            }

            for (int i = 0; i < meta.passengerMasteries.Length; i++)
            {
                MetaPassengerMasteryEntry candidate = meta.passengerMasteries[i];
                if (candidate != null && string.Equals(candidate.passengerId, passengerId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>라이브 이벤트 보상 트랙 지급. 티켓·XP·승객 해금.</summary>
        public static bool TryGrantLiveEventReward(
            MetaSaveData meta,
            int ticketFragments,
            int accountXp,
            string unlockPassengerId)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            int tickets = Math.Max(0, ticketFragments);
            int xp = Math.Max(0, accountXp);
            meta.ticketFragments = SaturatingAdd(meta.ticketFragments, tickets);
            if (xp > 0)
            {
                meta.accountXp = SaturatingAdd(meta.accountXp, xp);
            }
            else if (tickets > 0)
            {
                meta.accountXp = SaturatingAdd(
                    meta.accountXp,
                    tickets * MetaProgressionDefaults.AccountXpPerTicketFragment);
            }

            meta.accountLevel = CalculateAccountLevel(meta.accountXp);

            if (!string.IsNullOrWhiteSpace(unlockPassengerId))
            {
                TryUnlockPassenger(meta, unlockPassengerId);
            }

            return true;
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

            if (meta.accountLevel >= MetaProgressionDefaults.PoliceUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerPoliceId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerPoliceId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.CatUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerCatId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerCatId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.ConductorUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerConductorId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerConductorId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.BaristaUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerBaristaId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerBaristaId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.SecurityUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerSecurityId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerSecurityId);
                }
            }

            if (meta.accountLevel >= MetaProgressionDefaults.StudentUnlockAccountLevel)
            {
                if (TryUnlockPassenger(meta, MetaProgressionDefaults.PassengerStudentId))
                {
                    newlyUnlocked.Add(MetaProgressionDefaults.PassengerStudentId);
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
