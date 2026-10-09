// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Web Storage API from a <see cref="Window"/>.
/// </summary>
public static class WindowWebStorageExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets storage shared by pages from the same origin.
        /// </summary>
        public Storage LocalStorage
            => window.GetOrAddFeature("localStorage", static jsRuntime => new Storage(jsRuntime, "localStorage"));

        /// <summary>
        /// Gets storage scoped to the current browser tab.
        /// </summary>
        public Storage SessionStorage
            => window.GetOrAddFeature("sessionStorage", static jsRuntime => new Storage(jsRuntime, "sessionStorage"));
    }
}
