// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Exception thrown by <see cref="IValueProviderFactory"/> when the input is unable to be read.
/// </summary>
public sealed class ValueProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValueProviderException"/> with the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ValueProviderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValueProviderException"/> with the specified <paramref name="message"/> and
    /// inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValueProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
