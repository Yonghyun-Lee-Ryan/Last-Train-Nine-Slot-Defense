using System.Collections.Generic;

namespace LastTrain.Enemy
{
    /// <summary>abilityId → IEnemyAbility 목록 생성.</summary>
    public static class EnemyAbilityResolver
    {
        public static IReadOnlyList<IEnemyAbility> Create(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return System.Array.Empty<IEnemyAbility>();
            }

            if (abilityId == EnemyAbilityIds.BossMvp)
            {
                return new IEnemyAbility[]
                {
                    new SpawnMinionsAbility(),
                    new PassengerAttackSpeedDebuffAbility(),
                    new EnrageMoveSpeedAbility()
                };
            }

            return abilityId switch
            {
                EnemyAbilityIds.SpawnMinions => new IEnemyAbility[] { new SpawnMinionsAbility() },
                EnemyAbilityIds.AttackSpeedDebuff => new IEnemyAbility[] { new PassengerAttackSpeedDebuffAbility() },
                EnemyAbilityIds.EnrageMoveSpeed => new IEnemyAbility[] { new EnrageMoveSpeedAbility() },
                _ => System.Array.Empty<IEnemyAbility>()
            };
        }
    }
}
