using System;
using LastTrain.Data;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>
    /// 적 런타임 상태. EnemyData(정적)와 분리된다.
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
            MoveSpeedMultiplier = 1f;
        }

        public event Action<EnemyRuntime> Died;
        public event Action<EnemyRuntime> ReachedTrain;
        public event Action<EnemyRuntime, float, float> HealthChanged;

        public EnemyData Data { get; }
        public string InstanceId { get; }
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public Vector2 Position { get; set; }
        public float MoveSpeedMultiplier { get; set; }
        public EnemyResolution Resolution { get; private set; } = EnemyResolution.None;
        public bool IsResolved => Resolution != EnemyResolution.None;
        public bool IsAlive => !IsResolved && CurrentHealth > 0f;
        public float HealthRatio => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        public float MoveSpeed => Data.MoveSpeed * Math.Max(0.01f, MoveSpeedMultiplier);
        public float TrainDamage => Data.TrainDamage;
        public int CoinReward => Data.CoinReward;
        public EnemyType EnemyType => Data.EnemyType;
        public float Defense => Data.Defense;

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(this, CurrentHealth, MaxHealth);
            if (CurrentHealth <= 0f)
            {
                TryResolve(EnemyResolution.Killed);
            }
        }

        /// <summary>사망·객차 도달 등 종료 처리를 한 번만 수행한다.</summary>
        public bool TryResolve(EnemyResolution resolution)
        {
            if (IsResolved || resolution == EnemyResolution.None)
            {
                return false;
            }

            Resolution = resolution;
            switch (resolution)
            {
                case EnemyResolution.Killed:
                    Died?.Invoke(this);
                    break;

                case EnemyResolution.ReachedTrain:
                    ReachedTrain?.Invoke(this);
                    break;
            }

            return true;
        }
    }
}
