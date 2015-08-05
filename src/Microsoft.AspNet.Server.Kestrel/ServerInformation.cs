// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Framework.Configuration;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServerInformation : IServerInformation, IKestrelServerInformation
    {
        public ServerInformation()
        {
            Addresses = new List<ServerAddress>();
        }

        public void Initialize(IConfiguration configuration)
        {
            string urls;
            if (!configuration.TryGet("server.urls", out urls))
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

        public string Name
        {
            get
            {
                return "Kestrel";
            }
        }

        public IList<ServerAddress> Addresses { get; private set; }

        public int ThreadCount { get; set; }
    }
}
