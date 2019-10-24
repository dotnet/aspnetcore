// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    enum Http3SettingType : long
    {
        QPACK_MAX_TABLE_CAPACITY = 0x1,
        MAX_HEADER_LIST_SIZE = 0x6,
        QPACK_BLOCKED_STREAMS = 0x7
    }
}
