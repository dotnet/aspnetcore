// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

// DirectTls terminates TLS through the runtime's native, file-descriptor-bound OpenSSL session, which is
// Linux-only. These end-to-end tests start a real Kestrel host on the DirectTls transport and drive it with
// a standard SslStream client.
public class DirectTlsFunctionalTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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
    public async Task ClientCertificate_RequiredAndValidated_AllowsRequest()
    {
        var validationInvoked = false;

        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
        endpoint.Options.ServerCertificate = TestResources.GetTestCertificate();
        endpoint.Options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        endpoint.Options.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            validationInvoked = true;
            return true;
        };

        using var host = await StartHostAsync(endpoint, context => context.Response.WriteAsync("ok"));

        var clientCertificate = TestResources.GetTestCertificate("eku.client.pfx");
        using var sslStream = await ConnectAsync(
            host.GetPort(),
            "localhost",
            applicationProtocols: [SslApplicationProtocol.Http11],
            clientCertificate: clientCertificate);

        var response = await SendRequestAsync(sslStream, "localhost");

        Assert.Contains("200 OK", response);
        Assert.True(validationInvoked);

        await host.StopAsync().WaitAsync(Timeout);
    }

    private static async Task<IHost> StartHostAsync(DirectTlsEndpoint endpoint, RequestDelegate app)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseDirectTlsTransport()
                    .ConfigureKestrel(options => options.Listen(endpoint))
                    .Configure(appBuilder => appBuilder.Run(app));
            })
            .Build();

        await host.StartAsync().WaitAsync(Timeout);
        return host;
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
}
