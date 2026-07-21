using System.Collections.Generic;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>
    /// 임시 터렛 Object Pool.
    /// 만료된 인스턴스는 재사용 큐로 반환한다.
    /// </summary>
    public sealed class TemporaryTurretService : ITemporaryTurretSpawner
    {
        private readonly List<TemporaryTurretRuntime> _active = new();
        private readonly Queue<TemporaryTurretRuntime> _available = new();

        public IReadOnlyList<TemporaryTurretRuntime> ActiveTurrets => _active;
        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;

        public void Spawn(
            Vector2 position,
            float durationSeconds,
            float damage,
            float rangeInWorldUnits,
            float attackInterval)
        {
            TemporaryTurretRuntime turret = _available.Count > 0
                ? _available.Dequeue()
                : new TemporaryTurretRuntime();

            turret.Activate(position, durationSeconds, damage, rangeInWorldUnits, attackInterval);
            _active.Add(turret);
        }

        public void Tick(float deltaTime, IReadOnlyList<EnemyRuntime> enemies)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                TemporaryTurretRuntime turret = _active[i];
                if (turret == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                turret.Tick(deltaTime, enemies);
                if (turret.IsExpired)
                {
                    _active.RemoveAt(i);
                    _available.Enqueue(turret);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null)
                {
                    _available.Enqueue(_active[i]);
                }
            }

            _active.Clear();
        }
    }
}
