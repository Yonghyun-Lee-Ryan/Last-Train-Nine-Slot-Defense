using LastTrain.Ability;
using LastTrain.Enemy;
using LastTrain.Run;

namespace LastTrain.Battle
{
    /// <summary>적 처치 보상 처리.</summary>
    public static class EnemyRewardService
    {
        public static bool TryGrantKillReward(RunState runState, EnemyRuntime enemy)
        {
            if (runState == null || enemy == null || enemy.Resolution != EnemyResolution.Killed)
            {
                return false;
            }

            int coins = enemy.CoinReward;
            if (runState.Abilities?.Modifiers != null)
            {
                coins = AbilityEffectCalculator.ApplyPercentBonus(
                    enemy.CoinReward,
                    runState.Abilities.Modifiers.CoinOnKillPercent);
            }

            runState.RecordEnemyKill(coins, enemy);
            return true;
        }
    }
}
