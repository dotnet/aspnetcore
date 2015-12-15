// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Server.Features;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelServerInformation : IKestrelServerInformation, IServerAddressesFeature
    {
        public KestrelServerInformation(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Addresses = GetAddresses(configuration);
            ThreadCount = GetThreadCount(configuration);
            NoDelay = GetNoDelay(configuration);
            ReuseStreams = GetReuseStreams(configuration);
        }

        public ICollection<string> Addresses { get; }

        public int ThreadCount { get; set; }

        public bool NoDelay { get; set; }

        public bool ReuseStreams { get; set; }

        public IConnectionFilter ConnectionFilter { get; set; }

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

        private static ICollection<string> GetAddresses(IConfiguration configuration)
        {
            var addresses = new List<string>();

            var urls = configuration["server.urls"];

            if (!string.IsNullOrEmpty(urls))
            {
                addresses.AddRange(urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return addresses;
        }

        private static int GetThreadCount(IConfiguration configuration)
        {
            var threadCountString = configuration["kestrel.threadCount"];

            if (string.IsNullOrEmpty(threadCountString))
            {
                return ProcessorThreadCount;
            }

            int threadCount;
            if (int.TryParse(threadCountString, NumberStyles.Integer, CultureInfo.InvariantCulture, out threadCount))
            {
                return threadCount;
            }

            return ProcessorThreadCount;
        }

        private static bool GetNoDelay(IConfiguration configuration)
        {
            var noDelayString = configuration["kestrel.noDelay"];

            if (string.IsNullOrEmpty(noDelayString))
            {
                return true;
            }

            bool noDelay;
            if (bool.TryParse(noDelayString, out noDelay))
            {
                return noDelay;
            }

            return true;
        }

        private static bool GetReuseStreams(IConfiguration configuration)
        {
            var reuseStreamsString = configuration["kestrel.reuseStreams"];

            bool reuseStreams;
            if (bool.TryParse(reuseStreamsString, out reuseStreams))
            {
                return reuseStreams;
            }

            return false;
        }
    }
}
