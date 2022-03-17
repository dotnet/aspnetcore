// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Options;

namespace RoutingWebSite.HelloExtension;

public class HelloMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HelloOptions _helloOptions;
    private readonly byte[] _helloPayload;

    public HelloMiddleware(RequestDelegate next, IOptions<HelloOptions> helloOptions)
    {
        _next = next;
        _helloOptions = helloOptions.Value;

        var payload = new List<byte>();
        payload.AddRange(Encoding.UTF8.GetBytes("Hello"));
        if (!string.IsNullOrEmpty(_helloOptions.Greeter))
        {
            payload.Add((byte)' ');
            payload.AddRange(Encoding.UTF8.GetBytes(_helloOptions.Greeter));
        }
        _helloPayload = payload.ToArray();
    }

    public Task InvokeAsync(HttpContext context)
    {
        var response = context.Response;
        var payloadLength = _helloPayload.Length;
        response.StatusCode = 200;
        response.ContentType = "text/plain";
        response.ContentLength = payloadLength;
        return response.Body.WriteAsync(_helloPayload, 0, payloadLength);
    }
}
