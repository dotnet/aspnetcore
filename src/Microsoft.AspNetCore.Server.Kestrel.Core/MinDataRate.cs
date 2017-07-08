// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class MinDataRate
    {
        /// <summary>
        /// Creates a new instance of <see cref="MinDataRate"/>.
        /// </summary>
        /// <param name="bytesPerSecond">The minimum rate in bytes/second at which data should be processed.</param>
        /// <param name="gracePeriod">The amount of time to delay enforcement of <paramref name="bytesPerSecond"/>,
        /// starting at the time data is first read or written.</param>
        public MinDataRate(double bytesPerSecond, TimeSpan gracePeriod)
        {
            if (bytesPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesPerSecond), CoreStrings.PositiveNumberOrNullMinDataRateRequired);
            }

            if (gracePeriod <= Heartbeat.Interval)
            {
                throw new ArgumentOutOfRangeException(nameof(gracePeriod), CoreStrings.FormatMinimumGracePeriodRequired(Heartbeat.Interval.TotalSeconds));
            }

            BytesPerSecond = bytesPerSecond;
            GracePeriod = gracePeriod;
        }

        /// <summary>
        /// The minimum rate in bytes/second at which data should be processed.
        /// </summary>
        public double BytesPerSecond { get; }

        /// <summary>
        /// The amount of time to delay enforcement of <see cref="MinDataRate" />,
        /// starting at the time data is first read or written.
        /// </summary>
        public TimeSpan GracePeriod { get; }
    }
}
