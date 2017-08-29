using System;
using System.Diagnostics;

namespace ProjectTestRunner.HandlerResults
{
    internal class ExecuteHandlerResult : IHandlerResult
    {
        private readonly Process _process;

        public ExecuteHandlerResult(TimeSpan duration, bool verificationSuccess, string failureMessage, Process process = null, string name = null)
        {
            Duration = duration;
            _process = process;
            Name = name;
            VerificationSuccess = verificationSuccess;
            FailureMessage = failureMessage;
        }

        public bool VerificationSuccess { get; }

        public string FailureMessage { get; }

        public string Name { get; }

        public TimeSpan Duration { get; }

        public void Kill()
        {
            _process?.Kill();
        }
    }
}