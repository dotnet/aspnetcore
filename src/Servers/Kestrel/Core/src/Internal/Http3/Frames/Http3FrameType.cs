// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal enum Http3FrameType
    {
        DATA = 0x0,
        HEADERS = 0x1,
        CANCEL_PUSH = 0x3,
        SETTINGS = 0x4,
        PUSH_PROMISE = 0x5,
        GOAWAY = 0x7,
        MAX_PUSH_ID = 0xD,
        DUPLICATE_PUSH = 0xE
    }
}
