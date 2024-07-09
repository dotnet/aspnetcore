// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Exception thrown when an <see cref="NavigationManager"/> is not able to navigate to a different url.
/// </summary>
public class NavigationException : Exception
{

    private const string RedirectExceptionMessage =
    "A navigation was initiated during static rendering. " +
    "This exception is not an error, but instead signals to the framework that a redirect should occur. " +
    "It should not be caught by application code.";

    /// <summary>
    /// Initializes a new <see cref="NavigationException"/> instance.
    /// </summary>
    public NavigationException(string uri) : base(RedirectExceptionMessage)
    {
        Location = uri;
    }

    /// <summary>
    /// Gets the uri to which navigation was attempted.
    /// </summary>
    public string Location { get; }
}
