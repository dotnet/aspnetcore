// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Server.Features;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelServerInformation : IKestrelServerInformation, IServerAddressesFeature
    {
        public KestrelServerInformation(IConfiguration configuration)
        {
            Addresses = GetAddresses(configuration);
            ThreadCount = GetThreadCount(configuration);
            NoDelay = true;
        }

        public ICollection<string> Addresses { get; }

        public int ThreadCount { get; set; }

        public bool NoDelay { get; set; }

        public IConnectionFilter ConnectionFilter { get; set; }

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
            // Actual core count would be a better number
            // rather than logical cores which includes hyper-threaded cores.
            // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
            // Can be user overriden using IKestrelServerInformation.ThreadCount
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
