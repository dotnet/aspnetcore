// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using AGUI.Formatting;
using Microsoft.AspNetCore.Http.Features;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimAgentEventStreamResult : IResult
{
    private readonly IAsyncEnumerable<BaseEvent> _events;
    private readonly IAGUIEventStreamFormatter _formatter;
    private readonly CancellationToken _cancellationToken;

    public ClaimAgentEventStreamResult(
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
        await _formatter.WriteAsync(_events, response.Body, linked.Token);
        await response.Body.FlushAsync(linked.Token);
    }
}
