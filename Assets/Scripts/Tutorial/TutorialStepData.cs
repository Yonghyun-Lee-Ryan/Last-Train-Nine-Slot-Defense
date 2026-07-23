using UnityEngine;

namespace LastTrain.Tutorial
{
    /// <summary>튜토리얼 단계 정적 데이터. 좌표 하드코딩 대신 UI Target Id를 쓴다.</summary>
    [CreateAssetMenu(fileName = "TutorialStep_", menuName = "Last Train/Tutorial Step Data")]
    public sealed class TutorialStepData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private TutorialStepKind stepKind;
        [SerializeField] private string title;
        [TextArea(2, 5)]
        [SerializeField] private string body;
        [SerializeField] private TutorialWaitEvent waitEvent = TutorialWaitEvent.Acknowledge;
        [Tooltip("BattleHud/Summon/Grid 등 하이라이트할 UI 키")]
        [SerializeField] private string uiTargetId;
        [SerializeField] private TutorialInputMask allowedInputs = TutorialInputMask.Acknowledge;
        [SerializeField] private bool showSkipButton = true;

        public string Id => id;
        public TutorialStepKind StepKind => stepKind;
        public string Title => title;
        public string Body => body;
        public TutorialWaitEvent WaitEvent => waitEvent;
        public string UiTargetId => uiTargetId ?? string.Empty;
        public TutorialInputMask AllowedInputs => allowedInputs;
        public bool ShowSkipButton => showSkipButton;

#if UNITY_EDITOR
        public void EditorSet(
            string newId,
            TutorialStepKind kind,
            string newTitle,
            string newBody,
            TutorialWaitEvent wait,
            string targetId,
            TutorialInputMask inputs,
            bool skip)
        {
            id = newId;
            stepKind = kind;
            title = newTitle;
            body = newBody;
            waitEvent = wait;
            uiTargetId = targetId;
            allowedInputs = inputs;
            showSkipButton = skip;
        }
#endif
    }
}
