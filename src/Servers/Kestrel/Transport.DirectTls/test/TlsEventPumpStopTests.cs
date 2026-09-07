// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for the shutdown contract that gates listener resource release on the pump thread having actually
/// exited (<see cref="TlsEventPump.StopAndJoinAsync"/> and <see cref="TlsEventPumpPool.StopAndConfirmExitAsync"/>).
/// A timed join that returns does not prove the thread stopped: a blocking user callback (certificate selector,
/// certificate validation, or ClientHello listener) can outlive it. If the owner then freed the epoll fd, the
/// OpenSSL contexts, or the memory pool that the still-running thread can reach, it would be a use-after-free.
/// These tests confirm the seam reports <see langword="true"/> only when the thread is gone. Linux-only,
/// matching the rest of the DirectTls suite.
/// </summary>
public class TlsEventPumpStopTests
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task StopAndJoinAsync_NeverStarted_ReturnsTrue_AndIsIdempotent()
    {
        var pump = new TlsEventPump(NullLogger<TlsEventPump>.Instance, id: 0, Timeout.InfiniteTimeSpan);

        // A pump whose thread was never started cannot touch any owned resource, so the owner may release them.
        Assert.True(await pump.StopAndJoinAsync(CancellationToken.None));

        // The second call must return the recorded result without re-closing the epoll fd (a double close could
        // hit an unrelated fd whose number was recycled).
        Assert.True(await pump.StopAndJoinAsync(CancellationToken.None));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Pool_StopAndConfirmExitAsync_NeverStartedPumps_ReturnsTrue_AndIsIdempotent()
    {
        var pool = new TlsEventPumpPool(pumpCount: 2, NullLoggerFactory.Instance);

        Assert.True(await pool.StopAndConfirmExitAsync(CancellationToken.None));
        Assert.True(await pool.StopAndConfirmExitAsync(CancellationToken.None));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task StopAndJoinAsync_ThreadBlockedInCallback_ReturnsFalse()
    {
        using var release = new ManualResetEventSlim(initialState: false);
        var pump = new BlockingAcceptPump(release);

        using var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listenSocket.Listen(backlog: 16);
        var listenFd = (int)listenSocket.Handle;
        var endpoint = (IPEndPoint)listenSocket.LocalEndPoint!;

        // The bootstrap context stands in for the real OpenSSL server credentials the listener owns and would
        // free once the pumps confirm exit; it must outlive the pump thread, so it is disposed only at the end.
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var readyConnections = Channel.CreateUnbounded<DirectTlsConnection>();

        pump.StartWithListenSocket(
            listenFd,
            endpoint,
            bootstrap,
            contextResolver: null,
            readyConnections.Writer,
            MemoryPool<byte>.Shared,
            NullLoggerFactory.Instance,
            noDelay: false,
            maxReadBufferSize: 0,
            maxWriteBufferSize: 0,
            onFatalError: static _ => { });

        // Drive one connection so the pump thread wakes on the listen fd and enters AcceptOne, where it blocks -
        // this stands in for a user callback that never returns.
        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(endpoint);

        Assert.True(pump.AcceptEntered.Wait(TimeSpan.FromSeconds(5)), "pump thread never reached the blocking callback");

        // The thread is provably parked in the callback, so a canceled wait must return false and the seam must
        // report the thread as still alive - the signal the listener uses to leak (not free) pump-reachable resources.
        using var stopCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        Assert.False(await pump.StopAndJoinAsync(stopCts.Token));

        // Let the thread unwind so it is not left parked, then give it a moment to observe the stop and exit
        // before the bootstrap context is disposed at scope exit.
        release.Set();
        Thread.Sleep(200);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task StopAndJoinAsync_AbandonedConnectionDisposalInFlight_ReturnsFalse()
    {
        var disposalStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDisposal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pump = new ControllableDisposalPump(disposalStarted, releaseDisposal);

        using var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listenSocket.Listen(backlog: 16);

        // Stands in for the OpenSSL server credentials the listener frees as soon as the pump confirms exit.
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var readyConnections = Channel.CreateUnbounded<DirectTlsConnection>();

        pump.StartWithListenSocket(
            (int)listenSocket.Handle,
            (IPEndPoint)listenSocket.LocalEndPoint!,
            bootstrap,
            contextResolver: null,
            readyConnections.Writer,
            MemoryPool<byte>.Shared,
            NullLoggerFactory.Instance,
            noDelay: false,
            maxReadBufferSize: 0,
            maxWriteBufferSize: 0,
            onFatalError: static _ => { });

        var connectionState = new ConnectionIoState(
            fd: 101,
            session: null!,
            NullLogger<ConnectionIoState>.Instance);
        var connection = new DirectTlsConnection(
            connectionState,
            pump,
            localEndPoint: null,
            remoteEndPoint: null,
            MemoryPool<byte>.Shared,
            maxReadBufferSize: 0,
            maxWriteBufferSize: 0,
            NullLogger<DirectTlsConnection>.Instance);

        // A connection that completed its handshake just as the listener stopped accepting. Its DisposeAsync
        // resumes the send/receive loops asynchronously, so it outlives the event loop.
        _ = pump.DisposeAbandonedConnectionAsync(connection);
        await disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The event loop is free to wind down, but the disposal is provably still running and still reaches the
        // memory pool and the OpenSSL contexts, so the pump must not yet report itself as exited - that report is
        // exactly what lets the listener free them.
        pump.SignalStop();
        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Assert.False(await pump.StopAndJoinAsync(stopCts.Token));

        // Let the disposal finish so the pump closes its own fds before the bootstrap context goes away.
        releaseDisposal.SetResult();
        Thread.Sleep(200);
    }

    /// <summary>
    /// A pump whose abandoned-connection disposal is held open by the test, standing in for send/receive loops
    /// that are still unwinding after the event loop has gone. The real TLS session is never touched.
    /// </summary>
    private sealed class ControllableDisposalPump : TlsEventPump
    {
        private readonly TaskCompletionSource _started;
        private readonly TaskCompletionSource _release;

        public ControllableDisposalPump(TaskCompletionSource started, TaskCompletionSource release)
            : base(NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
            _started = started;
            _release = release;
        }

        internal override async ValueTask DisposeConnectionAsync(DirectTlsConnection connection)
        {
            _started.TrySetResult();
            await _release.Task;
        }
    }

    /// <summary>
    /// A pump whose accept path blocks on the first call, standing in for a user callback that never returns.
    /// Once released it reports a drained backlog so the loop can wind down. The native TLS session is never
    /// touched because <see cref="ProcessAcceptedSocket"/> is stubbed out.
    /// </summary>
    private sealed class BlockingAcceptPump : TlsEventPump
    {
        private readonly ManualResetEventSlim _release;

        public ManualResetEventSlim AcceptEntered { get; } = new(initialState: false);

        public BlockingAcceptPump(ManualResetEventSlim release)
            : base(NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
            => _release = release;

        internal override int AcceptOne()
        {
            AcceptEntered.Set();
            _release.Wait();
            return -NativeTls.EAGAIN;   // report a drained backlog so the loop winds down once released
        }

        internal override void ProcessAcceptedSocket(Socket accepted)
        {
        }
    }
}
