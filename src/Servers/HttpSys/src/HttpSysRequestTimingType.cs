// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Defines the types of request processing timestamps exposed via the Http.Sys HTTP_REQUEST_TIMING_INFO extensibility point.
/// </summary>
/// <remarks>
/// Use <see cref="IHttpSysRequestTimingFeature"/> to access these timestamps.
/// </remarks>
public enum HttpSysRequestTimingType
{
    // IMPORTANT: Order matters and should match the order defined in HTTP_REQUEST_TIMING_TYPE

    /// <summary>
    /// Time the connection started.
    /// </summary>
    ConnectionStart,

    /// <summary>
    /// Time the first HTTP byte is received.
    /// </summary>
    DataStart,

    /// <summary>
    /// Time TLS certificate loading starts.
    /// </summary>
    TlsCertificateLoadStart,

    /// <summary>
    /// Time TLS certificate loading ends.
    /// </summary>
    TlsCertificateLoadEnd,

    /// <summary>
    /// Time TLS leg one handshake starts.
    /// </summary>
    TlsHandshakeLeg1Start,

    /// <summary>
    /// Time TLS leg one handshake ends.
    /// </summary>
    TlsHandshakeLeg1End,

    /// <summary>
    /// Time TLS leg two handshake starts.
    /// </summary>
    TlsHandshakeLeg2Start,

    /// <summary>
    /// Time TLS leg two handshake ends.
    /// </summary>
    TlsHandshakeLeg2End,

    /// <summary>
    /// Time TLS attribute query starts.
    /// </summary>
    TlsAttributesQueryStart,

    /// <summary>
    /// Time TLS attribute query ends.
    /// </summary>
    TlsAttributesQueryEnd,

    /// <summary>
    /// Time TLS client cert query starts.
    /// </summary>
    TlsClientCertQueryStart,

    /// <summary>
    /// Time TLS client cert query ends.
    /// </summary>
    TlsClientCertQueryEnd,

    /// <summary>
    /// Time HTTP2 streaming starts.
    /// </summary>
    Http2StreamStart,

    /// <summary>
    /// Time HTTP2 header decoding starts.
    /// </summary>
    Http2HeaderDecodeStart,

    /// <summary>
    /// Time HTTP2 header decoding ends.
    /// </summary>
    Http2HeaderDecodeEnd,

    /// <summary>
    /// Time HTTP header parsing starts.
    /// </summary>
    /// <remarks>
    /// For most requests, this is a good timestamp to use as a per request starting timestamp.
    /// </remarks>
    RequestHeaderParseStart,

    /// <summary>
    /// Time HTTP header parsing ends.
    /// </summary>
    RequestHeaderParseEnd,

    /// <summary>
    /// Time Http.Sys starts to determine which request queue to route the request to.
    /// </summary>
    RequestRoutingStart,

    /// <summary>
    /// Time Http.Sys has determined which request queue to route the request to.
    /// </summary>
    RequestRoutingEnd,

    /// <summary>
    /// Time the request is queued for inspection.
    /// </summary>
    RequestQueuedForInspection,

    /// <summary>
    /// Time the request is delivered for inspection.
    /// </summary>
    RequestDeliveredForInspection,

    /// <summary>
    /// Time the request has finished being inspected.
    /// </summary>
    RequestReturnedAfterInspection,

    /// <summary>
    /// Time the request is queued for delegation.
    /// </summary>
    RequestQueuedForDelegation,

    /// <summary>
    /// Time the request is delivered for delegation.
    /// </summary>
    RequestDeliveredForDelegation,

    /// <summary>
    /// Time the request was delegated.
    /// </summary>
    RequestReturnedAfterDelegation,

    /// <summary>
    /// Time the request was queued to the final request queue for processing.
    /// </summary>
    RequestQueuedForIO,

    /// <summary>
    /// Time the request was delivered to the final request queue for processing.
    /// </summary>
    RequestDeliveredForIO,

    /// <summary>
    /// Time HTTP3 streaming starts.
    /// </summary>
    Http3StreamStart,

    /// <summary>
    /// Time HTTP3 header decoding starts.
    /// </summary>
    Http3HeaderDecodeStart,

    /// <summary>
    /// Time HTTP3 header decoding ends.
    /// </summary>
    Http3HeaderDecodeEnd,
}
