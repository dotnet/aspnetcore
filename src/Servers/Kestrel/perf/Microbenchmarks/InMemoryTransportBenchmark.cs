// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class InMemoryTransportBenchmark
{
    private const string _plaintextExpectedResponse =
        "HTTP/1.1 200 OK\r\n" +
        "Content-Length: 13\r\n" +
        "Date: Fri, 02 Mar 2018 18:37:05 GMT\r\n" +
        "Content-Type: text/plain\r\n" +
        "Server: Kestrel\r\n" +
        "\r\n" +
        "Hello, World!";

    private static readonly string _plaintextPipelinedExpectedResponse =
        string.Concat(Enumerable.Repeat(_plaintextExpectedResponse, RequestParsingData.Pipelining));

    private IHost _host;
    private InMemoryConnection _connection;

    [GlobalSetup(Target = nameof(Plaintext) + "," + nameof(PlaintextPipelined))]
    public void GlobalSetupPlaintext()
    {
        var transportFactory = new InMemoryTransportFactory(connectionsPerEndPoint: 1);

        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    // Prevent VS from attaching to hosting startup which could impact results
                    .UseSetting("preventHostingStartup", "true")
                    .UseKestrel()
                    // Bind to a single non-HTTPS endpoint
                    .UseUrls("http://127.0.0.1:5000")
                    .Configure(app => app.UseMiddleware<PlaintextMiddleware>());
            })
            .ConfigureServices(services => services.AddSingleton<IConnectionListenerFactory>(transportFactory))
            .Build();

        _host.Start();

        // Ensure there is a single endpoint and single connection
        _connection = transportFactory.Connections.Values.Single().Single();

        ValidateResponseAsync(RequestParsingData.PlaintextTechEmpowerRequest, _plaintextExpectedResponse).Wait();
        ValidateResponseAsync(RequestParsingData.PlaintextTechEmpowerPipelinedRequests, _plaintextPipelinedExpectedResponse).Wait();
    }

    private async Task ValidateResponseAsync(byte[] request, string expectedResponse)
    {
        await _connection.SendRequestAsync(request);
        var response = Encoding.ASCII.GetString(await _connection.GetResponseAsync(expectedResponse.Length));

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

    [Benchmark]
    public async Task Plaintext()
    {
        await _connection.SendRequestAsync(RequestParsingData.PlaintextTechEmpowerRequest);
        await _connection.ReadResponseAsync(_plaintextExpectedResponse.Length);
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.Pipelining)]
    public async Task PlaintextPipelined()
    {
        await _connection.SendRequestAsync(RequestParsingData.PlaintextTechEmpowerPipelinedRequests);
        await _connection.ReadResponseAsync(_plaintextPipelinedExpectedResponse.Length);
    }

    internal sealed class InMemoryTransportFactory : IConnectionListenerFactory
    {
        private readonly int _connectionsPerEndPoint;

        private readonly Dictionary<EndPoint, IReadOnlyList<InMemoryConnection>> _connections =
            new Dictionary<EndPoint, IReadOnlyList<InMemoryConnection>>();

        public IReadOnlyDictionary<EndPoint, IReadOnlyList<InMemoryConnection>> Connections => _connections;

        public InMemoryTransportFactory(int connectionsPerEndPoint)
        {
            _connectionsPerEndPoint = connectionsPerEndPoint;
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var connections = new InMemoryConnection[_connectionsPerEndPoint];
            for (var i = 0; i < _connectionsPerEndPoint; i++)
            {
                connections[i] = new InMemoryConnection();
            }

            _connections.Add(endpoint, connections);

            return new ValueTask<IConnectionListener>(new InMemoryTransport(endpoint, connections));
        }
    }

    internal sealed class InMemoryTransport : IConnectionListener
    {
        private readonly IReadOnlyList<InMemoryConnection> _connections;
        private readonly TaskCompletionSource<ConnectionContext> _tcs = new TaskCompletionSource<ConnectionContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _acceptedConnections;

        public InMemoryTransport(EndPoint endpoint, IReadOnlyList<InMemoryConnection> connections)
        {
            EndPoint = endpoint;
            _connections = connections;
        }

        public EndPoint EndPoint { get; }

        public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_acceptedConnections < _connections.Count)
            {
                return new ValueTask<ConnectionContext>(_connections[_acceptedConnections++]);
            }
            return new ValueTask<ConnectionContext>(_tcs.Task);
        }

        public ValueTask DisposeAsync()
        {
            _tcs.TrySetResult(null);
            return default;
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            _tcs.TrySetResult(null);
            return default;
        }
    }

    internal sealed class InMemoryConnection : TransportConnection
    {
        public InMemoryConnection()
        {
            var inputOptions = new PipeOptions(useSynchronizationContext: false);
            var outputOptions = new PipeOptions(useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            Transport = pair.Transport;
            Application = pair.Application;
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public ValueTask<FlushResult> SendRequestAsync(byte[] request)
        {
            return Input.WriteAsync(request);
        }

        // Reads response as efficiently as possible, but doesn't return anything
        public async Task ReadResponseAsync(int length)
        {
            while (length > 0)
            {
                var result = await Output.ReadAsync();
                var buffer = result.Buffer;
                length -= (int)buffer.Length;
                Output.AdvanceTo(buffer.End);
            }

            if (length < 0)
            {
                throw new InvalidOperationException($"Invalid response, length={length}");
            }
        }

        // Returns response so it can be validated, but is slower and allocates more than ReadResponseAsync()
        public async Task<byte[]> GetResponseAsync(int length)
        {
            while (true)
            {
                var result = await Output.ReadAsync();
                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                try
                {
                    if (buffer.Length >= length)
                    {
                        var response = buffer.Slice(0, length);
                        consumed = response.End;
                        examined = response.End;
                        return response.ToArray();
                    }
                }
                finally
                {
                    Output.AdvanceTo(consumed, examined);
                }
            }
        }
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
