// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Internal
{
    /// <summary>
    /// Abstracts the system clock to facilitate testing.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }
}
