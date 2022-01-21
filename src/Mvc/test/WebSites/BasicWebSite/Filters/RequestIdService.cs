// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicWebSite;

public class RequestIdService
{
    // This service can only be instantiated by a request-scoped container
    public RequestIdService(IServiceProvider services, IHttpContextAccessor contextAccessor)
    {
        if (contextAccessor.HttpContext.RequestServices != services)
        {
            throw new InvalidOperationException();
        }
    }

    public string RequestId { get; set; }
}
