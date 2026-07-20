using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 GridManager를 RunState와 연결하고,
    /// 개발 단위 4 테스트용 승객을 초기 배치한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameGridBootstrap : MonoBehaviour
    {
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private PassengerData[] debugPassengers;
        [SerializeField] private int[] debugSlotIndices = { 0, 1, 2, 3 };

        private void Start()
        {
            if (gridManager == null)
            {
                Debug.LogError("[GameGridBootstrap] gridManager가 연결되지 않았습니다.", this);
                return;
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                Debug.LogError("[GameGridBootstrap] AppRoot가 없습니다. Bootstrap Scene부터 실행하세요.", this);
                return;
            }

            GameSession session = appRoot.GameSession;
            if (!session.HasActiveRun)
            {
                session.StartNewRun();
            }

            RunState runState = session.RunState;
            PlaceDebugPassengers(runState);
            gridManager.Initialize(runState);
        }

        private void PlaceDebugPassengers(RunState runState)
        {
            if (debugPassengers == null || debugPassengers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < debugPassengers.Length; i++)
            {
                PassengerData data = debugPassengers[i];
                if (data == null)
                {
                    continue;
                }

                int slotIndex = i < debugSlotIndices.Length ? debugSlotIndices[i] : i;
                if (slotIndex < 0 || slotIndex >= RunState.GridSlotCount)
                {
                    continue;
                }

                if (!runState.IsSlotEmpty(slotIndex))
                {
                    continue;
                }

                PassengerRuntime passenger = PassengerRuntime.Create(data);
                runState.TryPlacePassenger(slotIndex, passenger);
            }
        }
    }
}
