// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.UserCallbacks;

/// <summary>
/// The <c>NeedsTlsContext</c> suspension: the optional ClientHello listener followed by the server-certificate
/// selector.
/// </summary>
internal sealed class ResolveTlsContextCallback : HandshakeUserCallback
{
    private readonly Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)> _contextResolver;
    private readonly Action<ConnectionContext, ReadOnlySequence<byte>>? _clientHelloCallback;
    private readonly string? _targetHostName;

    // ClientHello record copied out of the session on the pump thread. Rented from the shared pool and
    // returned as soon as the (synchronous) user callback returns, matching the transient-buffer contract of
    // the socket-transport TlsListener.
    private byte[]? _clientHelloBuffer;
    private readonly int _clientHelloLength;

    /// <summary>
    /// Creates the work item for the <c>NeedsTlsContext</c> suspension. <paramref name="clientHelloBuffer"/>
    /// holds the ClientHello record already copied out of the session on the pump thread (null when there is
    /// no listener, or nothing was captured); this work item returns it to <see cref="ArrayPool{T}.Shared"/>
    /// once the listener has run.
    /// </summary>
    public ResolveTlsContextCallback(
        TlsEventPump pump,
        int fd,
        DirectTlsConnection? connection,
        string? targetHostName,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)> contextResolver,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback,
        byte[]? clientHelloBuffer,
        int clientHelloLength)
        : base(pump, fd, connection)
    {
        _targetHostName = targetHostName;
        _contextResolver = contextResolver;
        _clientHelloCallback = clientHelloCallback;
        _clientHelloBuffer = clientHelloBuffer;
        _clientHelloLength = clientHelloLength;
    }

    /// <summary>The TLS context the certificate selector resolved.</summary>
    public TlsContext? ResolvedContext { get; private set; }

    /// <summary>The client-certificate validation callback that came back with the resolved context.</summary>
    public RemoteCertificateValidationCallback? ResolvedClientCertificateValidation { get; private set; }

    /// <inheritdoc />
    protected override void RunUserCode()
    {
        // Fire the observe-only ClientHello listener first: the listener sees the ClientHello before the real context is installed.
        if (_clientHelloCallback is not null && Connection is not null && _clientHelloBuffer is not null && _clientHelloLength > 0)
        {
            _clientHelloCallback(Connection, new ReadOnlySequence<byte>(_clientHelloBuffer, 0, _clientHelloLength));
        }

        var (context, clientCertificateValidation) = _contextResolver(Connection, _targetHostName);
        ResolvedContext = context;
        ResolvedClientCertificateValidation = clientCertificateValidation;
    }

    /// <inheritdoc />
    protected override void ReleaseTransientState()
    {
        if (_clientHelloBuffer is { } buffer)
        {
            _clientHelloBuffer = null;
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
