// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// A generic authentication failure.
/// </summary>
public class AuthenticationFailureException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="AuthenticationFailureException"/>
    /// with the specified exception <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AuthenticationFailureException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="AuthenticationFailureException"/>
    /// with the specified exception <paramref name="message"/> and
    /// a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public AuthenticationFailureException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
