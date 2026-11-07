// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the screen from a <see cref="Window"/>.
/// </summary>
public static class WindowScreenExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets the screen that the browser window is shown on.
        /// </summary>
        public Screen Screen
            => window.GetOrAddFeature(static jsRuntime => new Screen(jsRuntime));
    }
}
