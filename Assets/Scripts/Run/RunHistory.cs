using System;
using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>회차 중 승객 숙련도 입력 스냅샷.</summary>
    public sealed class RunPassengerMasterySnapshot
    {
        public RunPassengerMasterySnapshot(
            string passengerId,
            int useCount,
            int highestStar,
            int bossKillParticipations)
        {
            PassengerId = passengerId ?? string.Empty;
            UseCount = Math.Max(0, useCount);
            HighestStar = Math.Max(1, highestStar);
            BossKillParticipations = Math.Max(0, bossKillParticipations);
        }

        public string PassengerId { get; }
        public int UseCount { get; }
        public int HighestStar { get; }
        public int BossKillParticipations { get; }
    }

    /// <summary>회차 통계 기록. 결과 화면·메타 보상·분석 이벤트에 사용한다.</summary>
    public sealed class RunHistory
    {
        private readonly HashSet<string> _discoveredPassengerIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _discoveredEnemyIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _discoveredBossIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MasteryAccumulator> _mastery = new(StringComparer.Ordinal);

        public int EnemiesKilled { get; private set; }
        public int BossesKilled { get; private set; }
        public int MergeCount { get; private set; }
        public int PassengersSummoned { get; private set; }
        public int PassengersSold { get; private set; }
        public int HighestPassengerStar { get; private set; } = 1;
        public int AbilityCardsSelected { get; private set; }

        public string[] DiscoveredPassengerIds => ToArray(_discoveredPassengerIds);
        public string[] DiscoveredEnemyIds => ToArray(_discoveredEnemyIds);
        public string[] DiscoveredBossIds => ToArray(_discoveredBossIds);

        public RunPassengerMasterySnapshot[] PassengerMasteries
        {
            get
            {
                var list = new List<RunPassengerMasterySnapshot>(_mastery.Count);
                foreach (KeyValuePair<string, MasteryAccumulator> pair in _mastery)
                {
                    list.Add(new RunPassengerMasterySnapshot(
                        pair.Key,
                        pair.Value.UseCount,
                        pair.Value.HighestStar,
                        pair.Value.BossKillParticipations));
                }

                return list.ToArray();
            }
        }

        public void RecordEnemyEncounter(string enemyId, EnemyType enemyType)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return;
            }

            _discoveredEnemyIds.Add(enemyId);
            if (enemyType == EnemyType.Boss)
            {
                _discoveredBossIds.Add(enemyId);
            }
        }

        public void RecordEnemyKill(string enemyId, EnemyType enemyType)
        {
            EnemiesKilled++;
            RecordEnemyEncounter(enemyId, enemyType);

            if (enemyType == EnemyType.Boss)
            {
                BossesKilled++;
            }
        }

        public void RecordBossKillParticipation(IReadOnlyList<string> passengerIdsOnGrid)
        {
            if (passengerIdsOnGrid == null)
            {
                return;
            }

            for (int i = 0; i < passengerIdsOnGrid.Count; i++)
            {
                string id = passengerIdsOnGrid[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                MasteryAccumulator acc = GetOrCreateMastery(id);
                acc.BossKillParticipations++;
            }
        }

        public void RecordPassengerUsage(string passengerId, int starLevel)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return;
            }

            _discoveredPassengerIds.Add(passengerId);
            MasteryAccumulator acc = GetOrCreateMastery(passengerId);
            acc.UseCount++;
            if (starLevel > acc.HighestStar)
            {
                acc.HighestStar = starLevel;
            }

            UpdateHighestStar(starLevel);
        }

        public void RecordMerge(int resultingStarLevel, string passengerId = null)
        {
            MergeCount++;
            UpdateHighestStar(resultingStarLevel);

            if (!string.IsNullOrWhiteSpace(passengerId))
            {
                MasteryAccumulator acc = GetOrCreateMastery(passengerId);
                if (resultingStarLevel > acc.HighestStar)
                {
                    acc.HighestStar = resultingStarLevel;
                }
            }
        }

        public void RecordSummon(int starLevel = 1)
        {
            PassengersSummoned++;
            UpdateHighestStar(starLevel);
        }

        public void RecordSell()
        {
            PassengersSold++;
        }

        public void RecordAbilitySelected()
        {
            AbilityCardsSelected++;
        }

        public void UpdateHighestStar(int starLevel)
        {
            if (starLevel > HighestPassengerStar)
            {
                HighestPassengerStar = starLevel;
            }
        }

        /// <summary>저장 데이터로부터 상태를 복원한다.</summary>
        public void RestoreFromSave(
            int enemiesKilled,
            int mergeCount,
            int passengersSummoned,
            int passengersSold,
            int highestPassengerStar,
            int abilityCardsSelected,
            int bossesKilled = 0)
        {
            EnemiesKilled = Math.Max(0, enemiesKilled);
            BossesKilled = Math.Max(0, bossesKilled);
            MergeCount = Math.Max(0, mergeCount);
            PassengersSummoned = Math.Max(0, passengersSummoned);
            PassengersSold = Math.Max(0, passengersSold);
            HighestPassengerStar = Math.Max(1, highestPassengerStar);
            AbilityCardsSelected = Math.Max(0, abilityCardsSelected);
        }

        private MasteryAccumulator GetOrCreateMastery(string passengerId)
        {
            if (!_mastery.TryGetValue(passengerId, out MasteryAccumulator acc))
            {
                acc = new MasteryAccumulator();
                _mastery[passengerId] = acc;
            }

            return acc;
        }

        private static string[] ToArray(HashSet<string> set)
        {
            if (set == null || set.Count == 0)
            {
                return Array.Empty<string>();
            }

            var array = new string[set.Count];
            set.CopyTo(array);
            return array;
        }

        private sealed class MasteryAccumulator
        {
            public int UseCount;
            public int HighestStar = 1;
            public int BossKillParticipations;
        }
    }
}
