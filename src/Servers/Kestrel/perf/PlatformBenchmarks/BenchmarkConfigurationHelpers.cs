// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace PlatformBenchmarks
{
    public static class BenchmarkConfigurationHelpers
    {
        public static IWebHostBuilder UseBenchmarksConfiguration(this IWebHostBuilder builder, IConfiguration configuration)
        {
            builder.UseConfiguration(configuration);

            // Handle the transport type
            var webHost = builder.GetSetting("KestrelTransport");

            // Handle the thread count
            var threadCountRaw = builder.GetSetting("threadCount");
            int? theadCount = null;

            if (!string.IsNullOrEmpty(threadCountRaw) &&
                Int32.TryParse(threadCountRaw, out var value))
            {
                theadCount = value;
            }

            if (string.Equals(webHost, "Libuv", StringComparison.OrdinalIgnoreCase))
            {
                builder.UseLibuv(options =>
                {
                    if (theadCount.HasValue)
                    {
                        options.ThreadCount = theadCount.Value;
                    }
                });
            }
            else if (string.Equals(webHost, "Sockets", StringComparison.OrdinalIgnoreCase))
            {
                builder.UseSockets(options =>
                {
                    if (theadCount.HasValue)
                    {
                        options.IOQueueCount = theadCount.Value;
                    }
                });
            }

            return builder;
        }

        public static IPEndPoint CreateIPEndPoint(this IConfiguration config)
        {
            var url = config["server.urls"] ?? config["urls"];

            if (string.IsNullOrEmpty(url))
            {
                return new IPEndPoint(IPAddress.Loopback, 8080);
            }

            var address = BindingAddress.Parse(url);

            IPAddress ip;

            if (string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                ip = IPAddress.Loopback;
            }
            else if (!IPAddress.TryParse(address.Host, out ip))
            {
                ip = IPAddress.IPv6Any;
            }

            return new IPEndPoint(ip, address.Port);
        }
    }
}
