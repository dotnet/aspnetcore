// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestApp
    {
        public static async Task EchoApp(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var buffer = new byte[httpContext.Request.ContentLength ?? 0];

            if (buffer.Length > 0)
            {
                await request.Body.ReadUntilEndAsync(buffer).DefaultTimeout();
                await response.Body.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public static async Task EchoAppChunked(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var data = new MemoryStream();
            await request.Body.CopyToAsync(data);
            var bytes = data.ToArray();

            response.Headers["Content-Length"] = bytes.Length.ToString();
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        public static Task EmptyApp(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        public static async Task EchoAppPipeWriter(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var buffer = new byte[httpContext.Request.ContentLength ?? 0];

            if (buffer.Length > 0)
            {
                await request.Body.ReadUntilEndAsync(buffer).DefaultTimeout();
                await response.StartAsync();
                var memory = response.BodyWriter.GetMemory(buffer.Length);
                buffer.CopyTo(memory);
                response.BodyWriter.Advance(buffer.Length);
                await response.BodyWriter.FlushAsync();
            }
        }

        public static async Task EchoAppPipeWriterChunked(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var data = new MemoryStream();
            await request.Body.CopyToAsync(data);
            var bytes = data.ToArray();

            response.Headers["Content-Length"] = bytes.Length.ToString();
            await response.StartAsync();

            var memory = response.BodyWriter.GetMemory(bytes.Length);
            bytes.CopyTo(memory);
            response.BodyWriter.Advance(bytes.Length);
            await response.BodyWriter.FlushAsync();
        }
    }
}
