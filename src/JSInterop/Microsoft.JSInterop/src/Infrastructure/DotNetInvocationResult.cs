using System;

namespace Microsoft.JSInterop.Infrastructure
{
    /// <summary>
    /// Result of a .NET invocation that is returned to JavaScript.
    /// </summary>
    public readonly struct DotNetInvocationResult
    {
        /// <summary>
        /// Constructor for a failed invocation.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> that caused the failure.</param>
        /// <param name="errorKind">The error kind.</param>
        public DotNetInvocationResult(Exception exception, string errorKind)
        {
            Result = default;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            ErrorKind = errorKind;
            Success = false;
        }

        /// <summary>
        /// Constructor for a successful invocation.
        /// </summary>
        /// <param name="result">The result.</param>
        public DotNetInvocationResult(object result)
        {
            Result = result;
            Exception = default;
            ErrorKind = default;
            Success = true;
        }

        /// <summary>
        /// Gets the <see cref="System.Exception"/> that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the error kind.
        /// </summary>
        public string ErrorKind { get; }

        /// <summary>
        /// Gets the result of a successful invocation.
        /// </summary>
        public object Result { get; }

        /// <summary>
        /// <see langword="true"/> if the invocation succeeded, otherwise <see langword="false"/>.
        /// </summary>
        public bool Success { get; }
    }
}
