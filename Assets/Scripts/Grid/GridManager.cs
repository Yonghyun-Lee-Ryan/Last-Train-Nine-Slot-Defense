using System;
using System.Collections.Generic;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Ux;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Grid
{
    /// <summary>
    /// 3×3 승객 Grid를 관리한다.
    /// RunState가 승객 상태의 단일 소스이며, GridManager는 배치·드래그·View 동기화를 담당한다.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public event Action<int, int, GridDropResult> PassengerDropped;
        /// <summary>그리드 승객 구성이 View와 맞춰진 직후(소환·판매·상점·드롭 등).</summary>
        public event Action GridCompositionChanged;
        public event Action<MergeResult> MergeStarted;
        public event Action<MergeResult> MergeCompleted;
        public event Action<int> PassengerSelected;
        public event Action PassengerDragStarted;

        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GridSlot[] slots = new GridSlot[RunState.GridSlotCount];
        [SerializeField] private PassengerView passengerViewPrefab;

        private const string DefaultViewPrefabPath = "Assets/Prefabs/UI/PassengerView.prefab";

        [Tooltip("전투 중 등 드래그를 막을 때 false")]
        [SerializeField] private bool allowDrag = true;

        private readonly Dictionary<string, PassengerView> _viewsByInstanceId = new();

        private RunState _runState;
        private PassengerView _draggingView;
        private int _dragOriginSlotIndex = -1;
        private int _selectedSlotIndex = -1;

        public bool CanDrag => allowDrag
                               && _runState != null
                               && _runState.Battle != null
                               && _runState.Battle.IsRunActive;

        public Canvas RootCanvas => rootCanvas;

        public IReadOnlyList<GridSlot> Slots => slots;

        public int SelectedSlotIndex => _selectedSlotIndex;

        public GridSlot GetSlot(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return null;
            }

            return slots[slotIndex];
        }

        public PassengerView FindViewByInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            return _viewsByInstanceId.TryGetValue(instanceId, out PassengerView view) ? view : null;
        }

        /// <summary>RunState를 연결하고 View를 동기화한다.</summary>
        public void Initialize(RunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            EnsureViewPrefab();
            ValidateSlots();
            RefreshViews();
        }

        /// <summary>Inspector 외부(테스트·런타임 생성)에서 참조를 주입한다.</summary>
        public void SetReferences(Canvas canvas, GridSlot[] gridSlots, PassengerView viewPrefab)
        {
            rootCanvas = canvas;
            slots = gridSlots;
            passengerViewPrefab = viewPrefab;
            ValidateSlots();
        }

        public void SetDragEnabled(bool enabled)
        {
            allowDrag = enabled;
        }

        /// <summary>RunState 기준으로 모든 PassengerView를 재배치한다.</summary>
        public void RefreshViews()
        {
            if (_runState == null)
            {
                return;
            }

            var activeIds = new HashSet<string>();

            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = _runState.GetPassengerAtSlot(i);
                if (passenger == null)
                {
                    continue;
                }

                activeIds.Add(passenger.InstanceId);
                PassengerView view = GetOrCreateView(passenger);
                if (view == null)
                {
                    continue;
                }

                if (!TryGetSlot(i, out GridSlot slot))
                {
                    Debug.LogError($"[GridManager] slots[{i}]가 비어 있어 View를 배치할 수 없습니다.", this);
                    continue;
                }

                view.SnapToSlot(slot);
            }

            RemoveInactiveViews(activeIds);
            ApplyLockedSlotVisuals();
            GridCompositionChanged?.Invoke();
        }

        internal void HandleDragStarted(PassengerView view)
        {
            _draggingView = view;
            _dragOriginSlotIndex = view.SlotIndex;
            SetAllHighlights(true);
            PassengerDragStarted?.Invoke();
        }

        internal void HandlePassengerClicked(PassengerView view)
        {
            if (view == null)
            {
                return;
            }

            _selectedSlotIndex = view.SlotIndex;
            PassengerSelected?.Invoke(_selectedSlotIndex);
        }

        public void ClearSelection()
        {
            _selectedSlotIndex = -1;
            PassengerSelected?.Invoke(_selectedSlotIndex);
        }

        internal void HandleDragEnded(PassengerView view, Vector2 screenPosition, Camera eventCamera)
        {
            SetAllHighlights(false);

            if (_runState == null || view != _draggingView)
            {
                view.RevertToOriginalTransform();
                ClearDragState();
                ApplyLockedSlotVisuals();
                MergeHighlightService.Refresh(this, _runState);
                return;
            }

            int targetSlot = FindSlotIndexAtScreenPoint(screenPosition, eventCamera);
            ApplyDrop(_dragOriginSlotIndex, targetSlot);
            ClearDragState();
            ApplyLockedSlotVisuals();
            MergeHighlightService.Refresh(this, _runState);
        }

        /// <summary>드롭 결과를 적용한다. 테스트·입력 공용.</summary>
        public GridDropResult ApplyDrop(int fromSlot, int toSlot)
        {
            if (_runState == null)
            {
                return GridDropResult.Reverted;
            }

            // 유효하지 않은 슬롯은 RunState 조회 전에 걸러, 드래그 View가 Canvas에 고아로 남지 않게 한다.
            if (fromSlot < 0 || fromSlot >= RunState.GridSlotCount
                || toSlot < 0 || toSlot >= RunState.GridSlotCount)
            {
                RefreshViews();
                return GridDropResult.Reverted;
            }

            PassengerRuntime source = _runState.GetPassengerAtSlot(fromSlot);
            PassengerRuntime target = _runState.GetPassengerAtSlot(toSlot);
            MergeResult pendingMerge = default;
            bool willMerge = target != null && MergeService.CanMerge(source, target);

            if (willMerge)
            {
                pendingMerge = new MergeResult(
                    fromSlot,
                    toSlot,
                    source.InstanceId,
                    target.InstanceId,
                    target.Data.Id,
                    target.StarLevel + 1);
                MergeStarted?.Invoke(pendingMerge);
            }

            GridDropResult result = GridInteractionService.TryDrop(_runState, fromSlot, toSlot);
            RefreshViews();

            if (result == GridDropResult.Merged)
            {
                PassengerRuntime merged = _runState.GetPassengerAtSlot(toSlot);
                var completed = new MergeResult(
                    fromSlot,
                    toSlot,
                    pendingMerge.ConsumedInstanceId,
                    merged != null ? merged.InstanceId : pendingMerge.ResultInstanceId,
                    pendingMerge.PassengerId,
                    merged != null ? merged.StarLevel : pendingMerge.ResultingStarLevel);
                MergeCompleted?.Invoke(completed);
            }

            if (result != GridDropResult.Reverted)
            {
                PassengerDropped?.Invoke(fromSlot, toSlot, result);
            }

            return result;
        }

        public bool TryGetSlot(int index, out GridSlot slot)
        {
            slot = null;
            if (slots == null || index < 0 || index >= slots.Length)
            {
                return false;
            }

            slot = slots[index];
            return slot != null;
        }

        public int FindSlotIndexAtScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                GridSlot slot = slots[i];
                if (slot != null && slot.ContainsScreenPoint(screenPosition, eventCamera))
                {
                    return slot.SlotIndex;
                }
            }

            return -1;
        }

        private void EnsureViewPrefab()
        {
            if (passengerViewPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            passengerViewPrefab = AssetDatabase.LoadAssetAtPath<PassengerView>(DefaultViewPrefabPath);
            if (passengerViewPrefab != null)
            {
                return;
            }
#endif

            GameObject loaded = Resources.Load<GameObject>("Combat/PassengerView");
            if (loaded != null)
            {
                passengerViewPrefab = loaded.GetComponent<PassengerView>();
            }

            if (passengerViewPrefab == null)
            {
                Debug.LogError(
                    $"[GridManager] passengerViewPrefab이 설정되지 않았습니다. Inspector에 연결하거나 Resources/Combat/PassengerView를 확인하세요.",
                    this);
            }
        }

        private PassengerView GetOrCreateView(PassengerRuntime passenger)
        {
            if (_viewsByInstanceId.TryGetValue(passenger.InstanceId, out PassengerView existing))
            {
                existing.Bind(this, passenger);
                return existing;
            }

            if (passengerViewPrefab == null)
            {
                Debug.LogError("[GridManager] passengerViewPrefab이 설정되지 않았습니다.", this);
                return null;
            }

            PassengerView created = Instantiate(passengerViewPrefab, transform);
            created.name = $"PassengerView_{passenger.Data.Id}";
            created.Bind(this, passenger);
            _viewsByInstanceId[passenger.InstanceId] = created;
            return created;
        }

        private void RemoveInactiveViews(HashSet<string> activeIds)
        {
            var removeKeys = new List<string>();
            foreach (KeyValuePair<string, PassengerView> pair in _viewsByInstanceId)
            {
                if (!activeIds.Contains(pair.Key))
                {
                    removeKeys.Add(pair.Key);
                    if (pair.Value != null)
                    {
                        Destroy(pair.Value.gameObject);
                    }
                }
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                _viewsByInstanceId.Remove(removeKeys[i]);
            }
        }

        private void SetAllHighlights(bool active)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetHighlightActive(active);
                }
            }
        }

        private void ApplyLockedSlotVisuals()
        {
            if (slots == null || _runState == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetLocked(_runState.IsSlotLocked(i));
                }
            }
        }

        private void ClearDragState()
        {
            _draggingView = null;
            _dragOriginSlotIndex = -1;
        }

        private void ValidateSlots()
        {
            if (slots == null || slots.Length != RunState.GridSlotCount)
            {
                Debug.LogError("[GridManager] slots 배열은 9개여야 합니다.", this);
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    Debug.LogError($"[GridManager] slots[{i}]가 비어 있습니다.", this);
                    continue;
                }

                slots[i].Configure(i);
            }
        }

        private void OnValidate()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }
    }
}
