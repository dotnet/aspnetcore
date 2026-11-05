// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the URL APIs from a <see cref="Window"/>.
/// </summary>
public static class WindowUrlExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets the browser URL constructor and static operations.
        /// </summary>
        public UrlConstructor Url
            => window.GetOrAddFeature(static jsRuntime => new UrlConstructor(jsRuntime));

        /// <summary>
        /// Gets the browser URLSearchParams constructor.
        /// </summary>
        public UrlSearchParamsConstructor UrlSearchParams
            => window.GetOrAddFeature(static jsRuntime => new UrlSearchParamsConstructor(jsRuntime));
    }
}
