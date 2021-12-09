// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Internal;

internal interface ISystemClock
{
    /// <summary>
    /// Retrieves ticks for the current system up time.
    /// </summary>
    long CurrentTicks { get; }
}
