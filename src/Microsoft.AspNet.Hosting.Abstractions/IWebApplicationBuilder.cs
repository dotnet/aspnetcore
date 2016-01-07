// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public interface IWebApplicationBuilder
    {
        IWebApplication Build();

        IWebApplicationBuilder UseConfiguration(IConfiguration configuration);

        IWebApplicationBuilder UseServer(IServerFactory factory);

        IWebApplicationBuilder UseStartup(Type startupType);

        IWebApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        IWebApplicationBuilder Configure(Action<IApplicationBuilder> configureApplication);

        IWebApplicationBuilder UseSetting(string key, string value);
    }
}