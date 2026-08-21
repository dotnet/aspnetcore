// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.UserCallbacks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// The half of the pump that runs endpoint-supplied handshake callbacks off the event loop.
/// </summary>
/// <remarks>
/// A user callback (certificate selector, ClientHello listener, client-certificate validation) may block for
/// an unbounded time, and a pump thread owns the accept path plus the I/O readiness of every connection
/// assigned to it, so running one inline would stall all of them. Instead the handshake is parked, the
/// callback runs on the thread pool, and the result is handed back for the pump to resume. The split is
/// strictly by thread ownership: everything below either runs on the pump thread or does nothing but move a
/// result towards it - the TLS session, the epoll set and the handshake bookkeeping stay pump-thread-only.
/// </remarks>
internal partial class TlsEventPump
{
    private readonly ConcurrentQueue<HandshakeUserCallback> _completedCallbacks = new();

    // Number of user callbacks queued to the thread pool that have not reported back yet. The pump's fds are
    // closed and _exitSignal is completed only once the loop has exited AND this reaches zero, so a callback
    // can never write to a recycled wakeup fd, and a caller that observes the exit signal knows no thread can
    // still be running user code that reaches this pump's resources.
    private int _outstandingUserCallbacks;

    // Number of abandoned-connection disposals that have been started but have not finished.
    // DirectTlsConnection.DisposeAsync resumes the connection's send/receive loops asynchronously, so that work
    // outlives this pump's event loop while still reaching the memory pool and the OpenSSL contexts - which the
    // listener frees as soon as _exitSignal completes. Gating the signal on this as well is what keeps that from
    // becoming a use-after-free. Counted separately from _outstandingUserCallbacks because these never write to
    // the wakeup fd, so the argument above does not apply to them, and because it makes plain which kind of
    // off-pump work is holding shutdown open.
    private int _outstandingConnectionDisposals;

    // Handshakes that were parked on a user callback when the pump loop exited. Their teardown is deferred to
    // the drained-shutdown path so the session (and the certificate handed to the validation callback) is not
    // disposed underneath live user code. Only ever touched by the thread that exits the loop and then by the
    // single thread that runs the one-shot shutdown completion, so it needs no synchronization of its own.
    private readonly List<HandshakingConnection> _handshakesAwaitingCallback = [];

    // Whether resolving the TLS context can run user code: an endpoint-supplied certificate selector, or the
    // ClientHello listener that runs just before it. When neither is configured the resolver is the transport's
    // own lambda over a static certificate and a per-certificate context cache, so there is nothing to move off
    // the event loop and the pump resolves inline instead of suspending. Defaults to true so a caller that does
    // not report its selector fails safe onto the suspending path.
    private bool _contextResolverRunsUserCode = true;

    // Creates the eventfd a thread pool thread writes to when a user callback has finished, and registers it
    // in this pump's epoll set so the write wakes the loop. Called from the constructor once _epollFd exists;
    // it returns the fd rather than assigning it so the field can stay readonly. On failure it closes what it
    // opened (including the caller's epoll fd) before throwing, because the pump is never constructed and so
    // nothing else will ever run its teardown.
    private int CreateWakeupFd()
    {
        int wakeupFd = NativeTls.eventfd(0, NativeTls.EFD_NONBLOCK | NativeTls.EFD_CLOEXEC);
        if (wakeupFd < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            NativeTls.close(_epollFd);
            throw new InvalidOperationException($"eventfd failed: errno={errno}");
        }

        var wakeupEvent = new EpollEvent
        {
            Events = NativeTls.EPOLLIN,
            Data = new EpollData { Fd = wakeupFd }
        };

        if (NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_ADD, wakeupFd, ref wakeupEvent) < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            NativeTls.close(wakeupFd);
            NativeTls.close(_epollFd);
            throw new InvalidOperationException($"Failed to add the pump wakeup fd to epoll: errno={errno}");
        }

        return wakeupFd;
    }

    // Releases the handshakes that were parked on user code when the loop exited. Called exactly once, from the
    // one-shot drained-shutdown path, by whichever thread observes the last in-flight callback completing - so
    // by then the loop is gone and no user callback is running, giving this thread sole ownership.
    private void ReleaseDeferredHandshakes()
    {
        foreach (var conn in _handshakesAwaitingCallback)
        {
            ReleaseHandshakeResources(conn);
        }

        _handshakesAwaitingCallback.Clear();
    }

    // Finishes the pump's teardown once the event loop has exited AND every piece of off-pump work it started -
    // dispatched user callbacks and abandoned-connection disposals - has reported back: closes the fds it owns
    // and completes the exit signal. Called by the pump thread when the loop ends and by each piece of off-pump
    // work as it finishes, so whichever happens last does the work. Deferring the close until the callbacks have
    // drained is what makes CompleteUserCallback's wakeup write safe: the wakeup fd cannot have been closed (and
    // its number recycled by an unrelated fd) while a callback is still in flight.
    private void CompletePumpShutdownIfDrained()
    {
        if (!_loopExited
            || Volatile.Read(ref _outstandingUserCallbacks) != 0
            || Volatile.Read(ref _outstandingConnectionDisposals) != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _shutdownCompleted, 1) != 0)
        {
            return;
        }

        CloseOwnedFds();
        ReleaseDeferredHandshakes();
        _exitSignal.TrySetResult();
    }

    /// <summary>
    /// Parks a handshake while its user callback runs on the thread pool.
    /// </summary>
    /// <remarks>
    /// The fd is removed from this pump's epoll set for the whole suspension, so a parked connection cannot
    /// generate pump work (a peer that keeps writing would otherwise re-fire level-triggered EPOLLIN on every
    /// epoll_wait and spin the pump). The work item is recorded on the handshaking entry: it is both the
    /// "suspended" marker the event loop and the timeout sweep honour, and the token
    /// <see cref="ResumeSuspendedHandshake"/> matches on so a completion can never resume a handshake that was
    /// torn down (or an unrelated connection that reused the fd number) while the callback ran.
    /// Runs on the pump thread only.
    /// </remarks>
    private void SuspendHandshake(int fd, ref HandshakingConnection conn, HandshakeUserCallback callback)
    {
        DeregisterFromEpoll(fd);

        conn.PendingUserCallback = callback;
        conn.CurrentEpollInterest = 0;
        _handshaking[fd] = conn;

        // Counted before the item is queued so the pump's shutdown can never observe zero in-flight callbacks
        // while one is about to run.
        Interlocked.Increment(ref _outstandingUserCallbacks);

        // preferLocal: false - this must not land on the pump thread's local queue; the whole point is to get
        // the user code onto a different thread.
        ThreadPool.UnsafeQueueUserWorkItem(callback, preferLocal: false);
    }

    /// <summary>
    /// Called from a thread pool thread once a suspended handshake's user callback has finished (or thrown).
    /// Hands the result to the owning pump and wakes it; the handshake itself is only ever resumed on the pump
    /// thread, which is the sole owner of the TLS session.
    /// </summary>
    internal void CompleteUserCallback(HandshakeUserCallback callback)
    {
        _completedCallbacks.Enqueue(callback);

        // Wake the pump before releasing the in-flight count: the wakeup fd is only closed once the loop has
        // exited AND that count reaches zero, so this write can never hit a recycled descriptor.
        Wakeup();

        Interlocked.Decrement(ref _outstandingUserCallbacks);
        CompletePumpShutdownIfDrained();
    }

    // Nudges the pump out of epoll_wait by making its eventfd readable. Called from thread-pool threads.
    private void Wakeup()
    {
        long value = 1;
        if (NativeTls.write(_wakeupFd, ref value, sizeof(long)) < 0)
        {
            // EAGAIN only happens if the 64-bit counter is saturated (2^64-1 pending wakeups), which cannot
            // happen here; anything else means the fd is gone, in which case the pump is already shutting down.
            _logger.LogDebug("Pump {Id}: writing the wakeup fd failed: errno={Errno}", _id, Marshal.GetLastWin32Error());
        }
    }

    // Consumes the eventfd counter so the level-triggered wakeup fd stops firing. Runs on the pump thread.
    //
    // The counter is not a message and carries no payload: an eventfd created without EFD_SEMAPHORE returns the
    // accumulated sum of every write since the last read and resets it to zero, so N callbacks completing
    // between two polls surface as a single readable event with value N. The value is deliberately not used to
    // decide how much work to do - the completion queue is the source of truth, and this fd only says "look at
    // it". PumpLoop drains that queue after every batch whether or not this read succeeded, so a failure here
    // costs a diagnostic, never a parked handshake.
    //
    // Failing to consume the counter cannot spin the loop. EAGAIN means the counter is already zero, so the fd
    // is no longer readable and epoll will not report it again. EINTR leaves the counter set, so the
    // level-triggered registration re-reports EPOLLIN and the next iteration retries this read - one extra
    // iteration rather than an inner retry loop on the pump thread. A closed fd leaves the epoll set entirely.
    private void DrainWakeup()
    {
        long value = 0;
        nint read = NativeTls.read(_wakeupFd, ref value, sizeof(long));

        if (read == sizeof(long))
        {
            // epoll reported EPOLLIN and the pump thread is the only reader, so the counter cannot have been
            // consumed in between; a zero counter would mean this is not the eventfd we registered.
            Debug.Assert(value >= 1, $"The wakeup fd reported a counter of {value}.");
            return;
        }

        if (read < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            if (errno is NativeTls.EAGAIN or NativeTls.EINTR)
            {
                return;
            }

            _logger.LogDebug("Pump {Id}: reading the wakeup fd failed: errno={Errno}", _id, errno);
            return;
        }

        // An eventfd read is all-or-nothing (the kernel rejects a count below 8 bytes with EINVAL), so a
        // partial read means this fd is not the eventfd we registered.
        Debug.Assert(false, $"Reading the wakeup fd returned {read} bytes.");
        _logger.LogDebug("Pump {Id}: reading the wakeup fd returned {Read} bytes.", _id, read);
    }

    // Resumes every handshake whose user callback has reported back. Runs on the pump thread only.
    private void DrainCompletedUserCallbacks()
    {
        while (_completedCallbacks.TryDequeue(out var callback))
        {
            ResumeSuspendedHandshake(callback);
        }
    }

    /// <summary>
    /// Resumes one suspended handshake with the result of its user callback. Runs on the pump thread only.
    /// </summary>
    private void ResumeSuspendedHandshake(HandshakeUserCallback callback)
    {
        int fd = callback.Fd;
        if (!_handshaking.TryGetValue(fd, out var conn) || !ReferenceEquals(conn.PendingUserCallback, callback))
        {
            throw new UnreachableException($"Pump {_id}: a user callback completed for fd={fd}, but the handshake it was parked on is gone or is parked on a different callback.");
        }

        // Clear the suspension marker first, so this handshake can never be resumed twice.
        conn.PendingUserCallback = null;
        _handshaking[fd] = conn;

        if (callback.Failure is { } failure)
        {
            // The ClientHello listener, the certificate selector, or the client-certificate validation callback
            // threw. Fail this one connection (matching the socket-transport TlsListener) and log it.
            _logger.LogDebug(failure, "A TLS handshake callback failed for fd={Fd}; dropping connection.", fd);
            DropHandshake(fd, conn);
            return;
        }

        // Re-arm the fd before touching the session again: from here on the handshake may need more epoll
        // round-trips, and a completed handshake is promoted with EPOLL_CTL_MOD, which requires registration.
        if (!TryArmHandshakeInterest(fd, DefaultEpollInterest))
        {
            DropHandshake(fd, conn);
            return;
        }

        conn.CurrentEpollInterest = DefaultEpollInterest;
        _handshaking[fd] = conn;

        switch (callback)
        {
            case ResolveTlsContextCallback resolvedContext:
                try
                {
                    conn.Session.SetContext(resolvedContext.ResolvedContext!);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Installing the resolved TLS context failed for fd={Fd}", fd);
                    DropHandshake(fd, conn);
                    return;
                }

                // The endpoint's client-certificate validation callback is resolved with the context. Persist
                // it on the handshaking entry so the Complete branch can drive mTLS validation, even if the
                // handshake needs several more epoll round-trips (each re-reads _handshaking[fd]).
                conn.ClientCertificateValidation = resolvedContext.ResolvedClientCertificateValidation;
                _handshaking[fd] = conn;

                // Real context is now set; continue the handshake immediately.
                TryAdvanceHandshake(fd, conn);
                return;

            case ValidateClientCertificateCallback validation:
                if (!validation.CertificateAccepted)
                {
                    _logger.LogDebug("Client certificate rejected for fd={Fd} (presented={Presented}).", fd, validation.PresentedCertificate is not null);
                    DropHandshake(fd, conn);
                    return;
                }

                // Record the accepted result so the runtime promotes the leaf into its canonical remote-cert
                // slot and clears its pending-validation state. Throws only in incorrect state of session, so we must drop the handshake.
                try
                {
                    conn.Session.SetRemoteCertificateValidationResult(SslPolicyErrors.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Recording the client certificate verdict failed for fd={Fd}; dropping connection.", fd);
                    DropHandshake(fd, conn);
                    return;
                }

                // Surface the accepted certificate to Kestrel via ITlsConnectionFeature. This is the same
                // instance the runtime just promoted into its canonical remote-cert slot (on the accept path
                // SetRemoteCertificateValidationResult moves _externalPendingCert into _remoteCertificate
                // without reallocating), and null when the client presented none on an AllowCertificate
                // endpoint.
                CompleteHandshake(fd, conn, validation.PresentedCertificate);
                return;

            default:
                throw new UnreachableException($"Pump {_id}: unhandled handshake user callback type {callback.GetType()} for fd={fd}.");
        }
    }
}
