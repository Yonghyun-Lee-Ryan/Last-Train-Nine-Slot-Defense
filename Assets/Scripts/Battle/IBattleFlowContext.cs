using LastTrain.Data;

namespace LastTrain.Battle
{
    /// <summary>StationManager가 BattleManager와 통신할 때 사용한다.</summary>
    public interface IBattleFlowContext
    {
        bool TrySpawnEnemy(EnemyData enemyData);

        int GetAliveEnemyCount();
    }
}
