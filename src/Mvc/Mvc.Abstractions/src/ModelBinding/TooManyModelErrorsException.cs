// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// The <see cref="Exception"/> that is thrown when too many model errors are encountered.
/// </summary>
public class TooManyModelErrorsException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="TooManyModelErrorsException"/> with the specified
    /// exception <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TooManyModelErrorsException(string message)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
}
