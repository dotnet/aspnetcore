// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Owns the native <see cref="TlsContext"/> instances created for a single listener: the credential-less
/// bootstrap context that drives the deferred SNI handshake, plus the per-certificate contexts the SNI
/// resolver creates on demand.
/// </summary>
/// <remarks>
/// Each <see cref="TlsContext"/> acquires OpenSSL server credentials, so they must be disposed when the
/// listener is torn down or they leak for the process lifetime. Disposal is idempotent and MUST run only
/// after the pump threads have stopped, so no in-flight handshake can still be using one of these contexts.
/// The per-certificate dictionary is the live instance shared with the resolver closure, so disposal sees
/// every context created up to that point.
/// </remarks>
internal sealed class ServerTlsContexts : IDisposable
{
    private readonly TlsContext _bootstrapContext;
    private readonly ConcurrentDictionary<X509Certificate2, TlsContext> _perCertificateContexts;
    private int _disposed;

    public ServerTlsContexts(
        TlsContext bootstrapContext,
        ConcurrentDictionary<X509Certificate2, TlsContext> perCertificateContexts)
    {
        ArgumentNullException.ThrowIfNull(bootstrapContext);
        ArgumentNullException.ThrowIfNull(perCertificateContexts);

        _bootstrapContext = bootstrapContext;
        _perCertificateContexts = perCertificateContexts;
    }

    public void Dispose()
    {
        // Guard so each native context is freed exactly once even if the listener's DisposeAsync runs twice.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _bootstrapContext.Dispose();

        foreach (var context in _perCertificateContexts.Values)
        {
            context.Dispose();
        }

        _perCertificateContexts.Clear();
    }
}
