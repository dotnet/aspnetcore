// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

public static class FlushReportingStream
{
    public static Stream GetThrowingStream()
    {
        return new NonFlushingStream();
    }

    private class NonFlushingStream : MemoryStream
    {
        public override void Flush()
        {
            throw new InvalidOperationException("Flush should not have been called.");
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("FlushAsync should not have been called.");
        }
    }
}
