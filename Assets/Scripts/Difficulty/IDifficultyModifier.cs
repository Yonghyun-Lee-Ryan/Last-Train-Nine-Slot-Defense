namespace LastTrain.Difficulty
{
    public interface IDifficultyModifier
    {
        string ModifierId { get; }

        void OnRunStarted(DifficultyModifierContext context);

        void OnStationStarted(DifficultyModifierContext context, Data.StationData station);

        void Tick(float deltaTime, DifficultyModifierContext context);
    }
}
