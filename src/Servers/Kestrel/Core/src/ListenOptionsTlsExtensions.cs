// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Middleware;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for <see cref="ListenOptions"/> that configure TLS in Kestrel
/// </summary>
public static class ListenOptionsTlsExtensions
{
    /// <summary>
    /// Adds a connection middleware that sniffs the TLS Client Hello message and invokes <paramref name="tlsClientHelloBytesCallback"/>
    /// with the raw bytes before the TLS handshake is performed.
    /// This must be called before <c>UseHttps()</c> so that the middleware runs prior to the TLS handshake.
    /// </summary>
    /// <param name="listenOptions">The <see cref="ListenOptions"/> to configure.</param>
    /// <param name="tlsClientHelloBytesCallback">
    /// The callback to invoke with the <see cref="ConnectionContext"/> and the raw TLS Client Hello bytes
    /// (still wrapped in the TLS record layer fragment).
    /// </param>
    /// <returns>The <see cref="ListenOptions"/>.</returns>
    public static ListenOptions UseTlsClientHelloListener(this ListenOptions listenOptions, Action<ConnectionContext, ReadOnlySequence<byte>> tlsClientHelloBytesCallback)
    {
        ArgumentNullException.ThrowIfNull(tlsClientHelloBytesCallback);

        var tlsListener = new TlsListener(tlsClientHelloBytesCallback);

        listenOptions.Use(next =>
        {
            return async context =>
            {
                await tlsListener.OnTlsClientHelloAsync(context, context.ConnectionClosed);
                await next(context);
            };
        });

        return listenOptions;
    }
}
