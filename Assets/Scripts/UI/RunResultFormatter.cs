using LastTrain.Run;

namespace LastTrain.UI
{
    /// <summary>Result Scene에 표시할 RunResult 텍스트를 만든다.</summary>
    public static class RunResultFormatter
    {
        public static string GetTitle(RunResult result)
        {
            if (result == null)
            {
                return "결과";
            }

            return result.IsVictory ? "도착 성공!" : "게임 오버";
        }

        public static string GetOverlayMessage(RunResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            return result.IsVictory ? "최종 역에 도착했습니다!" : "객차가 파괴되었습니다.";
        }

        public static string BuildStatsText(RunResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            return
                $"도달 역: {result.ReachedStationIndex}\n" +
                $"완료 역: {result.CompletedStationCount}\n" +
                $"처치 수: {result.EnemiesKilled}\n" +
                $"합성 수: {result.MergeCount}\n" +
                $"최고 승객 등급: {result.HighestPassengerStar}★\n" +
                $"남은 내구도: {result.RemainingTrainHp}/{result.TrainMaxHp}\n" +
                $"획득 코인: {result.TotalCoinsEarned}\n" +
                $"보유 코인: {result.FinalCoins}";
        }
    }
}
