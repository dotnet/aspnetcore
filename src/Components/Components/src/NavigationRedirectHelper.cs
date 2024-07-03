// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Helper class to throw a <see cref="NavigationException"/>.
/// </summary>
public static class NavigationRedirectHelper
{
    private const string RedirectExceptionMessage =
        "A navigation was initiated during static rendering. " +
        "This exception is not an error, but instead signals to the framework that a redirect should occur. " +
        "It should not be caught by application code.";

    /// <summary>
    /// Creates an absolute URI and throws a new <see cref="NavigationException"/>.
    /// </summary>

    [DoesNotReturn]
    public static void ThrowNavigationExceptionForRedirect(NavigationManager navigationManager, string uri)
    {
        var absoluteUriString = navigationManager.ToAbsoluteUri(uri).AbsoluteUri;
        throw new NavigationException(absoluteUriString, RedirectExceptionMessage);
    }
}
