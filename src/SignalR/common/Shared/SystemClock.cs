// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    /// <summary>
    /// Provides access to the normal system clock.
    /// </summary>
    internal class SystemClock : ISystemClock
    {
        /// <inheritdoc />
        public long CurrentTick => Environment.TickCount64;
    }
}
