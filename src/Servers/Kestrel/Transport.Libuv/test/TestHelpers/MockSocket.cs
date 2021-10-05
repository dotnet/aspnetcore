// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    class MockSocket : UvStreamHandle
    {
        public MockSocket(LibuvFunctions uv, int threadId, ILogger logger) : base(logger)
        {
            CreateMemory(uv, threadId, IntPtr.Size);
        }

        protected override bool ReleaseHandle()
        {
            DestroyMemory(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }
}
