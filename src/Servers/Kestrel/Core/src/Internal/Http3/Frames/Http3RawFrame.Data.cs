// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

internal partial class Http3RawFrame
{
    public void PrepareData()
    {
        RemainingLength = 0;
        Type = Http3FrameType.Data;
    }
}
