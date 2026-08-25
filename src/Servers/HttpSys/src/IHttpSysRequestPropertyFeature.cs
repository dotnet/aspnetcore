// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Provides API to read HTTP_REQUEST_PROPERTY value from the HTTP.SYS request.
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_request_property"/>
/// </summary>
// internal for backport
internal interface IHttpSysRequestPropertyFeature
{
    /// <summary>
    /// Reads the TLS client hello from HTTP.SYS
    /// </summary>
    /// <param name="tlsClientHelloBytesDestination">Where the raw bytes of the TLS Client Hello message are written.</param>
    /// <param name="bytesReturned">
    /// Returns the number of bytes written to <paramref name="tlsClientHelloBytesDestination"/>.
    /// Or can return the size of the buffer needed if <paramref name="tlsClientHelloBytesDestination"/> wasn't large enough.
    /// </param>
    /// <remarks>
    /// Works only if <c>HTTP_SERVICE_CONFIG_SSL_FLAG_ENABLE_CACHE_CLIENT_HELLO</c> flag is set on http.sys service configuration.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/http/nf-http-httpsetserviceconfiguration"/>
    /// and <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_service_config_id"/>
    /// <br/><br/>
    /// If you don't want to guess the required <paramref name="tlsClientHelloBytesDestination"/> size before first invocation,
    /// you should first call with <paramref name="tlsClientHelloBytesDestination"/> set to empty size, so that you can retrieve the required buffer size from <paramref name="bytesReturned"/>,
    /// then allocate that amount of memory and retry the query.
    /// </remarks>
    /// <returns>
    /// True, if fetching TLS client hello was successful, false if <paramref name="tlsClientHelloBytesDestination"/> size is not large enough.
    /// If unsuccessful for other reason throws an exception.
    /// </returns>
    /// <exception cref="HttpSysException">Any HttpSys error except for ERROR_INSUFFICIENT_BUFFER or ERROR_MORE_DATA.</exception>
    /// <exception cref="InvalidOperationException">If HttpSys does not support querying the TLS Client Hello.</exception>
    // has byte[] (not Span<byte>) for reflection-based invocation
    bool TryGetTlsClientHello(byte[] tlsClientHelloBytesDestination, out int bytesReturned);
}
