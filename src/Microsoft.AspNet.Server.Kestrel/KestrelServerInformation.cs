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
        public KestrelServerInformation(IConfiguration configuration, int threadCount)
        {
            Addresses = GetAddresses(configuration);
            ThreadCount = threadCount;
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
    }
}
