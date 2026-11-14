// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class InMemoryTransportTelemetryBenchmark
{
    private const int Pipelining = 16;

    private const string Request =
        "GET /plaintext HTTP/1.1\r\n" +
        "Host: localhost:5000\r\n" +
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36\r\n" +
        "Accept: text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    private const string ExpectedResponse =
        "HTTP/1.1 200 OK\r\n" +
        "Content-Length: 13\r\n" +
        "Date: Fri, 02 Mar 2018 18:37:05 GMT\r\n" +
        "Content-Type: text/plain\r\n" +
        "Server: Kestrel\r\n" +
        "\r\n" +
        "Hello, World!";

    private static readonly byte[] _pipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(Request, Pipelining)));
    private static readonly string _pipelinedExpectedResponse = string.Concat(Enumerable.Repeat(ExpectedResponse, Pipelining));

    private IHost _host;
    private InMemoryTransportBenchmark.InMemoryConnection _connection;
    private ActivityListener _activityListener;
    private MeterListener _meterListener;

    [Params(false, true)]
    public bool Tracing { get; set; }

    [Params(false, true)]
    public bool Metrics { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        if (Tracing)
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(_activityListener);
        }

        if (Metrics)
        {
            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name is "Microsoft.AspNetCore.Hosting" or "Microsoft.AspNetCore.Server.Kestrel")
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };
            _meterListener.SetMeasurementEventCallback<long>(static (_, _, _, _) => { });
            _meterListener.SetMeasurementEventCallback<double>(static (_, _, _, _) => { });
            _meterListener.Start();
        }

        var transportFactory = new InMemoryTransportBenchmark.InMemoryTransportFactory(connectionsPerEndPoint: 1);

        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    // Prevent VS from attaching to hosting startup which could impact results
                    .UseSetting("preventHostingStartup", "true")
                    .UseKestrel()
                    // Bind to a single non-HTTPS endpoint
                    .UseUrls("http://127.0.0.1:5000")
                    .Configure(app => app.UseMiddleware<InMemoryTransportBenchmark.PlaintextMiddleware>());
            })
            .ConfigureServices(services => services.AddSingleton<IConnectionListenerFactory>(transportFactory))
            .Build();

        _host.Start();

        // Ensure there is a single endpoint and single connection
        _connection = transportFactory.Connections.Values.Single().Single();

        ValidateResponseAsync().GetAwaiter().GetResult();
    }

    private async Task ValidateResponseAsync()
    {
        await _connection.SendRequestAsync(_pipelinedRequests);
        var response = Encoding.ASCII.GetString(await _connection.GetResponseAsync(_pipelinedExpectedResponse.Length));

        // Exclude date header since the value changes on every request
        var expectedResponseLines = _pipelinedExpectedResponse.Split("\r\n").Where(s => !s.StartsWith("Date:", StringComparison.Ordinal));
        var responseLines = response.Split("\r\n").Where(s => !s.StartsWith("Date:", StringComparison.Ordinal));

        if (!Enumerable.SequenceEqual(expectedResponseLines, responseLines))
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                "Invalid response", "Expected:", _pipelinedExpectedResponse, "Actual:", response));
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _host.Dispose();
        _activityListener?.Dispose();
        _meterListener?.Dispose();
    }

    [Benchmark(OperationsPerInvoke = Pipelining)]
    public async Task PlaintextPipelined()
    {
        await _connection.SendRequestAsync(_pipelinedRequests);
        await _connection.ReadResponseAsync(_pipelinedExpectedResponse.Length);
    }
}
