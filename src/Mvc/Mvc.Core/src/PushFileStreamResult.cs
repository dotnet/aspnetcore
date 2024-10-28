// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="FileResult" /> that on execution writes the file using the specified stream writer callback.
/// </summary>
public class PushFileStreamResult : FileResult
{
    /// <summary>
    /// The callback that writes the file to the provided stream.
    /// </summary>
    public Func<Stream, Task> StreamWriterCallback { get; set; }

    /// <summary>
    /// Creates a new <see cref="PushFileStreamResult"/> instance with
    /// the provided <paramref name="streamWriterCallback"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="streamWriterCallback">The callback that writes the file to the provided stream.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public PushFileStreamResult(Func<Stream, Task> streamWriterCallback, string contentType) : base(contentType)
    {
        ArgumentNullException.ThrowIfNull(streamWriterCallback);

        StreamWriterCallback = streamWriterCallback;
    }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<PushFileStreamResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
