// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Represents errors that occur when a JavaScript interop call times out.
/// </summary>
public class JSTimeoutException : JSException
{
    /// <summary>
    /// Constructs an instance of <see cref="JSTimeoutException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public JSTimeoutException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="JSTimeoutException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JSTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}