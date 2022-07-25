// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class QuicConnectionListenerTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_AfterUnbind_Error()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        await connectionListener.UnbindAsync().DefaultTimeout();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connectionListener.AcceptAndAddFeatureAsync().AsTask()).DefaultTimeout();
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
        await using var serverConnection = await acceptTask.DefaultTimeout();
        Assert.False(serverConnection.ConnectionClosed.IsCancellationRequested);

        await serverConnection.DisposeAsync().AsTask().DefaultTimeout();

        // ConnectionClosed isn't triggered because the server initiated close.
        Assert.False(serverConnection.ConnectionClosed.IsCancellationRequested);
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
        using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/42389")]
    public async Task ClientCertificate_Required_NotSent_AcceptedViaCallback()
    {
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, clientCertificateRequired: true);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);
    }
}
