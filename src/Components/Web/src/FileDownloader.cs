// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides an abstraction for downloading a file from a Blazor app.
/// </summary>
public sealed class FileDownloader : IFileDownloader
{
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Initializes the <see cref="FileDownloader"/>.
    /// </summary>
    /// <param name="JSRuntime">The <see cref="IJSRuntime"/> to use for interoperability.</param>
    public FileDownloader(IJSRuntime JSRuntime)
    {
        JS = JSRuntime;
    }

    /// <summary>
    /// Takes in a byte[] representing file data and converts it into a MemoryStream.
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that contains the specified file name.</param>
    /// <param name="data"> The <see cref="byte"/>[] data that will be converted into a <see cref="MemoryStream"/>.</param>
    public Task SaveAs(string fileName, byte[] data)
    {
        var fileStream = new MemoryStream(data);

        return SaveAs(fileName, fileStream);
    }

    /// <summary>
    /// Takes in a Stream representing file data, converts it into a DotNetStreamReference, and invokes JS to save the file to the specified file name.
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that contains the specified file name.</param>
    /// <param name="data"> The <see cref="Stream"/> data that is converted and streamed to the client.</param>
    public async Task SaveAs(string fileName, Stream data)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        using var streamRef = new DotNetStreamReference(stream: data);

        await JS.InvokeVoidAsync("Blazor._internal.downloadFile", streamRef, fileName);
    }
}
