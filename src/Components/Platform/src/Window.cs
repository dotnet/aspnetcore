// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the active browser window.
/// </summary>
public sealed class Window : IAsyncDisposable
{
    internal Window(IJSRuntime jsRuntime)
    {
        LocalStorage = new Storage(jsRuntime, "localStorage");
        SessionStorage = new Storage(jsRuntime, "sessionStorage");
    }

    /// <summary>
    /// Gets storage shared by pages from the same origin.
    /// </summary>
    public Storage LocalStorage { get; }

    /// <summary>
    /// Gets storage scoped to the current browser tab.
    /// </summary>
    public Storage SessionStorage { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await LocalStorage.DisposeAsync().ConfigureAwait(false);
        await SessionStorage.DisposeAsync().ConfigureAwait(false);
    }
}
