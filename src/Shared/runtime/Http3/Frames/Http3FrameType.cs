// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http
{
    internal enum Http3FrameType : long
    {
        Data = 0x0,
        Headers = 0x1,
        CancelPush = 0x3,
        Settings = 0x4,
        PushPromise = 0x5,
        GoAway = 0x7,
        MaxPushId = 0xD,
        DuplicatePush = 0xE
    }
}
