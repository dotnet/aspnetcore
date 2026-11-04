// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Exception thrown by <see cref="IInputFormatter"/> when the input is not in an expected format.
/// </summary>
public class InputFormatterException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputFormatterException"/>.
    /// </summary>
    public InputFormatterException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InputFormatterException"/> with the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public InputFormatterException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InputFormatterException"/> with the specified <paramref name="message"/> and
    /// inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InputFormatterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
