// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

internal sealed class BrowserPlatform : IBrowserPlatform, IAsyncDisposable
{
    public BrowserPlatform(Microsoft.JSInterop.IJSRuntime jsRuntime)
    {
        Window = new Window(jsRuntime);
    }

    public Window Window { get; }

    public ValueTask DisposeAsync()
    {
        return Window.DisposeAsync();
    }
}
