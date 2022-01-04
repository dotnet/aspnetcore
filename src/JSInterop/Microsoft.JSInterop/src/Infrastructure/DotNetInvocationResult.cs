// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

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
    internal DotNetInvocationResult(Exception exception, string? errorKind)
    {
        ResultJson = default;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        ErrorKind = errorKind;
        Success = false;
    }

    /// <summary>
    /// Constructor for a successful invocation.
    /// </summary>
    /// <param name="resultJson">The JSON representation of the result.</param>
    internal DotNetInvocationResult(string? resultJson)
    {
        ResultJson = resultJson;
        Exception = default;
        ErrorKind = default;
        Success = true;
    }

    /// <summary>
    /// Gets the <see cref="System.Exception"/> that caused the failure.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the error kind.
    /// </summary>
    public string? ErrorKind { get; }

    /// <summary>
    /// Gets a JSON representation of the result of a successful invocation.
    /// </summary>
    public string? ResultJson { get; }

    /// <summary>
    /// <see langword="true"/> if the invocation succeeded, otherwise <see langword="false"/>.
    /// </summary>
    public bool Success { get; }
}
