using System;
using System.Text;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.DebugTools
{
    /// <summary>
    /// Play Mode 디버그 전투 설정. Release 빌드에서는 비활성 스텁만 남긴다.
    /// </summary>
    public static class DebugCombatSettings
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool Invulnerable { get; set; }
        public static bool LogDamage { get; set; }
        public static int? FixedSeed { get; set; }
        public static float BattleSpeed { get; set; } = 1f;

        private static bool _damageLogSubscribed;
        private static readonly StringBuilder _logBuffer = new();

        public static void ApplyBattleSpeed()
        {
            Time.timeScale = Mathf.Clamp(BattleSpeed, 0f, 10f);
        }

        public static void EnsureDamageLogSubscription()
        {
            if (_damageLogSubscribed)
            {
                return;
            }

            LastTrain.Battle.CombatVisualEvents.EnemyDamaged += HandleEnemyDamaged;
            _damageLogSubscribed = true;
        }

        public static void ClearDamageLog()
        {
            _logBuffer.Clear();
        }

        public static string GetDamageLog()
        {
            return _logBuffer.ToString();
        }

        private static void HandleEnemyDamaged(EnemyRuntime enemy, float damage, bool isCrit)
        {
            if (!LogDamage || enemy?.Data == null)
            {
                return;
            }

            _logBuffer.Append('[')
                .Append(Time.frameCount)
                .Append("] ")
                .Append(enemy.Data.Id)
                .Append(isCrit ? " CRIT " : " ")
                .Append(damage.ToString("0.#"))
                .Append(" (hp ")
                .Append(enemy.CurrentHealth.ToString("0.#"))
                .Append('/')
                .Append(enemy.MaxHealth.ToString("0.#"))
                .AppendLine(")");

            if (_logBuffer.Length > 8000)
            {
                _logBuffer.Remove(0, _logBuffer.Length - 6000);
            }
        }
#else
        public static bool Invulnerable
        {
            get => false;
            set { }
        }

        public static bool LogDamage
        {
            get => false;
            set { }
        }

        public static int? FixedSeed
        {
            get => null;
            set { }
        }

        public static float BattleSpeed
        {
            get => 1f;
            set { }
        }

        public static void ApplyBattleSpeed()
        {
        }

        public static void EnsureDamageLogSubscription()
        {
        }

        public static void ClearDamageLog()
        {
        }

        public static string GetDamageLog() => string.Empty;
#endif
    }
}
