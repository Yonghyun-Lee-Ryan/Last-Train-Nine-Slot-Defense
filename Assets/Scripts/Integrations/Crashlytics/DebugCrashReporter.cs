using UnityEngine;

namespace LastTrain.Integrations
{
    public sealed class DebugCrashReporter : ICrashReporter
    {
        public void Log(string message)
        {
            Debug.Log($"[CrashReporter] {message}");
        }

        public void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
