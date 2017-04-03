// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MockConnectionInformation : IConnectionInformation
    {
        public ListenOptions ListenOptions { get; }
        public IPEndPoint RemoteEndPoint { get; }
        public IPEndPoint LocalEndPoint { get; }

        public PipeFactory PipeFactory { get; }
        public IScheduler InputWriterScheduler { get; }
        public IScheduler OutputWriterScheduler { get; }

        public ITimeoutControl TimeoutControl { get; } = new MockTimeoutControl();

        private class MockTimeoutControl : ITimeoutControl
        {
            public void CancelTimeout()
            {
            }

            public void ResetTimeout(long milliseconds, TimeoutAction timeoutAction)
            {
            }

            public void SetTimeout(long milliseconds, TimeoutAction timeoutAction)
            {
            }
        }
    }
}
