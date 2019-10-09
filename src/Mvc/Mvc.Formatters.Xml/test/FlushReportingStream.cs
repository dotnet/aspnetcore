// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.AspNetCore.Mvc
{
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
}