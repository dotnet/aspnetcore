// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using AGUI.Formatting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.AI;

namespace AGUIDojoApi;

// Streams AG-UI events to the client. The AG-UI dojo transport is Server-Sent Events, which
// is what AGUI.Client's HTTP transport reads, so the formatter is not negotiated here.
internal sealed class AGUIEventStreamResult : IResult
{
    private readonly IAsyncEnumerable<BaseEvent> _events;
    private readonly IAGUIEventStreamFormatter _formatter;
    private readonly CancellationToken _cancellationToken;

    internal AGUIEventStreamResult(
        IAsyncEnumerable<BaseEvent> events,
        IAGUIEventStreamFormatter formatter,
        CancellationToken cancellationToken)
    {
        _events = events;
        _formatter = formatter;
        _cancellationToken = cancellationToken;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = _formatter.MediaType;
        response.Headers.CacheControl = "no-cache,no-store";
        response.Headers.Pragma = "no-cache";

        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            httpContext.RequestAborted,
            _cancellationToken);

        var body = response.Body;
        await _formatter.WriteAsync(_events, body, linked.Token);
        await body.FlushAsync(linked.Token);
    }
}
