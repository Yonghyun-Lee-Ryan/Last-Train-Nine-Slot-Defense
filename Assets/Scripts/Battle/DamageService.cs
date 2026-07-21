using LastTrain.Enemy;

namespace LastTrain.Battle
{
    /// <summary>피해 계산 및 적용.</summary>
    public static class DamageService
    {
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

            float clampedDefense = UnityEngine.Mathf.Clamp01(defense);
            return rawDamage * (1f - clampedDefense);
        }

        public static float ApplyDamage(EnemyRuntime enemy, float rawDamage)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return 0f;
            }

            float finalDamage = CalculateFinalDamage(rawDamage, enemy.Defense);
            enemy.ApplyDamage(finalDamage);
            CombatVisualEvents.RaiseEnemyDamaged(enemy, finalDamage);
            return finalDamage;
        }
    }
}
