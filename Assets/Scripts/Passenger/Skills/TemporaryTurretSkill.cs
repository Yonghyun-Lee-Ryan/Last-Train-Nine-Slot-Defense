using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>개발자: 제한 시간 동안 공격하는 임시 터렛을 소환한다.</summary>
    public sealed class TemporaryTurretSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 8f;
        public const float BaseDurationSeconds = 5f;
        public const float TurretAttackInterval = 0.8f;
        public const float TurretDamageRatio = 0.6f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.TemporaryTurret;

        public void Tick(float deltaTime, in PassengerSkillContext context)
        {
            if (context.Runtime == null
                || context.Runtime.GridSlotIndex < 0
                || context.TurretSpawner == null)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            float duration = BaseDurationSeconds
                           * context.SkillValueMultiplier
                           * (1f + context.RelicTurretDurationPercent / 100f);
            float damage = context.Runtime.GetEffectiveAttack() * TurretDamageRatio;
            context.TurretSpawner.Spawn(
                context.AttackerPosition,
                duration,
                damage,
                context.RangeInWorldUnits,
                TurretAttackInterval);

            _cooldownRemaining = BaseCooldownSeconds;
        }
    }
}
