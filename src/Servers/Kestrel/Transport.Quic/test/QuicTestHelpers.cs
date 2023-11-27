// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

internal static class QuicTestHelpers
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    public static QuicTransportFactory CreateTransportFactory(
        ILoggerFactory loggerFactory = null,
        TimeProvider timeProvider = null,
        long defaultCloseErrorCode = 0)
    {
        var quicTransportOptions = new QuicTransportOptions();
        quicTransportOptions.MaxBidirectionalStreamCount = 200;
        quicTransportOptions.MaxUnidirectionalStreamCount = 200;
        quicTransportOptions.DefaultCloseErrorCode = defaultCloseErrorCode;
        if (timeProvider != null)
        {
            quicTransportOptions.TimeProvider = timeProvider;
        }

        return new QuicTransportFactory(loggerFactory ?? NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
    }

    public static async Task<QuicConnectionListener> CreateConnectionListenerFactory(
        ILoggerFactory loggerFactory = null,
        TimeProvider timeProvider = null,
        bool clientCertificateRequired = false,
        long defaultCloseErrorCode = 0,
        int port = 0)
    {
        var transportFactory = CreateTransportFactory(
            loggerFactory,
            timeProvider,
            defaultCloseErrorCode: defaultCloseErrorCode);

        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

        var features = CreateBindAsyncFeatures(clientCertificateRequired);
        return (QuicConnectionListener)await transportFactory.BindAsync(endpoint, features, cancellationToken: CancellationToken.None);
    }

    public static async Task<QuicConnectionListener> CreateConnectionListenerFactory(
        TlsConnectionCallbackOptions tlsConnectionOptions,
        ILoggerFactory loggerFactory = null,
        TimeProvider timeProvider = null,
        int port = 0)
    {
        var transportFactory = CreateTransportFactory(loggerFactory, timeProvider);

        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        var features = new FeatureCollection();
        features.Set(tlsConnectionOptions);
        return (QuicConnectionListener)await transportFactory.BindAsync(endpoint, features, cancellationToken: CancellationToken.None);
    }

    public static FeatureCollection CreateBindAsyncFeatures(bool clientCertificateRequired = false)
    {
        var cert = TestResources.GetTestCertificate();

        var sslServerAuthenticationOptions = new SslServerAuthenticationOptions();
        sslServerAuthenticationOptions.ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http3 };
        sslServerAuthenticationOptions.ServerCertificate = cert;
        sslServerAuthenticationOptions.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;
        sslServerAuthenticationOptions.ClientCertificateRequired = clientCertificateRequired;

        var features = new FeatureCollection();
        features.Set(new TlsConnectionCallbackOptions
        {
            ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols,
            OnConnection = (context, cancellationToken) => ValueTask.FromResult(sslServerAuthenticationOptions)
        });

        return features;
    }

    public static async ValueTask<MultiplexedConnectionContext> AcceptAndAddFeatureAsync(this IMultiplexedConnectionListener listener)
    {
        var connection = await listener.AcceptAsync();
        connection?.Features.Set<IConnectionHeartbeatFeature>(new TestConnectionHeartbeatFeature());
        return connection;
    }

    private class TestConnectionHeartbeatFeature : IConnectionHeartbeatFeature
    {
        public void OnHeartbeat(Action<object> action, object state)
        {
        }
    }

    private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    public static QuicClientConnectionOptions CreateClientConnectionOptions(EndPoint remoteEndPoint, bool? ignoreInvalidCertificate = null)
    {
        var options = new QuicClientConnectionOptions
        {
            MaxInboundBidirectionalStreams = 200,
            MaxInboundUnidirectionalStreams = 200,
            RemoteEndPoint = remoteEndPoint,
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    SslApplicationProtocol.Http3
                }
            },
            DefaultStreamErrorCode = 0,
            DefaultCloseErrorCode = 0,
        };
        if (ignoreInvalidCertificate ?? true)
        {
            options.ClientAuthenticationOptions.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;
        }
        return options;
    }

    public static async Task<QuicStreamContext> CreateAndCompleteBidirectionalStreamGracefully(QuicConnection clientConnection, MultiplexedConnectionContext serverConnection, ILogger logger)
    {
        logger.LogInformation("Client starting stream.");
        var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

        logger.LogInformation("Client sending data.");
        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

        logger.LogInformation("Server accepting stream.");
        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();

        logger.LogInformation("Server reading data.");
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        // Input should be completed.
        readResult = await serverStream.Transport.Input.ReadAsync();
        Assert.True(readResult.IsCompleted);

        // Complete reading and writing.
        logger.LogInformation("Server completing input and output.");
        await serverStream.Transport.Input.CompleteAsync();
        await serverStream.Transport.Output.CompleteAsync();

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Both send and receive loops have exited.
        logger.LogInformation("Server verifying stream is finished.");
        await quicStreamContext._processingTask.DefaultTimeout();
        Assert.True(quicStreamContext.CanWrite);
        Assert.True(quicStreamContext.CanRead);

        logger.LogInformation("Server disposing stream.");
        await quicStreamContext.DisposeAsync();
        quicStreamContext.Dispose();

        return quicStreamContext;
    }
}
