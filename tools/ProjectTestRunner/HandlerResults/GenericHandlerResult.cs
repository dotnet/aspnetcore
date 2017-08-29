using System;

namespace ProjectTestRunner.HandlerResults
{
    internal class GenericHandlerResult : IHandlerResult
    {
        public GenericHandlerResult(TimeSpan duration, bool verificationSuccess, string failureMessage, string name = null)
        {
            Duration = duration;
            Name = name;
            VerificationSuccess = verificationSuccess;
            FailureMessage = failureMessage;
        }

        public string Name { get; }

        public bool VerificationSuccess { get; }

        public string FailureMessage { get; }

        public TimeSpan Duration { get; }
    }
}