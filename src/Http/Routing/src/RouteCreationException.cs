// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// The exception that is thrown for invalid routes or constraints.
/// </summary>
public class RouteCreationException : Exception
#else
internal class RouteCreationException : Exception
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RouteCreationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RouteCreationException(string message)
            : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteCreationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RouteCreationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
