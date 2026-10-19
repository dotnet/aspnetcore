// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Represents errors that occur during an interop call from .NET to JavaScript when the JavaScript runtime becomes disconnected.
/// </summary>
public sealed class JSDisconnectedException : Exception
{
    /// <summary>
    /// Constructs an instance of <see cref="JSDisconnectedException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public JSDisconnectedException(string message) : base(message)
    {
    }
}
