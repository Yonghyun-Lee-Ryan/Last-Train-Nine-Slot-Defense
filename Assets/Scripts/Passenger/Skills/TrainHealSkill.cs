using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>간호사: 주기적으로 객차 내구도를 회복한다.</summary>
    public sealed class TrainHealSkill : IPassengerSkill
    {
        public const float BaseCooldownSeconds = 4f;
        public const int BaseHealAmount = 5;

        private float _cooldownRemaining;

        public string SkillId => PassengerSkillIds.TrainHeal;

        public void Tick(float deltaTime, in PassengerSkillContext context)
        {
            if (context.Runtime == null || context.Runtime.GridSlotIndex < 0 || context.Train == null)
            {
                return;
            }

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            float healFloat = BaseHealAmount
                              * context.SkillValueMultiplier
                              * (1f + context.Modifiers.NurseHealPercent / 100f)
                              * (1f + context.SynergyModifiers.TrainHealPercent / 100f);
            int heal = Mathf.Max(1, Mathf.RoundToInt(healFloat));
            context.Train.Heal(heal);
            _cooldownRemaining = BaseCooldownSeconds;
            LastTrain.Battle.CombatVisualEvents.RaiseTrainHealed(context.TrainTarget);
            LastTrain.Battle.CombatVisualEvents.RaisePassengerSkillActivated(context.Runtime.InstanceId);
        }
    }
}
