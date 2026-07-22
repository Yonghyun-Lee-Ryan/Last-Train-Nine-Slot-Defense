using UnityEngine;

namespace LastTrain.Data
{
    [CreateAssetMenu(fileName = "Event_", menuName = "Last Train/Event Data")]
    public sealed class EventData : ScriptableObject, IDataWithId
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [SerializeField] private EventChoiceData[] choices;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public EventChoiceData[] Choices => choices ?? System.Array.Empty<EventChoiceData>();
    }
}
