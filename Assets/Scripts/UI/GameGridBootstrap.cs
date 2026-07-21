using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Synergy;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 GridManager를 RunState와 연결하고,
    /// 새 회차에서만 테스트용 승객을 초기 배치한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameGridBootstrap : MonoBehaviour
    {
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private PassengerData[] debugPassengers;
        [SerializeField] private int[] debugSlotIndices = { 0, 1, 2, 3 };

        [Tooltip("첫 번째 debug 승객을 한 명 더 배치해 합성 테스트를 쉽게 한다.")]
        [SerializeField] private bool placeMergePair = true;
        [SerializeField] private int mergePairSlotIndex = 5;

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
            EnsureSynergyCatalog(runState);

            // 이미 배치된 승객이 있으면(씬 재진입) 디버그 시드를 다시 넣지 않는다.
            if (!HasAnyPassenger(runState))
            {
                PlaceDebugPassengers(runState);
            }

            SynergyEffectApplier.Refresh(runState);
            gridManager.Initialize(runState);
        }

        private void EnsureSynergyCatalog(RunState runState)
        {
            if (runState?.Synergies == null)
            {
                return;
            }

            if (runState.Synergies.Catalog.Count > 0)
            {
                return;
            }

            if (gameDatabase == null)
            {
                gameDatabase = Resources.Load<GameDatabase>("GameDatabase");
            }

            if (gameDatabase?.Synergies != null)
            {
                runState.Synergies.SetCatalog(gameDatabase.Synergies);
            }
        }

        private static bool HasAnyPassenger(RunState runState)
        {
            if (runState == null)
            {
                return false;
            }

            if (runState.AllPassengers != null && runState.AllPassengers.Count > 0)
            {
                return true;
            }

            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                if (runState.GetPassengerAtSlot(i) != null)
                {
                    return true;
                }
            }

            return false;
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

            if (placeMergePair
                && debugPassengers[0] != null
                && mergePairSlotIndex >= 0
                && mergePairSlotIndex < RunState.GridSlotCount
                && runState.IsSlotEmpty(mergePairSlotIndex))
            {
                runState.TryPlacePassenger(
                    mergePairSlotIndex,
                    PassengerRuntime.Create(debugPassengers[0]));
            }
        }
    }
}
