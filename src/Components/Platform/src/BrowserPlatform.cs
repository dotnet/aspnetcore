// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Platform;

internal sealed class BrowserPlatform : IBrowserPlatform, IAsyncDisposable
{
    public BrowserPlatform(Microsoft.JSInterop.IJSRuntime jsRuntime, ILoggerFactory? loggerFactory = null)
    {
        Window = new Window(jsRuntime, loggerFactory ?? NullLoggerFactory.Instance);
    }

    public Window Window { get; }

    public ValueTask DisposeAsync()
    {
        return ((IInternalAsyncDisposal)Window).InternalDisposeAsync();
    }
}
