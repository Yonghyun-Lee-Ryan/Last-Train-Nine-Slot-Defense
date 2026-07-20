using LastTrain.Run;

namespace LastTrain.Battle
{
    /// <summary>RunPhase에 따른 전투 활성 여부.</summary>
    public static class BattlePhaseUtility
    {
        public static bool IsCombatActive(RunPhase phase)
        {
            return phase == RunPhase.Fighting || phase == RunPhase.WaveStarting;
        }

        public static bool IsWaveSpawnActive(RunPhase phase)
        {
            return phase == RunPhase.Fighting;
        }
    }
}
