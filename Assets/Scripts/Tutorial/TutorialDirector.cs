using System;
using LastTrain.Ability;
using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Tutorial
{
    /// <summary>
    /// 튜토리얼 오버레이·입력 제한·이벤트 구독.
    /// UI Target Id로 하이라이트하며 좌표를 하드코딩하지 않는다.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [SerializeField] private GameDatabase gameDatabase;

        private TutorialStateMachine _machine;
        private GameObject _overlayRoot;
        private Text _titleLabel;
        private Text _bodyLabel;
        private Button _ackButton;
        private Button _skipButton;
        private StationManager _stationManager;
        private AbilityManager _abilityManager;
        private GridManager _gridManager;
        private bool _combatObserved;

        public TutorialStateMachine Machine => _machine;
        public bool IsTutorialActive => _machine != null && _machine.IsActive;

        public bool Allows(TutorialInputMask mask)
        {
            return _machine == null || _machine.Allows(mask);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            Unbind();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Begin(
            StationManager stationManager,
            AbilityManager abilityManager,
            GridManager gridManager)
        {
            gameDatabase ??= GameDatabaseLocator.Load();
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!TutorialProgressService.ShouldOfferTutorial(meta))
            {
                return;
            }

            IReadOnlyListWrapper steps = new IReadOnlyListWrapper(gameDatabase?.TutorialSteps);
            if (steps.Count == 0)
            {
                return;
            }

            Unbind();
            _stationManager = stationManager;
            _abilityManager = abilityManager;
            _gridManager = gridManager;
            _machine = new TutorialStateMachine(steps);
            _machine.StepStarted += HandleStepStarted;
            _machine.StepCompleted += _ => RefreshOverlay();
            _machine.Completed += HandleFinished;
            _machine.Skipped += HandleFinished;

            Bind();
            EnsureOverlay();
            _machine.StartOrResume(meta);
            RefreshOverlay();
            ApplyInputGate();
        }

        public void RestartFromSettings()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            TutorialProgressService.ResetProgress(meta);
            MetaSaveSystem.Save(meta);
            if (_machine == null)
            {
                return;
            }

            _machine.Restart(meta);
            RefreshOverlay();
            ApplyInputGate();
        }

        private void Bind()
        {
            if (_stationManager != null)
            {
                _stationManager.StationCompleted += HandleStationCompleted;
                _stationManager.StationStarted += HandleStationStarted;
            }

            if (_abilityManager != null)
            {
                _abilityManager.AbilitySelected += HandleAbilitySelected;
            }

            if (_gridManager != null)
            {
                _gridManager.PassengerDropped += HandlePassengerDropped;
                _gridManager.MergeCompleted += HandleMergeCompleted;
            }

            MergeService.Merged += HandleMerged;
            CombatVisualEvents.EnemyDamaged += HandleEnemyDamaged;
        }

        private void Unbind()
        {
            if (_stationManager != null)
            {
                _stationManager.StationCompleted -= HandleStationCompleted;
                _stationManager.StationStarted -= HandleStationStarted;
            }

            if (_abilityManager != null)
            {
                _abilityManager.AbilitySelected -= HandleAbilitySelected;
            }

            if (_gridManager != null)
            {
                _gridManager.PassengerDropped -= HandlePassengerDropped;
                _gridManager.MergeCompleted -= HandleMergeCompleted;
            }

            MergeService.Merged -= HandleMerged;
            CombatVisualEvents.EnemyDamaged -= HandleEnemyDamaged;

            if (_machine != null)
            {
                _machine.StepStarted -= HandleStepStarted;
                _machine.Completed -= HandleFinished;
                _machine.Skipped -= HandleFinished;
            }
        }

        private void HandleStepStarted(TutorialStepData step)
        {
            RefreshOverlay();
            ApplyInputGate();
            HighlightTarget(step?.UiTargetId);
            if (step != null && step.StepKind == TutorialStepKind.MergePassengers)
            {
                Ux.MergeHighlightService.Refresh(_gridManager, AppRoot.Instance?.GameSession?.RunState);
            }
        }

        private void HandleFinished()
        {
            HideOverlay();
            ApplyInputGate();
            Ux.MergeHighlightService.Clear(_gridManager);
        }

        private void HandleStationCompleted(StationData _)
        {
            _machine?.Notify(TutorialWaitEvent.StationCompleted);
        }

        private void HandleStationStarted(StationData station)
        {
            if (station != null && station.StationType == StationType.Boss)
            {
                _machine?.Notify(TutorialWaitEvent.BossBriefingShown);
            }
        }

        private void HandleAbilitySelected(AbilitySelectResult result, AbilityData _)
        {
            if (result == AbilitySelectResult.Success)
            {
                _machine?.Notify(TutorialWaitEvent.AbilitySelected);
            }
        }

        private void HandlePassengerDropped(int from, int to, GridDropResult result)
        {
            if (result == GridDropResult.Moved || result == GridDropResult.Swapped)
            {
                // 배치는 소환 선택에서도 발생. 드래그 이동도 배치 학습에 포함.
            }

            if (result == GridDropResult.Reverted)
            {
                Ux.UxGuidanceService.Show("잘못된 위치로 되돌렸습니다.");
            }
        }

        private void HandleMergeCompleted(MergeResult _)
        {
            _machine?.Notify(TutorialWaitEvent.PassengersMerged);
            Ux.MergeHighlightService.Refresh(_gridManager, AppRoot.Instance?.GameSession?.RunState);
        }

        private void HandleMerged(int _, string __)
        {
            _machine?.Notify(TutorialWaitEvent.PassengersMerged);
        }

        private void HandleEnemyDamaged(EnemyRuntime _, float damage, bool __)
        {
            if (damage <= 0f || _combatObserved)
            {
                return;
            }

            _combatObserved = true;
            _machine?.Notify(TutorialWaitEvent.EnemyDamaged);
        }

        /// <summary>소환 UI에서 호출.</summary>
        public void NotifySummonOpened()
        {
            _machine?.Notify(TutorialWaitEvent.SummonOpened);
        }

        /// <summary>소환 배치 성공 시 호출.</summary>
        public void NotifyPassengerPlaced()
        {
            _machine?.Notify(TutorialWaitEvent.PassengerPlaced);
        }

        private void EnsureOverlay()
        {
            if (_overlayRoot != null)
            {
                return;
            }

            _overlayRoot = UI.MenuOverlayUi.CreateRoot("TutorialOverlay", sortingOrder: 4500);
            GameObject box = UI.MenuOverlayUi.CreatePanel(
                _overlayRoot.transform,
                "Box",
                new Color(0.08f, 0.12f, 0.18f, 0.94f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.06f, 0.62f);
            boxRect.anchorMax = new Vector2(0.94f, 0.96f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;

            // 하단만 가리고 전투/그리드는 보이도록 Dim은 반투명 상단 바만 사용
            box.AddComponent<CanvasGroup>().blocksRaycasts = true;

            _titleLabel = UI.MenuOverlayUi.CreateText(box.transform, "Title", string.Empty, 32, TextAnchor.UpperLeft);
            RectTransform titleRect = _titleLabel.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-40f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -16f);

            _bodyLabel = UI.MenuOverlayUi.CreateText(box.transform, "Body", string.Empty, 24, TextAnchor.UpperLeft);
            RectTransform bodyRect = _bodyLabel.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(20f, 90f);
            bodyRect.offsetMax = new Vector2(-20f, -70f);

            _ackButton = UI.MenuOverlayUi.CreateButton(
                box.transform,
                "AckButton",
                "확인",
                new Vector2(-140f, 28f),
                new Vector2(240f, 72f),
                OnAckClicked);
            RectTransform ackRect = _ackButton.GetComponent<RectTransform>();
            ackRect.anchorMin = new Vector2(0.5f, 0f);
            ackRect.anchorMax = new Vector2(0.5f, 0f);
            ackRect.pivot = new Vector2(0.5f, 0f);

            _skipButton = UI.MenuOverlayUi.CreateButton(
                box.transform,
                "SkipButton",
                "건너뛰기",
                new Vector2(140f, 28f),
                new Vector2(240f, 72f),
                OnSkipClicked);
            RectTransform skipRect = _skipButton.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0f);
            skipRect.anchorMax = new Vector2(0.5f, 0f);
            skipRect.pivot = new Vector2(0.5f, 0f);
        }

        private void RefreshOverlay()
        {
            if (_overlayRoot == null)
            {
                return;
            }

            bool show = IsTutorialActive && _machine.CurrentStep != null;
            _overlayRoot.SetActive(show);
            if (!show)
            {
                return;
            }

            TutorialStepData step = _machine.CurrentStep;
            if (_titleLabel != null)
            {
                _titleLabel.text = step.Title;
            }

            if (_bodyLabel != null)
            {
                _bodyLabel.text = step.Body;
            }

            bool ack = step.WaitEvent == TutorialWaitEvent.Acknowledge;
            if (_ackButton != null)
            {
                _ackButton.gameObject.SetActive(ack);
                _ackButton.interactable = ack;
            }

            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(step.ShowSkipButton);
            }
        }

        private void HideOverlay()
        {
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }
        }

        private void OnAckClicked()
        {
            GameAudio.PlaySfx(SfxId.UiConfirm);
            _machine?.Acknowledge();
            RefreshOverlay();
            ApplyInputGate();
        }

        private void OnSkipClicked()
        {
            GameAudio.PlaySfx(SfxId.UiCancel);
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            _machine?.SkipAll(meta);
            MetaSaveSystem.Save(meta);
            RefreshOverlay();
            ApplyInputGate();
        }

        private void ApplyInputGate()
        {
            // Summon/Ready 등은 각 컨트롤러가 Allows()를 조회한다.
            bool drag = Allows(TutorialInputMask.GridDrag);
            if (_gridManager != null)
            {
                _gridManager.SetDragEnabled(drag || !IsTutorialActive);
            }
        }

        private static void HighlightTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return;
            }

            // 이름으로 UI를 찾아 한 프레임 강조 (좌표 하드코딩 없음)
            GameObject target = GameObject.Find(targetId);
            if (target == null)
            {
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 0.92f, 0.55f, 1f);
            }
        }

        /// <summary>GameDatabase.TutorialSteps용 얇은 래퍼.</summary>
        private sealed class IReadOnlyListWrapper : System.Collections.Generic.IReadOnlyList<TutorialStepData>
        {
            private readonly TutorialStepData[] _items;

            public IReadOnlyListWrapper(System.Collections.Generic.IReadOnlyList<TutorialStepData> source)
            {
                if (source == null)
                {
                    _items = Array.Empty<TutorialStepData>();
                    return;
                }

                _items = new TutorialStepData[source.Count];
                for (int i = 0; i < source.Count; i++)
                {
                    _items[i] = source[i];
                }
            }

            public int Count => _items.Length;
            public TutorialStepData this[int index] => _items[index];
            public System.Collections.Generic.IEnumerator<TutorialStepData> GetEnumerator() =>
                ((System.Collections.Generic.IEnumerable<TutorialStepData>)_items).GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        }
    }
}
