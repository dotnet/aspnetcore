// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheOptions : IOptions<MemoryCacheOptions>
    {
        private long? _sizeLimit;
        private double _compactionPercentage = 0.05;

        public ISystemClock Clock { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum size of the cache.
        /// </summary>
        public long? SizeLimit
        {
            get => _sizeLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _sizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the amount to compact the cache by when the maximum size is exceeded.
        /// </summary>
        public double CompactionPercentage
        {
            get => _compactionPercentage;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be between 0 and 1 inclusive.");
                }

                _compactionPercentage = value;
            }
        }

        MemoryCacheOptions IOptions<MemoryCacheOptions>.Value
        {
            get { return this; }
        }
    }
}
