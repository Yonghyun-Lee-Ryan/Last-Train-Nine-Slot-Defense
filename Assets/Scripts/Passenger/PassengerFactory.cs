using LastTrain.Run;

namespace LastTrain.Passenger
{
    /// <summary>승객 전투 컨트롤러 생성.</summary>
    public static class PassengerFactory
    {
        public static PassengerController CreateController(PassengerRuntime runtime)
        {
            return new PassengerController(runtime, new PassengerAttackController());
        }
    }
}
