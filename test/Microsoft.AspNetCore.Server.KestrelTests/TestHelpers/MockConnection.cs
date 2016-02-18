// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockConnection : Connection, IDisposable
    {
        public MockConnection()
        {
            RequestAbortedSource = new CancellationTokenSource();
        }

        public override void Abort()
        {
            if (RequestAbortedSource != null)
            {
                RequestAbortedSource.Cancel();
            }
        }

        public override void OnSocketClosed()
        {
        }

        public CancellationTokenSource RequestAbortedSource { get; }

        public void Dispose()
        {
            RequestAbortedSource.Dispose();
        }
    }
}
