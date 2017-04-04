// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class UvStreamHandleTests
    {
        [Fact]
        public void ReadStopIsIdempotent()
        {
            var libuvTrace = new LibuvTrace(new TestApplicationErrorLogger());

            var mockUvLoopHandle = new Mock<UvLoopHandle>(libuvTrace).Object;
            mockUvLoopHandle.Init(new MockLibuv());

            // Need to mock UvTcpHandle instead of UvStreamHandle, since the latter lacks an Init() method
            var mockUvStreamHandle = new Mock<UvTcpHandle>(libuvTrace).Object;
            mockUvStreamHandle.Init(mockUvLoopHandle, null);

            mockUvStreamHandle.ReadStop();
            mockUvStreamHandle.ReadStop();
        }
    }
}
