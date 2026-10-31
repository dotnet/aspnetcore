// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Microbenchmarks;

[AspNetCoreBenchmark]
public class PlaintextBenchmark
{
    private TestServer _server;

    private HttpClient _client;

    [GlobalSetup]
    public void Setup()
    {
        _server = TestServer.Create(builder => builder.UseMiddleware<PlaintextMiddleware>(), new LoggerFactory(), new IISServerOptions()).GetAwaiter().GetResult();
        // Recreate client, TestServer.Client has additional logging that can hurt performance
        _client = new HttpClient()
        {
            BaseAddress = _server.HttpClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(200),
        };
    }

    [Benchmark]
    public async Task Plaintext()
    {
        await _client.GetAsync("/plaintext");
    }

    // Copied from https://github.com/aspnet/benchmarks/blob/dev/src/Benchmarks/Middleware/PlaintextMiddleware.cs
    public class PlaintextMiddleware
    {
        private static readonly PathString _path = new PathString("/plaintext");
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        private readonly RequestDelegate _next;

        public PlaintextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments(_path, StringComparison.Ordinal))
            {
                return WriteResponse(httpContext.Response);
            }

            return _next(httpContext);
        }

        public static Task WriteResponse(HttpResponse response)
        {
            var payloadLength = _helloWorldPayload.Length;
            response.StatusCode = 200;
            response.ContentType = "text/plain";
            response.ContentLength = payloadLength;
            return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
        }
    }
}
