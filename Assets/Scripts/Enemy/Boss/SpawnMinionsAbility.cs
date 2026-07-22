using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>일정 주기마다 일반 적 3마리를 소환한다.</summary>
    public sealed class SpawnMinionsAbility : IEnemyAbility
    {
        public const float CooldownSeconds = 8f;
        public const int MinionCount = 3;

        private float _cooldownRemaining;
        private float _cooldownMultiplier = 1f;
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.SpawnMinions;

        public void OnAttach(in EnemyAbilityContext context)
        {
            _stopped = false;
            _cooldownRemaining = CooldownSeconds;
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
            if (_stopped || context.Owner == null || !context.Owner.IsAlive || context.Spawner == null)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            if (context.MinionData == null)
            {
                return;
            }

            for (int i = 0; i < MinionCount; i++)
            {
                Vector2 offset = new Vector2((i - 1) * 40f, i * 20f);
                context.Spawner.TrySpawn(context.MinionData, context.SpawnPosition + offset);
            }

            _cooldownRemaining = CooldownSeconds * _cooldownMultiplier;
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
            _cooldownMultiplier = next switch
            {
                BossPhase.Enraged => 0.6f,
                BossPhase.DoorOpen => 0.8f,
                _ => 1f
            };
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            _stopped = true;
            _cooldownRemaining = 0f;
        }
    }
}
