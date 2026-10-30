// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class InputFileJsCallbacksRelay : IDisposable
{
    private const long MaxFileListSize = 1024 * 1024;

    private readonly IInputFileJsCallbacks _callbacks;
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private readonly CancellationToken _disposeCancellationToken;
    private Task _pendingFileList = Task.CompletedTask;
    private bool _disposed;

    public IDisposable DotNetReference { get; }

    [DynamicDependency(nameof(NotifyChange))]
    [DynamicDependency(nameof(NotifyChangeFromStream))]
    public InputFileJsCallbacksRelay(IInputFileJsCallbacks callbacks)
    {
        _callbacks = callbacks;
        _disposeCancellationToken = _disposeCancellationTokenSource.Token;

        DotNetReference = DotNetObjectReference.Create(this);
    }

    [JSInvokable]
    public Task NotifyChange(BrowserFile[] files)
        => NotifyChangeAsync(Task.FromResult(files));

    [JSInvokable]
    public Task NotifyChangeFromStream(IJSStreamReference fileList)
        => NotifyChangeAsync(ReadFilesAsync(fileList, _disposeCancellationToken));

    private async Task NotifyChangeAsync(Task<BrowserFile[]> filesTask)
    {
        var previousFileList = _pendingFileList;
        var fileListReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingFileList = fileListReady.Task;
        Task callback;

        try
        {
            await previousFileList;
            var files = await filesTask;
            _disposeCancellationToken.ThrowIfCancellationRequested();
            callback = _callbacks.NotifyChange(files);
        }
        finally
        {
            // Preserve notification order without waiting for application callbacks to finish.
            fileListReady.SetResult();
        }

        await callback;
    }

    private static async Task<BrowserFile[]> ReadFilesAsync(IJSStreamReference fileList, CancellationToken cancellationToken)
    {
        await using (fileList)
        await using (var stream = await fileList.OpenReadStreamAsync(maxAllowedSize: MaxFileListSize, cancellationToken))
        {
            return await JsonSerializer.DeserializeAsync(stream, InputFileJsonSerializerContext.Default.BrowserFileArray, cancellationToken)
                ?? throw new JsonException("The file metadata must be a JSON array.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _disposeCancellationTokenSource.Cancel();
        DotNetReference.Dispose();
        _disposeCancellationTokenSource.Dispose();
    }
}
