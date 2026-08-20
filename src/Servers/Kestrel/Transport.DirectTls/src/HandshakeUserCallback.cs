// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// One suspended handshake's user code, executed on the thread pool instead of on the pump (epoll) thread.
/// </summary>
/// <remarks>
/// User-supplied handshake callbacks - the ClientHello listener, the server-certificate selector, and the
/// client-certificate validation callback - can block for an arbitrarily long time (a disk read, a key vault
/// round trip, a lock). A pump thread owns accept plus all I/O readiness for every connection assigned to it,
/// so running any of them inline stalls every one of those connections. Instead the pump parks the handshake
/// (de-registering its fd from the epoll set so it cannot generate pump work while parked) and queues this
/// work item. Everything a derived item touches was copied out of the session on the pump thread beforehand:
/// it never calls into <c>TlsSocketSession</c>, which stays single-threaded and owned by its pump. When the
/// user code returns - or throws - the result is handed back to the owning pump through
/// <see cref="TlsEventPump.CompleteUserCallback"/>, which resumes the handshake on the pump thread.
/// <para>
/// Each suspension point has its own derived type carrying only its own state, so the pump resumes by
/// switching on the work item's type. <see cref="Execute"/> is deliberately not virtual: the try/catch/finally
/// it wraps every callback in is what guarantees that a suspended handshake reports back exactly once, whether
/// the user code returns or throws, so a derived type must not be able to replace it.
/// </para>
/// </remarks>
internal abstract class HandshakeUserCallback : IThreadPoolWorkItem
{
    private readonly TlsEventPump _pump;

    protected HandshakeUserCallback(TlsEventPump pump, int fd, DirectTlsConnection? connection)
    {
        _pump = pump;
        Fd = fd;
        Connection = connection;
    }

    /// <summary>The handshaking file descriptor this callback belongs to.</summary>
    public int Fd { get; }

    /// <summary>
    /// The connection allocated early (at <c>NeedsTlsContext</c>) so user code sees a stable
    /// <see cref="ConnectionContext"/>. Null when nothing needed one that early: the pump resolved the TLS
    /// context inline because no user code runs at <c>NeedsTlsContext</c> (so only the client-certificate
    /// suspension is reachable, and it does not use this), or the pump has no memory pool (tests). In both
    /// cases the connection is allocated when the handshake completes instead.
    /// </summary>
    public DirectTlsConnection? Connection { get; }

    /// <summary>The exception the user code threw, if any. Non-null means the pump drops the connection.</summary>
    public Exception? Failure { get; private set; }

    /// <inheritdoc />
    public void Execute()
    {
        try
        {
            RunUserCode();
        }
        catch (Exception ex)
        {
            // A throwing user callback (or a selector that resolved no certificate) fails this one connection.
            // The pump logs it and drops the handshake when it picks the result up; it must never escape onto
            // a thread pool thread, where it would tear the process down.
            Failure = ex;
        }
        finally
        {
            ReleaseTransientState();

            // Hand the result back to the owning pump. Nothing here may touch the session, the epoll set, or
            // the handshake bookkeeping - those are pump-thread-only.
            _pump.CompleteUserCallback(this);
        }
    }

    /// <summary>
    /// Runs the endpoint-supplied callback on the thread pool and records its result on this instance. Any
    /// throw is captured by <see cref="Execute"/> as <see cref="Failure"/>.
    /// </summary>
    protected abstract void RunUserCode();

    /// <summary>
    /// Releases anything borrowed for the duration of the callback, whether it returned or threw. Runs before
    /// the result is handed back to the pump.
    /// </summary>
    protected virtual void ReleaseTransientState()
    {
    }
}

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

/// <summary>
/// The client-certificate validation suspension: the chain build plus the endpoint's validation callback, run
/// once the handshake reports <c>Complete</c>.
/// </summary>
internal sealed class ValidateClientCertificateCallback : HandshakeUserCallback
{
    private readonly RemoteCertificateValidationCallback _validateClientCertificate;
    private readonly object _validationSender;
    private readonly X509Certificate2Collection? _intermediates;

    /// <summary>
    /// Creates the work item for the client-certificate validation suspension. The certificates were read from
    /// the session on the pump thread; the chain build and the endpoint's callback run here.
    /// </summary>
    public ValidateClientCertificateCallback(
        TlsEventPump pump,
        int fd,
        DirectTlsConnection? connection,
        object validationSender,
        X509Certificate2? presentedCertificate,
        X509Certificate2Collection? intermediates,
        RemoteCertificateValidationCallback validateClientCertificate)
        : base(pump, fd, connection)
    {
        _validationSender = validationSender;
        PresentedCertificate = presentedCertificate;
        _intermediates = intermediates;
        _validateClientCertificate = validateClientCertificate;
    }

    /// <summary>The peer's leaf certificate, or null when it presented none.</summary>
    public X509Certificate2? PresentedCertificate { get; }

    /// <summary>Whether the endpoint's validation callback accepted the peer's certificate.</summary>
    public bool CertificateAccepted { get; private set; }

    /// <inheritdoc />
    protected override void RunUserCode()
        => CertificateAccepted = ClientCertificateValidator.Validate(
            _validationSender,
            PresentedCertificate,
            _intermediates,
            _validateClientCertificate);
}
