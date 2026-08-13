// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Sockets;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for <see cref="TlsEventPump.AcceptConnections"/> control flow and
/// <see cref="TlsEventPump.StopAccepting"/>. These exercise the real accept loop (guard, backlog drain,
/// transient-error skip, shutdown checks) without a live listen socket, epoll registration, or the pump thread,
/// by scripting <see cref="TlsEventPump.AcceptOne"/> outcomes (an accepted fd, or a negated errno) and stubbing
/// <see cref="TlsEventPump.WrapAcceptedFd"/> / <see cref="TlsEventPump.ProcessAcceptedSocket"/> so no native TLS
/// work runs. Linux-only, matching the rest of the DirectTls suite.
/// </summary>
public class TlsEventPumpAcceptTests
{
    // accept4 reports outcomes as a negated errno; the accept loop treats EAGAIN as "drained", EINTR as "retry",
    // and anything else as a fatal accept error that ends the current drain.
    private const int Drained = -NativeTls.EAGAIN;
    private const int FatalError = -NativeTls.EBADF;

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DrainsBacklog_ThenStopsOnWouldBlock()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<int>[]
            {
                () => 100,        // accepted #1
                () => 101,        // accepted #2
                () => Drained,    // backlog drained
            });
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(2, pump.ProcessedCount);
        Assert.Equal(3, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_StopsDrain_OnAcceptError()
    {
        // A non-EAGAIN accept error ends the current drain. epoll re-arms (level-triggered) if more connections
        // are pending, so we don't need to loop here - the second script entry is never reached.
        using var pump = new ScriptedAcceptPump(
            new Func<int>[]
            {
                () => FatalError,
                () => 100,   // must never be reached
            });
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(1, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_RetriesOnInterrupt_ThenDrains()
    {
        // EINTR means a signal interrupted accept4 before a connection was taken: the loop must retry, not treat
        // it as a fatal error or a drained backlog.
        using var pump = new ScriptedAcceptPump(
            new Func<int>[]
            {
                () => -NativeTls.EINTR,   // interrupted - retry
                () => 100,                // accepted
                () => Drained,            // drained
            });
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(1, pump.ProcessedCount);
        Assert.Equal(3, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DoesNotSpin_OnRepeatedFatalErrors()
    {
        // A listen fd that always fails (never yields EAGAIN) must not tight-spin: a single accept error breaks
        // the loop. Spin-safety comes from breaking here plus StopAccepting() de-registering the fd, not from a
        // bounded retry counter.
        using var pump = new ScriptedAcceptPump(
            script: Array.Empty<Func<int>>(),
            defaultOutcome: () => FatalError);
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(1, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DoesNotEnterLoop_WhenListenFdCleared()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<int>[] { () => 100, () => 101 });
        // _listenFd stays -1 (never seeded): the guard must break before the first accept.

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(0, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DoesNotEnterLoop_WhenNotRunning()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<int>[] { () => 100, () => 101 });
        pump.SetListenFd(1);
        pump.StopRunning();

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(0, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void StopAccepting_ClearsListenFd_SoAcceptLoopBreaks()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<int>[] { () => 100, () => 101 });
        pump.SetListenFd(1);

        pump.StopAccepting();
        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(0, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void StopAccepting_IsIdempotent()
    {
        using var pump = new ScriptedAcceptPump(Array.Empty<Func<int>>());
        pump.SetListenFd(1);

        pump.StopAccepting();
        pump.StopAccepting();   // second call must be a no-op, not throw
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DisposesSocket_WhenProcessingThrows()
    {
        // ProcessAcceptedSocket can throw before the fd is transferred to the TLS session (NoDelay/RemoteEndPoint
        // on a peer that reset after accept). The accept loop must dispose the socket wrapper so its fd is not
        // leaked to a finalizer, and must keep draining the rest of the backlog.
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var pump = new ThrowingProcessPump(
            socket,
            new Func<int>[]
            {
                () => 100,        // processing throws for this one
                () => Drained,    // backlog drained
            });
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.True(socket.SafeHandle.IsClosed);   // disposed - fd reclaimed
        Assert.Equal(1, pump.ProcessAttemptCount);
        Assert.Equal(2, pump.AcceptCallCount);     // drain continued past the failure
    }

    /// <summary>
    /// A pump that scripts <see cref="TlsEventPump.AcceptOne"/> outcomes (an accepted fd, or a negated errno)
    /// and records how many sockets reached <see cref="TlsEventPump.ProcessAcceptedSocket"/>, without ever
    /// touching a real listen socket, epoll, or the native TLS session. <see cref="TlsEventPump.WrapAcceptedFd"/>
    /// returns <c>null</c> because the stubbed <see cref="ProcessAcceptedSocket"/> never dereferences it.
    /// </summary>
    private sealed class ScriptedAcceptPump : TlsEventPump
    {
        private readonly Queue<Func<int>> _script;
        private readonly Func<int> _defaultOutcome;

        public int AcceptCallCount { get; private set; }
        public int ProcessedCount { get; private set; }

        public ScriptedAcceptPump(IEnumerable<Func<int>> script, Func<int>? defaultOutcome = null)
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
            _script = new Queue<Func<int>>(script);
            // When the script is exhausted, behave like a drained backlog unless told otherwise.
            _defaultOutcome = defaultOutcome ?? (() => -NativeTls.EAGAIN);
        }

        internal override int AcceptOne()
        {
            AcceptCallCount++;
            var next = _script.Count > 0 ? _script.Dequeue() : _defaultOutcome;
            return next();
        }

        internal override Socket WrapAcceptedFd(int fd) => null!;

        internal override void ProcessAcceptedSocket(Socket accepted)
        {
            ProcessedCount++;
        }
    }

    /// <summary>
    /// A pump that scripts accepted fds and makes <see cref="TlsEventPump.ProcessAcceptedSocket"/> throw, to
    /// model a pre-transfer configuration failure and verify the accept loop reclaims the socket's fd.
    /// <see cref="TlsEventPump.WrapAcceptedFd"/> returns the caller-provided socket so the disposal the loop
    /// performs on failure is observable.
    /// </summary>
    private sealed class ThrowingProcessPump : TlsEventPump
    {
        private readonly Queue<Func<int>> _script;
        private readonly Socket _acceptedSocket;

        public int AcceptCallCount { get; private set; }
        public int ProcessAttemptCount { get; private set; }

        public ThrowingProcessPump(Socket acceptedSocket, IEnumerable<Func<int>> script)
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
            _acceptedSocket = acceptedSocket;
            _script = new Queue<Func<int>>(script);
        }

        internal override int AcceptOne()
        {
            AcceptCallCount++;
            var next = _script.Count > 0 ? _script.Dequeue() : (() => -NativeTls.EAGAIN);
            return next();
        }

        internal override Socket WrapAcceptedFd(int fd) => _acceptedSocket;

        internal override void ProcessAcceptedSocket(Socket accepted)
        {
            ProcessAttemptCount++;
            // Model a throw from the socket-configuration window, before ownership transfers to the session.
            throw new SocketException((int)SocketError.NotConnected);
        }
    }
}
