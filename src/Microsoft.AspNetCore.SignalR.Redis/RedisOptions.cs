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
        public ConfigurationOptions Options { get; set; } = new ConfigurationOptions
        {
            // Enable reconnecting by default
            AbortOnConnectFail = false
        };

        public Func<TextWriter, Task<IConnectionMultiplexer>> Factory { get; set; }

        internal async Task<IConnectionMultiplexer> ConnectAsync(TextWriter log)
        {
            // Factory is publically settable. Assigning to a local variable before null check for thread safety.
            var localFactory = Factory;
            if (localFactory == null)
            {
                // REVIEW: Should we do this?
                if (Options.EndPoints.Count == 0)
                {
                    Options.EndPoints.Add(IPAddress.Loopback, 0);
                    Options.SetDefaultPorts();
                }

                return await ConnectionMultiplexer.ConnectAsync(Options, log);
            }

            return await localFactory(log);
        }
    }
}
