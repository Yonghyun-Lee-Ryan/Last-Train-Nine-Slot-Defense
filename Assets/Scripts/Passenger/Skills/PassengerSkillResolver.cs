namespace LastTrain.Passenger.Skills
{
    /// <summary>
    /// skillId → IPassengerSkill 인스턴스 생성.
    /// PassengerController는 구체 타입을 알지 않는다.
    /// </summary>
    public static class PassengerSkillResolver
    {
        public static IPassengerSkill Create(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return NullPassengerSkill.Instance;
            }

            return skillId switch
            {
                PassengerSkillIds.Knockback => new KnockbackSkill(),
                PassengerSkillIds.TrainHeal => new TrainHealSkill(),
                PassengerSkillIds.TemporaryTurret => new TemporaryTurretSkill(),
                PassengerSkillIds.CriticalAreaDamage => new CriticalAreaDamageSkill(),
                _ => NullPassengerSkill.Instance
            };
        }
    }
}
