// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for the listener's escalation of an unrecoverable pump failure. A single pump whose epoll loop
/// dies would otherwise leave its established connections permanently unserviced while the listen socket and the
/// other pumps keep the listener looking healthy. <see cref="DirectTlsConnectionListener.OnPumpFatalError"/> is
/// the seam a pump invokes in that case; these tests confirm it faults Accept (so the connection dispatcher sees
/// the error) and requests a host stop, exactly once. Driven directly without Bind(), so no real epoll failure
/// is needed. Linux-only, matching the rest of the DirectTls suite (the bootstrap context and the pump pool are
/// constructed on the runtime prototype).
/// </summary>
public class DirectTlsConnectionListenerFatalErrorTests
{
    private static DirectTlsConnectionListener CreateListener(
        TlsContext bootstrapContext,
        IHostApplicationLifetime applicationLifetime)
        => new(
            NullLoggerFactory.Instance,
            bootstrapContext,
            contextResolver: null,
            new TlsEventPumpPool(pumpCount: 1, NullLoggerFactory.Instance),
            new IPEndPoint(IPAddress.Loopback, 0),
            new DirectTlsTransportOptions(),
            MemoryPool<byte>.Shared,
            applicationLifetime);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task OnPumpFatalError_StopsApplication_AndFaultsAccept()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var lifetime = new TestHostApplicationLifetime();
        var listener = CreateListener(bootstrap, lifetime);

        var error = new InvalidOperationException("epoll_wait failed");
        listener.OnPumpFatalError(error);

        // The host is asked to stop, and the accept loop rethrows the exact failure so the connection dispatcher
        // logs it as critical instead of the listener silently returning null.
        Assert.Equal(1, lifetime.StopApplicationCallCount);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () => await listener.AcceptAsync());
        Assert.Same(error, thrown);

        await listener.DisposeAsync();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task OnPumpFatalError_ReportedByMultiplePumps_StopsApplicationOnce()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var lifetime = new TestHostApplicationLifetime();
        var listener = CreateListener(bootstrap, lifetime);

        var first = new InvalidOperationException("first");
        listener.OnPumpFatalError(first);
        listener.OnPumpFatalError(new InvalidOperationException("second"));

        // Only the first failing pump escalates; a second report is a no-op, so the host is stopped once and
        // Accept surfaces the first error.
        Assert.Equal(1, lifetime.StopApplicationCallCount);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () => await listener.AcceptAsync());
        Assert.Same(first, thrown);

        await listener.DisposeAsync();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task AcceptAsync_NormalChannelCompletion_ReturnsNull_WithoutStoppingApplication()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var lifetime = new TestHostApplicationLifetime();
        var listener = CreateListener(bootstrap, lifetime);

        // A normal shutdown (UnbindAsync) completes the channel without an error: Accept returns null and the
        // host is not asked to stop.
        await listener.UnbindAsync();

        Assert.Null(await listener.AcceptAsync());
        Assert.Equal(0, lifetime.StopApplicationCallCount);

        await listener.DisposeAsync();
    }
}
