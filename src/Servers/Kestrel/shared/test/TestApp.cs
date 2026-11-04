// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.InternalTesting;

public static class TestApp
{
    public static async Task EchoApp(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        var buffer = new byte[httpContext.Request.ContentLength ?? 0];

        if (buffer.Length > 0)
        {
            await request.Body.FillBufferUntilEndAsync(buffer).DefaultTimeout();
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

        response.Headers.ContentLength = bytes.Length;
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
            await request.Body.FillBufferUntilEndAsync(buffer).DefaultTimeout();
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

        response.Headers.ContentLength = bytes.Length;
        await response.StartAsync();

        var memory = response.BodyWriter.GetMemory(bytes.Length);
        bytes.CopyTo(memory);
        response.BodyWriter.Advance(bytes.Length);
        await response.BodyWriter.FlushAsync();
    }
}
