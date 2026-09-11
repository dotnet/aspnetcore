// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class MessageAttachButtonInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/MessageInput.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _registration;

    public async ValueTask InitializeAsync(ElementReference container, string dropZoneSelector)
    {
        _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _registration = await _module.InvokeAsync<IJSObjectReference>(
            "registerFileDropZone",
            container,
            dropZoneSelector);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_registration is not null)
            {
                await _registration.InvokeVoidAsync("dispose");
                await _registration.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
