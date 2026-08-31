// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

// DirectTls terminates TLS through the runtime's native, file-descriptor-bound OpenSSL session, which is
// Linux-only. These end-to-end tests start a real Kestrel host on the DirectTls transport and drive it with
// a standard SslStream client.
public class DirectTlsFunctionalTests : LoggedTest
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Handshake_RecordsExistingKestrelTlsMetrics()
    {
        using var metrics = new HandshakeMetricCollector();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));
        var port = host.GetPort();

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);
        await metrics.WaitForDurationAsync(port);

        Assert.Equal([1L, -1L], metrics.ActiveHandshakesForPort(port).Select(measurement => measurement.Value));
        var duration = Assert.Single(metrics.HandshakeDurationsForPort(port));
        Assert.True(duration.Value > 0);
        Assert.Equal(
            sslStream.SslProtocol == System.Security.Authentication.SslProtocols.Tls13 ? "1.3" : "1.2",
            duration.GetTag("tls.protocol.version"));

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task FailedHandshake_RecordsExistingKestrelTlsMetrics()
    {
        using var metrics = new HandshakeMetricCollector();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));
        var port = host.GetPort();

        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            await socket.ConnectAsync(IPAddress.Loopback, port);
            await socket.SendAsync("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n"u8.ToArray());
        }

        await metrics.WaitForDurationAsync(port);

        Assert.Equal([1L, -1L], metrics.ActiveHandshakesForPort(port).Select(measurement => measurement.Value));
        var duration = Assert.Single(metrics.HandshakeDurationsForPort(port));
        Assert.True(duration.Value > 0);
        Assert.NotNull(duration.GetTag("error.type"));

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Request_RecordsPumpTelemetry()
    {
        using var telemetry = new DirectTlsTelemetryListener();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        using (var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]))
        {
            await telemetry.WaitForCounterAsync("connections-owned", value => value > 0);
            var response = await SendRequestAsync(sslStream, "localhost");
            Assert.Contains("200 OK", response);
        }

        await telemetry.WaitForCounterAsync("accepts", value => value > 0);
        await telemetry.WaitForCounterAsync("bytes-read", value => value > 0);
        await telemetry.WaitForCounterAsync("bytes-written", value => value > 0);

        Assert.Contains(telemetry.Events, eventData => eventData.EventName == "ConnectionAccepted");
        Assert.Contains(telemetry.Events, eventData =>
            eventData.EventName == "PumpConnections" &&
            eventData.Payload is [_, int connectionCount] &&
            connectionCount > 0);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task InputBackpressure_RecordsPauseAndResumeTelemetry()
    {
        using var telemetry = new DirectTlsTelemetryListener();
        using var metrics = new PausedConnectionMetricCollector();
        var startReading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applicationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(
            endpoint,
            async context =>
            {
                applicationStarted.TrySetResult();
                await startReading.Task;
                await context.Request.Body.CopyToAsync(Stream.Null);
                await context.Response.WriteAsync("ok");
            },
            configureTransport: options => options.MaxReadBufferSize = 1024);

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);
        const int contentLength = 64 * 1024;
        await sslStream.WriteAsync(
            Encoding.ASCII.GetBytes(
                $"POST / HTTP/1.1\r\nHost: localhost\r\nContent-Length: {contentLength}\r\nConnection: close\r\n\r\n"));
        var bodyWrite = sslStream.WriteAsync(new byte[contentLength]).AsTask();

        await applicationStarted.Task.WaitAsync(Timeout);
        await WaitForLogAsync("ConnectionPause");
        await metrics.WaitForMeasurementAsync(1);
        await telemetry.WaitForCounterAsync("connections-paused", value => value >= 1);

        startReading.TrySetResult();
        await bodyWrite.WaitAsync(Timeout);
        await sslStream.FlushAsync();
        var response = await new StreamReader(sslStream, Encoding.ASCII).ReadToEndAsync().WaitAsync(Timeout);

        Assert.Contains("200 OK", response);
        await WaitForLogAsync("ConnectionResume");
        await metrics.WaitForMeasurementAsync(-1);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Get_OverTls_ReturnsResponse()
    {
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);
        var response = await SendRequestAsync(sslStream, "localhost");

        Assert.Contains("200 OK", response);
        Assert.Contains("ok", response);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ServerCertificateSelector_SelectsCertificateBySni()
    {
        var defaultCertificate = TestResources.GetTestCertificate();
        var sniCertificate = TestResources.GetTestCertificate("eku.server.pfx");

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificateSelector = (connection, hostName) =>
            string.Equals(hostName, "sni.example", StringComparison.OrdinalIgnoreCase)
                ? sniCertificate
                : defaultCertificate;

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));
        var port = host.GetPort();

        // The presented certificate's handle is owned by the SslStream and freed when it is disposed, so
        // capture its thumbprint inside the validation callback while it is still valid.
        string? defaultThumbprint = null;
        using (await ConnectAsync(port, "localhost", certificate => defaultThumbprint = certificate?.GetCertHashString(), [SslApplicationProtocol.Http11]))
        {
        }

        string? sniThumbprint = null;
        using (await ConnectAsync(port, "sni.example", certificate => sniThumbprint = certificate?.GetCertHashString(), [SslApplicationProtocol.Http11]))
        {
        }

        Assert.NotNull(defaultThumbprint);
        Assert.NotNull(sniThumbprint);
        Assert.Equal(defaultCertificate.GetCertHashString(), defaultThumbprint);
        Assert.Equal(sniCertificate.GetCertHashString(), sniThumbprint);
        Assert.NotEqual(defaultThumbprint, sniThumbprint);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Alpn_NegotiatesHttp2_WhenClientOffersHttp2()
    {
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http2]);

        Assert.Equal(SslApplicationProtocol.Http2, sslStream.NegotiatedApplicationProtocol);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ConnectionSocketFeature_CachesOneWrapper_AndDisposesItOnTeardown()
    {
        // The IConnectionSocketFeature.Socket wrapper is materialized lazily over the connection's raw fd. It
        // must be created once (repeated reads see the same instance) and disposed during connection teardown,
        // so a late read fails loudly instead of operating on a descriptor the session already closed and the
        // OS may have recycled - matching the sockets transport, which surfaces a disposed socket here.
        Socket? firstRead = null;
        Socket? secondRead = null;
        EndPoint? remoteWhileLive = null;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(endpoint, context =>
        {
            var feature = context.Features.Get<IConnectionSocketFeature>();
            Assert.NotNull(feature);

            // Two reads must return the identical wrapper.
            firstRead = feature.Socket;
            secondRead = feature.Socket;

            // Metadata is usable while the connection is live.
            remoteWhileLive = firstRead.RemoteEndPoint;

            return context.Response.WriteAsync("ok");
        });

        using (var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]))
        {
            var response = await SendRequestAsync(sslStream, "localhost");
            Assert.Contains("200 OK", response);
        }

        Assert.NotNull(firstRead);
        Assert.Same(firstRead, secondRead);
        Assert.NotNull(remoteWhileLive);

        // Stopping the host tears the connection down, which disposes the cached wrapper.
        await host.StopAsync().WaitAsync(Timeout);

        Assert.Throws<ObjectDisposedException>(() => firstRead!.RemoteEndPoint);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task TlsClientHelloBytesCallback_ReceivesClientHelloRecord()
    {
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.TlsClientHelloBytesCallback = (connection, bytes) => received.TrySetResult(bytes.ToArray());

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);

        var clientHello = await received.Task.WaitAsync(Timeout);

        Assert.NotEmpty(clientHello);
        Assert.Equal(0x16, clientHello[0]); // TLS handshake record content type.

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task TlsClientHelloBytesCallback_ThrowingCallback_DropsConnection_AndKeepsPumpHealthy()
    {
        // The ClientHello listener runs on the pump thread at NeedsTlsContext. Matching the socket-transport
        // TlsListener - which does not guard the user callback, so a throw fails the connection - a throwing
        // ClientHello callback must drop the handshake rather than be swallowed and continue. The first
        // connection trips the throwing callback; the second proves the pump survived and recovered.
        var callbackCalls = 0;
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.TlsClientHelloBytesCallback = (connection, bytes) =>
        {
            // Only the first callback throws; later connections observe the ClientHello normally.
            if (Interlocked.Increment(ref callbackCalls) == 1)
            {
                throw new InvalidOperationException("boom");
            }
        };

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        // First connection: the throwing callback drops the handshake. The client must observe the teardown
        // promptly - a connection/handshake error or a truncated (never a 200) response - and NOT hang, which is
        // what a stranded, spinning fd would cause. Bound the exchange well under the class Timeout so a hang
        // fails fast.
        var firstExchange = Task.Run(async () =>
        {
            using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);
            return await SendRequestAsync(sslStream, "localhost");
        });

        Exception? failure = null;
        string? firstResponse = null;
        try
        {
            firstResponse = await firstExchange.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        Assert.False(failure is TimeoutException, "Request hung: the dropped handshake stranded the fd instead of tearing it down.");
        if (failure is null)
        {
            Assert.DoesNotContain("200", firstResponse!);
        }

        // Second connection: the pump must still be healthy and serve a well-formed request.
        using var goodStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);
        var goodResponse = await SendRequestAsync(goodStream, "localhost");

        Assert.Contains("200 OK", goodResponse);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ClientCertificate_RequiredAndValidated_AllowsRequest()
    {
        var validationInvoked = false;
        string? serverObservedThumbprint = null;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        endpoint.Options.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            validationInvoked = true;
            return true;
        };

        using var host = await StartHostAsync(endpoint, context =>
        {
            // Read the surfaced client certificate while the request is in flight. This proves the accepted
            // leaf is still usable when Kestrel needs it - i.e. the transport does not dispose it too early.
            serverObservedThumbprint = context.Connection.ClientCertificate?.Thumbprint;
            return context.Response.WriteAsync("ok");
        });

        var clientCertificate = TestResources.GetTestCertificate("eku.client.pfx");
        using var sslStream = await ConnectAsync(
            host.GetPort(),
            "localhost",
            applicationProtocols: [SslApplicationProtocol.Http11],
            clientCertificate: clientCertificate);

        var response = await SendRequestAsync(sslStream, "localhost");

        Assert.Contains("200 OK", response);
        Assert.True(validationInvoked);
        Assert.Equal(clientCertificate.Thumbprint, serverObservedThumbprint);

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ClientCertificateValidation_ThrowingCallback_DropsConnection_AndKeepsPumpHealthy()
    {
        // The endpoint's client-certificate validation callback is dispatched off the pump once the fd-path
        // handshake reports Complete. If it throws, the transport must drop the connection - dispose the session
        // and de-register the fd - rather than leaving the fd epoll-registered but in neither the handshaking nor
        // the connection table, which would spin the pump on the level-triggered socket and hang the request. The
        // first connection here trips the throwing callback; the second proves the pump survived and recovered.
        var validationCalls = 0;
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        endpoint.Options.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            // Only the first validation throws; later connections validate normally.
            if (Interlocked.Increment(ref validationCalls) == 1)
            {
                throw new InvalidOperationException("boom");
            }

            return true;
        };

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));
        var clientCertificate = TestResources.GetTestCertificate("eku.client.pfx");

        // First connection: the throwing callback drops it. The client must observe the teardown promptly - a
        // connection error or a truncated (never a 200) response - and NOT hang, which is what a stranded,
        // spinning fd would cause. Bound the exchange well under the class Timeout so a hang fails fast.
        var firstExchange = Task.Run(async () =>
        {
            using var sslStream = await ConnectAsync(
                host.GetPort(),
                "localhost",
                applicationProtocols: [SslApplicationProtocol.Http11],
                clientCertificate: clientCertificate);
            return await SendRequestAsync(sslStream, "localhost");
        });

        Exception? failure = null;
        string? firstResponse = null;
        try
        {
            firstResponse = await firstExchange.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        Assert.False(failure is TimeoutException, "Request hung: the dropped handshake stranded the fd instead of tearing it down.");
        if (failure is null)
        {
            Assert.DoesNotContain("200", firstResponse!);
        }

        // Second connection: the pump must still be healthy and serve a well-formed mTLS request.
        using var goodStream = await ConnectAsync(
            host.GetPort(),
            "localhost",
            applicationProtocols: [SslApplicationProtocol.Http11],
            clientCertificate: clientCertificate);
        var goodResponse = await SendRequestAsync(goodStream, "localhost");

        Assert.Contains("200 OK", goodResponse);

        await host.StopAsync().WaitAsync(Timeout);
    }
    // fills to MaxReadBufferSize and the receive loop stops draining the socket, so the client's upload stalls
    // mid-flight. Once the app drains the body the upload completes and every byte arrives intact.
    //
    // Modeled on the canonical socket-transport test (MaxRequestBufferSizeTests.LargeUpload): the client sends
    // in small chunks and the test polls until the upload makes no progress for a while, rather than waiting a
    // fixed delay, so a slow machine keeps waiting instead of failing.
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task RequestBody_AppliesReadBackpressure_WhenAppStopsReading()
    {
        const int maxReadBufferSize = 64 * 1024;
        // Far larger than MaxReadBufferSize plus any kernel socket buffering on either side, so the upload
        // cannot finish while the app withholds reads.
        const int bodySize = 100 * 1024 * 1024;
        const int chunkSize = 4096;

        // Consider the upload stalled once no chunk has gone out for this long; poll ~10 times within that
        // window so a transient pause on a slow machine is not mistaken for a stall.
        var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
        var pollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);

        var handlerReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodyBytesRead = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(
            endpoint,
            async context =>
            {
                handlerReached.TrySetResult();

                // Withhold reads until the test releases us; the transport should stop draining the socket.
                await releaseRead.Task;

                long total = 0;
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await context.Request.Body.ReadAsync(buffer)) > 0)
                {
                    total += read;
                }
                bodyBytesRead.TrySetResult(total);

                await context.Response.WriteAsync("ok");
            },
            configureTransport: options => options.MaxReadBufferSize = maxReadBufferSize,
            configureKestrel: options =>
            {
                options.Limits.MinRequestBodyDataRate = null;
                options.Limits.MaxRequestBodySize = null;
            });

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);

        var requestHead = $"POST / HTTP/1.1\r\nHost: localhost\r\nContent-Length: {bodySize}\r\nConnection: close\r\n\r\n";
        await sslStream.WriteAsync(Encoding.ASCII.GetBytes(requestHead));
        await sslStream.FlushAsync();

        await handlerReached.Task.WaitAsync(Timeout);

        // Stream the body in small chunks, publishing how far we have gotten and when the last chunk went out
        // so the test thread can watch for the upload to stall.
        long bytesWritten = 0;
        long lastWriteTicks = DateTime.UtcNow.Ticks;

        var uploadTask = Task.Run(async () =>
        {
            var chunk = new byte[chunkSize];
            var sent = 0;
            while (sent < bodySize)
            {
                var size = Math.Min(bodySize - sent, chunkSize);
                await sslStream.WriteAsync(chunk.AsMemory(0, size));
                sent += size;
                Interlocked.Exchange(ref bytesWritten, sent);
                Volatile.Write(ref lastWriteTicks, DateTime.UtcNow.Ticks);
            }
            await sslStream.FlushAsync();
        });

        // The client must be able to push at least this much before the pipe pauses it. The transport input
        // pipe pauses at MaxReadBufferSize and Kestrel's own request buffer sits downstream, so the true pause
        // point is higher; this is a conservative lower bound.
        long minimumExpectedBytesWritten = maxReadBufferSize - chunkSize + 1;
        // The upload must pause before every byte is sent, otherwise no backpressure was applied.
        long maximumExpectedBytesWritten = bodySize - 1;

        // Wait until the upload has stalled (no progress for bytesWrittenTimeout) and has pushed at least the
        // minimum. A stall before the minimum may just be a slow machine, so keep waiting.
        while (!uploadTask.IsCompleted &&
               (DateTime.UtcNow.Ticks - Volatile.Read(ref lastWriteTicks) < bytesWrittenTimeout.Ticks ||
                Interlocked.Read(ref bytesWritten) < minimumExpectedBytesWritten))
        {
            await Task.Delay(pollingInterval);
        }

        // A faulted upload (for example a broken connection) should surface its own error rather than a range
        // mismatch below.
        if (uploadTask.IsFaulted)
        {
            await uploadTask;
        }

        Assert.InRange(Interlocked.Read(ref bytesWritten), minimumExpectedBytesWritten, maximumExpectedBytesWritten);
        Assert.False(uploadTask.IsCompleted);

        // Release the app; the upload now drains and completes with the full body observed.
        releaseRead.TrySetResult();

        await uploadTask.WaitAsync(Timeout);
        Assert.Equal(bodySize, await bodyBytesRead.Task.WaitAsync(Timeout));

        await host.StopAsync().WaitAsync(Timeout);
    }

    // Write-side backpressure: when the client stops reading the response, the transport's output pipe fills to
    // MaxWriteBufferSize and the send loop stalls, so the application's response write cannot complete. Once the
    // client drains the response the write completes and the whole payload is delivered.
    //
    // The socket suite has no write-side equivalent (that path is normally exercised by in-memory HTTP/2 and
    // HTTP/3 flow-control tests), so this mirrors the read-side approach in reverse: the app publishes how far
    // it has written and the test polls until the server's write stalls, rather than waiting a fixed delay.
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Response_AppliesWriteBackpressure_WhenClientStopsReading()
    {
        const int maxWriteBufferSize = 64 * 1024;
        // Far larger than MaxWriteBufferSize plus any kernel socket buffering on either side, so the response
        // cannot finish while the client withholds reads.
        const int responseSize = 100 * 1024 * 1024;
        const int chunkSize = 4096;

        // Consider the write stalled once no chunk has gone out for this long; poll ~10 times within that
        // window so a transient pause on a slow machine is not mistaken for a stall.
        var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
        var pollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);

        var responseWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Published by the app delegate as it writes, so the test thread can watch for the write to stall.
        long serverBytesWritten = 0;
        long lastWriteTicks = DateTime.UtcNow.Ticks;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();

        using var host = await StartHostAsync(
            endpoint,
            async context =>
            {
                context.Response.ContentLength = responseSize;

                var buffer = new byte[chunkSize];
                var remaining = responseSize;
                while (remaining > 0)
                {
                    var chunk = Math.Min(buffer.Length, remaining);
                    await context.Response.Body.WriteAsync(buffer.AsMemory(0, chunk));
                    remaining -= chunk;
                    Interlocked.Add(ref serverBytesWritten, chunk);
                    Volatile.Write(ref lastWriteTicks, DateTime.UtcNow.Ticks);
                }

                responseWritten.TrySetResult();
            },
            configureTransport: options => options.MaxWriteBufferSize = maxWriteBufferSize,
            configureKestrel: options => options.Limits.MinResponseDataRate = null);

        using var sslStream = await ConnectAsync(host.GetPort(), "localhost", applicationProtocols: [SslApplicationProtocol.Http11]);

        var request = "GET / HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n";
        await sslStream.WriteAsync(Encoding.ASCII.GetBytes(request));
        await sslStream.FlushAsync();

        // The app must be able to write at least this much before the output pipe pauses it. The transport
        // output pipe pauses at MaxWriteBufferSize and Kestrel's own response buffer sits upstream, so the true
        // pause point is higher; this is a conservative lower bound.
        long minimumExpectedBytesWritten = maxWriteBufferSize - chunkSize + 1;
        // The write must pause before the whole response is produced, otherwise no backpressure was applied.
        long maximumExpectedBytesWritten = (long)responseSize - 1;

        // Wait until the server's write has stalled (no progress for bytesWrittenTimeout) and has produced at
        // least the minimum. A stall before the minimum may just be a slow machine, so keep waiting.
        while (!responseWritten.Task.IsCompleted &&
               (DateTime.UtcNow.Ticks - Volatile.Read(ref lastWriteTicks) < bytesWrittenTimeout.Ticks ||
                Interlocked.Read(ref serverBytesWritten) < minimumExpectedBytesWritten))
        {
            await Task.Delay(pollingInterval);
        }

        Assert.InRange(Interlocked.Read(ref serverBytesWritten), minimumExpectedBytesWritten, maximumExpectedBytesWritten);
        Assert.False(responseWritten.Task.IsCompleted);

        // Drain the response; the server write now completes and the full payload is delivered.
        var totalRead = await DrainToEndAsync(sslStream);

        await responseWritten.Task.WaitAsync(Timeout);
        Assert.True(totalRead >= responseSize, $"Expected at least {responseSize} bytes, drained {totalRead}.");

        await host.StopAsync().WaitAsync(Timeout);
    }

    private async Task<IHost> StartHostAsync(
        DirectTlsEndpoint endpoint,
        RequestDelegate app,
        Action<DirectTlsTransportOptions>? configureTransport = null,
        Action<KestrelServerOptions>? configureKestrel = null)
    {
        var host = new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new TestLoggerProvider(TestSink));
                logging.AddFilter(
                    "Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls",
                    LogLevel.Debug);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();

                if (configureTransport is null)
                {
                    webHostBuilder.UseDirectTls();
                }
                else
                {
                    webHostBuilder.UseDirectTls(configureTransport);
                }

                webHostBuilder
                    .ConfigureKestrel(options =>
                    {
                        configureKestrel?.Invoke(options);
                        options.Listen(endpoint);
                    })
                    .Configure(appBuilder => appBuilder.Run(app));
            })
            .Build();

        await host.StartAsync().WaitAsync(Timeout);
        return host;
    }

    private async Task WaitForLogAsync(string eventName)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!TestSink.Writes.Any(write => write.EventId.Name == eventName))
        {
            await Task.Delay(10, cts.Token);
        }
    }

    private static async Task<SslStream> ConnectAsync(
        int port,
        string targetHost,
        Action<X509Certificate?>? captureServerCertificate = null,
        List<SslApplicationProtocol>? applicationProtocols = null,
        X509Certificate2? clientCertificate = null)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(IPAddress.Loopback, port);

        var sslStream = new SslStream(
            new NetworkStream(socket, ownsSocket: true),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (sender, certificate, chain, errors) =>
            {
                captureServerCertificate?.Invoke(certificate);
                return true;
            });

        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = targetHost,
        };

        if (applicationProtocols is not null)
        {
            clientOptions.ApplicationProtocols = applicationProtocols;
        }

        if (clientCertificate is not null)
        {
            clientOptions.ClientCertificates = [clientCertificate];
        }

        await sslStream.AuthenticateAsClientAsync(clientOptions).WaitAsync(Timeout);
        return sslStream;
    }

    private static async Task<string> SendRequestAsync(SslStream sslStream, string host)
    {
        var request = $"GET / HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n";
        await sslStream.WriteAsync(Encoding.ASCII.GetBytes(request));
        await sslStream.FlushAsync();

        using var reader = new StreamReader(sslStream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync().WaitAsync(Timeout);
    }

    private static async Task<long> DrainToEndAsync(SslStream sslStream)
    {
        long total = 0;
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await sslStream.ReadAsync(buffer).AsTask().WaitAsync(Timeout)) > 0)
        {
            total += read;
        }
        return total;
    }

    private sealed class HandshakeMetricCollector : IDisposable
    {
        private const string MeterName = "Microsoft.AspNetCore.Server.Kestrel";
        private const string ActiveHandshakesName = "kestrel.active_tls_handshakes";
        private const string HandshakeDurationName = "kestrel.tls_handshake.duration";

        private readonly MeterListener _listener = new();
        private readonly ConcurrentQueue<MetricMeasurement<long>> _activeHandshakes = new();
        private readonly ConcurrentQueue<MetricMeasurement<double>> _handshakeDurations = new();

        public HandshakeMetricCollector()
        {
            _listener.InstrumentPublished = static (instrument, listener) =>
            {
                if (instrument.Meter.Name == MeterName &&
                    instrument.Name is ActiveHandshakesName or HandshakeDurationName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
                _activeHandshakes.Enqueue(new MetricMeasurement<long>(value, tags.ToArray())));
            _listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
                _handshakeDurations.Enqueue(new MetricMeasurement<double>(value, tags.ToArray())));
            _listener.Start();
        }

        public IEnumerable<MetricMeasurement<long>> ActiveHandshakesForPort(int port)
            => _activeHandshakes.Where(measurement => measurement.GetTag("server.port") is int measuredPort && measuredPort == port);

        public IEnumerable<MetricMeasurement<double>> HandshakeDurationsForPort(int port)
            => _handshakeDurations.Where(measurement => measurement.GetTag("server.port") is int measuredPort && measuredPort == port);

        public async Task WaitForDurationAsync(int port)
        {
            using var cts = new CancellationTokenSource(Timeout);
            while (!HandshakeDurationsForPort(port).Any())
            {
                await Task.Delay(10, cts.Token);
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed class PausedConnectionMetricCollector : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly ConcurrentQueue<long> _measurements = new();

        public PausedConnectionMetricCollector()
        {
            _listener.InstrumentPublished = static (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.AspNetCore.Server.Kestrel" &&
                    instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, _, _) => _measurements.Enqueue(value));
            _listener.Start();
        }

        public async Task WaitForMeasurementAsync(long expected)
        {
            using var cts = new CancellationTokenSource(Timeout);
            while (!_measurements.Contains(expected))
            {
                await Task.Delay(10, cts.Token);
            }
        }

        public void Dispose() => _listener.Dispose();
    }

    private sealed class DirectTlsTelemetryListener : EventListener
    {
        private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new();
        private readonly ConcurrentDictionary<string, double> _counterValues = new();

        public DirectTlsTelemetryListener()
        {
            foreach (var eventSource in EventSource.GetSources())
            {
                EnableIfDirectTls(eventSource);
            }
        }

        public EventWrittenEventArgs[] Events => _events.ToArray();

        public async Task WaitForCounterAsync(string name, Func<double, bool> predicate)
        {
            using var cts = new CancellationTokenSource(Timeout);
            while (!_counterValues.TryGetValue(name, out var value) || !predicate(value))
            {
                await Task.Delay(10, cts.Token);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
            => EnableIfDirectTls(eventSource);

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "EventCounters" &&
                eventData.Payload?[0] is IDictionary<string, object> payload &&
                payload["Name"] is string counterName)
            {
                if (payload.TryGetValue("Mean", out var mean))
                {
                    _counterValues[counterName] = Convert.ToDouble(mean, CultureInfo.InvariantCulture);
                }
                else if (payload.TryGetValue("Increment", out var increment))
                {
                    _counterValues[counterName] = Convert.ToDouble(increment, CultureInfo.InvariantCulture);
                }

                return;
            }

            _events.Enqueue(eventData);
        }

        private void EnableIfDirectTls(EventSource eventSource)
        {
            if (eventSource.Name == DirectTlsEventSource.EventSourceName)
            {
                EnableEvents(
                    eventSource,
                    EventLevel.Informational,
                    EventKeywords.All,
                    new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "0.1" });
            }
        }
    }

    private sealed record MetricMeasurement<T>(
        T Value,
        KeyValuePair<string, object?>[] Tags)
    {
        public object? GetTag(string name)
            => Tags.FirstOrDefault(tag => tag.Key == name).Value;
    }
}
