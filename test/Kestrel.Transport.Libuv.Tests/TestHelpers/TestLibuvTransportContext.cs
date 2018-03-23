// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    public class TestLibuvTransportContext : LibuvTransportContext
    {
        public TestLibuvTransportContext()
        {
            var logger = new TestApplicationErrorLogger();

            AppLifetime = new LifetimeNotImplemented();
            ConnectionDispatcher = new MockConnectionDispatcher();
            Log = new LibuvTrace(logger);
            Options = new LibuvTransportOptions { ThreadCount = 1 };
        }
    }
}
