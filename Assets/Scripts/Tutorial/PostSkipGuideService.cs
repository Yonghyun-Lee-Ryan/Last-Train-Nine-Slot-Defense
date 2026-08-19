using LastTrain.Save;

namespace LastTrain.Tutorial
{
    /// <summary>튜토리얼 스킵 후 첫 전투에만 남기는 Ready/Summon 가이드.</summary>
    public static class PostSkipGuideService
    {
        public readonly struct Tip
        {
            public Tip(string uiTargetId, string message)
            {
                UiTargetId = uiTargetId ?? string.Empty;
                Message = message ?? string.Empty;
            }

            public string UiTargetId { get; }
            public string Message { get; }
        }

        public static readonly Tip[] Tips =
        {
            new Tip("SummonButton", "소환 버튼으로 승객을 뽑을 수 있습니다."),
            new Tip("ReadyButton", "준비 완료를 누르면 전투가 시작됩니다."),
        };

        public static bool ShouldShow(MetaSaveData meta)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            return meta.tutorialSkipped && !meta.tutorialPostSkipGuideDone;
        }

        public static void MarkDone(MetaSaveData meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            meta.tutorialPostSkipGuideDone = true;
        }

        public static void Reset(MetaSaveData meta)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            meta.tutorialPostSkipGuideDone = false;
        }
    }
}
