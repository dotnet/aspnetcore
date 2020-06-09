// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        QPackBlockedStreams = 0x7
    }
}
