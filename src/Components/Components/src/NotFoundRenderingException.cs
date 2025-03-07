// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Exception thrown when an <see cref="NavigationManager"/> is not able to render not found page.
/// </summary>
public class NotFoundRenderingException : Exception
{
        /// <summary>
    /// Creates a new instance of <see cref="NotFoundRenderingException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NotFoundRenderingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
