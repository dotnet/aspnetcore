// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Security;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for listener teardown of the owned native OpenSSL server credentials (bootstrap + per-SNI
/// contexts). A fake <see cref="IDisposable"/> stands in for the real <see cref="ServerTlsContexts"/> so the
/// lifecycle (disposed on <see cref="DirectTlsConnectionListener.DisposeAsync"/>, not on UnbindAsync, exactly
/// once) is observable without OpenSSL. The listener is exercised without Bind(), so no listen socket or pump
/// threads are involved. Linux-only, matching the rest of the DirectTls suite (the bootstrap context and the
/// pump pool are constructed on the runtime prototype).
/// </summary>
public class DirectTlsListenerDisposalTests
{
    private sealed class CountingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    private static DirectTlsConnectionListener CreateListener(
        TlsContext bootstrapContext,
        IDisposable ownedServerContexts)
        => new(
            NullLoggerFactory.Instance,
            bootstrapContext,
            contextResolver: null,
            new TlsEventPumpPool(pumpCount: 1, NullLoggerFactory.Instance),
            new IPEndPoint(IPAddress.Loopback, 0),
            new DirectTlsTransportOptions(),
            MemoryPool<byte>.Shared,
            new TestHostApplicationLifetime(),
            clientHelloCallback: null,
            ownedServerContexts: ownedServerContexts);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task DisposeAsync_DisposesOwnedServerContexts()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var owned = new CountingDisposable();
        var listener = CreateListener(bootstrap, owned);

        await listener.DisposeAsync();

        Assert.Equal(1, owned.DisposeCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task DisposeAsync_CalledTwice_DisposesOwnedServerContextsOnce()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var owned = new CountingDisposable();
        var listener = CreateListener(bootstrap, owned);

        await listener.DisposeAsync();
        await listener.DisposeAsync();

        Assert.Equal(1, owned.DisposeCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task UnbindAsync_DoesNotDisposeOwnedServerContexts()
    {
        using var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var owned = new CountingDisposable();
        var listener = CreateListener(bootstrap, owned);

        // Unbind stops listening but keeps serving established connections, so the contexts must survive it.
        await listener.UnbindAsync();

        Assert.Equal(0, owned.DisposeCount);

        await listener.DisposeAsync();
        Assert.Equal(1, owned.DisposeCount);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void ServerTlsContexts_Dispose_IsIdempotent()
    {
        var bootstrap = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        var contexts = new ServerTlsContexts(
            bootstrap,
            new System.Collections.Concurrent.ConcurrentDictionary<System.Security.Cryptography.X509Certificates.X509Certificate2, TlsContext>());

        contexts.Dispose();

        // Second call must be a guarded no-op: disposing the underlying native TlsContext twice would risk a
        // double free. Reaching here without throwing confirms the Interlocked guard short-circuits.
        contexts.Dispose();
    }
}
