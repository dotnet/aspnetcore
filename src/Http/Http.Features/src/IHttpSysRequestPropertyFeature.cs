// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides API to read HTTP_REQUEST_PROPERTY value from the HTTP.SYS request.
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_request_property"/>
/// </summary>
public interface IHttpSysRequestPropertyFeature
{
    /// <summary>
    /// Reads the TLS client hello from HTTP.SYS
    /// </summary>
    /// <param name="tlsClientHelloBytesDestination">where raw bytes of tls client hello message will be written</param>
    /// <param name="bytesReturned">
    /// Returns the number of bytes written to <paramref name="tlsClientHelloBytesDestination"/>.
    /// Or can return the size of buffer needed if <paramref name="tlsClientHelloBytesDestination"/> wasn't large enough.
    /// </param>
    /// <remarks>
    /// Works only if <c>HTTP_SERVICE_CONFIG_SSL_FLAG_ENABLE_CACHE_CLIENT_HELLO</c> flag is set on http.sys service configuration.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/http/nf-http-httpsetserviceconfiguration"/>
    /// and <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_service_config_id"/>
    /// <br/><br/>
    /// If you don't want to guess required <paramref name="tlsClientHelloBytesDestination"/> size before first invocation,
    /// you should call first with <paramref name="tlsClientHelloBytesDestination"/> set to empty size, so that you can retrieve through <paramref name="bytesReturned"/> the buffer size you need,
    /// then allocate that amount of memory, then retry the query.
    /// </remarks>
    /// <returns>
    /// True, if fetching TLS client hello was successful, false if <paramref name="tlsClientHelloBytesDestination"/> size is not large enough.
    /// If non-successful for other reason throws an exception.
    /// </returns>
    bool TryGetTlsClientHello(Span<byte> tlsClientHelloBytesDestination, out int bytesReturned);
}
