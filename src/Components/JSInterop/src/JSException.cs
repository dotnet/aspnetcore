// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Represents errors that occur during an interop call from .NET to JavaScript.
/// </summary>
public class JSException : Exception
{
    /// <summary>
    /// Constructs an instance of <see cref="JSException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public JSException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="JSException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JSException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
