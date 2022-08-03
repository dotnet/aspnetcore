// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Allows triggering a file download on the client from .NET.
/// </summary>
internal sealed class FileDownloader : IFileDownloader
{
    private readonly IJSRuntime _jsRuntime;
    public FileDownloader(IJSRuntime JSRuntime)
    {
        _jsRuntime = JSRuntime;
    }

    /// <inheritdoc />
    public Task SaveAs(string fileName, byte[] data)
    {
        var fileStream = new MemoryStream(data);

        return SaveAs(fileName, fileStream);
    }

    /// <inheritdoc />
    public async Task SaveAs(string fileName, Stream data)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        using var streamRef = new DotNetStreamReference(stream: data);

        await _jsRuntime.InvokeVoidAsync("Blazor._internal.downloadFile", streamRef, fileName);
    }
}
