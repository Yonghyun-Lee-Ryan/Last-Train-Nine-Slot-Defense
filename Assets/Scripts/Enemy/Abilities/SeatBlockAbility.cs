using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>주기적으로 특정 슬롯 승객의 공격을 방해한다.</summary>
    public sealed class SeatBlockAbility : IEnemyAbility
    {
        public const float CastCooldownSeconds = 8f;
        public const float BlockDurationSeconds = 3f;

        private float _castCooldown;
        private bool _stopped;

        public string AbilityId => EnemyAbilityIds.SeatBlock;

        public void OnAttach(in EnemyAbilityContext context)
        {
            _stopped = false;
            _castCooldown = CastCooldownSeconds * 0.5f;
        }

        public void Tick(float deltaTime, in EnemyAbilityContext context)
        {
            if (_stopped || context.RunState == null)
            {
                return;
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

            int slot = PickOccupiedSlot(context.RunState);
            if (slot < 0)
            {
                _castCooldown = CastCooldownSeconds;
                return;
            }

            PassengerRuntime passenger = context.RunState.GetPassengerAtSlot(slot);
            passenger?.SetAttackBlock(BlockDurationSeconds);
            _castCooldown = CastCooldownSeconds;
        }

        public void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context)
        {
        }

        public void OnOwnerDied(in EnemyAbilityContext context)
        {
            _stopped = true;
        }

        private static int PickOccupiedSlot(RunState runState)
        {
            int first = -1;
            int count = 0;
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                if (runState.GetPassengerAtSlot(i) == null)
                {
                    continue;
                }

                count++;
                if (first < 0)
                {
                    first = i;
                }
            }

            if (count <= 1)
            {
                return first;
            }

            int pick = Random.Range(0, count);
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                if (runState.GetPassengerAtSlot(i) == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return i;
                }

                pick--;
            }

            return first;
        }
    }
}
