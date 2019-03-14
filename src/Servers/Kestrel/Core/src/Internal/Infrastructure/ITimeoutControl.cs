// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Infrastructure
{
    internal interface ITimeoutControl
    {
        TimeoutReason TimerReason { get; }

        void SetTimeout(long ticks, TimeoutReason timeoutReason);
        void ResetTimeout(long ticks, TimeoutReason timeoutReason);
        void CancelTimeout();

        void InitializeHttp2(InputFlowControl connectionInputFlowControl);
        void StartRequestBody(MinDataRate minRate);
        void StopRequestBody();
        void StartTimingRead();
        void StopTimingRead();
        void BytesRead(long count);

        void StartTimingWrite();
        void StopTimingWrite();
        void BytesWrittenToBuffer(MinDataRate minRate, long count);
    }
}
