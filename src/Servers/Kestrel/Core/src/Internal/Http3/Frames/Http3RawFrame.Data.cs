// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http
{
    internal partial class Http3RawFrame
    {
        public void PrepareData()
        {
            Length = 0;
            Type = Http3FrameType.Data;
        }

        public string FormattedType => Type switch
        {
            Http3FrameType.Data => "DATA",
            Http3FrameType.Headers => "HEADERS",
            Http3FrameType.CancelPush => "CANCEL_PUSH",
            Http3FrameType.Settings => "SETTINGS",
            Http3FrameType.PushPromise => "PUSH_PROMISE",
            Http3FrameType.GoAway => "GO_AWAY",
            Http3FrameType.MaxPushId => "MAX_PUSH_ID",
            _ => Type.ToString()
        };
    }
}
