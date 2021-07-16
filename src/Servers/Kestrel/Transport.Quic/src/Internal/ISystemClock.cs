// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    /// <summary>
    /// Abstracts the system clock to facilitate testing.
    /// </summary>
    internal interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }

    internal class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
