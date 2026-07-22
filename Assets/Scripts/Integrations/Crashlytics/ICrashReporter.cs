namespace LastTrain.Integrations
{
    public interface ICrashReporter
    {
        void Log(string message);
        void LogException(System.Exception exception);
    }
}
