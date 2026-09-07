// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class LiveSpeechButtonInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/MessageInput.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _recognizer;

    public async ValueTask<bool> IsSupportedAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>(
            "isLiveSpeechRecognitionSupported");
    }

    public async ValueTask InitializeAsync<T>(
        DotNetObjectReference<T> callbacks,
        string? language)
        where T : class
    {
        var module = await GetModuleAsync();
        _recognizer ??= await module.InvokeAsync<IJSObjectReference>(
            "createLiveSpeechRecognizer",
            callbacks,
            language);
    }

    public ValueTask StartAsync()
    {
        return _recognizer is null
            ? ValueTask.CompletedTask
            : _recognizer.InvokeVoidAsync("start");
    }

    public ValueTask StopAsync()
    {
        return _recognizer is null
            ? ValueTask.CompletedTask
            : _recognizer.InvokeVoidAsync("stop");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_recognizer is not null)
            {
                await _recognizer.InvokeVoidAsync("dispose");
                await _recognizer.DisposeAsync();
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

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        return _module;
    }
}
