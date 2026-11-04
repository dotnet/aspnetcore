// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests.Http2;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.Http2;
#endif

public class HandshakeTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    public HttpClient Client { get; set; }

    public HandshakeTests()
    {
        Client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            DefaultRequestVersion = HttpVersion.Version20,
        };
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
    // Win7 SslStream is missing ALPN support.
    public void TlsAndHttp2NotSupportedOnWin7()
    {
        var ex = Assert.Throws<NotSupportedException>(() => new TestServer(context =>
        {
            throw new NotImplementedException();
        }, new TestServiceContext(LoggerFactory),
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        }));

        Assert.Equal("HTTP/2 over TLS is not supported on Windows 7 due to missing ALPN support.", ex.Message);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task TlsAlpnHandshakeSelectsHttp2From1and2()
    {
        await using (var server = new TestServer(context =>
        {
            var tlsFeature = context.Features.Get<ITlsApplicationProtocolFeature>();
            Assert.NotNull(tlsFeature);
            Assert.True(SslApplicationProtocol.Http2.Protocol.Span.SequenceEqual(tlsFeature.ApplicationProtocol.Span),
                "ALPN: " + tlsFeature.ApplicationProtocol.Length);

            return context.Response.WriteAsync("hello world " + context.Request.Protocol);
        }, new TestServiceContext(LoggerFactory),
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        }))
        {
            var result = await Client.GetStringAsync($"https://localhost:{server.Port}/");
            Assert.Equal("hello world HTTP/2", result);
        }
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task TlsAlpnHandshakeSelectsHttp2()
    {
        await using (var server = new TestServer(context =>
        {
            var tlsFeature = context.Features.Get<ITlsApplicationProtocolFeature>();
            Assert.NotNull(tlsFeature);
            Assert.True(SslApplicationProtocol.Http2.Protocol.Span.SequenceEqual(tlsFeature.ApplicationProtocol.Span),
                "ALPN: " + tlsFeature.ApplicationProtocol.Length);

            return context.Response.WriteAsync("hello world " + context.Request.Protocol);
        }, new TestServiceContext(LoggerFactory),
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        }))
        {
            var result = await Client.GetStringAsync($"https://localhost:{server.Port}/");
            Assert.Equal("hello world HTTP/2", result);
        }
    }
}
