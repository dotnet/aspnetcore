using System;
using GenerationTasks;

namespace GetDocument
{
    public class LogWrapper : ILogWrapper
    {
        public void LogError(string message, params object[] messageArgs)
        {
            Reporter.WriteError(string.Format(message, messageArgs));
        }

        public void LogError(Exception exception, bool showStackTrace)
        {
            var message = showStackTrace ? exception.ToString() : exception.Message;
            Reporter.WriteError(message);
        }

        public void LogInformational(string message, params object[] messageArgs)
        {
            Reporter.WriteInformation(string.Format(message, messageArgs));
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            Reporter.WriteWarning(string.Format(message, messageArgs));
        }
    }
}
