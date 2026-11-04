// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class TimeoutControlExtensions
{
    public static void StartDrainTimeout(this ITimeoutControl timeoutControl, MinDataRate? minDataRate, long? maxResponseBufferSize)
    {
        // If maxResponseBufferSize has no value, there's no backpressure and we can't reasonably time out draining.
        if (minDataRate == null || maxResponseBufferSize == null)
        {
            return;
        }

        // Ensure we have at least the grace period from this point to finish draining the response.
        timeoutControl.BytesWrittenToBuffer(minDataRate, 1);
        timeoutControl.StartTimingWrite();
    }
}
