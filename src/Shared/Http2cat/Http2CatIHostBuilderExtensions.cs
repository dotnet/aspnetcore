// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    internal static class Http2CatIHostBuilderExtensions
    {
        public static IHostBuilder UseHttp2Cat(this IHostBuilder hostBuilder, string address, Func<Http2Utilities, Task> scenario)
        {
            hostBuilder.ConfigureServices(services =>
            {
                services.UseHttp2Cat(options =>
                {
                    options.Url = address;
                    options.Scenaro = scenario;
                });
            });
            return hostBuilder;
         }
    }
}
