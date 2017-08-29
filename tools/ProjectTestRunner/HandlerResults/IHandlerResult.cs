using System;

namespace ProjectTestRunner.HandlerResults
{
    public interface IHandlerResult
    {
        string Name { get; }

        bool VerificationSuccess { get; }

        string FailureMessage { get; }

        TimeSpan Duration { get; }
    }
}
