// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

/// <summary>
/// A <see cref="FileResultBase" /> that on execution writes the file specified using a virtual path to the response
/// using mechanisms provided by the host.
/// </summary>
internal sealed class VirtualFileResult : FileResult, IResult
{
    private string _fileName;
    private IFileInfo? _fileInfo;

    /// <summary>
    /// Creates a new <see cref="VirtualFileResult"/> instance with the provided <paramref name="fileName"/>
    /// and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public VirtualFileResult(string fileName, string? contentType)
        : base(contentType)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    /// <summary>
    /// Gets or sets the path to the file that will be sent back as the response.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        [MemberNotNull(nameof(_fileName))]
        set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected override Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength)
    {
        var response = httpContext.Response;
        var offset = 0L;
        var count = (long?)null;
        if (range != null)
        {
            offset = range.From ?? 0L;
            count = rangeLength;
        }

        return response.SendFileAsync(
            _fileInfo!,
            offset,
            count);
    }

    protected override ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<ILogger<VirtualFileResult>>();
    }

    /// <inheritdoc/>
    public override Task ExecuteAsync(HttpContext httpContext)
    {
        var hostingEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        var fileInfo = GetFileInformation(hostingEnvironment.WebRootFileProvider);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not find file: {FileName}.", FileName);
        }

        _fileInfo = fileInfo;
        LastModified = LastModified ?? fileInfo.LastModified;
        FileLength = fileInfo.Length;

        return base.ExecuteAsync(httpContext);
    }

    internal IFileInfo GetFileInformation(IFileProvider fileProvider)
    {
        var normalizedPath = FileName;
        if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        var fileInfo = fileProvider.GetFileInfo(normalizedPath);
        return fileInfo;
    }
}
