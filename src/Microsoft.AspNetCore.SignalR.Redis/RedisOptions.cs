// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisOptions
    {
        public ConfigurationOptions Configuration { get; set; } = new ConfigurationOptions
        {
            // Enable reconnecting by default
            AbortOnConnectFail = false
        };

        public Func<TextWriter, Task<IConnectionMultiplexer>> ConnectionFactory { get; set; }

        internal async Task<IConnectionMultiplexer> ConnectAsync(TextWriter log)
        {
            // Factory is publically settable. Assigning to a local variable before null check for thread safety.
            var factory = ConnectionFactory;
            if (factory == null)
            {
                // REVIEW: Should we do this?
                if (Configuration.EndPoints.Count == 0)
                {
                    Configuration.EndPoints.Add(IPAddress.Loopback, 0);
                    Configuration.SetDefaultPorts();
                }

                return await ConnectionMultiplexer.ConnectAsync(Configuration, log);
            }

            return await factory(log);
        }
    }
}
