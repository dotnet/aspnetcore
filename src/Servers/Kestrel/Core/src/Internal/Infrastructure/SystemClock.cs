// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    /// <summary>
    /// Provides access to the normal system clock.
    /// </summary>
    internal class SystemClock : ISystemClock
    {
        /// <summary>
        /// Retrieves the current UTC system time.
        /// </summary>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Retrieves ticks for the current UTC system time.
        /// </summary>
        public long UtcNowTicks => DateTimeOffset.UtcNow.Ticks;

        /// <summary>
        /// Retrieves the current UTC system time.
        /// </summary>
        public DateTimeOffset UtcNowUnsynchronized => DateTimeOffset.UtcNow;
    }
}
