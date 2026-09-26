// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestTimeoutControl : ITimeoutControl
{
    public List<(TimeSpan Timeout, TimeoutReason Reason)> SetTimeoutCalls { get; } = new List<(TimeSpan Timeout, TimeoutReason Reason)>();
    public List<(TimeSpan Timeout, TimeoutReason Reason)> ResetTimeoutCalls { get; } = new List<(TimeSpan Timeout, TimeoutReason Reason)>();
    public List<MinDataRate> StartRequestBodyCalls { get; } = new List<MinDataRate>();

    public TimeoutReason TimerReason { get; set; }
    public int CancelTimeoutCount { get; private set; }
    public int InitializeHttp2Count { get; private set; }
    public int TickCount { get; private set; }
    public int StopRequestBodyCount { get; private set; }
    public int StartTimingReadCount { get; private set; }
    public int StopTimingReadCount { get; private set; }
    public int BytesReadCount { get; private set; }
    public int StartTimingWriteCount { get; private set; }
    public int StopTimingWriteCount { get; private set; }
    public int BytesWrittenToBufferCount { get; private set; }
    public Func<long, MinDataRate, long> GetResponseDrainDeadlineFunc { get; set; } = (_, _) => 0;

    public void SetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
        SetTimeoutCalls.Add((timeout, timeoutReason));
    }

    public void ResetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
        ResetTimeoutCalls.Add((timeout, timeoutReason));
    }

    public void CancelTimeout()
    {
        CancelTimeoutCount++;
    }

    public void InitializeHttp2(InputFlowControl connectionInputFlowControl)
    {
        InitializeHttp2Count++;
    }

    public void Tick(long timestamp)
    {
        TickCount++;
    }

    public void StartRequestBody(MinDataRate minRate)
    {
        StartRequestBodyCalls.Add(minRate);
    }

    public void StopRequestBody()
    {
        StopRequestBodyCount++;
    }

    public void StartTimingRead()
    {
        StartTimingReadCount++;
    }

    public void StopTimingRead()
    {
        StopTimingReadCount++;
    }

    public void BytesRead(long count)
    {
        BytesReadCount++;
    }

    public void StartTimingWrite()
    {
        StartTimingWriteCount++;
    }

    public void StopTimingWrite()
    {
        StopTimingWriteCount++;
    }

    public void BytesWrittenToBuffer(MinDataRate minRate, long count)
    {
        BytesWrittenToBufferCount++;
    }

    public long GetResponseDrainDeadline(long timestamp, MinDataRate minRate) => GetResponseDrainDeadlineFunc(timestamp, minRate);
}
