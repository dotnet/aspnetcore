// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

[OperatingSystemsFilter(true, OS.Windows)]
public class NamedPipesTransportBenchmark
{
    private const int _parallelCount = 10;
    private const int _parallelCallCount = 1000;
    private const string _plaintextExpectedResponse =
        "HTTP/1.1 200 OK\r\n" +
        "Content-Length: 13\r\n" +
        "Date: Fri, 02 Mar 2018 18:37:05 GMT\r\n" +
        "Content-Type: text/plain\r\n" +
        "Server: Kestrel\r\n" +
        "\r\n" +
        "Hello, World!";
    private static readonly byte[] _responseBuffer = new byte[_plaintextExpectedResponse.Length];

    private string _pipeName;
    private IHost _host;

    [Params(1, 2, 8, 16)]
    public int ListenerQueueCount { get; set; }

    [GlobalSetup]
    public void GlobalSetupPlaintext()
    {
        _pipeName = "MicrobenchmarksTestPipe-" + Path.GetRandomFileName();

        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    // Prevent VS from attaching to hosting startup which could impact results
                    .UseSetting("preventHostingStartup", "true")
                    .UseKestrel()
                    // Bind to a single non-HTTPS endpoint
                    .UseUrls($"http://pipe:/{_pipeName}")
                    .Configure(app => app.UseMiddleware<PlaintextMiddleware>())
                    .UseNamedPipes(options =>
                    {
                        options.ListenerQueueCount = ListenerQueueCount;
                    });
            })
            .Build();

        _host.Start();

        ValidateResponseAsync(RequestParsingData.PlaintextTechEmpowerRequest, _plaintextExpectedResponse).Wait();
    }

    private async Task ValidateResponseAsync(byte[] request, string expectedResponse)
    {
        var clientStream = CreateClientStream(_pipeName);
        await clientStream.ConnectAsync();
        await clientStream.WriteAsync(request);
        await clientStream.ReadAtLeastAsync(_responseBuffer, _responseBuffer.Length);
        await clientStream.DisposeAsync();

        var response = Encoding.ASCII.GetString(_responseBuffer);

        // Exclude date header since the value changes on every request
        var expectedResponseLines = expectedResponse.Split("\r\n").Where(s => !s.StartsWith("Date:", StringComparison.Ordinal));
        var responseLines = response.Split("\r\n").Where(s => !s.StartsWith("Date:", StringComparison.Ordinal));

        if (!Enumerable.SequenceEqual(expectedResponseLines, responseLines))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                "Invalid response", "Expected:", expectedResponse, "Actual:", response));
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _host.Dispose();
    }

    [Benchmark(OperationsPerInvoke = _parallelCount * _parallelCallCount)]
    public async Task Plaintext()
    {
        var parallelTasks = new Task[_parallelCount];
        for (var i = 0; i < _parallelCount; i++)
        {
            parallelTasks[i] = Task.Run(async () =>
            {
                var clientStreamCount = 0;
                while (clientStreamCount < _parallelCallCount)
                {
                    try
                    {
                        var namedPipeClient = CreateClientStream(_pipeName);
                        await namedPipeClient.ConnectAsync();
                        await namedPipeClient.WriteAsync(RequestParsingData.PlaintextTechEmpowerRequest);
                        await namedPipeClient.ReadAtLeastAsync(_responseBuffer, _responseBuffer.Length);
                        namedPipeClient.Dispose();

                        clientStreamCount++;
                    }
                    catch (IOException)
                    {
                    }
                }
            });
        }

        await Task.WhenAll(parallelTasks);
    }

    private static NamedPipeClientStream CreateClientStream(string pipeName)
    {
        var clientStream = new NamedPipeClientStream(
            serverName: ".",
            pipeName: pipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.WriteThrough | PipeOptions.Asynchronous,
            impersonationLevel: TokenImpersonationLevel.Anonymous);
        return clientStream;
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
