using System;
using Microsoft.Build.Utilities;

namespace GenerationTasks
{
    internal class LogWrapper : ILogWrapper
    {
        private readonly TaskLoggingHelper _log;

        public LogWrapper(TaskLoggingHelper log)
        {
            _log = log;
        }

        public void LogError(string message, params object[] messageArgs)
        {
            _log.LogError(message, messageArgs);
        }

        public void LogError(Exception exception, bool showStackTrace)
        {
            _log.LogErrorFromException(exception, showStackTrace);
        }

        public void LogInformational(string message, params object[] messageArgs)
        {
            _log.LogMessage(message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            _log.LogWarning(message, messageArgs);
        }
    }
}
