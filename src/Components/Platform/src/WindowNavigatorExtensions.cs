// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the navigator from a <see cref="Window"/>.
/// </summary>
public static class WindowNavigatorExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets the state and identity of the browser.
        /// </summary>
        public Navigator Navigator
            => window.GetOrAddFeature(jsRuntime => new Navigator(jsRuntime, window));
    }
}
