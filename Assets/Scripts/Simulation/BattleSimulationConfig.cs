using System;
using System.Collections.Generic;

namespace LastTrain.Simulation
{
    [Serializable]
    public sealed class BattleSimulationSlotConfig
    {
        public string passengerId = string.Empty;
        public int starLevel = 1;
    }

    [Serializable]
    public sealed class BattleSimulationConfig
    {
        public int baseSeed = 1;
        public int iterations = 100;
        public float deltaTime = 0.1f;
        public float maxSimulatedSeconds = 600f;
        public int startingStationIndex = 1;
        public int maxStationIndex = 3;
        public float difficultyMultiplier = 1f;
        public int initialCoins = 50;
        public int initialTrainHp = 100;
        public BattleSimulationSlotConfig[] slots = new BattleSimulationSlotConfig[9];
        public string[] abilityIds = Array.Empty<string>();
        public bool autoContinueAbilityRewards = true;
    }

    public sealed class BattleSimulationRunResult
    {
        public bool IsVictory;
        public int RemainingTrainHp;
        public int TrainMaxHp;
        public float SimulatedSeconds;
        public int EnemiesKilled;
        public int BossesKilled;
        public int Seed;
        public Dictionary<string, float> DamageByPassengerId = new();
        public Dictionary<string, int> SkillTicksByPassengerId = new();
        public Dictionary<string, int> TrainReachesByEnemyId = new();
    }

    public sealed class BattleSimulationAggregate
    {
        public int Iterations;
        public int Wins;
        public float WinRate;
        public float AvgRemainingHp;
        public float MinRemainingHp;
        public float MaxRemainingHp;
        public float StdDevRemainingHp;
        public float AvgSimulatedSeconds;
        public float MinSimulatedSeconds;
        public float MaxSimulatedSeconds;
        public Dictionary<string, float> AvgDamageByPassengerId = new();
        public Dictionary<string, float> AvgSkillTicksByPassengerId = new();
        public Dictionary<string, float> AvgTrainReachesByEnemyId = new();
        public List<BattleSimulationRunResult> Runs = new();
    }
}
