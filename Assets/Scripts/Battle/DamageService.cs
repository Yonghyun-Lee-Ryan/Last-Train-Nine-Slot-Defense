using LastTrain.Audio;
using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>피해 계산 및 적용.</summary>
    public static class DamageService
    {
        private static float _nextHitSfxTime;
        private static float _nextCritSfxTime;

        /// <summary>
        /// 방어력을 적용한 최종 피해를 계산한다.
        /// defense는 0~1 비율 감소(예: 0.2 = 20% 감소).
        /// </summary>
        public static float CalculateFinalDamage(float rawDamage, float defense)
        {
            if (rawDamage <= 0f)
            {
                return 0f;
            }

            float clampedDefense = Mathf.Clamp01(defense);
            return rawDamage * (1f - clampedDefense);
        }

        public static float ApplyDamage(EnemyRuntime enemy, float rawDamage, bool isCrit = false)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return 0f;
            }

            float finalDamage = CalculateFinalDamage(rawDamage, enemy.Defense);
            enemy.ApplyDamage(finalDamage);
            CombatVisualEvents.RaiseEnemyDamaged(enemy, finalDamage, isCrit);

            if (isCrit)
            {
                if (Time.unscaledTime >= _nextCritSfxTime)
                {
                    GameAudio.PlaySfx(SfxId.CombatCrit);
                    _nextCritSfxTime = Time.unscaledTime + 0.08f;
                }
            }
            else if (Time.unscaledTime >= _nextHitSfxTime)
            {
                GameAudio.PlaySfx(SfxId.CombatHit);
                _nextHitSfxTime = Time.unscaledTime + 0.04f;
            }

            if (!enemy.IsAlive)
            {
                GameAudio.PlaySfx(SfxId.EnemyDeath);
            }

            return finalDamage;
        }
    }
}
