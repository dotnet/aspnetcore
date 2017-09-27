// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
