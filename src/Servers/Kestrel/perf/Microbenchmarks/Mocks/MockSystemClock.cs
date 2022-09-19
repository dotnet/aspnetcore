// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

internal sealed class MockSystemClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; }
    public long UtcNowTicks { get; }
    public DateTimeOffset UtcNowUnsynchronized { get; }
}
