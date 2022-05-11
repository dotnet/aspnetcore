// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Internal;

/// <summary>
/// Provides access to the normal system clock.
/// </summary>
internal sealed class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public long CurrentTicks => Environment.TickCount64;
}
