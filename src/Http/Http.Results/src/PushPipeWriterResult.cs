// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

internal sealed class PushPipeWriterResult : FileResult
{
    private readonly Func<PipeWriter, Task> _pipeWriterCallback;

    public PushPipeWriterResult(Func<PipeWriter, Task> pipeWriterCallback, string? contentType) :
        base(contentType)
    {
        _pipeWriterCallback = pipeWriterCallback;
    }

    protected override ILogger GetLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<ILogger<PushPipeWriterResult>>();
    }

    protected override Task ExecuteAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength)
    {
        return _pipeWriterCallback(httpContext.Response.BodyWriter);
    }
}
