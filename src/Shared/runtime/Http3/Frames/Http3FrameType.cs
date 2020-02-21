// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
