// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    internal class MockSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; }
        public long UtcNowTicks { get; }
        public DateTimeOffset UtcNowUnsynchronized { get; }
    }
}
