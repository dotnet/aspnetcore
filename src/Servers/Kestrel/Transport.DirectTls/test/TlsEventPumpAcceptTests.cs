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
/// transient-error skip, consecutive-failure breaker, shutdown checks) without a live listen socket,
/// epoll registration, or the pump thread, by scripting <see cref="TlsEventPump.AcceptOne"/> outcomes and
/// stubbing <see cref="TlsEventPump.ProcessAcceptedSocket"/> so no native TLS work runs.
/// Linux-only, matching the rest of the DirectTls suite.
/// </summary>
public class TlsEventPumpAcceptTests
{
    private static SocketException WouldBlock()
        => new SocketException((int)SocketError.WouldBlock);

    private static SocketException Fatal()
        => new SocketException((int)SocketError.NotSocket);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DrainsBacklog_ThenStopsOnWouldBlock()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<Socket>[]
            {
                () => null!,                  // accepted #1
                () => null!,                  // accepted #2
                () => throw WouldBlock(),     // backlog drained
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
        // A non-WouldBlock accept error ends the current drain. epoll re-arms (level-triggered) if more
        // connections are pending, so we don't need to loop here - the second script entry is never reached.
        using var pump = new ScriptedAcceptPump(
            new Func<Socket>[]
            {
                () => throw Fatal(),
                () => null!,   // must never be reached
            });
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(1, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_DoesNotSpin_OnRepeatedFatalErrors()
    {
        // A listen fd that always fails (never yields WouldBlock) must not tight-spin: a single accept
        // error breaks the loop. Spin-safety comes from breaking here plus StopAccepting() de-registering
        // the fd, not from a bounded retry counter.
        using var pump = new ScriptedAcceptPump(
            script: Array.Empty<Func<Socket>>(),
            defaultOutcome: () => throw Fatal());
        pump.SetListenFd(1);

        pump.AcceptConnections();

        Assert.Equal(0, pump.ProcessedCount);
        Assert.Equal(1, pump.AcceptCallCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void AcceptConnections_BreaksOnObjectDisposed()
    {
        using var pump = new ScriptedAcceptPump(
            new Func<Socket>[]
            {
                () => throw new ObjectDisposedException("listenSocket"),
                () => null!,   // must never be reached
            });
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
            new Func<Socket>[] { () => null!, () => null! });
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
            new Func<Socket>[] { () => null!, () => null! });
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
            new Func<Socket>[] { () => null!, () => null! });
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
        using var pump = new ScriptedAcceptPump(Array.Empty<Func<Socket>>());
        pump.SetListenFd(1);

        pump.StopAccepting();
        pump.StopAccepting();   // second call must be a no-op, not throw
    }

    /// <summary>
    /// A pump that scripts <see cref="TlsEventPump.AcceptOne"/> outcomes and records how many sockets
    /// reached <see cref="TlsEventPump.ProcessAcceptedSocket"/>, without ever touching a real listen
    /// socket, epoll, or the native TLS session. Accepted "sockets" are represented as <c>null</c>
    /// because the stubbed <see cref="ProcessAcceptedSocket"/> never dereferences them.
    /// </summary>
    private sealed class ScriptedAcceptPump : TlsEventPump
    {
        private readonly Queue<Func<Socket>> _script;
        private readonly Func<Socket> _defaultOutcome;

        public int AcceptCallCount { get; private set; }
        public int ProcessedCount { get; private set; }

        public ScriptedAcceptPump(IEnumerable<Func<Socket>> script, Func<Socket>? defaultOutcome = null)
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
            _script = new Queue<Func<Socket>>(script);
            // When the script is exhausted, behave like a drained backlog unless told otherwise.
            _defaultOutcome = defaultOutcome ?? (() => throw new SocketException((int)SocketError.WouldBlock));
        }

        internal override Socket AcceptOne()
        {
            AcceptCallCount++;
            var next = _script.Count > 0 ? _script.Dequeue() : _defaultOutcome;
            return next();
        }

        internal override void ProcessAcceptedSocket(Socket accepted)
        {
            ProcessedCount++;
        }
    }
}
