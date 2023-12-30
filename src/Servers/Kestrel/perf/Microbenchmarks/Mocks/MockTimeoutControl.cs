// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

internal sealed class MockTimeoutControl : ITimeoutControl
{
    public TimeoutReason TimerReason { get; } = TimeoutReason.KeepAlive;

    public void BytesRead(long count)
    {
    }

    public void BytesWrittenToBuffer(MinDataRate minRate, long count)
    {
    }

    public void CancelTimeout()
    {
    }

    public long GetResponseDrainDeadline(long ticks, MinDataRate minRate)
    {
        return 0;
    }

    public void InitializeHttp2(InputFlowControl connectionInputFlowControl)
    {
    }

    public void ResetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
    }

    public void SetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
    }

    public void StartRequestBody(MinDataRate minRate)
    {
    }

    public void StartTimingRead()
    {
    }

    public void StartTimingWrite()
    {
    }

    public void StopRequestBody()
    {
    }

    public void StopTimingRead()
    {
    }

    public void StopTimingWrite()
    {
    }

    public void Tick(long timestamp)
    {
    }
}
