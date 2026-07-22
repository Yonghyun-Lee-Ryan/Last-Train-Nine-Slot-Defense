namespace LastTrain.Difficulty
{
    /// <summary>검표 강화: 승객 판매 가격 감소.</summary>
    public sealed class ReducedSellPriceModifier : IDifficultyModifier
    {
        private readonly DifficultyModifierData _data;

        public ReducedSellPriceModifier(DifficultyModifierData data)
        {
            _data = data;
        }

        public string ModifierId => _data != null ? _data.Id : "reduced_sell_price";

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

            float multiplier = _data.Magnitude > 0f ? _data.Magnitude : 0.8f;
            context.RunState.DifficultyModifiers.SetSellPriceMultiplier(multiplier);
        }
    }
}
