namespace LastTrain.Passenger.Skills
{
    /// <summary>스킬이 없거나 미등록 ID일 때 사용하는 무동작 스킬.</summary>
    public sealed class NullPassengerSkill : IPassengerSkill
    {
        public static NullPassengerSkill Instance { get; } = new();

        public string SkillId => string.Empty;

        public void Tick(float deltaTime, in PassengerSkillContext context)
        {
        }
    }
}
