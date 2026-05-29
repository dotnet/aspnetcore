// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Provides API to read HTTP_REQUEST_PROPERTY value from the HTTP.SYS request.
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_request_property"/>
/// </summary>
public interface IHttpSysRequestPropertyFeature
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
    bool TryGetTlsClientHello(Span<byte> tlsClientHelloBytesDestination, out int bytesReturned);

    /// <summary>
    /// Reads an arbitrary HTTP_REQUEST_PROPERTY value from HTTP.SYS using the
    /// <see href="https://learn.microsoft.com/windows/win32/api/http/nf-http-httpqueryrequestproperty">HttpQueryRequestProperty</see> Windows API.
    /// </summary>
    /// <param name="propertyId">
    /// The HTTP_REQUEST_PROPERTY identifier to query. The set of supported values is defined by the
    /// <c>HTTP_REQUEST_PROPERTY</c> enum in <c>http.h</c>; the caller is responsible for parsing the bytes returned in
    /// <paramref name="output"/> using the corresponding native struct.
    /// </param>
    /// <param name="qualifier">
    /// Optional property-specific qualifier bytes. Pass an empty span for properties that do not require a qualifier;
    /// it will be mapped to a null pointer when calling the underlying API.
    /// </param>
    /// <param name="output">
    /// Destination buffer that receives the property value. Pass an empty span to query the required buffer size via <paramref name="bytesReturned"/>.
    /// </param>
    /// <param name="bytesReturned">
    /// Returns the number of bytes written to <paramref name="output"/>.
    /// If <paramref name="output"/> was too small (or empty), returns the size of the buffer required to hold the value.
    /// </param>
    /// <remarks>
    /// If the required buffer size is not known up front, first call this method with an empty <paramref name="output"/>
    /// to retrieve the required size in <paramref name="bytesReturned"/>, then allocate that many bytes and retry the query.
    /// </remarks>
    /// <returns>
    /// True if the property was successfully read into <paramref name="output"/>.
    /// False if <paramref name="output"/> is not large enough to hold the value; in that case <paramref name="bytesReturned"/>
    /// contains the required buffer size.
    /// For any other failure, an exception is thrown.
    /// </returns>
    /// <exception cref="HttpSysException">Any HttpSys error except for ERROR_INSUFFICIENT_BUFFER or ERROR_MORE_DATA.</exception>
    /// <exception cref="InvalidOperationException">If the installed Windows HTTP Server API does not support HttpQueryRequestProperty.</exception>
    bool TryGetRequestProperty(int propertyId, ReadOnlySpan<byte> qualifier, Span<byte> output, out int bytesReturned)
        => throw new NotSupportedException();
}
