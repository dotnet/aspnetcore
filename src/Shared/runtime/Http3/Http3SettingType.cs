// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal enum Http3SettingType : long
    {
        /// <summary>
        /// SETTINGS_QPACK_MAX_TABLE_CAPACITY
        /// The maximum dynamic table size. The default is 0.
        /// https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-5
        /// </summary>
        QPackMaxTableCapacity = 0x1,

        // Below are explicitly reserved and should never be sent, per
        // https://tools.ietf.org/html/draft-ietf-quic-http-31#section-7.2.4.1
        // and
        // https://tools.ietf.org/html/draft-ietf-quic-http-31#section-11.2.2
        ReservedHttp2EnablePush = 0x2,
        ReservedHttp2MaxConcurrentStreams = 0x3,
        ReservedHttp2InitialWindowSize = 0x4,
        ReservedHttp2MaxFrameSize = 0x5,

        /// <summary>
        /// SETTINGS_MAX_HEADER_LIST_SIZE
        /// The maximum size of headers. The default is unlimited.
        /// https://tools.ietf.org/html/draft-ietf-quic-http-24#section-7.2.4.1
        /// </summary>
        MaxHeaderListSize = 0x6,

        /// <summary>
        /// SETTINGS_QPACK_BLOCKED_STREAMS
        /// The maximum number of request streams that can be blocked waiting for QPack instructions. The default is 0.
        /// https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-5
        /// </summary>
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
}
