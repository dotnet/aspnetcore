// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.KestrelTests.TestHelpers
{
    public class MockSystemClock : ISystemClock
    {
        private DateTimeOffset _utcNow = DateTimeOffset.Now;

        public DateTimeOffset UtcNow
        {
            get
            {
                UtcNowCalled++;
                return _utcNow;
            }
            set
            {
                _utcNow = value;
            }
        }

        public int UtcNowCalled { get; private set; }
    }
}
