// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests.TestHelpers
{
    public class MockFrameControl : IFrameControl
    {
        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        public void ProduceContinue()
        {
        }

        public void Write(ArraySegment<byte> data)
        {
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }
    }
}
