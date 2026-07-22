using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>일정 주기로 보호막(체력 회복)을 부여한다.</summary>
    public sealed class PeriodicShieldAbility : IEnemyAbility
    {
        public const float CooldownSeconds = 12f;
        public const float ShieldHealAmount = 60f;

        private float _cooldownRemaining;
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.PeriodicShield;

        public void OnAttach(in EnemyAbilityContext context)
        {
            _stopped = false;
            _cooldownRemaining = CooldownSeconds;
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
            if (_stopped || context.Owner == null || !context.Owner.IsAlive)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            float missing = context.Owner.MaxHealth - context.Owner.CurrentHealth;
            if (missing > 0f)
            {
                context.Owner.ApplyHeal(Mathf.Min(ShieldHealAmount, missing));
            }

            _cooldownRemaining = CooldownSeconds;
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            _stopped = true;
        }
    }
}
