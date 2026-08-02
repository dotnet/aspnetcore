// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="ActionResult"/> that when executed will
/// write a binary file to the response.
/// </summary>
public class FileContentResult : FileResult
{
    private byte[] _fileContents;

    /// <summary>
    /// Creates a new <see cref="FileContentResult"/> instance with
    /// the provided <paramref name="fileContents"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileContentResult(byte[] fileContents, string contentType)
        : this(fileContents, MediaTypeHeaderValue.Parse(contentType))
    {
    }

    /// <summary>
    /// Creates a new <see cref="FileContentResult"/> instance with
    /// the provided <paramref name="fileContents"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileContentResult(byte[] fileContents, MediaTypeHeaderValue contentType)
        : base(contentType.ToString())
    {
        ArgumentNullException.ThrowIfNull(fileContents);

        FileContents = fileContents;
    }

    /// <summary>
    /// Gets or sets the file contents.
    /// </summary>
    public byte[] FileContents
    {
        get => _fileContents;
        [MemberNotNull(nameof(_fileContents))]
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _fileContents = value;
        }
    }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<FileContentResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
