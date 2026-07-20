using System;
using LastTrain.Data;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>
    /// 적 런타임 상태. EnemyData(정적)와 분리된다.
    /// 이동·View는 개발 단위 6에서 확장한다.
    /// </summary>
    public sealed class EnemyRuntime
    {
        public EnemyRuntime(EnemyData data, float maxHealth, Vector2 spawnPosition, string instanceId = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Position = spawnPosition;
            InstanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
        }

        public event Action<EnemyRuntime> Died;

        public EnemyData Data { get; }
        public string InstanceId { get; }
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public Vector2 Position { get; set; }
        public bool IsAlive => CurrentHealth > 0f;

        public float MoveSpeed => Data.MoveSpeed;
        public EnemyType EnemyType => Data.EnemyType;
        public float Defense => Data.Defense;

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
            if (CurrentHealth <= 0f)
            {
                Died?.Invoke(this);
            }
        }
    }
}
