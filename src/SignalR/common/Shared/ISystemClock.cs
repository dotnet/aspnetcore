// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    internal interface ISystemClock
    {
        /// <summary>
        /// Retrieves ticks for the current system up time.
        /// </summary>
        long CurrentTick { get; }
    }
}
