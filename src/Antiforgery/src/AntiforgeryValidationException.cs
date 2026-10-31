// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// The <see cref="Exception"/> that is thrown when the antiforgery token validation fails.
/// </summary>
public class AntiforgeryValidationException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="AntiforgeryValidationException"/> with the specified
    /// exception message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AntiforgeryValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="AntiforgeryValidationException"/> with the specified
    /// exception message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner <see cref="Exception"/>.</param>
    public AntiforgeryValidationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
