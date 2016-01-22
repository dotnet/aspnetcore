// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
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

            var requestId = context.Request.Headers["RequestId"];
            requestIdService.RequestId = requestId;

            return _next(context);
        }
    }
}