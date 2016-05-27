// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockFrameControl : IFrameControl
    {
        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            return TaskUtilities.CompletedTask;
        }

        public void ProduceContinue()
        {
        }

        public void Write(ArraySegment<byte> data)
        {
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return TaskUtilities.CompletedTask;
        }
    }
}
