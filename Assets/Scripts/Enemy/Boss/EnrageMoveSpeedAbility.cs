namespace LastTrain.Enemy
{
    /// <summary>Enraged 단계에서 보스 이동속도를 증가시킨다.</summary>
    public sealed class EnrageMoveSpeedAbility : IEnemyAbility
    {
        public const float EnrageMultiplier = 1.5f;

        public string AbilityId => EnemyAbilityIds.EnrageMoveSpeed;

        public void OnAttach(in EnemyAbilityContext context)
        {
            if (context.Owner != null)
            {
                context.Owner.MoveSpeedMultiplier = 1f;
            }
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
            if (context.Owner == null)
            {
                return;
            }

            context.Owner.MoveSpeedMultiplier = next == BossPhase.Enraged ? EnrageMultiplier : 1f;
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            if (context.Owner != null)
            {
                context.Owner.MoveSpeedMultiplier = 1f;
            }
        }
    }
}
