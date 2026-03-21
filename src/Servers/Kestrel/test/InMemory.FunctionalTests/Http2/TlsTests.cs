// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.Http2;

public class TlsTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    [ConditionalFact]
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.Linux, SkipReason = "TLS 1.1 ciphers are now disabled by default: https://github.com/dotnet/docs/issues/20842")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10,
        SkipReason = "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support or incompatible ciphers on Windows 8.1")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2,
        SkipReason = "Windows versions newer than 20H2 do not enable TLS 1.1: https://github.com/dotnet/aspnetcore/issues/37761")]
    public async Task TlsHandshakeRejectsTlsLessThan12()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = new TestServer(context =>
        {
            var tlsFeature = context.Features.Get<ITlsApplicationProtocolFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Equal(tlsFeature.ApplicationProtocol, SslApplicationProtocol.Http2.Protocol);

            return context.Response.WriteAsync("hello world " + context.Request.Protocol);
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)),
        listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
            listenOptions.UseHttps(_x509Certificate2, httpsOptions =>
            {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                httpsOptions.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039
            });
        }))
        {
            using (var connection = server.CreateConnection())
            {
                var sslStream = new SslStream(connection.Stream);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (_, __, ___, ____) => true,
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 },
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    EnabledSslProtocols = SslProtocols.Tls11, // Intentionally less than the required 1.2
#pragma warning restore SYSLIB0039
                }, CancellationToken.None);

                var reader = PipeReaderFactory.CreateFromStream(PipeOptions.Default, sslStream, CancellationToken.None);
                await WaitForConnectionErrorAsync(reader, ignoreNonGoAwayFrames: false, expectedLastStreamId: 0, expectedErrorCode: Http2ErrorCode.INADEQUATE_SECURITY);
                reader.Complete();
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.InsufficientTlsVersion, m.Tags));
    }

    private async Task WaitForConnectionErrorAsync(PipeReader reader, bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
    {
        var frame = await ReceiveFrameAsync(reader);

        if (ignoreNonGoAwayFrames)
        {
            while (frame.Type != Http2FrameType.GOAWAY)
            {
                frame = await ReceiveFrameAsync(reader);
            }
        }

        Assert.Equal(Http2FrameType.GOAWAY, frame.Type);
        Assert.Equal(8, frame.PayloadLength);
        Assert.Equal(0, frame.Flags);
        Assert.Equal(0, frame.StreamId);
        Assert.Equal(expectedLastStreamId, frame.GoAwayLastStreamId);
        Assert.Equal(expectedErrorCode, frame.GoAwayErrorCode);
    }

    private async Task<Http2Frame> ReceiveFrameAsync(PipeReader reader)
    {
        var frame = new Http2Frame();

        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.Start;

            try
            {
                if (Http2FrameReader.TryReadFrame(ref buffer, frame, 16_384, out var framePayload))
                {
                    consumed = examined = framePayload.End;
                    return frame;
                }
                else
                {
                    examined = buffer.End;
                }

                if (result.IsCompleted)
                {
                    throw new IOException("The reader completed without returning a frame.");
                }
            }
            finally
            {
                reader.AdvanceTo(consumed, examined);
            }
        }
    }
}
