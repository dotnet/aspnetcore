// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

/// <summary>
/// Represents an <see cref="FileResultBase"/> that when executed will
/// write a file from a stream to the response.
/// </summary>
internal sealed class FileStreamResult : FileResult, IResult
{
    /// <summary>
    /// Creates a new <see cref="FileStreamResult"/> instance with
    /// the provided <paramref name="fileStream"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileStreamResult(Stream fileStream, string? contentType)
        : base(contentType)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        FileStream = fileStream;
        if (fileStream.CanSeek)
        {
            FileLength = fileStream.Length;
        }
    }

    /// <summary>
    /// Gets or sets the stream with the file that will be sent back as the response.
    /// </summary>
    public Stream FileStream { get; }

    protected override ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<ILogger<FileStreamResult>>();
    }

    public override async Task ExecuteAsync(HttpContext httpContext)
    {
        await using (FileStream)
        {
            await base.ExecuteAsync(httpContext);
        }
    }

    protected override Task ExecuteCoreAsync(HttpContext context, RangeItemHeaderValue? range, long rangeLength)
    {
        return FileResultHelper.WriteFileAsync(context, FileStream, range, rangeLength);
    }
}
