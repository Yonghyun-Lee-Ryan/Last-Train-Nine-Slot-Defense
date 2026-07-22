using LastTrain.Data;

namespace LastTrain.Relic
{
    public sealed class RelicRuntime
    {
        public RelicRuntime(RelicData data)
        {
            Data = data;
        }

        public RelicData Data { get; }
        public string Id => Data?.Id ?? string.Empty;
    }
}
