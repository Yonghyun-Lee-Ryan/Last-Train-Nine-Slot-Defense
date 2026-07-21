namespace LastTrain.Passenger.Skills
{
    /// <summary>승객 고유 스킬. 전략 패턴으로 교체 가능하다.</summary>
    public interface IPassengerSkill
    {
        string SkillId { get; }

        /// <summary>전투 틱마다 호출. 쿨타임·발동 조건을 스킬이 자체 관리한다.</summary>
        void Tick(float deltaTime, in PassengerSkillContext context);
    }
}
