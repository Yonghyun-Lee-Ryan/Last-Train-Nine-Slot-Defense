namespace LastTrain.Integrations
{
    /// <summary>게임 시스템이 읽는 현재 Remote Config 스냅샷.</summary>
    public static class RemoteConfigRuntime
    {
        public static RemoteConfigSnapshot Current { get; private set; } = RemoteConfigSnapshot.Default;

        public static void Apply(RemoteConfigSnapshot snapshot)
        {
            Current = snapshot ?? RemoteConfigSnapshot.Default;
        }
    }
}
