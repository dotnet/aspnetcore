// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public interface ITimeoutControl
    {
        void SetTimeout(long ticks, TimeoutReason timeoutReason);
        void ResetTimeout(long ticks, TimeoutReason timeoutReason);
        void CancelTimeout();

        void StartTimingReads(MinDataRate minRate);
        void PauseTimingReads();
        void ResumeTimingReads();
        void StopTimingReads();
        void BytesRead(long count);

        void StartTimingWrite(MinDataRate minRate, long size);
        void StopTimingWrite();
    }
}
