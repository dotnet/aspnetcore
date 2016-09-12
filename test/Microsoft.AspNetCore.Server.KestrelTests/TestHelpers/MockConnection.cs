// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockConnection : Connection, IDisposable
    {
        private readonly TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>();

        public MockConnection(KestrelServerOptions options)
        {
            ConnectionControl = this;
            RequestAbortedSource = new CancellationTokenSource();
            ServerOptions = options;
        }

        public override void Abort(Exception error = null)
        {
            RequestAbortedSource?.Cancel();
        }

        public override void OnSocketClosed()
        {
            _socketClosedTcs.SetResult(null);
        }

        public CancellationTokenSource RequestAbortedSource { get; }

        public Task SocketClosed => _socketClosedTcs.Task;

        public void Dispose()
        {
            RequestAbortedSource.Dispose();
        }
    }
}
