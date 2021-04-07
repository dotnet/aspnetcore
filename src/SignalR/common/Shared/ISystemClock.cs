// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Internal
{
    internal interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current UTC system time.
        /// </summary>
        DateTimeOffset UtcNow { get; }

        /// <summary>
        /// Retrieves ticks for the current UTC system time.
        /// </summary>
        long UtcNowTicks { get; }

        /// <summary>
        /// Retrieves the current UTC system time.
        /// This is only safe to use from code called by the Heartbeat.
        /// </summary>
        DateTimeOffset UtcNowUnsynchronized { get; }
    }
}
