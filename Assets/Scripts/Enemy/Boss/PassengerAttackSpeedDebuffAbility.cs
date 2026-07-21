using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>주기적으로 모든 배치 승객의 공격속도를 일정 시간 감소시킨다.</summary>
    public sealed class PassengerAttackSpeedDebuffAbility : IEnemyAbility
    {
        public const float CastCooldownSeconds = 10f;
        public const float DebuffDurationSeconds = 4f;
        public const float AttackSpeedPenaltyPercent = -20f;

        private float _castCooldown;
        private float _debuffRemaining;
        private bool _debuffActive;
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.AttackSpeedDebuff;
        public bool IsDebuffActive => _debuffActive;
        public float DebuffRemaining => _debuffRemaining;

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

            if (context.Owner == null || !context.Owner.IsAlive)
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

                passenger.RemoveBuff(BossDebuffIds.AttackSpeedSlow);
                passenger.AddBuff(new RuntimeBuff(
                    BossDebuffIds.AttackSpeedSlow,
                    attackPercentBonus: 0f,
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
                    runState.GetPassengerAtSlot(i)?.RemoveBuff(BossDebuffIds.AttackSpeedSlow);
                }
            }

            _debuffActive = false;
            _debuffRemaining = 0f;
        }
    }
}
