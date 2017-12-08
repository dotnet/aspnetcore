// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace AspNetCoreModule.Test.WebSocketClient
{
    //*  %x0 denotes a continuation frame
    //*  %x1 denotes a text frame
    //*  %x2 denotes a binary frame
    //*  %x3-7 are reserved for further non-control frames
    //*  %x8 denotes a connection close
    //*  %x9 denotes a ping
    //*  %xA denotes a pong
    public enum FrameType
    {
        NonControlFrame,
        Ping,
        Pong,
        Text,
        SegmentedText,
        Binary,
        SegmentedBinary,
        Continuation,
        ContinuationControlled,
        ContinuationFrameEnd,
        Close,
    }
}
