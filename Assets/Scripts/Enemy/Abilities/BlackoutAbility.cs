using LastTrain.Difficulty;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>높은 난이도에서만 모든 승객 공격속도를 감소시킨다.</summary>
    public sealed class BlackoutAbility : IEnemyAbility
    {
        public const float CastCooldownSeconds = 14f;
        public const float DebuffDurationSeconds = 5f;
        public const float AttackSpeedPenaltyPercent = -25f;

        private float _castCooldown;
        private float _debuffRemaining;
        private bool _debuffActive;
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.Blackout;

        public void OnAttach(in EnemyAbilityContext context)
        {
            _stopped = false;
            _castCooldown = CastCooldownSeconds;
            _debuffRemaining = 0f;
            _debuffActive = false;
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
            if (_stopped)
            {
                return;
            }

            if (_debuffActive)
            {
                _debuffRemaining = Mathf.Max(0f, _debuffRemaining - deltaTime);
                if (_debuffRemaining <= 0f)
                {
                    ClearDebuff(context.RunState);
                }
            }

            if (!IsDifficultyHighEnough(context.RunState) || context.Owner == null || !context.Owner.IsAlive)
            {
                return;
            }

            _castCooldown = Mathf.Max(0f, _castCooldown - deltaTime);
            if (_castCooldown > 0f)
            {
                return;
            }

            ApplyDebuff(context.RunState);
            _castCooldown = CastCooldownSeconds;
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            _stopped = true;
            ClearDebuff(context.RunState);
        }

        private static bool IsDifficultyHighEnough(RunState runState)
        {
            string id = runState?.DifficultyId;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            return id == DifficultyIds.Express
                   || id == DifficultyIds.MidnightExpress
                   || id == DifficultyIds.NonstopHell;
        }

        private void ApplyDebuff(RunState runState)
        {
            if (runState == null)
            {
                return;
            }

            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger == null)
                {
                    continue;
                }

                passenger.RemoveBuff(BossDebuffIds.BlackoutSlow);
                passenger.AddBuff(new RuntimeBuff(
                    BossDebuffIds.BlackoutSlow,
                    attackSpeedPercentBonus: AttackSpeedPenaltyPercent));
            }

            _debuffActive = true;
            _debuffRemaining = DebuffDurationSeconds;
        }

        private void ClearDebuff(RunState runState)
        {
            if (!_debuffActive)
            {
                _debuffRemaining = 0f;
                return;
            }

            if (runState != null)
            {
                for (int i = 0; i < RunState.GridSlotCount; i++)
                {
                    runState.GetPassengerAtSlot(i)?.RemoveBuff(BossDebuffIds.BlackoutSlow);
                }
            }

            _debuffActive = false;
            _debuffRemaining = 0f;
        }
    }
}
