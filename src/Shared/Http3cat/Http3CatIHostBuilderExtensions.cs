// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http3Cat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

internal static class Http3CatIHostBuilderExtensions
{
    public static IHostBuilder UseHttp3Cat(this IHostBuilder hostBuilder, string address, Func<Http3Utilities, Task> scenario)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.UseHttp3Cat(options =>
            {
                options.Url = address;
                options.Scenaro = scenario;
            });
        });
        return hostBuilder;
    }
}
