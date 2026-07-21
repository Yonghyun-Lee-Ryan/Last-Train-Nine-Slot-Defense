using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Enemy;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>스킬 발동에 필요한 전투 컨텍스트. 값으로 전달한다.</summary>
    public readonly struct PassengerSkillContext
    {
        public PassengerSkillContext(
            PassengerRuntime runtime,
            Vector2 attackerPosition,
            float rangeInWorldUnits,
            IReadOnlyList<EnemyRuntime> enemies,
            TrainState train,
            AbilityModifiers modifiers,
            Vector2 spawnPoint,
            Vector2 trainTarget,
            ITemporaryTurretSpawner turretSpawner,
            RandomService random,
            SynergyModifiers synergyModifiers = null)
        {
            Runtime = runtime;
            AttackerPosition = attackerPosition;
            RangeInWorldUnits = rangeInWorldUnits;
            Enemies = enemies;
            Train = train;
            Modifiers = modifiers ?? AbilityModifiers.Empty;
            SpawnPoint = spawnPoint;
            TrainTarget = trainTarget;
            TurretSpawner = turretSpawner;
            Random = random;
            SynergyModifiers = synergyModifiers ?? SynergyModifiers.Empty;
        }

        public PassengerRuntime Runtime { get; }
        public Vector2 AttackerPosition { get; }
        public float RangeInWorldUnits { get; }
        public IReadOnlyList<EnemyRuntime> Enemies { get; }
        public TrainState Train { get; }
        public AbilityModifiers Modifiers { get; }
        public SynergyModifiers SynergyModifiers { get; }
        public Vector2 SpawnPoint { get; }
        public Vector2 TrainTarget { get; }
        public ITemporaryTurretSpawner TurretSpawner { get; }
        public RandomService Random { get; }

        public float SkillValueMultiplier =>
            Runtime != null ? Runtime.GetEffectiveSkillMultiplier() : 1f;
    }
}
