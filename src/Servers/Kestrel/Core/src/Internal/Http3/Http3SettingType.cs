// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    enum Http3SettingType : long
    {
        QPackMaxTableCapacity = 0x1,
        /// <summary>
        /// SETTINGS_MAX_HEADER_LIST_SIZE, default is unlimited.
        /// </summary>
        MaxHeaderListSize = 0x6,
        QPackBlockedStreams = 0x7
    }
}
