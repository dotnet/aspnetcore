// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
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

        public Func<TextWriter, IConnectionMultiplexer> Factory { get; set; }

        // TODO: Async
        internal IConnectionMultiplexer Connect(TextWriter log)
        {
            if (Factory == null)
            {
                // REVIEW: Should we do this?
                if (Options.EndPoints.Count == 0)
                {
                    Options.EndPoints.Add(IPAddress.Loopback, 0);
                    Options.SetDefaultPorts();
                }

                return ConnectionMultiplexer.Connect(Options, log);
            }

            return Factory(log);
        }
    }
}
