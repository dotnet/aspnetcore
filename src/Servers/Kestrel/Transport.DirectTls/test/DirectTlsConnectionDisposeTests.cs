// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for <see cref="DirectTlsConnection.DisposeAsync"/> teardown that has to survive application code.
/// Cancelling ConnectionClosed runs whatever the application registered on it,
/// and the steps after it release resources the process does not get back on its own - most importantly the
/// accepted client certificate, which owns a native key handle the transport is solely responsible for freeing.
/// Linux-only, matching the rest of the DirectTls suite.
/// </summary>
public class DirectTlsConnectionDisposeTests
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task DisposeAsync_ConnectionClosedCallbackThrows_StillDisposesClientCertificate()
    {
        using var pump = new TlsEventPump(NullLogger<TlsEventPump>.Instance, id: 0, Timeout.InfiniteTimeSpan);
        var certificate = CreateSelfSignedCertificate();
        var connection = CreateConnection(pump, certificate);

        connection.ConnectionClosed.Register(static () => throw new InvalidOperationException("ConnectionClosed callback failed."));

        // The throwing callback must not escape and abandon the rest of the teardown.
        await connection.DisposeAsync();

        Assert.True(connection.ConnectionClosed.IsCancellationRequested);

        // A disposed X509Certificate2 reports a null handle. Reaching this means disposal continued past the
        // failing callback, so the native key handle is released instead of leaking once per accepted mTLS
        // connection.
        Assert.Equal(IntPtr.Zero, certificate.Handle);
    }

    private static DirectTlsConnection CreateConnection(TlsEventPump pump, X509Certificate2 clientCertificate)
    {
        // The session is only reached by the graceful-shutdown step, which is already guarded, so the fd and
        // session can stay fake: this test is about the ordering of the steps that follow the callback.
        var connectionState = new ConnectionIoState(
            fd: 101,
            session: null!,
            NullLogger<ConnectionIoState>.Instance);

        return new DirectTlsConnection(
            connectionState,
            pump,
            localEndPoint: null,
            remoteEndPoint: null,
            MemoryPool<byte>.Shared,
            maxReadBufferSize: 0,
            maxWriteBufferSize: 0,
            NullLogger<DirectTlsConnection>.Instance,
            clientCertificate: clientCertificate);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=directtls-test-client", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = DateTimeOffset.UtcNow;
        return request.CreateSelfSigned(now.AddDays(-1), now.AddDays(1));
    }
}
