using LastTrain.Enemy;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>헬스 트레이너: 사거리 내 적을 스폰 방향으로 넉백.</summary>
    public sealed class KnockbackSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 2.5f;
        public const float BaseKnockbackDistance = 120f;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.Knockback;

        public void Tick(float deltaTime, in PassengerSkillContext context)
        {
            if (context.Runtime == null || context.Runtime.GridSlotIndex < 0)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            EnemyRuntime target = LastTrain.Passenger.TargetingService.SelectTarget(
                context.Enemies,
                context.AttackerPosition,
                context.RangeInWorldUnits,
                context.Runtime.Data.TargetPriority);

            if (target == null || !target.IsAlive)
            {
                return;
            }

            float distance = BaseKnockbackDistance * context.SkillValueMultiplier;
            EnemyMovementService.ApplyKnockback(
                target,
                context.SpawnPoint,
                context.TrainTarget,
                distance);

            _cooldownRemaining = BaseCooldownSeconds;
        }
    }
}
