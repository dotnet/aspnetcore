// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal enum Http3SettingType : long
{
    // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-5
    QPackMaxTableCapacity = 0x1,
    /// <summary>
    /// SETTINGS_MAX_FIELD_SECTION_SIZE, default is unlimited.
    /// https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-5
    /// </summary>
    MaxFieldSectionSize = 0x6,
    // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#section-5
    QPackBlockedStreams = 0x7,

    /// <summary>
    /// SETTINGS_ENABLE_WEBTRANSPORT, default is 0 (off)
    /// https://www.ietf.org/archive/id/draft-ietf-webtrans-http3-01.html#name-http-3-settings-parameter-r
    /// </summary>
    EnableWebTransport = 0x2b603742,

    /// <summary>
    /// H3_DATAGRAM, default is 0 (off)
    /// indicates that the server suppprts sending individual datagrams over Http/3
    /// rather than just streams.
    /// </summary>
    H3Datagram = 0xffd277
}
