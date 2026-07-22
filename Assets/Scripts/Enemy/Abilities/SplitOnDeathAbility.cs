using LastTrain.Data;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>사망 시 소형 적을 생성한다.</summary>
    public sealed class SplitOnDeathAbility : IEnemyAbility
    {
        public const int SplitCount = 2;

        public string AbilityId => EnemyAbilityIds.SplitOnDeath;

        public void OnAttach(in EnemyAbilityContext context)
        {
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            EnemyData splitData = context.SplitMinionData ?? context.MinionData;
            if (splitData == null || context.Spawner == null)
            {
                return;
            }

            for (int i = 0; i < SplitCount; i++)
            {
                Vector2 offset = new Vector2((i - 0.5f) * 50f, i * 15f);
                context.Spawner.TrySpawn(splitData, context.Owner?.Position ?? context.SpawnPosition + offset);
            }
        }
    }
}
