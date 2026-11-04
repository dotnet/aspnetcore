// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal interface ITimeoutControl
{
    TimeoutReason TimerReason { get; }

    void SetTimeout(TimeSpan timeout, TimeoutReason timeoutReason);
    void ResetTimeout(TimeSpan timeout, TimeoutReason timeoutReason);
    void CancelTimeout();

    void InitializeHttp2(InputFlowControl connectionInputFlowControl);
    void Tick(long timestamp);

    void StartRequestBody(MinDataRate minRate);
    void StopRequestBody();
    void StartTimingRead();
    void StopTimingRead();
    void BytesRead(long count);

    void StartTimingWrite();
    void StopTimingWrite();
    void BytesWrittenToBuffer(MinDataRate minRate, long count);
    long GetResponseDrainDeadline(long timestamp, MinDataRate minRate);
}
