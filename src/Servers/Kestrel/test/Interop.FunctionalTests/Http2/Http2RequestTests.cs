// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Interop.FunctionalTests.Http2;

[Collection(nameof(NoParallelCollection))]
public class Http2RequestTests : LoggedTest
{
    [Fact]
    public async Task InvalidHandshake_MetricsHasErrorType()
    {
        // Arrange
        var builder = CreateHostBuilder(
            c =>
            {
                return Task.CompletedTask;
            },
            protocol: HttpProtocols.Http2,
            plaintext: true);

        using (var host = builder.Build())
        {
            var meterFactory = host.Services.GetRequiredService<IMeterFactory>();

            // Use MeterListener for this test because we want to check that a single error.type tag is added.
            // MetricCollector can't be used for this because it stores tags in a dictionary and overwrites values.
            var measurementTcs = new TaskCompletionSource<Measurement<double>>();
            var meterListener = new MeterListener();
            meterListener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Scope == meterFactory &&
                    instrument.Meter.Name == "Microsoft.AspNetCore.Server.Kestrel" &&
                    instrument.Name == "kestrel.connection.duration")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                    meterListener.SetMeasurementEventCallback<double>((Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state) =>
                    {
                        measurementTcs.SetResult(new Measurement<double>(measurement, tags));
                    });
                }
            };
            meterListener.Start();

            await host.StartAsync();
            var client = HttpHelpers.CreateClient(maxResponseHeadersLength: 1024);

            // Act
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.LingerState = new LingerOption(false, 0);

            socket.Connect(IPAddress.Loopback, host.GetPort());
            socket.Send(new byte[1024 * 16]);

            // Wait for measurement to be available.
            var measurement = await measurementTcs.Task.DefaultTimeout();

            // Assert
            Assert.True(measurement.Value > 0);

            var tags = measurement.Tags.ToArray();
            Assert.Equal("http", (string)tags.Single(t => t.Key == "network.protocol.name").Value);
            Assert.Equal("2", (string)tags.Single(t => t.Key == "network.protocol.version").Value);
            Assert.Equal("tcp", (string)tags.Single(t => t.Key == "network.transport").Value);
            Assert.Equal("ipv4", (string)tags.Single(t => t.Key == "network.type").Value);
            Assert.Equal("127.0.0.1", (string)tags.Single(t => t.Key == "server.address").Value);
            Assert.Equal(host.GetPort(), (int)tags.Single(t => t.Key == "server.port").Value);
            Assert.Equal("invalid_handshake", (string)tags.Single(t => t.Key == "error.type").Value);

            socket.Close();

            await host.StopAsync();
        }
    }

    [Fact]
    public async Task GET_Metrics_HttpProtocolAndTlsSet()
    {
        // Arrange
        var protocolTcs = new TaskCompletionSource<SslProtocols>(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = CreateHostBuilder(
            c =>
            {
                protocolTcs.SetResult(c.Features.Get<ISslStreamFeature>().SslStream.SslProtocol);
                return Task.CompletedTask;
            },
            configureKestrel: o =>
            {
                // Test IPv6 endpoint with metrics.
                o.Listen(IPAddress.IPv6Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    listenOptions.UseHttps(TestResources.GetTestCertificate(), https =>
                    {
                        https.SslProtocols = SslProtocols.Tls12;
                    });
                });
            });

        using (var host = builder.Build())
        {
            var meterFactory = host.Services.GetRequiredService<IMeterFactory>();

            using var connectionDuration = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

            await host.StartAsync();
            var client = HttpHelpers.CreateClient();

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://[::1]:{host.GetPort()}/");
            request1.Version = HttpVersion.Version20;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response1 = await client.SendAsync(request1, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            var protocol = await protocolTcs.Task.DefaultTimeout();

            // Dispose the client to end the connection.
            client.Dispose();
            // Wait for measurement to be available.
            await connectionDuration.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

            // Assert
            Assert.Collection(connectionDuration.GetMeasurementSnapshot(),
                m =>
                {
                    Assert.True(m.Value > 0);
                    Assert.Equal("http", (string)m.Tags["network.protocol.name"]);
                    Assert.Equal("2", (string)m.Tags["network.protocol.version"]);
                    Assert.Equal("tcp", (string)m.Tags["network.transport"]);
                    Assert.Equal("ipv6", (string)m.Tags["network.type"]);
                    Assert.Equal("::1", (string)m.Tags["server.address"]);
                    Assert.Equal(host.GetPort(), (int)m.Tags["server.port"]);
                    Assert.Equal("1.2", (string)m.Tags["tls.protocol.version"]);
                });

            await host.StopAsync();
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GET_LargeResponseHeader_Success(bool largeValue, bool largeKey)
    {
        // Arrange
        var longKey = "key-" + new string('$', largeKey ? 128 * 1024 : 1);
        var longValue = "value-" + new string('!', largeValue ? 128 * 1024 : 1);
        var builder = CreateHostBuilder(
            c =>
            {
                c.Response.Headers["test"] = "abc";
                c.Response.Headers[longKey] = longValue;
                return Task.CompletedTask;
            },
            protocol: HttpProtocols.Http2,
            plaintext: true);

        using (var host = builder.Build())
        {
            await host.StartAsync();
            var client = HttpHelpers.CreateClient(maxResponseHeadersLength: 1024);

            // Act
            var request1 = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{host.GetPort()}/");
            request1.Version = HttpVersion.Version20;
            request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request1, CancellationToken.None);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("abc", response.Headers.GetValues("test").Single());
            Assert.Equal(longValue, response.Headers.GetValues(longKey).Single());

            await host.StopAsync();
        }
    }

    [Fact]
    public async Task GET_NoTLS_Http11RequestToHttp2Endpoint_400Result()
    {
        // Arrange
        var builder = CreateHostBuilder(c => Task.CompletedTask, protocol: HttpProtocols.Http2, plaintext: true);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseMessage = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, responseMessage.StatusCode);
            Assert.Equal("An HTTP/1.x request was sent to an HTTP/2 only endpoint.", await responseMessage.Content.ReadAsStringAsync());
        }
    }

    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/41074")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GET_RequestReturnsLargeData_GracefulShutdownDuringRequest_RequestGracefullyCompletes(bool hasTrailers)
    {
        // Enable client logging.
        // Test failure on CI could be from HttpClient bug.
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        // Arrange
        const int DataLength = 500_000;
        var randomBytes = Enumerable.Range(1, DataLength).Select(i => (byte)((i % 10) + 48)).ToArray();

        var syncPoint = new SyncPoint();

        ILogger logger = null;
        var builder = CreateHostBuilder(
            async c =>
            {
                await syncPoint.WaitToContinue();

                var memory = c.Response.BodyWriter.GetMemory(randomBytes.Length);

                logger.LogInformation($"Server writing {randomBytes.Length} bytes response");
                randomBytes.CopyTo(memory);

                // It's important for this test that the large write is the last data written to
                // the response and it's not awaited by the request delegate.
                logger.LogInformation($"Server advancing {randomBytes.Length} bytes response");
                c.Response.BodyWriter.Advance(randomBytes.Length);

                if (hasTrailers)
                {
                    c.Response.AppendTrailer("test-trailer", "value!");
                }
            },
            protocol: HttpProtocols.Http2,
            plaintext: true);

        using var host = builder.Build();
        logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Test");

        var client = HttpHelpers.CreateClient();

        // Act
        await host.StartAsync().DefaultTimeout();

        var longRunningTask = StartLongRunningRequestAsync(logger, host, client);

        logger.LogInformation("Waiting for request on server");
        await syncPoint.WaitForSyncPoint().DefaultTimeout();

        logger.LogInformation("Stopping server");
        var stopTask = host.StopAsync();

        syncPoint.Continue();

        var (readData, trailers) = await longRunningTask.DefaultTimeout();
        await stopTask.DefaultTimeout();

        // Assert
        Assert.Equal(randomBytes, readData);
        if (hasTrailers)
        {
            Assert.Equal("value!", trailers.GetValues("test-trailer").Single());
        }
    }

    private static async Task<(byte[], HttpResponseHeaders)> StartLongRunningRequestAsync(ILogger logger, IHost host, HttpMessageInvoker client)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{host.GetPort()}/");
        request.Headers.Host = "localhost2";
        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        logger.LogInformation($"Sending request to '{request.RequestUri}'.");
        var responseMessage = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();
        responseMessage.EnsureSuccessStatusCode();

        var responseStream = await responseMessage.Content.ReadAsStreamAsync();

        logger.LogInformation($"Started reading response content");
        var data = new List<byte>();
        var buffer = new byte[1024 * 128];
        int readCount;
        try
        {
            while ((readCount = await responseStream.ReadAsync(buffer)) != 0)
            {
                data.AddRange(buffer.AsMemory(0, readCount).ToArray());
                logger.LogInformation($"Received {readCount} bytes. Total {data.Count} bytes.");
            }
        }
        catch
        {
            logger.LogInformation($"Error reading response. Total {data.Count} bytes.");

            throw;
        }
        logger.LogInformation($"Finished reading response content");

        return (data.ToArray(), responseMessage.TrailingHeaders);
    }

    private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null, bool? plaintext = null)
    {
        return HttpHelpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel, plaintext);
    }
}
