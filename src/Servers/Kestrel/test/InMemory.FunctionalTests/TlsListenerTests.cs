// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace InMemory.FunctionalTests;

public class TlsListenerTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    [Fact]
    public async Task TlsClientHelloBytesCallback_InvokedAndHasTlsMessageBytes()
    {
        var tlsClientHelloCallbackInvoked = false;

        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            listenOptions =>
            {
                listenOptions.UseHttps(_x509Certificate2, httpsOptions =>
                {
                    httpsOptions.TlsClientHelloBytesCallback = (connection, clientHelloBytes) =>
                    {
                        Logger.LogDebug("[Received TlsClientHelloBytesCallback] Connection: {0}; TLS client hello buffer: {1}", connection.ConnectionId, clientHelloBytes.Length);
                        tlsClientHelloCallbackInvoked = true;
                        Assert.True(clientHelloBytes.Length > 32);
                        Assert.NotNull(connection);
                    };
                });
            }))
        {
            using (var connection = server.CreateConnection())
            {
                using (var sslStream = new SslStream(connection.Stream, false, (sender, cert, chain, errors) => true, null))
                {
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = "localhost",
                        EnabledSslProtocols = SslProtocols.None
                    }, CancellationToken.None);

                    var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                    await sslStream.WriteAsync(request, 0, request.Length);
                    await sslStream.ReadAsync(new Memory<byte>(new byte[1024]));
                }
            }
        }

        Assert.True(tlsClientHelloCallbackInvoked);
    }

    [Fact]
    public async Task TlsClientHelloBytesCallback_UsesOptionsTimeout()
    {
        var tlsClientHelloCallbackInvoked = false;
        var testContext = new TestServiceContext(LoggerFactory);
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            listenOptions =>
            {
                listenOptions.UseHttps(_x509Certificate2, httpsOptions =>
                {
                    httpsOptions.HandshakeTimeout = TimeSpan.FromMilliseconds(1);

                    httpsOptions.TlsClientHelloBytesCallback = (connection, clientHelloBytes) =>
                    {
                        Logger.LogDebug("[Received TlsClientHelloBytesCallback] Connection: {0}; TLS client hello buffer: {1}", connection.ConnectionId, clientHelloBytes.Length);
                        tlsClientHelloCallbackInvoked = true;
                        Assert.True(clientHelloBytes.Length > 32);
                        Assert.NotNull(connection);
                    };
                });
            }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.Input.WriteAsync(new byte[] { 0x16 });
                var readResult = await connection.TransportConnection.Output.ReadAsync();

                // HttpsConnectionMiddleware catches the exception, so we can only check the effects of the timeout here
                Assert.True(readResult.IsCompleted);
            }
        }

        Assert.False(tlsClientHelloCallbackInvoked);
    }
}
