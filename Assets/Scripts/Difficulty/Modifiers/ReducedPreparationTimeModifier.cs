namespace LastTrain.Difficulty
{
    /// <summary>무정차: 역 사이 준비 시간 감소.</summary>
    public sealed class ReducedPreparationTimeModifier : IDifficultyModifier
    {
        private readonly DifficultyModifierData _data;

        public ReducedPreparationTimeModifier(DifficultyModifierData data)
        {
            _data = data;
        }

        public string ModifierId => _data != null ? _data.Id : "reduced_preparation";

        public void OnRunStarted(DifficultyModifierContext context)
        {
            Apply(context);
        }

        public void OnStationStarted(DifficultyModifierContext context, Data.StationData station)
        {
            Apply(context);
        }

        public void Tick(float deltaTime, DifficultyModifierContext context)
        {
        }

        private void Apply(DifficultyModifierContext context)
        {
            if (context?.RunState == null || _data == null)
            {
                return;
            }

            float seconds = _data.Magnitude > 0f ? _data.Magnitude : 2f;
            context.RunState.DifficultyModifiers.SetPreparationTimeSeconds(seconds);
        }
    }
}
