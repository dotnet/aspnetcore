// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public static class TimeoutControlExtensions
    {
        public static void StartDrainTimeout(this ITimeoutControl timeoutControl, MinDataRate minDataRate, long? maxResponseBufferSize)
        {
            // If maxResponseBufferSize has no value, there's no backpressure and we can't reasonably timeout draining.
            if (minDataRate == null || maxResponseBufferSize == null)
            {
                return;
            }

            // With full backpressure and a connection adapter there could be 2 two pipes buffering.
            // We already validate that the buffer size is positive.
            // There's no reason to stop timing the write after the connection is closed.
            var oneBufferSize = maxResponseBufferSize.Value;
            var maxBufferedBytes = oneBufferSize < long.MaxValue / 2 ? oneBufferSize * 2 : long.MaxValue;
            timeoutControl.StartTimingWrite(minDataRate, maxBufferedBytes);
        }
    }
}
