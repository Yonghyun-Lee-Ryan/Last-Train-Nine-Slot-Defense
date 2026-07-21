namespace LastTrain.Enemy
{
    public sealed class NullEnemyAbility : IEnemyAbility
    {
        public static NullEnemyAbility Instance { get; } = new();

        public string AbilityId => string.Empty;

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
        }
    }
}
