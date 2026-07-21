using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>
    /// 보스 전투 두뇌. EnemyController(View)와 분리되어 이동·피해는 기존 파이프라인을 재사용한다.
    /// </summary>
    public sealed class BossBrain : IDisposable
    {
        private readonly EnemyRuntime _owner;
        private readonly RunState _runState;
        private readonly IEnemySpawner _spawner;
        private readonly EnemyData _minionData;
        private readonly IReadOnlyList<IEnemyAbility> _abilities;
        private readonly BossPhaseController _phaseController = new();
        private readonly Vector2 _spawnPosition;
        private bool _disposed;

        public BossBrain(
            EnemyRuntime owner,
            RunState runState,
            IEnemySpawner spawner,
            EnemyData minionData,
            IReadOnlyList<IEnemyAbility> abilities)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _runState = runState;
            _spawner = spawner;
            _minionData = minionData;
            _abilities = abilities ?? Array.Empty<IEnemyAbility>();
            _spawnPosition = owner.Position;

            _owner.HealthChanged += HandleHealthChanged;
            _owner.Died += HandleOwnerDied;
            _owner.ReachedTrain += HandleOwnerDied;
            _phaseController.PhaseChanged += HandlePhaseChanged;

            EnemyAbilityContext context = BuildContext();
            for (int i = 0; i < _abilities.Count; i++)
            {
                _abilities[i].OnAttach(context);
            }
        }

        public EnemyRuntime Owner => _owner;
        public BossPhase CurrentPhase => _phaseController.Current;
        public event Action<EnemyRuntime, float, float> HealthChanged;
        public event Action<BossPhase, BossPhase> PhaseChanged;

        public static BossBrain Create(
            EnemyRuntime owner,
            RunState runState,
            IEnemySpawner spawner,
            EnemyData minionData)
        {
            if (owner?.Data == null)
            {
                return null;
            }

            bool isBoss = owner.EnemyType == EnemyType.Boss
                          || !string.IsNullOrWhiteSpace(owner.Data.AbilityId);
            if (!isBoss)
            {
                return null;
            }

            string abilityId = string.IsNullOrWhiteSpace(owner.Data.AbilityId)
                ? EnemyAbilityIds.BossMvp
                : owner.Data.AbilityId;

            return new BossBrain(
                owner,
                runState,
                spawner,
                minionData,
                EnemyAbilityResolver.Create(abilityId));
        }

        public void Tick(float deltaTime)
        {
            if (_disposed || _owner == null || !_owner.IsAlive)
            {
                return;
            }

            EnemyAbilityContext context = BuildContext();
            for (int i = 0; i < _abilities.Count; i++)
            {
                _abilities[i].Tick(deltaTime, context);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EnemyAbilityContext context = BuildContext();
            for (int i = 0; i < _abilities.Count; i++)
            {
                _abilities[i].OnOwnerDied(context);
            }

            if (_owner != null)
            {
                _owner.HealthChanged -= HandleHealthChanged;
                _owner.Died -= HandleOwnerDied;
                _owner.ReachedTrain -= HandleOwnerDied;
            }

            _phaseController.PhaseChanged -= HandlePhaseChanged;
        }

        private void HandleHealthChanged(EnemyRuntime enemy, float current, float max)
        {
            _phaseController.NotifyHealth(current, max);
            HealthChanged?.Invoke(enemy, current, max);
        }

        private void HandlePhaseChanged(BossPhase previous, BossPhase next)
        {
            EnemyAbilityContext context = BuildContext();
            for (int i = 0; i < _abilities.Count; i++)
            {
                _abilities[i].OnPhaseChanged(previous, next, context);
            }

            PhaseChanged?.Invoke(previous, next);
        }

        private void HandleOwnerDied(EnemyRuntime _)
        {
            Dispose();
        }

        private EnemyAbilityContext BuildContext()
        {
            return new EnemyAbilityContext(
                _owner,
                _runState,
                _spawner,
                _minionData,
                _spawnPosition);
        }
    }
}
