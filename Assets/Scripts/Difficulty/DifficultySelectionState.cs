using LastTrain.Difficulty;

namespace LastTrain.Difficulty
{
    /// <summary>메인 메뉴에서 선택한 난이도. 이어하기 중에는 변경할 수 없다.</summary>
    public static class DifficultySelectionState
    {
        public static string SelectedDifficultyId { get; private set; } = DifficultyIds.Normal;
        public static bool IsLockedByContinue { get; private set; }

        public static void Select(string difficultyId)
        {
            if (IsLockedByContinue)
            {
                return;
            }

            SelectedDifficultyId = DifficultyService.ResolveSavedDifficultyId(difficultyId);
        }

        public static void LockToContinueSave(string difficultyId)
        {
            SelectedDifficultyId = DifficultyService.ResolveSavedDifficultyId(difficultyId);
            IsLockedByContinue = true;
        }

        public static void UnlockSelection()
        {
            IsLockedByContinue = false;
        }

        public static void ResetToDefault()
        {
            if (!IsLockedByContinue)
            {
                SelectedDifficultyId = DifficultyIds.Normal;
            }
        }
    }
}
