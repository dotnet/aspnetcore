// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class BrowserFileFromFormFile(IFormFile formFile) : IBrowserFile
{
    public string Name => formFile.Name;

    public DateTimeOffset LastModified => DateTimeOffset.Parse(formFile.Headers.LastModified.ToString(), CultureInfo.InvariantCulture);

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
