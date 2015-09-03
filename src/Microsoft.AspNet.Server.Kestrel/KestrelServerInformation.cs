// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Configuration;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelServerInformation : IKestrelServerInformation
    {
        public KestrelServerInformation()
        {
            Addresses = new List<ServerAddress>();
        }

        public IList<ServerAddress> Addresses { get; private set; }

        public int ThreadCount { get; set; }

        public void Initialize(IConfiguration configuration)
        {
            var urls = configuration["server.urls"];
            if (string.IsNullOrEmpty(urls))
            {
                urls = "http://+:5000/";
            }
            foreach (var url in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var address = ServerAddress.FromUrl(url);
                if (address != null)
                {
                    Addresses.Add(address);
                }
            }
        }
    }
}
