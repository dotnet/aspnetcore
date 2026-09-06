// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Defaults <see cref="DirectTlsTransportOptions.MaxConcurrentHandshakes"/> from
/// <see cref="KestrelServerLimits.MaxConcurrentConnections"/> when it has not been set explicitly. On this
/// transport the TLS handshake runs before a connection is surfaced to Kestrel, so Kestrel's own
/// <c>MaxConcurrentConnections</c> limit - which only counts already-accepted connections - cannot gate a
/// handshake flood. Reusing the same value bounds the pre-handshake work without introducing a separate knob.
/// A <see langword="null"/> connection limit (the default) leaves handshakes unlimited.
/// </summary>
internal sealed class DirectTlsHandshakeLimitSetup : IPostConfigureOptions<DirectTlsTransportOptions>
{
    private readonly KestrelServerOptions _serverOptions;

    public DirectTlsHandshakeLimitSetup(IOptions<KestrelServerOptions> serverOptions)
    {
        ArgumentNullException.ThrowIfNull(serverOptions);
        _serverOptions = serverOptions.Value;
    }

    public void PostConfigure(string? name, DirectTlsTransportOptions options)
    {
        // Only supply the default when the transport option was not set explicitly. The right-hand side may
        // itself be null (unlimited), which is the intended behavior when no connection limit is configured.
        options.MaxConcurrentHandshakes ??= _serverOptions.Limits.MaxConcurrentConnections;
    }
}
