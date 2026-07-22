using System.Collections.Generic;
using LastTrain.Audio;
using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 난이도 선택 UI.</summary>
    public sealed class DifficultySelectionController : MonoBehaviour
    {
            private const float ButtonHeight = 58f;

        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Text statusLabel;

        private readonly List<Button> _buttons = new();
        private GameDatabase _database;
        private MetaSaveData _meta;

        public void Refresh()
        {
            _database ??= GameDatabaseLocator.Load();
            _meta = MetaSaveSystem.LoadOrCreate();
            EnsureUi();
            RebuildButtons();

            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform safeArea = canvas != null ? canvas.transform.Find("SafeArea") : null;
            if (safeArea != null)
            {
                MainMenuUiLayout.Apply(safeArea);
            }

            RefreshStatusLabel();
            HighlightSelected();
        }

        private void EnsureUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea") ?? canvas.transform;
            if (buttonContainer == null)
            {
                var containerGo = new GameObject("DifficultySelection", typeof(RectTransform));
                containerGo.transform.SetParent(safeArea, false);
                buttonContainer = containerGo.transform;
            }

            if (statusLabel == null)
            {
                statusLabel = MenuOverlayUi.CreateText(
                    safeArea,
                    "DifficultyStatusLabel",
                    string.Empty,
                    24,
                    TextAnchor.MiddleLeft);
            }
        }

        private void RebuildButtons()
        {
            ClearButtons();
            if (_database == null || buttonContainer == null)
            {
                return;
            }

            IReadOnlyList<Difficulty.DifficultyData> difficulties = _database.Difficulties;
            if (difficulties == null || difficulties.Count == 0)
            {
                CreateFallbackButton(Difficulty.DifficultyIds.Normal, "일반 막차", unlocked: true);
                return;
            }

            var sorted = new List<Difficulty.DifficultyData>(difficulties);
            sorted.Sort((a, b) => (a?.SortOrder ?? 0).CompareTo(b?.SortOrder ?? 0));

            for (int i = 0; i < sorted.Count; i++)
            {
                Difficulty.DifficultyData data = sorted[i];
                if (data == null)
                {
                    continue;
                }

                bool unlocked = Difficulty.DifficultyProgressService.IsUnlocked(data, _meta);
                CreateDifficultyButton(data, unlocked);
            }
        }

        private void CreateFallbackButton(string id, string label, bool unlocked)
        {
            Button button = CreateDifficultyButtonInternal(id, label, unlocked);
            _buttons.Add(button);
        }

        private void CreateDifficultyButton(Difficulty.DifficultyData data, bool unlocked)
        {
            string prefix = unlocked ? string.Empty : "[잠김] ";
            Button button = CreateDifficultyButtonInternal(
                data.Id,
                prefix + data.DisplayName,
                unlocked);
            _buttons.Add(button);
        }

        private Button CreateDifficultyButtonInternal(string id, string label, bool unlocked)
        {
            Button button = MenuOverlayUi.CreateLayoutButton(
                buttonContainer,
                $"Difficulty_{id}",
                label,
                ButtonHeight,
                () => SelectDifficulty(id, unlocked),
                fontSize: 30);
            UiButtonStyler.ApplyStandardTheme(button);
            Text labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.color = Color.white;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.fontSize = 30;
            }

            button.interactable = unlocked && !Difficulty.DifficultySelectionState.IsLockedByContinue;
            return button;
        }

        private void SelectDifficulty(string difficultyId, bool unlocked)
        {
            if (!unlocked || Difficulty.DifficultySelectionState.IsLockedByContinue)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                RefreshStatusLabel();
                return;
            }

            Difficulty.DifficultySelectionState.Select(difficultyId);
            GameAudio.PlaySfx(SfxId.UiConfirm);
            RefreshStatusLabel();
            HighlightSelected();
        }

        private void HighlightSelected()
        {
            string selectedId = Difficulty.DifficultySelectionState.SelectedDifficultyId;
            for (int i = 0; i < _buttons.Count; i++)
            {
                Button button = _buttons[i];
                if (button == null)
                {
                    continue;
                }

                bool isSelected = button.name == $"Difficulty_{selectedId}";
                Image image = button.GetComponent<Image>();
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                    label.color = Color.white;
                }

                if (image != null)
                {
                    // 이어하기 잠금으로 interactable=false여도 선택 난이도는 밝게 유지
                    image.color = isSelected
                        ? Color.white
                        : new Color(0.78f, 0.82f, 0.88f, 1f);
                }
            }
        }

        private void RefreshStatusLabel()
        {
            if (statusLabel == null)
            {
                return;
            }

            if (Difficulty.DifficultySelectionState.IsLockedByContinue)
            {
                statusLabel.text =
                    $"이어하기 중에는 난이도를 변경할 수 없습니다.\n({Difficulty.DifficultySelectionState.SelectedDifficultyId})";
                return;
            }

            Difficulty.DifficultyData selected = Difficulty.DifficultyService.ResolveData(
                Difficulty.DifficultySelectionState.SelectedDifficultyId,
                _database);
            if (selected == null)
            {
                statusLabel.text = $"선택: {Difficulty.DifficultySelectionState.SelectedDifficultyId}";
                return;
            }

            Difficulty.DifficultyUnlockProgress progress =
                Difficulty.DifficultyProgressService.GetUnlockProgress(selected, _meta);
            MetaDifficultyRecord record = Difficulty.DifficultyProgressService.GetOrCreateRecord(
                _meta,
                selected.Id);

            string progressText = string.IsNullOrWhiteSpace(progress.ProgressText)
                ? "해금됨"
                : progress.ProgressText.Replace("\n", " · ");

            statusLabel.text =
                $"선택: {selected.DisplayName}  |  "
                + $"최고 역 {record.highestStationReached}  |  "
                + $"클리어 {record.clearCount}회  |  "
                + $"최고 점수 {record.bestScore}\n"
                + progressText;
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                {
                    Destroy(_buttons[i].gameObject);
                }
            }

            _buttons.Clear();
        }
    }
}
