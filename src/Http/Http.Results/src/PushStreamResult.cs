// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

internal sealed class PushStreamResult : FileResult
{
    private readonly Func<Stream, Task> _streamWriterCallback;

    public PushStreamResult(Func<Stream, Task> streamWriterCallback, string? contentType)
        : base(contentType)
    {
        _streamWriterCallback = streamWriterCallback;
    }

    protected override ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<ILogger<PushStreamResult>>();
    }

    protected override Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength)
    {
        return _streamWriterCallback(httpContext.Response.Body);
    }
}
