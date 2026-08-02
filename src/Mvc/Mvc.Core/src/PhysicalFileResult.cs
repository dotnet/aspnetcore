// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="FileResult"/> on execution will write a file from disk to the response
/// using mechanisms provided by the host.
/// </summary>
public class PhysicalFileResult : FileResult
{
    private string _fileName;

    /// <summary>
    /// Creates a new <see cref="PhysicalFileResult"/> instance with
    /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public PhysicalFileResult(string fileName, string contentType)
        : this(fileName, MediaTypeHeaderValue.Parse(contentType))
    {
        ArgumentNullException.ThrowIfNull(fileName);
    }

    /// <summary>
    /// Creates a new <see cref="PhysicalFileResult"/> instance with
    /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public PhysicalFileResult(string fileName, MediaTypeHeaderValue contentType)
        : base(contentType.ToString())
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

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<PhysicalFileResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
