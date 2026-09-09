// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class AudioCaptureButtonInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/MessageInput.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _recorder;
    private IJSObjectReference? _speechRecognizer;

    public async ValueTask<bool> IsAudioCaptureSupportedAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("isAudioCaptureSupported");
    }

    public async ValueTask StartRecordingAsync<T>(
        long maximumBytes,
        DotNetObjectReference<T> callbacks)
        where T : class
    {
        var module = await GetModuleAsync();
        _recorder ??= await module.InvokeAsync<IJSObjectReference>(
            "createAudioRecorder",
            maximumBytes,
            callbacks);
        await _recorder.InvokeVoidAsync("start");
    }

    public ValueTask<AudioCaptureResult> StopRecordingAsync()
    {
        return _recorder is null
            ? ValueTask.FromResult(new AudioCaptureResult())
            : _recorder.InvokeAsync<AudioCaptureResult>("stop");
    }

    public async ValueTask<bool> IsSpeechRecognitionSupportedAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>(
            "isLiveSpeechRecognitionSupported");
    }

    public async ValueTask InitializeSpeechRecognitionAsync<T>(
        DotNetObjectReference<T> callbacks,
        string? language)
        where T : class
    {
        var module = await GetModuleAsync();
        _speechRecognizer ??= await module.InvokeAsync<IJSObjectReference>(
            "createLiveSpeechRecognizer",
            callbacks,
            language);
    }

    public ValueTask StartSpeechRecognitionAsync()
    {
        return _speechRecognizer is null
            ? ValueTask.CompletedTask
            : _speechRecognizer.InvokeVoidAsync("start");
    }

    public ValueTask StopSpeechRecognitionAsync()
    {
        return _speechRecognizer is null
            ? ValueTask.CompletedTask
            : _speechRecognizer.InvokeVoidAsync("stop");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_recorder is not null)
            {
                await _recorder.InvokeVoidAsync("dispose");
                await _recorder.DisposeAsync();
            }

            if (_speechRecognizer is not null)
            {
                await _speechRecognizer.InvokeVoidAsync("dispose");
                await _speechRecognizer.DisposeAsync();
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

internal sealed class AudioCaptureResult
{
    public IJSStreamReference? StreamReference { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public long Size { get; set; }

    public bool TooLarge { get; set; }
}
