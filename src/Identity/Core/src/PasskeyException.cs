// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents an error that occurred during passkey attestation or assertion.
/// </summary>
public sealed class PasskeyException : Exception
{
    /// <summary>
    /// Constructs a new <see cref="PasskeyException"/> instance.
    /// </summary>
    public PasskeyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="PasskeyException"/> instance.
    /// </summary>
    public PasskeyException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
