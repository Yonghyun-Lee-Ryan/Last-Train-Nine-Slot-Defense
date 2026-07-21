using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>보스 스킬이 적/승객을 스폰할 때 사용한다.</summary>
    public interface IEnemySpawner
    {
        bool TrySpawn(EnemyData data, Vector2? position = null);
    }

    /// <summary>적 고유 스킬. 틱 기반이며 Coroutine에 의존하지 않는다.</summary>
    public interface IEnemyAbility
    {
        string AbilityId { get; }

        void OnAttach(in EnemyAbilityContext context);

        void Tick(float deltaTime, in EnemyAbilityContext context);

        void OnPhaseChanged(BossPhase previous, BossPhase next, in EnemyAbilityContext context);

        void OnOwnerDied(in EnemyAbilityContext context);
    }

    public readonly struct EnemyAbilityContext
    {
        public EnemyAbilityContext(
            EnemyRuntime owner,
            RunState runState,
            IEnemySpawner spawner,
            EnemyData minionData,
            Vector2 spawnPosition)
        {
            Owner = owner;
            RunState = runState;
            Spawner = spawner;
            MinionData = minionData;
            SpawnPosition = spawnPosition;
        }

        public EnemyRuntime Owner { get; }
        public RunState RunState { get; }
        public IEnemySpawner Spawner { get; }
        public EnemyData MinionData { get; }
        public Vector2 SpawnPosition { get; }
    }
}
