using System;
using System.Collections.Generic;

namespace LastTrain.Enemy
{
    /// <summary>활성 적 목록을 관리한다.</summary>
    public sealed class EnemyRegistry
    {
        private readonly List<EnemyRuntime> _enemies = new();

        public IReadOnlyList<EnemyRuntime> Enemies => _enemies;

        public void Register(EnemyRuntime enemy)
        {
            if (enemy == null || _enemies.Contains(enemy))
            {
                return;
            }

            _enemies.Add(enemy);
            enemy.Died += HandleEnemyRemoved;
            enemy.ReachedTrain += HandleEnemyRemoved;
        }

        public void Unregister(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.Died -= HandleEnemyRemoved;
            enemy.ReachedTrain -= HandleEnemyRemoved;
            _enemies.Remove(enemy);
        }

        public void Clear()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemyRuntime enemy = _enemies[i];
                enemy.Died -= HandleEnemyRemoved;
                enemy.ReachedTrain -= HandleEnemyRemoved;
            }

            _enemies.Clear();
        }

        private void HandleEnemyRemoved(EnemyRuntime enemy)
        {
            Unregister(enemy);
        }
    }
}
