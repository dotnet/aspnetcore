// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance.Mocks
{
    public class MockTimeoutControl : ITimeoutControl
    {
        public bool TimedOut { get; }

        public void CancelTimeout()
        {
        }

        public void ResetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }

        public void SetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }

        public void StartTimingReads()
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

        public void BytesRead(int count)
        {
        }
    }
}
