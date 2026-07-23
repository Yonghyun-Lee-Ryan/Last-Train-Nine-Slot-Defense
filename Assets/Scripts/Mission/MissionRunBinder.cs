using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Save;
using LastTrain.Shop;
using UnityEngine;

namespace LastTrain.Mission
{
    /// <summary>전투/상점/능력 이벤트를 구독해 미션 진행을 갱신한다.</summary>
    public sealed class MissionRunBinder : IDisposable
    {
        private readonly GameDatabase _database;
        private StationManager _stationManager;
        private AbilityManager _abilityManager;
        private RunState _runState;
        private readonly HashSet<string> _distinctPassengers = new(StringComparer.Ordinal);
        private bool _boundCombatVisuals;

        public MissionRunBinder(GameDatabase database)
        {
            _database = database;
        }

        public void Bind(
            RunState runState,
            StationManager stationManager,
            AbilityManager abilityManager,
            ShopService shop)
        {
            Unbind();
            _runState = runState;
            _stationManager = stationManager;
            _abilityManager = abilityManager;
            _distinctPassengers.Clear();

            if (_stationManager != null)
            {
                _stationManager.StationCompleted += HandleStationCompleted;
            }

            if (_abilityManager != null)
            {
                _abilityManager.AbilitySelected += HandleAbilitySelected;
            }

            MergeService.Merged += HandleMerged;
            CombatVisualEvents.EnemyDamaged += HandleEnemyDamaged;
            _boundCombatVisuals = true;
        }

        public void NotifyPassengerPlaced(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return;
            }

            if (_distinctPassengers.Add(passengerId))
            {
                Apply(MissionEventType.DistinctPassengerPlaced, 1, passengerId);
            }
        }

        public void NotifySummoned()
        {
            Apply(MissionEventType.Summoned, 1);
        }

        public void NotifyShopPurchased()
        {
            Apply(MissionEventType.ShopPurchased, 1);
        }

        public void NotifyAdsUsed()
        {
            _runState?.MarkAdsUsed();
        }

        public void Dispose()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_stationManager != null)
            {
                _stationManager.StationCompleted -= HandleStationCompleted;
            }

            if (_abilityManager != null)
            {
                _abilityManager.AbilitySelected -= HandleAbilitySelected;
            }

            MergeService.Merged -= HandleMerged;

            if (_boundCombatVisuals)
            {
                CombatVisualEvents.EnemyDamaged -= HandleEnemyDamaged;
                _boundCombatVisuals = false;
            }

            _stationManager = null;
            _abilityManager = null;
            _runState = null;
        }

        private void HandleStationCompleted(StationData _)
        {
            int hp = _runState?.Train?.CurrentHp ?? 0;
            bool noAds = _runState == null || !_runState.AdsUsedThisRun;
            Apply(MissionEventType.StationCompleted, 1, noAds ? "no_ads" : "ads", hp);
        }

        private void HandleAbilitySelected(AbilitySelectResult result, AbilityData ability)
        {
            if (result != AbilitySelectResult.Success || ability == null)
            {
                return;
            }

            Apply(MissionEventType.AbilitySelected, 1, ability.Id, (int)ability.Rarity);
        }

        private void HandleMerged(int resultingStar, string passengerId)
        {
            Apply(MissionEventType.Merge, 1, passengerId);
            if (resultingStar > 0)
            {
                Apply(MissionEventType.PassengerStarReached, resultingStar, passengerId);
            }
        }

        private void HandleEnemyDamaged(EnemyRuntime enemy, float damage, bool isCrit)
        {
            if (enemy?.Data == null || damage <= 0f)
            {
                return;
            }

            if (enemy.Data.EnemyType == EnemyType.Boss)
            {
                Apply(MissionEventType.BossDamaged, Mathf.Max(1, Mathf.RoundToInt(damage)));
            }

            if (!enemy.IsAlive)
            {
                Apply(MissionEventType.EnemyKilled, 1, enemy.Data.Id, (int)enemy.Data.EnemyType);
            }
        }

        private void Apply(MissionEventType type, int amount, string id = null, int param = 0)
        {
            IReadOnlyList<MissionData> missions = _database?.Missions;
            if (missions == null || missions.Count == 0)
            {
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            MissionProgressService.ApplyEvent(meta, missions, type, amount, id, param);
            MetaSaveSystem.Save(meta);
        }
    }
}
