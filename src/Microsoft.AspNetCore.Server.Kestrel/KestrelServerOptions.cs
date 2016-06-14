// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Filter;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServerOptions
    {
        // Matches the default client_max_body_size in nginx.  Also large enough that most requests
        // should be under the limit.
        private long? _maxRequestBufferSize = 1024 * 1024;

        /// <summary>
        /// Gets or sets whether the <c>Server</c> header should be included in each response.
        /// </summary>
        public bool AddServerHeader { get; set; } = true;

        public IServiceProvider ApplicationServices { get; set; }

        public IConnectionFilter ConnectionFilter { get; set; }

        /// <summary>
        /// Maximum size of the request buffer.  Default is 1,048,576 bytes (1 MB).
        /// If value is null, the size of the request buffer is unlimited.
        /// </summary>
        public long? MaxRequestBufferSize
        {
            get
            {
                return _maxRequestBufferSize;
            }
            set
            {
                if (value.HasValue && value.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be null or a positive integer.");
                }
                _maxRequestBufferSize = value;
            }
        }

        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// The amount of time after the server begins shutting down before connections will be forcefully closed.
        /// By default, Kestrel will wait 5 seconds for any ongoing requests to complete before terminating
        /// the connection.
        /// </summary>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public int ThreadCount { get; set; } = ProcessorThreadCount;

        private static int ProcessorThreadCount
        {
            get
            {
                // Actual core count would be a better number
                // rather than logical cores which includes hyper-threaded cores.
                // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
                var threadCount = Environment.ProcessorCount >> 1;

                if (threadCount < 1)
                {
                    // Ensure shifted value is at least one
                    return 1;
                }

                if (threadCount > 16)
                {
                    // Receive Side Scaling RSS Processor count currently maxes out at 16
                    // would be better to check the NIC's current hardware queues; but xplat...
                    return 16;
                }

                return threadCount;
            }
        }
    }
}
