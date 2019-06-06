// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SampleDestination
{
    public class SampleMiddleware
    {
        private readonly RequestDelegate _next;

        public SampleMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            var content = Encoding.UTF8.GetBytes("Hello world");

            context.Response.Headers["X-AllowedHeader"] = "Test-Value";
            context.Response.Headers["X-DisallowedHeader"] = "Test-Value";

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength = content.Length;
            return context.Response.Body.WriteAsync(content, 0, content.Length);
        }
    }
}
