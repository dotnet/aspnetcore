// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

public enum HttpSysRequestTimingType
{
    ConnectionStart,
    DataStart,
    TlsCertificateLoadStart,
    TlsCertificateLoadEnd,
    TlsHandshakeLeg1Start,
    TlsHandshakeLeg1End,
    TlsHandshakeLeg2Start,
    TlsHandshakeLeg2End,
    TlsAttributesQueryStart,
    TlsAttributesQueryEnd,
    TlsClientCertQueryStart,
    TlsClientCertQueryEnd,
    Http2StreamStart,
    Http2HeaderDecodeStart,
    Http2HeaderDecodeEnd,
    RequestHeaderParseStart,
    RequestHeaderParseEnd,
    RequestRoutingStart,
    RequestRoutingEnd,
    RequestQueuedForInspection,
    RequestDeliveredForInspection,
    RequestReturnedAfterInspection,
    RequestQueuedForDelegation,
    RequestDeliveredForDelegation,
    RequestReturnedAfterDelegation,
    RequestQueuedForIO,
    RequestDeliveredForIO,
    Http3StreamStart,
    Http3HeaderDecodeStart,
    Http3HeaderDecodeEnd,
}
