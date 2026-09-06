// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPNETCORE_DIRECTTLS_001 // Experimental API

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

/// <summary>
/// Covers the guarantee that user-supplied handshake callbacks - the server-certificate selector, the
/// ClientHello listener and client-certificate validation - never run on a pump's epoll thread. Every test pins the transport to a single pump
/// (<c>WorkerCount = 1</c>) so all connections are provably owned by the same event loop: if a callback were
/// still invoked inline, one slow or throwing connection would stall or break every other connection here.
/// </summary>
public class DirectTlsUserCallbackDispatchTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    // Deliberately much shorter than Timeout: this is what "the pump is not stalled" means. A connection that
    // has to wait for the blocked callback would need the full release delay, which never arrives on its own.
    private static readonly TimeSpan ProgressTimeout = TimeSpan.FromSeconds(15);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BlockingCertificateSelector_DoesNotStallHandshakesOnTheSamePump()
    {
        var certificate = TestResources.GetTestCertificate();
        using var selectorEntered = new SemaphoreSlim(0);
        using var releaseSelector = new ManualResetEventSlim(false);
        var blockedOnce = 0;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificateSelector = (connection, hostName) =>
        {
            // Only the first connection blocks; every later one resolves immediately.
            if (Interlocked.Exchange(ref blockedOnce, 1) == 0)
            {
                selectorEntered.Release();
                releaseSelector.Wait(Timeout);
            }

            return certificate;
        };

        using var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);
        var port = host.GetPort();

        try
        {
            var blockedHandshake = ConnectAsync(port);
            Assert.True(await selectorEntered.WaitAsync(Timeout), "The certificate selector was never entered.");

            // The pump that owns the blocked handshake also owns this one. It must accept it and drive its
            // handshake to completion while the first connection's user code is still parked.
            using var progressing = await ConnectAsync(port).WaitAsync(ProgressTimeout);
            Assert.Contains("200 OK", await SendRequestAsync(progressing));

            releaseSelector.Set();

            // The parked handshake resumes on the pump once its callback returns.
            using var resumed = await blockedHandshake.WaitAsync(Timeout);
            Assert.Contains("200 OK", await SendRequestAsync(resumed));
        }
        finally
        {
            releaseSelector.Set();
        }

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BlockingClientHelloCallback_DoesNotStallHandshakesOnTheSamePump()
    {
        using var callbackEntered = new SemaphoreSlim(0);
        using var releaseCallback = new ManualResetEventSlim(false);
        var blockedOnce = 0;
        var observedClientHelloLength = 0L;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.TlsClientHelloBytesCallback = (connection, clientHelloBytes) =>
        {
            Interlocked.Exchange(ref observedClientHelloLength, clientHelloBytes.Length);

            if (Interlocked.Exchange(ref blockedOnce, 1) == 0)
            {
                callbackEntered.Release();
                releaseCallback.Wait(Timeout);
            }
        };

        using var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);
        var port = host.GetPort();

        try
        {
            var blockedHandshake = ConnectAsync(port);
            Assert.True(await callbackEntered.WaitAsync(Timeout), "The ClientHello callback was never entered.");

            using var progressing = await ConnectAsync(port).WaitAsync(ProgressTimeout);
            Assert.Contains("200 OK", await SendRequestAsync(progressing));

            releaseCallback.Set();

            using var resumed = await blockedHandshake.WaitAsync(Timeout);
            Assert.Contains("200 OK", await SendRequestAsync(resumed));
        }
        finally
        {
            releaseCallback.Set();
        }

        // The callback still sees the real ClientHello record even though it now runs off the pump against a
        // copy taken there.
        Assert.True(Interlocked.Read(ref observedClientHelloLength) > 0, "The ClientHello callback saw no bytes.");

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ThrowingCertificateSelector_FailsOnlyThatConnection()
    {
        var certificate = TestResources.GetTestCertificate();
        var throwOnce = 1;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificateSelector = (connection, hostName) =>
            Interlocked.Exchange(ref throwOnce, 0) == 1
                ? throw new InvalidOperationException("Certificate selection failed.")
                : certificate;

        using var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);
        var port = host.GetPort();

        // The exception escaped user code on a thread pool thread; it must fail this handshake rather than
        // reach the pump (which would take the process, and every other connection, down with it).
        await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(port).WaitAsync(ProgressTimeout));

        using var healthy = await ConnectAsync(port).WaitAsync(ProgressTimeout);
        Assert.Contains("200 OK", await SendRequestAsync(healthy));

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ThrowingClientHelloCallback_FailsOnlyThatConnection()
    {
        var throwOnce = 1;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.TlsClientHelloBytesCallback = (connection, clientHelloBytes) =>
        {
            if (Interlocked.Exchange(ref throwOnce, 0) == 1)
            {
                throw new InvalidOperationException("ClientHello inspection failed.");
            }
        };

        using var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);
        var port = host.GetPort();

        await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(port).WaitAsync(ProgressTimeout));

        using var healthy = await ConnectAsync(port).WaitAsync(ProgressTimeout);
        Assert.Contains("200 OK", await SendRequestAsync(healthy));

        await host.StopAsync().WaitAsync(Timeout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BlockingClientCertificateValidation_DoesNotStallHandshakesOnTheSamePump()
    {
        // Client-certificate validation is the third suspension site, and the odd one out: it runs after the
        // handshake already reported Complete, so the blocked connection is one the pump has not yet surfaced
        // to Kestrel rather than one still negotiating. Parking it must not stop the pump from driving other
        // connections to a served request.
        var blockedOnce = 0;
        string validationThreadName = null;
        using var validationEntered = new SemaphoreSlim(0);
        using var releaseValidation = new ManualResetEventSlim(false);

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        endpoint.Options.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            // Only the first connection blocks; everything after it validates immediately.
            if (Interlocked.Exchange(ref blockedOnce, 1) == 0)
            {
                // Published before the release below, so the test thread observes it after the wait.
                validationThreadName = Thread.CurrentThread.Name;
                validationEntered.Release();
                releaseValidation.Wait(Timeout);
            }

            return true;
        };

        try
        {
            using var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);
            var port = host.GetPort();
            var clientCertificate = TestResources.GetTestCertificate("eku.client.pfx");

            // The TLS handshake can finish on the client before the server runs validation, so this connect may
            // complete while the server side is still parked. Either way the request cannot be served until the
            // callback returns, so the response is only read after the release below.
            var blocked = ConnectAsync(port, clientCertificate);
            Assert.True(await validationEntered.WaitAsync(Timeout));

            // The pump owns both connections (WorkerCount = 1). If validation were still inline, this second
            // handshake could not even start, let alone be served.
            using var progressing = await ConnectAsync(port, clientCertificate).WaitAsync(ProgressTimeout);
            Assert.Contains("200 OK", await SendRequestAsync(progressing));

            releaseValidation.Set();

            using var resumed = await blocked.WaitAsync(Timeout);
            Assert.Contains("200 OK", await SendRequestAsync(resumed));

            // Pump threads are named TlsEventPump-{id}; the callback must have run somewhere else.
            Assert.DoesNotContain("TlsEventPump-", validationThreadName ?? string.Empty);

            await host.StopAsync().WaitAsync(Timeout);
        }
        finally
        {
            releaseValidation.Set();
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ShutdownWhileCallbackIsParked_CompletesWithoutLeakingTheConnection()
    {
        var certificate = TestResources.GetTestCertificate();
        using var selectorEntered = new SemaphoreSlim(0);
        using var releaseSelector = new ManualResetEventSlim(false);

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificateSelector = (connection, hostName) =>
        {
            selectorEntered.Release();
            releaseSelector.Wait(Timeout);
            return certificate;
        };

        var host = await StartHostAsync(endpoint, options => options.WorkerCount = 1);

        try
        {
            _ = ConnectAsync(host.GetPort());
            Assert.True(await selectorEntered.WaitAsync(Timeout), "The certificate selector was never entered.");

            // Tear the server down while the handshake is parked on user code. The pump loop must exit, and the
            // parked handshake's resources must be released once its callback reports back - without the resume
            // path touching a session that shutdown already disposed.
            var stop = host.StopAsync();
            releaseSelector.Set();
            await stop.WaitAsync(Timeout);
        }
        finally
        {
            releaseSelector.Set();
            host.Dispose();
        }
    }

    private static async Task<IHost> StartHostAsync(DirectTlsEndpoint endpoint, Action<DirectTlsTransportOptions> configureTransport)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseDirectTls(configureTransport)
                    .ConfigureKestrel(options => options.Listen(endpoint))
                    .Configure(appBuilder => appBuilder.Run(context => context.Response.WriteAsync("ok")));
            })
            .Build();

        await host.StartAsync().WaitAsync(Timeout);
        return host;
    }

    private static async Task<SslStream> ConnectAsync(int port, X509Certificate2 clientCertificate = null)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(IPAddress.Loopback, port);

        var sslStream = new SslStream(
            new NetworkStream(socket, ownsSocket: true),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (sender, certificate, chain, errors) => true);

        try
        {
            var clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                ApplicationProtocols = [SslApplicationProtocol.Http11],
            };

            if (clientCertificate is not null)
            {
                clientOptions.ClientCertificates = [clientCertificate];
            }

            await sslStream.AuthenticateAsClientAsync(clientOptions);
        }
        catch
        {
            await sslStream.DisposeAsync();
            throw;
        }

        return sslStream;
    }

    private static async Task<string> SendRequestAsync(SslStream sslStream)
    {
        var request = "GET / HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n";
        await sslStream.WriteAsync(Encoding.ASCII.GetBytes(request));
        await sslStream.FlushAsync();

        using var reader = new StreamReader(sslStream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync().WaitAsync(ProgressTimeout);
    }
}
