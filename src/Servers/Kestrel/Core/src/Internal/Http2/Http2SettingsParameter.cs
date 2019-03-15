// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal enum Http2SettingsParameter : ushort
    {
        SETTINGS_HEADER_TABLE_SIZE = 0x1,
        SETTINGS_ENABLE_PUSH = 0x2,
        SETTINGS_MAX_CONCURRENT_STREAMS = 0x3,
        SETTINGS_INITIAL_WINDOW_SIZE = 0x4,
        SETTINGS_MAX_FRAME_SIZE = 0x5,
        SETTINGS_MAX_HEADER_LIST_SIZE = 0x6,
    }
}
