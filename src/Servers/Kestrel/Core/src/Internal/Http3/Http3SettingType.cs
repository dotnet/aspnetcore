// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
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
        QPackBlockedStreams = 0x7
    }
}
