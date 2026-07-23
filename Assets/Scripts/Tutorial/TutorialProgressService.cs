using LastTrain.Save;

namespace LastTrain.Tutorial
{
    /// <summary>튜토리얼 해금·재시작·건너뛰기 판정.</summary>
    public static class TutorialProgressService
    {
        public static bool ShouldOfferTutorial(MetaSaveData meta)
        {
            if (meta == null)
            {
                return true;
            }

            meta.EnsureDefaults();
            return !meta.tutorialCompleted && !meta.tutorialSkipped;
        }

        public static bool CanRestart(MetaSaveData meta)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            return meta.tutorialCompleted || meta.tutorialSkipped || meta.tutorialStepIndex > 0;
        }

        public static void MarkSkipped(MetaSaveData meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            meta.tutorialSkipped = true;
            meta.tutorialCompleted = true;
        }

        public static void ResetProgress(MetaSaveData meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            meta.tutorialCompleted = false;
            meta.tutorialSkipped = false;
            meta.tutorialStepIndex = 0;
        }
    }
}
