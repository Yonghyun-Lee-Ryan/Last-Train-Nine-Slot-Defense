using UnityEngine;

namespace LastTrain.Passenger.Skills
{
    /// <summary>임시 터렛 스폰 추상화. 풀/뷰 구현과 스킬 로직을 분리한다.</summary>
    public interface ITemporaryTurretSpawner
    {
        void Spawn(Vector2 position, float durationSeconds, float damage, float rangeInWorldUnits, float attackInterval);
    }
}
