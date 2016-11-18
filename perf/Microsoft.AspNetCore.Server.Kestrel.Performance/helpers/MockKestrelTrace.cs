// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MockKestrelTrace : KestrelTrace
    {
        public MockKestrelTrace() : this(new MockApplicationErrorLogger())
        {
        }

        public MockKestrelTrace(MockApplicationErrorLogger testLogger) : base(testLogger)
        {
            Logger = testLogger;
        }

        public MockApplicationErrorLogger Logger { get; private set; }

        public override void ConnectionRead(string connectionId, int count)
        {
        }

        public override void ConnectionWrite(string connectionId, int count)
        {
        }

        public override void ConnectionWriteCallback(string connectionId, int status)
        {
        }
    }
}