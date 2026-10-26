// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Cookie Store API from a <see cref="Window"/>.
/// </summary>
public static class WindowCookieStoreExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets the cookie store of the current page.
        /// </summary>
        public CookieStore CookieStore
            => window.GetOrAddFeature(static jsRuntime => new CookieStore(jsRuntime));
    }
}
