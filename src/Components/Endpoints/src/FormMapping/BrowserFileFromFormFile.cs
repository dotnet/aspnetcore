// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class BrowserFileFromFormFile(IFormFile formFile) : IBrowserFile
{
    public string Name => formFile.Name;

    public DateTimeOffset LastModified => 
        HeaderUtilities.TryParseDate(formFile.Headers.LastModified.ToString(), out var lastModified) 
            ? lastModified 
            : DateTimeOffset.MinValue;

    public long Size => formFile.Length;

    public string ContentType => formFile.ContentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (Size > maxAllowedSize)
        {
            throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.");
        }

        return formFile.OpenReadStream();
    }
}
