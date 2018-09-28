// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance.Mocks
{
    public class MockTimeoutControl : ITimeoutControl
    {
        public void CancelTimeout()
        {
        }

        public void ResetTimeout(long ticks, TimeoutReason timeoutReason)
        {
        }

        public void SetTimeout(long ticks, TimeoutReason timeoutReason)
        {
        }

        public void StartTimingReads(MinDataRate minRate)
        {
        }

        public void StopTimingReads()
        {
        }

        public void PauseTimingReads()
        {
        }

        public void ResumeTimingReads()
        {
        }

        public void BytesRead(long count)
        {
        }

        public void StartTimingWrite(MinDataRate rate, long size)
        {
        }

        public void StopTimingWrite()
        {
        }
    }
}
