using LastTrain.Audio;
using LastTrain.DebugTools;
using LastTrain.Enemy;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>적의 객차 도달 피해 처리.</summary>
    public static class TrainDamageService
    {
        public static bool TryApplyTrainDamage(RunState runState, EnemyRuntime enemy)
        {
            if (runState?.Train == null || enemy == null || !enemy.TryResolve(EnemyResolution.ReachedTrain))
            {
                return false;
            }

            if (DebugCombatSettings.Invulnerable)
            {
                return true;
            }

            int damage = Mathf.Max(0, Mathf.RoundToInt(enemy.TrainDamage));
            if (damage > 0)
            {
                runState.Train.ApplyDamage(damage);
                GameAudio.PlaySfx(SfxId.TrainDamage);
            }

            return true;
        }
    }
}
