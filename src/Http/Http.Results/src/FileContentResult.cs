// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

internal sealed partial class FileContentResult : FileResult
{
    /// <summary>
    /// Creates a new <see cref="FileContentResult"/> instance with
    /// the provided <paramref name="fileContents"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileContentResult(ReadOnlyMemory<byte> fileContents, string? contentType)
        : base(contentType)
    {
        FileContents = fileContents;
        FileLength = fileContents.Length;
    }

    /// <summary>
    /// Gets or sets the file contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileContents { get; init; }

    protected override ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<ILogger<FileContentResult>>();
    }

    protected override Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength)
    {
        return FileResultHelper.WriteFileAsync(httpContext, FileContents, range, rangeLength);
    }
}
