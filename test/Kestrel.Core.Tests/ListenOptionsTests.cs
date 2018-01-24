// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ListenOptionsTests
    {
        [Fact]
        public void ProtocolsDefault()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            Assert.Equal(HttpProtocols.Http1, listenOptions.Protocols);
        }

        [Fact]
        public void Http2DisabledByDefault()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            var ex = Assert.Throws<NotSupportedException>(() => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
            Assert.Equal(CoreStrings.Http2NotSupported, ex.Message);
            ex = Assert.Throws<NotSupportedException>(() => listenOptions.Protocols = HttpProtocols.Http2);
            Assert.Equal(CoreStrings.Http2NotSupported, ex.Message);
        }
    }
}
