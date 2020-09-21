// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal enum Http2FrameType : byte
    {
        DATA = 0x0,
        HEADERS = 0x1,
        PRIORITY = 0x2,
        RST_STREAM = 0x3,
        SETTINGS = 0x4,
        PUSH_PROMISE = 0x5,
        PING = 0x6,
        GOAWAY = 0x7,
        WINDOW_UPDATE = 0x8,
        CONTINUATION = 0x9
    }
}
