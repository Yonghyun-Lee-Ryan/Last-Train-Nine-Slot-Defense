using LastTrain.Passenger.Skills;
using LastTrain.Run;

namespace LastTrain.Passenger
{
    /// <summary>승객 전투 컨트롤러 생성.</summary>
    public static class PassengerFactory
    {
        public static PassengerController CreateController(PassengerRuntime runtime)
        {
            IPassengerSkill skill = PassengerSkillResolver.Create(runtime?.Data?.SkillId);
            return new PassengerController(runtime, new PassengerAttackController(), skill);
        }
    }
}
