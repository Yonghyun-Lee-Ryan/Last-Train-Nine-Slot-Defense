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
            enemy.Died += HandleEnemyDied;
        }

        public void Clear()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Died -= HandleEnemyDied;
            }

            _enemies.Clear();
        }

        private void HandleEnemyDied(EnemyRuntime enemy)
        {
            enemy.Died -= HandleEnemyDied;
            _enemies.Remove(enemy);
        }
    }
}
