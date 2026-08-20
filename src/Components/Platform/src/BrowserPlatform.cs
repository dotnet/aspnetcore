// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

internal sealed class BrowserPlatform : IBrowserPlatform, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserPlatform(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        Window = new Window(jsRuntime);
        Url = new UrlConstructor(jsRuntime);
    }

    public Window Window { get; }

    public UrlConstructor Url { get; }

    public async ValueTask<Response> FetchAsync(
        string resource,
        RequestInit? options = null)
    {
        ArgumentNullException.ThrowIfNull(resource);

        object?[] arguments = options is null
            ? [resource]
            : [resource, options.ToJavaScriptValue()];

        var response = await _jsRuntime
            .InvokeAsync<IJSObjectReference>("fetch", arguments)
            .ConfigureAwait(false);

        return new Response(response);
    }

    public ValueTask DisposeAsync()
    {
        return Window.DisposeAsync();
    }
}
