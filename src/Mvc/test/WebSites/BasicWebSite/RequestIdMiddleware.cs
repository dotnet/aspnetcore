// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicWebSite;

// Initializes a scoped-service with a request Id from a header
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var requestIdService = context.RequestServices.GetService<RequestIdService>();
        if (requestIdService.RequestId != null)
        {
            throw new InvalidOperationException("RequestId should be null here");
        }

        var requestId = context.Request.Headers.RequestId;
        requestIdService.RequestId = requestId;

        return _next(context);
    }
}
