// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class QuicConnectionListenerTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_AfterUnbind_ReturnNull()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        await connectionListener.UnbindAsync().DefaultTimeout();

        // Assert
        Assert.Null(await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout());
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientCreatesConnection_ServerAccepts()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        // Assert
        var serverConnection = await acceptTask.DefaultTimeout();
        Assert.False(serverConnection.ConnectionClosed.IsCancellationRequested);

        await serverConnection.DisposeAsync().AsTask().DefaultTimeout();

        // ConnectionClosed isn't triggered because the server initiated close.
        Assert.False(serverConnection.ConnectionClosed.IsCancellationRequested);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientCreatesInvalidConnection_ServerContinuesToAccept()
    {
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act & Assert 1
        Logger.LogInformation("Client creating successful connection 1");
        var acceptTask1 = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();
        await using var clientConnection1 = await QuicConnection.ConnectAsync(
            QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint));

        var serverConnection1 = await acceptTask1.DefaultTimeout();
        Assert.False(serverConnection1.ConnectionClosed.IsCancellationRequested);
        await serverConnection1.DisposeAsync().AsTask().DefaultTimeout();

        // Act & Assert 2
        var serverFailureLogTask = WaitForLogMessage(m => m.EventId.Name == "ConnectionListenerAcceptConnectionFailed");

        Logger.LogInformation("Client creating unsuccessful connection 2");
        var acceptTask2 = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();
        var ex = await Assert.ThrowsAsync<AuthenticationException>(async () =>
        {
            await QuicConnection.ConnectAsync(
                QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint, ignoreInvalidCertificate: false));
        });
        Assert.Contains("RemoteCertificateChainErrors", ex.Message);

        Assert.False(acceptTask2.IsCompleted, "Accept doesn't return for failed client connection.");
        var serverFailureLog = await serverFailureLogTask.DefaultTimeout();
        Assert.NotNull(serverFailureLog.Exception);

        // Act & Assert 3
        Logger.LogInformation("Client creating successful connection 3");
        await using var clientConnection2 = await QuicConnection.ConnectAsync(
            QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint));

        var serverConnection2 = await acceptTask2.DefaultTimeout();
        Assert.False(serverConnection2.ConnectionClosed.IsCancellationRequested);
        await serverConnection2.DisposeAsync().AsTask().DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2,
        SkipReason = "Windows versions newer than 20H2 do not enable TLS 1.1: https://github.com/dotnet/aspnetcore/issues/37761")]
    public async Task ClientCertificate_Required_Sent_Populated()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, clientCertificateRequired: true);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        var testCert = TestResources.GetTestCertificate();
        options.ClientAuthenticationOptions.ClientCertificates = new X509CertificateCollection { testCert };

        // Act
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();
        // Server waits for stream from client
        var serverStreamTask = serverConnection.AcceptAsync().DefaultTimeout();

        // Client creates stream
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        // Server finishes accepting
        var serverStream = await serverStreamTask.DefaultTimeout();

        // Assert
        AssertTlsConnectionFeature(serverConnection.Features, testCert);
        AssertTlsConnectionFeature(serverStream.Features, testCert);

        static void AssertTlsConnectionFeature(IFeatureCollection features, X509Certificate2 testCert)
        {
            var tlsFeature = features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.NotNull(tlsFeature.ClientCertificate);
            Assert.Equal(testCert, tlsFeature.ClientCertificate);
        }
    }

    [ConditionalFact]
    [MsQuicSupported]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task ClientCertificate_Required_NotSent_AcceptedViaCallback()
    {
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, clientCertificateRequired: true);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_NoCertificateOrApplicationProtocol_Log()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) =>
                {
                    var options = new SslServerAuthenticationOptions();
                    options.ApplicationProtocols = new List<SslApplicationProtocol>();
                    return ValueTask.FromResult(options);
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await Assert.ThrowsAsync<AuthenticationException>(() => QuicConnection.ConnectAsync(options).AsTask());

        // Assert
        Assert.Contains(LogMessages, m => m.EventId.Name == "ConnectionListenerCertificateNotSpecified");
        Assert.Contains(LogMessages, m => m.EventId.Name == "ConnectionListenerApplicationProtocolsNotSpecified");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_UnbindAfterCall_CleanExitAndLog()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        await connectionListener.UnbindAsync().DefaultTimeout();

        // Assert
        Assert.Null(await acceptTask.AsTask().DefaultTimeout());

        Assert.Contains(LogMessages, m => m.EventId.Name == "ConnectionListenerAborted");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_DisposeAfterCall_CleanExitAndLog()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        await connectionListener.DisposeAsync().DefaultTimeout();

        // Assert
        Assert.Null(await acceptTask.AsTask().DefaultTimeout());

        Assert.Contains(LogMessages, m => m.EventId.Name == "ConnectionListenerAborted");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ErrorFromServerCallback_CleanExitAndLog()
    {
        // Arrange
        var throwErrorInCallback = true;
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) =>
                {
                    if (throwErrorInCallback)
                    {
                        throwErrorInCallback = false;
                        throw new Exception("An error!");
                    }

                    var options = new SslServerAuthenticationOptions();
                    options.ServerCertificate = TestResources.GetTestCertificate();
                    return ValueTask.FromResult(options);
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => QuicConnection.ConnectAsync(options).AsTask()).DefaultTimeout();
        Assert.Equal("Authentication failed because the remote party sent a TLS alert: 'UserCanceled'.", ex.Message);

        // Assert
        Assert.False(acceptTask.IsCompleted, "Still waiting for non-errored connection.");

        await using var clientConnection = await QuicConnection.ConnectAsync(options).DefaultTimeout();
        await using var serverConnection = await acceptTask.DefaultTimeout();

        Assert.NotNull(serverConnection);
        Assert.NotNull(clientConnection);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BindAsync_ListenersSharePort_ThrowAddressInUse()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act & Assert
        var port = ((IPEndPoint)connectionListener.EndPoint).Port;

        await Assert.ThrowsAsync<AddressInUseException>(() => QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, port: port));
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BindAsync_ListenersSharePortWithPlainUdpSocket_ThrowAddressInUse()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        using var socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endpoint);

        // Act & Assert
        var port = ((IPEndPoint)socket.LocalEndPoint).Port;

        await Assert.ThrowsAsync<AddressInUseException>(() => QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, port: port));
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_NoApplicationProtocolsInCallback_DefaultToConnectionProtocols()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) =>
                {
                    var options = new SslServerAuthenticationOptions();
                    options.ServerCertificate = TestResources.GetTestCertificate();
                    return ValueTask.FromResult(options);
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options).DefaultTimeout();
        await using var serverConnection = await acceptTask.DefaultTimeout();

        // Assert
        Assert.Equal(SslApplicationProtocol.Http3, clientConnection.NegotiatedApplicationProtocol);
        Assert.NotNull(serverConnection);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_Success_RemovedFromPendingConnections()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = async (context, cancellationToken) =>
                {
                    await syncPoint.WaitToContinue();

                    var options = new SslServerAuthenticationOptions();
                    options.ServerCertificate = TestResources.GetTestCertificate();
                    return options;
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        var clientConnectionTask = QuicConnection.ConnectAsync(options);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();
        Assert.Single(connectionListener._pendingConnections);

        syncPoint.Continue();

        await using var serverConnection = await acceptTask.DefaultTimeout();
        await using var clientConnection = await clientConnectionTask.DefaultTimeout();

        // Assert
        Assert.NotNull(serverConnection);
        Assert.NotNull(clientConnection);
        Assert.Empty(connectionListener._pendingConnections);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_NoCertificateCallback_RemovedFromPendingConnections()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = async (context, cancellationToken) =>
                {
                    await syncPoint.WaitToContinue();

                    // Options are invalid and S.N.Q will throw an error from AcceptConnectionAsync.
                    return new SslServerAuthenticationOptions();
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        var clientConnectionTask = QuicConnection.ConnectAsync(options);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();
        Assert.Single(connectionListener._pendingConnections);

        syncPoint.Continue();

        await Assert.ThrowsAsync<AuthenticationException>(() => clientConnectionTask.AsTask()).DefaultTimeout();
        Assert.False(acceptTask.IsCompleted);

        // Assert
        for (var i = 0; i < 20; i++)
        {
            // Wait until msquic and S.N.Q have finished with QuicConnection and verify it's removed from CWT.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (connectionListener._pendingConnections.Count() == 0)
            {
                return;
            }

            await Task.Delay(100 * i);
        }

        throw new Exception("Connection not removed from CWT.");
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_TlsCallback_ConnectionContextInArguments()
    {
        // Arrange
        BaseConnectionContext connectionContext = null;
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) =>
                {
                    var options = new SslServerAuthenticationOptions();
                    options.ServerCertificate = TestResources.GetTestCertificate();

                    connectionContext = context.Connection;

                    return ValueTask.FromResult(options);
                }
            },
            LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options).DefaultTimeout();

        // Assert
        Assert.NotNull(connectionContext);
    }
}
