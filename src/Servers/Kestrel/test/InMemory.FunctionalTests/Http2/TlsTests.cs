// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.Http2
{
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81,
        SkipReason = "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support")]
    public class TlsTests : LoggedTest
    {
        private static X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/7000")]
        public async Task TlsHandshakeRejectsTlsLessThan12()
        {
            using (var server = new TestServer(context =>
            {
                var tlsFeature = context.Features.Get<ITlsApplicationProtocolFeature>();
                Assert.NotNull(tlsFeature);
                Assert.Equal(tlsFeature.ApplicationProtocol, SslApplicationProtocol.Http2.Protocol);

                return context.Response.WriteAsync("hello world " + context.Request.Protocol);
            },
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2, httpsOptions =>
                {
                    httpsOptions.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
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
                        EnabledSslProtocols = SslProtocols.Tls11, // Intentionally less than the required 1.2
                    }, CancellationToken.None);

                    var reader = PipeReaderFactory.CreateFromStream(PipeOptions.Default, sslStream, CancellationToken.None);
                    await WaitForConnectionErrorAsync(reader, ignoreNonGoAwayFrames: false, expectedLastStreamId: 0, expectedErrorCode: Http2ErrorCode.INADEQUATE_SECURITY);
                    reader.Complete();
                }
                await server.StopAsync();
            }
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
}
