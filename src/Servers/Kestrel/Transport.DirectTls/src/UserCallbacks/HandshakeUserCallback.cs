// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.UserCallbacks;

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
